using System;
using System.Text;
using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class TelemetryTests
{
    [Fact]
    public void LinesSplitAcrossReadsProduceExactSamples()
    {
        var t = new TelemetryBuffer();
        t.Append("1.2,3"u8);
        Assert.Empty(t.Snapshot());
        t.Append(".5\r\n-2,4\n"u8);
        Assert.Equal(2, t.SampleCount);
        Assert.Equal(new[] { 1.2, 3.5 }, t.Snapshot()[0]);
        Assert.Equal(new[] { -2d, 4d }, t.Snapshot()[1]);
    }

    [Fact]
    public void HistoryIsBoundedAndRejectsMalformedNumbers()
    {
        var t = new TelemetryBuffer(2);
        t.Append("NaN\nInfinity\n4,5\n6,7\n8,9\n"u8);
        Assert.Equal(2, t.SampleCount);
        Assert.Equal(2, t.ChannelCount);
        Assert.Equal(new[] { 6d, 7d }, t.Snapshot()[0]);
        Assert.Equal(new[] { 8d, 9d }, t.Snapshot()[1]);
    }

    [Fact]
    public void PartialLinesCanBeCleared()
    {
        var t = new TelemetryBuffer();
        t.Append("999,"u8);
        t.Clear();
        t.Append("5\n"u8);
        Assert.Equal(new[] { 5d }, Assert.Single(t.Snapshot()));
    }
}
