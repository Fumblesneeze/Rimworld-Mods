using System.Reflection;
using HarmonyLib;
using Verse;

namespace ImmersiveChefs;

internal static class TextureVariationCompatibility
{
    internal const string AssemblyName = "VEF";
    internal const string PropertiesTypeName =
        "VEF.Buildings.CompProperties_RandomBuildingGraphic";
    internal const string CompTypeName = "VEF.Buildings.CompRandomBuildingGraphic";
    internal const string RandomGraphicsFieldName = "randomGraphics";
    internal const string OptionalNamesFieldName = "optionalNames";

    internal static bool IsSupported(
        string? assemblyName,
        string? propertiesTypeName,
        bool propertiesIsPublicConcreteCompProperties,
        bool propertiesHasPublicParameterlessConstructor,
        bool randomGraphicsIsPublicInstanceStringList,
        bool optionalNamesIsPublicInstanceStringList,
        string? compTypeName,
        bool compIsPublicConcreteThingComp,
        bool compHasPublicParameterlessConstructor,
        bool constructorAssignsExactCompClass,
        bool changeGraphicHasExactShape,
        bool exposeDataOverridesThingComp,
        bool gizmosOverrideThingComp)
    {
        return assemblyName == AssemblyName &&
               propertiesTypeName == PropertiesTypeName &&
               propertiesIsPublicConcreteCompProperties &&
               propertiesHasPublicParameterlessConstructor &&
               randomGraphicsIsPublicInstanceStringList &&
               optionalNamesIsPublicInstanceStringList &&
               compTypeName == CompTypeName &&
               compIsPublicConcreteThingComp &&
               compHasPublicParameterlessConstructor &&
               constructorAssignsExactCompClass &&
               changeGraphicHasExactShape &&
               exposeDataOverridesThingComp &&
               gizmosOverrideThingComp;
    }
}

internal static class TextureVariationAdapter
{
    internal static bool TryValidateBuildingShape(out string reason)
    {
        var propertiesType = AccessTools.TypeByName(
            TextureVariationCompatibility.PropertiesTypeName);
        var compType = AccessTools.TypeByName(TextureVariationCompatibility.CompTypeName);
        var propertiesConstructor = propertiesType?.GetConstructor(Type.EmptyTypes);
        var compConstructor = compType?.GetConstructor(Type.EmptyTypes);
        var randomGraphics = DeclaredPublicInstanceField(
            propertiesType,
            TextureVariationCompatibility.RandomGraphicsFieldName);
        var optionalNames = DeclaredPublicInstanceField(
            propertiesType,
            TextureVariationCompatibility.OptionalNamesFieldName);
        var changeGraphic = compType?.GetMethod(
            "ChangeGraphic",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            binder: null,
            types: new[] { typeof(bool), typeof(int), typeof(bool) },
            modifiers: null);
        var exposeData = compType?.GetMethod(
            nameof(ThingComp.PostExposeData),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        var gizmos = compType?.GetMethod(
            nameof(ThingComp.CompGetGizmosExtra),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        var exactCompClass = false;
        if (propertiesConstructor is not null && compType is not null)
        {
            try
            {
                exactCompClass = propertiesConstructor.Invoke(Array.Empty<object>()) is
                    CompProperties properties && properties.compClass == compType;
            }
            catch
            {
                exactCompClass = false;
            }
        }

        var assemblyName = propertiesType?.Assembly.GetName().Name;
        var supported = TextureVariationCompatibility.IsSupported(
            assemblyName,
            propertiesType?.FullName,
            propertiesType?.IsPublic == true &&
            propertiesType.IsAbstract == false &&
            typeof(CompProperties).IsAssignableFrom(propertiesType),
            propertiesConstructor?.IsPublic == true,
            IsPublicInstanceStringList(randomGraphics),
            IsPublicInstanceStringList(optionalNames),
            compType?.FullName,
            compType?.IsPublic == true &&
            compType.IsAbstract == false &&
            typeof(ThingComp).IsAssignableFrom(compType),
            compConstructor?.IsPublic == true,
            exactCompClass,
            changeGraphic?.ReturnType == typeof(void),
            IsExactThingCompOverride(exposeData, typeof(void)),
            IsExactThingCompOverride(gizmos, typeof(IEnumerable<Gizmo>)));
        if (!supported ||
            propertiesType?.Assembly != compType?.Assembly ||
            compConstructor is null)
        {
            reason =
                "the installed Vanilla Expanded Framework building-variation shape no longer matches the validated RimWorld 1.6 API";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static FieldInfo? DeclaredPublicInstanceField(Type? type, string name)
    {
        return type?.GetField(
            name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }

    private static bool IsPublicInstanceStringList(FieldInfo? field)
    {
        return field is { IsPublic: true, IsStatic: false } &&
               field.FieldType == typeof(List<string>);
    }

    private static bool IsExactThingCompOverride(MethodInfo? method, Type returnType)
    {
        return method is { IsVirtual: true } &&
               method.ReturnType == returnType &&
               method.GetBaseDefinition().DeclaringType == typeof(ThingComp);
    }
}
