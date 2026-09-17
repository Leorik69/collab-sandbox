#!/usr/bin/env python3
"""P1: per-app toast icons + Windows notification listener (source-level)."""
from __future__ import annotations

import sys
from pathlib import Path

ISLAND = Path(__file__).resolve().parents[1]
DROP = ISLAND.parent / "drop" / "009-island-per-app-notifications.md"
MAIN_CS = ISLAND / "MainWindow.xaml.cs"
MAIN_XAML = ISLAND / "MainWindow.xaml"
HUB = ISLAND / "AppNotificationHub.cs"
LISTEN = ISLAND / "ToastNotificationListener.cs"


def _read(path: Path) -> str:
    return path.read_text(encoding="utf-8") if path.is_file() else ""


def test_drop_009_exists() -> None:
    text = _read(DROP)
    assert DROP.is_file(), "need drop/009-island-per-app-notifications.md"
    for needle in ("idle", "app icon", "UserNotificationListener", "AppIcons"):
        assert needle.lower() in text.lower(), f"009 missing {needle}"


def test_hub_and_listener_files() -> None:
    assert HUB.is_file(), "need AppNotificationHub.cs"
    assert LISTEN.is_file(), "need ToastNotificationListener.cs"
    hub = _read(HUB)
    listen = _read(LISTEN)
    assert "record" in hub or "class AppToast" in hub or "AppCount" in hub
    assert "Snapshot" in hub
    assert "Bump" in hub
    assert "UserNotificationListener" in listen
    assert "RequestAccess" in listen or "RequestAccessAsync" in listen


def test_xaml_app_icons_row() -> None:
    xaml = _read(MAIN_XAML)
    assert 'x:Name="AppIcons"' in xaml, "need AppIcons stack in expanded pill"
    assert "ExpandedLeading" in xaml


def test_mainwindow_paints_and_clicks() -> None:
    cs = _read(MAIN_CS)
    assert "PaintAppIcons" in cs
    assert "ToastNotificationListener" in cs or "AppNotificationHub" in cs
    assert "AppIcons" in cs
    assert "OpenNotificationCenter" in cs


def main() -> int:
    failed = 0
    for fn in (
        test_drop_009_exists,
        test_hub_and_listener_files,
        test_xaml_app_icons_row,
        test_mainwindow_paints_and_clicks,
    ):
        try:
            fn()
            print(f"PASS  {fn.__name__}")
        except AssertionError as e:
            failed += 1
            print(f"FAIL  {fn.__name__}: {e}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
