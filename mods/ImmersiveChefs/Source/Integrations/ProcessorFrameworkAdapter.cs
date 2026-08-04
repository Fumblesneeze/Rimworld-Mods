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
    private static FieldInfo? processorEnabledProcesses;
    private static FieldInfo? activeProcessIngredients;
    private static FieldInfo? activeProcessProcessor;
    private static FieldInfo? processIngredientFilter;
    private static FieldInfo? processorEmptyNow;
    private static PropertyInfo? activeProcessComplete;
    private static PropertyInfo? activeProcessPercent;
    private static MethodInfo? spaceLeftFor;
    private static MethodInfo? graphicChange;
    private static MethodInfo? enableAllProcesses;
    private static MethodInfo? findIngredient;
    private static MethodInfo? resolveProcessReferences;
    private static MethodInfo? addProcessDef;
    private static MethodInfo? recacheAll;

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
                2500);
            AddProcessor(
                ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher,
                "ImmersiveChefs_IndustrialDishwashing",
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
        processorEnabledProcesses = AccessTools.Field(processorType, "enabledProcesses");
        activeProcessIngredients = AccessTools.Field(activeProcessType, "ingredientThings");
        activeProcessProcessor = AccessTools.Field(activeProcessType, "processor");
        processIngredientFilter = AccessTools.Field(processDefType, "ingredientFilter");
        processorEmptyNow = AccessTools.Field(processorType, "emptyNow");
        activeProcessComplete = AccessTools.Property(activeProcessType, "Complete");
        activeProcessPercent = AccessTools.Property(activeProcessType, "ActiveProcessPercent");
        spaceLeftFor = AccessTools.Method(processorType, "SpaceLeftFor");
        graphicChange = AccessTools.Method(processorType, "GraphicChange");
        enableAllProcesses = AccessTools.Method(processorType, "EnableAllProcesses");
        var workGiverType = AccessTools.TypeByName("ProcessorFramework.WorkGiver_FillProcessor");
        findIngredient = workGiverType is null ? null : AccessTools.Method(workGiverType, "FindIngredient");
        resolveProcessReferences = AccessTools.Method(processDefType, "ResolveReferences");
        recacheAll = AccessTools.Method(
            AccessTools.TypeByName("ProcessorFramework.ProcessorFramework_Utility"),
            "RecacheAll");
        addProcessDef = typeof(DefDatabase<>).MakeGenericType(processDefType)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(method => method.Name == "Add" && method.GetParameters().Length == 1 &&
                                       method.GetParameters()[0].ParameterType == processDefType);
        var requiredMethods = new[]
        {
            AccessTools.Method(processorType, "Initialize"),
            AccessTools.Method(processorType, "AddIngredient"),
            AccessTools.Method(processorType, "TakeOutProduct"),
            AccessTools.Method(processorType, "DoTicks"),
            AccessTools.Method(activeProcessType, "CalcSpeedFactor")
        };
        var requiredProcessFields = new[]
        {
            "defName",
            "label",
            "thingDef",
            "processDays",
            "capacityFactor",
            "efficiency",
            "usesTemperature",
            "unpoweredFactor",
            "unfueledFactor",
            "destroyChance",
            "ingredientFilter"
        };
        var requiredProcessorPropertyFields = new[]
        {
            "capacity",
            "independentProcesses",
            "parallelProcesses",
            "dropIngredients",
            "showProductIcon",
            "colorCoded",
            "processes"
        };
        if (processorInnerContainer is null || processorActiveProcesses is null ||
            processorEnabledProcesses is null ||
            activeProcessIngredients is null || activeProcessProcessor is null ||
            processIngredientFilter is null || processorEmptyNow is null ||
            activeProcessComplete is null || activeProcessPercent is null ||
            spaceLeftFor is null || graphicChange is null || enableAllProcesses is null ||
            findIngredient is null || resolveProcessReferences is null || addProcessDef is null ||
            recacheAll is null || requiredMethods.Any(method => method is null) ||
            requiredProcessFields.Any(fieldName => AccessTools.Field(processDefType, fieldName) is null) ||
            requiredProcessorPropertyFields.Any(fieldName =>
                AccessTools.Field(processorPropertiesType, fieldName) is null))
        {
            reason = "the installed Processor Framework process lifecycle no longer matches the validated 1.6 shape";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static void AddProcessor(ThingDef buildingDef, string processDefName, int cycleTicks)
    {
        if (buildingDef.comps?.Any(properties => processorPropertiesType!.IsInstanceOfType(properties)) == true)
        {
            return;
        }

        var processList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(processDefType!))!;
        foreach (var ware in DefDatabase<ThingDef>.AllDefsListForReading
                     .Where(IsReusableWareDef)
                     .OrderBy(def => def.defName, StringComparer.Ordinal))
        {
            var process = CreateProcess(
                $"{processDefName}_{ware.defName}",
                ware,
                cycleTicks);
            processList.Add(process);
        }

        if (processList.Count == 0)
        {
            throw new InvalidOperationException("No reusable kitchenware Defs were available for Processor Framework.");
        }

        var properties = (CompProperties)(Activator.CreateInstance(processorPropertiesType!)
                         ?? throw new InvalidOperationException("Processor properties could not be created."));
        var dishwasherProperties = buildingDef.comps?
            .OfType<CompProperties_Dishwasher>()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Dishwasher capacity properties were not finalized.");
        SetField(properties, "capacity", Math.Max(1, (int)Math.Round(
            dishwasherProperties.basePlateCapacity)));
        SetField(properties, "independentProcesses", true);
        SetField(properties, "parallelProcesses", true);
        SetField(properties, "dropIngredients", true);
        SetField(properties, "showProductIcon", true);
        SetField(properties, "colorCoded", false);
        SetField(properties, "processes", processList);
        properties.ResolveReferences(buildingDef);
        buildingDef.drawerType = DrawerType.MapMeshAndRealTime;
        buildingDef.comps ??= new List<CompProperties>();
        buildingDef.comps.Add(properties);
    }

    private static object CreateProcess(string defName, ThingDef ware, int cycleTicks)
    {
        var process = Activator.CreateInstance(processDefType!)
                      ?? throw new InvalidOperationException("Processor process Def could not be created.");
        SetField(process, "defName", defName);
        SetField(process, "label", $"wash {ware.label}");
        SetField(process, "thingDef", ware);
        SetField(process, "processDays",
            Math.Max(1, cycleTicks) * ImmersiveChefsMod.Settings.DishwashingWorkScale / 60000f);
        SetField(process, "capacityFactor", DishwasherCapacityPolicy.ProcessorCapacityFactor(
            ware.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f));
        SetField(process, "efficiency", 1f);
        SetField(process, "usesTemperature", false);
        SetField(process, "unpoweredFactor", 0f);
        SetField(process, "unfueledFactor", 0f);
        SetField(process, "destroyChance", 0f);

        var filter = new ThingFilter();
        filter.SetAllow(ware, true);
        SetField(process, "ingredientFilter", filter);
        AddDef(process);
        resolveProcessReferences!.Invoke(process, null);
        return process;
    }

    private static void AddDef(object process)
    {
        addProcessDef!.Invoke(null, new[] { process });
    }

    private static void InstallPatches(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(processorType!, "Initialize"),
            postfix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(ProcessorInitializePostfix)));
        harmony.Patch(
            AccessTools.Method(processorType!, "AddIngredient"),
            prefix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(AddIngredientPrefix)),
            postfix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(AddIngredientPostfix)));
        harmony.Patch(
            AccessTools.Method(processorType!, "TakeOutProduct"),
            prefix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(TakeOutProductPrefix)));
        harmony.Patch(
            AccessTools.Method(processorType!, "DoTicks"),
            prefix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(ProcessorDoTicksPrefix)));
        harmony.Patch(
            AccessTools.Method(activeProcessType!, "CalcSpeedFactor"),
            postfix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(CalcSpeedFactorPostfix)));

        harmony.Patch(
            findIngredient!,
            postfix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(FindIngredientPostfix)));
    }

    private static bool AddIngredientPrefix(object __instance, Thing __0, ref int __state)
    {
        __state = -1;
        var parent = ParentOfProcessor(__instance);
        if (parent is null || !IsDishwasher(parent))
        {
            return true;
        }

        if (!IsDirtyWare(__0))
        {
            return false;
        }

        var dishwasher = parent.GetComp<CompDishwasher>();
        var before = ProcessorInputCount(__instance);
        if (dishwasher is null || !dishwasher.CanAcceptProcessorWare(before > 0))
        {
            return false;
        }

        __state = before;
        return true;
    }

    private static void ProcessorInitializePostfix(object __instance)
    {
        var parent = ParentOfProcessor(__instance);
        if (parent is not null && IsDishwasher(parent))
        {
            enableAllProcesses!.Invoke(__instance, null);
        }
    }

    private static void AddIngredientPostfix(object __instance, int __state)
    {
        if (__state < 0)
        {
            return;
        }

        var parent = ParentOfProcessor(__instance);
        var after = ProcessorInputCount(__instance);
        if (parent is null || after <= __state)
        {
            return;
        }

        parent.GetComp<CompDishwasher>()?.NotifyProcessorAdmission(startedNewBatch: __state == 0);
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

        var processes = processorActiveProcesses!.GetValue(__instance) as IList;
        processes?.Remove(__0);
        __result = originals[0];
        for (var index = 1; index < originals.Count; index++)
        {
            if (parent.Map is { } map)
            {
                GenPlace.TryPlaceThing(originals[index], parent.InteractionCell, map, ThingPlaceMode.Near);
            }
        }

        if (processes?.Count == 0)
        {
            FinalizeEmptyProcessor(__instance, parent);
        }

        return false;
    }

    private static void CalcSpeedFactorPostfix(object __instance, ref float __result)
    {
        var processor = activeProcessProcessor!.GetValue(__instance);
        var parent = processor is null ? null : ParentOfProcessor(processor);
        if (parent is not null && IsDishwasher(parent) &&
            parent.GetComp<CompDishwasher>()?.ProcessorCycleCanProgress() != true)
        {
            __result = 0f;
        }
    }

    private static bool ProcessorDoTicksPrefix(object __instance)
    {
        var parent = ParentOfProcessor(__instance);
        return parent is null || !IsDishwasher(parent) ||
               parent.GetComp<CompDishwasher>()?.ProcessorCycleCanProgress() == true;
    }

    private static void FindIngredientPostfix(Pawn __0, object __1, ref Thing? __result)
    {
        var parent = ParentOfProcessor(__1);
        if (parent is null || !IsDishwasher(parent))
        {
            return;
        }

        var dishwasher = parent.GetComp<CompDishwasher>();
        if (dishwasher is null || !dishwasher.CanAcceptProcessorWare(HasContents(parent)))
        {
            __result = null;
            return;
        }

        __result = __0.Map.listerThings.AllThings
            .Where(IsDirtyWare)
            .Where(thing => ProcessorHasSpaceFor(__1, thing.def))
            .Where(thing => !thing.IsForbidden(__0) &&
                            __0.CanReserveAndReach(thing, Verse.AI.PathEndMode.Touch, Danger.Some))
            .OrderBy(thing => thing.Position.DistanceToSquared(parent.Position))
            .FirstOrDefault();
    }

    internal static bool HasContents(Thing thing)
    {
        var processor = ProcessorOf(thing);
        return processor is not null && ProcessorInputCount(processor) > 0;
    }

    internal static float UsedPlateEquivalentCapacity(Thing thing)
    {
        var processor = ProcessorOf(thing);
        if (processor is null)
        {
            return 0f;
        }

        return ActiveProcesses(processor)
            .Cast<object>()
            .SelectMany(ProcessIngredients)
            .Sum(ware => PlateEquivalentsPerItem(ware) * ware.stackCount);
    }

    internal static float ProgressPercent(Thing thing)
    {
        var processor = ProcessorOf(thing);
        var progress = processor is null
            ? new List<float>()
            : ActiveProcesses(processor)
                .Cast<object>()
                .Select(process => Convert.ToSingle(activeProcessPercent!.GetValue(process)))
                .ToList();
        return progress.Count == 0 ? 0f : 100f * progress.Min();
    }

    internal static bool EjectAllDirty(Thing thing)
    {
        var processor = ProcessorOf(thing);
        if (processor is null || thing.Map is not { } map)
        {
            return false;
        }

        var originals = ActiveProcesses(processor)
            .Cast<object>()
            .SelectMany(ProcessIngredients)
            .Distinct()
            .ToList();
        if (originals.Count == 0)
        {
            return false;
        }

        var owner = processorInnerContainer!.GetValue(processor) as ThingOwner;
        foreach (var original in originals)
        {
            owner?.Remove(original);
            GenPlace.TryPlaceThing(original, thing.InteractionCell, map, ThingPlaceMode.Near);
        }

        ActiveProcesses(processor).Clear();
        FinalizeEmptyProcessor(processor, (ThingWithComps)thing);
        return true;
    }

    private static object? ProcessorOf(Thing thing)
    {
        return thing is ThingWithComps withComps
            ? withComps.AllComps.FirstOrDefault(comp => processorType?.IsInstanceOfType(comp) == true)
            : null;
    }

    private static IList ActiveProcesses(object processor)
    {
        return processorActiveProcesses!.GetValue(processor) as IList
               ?? throw new InvalidOperationException("Processor active-process collection is unavailable.");
    }

    private static IEnumerable<Thing> ProcessIngredients(object process)
    {
        return (activeProcessIngredients!.GetValue(process) as IEnumerable)?
                   .Cast<object>()
                   .OfType<Thing>()
               ?? Enumerable.Empty<Thing>();
    }

    private static int ProcessorInputCount(object processor)
    {
        return ActiveProcesses(processor)
            .Cast<object>()
            .SelectMany(ProcessIngredients)
            .Sum(thing => thing.stackCount);
    }

    private static bool ProcessorHasSpaceFor(object processor, ThingDef ingredient)
    {
        if (processor is not ThingComp comp)
        {
            return false;
        }

        var enabled = processorEnabledProcesses!.GetValue(processor) as IDictionary;
        var process = enabled?.Keys.Cast<object>().FirstOrDefault(candidate =>
            (processIngredientFilter!.GetValue(candidate) as ThingFilter)?.Allows(ingredient) == true);
        return process is not null &&
               Convert.ToInt32(spaceLeftFor!.Invoke(processor, new[] { process, 1f })) > 0;
    }

    private static void FinalizeEmptyProcessor(object processor, ThingWithComps parent)
    {
        processorEmptyNow!.SetValue(processor, false);
        graphicChange!.Invoke(processor, new object[] { true });
        parent.GetComp<CompDishwasher>()?.NotifyProcessorEmptied();
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

    private static float PlateEquivalentsPerItem(Thing ware) =>
        DishwasherCapacityPolicy.ProcessorCapacityFactor(
            ware.def.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f);

    private static void RecacheFramework()
    {
        recacheAll!.Invoke(null, null);
    }

    private static void SetField(object target, string fieldName, object? value)
    {
        var field = AccessTools.Field(target.GetType(), fieldName)
                    ?? throw new MissingFieldException(target.GetType().FullName, fieldName);
        field.SetValue(target, value);
    }
}
