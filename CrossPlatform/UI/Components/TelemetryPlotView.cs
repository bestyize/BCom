using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Shapes;

namespace BCom.CrossPlatform;

/// <summary>
/// Lightweight Avalonia vector plot. Samples are retained in TelemetryBuffer,
/// not in visual controls; at most eight paths are created per redraw.
/// </summary>
public sealed class TelemetryPlotView
{
    private readonly Canvas _canvas = new() { Height = 154, ClipToBounds = true };
    private readonly Button _pauseButton = new() { MinWidth = 66, Height = 26 };
    private readonly ComboBox _range = new() { Width = 78, Height = 28 };
    private bool _paused;
    private int _windowSize = 512;
    private readonly TextBlock _label = new()
    {
        FontSize = 11,
        Foreground = MacTheme.TextMuted,
        Text = "CSV: value1,value2,..."
    };
    private IReadOnlyList<double[]> _samples = Array.Empty<double[]>();
    private static readonly IBrush[] Strokes =
    {
        Brushes.DodgerBlue, Brushes.OrangeRed, Brushes.MediumSeaGreen,
        Brushes.DarkOrchid, Brushes.Goldenrod, Brushes.DeepPink,
        Brushes.Turquoise, Brushes.CadetBlue
    };

    public Control Control { get; }

    public TelemetryPlotView()
    {
        var layout = new DockPanel { LastChildFill = true };
        var toolbar = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 6 };
        toolbar.Children.Add(_label);
        _pauseButton.Click += (_, _) =>
        {
            _paused = !_paused;
            RefreshLanguage();
            if (!_paused) Draw();
        };
        Grid.SetColumn(_pauseButton, 1);
        toolbar.Children.Add(_pauseButton);
        _range.ItemsSource = new[] { "128", "256", "512" };
        _range.SelectedIndex = 2;
        _range.SelectionChanged += (_, _) =>
        {
            if (int.TryParse(_range.SelectedItem as string, out int count))
                _windowSize = count;
            Draw();
        };
        Grid.SetColumn(_range, 2);
        toolbar.Children.Add(_range);
        DockPanel.SetDock(toolbar, Dock.Top);
        layout.Children.Add(toolbar);
        layout.Children.Add(_canvas);
        Control = new Border
        {
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(8),
            Background = MacTheme.BgEditor,
            Child = layout
        };
        _canvas.SizeChanged += (_, _) => Draw();
        RefreshLanguage();
    }

    public void RefreshLanguage()
    {
        _pauseButton.Content = I18n.T(_paused ? "PlotResume" : "PlotPause");
    }

    public void Resume()
    {
        _paused = false;
        RefreshLanguage();
    }

    public void Update(IReadOnlyList<double[]> samples)
    {
        if (_paused) return;
        _samples = samples;
        Draw();
    }

    private void Draw()
    {
        _canvas.Children.Clear();
        double width = Math.Max(0, _canvas.Bounds.Width - 8);
        double height = _canvas.Height - 10;
        if (width < 10 || _samples.Count < 2) return;
        var visible = _samples.Skip(Math.Max(0, _samples.Count - _windowSize)).ToArray();
        int channels = 0;
        foreach (var row in visible) channels = Math.Max(channels, row.Length);
        for (int ch = 0; ch < Math.Min(8, channels); ch++)
        {
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            foreach (var row in visible)
            {
                if (ch >= row.Length) continue;
                min = Math.Min(min, row[ch]);
                max = Math.Max(max, row[ch]);
            }
            if (!double.IsFinite(min)) continue;
            double range = Math.Max(1e-9, max - min);
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                bool started = false;
                for (int i = 0; i < visible.Length; i++)
                {
                    if (ch >= visible[i].Length)
                    {
                        if (started) ctx.EndFigure(false);
                        started = false;
                        continue;
                    }
                    double x = 4 + width * i / Math.Max(1, visible.Length - 1);
                    double y = 5 + height * (1 - (visible[i][ch] - min) / range);
                    var point = new Point(x, y);
                    if (!started)
                    {
                        ctx.BeginFigure(point, false);
                        started = true;
                    }
                    else ctx.LineTo(point);
                }
                if (started) ctx.EndFigure(false);
            }
            _canvas.Children.Add(new Avalonia.Controls.Shapes.Path
            {
                Data = geometry,
                Stroke = Strokes[ch],
                StrokeThickness = 1.7
            });
        }
        _label.Text = I18n.Format("PlotSamples", visible.Length, Math.Min(channels, 8));
    }
}
