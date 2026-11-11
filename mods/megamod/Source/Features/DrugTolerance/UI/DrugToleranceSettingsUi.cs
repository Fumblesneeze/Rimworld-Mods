using System;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Settings;
using Verse;

namespace Megamod.Features.DrugTolerance;

public static class DrugToleranceSettingsUi
{
    public static void Register(SettingsRenderer renderer, Func<string?>? statusKey = null) => renderer.AddDynamicBoolean(DrugToleranceSettings.Enabled,
        () => "Megamod.DrugTolerance.Enabled".Translate(), "Megamod_DrugPolicy_Category", () => "Megamod.DrugTolerance.EnabledHelp".Translate(),
        () => statusKey?.Invoke()?.Translate().ToString(), () => true, () => null);
}
