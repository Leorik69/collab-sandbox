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

public enum TempUnit
{
    Celsius,
    Fahrenheit
}

/// <summary>DI-like presentation (system model; not copied from missing Packt Live Widget sources).</summary>
public enum PresentationMode
{
    Compact,
    Expanded,
    Minimal
}

public sealed class IslandSettings
{
    public double CornerRadius { get; set; } = 16;
    public double Opacity { get; set; } = 1.0;
    public MaterialMode Material { get; set; } = MaterialMode.Solid;
    public IslandShape Shape { get; set; } = IslandShape.Capsule;
    public string BackgroundHex { get; set; } = "#0A0A0A";
    public string BorderHex { get; set; } = "#FF9F0A";
    public string AccentHex { get; set; } = "#FF9F0A"; // CC: FocusTimer-demo orange, not #0A84FF
    public bool FollowSystemTheme { get; set; } = false;
    public bool DoNotDisturb { get; set; } = false;
    public int UnreadCount { get; set; } = 0;
    public bool PulseAura { get; set; } = true;
    public bool BorderGlow { get; set; } = false;
    public ClockStyle ClockStyle { get; set; } = ClockStyle.DigitalModern;
    public WeatherMode WeatherMode { get; set; } = WeatherMode.ExpandOnHover;
    public IconSet IconSet { get; set; } = IconSet.Fluent;
    public TempUnit TempUnit { get; set; } = TempUnit.Celsius;
    public PresentationMode Presentation { get; set; } = PresentationMode.Compact;

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
