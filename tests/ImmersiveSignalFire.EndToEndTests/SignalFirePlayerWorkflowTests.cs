using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImmersiveSignalFire.Buildings;
using ImmersiveSignalFire.Effects;
using ImmersiveSignalFire.Interactions;
using ImmersiveSignalFire.Signals;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveSignalFire.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-signal-fire.player-workflow",
    "fumblesneeze.immersivesignalfire",
    "brrainz.harmony",
    "ludeon.rimworld",
    "blues.forge",
    "meathax.showmeyourtools",
    "fumblesneeze.immersivesignalfire",
    MaxFrames = 30_000,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 720)]
public sealed class SignalFirePlayerWorkflowTests : IRimWorldEndToEndTest
{
    private static readonly FieldInfo? FloatMenuOptions = typeof(FloatMenu).GetField(
        "options",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly FieldInfo? ScheduledResponses = typeof(MapComponent_SignalResponses).GetField(
        "scheduled",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? LiveMessages = typeof(Messages).GetField(
        "liveMessages",
        BindingFlags.Static | BindingFlags.NonPublic);

    private readonly List<(int FactionLoadId, int Goodwill, int AidRequestTick)> relationSnapshots = new();
    private readonly List<(string PawnId, int Melee, int Social, int Cleaning)> pawnSnapshots = new();
    private readonly Dictionary<string, int> baselineAsh = new(StringComparer.Ordinal);
    private readonly List<(int FactionLoadId, int DueTick)> scheduledResponseBaseline = new();
    private readonly List<Pawn> generatedColonists = new();
    private readonly List<Pawn> supportPawns = new();
    private readonly List<Settlement> settlements = new();
    private readonly List<(IntVec3 Cell, TerrainDef Terrain)> terrainSnapshots = new();
    private readonly HashSet<int> originalFactionPawnIds = new();
    private readonly HashSet<string> createdFireIds = new(StringComparer.Ordinal);
    private Map map = null!;
    private Building_SignalFire fire = null!;
    private ThingDef fireDef = null!;
    private IntVec3 fireCell;
    private Faction delayedFaction = null!;
    private Faction immediateFaction = null!;
    private Pawn menuActor = null!;
    private string topLevelLabel = null!;
    private int baselineAshThickness;
    private int ashBeforeImmediate;
    private int firstStartTick;
    private int secondStartTick;
    private int thirdStartTick;
    private int firstDuration;
    private int secondDuration;
    private int thirdDuration;
    private int delayedGoodwillBeforeAid;
    private int delayedGoodwillAfterAid;
    private int delayedAidTickAfter;
    private int immediateGoodwillBeforeAid;
    private int immediateAidTickAfter;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap ?? throw new EndToEndAssertionException("A playable map is required.");
        baselineAshThickness = AshThickness();
        foreach (Filth ash in map.listerThings.ThingsOfDef(ThingDefOf.Filth_Ash).OfType<Filth>())
        {
            baselineAsh[ash.ThingID] = ash.thickness;
        }
        scheduledResponseBaseline.AddRange(CurrentScheduledResponses().Select(response =>
            (response.Faction?.loadID ?? -1, response.DueTick)));
        bool originalScreenshotMode = Find.ScreenshotModeHandler?.Active == true;
        context.DeferCleanup(() => RestoreEarlyRegisteredState(originalScreenshotMode));
        List<Faction> lowTechFactions = Find.FactionManager.AllFactionsListForReading
            .Where(faction => faction != Faction.OfPlayer && !faction.defeated &&
                              faction.def.techLevel < TechLevel.Industrial)
            .Take(2)
            .ToList();
        EndToEndAssert.Equal(2, lowTechFactions.Count,
            "The isolated generated world must provide two sub-industrial factions.");
        delayedFaction = lowTechFactions[0];
        immediateFaction = lowTechFactions[1];

        foreach (Faction faction in Find.FactionManager.AllFactionsListForReading
                     .Where(faction => faction != Faction.OfPlayer && !faction.defeated &&
                                       faction.def.techLevel < TechLevel.Industrial))
        {
            relationSnapshots.Add((faction.loadID, faction.PlayerGoodwill, faction.lastMilitaryAidRequestTick));
            SetPlayerRelation(faction, FactionRelationKind.Neutral);
        }

        List<PlanetTile> emptyNearbyTiles = EmptyNearbyTiles(map.Tile, 2);
        settlements.Add(CreateSettlement(delayedFaction, emptyNearbyTiles[0], "Delayed Smoke Camp"));
        settlements.Add(CreateSettlement(immediateFaction, emptyNearbyTiles[1], "Immediate Smoke Camp"));

        fireCell = map.AllCells
            .Where(cell => CellRect.SingleCell(cell).ExpandedBy(3).Cells.All(candidate =>
                candidate.InBounds(map) && candidate.Standable(map) && !candidate.Fogged(map) &&
                candidate.GetThingList(map).Count == 0))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        fireDef = DefDatabase<ThingDef>.GetNamed("ImmersiveSignalFire_SignalFire");
        foreach (IntVec3 cell in CellRect.SingleCell(fireCell).ExpandedBy(4).Cells.Where(cell => cell.InBounds(map)))
        {
            terrainSnapshots.Add((cell, map.terrainGrid.TerrainAt(cell)));
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.PavedTile);
        }
        SpawnPreparedFire();

        List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned
            .Where(pawn => pawn.ageTracker.Adult && !pawn.Downed && !pawn.InMentalState)
            .ToList();
        for (int generatedIndex = 0; generatedIndex < 3; generatedIndex++)
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
            IntVec3 pawnCell = map.AllCells
                .Where(cell => cell.Standable(map) && !cell.Fogged(map) &&
                               cell.GetThingList(map).Count == 0 &&
                               !fire.OccupiedRect().ExpandedBy(2).Contains(cell))
                .OrderBy(cell => cell.DistanceToSquared(fire.Position))
                .First();
            GenSpawn.Spawn(pawn, pawnCell, map);
            generatedColonists.Add(pawn);
            colonists.Add(pawn);
        }

        Pawn deconstructor = GenerateDeconstructor();
        IntVec3 deconstructorCell = map.AllCells
            .Where(cell => cell.Standable(map) && !cell.Fogged(map) &&
                           cell.GetThingList(map).Count == 0 &&
                           !fire.OccupiedRect().ExpandedBy(2).Contains(cell))
            .OrderBy(cell => cell.DistanceToSquared(fire.Position))
            .First();
        GenSpawn.Spawn(deconstructor, deconstructorCell, map);
        supportPawns.Add(deconstructor);
        colonists.Add(deconstructor);

        menuActor = generatedColonists[0];
        foreach (Pawn pawn in colonists)
        {
            int cleaning = pawn.workSettings?.GetPriority(WorkTypeDefOf.Cleaning) ?? 0;
            pawnSnapshots.Add((
                pawn.ThingID,
                pawn.skills.GetSkill(SkillDefOf.Melee).Level,
                pawn.skills.GetSkill(SkillDefOf.Social).Level,
                cleaning));
            pawn.workSettings?.SetPriority(WorkTypeDefOf.Cleaning, 0);
        }

        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.Where(pawn =>
                     pawn.Faction == delayedFaction || pawn.Faction == immediateFaction))
        {
            originalFactionPawnIds.Add(pawn.thingIDNumber);
        }

        topLevelLabel = "ImmersiveSignalFire_Contact".Translate().ToString();
        context.DeferCleanup(() =>
        {
            map = Find.CurrentMap ?? map;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.Where(pawn =>
                         (pawn.Faction == delayedFaction || pawn.Faction == immediateFaction) &&
                         !originalFactionPawnIds.Contains(pawn.thingIDNumber)).ToArray())
            {
                pawn.Destroy(DestroyMode.Vanish);
            }

            foreach ((string pawnId, int melee, int social, int cleaning) in pawnSnapshots)
            {
                Pawn? pawn = map.mapPawns.AllPawnsSpawned.FirstOrDefault(candidate => candidate.ThingID == pawnId);
                if (pawn is not null && !pawn.Destroyed)
                {
                    pawn.skills.GetSkill(SkillDefOf.Melee).Level = melee;
                    pawn.skills.GetSkill(SkillDefOf.Social).Level = social;
                    pawn.workSettings?.SetPriority(WorkTypeDefOf.Cleaning, cleaning);
                }
            }

            foreach (string generatedId in generatedColonists.Concat(supportPawns)
                         .Select(pawn => pawn.ThingID).ToArray())
            {
                Pawn? pawn = map.mapPawns.AllPawnsSpawned.FirstOrDefault(candidate => candidate.ThingID == generatedId);
                if (pawn is not null && !pawn.Destroyed)
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
            }

            foreach (Building_SignalFire currentFire in map.listerThings.AllThings
                         .OfType<Building_SignalFire>()
                         .Where(candidate => createdFireIds.Contains(candidate.ThingID))
                         .ToArray())
            {
                currentFire.Destroy(DestroyMode.Vanish);
            }

            foreach (Settlement settlement in settlements.Where(settlement => !settlement.Destroyed))
            {
                settlement.Destroy();
            }

            foreach ((int factionLoadId, int goodwill, int aidRequestTick) in relationSnapshots)
            {
                Faction? faction = Find.FactionManager.AllFactionsListForReading
                    .FirstOrDefault(candidate => candidate.loadID == factionLoadId);
                if (faction is not null)
                {
                    SetPlayerGoodwill(faction, goodwill);
                    faction.lastMilitaryAidRequestTick = aidRequestTick;
                }
            }

            foreach (Filth ash in map.listerThings.ThingsOfDef(ThingDefOf.Filth_Ash).OfType<Filth>().ToArray())
            {
                if (baselineAsh.TryGetValue(ash.ThingID, out int originalThickness))
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
                "close the startup developer log before the full signal-fire player workflow",
                "LudeonTK.EditWindow_Log");
        }

        int burnoutInspectionTick = 0;
        int firstCompletionTick = 0;
        int misunderstoodStartTick = 0;
        int misunderstoodCompletionTick = 0;
        int interruptedStartTick = 0;
        int interruptedCompletionTick = 0;
        int externallyDestroyedStartTick = 0;
        int secondCompletionTick = 0;
        int thirdCompletionTick = 0;
        int cooldownStartTick = 0;
        int cooldownCompletionTick = 0;
        int cooldownGoodwill = 0;
        int cooldownAidTick = 0;
        int ashBeforeUnnoticed = 0;
        int ashBeforeMisunderstood = 0;
        int ashBeforeInterrupted = 0;
        int ashBeforeDelayed = 0;
        int ashBeforeCooldown = 0;
        int delayedDueTick = 0;
        int delayedWarriorCount = 0;
        int delayedExpectedGoodwillChange = 0;
        int immediateExpectedGoodwillChange = 0;
        yield return new ScreenshotModeActionStep(
            "hide interface chrome for the cold signal-hearth visual catalog",
            enabled: true);
        foreach (EndToEndStep step in CaptureVisualZooms("idle cold", includeParticipants: false))
        {
            yield return step;
        }
        yield return new ScreenshotModeActionStep(
            "restore interface chrome after the cold signal-hearth visual catalog",
            enabled: false);
        yield return new CameraActionStep("frame the cold signal hearth for player input", new[] { fire.ThingID }, 320);
        yield return new SelectionActionStep(
            "clear every selected pawn before the native no-pawn right-click",
            Array.Empty<string>(),
            additive: false);
        yield return new AssertionStep("the spawned fire retained its no-pawn input patch before player input", _ =>
        {
            SignalFireMod? modHandle = LoadedModManager.GetMod<SignalFireMod>();
            EndToEndAssert.True(modHandle is not null,
                "RimWorld must discover the product's public Mod bootstrap. Live Mod handles: " +
                string.Join(", ", LoadedModManager.ModHandles.Select(handle => handle.GetType().FullName)));
            EndToEndAssert.True(NoPawnSignalFireMenuPatch.IsInstalled,
                "The spawned signal fire must retain its exact owner-scoped no-pawn prefix before player input.");
        });
        yield return new NoPawnMapRightClickActionStep(
            "invoke the native in-process signal-hearth right-click with no pawn selected while minimized",
            fire.ThingID);
        yield return new WaitUntilStep(
            "the no-pawn post-window Selector route opens a native map float menu",
            _ => Find.WindowStack.Windows.OfType<FloatMenu>().Count(menu => menu.IsOpen) == 1,
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new AssertionStep("the native no-pawn menu exposes the signal contact action", _ =>
        {
            FloatMenuOption[] options = OpenMenuOptions();
            EndToEndAssert.Equal(1, options.Count(option =>
                    !option.Disabled && string.Equals(option.Label, topLevelLabel, StringComparison.Ordinal)),
                "The faithful in-process zero-selected-pawn selector event must expose exactly one enabled contact action.");
        });
        yield return new ScreenshotStep(
            "native in-process no-pawn right-click exposes the signal contact action",
            Array.Empty<string>(),
            0);
        yield return new CurrentFloatMenuActionStep(
            "open the no-pawn contact submenu",
            topLevelLabel,
            expectReplacementMenu: true,
            captureSoleUnownedMenu: true);
        yield return new AssertionStep("empty submenu has the requested disabled explanation", _ =>
        {
            FloatMenuOption[] options = OpenMenuOptions();
            EndToEndAssert.Equal(1, options.Length, "The no-contact submenu must contain exactly one explanation.");
            EndToEndAssert.Equal(
                "no allied neolithic or medieval settlements nearby",
                options[0].Label,
                "The no-contact text must match the player contract exactly.");
            EndToEndAssert.True(options[0].Disabled, "The no-contact explanation must be inactive.");
        });
        yield return new ScreenshotStep("disabled no-allies submenu reached through no-pawn right-click", Array.Empty<string>(), 0);
        yield return new WindowCancelActionStep("close the no-contact submenu", "Verse.FloatMenu");

        yield return new AssertionStep("arrange the first allied nearby settlement for a pre-use cancellation", _ =>
        {
            SetPlayerRelation(delayedFaction, FactionRelationKind.Ally);
            delayedFaction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame -
                                                         MilitaryAidAvailabilityPolicy.CooldownTicks;
            SetAllParticipantSkills(20);
        });
        foreach (EndToEndStep step in StartSignal(
                     context,
                     "gathering-cancelled",
                     delayedFaction,
                     new[] { menuActor.ThingID },
                     _ => { },
                     cancelBeforeActive: true))
        {
            yield return step;
        }
        yield return new AssertionStep("arrange the unnoticed skill band on the preserved unused fire", _ =>
        {
            SetAllParticipantSkills(2);
            ashBeforeUnnoticed = AshThickness();
        });
        foreach (EndToEndStep step in StartSignal(
                     context,
                     "unnoticed",
                     delayedFaction,
                     actorIds: Array.Empty<string>(),
                     startTick => firstStartTick = startTick,
                     reloadDuringActive: true))
        {
            yield return step;
        }
        yield return new WaitUntilStep(
            "the unnoticed 600-tick signal burns out without warriors",
            _ => !fire.SignalComp.IsBusy,
            new EndToEndDeadline(900, 900, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep("bound the frame-polled unnoticed terminal observation", _ =>
        {
            firstCompletionTick = Find.TickManager.TicksGame;
            int observedDuration = firstCompletionTick - firstStartTick;
            EndToEndAssert.True(observedDuration >= 600 && observedDuration <= 610,
                $"Frame polling must not observe an early terminal state and may lag the exact transition by at most 10 ticks; " +
                $"observed {observedDuration}. The component's exact completed duration is asserted separately.");
        });
        yield return new AssertionStep(
            "allow the unnoticed terminal flecks to fade",
            _ => burnoutInspectionTick = Find.TickManager.TicksGame + 120);
        yield return new WaitUntilStep(
            "the unnoticed hearth is visibly cold after the terminal flecks fade",
            _ => Find.TickManager.TicksGame >= burnoutInspectionTick,
            new EndToEndDeadline(180, 180, TimeSpan.FromSeconds(10)));
        yield return new TimeControlActionStep("pause after the unnoticed result", paused: true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("unnoticed result creates soot and no aid", _ =>
        {
            firstDuration = fire.SignalComp.LastCompletedDurationTicks;
            EndToEndAssert.Equal(600, firstDuration, "The active signal must last exactly 600 game ticks.");
            EndToEndAssert.Equal(0, NewFactionPawnCount(delayedFaction),
                "An unnoticed signal must not create allied warriors.");
            EndToEndAssert.Equal(16, AshThickness() - ashBeforeUnnoticed,
                "The first burned-out signal must leave exactly 16 ash thickness.");
            AssertCurrentFireWasConsumed("unnoticed");
        });
        yield return new ScreenshotStep("unnoticed single-use fire is gone with cleanable soot left behind", Array.Empty<string>(), 0);

        yield return new AssertionStep("arrange a fresh prepared fire and the misunderstood quality band", _ =>
        {
            SpawnPreparedFire();
            SetAllParticipantSkills(6);
            ashBeforeMisunderstood = AshThickness();
        });
        yield return new TimeControlActionStep("resume for the misunderstood signal", paused: false, EndToEndGameSpeed.Normal);
        foreach (EndToEndStep step in StartSignal(
                     context,
                     "misunderstood",
                     delayedFaction,
                     new[] { menuActor.ThingID },
                     startTick => misunderstoodStartTick = startTick))
        {
            yield return step;
        }
        yield return new WaitUntilStep(
            "the misunderstood signal completes without warriors",
            _ => !fire.SignalComp.IsBusy,
            new EndToEndDeadline(900, 900, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep("measure the misunderstood terminal tick independently", _ =>
        {
            int observedCompletionTick = Find.TickManager.TicksGame;
            int observedDuration = observedCompletionTick - misunderstoodStartTick;
            EndToEndAssert.True(observedDuration >= 600 && observedDuration <= 610,
                "Frame polling must not observe an early misunderstood terminal state and may lag " +
                $"the exact transition by at most 10 ticks; observed {observedDuration}.");
            EndToEndAssert.Equal(600, fire.SignalComp.LastCompletedDurationTicks,
                "The misunderstood active signal must last exactly 600 component ticks.");
            misunderstoodCompletionTick = misunderstoodStartTick + fire.SignalComp.LastCompletedDurationTicks;
            burnoutInspectionTick = Find.TickManager.TicksGame + 120;
        });
        yield return new WaitUntilStep(
            "the misunderstood terminal flecks fade",
            _ => Find.TickManager.TicksGame >= burnoutInspectionTick,
            new EndToEndDeadline(180, 180, TimeSpan.FromSeconds(10)));
        yield return new TimeControlActionStep("pause after the misunderstood result", paused: true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("misunderstood result adds soot without aid", _ =>
        {
            EndToEndAssert.Equal(0, NewFactionPawnCount(delayedFaction),
                "A misunderstood signal must not create allied warriors.");
            EndToEndAssert.Equal(16, AshThickness() - ashBeforeMisunderstood,
                "The misunderstood signal must add exactly 16 ash thickness after its fresh-fire fixture.");
            AssertCurrentFireWasConsumed("misunderstood");
        });

        yield return new AssertionStep("arrange a fresh prepared fire for native interruption", _ =>
        {
            SpawnPreparedFire();
            SetAllParticipantSkills(20);
            ashBeforeInterrupted = AshThickness();
        });
        yield return new TimeControlActionStep("resume for the interrupted signal", paused: false, EndToEndGameSpeed.Normal);
        foreach (EndToEndStep step in StartSignal(
                     context,
                     "interrupted",
                     delayedFaction,
                     new[] { menuActor.ThingID },
                     startTick => interruptedStartTick = startTick))
        {
            yield return step;
        }
        yield return ToggleDraft(context, menuActor, "draft one active signaler through the native pawn command", expectedCurrentState: false);
        yield return new WaitUntilStep(
            "draft interruption cancels the shared signal once",
            _ => !fire.SignalComp.IsBusy,
            new EndToEndDeadline(180, 180, TimeSpan.FromSeconds(10)));
        yield return new AssertionStep("measure the interrupted terminal tick", _ =>
        {
            interruptedCompletionTick = Find.TickManager.TicksGame;
            EndToEndAssert.True(interruptedCompletionTick - interruptedStartTick is >= 170 and < 600,
                "The native draft action must terminate the already-visible signal before its normal end tick.");
            burnoutInspectionTick = Find.TickManager.TicksGame + 120;
        });
        yield return new WaitUntilStep(
            "the interrupted terminal flecks fade",
            _ => Find.TickManager.TicksGame >= burnoutInspectionTick,
            new EndToEndDeadline(180, 180, TimeSpan.FromSeconds(10)));
        yield return new TimeControlActionStep("pause after the interrupted result", paused: true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("interruption is idempotent and still cleans up", _ =>
        {
            EndToEndAssert.Equal(0, NewFactionPawnCount(delayedFaction),
                "An interrupted signal must request no warriors.");
            EndToEndAssert.Equal(16, AshThickness() - ashBeforeInterrupted,
                "The interrupted signal must add exactly one 16-thickness cleanup.");
            AssertCurrentFireWasConsumed("interrupted");
        });
        yield return new ScreenshotStep("draft-interrupted single-use fire is gone with one soot cleanup", Array.Empty<string>(), 0);
        yield return ToggleDraft(context, menuActor, "undraft the reporting signaler after cleanup", expectedCurrentState: true);

        int ashBeforeExternalDestruction = 0;
        int warriorsBeforeExternalDestruction = 0;
        yield return new AssertionStep("arrange a fresh prepared fire for native active deconstruction", _ =>
        {
            SpawnPreparedFire();
            SetAllParticipantSkills(20);
            ashBeforeExternalDestruction = AshThickness();
            warriorsBeforeExternalDestruction = NewFactionPawnCount(delayedFaction);
        });
        yield return new TimeControlActionStep(
            "resume for the externally deconstructed active signal",
            paused: false,
            EndToEndGameSpeed.Normal);
        foreach (EndToEndStep step in StartSignal(
                     context,
                     "externally-deconstructed",
                     delayedFaction,
                     new[] { menuActor.ThingID },
                     startTick => externallyDestroyedStartTick = startTick))
        {
            yield return step;
        }
        EndToEndGizmoOption deconstruct = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { fire.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled &&
                              option.Interaction == EndToEndGizmoInteraction.Invoke &&
                              option.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new GizmoActionStep(
            "designate the actively burning single-use fire for native deconstruction",
            new[] { fire.ThingID },
            deconstruct.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: deconstruct.StableId);
        Pawn deconstructionPawn = supportPawns.Single();
        EndToEndFloatMenuOption[] prioritizeDeconstruction = context
            .GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(deconstructionPawn.ThingID, fire.ThingID)
            .Where(option => !option.Disabled &&
                             option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0 &&
                             option.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, prioritizeDeconstruction.Length,
            "The capable support pawn must expose one enabled native prioritize-deconstruction order on the active fire.");
        yield return new FloatMenuActionStep(
            "prioritize ordinary deconstruction of the active signal fire",
            deconstructionPawn.ThingID,
            fire.ThingID,
            prioritizeDeconstruction[0].StableId);
        yield return new TimeControlActionStep(
            "run ordinary active-fire deconstruction",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "ordinary Construction externally destroys the active signal fire",
            _ => fire.Destroyed,
            new EndToEndDeadline(900, 1_200, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after native active-fire deconstruction",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep("external active destruction resolves once inside the bounded footprint", _ =>
        {
            EndToEndAssert.True(fire.SignalComp.LastCompletedDurationTicks is > 0 and < 600,
                "Native deconstruction must interrupt an active performance before its ordinary terminal tick.");
            EndToEndAssert.True(Find.TickManager.TicksGame - externallyDestroyedStartTick < 600,
                "The external destruction observation must occur before the normal 600-tick completion.");
            EndToEndAssert.Equal(16, AshThickness() - ashBeforeExternalDestruction,
                "PostDeSpawn cleanup must add exactly 16 ash thickness once.");
            EndToEndAssert.Equal(warriorsBeforeExternalDestruction, NewFactionPawnCount(delayedFaction),
                "External destruction must not request allied warriors.");
            AssertCurrentFireWasConsumed("externally-deconstructed");
        });
        yield return new ScreenshotStep(
            "native deconstruction removed the active fire and left one bounded soot cleanup",
            Array.Empty<string>(),
            0);

        yield return new AssertionStep("arrange a fresh prepared fire and the delayed quality band", _ =>
        {
            SpawnPreparedFire();
            SetAllParticipantSkills(8);
            ashBeforeDelayed = AshThickness();
        });
        yield return new TimeControlActionStep("resume for the delayed signal", paused: false, EndToEndGameSpeed.Normal);
        foreach (EndToEndStep step in StartSignal(
                     context,
                     "delayed",
                     delayedFaction,
                     new[] { menuActor.ThingID },
                     startTick => secondStartTick = startTick))
        {
            yield return step;
        }
        yield return new WaitUntilStep(
            "the delayed signal completes before its answer",
            _ => !fire.SignalComp.IsBusy,
            new EndToEndDeadline(900, 900, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep("measure the delayed terminal tick and saved due tick", _ =>
        {
            int observedCompletionTick = Find.TickManager.TicksGame;
            int observedDuration = observedCompletionTick - secondStartTick;
            EndToEndAssert.True(observedDuration >= 600 && observedDuration <= 610,
                "Frame polling must not observe an early delayed terminal state and may lag " +
                $"the exact transition by at most 10 ticks; observed {observedDuration}.");
            EndToEndAssert.Equal(600, fire.SignalComp.LastCompletedDurationTicks,
                "The delayed active signal must last exactly 600 component ticks.");
            secondCompletionTick = secondStartTick + fire.SignalComp.LastCompletedDurationTicks;
            delayedDueTick = ScheduledDueTick(delayedFaction);
            EndToEndAssert.True(delayedDueTick - secondCompletionTick is >= 600 and <= 3_600,
                "The sampled delayed response must be due 600 through 3,600 ticks after completion.");
        });
        yield return new AssertionStep(
            "allow the delayed terminal flecks to fade",
            _ => burnoutInspectionTick = Find.TickManager.TicksGame + 120);
        yield return new WaitUntilStep(
            "the delayed hearth is visibly cold before its answer",
            _ => Find.TickManager.TicksGame >= burnoutInspectionTick,
            new EndToEndDeadline(180, 180, TimeSpan.FromSeconds(10)));
        yield return new TimeControlActionStep("pause at delayed completion", paused: true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("delayed completion has not spawned immediate warriors", _ =>
        {
            secondDuration = fire.SignalComp.LastCompletedDurationTicks;
            EndToEndAssert.Equal(600, secondDuration, "The delayed active signal must last exactly 600 ticks.");
            EndToEndAssert.Equal(0, NewFactionPawnCount(delayedFaction),
                "Delayed aid must not arrive in the completion tick.");
            EndToEndAssert.Equal(16, AshThickness() - ashBeforeDelayed,
                "The delayed terminal signal must add exactly 16 ash thickness after its fresh-fire fixture.");
            AssertCurrentFireWasConsumed("delayed");
            delayedGoodwillBeforeAid = delayedFaction.PlayerGoodwill;
            delayedExpectedGoodwillChange = Faction.OfPlayer.CalculateAdjustedGoodwillChange(
                delayedFaction,
                -25);
        });

        string menuActorId = menuActor.ThingID;
        string[] generatedIds = generatedColonists.Select(pawn => pawn.ThingID).ToArray();
        int delayedFactionId = delayedFaction.loadID;
        int immediateFactionId = immediateFaction.loadID;
        int[] settlementIds = settlements.Select(settlement => settlement.ID).ToArray();
        yield return new SaveLoadActionStep(
            "save and reload while the delayed warband response is pending",
            "immersive-signal-fire-delayed-response");
        yield return new AssertionStep("rebind the exact signal fixtures after reload", _ =>
        {
            RebindAfterLoad(
                menuActorId,
                generatedIds,
                delayedFactionId,
                immediateFactionId,
                settlementIds);
            EndToEndAssert.Equal(delayedDueTick, ScheduledDueTick(delayedFaction),
                "Reload must retain the already-sampled due tick without resampling.");
            EndToEndAssert.Equal(0, NewFactionPawnCount(delayedFaction),
                "Reloading a pending response must not deliver it early.");
        });
        yield return new TimeControlActionStep("resume while the delayed warband travels", paused: false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the saved delayed native aid request produces allied warriors",
            _ => NewFactionPawnCount(delayedFaction) > 0,
            new EndToEndDeadline(4_000, 4_000, TimeSpan.FromSeconds(75)));
        yield return new AssertionStep("capture the one delayed native warrior response", _ =>
        {
            delayedWarriorCount = NewFactionPawnCount(delayedFaction);
            EndToEndAssert.True(delayedWarriorCount > 0,
                "The persisted delayed request must produce player-observable allied warriors.");
            delayedGoodwillAfterAid = delayedFaction.PlayerGoodwill;
            delayedAidTickAfter = delayedFaction.lastMilitaryAidRequestTick;
            EndToEndAssert.Equal(
                delayedGoodwillBeforeAid + delayedExpectedGoodwillChange,
                delayedGoodwillAfterAid,
                "The delayed aid request must apply Core's native adjusted -25 goodwill request once.");
            EndToEndAssert.Equal(delayedDueTick, delayedAidTickAfter,
                "The delayed response must stamp its native military-aid cooldown on its exact due tick.");
            EndToEndAssert.Equal(0, ScheduledResponseCount(delayedFaction),
                "The due delayed response must be removed from the persisted queue before observation.");
            burnoutInspectionTick = Find.TickManager.TicksGame + 300;
        });
        yield return new WaitUntilStep(
            "observe that the persisted delayed request is consumed exactly once",
            _ => Find.TickManager.TicksGame >= burnoutInspectionTick,
            new EndToEndDeadline(360, 360, TimeSpan.FromSeconds(10)));
        yield return new AssertionStep("no second delayed aid mutation is scheduled", _ =>
        {
            int fullyArrivedWarriorCount = NewFactionPawnCount(delayedFaction);
            EndToEndAssert.True(fullyArrivedWarriorCount >= delayedWarriorCount,
                "The first observed delayed warrior must remain while the same native raid finishes arriving.");
            delayedWarriorCount = fullyArrivedWarriorCount;
            EndToEndAssert.Equal(delayedGoodwillAfterAid, delayedFaction.PlayerGoodwill,
                "Exactly-once delivery must not apply a second native-adjusted goodwill charge after arrival.");
            EndToEndAssert.Equal(delayedAidTickAfter, delayedFaction.lastMilitaryAidRequestTick,
                "Exactly-once delivery must not stamp the military-aid cooldown a second time.");
            EndToEndAssert.Equal(0, ScheduledResponseCount(delayedFaction),
                "Exactly-once delivery must leave no duplicate persisted response.");
        });
        yield return new CameraActionStep(
            "frame the delayed allied warriors that answered the consumed signal fire",
            NewFactionPawnIds(delayedFaction),
            160);
        yield return new ScreenshotStep(
            "delayed allied warriors answer after the single-use fire was consumed",
            NewFactionPawnIds(delayedFaction),
            260);

        yield return new AssertionStep("arrange a fresh prepared fire, second allied faction, and immediate quality band", _ =>
        {
            SpawnPreparedFire();
            SetPlayerRelation(immediateFaction, FactionRelationKind.Ally);
            immediateFaction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame -
                                                           MilitaryAidAvailabilityPolicy.CooldownTicks;
            SetAllParticipantSkills(20);
            ashBeforeImmediate = AshThickness();
            immediateGoodwillBeforeAid = immediateFaction.PlayerGoodwill;
            immediateExpectedGoodwillChange = Faction.OfPlayer.CalculateAdjustedGoodwillChange(
                immediateFaction,
                -25);
        });
        foreach (EndToEndStep step in StartSignal(
                     context,
                     "immediate",
                     immediateFaction,
                     new[] { menuActor.ThingID },
                     startTick => thirdStartTick = startTick))
        {
            yield return step;
        }
        yield return new WaitUntilStep(
            "observe immediate signal completion independently of native warrior arrival",
            _ => !fire.SignalComp.IsBusy,
            new EndToEndDeadline(900, 900, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep("measure the immediate terminal tick independently", _ =>
        {
            int observedCompletionTick = Find.TickManager.TicksGame;
            int observedDuration = observedCompletionTick - thirdStartTick;
            EndToEndAssert.True(observedDuration >= 600 && observedDuration <= 610,
                "Frame polling must not observe an early immediate terminal state and may lag " +
                $"the exact transition by at most 10 ticks; observed {observedDuration}.");
            EndToEndAssert.Equal(600, fire.SignalComp.LastCompletedDurationTicks,
                "The immediate active signal must last exactly 600 component ticks.");
            thirdCompletionTick = thirdStartTick + fire.SignalComp.LastCompletedDurationTicks;
            EndToEndAssert.Equal(16, AshThickness() - ashBeforeImmediate,
                "The immediate terminal transition must add exactly 16 ash thickness.");
        });
        yield return new WaitUntilStep(
            "observe native allied warrior presence independently after measuring completion",
            _ => NewFactionPawnCount(immediateFaction) > 0,
            new EndToEndDeadline(600, 600, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep(
            "allow the immediate terminal flecks to fade",
            _ => burnoutInspectionTick = Find.TickManager.TicksGame + 120);
        yield return new WaitUntilStep(
            "the immediate hearth is visibly cold after the terminal flecks fade",
            _ => Find.TickManager.TicksGame >= burnoutInspectionTick,
            new EndToEndDeadline(180, 180, TimeSpan.FromSeconds(10)));
        yield return new TimeControlActionStep("pause for final reviewed evidence", paused: true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("immediate result and native aid mutations are exact", _ =>
        {
            thirdDuration = fire.SignalComp.LastCompletedDurationTicks;
            EndToEndAssert.Equal(600, thirdDuration, "The immediate active signal must last exactly 600 ticks.");
            EndToEndAssert.True(NewFactionPawnCount(immediateFaction) > 0,
                "Immediate success must create player-observable allied warriors.");
            EndToEndAssert.Equal(
                immediateGoodwillBeforeAid + immediateExpectedGoodwillChange,
                immediateFaction.PlayerGoodwill,
                "Immediate aid must apply Core's native adjusted -25 goodwill request once.");
            immediateAidTickAfter = immediateFaction.lastMilitaryAidRequestTick;
            EndToEndAssert.Equal(thirdCompletionTick, immediateAidTickAfter,
                "Immediate native-equivalent aid must stamp the military-aid cooldown on the exact completion tick.");
            AssertCurrentFireWasConsumed("immediate");
        });
        yield return new CameraActionStep(
            "frame the immediate allied warriors after the prepared fire is consumed",
            NewFactionPawnIds(immediateFaction),
            160);
        yield return new ScreenshotStep(
            "immediate allied warriors answer after the prepared fire is consumed",
            NewFactionPawnIds(immediateFaction),
            260);

        yield return new AssertionStep("arrange a fresh prepared fire while native military aid is cooling down", _ =>
        {
            SpawnPreparedFire();
            cooldownGoodwill = immediateFaction.PlayerGoodwill;
            cooldownAidTick = immediateFaction.lastMilitaryAidRequestTick;
            ashBeforeCooldown = AshThickness();
            EndToEndAssert.True(
                MilitaryAidAvailabilityPolicy.RemainingCooldownTicks(
                    cooldownAidTick,
                    Find.TickManager.TicksGame) > 0,
                "The same allied faction must still be inside its native 60,000-tick aid cooldown.");
        });
        foreach (EndToEndStep step in StartSignal(
                     context,
                     "cooldown-rejected",
                     immediateFaction,
                     new[] { menuActor.ThingID },
                     startTick => cooldownStartTick = startTick))
        {
            yield return step;
        }
        yield return new WaitUntilStep(
            "the cooldown-rejected signal finishes without another aid mutation",
            _ => !fire.SignalComp.IsBusy,
            new EndToEndDeadline(900, 900, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep("native cooldown rejection is visible and mutation-free", _ =>
        {
            int observedCompletionTick = Find.TickManager.TicksGame;
            int observedDuration = observedCompletionTick - cooldownStartTick;
            EndToEndAssert.True(observedDuration >= 600 && observedDuration <= 610,
                "Frame polling must not observe an early cooldown-rejected terminal state and may lag " +
                $"the exact transition by at most 10 ticks; observed {observedDuration}.");
            EndToEndAssert.Equal(600, fire.SignalComp.LastCompletedDurationTicks,
                "The cooldown-rejected signal must still perform the full 600-tick player workflow.");
            cooldownCompletionTick = cooldownStartTick + fire.SignalComp.LastCompletedDurationTicks;
            EndToEndAssert.Equal(cooldownGoodwill, immediateFaction.PlayerGoodwill,
                "An active military-aid cooldown must reject before charging goodwill.");
            EndToEndAssert.Equal(cooldownAidTick, immediateFaction.lastMilitaryAidRequestTick,
                "An active military-aid cooldown must not overwrite the prior request tick.");
            EndToEndAssert.Equal(0, ScheduledResponseCount(immediateFaction),
                "An immediate cooldown rejection must not create a delayed response entry.");
            EndToEndAssert.Equal(16, AshThickness() - ashBeforeCooldown,
                "The rejected response must still leave one ordinary 16-thickness soot cleanup.");
            AssertCurrentFireWasConsumed("cooldown-rejected");

            int remaining = MilitaryAidAvailabilityPolicy.RemainingCooldownTicks(
                cooldownAidTick,
                cooldownCompletionTick);
            string expectedMessage = "ImmersiveSignalFire_AidCooldown"
                .Translate(remaining.ToStringTicksToPeriod())
                .CapitalizeFirst();
            var messages = LiveMessages?.GetValue(null) as IEnumerable<Message>;
            EndToEndAssert.NotNull(messages,
                "RimWorld's live message collection must retain its 1.6 shape.");
            EndToEndAssert.True(messages!.Any(message =>
                    string.Equals(message.text, expectedMessage, StringComparison.Ordinal)),
                "The player must see the localized wait message produced by the cooldown rejection.");
        });
        yield return new ScreenshotStep(
            "cooldown rejection displays its wait message without aid mutation",
            Array.Empty<string>(),
            0);
        yield return new AssertionStep(
            "allow the cooldown-rejected terminal flecks to fade",
            _ => burnoutInspectionTick = Find.TickManager.TicksGame + 120);
        yield return new WaitUntilStep(
            "the cooldown-rejected hearth is visibly cold",
            _ => Find.TickManager.TicksGame >= burnoutInspectionTick,
            new EndToEndDeadline(180, 180, TimeSpan.FromSeconds(10)));
        yield return new TimeControlActionStep(
            "pause after the cooldown rejection",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "cooldown-rejected single-use fire is gone with soot",
            Array.Empty<string>(),
            0);
        yield return new AssertionStep("spawn one fresh unused fire for damaged sprite inspection", _ =>
            SpawnPreparedFire());
        yield return new SupportingHitPointFixtureActionStep(
            "prepare the unused single-use hearth's supporting damaged visual state",
            new[] { new EndToEndHitPointFixture(fire.ThingID, 0.35f) });
        yield return new ScreenshotModeActionStep(
            "hide interface chrome for the damaged signal-hearth visual catalog",
            enabled: true);
        foreach (EndToEndStep step in CaptureVisualZooms("damaged prepared unused", includeParticipants: false))
        {
            yield return step;
        }
        yield return new ScreenshotModeActionStep(
            "restore interface chrome after the damaged signal-hearth visual catalog",
            enabled: false);
        yield return new CheckpointStep("complete signal-fire player workflow", _ => new Dictionary<string, string>
        {
            ["fireThingId"] = fire.ThingID,
            ["delayedFaction"] = delayedFaction.Name,
            ["immediateFaction"] = immediateFaction.Name,
            ["unnoticedDurationTicks"] = firstDuration.ToString(),
            ["delayedDurationTicks"] = secondDuration.ToString(),
            ["immediateDurationTicks"] = thirdDuration.ToString(),
            ["misunderstoodDurationTicks"] = (misunderstoodCompletionTick - misunderstoodStartTick).ToString(),
            ["interruptedDurationTicks"] = (interruptedCompletionTick - interruptedStartTick).ToString(),
            ["delayedDueTickPreserved"] = delayedDueTick.ToString(),
            ["ashThicknessAdded"] = (AshThickness() - baselineAshThickness).ToString(),
            ["delayedWarriors"] = delayedWarriorCount.ToString(),
            ["immediateWarriors"] = NewFactionPawnCount(immediateFaction).ToString(),
            ["delayedGoodwillCost"] = (delayedGoodwillBeforeAid - delayedGoodwillAfterAid).ToString(),
            ["delayedAidTick"] = delayedAidTickAfter.ToString(),
            ["immediateGoodwillCost"] = (immediateGoodwillBeforeAid - immediateFaction.PlayerGoodwill).ToString(),
            ["immediateAidTick"] = immediateAidTickAfter.ToString(),
            ["cooldownRejected"] = (cooldownGoodwill == immediateFaction.PlayerGoodwill &&
                                     cooldownAidTick == immediateFaction.lastMilitaryAidRequestTick).ToString(),
            ["cooldownRemainingTicks"] = MilitaryAidAvailabilityPolicy.RemainingCooldownTicks(
                cooldownAidTick,
                cooldownCompletionTick).ToString(),
            ["optionalBlanketsEnabled"] = OptionalToolsAdapter.Enabled.ToString(),
        });
    }

    private IEnumerable<EndToEndStep> StartSignal(
        IEndToEndContext context,
        string outcomeName,
        Faction contactFaction,
        IReadOnlyList<string> actorIds,
        Action<int> captureStartTick,
        bool cancelBeforeActive = false,
        bool reloadDuringActive = false)
    {
        string contactLabel = string.Empty;
        int gatherInspectionTick = 0;
        int activeStartTick = -1;
        yield return new CameraActionStep(
            $"frame the prepared single-use fire for the {outcomeName} player workflow",
            new[] { fire.ThingID },
            320);
        yield return new SelectionActionStep(
            $"set the declared pawn selection before the native {outcomeName} right-click",
            actorIds,
            additive: false);
        if (actorIds.Count == 0)
        {
            yield return new NoPawnMapRightClickActionStep(
                $"invoke the native minimized no-pawn right-click for {outcomeName}",
                fire.ThingID);
        }
        else
        {
            yield return new MapFloatMenuOpenActionStep(
                $"open the exact selected-pawn native map menu for {outcomeName}",
                fire.ThingID,
                actorIds);
        }
        yield return new WaitUntilStep(
            $"the Selector route opens a native map menu for {outcomeName}",
            _ => Find.WindowStack.Windows.OfType<FloatMenu>().Count(menu => menu.IsOpen) == 1,
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new AssertionStep(
            $"the native map menu exposes the contact action for {outcomeName}",
            _ => EndToEndAssert.Equal(1, OpenMenuOptions().Count(option =>
                    !option.Disabled && string.Equals(option.Label, topLevelLabel, StringComparison.Ordinal)),
                actorIds.Count == 0
                    ? "The faithful in-process no-pawn selector event must expose exactly one enabled contact action."
                    : "The exact selected-pawn GetOptions projection must expose exactly one enabled contact action."));
        yield return new CurrentFloatMenuActionStep(
            $"open the allied contact submenu for the {outcomeName} signal",
            topLevelLabel,
            expectReplacementMenu: true,
            captureSoleUnownedMenu: actorIds.Count == 0);
        yield return new ScreenshotStep(
            $"allied nearby settlement dropdown for the {outcomeName} signal",
            Array.Empty<string>(),
            0);
        yield return new AssertionStep(
            $"the allied submenu has one live contact for {contactFaction.Name}",
            _ =>
            {
                FloatMenuOption[] options = OpenMenuOptions();
                FloatMenuOption[] matches = options
                    .Where(option => !option.Disabled &&
                                     option.Label.StartsWith(contactFaction.Name + " (", StringComparison.Ordinal))
                    .ToArray();
                EndToEndAssert.Equal(1, matches.Length,
                    "The contact submenu must have one enabled entry for the intended allied faction. " +
                    $"Relation={contactFaction.PlayerRelationKind}; " +
                    $"settlementDistance={Find.WorldGrid.ApproxDistanceInTiles(map.Tile, settlements.First(settlement => settlement.Faction == contactFaction).Tile)}; " +
                    "options=" + string.Join(" | ", options.Select(option => $"{option.Label} [disabled={option.Disabled}]")));
                contactLabel = matches[0].Label;
            });
        yield return new CurrentFloatMenuActionStep(
            $"choose {contactLabel} without opening the comms console",
            contactLabel);
        yield return new WaitUntilStep(
            $"the shared begin-lord-job dialog opens for the {outcomeName} signal",
            _ => Find.WindowStack.Windows.Any(window => window.IsOpen &&
                window.GetType().FullName == "ImmersiveSignalFire.UI.Dialog_BeginSignalFire"),
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new ScreenshotStep(
            $"shared participant and quality dialog for the {outcomeName} band",
            Array.Empty<string>(),
            0);
        if (cancelBeforeActive)
        {
            yield return new TimeControlActionStep(
                $"pause before accepting the pre-use {outcomeName} gathering",
                paused: true,
                EndToEndGameSpeed.Normal);
        }
        yield return new WindowAcceptActionStep(
            $"accept the {outcomeName} signal through the shared dialog",
            "ImmersiveSignalFire.UI.Dialog_BeginSignalFire");
        if (cancelBeforeActive)
        {
            int ashBeforeCancellation = AshThickness();
            yield return new AssertionStep("the accepted pre-use signal is gathering but not lit", _ =>
            {
                EndToEndAssert.True(fire.SignalComp.IsBusy && !fire.SignalComp.IsActive,
                    "Paused dialog acceptance must assign native gathering jobs before any active flame or smoke tick.");
                EndToEndAssert.True(fire.Spawned && !fire.Destroyed,
                    "The unused prepared fire must remain spawned during gathering.");
            });
            yield return ToggleDraft(
                context,
                menuActor,
                "draft the gathering caller through the native pawn command before smoke begins",
                expectedCurrentState: false);
            yield return new WaitUntilStep(
                "the native draft action cancels gathering without lighting the fire",
                _ => !fire.SignalComp.IsBusy,
                new EndToEndDeadline(120, 1, TimeSpan.FromSeconds(10)));
            yield return new AssertionStep("pre-use cancellation preserves the prepared fire", _ =>
            {
                EndToEndAssert.True(fire.Spawned && !fire.Destroyed,
                    "A gathering cancellation before flame or smoke must leave the single-use fire available.");
                EndToEndAssert.Equal(ashBeforeCancellation, AshThickness(),
                    "A gathering cancellation before use must not create soot.");
            });
            yield return ToggleDraft(
                context,
                menuActor,
                "undraft the gathering caller after the preserved-fire check",
                expectedCurrentState: true);
            yield break;
        }
        yield return new TimeControlActionStep(
            $"run the {outcomeName} signal at the lowest active speed",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            $"start the bounded {outcomeName} gathering observation",
            _ => gatherInspectionTick = Find.TickManager.TicksGame + 120);
        yield return new WaitUntilStep(
            $"the {outcomeName} gathering phase reaches activity, aborts, or reaches its diagnostic bound",
            _ => fire.SignalComp.IsActive || !fire.SignalComp.IsBusy ||
                 Find.TickManager.TicksGame >= gatherInspectionTick,
            new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(15)));
        yield return new AssertionStep(
            $"all selected colonists begin the synchronized {outcomeName} signal job",
            _ => EndToEndAssert.True(
                fire.SignalComp.IsActive && fire.SignalComp.ActiveParticipants.Count >= 1,
                "The accepted signal must enter its active phase. " +
                $"busy={fire.SignalComp.IsBusy}; tick={Find.TickManager.TicksGame}; " +
                "participants=" + string.Join(" | ", generatedColonists.Select(pawn =>
                    $"{pawn.LabelShort}:pos={pawn.Position},job={pawn.CurJobDef?.defName ?? "none"},moving={pawn.pather?.MovingNow ?? false}"))));
        yield return new AssertionStep($"capture the exact {outcomeName} active start tick", _ =>
        {
            activeStartTick = Find.TickManager.TicksGame - fire.SignalComp.ActiveElapsedTicks;
            captureStartTick(activeStartTick);
            EndToEndAssert.Equal(3, fire.SignalComp.ActiveParticipants.Count,
                "The shared dialog must start one caller and two helpers in the same session.");
            if (actorIds.Count > 0)
            {
                EndToEndAssert.Equal(actorIds[0], fire.SignalComp.ActiveParticipants[0].ThingID,
                    "A pawn that invoked the menu must remain the locked caller through dialog acceptance.");
            }
            EndToEndAssert.True(OptionalToolsAdapter.Enabled,
                "The installed Show Me Your Tools fork must enable synchronized blanket rendering.");
            EndToEndAssert.False(Find.WindowStack.Windows.Any(window => window is Dialog_Negotiation),
                "Choosing a signal target must never open the comms negotiation window.");
        });
        if (string.Equals(outcomeName, "unnoticed", StringComparison.Ordinal))
        {
            yield return new TimeControlActionStep(
                "pause inside the first raised puff before active persistence",
                paused: true,
                EndToEndGameSpeed.Normal,
                atGameTick: activeStartTick + 15,
                deadline: new EndToEndDeadline(300, 300, TimeSpan.FromSeconds(10)));
            yield return new AssertionStep(
                "the paused pre-reload phase remains inside the owned emitter window",
                _ => EndToEndAssert.True(
                    MorseCadence.IsSmokeOn(fire.SignalComp.ActiveElapsedTicks),
                    $"The pre-reload phase slipped outside its bounded plume. elapsed={fire.SignalComp.ActiveElapsedTicks}"));
            int elapsedBeforeReload = -1;
            if (reloadDuringActive)
            {
                string activeFireId = fire.ThingID;
                string activeMenuActorId = menuActor.ThingID;
                string[] activeGeneratedIds = generatedColonists.Select(pawn => pawn.ThingID).ToArray();
                int activeDelayedFactionId = delayedFaction.loadID;
                int activeImmediateFactionId = immediateFaction.loadID;
                int[] activeSettlementIds = settlements.Select(settlement => settlement.ID).ToArray();
                elapsedBeforeReload = fire.SignalComp.ActiveElapsedTicks;
                yield return new SaveLoadActionStep(
                    "save and reload inside the same raised 21-tick smoke puff",
                    "immersive-signal-fire-active-performance");
                yield return new AssertionStep("rebind and retain the active mid-puff performance after reload", _ =>
                {
                    RebindAfterLoad(
                        activeMenuActorId,
                        activeGeneratedIds,
                        activeDelayedFactionId,
                        activeImmediateFactionId,
                        activeSettlementIds);
                    fire = map.listerThings.AllThings.OfType<Building_SignalFire>()
                        .Single(candidate => candidate.ThingID == activeFireId);
                    EndToEndAssert.True(fire.SignalComp.IsActive,
                        "Reload must preserve the active performance rather than consuming or resetting the fire early.");
                    EndToEndAssert.True(fire.SignalComp.ActiveElapsedTicks >= elapsedBeforeReload,
                        "Reload must preserve the active elapsed tick without rewinding the smoke cadence.");
                    EndToEndAssert.True(fire.SignalComp.ActiveElapsedTicks - elapsedBeforeReload <= 5,
                        $"The paused save/load action must not advance beyond its bounded preservation allowance. before={elapsedBeforeReload}; after={fire.SignalComp.ActiveElapsedTicks}");
                    EndToEndAssert.True(MorseCadence.IsSmokeOn(fire.SignalComp.ActiveElapsedTicks),
                        $"The paused mid-puff reload must preserve the same shared raised-blanket emitter phase. elapsed={fire.SignalComp.ActiveElapsedTicks}");
                    EndToEndAssert.Equal(3, fire.SignalComp.ActiveParticipants.Count,
                        "Reload must restore the caller and two helpers in the same active performance.");
                });
            }
            yield return new TimeControlActionStep(
                "resume the reloaded signal toward a lowered-blanket Morse gap",
                paused: false,
                EndToEndGameSpeed.Normal);
            yield return new TimeControlActionStep(
                "pause in the long lowered-blanket break between three-puff groups",
                paused: true,
                EndToEndGameSpeed.Normal,
                atGameTick: activeStartTick + 350,
                deadline: new EndToEndDeadline(2400, 360, TimeSpan.FromSeconds(15)));
            yield return new AssertionStep(
                "the cleared-source capture remains in the lowered interval before the next puff",
                _ =>
                {
                    EndToEndAssert.False(
                        MorseCadence.IsSmokeOn(fire.SignalComp.ActiveElapsedTicks),
                        $"The lowered capture slipped into a new plume. elapsed={fire.SignalComp.ActiveElapsedTicks}");
                    EndToEndAssert.True(
                        fire.SignalComp.ActiveElapsedTicks >= 340 && fire.SignalComp.ActiveElapsedTicks < 360,
                        $"The lowered capture must occur after the first three-puff group and before the next group. elapsed={fire.SignalComp.ActiveElapsedTicks}");
                });
            yield return new ScreenshotModeActionStep(
                "hide interface chrome for the lowered-gap evidence",
                enabled: true);
            yield return new CameraActionStep(
                "close gameplay zoom for the lowered-blanket non-emitting gap",
                new[] { fire.ThingID }.Concat(fire.SignalComp.ActiveParticipants.Select(pawn => pawn.ThingID)).ToArray(),
                60);
            yield return new ScreenshotStep(
                "blankets are lowered and no new smoke emits from the hearth during the Morse gap",
                Array.Empty<string>(),
                0);
            yield return new ScreenshotModeActionStep(
                "restore interface chrome after the lowered-gap evidence",
                enabled: false);
            yield return new TimeControlActionStep(
                "resume the signal toward the second three-puff group",
                paused: false,
                EndToEndGameSpeed.Normal);
            yield return new TimeControlActionStep(
                "pause on the final included tick of the fully formed second three-puff group",
                paused: true,
                EndToEndGameSpeed.Normal,
                atGameTick: activeStartTick + 500,
                deadline: new EndToEndDeadline(1800, 300, TimeSpan.FromSeconds(15)));
            yield return new AssertionStep(
                "the periodic capture remains inside the last raised-blanket emitter window",
                _ => EndToEndAssert.True(
                    MorseCadence.IsSmokeOn(fire.SignalComp.ActiveElapsedTicks),
                    $"The periodic capture slipped outside its bounded plume. elapsed={fire.SignalComp.ActiveElapsedTicks}"));
            yield return new ScreenshotModeActionStep(
                "hide interface chrome for the mature periodic signal-hearth visual catalog",
                enabled: true);
            foreach (EndToEndStep step in CaptureVisualZooms("restored dense dark-smoke puff and synchronized raised blankets", includeParticipants: true))
            {
                yield return step;
            }
            yield return new ScreenshotModeActionStep(
                "restore interface chrome after the mature periodic signal-hearth visual catalog",
                enabled: false);
            yield return new TimeControlActionStep(
                "resume the first signal after smoke-gap and periodic-puff comparison",
                paused: false,
                EndToEndGameSpeed.Normal);
        }
        else
        {
            yield return new WaitUntilStep(
                $"reach the restored compact dark-smoke plume after a raised-symbol start during the {outcomeName} signal",
                _ => fire.SignalComp.ActiveElapsedTicks >= 122 &&
                      MorseCadence.IsSmokeOn(fire.SignalComp.ActiveElapsedTicks),
                new EndToEndDeadline(1800, 300, TimeSpan.FromSeconds(10)));
            yield return new ScreenshotStep(
                $"three colonists lift synchronized blankets while the restored thick dark plume rises for {outcomeName}",
                Array.Empty<string>(),
                0);
        }
    }

    private IEnumerable<EndToEndStep> CaptureVisualZooms(string state, bool includeParticipants)
    {
        string[] targets = includeParticipants
            ? new[] { fire.ThingID }.Concat(fire.SignalComp.ActiveParticipants.Select(pawn => pawn.ThingID)).ToArray()
            : new[] { fire.ThingID };
        IEnumerable<(string zoom, int cameraPadding, int screenshotPadding)> zooms = includeParticipants
            ? new[]
            {
                ("close", 60, 150),
                ("far", 420, 440),
            }
            : new[]
            {
                ("close", 60, 150),
                ("ordinary", 420, 340),
                ("far", 440, 440),
            };
        foreach ((string zoom, int cameraPadding, int screenshotPadding) in zooms)
        {
            yield return new CameraActionStep(
                $"{zoom} gameplay zoom for the {state} signal hearth",
                targets,
                cameraPadding);
            yield return new ScreenshotStep(
                $"{zoom} gameplay zoom shows the {state} signal hearth on contrasting paved terrain",
                includeParticipants ? Array.Empty<string>() : targets,
                includeParticipants ? 0 : screenshotPadding);
        }
    }

    private void SetAllParticipantSkills(int level)
    {
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.Where(pawn =>
                     pawnSnapshots.Any(snapshot => snapshot.PawnId == pawn.ThingID) && !pawn.Destroyed))
        {
            int assignedLevel = generatedColonists.Any(candidate => candidate.ThingID == pawn.ThingID) ? level : 0;
            pawn.skills.GetSkill(SkillDefOf.Melee).Level = assignedLevel;
            pawn.skills.GetSkill(SkillDefOf.Social).Level = assignedLevel;
        }
    }

    private static GizmoActionStep ToggleDraft(
        IEndToEndContext context,
        Pawn pawn,
        string description,
        bool expectedCurrentState)
    {
        EndToEndGizmoOption[] options = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { pawn.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == expectedCurrentState &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, options.Length,
            "The exact signal participant must expose one native Draft toggle in the expected state.");
        return new GizmoActionStep(
            description,
            new[] { pawn.ThingID },
            options[0].RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: options[0].StableId);
    }

    private int ScheduledDueTick(Faction faction)
    {
        ScheduledSignalResponse[] matches = CurrentScheduledResponses()
            .Where(response => response.Faction?.loadID == faction.loadID)
            .ToArray();
        EndToEndAssert.Equal(1, matches.Length,
            "Exactly one delayed response must be scheduled for the selected faction.");
        return matches[0].DueTick;
    }

    private int ScheduledResponseCount(Faction faction)
    {
        return CurrentScheduledResponses().Count(response => response.Faction?.loadID == faction.loadID);
    }

    private List<ScheduledSignalResponse> CurrentScheduledResponses()
    {
        var responses = ScheduledResponses?.GetValue(map.GetComponent<MapComponent_SignalResponses>())
            as List<ScheduledSignalResponse>;
        EndToEndAssert.NotNull(responses,
            "The delayed-response collection must retain its RimWorld 1.6 mutable list shape.");
        return responses!;
    }

    private void RestoreEarlyRegisteredState(bool originalScreenshotMode)
    {
        map = Find.CurrentMap ?? map;
        foreach ((IntVec3 cell, TerrainDef terrain) in terrainSnapshots)
        {
            map.terrainGrid.SetTerrain(cell, terrain);
        }

        if (Find.ScreenshotModeHandler is not null)
        {
            Find.ScreenshotModeHandler.Active = originalScreenshotMode;
        }

        foreach (string generatedId in generatedColonists.Concat(supportPawns)
                     .Select(pawn => pawn.ThingID).ToArray())
        {
            Pawn? pawn = map.mapPawns.AllPawnsSpawned.FirstOrDefault(candidate => candidate.ThingID == generatedId);
            if (pawn is not null && !pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        foreach (Building_SignalFire currentFire in map.listerThings.AllThings
                     .OfType<Building_SignalFire>()
                     .Where(candidate => createdFireIds.Contains(candidate.ThingID))
                     .ToArray())
        {
            currentFire.Destroy(DestroyMode.Vanish);
        }

        foreach (Settlement current in settlements)
        {
            Settlement? loaded = Find.WorldObjects.AllWorldObjects.OfType<Settlement>()
                .FirstOrDefault(candidate => candidate.ID == current.ID);
            if (loaded is not null && !loaded.Destroyed)
            {
                loaded.Destroy();
            }
        }

        foreach ((int factionLoadId, int goodwill, int aidRequestTick) in relationSnapshots)
        {
            Faction? faction = Find.FactionManager.AllFactionsListForReading
                .FirstOrDefault(candidate => candidate.loadID == factionLoadId);
            if (faction is not null)
            {
                SetPlayerGoodwill(faction, goodwill);
                faction.lastMilitaryAidRequestTick = aidRequestTick;
            }
        }

        foreach ((string pawnId, int melee, int social, int cleaning) in pawnSnapshots)
        {
            Pawn? pawn = map.mapPawns.AllPawnsSpawned.FirstOrDefault(candidate => candidate.ThingID == pawnId);
            if (pawn is not null && !pawn.Destroyed)
            {
                pawn.skills.GetSkill(SkillDefOf.Melee).Level = melee;
                pawn.skills.GetSkill(SkillDefOf.Social).Level = social;
                pawn.workSettings?.SetPriority(WorkTypeDefOf.Cleaning, cleaning);
            }
        }

        if (originalFactionPawnIds.Count > 0)
        {
            var fixtureFactionIds = relationSnapshots.Select(snapshot => snapshot.FactionLoadId).ToHashSet();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.Where(pawn =>
                         pawn.Faction is not null && fixtureFactionIds.Contains(pawn.Faction.loadID) &&
                         !originalFactionPawnIds.Contains(pawn.thingIDNumber)).ToArray())
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        foreach (Filth ash in map.listerThings.ThingsOfDef(ThingDefOf.Filth_Ash).OfType<Filth>().ToArray())
        {
            if (baselineAsh.TryGetValue(ash.ThingID, out int originalThickness))
            {
                ash.thickness = originalThickness;
            }
            else
            {
                ash.Destroy(DestroyMode.Vanish);
            }
        }

        List<ScheduledSignalResponse> scheduled = CurrentScheduledResponses();
        scheduled.Clear();
        foreach ((int factionLoadId, int dueTick) in scheduledResponseBaseline)
        {
            Faction? faction = Find.FactionManager.AllFactionsListForReading
                .FirstOrDefault(candidate => candidate.loadID == factionLoadId);
            if (faction is not null)
            {
                scheduled.Add(new ScheduledSignalResponse(faction, dueTick));
            }
        }
    }

    private void RebindAfterLoad(
        string menuActorId,
        IReadOnlyList<string> generatedIds,
        int delayedFactionId,
        int immediateFactionId,
        IReadOnlyList<int> settlementIds)
    {
        map = Find.CurrentMap ?? throw new EndToEndAssertionException(
            "The reloaded delayed-response fixture needs a current map.");
        delayedFaction = Find.FactionManager.AllFactionsListForReading
            .Single(faction => faction.loadID == delayedFactionId);
        immediateFaction = Find.FactionManager.AllFactionsListForReading
            .Single(faction => faction.loadID == immediateFactionId);
        generatedColonists.Clear();
        generatedColonists.AddRange(generatedIds.Select(id =>
            map.mapPawns.AllPawnsSpawned.Single(pawn => pawn.ThingID == id)));
        menuActor = generatedColonists.Single(pawn => pawn.ThingID == menuActorId);
        settlements.Clear();
        settlements.AddRange(settlementIds.Select(id =>
            Find.WorldObjects.AllWorldObjects.OfType<Settlement>().Single(settlement => settlement.ID == id)));
    }

    private int NewFactionPawnCount(Faction faction) => map.mapPawns.AllPawnsSpawned.Count(pawn =>
        pawn.Faction == faction && !originalFactionPawnIds.Contains(pawn.thingIDNumber));

    private string[] NewFactionPawnIds(Faction faction) => map.mapPawns.AllPawnsSpawned
        .Where(pawn => pawn.Faction == faction && !originalFactionPawnIds.Contains(pawn.thingIDNumber))
        .Select(pawn => pawn.ThingID)
        .ToArray();

    private void SpawnPreparedFire()
    {
        fire = (Building_SignalFire)ThingMaker.MakeThing(fireDef);
        fire.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(fire, fireCell, map);
        createdFireIds.Add(fire.ThingID);
    }

    private void AssertCurrentFireWasConsumed(string outcome)
    {
        EndToEndAssert.True(fire.Destroyed || !fire.Spawned,
            $"The {outcome} active performance must consume and destroy its prepared single-use fire.");
        EndToEndAssert.False(map.listerThings.AllThings.Any(candidate => candidate.ThingID == fire.ThingID),
            $"The destroyed {outcome} fire must no longer be registered on the map.");
    }

    private int AshThickness() => map.listerThings.ThingsOfDef(ThingDefOf.Filth_Ash)
        .OfType<Filth>()
        .Sum(filth => filth.thickness);

    private static FloatMenuOption[] OpenMenuOptions()
    {
        FloatMenu[] menus = Find.WindowStack.Windows.OfType<FloatMenu>().Where(menu => menu.IsOpen).ToArray();
        EndToEndAssert.Equal(1, menus.Length, "Exactly one native FloatMenu must be open.");
        var options = FloatMenuOptions?.GetValue(menus[0]) as IEnumerable<FloatMenuOption>;
        EndToEndAssert.NotNull(options, "The RimWorld 1.6 FloatMenu option shape must remain available.");
        return options!.ToArray();
    }

    private static Settlement CreateSettlement(Faction faction, PlanetTile tile, string name)
    {
        var settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
        settlement.Tile = tile;
        settlement.SetFaction(faction);
        settlement.Name = name;
        Find.WorldObjects.Add(settlement);
        return settlement;
    }

    private static Pawn GenerateDeconstructor()
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            Pawn candidate = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (candidate.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
            {
                candidate.Destroy(DestroyMode.Vanish);
                continue;
            }

            candidate.workSettings.EnableAndInitialize();
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!candidate.WorkTypeIsDisabled(workType))
                {
                    candidate.workSettings.SetPriority(workType, 0);
                }
            }

            candidate.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
            candidate.skills.GetSkill(SkillDefOf.Construction).Level = 20;
            if (candidate.needs?.food is { } food)
            {
                food.CurLevelPercentage = 1f;
            }

            if (candidate.needs?.rest is { } rest)
            {
                rest.CurLevelPercentage = 1f;
            }

            return candidate;
        }

        throw new EndToEndAssertionException("Could not generate a capable active-fire deconstructor.");
    }

    private static void SetPlayerRelation(Faction faction, FactionRelationKind kind)
    {
        SetPlayerGoodwill(faction, kind switch
        {
            FactionRelationKind.Ally => 100,
            FactionRelationKind.Neutral => 0,
            _ => -100,
        });
    }

    private static void SetPlayerGoodwill(Faction faction, int desiredGoodwill)
    {
        int change = desiredGoodwill - faction.PlayerGoodwill;
        if (change != 0)
        {
            faction.TryAffectGoodwillWith(
                Faction.OfPlayer,
                change,
                canSendMessage: false,
                canSendHostilityLetter: false);
        }
    }

    private static List<PlanetTile> EmptyNearbyTiles(PlanetTile origin, int count)
    {
        var found = new List<PlanetTile>();
        var visited = new HashSet<PlanetTile> { origin };
        var frontier = new Queue<(PlanetTile Tile, int Distance)>();
        frontier.Enqueue((origin, 0));
        while (frontier.Count > 0 && found.Count < count)
        {
            (PlanetTile tile, int distance) = frontier.Dequeue();
            if (distance > 0 && distance <= 10 && !Find.WorldObjects.AnyWorldObjectAt(tile))
            {
                found.Add(tile);
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

        EndToEndAssert.Equal(count, found.Count,
            "The isolated world must provide enough empty nearby settlement tiles.");
        return found;
    }
}
