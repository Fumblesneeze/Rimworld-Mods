using System;
using Megamod.Settings;

namespace Megamod.Features.MessagePlacement;

public static class MessagePlacementSettingsUi
{
    // Root owns definition registration and the decision to expose this unfinished feature.
    public static void Register(SettingsRenderer renderer, SettingsStore store, Func<bool> placementAvailable)
    {
        renderer.AddBoolean(MessagePlacementSettings.Enabled, "Megamod_Messages_Enabled", "Megamod_Messages_Category",
            "Megamod_Messages_EnabledHelp");
        renderer.AddComposite(
        MessagePlacementSettings.PositionOffset, "Megamod_Messages_PositionOffset", "Megamod_Messages_Category",
        "Megamod_Messages_PositionOffsetHelp", 176, (_, _) => new MessageOffsetsEditor());
        var preview = new MessagePlacementPreviewUi(store, placementAvailable);
        renderer.AddSection("messages.preview", "interface.messages", 1, "Megamod_Messages_Category", 260,
            preview.Draw, () => { });
    }
}
