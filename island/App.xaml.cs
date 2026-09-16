using System;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace NotifyIsland;

public partial class App : Application
{
    public static MainWindow? Island { get; private set; }
    public static IslandSettings Settings { get; private set; } = IslandSettings.Load();

    private SettingsWindow? _settingsWin;
    private TrayIcon? _tray;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Settings = IslandSettings.Load();
        Island = new MainWindow();
        Island.Activate();
        var hwnd = WindowNative.GetWindowHandle(Island);
        _tray = new TrayIcon(hwnd, OpenSettings, () => Island?.Activate(), Quit);
        Island.Closed += (_, _) => { _tray?.Dispose(); _settingsWin?.Close(); };
    }

    public void OpenSettings()
    {
        if (_settingsWin is null)
        {
            _settingsWin = new SettingsWindow();
            _settingsWin.Closed += (_, _) => _settingsWin = null;
        }
        _settingsWin.Activate();
    }

    private void Quit()
    {
        _tray?.Dispose();
        Environment.Exit(0);
    }
}
