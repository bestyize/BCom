using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class SerialStagesTests
{
    [Fact]
    public void ReconnectBackoffIsBoundedAndResettable()
    {
        var backoff = new ReconnectBackoff();
        var now = DateTimeOffset.UtcNow;
        Assert.True(backoff.IsDue(now));
        backoff.Failed(now);
        Assert.False(backoff.IsDue(now.AddSeconds(1)));
        Assert.True(backoff.IsDue(now.AddSeconds(2)));
        for (int i = 0; i < 30; i++) backoff.Failed(now);
        Assert.True(backoff.NextAttempt <= now.AddSeconds(30));
        backoff.Reset();
        Assert.Equal(0, backoff.Failures);
        Assert.True(backoff.IsDue(now));
    }

    [Theory]
    [InlineData(SerialLineEnding.None, "41")]
    [InlineData(SerialLineEnding.Cr, "41 0D")]
    [InlineData(SerialLineEnding.Lf, "41 0A")]
    [InlineData(SerialLineEnding.CrLf, "41 0D 0A")]
    public void AllLineEndingsPreserveExactBytes(SerialLineEnding ending, string expected)
    {
        Assert.True(SerialDataConverter.PreparePayload("A", false, ending,
            SerialTextEncoding.Ascii, out var payload, out _));
        Assert.Equal(expected, SerialDataConverter.ToHexString(payload));
    }

    [Fact]
    public void ChineseGbkRoundTripsAcrossPlatforms()
    {
        Assert.True(SerialDataConverter.PreparePayload("中文", false, SerialLineEnding.None,
            SerialTextEncoding.Gbk, out var bytes, out _));
        Assert.Equal("D6 D0 CE C4", SerialDataConverter.ToHexString(bytes));
        Assert.Equal("中文", SerialDataConverter.ToTextString(bytes, SerialTextEncoding.Gbk));
    }

    [Fact]
    public async Task ExistingEmptyCaptureDestinationCanBeUsed()
    {
        string path = Path.GetTempFileName();
        try
        {
            await using (var capture = new SerialCapture(path, "COM3"))
                Assert.True(capture.TryRecord(DateTimeOffset.UtcNow, SerialTraceDirection.Rx, new byte[] { 1, 2, 3 }));
            var frames = new System.Collections.Generic.List<SerialTraceEvent>();
            await foreach (var frame in SerialCapture.ReplayAsync(path)) frames.Add(frame);
            Assert.Equal(new byte[] { 1, 2, 3 }, Assert.Single(frames).Data);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task BoundedCaptureQueueAccountsForEveryAcceptedAndRejectedByte()
    {
        string path = Path.Combine(Path.GetTempPath(), "bcom-stress-" + Guid.NewGuid().ToString("N") + ".bcomcap");
        try
        {
            await using (var capture = new SerialCapture(path, "TEST", maxPendingEvents: 4))
            {
                int accepted = 0;
                for (int i = 0; i < 1000; i++)
                {
                    if (capture.TryRecord(DateTimeOffset.UtcNow, SerialTraceDirection.Rx, new byte[1024]))
                        accepted++;
                }
                Assert.Equal(1024L * 1000, capture.DroppedBytes + accepted * 1024L);
            }
            Assert.True(new FileInfo(path).Length > 10);
        }
        finally { File.Delete(path); }
    }
}
