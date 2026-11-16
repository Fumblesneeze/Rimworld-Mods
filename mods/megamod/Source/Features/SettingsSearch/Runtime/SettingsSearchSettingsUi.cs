using Megamod.Features.ModSettingsNavigation;
using Megamod.Settings;

namespace Megamod.Features.SettingsSearch.Runtime;

public static class SettingsSearchSettingsUi
{
    public static void Register(SettingsRenderer renderer)
    {
        renderer.AddBoolean(ModSettingsNavigationSettings.Enabled, "Megamod_Search_SortEnabled",
            "Megamod_Search_Category", "Megamod_Search_SortHelp");
        renderer.AddBoolean(SettingsSearchSettings.Enabled, "Megamod_Search_Enabled",
            "Megamod_Search_Category", "Megamod_Search_EnabledHelp");
    }
}
