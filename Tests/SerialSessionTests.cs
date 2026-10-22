using System;
using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class SerialSessionTests
{
    private static SerialConfig Config(string port) => new()
    {
        PortName = port,
        BaudRate = 115200,
        DataBits = 8
    };

    [Fact]
    public void SeparateSessionsKeepIndependentBuffersAndCounters()
    {
        using var left = new SerialSession(Config("COM1"));
        using var right = new SerialSession(Config("COM2"));
        var sample = DateTimeOffset.UtcNow;

        left.Received.Append(new byte[] { 1, 2, 3 });
        left.Trace.Append(sample, left.Config.PortName, SerialTraceDirection.Rx,
            new byte[] { 1, 2, 3 });
        right.Received.Append(new byte[] { 9 });

        Assert.Equal(3, left.Received.TotalBytes);
        Assert.Equal(1, right.Received.TotalBytes);
        Assert.Single(left.Trace.Snapshot());
        Assert.Empty(right.Trace.Snapshot());

        left.ClearReceived();
        Assert.Equal(1, right.Received.TotalBytes);
    }

    [Fact]
    public void SessionCopiesConfigurationAndKeepsDraftStateSeparate()
    {
        var config = Config("COM1");
        using var first = new SerialSession(config);
        using var second = new SerialSession(Config("COM2"));

        first.Draft = "AT";
        first.SendHex = false;
        second.Draft = "AA 55";
        second.SendHex = true;
        config.PortName = "COM9";

        Assert.Equal("COM1", first.Config.PortName);
        Assert.Equal("COM2", second.Config.PortName);
        Assert.Equal("AT", first.Draft);
        Assert.Equal("AA 55", second.Draft);
        Assert.False(first.SendHex);
        Assert.True(second.SendHex);
    }

    [Fact]
    public void ManagerRejectsUnknownSessionAndBlankPortWithoutOpeningHardware()
    {
        using var manager = new SerialSessionManager();
        using var unknown = new SerialSession(Config("COM42"));

        Assert.False(manager.Select(unknown));
        Assert.False(manager.Remove(unknown));
        Assert.False(manager.TryOpen(Config(""), out var result, out var error));
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Empty(manager.Sessions);
        Assert.Null(manager.Active);
    }

    [Fact]
    public void DisposedSessionRejectsOpenAndSend()
    {
        var session = new SerialSession(Config("COM1"));
        session.Dispose();
        Assert.Throws<ObjectDisposedException>(() => session.Open(out _));
        var result = session.SendAsync().GetAwaiter().GetResult();
        Assert.False(result.Success);
    }
}
