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
            DtrEnable = config.DtrEnable
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

    public bool Open(out string? error)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SerialSession));
        return Service.Open(Config, out error);
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
        if (!SerialDataConverter.PreparePayload(Draft, SendHex, AppendNewLine,
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
        if (!connected) StopRepeat();
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
        if (capture != null) capture.DisposeAsync().AsTask().GetAwaiter().GetResult();
        Service.RawDataReceived -= OnRawReceived;
        Service.DataReceived -= OnReceived;
        Service.ConnectionChanged -= OnConnectionChanged;
        Service.ErrorOccurred -= OnError;
        Service.Dispose();
    }
}
