using Megamod.Settings;

namespace Megamod.Features.Diagnostics;

public static class DiagnosticSettings
{
    public static readonly SettingDefinition<bool> DefinitionContext = new(
        "diagnostics.definitionContext", true, SettingScope.GlobalOnly, "diagnostics", 0,
        SettingApplicationTiming.NextEligibleOccurrence, SettingCodecs.Boolean);

    public static void Register(SettingsStore store) => store.Register(DefinitionContext);
}
