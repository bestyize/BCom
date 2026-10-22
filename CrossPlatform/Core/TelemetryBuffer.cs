using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BCom.CrossPlatform;

/// <summary>
/// Bounded CSV numeric sample decoder. Each LF-terminated line may contain
/// up to eight comma-separated finite numbers. Partial lines survive reads.
/// Invalid or oversized lines are skipped without growing the receive buffer.
/// </summary>
public sealed class TelemetryBuffer
{
    private readonly Queue<double[]> _samples = new();
    private readonly StringBuilder _partial = new();
    public int MaxSamples { get; }
    public int SampleCount => _samples.Count;
    public int ChannelCount { get; private set; }

    public TelemetryBuffer(int maxSamples = 512)
    {
        if (maxSamples < 2) throw new ArgumentOutOfRangeException(nameof(maxSamples));
        MaxSamples = maxSamples;
    }

    public void Append(ReadOnlySpan<byte> bytes)
    {
        foreach (byte b in bytes)
        {
            if (b == (byte)'\n')
            {
                ParseLine(_partial.ToString().TrimEnd('\r'));
                _partial.Clear();
            }
            else if (b is >= 32 and <= 126 && _partial.Length < 4096)
                _partial.Append((char)b);
            else if (_partial.Length >= 4096)
                _partial.Clear();
        }
    }

    private void ParseLine(string line)
    {
        if (line.Length == 0) return;
        string[] fields = line.Split(',');
        if (fields.Length is < 1 or > 8) return;
        var values = new double[fields.Length];
        for (int i = 0; i < fields.Length; i++)
        {
            if (!double.TryParse(fields[i], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out values[i]) || !double.IsFinite(values[i]))
                return;
        }
        ChannelCount = Math.Max(ChannelCount, values.Length);
        _samples.Enqueue(values);
        if (_samples.Count > MaxSamples) _samples.Dequeue();
    }

    public IReadOnlyList<double[]> Snapshot() => _samples.Select(x => (double[])x.Clone()).ToArray();

    public void Clear()
    {
        _samples.Clear();
        _partial.Clear();
        ChannelCount = 0;
    }
}
