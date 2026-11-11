using System.Text.Json;
using Megamod.Settings;

namespace Megamod.Features.MessagePlacement;

public static class MessagePlacementSettings
{
    public static readonly SettingDefinition<bool> Enabled = new(
        "messages.enabled", true, SettingScope.GlobalAndSave, "interface.messages", -1,
        SettingApplicationTiming.NextDisplay, SettingCodecs.Boolean);
    public static readonly SettingDefinition<MessageOffsets> PositionOffset = new(
        "messages.positionOffset", default, SettingScope.GlobalAndSave, "interface.messages", 0,
        SettingApplicationTiming.NextDisplay, new MessageOffsetsCodec(), MessageOffsets.Validate);

    public static void Register(SettingsStore store) { store.Register(Enabled); store.Register(PositionOffset); }
}

public sealed class MessageOffsetsCodec : ISettingCodec<MessageOffsets>
{
    public string Encode(MessageOffsets value) => JsonSerializer.Serialize(value);
    public bool TryDecode(string encoded, out MessageOffsets value)
    {
        value = default;
        try
        {
            using var document = JsonDocument.Parse(encoded);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty(nameof(MessageOffsets.HorizontalPercent), out var horizontal) ||
                horizontal.ValueKind != JsonValueKind.Number || !horizontal.TryGetDouble(out double x) ||
                !root.TryGetProperty(nameof(MessageOffsets.VerticalPercent), out var vertical) ||
                vertical.ValueKind != JsonValueKind.Number || !vertical.TryGetDouble(out double y)) return false;
            value = new(x, y);
            return true;
        }
        catch (JsonException) { return false; }
    }
}
