using System.Reflection;
using HarmonyLib;
using Verse;

namespace ImmersiveChefs;

internal static class DubsWaterAdapter
{
    private static bool shapeInspected;
    private static Type? pipeCompType;
    private static MethodInfo? pipeInspectMethod;
    private static PropertyInfo? pipeNetProperty;
    private static MethodInfo? pullWaterMethod;
    private static PropertyInfo? waterStorageProperty;
    private static FieldInfo? waterTowersField;
    private static Type? pipePropertiesType;
    private static FieldInfo? pipeModeField;
    private static Type? washBucketType;
    private static Type? assignableFixtureType;
    private static Type? baseWellCompType;
    private static PropertyInfo? fixtureKindProperty;
    private static PropertyInfo? forAnimalsOnlyProperty;
    private static object? basinFixtureKind;
    private static object? bathFixtureKind;
    private static MethodInfo? fixtureWorkingMethod;
    private static MethodInfo? pawnAllowedMethod;
    private static MethodInfo? useBucketMethod;
    private static FieldInfo? waterUsesRemainingField;
    private static MethodInfo? allFixturesMethod;
    private static MethodInfo? fixtureEverUsableMethod;
    private static MethodInfo? fixtureUsableNowMethod;
    private static bool dishwasherDefsInitialized;
    private static bool pipeInspectPatched;
    private static bool integratedSinkDiscoveryPatched;

    internal static bool IntegratedSinkBridgeAvailable => dishwasherDefsInitialized;

    internal static bool TryInitializeDishwasherDefs(out string reason)
    {
        InspectShape();
        if (pipeCompType is null || pipeNetProperty is null || pullWaterMethod is null ||
            waterStorageProperty is null || waterTowersField is null ||
            pipePropertiesType is null || pipeModeField is null || pipeInspectMethod is null ||
            assignableFixtureType is null || fixtureKindProperty is null ||
            forAnimalsOnlyProperty is null || basinFixtureKind is null || bathFixtureKind is null ||
            fixtureWorkingMethod is null || pawnAllowedMethod is null ||
            allFixturesMethod is null || fixtureEverUsableMethod is null || fixtureUsableNowMethod is null ||
            ImmersiveChefsMod.HarmonyInstance is null)
        {
            reason = "the installed Dubs plumbing API no longer matches the validated 1.6 shape";
            return false;
        }

        if (dishwasherDefsInitialized)
        {
            reason = string.Empty;
            return true;
        }

        try
        {
            AddPipeComp(ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher);
            AddPipeComp(ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher);
            foreach (var sinkDef in DefDatabase<ThingDef>.AllDefsListForReading.Where(def =>
                         def.GetModExtension<IntegratedSinkExtension>() is not null))
            {
                AddPipeComp(sinkDef);
            }
            PatchPipeInspect(ImmersiveChefsMod.HarmonyInstance);
            PatchIntegratedSinkDiscovery(ImmersiveChefsMod.HarmonyInstance);
            dishwasherDefsInitialized = true;
            reason = string.Empty;
            Log.Message("[ImmersiveChefs] Dubs Bad Hygiene adapter active; dishwashers and integrated sinks require supplied plumbing.");
            return true;
        }
        catch (Exception exception)
        {
            reason = $"dishwasher plumbing setup failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    internal static bool TryConsumeCycleWater(Thing appliance, float amount, out string reason)
    {
        if (appliance is not ThingWithComps withComps)
        {
            reason = "plumbing component unavailable";
            return false;
        }

        InspectShape();
        if (pipeCompType is null || pipeNetProperty is null || pullWaterMethod is null)
        {
            reason = "Dubs plumbing adapter unavailable";
            return false;
        }

        var comp = withComps.AllComps.FirstOrDefault(pipeCompType.IsInstanceOfType);
        var pipeNet = comp is null ? null : pipeNetProperty.GetValue(comp);
        if (pipeNet is null)
        {
            reason = "not connected to water";
            return false;
        }

        if (!HasSuppliedConnection(appliance))
        {
            reason = "water supply unavailable";
            return false;
        }

        try
        {
            var parameters = pullWaterMethod.GetParameters();
            var contamination = Activator.CreateInstance(parameters[1].ParameterType.GetElementType()!);
            var arguments = new object?[] { Math.Max(0.001f, amount), contamination };
            var result = pullWaterMethod.Invoke(pipeNet, arguments);
            if (result is not true)
            {
                reason = "insufficient water";
                return false;
            }

            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning($"[ImmersiveChefs] Dubs water debit failed closed: {exception.GetType().Name}: {exception.Message}");
            reason = "water debit failed";
            return false;
        }
    }

    internal static bool IsPlumbedDubsFixture(Thing thing)
    {
        InspectShape();
        return pipeCompType is not null && thing is ThingWithComps withComps &&
               withComps.AllComps.Any(pipeCompType.IsInstanceOfType);
    }

    internal static bool IsConnected(Thing thing)
    {
        InspectShape();
        if (pipeCompType is null || pipeNetProperty is null || thing is not ThingWithComps withComps)
        {
            return false;
        }

        var comp = withComps.AllComps.FirstOrDefault(pipeCompType.IsInstanceOfType);
        return comp is not null && pipeNetProperty.GetValue(comp) is not null;
    }

    internal static bool IsOperationalFixture(Thing thing)
    {
        if (!IsConnected(thing))
        {
            return false;
        }

        var power = thing.TryGetComp<RimWorld.CompPowerTrader>();
        var flick = thing.TryGetComp<RimWorld.CompFlickable>();
        var breakdown = thing.TryGetComp<RimWorld.CompBreakdownable>();
        return (power is null || power.PowerOn) &&
               (flick is null || flick.SwitchIsOn) &&
               (breakdown is null || !breakdown.BrokenDown);
    }

    internal static bool CanSupplyCycleWater(Thing thing, float amount = 1f)
    {
        InspectShape();
        if (pipeCompType is null || pipeNetProperty is null || waterStorageProperty is null ||
            thing is not ThingWithComps withComps)
        {
            return false;
        }

        var comp = withComps.AllComps.FirstOrDefault(pipeCompType.IsInstanceOfType);
        var pipeNet = comp is null ? null : pipeNetProperty.GetValue(comp);
        return pipeNet is not null && waterStorageProperty.GetValue(pipeNet) is float stored &&
                stored >= Math.Max(0.001f, amount);
    }

    internal static bool HasSuppliedConnection(Thing thing)
    {
        InspectShape();
        if (pipeCompType is null || pipeNetProperty is null || waterTowersField is null ||
            thing is not ThingWithComps withComps)
        {
            return false;
        }

        var comp = withComps.AllComps.FirstOrDefault(pipeCompType.IsInstanceOfType);
        var pipeNet = comp is null ? null : pipeNetProperty.GetValue(comp);
        return pipeNet is not null &&
               waterTowersField.GetValue(pipeNet) is System.Collections.ICollection towers &&
               towers.Count > 0;
    }

    internal static bool CanUseIntegratedSink(Thing thing, float requiredWater)
    {
        if (thing.def.GetModExtension<IntegratedSinkExtension>() is null)
        {
            return false;
        }

        return IsOperationalFixture(thing) &&
               HasSuppliedConnection(thing) &&
               CanSupplyCycleWater(thing, IntegratedSinkWaterPolicy.RequiredAmount(requiredWater));
    }

    internal static bool TryClassifyHandwashingSource(
        Pawn pawn,
        Thing thing,
        out HandwashingSourceKind kind,
        out bool pawnAllowed,
        out bool operational,
        out bool hasAvailableWater)
    {
        InspectShape();
        kind = default;
        pawnAllowed = false;
        operational = false;
        hasAvailableWater = false;

        if (thing.def.GetModExtension<IntegratedSinkExtension>() is { } integratedSink)
        {
            kind = HandwashingSourceKind.ConnectedFixture;
            pawnAllowed = true;
            operational = IsOperationalFixture(thing);
            hasAvailableWater = HasSuppliedConnection(thing) &&
                                CanSupplyCycleWater(
                                    thing,
                                    IntegratedSinkWaterPolicy.RequiredAmount(
                                        integratedSink.dishwashingWater));
            return IsPlumbedDubsFixture(thing);
        }

        if (HasDrinkableFixtureCapability(thing))
        {
            if (!TryReadNativeFixtureReports(pawn, thing, 1f, out pawnAllowed, out var fixtureWorking))
            {
                return false;
            }

            if (IsPlumbedDubsFixture(thing))
            {
                kind = HandwashingSourceKind.ConnectedFixture;
                operational = IsOperationalFixture(thing) && fixtureWorking;
                hasAvailableWater = HasSuppliedConnection(thing) &&
                                    CanSupplyCycleWater(thing, 1f);
                return true;
            }

            if (washBucketType?.IsInstanceOfType(thing) == true)
            {
                kind = HandwashingSourceKind.HauledWater;
                operational = IsOperationalObject(thing) && fixtureWorking;
                hasAvailableWater = CanUseHauledWater(thing);
                return true;
            }

            return false;
        }

        if (HasBaseWellComp(thing))
        {
            kind = HandwashingSourceKind.Well;
            pawnAllowed = true;
            operational = IsOperationalObject(thing);
            hasAvailableWater = true;
            return true;
        }

        return false;
    }

    internal static bool TryUseHandwashingSource(Pawn pawn, Thing thing, out string reason)
    {
        if (!TryClassifyHandwashingSource(
                pawn,
                thing,
                out var kind,
                out var pawnAllowed,
                out var operational,
                out var hasAvailableWater) ||
            !pawnAllowed || !operational || !hasAvailableWater)
        {
            reason = "water source unavailable";
            return false;
        }

        if (kind == HandwashingSourceKind.ConnectedFixture)
        {
            var waterUse = thing.def.GetModExtension<IntegratedSinkExtension>()?.dishwashingWater ?? 1f;
            return TryConsumeCycleWater(
                thing,
                IntegratedSinkWaterPolicy.RequiredAmount(waterUse),
                out reason);
        }

        if (kind == HandwashingSourceKind.Well)
        {
            reason = string.Empty;
            return true;
        }

        try
        {
            useBucketMethod!.Invoke(thing, Array.Empty<object>());
            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning($"[ImmersiveChefs] Dubs hauled-water use failed closed: {exception.GetType().Name}: {exception.Message}");
            reason = "hauled water use failed";
            return false;
        }
    }

    private static void InspectShape()
    {
        if (shapeInspected)
        {
            return;
        }

        shapeInspected = true;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name?.IndexOf("BadHygiene", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            pipeCompType = assembly.GetType("DubsBadHygiene.CompPipe", throwOnError: false);
            pipePropertiesType = assembly.GetType("DubsBadHygiene.CompProperties_Pipe", throwOnError: false);
            var pipeNetType = assembly.GetType("DubsBadHygiene.PlumbingNet", throwOnError: false);
            var contaminationType = assembly.GetType("DubsBadHygiene.ContaminationLevel", throwOnError: false);
            washBucketType = assembly.GetType("DubsBadHygiene.Building_washbucket", throwOnError: false);
            assignableFixtureType = assembly.GetType(
                "DubsBadHygiene.Building_AssignableFixture",
                throwOnError: false);
            baseWellCompType = assembly.GetType("DubsBadHygiene.CompBaseWell", throwOnError: false);
            var fixtureKindType = assembly.GetType("DubsBadHygiene.FixtureType", throwOnError: false);
            var sanitationUtilType = assembly.GetType("DubsBadHygiene.SanitationUtil", throwOnError: false);
            var closestSanitationType = assembly.GetType("DubsBadHygiene.ClosestSanitation", throwOnError: false);
            if (pipeCompType is null || pipeNetType is null || contaminationType is null)
            {
                continue;
            }

            pipeNetProperty = pipeCompType?.GetProperty("pipeNet", BindingFlags.Public | BindingFlags.Instance);
            pipeInspectMethod = FindPipeInspectOverride(pipeCompType!);
            waterStorageProperty = pipeNetType.GetProperty("WaterStorage", BindingFlags.Public | BindingFlags.Instance);
            waterTowersField = pipeNetType.GetField(
                "WaterTowers",
                BindingFlags.Public | BindingFlags.Instance);
            if (waterTowersField is not null &&
                !typeof(System.Collections.ICollection).IsAssignableFrom(waterTowersField.FieldType))
            {
                waterTowersField = null;
            }
            pullWaterMethod = pipeNetType?.GetMethod(
                "PullWater",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[]
                {
                    typeof(float),
                    contaminationType.MakeByRefType()
                },
                modifiers: null);
            pipeModeField = pipePropertiesType?.GetField(
                "mode",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            fixtureWorkingMethod = assignableFixtureType?.GetMethod(
                "Working",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                binder: null,
                types: new[] { typeof(float) },
                modifiers: null);
            if (fixtureWorkingMethod?.ReturnType != typeof(AcceptanceReport))
            {
                fixtureWorkingMethod = null;
            }
            pawnAllowedMethod = assignableFixtureType?.GetMethod(
                "PawnAllowed",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                binder: null,
                types: new[] { typeof(Pawn) },
                modifiers: null);
            if (pawnAllowedMethod?.ReturnType != typeof(AcceptanceReport))
            {
                pawnAllowedMethod = null;
            }
            useBucketMethod = washBucketType?.GetMethod(
                "UseBucket",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (useBucketMethod?.ReturnType != typeof(void))
            {
                useBucketMethod = null;
            }
            waterUsesRemainingField = washBucketType?.GetField(
                "WaterUsesRemaining",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (waterUsesRemainingField?.FieldType != typeof(int))
            {
                waterUsesRemainingField = null;
            }
            fixtureKindProperty = assignableFixtureType?.GetProperty(
                "fixture",
                BindingFlags.Public | BindingFlags.Instance);
            forAnimalsOnlyProperty = assignableFixtureType?.GetProperty(
                "ForAnimalsOnly",
                BindingFlags.Public | BindingFlags.Instance);
            if (fixtureKindType is null || !fixtureKindType.IsEnum ||
                fixtureKindProperty?.PropertyType != fixtureKindType ||
                forAnimalsOnlyProperty?.PropertyType != typeof(bool))
            {
                fixtureKindProperty = null;
                forAnimalsOnlyProperty = null;
            }
            else
            {
                basinFixtureKind = Enum.Parse(fixtureKindType, "Basin", ignoreCase: false);
                bathFixtureKind = Enum.Parse(fixtureKindType, "Bath", ignoreCase: false);
            }
            allFixturesMethod = sanitationUtilType?.GetMethod(
                "AllFixtures",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Map) },
                modifiers: null);
            if (allFixturesMethod?.ReturnType != typeof(IEnumerable<Thing>))
            {
                allFixturesMethod = null;
            }
            fixtureEverUsableMethod = fixtureKindType is null
                ? null
                : closestSanitationType?.GetMethod(
                    "IsEverUsable",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    binder: null,
                    types: new[]
                    {
                        typeof(Thing),
                        typeof(Pawn),
                        typeof(Pawn),
                        typeof(bool),
                        fixtureKindType.MakeArrayType(),
                        typeof(bool)
                    },
                    modifiers: null);
            if (fixtureEverUsableMethod?.ReturnType != typeof(bool))
            {
                fixtureEverUsableMethod = null;
            }
            fixtureUsableNowMethod = closestSanitationType?.GetMethod(
                "UsableNow",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Thing), typeof(Pawn), typeof(bool), typeof(float) },
                modifiers: null);
            if (fixtureUsableNowMethod?.ReturnType != typeof(bool))
            {
                fixtureUsableNowMethod = null;
            }
            if (pipeCompType is not null && pipeNetProperty is not null && pullWaterMethod is not null &&
                waterStorageProperty is not null && waterTowersField is not null &&
                pipePropertiesType is not null && pipeModeField is not null && pipeInspectMethod is not null &&
                assignableFixtureType is not null && fixtureKindProperty is not null &&
                forAnimalsOnlyProperty is not null && basinFixtureKind is not null && bathFixtureKind is not null &&
                fixtureWorkingMethod is not null && pawnAllowedMethod is not null &&
                allFixturesMethod is not null && fixtureEverUsableMethod is not null &&
                fixtureUsableNowMethod is not null)
            {
                return;
            }
        }
    }

    internal static MethodInfo? FindPipeInspectOverride(Type type)
    {
        var method = type.GetMethod(
            nameof(ThingComp.CompInspectStringExtra),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        return method is { ReturnType: not null } &&
               method.ReturnType == typeof(string) &&
               method.IsVirtual &&
               method.GetBaseDefinition().DeclaringType == typeof(ThingComp)
            ? method
            : null;
    }

    private static void PatchPipeInspect(Harmony harmony)
    {
        if (pipeInspectPatched)
        {
            return;
        }

        var postfix = typeof(DubsWaterAdapter).GetMethod(
            nameof(SuppressDishwasherPipeInspect),
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(typeof(DubsWaterAdapter).FullName, nameof(SuppressDishwasherPipeInspect));
        harmony.Patch(pipeInspectMethod!, postfix: new HarmonyMethod(postfix));
        pipeInspectPatched = true;
    }

    private static void PatchIntegratedSinkDiscovery(Harmony harmony)
    {
        if (integratedSinkDiscoveryPatched)
        {
            return;
        }

        var append = typeof(DubsWaterAdapter).GetMethod(
            nameof(AppendIntegratedSinkFixtures),
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(
                typeof(DubsWaterAdapter).FullName,
                nameof(AppendIntegratedSinkFixtures));
        var everUsable = typeof(DubsWaterAdapter).GetMethod(
            nameof(FilterIntegratedSinkEverUsable),
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(
                typeof(DubsWaterAdapter).FullName,
                nameof(FilterIntegratedSinkEverUsable));
        var usableNow = typeof(DubsWaterAdapter).GetMethod(
            nameof(FilterIntegratedSinkUsableNow),
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(
                typeof(DubsWaterAdapter).FullName,
                nameof(FilterIntegratedSinkUsableNow));

        harmony.Patch(allFixturesMethod!, postfix: new HarmonyMethod(append));
        harmony.Patch(fixtureEverUsableMethod!, postfix: new HarmonyMethod(everUsable));
        harmony.Patch(fixtureUsableNowMethod!, postfix: new HarmonyMethod(usableNow));
        integratedSinkDiscoveryPatched = true;
    }

    private static void AppendIntegratedSinkFixtures(Map __0, ref IEnumerable<Thing> __result)
    {
        __result = EnumerateFixturesWithIntegratedSinks(__result, __0);
    }

    private static IEnumerable<Thing> EnumerateFixturesWithIntegratedSinks(
        IEnumerable<Thing> original,
        Map map)
    {
        var seen = new HashSet<Thing>();
        foreach (var thing in original)
        {
            if (thing is not null && seen.Add(thing))
            {
                yield return thing;
            }
        }

        foreach (var thing in map.listerThings.AllThings)
        {
            if (IntegratedSinkRuntime.IsIntegratedSink(thing) && seen.Add(thing))
            {
                yield return thing;
            }
        }
    }

    private static void FilterIntegratedSinkEverUsable(Thing __0, ref bool __result)
    {
        if (__result && IntegratedSinkRuntime.IsIntegratedSink(__0))
        {
            var water = __0.def.GetModExtension<IntegratedSinkExtension>()?.minimumDrinkWater ?? 0.04f;
            __result = CanUseIntegratedSink(__0, water);
        }
    }

    private static void FilterIntegratedSinkUsableNow(Thing __0, ref bool __result)
    {
        if (__result && IntegratedSinkRuntime.IsIntegratedSink(__0))
        {
            var water = __0.def.GetModExtension<IntegratedSinkExtension>()?.minimumDrinkWater ?? 0.04f;
            __result = CanUseIntegratedSink(__0, water);
        }
    }

    private static void SuppressDishwasherPipeInspect(ThingComp __instance, ref string __result)
    {
        if (__instance.parent is not null &&
            (IntegratedSinkRuntime.IsIntegratedSink(__instance.parent) ||
            DishwasherInspectPolicy.ShouldSuppressOptionalDiagnostic(
                __instance.parent?.def?.defName,
                __instance.GetType().Assembly.GetName().Name,
                __instance.GetType().FullName)))
        {
            __result = string.Empty;
        }
    }

    private static void AddPipeComp(ThingDef def)
    {
        def.comps ??= new List<CompProperties>();
        if (def.comps.Any(properties => pipePropertiesType!.IsInstanceOfType(properties)))
        {
            return;
        }

        var properties = (CompProperties)(Activator.CreateInstance(pipePropertiesType!)
                         ?? throw new InvalidOperationException("Dubs pipe properties could not be created."));
        pipeModeField!.SetValue(
            properties,
            Enum.Parse(pipeModeField.FieldType, "Sewage", ignoreCase: true));
        properties.ResolveReferences(def);
        def.comps.Add(properties);
    }

    private static bool CanUseHauledWater(Thing thing)
    {
        if (washBucketType?.IsInstanceOfType(thing) != true ||
            useBucketMethod is null ||
            waterUsesRemainingField?.GetValue(thing) is not int uses || uses <= 0)
        {
            return false;
        }

        return true;
    }

    private static bool TryReadNativeFixtureReports(
        Pawn pawn,
        Thing thing,
        float waterUsed,
        out bool pawnAllowed,
        out bool working)
    {
        pawnAllowed = false;
        working = false;
        if (assignableFixtureType?.IsInstanceOfType(thing) != true ||
            pawnAllowedMethod is null || fixtureWorkingMethod is null)
        {
            return false;
        }

        try
        {
            pawnAllowed = pawnAllowedMethod.Invoke(thing, new object[] { pawn }) is AcceptanceReport pawnReport &&
                          pawnReport.Accepted;
            working = fixtureWorkingMethod.Invoke(thing, new object[] { waterUsed }) is AcceptanceReport workReport &&
                      workReport.Accepted;
            return true;
        }
        catch
        {
            pawnAllowed = false;
            working = false;
            return false;
        }
    }

    internal static bool HasDrinkableFixtureCapability(Thing thing)
    {
        if (assignableFixtureType?.IsInstanceOfType(thing) != true ||
            fixtureKindProperty is null || forAnimalsOnlyProperty is null ||
            basinFixtureKind is null || bathFixtureKind is null)
        {
            return false;
        }

        try
        {
            if (forAnimalsOnlyProperty.GetValue(thing) is true)
            {
                return false;
            }

            var fixtureKind = fixtureKindProperty.GetValue(thing);
            return Equals(fixtureKind, basinFixtureKind) || Equals(fixtureKind, bathFixtureKind);
        }
        catch
        {
            return false;
        }
    }

    private static bool HasBaseWellComp(Thing thing)
    {
        return baseWellCompType is not null && thing is ThingWithComps withComps &&
               withComps.AllComps.Any(baseWellCompType.IsInstanceOfType);
    }

    private static bool IsOperationalObject(Thing thing)
    {
        var power = thing.TryGetComp<RimWorld.CompPowerTrader>();
        var fuel = thing.TryGetComp<RimWorld.CompRefuelable>();
        var flick = thing.TryGetComp<RimWorld.CompFlickable>();
        var breakdown = thing.TryGetComp<RimWorld.CompBreakdownable>();
        return (power is null || power.PowerOn) &&
               (fuel is null || fuel.HasFuel) &&
               (flick is null || flick.SwitchIsOn) &&
               (breakdown is null || !breakdown.BrokenDown);
    }
}
