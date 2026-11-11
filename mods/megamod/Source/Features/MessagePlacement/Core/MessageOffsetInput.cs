using System;
using System.Globalization;

namespace Megamod.Features.MessagePlacement;

public static class MessageOffsetInput
{
    public static bool TryParse(string horizontal, string vertical, IFormatProvider culture, out MessageOffsets offsets)
    {
        offsets = default;
        if (!double.TryParse(horizontal, NumberStyles.Float, culture, out double x) ||
            !double.TryParse(vertical, NumberStyles.Float, culture, out double y)) return false;
        var pair = new MessageOffsets(x, y);
        if (MessageOffsets.Validate(pair) != null) return false;
        offsets = pair;
        return true;
    }
}
