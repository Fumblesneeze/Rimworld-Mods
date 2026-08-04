using ImmersiveChefs;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PasteDispenserCompatibilityTests
{
    private static readonly string[] VnpePipeComponents =
    {
        "PipeSystem.CompProperties_Resource"
    };

    [TestCase(false)]
    [TestCase(true)]
    public void Vanilla_dispenser_remains_supported_independently_of_optional_integration(bool vnpeEnabled)
    {
        var kind = PasteDispenserCompatibility.Classify(
            "NutrientPasteDispenser",
            "RimWorld.Building_NutrientPasteDispenser",
            "Assembly-CSharp",
            directBaseTypeName: "Verse.Building",
            componentTypeNames: Array.Empty<string>(),
            vnpeEnabled);

        Assert.That(kind, Is.EqualTo(PasteDispenserKind.Vanilla));
    }

    [Test]
    public void Exact_installed_vnpe_tap_shape_is_supported_only_when_integration_is_enabled()
    {
        var disabled = ClassifyVnpe(vnpeEnabled: false);
        var enabled = ClassifyVnpe(vnpeEnabled: true);

        Assert.Multiple(() =>
        {
            Assert.That(disabled, Is.EqualTo(PasteDispenserKind.Unsupported));
            Assert.That(enabled, Is.EqualTo(PasteDispenserKind.VanillaNutrientPasteExpanded));
        });
    }

    [TestCase("OtherTap", "VNPE.Building_NutrientPasteTap", "VNPE", "RimWorld.Building_NutrientPasteDispenser")]
    [TestCase("VNPE_NutrientPasteTap", "Other.Building_NutrientPasteTap", "VNPE", "RimWorld.Building_NutrientPasteDispenser")]
    [TestCase("VNPE_NutrientPasteTap", "VNPE.Building_NutrientPasteTap", "Lookalike", "RimWorld.Building_NutrientPasteDispenser")]
    [TestCase("VNPE_NutrientPasteTap", "VNPE.Building_NutrientPasteTap", "VNPE", "Verse.Building")]
    public void Changed_or_lookalike_vnpe_identity_fails_closed(
        string defName,
        string thingClassName,
        string assemblyName,
        string directBaseTypeName)
    {
        var kind = PasteDispenserCompatibility.Classify(
            defName,
            thingClassName,
            assemblyName,
            directBaseTypeName,
            VnpePipeComponents,
            vnpeEnabled: true);

        Assert.That(kind, Is.EqualTo(PasteDispenserKind.Unsupported));
    }

    [Test]
    public void Vnpe_tap_without_its_pipe_resource_component_fails_closed()
    {
        var kind = PasteDispenserCompatibility.Classify(
            "VNPE_NutrientPasteTap",
            "VNPE.Building_NutrientPasteTap",
            "VNPE",
            "RimWorld.Building_NutrientPasteDispenser",
            new[] { "Verse.CompProperties_Power" },
            vnpeEnabled: true);

        Assert.That(kind, Is.EqualTo(PasteDispenserKind.Unsupported));
    }

    [Test]
    public void Generic_method_name_lookalikes_are_not_supported()
    {
        var kind = PasteDispenserCompatibility.Classify(
            "ThirdPartyPasteThing",
            "ThirdParty.HasCanDispenseNowAndTryDispenseFood",
            "ThirdParty",
            "Verse.ThingWithComps",
            Array.Empty<string>(),
            vnpeEnabled: true);

        Assert.That(kind, Is.EqualTo(PasteDispenserKind.Unsupported));
    }

    private static PasteDispenserKind ClassifyVnpe(bool vnpeEnabled)
    {
        return PasteDispenserCompatibility.Classify(
            "VNPE_NutrientPasteTap",
            "VNPE.Building_NutrientPasteTap",
            "VNPE",
            "RimWorld.Building_NutrientPasteDispenser",
            VnpePipeComponents,
            vnpeEnabled);
    }
}
