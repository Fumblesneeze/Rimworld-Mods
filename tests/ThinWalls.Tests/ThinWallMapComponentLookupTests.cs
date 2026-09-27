using NUnit.Framework;
using ThinWalls.Pathing;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class ThinWallMapComponentLookupTests
{
    [Test]
    public void RemovingAReplacedComponentDoesNotUnregisterTheCurrentMapComponent()
    {
        var map = new Map();
        map.events = new MapEvents(map);
        var original = new ThinWallMapComponent(map);
        var replacement = new ThinWallMapComponent(map);

        original.MapRemoved();
        Assert.That(ThinWallMapComponent.TryGet(map, out var current), Is.True);
        Assert.That(current, Is.SameAs(replacement));

        replacement.MapRemoved();
        Assert.That(ThinWallMapComponent.TryGet(map, out _), Is.False,
            "Removing the current component must release its map registration.");
        Assert.That(System.AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "0Harmony"), Is.False,
            "Lightweight lifecycle tests must not load Harmony or pretend to activate the mod.");
    }

    [Test]
    public void ComponentRegistrationResolvesItsOwnMapWithoutSearchingTheComponentList()
    {
        // These are lightweight map/event containers, not generated maps or loaded Def fixtures.
        var firstMap = new Map();
        firstMap.events = new MapEvents(firstMap);
        var secondMap = new Map();
        secondMap.events = new MapEvents(secondMap);
        var first = new ThinWallMapComponent(firstMap);
        var second = new ThinWallMapComponent(secondMap);

        Assert.Multiple(() =>
        {
            Assert.That(firstMap.components, Is.Empty);
            Assert.That(ThinWallMapComponent.TryGet(firstMap, out var foundFirst), Is.True);
            Assert.That(foundFirst, Is.SameAs(first));
            Assert.That(ThinWallMapComponent.TryGet(secondMap, out var foundSecond), Is.True);
            Assert.That(foundSecond, Is.SameAs(second));
        });
    }
}
