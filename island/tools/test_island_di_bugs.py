#!/usr/bin/env python3
"""Contract tests for Dynamic Island bugs a–d (chrome, morph, hover, RMB)."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "MainWindow.xaml"
CS = ROOT / "MainWindow.xaml.cs"
BACKDROP = ROOT / "TransparentBackdrop.cs"
IDEAL = ROOT.parent / "drop" / "008-island-ideal-ux.md"

errors: list[str] = []


def fail(msg: str) -> None:
    errors.append(msg)


def main() -> int:
    xaml = XAML.read_text(encoding="utf-8") if XAML.is_file() else ""
    cs = CS.read_text(encoding="utf-8") if CS.is_file() else ""
    backdrop = BACKDROP.read_text(encoding="utf-8") if BACKDROP.is_file() else ""
    ideal = IDEAL.read_text(encoding="utf-8") if IDEAL.is_file() else ""

    if not XAML.is_file():
        fail(f"missing {XAML}")
    if not CS.is_file():
        fail(f"missing {CS}")

    # a) no rectangular host chrome — transparent HWND, no DWM border
    if not BACKDROP.is_file():
        fail("missing TransparentBackdrop.cs (chromeless host)")
    else:
        if "DwmEnableBlurBehindWindow" not in backdrop and "DwmEnableBlurBehindWindow" not in cs:
            fail("a: DwmEnableBlurBehindWindow required for per-pixel transparent host")
        if "ICompositionSupportsSystemBackdrop" not in backdrop and "ICompositionSupportsSystemBackdrop" not in cs:
            fail("a: ICompositionSupportsSystemBackdrop transparent brush required")
    host = cs + "\n" + backdrop
    if "DWMWA_BORDER_COLOR" not in host and "34" not in host:
        fail("a: DWMWA_BORDER_COLOR must be set")
    if "0xFFFFFFFE" not in host and "DWMWA_COLOR_NONE" not in host:
        fail("a: DWMWA_COLOR_NONE (0xFFFFFFFE) to suppress white window border")
    if "SetBorderAndTitleBar(false, false)" not in cs:
        fail("a: SetBorderAndTitleBar(false, false) required")
    if "TransparentBackdrop" not in cs:
        fail("a: Window.SystemBackdrop must use TransparentBackdrop")

    # b) compact → expanded morph ~250–300ms
    m = re.search(r"MorphMs\s*=\s*(\d+)", cs)
    if not m:
        fail("b: MorphMs constant missing")
    else:
        ms = int(m.group(1))
        if ms < 250 or ms > 300:
            fail(f"b: MorphMs={ms} not in 250–300ms")
    if not re.search(r'MorphAnim\([^)]*"Width"', cs) and not re.search(r'SetTargetProperty\([^,]+,\s*"Width"\)', cs):
        fail("b: Width morph animation missing")
    if not re.search(r'MorphAnim\([^)]*"Height"', cs) and not re.search(r'SetTargetProperty\([^,]+,\s*"Height"\)', cs):
        fail("b: Height morph animation missing")
    if not re.search(r"SetTarget\([^)]*ExpandedRegion", cs) and "_morphOp" not in cs:
        fail("b: ExpandedRegion opacity should morph with size (not a visibility pop)")

    # c) hover must not move the pill; tabular figures
    if "FontNumeralAlignment=" in xaml:
        fail("c: FontNumeralAlignment is not a WinUI 3 TextBlock XAML member (WMC0011)")
    if "SetNumeralAlignment" not in cs and "FontNumeralAlignment" not in cs:
        fail("c: FontNumeralAlignment.Tabular required on clock (Typography.SetNumeralAlignment)")
    elif "Tabular" not in cs:
        fail("c: FontNumeralAlignment must be Tabular")
    clock = re.search(r'<TextBlock\s+x:Name="ClockText"[\s\S]*?/>', xaml)
    if not clock or "MinWidth" not in clock.group(0):
        fail("c: ClockText needs MinWidth so HH:MM vs HH:MM:SS does not reflow")
    if 'x:Name="Pill"' in xaml and 'VerticalAlignment="Top"' not in xaml:
        fail("c: Pill must be top-aligned so hover morph does not drop from the top edge")
    if "PlaceWindow" in cs and re.search(
        r"if \(grow\)[\s\S]{0,80}PlaceWindow", cs
    ):
        fail("c: do not PlaceWindow-grow on hover (HWND jump)")

    # d) RMB / flyout must not collapse the island
    if "_menuOpen" not in cs:
        fail("d: _menuOpen flag missing")
    if ".Closed" not in cs:
        fail("d: MenuFlyout.Closed handler missing")
    if "_menuOpen" in cs and "IsExpanded" in cs:
        # IsExpanded (or collapse path) must keep expanded while menu is open
        expanded_fn = re.search(
            r"private bool IsExpanded\(\)[^{]*\{([^}]+)\}", cs
        )
        if expanded_fn and "_menuOpen" not in expanded_fn.group(1):
            if "PointerExited" in cs and "_menuOpen" not in cs[cs.find("Pill_PointerExited"):cs.find("Pill_PointerExited")+500]:
                fail("d: collapse path must skip while _menuOpen")
        # PointerExited must consult the flag
        exited = cs[cs.find("Pill_PointerExited"):cs.find("Pill_PointerExited") + 600] if "Pill_PointerExited" in cs else ""
        if "_menuOpen" not in exited and (not expanded_fn or "_menuOpen" not in expanded_fn.group(1)):
            fail("d: PointerExited/IsExpanded must keep island up while menu is open")

    # ideal UX: plan only, no feature code
    if not IDEAL.is_file():
        fail("missing drop/008-island-ideal-ux.md (plan only)")
    else:
        for needle in ("idle", "notification", "UX research"):
            if needle.lower() not in ideal.lower():
                fail(f"ideal plan must mention {needle!r}")
        if "do not implement" not in ideal.lower() and "waits" not in ideal.lower() and "pending" not in ideal.lower():
            fail("ideal plan must state code waits on UX research")

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
