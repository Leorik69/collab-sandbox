using System;
using System.Text.Json;
using Windows.Storage;

namespace NotifyIsland;

public enum MaterialMode
{
    Solid,
    Acrylic,
    Mica
}

public enum IslandShape
{
    Capsule,
    SoftRect,
    Oval
}

public enum ClockStyle
{
    DigitalModern,   // C1 — default, colon blink
    DigitalMinimal,  // C2
    SecondsMinuteArc // C3 — HH:MM:SS + minute arc
}

public enum WeatherMode
{
    Compact,       // W1 — glyph + temp always
    ExpandOnHover  // W2 — desc + feels/min-max on hover
}

public enum IconSet
{
    Fluent, // I1 — default FontIcon
    Thin    // I2 — ExtraLight / thinner glyph
}

public enum IconSourceMode
{
    FontIcon,  // Segoe Fluent glyphs (default, no assets needed)
    LocalAsset // PNG/SVG from island/Assets via ms-appx:///
}

public enum TempUnit
{
    Celsius,
    Fahrenheit
}

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
    public bool PulseAura { get; set; } = true;      // E2 on
    public bool BorderGlow { get; set; } = false;    // E1 off
    public ClockStyle ClockStyle { get; set; } = ClockStyle.DigitalModern; // C1
    public WeatherMode WeatherMode { get; set; } = WeatherMode.Compact;    // W1
    public IconSet IconSet { get; set; } = IconSet.Fluent;                 // I1
    public IconSourceMode IconSource { get; set; } = IconSourceMode.FontIcon; // 005: FontIcon vs Assets
    public TempUnit TempUnit { get; set; } = TempUnit.Celsius;
    public int Width { get; set; } = 240;
    public int Height { get; set; } = 40;

    // Back-compat alias for older persisted JSON
    public bool PulseOnUnread
    {
        get => PulseAura;
        set => PulseAura = value;
    }

    private const string Key = "NotifyIsland.Settings.v1";

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
