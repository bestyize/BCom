using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace BCom.CrossPlatform;

/// <summary>
/// Handles cross-platform system utility execution and browser URL launching.
/// </summary>
public static class SystemLauncher
{
    public static async Task OpenUrlAsync(IStorageProvider? storageProvider, string url)
    {
        try
        {
            await Avalonia.Input.Platform.Launcher.LaunchUriAsync(new Uri(url));
        }
        catch (Exception)
        {
            // Silently ignore or caller can handle error
        }
    }

    public static bool Launch(string kind, out string? errorMessage)
    {
        errorMessage = null;

        if (kind == "registry" && !OperatingSystem.IsWindows())
        {
            errorMessage = I18n.T("PlatformWindowsOnly");
            return false;
        }

        string[] command = OperatingSystem.IsWindows() ? kind switch
        {
            "calculator" => new[] { "calc.exe" },
            "editor" => new[] { "notepad.exe" },
            "terminal" => new[] { "cmd.exe" },
            "registry" => new[] { "regedit.exe" },
            "screenshot" => new[] { "SnippingTool.exe" },
            "paint" => new[] { "mspaint.exe" },
            _ => Array.Empty<string>()
        } : OperatingSystem.IsMacOS() ? kind switch
        {
            "calculator" => new[] { "open", "-a", "Calculator" },
            "editor" => new[] { "open", "-a", "TextEdit" },
            "terminal" => new[] { "open", "-a", "Terminal" },
            "screenshot" => new[] { "screencapture", "-i" },
            "paint" => new[] { "open", "-a", "Preview" },
            _ => Array.Empty<string>()
        } : kind switch
        {
            "calculator" => new[] { "gnome-calculator" },
            "editor" => new[] { "gedit" },
            "terminal" => new[] { "x-terminal-emulator" },
            "screenshot" => new[] { "gnome-screenshot" },
            "paint" => new[] { "pinta" },
            _ => Array.Empty<string>()
        };

        if (command.Length == 0)
        {
            errorMessage = I18n.T("ErrorTool");
            return false;
        }

        try
        {
            var info = new ProcessStartInfo(command[0]) { UseShellExecute = false };
            foreach (var arg in command.Skip(1))
            {
                info.ArgumentList.Add(arg);
            }
            Process.Start(info);
            return true;
        }
        catch (Exception)
        {
            errorMessage = I18n.T("ErrorTool");
            return false;
        }
    }
}
