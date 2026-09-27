using NUnit.Framework;
using ThinWalls.Rooms;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class ThinWallHeatTransferTests
{
    [Test]
    public void HalfResistanceTransfersTwiceCoreWallEnergy()
    {
        const float first = 40f, second = 10f;
        float normalWallEnergy = (second - first) * 120f * 0.00017f;
        Assert.That(ThinWallHeatTransfer.Energy(first, 16, second, 16),
            Is.EqualTo(normalWallEnergy * 2f).Within(0.000001f));
    }

    [Test]
    public void TwoOutdoorReservoirsNeedNoEnergyTransfer()
    {
        Assert.That(ThinWallHeatTransfer.Energy(40f, 0, 10f, 0), Is.Zero);
    }

    [Test]
    public void VacuumPreservesCoreWallAttenuation()
    {
        float normal = ThinWallHeatTransfer.Energy(40f, 4, 10f, 9);
        Assert.That(ThinWallHeatTransfer.Energy(40f, 4, 10f, 9, inVacuum: true),
            Is.EqualTo(normal * 0.00005f).Within(0.00000001f));
    }

    [TestCase(1, 1)]
    [TestCase(1, 100)]
    [TestCase(16, 4)]
    public void RepeatedExchangeConservesHeatAndStaysWithinInitialTemperatures(int firstCells, int secondCells)
    {
        float first = 100f, second = -20f;
        float initialEnergy = first * firstCells + second * secondCells;
        for (int i = 0; i < 1000; i++)
        {
            float energy = ThinWallHeatTransfer.Energy(first, firstCells, second, secondCells);
            Assert.That(ThinWallHeatTransfer.Energy(second, secondCells, first, firstCells), Is.EqualTo(-energy));
            first += energy / firstCells;
            second -= energy / secondCells;
            Assert.That(first, Is.InRange(-20f, 100f));
            Assert.That(second, Is.InRange(-20f, 100f));
            Assert.That(first, Is.GreaterThanOrEqualTo(second - 0.0001f));
        }
        Assert.That(first * firstCells + second * secondCells, Is.EqualTo(initialEnergy).Within(0.01f));
    }

    [Test]
    public void OutdoorReservoirWarmsIndoorAirWithoutRequiringOutdoorCapacity()
    {
        float energy = ThinWallHeatTransfer.Energy(0f, 4, 20f, 0);
        Assert.That(energy / 4, Is.EqualTo(0.204f).Within(0.000001f));
        Assert.That(ThinWallHeatTransfer.Energy(20f, 0, 0f, 4), Is.EqualTo(-energy));
        Assert.That(ThinWallHeatTransfer.Energy(20f, 1, 20f, 100), Is.Zero);
    }
}
