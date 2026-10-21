using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace BCom.CrossPlatform;

/// <summary>
/// Data monitoring and transmission view with monospace console formatting.
/// </summary>
public sealed class MonitorView
{
    private readonly TextBox _output = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = MacTheme.MonoFont,
        FontSize = 13,
        Background = MacTheme.BgEditor,
        BorderBrush = MacTheme.BorderMuted,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(10, 8)
    };

    private readonly TextBox _input = new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = MacTheme.MonoFont,
        FontSize = 13,
        Background = Brushes.White,
        BorderBrush = MacTheme.BorderMuted,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(10, 8),
        MinHeight = 85
    };

    private readonly CheckBox _showHex = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _sendHex = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    private readonly CheckBox _newline = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };

    private readonly TextBlock _rxCountBadge = new()
    {
        FontSize = 11.5,
        FontWeight = FontWeight.SemiBold,
        Foreground = new SolidColorBrush(Color.Parse("#16A34A"))
    };

    private readonly TextBlock _txCountBadge = new()
    {
        FontSize = 11.5,
        FontWeight = FontWeight.SemiBold,
        Foreground = new SolidColorBrush(Color.Parse("#0284C7"))
    };

    private TextBlock? _receiveTitle;
    private TextBlock? _sendTitle;
    private Button? _sendButton;
    private Button? _clearReceiveBtn;
    private Button? _exportReceiveBtn;
    private Button? _importSendBtn;
    private Button? _clearSendBtn;

    public event Action? SendRequested;
    public event Action? ClearSendRequested;
    public event Action? ClearReceiveRequested;
    public event Action? ImportFileRequested;
    public event Action? ExportReceiveRequested;
    public event Action<bool>? HexDisplayToggled;

    public Control Control { get; }

    public bool IsShowHex => _showHex.IsChecked == true;
    public bool IsSendHex => _sendHex.IsChecked == true;
    public bool IsNewLine => _newline.IsChecked == true;

    public string SendText
    {
        get => _input.Text ?? string.Empty;
        set => _input.Text = value;
    }

    public string ReceiveText
    {
        get => _output.Text ?? string.Empty;
        set
        {
            _output.Text = value;
            _output.CaretIndex = value?.Length ?? 0;
        }
    }

    public MonitorView()
    {
        _showHex.IsCheckedChanged += (_, _) => HexDisplayToggled?.Invoke(IsShowHex);
        Control = BuildView();
        RefreshLanguage();
    }

    public void UpdateCounts(int rxCount, int txCount)
    {
        _rxCountBadge.Text = $"{I18n.T("RxLabel")}: {rxCount} {I18n.T("BytesUnit")}";
        _txCountBadge.Text = $"{I18n.T("TxLabel")}: {txCount} {I18n.T("BytesUnit")}";
    }

    public void RefreshLanguage()
    {
        if (_receiveTitle != null) _receiveTitle.Text = I18n.T("Receive");
        if (_sendTitle != null) _sendTitle.Text = I18n.T("SendPanel");
        if (_clearReceiveBtn != null) _clearReceiveBtn.Content = I18n.T("ClearReceive");
        if (_exportReceiveBtn != null) _exportReceiveBtn.Content = I18n.T("Export");
        if (_importSendBtn != null) _importSendBtn.Content = I18n.T("Import");
        if (_clearSendBtn != null) _clearSendBtn.Content = I18n.T("ClearSend");
        if (_sendButton != null) _sendButton.Content = I18n.T("Send");

        _showHex.Content = I18n.T("HexDisplay");
        _sendHex.Content = I18n.T("HexSend");
        _newline.Content = I18n.T("NewLine");
        _output.PlaceholderText = I18n.T("ReceivePlaceholder");
        _input.PlaceholderText = I18n.T("SendPlaceholder");
    }

    private Control BuildView()
    {
        var mainGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12
        };

        // --- 1. Receive Card ---
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

        _receiveTitle = new TextBlock
        {
            Text = I18n.T("Receive"),
            FontSize = 13.5,
            FontWeight = FontWeight.Bold,
            Foreground = MacTheme.TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };
        receiveHeaderGrid.Children.Add(_receiveTitle);

        var rxBadge = MacTheme.CreateBadge(_rxCountBadge, "#F0FDF4", "#DCFCE7");
        rxBadge.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(rxBadge, 1);
        receiveHeaderGrid.Children.Add(rxBadge);

        var receiveActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        receiveActions.Children.Add(_showHex);
        _clearReceiveBtn = MacTheme.CreateSecondaryButton(I18n.T("ClearReceive"), () => ClearReceiveRequested?.Invoke());
        _exportReceiveBtn = MacTheme.CreateSecondaryButton(I18n.T("Export"), () => ExportReceiveRequested?.Invoke());
        receiveActions.Children.Add(_clearReceiveBtn);
        receiveActions.Children.Add(_exportReceiveBtn);
        Grid.SetColumn(receiveActions, 3);
        receiveHeaderGrid.Children.Add(receiveActions);
        receiveGrid.Children.Add(receiveHeaderGrid);

        Grid.SetRow(_output, 1);
        receiveGrid.Children.Add(_output);
        mainGrid.Children.Add(MacTheme.CreateCard(receiveGrid));

        // --- 2. Send Card ---
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

        _sendTitle = new TextBlock
        {
            Text = I18n.T("SendPanel"),
            FontSize = 13.5,
            FontWeight = FontWeight.Bold,
            Foreground = MacTheme.TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };
        sendHeaderGrid.Children.Add(_sendTitle);

        var txBadge = MacTheme.CreateBadge(_txCountBadge, "#EFF6FF", "#DBEAFE");
        txBadge.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(txBadge, 1);
        sendHeaderGrid.Children.Add(txBadge);

        var sendHeaderActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        sendHeaderActions.Children.Add(_sendHex);
        sendHeaderActions.Children.Add(_newline);
        _importSendBtn = MacTheme.CreateSecondaryButton(I18n.T("Import"), () => ImportFileRequested?.Invoke());
        _clearSendBtn = MacTheme.CreateSecondaryButton(I18n.T("ClearSend"), () => ClearSendRequested?.Invoke());
        sendHeaderActions.Children.Add(_importSendBtn);
        sendHeaderActions.Children.Add(_clearSendBtn);
        Grid.SetColumn(sendHeaderActions, 3);
        sendHeaderGrid.Children.Add(sendHeaderActions);
        sendGrid.Children.Add(sendHeaderGrid);

        Grid.SetRow(_input, 1);
        sendGrid.Children.Add(_input);

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
            Foreground = MacTheme.TextMuted,
            VerticalAlignment = VerticalAlignment.Center
        };
        sendActionBar.Children.Add(shortcutHint);

        _sendButton = new Button
        {
            Content = I18n.T("Send"),
            Background = MacTheme.AccentBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            FontSize = 13.5,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(28, 8),
            MinHeight = 36,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        _sendButton.Click += (_, _) => SendRequested?.Invoke();
        Grid.SetColumn(_sendButton, 1);
        sendActionBar.Children.Add(_sendButton);

        Grid.SetRow(sendActionBar, 2);
        sendGrid.Children.Add(sendActionBar);

        var sendCard = MacTheme.CreateCard(sendGrid);
        Grid.SetRow(sendCard, 1);
        mainGrid.Children.Add(sendCard);

        return mainGrid;
    }
}
