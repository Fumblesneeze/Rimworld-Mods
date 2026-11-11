using System;
using System.Globalization;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Settings;

namespace Megamod.Features.DrugPolicyGeneration.Core;

// Transient text for the common settings renderer; policy drafts and persistence stay elsewhere.
public sealed class DrugGenerationPreferenceDraft
{
    private readonly bool allowAny;
    private DrugToleranceChoice? stored;
    private CultureInfo? culture;
    private DrugToleranceEditDraft? draft;
    public DrugGenerationPreferenceDraft(bool allowAny) => this.allowAny = allowAny;
    public bool Any => draft?.Any ?? false;
    public string Percentage => draft?.Percentage ?? string.Empty;
    public string? ErrorKey { get; private set; }
    public void Synchronize(DrugToleranceChoice value, CultureInfo culture)
    {
        if (stored != null && stored.Kind == value.Kind && stored.Limit == value.Limit &&
            stored.Encoding == value.Encoding && this.culture?.Equals(culture) == true) return;
        SetDraft(value, culture);
    }
    public void Edit(bool any, string percentage)
    {
        if (draft == null) throw new InvalidOperationException("The preference draft has no current value.");
        draft.Any = any; draft.Percentage = percentage;
        ErrorKey = CanApply ? null : InvalidKey;
    }
    public bool CanApply => draft != null && (allowAny || !draft.Any) && draft.TryGetChoice(out _);
    public SettingEditResult Apply(bool enabled, Func<DrugToleranceChoice, SettingEditResult> commit)
    {
        if (!enabled) return new(false);
        if (!CanApply || !draft!.TryGetChoice(out var value)) return new(false, ErrorKey = InvalidKey);
        var result = commit(value);
        ErrorKey = result.ErrorKey;
        if (result.Succeeded) SetDraft(value, culture!);
        return result;
    }
    public void Reset() { stored = null; culture = null; draft = null; ErrorKey = null; }
    private string InvalidKey => allowAny ? "Megamod_DrugGeneration_InvalidTolerance" : "Megamod_DrugGeneration_InvalidThreshold";
    private void SetDraft(DrugToleranceChoice value, CultureInfo culture)
    {
        stored = value; this.culture = culture;
        draft = new DrugToleranceEditDraft(value, culture);
        ErrorKey = null;
    }
}
