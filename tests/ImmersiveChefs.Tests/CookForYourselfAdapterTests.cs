using NUnit.Framework;
using System.IO;
using System.Xml.Linq;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class CookForYourselfAdapterTests
{
    [TestCase(true, true, true, true, true, true)]
    [TestCase(false, true, true, true, true, false)]
    [TestCase(true, false, true, true, true, false)]
    [TestCase(true, true, false, true, true, false)]
    [TestCase(true, true, true, false, true, false)]
    [TestCase(true, true, true, true, false, false)]
    public void Loaded_cfs_jobs_are_aborted_only_for_the_exact_active_covered_shape(
        bool adapterEnabled,
        bool integrationEnabled,
        bool exactDriver,
        bool exactJobDef,
        bool coveredRecipeTag,
        bool expected)
    {
        Assert.That(
            CookForYourselfRecoveryPolicy.ShouldAbortOnLoad(
                adapterEnabled,
                integrationEnabled,
                exactDriver,
                exactJobDef,
                coveredRecipeTag),
            Is.EqualTo(expected));
    }

    private static CookForYourselfShape SupportedShape => new(
        assemblyName: "CookForYourself",
        assemblyVersion: new Version(1, 0, 0, 0),
        moduleVersionId: Guid.Parse("d559047c-a763-4d2f-8cab-7077d5f4a309"),
        selfJobGiverTypeName: "CookForYourself.JobGiver_CookMealForSelf",
        selfTryGiveJobIsProtectedInstancePawnJob: true,
        dependentJobGiverTypeName: "CookForYourself.JobGiver_CookMealForDependent",
        dependentTryGiveJobIsProtectedInstancePawnJob: true,
        driverTypeName: "CookForYourself.JobDriver_CookMealForSelf",
        driverIsPublicSealedJobDriver: true,
        makeNewToilsIsProtectedInstanceEnumerableToil: true,
        workLeftIsPrivateInstanceFloat: true,
        recipeIsPrivateInstanceRecipeDef: true,
        recipientIsPrivateInstancePawn: true,
        deliveryModeIsPrivateInstanceInt: true,
        jobDefName: "CFS_CookMealForSelf",
        jobDefUsesDriver: true);

    [Test]
    public void Inspected_installed_shape_is_supported()
    {
        Assert.That(CookForYourselfCompatibility.IsSupported(SupportedShape), Is.True);
    }

    [Test]
    public void Any_changed_or_lookalike_shape_fails_closed()
    {
        var changed = new[]
        {
            Changed(shape => shape.AssemblyName = "Lookalike"),
            Changed(shape => shape.AssemblyVersion = new Version(2, 0, 0, 0)),
            Changed(shape => shape.ModuleVersionId = Guid.Empty),
            Changed(shape => shape.SelfJobGiverTypeName = "Changed.Self"),
            Changed(shape => shape.SelfTryGiveJobIsProtectedInstancePawnJob = false),
            Changed(shape => shape.DependentJobGiverTypeName = "Changed.Dependent"),
            Changed(shape => shape.DependentTryGiveJobIsProtectedInstancePawnJob = false),
            Changed(shape => shape.DriverTypeName = "Changed.Driver"),
            Changed(shape => shape.DriverIsPublicSealedJobDriver = false),
            Changed(shape => shape.MakeNewToilsIsProtectedInstanceEnumerableToil = false),
            Changed(shape => shape.WorkLeftIsPrivateInstanceFloat = false),
            Changed(shape => shape.RecipeIsPrivateInstanceRecipeDef = false),
            Changed(shape => shape.RecipientIsPrivateInstancePawn = false),
            Changed(shape => shape.DeliveryModeIsPrivateInstanceInt = false),
            Changed(shape => shape.JobDefName = "ChangedJob"),
            Changed(shape => shape.JobDefUsesDriver = false)
        };

        Assert.That(changed, Has.All.Matches<CookForYourselfShape>(
            shape => !CookForYourselfCompatibility.IsSupported(shape)));
    }

    [Test]
    public void Exact_job_contract_requires_recipe_station_and_matching_ingredient_queues()
    {
        var exactIngredients = new[]
        {
            new CookForYourselfIngredientEntry(true, false, 2, 10),
            new CookForYourselfIngredientEntry(true, false, 1, 1)
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                CookForYourselfJobContract.IsValid(
                    isExactJobDef: true,
                    hasConcreteRecipeTag: true,
                    stationIsUsableBillGiver: true,
                    ingredients: exactIngredients,
                    ingredientCountQueueCount: 2,
                    recipientIsAbsentOrPawn: true),
                Is.True);
            Assert.That(CookForYourselfJobContract.IsValid(true, false, true, exactIngredients, 2, true), Is.False);
            Assert.That(CookForYourselfJobContract.IsValid(true, true, false, exactIngredients, 2, true), Is.False);
            Assert.That(CookForYourselfJobContract.IsValid(true, true, true, Array.Empty<CookForYourselfIngredientEntry>(), 0, true), Is.False);
            Assert.That(CookForYourselfJobContract.IsValid(true, true, true, exactIngredients, 1, true), Is.False);
            Assert.That(CookForYourselfJobContract.IsValid(false, true, true, exactIngredients, 2, true), Is.False);
            Assert.That(CookForYourselfJobContract.IsValid(true, true, true, exactIngredients, 2, false), Is.False);
        });
    }

    [TestCase(false, false, 1, 1)]
    [TestCase(true, true, 1, 1)]
    [TestCase(true, false, 0, 1)]
    [TestCase(true, false, -1, 1)]
    [TestCase(true, false, 2, 1)]
    public void Invalid_ingredient_entries_fail_the_exact_job_contract(
        bool hasThing,
        bool destroyed,
        int requestedCount,
        int stackCount)
    {
        var entries = new[]
        {
            new CookForYourselfIngredientEntry(hasThing, destroyed, requestedCount, stackCount)
        };

        Assert.That(
            CookForYourselfJobContract.IsValid(true, true, true, entries, 1, true),
            Is.False);
    }

    [Test]
    public void Package_metadata_loads_after_the_optional_owners_without_requiring_them()
    {
        var project = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "ImmersiveChefs.csproj"));
        XNamespace ns = project.Root?.Name.Namespace ?? XNamespace.None;
        var loadAfter = project.Descendants(ns + "RimWorldLoadAfter")
            .Select(element => (string?)element.Attribute("Include"));
        var required = project.Descendants(ns + "RimWorldModDependency")
            .Select(element => (string?)element.Attribute("Include"));

        Assert.Multiple(() =>
        {
            Assert.That(loadAfter, Does.Contain("lordfelix.CookForYourself"));
            Assert.That(loadAfter, Does.Contain("Andromeda.StackGap"));
            Assert.That(required, Does.Not.Contain("lordfelix.CookForYourself"));
            Assert.That(required, Does.Not.Contain("Andromeda.StackGap"));
        });
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void Only_existing_covered_meals_enter_the_ware_lifecycle(
        bool recipeIsCovered,
        bool shouldAttach)
    {
        Assert.That(
            CookForYourselfJobContract.AdmissionFor(
                adapterEnabled: true,
                jobContractValid: true,
                recipeExists: true,
                recipeIsCovered) == CookForYourselfAdmission.Attach,
            Is.EqualTo(shouldAttach));
    }

    [TestCase(false, true, true, true)]
    [TestCase(true, false, true, true)]
    [TestCase(true, true, false, true)]
    public void Disabled_or_drifted_jobs_pass_through_without_partial_attachment(
        bool adapterEnabled,
        bool jobContractValid,
        bool recipeExists,
        bool recipeIsCovered)
    {
        Assert.That(
            CookForYourselfJobContract.AdmissionFor(
                adapterEnabled,
                jobContractValid,
                recipeExists,
                recipeIsCovered),
            Is.EqualTo(CookForYourselfAdmission.PassThrough));
    }

    [Test]
    public void Cooking_toil_selection_requires_one_exact_active_work_toil()
    {
        var toils = new[]
        {
            new CookForYourselfToilShape("GotoThing", false, false, false, false),
            new CookForYourselfToilShape("CookMealForSelf", true, true, true, true),
            new CookForYourselfToilShape("FinishCookMealForSelf", false, false, true, false)
        };

        Assert.That(CookForYourselfToilPolicy.TrySelectExactIndex(toils, out var index), Is.True);
        Assert.That(index, Is.EqualTo(1));
    }

    [Test]
    public void Ware_pickup_begins_after_upstream_ingredient_placement_and_before_cooking()
    {
        var toils = new[]
        {
            new CookForYourselfToilShape("ExtractNextTargetFromQueue", false, false, true, false),
            new CookForYourselfToilShape("StartCarryThing", false, false, true, false),
            new CookForYourselfToilShape("SetTargetToIngredientPlaceCell", false, false, true, false),
            new CookForYourselfToilShape("PlaceCookIngredient", false, false, true, false),
            new CookForYourselfToilShape("JumpIfHaveTargetInQueue", false, false, true, false),
            new CookForYourselfToilShape("CookMealForSelf", true, true, true, true),
            new CookForYourselfToilShape("FinishCookMealForSelf", false, false, true, false)
        };

        Assert.That(
            CookForYourselfToilPolicy.TrySelectWarePickupIndex(toils, out var pickupIndex),
            Is.True);
        Assert.That(pickupIndex, Is.EqualTo(5));
        Assert.That(pickupIndex, Is.GreaterThan(3));
    }

    [Test]
    public void Missing_reordered_or_ambiguous_ingredient_placement_fails_closed()
    {
        var placement = new CookForYourselfToilShape(
            "PlaceCookIngredient",
            false,
            false,
            true,
            false);
        var cooking = new CookForYourselfToilShape(
            "CookMealForSelf",
            true,
            true,
            true,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(
                CookForYourselfToilPolicy.TrySelectWarePickupIndex(
                    new[] { cooking },
                    out _),
                Is.False);
            Assert.That(
                CookForYourselfToilPolicy.TrySelectWarePickupIndex(
                    new[] { cooking, placement },
                    out _),
                Is.False);
            Assert.That(
                CookForYourselfToilPolicy.TrySelectWarePickupIndex(
                    new[] { placement, placement, cooking },
                    out _),
                Is.False);
        });
    }

    [Test]
    public void Missing_or_ambiguous_cooking_toil_fails_closed()
    {
        var exact = new CookForYourselfToilShape("CookMealForSelf", true, true, true, true);

        Assert.Multiple(() =>
        {
            Assert.That(
                CookForYourselfToilPolicy.TrySelectExactIndex(
                    new[] { new CookForYourselfToilShape("Changed", true, true, true, true) },
                    out _),
                Is.False);
            Assert.That(
                CookForYourselfToilPolicy.TrySelectExactIndex(new[] { exact, exact }, out _),
                Is.False);
        });
    }

    [TestCase(true, true, true, true, true, true, true)]
    [TestCase(false, true, true, true, true, true, false)]
    [TestCase(true, false, true, true, true, true, false)]
    [TestCase(true, true, false, true, true, true, false)]
    [TestCase(true, true, true, false, true, true, false)]
    [TestCase(true, true, true, true, false, true, false)]
    [TestCase(true, true, true, true, true, false, false)]
    public void Stack_gap_is_bypassed_only_for_the_current_prework_ingredient_drop(
        bool adapterEnabled,
        bool jobAdmitted,
        bool directMode,
        bool beforeActiveCooking,
        bool currentToilIsIngredientPlacement,
        bool hasCarriedThing,
        bool expected)
    {
        Assert.That(
            StackGapIngredientDropPolicy.ShouldBypass(
                adapterEnabled,
                jobAdmitted,
                directMode,
                beforeActiveCooking,
                currentToilIsIngredientPlacement,
                hasCarriedThing),
            Is.EqualTo(expected));
    }

    [TestCase(1f, 1f, 1, 1f, 0f, 0f)]
    [TestCase(2f, 1f, 3, 1.5f, 0f, 3f)]
    [TestCase(2f, 1f, 3, 0.5f, 0f, -3f)]
    [TestCase(2f, 1f, 3, 1f, 0.25f, 1.5f)]
    [TestCase(2f, 2f, 3, 1.5f, 0f, 6f)]
    [TestCase(2f, 2f, 3, 1f, 0.25f, 1.5f)]
    public void Additional_progress_preserves_upstream_base_work_and_adds_owned_modifiers(
        float pawnWorkPerTick,
        float workTableFactor,
        int delta,
        float cookingSpeedFactor,
        float assistantBonus,
        float expectedAdditionalProgress)
    {
        Assert.That(
            CookForYourselfWorkPolicy.AdditionalProgress(
                pawnWorkPerTick,
                workTableFactor,
                delta,
                cookingSpeedFactor,
                assistantBonus),
            Is.EqualTo(expectedAdditionalProgress).Within(0.0001f));
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void Only_exactly_admitted_jobs_are_decorated_even_after_global_disable(
        bool jobWasAdmitted,
        bool expected)
    {
        Assert.That(
            CookForYourselfPatchPolicy.ShouldDecorateDriver(jobWasAdmitted),
            Is.EqualTo(expected));
    }

    [TestCase(false, 0, false)]
    [TestCase(true, 0, true)]
    [TestCase(true, 1, false)]
    public void Patch_installation_is_all_or_nothing_and_not_duplicated(
        bool allTargetsValidated,
        int existingOwnedPatchCount,
        bool expected)
    {
        Assert.That(
            CookForYourselfPatchPolicy.ShouldInstall(allTargetsValidated, existingOwnedPatchCount),
            Is.EqualTo(expected));
    }

    private static CookForYourselfShape Changed(Action<CookForYourselfShape> mutation)
    {
        var shape = SupportedShape;
        mutation(shape);
        return shape;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
