using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class EndToEndNativeXmlLoaderTests
{
    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Repository root missing.");
        }
    }
    private static string FixturePath => Path.Combine(Root, "tests", "Fixtures", "EndToEndHost.ValidFixtures", "bin",
#if DEBUG
        "Debug",
#else
        "Release",
#endif
        "net480", "EndToEndHost.ValidFixtures.dll");

    [Test]
    public void Compiled_declaration_preserves_legacy_and_omitted_default_without_executing_fixture()
    {
        var metadata = EndToEndAssemblyMetadataReader.Read(FixturePath);
        Assert.That(metadata.Declarations.Single(test => test.Id == "alpha.base-a").LegacyXmlDeserializer, Is.False);
        Assert.That(metadata.Declarations.Single(test => test.Id == "alpha.legacy").LegacyXmlDeserializer, Is.True);
    }
    private static EndToEndAssemblyCandidate Candidate() => new(
        Path.Combine(Root, "tests", "Fixtures", "EndToEndHost.ValidFixtures", "EndToEndHost.ValidFixtures.csproj"),
        "alpha.mod", "EndToEndHost.ValidFixtures", EndToEndAssemblyMetadataReader.Read(FixturePath));
    [Test]
    public void Same_packages_in_two_modes_have_separate_groups_with_stable_default_identity()
    {
        var discovery = EndToEndDiscoveryValidator.DiscoverAndSelect(new[] { Candidate() }, Array.Empty<string>(), Array.Empty<string>());
        const string packages = "brrainz.harmony|ludeon.rimworld|alpha.mod";
        Assert.That(discovery.Groups.Single(group => group.GroupId == packages).Tests.Select(test => test.Id),
            Is.EqualTo(new[] { "alpha.base-a", "alpha.base-b" }));
        Assert.That(discovery.Groups.Single(group => group.GroupId == packages + "::legacy-xml").Tests.Select(test => test.Id),
            Is.EqualTo(new[] { "alpha.legacy" }));
        var selected = EndToEndDiscoveryValidator.DiscoverAndSelect(new[] { Candidate() }, Array.Empty<string>(), new[] { packages + "::legacy-xml" });
        Assert.That(selected.SelectedTests.Select(test => test.Id), Is.EqualTo(new[] { "alpha.legacy" }));
    }
    [Test]
    public void Legacy_selected_fixture_plan_uses_its_mode_key_and_preserves_file_identities()
    {
        const string owner = "fumblesneeze.xmltestowner", fixture = "fumblesneeze.xmltestfixture";
        var source = EndToEndAssemblyMetadataReader.Read(FixturePath);
        var packages = new[] { "brrainz.harmony", "ludeon.rimworld", owner, fixture };
        var declarations = source.Declarations.Where(test => test.Id is "alpha.base-a" or "alpha.legacy")
            .Select(test => new EndToEndMetadataDeclaration(test.TypeName, test.Id, owner, packages,
                test.MaxFrames, test.MaxGameTicks, test.MaxWallClockSeconds, test.IsConcrete,
                test.ImplementsContract, test.HasPublicParameterlessConstructor, test.LegacyXmlDeserializer));
        var metadata = new EndToEndAssemblyMetadata(source.AssemblyPath, source.AssemblyName, source.AssemblyVersion,
            source.AssemblyCulture, source.AssemblyPublicKey, source.ModuleVersionId, source.Length, source.Sha256,
            source.AssemblyReferences, declarations);
        string temporaryParent = Path.GetFullPath(Path.GetTempPath());
        string repository = Path.Combine(temporaryParent, "NativeXmlLoaderTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            string fixtureDirectory = Path.Combine(repository, "tests", "Fixtures", "XmlLoader");
            Directory.CreateDirectory(Path.Combine(fixtureDirectory, "About"));
            Directory.CreateDirectory(Path.Combine(fixtureDirectory, "Defs"));
            File.WriteAllText(Path.Combine(fixtureDirectory, "About", "About.xml"),
                $"<ModMetaData><packageId>{fixture}</packageId><supportedVersions><li>1.7</li></supportedVersions>" +
                $"<modDependencies><li><packageId>{owner}</packageId></li></modDependencies></ModMetaData>");
            string defsFile = Path.Combine(fixtureDirectory, "Defs", "Diagnostic.xml");
            File.WriteAllText(defsFile, "<Defs><MissingTestType><defName>Diagnostic</defName></MissingTestType></Defs>");
            File.WriteAllText(Path.Combine(fixtureDirectory, "fixture.json"), JsonSerializer.Serialize(new
            {
                schema = "RimWorldDevGateway/XmlFixture/v1", ownerPackageId = owner,
                packageId = fixture, rimWorldVersion = "1.7",
                packageInclude = new[] { "About/About.xml", "Defs/Diagnostic.xml" }
            }));
            string project = Path.Combine(repository, "tests", "Loader.EndToEnd", "Loader.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(project)!);
            File.WriteAllText(project, "<Project><PropertyGroup><RimWorldEndToEndTest>true</RimWorldEndToEndTest>" +
                $"<RimWorldEndToEndTestOwnerPackageId>{owner}</RimWorldEndToEndTestOwnerPackageId></PropertyGroup>" +
                "<ItemGroup><RimWorldEndToEndXmlFixture Include=\"../Fixtures/XmlLoader/fixture.json\" /></ItemGroup></Project>");

            var candidate = new EndToEndAssemblyCandidate(project, owner, source.AssemblyName, metadata);
            string modsRoot = Path.Combine(repository, "game", "Mods");
            var selected = EndToEndDiscoveryValidator.DiscoverAndSelect(new[] { candidate }, new[] { "alpha.legacy" }, Array.Empty<string>());
            var installed = packages.Take(3).ToArray();
            var plans = EndToEndProjectXmlFixtures.PlanSelected(repository, selected, installed, modsRoot, "1.7");
            string group = string.Join("|", packages) + "::legacy-xml";
            Assert.That(plans.Keys, Is.EqualTo(new[] { group }));
            var plan = plans[group].Single();
            Assert.That(plan.PackageId, Is.EqualTo(fixture));
            Assert.That(plan.Files.Select(file => file.RelativePath), Is.EqualTo(new[] { "About/About.xml", "Defs/Diagnostic.xml" }));
            var plannedDefs = plan.Files.Single(file => file.RelativePath == "Defs/Diagnostic.xml");
            Assert.That(plannedDefs.Sha256, Is.EqualTo(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(defsFile)))));
            Assert.That(plannedDefs.Length, Is.EqualTo(new FileInfo(defsFile).Length));
            Assert.DoesNotThrow(() => EndToEndDiscoveryValidator.ValidatePackageAvailability(selected, installed, plans));
            Assert.That(Directory.Exists(modsRoot), Is.False);
        }
        finally
        {
            string resolved = Path.GetFullPath(repository);
            if (!string.Equals(Path.GetDirectoryName(resolved), Path.TrimEndingDirectorySeparator(temporaryParent), StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(resolved).StartsWith("NativeXmlLoaderTests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unsafe XML loader test cleanup path.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, recursive: true);
        }
    }
    [TestCase(false)]
    [TestCase(true)]
    public void Only_mode_bearing_bundles_require_v2_and_preserve_each_test_mode(bool includeLegacy)
    {
        var candidate = Candidate();
        // This case isolates the v1/v2 loader contract; diagnostic-bearing v3 is covered separately.
        {
            var source = candidate.Metadata;
            candidate = new EndToEndAssemblyCandidate(candidate.ProjectPath, candidate.ExpectedOwnerPackageId, candidate.ExpectedAssemblyName,
                new EndToEndAssemblyMetadata(source.AssemblyPath, source.AssemblyName, source.AssemblyVersion,
                    source.AssemblyCulture, source.AssemblyPublicKey, source.ModuleVersionId, source.Length, source.Sha256,
                    source.AssemblyReferences, source.Declarations.Where(test => test.ExpectedDiagnostics.Count == 0 && (includeLegacy || !test.LegacyXmlDeserializer))));
        }
        var plan = EndToEndBundlePlanner.Create(new[] { candidate }, new[] { "brrainz.harmony", "ludeon.rimworld", "alpha.mod" },
            Path.Combine(Path.GetTempPath(), "NativeXmlLoaderPlan", Guid.NewGuid().ToString("N"), "Mods"), "1.7",
            new[] { "alpha.base-a" }, Array.Empty<string>());
        using var json = JsonDocument.Parse(plan.OwnerStages.Single().Bundles.Single().ManifestJson);
        Assert.That(json.RootElement.GetProperty("schema").GetString(), Is.EqualTo("RimWorldDevGateway.EndToEndTests/" + (includeLegacy ? "v2" : "v1")));
        var tests = json.RootElement.GetProperty("tests").EnumerateArray().ToArray();
        Assert.That(tests.Single(test => test.GetProperty("id").GetString() == "alpha.base-a").TryGetProperty("legacyXmlDeserializer", out _), Is.False);
        if (includeLegacy)
            Assert.That(tests.Single(test => test.GetProperty("id").GetString() == "alpha.legacy").GetProperty("legacyXmlDeserializer").GetBoolean(), Is.True);
    }
    [Test]
    public void Unsupported_runtime_rejects_selected_legacy_before_fixture_or_stage_planning()
    {
        var destination = Path.Combine(Path.GetTempPath(), "NativeXmlLoaderPlan", Guid.NewGuid().ToString("N"), "Mods");
        var packages = new[] { "brrainz.harmony", "ludeon.rimworld", "alpha.mod" };
        Assert.DoesNotThrow(() => EndToEndBundlePlanner.Create(new[] { Candidate() }, packages, destination, "1.6",
            new[] { "alpha.base-a" }, Array.Empty<string>()));
        var error = Assert.Throws<EndToEndDiscoveryException>(() => EndToEndBundlePlanner.Create(new[] { Candidate() }, packages,
            destination, "1.6", new[] { "alpha.legacy" }, Array.Empty<string>()));
        Assert.That(error!.Message, Does.Contain("1.7"));
        Assert.That(Directory.Exists(destination), Is.False);
    }
}
