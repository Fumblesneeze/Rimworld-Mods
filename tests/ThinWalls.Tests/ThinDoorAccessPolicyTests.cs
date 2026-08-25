using NUnit.Framework;
using ThinWalls.Pathing;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class ThinDoorAccessPolicyTests
{
    [TestCase(true, false, true)]
    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(false, true, false)]
    public void PassageRequiresBothPhysicalAccessAndNoForbiddenRestriction(
        bool canPhysicallyPass,
        bool forbidden,
        bool expected)
    {
        Assert.That(
            ThinDoorAccessPolicy.PermitsPassage(canPhysicallyPass, forbidden),
            Is.EqualTo(expected));
    }

    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    public void EdgeReachabilityCanOnlyRefineAnAcceptedVanillaAnswer(
        bool vanillaAccepted,
        bool edgeGraphAccepted,
        bool expected)
    {
        Assert.That(
            ThinDoorAccessPolicy.RefineReachability(vanillaAccepted, edgeGraphAccepted),
            Is.EqualTo(expected));
    }

    [TestCase(false, true, true, true, true)]
    [TestCase(false, true, true, false, false)]
    [TestCase(false, true, false, true, false)]
    [TestCase(false, false, true, true, false)]
    [TestCase(true, true, true, true, false)]
    public void ADeliberateDoorRegionSplitIsRecoveredOnlyByAnActualDoorCrossingPath(
        bool vanillaAccepted,
        bool edgeGraphAccepted,
        bool nativePathFound,
        bool pathCrossesThinDoor,
        bool expected)
    {
        Assert.That(
            ThinDoorAccessPolicy.MayRecoverThinDoorRegionSplit(
                vanillaAccepted,
                edgeGraphAccepted,
                nativePathFound,
                pathCrossesThinDoor),
            Is.EqualTo(expected));
    }

    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    public void FriendlyTouchRefreshesOnlyForSpawnedPawnsActuallyCrossingTheOwnedEdge(
        bool pawnSpawned,
        bool crossesOwnedEdge,
        bool expected)
    {
        Assert.That(
            ThinDoorAccessPolicy.ShouldRefreshFriendlyTouch(pawnSpawned, crossesOwnedEdge),
            Is.EqualTo(expected));
    }

    [TestCase(90, true, 90)]
    [TestCase(1, true, 1)]
    [TestCase(90, false, 89)]
    [TestCase(1, false, 0)]
    public void OwnerCellOccupancyNeverRefreshesAndHoldOpenPreservesAPositiveCountdown(
        int previousTicks,
        bool holdOpen,
        int expected)
    {
        Assert.That(
            ThinDoorAccessPolicy.CorrectOwnerCellCountdown(previousTicks, holdOpen),
            Is.EqualTo(expected));
    }
}
