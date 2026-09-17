using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace NotifyIsland;

public partial class App : Application
{
    public static bool DemoMode { get; private set; }

    private Window? _window;

    public App()
    {
        foreach (var a in Environment.GetCommandLineArgs())
        {
            if (string.Equals(a, "--demo", StringComparison.OrdinalIgnoreCase))
                DemoMode = true;
        }
        UnhandledException += (_, e) =>
        {
            Log("xaml", e.Exception);
            e.Handled = true;
            Alert(e.Exception);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log("domain", e.ExceptionObject as Exception);

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Log("init", ex);
            Alert(ex);
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            Log("launch", ex);
            Alert(ex);
        }
    }

    private static void Log(string where, Exception? ex)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.AppendAllText(path, $"[{DateTime.Now:O}] {where}\n{ex}\n\n");
        }
        catch
        {
            // ignore
        }
    }

    private static void Alert(Exception? ex)
    {
        try
        {
            MessageBoxW(IntPtr.Zero, ex?.ToString() ?? "unknown", "NotifyIsland crash", 0x00000010);
        }
        catch
        {
            // ignore
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
