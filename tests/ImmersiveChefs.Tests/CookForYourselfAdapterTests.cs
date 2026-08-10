using NUnit.Framework;
using System.IO;
using System.Xml.Linq;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class CookForYourselfAdapterTests
{
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
    public void Package_metadata_loads_after_the_optional_owner_without_requiring_it()
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
            Assert.That(required, Does.Not.Contain("lordfelix.CookForYourself"));
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
