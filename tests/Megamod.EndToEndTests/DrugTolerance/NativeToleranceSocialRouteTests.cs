using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using Megamod.Features.DrugTolerance.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;
using HarmonyOwner = HarmonyLib.Harmony;

namespace Megamod.EndToEndTests.DrugTolerance;

[RimWorldEndToEndTest("megamod.drug-tolerance.native-social-drinks", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 20000, MaxWallClockSeconds = 1440)]
public sealed class NativeToleranceSocialRouteTests : IRimWorldEndToEndTest
{
    private readonly NativeToleranceSocialFixture fixture = new();
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

// Private S138 fixture: ordinary table recreation, not party-event lifecycle.
// Only disposable preconditions are constructed. Native gather-spot/undraft/time actions
// let the ordinary engine select, reserve, carry, socially relax and finally ingest.
internal sealed class NativeToleranceSocialFixture
{
    private static readonly FieldInfo RuntimeField = typeof(MegamodMod).GetField("drugTolerance", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo Selector = typeof(JoyGiver_SocialRelax).GetMethod("TryFindIngestibleToNurse", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static NativeToleranceSocialFixture? active;
    private readonly string marker = "ToleranceSocial-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private readonly List<Subject> subjects = new();
    private readonly List<DrugPolicy> policies = new();
    private readonly List<Thing> ownedThings = new(), beers = new(), chairs = new();
    private Game game = null!;
    private Map map = null!;
    private MegamodMod mod = null!;
    private NativeDrugToleranceRuntime runtime = null!;
    private ThingDef beerDef = null!;
    private HediffDef highDef = null!, toleranceDef = null!;
    private JoyKindDef socialKind = null!;
    private JobDef socialJob = null!;
    private Thing table = null!;
    private CompGatherSpot gatherSpot = null!;
    private HarmonyOwner traceOwner = null!;
    private bool traceInstalled;
    private EditWindow_Log? ownedLog;
    private Thing? consumedBeer;
    private int nativeOperations, screenshots, waitSeconds;
    private string phase = "arranged";

    internal void Arrange(IEndToEndContext context)
    {
        game = Current.Game; map = Find.CurrentMap;
        RequireGame();
        EndToEndAssert.True(game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(Prefs.DevMode && Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<EditWindow_Log>());
        EndToEndAssert.True(active == null && map.gatherSpotLister.activeSpots.Count == 0,
            "This isolated fixture requires no pre-existing active gather spots.");
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() => { RequireGame(); Find.TickManager.CurTimeSpeed = speed; });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        mod = LoadedModManager.GetMod<MegamodMod>();
        runtime = RuntimeField.GetValue(mod) as NativeDrugToleranceRuntime
            ?? throw new InvalidOperationException("The real megamod tolerance runtime has not been registered.");
        RequireAvailable();
        beerDef = DefDatabase<ThingDef>.GetNamed("Beer");
        highDef = DefDatabase<HediffDef>.GetNamed("AlcoholHigh");
        toleranceDef = DefDatabase<HediffDef>.GetNamed("AlcoholTolerance");
        socialKind = DefDatabase<JoyKindDef>.GetNamed("Social");
        socialJob = DefDatabase<JobDef>.GetNamed("SocialRelax");
        EndToEndAssert.True(beerDef.ingestible.nurseable && beerDef.ingestible.defaultNumToIngestAtOnce == 1 &&
            beerDef.ingestible.joyKind != socialKind && socialJob.joyDuration == 4000 && socialJob.joyKind == socialKind);
        EndToEndAssert.True(beerDef.ingestible.outcomeDoers.OfType<IngestionOutcomeDoer_GiveHediff>()
            .Any(value => ReferenceEquals(value.hediffDef, toleranceDef)));
        context.DeferCleanup(() =>
        {
            RequireGame();
            if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(false);
            Find.Selector.ClearSelection();
            foreach (var thing in ownedThings.AsEnumerable().Reverse())
                if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
            foreach (var policy in policies)
                if (policy.label.StartsWith(marker, StringComparison.Ordinal)) game.drugPolicyDatabase.AllPolicies.Remove(policy);
        });
        // Refuse occupied furniture/supply cells instead of clearing unrelated objects.
        var center = CellRect.CenteredOn(map.Center, 25).OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(CanArrangeAt);
        table = SpawnFurniture("Table2x2c", center, Rot4.North);
        gatherSpot = table.TryGetComp<CompGatherSpot>();
        EndToEndAssert.True(gatherSpot != null && gatherSpot.Active);
        gatherSpot.Active = false; // Initial condition; the native gizmo performs the tested activation.
        foreach (var cell in table.OccupiedRect().AdjacentCellsCardinal)
            chairs.Add(SpawnFurniture("DiningChair", cell, Rot4.FromIntVec3(table.OccupiedRect().ClosestCellTo(cell) - cell)));
        EndToEndAssert.Equal(8, chairs.Count);
        foreach (int offset in new[] { -2, 0, 2 })
        {
            var beer = ThingMaker.MakeThing(beerDef); beer.stackCount = 1; ownedThings.Add(beer); beers.Add(beer);
            GenSpawn.Spawn(beer, center + new IntVec3(offset, 0, -5), map);
            beer.SetForbidden(false, false);
        }
        subjects.Add(CreateSubject("Social Zero", center + new IntVec3(-4, 0, 1), any: false, drugDesire: false));
        subjects.Add(CreateSubject("Social Trait Zero", center + new IntVec3(4, 0, 1), any: false, drugDesire: true));
        subjects.Add(CreateSubject("Social Any", center + new IntVec3(0, 0, -3), any: true, drugDesire: false));
        traceOwner = new HarmonyOwner("fumblesneeze.tests.s138.social." + marker);
        active = this;
        context.DeferCleanup(() =>
        {
            if (ReferenceEquals(active, this)) active = null;
            traceOwner.Unpatch(Selector, HarmonyPatchType.All, traceOwner.Id);
            EndToEndAssert.False(HarmonyOwner.GetPatchInfo(Selector)?.Owners.Contains(traceOwner.Id) == true);
        });
        traceOwner.Patch(Selector, postfix: new HarmonyMethod(typeof(NativeToleranceSocialFixture)
            .GetMethod(nameof(ObserveSelection), BindingFlags.Static | BindingFlags.NonPublic)!) { priority = Priority.Last });
        traceInstalled = true;
        RequireAvailable(); // A tracing-induced permission fallback cannot prove tolerance behavior.
        AssertInitial();
    }

    private bool CanArrangeAt(IntVec3 center)
    {
        if (!CellRect.CenteredOn(center, 5).All(cell => cell.InBounds(map) && cell.Standable(map) && !cell.Fogged(map))) return false;
        var footprint = GenAdj.OccupiedRect(center, Rot4.North, DefDatabase<ThingDef>.GetNamed("Table2x2c").size);
        var positions = footprint.Concat(footprint.AdjacentCellsCardinal).Concat(new[]
        {
            center + new IntVec3(-2, 0, -5), center + new IntVec3(0, 0, -5), center + new IntVec3(2, 0, -5),
            center + new IntVec3(-4, 0, 1), center + new IntVec3(4, 0, 1), center + new IntVec3(0, 0, -3)
        });
        return positions.All(cell => cell.GetThingList(map).Count == 0);
    }

    private Thing SpawnFurniture(string defName, IntVec3 cell, Rot4 rotation)
    {
        var thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName), ThingDefOf.WoodLog);
        ownedThings.Add(thing); thing.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(thing, cell, map, rotation);
        thing.SetForbidden(false, false);
        return thing;
    }

    private Subject CreateSubject(string label, IntVec3 cell, bool any, bool drugDesire)
    {
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowFood: false, allowAddictions: false,
            fixedBiologicalAge: 25, fixedChronologicalAge: 25, forceNoIdeo: true, forceNoGear: true));
        ownedThings.Add(pawn);
        pawn.Name = new NameTriple("Tolerance", label, marker);
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray()) pawn.health.RemoveHediff(hediff);
        foreach (var trait in pawn.story.traits.allTraits.ToArray()) pawn.story.traits.RemoveTrait(trait);
        if (drugDesire) pawn.story.traits.GainTrait(new Trait(TraitDefOf.DrugDesire, 1));
        pawn.inventory.innerContainer.ClearAndDestroyContents();
        pawn.workSettings.EnableAndInitialize();
        foreach (var work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (!pawn.WorkTypeIsDisabled(work)) pawn.workSettings.SetPriority(work, 0);
        for (int hour = 0; hour < 24; hour++) pawn.timetable.SetAssignment(hour, TimeAssignmentDefOf.Joy);
        pawn.needs.food.CurLevelPercentage = 1f;
        pawn.needs.rest.CurLevelPercentage = 1f;
        pawn.needs.mood.CurLevelPercentage = 1f;
        pawn.needs.joy.CurLevelPercentage = .05f;
        foreach (var kind in DefDatabase<JoyKindDef>.AllDefsListForReading)
            if (kind != socialKind) pawn.needs.joy.tolerances.Notify_JoyGained(2f, kind);
        var policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); policies.Add(policy);
        policy.label = marker + " " + label;
        for (int i = 0; i < policy.Count; i++)
        {
            policy[i].allowedForAddiction = false; policy[i].allowedForJoy = false;
            policy[i].allowScheduled = false; policy[i].takeToInventory = 0;
        }
        var entry = policy[beerDef]; entry.allowedForJoy = true;
        NativeDrugToleranceState.WriteEncoded(entry, any ? "1:any" : "1:limit:0");
        pawn.drugs.CurrentPolicy = policy;
        GenSpawn.Spawn(pawn, cell, map); pawn.drafter.Drafted = true;
        return new Subject(pawn, policy, entry, any, drugDesire);
    }

    internal IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Workflow(context))
        {
            if (step is ScreenshotStep) screenshots++;
            else if (step is WaitUntilStep wait) waitSeconds += checked((int)wait.Deadline.MaxWallClock.TotalSeconds);
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep) nativeOperations++;
            if (step.Kind == EndToEndStepKind.Act)
                yield return new AssertionStep("confirm current tolerance group before " + step.Name, _ => RequireAvailable());
            yield return step;
        }
        yield return new AssertionStep("account for the complete compiled social workflow", _ =>
        {
            EndToEndAssert.Equal(37, nativeOperations);
            EndToEndAssert.Equal(11, screenshots);
            EndToEndAssert.Equal(360, waitSeconds);
            EndToEndAssert.True(nativeOperations * 15 + screenshots * 30 + waitSeconds + 180 <= 1440);
        });
        yield return new CheckpointStep("complete social operation and timeout accounting", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> Workflow(IEndToEndContext context)
    {
        yield return Run("advance ordinary time beyond Core's first-5000-tick joy gate while all subjects remain drafted", EndToEndGameSpeed.Superfast);
        yield return Wait("reach native recreation start time without forcing the clock or a job", _ =>
        {
            AssertInitial(); return Find.TickManager.TicksGame >= 5000;
        }, 6000, 90);
        yield return Pause("pause before native gather-spot activation");
        yield return Frame("frame the table, eight chairs, three ground Beers and three named subjects");
        yield return Select(table, "select the initially disabled native gather spot");
        yield return Shot("disabled native gather spot and three physical single-dose Beers before action");
        foreach (var subject in Blocked)
        {
            yield return Select(subject.Pawn, "select initial recreation need for " + subject.Name);
            yield return new PawnInspectTabActionStep("show low initial recreation for " + subject.Name, subject.Pawn.ThingID, EndToEndPawnInspectTab.Needs);
            yield return Shot(subject.Name + " before social recreation");
        }
        yield return Select(table, "select the table's native gather-spot command");
        var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { table.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == false && option.HotKeyDefName == "Command_TogglePower");
        yield return new GizmoActionStep("enable this table as a gather spot using its native gizmo", new[] { table.ThingID },
            toggle.RuntimeType, EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
        yield return new AssertionStep("observe native gather-spot activation before any undraft", _ =>
        {
            EndToEndAssert.True(gatherSpot.Active && map.gatherSpotLister.activeSpots.Count == 1 &&
                ReferenceEquals(map.gatherSpotLister.activeSpots[0], gatherSpot));
            foreach (var subject in subjects) { subject.TraceArmed = true; subject.StartJoy = subject.Pawn.needs.joy.CurLevelPercentage; }
            phase = "drinkless selection";
        });
        yield return Shot("native table gather spot enabled for ordinary social recreation");
        yield return new CheckpointStep("native activation, initial needs and real atomic patch ownership", _ => Evidence());
        foreach (var subject in Blocked)
            foreach (var step in Undraft(context, subject)) yield return step;
        yield return Run("let the two numeric-zero subjects choose ordinary social jobs", EndToEndGameSpeed.Normal);
        yield return Wait("observe actual drinkless selections, reserved chairs and increasing recreation", _ =>
        {
            ObserveBlocked(); AssertGroundSupply(3);
            if (!Blocked.All(subject => AtSocialSeat(subject) && subject.FalseSelections > 0 &&
                subject.Pawn.needs.joy.CurLevelPercentage > subject.StartJoy + .02f)) return false;
            foreach (var subject in Blocked)
            {
                subject.DrinklessTick = Find.TickManager.TicksGame;
                subject.DrinklessJoy = subject.Pawn.needs.joy.CurLevelPercentage;
            }
            return true;
        }, 2400, 90);
        yield return Pause("pause actual drinkless social recreation with all Beer still present");
        yield return Frame("frame both reserved chairs, social pawns and untouched Beer");
        foreach (var subject in Blocked)
        {
            yield return Select(subject.Pawn, "select the actual drinkless social participant " + subject.Name);
            yield return new PawnInspectTabActionStep("show recreation gained without a drink for " + subject.Name,
                subject.Pawn.ThingID, EndToEndPawnInspectTab.Needs);
            yield return Shot(subject.Name + " relaxes socially in a reserved chair while all three Beers remain");
        }
        yield return new CheckpointStep("native drinkless fallback including positive DrugDesire at zero", _ => Evidence());
        var any = subjects[2];
        foreach (var step in Undraft(context, any)) yield return step;
        yield return Run("let the Any subject choose and carry its own social drink", EndToEndGameSpeed.Normal);
        yield return Wait("observe the Any subject seated with the exact natively selected ground Beer", _ =>
        {
            ObserveBlocked();
            if (!AtSocialSeat(any)) return false;
            var drink = any.Pawn.CurJob.targetC.Thing;
            EndToEndAssert.True(any.TrueSelections > 0 && drink != null && beers.Contains(drink) &&
                ReferenceEquals(drink, any.LastSelectedDrink) && ReferenceEquals(drink, any.Pawn.carryTracker.CarriedThing));
            EndToEndAssert.True(!drink!.Destroyed && drink.stackCount == 1 && !any.Pawn.drugs.HasEverTaken(beerDef));
            consumedBeer = drink; any.DrinkingTick = Find.TickManager.TicksGame; phase = "social drinking";
            AssertGroundSupply(2);
            return true;
        }, 1800, 60);
        yield return Pause("pause the real social drinking toil before final ingestion");
        yield return Frame("frame Any carrying Beer at the table beside drinkless participants");
        yield return new PawnInspectTabActionStep("show the native social activity of the Any participant", any.Pawn.ThingID, EndToEndPawnInspectTab.Needs);
        yield return Shot("Social Any carries its exact Beer during native seated social recreation");
        yield return new CheckpointStep("native social job, chair reservation and physical drink provenance", _ => Evidence());
        yield return Run("finish the native social duration and its ordinary final ingestion", EndToEndGameSpeed.Superfast);
        yield return Wait("observe real social ingestion, consumed source and ordinary alcohol effects", _ =>
        {
            ObserveBlocked();
            if (consumedBeer == null || !consumedBeer.Destroyed || !any.Pawn.drugs.HasEverTaken(beerDef)) return false;
            EndToEndAssert.True(Severity(any, highDef) > 0 && Severity(any, toleranceDef) > 0);
            EndToEndAssert.True(beers.Where(beer => !ReferenceEquals(beer, consumedBeer)).All(beer => beer.Spawned && beer.stackCount == 1));
            AssertGroundSupply(2); any.CompletedTick = Find.TickManager.TicksGame; phase = "social result";
            return true;
        }, 6000, 120);
        yield return Pause("pause after the actual social Beer dose");
        yield return new PawnInspectTabActionStep("show native alcohol effects after social ingestion", any.Pawn.ThingID, EndToEndPawnInspectTab.Health);
        yield return Shot("Social Any has native alcohol and tolerance effects after one physical Beer was consumed");
        foreach (var subject in Blocked)
        {
            yield return Select(subject.Pawn, "select the still drinkless participant after the paired dose " + subject.Name);
            yield return new PawnInspectTabActionStep("show recreation retained after the Any dose for " + subject.Name,
                subject.Pawn.ThingID, EndToEndPawnInspectTab.Needs);
            yield return Shot(subject.Name + " gained social recreation while the paired Any subject drank");
        }
        yield return new AssertionStep("retain scoped paired social results", _ =>
        {
            ObserveBlocked(); AssertGroundSupply(2);
            EndToEndAssert.True(Blocked.All(subject => subject.FalseSelections > 0 && subject.DrinklessTick >= 0 &&
                subject.DrinklessJoy > subject.StartJoy + .02f && subject.Pawn.needs.joy.CurLevelPercentage > subject.StartJoy));
        });
        yield return new NativeDeveloperLogActionStep("open the native console after social actions", true);
        yield return new NativeWindowCaptureStep("retain this workflow's exact native console", windows =>
        {
            RequireAvailable();
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Missing native console.");
            EndToEndAssert.True(windows.Any(window => window.NativeId == ownedLog.ID && window.RuntimeType == ownedLog.GetType().FullName));
        });
        yield return Shot("native console after actual social drinking and drinkless fallback");
        yield return new NativeDeveloperLogActionStep("close the native console after social evidence", false);
        yield return new CheckpointStep("final scoped social evidence; full S138 remains open", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> Undraft(IEndToEndContext context, Subject subject)
    {
        yield return Select(subject.Pawn, "select drafted participant " + subject.Name);
        EndToEndAssert.True(subject.Pawn.Drafted);
        var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { subject.Pawn.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == true && option.HotKeyDefName == "Command_ColonistDraft");
        yield return new GizmoActionStep("undraft " + subject.Name + " through its native gizmo", new[] { subject.Pawn.ThingID },
            toggle.RuntimeType, EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
    }

    private IEnumerable<Subject> Blocked => subjects.Where(subject => !subject.Any);
    private bool AtSocialSeat(Subject subject)
    {
        var pawn = subject.Pawn; var job = pawn.CurJob;
        if (job?.def != socialJob || pawn.jobs.curDriver is not JobDriver_SocialRelax driver ||
            driver.DesiredSocialMode() != RandomSocialMode.SuperActive || pawn.Position != job.targetB.Cell) return false;
        EndToEndAssert.True(!job.playerForced && job.jobGiver?.GetType() == typeof(JobGiver_GetJoy) &&
            ReferenceEquals(job.targetA.Thing, table) && subject.TrueSelections + subject.FalseSelections > 0);
        var chair = pawn.Position.GetEdifice(map);
        EndToEndAssert.True(chair != null && chairs.Contains(chair) && map.reservationManager.ReservedBy(chair, pawn, job),
            "The ordinary social driver must reserve and use an actual chair at this table.");
        subject.ObservedJob = job.ToString(); subject.ChairId = chair!.ThingID;
        subject.Toil = driver.CurToilString; subject.ToilTicksLeft = driver.ticksLeftThisToil;
        return true;
    }
    private void ObserveBlocked()
    {
        RequireAvailable();
        foreach (var subject in Blocked)
        {
            EndToEndAssert.False(subject.Pawn.Dead || subject.Pawn.Downed || subject.Pawn.InMentalState);
            EndToEndAssert.False(subject.Pawn.drugs.HasEverTaken(beerDef) || subject.Pawn.CurJobDef == JobDefOf.Ingest);
            EndToEndAssert.True(subject.TrueSelections == 0 && subject.LastSelectedDrink == null && HeldBeer(subject) == 0);
            if (subject.Pawn.CurJobDef == socialJob) EndToEndAssert.False(subject.Pawn.CurJob.targetC.IsValid);
            EndToEndAssert.Equal(0f, Severity(subject, highDef));
            EndToEndAssert.Equal(0f, Severity(subject, toleranceDef));
        }
    }
    private void AssertInitial()
    {
        RequireAvailable(); AssertGroundSupply(3);
        EndToEndAssert.False(gatherSpot.Active);
        foreach (var subject in subjects)
        {
            EndToEndAssert.True(subject.Pawn.Drafted && !subject.Pawn.Dead && !subject.Pawn.Downed && !subject.Pawn.InMentalState);
            EndToEndAssert.False(subject.Pawn.drugs.HasEverTaken(beerDef));
            EndToEndAssert.True(HeldBeer(subject) == 0 && subject.TrueSelections + subject.FalseSelections == 0);
            EndToEndAssert.True(subject.Pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                subject.Pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
            EndToEndAssert.Equal(0f, Severity(subject, highDef)); EndToEndAssert.Equal(0f, Severity(subject, toleranceDef));
        }
    }
    private void AssertGroundSupply(int count) => EndToEndAssert.Equal(count, beers.Count(beer => beer.Spawned && beer.stackCount == 1));

    private static void ObserveSelection(Pawn __1, Thing __2, bool __result, bool __runOriginal)
    {
        // Value-only completion observation: no target calls, ref arguments, game mutation or output replacement.
        var fixture = active;
        if (fixture == null || !ReferenceEquals(Current.Game, fixture.game)) return;
        var subject = fixture.subjects.FirstOrDefault(value => value.TraceArmed && ReferenceEquals(value.Pawn, __1));
        if (subject == null) return;
        if (!__runOriginal) subject.SkippedOriginal++;
        if (__result) subject.TrueSelections++; else subject.FalseSelections++;
        subject.LastSelectedDrink = __2; subject.LastSelectionTick = Find.TickManager.TicksGame;
    }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && ReferenceEquals(Find.CurrentMap, map) &&
        Current.ProgramState == ProgramState.Playing, "Refuse an unrelated Game/map during native social work or cleanup.");
    private void RequireAvailable()
    {
        RequireGame();
        EndToEndAssert.True(ReferenceEquals(RuntimeField.GetValue(mod), runtime) && runtime.Available,
            "The real atomic enforcement/editor group must remain available: " + runtime.StatusKey);
        if (traceInstalled)
        {
            var info = HarmonyOwner.GetPatchInfo(Selector);
            EndToEndAssert.True(info != null && info.Postfixes.Count(patch => patch.owner == traceOwner.Id) == 1 &&
                info.Transpilers.Any(patch => patch.owner == "fumblesneeze.megamod") &&
                info.Finalizers.Any(patch => patch.owner == "fumblesneeze.megamod"), "Required live social trace/product ownership changed.");
        }
        foreach (var subject in subjects)
        {
            EndToEndAssert.True(ReferenceEquals(subject.Pawn.drugs.CurrentPolicy, subject.Policy) &&
                game.drugPolicyDatabase.AllPolicies.Contains(subject.Policy) && ReferenceEquals(subject.Policy[beerDef], subject.Entry) &&
                NativeDrugToleranceState.ReadEncoded(subject.Entry) == (subject.Any ? "1:any" : "1:limit:0") &&
                subject.Entry.allowedForJoy && !subject.Entry.allowScheduled && !subject.Entry.allowedForAddiction && subject.Entry.takeToInventory == 0);
            EndToEndAssert.Equal(subject.DrugDesire ? 1 : 0, subject.Pawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire));
            EndToEndAssert.True(subject.Pawn.needs.joy.tolerances.BoredOf(beerDef.ingestible.joyKind) && subject.SkippedOriginal == 0,
                "Chemical recreation stays initially bored; no skipped-original social result is evidence.");
        }
    }
    private IReadOnlyDictionary<string, string> Evidence()
    {
        RequireAvailable();
        var info = HarmonyOwner.GetPatchInfo(Selector)!;
        var result = new Dictionary<string, string>
        {
            ["scope"] = "Ordinary Core table SocialRelax: zero/Any/positive DrugDesire zero; no party-event or full-S138 claim",
            ["phase"] = phase, ["tick"] = Find.TickManager.TicksGame.ToString(), ["available"] = runtime.Available.ToString(),
            ["traceOwner"] = traceOwner.Id, ["table"] = table.ThingID + ":active=" + gatherSpot.Active,
            ["chairs"] = string.Join(",", chairs.Select(chair => chair.ThingID)),
            ["physicalBeers"] = string.Join("; ", beers.Select(beer => beer.ThingID + ":spawned=" + beer.Spawned + ":destroyed=" + beer.Destroyed + ":count=" + beer.stackCount)),
            ["consumedBeer"] = consumedBeer?.ThingID ?? "none", ["nativeOperations"] = nativeOperations.ToString(),
            ["screenshots"] = screenshots.ToString(), ["maximumWaitSeconds"] = waitSeconds.ToString(),
            ["countedFixedAllowanceSeconds"] = (nativeOperations * 15 + screenshots * 30 + waitSeconds).ToString(),
            ["selectorPatchOwners"] = string.Join("; ", info.Prefixes.Select(patch => "prefix:" + patch.owner + ":" + patch.PatchMethod.Name)
                .Concat(info.Postfixes.Select(patch => "postfix:" + patch.owner + ":" + patch.PatchMethod.Name))
                .Concat(info.Transpilers.Select(patch => "transpiler:" + patch.owner + ":" + patch.PatchMethod.Name))
                .Concat(info.Finalizers.Select(patch => "finalizer:" + patch.owner + ":" + patch.PatchMethod.Name)))
        };
        foreach (var subject in subjects)
            result[subject.Name] = string.Join("; ", new[]
            {
                "pawn=" + subject.Pawn.ThingID, "policy=" + subject.Policy.id + ":" + subject.Policy.label,
                "choice=" + NativeDrugToleranceState.ReadEncoded(subject.Entry), "trait=" + subject.DrugDesire,
                "falseSelections=" + subject.FalseSelections, "trueSelections=" + subject.TrueSelections,
                "skippedOriginal=" + subject.SkippedOriginal, "lastSelectionTick=" + subject.LastSelectionTick,
                "lastSelectedDrink=" + (subject.LastSelectedDrink?.ThingID ?? "none"), "heldBeer=" + HeldBeer(subject),
                "everTaken=" + subject.Pawn.drugs.HasEverTaken(beerDef), "alcohol=" + Number(Severity(subject, highDef)),
                "tolerance=" + Number(Severity(subject, toleranceDef)), "startJoy=" + Number(subject.StartJoy),
                "drinklessJoy=" + Number(subject.DrinklessJoy), "currentJoy=" + Number(subject.Pawn.needs.joy.CurLevelPercentage),
                "drinklessTick=" + subject.DrinklessTick, "drinkingTick=" + subject.DrinkingTick, "completedTick=" + subject.CompletedTick,
                "observedJob=" + subject.ObservedJob, "chair=" + subject.ChairId, "toil=" + subject.Toil,
                "toilTicksLeft=" + subject.ToilTicksLeft, "currentJob=" + subject.Pawn.CurJob
            });
        return result;
    }
    private float Severity(Subject subject, HediffDef def) => subject.Pawn.health.hediffSet.GetFirstHediffOfDef(def)?.Severity ?? 0f;
    private int HeldBeer(Subject subject) => subject.Pawn.inventory.innerContainer.TotalStackCountOfDef(beerDef) +
        (subject.Pawn.carryTracker.CarriedThing?.def == beerDef ? subject.Pawn.carryTracker.CarriedThing.stackCount : 0);
    private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    private CameraActionStep Frame(string name) => new(name, subjects.Select(subject => subject.Pawn.ThingID)
        .Concat(new[] { table.ThingID }).Concat(beers.Where(beer => beer.Spawned).Select(beer => beer.ThingID)), paddingPixels: 180);
    private static SelectionActionStep Select(Thing thing, string name) => new(name, new[] { thing.ThingID }, additive: false);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static TimeControlActionStep Pause(string name) => new(name, true, EndToEndGameSpeed.Normal);
    private static TimeControlActionStep Run(string name, EndToEndGameSpeed speed) => new(name, false, speed);
    private static WaitUntilStep Wait(string name, Func<IEndToEndContext, bool> predicate, int ticks, int seconds) =>
        new(name, predicate, new EndToEndDeadline(int.MaxValue, ticks, TimeSpan.FromSeconds(seconds)));

    private sealed class Subject
    {
        internal readonly Pawn Pawn;
        internal readonly DrugPolicy Policy;
        internal readonly DrugPolicyEntry Entry;
        internal readonly bool Any, DrugDesire;
        internal bool TraceArmed;
        internal int TrueSelections, FalseSelections, SkippedOriginal, LastSelectionTick = -1;
        internal int DrinklessTick = -1, DrinkingTick = -1, CompletedTick = -1, ToilTicksLeft;
        internal float StartJoy, DrinklessJoy;
        internal Thing? LastSelectedDrink;
        internal string ObservedJob = "none", ChairId = "none", Toil = "none";
        internal string Name => Pawn.Name.ToStringShort;
        internal Subject(Pawn pawn, DrugPolicy policy, DrugPolicyEntry entry, bool any, bool drugDesire)
        { Pawn = pawn; Policy = policy; Entry = entry; Any = any; DrugDesire = drugDesire; }
    }
}
