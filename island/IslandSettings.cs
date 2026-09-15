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
    public int Width { get; set; } = 176;
    public int Height { get; set; } = 40;

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
