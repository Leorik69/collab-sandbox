#!/usr/bin/env python3
"""P2 settings contract: dedicated window + glow/border keys (source-level)."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ISLAND = Path(__file__).resolve().parents[1]
SETTINGS = ISLAND / "IslandSettings.cs"
MAIN_CS = ISLAND / "MainWindow.xaml.cs"
MAIN_XAML = ISLAND / "MainWindow.xaml"
WIN_XAML = ISLAND / "SettingsWindow.xaml"
WIN_CS = ISLAND / "SettingsWindow.xaml.cs"


def _read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_settings_window_files_exist() -> None:
    assert WIN_XAML.is_file(), "need SettingsWindow.xaml"
    assert WIN_CS.is_file(), "need SettingsWindow.xaml.cs"


def test_new_settings_keys() -> None:
    text = _read(SETTINGS)
    for needle in (
        "enum TransparencyLevel",
        "enum UnreadAnimation",
        "IslandGlowIntensity",
        "ClockGlow",
        "ClockGlowIntensity",
        "BorderThickness",
        "SettingsDark",
        "UnreadAnim",
    ):
        assert needle in text, f"IslandSettings missing {needle}"
    for name in ("Solid", "Soft", "Glass"):
        assert name in text, f"TransparencyLevel missing {name}"
    for name in ("Pulse", "Glow", "SoftBounce", "Off"):
        assert name in text, f"UnreadAnimation missing {name}"
    assert 'BackgroundHex { get; set; } = "#0A0A0A"' in text
    assert 'AccentHex { get; set; } = "#FF9F0A"' in text
    assert 'BorderHex { get; set; } = "#FF9F0A"' in text
    assert not re.search(r'(AccentHex|BorderHex) \{ get; set; \} = "#0A84FF"', text)


def test_glow_fields_are_independent() -> None:
    text = _read(SETTINGS)
    assert "IslandGlowIntensity" in text and "ClockGlow" in text
    assert text.find("IslandGlowIntensity") != text.find("ClockGlow")


def test_settings_window_is_compact_sections() -> None:
    xaml = _read(WIN_XAML)
    cs = _read(WIN_CS)
    blob = xaml + cs
    for label in ("Прозрачность", "Палитра", "Свечение", "Бордерлайн", "Анимация"):
        assert label in blob, f"settings UI missing section {label}"
    assert "RequestedTheme" in cs, "settings window theme must use RequestedTheme"
    assert "SettingsDark" in cs
    assert "IslandGlow" in blob and "ClockGlow" in blob
    assert "ToggleSwitch" in xaml or "ToggleSwitch" in cs


def test_mainwindow_opens_settings_and_applies() -> None:
    cs = _read(MAIN_CS)
    xaml = _read(MAIN_XAML)
    assert "SettingsWindow" in cs
    assert "Настройки" in cs
    assert "UnreadAnim" in cs
    assert "SoftBounce" in cs
    assert "IslandGlowIntensity" in cs
    assert "ClockGlow" in cs
    assert "BorderThickness" in cs or "BorderWidth" in cs
    assert "ClockGlowText" in xaml
    assert "PillNudge" in xaml
    assert "ParseColor(_settings.AccentHex)" in cs or "ParseColor(_settings.BorderHex)" in cs
    assert not re.search(r'(BorderBrush|Foreground|Fill|Background)="#0A84FF"', xaml)
    assert not re.search(r'ParseColor\("#0A84FF"\)', cs)


def main() -> int:
    tests = (
        test_settings_window_files_exist,
        test_new_settings_keys,
        test_glow_fields_are_independent,
        test_settings_window_is_compact_sections,
        test_mainwindow_opens_settings_and_applies,
    )
    failed = 0
    for test in tests:
        try:
            test()
            print(f"PASS  {test.__name__}")
        except (AssertionError, FileNotFoundError) as exc:
            failed += 1
            print(f"FAIL  {test.__name__}: {exc}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
