using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace ImmersiveChefs;

internal static class FoodTextureVarietyCompatibility
{
    internal static bool IsSupportedAssembly(AssemblyName identity) =>
        string.Equals(identity.Name, "FoodTextureVariety", StringComparison.Ordinal);

    internal static bool CanSupplementPersistence(
        bool requiredFieldsMatch, bool subGraphicForMatches, bool declaresPersistence) =>
        requiredFieldsMatch && subGraphicForMatches && !declaresPersistence;
}

internal static class FoodTextureVarietyAdapter
{
    internal const string ScribeLabel = "immersiveChefsFtvTextureGroup";
    internal const string GraphicTypeNameForDiagnostics = GraphicTypeName;

    private const string CompTypeName = "FoodTextureVariety.CompFoodAlternateTexture";
    private const string GraphicTypeName = "FoodTextureVariety.Graphic_MealVariantsExpanded";

    private static readonly ConditionalWeakTable<object, PersistedGroup> PendingGroups = new();
    private static Type? compType;
    private static FieldInfo? textureIndexField;
    private static FieldInfo? storedGraphicsField;
    private static FieldInfo? firstLoadField;
    private static FieldInfo? rotateOverrideField;
    private static FieldInfo? subGraphicsField;
    private static FieldInfo? graphicPathField;
    private static bool runtimeWarningIssued;

    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(Harmony harmony, out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        if (!TryResolveShape(out var subGraphicFor, out reason))
        {
            return false;
        }

        var exposeData = AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.ExposeData));
        var exposeDataPostfix = AccessTools.Method(
            typeof(FoodTextureVarietyAdapter),
            nameof(ThingWithCompsExposeDataPostfix));
        var subGraphicPrefix = AccessTools.Method(
            typeof(FoodTextureVarietyAdapter),
            nameof(SubGraphicForPrefix));
        var subGraphicPostfix = AccessTools.Method(
            typeof(FoodTextureVarietyAdapter),
            nameof(SubGraphicForPostfix));
        try
        {
            harmony.Patch(
                exposeData,
                postfix: new HarmonyMethod(exposeDataPostfix));
            harmony.Patch(
                subGraphicFor,
                prefix: new HarmonyMethod(subGraphicPrefix),
                postfix: new HarmonyMethod(subGraphicPostfix));
            Enabled = true;
            reason = string.Empty;
            Log.Message(
                "[ImmersiveChefs] Food Texture Variety save/load adapter active; " +
                "upstream texture ownership remains unchanged.");
            return true;
        }
        catch (Exception exception)
        {
            RemovePartialPatches(
                harmony,
                (exposeData, exposeDataPostfix),
                (subGraphicFor, subGraphicPrefix),
                (subGraphicFor, subGraphicPostfix));
            reason = $"patch installation failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    private static bool TryResolveShape(out MethodInfo subGraphicFor, out string reason)
    {
        subGraphicFor = null!;
        compType = AccessTools.TypeByName(CompTypeName);
        var graphicType = AccessTools.TypeByName(GraphicTypeName);
        if (compType is null || graphicType is null ||
            !typeof(ThingComp).IsAssignableFrom(compType) ||
            !typeof(Graphic_Collection).IsAssignableFrom(graphicType) ||
            !FoodTextureVarietyCompatibility.IsSupportedAssembly(compType.Assembly.GetName()) ||
            graphicType.Assembly != compType.Assembly)
        {
            reason = "Food Texture Variety must expose CompFoodAlternateTexture as a ThingComp and Graphic_MealVariantsExpanded as a Graphic_Collection in its own assembly";
            return false;
        }

        textureIndexField = DeclaredPublicField(compType, "textureIndex", typeof(int));
        storedGraphicsField = DeclaredPublicField(compType, "storedGraphics", typeof(Graphic[]));
        firstLoadField = DeclaredPublicField(compType, "firstLoad", typeof(bool));
        rotateOverrideField = DeclaredPublicField(compType, "rotateOverride", typeof(bool));
        subGraphicsField = typeof(Graphic_Collection).GetField(
            "subGraphics",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        graphicPathField = typeof(Graphic).GetField(
            "path",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        subGraphicFor = graphicType.GetMethod(
            "SubGraphicFor",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
            null,
            new[] { typeof(Thing) },
            null)!;
        var declaresPersistence = compType.GetMethod(
            "PostExposeData",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is not null;

        if (!FoodTextureVarietyCompatibility.CanSupplementPersistence(
                textureIndexField is not null && storedGraphicsField is not null && firstLoadField is not null &&
                rotateOverrideField is not null && subGraphicsField?.FieldType == typeof(Graphic[]) &&
                graphicPathField?.FieldType == typeof(string),
                subGraphicFor?.ReturnType == typeof(Graphic),
                declaresPersistence))
        {
            reason = declaresPersistence
                ? "Food Texture Variety now declares PostExposeData; its own persistence must be assessed before adding the supplemental save/load repair"
                : $"Food Texture Variety persistence contract is incompatible: textureIndex={textureIndexField is not null}; " +
                  $"storedGraphics={storedGraphicsField is not null}; firstLoad={firstLoadField is not null}; " +
                  $"rotateOverride={rotateOverrideField is not null}; subGraphics={subGraphicsField?.FieldType}; " +
                  $"path={graphicPathField?.FieldType}; SubGraphicFor={subGraphicFor?.ReturnType}";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static FieldInfo? DeclaredPublicField(Type type, string name, Type fieldType)
    {
        var field = type.GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        return field?.FieldType == fieldType ? field : null;
    }

    private static void ThingWithCompsExposeDataPostfix(ThingWithComps __instance)
    {
        object? comp;
        try
        {
            comp = FindComp(__instance);
        }
        catch (Exception exception)
        {
            WarnRuntimeOnce("could not find a meal texture component during serialization", exception);
            return;
        }

        if (comp is null)
        {
            return;
        }

        var group = FoodTextureVarietyPersistencePolicy.NoPersistedGroup;
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            try
            {
                group = CaptureGroupStart(__instance, comp);
            }
            catch (Exception exception)
            {
                WarnRuntimeOnce("could not capture a meal texture selection", exception);
            }
        }

        // Scribe failures must remain visible to RimWorld's save pipeline. Only the
        // optional adapter's reflection work is allowed to degrade gracefully.
        Scribe_Values.Look(
            ref group,
            ScribeLabel,
            FoodTextureVarietyPersistencePolicy.NoPersistedGroup);

        if (Scribe.mode != LoadSaveMode.LoadingVars)
        {
            return;
        }

        try
        {
            PendingGroups.Remove(comp);
            textureIndexField!.SetValue(comp, group);
            if (group >= 0)
            {
                PendingGroups.Add(comp, new PersistedGroup(group));
            }
        }
        catch (Exception exception)
        {
            WarnRuntimeOnce("could not register a loaded meal texture selection", exception);
        }
    }

    private static void SubGraphicForPrefix(object __instance, Thing __0)
    {
        try
        {
            var comp = FindComp(__0 as ThingWithComps);
            if (comp is null || firstLoadField!.GetValue(comp) is not true ||
                !PendingGroups.TryGetValue(comp, out var persisted))
            {
                return;
            }

            PendingGroups.Remove(comp);
            var finalized = subGraphicsField!.GetValue(__instance) as Graphic[];
            if (finalized is null ||
                !FoodTextureVarietyPersistencePolicy.CanRestoreGroup(persisted.Start, finalized.Length))
            {
                return;
            }

            var selected = new[]
            {
                finalized[persisted.Start],
                finalized[persisted.Start + 1],
                finalized[persisted.Start + 2]
            };
            storedGraphicsField!.SetValue(comp, selected);
            rotateOverrideField!.SetValue(comp, GraphicPath(selected[0]).Contains(
                "norotate",
                StringComparison.OrdinalIgnoreCase));
            textureIndexField!.SetValue(comp, persisted.Start);
            firstLoadField.SetValue(comp, false);
        }
        catch (Exception exception)
        {
            WarnRuntimeOnce("could not restore a meal texture selection", exception);
        }
    }

    private static void SubGraphicForPostfix(object __instance, Thing __0)
    {
        try
        {
            var comp = FindComp(__0 as ThingWithComps);
            if (comp is null || firstLoadField!.GetValue(comp) is not false)
            {
                return;
            }

            var finalized = subGraphicsField!.GetValue(__instance) as Graphic[];
            var selected = storedGraphicsField!.GetValue(comp) as Graphic[];
            var start = FoodTextureVarietyPersistencePolicy.FindUniqueGroupStart(
                finalized,
                selected,
                GraphicReferenceComparer.Instance);
            textureIndexField!.SetValue(comp, start);
        }
        catch (Exception exception)
        {
            WarnRuntimeOnce("could not record a meal texture selection", exception);
        }
    }

    private static object? FindComp(ThingWithComps? thing)
    {
        return thing?.AllComps.FirstOrDefault(comp => compType!.IsInstanceOfType(comp));
    }

    private static int CaptureGroupStart(ThingWithComps thing, object comp)
    {
        if (thing.Graphic is not Graphic_Collection graphic)
        {
            return FoodTextureVarietyPersistencePolicy.NoPersistedGroup;
        }

        var finalized = subGraphicsField!.GetValue(graphic) as Graphic[];
        if (firstLoadField!.GetValue(comp) is true)
        {
            return PendingGroups.TryGetValue(comp, out var pending) &&
                   FoodTextureVarietyPersistencePolicy.CanRestoreGroup(
                       pending.Start,
                       finalized?.Length ?? 0)
                ? pending.Start
                : FoodTextureVarietyPersistencePolicy.NoPersistedGroup;
        }

        var selected = storedGraphicsField!.GetValue(comp) as Graphic[];
        var start = FoodTextureVarietyPersistencePolicy.FindUniqueGroupStart(
            finalized,
            selected,
            GraphicReferenceComparer.Instance);
        textureIndexField!.SetValue(comp, start);
        return start;
    }

    private static void RemovePartialPatches(
        Harmony harmony,
        params (MethodBase Original, MethodInfo Patch)[] patches)
    {
        foreach (var (original, patch) in patches)
        {
            try
            {
                harmony.Unpatch(original, patch);
            }
            catch
            {
                // Keep the original installation failure authoritative.
            }
        }
    }

    private static string GraphicPath(Graphic graphic)
    {
        return graphicPathField!.GetValue(graphic) as string ?? string.Empty;
    }

    private static void WarnRuntimeOnce(string context, Exception exception)
    {
        if (runtimeWarningIssued)
        {
            return;
        }

        runtimeWarningIssued = true;
        Log.Warning(
            $"[ImmersiveChefs] Food Texture Variety adapter {context}; upstream selection will be used " +
            $"({exception.GetType().Name}: {exception.Message}).");
    }

    private sealed class PersistedGroup
    {
        internal PersistedGroup(int start)
        {
            Start = start;
        }

        internal int Start { get; }
    }

    private sealed class GraphicReferenceComparer : IEqualityComparer<Graphic>
    {
        internal static readonly GraphicReferenceComparer Instance = new();

        public bool Equals(Graphic? left, Graphic? right)
        {
            return ReferenceEquals(left, right);
        }

        public int GetHashCode(Graphic graphic)
        {
            return RuntimeHelpers.GetHashCode(graphic);
        }
    }
}
