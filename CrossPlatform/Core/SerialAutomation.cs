using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace BCom.CrossPlatform;

public sealed record SerialTestStep(byte[] Request, byte[] Expected);
public sealed record SerialTestResult(int Step, bool Passed, int ReceivedBytes, string Detail);

/// <summary>
/// Deliberately limited device test format: one "send => expected" on each line.
/// HEX: prefixes describe raw bytes. No shell, loops, variables or eval.
/// </summary>
public static class SerialAutomation
{
    public static IReadOnlyList<SerialTestStep> Parse(string script)
    {
        var steps = new List<SerialTestStep>();
        foreach (string raw in script.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            int separator = line.IndexOf("=>", StringComparison.Ordinal);
            if (separator < 0) throw new FormatException("Each step must contain =>.");
            string request = line[..separator].Trim();
            string expected = line[(separator + 2)..].Trim();
            if (request.Length == 0 || expected.Length == 0)
                throw new FormatException("Request and expected response cannot be empty.");
            var req = Decode(request);
            var exp = Decode(expected);
            if (req.Length > 4096 || exp.Length > 4096)
                throw new FormatException("Maximum step payload is 4096 bytes.");
            steps.Add(new SerialTestStep(req, exp));
            if (steps.Count > 256) throw new FormatException("Maximum 256 test steps.");
        }
        if (steps.Count == 0) throw new FormatException("Test script has no steps.");
        return steps;
    }

    private static byte[] Decode(string s)
    {
        if (s.StartsWith("HEX:", StringComparison.OrdinalIgnoreCase))
        {
            if (!SerialDataConverter.TryParseHex(s[4..].Trim(), out var bytes) || bytes.Length == 0)
                throw new FormatException("Malformed HEX payload.");
            return bytes;
        }
        return Encoding.UTF8.GetBytes(s.Replace("\\r", "\r").Replace("\\n", "\n"));
    }

    public static IReadOnlyList<SerialTestResult> Execute(
        string portName, int baud, IReadOnlyList<SerialTestStep> steps, int waitMs = 1000)
    {
        if (waitMs is < 1 or > 30000) throw new ArgumentOutOfRangeException(nameof(waitMs));
        if (baud is < 1 or > 10000000) throw new ArgumentOutOfRangeException(nameof(baud));
        using var port = new SerialPort(portName, baud) { ReadTimeout = 100, WriteTimeout = 1500 };
        port.Open();
        var results = new List<SerialTestResult>();
        for (int index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            port.DiscardInBuffer();
            port.Write(step.Request, 0, step.Request.Length);
            var got = new List<byte>();
            var sw = Stopwatch.StartNew();
            bool passed = false;
            var buffer = new byte[1024];
            while (sw.ElapsedMilliseconds < waitMs)
            {
                try
                {
                    int count = port.Read(buffer, 0, buffer.Length);
                    for (int i = 0; i < count; i++) got.Add(buffer[i]);
                    if (System.Runtime.InteropServices.CollectionsMarshal.AsSpan(got)
                        .IndexOf(step.Expected.AsSpan()) >= 0)
                    {
                        passed = true;
                        break;
                    }
                    if (got.Count > 64 * 1024) got.RemoveRange(0, got.Count - 4096);
                }
                catch (TimeoutException) { }
            }
            results.Add(new SerialTestResult(index + 1, passed, got.Count,
                passed ? "matched" : "timeout / expected response not found"));
            if (!passed) break;
        }
        return results;
    }
}
