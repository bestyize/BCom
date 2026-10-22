using System;

namespace BCom.CrossPlatform;

/// <summary>
/// Bounded circular byte history for the on-screen serial monitor. The total
/// received counter remains accurate even when old display data is evicted.
/// </summary>
public sealed class BoundedReceiveBuffer
{
    private readonly byte[] _buffer;
    private int _start;

    public BoundedReceiveBuffer(int capacity = 128 * 1024)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _buffer = new byte[capacity];
    }

    public int Count { get; private set; }
    public int Capacity => _buffer.Length;
    public long TotalBytes { get; private set; }
    public long EvictedBytes => TotalBytes - Count;

    public void Append(ReadOnlySpan<byte> data)
    {
        TotalBytes += data.Length;
        if (data.Length == 0) return;

        if (data.Length >= Capacity)
        {
            data[^Capacity..].CopyTo(_buffer);
            _start = 0;
            Count = Capacity;
            return;
        }

        int overflow = Math.Max(0, Count + data.Length - Capacity);
        _start = (_start + overflow) % Capacity;
        Count -= overflow;

        int end = (_start + Count) % Capacity;
        int first = Math.Min(data.Length, Capacity - end);
        data[..first].CopyTo(_buffer.AsSpan(end, first));
        data[first..].CopyTo(_buffer.AsSpan(0, data.Length - first));
        Count += data.Length;
    }

    public byte[] Snapshot()
    {
        var result = new byte[Count];
        int first = Math.Min(Count, Capacity - _start);
        _buffer.AsSpan(_start, first).CopyTo(result);
        _buffer.AsSpan(0, Count - first).CopyTo(result.AsSpan(first));
        return result;
    }

    public void Clear()
    {
        _start = 0;
        Count = 0;
        TotalBytes = 0;
    }
}
