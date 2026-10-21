using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace BCom.CrossPlatform;

/// <summary>
/// Centralized macOS design tokens, color palette, and reusable control factories.
/// </summary>
public static class MacTheme
{
    // Typography
    public static readonly FontFamily MonoFont = new("Menlo, Monaco, SF Mono, Consolas, Courier New, monospace");

    // Backgrounds
    public static readonly SolidColorBrush BgWindow = new(Color.Parse("#F2F4F7"));
    public static readonly SolidColorBrush BgCard = new(Color.Parse("#FFFFFF"));
    public static readonly SolidColorBrush BgSubtle = new(Color.Parse("#F8FAFC"));
    public static readonly SolidColorBrush BgEditor = new(Color.Parse("#FAFBFD"));

    // Borders
    public static readonly SolidColorBrush BorderMuted = new(Color.Parse("#E2E8F0"));
    public static readonly SolidColorBrush BorderStrong = new(Color.Parse("#CBD5E1"));

    // Text colors
    public static readonly SolidColorBrush TextPrimary = new(Color.Parse("#0F172A"));
    public static readonly SolidColorBrush TextSecondary = new(Color.Parse("#475569"));
    public static readonly SolidColorBrush TextMuted = new(Color.Parse("#94A3B8"));

    // Accent colors (Apple HIG)
    public static readonly SolidColorBrush AccentBlue = new(Color.Parse("#007AFF"));
    public static readonly SolidColorBrush AccentGreen = new(Color.Parse("#34C759"));
    public static readonly SolidColorBrush AccentRed = new(Color.Parse("#FF3B30"));
    public static readonly SolidColorBrush StatusGray = new(Color.Parse("#94A3B8"));

    /// <summary>
    /// Creates a styled macOS-like card container with subtle border and rounded corners.
    /// </summary>
    public static Border CreateCard(Control child, Thickness? padding = null) =>
        new()
        {
            Background = BgCard,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = padding ?? new Thickness(14),
            Child = child
        };

    /// <summary>
    /// Creates a secondary action button with subtle borders and macOS styling.
    /// </summary>
    public static Button CreateSecondaryButton(string text, Action onClick)
    {
        var button = new Button
        {
            Content = text,
            Background = BgSubtle,
            Foreground = TextSecondary,
            BorderBrush = BorderMuted,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 4),
            FontSize = 12,
            FontWeight = FontWeight.Medium,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        button.Click += (_, _) => onClick();
        return button;
    }

    /// <summary>
    /// Creates an uppercase field section label.
    /// </summary>
    public static TextBlock CreateFormLabel(string text) =>
        new()
        {
            Text = text.ToUpperInvariant(),
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextMuted,
            Margin = new Thickness(0, 8, 0, 3)
        };

    /// <summary>
    /// Creates a pill badge container with customized background and border.
    /// </summary>
    public static Border CreateBadge(Control content, string bgHex, string borderHex) =>
        new()
        {
            Background = new SolidColorBrush(Color.Parse(bgHex)),
            BorderBrush = new SolidColorBrush(Color.Parse(borderHex)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(7, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = content
        };
}
