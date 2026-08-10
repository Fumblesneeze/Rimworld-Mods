namespace ImmersiveChefs;

internal sealed class CookForYourselfShape
{
    internal CookForYourselfShape(
        string? assemblyName,
        Version? assemblyVersion,
        Guid moduleVersionId,
        string? selfJobGiverTypeName,
        bool selfTryGiveJobIsProtectedInstancePawnJob,
        string? dependentJobGiverTypeName,
        bool dependentTryGiveJobIsProtectedInstancePawnJob,
        string? driverTypeName,
        bool driverIsPublicSealedJobDriver,
        bool makeNewToilsIsProtectedInstanceEnumerableToil,
        bool workLeftIsPrivateInstanceFloat,
        bool recipeIsPrivateInstanceRecipeDef,
        bool recipientIsPrivateInstancePawn,
        bool deliveryModeIsPrivateInstanceInt,
        string? jobDefName,
        bool jobDefUsesDriver)
    {
        AssemblyName = assemblyName;
        AssemblyVersion = assemblyVersion;
        ModuleVersionId = moduleVersionId;
        SelfJobGiverTypeName = selfJobGiverTypeName;
        SelfTryGiveJobIsProtectedInstancePawnJob = selfTryGiveJobIsProtectedInstancePawnJob;
        DependentJobGiverTypeName = dependentJobGiverTypeName;
        DependentTryGiveJobIsProtectedInstancePawnJob = dependentTryGiveJobIsProtectedInstancePawnJob;
        DriverTypeName = driverTypeName;
        DriverIsPublicSealedJobDriver = driverIsPublicSealedJobDriver;
        MakeNewToilsIsProtectedInstanceEnumerableToil = makeNewToilsIsProtectedInstanceEnumerableToil;
        WorkLeftIsPrivateInstanceFloat = workLeftIsPrivateInstanceFloat;
        RecipeIsPrivateInstanceRecipeDef = recipeIsPrivateInstanceRecipeDef;
        RecipientIsPrivateInstancePawn = recipientIsPrivateInstancePawn;
        DeliveryModeIsPrivateInstanceInt = deliveryModeIsPrivateInstanceInt;
        JobDefName = jobDefName;
        JobDefUsesDriver = jobDefUsesDriver;
    }

    internal string? AssemblyName { get; set; }
    internal Version? AssemblyVersion { get; set; }
    internal Guid ModuleVersionId { get; set; }
    internal string? SelfJobGiverTypeName { get; set; }
    internal bool SelfTryGiveJobIsProtectedInstancePawnJob { get; set; }
    internal string? DependentJobGiverTypeName { get; set; }
    internal bool DependentTryGiveJobIsProtectedInstancePawnJob { get; set; }
    internal string? DriverTypeName { get; set; }
    internal bool DriverIsPublicSealedJobDriver { get; set; }
    internal bool MakeNewToilsIsProtectedInstanceEnumerableToil { get; set; }
    internal bool WorkLeftIsPrivateInstanceFloat { get; set; }
    internal bool RecipeIsPrivateInstanceRecipeDef { get; set; }
    internal bool RecipientIsPrivateInstancePawn { get; set; }
    internal bool DeliveryModeIsPrivateInstanceInt { get; set; }
    internal string? JobDefName { get; set; }
    internal bool JobDefUsesDriver { get; set; }
}

internal enum CookForYourselfAdmission
{
    PassThrough,
    Attach
}

internal readonly struct CookForYourselfIngredientEntry
{
    internal CookForYourselfIngredientEntry(
        bool hasThing,
        bool destroyed,
        int requestedCount,
        int stackCount)
    {
        HasThing = hasThing;
        Destroyed = destroyed;
        RequestedCount = requestedCount;
        StackCount = stackCount;
    }

    internal bool HasThing { get; }
    internal bool Destroyed { get; }
    internal int RequestedCount { get; }
    internal int StackCount { get; }
}

internal static class CookForYourselfCompatibility
{
    internal const string AssemblyName = "CookForYourself";
    internal static readonly Version AssemblyVersion = new(1, 0, 0, 0);
    internal static readonly Guid ModuleVersionId =
        Guid.Parse("d559047c-a763-4d2f-8cab-7077d5f4a309");
    internal const string SelfJobGiverTypeName = "CookForYourself.JobGiver_CookMealForSelf";
    internal const string DependentJobGiverTypeName = "CookForYourself.JobGiver_CookMealForDependent";
    internal const string DriverTypeName = "CookForYourself.JobDriver_CookMealForSelf";
    internal const string JobDefName = "CFS_CookMealForSelf";

    internal static bool IsSupported(CookForYourselfShape shape)
    {
        return shape.AssemblyName == AssemblyName &&
               shape.AssemblyVersion == AssemblyVersion &&
               shape.ModuleVersionId == ModuleVersionId &&
               shape.SelfJobGiverTypeName == SelfJobGiverTypeName &&
               shape.SelfTryGiveJobIsProtectedInstancePawnJob &&
               shape.DependentJobGiverTypeName == DependentJobGiverTypeName &&
               shape.DependentTryGiveJobIsProtectedInstancePawnJob &&
               shape.DriverTypeName == DriverTypeName &&
               shape.DriverIsPublicSealedJobDriver &&
               shape.MakeNewToilsIsProtectedInstanceEnumerableToil &&
               shape.WorkLeftIsPrivateInstanceFloat &&
               shape.RecipeIsPrivateInstanceRecipeDef &&
               shape.RecipientIsPrivateInstancePawn &&
               shape.DeliveryModeIsPrivateInstanceInt &&
               shape.JobDefName == JobDefName &&
               shape.JobDefUsesDriver;
    }
}

internal static class CookForYourselfJobContract
{
    internal static bool IsValid(
        bool isExactJobDef,
        bool hasConcreteRecipeTag,
        bool stationIsUsableBillGiver,
        IReadOnlyList<CookForYourselfIngredientEntry> ingredients,
        int ingredientCountQueueCount,
        bool recipientIsAbsentOrPawn)
    {
        return isExactJobDef &&
               hasConcreteRecipeTag &&
               stationIsUsableBillGiver &&
               recipientIsAbsentOrPawn &&
               ingredients.Count > 0 &&
               ingredients.Count == ingredientCountQueueCount &&
               ingredients.All(ingredient =>
                   ingredient.HasThing &&
                   !ingredient.Destroyed &&
                   ingredient.RequestedCount > 0 &&
                   ingredient.RequestedCount <= ingredient.StackCount);
    }

    internal static CookForYourselfAdmission AdmissionFor(
        bool adapterEnabled,
        bool jobContractValid,
        bool recipeExists,
        bool recipeIsCovered)
    {
        return adapterEnabled && jobContractValid && recipeExists && recipeIsCovered
            ? CookForYourselfAdmission.Attach
            : CookForYourselfAdmission.PassThrough;
    }
}
