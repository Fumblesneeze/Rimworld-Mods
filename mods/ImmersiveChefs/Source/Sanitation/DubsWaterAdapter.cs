using System.Reflection;
using Verse;

namespace ImmersiveChefs;

internal static class DubsWaterAdapter
{
    private static bool shapeInspected;
    private static Type? pipeCompType;
    private static PropertyInfo? pipeNetProperty;
    private static MethodInfo? pullWaterMethod;
    private static PropertyInfo? waterStorageProperty;
    private static Type? pipePropertiesType;
    private static FieldInfo? pipeModeField;
    private static bool dishwasherDefsInitialized;

    internal static bool TryInitializeDishwasherDefs(out string reason)
    {
        InspectShape();
        if (pipeCompType is null || pipeNetProperty is null || pullWaterMethod is null ||
            waterStorageProperty is null || pipePropertiesType is null || pipeModeField is null)
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
            dishwasherDefsInitialized = true;
            reason = string.Empty;
            Log.Message("[ImmersiveChefs] Dubs Bad Hygiene adapter active; dishwashers require supplied plumbing.");
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
            if (pipeCompType is null || pipeNetType is null || contaminationType is null)
            {
                continue;
            }

            pipeNetProperty = pipeCompType?.GetProperty("pipeNet", BindingFlags.Public | BindingFlags.Instance);
            waterStorageProperty = pipeNetType.GetProperty("WaterStorage", BindingFlags.Public | BindingFlags.Instance);
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
            if (pipeCompType is not null && pipeNetProperty is not null && pullWaterMethod is not null &&
                waterStorageProperty is not null && pipePropertiesType is not null && pipeModeField is not null)
            {
                return;
            }
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
}
