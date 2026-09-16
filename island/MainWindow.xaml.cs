using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace NotifyIsland;

public sealed partial class MainWindow : Window
{
    private const int KeyeventfExtendedkey = 0x0001;
    private const int KeyeventfKeyup = 0x0002;
    private const byte VkLwin = 0x5B;
    private const byte VkN = 0x4E;

    // Sizes + 280ms: WinUI mapping (Packt Live Widget sources are 404).
    private const double MinimalW = 120, MinimalH = 28;
    private const double CompactW = 176, CompactH = 32;
    private const double ExpandedW = 248, ExpandedH = 52;
    private const int MorphMs = 280;
    private const int PadW = 32, PadH = 16;

    private IslandSettings _settings = IslandSettings.Load();
    private Storyboard? _pulse;
    private Storyboard? _morph;
    private AppWindow? _appWindow;
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _colonOn = true;
    private bool _hovering;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetupOverlay();
        BuildPulse();
        ApplySettings();
        _clock.Tick += (_, _) => TickClock();
        _clock.Start();
        TickClock();
        Closed += (_, _) => { _clock.Stop(); _settings.Save(); };
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

    private (double w, double h) PillSize()
    {
        if (_settings.Presentation == PresentationMode.Minimal) return (MinimalW, MinimalH);
        if (IsExpanded()) return (ExpandedW, ExpandedH);
        return (CompactW, CompactH);
    }

    private bool IsExpanded() =>
        _settings.Presentation == PresentationMode.Expanded
        || (_hovering && _settings.Presentation == PresentationMode.Compact);

    private void Reposition()
    {
        if (_appWindow is null) return;
        var (pw, ph) = PillSize();
        Pill.Width = pw;
        Pill.Height = ph;
        PlaceWindow(pw, ph);
        SyncChrome();
    }

    private void PlaceWindow(double pillW, double pillH)
    {
        if (_appWindow is null) return;
        var wa = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var w = (int)Math.Clamp(Math.Round(pillW) + PadW, 140, 360);
        var h = (int)Math.Clamp(Math.Round(pillH) + PadH, 40, 88);
        _appWindow.MoveAndResize(new RectInt32(wa.X + (wa.Width - w) / 2, wa.Y + 6, w, h));
    }

    private void SyncChrome()
    {
        GlowBorder.Width = Pill.Width + 8;
        GlowBorder.Height = Pill.Height + 4;
        GlowBorder.CornerRadius = new CornerRadius(Pill.Height / 2 + 2);
        if (Pill.RenderTransform is ScaleTransform st)
        {
            st.CenterX = Pill.Width / 2;
            st.CenterY = Pill.Height / 2;
        }
        var radius = _settings.Shape switch
        {
            IslandShape.SoftRect => Math.Min(_settings.CornerRadius, 8),
            _ => Pill.Height / 2
        };
        Pill.CornerRadius = new CornerRadius(radius);
    }

    private void Pill_SizeChanged(object sender, SizeChangedEventArgs e) => SyncChrome();

    private void ApplySettings()
    {
        if (_settings.FollowSystemTheme)
        {
            var light = Application.Current.RequestedTheme == ApplicationTheme.Light;
            _settings.BackgroundHex = light ? "#F5F5F7" : "#0A0A0A";
        }

        var bg = ParseColor(_settings.BackgroundHex);
        var accent = ParseColor("#FF9F0A"); // CC: paint orange keyline; do not rewrite LocalSettings
        var a = (byte)(255 * Math.Clamp(_settings.Opacity, 0.2, 1.0));
        try
        {
            SystemBackdrop = _settings.Material switch
            {
                MaterialMode.Mica => new MicaBackdrop(),
                MaterialMode.Acrylic => new DesktopAcrylicBackdrop(),
                _ => null
            };
        }
        catch { SystemBackdrop = null; }

        Pill.Background = new SolidColorBrush(Color.FromArgb(a, bg.R, bg.G, bg.B));
        var keyline = new SolidColorBrush(accent);
        Pill.BorderBrush = keyline;
        GlowBorder.BorderBrush = keyline;
        MinuteArc.Foreground = keyline;
        Badge.Background = keyline;

        var filled = _settings.UnreadCount > 0 || _settings.DoNotDisturb;
        StateIcon.Glyph = _settings.DoNotDisturb ? "\uE708" : "\uEA8F";
        StateIcon.FontWeight = _settings.IconSet == IconSet.Thin
            ? Microsoft.UI.Text.FontWeights.ExtraLight
            : Microsoft.UI.Text.FontWeights.Normal;
        StateIcon.Foreground = new SolidColorBrush(filled ? accent : Color.FromArgb(255, 245, 245, 247));

        Dot.Visibility = Visibility.Collapsed;
        var activity = _settings.UnreadCount > 0 && !_settings.DoNotDisturb;
        Badge.Visibility = activity ? Visibility.Visible : Visibility.Collapsed;
        if (activity)
        {
            BadgeText.Text = _settings.UnreadCount > 9 ? "9+" : _settings.UnreadCount.ToString();
            if (_settings.PulseAura) _pulse?.Begin();
            else StopPulse();
        }
        else
        {
            StopPulse();
        }
        GlowBorder.Opacity = _settings.BorderGlow || (activity && _settings.PulseAura) ? 0.85 : 0;

        ApplyClockStyle();
        ApplyWeather();
        ApplyPresentation();
        _settings.Save();
    }

    private void StopPulse()
    {
        _pulse?.Stop();
        PillScale.ScaleX = PillScale.ScaleY = 1;
        Pill.Opacity = 1;
        if (!_settings.BorderGlow) GlowBorder.Opacity = 0;
    }

    private void ApplyClockStyle()
    {
        var now = DateTime.Now;
        MinuteArc.Visibility = _settings.ClockStyle == ClockStyle.SecondsMinuteArc ? Visibility.Visible : Visibility.Collapsed;
        ClockText.FontWeight = _settings.ClockStyle == ClockStyle.DigitalMinimal
            ? Microsoft.UI.Text.FontWeights.ExtraLight
            : Microsoft.UI.Text.FontWeights.SemiBold;
        ClockText.Foreground = new SolidColorBrush(_settings.ClockStyle == ClockStyle.DigitalMinimal
            ? Color.FromArgb(255, 160, 160, 168)
            : Color.FromArgb(255, 245, 245, 247));
        // HH:MM:SS in expanded = WinUI addition (not Packt Live Widget).
        if (_settings.ClockStyle == ClockStyle.SecondsMinuteArc || IsExpanded())
        {
            ClockText.Text = now.ToString("HH:mm:ss");
            if (_settings.ClockStyle == ClockStyle.SecondsMinuteArc) MinuteArc.Value = now.Second;
        }
        else
        {
            var sep = _settings.ClockStyle == ClockStyle.DigitalModern && !_colonOn ? " " : ":";
            ClockText.Text = $"{now:HH}{sep}{now:mm}";
        }
    }

    private void TickClock()
    {
        _colonOn = !_colonOn;
        ApplyClockStyle();
    }

    private void ApplyWeather()
    {
        int c = 18, feels = 16, min = 12, max = 21;
        string Unit(int t) => _settings.TempUnit == TempUnit.Fahrenheit ? $"{t * 9 / 5 + 32}°F" : $"{t}°";
        WeatherTemp.Text = Unit(c);
        WeatherGlyph.Glyph = WeatherGlyphExp.Glyph = "\uE706";
        WeatherDesc.Text = "Clear";
        WeatherFeels.Text = "feels " + Unit(feels);
        WeatherRange.Text = Unit(min) + " / " + Unit(max);
    }

    private void ApplyPresentation()
    {
        var minimal = _settings.Presentation == PresentationMode.Minimal;
        var expanded = IsExpanded() && !minimal;
        CompactTrailing.Visibility = minimal ? Visibility.Collapsed : Visibility.Visible;
        var extras = expanded && (_settings.Presentation == PresentationMode.Expanded
            || _settings.WeatherMode == WeatherMode.ExpandOnHover);
        ExpandedRegion.Visibility = extras ? Visibility.Visible : Visibility.Collapsed;
        MorphTo();
    }

    private void MorphTo()
    {
        var (w, h) = PillSize();
        _morph?.Stop();
        _morph = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(MorphMs));
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
        void Add(string prop, double to)
        {
            var a = new DoubleAnimation { To = to, Duration = duration, EasingFunction = ease };
            Storyboard.SetTarget(a, Pill);
            Storyboard.SetTargetProperty(a, prop);
            _morph.Children.Add(a);
        }
        Add("Width", w);
        Add("Height", h);
        _morph.Completed += (_, _) => PlaceWindow(w, h);
        _morph.Begin();
        PlaceWindow(w, h);
    }

    private void BuildPulse()
    {
        _pulse = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        var d = TimeSpan.FromMilliseconds(700);
        void Scale(string prop)
        {
            var a = new DoubleAnimation { From = 1, To = 1.06, Duration = d, AutoReverse = true };
            Storyboard.SetTarget(a, PillScale);
            Storyboard.SetTargetProperty(a, prop);
            _pulse.Children.Add(a);
        }
        Scale("ScaleX");
        Scale("ScaleY");
        var fade = new DoubleAnimation { From = 1, To = 0.78, Duration = d, AutoReverse = true };
        Storyboard.SetTarget(fade, Pill);
        Storyboard.SetTargetProperty(fade, "Opacity");
        _pulse.Children.Add(fade);
        var glow = new DoubleAnimation { From = 0.25, To = 0.9, Duration = d, AutoReverse = true };
        Storyboard.SetTarget(glow, GlowBorder);
        Storyboard.SetTargetProperty(glow, "Opacity");
        _pulse.Children.Add(glow);
    }

    private static Color ParseColor(string hex)
    {
        hex = (hex ?? "#0A0A0A").Trim().TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        if (hex.Length != 8) return Color.FromArgb(255, 10, 10, 10);
        return Color.FromArgb(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16), Convert.ToByte(hex.Substring(6, 2), 16));
    }

    private void Pill_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(Pill).Properties.IsLeftButtonPressed)
        {
            OpenNotificationCenter();
            e.Handled = true;
        }
    }

    private void Pill_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Hover expand = WinUI addition.
        _hovering = true;
        ApplyClockStyle();
        ApplyPresentation();
    }

    private void Pill_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _hovering = false;
        ApplyClockStyle();
        ApplyPresentation();
    }

    private void Pill_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var menu = new MenuFlyout();
        void AddEnum<T>(string title, T current, Action<T> set) where T : struct, Enum
        {
            var sub = new MenuFlyoutSubItem { Text = title };
            foreach (T v in Enum.GetValues<T>())
            {
                var item = new MenuFlyoutItem { Text = v.ToString() };
                var cap = v;
                item.Click += (_, _) => { set(cap); ApplySettings(); };
                sub.Items.Add(item);
            }
            menu.Items.Add(sub);
        }

        var dnd = new ToggleMenuFlyoutItem { Text = "DND", IsChecked = _settings.DoNotDisturb };
        dnd.Click += (_, _) => { _settings.DoNotDisturb = dnd.IsChecked; ApplySettings(); };
        menu.Items.Add(dnd);
        var glow = new ToggleMenuFlyoutItem { Text = "Border Glow", IsChecked = _settings.BorderGlow };
        glow.Click += (_, _) => { _settings.BorderGlow = glow.IsChecked; ApplySettings(); };
        menu.Items.Add(glow);
        var pulse = new ToggleMenuFlyoutItem { Text = "Pulse Aura", IsChecked = _settings.PulseAura };
        pulse.Click += (_, _) => { _settings.PulseAura = pulse.IsChecked; ApplySettings(); };
        menu.Items.Add(pulse);
        menu.Items.Add(new MenuFlyoutSeparator());
        AddEnum("Presentation", _settings.Presentation, v => _settings.Presentation = v);
        AddEnum("Clock", _settings.ClockStyle, v => _settings.ClockStyle = v);
        AddEnum("Weather", _settings.WeatherMode, v => _settings.WeatherMode = v);
        AddEnum("Icons", _settings.IconSet, v => _settings.IconSet = v);
        AddEnum("Temp", _settings.TempUnit, v => _settings.TempUnit = v);
        AddEnum("Material", _settings.Material, v => _settings.Material = v);
        menu.Items.Add(new MenuFlyoutSeparator());
        var demo = new MenuFlyoutItem { Text = "Demo +1 unread" };
        demo.Click += (_, _) => { _settings.UnreadCount = Math.Min(99, _settings.UnreadCount + 1); ApplySettings(); };
        menu.Items.Add(demo);
        var clear = new MenuFlyoutItem { Text = "Clear unread" };
        clear.Click += (_, _) => { _settings.UnreadCount = 0; ApplySettings(); };
        menu.Items.Add(clear);
        menu.ShowAt(Pill, e.GetPosition(Pill));
        e.Handled = true;
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
