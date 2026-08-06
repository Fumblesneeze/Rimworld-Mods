using System.Drawing;
using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class MicrowaveCountertopTests
{
    [Test]
    public void Recovery_search_is_complete_unique_and_nearest_first()
    {
        var origin = new CountertopCell(2, 1);

        var cells = MicrowaveRecoveryPolicy.OrderedCells(origin, width: 9, height: 7).ToArray();

        Assert.That(cells, Has.Length.EqualTo(63));
        Assert.That(cells.Distinct().ToArray(), Has.Length.EqualTo(63));
        Assert.That(cells[0], Is.EqualTo(origin));
        Assert.That(cells, Does.Contain(new CountertopCell(8, 6)),
            "Recovery must search beyond the old four-cell local radius.");
        Assert.That(
            cells.Zip(cells.Skip(1), (left, right) =>
                DistanceSquared(origin, left) <= DistanceSquared(origin, right)),
            Is.All.True,
            "Recovery candidates must be ordered from nearest to farthest.");
    }

    [TestCase(true, false, true, false, true, true)]
    [TestCase(true, false, true, false, false, false)]
    [TestCase(false, false, true, false, true, false)]
    [TestCase(true, true, true, false, true, false)]
    [TestCase(true, false, false, false, true, false)]
    [TestCase(true, false, true, true, true, false)]
    public void Operational_policy_requires_spawn_power_integrity_and_live_support(
        bool spawned,
        bool forbidden,
        bool powered,
        bool brokenDown,
        bool supported,
        bool expected)
    {
        Assert.That(
            MicrowaveOperationalPolicy.Allows(
                spawned,
                forbidden,
                powered,
                brokenDown,
                supported),
            Is.EqualTo(expected));
    }

    private static int DistanceSquared(CountertopCell origin, CountertopCell candidate)
    {
        var x = candidate.X - origin.X;
        var z = candidate.Z - origin.Z;
        return (x * x) + (z * z);
    }

    [TestCase(true, true, (int)CountertopSurfaceKind.Eat, false, true)]
    [TestCase(true, true, (int)CountertopSurfaceKind.Item, false, true)]
    [TestCase(false, true, (int)CountertopSurfaceKind.Eat, false, false)]
    [TestCase(true, false, (int)CountertopSurfaceKind.Eat, false, false)]
    [TestCase(true, true, (int)CountertopSurfaceKind.None, false, false)]
    [TestCase(true, true, (int)CountertopSurfaceKind.Item, true, false)]
    public void Support_policy_is_capability_based_and_rejects_storage_or_incomplete_things(
        bool spawned,
        bool completedBuilding,
        int surface,
        bool storage,
        bool expected)
    {
        var facts = new MicrowaveSupportFacts(
            spawned,
            completedBuilding,
            (CountertopSurfaceKind)surface,
            storage);

        Assert.That(MicrowaveSupportPolicy.Allows(facts), Is.EqualTo(expected));
    }

    [Test]
    public void Interaction_cell_must_be_standable_and_not_obstruct_the_support()
    {
        var supportCells = new[] { new CountertopCell(10, 10), new CountertopCell(11, 10) };
        var supportInteractionCell = new CountertopCell(10, 9);

        Assert.Multiple(() =>
        {
            Assert.That(
                MicrowaveSupportPolicy.InteractionCellIsUsable(
                    supportCells,
                    supportInteractionCell,
                    new CountertopCell(11, 9),
                    inBounds: true,
                    standable: true),
                Is.True);
            Assert.That(
                MicrowaveSupportPolicy.InteractionCellIsUsable(
                    supportCells,
                    supportInteractionCell,
                    new CountertopCell(11, 10),
                    inBounds: true,
                    standable: true),
                Is.False);
            Assert.That(
                MicrowaveSupportPolicy.InteractionCellIsUsable(
                    supportCells,
                    supportInteractionCell,
                    supportInteractionCell,
                    inBounds: true,
                    standable: true),
                Is.False);
            Assert.That(
                MicrowaveSupportPolicy.InteractionCellIsUsable(
                    supportCells,
                    supportInteractionCell,
                    new CountertopCell(11, 9),
                    inBounds: false,
                    standable: true),
                Is.False);
            Assert.That(
                MicrowaveSupportPolicy.InteractionCellIsUsable(
                    supportCells,
                    supportInteractionCell,
                    new CountertopCell(11, 9),
                    inBounds: true,
                    standable: false),
                Is.False);
        });
    }

    [Test]
    public void Fallback_def_is_a_minifiable_rare_ticking_countertop_appliance_with_custom_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Patches",
            "Compatibility",
            "ThermodynamicsHotMeals.xml"));
        var def = document.Descendants("ThingDef")
            .Single(element => (string?)element.Element("defName") == "ImmersiveChefs_Microwave");
        var texturePath = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            "Things",
            "Building",
            "Microwave",
            "Microwave.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)def.Element("thingClass"), Is.EqualTo("ImmersiveChefs.Building_Microwave"));
            Assert.That((string?)def.Element("tickerType"), Is.EqualTo("Rare"));
            Assert.That((string?)def.Element("minifiedDef"), Is.EqualTo("MinifiedThing"));
            Assert.That((string?)def.Element("altitudeLayer"), Is.EqualTo("BuildingOnTop"));
            Assert.That((string?)def.Element("clearBuildingArea"), Is.EqualTo("false"));
            Assert.That((string?)def.Element("building")?.Element("isEdifice"), Is.EqualTo("false"));
            Assert.That(
                def.Element("blocksAltitudes")?.Elements("li").Select(element => element.Value),
                Does.Contain("BuildingOnTop"));
            Assert.That(
                def.Element("placeWorkers")?.Elements("li").Select(element => element.Value),
                Does.Contain("ImmersiveChefs.PlaceWorker_MicrowaveCountertop"));
            Assert.That(
                (string?)def.Element("graphicData")?.Element("texPath"),
                Is.EqualTo("Things/Building/Microwave/Microwave"));
            Assert.That(
                (string?)def.Element("graphicData")?.Element("graphicClass"),
                Is.EqualTo("Graphic_Single"));
            Assert.That(File.Exists(texturePath), Is.True);
        });

        if (!File.Exists(texturePath))
        {
            return;
        }

        using var bitmap = new Bitmap(texturePath);
        Assert.Multiple(() =>
        {
            Assert.That(bitmap.Width, Is.EqualTo(512));
            Assert.That(bitmap.Height, Is.EqualTo(512));
            Assert.That(bitmap.GetPixel(0, 0).A, Is.EqualTo(0));
            Assert.That(bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2).A, Is.GreaterThan(0));
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
