using System;
using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class SerialMacroTests
{
    [Fact]
    public void MacroDecodesCommandsAndDelays()
    {
        var steps = SerialMacroScript.Parse("TEXT:AT\nWAIT:200\nHEX:01 03 FF",
            SerialTextEncoding.Ascii, SerialLineEnding.CrLf);
        Assert.Equal(3, steps.Count);
        Assert.Equal(new byte[] { 65, 84, 13, 10 }, steps[0].Payload);
        Assert.Equal(200, steps[1].WaitMilliseconds);
        Assert.Equal(new byte[] { 1, 3, 255 }, steps[2].Payload);
    }

    [Theory]
    [InlineData("WAIT:30001")]
    [InlineData("SEND:AT")]
    [InlineData("HEX:GG")]
    [InlineData("")]
    public void InvalidMacroDoesNotExecute(string input) =>
        Assert.Throws<FormatException>(() =>
            SerialMacroScript.Parse(input, SerialTextEncoding.Utf8, SerialLineEnding.None));
}
