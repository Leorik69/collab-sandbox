using System.Text.Json;
using Windows.Storage;

namespace NotifyIsland;

public enum MaterialMode { Solid, Acrylic, Mica }
public enum IslandShape { Capsule, SoftRect, Oval }

public enum ClockStyle { Modern, Minimal, SecondsArc }
public enum WeatherMode { Compact, Expanded }
public enum IconSet { Fluent, MinimalLine, FilledActive }
public enum WeatherKind { Clear, Cloud, Rain, Snow, Thunder, Fog }
public enum TempUnit { C, F }

public sealed class IslandSettings
{
    public double CornerRadius { get; set; } = 16;
    public double Opacity { get; set; } = 0.85;
    public MaterialMode Material { get; set; } = MaterialMode.Acrylic;
    public IslandShape Shape { get; set; } = IslandShape.Capsule;
    public string BackgroundHex { get; set; } = "#1C1C1E";
    public string BorderHex { get; set; } = "#33FFFFFF";
    public string AccentHex { get; set; } = "#0A84FF";
    public bool FollowSystemTheme { get; set; } = true;
    public bool DoNotDisturb { get; set; } = false;
    public int UnreadCount { get; set; } = 0;
    public bool PulseOnUnread { get; set; } = true;
    public int Width { get; set; } = 220;
    public int Height { get; set; } = 40;

    // 004 — CC package defaults: C1 / W1 / I1 / E2+E3 (E1 off)
    public ClockStyle ClockStyle { get; set; } = ClockStyle.Modern;
    public WeatherMode WeatherMode { get; set; } = WeatherMode.Compact;
    public IconSet IconSet { get; set; } = IconSet.Fluent;
    public bool BorderGlow { get; set; } = false;
    public bool PulseAura { get; set; } = true;
    public bool HoverHighlight { get; set; } = true;
    public TempUnit TempUnit { get; set; } = TempUnit.C;
    public bool Clock24h { get; set; } = true;
    public bool Muted { get; set; } = false;
    public bool WifiOn { get; set; } = true;
    public WeatherKind Weather { get; set; } = WeatherKind.Clear;
    public int TempC { get; set; } = 18;
    public int FeelsC { get; set; } = 16;
    public int MinC { get; set; } = 12;
    public int MaxC { get; set; } = 22;

    private const string Key = "NotifyIsland.Settings.v2";

    public static IslandSettings Load()
    {
        try
        {
            var values = ApplicationData.Current.LocalSettings.Values;
            if (values.TryGetValue(Key, out var raw) && raw is string json && !string.IsNullOrWhiteSpace(json))
            {
                var s = JsonSerializer.Deserialize<IslandSettings>(json);
                if (s != null) return s;
            }
        }
        catch
        {
            // LocalSettings unavailable in some unpackaged hosts — fall through to defaults
        }
        return new IslandSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this);
            ApplicationData.Current.LocalSettings.Values[Key] = json;
        }
        catch
        {
            // ignore persist failures
        }
    }
}
