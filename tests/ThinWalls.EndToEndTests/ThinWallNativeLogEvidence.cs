using System;
using System.Collections.Generic;
using LudeonTK;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ThinWalls.EndToEndTests;

internal static class ThinWallNativeLogEvidence
{
    // Diagnostic observation only: never a substitute for a product player action.
    public static IEnumerable<EndToEndStep> Capture(IEndToEndContext context)
    {
        bool originalScreenshotMode = Find.ScreenshotModeHandler.Active;
        EditWindow_Log? ownedWindow = null;
        context.DeferCleanup(() =>
        {
            ownedWindow?.Close();
            Find.ScreenshotModeHandler.Active = originalScreenshotMode;
        });
        yield return new ScreenshotModeActionStep("show UI for native console inspection", false);
        if (Find.WindowStack.WindowOfType<EditWindow_Log>() == null)
        {
            ownedWindow = new EditWindow_Log();
            Find.WindowStack.Add(ownedWindow);
        }
        yield return new ScreenshotStep("native developer console after player workflow", Array.Empty<string>(), 0);
        ownedWindow?.Close();
        ownedWindow = null;
        yield return new ScreenshotModeActionStep("restore screenshot mode after console inspection", originalScreenshotMode);
    }
}
