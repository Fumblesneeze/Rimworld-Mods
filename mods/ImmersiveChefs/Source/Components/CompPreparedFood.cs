using RimWorld;
using Verse;

namespace ImmersiveChefs;

public sealed class CompProperties_PreparedFood : CompProperties
{
    public CompProperties_PreparedFood()
    {
        compClass = typeof(CompPreparedFood);
    }
}

public sealed class CompPreparedFood : ThingComp
{
    private List<PreparedContributionData> contributions = new();
    private int preparationQuality = 50;
    private string preparerThingId = string.Empty;
    private DietaryFlags dietaryFlags;
    private bool exactSourcesHidden;
    private float ingredientPoisonChance;

    public int PreparationQuality => preparationQuality;
    public string? PreparerThingId => string.IsNullOrEmpty(preparerThingId) ? null : preparerThingId;
    public DietaryFlags DietaryFlags => dietaryFlags;
    public bool ExactSourcesHidden => exactSourcesHidden;
    public float IngredientPoisonChance => ingredientPoisonChance;
    public IReadOnlyList<IngredientContribution> Contributions =>
        contributions.Select(value => value.ToContribution()).ToList().AsReadOnly();

    public float NutritionPerItem => Contributions.Sum(value => value.Nutrition);

    public void Initialize(PreparedFoodState state)
    {
        contributions = state.Contributions.Select(PreparedContributionData.From).ToList();
        preparationQuality = state.PreparationQuality;
        preparerThingId = state.PreparerThingId ?? string.Empty;
        dietaryFlags = state.DietaryFlags;
        exactSourcesHidden = state.ExactSourcesHidden;
        ingredientPoisonChance = state.IngredientPoisonChance;
    }

    public bool CompatibleWith(CompPreparedFood other)
    {
        if (preparationQuality != other.preparationQuality ||
            !string.Equals(preparerThingId, other.preparerThingId, StringComparison.Ordinal) ||
            dietaryFlags != other.dietaryFlags ||
            exactSourcesHidden != other.exactSourcesHidden ||
            Math.Abs(ingredientPoisonChance - other.ingredientPoisonChance) > 0.0001f ||
            contributions.Count != other.contributions.Count)
        {
            return false;
        }

        return contributions.OrderBy(value => value.DefName, StringComparer.Ordinal)
            .Zip(other.contributions.OrderBy(value => value.DefName, StringComparer.Ordinal),
                (left, right) => left.Equivalent(right))
            .All(equal => equal);
    }

    public override void PostSplitOff(Thing piece)
    {
        if (ReferenceEquals(piece, parent))
        {
            return;
        }

        var target = (piece as ThingWithComps)?.GetComp<CompPreparedFood>();
        if (target is null)
        {
            return;
        }

        target.contributions = contributions.Select(value => value.Copy()).ToList();
        target.preparationQuality = preparationQuality;
        target.preparerThingId = preparerThingId;
        target.dietaryFlags = dietaryFlags;
        target.exactSourcesHidden = exactSourcesHidden;
        target.ingredientPoisonChance = ingredientPoisonChance;
    }

    public override string CompInspectStringExtra()
    {
        var sources = exactSourcesHidden
            ? "ImmersiveChefs_PreparedSource_NutrientPaste".Translate().ToString()
            : string.Join(", ", contributions
                .Select(value => DefDatabase<ThingDef>.GetNamedSilentFail(value.DefName)?.LabelCap.ToString() ??
                                 "ImmersiveChefs_PreparedSource_Unknown".Translate().ToString())
                .Distinct());
        return "ImmersiveChefs_PreparedFoodInspect".Translate(
            preparationQuality,
            sources,
            NutritionPerItem.ToString("0.###"));
    }

    public override void PostExposeData()
    {
        List<PreparedContributionData>? persisted = contributions;
        Scribe_Collections.Look(ref persisted, "preparedContributions", LookMode.Deep);
        contributions = persisted ?? new List<PreparedContributionData>();
        Scribe_Values.Look(ref preparationQuality, "preparationQuality", 50);
        string? persistedPreparer = preparerThingId;
        Scribe_Values.Look(ref persistedPreparer, "preparerThingId");
        preparerThingId = persistedPreparer ?? string.Empty;
        Scribe_Values.Look(ref dietaryFlags, "dietaryFlags", DietaryFlags.None);
        Scribe_Values.Look(ref exactSourcesHidden, "exactSourcesHidden", false);
        Scribe_Values.Look(ref ingredientPoisonChance, "ingredientPoisonChance", 0f);
    }

    private sealed class PreparedContributionData : IExposable
    {
        private string defName = string.Empty;
        private float nutrition;
        private int sourceCount = 1;
        private int craftsmanshipScore = 50;

        public string DefName => defName;

        public void ExposeData()
        {
            string? persistedDefName = defName;
            Scribe_Values.Look(ref persistedDefName, "defName");
            defName = persistedDefName ?? string.Empty;
            Scribe_Values.Look(ref nutrition, "nutrition", 0f);
            Scribe_Values.Look(ref sourceCount, "sourceCount", 1);
            Scribe_Values.Look(ref craftsmanshipScore, "craftsmanshipScore", 50);
        }

        public IngredientContribution ToContribution() => new(defName, nutrition, sourceCount, craftsmanshipScore);

        public bool Equivalent(PreparedContributionData other) =>
            defName == other.defName &&
            Math.Abs(nutrition - other.nutrition) < 0.0001f &&
            sourceCount == other.sourceCount &&
            craftsmanshipScore == other.craftsmanshipScore;

        public PreparedContributionData Copy() => From(ToContribution());

        public static PreparedContributionData From(IngredientContribution contribution) => new()
        {
            defName = contribution.DefName,
            nutrition = contribution.Nutrition,
            sourceCount = contribution.SourceCount,
            craftsmanshipScore = contribution.CraftsmanshipScore
        };
    }
}
