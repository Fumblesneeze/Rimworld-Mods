using System;
using Megamod.Features.PracticalNeeds.Core;
using Megamod.Settings;
using Verse;

namespace Megamod.Features.PracticalNeeds;

public static class PotentialMoodSettingsUi
{
    public static void Register(SettingsRenderer renderer, Func<string?>? statusKey = null) => renderer.AddDynamicBoolean(PotentialMoodSettings.Enabled,
        () => "Megamod.PotentialMood.Enabled".Translate(), "Megamod.PotentialMood.Category", () => "Megamod.PotentialMood.EnabledHelp".Translate(),
        () => statusKey?.Invoke()?.Translate().ToString(), () => true, () => null);
}
