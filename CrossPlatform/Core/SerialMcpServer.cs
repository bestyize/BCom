using System;
using System.Buffers;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BCom.CrossPlatform;

/// <summary>
/// Local, stdio-only JSON-RPC MCP server. By default it exposes only port
/// enumeration. Serial writes require BOTH --allow-write and --port NAME.
/// No shell execution, network listener, arbitrary path access, or discovery-based
/// escalation of permissions is supported.
/// </summary>
public static class SerialMcpServer
{
    public static async Task RunAsync(TextReader input, TextWriter output,
        bool allowWrite = false, string? allowedPort = null, CancellationToken cancellation = default)
    {
        while (true)
        {
            string? line = await input.ReadLineAsync(cancellation);
            if (line == null) return;
            if (line.Length is 0 or > 65536) continue;
            try
            {
                using var document = JsonDocument.Parse(line);
                var request = document.RootElement;
                if (request.ValueKind != JsonValueKind.Object) continue;
                if (!request.TryGetProperty("id", out var id)) continue;
                string method = GetString(request, "method") ?? "";
                var buffer = new ArrayBufferWriter<byte>();
                using (var json = new Utf8JsonWriter(buffer))
                {
                    json.WriteStartObject();
                    json.WriteString("jsonrpc", "2.0");
                    json.WritePropertyName("id");
                    id.WriteTo(json);
                    if (method == "initialize")
                    {
                        json.WritePropertyName("result");
                        json.WriteStartObject();
                        json.WriteString("protocolVersion", "2025-06-18");
                        json.WritePropertyName("capabilities");
                        json.WriteStartObject();
                        json.WritePropertyName("tools");
                        json.WriteStartObject();
                        json.WriteEndObject();
                        json.WriteEndObject();
                        json.WritePropertyName("serverInfo");
                        json.WriteStartObject();
                        json.WriteString("name", "bcom-serial");
                        json.WriteString("version", "1.0");
                        json.WriteEndObject();
                        json.WriteEndObject();
                    }
                    else if (method == "ping")
                    {
                        json.WritePropertyName("result");
                        json.WriteStartObject();
                        json.WriteEndObject();
                    }
                    else if (method == "tools/list")
                    {
                        json.WritePropertyName("result");
                        json.WriteStartObject();
                        json.WritePropertyName("tools");
                        json.WriteStartArray();
                        WriteTool(json, "serial_list_ports", "List available local serial ports.", false);
                        if (allowWrite && !string.IsNullOrWhiteSpace(allowedPort))
                            WriteTool(json, "serial_send", "Send bytes only to the explicitly allowed serial port.", true);
                        json.WriteEndArray();
                        json.WriteEndObject();
                    }
                    else if (method == "tools/call")
                    {
                        string tool = request.TryGetProperty("params", out var parameters) &&
                            parameters.ValueKind == JsonValueKind.Object
                            ? GetString(parameters, "name") ?? "" : "";
                        var arguments = parameters.ValueKind == JsonValueKind.Object &&
                            parameters.TryGetProperty("arguments", out var args) ? args : default;
                        bool authorizedSend = allowWrite && !string.IsNullOrWhiteSpace(allowedPort);
                        string result;
                        bool isError = false;
                        if (tool == "serial_list_ports")
                        {
                            try { result = string.Join("\n", SerialPort.GetPortNames().OrderBy(x => x)); }
                            catch (Exception e) { result = e.Message; isError = true; }
                        }
                        else if (tool == "serial_send")
                        {
                            if (!authorizedSend)
                            {
                                result = "Write access denied. Start BCom with --allow-write --port <name>.";
                                isError = true;
                            }
                            else
                            {
                                try { result = SendAllowedPort(arguments, allowedPort!); }
                                catch (Exception e) when (e is ArgumentException or InvalidOperationException
                                    or IOException or UnauthorizedAccessException or TimeoutException)
                                {
                                    result = e.Message;
                                    isError = true;
                                }
                            }
                        }
                        else { result = "Unknown tool."; isError = true; }
                        json.WritePropertyName("result");
                        json.WriteStartObject();
                        json.WritePropertyName("content");
                        json.WriteStartArray();
                        json.WriteStartObject();
                        json.WriteString("type", "text");
                        json.WriteString("text", result);
                        json.WriteEndObject();
                        json.WriteEndArray();
                        if (isError) json.WriteBoolean("isError", true);
                        json.WriteEndObject();
                    }
                    else
                    {
                        json.WritePropertyName("error");
                        json.WriteStartObject();
                        json.WriteNumber("code", -32601);
                        json.WriteString("message", "Method not found");
                        json.WriteEndObject();
                    }
                    json.WriteEndObject();
                    json.Flush();
                }
                await output.WriteLineAsync(Encoding.UTF8.GetString(buffer.WrittenSpan));
                await output.FlushAsync();
            }
            catch (JsonException)
            {
                // Malformed requests are discarded without executing any device action.
            }
        }
    }

    private static string? GetString(JsonElement value, string key) =>
        value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty(key, out var field) &&
        field.ValueKind == JsonValueKind.String ? field.GetString() : null;

    private static void WriteTool(Utf8JsonWriter json, string name, string description, bool sends)
    {
        json.WriteStartObject();
        json.WriteString("name", name);
        json.WriteString("description", description);
        json.WritePropertyName("inputSchema");
        json.WriteStartObject();
        json.WriteString("type", "object");
        json.WritePropertyName("properties");
        json.WriteStartObject();
        if (sends)
        {
            json.WritePropertyName("data");
            json.WriteStartObject();
            json.WriteString("type", "string");
            json.WriteEndObject();
            json.WritePropertyName("hex");
            json.WriteStartObject();
            json.WriteString("type", "boolean");
            json.WriteEndObject();
        }
        json.WriteEndObject();
        if (sends)
        {
            json.WritePropertyName("required");
            json.WriteStartArray();
            json.WriteStringValue("data");
            json.WriteEndArray();
        }
        json.WriteEndObject();
        json.WriteEndObject();
    }

    private static string SendAllowedPort(JsonElement args, string portName)
    {
        string? data = GetString(args, "data");
        if (data == null) throw new ArgumentException("A data string is required.");
        bool hex = args.ValueKind == JsonValueKind.Object &&
            args.TryGetProperty("hex", out var h) && h.ValueKind == JsonValueKind.True;
        if (!SerialDataConverter.PreparePayload(data, hex, false, out var payload, out var error))
            throw new ArgumentException(error);
        if (payload.Length > 4096)
            throw new ArgumentException("Maximum 4096 bytes per request.");
        using var port = new SerialPort(portName, 115200) { WriteTimeout = 1500 };
        port.Open();
        port.Write(payload, 0, payload.Length);
        return "Sent " + payload.Length + " bytes to " + portName;
    }
}
