using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Features.Diagnostics;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.Diagnostics;

// Test-only measurement of the current owned renderer. Draw/Reset/commit delegates are never invoked.
internal sealed class NativeDiagnosticSettingsView
{
    private readonly SettingsRenderer renderer;
    private readonly FieldInfo scrollField;
    private readonly Func<string> pointer;
    private readonly string settingId;
    private readonly float editorOffset;
    private EndToEndNativeWindow window = null!;
    private float viewHeight;
    internal NativeDiagnosticSettingsView(Func<string> pointer, string? settingId = null, float editorOffset = 0)
    {
        this.pointer = pointer;
        this.settingId = settingId ?? DiagnosticSettings.DefinitionContext.Id;
        this.editorOffset = editorOffset;
        renderer = (SettingsRenderer)(typeof(MegamodMod).GetField("renderer", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(LoadedModManager.GetMod<MegamodMod>()) ?? throw new InvalidOperationException("Owned settings renderer is unavailable."));
        scrollField = typeof(SettingsRenderer).GetField("scroll", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Owned settings scroll observation is unavailable.");
        EndToEndAssert.Equal(typeof(Vector2), scrollField.FieldType);
    }
    internal Vector2 Scroll => (Vector2)scrollField.GetValue(renderer)!;
    internal void RestoreScroll(Vector2 prior) => scrollField.SetValue(renderer, prior); // Cleanup only.
    internal void Bind(EndToEndNativeWindow captured, Dialog_ModSettings actual)
    {
        window = captured;
        EndToEndAssert.True(string.IsNullOrEmpty(actual.optionalTitle), "The inspected settings origin requires an untitled dialog.");
        EndToEndAssert.True(window.Width >= 700 && window.Height >= 500, "Settings viewport is too small for this inspected layout.");
    }
    private float ViewportWidth => (float)window.Width - 36;
    private float ViewportHeight => (float)window.Height - 36 - 40 - Window.CloseButSize.y;
    private float ViewWidth => ViewportWidth - 20;

    internal Rect MeasureCheckbox(out bool visible)
    {
        var rows = (IDictionary)(typeof(SettingsRenderer).GetField("rows", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(renderer) ?? throw new InvalidOperationException("Owned row metadata is unavailable."));
        EndToEndAssert.True(rows.Count > 0 && rows.Count <= 2048, "Settings row measurement exceeds its explicit bound.");
        var rowObjects = rows.Values.Cast<object>().ToDictionary(row => Property<SettingsRowLayout>(row, "Layout").Id, StringComparer.Ordinal);
        var ordered = SettingsRowLayout.Capture(rowObjects.Values.Select(row => Property<SettingsRowLayout>(row, "Layout")));
        var font = Text.Font;
        var anchor = Text.Anchor;
        Rect target = default;
        try
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            float y = 0;
            string? category = null;
            foreach (var placement in ordered)
            {
                if (category != placement.CategoryId) { y += 32; category = placement.CategoryId; }
                object row = rowObjects[placement.Id];
                var measure = Property<Func<float, float>?>(row, "MeasureHeight");
                float height = measure?.Invoke(ViewWidth) ?? Property<float>(row, "Height");
                EndToEndAssert.True(float.IsFinite(height) && height > 0 && height <= 4096, "Row measurement is unsupported.");
                if (placement.Id == settingId)
                {
                    EndToEndAssert.True(settingId != DiagnosticSettings.DefinitionContext.Id ||
                        (measure == null && Math.Abs(height - 160) < 0.01f),
                        "The expected ordinary Boolean editor shape changed.");
                    float column = (ViewWidth - 16) / 3;
                    target = new Rect(2 * (column + 8) - Scroll.x, y + 54 + editorOffset - Scroll.y, column, 28);
                }
                y += height;
            }
            viewHeight = y;
        }
        finally { Text.Font = font; Text.Anchor = anchor; }
        visible = target.height == 28 && target.yMin >= 0 && target.yMax <= ViewportHeight &&
            target.xMin >= 0 && target.xMax <= ViewWidth;
        return target;
    }

    // Sibling controls are measured from the same current row; this never draws or commits it.
    internal (Rect UseGlobal, Rect CurrentSave, Rect GlobalDefault) MeasureBooleanControls(out bool visible)
    {
        EndToEndAssert.True(editorOffset == 0, "Ordinary Boolean sibling controls require the native editor offset.");
        var global = MeasureCheckbox(out bool globalVisible);
        var inheritance = new Rect(global.x - 2 * (global.width + 8), global.y - 22, global.width, 28);
        var save = new Rect(global.x - global.width - 8, global.y, global.width, 28);
        bool Fits(Rect rect) => float.IsFinite(rect.x) && float.IsFinite(rect.y) && float.IsFinite(rect.width) &&
            rect.width > 0 && rect.height == 28 && rect.xMin >= 0 && rect.xMax <= ViewWidth &&
            rect.yMin >= 0 && rect.yMax <= ViewportHeight;
        visible = globalVisible && Fits(global) && Fits(inheritance) && Fits(save);
        return (inheritance, save, global);
    }

    internal Vector2 PlanTrackClick(Rect target)
    {
        var handler = typeof(GUI).GetMethod("HandleScrollbars", BindingFlags.Static | BindingFlags.NonPublic, null,
            new[] { typeof(Rect), typeof(Vector2), typeof(Rect), typeof(float), typeof(float) }, null);
        EndToEndAssert.True(typeof(GUI).Assembly.GetName().Name == "UnityStubs" && handler?.ReturnType == typeof(Vector2),
            "The inspected center-on-track-click provider is unavailable.");
        float maximum = viewHeight - ViewportHeight;
        EndToEndAssert.True(maximum > 0 && target.height > 0 && target.height < ViewportHeight && Math.Abs(Scroll.x) < 0.01f,
            "No supported vertical scroll can expose the measured editor.");
        float thumb = Math.Max(20, ViewportHeight * Mathf.Clamp01(ViewportHeight / viewHeight));
        float travel = ViewportHeight - thumb;
        EndToEndAssert.True(travel > 0);
        float top = Scroll.y / maximum * travel;
        float absolute = target.yMin + Scroll.y;
        float desired = Mathf.Clamp(absolute + target.height / 2 - ViewportHeight / 2, 0, maximum);
        foreach (float point in new[] { thumb / 2 + desired / maximum * travel, top - 2, top + thumb + 2 })
        {
            if (point < 1 || point > ViewportHeight - 1 || (point >= top - 1 && point <= top + thumb + 1)) continue;
            float resulting = Mathf.Clamp01((point - thumb / 2) / travel) * maximum;
            if (absolute - resulting >= 1 && absolute - resulting + target.height <= ViewportHeight - 1)
                return new Vector2(ViewportWidth - 8, point);
        }
        throw new InvalidOperationException("No measured outside-thumb native track point exposes the editor.");
    }

    internal IReadOnlyDictionary<string, string> Evidence(Rect target, bool visible) => new Dictionary<string, string>
    {
        ["window"] = $"{window.X},{window.Y} {window.Width}x{window.Height}; scale={window.UiScale}",
        ["contentOrigin"] = "window + (18,58); native mod dialog header and margin",
        ["editor"] = target.ToString(), ["visible"] = visible.ToString(), ["scroll"] = Scroll.ToString(),
        ["viewportHeight"] = ViewportHeight.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["measuredViewHeight"] = viewHeight.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
    internal NativeWindowClickActionStep Click(string name, Vector2 point) => new(name, window.Handle,
        (window.X + 18 + point.x) * window.UiScale, (window.Y + 58 + point.y) * window.UiScale, null, pointer());
    private static T Property<T>(object row, string name) => (T)(row.GetType().GetProperty(name)
        ?? throw new InvalidOperationException("Owned row metadata changed: " + name)).GetValue(row)!;
}
