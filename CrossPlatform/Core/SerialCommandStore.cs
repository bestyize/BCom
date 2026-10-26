using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BCom.CrossPlatform;

/// <summary>Recent quick-send commands; stores at most 30 entries and never captures received data.</summary>
public static class SerialCommandStore
{
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BCom", "quick-commands.txt");
    public static IReadOnlyList<string> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return Array.Empty<string>();
            return File.ReadAllLines(FilePath).Select(x =>
            {
                try { return Encoding.UTF8.GetString(Convert.FromBase64String(x)); }
                catch (FormatException) { return ""; }
            }).Where(x => x.Length is > 0 and <= 4096).ToArray();
        }
        catch (IOException) { return Array.Empty<string>(); }
        catch (UnauthorizedAccessException) { return Array.Empty<string>(); }
    }
    public static void Remember(string command)
    {
        if (string.IsNullOrWhiteSpace(command) || command.Length > 4096) return;
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var values = new[] { command }.Concat(Load().Where(x => x != command)).Take(30);
        var temp = FilePath + ".tmp";
        try
        {
            File.WriteAllLines(temp, values.Select(x => Convert.ToBase64String(Encoding.UTF8.GetBytes(x))));
            File.Move(temp, FilePath, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
