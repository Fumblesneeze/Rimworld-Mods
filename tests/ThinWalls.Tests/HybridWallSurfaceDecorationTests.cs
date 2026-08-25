using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class HybridWallSurfaceDecorationTests
{
    [Test]
    public void StoneUsesOnlyTheInstalledCoreBrickSurface()
    {
        Assert.That(HybridWallSurfaceDecorationRecipe.Compile(ThinWallMaterialFamily.Stone), Is.Empty);
    }

    [TestCase(ThinWallMaterialFamily.Wood, HybridWallSurfaceDecorationKind.OsbFlake)]
    [TestCase(ThinWallMaterialFamily.Metal, HybridWallSurfaceDecorationKind.Rivet)]
    public void ManufacturedFamiliesUseDeterministicClippedFaceMarks(
        ThinWallMaterialFamily family,
        HybridWallSurfaceDecorationKind identifyingMark)
    {
        HybridWallSurfaceDecoration[] first = HybridWallSurfaceDecorationRecipe.Compile(family).ToArray();
        HybridWallSurfaceDecoration[] second = HybridWallSurfaceDecorationRecipe.Compile(family).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Select(mark => mark.Kind), Does.Contain(identifyingMark));
            Assert.That(first, Is.All.Matches<HybridWallSurfaceDecoration>(mark =>
                mark.MinU >= 0f && mark.MaxU <= 1f &&
                mark.MinV >= 0f && mark.MaxV <= 1f &&
                mark.MinU < mark.MaxU && mark.MinV < mark.MaxV));
            Assert.That(first, Has.None.Matches<HybridWallSurfaceDecoration>(mark =>
                mark.MinV <= 0f && mark.MaxV >= 1f),
                "a generated full-height face mark would recreate the rejected vertical artifacts");
        });
    }
}
