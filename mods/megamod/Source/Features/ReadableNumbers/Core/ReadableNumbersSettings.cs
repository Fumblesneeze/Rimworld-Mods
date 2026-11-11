using Megamod.Settings;

namespace Megamod.Features.ReadableNumbers;

public static class ReadableNumbersSettings
{
    public static readonly SettingDefinition<bool> SmallDecimals = new(
        "numbers.smallDecimals", true, SettingScope.GlobalAndSave, "numbers", 40,
        SettingApplicationTiming.NextDisplay, SettingCodecs.Boolean);

    public static readonly SettingDefinition<NumberGrouping> Grouping = new(
        "numbers.grouping", NumberGrouping.ThreeDigits, SettingScope.GlobalAndSave, "numbers", 20,
        SettingApplicationTiming.NextDisplay, new NumberGroupingCodec(),
        value => Enum.IsDefined(value) ? null : "Megamod_Numbers_GroupingInvalid");

    public static readonly SettingDefinition<PunctuationPreference> Punctuation = new(
        "numbers.punctuation", PunctuationPreference.BuiltIn, SettingScope.GlobalAndSave, "numbers", 10,
        SettingApplicationTiming.NextDisplay, new PunctuationPreferenceCodec(), PunctuationPreference.Validate);

    public static void Register(SettingsStore store)
    {
        store.Register(Enabled);
        store.Register(Grouping);
        store.Register(MinimumIntegerDigits);
        store.Register(SmallDecimals);
    }

    public static readonly SettingDefinition<int> MinimumIntegerDigits = new(
        "numbers.minimumIntegerDigits", 4, SettingScope.GlobalAndSave, "numbers", 30,
        SettingApplicationTiming.NextDisplay, SettingCodecs.Int32,
        value => value >= 4 ? null : "Megamod_Numbers_MinimumIntegerDigitsInvalid");

    public static readonly SettingDefinition<bool> Enabled = new(
        "numbers.enabled", true, SettingScope.GlobalAndSave, "numbers", 0,
        SettingApplicationTiming.NextDisplay, SettingCodecs.Boolean);
}

public sealed class NumberGroupingCodec : ISettingCodec<NumberGrouping>
{
    public string Encode(NumberGrouping value) => value.ToString();
    public bool TryDecode(string encoded, out NumberGrouping value) =>
        Enum.TryParse(encoded, out value) && Enum.IsDefined(value) && encoded == value.ToString();
}
