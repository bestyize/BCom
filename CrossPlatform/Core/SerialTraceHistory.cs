using System;
using System.Collections.Generic;
using System.Linq;

namespace BCom.CrossPlatform;

/// <summary>
/// Direction and capture-time metadata for one physical serial read/write.
/// A read callback may contain an arbitrary stream chunk, not a protocol frame.
/// </summary>
public enum SerialTraceDirection
{
    Rx,
    Tx
}

public sealed record SerialTraceEvent(
    DateTimeOffset Timestamp,
    string Port,
    SerialTraceDirection Direction,
    byte[] Data);

/// <summary>
/// Bounded event history independent from raw receive history.
/// Oldest whole events are removed to keep capture memory predictable.
/// </summary>
public sealed class SerialTraceHistory
{
    private readonly Queue<SerialTraceEvent> _events = new();
    private readonly int _capacityBytes;
    private int _retainedBytes;

    public SerialTraceHistory(int capacityBytes = 128 * 1024)
    {
        if (capacityBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacityBytes));
        _capacityBytes = capacityBytes;
    }

    public int RetainedBytes => _retainedBytes;
    public long TotalBytes { get; private set; }
    public long EvictedBytes { get; private set; }

    public void Append(
        DateTimeOffset timestamp,
        string port,
        SerialTraceDirection direction,
        ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return;

        TotalBytes += data.Length;
        if (data.Length > _capacityBytes)
        {
            EvictedBytes += data.Length - _capacityBytes;
            data = data[^_capacityBytes..];
        }

        byte[] copy = data.ToArray();
        while (_events.Count > 0 && _retainedBytes + copy.Length > _capacityBytes)
        {
            var oldest = _events.Dequeue();
            _retainedBytes -= oldest.Data.Length;
            EvictedBytes += oldest.Data.Length;
        }

        _events.Enqueue(new SerialTraceEvent(timestamp, port, direction, copy));
        _retainedBytes += copy.Length;
    }

    public IReadOnlyList<SerialTraceEvent> Snapshot() => _events.ToArray();

    public void Clear()
    {
        _events.Clear();
        _retainedBytes = 0;
        TotalBytes = 0;
        EvictedBytes = 0;
    }
}
