using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace BCom.CrossPlatform;
public sealed record SerialPreset(string Name, SerialConfig Config);

/// <summary>Persist named serial configurations independently of the last-used device.</summary>
public static class SerialPresetStore
{
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BCom", "presets.tsv");
    public static IReadOnlyList<SerialPreset> Load()
    {
        var result = new List<SerialPreset>();
        try
        {
            if (!File.Exists(FilePath)) return result;
            foreach (string line in File.ReadAllLines(FilePath))
            {
                try
                {
                    var p = line.Split('|');
                    if (p.Length != 9) continue;
                    string name = Encoding.UTF8.GetString(Convert.FromBase64String(p[0]));
                    string port = Encoding.UTF8.GetString(Convert.FromBase64String(p[1]));
                    if (name.Length is < 1 or > 64 || !int.TryParse(p[2], out int baud) || baud is < 1 or > 10000000 ||
                        !int.TryParse(p[3], out int bits) || bits is < 5 or > 8 ||
                        !Enum.TryParse<StopBits>(p[4], out var stop) || !Enum.IsDefined(stop) ||
                        !Enum.TryParse<Parity>(p[5], out var parity) || !Enum.IsDefined(parity) ||
                        !bool.TryParse(p[6], out bool rts) || !bool.TryParse(p[7], out bool dtr) ||
                        !Enum.TryParse<Handshake>(p[8], out var flow) || !Enum.IsDefined(flow)) continue;
                    result.Add(new SerialPreset(name, new SerialConfig
                    {
                        PortName = port, BaudRate = baud, DataBits = bits, StopBits = stop,
                        Parity = parity, RtsEnable = rts, DtrEnable = dtr, Handshake = flow
                    }));
                }
                catch (FormatException) { }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return result;
    }
    public static void Save(string name, SerialConfig config)
    {
        name = name.Trim();
        if (name.Length is < 1 or > 64) throw new ArgumentException("Profile name must be 1-64 chars.");
        Write(Load().Where(x => !string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
            .Append(new SerialPreset(name, config)));
    }
    public static void Delete(string name) => Write(Load().Where(x => x.Name != name));
    private static void Write(System.Collections.Generic.IEnumerable<SerialPreset> values)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var lines = values.Select(x => string.Join("|",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(x.Name)),
            Convert.ToBase64String(Encoding.UTF8.GetBytes(x.Config.PortName)),
            x.Config.BaudRate, x.Config.DataBits, x.Config.StopBits, x.Config.Parity,
            x.Config.RtsEnable, x.Config.DtrEnable, x.Config.Handshake));
        var temp = FilePath + ".tmp";
        try { File.WriteAllLines(temp, lines); File.Move(temp, FilePath, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
