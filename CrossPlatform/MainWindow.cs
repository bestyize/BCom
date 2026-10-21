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

/// <summary>
/// Modern macOS-inspired serial port debugging assistant for Windows, macOS and Linux.
/// </summary>
public sealed class MainWindow : Window
{
    private static readonly FontFamily MonoFont = new("Menlo, Monaco, SF Mono, Consolas, Courier New, monospace");
    private static readonly SolidColorBrush BgWindow = new(Color.Parse("#F2F4F7"));
    private static readonly SolidColorBrush BgCard = new(Color.Parse("#FFFFFF"));
    private static readonly SolidColorBrush BgSubtle = new(Color.Parse("#F8FAFC"));
    private static readonly SolidColorBrush BgEditor = new(Color.Parse("#FAFBFD"));
    private static readonly SolidColorBrush BorderMuted = new(Color.Parse("#E2E8F0"));
    private static readonly SolidColorBrush BorderStrong = new(Color.Parse("#CBD5E1"));

    private static readonly SolidColorBrush TextPrimary = new(Color.Parse("#0F172A"));
    private static readonly SolidColorBrush TextSecondary = new(Color.Parse("#475569"));
    private static readonly SolidColorBrush TextMuted = new(Color.Parse("#94A3B8"));

    private static readonly SolidColorBrush AccentBlue = new(Color.Parse("#007AFF"));
    private static readonly SolidColorBrush AccentGreen = new(Color.Parse("#34C759"));
    private static readonly SolidColorBrush AccentRed = new(Color.Parse("#FF3B30"));
    private static readonly SolidColorBrush StatusGray = new(Color.Parse("#94A3B8"));

    private readonly List<Action> _translate = new();
    private readonly List<byte> _received = new();
    private readonly ComboBox _ports = new();
    private readonly ComboBox _bauds = new();
    private readonly ComboBox _dataBits = new();
    private readonly ComboBox _stopBits = new();
    private readonly ComboBox _parities = new();
    private readonly ComboBox _languages = new();

    private readonly TextBox _input = new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = MonoFont,
        FontSize = 13,
        Background = Brushes.White,
        BorderBrush = BorderMuted,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(10, 8),
        MinHeight = 85
    };

    private readonly TextBox _output = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = MonoFont,
        FontSize = 13,
        Background = BgEditor,
        BorderBrush = BorderMuted,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(10, 8)
    };

    private readonly TextBox _interval = new()
    {
        Text = "1000",
        Width = 75,
        Height = 28,
        FontFamily = MonoFont,
        FontSize = 12,
        BorderBrush = BorderMuted,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(6, 3),
        VerticalAlignment = VerticalAlignment.Center
    };

    private readonly CheckBox _showHex = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _sendHex = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _newline = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _periodic = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _rts = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _dtr = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };

    private readonly Button _openButton = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        MinHeight = 40,
        CornerRadius = new CornerRadius(9),
        FontSize = 13.5,
        FontWeight = FontWeight.Bold,
        Foreground = Brushes.White,
        Background = AccentBlue,
        Cursor = new Cursor(StandardCursorType.Hand)
    };

    private readonly Border _statusDot = new()
    {
        Width = 8,
        Height = 8,
        CornerRadius = new CornerRadius(4),
        Background = StatusGray,
        VerticalAlignment = VerticalAlignment.Center
    };

    private readonly TextBlock _statusPillText = new()
    {
        FontSize = 11.5,
        FontWeight = FontWeight.SemiBold,
        Foreground = TextSecondary,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(6, 0, 0, 0)
    };

    private readonly TextBlock _status = new()
    {
        FontSize = 12,
        Foreground = TextSecondary,
        VerticalAlignment = VerticalAlignment.Center
    };

    private readonly TextBlock _counts = new() { IsVisible = false };
    private readonly TextBlock _rxCountBadge = new() { FontSize = 11.5, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#16A34A")) };
    private readonly TextBlock _txCountBadge = new() { FontSize = 11.5, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#0284C7")) };

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
        Title = "BCom · Serial Monitor";
        Width = 1120;
        Height = 740;
        MinWidth = 800;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = BgWindow;
        Content = BuildLayout();

        _bauds.ItemsSource = BaudRates.Select(x => x.ToString()).ToArray();
        _dataBits.ItemsSource = DataBitsOptions.Select(x => x.ToString()).ToArray();
        _stopBits.ItemsSource = StopBitsOptions.Select(x => x.ToString()).ToArray();
        _parities.ItemsSource = ParityOptions.Select(x => x.ToString()).ToArray();
        _bauds.SelectedIndex = 3;
        _dataBits.SelectedIndex = 0;
        _stopBits.SelectedIndex = 0;
        _parities.SelectedIndex = 0;

        _ports.HorizontalAlignment = HorizontalAlignment.Stretch;
        _bauds.HorizontalAlignment = HorizontalAlignment.Stretch;
        _dataBits.HorizontalAlignment = HorizontalAlignment.Stretch;
        _stopBits.HorizontalAlignment = HorizontalAlignment.Stretch;
        _parities.HorizontalAlignment = HorizontalAlignment.Stretch;
        _ports.CornerRadius = new CornerRadius(7);
        _bauds.CornerRadius = new CornerRadius(7);
        _dataBits.CornerRadius = new CornerRadius(7);
        _stopBits.CornerRadius = new CornerRadius(7);
        _parities.CornerRadius = new CornerRadius(7);

        _languages.ItemsSource = new[] { "System / 系统", "English", "简体中文" };
        _languages.SelectedIndex = I18n.Preference switch { "en" => 1, "zh-CN" => 2, _ => 0 };
        _languages.CornerRadius = new CornerRadius(7);
        _languages.Height = 30;
        _languages.Padding = new Thickness(8, 4);
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

    private Button SecondaryButton(string key, EventHandler<Avalonia.Interactivity.RoutedEventArgs> action)
    {
        var button = Translated(new Button
        {
            Background = new SolidColorBrush(Color.Parse("#F8FAFC")),
            Foreground = TextSecondary,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 4),
            FontSize = 12,
            FontWeight = FontWeight.Medium,
            Cursor = new Cursor(StandardCursorType.Hand)
        }, key, (c, text) => c.Content = text);
        button.Click += action;
        return button;
    }

    private MenuItem Item(string key, Action action)
    {
        var menu = Translated(new MenuItem(), key, (c, value) => c.Header = value);
        menu.Click += (_, _) => action();
        return menu;
    }

    private TextBlock FormLabel(string key) =>
        Translated(new TextBlock
        {
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextMuted,
            Margin = new Thickness(0, 8, 0, 3)
        }, key, (c, text) => c.Text = text.ToUpperInvariant());

    private Border Card(Control child, Thickness? padding = null) =>
        new()
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = padding ?? new Thickness(14),
            Child = child
        };

    private Control BuildLayout()
    {
        // 1. Top macOS-style App Navigation Bar
        var fileMenu = Translated(new MenuItem(), "File", (c, text) => c.Header = text);
        fileMenu.ItemsSource = new[]
        {
            Item("OpenFile", () => _ = OpenTextFile()),
            Item("SaveFile", () => _ = SaveReceived())
        };

        var toolsMenu = Translated(new MenuItem(), "Tools", (c, text) => c.Header = text);
        _modeMenu = Item("ModeCompact", ToggleCompact);
        toolsMenu.ItemsSource = new[]
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

        var helpMenu = Translated(new MenuItem(), "Help", (c, text) => c.Header = text);
        helpMenu.ItemsSource = new[]
        {
            Item("Source", () => _ = OpenUrl("https://github.com/bestyize/BCom")),
            Item("WeChat", () => _ = ShowWeChat()),
            Item("About", () => _ = ShowMessage("AboutBody", null, "AboutTitle"))
        };

        var menuBar = new Menu
        {
            ItemsSource = new[] { fileMenu, toolsMenu, helpMenu },
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center
        };

        var topBarGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new Thickness(0)
        };

        // Brand Icon + Title + Status Pill
        var brandStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        var logoIcon = new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(7),
            Background = AccentBlue,
            Child = new TextBlock
            {
                Text = "BC",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        brandStack.Children.Add(logoIcon);

        var titleText = Translated(new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        }, "Title", (c, text) => c.Text = text);
        brandStack.Children.Add(titleText);

        var statusPill = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F1F5F9")),
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(9, 3),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { _statusDot, _statusPillText }
            }
        };
        brandStack.Children.Add(statusPill);
        topBarGrid.Children.Add(brandStack);

        // Right side: Menu Bar + Language Selector
        var topControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        topControls.Children.Add(menuBar);
        topControls.Children.Add(_languages);
        Grid.SetColumn(topControls, 2);
        topBarGrid.Children.Add(topControls);

        var topBarCard = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(18, 9, 18, 9),
            Child = topBarGrid
        };
        DockPanel.SetDock(topBarCard, Dock.Top);

        // 2. Left Column: Receive Card & Send Card
        var leftGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12
        };

        // --- Receive Card ---
        var receiveGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            RowSpacing = 9
        };

        var receiveHeaderGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto"),
            VerticalAlignment = VerticalAlignment.Center
        };

        var receiveTitle = Translated(new TextBlock
        {
            FontSize = 13.5,
            FontWeight = FontWeight.Bold,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        }, "Receive", (c, text) => c.Text = text);
        receiveHeaderGrid.Children.Add(receiveTitle);

        var rxBadge = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F0FDF4")),
            BorderBrush = new SolidColorBrush(Color.Parse("#DCFCE7")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(7, 2),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = _rxCountBadge
        };
        Grid.SetColumn(rxBadge, 1);
        receiveHeaderGrid.Children.Add(rxBadge);

        var receiveActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        receiveActions.Children.Add(Translated(_showHex, "HexDisplay", (c, text) => c.Content = text));
        var clearReceive = SecondaryButton("ClearReceive", (_, _) => { _received.Clear(); RenderReceived(); });
        var exportReceive = SecondaryButton("Export", (_, _) => _ = SaveReceived());
        receiveActions.Children.Add(clearReceive);
        receiveActions.Children.Add(exportReceive);
        Grid.SetColumn(receiveActions, 3);
        receiveHeaderGrid.Children.Add(receiveActions);
        receiveGrid.Children.Add(receiveHeaderGrid);

        _output.PlaceholderText = I18n.T("ReceivePlaceholder");
        Grid.SetRow(_output, 1);
        receiveGrid.Children.Add(_output);
        leftGrid.Children.Add(Card(receiveGrid));

        // --- Send Card ---
        var sendGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            RowSpacing = 8
        };

        var sendHeaderGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto"),
            VerticalAlignment = VerticalAlignment.Center
        };

        var sendTitle = Translated(new TextBlock
        {
            FontSize = 13.5,
            FontWeight = FontWeight.Bold,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        }, "SendPanel", (c, text) => c.Text = text);
        sendHeaderGrid.Children.Add(sendTitle);

        var txBadge = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#EFF6FF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#DBEAFE")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(7, 2),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = _txCountBadge
        };
        Grid.SetColumn(txBadge, 1);
        sendHeaderGrid.Children.Add(txBadge);

        var sendHeaderActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        sendHeaderActions.Children.Add(Translated(_sendHex, "HexSend", (c, text) => c.Content = text));
        sendHeaderActions.Children.Add(Translated(_newline, "NewLine", (c, text) => c.Content = text));
        var importFile = SecondaryButton("Import", (_, _) => _ = OpenTextFile());
        var clearSend = SecondaryButton("ClearSend", (_, _) => _input.Text = "");
        sendHeaderActions.Children.Add(importFile);
        sendHeaderActions.Children.Add(clearSend);
        Grid.SetColumn(sendHeaderActions, 3);
        sendHeaderGrid.Children.Add(sendHeaderActions);
        sendGrid.Children.Add(sendHeaderGrid);

        _input.PlaceholderText = I18n.T("SendPlaceholder");
        Grid.SetRow(_input, 1);
        sendGrid.Children.Add(_input);

        // Send Bottom Action Bar
        var sendActionBar = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var shortcutHint = new TextBlock
        {
            Text = "HEX: 0A FF 12 · ASCII / UTF-8",
            FontSize = 11,
            Foreground = TextMuted,
            VerticalAlignment = VerticalAlignment.Center
        };
        sendActionBar.Children.Add(shortcutHint);

        var sendButton = Translated(new Button
        {
            Background = AccentBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            FontSize = 13.5,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(28, 8),
            MinHeight = 36,
            Cursor = new Cursor(StandardCursorType.Hand)
        }, "Send", (c, text) => c.Content = text);
        sendButton.Click += (_, _) => SendData();
        Grid.SetColumn(sendButton, 1);
        sendActionBar.Children.Add(sendButton);

        Grid.SetRow(sendActionBar, 2);
        sendGrid.Children.Add(sendActionBar);

        var sendCard = Card(sendGrid);
        Grid.SetRow(sendCard, 1);
        leftGrid.Children.Add(sendCard);

        // 3. Right Column: Inspector Side Panel (Serial Settings)
        var settingsStack = new StackPanel { Spacing = 4 };

        var settingsHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };
        var settingsTitle = Translated(new TextBlock
        {
            FontSize = 13.5,
            FontWeight = FontWeight.Bold,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        }, "SerialSettings", (c, text) => c.Text = text);
        settingsHeader.Children.Add(settingsTitle);

        var refreshBtn = SecondaryButton("Refresh", (_, _) => RefreshPorts());
        Grid.SetColumn(refreshBtn, 1);
        settingsHeader.Children.Add(refreshBtn);
        settingsStack.Children.Add(settingsHeader);

        // Port selector
        settingsStack.Children.Add(FormLabel("Port"));
        settingsStack.Children.Add(_ports);

        // Baud Rate
        settingsStack.Children.Add(FormLabel("Baud"));
        settingsStack.Children.Add(_bauds);

        // Data Bits & Stop Bits in 2 columns
        var bitsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,8,*"),
            Margin = new Thickness(0, 0, 0, 2)
        };
        var dataStack = new StackPanel { Children = { FormLabel("DataBits"), _dataBits } };
        var stopStack = new StackPanel { Children = { FormLabel("StopBits"), _stopBits } };
        Grid.SetColumn(stopStack, 2);
        bitsGrid.Children.Add(dataStack);
        bitsGrid.Children.Add(stopStack);
        settingsStack.Children.Add(bitsGrid);

        // Parity
        settingsStack.Children.Add(FormLabel("Parity"));
        settingsStack.Children.Add(_parities);

        // Open/Close Connection Button
        _openButton.Click += (_, _) => TogglePort();
        _openButton.Margin = new Thickness(0, 12, 0, 8);
        settingsStack.Children.Add(_openButton);

        // Flow control signals card
        var signalsCard = new Border
        {
            Background = BgSubtle,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 6),
            Margin = new Thickness(0, 4, 0, 4),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    Translated(new TextBlock { FontSize = 10.5, FontWeight = FontWeight.SemiBold, Foreground = TextMuted },
                        "Signals", (c, text) => c.Text = text.ToUpperInvariant()),
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 16,
                        Children =
                        {
                            Translated(_rts, "Rts", (c, text) => c.Content = text),
                            Translated(_dtr, "Dtr", (c, text) => c.Content = text)
                        }
                    }
                }
            }
        };
        settingsStack.Children.Add(signalsCard);

        // Periodic auto-send card
        var periodicCard = new Border
        {
            Background = BgSubtle,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8),
            Margin = new Thickness(0, 4, 0, 4),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    Translated(_periodic, "AutoRepeat", (c, text) => c.Content = text),
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            _interval,
                            Translated(new TextBlock { FontSize = 12, Foreground = TextSecondary, VerticalAlignment = VerticalAlignment.Center },
                                "BytesUnit", (c, text) => c.Text = "ms")
                        }
                    }
                }
            }
        };
        settingsStack.Children.Add(periodicCard);

        _settingsPane.Child = settingsStack;
        _settingsPane.Background = BgCard;
        _settingsPane.BorderBrush = BorderMuted;
        _settingsPane.BorderThickness = new Thickness(1);
        _settingsPane.CornerRadius = new CornerRadius(11);
        _settingsPane.Padding = new Thickness(16);
        _settingsPane.Width = 270;

        // Main Center Grid
        var mainGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 14,
            Margin = new Thickness(14, 12, 14, 12)
        };
        mainGrid.Children.Add(leftGrid);
        Grid.SetColumn(_settingsPane, 1);
        mainGrid.Children.Add(_settingsPane);

        // 4. Bottom Status Bar (macOS Status Strip)
        var bottomStatusGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            VerticalAlignment = VerticalAlignment.Center
        };

        var statusLeft = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        statusLeft.Children.Add(_status);
        bottomStatusGrid.Children.Add(statusLeft);

        var statusRight = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        statusRight.Children.Add(_counts);
        Grid.SetColumn(statusRight, 1);
        bottomStatusGrid.Children.Add(statusRight);

        var bottomBar = new Border
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 6, 16, 6),
            Child = bottomStatusGrid
        };
        DockPanel.SetDock(bottomBar, Dock.Bottom);

        var root = new DockPanel();
        root.Children.Add(topBarCard);
        root.Children.Add(bottomBar);
        root.Children.Add(mainGrid);
        return root;
    }

    private void RefreshLanguage()
    {
        foreach (var update in _translate) update();
        _output.PlaceholderText = I18n.T("ReceivePlaceholder");
        _input.PlaceholderText = I18n.T("SendPlaceholder");
        UpdateConnectionUI();
        _status.Text = I18n.Format(_statusKey, _statusArguments);
        if (_modeMenu != null)
            _modeMenu.Header = I18n.T(_compact ? "ModeAdvanced" : "ModeCompact");
    }

    private void UpdateConnectionUI()
    {
        bool isOpen = _port?.IsOpen == true;
        _openButton.Content = I18n.T(isOpen ? "ClosePort" : "OpenPort");
        _openButton.Background = isOpen ? AccentRed : AccentBlue;
        _statusDot.Background = isOpen ? AccentGreen : StatusGray;
        _statusPillText.Text = I18n.T(isOpen ? "ConnectedState" : "DisconnectedState");
        _statusPillText.Foreground = isOpen ? AccentGreen : TextSecondary;
    }

    private void SetStatus(string key, params object[] args)
    {
        _statusKey = key;
        _statusArguments = args;
        _status.Text = I18n.Format(key, args);
    }

    private void UpdateCounts()
    {
        _rxCountBadge.Text = $"{I18n.T("RxLabel")}: {_received.Count} {I18n.T("BytesUnit")}";
        _txCountBadge.Text = $"{I18n.T("TxLabel")}: {_sent} {I18n.T("BytesUnit")}";
        _counts.Text = $"R: {_received.Count}   S: {_sent}";
    }

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
            UpdateConnectionUI();
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
        UpdateConnectionUI();
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
        Width = _compact ? 820 : 1120;
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
        var image = new Image { Source = bitmap, Height = 340, Stretch = Stretch.Uniform };
        var window = new Window
        {
            Title = I18n.T("QrTitle"),
            Width = 430,
            Height = 465,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = BgCard,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = I18n.T("QrSubtitle"),
                        FontSize = 13,
                        FontWeight = FontWeight.Medium,
                        Foreground = TextSecondary,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new Border
                    {
                        BorderBrush = BorderMuted,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(10),
                        Background = Brushes.White,
                        Child = image
                    }
                }
            }
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
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = BgCard
        };
        var close = new Button
        {
            Content = I18n.T("Ok"),
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 85,
            Background = AccentBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold,
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(16, 6),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        close.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 20,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                    Foreground = TextPrimary
                },
                close
            }
        };
        await dialog.ShowDialog(this);
    }
}
