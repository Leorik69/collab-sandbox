using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
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
    private DoubleAnimation? _morphW;
    private DoubleAnimation? _morphH;
    private AppWindow? _appWindow;
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _colonOn = true;
    private bool _hovering;
    private SolidColorBrush? _keylineBrush;
    private Color _keylineCached;
    private double _placeW, _placeH;
    private int _morphGen;
    private int _placeWhenGen;
    private readonly ThemeShadow _clockShadow = new();

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetupOverlay();
        BuildPulse();
        BuildMorph();
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

    private static (int w, int h) WindowPixelSize(double pillW, double pillH)
    {
        var w = (int)Math.Clamp(Math.Round(pillW) + PadW, 140, 360);
        var h = (int)Math.Clamp(Math.Round(pillH) + PadH, 40, 88);
        return (w, h);
    }

    private void PlaceWindow(double pillW, double pillH)
    {
        if (_appWindow is null) return;
        var wa = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var (w, h) = WindowPixelSize(pillW, pillH);
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
        var look = PackTheme.ClockLook(_settings.ThemePack);
        if (_settings.ThemePack == ThemePack.Light || !_settings.FollowSystemTheme)
            _settings.BackgroundHex = look.PillBackgroundHex;
        else
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
        var keyline = KeylineBrush(accent);
        Pill.BorderBrush = keyline;
        GlowBorder.BorderBrush = keyline;
        MinuteArc.Foreground = keyline;
        Badge.Background = keyline;

        var digits = new SolidColorBrush(ParseColor(look.ForegroundHex));
        WeatherTemp.Foreground = digits;
        WeatherDesc.Foreground = digits;
        WeatherGlyph.Foreground = digits;
        WeatherGlyphExp.Foreground = digits;
        var muted = _settings.ThemePack == ThemePack.Light
            ? Color.FromArgb(255, 110, 110, 115)
            : Color.FromArgb(255, 160, 160, 168);
        WeatherFeels.Foreground = WeatherRange.Foreground = new SolidColorBrush(muted);

        var filled = _settings.UnreadCount > 0 || _settings.DoNotDisturb;
        StateIcon.Glyph = _settings.DoNotDisturb ? "\uE708" : "\uEA8F";
        StateIcon.FontWeight = _settings.IconSet == IconSet.Thin
            ? FontWeights.ExtraLight
            : FontWeights.Normal;
        StateIcon.Foreground = filled ? keyline : digits;

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
        var look = PackTheme.ClockLook(_settings.ThemePack);
        var digitBrush = new SolidColorBrush(ParseColor(look.ForegroundHex));
        ClockText.FontFamily = new FontFamily(look.FontFamily);
        ClockText.FontWeight = PackFontWeight(look.FontWeight);
        ClockText.Foreground = digitBrush;
        ClockText.CharacterSpacing = look.CharacterSpacing;
        Typography.SetNumeralAlignment(ClockText,
            look.TabularNumerals ? FontNumeralAlignment.Tabular : FontNumeralAlignment.Default);
        if (look.NeonShadow)
        {
            ClockText.Shadow = _clockShadow;
            ClockText.Translation = new Vector3(0, 0, 8);
        }
        else
        {
            ClockText.Shadow = null;
            ClockText.Translation = Vector3.Zero;
        }

        MinuteArc.Visibility = _settings.ClockStyle == ClockStyle.SecondsMinuteArc ? Visibility.Visible : Visibility.Collapsed;
        // Pack owns font/fg/shadow/colon; ClockStyle keeps HH:mm vs seconds/arc/minimal.
        if (_settings.ClockStyle == ClockStyle.SecondsMinuteArc || IsExpanded())
        {
            WriteClock(now.ToString("HH:mm:ss"), look, digitBrush);
            if (_settings.ClockStyle == ClockStyle.SecondsMinuteArc) MinuteArc.Value = now.Second;
        }
        else
        {
            var sep = _settings.ClockStyle == ClockStyle.DigitalModern && !_colonOn ? " " : ":";
            WriteClock($"{now:HH}{sep}{now:mm}", look, digitBrush);
        }
    }

    private void WriteClock(string text, PackClockLook look, Brush digitBrush)
    {
        ClockText.Inlines.Clear();
        if (!look.AmberColon || !text.Contains(':'))
        {
            ClockText.Inlines.Add(new Run { Text = text, Foreground = digitBrush });
            return;
        }

        var colonBrush = new SolidColorBrush(ParseColor(
            string.IsNullOrEmpty(look.ColonHex) ? "#FF9F0A" : look.ColonHex));
        var parts = text.Split(':');
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
                ClockText.Inlines.Add(new Run { Text = ":", Foreground = colonBrush });
            ClockText.Inlines.Add(new Run { Text = parts[i], Foreground = digitBrush });
        }
    }

    private static Windows.UI.Text.FontWeight PackFontWeight(string name) => name switch
    {
        "Bold" => FontWeights.Bold,
        "SemiBold" => FontWeights.SemiBold,
        "SemiLight" => FontWeights.SemiLight,
        "ExtraLight" => FontWeights.ExtraLight,
        _ => FontWeights.Normal,
    };

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
        ApplyPackIcons();
    }

    private void ApplyPackIcons()
    {
        var pack = _settings.ThemePack;
        BindPackImage(WeatherImage, WeatherGlyph, PackTheme.WeatherPngUri(pack, "sun"));
        BindPackImage(WeatherImageExp, WeatherGlyphExp, PackTheme.WeatherPngUri(pack, "sun"));
        var stem = PackTheme.StatusStem(_settings.DoNotDisturb, _settings.UnreadCount);
        BindPackImage(StateImage, StateIcon, PackTheme.StatusPngUri(pack, stem));
    }

    private void BindPackImage(Image image, FontIcon fallback, string uri)
    {
        if (image.Source is BitmapImage current && current.UriSource is Uri src
            && src.OriginalString == uri)
            return;

        image.Tag = fallback;
        image.ImageOpened -= PackImageOpened;
        image.ImageOpened += PackImageOpened;
        image.ImageFailed -= PackImageFailed;
        image.ImageFailed += PackImageFailed;
        fallback.Visibility = Visibility.Visible;
        image.Visibility = Visibility.Collapsed;
        try
        {
            image.Source = new BitmapImage(new Uri(uri));
        }
        catch
        {
            ShowFontIconFallback(image, fallback);
        }
    }

    private void PackImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is Image image && image.Tag is FontIcon fallback)
        {
            image.Visibility = Visibility.Visible;
            fallback.Visibility = Visibility.Collapsed;
        }
    }

    private void PackImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image image && image.Tag is FontIcon fallback)
            ShowFontIconFallback(image, fallback);
    }

    private static void ShowFontIconFallback(Image image, FontIcon fallback)
    {
        image.Source = null;
        image.Visibility = Visibility.Collapsed;
        fallback.Visibility = Visibility.Visible;
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

    private void BuildMorph()
    {
        _morph = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(MorphMs));
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
        _morphW = MorphAnim("Width", duration, ease);
        _morphH = MorphAnim("Height", duration, ease);
        _morph.Children.Add(_morphW);
        _morph.Children.Add(_morphH);
        _morph.Completed += Morph_Completed;
    }

    private DoubleAnimation MorphAnim(string prop, Duration duration, EasingFunctionBase ease)
    {
        var a = new DoubleAnimation { Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(a, Pill);
        Storyboard.SetTargetProperty(a, prop);
        return a;
    }

    private void Morph_Completed(object sender, object e)
    {
        if (_placeWhenGen != _morphGen) return;
        _placeWhenGen = 0;
        PlaceWindow(_placeW, _placeH);
    }

    private void MorphTo()
    {
        var (w, h) = PillSize();
        _morphW!.To = w;
        _morphH!.To = h;
        _placeW = w;
        _placeH = h;
        var gen = ++_morphGen;

        var (tw, th) = WindowPixelSize(w, h);
        var cur = _appWindow?.Size ?? default;
        var grow = _appWindow is null || tw > cur.Width || th > cur.Height;
        if (grow)
        {
            _placeWhenGen = 0;
            PlaceWindow(w, h);
        }
        else
            _placeWhenGen = gen;

        _morph!.Begin();
    }

    private SolidColorBrush KeylineBrush(Color accent)
    {
        if (_keylineBrush is null || _keylineCached != accent)
        {
            _keylineBrush = new SolidColorBrush(accent);
            _keylineCached = accent;
        }
        return _keylineBrush;
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
        AddEnum("Theme", _settings.ThemePack, v => _settings.ThemePack = v);
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
