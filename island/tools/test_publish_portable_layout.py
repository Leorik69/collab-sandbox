#!/usr/bin/env python3
"""Guard: portable zip root must contain NotifyIsland.exe and Запустить.bat.

Fails if Publish-Portable.ps1 zips publish\\* without copying the root launchers
into $publishDir first. Also checks launchers prefer same-directory exe.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parent
ISLAND = TOOLS.parent
SCRIPT = TOOLS / "Publish-Portable.ps1"
BAT = ISLAND / "Запустить.bat"
PS1 = ISLAND / "Start-Portable.ps1"
LAUNCHERS = ("Запустить.bat", "Start-Portable.ps1")


def _read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def test_script_copies_launchers_before_zip() -> None:
    text = _read(SCRIPT)
    zip_at = text.find("Compress-Archive")
    assert zip_at != -1, "Publish-Portable.ps1 must zip via Compress-Archive"
    before_zip = text[:zip_at]
    after_publish = before_zip.split("LASTEXITCODE", 1)
    assert len(after_publish) == 2, "expected publish exit-code check before zip"
    copy_block = after_publish[1]
    assert re.search(r"Copy-Item\b", copy_block), (
        "launchers must be Copy-Item'd into $publishDir after Publish, before Compress-Archive"
    )
    assert "$publishDir" in copy_block
    for name in LAUNCHERS:
        assert name in copy_block, f"{name} must be copied into publish/ before zip"


def test_bat_prefers_same_directory_exe() -> None:
    text = _read(BAT)
    same = text.lower().find('exist "notifyisland.exe"')
    nested = text.lower().find(r'exist "publish\notifyisland.exe"')
    assert same != -1, "Запустить.bat must look for NotifyIsland.exe in the same directory"
    assert nested != -1, "Запустить.bat must keep publish\\NotifyIsland.exe as fallback"
    assert same < nested, "same-directory exe must be the primary path (zip root)"


def test_ps1_prefers_same_directory_exe() -> None:
    text = _read(PS1)
    same = text.find(r"NotifyIsland.exe")
    # first candidate should be same-dir, not publish\
    candidates = re.findall(
        r"Join-Path \$root '([^']*NotifyIsland\.exe)'",
        text,
    )
    assert candidates, "Start-Portable.ps1 must list exe candidates"
    assert candidates[0] == "NotifyIsland.exe", (
        f"primary candidate should be same-dir exe, got {candidates[0]!r}"
    )
    assert any(c.replace("\\", "/") == "publish/NotifyIsland.exe" for c in candidates), (
        "publish\\NotifyIsland.exe must remain a fallback"
    )
    del same


def main() -> int:
    tests = (
        test_script_copies_launchers_before_zip,
        test_bat_prefers_same_directory_exe,
        test_ps1_prefers_same_directory_exe,
    )
    failed = 0
    for test in tests:
        try:
            test()
            print(f"PASS  {test.__name__}")
        except AssertionError as exc:
            failed += 1
            print(f"FAIL  {test.__name__}: {exc}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
