using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;

namespace Megamod.Features.SettingsSearch.Runtime;

// Each step returns with every tab it changed restored. Cancellation never retains a foreign undo callback.
public sealed class SettingsForeignPageRead
{
    private const int MaxStates = 32, MaxDepth = 3;
    private readonly SettingsPage page;
    private readonly SettingsForeignRecorder recorder;
    private readonly Queue<SettingsForeignPath> pending = new();
    private readonly HashSet<string> queued = new(StringComparer.Ordinal);
    private readonly HashSet<string> visited = new(StringComparer.Ordinal);
    private readonly List<SettingsForeignOption> options = new();
    private readonly HashSet<(string Path, SettingsForeignControlKind Kind, string Label, string? Help, int Occurrence)> retained = new();
    private readonly SettingsForeignRetention retention = new();
    private readonly SettingsForeignReadBudget budget = new();

    public SettingsForeignPageRead(SettingsPage page, SettingsForeignRecorder recorder)
    {
        this.page = page ?? throw new ArgumentNullException(nameof(page));
        this.recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        retention.TryRetainPath(Array.Empty<SettingsForeignTab>(), out var root);
        pending.Enqueue(root);
        queued.Add(root.Identity);
    }
    public bool Finished { get; private set; }
    public string? FailureReason { get; private set; }
    public SettingsCaptureSnapshot? LastCapture { get; private set; }
    public IReadOnlyList<SettingsForeignOption> Options => options.AsReadOnly();
    public int DrawCount => budget.DrawCount;
    public int LastDrawCount { get; private set; }
    public double LastMaximumDrawMilliseconds { get; private set; }

    public SettingsNeutralReadStatus Step(SettingsNeutralReader reader, Action draw)
    {
        if (Finished) return SettingsNeutralReadStatus.Captured;
        LastDrawCount = 0;
        LastMaximumDrawMilliseconds = 0;
        var requested = pending.Peek();
        SettingsForeignFrame? completed = null;
        string? callbackFailure = null;
        var status = reader.Read(page, requested.Identity, () =>
        {
            var undo = new List<SettingsForeignTabView>();
            SettingsForeignFrame Draw(Action? select = null)
            {
                var clock = Stopwatch.StartNew();
                bool invoked = false;
                try
                {
                    select?.Invoke();
                    var frame = recorder.Observe(page, () => { invoked = true; draw(); });
                    if (frame.FailureReason != null)
                    { callbackFailure = frame.FailureReason; throw new InvalidOperationException(callbackFailure); }
                    return frame;
                }
                finally
                {
                    if (invoked)
                    {
                        double elapsed = clock.Elapsed.TotalMilliseconds;
                        budget.RecordDraw(elapsed);
                        LastDrawCount++;
                        LastMaximumDrawMilliseconds = Math.Max(LastMaximumDrawMilliseconds, elapsed);
                    }
                }
            }
            try
            {
                var current = Draw();
                completed = current;
                foreach (var step in requested.Tabs)
                {
                    if (!budget.CanExplore(undo.Count + 1)) return;
                    var target = current.Tabs.Where(tab => tab.Identity == step).ToArray();
                    var original = current.Tabs.Where(tab => tab.Identity.Group == step.Group && tab.Selected).ToArray();
                    if (target.Length != 1 || original.Length != 1 || target[0].Select == null || original[0].Select == null)
                    { callbackFailure = "settings-foreign-tab-missing"; throw new InvalidOperationException(callbackFailure); }
                    if (target[0].Selected) continue;
                    undo.Add(original[0]);
                    current = Draw(target[0].Select);
                    completed = current;
                    if (!current.Tabs.Any(tab => tab.Identity == step && tab.Selected))
                    { callbackFailure = "settings-foreign-tab-not-selected"; throw new InvalidOperationException(callbackFailure); }
                }
                completed = current;
            }
            catch (Exception)
            {
                callbackFailure ??= "settings-foreign-tab-callback-failed";
                throw;
            }
            finally
            {
                bool restored = true;
                for (int index = undo.Count - 1; index >= 0; index--)
                {
                    try
                    {
                        var current = Draw(undo[index].Select);
                        restored &= current.Tabs.Any(tab => tab.Identity == undo[index].Identity && tab.Selected);
                    }
                    catch (Exception) { restored = false; }
                }
                if (!restored)
                {
                    callbackFailure = "settings-foreign-tab-restore-failed";
                    throw SettingsReadScope.Current!.Refuse(callbackFailure);
                }
            }
        }, out _, out string? reason);
        if (status == SettingsNeutralReadStatus.Deferred) return status;
        if (status != SettingsNeutralReadStatus.Captured || completed == null)
        {
            Finished = true;
            options.Clear();
            LastCapture = null;
            FailureReason = callbackFailure ?? reason ?? "settings-foreign-read-failed";
            return status == SettingsNeutralReadStatus.Captured ? SettingsNeutralReadStatus.Failed : status;
        }
        pending.Dequeue();
        LastCapture = null;
        FailureReason ??= completed.CoverageReason;
        string path = completed.PathIdentity;
        if (visited.Add(path))
        {
            var occurrences = new Dictionary<(string Path, SettingsForeignControlKind Kind, string Label, string? Help), int>();
            foreach (var option in completed.Options)
            {
                var key = (Path: option.Entry.PathIdentity, option.Target.Kind, option.Target.Label, option.Target.Help);
                occurrences.TryGetValue(key, out int occurrence);
                occurrences[key] = occurrence + 1;
                if (!retained.Add((key.Path, key.Kind, key.Label, key.Help, occurrence))) continue;
                if (!retention.TryRetain(option, out var admitted))
                {
                    FailureReason = "settings-foreign-retention-limit"; pending.Clear(); Finished = true;
                    LastCapture = completed.Capture with { PathIdentity = "foreign:", Records = Array.Empty<SettingsCaptureRecord>(),
                        Status = SettingsCaptureStatus.Truncated };
                    return SettingsNeutralReadStatus.Captured;
                }
                options.Add(admitted);
            }
            if (!retention.TryRetainCapture(completed.Capture with { PathIdentity = completed.PathIdentity }, out var retainedCapture))
            { FailureReason = "settings-foreign-retention-limit"; pending.Clear(); }
            LastCapture = retainedCapture;
            var prefix = new List<SettingsForeignTab>();
            foreach (var group in completed.Tabs.GroupBy(tab => tab.Identity.Group))
            {
                if (budget.StopReason != null || completed.CoverageReason != null || FailureReason != null) break;
                var selected = group.Single(tab => tab.Selected);
                foreach (var alternative in group.Where(tab => !tab.Selected))
                {
                    if (prefix.Count >= MaxDepth) { FailureReason ??= "settings-foreign-tab-depth-limit"; break; }
                    var next = prefix.Append(alternative.Identity).ToArray();
                    if (!retention.TryRetainPath(next, out var nextPath))
                    { FailureReason ??= "settings-foreign-retention-limit"; pending.Clear(); break; }
                    if (!queued.Add(nextPath.Identity)) continue;
                    if (queued.Count > MaxStates) { FailureReason ??= "settings-foreign-tab-state-limit"; break; }
                    pending.Enqueue(nextPath);
                }
                prefix.Add(selected.Identity);
            }
        }
        // A repeated state contributes no new observations and must not retain another raw frame.
        LastCapture ??= completed.Capture with { PathIdentity = "foreign:", Records = Array.Empty<SettingsCaptureRecord>() };
        if (LastCapture.Status == SettingsCaptureStatus.Truncated || FailureReason == "settings-foreign-retention-limit")
        { FailureReason = "settings-foreign-retention-limit"; LastCapture = LastCapture with { Status = SettingsCaptureStatus.Truncated }; pending.Clear(); }
        // Reserve enough for the next root draw, three switches, and their three guaranteed undos.
        if (!budget.CanExplore(2 * MaxDepth))
        { FailureReason ??= budget.StopReason; pending.Clear(); }
        Finished = pending.Count == 0;
        return SettingsNeutralReadStatus.Captured;
    }
}
