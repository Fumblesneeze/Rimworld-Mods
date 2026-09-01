using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal static class ProcessorEmptyLifecyclePolicy
{
    internal static bool ShouldFail(bool anyComplete, bool anyRuined, bool empty) =>
        (!anyComplete && !anyRuined) || empty;

    internal static bool ShouldSucceed(bool empty) => empty;
}

internal static class ProcessorDishwasherTransferPolicy
{
    internal const int ArrivalLatchTicks = DishwashingBatchPolicy.DishwasherAdmissionTicksPerUnit;

    internal static bool ShouldReplaceFillDriver(bool processorDishwasher) => processorDishwasher;

    internal static bool ShouldReplaceEmptyDriver(bool processorDishwasher) => processorDishwasher;

    internal static bool ShouldUseTrackedOutputBatch(
        bool processorDishwasher,
        bool canTrack,
        bool hasFittingNaturalOutput) =>
        processorDishwasher && canTrack && hasFittingNaturalOutput;

    internal static bool ShouldUseImmediateOneOutputFallback(
        bool canTrack,
        bool hasFittingNaturalOutput) =>
        !canTrack || !hasFittingNaturalOutput;
}

internal static class ProcessorDishwasherInputCompatibility
{
    internal const string AssemblyName = "ProcessorFramework";
    internal static readonly Version AssemblyVersion = new(1, 0, 0, 0);
    internal const string FillDriverTypeName = "ProcessorFramework.JobDriver_FillProcessor";

    internal static bool IsSupported(
        string? assemblyName,
        Version? assemblyVersion,
        string? driverTypeName,
        bool driverIsPublicJobDriver,
        bool makeNewToilsIsProtectedInstanceEnumerable,
        bool fillJobUsesDriver,
        bool reservationsArePublicInstanceBoolean,
        bool processFilterHasAllowedIngredientList)
    {
        return assemblyName == AssemblyName &&
               assemblyVersion == AssemblyVersion &&
               driverTypeName == FillDriverTypeName &&
               driverIsPublicJobDriver &&
               makeNewToilsIsProtectedInstanceEnumerable &&
               fillJobUsesDriver &&
               reservationsArePublicInstanceBoolean &&
               processFilterHasAllowedIngredientList;
    }
}

internal static class ProcessorDishwasherOutputCompatibility
{
    internal const string AssemblyName = "ProcessorFramework";
    internal static readonly Version AssemblyVersion = new(1, 0, 0, 0);
    internal const string EmptyDriverTypeName = "ProcessorFramework.JobDriver_EmptyProcessor";

    internal static bool IsSupported(
        string? assemblyName,
        Version? assemblyVersion,
        string? driverTypeName,
        bool driverIsPublicJobDriver,
        bool makeNewToilsIsProtectedInstanceEnumerable,
        bool emptyJobUsesDriver,
        bool ruinedIsPublicInstanceBoolean,
        bool progressIsPublicInstanceFloat,
        bool lifecyclePropertiesArePublicInstanceBoolean)
    {
        return assemblyName == AssemblyName &&
               assemblyVersion == AssemblyVersion &&
               driverTypeName == EmptyDriverTypeName &&
               driverIsPublicJobDriver &&
               makeNewToilsIsProtectedInstanceEnumerable &&
               emptyJobUsesDriver &&
               ruinedIsPublicInstanceBoolean &&
               progressIsPublicInstanceFloat &&
               lifecyclePropertiesArePublicInstanceBoolean;
    }
}

internal static class ProcessorFrameworkAdapter
{
    private const string ProcessorTypeName = "ProcessorFramework.CompProcessor";
    private const string ProcessorPropertiesTypeName = "ProcessorFramework.CompProperties_Processor";
    private const string ProcessDefTypeName = "ProcessorFramework.ProcessDef";
    private const string ProcessFilterTypeName = "ProcessorFramework.ProcessFilter";
    private const string ActiveProcessTypeName = "ProcessorFramework.ActiveProcess";
    private const string FillProcessorJobDriverTypeName =
        ProcessorDishwasherInputCompatibility.FillDriverTypeName;

    private static Type? processorType;
    private static Type? processorPropertiesType;
    private static Type? processDefType;
    private static Type? processFilterType;
    private static Type? activeProcessType;
    private static FieldInfo? processorInnerContainer;
    private static FieldInfo? processorActiveProcesses;
    private static FieldInfo? processorEnabledProcesses;
    private static FieldInfo? activeProcessIngredients;
    private static FieldInfo? activeProcessIngredientCount;
    private static FieldInfo? activeProcessProcessor;
    private static FieldInfo? processIngredientFilter;
    private static FieldInfo? processCapacityFactor;
    private static FieldInfo? processFilterAllowedIngredients;
    private static FieldInfo? processorEmptyNow;
    private static PropertyInfo? activeProcessComplete;
    private static PropertyInfo? activeProcessRuined;
    private static PropertyInfo? activeProcessPercent;
    private static PropertyInfo? processorAnyComplete;
    private static PropertyInfo? processorAnyRuined;
    private static PropertyInfo? processorEmpty;
    private static MethodInfo? spaceLeftFor;
    private static MethodInfo? graphicChange;
    private static MethodInfo? enableAllProcesses;
    private static MethodInfo? addIngredient;
    private static MethodInfo? takeOutProduct;
    private static MethodInfo? findIngredient;
    private static MethodInfo? fillProcessorTryMakeReservations;
    private static MethodInfo? fillProcessorMakeNewToils;
    private static MethodInfo? emptyProcessorMakeNewToils;
    private static MethodInfo? resolveProcessReferences;
    private static MethodInfo? addProcessDef;
    private static MethodInfo? recacheAll;

    private sealed class ProcessorAdmissionState
    {
        internal int BeforeCount;
        internal float PlateEquivalentsPerItem;
        internal List<object> ExistingProcesses = new();
        internal Thing Ware = null!;
        internal ThingOwner? PreviousOwner;
        internal Map? PreviousMap;
        internal IntVec3 PreviousPosition;
    }

    private sealed class CompletedOutputCandidate
    {
        internal string Id = string.Empty;
        internal object Process = null!;
        internal Thing Ware = null!;
    }

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
        processFilterType = AccessTools.TypeByName(ProcessFilterTypeName);
        activeProcessType = AccessTools.TypeByName(ActiveProcessTypeName);
        if (processorType is null || processorPropertiesType is null || processDefType is null ||
            processFilterType is null ||
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
        activeProcessIngredientCount = AccessTools.Field(activeProcessType, "ingredientCount");
        activeProcessProcessor = AccessTools.Field(activeProcessType, "processor");
        processIngredientFilter = AccessTools.Field(processDefType, "ingredientFilter");
        processCapacityFactor = AccessTools.Field(processDefType, "capacityFactor");
        processFilterAllowedIngredients = AccessTools.Field(processFilterType, "allowedIngredients");
        processorEmptyNow = AccessTools.Field(processorType, "emptyNow");
        activeProcessComplete = AccessTools.Property(activeProcessType, "Complete");
        activeProcessRuined = AccessTools.Property(activeProcessType, "Ruined");
        activeProcessPercent = AccessTools.Property(activeProcessType, "ActiveProcessPercent");
        processorAnyComplete = AccessTools.Property(processorType, "AnyComplete");
        processorAnyRuined = AccessTools.Property(processorType, "AnyRuined");
        processorEmpty = AccessTools.Property(processorType, "Empty");
        spaceLeftFor = AccessTools.Method(processorType, "SpaceLeftFor");
        graphicChange = AccessTools.Method(processorType, "GraphicChange");
        enableAllProcesses = AccessTools.Method(processorType, "EnableAllProcesses");
        addIngredient = processorType.GetMethod(
            "AddIngredient",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(Thing), processDefType },
            modifiers: null);
        takeOutProduct = processorType.GetMethod(
            "TakeOutProduct",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { activeProcessType },
            modifiers: null);
        var workGiverType = AccessTools.TypeByName("ProcessorFramework.WorkGiver_FillProcessor");
        findIngredient = workGiverType is null ? null : AccessTools.Method(workGiverType, "FindIngredient");
        var fillProcessorJobDriverType = AccessTools.TypeByName(FillProcessorJobDriverTypeName);
        fillProcessorTryMakeReservations = fillProcessorJobDriverType?.GetMethod(
            nameof(JobDriver.TryMakePreToilReservations),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(bool) },
            modifiers: null);
        fillProcessorMakeNewToils = fillProcessorJobDriverType?.GetMethod(
            "MakeNewToils",
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        var fillProcessorJob = DefDatabase<JobDef>.GetNamedSilentFail("FillProcessor");
        var emptyProcessorJobDriverType = AccessTools.TypeByName(
            ProcessorDishwasherOutputCompatibility.EmptyDriverTypeName);
        emptyProcessorMakeNewToils = emptyProcessorJobDriverType?.GetMethod(
            "MakeNewToils",
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        var emptyProcessorJob = DefDatabase<JobDef>.GetNamedSilentFail("EmptyProcessor");
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
        var requiredProcessFields = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["defName"] = typeof(string),
            ["label"] = typeof(string),
            ["thingDef"] = typeof(ThingDef),
            ["processDays"] = typeof(float),
            ["capacityFactor"] = typeof(float),
            ["efficiency"] = typeof(float),
            ["usesTemperature"] = typeof(bool),
            ["unpoweredFactor"] = typeof(float),
            ["unfueledFactor"] = typeof(float),
            ["destroyChance"] = typeof(float),
            ["ingredientFilter"] = typeof(ThingFilter)
        };
        var requiredProcessorPropertyFields = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["capacity"] = typeof(int),
            ["independentProcesses"] = typeof(bool),
            ["parallelProcesses"] = typeof(bool),
            ["dropIngredients"] = typeof(bool),
            ["showProductIcon"] = typeof(bool),
            ["colorCoded"] = typeof(bool),
            ["processes"] = typeof(List<>).MakeGenericType(processDefType)
        };
        if (processorInnerContainer is null || processorActiveProcesses is null ||
            processorEnabledProcesses is null ||
            activeProcessIngredients is null || activeProcessIngredientCount is not { FieldType: { } ingredientCountType } ||
            ingredientCountType != typeof(int) || activeProcessProcessor is null ||
            processIngredientFilter is null || processCapacityFactor is null ||
            processFilterAllowedIngredients is not { FieldType: { } allowedIngredientsType } ||
            allowedIngredientsType != typeof(List<ThingDef>) ||
            processFilterAllowedIngredients.DeclaringType != processFilterType ||
            processorEmptyNow is null ||
            activeProcessComplete is null || activeProcessRuined is null || activeProcessPercent is null ||
            spaceLeftFor is null || graphicChange is null || enableAllProcesses is null ||
            addIngredient is not { ReturnType: { } addIngredientReturn } ||
            addIngredientReturn != typeof(void) || addIngredient.DeclaringType != processorType ||
            takeOutProduct is not { ReturnType: { } takeOutReturn } ||
            takeOutReturn != typeof(Thing) || takeOutProduct.DeclaringType != processorType ||
            findIngredient is null || fillProcessorJobDriverType is null ||
            !typeof(JobDriver).IsAssignableFrom(fillProcessorJobDriverType) ||
            resolveProcessReferences is null || addProcessDef is null ||
            recacheAll is null || requiredMethods.Any(method => method is null) ||
            requiredProcessFields.Any(field =>
                !HasExactFieldShape(processDefType, field.Key, field.Value)) ||
            requiredProcessorPropertyFields.Any(field =>
                !HasExactFieldShape(processorPropertiesType, field.Key, field.Value)))
        {
            reason = "the installed Processor Framework process lifecycle no longer matches the validated 1.6 shape";
            return false;
        }

        var processorAssembly = processorType.Assembly;
        var inputShapeSupported = ProcessorDishwasherInputCompatibility.IsSupported(
            processorAssembly.GetName().Name,
            processorAssembly.GetName().Version,
            fillProcessorJobDriverType.FullName,
            fillProcessorJobDriverType.IsPublic &&
            typeof(JobDriver).IsAssignableFrom(fillProcessorJobDriverType),
            fillProcessorMakeNewToils is { IsStatic: false, IsFamily: true } &&
            fillProcessorMakeNewToils.DeclaringType == fillProcessorJobDriverType &&
            fillProcessorMakeNewToils.ReturnType == typeof(IEnumerable<Toil>),
            fillProcessorJob?.driverClass == fillProcessorJobDriverType,
            fillProcessorTryMakeReservations is { IsPublic: true, IsStatic: false } &&
            fillProcessorTryMakeReservations.ReturnType == typeof(bool) &&
            fillProcessorTryMakeReservations.DeclaringType == fillProcessorJobDriverType,
            processFilterAllowedIngredients is { IsPublic: true, IsStatic: false });
        var outputShapeSupported = ProcessorDishwasherOutputCompatibility.IsSupported(
            processorAssembly.GetName().Name,
            processorAssembly.GetName().Version,
            emptyProcessorJobDriverType?.FullName,
            emptyProcessorJobDriverType is { IsPublic: true } &&
            typeof(JobDriver).IsAssignableFrom(emptyProcessorJobDriverType),
            emptyProcessorMakeNewToils is { IsStatic: false, IsFamily: true } &&
            emptyProcessorMakeNewToils.DeclaringType == emptyProcessorJobDriverType &&
            emptyProcessorMakeNewToils.ReturnType == typeof(IEnumerable<Toil>),
            emptyProcessorJob?.driverClass == emptyProcessorJobDriverType,
            activeProcessRuined.GetMethod is { IsPublic: true, IsStatic: false } &&
            activeProcessRuined.PropertyType == typeof(bool) &&
            activeProcessRuined.DeclaringType == activeProcessType,
            activeProcessPercent.GetMethod is { IsPublic: true, IsStatic: false } &&
            activeProcessPercent.PropertyType == typeof(float) &&
            activeProcessPercent.DeclaringType == activeProcessType,
            IsPublicInstanceBoolean(processorAnyComplete, processorType) &&
            IsPublicInstanceBoolean(processorAnyRuined, processorType) &&
            IsPublicInstanceBoolean(processorEmpty, processorType));
        if (!inputShapeSupported || !outputShapeSupported ||
            fillProcessorJobDriverType.Assembly != processorAssembly ||
            emptyProcessorJobDriverType?.Assembly != processorAssembly)
        {
            reason = "the installed Processor Framework emptying lifecycle no longer matches the validated 1.6 shape";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsPublicInstanceBoolean(PropertyInfo? property, Type declaringType)
    {
        return property?.GetMethod is { IsPublic: true, IsStatic: false } &&
               property.PropertyType == typeof(bool) &&
               property.DeclaringType == declaringType;
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
        SetField(process, "label", "ImmersiveChefs_Processor_Wash".Translate(ware.label));
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
            spaceLeftFor!,
            postfix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(SpaceLeftForPostfix)));
        harmony.Patch(
            takeOutProduct!,
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
        harmony.Patch(
            fillProcessorTryMakeReservations!,
            prefix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(FillProcessorReservationsPrefix)));
        harmony.Patch(
            fillProcessorMakeNewToils!,
            prefix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(FillProcessorMakeNewToilsPrefix)));
        harmony.Patch(
            emptyProcessorMakeNewToils!,
            prefix: new HarmonyMethod(typeof(ProcessorFrameworkAdapter), nameof(EmptyProcessorMakeNewToilsPrefix)));
    }

    internal static bool AllowsFillReservation(bool isDishwasher, bool isDirtyWare)
    {
        return !isDishwasher || isDirtyWare;
    }

    private static bool FillProcessorReservationsPrefix(JobDriver __instance, ref bool __result)
    {
        var job = __instance.job;
        var processor = job?.GetTarget(TargetIndex.A).Thing;
        if (job is null || processor is null)
        {
            return true;
        }

        var ingredient = job.GetTarget(TargetIndex.B).Thing;
        if (AllowsFillReservation(IsDishwasher(processor), ingredient is not null && IsDirtyWare(ingredient)))
        {
            return true;
        }

        __result = false;
        return false;
    }

    private static bool FillProcessorMakeNewToilsPrefix(
        JobDriver __instance,
        ref IEnumerable<Toil> __result)
    {
        var processor = __instance.job?.GetTarget(TargetIndex.A).Thing;
        if (!ProcessorDishwasherTransferPolicy.ShouldReplaceFillDriver(
                processor is not null && Controls(processor)))
        {
            return true;
        }

        __result = MakeImmediateFillProcessorToils(__instance, processor!);
        return false;
    }

    private static IEnumerable<Toil> MakeImmediateFillProcessorToils(
        JobDriver driver,
        Thing dishwasher)
    {
        var processor = ProcessorOf(dishwasher)!;
        var ingredient = driver.job.GetTarget(TargetIndex.B).Thing;
        var process = ingredient is null
            ? null
            : FindEnabledProcess(processor, ingredient.def);
        if (process is null)
        {
            Log.Error($"[ImmersiveChefs] Processor Framework exposed no enabled dishwasher process for {ingredient?.Label ?? "missing ingredient"}.");
        }

        driver.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        driver.FailOnBurningImmobile(TargetIndex.A);
        driver.AddEndCondition(() =>
            process is not null &&
            driver.job.GetTarget(TargetIndex.B).Thing is { } currentIngredient &&
            ProcessHasSpaceAndRemainsEnabled(processor, process, currentIngredient.def)
                ? JobCondition.Ongoing
                : JobCondition.Incompletable);
        var reserveIngredient = Toils_Reserve.Reserve(
            TargetIndex.B,
            maxPawns: 1,
            stackCount: driver.job.count);
        yield return reserveIngredient;
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B)
            .FailOnSomeonePhysicallyInteracting(TargetIndex.B);
        yield return Toils_Haul.StartCarryThing(
                TargetIndex.B,
                putRemainderInQueue: false,
                subtractNumTakenFromJobCount: true,
                failIfStackCountLessThanJobCount: false,
                reserve: true,
                canTakeFromInventory: false)
            .FailOnDestroyedNullOrForbidden(TargetIndex.B);
        yield return Toils_Haul.CheckForGetOpportunityDuplicate(
            reserveIngredient,
            TargetIndex.B,
            TargetIndex.None);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
        yield return Toils_General.Wait(
                ProcessorDishwasherTransferPolicy.ArrivalLatchTicks,
                TargetIndex.A)
            .FailOnDestroyedNullOrForbidden(TargetIndex.B)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
            .WithProgressBarToilDelay(TargetIndex.A);
        yield return Toils_General.DoAtomic(() =>
        {
            var currentIngredient = driver.job.GetTarget(TargetIndex.B).Thing;
            if (process is not null && currentIngredient is not null)
            {
                addIngredient!.Invoke(processor, new[] { currentIngredient, process });
                return;
            }

            driver.EndJobWith(JobCondition.Incompletable);
        });
    }

    private static bool EmptyProcessorMakeNewToilsPrefix(
        JobDriver __instance,
        ref IEnumerable<Toil> __result)
    {
        var processor = __instance.job?.GetTarget(TargetIndex.A).Thing;
        var processorDishwasher = processor is not null && Controls(processor);
        if (!ProcessorDishwasherTransferPolicy.ShouldReplaceEmptyDriver(processorDishwasher))
        {
            return true;
        }

        __result = MakeImmediateEmptyProcessorToils(__instance, processor!);
        return false;
    }

    private static IEnumerable<Toil> MakeImmediateEmptyProcessorToils(
        JobDriver driver,
        Thing dishwasher)
    {
        var processor = ProcessorOf(dishwasher)!;
        driver.FailOn(() => ProcessorEmptyLifecyclePolicy.ShouldFail(
            processorAnyComplete!.GetValue(processor) is true,
            processorAnyRuined!.GetValue(processor) is true,
            processorEmpty!.GetValue(processor) is true));
        driver.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        driver.AddEndCondition(() => ProcessorEmptyLifecyclePolicy.ShouldSucceed(
                processorEmpty!.GetValue(processor) is true)
            ? JobCondition.Succeeded
            : JobCondition.Ongoing);
        var terminal = Toils_General.DoAtomic(() => { });
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
        yield return Toils_General.Wait(
                ProcessorDishwasherTransferPolicy.ArrivalLatchTicks,
                TargetIndex.None)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .WithProgressBarToilDelay(TargetIndex.A);
        yield return new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Instant,
            initAction = () =>
            {
                var canTrack = PickUpAndHaulAdapter.CanTrack(driver.pawn);
                var hasFittingNaturalOutput =
                    HasFittingNaturalCompletedOutput(driver.pawn, dishwasher);
                if (ProcessorDishwasherTransferPolicy.ShouldUseImmediateOneOutputFallback(
                        canTrack,
                        hasFittingNaturalOutput))
                {
                    return;
                }

                if (!ProcessorDishwasherTransferPolicy.ShouldUseTrackedOutputBatch(
                        processorDishwasher: true,
                        canTrack,
                        hasFittingNaturalOutput))
                {
                    return;
                }

                var extracted = TryTakeCompletedOutputsToInventory(
                    driver.pawn,
                    dishwasher,
                    out var reason);
                if (extracted <= 0)
                {
                    return;
                }

                if (!PickUpAndHaulAdapter.TryQueueUnload(driver.pawn, out reason))
                {
                    OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.PickUpAndHaul, reason);
                }

                driver.JumpToToil(terminal);
            }
        };
        yield return MakeStockEmptyProcessorExtractionToil(driver, dishwasher);
        yield return Toils_Reserve.Reserve(TargetIndex.B);
        yield return Toils_Reserve.Reserve(TargetIndex.C);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.B);
        var carry = Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
        yield return carry;
        yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, carry, storageMode: true);
        yield return terminal;
    }

    private static Toil MakeStockEmptyProcessorExtractionToil(
        JobDriver driver,
        Thing dishwasher)
    {
        return new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Instant,
            initAction = () =>
            {
                var processor = ProcessorOf(dishwasher);
                var process = processor is null
                    ? null
                    : ActiveProcesses(processor)
                        .Cast<object>()
                        .FirstOrDefault(candidate =>
                            activeProcessComplete!.GetValue(candidate) is true ||
                            activeProcessRuined!.GetValue(candidate) is true);
                var output = process is null
                    ? null
                    : takeOutProduct!.Invoke(processor, new[] { process }) as Thing;
                var map = driver.pawn.Map;
                if (process is null)
                {
                    driver.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (output is null || output.stackCount <= 0 || map is null)
                {
                    driver.EndJobWith(JobCondition.Succeeded);
                    return;
                }

                if (output.def.race is not null)
                {
                    for (var index = 0; index < output.stackCount; index++)
                    {
                        GenSpawn.Spawn(
                            PawnGenerator.GeneratePawn(
                                output.def.race.AnyPawnKind,
                                Faction.OfPlayerSilentFail),
                            driver.pawn.Position,
                            map);
                    }

                    driver.EndJobWith(JobCondition.Succeeded);
                    return;
                }

                GenPlace.TryPlaceThing(
                    output,
                    driver.pawn.Position,
                    map,
                    ThingPlaceMode.Near);

                var currentPriority = StoreUtility.CurrentStoragePriorityOf(output);
                if (!StoreUtility.TryFindBestBetterStoreCellFor(
                        output,
                        driver.pawn,
                        map,
                        currentPriority,
                        driver.pawn.Faction,
                        out var storeCell,
                        needAccurateResult: true))
                {
                    driver.EndJobWith(JobCondition.Succeeded);
                    return;
                }

                driver.job.SetTarget(TargetIndex.B, output);
                driver.job.count = output.stackCount;
                driver.job.SetTarget(TargetIndex.C, storeCell);
            }
        };
    }

    private static bool HasFittingNaturalCompletedOutput(Pawn pawn, Thing dishwasherThing)
    {
        var processor = ProcessorOf(dishwasherThing);
        if (processor is null)
        {
            return false;
        }

        var availableMass = RemainingInventoryMassCapacity(pawn);
        return ActiveProcesses(processor)
            .Cast<object>()
            .Where(process => NaturalProgress(process) >= 1f &&
                              activeProcessRuined!.GetValue(process) is not true)
            .SelectMany(ProcessIngredients)
            .Any(ware => ware.stackCount > 0 &&
                         availableMass + 0.0001f >= UnitMass(ware));
    }

    private static bool AddIngredientPrefix(
        object __instance,
        Thing __0,
        object __1,
        ref ProcessorAdmissionState? __state)
    {
        __state = null;
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
        var capacityCount = ProcessorAdmissionCapacityCount(__instance, __1, __0);
        if (dishwasher is null || capacityCount <= 0)
        {
            return false;
        }

        __state = new ProcessorAdmissionState
        {
            BeforeCount = before,
            PlateEquivalentsPerItem = PlateEquivalentsPerItem(__0),
            ExistingProcesses = ActiveProcesses(__instance).Cast<object>().ToList(),
            Ware = __0,
            PreviousOwner = __0.holdingOwner,
            PreviousMap = __0.MapHeld,
            PreviousPosition = __0.PositionHeld
        };
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

    private static void AddIngredientPostfix(object __instance, ProcessorAdmissionState? __state)
    {
        if (__state is null)
        {
            return;
        }

        var parent = ParentOfProcessor(__instance);
        var after = ProcessorInputCount(__instance);
        if (parent is null || after <= __state.BeforeCount)
        {
            return;
        }

        var dishwasher = parent.GetComp<CompDishwasher>();
        var admittedCount = after - __state.BeforeCount;
        var reason = "dishwasher component unavailable";
        if (dishwasher is not null && dishwasher.TryCommitProcessorAdmission(
                __state.PlateEquivalentsPerItem * admittedCount,
                out reason))
        {
            return;
        }

        RollBackProcessorAdmission(__instance, parent, __state);
        OptionalIntegrationDiagnostics.WarnOnce(
            OptionalIntegration.ProcessorFramework,
            $"Dishwasher admission was rolled back because its utility debit could not commit ({reason}).");
    }

    private static void SpaceLeftForPostfix(object __instance, object __0, ref int __result)
    {
        if (__result <= 0)
        {
            return;
        }

        var parent = ParentOfProcessor(__instance);
        if (parent is null || !IsDishwasher(parent))
        {
            return;
        }

        var dishwasher = parent.GetComp<CompDishwasher>();
        __result = dishwasher is null
            ? 0
            : dishwasher.CountCanAcceptProcessorWare(
                Convert.ToSingle(processCapacityFactor!.GetValue(__0)),
                __result);
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
        if (dishwasher is null)
        {
            __result = null;
            return;
        }

        __result = __0.Map.listerThings.AllThings
            .Where(IsDirtyWare)
            .Where(thing => CanAdmitProcessorStack(dishwasher, __1, thing))
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

    internal static float AvailablePlateEquivalentCapacity(Thing thing)
    {
        var dishwasher = (thing as ThingWithComps)?.GetComp<CompDishwasher>();
        return dishwasher is null
            ? 0f
            : Math.Max(0f, dishwasher.Capacity - UsedPlateEquivalentCapacity(thing));
    }

    internal static bool CanAcceptTrackedWare(Thing dishwasherThing, Thing ware)
    {
        var processor = ProcessorOf(dishwasherThing);
        var dishwasher = (dishwasherThing as ThingWithComps)?.GetComp<CompDishwasher>();
        return processor is not null && dishwasher is not null && IsDirtyWare(ware) &&
               dishwasher.CanAcceptProcessorWare(ware) &&
               ProcessorHasSpaceFor(processor, ware.def) &&
               AvailablePlateEquivalentCapacity(dishwasherThing) + 0.0001f >=
               PlateEquivalentsPerItem(ware);
    }

    internal static bool TryAcceptTrackedWare(Pawn pawn, Thing dishwasherThing, Thing ware, out string reason)
    {
        var processor = ProcessorOf(dishwasherThing);
        var inventory = pawn.inventory?.innerContainer;
        if (processor is null || inventory is null ||
            !ReferenceEquals(ware.holdingOwner, inventory) ||
            !CanAcceptTrackedWare(dishwasherThing, ware) || addIngredient is null)
        {
            reason = "the tracked kitchenware can no longer enter the selected Processor dishwasher";
            return false;
        }

        var enabled = processorEnabledProcesses!.GetValue(processor) as IDictionary;
        var process = enabled?.Keys.Cast<object>().FirstOrDefault(candidate =>
            (processIngredientFilter!.GetValue(candidate) as ThingFilter)?.Allows(ware.def) == true);
        if (process is null)
        {
            reason = "Processor Framework no longer exposes an enabled process for the tracked kitchenware";
            return false;
        }

        if (!PickUpAndHaulAdapter.TryResolveTrackedItems(pawn, ware, out var tracked, out reason) ||
            !PickUpAndHaulAdapter.TryRemoveResolvedTrackedItem(tracked!, ware, out reason))
        {
            return false;
        }

        try
        {
            addIngredient.Invoke(processor, new[] { ware, process });
            if (ReferenceEquals(ware.holdingOwner, inventory) ||
                !IsOwnedByProcessor(processor, ware) ||
                !HeldWare(dishwasherThing).Any(item => ReferenceEquals(item, ware)))
            {
                RemoveDanglingProcessorReference(processor, ware);
                RestoreTrackingOrDrop(pawn, ware, inventory, out _);
                reason = "Processor Framework declined the tracked kitchenware admission";
                return false;
            }

            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            var root = exception is TargetInvocationException { InnerException: { } inner }
                ? inner
                : exception;
            if (IsOwnedByProcessor(processor, ware) &&
                HeldWare(dishwasherThing).Any(item => ReferenceEquals(item, ware)))
            {
                reason = string.Empty;
                return true;
            }

            RemoveDanglingProcessorReference(processor, ware);
            RestoreTrackingOrDrop(pawn, ware, inventory, out _);
            reason = $"Processor tracked admission failed ({root.GetType().Name}: {root.Message})";
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.ProcessorFramework, reason);
            return false;
        }
    }

    internal static bool TryAcceptCarriedWare(Pawn pawn, Thing dishwasherThing, out string reason)
    {
        var processor = ProcessorOf(dishwasherThing);
        var carry = pawn.carryTracker?.innerContainer;
        var ware = pawn.carryTracker?.CarriedThing;
        if (processor is null || carry is null || ware is null ||
            !ReferenceEquals(ware.holdingOwner, carry) ||
            !CanAcceptTrackedWare(dishwasherThing, ware) || addIngredient is null)
        {
            reason = "the carried kitchenware can no longer enter the selected Processor dishwasher";
            return false;
        }

        var enabled = processorEnabledProcesses!.GetValue(processor) as IDictionary;
        var process = enabled?.Keys.Cast<object>().FirstOrDefault(candidate =>
            (processIngredientFilter!.GetValue(candidate) as ThingFilter)?.Allows(ware.def) == true);
        if (process is null)
        {
            reason = "Processor Framework no longer exposes an enabled process for the carried kitchenware";
            return false;
        }

        try
        {
            addIngredient.Invoke(processor, new[] { ware, process });
            if (!ReferenceEquals(ware.holdingOwner, carry) &&
                IsOwnedByProcessor(processor, ware) &&
                HeldWare(dishwasherThing).Any(item => ReferenceEquals(item, ware)))
            {
                reason = string.Empty;
                return true;
            }

            RemoveDanglingProcessorReference(processor, ware);
            RestoreCarryOrDrop(pawn, processor, ware, carry);
            reason = "Processor Framework declined the carried kitchenware admission";
            return false;
        }
        catch (Exception exception)
        {
            var root = exception is TargetInvocationException { InnerException: { } inner }
                ? inner
                : exception;
            if (IsOwnedByProcessor(processor, ware) &&
                HeldWare(dishwasherThing).Any(item => ReferenceEquals(item, ware)))
            {
                reason = string.Empty;
                return true;
            }

            RemoveDanglingProcessorReference(processor, ware);
            RestoreCarryOrDrop(pawn, processor, ware, carry);
            reason = $"Processor carried admission failed ({root.GetType().Name}: {root.Message})";
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.ProcessorFramework, reason);
            return false;
        }
    }

    private static bool IsOwnedByProcessor(object processor, Thing ware)
    {
        return processorInnerContainer!.GetValue(processor) is ThingOwner owner &&
               ReferenceEquals(ware.holdingOwner, owner);
    }

    private static bool RestoreCarryOrDrop(
        Pawn pawn,
        object processor,
        Thing ware,
        ThingOwner carry)
    {
        if (ReferenceEquals(ware.holdingOwner, carry))
        {
            return true;
        }

        if (processorInnerContainer!.GetValue(processor) is ThingOwner processorOwner &&
            ReferenceEquals(ware.holdingOwner, processorOwner))
        {
            processorOwner.Remove(ware);
        }

        if (ware.holdingOwner is null && carry.TryAdd(ware, canMergeWithExistingStacks: false))
        {
            return true;
        }

        return ware.Spawned ||
               (ware.holdingOwner is null && pawn.MapHeld is { } map &&
                GenPlace.TryPlaceThing(ware, pawn.PositionHeld, map, ThingPlaceMode.Near));
    }

    private static void RemoveDanglingProcessorReference(object processor, Thing ware)
    {
        var processes = ActiveProcesses(processor);
        for (var index = processes.Count - 1; index >= 0; index--)
        {
            var active = processes[index]!;
            if (activeProcessIngredients!.GetValue(active) is not IList ingredients ||
                !ingredients.Contains(ware))
            {
                continue;
            }

            ingredients.Remove(ware);
            var remaining = Math.Max(0,
                Convert.ToInt32(activeProcessIngredientCount!.GetValue(active)) - ware.stackCount);
            activeProcessIngredientCount.SetValue(active, remaining);
            if (ingredients.Count == 0 || remaining == 0)
            {
                processes.RemoveAt(index);
            }
        }
    }

    private static bool RestoreTrackingOrDrop(
        Pawn pawn,
        Thing ware,
        ThingOwner inventory,
        out string reason)
    {
        if (ware.Spawned)
        {
            reason = string.Empty;
            return true;
        }

        if (ReferenceEquals(ware.holdingOwner, inventory) &&
            PickUpAndHaulAdapter.TryRegister(pawn, ware, out reason))
        {
            return true;
        }

        if (ReferenceEquals(ware.holdingOwner, inventory) && pawn.MapHeld is { } map &&
            inventory.TryDrop(ware, pawn.PositionHeld, map, ThingPlaceMode.Near, out var dropped) &&
            dropped is { Spawned: true })
        {
            reason = "Pick Up And Haul could not retake ownership; the unadmitted unit was returned to the map";
            return true;
        }

        reason = "the unadmitted kitchenware could neither be retracked nor returned to the map";
        return false;
    }

    internal static IReadOnlyList<Thing> HeldWare(Thing thing)
    {
        var processor = ProcessorOf(thing);
        return processor is null
            ? Array.Empty<Thing>()
            : ActiveProcesses(processor)
                .Cast<object>()
                .SelectMany(ProcessIngredients)
                .Distinct()
                .ToArray();
    }

    internal static int TryTakeCompletedOutputsToInventory(
        Pawn pawn,
        Thing dishwasherThing,
        out string reason)
    {
        reason = string.Empty;
        var processor = ProcessorOf(dishwasherThing);
        var inventory = pawn.inventory?.innerContainer;
        var owner = processor is null
            ? null
            : processorInnerContainer!.GetValue(processor) as ThingOwner;
        if (processor is null || inventory is null || owner is null ||
            !Controls(dishwasherThing) || !PickUpAndHaulAdapter.CanTrack(pawn))
        {
            reason = "the validated dishwasher output batch is no longer available";
            return 0;
        }

        var candidates = new List<CompletedOutputCandidate>();
        var descriptors = new List<DishwasherOutputBatchCandidate>();
        var processIndex = 0;
        foreach (var process in ActiveProcesses(processor).Cast<object>().ToList())
        {
            var progress = NaturalProgress(process);
            var ruined = activeProcessRuined!.GetValue(process) is true;
            foreach (var ware in ProcessIngredients(process).ToList())
            {
                var id = $"{processIndex}:{ware.ThingID}";
                candidates.Add(new CompletedOutputCandidate
                {
                    Id = id,
                    Process = process,
                    Ware = ware
                });
                descriptors.Add(new DishwasherOutputBatchCandidate(
                    id,
                    ware.stackCount,
                    UnitMass(ware),
                    progress,
                    ruined));
            }

            processIndex++;
        }

        var availableMass = RemainingInventoryMassCapacity(pawn);
        var selections = DishwasherOutputBatchPolicy.Select(descriptors, availableMass);
        if (selections.Count == 0)
        {
            reason = "the dishwasher has no completed output that fits the pawn's remaining capacity";
            return 0;
        }

        var byId = candidates.ToDictionary(candidate => candidate.Id, StringComparer.Ordinal);
        var processes = ActiveProcesses(processor);
        var extractedCount = 0;
        var processorChanged = false;
        foreach (var selection in selections)
        {
            if (!byId.TryGetValue(selection.Id, out var candidate) ||
                !processes.Contains(candidate.Process) ||
                NaturalProgress(candidate.Process) < 1f ||
                activeProcessRuined!.GetValue(candidate.Process) is true ||
                candidate.Ware.Destroyed ||
                !ReferenceEquals(candidate.Ware.holdingOwner, owner))
            {
                continue;
            }

            var count = Math.Min(selection.Count, candidate.Ware.stackCount);
            if (count <= 0)
            {
                continue;
            }

            var priorCount = candidate.Ware.stackCount;
            var transferred = owner.TryTransferToContainer(
                candidate.Ware,
                inventory,
                count,
                out var moved,
                canMergeWithExistingStacks: false);
            if (transferred <= 0 || moved is null)
            {
                continue;
            }

            var ingredients = activeProcessIngredients!.GetValue(candidate.Process) as IList;
            if (!PickUpAndHaulAdapter.TryRegister(pawn, moved, out reason))
            {
                var registrationFailure = reason;
                PickUpAndHaulAdapter.TryRemoveTracked(pawn, moved, out _);
                if (owner.TryAddOrTransfer(moved, canMergeWithExistingStacks: false))
                {
                    if (ingredients is not null && !ingredients.Contains(moved))
                    {
                        ingredients.Add(moved);
                    }

                    reason = registrationFailure;
                    break;
                }

                CommitCompletedOutputExtraction(
                    processes,
                    candidate.Process,
                    ingredients,
                    candidate.Ware,
                    priorCount,
                    transferred);
                (moved as ThingWithComps)?.GetComp<CompSanitation>()?
                    .MarkClean(WashProvenance.Safe);
                if (pawn.MapHeld is { } map)
                {
                    DiningWarePlacementRuntime.DropAt(moved, dishwasherThing.InteractionCell, map);
                }

                processorChanged = true;
                reason = $"{registrationFailure}; the extracted output could not be restored to the processor";
                OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.PickUpAndHaul, reason);
                break;
            }

            CommitCompletedOutputExtraction(
                processes,
                candidate.Process,
                ingredients,
                candidate.Ware,
                priorCount,
                transferred);
            (moved as ThingWithComps)?.GetComp<CompSanitation>()?
                .MarkClean(WashProvenance.Safe);
            extractedCount += transferred;
            processorChanged = true;
        }

        if (processorChanged)
        {
            graphicChange!.Invoke(processor, new object[] { true });
        }
        if (processes.Count == 0)
        {
            FinalizeEmptyProcessor(processor, (ThingWithComps)dishwasherThing);
        }

        return extractedCount;
    }

    private static void CommitCompletedOutputExtraction(
        IList processes,
        object process,
        IList? ingredients,
        Thing originalWare,
        int priorCount,
        int transferred)
    {
        var remainingIngredientCount = Math.Max(
            0,
            Convert.ToInt32(activeProcessIngredientCount!.GetValue(process)) - transferred);
        activeProcessIngredientCount.SetValue(process, remainingIngredientCount);
        if (transferred >= priorCount)
        {
            ingredients?.Remove(originalWare);
        }

        if (remainingIngredientCount == 0 || ingredients is null || ingredients.Count == 0)
        {
            processes.Remove(process);
        }
    }

    private static float NaturalProgress(object process) =>
        Convert.ToSingle(activeProcessPercent!.GetValue(process));

    private static float UnitMass(Thing thing) =>
        Math.Max(0.001f, thing.GetStatValue(RimWorld.StatDefOf.Mass));

    private static float RemainingInventoryMassCapacity(Pawn pawn) =>
        Math.Max(
            0f,
            RimWorld.MassUtility.Capacity(pawn) *
            (1f - RimWorld.MassUtility.EncumbrancePercent(pawn)));

    internal static float ProgressPercent(Thing thing)
    {
        var processor = ProcessorOf(thing);
        var progress = processor is null
            ? new List<float>()
            : ActiveProcesses(processor)
                .Cast<object>()
                .Select(process => Convert.ToSingle(activeProcessPercent!.GetValue(process)))
                .ToList();
        return progress.Count == 0 ? 0f : 100f * progress.Max();
    }

    internal static bool AllProcessesNaturallyComplete(Thing thing)
    {
        var processor = ProcessorOf(thing);
        if (processor is null)
        {
            return false;
        }

        var processes = ActiveProcesses(processor).Cast<object>().ToArray();
        return processes.Length > 0 && processes.All(process => NaturalProgress(process) >= 1f);
    }

    internal static float ProgressPercent(Thing thing, Thing exactWare)
    {
        var processor = ProcessorOf(thing);
        if (processor is null)
        {
            return 0f;
        }

        var process = ActiveProcesses(processor)
            .Cast<object>()
            .FirstOrDefault(candidate => ProcessIngredients(candidate)
                .Any(ware => ReferenceEquals(ware, exactWare)));
        return process is null
            ? 0f
            : 100f * Convert.ToSingle(activeProcessPercent!.GetValue(process));
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

    private static int ProcessorAdmissionCapacityCount(object processor, object process, Thing ware)
    {
        if (spaceLeftFor is null || ware.stackCount <= 0)
        {
            return 0;
        }

        try
        {
            return Math.Max(0, Math.Min(
                ware.stackCount,
                Convert.ToInt32(spaceLeftFor.Invoke(processor, new[] { process, (object)1f }))));
        }
        catch
        {
            return 0;
        }
    }

    private static bool CanAdmitProcessorStack(
        CompDishwasher dishwasher,
        object processor,
        Thing ware)
    {
        var enabled = processorEnabledProcesses!.GetValue(processor) as IDictionary;
        var process = enabled?.Keys.Cast<object>().FirstOrDefault(candidate =>
            (processIngredientFilter!.GetValue(candidate) as ThingFilter)?.Allows(ware.def) == true);
        if (process is null)
        {
            return false;
        }

        var capacityCount = ProcessorAdmissionCapacityCount(processor, process, ware);
        return capacityCount > 0 &&
               dishwasher.CountCanAcceptProcessorWare(ware, capacityCount) >= capacityCount;
    }

    private static void RollBackProcessorAdmission(
        object processor,
        ThingWithComps parent,
        ProcessorAdmissionState state)
    {
        var processes = ActiveProcesses(processor);
        var addedProcesses = processes
            .Cast<object>()
            .Where(candidate => state.ExistingProcesses.All(existing =>
                !ReferenceEquals(existing, candidate)))
            .ToList();
        var addedWare = addedProcesses
            .SelectMany(ProcessIngredients)
            .Distinct()
            .ToList();
        foreach (var process in addedProcesses)
        {
            processes.Remove(process);
        }

        var owner = processorInnerContainer!.GetValue(processor) as ThingOwner;
        foreach (var ware in addedWare)
        {
            owner?.Remove(ware);
            RestoreAdmissionThing(ware, parent, state);
        }

        if (addedWare.Count == 0 && IsOwnedByProcessor(processor, state.Ware))
        {
            RemoveDanglingProcessorReference(processor, state.Ware);
            owner?.Remove(state.Ware);
            RestoreAdmissionThing(state.Ware, parent, state);
        }

        if (processes.Count == 0)
        {
            FinalizeEmptyProcessor(processor, parent);
        }
    }

    private static void RestoreAdmissionThing(
        Thing ware,
        ThingWithComps parent,
        ProcessorAdmissionState state)
    {
        if (ware.Spawned ||
            state.PreviousOwner is not null &&
            state.PreviousOwner.TryAddOrTransfer(ware, canMergeWithExistingStacks: false))
        {
            return;
        }

        if (state.PreviousMap is not null && state.PreviousPosition.IsValid &&
            GenPlace.TryPlaceThing(
                ware,
                state.PreviousPosition,
                state.PreviousMap,
                ThingPlaceMode.Near))
        {
            return;
        }

        if (parent.Map is { } map)
        {
            GenPlace.TryPlaceThing(ware, parent.InteractionCell, map, ThingPlaceMode.Near);
        }
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

    private static object? FindEnabledProcess(object processor, ThingDef ingredient)
    {
        var enabled = processorEnabledProcesses!.GetValue(processor) as IDictionary;
        if (enabled is null)
        {
            return null;
        }

        foreach (DictionaryEntry entry in enabled)
        {
            if (entry.Key is not null &&
                ProcessFilterAllows(entry.Value, ingredient))
            {
                return entry.Key;
            }
        }

        return null;
    }

    private static bool ProcessHasSpaceAndRemainsEnabled(
        object processor,
        object process,
        ThingDef ingredient)
    {
        var enabled = processorEnabledProcesses!.GetValue(processor) as IDictionary;
        return enabled is not null &&
               enabled.Contains(process) &&
               ProcessFilterAllows(enabled[process], ingredient) &&
               Convert.ToInt32(spaceLeftFor!.Invoke(processor, new[] { process, 1f })) >= 1;
    }

    private static bool ProcessFilterAllows(object? processFilter, ThingDef ingredient)
    {
        return processFilter is not null &&
               processFilterAllowedIngredients!.GetValue(processFilter) is List<ThingDef> allowed &&
               allowed.Contains(ingredient);
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
        field.SetValue(target, CoerceFieldValue(field.FieldType, value));
    }

    internal static object? CoerceFieldValue(Type fieldType, object? value)
    {
        if (fieldType == typeof(string) && value is TaggedString tagged)
        {
            return tagged.ToString();
        }

        if (value is null || fieldType.IsInstanceOfType(value))
        {
            return value;
        }

        throw new ArgumentException(
            $"Value of type '{value.GetType().FullName}' cannot be assigned to '{fieldType.FullName}'.",
            nameof(value));
    }

    internal static bool HasExactFieldShape(Type declaringType, string fieldName, Type expectedType)
    {
        return declaringType.GetField(
                   fieldName,
                   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.FieldType == expectedType;
    }
}
