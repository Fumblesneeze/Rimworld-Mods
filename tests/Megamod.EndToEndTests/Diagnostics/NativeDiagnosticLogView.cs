using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using LudeonTK;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.Diagnostics;

// Measured controls for the inspected 1.7 native log. Never changes selection or scrolling directly.
internal sealed class NativeDiagnosticLogView
{
    private readonly Func<string> pointerScope;
    private EndToEndNativeWindow window = null!;
    private EditWindow_Log ownedLog = null!;
    private float measuredListingHeight;
    internal NativeDiagnosticLogView(Func<string> pointerScope) => this.pointerScope = pointerScope;
    internal void Bind(EndToEndNativeWindow window, EditWindow_Log actual)
    {
        this.window = window;
        ownedLog = actual;
    }

    internal NativeWindowClickActionStep ToolbarClick(string name, string label)
    {
        var font = Text.Font;
        try
        {
            Text.Font = GameFont.Tiny;
            float x = 0;
            foreach (string candidate in new[] { "Clear", "Trace big", "Trace medium", "Trace small",
                         Read<bool>("canAutoOpen") ? "Auto-open is ON" : "Auto-open is OFF", "Copy to clipboard" })
            {
                float width = Text.CalcSize(candidate).x + 10;
                if (candidate == label)
                {
                    EndToEndAssert.True(x >= 0 && x + width <= ContentWidth && ContentHeight > 24,
                        "The inspected native toolbar control is clipped; inspect the preceding window screenshot.");
                    return Click(name, new Vector2(x + width / 2, 12));
                }
                x += width + 4;
            }
        }
        finally { Text.Font = font; }
        throw new InvalidOperationException("Unsupported native toolbar control.");
    }

    internal Rect MeasureRow(LogMessage target, out bool visible)
    {
        visible = false;
        EndToEndAssert.Equal("Debug log", ownedLog!.optionalTitle, "The inspected native content origin requires this title.");
        float width = ContentWidth - 16 - 28;
        EndToEndAssert.True(width > 0 && ContentHeight > 180, "Native log viewport is too small.");
        Vector2 scroll = Read<Vector2>("messagesScrollPosition");
        float listingBottom = ContentHeight - (Selected == null ? 0 : Read<float>("detailsPaneHeight"));
        var font = Text.Font;
        try
        {
            Text.Font = GameFont.Tiny;
            float y = 0;
            Rect targetRow = default;
            foreach (var message in Log.Messages)
            {
                if ((message.type == LogMessageType.Message && !Read<bool>("showMessages"))
                    || (message.type == LogMessageType.Warning && !Read<bool>("showWarnings"))
                    || (message.type == LogMessageType.Error && !Read<bool>("showErrors"))) continue;
                string preview = message.ToString();
                if (preview.Length > 1000) preview = preview.Substring(0, 1000);
                float height = Math.Min(Text.TinyFontSupported ? 30 : Text.LineHeight, Text.CalcHeight(preview, width));
                var row = new Rect(28 - scroll.x, 26 + y - scroll.y, width, height);
                if (ReferenceEquals(message, target))
                {
                    visible = row.yMin >= 26 && row.yMax <= listingBottom && row.xMin >= 0 && row.xMax <= ContentWidth - 16;
                    targetRow = row;
                }
                y += height;
            }
            measuredListingHeight = y;
            return targetRow;
        }
        finally { Text.Font = font; }
    }

    internal Vector2 PlanNativeTrackClick(Rect row)
    {
        // 1.7 UnityStubs has a center-on-click track, not Unity's old page buttons or arrows.
        // Read-only shape/layout prerequisites plus observed movement and final selected identity guard this scenario.
        var handler = typeof(GUI).GetMethod("HandleScrollbars", BindingFlags.Static | BindingFlags.NonPublic, null,
            new[] { typeof(Rect), typeof(Vector2), typeof(Rect), typeof(float), typeof(float) }, null);
        EndToEndAssert.True(typeof(GUI).Assembly.GetName().Name == "UnityStubs" && handler?.ReturnType == typeof(Vector2),
            "The inspected native scrollbar provider/callable shape is unavailable; no coordinate scroll will be attempted.");
        var listingField = typeof(EditWindow_Log).GetField("listingViewHeight", BindingFlags.Instance | BindingFlags.NonPublic);
        EndToEndAssert.True(listingField?.FieldType == typeof(float), "Native listing height is unavailable.");
        float nativeListingHeight = (float)listingField!.GetValue(ownedLog)!;
        EndToEndAssert.True(Math.Abs(nativeListingHeight - measuredListingHeight) < 0.5f,
            "Measured rows differ from the last rendered native layout; inspect the captured viewport before retrying.");
        Vector2 scroll = Read<Vector2>("messagesScrollPosition");
        float bottom = ContentHeight - (Selected == null ? 0 : Read<float>("detailsPaneHeight"));
        float viewportHeight = bottom - 26;
        float viewHeight = nativeListingHeight + 100;
        float maximum = viewHeight - viewportHeight;
        EndToEndAssert.True(row.height > 0 && row.height < viewportHeight && maximum > 0 && Math.Abs(scroll.x) < 0.01f,
            "The target is filtered/missing or a supported vertical scrollbar is not available.");
        float thumb = Math.Max(20, viewportHeight * Mathf.Clamp01(viewportHeight / viewHeight));
        float travel = viewportHeight - thumb;
        EndToEndAssert.True(travel > 0, "Native scrollbar has no usable travel.");
        float thumbTop = 26 + scroll.y / maximum * travel;
        float absoluteTop = row.yMin - 26 + scroll.y;
        float desiredScroll = Mathf.Clamp(absoluteTop + row.height / 2 - viewportHeight / 2, 0, maximum);
        float desiredPoint = 26 + thumb / 2 + desiredScroll / maximum * travel;
        foreach (float point in new[] { desiredPoint, thumbTop - 2, thumbTop + thumb + 2 })
        {
            if (point < 27 || point > bottom - 1 || (point >= thumbTop - 1 && point <= thumbTop + thumb + 1)) continue;
            float resultingScroll = Mathf.Clamp01((point - thumb / 2 - 26) / travel) * maximum;
            float resultingTop = 26 + absoluteTop - resultingScroll;
            if (resultingTop >= 27 && resultingTop + row.height <= bottom - 1)
                return new Vector2(ContentWidth - 8, point);
        }
        throw new InvalidOperationException("No observed outside-thumb track point exposes the whole row. Native dragging/scrolling support is required; no direct scroll assignment was made.");
    }

    internal IReadOnlyDictionary<string, string> GeometryEvidence(Rect row, bool visible) => new Dictionary<string, string>
    {
        ["window"] = $"{window.X},{window.Y} {window.Width}x{window.Height}; scale={window.UiScale}",
        ["contentOrigin"] = "window + (8,41), from inspected EditWindow.Margin and Window optional-title layout",
        ["rowInContent"] = row.ToString(), ["whollyVisible"] = visible.ToString(),
        ["messagesScroll"] = Read<Vector2>("messagesScrollPosition").ToString(),
        ["measuredListingHeight"] = measuredListingHeight.ToString(CultureInfo.InvariantCulture),
        ["detailsHeight"] = Read<float>("detailsPaneHeight").ToString(CultureInfo.InvariantCulture),
        ["showWarnings"] = Read<bool>("showWarnings").ToString(), ["showErrors"] = Read<bool>("showErrors").ToString()
    };

    internal NativeWindowClickActionStep Click(string name, Vector2 contentPoint) => new(name, window.Handle,
        (window.X + 8 + contentPoint.x) * window.UiScale, (window.Y + 41 + contentPoint.y) * window.UiScale, null, pointerScope());
    internal float ContentWidth => (float)window.Width - 16;
    internal float ContentHeight => (float)window.Height - 49;
    internal static LogMessage? Selected => (LogMessage?)RequiredField("selectedMessage", typeof(LogMessage)).GetValue(null);
    internal static T Read<T>(string name) => (T)RequiredField(name, typeof(T)).GetValue(null)!;
    private static FieldInfo RequiredField(string name, Type type)
    {
        var field = typeof(EditWindow_Log).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
        if (field == null || field.FieldType != type) throw new InvalidOperationException("Native log observation shape changed: " + name);
        return field;
    }

}
