using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace BCom.CrossPlatform;

/// <summary>The same serial monitor UI and backend on Windows, macOS and Linux.</summary>
public sealed class MainWindow : Window
{
    private readonly List<Action> _translate = new();
    private readonly List<byte> _received = new();
    private readonly ComboBox _ports = new();
    private readonly ComboBox _bauds = new();
    private readonly ComboBox _dataBits = new();
    private readonly ComboBox _stopBits = new();
    private readonly ComboBox _parities = new();
    private readonly ComboBox _languages = new();
    private readonly TextBox _input = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
    private readonly TextBox _output = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
    private readonly TextBox _interval = new() { Text = "1000", Width = 90 };
    private readonly CheckBox _showHex = new();
    private readonly CheckBox _sendHex = new();
    private readonly CheckBox _newline = new();
    private readonly CheckBox _periodic = new();
    private readonly CheckBox _rts = new();
    private readonly CheckBox _dtr = new();
    private readonly Button _openButton = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _counts = new();
    private readonly Border _settingsPane = new();
    private readonly DispatcherTimer _scanner = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _sender = new();
    private MenuItem? _modeMenu;
    private SerialPort? _port;
    private int _sent;
    private string _statusKey = "StatusReady";
    private object[] _statusArguments = Array.Empty<object>();
    private bool _compact;

    private static readonly int[] BaudRates = { 1382400, 460800, 230400, 115200, 38400, 9600 };
    private static readonly int[] DataBitsOptions = { 8, 7, 6, 5 };
    private static readonly StopBits[] StopBitsOptions = { StopBits.One, StopBits.Two, StopBits.OnePointFive };
    private static readonly Parity[] ParityOptions = { Parity.None, Parity.Odd, Parity.Even, Parity.Mark, Parity.Space };

    public MainWindow()
    {
        Title = "BCom";
        Width = 1050;
        Height = 700;
        MinWidth = 680;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse("#F4F6FA"));
        Content = BuildLayout();

        _bauds.ItemsSource = BaudRates.Select(x => x.ToString()).ToArray();
        _dataBits.ItemsSource = DataBitsOptions.Select(x => x.ToString()).ToArray();
        _stopBits.ItemsSource = StopBitsOptions.Select(x => x.ToString()).ToArray();
        _parities.ItemsSource = ParityOptions.Select(x => x.ToString()).ToArray();
        _bauds.SelectedIndex = 3;
        _dataBits.SelectedIndex = 0;
        _stopBits.SelectedIndex = 0;
        _parities.SelectedIndex = 0;
        _languages.ItemsSource = new[] { "System / 系统", "English", "简体中文" };
        _languages.SelectedIndex = I18n.Preference switch { "en" => 1, "zh-CN" => 2, _ => 0 };
        _languages.SelectionChanged += (_, _) =>
        {
            var preference = _languages.SelectedIndex switch { 1 => "en", 2 => "zh-CN", _ => "auto" };
            I18n.Change(preference);
            RefreshLanguage();
        };

        _showHex.IsCheckedChanged += (_, _) => RenderReceived();
        _periodic.IsCheckedChanged += (_, _) => UpdatePeriodic();
        _rts.IsCheckedChanged += (_, _) => ApplySignals();
        _dtr.IsCheckedChanged += (_, _) => ApplySignals();
        _scanner.Tick += (_, _) => RefreshPorts();
        _sender.Tick += (_, _) => SendData();
        Opened += (_, _) => { RefreshPorts(); _scanner.Start(); };
        Closed += (_, _) =>
        {
            _scanner.Stop();
            _sender.Stop();
            ClosePort();
        };
        RefreshLanguage();
        SetStatus("StatusReady");
        UpdateCounts();
    }

    private T Translated<T>(T control, string key, Action<T, string> apply) where T : class
    {
        _translate.Add(() => apply(control, I18n.T(key)));
        return control;
    }

    private TextBlock Label(string key, double size = 13) =>
        Translated(new TextBlock { FontSize = size, VerticalAlignment = VerticalAlignment.Center },
            key, (c, text) => c.Text = text);

    private Button ActionButton(string key, EventHandler<Avalonia.Interactivity.RoutedEventArgs> action)
    {
        var button = Translated(new Button { MinHeight = 35, Padding = new Thickness(13, 6) },
            key, (c, text) => c.Content = text);
        button.Click += action;
        return button;
    }

    private MenuItem Item(string key, Action action)
    {
        var menu = Translated(new MenuItem(), key, (c, value) => c.Header = value);
        menu.Click += (_, _) => action();
        return menu;
    }

    private Border Panel(Control child, Thickness? padding = null) =>
        new()
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(13),
            Padding = padding ?? new Thickness(16),
            Child = child
        };

    private Control BuildLayout()
    {
        var file = Translated(new MenuItem(), "File", (c, text) => c.Header = text);
        file.ItemsSource = new[]
        {
            Item("OpenFile", () => _ = OpenTextFile()),
            Item("SaveFile", () => _ = SaveReceived())
        };
        var tools = Translated(new MenuItem(), "Tools", (c, text) => c.Header = text);
        _modeMenu = Item("ModeCompact", ToggleCompact);
        tools.ItemsSource = new[]
        {
            Item("Calculator", () => LaunchUtility("calculator")),
            Item("Notepad", () => LaunchUtility("editor")),
            Item("Terminal", () => LaunchUtility("terminal")),
            Item("Registry", () => LaunchUtility("registry")),
            Item("Screenshot", () => LaunchUtility("screenshot")),
            Item("Paint", () => LaunchUtility("paint")),
            Item("MindMap", () => _ = OpenUrl("https://www.freedraw.xyz/")),
            _modeMenu
        };
        var help = Translated(new MenuItem(), "Help", (c, text) => c.Header = text);
        help.ItemsSource = new[]
        {
            Item("Source", () => _ = OpenUrl("https://github.com/bestyize/BCom")),
            Item("WeChat", () => _ = ShowWeChat()),
            Item("About", () => _ = ShowMessage("AboutBody", null, "AboutTitle"))
        };
        var menu = new Menu { ItemsSource = new[] { file, tools, help }, Background = Brushes.White };
        DockPanel.SetDock(menu, Dock.Top);

        var headerRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(2, 12, 2, 14) };
        var brand = new StackPanel { Spacing = 4 };
        brand.Children.Add(Translated(new TextBlock { FontSize = 23, FontWeight = FontWeight.Bold },
            "Title", (c, text) => c.Text = text));
        brand.Children.Add(Translated(new TextBlock { FontSize = 12, Foreground = Brushes.Gray },
            "Subtitle", (c, text) => c.Text = text));
        headerRow.Children.Add(brand);
        var languageControls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        languageControls.Children.Add(Label("Language"));
        _languages.MinWidth = 145;
        languageControls.Children.Add(_languages);
        Grid.SetColumn(languageControls, 1);
        headerRow.Children.Add(languageControls);

        var left = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto,135,Auto"), RowSpacing = 10 };
        var receiveHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        receiveHeader.Children.Add(Label("Receive", 14));
        var clearReceive = ActionButton("ClearReceive", (_, _) => { _received.Clear(); RenderReceived(); });
        Grid.SetColumn(clearReceive, 1);
        receiveHeader.Children.Add(clearReceive);
        left.Children.Add(receiveHeader);

        _output.PlaceholderText = I18n.T("ReceivePlaceholder");
        Grid.SetRow(_output, 1);
        left.Children.Add(_output);
        var receiveFlags = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        receiveFlags.Children.Add(Translated(_showHex, "HexDisplay", (c, text) => c.Content = text));
        Grid.SetRow(receiveFlags, 2);
        left.Children.Add(receiveFlags);
        var sendTitle = Label("SendPanel", 14);
        Grid.SetRow(sendTitle, 3);
        left.Children.Add(sendTitle);
        _input.PlaceholderText = I18n.T("SendPlaceholder");
        Grid.SetRow(_input, 4);
        left.Children.Add(_input);
        var sendActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 9 };
        sendActions.Children.Add(ActionButton("Send", (_, _) => SendData()));
        sendActions.Children.Add(ActionButton("ClearSend", (_, _) => _input.Text = ""));
        sendActions.Children.Add(Translated(_sendHex, "HexSend", (c, text) => c.Content = text));
        sendActions.Children.Add(Translated(_newline, "NewLine", (c, text) => c.Content = text));
        Grid.SetRow(sendActions, 5);
        left.Children.Add(sendActions);

        var right = new StackPanel { Spacing = 10 };
        right.Children.Add(Label("SerialSettings", 14));
        right.Children.Add(Label("Port"));
        var portRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 7 };
        _ports.MinWidth = 120;
        portRow.Children.Add(_ports);
        var refresh = ActionButton("Refresh", (_, _) => RefreshPorts());
        Grid.SetColumn(refresh, 1);
        portRow.Children.Add(refresh);
        right.Children.Add(portRow);
        right.Children.Add(Label("Baud"));
        right.Children.Add(_bauds);
        right.Children.Add(Label("DataBits"));
        right.Children.Add(_dataBits);
        right.Children.Add(Label("StopBits"));
        right.Children.Add(_stopBits);
        right.Children.Add(Label("Parity"));
        right.Children.Add(_parities);
        _openButton.MinHeight = 39;
        _openButton.Click += (_, _) => TogglePort();
        right.Children.Add(_openButton);
        var signals = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
        signals.Children.Add(Translated(_rts, "Rts", (c, text) => c.Content = text));
        signals.Children.Add(Translated(_dtr, "Dtr", (c, text) => c.Content = text));
        right.Children.Add(signals);
        right.Children.Add(Translated(_periodic, "Periodic", (c, text) => c.Content = text));
        right.Children.Add(_interval);

        var main = new Grid { ColumnDefinitions = new ColumnDefinitions("2.5*,1*"), ColumnSpacing = 12 };
        main.Children.Add(Panel(left));
        _settingsPane.Child = right;
        _settingsPane.Background = Brushes.White;
        _settingsPane.CornerRadius = new CornerRadius(13);
        _settingsPane.Padding = new Thickness(16);
        Grid.SetColumn(_settingsPane, 1);
        main.Children.Add(_settingsPane);

        var statusRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(4, 12, 4, 2) };
        _status.Foreground = Brushes.DimGray;
        _status.FontSize = 12;
        statusRow.Children.Add(_status);
        _counts.Foreground = Brushes.DimGray;
        _counts.FontSize = 12;
        Grid.SetColumn(_counts, 1);
        statusRow.Children.Add(_counts);

        var contentGrid = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(16) };
        contentGrid.Children.Add(headerRow);
        Grid.SetRow(main, 1);
        contentGrid.Children.Add(main);
        Grid.SetRow(statusRow, 2);
        contentGrid.Children.Add(statusRow);

        var root = new DockPanel();
        root.Children.Add(menu);
        root.Children.Add(contentGrid);
        return root;
    }

    private void RefreshLanguage()
    {
        foreach (var update in _translate) update();
        _output.PlaceholderText = I18n.T("ReceivePlaceholder");
        _input.PlaceholderText = I18n.T("SendPlaceholder");
        _openButton.Content = I18n.T(_port?.IsOpen == true ? "ClosePort" : "OpenPort");
        _status.Text = I18n.Format(_statusKey, _statusArguments);
        if (_modeMenu != null)
            _modeMenu.Header = I18n.T(_compact ? "ModeAdvanced" : "ModeCompact");
    }

    private void SetStatus(string key, params object[] args)
    {
        _statusKey = key;
        _statusArguments = args;
        _status.Text = I18n.Format(key, args);
    }
    private void UpdateCounts() => _counts.Text = $"R: {_received.Count}   S: {_sent}";

    private void RefreshPorts()
    {
        try
        {
            var available = SerialPort.GetPortNames().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            var current = _ports.SelectedItem as string;
            if ((_ports.ItemsSource as string[])?.SequenceEqual(available) == true) return;
            _ports.ItemsSource = available;
            _ports.SelectedItem = available.Contains(current) ? current : available.FirstOrDefault();
            if (available.Length == 0 && _port?.IsOpen != true) SetStatus("StatusNoPorts");
        }
        catch (Exception ex) { SetStatus("ErrorPorts", ex.Message); }
    }

    private void TogglePort()
    {
        if (_port?.IsOpen == true) { ClosePort(); return; }
        var name = _ports.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(name)) { _ = ShowMessage("ErrorNoPort"); return; }
        try
        {
            var port = new SerialPort(name)
            {
                BaudRate = BaudRates[Math.Max(0, _bauds.SelectedIndex)],
                DataBits = DataBitsOptions[Math.Max(0, _dataBits.SelectedIndex)],
                StopBits = StopBitsOptions[Math.Max(0, _stopBits.SelectedIndex)],
                Parity = ParityOptions[Math.Max(0, _parities.SelectedIndex)],
                Encoding = Encoding.UTF8,
                RtsEnable = _rts.IsChecked == true,
                DtrEnable = _dtr.IsChecked == true
            };
            port.DataReceived += ReceiveData;
            port.Open();
            _port = port;
            _openButton.Content = I18n.T("ClosePort");
            SetStatus("StatusOpen", name);
        }
        catch (Exception ex)
        {
            _ = ShowMessage("ErrorOpen", new object[] { name, ex.Message });
        }
    }

    private void ClosePort()
    {
        _sender.Stop();
        _periodic.IsChecked = false;
        var port = _port;
        _port = null;
        if (port != null)
        {
            port.DataReceived -= ReceiveData;
            try { if (port.IsOpen) port.Close(); }
            catch (Exception ex) { SetStatus("ErrorClose", ex.Message); }
            finally { port.Dispose(); }
        }
        _openButton.Content = I18n.T("OpenPort");
        SetStatus("StatusClosed");
    }

    private void ApplySignals()
    {
        try
        {
            if (_port?.IsOpen == true)
            {
                _port.RtsEnable = _rts.IsChecked == true;
                _port.DtrEnable = _dtr.IsChecked == true;
            }
        }
        catch (Exception ex) { SetStatus("ErrorOpen", _port?.PortName ?? "", ex.Message); }
    }

    private void ReceiveData(object? sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (sender is not SerialPort port || !port.IsOpen) return;
            int length = port.BytesToRead;
            if (length <= 0) return;
            byte[] data = new byte[length];
            int read = port.Read(data, 0, length);
            if (read <= 0) return;
            Dispatcher.UIThread.Post(() =>
            {
                for (var i = 0; i < read; i++) _received.Add(data[i]);
                RenderReceived();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => SetStatus("ErrorReceive", ex.Message));
        }
    }

    private void RenderReceived()
    {
        var bytes = _received.ToArray();
        _output.Text = _showHex.IsChecked == true
            ? BitConverter.ToString(bytes).Replace("-", " ")
            : Encoding.UTF8.GetString(bytes);
        UpdateCounts();
        _output.CaretIndex = _output.Text?.Length ?? 0;
    }

    private void SendData()
    {
        if (_port?.IsOpen != true)
        {
            _sender.Stop();
            _periodic.IsChecked = false;
            _ = ShowMessage("ErrorNotOpen");
            return;
        }
        try
        {
            byte[] data;
            if (_sendHex.IsChecked == true)
            {
                var normalized = new string((_input.Text ?? "").Where(c => !char.IsWhiteSpace(c) && c != ',' && c != '-').ToArray());
                if (normalized.Length % 2 != 0 || !normalized.All(Uri.IsHexDigit))
                {
                    _ = ShowMessage("ErrorHex");
                    return;
                }
                data = Convert.FromHexString(normalized);
            }
            else data = Encoding.UTF8.GetBytes(_input.Text ?? "");

            if (_newline.IsChecked == true) data = data.Concat(new byte[] { 13, 10 }).ToArray();
            _port.Write(data, 0, data.Length);
            _sent += data.Length;
            UpdateCounts();
        }
        catch (Exception ex) { _ = ShowMessage("ErrorSend", new object[] { ex.Message }); }
    }

    private void UpdatePeriodic()
    {
        _sender.Stop();
        if (_periodic.IsChecked != true) return;
        if (!int.TryParse(_interval.Text, out int ms) || ms <= 0)
        {
            _periodic.IsChecked = false;
            _ = ShowMessage("ErrorTimer");
            return;
        }
        _sender.Interval = TimeSpan.FromMilliseconds(ms);
        _sender.Start();
    }

    private void ToggleCompact()
    {
        _compact = !_compact;
        _settingsPane.IsVisible = !_compact;
        Width = _compact ? 760 : 1050;
        if (_modeMenu != null)
            _modeMenu.Header = I18n.T(_compact ? "ModeAdvanced" : "ModeCompact");
    }

    private async Task OpenTextFile()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType(I18n.T("TextFiles")) { Patterns = new[] { "*.txt" } },
                    FilePickerFileTypes.All }
            });
            if (files.Count == 0) return;
            await using var input = await files[0].OpenReadAsync();
            using var reader = new StreamReader(input);
            _input.Text = await reader.ReadToEndAsync();
        }
        catch (Exception ex) { await ShowMessage("ErrorFile", new object[] { ex.Message }); }
    }

    private async Task SaveReceived()
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = "bcom-received.txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[] { new FilePickerFileType(I18n.T("TextFiles")) { Patterns = new[] { "*.txt" } } }
            });
            if (file == null) return;
            await using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(_output.Text ?? "");
            await writer.FlushAsync();
            SetStatus("Saved");
        }
        catch (Exception ex) { await ShowMessage("ErrorFile", new object[] { ex.Message }); }
    }

    private async Task OpenUrl(string url)
    {
        try { await Launcher.LaunchUriAsync(new Uri(url)); }
        catch (Exception) { await ShowMessage("ErrorTool"); }
    }

    private void LaunchUtility(string kind)
    {
        if (kind == "registry" && !OperatingSystem.IsWindows())
        {
            _ = ShowMessage("PlatformWindowsOnly");
            return;
        }
        string[] options = OperatingSystem.IsWindows() ? kind switch
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
        if (options.Length == 0) { _ = ShowMessage("ErrorTool"); return; }
        try
        {
            var info = new ProcessStartInfo(options[0]) { UseShellExecute = false };
            foreach (var part in options.Skip(1)) info.ArgumentList.Add(part);
            Process.Start(info);
        }
        catch (Exception) { _ = ShowMessage("ErrorTool"); }
    }

    private async Task ShowWeChat()
    {
        using var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://BCom/res/weixin.png")));
        // A dedicated Bitmap is also held by the window while the dialog is visible.
        var image = new Image { Source = bitmap, Height = 340, Stretch = Stretch.Uniform };
        var window = new Window
        {
            Title = I18n.T("QrTitle"),
            Width = 430,
            Height = 465,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel { Margin = new Thickness(20), Spacing = 15, Children =
            {
                new TextBlock { Text = I18n.T("QrSubtitle"), HorizontalAlignment = HorizontalAlignment.Center },
                image
            } }
        };
        await window.ShowDialog(this);
    }

    private async Task ShowMessage(string key, object[]? args = null, string? titleKey = null)
    {
        var message = args == null ? I18n.T(key) : I18n.Format(key, args);
        var dialog = new Window
        {
            Title = I18n.T(titleKey ?? "Error"),
            Width = 440,
            Height = 195,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var close = new Button { Content = I18n.T("Ok"), HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 75 };
        close.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel { Margin = new Thickness(22), Spacing = 22, Children =
        {
            new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            close
        } };
        await dialog.ShowDialog(this);
    }
}
