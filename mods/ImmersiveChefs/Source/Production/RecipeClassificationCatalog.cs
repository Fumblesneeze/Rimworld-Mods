namespace ImmersiveChefs;

public enum MealComplexity
{
    Simple,
    Advanced,
    Elaborate
}

public sealed class RecipeClassificationCatalog
{
    private readonly Dictionary<string, MealComplexity> classifications;

    private RecipeClassificationCatalog(Dictionary<string, MealComplexity> classifications)
    {
        this.classifications = classifications;
    }

    public static RecipeClassificationCatalog CreateVanilla()
    {
        var table = new Dictionary<string, MealComplexity>(StringComparer.OrdinalIgnoreCase);
        Add(table, MealComplexity.Simple,
            "CookMealSimple", "CookMealSimpleBulk");
        Add(table, MealComplexity.Advanced,
            "CookMealFine", "CookMealFine_Veg", "CookMealFine_Meat",
            "CookMealFineBulk", "CookMealFineBulk_Meat", "CookMealFineBulk_Veg");
        Add(table, MealComplexity.Elaborate,
            "CookMealLavish", "CookMealLavish_Meat", "CookMealLavish_Veg",
            "CookMealLavishBulk", "CookMealLavishBulk_Veg", "CookMealLavishBulk_Meat");
        return new RecipeClassificationCatalog(table);
    }

    public MealComplexity? Classify(string recipeDefName)
    {
        return classifications.TryGetValue(recipeDefName, out var complexity)
            ? complexity
            : null;
    }

    public void Register(string recipeDefName, MealComplexity complexity)
    {
        if (string.IsNullOrWhiteSpace(recipeDefName))
        {
            throw new ArgumentException("A recipe Def name is required.", nameof(recipeDefName));
        }

        classifications[recipeDefName] = complexity;
    }

    public float AdjustWorkAmount(
        string recipeDefName,
        float originalWorkAmount,
        ImmersiveChefsSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var complexity = Classify(recipeDefName);
        if (!complexity.HasValue)
        {
            return originalWorkAmount;
        }

        var multiplier = complexity.Value switch
        {
            MealComplexity.Simple => settings.SimpleRecipeTimeMultiplier,
            MealComplexity.Advanced => settings.AdvancedRecipeTimeMultiplier,
            MealComplexity.Elaborate => settings.ElaborateRecipeTimeMultiplier,
            _ => 1f
        };
        return originalWorkAmount * multiplier;
    }

    private static void Add(
        IDictionary<string, MealComplexity> target,
        MealComplexity complexity,
        params string[] recipeDefNames)
    {
        foreach (var recipeDefName in recipeDefNames)
        {
            target[recipeDefName] = complexity;
        }
    }
}
