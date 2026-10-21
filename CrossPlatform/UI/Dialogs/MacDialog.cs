using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace BCom.CrossPlatform;

/// <summary>
/// Reusable modal dialogs with native macOS aesthetic.
/// </summary>
public static class MacDialog
{
    public static async Task ShowAlertAsync(Window owner, string message, string? title = null)
    {
        var dialog = new Window
        {
            Title = title ?? I18n.T("Error"),
            Width = 440,
            Height = 195,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = MacTheme.BgCard
        };

        var closeBtn = new Button
        {
            Content = I18n.T("Ok"),
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 85,
            Background = MacTheme.AccentBlue,
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold,
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(16, 6),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        closeBtn.Click += (_, _) => dialog.Close();

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
                    Foreground = MacTheme.TextPrimary
                },
                closeBtn
            }
        };

        await dialog.ShowDialog(owner);
    }

    public static async Task ShowWeChatQrAsync(Window owner)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://BCom/res/weixin.png"));
            using var bitmap = new Bitmap(stream);
            var image = new Image { Source = bitmap, Height = 340, Stretch = Stretch.Uniform };

            var window = new Window
            {
                Title = I18n.T("QrTitle"),
                Width = 430,
                Height = 465,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = MacTheme.BgCard,
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
                            Foreground = MacTheme.TextSecondary,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new Border
                        {
                            BorderBrush = MacTheme.BorderMuted,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(10),
                            Background = Brushes.White,
                            Child = image
                        }
                    }
                }
            };

            await window.ShowDialog(owner);
        }
        catch (Exception)
        {
            await ShowAlertAsync(owner, I18n.T("NotSupported"), I18n.T("QrTitle"));
        }
    }
}
