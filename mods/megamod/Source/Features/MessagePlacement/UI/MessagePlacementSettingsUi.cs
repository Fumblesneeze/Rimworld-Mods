using Megamod.Settings;
using Verse;

namespace Megamod.Features.MessagePlacement;

public static class MessagePlacementSettingsUi
{
    // Root owns definition registration and the decision to expose this unfinished feature.
    public static void Register(SettingsRenderer renderer)
    {
        renderer.AddBoolean(MessagePlacementSettings.Enabled, "Megamod_Messages_Enabled", "Megamod_Messages_Category",
            "Megamod_Messages_EnabledHelp");
        renderer.AddSection("messages.automaticStatus", "interface.messages", -2, "Megamod_Messages_Category", 50,
            rect => Widgets.Label(rect, "Megamod_Messages_BarIntegrationPending".Translate()), () => { });
        renderer.AddComposite(
        MessagePlacementSettings.PositionOffset, "Megamod_Messages_PositionOffset", "Megamod_Messages_Category",
        "Megamod_Messages_PositionOffsetHelp", 176, (_, _) => new MessageOffsetsEditor());
    }
}
