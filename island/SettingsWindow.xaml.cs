using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace NotifyIsland;

public sealed partial class SettingsWindow : Window
{
    private static readonly string[] Backgrounds = ["#0A0A0A", "#1C1C1E", "#14120C", "#2C2C2E", "#F5F5F7"];
    private static readonly string[] Accents = ["#FF9F0A", "#FFD60A", "#FF453A", "#30D158", "#BF5AF2", "#EBEBF5"];

    private readonly IslandSettings _s;
    private readonly Action _applied;
    private bool _loading = true;

    public SettingsWindow(IslandSettings settings, Action applied)
    {
        _s = settings;
        _applied = applied;
        InitializeComponent();
        Title = "Настройки";
        try
        {
            var id = Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this));
            AppWindow.GetFromWindowId(id).Resize(new SizeInt32(372, 620));
        }
        catch { }
        ApplyChrome();
        LoadUi();
        _loading = false;
    }

    private void ApplyChrome()
    {
        Root.RequestedTheme = _s.SettingsDark ? ElementTheme.Dark : ElementTheme.Light;
        ThemeSwitch.IsOn = _s.SettingsDark;
    }

    private void LoadUi()
    {
        SelectTag(TransparencyBox, _s.GetTransparency().ToString());
        SelectTag(AnimBox, _s.UnreadAnim.ToString());
        IslandGlowSlider.Value = _s.IslandGlowIntensity;
        ClockGlowSlider.Value = _s.ClockGlowIntensity;
        BorderSlider.Value = _s.BorderThickness;
        AutostartSwitch.IsOn = _s.StartWithWindows;
        AnimSwitch.IsOn = _s.AnimationsEnabled;
        SoundSwitch.IsOn = _s.SoundEnabled;
        NotifySlider.Value = _s.NotificationDurationMs;
        PaintSwatches();
    }

    private void PaintSwatches()
    {
        Fill(BgRow, Backgrounds, _s.BackgroundHex, hex =>
        {
            _s.FollowSystemTheme = false;
            _s.CustomPalette = true;
            _s.BackgroundHex = hex;
        });
        Fill(AccentRow, Accents, _s.AccentHex, hex => { _s.CustomPalette = true; _s.AccentHex = hex; });
        Fill(BorderRow, Accents, _s.BorderHex, hex => { _s.CustomPalette = true; _s.BorderHex = hex; });
    }

    private void Fill(StackPanel row, string[] palette, string current, Action<string> set)
    {
        row.Children.Clear();
        foreach (var hex in palette)
        {
            var on = hex.Equals(current, StringComparison.OrdinalIgnoreCase);
            var btn = new Button
            {
                Width = 22,
                Height = 22,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(Parse(hex)),
                BorderThickness = new Thickness(on ? 2 : 1),
                BorderBrush = new SolidColorBrush(on
                    ? Parse("#FF9F0A")
                    : Color.FromArgb(70, 160, 160, 168))
            };
            var pick = hex;
            btn.Click += (_, _) =>
            {
                if (_loading) return;
                set(pick);
                Persist();
                PaintSwatches();
            };
            row.Children.Add(btn);
        }
    }

    private void OnCombo(object sender, SelectionChangedEventArgs e) => Pull();

    private void OnSlider(object sender, RangeBaseValueChangedEventArgs e) => Pull();

    private void Pull()
    {
        if (_loading) return;
        if (TagOf(TransparencyBox) is { } t && Enum.TryParse<TransparencyLevel>(t, out var tl))
            _s.SetTransparency(tl);
        if (TagOf(AnimBox) is { } a && Enum.TryParse<UnreadAnimation>(a, out var anim))
            _s.UnreadAnim = anim;
        _s.IslandGlowIntensity = IslandGlowSlider.Value;
        _s.ClockGlowIntensity = ClockGlowSlider.Value;
        _s.BorderThickness = BorderSlider.Value;
        _s.StartWithWindows = AutostartSwitch.IsOn;
        _s.AnimationsEnabled = AnimSwitch.IsOn;
        _s.SoundEnabled = SoundSwitch.IsOn;
        _s.NotificationDurationMs = (int)NotifySlider.Value;
        OverlayStartup.Apply(_s.StartWithWindows);
        Persist();
    }

    private void OnFlags(object sender, RoutedEventArgs e) => Pull();

    private void OnThemeToggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _s.SettingsDark = ThemeSwitch.IsOn;
        Root.RequestedTheme = _s.SettingsDark ? ElementTheme.Dark : ElementTheme.Light;
        _s.Save();
    }

    private void Persist()
    {
        _s.SyncDerived();
        _s.Save();
        _applied();
    }

    private static void SelectTag(ComboBox box, string tag)
    {
        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is ComboBoxItem item && (item.Tag as string) == tag)
            {
                box.SelectedIndex = i;
                return;
            }
        }
        box.SelectedIndex = 0;
    }

    private static string? TagOf(ComboBox box)
    {
        if (box.SelectedItem is not ComboBoxItem item) return null;
        return item.Tag as string ?? item.Content as string;
    }

    private static Color Parse(string hex)
    {
        hex = (hex ?? "#0A0A0A").Trim().TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        if (hex.Length != 8) return Color.FromArgb(255, 10, 10, 10);
        return Color.FromArgb(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16), Convert.ToByte(hex.Substring(6, 2), 16));
    }
}
