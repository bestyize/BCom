using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;

namespace BCom.CrossPlatform;

/// <summary>
/// Main application window coordinator, orchestrating decoupled UI components and serial services.
/// Supports dynamic day/night theme adaptation.
/// </summary>
public sealed class MainWindow : Window
{
    private readonly SerialService _portScanner = new();
    private readonly SerialSessionManager _sessions = new();
    private readonly ComboBox _sessionPicker = new() { MinWidth = 195, MaxWidth = 310 };
    private readonly Button _newSessionButton = new() { MinHeight = 30 };
    private readonly Button _captureButton = new() { MinHeight = 30 };
    private bool _updatingPicker;
    private SerialSession? _displayedSession;
    private readonly DispatcherTimer _receiveRenderTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private bool _receiveDirty;
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

        _receiveRenderTimer.Tick += (_, _) => { if (_receiveDirty) RenderReceived(); };
        Opened += (_, _) => { _receiveRenderTimer.Start(); _portScanner.StartPortScanning(); };
        Closed += (_, _) =>
        {
            _receiveRenderTimer.Stop();
            _sessions.Dispose();
            _portScanner.Dispose();
        };

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
        var sessionHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,*"),
            ColumnSpacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        var sessionLabel = new TextBlock
        {
            Text = I18n.T("Sessions"),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            Foreground = MacTheme.TextSecondary
        };
        sessionHeader.Children.Add(sessionLabel);
        Grid.SetColumn(_sessionPicker, 1);
        sessionHeader.Children.Add(_sessionPicker);
        _newSessionButton.Content = I18n.T("NewConnection");
        Grid.SetColumn(_newSessionButton, 2);
        sessionHeader.Children.Add(_newSessionButton);
        _captureButton.Content = I18n.T("StartCapture");
        Grid.SetColumn(_captureButton, 3);
        sessionHeader.Children.Add(_captureButton);
        var monitorLayout = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            RowSpacing = 10
        };
        monitorLayout.Children.Add(sessionHeader);
        Grid.SetRow(_monitor.Control, 1);
        monitorLayout.Children.Add(_monitor.Control);
        mainGrid.Children.Add(monitorLayout);
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

        _sessions.ActiveChanged += ShowSession;
        _sessions.SessionsChanged += RefreshSessionPicker;
        _sessionPicker.SelectionChanged += (_, _) =>
        {
            if (!_updatingPicker)
                _sessions.Select(_sessionPicker.SelectedItem as SerialSession);
        };
        _newSessionButton.Click += (_, _) =>
        {
            _sessions.Select(null);
            _settings.SelectFirstUnusedPort(_sessions.Sessions.Where(s => s.IsOpen)
                .Select(s => s.Config.PortName));
            RefreshConnectionControls();
        };

        _captureButton.Click += (_, _) => _ = ToggleCaptureAsync();

        // Monitor View Events
        _monitor.SendRequested += HandleSendData;
        _monitor.ComposerChanged += () =>
        {
            if (_displayedSession != null) _monitor.CaptureSession(_displayedSession);
        };
        _monitor.ClearSendRequested += () => _monitor.SendText = string.Empty;
        _monitor.ClearReceiveRequested += HandleClearReceived;
        _monitor.ImportFileRequested += () => _ = OpenTextFileAsync();
        _monitor.ExportReceiveRequested += () => _ = SaveReceivedAsync();
        _monitor.HexDisplayToggled += _ => RenderReceived();
        _monitor.TimelineDisplayToggled += _ => RenderReceived();

        // Settings Panel Events
        _settings.TogglePortRequested += HandleTogglePort;
        _settings.RefreshPortsRequested += () => _portScanner.CheckPorts();
        _settings.PortSelectionChanged += RefreshConnectionControls;
        _settings.SignalsChanged += (rts, dtr) =>
        {
            var session = _sessions.Find(_settings.SelectedPort ?? "");
            if (session?.IsOpen == true && ReferenceEquals(session, _sessions.Active))
                session.SetSignals(rts, dtr);
        };
        _settings.PeriodicChanged += HandlePeriodicChanged;

        _portScanner.ErrorOccurred += SetStatusRaw;
        _portScanner.PortsScanned += ports =>
        {
            _settings.SetAvailablePorts(ports);
            foreach (var session in _sessions.Sessions.ToArray())
                session.Service.CheckPorts();
            RefreshConnectionControls();
        };
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
        _newSessionButton.Background = MacTheme.BgSubtle;
        _newSessionButton.Foreground = MacTheme.TextSecondary;
        _captureButton.Background = MacTheme.BgSubtle;
        _captureButton.Foreground = MacTheme.TextSecondary;
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

    private async Task ToggleCaptureAsync()
    {
        var session = _sessions.Active;
        if (session?.IsOpen != true)
        {
            await MacDialog.ShowAlertAsync(this, I18n.T("ErrorNotOpen"));
            return;
        }
        try
        {
            if (session.IsRecording)
            {
                await session.StopRecordingAsync();
                SetStatus("CaptureStopped");
            }
            else
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    SuggestedFileName = "bcom-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bcomcap",
                    DefaultExtension = "bcomcap",
                    FileTypeChoices = new[] { new FilePickerFileType("BCom Capture") { Patterns = new[] { "*.bcomcap" } } }
                });
                if (file == null) return;
                string? path = file.TryGetLocalPath();
                if (path == null)
                {
                    await MacDialog.ShowAlertAsync(this, I18n.T("CaptureLocalOnly"));
                    return;
                }
                session.StartRecording(path);
                SetStatus("CaptureStarted");
            }
            UpdateCaptureButton();
        }
        catch (Exception ex)
        {
            await MacDialog.ShowAlertAsync(this, I18n.Format("ErrorFile", ex.Message));
            UpdateCaptureButton();
        }
    }

    private void UpdateCaptureButton()
    {
        _captureButton.Content = I18n.T(_sessions.Active?.IsRecording == true
            ? "StopCapture" : "StartCapture");
        _captureButton.IsEnabled = _sessions.Active?.IsOpen == true;
    }

    private void HandleTogglePort()
    {
        SerialConfig config;
        try
        {
            config = _settings.GetConfig();
        }
        catch (ArgumentException ex)
        {
            _ = MacDialog.ShowAlertAsync(this, ex.Message);
            return;
        }

        var existing = _sessions.Find(config.PortName);
        if (existing?.IsOpen == true)
        {
            _sessions.Remove(existing);
            SetStatus("StatusClosed");
            RefreshConnectionControls();
            return;
        }

        if (_sessions.TryOpen(config, out var session, out var error) && session != null)
        {
            session.Changed += OnSessionChanged;
            session.ConnectionChanged += OnSessionConnectionChanged;
            session.ErrorOccurred += OnSessionError;
            SerialProfileStore.Save(config);
            SetStatus("StatusOpen", config.PortName);
            RefreshSessionPicker();
            RefreshConnectionControls();
        }
        else if (error != null)
        {
            _ = MacDialog.ShowAlertAsync(this, error);
        }
    }

    private void OnSessionChanged(SerialSession session)
    {
        if (ReferenceEquals(session, _sessions.Active))
            _receiveDirty = true;
    }

    private void OnSessionConnectionChanged(SerialSession session, bool connected)
    {
        if (ReferenceEquals(session, _sessions.Active))
        {
            _topNav.SetConnectionStatus(connected, session.Config.PortName);
            _settings.SetConnectionState(connected);
            if (!connected) _settings.ResetPeriodic();
        }
        RefreshSessionPicker();
        RefreshConnectionControls();
    }

    private void OnSessionError(SerialSession session, string error)
    {
        if (ReferenceEquals(session, _sessions.Active))
            SetStatusRaw(error);
    }

    private void ShowSession(SerialSession? session)
    {
        if (_displayedSession != null)
            _monitor.CaptureSession(_displayedSession);

        _displayedSession = session;
        _monitor.ApplySession(session);
        if (session != null)
            _settings.ApplySession(session.Config, session.RepeatEnabled, session.RepeatIntervalMs);
        else
            _settings.ResetPeriodic();

        _topNav.SetConnectionStatus(session?.IsOpen == true, session?.Config.PortName ?? "");
        RefreshSessionPicker();
        RefreshConnectionControls();
        UpdateCaptureButton();
        RenderReceived();
    }

    private void RefreshSessionPicker()
    {
        _updatingPicker = true;
        try
        {
            _sessionPicker.ItemsSource = _sessions.Sessions.ToArray();
            _sessionPicker.SelectedItem = _sessions.Active;
        }
        finally { _updatingPicker = false; }
    }

    private void RefreshConnectionControls()
    {
        var connected = _sessions.Find(_settings.SelectedPort ?? "")?.IsOpen == true;
        _settings.SetConnectionState(connected);
    }

    private void RenderReceived()
    {
        _receiveDirty = false;
        var session = _sessions.Active;
        var bytes = session?.Received.Snapshot() ?? Array.Empty<byte>();
        _monitor.ReceiveText = session == null
            ? string.Empty
            : _monitor.IsTimelineView
                ? SerialTraceFormatter.Format(session.Trace)
                : _monitor.IsShowHex
                    ? SerialDataConverter.ToHexString(bytes)
                    : SerialDataConverter.ToUtf8String(bytes);
        _monitor.UpdatePlot(session?.Telemetry);
        UpdateCounts();
    }

    private async void HandleSendData()
    {
        var session = _sessions.Active;
        if (session?.IsOpen != true)
        {
            _ = MacDialog.ShowAlertAsync(this, I18n.T("ErrorNotOpen"));
            return;
        }

        _monitor.CaptureSession(session);
        var (success, error) = await session.SendAsync();
        if (!success && error != null && ReferenceEquals(session, _sessions.Active))
            _ = MacDialog.ShowAlertAsync(this, error);
        if (success && ReferenceEquals(session, _sessions.Active))
            UpdateCounts();
    }

    private void HandleClearReceived()
    {
        _sessions.Active?.ClearReceived();
        RenderReceived();
    }

    private void HandlePeriodicChanged(bool enabled, int intervalMs)
    {
        var session = _sessions.Active;
        if (session == null || !ReferenceEquals(session, _sessions.Find(_settings.SelectedPort ?? "")))
            return;
        _monitor.CaptureSession(session);
        session.ConfigureRepeat(enabled, intervalMs);
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
        _newSessionButton.Content = I18n.T("NewConnection");
        UpdateCaptureButton();
        _topNav.SetConnectionStatus(_sessions.Active?.IsOpen == true,
            _sessions.Active?.Config.PortName ?? "");
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
        var session = _sessions.Active;
        _monitor.UpdateCounts(session?.Received.TotalBytes ?? 0,
            session?.SentBytes ?? 0, session?.Received.Count ?? 0);
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
