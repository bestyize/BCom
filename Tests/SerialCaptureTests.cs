using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class SerialCaptureTests
{
    [Fact]
    public async Task CaptureAndReplayPreserveBinaryPayloadDirectionPortAndUtcTime()
    {
        var path = Path.Combine(Path.GetTempPath(), "bcom-" + Guid.NewGuid().ToString("N") + ".bcomcap");
        var moment = new DateTimeOffset(2026, 10, 22, 12, 34, 56, TimeSpan.Zero);
        try
        {
            var rx = new byte[] { 0, 255, 10 };
            await using (var capture = new SerialCapture(path, "COM7"))
            {
                Assert.True(capture.TryRecord(moment, SerialTraceDirection.Rx, rx));
                rx[0] = 99;
                Assert.True(capture.TryRecord(moment.AddMilliseconds(1), SerialTraceDirection.Tx, new byte[] { 65 }));
            }
            var data = new System.Collections.Generic.List<SerialTraceEvent>();
            await foreach (var e in SerialCapture.ReplayAsync(path)) data.Add(e);
            Assert.Equal(2, data.Count);
            Assert.Equal(moment, data[0].Timestamp);
            Assert.Equal("COM7", data[0].Port);
            Assert.Equal(SerialTraceDirection.Rx, data[0].Direction);
            Assert.Equal(new byte[] { 0, 255, 10 }, data[0].Data);
            Assert.Equal(SerialTraceDirection.Tx, data[1].Direction);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task RotatingFilesRemainIndependentlyReadable()
    {
        var path = Path.Combine(Path.GetTempPath(), "bcom-" + Guid.NewGuid().ToString("N") + ".bcomcap");
        try
        {
            await using (var capture = new SerialCapture(path, "/dev/ttyTEST", maxFileBytes: 4096))
            {
                Assert.True(capture.TryRecord(DateTimeOffset.UtcNow, SerialTraceDirection.Rx, new byte[2900]));
                Assert.True(capture.TryRecord(DateTimeOffset.UtcNow, SerialTraceDirection.Tx, new byte[2900]));
            }
            var rx = new System.Collections.Generic.List<SerialTraceEvent>();
            var tx = new System.Collections.Generic.List<SerialTraceEvent>();
            await foreach (var e in SerialCapture.ReplayAsync(path)) rx.Add(e);
            await foreach (var e in SerialCapture.ReplayAsync(path + ".part001")) tx.Add(e);
            Assert.Equal(2900, Assert.Single(rx).Data.Length);
            Assert.Equal(SerialTraceDirection.Tx, Assert.Single(tx).Direction);
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".part001");
        }
    }

    [Fact]
    public async Task InvalidMagicIsRejected()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, new byte[10]);
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await foreach (var _ in SerialCapture.ReplayAsync(path)) { }
            });
        }
        finally { File.Delete(path); }
    }
}
