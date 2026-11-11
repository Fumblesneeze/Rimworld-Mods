using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class EndToEndNativeXmlLoaderCatalogTests
{
    private static readonly string[] Active = { "brrainz.harmony", "ludeon.rimworld", "alpha.mod", EndToEndTestContract.GatewayPackageId };

    [TestCase(false, "alpha.legacy")]
    [TestCase(true, "alpha.base-a")]
    public void Coordinator_wrong_mode_never_reaches_execution_factory(bool legacy, string selected)
    {
        using var stage = new Stage();
        using var coordinator = GatewayEndToEndCoordinator.CreateEnabled(stage.SaveData,
            new Source(stage.Candidate), selectedTestIds: new[] { selected }, observedLegacyXmlDeserializer: legacy);
        var execution = new ExecutionProbe();
        coordinator.ConfigureExecution(execution, execution);
        coordinator.AttachSession("mismatch-fixture");
        Complete(coordinator);
        for (var i = 0; i < 5; i++) coordinator.Tick();
        Assert.That(coordinator.Snapshot.Failures.Select(failure => failure.Code), Does.Contain("xml_loader_mode_mismatch"));
        Assert.That(coordinator.Snapshot.Tests, Is.Empty);
        Assert.That(execution.Calls, Is.Zero, "No constructor or Arrange can be reached through the execution factory.");
    }

    [Test]
    public void No_compatible_mode_is_a_failed_zero_test_run()
    {
        using var stage = new Stage();
        var optionalGroup = new[] { "brrainz.harmony", "ludeon.rimworld", "alpha.mod", "optional.mod", EndToEndTestContract.GatewayPackageId };
        using var coordinator = GatewayEndToEndCoordinator.CreateEnabled(stage.SaveData,
            new Source(stage.Candidate, optionalGroup), observedLegacyXmlDeserializer: true);
        coordinator.AttachSession("empty-mode-fixture");
        Complete(coordinator);
        Assert.That(coordinator.Snapshot.Failures.Select(failure => failure.Code), Does.Contain("no_tests_admitted"));
        Assert.That(coordinator.Snapshot.Tests, Is.Empty);
    }

    [TestCase("unknown-schema", "manifest_schema_mismatch")]
    [TestCase("tampered-v2-mode", "compiled_test_manifest_mismatch")]
    [TestCase("v1-false-is-default", "compiled_test_manifest_mismatch")]
    [TestCase("hash", "assembly_hash_mismatch")]
    [TestCase("mvid", "assembly_mvid_mismatch")]
    public void Mode_support_preserves_full_bundle_validation(string mutation, string code)
    {
        using var stage = new Stage(manifest =>
        {
            if (mutation == "unknown-schema") manifest.Schema = "RimWorldDevGateway.EndToEndTests/v3";
            if (mutation is "tampered-v2-mode" or "v1-false-is-default")
                manifest.Tests.Single(test => test.Id == "alpha.legacy").LegacyXmlDeserializer = false;
            if (mutation == "v1-false-is-default") manifest.Schema = EndToEndBundleManifest.SchemaValue;
            if (mutation == "hash") manifest.AssemblySha256 = new string('0', 64);
            if (mutation == "mvid") manifest.ModuleVersionId = Guid.Empty.ToString("D");
        });
        var result = new GatewayEndToEndBundleCatalog(selectedTestIds: new[] { "alpha.base-a" }).Inspect(stage.Candidate, Active);
        Assert.That(result.Failure?.Code, Is.EqualTo(code));
        Assert.That(result.Source, Is.Null);
    }

    private sealed class ExecutionProbe : IGatewayEndToEndExecutionFactory, IGatewayEndToEndExecutionReadiness
    {
        public int Calls { get; private set; }
        public bool IsPlayableMapReady() => true;
        public IGatewayEndToEndExecutionMachine Create(IReadOnlyList<GatewayEndToEndRuntimeTestDescriptor> tests, string? sessionCredential, string artifactDirectory)
        {
            Calls++;
            throw new InvalidOperationException("Wrong-mode execution must not start.");
        }
    }

    [Test]
    public void Requested_legacy_mode_survives_contract_catalog_and_snapshot_projection()
    {
        using var stage = new Stage();
        var result = new GatewayEndToEndBundleCatalog(observedLegacyXmlDeserializer: true).Inspect(stage.Candidate, Active);
        Assert.That(result.State, Is.EqualTo("loaded"), result.Failure?.Message);
        var descriptor = result.Source!.Tests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Id, Is.EqualTo("alpha.legacy"));
            Assert.That(EndToEndTestContract.Describe(descriptor.TestType).LegacyXmlDeserializer, Is.True);
            Assert.That(descriptor.LegacyXmlDeserializer, Is.True);
            Assert.That(new GatewayEndToEndTestSnapshot(descriptor).RequestedXmlDeserializer, Is.EqualTo("legacy"));
        });
    }

    [TestCase(false, "alpha.base-a", "default")]
    [TestCase(true, "alpha.legacy", "legacy")]
    public void Coordinator_persists_observed_mode_and_admits_the_selected_matching_declaration(bool legacy, string id, string mode)
    {
        using var stage = new Stage();
        using var coordinator = GatewayEndToEndCoordinator.CreateEnabled(stage.SaveData,
            new Source(stage.Candidate), selectedTestIds: new[] { id }, observedLegacyXmlDeserializer: legacy);
        coordinator.AttachSession("mode-fixture");
        Complete(coordinator);
        Assert.Multiple(() =>
        {
            Assert.That(coordinator.Snapshot.Failures, Is.Empty);
            Assert.That(coordinator.Snapshot.Tests.Select(test => test.Id), Is.EqualTo(new[] { id }));
            Assert.That(coordinator.Snapshot.ObservedXmlDeserializer, Is.EqualTo(mode));
            Assert.That(File.ReadAllText(coordinator.ArtifactPath!), Does.Contain("\"ObservedXmlDeserializer\":\"" + mode + "\""));
        });
    }

    private static void Complete(GatewayEndToEndCoordinator coordinator)
    {
        Assert.That(SpinWait.SpinUntil(() =>
        {
            coordinator.Tick();
            return coordinator.PublishedSnapshot?.DiscoveryState.StartsWith("completed", StringComparison.Ordinal) == true;
        }, TimeSpan.FromSeconds(10)), Is.True, "Bounded coordinator discovery/persistence did not finish.");
    }

    private sealed class Source(GatewayEndToEndManifestCandidate candidate, IReadOnlyList<string>? active = null) : IGatewayEndToEndManifestSource
    {
        public IGatewayEndToEndManifestDiscoveryCursor BeginDiscovery() => new Cursor((active ?? Active)
            .Select(GatewayEndToEndManifestDiscoveryStep.ActivePackage)
            .Concat(new[] { GatewayEndToEndManifestDiscoveryStep.Candidate(candidate), GatewayEndToEndManifestDiscoveryStep.Complete() }).GetEnumerator());
        private sealed class Cursor(IEnumerator<GatewayEndToEndManifestDiscoveryStep> steps) : IGatewayEndToEndManifestDiscoveryCursor
        {
            public GatewayEndToEndManifestDiscoveryStep Advance() => steps.MoveNext() ? steps.Current : GatewayEndToEndManifestDiscoveryStep.Complete();
            public void Dispose() => steps.Dispose();
        }
    }

    [TestCase(false, "alpha.legacy", "legacy", "default")]
    [TestCase(true, "alpha.base-a", "default", "legacy")]
    public void Selected_mode_mismatch_fails_before_constructing_or_arranging_any_test(bool observedLegacy, string selected, string requested, string observed)
    {
        using var stage = new Stage();
        var result = new GatewayEndToEndBundleCatalog(observedLegacyXmlDeserializer: observedLegacy,
            selectedTestIds: new[] { selected }).Inspect(stage.Candidate, Active);
        Assert.Multiple(() =>
        {
            Assert.That(result.Failure?.Code, Is.EqualTo("xml_loader_mode_mismatch"));
            Assert.That(result.Failure?.Message ?? "", Does.Contain("requested '" + requested + "'").And.Contain("observed '" + observed + "'"));
            Assert.That(result.Source, Is.Null);
            Assert.That(result.AdmittedTestInstanceCount, Is.Zero);
        });
    }

    [Test]
    public void Version_one_cannot_smuggle_a_legacy_declaration()
    {
        using var stage = new Stage(manifest => manifest.Schema = EndToEndBundleManifest.SchemaValue);
        var result = new GatewayEndToEndBundleCatalog().Inspect(stage.Candidate, Active);
        Assert.That(result.Failure?.Code, Is.EqualTo("manifest_xml_loader_invalid"));
        Assert.That(result.Source, Is.Null);
    }

    [Test]
    public void Mode_bearing_bundle_admits_only_default_tests_in_default_process_without_constructing_legacy_fixture()
    {
        using var stage = new Stage();
        var result = new GatewayEndToEndBundleCatalog().Inspect(stage.Candidate, Active);
        Assert.That(result.State, Is.EqualTo("loaded"), result.Failure?.Message);
        Assert.That(result.Source!.Tests.Select(test => test.Id), Is.EqualTo(new[] { "alpha.base-a", "alpha.base-b" }));
        Assert.That(result.AdmittedTestInstanceCount, Is.Zero);
    }

    private sealed class Stage : IDisposable
    {
        private readonly string root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "native-xml-catalog", Guid.NewGuid().ToString("N"));
        public Stage(Action<EndToEndBundleManifest>? mutate = null)
        {
            var repository = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "RimWorldMods.sln"))) repository = repository.Parent;
            if (repository is null) throw new InvalidOperationException("Repository root missing.");
#if DEBUG
            const string configuration = "Debug";
#else
            const string configuration = "Release";
#endif
            var bytes = File.ReadAllBytes(Path.Combine(repository.FullName, "tests", "Fixtures", "EndToEndHost.ValidFixtures", "bin", configuration, "net480", "EndToEndHost.ValidFixtures.dll"));
            var assembly = Assembly.Load(bytes);
            var directory = Path.Combine(root, "alpha.mod", "1.7", "DevEndToEndTests");
            Directory.CreateDirectory(directory);
            const string file = "EndToEndHost.ValidFixtures.dll";
            const string manifestFile = "EndToEndHost.ValidFixtures.e2etests.json";
            File.WriteAllBytes(Path.Combine(directory, file), bytes);
            var manifest = new EndToEndBundleManifest
            {
                Schema = EndToEndBundleManifest.NativeXmlLoaderSchemaValue,
                OwnerPackageId = "alpha.mod", Assembly = file,
                AssemblyIdentity = GatewayEndToEndAssemblyIdentity.FormatDefinition(assembly.GetName()),
                ModuleVersionId = assembly.ManifestModule.ModuleVersionId.ToString("D"),
                AssemblyLength = bytes.LongLength,
                AssemblySha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                Dependencies = assembly.GetReferencedAssemblies().Select(reference => new EndToEndBundleDependency
                {
                    Name = reference.Name!, Version = reference.Version!.ToString(),
                    Culture = string.IsNullOrWhiteSpace(reference.CultureName) ? "neutral" : reference.CultureName,
                    KeyKind = "PublicKeyToken", PublicKeyOrToken = GatewayEndToEndAssemblyIdentity.FormatKey(reference.GetPublicKeyToken()),
                    Identity = GatewayEndToEndAssemblyIdentity.FormatReference(reference)
                }).ToArray(),
                Tests = assembly.GetTypes().Where(type => type.IsDefined(typeof(RimWorldEndToEndTestAttribute), false))
                    .Select(type =>
                    {
                        var declaration = type.GetCustomAttribute<RimWorldEndToEndTestAttribute>()!;
                        var descriptor = EndToEndTestContract.Describe(type);
                        return new EndToEndBundleTest
                        {
                            Id = descriptor.Id, TypeName = type.FullName!, OwnerPackageId = descriptor.OwnerPackageId,
                            ActivePackageIds = descriptor.ActivePackageIds.ToArray(),
                            LegacyXmlDeserializer = declaration.LegacyXmlDeserializer,
                            MaxFrames = descriptor.Deadline.MaxFrames, MaxGameTicks = descriptor.Deadline.MaxGameTicks,
                            MaxWallClockSeconds = (int)descriptor.Deadline.MaxWallClock.TotalSeconds
                        };
                    }).ToArray()
            };
            mutate?.Invoke(manifest);
            File.WriteAllText(Path.Combine(directory, manifestFile), GatewayContractJson.Write(manifest));
            File.WriteAllText(Path.Combine(directory, EndToEndStageMarker.FileName), GatewayContractJson.Write(new EndToEndStageMarker
            {
                OwnerPackageId = "alpha.mod", RimWorldVersion = "1.7", TransactionId = Guid.NewGuid().ToString("N"),
                Assemblies = new[] { file }, Manifests = new[] { manifestFile }
            }));
            Candidate = new GatewayEndToEndManifestCandidate("alpha.mod", Path.Combine(directory, manifestFile));
        }
        public GatewayEndToEndManifestCandidate Candidate { get; }
        public string SaveData => Path.Combine(root, "save-data");
        public void Dispose() => Directory.Delete(root, recursive: true);
    }
}
