using System;

namespace NotifyIsland;

public readonly record struct OverlayLayout(
    int WindowW,
    int WindowH,
    int PillW,
    int PillH,
    int HaloW,
    int HaloH);

/// <summary>
/// HWND vs pill vs GlowRing/AuraHalo. Window must contain the +10px halo and the
/// E2×E3 TransformGroup scale (1.08 × 1.06). Keep scales in sync with MainWindow.
/// </summary>
public static class IslandOverlay
{
    public const int HaloExtraPx = 10;
    public const double AuraScale = 1.08;
    public const double HoverScale = 1.06;
    public const int SafetyPx = 2;

    public static double CombinedScale => AuraScale * HoverScale;

    public static OverlayLayout FromSuggested(int suggestedW, int suggestedH)
    {
        var pillW = Math.Max(80, Math.Clamp(suggestedW, 120, 420));
        var pillH = Math.Max(24, Math.Clamp(suggestedH, 28, 64));
        var haloW = pillW + HaloExtraPx;
        var haloH = pillH + HaloExtraPx;
        var scaledW = (int)Math.Ceiling(pillW * CombinedScale);
        var scaledH = (int)Math.Ceiling(pillH * CombinedScale);
        var windowW = Math.Max(haloW, scaledW) + 2 * SafetyPx;
        var windowH = Math.Max(haloH, scaledH) + 2 * SafetyPx;
        return new OverlayLayout(windowW, windowH, pillW, pillH, haloW, haloH);
    }
}
