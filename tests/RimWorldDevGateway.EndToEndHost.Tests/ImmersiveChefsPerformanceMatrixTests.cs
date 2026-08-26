using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class ImmersiveChefsPerformanceMatrixTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string Configuration =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    [Test]
    public void Product_fixture_honors_runner_dependency_path_overrides()
    {
        var project = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "tests",
            "ImmersiveChefs.PerformanceTests",
            "ImmersiveChefs.PerformanceTests.csproj"));
        var properties = project.Root?.Elements("PropertyGroup").Elements()
            .ToDictionary(element => element.Name.LocalName, element => element.Value, StringComparer.Ordinal);
        var hintPaths = project.Root?.Elements("ItemGroup").Elements("Reference").Elements("HintPath")
            .Select(element => element.Value)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(properties?["RimWorldPath"], Is.EqualTo("$(DefaultRimWorldPath)"));
            Assert.That(properties?["SteamModContentFolder"], Is.EqualTo("$(DefaultSteamModContentFolder)"));
            Assert.That(properties?["HarmonyAssemblyPath"], Does.StartWith("$(SteamModContentFolder)"));
            Assert.That(hintPaths, Does.Contain("$(RimWorldPath)\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll"));
            Assert.That(hintPaths, Does.Contain("$(RimWorldPath)\\RimWorldWin64_Data\\Managed\\UnityEngine.CoreModule.dll"));
        });
    }

    [Test]
    public void Product_fixture_declares_complete_exact_optional_comparison_families()
    {
        var candidate = BuildProductPerformanceCandidate();
        var discovery = PerformanceDiscoveryValidator.ValidateAndGroup(
            new[] { candidate },
            RequiredPackageCatalog());

        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["immersive-chefs.processor-dubs"] =
            [
                "brrainz.harmony", "ludeon.rimworld", "astryl.circinus", "imranfish.xmlextensions",
                "syrchalis.processor.framework", "dubwise.dubsbadhygiene",
                "fumblesneeze.immersivechefs"
            ],
            ["immersive-chefs.guest-service"] =
            [
                "brrainz.harmony", "ludeon.rimworld", "astryl.circinus", "imranfish.xmlextensions",
                "orion.hospitality", "orion.cashregister", "orion.gastronomy",
                "avilmask.commonsense", "fumblesneeze.immersivechefs"
            ],
            ["immersive-chefs.variety-vnpe-material-dlc"] =
            [
                "brrainz.harmony", "ludeon.rimworld", "astryl.circinus", "imranfish.xmlextensions",
                "ludeon.rimworld.royalty", "ludeon.rimworld.biotech", "argon.corelib",
                "oskarpotocki.vanillafactionsexpanded.core", "argon.expandedmaterials.masonry",
                "argon.expandedmaterials.metals", "evyatar108.varietymattersimprovedredux",
                "vanillaexpanded.vanillafoodvarietyexpanded", "vanillaexpanded.vnutriente",
                "fumblesneeze.immersivechefs"
            ],
            ["immersive-chefs.all-supported"] =
            [
                "brrainz.harmony", "ludeon.rimworld", "astryl.circinus", "imranfish.xmlextensions",
                "ludeon.rimworld.royalty", "ludeon.rimworld.biotech", "argon.corelib",
                "oskarpotocki.vanillafactionsexpanded.core", "syrchalis.processor.framework",
                "argon.expandedmaterials.masonry", "argon.expandedmaterials.metals",
                "dubwise.dubsbadhygiene", "orion.hospitality", "orion.cashregister",
                "orion.gastronomy", "avilmask.commonsense",
                "evyatar108.varietymattersimprovedredux",
                "vanillaexpanded.vanillafoodvarietyexpanded", "vanillaexpanded.vnutriente",
                "fumblesneeze.immersivechefs"
            ]
        };

        foreach (var item in expected)
        {
            var family = discovery.Groups.SelectMany(group => group.Benchmarks)
                .Where(benchmark => string.Equals(benchmark.ComparisonId, item.Key, StringComparison.Ordinal))
                .OrderBy(benchmark => benchmark.EvidenceLens)
                .ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(family, Has.Length.EqualTo(3), item.Key);
                Assert.That(family.Select(benchmark => benchmark.EvidenceLens), Is.EqualTo(new[]
                {
                    0, 1, 2
                }), item.Key);
                Assert.That(family.Select(benchmark => benchmark.ActivePackageIds),
                    Is.All.EqualTo(item.Value), item.Key);
                Assert.That(family.SelectMany(benchmark => benchmark.ThroughputCheckpoints)
                        .Select(checkpoint => checkpoint.Id),
                    Does.Contain("optional-group-branches"), item.Key);
                Assert.That(family.SelectMany(benchmark => benchmark.ThroughputCheckpoints)
                        .Select(checkpoint => checkpoint.Id),
                    Does.Contain(item.Key + "-branch"), item.Key);
                foreach (var benchmark in family)
                {
                    Assert.That(benchmark.ThroughputCheckpoints.Select(checkpoint => checkpoint.Id),
                        Is.SupersetOf(BaseCheckpointIds), benchmark.Id);
                    if (item.Key is "immersive-chefs.processor-dubs" or "immersive-chefs.all-supported")
                        Assert.That(benchmark.ThroughputCheckpoints.Select(checkpoint => checkpoint.Id),
                            Is.SupersetOf(ProcessorDubsCheckpointIds), benchmark.Id);
                    if (item.Key is "immersive-chefs.guest-service" or "immersive-chefs.all-supported")
                        Assert.That(benchmark.ThroughputCheckpoints.Select(checkpoint => checkpoint.Id),
                            Is.SupersetOf(GuestServiceCheckpointIds), benchmark.Id);
                }
            });
        }
    }

    private static readonly string[] BaseCheckpointIds =
    [
        "native-map-ticks", "observer-scans", "meals-produced", "simple-meals-produced",
        "fine-meals-produced", "lavish-meals-produced", "assisted-cooking-sessions", "ware-cleaned",
        "domestic-dishwasher-cycles", "industrial-dishwasher-cycles", "map-meals-ingested",
        "animal-meals-ingested", "patients-fed", "caravan-meals-ingested", "microwave-reheats"
    ];

    private static readonly string[] ProcessorDubsCheckpointIds =
    [
        "processor-dubs-domestic-cycles",
        "processor-dubs-industrial-cycles",
        "processor-dubs-water-milliliters"
    ];

    private static readonly string[] GuestServiceCheckpointIds =
    [
        "guest-service-orders-served",
        "guest-service-colony-settings-returned",
        "guest-service-gastronomy-clearing-owned"
    ];

    private static PerformanceAssemblyCandidate BuildProductPerformanceCandidate()
    {
        var project = Path.Combine(
            RepositoryRoot, "tests", "ImmersiveChefs.PerformanceTests", "ImmersiveChefs.PerformanceTests.csproj");
        var assembly = Path.Combine(
            RepositoryRoot, "tests", "ImmersiveChefs.PerformanceTests", "bin", Configuration,
            "net480", "ImmersiveChefs.PerformanceTests.dll");
        Assert.That(File.Exists(assembly), Is.True,
            "Build ImmersiveChefs.PerformanceTests before running the metadata contract.");
        return new PerformanceAssemblyCandidate(
            project,
            "fumblesneeze.immersivechefs",
            "ImmersiveChefs.PerformanceTests",
            PerformanceAssemblyMetadataReader.Read(assembly));
    }

    private static string[] RequiredPackageCatalog() =>
    [
        "brrainz.harmony", "ludeon.rimworld", "astryl.circinus", "imranfish.xmlextensions",
        "syrchalis.processor.framework", "dubwise.dubsbadhygiene",
        "orion.hospitality", "orion.cashregister", "orion.gastronomy", "avilmask.commonsense",
        "ludeon.rimworld.royalty", "ludeon.rimworld.biotech", "argon.corelib",
        "oskarpotocki.vanillafactionsexpanded.core", "argon.expandedmaterials.metals",
        "argon.expandedmaterials.masonry", "evyatar108.varietymattersimprovedredux",
        "vanillaexpanded.vanillafoodvarietyexpanded", "vanillaexpanded.vnutriente",
        "fumblesneeze.immersivechefs", "fumblesneeze.rimworlddevgateway"
    ];

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
