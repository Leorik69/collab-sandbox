using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;

namespace NotifyIsland;

/// <summary>
/// Stored in LocalSettings JSON. Unused for the host HWND (always TransparentBackdrop;
/// Mica/Acrylic on the HWND would recreate the rectangular frame).
/// </summary>
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

/// <summary>PNG theme under Assets/Packs/&lt;id&gt;. Separate from IconSet (Fluent/Thin).</summary>
public enum IconPack
{
    Light,
    Dark,
    Colorful,
    Mono,
    Neon,
    Pastel
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

public enum TransparencyLevel
{
    Solid,
    Soft,
    Glass
}

public enum UnreadAnimation
{
    Pulse,
    Glow,
    SoftBounce,
    Off
}

public sealed class IslandSettings
{
    public double CornerRadius { get; set; } = 16;
    public double Opacity { get; set; } = 1.0;
    /// <summary>Unused for HWND; kept so LocalSettings JSON still deserializes.</summary>
    public MaterialMode Material { get; set; } = MaterialMode.Solid;
    public IslandShape Shape { get; set; } = IslandShape.Capsule;
    public string BackgroundHex { get; set; } = "#0A0A0A";
    public string BorderHex { get; set; } = "#FF9F0A";
    public string AccentHex { get; set; } = "#FF9F0A"; // CC: FocusTimer-demo orange, not #0A84FF
    public bool FollowSystemTheme { get; set; } = false;
    public bool DoNotDisturb { get; set; } = false;
    public int UnreadCount { get; set; } = 0;
    public bool StartWithWindows { get; set; } = false;
    public int TopOffsetPx { get; set; } = 8;
    public int NotificationDurationMs { get; set; } = 4000;
    public bool SoundEnabled { get; set; } = false;
    public bool AnimationsEnabled { get; set; } = true;
    public bool PulseAura { get; set; } = true;
    public UnreadAnimation UnreadAnim { get; set; } = UnreadAnimation.Pulse;
    public bool BorderGlow { get; set; } = false;
    public double IslandGlowIntensity { get; set; } = 0;
    public bool ClockGlow { get; set; } = false;
    public double ClockGlowIntensity { get; set; } = 0;
    public double BorderThickness { get; set; } = 1;
    public bool SettingsDark { get; set; } = true;
    public bool CustomPalette { get; set; } = false;
    public ClockStyle ClockStyle { get; set; } = ClockStyle.DigitalModern;
    public WeatherMode WeatherMode { get; set; } = WeatherMode.ExpandOnHover;
    public IconSet IconSet { get; set; } = IconSet.Fluent;
    [JsonConverter(typeof(IconPackJsonConverter))]
    public IconPack IconPack { get; set; } = IconPack.Dark;
    public TempUnit TempUnit { get; set; } = TempUnit.Celsius;
    public PresentationMode Presentation { get; set; } = PresentationMode.Compact;

    public bool PulseOnUnread
    {
        get => PulseAura;
        set => PulseAura = value;
    }

    public TransparencyLevel GetTransparency() =>
        Opacity >= 0.92 ? TransparencyLevel.Solid
        : Opacity >= 0.68 ? TransparencyLevel.Soft
        : TransparencyLevel.Glass;

    public void SetTransparency(TransparencyLevel level) =>
        Opacity = level switch
        {
            TransparencyLevel.Soft => 0.82,
            TransparencyLevel.Glass => 0.55,
            _ => 1.0
        };

    public void SyncDerived()
    {
        PulseAura = UnreadAnim != UnreadAnimation.Off;
        BorderGlow = IslandGlowIntensity > 0.02;
        ClockGlow = ClockGlowIntensity > 0.02;
        BorderThickness = Math.Clamp(BorderThickness, 0.5, 4);
        NotificationDurationMs = Math.Clamp(NotificationDurationMs, 500, 30000);
        TopOffsetPx = Math.Clamp(TopOffsetPx, 0, 80);
        IslandGlowIntensity = Math.Clamp(IslandGlowIntensity, 0, 1);
        ClockGlowIntensity = Math.Clamp(ClockGlowIntensity, 0, 1);
        Opacity = Math.Clamp(Opacity, 0.2, 1);
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
                if (s != null)
                {
                    if (!s.PulseAura && s.UnreadAnim == UnreadAnimation.Pulse)
                        s.UnreadAnim = UnreadAnimation.Off;
                    if (s.BorderGlow && s.IslandGlowIntensity <= 0.02)
                        s.IslandGlowIntensity = 0.85;
                    s.SyncDerived();
                    return s;
                }
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
