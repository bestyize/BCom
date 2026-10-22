using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace BCom.CrossPlatform;

/// <summary>
/// Headless command-line interface handler for AI agents and script automation.
/// </summary>
public static class CliHandler
{
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            PrintUsage();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var cmdArgs = args.Skip(1).ToArray();

        return command switch
        {
            "ports" or "list" => ListPorts(cmdArgs),
            "ping" => PingPort(cmdArgs),
            "send" => SendData(cmdArgs),
            "monitor" or "read" => MonitorPort(cmdArgs),
            _ => HandleUnknownCommand(command)
        };
    }

    public static bool IsCliInvocation(string[] args)
    {
        if (args.Length == 0) return false;
        var first = args[0].ToLowerInvariant();
        return first is not ("gui" or "--gui");
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"BCom Serial Monitor & Automation CLI (v3.2.0)

USAGE:
  bcom [command] [options]

COMMANDS:
  ports, list                 List all detected serial communication ports
  ping <port>                 Verify that a serial port can be opened
  send <port> <data>          Send ASCII or HEX payload and optionally read reply
  monitor <port>              Listen on a serial port and stream received data
  gui                         Launch graphical desktop interface (default)
  help, --help, -h            Show this command reference

OPTIONS FOR 'send':
  --baud <rate>               Baud rate (default: 115200)
  --hex                       Interpret input data as space/comma-separated HEX
  --crlf                      Append carriage return and line feed (\r\n)
  --wait <ms>                 Wait up to <ms> milliseconds for response (default: 1000)
  --rts                       Enable Request To Send signal
  --dtr                       Enable Data Terminal Ready signal
  --json                      Output machine-readable JSON format for AI agents

OPTIONS FOR 'monitor':
  --baud <rate>               Baud rate (default: 115200)
  --duration <ms>             Listen duration in milliseconds (default: 5000, 0 = indefinite)
  --hex                       Display captured data in hexadecimal format
  --json                      Output captured buffer as JSON

EXAMPLES FOR AI AGENTS:
  bcom ports --json
  bcom send /dev/tty.usbserial-10 ""AT"" --crlf --wait 1000 --json
  bcom send COM3 ""0A FF 12"" --hex --wait 500
  bcom monitor /dev/ttyACM0 --baud 9600 --duration 3000");
    }

    private static int ListPorts(string[] args)
    {
        var asJson = args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase));
        string[] ports;
        try
        {
            ports = SerialPort.GetPortNames().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (Exception ex)
        {
            if (asJson)
                Console.WriteLine($"{{\"error\":\"{EscapeJson(ex.Message)}\",\"ports\":[]}}");
            else
                Console.Error.WriteLine($"Error enumerating serial ports: {ex.Message}");
            return 1;
        }

        if (asJson)
        {
            var items = string.Join(",", ports.Select(p => $"\"{EscapeJson(p)}\""));
            Console.WriteLine($"{{\"count\":{ports.Length},\"ports\":[{items}]}}");
        }
        else
        {
            if (ports.Length == 0)
            {
                Console.WriteLine("No serial ports detected.");
            }
            else
            {
                foreach (var port in ports)
                {
                    Console.WriteLine(port);
                }
            }
        }
        return 0;
    }

    private static int PingPort(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Error: Missing port name. Usage: bcom ping <port> [--baud <rate>]");
            return 1;
        }

        var portName = args[0];
        if (!TryGetIntOption(args, "--baud", 115200, 1, 10000000, out var baud)) return 2;

        try
        {
            using var port = new SerialPort(portName, baud);
            port.Open();
            Console.WriteLine($"{{\"status\":\"open\",\"port\":\"{EscapeJson(portName)}\",\"baud\":{baud}}}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{{\"status\":\"error\",\"port\":\"{EscapeJson(portName)}\",\"message\":\"{EscapeJson(ex.Message)}\"}}");
            return 1;
        }
    }

    private static int SendData(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Error: Missing parameters. Usage: bcom send <port> <data> [options]");
            return 1;
        }

        var portName = args[0];
        var inputData = args[1];
        if (!TryGetIntOption(args, "--baud", 115200, 1, 10000000, out var baud)) return 2;
        if (!TryGetIntOption(args, "--wait", 1000, 0, 600000, out var waitMs)) return 2;
        var isHex = args.Any(a => a.Equals("--hex", StringComparison.OrdinalIgnoreCase));
        var crlf = args.Any(a => a.Equals("--crlf", StringComparison.OrdinalIgnoreCase));
        var rts = args.Any(a => a.Equals("--rts", StringComparison.OrdinalIgnoreCase));
        var dtr = args.Any(a => a.Equals("--dtr", StringComparison.OrdinalIgnoreCase));
        var asJson = args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase));

        if (!SerialDataConverter.PreparePayload(inputData, isHex, crlf, out var payload, out var prepError))
        {
            var msg = prepError ?? "Invalid hexadecimal payload";
            if (asJson) Console.WriteLine($"{{\"success\":false,\"error\":\"{EscapeJson(msg)}\"}}");
            else Console.Error.WriteLine($"Error: {msg}");
            return 1;
        }

        var receivedBytes = new List<byte>();
        try
        {
            using var port = new SerialPort(portName, baud)
            {
                Encoding = Encoding.UTF8,
                RtsEnable = rts,
                DtrEnable = dtr
            };

            port.Open();

            port.Write(payload, 0, payload.Length);

            if (waitMs > 0)
            {
                var deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
                var buffer = new byte[1024];

                while (DateTime.UtcNow < deadline)
                {
                    if (port.BytesToRead > 0)
                    {
                        int read = port.Read(buffer, 0, Math.Min(buffer.Length, port.BytesToRead));
                        if (read > 0)
                        {
                            for (int i = 0; i < read; i++) receivedBytes.Add(buffer[i]);
                        }
                    }
                    else
                    {
                        Thread.Sleep(20);
                    }
                }
            }

            var rxArray = receivedBytes.ToArray();
            var rxText = SerialDataConverter.ToUtf8String(rxArray);
            var rxHex = SerialDataConverter.ToHexString(rxArray);

            if (asJson)
            {
                Console.WriteLine($"{{\"success\":true,\"port\":\"{EscapeJson(portName)}\",\"sent_bytes\":{payload.Length},\"received_bytes\":{rxArray.Length},\"response\":\"{EscapeJson(rxText)}\",\"hex\":\"{rxHex}\"}}");
            }
            else
            {
                Console.WriteLine($"Sent {payload.Length} bytes.");
                if (rxArray.Length > 0)
                {
                    Console.WriteLine("--- Received Response ---");
                    Console.WriteLine(isHex ? rxHex : rxText);
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            if (asJson)
                Console.WriteLine($"{{\"success\":false,\"error\":\"{EscapeJson(ex.Message)}\"}}");
            else
                Console.Error.WriteLine($"Serial Communication Error: {ex.Message}");
            return 1;
        }
    }

    private static int MonitorPort(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Error: Missing port name. Usage: bcom monitor <port> [options]");
            return 1;
        }

        var portName = args[0];
        if (!TryGetIntOption(args, "--baud", 115200, 1, 10000000, out var baud)) return 2;
        if (!TryGetIntOption(args, "--duration", 5000, 0, 3600000, out var duration)) return 2;
        var isHex = args.Any(a => a.Equals("--hex", StringComparison.OrdinalIgnoreCase));
        var asJson = args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase));

        // JSON aggregation of an indefinite stream has no bounded result.
        if (asJson && duration == 0)
        {
            Console.Error.WriteLine("Use a finite --duration with --json; indefinite capture must be streamed.");
            return 2;
        }

        var collected = new List<byte>();

        try
        {
            using var port = new SerialPort(portName, baud)
            {
                Encoding = Encoding.UTF8
            };

            port.Open();
            if (!asJson)
            {
                Console.WriteLine($"Monitoring {portName} at {baud} baud (duration: {(duration > 0 ? $"{duration}ms" : "indefinite, press Ctrl+C to stop")})...");
            }

            var deadline = duration > 0 ? DateTime.UtcNow.AddMilliseconds(duration) : DateTime.MaxValue;
            var buffer = new byte[1024];

            while (DateTime.UtcNow < deadline)
            {
                if (port.BytesToRead > 0)
                {
                    int read = port.Read(buffer, 0, Math.Min(buffer.Length, port.BytesToRead));
                    if (read > 0)
                    {
                        for (int i = 0; i < read; i++) collected.Add(buffer[i]);

                        if (!asJson)
                        {
                            var chunk = buffer.Take(read).ToArray();
                            Console.Write(isHex ? SerialDataConverter.ToHexString(chunk) + " " : SerialDataConverter.ToUtf8String(chunk));
                        }
                    }
                }
                else
                {
                    Thread.Sleep(20);
                }
            }

            if (!asJson && isHex) Console.WriteLine();

            if (asJson)
            {
                var array = collected.ToArray();
                Console.WriteLine($"{{\"port\":\"{EscapeJson(portName)}\",\"total_bytes\":{array.Length},\"text\":\"{EscapeJson(SerialDataConverter.ToUtf8String(array))}\",\"hex\":\"{SerialDataConverter.ToHexString(array)}\"}}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            if (asJson)
                Console.WriteLine($"{{\"error\":\"{EscapeJson(ex.Message)}\"}}");
            else
                Console.Error.WriteLine($"Monitor Error: {ex.Message}");
            return 1;
        }
    }

    private static bool TryGetIntOption(
        string[] args, string flag, int defaultValue, int min, int max, out int value)
    {
        value = defaultValue;
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].Equals(flag, StringComparison.OrdinalIgnoreCase)) continue;
            if (i + 1 >= args.Length ||
                !int.TryParse(args[i + 1], out var parsed) ||
                parsed < min || parsed > max)
            {
                Console.Error.WriteLine($"Invalid {flag}: expected an integer between {min} and {max}.");
                return false;
            }
            value = parsed;
        }
        return true;
    }

    private static string EscapeJson(string? s)
    {
        // JsonEncodedText handles all U+0000..U+001F control characters,
        // quotes and backslashes (unlike the previous manual replacement).
        return JsonEncodedText.Encode(s ?? string.Empty).ToString();
    }

    private static int HandleUnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: '{command}'. Use 'bcom help' for supported options.");
        return 1;
    }
}
