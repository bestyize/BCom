using System;
using System.Linq;
using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class SerialAutomationTests
{
    [Fact]
    public void ScriptParsesAsciiCrlfAndBinaryPayloads()
    {
        var steps = SerialAutomation.Parse("# setup\nAT\\r\\n => OK\nHEX:00 FF => HEX:0A 00\n");
        Assert.Equal(2, steps.Count);
        Assert.Equal("AT\r\n"u8.ToArray(), steps[0].Request);
        Assert.Equal("OK"u8.ToArray(), steps[0].Expected);
        Assert.Equal(new byte[] { 0, 255 }, steps[1].Request);
        Assert.Equal(new byte[] { 10, 0 }, steps[1].Expected);
    }

    [Theory]
    [InlineData("AT")]
    [InlineData("=> response")]
    [InlineData("request =>")]
    [InlineData("HEX:GG => OK")]
    public void InvalidStepsFailBeforeOpeningHardware(string script)
    {
        Assert.Throws<FormatException>(() => SerialAutomation.Parse(script));
    }

    [Fact]
    public void EmptyScriptFailsBeforeOpeningHardware()
    {
        Assert.Throws<FormatException>(() => SerialAutomation.Parse(" # comments only"));
    }
}
