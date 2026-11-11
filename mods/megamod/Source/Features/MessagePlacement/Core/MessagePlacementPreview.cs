using System.Collections.Generic;
using Megamod.Settings;

namespace Megamod.Features.MessagePlacement;

public sealed record MessagePreviewSnapshot(string ScopeKey, string StatusKey,
    IReadOnlyList<MessageBounds> Messages, double ViewportWidth, double ViewportHeight);

// A detached projection of common settings. Samples never enter the native message system.
public static class MessagePlacementPreview
{
    public static MessagePreviewSnapshot Capture(SettingsStore settings, IReadOnlyList<MessageBounds> samples,
        double viewportWidth, double viewportHeight, bool placementAvailable)
    {
        var rectangles = new List<MessageBounds>();
        bool enabled = settings.GetEffective(MessagePlacementSettings.Enabled);
        string status = !enabled ? "Megamod_Messages_PreviewDisabled" :
            !placementAvailable ? "Megamod_Messages_PreviewUnavailable" : "Megamod_Messages_BarIntegrationPending";
        if (enabled && placementAvailable &&
            MessagePlacement.TryCalculate(samples, viewportWidth, viewportHeight, null,
            settings.GetEffective(MessagePlacementSettings.PositionOffset), out var translation))
            foreach (var sample in samples)
                rectangles.Add(sample with { X = sample.X + translation.X, Y = sample.Y + translation.Y });
        return new(settings.CurrentSaveIdentity == null ? "Megamod_Messages_PreviewGlobal" : "Megamod_Messages_PreviewSave",
            status,
            rectangles.AsReadOnly(), viewportWidth, viewportHeight);
    }
}
