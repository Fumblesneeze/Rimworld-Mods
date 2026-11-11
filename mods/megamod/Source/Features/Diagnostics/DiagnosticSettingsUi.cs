using Megamod.Settings;

namespace Megamod.Features.Diagnostics;

public static class DiagnosticSettingsUi
{
    public static void Register(SettingsRenderer renderer) => renderer.AddBoolean(
        DiagnosticSettings.DefinitionContext, "Megamod_Diagnostics_DefinitionContext",
        "Megamod_Diagnostics_Category", "Megamod_Diagnostics_DefinitionContextHelp",
        "Megamod_Diagnostics_GlobalOnlyReason");
}
