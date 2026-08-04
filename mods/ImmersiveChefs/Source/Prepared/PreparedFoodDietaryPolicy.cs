namespace ImmersiveChefs;

public static class PreparedFoodDietaryPolicy
{
    public static bool AllSourcesAllowed(
        IEnumerable<string> sourceDefNames,
        Func<string, bool> sourceAllowed)
    {
        if (sourceDefNames is null)
        {
            throw new ArgumentNullException(nameof(sourceDefNames));
        }

        if (sourceAllowed is null)
        {
            throw new ArgumentNullException(nameof(sourceAllowed));
        }

        var any = false;
        foreach (var sourceDefName in sourceDefNames)
        {
            if (string.IsNullOrWhiteSpace(sourceDefName) || !sourceAllowed(sourceDefName))
            {
                return false;
            }

            any = true;
        }

        return any;
    }

    public static IReadOnlyList<IngredientContribution> CreatePasteContributions(
        IEnumerable<string> sourceDefNames,
        float totalNutrition)
    {
        if (sourceDefNames is null)
        {
            throw new ArgumentNullException(nameof(sourceDefNames));
        }

        var distinctSources = sourceDefNames
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(source => source, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinctSources.Count == 0)
        {
            distinctSources.Add("MealNutrientPaste");
        }

        var boundedNutrition = Math.Max(0f, totalNutrition);
        var nutritionPerSource = boundedNutrition / distinctSources.Count;
        return distinctSources
            .Select(source => new IngredientContribution(source, nutritionPerSource, 1, 50))
            .ToList()
            .AsReadOnly();
    }

    public static IReadOnlyList<string> VisibleSourceDefNames(
        IEnumerable<string> sourceDefNames,
        bool exactSourcesHidden)
    {
        if (sourceDefNames is null)
        {
            throw new ArgumentNullException(nameof(sourceDefNames));
        }

        if (exactSourcesHidden)
        {
            return new[] { "MealNutrientPaste" };
        }

        return sourceDefNames
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(source => source, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }
}
