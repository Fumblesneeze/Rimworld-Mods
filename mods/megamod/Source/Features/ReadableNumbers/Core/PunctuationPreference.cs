using System.Buffers;
using System.Text;
using System.Text.Json;
using Megamod.Settings;

namespace Megamod.Features.ReadableNumbers;

public enum PunctuationPreset
{
    CommaPeriod, PeriodComma, SpacePeriod, SpaceComma, ApostrophePeriod, ApostropheComma, Custom
}

public sealed record PunctuationPreference(PunctuationPreset Preset, string CustomGroup, string CustomDecimal)
{
    public static PunctuationPreference BuiltIn { get; } = new(PunctuationPreset.CommaPeriod, ",", ".");
    public static string? Validate(PunctuationPreference value) =>
        value == null || !Enum.IsDefined(value.Preset) || value.CustomGroup == value.CustomDecimal ||
        !ValidMark(value.CustomGroup, allowSpace: true) || !ValidMark(value.CustomDecimal, allowSpace: false)
            ? "Megamod_Numbers_PunctuationInvalid" : null;

    private static bool ValidMark(string? mark, bool allowSpace)
    {
        if (allowSpace && mark == " ") return true;
        if (string.IsNullOrEmpty(mark) || Rune.DecodeFromUtf16(mark, out var rune, out int consumed) !=
            OperationStatus.Done || consumed != mark.Length) return false;
        // Braille pattern blank is classified as a symbol despite having no visible pattern.
        if (mark is "<" or ">" or "&" or "\u2800") return false;
        if (mark.Normalize(NormalizationForm.FormKC) is "+" or "-" or "\u2212" or "\u00b1" or
            "\u2213" or "\u2795" or "\u2796" or "\u02d6" or "\u02d7") return false;
        return Rune.IsPunctuation(rune) || Rune.IsSymbol(rune);
    }
}

public sealed class PunctuationPreferenceCodec : ISettingCodec<PunctuationPreference>
{
    public string Encode(PunctuationPreference value) => JsonSerializer.Serialize(value);

    public bool TryDecode(string encoded, out PunctuationPreference value)
    {
        value = PunctuationPreference.BuiltIn;
        try
        {
            using var document = JsonDocument.Parse(encoded);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3 ||
                !root.TryGetProperty(nameof(PunctuationPreference.Preset), out var preset) ||
                preset.ValueKind != JsonValueKind.Number || !preset.TryGetInt32(out int presetValue) ||
                !Enum.IsDefined((PunctuationPreset)presetValue) ||
                !root.TryGetProperty(nameof(PunctuationPreference.CustomGroup), out var group) ||
                group.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty(nameof(PunctuationPreference.CustomDecimal), out var mark) ||
                mark.ValueKind != JsonValueKind.String) return false;
            value = new((PunctuationPreset)presetValue, group.GetString()!, mark.GetString()!);
            return true;
        }
        catch (JsonException) { return false; }
    }
}
