using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class SerialFrameDecoderTests
{
    [Fact]
    public void NewlineFramingHandlesSplitChunks()
    {
        var decoder = new SerialFrameDecoder(SerialFramingMode.Newline);
        Assert.Empty(decoder.Append("abc"u8));
        var frames = decoder.Append("\ndef\n"u8);
        Assert.Equal(2, frames.Count);
        Assert.Equal("abc\n"u8.ToArray(), frames[0]);
        Assert.Equal("def\n"u8.ToArray(), frames[1]);
    }

    [Fact]
    public void FixedAndLengthPrefixedFramingHonorBoundaries()
    {
        var fixedLength = new SerialFrameDecoder(SerialFramingMode.FixedLength, 3);
        Assert.Equal(2, fixedLength.Append(new byte[] { 1, 2, 3, 4, 5, 6 }).Count);
        var length = new SerialFrameDecoder(SerialFramingMode.LengthPrefixed);
        Assert.Empty(length.Append(new byte[] { 2, 0xA0 }));
        Assert.Equal(new byte[] { 2, 0xA0, 0xB0 }, Assert.Single(length.Append(new byte[] { 0xB0 })));
    }

    [Fact]
    public void OversizedUnterminatedFramesAreDiscarded()
    {
        var decoder = new SerialFrameDecoder(SerialFramingMode.Newline, maxFrameSize: 4);
        Assert.Empty(decoder.Append("abcde"u8));
        Assert.True(decoder.DroppedBytes >= 4);
    }

    [Fact]
    public void ModbusRtuCrcMatchesPublishedVector()
    {
        var body = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        var crc = SerialFrameDecoder.ModbusCrc16(body);
        Assert.Equal((ushort)0xCDC5, crc);
        Assert.True(SerialFrameDecoder.CheckModbusCrc(new byte[] { 1, 3, 0, 0, 0, 10, 0xC5, 0xCD }));
        Assert.False(SerialFrameDecoder.CheckModbusCrc(new byte[] { 1, 3, 0, 0, 0, 10, 0, 0 }));
    }
}
