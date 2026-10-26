using System;
using System.Collections.Generic;
using System.Linq;

namespace BCom.CrossPlatform;

public sealed record SerialMacroStep(byte[] Payload, int WaitMilliseconds);

/// <summary>
/// Explicit, non-Turing-complete macro DSL: TEXT:, HEX:, WAIT:.
/// No loops, shell execution, implicit retries or background replays.
/// </summary>
public static class SerialMacroScript
{
    public static IReadOnlyList<SerialMacroStep> Parse(string source, SerialTextEncoding encoding, SerialLineEnding ending)
    {
        var steps = new List<SerialMacroStep>();
        int totalBytes = 0;
        foreach (var raw in source.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (steps.Count >= 100) throw new FormatException("Maximum 100 instructions.");
            if (line.StartsWith("WAIT:", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(line[5..].Trim(), out int delay) || delay is < 0 or > 30000)
                    throw new FormatException("WAIT must be 0–30000 milliseconds.");
                steps.Add(new SerialMacroStep(Array.Empty<byte>(), delay));
            }
            else
            {
                bool hex = line.StartsWith("HEX:", StringComparison.OrdinalIgnoreCase);
                if (!hex && !line.StartsWith("TEXT:", StringComparison.OrdinalIgnoreCase))
                    throw new FormatException("Macro instructions must use TEXT:, HEX: or WAIT:.");
                if (!SerialDataConverter.PreparePayload(line[(hex ? 4 : 5)..], hex,
                    hex ? SerialLineEnding.None : ending, encoding, out var bytes, out _))
                    throw new FormatException("Invalid HEX instruction.");
                if (bytes.Length is < 1 or > 4096)
                    throw new FormatException("Macro send must contain 1–4096 bytes.");
                totalBytes += bytes.Length;
                if (totalBytes > 256 * 1024) throw new FormatException("Macro exceeds 256 KiB.");
                steps.Add(new SerialMacroStep(bytes, 0));
            }
        }
        if (steps.Count == 0) throw new FormatException("No macro instructions.");
        if (steps.Sum(s => s.WaitMilliseconds) > 120000)
            throw new FormatException("Macro waits exceed 120 seconds.");
        return steps;
    }
}
