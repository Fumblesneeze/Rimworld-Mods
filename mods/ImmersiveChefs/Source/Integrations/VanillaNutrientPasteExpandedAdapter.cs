using HarmonyLib;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

internal enum PasteDispenserKind
{
    Unsupported,
    Vanilla,
    VanillaNutrientPasteExpanded
}

internal static class PasteDispenserCompatibility
{
    internal const string VanillaDefName = "NutrientPasteDispenser";
    internal const string VanillaTypeName = "RimWorld.Building_NutrientPasteDispenser";
    internal const string VnpeTapDefName = "VNPE_NutrientPasteTap";
    internal const string VnpeTapTypeName = "VNPE.Building_NutrientPasteTap";
    internal const string VnpeAssemblyName = "VNPE";
    internal const string PipeResourcePropertiesTypeName = "PipeSystem.CompProperties_Resource";

    internal static PasteDispenserKind Classify(
        string? defName,
        string? thingClassName,
        string? assemblyName,
        string? directBaseTypeName,
        IEnumerable<string>? componentTypeNames,
        bool vnpeEnabled)
    {
        if (defName == VanillaDefName &&
            thingClassName == VanillaTypeName &&
            assemblyName == "Assembly-CSharp")
        {
            return PasteDispenserKind.Vanilla;
        }

        if (!vnpeEnabled ||
            defName != VnpeTapDefName ||
            thingClassName != VnpeTapTypeName ||
            assemblyName != VnpeAssemblyName ||
            directBaseTypeName != VanillaTypeName)
        {
            return PasteDispenserKind.Unsupported;
        }

        var pipeComponents = componentTypeNames?.Count(name =>
            name == PipeResourcePropertiesTypeName) ?? 0;
        return pipeComponents == 1
            ? PasteDispenserKind.VanillaNutrientPasteExpanded
            : PasteDispenserKind.Unsupported;
    }
}

internal static class VanillaNutrientPasteExpandedAdapter
{
    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        var tapDef = DefDatabase<ThingDef>.GetNamedSilentFail(PasteDispenserCompatibility.VnpeTapDefName);
        var tapType = AccessTools.TypeByName(PasteDispenserCompatibility.VnpeTapTypeName);
        if (tapDef is null || tapType is null || tapDef.thingClass != tapType ||
            Describe(tapDef, tapType, vnpeEnabled: true) !=
            PasteDispenserKind.VanillaNutrientPasteExpanded)
        {
            reason = "the installed VNPE nutrient-paste tap Def/type/pipe shape no longer matches the validated 1.6 API";
            return false;
        }

        var canDispenseOverride = AccessTools.PropertyGetter(tapType, "CanDispenseNowOverride");
        var tryDispenseOverride = AccessTools.Method(tapType, "TryDispenseFoodOverride", Type.EmptyTypes);
        if (canDispenseOverride is null || canDispenseOverride.DeclaringType != tapType ||
            canDispenseOverride.IsStatic ||
            canDispenseOverride.ReturnType != typeof(bool) ||
            tryDispenseOverride is null || tryDispenseOverride.DeclaringType != tapType ||
            tryDispenseOverride.IsStatic ||
            !typeof(Thing).IsAssignableFrom(tryDispenseOverride.ReturnType))
        {
            reason = "the installed VNPE tap no longer exposes its validated network-backed dispense overrides";
            return false;
        }

        Enabled = true;
        reason = string.Empty;
        Log.Message("[ImmersiveChefs] Vanilla Nutrient Paste Expanded adapter active; prepared paste uses the native pipe-backed tap lifecycle.");
        return true;
    }

    internal static bool Controls(Thing thing)
    {
        return Controls(thing.def, thing.GetType());
    }

    internal static bool Controls(ThingDef def, Type runtimeType)
    {
        return Enabled &&
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.VanillaNutrientPasteExpanded) &&
            Describe(def, runtimeType, vnpeEnabled: true) ==
            PasteDispenserKind.VanillaNutrientPasteExpanded;
    }

    internal static void DisableAfterInvocationFailure(Exception exception)
    {
        Enabled = false;
        OptionalIntegrationDiagnostics.WarnOnce(
            OptionalIntegration.VanillaNutrientPasteExpanded,
            $"native tap dispense invocation failed ({exception.GetType().Name}: {exception.Message})");
    }

    private static PasteDispenserKind Describe(ThingDef def, Type runtimeType, bool vnpeEnabled)
    {
        return PasteDispenserCompatibility.Classify(
            def.defName,
            runtimeType.FullName,
            runtimeType.Assembly.GetName().Name,
            runtimeType.BaseType?.FullName,
            def.comps?.Select(properties => properties.GetType().FullName ?? string.Empty),
            vnpeEnabled);
    }
}
