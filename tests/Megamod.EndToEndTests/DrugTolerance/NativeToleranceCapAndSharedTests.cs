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

[RimWorldEndToEndTest("megamod.drug-tolerance.native-any-vs-hundred", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 12000, MaxWallClockSeconds = 1440)]
public sealed class NativeToleranceCapTests : IRimWorldEndToEndTest
{
    private readonly NativeToleranceCapAndSharedFixture fixture = new(false);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

[RimWorldEndToEndTest("megamod.drug-tolerance.native-shared-psychite", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 18000, MaxWallClockSeconds = 1800)]
public sealed class NativeToleranceSharedChemicalTests : IRimWorldEndToEndTest
{
    private readonly NativeToleranceCapAndSharedFixture fixture = new(true);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

// Private S138 B/C fixtures. Shared setup, passive observations and physical-dose capture only.
// B seeds the native maximum once per subject; C obtains tolerance only from real native tea orders.
// Native undraft/time/float-menu actions select and execute every claimed job. A remains separate.
internal sealed class NativeToleranceCapAndSharedFixture
{
    private static readonly FieldInfo RuntimeField = typeof(MegamodMod).GetField("drugTolerance", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo ShouldTarget = typeof(Pawn_DrugPolicyTracker).GetMethod(nameof(Pawn_DrugPolicyTracker.ShouldTryToTakeScheduledNow))!;
    private static readonly MethodInfo AllowedTarget = typeof(Pawn_DrugPolicyTracker).GetMethod(nameof(Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow))!;
    private static readonly MethodInfo HealthTarget = typeof(HediffWithComps).GetMethod(nameof(HediffWithComps.PostTickInterval), new[] { typeof(int) })!;
    private static readonly MethodInfo IngestedTarget = typeof(Thing).GetMethod(nameof(Thing.Ingested), new[] { typeof(Pawn), typeof(float) })!;
    private static readonly FieldInfo ChewingField = typeof(JobDriver_Ingest).GetField("chewing", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo ToilsField = typeof(JobDriver).GetField("toils", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static NativeToleranceCapAndSharedFixture? active;
    private readonly bool shared;
    private readonly string marker = "ToleranceCapShared-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private readonly List<Subject> subjects = new();
    private readonly List<Thing> owned = new();
    private readonly List<DrugPolicy> policies = new();
    private readonly List<MethodInfo> traced = new();
    private Game game = null!;
    private Map map = null!;
    private MegamodMod mod = null!;
    private NativeDrugToleranceRuntime runtime = null!;
    private HarmonyOwner traceOwner = null!;
    private ThingDef scheduledDef = null!, teaDef = null!;
    private HediffDef toleranceDef = null!, scheduledHigh = null!, teaHigh = null!;
    private EditWindow_Log? ownedLog;
    private bool traceFault, sharedNegativeArmed;
    private int seedTick = -1, firstTeaCompletion = -1, sharedObservationStart = -1;
    private int nativeOperations, screenshots, waitSeconds;
    private string phase = "unseeded preconditions";
    private float NativeDecayRate => shared ? -.015f : -.016f;

    internal NativeToleranceCapAndSharedFixture(bool shared) => this.shared = shared;

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
            ?? throw new InvalidOperationException("The actual tolerance runtime has not been registered.");
        RequireAvailable();
        scheduledDef = DefDatabase<ThingDef>.GetNamed(shared ? "Yayo" : "Beer");
        toleranceDef = DefDatabase<HediffDef>.GetNamed(shared ? "PsychiteTolerance" : "AlcoholTolerance");
        scheduledHigh = DefDatabase<HediffDef>.GetNamed(shared ? "YayoHigh" : "AlcoholHigh");
        AssertNativeDoseMapping(scheduledDef);
        EndToEndAssert.True(toleranceDef.maxSeverity == 1f && !ModsConfig.BiotechActive &&
            ChewingField.FieldType == typeof(Toil) && ToilsField.FieldType == typeof(List<Toil>));
        if (shared)
        {
            teaDef = DefDatabase<ThingDef>.GetNamed("PsychiteTea"); teaHigh = DefDatabase<HediffDef>.GetNamed("PsychiteTeaHigh");
            AssertNativeDoseMapping(teaDef);
        }
        context.DeferCleanup(() =>
        {
            RequireGame();
            var failures = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { failures.Add(error); } }
            Attempt(() => { if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(false); });
            Attempt(() => Find.Selector.ClearSelection());
            foreach (var thing in owned.AsEnumerable().Reverse())
                Attempt(() => { if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish); });
            foreach (var policy in policies)
                Attempt(() => { if (policy.label.StartsWith(marker, StringComparison.Ordinal)) game.drugPolicyDatabase.AllPolicies.Remove(policy); });
            if (failures.Count != 0) throw new AggregateException("Tolerance B/C owned-resource cleanup failed.", failures);
        });
        var offsets = new[] { new IntVec3(-3, 0, 0), new IntVec3(3, 0, 0), new IntVec3(-3, 0, 2), new IntVec3(3, 0, 2) };
        var center = CellRect.CenteredOn(map.Center, 20).OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(cell => offsets.All(offset => (cell + offset).InBounds(map) && (cell + offset).Standable(map) &&
                !(cell + offset).Fogged(map) && (cell + offset).GetThingList(map).Count == 0));
        subjects.Add(CreateSubject(shared ? "Psychite Two" : "Numeric Hundred", center + offsets[0], shared ? .02f : 1f, false));
        subjects.Add(CreateSubject(shared ? "Psychite Four" : "Cap Any", center + offsets[1], shared ? .04f : 1f, !shared));
        if (shared)
            for (int index = 0; index < subjects.Count; index++)
            {
                var tea = NewThing(teaDef);
                GenSpawn.Spawn(tea, center + offsets[index + 2], map); tea.SetForbidden(false, false);
                subjects[index].Tea = new Dose(tea, teaHigh, true);
            }
        traceOwner = new HarmonyOwner("fumblesneeze.tests.s138.cap-shared." + marker); active = this;
        context.DeferCleanup(() =>
        {
            if (ReferenceEquals(active, this)) active = null;
            var failures = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { failures.Add(error); } }
            foreach (var target in traced)
                Attempt(() => traceOwner.Unpatch(target, HarmonyPatchType.All, traceOwner.Id));
            foreach (var target in traced)
                Attempt(() => EndToEndAssert.False(HarmonyOwner.GetPatchInfo(target)?.Owners.Contains(traceOwner.Id) == true));
            if (failures.Count != 0) throw new AggregateException("Tolerance B/C observer cleanup failed.", failures);
        });
        Install(ShouldTarget, nameof(BeginDecision), nameof(CompleteDecision));
        Install(AllowedTarget, nameof(ObserveAllowed));
        Install(HealthTarget, nameof(BeginHealth), nameof(CompleteHealth));
        Install(IngestedTarget, nameof(ObserveIngestion));
        AssertUnseeded();
    }

    private void AssertNativeDoseMapping(ThingDef drug) => EndToEndAssert.True(drug.IsDrug && drug.IsAddictiveDrug &&
        drug.ingestible.defaultNumToIngestAtOnce == 1 && drug.ingestible.outcomeDoers.OfType<IngestionOutcomeDoer_GiveHediff>()
            .Any(value => ReferenceEquals(value.hediffDef, toleranceDef) && value.toleranceChemical?.toleranceHediff == toleranceDef));
    private Thing NewThing(ThingDef def)
    {
        var thing = ThingMaker.MakeThing(def); thing.stackCount = 1; owned.Add(thing); return thing;
    }
    private Subject CreateSubject(string label, IntVec3 cell, float limit, bool any)
    {
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowFood: false, allowAddictions: false,
            fixedBiologicalAge: 25, fixedChronologicalAge: 25, forceNoIdeo: true, forceNoGear: true));
        owned.Add(pawn); pawn.Name = new NameTriple("Tolerance", label, marker);
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray()) pawn.health.RemoveHediff(hediff);
        foreach (var trait in pawn.story.traits.allTraits.ToArray()) pawn.story.traits.RemoveTrait(trait);
        pawn.inventory.innerContainer.ClearAndDestroyContents(); pawn.workSettings.EnableAndInitialize();
        foreach (var work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (!pawn.WorkTypeIsDisabled(work)) pawn.workSettings.SetPriority(work, 0);
        for (int hour = 0; hour < 24; hour++) pawn.timetable.SetAssignment(hour, TimeAssignmentDefOf.Anything);
        pawn.needs.food.CurLevelPercentage = 1f; pawn.needs.rest.CurLevelPercentage = 1f;
        pawn.needs.mood.CurLevelPercentage = 1f; pawn.needs.joy.CurLevelPercentage = 1f;
        var policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); policies.Add(policy); policy.label = marker + " " + label;
        for (int index = 0; index < policy.Count; index++)
        {
            policy[index].allowScheduled = false; policy[index].allowedForJoy = false;
            policy[index].allowedForAddiction = false; policy[index].takeToInventory = 0;
        }
        var entry = policy[scheduledDef]; entry.allowScheduled = true; entry.daysFrequency = 1f;
        entry.onlyIfMoodBelow = 1f; entry.onlyIfJoyBelow = 1f;
        string choice = any ? "1:any" : "1:limit:" + Number(limit);
        NativeDrugToleranceState.WriteEncoded(entry, choice); pawn.drugs.CurrentPolicy = policy;
        var source = NewThing(scheduledDef); EndToEndAssert.True(pawn.inventory.innerContainer.TryAdd(source));
        GenSpawn.Spawn(pawn, cell, map); pawn.drafter.Drafted = true;
        EndToEndAssert.Equal(1f, pawn.BodySize);
        return new Subject(pawn, policy, entry, choice, new Dose(source, scheduledHigh, false));
    }

    internal IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Workflow(context))
        {
            if (step is ScreenshotStep) screenshots++;
            else if (step is WaitUntilStep wait) waitSeconds += checked((int)wait.Deadline.MaxWallClock.TotalSeconds);
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep) nativeOperations++;
            if (step.Kind == EndToEndStepKind.Act)
                yield return new AssertionStep("confirm actual atomic admission before " + step.Name, _ => RequireAvailable());
            yield return step;
        }
        yield return new AssertionStep("account for the complete selected B/C sequence", _ =>
        {
            EndToEndAssert.Equal(shared ? 45 : 36, nativeOperations); EndToEndAssert.Equal(shared ? 15 : 14, screenshots);
            EndToEndAssert.Equal(shared ? 480 : 270, waitSeconds);
            EndToEndAssert.True(nativeOperations * 15 + screenshots * 30 + waitSeconds + 180 <= (shared ? 1800 : 1440));
        });
        yield return new CheckpointStep("complete B/C native operation and wait accounting", _ => Evidence());
    }
    private IEnumerable<EndToEndStep> Workflow(IEndToEndContext context)
    {
        yield return Run("let both drafted subjects obtain ordinary native wait jobs");
        yield return Wait("observe native drafted holds before the selected case starts", _ =>
        {
            AssertUnseeded(); return subjects.All(IsDraftedHold);
        }, 600, 30);
        yield return Pause("pause the native prelude before the distinct initial conditions");
        yield return new CheckpointStep("unseeded native drafted prelude", _ => Evidence());
        if (!shared) SeedNativeCap(); // Explicit one-time supporting precondition, not an observed ingestion result.
        yield return Frame("frame the two named subjects and their physical preconditions");
        foreach (var subject in subjects)
        {
            yield return Select(subject, "select starting conditions for ");
            yield return Health(subject, "show " + (shared ? "initially absent chemical tolerance for " : "seeded native maximum for "));
            yield return Shot(subject.Name + (shared ? " has no starting Psychite tolerance" : " starts at the explicitly seeded native cap"));
            yield return Gear(subject, "show the untouched scheduled dose held by ");
            yield return Shot(subject.Name + " holds its exact original " + scheduledDef.label);
        }
        foreach (var step in shared ? SharedWorkflow(context) : CapWorkflow(context)) yield return step;
        yield return new NativeDeveloperLogActionStep("open the native console after the selected B/C workflow", true);
        yield return new NativeWindowCaptureStep("retain the exact native B/C console", windows =>
        {
            RequireAvailable(); ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Missing native console.");
            EndToEndAssert.True(windows.Any(window => window.NativeId == ownedLog.ID && window.RuntimeType == ownedLog.GetType().FullName));
        });
        yield return Shot("native console after the selected tolerance cap or shared-chemical actions");
        yield return new NativeDeveloperLogActionStep("close the native console after B/C evidence", false);
        yield return new CheckpointStep("final scoped B/C evidence; remaining S138 obligations stay open", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> CapWorkflow(IEndToEndContext context)
    {
        var hundred = subjects[0]; var any = subjects[1];
        yield return new AssertionStep("arm exact native-cap comparisons with ordinary time still paused", _ =>
        {
            RequireCapPause(); foreach (var subject in subjects) { AssertRetained(subject); subject.DecisionArmed = true; }
            phase = "paused numeric100 versus Any";
        });
        foreach (var step in Undraft(context, hundred)) yield return step;
        yield return new AssertionStep("observe final numeric100 rejection at native severity1", _ =>
        {
            RequireCapPause(); AssertRetained(hundred);
            EndToEndAssert.False(hundred.Pawn.Drafted || hundred.Pawn.CurJobDef == JobDefOf.Ingest);
            AssertDecisions(hundred, 0, false, value => value.Before == 1f && value.After == 1f && value.Tick == seedTick);
        });
        yield return Gear(hundred, "show the exact Beer retained after numeric100 rejected ");
        yield return Shot("Numeric Hundred retains its Beer after an actual decision at native100%");
        yield return DraftToggle(context, hundred, false, "draft Numeric Hundred through the native gizmo while its health continues normally");
        foreach (var step in Undraft(context, any)) yield return step;
        yield return new AssertionStep("observe final Any permission and real ordinary job at the same native severity1", _ =>
        {
            RequireCapPause(); EndToEndAssert.True(hundred.Pawn.Drafted);
            AssertDecisions(any, 0, true, value => value.Before == 1f && value.After == 1f && value.Tick == seedTick);
            CaptureSelectedJob(any, any.Scheduled); phase = "Any selected at native cap";
        });
        yield return Frame("frame the native Any job selected at100% beside the retained numeric100 supply");
        yield return Shot("Any selected its ordinary Beer job at native100% while numeric100 withheld it");
        foreach (var step in Consume(any, any.Scheduled, false, true)) yield return step;
        yield return Select(hundred, "select the numeric100 subject after the Any dose ");
        yield return Health(hundred, "show native decay during the drafted hold for ");
        yield return Shot("Numeric Hundred has naturally decayed while Any consumed its selected dose");
        int prior = hundred.Decisions.Count;
        yield return new AssertionStep("require actual native decay before trying unchanged numeric100 again", _ =>
        {
            RequireAvailable(); AssertRetained(hundred);
            EndToEndAssert.True(IsDraftedHold(hundred) && hundred.Tolerance!.Severity < 1f &&
                hundred.Decays.Any(value => value.Before == 1f && value.After < 1f && value.RanOriginal));
        });
        yield return DraftToggle(context, hundred, true, "undraft the naturally decayed subject under unchanged numeric100");
        yield return new AssertionStep("observe below100 native permission and the actual selected original Beer", _ =>
        {
            RequireAvailable(); AssertDecisions(hundred, prior, true, value => value.Before < 1f && value.After < 1f);
            CaptureSelectedJob(hundred, hundred.Scheduled); phase = "unchanged numeric100 permits after native decay";
        });
        foreach (var step in Consume(hundred, hundred.Scheduled, false, true)) yield return step;
    }

    private IEnumerable<EndToEndStep> SharedWorkflow(IEndToEndContext context)
    {
        foreach (var subject in subjects)
        {
            var tea = subject.Tea!;
            yield return Select(subject, "select the drafted pawn for its native tea order ");
            EndToEndAssert.True(IsDraftedHold(subject) && tea.Source.Spawned && tea.Source.stackCount == 1);
            string expected = teaDef.ingestible.ingestCommandString.Formatted(tea.Source.LabelShort).ToString();
            var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(subject.Pawn.ThingID, tea.Source.ThingID);
            var drink = options.Where(option => !option.Disabled && string.Equals(option.Label, expected, StringComparison.Ordinal)).ToArray();
            EndToEndAssert.Equal(1, drink.Length, "Expected the enabled native tea action: " + string.Join(" | ", options.Select(option => option.Label)));
            yield return new FloatMenuActionStep("order native Drink psychite tea while drafted for " + subject.Name,
                subject.Pawn.ThingID, tea.Source.ThingID, drink[0].StableId);
            yield return new AssertionStep("retain the actual ordered tea job before native time advances", _ =>
            {
                RequireAvailable(); EndToEndAssert.True(subject.SeedWrites == 0 && Severity(subject, toleranceDef) == 0f);
                CaptureSelectedJob(subject, tea); phase = "native tea creates shared tolerance";
            });
            foreach (var step in Consume(subject, tea, true, false)) yield return step;
        }
        yield return new AssertionStep("arm scheduled Yayo decisions against only the tea-created chemical tolerance", _ =>
        {
            RequireAvailable();
            foreach (var subject in subjects)
            {
                AssertRetained(subject);
                EndToEndAssert.True(IsDraftedHold(subject) && subject.SeedWrites == 0 && subject.Tea!.Source.Destroyed &&
                    subject.Tea.IngestionCalls == 1 && subject.Tea.BeforeTolerance == 0f && subject.Tolerance!.Severity > .02f && subject.Tolerance.Severity < .04f);
                subject.DecisionArmed = true;
            }
            sharedNegativeArmed = true; sharedObservationStart = Find.TickManager.TicksGame; phase = "tea tolerance governs Yayo";
        });
        var low = subjects[0]; var high = subjects[1];
        foreach (var step in Undraft(context, low)) yield return step;
        yield return new AssertionStep("observe actual Yayo rejection against the same tea-created tolerance", _ =>
        {
            AssertSharedBlocked(); AssertDecisions(low, 0, false, value => value.Before > .02f && value.Before < .04f && value.After == value.Before);
        });
        yield return Gear(low, "show the Yayo retained after tea created shared tolerance for ");
        yield return Shot("Psychite Two retains its Yayo after real tea consumption and an actual shared-tolerance rejection");
        foreach (var step in Undraft(context, high)) yield return step;
        yield return new AssertionStep("observe real below-four-percent permission for the matched Yayo control", _ =>
        {
            RequireAvailable(); AssertSharedBlocked();
            AssertDecisions(high, 0, true, value => value.Before > .02f && value.Before < .04f && value.After == value.Before);
            CaptureSelectedJob(high, high.Scheduled);
        });
        foreach (var step in Consume(high, high.Scheduled, false, true)) yield return step;
        yield return Run("observe the retained two-percent limit through further ordinary native attempts", EndToEndGameSpeed.Superfast);
        yield return Wait("require repeated actual blocked Yayo decisions after the paired real dose", _ =>
        {
            RequireAvailable(); AssertSharedBlocked();
            return Find.TickManager.TicksGame - sharedObservationStart >= 600 &&
                low.Decisions.Where(value => value.Completed && !value.Result).Select(value => value.Tick).Distinct().Count() >= 2;
        }, 2400, 90);
        yield return Pause("pause after the bounded shared-chemical comparison");
        yield return Select(low, "select the still-blocked shared-chemical subject ");
        yield return Health(low, "show tea-created tolerance without a Yayo dose for ");
        yield return Shot("Psychite Two retains its tea-created tolerance without consuming Yayo");
        yield return Gear(low, "show the untouched original Yayo after the paired control for ");
        yield return Shot("Psychite Two still holds its exact original Yayo after genuine repeated attempts");
        yield return new AssertionStep("retain scoped same-chemical results", _ =>
        {
            RequireAvailable(); AssertSharedBlocked();
            EndToEndAssert.True(high.Scheduled.Source.Destroyed && high.Scheduled.CompletedTolerance > high.Scheduled.BeforeTolerance &&
                subjects.All(subject => subject.SeedWrites == 0));
        });
    }

    private IEnumerable<EndToEndStep> Consume(Subject subject, Dose dose, bool requireDraftedHold, bool showEmptyGear)
    {
        yield return Run("let the selected native dose reach actual chewing for " + subject.Name);
        yield return Wait("observe the same selected job carrying and chewing " + dose.Source.def.label + " for " + subject.Name, _ =>
        {
            RequireAvailable(); if (sharedNegativeArmed) AssertSharedBlocked();
            AssertSelectedJob(subject, dose); AssertDoseUnconsumed(subject, dose);
            if (subject.Pawn.jobs.curDriver is not JobDriver_Ingest driver || !InNativeIngestionToil(driver) ||
                !ReferenceEquals(subject.Pawn.carryTracker.CarriedThing, dose.Source)) return false;
            dose.ChewingTick = Find.TickManager.TicksGame; dose.ChewingToilIndex = driver.CurToilIndex;
            dose.ChewingTicksLeft = driver.ticksLeftThisToil; return true;
        }, 1800, 60);
        yield return Pause("pause actual native chewing for " + subject.Name);
        yield return Frame("frame both named subjects and the actual carried dose for " + subject.Name);
        yield return Shot(subject.Name + " performs its actual " + (dose.Manual ? "native tea order" : "ordinary scheduled dose"));
        yield return Run("finish the observed native dose for " + subject.Name);
        yield return Wait("observe physical consumption under the original job loadID for " + subject.Name, _ =>
        {
            RequireAvailable(); if (sharedNegativeArmed) AssertSharedBlocked();
            if (!dose.Source.Destroyed || !subject.Pawn.drugs.HasEverTaken(dose.Source.def)) return false;
            EndToEndAssert.True(dose.IngestionCalls == 1 && dose.IngestionSameJob && dose.IngestionNativeSource &&
                dose.IngestionJobLoadId == dose.JobLoadId && dose.IngestionTick >= dose.ChewingTick && Severity(subject, dose.HighDef) > 0);
            if (dose.Manual)
            {
                BindObservedTolerance(subject);
                EndToEndAssert.True(subject.SeedWrites == 0 && dose.BeforeTolerance == 0f && subject.Tolerance!.Severity > .02f &&
                    subject.Tolerance.Severity < .04f && !subject.Pawn.drugs.HasEverTaken(scheduledDef));
            }
            EndToEndAssert.True(Held(subject, dose.Source.def) == 0);
            if (requireDraftedHold && !IsDraftedHold(subject)) return false;
            dose.CompletedTick = Find.TickManager.TicksGame; dose.CompletedTolerance = subject.Tolerance!.Severity;
            if (dose.Manual && firstTeaCompletion < 0) firstTeaCompletion = dose.CompletedTick;
            phase = dose.Manual ? "tea actually created tolerance" : "scheduled dose actually consumed";
            return true;
        }, 1800, 60);
        yield return Pause("pause after the native physical dose for " + subject.Name);
        yield return Health(subject, "show native effects after actual ingestion for ");
        yield return Shot(subject.Name + " has native " + dose.Source.def.label + " effects after exact-source consumption");
        if (showEmptyGear)
        {
            yield return Gear(subject, "show the consumed scheduled dose is absent for ");
            yield return Shot(subject.Name + " no longer holds its original scheduled dose");
        }
        yield return new CheckpointStep("native dose provenance for " + subject.Name + " " + dose.Source.def.defName, _ => Evidence());
    }
    private IEnumerable<EndToEndStep> Undraft(IEndToEndContext context, Subject subject)
    {
        yield return Select(subject, "select the drafted subject for ordinary native selection ");
        yield return DraftToggle(context, subject, true, "undraft " + subject.Name + " through the native gizmo");
    }
    private GizmoActionStep DraftToggle(IEndToEndContext context, Subject subject, bool drafted, string name)
    {
        EndToEndAssert.Equal(drafted, subject.Pawn.Drafted);
        var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { subject.Pawn.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == drafted && option.HotKeyDefName == "Command_ColonistDraft");
        return new GizmoActionStep(name, new[] { subject.Pawn.ThingID }, toggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
    }

    private void SeedNativeCap()
    {
        AssertUnseeded(); EndToEndAssert.True(!shared && Find.TickManager.CurTimeSpeed == TimeSpeed.Paused && subjects.All(IsDraftedHold));
        foreach (var subject in subjects)
        {
            var hediff = HediffMaker.MakeHediff(toleranceDef, subject.Pawn);
            hediff.Severity = 1f; subject.SeedWrites++;
            subject.Pawn.health.AddHediff(hediff); BindObservedTolerance(subject);
        }
        seedTick = Find.TickManager.TicksGame; phase = "seeded native cap preconditions only"; RequireCapPause();
    }
    private void BindObservedTolerance(Subject subject)
    {
        var current = subject.Pawn.health.hediffSet.GetFirstHediffOfDef(toleranceDef) as HediffWithComps;
        EndToEndAssert.True(current != null && (subject.Tolerance == null || ReferenceEquals(current, subject.Tolerance)));
        subject.Tolerance = current; subject.DecayComp = current!.TryGetComp<HediffComp_SeverityPerDay>();
        EndToEndAssert.True(subject.DecayComp != null && subject.DecayComp.severityPerDay == NativeDecayRate);
    }
    private void RequireCapPause()
    {
        RequireAvailable();
        EndToEndAssert.True(!shared && Find.TickManager.CurTimeSpeed == TimeSpeed.Paused && Find.TickManager.TicksGame == seedTick &&
            subjects.All(subject => subject.SeedWrites == 1 && subject.Tolerance!.Severity == 1f && subject.Decays.Count == 0));
    }
    private void AssertUnseeded()
    {
        RequireAvailable();
        foreach (var subject in subjects)
        {
            AssertRetained(subject);
            EndToEndAssert.True(subject.Pawn.Drafted && subject.SeedWrites == 0 && subject.Tolerance == null && Severity(subject, toleranceDef) == 0f);
            if (shared) EndToEndAssert.True(subject.Tea!.Source.Spawned && subject.Tea.IngestionCalls == 0 && !subject.Pawn.drugs.HasEverTaken(teaDef));
        }
    }
    private void AssertRetained(Subject subject)
    {
        AssertDoseUnconsumed(subject, subject.Scheduled);
        EndToEndAssert.True(ReferenceEquals(subject.Scheduled.Source.holdingOwner, subject.Pawn.inventory.innerContainer));
    }
    private void AssertDoseUnconsumed(Subject subject, Dose dose)
    {
        EndToEndAssert.True(!dose.Source.Destroyed && dose.Source.stackCount == 1 && dose.IngestionCalls == 0);
        EndToEndAssert.False(subject.Pawn.drugs.HasEverTaken(dose.Source.def));
    }
    private void AssertSharedBlocked()
    {
        var low = subjects[0]; AssertRetained(low);
        EndToEndAssert.True(low.Tolerance!.Severity > .02f && low.SeedWrites == 0 && !low.Pawn.InMentalState &&
            low.Pawn.CurJobDef != JobDefOf.Ingest && Severity(low, scheduledHigh) == 0f && low.Decisions.All(value => value.Completed && !value.Result));
    }
    private void AssertDecisions(Subject subject, int start, bool expected, Func<Decision, bool> values)
    {
        var completed = subject.Decisions.Skip(start).ToArray();
        EndToEndAssert.True(completed.Length > 0 && completed.All(value => value.Completed && value.RanOriginal && value.Result == expected &&
            value.AllowedCalls == 1 && value.AllowedBefore == value.Before && value.SameTolerance && !value.HadHistory &&
            value.ExitTick == value.Tick && values(value)), "Require completed actual scheduled attempts with native entry and unchanged raw input.");
    }
    private void CaptureSelectedJob(Subject subject, Dose dose)
    {
        AssertDoseUnconsumed(subject, dose);
        var job = subject.Pawn.CurJob;
        EndToEndAssert.True(job?.def == JobDefOf.Ingest && job.playerForced == dose.Manual && ReferenceEquals(job.targetA.Thing, dose.Source) &&
            (dose.Manual || job.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy)));
        dose.Job = job; dose.JobLoadId = job!.loadID; dose.SelectedTick = Find.TickManager.TicksGame;
    }
    private void AssertSelectedJob(Subject subject, Dose dose) => EndToEndAssert.True(ReferenceEquals(subject.Pawn.CurJob, dose.Job) &&
        subject.Pawn.CurJob.loadID == dose.JobLoadId && subject.Pawn.CurJob.def == JobDefOf.Ingest &&
        subject.Pawn.CurJob.playerForced == dose.Manual && ReferenceEquals(subject.Pawn.CurJob.targetA.Thing, dose.Source));
    private static bool InNativeIngestionToil(JobDriver_Ingest driver)
    {
        // GainingNutritionNow excludes non-nutritive tea/Yayo. Read the native active/chewing
        // toil identities directly; do not invoke toil creation, init/tick callbacks or ingestion.
        var chewing = ChewingField.GetValue(driver) as Toil;
        var toils = ToilsField.GetValue(driver) as List<Toil>;
        int index = driver.CurToilIndex;
        return chewing != null && toils != null && index >= 0 && index < toils.Count &&
            ReferenceEquals(driver.job, driver.pawn.CurJob) && ReferenceEquals(toils[index], chewing);
    }
    private static bool IsDraftedHold(Subject subject) => subject.Pawn.Drafted && subject.Pawn.CurJobDef == JobDefOf.Wait_Combat &&
        subject.Pawn.jobs.IsCurrentJobPlayerInterruptible();

    private void Install(MethodInfo target, string prefix, string? postfix = null)
    {
        traced.Add(target);
        HarmonyMethod Callback(string name) => new(typeof(NativeToleranceCapAndSharedFixture).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!);
        traceOwner.Patch(target, prefix: Callback(prefix), postfix: postfix == null ? null : Callback(postfix)); RequireAvailable();
    }
    private Subject? FindSubject(Pawn pawn) => ReferenceEquals(Current.Game, game) ? subjects.FirstOrDefault(value => ReferenceEquals(value.Pawn, pawn)) : null;
    private static void BeginDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        var fixture = active; var subject = fixture?.FindSubject(__instance.pawn);
        if (fixture == null || subject == null || !subject.DecisionArmed || __0 != fixture.scheduledDef) return;
        if (subject.Decisions.Count >= 1024) { fixture.traceFault = true; return; }
        var decision = new Decision { Tick = Find.TickManager.TicksGame, Before = subject.Tolerance!.Severity,
            HadHistory = __instance.HasEverTaken(__0), SameTolerance = ReferenceEquals(subject.Tolerance,
                subject.Pawn.health.hediffSet.GetFirstHediffOfDef(fixture.toleranceDef)) };
        subject.Decisions.Add(decision); subject.Pending.Push(decision);
    }
    private static void ObserveAllowed(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        var fixture = active; var subject = fixture?.FindSubject(__instance.pawn);
        if (fixture == null || subject == null || !subject.DecisionArmed || __0 != fixture.scheduledDef || fixture.traceFault) return;
        if (subject.Pending.Count == 0) { fixture.traceFault = true; return; }
        var decision = subject.Pending.Peek(); decision.AllowedCalls++; decision.AllowedBefore = subject.Tolerance!.Severity;
    }
    private static void CompleteDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0, bool __result, bool __runOriginal)
    {
        var fixture = active; var subject = fixture?.FindSubject(__instance.pawn);
        if (fixture == null || subject == null || !subject.DecisionArmed || __0 != fixture.scheduledDef || fixture.traceFault) return;
        if (subject.Pending.Count == 0) { fixture.traceFault = true; return; }
        var decision = subject.Pending.Pop(); decision.Completed = true; decision.Result = __result; decision.RanOriginal = __runOriginal;
        decision.After = subject.Tolerance!.Severity; decision.ExitTick = Find.TickManager.TicksGame;
    }
    private static void BeginHealth(HediffWithComps __instance)
    {
        var fixture = active; var subject = fixture?.FindSubject(__instance.pawn);
        if (fixture == null || subject == null || !ReferenceEquals(subject.Tolerance, __instance)) return;
        if (subject.HealthPending) fixture.traceFault = true;
        subject.HealthPending = true; subject.HealthBefore = __instance.Severity;
    }
    private static void CompleteHealth(HediffWithComps __instance, bool __runOriginal)
    {
        var fixture = active; var subject = fixture?.FindSubject(__instance.pawn);
        if (fixture == null || subject == null || !ReferenceEquals(subject.Tolerance, __instance)) return;
        if (!subject.HealthPending || !__runOriginal || subject.Decays.Count >= 160) fixture.traceFault = true;
        if (__instance.Severity < subject.HealthBefore)
            subject.Decays.Add(new Decay(subject.HealthBefore, __instance.Severity, Find.TickManager.TicksGame, __runOriginal));
        subject.HealthPending = false;
    }
    private static void ObserveIngestion(Thing __instance, Pawn __0)
    {
        var fixture = active; var subject = fixture?.FindSubject(__0);
        if (fixture == null || subject == null) return;
        var dose = ReferenceEquals(subject.Scheduled.Source, __instance) ? subject.Scheduled :
            subject.Tea != null && ReferenceEquals(subject.Tea.Source, __instance) ? subject.Tea : null;
        if (dose == null) return;
        // Passive input/provenance only: no ref arguments, target calls, game or result mutations.
        dose.IngestionCalls++; dose.IngestionTick = Find.TickManager.TicksGame; dose.IngestionJobLoadId = __0.CurJob?.loadID ?? -1;
        dose.BeforeTolerance = fixture.Severity(subject, fixture.toleranceDef); dose.IngestionSameJob = ReferenceEquals(__0.CurJob, dose.Job);
        dose.IngestionNativeSource = __0.CurJob?.def == JobDefOf.Ingest && __0.CurJob.playerForced == dose.Manual &&
            ReferenceEquals(__0.CurJob.targetA.Thing, __instance) && (dose.Manual || __0.CurJob.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy));
    }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && ReferenceEquals(Find.CurrentMap, map) &&
        Current.ProgramState == ProgramState.Playing, "Refuse an unrelated Game/map during B/C work or cleanup.");
    private void RequireAvailable()
    {
        RequireGame();
        EndToEndAssert.True(ReferenceEquals(RuntimeField.GetValue(mod), runtime) && runtime.Available,
            "The actual atomic tolerance enforcement/editor group must remain available: " + runtime.StatusKey);
        EndToEndAssert.False(traceFault);
        foreach (var target in traced)
        {
            var info = HarmonyOwner.GetPatchInfo(target);
            EndToEndAssert.True(info != null && info.Prefixes.Count(patch => patch.owner == traceOwner.Id) == 1 &&
                info.Postfixes.Count(patch => patch.owner == traceOwner.Id) == (target == ShouldTarget || target == HealthTarget ? 1 : 0));
            if (target == AllowedTarget)
                EndToEndAssert.True(info!.Prefixes.Any(patch => patch.owner == "fumblesneeze.megamod") && info.Finalizers.Any(patch => patch.owner == "fumblesneeze.megamod"));
            if (target == ShouldTarget)
                EndToEndAssert.True(info!.Postfixes.Count == 1 && info.Finalizers.Count == 0,
                    "The observed ShouldTry result must not be replaced by a later outer callback.");
        }
        foreach (var subject in subjects)
        {
            EndToEndAssert.True(subject.Pending.Count == 0 && !subject.HealthPending && !subject.Pawn.Dead);
            // Preserve genuine post-dose effects, including native Yayo overdose. Eligibility
            // preconditions must hold until that exact scheduled dose has actually been consumed.
            if (!subject.Scheduled.Source.Destroyed || subject.Scheduled.IngestionCalls != 1)
                EndToEndAssert.False(subject.Pawn.Downed || subject.Pawn.InMentalState);
            EndToEndAssert.True(ReferenceEquals(subject.Pawn.drugs.CurrentPolicy, subject.Policy) && game.drugPolicyDatabase.AllPolicies.Contains(subject.Policy) &&
                ReferenceEquals(subject.Policy[scheduledDef], subject.Entry) && NativeDrugToleranceState.ReadEncoded(subject.Entry) == subject.Choice &&
                subject.Entry.allowScheduled && !subject.Entry.allowedForJoy && !subject.Entry.allowedForAddiction && subject.Entry.takeToInventory == 0 &&
                subject.Entry.daysFrequency == 1f && subject.Entry.onlyIfMoodBelow == 1f && subject.Entry.onlyIfJoyBelow == 1f);
            if (shared)
            {
                var teaEntry = subject.Policy[teaDef];
                EndToEndAssert.True(subject.SeedWrites == 0 && !teaEntry.allowScheduled && !teaEntry.allowedForJoy && !teaEntry.allowedForAddiction && teaEntry.takeToInventory == 0);
            }
            if (subject.Tolerance != null)
                EndToEndAssert.True(ReferenceEquals(subject.Tolerance, subject.Pawn.health.hediffSet.GetFirstHediffOfDef(toleranceDef)) &&
                    ReferenceEquals(subject.DecayComp, subject.Tolerance.TryGetComp<HediffComp_SeverityPerDay>()) && subject.DecayComp!.severityPerDay == NativeDecayRate &&
                    subject.Tolerance.Severity <= 1f && (shared || subject.SeedWrites == 1));
        }
        if (shared && firstTeaCompletion >= 0)
            EndToEndAssert.True(Find.TickManager.TicksGame - firstTeaCompletion <= 12000, "The post-tea comparison must remain inside its bounded native window.");
    }
    private IReadOnlyDictionary<string, string> Evidence()
    {
        RequireAvailable();
        var result = new Dictionary<string, string>
        {
            ["scope"] = shared ? "Actual Tea to Yayo shared Psychite tolerance; no Flake/medical/safety/full-S138 claim" :
                "Seeded Core cap1: numeric100 versus Any selection, then native decay; no >1/natural-accumulation/full-S138 claim",
            ["phase"] = phase, ["tick"] = Find.TickManager.TicksGame.ToString(), ["seedTick"] = seedTick.ToString(),
            ["firstTeaCompletion"] = firstTeaCompletion.ToString(), ["available"] = runtime.Available.ToString(), ["traceOwner"] = traceOwner.Id,
            ["scheduledDrug"] = scheduledDef.defName, ["sharedToleranceDef"] = toleranceDef.defName,
            ["nativeOperations"] = nativeOperations.ToString(), ["screenshots"] = screenshots.ToString(), ["maximumWaitSeconds"] = waitSeconds.ToString(),
            ["countedFixedAllowanceSeconds"] = (nativeOperations * 15 + screenshots * 30 + waitSeconds).ToString()
        };
        foreach (var subject in subjects)
        {
            result[subject.Name] = "pawn=" + subject.Pawn.ThingID + ";policy=" + subject.Policy.id + ";choice=" + subject.Choice +
                ";seedWrites=" + subject.SeedWrites + ";tolerance=" + Number(Severity(subject, toleranceDef)) +
                ";scheduledHigh=" + Number(Severity(subject, scheduledHigh)) + ";heldScheduled=" + Held(subject, scheduledDef) +
                ";scheduledHistory=" + subject.Pawn.drugs.HasEverTaken(scheduledDef) + ";downed=" + subject.Pawn.Downed +
                ";overdose=" + Number(Severity(subject, HediffDefOf.DrugOverdose)) + ";job=" + subject.Pawn.CurJob;
            result[subject.Name + " scheduledDose"] = subject.Scheduled.ToString();
            if (subject.Tea != null) result[subject.Name + " teaDose"] = subject.Tea.ToString();
            result[subject.Name + " decisions"] = string.Join("\n", subject.Decisions.Select((value, index) => index + ":" + value));
            result[subject.Name + " decays"] = string.Join("\n", subject.Decays.Select((value, index) => index + ":" + value));
        }
        foreach (var target in traced)
        {
            var info = HarmonyOwner.GetPatchInfo(target)!;
            result[target.DeclaringType!.Name + "." + target.Name] = string.Join("; ", info.Prefixes.Select(patch => "prefix:" + patch.owner + ":" + patch.PatchMethod.Name)
                .Concat(info.Postfixes.Select(patch => "postfix:" + patch.owner + ":" + patch.PatchMethod.Name))
                .Concat(info.Transpilers.Select(patch => "transpiler:" + patch.owner + ":" + patch.PatchMethod.Name))
                .Concat(info.Finalizers.Select(patch => "finalizer:" + patch.owner + ":" + patch.PatchMethod.Name)));
        }
        return result;
    }
    private float Severity(Subject subject, HediffDef def) => subject.Pawn.health.hediffSet.GetFirstHediffOfDef(def)?.Severity ?? 0f;
    private static int Held(Subject subject, ThingDef def) => subject.Pawn.inventory.innerContainer.TotalStackCountOfDef(def) +
        (subject.Pawn.carryTracker.CarriedThing?.def == def ? subject.Pawn.carryTracker.CarriedThing.stackCount : 0);
    private CameraActionStep Frame(string name) => new(name, subjects.Select(subject => subject.Pawn.ThingID)
        .Concat(subjects.Where(subject => subject.Tea?.Source.Spawned == true).Select(subject => subject.Tea!.Source.ThingID)), paddingPixels: 200);
    private static SelectionActionStep Select(Subject subject, string prefix) => new(prefix + subject.Name, new[] { subject.Pawn.ThingID }, additive: false);
    private static PawnInspectTabActionStep Health(Subject subject, string prefix) => new(prefix + subject.Name, subject.Pawn.ThingID, EndToEndPawnInspectTab.Health);
    private static PawnInspectTabActionStep Gear(Subject subject, string prefix) => new(prefix + subject.Name, subject.Pawn.ThingID, EndToEndPawnInspectTab.Gear);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static TimeControlActionStep Pause(string name) => new(name, true, EndToEndGameSpeed.Normal);
    private static TimeControlActionStep Run(string name, EndToEndGameSpeed speed = EndToEndGameSpeed.Normal) => new(name, false, speed);
    private static WaitUntilStep Wait(string name, Func<IEndToEndContext, bool> predicate, int ticks, int seconds) =>
        new(name, predicate, new EndToEndDeadline(int.MaxValue, ticks, TimeSpan.FromSeconds(seconds)));
    private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed class Subject
    {
        internal readonly Pawn Pawn;
        internal readonly DrugPolicy Policy;
        internal readonly DrugPolicyEntry Entry;
        internal readonly string Choice;
        internal readonly Dose Scheduled;
        internal readonly List<Decision> Decisions = new();
        internal readonly Stack<Decision> Pending = new();
        internal readonly List<Decay> Decays = new();
        internal Dose? Tea;
        internal HediffWithComps? Tolerance;
        internal HediffComp_SeverityPerDay? DecayComp;
        internal bool DecisionArmed, HealthPending;
        internal float HealthBefore;
        internal int SeedWrites;
        internal string Name => Pawn.Name.ToStringShort;
        internal Subject(Pawn pawn, DrugPolicy policy, DrugPolicyEntry entry, string choice, Dose scheduled)
        { Pawn = pawn; Policy = policy; Entry = entry; Choice = choice; Scheduled = scheduled; }
    }
    private sealed class Dose
    {
        internal readonly Thing Source;
        internal readonly HediffDef HighDef;
        internal readonly bool Manual;
        internal Job? Job;
        internal int JobLoadId = -1, SelectedTick = -1, ChewingTick = -1, CompletedTick = -1;
        internal int ChewingToilIndex = -1, ChewingTicksLeft;
        internal int IngestionCalls, IngestionJobLoadId = -1, IngestionTick = -1;
        internal bool IngestionSameJob, IngestionNativeSource;
        internal float BeforeTolerance, CompletedTolerance;
        internal Dose(Thing source, HediffDef highDef, bool manual) { Source = source; HighDef = highDef; Manual = manual; }
        public override string ToString() => "source=" + Source.ThingID + ":destroyed=" + Source.Destroyed + ":manual=" + Manual +
            ":selectedJob=" + JobLoadId + ":selectedTick=" + SelectedTick + ":chewing=" + ChewingTick + ":chewingToil=" + ChewingToilIndex +
            ":chewingTicksLeft=" + ChewingTicksLeft + ":completed=" + CompletedTick +
            ":ingestionCalls=" + IngestionCalls + ":ingestionJob=" + IngestionJobLoadId + ":ingestionTick=" + IngestionTick +
            ":sameJob=" + IngestionSameJob + ":nativeSource=" + IngestionNativeSource + ":beforeTolerance=" + Number(BeforeTolerance) +
            ":completedTolerance=" + Number(CompletedTolerance);
    }
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
        internal Decay(float before, float after, int tick, bool ranOriginal) { Before = before; After = after; Tick = tick; RanOriginal = ranOriginal; }
        public override string ToString() => "tick=" + Tick + ":before=" + Number(Before) + ":after=" + Number(After) + ":original=" + RanOriginal;
    }
}
