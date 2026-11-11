using Megamod.Settings;

namespace Megamod.Features.ReadableNumbers;

public static class NumberPresentation
{
    public static string Format(RawDecimalQuantity quantity, SettingsStore store, bool globalDefault = false)
    {
        bool enabled = globalDefault ? store.GetGlobal(ReadableNumbersSettings.Enabled) :
            store.GetEffective(ReadableNumbersSettings.Enabled);
        if (!enabled) return quantity.OriginalText;
        var punctuation = globalDefault ? store.GetGlobal(ReadableNumbersSettings.Punctuation) :
            store.GetEffective(ReadableNumbersSettings.Punctuation);
        (string group, string mark) = punctuation.Preset switch
        {
            PunctuationPreset.CommaPeriod => (",", "."),
            PunctuationPreset.PeriodComma => (".", ","),
            PunctuationPreset.SpacePeriod => (" ", "."),
            PunctuationPreset.SpaceComma => (" ", ","),
            PunctuationPreset.ApostrophePeriod => ("'", "."),
            PunctuationPreset.ApostropheComma => ("'", ","),
            _ => (punctuation.CustomGroup, punctuation.CustomDecimal)
        };
        int minimum = globalDefault ? store.GetGlobal(ReadableNumbersSettings.MinimumIntegerDigits) :
            store.GetEffective(ReadableNumbersSettings.MinimumIntegerDigits);
        var grouping = globalDefault ? store.GetGlobal(ReadableNumbersSettings.Grouping) :
            store.GetEffective(ReadableNumbersSettings.Grouping);
        bool smallDecimals = globalDefault ? store.GetGlobal(ReadableNumbersSettings.SmallDecimals) :
            store.GetEffective(ReadableNumbersSettings.SmallDecimals);
        return QuantityFormatter.Format(quantity, new(Grouping: grouping, MinimumIntegerDigits: minimum,
            GroupSeparator: group, DecimalMark: mark, ApplyDecimalToSmallNumbers: smallDecimals));
    }
}
