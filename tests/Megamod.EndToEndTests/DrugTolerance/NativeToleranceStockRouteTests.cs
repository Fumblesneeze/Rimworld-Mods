using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using Megamod.EndToEndTests.DrugPolicyMaintenance;
using Megamod.Features.DrugTolerance;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;
using Verse.AI;
using HarmonyOwner = HarmonyLib.Harmony;

namespace Megamod.EndToEndTests.DrugTolerance;

// Native scheduled-dose pickup and independent refill; root owns final live acceptance.
[RimWorldEndToEndTest("megamod.drug-tolerance.native-stock-routes", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 12000, MaxWallClockSeconds = 1800)]
public sealed class NativeToleranceStockRouteTests : IRimWorldEndToEndTest
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private static readonly FieldInfo RuntimeField = Field(typeof(MegamodMod), "drugTolerance");
    private static readonly FieldInfo BindingField = Field(typeof(SettingsRuntime), "binding");
    private static readonly FieldInfo PolicyField = Field(typeof(Pawn_DrugPolicyTracker), "curPolicy");
    private static readonly FieldInfo HistoryField = Field(typeof(Pawn_DrugPolicyTracker), "drugTakeRecords");
    private static readonly FieldInfo TimesTakenField = Field(typeof(DrugTakeRecord), "timesTakenThisDayInt");
    private static readonly FieldInfo ChewingField = Field(typeof(JobDriver_Ingest), "chewing");
    private static readonly FieldInfo ToilsField = Field(typeof(JobDriver), "toils");
    private static readonly FieldInfo ScrollField = Field(typeof(Dialog_ManageDrugPolicies), "scrollPosition");
    private static readonly MethodInfo ShouldTarget = Method(typeof(Pawn_DrugPolicyTracker), nameof(Pawn_DrugPolicyTracker.ShouldTryToTakeScheduledNow), typeof(ThingDef));
    private static readonly MethodInfo AllowedTarget = Method(typeof(Pawn_DrugPolicyTracker), nameof(Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow), typeof(ThingDef));
    private static readonly MethodInfo SplitTarget = Method(typeof(ThingWithComps), nameof(Thing.SplitOff), typeof(int));
    private static readonly MethodInfo IngestedTarget = Method(typeof(Thing), nameof(Thing.Ingested), typeof(Pawn), typeof(float));
    private static NativeToleranceStockRouteTests? active;

    private readonly string marker = "ToleranceStock-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private readonly NativeDrugPolicyView view = new();
    private readonly List<Pawn> ownedPawns = new();
    private readonly List<DrugPolicy> ownedPolicies = new();
    private readonly HashSet<Thing> ownedBeer = new();
    private readonly List<Window> ownedWindows = new();
    private readonly List<MethodInfo> traced = new();
    private readonly List<Decision> decisions = new();
    private readonly Stack<Decision> pending = new();
    private readonly List<Split> splits = new();
    private readonly List<Ingestion> ingestions = new();
    private Game game = null!;
    private Map map = null!;
    private MegamodMod mod = null!;
    private NativeDrugToleranceRuntime runtime = null!;
    private SettingsRuntime settings = null!;
    private SaveSettingsBinding binding = null!;
    private SaveSettingsSnapshot initialSave = null!;
    private GlobalSettingsSnapshot initialGlobals = null!;
    private HarmonyOwner traceOwner = null!;
    private ThingDef beerDef = null!;
    private HediffDef highDef = null!, toleranceDef = null!;
    private Subject a = null!, b = null!;
    private Thing ground = null!;
    private IntVec3 groundCell;
    private JobSnapshot? attachedJob, refillJob;
    private Thing? dose, attachedStock, independentStock;
    private int chewingTick = -1, doseCompletedTick = -1, refillSelectedTick = -1;
    private int phase, nativeOperations, screenshots, waitSeconds;
    private bool anyApplied;
    private string? traceFailure;
    private EndToEndNativeWindow window = null!;
    private MainTabWindow_Assign assign = null!;
    private Dialog_ManageDrugPolicies manager = null!;
    private Dialog_DrugTolerance editor = null!;
    private string pointer = null!;

    public void Arrange(IEndToEndContext context)
    {
        game = Current.Game; map = Find.CurrentMap; RequireGame();
        EndToEndAssert.True(active == null && game.PlayerHasControl && Prefs.DevMode && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && Find.Selector.SelectedObjects.Count == 0 &&
            !Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>() && !Find.WindowStack.IsOpen<Dialog_DrugTolerance>() &&
            !Find.WindowStack.IsOpen<EditWindow_Log>() && map.mapPawns.AllPawnsSpawned.Count == 0,
            "This scenario requires the runner's empty disposable map, with no competing actors.");
        EndToEndAssert.True(BindingField.FieldType == typeof(SaveSettingsBinding) && PolicyField.FieldType == typeof(DrugPolicy) &&
            HistoryField.FieldType == typeof(List<DrugTakeRecord>) && TimesTakenField.FieldType == typeof(int) &&
            ChewingField.FieldType == typeof(Toil) && ToilsField.FieldType == typeof(List<Toil>) && ScrollField.FieldType == typeof(Vector2));
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings unavailable.");
        binding = BindingField.GetValue(settings) as SaveSettingsBinding ?? throw new InvalidOperationException("Native lifecycle has not published a save binding.");
        initialGlobals = settings.Store.ExportGlobals(); initialSave = settings.Store.ExportSave(binding);
        mod = LoadedModManager.GetMod<MegamodMod>();
        runtime = RuntimeField.GetValue(mod) as NativeDrugToleranceRuntime ?? throw new InvalidOperationException("Tolerance runtime unavailable.");
        EndToEndAssert.True(settings.SaveLifecycleAvailable && !binding.IsRevoked && settings.Store.GetEffective(DrugToleranceSettings.Enabled) && runtime.Available);

        var oldSpeed = Find.TickManager.CurTimeSpeed;
        // The runner restores camera/preferences and clears the initially empty selection. This callback independently
        // restores the fixture's speed even if its later object cleanup reports a failure.
        context.DeferCleanup(() => { RequireGame(); Find.TickManager.CurTimeSpeed = oldSpeed; });
        context.DeferCleanup(CleanupObjects);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        beerDef = DefDatabase<ThingDef>.GetNamed("Beer"); highDef = DefDatabase<HediffDef>.GetNamed("AlcoholHigh");
        toleranceDef = DefDatabase<HediffDef>.GetNamed("AlcoholTolerance");
        EndToEndAssert.True(beerDef.thingClass == typeof(ThingWithComps) && beerDef.IsDrug && beerDef.IsAddictiveDrug &&
            beerDef.ingestible.defaultNumToIngestAtOnce == 1 && beerDef.stackLimit >= 6 &&
            beerDef.ingestible.outcomeDoers.OfType<IngestionOutcomeDoer_GiveHediff>().Any(value =>
                ReferenceEquals(value.hediffDef, toleranceDef) && value.toleranceChemical?.toleranceHediff == toleranceDef));
        EndToEndAssert.Equal(0, map.listerThings.ThingsOfDef(beerDef).Count);
        var offsets = new[] { new IntVec3(-8, 0, 0), new IntVec3(8, 0, 0), IntVec3.Zero };
        groundCell = CellRect.CenteredOn(map.Center, 20).OrderBy(cell => cell.DistanceToSquared(map.Center)).First(cell =>
            Enumerable.Range(-8, 17).All(x => (cell + new IntVec3(x, 0, 0)).InBounds(map) &&
                (cell + new IntVec3(x, 0, 0)).Standable(map) && !(cell + new IntVec3(x, 0, 0)).Fogged(map)) &&
            offsets.All(offset => (cell + offset).GetThingList(map).Count == 0));
        a = CreateSubject("A Scheduled", groundCell + offsets[0], scheduled: true);
        b = CreateSubject("B Independent", groundCell + offsets[1], scheduled: false);
        ground = ThingMaker.MakeThing(beerDef); ground.stackCount = 6; ownedBeer.Add(ground);
        GenSpawn.Spawn(ground, groundCell, map); ground.SetForbidden(false, false);
        EndToEndAssert.True(a.Pawn.CanReach(ground, PathEndMode.ClosestTouch, Danger.Some) &&
            b.Pawn.CanReach(ground, PathEndMode.ClosestTouch, Danger.Some));
        traceOwner = new HarmonyOwner("fumblesneeze.tests.s138.stock." + marker); active = this;
        context.DeferCleanup(CleanupTrace);
        Install(ShouldTarget, nameof(BeginDecision), nameof(CompleteDecision));
        Install(AllowedTarget, nameof(ObserveAllowed));
        Install(SplitTarget, nameof(BeginSplit), nameof(CompleteSplit));
        Install(IngestedTarget, nameof(BeginIngestion), nameof(CompleteIngestion));
        RequireCurrent(); AssertUntouched();
    }

    private Subject CreateSubject(string label, IntVec3 cell, bool scheduled)
    {
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowFood: false, allowAddictions: false,
            fixedBiologicalAge: 25, fixedChronologicalAge: 25, forceNoIdeo: true, forceNoGear: true));
        ownedPawns.Add(pawn); pawn.Name = new NameTriple("Tolerance", label, marker);
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray()) pawn.health.RemoveHediff(hediff);
        foreach (var trait in pawn.story.traits.allTraits.ToArray()) pawn.story.traits.RemoveTrait(trait);
        pawn.inventory.innerContainer.ClearAndDestroyContents(); pawn.workSettings.EnableAndInitialize();
        foreach (var work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (!pawn.WorkTypeIsDisabled(work)) pawn.workSettings.SetPriority(work, 0);
        for (int hour = 0; hour < 24; hour++) pawn.timetable.SetAssignment(hour, TimeAssignmentDefOf.Anything);
        pawn.needs.food.CurLevelPercentage = 1f; pawn.needs.rest.CurLevelPercentage = 1f;
        pawn.needs.mood.CurLevelPercentage = 1f; pawn.needs.joy.CurLevelPercentage = 1f;
        var policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); ownedPolicies.Add(policy); policy.label = marker + " " + label;
        for (int i = 0; i < policy.Count; i++)
        {
            policy[i].allowScheduled = false; policy[i].allowedForJoy = false;
            policy[i].allowedForAddiction = false; policy[i].takeToInventory = 0;
        }
        var entry = policy[beerDef]; entry.allowScheduled = scheduled; entry.allowedForJoy = !scheduled;
        entry.daysFrequency = 1f; entry.onlyIfMoodBelow = 1f; entry.onlyIfJoyBelow = 1f; entry.takeToInventory = 2;
        NativeDrugToleranceState.WriteEncoded(entry, "1:limit:0"); // Disposable starting policy only.
        pawn.drugs.CurrentPolicy = policy; GenSpawn.Spawn(pawn, cell, map); pawn.drafter.Drafted = true;
        EndToEndAssert.Equal(0, History(pawn).Count);
        return new Subject(pawn, policy, entry, scheduled);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Workflow(context))
        {
            if (step is ScreenshotStep) screenshots++;
            else if (step is WaitUntilStep wait) waitSeconds += checked((int)wait.Deadline.MaxWallClock.TotalSeconds);
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep) nativeOperations++;
            if (step.Kind == EndToEndStepKind.Act)
                yield return new AssertionStep("validate current identities before " + step.Name, _ => RequireCurrent());
            yield return step;
        }
        yield return new AssertionStep("verify exact stock-route action and deadline accounting", _ =>
        {
            EndToEndAssert.Equal(51, nativeOperations); EndToEndAssert.Equal(14, screenshots); EndToEndAssert.Equal(335, waitSeconds);
            EndToEndAssert.True(nativeOperations * 15 + screenshots * 30 + waitSeconds + 180 == 1700);
        });
        yield return new CheckpointStep("final native stock evidence; no broader S138 acceptance", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> Workflow(IEndToEndContext context)
    {
        yield return Frame("frame both drafted stock subjects and the sole six-Beer supply");
        yield return Select(a); yield return Gear(a); yield return Shot("A starts empty with numeric zero and the ground supply untouched");
        yield return Select(b); yield return Gear(b); yield return Shot("B starts empty before its independent refill workflow");
        yield return SelectGround(); yield return Shot("the sole original ground Beer stack contains six units");
        foreach (var step in SetDraft(context, a, drafted: false)) yield return step;
        int blockedAt = Find.TickManager.TicksGame;
        yield return Run("allow genuine scheduled attempts under A's numeric zero", EndToEndGameSpeed.Superfast);
        yield return Wait("observe repeated actual final rejections without any pickup", _ =>
        {
            RequireCurrent(); AssertUntouched(); AssertZeroDecisions();
            return Find.TickManager.TicksGame - blockedAt >= 600 && decisions.Select(value => value.Tick).Distinct().Count() >= 2;
        }, 2400, 90);
        yield return Pause("pause the rejected scheduled pawn and untouched supply");
        yield return Gear(a); yield return Shot("A remains empty after real zero-limit refusals; all six Beer remain on ground");
        yield return new CheckpointStep("withheld scheduled dose and attached pickup", _ => Evidence());
        foreach (var step in ApplyAny()) yield return step;

        yield return Run("let native scheduled selection obtain the dose and its extra stock", EndToEndGameSpeed.Normal);
        yield return Wait("observe A actually chewing after the two attached extra units were collected", _ =>
        {
            RequireCurrent(); AssertBEmpty(); AssertConservation(0);
            if (a.Pawn.CurJobDef != JobDefOf.Ingest) return false;
            var job = a.Pawn.CurJob;
            AssertJob(job, JobDefOf.Ingest, typeof(JobGiver_TakeDrugsForDrugPolicy), 1);
            EndToEndAssert.Equal(2, job.takeExtraIngestibles);
            if (splits.Count < 2 || !IsChewing(a.Pawn)) return false;
            attachedJob = new JobSnapshot(a.Pawn, job); dose = splits[0].Result; attachedStock = splits[1].Result; chewingTick = Find.TickManager.TicksGame;
            AssertAttachedSplits(); AssertHeld(a, attachedStock, 2); AssertGround(3);
            EndToEndAssert.True(ReferenceEquals(job.targetA.Thing, dose) && ReferenceEquals(a.Pawn.carryTracker.CarriedThing, dose) &&
                !dose!.Destroyed && dose.stackCount == 1 && ingestions.Count == 0 && History(a.Pawn).Count == 0);
            EndToEndAssert.True(decisions.Any(value => value.Choice == "1:any" && value.Completed && value.Result && value.RanOriginal && value.AllowedCalls == 1));
            return true;
        }, 1800, 60);
        yield return Pause("pause A's observed chewing job after attached pickup");
        yield return Frame("frame A's actual drinking job and remaining ground supply");
        yield return Select(a); yield return Gear(a); yield return Shot("A is drinking its selected dose while carrying two extra Beer; three remain on ground");
        yield return new CheckpointStep("actual job and split-source provenance before consumption", _ => Evidence());
        yield return Run("finish A's already selected ordinary dose", EndToEndGameSpeed.Normal);
        yield return Wait("observe one actual consumed Beer and native Alcohol effects", _ =>
        {
            RequireCurrent(); AssertBEmpty();
            if (dose == null || !dose.Destroyed || History(a.Pawn).Count == 0) return false;
            AssertADoseCompleted(); doseCompletedTick = Find.TickManager.TicksGame; return true;
        }, 1800, 60);
        yield return Pause("pause after A's one completed native dose");
        yield return Health(a); yield return Shot("A has native Alcohol and tolerance effects from exactly one consumed dose");
        yield return SelectGround(); yield return Shot("three units remain in the original ground stack after A carried two and consumed one");
        foreach (var step in SetDraft(context, a, drafted: true)) yield return step;
        yield return new AssertionStep("isolate A before releasing independent refill B", _ =>
        { RequireCurrent(); AssertADoseCompleted(); AssertBEmpty(); EndToEndAssert.True(a.Pawn.Drafted && b.Pawn.Drafted); phase = 2; });
        foreach (var step in SetDraft(context, b, drafted: false)) yield return step;
        yield return Run("allow B's ordinary independent inventory refill selection", EndToEndGameSpeed.Normal);
        yield return Wait("observe the autonomous TakeInventory job before B reaches its source", _ =>
        {
            RequireCurrent(); AssertADoseCompleted(); AssertBEmpty(); AssertGround(3);
            if (b.Pawn.CurJobDef != JobDefOf.TakeInventory) return false;
            var job = b.Pawn.CurJob;
            AssertJob(job, JobDefOf.TakeInventory, typeof(JobGiver_MoveDrugsToInventory), 2);
            EndToEndAssert.True(ReferenceEquals(job.targetA.Thing, ground) && b.Pawn.jobs.curDriver is JobDriver_TakeInventory &&
                b.Pawn.Position.DistanceToSquared(ground.Position) > 2, "Capture B's real refill selection while it still has a physical walk to perform.");
            refillJob = new JobSnapshot(b.Pawn, job);
            refillSelectedTick = Find.TickManager.TicksGame; return true;
        }, 1800, 60);
        yield return Pause("pause B's selected independent refill before physical pickup");
        yield return Frame("frame B's real stock collection path");
        yield return Shot("B is taking Beer to inventory through its autonomous native job, with no ingestion order");
        yield return Run("allow B to finish its selected independent refill", EndToEndGameSpeed.Normal);
        yield return Wait("observe B's two carried units without consumption or Health changes", _ =>
        {
            RequireCurrent(); AssertADoseCompleted(); AssertNoIngestion(b);
            if (b.Pawn.inventory.innerContainer.TotalStackCountOfDef(beerDef) != 2) return false;
            EndToEndAssert.Equal(3, splits.Count); independentStock = splits[2].Result;
            AssertIndependentSplit(); AssertHeld(b, independentStock, 2); AssertGround(1); AssertConservation(1); return true;
        }, 1800, 60);
        yield return Pause("pause B with its completed independent refill");
        yield return Gear(b); yield return Shot("B carries two Beer despite its retained numeric-zero tolerance choice");
        yield return Health(b); yield return Shot("B has no Alcohol or tolerance effects and unchanged ingestion history after carrying supply");
        yield return SelectGround(); yield return Shot("one unit remains on ground: A carries two, B carries two, and exactly one was consumed");
        yield return new CheckpointStep("native zero-to-Any attached pickup and independent refill result", _ => Evidence());
        yield return new NativeDeveloperLogActionStep("open the native console after stock workflows", true);
        yield return Capture<EditWindow_Log>("retain the exact native console", _ => { });
        yield return Shot("native console after both autonomous stock routes");
        yield return new NativeDeveloperLogActionStep("close the native console after stock workflows", false);
    }

    private IEnumerable<EndToEndStep> ApplyAny()
    {
        yield return new NativeMainTabActionStep("open Assign for A's native policy edit", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture native manager entry", current => assign = current);
        yield return new NativePointerScopeBeginStep("own pointer for the native Any edit", handle => pointer = handle);
        yield return Click("open the native drug-policy manager", view.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture the current native policy list", current => manager = current);
        yield return Click("select A's exact scheduled policy", view.PolicyRow(manager, a.Policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture A's selected zero row", current => manager = current);
        yield return Shot("A's scheduled Beer row shows numeric zero before the native edit");
        yield return Click("open A's contextual Beer tolerance editor", RowPoint());
        yield return Capture<Dialog_DrugTolerance>("capture the native contextual draft", current => editor = current);
        yield return Click("select Any in A's native contextual draft", AnyPoint());
        yield return Capture<Dialog_DrugTolerance>("recapture the checked Any draft before Apply", current => editor = current);
        yield return new AssertionStep("Any remains an unapplied draft", _ =>
        { EndToEndAssert.True(editor.Any); EndToEndAssert.Equal("1:limit:0", NativeDrugToleranceState.ReadEncoded(a.Entry)); });
        yield return Click("apply Any through the native button", new Vector2(18 + (editor.windowRect.width - 36) * .75f + 2.5f, editor.windowRect.height - 35.5f));
        yield return new AssertionStep("observe the native Any commit without changing B's zero", _ =>
        {
            EndToEndAssert.Equal("1:any", NativeDrugToleranceState.ReadEncoded(a.Entry));
            EndToEndAssert.Equal("1:limit:0", NativeDrugToleranceState.ReadEncoded(b.Entry));
            anyApplied = true; phase = 1; RequireCurrent(); AssertUntouched();
        });
        yield return Wait("wait for the native Apply to close its exact editor", _ => !editor.IsOpen && !Find.WindowStack.Windows.Contains(editor), 120, 5);
        yield return Capture<Dialog_ManageDrugPolicies>("capture A's newly applied Any row", current => manager = current);
        yield return Shot("native Apply changes A to Any while scheduling and stock target two remain intact");
        yield return new WindowCancelActionStep("close the native policy manager", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore the native editor pointer", pointer);
        yield return new NativeMainTabActionStep("close Assign after the native Any edit", "Assign", false);
    }

    private IEnumerable<EndToEndStep> SetDraft(IEndToEndContext context, Subject subject, bool drafted)
    {
        yield return Select(subject);
        EndToEndAssert.Equal(!drafted, subject.Pawn.Drafted);
        var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { subject.Pawn.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == !drafted && option.HotKeyDefName == "Command_ColonistDraft");
        yield return new GizmoActionStep((drafted ? "draft " : "undraft ") + subject.Pawn.Name.ToStringShort + " through its native gizmo",
            new[] { subject.Pawn.ThingID }, toggle.RuntimeType, EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
        yield return new AssertionStep("observe native draft state", _ => EndToEndAssert.Equal(drafted, subject.Pawn.Drafted));
    }

    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && ReferenceEquals(Find.CurrentMap, map) &&
        Current.ProgramState == ProgramState.Playing, "Refuse stale Game/map references.");
    private void RequireCurrent()
    {
        RequireGame(); EndToEndAssert.True(traceFailure == null, traceFailure ?? string.Empty);
        EndToEndAssert.True(ReferenceEquals(RuntimeField.GetValue(mod), runtime) && runtime.Available &&
            ReferenceEquals(BindingField.GetValue(settings), binding) && !binding.IsRevoked && settings.SaveLifecycleAvailable &&
            settings.Store.GetEffective(DrugToleranceSettings.Enabled), "Current atomic enforcement must remain available: " + runtime.StatusKey);
        EndToEndAssert.True(initialGlobals.Values.OrderBy(pair => pair.Key).SequenceEqual(settings.Store.ExportGlobals().Values.OrderBy(pair => pair.Key)) &&
            initialSave.Values.OrderBy(pair => pair.Key).SequenceEqual(settings.Store.ExportSave(binding).Values.OrderBy(pair => pair.Key)),
            "This workflow does not edit global or save configuration.");
        EndToEndAssert.True(pending.Count == 0 && map.mapPawns.AllPawnsSpawned.Count == 2 &&
            map.mapPawns.AllPawnsSpawned.All(pawn => ReferenceEquals(pawn, a.Pawn) || ReferenceEquals(pawn, b.Pawn)),
            "Only the two controlled actors may compete for the supply.");
        foreach (var subject in new[] { a, b })
        {
            var pawn = subject.Pawn; var entry = subject.Entry;
            EndToEndAssert.True(pawn.Spawned && ReferenceEquals(pawn.Map, map) && !pawn.Dead && !pawn.Downed && !pawn.InMentalState &&
                pawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) == 0 &&
                ReferenceEquals(PolicyField.GetValue(pawn.drugs), subject.Policy) && game.drugPolicyDatabase.AllPolicies.Contains(subject.Policy) &&
                ReferenceEquals(subject.Policy[beerDef], entry) && entry.allowScheduled == subject.Scheduled && entry.allowedForJoy == !subject.Scheduled &&
                !entry.allowedForAddiction && entry.takeToInventory == 2 && entry.daysFrequency == 1f &&
                entry.onlyIfMoodBelow == 1f && entry.onlyIfJoyBelow == 1f &&
                NativeDrugToleranceState.ReadEncoded(entry) == (ReferenceEquals(subject, a) && anyApplied ? "1:any" : "1:limit:0"));
        }
        foreach (var target in traced)
        {
            var info = HarmonyOwner.GetPatchInfo(target);
            EndToEndAssert.True(info != null && info.Prefixes.Count(patch => patch.owner == traceOwner.Id) == 1 &&
                info.Postfixes.Count(patch => patch.owner == traceOwner.Id) == (target == AllowedTarget ? 0 : 1));
            if (target == AllowedTarget) EndToEndAssert.True(info!.Prefixes.Any(patch => patch.owner == "fumblesneeze.megamod") &&
                info.Finalizers.Any(patch => patch.owner == "fumblesneeze.megamod"));
            if (target == ShouldTarget) EndToEndAssert.True(info!.Postfixes.Count == 1 && info.Finalizers.Count == 0,
                "The observer must retain the actual final scheduled result.");
        }
    }
    private void AssertUntouched()
    {
        AssertGround(6); AssertNoIngestion(a); AssertBEmpty();
        EndToEndAssert.Equal(0, a.Pawn.inventory.innerContainer.Count);
        EndToEndAssert.True(a.Pawn.carryTracker.CarriedThing == null && a.Pawn.CurJobDef != JobDefOf.Ingest &&
            a.Pawn.CurJobDef != JobDefOf.TakeInventory && splits.Count == 0);
        AssertConservation(0);
    }
    private void AssertBEmpty()
    {
        AssertNoIngestion(b); EndToEndAssert.Equal(0, b.Pawn.inventory.innerContainer.Count);
        EndToEndAssert.True(b.Pawn.carryTracker.CarriedThing == null && (phase == 2 || b.Pawn.Drafted));
    }
    private void AssertNoIngestion(Subject subject)
    {
        EndToEndAssert.Equal(0, History(subject.Pawn).Count);
        EndToEndAssert.True(ingestions.All(value => !ReferenceEquals(value.Pawn, subject.Pawn)) && subject.Pawn.CurJobDef != JobDefOf.Ingest);
        EndToEndAssert.Equal(0f, Severity(subject, highDef)); EndToEndAssert.Equal(0f, Severity(subject, toleranceDef));
        if (ReferenceEquals(subject, b)) EndToEndAssert.Equal(0, subject.Pawn.health.hediffSet.hediffs.Count,
            "B's initially empty Health list must stay unchanged while it only carries supply.");
    }
    private void AssertZeroDecisions() => EndToEndAssert.True(decisions.All(value => value.Choice == "1:limit:0" && value.Completed &&
        value.RanOriginal && !value.Result && value.AllowedCalls == 1), "Actual scheduled zero decisions must finish false through the admitted boundary.");
    private void AssertADoseCompleted()
    {
        AssertAttachedSplits(); AssertHeld(a, attachedStock, 2); AssertNoIngestion(b);
        var history = History(a.Pawn);
        EndToEndAssert.True(dose != null && dose.Destroyed && ingestions.Count == 1 && ingestions[0].Completed && ingestions[0].RanOriginal &&
            ReferenceEquals(ingestions[0].Pawn, a.Pawn) && ReferenceEquals(ingestions[0].Source, dose) &&
            ingestions[0].Job?.SameOrder(attachedJob!) == true && ingestions[0].Job!.TargetA == dose.ThingID && ingestions[0].Tick >= chewingTick &&
            ingestions[0].SourceCount == 1 && history.Count == 1 && ReferenceEquals(history[0].drug, beerDef) &&
            history[0].lastTakenTicks == ingestions[0].Tick && (int)TimesTakenField.GetValue(history[0])! == 1 &&
            Severity(a, highDef) > 0 && Severity(a, toleranceDef) > 0 && a.Pawn.carryTracker.CarriedThing == null);
        AssertConservation(1);
    }
    private void AssertAttachedSplits()
    {
        EndToEndAssert.True(splits.Count >= 2 && attachedJob != null);
        AssertSplit(splits[0], 6, 1, 5, a, attachedJob!); AssertSplit(splits[1], 5, 2, 3, a, attachedJob!);
        EndToEndAssert.True(ReferenceEquals(splits[0].Result, dose) && ReferenceEquals(splits[1].Result, attachedStock) &&
            !ReferenceEquals(dose, attachedStock) && splits[0].Tick <= splits[1].Tick && splits[1].Tick <= chewingTick &&
            splits[0].Job!.TargetA == ground.ThingID && splits[1].Job!.TargetA == dose!.ThingID &&
            splits[1].Job!.TargetC == ground.ThingID && attachedJob!.TargetA == dose.ThingID);
    }
    private void AssertIndependentSplit()
    {
        EndToEndAssert.True(refillJob != null && a.Pawn.Drafted && !b.Pawn.Drafted);
        AssertSplit(splits[2], 3, 2, 1, b, refillJob!);
        EndToEndAssert.True(ReferenceEquals(splits[2].Result, independentStock) && splits[2].Tick >= refillSelectedTick &&
            !ReferenceEquals(independentStock, dose) && !ReferenceEquals(independentStock, attachedStock) &&
            refillJob!.TargetA == ground.ThingID && splits[2].Job!.TargetA == ground.ThingID);
    }
    private void AssertSplit(Split split, int before, int count, int after, Subject subject, JobSnapshot job)
    {
        EndToEndAssert.True(split.Completed && split.RanOriginal && split.Before == before && split.Requested == count && split.After == after &&
            split.ResultCount == count && split.SourceWasGround && ReferenceEquals(split.Pawn, subject.Pawn) && split.Job?.SameOrder(job) == true &&
            split.Result != null && !ReferenceEquals(split.Result, ground) && ownedBeer.Contains(split.Result) &&
            split.Job!.PawnId == subject.Pawn.ThingID && !split.Job.Forced && split.Job.Extra == (ReferenceEquals(subject, a) ? 2 : 0));
        EndToEndAssert.Equal((ReferenceEquals(subject, a) ? typeof(JobGiver_TakeDrugsForDrugPolicy) : typeof(JobGiver_MoveDrugsToInventory)).FullName, split.Job!.Giver);
    }
    private void AssertGround(int count) => EndToEndAssert.True(!ground.Destroyed && ground.Spawned &&
        ReferenceEquals(ground.Map, map) && ground.Position == groundCell && ground.stackCount == count &&
        map.listerThings.ThingsOfDef(beerDef).Count == 1 && ReferenceEquals(map.listerThings.ThingsOfDef(beerDef)[0], ground));
    private static void AssertJob(Job job, JobDef def, Type giver, int count) => EndToEndAssert.True(job.def == def &&
        job.jobGiver?.GetType() == giver && !job.playerForced && job.count == count);
    private void AssertHeld(Subject subject, Thing? thing, int count) => EndToEndAssert.True(thing != null && !thing.Destroyed &&
        thing.stackCount == count && ReferenceEquals(thing.holdingOwner, subject.Pawn.inventory.innerContainer) &&
        subject.Pawn.inventory.innerContainer.Count == 1 && subject.Pawn.inventory.innerContainer.TotalStackCountOfDef(beerDef) == count);
    private void AssertConservation(int consumed)
    {
        var physical = map.listerThings.ThingsOfDef(beerDef).Concat(new[] { a, b }.SelectMany(subject => subject.Pawn.inventory.innerContainer))
            .Concat(new[] { a.Pawn.carryTracker.CarriedThing, b.Pawn.carryTracker.CarriedThing }.OfType<Thing>())
            .Where(thing => thing != null && thing.def == beerDef && !thing.Destroyed).Distinct().ToArray();
        EndToEndAssert.True(physical.All(ownedBeer.Contains) && physical.Sum(thing => thing.stackCount) + consumed == 6 &&
            ownedBeer.Where(thing => !thing.Destroyed).All(physical.Contains), "Every live unit must remain in an observed physical holder; only actual ingestion reduces six.");
    }
    private static bool IsChewing(Pawn pawn)
    {
        if (pawn.jobs.curDriver is not JobDriver_Ingest driver || ChewingField.GetValue(driver) is not Toil chew ||
            ToilsField.GetValue(driver) is not List<Toil> toils || driver.CurToilIndex < 0 || driver.CurToilIndex >= toils.Count) return false;
        return ReferenceEquals(toils[driver.CurToilIndex], chew) && driver.ticksLeftThisToil > 0;
    }
    private static List<DrugTakeRecord> History(Pawn pawn) => (List<DrugTakeRecord>)HistoryField.GetValue(pawn.drugs)!;
    private static float Severity(Subject subject, HediffDef def) => subject.Pawn.health.hediffSet.GetFirstHediffOfDef(def)?.Severity ?? 0f;

    private void Install(MethodInfo target, string prefix, string? postfix = null)
    {
        traced.Add(target);
        HarmonyMethod Callback(string name) => new(typeof(NativeToleranceStockRouteTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!);
        traceOwner.Patch(target, prefix: Callback(prefix), postfix: postfix == null ? null : Callback(postfix));
        EndToEndAssert.True(runtime.Available, "Passive tracing must not make the real tolerance group unavailable.");
    }
    private static void BeginDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        var f = active;
        if (f == null || !ReferenceEquals(Current.Game, f.game) || !ReferenceEquals(__instance, f.a.Pawn.drugs) || __0 != f.beerDef || f.traceFailure != null) return;
        if (f.decisions.Count >= 1024) { f.traceFailure = "Scheduled observation bound reached."; return; }
        var value = new Decision { Tick = Find.TickManager.TicksGame, Choice = NativeDrugToleranceState.ReadEncoded(f.a.Entry) ?? "absent" };
        f.decisions.Add(value); f.pending.Push(value);
    }
    private static void ObserveAllowed(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        var f = active;
        if (f == null || !ReferenceEquals(Current.Game, f.game) || !ReferenceEquals(__instance, f.a.Pawn.drugs) || __0 != f.beerDef || f.traceFailure != null) return;
        if (f.pending.Count == 0) { f.traceFailure = "Scheduled eligibility had no actual selection caller."; return; }
        f.pending.Peek().AllowedCalls++;
    }
    private static void CompleteDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0, bool __result, bool __runOriginal)
    {
        var f = active;
        if (f == null || !ReferenceEquals(Current.Game, f.game) || !ReferenceEquals(__instance, f.a.Pawn.drugs) || __0 != f.beerDef || f.traceFailure != null) return;
        if (f.pending.Count == 0) { f.traceFailure = "Scheduled result had no matching entry."; return; }
        var value = f.pending.Pop(); value.Completed = true; value.Result = __result; value.RanOriginal = __runOriginal;
    }
    private static void BeginSplit(ThingWithComps __instance, int __0, out Split? __state)
    {
        __state = null; var f = active;
        if (f == null || !ReferenceEquals(Current.Game, f.game) || !ReferenceEquals(__instance, f.ground) || f.traceFailure != null) return;
        if (f.splits.Count >= 16 || f.phase is not (1 or 2)) { f.traceFailure = "Unexpected physical source split."; return; }
        var pawn = f.phase == 1 ? f.a.Pawn : f.b.Pawn; var job = pawn.CurJob;
        __state = new Split { Before = __instance.stackCount, Requested = __0, Tick = Find.TickManager.TicksGame,
            SourceWasGround = __instance.Spawned && ReferenceEquals(__instance.Map, f.map) && __instance.Position == f.groundCell,
            Pawn = pawn, Job = job == null ? null : new JobSnapshot(pawn, job) };
        f.splits.Add(__state);
    }
    private static void CompleteSplit(ThingWithComps __instance, Thing __result, bool __runOriginal, Split? __state)
    {
        var f = active; if (f == null || __state == null || !ReferenceEquals(Current.Game, f.game)) return;
        __state.Result = __result; __state.After = __instance.stackCount; __state.ResultCount = __result?.stackCount ?? -1;
        __state.Completed = true; __state.RanOriginal = __runOriginal;
        if (__result != null && ReferenceEquals(__result.def, f.beerDef)) f.ownedBeer.Add(__result);
    }
    private static void BeginIngestion(Thing __instance, Pawn __0, out Ingestion? __state)
    {
        __state = null; var f = active;
        if (f == null || !ReferenceEquals(Current.Game, f.game) || __instance.def != f.beerDef ||
            !ReferenceEquals(__0, f.a.Pawn) && !ReferenceEquals(__0, f.b.Pawn)) return;
        if (f.ingestions.Count >= 16) { f.traceFailure = "Ingestion observation bound reached."; return; }
        var job = __0.CurJob;
        __state = new Ingestion { Pawn = __0, Source = __instance, SourceCount = __instance.stackCount,
            Job = job == null ? null : new JobSnapshot(__0, job), Tick = Find.TickManager.TicksGame }; f.ingestions.Add(__state);
    }
    private static void CompleteIngestion(bool __runOriginal, Ingestion? __state)
    {
        if (__state == null) return;
        __state.Completed = true; __state.RanOriginal = __runOriginal;
    }

    private Vector2 RowPoint()
    {
        EndToEndAssert.True(ReferenceEquals(view.Selected(manager), a.Policy) && (Vector2)ScrollField.GetValue(manager)! == Vector2.zero);
        int index = Enumerable.Range(0, a.Policy.Count).Single(i => ReferenceEquals(a.Policy[i], a.Entry));
        EndToEndAssert.True(147 + a.Policy.Count * 35 < manager.windowRect.height - 36 - Window.CloseButSize.y - 11);
        return new(manager.windowRect.width - 18 - 1 - 60, 18 + 147 + index * 35 + 17.5f);
    }
    private Vector2 AnyPoint()
    {
        var font = Text.Font; bool wrap = Text.WordWrap;
        try
        {
            Text.Font = GameFont.Small; Text.WordWrap = true;
            float height = Text.CalcHeight("Megamod.DrugTolerance.RuleHelp".Translate(), editor.windowRect.width - 36);
            return new(editor.windowRect.width - 30, 18 + 50 + height + 14);
        }
        finally { Text.Font = font; Text.WordWrap = wrap; }
    }
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> retain) where T : Window => new(name, windows =>
    {
        RequireCurrent();
        var actual = Find.WindowStack.Windows.OfType<T>().Single();
        if (!ownedWindows.Contains(actual)) ownedWindows.Add(actual);
        window = windows.Single(item => item.NativeId == actual.ID && item.RuntimeType == actual.GetType().FullName);
        EndToEndAssert.True(window.Width > 0 && window.Height > 0 && window.UiScale > 0 &&
            actual.windowRect.xMin >= 0 && actual.windowRect.yMin >= 0 && actual.windowRect.xMax <= UI.screenWidth && actual.windowRect.yMax <= UI.screenHeight);
        retain(actual);
    });
    private NativeWindowClickActionStep Click(string name, Vector2 local)
    {
        EndToEndAssert.True(float.IsFinite(local.x) && float.IsFinite(local.y) && local.x >= 0 && local.y >= 0 && local.x < window.Width && local.y < window.Height);
        return new(name, window.Handle, (window.X + local.x) * window.UiScale, (window.Y + local.y) * window.UiScale, null, pointer);
    }
    private CameraActionStep Frame(string name) => new(name, new[] { a.Pawn.ThingID, b.Pawn.ThingID, ground.ThingID }, paddingPixels: 200);
    private static SelectionActionStep Select(Subject subject) => new("select " + subject.Pawn.Name.ToStringShort, new[] { subject.Pawn.ThingID }, additive: false);
    private SelectionActionStep SelectGround() => new("select the original physical ground Beer stack", new[] { ground.ThingID }, additive: false);
    private static PawnInspectTabActionStep Gear(Subject subject) => new("show native Gear for " + subject.Pawn.Name.ToStringShort, subject.Pawn.ThingID, EndToEndPawnInspectTab.Gear);
    private static PawnInspectTabActionStep Health(Subject subject) => new("show native Health for " + subject.Pawn.Name.ToStringShort, subject.Pawn.ThingID, EndToEndPawnInspectTab.Health);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static TimeControlActionStep Run(string name, EndToEndGameSpeed speed) => new(name, false, speed);
    private static TimeControlActionStep Pause(string name) => new(name, true, EndToEndGameSpeed.Normal);
    private static WaitUntilStep Wait(string name, Func<IEndToEndContext, bool> predicate, int ticks, int seconds) =>
        new(name, predicate, new EndToEndDeadline(int.MaxValue, ticks, TimeSpan.FromSeconds(seconds)));

    private IReadOnlyDictionary<string, string> Evidence()
    {
        RequireCurrent();
        return new Dictionary<string, string>
        {
            ["scope"] = "Core scheduled attached pickup and independent refill from empty; no substitute refill, medical, S024/S124/S134 or full S138 claim",
            ["phase"] = phase.ToString(), ["tick"] = Find.TickManager.TicksGame.ToString(),
            ["ground"] = ThingEvidence(ground), ["A"] = SubjectEvidence(a), ["B"] = SubjectEvidence(b),
            ["splits"] = string.Join(" | ", splits.Select(value => $"tick={value.Tick};before={value.Before};requested={value.Requested};after={value.After};result={ThingEvidence(value.Result)};returnedCount={value.ResultCount};pawn={value.Pawn?.ThingID};job=[{value.Job}];completed={value.Completed};original={value.RanOriginal}")),
            ["decisions"] = string.Join(" | ", decisions.Select(value => $"{value.Tick}:{value.Choice}:result={value.Result}:original={value.RanOriginal}:allowedCalls={value.AllowedCalls}:completed={value.Completed}")),
            ["ingestions"] = string.Join(" | ", ingestions.Select(value => $"tick={value.Tick};pawn={value.Pawn.ThingID};source={ThingEvidence(value.Source)};beforeCount={value.SourceCount};job=[{value.Job}];completed={value.Completed};original={value.RanOriginal}")),
            ["selectedAttachedJob"] = attachedJob?.ToString() ?? "unobserved", ["selectedRefillJob"] = refillJob?.ToString() ?? "unobserved",
            ["chewingTick"] = chewingTick.ToString(), ["doseCompletedTick"] = doseCompletedTick.ToString(), ["refillSelectedTick"] = refillSelectedTick.ToString(),
            ["traceOwner"] = traceOwner.Id, ["nativeOperations"] = nativeOperations.ToString(), ["screenshots"] = screenshots.ToString(),
            ["maximumWaitSeconds"] = waitSeconds.ToString(), ["scopeBudgetSeconds"] = "1800; launcher2100; exact fixed+wait+reserve1700"
        };
    }
    private string SubjectEvidence(Subject subject) => $"pawn={subject.Pawn.ThingID};policy={subject.Policy.id}:{subject.Policy.label};choice={NativeDrugToleranceState.ReadEncoded(subject.Entry)};scheduled={subject.Entry.allowScheduled};joy={subject.Entry.allowedForJoy};target={subject.Entry.takeToInventory};drafted={subject.Pawn.Drafted};currentJob=[{CurrentJobEvidence(subject.Pawn)}];inventory={string.Join(",", subject.Pawn.inventory.innerContainer.Select(ThingEvidence))};carried={ThingEvidence(subject.Pawn.carryTracker.CarriedThing)};history={string.Join(",", History(subject.Pawn).Select(record => record.drug.defName + ":tick=" + record.lastTakenTicks + ":count=" + TimesTakenField.GetValue(record)))};alcohol={Severity(subject, highDef).ToString("R", CultureInfo.InvariantCulture)};tolerance={Severity(subject, toleranceDef).ToString("R", CultureInfo.InvariantCulture)}";
    private static string CurrentJobEvidence(Pawn pawn) => pawn.CurJob is { } job ? new JobSnapshot(pawn, job).ToString() : "none";
    private static string ThingEvidence(Thing? thing) => thing == null ? "absent" :
        $"{thing.ThingID}:count={thing.stackCount}:destroyed={thing.Destroyed}:spawned={thing.Spawned}:holder={thing.ParentHolder?.GetType().FullName}:cell={thing.Position}";

    private void CleanupTrace()
    {
        if (ReferenceEquals(active, this)) active = null;
        var errors = new List<Exception>();
        foreach (var target in traced) Attempt(errors, () => traceOwner.Unpatch(target, HarmonyPatchType.All, traceOwner.Id));
        foreach (var target in traced) Attempt(errors, () => EndToEndAssert.False(HarmonyOwner.GetPatchInfo(target)?.Owners.Contains(traceOwner.Id) == true));
        if (errors.Count != 0) throw new AggregateException("Stock observer cleanup failed.", errors);
    }
    private void CleanupObjects()
    {
        RequireGame(); var errors = new List<Exception>();
        Attempt(errors, () => Find.Selector.ClearSelection());
        foreach (var owned in ownedWindows.AsEnumerable().Reverse())
            Attempt(errors, () =>
            {
                if (!Find.WindowStack.Windows.Contains(owned)) return;
                if (owned is MainTabWindow_Assign) Find.MainTabsRoot.EscapeCurrentTab(false);
                else owned.Close(false);
            });
        // Pawns own only generated fixture inventory; destroy their exact owned contents as well
        // as every observed split, including a failure halfway through pickup or chewing.
        foreach (var pawn in ownedPawns.AsEnumerable().Reverse())
            Attempt(errors, () => { if (!pawn.Destroyed) pawn.Destroy(DestroyMode.Vanish); });
        foreach (var thing in ownedBeer)
            Attempt(errors, () => { if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish); });
        foreach (var policy in ownedPolicies)
            Attempt(errors, () => { if (policy.label.StartsWith(marker, StringComparison.Ordinal)) game.drugPolicyDatabase.AllPolicies.Remove(policy); });
        if (errors.Count != 0) throw new AggregateException("Stock fixture resource cleanup failed.", errors);
    }
    private static void Attempt(List<Exception> errors, Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
    private static FieldInfo Field(Type owner, string name) => owner.GetField(name, InstanceFields) ?? throw new MissingFieldException(owner.FullName, name);
    private static MethodInfo Method(Type owner, string name, params Type[] arguments) => owner.GetMethod(name,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, arguments, null) ?? throw new MissingMethodException(owner.FullName, name);

    private sealed class Subject
    {
        internal readonly Pawn Pawn; internal readonly DrugPolicy Policy; internal readonly DrugPolicyEntry Entry; internal readonly bool Scheduled;
        internal Subject(Pawn pawn, DrugPolicy policy, DrugPolicyEntry entry, bool scheduled) { Pawn = pawn; Policy = policy; Entry = entry; Scheduled = scheduled; }
    }
    private sealed class Decision
    {
        internal int Tick, AllowedCalls; internal string Choice = ""; internal bool Completed, Result, RanOriginal;
    }
    private sealed class Split
    {
        internal int Before, Requested, After, ResultCount, Tick;
        internal bool SourceWasGround, Completed, RanOriginal;
        internal Pawn? Pawn; internal JobSnapshot? Job; internal Thing? Result;
    }
    private sealed class Ingestion
    {
        internal Pawn Pawn = null!; internal Thing Source = null!; internal JobSnapshot? Job;
        internal int Tick, SourceCount; internal bool Completed, RanOriginal;
    }
    // Native jobs are pooled and cleared after completion. Only values captured while the job is
    // the actor's current order may cross a yield, completion, Draft action or evidence checkpoint.
    private sealed class JobSnapshot
    {
        internal readonly int Id, Count, Extra;
        internal readonly bool Forced;
        internal readonly string PawnId, Def, Giver, TargetA, TargetC;
        internal JobSnapshot(Pawn pawn, Job job)
        {
            Id = job.loadID; Count = job.count; Extra = job.takeExtraIngestibles; Forced = job.playerForced;
            PawnId = pawn.ThingID; Def = job.def?.defName ?? "absent"; Giver = job.jobGiver?.GetType().FullName ?? "absent";
            TargetA = job.targetA.Thing?.ThingID ?? "absent"; TargetC = job.targetC.Thing?.ThingID ?? "absent";
        }
        internal bool SameOrder(JobSnapshot other) => Id >= 0 && Id == other.Id && PawnId == other.PawnId &&
            Def == other.Def && Giver == other.Giver && Count == other.Count && Extra == other.Extra && Forced == other.Forced;
        public override string ToString() => $"id={Id};pawn={PawnId};def={Def};giver={Giver};count={Count};extra={Extra};forced={Forced};targetA={TargetA};targetC={TargetC}";
    }
}
