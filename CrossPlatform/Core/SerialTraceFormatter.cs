using System;
using System.Globalization;
using System.Text;

namespace BCom.CrossPlatform;

/// <summary>
/// Generates a searchable, human-readable event timeline.
/// Timestamps are read/write callback times, not protocol-frame boundaries.
/// </summary>
public static class SerialTraceFormatter
{
    public static string Format(SerialTraceHistory history)
    {
        var output = new StringBuilder();
        if (history.EvictedBytes > 0)
        {
            output.Append("[... ")
                  .Append(history.EvictedBytes.ToString(CultureInfo.InvariantCulture))
                  .AppendLine(" earlier bytes omitted ...]");
        }

        foreach (var entry in history.Snapshot())
        {
            output.Append('[')
                  .Append(entry.Timestamp.ToLocalTime()
                      .ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture))
                  .Append("] [")
                  .Append(entry.Direction == SerialTraceDirection.Rx ? "RX" : "TX")
                  .Append("] [")
                  .Append(entry.Port.Replace("\r", "").Replace("\n", ""))
                  .Append("] ")
                  .AppendLine(SerialDataConverter.ToHexString(entry.Data));
        }

        return output.ToString();
    }
}
