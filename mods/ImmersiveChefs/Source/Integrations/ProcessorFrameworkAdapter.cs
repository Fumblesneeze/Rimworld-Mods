using System.Collections;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal static class ProcessorFrameworkAdapter
{
    private const string ProcessorTypeName = "ProcessorFramework.CompProcessor";
    private const string ProcessorPropertiesTypeName = "ProcessorFramework.CompProperties_Processor";
    private const string ProcessDefTypeName = "ProcessorFramework.ProcessDef";
    private const string ActiveProcessTypeName = "ProcessorFramework.ActiveProcess";

    private static Type? processorType;
    private static Type? processorPropertiesType;
    private static Type? processDefType;
    private static Type? activeProcessType;
    private static FieldInfo? processorInnerContainer;
    private static FieldInfo? processorActiveProcesses;
    private static FieldInfo? activeProcessIngredients;
    private static FieldInfo? activeProcessProcessor;
    private static PropertyInfo? activeProcessComplete;
    private static MethodInfo? spaceLeftFor;

    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(Harmony harmony, out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        if (!TryResolveShape(out reason))
        {
            return false;
        }

        try
        {
            AddProcessor(
                ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher,
                "ImmersiveChefs_DomesticDishwashing",
                16,
                2500);
            AddProcessor(
                ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher,
                "ImmersiveChefs_IndustrialDishwashing",
                64,
                1800);
            InstallPatches(harmony);
            RecacheFramework();
            Enabled = true;
            reason = string.Empty;
            Log.Message("[ImmersiveChefs] Processor Framework adapter active; reusable dish identity is preserved.");
            return true;
        }
        catch (Exception exception)
        {
            reason = $"shape initialization failed ({exception.GetType().Name}: {exception.Message})";
            Enabled = false;
            return false;
        }
    }

    internal static bool Controls(Thing thing)
    {
        return Enabled && IsDishwasher(thing) && thing is ThingWithComps withComps &&
               withComps.AllComps.Any(comp => processorType?.IsInstanceOfType(comp) == true);
    }

    private static bool TryResolveShape(out string reason)
    {
        processorType = AccessTools.TypeByName(ProcessorTypeName);
        processorPropertiesType = AccessTools.TypeByName(ProcessorPropertiesTypeName);
        processDefType = AccessTools.TypeByName(ProcessDefTypeName);
        activeProcessType = AccessTools.TypeByName(ActiveProcessTypeName);
        if (processorType is null || processorPropertiesType is null || processDefType is null ||
            activeProcessType is null || !typeof(ThingComp).IsAssignableFrom(processorType) ||
            !typeof(CompProperties).IsAssignableFrom(processorPropertiesType) ||
            !typeof(Def).IsAssignableFrom(processDefType))
        {
            reason = "required Processor Framework types were not found";
            return false;
        }

        processorInnerContainer = AccessTools.Field(processorType, "innerContainer");
        processorActiveProcesses = AccessTools.Field(processorType, "activeProcesses");
        activeProcessIngredients = AccessTools.Field(activeProcessType, "ingredientThings");
        activeProcessProcessor = AccessTools.Field(activeProcessType, "processor");
        activeProcessComplete = AccessTools.Property(activeProcessType, "Complete");
        spaceLeftFor = AccessTools.Method(processorType, "SpaceLeftFor");
        var requiredMethods = new[]
        {
            AccessTools.Method(processorType, "AddIngredient"),
            AccessTools.Method(processorType, "TakeOutProduct"),
            AccessTools.Method(activeProcessType, "CalcSpeedFactor")
        };
        if (processorInnerContainer is null || processorActiveProcesses is null ||
            activeProcessIngredients is null || activeProcessProcessor is null ||
            activeProcessComplete is null || spaceLeftFor is null || requiredMethods.Any(method => method is null))
        {
            reason = "the installed Processor Framework process lifecycle no longer matches the validated 1.6 shape";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static void AddProcessor(ThingDef buildingDef, string processDefName, int capacity, int cycleTicks)
    {
        if (buildingDef.comps?.Any(properties => processorPropertiesType!.IsInstanceOfType(properties)) == true)
        {
            return;
        }

        var process = Activator.CreateInstance(processDefType!)
                      ?? throw new InvalidOperationException("Processor process Def could not be created.");
        SetField(process, "defName", processDefName);
        SetField(process, "label", "wash reusable kitchenware");
        SetField(process, "thingDef", DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"));
        SetField(process, "processDays", Math.Max(1, cycleTicks) * ImmersiveChefsMod.Settings.DishwashingWorkScale / 60000f);
        SetField(process, "capacityFactor", 1f);
        SetField(process, "efficiency", 1f);
        SetField(process, "usesTemperature", false);
        SetField(process, "unpoweredFactor", 0f);
        SetField(process, "unfueledFactor", 0f);
        SetField(process, "destroyChance", 0f);

        var filter = new ThingFilter();
        foreach (var ware in DefDatabase<ThingDef>.AllDefsListForReading.Where(IsReusableWareDef))
        {
            filter.SetAllow(ware, true);
        }

        SetField(process, "ingredientFilter", filter);
        AddDef(process);
        AccessTools.Method(processDefType!, "ResolveReferences")!.Invoke(process, null);

        var properties = (CompProperties)(Activator.CreateInstance(processorPropertiesType!)
                         ?? throw new InvalidOperationException("Processor properties could not be created."));
        SetField(properties, "capacity", Math.Max(1, (int)Math.Round(
            capacity * ImmersiveChefsMod.Settings.DishwasherCapacityScale)));
        SetField(properties, "independentProcesses", true);
        SetField(properties, "parallelProcesses", true);
        SetField(properties, "dropIngredients", true);
        SetField(properties, "showProductIcon", true);
        SetField(properties, "colorCoded", false);
        var processList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(processDefType!))!;
        processList.Add(process);
        SetField(properties, "processes", processList);
        properties.ResolveReferences(buildingDef);
        buildingDef.drawerType = DrawerType.MapMeshAndRealTime;
        buildingDef.comps ??= new List<CompProperties>();
        buildingDef.comps.Add(properties);
    }

    private static void AddDef(object process)
    {
        var database = typeof(DefDatabase<>).MakeGenericType(processDefType!);
        var add = database.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == "Add" && method.GetParameters().Length == 1 &&
                              method.GetParameters()[0].ParameterType == processDefType);
        add.Invoke(null, new[] { process });
    }

    private static void InstallPatches(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(processorType!, "AddIngredient"),
            prefix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(AddIngredientPrefix)));
        harmony.Patch(
            AccessTools.Method(processorType!, "TakeOutProduct"),
            prefix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(TakeOutProductPrefix)));
        harmony.Patch(
            AccessTools.Method(activeProcessType!, "CalcSpeedFactor"),
            postfix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(CalcSpeedFactorPostfix)));

        var workGiverType = AccessTools.TypeByName("ProcessorFramework.WorkGiver_FillProcessor");
        var findIngredient = workGiverType is null ? null : AccessTools.Method(workGiverType, "FindIngredient");
        if (findIngredient is null)
        {
            throw new MissingMethodException("ProcessorFramework.WorkGiver_FillProcessor.FindIngredient");
        }

        harmony.Patch(
            findIngredient,
            postfix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(FindIngredientPostfix)));
    }

    private static bool AddIngredientPrefix(object __instance, Thing __0, object __1)
    {
        var parent = ParentOfProcessor(__instance);
        if (parent is null || !IsDishwasher(parent))
        {
            return true;
        }

        if (!IsDirtyWare(__0))
        {
            return false;
        }

        if (!UsesDubsWater())
        {
            return true;
        }

        var accepted = Math.Min(
            __0.stackCount,
            Math.Max(0, Convert.ToInt32(spaceLeftFor!.Invoke(__instance, new[] { __1, 1f }))));
        var water = accepted * WaterPerPlateEquivalent(parent, __0);
        return accepted > 0 && DubsWaterAdapter.TryConsumeCycleWater(parent, water, out _);
    }

    private static bool TakeOutProductPrefix(object __instance, object __0, ref Thing? __result)
    {
        var parent = ParentOfProcessor(__instance);
        if (parent is null || !IsDishwasher(parent))
        {
            return true;
        }

        var ingredients = activeProcessIngredients!.GetValue(__0) as IEnumerable;
        var originals = ingredients?.Cast<object>().OfType<Thing>().ToList() ?? new List<Thing>();
        if (originals.Count == 0)
        {
            return true;
        }

        var completed = activeProcessComplete!.GetValue(__0) is true;
        if (completed)
        {
            foreach (var thing in originals)
            {
                (thing as ThingWithComps)?.GetComp<CompSanitation>()?.MarkClean(WashProvenance.Safe);
            }
        }

        var owner = processorInnerContainer!.GetValue(__instance) as ThingOwner;
        foreach (var thing in originals)
        {
            owner?.Remove(thing);
        }

        (processorActiveProcesses!.GetValue(__instance) as IList)?.Remove(__0);
        __result = originals[0];
        for (var index = 1; index < originals.Count; index++)
        {
            if (parent.Map is { } map)
            {
                GenPlace.TryPlaceThing(originals[index], parent.InteractionCell, map, ThingPlaceMode.Near);
            }
        }

        return false;
    }

    private static void CalcSpeedFactorPostfix(object __instance, ref float __result)
    {
        if (!UsesDubsWater())
        {
            return;
        }

        var processor = activeProcessProcessor!.GetValue(__instance);
        var parent = processor is null ? null : ParentOfProcessor(processor);
        if (parent is not null && IsDishwasher(parent) && !DubsWaterAdapter.IsConnected(parent))
        {
            __result = 0f;
        }
    }

    private static void FindIngredientPostfix(Pawn __0, object __1, ref Thing? __result)
    {
        var parent = ParentOfProcessor(__1);
        if (parent is null || !IsDishwasher(parent))
        {
            return;
        }

        if (UsesDubsWater() && !DubsWaterAdapter.CanSupplyCycleWater(parent, 0.001f))
        {
            __result = null;
            return;
        }

        __result = __0.Map.listerThings.AllThings
            .Where(IsDirtyWare)
            .Where(thing => !thing.IsForbidden(__0) &&
                            __0.CanReserveAndReach(thing, Verse.AI.PathEndMode.Touch, Danger.Some))
            .OrderBy(thing => thing.Position.DistanceToSquared(parent.Position))
            .FirstOrDefault();
    }

    private static ThingWithComps? ParentOfProcessor(object processor)
    {
        return processor is ThingComp comp ? comp.parent : null;
    }

    private static bool IsDishwasher(Thing thing)
    {
        return thing.def == ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher ||
               thing.def == ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher;
    }

    private static bool IsReusableWareDef(ThingDef def)
    {
        return def.GetModExtension<KitchenwareExtension>()?.product is
            KitchenwareProduct.Cookware or KitchenwareProduct.Plate or KitchenwareProduct.Cutlery;
    }

    private static bool IsDirtyWare(Thing thing)
    {
        return IsReusableWareDef(thing.def) &&
               (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true;
    }

    private static bool UsesDubsWater()
    {
        return ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene);
    }

    private static float WaterPerPlateEquivalent(Thing appliance, Thing ware)
    {
        var local = (appliance as ThingWithComps)?.GetComp<CompDishwasher>();
        var perEquivalent = local?.WaterPerPlateEquivalent ?? 0.1f;
        var equivalent = ware.def.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f;
        return Math.Max(0.001f, perEquivalent * Math.Max(0.01f, equivalent));
    }

    private static void RecacheFramework()
    {
        AccessTools.Method(
            AccessTools.TypeByName("ProcessorFramework.ProcessorFramework_Utility"),
            "RecacheAll")?.Invoke(null, null);
    }

    private static void SetField(object target, string fieldName, object? value)
    {
        var field = AccessTools.Field(target.GetType(), fieldName)
                    ?? throw new MissingFieldException(target.GetType().FullName, fieldName);
        field.SetValue(target, value);
    }
}
