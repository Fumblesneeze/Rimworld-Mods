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

[RimWorldEndToEndTest("megamod.drug-tolerance.native-positive-boundary", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 9000, MaxWallClockSeconds = 1440)]
public sealed class NativeTolerancePositiveBoundaryTests : IRimWorldEndToEndTest
{
    private readonly NativeTolerancePositiveBoundaryFixture fixture = new();
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

// Private S138 fixture: one seeded 20% starting condition, then only native decisions/decay/ingestion.
// The native drafted-hold prelude makes paused undraft exercise ordinary selection synchronously.
// The initial seed is supporting setup, never claimed as tolerance accumulated by ingestion.
internal sealed class NativeTolerancePositiveBoundaryFixture
{
    private const float Limit = .2f;
    private const string Choice = "1:limit:0.2";
    private static readonly FieldInfo RuntimeField = typeof(MegamodMod).GetField("drugTolerance", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo ShouldTarget = typeof(Pawn_DrugPolicyTracker).GetMethod(nameof(Pawn_DrugPolicyTracker.ShouldTryToTakeScheduledNow))!;
    private static readonly MethodInfo AllowedTarget = typeof(Pawn_DrugPolicyTracker).GetMethod(nameof(Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow))!;
    private static readonly MethodInfo HealthTarget = typeof(HediffWithComps).GetMethod(nameof(HediffWithComps.PostTickInterval), new[] { typeof(int) })!;
    private static readonly MethodInfo IngestedTarget = typeof(Thing).GetMethod(nameof(Thing.Ingested), new[] { typeof(Pawn), typeof(float) })!;
    private static NativeTolerancePositiveBoundaryFixture? active;
    private readonly string marker = "ToleranceBoundary-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private readonly List<Thing> owned = new();
    private readonly List<MethodInfo> traced = new();
    private readonly List<Decision> decisions = new();
    private readonly Stack<Decision> pending = new();
    private readonly List<Decay> decays = new();
    private Game game = null!;
    private Map map = null!;
    private MegamodMod mod = null!;
    private NativeDrugToleranceRuntime runtime = null!;
    private HarmonyOwner traceOwner = null!;
    private Pawn pawn = null!;
    private DrugPolicy? policy;
    private DrugPolicyEntry entry = null!;
    private ThingDef beerDef = null!;
    private HediffDef toleranceDef = null!, highDef = null!;
    private Thing beer = null!;
    private HediffWithComps? tolerance;
    private HediffComp_SeverityPerDay? decayComp;
    private Job? observedJob;
    private EditWindow_Log? ownedLog;
    private bool armed, healthPending, traceFault;
    private float healthBefore;
    private int seedWrites, seedTick = -1, equalityTick = -1, observedJobLoadId = -1, observedJobTick = -1;
    private int ingestionCalls, ingestionJobLoadId = -1, ingestionTick = -1, completedTick = -1;
    private bool ingestionSameJob, ingestionNativeSource;
    private int nativeOperations, screenshots, waitSeconds;
    private string phase = "unseeded preconditions";

    internal void Arrange(IEndToEndContext context)
    {
        game = Current.Game; map = Find.CurrentMap; RequireGame();
        EndToEndAssert.True(active == null && game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(Prefs.DevMode && Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<EditWindow_Log>());
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() => { RequireGame(); Find.TickManager.CurTimeSpeed = speed; });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        mod = LoadedModManager.GetMod<MegamodMod>();
        runtime = RuntimeField.GetValue(mod) as NativeDrugToleranceRuntime
            ?? throw new InvalidOperationException("The real megamod tolerance runtime has not been registered.");
        RequireAvailable();
        beerDef = DefDatabase<ThingDef>.GetNamed("Beer");
        toleranceDef = DefDatabase<HediffDef>.GetNamed("AlcoholTolerance");
        highDef = DefDatabase<HediffDef>.GetNamed("AlcoholHigh");
        EndToEndAssert.True(toleranceDef.maxSeverity == 1f && beerDef.ingestible.defaultNumToIngestAtOnce == 1 &&
            beerDef.ingestible.outcomeDoers.OfType<IngestionOutcomeDoer_GiveHediff>().Any(value => value.hediffDef == toleranceDef));
        context.DeferCleanup(() =>
        {
            RequireGame();
            if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(false);
            Find.Selector.ClearSelection();
            foreach (var thing in owned.AsEnumerable().Reverse()) if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
            if (policy != null && policy.label.StartsWith(marker, StringComparison.Ordinal)) game.drugPolicyDatabase.AllPolicies.Remove(policy);
        });
        pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowFood: false, allowAddictions: false,
            fixedBiologicalAge: 25, fixedChronologicalAge: 25, forceNoIdeo: true, forceNoGear: true));
        owned.Add(pawn); pawn.Name = new NameTriple("Tolerance", "Boundary Twenty", marker);
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray()) pawn.health.RemoveHediff(hediff);
        foreach (var trait in pawn.story.traits.allTraits.ToArray()) pawn.story.traits.RemoveTrait(trait);
        pawn.inventory.innerContainer.ClearAndDestroyContents();
        pawn.workSettings.EnableAndInitialize();
        foreach (var work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (!pawn.WorkTypeIsDisabled(work)) pawn.workSettings.SetPriority(work, 0);
        for (int hour = 0; hour < 24; hour++) pawn.timetable.SetAssignment(hour, TimeAssignmentDefOf.Anything);
        pawn.needs.food.CurLevelPercentage = 1f; pawn.needs.rest.CurLevelPercentage = 1f;
        pawn.needs.mood.CurLevelPercentage = 1f; pawn.needs.joy.CurLevelPercentage = 1f;
        policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); policy.label = marker + " numeric twenty";
        for (int index = 0; index < policy.Count; index++)
        {
            policy[index].allowScheduled = false; policy[index].allowedForJoy = false;
            policy[index].allowedForAddiction = false; policy[index].takeToInventory = 0;
        }
        entry = policy[beerDef]; entry.allowScheduled = true; entry.daysFrequency = 1f;
        entry.onlyIfMoodBelow = 1f; entry.onlyIfJoyBelow = 1f;
        NativeDrugToleranceState.WriteEncoded(entry, Choice); pawn.drugs.CurrentPolicy = policy;
        beer = ThingMaker.MakeThing(beerDef); beer.stackCount = 1; owned.Add(beer);
        EndToEndAssert.True(pawn.inventory.innerContainer.TryAdd(beer));
        var cell = CellRect.CenteredOn(map.Center, 15).OrderBy(value => value.DistanceToSquared(map.Center))
            .First(value => value.InBounds(map) && value.Standable(map) && !value.Fogged(map) && value.GetThingList(map).Count == 0);
        GenSpawn.Spawn(pawn, cell, map); pawn.drafter.Drafted = true;
        EndToEndAssert.True(pawn.BodySize == 1f && !ModsConfig.BiotechActive);
        traceOwner = new HarmonyOwner("fumblesneeze.tests.s138.boundary." + marker); active = this;
        context.DeferCleanup(() =>
        {
            if (ReferenceEquals(active, this)) active = null;
            foreach (var target in traced) traceOwner.Unpatch(target, HarmonyPatchType.All, traceOwner.Id);
            foreach (var target in traced) EndToEndAssert.False(HarmonyOwner.GetPatchInfo(target)?.Owners.Contains(traceOwner.Id) == true);
        });
        Install(ShouldTarget, nameof(BeginDecision), nameof(CompleteDecision));
        Install(AllowedTarget, nameof(ObserveAllowed));
        Install(HealthTarget, nameof(BeginHealthInterval), nameof(CompleteHealthInterval));
        Install(IngestedTarget, nameof(ObserveIngestion));
        AssertUnseeded();
    }

    internal IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Workflow(context))
        {
            if (step is ScreenshotStep) screenshots++;
            else if (step is WaitUntilStep wait) waitSeconds += checked((int)wait.Deadline.MaxWallClock.TotalSeconds);
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep) nativeOperations++;
            if (step.Kind == EndToEndStepKind.Act)
                yield return new AssertionStep("confirm current native tolerance group before " + step.Name, _ => RequireAvailable());
            yield return step;
        }
        yield return new AssertionStep("account for the complete positive-boundary sequence", _ =>
        {
            EndToEndAssert.Equal(20, nativeOperations); EndToEndAssert.Equal(8, screenshots); EndToEndAssert.Equal(210, waitSeconds);
            EndToEndAssert.True(nativeOperations * 15 + screenshots * 30 + waitSeconds + 180 <= 1440);
        });
        yield return new CheckpointStep("complete boundary operation and wait accounting", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> Workflow(IEndToEndContext context)
    {
        yield return Run("let the drafted pawn obtain its ordinary native hold job");
        yield return Wait("observe an actual drafted Wait_Combat before finishing initial conditions", _ =>
        {
            AssertUnseeded(); return pawn.CurJobDef == JobDefOf.Wait_Combat && pawn.jobs.IsCurrentJobPlayerInterruptible();
        }, 600, 30);
        yield return Pause("pause the native drafted hold before the one initial tolerance seed");
        yield return new CheckpointStep("unseeded drafted-hold prelude; no tolerance outcome claimed", _ => Evidence());

        // Supporting setup continues once the engine has created the initial hold job. This is
        // deliberately outside an assertion/observer callback, and precedes every claimed result.
        SeedStartingTolerance();
        yield return new CheckpointStep("one seeded twenty-percent starting condition; not naturally accumulated tolerance", _ => Evidence());
        yield return Frame("frame Boundary Twenty before its native selection action");
        yield return new SelectionActionStep("select the pawn with seeded twenty-percent starting tolerance", new[] { pawn.ThingID }, additive: false);
        yield return new PawnInspectTabActionStep("show the explicitly seeded initial native alcohol tolerance", pawn.ThingID, EndToEndPawnInspectTab.Health);
        yield return Shot("supporting initial condition: Boundary Twenty has seeded alcohol tolerance before action");
        yield return new PawnInspectTabActionStep("show the original physical Beer before the boundary action", pawn.ThingID, EndToEndPawnInspectTab.Gear);
        yield return Shot("Boundary Twenty holds one original Beer before native undraft");
        yield return new AssertionStep("arm passive observation at exact equality while native time remains paused", _ =>
        {
            RequirePausedEquality(); AssertUnconsumed();
            EndToEndAssert.True(pawn.Drafted && pawn.CurJobDef == JobDefOf.Wait_Combat && pawn.jobs.IsCurrentJobPlayerInterruptible());
            EndToEndAssert.True(decisions.Count == 0 && decays.Count == 0);
            armed = true; phase = "native equality action";
        });
        var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { pawn.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == true && option.HotKeyDefName == "Command_ColonistDraft");
        yield return new GizmoActionStep("undraft through the native gizmo at exact positive equality", new[] { pawn.ThingID },
            toggle.RuntimeType, EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
        yield return new AssertionStep("observe the final ordinary scheduled rejection at exactly twenty percent before any tick", _ =>
        {
            RequirePausedEquality(); AssertUnconsumed();
            EndToEndAssert.False(pawn.Drafted || pawn.CurJobDef == JobDefOf.Ingest);
            EndToEndAssert.True(decisions.Count > 0 && decisions.All(value => value.Tick == seedTick && value.ExitTick == seedTick &&
                value.Before == Limit && value.After == Limit && value.AllowedBefore == Limit && value.AllowedCalls == 1 &&
                value.Completed && value.RanOriginal && !value.Result && !value.HadHistory && value.SameTolerance));
            equalityTick = Find.TickManager.TicksGame; phase = "observed equality rejection";
        });
        yield return Shot("after actual native equality decisions Boundary Twenty retains its Beer");
        yield return new CheckpointStep("actual final ShouldTry rejection at raw equality; no health tick elapsed", _ => Evidence());
        yield return Run("allow ordinary native health decay after the observed equality rejection");
        yield return Wait("observe the same native tolerance decrease through its actual health interval", _ =>
        {
            RequireAvailable(); AssertUnconsumed();
            if (decays.Count == 0) return false;
            EndToEndAssert.True(decays[0].Before == Limit && decays[0].After < Limit && decays[0].RanOriginal &&
                decays[0].Tick > equalityTick && tolerance!.Severity < Limit);
            phase = "native decay below limit"; return true;
        }, 1800, 60);
        yield return Pause("pause after native decay with the original dose still unconsumed");
        yield return new PawnInspectTabActionStep("show native health after actual tolerance decay", pawn.ThingID, EndToEndPawnInspectTab.Health);
        yield return Shot("native alcohol tolerance has decayed before the physical Beer is consumed");
        yield return new CheckpointStep("actual native health interval lowered the unchanged starting hediff", _ => Evidence());
        yield return Run("let ordinary scheduling obtain and ingest the now-permitted dose");
        yield return Wait("observe a real native scheduled job chewing the exact original Beer below the limit", _ =>
        {
            RequireAvailable(); AssertUnconsumed();
            var job = pawn.CurJob;
            if (job?.def != JobDefOf.Ingest || pawn.jobs.curDriver is not JobDriver_Ingest driver || !driver.GainingNutritionNow) return false;
            EndToEndAssert.True(!job.playerForced && job.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy) &&
                ReferenceEquals(job.targetA.Thing, beer) && ReferenceEquals(pawn.carryTracker.CarriedThing, beer));
            EndToEndAssert.True(decisions.Any(value => value.Completed && value.RanOriginal && value.Result && value.AllowedCalls == 1 &&
                value.SameTolerance && !value.HadHistory && value.Before < Limit && value.After < Limit && value.Tick >= decays[0].Tick));
            observedJob = job; observedJobLoadId = job.loadID; observedJobTick = Find.TickManager.TicksGame;
            phase = "ordinary scheduled ingestion below limit"; return true;
        }, 1800, 60);
        yield return Pause("pause the native scheduled chewing job before consumption");
        yield return Frame("frame the pawn carrying its original Beer during actual native ingestion");
        yield return Shot("Boundary Twenty performs the ordinary Beer job after natural decay");
        yield return new CheckpointStep("native job and immutable loadID before actual consumption", _ => Evidence());
        yield return Run("finish the observed native Beer job");
        yield return Wait("observe that exact native job consume its physical Beer and apply ordinary effects", _ =>
        {
            RequireAvailable();
            if (!beer.Destroyed || !pawn.drugs.HasEverTaken(beerDef)) return false;
            EndToEndAssert.True(ingestionCalls == 1 && ingestionSameJob && ingestionNativeSource &&
                ingestionJobLoadId == observedJobLoadId && ingestionTick >= observedJobTick);
            EndToEndAssert.True(HeldBeer() == 0 && Severity(highDef) > 0 && tolerance!.Severity > 0);
            completedTick = Find.TickManager.TicksGame; phase = "native ingestion result"; return true;
        }, 1800, 60);
        yield return Pause("pause after the actual scheduled dose");
        yield return new PawnInspectTabActionStep("show native alcohol effects after actual Beer ingestion", pawn.ThingID, EndToEndPawnInspectTab.Health);
        yield return Shot("Boundary Twenty has ordinary alcohol effects after the same scheduled job consumed its Beer");
        yield return new PawnInspectTabActionStep("show the now-empty original Beer inventory", pawn.ThingID, EndToEndPawnInspectTab.Gear);
        yield return Shot("the original physical Beer is gone after the native dose");
        yield return new NativeDeveloperLogActionStep("open the native console after the boundary workflow", true);
        yield return new NativeWindowCaptureStep("retain this boundary workflow's exact native console", windows =>
        {
            RequireAvailable();
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Missing native console.");
            EndToEndAssert.True(windows.Any(window => window.NativeId == ownedLog.ID && window.RuntimeType == ownedLog.GetType().FullName));
        });
        yield return Shot("native console after equality rejection, natural decay and actual ingestion");
        yield return new NativeDeveloperLogActionStep("close the native console after boundary evidence", false);
        yield return new CheckpointStep("final scoped positive-boundary evidence; B/C and full S138 remain open", _ => Evidence());
    }

    private void SeedStartingTolerance()
    {
        AssertUnseeded();
        EndToEndAssert.True(Find.TickManager.CurTimeSpeed == TimeSpeed.Paused && pawn.CurJobDef == JobDefOf.Wait_Combat &&
            pawn.jobs.IsCurrentJobPlayerInterruptible() && seedWrites == 0 && !armed);
        tolerance = HediffMaker.MakeHediff(toleranceDef, pawn) as HediffWithComps
            ?? throw new InvalidOperationException("Expected the native tolerance hediff with its ordinary comps.");
        tolerance.Severity = Limit; seedWrites++;
        pawn.health.AddHediff(tolerance);
        decayComp = tolerance.TryGetComp<HediffComp_SeverityPerDay>();
        EndToEndAssert.True(decayComp != null && decayComp.severityPerDay == -.016f);
        seedTick = Find.TickManager.TicksGame; phase = "seeded initial precondition only";
        RequirePausedEquality();
    }
    private void AssertUnseeded()
    {
        RequireAvailable(); AssertUnconsumed();
        EndToEndAssert.True(pawn.Drafted && seedWrites == 0 && tolerance == null && Severity(toleranceDef) == 0f);
    }
    private void AssertUnconsumed()
    {
        EndToEndAssert.True(!beer.Destroyed && beer.stackCount == 1 && HeldBeer() == 1 &&
            (ReferenceEquals(beer.holdingOwner, pawn.inventory.innerContainer) || ReferenceEquals(pawn.carryTracker.CarriedThing, beer)));
        EndToEndAssert.False(pawn.drugs.HasEverTaken(beerDef)); EndToEndAssert.Equal(0f, Severity(highDef));
        EndToEndAssert.Equal(0, ingestionCalls);
    }
    private void RequirePausedEquality()
    {
        RequireAvailable();
        EndToEndAssert.True(Find.TickManager.CurTimeSpeed == TimeSpeed.Paused && Find.TickManager.TicksGame == seedTick &&
            seedWrites == 1 && tolerance!.Severity == Limit && decays.Count == 0);
    }
    private void Install(MethodInfo target, string prefix, string? postfix = null)
    {
        traced.Add(target);
        HarmonyMethod Callback(string name) => new(typeof(NativeTolerancePositiveBoundaryFixture).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!);
        traceOwner.Patch(target, prefix: Callback(prefix), postfix: postfix == null ? null : Callback(postfix));
        RequireAvailable();
    }
    private bool Tracks(Pawn subject, ThingDef drug) => armed && ReferenceEquals(Current.Game, game) &&
        ReferenceEquals(subject, pawn) && ReferenceEquals(drug, beerDef);
    private static void BeginDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        var fixture = active;
        if (fixture == null || !fixture.Tracks(__instance.pawn, __0)) return;
        if (fixture.decisions.Count >= 1024) { fixture.traceFault = true; return; }
        var decision = new Decision
        {
            Tick = Find.TickManager.TicksGame, Before = fixture.tolerance!.Severity,
            HadHistory = __instance.HasEverTaken(__0), SameTolerance = ReferenceEquals(fixture.tolerance,
                __instance.pawn.health.hediffSet.GetFirstHediffOfDef(fixture.toleranceDef))
        };
        fixture.decisions.Add(decision); fixture.pending.Push(decision);
    }
    private static void ObserveAllowed(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        var fixture = active;
        if (fixture == null || !fixture.Tracks(__instance.pawn, __0) || fixture.traceFault) return;
        if (fixture.pending.Count == 0) { fixture.traceFault = true; return; }
        var decision = fixture.pending.Peek(); decision.AllowedCalls++; decision.AllowedBefore = fixture.tolerance!.Severity;
    }
    private static void CompleteDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0, bool __result, bool __runOriginal)
    {
        var fixture = active;
        if (fixture == null || !fixture.Tracks(__instance.pawn, __0) || fixture.traceFault) return;
        if (fixture.pending.Count == 0) { fixture.traceFault = true; return; }
        var decision = fixture.pending.Pop(); decision.Completed = true; decision.Result = __result;
        decision.RanOriginal = __runOriginal; decision.After = fixture.tolerance!.Severity; decision.ExitTick = Find.TickManager.TicksGame;
    }
    private static void BeginHealthInterval(HediffWithComps __instance)
    {
        var fixture = active;
        if (fixture == null || !fixture.armed || !ReferenceEquals(Current.Game, fixture.game) || !ReferenceEquals(__instance, fixture.tolerance)) return;
        if (fixture.healthPending) fixture.traceFault = true;
        fixture.healthPending = true; fixture.healthBefore = __instance.Severity;
    }
    private static void CompleteHealthInterval(HediffWithComps __instance, bool __runOriginal)
    {
        var fixture = active;
        if (fixture == null || !fixture.armed || !ReferenceEquals(Current.Game, fixture.game) || !ReferenceEquals(__instance, fixture.tolerance)) return;
        if (!fixture.healthPending || !__runOriginal || fixture.decays.Count >= 128) fixture.traceFault = true;
        if (__instance.Severity < fixture.healthBefore)
            fixture.decays.Add(new Decay(fixture.healthBefore, __instance.Severity, Find.TickManager.TicksGame, __runOriginal));
        fixture.healthPending = false;
    }
    private static void ObserveIngestion(Thing __instance, Pawn __0)
    {
        var fixture = active;
        if (fixture == null || !fixture.armed || !ReferenceEquals(Current.Game, fixture.game) ||
            !ReferenceEquals(__instance, fixture.beer) || !ReferenceEquals(__0, fixture.pawn)) return;
        // Value-only provenance, never a target call, ref argument, result/job/hediff mutation.
        fixture.ingestionCalls++; fixture.ingestionTick = Find.TickManager.TicksGame;
        fixture.ingestionJobLoadId = __0.CurJob?.loadID ?? -1;
        fixture.ingestionSameJob = ReferenceEquals(__0.CurJob, fixture.observedJob);
        fixture.ingestionNativeSource = __0.CurJob?.def == JobDefOf.Ingest && !__0.CurJob.playerForced &&
            __0.CurJob.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy) && ReferenceEquals(__0.CurJob.targetA.Thing, __instance);
    }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && ReferenceEquals(Find.CurrentMap, map) &&
        Current.ProgramState == ProgramState.Playing, "Refuse an unrelated Game/map during boundary work or cleanup.");
    private void RequireAvailable()
    {
        RequireGame();
        EndToEndAssert.True(ReferenceEquals(RuntimeField.GetValue(mod), runtime) && runtime.Available,
            "The actual atomic tolerance enforcement/editor group must remain available: " + runtime.StatusKey);
        EndToEndAssert.True(!traceFault && pending.Count == 0 && !healthPending, "Passive native observations must remain matched and bounded.");
        foreach (var target in traced)
        {
            var info = HarmonyOwner.GetPatchInfo(target);
            EndToEndAssert.True(info != null && info.Prefixes.Count(patch => patch.owner == traceOwner.Id) == 1 &&
                info.Postfixes.Count(patch => patch.owner == traceOwner.Id) == (target == ShouldTarget || target == HealthTarget ? 1 : 0));
            if (target == AllowedTarget)
                EndToEndAssert.True(info!.Prefixes.Any(patch => patch.owner == "fumblesneeze.megamod") &&
                    info.Finalizers.Any(patch => patch.owner == "fumblesneeze.megamod"), "Current product decision ownership is required.");
        }
        if (policy != null)
        {
            EndToEndAssert.True(ReferenceEquals(pawn.drugs.CurrentPolicy, policy) && game.drugPolicyDatabase.AllPolicies.Contains(policy) &&
                ReferenceEquals(policy[beerDef], entry) && NativeDrugToleranceState.ReadEncoded(entry) == Choice &&
                entry.allowScheduled && !entry.allowedForJoy && !entry.allowedForAddiction && entry.takeToInventory == 0 &&
                entry.daysFrequency == 1f && entry.onlyIfMoodBelow == 1f && entry.onlyIfJoyBelow == 1f);
            EndToEndAssert.False(pawn.Dead || pawn.Downed || pawn.InMentalState);
        }
        if (tolerance != null)
            EndToEndAssert.True(seedWrites == 1 && ReferenceEquals(tolerance, pawn.health.hediffSet.GetFirstHediffOfDef(toleranceDef)) &&
                ReferenceEquals(decayComp, tolerance.TryGetComp<HediffComp_SeverityPerDay>()) && decayComp!.severityPerDay == -.016f,
                "The same native tolerance and unchanged native decay comp must survive the single initial seed.");
    }
    private IReadOnlyDictionary<string, string> Evidence()
    {
        RequireAvailable();
        var result = new Dictionary<string, string>
        {
            ["scope"] = "One seeded20% equality, actual native rejection, native decay and same-dose scheduled ingestion; no natural-accumulation/B/C/full-S138 claim",
            ["phase"] = phase, ["pawn"] = pawn.ThingID, ["tick"] = Find.TickManager.TicksGame.ToString(),
            ["available"] = runtime.Available.ToString(), ["traceOwner"] = traceOwner.Id,
            ["policy"] = policy!.id + ":" + policy.label, ["choice"] = NativeDrugToleranceState.ReadEncoded(entry),
            ["seedWrites"] = seedWrites.ToString(), ["seedTick"] = seedTick.ToString(), ["equalityTick"] = equalityTick.ToString(),
            ["tolerance"] = Number(Severity(toleranceDef)), ["alcohol"] = Number(Severity(highDef)),
            ["nativeDecayRate"] = decayComp == null ? "not seeded" : Number(decayComp.severityPerDay),
            ["source"] = beer.ThingID + ":destroyed=" + beer.Destroyed + ":held=" + HeldBeer(),
            ["everTaken"] = pawn.drugs.HasEverTaken(beerDef).ToString(), ["currentJob"] = pawn.CurJob?.ToString() ?? "none",
            ["observedJobLoadId"] = observedJobLoadId.ToString(), ["observedJobTick"] = observedJobTick.ToString(),
            ["ingestionCalls"] = ingestionCalls.ToString(), ["ingestionJobLoadId"] = ingestionJobLoadId.ToString(),
            ["ingestionSameJob"] = ingestionSameJob.ToString(), ["ingestionNativeSource"] = ingestionNativeSource.ToString(),
            ["ingestionTick"] = ingestionTick.ToString(), ["completedTick"] = completedTick.ToString(),
            ["nativeOperations"] = nativeOperations.ToString(), ["screenshots"] = screenshots.ToString(),
            ["maximumWaitSeconds"] = waitSeconds.ToString(),
            ["countedFixedAllowanceSeconds"] = (nativeOperations * 15 + screenshots * 30 + waitSeconds).ToString()
        };
        // One bounded field per observation stream avoids the checkpoint's 256-field ceiling.
        result["scheduledDecisions"] = string.Join("\n", decisions.Select((value, index) => index + ":" + value));
        result["nativeDecays"] = string.Join("\n", decays.Select((value, index) => index + ":" + value));
        foreach (var target in traced)
        {
            var info = HarmonyOwner.GetPatchInfo(target)!;
            result[target.DeclaringType!.Name + "." + target.Name] = string.Join("; ",
                info.Prefixes.Select(patch => "prefix:" + patch.owner + ":" + patch.PatchMethod.Name)
                .Concat(info.Postfixes.Select(patch => "postfix:" + patch.owner + ":" + patch.PatchMethod.Name))
                .Concat(info.Transpilers.Select(patch => "transpiler:" + patch.owner + ":" + patch.PatchMethod.Name))
                .Concat(info.Finalizers.Select(patch => "finalizer:" + patch.owner + ":" + patch.PatchMethod.Name)));
        }
        return result;
    }
    private float Severity(HediffDef def) => pawn.health.hediffSet.GetFirstHediffOfDef(def)?.Severity ?? 0f;
    private int HeldBeer() => pawn.inventory.innerContainer.TotalStackCountOfDef(beerDef) +
        (pawn.carryTracker.CarriedThing?.def == beerDef ? pawn.carryTracker.CarriedThing.stackCount : 0);
    private CameraActionStep Frame(string name) => new(name, new[] { pawn.ThingID }, paddingPixels: 200);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static TimeControlActionStep Pause(string name) => new(name, true, EndToEndGameSpeed.Normal);
    private static TimeControlActionStep Run(string name) => new(name, false, EndToEndGameSpeed.Normal);
    private static WaitUntilStep Wait(string name, Func<IEndToEndContext, bool> predicate, int ticks, int seconds) =>
        new(name, predicate, new EndToEndDeadline(int.MaxValue, ticks, TimeSpan.FromSeconds(seconds)));
    private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed class Decision
    {
        internal int Tick, ExitTick = -1, AllowedCalls;
        internal float Before, After, AllowedBefore;
        internal bool Completed, RanOriginal, Result, HadHistory, SameTolerance;
        public override string ToString() => "tick=" + Tick + ":exit=" + ExitTick + ":before=" + Number(Before) + ":after=" + Number(After) +
            ":allowedInput=" + Number(AllowedBefore) + ":allowedCalls=" + AllowedCalls + ":completed=" + Completed +
            ":original=" + RanOriginal + ":result=" + Result + ":history=" + HadHistory + ":sameTolerance=" + SameTolerance;
    }
    private sealed class Decay
    {
        internal readonly float Before, After;
        internal readonly int Tick;
        internal readonly bool RanOriginal;
        internal Decay(float before, float after, int tick, bool ranOriginal)
        { Before = before; After = after; Tick = tick; RanOriginal = ranOriginal; }
        public override string ToString() => "tick=" + Tick + ":before=" + Number(Before) + ":after=" + Number(After) + ":original=" + RanOriginal;
    }
}
