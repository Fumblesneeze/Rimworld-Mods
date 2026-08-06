using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class TextureVariationCompatibilityTests
{
    [Test]
    public void Validated_vef_building_variation_shape_descriptor_is_supported()
    {
        var supported = TextureVariationCompatibility.IsSupported(
            assemblyName: "VEF",
            propertiesTypeName: "VEF.Buildings.CompProperties_RandomBuildingGraphic",
            propertiesIsPublicConcreteCompProperties: true,
            propertiesHasPublicParameterlessConstructor: true,
            randomGraphicsIsPublicInstanceStringList: true,
            optionalNamesIsPublicInstanceStringList: true,
            compTypeName: "VEF.Buildings.CompRandomBuildingGraphic",
            compIsPublicConcreteThingComp: true,
            compHasPublicParameterlessConstructor: true,
            constructorAssignsExactCompClass: true,
            changeGraphicHasExactShape: true,
            exposeDataOverridesThingComp: true,
            gizmosOverrideThingComp: true);

        Assert.That(supported, Is.True);
    }

    [TestCase("WrongAssembly", true, true, true, true, true, true, true, true, true, true, true)]
    [TestCase("VEF", false, true, true, true, true, true, true, true, true, true, true)]
    [TestCase("VEF", true, false, true, true, true, true, true, true, true, true, true)]
    [TestCase("VEF", true, true, false, true, true, true, true, true, true, true, true)]
    [TestCase("VEF", true, true, true, false, true, true, true, true, true, true, true)]
    [TestCase("VEF", true, true, true, true, false, true, true, true, true, true, true)]
    [TestCase("VEF", true, true, true, true, true, false, true, true, true, true, true)]
    [TestCase("VEF", true, true, true, true, true, true, false, true, true, true, true)]
    [TestCase("VEF", true, true, true, true, true, true, true, false, true, true, true)]
    [TestCase("VEF", true, true, true, true, true, true, true, true, false, true, true)]
    [TestCase("VEF", true, true, true, true, true, true, true, true, true, false, true)]
    [TestCase("VEF", true, true, true, true, true, true, true, true, true, true, false)]
    public void Any_changed_runtime_shape_fails_closed(
        string assemblyName,
        bool propertiesIsPublicConcreteCompProperties,
        bool propertiesHasPublicParameterlessConstructor,
        bool randomGraphicsIsPublicInstanceStringList,
        bool optionalNamesIsPublicInstanceStringList,
        bool compIsPublicConcreteThingComp,
        bool compHasPublicParameterlessConstructor,
        bool constructorAssignsExactCompClass,
        bool changeGraphicHasExactShape,
        bool exposeDataOverridesThingComp,
        bool gizmosOverrideThingComp,
        bool exactTypeNames)
    {
        var supported = TextureVariationCompatibility.IsSupported(
            assemblyName,
            exactTypeNames
                ? "VEF.Buildings.CompProperties_RandomBuildingGraphic"
                : "VEF.Buildings.ChangedProperties",
            propertiesIsPublicConcreteCompProperties,
            propertiesHasPublicParameterlessConstructor,
            randomGraphicsIsPublicInstanceStringList,
            optionalNamesIsPublicInstanceStringList,
            exactTypeNames
                ? "VEF.Buildings.CompRandomBuildingGraphic"
                : "VEF.Buildings.ChangedComp",
            compIsPublicConcreteThingComp,
            compHasPublicParameterlessConstructor,
            constructorAssignsExactCompClass,
            changeGraphicHasExactShape,
            exposeDataOverridesThingComp,
            gizmosOverrideThingComp);

        Assert.That(supported, Is.False);
    }
}
