using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace BCom.CrossPlatform;

/// <summary>
/// Encapsulates serial port lifecycle, background scanning, data streaming, and hardware signals.
/// Completely decoupled from UI rendering.
/// </summary>
public sealed class SerialService : IDisposable
{
    private SerialPort? _port;
    private readonly object _receiveQueueLock = new();
    private readonly Queue<byte[]> _pendingReceive = new();
    private int _pendingReceiveBytes;
    private bool _drainScheduled;
    private long _droppedDisplayBytes;
    private const int MaxDisplayQueueBytes = 512 * 1024;
    public long DroppedDisplayBytes => Interlocked.Read(ref _droppedDisplayBytes);
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly DispatcherTimer _scanTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _periodicTimer = new();
    private Action? _periodicAction;
    private string[] _lastPorts = Array.Empty<string>();

    public event Action<byte[]>? DataReceived;
    // Invoked on the serial worker before UI dispatch; safe for nonblocking capture.
    public event Action<DateTimeOffset, byte[]>? RawDataReceived;
    public event Action<bool, string>? ConnectionChanged;
    public event Action<string>? ErrorOccurred;
    public event Action<string[]>? PortsScanned;

    public bool IsOpen => _port?.IsOpen == true;
    public string CurrentPortName => _port?.PortName ?? string.Empty;

    public SerialService()
    {
        _scanTimer.Tick += (_, _) => CheckPorts();
        _periodicTimer.Tick += (_, _) => _periodicAction?.Invoke();
    }

    public void StartPortScanning()
    {
        CheckPorts();
        _scanTimer.Start();
    }

    public void StopPortScanning()
    {
        _scanTimer.Stop();
    }

    public string[] GetAvailablePorts()
    {
        try
        {
            return SerialPort.GetPortNames().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(I18n.Format("ErrorPorts", ex.Message));
            return Array.Empty<string>();
        }
    }

    public void CheckPorts()
    {
        var current = GetAvailablePorts();
        if (!_lastPorts.SequenceEqual(current))
        {
            _lastPorts = current;
            PortsScanned?.Invoke(current);
        }

        // SerialPort.IsOpen may stay true after a USB serial adapter is removed.
        if (IsOpen && !current.Contains(CurrentPortName, StringComparer.OrdinalIgnoreCase))
        {
            Close();
            ErrorOccurred?.Invoke(I18n.T("ErrorDisconnected"));
        }
    }

    public bool Open(SerialConfig config, out string? errorMessage)
    {
        errorMessage = null;
        if (IsOpen) Close();

        if (string.IsNullOrWhiteSpace(config.PortName))
        {
            errorMessage = I18n.T("ErrorNoPort");
            return false;
        }

        SerialPort? port = null;
        try
        {
            port = new SerialPort(config.PortName)
            {
                BaudRate = config.BaudRate,
                DataBits = config.DataBits,
                StopBits = config.StopBits,
                Parity = config.Parity,
                Encoding = System.Text.Encoding.UTF8,
                RtsEnable = config.RtsEnable,
                DtrEnable = config.DtrEnable,
                Handshake = config.Handshake,
                WriteTimeout = 1500,
                ReadTimeout = 1500
            };

            port.DataReceived += OnSerialDataReceived;
            port.Open();
            _port = port;

            ConnectionChanged?.Invoke(true, config.PortName);
            return true;
        }
        catch (Exception ex)
        {
            if (port != null)
            {
                port.DataReceived -= OnSerialDataReceived;
                port.Dispose();
            }
            errorMessage = I18n.Format("ErrorOpen", config.PortName, ex.Message);
            ErrorOccurred?.Invoke(errorMessage);
            return false;
        }
    }

    public void Close()
    {
        StopPeriodicSend();
        var port = _port;
        _port = null;
        lock (_receiveQueueLock)
        {
            _pendingReceive.Clear();
            _pendingReceiveBytes = 0;
        }

        if (port != null)
        {
            port.DataReceived -= OnSerialDataReceived;
            try
            {
                if (port.IsOpen) port.Close();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(I18n.Format("ErrorClose", ex.Message));
            }
            finally
            {
                port.Dispose();
            }
        }

        ConnectionChanged?.Invoke(false, string.Empty);
    }

    public (bool Cts, bool Dsr, bool Carrier)? LineStatus()
    {
        try { var port = _port; return port?.IsOpen == true ?
            (port.CtsHolding, port.DsrHolding, port.CDHolding) : null; }
        catch (Exception) { return null; }
    }

    public void SetSignals(bool rts, bool dtr)
    {
        if (_port?.IsOpen != true) return;
        try
        {
            _port.RtsEnable = rts;
            _port.DtrEnable = dtr;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(I18n.Format("ErrorOpen", _port.PortName, ex.Message));
        }
    }

    public bool Write(byte[] data, out string? errorMessage)
    {
        errorMessage = null;
        if (!IsOpen || _port == null)
        {
            errorMessage = I18n.T("ErrorNotOpen");
            return false;
        }

        try
        {
            _port.Write(data, 0, data.Length);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = I18n.Format("ErrorSend", ex.Message);
            ErrorOccurred?.Invoke(errorMessage);
            return false;
        }
    }

    /// <summary>
    /// Serializes outgoing writes on a worker thread so a blocked device cannot
    /// freeze Avalonia's dispatcher. Close/disconnect errors are reported to
    /// the caller without accessing UI controls from a worker thread.
    /// </summary>
    public async Task<(bool Success, string? Error)> WriteAsync(
        byte[] data, CancellationToken cancellationToken = default)
    {
        if (data.Length == 0) return (true, null);

        try
        {
            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return (false, "Send cancelled.");
        }

        try
        {
            var port = _port;
            if (port?.IsOpen != true)
                return (false, I18n.T("ErrorNotOpen"));

            await Task.Run(() => port.Write(data, 0, data.Length), cancellationToken)
                .ConfigureAwait(false);
            return (true, null);
        }
        catch (OperationCanceledException)
        {
            return (false, "Send cancelled.");
        }
        catch (Exception ex)
        {
            var message = I18n.Format("ErrorSend", ex.Message);
            Dispatcher.UIThread.Post(() => ErrorOccurred?.Invoke(message));
            return (false, message);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public void StartPeriodicSend(int intervalMs, Action action)
    {
        _periodicTimer.Stop();
        if (intervalMs <= 0) return;

        _periodicAction = action;
        _periodicTimer.Interval = TimeSpan.FromMilliseconds(intervalMs);
        _periodicTimer.Start();
    }

    public void StopPeriodicSend()
    {
        _periodicTimer.Stop();
        _periodicAction = null;
    }

    private void QueueForDisplay(byte[] bytes)
    {
        bool schedule = false;
        lock (_receiveQueueLock)
        {
            if (bytes.Length > MaxDisplayQueueBytes ||
                _pendingReceiveBytes + bytes.Length > MaxDisplayQueueBytes)
            {
                Interlocked.Add(ref _droppedDisplayBytes, bytes.Length);
                return;
            }
            _pendingReceive.Enqueue(bytes);
            _pendingReceiveBytes += bytes.Length;
            if (!_drainScheduled)
            {
                _drainScheduled = true;
                schedule = true;
            }
        }
        if (schedule) Dispatcher.UIThread.Post(DrainDisplayQueue);
    }

    private void DrainDisplayQueue()
    {
        int processed = 0;
        int packets = 0;
        while (processed < 128 * 1024 && packets < 32)
        {
            byte[] next;
            lock (_receiveQueueLock)
            {
                if (_pendingReceive.Count == 0)
                {
                    _drainScheduled = false;
                    return;
                }
                next = _pendingReceive.Dequeue();
                _pendingReceiveBytes -= next.Length;
            }
            DataReceived?.Invoke(next);
            processed += next.Length;
            packets++;
        }
        // Keep UI input responsive even under sustained high-baud serial traffic.
        Dispatcher.UIThread.Post(DrainDisplayQueue, DispatcherPriority.Background);
    }

    private void OnSerialDataReceived(object? sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (sender is not SerialPort port || !port.IsOpen) return;
            // Drain every available byte in bounded chunks. Reading only 64 KiB once
            // can strand remaining bytes if a driver does not raise another callback.
            while (port.IsOpen && ReferenceEquals(port, _port))
            {
                int length = Math.Min(port.BytesToRead, 64 * 1024);
                if (length <= 0) break;
                byte[] buffer = new byte[length];
                int read = port.Read(buffer, 0, length);
                if (read <= 0) break;
                if (read < length) Array.Resize(ref buffer, read);
                if (!ReferenceEquals(port, _port)) break;
                RawDataReceived?.Invoke(DateTimeOffset.UtcNow, buffer);
                QueueForDisplay(buffer);
            }
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => ErrorOccurred?.Invoke(I18n.Format("ErrorReceive", ex.Message)));
        }
    }

    public void Dispose()
    {
        StopPortScanning();
        Close();
    }
}
