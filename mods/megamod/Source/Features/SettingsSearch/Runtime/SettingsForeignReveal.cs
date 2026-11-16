using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using UnityEngine;
using UnityStubs.Gui;

namespace Megamod.Features.SettingsSearch.Runtime;

// Uses only the exact page, observed navigation callbacks and this repaint's native control/scroll bounds.
public sealed class SettingsForeignReveal
{
    private readonly SettingsForeignRecorder recorder;
    private readonly Dictionary<int, Vector2> ownedScroll = new();
    private SettingsForeignTarget? target;
    private SettingsForeignFrame? previous;
    private readonly SettingsForeignRevealLifetime lifetime = new();
    private bool needsLayout;
    public SettingsForeignReveal(SettingsForeignRecorder recorder) => this.recorder = recorder;
    public SettingsForeignRevealStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public bool NeedsLayout => needsLayout && target != null &&
        Status is SettingsForeignRevealStatus.Pending or SettingsForeignRevealStatus.Highlighting;

    public void Request(SettingsForeignTarget target)
    {
        Cancel();
        this.target = target with { Tabs = Array.AsReadOnly(target.Tabs.ToArray()) };
        Status = SettingsForeignRevealStatus.Pending;
        FailureReason = null;
        lifetime.Start();
        if (!target.CanReveal) Refuse("settings-foreign-tab-navigation-incomplete");
    }

    public SettingsForeignFrame ObserveVisible(SettingsPage page, Action draw)
    {
        bool hidden = SettingsReadScope.Current != null;
        var input = Event.current;
        if (!hidden && (input?.type is EventType.ScrollWheel or EventType.MouseDown or EventType.KeyDown)) Cancel();
        SettingsForeignFrame frame;
        try { frame = recorder.Observe(page, draw); }
        catch { if (!hidden) Cancel(); throw; }
        if (hidden) return frame;
        if (input?.type != EventType.Repaint) return previous ?? frame;
        previous = frame;
        RemovePending(); // Native BeginScrollView already consumed applicable entries on this draw.
        if (target == null || Status is not (SettingsForeignRevealStatus.Pending or SettingsForeignRevealStatus.Highlighting)) return frame;
        if (!ReferenceEquals(page, target.Page)) { Cancel(); return frame; }
        if (frame.FailureReason != null) { Refuse(frame.FailureReason); return frame; }
        double now = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        var timed = lifetime.NextRepaint(now);
        if (timed == SettingsForeignRevealStatus.Unavailable) { Refuse("settings-foreign-reveal-limit"); return frame; }
        if (timed == SettingsForeignRevealStatus.Expired)
        { Status = SettingsForeignRevealStatus.Expired; target = null; return frame; }

        foreach (var step in target.Tabs)
        {
            var tabs = frame.Tabs.Where(tab => tab.Identity == step).ToArray();
            if (tabs.Length != 1 || tabs[0].Select == null) { Refuse("settings-foreign-tab-missing"); return frame; }
            if (tabs[0].Selected) continue;
            try { tabs[0].Select!(); }
            catch (Exception) { Refuse("settings-foreign-tab-callback-failed"); }
            return frame;
        }
        var option = SettingsForeignControls.Resolve(target, page, frame.Options, out string? failure);
        if (option == null)
        {
            if (failure == "settings-foreign-control-missing") needsLayout = true;
            else Refuse(failure!);
            return frame;
        }
        needsLayout = false;
        try
        {
            if (ScheduleScroll(frame, option)) return frame;
            var visibleBounds = option.Bounds;
            foreach (int index in option.Scrolls) visibleBounds = Intersection(visibleBounds, frame.Scrolls[index].Viewport);
            if (!SettingsForeignControls.Valid(visibleBounds)) { Refuse("settings-foreign-control-not-visible"); return frame; }
            // Start only after the current native draw actually shows the resolved row.
            Highlight(visibleBounds);
            lifetime.Visible(now);
            Status = SettingsForeignRevealStatus.Highlighting;
        }
        catch (Exception) { Refuse("settings-foreign-scroll-unavailable"); }
        return frame;
    }

    // The caller invokes this only after the native root callback, never inside page/listing drawing.
    public SettingsNeutralReadStatus ReadLayout(SettingsNeutralReader reader, SettingsPage page, Action draw)
    {
        if (!NeedsLayout || previous == null || target == null || !ReferenceEquals(target.Page, page))
            return SettingsNeutralReadStatus.Unavailable;
        var requested = target;
        var visible = previous;
        SettingsForeignFrame? hidden = null;
        var result = reader.Read(page, "foreign-reveal-layout", () => hidden = recorder.Observe(page, draw), out _, out string? reason);
        if (!ReferenceEquals(target, requested) || result == SettingsNeutralReadStatus.Deferred) return result;
        needsLayout = false;
        if (result != SettingsNeutralReadStatus.Captured || hidden == null || hidden.FailureReason != null)
        { Refuse(hidden?.FailureReason ?? reason ?? "settings-foreign-layout-unavailable"); return result; }
        var option = SettingsForeignControls.Resolve(requested, page, hidden.Options, out var failure);
        if (option == null) { Refuse(failure!); return SettingsNeutralReadStatus.Captured; }
        // Hidden GUI state owns different native IDs and an independent origin. Require the same
        // complete scroll sequence and geometry before mapping to the just-observed visible views.
        if (hidden.Scrolls.Count != visible.Scrolls.Count || !hidden.SelectedTabs.SequenceEqual(visible.SelectedTabs) ||
            option.Scrolls.Count == 0 || hidden.Scrolls.Where((scroll, index) => !SameLayout(scroll, visible.Scrolls[index])).Any())
        { Refuse("settings-foreign-layout-changed"); return SettingsNeutralReadStatus.Captured; }
        int inner = option.Scrolls[option.Scrolls.Count - 1];
        var from = hidden.Scrolls[inner];
        var to = visible.Scrolls[inner];
        var mapped = option with { Bounds = option.Bounds with
        {
            X = to.Viewport.X + (option.Bounds.X - from.Viewport.X) / from.ScreenScale.x * to.ScreenScale.x,
            Y = to.Viewport.Y + (option.Bounds.Y - from.Viewport.Y) / from.ScreenScale.y * to.ScreenScale.y,
            Width = option.Bounds.Width / from.ScreenScale.x * to.ScreenScale.x,
            Height = option.Bounds.Height / from.ScreenScale.y * to.ScreenScale.y
        } };
        try
        {
            if (!ScheduleScroll(visible, mapped)) Refuse("settings-foreign-control-not-visible");
        }
        catch (Exception) { Refuse("settings-foreign-scroll-unavailable"); }
        return SettingsNeutralReadStatus.Captured;
    }

    private bool ScheduleScroll(SettingsForeignFrame frame, SettingsForeignOption option)
    {
        var bounds = option.Bounds;
        bool scheduled = false;
        for (int index = option.Scrolls.Count - 1; index >= 0; index--)
        {
            var view = frame.Scrolls[option.Scrolls[index]];
            var position = new Vector2(
                Math.Clamp(view.Position.x + Shift(bounds.X, bounds.Width, view.Viewport.X, view.Viewport.Width) / view.ScreenScale.x, 0, view.Maximum.x),
                Math.Clamp(view.Position.y + Shift(bounds.Y, bounds.Height, view.Viewport.Y, view.Viewport.Height) / view.ScreenScale.y, 0, view.Maximum.y));
            var delta = position - view.Position;
            if (Math.Abs(delta.x) <= 0.5f && Math.Abs(delta.y) <= 0.5f) continue;
            SettingsForeignScroll.Schedule(view.NativeId, position);
            ownedScroll[view.NativeId] = position;
            bounds = bounds with { X = bounds.X - delta.x * view.ScreenScale.x, Y = bounds.Y - delta.y * view.ScreenScale.y };
            scheduled = true;
        }
        return scheduled;
    }

    private static bool SameLayout(SettingsForeignScrollView a, SettingsForeignScrollView b) =>
        a.Position == b.Position && a.Maximum == b.Maximum &&
        Math.Abs(a.Viewport.Width / a.ScreenScale.x - b.Viewport.Width / b.ScreenScale.x) <= 0.01f &&
        Math.Abs(a.Viewport.Height / a.ScreenScale.y - b.Viewport.Height / b.ScreenScale.y) <= 0.01f;

    public void Cancel()
    {
        RemovePending();
        target = null;
        lifetime.Start();
        needsLayout = false;
        FailureReason = null;
        Status = SettingsForeignRevealStatus.Cancelled;
    }
    private void Refuse(string reason)
    { Cancel(); FailureReason = reason; Status = SettingsForeignRevealStatus.Unavailable; }
    private void RemovePending()
    {
        foreach (var pair in ownedScroll) SettingsForeignScroll.RemoveOwned(pair.Key, pair.Value);
        ownedScroll.Clear();
    }
    private static float Shift(float start, float length, float viewportStart, float viewportLength) =>
        length > viewportLength || start < viewportStart ? start - viewportStart :
        start + length > viewportStart + viewportLength ? start + length - viewportStart - viewportLength : 0;
    private static SettingsCaptureRect Intersection(SettingsCaptureRect a, SettingsCaptureRect b)
    {
        float x = Math.Max(a.X, b.X), y = Math.Max(a.Y, b.Y);
        return new(x, y, Math.Min(a.X + a.Width, b.X + b.Width) - x, Math.Min(a.Y + a.Height, b.Y + b.Height) - y);
    }
    private static void Highlight(SettingsCaptureRect screen)
    {
        var backend = GuiBackend.Current;
        var a = backend.ScreenToGuiPoint(new Vector2(screen.X, screen.Y));
        var b = backend.ScreenToGuiPoint(new Vector2(screen.X + screen.Width, screen.Y + screen.Height));
        var rect = new Rect(a.x, a.y, b.x - a.x, b.y - a.y);
        backend.FillRect(rect, new Color(0.8f, 0.7f, 0.2f, 0.18f));
        var outline = new Color(0.9f, 0.8f, 0.3f, 0.9f);
        backend.FillRect(new Rect(rect.x, rect.y, rect.width, 1), outline);
        backend.FillRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), outline);
        backend.FillRect(new Rect(rect.x, rect.y, 1, rect.height), outline);
        backend.FillRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), outline);
    }
}
