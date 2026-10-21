using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace BCom.CrossPlatform;

/// <summary>
/// macOS-style top navigation bar component with theme and language selectors.
/// </summary>
public sealed class TopNavBar
{
    private readonly Border _statusDot = new()
    {
        Width = 8,
        Height = 8,
        CornerRadius = new CornerRadius(4),
        Background = MacTheme.StatusGray,
        VerticalAlignment = VerticalAlignment.Center
    };

    private readonly TextBlock _statusPillText = new()
    {
        FontSize = 11.5,
        FontWeight = FontWeight.SemiBold,
        Foreground = MacTheme.TextSecondary,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(6, 0, 0, 0)
    };

    private readonly ComboBox _languages = new();
    private readonly ComboBox _themes = new();
    private Border? _barBorder;
    private Border? _statusPill;
    private MenuItem? _fileMenu;
    private MenuItem? _toolsMenu;
    private MenuItem? _helpMenu;
    private MenuItem? _modeMenu;
    private TextBlock? _titleText;
    private bool _compact;
    private bool _suppressThemeEvent;

    public event Action? OpenFileRequested;
    public event Action? SaveReceivedRequested;
    public event Action? ToggleCompactRequested;
    public event Action<string>? LaunchUtilityRequested;
    public event Action? ShowWeChatRequested;
    public event Action? ShowAboutRequested;
    public event Action<string>? LanguageChanged;
    public event Action<string>? ThemeChanged;

    public Control Control { get; }

    public TopNavBar()
    {
        _languages.ItemsSource = new[] { "System / 系统", "English", "简体中文" };
        _languages.SelectedIndex = I18n.Preference switch { "en" => 1, "zh-CN" => 2, _ => 0 };
        _languages.CornerRadius = new CornerRadius(7);
        _languages.Height = 30;
        _languages.Padding = new Thickness(8, 4);
        _languages.SelectionChanged += (_, _) =>
        {
            var preference = _languages.SelectedIndex switch { 1 => "en", 2 => "zh-CN", _ => "auto" };
            LanguageChanged?.Invoke(preference);
        };

        _themes.ItemsSource = new[] { I18n.T("ThemeSystem"), I18n.T("ThemeLight"), I18n.T("ThemeDark") };
        _themes.SelectedIndex = I18n.ThemePreference switch { "light" => 1, "dark" => 2, _ => 0 };
        _themes.CornerRadius = new CornerRadius(7);
        _themes.Height = 30;
        _themes.Padding = new Thickness(8, 4);
        _themes.SelectionChanged += (_, _) =>
        {
            if (_suppressThemeEvent) return;
            var theme = _themes.SelectedIndex switch { 1 => "light", 2 => "dark", _ => "auto" };
            ThemeChanged?.Invoke(theme);
        };

        Control = BuildView();
        ApplyTheme();
        RefreshLanguage();
    }

    public void SetConnectionStatus(bool isConnected, string portName)
    {
        _statusDot.Background = isConnected ? MacTheme.AccentGreen : MacTheme.StatusGray;
        _statusPillText.Text = isConnected
            ? $"{I18n.T("ConnectedState")} {portName}".Trim()
            : I18n.T("DisconnectedState");
        _statusPillText.Foreground = isConnected ? MacTheme.AccentGreen : MacTheme.TextSecondary;
    }

    public void SetCompactMode(bool compact)
    {
        _compact = compact;
        if (_modeMenu != null)
        {
            _modeMenu.Header = I18n.T(_compact ? "ModeAdvanced" : "ModeCompact");
        }
    }

    public void RefreshLanguage()
    {
        if (_titleText != null) _titleText.Text = I18n.T("Title");
        if (_fileMenu != null) _fileMenu.Header = I18n.T("File");
        if (_toolsMenu != null) _toolsMenu.Header = I18n.T("Tools");
        if (_helpMenu != null) _helpMenu.Header = I18n.T("Help");
        if (_modeMenu != null) _modeMenu.Header = I18n.T(_compact ? "ModeAdvanced" : "ModeCompact");

        _suppressThemeEvent = true;
        _themes.ItemsSource = new[] { I18n.T("ThemeSystem"), I18n.T("ThemeLight"), I18n.T("ThemeDark") };
        _themes.SelectedIndex = I18n.ThemePreference switch { "light" => 1, "dark" => 2, _ => 0 };
        _suppressThemeEvent = false;
    }

    public void ApplyTheme()
    {
        if (_barBorder != null)
        {
            _barBorder.Background = MacTheme.BgCard;
            _barBorder.BorderBrush = MacTheme.BorderMuted;
        }

        if (_statusPill != null)
        {
            _statusPill.Background = MacTheme.BgSubtle;
            _statusPill.BorderBrush = MacTheme.BorderMuted;
        }

        if (_titleText != null)
        {
            _titleText.Foreground = MacTheme.TextPrimary;
        }

        _statusPillText.Foreground = MacTheme.TextSecondary;
    }

    private Control BuildView()
    {
        _fileMenu = new MenuItem { Header = I18n.T("File") };
        var openItem = new MenuItem { Header = I18n.T("OpenFile") };
        openItem.Click += (_, _) => OpenFileRequested?.Invoke();
        var saveItem = new MenuItem { Header = I18n.T("SaveFile") };
        saveItem.Click += (_, _) => SaveReceivedRequested?.Invoke();
        _fileMenu.ItemsSource = new[] { openItem, saveItem };

        _toolsMenu = new MenuItem { Header = I18n.T("Tools") };
        _modeMenu = new MenuItem { Header = I18n.T("ModeCompact") };
        _modeMenu.Click += (_, _) => ToggleCompactRequested?.Invoke();

        _toolsMenu.ItemsSource = new[]
        {
            CreateToolItem("Calculator", () => LaunchUtilityRequested?.Invoke("calculator")),
            CreateToolItem("Notepad", () => LaunchUtilityRequested?.Invoke("editor")),
            CreateToolItem("Terminal", () => LaunchUtilityRequested?.Invoke("terminal")),
            CreateToolItem("Registry", () => LaunchUtilityRequested?.Invoke("registry")),
            CreateToolItem("Screenshot", () => LaunchUtilityRequested?.Invoke("screenshot")),
            CreateToolItem("Paint", () => LaunchUtilityRequested?.Invoke("paint")),
            CreateToolItem("MindMap", () => LaunchUtilityRequested?.Invoke("mindmap")),
            _modeMenu
        };

        _helpMenu = new MenuItem { Header = I18n.T("Help") };
        var sourceItem = CreateToolItem("Source", () => LaunchUtilityRequested?.Invoke("source"));
        var wechatItem = CreateToolItem("WeChat", () => ShowWeChatRequested?.Invoke());
        var aboutItem = CreateToolItem("About", () => ShowAboutRequested?.Invoke());
        _helpMenu.ItemsSource = new[] { sourceItem, wechatItem, aboutItem };

        var menuBar = new Menu
        {
            ItemsSource = new[] { _fileMenu, _toolsMenu, _helpMenu },
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center
        };

        var topBarGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };

        var brandStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };

        var logoIcon = new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(7),
            Background = MacTheme.AccentBlue,
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

        _titleText = new TextBlock
        {
            Text = I18n.T("Title"),
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = MacTheme.TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };
        brandStack.Children.Add(_titleText);

        _statusPill = new Border
        {
            Background = MacTheme.BgSubtle,
            BorderBrush = MacTheme.BorderMuted,
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
        brandStack.Children.Add(_statusPill);
        topBarGrid.Children.Add(brandStack);

        var topControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        topControls.Children.Add(menuBar);
        topControls.Children.Add(_themes);
        topControls.Children.Add(_languages);
        Grid.SetColumn(topControls, 2);
        topBarGrid.Children.Add(topControls);

        _barBorder = new Border
        {
            Background = MacTheme.BgCard,
            BorderBrush = MacTheme.BorderMuted,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(18, 9, 18, 9),
            Child = topBarGrid
        };
        return _barBorder;
    }

    private static MenuItem CreateToolItem(string key, Action action)
    {
        var item = new MenuItem { Header = I18n.T(key) };
        item.Click += (_, _) => action();
        return item;
    }
}
