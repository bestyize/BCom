using System;
using System.Globalization;
using System.Text;

namespace BCom.CrossPlatform;

/// <summary>
/// Small, verified protocol presets. This is not a claim of full Modbus/NMEA
/// decoding; higher-level response interpretation remains device-specific.
/// </summary>
public static class SerialProtocolTemplates
{
    public static byte[] ModbusReadHoldingRegisters(byte slave, ushort firstRegister, ushort registerCount)
    {
        if (slave is < 1 or > 247) throw new ArgumentOutOfRangeException(nameof(slave));
        if (registerCount is < 1 or > 125) throw new ArgumentOutOfRangeException(nameof(registerCount));
        byte[] frame =
        {
            slave, 0x03,
            (byte)(firstRegister >> 8), (byte)firstRegister,
            (byte)(registerCount >> 8), (byte)registerCount,
            0, 0
        };
        ushort crc = SerialFrameDecoder.ModbusCrc16(frame.AsSpan(0, 6));
        frame[6] = (byte)crc;
        frame[7] = (byte)(crc >> 8);
        return frame;
    }

    public static byte[] AtCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command) || command.Length > 256 ||
            command.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0 ||
            !command.StartsWith("AT", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only one AT command (max 256 chars) is supported.", nameof(command));
        return Encoding.ASCII.GetBytes(command + "\r\n");
    }

    public static bool ValidateNmeaChecksum(string sentence)
    {
        sentence = sentence.TrimEnd('\r', '\n');
        if (sentence.Length is < 5 or > 1024 || sentence[0] != '$')
            return false;
        int star = sentence.LastIndexOf('*');
        if (star < 2 || star != sentence.Length - 3 ||
            !byte.TryParse(sentence.AsSpan(star + 1, 2), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out byte expected))
            return false;
        byte crc = 0;
        for (int i = 1; i < star; i++)
        {
            char ch = sentence[i];
            if (ch > 127 || ch < 32) return false;
            crc ^= (byte)ch;
        }
        return crc == expected;
    }
}
