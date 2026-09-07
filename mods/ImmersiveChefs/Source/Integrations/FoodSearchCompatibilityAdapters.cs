using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

internal static class MealsOnWheelsCompatibility
{
    internal const string AssemblyName = "Meals_On_Wheels";
    internal const string PatchTypeName = "Meals_On_Wheels.FoodGrabbing";
    internal const string PostfixMethodName = "Postfix";
    internal const string PatchOwner = "uuugggg.rimworld.Meals_On_Wheels.main";

    internal static bool IsSupported(
        string? assemblyName,
        string? assemblyVersion,
        string? patchTypeName,
        string? postfixMethodName,
        bool postfixShape,
        string? patchOwner)
    {
        return assemblyName == AssemblyName &&
               patchTypeName == PatchTypeName &&
               postfixMethodName == PostfixMethodName &&
               postfixShape &&
               patchOwner == PatchOwner;
    }

    internal static bool IsSupportedPatch(
        bool exactTargetShape,
        string? patchOwner,
        int patchPriority)
    {
        return exactTargetShape &&
               patchOwner == PatchOwner &&
               patchPriority == Priority.Low;
    }
}

internal static class MealsOnWheelsAdapter
{
    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(out string reason)
    {
        try
        {
            if (Enabled)
            {
                reason = string.Empty;
                return true;
            }

            var patchType = AccessTools.TypeByName(MealsOnWheelsCompatibility.PatchTypeName);
            var postfix = patchType?.GetMethod(
                MealsOnWheelsCompatibility.PostfixMethodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(bool).MakeByRefType(), typeof(Pawn), typeof(Pawn),
                    typeof(Thing).MakeByRefType(), typeof(ThingDef).MakeByRefType(),
                    typeof(bool), typeof(bool), typeof(bool)
                },
                modifiers: null);
            var assembly = patchType?.Assembly;
            var postfixShape = postfix?.ReturnType == typeof(void) && postfix.DeclaringType == patchType;
            var target = AccessTools.Method(
                typeof(FoodUtility),
                nameof(FoodUtility.TryFindBestFoodSourceFor),
                new[]
                {
                    typeof(Pawn), typeof(Pawn), typeof(bool),
                    typeof(Thing).MakeByRefType(), typeof(ThingDef).MakeByRefType(),
                    typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                    typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                    typeof(bool), typeof(FoodPreferability)
                });
            var exactTargetShape = target?.DeclaringType == typeof(FoodUtility) &&
                                   target.ReturnType == typeof(bool);
            var patch = FindExactPatch(postfix, target);
            var supported = MealsOnWheelsCompatibility.IsSupported(
                assembly?.GetName().Name,
                assembly?.GetName().Version?.ToString(),
                patchType?.FullName,
                postfix?.Name,
                postfixShape,
                patch?.owner) &&
                            MealsOnWheelsCompatibility.IsSupportedPatch(
                                exactTargetShape,
                                patch?.owner,
                                patch?.priority ?? int.MinValue);
            if (!supported)
            {
                Enabled = false;
                reason =
                    "the installed Meals on Wheels shape differs from the validated 1.6 contract " +
                    $"(assembly={assembly?.GetName().Name ?? "missing"}; " +
                    $"version={assembly?.GetName().Version?.ToString() ?? "missing"}; " +
                    $"postfix={patchType?.FullName ?? "missing"}.{postfix?.Name ?? "missing"}; " +
                    $"postfixShape={postfixShape}; exactTarget={exactTargetShape}; " +
                    $"owner={patch?.owner ?? "missing"}; priority={patch?.priority.ToString() ?? "missing"})";
                return false;
            }

            Enabled = true;
            reason = string.Empty;
            Log.Message(
                "[ImmersiveChefs] Meals on Wheels coexistence active; upstream mobile food-source selection remains authoritative.");
            return true;
        }
        catch (Exception exception)
        {
            Enabled = false;
            reason = $"Meals on Wheels shape validation failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    private static Patch? FindExactPatch(MethodInfo? patchMethod, MethodInfo? target)
    {
        if (patchMethod is null || target is null)
        {
            return null;
        }

        var matches = (Harmony.GetPatchInfo(target)?.Postfixes ?? Enumerable.Empty<Patch>())
            .Where(patch => patch.PatchMethod == patchMethod)
            .ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
}

internal static class PrioritizeMealsCompatibility
{
    internal const string AssemblyName = "Prioritize Meals over Preserved Foods";
    internal const string StartupTypeName = "seekiworks_Prioritize_Meals_over_Preserved_Foods.Main";
    internal const string FoodsTypeName = "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods";
    internal const string PreservedFoodsFieldName = "preservedFoods";
    internal const string CaravanPatchTypeName =
        "seekiworks_Prioritize_Meals_over_Preserved_Foods.Patch_IncidentWorker_TraderCaravanArrival";
    internal const string CaravanPostfixMethodName = "SendLetter_Postfix";
    internal const string PatchOwner = "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch";

    internal static bool IsSupported(
        string? assemblyName,
        string? assemblyVersion,
        string? startupTypeName,
        bool hasStartupAttribute,
        string? foodsTypeName,
        bool preservedFoodsFieldShape,
        string? caravanPatchTypeName,
        string? caravanPostfixMethodName,
        bool caravanPostfixShape,
        string? patchOwner,
        bool allTypesShareAssembly)
    {
        return HasSupportedShape(
                   assemblyName,
                   assemblyVersion,
                   startupTypeName,
                   hasStartupAttribute,
                   foodsTypeName,
                   preservedFoodsFieldShape,
                   caravanPatchTypeName,
                   caravanPostfixMethodName,
                   caravanPostfixShape,
                   allTypesShareAssembly) &&
               patchOwner == PatchOwner;
    }

    internal static bool HasSupportedShape(
        string? assemblyName,
        string? assemblyVersion,
        string? startupTypeName,
        bool hasStartupAttribute,
        string? foodsTypeName,
        bool preservedFoodsFieldShape,
        string? caravanPatchTypeName,
        string? caravanPostfixMethodName,
        bool caravanPostfixShape,
        bool allTypesShareAssembly)
    {
        return assemblyName == AssemblyName &&
               startupTypeName == StartupTypeName &&
               hasStartupAttribute &&
               foodsTypeName == FoodsTypeName &&
               preservedFoodsFieldShape &&
               caravanPatchTypeName == CaravanPatchTypeName &&
               caravanPostfixMethodName == CaravanPostfixMethodName &&
               caravanPostfixShape &&
               allTypesShareAssembly;
    }
}

internal static class PrioritizeMealsAdapter
{
    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(out string reason)
    {
        try
        {
            if (Enabled)
            {
                reason = string.Empty;
                return true;
            }

            var startupType = AccessTools.TypeByName(PrioritizeMealsCompatibility.StartupTypeName);
            var foodsType = AccessTools.TypeByName(PrioritizeMealsCompatibility.FoodsTypeName);
            var patchType = AccessTools.TypeByName(PrioritizeMealsCompatibility.CaravanPatchTypeName);
            var preservedFoods = foodsType?.GetField(
                PrioritizeMealsCompatibility.PreservedFoodsFieldName,
                BindingFlags.NonPublic | BindingFlags.Static);
            var postfix = patchType?.GetMethod(
                PrioritizeMealsCompatibility.CaravanPostfixMethodName,
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(List<Pawn>).MakeByRefType() },
                modifiers: null);
            var assembly = startupType?.Assembly;
            var hasStartupAttribute = startupType?.CustomAttributes.Any(attribute =>
                attribute.AttributeType == typeof(StaticConstructorOnStartup)) == true;
            var fieldShape = preservedFoods?.FieldType == typeof(HashSet<ThingDef>);
            var postfixShape = postfix?.ReturnType == typeof(void) && postfix.DeclaringType == patchType;
            var allTypesShareAssembly = assembly is not null &&
                                        foodsType?.Assembly == assembly &&
                                        patchType?.Assembly == assembly;
            var shapeSupported = PrioritizeMealsCompatibility.HasSupportedShape(
                assembly?.GetName().Name,
                assembly?.GetName().Version?.ToString(),
                startupType?.FullName,
                hasStartupAttribute,
                foodsType?.FullName,
                fieldShape,
                patchType?.FullName,
                postfix?.Name,
                postfixShape,
                allTypesShareAssembly);
            if (shapeSupported && startupType is not null)
            {
                // RimWorld does not guarantee ordering between different mods' startup
                // constructors. The exact validated upstream type owns this initialization;
                // the CLR still guarantees it runs at most once.
                RuntimeHelpers.RunClassConstructor(startupType.TypeHandle);
            }

            var owner = FindExactCaravanPatchOwner(postfix);
            var supported = PrioritizeMealsCompatibility.IsSupported(
                assembly?.GetName().Name,
                assembly?.GetName().Version?.ToString(),
                startupType?.FullName,
                hasStartupAttribute,
                foodsType?.FullName,
                fieldShape,
                patchType?.FullName,
                postfix?.Name,
                postfixShape,
                owner,
                allTypesShareAssembly);
            if (!supported)
            {
                Enabled = false;
                reason =
                    "the installed Prioritize Meals over Preserved Foods shape differs from the validated 1.6 contract " +
                    $"(assembly={assembly?.GetName().Name ?? "missing"}; " +
                    $"version={assembly?.GetName().Version?.ToString() ?? "missing"}; " +
                    $"startup={startupType?.FullName ?? "missing"}; startupAttribute={hasStartupAttribute}; " +
                    $"foods={foodsType?.FullName ?? "missing"}; fieldShape={fieldShape}; " +
                    $"postfix={patchType?.FullName ?? "missing"}.{postfix?.Name ?? "missing"}; " +
                    $"postfixShape={postfixShape}; owner={owner ?? "missing"}; " +
                    $"sameAssembly={allTypesShareAssembly})";
                return false;
            }

            Enabled = true;
            reason = string.Empty;
            Log.Message(
                "[ImmersiveChefs] Prioritize Meals coexistence active; upstream food ordering and trader compensation remain authoritative.");
            return true;
        }
        catch (Exception exception)
        {
            Enabled = false;
            reason = $"Prioritize Meals shape validation failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    private static string? FindExactCaravanPatchOwner(MethodInfo? postfix)
    {
        if (postfix is null)
        {
            return null;
        }

        var matches = Harmony.GetAllPatchedMethods()
            .Where(target =>
                target.DeclaringType == typeof(IncidentWorker_TraderCaravanArrival) &&
                target.Name == "SendLetter")
            .SelectMany(target => Harmony.GetPatchInfo(target)?.Postfixes ?? Enumerable.Empty<Patch>())
            .Where(patch => patch.PatchMethod == postfix)
            .ToArray();
        return matches.Length == 1 ? matches[0].owner : null;
    }
}
