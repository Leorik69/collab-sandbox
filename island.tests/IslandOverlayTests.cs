using Xunit;

namespace NotifyIsland.Tests;

/// <summary>
/// Overlay HWND must contain GlowRing/AuraHalo (pill + 10) and the E2×E3
/// TransformGroup scale (1.08 × 1.06). Sizes match IslandStyles.SuggestSize.
/// </summary>
public class IslandOverlayTests
{
    // C1 compact, C3 compact, C1 expanded, C3 expanded
    public static TheoryData<int, int> SuggestedSizes => new()
    {
        { 202, 40 },
        { 240, 40 },
        { 314, 52 },
        { 352, 52 },
    };

    [Theory]
    [MemberData(nameof(SuggestedSizes))]
    public void HaloFitsInsideWindow(int suggestedW, int suggestedH)
    {
        var layout = IslandOverlay.FromSuggested(suggestedW, suggestedH);

        Assert.True(layout.HaloW <= layout.WindowW,
            $"halo width {layout.HaloW} exceeds window {layout.WindowW}");
        Assert.True(layout.HaloH <= layout.WindowH,
            $"halo height {layout.HaloH} exceeds window {layout.WindowH}");
    }

    [Theory]
    [MemberData(nameof(SuggestedSizes))]
    public void CombinedAuraAndHoverScaleFitsInsideWindow(int suggestedW, int suggestedH)
    {
        var layout = IslandOverlay.FromSuggested(suggestedW, suggestedH);
        var scaledW = (int)Math.Ceiling(layout.PillW * IslandOverlay.CombinedScale);
        var scaledH = (int)Math.Ceiling(layout.PillH * IslandOverlay.CombinedScale);

        Assert.True(scaledW <= layout.WindowW,
            $"scaled pill width {scaledW} exceeds window {layout.WindowW}");
        Assert.True(scaledH <= layout.WindowH,
            $"scaled pill height {scaledH} exceeds window {layout.WindowH}");
    }

    [Fact]
    public void HaloIsTenPixelsLargerThanPill()
    {
        var layout = IslandOverlay.FromSuggested(202, 40);
        Assert.Equal(layout.PillW + IslandOverlay.HaloExtraPx, layout.HaloW);
        Assert.Equal(layout.PillH + IslandOverlay.HaloExtraPx, layout.HaloH);
        Assert.Equal(10, IslandOverlay.HaloExtraPx);
    }

    [Fact]
    public void CombinedScaleMatchesE2AndE3()
    {
        Assert.Equal(1.08, IslandOverlay.AuraScale);
        Assert.Equal(1.06, IslandOverlay.HoverScale);
        Assert.Equal(1.08 * 1.06, IslandOverlay.CombinedScale);
    }

    [Fact]
    public void DefaultUnreadPathAfterClickUsesCompactC1()
    {
        // First LMB demo sets UnreadCount=1; defaults are E2+E3 on C1/W1.
        var layout = IslandOverlay.FromSuggested(202, 40);
        var scaledW = (int)Math.Ceiling(layout.PillW * IslandOverlay.CombinedScale);
        var scaledH = (int)Math.Ceiling(layout.PillH * IslandOverlay.CombinedScale);
        Assert.True(layout.HaloW <= layout.WindowW && layout.HaloH <= layout.WindowH);
        Assert.True(scaledW <= layout.WindowW && scaledH <= layout.WindowH);
    }
}
