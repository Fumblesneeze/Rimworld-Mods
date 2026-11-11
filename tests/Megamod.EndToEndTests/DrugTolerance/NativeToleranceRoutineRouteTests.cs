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

[RimWorldEndToEndTest("megamod.drug-tolerance.native-scheduled-routes", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 18000, MaxWallClockSeconds = 1440)]
public sealed class NativeToleranceScheduledRouteTests : IRimWorldEndToEndTest
{
    private readonly NativeToleranceRouteFixture fixture = new(false);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

[RimWorldEndToEndTest("megamod.drug-tolerance.native-direct-joy-routes", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 24000, MaxWallClockSeconds = 1440)]
public sealed class NativeToleranceDirectJoyRouteTests : IRimWorldEndToEndTest
{
    private readonly NativeToleranceRouteFixture fixture = new(true);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

// Private native S138 fixtures; the orchestrator owns reviewed-build acceptance.
// Only disposable preconditions are constructed. Engine thinking after native undraft/unpause
// selects every routine job. The sole manual order is the ordinary native Beer float-menu action.
internal sealed class NativeToleranceRouteFixture
{
    private static readonly FieldInfo RuntimeField = typeof(MegamodMod).GetField("drugTolerance", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo ScheduledTarget = typeof(Pawn_DrugPolicyTracker).GetMethod(nameof(Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow))!;
    private static readonly MethodInfo JoyTarget = typeof(JoyGiver_Ingest).GetMethod("CanIngestForJoy", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static NativeToleranceRouteFixture? active;
    private readonly bool directJoy;
    private readonly string marker = "ToleranceRoutes-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private readonly List<Subject> subjects = new();
    private readonly List<DrugPolicy> policies = new();
    private readonly List<Thing> ownedThings = new();
    private readonly List<MethodInfo> tracedTargets = new();
    private Game game = null!;
    private Map map = null!;
    private MegamodMod mod = null!;
    private NativeDrugToleranceRuntime runtime = null!;
    private ThingDef beerDef = null!;
    private HediffDef highDef = null!, toleranceDef = null!;
    private HarmonyOwner traceOwner = null!;
    private Thing? manualBeer;
    private EditWindow_Log? ownedLog;
    private int observationStart = -1;
    private int nativeOperations, screenshots, waitSeconds;
    private string phase = "arranged";

    internal NativeToleranceRouteFixture(bool directJoy) => this.directJoy = directJoy;

    internal void Arrange(IEndToEndContext context)
    {
        game = Current.Game; map = Find.CurrentMap;
        RequireGame();
        EndToEndAssert.True(game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(Prefs.DevMode && Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<EditWindow_Log>());
        EndToEndAssert.True(active == null, "A previous route trace must not survive its fixture.");
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
        EndToEndAssert.True(beerDef.ingestible.defaultNumToIngestAtOnce == 1 && beerDef.ingestible.joy > 0 &&
            beerDef.ingestible.joyKind.defName == "Chemical");
        EndToEndAssert.True(beerDef.ingestible.outcomeDoers.OfType<IngestionOutcomeDoer_GiveHediff>()
            .Any(value => ReferenceEquals(value.hediffDef, toleranceDef)));
        var offsets = new[] { new IntVec3(-4, 0, 0), IntVec3.Zero, new IntVec3(4, 0, 0), new IntVec3(-4, 0, 2) };
        var center = CellRect.CenteredOn(map.Center, 15).OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(cell => offsets.All(offset => (cell + offset).InBounds(map) && (cell + offset).Standable(map) && !(cell + offset).Fogged(map)));
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
        string prefix = directJoy ? "Joy" : "Scheduled";
        subjects.Add(CreateSubject(prefix + " Zero", center + offsets[0], any: false, drugDesire: false));
        subjects.Add(CreateSubject(prefix + " Any", center + offsets[1], any: true, drugDesire: false));
        subjects.Add(CreateSubject(prefix + " Trait Zero", center + offsets[2], any: false, drugDesire: true));
        if (!directJoy)
        {
            manualBeer = ThingMaker.MakeThing(beerDef); manualBeer.stackCount = 1;
            ownedThings.Add(manualBeer);
            GenSpawn.Spawn(manualBeer, center + offsets[3], map);
            manualBeer.SetForbidden(false, false);
        }
        traceOwner = new HarmonyOwner("fumblesneeze.tests.s138.routes." + marker);
        active = this;
        context.DeferCleanup(() =>
        {
            if (ReferenceEquals(active, this)) active = null;
            // Removing only this exact test owner is safe even if the disposable Game was lost.
            foreach (var target in tracedTargets) traceOwner.Unpatch(target, HarmonyPatchType.All, traceOwner.Id);
            foreach (var target in tracedTargets)
                EndToEndAssert.False(HarmonyOwner.GetPatchInfo(target)?.Owners.Contains(traceOwner.Id) == true);
        });
        InstallTrace(ScheduledTarget, nameof(ObserveScheduled));
        InstallTrace(JoyTarget, nameof(ObserveJoy));
        AssertInitial();
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
        for (int hour = 0; hour < 24; hour++) pawn.timetable.SetAssignment(hour, directJoy ? TimeAssignmentDefOf.Joy : TimeAssignmentDefOf.Anything);
        pawn.needs.food.CurLevelPercentage = 1f;
        pawn.needs.rest.CurLevelPercentage = 1f;
        pawn.needs.mood.CurLevelPercentage = 1f;
        pawn.needs.joy.CurLevelPercentage = directJoy ? .05f : 1f;
        if (directJoy)
            foreach (var kind in DefDatabase<JoyKindDef>.AllDefsListForReading)
                if (kind != beerDef.ingestible.joyKind) pawn.needs.joy.tolerances.Notify_JoyGained(2f, kind);
        var policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); policies.Add(policy);
        policy.label = marker + " " + label;
        for (int i = 0; i < policy.Count; i++)
        {
            policy[i].allowedForAddiction = false; policy[i].allowedForJoy = false;
            policy[i].allowScheduled = false; policy[i].takeToInventory = 0;
        }
        var entry = policy[beerDef];
        entry.allowedForJoy = directJoy; entry.allowScheduled = !directJoy;
        entry.daysFrequency = 1f; entry.onlyIfMoodBelow = 1f; entry.onlyIfJoyBelow = 1f;
        // Native DropUnusedInventory would otherwise drop unscheduled carried drugs in home area.
        // No map supply exists in the joy fixture; this retained-stock setup does not test refill.
        entry.takeToInventory = directJoy ? 1 : 0;
        NativeDrugToleranceState.WriteEncoded(entry, any ? "1:any" : "1:limit:0");
        pawn.drugs.CurrentPolicy = policy;
        var supply = ThingMaker.MakeThing(beerDef); supply.stackCount = 1; ownedThings.Add(supply);
        EndToEndAssert.True(pawn.inventory.innerContainer.TryAdd(supply));
        GenSpawn.Spawn(pawn, cell, map);
        pawn.drafter.Drafted = true;
        return new Subject(pawn, policy, entry, supply, any, drugDesire);
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
        yield return new AssertionStep("verify the complete compiled route sequence fits its declared wall allowance", _ =>
        {
            EndToEndAssert.Equal(directJoy ? 36 : 38, nativeOperations);
            EndToEndAssert.Equal(directJoy ? 9 : 10, screenshots);
            EndToEndAssert.Equal(directJoy ? 450 : 360, waitSeconds);
            EndToEndAssert.True(nativeOperations * 15 + screenshots * 30 + waitSeconds + 180 <= 1440);
        });
        yield return new CheckpointStep("complete route operation and timeout accounting", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> Workflow(IEndToEndContext context)
    {
        if (directJoy)
        {
            yield return Run("advance native time past Core's first-5000-tick joy gate while every subject stays drafted", EndToEndGameSpeed.Superfast);
            yield return Wait("reach native joy start time without an ingestion or forced tick assignment", _ =>
            {
                AssertInitial(); return Find.TickManager.TicksGame >= 5000;
            }, 6000, 90);
            yield return Pause("pause before the direct-recreation player workflow");
        }
        yield return new CheckpointStep("passive route setup and current real patch ownership", _ => Evidence());
        yield return Frame("frame the three named route subjects before native actions");
        foreach (var subject in subjects)
        {
            yield return Select(subject, "select starting supply for ");
            yield return new PawnInspectTabActionStep("show starting Beer in " + subject.Name, subject.Pawn.ThingID, EndToEndPawnInspectTab.Gear);
            yield return Shot(subject.Name + " holds one Beer before native undraft");
        }
        yield return new AssertionStep("begin bounded native attempt observation", _ =>
        {
            AssertInitial(); observationStart = Find.TickManager.TicksGame; phase = "routine";
            foreach (var subject in subjects) subject.TraceArmed = true;
        });
        foreach (var step in Undraft(context, subjects[0])) yield return step;
        if (!directJoy)
            foreach (var step in Undraft(context, subjects[2])) yield return step;
        foreach (var step in Undraft(context, subjects[1])) yield return step;
        foreach (var step in Consume(context, subjects[1], subjects[1].Supply, manual: false)) yield return step;
        if (directJoy)
        {
            // The second brief positive job starts only after the first action/result captures.
            foreach (var step in Undraft(context, subjects[2])) yield return step;
            foreach (var step in Consume(context, subjects[2], subjects[2].Supply, manual: false)) yield return step;
        }
        yield return Run("observe blocked choices through ordinary repeated engine selection", EndToEndGameSpeed.Superfast);
        yield return Wait("require real candidate attempts and unchanged blocked supply after the positive control", _ =>
        {
            RequireAvailable();
            foreach (var subject in BlockedSubjects()) AssertBlocked(subject);
            return Find.TickManager.TicksGame - observationStart >= 600 &&
                BlockedSubjects().All(subject => RelevantAttempts(subject).DistinctTicks >= 2);
        }, 6000, 120);
        yield return Pause("pause after observed blocked native selections");
        foreach (var subject in BlockedSubjects())
        {
            yield return Select(subject, "select observed blocked subject ");
            yield return new PawnInspectTabActionStep("show retained Beer after blocked attempts for " + subject.Name,
                subject.Pawn.ThingID, EndToEndPawnInspectTab.Gear);
            yield return Shot(subject.Name + " retains its Beer after actual native attempts and the Any dose");
        }
        yield return new CheckpointStep("paired native routine results and exact selection attempts", _ => Evidence());
        if (!directJoy)
        {
            var subject = subjects[0];
            yield return Select(subject, "select the blocked pawn for its explicit manual order ");
            EndToEndAssert.True(manualBeer is { Spawned: true, stackCount: 1 });
            var expected = beerDef.ingestible.ingestCommandString.Formatted(manualBeer!.LabelShort).ToString();
            var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(subject.Pawn.ThingID, manualBeer.ThingID);
            var consume = options.Where(option => !option.Disabled && string.Equals(option.Label, expected, StringComparison.Ordinal)).ToArray();
            EndToEndAssert.Equal(1, consume.Length, "Expected the exact native Drink Beer option; observed " + string.Join(" | ", options.Select(option => option.Label)));
            yield return new AssertionStep("retain zero and physical ground supply before manual order", _ =>
            {
                AssertBlocked(subject); phase = "manual exclusion";
                subject.AllowIngestion = true;
            });
            yield return new FloatMenuActionStep("order the native Drink Beer option despite retained numeric zero",
                subject.Pawn.ThingID, manualBeer.ThingID, consume[0].StableId);
            foreach (var step in Consume(context, subject, manualBeer, manual: true)) yield return step;
        }
        yield return new NativeDeveloperLogActionStep("open the native console after the route workflows", true);
        yield return new NativeWindowCaptureStep("retain only this workflow's exact native console", windows =>
        {
            RequireAvailable();
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Missing native console.");
            EndToEndAssert.True(windows.Any(window => window.NativeId == ownedLog.ID && window.RuntimeType == ownedLog.GetType().FullName));
        });
        yield return Shot("native console after actual tolerance route actions");
        yield return new NativeDeveloperLogActionStep("close the native console after route evidence", false);
        yield return new CheckpointStep("final native route evidence; full S138 remains open", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> Undraft(IEndToEndContext context, Subject subject)
    {
        yield return Select(subject, "select drafted route subject ");
        EndToEndAssert.True(subject.Pawn.Drafted);
        var toggle = DraftToggle(context, subject, true);
        yield return new GizmoActionStep("undraft " + subject.Name + " through its native gizmo", new[] { subject.Pawn.ThingID },
            toggle.RuntimeType, EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
    }

    private IEnumerable<EndToEndStep> Consume(IEndToEndContext context, Subject subject, Thing source, bool manual)
    {
        yield return new AssertionStep("record the pending native ingestion observation for " + subject.Name, _ =>
        {
            RequireAvailable(); subject.AllowIngestion = true;
            EndToEndAssert.False(subject.Pawn.Drafted);
            EndToEndAssert.False(subject.Pawn.drugs.HasEverTaken(beerDef));
        });
        yield return Run("let ordinary native ingestion start for " + subject.Name, EndToEndGameSpeed.Normal);
        yield return Wait("observe the actual selected Beer ingestion job for " + subject.Name, _ =>
        {
            ObserveBlocked();
            var job = subject.Pawn.CurJob;
            if (job?.def != JobDefOf.Ingest) return false;
            EndToEndAssert.True(ReferenceEquals(job.targetA.Thing, source), "The native job must target this subject's exact physical dose.");
            EndToEndAssert.Equal(manual, job.playerForced);
            if (!manual)
            {
                EndToEndAssert.Equal(directJoy ? typeof(JobGiver_GetJoy) : typeof(JobGiver_TakeDrugsForDrugPolicy), job.jobGiver?.GetType());
                EndToEndAssert.True(RelevantAttempts(subject).Count > 0,
                    "The real engine must invoke the admitted route before its selected ingestion job.");
            }
            EndToEndAssert.False(source.Destroyed || subject.Pawn.drugs.HasEverTaken(beerDef), "The action capture must precede actual consumption.");
            if (subject.Pawn.jobs.curDriver is not JobDriver_Ingest driver || !driver.GainingNutritionNow ||
                !ReferenceEquals(subject.Pawn.carryTracker.CarriedThing, source)) return false;
            subject.JobObserved = job.ToString(); subject.JobObservedTick = Find.TickManager.TicksGame;
            return true;
        }, 1800, 60);
        yield return Pause("pause the real ingestion job for " + subject.Name);
        yield return Frame("frame " + subject.Name + " during its native Beer job");
        yield return Shot(subject.Name + (manual ? " is carrying out its explicit native Drink order" : " is carrying out its ordinary native routine job"));
        yield return Run("finish ordinary Beer ingestion for " + subject.Name, EndToEndGameSpeed.Normal);
        yield return Wait("observe the real physical dose and native effects for " + subject.Name, _ =>
        {
            ObserveBlocked();
            if (!source.Destroyed || !subject.Pawn.drugs.HasEverTaken(beerDef)) return false;
            EndToEndAssert.True(Severity(subject, highDef) > 0 && Severity(subject, toleranceDef) > 0,
                "Native ingestion must create both ordinary alcohol and tolerance effects.");
            EndToEndAssert.Equal(manual ? 1 : 0, HeldBeer(subject));
            if (manual) EndToEndAssert.True(ReferenceEquals(subject.Supply.holdingOwner, subject.Pawn.inventory.innerContainer));
            subject.CompletedTick = Find.TickManager.TicksGame;
            return true;
        }, 1800, 60);
        yield return Pause("pause after the actual Beer result for " + subject.Name);
        yield return new PawnInspectTabActionStep("show native alcohol effects after ingestion for " + subject.Name,
            subject.Pawn.ThingID, EndToEndPawnInspectTab.Health);
        yield return Shot(subject.Name + " has native alcohol effects after losing exactly one physical Beer");
        yield return new CheckpointStep("native ingestion provenance for " + subject.Name, _ => Evidence());
        var toggle = DraftToggle(context, subject, false);
        yield return new GizmoActionStep("draft the completed subject through its native gizmo", new[] { subject.Pawn.ThingID },
            toggle.RuntimeType, EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
    }

    private IEnumerable<Subject> BlockedSubjects() => subjects.Where((_, index) => index == 0 || (!directJoy && index == 2));
    private Attempts RelevantAttempts(Subject subject) => directJoy ? subject.JoyAttempts : subject.ScheduledAttempts;
    private void ObserveBlocked()
    {
        RequireAvailable();
        foreach (var subject in subjects.Where(value => !value.AllowIngestion)) AssertBlocked(subject);
    }
    private void AssertBlocked(Subject subject)
    {
        RequireAvailable();
        EndToEndAssert.False(subject.Pawn.InMentalState || subject.Pawn.Downed || subject.Pawn.Dead);
        EndToEndAssert.False(subject.Pawn.CurJobDef == JobDefOf.Ingest || subject.Pawn.drugs.HasEverTaken(beerDef), subject.Name + " must not ingest.");
        EndToEndAssert.True(!subject.Supply.Destroyed && subject.Supply.stackCount == 1 &&
            ReferenceEquals(subject.Supply.holdingOwner, subject.Pawn.inventory.innerContainer), subject.Name + " must retain its starting Beer.");
        EndToEndAssert.Equal(0f, Severity(subject, highDef));
        EndToEndAssert.Equal(0f, Severity(subject, toleranceDef));
    }
    private void AssertInitial()
    {
        RequireAvailable();
        foreach (var subject in subjects)
        {
            EndToEndAssert.True(subject.Pawn.Drafted);
            AssertBlocked(subject);
            EndToEndAssert.True(subject.Pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                subject.Pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
            EndToEndAssert.Equal(subject.DrugDesire ? 1 : 0, subject.Pawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire));
        }
    }

    private void InstallTrace(MethodInfo target, string callback)
    {
        tracedTargets.Add(target);
        var method = typeof(NativeToleranceRouteFixture).GetMethod(callback, BindingFlags.Static | BindingFlags.NonPublic)!;
        traceOwner.Patch(target, prefix: new HarmonyMethod(method) { priority = Priority.First });
        RequireAvailable(); // A native fallback caused by tracing is not tolerance evidence.
    }
    private static void ObserveScheduled(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        if (__instance != null) active?.RecordAttempt(__instance.pawn, __0, direct: false);
    }
    private static void ObserveJoy(JoyGiver_Ingest __instance, Pawn __0, Thing __1)
    {
        if (__instance?.GetType() == typeof(JoyGiver_TakeDrug) && __1 != null) active?.RecordAttempt(__0, __1.def, direct: true);
    }
    private void RecordAttempt(Pawn pawn, ThingDef drug, bool direct)
    {
        // No product predicate, native return, ref argument, exception or game mutation here.
        if (!ReferenceEquals(Current.Game, game) || !ReferenceEquals(drug, beerDef)) return;
        foreach (var subject in subjects)
            if (subject.TraceArmed && ReferenceEquals(subject.Pawn, pawn))
            {
                (direct ? subject.JoyAttempts : subject.ScheduledAttempts).Record(Find.TickManager.TicksGame);
                return;
            }
    }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && ReferenceEquals(Find.CurrentMap, map) &&
        Current.ProgramState == ProgramState.Playing, "Refuse an unrelated Game/map during native route work or cleanup.");
    private void RequireAvailable()
    {
        RequireGame();
        EndToEndAssert.True(ReferenceEquals(RuntimeField.GetValue(mod), runtime) && runtime.Available,
            "The real atomic enforcement/editor group must remain available: " + runtime.StatusKey);
        foreach (var target in tracedTargets)
        {
            var info = HarmonyOwner.GetPatchInfo(target);
            EndToEndAssert.True(info != null && info.Prefixes.Count(patch => patch.owner == traceOwner.Id) == 1 &&
                info.Prefixes.Any(patch => patch.owner == "fumblesneeze.megamod") &&
                info.Finalizers.Any(patch => patch.owner == "fumblesneeze.megamod"), "Required live trace/product ownership changed.");
        }
        foreach (var subject in subjects)
            EndToEndAssert.True(ReferenceEquals(subject.Pawn.drugs.CurrentPolicy, subject.Policy) &&
                game.drugPolicyDatabase.AllPolicies.Contains(subject.Policy) && ReferenceEquals(subject.Policy[beerDef], subject.Entry) &&
                NativeDrugToleranceState.ReadEncoded(subject.Entry) == (subject.Any ? "1:any" : "1:limit:0") &&
                subject.Entry.allowScheduled == !directJoy && subject.Entry.allowedForJoy == directJoy &&
                !subject.Entry.allowedForAddiction && subject.Entry.daysFrequency == 1f &&
                subject.Entry.onlyIfMoodBelow == 1f && subject.Entry.onlyIfJoyBelow == 1f &&
                subject.Entry.takeToInventory == (directJoy ? 1 : 0),
                "Current policy/row identity and arranged permissions must remain unchanged.");
    }
    private IReadOnlyDictionary<string, string> Evidence()
    {
        RequireAvailable();
        var result = new Dictionary<string, string>
        {
            ["scope"] = directJoy ? "Core direct recreation: zero/Any/positive DrugDesire; full S138 remains open" : "Core scheduled zero/Any/positive DrugDesire plus explicit manual order; full S138 remains open",
            ["phase"] = phase, ["tick"] = Find.TickManager.TicksGame.ToString(), ["observationStart"] = observationStart.ToString(),
            ["available"] = runtime.Available.ToString(), ["traceOwner"] = traceOwner.Id,
            ["nativeOperations"] = nativeOperations.ToString(), ["screenshots"] = screenshots.ToString(),
            ["maximumWaitSeconds"] = waitSeconds.ToString(),
            ["countedFixedAllowanceSeconds"] = (nativeOperations * 15 + screenshots * 30 + waitSeconds).ToString(),
            ["manualBeer"] = manualBeer == null ? "absent" : manualBeer.ThingID + ":destroyed=" + manualBeer.Destroyed
        };
        foreach (var target in tracedTargets)
        {
            var info = HarmonyOwner.GetPatchInfo(target)!;
            result[target.DeclaringType!.Name + "." + target.Name] = string.Join("; ",
                info.Prefixes.Select(patch => "prefix:" + patch.owner + ":" + patch.PatchMethod.Name)
                .Concat(info.Postfixes.Select(patch => "postfix:" + patch.owner + ":" + patch.PatchMethod.Name))
                .Concat(info.Transpilers.Select(patch => "transpiler:" + patch.owner + ":" + patch.PatchMethod.Name))
                .Concat(info.Finalizers.Select(patch => "finalizer:" + patch.owner + ":" + patch.PatchMethod.Name)));
        }
        foreach (var subject in subjects)
            result[subject.Name] = string.Join("; ", new[]
            {
                "pawn=" + subject.Pawn.ThingID, "policy=" + subject.Policy.id + ":" + subject.Policy.label,
                "choice=" + NativeDrugToleranceState.ReadEncoded(subject.Entry), "stockTarget=" + subject.Entry.takeToInventory,
                "initialSource=" + subject.Supply.ThingID, "initialSourceDestroyed=" + subject.Supply.Destroyed,
                "heldBeer=" + HeldBeer(subject), "everTaken=" + subject.Pawn.drugs.HasEverTaken(beerDef),
                "alcohol=" + Severity(subject, highDef).ToString("R", CultureInfo.InvariantCulture),
                "tolerance=" + Severity(subject, toleranceDef).ToString("R", CultureInfo.InvariantCulture),
                "joy=" + subject.Pawn.needs.joy.CurLevelPercentage.ToString("R", CultureInfo.InvariantCulture),
                "recreationBoredom=" + subject.Pawn.needs.joy.tolerances.TolerancesString(),
                "scheduledAttempts=" + subject.ScheduledAttempts, "directJoyAttempts=" + subject.JoyAttempts,
                "observedJob=" + subject.JobObserved, "jobTick=" + subject.JobObservedTick, "completionTick=" + subject.CompletedTick,
                "currentJob=" + subject.Pawn.CurJob, "mentalState=" + subject.Pawn.InMentalState
            });
        return result;
    }
    private float Severity(Subject subject, HediffDef def) => subject.Pawn.health.hediffSet.GetFirstHediffOfDef(def)?.Severity ?? 0f;
    private int HeldBeer(Subject subject) => subject.Pawn.inventory.innerContainer.TotalStackCountOfDef(beerDef) +
        (subject.Pawn.carryTracker.CarriedThing?.def == beerDef ? subject.Pawn.carryTracker.CarriedThing.stackCount : 0);
    private CameraActionStep Frame(string name) => new(name, subjects.Select(subject => subject.Pawn.ThingID), paddingPixels: 200);
    private static SelectionActionStep Select(Subject subject, string prefix) => new(prefix + subject.Name, new[] { subject.Pawn.ThingID }, additive: false);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static TimeControlActionStep Pause(string name) => new(name, true, EndToEndGameSpeed.Normal);
    private static TimeControlActionStep Run(string name, EndToEndGameSpeed speed) => new(name, false, speed);
    private static WaitUntilStep Wait(string name, Func<IEndToEndContext, bool> predicate, int ticks, int seconds) =>
        new(name, predicate, new EndToEndDeadline(int.MaxValue, ticks, TimeSpan.FromSeconds(seconds)));
    private static EndToEndGizmoOption DraftToggle(IEndToEndContext context, Subject subject, bool drafted) =>
        context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { subject.Pawn.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == drafted && option.HotKeyDefName == "Command_ColonistDraft");

    private sealed class Subject
    {
        internal readonly Pawn Pawn;
        internal readonly DrugPolicy Policy;
        internal readonly DrugPolicyEntry Entry;
        internal readonly Thing Supply;
        internal readonly bool Any, DrugDesire;
        internal readonly Attempts ScheduledAttempts = new(), JoyAttempts = new();
        internal bool TraceArmed, AllowIngestion;
        internal string JobObserved = "none";
        internal int JobObservedTick = -1, CompletedTick = -1;
        internal string Name => Pawn.Name.ToStringShort;
        internal Subject(Pawn pawn, DrugPolicy policy, DrugPolicyEntry entry, Thing supply, bool any, bool drugDesire)
        { Pawn = pawn; Policy = policy; Entry = entry; Supply = supply; Any = any; DrugDesire = drugDesire; }
    }
    private sealed class Attempts
    {
        internal int Count, DistinctTicks, FirstTick = -1, LastTick = -1;
        internal void Record(int tick)
        {
            if (Count < int.MaxValue) Count++;
            if (FirstTick < 0) FirstTick = tick;
            if (tick != LastTick && DistinctTicks < int.MaxValue) DistinctTicks++;
            LastTick = tick;
        }
        public override string ToString() => Count + " calls/" + DistinctTicks + " distinct ticks/first=" + FirstTick + "/last=" + LastTick;
    }
}
