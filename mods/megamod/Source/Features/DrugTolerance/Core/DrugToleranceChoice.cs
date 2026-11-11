using System;
using System.Globalization;

namespace Megamod.Features.DrugTolerance.Core;

public enum DrugToleranceChoiceKind { Any, Limit, NeedsReview }

/// <summary>An immutable row choice, retaining unrecognized save data until an explicit edit.</summary>
public sealed class DrugToleranceChoice
{
    private DrugToleranceChoice(DrugToleranceChoiceKind kind, float limit, string? encoding)
    {
        Kind = kind;
        Limit = limit;
        Encoding = encoding;
    }

    public static DrugToleranceChoice Any { get; } = new(DrugToleranceChoiceKind.Any, 0f, "1:any");
    public DrugToleranceChoiceKind Kind { get; }
    public float Limit { get; }
    public string? Encoding { get; }

    public static DrugToleranceChoice Numeric(float limit)
    {
        if (!float.IsFinite(limit) || limit < 0f || limit > 1f)
            throw new ArgumentOutOfRangeException(nameof(limit));
        return new(DrugToleranceChoiceKind.Limit, limit, "1:limit:" + limit.ToString("R", CultureInfo.InvariantCulture));
    }

    public static DrugToleranceChoice Decode(string? encoding)
    {
        if (encoding == null || encoding == "1:any")
            return new(DrugToleranceChoiceKind.Any, 0f, encoding);
        const string prefix = "1:limit:";
        if (encoding.StartsWith(prefix, StringComparison.Ordinal) &&
            float.TryParse(encoding.Substring(prefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out var limit) &&
            float.IsFinite(limit) && limit >= 0f && limit <= 1f)
            return new(DrugToleranceChoiceKind.Limit, limit, encoding);
        return new(DrugToleranceChoiceKind.NeedsReview, 0f, encoding);
    }
}
