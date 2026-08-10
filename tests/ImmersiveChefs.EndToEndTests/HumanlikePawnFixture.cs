using System;
using System.Globalization;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

internal static class HumanlikePawnFixture
{
    internal static void SetName(Pawn pawn, string shortLabel)
    {
        if (pawn is null)
        {
            throw new ArgumentNullException(nameof(pawn));
        }
        if (pawn.RaceProps?.Humanlike != true)
        {
            throw new ArgumentException("Only humanlike E2E pawns use native triple names.", nameof(pawn));
        }

        if (string.IsNullOrWhiteSpace(shortLabel))
        {
            throw new ArgumentException("A humanlike E2E pawn requires a visible short label.", nameof(shortLabel));
        }

        var stableLabel = shortLabel.Trim();
        var nativeName = new NameTriple(
            "E2E",
            stableLabel,
            "Fixture" + pawn.thingIDNumber.ToString(CultureInfo.InvariantCulture));
        if (!nativeName.IsValid || !string.Equals(nativeName.ToStringShort, stableLabel, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The humanlike E2E pawn name is not a valid stable NameTriple.");
        }

        pawn.Name = nativeName;
    }
}
