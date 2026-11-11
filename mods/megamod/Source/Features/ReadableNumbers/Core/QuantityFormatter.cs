using System.Text;

namespace Megamod.Features.ReadableNumbers;

/// <summary>A caller-identified, ungrouped decimal token; affixes are not parsed as quantities.</summary>
public sealed record RawDecimalQuantity(string Token, string Prefix = "", string Suffix = "")
{
    public string OriginalText => Prefix + Token + Suffix;
}

public enum NumberGrouping { ThreeDigits, ThreeThenTwoDigits, FourDigits, None }

public sealed record NumberFormatOptions(NumberGrouping Grouping = NumberGrouping.ThreeDigits,
    int MinimumIntegerDigits = 4, string GroupSeparator = ",", string DecimalMark = ".",
    bool ApplyDecimalToSmallNumbers = true);

public static class QuantityFormatter
{
    public static string Format(RawDecimalQuantity quantity, NumberFormatOptions? options = null)
    {
        options ??= new();
        string token = quantity.Token;
        int start = token.StartsWith('-') || token.StartsWith('+') ? 1 : 0;
        int integerEnd = token.IndexOf('.');
        if (integerEnd < 0) integerEnd = token.Length;
        if (integerEnd <= start || integerEnd == token.Length - 1 && token[integerEnd] == '.')
            return quantity.OriginalText;
        for (int i = start; i < token.Length; i++)
            if (i != integerEnd && token[i] is < '0' or > '9') return quantity.OriginalText;
        bool qualifies = integerEnd - start >= options.MinimumIntegerDigits;
        var result = new StringBuilder(quantity.Prefix);
        result.Append(token.AsSpan(0, start));
        for (int i = start; i < integerEnd; i++)
        {
            int remaining = integerEnd - i;
            bool boundary = options.Grouping switch
            {
                NumberGrouping.ThreeDigits => remaining % 3 == 0,
                NumberGrouping.ThreeThenTwoDigits => remaining >= 3 && (remaining - 3) % 2 == 0,
                NumberGrouping.FourDigits => remaining % 4 == 0,
                _ => false
            };
            if (qualifies && i > start && boundary)
                result.Append(options.GroupSeparator == " " ? "\u00a0" : options.GroupSeparator);
            result.Append(token[i]);
        }
        if (integerEnd < token.Length)
        {
            result.Append(qualifies || options.ApplyDecimalToSmallNumbers ? options.DecimalMark : ".");
            result.Append(token.AsSpan(integerEnd + 1));
        }
        result.Append(quantity.Suffix);
        return result.ToString();
    }
}
