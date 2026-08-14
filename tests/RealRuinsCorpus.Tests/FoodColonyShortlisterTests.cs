using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RealRuinsCorpus.Tests;

[TestFixture]
public sealed class FoodColonyShortlisterTests
{
    [Test]
    public async Task Shortlist_ranks_only_blueprints_with_residential_dining_and_kitchen_context()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "shortlist-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            WriteBlueprint(Path.Combine(root, "food-colony.bp"), FoodColonyXml());
            WriteBlueprint(Path.Combine(root, "warehouse.bp"), """
                <snapshot width="80" height="80">
                  <cell x="1" z="1"><item def="Wall" actsAsWall="1"/></cell>
                  <cell x="2" z="1"><item def="Shelf"/></cell>
                </snapshot>
                """);

            var result = await FoodColonyShortlister.ShortlistAsync(root, 12, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.BlueprintsInspected, Is.EqualTo(2));
                Assert.That(result.RejectedBlueprints, Is.EqualTo(0));
                Assert.That(result.EligibleBlueprints, Is.EqualTo(1));
                Assert.That(result.Candidates, Has.Count.EqualTo(1));
                Assert.That(result.Candidates[0].SourceId, Is.EqualTo("food-colony"));
                Assert.That(result.Candidates[0].Beds, Is.EqualTo(4));
                Assert.That(result.Candidates[0].DiningSeats, Is.EqualTo(4));
                Assert.That(result.Candidates[0].CookingWorkstations, Is.EqualTo(2));
                Assert.That(result.Candidates[0].FoodDistrictWidth, Is.LessThanOrEqualTo(16));
                Assert.That(result.Candidates[0].RelevantPlacements, Has.Count.EqualTo(12));
                Assert.That(result.Candidates[0].RelevantPlacements,
                    Has.Some.Matches<FoodColonyPlacement>(item =>
                        item.DefName == "ElectricStove" && item.X == 18 && item.Z == 16));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Shortlist_prefers_a_complete_showcase_scale_colony_over_a_bulk_barracks()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "shortlist-scale-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            WriteBlueprint(Path.Combine(root, "compact.bp"), FoodColonyXml(8));
            WriteBlueprint(Path.Combine(root, "bulk-barracks.bp"), FoodColonyXml(40));

            var result = await FoodColonyShortlister.ShortlistAsync(root, 12, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.Candidates, Has.Count.EqualTo(2));
                Assert.That(result.Candidates[0].SourceId, Is.EqualTo("compact"));
                Assert.That(result.Candidates[0].Score, Is.GreaterThan(result.Candidates[1].Score));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string FoodColonyXml(int beds = 4)
    {
        var xml = new StringBuilder("<snapshot width=\"40\" height=\"30\">");
        for (var x = 4; x <= 25; x++)
        {
            xml.Append($"<cell x=\"{x}\" z=\"4\"><roof/><item def=\"Wall\" actsAsWall=\"1\"/><item def=\"PowerConduit\"/></cell>");
            xml.Append($"<cell x=\"{x}\" z=\"18\"><roof/><item def=\"Wall\" actsAsWall=\"1\"/></cell>");
        }
        for (var z = 5; z < 18; z++)
        {
            xml.Append($"<cell x=\"4\" z=\"{z}\"><roof/><item def=\"Wall\" actsAsWall=\"1\"/></cell>");
            xml.Append($"<cell x=\"25\" z=\"{z}\"><roof/><item def=\"Wall\" actsAsWall=\"1\"/></cell>");
        }
        for (var index = 0; index < beds; index++)
        {
            var x = 2 + index % 18;
            var z = 20 + index / 18;
            xml.Append($"<cell x=\"{x}\" z=\"{z}\"><roof/><item def=\"Bed\"/></cell>");
        }
        xml.Append("<cell x=\"10\" z=\"9\"><roof/><item def=\"Table2x2c\"/></cell>");
        foreach (var cell in new[] { (9, 9), (12, 9), (10, 8), (10, 11) })
            xml.Append($"<cell x=\"{cell.Item1}\" z=\"{cell.Item2}\"><roof/><item def=\"DiningChair\"/></cell>");
        xml.Append("<cell x=\"18\" z=\"16\"><roof/><item def=\"ElectricStove\"/></cell>");
        xml.Append("<cell x=\"21\" z=\"16\"><roof/><item def=\"ButcherTable\"/></cell>");
        xml.Append("<cell x=\"23\" z=\"16\"><roof/><item def=\"Cooler\"/></cell>");
        xml.Append("</snapshot>");
        return xml.ToString();
    }

    private static void WriteBlueprint(string path, string xml)
    {
        using var file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.SmallestSize);
        var bytes = Encoding.UTF8.GetBytes(xml);
        gzip.Write(bytes, 0, bytes.Length);
    }
}
