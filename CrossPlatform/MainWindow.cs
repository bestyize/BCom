using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace BCom.CrossPlatform;

/// <summary>
/// Main application window coordinator, orchestrating decoupled UI components and serial services.
/// Supports dynamic day/night theme adaptation.
/// </summary>
public sealed class MainWindow : Window
{
    private readonly SerialService _serial = new();
    private readonly List<byte> _receivedBuffer = new();
    private readonly TopNavBar _topNav = new();
    private readonly MonitorView _monitor = new();
    private readonly SettingsPanel _settings = new();

    private readonly TextBlock _statusText = new()
    {
        FontSize = 12,
        Foreground = MacTheme.TextSecondary,
        VerticalAlignment = VerticalAlignment.Center
    };

    private Border? _bottomBar;
    private int _sentBytes;
    private string _statusKey = "StatusReady";
    private object[] _statusArguments = Array.Empty<object>();
    private bool _compact;

    public MainWindow()
    {
        Title = "BCom · Serial Monitor";
        Width = 1120;
        Height = 740;
        MinWidth = 800;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        Content = BuildLayout();
        WireEvents();

        Opened += (_, _) => _serial.StartPortScanning();
        Closed += (_, _) => _serial.Dispose();

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += (_, _) => ApplyTheme();
        }

        ApplyTheme();
        RefreshLanguage();
        SetStatus("StatusReady");
        UpdateCounts();
        LoadWindowIcon();
    }

    private void LoadWindowIcon()
    {
        try
        {
            var uri = new Uri("avares://BCom/res/icon.png");
            if (AssetLoader.Exists(uri))
            {
                using var stream = AssetLoader.Open(uri);
                Icon = new WindowIcon(stream);
            }
        }
        catch (Exception) { /* Non-fatal icon load fallback */ }
    }

    private Control BuildLayout()
    {
        var mainGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 14,
            Margin = new Thickness(14, 12, 14, 12)
        };
        mainGrid.Children.Add(_monitor.Control);
        Grid.SetColumn(_settings.Container, 1);
        mainGrid.Children.Add(_settings.Container);

        _bottomBar = new Border
        {
            Background = MacTheme.BgCard,
            BorderBrush = MacTheme.BorderMuted,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 6, 16, 6),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                VerticalAlignment = VerticalAlignment.Center,
                Children = { _statusText }
            }
        };
        DockPanel.SetDock(_bottomBar, Dock.Bottom);

        var root = new DockPanel();
        root.Children.Add(_topNav.Control);
        root.Children.Add(_bottomBar);
        root.Children.Add(mainGrid);
        return root;
    }

    private void WireEvents()
    {
        // Top Nav Events
        _topNav.OpenFileRequested += () => _ = OpenTextFileAsync();
        _topNav.SaveReceivedRequested += () => _ = SaveReceivedAsync();
        _topNav.ToggleCompactRequested += ToggleCompactMode;
        _topNav.LaunchUtilityRequested += HandleUtilityLaunch;
        _topNav.ShowWeChatRequested += () => _ = MacDialog.ShowWeChatQrAsync(this);
        _topNav.ShowAboutRequested += () => _ = MacDialog.ShowAlertAsync(this, I18n.T("AboutBody"), I18n.T("AboutTitle"));
        _topNav.LanguageChanged += OnLanguageChanged;
        _topNav.ThemeChanged += OnThemeChanged;

        // Monitor View Events
        _monitor.SendRequested += HandleSendData;
        _monitor.ClearSendRequested += () => _monitor.SendText = string.Empty;
        _monitor.ClearReceiveRequested += HandleClearReceived;
        _monitor.ImportFileRequested += () => _ = OpenTextFileAsync();
        _monitor.ExportReceiveRequested += () => _ = SaveReceivedAsync();
        _monitor.HexDisplayToggled += _ => RenderReceived();

        // Settings Panel Events
        _settings.TogglePortRequested += HandleTogglePort;
        _settings.RefreshPortsRequested += () => _serial.CheckPorts();
        _settings.SignalsChanged += (rts, dtr) => _serial.SetSignals(rts, dtr);
        _settings.PeriodicChanged += HandlePeriodicChanged;

        // Serial Service Events
        _serial.DataReceived += HandleDataReceived;
        _serial.ConnectionChanged += HandleConnectionChanged;
        _serial.ErrorOccurred += msg => SetStatusRaw(msg);
        _serial.PortsScanned += ports => _settings.SetAvailablePorts(ports);
    }

    private void ApplyTheme()
    {
        Background = MacTheme.BgWindow;

        if (_bottomBar != null)
        {
            _bottomBar.Background = MacTheme.BgCard;
            _bottomBar.BorderBrush = MacTheme.BorderMuted;
        }

        _statusText.Foreground = MacTheme.TextSecondary;

        _topNav.ApplyTheme();
        _monitor.ApplyTheme();
        _settings.ApplyTheme();
    }

    private void OnThemeChanged(string theme)
    {
        I18n.ChangeTheme(theme);
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = theme switch
            {
                "dark" => ThemeVariant.Dark,
                "light" => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };
        }
        ApplyTheme();
    }

    private void HandleTogglePort()
    {
        if (_serial.IsOpen)
        {
            _serial.Close();
            _settings.ResetPeriodic();
            SetStatus("StatusClosed");
            return;
        }

        var config = _settings.GetConfig();
        if (_serial.Open(config, out var error))
        {
            SetStatus("StatusOpen", config.PortName);
        }
        else if (error != null)
        {
            _ = MacDialog.ShowAlertAsync(this, error);
        }
    }

    private void HandleConnectionChanged(bool isOpen, string portName)
    {
        _topNav.SetConnectionStatus(isOpen, portName);
        _settings.SetConnectionState(isOpen);
    }

    private void HandleDataReceived(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            _receivedBuffer.Add(data[i]);
        }
        RenderReceived();
    }

    private void RenderReceived()
    {
        var bytes = _receivedBuffer.ToArray();
        _monitor.ReceiveText = _monitor.IsShowHex
            ? SerialDataConverter.ToHexString(bytes)
            : SerialDataConverter.ToUtf8String(bytes);
        UpdateCounts();
    }

    private void HandleSendData()
    {
        if (!_serial.IsOpen)
        {
            _ = MacDialog.ShowAlertAsync(this, I18n.T("ErrorNotOpen"));
            _settings.ResetPeriodic();
            return;
        }

        if (!SerialDataConverter.PreparePayload(
                _monitor.SendText,
                _monitor.IsSendHex,
                _monitor.IsNewLine,
                out var payload,
                out var prepError))
        {
            _ = MacDialog.ShowAlertAsync(this, prepError ?? I18n.T("ErrorHex"));
            return;
        }

        if (_serial.Write(payload, out var writeError))
        {
            _sentBytes += payload.Length;
            UpdateCounts();
        }
        else if (writeError != null)
        {
            _ = MacDialog.ShowAlertAsync(this, writeError);
        }
    }

    private void HandleClearReceived()
    {
        _receivedBuffer.Clear();
        RenderReceived();
    }

    private void HandlePeriodicChanged(bool enabled, int intervalMs)
    {
        if (enabled && intervalMs > 0)
        {
            _serial.StartPeriodicSend(intervalMs, HandleSendData);
        }
        else
        {
            _serial.StopPeriodicSend();
        }
    }

    private void ToggleCompactMode()
    {
        _compact = !_compact;
        _settings.Container.IsVisible = !_compact;
        Width = _compact ? 820 : 1120;
        _topNav.SetCompactMode(_compact);
    }

    private void OnLanguageChanged(string preference)
    {
        I18n.Change(preference);
        RefreshLanguage();
    }

    private void RefreshLanguage()
    {
        _topNav.RefreshLanguage();
        _monitor.RefreshLanguage();
        _settings.RefreshLanguage();
        _topNav.SetConnectionStatus(_serial.IsOpen, _serial.CurrentPortName);
        _statusText.Text = I18n.Format(_statusKey, _statusArguments);
    }

    private void SetStatus(string key, params object[] args)
    {
        _statusKey = key;
        _statusArguments = args;
        _statusText.Text = I18n.Format(key, args);
    }

    private void SetStatusRaw(string message)
    {
        _statusText.Text = message;
    }

    private void UpdateCounts()
    {
        _monitor.UpdateCounts(_receivedBuffer.Count, _sentBytes);
    }

    private void HandleUtilityLaunch(string kind)
    {
        if (kind is "source" or "mindmap")
        {
            var url = kind == "source"
                ? "https://github.com/bestyize/BCom"
                : "https://www.freedraw.xyz/";
            _ = SystemLauncher.OpenUrlAsync(Launcher, url);
            return;
        }

        if (!SystemLauncher.Launch(kind, out var error) && error != null)
        {
            _ = MacDialog.ShowAlertAsync(this, error);
        }
    }

    private async Task OpenTextFileAsync()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(I18n.T("TextFiles")) { Patterns = new[] { "*.txt" } },
                    FilePickerFileTypes.All
                }
            });
            if (files.Count == 0) return;
            await using var input = await files[0].OpenReadAsync();
            using var reader = new StreamReader(input);
            _monitor.SendText = await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            await MacDialog.ShowAlertAsync(this, I18n.Format("ErrorFile", ex.Message));
        }
    }

    private async Task SaveReceivedAsync()
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = "bcom-received.txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(I18n.T("TextFiles")) { Patterns = new[] { "*.txt" } }
                }
            });
            if (file == null) return;
            await using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(_monitor.ReceiveText);
            await writer.FlushAsync();
            SetStatus("Saved");
        }
        catch (Exception ex)
        {
            await MacDialog.ShowAlertAsync(this, I18n.Format("ErrorFile", ex.Message));
        }
    }
}
