using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class HybridWallAtlasSupportTests
{
    [TestCase("ModdedWallAtlas", 640, 640)]
    [TestCase("Wall_Blueprint_Atlas", 320, 320)]
    [TestCase("Wall_Atlas_Bricks", 512, 256)]
    public void ReplacementAndBlueprintAtlasesDoNotNeedCorePixelIdentity(string name, int width, int height)
    {
        Assert.Multiple(() =>
        {
            Assert.That(HybridWallAtlasSupport.IsSupported(width, height), Is.True, name);
            Assert.That(HybridWallAtlasSupport.IsSupported(0, height), Is.False);
        });
    }
}
