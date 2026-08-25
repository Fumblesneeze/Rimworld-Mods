using System;
using System.Collections.Generic;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway.EndToEndTests;

[RimWorldEndToEndTest(
    "gateway.escape-menu-overlay",
    EndToEndTestContract.GatewayPackageId,
    EndToEndTestContract.CorePackageId,
    MaxFrames = 600,
    MaxGameTicks = 1_000,
    MaxWallClockSeconds = 45)]
public sealed class GatewayEscapeMenuOverlayTests : IRimWorldEndToEndTest
{
    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(
            !ReferenceEquals(Find.MainTabsRoot.OpenTab, MainButtonDefOf.Menu),
            "The disposable game must begin with the native Escape menu closed.");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new ScreenshotStep(
            "ordinary map before opening Escape menu",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new EscapeMenuActionStep("open native Escape menu", open: true);
        yield return new AssertionStep(
            "native Escape menu is current",
            _ => EndToEndAssert.True(
                ReferenceEquals(Find.MainTabsRoot.OpenTab, MainButtonDefOf.Menu),
                "RimWorld's native in-play Escape menu must be current."));
        yield return new ScreenshotStep(
            "native Escape menu with Gateway warning",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new EscapeMenuActionStep("close native Escape menu", open: false);
        yield return new AssertionStep(
            "ordinary map is restored",
            _ => EndToEndAssert.True(
                !ReferenceEquals(Find.MainTabsRoot.OpenTab, MainButtonDefOf.Menu),
                "RimWorld's native in-play Escape menu must be closed."));
        yield return new ScreenshotStep(
            "ordinary map after closing Escape menu",
            Array.Empty<string>(),
            paddingPixels: 0);
    }
}
