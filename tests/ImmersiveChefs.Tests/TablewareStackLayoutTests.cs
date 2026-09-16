using NUnit.Framework;

namespace ImmersiveChefs.Tests;

public sealed class TablewareStackLayoutTests
{
    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(5, 5)]
    [TestCase(25, 5)]
    public void Physical_piles_show_bounded_layers_or_neighboring_settings(int stackCount, int expectedVisible)
    {
        var plates = TablewareStackLayout.For(KitchenwareProduct.Plate, stackCount);
        var cutlery = TablewareStackLayout.For(KitchenwareProduct.Cutlery, stackCount);
        Assert.Multiple(() =>
        {
            Assert.That(plates.Count, Is.EqualTo(expectedVisible));
            Assert.That(cutlery.Count, Is.EqualTo(expectedVisible));
            Assert.That(plates.All(p => p.X == 0), Is.True, "Plates stay aligned in one vertical pile.");
            Assert.That(plates.Select(p => p.Z).Distinct().Count(), Is.EqualTo(expectedVisible));
            Assert.That(cutlery.Select(p => p.X).Distinct().Count(), Is.EqualTo(expectedVisible));
            Assert.That(cutlery.Max(p => p.X) - cutlery.Min(p => p.X) + .55f, Is.LessThanOrEqualTo(1f),
                "The bunch fits within one map cell at the existing sprite size.");
        });
    }
}
