using System.Globalization;

namespace Megamod.Features.DrugTolerance.Core;

public sealed class DrugToleranceEditDraft
{
    private readonly DrugToleranceChoice original;
    private readonly CultureInfo culture;
    private readonly string initialPercentage;

    public DrugToleranceEditDraft(DrugToleranceChoice original, CultureInfo culture)
    {
        this.original = original; this.culture = culture;
        Any = original.Kind == DrugToleranceChoiceKind.Any;
        Percentage = initialPercentage = original.Kind == DrugToleranceChoiceKind.Limit
            ? (original.Limit * 100f).ToString("R", culture)
            : original.Kind == DrugToleranceChoiceKind.Any ? "100" : "";
    }
    public bool Any { get; set; }
    public string Percentage { get; set; }
    public bool TryGetChoice(out DrugToleranceChoice choice)
    {
        choice = original;
        if (Any)
        {
            choice = original.Kind == DrugToleranceChoiceKind.Any ? original : DrugToleranceChoice.Any;
            return true;
        }
        if (original.Kind == DrugToleranceChoiceKind.Limit && Percentage == initialPercentage) return true;
        if (!float.TryParse(Percentage, NumberStyles.Float, culture, out float value) ||
            !float.IsFinite(value) || value < 0 || value > 100) return false;
        choice = DrugToleranceChoice.Numeric(value / 100f);
        return true;
    }
}
