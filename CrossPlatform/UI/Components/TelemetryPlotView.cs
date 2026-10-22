using System;
using System.Collections.Generic;
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
        DockPanel.SetDock(_label, Dock.Top);
        layout.Children.Add(_label);
        layout.Children.Add(_canvas);
        Control = new Border
        {
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(8),
            Background = MacTheme.BgEditor,
            Child = layout
        };
        _canvas.SizeChanged += (_, _) => Draw();
    }

    public void Update(IReadOnlyList<double[]> samples)
    {
        _samples = samples;
        Draw();
    }

    private void Draw()
    {
        _canvas.Children.Clear();
        double width = Math.Max(0, _canvas.Bounds.Width - 8);
        double height = _canvas.Height - 10;
        if (width < 10 || _samples.Count < 2) return;

        int channels = 0;
        foreach (var row in _samples) channels = Math.Max(channels, row.Length);
        for (int ch = 0; ch < Math.Min(8, channels); ch++)
        {
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            foreach (var row in _samples)
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
                for (int i = 0; i < _samples.Count; i++)
                {
                    if (ch >= _samples[i].Length)
                    {
                        started = false;
                        continue;
                    }
                    double x = 4 + width * i / Math.Max(1, _samples.Count - 1);
                    double y = 5 + height * (1 - (_samples[i][ch] - min) / range);
                    var point = new Point(x, y);
                    if (!started)
                    {
                        ctx.BeginFigure(point, false);
                        started = true;
                    }
                    else ctx.LineTo(point);
                }
            }
            _canvas.Children.Add(new Path
            {
                Data = geometry,
                Stroke = Strokes[ch],
                StrokeThickness = 1.7
            });
        }
        _label.Text = I18n.Format("PlotSamples", _samples.Count, Math.Min(channels, 8));
    }
}
