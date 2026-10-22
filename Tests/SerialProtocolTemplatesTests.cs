using System;
using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class SerialProtocolTemplatesTests
{
    [Fact]
    public void ModbusReadRegistersHasCorrectLittleEndianCrc()
    {
        Assert.Equal(new byte[] { 1, 3, 0, 0, 0, 10, 0xC5, 0xCD },
            SerialProtocolTemplates.ModbusReadHoldingRegisters(1, 0, 10));
    }

    [Fact]
    public void InvalidModbusAddressesAndCountsFailClosed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SerialProtocolTemplates.ModbusReadHoldingRegisters(0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SerialProtocolTemplates.ModbusReadHoldingRegisters(1, 0, 126));
    }

    [Fact]
    public void AtTemplateAppendsCrlfButRejectsEmbeddedLines()
    {
        Assert.Equal("AT+GMR\r\n"u8.ToArray(), SerialProtocolTemplates.AtCommand("AT+GMR"));
        Assert.Throws<ArgumentException>(() => SerialProtocolTemplates.AtCommand("AT\r\nAT+RST"));
    }

    [Fact]
    public void NmeaXorChecksumIsVerified()
    {
        Assert.True(SerialProtocolTemplates.ValidateNmeaChecksum("$ABC*40\r\n"));
        Assert.False(SerialProtocolTemplates.ValidateNmeaChecksum("$ABC*41"));
        Assert.False(SerialProtocolTemplates.ValidateNmeaChecksum("ABC*40"));
    }
}
