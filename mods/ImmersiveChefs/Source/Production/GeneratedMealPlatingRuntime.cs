using RimWorld;
using Verse;

namespace ImmersiveChefs;

internal static class GeneratedMealPlatingRuntime
{
    private static IReadOnlyList<GeneratedPlateCandidate>? cachedCandidates;

    internal static int EnsurePlated(Thing thing, GeneratedMealOrigin origin)
    {
        if (!GeneratedMealPlatePolicy.AllowsOrigin(origin) ||
            thing is not ThingWithComps meal ||
            meal.GetComp<CompEmbeddedWare>() is not { } embedded)
        {
            return 0;
        }

        var missing = GeneratedMealPlatePolicy.MissingPlateCount(
            MealCoveragePolicy.IsCovered(meal.def),
            meal.stackCount,
            embedded.EmbeddedPlateCount);
        if (missing == 0)
        {
            return 0;
        }

        var selected = GeneratedMealPlatePolicy.SelectForService(
            MealComplexityRuntime.Classify(meal.def),
            Candidates());
        if (!selected.HasValue)
        {
            return 0;
        }

        var attached = 0;
        for (var index = 0; index < missing; index++)
        {
            var plate = CreatePlate(selected.Value);
            if (plate is null)
            {
                break;
            }

            if (embedded.TryEmbedPlate(plate))
            {
                attached++;
                continue;
            }

            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }

            break;
        }

        return attached;
    }

    internal static void EnsurePlated(
        IEnumerable<Thing>? things,
        GeneratedMealOrigin origin)
    {
        if (!GeneratedMealPlatePolicy.AllowsOrigin(origin) || things is null)
        {
            return;
        }

        foreach (var thing in things.ToList())
        {
            try
            {
                EnsurePlated(thing, origin);
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(
                    "[ImmersiveChefs] Could not attach generated meal plates for " +
                    origin + ": " + exception.GetType().Name + ": " + exception.Message,
                    GenText.StableStringHash(
                        "ImmersiveChefs.GeneratedMealPlating." + origin + "." +
                        exception.GetType().FullName));
            }
        }
    }

    private static Thing? CreatePlate(GeneratedPlateCandidate candidate)
    {
        var plateDef = DefDatabase<ThingDef>.GetNamedSilentFail(candidate.PlateDefName);
        var stuff = candidate.StuffDefName is null
            ? null
            : DefDatabase<ThingDef>.GetNamedSilentFail(candidate.StuffDefName);
        if (plateDef is null || (plateDef.MadeFromStuff && stuff is null))
        {
            return null;
        }

        var plate = ThingMaker.MakeThing(plateDef, stuff);
        (plate as ThingWithComps)?.GetComp<CompSanitation>()
            ?.MarkClean(WashProvenance.None);
        return plate;
    }

    private static IReadOnlyList<GeneratedPlateCandidate> Candidates()
    {
        if (cachedCandidates is not null)
        {
            return cachedCandidates;
        }

        var classifier = OptionalMaterialAdapter.CreateClassifier();
        var candidates = new List<GeneratedPlateCandidate>();
        foreach (var plateDef in DefDatabase<ThingDef>.AllDefsListForReading
                     .Where(definition =>
                         definition.GetModExtension<KitchenwareExtension>() is
                         { product: KitchenwareProduct.Plate }))
        {
            var extension = plateDef.GetModExtension<KitchenwareExtension>()!;
            if (extension.fixedMaterialKind is { } fixedMaterial)
            {
                candidates.Add(new GeneratedPlateCandidate(
                    plateDef.defName,
                    null,
                    fixedMaterial,
                    plateDef.GetStatValueAbstract(StatDefOf.MarketValue, null)));
                continue;
            }

            if (!plateDef.MadeFromStuff)
            {
                continue;
            }

            foreach (var stuff in GenStuff.AllowedStuffsFor(plateDef))
            {
                var categories = stuff.stuffProps?.categories;
                var material = classifier.Classify(
                    new KitchenMaterialDescriptor(
                        stuff.defName,
                        categories?.Any(category => category.defName.Equals(
                            "Metallic",
                            StringComparison.OrdinalIgnoreCase)) == true,
                        categories?.Any(category => category.defName.Equals(
                            "Woody",
                            StringComparison.OrdinalIgnoreCase)) == true,
                        categories?.Any(category => category.defName.Equals(
                            "Stony",
                            StringComparison.OrdinalIgnoreCase)) == true),
                    KitchenwareProduct.Plate);
                if (material is null)
                {
                    continue;
                }

                candidates.Add(new GeneratedPlateCandidate(
                    plateDef.defName,
                    stuff.defName,
                    material.Kind,
                    stuff.GetStatValueAbstract(StatDefOf.MarketValue, null)));
            }
        }

        cachedCandidates = candidates
            .OrderBy(candidate => candidate.PlateDefName, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.StuffDefName ?? string.Empty, StringComparer.Ordinal)
            .ToArray();
        return cachedCandidates;
    }
}
