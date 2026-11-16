using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Megamod.Features.SettingsSearch.Core;
using UnityEngine;
using UnityStubs.Gui;

namespace Megamod.Features.SettingsSearch.Runtime;

internal sealed record SettingsForeignScrollView(int NativeId, SettingsCaptureRect Viewport,
    Vector2 Position, Vector2 Maximum, Vector2 ScreenScale);

// Exact inspected native pending-scroll route. It changes the original viewport on its next draw.
internal static class SettingsForeignScroll
{
    private static readonly Type? State = typeof(GUI).GetNestedType("ScrollViewState", BindingFlags.NonPublic);
    private static readonly FieldInfo? States = Field(typeof(GUI), "_scrollViewStates", State == null ? null : typeof(List<>).MakeGenericType(State), true);
    private static readonly FieldInfo? Depth = Field(typeof(GUI), "_scrollViewDepth", typeof(int), true);
    private static readonly FieldInfo? Pending = Field(typeof(GUI), "_pendingScroll", typeof(Dictionary<int, Vector2>), true);
    private static readonly FieldInfo? Id = Field(State, "id", typeof(int), false);
    private static readonly FieldInfo? Position = Field(State, "scrollPosition", typeof(Vector2), false);
    private static readonly FieldInfo? MaxX = Field(State, "maxX", typeof(float), false);
    private static readonly FieldInfo? MaxY = Field(State, "maxY", typeof(float), false);

    internal static bool Available => States != null && Depth != null && Pending != null && Id != null &&
        Position != null && MaxX != null && MaxY != null;

    internal static SettingsForeignScrollView Current(Rect viewport, Rect content)
    {
        if (!Available || States!.GetValue(null) is not IList states || Depth!.GetValue(null) is not int depth ||
            depth <= 0 || depth > states.Count || states[depth - 1] is not { } state)
            throw new InvalidOperationException("settings-foreign-scroll-state-unavailable");
        var position = (Vector2)Position!.GetValue(state)!;
        var maximum = new Vector2((float)MaxX!.GetValue(state)!, (float)MaxY!.GetValue(state)!);
        // BeginScrollView has already pushed its content transform when this postfix observes it.
        GuiBackend.Current.GetGroup(GuiBackend.Current.GroupCount - 1, out Rect clip, out _);
        var bounds = ScreenBounds(new Rect(content.x + position.x, content.y + position.y, clip.width, clip.height));
        var origin = GuiBackend.Current.GuiToScreenPoint(Vector2.zero);
        var unit = GuiBackend.Current.GuiToScreenPoint(Vector2.one) - origin;
        var x = GuiBackend.Current.GuiToScreenPoint(Vector2.right) - origin;
        var y = GuiBackend.Current.GuiToScreenPoint(Vector2.up) - origin;
        if (!SettingsForeignControls.Valid(bounds) || !float.IsFinite(unit.x) || !float.IsFinite(unit.y) ||
            unit.x <= 0 || unit.y <= 0 || Math.Abs(x.y) > 0.001f || Math.Abs(y.x) > 0.001f ||
            !float.IsFinite(maximum.x) || !float.IsFinite(maximum.y) || maximum.x < 0 || maximum.y < 0)
            throw new InvalidOperationException("settings-foreign-scroll-transform-unavailable");
        return new((int)Id!.GetValue(state)!, bounds, position, maximum, unit);
    }

    internal static SettingsCaptureRect ScreenBounds(Rect rect)
    {
        var backend = GuiBackend.Current;
        var a = backend.GuiToScreenPoint(new Vector2(rect.xMin, rect.yMin));
        var b = backend.GuiToScreenPoint(new Vector2(rect.xMax, rect.yMin));
        var c = backend.GuiToScreenPoint(new Vector2(rect.xMin, rect.yMax));
        var d = backend.GuiToScreenPoint(new Vector2(rect.xMax, rect.yMax));
        float left = Math.Min(Math.Min(a.x, b.x), Math.Min(c.x, d.x));
        float top = Math.Min(Math.Min(a.y, b.y), Math.Min(c.y, d.y));
        return new(left, top, Math.Max(Math.Max(a.x, b.x), Math.Max(c.x, d.x)) - left,
            Math.Max(Math.Max(a.y, b.y), Math.Max(c.y, d.y)) - top);
    }

    internal static void Schedule(int id, Vector2 position)
    {
        if (SettingsReadScope.Current != null || Pending?.GetValue(null) is not Dictionary<int, Vector2> pending)
            throw new InvalidOperationException("settings-foreign-scroll-unavailable");
        pending[id] = position;
    }

    internal static void RemoveOwned(int id, Vector2 position)
    {
        if (Pending?.GetValue(null) is Dictionary<int, Vector2> pending &&
            pending.TryGetValue(id, out var current) && current == position) pending.Remove(id);
    }

    private static FieldInfo? Field(Type? type, string name, Type? expected, bool isStatic)
    {
        var field = type?.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic |
            (isStatic ? BindingFlags.Static : BindingFlags.Instance));
        return field != null && field.FieldType == expected && field.IsStatic == isStatic ? field : null;
    }
}
