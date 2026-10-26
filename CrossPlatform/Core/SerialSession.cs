using System;
using System.Threading;
using System.Threading.Tasks;

namespace BCom.CrossPlatform;

/// <summary>
/// Owns exactly one physical serial connection and all mutable state for its tab.
/// No receive or transmit data is shared across sessions.
/// </summary>
public sealed class SerialSession : IDisposable
{
    private bool _disposed;
    private bool _sending;
    private SerialCapture? _capture;
    private long _lastCaptureDroppedBytes;

    public SerialSession(SerialConfig config, SerialService? service = null)
    {
        Config = new SerialConfig
        {
            PortName = config.PortName,
            BaudRate = config.BaudRate,
            DataBits = config.DataBits,
            StopBits = config.StopBits,
            Parity = config.Parity,
            RtsEnable = config.RtsEnable,
            DtrEnable = config.DtrEnable,
            Handshake = config.Handshake
        };
        Service = service ?? new SerialService();
        Service.DataReceived += OnReceived;
        Service.RawDataReceived += OnRawReceived;
        Service.ConnectionChanged += OnConnectionChanged;
        Service.ErrorOccurred += OnError;
    }

    public SerialConfig Config { get; }
    public SerialService Service { get; }
    public BoundedReceiveBuffer Received { get; } = new();
    public SerialTraceHistory Trace { get; } = new();
    public TelemetryBuffer Telemetry { get; } = new();
    public long SentBytes { get; private set; }
    public bool IsRecording => System.Threading.Volatile.Read(ref _capture) != null;
    public Exception? CaptureWriteError => System.Threading.Volatile.Read(ref _capture)?.WriteError;
    public long CaptureDroppedBytes => System.Threading.Volatile.Read(ref _capture)?.DroppedBytes ??
        System.Threading.Interlocked.Read(ref _lastCaptureDroppedBytes);
    public long UiDroppedBytes => Service.DroppedDisplayBytes;

    public void StartRecording(string file)
    {
        if (_disposed || !IsOpen) throw new InvalidOperationException("Connect a serial port before recording.");
        if (IsRecording) throw new InvalidOperationException("Capture already active.");
        System.Threading.Interlocked.Exchange(ref _lastCaptureDroppedBytes, 0);
        var writer = new SerialCapture(file, Config.PortName);
        if (System.Threading.Interlocked.CompareExchange(ref _capture, writer, null) != null)
        {
            writer.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw new InvalidOperationException("Capture already active.");
        }
        Changed?.Invoke(this);
    }

    public async Task StopRecordingAsync()
    {
        var writer = System.Threading.Interlocked.Exchange(ref _capture, null);
        if (writer != null)
        {
            try { await writer.DisposeAsync(); }
            finally { System.Threading.Interlocked.Exchange(ref _lastCaptureDroppedBytes, writer.DroppedBytes); }
        }
        Changed?.Invoke(this);
    }
    public string Draft { get; set; } = "";
    public bool SendHex { get; set; }
    public bool AppendNewLine { get; set; }
    public SerialLineEnding LineEnding { get; set; } = SerialLineEnding.None;
    public SerialTextEncoding TextEncoding { get; set; } = SerialTextEncoding.Utf8;
    public bool AutoReconnect { get; set; } = true;
    public ReconnectBackoff Reconnect { get; } = new();
    public bool ShowHex { get; set; }
    public bool ShowTimeline { get; set; }
    public bool PlotEnabled { get; set; }
    public bool FollowOutput { get; set; } = true;
    public string SearchQuery { get; set; } = string.Empty;
    public bool RepeatEnabled { get; private set; }
    public int RepeatIntervalMs { get; private set; } = 1000;
    public bool IsOpen => !_disposed && Service.IsOpen;
    public bool IsDisposed => _disposed;

    public event Action<SerialSession>? Changed;
    public event Action<SerialSession, bool>? ConnectionChanged;
    public event Action<SerialSession, string>? ErrorOccurred;

    public bool TryReconnect(DateTimeOffset now, out string? error)
    {
        error = null;
        if (_disposed || IsOpen || !AutoReconnect || !Reconnect.IsDue(now)) return false;
        bool success = Service.Open(Config, out error);
        if (success) Reconnect.Reset(); else Reconnect.Failed(now);
        return success;
    }

    public bool Open(out string? error)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SerialSession));
        bool opened = Service.Open(Config, out error);
        if (opened) Reconnect.Reset();
        return opened;
    }

    public void StartScanning()
    {
        if (!_disposed) Service.StartPortScanning();
    }

    public void StopRepeat()
    {
        RepeatEnabled = false;
        if (!_disposed) Service.StopPeriodicSend();
    }

    public void ConfigureRepeat(bool enabled, int intervalMs)
    {
        if (_disposed) return;
        StopRepeat();
        if (!enabled || intervalMs <= 0) return;
        RepeatEnabled = true;
        RepeatIntervalMs = intervalMs;
        Service.StartPeriodicSend(intervalMs, () => _ = SendAsync());
    }

    public async Task<(bool Success, string? Error)> SendAsync(CancellationToken token = default)
    {
        if (_disposed || !Service.IsOpen) return (false, I18n.T("ErrorNotOpen"));
        // Drop overlapping periodic ticks instead of queuing unlimited writes.
        if (_sending) return (false, null);
        if (!SerialDataConverter.PreparePayload(Draft, SendHex, LineEnding, TextEncoding,
            out var payload, out var conversionError))
            return (false, conversionError);

        _sending = true;
        try
        {
            var (success, error) = await Service.WriteAsync(payload, token);
            if (success && !_disposed)
            {
                SentBytes += payload.Length;
                var timestamp = DateTimeOffset.UtcNow;
                Trace.Append(timestamp, Config.PortName, SerialTraceDirection.Tx, payload);
                System.Threading.Volatile.Read(ref _capture)?.TryRecord(timestamp, SerialTraceDirection.Tx, payload);
                Changed?.Invoke(this);
            }
            return (success, error);
        }
        finally
        {
            _sending = false;
        }
    }

    /// <summary>Transmit a binary stream in 4 KiB segments, retaining accurate TX events.</summary>
    public async Task<(bool Success, string? Error)> SendStreamAsync(System.IO.Stream input, CancellationToken token = default)
    {
        if (_disposed || !Service.IsOpen) return (false, I18n.T("ErrorNotOpen"));
        if (_sending) return (false, "Another transfer is running.");
        _sending = true;
        try
        {
            var buffer = new byte[4096];
            int count;
            while ((count = await input.ReadAsync(buffer, token)) > 0)
            {
                var data = buffer.AsSpan(0, count).ToArray();
                var (success, error) = await Service.WriteAsync(data, token);
                if (!success) return (false, error);
                if (_disposed) return (false, "Session closed during transfer.");
                SentBytes += data.Length;
                var now = DateTimeOffset.UtcNow;
                Trace.Append(now, Config.PortName, SerialTraceDirection.Tx, data);
                System.Threading.Volatile.Read(ref _capture)?.TryRecord(now, SerialTraceDirection.Tx, data);
                Changed?.Invoke(this);
            }
            return (true, null);
        }
        catch (OperationCanceledException) { return (false, "Transfer cancelled."); }
        catch (System.IO.IOException ex) { return (false, ex.Message); }
        finally { _sending = false; }
    }

    /// <summary>Runs a bounded, explicit sequence; no retry on disconnect.</summary>
    public async Task<(bool Success, string? Error)> RunMacroAsync(
        System.Collections.Generic.IReadOnlyList<SerialMacroStep> steps, CancellationToken token = default)
    {
        if (_disposed || !Service.IsOpen) return (false, I18n.T("ErrorNotOpen"));
        if (_sending) return (false, "Another send is in progress.");
        _sending = true;
        try
        {
            foreach (var step in steps)
            {
                token.ThrowIfCancellationRequested();
                if (!IsOpen) return (false, "Port disconnected; macro stopped.");
                if (step.Payload.Length == 0)
                {
                    await Task.Delay(step.WaitMilliseconds, token);
                    continue;
                }
                var (ok, error) = await Service.WriteAsync(step.Payload, token);
                if (!ok) return (false, error);
                SentBytes += step.Payload.Length;
                var timestamp = DateTimeOffset.UtcNow;
                Trace.Append(timestamp, Config.PortName, SerialTraceDirection.Tx, step.Payload);
                System.Threading.Volatile.Read(ref _capture)?.TryRecord(timestamp, SerialTraceDirection.Tx, step.Payload);
                Changed?.Invoke(this);
            }
            return (true, null);
        }
        catch (OperationCanceledException) { return (false, "Macro cancelled."); }
        finally { _sending = false; }
    }

    public void ClearReceived()
    {
        Received.Clear();
        Trace.Clear();
        Telemetry.Clear();
        Changed?.Invoke(this);
    }

    public void SetSignals(bool rts, bool dtr)
    {
        Config.RtsEnable = rts;
        Config.DtrEnable = dtr;
        if (!_disposed) Service.SetSignals(rts, dtr);
    }

    private void OnRawReceived(DateTimeOffset timestamp, byte[] data)
    {
        System.Threading.Volatile.Read(ref _capture)?.TryRecord(timestamp, SerialTraceDirection.Rx, data);
    }

    private void OnReceived(byte[] data)
    {
        if (_disposed || !Service.IsOpen) return;
        Received.Append(data);
        Telemetry.Append(data);
        Trace.Append(DateTimeOffset.UtcNow, Config.PortName, SerialTraceDirection.Rx, data);
        Changed?.Invoke(this);
    }

    private void OnConnectionChanged(bool connected, string _)
    {
        if (_disposed) return;
        if (!connected) { StopRepeat(); Reconnect.Failed(DateTimeOffset.UtcNow); }
        ConnectionChanged?.Invoke(this, connected);
    }

    private void OnError(string error)
    {
        if (!_disposed) ErrorOccurred?.Invoke(this, error);
    }

    public override string ToString() => Config.PortName + (IsOpen ? " ●" : " ○");

    public void Dispose()
    {
        if (_disposed) return;
        StopRepeat();
        _disposed = true;
        var capture = System.Threading.Interlocked.Exchange(ref _capture, null);
        if (capture != null)
        {
            // UI callers should stop captures asynchronously first; bound the shutdown fallback.
            try { capture.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3)); }
            catch (Exception) { /* Capture errors are surfaced by the recording state monitor. */ }
        }
        Service.RawDataReceived -= OnRawReceived;
        Service.DataReceived -= OnReceived;
        Service.ConnectionChanged -= OnConnectionChanged;
        Service.ErrorOccurred -= OnError;
        Service.Dispose();
    }
}
