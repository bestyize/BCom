using System;
using System.Linq;
using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class SerialTraceTests
{
    [Fact]
    public void HistoryCopiesInputBuffersAndPreservesPortDirectionAndTimestamp()
    {
        var trace = new SerialTraceHistory(8);
        var timestamp = new DateTimeOffset(2026, 10, 22, 11, 30, 0, TimeSpan.Zero);
        var bytes = new byte[] { 0x01, 0x02 };

        trace.Append(timestamp, "COM4", SerialTraceDirection.Rx, bytes);
        bytes[0] = 0xFF;

        var entry = Assert.Single(trace.Snapshot());
        Assert.Equal(timestamp, entry.Timestamp);
        Assert.Equal("COM4", entry.Port);
        Assert.Equal(SerialTraceDirection.Rx, entry.Direction);
        Assert.Equal(new byte[] { 0x01, 0x02 }, entry.Data);
    }

    [Fact]
    public void OldestEventsAreEvictedAtByteCapacity()
    {
        var trace = new SerialTraceHistory(4);
        var timestamp = DateTimeOffset.UtcNow;
        trace.Append(timestamp, "a", SerialTraceDirection.Rx, new byte[] { 1, 2, 3 });
        trace.Append(timestamp, "a", SerialTraceDirection.Tx, new byte[] { 4, 5 });

        Assert.Equal(5, trace.TotalBytes);
        Assert.Equal(3, trace.EvictedBytes);
        Assert.Equal(2, trace.RetainedBytes);
        Assert.Equal(SerialTraceDirection.Tx, Assert.Single(trace.Snapshot()).Direction);
    }

    [Fact]
    public void OversizeInputRetainsOnlyTheNewestBytes()
    {
        var trace = new SerialTraceHistory(3);
        trace.Append(DateTimeOffset.UtcNow, "/dev/ttyUSB0", SerialTraceDirection.Rx,
            new byte[] { 1, 2, 3, 4, 5 });

        Assert.Equal(new byte[] { 3, 4, 5 }, Assert.Single(trace.Snapshot()).Data);
        Assert.Equal(5, trace.TotalBytes);
        Assert.Equal(2, trace.EvictedBytes);
    }

    [Fact]
    public void TimelineHasDirectionalLabelsAndHexBytes()
    {
        var trace = new SerialTraceHistory();
        trace.Append(DateTimeOffset.UtcNow, "COM3", SerialTraceDirection.Tx, new byte[] { 0x00, 0xFF });
        var display = SerialTraceFormatter.Format(trace);

        Assert.Contains("[TX] [COM3] 00 FF", display);
    }

    [Fact]
    public void ClearResetsAllEventStatistics()
    {
        var trace = new SerialTraceHistory(2);
        trace.Append(DateTimeOffset.UtcNow, "COM3", SerialTraceDirection.Rx, new byte[] { 1, 2, 3 });
        trace.Clear();

        Assert.Empty(trace.Snapshot());
        Assert.Equal(0, trace.TotalBytes);
        Assert.Equal(0, trace.RetainedBytes);
        Assert.Equal(0, trace.EvictedBytes);
    }
}
