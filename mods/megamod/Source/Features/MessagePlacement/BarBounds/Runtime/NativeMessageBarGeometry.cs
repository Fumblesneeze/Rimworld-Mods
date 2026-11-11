using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Megamod.Features.MessagePlacement;

// One GUI invocation's native preparation. No drawing, collision queries, world scan or retained layout.
internal sealed class NativeMessageBarGeometry
{
    private const string Unavailable = "Megamod_Messages_BarGeometryUnavailable";
    private static readonly FieldInfo EntriesField = Field(typeof(ColonistBar), "cachedEntries");
    private static readonly FieldInfo DirtyField = Field(typeof(ColonistBar), "entriesDirty");
    private static readonly FieldInfo LabelsField = Field(typeof(ColonistBarColonistDrawer), "pawnLabelsCache");
    private static readonly FieldInfo EntryVersion = Field(typeof(List<ColonistBar.Entry>), "_version");
    private static readonly FieldInfo LocationVersion = Field(typeof(List<Vector2>), "_version");
    private static readonly MethodInfo LabelMethod = LabelHelper("GetPawnLabel");
    private static readonly MethodInfo WidthMethod = LabelHelper("GetPawnLabelNameWidth");
    private readonly Func<bool> admitted;
    private readonly Func<Func<bool>> captureAuthority;
    private readonly Action<string> invalidated;
    private Context? previous;
    private Func<bool>? previousAuthority;
    private Snapshot? snapshot;
    private MessageBarBoundsReceipt? receipt;
    private bool reported;

    internal NativeMessageBarGeometry(Func<bool> admitted, Func<Func<bool>> captureAuthority, Action<string> invalidated)
    { this.admitted = admitted; this.captureAuthority = captureAuthority; this.invalidated = invalidated; }

    internal MessageBarBoundsReceipt? Current => receipt;

    internal void Preparing(object actualBar)
    {
        snapshot = null; receipt = null;
        if (actualBar is not ColonistBar bar || !admitted() || !TryContext(bar, out var context)) return;
        // Native's own dirty path prepares rows for the first/new viewport before the real Entries getter.
        if (previous != context || previousAuthority?.Invoke() != true) bar.MarkColonistsDirty();
        previous = context;
        previousAuthority = captureAuthority();
    }

    internal MessageBarBoundsReceipt Capture(object actualBar, object actualEntries, bool frames)
    {
        try { return receipt = CaptureCurrent(actualBar, actualEntries, frames); }
        catch (Exception)
        {
            snapshot = null;
            return receipt = MessageBarBoundsReceipt.Unavailable(Unavailable, () => UnityData.IsInMainThread && Event.current != null);
        }
    }
    private MessageBarBoundsReceipt CaptureCurrent(object actualBar, object actualEntries, bool frames)
    {
        snapshot = null;
        if (actualBar is not ColonistBar bar || !TryContext(bar, out var context))
            return MessageBarBoundsReceipt.Unavailable(Unavailable, () => false);
        bool ContextCurrent() => TryContext(bar, out var now) && context == now;
        MessageBarBoundsReceipt Decline() => MessageBarBoundsReceipt.Unavailable(Unavailable, ContextCurrent);
        if (!admitted() || actualEntries is not List<ColonistBar.Entry> entries ||
            !ReferenceEquals(EntriesField.GetValue(bar), entries) || (bool)DirtyField.GetValue(bar)!) return Decline();
        var locations = bar.DrawLocs;
        if (locations.Count != entries.Count) return Decline();
        // These current native paths need their own downstream guards. Do not reserve unseen decorations.
        if (frames || context!.World || ReorderableWidget.Dragging) return Decline();
        var size = bar.Size;
        float scale = bar.Scale;
        var nativeEntries = entries.ToArray();
        var nativeLocations = locations.ToArray();
        var pawns = new PawnState?[entries.Count];
        var measured = new MessageBarEntryGeometry[entries.Count];
        for (int index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var rectangle = new MessageBounds(locations[index].x, locations[index].y, size.x, size.y);
            if (entry.pawn == null) { measured[index] = new(rectangle, false, 0, false, 0, null); continue; }
            if (!TryPawn(entry.pawn, entry.map, bar, out var pawn)) return Decline();
            pawns[index] = pawn;
            measured[index] = new(rectangle, true, pawn!.LabelExtent, false, 0, null);
        }
        if (!MessageBarGeometry.TryMeasure(measured, scale, false, out double? bottom)) return Decline();
        var captured = new Snapshot(this, context!, entries, locations, nativeEntries, nativeLocations,
            pawns, size, scale, (int)EntryVersion.GetValue(entries)!, (int)LocationVersion.GetValue(locations)!, captureAuthority());
        captured.Bounds = bottom.HasValue ? MessageBarBoundsReceipt.Visible(bottom.Value, captured.IsCurrent) :
            MessageBarBoundsReceipt.Absent(captured.IsCurrent);
        snapshot = captured;
        return captured.Bounds;
    }

    internal void Consuming(object actualBar, int index, object actualRectangle)
    {
        var current = snapshot;
        if (current == null) return;
        if (ReferenceEquals(current.Context.Bar, actualBar) && actualRectangle is Rect rect && current.Matches(index, rect)) return;
        snapshot = null; receipt = null;
        if (reported) return;
        reported = true;
        invalidated("The current colonist bar changed after message placement; completed native effects were preserved.");
    }

    internal void EndInvocation()
    {
        snapshot = null; receipt = null;
        if (previous != null && (!TryContext(previous.Bar, out var current) || current != previous))
        { previous = null; previousAuthority = null; }
    }

    private static bool TryPawn(Pawn pawn, Map map, ColonistBar bar, out PawnState? result)
    {
        result = null;
        if (Selected(pawn) || WeaponVisible(pawn)) return false;
        if (MoodThresholdExtensions.CurrentMoodThresholdFor(pawn).EdgeExpansion() > 0) return false;
        string source = pawn.LabelShortCap;
        float width = bar.Size.x + bar.SpaceBetweenColonistsHorizontal - 2;
        object? cache = LabelsField.GetValue(bar.drawer);
        var arguments = new object?[] { pawn, width, cache, GameFont.Tiny };
        // Use the same native truncated text/width helpers; their cache holds strings, not geometry.
        string label = (string)LabelMethod.Invoke(null, arguments)!;
        float nameWidth = (float)WidthMethod.Invoke(null, arguments)!;
        GameFont oldFont = Text.Font;
        double extent;
        try
        {
            Text.Font = GameFont.Tiny;
            extent = Math.Max(Prefs.DisableTinyText ? 16 : 12, Math.Min(100, Text.CalcHeight(label, nameWidth)) - 2);
            if (pawn.Drafted) extent = Math.Max(extent, Text.TinyFontSupported ? 12 : 15);
        }
        finally { Text.Font = oldFont; }
        result = new(pawn, map, source, pawn.Drafted, pawn.Dead, pawn.Downed,
            MoodThresholdExtensions.CurrentMoodThresholdFor(pawn), cache, extent);
        return double.IsFinite(extent) && extent >= 0;
    }

    private static bool Selected(Pawn pawn) => Find.Selector.SelectedObjects.Contains(pawn.Dead ? pawn.Corpse : pawn);
    private static bool WeaponVisible(Pawn pawn) => pawn.equipment?.Primary is { } weapon && weapon.def.IsWeapon &&
        (Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.Always ||
         Prefs.ShowWeaponsUnderPortraitMode == ShowWeaponsUnderPortraitMode.WhileDrafted && pawn.Drafted);
    private static bool TryContext(ColonistBar bar, out Context? context)
    {
        context = null;
        if (!UnityData.IsInMainThread || Event.current == null || Verse.Current.Game == null ||
            !ReferenceEquals(Find.ColonistBar, bar) || Find.UIRoot is not UIRoot_Play root ||
            UI.screenWidth <= 0 || UI.screenHeight <= 0) return false;
        context = new(root, Verse.Current.Game, bar, Find.CurrentMap, WorldRendererUtility.WorldSelected,
            UI.screenWidth, UI.screenHeight, Prefs.UIScale, Prefs.DisableTinyText, Prefs.VisibleMood,
            Prefs.ShowWeaponsUnderPortraitMode, LanguageDatabase.activeLanguage, Find.PlaySettings.showColonistBar);
        return true;
    }

    private sealed record Context(UIRoot Root, Game Game, ColonistBar Bar, Map Map, bool World,
        int Width, int Height, float UiScale, bool DisableTinyText, bool VisibleMood,
        ShowWeaponsUnderPortraitMode Weapons, LoadedLanguage Language, bool ShowBar);
    private sealed record PawnState(Pawn Pawn, Map Map, string Name, bool Drafted, bool Dead,
        bool Downed, MoodThreshold Mood, object? LabelCache, double LabelExtent)
    {
        internal bool IsCurrent(ColonistBar bar) => Pawn.LabelShortCap == Name && Pawn.Drafted == Drafted &&
            Pawn.Dead == Dead && Pawn.Downed == Downed && !Selected(Pawn) && !WeaponVisible(Pawn) &&
            MoodThresholdExtensions.CurrentMoodThresholdFor(Pawn) == Mood &&
            ReferenceEquals(LabelsField.GetValue(bar.drawer), LabelCache);
    }
    private sealed class Snapshot
    {
        private readonly NativeMessageBarGeometry owner;
        private readonly Context context;
        private readonly List<ColonistBar.Entry> entries;
        private readonly List<Vector2> locations;
        private readonly ColonistBar.Entry[] capturedEntries;
        private readonly Vector2[] capturedLocations;
        private readonly PawnState?[] pawns;
        private readonly Vector2 size;
        private readonly float scale;
        private readonly int entryVersion, locationVersion;
        private readonly Func<bool> authority;
        internal Snapshot(NativeMessageBarGeometry owner, Context context,
            List<ColonistBar.Entry> entries, List<Vector2> locations, ColonistBar.Entry[] capturedEntries,
            Vector2[] capturedLocations, PawnState?[] pawns, Vector2 size, float scale, int entryVersion, int locationVersion, Func<bool> authority)
        {
            this.owner = owner; this.context = context; this.entries = entries; this.locations = locations;
            this.capturedEntries = capturedEntries; this.capturedLocations = capturedLocations; this.pawns = pawns;
            this.size = size; this.scale = scale; this.entryVersion = entryVersion; this.locationVersion = locationVersion;
            this.authority = authority;
        }
        internal Context Context => context;
        internal MessageBarBoundsReceipt Bounds = null!;
        private bool Common() => ReferenceEquals(owner.snapshot, this) && authority() &&
            TryContext(context.Bar, out var now) && now == context && !ReorderableWidget.Dragging &&
            !(bool)DirtyField.GetValue(context.Bar)! && ReferenceEquals(EntriesField.GetValue(context.Bar), entries) &&
            ReferenceEquals(context.Bar.DrawLocs, locations) && context.Bar.Size == size && context.Bar.Scale == scale &&
            (int)EntryVersion.GetValue(entries)! == entryVersion && (int)LocationVersion.GetValue(locations)! == locationVersion &&
            entries.Count == capturedEntries.Length && locations.Count == capturedLocations.Length;
        internal bool IsCurrent()
        {
            if (!Common()) return false;
            for (int index = 0; index < entries.Count; index++) if (!EntryCurrent(index)) return false;
            return true;
        }
        private bool EntryCurrent(int index) => ReferenceEquals(entries[index].pawn, capturedEntries[index].pawn) &&
            ReferenceEquals(entries[index].map, capturedEntries[index].map) && entries[index].group == capturedEntries[index].group &&
            locations[index] == capturedLocations[index] && (pawns[index]?.IsCurrent(context.Bar) ?? true);
        internal bool Matches(int index, Rect rect) => Common() && index >= 0 && index < entries.Count && EntryCurrent(index) &&
            rect.x == capturedLocations[index].x && rect.y == capturedLocations[index].y && rect.width == size.x && rect.height == size.y;
    }
    private static FieldInfo Field(Type owner, string name) => owner.GetField(name,
        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) ?? throw new MissingFieldException(owner.FullName, name);
    private static MethodInfo LabelHelper(string name) => typeof(GenMapUI).GetMethod(name,
        BindingFlags.NonPublic | BindingFlags.Static, null,
        new[] { typeof(Pawn), typeof(float), typeof(Dictionary<string, string>), typeof(GameFont) }, null) ??
        throw new MissingMethodException(typeof(GenMapUI).FullName, name);
}
