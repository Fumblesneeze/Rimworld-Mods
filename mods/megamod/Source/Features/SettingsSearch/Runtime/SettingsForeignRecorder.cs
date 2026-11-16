using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Patching;
using UnityEngine;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

public sealed class SettingsForeignFrame
{
    internal SettingsForeignFrame(SettingsCaptureSnapshot capture, IReadOnlyList<SettingsForeignOption> options,
        IReadOnlyList<SettingsForeignTabView> tabs, IReadOnlyList<SettingsForeignScrollView> scrolls,
        SettingsForeignPath path, string? failure, string? coverageReason)
    { Capture = capture; Options = options; Tabs = tabs; Scrolls = scrolls; SelectedTabs = path.Tabs;
        PathIdentity = path.Identity; FailureReason = failure; CoverageReason = coverageReason; }
    public SettingsCaptureSnapshot Capture { get; }
    public IReadOnlyList<SettingsForeignOption> Options { get; }
    public string? FailureReason { get; }
    public string? CoverageReason { get; }
    public string PathIdentity { get; }
    internal IReadOnlyList<SettingsForeignTabView> Tabs { get; }
    internal IReadOnlyList<SettingsForeignScrollView> Scrolls { get; }
    internal IReadOnlyList<SettingsForeignTab> SelectedTabs { get; }
}

internal sealed record SettingsForeignTabView(SettingsForeignTab Identity, bool Selected, Action? Select,
    SettingsCaptureRect Bounds);

// Observes concrete native control completions alongside the existing text/help recorder.
// No foreign settings fields, editor callbacks, or arbitrary tooltip getters are read.
public sealed class SettingsForeignRecorder : IDisposable
{
    [ThreadStatic] private static FrameBuilder? active;
    private readonly int thread = Environment.CurrentManagedThreadId;
    private readonly AdmissionCoordinator admission;
    private readonly SettingsForeignTabScopes tabScopes;
    private readonly List<(MethodInfo Target, MethodInfo Callback, OwnedHarmonyPatch Patch)> patches = new();
    private readonly List<PatchCandidate> candidates = new();
    private readonly PatchGroup group;
    private bool initialized, disposed;

    public SettingsForeignRecorder(Action<string> warning)
    {
        admission = new(warning);
        tabScopes = new(warning);
        Bind(typeof(Widgets), "CheckboxLabeled", true, true, typeof(void), nameof(Checkbox),
            typeof(Rect), typeof(string), typeof(bool).MakeByRefType(), typeof(bool), typeof(Texture2D), typeof(Texture2D), typeof(bool), typeof(bool));
        Bind(typeof(Widgets), "RadioButtonLabeled", true, true, typeof(bool), nameof(Radio),
            typeof(Rect), typeof(string), typeof(bool), typeof(bool));
        Bind(typeof(Widgets), "ButtonTextWorker", false, true, typeof(Widgets.DraggableResult), nameof(Button),
            typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(Color), typeof(bool), typeof(bool), typeof(TextAnchor?));
        Bind(typeof(Widgets), "HorizontalSlider", true, true, typeof(float), nameof(Slider),
            typeof(Rect), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(string), typeof(string), typeof(string), typeof(float));
        Bind(typeof(GUI), "TextField", true, true, typeof(string), nameof(TextField), typeof(Rect), typeof(string), typeof(int), typeof(GUIStyle));
        Bind(typeof(GUI), "TextArea", true, true, typeof(string), nameof(TextField), typeof(Rect), typeof(string), typeof(int), typeof(GUIStyle));
        Bind(typeof(TabRecord), "get_Selected", true, false, typeof(bool), nameof(Selected));
        Bind(typeof(TabRecord), "Draw", true, false, typeof(void), nameof(Tab), typeof(Rect));
        Bind(typeof(GUI), "BeginScrollViewImpl", false, true, typeof(Vector2), nameof(BeginScroll),
            typeof(Rect), typeof(Vector2), typeof(Rect), typeof(GUIStyle), typeof(GUIStyle));
        Bind(typeof(GUI), "EndScrollView", true, true, typeof(void), nameof(EndScroll), typeof(bool));
        Bind(typeof(TooltipHandler), "TipRegion", true, true, typeof(void), nameof(Tooltip), typeof(Rect), typeof(TipSignal));
        group = new("settings-search.foreign-observation", AdmissionPhase.GameReady, candidates.ToArray());
    }

    public bool Initialize()
    {
        RequireOwner();
        initialized = admission.Apply(group, AdmissionPhase.GameReady);
        if (initialized) tabScopes.Initialize();
        return Available;
    }

    public bool Available
    {
        get
        {
            if (!initialized || disposed || !SettingsForeignScroll.Available) return false;
            try { return patches.All(patch => Owns(patch.Target, patch.Callback)); }
            catch (Exception) { return false; }
        }
    }

    public SettingsForeignFrame Observe(SettingsPage page, Action draw)
    {
        RequireOwner();
        var parent = active;
        var frame = new FrameBuilder(tabScopes.Available);
        var recorder = new SettingsPageRecorder(page, "foreign", () => Capture(builder => builder.OutsideDraw()));
        active = frame;
        try { recorder.Observe(draw); }
        finally { active = parent; }
        var capture = recorder.LastCapture ?? new SettingsCaptureSnapshot(page, "foreign", Array.Empty<SettingsCaptureRecord>(),
            SettingsCaptureStatus.Failed, SettingsCaptureFailure.RecordingFailed);
        if (!Available) frame.Failure ??= "settings-foreign-observation-unavailable";
        if (!tabScopes.Available) frame.BoundariesUnavailable();
        return frame.Complete(capture);
    }

    public void Dispose()
    {
        RequireOwner();
        if (disposed) return;
        if (active != null) throw new InvalidOperationException("settings-foreign-dispose-during-draw");
        disposed = true;
        initialized = false;
        List<Exception>? errors = null;
        try { tabScopes.Dispose(); }
        catch (Exception error) { (errors ??= new()).Add(error); }
        for (int i = patches.Count - 1; i >= 0; i--)
            try { patches[i].Patch.Rollback(); }
            catch (Exception error) { (errors ??= new()).Add(error); }
        if (errors != null) throw new AggregateException("Settings foreign observation cleanup failed.", errors);
    }

    private void Bind(Type type, string name, bool isPublic, bool isStatic, Type result, string callbackName, params Type[] arguments)
    {
        var target = type.GetMethod(name, BindingFlags.DeclaredOnly | (isPublic ? BindingFlags.Public : BindingFlags.NonPublic) |
            (isStatic ? BindingFlags.Static : BindingFlags.Instance), null, arguments, null);
        var callback = typeof(SettingsForeignRecorder).GetMethod(callbackName, BindingFlags.Static | BindingFlags.NonPublic)!;
        OwnedHarmonyPatch? patch = null;
        if (target != null)
        {
            patch = new(target, HarmonyPatchType.Postfix, callback, () => OwnedSlot(target));
            patches.Add((target, callback, patch));
        }
        string id = "settings-search.foreign-observe." + type.Name + "." + name;
        candidates.Add(new(id, "SS-FOREIGN-OBSERVATION", type.FullName + "." + name, "foreign option discovery and reveal",
            "ordinary settings pages", () => target == null || target.ReturnType != result || target.IsStatic != isStatic ||
                target.IsPublic != isPublic || target.IsGenericMethod || target.IsAbstract || target.GetMethodBody() == null ||
                !SettingsForeignScroll.Available ? "The native observation callable or scroll fields changed." : patch!.ValidateOwnership(),
            () => patch!.Install(), () => patch?.Rollback()));
    }

    private static IReadOnlyList<Patch> OwnedSlot(MethodInfo target)
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
            { owned = snapshot.Postfixes.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId).Select(patch => patch.ToOrderingPatch()).ToArray(); return true; }))
            throw new InvalidOperationException("settings-foreign-observation-publication-unavailable");
        return owned!;
    }
    private static bool Owns(MethodInfo target, MethodInfo callback)
    {
        var owned = OwnedSlot(target);
        return owned.Count == 1 && owned[0].PatchMethod == callback;
    }
    private void RequireOwner()
    {
        if (Environment.CurrentManagedThreadId != thread || !UnityData.IsInMainThread)
            throw new InvalidOperationException("settings-foreign-gui-thread-required");
    }

    private static void Capture(Action<FrameBuilder> observe)
    {
        if (active is not { } frame || Event.current?.type != EventType.Repaint || frame.Failure != null) return;
        try { observe(frame); }
        catch (Exception) { frame.Failure = "settings-foreign-observation-failed"; }
    }
    private static void Checkbox(Rect __0, string __1, bool __2, bool __runOriginal)
    { if (__runOriginal) Capture(frame => frame.Widget(SettingsForeignControlKind.Checkbox, __0, __1,
        (__2 ? "Megamod_Settings_On" : "Megamod_Settings_Off").Translate())); }
    private static void Radio(Rect __0, string __1, bool __2, bool __runOriginal)
    { if (__runOriginal) Capture(frame => frame.Widget(SettingsForeignControlKind.Radio, __0, __1,
        (__2 ? "Megamod_Settings_On" : "Megamod_Settings_Off").Translate())); }
    private static void Button(Rect __0, string __1, bool __runOriginal)
    { if (__runOriginal) Capture(frame => frame.Widget(SettingsForeignControlKind.Button, __0, __1, null)); }
    private static void Slider(Rect __0, string __5, bool __runOriginal)
    { if (__runOriginal) Capture(frame => frame.Widget(SettingsForeignControlKind.Slider, __0, __5, null)); }
    private static void TextField(Rect __0, string __result, bool __runOriginal)
    { if (__runOriginal) Capture(frame => frame.Widget(SettingsForeignControlKind.Text, __0, null, __result)); }
    private static void Selected(TabRecord __instance, bool __result) => Capture(frame => frame.Selected(__instance, __result));
    private static void Tab(TabRecord __instance, Rect __0, bool __runOriginal)
    { if (__runOriginal) Capture(frame => frame.Tab(__instance, __0)); }
    private static void BeginScroll(Rect __0, Rect __2, bool __runOriginal)
    { if (__runOriginal) Capture(frame => frame.BeginScroll(__0, __2)); }
    private static void EndScroll(bool __runOriginal)
    { if (__runOriginal) Capture(frame => frame.EndScroll()); }
    private static void Tooltip(Rect __0, TipSignal __1, bool __runOriginal)
    { if (__runOriginal && active != null) SettingsPageRecorder.ObserveTooltip(__0, __1.text); }

    internal static IDisposable? EnterTabScope()
    {
        IDisposable? scope = null;
        Capture(frame => scope = frame.EnterTab());
        return scope;
    }
    internal static void TabScopeFailed() => Capture(frame => frame.NavigationUnavailable());

    private sealed class FrameBuilder
    {
        private readonly List<(SettingsForeignWidget Widget, int Sequence)> widgets = new();
        private readonly List<SettingsForeignScrollView> scrolls = new();
        private readonly List<int> scrollStack = new();
        private readonly Dictionary<TabRecord, bool> selections = new(ReferenceEqualityComparer.Instance);
        private readonly List<LogicalTabs> groups = new();
        private readonly bool scopesAvailable;
        private LogicalTabs? currentTabs;
        private int textUnits, sequence, tabsCount, drawingTabs, unknownFrom = int.MaxValue;
        private bool truncated;
        private string? coverageReason;
        internal string? Failure;
        internal FrameBuilder(bool scopesAvailable) => this.scopesAvailable = scopesAvailable;
        internal void BoundariesUnavailable() { if (groups.Count != 0) NavigationUnavailable(); }
        internal void NavigationUnavailable()
        {
            coverageReason ??= "settings-foreign-tab-navigation-incomplete";
            unknownFrom = 0;
        }
        private void Incomplete(int from, string reason = "settings-foreign-tab-navigation-incomplete")
        { coverageReason ??= reason; unknownFrom = Math.Min(unknownFrom, from); }
        private void Truncate(string reason)
        { truncated = true; coverageReason ??= reason; }

        internal IDisposable? EnterTab()
        {
            if (!scopesAvailable) return null;
            if (currentTabs == null)
            {
                if (groups.Count >= 256) { Incomplete(sequence); return null; }
                currentTabs = new(sequence);
                groups.Add(currentTabs);
            }
            drawingTabs++;
            return new TabScope(this);
        }
        internal void OutsideDraw()
        {
            // Inspected native DrawTabs multirow/overflow paths emit no rendering between
            // their rows. A completed outside draw delimits the next logical tab run.
            if (drawingTabs != 0 || currentTabs == null) return;
            currentTabs.End = sequence++;
            currentTabs = null;
        }
        internal void Widget(SettingsForeignControlKind kind, Rect rect, string? label, string? value)
        {
            OutsideDraw();
            if (truncated) return;
            if (widgets.Count >= 4096 || (label?.Length ?? 0) + (long)(value?.Length ?? 0) > 262144 - textUnits)
            { Truncate("settings-foreign-control-limit"); return; }
            textUnits += (label?.Length ?? 0) + (value?.Length ?? 0);
            widgets.Add((new(kind, label, value, SettingsForeignScroll.ScreenBounds(rect), Array.AsReadOnly(scrollStack.ToArray())), sequence++));
        }
        internal void Selected(TabRecord tab, bool selected)
        {
            if (!selections.ContainsKey(tab) && selections.Count >= 256)
            { Incomplete(sequence, "settings-foreign-tab-limit"); return; }
            selections[tab] = selected;
        }
        internal void Tab(TabRecord tab, Rect rect)
        {
            if (currentTabs == null || tabsCount >= 256 || (tab.label?.Length ?? 0) > 262144 - textUnits ||
                !selections.TryGetValue(tab, out bool selected))
            { Incomplete(sequence, "settings-foreign-tab-state-unavailable"); return; }
            textUnits += tab.label?.Length ?? 0;
            currentTabs.Observed.Add((tab, selected, SettingsForeignScroll.ScreenBounds(rect)));
            tabsCount++;
            sequence++;
        }
        internal void BeginScroll(Rect viewport, Rect content)
        {
            if (scrolls.Count >= 256 || scrollStack.Count >= 16) { Failure = "settings-foreign-scroll-limit"; return; }
            scrollStack.Add(scrolls.Count);
            scrolls.Add(SettingsForeignScroll.Current(viewport, content));
        }
        internal void EndScroll()
        {
            if (scrollStack.Count == 0) { Failure = "settings-foreign-scroll-unbalanced"; return; }
            scrollStack.RemoveAt(scrollStack.Count - 1);
        }
        internal SettingsForeignFrame Complete(SettingsCaptureSnapshot capture)
        {
            OutsideDraw();
            if (capture.Status == SettingsCaptureStatus.Failed || scrollStack.Count != 0 || drawingTabs != 0)
                Failure ??= "settings-foreign-draw-incomplete";
            var observedTabs = new List<SettingsForeignTabView>();
            var selected = new List<(int End, SettingsForeignTab Tab)>();
            var groupIds = new Dictionary<string, int>(StringComparer.Ordinal);
            var budget = new SettingsForeignTextBudget();
            var paths = new SettingsForeignPaths(budget);
            paths.TryGet(Array.Empty<SettingsForeignTab>(), out var rootPath);
            // Preserve room for already observed row text before generating tab identities.
            long rowUnits = widgets.Sum(entry => (long)(entry.Widget.Label?.Length ?? 0) + (entry.Widget.Value?.Length ?? 0));
            foreach (var group in groups)
            {
                if (group.Observed.Count == 0) continue;
                var members = group.Observed.OrderBy(tab => tab.Tab.label, StringComparer.Ordinal).ToArray();
                if (group.End == null ||
                    group.Observed.Count(tab => tab.Selected) != 1 ||
                    members.Select(tab => tab.Tab).Distinct(ReferenceEqualityComparer.Instance).Count() != members.Length ||
                    members.Select(tab => tab.Tab.label).Distinct(StringComparer.Ordinal).Count() != members.Length)
                { Incomplete(group.Start, "settings-foreign-tabs-ambiguous"); continue; }
                long units = members.Sum(tab => (long)(tab.Tab.label?.Length ?? 0) + SettingsForeignTextBudget.PackedLength(tab.Tab.label ?? ""));
                if (units > budget.Remaining - rowUnits || !budget.Reserve(units))
                { Truncate("settings-foreign-retention-limit"); Incomplete(group.Start); break; }
                string id = string.Concat(members.Select(tab => SettingsForeignControls.Pack(tab.Tab.label ?? "")));
                if (groupIds.TryGetValue(id, out int previous))
                { Incomplete(previous, "settings-foreign-tabs-ambiguous"); continue; }
                groupIds.Add(id, group.Start);
                foreach (var tab in members)
                {
                    observedTabs.Add(new(new(id, tab.Tab.label ?? ""), tab.Selected, tab.Tab.clickedAction, tab.Bounds));
                    if (tab.Selected) selected.Add((group.End.Value, new(id, tab.Tab.label ?? "")));
                }
            }
            var annotated = new List<SettingsForeignWidget>();
            var pathByCount = new Dictionary<int, SettingsForeignPath> { [0] = rootPath };
            foreach (var entry in widgets)
            {
                int count = selected.Count(tab => tab.End <= entry.Sequence);
                if (!pathByCount.TryGetValue(count, out var path))
                {
                    if (!paths.TryGet(selected.Take(count).Select(tab => tab.Tab).ToArray(), out path))
                    { Truncate("settings-foreign-retention-limit"); break; }
                    pathByCount.Add(count, path);
                }
                annotated.Add(entry.Widget with { Tabs = path.Tabs, CanReveal = entry.Sequence < unknownFrom });
            }
            var fullPath = rootPath;
            if (!paths.TryGet(selected.Select(tab => tab.Tab).ToArray(), out fullPath))
            { Truncate("settings-foreign-retention-limit"); fullPath = rootPath; Incomplete(0); }
            if (unknownFrom == 0)
                for (int i = 0; i < annotated.Count; i++) annotated[i] = annotated[i] with { CanReveal = false };
            var projection = Failure == null ? SettingsForeignControls.ProjectBounded(capture, annotated,
                Array.Empty<SettingsForeignTab>(), budget, paths) : new SettingsForeignProjection(Array.Empty<SettingsForeignOption>(), false);
            if (truncated || projection.Truncated)
            { capture = capture with { Status = SettingsCaptureStatus.Truncated }; coverageReason ??= "settings-foreign-retention-limit"; }
            return new(capture, projection.Options, observedTabs.AsReadOnly(), scrolls.AsReadOnly(), fullPath, Failure, coverageReason);
        }
        private sealed class LogicalTabs
        {
            internal LogicalTabs(int start) => Start = start;
            internal readonly int Start;
            internal int? End;
            internal readonly List<(TabRecord Tab, bool Selected, SettingsCaptureRect Bounds)> Observed = new();
        }
        private sealed class TabScope : IDisposable
        {
            private FrameBuilder? owner;
            internal TabScope(FrameBuilder owner) => this.owner = owner;
            public void Dispose()
            {
                if (owner == null) return;
                owner.drawingTabs--;
                owner = null;
            }
        }
    }
}
