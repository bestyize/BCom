using System;
using System.IO.Ports;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace BCom.CrossPlatform;

/// <summary>
/// Serial configuration inspector panel for selecting port, baud rates, and hardware signals with day/night adaptation.
/// </summary>
public sealed class SettingsPanel
{
    private readonly ComboBox _ports = new();
    private readonly ComboBox _bauds = new();
    private readonly ComboBox _dataBits = new();
    private readonly ComboBox _stopBits = new();
    private readonly ComboBox _parities = new();

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
        Background = MacTheme.AccentBlue,
        Cursor = new Cursor(StandardCursorType.Hand),
        Margin = new Thickness(0, 12, 0, 8)
    };

    private readonly CheckBox _rts = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _dtr = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _periodic = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };

    private readonly TextBox _interval = new()
    {
        Text = "1000",
        Width = 75,
        Height = 28,
        FontFamily = MacTheme.MonoFont,
        FontSize = 12,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(6, 3),
        VerticalAlignment = VerticalAlignment.Center
    };

    private Border? _signalsCard;
    private Border? _periodicCard;
    private TextBlock? _settingsTitle;
    private TextBlock? _portLabel;
    private TextBlock? _baudLabel;
    private TextBlock? _dataLabel;
    private TextBlock? _stopLabel;
    private TextBlock? _parityLabel;
    private TextBlock? _signalsTitle;
    private TextBlock? _msLabel;
    private Button? _refreshBtn;
    private bool _isOpen;

    public event Action? TogglePortRequested;
    public event Action? RefreshPortsRequested;
    public event Action<bool, bool>? SignalsChanged;
    public event Action<bool, int>? PeriodicChanged;

    public Border Container { get; }

    public string? SelectedPort => _ports.SelectedItem as string;

    public SettingsPanel()
    {
        _bauds.ItemsSource = SerialConfig.BaudRates.Select(x => x.ToString()).ToArray();
        _dataBits.ItemsSource = SerialConfig.DataBitsOptions.Select(x => x.ToString()).ToArray();
        _stopBits.ItemsSource = SerialConfig.StopBitsOptions.Select(x => x.ToString()).ToArray();
        _parities.ItemsSource = SerialConfig.ParityOptions.Select(x => x.ToString()).ToArray();

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

        _openButton.Click += (_, _) => TogglePortRequested?.Invoke();
        _rts.IsCheckedChanged += (_, _) => OnSignalsChanged();
        _dtr.IsCheckedChanged += (_, _) => OnSignalsChanged();
        _periodic.IsCheckedChanged += (_, _) => OnPeriodicChanged();
        _interval.TextChanged += (_, _) => OnPeriodicChanged();

        Container = BuildView();
        ApplyTheme();
        RefreshLanguage();
    }

    public SerialConfig GetConfig()
    {
        return new SerialConfig
        {
            PortName = _ports.SelectedItem as string ?? string.Empty,
            BaudRate = SerialConfig.BaudRates[Math.Max(0, _bauds.SelectedIndex)],
            DataBits = SerialConfig.DataBitsOptions[Math.Max(0, _dataBits.SelectedIndex)],
            StopBits = SerialConfig.StopBitsOptions[Math.Max(0, _stopBits.SelectedIndex)],
            Parity = SerialConfig.ParityOptions[Math.Max(0, _parities.SelectedIndex)],
            RtsEnable = _rts.IsChecked == true,
            DtrEnable = _dtr.IsChecked == true
        };
    }

    public void SetAvailablePorts(string[] ports)
    {
        var current = _ports.SelectedItem as string;
        if ((_ports.ItemsSource as string[])?.SequenceEqual(ports) == true) return;
        _ports.ItemsSource = ports;
        _ports.SelectedItem = ports.Contains(current) ? current : ports.FirstOrDefault();
    }

    public void SetConnectionState(bool isOpen)
    {
        _isOpen = isOpen;
        _openButton.Content = I18n.T(_isOpen ? "ClosePort" : "OpenPort");
        _openButton.Background = _isOpen ? MacTheme.AccentRed : MacTheme.AccentBlue;
    }

    public void ResetPeriodic()
    {
        _periodic.IsChecked = false;
    }

    public void RefreshLanguage()
    {
        if (_settingsTitle != null) _settingsTitle.Text = I18n.T("SerialSettings");
        if (_refreshBtn != null) _refreshBtn.Content = I18n.T("Refresh");
        if (_portLabel != null) _portLabel.Text = I18n.T("Port").ToUpperInvariant();
        if (_baudLabel != null) _baudLabel.Text = I18n.T("Baud").ToUpperInvariant();
        if (_dataLabel != null) _dataLabel.Text = I18n.T("DataBits").ToUpperInvariant();
        if (_stopLabel != null) _stopLabel.Text = I18n.T("StopBits").ToUpperInvariant();
        if (_parityLabel != null) _parityLabel.Text = I18n.T("Parity").ToUpperInvariant();
        if (_signalsTitle != null) _signalsTitle.Text = I18n.T("Signals").ToUpperInvariant();

        _openButton.Content = I18n.T(_isOpen ? "ClosePort" : "OpenPort");
        _rts.Content = I18n.T("Rts");
        _dtr.Content = I18n.T("Dtr");
        _periodic.Content = I18n.T("AutoRepeat");
    }

    public void ApplyTheme()
    {
        Container.Background = MacTheme.BgCard;
        Container.BorderBrush = MacTheme.BorderMuted;

        if (_settingsTitle != null) _settingsTitle.Foreground = MacTheme.TextPrimary;
        if (_portLabel != null) _portLabel.Foreground = MacTheme.TextMuted;
        if (_baudLabel != null) _baudLabel.Foreground = MacTheme.TextMuted;
        if (_dataLabel != null) _dataLabel.Foreground = MacTheme.TextMuted;
        if (_stopLabel != null) _stopLabel.Foreground = MacTheme.TextMuted;
        if (_parityLabel != null) _parityLabel.Foreground = MacTheme.TextMuted;
        if (_signalsTitle != null) _signalsTitle.Foreground = MacTheme.TextMuted;
        if (_msLabel != null) _msLabel.Foreground = MacTheme.TextSecondary;

        if (_refreshBtn != null)
        {
            _refreshBtn.Background = MacTheme.BgSubtle;
            _refreshBtn.Foreground = MacTheme.TextSecondary;
            _refreshBtn.BorderBrush = MacTheme.BorderMuted;
        }

        if (_signalsCard != null)
        {
            _signalsCard.Background = MacTheme.BgSubtle;
            _signalsCard.BorderBrush = MacTheme.BorderMuted;
        }

        if (_periodicCard != null)
        {
            _periodicCard.Background = MacTheme.BgSubtle;
            _periodicCard.BorderBrush = MacTheme.BorderMuted;
        }

        _interval.Background = MacTheme.IsDark ? new SolidColorBrush(Color.Parse("#1C1C20")) : Brushes.White;
        _interval.Foreground = MacTheme.TextPrimary;
        _interval.BorderBrush = MacTheme.BorderMuted;

        _openButton.Background = _isOpen ? MacTheme.AccentRed : MacTheme.AccentBlue;
    }

    private void OnSignalsChanged()
    {
        SignalsChanged?.Invoke(_rts.IsChecked == true, _dtr.IsChecked == true);
    }

    private void OnPeriodicChanged()
    {
        bool enabled = _periodic.IsChecked == true;
        int ms = int.TryParse(_interval.Text, out var val) ? val : 0;
        PeriodicChanged?.Invoke(enabled, ms);
    }

    private Border BuildView()
    {
        var stack = new StackPanel { Spacing = 4 };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };

        _settingsTitle = new TextBlock
        {
            Text = I18n.T("SerialSettings"),
            FontSize = 13.5,
            FontWeight = FontWeight.Bold,
            Foreground = MacTheme.TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };
        header.Children.Add(_settingsTitle);

        _refreshBtn = MacTheme.CreateSecondaryButton(I18n.T("Refresh"), () => RefreshPortsRequested?.Invoke());
        Grid.SetColumn(_refreshBtn, 1);
        header.Children.Add(_refreshBtn);
        stack.Children.Add(header);

        _portLabel = MacTheme.CreateFormLabel(I18n.T("Port"));
        stack.Children.Add(_portLabel);
        stack.Children.Add(_ports);

        _baudLabel = MacTheme.CreateFormLabel(I18n.T("Baud"));
        stack.Children.Add(_baudLabel);
        stack.Children.Add(_bauds);

        var bitsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,8,*"),
            Margin = new Thickness(0, 0, 0, 2)
        };
        _dataLabel = MacTheme.CreateFormLabel(I18n.T("DataBits"));
        _stopLabel = MacTheme.CreateFormLabel(I18n.T("StopBits"));
        var dataStack = new StackPanel { Children = { _dataLabel, _dataBits } };
        var stopStack = new StackPanel { Children = { _stopLabel, _stopBits } };
        Grid.SetColumn(stopStack, 2);
        bitsGrid.Children.Add(dataStack);
        bitsGrid.Children.Add(stopStack);
        stack.Children.Add(bitsGrid);

        _parityLabel = MacTheme.CreateFormLabel(I18n.T("Parity"));
        stack.Children.Add(_parityLabel);
        stack.Children.Add(_parities);

        stack.Children.Add(_openButton);

        _signalsTitle = new TextBlock
        {
            Text = I18n.T("Signals").ToUpperInvariant(),
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = MacTheme.TextMuted
        };

        _signalsCard = new Border
        {
            Background = MacTheme.BgSubtle,
            BorderBrush = MacTheme.BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 6),
            Margin = new Thickness(0, 4, 0, 4),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    _signalsTitle,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 16,
                        Children = { _rts, _dtr }
                    }
                }
            }
        };
        stack.Children.Add(_signalsCard);

        _msLabel = new TextBlock
        {
            Text = "ms",
            FontSize = 12,
            Foreground = MacTheme.TextSecondary,
            VerticalAlignment = VerticalAlignment.Center
        };

        _periodicCard = new Border
        {
            Background = MacTheme.BgSubtle,
            BorderBrush = MacTheme.BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8),
            Margin = new Thickness(0, 4, 0, 4),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    _periodic,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children = { _interval, _msLabel }
                    }
                }
            }
        };
        stack.Children.Add(_periodicCard);

        return new Border
        {
            Child = stack,
            Background = MacTheme.BgCard,
            BorderBrush = MacTheme.BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(16),
            Width = 270
        };
    }
}
