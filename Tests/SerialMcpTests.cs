using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BCom.CrossPlatform;
using Xunit;

namespace BCom.Tests;

public sealed class SerialMcpTests
{
    [Fact]
    public async Task InitializationAndReadOnlyToolListingWorkWithoutHardware()
    {
        using var input = new StringReader(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\"}\n" +
            "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}\n");
        using var output = new StringWriter();
        await SerialMcpServer.RunAsync(input, output);
        var lines = output.ToString().Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        using var init = JsonDocument.Parse(lines[0]);
        Assert.Equal("2025-06-18", init.RootElement.GetProperty("result")
            .GetProperty("protocolVersion").GetString());
        using var list = JsonDocument.Parse(lines[1]);
        var tools = list.RootElement.GetProperty("result").GetProperty("tools");
        Assert.Equal(1, tools.GetArrayLength());
        Assert.Equal("serial_list_ports", tools[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task ReadOnlyServerExplicitlyDeniesSerialWrites()
    {
        using var input = new StringReader(
            "{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"tools/call\",\"params\":{\"name\":\"serial_send\",\"arguments\":{\"data\":\"AT\"}}}\n");
        using var output = new StringWriter();
        await SerialMcpServer.RunAsync(input, output);
        using var response = JsonDocument.Parse(output.ToString());
        Assert.True(response.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
    }

    [Fact]
    public async Task AuditRecordsDeniedDeviceAccessWithoutIncludingPayload()
    {
        using var input = new StringReader(
            "{\"jsonrpc\":\"2.0\",\"id\":5,\"method\":\"tools/call\",\"params\":{\"name\":\"serial_send\",\"arguments\":{\"data\":\"SECRET\\"}}}\n");
        using var output = new StringWriter();
        using var audit = new StringWriter();
        await SerialMcpServer.RunAsync(input, output, audit: audit);
        Assert.Contains("tool=serial_send", audit.ToString());
        Assert.Contains("outcome=error", audit.ToString());
        Assert.DoesNotContain("SECRET", audit.ToString());
    }

    [Fact]
    public async Task UnsafeArgsDoNotTerminateService()
    {
        using var input = new StringReader(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":null}\n" +
            "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"ping\"}\n");
        using var output = new StringWriter();
        await SerialMcpServer.RunAsync(input, output);
        Assert.Equal(2, output.ToString().Split('\n',
            System.StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
