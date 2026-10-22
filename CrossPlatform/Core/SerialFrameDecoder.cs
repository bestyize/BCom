using System;
using System.Collections.Generic;

namespace BCom.CrossPlatform;

public enum SerialFramingMode { Newline, FixedLength, LengthPrefixed }

/// <summary>
/// Stateful byte-oriented framing. Newline includes the LF terminator.
/// LengthPrefixed means one unsigned length byte followed by N payload bytes.
/// The decoder never treats SerialPort callback boundaries as frame boundaries.
/// </summary>
public sealed class SerialFrameDecoder
{
    private readonly List<byte> _pending = new();
    private readonly int _size;
    public SerialFramingMode Mode { get; }
    public int DroppedBytes { get; private set; }
    public int MaxFrameSize { get; }

    public SerialFrameDecoder(SerialFramingMode mode, int fixedLength = 0, int maxFrameSize = 8192)
    {
        if (maxFrameSize is < 2 or > 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(maxFrameSize));
        if (mode == SerialFramingMode.FixedLength && (fixedLength < 1 || fixedLength > maxFrameSize))
            throw new ArgumentOutOfRangeException(nameof(fixedLength));
        Mode = mode;
        _size = fixedLength;
        MaxFrameSize = maxFrameSize;
    }

    public IReadOnlyList<byte[]> Append(ReadOnlySpan<byte> bytes)
    {
        var frames = new List<byte[]>();
        foreach (byte b in bytes)
        {
            _pending.Add(b);
            int needed = Mode switch
            {
                SerialFramingMode.FixedLength => _size,
                SerialFramingMode.LengthPrefixed => _pending[0] + 1,
                _ => _pending.IndexOf((byte)'\n') is int i && i >= 0 ? i + 1 : 0
            };
            if (needed > 0 && needed <= _pending.Count && needed <= MaxFrameSize)
            {
                frames.Add(_pending.GetRange(0, needed).ToArray());
                _pending.RemoveRange(0, needed);
            }
            if (_pending.Count >= MaxFrameSize)
            {
                DroppedBytes += _pending.Count;
                _pending.Clear();
            }
        }
        return frames;
    }

    public void Reset()
    {
        _pending.Clear();
        DroppedBytes = 0;
    }

    public static ushort ModbusCrc16(ReadOnlySpan<byte> bytes)
    {
        ushort crc = 0xFFFF;
        foreach (byte b in bytes)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
                crc = (ushort)((crc & 1) != 0 ? (crc >> 1) ^ 0xA001 : crc >> 1);
        }
        return crc;
    }

    public static bool CheckModbusCrc(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 3) return false;
        ushort actual = (ushort)(frame[^2] | (frame[^1] << 8));
        return actual == ModbusCrc16(frame[..^2]);
    }
}
