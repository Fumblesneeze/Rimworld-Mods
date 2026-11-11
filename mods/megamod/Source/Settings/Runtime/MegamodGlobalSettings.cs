using System;
using System.Collections.Generic;
using Verse;

namespace Megamod.Settings;

public sealed class MegamodGlobalSettings : ModSettings
{
    private Dictionary<string, string> values = new(StringComparer.Ordinal);

    public GlobalSettingsSnapshot Snapshot => new(values);

    public void Capture(GlobalSettingsSnapshot snapshot) => values = new(snapshot.Values, StringComparer.Ordinal);

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref values, "values", LookMode.Value, LookMode.Value);
        values ??= new(StringComparer.Ordinal);
    }
}
