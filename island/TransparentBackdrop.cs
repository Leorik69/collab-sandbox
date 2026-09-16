using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Composition; // ICompositionSupportsSystemBackdrop
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WUC = Windows.UI.Composition;

namespace NotifyIsland;

/// <summary>Per-pixel transparent HWND so only the capsule paints. No rectangular host fill.</summary>
internal sealed class TransparentBackdrop : SystemBackdrop
{
    internal const int DwmwaWindowCornerPreference = 33;
    internal const int DwmwaBorderColor = 34;
    internal const int DwmwaSystemBackdropType = 38;
    internal const uint DwmwaColorNone = 0xFFFFFFFE;
    internal const uint DwmwcpDonotround = 1;
    internal const uint DwmsbtNone = 1;

    private static readonly UIntPtr SubclassId = (UIntPtr)0x4E49534C; // NISL
    private const uint WmErasebkgnd = 0x0014;
    private const uint WmDwmcompositionchanged = 0x031E;
    private const uint DwmBbEnable = 0x1;
    private const uint DwmBbBlurregion = 0x2;

    private SubclassProc? _proc;
    private IntPtr _hwnd;
    private IntPtr _blackBrush;
    private WUC.Compositor? _wuc;
    private WUC.CompositionColorBrush? _tint;

    private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData);

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        // ICompositionSupportsSystemBackdrop.SystemBackdrop is Windows.UI.Composition
        // (Microsoft.UI.Composition.Compositor.CreateColorBrush is CS0029).
        _wuc ??= new WUC.Compositor();
        _tint = _wuc.CreateColorBrush(Color.FromArgb(0, 0, 0, 0));
        connectedTarget.SystemBackdrop = _tint;

        // HWND chrome + subclass: AttachHwnd (Win32 handle from WindowNative).
        // AppWindowId.Value is not an HWND — do not cast it.
        base.OnTargetConnected(connectedTarget, xamlRoot);
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        disconnectedTarget.SystemBackdrop = null;
        _tint?.Dispose();
        _tint = null;
        Unhook();
        if (_blackBrush != IntPtr.Zero)
        {
            DeleteObject(_blackBrush);
            _blackBrush = IntPtr.Zero;
        }
        base.OnTargetDisconnected(disconnectedTarget);
    }

    internal static void StripChrome(IntPtr hwnd)
    {
        uint none = DwmwaColorNone;
        DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref none, sizeof(uint));
        uint corners = DwmwcpDonotround;
        DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref corners, sizeof(uint));
        uint backdrop = DwmsbtNone;
        DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref backdrop, sizeof(uint));
        ConfigureDwm(hwnd);
    }

    internal static void ConfigureDwm(IntPtr hwnd)
    {
        var margins = new Margins();
        DwmExtendFrameIntoClientArea(hwnd, ref margins);
        var rgn = CreateRectRgn(-2, -2, -1, -1);
        var bb = new DwmBlurBehind
        {
            dwFlags = DwmBbEnable | DwmBbBlurregion,
            fEnable = true,
            hRgnBlur = rgn,
            fTransitionOnMaximized = false
        };
        DwmEnableBlurBehindWindow(hwnd, ref bb);
        if (rgn != IntPtr.Zero) DeleteObject(rgn);
    }

    /// <summary>Owns HWND chrome and WM_ERASEBKGND hook. hwnd from WindowNative.GetWindowHandle.</summary>
    internal void AttachHwnd(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        if (_hwnd != IntPtr.Zero && _hwnd != hwnd)
            Unhook();
        _hwnd = hwnd;
        StripChrome(hwnd); // DWM attributes + ConfigureDwm (blur-behind 1px) — single setup path
        Hook(hwnd);
        var hdc = GetDC(hwnd);
        ClearBackground(hwnd, hdc);
        if (hdc != IntPtr.Zero) ReleaseDC(hwnd, hdc);
    }

    private void Hook(IntPtr hwnd)
    {
        if (_proc is not null) return;
        _proc = SubclassWndProc;
        SetWindowSubclass(hwnd, _proc, SubclassId, UIntPtr.Zero);
    }

    private void Unhook()
    {
        if (_hwnd != IntPtr.Zero && _proc is not null)
            RemoveWindowSubclass(_hwnd, _proc, SubclassId);
        _proc = null;
        _hwnd = IntPtr.Zero;
    }

    private IntPtr SubclassWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data)
    {
        if (msg == WmErasebkgnd)
        {
            ClearBackground(hWnd, wParam);
            return (IntPtr)1;
        }
        if (msg == WmDwmcompositionchanged)
            ConfigureDwm(hWnd);
        return DefSubclassProc(hWnd, msg, wParam, lParam);
    }

    private void ClearBackground(IntPtr hwnd, IntPtr hdc)
    {
        if (hdc == IntPtr.Zero) return;
        if (!GetClientRect(hwnd, out var rect)) return;
        if (_blackBrush == IntPtr.Zero)
            _blackBrush = CreateSolidBrush(0);
        FillRect(hdc, ref rect, _blackBrush);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DwmBlurBehind
    {
        public uint dwFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fEnable;
        public IntPtr hRgnBlur;
        [MarshalAs(UnmanagedType.Bool)] public bool fTransitionOnMaximized;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);

    [DllImport("dwmapi.dll")]
    private static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref DwmBlurBehind pBlurBehind);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref uint attrValue, int attrSize);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(int color);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll")]
    private static extern int FillRect(IntPtr hDC, ref NativeRect lprc, IntPtr hbr);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("comctl32.dll", ExactSpelling = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass, UIntPtr dwRefData);

    [DllImport("comctl32.dll", ExactSpelling = true)]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass);

    [DllImport("comctl32.dll", ExactSpelling = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
}
