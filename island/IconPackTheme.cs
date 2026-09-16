using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotifyIsland;

/// <summary>
/// Runtime catalog for <c>Assets/Packs/&lt;pack&gt;</c> (see Packs README + clock-styles.md).
/// IconPack is independent of IconSet (Fluent/Thin FontIcon).
/// </summary>
public readonly record struct IconPackClockStyle(
    string FontFamily,
    string FontWeight,
    string ForegroundHex,
    int CharacterSpacing,
    bool TabularNums,
    bool AmberColon,
    bool NeonShadow);

public static class IconPackTheme
{
    public const string KeylineHex = "#FF9F0A";

    public static string Id(IconPack pack) => pack switch
    {
        IconPack.Light => "light",
        IconPack.Dark => "dark",
        IconPack.Colorful => "colorful",
        IconPack.Mono => "mono",
        IconPack.Neon => "neon",
        IconPack.Pastel => "pastel",
        _ => "dark"
    };

    public static IconPack Parse(string? id)
    {
        return (id ?? "").Trim().ToLowerInvariant() switch
        {
            "light" => IconPack.Light,
            "colorful" => IconPack.Colorful,
            "mono" => IconPack.Mono,
            "neon" => IconPack.Neon,
            "pastel" => IconPack.Pastel,
            _ => IconPack.Dark
        };
    }

    public static string WeatherUri(IconPack pack, string name) =>
        $"ms-appx:///Assets/Packs/{Id(pack)}/Weather/{name}.png";

    public static string StatusUri(IconPack pack, bool doNotDisturb, bool unread)
    {
        var file = doNotDisturb ? "dnd-active" : unread ? "bell-unread" : "bell-none";
        return $"ms-appx:///Assets/Packs/{Id(pack)}/Statuses/{file}.png";
    }

    public static string PillBackgroundHex(IconPack pack) =>
        pack == IconPack.Light ? "#F5F5F7" : "#0A0A0A";

    public static string ContentForegroundHex(IconPack pack) =>
        pack == IconPack.Light ? "#1C1C1E" : "#F5F5F7";

    public static string MutedForegroundHex(IconPack pack) =>
        pack == IconPack.Light ? "#6E6E73" : "#A0A0A8";

    public static IconPackClockStyle Clock(IconPack pack) => pack switch
    {
        IconPack.Light => new("Segoe UI Variable Display", "SemiBold", "#1C1C1E", 0, true, false, false),
        IconPack.Dark => new("Segoe UI Variable Display", "SemiBold", "#F5F5F7", 0, true, false, false),
        IconPack.Colorful => new("Segoe UI Variable Display", "Bold", "#F5F5F7", 0, true, true, false),
        IconPack.Mono => new("Cascadia Mono,Consolas", "Normal", "#F5F5F7", 200, false, false, false),
        IconPack.Neon => new("Segoe UI Variable Display", "Bold", "#EAFDFF", 0, true, false, true),
        IconPack.Pastel => new("Segoe UI Variable Text", "SemiLight", "#F2EAD9", 0, true, false, false),
        _ => Clock(IconPack.Dark)
    };
}

public sealed class IconPackJsonConverter : JsonConverter<IconPack>
{
    public override IconPack Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var n) && Enum.IsDefined(typeof(IconPack), n))
            return (IconPack)n;
        if (reader.TokenType == JsonTokenType.String)
            return IconPackTheme.Parse(reader.GetString());
        return IconPack.Dark;
    }

    public override void Write(Utf8JsonWriter writer, IconPack value, JsonSerializerOptions options) =>
        writer.WriteStringValue(IconPackTheme.Id(value));
}
