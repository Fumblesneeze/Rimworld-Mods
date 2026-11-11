using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;

namespace Megamod.Settings;

// Native controls project the common store. This renderer never owns persistence or feature truth.
public sealed class SettingsRenderer
{
    private readonly SettingsRuntime runtime;
    private readonly Dictionary<string, Row> rows = new(StringComparer.Ordinal);
    private Vector2 scroll;
    private long activation = -1;

    public SettingsRenderer(SettingsRuntime runtime) => this.runtime = runtime;

    public void AddBoolean(SettingDefinition<bool> definition, string labelKey, string categoryKey,
        string helpKey, string? globalOnlyReasonKey = null) =>
        Add(definition, labelKey, categoryKey, helpKey, globalOnlyReasonKey, DrawBoolean);

    public void AddInteger(SettingDefinition<int> definition, string labelKey, string categoryKey,
        string helpKey, string? globalOnlyReasonKey = null) =>
        Add(definition, labelKey, categoryKey, helpKey, globalOnlyReasonKey, DrawInteger);

    public void AddDynamicBoolean(SettingDefinition<bool> definition, Func<string> label, string categoryKey,
        Func<string> help, Func<string?> status, Func<bool> visible, Func<string?> unavailableReasonKey,
        Func<int>? displayOrder = null)
    {
        if (definition.Scope != SettingScope.GlobalAndSave)
            throw new ArgumentException("Dynamic Boolean rows require global/save scope.", nameof(definition));
        AddRow(definition, label, categoryKey, help, null, DrawBoolean, DrawBoolean,
            new EditorState(), new EditorState(), 28, status, new SettingsEditGuard(unavailableReasonKey),
            visible, displayOrder);
    }

    public void AddSection(string id, string categoryId, int order, string categoryKey, float height,
        Action<Rect> draw, Action reset)
    {
        if (!float.IsFinite(height) || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        rows.Add(id, new Row(new(id, categoryId, order), categoryKey, height + 4, rect =>
        {
            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            bool wrap = Text.WordWrap;
            Color color = GUI.color;
            try { draw(rect); }
            finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.color = color; }
        }, reset));
    }

    public void AddComposite<T>(SettingDefinition<T> definition, string labelKey, string categoryKey,
        string helpKey, float editorHeight, Func<SettingsStore, bool, ISettingsCompositeEditor<T>> createEditor,
        string? globalOnlyReasonKey = null)
    {
        var saveEditor = createEditor(runtime.Store, false);
        var globalEditor = createEditor(runtime.Store, true);
        AddRow(definition, () => labelKey.Translate().ToString(), categoryKey, () => helpKey.Translate().ToString(), globalOnlyReasonKey,
            (rect, _, value, enabled, commit, state) => state.ErrorKey = saveEditor.Draw(rect, value, enabled, commit),
            (rect, _, value, enabled, commit, state) => state.ErrorKey = globalEditor.Draw(rect, value, enabled, commit),
            new EditorState { OnReset = saveEditor.Reset }, new EditorState { OnReset = globalEditor.Reset }, editorHeight);
    }

    private delegate void Editor<T>(Rect rect, SettingDefinition<T> definition, T value, bool enabled,
        Func<T, SettingEditResult> commit, EditorState state);

    private void Add<T>(SettingDefinition<T> definition, string labelKey, string categoryKey,
        string helpKey, string? globalOnlyReasonKey, Editor<T> editor)
        => AddRow(definition, () => labelKey.Translate().ToString(), categoryKey, () => helpKey.Translate().ToString(), globalOnlyReasonKey, editor, editor,
            new EditorState(), new EditorState(), 28);

    private void AddRow<T>(SettingDefinition<T> definition, Func<string> label, string categoryKey,
        Func<string> help, string? globalOnlyReasonKey, Editor<T> saveEditor, Editor<T> globalEditor,
        EditorState saveState, EditorState globalState, float editorHeight, Func<string?>? status = null,
        SettingsEditGuard? featureGuard = null, Func<bool>? visible = null, Func<int>? displayOrder = null)
    {
        if (rows.ContainsKey(definition.Id))
            throw new InvalidOperationException("A settings row with this ID is already registered.");
        if (definition.Scope == SettingScope.GlobalOnly && string.IsNullOrEmpty(globalOnlyReasonKey))
            throw new ArgumentException("A global-only row must explain its scope.", nameof(globalOnlyReasonKey));
        string? effectiveOffReason = runtime.Store.EffectiveOffReason(definition);
        MessageBlock? messages = null;
        float MeasureDynamicHeight(float width)
        {
            var binding = runtime.CurrentBinding;
            string? reason = Reason(definition, globalOnlyReasonKey, binding, runtime.Store.UsesGlobal(definition),
                featureGuard?.UnavailableReasonKey);
            string? error = saveState.ErrorKey ?? globalState.ErrorKey ?? runtime.Store.ReadCurrentRequested(definition).ErrorKey ??
                runtime.Store.ReadGlobal(definition).ErrorKey;
            messages = MessageBlock.Measure(width, reason?.Translate().ToString(), error?.Translate().ToString(),
                status?.Invoke(), effectiveOffReason?.Translate().ToString());
            return editorHeight + 56 + messages.Height + 8;
        }
        rows.Add(definition.Id, new Row(new(definition.Id, definition.CategoryId, definition.Order, visible, displayOrder), categoryKey, editorHeight + 132,
            rect => DrawRow(rect, definition, label(), help(), globalOnlyReasonKey,
                saveEditor, globalEditor, saveState, globalState, editorHeight, messages, featureGuard),
            () => { saveState.Reset(); globalState.Reset(); }, status == null && effectiveOffReason == null ? null : MeasureDynamicHeight));
    }

    public void Draw(Rect rect)
    {
        runtime.ProcessPendingLifecycle();
        if (activation != runtime.Store.Revision.Activation)
        {
            activation = runtime.Store.Revision.Activation;
            foreach (var row in rows.Values) row.Reset();
        }
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        bool beganScroll = false;
        try
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            float width = Math.Max(0, rect.width - 20);
            var ordered = SettingsRowLayout.Capture(rows.Values.Select(row => row.Layout))
                .Select(placement => rows[placement.Id])
                .Select(row => (Row: row, Height: row.MeasureHeight?.Invoke(width) ?? row.Height)).ToArray();
            int categories = ordered.Select(entry => entry.Row.Layout.CategoryId).Distinct().Count();
            var view = new Rect(0, 0, width, ordered.Sum(entry => entry.Height) + categories * 32);
            Widgets.BeginScrollView(rect, ref scroll, view);
            beganScroll = true;
            float y = 0;
            string? category = null;
            foreach (var entry in ordered)
            {
                var row = entry.Row;
                if (category != row.Layout.CategoryId)
                {
                    Widgets.Label(new Rect(0, y, view.width, 28), row.CategoryKey.Translate());
                    y += 32;
                    category = row.Layout.CategoryId;
                }
                row.Draw(new Rect(0, y, view.width, entry.Height - 4));
                y += entry.Height;
            }
        }
        finally
        {
            if (beganScroll) Widgets.EndScrollView();
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }
    }

    private void DrawRow<T>(Rect rect, SettingDefinition<T> definition, string label, string help,
        string? globalOnlyReasonKey, Editor<T> saveEditor, Editor<T> globalEditor,
        EditorState saveState, EditorState globalState, float editorHeight,
        MessageBlock? messages, SettingsEditGuard? featureGuard)
    {
        var store = runtime.Store;
        var binding = runtime.CurrentBinding;
        string? featureReason = featureGuard?.UnavailableReasonKey;
        bool featureAvailable = featureReason == null;
        bool saveAvailable = featureAvailable && binding != null && definition.Scope == SettingScope.GlobalAndSave;
        bool useGlobal = store.UsesGlobal(definition);
        float column = (rect.width - 16) / 3;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 26), label);
        TooltipHandler.TipRegion(new Rect(rect.x, rect.y, rect.width, 26), help);
        var inheritanceRect = new Rect(rect.x, rect.y + 32, column, 28);
        bool requestedInheritance = useGlobal;
        Widgets.CheckboxLabeled(inheritanceRect, "Megamod_Settings_UseGlobal".Translate(), ref requestedInheritance,
            disabled: !saveAvailable);
        if (saveAvailable && requestedInheritance != useGlobal)
        {
            var result = Commit(featureGuard, () => runtime.SetUseGlobal(binding!, definition, requestedInheritance));
            if (result.Succeeded)
                saveState.Reset();
            else
                saveState.ErrorKey = result.ErrorKey;
            useGlobal = store.UsesGlobal(definition);
        }

        float saveX = rect.x + column + 8;
        float globalX = rect.x + 2 * (column + 8);
        Widgets.Label(new Rect(saveX, rect.y + 28, column, 24), "Megamod_Settings_CurrentSave".Translate());
        Widgets.Label(new Rect(globalX, rect.y + 28, column, 24), "Megamod_Settings_GlobalDefault".Translate());
        var requested = store.ReadCurrentRequested(definition);
        var global = store.ReadGlobal(definition);
        saveEditor(new Rect(saveX, rect.y + 54, column, editorHeight), definition, requested.Value, saveAvailable && !useGlobal,
            value => Commit(featureGuard, () => binding == null ? new(false, "Megamod_Settings_NoActiveSave") : runtime.SetSaveOverride(binding, definition, value)), saveState);
        globalEditor(new Rect(globalX, rect.y + 54, column, editorHeight), definition, global.Value, featureAvailable,
            value => Commit(featureGuard, () => store.SetGlobal(definition, value)), globalState);

        if (messages != null)
        {
            messages.Draw(rect.x, rect.y + editorHeight + 56, rect.width);
            return;
        }
        string? reason = Reason(definition, globalOnlyReasonKey, binding, useGlobal, featureReason);
        if (reason != null)
            Widgets.Label(new Rect(rect.x, rect.y + editorHeight + 56, rect.width, 34), reason.Translate());
        string? error = saveState.ErrorKey ?? globalState.ErrorKey ?? requested.ErrorKey ?? global.ErrorKey;
        if (error != null)
            Widgets.Label(new Rect(rect.x, rect.y + editorHeight + 90, rect.width, 34), error.Translate());
    }

    private string? Reason<T>(SettingDefinition<T> definition, string? globalOnlyReasonKey,
        SaveSettingsBinding? binding, bool useGlobal, string? featureReason) =>
        featureReason ?? (definition.Scope == SettingScope.GlobalOnly ? globalOnlyReasonKey :
            !runtime.SaveLifecycleAvailable ? "Megamod_Settings_SaveScopeUnavailable" :
            binding == null ? "Megamod_Settings_NoActiveSave" : useGlobal ? "Megamod_Settings_Inheriting" : null);

    private static SettingEditResult Commit(SettingsEditGuard? guard, Func<SettingEditResult> commit) =>
        guard == null ? commit() : guard.Commit(commit);

    private static void DrawBoolean(Rect rect, SettingDefinition<bool> definition, bool value, bool enabled,
        Func<bool, SettingEditResult> commit, EditorState state)
    {
        bool edited = value;
        Widgets.CheckboxLabeled(rect, value ? "Megamod_Settings_On".Translate() : "Megamod_Settings_Off".Translate(),
            ref edited, disabled: !enabled);
        if (enabled && edited != value)
            state.ErrorKey = commit(edited).ErrorKey;
    }

    private static void DrawInteger(Rect rect, SettingDefinition<int> definition, int value, bool enabled,
        Func<int, SettingEditResult> commit, EditorState state)
    {
        string canonical = value.ToString(CultureInfo.InvariantCulture);
        if (state.Stored != canonical)
        {
            state.Stored = canonical;
            state.Buffer = canonical;
            state.ErrorKey = null;
        }
        if (!enabled)
        {
            Widgets.Label(rect, canonical);
            return;
        }
        string edited = Widgets.TextField(rect, state.Buffer!);
        if (edited == state.Buffer) return;
        state.Buffer = edited;
        if (!int.TryParse(edited, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            state.ErrorKey = "Megamod_Settings_InvalidInteger";
            return;
        }
        var result = commit(parsed);
        state.ErrorKey = result.ErrorKey;
        if (result.Succeeded) state.Stored = parsed.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record Row(SettingsRowLayout Layout, string CategoryKey, float Height,
        Action<Rect> Draw, Action Reset, Func<float, float>? MeasureHeight = null);

    private sealed record MessageBlock((string Text, float Height)[] Lines)
    {
        public float Height => Lines.Sum(line => line.Height + 4);

        public static MessageBlock Measure(float width, params string?[] messages)
        {
            bool previousWrap = Text.WordWrap;
            try
            {
                Text.WordWrap = true;
                return new(messages.Where(message => !string.IsNullOrEmpty(message))
                    .Select(message => (message!, Math.Max(34, Text.CalcHeight(message!, width)))).ToArray());
            }
            finally { Text.WordWrap = previousWrap; }
        }

        public void Draw(float x, float y, float width)
        {
            bool previousWrap = Text.WordWrap;
            try
            {
                Text.WordWrap = true;
                foreach (var line in Lines)
                {
                    Widgets.Label(new Rect(x, y, width, line.Height), line.Text);
                    y += line.Height + 4;
                }
            }
            finally { Text.WordWrap = previousWrap; }
        }
    }
    private sealed class EditorState
    {
        public string? Stored;
        public string? Buffer;
        public string? ErrorKey;
        public Action? OnReset;
        public void Reset() { Stored = null; Buffer = null; ErrorKey = null; OnReset?.Invoke(); }
    }
}
