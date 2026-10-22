using System.Text;
using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class SerialCoreTests
{
    [Fact]
    public void CircularHistoryKeepsOnlyTheNewestBytes()
    {
        var history = new BoundedReceiveBuffer(5);
        history.Append("ABC"u8);
        history.Append("DEFG"u8);

        Assert.Equal("CDEFG", Encoding.ASCII.GetString(history.Snapshot()));
        Assert.Equal(7L, history.TotalBytes);
        Assert.Equal(2L, history.EvictedBytes);
    }

    [Fact]
    public void OversizedChunkKeepsTheTailAndCountsAllBytes()
    {
        var history = new BoundedReceiveBuffer(4);
        history.Append("abcdefgh"u8);

        Assert.Equal("efgh", Encoding.ASCII.GetString(history.Snapshot()));
        Assert.Equal(8L, history.TotalBytes);
        Assert.Equal(4, history.Count);
    }

    [Fact]
    public void ClearResetsHistoryAndCounters()
    {
        var history = new BoundedReceiveBuffer(3);
        history.Append("12345"u8);
        history.Clear();
        history.Append("XY"u8);

        Assert.Equal("XY", Encoding.ASCII.GetString(history.Snapshot()));
        Assert.Equal(2L, history.TotalBytes);
        Assert.Equal(0L, history.EvictedBytes);
    }

    [Fact]
    public void EmptyHistoryIsSafe()
    {
        var history = new BoundedReceiveBuffer(1);
        history.Append(ReadOnlySpan<byte>.Empty);
        Assert.Empty(history.Snapshot());
    }

    [Theory]
    [InlineData("01 0a,FF", "01 0A FF")]
    [InlineData("00-10-ff", "00 10 FF")]
    public void HexParsingPreservesBytes(string input, string expected)
    {
        Assert.True(SerialDataConverter.TryParseHex(input, out var bytes));
        Assert.Equal(expected, SerialDataConverter.ToHexString(bytes));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("HH")]
    [InlineData("01 234")]
    public void InvalidHexIsRejected(string input)
    {
        Assert.False(SerialDataConverter.TryParseHex(input, out _));
    }
}
