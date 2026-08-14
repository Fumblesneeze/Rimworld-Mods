using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RealRuinsCorpus.Tests;

[TestFixture]
public sealed class BlueprintAnalyzerTests
{
    [Test]
    public void Analyze_measures_exact_table_seating_hidden_conduits_and_outdoor_utilities()
    {
        const string xml = """
            <snapshot version="1.6" width="40" height="30" biomeDef="TemperateForest">
              <cell x="10" z="10"><roof/><item def="Table2x2c" rot="0"/></cell>
              <cell x="9" z="10"><roof/><item def="DiningChair" rot="1"/></cell>
              <cell x="12" z="10"><roof/><item def="DiningChair" rot="3"/></cell>
              <cell x="10" z="9"><roof/><item def="DiningChair" rot="0"/></cell>
              <cell x="10" z="12"><roof/><item def="DiningChair" rot="2"/></cell>
              <cell x="3" z="3"><roof/><item def="Wall" actsAsWall="1"/><item def="PowerConduit"/></cell>
              <cell x="4" z="3"><roof/><item def="PowerConduit"/></cell>
              <cell x="20" z="20"><item def="ChemfuelPoweredGenerator" rot="0"/></cell>
              <cell x="24" z="20"><roof/><item def="WaterTowerS" rot="0"/></cell>
            </snapshot>
            """;

        using var compressed = Compress(xml);
        var result = BlueprintAnalyzer.Analyze(compressed, "fixture");

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(40));
            Assert.That(result.Height, Is.EqualTo(30));
            Assert.That(result.SerializedCells, Is.EqualTo(9));
            Assert.That(result.DiningTablesWithEnoughAdjacentChairs, Is.EqualTo(new PlacementMetric(1, 1)));
            Assert.That(result.PowerConduitsUnderWalls, Is.EqualTo(new PlacementMetric(2, 1)));
            Assert.That(result.FixedOutdoorUtilitiesRoofed, Is.EqualTo(new PlacementMetric(2, 1)));
        });
    }

    [Test]
    public void Analyze_rejects_a_DTD_before_reading_external_content()
    {
        const string xml = """
            <!DOCTYPE snapshot [<!ENTITY xxe SYSTEM "file:///definitely-not-readable">]>
            <snapshot width="40" height="30"><cell x="1" z="1"><item def="&xxe;"/></cell></snapshot>
            """;

        using var compressed = Compress(xml);
        Assert.That(
            () => BlueprintAnalyzer.Analyze(compressed, "untrusted"),
            Throws.InstanceOf<System.Xml.XmlException>());
    }

    [Test]
    public async Task Cli_rejects_an_invalid_output_format_with_usage_exit_code()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await CorpusCli.InvokeAsync(
            new[]
            {
                "analyze",
                "--metadata-limit", "5000",
                "--blueprint-limit", "2000",
                "--output-directory", "ignored",
                "--output", "yaml"
            },
            output,
            error);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(2));
            Assert.That(error.ToString(), Does.Contain("json or table"));
        });
    }

    private static MemoryStream Compress(string xml)
    {
        var result = new MemoryStream();
        using (var gzip = new GZipStream(result, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            var bytes = Encoding.UTF8.GetBytes(xml);
            gzip.Write(bytes, 0, bytes.Length);
        }

        result.Position = 0;
        return result;
    }
}
