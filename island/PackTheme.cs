namespace NotifyIsland;

/// <summary>Digit cosmetics + pack PNG URIs. ClockStyle still owns HH:mm vs seconds/arc.</summary>
public readonly record struct PackClockLook(
    string FontFamily,
    string FontWeight,
    string ForegroundHex,
    int CharacterSpacing,
    bool TabularNumerals,
    bool AmberColon,
    string ColonHex,
    bool NeonShadow,
    string PillBackgroundHex);

public static class PackTheme
{
    public static string FolderName(ThemePack pack) => pack.ToString().ToLowerInvariant();

    public static string WeatherPngUri(ThemePack pack, string stem) =>
        $"ms-appx:///Assets/Packs/{FolderName(pack)}/Weather/{stem}.png";

    public static string StatusPngUri(ThemePack pack, string stem) =>
        $"ms-appx:///Assets/Packs/{FolderName(pack)}/Statuses/{stem}.png";

    /// <summary>DND-active beats unread; else bell-unread / bell-none.</summary>
    public static string StatusStem(bool doNotDisturb, int unreadCount) =>
        doNotDisturb ? "dnd-active" : unreadCount > 0 ? "bell-unread" : "bell-none";

    public static PackClockLook ClockLook(ThemePack pack) => pack switch
    {
        ThemePack.Light => new("Segoe UI Variable Display", "SemiBold", "#1C1C1E", 0, true, false, "", false, "#F5F5F7"),
        ThemePack.Dark => new("Segoe UI Variable Display", "SemiBold", "#F5F5F7", 0, true, false, "", false, "#0A0A0A"),
        ThemePack.Colorful => new("Segoe UI Variable Display", "Bold", "#F5F5F7", 0, true, true, "#FF9F0A", false, "#0A0A0A"),
        ThemePack.Mono => new("Cascadia Mono,Consolas", "Normal", "#F5F5F7", 200, false, false, "", false, "#0A0A0A"),
        ThemePack.Neon => new("Segoe UI Variable Display", "Bold", "#EAFDFF", 0, true, false, "", true, "#0A0A0A"),
        ThemePack.Pastel => new("Segoe UI Variable Text", "SemiLight", "#F2EAD9", 0, true, false, "", false, "#0A0A0A"),
        _ => ClockLook(ThemePack.Dark),
    };
}
