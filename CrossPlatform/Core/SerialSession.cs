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
        Service.ConnectionChanged += OnConnectionChanged;
        Service.ErrorOccurred += OnError;
    }

    public SerialConfig Config { get; }
    public SerialService Service { get; }
    public BoundedReceiveBuffer Received { get; } = new();
    public SerialTraceHistory Trace { get; } = new();
    public long SentBytes { get; private set; }
    public string Draft { get; set; } = "";
    public bool SendHex { get; set; }
    public bool AppendNewLine { get; set; }
    public bool ShowHex { get; set; }
    public bool ShowTimeline { get; set; }
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
                Trace.Append(DateTimeOffset.UtcNow, Config.PortName, SerialTraceDirection.Tx, payload);
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
        Changed?.Invoke(this);
    }

    public void SetSignals(bool rts, bool dtr)
    {
        Config.RtsEnable = rts;
        Config.DtrEnable = dtr;
        if (!_disposed) Service.SetSignals(rts, dtr);
    }

    private void OnReceived(byte[] data)
    {
        if (_disposed || !Service.IsOpen) return;
        Received.Append(data);
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
        Service.DataReceived -= OnReceived;
        Service.ConnectionChanged -= OnConnectionChanged;
        Service.ErrorOccurred -= OnError;
        Service.Dispose();
    }
}
