using System;
using System.Linq;
using System.Text;

namespace BCom.CrossPlatform;

/// <summary>
/// Data encoding, decoding and hexadecimal conversion utilities.
/// </summary>
public static class SerialDataConverter
{
    private static readonly byte[] CrLf = { 13, 10 };

    /// <summary>
    /// Formats a byte buffer into space-separated hexadecimal string representation (e.g. "0A FF 12").
    /// </summary>
    public static string ToHexString(byte[] data)
    {
        if (data == null || data.Length == 0) return string.Empty;
        return BitConverter.ToString(data).Replace("-", " ");
    }

    /// <summary>
    /// Decodes a byte buffer into a UTF-8 string.
    /// </summary>
    public static string ToUtf8String(byte[] data)
    {
        if (data == null || data.Length == 0) return string.Empty;
        return Encoding.UTF8.GetString(data);
    }

    /// <summary>
    /// Validates and parses a hexadecimal string into raw bytes.
    /// Supports inputs with spaces, commas, or dashes.
    /// </summary>
    public static bool TryParseHex(string? input, out byte[] result)
    {
        result = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(input)) return true;

        var normalized = new string(input.Where(c => !char.IsWhiteSpace(c) && c != ',' && c != '-').ToArray());
        if (normalized.Length % 2 != 0 || !normalized.All(Uri.IsHexDigit))
        {
            return false;
        }

        try
        {
            result = Convert.FromHexString(normalized);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a text input into bytes, optionally formatting as HEX or appending CRLF.
    /// </summary>
    public static bool PreparePayload(string? input, bool isHex, bool appendNewLine, out byte[] payload, out string? errorMessage)
    {
        errorMessage = null;
        if (isHex)
        {
            if (!TryParseHex(input, out payload))
            {
                errorMessage = I18n.T("ErrorHex");
                return false;
            }
        }
        else
        {
            payload = Encoding.UTF8.GetBytes(input ?? string.Empty);
        }

        if (appendNewLine)
        {
            payload = payload.Concat(CrLf).ToArray();
        }

        return true;
    }
}
