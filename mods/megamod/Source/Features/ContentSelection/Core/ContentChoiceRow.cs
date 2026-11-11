using System;
using Megamod.Settings;

namespace Megamod.Features.ContentSelection.Core;

public enum ContentChoiceStatus { AppliedAllowed, AppliedExcluded, PendingAllow, PendingExclude, NextLoad, Unavailable, InvalidRequested }
public sealed record ContentChoiceState(bool RequestedAllowed, bool? AppliedAllowed, ContentChoiceStatus Status,
    string? UnavailableReasonKey, string? ValueErrorKey = null);

// A presentation of the shared definition and actual policy; it never owns or publishes settings.
public sealed class ContentChoiceRow
{
    public ContentChoiceRow(ContentSettings settings, ContentKey key, object identity, string label, string kindLabel, string sourceLabel)
    {
        Key = key;
        Identity = identity;
        Label = label;
        KindLabel = kindLabel;
        SourceLabel = sourceLabel;
        Definition = settings.Individual(key);
    }
    public ContentKey Key { get; }
    public object Identity { get; }
    public string Label { get; }
    public string KindLabel { get; }
    public string SourceLabel { get; }
    public SettingDefinition<bool> Definition { get; }
    public ContentChoiceState ReadState(SettingsStore store, AppliedContentPolicy? applied, bool hasActiveSave,
        string? unavailableReasonKey = null)
    {
        var read = hasActiveSave ? store.ReadEffective(Definition) : store.ReadGlobal(Definition);
        bool requested = read.Value;
        if (unavailableReasonKey != null) return new(requested, null, ContentChoiceStatus.Unavailable, unavailableReasonKey);
        if (read.ErrorKey != null) return new(requested, hasActiveSave ? applied?.IsAllowed(Identity) : null,
            ContentChoiceStatus.InvalidRequested, null, read.ErrorKey);
        if (!hasActiveSave) return new(requested, null, ContentChoiceStatus.NextLoad, null);
        if (applied == null) return new(requested, null, ContentChoiceStatus.Unavailable, "Megamod_Content_ApplicationUnavailable");
        bool current = applied.IsAllowed(Identity);
        var status = requested != current
            ? requested ? ContentChoiceStatus.PendingAllow : ContentChoiceStatus.PendingExclude
            : current ? ContentChoiceStatus.AppliedAllowed : ContentChoiceStatus.AppliedExcluded;
        return new(requested, current, status, null);
    }
}
