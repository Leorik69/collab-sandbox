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
using Windows.Foundation;
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
    private SettingsWindow? _settingsUi;
    private Storyboard? _pulse;
    private Storyboard? _morph;
    private DoubleAnimation? _morphW;
    private DoubleAnimation? _morphH;
    private DoubleAnimation? _morphOp;
    private AppWindow? _appWindow;
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _colonOn = true;
    private bool _hovering;
    private bool _menuOpen;
    private SolidColorBrush? _keylineBrush;
    private Color _keylineCached;
    private ThemeShadow? _clockNeonShadow;
    private RectangleGeometry? _pillClip;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetupOverlay();
        BuildMorph();
        ApplySettings();
        _ = ToastNotificationListener.StartAsync(() =>
            DispatcherQueue.TryEnqueue(() =>
            {
                if (ToastNotificationListener.IsListening)
                    _settings.UnreadCount = AppNotificationHub.Total;
                ApplySettings();
            }));
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
        var host = new TransparentBackdrop();
        SystemBackdrop = host;
        host.AttachHwnd(hwnd);
        PlaceWindow();
        Reposition();
    }

    private (double w, double h) PillSize()
    {
        if (_settings.Presentation == PresentationMode.Minimal) return (MinimalW, MinimalH);
        if (IsExpanded())
        {
            var extra = Math.Min(96, AppNotificationHub.Snapshot().Count * 32);
            return (ExpandedW + extra, ExpandedH);
        }
        return (CompactW, CompactH);
    }

    private bool HasAppToasts() =>
        !_settings.DoNotDisturb && (AppNotificationHub.Total > 0 || _settings.UnreadCount > 0);

    private bool IsExpanded() =>
        _settings.Presentation == PresentationMode.Expanded
        || ((_hovering || _menuOpen || HasAppToasts()) && _settings.Presentation == PresentationMode.Compact);

    private bool ShowExpandedExtras() =>
        IsExpanded()
        && _settings.Presentation != PresentationMode.Minimal
        && (_settings.Presentation == PresentationMode.Expanded
            || _settings.WeatherMode == WeatherMode.ExpandOnHover);

    private void Reposition()
    {
        if (_appWindow is null) return;
        var (pw, ph) = PillSize();
        Pill.Width = pw;
        Pill.Height = ph;
        PlaceWindow();
        SyncChrome();
    }

    private static (int w, int h) HostPixelSize()
    {
        var w = (int)Math.Clamp(Math.Round(ExpandedW) + PadW + 96, 140, 420);
        var h = (int)Math.Clamp(Math.Round(ExpandedH) + PadH, 40, 88);
        return (w, h);
    }

    private void PlaceWindow()
    {
        if (_appWindow is null) return;
        var wa = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var (w, h) = HostPixelSize();
        _appWindow.MoveAndResize(new RectInt32(wa.X + (wa.Width - w) / 2, wa.Y + 6, w, h));
    }

    private void SyncChrome()
    {
        GlowBorder.Width = Pill.Width + 8;
        GlowBorder.Height = Pill.Height + 4;
        GlowBorder.CornerRadius = new CornerRadius(Pill.Height / 2 + 2);
        PillScale.CenterX = Pill.Width / 2;
        PillScale.CenterY = Pill.Height / 2;
        var radius = _settings.Shape switch
        {
            IslandShape.SoftRect => Math.Min(_settings.CornerRadius, 8),
            _ => Pill.Height / 2
        };
        Pill.CornerRadius = new CornerRadius(radius);
    }

    private void Pill_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SyncChrome();
        if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0) return;
        var rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
        if (_pillClip is null)
        {
            _pillClip = new RectangleGeometry { Rect = rect };
            Pill.Clip = _pillClip;
        }
        else
        {
            _pillClip.Rect = rect;
        }
    }

    private void ApplySettings()
    {
        if (_settings.FollowSystemTheme)
        {
            var light = Application.Current.RequestedTheme == ApplicationTheme.Light;
            _settings.BackgroundHex = light ? "#F5F5F7" : "#0A0A0A";
        }
        else if (!_settings.CustomPalette)
        {
            _settings.BackgroundHex = IconPackTheme.PillBackgroundHex(_settings.IconPack);
        }

        var bg = ParseColor(_settings.BackgroundHex);
        var accent = ParseColor(_settings.AccentHex);
        var border = ParseColor(_settings.BorderHex);
        var a = (byte)(255 * Math.Clamp(_settings.Opacity, 0.2, 1.0));
        // Host stays TransparentBackdrop. Ignore MaterialMode — Mica/Acrylic on the HWND
        // would recreate the rectangular frame (bug a). Enum kept for LocalSettings JSON.

        Pill.Background = new SolidColorBrush(Color.FromArgb(a, bg.R, bg.G, bg.B));
        var keyline = KeylineBrush(border);
        var stroke = Math.Clamp(_settings.BorderThickness, 0.5, 4);
        Pill.BorderBrush = keyline;
        Pill.BorderThickness = new Thickness(stroke);
        GlowBorder.BorderBrush = keyline;
        GlowBorder.BorderThickness = new Thickness(stroke + 1);
        MinuteArc.Foreground = new SolidColorBrush(accent);
        Badge.Background = new SolidColorBrush(accent);

        var filled = _settings.UnreadCount > 0 || _settings.DoNotDisturb;
        StateIcon.Glyph = _settings.DoNotDisturb ? "\uE708" : "\uEA8F";
        StateIcon.FontWeight = _settings.IconSet == IconSet.Thin
            ? FontWeights.ExtraLight
            : FontWeights.Normal;
        StateIcon.Foreground = filled ? new SolidColorBrush(accent) : new SolidColorBrush(ContentFg());
        WeatherGlyph.Visibility = WeatherGlyphExp.Visibility = StateIcon.Visibility = Visibility.Collapsed;
        SetPackImage(StateIconImage, IconPackTheme.StatusUri(
            _settings.IconPack, _settings.DoNotDisturb, _settings.UnreadCount > 0));

        Dot.Visibility = Visibility.Collapsed;
        PaintAppIcons();
        var activity = HasAppToasts();
        Badge.Visibility = activity ? Visibility.Visible : Visibility.Collapsed;
        _settings.SyncDerived();
        BuildUnreadAnim();
        if (activity)
        {
            BadgeText.Text = _settings.UnreadCount > 9 ? "9+" : _settings.UnreadCount.ToString();
            if (_settings.UnreadAnim != UnreadAnimation.Off) _pulse?.Begin();
            else StopPulse();
        }
        else
        {
            StopPulse();
        }
        if (!activity || _settings.UnreadAnim is UnreadAnimation.Off or UnreadAnimation.SoftBounce)
            GlowBorder.Opacity = _settings.BorderGlow ? _settings.IslandGlowIntensity : 0;

        ApplyClockStyle();
        ApplyWeather();
        ApplyContentContrast();
        PaintAppIcons();
        ApplyPresentation();
        _settings.Save();
    }

    private void StopPulse()
    {
        _pulse?.Stop();
        PillScale.ScaleX = PillScale.ScaleY = 1;
        PillNudge.Y = 0;
        Pill.Opacity = 1;
        GlowBorder.Opacity = _settings.BorderGlow ? _settings.IslandGlowIntensity : 0;
    }

    private void ApplyClockStyle()
    {
        ApplyPackClockLook();
        var now = DateTime.Now;
        MinuteArc.Visibility = _settings.ClockStyle == ClockStyle.SecondsMinuteArc ? Visibility.Visible : Visibility.Collapsed;
        Tabular(ClockText, WeatherTemp, WeatherFeels, WeatherRange, BadgeText);
        // HH:MM:SS in expanded = WinUI addition (not Packt Live Widget).
        if (_settings.ClockStyle == ClockStyle.SecondsMinuteArc || IsExpanded())
        {
            SetClockParts(now.ToString("HH"), ":", now.ToString("mm"), ":", now.ToString("ss"));
            if (_settings.ClockStyle == ClockStyle.SecondsMinuteArc) MinuteArc.Value = now.Second;
            PaintClockGlow($"{now:HH}:{now:mm}:{now:ss}");
        }
        else
        {
            var sep = _settings.ClockStyle == ClockStyle.DigitalModern && !_colonOn ? "\u2007" : ":";
            SetClockParts(now.ToString("HH"), sep, now.ToString("mm"));
            PaintClockGlow($"{now:HH}{sep}{now:mm}");
        }
        ApplyContentContrast();
    }

    private void ApplyPackClockLook()
    {
        var look = IconPackTheme.Clock(_settings.IconPack);
        ClockText.FontFamily = new FontFamily(look.FontFamily);
        ClockText.FontSize = 12;
        ClockText.FontWeight = look.FontWeight switch
        {
            "Bold" => FontWeights.Bold,
            "Normal" => FontWeights.Normal,
            "SemiLight" => FontWeights.SemiLight,
            _ => FontWeights.SemiBold
        };
        ClockText.Foreground = new SolidColorBrush(ParseColor(look.ForegroundHex));
        ClockText.CharacterSpacing = look.CharacterSpacing;
        Typography.SetNumeralAlignment(ClockText,
            look.TabularNums ? FontNumeralAlignment.Tabular : FontNumeralAlignment.Normal);

        if (look.NeonShadow)
        {
            try
            {
                _clockNeonShadow ??= new ThemeShadow();
                if (_clockNeonShadow.Receivers.Count == 0)
                    _clockNeonShadow.Receivers.Add(GlowBorder);
                ClockText.Shadow = _clockNeonShadow;
                ClockText.Translation = new Vector3(0, 0, 8);
            }
            catch
            {
                ClockText.Shadow = null;
            }
        }
        else
        {
            ClockText.Shadow = null;
            ClockText.Translation = Vector3.Zero;
        }
    }

    private void SetClockParts(string left, string sep, string mid, string? sep2 = null, string? right = null)
    {
        var look = IconPackTheme.Clock(_settings.IconPack);
        if (!look.AmberColon)
        {
            ClockText.Text = sep2 is null ? left + sep + mid : left + sep + mid + sep2 + right;
            return;
        }

        var amber = new SolidColorBrush(ParseColor(_settings.AccentHex));
        ClockText.Inlines.Clear();
        ClockText.Inlines.Add(new Run { Text = left });
        ClockText.Inlines.Add(AmberRun(sep, amber));
        ClockText.Inlines.Add(new Run { Text = mid });
        if (sep2 is null) return;
        ClockText.Inlines.Add(AmberRun(sep2, amber));
        ClockText.Inlines.Add(new Run { Text = right ?? "" });
    }

    private static Run AmberRun(string sep, Brush amber)
    {
        var run = new Run { Text = sep };
        if (sep == ":") run.Foreground = amber;
        return run;
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
        var pack = _settings.IconPack;
        SetPackImage(WeatherGlyphImage, IconPackTheme.WeatherUri(pack, "sun"));
        SetPackImage(WeatherGlyphExpImage, IconPackTheme.WeatherUri(pack, "sun"));
        var content = new SolidColorBrush(ParseColor(IconPackTheme.ContentForegroundHex(pack)));
        WeatherTemp.Foreground = content;
        WeatherDesc.Foreground = content;
        var muted = new SolidColorBrush(ParseColor(IconPackTheme.MutedForegroundHex(pack)));
        WeatherFeels.Foreground = muted;
        WeatherRange.Foreground = muted;
        WeatherDesc.Text = "Clear";
        WeatherFeels.Text = "feels " + Unit(feels);
        WeatherRange.Text = Unit(min) + " / " + Unit(max);
    }

    private void ApplyPresentation()
    {
        var minimal = _settings.Presentation == PresentationMode.Minimal;
        CompactTrailing.Visibility = minimal ? Visibility.Collapsed : Visibility.Visible;
        ExpandedRegion.IsHitTestVisible = ShowExpandedExtras();
        MorphTo();
    }

    private void BuildMorph()
    {
        _morph = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(MorphMs));
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
        _morphW = MorphAnim(Pill, "Width", duration, ease);
        _morphH = MorphAnim(Pill, "Height", duration, ease);
        _morphOp = MorphAnim(ExpandedRegion, "Opacity", duration, ease);
        _morph.Children.Add(_morphW);
        _morph.Children.Add(_morphH);
        _morph.Children.Add(_morphOp);
        _morph.Completed += Morph_Completed;
    }

    private static DoubleAnimation MorphAnim(DependencyObject target, string prop, Duration duration, EasingFunctionBase ease)
    {
        var a = new DoubleAnimation { Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(a, target);
        Storyboard.SetTargetProperty(a, prop);
        return a;
    }

    private void Morph_Completed(object? sender, object e)
    {
        ExpandedRegion.IsHitTestVisible = ShowExpandedExtras();
    }

    private void MorphTo()
    {
        var (w, h) = PillSize();
        _morphW!.To = w;
        _morphH!.To = h;
        _morphOp!.To = ShowExpandedExtras() ? 1 : 0;
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

    private void BuildUnreadAnim()
    {
        _pulse?.Stop();
        if (_settings.UnreadAnim == UnreadAnimation.Off)
        {
            _pulse = null;
            return;
        }
        _pulse = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        var d = TimeSpan.FromMilliseconds(700);
        switch (_settings.UnreadAnim)
        {
            case UnreadAnimation.Glow:
                Anim(_pulse, GlowBorder, "Opacity", GlowFloor(), GlowPeak(), d);
                break;
            case UnreadAnimation.SoftBounce:
                var bounce = TimeSpan.FromMilliseconds(520);
                Anim(_pulse, PillNudge, "Y", 0, -3.5, bounce, new QuadraticEase { EasingMode = EasingMode.EaseInOut });
                Anim(_pulse, PillScale, "ScaleX", 1, 1.04, bounce, new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut });
                Anim(_pulse, PillScale, "ScaleY", 1, 1.04, bounce, new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut });
                break;
            default:
                Anim(_pulse, PillScale, "ScaleX", 1, 1.06, d);
                Anim(_pulse, PillScale, "ScaleY", 1, 1.06, d);
                Anim(_pulse, Pill, "Opacity", 1, 0.78, d);
                Anim(_pulse, GlowBorder, "Opacity", GlowFloor(), GlowPeak(), d);
                break;
        }
    }

    private double GlowPeak() => Math.Clamp(Math.Max(_settings.IslandGlowIntensity, 0.85), 0.4, 1);

    private double GlowFloor() =>
        _settings.IslandGlowIntensity > 0.02
            ? Math.Clamp(_settings.IslandGlowIntensity * 0.4, 0.08, 0.55)
            : 0.18;

    private static void Anim(Storyboard board, DependencyObject target, string prop, double from, double to, TimeSpan d, EasingFunctionBase? ease = null)
    {
        var a = new DoubleAnimation { From = from, To = to, Duration = d, AutoReverse = true, EasingFunction = ease };
        Storyboard.SetTarget(a, target);
        Storyboard.SetTargetProperty(a, prop);
        board.Children.Add(a);
    }

    private void PaintClockGlow(string text)
    {
        ClockGlowText.Text = text;
        ClockGlowText.FontFamily = ClockText.FontFamily;
        ClockGlowText.FontWeight = ClockText.FontWeight;
        var glow = ParseColor(_settings.AccentHex);
        ClockGlowText.Foreground = new SolidColorBrush(Color.FromArgb(200, glow.R, glow.G, glow.B));
        ClockGlowText.Opacity = _settings.ClockGlow ? _settings.ClockGlowIntensity : 0;
    }

    private bool LightPill()
    {
        var c = ParseColor(_settings.BackgroundHex);
        return 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B > 140;
    }

    private Color ContentFg() => LightPill()
        ? Color.FromArgb(255, 28, 28, 30)
        : Color.FromArgb(255, 245, 245, 247);

    private Color MutedFg() => LightPill()
        ? Color.FromArgb(255, 110, 110, 115)
        : Color.FromArgb(255, 160, 160, 168);

    private void ApplyContentContrast()
    {
        if (!_settings.CustomPalette) return;
        var fg = new SolidColorBrush(ContentFg());
        var muted = new SolidColorBrush(MutedFg());
        ClockText.Foreground = fg;
        WeatherTemp.Foreground = fg;
        WeatherDesc.Foreground = fg;
        WeatherFeels.Foreground = muted;
        WeatherRange.Foreground = muted;
    }

    private void PaintAppIcons()
    {
        AppIcons.Children.Clear();
        foreach (var app in AppNotificationHub.Snapshot())
        {
            var chip = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 1, 6, 1),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Tag = app
            };
            chip.Child = new TextBlock
            {
                Text = $"{app.Name.Trim()[..Math.Min(3, app.Name.Trim().Length)]} {app.Count}",
                FontSize = 10,
                Foreground = ClockText.Foreground
            };
            chip.Tapped += (_, e) =>
            {
                OpenNotificationCenter();
                e.Handled = true;
            };
            AppIcons.Children.Add(chip);
        }
        AppIcons.Visibility = AppIcons.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void SetPackImage(Image image, string uri)
    {
        if (image.Source is BitmapImage existing && existing.UriSource?.OriginalString == uri)
            return;
        image.Source = new BitmapImage(new Uri(uri));
    }

    private static void Tabular(params TextBlock[] blocks)
    {
        foreach (var t in blocks)
            Typography.SetNumeralAlignment(t, FontNumeralAlignment.Tabular);
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
        if (_menuOpen) return;
        ApplyClockStyle();
        ApplyPresentation();
    }

    private void Pill_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var menu = new MenuFlyout();
        var settingsItem = new MenuFlyoutItem { Text = "Настройки…" };
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);
        menu.Items.Add(new MenuFlyoutSeparator());
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
        glow.Click += (_, _) =>
        {
            _settings.BorderGlow = glow.IsChecked;
            _settings.IslandGlowIntensity = glow.IsChecked ? Math.Max(_settings.IslandGlowIntensity, 0.85) : 0;
            ApplySettings();
        };
        menu.Items.Add(glow);
        var pulse = new ToggleMenuFlyoutItem { Text = "Pulse Aura", IsChecked = _settings.PulseAura };
        pulse.Click += (_, _) =>
        {
            _settings.UnreadAnim = pulse.IsChecked ? UnreadAnimation.Pulse : UnreadAnimation.Off;
            ApplySettings();
        };
        menu.Items.Add(pulse);
        menu.Items.Add(new MenuFlyoutSeparator());
        AddEnum("Presentation", _settings.Presentation, v => _settings.Presentation = v);
        AddEnum("Clock", _settings.ClockStyle, v => _settings.ClockStyle = v);
        AddEnum("Weather", _settings.WeatherMode, v => _settings.WeatherMode = v);
        AddEnum("Icons", _settings.IconSet, v => _settings.IconSet = v);
        AddEnum("Icon Pack", _settings.IconPack, v => { _settings.IconPack = v; _settings.CustomPalette = false; });
        AddEnum("Temp", _settings.TempUnit, v => _settings.TempUnit = v);
        // MaterialMode is unused for HWND (TransparentBackdrop only) — omit from menu.
        menu.Items.Add(new MenuFlyoutSeparator());
        var demo = new MenuFlyoutItem { Text = "Demo +1 unread" };
        demo.Click += (_, _) =>
        {
            var names = new[] { "Telegram", "Discord", "Mail" };
            AppNotificationHub.Bump(names[_settings.UnreadCount % names.Length]);
            _settings.UnreadCount = Math.Min(99, AppNotificationHub.Total);
            ApplySettings();
        };
        menu.Items.Add(demo);
        var clear = new MenuFlyoutItem { Text = "Clear unread" };
        clear.Click += (_, _) =>
        {
            AppNotificationHub.Clear();
            _settings.UnreadCount = 0;
            ApplySettings();
        };
        menu.Items.Add(clear);

        _menuOpen = true;
        ApplyClockStyle();
        ApplyPresentation();
        void OnClosed(object? s, object ev)
        {
            menu.Closed -= OnClosed;
            _menuOpen = false;
            ApplyClockStyle();
            ApplyPresentation();
        }
        menu.Closed += OnClosed;
        menu.ShowAt(Pill, e.GetPosition(Pill));
        e.Handled = true;
    }

    private void OpenSettings()
    {
        if (_settingsUi is null)
        {
            _settingsUi = new SettingsWindow(_settings, ApplySettings);
            _settingsUi.Closed += (_, _) => _settingsUi = null;
        }
        _settingsUi.Activate();
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
