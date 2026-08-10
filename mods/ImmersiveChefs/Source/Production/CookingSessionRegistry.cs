using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal sealed class ReservedWarePortion
{
    internal ReservedWarePortion(Thing thing, int count)
    {
        Thing = thing;
        Count = count;
        RemainingCount = count;
        var sanitation = (thing as ThingWithComps)?.GetComp<CompSanitation>();
        WasDirty = sanitation?.IsDirty == true;
        WasWildWaterWashed = sanitation?.WashedInWildWater == true;
    }

    internal Thing Thing { get; private set; }
    internal int Count { get; }
    internal bool WasDirty { get; }
    internal bool WasWildWaterWashed { get; }
    internal int RemainingCount { get; private set; }

    internal void ReplaceWithHeldThing(Thing thing)
    {
        Thing = thing;
    }

    internal Thing? TryEmbedOne(CompEmbeddedWare target)
    {
        if (RemainingCount <= 0 || Thing.Destroyed || !target.TryEmbedPlate(Thing))
        {
            return null;
        }

        RemainingCount--;
        var embedded = target.PeekPlateThing();
        (embedded as ThingWithComps)?.GetComp<CompSanitation>()?.ClearSessionTransfer();
        return embedded;
    }
}

internal sealed class CookingSession
{
    internal CookingSession(
        Pawn pawn,
        Job job,
        RecipeDef reservationRecipe,
        Thing billGiver,
        ReservedWarePortion? cookware,
        IReadOnlyList<ReservedWarePortion> plates,
        bool emergencyMissingWare,
        bool wareExempt)
    {
        Pawn = pawn;
        Job = job;
        ReservationRecipe = reservationRecipe;
        BillGiver = billGiver;
        Cookware = cookware;
        Plates = plates;
        EmergencyMissingWare = emergencyMissingWare;
        WareExempt = wareExempt;
        PreparedWorkFactor = PreparedFoodCalculator.WorkFactor(
            PreparedFoodRuntime.PreparedNutritionFraction(job),
            ImmersiveChefsMod.Settings.PreparedWorkReduction);
    }

    internal Pawn Pawn { get; }
    internal Job Job { get; }
    internal RecipeDef ReservationRecipe { get; }
    internal Thing BillGiver { get; }
    internal ReservedWarePortion? Cookware { get; }
    internal IReadOnlyList<ReservedWarePortion> Plates { get; }
    internal bool EmergencyMissingWare { get; }
    internal bool WareExempt { get; }
    internal bool WorkStarted { get; private set; }
    internal bool ProductsCompleted { get; private set; }
    internal float PreparedWorkFactor { get; }
    internal AssistantContributionAccumulator AssistantContribution { get; } = new();
    private FinalProductLedger<Thing> FinalizedProducts { get; } = new();

    internal float NotifyWorkTick()
    {
        if (!WorkStarted)
        {
            WorkStarted = true;
            (Cookware?.Thing as ThingWithComps)?.GetComp<CompSanitation>()?.MarkDirty();
        }

        AssistantContribution.RecordLeadTick(
            KitchenAssistanceRegistry.ActiveSkills(Job),
            ImmersiveChefsMod.Settings.AssistantEffectScale);
        return AssistantContribution.CurrentSpeedBonus;
    }

    internal IEnumerable<ReservedWarePortion> PortionsToCollect()
    {
        if (Cookware is not null)
        {
            yield return Cookware;
        }

        foreach (var plate in Plates)
        {
            yield return plate;
        }
    }

    internal void Pickup(Pawn pawn, ReservedWarePortion portion)
    {
        var source = portion.Thing;
        if (source.Destroyed)
        {
            return;
        }

        if (source.ParentHolder == pawn.inventory?.innerContainer)
        {
            (source as ThingWithComps)?.GetComp<CompSanitation>()?.MarkSessionTransferredWare();
            return;
        }

        var picked = portion.Count < source.stackCount ? source.SplitOff(portion.Count) : source;
        portion.ReplaceWithHeldThing(picked);
        if (picked.Spawned)
        {
            picked.DeSpawn(DestroyMode.Vanish);
        }

        if (pawn.inventory?.innerContainer.TryAdd(picked, canMergeWithExistingStacks: false) == true)
        {
            (picked as ThingWithComps)?.GetComp<CompSanitation>()?.MarkSessionTransferredWare();
            return;
        }

        if (pawn.MapHeld is { } map)
        {
            GenPlace.TryPlaceThing(picked, pawn.PositionHeld, map, ThingPlaceMode.Near);
        }
    }

    internal void ApplyToProduct(
        Thing product,
        RecipeDef finalRecipe,
        IReadOnlyList<Thing> ingredients)
    {
        if (!MealCoveragePolicy.IsCovered(product.def) ||
            (AdaptiveMealBillAdapter.Controls(Job.RecipeDef) &&
             !ReferenceEquals(finalRecipe, ReservationRecipe)) ||
            !FinalizedProducts.TryBegin(product))
        {
            return;
        }

        var embeddedWare = (product as ThingWithComps)?.GetComp<CompEmbeddedWare>();
        var culinaryState = (product as ThingWithComps)?.GetComp<CompCulinaryState>();
        RestorePreparedIngredientProvenance(product, ingredients);
        var qualityScore = CalculateQuality(ingredients);
        if (OvercookedMealsAdapter.IsFinalSurvivor(product))
        {
            qualityScore = OvercookedMealQualityPolicy.Apply(qualityScore);
        }
        var currentTick = Find.TickManager?.TicksGame ?? 0;
        var records = new List<CulinaryServingRecord>();
        var hiddenPrepared = ingredients
            .OfType<ThingWithComps>()
            .Select(ingredient => ingredient.GetComp<CompPreparedFood>())
            .Where(prepared => prepared?.ExactSourcesHidden == true)
            .Cast<CompPreparedFood>()
            .ToList();
        var hiddenSourceDefNames = hiddenPrepared
            .SelectMany(prepared => prepared.Contributions)
            .Select(contribution => contribution.DefName)
            .ToList();
        var hiddenDietaryFlags = hiddenPrepared.Aggregate(
            DietaryFlags.None,
            (flags, prepared) => flags | prepared.DietaryFlags);

        for (var index = 0; index < product.stackCount; index++)
        {
            var contamination = WareExempt
                ? ContaminationSources.None
                : SanitationContamination.ForCookware(
                    Cookware?.WasDirty == true,
                    Cookware?.WasWildWaterWashed == true
                        ? WashProvenance.WildWater
                        : WashProvenance.Safe);

            var embeddedPlate = embeddedWare is null ? null : TryEmbedNextPlate(embeddedWare);
            if (embeddedPlate is not null)
            {
                var sanitation = (embeddedPlate as ThingWithComps)?.GetComp<CompSanitation>();
                contamination |= SanitationContamination.ForPlate(
                    sanitation?.IsDirty == true,
                    sanitation?.WashProvenance ?? WashProvenance.None);
            }
            else if (!WareExempt)
            {
                contamination |= ContaminationSources.EmergencyUnplated;
            }

            var ownsTemperature = TemperatureOwnership.ImmersiveChefsFeaturesActive;
            records.Add(new CulinaryServingRecord(
                qualityScore,
                ownsTemperature ? 70f : 21f,
                contamination,
                microwaveReheatCount: 0,
                lastThermalTick: ownsTemperature ? currentTick : 0,
                hiddenSourceDefNames,
                hiddenDietaryFlags));
        }

        culinaryState?.ReplaceServings(records);
    }

    private Thing? TryEmbedNextPlate(CompEmbeddedWare embeddedWare)
    {
        foreach (var portion in Plates)
        {
            if (portion.TryEmbedOne(embeddedWare) is { } plate)
            {
                return plate;
            }
        }

        return null;
    }

    private static void RestorePreparedIngredientProvenance(Thing product, IEnumerable<Thing> ingredients)
    {
        var compIngredients = (product as ThingWithComps)?.GetComp<CompIngredients>();
        if (compIngredients is null)
        {
            return;
        }

        foreach (var ingredient in ingredients)
        {
            var prepared = (ingredient as ThingWithComps)?.GetComp<CompPreparedFood>();
            if (prepared is null)
            {
                continue;
            }

            compIngredients.ingredients.Remove(ingredient.def);
            foreach (var sourceDefName in PreparedFoodDietaryPolicy.VisibleSourceDefNames(
                         prepared.Contributions.Select(contribution => contribution.DefName),
                         prepared.ExactSourcesHidden))
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(sourceDefName) is { } sourceDef)
                {
                    compIngredients.RegisterIngredient(sourceDef);
                }
            }
        }
    }

    internal void MarkProductsCompleted()
    {
        if (ProductsCompleted)
        {
            return;
        }

        ProductsCompleted = true;
        UrgentProductionRequestRegistry.Consume(BillGiver.MapHeld);
    }

    internal void ReleaseAtEnd()
    {
        var map = BillGiver.Map ?? Pawn.MapHeld;
        if (map is null)
        {
            return;
        }

        if (Cookware?.Thing is { Destroyed: false } cookware)
        {
            DropExact(
                cookware,
                WorkStarted && BillGiver.Spawned ? BillGiver.Position : Pawn.PositionHeld,
                map);
        }

        foreach (var plate in Plates.Where(portion =>
                     portion.RemainingCount > 0 && !portion.Thing.Destroyed))
        {
            DropExact(plate.Thing, Pawn.PositionHeld, map);
        }
    }

    private int CalculateQuality(IReadOnlyList<Thing> ingredients)
    {
        var leadSkill = Pawn.skills?.GetSkill(SkillDefOf.Cooking).Level ?? 0;
        var distinctIngredients = DistinctOriginalIngredients(ingredients);
        var ingredientQuality = IngredientQualityScore(ingredients);
        var preparationQuality = PreparationQualityScore(ingredients);
        var cookwareScore = Cookware?.Thing is ThingWithComps cookware
            ? cookware.GetComp<CompKitchenwareStats>()?.CurrentStats.CulinaryToolScore ?? 0
            : 0;
        var knifeScore = Pawn.apparel?.WornApparel
            .FirstOrDefault(item => item.def.GetModExtension<KitchenwareExtension>()?.product == KitchenwareProduct.ChefsKnife)
            ?.GetComp<CompKitchenwareStats>()?.CurrentStats.CulinaryToolScore ?? 0;

        return CulinaryQualityCalculator.Calculate(new CulinaryQualityInputs(
            leadSkill * 5f,
            Math.Min(100f, distinctIngredients * 20f),
            ingredientQuality,
            preparationQuality,
            cookware: cookwareScore,
            knife: knifeScore,
            assistants: AssistantContribution.QualityScore));
    }

    private static float IngredientQualityScore(IReadOnlyList<Thing> ingredients)
    {
        if (ingredients.Count == 0)
        {
            return 50f;
        }

        var weightedTotal = 0f;
        var totalWeight = 0f;
        foreach (var ingredient in ingredients)
        {
            if ((ingredient as ThingWithComps)?.GetComp<CompPreparedFood>() is { } prepared)
            {
                foreach (var contribution in prepared.Contributions)
                {
                    var preparedWeight = Math.Max(0.0001f, contribution.Nutrition * ingredient.stackCount);
                    weightedTotal += contribution.CraftsmanshipScore * preparedWeight;
                    totalWeight += preparedWeight;
                }

                continue;
            }

            var quality = QualityUtility.TryGetQuality(ingredient, out var found)
                ? found
                : QualityCategory.Normal;
            var weight = Math.Max(0.0001f,
                ingredient.GetStatValue(StatDefOf.Nutrition) * ingredient.stackCount);
            weightedTotal += CraftsmanshipScore(quality) * weight;
            totalWeight += weight;
        }

        return totalWeight <= 0f ? 50f : weightedTotal / totalWeight;
    }

    private static int DistinctOriginalIngredients(IEnumerable<Thing> ingredients)
    {
        return ingredients.SelectMany(ingredient =>
        {
            var prepared = (ingredient as ThingWithComps)?.GetComp<CompPreparedFood>();
            return prepared is null
                ? new[] { ingredient.def.defName }
                : prepared.Contributions.Select(value => value.DefName);
        }).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }

    private static float PreparationQualityScore(IReadOnlyList<Thing> ingredients)
    {
        var weightedTotal = 0f;
        var totalWeight = 0f;
        foreach (var ingredient in ingredients)
        {
            var nutrition = Math.Max(0.0001f,
                ingredient.GetStatValue(StatDefOf.Nutrition) * ingredient.stackCount);
            var quality = (ingredient as ThingWithComps)?.GetComp<CompPreparedFood>()?.PreparationQuality ?? 50;
            weightedTotal += quality * nutrition;
            totalWeight += nutrition;
        }

        return totalWeight <= 0f ? 50f : weightedTotal / totalWeight;
    }

    private static int CraftsmanshipScore(QualityCategory quality)
    {
        return quality switch
        {
            QualityCategory.Awful => 0,
            QualityCategory.Poor => 25,
            QualityCategory.Normal => 50,
            QualityCategory.Good => 65,
            QualityCategory.Excellent => 80,
            QualityCategory.Masterwork => 90,
            QualityCategory.Legendary => 100,
            _ => 50
        };
    }

    private void DropExact(Thing thing, IntVec3 position, Map map)
    {
        var wasInPawnInventory = ReferenceEquals(
            thing.holdingOwner,
            Pawn.inventory?.innerContainer);
        var placed = false;
        if (thing.holdingOwner is { } owner)
        {
            placed = owner.TryDrop(thing, position, map, ThingPlaceMode.Near, out _);
        }
        else if (thing.Spawned)
        {
            if (thing.Position != position)
            {
                thing.DeSpawn(DestroyMode.Vanish);
                placed = GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Near);
            }
            else
            {
                placed = true;
            }
        }
        else
        {
            placed = GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Near);
        }

        if (placed)
        {
            (thing as ThingWithComps)?.GetComp<CompSanitation>()?.ClearSessionTransfer();
        }
        else if (wasInPawnInventory &&
                 (thing as ThingWithComps)?.GetComp<CompSanitation>()
                     ?.ReturnToMapAfterInterruptedSession == true)
        {
            GameComponent_ImmersiveChefsRecovery.ScheduleWareRecovery(Pawn, thing);
        }
    }
}

internal static class CookingSessionRegistry
{
    private static readonly ConditionalWeakTable<Job, CookingSession> Sessions = new();

    internal static bool TryAttach(Pawn pawn, Job job, Thing billGiver, out string? missingReason)
    {
        missingReason = null;
        if (!AdaptiveMealBillAdapter.TryResolveConcreteRecipe(job, out var reservationRecipe))
        {
            missingReason = "adaptive concrete recipe";
            return false;
        }

        if (!MealCoveragePolicy.IsCovered(reservationRecipe))
        {
            return true;
        }

        var settings = ImmersiveChefsMod.Settings;
        if (settings.WareRequirementMode == WareRequirementMode.Off)
        {
            Sessions.Add(job, new CookingSession(
                pawn, job, reservationRecipe!, billGiver, null,
                Array.Empty<ReservedWarePortion>(), false, true));
            KitchenAssistanceRegistry.Open(pawn, job, billGiver);
            return true;
        }

        // Stock-production bills have no consumer identity. Treating the cook's hunger as the
        // consumer emergency incorrectly makes an unrelated colonist's bill use dirty ware.
        var emergency = UrgentProductionRequestRegistry.HasActive(pawn.Map);
        var cookware = FindPortions(pawn, job, KitchenwareProduct.Cookware, 1, emergency, out var cookwareUse);
        var requiredPlates = MealCoveragePolicy.ServingCount(reservationRecipe!);
        var plateComplexity = MealClassificationRuntime.ClassifyRecipe(reservationRecipe);
        var plates = FindPortions(
            pawn,
            job,
            KitchenwareProduct.Plate,
            requiredPlates,
            emergency,
            out var plateUse,
            plateComplexity);
        var cookwareAllowed = cookwareUse.Admission == WareAdmission.Allowed;
        var platesAllowed = plateUse.Admission == WareAdmission.Allowed;
        if (!cookwareAllowed || !platesAllowed)
        {
            ReleaseReservations(pawn, job, cookware);
            ReleaseReservations(pawn, job, plates);
            missingReason = !cookwareAllowed && !platesAllowed
                ? "a clean cookware set and clean plates"
                : !cookwareAllowed ? "a clean cookware set" : "clean plates";
            return false;
        }

        var selectedCookware = cookware.FirstOrDefault();
        var emergencyMissing = emergency &&
                               (selectedCookware is null || plates.Sum(portion => portion.Count) < requiredPlates);
        Sessions.Add(job, new CookingSession(
            pawn, job, reservationRecipe!, billGiver, selectedCookware, plates, emergencyMissing, false));
        KitchenAssistanceRegistry.Open(pawn, job, billGiver);
        return true;
    }

    internal static float NotifyWorkTick(Pawn pawn)
    {
        if (pawn.CurJob is { } job && Sessions.TryGetValue(job, out var session))
        {
            return session.NotifyWorkTick();
        }

        return 0f;
    }

    internal static IEnumerable<Toil> AddWarePickupToils(Pawn pawn, IEnumerable<Toil> original)
    {
        if (pawn.CurJob is not { } job || !Sessions.TryGetValue(job, out var session))
        {
            return original;
        }

        var originalTargetC = job.GetTarget(TargetIndex.C);
        return Wrap();

        IEnumerable<Toil> Wrap()
        {
            foreach (var portion in session.PortionsToCollect())
            {
                yield return new Toil
                {
                    initAction = () => pawn.CurJob?.SetTarget(TargetIndex.C, portion.Thing),
                    defaultCompleteMode = ToilCompleteMode.Instant
                };
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.Touch);
                yield return new Toil
                {
                    initAction = () => session.Pickup(pawn, portion),
                    defaultCompleteMode = ToilCompleteMode.Instant
                };
            }

            yield return new Toil
            {
                initAction = () => pawn.CurJob?.SetTarget(TargetIndex.C, originalTargetC),
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            foreach (var toil in original)
            {
                yield return toil;
            }
        }
    }

    internal static float CookingSpeedFactor(Pawn pawn)
    {
        if (pawn.CurJob is not { } job || !Sessions.TryGetValue(job, out var session))
        {
            return 1f;
        }

        var factor = session.Cookware?.Thing is ThingWithComps cookware
            ? cookware.GetComp<CompKitchenwareStats>()?.CurrentStats.CookingSpeedFactor ?? 1f
            : 1f;
        var knifeFactor = pawn.apparel?.WornApparel
            .FirstOrDefault(item => item.def.GetModExtension<KitchenwareExtension>()?.product == KitchenwareProduct.ChefsKnife)
            ?.GetComp<CompKitchenwareStats>()?.CurrentStats.CookingSpeedFactor ?? 1f;
        return Math.Max(0.1f, factor * knifeFactor / Math.Max(0.25f, session.PreparedWorkFactor));
    }

    internal static IEnumerable<Thing> ApplyProducts(
        IEnumerable<Thing> products,
        RecipeDef finalRecipe,
        Pawn worker,
        IReadOnlyList<Thing> ingredients)
    {
        if (worker.CurJob is not { } job || !Sessions.TryGetValue(job, out var session))
        {
            foreach (var product in products)
            {
                yield return product;
            }

            yield break;
        }

        foreach (var product in products)
        {
            session.ApplyToProduct(product, finalRecipe, ingredients);
            yield return product;
        }

        session.MarkProductsCompleted();
    }

    internal static void Cleanup(Pawn pawn, Job? job)
    {
        if (job is null || !Sessions.TryGetValue(job, out var session))
        {
            return;
        }

        session.ReleaseAtEnd();
        KitchenAssistanceRegistry.Cleanup(pawn, job);
        Sessions.Remove(job);
    }

    private static List<ReservedWarePortion> FindPortions(
        Pawn pawn,
        Job job,
        KitchenwareProduct product,
        int requiredCount,
        bool emergency,
        out WareSelectionResult selection,
        MealComplexity? plateComplexity = null)
    {
        var candidates = pawn.Map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product == product)
            .Where(thing => product != KitchenwareProduct.Plate ||
                            PlateMaterialEligibilityRuntime.Allows(thing, plateComplexity))
            .Where(thing => !thing.IsForbidden(pawn) && pawn.CanReach(thing, PathEndMode.Touch, Danger.Some))
            .Where(thing => pawn.CanReserve(thing, 1, Math.Min(requiredCount, thing.stackCount)))
            .Select(thing => new
            {
                Thing = thing,
                Dirty = (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true,
                Score = SecondaryScore(thing, pawn)
            })
            .OrderBy(candidate => candidate.Dirty)
            .ThenByDescending(candidate => candidate.Score)
            .ToList();
        var cleanAvailable = candidates.Any(candidate => !candidate.Dirty);
        var dirtyAvailable = candidates.Any(candidate => candidate.Dirty);
        selection = WareSelectionPolicy.Select(
            ImmersiveChefsMod.Settings.WareRequirementMode,
            ImmersiveChefsMod.Settings.DirtyWareFallback,
            emergency,
            cleanAvailable,
            dirtyAvailable);
        if (selection.Use is WareUse.Missing or WareUse.MissingEmergency or WareUse.Exempt)
        {
            return new List<ReservedWarePortion>();
        }

        var wantDirty = selection.Use == WareUse.Dirty;
        var remaining = requiredCount;
        var result = new List<ReservedWarePortion>();
        foreach (var candidate in candidates.Where(candidate => candidate.Dirty == wantDirty))
        {
            var count = Math.Min(remaining, candidate.Thing.stackCount);
            if (count <= 0 || !pawn.Reserve(candidate.Thing, job, 1, count))
            {
                continue;
            }

            result.Add(new ReservedWarePortion(candidate.Thing, count));
            remaining -= count;
            if (remaining == 0)
            {
                break;
            }
        }

        if (remaining > 0)
        {
            ReleaseReservations(pawn, job, result);
            result.Clear();
            selection = WareSelectionPolicy.Select(
                ImmersiveChefsMod.Settings.WareRequirementMode,
                ImmersiveChefsMod.Settings.DirtyWareFallback,
                emergency,
                cleanAvailable: false,
                dirtyAvailable: false);
        }

        return result;
    }

    private static float SecondaryScore(Thing thing, Pawn pawn)
    {
        var stats = (thing as ThingWithComps)?.GetComp<CompKitchenwareStats>()?.CurrentStats;
        var quality = QualityUtility.TryGetQuality(thing, out var foundQuality) ? (int)foundQuality : 2;
        return (stats?.MaterialCleanliness ?? 0f) +
               ((stats?.Comfort ?? 0f) * 20f) +
               ((stats?.CulinaryToolScore ?? 0) * 0.5f) +
               (quality * 3f) -
               (thing.PositionHeld.DistanceToSquared(pawn.PositionHeld) * 0.01f);
    }

    private static void ReleaseReservations(Pawn pawn, Job job, IEnumerable<ReservedWarePortion> portions)
    {
        foreach (var portion in portions)
        {
            pawn.Map.reservationManager.Release(portion.Thing, pawn, job);
        }
    }
}
