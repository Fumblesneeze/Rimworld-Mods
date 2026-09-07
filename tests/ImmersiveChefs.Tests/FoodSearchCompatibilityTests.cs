using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class FoodSearchCompatibilityTests
{
    [TestCase("1.0.0.0")]
    [TestCase("9.8.7.6")]
    public void Exact_meals_on_wheels_shape_is_supported(string version)
    {
        Assert.That(
            MealsOnWheelsCompatibility.IsSupported(
                assemblyName: "Meals_On_Wheels",
                assemblyVersion: version,
                patchTypeName: "Meals_On_Wheels.FoodGrabbing",
                postfixMethodName: "Postfix",
                postfixShape: true,
                patchOwner: "uuugggg.rimworld.Meals_On_Wheels.main"),
            Is.True);
    }

    [TestCase("Lookalike", "1.0.0.0", "Meals_On_Wheels.FoodGrabbing", "Postfix", true, "uuugggg.rimworld.Meals_On_Wheels.main")]
    [TestCase("Meals_On_Wheels", "1.0.0.0", "Changed.FoodGrabbing", "Postfix", true, "uuugggg.rimworld.Meals_On_Wheels.main")]
    [TestCase("Meals_On_Wheels", "1.0.0.0", "Meals_On_Wheels.FoodGrabbing", "Changed", true, "uuugggg.rimworld.Meals_On_Wheels.main")]
    [TestCase("Meals_On_Wheels", "1.0.0.0", "Meals_On_Wheels.FoodGrabbing", "Postfix", false, "uuugggg.rimworld.Meals_On_Wheels.main")]
    [TestCase("Meals_On_Wheels", "1.0.0.0", "Meals_On_Wheels.FoodGrabbing", "Postfix", true, "lookalike.owner")]
    public void Changed_or_lookalike_meals_on_wheels_shape_fails_closed(
        string assemblyName,
        string assemblyVersion,
        string patchTypeName,
        string postfixMethodName,
        bool postfixShape,
        string patchOwner)
    {
        Assert.That(
            MealsOnWheelsCompatibility.IsSupported(
                assemblyName,
                assemblyVersion,
                patchTypeName,
                postfixMethodName,
                postfixShape,
                patchOwner),
            Is.False);
    }

    [Test]
    public void Exact_meals_on_wheels_target_and_priority_are_supported()
    {
        Assert.That(
            MealsOnWheelsCompatibility.IsSupportedPatch(
                exactTargetShape: true,
                patchOwner: "uuugggg.rimworld.Meals_On_Wheels.main",
                patchPriority: 200),
            Is.True);
    }

    [TestCase(false, "uuugggg.rimworld.Meals_On_Wheels.main", 200)]
    [TestCase(true, "lookalike.owner", 200)]
    [TestCase(true, "uuugggg.rimworld.Meals_On_Wheels.main", 400)]
    public void Changed_meals_on_wheels_target_owner_or_priority_fails_closed(
        bool exactTargetShape,
        string patchOwner,
        int patchPriority)
    {
        Assert.That(
            MealsOnWheelsCompatibility.IsSupportedPatch(exactTargetShape, patchOwner, patchPriority),
            Is.False);
    }

    [TestCase("2.3.0.0")]
    [TestCase("9.8.7.6")]
    public void Exact_prioritize_meals_shape_is_supported(string version)
    {
        Assert.That(
            PrioritizeMealsCompatibility.IsSupported(
                assemblyName: "Prioritize Meals over Preserved Foods",
                assemblyVersion: version,
                startupTypeName: "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main",
                hasStartupAttribute: true,
                foodsTypeName: "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods",
                preservedFoodsFieldShape: true,
                caravanPatchTypeName: "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival",
                caravanPostfixMethodName: "SendLetter_Postfix",
                caravanPostfixShape: true,
                patchOwner: "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch",
                allTypesShareAssembly: true),
            Is.True);
    }

    [Test]
    public void Prioritize_meals_types_from_different_assemblies_fail_closed()
    {
        Assert.That(
            PrioritizeMealsCompatibility.IsSupported(
                assemblyName: "Prioritize Meals over Preserved Foods",
                assemblyVersion: "2.3.0.0",
                startupTypeName: "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main",
                hasStartupAttribute: true,
                foodsTypeName: "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods",
                preservedFoodsFieldShape: true,
                caravanPatchTypeName: "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival",
                caravanPostfixMethodName: "SendLetter_Postfix",
                caravanPostfixShape: true,
                patchOwner: "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch",
                allTypesShareAssembly: false),
            Is.False);
    }

    [TestCase("Lookalike", "2.3.0.0", "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival", "SendLetter_Postfix", true, "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch")]
    [TestCase("Prioritize Meals over Preserved Foods", "2.3.0.0", "Changed.Main", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival", "SendLetter_Postfix", true, "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch")]
    [TestCase("Prioritize Meals over Preserved Foods", "2.3.0.0", "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main", false, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival", "SendLetter_Postfix", true, "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch")]
    [TestCase("Prioritize Meals over Preserved Foods", "2.3.0.0", "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main", true, "Changed.Foods", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival", "SendLetter_Postfix", true, "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch")]
    [TestCase("Prioritize Meals over Preserved Foods", "2.3.0.0", "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods", false, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival", "SendLetter_Postfix", true, "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch")]
    [TestCase("Prioritize Meals over Preserved Foods", "2.3.0.0", "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods", true, "Changed.Patch", "SendLetter_Postfix", true, "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch")]
    [TestCase("Prioritize Meals over Preserved Foods", "2.3.0.0", "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival", "Changed", true, "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch")]
    [TestCase("Prioritize Meals over Preserved Foods", "2.3.0.0", "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival", "SendLetter_Postfix", false, "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch")]
    [TestCase("Prioritize Meals over Preserved Foods", "2.3.0.0", "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods", true, "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival", "SendLetter_Postfix", true, "lookalike.owner")]
    public void Changed_or_lookalike_prioritize_meals_shape_fails_closed(
        string assemblyName,
        string assemblyVersion,
        string startupTypeName,
        bool hasStartupAttribute,
        string foodsTypeName,
        bool preservedFoodsFieldShape,
        string caravanPatchTypeName,
        string caravanPostfixMethodName,
        bool caravanPostfixShape,
        string patchOwner)
    {
        Assert.That(
            PrioritizeMealsCompatibility.IsSupported(
                assemblyName,
                assemblyVersion,
                startupTypeName,
                hasStartupAttribute,
                foodsTypeName,
                preservedFoodsFieldShape,
                caravanPatchTypeName,
                caravanPostfixMethodName,
                caravanPostfixShape,
                patchOwner,
                allTypesShareAssembly: true),
            Is.False);
    }
}
