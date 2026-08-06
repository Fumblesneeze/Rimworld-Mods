using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal enum DispenserMealSource
{
    VanillaPaste,
    Replimat,
    MealPrinter
}

internal static class DispenserMealBindingPolicy
{
    internal static bool ShouldBind(
        DispenserMealSource source,
        string? mealDefName,
        bool covered,
        int stackCount)
    {
        if (!covered || stackCount != 1 || string.IsNullOrWhiteSpace(mealDefName))
        {
            return false;
        }

        return source switch
        {
            DispenserMealSource.VanillaPaste => mealDefName == "MealNutrientPaste",
            DispenserMealSource.Replimat => true,
            DispenserMealSource.MealPrinter => mealDefName != "MealPrinter_NutriBar",
            _ => false
        };
    }

    internal static bool UsePasteInitialization(DispenserMealSource source, string? mealDefName)
    {
        return mealDefName == "MealNutrientPaste" &&
               source is DispenserMealSource.VanillaPaste or DispenserMealSource.MealPrinter;
    }
}

internal static class DispenserMealResolutionPolicy
{
    internal static bool Failed(bool sourceRecognized, bool resolutionSucceeded)
    {
        return sourceRecognized && !resolutionSucceeded;
    }
}

internal static class ReplimatCompatibility
{
    internal const string AssemblyName = "Replimat";
    internal const string AssemblyVersion = "1.0.0.0";
    internal const string TerminalTypeName = "Replimat.Building_ReplimatTerminal";
    internal const string UtilityTypeName = "Replimat.ReplimatUtility";
    internal const string ToilPrefixTypeName = "Replimat.Harmony_Toils_Ingest_TakeMealFromDispenser";
    internal const string UpstreamPatchOwner = "com.Replimat.patches";

    internal static bool HasSupportedMealResolver(
        string? utilityTypeName,
        bool sharesTerminalAssembly,
        IEnumerable<string> parameterTypeNames,
        bool returnsThingDef)
    {
        return utilityTypeName == UtilityTypeName &&
               sharesTerminalAssembly &&
               parameterTypeNames.SequenceEqual(
                   new[] { "Verse.Pawn", "Verse.Pawn" },
                   StringComparer.Ordinal) &&
               returnsThingDef;
    }

    internal static bool IsSupported(
        string? assemblyName,
        string? assemblyVersion,
        string? terminalTypeName,
        bool terminalIsPasteDispenser,
        IEnumerable<string> tryDispenseParameterTypeNames,
        bool tryDispenseReturnsThing,
        string? toilPrefixTypeName,
        bool toilPrefixShape,
        string? upstreamPatchOwner,
        IEnumerable<string> terminalDefNames,
        bool mealRegistryValidated)
    {
        return assemblyName == AssemblyName &&
               assemblyVersion == AssemblyVersion &&
               terminalTypeName == TerminalTypeName &&
               terminalIsPasteDispenser &&
               tryDispenseParameterTypeNames.SequenceEqual(new[]
               {
                   "Verse.Pawn", "Verse.Pawn", "Verse.ThingDef", "System.Int32"
               }, StringComparer.Ordinal) &&
               tryDispenseReturnsThing &&
               toilPrefixTypeName == ToilPrefixTypeName &&
               toilPrefixShape &&
               upstreamPatchOwner == UpstreamPatchOwner &&
               terminalDefNames.SequenceEqual(
                   new[] { "ReplimatTerminal", "ReplimatTerminalWall" },
                   StringComparer.Ordinal) &&
               mealRegistryValidated;
    }
}

internal static class MealPrinterCompatibility
{
    internal const string AssemblyName = "MealPrinter";
    internal const string AssemblyVersion = "0.0.0.0";
    internal const string PrinterTypeName = "MealPrinter.Building_MealPrinter";
    internal const string StartupTypeName = "MealPrinter.MealPrinterMain";
    internal const string ToilPrefixTypeName =
        "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser";
    internal const string UpstreamPatchOwner = "MealPrinter";

    internal static bool HasSupportedMealResolver(
        string? methodName,
        bool sharesPrinterType,
        int parameterCount,
        bool returnsThingDef)
    {
        return methodName == "GetMealThing" &&
               sharesPrinterType &&
               parameterCount == 0 &&
               returnsThingDef;
    }

    internal static bool HasSupportedStartupType(
        string? startupTypeName,
        bool hasStartupAttribute,
        bool sharesPrinterAssembly)
    {
        return startupTypeName == StartupTypeName &&
               hasStartupAttribute &&
               sharesPrinterAssembly;
    }

    internal static bool HasSupportedPassiveShape(
        string? assemblyName,
        string? assemblyVersion,
        string? printerTypeName,
        bool printerIsPasteDispenser,
        int tryDispenseParameterCount,
        bool tryDispenseReturnsThing,
        string? toilPrefixTypeName,
        bool toilPrefixShape,
        string? printerDefName,
        string? nutriBarDefName,
        bool vanillaMealDefsPresent)
    {
        return assemblyName == AssemblyName &&
               assemblyVersion == AssemblyVersion &&
               printerTypeName == PrinterTypeName &&
               printerIsPasteDispenser &&
               tryDispenseParameterCount == 0 &&
               tryDispenseReturnsThing &&
               toilPrefixTypeName == ToilPrefixTypeName &&
               toilPrefixShape &&
               printerDefName == "MealPrinter" &&
               nutriBarDefName == "MealPrinter_NutriBar" &&
               vanillaMealDefsPresent;
    }

    internal static bool IsSupported(
        string? assemblyName,
        string? assemblyVersion,
        string? printerTypeName,
        bool printerIsPasteDispenser,
        int tryDispenseParameterCount,
        bool tryDispenseReturnsThing,
        string? toilPrefixTypeName,
        bool toilPrefixShape,
        string? upstreamPatchOwner,
        string? printerDefName,
        string? nutriBarDefName,
        bool vanillaMealDefsPresent)
    {
        return HasSupportedPassiveShape(
                   assemblyName,
                   assemblyVersion,
                   printerTypeName,
                   printerIsPasteDispenser,
                   tryDispenseParameterCount,
                   tryDispenseReturnsThing,
                   toilPrefixTypeName,
                   toilPrefixShape,
                   printerDefName,
                   nutriBarDefName,
                   vanillaMealDefsPresent) &&
               upstreamPatchOwner == UpstreamPatchOwner;
    }
}

internal static class DispenserMealBindingRuntime
{
    internal static void TryBind(
        Building_NutrientPasteDispenser dispenser,
        Pawn? getter,
        Thing? result,
        DispenserMealSource source)
    {
        if (getter is null || result is null ||
            !DiningPawnPolicy.AppliesDiningConsequences(getter.RaceProps.Humanlike) ||
            getter.CurJob?.GetTarget(TargetIndex.A).Thing != dispenser ||
            !DispenserMealBindingPolicy.ShouldBind(
                source,
                result.def.defName,
                MealCoveragePolicy.IsCovered(result.def),
                result.stackCount))
        {
            return;
        }

        if (DispenserMealBindingPolicy.UsePasteInitialization(source, result.def.defName))
        {
            DiningSessionRegistry.BindPastePlate(getter, result);
        }
        else
        {
            DiningSessionRegistry.BindReservedPlate(getter, result);
        }
    }

    internal static Pawn? FindCurrentGetter(Building_NutrientPasteDispenser dispenser)
    {
        var candidates = dispenser.Map?.mapPawns.AllPawnsSpawned
            .Where(candidate => candidate.RaceProps.Humanlike &&
                                candidate.Position == dispenser.InteractionCell &&
                                candidate.CurJob?.GetTarget(TargetIndex.A).Thing == dispenser)
            .Take(2)
            .ToArray();
        return candidates?.Length == 1 ? candidates[0] : null;
    }
}

internal static class ReplimatAdapter
{
    private static Type? supportedTerminalType;
    private static MethodInfo? nativeMealResolver;

    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(Harmony harmony, out string reason)
    {
        try
        {
            return TryInitializeCore(harmony, out reason);
        }
        catch (Exception exception)
        {
            Reset();
            reason = $"Replimat shape validation failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    private static bool TryInitializeCore(Harmony harmony, out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        var terminalType = AccessTools.TypeByName(ReplimatCompatibility.TerminalTypeName);
        var utilityType = AccessTools.TypeByName(ReplimatCompatibility.UtilityTypeName);
        var prefixType = AccessTools.TypeByName(ReplimatCompatibility.ToilPrefixTypeName);
        var assembly = terminalType?.Assembly;
        var method = terminalType?.GetMethod(
            "TryDispenseFood",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(Pawn), typeof(Pawn), typeof(ThingDef), typeof(int) },
            modifiers: null);
        var mealResolver = utilityType?.GetMethod(
            "PickMeal",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Pawn), typeof(Pawn) },
            modifiers: null);
        var prefix = ResolveToilPrefix(prefixType);
        var upstreamTarget = AccessTools.Method(
            typeof(Toils_Ingest),
            nameof(Toils_Ingest.TakeMealFromDispenser),
            new[] { typeof(TargetIndex), typeof(Pawn) });
        var patchOwner = FindPatchOwner(upstreamTarget, prefix);
        var terminalDefs = new[] { "ReplimatTerminal", "ReplimatTerminalWall" }
            .Select(DefDatabase<ThingDef>.GetNamedSilentFail)
            .ToArray();
        var terminalDefShape = terminalDefs.All(def =>
            def is not null &&
            def.thingClass == terminalType &&
            string.Equals(
                def.modContentPack?.PackageId,
                MealClassificationCatalog.ReplimatPackageId,
                StringComparison.OrdinalIgnoreCase));
        var mealRegistryValidated = MealClassificationCatalog.ReplimatMealDefNames.All(defName =>
            MealClassificationRuntime.IsMealRegisteredForActivePackage(
                MealClassificationCatalog.ReplimatMealsPackageId,
                defName));
        var supported = ReplimatCompatibility.IsSupported(
            assembly?.GetName().Name,
            assembly?.GetName().Version?.ToString(),
            terminalType?.FullName,
            terminalType is not null &&
            typeof(Building_NutrientPasteDispenser).IsAssignableFrom(terminalType),
            method?.GetParameters().Select(parameter => parameter.ParameterType.FullName ?? string.Empty) ??
            Array.Empty<string>(),
            method?.ReturnType == typeof(Thing),
            prefixType?.FullName,
            IsToilPrefixShape(prefix, prefixType) && prefixType?.Assembly == assembly,
            patchOwner,
            terminalDefShape
                ? terminalDefs.Select(def => def!.defName)
                : Array.Empty<string>(),
            mealRegistryValidated) &&
            ReplimatCompatibility.HasSupportedMealResolver(
                utilityType?.FullName,
                utilityType?.Assembly == assembly,
                mealResolver?.GetParameters()
                    .Select(parameter => parameter.ParameterType.FullName ?? string.Empty) ??
                Array.Empty<string>(),
                mealResolver?.ReturnType == typeof(ThingDef));
        if (!supported || method is null || mealResolver is null)
        {
            reason =
                "the installed Replimat shape differs from the validated 1.6 contract " +
                $"(assembly={assembly?.GetName().Name ?? "missing"}; " +
                $"version={assembly?.GetName().Version?.ToString() ?? "missing"}; " +
                $"terminal={terminalType?.FullName ?? "missing"}; method={method?.Name ?? "missing"}; " +
                $"resolver={utilityType?.FullName ?? "missing"}.{mealResolver?.Name ?? "missing"}; " +
                $"prefix={prefixType?.FullName ?? "missing"}; owner={patchOwner ?? "missing"}; " +
                $"terminalDefs={terminalDefShape}; mealRegistry={mealRegistryValidated})";
            return false;
        }

        harmony.Patch(
            method,
            prefix: new HarmonyMethod(typeof(ReplimatAdapter), nameof(Prefix)),
            postfix: new HarmonyMethod(typeof(ReplimatAdapter), nameof(Postfix)));
        supportedTerminalType = terminalType;
        nativeMealResolver = mealResolver;
        Enabled = true;
        reason = string.Empty;
        Log.Message("[ImmersiveChefs] Replimat adapter active; exact carried plates bind after native network dispensing.");
        return true;
    }

    internal static bool AppliesTo(Thing foodSource)
    {
        return supportedTerminalType is not null && foodSource.GetType() == supportedTerminalType;
    }

    internal static bool TryResolveMealDef(
        Thing foodSource,
        Pawn eater,
        Pawn getter,
        out ThingDef? mealDef)
    {
        mealDef = null;
        if (!Enabled || !AppliesTo(foodSource) || nativeMealResolver is null)
        {
            return false;
        }

        try
        {
            mealDef = nativeMealResolver.Invoke(null, new object[] { eater, getter }) as ThingDef;
            return mealDef is not null;
        }
        catch (Exception exception)
        {
            DisableAfterInvocationFailure(exception);
            return false;
        }
    }

    private static void Prefix(Pawn eater, Pawn getter, ref ThingDef? specifiedMeal)
    {
        if (!Enabled || specifiedMeal is not null)
        {
            return;
        }

        specifiedMeal = DiningSessionRegistry.RequestedMealDefFor(getter);
    }

    private static void Postfix(
        object __instance,
        Pawn getter,
        Thing? __result)
    {
        if (!Enabled || __instance is not Building_NutrientPasteDispenser dispenser)
        {
            return;
        }

        try
        {
            DispenserMealBindingRuntime.TryBind(
                dispenser,
                getter,
                __result,
                DispenserMealSource.Replimat);
        }
        catch (Exception exception)
        {
            DisableAfterInvocationFailure(exception);
        }
    }

    private static void DisableAfterInvocationFailure(Exception exception)
    {
        Enabled = false;
        nativeMealResolver = null;
        OptionalIntegrationDiagnostics.WarnOnce(
            OptionalIntegration.Replimat,
            $"native meal resolution or post-dispense plate binding failed " +
            $"({exception.GetType().Name}: {exception.Message})");
    }

    private static void Reset()
    {
        Enabled = false;
        supportedTerminalType = null;
        nativeMealResolver = null;
    }

    private static MethodInfo? ResolveToilPrefix(Type? prefixType)
    {
        return prefixType?.GetMethod(
            "Prefix",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[]
            {
                typeof(TargetIndex).MakeByRefType(),
                typeof(Pawn).MakeByRefType(),
                typeof(Toil).MakeByRefType()
            },
            modifiers: null);
    }

    private static bool IsToilPrefixShape(MethodInfo? prefix, Type? prefixType)
    {
        return prefix?.ReturnType == typeof(bool) && prefix.DeclaringType == prefixType;
    }

    private static string? FindPatchOwner(MethodBase? target, MethodInfo? prefix)
    {
        return target is null || prefix is null
            ? null
            : Harmony.GetPatchInfo(target)?.Prefixes
                .SingleOrDefault(patch => patch.PatchMethod == prefix)?.owner;
    }
}

internal static class MealPrinterAdapter
{
    private static Type? supportedPrinterType;
    private static MethodInfo? nativeMealResolver;

    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(Harmony harmony, out string reason)
    {
        try
        {
            return TryInitializeCore(harmony, out reason);
        }
        catch (Exception exception)
        {
            Reset();
            reason = $"Meal Printer shape validation failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    private static bool TryInitializeCore(Harmony harmony, out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        var printerType = AccessTools.TypeByName(MealPrinterCompatibility.PrinterTypeName);
        var startupType = AccessTools.TypeByName(MealPrinterCompatibility.StartupTypeName);
        var prefixType = AccessTools.TypeByName(MealPrinterCompatibility.ToilPrefixTypeName);
        var assembly = printerType?.Assembly;
        var method = printerType?.GetMethod(
            "TryDispenseFood",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        var mealResolver = printerType?.GetMethod(
            "GetMealThing",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        var prefix = prefixType?.GetMethod(
            "Prefix",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[]
            {
                typeof(TargetIndex).MakeByRefType(),
                typeof(Pawn).MakeByRefType(),
                typeof(Toil).MakeByRefType()
            },
            modifiers: null);
        var printerDef = DefDatabase<ThingDef>.GetNamedSilentFail("MealPrinter");
        var nutriBarDef = DefDatabase<ThingDef>.GetNamedSilentFail("MealPrinter_NutriBar");
        var defOwnerShape = printerDef is not null &&
                            printerDef.thingClass == printerType &&
                            string.Equals(
                                printerDef.modContentPack?.PackageId,
                                "Mlie.MealPrinter",
                                StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(
                                nutriBarDef?.modContentPack?.PackageId,
                                "Mlie.MealPrinter",
                                StringComparison.OrdinalIgnoreCase);
        var vanillaMealDefsPresent = new[] { "MealNutrientPaste", "MealSimple", "MealFine" }
            .All(defName => DefDatabase<ThingDef>.GetNamedSilentFail(defName) is not null);
        var prefixShape = prefix?.ReturnType == typeof(bool) &&
                          prefix.DeclaringType == prefixType &&
                          prefixType?.Assembly == assembly;
        var startupTypeSupported = MealPrinterCompatibility.HasSupportedStartupType(
            startupType?.FullName,
            startupType?.CustomAttributes.Any(attribute =>
                attribute.AttributeType == typeof(StaticConstructorOnStartup)) == true,
            startupType?.Assembly == assembly);
        var mealResolverSupported = MealPrinterCompatibility.HasSupportedMealResolver(
            mealResolver?.Name,
            mealResolver?.DeclaringType == printerType,
            mealResolver?.GetParameters().Length ?? -1,
            mealResolver?.ReturnType == typeof(ThingDef));
        var passiveShapeSupported = MealPrinterCompatibility.HasSupportedPassiveShape(
            assembly?.GetName().Name,
            assembly?.GetName().Version?.ToString(),
            printerType?.FullName,
            printerType is not null &&
            typeof(Building_NutrientPasteDispenser).IsAssignableFrom(printerType),
            method?.GetParameters().Length ?? -1,
            method?.ReturnType == typeof(Thing),
            prefixType?.FullName,
            prefixShape,
            defOwnerShape ? printerDef?.defName : null,
            defOwnerShape ? nutriBarDef?.defName : null,
            vanillaMealDefsPresent);
        if (passiveShapeSupported && startupTypeSupported && mealResolverSupported && startupType is not null)
        {
            // RimWorld does not guarantee startup-constructor order across mods. Only after
            // validating the exact passive assembly/type/method/Def shape do we let the
            // upstream patch class initialize itself; the CLR guarantees this runs once.
            RuntimeHelpers.RunClassConstructor(startupType.TypeHandle);
        }

        var upstreamTarget = AccessTools.Method(
            typeof(Toils_Ingest),
            nameof(Toils_Ingest.TakeMealFromDispenser),
            new[] { typeof(TargetIndex), typeof(Pawn) });
        var patchOwner = upstreamTarget is null || prefix is null
            ? null
            : Harmony.GetPatchInfo(upstreamTarget)?.Prefixes
                .SingleOrDefault(patch => patch.PatchMethod == prefix)?.owner;
        var supported = startupTypeSupported && mealResolverSupported && MealPrinterCompatibility.IsSupported(
            assembly?.GetName().Name,
            assembly?.GetName().Version?.ToString(),
            printerType?.FullName,
            printerType is not null &&
            typeof(Building_NutrientPasteDispenser).IsAssignableFrom(printerType),
            method?.GetParameters().Length ?? -1,
            method?.ReturnType == typeof(Thing),
            prefixType?.FullName,
            prefixShape,
            patchOwner,
            defOwnerShape ? printerDef?.defName : null,
            defOwnerShape ? nutriBarDef?.defName : null,
            vanillaMealDefsPresent);
        if (!supported || method is null || mealResolver is null)
        {
            reason =
                "the installed Meal Printer shape differs from the validated 1.6 contract " +
                $"(assembly={assembly?.GetName().Name ?? "missing"}; " +
                $"version={assembly?.GetName().Version?.ToString() ?? "missing"}; " +
                $"printer={printerType?.FullName ?? "missing"}; method={method?.Name ?? "missing"}; " +
                $"startup={startupType?.FullName ?? "missing"}; startupShape={startupTypeSupported}; " +
                $"resolver={mealResolver?.Name ?? "missing"}; resolverShape={mealResolverSupported}; " +
                $"prefix={prefixType?.FullName ?? "missing"}; owner={patchOwner ?? "missing"}; " +
                $"defs={defOwnerShape}; vanillaMeals={vanillaMealDefsPresent})";
            return false;
        }

        harmony.Patch(
            method,
            postfix: new HarmonyMethod(typeof(MealPrinterAdapter), nameof(Postfix)));
        supportedPrinterType = printerType;
        nativeMealResolver = mealResolver;
        Enabled = true;
        reason = string.Empty;
        Log.Message("[ImmersiveChefs] Meal Printer adapter active; exact carried plates bind after native hopper dispensing.");
        return true;
    }

    internal static bool AppliesTo(Thing foodSource)
    {
        return supportedPrinterType is not null && foodSource.GetType() == supportedPrinterType;
    }

    internal static bool TryResolveMealDef(Thing foodSource, out ThingDef? mealDef)
    {
        mealDef = null;
        if (!Enabled || !AppliesTo(foodSource) || nativeMealResolver is null)
        {
            return false;
        }

        try
        {
            mealDef = nativeMealResolver.Invoke(foodSource, Array.Empty<object>()) as ThingDef;
            return mealDef is not null;
        }
        catch (Exception exception)
        {
            DisableAfterRuntimeFailure();
            OptionalIntegrationDiagnostics.WarnOnce(
                OptionalIntegration.MealPrinter,
                $"configured meal resolution failed ({exception.GetType().Name}: {exception.Message})");
            return false;
        }
    }

    private static void Postfix(object __instance, Thing? __result)
    {
        if (!Enabled || __instance is not Building_NutrientPasteDispenser dispenser)
        {
            return;
        }

        try
        {
            DispenserMealBindingRuntime.TryBind(
                dispenser,
                DispenserMealBindingRuntime.FindCurrentGetter(dispenser),
                __result,
                DispenserMealSource.MealPrinter);
        }
        catch (Exception exception)
        {
            DisableAfterRuntimeFailure();
            OptionalIntegrationDiagnostics.WarnOnce(
                OptionalIntegration.MealPrinter,
                $"post-dispense plate binding failed ({exception.GetType().Name}: {exception.Message})");
        }
    }

    private static void DisableAfterRuntimeFailure()
    {
        Enabled = false;
        nativeMealResolver = null;
    }

    private static void Reset()
    {
        Enabled = false;
        supportedPrinterType = null;
        nativeMealResolver = null;
    }
}
