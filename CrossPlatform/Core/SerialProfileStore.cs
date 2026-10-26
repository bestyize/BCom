using System;
using System.IO;
using System.IO.Ports;

namespace BCom.CrossPlatform;

/// <summary>
/// Stores the last successfully opened serial configuration without exposing
/// credentials or requiring a reflection-based JSON serializer in trimmed builds.
/// </summary>
public static class SerialProfileStore
{
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BCom", "serial-profile.ini");

    public static SerialConfig? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var profile = new SerialConfig();
            foreach (string line in File.ReadAllLines(FilePath))
            {
                int split = line.IndexOf('=');
                if (split <= 0) continue;
                string key = line[..split], value = line[(split + 1)..];
                switch (key)
                {
                    case "port": profile.PortName = value; break;
                    case "baud" when int.TryParse(value, out var baud) && baud is >= 1 and <= 10000000:
                        profile.BaudRate = baud; break;
                    case "bits" when int.TryParse(value, out var bits) && bits is >= 5 and <= 8:
                        profile.DataBits = bits; break;
                    case "stop" when Enum.TryParse<StopBits>(value, out var stop):
                        profile.StopBits = stop; break;
                    case "parity" when Enum.TryParse<Parity>(value, out var parity):
                        profile.Parity = parity; break;
                    case "rts" when bool.TryParse(value, out var rts):
                        profile.RtsEnable = rts; break;
                    case "dtr" when bool.TryParse(value, out var dtr):
                        profile.DtrEnable = dtr; break;
                    case "handshake" when Enum.TryParse<Handshake>(value, out var flow) && Enum.IsDefined(flow):
                        profile.Handshake = flow; break;
                }
            }
            return profile;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    public static void Save(SerialConfig config)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllLines(FilePath, new[]
            {
                $"port={config.PortName}",
                $"baud={config.BaudRate}",
                $"bits={config.DataBits}",
                $"stop={config.StopBits}",
                $"parity={config.Parity}",
                $"rts={config.RtsEnable}",
                $"dtr={config.DtrEnable}",
                $"handshake={config.Handshake}"
            });
        }
        catch (IOException) { /* Read-only user profile: keep running. */ }
        catch (UnauthorizedAccessException) { }
    }
}
