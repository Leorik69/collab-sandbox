#!/usr/bin/env python3
"""Guard portable payload: unpackaged self-contained WinUI + launchers + app PRI.

APPCRASH 0xc000027b in Microsoft.UI.Xaml.dll happens when publish/zip omit the
app's resources.pri / NotifyIsland.pri (WASDK #3451/#3718/#6720). Windows App
SDK PRIs alone are not enough.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parent
ISLAND = TOOLS.parent
SCRIPT = TOOLS / "Publish-Portable.ps1"
CSPROJ = ISLAND / "NotifyIsland.csproj"
BAT = ISLAND / "Запустить.bat"
PS1 = ISLAND / "Start-Portable.ps1"
WORKFLOW = ISLAND.parent / ".github" / "workflows" / "portable-win-x64.yml"
LAUNCHERS = ("Запустить.bat", "Start-Portable.ps1")


def _read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def _zip_index(text: str) -> int:
    for marker in (
        "Compress-PublishDir -SourceDir",
        "Compress-Archive",
        "ZipFile]::CreateFromDirectory",
    ):
        at = text.find(marker)
        if at != -1:
            return at
    raise AssertionError("Publish-Portable.ps1 must create a zip after publish")


def test_script_copies_launchers_before_zip() -> None:
    text = _read(SCRIPT)
    zip_at = _zip_index(text)
    before_zip = text[:zip_at]
    after_publish = before_zip.split("LASTEXITCODE", 1)
    assert len(after_publish) == 2, "expected publish exit-code check before zip"
    copy_block = after_publish[1]
    assert re.search(r"Copy-Item\b", copy_block), (
        "launchers must be Copy-Item'd into $publishDir after Publish, before zip"
    )
    assert "$publishDir" in copy_block
    for name in LAUNCHERS:
        assert name in copy_block, f"{name} must be copied into publish/ before zip"


def test_script_is_unpackaged_self_contained_win_x64() -> None:
    text = _read(SCRIPT)
    assert "win-x64" in text
    assert "SelfContained=true" in text
    assert "WindowsAppSDKSelfContained=true" in text
    assert "WindowsPackageType=None" in text
    assert "EnableMsixTooling=true" in text, (
        "EnableMsixTooling wires app PRI into unpackaged publish (WASDK #3451/#3718)"
    )


def test_script_ensures_app_pri_before_zip() -> None:
    text = _read(SCRIPT)
    zip_at = _zip_index(text)
    before_zip = text[:zip_at]
    assert "resources.pri" in before_zip, "must look for resources.pri before zipping"
    assert "NotifyIsland.pri" in before_zip, "must look for NotifyIsland.pri before zipping"
    assert re.search(r"throw\b", before_zip), "must fail the zip if app PRI is still missing"


def test_csproj_enable_msix_tooling() -> None:
    text = _read(CSPROJ)
    assert re.search(
        r"<EnableMsixTooling>\s*true\s*</EnableMsixTooling>",
        text,
    ), "csproj must set EnableMsixTooling so unpackaged publish copies app PRI"
    assert re.search(
        r"<WindowsAppSDKSelfContained>\s*true\s*</WindowsAppSDKSelfContained>",
        text,
    )
    assert re.search(r"<WindowsPackageType>\s*None\s*</WindowsPackageType>", text)


def test_csproj_includes_app_pri_in_publish() -> None:
    text = _read(CSPROJ)
    assert "ResolvedFileToPublish" in text, (
        "csproj must add the app PRI to ResolvedFileToPublish (WASDK #6720)"
    )
    assert "resources.pri" in text
    assert "_PublishedAppPri" in text, (
        "must skip adding resources.pri if EnableMsixTooling already published it (NETSDK1152)"
    )


def test_bat_prefers_publish_then_root() -> None:
    text = _read(BAT)
    nested = text.lower().find(r'exist "publish\notifyisland.exe"')
    same = text.lower().find('exist "notifyisland.exe"')
    assert nested != -1, "Запустить.bat must look for publish\\NotifyIsland.exe first"
    assert same != -1, "Запустить.bat must fall back to same-directory NotifyIsland.exe"
    assert nested < same, "publish\\ exe must be preferred when both exist"


def test_ps1_prefers_publish_then_root() -> None:
    text = _read(PS1)
    candidates = re.findall(
        r"Join-Path \$root '([^']*NotifyIsland\.exe)'",
        text,
    )
    assert candidates, "Start-Portable.ps1 must list exe candidates"
    assert candidates[0].replace("\\", "/") == "publish/NotifyIsland.exe", (
        f"primary candidate should be publish\\ exe, got {candidates[0]!r}"
    )
    assert any(c == "NotifyIsland.exe" for c in candidates), (
        "same-directory exe must remain a fallback"
    )


def test_workflow_requires_pri_and_wasdk() -> None:
    text = _read(WORKFLOW)
    for need in (
        "NotifyIsland.exe",
        "Запустить.bat",
        "resources.pri",
        "Microsoft.WindowsAppRuntime.Bootstrap.dll",
        "Microsoft.ui.xaml.dll",
    ):
        assert need in text, f"CI zip verify must require {need}"


def main() -> int:
    tests = (
        test_script_copies_launchers_before_zip,
        test_script_is_unpackaged_self_contained_win_x64,
        test_script_ensures_app_pri_before_zip,
        test_csproj_enable_msix_tooling,
        test_csproj_includes_app_pri_in_publish,
        test_bat_prefers_publish_then_root,
        test_ps1_prefers_publish_then_root,
        test_workflow_requires_pri_and_wasdk,
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
