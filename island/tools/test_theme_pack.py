#!/usr/bin/env python3
"""Contract tests for island theme packs (live switch, no SVG regen).

Guards PackTheme / IslandSettings / MainWindow against clock-styles.md and
Assets/Packs/<pack>/{Weather,Statuses}/*.png. Runs on Linux; does not smoke
WinUI win-x64.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ISLAND = Path(__file__).resolve().parents[1]
PACKS = ISLAND / "Assets" / "Packs"
PACK_THEME = ISLAND / "PackTheme.cs"
SETTINGS = ISLAND / "IslandSettings.cs"
WINDOW_CS = ISLAND / "MainWindow.xaml.cs"
WINDOW_XAML = ISLAND / "MainWindow.xaml"
CSPROJ = ISLAND / "NotifyIsland.csproj"
CLOCK_STYLES = PACKS / "clock-styles.md"

PACKS_ORDER = ("dark", "light", "colorful", "mono", "neon", "pastel")
WEATHER = ("sun", "cloud", "rain", "snow", "storm", "fog", "thunder")
STATUSES = ("dnd-active", "dnd-off", "bell-unread", "bell-none")

CLOCK_LOOKS = {
    "Light": {
        "family": "Segoe UI Variable Display",
        "weight": "SemiBold",
        "fg": "#1C1C1E",
        "pill": "#F5F5F7",
    },
    "Dark": {
        "family": "Segoe UI Variable Display",
        "weight": "SemiBold",
        "fg": "#F5F5F7",
        "pill": "#0A0A0A",
    },
    "Colorful": {
        "family": "Segoe UI Variable Display",
        "weight": "Bold",
        "fg": "#F5F5F7",
        "colon": "#FF9F0A",
        "pill": "#0A0A0A",
    },
    "Mono": {
        "family": "Cascadia Mono,Consolas",
        "weight": "Normal",
        "fg": "#F5F5F7",
        "spacing": "200",
        "pill": "#0A0A0A",
    },
    "Neon": {
        "family": "Segoe UI Variable Display",
        "weight": "Bold",
        "fg": "#EAFDFF",
        "pill": "#0A0A0A",
    },
    "Pastel": {
        "family": "Segoe UI Variable Text",
        "weight": "SemiLight",
        "fg": "#F2EAD9",
        "pill": "#0A0A0A",
    },
}

errors: list[str] = []


def fail(msg: str) -> None:
    errors.append(msg)


def _read(path: Path) -> str:
    if not path.is_file():
        fail(f"missing {path.relative_to(ISLAND.parent)}")
        return ""
    return path.read_text(encoding="utf-8")


def test_pack_pngs_present() -> None:
    for pack in PACKS_ORDER:
        for stem in WEATHER:
            p = PACKS / pack / "Weather" / f"{stem}.png"
            if not p.is_file():
                fail(f"missing pack asset {p.relative_to(ISLAND)}")
        for stem in STATUSES:
            p = PACKS / pack / "Statuses" / f"{stem}.png"
            if not p.is_file():
                fail(f"missing pack asset {p.relative_to(ISLAND)}")


def test_theme_pack_enum_and_default() -> None:
    settings = _read(SETTINGS)
    pack_theme = _read(PACK_THEME)
    blob = settings + "\n" + pack_theme
    if "enum ThemePack" not in blob:
        fail("ThemePack enum missing (IslandSettings.cs or PackTheme.cs)")
    for name in ("Dark", "Light", "Colorful", "Mono", "Neon", "Pastel"):
        if not re.search(rf"\b{name}\b", blob):
            fail(f"ThemePack missing member {name}")
    if not re.search(r"ThemePack\s+ThemePack\s*\{\s*get;\s*set;\s*\}\s*=\s*ThemePack\.Dark", settings):
        fail("IslandSettings.ThemePack must persist and default to Dark")


def test_pack_theme_uris_and_status_priority() -> None:
    src = _read(PACK_THEME)
    if "ms-appx:///Assets/Packs/" not in src:
        fail("PackTheme must build ms-appx:///Assets/Packs/<pack>/... png URIs")
    if "Weather/" not in src or "Statuses/" not in src:
        fail("PackTheme URIs must include Weather/ and Statuses/")
    if '"dnd-active"' not in src:
        fail("PackTheme.StatusStem must return dnd-active when DND is on")
    if '"bell-unread"' not in src or '"bell-none"' not in src:
        fail("PackTheme.StatusStem must return bell-unread / bell-none")
    # DND beats unread: dnd-active branch must come before unread check.
    dnd_at = src.find('"dnd-active"')
    unread_at = src.find('"bell-unread"')
    if dnd_at == -1 or unread_at == -1 or dnd_at > unread_at:
        fail("DND-active must beat unread in StatusStem")


def test_clock_looks_match_clock_styles_md() -> None:
    src = _read(PACK_THEME)
    styles = _read(CLOCK_STYLES)
    if "Typography.NumeralAlignment" not in styles:
        fail("clock-styles.md must still document tabular numerals")
    for pack, look in CLOCK_LOOKS.items():
        chunk_start = src.find(f"ThemePack.{pack}")
        if chunk_start == -1:
            fail(f"PackTheme.ClockLook missing ThemePack.{pack}")
            continue
        chunk = src[chunk_start : chunk_start + 420]
        for key, value in look.items():
            if value not in chunk:
                fail(f"PackTheme.ClockLook {pack} missing {key}={value}")
    if "AmberColon" not in src and "amber" not in src.lower():
        fail("PackTheme must flag colorful amber colon")
    if "NeonShadow" not in src:
        fail("PackTheme must flag neon ThemeShadow")


def test_main_window_wires_live_switch() -> None:
    cs = _read(WINDOW_CS)
    xaml = _read(WINDOW_XAML)
    if 'AddEnum("Theme"' not in cs and "AddEnum(\"Пак\"" not in cs and 'AddEnum("Pack"' not in cs:
        fail('MainWindow RMB must add Theme/Pack submenu calling ApplySettings')
    if "ApplyPackIcons" not in cs and "BindPackImage" not in cs:
        fail("MainWindow must bind BitmapImage pack icons with FontIcon fallback")
    if "BitmapImage" not in cs:
        fail("MainWindow must use BitmapImage for pack PNGs")
    if "WeatherImage" not in xaml or "StateImage" not in xaml:
        fail("MainWindow.xaml must host Image for weather+state (FontIcon fallback)")
    if "WeatherImageExp" not in xaml:
        fail("MainWindow.xaml must host expanded weather Image")
    if "ThemePack.Light" not in cs or "#F5F5F7" not in cs:
        fail("Light pack must set light pill background #F5F5F7")
    if "Inlines" not in cs or "Run" not in cs:
        fail("Colorful amber colon must use TextBlock Runs (blink via _colonOn)")
    if "ThemeShadow" not in cs + xaml and "ClockText.Shadow" not in cs:
        fail("Neon pack must apply ThemeShadow (soft default shadow ok)")
    if "FontNumeralAlignment.Default" in cs:
        fail("WinUI FontNumeralAlignment has Normal/Tabular/Proportional, not Default")
    if "FontNumeralAlignment.Tabular" not in cs:
        fail("ApplyClockStyle must set FontNumeralAlignment.Tabular for pack digits")
    if "FontFamily" not in cs or "CharacterSpacing" not in cs:
        fail("ApplyClockStyle must apply pack font/spacing; ClockStyle keeps HH:mm vs seconds/arc")


def test_csproj_keeps_assets_and_pri() -> None:
    csproj = _read(CSPROJ)
    if "Assets\\**\\*" not in csproj and r"Assets\**\*" not in csproj:
        fail("csproj must keep Content Include Assets/**")
    if "IncludeAppPriInPublish" not in csproj:
        fail("csproj must keep IncludeAppPriInPublish from PR #10")


def main() -> int:
    test_pack_pngs_present()
    test_theme_pack_enum_and_default()
    test_pack_theme_uris_and_status_priority()
    test_clock_looks_match_clock_styles_md()
    test_main_window_wires_live_switch()
    test_csproj_keeps_assets_and_pri()
    if errors:
        print("FAIL")
        for e in errors:
            print(f"  - {e}")
        return 1
    print("PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
