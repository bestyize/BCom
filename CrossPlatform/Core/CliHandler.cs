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
            "replay" => ReplayCapture(cmdArgs),
            "verify" => VerifyScript(cmdArgs),
            "decode" => DecodeCapture(cmdArgs),
            "template" => PrintProtocolTemplate(cmdArgs),
            "mcp" => RunMcpServer(cmdArgs),
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
        Console.WriteLine(@"BCom Serial Monitor & Automation CLI (v3.3.0)

USAGE:
  bcom [command] [options]

COMMANDS:
  ports, list                 List all detected serial communication ports
  ping <port>                 Verify that a serial port can be opened
  send <port> <data>          Send ASCII or HEX payload and optionally read reply
  monitor <port>              Listen on a serial port and stream received data
  replay <file.bcomcap>       Replay a recorded capture as timestamped JSONL
  verify <port> <script.txt>  Execute bounded send/expect steps against a port
  decode <file> <mode>        Decode RX capture into frames (lf, length, fixed:N)
  template <protocol>         Generate safe AT/Modbus frames or verify NMEA checksums
  mcp [--allow-write --port P] Run a local MCP stdio server (read-only by default)
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
  --jsonl                     Emit timestamped newline-delimited JSON events

EXAMPLES FOR AI AGENTS:
  bcom ports --json
  bcom send /dev/tty.usbserial-10 ""AT"" --crlf --wait 1000 --json
  bcom send COM3 ""0A FF 12"" --hex --wait 500
  bcom monitor /dev/ttyACM0 --baud 9600 --duration 3000");
    }

    private static int PrintProtocolTemplate(string[] args)
    {
        try
        {
            byte[] bytes;
            if (args.Length is >= 1 and <= 2 && args[0] == "at")
                bytes = SerialProtocolTemplates.AtCommand(args.Length == 2 ? args[1] : "AT");
            else if (args.Length == 4 && args[0] == "modbus-read" &&
                byte.TryParse(args[1], out var slave) &&
                ushort.TryParse(args[2], out var first) &&
                ushort.TryParse(args[3], out var count))
                bytes = SerialProtocolTemplates.ModbusReadHoldingRegisters(slave, first, count);
            else if (args.Length == 2 && args[0] == "nmea-check")
            {
                bool valid = SerialProtocolTemplates.ValidateNmeaChecksum(args[1]);
                Console.WriteLine(valid ? "valid checksum" : "invalid checksum");
                return valid ? 0 : 1;
            }
            else
            {
                Console.Error.WriteLine("Usage: BCom template at [command] | modbus-read <slave> <register> <count> | nmea-check <sentence>");
                return 2;
            }
            Console.WriteLine(SerialDataConverter.ToHexString(bytes));
            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static int DecodeCapture(string[] args)
    {
        if (args.Length is < 2 or > 3 || (args.Length == 3 && args[2] != "--crc"))
        {
            Console.Error.WriteLine("Usage: BCom decode <file.bcomcap> <lf|length|fixed:N> [--crc]");
            return 2;
        }
        try
        {
            var mode = args[1].ToLowerInvariant();
            SerialFrameDecoder decoder;
            if (mode == "lf") decoder = new SerialFrameDecoder(SerialFramingMode.Newline);
            else if (mode == "length") decoder = new SerialFrameDecoder(SerialFramingMode.LengthPrefixed);
            else if (mode.StartsWith("fixed:", StringComparison.Ordinal) &&
                int.TryParse(mode[6..], out int size))
                decoder = new SerialFrameDecoder(SerialFramingMode.FixedLength, size);
            else throw new ArgumentException("Invalid frame mode.");
            DecodeToConsoleAsync(args[0], decoder, args.Length == 3).GetAwaiter().GetResult();
            return 0;
        }
        catch (Exception e) when (e is ArgumentException or System.IO.IOException
            or UnauthorizedAccessException or InvalidOperationException)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    private static async System.Threading.Tasks.Task DecodeToConsoleAsync(
        string file, SerialFrameDecoder decoder, bool checkCrc)
    {
        await foreach (var e in SerialCapture.ReplayAsync(file))
        {
            if (e.Direction != SerialTraceDirection.Rx) continue;
            foreach (var frame in decoder.Append(e.Data))
            {
                var buffer = new System.Buffers.ArrayBufferWriter<byte>();
                using (var writer = new Utf8JsonWriter(buffer))
                {
                    writer.WriteStartObject();
                    writer.WriteString("timestamp", e.Timestamp);
                    writer.WriteString("port", e.Port);
                    writer.WriteNumber("bytes", frame.Length);
                    writer.WriteString("hex", SerialDataConverter.ToHexString(frame));
                    if (checkCrc) writer.WriteBoolean("modbus_crc_ok",
                        SerialFrameDecoder.CheckModbusCrc(frame));
                    writer.WriteEndObject();
                    writer.Flush();
                }
                Console.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
            }
        }
    }

    private static int RunMcpServer(string[] args)
    {
        if (args.Length == 1 && args[0] == "--help")
        {
            Console.Error.WriteLine("Usage: BCom mcp [--allow-write --port <exact-port-name>]");
            return 0;
        }
        bool allowWrite = false;
        string? port = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--allow-write") allowWrite = true;
            else if (args[i] == "--port" && i + 1 < args.Length) port = args[++i];
            else
            {
                Console.Error.WriteLine("Unknown MCP option. Usage: mcp [--allow-write --port PORT]");
                return 2;
            }
        }
        if (allowWrite && string.IsNullOrWhiteSpace(port))
        {
            Console.Error.WriteLine("Write permission requires an explicitly allowed port.");
            return 2;
        }
        SerialMcpServer.RunAsync(Console.In, Console.Out, allowWrite, port, audit: Console.Error)
            .GetAwaiter().GetResult();
        return 0;
    }

    private static int VerifyScript(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: bcom verify <port> <script.txt> [--baud rate] [--wait ms]");
            return 2;
        }
        int baud = 115200, wait = 1000;
        for (int i = 2; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out int value))
                return 2;
            if (args[i] == "--baud") baud = value;
            else if (args[i] == "--wait") wait = value;
            else return 2;
        }
        try
        {
            var steps = SerialAutomation.Parse(System.IO.File.ReadAllText(args[1]));
            var results = SerialAutomation.Execute(args[0], baud, steps, wait);
            foreach (var result in results)
                Console.WriteLine($"Step {result.Step}: {(result.Passed ? "PASS" : "FAIL")} ({result.Detail}, {result.ReceivedBytes} bytes)");
            return results.Count == steps.Count && results.All(x => x.Passed) ? 0 : 1;
        }
        catch (Exception e) when (e is FormatException or System.IO.IOException
            or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    private static int ReplayCapture(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: bcom replay <file.bcomcap>");
            return 2;
        }
        try
        {
            ReplayToConsoleAsync(args[0]).GetAwaiter().GetResult();
            return 0;
        }
        catch (Exception e) when (e is System.IO.IOException or System.IO.InvalidDataException
            or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    private static async System.Threading.Tasks.Task ReplayToConsoleAsync(string file)
    {
        await foreach (var entry in SerialCapture.ReplayAsync(file))
        {
            var writer = new System.Buffers.ArrayBufferWriter<byte>();
            using (var json = new Utf8JsonWriter(writer))
            {
                json.WriteStartObject();
                json.WriteString("timestamp", entry.Timestamp);
                json.WriteString("port", entry.Port);
                json.WriteString("direction", entry.Direction == SerialTraceDirection.Rx ? "rx" : "tx");
                json.WriteNumber("bytes", entry.Data.Length);
                json.WriteString("hex", SerialDataConverter.ToHexString(entry.Data));
                json.WriteEndObject();
                json.Flush();
            }
            Console.WriteLine(Encoding.UTF8.GetString(writer.WrittenSpan));
        }
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
        var asJsonLines = args.Any(a => a.Equals("--jsonl", StringComparison.OrdinalIgnoreCase));
        if (asJson && asJsonLines)
        {
            Console.Error.WriteLine("Choose either --json or --jsonl.");
            return 2;
        }

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
            if (!asJson && !asJsonLines)
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
                        if (asJson)
                            for (int i = 0; i < read; i++) collected.Add(buffer[i]);

                        if (asJsonLines)
                        {
                            var chunk = buffer.Take(read).ToArray();
                            string timestamp = DateTimeOffset.UtcNow.ToString("O");
                            Console.WriteLine($"{{\"event\":\"data\",\"timestamp\":\"{timestamp}\",\"port\":\"{EscapeJson(portName)}\",\"bytes\":{read},\"hex\":\"{SerialDataConverter.ToHexString(chunk)}\"}}");
                            Console.Out.Flush();
                        }
                        else if (!asJson)
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

            if (!asJson && !asJsonLines && isHex) Console.WriteLine();

            if (asJson)
            {
                var array = collected.ToArray();
                Console.WriteLine($"{{\"port\":\"{EscapeJson(portName)}\",\"total_bytes\":{array.Length},\"text\":\"{EscapeJson(SerialDataConverter.ToUtf8String(array))}\",\"hex\":\"{SerialDataConverter.ToHexString(array)}\"}}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            if (asJson || asJsonLines)
                Console.WriteLine($"{{\"event\":\"error\",\"error\":\"{EscapeJson(ex.Message)}\"}}");
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
