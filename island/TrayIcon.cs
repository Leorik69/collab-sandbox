using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace NotifyIsland;

/// <summary>Unpackaged Win32 tray. Left click opens settings; right click is a tiny Win32 menu.</summary>
internal sealed class TrayIcon : IDisposable
{
    private const uint NimAdd = 0x00000000, NimDelete = 0x00000002, NimModify = 0x00000001;
    private const uint NifMessage = 0x01, NifIcon = 0x02, NifTip = 0x04;
    private const uint WmApp = 0x8000, WmLbuttonup = 0x0202, WmRbuttonup = 0x0205, WmDestroy = 0x0002;
    private const uint MfString = 0, MfSeparator = 0x800;
    private const int CmdSettings = 1, CmdShow = 2, CmdExit = 3;
    private const int IdiApplication = 32512;

    private readonly IntPtr _hwnd;
    private readonly NativeWindow _wnd;
    private bool _added;
    private readonly Action _openSettings, _showIsland, _exit;

    public TrayIcon(IntPtr islandHwnd, Action openSettings, Action showIsland, Action exit)
    {
        _openSettings = openSettings;
        _showIsland = showIsland;
        _exit = exit;
        _wnd = new NativeWindow(WndProc);
        _hwnd = _wnd.Handle;
        _ = islandHwnd;
        Add();
    }

    private void Add()
    {
        var data = Data(NimAdd);
        _added = Shell_NotifyIcon(NimAdd, ref data);
        if (!_added)
        {
            data = Data(NimModify);
            _added = Shell_NotifyIcon(NimAdd, ref data);
        }
    }

    private NotifyIconData Data(uint _)
    {
        var d = new NotifyIconData
        {
            cbSize = Marshal.SizeOf<NotifyIconData>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NifMessage | NifIcon | NifTip,
            uCallbackMessage = WmApp,
            hIcon = LoadIcon(IntPtr.Zero, (IntPtr)IdiApplication),
            szTip = "NotifyIsland"
        };
        return d;
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WmApp)
        {
            var ev = (uint)lParam.ToInt64() & 0xFFFF;
            if (ev == WmLbuttonup) Application.Current.DispatcherQueue.TryEnqueue(() => _openSettings());
            else if (ev == WmRbuttonup) ShowMenu();
            return IntPtr.Zero;
        }
        if (msg == WmDestroy) Remove();
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void ShowMenu()
    {
        var menu = CreatePopupMenu();
        AppendMenu(menu, MfString, (UIntPtr)CmdSettings, "Настройки");
        AppendMenu(menu, MfString, (UIntPtr)CmdShow, "Показать остров");
        AppendMenu(menu, MfSeparator, UIntPtr.Zero, null);
        AppendMenu(menu, MfString, (UIntPtr)CmdExit, "Выход");
        GetCursorPos(out var pt);
        SetForegroundWindow(_hwnd);
        var cmd = (int)TrackPopupMenu(menu, 0x0100, pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);
        DestroyMenu(menu);
        Application.Current.DispatcherQueue.TryEnqueue(() =>
        {
            if (cmd == CmdSettings) _openSettings();
            else if (cmd == CmdShow) _showIsland();
            else if (cmd == CmdExit) _exit();
        });
    }

    private void Remove()
    {
        if (!_added) return;
        var d = Data(NimDelete);
        Shell_NotifyIcon(NimDelete, ref d);
        _added = false;
    }

    public void Dispose()
    {
        Remove();
        _wnd.Dispose();
    }

    private sealed class NativeWindow : IDisposable
    {
        private readonly WndProcDelegate _proc;
        public IntPtr Handle { get; }
        private readonly ushort _atom;
        private bool _disposed;

        public NativeWindow(WndProcDelegate proc)
        {
            _proc = proc;
            var name = "NotifyIslandTray" + Guid.NewGuid().ToString("N");
            var wc = new WndClassEx
            {
                cbSize = (uint)Marshal.SizeOf<WndClassEx>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_proc),
                hInstance = GetModuleHandle(null),
                lpszClassName = name
            };
            _atom = RegisterClassEx(ref wc);
            Handle = CreateWindowEx(0, name, "", 0, 0, 0, 0, 0, (IntPtr)(-3), IntPtr.Zero, wc.hInstance, IntPtr.Zero);
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (Handle != IntPtr.Zero) DestroyWindow(Handle);
            _disposed = true;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID, uFlags, uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClassEx
    {
        public uint cbSize, style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground, lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X, Y; }

    internal delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpData);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);
    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WndClassEx lpwcx);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string? lpNewItem);
    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);
    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point lpPoint);
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
