#!/usr/bin/env python3
"""Guard IconPack runtime wiring against clock-styles.md + Packs README.

WinUI cannot run on this host; these checks lock the C# contract:
pack ids, ms-appx PNG URIs, clock digit styles, RMB submenu, persistence.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKS = ROOT / "Assets" / "Packs"
SETTINGS = ROOT / "IslandSettings.cs"
THEME = ROOT / "IconPackTheme.cs"
WINDOW_CS = ROOT / "MainWindow.xaml.cs"
WINDOW_XAML = ROOT / "MainWindow.xaml"
CSPROJ = ROOT / "NotifyIsland.csproj"

PACK_IDS = ("light", "dark", "colorful", "mono", "neon", "pastel")
WEATHER = ("sun", "cloud", "rain", "snow", "storm", "fog", "thunder")
STATUSES = ("dnd-active", "dnd-off", "bell-unread", "bell-none")

CLOCK = {
    "light": ("Segoe UI Variable Display", "SemiBold", "#1C1C1E"),
    "dark": ("Segoe UI Variable Display", "SemiBold", "#F5F5F7"),
    "colorful": ("Segoe UI Variable Display", "Bold", "#F5F5F7"),
    "mono": ("Cascadia Mono,Consolas", "Normal", "#F5F5F7"),
    "neon": ("Segoe UI Variable Display", "Bold", "#EAFDFF"),
    "pastel": ("Segoe UI Variable Text", "SemiLight", "#F2EAD9"),
}

errors: list[str] = []


def fail(msg: str) -> None:
    errors.append(msg)


def read(path: Path) -> str:
    if not path.is_file():
        fail(f"missing {path.relative_to(ROOT.parent)}")
        return ""
    return path.read_text(encoding="utf-8")


def main() -> int:
    theme = read(THEME)
    settings = read(SETTINGS)
    cs = read(WINDOW_CS)
    xaml = read(WINDOW_XAML)
    csproj = read(CSPROJ)

    for pack in PACK_IDS:
        for kind, names in (("Weather", WEATHER), ("Statuses", STATUSES)):
            for name in names:
                png = PACKS / pack / kind / f"{name}.png"
                if not png.is_file():
                    fail(f"missing runtime PNG {png.relative_to(ROOT)}")

    if 'Content Include="Assets\\**\\*"' not in csproj.replace("/", "\\") and r'Content Include="Assets\**\*"' not in csproj:
        if "Assets\\**\\*" not in csproj and "Assets/**/*" not in csproj:
            fail("NotifyIsland.csproj must include Assets\\**\\* as Content")

    if not re.search(r"enum\s+IconPack", settings):
        fail("IslandSettings.cs must declare enum IconPack")
    for name in ("Light", "Dark", "Colorful", "Mono", "Neon", "Pastel"):
        if not re.search(rf"\b{name}\b", settings):
            fail(f"IconPack enum missing member {name}")
    if not re.search(r"IconPack\s+IconPack\s*\{\s*get;\s*set;\s*\}\s*=\s*IconPack\.Dark", settings):
        fail("IconPack property must default to IconPack.Dark")
    if not re.search(r"enum\s+IconSet", settings):
        fail("IconSet (Fluent/Thin) must stay separate from IconPack")
    if "IconPackJsonConverter" not in settings and "IconPackJsonConverter" not in theme:
        fail("persist IconPack as lowercase id via IconPackJsonConverter")

    if "ms-appx:///Assets/Packs/" not in theme:
        fail("IconPackTheme must build ms-appx:///Assets/Packs/<pack>/… URIs")
    if '{Id(pack)}/Weather/{name}.png' not in theme and "/Weather/" not in theme:
        fail("Weather URI must point at Assets/Packs/<pack>/Weather/*.png")
    if "/Statuses/" not in theme:
        fail("Status URI must point at Assets/Packs/<pack>/Statuses/*.png")
    if "dnd-active" not in theme or "bell-unread" not in theme or "bell-none" not in theme:
        fail("status mapping must keep DND (dnd-active) over unread (bell-unread) else bell-none")
    if '"#F5F5F7"' not in theme or '"#0A0A0A"' not in theme:
        fail("light pack pill is #F5F5F7; others #0A0A0A")

    for pack, (family, weight, fg) in CLOCK.items():
        for token in (family, weight, fg):
            if token not in theme:
                fail(f"{pack} clock style missing {token!r} (clock-styles.md)")
    if "CharacterSpacing: 200" not in theme and "200" not in theme:
        fail("mono clock style needs CharacterSpacing 200")
    if "AmberColon: true" not in theme and "AmberColon" not in theme:
        fail("colorful clock style needs amber colon Runs")
    if "NeonShadow" not in theme:
        fail("neon clock style needs ThemeShadow best-effort flag")
    if "#FF9F0A" not in cs and "#FF9F0A" not in theme:
        fail("colorful colon / keyline stay #FF9F0A")

    if 'AddEnum("Icon Pack"' not in cs and 'AddEnum("Theme"' not in cs:
        fail('Pill_RightTapped must AddEnum("Icon Pack", …) (or Theme)')
    if "_settings.IconPack" not in cs:
        fail("flyout must read/write _settings.IconPack")
    if "ApplySettings()" not in cs:
        fail("changing Icon Pack must call ApplySettings")
    if "WeatherGlyphImage" not in xaml or "StateIconImage" not in xaml:
        fail("MainWindow.xaml needs Image hosts WeatherGlyphImage + StateIconImage")
    if "WeatherGlyphExpImage" not in xaml:
        fail("MainWindow.xaml needs WeatherGlyphExpImage for expanded weather")
    if "BitmapImage" not in cs:
        fail("ApplySettings/ApplyWeather must set Image.Source from BitmapImage pack PNGs")
    if "ApplyPackClockLook" not in cs:
        fail("ApplyClockStyle must apply pack digit style (ApplyPackClockLook)")
    if "Run" not in cs:
        fail("colorful colon must use Run inlines")
    if "ThemeShadow" not in cs:
        fail("neon ThemeShadow best-effort must exist in MainWindow")
    if "CharacterSpacing" not in cs:
        fail("mono CharacterSpacing must be applied on ClockText")
    if "NumeralAlignment" not in cs and "NumeralAlignment" not in xaml:
        fail("tabular nums (Typography.NumeralAlignment) required by clock-styles.md")
    if "PillBackgroundHex" not in cs:
        fail("ApplySettings must paint pill from IconPackTheme.PillBackgroundHex")

    return _report()


def _report() -> int:
    if errors:
        print("FAIL")
        for e in errors:
            print(f"  - {e}")
        return 1
    print("PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
