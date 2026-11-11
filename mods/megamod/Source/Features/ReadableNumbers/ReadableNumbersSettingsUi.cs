using Megamod.Settings;

namespace Megamod.Features.ReadableNumbers;

public static class ReadableNumbersSettingsUi
{
    // Explicit integration point while the punctuation slice awaits independent/native review.
    public static void RegisterPunctuation(SettingsRenderer renderer) =>
        renderer.AddComposite(ReadableNumbersSettings.Punctuation, "Megamod_Numbers_Punctuation",
            "Megamod_Numbers_Category", "Megamod_Numbers_PunctuationHelp", 228,
            (store, globalDefault) => new PunctuationSettingsEditor(store, globalDefault));

    public static void Register(SettingsRenderer renderer)
    {
        renderer.AddBoolean(ReadableNumbersSettings.Enabled, "Megamod_Numbers_Enabled",
            "Megamod_Numbers_Category", "Megamod_Numbers_EnabledHelp");
        renderer.AddComposite(ReadableNumbersSettings.Grouping, "Megamod_Numbers_Grouping",
            "Megamod_Numbers_Category", "Megamod_Numbers_GroupingHelp", 28,
            (_, _) => new GroupingSettingsEditor());
        renderer.AddInteger(ReadableNumbersSettings.MinimumIntegerDigits, "Megamod_Numbers_MinimumIntegerDigits",
            "Megamod_Numbers_Category", "Megamod_Numbers_MinimumIntegerDigitsHelp");
        renderer.AddBoolean(ReadableNumbersSettings.SmallDecimals, "Megamod_Numbers_SmallDecimals",
            "Megamod_Numbers_Category", "Megamod_Numbers_SmallDecimalsHelp");
    }
}
