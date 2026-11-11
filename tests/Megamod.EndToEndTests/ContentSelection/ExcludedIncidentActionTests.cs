using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Features.ContentSelection.Core;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace Megamod.EndToEndTests.ContentSelection;

// Requires an isolated run whose persisted loading-save/global choice already excludes this incident.
// This exercises the native final action gate; editing/pending state is a separate UI scenario.
[RimWorldEndToEndTest("megamod.content.excluded-incident-action", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = 900, MaxGameTicks = 1200, MaxWallClockSeconds = 60)]
public sealed class ExcludedIncidentActionTests : IRimWorldEndToEndTest
{
    private Map map = null!;
    private IncidentDef incident = null!;
    private HashSet<Thing> before = null!;
    private bool hadFired;
    private int firedTick;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap ?? throw new InvalidOperationException("A disposable map is required.");
        incident = DefDatabase<IncidentDef>.GetNamed("ShipChunkDrop");
        var runtime = SettingsRuntime.Instance ?? throw new InvalidOperationException("Settings runtime is missing.");
        var binding = runtime.CurrentBinding ?? throw new InvalidOperationException("The loaded save has no settings binding.");
        var key = new ContentKey(incident.modContentPack.PackageIdPlayerFacing, incident.GetType().FullName!, incident.defName);
        string id = "content.allowed/" + key.ToStorageToken();
        var save = runtime.Store.ExportSave(binding);
        string? encoded = save.Values.TryGetValue(id, out var value) && !value.UseGlobal ? value.Override :
            runtime.Store.ExportGlobals().Values.TryGetValue(id, out var global) ? global : null;
        EndToEndAssert.True(encoded == "false", "Fixture must load the excluded choice before this scenario; the scenario does not edit it directly.");
        EndToEndAssert.True(CellFinderLoose.TryFindSkyfallerCell(ThingDefOf.ShipChunkIncoming, map,
            ThingDefOf.ShipChunk.terrainAffordanceNeeded, out _, 10, map.Center, 999999),
            "The native incident must have a valid placement cell, so natural placement failure cannot satisfy refusal.");
        before = map.listerThings.AllThings.ToHashSet();
        hadFired = map.StoryState.lastFireTicks.TryGetValue(incident, out firedTick);
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() =>
        {
            foreach (var thing in map.listerThings.AllThings.Where(IsNewChunkOrIncoming).ToArray())
                thing.Destroy(DestroyMode.Vanish);
            Find.TickManager.CurTimeSpeed = speed;
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause before the excluded incident request", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("before excluded native incident action", Array.Empty<string>(), 0);
        yield return new IncidentActionStep("request excluded native ShipChunkDrop and observe refusal", "ShipChunkDrop", expectRejected: true);
        yield return new ScreenshotStep("native incident refusal and unchanged map", Array.Empty<string>(), 0);
        yield return new AssertionStep("refusal precedes physical and fired-history side effects", _ =>
        {
            EndToEndAssert.False(map.listerThings.AllThings.Any(IsNewChunkOrIncoming), "Refused action must not create incoming or landed chunks.");
            bool nowFired = map.StoryState.lastFireTicks.TryGetValue(incident, out int nowTick);
            EndToEndAssert.True(nowFired == hadFired && (!nowFired || nowTick == firedTick), "Refused action must not record a fired incident.");
            EndToEndAssert.True(ReferenceEquals(DefDatabase<IncidentDef>.GetNamed("ShipChunkDrop"), incident), "The Def must remain registered with the same identity.");
        });
    }

    private bool IsNewChunkOrIncoming(Thing thing) => !before.Contains(thing) &&
        (thing.def == ThingDefOf.ShipChunk || thing.def == ThingDefOf.ShipChunkIncoming);
}
