using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImmersiveSignalFire.Buildings;
using ImmersiveSignalFire.Effects;
using ImmersiveSignalFire.Signals;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveSignalFire.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-signal-fire.optional-tools-absent",
    "fumblesneeze.immersivesignalfire",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "blues.forge",
    "fumblesneeze.immersivesignalfire",
    MaxFrames = 4_000,
    MaxGameTicks = 3_000,
    MaxWallClockSeconds = 120)]
public sealed class SignalFireOptionalAbsentWorkflowTests : IRimWorldEndToEndTest
{
    private static readonly FieldInfo? FloatMenuOptions = typeof(FloatMenu).GetField(
        "options",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private readonly List<Pawn> participants = new();
    private readonly List<(int Id, int Goodwill, int AidTick)> relations = new();
    private readonly HashSet<int> originalFactionPawns = new();
    private readonly Dictionary<string, int> baselineAshById = new(StringComparer.Ordinal);
    private Map map = null!;
    private Building_SignalFire fire = null!;
    private Faction faction = null!;
    private Settlement settlement = null!;
    private int baselineAsh;
    private int completedDuration;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap ?? throw new EndToEndAssertionException("A playable map is required.");
        faction = Find.FactionManager.AllFactionsListForReading.First(candidate =>
            candidate != Faction.OfPlayer && !candidate.defeated &&
            candidate.def.techLevel < TechLevel.Industrial);
        foreach (Faction candidate in Find.FactionManager.AllFactionsListForReading.Where(candidate =>
                     candidate != Faction.OfPlayer && !candidate.defeated &&
                     candidate.def.techLevel < TechLevel.Industrial))
        {
            relations.Add((candidate.loadID, candidate.PlayerGoodwill, candidate.lastMilitaryAidRequestTick));
        }
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.Where(pawn => pawn.Faction == faction))
        {
            originalFactionPawns.Add(pawn.thingIDNumber);
        }
        baselineAsh = AshThickness();
        foreach (Filth ash in map.listerThings.ThingsOfDef(ThingDefOf.Filth_Ash).OfType<Filth>())
        {
            baselineAshById[ash.ThingID] = ash.thickness;
        }
        context.DeferCleanup(RestoreEarlyRegisteredState);

        foreach (Faction candidate in Find.FactionManager.AllFactionsListForReading.Where(candidate =>
                     candidate != Faction.OfPlayer && !candidate.defeated &&
                     candidate.def.techLevel < TechLevel.Industrial))
        {
            SetGoodwill(candidate, candidate == faction ? 100 : 0);
        }
        faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame -
                                              MilitaryAidAvailabilityPolicy.CooldownTicks;

        PlanetTile settlementTile = EmptyNearbyTile(map.Tile);
        settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
        settlement.Tile = settlementTile;
        settlement.SetFaction(faction);
        settlement.Name = "Blanketless Smoke Camp";
        Find.WorldObjects.Add(settlement);

        IntVec3 cell = map.AllCells.Where(candidate =>
                CellRect.CenteredOn(candidate, 2).ExpandedBy(3).Cells.All(near =>
                    near.InBounds(map) && near.Standable(map) && !near.Fogged(map) &&
                    near.GetThingList(map).Count == 0))
            .OrderBy(candidate => candidate.DistanceToSquared(map.Center))
            .First();
        fire = (Building_SignalFire)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveSignalFire_SignalFire"));
        fire.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(fire, cell, map);

        for (int index = 0; index < 3; index++)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                fixedBiologicalAge: 30f,
                fixedChronologicalAge: 30f,
                developmentalStages: DevelopmentalStage.Adult,
                forceNoGear: true));
            pawn.skills.GetSkill(SkillDefOf.Melee).Level = 20;
            pawn.skills.GetSkill(SkillDefOf.Social).Level = 20;
            pawn.workSettings?.SetPriority(WorkTypeDefOf.Cleaning, 0);
            IntVec3 pawnCell = map.AllCells.Where(candidate =>
                    candidate.Standable(map) && !candidate.Fogged(map) &&
                    candidate.GetThingList(map).Count == 0 &&
                    !fire.OccupiedRect().ExpandedBy(2).Contains(candidate))
                .OrderBy(candidate => candidate.DistanceToSquared(fire.Position))
                .First();
            GenSpawn.Spawn(pawn, pawnCell, map);
            participants.Add(pawn);
        }

        context.DeferCleanup(() =>
        {
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.Where(pawn =>
                         (participants.Contains(pawn) || pawn.Faction == faction) &&
                         !originalFactionPawns.Contains(pawn.thingIDNumber)).ToArray())
            {
                pawn.Destroy(DestroyMode.Vanish);
            }

            if (!fire.Destroyed)
            {
                fire.Destroy(DestroyMode.Vanish);
            }

            if (!settlement.Destroyed)
            {
                settlement.Destroy();
            }

            foreach ((int id, int goodwill, int aidTick) in relations)
            {
                Faction restored = Find.FactionManager.AllFactionsListForReading.Single(candidate => candidate.loadID == id);
                SetGoodwill(restored, goodwill);
                restored.lastMilitaryAidRequestTick = aidTick;
            }

            foreach (Filth ash in map.listerThings.ThingsOfDef(ThingDefOf.Filth_Ash).OfType<Filth>().ToArray())
            {
                if (baselineAshById.TryGetValue(ash.ThingID, out int originalThickness))
                {
                    ash.thickness = originalThickness;
                }
                else
                {
                    ash.Destroy(DestroyMode.Vanish);
                }
            }
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        if (Find.WindowStack.Windows.Any(window =>
                window.IsOpen &&
                string.Equals(window.GetType().FullName, "LudeonTK.EditWindow_Log", StringComparison.Ordinal)))
        {
            yield return new WindowCancelActionStep(
                "close the startup developer log before the blanketless player workflow",
                "LudeonTK.EditWindow_Log");
        }

        string topLabel = "ImmersiveSignalFire_Contact".Translate().ToString();
        string contactLabel = string.Empty;
        int fadeTick = 0;
        yield return new CameraActionStep("frame the blanketless signal hearth", new[] { fire.ThingID }, 320);
        yield return new SelectionActionStep(
            "select one caller before the blanketless native right-click",
            new[] { participants[0].ThingID },
            additive: false);
        yield return new MapFloatMenuOpenActionStep(
            "open the exact selected-pawn signal-hearth map menu without Show Me Your Tools",
            fire.ThingID,
            new[] { participants[0].ThingID });
        yield return new WaitUntilStep(
            "the blanketless selected-pawn map menu opens",
            _ => Find.WindowStack.Windows.OfType<FloatMenu>().Count(menu => menu.IsOpen) == 1,
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new CurrentFloatMenuActionStep(
            "open the blanketless faction submenu",
            topLabel,
            expectReplacementMenu: true);
        yield return new AssertionStep("resolve the one blanketless faction contact", _ =>
        {
            FloatMenuOption[] options = OpenMenuOptions();
            FloatMenuOption[] matches = options.Where(option =>
                    !option.Disabled && option.Label.StartsWith(faction.Name + " (", StringComparison.Ordinal))
                .ToArray();
            EndToEndAssert.Equal(1, matches.Length,
                "The optional-absent run must retain the ordinary allied contact.");
            contactLabel = matches[0].Label;
        });
        yield return new CurrentFloatMenuActionStep("choose the blanketless allied contact", contactLabel);
        yield return new WaitUntilStep(
            "the Core-safe signal dialog opens without the optional mod",
            _ => Find.WindowStack.Windows.Any(window => window.IsOpen &&
                window.GetType().FullName == "ImmersiveSignalFire.UI.Dialog_BeginSignalFire"),
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new WindowAcceptActionStep(
            "accept the blanketless signal through the Core-safe dialog",
            "ImmersiveSignalFire.UI.Dialog_BeginSignalFire");
        yield return new TimeControlActionStep(
            "run the blanketless signal at normal speed",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "all three pawns reach the active blanketless signal",
            _ => fire.SignalComp.IsActive && fire.SignalComp.ActiveParticipants.Count == 3,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(15)));
        yield return new AssertionStep("the absent optional package disables only blankets", _ =>
        {
            EndToEndAssert.False(OptionalToolsAdapter.Enabled,
                "Show Me Your Tools must be absent and blanket rendering disabled in this exact mod list.");
            EndToEndAssert.True(fire.SignalComp.IsActive,
                "Dynamic Effects Forge fire and smoke must remain active without the optional package.");
        });
        yield return new WaitUntilStep(
            "reach one compact high-rising dark puff without blanket rendering",
            _ => fire.SignalComp.ActiveElapsedTicks >= 182 &&
                 MorseCadence.IsSmokeOn(fire.SignalComp.ActiveElapsedTicks),
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(10)));
        yield return new ScreenshotStep(
            "three colonists signal under a compact high-rising Forge puff with no optional blankets",
            Array.Empty<string>(),
            0);
        yield return new WaitUntilStep(
            "the blanketless signal completes on its shared terminal tick",
            _ => !fire.SignalComp.IsBusy,
            new EndToEndDeadline(1_200, 1_200, TimeSpan.FromSeconds(30)));
        yield return new AssertionStep("measure the blanketless active interval and spent-fire destruction", _ =>
        {
            completedDuration = fire.SignalComp.LastCompletedDurationTicks;
            EndToEndAssert.Equal(600, completedDuration,
                "Optional-mod absence must not change the component's exact active duration.");
            EndToEndAssert.True(fire.Destroyed || !fire.Spawned,
                "Completing the blanketless active signal must consume its single-use fire.");
            EndToEndAssert.False(map.listerThings.AllThings.Any(candidate => candidate.ThingID == fire.ThingID),
                "The spent blanketless fire must no longer be registered on the map.");
        });
        yield return new WaitUntilStep(
            "native allied warriors answer the blanketless signal",
            _ => NewFactionPawnCount() > 0,
            new EndToEndDeadline(600, 600, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep("measure and fade the blanketless completion", _ =>
        {
            EndToEndAssert.Equal(600, completedDuration,
                "The component must retain the exact blanketless active duration after warriors arrive.");
            EndToEndAssert.Equal(16, AshThickness() - baselineAsh,
                "Optional-mod absence must not change terminal soot cleanup.");
            fadeTick = Find.TickManager.TicksGame + 120;
        });
        yield return new WaitUntilStep(
            "the blanketless terminal effects are fully stopped",
            _ => Find.TickManager.TicksGame >= fadeTick,
            new EndToEndDeadline(180, 180, TimeSpan.FromSeconds(10)));
        yield return new TimeControlActionStep(
            "pause the completed blanketless workflow",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new CameraActionStep(
            "frame the blanketless allied warriors after the fire is consumed",
            NewFactionPawnIds(),
            160);
        yield return new ScreenshotStep(
            "blanketless warriors answer after the single-use fire is gone",
            NewFactionPawnIds(),
            260);
        yield return new CheckpointStep("complete optional-tools-absent signal workflow", _ =>
            new Dictionary<string, string>
            {
                ["optionalBlanketsEnabled"] = OptionalToolsAdapter.Enabled.ToString(),
                ["activeDurationTicks"] = completedDuration.ToString(),
                ["fireDestroyed"] = (fire.Destroyed || !fire.Spawned).ToString(),
                ["warriors"] = NewFactionPawnCount().ToString(),
                ["ashThicknessAdded"] = (AshThickness() - baselineAsh).ToString(),
            });
    }

    private int NewFactionPawnCount() => map.mapPawns.AllPawnsSpawned.Count(pawn =>
        pawn.Faction == faction && !originalFactionPawns.Contains(pawn.thingIDNumber));

    private void RestoreEarlyRegisteredState()
    {
        map = Find.CurrentMap ?? map;
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.Where(pawn =>
                     (participants.Any(candidate => candidate.ThingID == pawn.ThingID) ||
                      pawn.Faction?.loadID == faction?.loadID) &&
                     !originalFactionPawns.Contains(pawn.thingIDNumber)).ToArray())
        {
            pawn.Destroy(DestroyMode.Vanish);
        }

        if (fire is not null && !fire.Destroyed)
        {
            Building_SignalFire? loaded = map.listerThings.AllThings.OfType<Building_SignalFire>()
                .FirstOrDefault(candidate => candidate.ThingID == fire.ThingID);
            loaded?.Destroy(DestroyMode.Vanish);
        }

        if (settlement is not null)
        {
            Settlement? loaded = Find.WorldObjects.AllWorldObjects.OfType<Settlement>()
                .FirstOrDefault(candidate => candidate.ID == settlement.ID);
            if (loaded is not null && !loaded.Destroyed)
            {
                loaded.Destroy();
            }
        }

        foreach ((int id, int goodwill, int aidTick) in relations)
        {
            Faction? restored = Find.FactionManager.AllFactionsListForReading
                .FirstOrDefault(candidate => candidate.loadID == id);
            if (restored is not null)
            {
                SetGoodwill(restored, goodwill);
                restored.lastMilitaryAidRequestTick = aidTick;
            }
        }

        foreach (Filth ash in map.listerThings.ThingsOfDef(ThingDefOf.Filth_Ash).OfType<Filth>().ToArray())
        {
            if (baselineAshById.TryGetValue(ash.ThingID, out int originalThickness))
            {
                ash.thickness = originalThickness;
            }
            else
            {
                ash.Destroy(DestroyMode.Vanish);
            }
        }
    }

    private string[] NewFactionPawnIds() => map.mapPawns.AllPawnsSpawned
        .Where(pawn => pawn.Faction == faction && !originalFactionPawns.Contains(pawn.thingIDNumber))
        .Select(pawn => pawn.ThingID)
        .ToArray();

    private int AshThickness() => map.listerThings.ThingsOfDef(ThingDefOf.Filth_Ash)
        .OfType<Filth>()
        .Sum(ash => ash.thickness);

    private static FloatMenuOption[] OpenMenuOptions()
    {
        FloatMenu menu = Find.WindowStack.Windows.OfType<FloatMenu>().Single(window => window.IsOpen);
        var options = FloatMenuOptions?.GetValue(menu) as IEnumerable<FloatMenuOption>;
        EndToEndAssert.NotNull(options, "The current FloatMenu option shape must remain available.");
        return options!.ToArray();
    }

    private static PlanetTile EmptyNearbyTile(PlanetTile origin)
    {
        var visited = new HashSet<PlanetTile> { origin };
        var frontier = new Queue<(PlanetTile Tile, int Distance)>();
        frontier.Enqueue((origin, 0));
        while (frontier.Count > 0)
        {
            (PlanetTile tile, int distance) = frontier.Dequeue();
            if (distance > 0 && distance <= 10 && !Find.WorldObjects.AnyWorldObjectAt(tile))
            {
                return tile;
            }

            if (distance >= 10)
            {
                continue;
            }

            var neighbors = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(tile, neighbors);
            foreach (PlanetTile neighbor in neighbors.Where(visited.Add))
            {
                frontier.Enqueue((neighbor, distance + 1));
            }
        }

        throw new EndToEndAssertionException("No empty nearby settlement tile was available.");
    }

    private static void SetGoodwill(Faction target, int desired)
    {
        int delta = desired - target.PlayerGoodwill;
        if (delta != 0)
        {
            target.TryAffectGoodwillWith(
                Faction.OfPlayer,
                delta,
                canSendMessage: false,
                canSendHostilityLetter: false);
        }
    }
}
