using NUnit.Framework;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndHost;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class ExpectedDiagnosticCatalogTests
{
    [Test]
    public void Selected_compiled_diagnostics_survive_real_host_plan_and_gateway_admission()
    {
        using var stage = new Stage();
        var result = new GatewayEndToEndBundleCatalog(selectedTestIds: new[] { "alpha.optional" })
            .Inspect(stage.Candidate, new[] { "brrainz.harmony", "ludeon.rimworld", "alpha.mod", "optional.mod", "fumblesneeze.rimworlddevgateway" });
        Assert.That(result.State, Is.EqualTo("loaded"), result.Failure?.Code + ": " + result.Failure?.Message);
        Assert.That(result.Source!.Tests.Select(test => test.Id), Is.EqualTo(new[] { "alpha.optional" }));
    }

    [TestCase("id")]
    [TestCase("severity")]
    [TestCase("message")]
    [TestCase("count")]
    [TestCase("cause")]
    [TestCase("removed")]
    [TestCase("old-schema")]
    public void Gateway_rejects_diagnostic_manifest_drift(string mode)
    {
        using var stage = new Stage(manifest =>
        {
            var test = manifest.Tests.Single(test => test.Id == "alpha.optional");
            var rule = test.ExpectedDiagnostics!.Single();
            switch (mode)
            {
                case "id": rule.Id = "foreign-id"; break;
                case "severity": rule.Severity = "ERROR"; break;
                case "message": rule.Message += " changed"; break;
                case "count": rule.Count = 1; break;
                case "cause": rule.InnerCauses[0] += " changed"; break;
                case "removed": test.ExpectedDiagnostics = null; break;
                case "old-schema": manifest.Schema = EndToEndBundleManifest.NativeXmlLoaderSchemaValue; break;
            }
        });
        var result = new GatewayEndToEndBundleCatalog(selectedTestIds: new[] { "alpha.optional" })
            .Inspect(stage.Candidate, new[] { "brrainz.harmony", "ludeon.rimworld", "alpha.mod", "optional.mod", "fumblesneeze.rimworlddevgateway" });
        Assert.That(result.State, Is.EqualTo("failed"));
        Assert.That(result.Failure!.Code, Is.EqualTo(mode == "old-schema" ? "manifest_diagnostics_invalid" : "compiled_test_manifest_mismatch"));
    }

    private sealed class Stage : IDisposable
    {
        private readonly string parent = Path.Combine(TestContext.CurrentContext.WorkDirectory, "expected-diagnostic-catalog");
        private readonly string root;
        public GatewayEndToEndManifestCandidate Candidate { get; }

        public Stage(Action<EndToEndBundleManifest>? mutate = null)
        {
            root = Path.Combine(parent, Guid.NewGuid().ToString("N"));
            string repository = FindRoot();
#if DEBUG
            const string configuration = "Debug";
#else
            const string configuration = "Release";
#endif
            string project = Path.Combine(repository, "tests", "Fixtures", "EndToEndHost.ValidFixtures");
            var metadata = EndToEndAssemblyMetadataReader.Read(Path.Combine(project, "bin", configuration, "net10.0", "EndToEndHost.ValidFixtures.dll"));
            var candidate = new EndToEndAssemblyCandidate(Path.Combine(project, "EndToEndHost.ValidFixtures.csproj"), "alpha.mod", metadata.AssemblyName, metadata);
            var plan = EndToEndBundlePlanner.Create(new[] { candidate },
                new[] { "brrainz.harmony", "ludeon.rimworld", "alpha.mod", "optional.mod" }, root, "1.7",
                new[] { "alpha.optional" }, Array.Empty<string>());
            var bundle = plan.OwnerStages.Single().Bundles.Single();
            mutate?.Invoke(bundle.Manifest);
            string stageDirectory = Path.Combine(root, "alpha.mod", "1.7", "DevEndToEndTests");
            Directory.CreateDirectory(stageDirectory);
            File.Copy(bundle.SourceAssemblyPath, Path.Combine(stageDirectory, bundle.AssemblyFileName));
            string manifestPath = Path.Combine(stageDirectory, bundle.ManifestFileName);
            File.WriteAllText(manifestPath, GatewayContractJson.Write(bundle.Manifest));
            File.WriteAllText(Path.Combine(stageDirectory, EndToEndStageMarker.FileName), GatewayContractJson.Write(new EndToEndStageMarker
            {
                OwnerPackageId = "alpha.mod", RimWorldVersion = "1.7", TransactionId = Guid.NewGuid().ToString("N"),
                Assemblies = new[] { bundle.AssemblyFileName }, Manifests = new[] { bundle.ManifestFileName }
            }));
            Candidate = new GatewayEndToEndManifestCandidate("alpha.mod", manifestPath);
        }

        public void Dispose()
        {
            string resolved = Path.GetFullPath(root);
            if (!string.Equals(Path.GetDirectoryName(resolved), Path.GetFullPath(parent), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refuse catalog fixture cleanup outside its exact parent.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, recursive: true);
        }
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln"))) return directory.FullName;
        throw new InvalidOperationException("Repository root was not found.");
    }
}
