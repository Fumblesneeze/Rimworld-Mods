using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Features.ContentSelection.Core;
using Megamod.Features.ContentSelection.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.ContentSelection;

// Executable baseline only: native allowed action and identity retention, not exclusion acceptance.
[RimWorldEndToEndTest("megamod.content.allowed-incident-catalog", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = 1800, MaxGameTicks = 3000, MaxWallClockSeconds = 90)]
public sealed class AllowedIncidentCatalogTests : IRimWorldEndToEndTest
{
    private Map map = null!;
    private IncidentDef incident = null!;
    private ContentCatalog catalog = null!;
    private ContentKey key = null!;
    private HashSet<Thing> before = null!;
    private Thing? observedChunk;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap ?? throw new InvalidOperationException("A disposable loaded map is required.");
        incident = DefDatabase<IncidentDef>.GetNamed("ShipChunkDrop");
        catalog = NativeContentCatalog.Capture().Catalog;
        key = new ContentKey(incident.modContentPack.PackageIdPlayerFacing, incident.GetType().FullName!, incident.defName);
        EndToEndAssert.True(ReferenceEquals(catalog.Resolve(key).Identity, incident), "Catalog must retain the actual native incident.");
        before = map.listerThings.AllThings.ToHashSet();
        var speed = Find.TickManager.CurTimeSpeed;
        var camera = Find.CameraDriver;
        var positionField = typeof(CameraDriver).GetField("rootPos", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Cannot retain the native camera position for cleanup.");
        var position = (Vector3)positionField.GetValue(camera)!;
        var size = camera.RootSize;
        context.DeferCleanup(() =>
        {
            foreach (var thing in map.listerThings.AllThings.Where(IsNewChunkOrIncoming).ToArray())
                thing.Destroy(DestroyMode.Vanish);
            Find.TickManager.CurTimeSpeed = speed;
            camera.SetRootPosAndSize(position, size);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause before the ordinary incident action", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("before native ship-chunk incident", Array.Empty<string>(), 0);
        yield return new IncidentActionStep("execute native ShipChunkDrop", "ShipChunkDrop");
        yield return new TimeControlActionStep("allow the native incoming chunk to land", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("observe a newly landed physical ship chunk", _ =>
        {
            observedChunk = map.listerThings.ThingsOfDef(ThingDefOf.ShipChunk).FirstOrDefault(thing => !before.Contains(thing));
            return observedChunk != null;
        }, new EndToEndDeadline(900, 1800, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep("pause on the native action result", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame the chunk created by the native incident", new[] { observedChunk!.ThingID }, 120);
        yield return new ScreenshotStep("native chunk exists after allowed incident", new[] { observedChunk!.ThingID }, 120);
        yield return new AssertionStep("incident action preserves catalog and physical Def identity", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(catalog.Resolve(key).Identity, DefDatabase<IncidentDef>.GetNamed("ShipChunkDrop")));
            EndToEndAssert.True(ReferenceEquals(observedChunk!.def, ThingDefOf.ShipChunk));
            EndToEndAssert.True(observedChunk.Spawned, "The observed result must be a physical spawned chunk.");
        });
    }

    private bool IsNewChunkOrIncoming(Thing thing) => !before.Contains(thing) &&
        (thing.def == ThingDefOf.ShipChunk || thing.def == ThingDefOf.ShipChunkIncoming);
}
