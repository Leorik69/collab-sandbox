using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Composition;
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

    public MainWindow()
    {
        InitializeComponent();
        // Do NOT SetTitleBar(Pill) — that swallows LMB (SL nit). Click = NC; drag via window chrome free.
        ExtendsContentIntoTitleBar = true;
        SetupOverlay();
        ApplySettings();
        BuildPulse();
        Closed += (_, _) => _settings.Save();
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
        var windowId = _appWindow.Id;
        var display = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var wa = display.WorkArea;
        var w = Math.Clamp(_settings.Width, 120, 280);
        var h = Math.Clamp(_settings.Height, 28, 64);
        var x = wa.X + (wa.Width - w) / 2;
        var y = wa.Y + 6;
        _appWindow.MoveAndResize(new RectInt32(x, y, w, h));
        Pill.Width = Math.Max(80, w - 8);
        Pill.Height = Math.Max(24, h - 8);
        if (Pill.RenderTransform is ScaleTransform st)
        {
            st.CenterX = Pill.Width / 2;
            st.CenterY = Pill.Height / 2;
        }
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

        var bg = ParseColor(_settings.BackgroundHex);
        var border = ParseColor(_settings.BorderHex);
        var accent = ParseColor(_settings.AccentHex);

        // Material: Acrylic/Mica best-effort; Solid always works
        try
        {
            if (_settings.Material == MaterialMode.Mica)
            {
                SystemBackdrop = new MicaBackdrop();
                Pill.Background = new SolidColorBrush(Color.FromArgb(
                    (byte)(255 * Math.Clamp(_settings.Opacity, 0.2, 1.0)), bg.R, bg.G, bg.B));
            }
            else if (_settings.Material == MaterialMode.Acrylic)
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
                Pill.Background = new SolidColorBrush(Color.FromArgb(
                    (byte)(255 * Math.Clamp(_settings.Opacity, 0.2, 1.0)), bg.R, bg.G, bg.B));
            }
            else
            {
                SystemBackdrop = null;
                Pill.Background = new SolidColorBrush(Color.FromArgb(
                    (byte)(255 * Math.Clamp(_settings.Opacity, 0.2, 1.0)), bg.R, bg.G, bg.B));
            }
        }
        catch
        {
            SystemBackdrop = null;
            Pill.Background = new SolidColorBrush(Color.FromArgb(
                (byte)(255 * Math.Clamp(_settings.Opacity, 0.2, 1.0)), bg.R, bg.G, bg.B));
        }

        Pill.BorderBrush = new SolidColorBrush(border);
        Dot.Fill = new SolidColorBrush(_settings.DoNotDisturb
            ? Color.FromArgb(200, 180, 180, 180)
            : accent);
        Badge.Background = new SolidColorBrush(accent);

        StateIcon.Glyph = _settings.DoNotDisturb ? "\uE708" : "\uEA8F"; // Mute / Notification
        StateIcon.Foreground = new SolidColorBrush(Colors.White);

        if (_settings.UnreadCount > 0 && !_settings.DoNotDisturb)
        {
            Badge.Visibility = Visibility.Visible;
            BadgeText.Text = _settings.UnreadCount > 9 ? "9+" : _settings.UnreadCount.ToString();
            Dot.Visibility = Visibility.Collapsed;
            if (_settings.PulseOnUnread) _pulse?.Begin();
            else _pulse?.Stop();
        }
        else
        {
            Badge.Visibility = Visibility.Collapsed;
            Dot.Visibility = Visibility.Visible;
            _pulse?.Stop();
            Dot.Opacity = 1;
        }

        Reposition();
        _settings.Save();
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
            // Demo: bump unread pulse feedback
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
        PillScale.ScaleX = 1.06;
        PillScale.ScaleY = 1.06;
    }

    private void Pill_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        PillScale.ScaleX = 1.0;
        PillScale.ScaleY = 1.0;
    }

    private void Pill_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var menu = new MenuFlyout();

        var dnd = new ToggleMenuFlyoutItem { Text = "Не беспокоить (DND)", IsChecked = _settings.DoNotDisturb };
        dnd.Click += (_, _) => { _settings.DoNotDisturb = dnd.IsChecked; ApplySettings(); };
        menu.Items.Add(dnd);

        var clear = new MenuFlyoutItem { Text = "Сбросить счётчик" };
        clear.Click += (_, _) => { _settings.UnreadCount = 0; ApplySettings(); };
        menu.Items.Add(clear);

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
        var demo = new MenuFlyoutItem { Text = "Демо: +1 уведомление" };
        demo.Click += (_, _) => { _settings.UnreadCount = Math.Min(99, _settings.UnreadCount + 1); ApplySettings(); };
        menu.Items.Add(demo);

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
