using Megamod.Settings;
using UnityEngine;
using Verse;

namespace Megamod.Features.ReadableNumbers;

public sealed class PunctuationSettingsEditor : ISettingsCompositeEditor<PunctuationPreference>
{
    private readonly SettingsStore store;
    private readonly bool globalDefault;
    private PunctuationPreference? stored;
    private string groupDraft = "";
    private string decimalDraft = "";
    private string? error;

    public PunctuationSettingsEditor(SettingsStore store, bool globalDefault)
    {
        this.store = store;
        this.globalDefault = globalDefault;
    }

    public void Reset() { stored = null; error = null; groupDraft = ""; decimalDraft = ""; }

    public string? Draw(Rect rect, PunctuationPreference value, bool enabled,
        Func<PunctuationPreference, SettingEditResult> commit)
    {
        if (stored != value) SetDraft(value);
        var modeRect = new Rect(rect.x, rect.y, rect.width, 28);
        if (NumberSettingsButton.Draw(modeRect, PresetKey(value.Preset).Translate(), enabled) && enabled)
        {
            var selected = value with { Preset = (PunctuationPreset)(((int)value.Preset + 1) % 7) };
            var result = commit(selected);
            error = result.ErrorKey;
            if (result.Succeeded) { value = selected; SetDraft(value); }
        }
        if (enabled) TooltipHandler.TipRegion(modeRect, "Megamod_Numbers_PresetCycleHelp".Translate());
        float half = (rect.width - 8) / 2;
        Widgets.Label(new Rect(rect.x, rect.y + 32, half, 26), "Megamod_Numbers_GroupMark".Translate());
        Widgets.Label(new Rect(rect.x + half + 8, rect.y + 32, half, 26), "Megamod_Numbers_DecimalMark".Translate());
        var groupRect = new Rect(rect.x, rect.y + 60, half, 28);
        var decimalRect = new Rect(rect.x + half + 8, rect.y + 60, half, 28);
        bool editable = enabled && value.Preset == PunctuationPreset.Custom;
        if (editable)
        {
            string group = Widgets.TextField(groupRect, groupDraft);
            string mark = Widgets.TextField(decimalRect, decimalDraft);
            if (group != groupDraft || mark != decimalDraft)
            {
                groupDraft = group;
                decimalDraft = mark;
                error = PunctuationPreference.Validate(value with { CustomGroup = group, CustomDecimal = mark });
            }
        }
        else
        {
            Widgets.Label(groupRect, value.CustomGroup);
            Widgets.Label(decimalRect, value.CustomDecimal);
        }
        var candidate = value with { CustomGroup = groupDraft, CustomDecimal = decimalDraft };
        bool valid = PunctuationPreference.Validate(candidate) == null;
        bool canApply = editable && valid;
        if (NumberSettingsButton.Draw(new Rect(rect.x, rect.y + 92, rect.width, 28),
            "Megamod_Numbers_ApplyPair".Translate(), canApply) && canApply)
        {
            var result = commit(candidate);
            error = result.ErrorKey;
            if (result.Succeeded) SetDraft(candidate);
        }
        Widgets.Label(new Rect(rect.x, rect.y + 124, rect.width, 24), "Megamod_Numbers_Preview".Translate());
        string preview = NumberPresentation.Format(new("-1234567.00"), store, globalDefault) + "\n" +
            NumberPresentation.Format(new("123456789.25"), store, globalDefault) + "\n" +
            NumberPresentation.Format(new("9.18"), store, globalDefault);
        Widgets.Label(new Rect(rect.x, rect.y + 150, rect.width, 78), preview);
        return error;
    }

    private void SetDraft(PunctuationPreference value)
    {
        stored = value;
        groupDraft = value.CustomGroup;
        decimalDraft = value.CustomDecimal;
        error = null;
    }

    private static string PresetKey(PunctuationPreset preset) => preset switch
    {
        PunctuationPreset.CommaPeriod => "Megamod_Numbers_CommaPeriod",
        PunctuationPreset.PeriodComma => "Megamod_Numbers_PeriodComma",
        PunctuationPreset.SpacePeriod => "Megamod_Numbers_SpacePeriod",
        PunctuationPreset.SpaceComma => "Megamod_Numbers_SpaceComma",
        PunctuationPreset.ApostrophePeriod => "Megamod_Numbers_ApostrophePeriod",
        PunctuationPreset.ApostropheComma => "Megamod_Numbers_ApostropheComma",
        _ => "Megamod_Numbers_Custom"
    };
}
