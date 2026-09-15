using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
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

    private IslandSettings _settings = IslandSettings.Load();
    private Storyboard? _pulse;
    private AppWindow? _appWindow;
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _colonOn = true;
    private bool _hovering;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true; // no SetTitleBar(Pill)
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

    private void Reposition()
    {
        if (_appWindow is null) return;
        var wa = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var w = Math.Clamp(_settings.Width, 160, 320);
        var h = Math.Clamp(_settings.Height, 32, 56);
        _appWindow.MoveAndResize(new RectInt32(wa.X + (wa.Width - w) / 2, wa.Y + 6, w, h));
        Pill.Width = Math.Max(120, w - 16);
        Pill.Height = Math.Max(24, h - 8);
        GlowBorder.Width = Pill.Width + 8;
        GlowBorder.Height = Pill.Height + 4;
        GlowBorder.CornerRadius = new CornerRadius(Pill.Height / 2 + 2);
        if (Pill.RenderTransform is ScaleTransform st)
        {
            st.CenterX = Pill.Width / 2;
            st.CenterY = Pill.Height / 2;
        }
    }

    private void ApplySettings()
    {
        if (_settings.FollowSystemTheme)
        {
            var light = Application.Current.RequestedTheme == ApplicationTheme.Light;
            _settings.BackgroundHex = light ? "#F2F2F7" : "#1C1C1E";
        }

        var radius = _settings.Shape switch
        {
            IslandShape.Oval => Pill.Height / 2,
            IslandShape.SoftRect => Math.Min(_settings.CornerRadius, 8),
            _ => _settings.CornerRadius
        };
        Pill.CornerRadius = new CornerRadius(radius);

        var bg = ParseColor(_settings.BackgroundHex);
        var border = ParseColor(_settings.BorderHex);
        var accent = ParseColor(_settings.AccentHex);
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
        Pill.BorderBrush = new SolidColorBrush(border);

        GlowBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(140, accent.R, accent.G, accent.B));
        GlowBorder.Opacity = _settings.BorderGlow ? 0.85 : 0;

        var filled = _settings.UnreadCount > 0 || _settings.DoNotDisturb; // I3 overlay
        ApplyStateIcon(filled, accent);

        Badge.Background = new SolidColorBrush(accent);
        if (_settings.UnreadCount > 0 && !_settings.DoNotDisturb)
        {
            Badge.Visibility = Visibility.Visible;
            BadgeText.Text = _settings.UnreadCount > 9 ? "9+" : _settings.UnreadCount.ToString();
            Dot.Visibility = Visibility.Collapsed;
            if (_settings.PulseAura) _pulse?.Begin();
            else { _pulse?.Stop(); PillScale.ScaleX = PillScale.ScaleY = 1; }
        }
        else
        {
            Badge.Visibility = Visibility.Collapsed;
            Dot.Visibility = Visibility.Visible;
            Dot.Fill = new SolidColorBrush(_settings.DoNotDisturb ? Color.FromArgb(200, 180, 180, 180) : accent);
            _pulse?.Stop();
            if (!_hovering) PillScale.ScaleX = PillScale.ScaleY = 1;
        }

        ApplyClockStyle();
        ApplyWeather();
        Reposition();
        _settings.Save();
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
            : Color.FromArgb(255, 230, 230, 235));
        if (_settings.ClockStyle == ClockStyle.SecondsMinuteArc)
        {
            ClockText.Text = now.ToString("HH:mm:ss");
            MinuteArc.Value = now.Second;
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
        // mock: 18°C Clear, feels 16, 12–21
        int c = 18, feels = 16, min = 12, max = 21;
        string Unit(int t) => _settings.TempUnit == TempUnit.Fahrenheit ? $"{t * 9 / 5 + 32}°F" : $"{t}°";
        WeatherTemp.Text = Unit(c);
        WeatherDesc.Text = "Clear";
        WeatherFeels.Text = "feels " + Unit(feels);
        WeatherRange.Text = Unit(min) + " / " + Unit(max);
        ApplyWeatherIcon("sun");
        ShowExpanded(_hovering && _settings.WeatherMode == WeatherMode.ExpandOnHover);
    }

    private bool UseAssets => _settings.IconSource == IconSourceMode.LocalAsset;

    private static Microsoft.UI.Xaml.Media.Imaging.BitmapImage AssetImage(string relative)
    {
        return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/" + relative));
    }

    private void ApplyWeatherIcon(string condition)
    {
        if (UseAssets)
        {
            WeatherGlyph.Visibility = WeatherGlyphExp.Visibility = Visibility.Collapsed;
            WeatherImage.Visibility = WeatherImageExp.Visibility = Visibility.Visible;
            try
            {
                WeatherImage.Source = AssetImage($"Weather/{condition}.png");
                WeatherImageExp.Source = AssetImage($"Weather/{condition}.png");
            }
            catch { }
        }
        else
        {
            WeatherGlyph.Visibility = WeatherGlyphExp.Visibility = Visibility.Visible;
            WeatherImage.Visibility = WeatherImageExp.Visibility = Visibility.Collapsed;
            WeatherGlyph.Glyph = WeatherGlyphExp.Glyph = "\uE706";
        }
    }

    private void ApplyStateIcon(bool filled, Color accent)
    {
        if (UseAssets)
        {
            StateIcon.Visibility = Visibility.Collapsed;
            StateImage.Visibility = Visibility.Visible;
            try
            {
                var name = _settings.DoNotDisturb ? "dnd-active"
                    : _settings.UnreadCount > 0 ? "bell-unread" : "bell-none";
                StateImage.Source = AssetImage($"Statuses/{name}.png");
            }
            catch { }
        }
        else
        {
            StateImage.Visibility = Visibility.Collapsed;
            StateIcon.Visibility = Visibility.Visible;
            StateIcon.Glyph = _settings.DoNotDisturb ? "\uE708" : "\uEA8F";
            StateIcon.FontWeight = _settings.IconSet == IconSet.Thin ? Microsoft.UI.Text.FontWeights.ExtraLight : Microsoft.UI.Text.FontWeights.Normal;
            StateIcon.Foreground = new SolidColorBrush(filled ? accent : Colors.White);
        }
    }

    private void ShowExpanded(bool on)
    {
        WeatherExpanded.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        MainRow.Opacity = on ? 0 : 1;
    }

    private void BuildPulse()
    {
        _pulse = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        var sx = new DoubleAnimation { From = 1, To = 1.08, Duration = TimeSpan.FromMilliseconds(700), AutoReverse = true };
        var sy = new DoubleAnimation { From = 1, To = 1.08, Duration = TimeSpan.FromMilliseconds(700), AutoReverse = true };
        Storyboard.SetTarget(sx, PillScale);
        Storyboard.SetTargetProperty(sx, "ScaleX");
        Storyboard.SetTarget(sy, PillScale);
        Storyboard.SetTargetProperty(sy, "ScaleY");
        _pulse.Children.Add(sx);
        _pulse.Children.Add(sy);
    }

    private static Color ParseColor(string hex)
    {
        hex = (hex ?? "#1C1C1E").Trim().TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        if (hex.Length != 8) return Color.FromArgb(255, 28, 28, 30);
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
        _hovering = true;
        if (_pulse?.GetCurrentState() != ClockState.Active)
        {
            PillScale.ScaleX = 1.06;
            PillScale.ScaleY = 1.06;
        }
        ApplyWeather();
    }

    private void Pill_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _hovering = false;
        if (_pulse?.GetCurrentState() != ClockState.Active)
        {
            PillScale.ScaleX = 1;
            PillScale.ScaleY = 1;
        }
        ApplyWeather();
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
        AddEnum("Clock", _settings.ClockStyle, v => _settings.ClockStyle = v);
        AddEnum("Weather", _settings.WeatherMode, v => _settings.WeatherMode = v);
        AddEnum("Icons", _settings.IconSet, v => _settings.IconSet = v);
        AddEnum("Icon source", _settings.IconSource, v => _settings.IconSource = v);
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
