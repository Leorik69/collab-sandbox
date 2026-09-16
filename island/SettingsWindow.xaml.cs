using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace NotifyIsland;

public sealed partial class SettingsWindow : Window
{
    private IslandSettings S => App.Settings;
    private bool _ready;

    public SettingsWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        Fill(PackBox, Enum.GetNames<IconPack>(), S.IconPack.ToString());
        Fill(ClockBox, Enum.GetNames<ClockStyle>(), S.ClockStyle.ToString());
        Fill(GlowBox, Enum.GetNames<GlowMode>(), S.Glow.ToString());
        Fill(AnimBox, Enum.GetNames<NotifyAnimation>(), S.NotifyAnim.ToString());
        OpacitySlider.Value = S.Opacity * 100;
        GlowStrSlider.Value = S.GlowStrength * 100;
        BorderSlider.Value = S.BorderThickness;
        BgBox.Text = S.BackgroundHex;
        TextBoxHex.Text = S.TextHex;
        AccentBox.Text = S.AccentHex;
        GlowHexBox.Text = S.GlowHex;
        BorderHexBox.Text = S.BorderHex;
        ThemeToggle.IsOn = S.SettingsWindowDark;
        ApplyWindowTheme();
        _ready = true;
        PaintPreview();
    }

    private static void Fill(ComboBox box, string[] names, string current)
    {
        box.ItemsSource = names;
        box.SelectedItem = current;
    }

    private void Theme_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        S.SettingsWindowDark = ThemeToggle.IsOn;
        ApplyWindowTheme();
        Push();
    }

    private void ApplyWindowTheme()
    {
        if (Content is FrameworkElement root)
            root.RequestedTheme = S.SettingsWindowDark ? ElementTheme.Dark : ElementTheme.Light;
    }

    private void Pack_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        if (PackBox.SelectedItem is string p && Enum.TryParse<IconPack>(p, out var pack))
        {
            S.IconPack = pack;
            S.BackgroundHex = IconPackTheme.PillBackgroundHex(pack);
            S.TextHex = IconPackTheme.ContentForegroundHex(pack);
            BgBox.Text = S.BackgroundHex;
            TextBoxHex.Text = S.TextHex;
        }
        Push();
    }

    private void AnyChanged(object sender, object e)
    {
        if (!_ready) return;
        if (ClockBox.SelectedItem is string c && Enum.TryParse<ClockStyle>(c, out var clock)) S.ClockStyle = clock;
        if (GlowBox.SelectedItem is string g && Enum.TryParse<GlowMode>(g, out var glow)) S.Glow = glow;
        if (AnimBox.SelectedItem is string a && Enum.TryParse<NotifyAnimation>(a, out var anim)) S.NotifyAnim = anim;
        S.Opacity = OpacitySlider.Value / 100.0;
        S.GlowStrength = GlowStrSlider.Value / 100.0;
        S.BorderThickness = BorderSlider.Value;
        S.BackgroundHex = BgBox.Text.Trim();
        S.TextHex = TextBoxHex.Text.Trim();
        S.AccentHex = AccentBox.Text.Trim();
        S.GlowHex = GlowHexBox.Text.Trim();
        S.BorderHex = BorderHexBox.Text.Trim();
        S.BorderGlow = S.Glow != GlowMode.Off;
        Push();
    }

    private void Demo_Click(object sender, RoutedEventArgs e)
    {
        S.UnreadCount = Math.Min(99, S.UnreadCount + 1);
        AppNotificationHub.Bump("Telegram");
        Push();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        S.UnreadCount = 0;
        AppNotificationHub.Clear();
        Push();
    }

    private void Push()
    {
        S.Save();
        App.Island?.ApplySettings();
        PaintPreview();
    }

    private void PaintPreview()
    {
        var bg = Parse(S.BackgroundHex, Color.FromArgb(255, 10, 10, 10));
        var a = (byte)(255 * Math.Clamp(S.Opacity, 0.2, 1));
        PreviewPill.Background = new SolidColorBrush(Color.FromArgb(a, bg.R, bg.G, bg.B));
        PreviewPill.BorderBrush = new SolidColorBrush(Parse(S.BorderHex, Color.FromArgb(255, 255, 159, 10)));
        PreviewPill.BorderThickness = new Thickness(S.BorderThickness);
        PreviewClock.Foreground = new SolidColorBrush(Parse(S.TextHex, Color.FromArgb(255, 245, 245, 247)));
        PreviewHint.Text = $"Свечение {S.Glow}, анимация {S.NotifyAnim}";
    }

    private static Color Parse(string hex, Color fallback)
    {
        hex = (hex ?? "").Trim().TrimStart('#');
        if (hex.Length != 6) return fallback;
        try
        {
            return Color.FromArgb(255, Convert.ToByte(hex[..2], 16), Convert.ToByte(hex[2..4], 16), Convert.ToByte(hex[4..6], 16));
        }
        catch { return fallback; }
    }
}
