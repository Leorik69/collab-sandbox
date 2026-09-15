using System;

namespace NotifyIsland;

/// <summary>004 style maps. No UI types — keep MainWindow thin.</summary>
public static class IslandStyles
{
    // Segoe Variable first; Cascadia only as last-resort tabular fallback.
    public const string ClockFont = "Segoe UI Variable Display, Segoe Variable, Cascadia Mono";

    public static string Temp(int celsius, TempUnit unit)
    {
        var v = unit == TempUnit.F ? celsius * 9 / 5 + 32 : celsius;
        return unit == TempUnit.F ? $"{v}°F" : $"{v}°C";
    }

    public static string WeatherDesc(WeatherKind k) => k switch
    {
        WeatherKind.Cloud => "Пасмурно",
        WeatherKind.Rain => "Дождь",
        WeatherKind.Snow => "Снег",
        WeatherKind.Thunder => "Гроза",
        WeatherKind.Fog => "Туман",
        _ => "Ясно"
    };

    public static string WeatherGlyph(WeatherKind k) => k switch
    {
        WeatherKind.Cloud => "\uE753",
        WeatherKind.Rain => "\uE71A",
        WeatherKind.Snow => "\uE9C3",
        WeatherKind.Thunder => "\uE945",
        WeatherKind.Fog => "\uE909",
        _ => "\uE706"
    };

    public static (string hh, string mm, string ss) SplitClock(DateTime t, bool clock24h)
    {
        var h = clock24h ? t.ToString("HH") : t.ToString("%h");
        return (h, t.ToString("mm"), t.ToString("ss"));
    }

    public static double MinuteProgress(DateTime t) =>
        Math.Clamp(t.Second + t.Millisecond / 1000.0, 0, 60);

    public static (string bell, string dnd, string mute, string wifi, string wifiOff) StatusGlyphs(bool filled) =>
        filled
            ? ("\uE7E7", "\uE708", "\uE74F", "\uE701", "\uE704")
            : ("\uEA8F", "\uE708", "\uE74F", "\uE701", "\uE704");

    public static (int w, int h) SuggestSize(ClockStyle clock, bool expandedWeather)
    {
        var w = 24 + (clock == ClockStyle.SecondsArc ? 96 : 58) + (expandedWeather ? 168 : 56) + 64;
        var h = expandedWeather ? 52 : 40;
        return (Math.Clamp(w, 160, 420), Math.Clamp(h, 32, 64));
    }

    public static WeatherKind NextWeather(WeatherKind k) =>
        (WeatherKind)(((int)k + 1) % 6);
}
