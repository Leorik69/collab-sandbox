#!/usr/bin/env python3
"""Guard Publish-Portable.ps1 against PowerShell $Zip / $zip name collision.

PowerShell variables are case-insensitive. param([switch]$Zip) and a later
$zip = Join-Path ... share one slot. Assigning a path then fails (cannot
convert String -> SwitchParameter) or silently overwrites the switch.
The zip path must live in $zipPath; the -Zip switch stays as-is.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = Path(__file__).resolve().parent / "Publish-Portable.ps1"
GITIGNORE = ROOT / ".gitignore"
README = ROOT / "README.md"

errors: list[str] = []


def fail(msg: str) -> None:
    errors.append(msg)


def main() -> int:
    if not SCRIPT.is_file():
        fail(f"missing {SCRIPT}")
        return _report()

    text = SCRIPT.read_text(encoding="utf-8")

    if not re.search(r"\[switch\]\$Zip\s*=\s*\$true", text):
        fail("param [switch]$Zip = $true is missing (keep -Zip, default $true)")

    if "NotifyIsland-portable-win-x64.zip" not in text:
        fail("output zip name must stay NotifyIsland-portable-win-x64.zip")

    if not re.search(r"\$zipPath\s*=\s*Join-Path", text):
        fail("zip destination must be assigned to $zipPath, not $zip")

    for name in ("Test-Path $zipPath", "Remove-Item $zipPath", "-DestinationPath $zipPath"):
        if name not in text:
            fail(f"missing $zipPath use: {name}")

    if "OK: $zipPath" not in text:
        fail('Write-Host must print $zipPath, not $zip')

    for i, line in enumerate(text.splitlines(), 1):
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        if re.search(r"\[switch\]\$Zip", line):
            continue
        if re.search(r"if\s*\(\s*\$Zip\s*\)", line):
            continue
        if re.search(r"\$zip\b(?!Path)", line, re.I):
            fail(f"line {i}: colliding $zip/$Zip (use $zipPath): {stripped}")

    gitignore = GITIGNORE.read_text(encoding="utf-8") if GITIGNORE.is_file() else ""
    for pattern in ("publish/", "*.zip"):
        if pattern not in gitignore:
            fail(f"island/.gitignore must ignore {pattern}")

    readme = README.read_text(encoding="utf-8") if README.is_file() else ""
    if "Publish-Portable.ps1" not in readme:
        fail("island/README.md must document Publish-Portable.ps1")
    if "Windows" not in readme:
        fail("island/README.md must note that the portable zip is built on Windows")

    for extra in (
        ROOT / "Start-Portable.ps1",
        ROOT / "Запустить.bat",
    ):
        if not extra.is_file():
            fail(f"missing portable launcher: {extra.name}")

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
