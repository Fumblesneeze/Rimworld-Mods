using System;

namespace Megamod.Features.DrugTolerance.Core;

public static class DrugToleranceSlider
{
    public const float Maximum = 101f;
    public static float Position(DrugToleranceChoice choice) => choice.Kind == DrugToleranceChoiceKind.Limit ? choice.Limit * 100f : Maximum;

    public static DrugToleranceChoice Apply(DrugToleranceChoice original, float position, bool interacted)
    {
        if (!interacted || !float.IsFinite(position) || position < 0 || position > Maximum) return original;
        int percent = (int)MathF.Round(position, MidpointRounding.AwayFromZero);
        return percent == 101 ? DrugToleranceChoice.Any : DrugToleranceChoice.Numeric(percent / 100f);
    }
}
