using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.Text;
using WinRT.Interop;

namespace NotifyIsland;

public sealed partial class MainWindow : Window
{
    private const int KeyeventfExtendedkey = 0x0001;
    private const int KeyeventfKeyup = 0x0002;
    private const byte VkLwin = 0x5B;
    private const byte VkN = 0x4E;

    private IslandSettings _settings = IslandSettings.Load();
    private Storyboard? _pulse;
    private Storyboard? _aura;
    private AppWindow? _appWindow;
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer _dwell = new() { Interval = TimeSpan.FromMilliseconds(450) };
    private bool _weatherPeek;
    private bool _colonOn = true;
    private DropShadow? _shadow;
    private SpriteVisual? _glowSprite;

    public MainWindow()
    {
        InitializeComponent();
        // Do NOT SetTitleBar(Pill) — that swallows LMB (SL nit). Click = NC; drag via window chrome free.
        ExtendsContentIntoTitleBar = true;
        SetupOverlay();
        BuildPulse();
        BuildAura();
        ApplySettings();
        _tick.Tick += Tick;
        _tick.Start();
        _dwell.Tick += DwellExpand;
        Closed += (_, _) =>
        {
            _tick.Stop();
            _dwell.Stop();
            _settings.Save();
        };
    }

    private void SetupOverlay()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.IsShownInSwitchers = false;
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }
        Reposition();
    }

    private bool ExpandedWeather =>
        _settings.WeatherMode == WeatherMode.Expanded || _weatherPeek;

    private void Reposition()
    {
        if (_appWindow is null) return;
        var (cw, ch) = IslandStyles.SuggestSize(_settings.ClockStyle, ExpandedWeather);
        _settings.Width = cw;
        _settings.Height = ch;
        var display = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        var wa = display.WorkArea;
        var w = Math.Clamp(cw, 120, 420);
        var h = Math.Clamp(ch, 28, 64);
        var x = wa.X + (wa.Width - w) / 2;
        var y = wa.Y + 6;
        _appWindow.MoveAndResize(new RectInt32(x, y, w, h));
        Pill.Width = Math.Max(80, w - 8);
        Pill.Height = Math.Max(24, h - 8);
        GlowRing.Width = AuraHalo.Width = Pill.Width + 10;
        GlowRing.Height = AuraHalo.Height = Pill.Height + 10;
        GlowHost.Width = Pill.Width;
        GlowHost.Height = Pill.Height;
        var cx = Pill.Width / 2;
        var cy = Pill.Height / 2;
        AuraScale.CenterX = HoverScale.CenterX = cx;
        AuraScale.CenterY = HoverScale.CenterY = cy;
        HoverBlush.CornerRadius = Pill.CornerRadius;
        if (_glowSprite is not null)
            _glowSprite.Size = new Vector2((float)Pill.Width, (float)Pill.Height);
    }

    private void ApplySettings()
    {
        var radius = _settings.Shape switch
        {
            IslandShape.Oval => Pill.Height / 2,
            IslandShape.SoftRect => Math.Min(_settings.CornerRadius, 8),
            _ => _settings.CornerRadius
        };
        Pill.CornerRadius = new CornerRadius(radius);
        GlowRing.CornerRadius = AuraHalo.CornerRadius = new CornerRadius(radius + 2);
        HoverBlush.CornerRadius = Pill.CornerRadius;

        var bg = ParseColor(_settings.BackgroundHex);
        var border = ParseColor(_settings.BorderHex);
        var accent = ParseColor(_settings.AccentHex);
        var alpha = (byte)(255 * Math.Clamp(_settings.Opacity, 0.2, 1.0));
        var fill = Color.FromArgb(alpha, bg.R, bg.G, bg.B);

        try
        {
            SystemBackdrop = _settings.Material switch
            {
                MaterialMode.Mica => new MicaBackdrop(),
                MaterialMode.Acrylic => new DesktopAcrylicBackdrop(),
                _ => null
            };
        }
        catch
        {
            SystemBackdrop = null;
        }
        Pill.Background = new SolidColorBrush(fill);
        Pill.BorderBrush = new SolidColorBrush(border);

        ApplyClock();
        ApplyWeather();
        ApplyIcons(accent);
        ApplyUnread(accent);
        ApplyEffects(accent);
        Reposition();
        _settings.Save();
    }

    private void ApplyClock()
    {
        var modern = _settings.ClockStyle == ClockStyle.Modern;
        var minimal = _settings.ClockStyle == ClockStyle.Minimal;
        var secs = _settings.ClockStyle == ClockStyle.SecondsArc;
        var fg = ParseColor(minimal ? "#A0A0A8" : "#F2F2F7");
        var brush = new SolidColorBrush(fg);
        var weight = minimal ? FontWeights.Light : FontWeights.SemiBold;
        var size = minimal ? 14.0 : 15.0;
        foreach (var t in new[] { ClockHH, ClockColon, ClockMM })
        {
            t.Foreground = brush;
            t.FontWeight = weight;
            t.FontSize = size;
            t.CharacterSpacing = minimal ? -20 : 20;
            t.FontFamily = new FontFamily(IslandStyles.ClockFont);
        }
        ClockSec.FontFamily = new FontFamily(IslandStyles.ClockFont);
        ClockSec.Visibility = secs ? Visibility.Visible : Visibility.Collapsed;
        MinuteBar.Visibility = secs ? Visibility.Visible : Visibility.Collapsed;
        if (!modern) ClockColon.Opacity = 1;
        TickClock(DateTime.Now);
    }

    private void ApplyWeather()
    {
        WeatherIcon.Glyph = IslandStyles.WeatherGlyph(_settings.Weather);
        WeatherTemp.Text = IslandStyles.Temp(_settings.TempC, _settings.TempUnit);
        WeatherDesc.Text = IslandStyles.WeatherDesc(_settings.Weather);
        var unit = _settings.TempUnit;
        WeatherRange.Text =
            $"ощущ. {IslandStyles.Temp(_settings.FeelsC, unit)} · {IslandStyles.Temp(_settings.MinC, unit)}–{IslandStyles.Temp(_settings.MaxC, unit)}";
        WeatherExtra.Visibility = ExpandedWeather ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyIcons(Color accent)
    {
        var line = _settings.IconSet == IconSet.MinimalLine;
        var filled = _settings.IconSet == IconSet.FilledActive;
        var active = _settings.DoNotDisturb || _settings.UnreadCount > 0 || _settings.Muted;
        var idle = Color.FromArgb(230, 230, 230, 235);
        var fg = filled && active ? accent : idle;
        var brush = new SolidColorBrush(fg);

        StateIcon.Visibility = line ? Visibility.Collapsed : Visibility.Visible;
        NetIcon.Visibility = line ? Visibility.Collapsed : Visibility.Visible;

        var g = IslandStyles.StatusGlyphs(filled);
        StateIcon.Glyph = _settings.DoNotDisturb ? g.dnd : _settings.Muted ? g.mute : g.bell;
        NetIcon.Glyph = _settings.WifiOn ? g.wifi : g.wifiOff;
        StateIcon.Foreground = brush;
        NetIcon.Foreground = brush;
        WeatherIcon.Foreground = new SolidColorBrush(idle);

        HidePaths();
        if (!line) return;
        var state = _settings.DoNotDisturb ? PathMoon : _settings.Muted ? PathMute : PathBell;
        var net = _settings.WifiOn ? PathWifi : PathWifiOff;
        state.Visibility = Visibility.Visible;
        net.Visibility = Visibility.Visible;
        state.Stroke = brush;
        net.Stroke = brush;
    }

    private void HidePaths()
    {
        foreach (var p in new[] { PathBell, PathMoon, PathMute, PathWifi, PathWifiOff })
            p.Visibility = Visibility.Collapsed;
    }

    private void ApplyUnread(Color accent)
    {
        Dot.Fill = new SolidColorBrush(_settings.DoNotDisturb
            ? Color.FromArgb(200, 180, 180, 180)
            : accent);
        Badge.Background = new SolidColorBrush(accent);

        if (_settings.UnreadCount > 0 && !_settings.DoNotDisturb)
        {
            Badge.Visibility = Visibility.Visible;
            BadgeText.Text = _settings.UnreadCount > 9 ? "9+" : _settings.UnreadCount.ToString();
            Dot.Visibility = Visibility.Collapsed;
            if (_settings.PulseOnUnread) _pulse?.Begin();
            else _pulse?.Stop();
            if (_settings.PulseAura) _aura?.Begin();
            else StopAura();
        }
        else
        {
            Badge.Visibility = Visibility.Collapsed;
            Dot.Visibility = Visibility.Visible;
            _pulse?.Stop();
            Dot.Opacity = 1;
            StopAura();
        }
    }

    private void ApplyEffects(Color accent)
    {
        GlowRing.BorderBrush = new SolidColorBrush(Color.FromArgb(180, accent.R, accent.G, accent.B));
        GlowRing.Opacity = _settings.BorderGlow ? 0.7 : 0;
        AuraHalo.Background = new SolidColorBrush(Color.FromArgb(70, accent.R, accent.G, accent.B));
        BlushA.Color = Color.FromArgb(0x40, accent.R, accent.G, accent.B);
        EnsureGlow(accent);
        if (_shadow is not null)
        {
            _shadow.Color = accent;
            _shadow.BlurRadius = _settings.BorderGlow ? 20f : 0f;
            _shadow.Opacity = _settings.BorderGlow ? 0.8f : 0f;
        }
        MinuteBar.Foreground = new SolidColorBrush(accent);
    }

    private void EnsureGlow(Color accent)
    {
        try
        {
            var host = ElementCompositionPreview.GetElementVisual(GlowHost);
            var c = host.Compositor;
            if (_shadow is null)
            {
                _shadow = c.CreateDropShadow();
                _shadow.Offset = new Vector3(0, 0, 0);
                _glowSprite = c.CreateSpriteVisual();
                _glowSprite.Shadow = _shadow;
                _glowSprite.Size = new Vector2((float)Math.Max(1, Pill.Width), (float)Math.Max(1, Pill.Height));
                ElementCompositionPreview.SetElementChildVisual(GlowHost, _glowSprite);
            }
            _shadow.Color = accent;
        }
        catch
        {
            // Composition shadow is best-effort (E1 still has GlowRing)
        }
    }

    private void Tick(object sender, object e)
    {
        var now = DateTime.Now;
        TickClock(now);
        if (_settings.ClockStyle == ClockStyle.Modern)
        {
            _colonOn = now.Millisecond < 500;
            ClockColon.Opacity = _colonOn ? 1 : 0.2;
        }
    }

    private void TickClock(DateTime now)
    {
        var (hh, mm, ss) = IslandStyles.SplitClock(now, _settings.Clock24h);
        ClockHH.Text = hh;
        ClockMM.Text = mm;
        ClockSec.Text = ":" + ss;
        if (_settings.ClockStyle == ClockStyle.SecondsArc)
            MinuteBar.Value = IslandStyles.MinuteProgress(now);
    }

    private void BuildPulse()
    {
        _pulse = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        var anim = new DoubleAnimation
        {
            From = 1,
            To = 0.25,
            Duration = new Duration(TimeSpan.FromMilliseconds(700)),
            AutoReverse = true
        };
        Storyboard.SetTarget(anim, Dot);
        Storyboard.SetTargetProperty(anim, "Opacity");
        _pulse.Children.Add(anim);
    }

    private void BuildAura()
    {
        _aura = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        foreach (var prop in new[] { "ScaleX", "ScaleY" })
        {
            var anim = new DoubleAnimation
            {
                From = 1,
                To = 1.08,
                Duration = new Duration(TimeSpan.FromMilliseconds(700)),
                AutoReverse = true
            };
            Storyboard.SetTarget(anim, AuraScale);
            Storyboard.SetTargetProperty(anim, prop);
            _aura.Children.Add(anim);
        }
        var fade = new DoubleAnimation
        {
            From = 0.12,
            To = 0.55,
            Duration = new Duration(TimeSpan.FromMilliseconds(700)),
            AutoReverse = true
        };
        Storyboard.SetTarget(fade, AuraHalo);
        Storyboard.SetTargetProperty(fade, "Opacity");
        _aura.Children.Add(fade);
    }

    private void StopAura()
    {
        _aura?.Stop();
        AuraScale.ScaleX = 1;
        AuraScale.ScaleY = 1;
        AuraHalo.Opacity = 0;
    }

    private static Color ParseColor(string hex)
    {
        hex = (hex ?? "#1C1C1E").Trim().TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        if (hex.Length != 8) return Color.FromArgb(255, 28, 28, 30);
        byte a = Convert.ToByte(hex[..2], 16);
        byte r = Convert.ToByte(hex.Substring(2, 2), 16);
        byte g = Convert.ToByte(hex.Substring(4, 2), 16);
        byte b = Convert.ToByte(hex.Substring(6, 2), 16);
        return Color.FromArgb(a, r, g, b);
    }

    private void Pill_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(Pill);
        if (pt.Properties.IsLeftButtonPressed)
        {
            OpenNotificationCenter();
            if (_settings.UnreadCount == 0)
            {
                _settings.UnreadCount = 1;
                ApplySettings();
            }
            e.Handled = true;
        }
    }

    private void Pill_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (_settings.HoverHighlight)
        {
            HoverScale.ScaleX = 1.06;
            HoverScale.ScaleY = 1.06;
            HoverBlush.Opacity = 1;
        }
        if (_settings.WeatherMode == WeatherMode.Compact)
        {
            _dwell.Stop();
            _dwell.Start();
        }
    }

    private void Pill_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _dwell.Stop();
        HoverScale.ScaleX = 1;
        HoverScale.ScaleY = 1;
        HoverBlush.Opacity = 0;
        if (_weatherPeek)
        {
            _weatherPeek = false;
            ApplyWeather();
            Reposition();
        }
    }

    private void DwellExpand(object sender, object e)
    {
        _dwell.Stop();
        if (_settings.WeatherMode != WeatherMode.Compact) return;
        _weatherPeek = true;
        ApplyWeather();
        Reposition();
    }

    private void Pill_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var menu = new MenuFlyout();

        Toggle(menu, "Не беспокоить (DND)", _settings.DoNotDisturb, v => _settings.DoNotDisturb = v);
        Item(menu, "Сбросить счётчик", () => _settings.UnreadCount = 0);
        menu.Items.Add(new MenuFlyoutSeparator());

        EnumSub(menu, "Часы", _settings.ClockStyle, v => _settings.ClockStyle = v,
            (ClockStyle.Modern, "C1 Digital Modern"),
            (ClockStyle.Minimal, "C2 Digital Minimal"),
            (ClockStyle.SecondsArc, "C3 Seconds+arc"));
        EnumSub(menu, "Погода", _settings.WeatherMode, v => _settings.WeatherMode = v,
            (WeatherMode.Compact, "W1 Compact Badge"),
            (WeatherMode.Expanded, "W2 Expanded"));
        EnumSub(menu, "Иконки", _settings.IconSet, v => _settings.IconSet = v,
            (IconSet.Fluent, "I1 Fluent"),
            (IconSet.MinimalLine, "I2 Minimal Line"),
            (IconSet.FilledActive, "I3 Filled Active"));

        var fx = new MenuFlyoutSubItem { Text = "Эффекты" };
        ToggleItem(fx, "E1 Border Glow", _settings.BorderGlow, v => _settings.BorderGlow = v);
        ToggleItem(fx, "E2 Pulse Aura", _settings.PulseAura, v => _settings.PulseAura = v);
        ToggleItem(fx, "E3 Hover Highlight", _settings.HoverHighlight, v => _settings.HoverHighlight = v);
        menu.Items.Add(fx);

        EnumSub(menu, "Температура", _settings.TempUnit, v => _settings.TempUnit = v,
            (TempUnit.C, "°C"), (TempUnit.F, "°F"));

        menu.Items.Add(new MenuFlyoutSeparator());
        var mat = new MenuFlyoutSubItem { Text = "Материал" };
        foreach (MaterialMode m in Enum.GetValues(typeof(MaterialMode)))
        {
            var item = new MenuFlyoutItem { Text = m.ToString() };
            var captured = m;
            item.Click += (_, _) => { _settings.Material = captured; ApplySettings(); };
            mat.Items.Add(item);
        }
        menu.Items.Add(mat);

        var shape = new MenuFlyoutSubItem { Text = "Форма" };
        foreach (IslandShape s in Enum.GetValues(typeof(IslandShape)))
        {
            var item = new MenuFlyoutItem { Text = s.ToString() };
            var captured = s;
            item.Click += (_, _) => { _settings.Shape = captured; ApplySettings(); };
            shape.Items.Add(item);
        }
        menu.Items.Add(shape);

        var radius = new MenuFlyoutSubItem { Text = "Скругление" };
        foreach (var r in new[] { 8.0, 12.0, 16.0, 20.0 })
        {
            var item = new MenuFlyoutItem { Text = $"{r:0}" };
            var captured = r;
            item.Click += (_, _) => { _settings.CornerRadius = captured; _settings.Shape = IslandShape.Capsule; ApplySettings(); };
            radius.Items.Add(item);
        }
        menu.Items.Add(radius);

        var opacity = new MenuFlyoutSubItem { Text = "Прозрачность" };
        foreach (var o in new[] { 0.55, 0.7, 0.85, 1.0 })
        {
            var item = new MenuFlyoutItem { Text = $"{o:P0}" };
            var captured = o;
            item.Click += (_, _) => { _settings.Opacity = captured; ApplySettings(); };
            opacity.Items.Add(item);
        }
        menu.Items.Add(opacity);

        var accent = new MenuFlyoutSubItem { Text = "Акцент" };
        foreach (var (name, hex) in new[] { ("Blue", "#0A84FF"), ("Green", "#30D158"), ("Orange", "#FF9F0A"), ("Pink", "#FF375F") })
        {
            var item = new MenuFlyoutItem { Text = name };
            var captured = hex;
            item.Click += (_, _) => { _settings.AccentHex = captured; ApplySettings(); };
            accent.Items.Add(item);
        }
        menu.Items.Add(accent);

        menu.Items.Add(new MenuFlyoutSeparator());
        Item(menu, "Демо: +1 уведомление", () => _settings.UnreadCount = Math.Min(99, _settings.UnreadCount + 1));
        Item(menu, "Демо: смена погоды", () => _settings.Weather = IslandStyles.NextWeather(_settings.Weather));
        Toggle(menu, "Без звука", _settings.Muted, v => _settings.Muted = v);
        Toggle(menu, "Wi-Fi", _settings.WifiOn, v => _settings.WifiOn = v);

        menu.ShowAt(Pill, e.GetPosition(Pill));
        e.Handled = true;
    }

    private void Item(MenuFlyout menu, string text, Action act)
    {
        var it = new MenuFlyoutItem { Text = text };
        it.Click += (_, _) => { act(); ApplySettings(); };
        menu.Items.Add(it);
    }

    private void Toggle(MenuFlyout menu, string text, bool on, Action<bool> set)
    {
        var it = new ToggleMenuFlyoutItem { Text = text, IsChecked = on };
        it.Click += (_, _) => { set(it.IsChecked); ApplySettings(); };
        menu.Items.Add(it);
    }

    private void ToggleItem(MenuFlyoutSubItem parent, string text, bool on, Action<bool> set)
    {
        var it = new ToggleMenuFlyoutItem { Text = text, IsChecked = on };
        it.Click += (_, _) => { set(it.IsChecked); ApplySettings(); };
        parent.Items.Add(it);
    }

    private void EnumSub<T>(MenuFlyout menu, string title, T current, Action<T> set, params (T value, string label)[] items) where T : struct, Enum
    {
        var sub = new MenuFlyoutSubItem { Text = title };
        foreach (var (value, label) in items)
        {
            var it = new MenuFlyoutItem { Text = label + (Equals(value, current) ? "  ✓" : "") };
            var captured = value;
            it.Click += (_, _) => { set(captured); ApplySettings(); };
            sub.Items.Add(it);
        }
        menu.Items.Add(sub);
    }

    private static void OpenNotificationCenter()
    {
        keybd_event(VkLwin, 0, KeyeventfExtendedkey, 0);
        keybd_event(VkN, 0, 0, 0);
        keybd_event(VkN, 0, KeyeventfKeyup, 0);
        keybd_event(VkLwin, 0, KeyeventfExtendedkey | KeyeventfKeyup, 0);
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
}
