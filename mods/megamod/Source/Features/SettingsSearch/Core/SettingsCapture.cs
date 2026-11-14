using System;
using System.Collections.Generic;
using Megamod.Features.ModSettingsNavigation;

namespace Megamod.Features.SettingsSearch.Core;

public readonly record struct SettingsCaptureRect(float X, float Y, float Width, float Height);
public enum SettingsCaptureKind { Text, Tooltip }
public sealed record SettingsCaptureRecord(int Occurrence, SettingsCaptureKind Kind, string Text,
    SettingsCaptureRect Bounds);
public enum SettingsCaptureStatus { Partial, Truncated, Failed }
public enum SettingsCaptureFailure { None, CallbackThrew, BackendReplaced, RecordingFailed }
public sealed record SettingsCaptureSnapshot(SettingsPage Page, string PathIdentity,
    IReadOnlyList<SettingsCaptureRecord> Records, SettingsCaptureStatus Status, SettingsCaptureFailure Failure);

// One completed page draw supplies observations, not proof of complete page coverage or control identity.
public sealed class SettingsCaptureBuffer
{
    private readonly SettingsPage page;
    private readonly string pathIdentity;
    private readonly List<SettingsCaptureRecord> records = new();
    private readonly int maxRecords;
    private readonly int maxTextUnits;
    private int textUnits;
    private bool truncated;
    private bool published;

    public SettingsCaptureBuffer(SettingsPage page, string pathIdentity, int maxRecords = 16384,
        int maxTextUnits = 262144)
    {
        if (maxRecords < 0) throw new ArgumentOutOfRangeException(nameof(maxRecords));
        if (maxTextUnits < 0) throw new ArgumentOutOfRangeException(nameof(maxTextUnits));
        this.page = page;
        this.pathIdentity = pathIdentity;
        this.maxRecords = maxRecords;
        this.maxTextUnits = maxTextUnits;
    }

    public void Add(SettingsCaptureKind kind, string? text, SettingsCaptureRect bounds)
    {
        RequireUnpublished();
        if (truncated || string.IsNullOrEmpty(text) || !ValidBounds(bounds)) return;
        if (records.Count >= maxRecords || text.Length > maxTextUnits - textUnits)
        {
            truncated = true;
            return;
        }
        records.Add(new(records.Count, kind, text, bounds));
        textUnits += text.Length;
    }

    public SettingsCaptureSnapshot Complete()
    {
        RequireUnpublished();
        published = true;
        return new(page, pathIdentity, Array.AsReadOnly(records.ToArray()),
            truncated ? SettingsCaptureStatus.Truncated : SettingsCaptureStatus.Partial, SettingsCaptureFailure.None);
    }

    public SettingsCaptureSnapshot Fail(SettingsCaptureFailure reason)
    {
        RequireUnpublished();
        if (reason == SettingsCaptureFailure.None || !Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason));
        published = true;
        records.Clear();
        return new(page, pathIdentity, Array.Empty<SettingsCaptureRecord>(), SettingsCaptureStatus.Failed, reason);
    }

    private void RequireUnpublished()
    {
        if (published) throw new InvalidOperationException("The capture has already been published.");
    }

    private static bool ValidBounds(SettingsCaptureRect bounds) =>
        float.IsFinite(bounds.X) && float.IsFinite(bounds.Y) && float.IsFinite(bounds.Width) &&
        float.IsFinite(bounds.Height) && bounds.Width > 0 && bounds.Height > 0 &&
        float.IsFinite(bounds.X + bounds.Width) && float.IsFinite(bounds.Y + bounds.Height);
}
