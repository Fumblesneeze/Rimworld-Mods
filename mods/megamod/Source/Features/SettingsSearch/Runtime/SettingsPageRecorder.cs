using System;
using System.Collections.Generic;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using UnityEngine;
using UnityStubs.Gui;
using Waylith.Rendering;

namespace Megamod.Features.SettingsSearch.Runtime;

// A passive recorder for one complete, ordinary page draw. This does not perform hidden drawing,
// select tabs, resolve controls, or assert that the page's custom/conditional UI was fully read.
public sealed class SettingsPageRecorder
{
    [ThreadStatic] private static RecordingBackend? active;
    private readonly SettingsPage page;
    private readonly string pathIdentity;
    private readonly Action? observedDraw;

    public SettingsPageRecorder(SettingsPage page, string pathIdentity, Action? observedDraw = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(pathIdentity);
        this.page = page;
        this.pathIdentity = pathIdentity;
        this.observedDraw = observedDraw;
    }

    // The latest completed repaint only. Callers own language/page/lifecycle invalidation.
    public SettingsCaptureSnapshot? LastCapture { get; private set; }

    public void Observe(Action draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        if (Event.current?.type != EventType.Repaint)
        {
            draw();
            return;
        }

        var buffer = new SettingsCaptureBuffer(page, pathIdentity);
        var previous = GuiBackend.Current;
        var parent = active;
        var recording = new RecordingBackend(previous, buffer, observedDraw);
        bool callbackThrew = false;
        GuiBackend.Current = recording;
        active = recording;
        try
        {
            draw();
        }
        catch
        {
            callbackThrew = true;
            throw;
        }
        finally
        {
            bool replaced = !ReferenceEquals(GuiBackend.Current, recording);
            if (!replaced) GuiBackend.Current = previous;
            active = parent;
            // Preserve the callback's failure even when it also replaced the backend. Never
            // restore over a foreign replacement, and never publish the partial failed draw.
            LastCapture = callbackThrew ? buffer.Fail(SettingsCaptureFailure.CallbackThrew)
                : replaced ? buffer.Fail(SettingsCaptureFailure.BackendReplaced)
                : recording.Failed ? buffer.Fail(SettingsCaptureFailure.RecordingFailed)
                : buffer.Complete();
        }
    }

    // The caller supplies a real TipRegion's literal text; no lazy tooltip getter is invoked.
    public static void ObserveTooltip(Rect rect, string? literalText) =>
        active?.Record(SettingsCaptureKind.Tooltip, rect, literalText);

    private sealed class RecordingBackend : IGuiBackend
    {
        private readonly IGuiBackend inner;
        private readonly SettingsCaptureBuffer buffer;
        private readonly Action? observedDraw;

        internal RecordingBackend(IGuiBackend inner, SettingsCaptureBuffer buffer, Action? observedDraw)
        {
            this.inner = inner;
            this.buffer = buffer;
            this.observedDraw = observedDraw;
        }

        internal bool Failed { get; private set; }

        private bool CanRecord => !Failed && ReferenceEquals(active, this) &&
            ReferenceEquals(GuiBackend.Current, this) && Event.current?.type == EventType.Repaint;

        private void ObserveDraw()
        {
            if (!CanRecord) return;
            try { observedDraw?.Invoke(); }
            catch { Failed = true; }
        }

        internal void Record(SettingsCaptureKind kind, Rect rect, string? text)
        {
            if (!CanRecord || string.IsNullOrEmpty(text)) return;
            var bounds = Bounds(rect);
            if (bounds.HasValue) Add(kind, text, bounds.Value);
        }

        private SettingsCaptureRect? Bounds(Rect rect)
        {
            if (!float.IsFinite(rect.x) || !float.IsFinite(rect.y) || !float.IsFinite(rect.width) ||
                !float.IsFinite(rect.height) || rect.width <= 0 || rect.height <= 0) return null;
            try
            {
                // All four corners preserve rotated/sheared screen envelopes. Clipping and
                // control identity remain separate work; offscreen text is still an observation.
                var a = inner.GuiToScreenPoint(new Vector2(rect.xMin, rect.yMin));
                var b = inner.GuiToScreenPoint(new Vector2(rect.xMax, rect.yMin));
                var c = inner.GuiToScreenPoint(new Vector2(rect.xMin, rect.yMax));
                var d = inner.GuiToScreenPoint(new Vector2(rect.xMax, rect.yMax));
                float x = Math.Min(Math.Min(a.x, b.x), Math.Min(c.x, d.x));
                float y = Math.Min(Math.Min(a.y, b.y), Math.Min(c.y, d.y));
                return new SettingsCaptureRect(x, y,
                    Math.Max(Math.Max(a.x, b.x), Math.Max(c.x, d.x)) - x,
                    Math.Max(Math.Max(a.y, b.y), Math.Max(c.y, d.y)) - y);
            }
            catch
            {
                Failed = true;
                return null;
            }
        }

        private void Add(SettingsCaptureKind kind, string text, SettingsCaptureRect bounds)
        {
            try { buffer.Add(kind, text, bounds); }
            catch { Failed = true; }
        }

        public void DrawText(Rect rect, string text, int fontIndex, float fontSize,
            FontStyle fontStyle, TextAnchor anchor, bool wordWrap, Color color,
            Rect clipRect = default, bool richText = true)
        {
            // Measure before the real draw, but retain only a successfully returned draw.
            var bounds = CanRecord && !string.IsNullOrEmpty(text) ? Bounds(rect) : null;
            inner.DrawText(rect, text, fontIndex, fontSize, fontStyle, anchor, wordWrap, color, clipRect, richText);
            ObserveDraw();
            if (bounds.HasValue && CanRecord) Add(SettingsCaptureKind.Text, text, bounds.Value);
        }

        // Nested recorders wrap this backend. Only the active scope records, while every layer
        // forwards unchanged, so an inner page cannot contribute observations to its parent.
        public bool MouseOutsideHitClip => inner.MouseOutsideHitClip;
        public int GroupCount => inner.GroupCount;
        public bool InlineControlsObscured => inner.InlineControlsObscured;
        public Vector2 MeasureText(int fontIndex, float fontSize, FontStyle fontStyle, string text,
            float wrapWidth, bool richText = true) =>
            inner.MeasureText(fontIndex, fontSize, fontStyle, text, wrapWidth, richText);
        public float LineSpacing(int fontIndex, float fontSize) => inner.LineSpacing(fontIndex, fontSize);
        public void FontMetrics(int fontIndex, out float importSize, out float ascent, out float lineSpacing) =>
            inner.FontMetrics(fontIndex, out importSize, out ascent, out lineSpacing);
        public int RegisterFont(string name, byte[] fontData, float importSize, int renderingMode) =>
            inner.RegisterFont(name, fontData, importSize, renderingMode);
        public int TextIndexAt(int fontIndex, float fontSize, FontStyle fontStyle, string text,
            float wrapWidth, Vector2 local, bool richText = true) =>
            inner.TextIndexAt(fontIndex, fontSize, fontStyle, text, wrapWidth, local, richText);
        public Vector2 TextCursorPos(int fontIndex, float fontSize, FontStyle fontStyle, string text,
            float wrapWidth, int index, out float lineHeight, bool richText = true) =>
            inner.TextCursorPos(fontIndex, fontSize, fontStyle, text, wrapWidth, index, out lineHeight, richText);
        public void TextSelectionRects(int fontIndex, float fontSize, FontStyle fontStyle, string text,
            float wrapWidth, int from, int to, List<Rect> rects, bool richText = true) =>
            inner.TextSelectionRects(fontIndex, fontSize, fontStyle, text, wrapWidth, from, to, rects, richText);
        public void DrawTexture(Rect rect, UnityEngine.Texture image, ScaleMode scaleMode, Color color)
        { inner.DrawTexture(rect, image, scaleMode, color); ObserveDraw(); }
        public void DrawTextureWithTexCoords(Rect rect, UnityEngine.Texture image, Rect texCoords, Color color)
        { inner.DrawTextureWithTexCoords(rect, image, texCoords, color); ObserveDraw(); }
        public void DrawTextureWithMaterial(Rect rect, UnityEngine.Texture image, Rect texCoords, Color color,
            UnityEngine.Material material, Waylith.Rendering.Material passMaterial)
        { inner.DrawTextureWithMaterial(rect, image, texCoords, color, material, passMaterial); ObserveDraw(); }
        public void FillRect(Rect rect, Color color) { inner.FillRect(rect, color); ObserveDraw(); }
        public void FillRectRounded(Rect rect, Color color, float rounding) { inner.FillRectRounded(rect, color, rounding); ObserveDraw(); }
        public void DrawTextureQuad(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, UnityEngine.Texture image, Color color)
        { inner.DrawTextureQuad(p0, p1, p2, p3, image, color); ObserveDraw(); }
        public void DrawPrimitives(int mode, ReadOnlySpan<GuiVertex> vertices, UnityEngine.Texture texture,
            bool screenSpace) { inner.DrawPrimitives(mode, vertices, texture, screenSpace); ObserveDraw(); }
        public void SetMatrix(Matrix4x4 matrix) => inner.SetMatrix(matrix);
        public void PushGroup(Rect rect) => inner.PushGroup(rect);
        public void PushScrollView(Rect clipRect, Vector2 contentOffset) => inner.PushScrollView(clipRect, contentOffset);
        public void PopGroup() => inner.PopGroup();
        public void GetGroup(int index, out Rect rect, out Vector2 contentOrigin) =>
            inner.GetGroup(index, out rect, out contentOrigin);
        public void BeginWindowContent(Rect windowRect) => inner.BeginWindowContent(windowRect);
        public void EndWindowContent() => inner.EndWindowContent();
        public void PushClipRect(Rect rect) => inner.PushClipRect(rect);
        public void PopClipRect() => inner.PopClipRect();
        public void ActiveRenderTextureChanged(UnityEngine.RenderTexture texture) => inner.ActiveRenderTextureChanged(texture);
        public void SetTextLanguage(string languageFolderName) => inner.SetTextLanguage(languageFolderName);
        public Vector2 ScreenToGuiPoint(Vector2 point) => inner.ScreenToGuiPoint(point);
        public Vector2 GuiToScreenPoint(Vector2 point) => inner.GuiToScreenPoint(point);
        public Vector2 GuiToDevicePoint(Vector2 point) => inner.GuiToDevicePoint(point);
    }
}
