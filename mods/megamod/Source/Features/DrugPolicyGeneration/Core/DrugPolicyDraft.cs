using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugTolerance.Core;

namespace Megamod.Features.DrugPolicyGeneration.Core;

public sealed record DrugGenerationPreferences(float InitialMood, float InitialJoy, DrugToleranceChoice InitialTolerance);

/// <summary>Detached preview editing only. Native publication must separately validate currentness and native limits.</summary>
public sealed class DrugPolicyDraft
{
    private readonly DrugSuggestionSet suggestions;

    private DrugPolicyDraft(string name, DrugSuggestionSet suggestions,
        IReadOnlyList<DrugSuggestionReview> rows, bool toleranceAvailable)
    {
        Name = name;
        this.suggestions = suggestions;
        Rows = rows;
        ToleranceAvailable = toleranceAvailable;
    }

    public string Name { get; }
    public IReadOnlyList<DrugSuggestionReview> Rows { get; }
    public bool ToleranceAvailable { get; }

    public static DrugPolicyDraft Create(string name, DrugSuggestionSet suggestions,
        DrugGenerationPreferences preferences, bool toleranceAvailable)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(suggestions);
        ArgumentNullException.ThrowIfNull(preferences);
        ValidateThreshold(preferences.InitialMood, nameof(preferences.InitialMood));
        ValidateThreshold(preferences.InitialJoy, nameof(preferences.InitialJoy));
        ValidateTolerance(preferences.InitialTolerance);
        var edits = suggestions.Rows.Select(row => new DrugGenerationEdit(row.Facts.Drug.Identity,
            row.Choices with { MoodThreshold = preferences.InitialMood, RecreationThreshold = preferences.InitialJoy },
            toleranceAvailable ? preferences.InitialTolerance : DrugToleranceChoice.Any)).ToArray();
        return new(name, suggestions, suggestions.ReviewEdits(edits), toleranceAvailable);
    }

    public DrugPolicyDraft Edit(object drugIdentity, DrugPolicyChoices choices, DrugToleranceChoice tolerance)
    {
        ArgumentNullException.ThrowIfNull(drugIdentity);
        ArgumentNullException.ThrowIfNull(choices);
        ValidateTolerance(tolerance);
        var target = Rows.SingleOrDefault(row => ReferenceEquals(row.Suggestion.Facts.Drug.Identity, drugIdentity));
        if (target == null) throw new ArgumentException("The edit does not identify a row in this draft.", nameof(drugIdentity));
        var drug = target.Suggestion.Facts.Drug;
        if ((choices.Addiction && !drug.Addictive) || (choices.Recreation && !drug.Pleasure) ||
            !float.IsFinite(choices.DaysFrequency) || choices.DaysFrequency <= 0 ||
            !ValidThreshold(choices.MoodThreshold) || !ValidThreshold(choices.RecreationThreshold) || choices.InventoryTarget < 0)
            throw new ArgumentException("The draft edit contains invalid or inapplicable policy choices.", nameof(choices));
        if (!ToleranceAvailable && tolerance.Kind != DrugToleranceChoiceKind.Any)
            throw new InvalidOperationException("Numeric tolerance editing requires current tolerance support.");
        var edits = Rows.Select(row => new DrugGenerationEdit(row.Suggestion.Facts.Drug.Identity,
            ReferenceEquals(row.Suggestion.Facts.Drug.Identity, drugIdentity) ? choices : row.Choices,
            ReferenceEquals(row.Suggestion.Facts.Drug.Identity, drugIdentity) ? tolerance : row.Tolerance)).ToArray();
        return new(Name, suggestions, suggestions.ReviewEdits(edits), ToleranceAvailable);
    }

    public DrugPolicyDraft Rename(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new(name, suggestions, Rows, ToleranceAvailable);
    }

    public DrugPolicyDraft Reset(string name, DrugSuggestionSet suggestions,
        DrugGenerationPreferences preferences, bool toleranceAvailable) => Create(name, suggestions, preferences, toleranceAvailable);

    private static bool ValidThreshold(float value) => float.IsFinite(value) && value >= 0 && value <= 1;

    private static void ValidateThreshold(float value, string name)
    {
        if (!ValidThreshold(value)) throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateTolerance(DrugToleranceChoice tolerance)
    {
        ArgumentNullException.ThrowIfNull(tolerance);
        if (tolerance.Kind == DrugToleranceChoiceKind.NeedsReview)
            throw new ArgumentException("An unresolved saved tolerance value cannot initialize a new policy choice.", nameof(tolerance));
    }
}
