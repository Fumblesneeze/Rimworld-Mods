using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Patching;
using Megamod.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace Megamod.Features.DrugTolerance.Runtime;

// Root owns registration and admission. Persistence remains independently active when this
// atomic group or its ordinary setting is off. No decision or GUI context survives in this owner.
public sealed class NativeDrugToleranceRuntime
{
    private readonly SettingsRuntime settings;
    private readonly DrugTolerancePersistencePatch persistence;
    private readonly Action<string> report;
    private readonly HashSet<string> reported = new(StringComparer.Ordinal);
    private static readonly ConditionalWeakTable<Dialog_ManageDrugPolicies, DrugToleranceWindowState> windowStates = new();
    private readonly ConditionalWeakTable<Game, object> readingNotices = new();
    private readonly Dictionary<Type, bool> joyTypes = new();
    private readonly NativeDrugToleranceGeometry? geometry;
    private readonly DrugToleranceAllocationAdmission? allocation;
    private readonly DrugToleranceDecisionPatch? scheduled, joy;
    private readonly DrugToleranceSocialPatch? social;
    private readonly DrugToleranceEditorPatch? header, row;
    private readonly FieldInfo? selectedPolicy, policies;
    private readonly MethodInfo? joyTarget;
    private string? resolutionFailure, status;
    private bool installed;

    public NativeDrugToleranceRuntime(SettingsRuntime settings, DrugTolerancePersistencePatch persistence, Action<string> protectedWarning)
    {
        this.settings = settings; this.persistence = persistence; report = protectedWarning;
        try
        {
            settings.Store.ReadGlobal(DrugToleranceSettings.Enabled); // Root registers before loading the common store.
            selectedPolicy = Field(typeof(Dialog_ManagePolicies<DrugPolicy>), "policyInt", typeof(DrugPolicy));
            policies = Field(typeof(DrugPolicyDatabase), "policies", typeof(List<DrugPolicy>));
            var manager = typeof(Dialog_ManageDrugPolicies);
            var calculator = Method(manager, "CalculateColumnsWidths", false,
                new[] { typeof(Rect) }.Concat(Enumerable.Repeat(typeof(float).MakeByRefType(), 9)).ToArray());
            var width = typeof(Rect).GetProperty(nameof(Rect.width))!;
            allocation = new(calculator, new(width.GetMethod!));
            geometry = new(Report);
            var readers = new[] { "x", "y", "width", "height", "xMin", "xMax", "yMin", "yMax" }
                .Select(name => typeof(Rect).GetProperty(name)!.GetMethod!).ToArray();
            header = new("DT-POLICY-HEADER", Method(manager, "DoColumnLabels", false, typeof(Rect)), true,
                NativeDrugToleranceEditorBindings.Callbacks(true), new DrugToleranceEditorCallGuard(calculator, width.GetMethod!, width.SetMethod!, true, readers).Validate,
                (owner, rect, _, _) => Prepare(owner, (Rect)rect, null, -1, true), CoreAvailable, Report);
            row = new("DT-POLICY-ROW", Method(manager, "DoEntryRow", false, typeof(Rect), typeof(DrugPolicyEntry), typeof(int)), false,
                NativeDrugToleranceEditorBindings.Callbacks(false), new DrugToleranceEditorCallGuard(calculator, width.GetMethod!, width.SetMethod!, false, readers).Validate,
                (owner, rect, entry, index) => Prepare(owner, (Rect)rect, entry as DrugPolicyEntry, index, false), CoreAvailable, Report);
            scheduled = new("DT-SCHEDULED", Method(typeof(Pawn_DrugPolicyTracker), nameof(Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow), false, typeof(ThingDef)), true,
                (tracker, drug) => tracker is Pawn_DrugPolicyTracker actual && drug is ThingDef definition && ReferenceEquals(actual.pawn?.drugs, actual)
                    ? Allows(actual.pawn, definition, RoutineDrugUse.Scheduled) : true, () => Available, Report);
            joyTarget = Method(typeof(JoyGiver_Ingest), "CanIngestForJoy", false, typeof(Pawn), typeof(Thing));
            var exception = new DrugToleranceJoyExceptionGuard(new(
                typeof(Pawn).GetField(nameof(Pawn.story))!, typeof(Pawn_StoryTracker).GetField(nameof(Pawn_StoryTracker.traits))!,
                typeof(TraitDefOf).GetField(nameof(TraitDefOf.DrugDesire))!, typeof(TraitSet).GetMethod(nameof(TraitSet.DegreeOfTrait), new[] { typeof(TraitDef) })!,
                typeof(Pawn).GetProperty(nameof(Pawn.InMentalState))!.GetMethod!));
            joy = new("DT-DIRECT-JOY", joyTarget, false,
                (actor, candidate) => actor is Pawn pawn && candidate is Thing thing ? Allows(pawn, thing.def, RoutineDrugUse.DirectRecreation) : true,
                () => Available, Report, exception.Validate);
            var shuffle = typeof(GenList).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == nameof(GenList.Shuffle) && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(ThingDef));
            social = new(Method(typeof(JoyGiver_SocialRelax), "TryFindIngestibleToNurse", true, typeof(IntVec3), typeof(Pawn), typeof(Thing).MakeByRefType()),
                Field(typeof(JoyGiver_SocialRelax), "nurseableDrugs", typeof(List<ThingDef>), true), shuffle,
                (actor, drug) => actor is Pawn pawn && drug is ThingDef definition ? Allows(pawn, definition, RoutineDrugUse.SocialDrink) : true,
                () => Available, Report);
            Group = new("drug-tolerance.enforcement-editor", AdmissionPhase.Early,
                geometry.Candidates.Concat(new[] { allocation.Candidate, scheduled.Candidate, joy.Candidate, social.Candidate, header.Candidate, row.Candidate,
                    new PatchCandidate("DT-NATIVE-RUNTIME", "DT-POLICY-EDITOR", "native tolerance composition", "tolerance editing and enforcement", "native decisions and retained row state",
                        () => null, () => installed = true, () => installed = false) }).ToArray());
        }
        catch (Exception error)
        {
            resolutionFailure = error.GetBaseException().Message;
            Group = new("drug-tolerance.enforcement-editor", AdmissionPhase.Early, new[]
            {
                new PatchCandidate("DT-NATIVE-RESOLVE", "DT-POLICY-EDITOR", "native tolerance contracts", "tolerance editing and enforcement", "native decisions and retained row state",
                    () => resolutionFailure, () => {}, () => {})
            });
        }
    }

    public PatchGroup Group { get; }
    public string? StatusKey
    {
        get
        {
            if (!UnityData.IsInMainThread) return "Megamod.DrugTolerance.ContextUnavailable";
            if (!settings.Store.GetEffective(DrugToleranceSettings.Enabled)) return null;
            return Available ? null : status ?? "Megamod.DrugTolerance.Unavailable";
        }
    }
    public bool Available
    {
        get
        {
            try
            {
                if (!CoreAvailable()) return false;
                var windows = Find.WindowStack;
                for (int i = 0; i < windows.Count; i++)
                    if (windows[i] is Dialog_ManageDrugPolicies manager)
                    {
                        if (!DrugToleranceWindowState.Supports(manager, typeof(Dialog_ManageDrugPolicies)))
                        { status = "Megamod.DrugTolerance.Unavailable"; return false; }
                        if (!NativeDrugToleranceGeometry.FitsCurrentViewport(manager.windowRect))
                        { status = "Megamod.DrugTolerance.LayoutUnavailable"; return false; }
                    }
                float column = MeasureColumn();
                if (!geometry!.TryGetProspectiveWidths(out float headerWidth, out float rowWidth) ||
                    !DrugToleranceLayout.TryReserve(headerWidth, column, true, out _) ||
                    !DrugToleranceLayout.TryReserve(rowWidth, column, false, out _))
                { status = "Megamod.DrugTolerance.LayoutUnavailable"; return false; }
                for (int i = 0; i < windows.Count; i++)
                    if (windows[i] is Dialog_ManageDrugPolicies manager && windowStates.TryGetValue(manager, out var failure) &&
                        failure.HasFailure((policy, entry) => policy is DrugPolicy actual && entry is DrugPolicyEntry native &&
                            CurrentPolicy(Current.Game, actual) && native.drug != null && NativeDrugToleranceRows.IsCurrent(actual, native, native.drug)))
                    { status = "Megamod.DrugTolerance.LayoutUnavailable"; return false; }
                status = null; return true;
            }
            catch (Exception error) { status = "Megamod.DrugTolerance.Unavailable"; Report(error.GetBaseException().Message); return false; }
        }
    }
    // Root's existing lifecycle pump calls this even when the ordinary feature setting is off.
    // Notices belong to an actual policy workflow, never every global tick or a failed-patch log.
    public static void ObserveContext(SettingsRuntime settings, NativeDrugToleranceRuntime? runtime)
    {
        if (!UnityData.IsInMainThread) return;
        foreach (var item in windowStates)
            if (!OpenWindow(item.Key)) windowStates.Remove(item.Key);
        if (Current.Game == null || Current.ProgramState != ProgramState.Playing || LongEventHandler.AnyEventNowOrWaiting ||
            Find.WindowStack == null || settings.CurrentBinding == null) return;
        var managers = Find.WindowStack.Windows.OfType<Dialog_ManageDrugPolicies>().ToArray();
        if (managers.Length == 0) return;
        string? reason = !settings.Store.GetEffective(DrugToleranceSettings.Enabled) ? "Megamod.DrugTolerance.Off" :
            runtime?.Available == true ? null : runtime?.status ?? "Megamod.DrugTolerance.Unavailable";
        foreach (var manager in managers)
            if (OpenWindow(manager) && windowStates.GetValue(manager, _ => new()).TakeNotice(reason))
                try { Messages.Message(reason!.Translate(), MessageTypeDefOf.NeutralEvent, false); }
                catch (Exception) { }
    }
    private bool CoreAvailable()
    {
        if (!UnityData.IsInMainThread || Current.Game == null || Current.ProgramState != ProgramState.Playing ||
            LongEventHandler.AnyEventNowOrWaiting || Find.WindowStack == null || settings.CurrentBinding == null)
        { status = "Megamod.DrugTolerance.ContextUnavailable"; return false; }
        if (!settings.Store.GetEffective(DrugToleranceSettings.Enabled)) return false;
        if (!installed || resolutionFailure != null || !persistence.CanUseChoices || geometry?.Available != true ||
            allocation?.Available != true || scheduled?.Available != true || joy?.Available != true || social?.Available != true ||
            header?.Available != true || row?.Available != true || allocation.Failure != null || scheduled.Failure != null ||
            joy.Failure != null || social.Failure != null || header.Failure != null || row.Failure != null || !SupportedJoyProviders())
        { status = "Megamod.DrugTolerance.Unavailable"; return false; }
        return true;
    }
    private bool SupportedJoyProviders()
    {
        foreach (var definition in DefDatabase<JoyGiverDef>.AllDefsListForReading)
        {
            var type = definition.giverClass;
            if (type == null || !typeof(JoyGiver_Ingest).IsAssignableFrom(type) ||
                (!typeof(JoyGiver_TakeDrug).IsAssignableFrom(type) && definition.thingDefs?.Any(d => d?.IsDrug == true) != true)) continue;
            if (!joyTypes.TryGetValue(type, out bool supported))
                joyTypes[type] = supported = DrugToleranceJoyProvider.UsesNativeDecision(type.GetMethod("CanIngestForJoy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(Pawn), typeof(Thing) }, null), joyTarget!);
            if (!supported) { Report("A direct drug joy provider overrides the admitted native decision: " + type.FullName); return false; }
        }
        return true;
    }

    // Future owned S024 calls this for its actual patient at selection; no native feeding hook.
    public bool AllowsPatientTreat(Pawn patient, ThingDef drug) => !Available || Allows(patient, drug, RoutineDrugUse.PatientTreat);
    private bool Allows(Pawn pawn, ThingDef drug, RoutineDrugUse route)
    {
        if (!Available || pawn?.drugs == null || drug == null || !CurrentDrug(drug)) return true;
        var policy = pawn.drugs.CurrentPolicy;
        if (policy == null || !CurrentPolicy(Current.Game, policy)) return true;
        var entry = NativeDrugToleranceRows.FindUnique(policy, drug);
        if (entry == null || (!entry.allowedForJoy && !entry.allowScheduled)) return true;
        var reading = NativeDrugToleranceReader.Read(drug, pawn.health?.hediffSet?.hediffs);
        bool directException = route == RoutineDrugUse.DirectRecreation &&
            ((pawn.story?.traits?.DegreeOfTrait(TraitDefOf.DrugDesire) ?? 0) > 0 || pawn.InMentalState);
        var result = DrugToleranceRule.Evaluate(true, true, route, directException, NativeDrugToleranceState.Read(entry), reading);
        if (result == DrugToleranceDecision.Unavailable && NativeDrugToleranceReader.Resolve(drug) != null)
            ReadingUnavailable();
        return result is DrugToleranceDecision.Allowed or DrugToleranceDecision.Unavailable;
    }
    private void ReadingUnavailable()
    {
        var game = Current.Game;
        if (game == null || readingNotices.TryGetValue(game, out _)) return;
        readingNotices.Add(game, new object());
        try { report("[fumblesneeze.megamod] DT-NATIVE targetSpec=DT-SCHEDULED/DT-DIRECT-JOY/DT-SOCIAL-DRINK; current pawn tolerance reading unavailable; this routine selection keeps native permission; retained choices are unchanged."); }
        catch (Exception) { }
        try { Messages.Message("Megamod.DrugTolerance.ReadingUnavailable".Translate(), MessageTypeDefOf.RejectInput, false); }
        catch (Exception) { }
    }

    private DrugToleranceEditorReservation? Prepare(object owner, Rect rectangle, DrugPolicyEntry? entry, int index, bool isHeader)
    {
        if (owner is not Dialog_ManageDrugPolicies manager || !OpenManager(manager) || !CoreAvailable()) return null;
        var game = Current.Game;
        var policy = Selected(manager);
        if (policy == null || !CurrentPolicy(game, policy)) return null;
        if (!isHeader)
        {
            var rows = NativeDrugToleranceRows.Read(policy);
            if (entry?.drug == null || rows == null || index < 0 || index >= rows.Count || !ReferenceEquals(rows[index], entry) ||
                !NativeDrugToleranceRows.IsCurrent(policy, entry, entry.drug)) return null;
        }
        var failure = windowStates.GetValue(manager, _ => new());
        DrugToleranceReservation space = default;
        bool viewportFits = NativeDrugToleranceGeometry.FitsCurrentViewport(manager.windowRect);
        bool allocationFits = Finite(rectangle) && DrugToleranceLayout.TryReserve(rectangle.width, MeasureColumn(), isHeader, out space);
        bool fits = isHeader ? failure.AssessHeader(allocationFits, viewportFits) : failure.AssessRow(policy, entry!, allocationFits, viewportFits);
        if (!fits) { status = "Megamod.DrugTolerance.LayoutUnavailable"; Report("A native policy " + (isHeader ? "header" : "row") + " cannot fit its current tolerance control."); return null; }
        if (!Available) return null;
        var original = rectangle;
        var owned = new Rect(original.x + space.ColumnOffset, original.y, space.ColumnWidth, original.height);
        rectangle.width = space.NativeWidth;
        return new(rectangle, () =>
        {
            if (!ReferenceEquals(Current.Game, game) || !OpenManager(manager) || !ReferenceEquals(Selected(manager), policy) || !CurrentPolicy(game, policy) || !Available) return;
            if (isHeader) DrawHeader(owned);
            else if (entry?.drug != null && NativeDrugToleranceRows.IsCurrent(policy, entry, entry.drug)) DrawRow(manager, entry, owned);
        });
    }
    private static void DrawHeader(Rect rectangle) => WithGui(() =>
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rectangle, "Megamod.DrugTolerance.Header".Translate());
        TooltipHandler.TipRegion(rectangle, "Megamod.DrugTolerance.RuleHelp".Translate());
    });
    private void DrawRow(Dialog_ManageDrugPolicies manager, DrugPolicyEntry entry, Rect rectangle) => WithGui(() =>
    {
        var game = Current.Game; var policy = Selected(manager); var drug = entry.drug;
        var original = NativeDrugToleranceState.Read(entry);
        bool applicable = CurrentDrug(drug) && NativeDrugToleranceReader.Resolve(drug) != null;
        string label = applicable ? ChoiceLabel(original) : "Megamod.DrugTolerance.UnavailableShort".Translate().ToString();
        bool inactive = !entry.allowedForJoy && !entry.allowScheduled;
        if (applicable && inactive) label = "Megamod.DrugTolerance.Inactive".Translate(label);
        if (GUI.enabled && applicable)
        {
            // Use the same native control and rectangle treatment as mood/recreation. Do not
            // pass roundTo: native rounding also runs on repaint and would alter retained fractions.
            float before = DrugToleranceSlider.Position(original);
            bool mouseDrag = Event.current.type == EventType.MouseDrag;
            float after = Widgets.HorizontalSlider(new Rect(rectangle.x, rectangle.y + 2, rectangle.width, rectangle.height).ContractedBy(4),
                before, 0, DrugToleranceSlider.Maximum, middleAlignment: true, label: label);
            bool interacted = after != before || mouseDrag && Event.current.type == EventType.Used;
            var choice = DrugToleranceSlider.Apply(original, after, interacted);
            if (!ReferenceEquals(choice, original) && ReferenceEquals(Current.Game, game) && Available && OpenManager(manager) &&
                policy != null && ReferenceEquals(Selected(manager), policy) && CurrentPolicy(game, policy) && CurrentDrug(drug) &&
                NativeDrugToleranceReader.Resolve(drug) != null && NativeDrugToleranceRows.IsCurrent(policy, entry, drug) &&
                ReferenceEquals(NativeDrugToleranceState.Read(entry), original)) NativeDrugToleranceState.Write(entry, choice);
        }
        else Widgets.Label(rectangle, label);
        TooltipHandler.TipRegion(rectangle, (applicable ? inactive ? "Megamod.DrugTolerance.InactiveHelp" : "Megamod.DrugTolerance.RuleHelp" : "Megamod.DrugTolerance.CapabilityUnavailable").Translate());
    });
    private static string ChoiceLabel(DrugToleranceChoice choice) => choice.Kind switch
    {
        DrugToleranceChoiceKind.Any => "Megamod.DrugTolerance.Any".Translate(),
        DrugToleranceChoiceKind.NeedsReview => "Megamod.DrugTolerance.NeedsReview".Translate(),
        _ => choice.Limit.ToString("P2", CultureInfo.CurrentCulture)
    };
    private static float MeasureColumn()
    {
        var font = Text.Font;
        try
        {
            Text.Font = GameFont.Small;
            var choices = new[] { "Megamod.DrugTolerance.Any".Translate().ToString(), "Megamod.DrugTolerance.NeedsReview".Translate().ToString(),
                "Megamod.DrugTolerance.UnavailableShort".Translate().ToString(), "Megamod.DrugTolerance.Header".Translate().ToString(), 1f.ToString("P2", CultureInfo.CurrentCulture) };
            return choices.Concat(choices.Take(2).Concat(new[] { choices[4] }).Select(label => "Megamod.DrugTolerance.Inactive".Translate(label).ToString()))
                .Max(label => Text.CalcSize(label).x) + 16;
        }
        finally { Text.Font = font; }
    }
    private static void WithGui(Action draw)
    {
        var font = Text.Font; var anchor = Text.Anchor; bool wrap = Text.WordWrap, enabled = GUI.enabled; var color = GUI.color;
        try { Text.Font = GameFont.Small; Text.Anchor = TextAnchor.MiddleCenter; Text.WordWrap = false; draw(); }
        finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.enabled = enabled; GUI.color = color; }
    }
    private DrugPolicy? Selected(Dialog_ManageDrugPolicies manager) => selectedPolicy?.GetValue(manager) as DrugPolicy;
    private bool CurrentPolicy(Game game, DrugPolicy policy) => game != null && ReferenceEquals(Current.Game, game) &&
        policies?.GetValue(game.drugPolicyDatabase) is List<DrugPolicy> current && current.Any(item => ReferenceEquals(item, policy));
    private static bool CurrentDrug(ThingDef drug) => drug != null && drug.category == ThingCategory.Item && drug.IsDrug &&
        DefDatabase<ThingDef>.AllDefsListForReading.Any(item => ReferenceEquals(item, drug));
    private static bool OpenManager(Dialog_ManageDrugPolicies manager) =>
        DrugToleranceWindowState.Supports(manager, typeof(Dialog_ManageDrugPolicies)) && OpenWindow(manager);
    private static bool OpenWindow(Dialog_ManageDrugPolicies manager) => manager.IsOpen && Find.WindowStack != null &&
        Find.WindowStack.Windows.Any(window => ReferenceEquals(window, manager));
    private static bool Finite(Rect rectangle) => float.IsFinite(rectangle.x) && float.IsFinite(rectangle.y) &&
        float.IsFinite(rectangle.width) && float.IsFinite(rectangle.height) && rectangle.width > 0 && rectangle.height >= 28;
    private void Report(string reason)
    {
        string bounded = reason.Replace('\r', ' ').Replace('\n', ' ');
        if (bounded.Length > 256) bounded = bounded.Substring(0, 256);
        if (reported.Count >= 16 || !reported.Add(bounded)) return;
        try { report("[fumblesneeze.megamod] DT-NATIVE targetSpec=DT-POLICY-EDITOR; reason=" + bounded + "; tolerance controls/restrictions unavailable; native decisions and retained choices remain."); }
        catch (Exception) { }
    }
    private static FieldInfo Field(Type owner, string name, Type type, bool isStatic = false)
    {
        var field = owner.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance));
        return field?.FieldType == type && field.IsStatic == isStatic ? field : throw new InvalidOperationException("Native field changed: " + owner.FullName + "." + name);
    }
    private static MethodInfo Method(Type owner, string name, bool isStatic, params Type[] arguments) =>
        owner.GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance), null, arguments, null)
        ?? throw new InvalidOperationException("Native method changed: " + owner.FullName + "." + name);
}
