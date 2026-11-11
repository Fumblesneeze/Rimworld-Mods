using System;
using System.Collections.Generic;
using System.Threading;
using Verse;

namespace Megamod.Settings;

public sealed class MegamodSaveSettings : GameComponent
{
    private readonly Game owner;
    private string? saveIdentity = Guid.NewGuid().ToString("N");
    private Dictionary<string, bool> inheritance = new(StringComparer.Ordinal);
    private Dictionary<string, string> overrides = new(StringComparer.Ordinal);
    private SaveSettingsSnapshot published;

    public MegamodSaveSettings(Game game)
    {
        owner = game;
        published = new(GetSaveIdentity());
    }

    // Available as soon as LoadingVars finishes, without activating this game for UI writes.
    public SaveSettingsSnapshot Snapshot => Volatile.Read(ref published);

    private SaveSettingsSnapshot BuildSnapshot()
    {
        var values = new Dictionary<string, SaveSettingValue>(StringComparer.Ordinal);
        foreach (var pair in inheritance)
            values.Add(pair.Key, new(pair.Value, overrides.TryGetValue(pair.Key, out string? value) ? value : null));
        foreach (var pair in overrides)
            if (!values.ContainsKey(pair.Key))
                values.Add(pair.Key, new(true, pair.Value));
        return new(GetSaveIdentity(), values);
    }

    internal void Capture(SaveSettingsSnapshot snapshot)
        => Volatile.Write(ref published, snapshot);

    private void PrepareSerialization(SaveSettingsSnapshot snapshot)
    {
        inheritance.Clear();
        overrides.Clear();
        foreach (var pair in snapshot.Values)
        {
            inheritance.Add(pair.Key, pair.Value.UseGlobal);
            if (pair.Value.Override != null)
                overrides.Add(pair.Key, pair.Value.Override);
        }
    }

    public override void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving)
            PrepareSerialization(Snapshot);
        Scribe_Values.Look(ref saveIdentity, "saveIdentity");
        Scribe_Collections.Look(ref inheritance, "inheritance", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref overrides, "overrides", LookMode.Value, LookMode.Value);
        GetSaveIdentity();
        inheritance ??= new(StringComparer.Ordinal);
        overrides ??= new(StringComparer.Ordinal);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
            Volatile.Write(ref published, BuildSnapshot());
    }

    public override void FinalizeInit() => SettingsRuntime.Instance?.NotifyGameFinalized(owner, Snapshot);

    private string GetSaveIdentity()
    {
        if (string.IsNullOrWhiteSpace(saveIdentity))
            saveIdentity = Guid.NewGuid().ToString("N");
        return saveIdentity;
    }
}
