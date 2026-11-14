using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Megamod.Features.ModSettingsNavigation;

namespace Megamod.Features.SettingsSearch;

// Detached displayed text. Page/path identities are supplied by the native observer, not labels.
public sealed record SettingsSearchEntry(SettingsPage Page, string PathIdentity, int Occurrence,
    string Label, string? Help = null, string? DisplayedValue = null, string? Tab = null,
    string? Feature = null, string? Category = null, bool IsPage = false);

public sealed record SettingsSearchHit(SettingsSearchEntry Entry, bool UsesTypo, bool LabelMatch);

public sealed class SettingsSearch
{
    private static readonly Regex Tokens = new("(?:[^\\s\"]+|\"[^\"]*(?:\"|$))+",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex LineBreaks = new("<br\\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Tags = new("</?[A-Za-z][A-Za-z0-9-]*(?:\\s+[^<>]*|=[^<>]*)?>",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex Whitespace = new("\\s+", RegexOptions.CultureInvariant);
    private static readonly string[] FieldNames = { "", "mod", "author", "id", "tab" };
    private readonly Document[] entries;
    private sealed record Term(string Value, string Field, bool Excluded, bool Phrase);
    private sealed record Document(SettingsSearchEntry Entry, string Label, Dictionary<string, string[]> Fields);

    public SettingsSearch(IEnumerable<SettingsSearchEntry> entries) => this.entries = entries.Select(entry =>
        new Document(entry, Normalize(Label(entry)), FieldNames.ToDictionary(field => field,
            field => Context(entry, field).Select(Normalize).Where(value => value.Length > 0).Distinct().ToArray())))
        .ToArray();

    public IReadOnlyList<SettingsSearchHit> Find(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<SettingsSearchHit>();
        var terms = Parse(query);
        if (terms.Length == 0) return Array.Empty<SettingsSearchHit>();
        var typos = terms.Where(term => !term.Excluded && !term.Phrase && term.Field.Length == 0 &&
                term.Value.EnumerateRunes().All(IsWordCharacter) && term.Value.EnumerateRunes().Count() >= 5 &&
                !entries.Any(entry => entry.Fields[""].Any(value => Contains(value, term.Value))))
            .Distinct().ToDictionary(term => term, term => term.Value.EnumerateRunes().Count() < 9 ? 1 : 2);
        var hits = new List<SettingsSearchHit>();
        foreach (var entry in entries)
        {
            var accepted = true;
            var usesTypo = false;
            var labelMatch = false;
            foreach (var term in terms)
            {
                var exact = entry.Fields[term.Field].Any(value => Contains(value, term.Value));
                if (term.Excluded)
                {
                    if (exact) { accepted = false; break; }
                    continue;
                }
                if (exact)
                {
                    labelMatch |= term.Field.Length == 0 && Contains(entry.Label, term.Value);
                    continue;
                }
                if (!typos.TryGetValue(term, out var edits) ||
                    !entry.Fields[""].Any(value => MatchesTypo(value, term.Value, edits)))
                { accepted = false; break; }
                usesTypo = true;
                labelMatch |= MatchesTypo(entry.Label, term.Value, edits);
            }
            if (accepted) hits.Add(new SettingsSearchHit(entry.Entry, usesTypo, labelMatch));
        }
        return Array.AsReadOnly(hits
            .OrderBy(hit => hit.UsesTypo).ThenByDescending(hit => hit.LabelMatch)
            .ThenBy(hit => hit.Entry.Page.PackageIdentity, StringComparer.Ordinal)
            .ThenBy(hit => hit.Entry.Page.PageIdentity, StringComparer.Ordinal)
            .ThenBy(hit => hit.Entry.PathIdentity, StringComparer.Ordinal)
            .ThenBy(hit => hit.Entry.Occurrence).ToArray());
    }

    private static Term[] Parse(string query) => Tokens.Matches(query).Select(match =>
    {
        var value = match.Value;
        var excluded = value.Length > 1 && value[0] == '-';
        if (excluded) value = value.Substring(1);
        var colon = value.IndexOf(':');
        var field = colon < 0 ? string.Empty : value.Substring(0, colon).ToLowerInvariant();
        if (field is "mod" or "author" or "id" or "tab") value = value.Substring(colon + 1);
        else field = string.Empty;
        var phrase = value.StartsWith('"');
        if (phrase)
        {
            value = value.Substring(1);
            if (value.EndsWith('"')) value = value.Substring(0, value.Length - 1);
        }
        return new Term(Normalize(value), field, excluded, phrase);
    }).Where(term => term.Value.Length > 0).ToArray();

    private static IEnumerable<string?> Context(SettingsSearchEntry entry, string field) => field switch
    {
        "mod" => new[] { entry.Page.Title, entry.Page.ModName },
        "author" => new[] { entry.Page.RawAuthors },
        "id" => new[] { entry.Page.PackageIdentity },
        "tab" => entry.IsPage ? Array.Empty<string>() : new[] { entry.Tab },
        _ when entry.IsPage => new[]
        {
            entry.Page.Title, entry.Page.ModName, entry.Page.RawAuthors, entry.Page.PackageIdentity
        },
        _ => new[]
        {
            entry.Label, entry.Help, entry.DisplayedValue, entry.Tab, entry.Feature, entry.Category,
            entry.Page.Title, entry.Page.ModName, entry.Page.RawAuthors, entry.Page.PackageIdentity
        }
    };

    private static string? Label(SettingsSearchEntry entry) => !entry.IsPage ? entry.Label :
        !string.IsNullOrEmpty(entry.Page.Title) ? entry.Page.Title :
        !string.IsNullOrEmpty(entry.Page.ModName) ? entry.Page.ModName : entry.Page.PackageIdentity;

    private static bool Contains(string value, string term) => value.Contains(term, StringComparison.Ordinal);

    private static string Normalize(string? value) => Whitespace.Replace(
        Tags.Replace(LineBreaks.Replace(value ?? string.Empty, " "), string.Empty), " ")
        .Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant().ToUpperInvariant();

    private static bool MatchesTypo(string text, string term, int edits)
    {
        var requested = term.EnumerateRunes().Select(rune => rune.Value).ToArray();
        foreach (var candidate in Words(text))
        {
            if (WithinEdits(requested, 0, candidate, 0, edits)) return true;
        }
        return false;
    }

    private static bool IsWordCharacter(Rune value) => Rune.IsLetterOrDigit(value) ||
        Rune.GetUnicodeCategory(value) is UnicodeCategory.NonSpacingMark or
            UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;

    private static IEnumerable<int[]> Words(string text)
    {
        var word = new List<int>();
        foreach (var value in text.EnumerateRunes())
        {
            if (IsWordCharacter(value)) word.Add(value.Value);
            else if (word.Count > 0) { yield return word.ToArray(); word.Clear(); }
        }
        if (word.Count > 0) yield return word.ToArray();
    }

    // At most two edits: bounded branching avoids a word-length-squared distance matrix.
    // Swaps on either side also cover insertion/deletion between the transposed characters.
    private static bool WithinEdits(int[] left, int l, int[] right, int r, int edits)
    {
        if (Math.Abs((left.Length - l) - (right.Length - r)) > edits) return false;
        while (l < left.Length && r < right.Length && left[l] == right[r]) { l++; r++; }
        if (l == left.Length || r == right.Length) return left.Length - l + right.Length - r <= edits;
        if (edits == 0) return false;
        if (WithinEdits(left, l + 1, right, r + 1, edits - 1) ||
            WithinEdits(left, l + 1, right, r, edits - 1) ||
            WithinEdits(left, l, right, r + 1, edits - 1)) return true;
        if (l + 1 < left.Length)
        {
            (left[l], left[l + 1]) = (left[l + 1], left[l]);
            var result = WithinEdits(left, l, right, r, edits - 1);
            (left[l], left[l + 1]) = (left[l + 1], left[l]);
            if (result) return true;
        }
        if (r + 1 < right.Length)
        {
            (right[r], right[r + 1]) = (right[r + 1], right[r]);
            var result = WithinEdits(left, l, right, r, edits - 1);
            (right[r], right[r + 1]) = (right[r + 1], right[r]);
            if (result) return true;
        }
        return false;
    }
}
