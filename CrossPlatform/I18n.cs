using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BCom.CrossPlatform;

/// <summary>English fallback, Simplified Chinese override and persisted language preference.</summary>
internal static class I18n
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BCom", "settings.json");

    private static readonly Dictionary<string, string> English = new()
    {
        ["Title"] = "BCom · Serial Monitor",
        ["Subtitle"] = "Cross-platform serial debugging assistant",
        ["Language"] = "Language",
        ["File"] = "File",
        ["OpenFile"] = "Open text file",
        ["SaveFile"] = "Save received text",
        ["Tools"] = "Tools",
        ["Calculator"] = "Calculator",
        ["Notepad"] = "Text editor",
        ["Terminal"] = "Terminal",
        ["Registry"] = "Registry Editor (Windows)",
        ["Screenshot"] = "Screenshot tool",
        ["Paint"] = "Image editor",
        ["MindMap"] = "Mind map (web)",
        ["ModeCompact"] = "Compact mode",
        ["ModeAdvanced"] = "Full mode",
        ["Help"] = "Help",
        ["Source"] = "Source code",
        ["WeChat"] = "WeChat official account",
        ["About"] = "About",
        ["Receive"] = "RECEIVED DATA",
        ["SendPanel"] = "SEND DATA",
        ["SerialSettings"] = "SERIAL SETTINGS",
        ["Port"] = "Serial port",
        ["Refresh"] = "Refresh",
        ["Baud"] = "Baud rate",
        ["DataBits"] = "Data bits",
        ["StopBits"] = "Stop bits",
        ["Parity"] = "Parity",
        ["OpenPort"] = "Open port",
        ["ClosePort"] = "Close port",
        ["Send"] = "Send",
        ["ClearSend"] = "Clear input",
        ["ClearReceive"] = "Clear received",
        ["HexDisplay"] = "Display as HEX",
        ["HexSend"] = "Send as HEX",
        ["NewLine"] = "Append CRLF",
        ["Periodic"] = "Repeat every (ms)",
        ["ConnectedState"] = "Connected",
        ["DisconnectedState"] = "Disconnected",
        ["Export"] = "Export",
        ["Import"] = "Import",
        ["Signals"] = "Signals (RTS/DTR)",
        ["AutoRepeat"] = "Repeat Send",
        ["RxLabel"] = "Rx",
        ["TxLabel"] = "Tx",
        ["BytesUnit"] = "B",
        ["Theme"] = "Theme",
        ["ThemeSystem"] = "System",
        ["ThemeLight"] = "Light",
        ["ThemeDark"] = "Dark",
        ["Rts"] = "RTS",
        ["Dtr"] = "DTR",
        ["StatusReady"] = "Ready",
        ["StatusOpen"] = "Connected to {0}",
        ["StatusClosed"] = "Port closed",
        ["StatusNoPorts"] = "No serial ports found",
        ["Error"] = "Error",
        ["ErrorOpen"] = "Cannot open {0}: {1}",
        ["ErrorClose"] = "Failed to close the serial port: {0}",
        ["ErrorNoPort"] = "Select an available serial port first.",
        ["ErrorNotOpen"] = "Open a serial port before sending data.",
        ["ErrorDisconnected"] = "The serial device was disconnected.",
        ["ErrorBaud"] = "Enter a baud rate between 1 and 10,000,000.",
        ["ErrorHex"] = "Invalid hexadecimal input. Use pairs of hex digits, e.g. 0A FF 12.",
        ["ErrorTimer"] = "Enter a positive interval in milliseconds.",
        ["ErrorSend"] = "Could not send data: {0}",
        ["ErrorReceive"] = "Serial receive error: {0}",
        ["ErrorPorts"] = "Could not enumerate serial ports: {0}",
        ["ErrorTool"] = "The selected system tool is not available on this platform.",
        ["ErrorFile"] = "File operation failed: {0}",
        ["Saved"] = "File saved.",
        ["AboutTitle"] = "About BCom",
        ["AboutBody"] = "BCom is a free, open-source serial port debugging assistant.\nBuilt with .NET 10 and Avalonia for Windows, macOS and Linux.",
        ["QrTitle"] = "WeChat official account",
        ["QrSubtitle"] = "Scan this QR code using WeChat",
        ["Ok"] = "OK",
        ["TextFiles"] = "Text files",
        ["AllFiles"] = "All files",
        ["ReceivePlaceholder"] = "Incoming serial data will appear here...",
        ["SendPlaceholder"] = "Type or paste the data to send...",
        ["PlatformWindowsOnly"] = "This utility is only available on Windows.",
        ["NotSupported"] = "This feature is not available on this system."
    };

    private static readonly Dictionary<string, string> Chinese = new()
    {
        ["Title"] = "BCom · 串口调试助手",
        ["Subtitle"] = "跨平台多功能串口调试助手",
        ["Language"] = "语言",
        ["File"] = "文件",
        ["OpenFile"] = "打开文本文件",
        ["SaveFile"] = "保存接收内容",
        ["Tools"] = "工具",
        ["Calculator"] = "计算器",
        ["Notepad"] = "文本编辑器",
        ["Terminal"] = "终端",
        ["Registry"] = "注册表编辑器（仅 Windows）",
        ["Screenshot"] = "截图工具",
        ["Paint"] = "图像编辑器",
        ["MindMap"] = "思维导图（网页）",
        ["ModeCompact"] = "精简模式",
        ["ModeAdvanced"] = "完整模式",
        ["Help"] = "帮助",
        ["Source"] = "开源地址",
        ["WeChat"] = "微信公众号",
        ["About"] = "关于",
        ["Receive"] = "接收数据",
        ["SendPanel"] = "发送数据",
        ["SerialSettings"] = "串口设置",
        ["Port"] = "串口选择",
        ["Refresh"] = "刷新",
        ["Baud"] = "波特率",
        ["DataBits"] = "数据位",
        ["StopBits"] = "停止位",
        ["Parity"] = "奇偶校验",
        ["OpenPort"] = "打开串口",
        ["ClosePort"] = "关闭串口",
        ["Send"] = "发送",
        ["ClearSend"] = "清空输入",
        ["ClearReceive"] = "清除接收",
        ["HexDisplay"] = "16 进制显示",
        ["HexSend"] = "16 进制发送",
        ["NewLine"] = "追加换行 CRLF",
        ["Periodic"] = "定时发送间隔（毫秒）",
        ["ConnectedState"] = "已连接",
        ["DisconnectedState"] = "未连接",
        ["Export"] = "导出",
        ["Import"] = "导入",
        ["Signals"] = "流控信号 (RTS/DTR)",
        ["AutoRepeat"] = "自动重发",
        ["RxLabel"] = "接收",
        ["TxLabel"] = "发送",
        ["BytesUnit"] = "字节",
        ["Theme"] = "主题",
        ["ThemeSystem"] = "跟随系统",
        ["ThemeLight"] = "日间浅色",
        ["ThemeDark"] = "夜间深色",
        ["Rts"] = "RTS",
        ["Dtr"] = "DTR",
        ["StatusReady"] = "就绪",
        ["StatusOpen"] = "已连接 {0}",
        ["StatusClosed"] = "串口已关闭",
        ["StatusNoPorts"] = "未发现可用串口",
        ["Error"] = "错误",
        ["ErrorOpen"] = "无法打开 {0}：{1}",
        ["ErrorClose"] = "关闭串口失败：{0}",
        ["ErrorNoPort"] = "请先选择可用串口。",
        ["ErrorNotOpen"] = "请先打开串口再发送。",
        ["ErrorDisconnected"] = "串口设备已断开连接。",
        ["ErrorBaud"] = "请输入 1 到 10,000,000 之间的波特率。",
        ["ErrorHex"] = "十六进制输入无效，请使用成对数字，例如 0A FF 12。",
        ["ErrorTimer"] = "请输入有效的正数毫秒间隔。",
        ["ErrorSend"] = "发送失败：{0}",
        ["ErrorReceive"] = "接收失败：{0}",
        ["ErrorPorts"] = "无法枚举串口：{0}",
        ["ErrorTool"] = "当前系统没有可用的对应工具。",
        ["ErrorFile"] = "文件操作失败：{0}",
        ["Saved"] = "文件已保存。",
        ["AboutTitle"] = "关于 BCom",
        ["AboutBody"] = "BCom 是免费的开源串口调试助手。\n基于 .NET 10 与 Avalonia，可在 Windows、macOS、Linux 上运行。",
        ["QrTitle"] = "微信公众号",
        ["QrSubtitle"] = "使用微信扫描二维码",
        ["Ok"] = "确定",
        ["TextFiles"] = "文本文件",
        ["AllFiles"] = "所有文件",
        ["ReceivePlaceholder"] = "这里显示接收到的串口数据…",
        ["SendPlaceholder"] = "在此输入或粘贴要发送的数据…",
        ["PlatformWindowsOnly"] = "此工具仅支持 Windows 系统。",
        ["NotSupported"] = "当前系统不支持此功能。"
    };

    public static string Preference { get; private set; } = LoadPreference();
    public static string ThemePreference { get; private set; } = LoadThemePreference();

    public static bool IsChinese => Preference switch
    {
        "zh-CN" => true,
        "en" => false,
        _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
    };

    public static string T(string key)
    {
        if (IsChinese && Chinese.TryGetValue(key, out var translation)) return translation;
        return English.TryGetValue(key, out var english) ? english : key;
    }

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(key), args);

    public static void Change(string preference)
    {
        if (preference is not ("auto" or "en" or "zh-CN")) return;
        Preference = preference;
        SaveSettings();
    }

    public static void ChangeTheme(string theme)
    {
        if (theme is not ("auto" or "light" or "dark")) return;
        ThemePreference = theme;
        SaveSettings();
    }

    private static void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, $"{{\"Language\":\"{Preference}\",\"Theme\":\"{ThemePreference}\"}}");
        }
        catch (IOException) { /* Read-only settings location: keep session selection. */ }
        catch (UnauthorizedAccessException) { }
    }

    private static string LoadPreference()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var text = File.ReadAllText(SettingsPath);
                if (text.Contains("\"Language\":\"zh-CN\"", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
                if (text.Contains("\"Language\":\"en\"", StringComparison.OrdinalIgnoreCase)) return "en";
                if (text.Contains("\"Language\":\"auto\"", StringComparison.OrdinalIgnoreCase)) return "auto";
                if (text.Contains("\"zh-CN\"", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
                if (text.Contains("\"en\"", StringComparison.OrdinalIgnoreCase)) return "en";
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return "auto";
    }

    private static string LoadThemePreference()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var text = File.ReadAllText(SettingsPath);
                if (text.Contains("\"Theme\":\"dark\"", StringComparison.OrdinalIgnoreCase)) return "dark";
                if (text.Contains("\"Theme\":\"light\"", StringComparison.OrdinalIgnoreCase)) return "light";
                if (text.Contains("\"Theme\":\"auto\"", StringComparison.OrdinalIgnoreCase)) return "auto";
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return "auto";
    }
}
