using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace BCom.CrossPlatform;

/// <summary>
/// Centralized macOS design tokens, dynamic day/night color palette, and reusable control factories.
/// </summary>
public static class MacTheme
{
    // Typography
    public static readonly FontFamily MonoFont = new("Menlo, Monaco, SF Mono, Consolas, Courier New, monospace");

    // Day / Light Mode Palette
    public static readonly SolidColorBrush BgWindowLight = new(Color.Parse("#F2F4F7"));
    public static readonly SolidColorBrush BgCardLight = new(Color.Parse("#FFFFFF"));
    public static readonly SolidColorBrush BgSubtleLight = new(Color.Parse("#F8FAFC"));
    public static readonly SolidColorBrush BgEditorLight = new(Color.Parse("#FAFBFD"));

    public static readonly SolidColorBrush BorderMutedLight = new(Color.Parse("#E2E8F0"));
    public static readonly SolidColorBrush BorderStrongLight = new(Color.Parse("#CBD5E1"));

    public static readonly SolidColorBrush TextPrimaryLight = new(Color.Parse("#0F172A"));
    public static readonly SolidColorBrush TextSecondaryLight = new(Color.Parse("#475569"));
    public static readonly SolidColorBrush TextMutedLight = new(Color.Parse("#94A3B8"));

    public static readonly SolidColorBrush RxBgLight = new(Color.Parse("#F0FDF4"));
    public static readonly SolidColorBrush RxBorderLight = new(Color.Parse("#DCFCE7"));
    public static readonly SolidColorBrush RxTextLight = new(Color.Parse("#16A34A"));

    public static readonly SolidColorBrush TxBgLight = new(Color.Parse("#EFF6FF"));
    public static readonly SolidColorBrush TxBorderLight = new(Color.Parse("#DBEAFE"));
    public static readonly SolidColorBrush TxTextLight = new(Color.Parse("#0284C7"));

    // Night / Dark Mode Palette
    public static readonly SolidColorBrush BgWindowDark = new(Color.Parse("#18181B"));
    public static readonly SolidColorBrush BgCardDark = new(Color.Parse("#242428"));
    public static readonly SolidColorBrush BgSubtleDark = new(Color.Parse("#1E1E22"));
    public static readonly SolidColorBrush BgEditorDark = new(Color.Parse("#141416"));

    public static readonly SolidColorBrush BorderMutedDark = new(Color.Parse("#38383E"));
    public static readonly SolidColorBrush BorderStrongDark = new(Color.Parse("#4A4A52"));

    public static readonly SolidColorBrush TextPrimaryDark = new(Color.Parse("#F4F4F6"));
    public static readonly SolidColorBrush TextSecondaryDark = new(Color.Parse("#A1A1AA"));
    public static readonly SolidColorBrush TextMutedDark = new(Color.Parse("#71717A"));

    public static readonly SolidColorBrush RxBgDark = new(Color.Parse("#052E16"));
    public static readonly SolidColorBrush RxBorderDark = new(Color.Parse("#14532D"));
    public static readonly SolidColorBrush RxTextDark = new(Color.Parse("#4ADE80"));

    public static readonly SolidColorBrush TxBgDark = new(Color.Parse("#082F49"));
    public static readonly SolidColorBrush TxBorderDark = new(Color.Parse("#075985"));
    public static readonly SolidColorBrush TxTextDark = new(Color.Parse("#38BDF8"));

    // Universal Accent Colors (Apple HIG)
    public static readonly SolidColorBrush AccentBlue = new(Color.Parse("#007AFF"));
    public static readonly SolidColorBrush AccentGreen = new(Color.Parse("#34C759"));
    public static readonly SolidColorBrush AccentRed = new(Color.Parse("#FF3B30"));
    public static readonly SolidColorBrush StatusGray = new(Color.Parse("#94A3B8"));

    public static bool IsDark => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    public static SolidColorBrush BgWindow => IsDark ? BgWindowDark : BgWindowLight;
    public static SolidColorBrush BgCard => IsDark ? BgCardDark : BgCardLight;
    public static SolidColorBrush BgSubtle => IsDark ? BgSubtleDark : BgSubtleLight;
    public static SolidColorBrush BgEditor => IsDark ? BgEditorDark : BgEditorLight;

    public static SolidColorBrush BorderMuted => IsDark ? BorderMutedDark : BorderMutedLight;
    public static SolidColorBrush BorderStrong => IsDark ? BorderStrongDark : BorderStrongLight;

    public static SolidColorBrush TextPrimary => IsDark ? TextPrimaryDark : TextPrimaryLight;
    public static SolidColorBrush TextSecondary => IsDark ? TextSecondaryDark : TextSecondaryLight;
    public static SolidColorBrush TextMuted => IsDark ? TextMutedDark : TextMutedLight;

    public static SolidColorBrush RxBg => IsDark ? RxBgDark : RxBgLight;
    public static SolidColorBrush RxBorder => IsDark ? RxBorderDark : RxBorderLight;
    public static SolidColorBrush RxText => IsDark ? RxTextDark : RxTextLight;

    public static SolidColorBrush TxBg => IsDark ? TxBgDark : TxBgLight;
    public static SolidColorBrush TxBorder => IsDark ? TxBorderDark : TxBorderLight;
    public static SolidColorBrush TxText => IsDark ? TxTextDark : TxTextLight;

    /// <summary>
    /// Creates a styled macOS card container with subtle border and rounded corners.
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
    public static Border CreateBadge(Control content, IBrush background, IBrush border) =>
        new()
        {
            Background = background,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(7, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = content
        };
}
