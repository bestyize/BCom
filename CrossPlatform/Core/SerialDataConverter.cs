using System;
using System.Linq;
using System.Text;

namespace BCom.CrossPlatform;

/// <summary>
/// Data encoding, decoding and hexadecimal conversion utilities.
/// </summary>
public enum SerialTextEncoding { Utf8, Ascii, Latin1, Gbk }
public enum SerialLineEnding { None, Cr, Lf, CrLf }

public static class SerialDataConverter
{
    private static readonly byte[] CrLf = { 13, 10 };
    static SerialDataConverter() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    public static Encoding GetEncoding(SerialTextEncoding mode) => mode switch
    {
        SerialTextEncoding.Ascii => Encoding.ASCII,
        SerialTextEncoding.Latin1 => Encoding.Latin1,
        SerialTextEncoding.Gbk => Encoding.GetEncoding(936),
        _ => Encoding.UTF8
    };
    public static string ToTextString(byte[] bytes, SerialTextEncoding mode) => GetEncoding(mode).GetString(bytes);

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
        return PreparePayload(input, isHex, appendNewLine ? SerialLineEnding.CrLf : SerialLineEnding.None,
            SerialTextEncoding.Utf8, out payload, out errorMessage);
    }

    public static bool PreparePayload(string? input, bool isHex, SerialLineEnding ending,
        SerialTextEncoding encoding, out byte[] payload, out string? errorMessage)
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
            payload = GetEncoding(encoding).GetBytes(input ?? string.Empty);
        }

        byte[] suffix = ending switch
        {
            SerialLineEnding.Cr => new byte[] { 13 },
            SerialLineEnding.Lf => new byte[] { 10 },
            SerialLineEnding.CrLf => CrLf,
            _ => Array.Empty<byte>()
        };
        if (suffix.Length > 0) payload = payload.Concat(suffix).ToArray();

        return true;
    }
}
