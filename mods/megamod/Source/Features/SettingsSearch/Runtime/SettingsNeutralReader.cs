using System;
using System.Collections.Generic;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

public enum SettingsNeutralReadStatus { Captured, Deferred, Unavailable, Failed }

// Call after the visible-draw delegate and all nested native widget/layout calls have returned.
// The enclosing Window callback may still be active. This component neither discovers pages
// nor registers automatic production callbacks. Arbitrary foreign effects are not transactional.
public sealed class SettingsNeutralReader : IDisposable
{
    private readonly int thread = Environment.CurrentManagedThreadId;
    private readonly Action<string> warning;
    private SettingsReadInputAdmission? input;
    private SettingsReadActionGuards? actions;
    private SettingsReadControlGuards? controls;
    private SettingsReadListingGuards? caches;
    private SettingsReadListingBoundsGuard? bounds;
    private bool attempted, initialized, disposed, reading;

    public SettingsNeutralReader(Action<string> warning) => this.warning = warning ?? throw new ArgumentNullException(nameof(warning));

    public SettingsNeutralReadStatus Read(SettingsPage page, string pathIdentity, Action draw,
        out SettingsCaptureSnapshot? capture, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(pathIdentity);
        ArgumentNullException.ThrowIfNull(draw);
        capture = null;
        reason = null;
        if (Environment.CurrentManagedThreadId != thread || !UnityData.IsInMainThread)
            return Result(SettingsNeutralReadStatus.Unavailable, "settings-reader-gui-thread-required", out reason);
        if (SettingsReadScope.Current is { } parent)
        {
            parent.Refuse("nested-settings-read");
            return Result(SettingsNeutralReadStatus.Failed, "nested-settings-read", out reason);
        }
        if (disposed)
            return Result(SettingsNeutralReadStatus.Unavailable, "settings-reader-disposed", out reason);
        if (Event.current?.type != EventType.Repaint || Event.current.rawType != EventType.Repaint)
            return Result(SettingsNeutralReadStatus.Deferred, "settings-input-not-repaint", out reason);
        try
        {
            input ??= new SettingsReadInputAdmission();
            var status = input.Check(out reason);
            if (status != SettingsReadInputStatus.Idle)
                return status == SettingsReadInputStatus.Busy ? SettingsNeutralReadStatus.Deferred : SettingsNeutralReadStatus.Unavailable;
            if (!Initialize() || !Available)
                return Result(SettingsNeutralReadStatus.Unavailable, "settings-reader-prerequisites-unavailable", out reason);
        }
        catch (Exception)
        {
            return Result(SettingsNeutralReadStatus.Unavailable, "settings-reader-admission-failed", out reason);
        }

        var previousBackend = GuiBackend.Current;
        var previousEvent = Event.current;
        SettingsGuiStateScope? state = null;
        SettingsReadScope? scope = null;
        SettingsHiddenBackend? hidden = null;
        Event? neutral = null;
        SettingsCaptureSnapshot? candidate = null;
        reading = true;
        try
        {
            state = SettingsGuiStateScope.Capture();
            hidden = new SettingsHiddenBackend(previousBackend);
            neutral = new Event { type = EventType.Repaint, rawType = EventType.Repaint,
                mousePosition = new Vector2(-1000000, -1000000) };
            GuiBackend.Current = hidden;
            Event.current = neutral;
            state.Isolate();
            GUI.matrix = Matrix4x4.identity;
            GUI.enabled = false;
            scope = actions!.BeginRead();
            var recorder = new SettingsPageRecorder(page, pathIdentity);
            recorder.Observe(draw);
            if (scope.Failed) reason = scope.FailureReason;
            else if (hidden.Failed) reason = hidden.FailureReason;
            else if (!hidden.IsBalanced) reason = "settings-reader-unbalanced-gui";
            else if (!ReferenceEquals(Event.current, neutral)) reason = "settings-reader-event-replaced";
            else if (!Available) reason = "settings-reader-prerequisites-changed";
            else if (recorder.LastCapture is not { Status: not SettingsCaptureStatus.Failed } completed)
                reason = "settings-reader-recording-failed";
            else candidate = completed;
        }
        catch (Exception)
        {
            reason = scope?.FailureReason ?? hidden?.FailureReason ?? "settings-reader-callback-failed";
        }
        finally
        {
            // Every acquired cleanup is attempted even if an earlier one fails. Preserve foreign
            // Event/backend replacements instead of restoring over state we no longer own.
            try { scope?.Dispose(); }
            catch (Exception) { reason = "settings-reader-scope-restore-failed"; }
            try { state?.Dispose(); }
            catch (Exception) { reason = "settings-reader-gui-restore-failed"; }
            try { if (hidden != null && ReferenceEquals(GuiBackend.Current, hidden)) GuiBackend.Current = previousBackend; }
            catch (Exception) { reason = "settings-reader-backend-restore-failed"; }
            try { if (neutral != null && ReferenceEquals(Event.current, neutral)) Event.current = previousEvent; }
            catch (Exception) { reason = "settings-reader-event-restore-failed"; }
            reading = false;
        }
        if (reason != null || candidate == null)
            return Result(SettingsNeutralReadStatus.Failed, reason ?? "settings-reader-no-capture", out reason);
        capture = candidate;
        return SettingsNeutralReadStatus.Captured;
    }

    private bool Available => initialized && !disposed && actions!.Available && controls!.Available && caches!.Available && bounds!.Available;

    private bool Initialize()
    {
        if (attempted) return initialized;
        attempted = true;
        try
        {
            actions = new(warning);
            controls = new(warning);
            caches = new(warning);
            bounds = new(controls, caches, warning);
            initialized = actions.Initialize() && controls.Initialize() && caches.Initialize() && bounds.Initialize();
            return initialized;
        }
        finally
        {
            // A failed acquisition must not leave a partially installed reader behind.
            if (!initialized) ReleaseGuards();
        }
    }

    public void Dispose()
    {
        if (Environment.CurrentManagedThreadId != thread)
            throw new InvalidOperationException("settings-reader-wrong-thread");
        if (reading) throw SettingsReadScope.Current?.Refuse("settings-reader-dispose-during-read")
            ?? new InvalidOperationException("settings-reader-dispose-during-read");
        if (disposed) return;
        disposed = true;
        initialized = false;
        ReleaseGuards();
    }

    private void ReleaseGuards()
    {
        List<Exception>? failures = null;
        foreach (IDisposable? guard in new IDisposable?[] { bounds, caches, controls, actions })
            try { guard?.Dispose(); }
            catch (Exception error) { (failures ??= new()).Add(error); }
        if (failures != null) throw new AggregateException("Settings-reader guard cleanup failed.", failures);
    }

    private static SettingsNeutralReadStatus Result(SettingsNeutralReadStatus status, string message, out string? reason)
    { reason = message; return status; }
}
