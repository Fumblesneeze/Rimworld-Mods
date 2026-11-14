using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

// This scope protects the explicitly inspected native GUI state, not arbitrary foreign callbacks.
// The caller owns GUI-thread admission, neutral Event, hidden backend, action guards and their cleanup.
public sealed class SettingsGuiStateScope : IDisposable
{
    private const int MaximumSnapshotEntries = 65536;
    private readonly List<StatePart> parts;
    private readonly int ownerThread;
    private bool isolationStarted;
    private bool disposed;

    private SettingsGuiStateScope(List<StatePart> parts)
    {
        this.parts = parts;
        ownerThread = Environment.CurrentManagedThreadId;
    }

    // No explicit state writes occur here. All shape/budget checks and snapshots precede Isolate.
    // The normal GUI lifecycle must already have initialized native GUI/Text/Widgets types.
    public static SettingsGuiStateScope Capture()
    {
        try
        {
            var capture = new CaptureBuilder();
            CaptureGui(capture);
            CaptureGuiUtility(capture);
            CaptureText(capture);
            CaptureWidgets(capture);
            CaptureInputLatches(capture);
            return new SettingsGuiStateScope(capture.Parts);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Cannot capture the required native settings GUI state.", exception);
        }
    }

    // Install the hidden backend and neutral Event before invoking this method. A failed isolate
    // restores even partially changed state immediately; a subsequent Dispose remains harmless.
    public void Isolate()
    {
        RequireOwnerThread();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (isolationStarted)
            throw new InvalidOperationException("The settings GUI state is already isolated.");

        isolationStarted = true;
        try
        {
            foreach (StatePart part in parts)
                part.Isolate?.Invoke();
        }
        catch (Exception isolationError)
        {
            try
            {
                Dispose();
            }
            catch (Exception restorationError)
            {
                throw new AggregateException("Settings GUI isolation and restoration failed.", isolationError, restorationError);
            }
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;
        RequireOwnerThread();
        disposed = true;
        if (!isolationStarted)
        {
            parts.Clear();
            return;
        }

        List<Exception>? failures = null;
        // Reverse order restores collection contents before putting mutable original references
        // back in their native slots. A failed component cannot prevent the remaining restoration.
        for (int index = parts.Count - 1; index >= 0; index--)
        {
            StatePart part = parts[index];
            try
            {
                part.Restore();
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(
                    new InvalidOperationException("Could not restore native GUI state " + part.Name + ".", exception));
            }
        }
        parts.Clear();
        if (failures != null)
            throw new AggregateException("Native settings GUI restoration was incomplete.", failures);
    }

    private void RequireOwnerThread()
    {
        if (ownerThread != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Settings GUI state must be isolated and restored on its capturing GUI thread.");
    }

    private static void CaptureGui(CaptureBuilder capture)
    {
        Type gui = typeof(GUI);
        capture.Save(gui, "_skin", typeof(GUISkin));
        capture.Save(gui, "_color", typeof(Color));
        capture.Save(gui, "_contentColor", typeof(Color));
        // Never restore through GUI.matrix: its setter would call a possibly replaced backend.
        capture.Save(gui, "_matrix", typeof(Matrix4x4));
        capture.Reset(gui, "<enabled>k__BackingField", false);
        capture.Save(gui, "<backgroundColor>k__BackingField", typeof(Color));
        capture.Reset(gui, "<changed>k__BackingField", false);
        capture.Reset(gui, "<tooltip>k__BackingField", string.Empty);
        capture.CaptureDepth();

        capture.Reset(gui, "_buttonDownRect", default(Rect));
        capture.Reset(gui, "_buttonDownActive", false);
        capture.Reset(gui, "_buttonDownButton", 0);
        capture.Reset(gui, "_repeatDownRect", default(Rect));
        capture.Reset(gui, "_repeatDownActive", false);

        foreach (string name in new[] { "_syncedEditorId", "_syncedCaret", "_syncedAnchor",
            "_caretIndex", "_selectAnchor", "_selectAllOnFocusId" })
            capture.Reset(gui, name, 0);
        capture.Reset(gui, "_nextNamedControlId", int.MinValue);
        capture.Reset<string?>(gui, "_nextControlName", null);
        capture.Reset<string?>(gui, "_pendingFocusName", null);
        capture.Reset(gui, "_textScrollX", 0f);
        capture.Reset(gui, "_textScrollY", 0f);
        capture.Reset(gui, "_fieldRichText", false);
        capture.Reset(gui, "_focusMovedThisPass", false);

        capture.Dictionary<int, string>(gui, "_idToName", true);
        capture.Dictionary<string, int>(gui, "_nameToId", true);
        capture.Dictionary<int, int>(gui, "_idToWindowId", true);
        capture.IntSet(gui, "_fieldsDrawnThisPass", true);
        capture.Dictionary<(string, int), int>(gui, "_repeatedNameToId", true);
        capture.Dictionary<string, int>(gui, "_nameOccurrences", true);
        capture.List(gui, "_selectionRects", typeof(Rect), true);

        Type scroll = RequireNestedType(gui, "ScrollViewState", false);
        RequireField(scroll, "id", typeof(int), false, false);
        RequireField(scroll, "position", typeof(Rect), false, false);
        RequireField(scroll, "scrollPosition", typeof(Vector2), false, false);
        RequireField(scroll, "maxX", typeof(float), false, false);
        RequireField(scroll, "maxY", typeof(float), false, false);
        capture.List(gui, "_scrollViewStates", scroll, true);
        capture.Dictionary<int, Vector2>(gui, "_pendingScroll", true);
        capture.Reset(gui, "_scrollViewDepth", 0);
        capture.Reset(gui, "_scrollDragId", 0);
        capture.Reset(gui, "_scrollDragGrab", 0f);
        capture.Reset(gui, "_guiSliderDragId", 0);
        capture.Reset(gui, "_draggingWindowId", -1);
        capture.Reset(gui, "_dragStartMouseGui", default(Vector2));
        capture.Reset(gui, "_dragStartWindowPos", default(Vector2));
        capture.Reset(gui, "_currentWindowId", int.MinValue);
        capture.Reset(gui, "_currentWindowRect", default(Rect));
        capture.Reset(gui, "_currentWindowDragged", false);
        capture.Reset(gui, "_draggedWindowPos", default(Vector2));
        capture.Reset(gui, "_focusedWindowId", int.MinValue);

        Type window = RequireNestedType(gui, "PendingWindow", true);
        RequireField(window, "Id", typeof(int), false, false);
        RequireField(window, "Rect", typeof(Rect), false, false);
        RequireField(window, "Func", typeof(GUI.WindowFunction), false, false);
        RequireField(window, "Matrix", typeof(Matrix4x4), false, false);
        RequireField(window, "Style", typeof(GUIStyle), false, false);
        capture.List(gui, "_pendingWindows", window, true);
        capture.List(gui, "_lastWindows", window, true);
        capture.List(gui, "_layoutWindows", window, true);
        capture.Dictionary<int, Rect>(gui, "_movedWindowRects", true);
        capture.List(gui, "_staleMovedRectIds", typeof(int), true);
        capture.Content(gui, "_tempContent", true);
        capture.Content(typeof(GUIContent), "s_Text", true);
        capture.Content(typeof(GUIContent), "none", false);
    }

    private static void CaptureGuiUtility(CaptureBuilder capture)
    {
        Type utility = typeof(GUIUtility);
        capture.Reset(utility, "nextControlId", 1000);
        capture.Reset(utility, "<keyboardControl>k__BackingField", 0);
        capture.Reset(utility, "<hotControl>k__BackingField", 0);
        // Original TextEditor objects are retained only in the snapshot, not this native map.
        capture.Dictionary<(Type, int), object>(utility, "stateObjects", true);
        Type textFocus = RequireNativeType("UnityStubs.Internal.TextFocus");
        capture.Reset(textFocus, "ReadOnlyId", 0);

        Type ime = RequireNativeType("UnityStubs.Internal.ImeComposition");
        capture.Save(ime, "Mode", typeof(IMECompositionMode));
        capture.Save(ime, "<Text>k__BackingField", typeof(string));
        capture.Save(ime, "<Cursor>k__BackingField", typeof(int));
        capture.Save(ime, "<CandidatePos>k__BackingField", typeof(Vector2));
        // Never call ImeComposition.Set: it notifies arbitrary subscribers before cleanup.
    }

    private static void CaptureText(CaptureBuilder capture)
    {
        Type text = typeof(Text);
        // The public Font setter may normalize Tiny to Small. Preserve the exact backing values.
        capture.Save(text, "fontInt", typeof(GameFont));
        capture.Save(text, "anchorInt", typeof(TextAnchor));
        capture.Save(text, "wordWrapInt", typeof(bool));
        capture.Content(text, "tmpTextGUIContent", false);

        var styles = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (string name in new[] { "fontStyles", "textFieldStyles", "textAreaStyles", "textAreaReadOnlyStyles" })
        {
            GUIStyle[] originals = capture.Array<GUIStyle>(text, name, true, 3, false);
            foreach (GUIStyle style in originals)
            {
                if (ReferenceEquals(style, null))
                    throw ShapeFailure(text, name, "contains an uninitialized style");
                if (!styles.Add(style))
                    continue;
                // CurFontStyle mutates these two exact properties on existing shared objects.
                // No GUIStyle clone is used: the native copy constructor is not a complete copy.
                capture.SaveInstance(style, typeof(GUIStyle), "<alignment>k__BackingField", typeof(TextAnchor));
                capture.SaveInstance(style, typeof(GUIStyle), "<wordWrap>k__BackingField", typeof(bool));
            }
        }
    }

    private static void CaptureWidgets(CaptureBuilder capture)
    {
        Type widgets = typeof(Widgets);
        capture.BooleanStack(widgets, "mouseOverScrollViewStack", false);
        capture.Reset(widgets, "checkboxPainting", false);
        capture.Reset(widgets, "checkboxPaintingState", false);
        capture.Reset(widgets, "dropdownPainting", false);
        capture.Reset<object?>(widgets, "dropdownPainting_Payload", null);
        capture.Reset<Type?>(widgets, "dropdownPainting_Type", null);
        capture.Reset(widgets, "dropdownPainting_Text", string.Empty);
        capture.Reset<Texture2D?>(widgets, "dropdownPainting_Icon", null);
        capture.Reset(widgets, "buttonInvisibleDraggable_activeControl", 0);
        capture.Reset(widgets, "buttonInvisibleDraggable_dragged", false);
        capture.Reset(widgets, "buttonInvisibleDraggable_mouseStart", default(Vector3));
        capture.Reset(widgets, "sliderDraggingID", 0);
        capture.Reset(widgets, "draggingId", 0);
        Type rangeEnd = RequireNestedType(widgets, "RangeEnd", true);
        if (!rangeEnd.IsEnum || Enum.GetUnderlyingType(rangeEnd) != typeof(byte)
            || !Enum.IsDefined(rangeEnd, "None") || Convert.ToByte(Enum.Parse(rangeEnd, "None")) != 0)
            throw ShapeFailure(widgets, "RangeEnd", "requires the native byte enum with None=0");
        capture.ResetUntyped(widgets, "curDragEnd", rangeEnd, Enum.ToObject(rangeEnd, (byte)0));
        capture.Save(widgets, "lastDragSliderSoundTime", typeof(float));
        capture.Array<string>(widgets, "tmpTranslatedColorComponentLabels", false, 6, true);
        capture.Array<int>(widgets, "intColorComponents", false, 6, true);
    }

    private static void CaptureInputLatches(CaptureBuilder capture)
    {
        Type fixer = typeof(UnityGUIBugsFixer);
        // Pin both consumed/computed markers to this frame so CurrentEventDelta returns zero
        // without sampling real mouse position. Native -1 means "recompute", not "neutral".
        int frame = Time.frameCount;
        capture.Reset(fixer, "currentEventDelta", default(Vector2));
        capture.Reset(fixer, "lastMousePositionFrame", frame);
        capture.Reset(fixer, "leftMouseButtonPressed", false);
        capture.Reset(fixer, "lastFrameMousePosition", default(Vector2));
        capture.Reset(fixer, "frameDeltaComputedFrame", frame);
        capture.Reset(fixer, "frameDeltaConsumedFrame", frame);
        capture.Reset<Vector2?>(fixer, "lastMousePosition", null);
    }

    private static Type RequireNativeType(string name)
    {
        Type? type = typeof(GUI).Assembly.GetType(name, false, false);
        if (type == null || !type.IsAbstract || !type.IsSealed)
            throw new InvalidOperationException("Required native settings GUI type is unavailable: " + name + ".");
        return type;
    }

    private static Type RequireNestedType(Type owner, string name, bool valueType)
    {
        Type? type = owner.GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic);
        if (type == null || type.DeclaringType != owner || type.IsValueType != valueType || type.ContainsGenericParameters)
            throw ShapeFailure(owner, name, "missing exact nested native type");
        return type;
    }

    private static FieldInfo RequireField(Type owner, string name, Type fieldType, bool isStatic, bool readOnly)
    {
        FieldInfo? field = owner.GetField(name, BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        if (field == null || field.DeclaringType != owner || field.FieldType != fieldType
            || field.IsStatic != isStatic || field.IsInitOnly != readOnly || field.IsLiteral)
            throw ShapeFailure(owner, name, "missing exact " + (readOnly ? "readonly " : string.Empty)
                + (isStatic ? "static " : "instance ") + fieldType.FullName + " field");
        return field;
    }

    private static InvalidOperationException ShapeFailure(Type owner, string name, string reason) =>
        new("Cannot isolate native settings GUI state " + owner.FullName + "." + name + ": " + reason + ".");

    private sealed class StatePart
    {
        internal readonly string Name;
        internal readonly Action? Isolate;
        internal readonly Action Restore;

        internal StatePart(string name, Action? isolate, Action restore)
        {
            Name = name;
            Isolate = isolate;
            Restore = restore;
        }
    }

    private sealed class CaptureBuilder
    {
        internal readonly List<StatePart> Parts = new();
        private int retainedEntries;

        internal void Save(Type owner, string name, Type fieldType) =>
            CaptureField(RequireField(owner, name, fieldType, true, false), null, null);

        internal void SaveInstance(object target, Type owner, string name, Type fieldType) =>
            CaptureField(RequireField(owner, name, fieldType, false, false), target, null);

        internal void Reset<T>(Type owner, string name, T neutral) =>
            ResetUntyped(owner, name, typeof(T), neutral);

        internal void ResetUntyped(Type owner, string name, Type fieldType, object? neutral)
        {
            FieldInfo field = RequireField(owner, name, fieldType, true, false);
            CaptureField(field, null, () => field.SetValue(null, neutral));
        }

        private void CaptureField(FieldInfo field, object? target, Action? isolate)
        {
            object? original = field.GetValue(target);
            Parts.Add(new StatePart(field.DeclaringType!.FullName + "." + field.Name, isolate,
                () => field.SetValue(target, original)));
        }

        private T Reference<T>(Type owner, string name, Type exactType, bool readOnly, bool exactInstance = true)
            where T : class
        {
            FieldInfo field = RequireField(owner, name, exactType, true, readOnly);
            object? value = field.GetValue(null);
            if (value is not T original || (exactInstance && value.GetType() != exactType))
                throw ShapeFailure(owner, name, "requires an initialized " + exactType.FullName + " object");
            Parts.Add(new StatePart(owner.FullName + "." + name + " reference", null, () =>
            {
                if (readOnly)
                {
                    if (!ReferenceEquals(field.GetValue(null), original))
                        throw ShapeFailure(owner, name, "readonly native reference changed unexpectedly");
                }
                else
                    field.SetValue(null, original);
            }));
            return original;
        }

        private void Reserve(Type owner, string name, int entries)
        {
            if (entries < 0 || entries > MaximumSnapshotEntries - retainedEntries)
                throw ShapeFailure(owner, name, "exceeds the aggregate 65536-entry native snapshot safety budget");
            retainedEntries += entries;
        }

        internal void Dictionary<TKey, TValue>(Type owner, string name, bool readOnly) where TKey : notnull
        {
            var original = Reference<Dictionary<TKey, TValue>>(owner, name, typeof(Dictionary<TKey, TValue>), readOnly);
            Reserve(owner, name, original.Count);
            var entries = new KeyValuePair<TKey, TValue>[original.Count];
            ((ICollection<KeyValuePair<TKey, TValue>>)original).CopyTo(entries, 0);
            Parts.Add(new StatePart(owner.FullName + "." + name + " entries", original.Clear, () =>
            {
                original.Clear();
                foreach (KeyValuePair<TKey, TValue> entry in entries)
                    original.Add(entry.Key, entry.Value);
            }));
        }

        internal void List(Type owner, string name, Type elementType, bool readOnly)
        {
            Type exactType = typeof(List<>).MakeGenericType(elementType);
            IList original = Reference<IList>(owner, name, exactType, readOnly);
            Reserve(owner, name, original.Count);
            var entries = new object?[original.Count];
            original.CopyTo(entries, 0);
            Parts.Add(new StatePart(owner.FullName + "." + name + " entries", original.Clear, () =>
            {
                original.Clear();
                foreach (object? entry in entries)
                    original.Add(entry);
            }));
        }

        internal void IntSet(Type owner, string name, bool readOnly)
        {
            var original = Reference<HashSet<int>>(owner, name, typeof(HashSet<int>), readOnly);
            Reserve(owner, name, original.Count);
            var entries = new int[original.Count];
            original.CopyTo(entries);
            Parts.Add(new StatePart(owner.FullName + "." + name + " entries", original.Clear, () =>
            {
                original.Clear();
                foreach (int entry in entries)
                    original.Add(entry);
            }));
        }

        internal void BooleanStack(Type owner, string name, bool readOnly)
        {
            var original = Reference<Stack<bool>>(owner, name, typeof(Stack<bool>), readOnly);
            Reserve(owner, name, original.Count);
            bool[] topToBottom = original.ToArray();
            Parts.Add(new StatePart(owner.FullName + "." + name + " entries", original.Clear, () =>
            {
                original.Clear();
                for (int index = topToBottom.Length - 1; index >= 0; index--)
                    original.Push(topToBottom[index]);
            }));
        }

        internal T[] Array<T>(Type owner, string name, bool readOnly, int expectedLength, bool clear)
        {
            var original = Reference<T[]>(owner, name, typeof(T[]), readOnly);
            if (original.Length != expectedLength)
                throw ShapeFailure(owner, name, "requires exactly " + expectedLength + " native array entries");
            Reserve(owner, name, original.Length);
            var entries = (T[])original.Clone();
            Parts.Add(new StatePart(owner.FullName + "." + name + " entries",
                clear ? () => System.Array.Clear(original, 0, original.Length) : null,
                () => System.Array.Copy(entries, original, entries.Length)));
            return entries;
        }

        internal void Content(Type owner, string name, bool readOnly)
        {
            var original = Reference<GUIContent>(owner, name, typeof(GUIContent), readOnly, false);
            SaveInstance(original, typeof(GUIContent), "m_Text", typeof(string));
            SaveInstance(original, typeof(GUIContent), "m_Image", typeof(Texture));
            SaveInstance(original, typeof(GUIContent), "m_Tooltip", typeof(string));
        }

        internal void CaptureDepth()
        {
            PropertyInfo? depth = typeof(GUI).GetProperty("depth", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            MethodInfo? getter = depth?.GetGetMethod();
            MethodInfo? setter = depth?.GetSetMethod();
            if (depth == null || depth.PropertyType != typeof(int) || depth.GetIndexParameters().Length != 0
                || getter == null || !getter.IsStatic || getter.ReturnType != typeof(int) || getter.GetParameters().Length != 0
                || setter == null || !setter.IsStatic || setter.ReturnType != typeof(void)
                || setter.GetParameters().Length != 1 || setter.GetParameters()[0].ParameterType != typeof(int))
                throw ShapeFailure(typeof(GUI), "depth", "missing native static int getter/setter");
            int original = GUI.depth;
            Parts.Add(new StatePart("UnityEngine.GUI.depth", null, () => GUI.depth = original));
        }
    }
}
