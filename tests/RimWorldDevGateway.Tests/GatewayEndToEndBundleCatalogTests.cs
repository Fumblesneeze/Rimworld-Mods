using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndBundleCatalogTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string Configuration =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    private static readonly string[] BaseActivePackages =
    {
        "ludeon.rimworld",
        "brrainz.harmony",
        "alpha.mod",
        EndToEndTestContract.GatewayPackageId
    };

    [Test]
    public void Exact_active_group_loads_only_its_matching_tests_after_all_guards_pass()
    {
        using var stage = new CatalogStage();
        var loader = new RecordingAssemblyLoader();
        var catalog = new GatewayEndToEndBundleCatalog(loader);

        var result = catalog.Inspect(stage.Candidate, BaseActivePackages);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo("loaded"));
            Assert.That(result.Failure, Is.Null);
            Assert.That(result.Source, Is.Not.Null);
            Assert.That(result.Source!.Tests.Select(test => test.Id), Is.EqualTo(new[]
            {
                "alpha.base-a",
                "alpha.base-b"
            }));
            Assert.That(result.Source.AssemblyIdentity, Is.EqualTo(stage.Manifest.AssemblyIdentity));
            Assert.That(result.Source.AssemblySha256, Is.EqualTo(stage.Manifest.AssemblySha256));
            Assert.That(loader.LoadCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Mismatched_exact_group_is_skipped_without_reading_or_loading_the_assembly()
    {
        using var stage = new CatalogStage();
        var loader = new RecordingAssemblyLoader();
        var catalog = new GatewayEndToEndBundleCatalog(loader);
        var activePackages = BaseActivePackages
            .Take(BaseActivePackages.Length - 1)
            .Concat(new[] { "unexpected.mod", EndToEndTestContract.GatewayPackageId })
            .ToArray();

        var result = catalog.Inspect(stage.Candidate, activePackages);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo("skipped"));
            Assert.That(result.Source, Is.Null);
            Assert.That(loader.LoadCount, Is.Zero);
        });
    }

    [TestCase("hash", "assembly_hash_mismatch")]
    [TestCase("identity", "assembly_identity_mismatch")]
    [TestCase("mvid", "assembly_mvid_mismatch")]
    [TestCase("dependency", "dependency_identity_mismatch")]
    [TestCase("attribute", "compiled_test_manifest_mismatch")]
    public void Drift_is_rejected_before_any_test_can_be_instantiated(string drift, string expectedCode)
    {
        using var stage = new CatalogStage(manifest =>
        {
            switch (drift)
            {
                case "hash":
                    manifest.AssemblySha256 = new string('0', 64);
                    break;
                case "identity":
                    manifest.AssemblyIdentity += ".drift";
                    break;
                case "mvid":
                    manifest.ModuleVersionId = Guid.NewGuid().ToString("D");
                    break;
                case "dependency":
                    manifest.Dependencies[0].Identity += ".drift";
                    break;
                case "attribute":
                    manifest.Tests = manifest.Tests.Take(manifest.Tests.Length - 1).ToArray();
                    break;
            }
        });
        var loader = new RecordingAssemblyLoader();

        var result = new GatewayEndToEndBundleCatalog(loader).Inspect(stage.Candidate, BaseActivePackages);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo("failed"));
            Assert.That(result.Failure!.Code, Is.EqualTo(expectedCode));
            Assert.That(result.Source, Is.Null);
            Assert.That(result.AdmittedTestInstanceCount, Is.Zero);
        });
    }

    [Test]
    public void Inactive_or_wrong_containing_owner_is_rejected()
    {
        using var stage = new CatalogStage();
        var catalog = new GatewayEndToEndBundleCatalog();
        var wrongContainer = new GatewayEndToEndManifestCandidate("wrong.mod", stage.Candidate.ManifestPath);

        var wrongResult = catalog.Inspect(wrongContainer, BaseActivePackages);
        var inactiveResult = catalog.Inspect(
            stage.Candidate,
            new[] { "ludeon.rimworld", "brrainz.harmony", EndToEndTestContract.GatewayPackageId });

        Assert.Multiple(() =>
        {
            Assert.That(wrongResult.Failure!.Code, Is.EqualTo("manifest_path_invalid"));
            Assert.That(inactiveResult.Failure!.Code, Is.EqualTo("manifest_owner_inactive"));
        });
    }

    [Test]
    public void One_load_failure_is_isolated_and_a_later_bundle_still_loads()
    {
        using var stage = new CatalogStage();
        var missing = new GatewayEndToEndManifestCandidate(
            "alpha.mod",
            Path.Combine(Path.GetDirectoryName(stage.Candidate.ManifestPath)!, "A-Missing.e2etests.json"));
        var catalog = new GatewayEndToEndBundleCatalog();

        var results = catalog.InspectAll(new[] { missing, stage.Candidate }, BaseActivePackages);

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Count.EqualTo(2));
            Assert.That(results[0].State, Is.EqualTo("failed"));
            Assert.That(results[0].Failure!.Code, Is.EqualTo("manifest_missing"));
            Assert.That(results[1].State, Is.EqualTo("loaded"));
        });
    }

    [Test]
    public void Assembly_loader_exception_is_suppressed_to_a_fixed_failure_and_does_not_escape()
    {
        using var stage = new CatalogStage();
        var result = new GatewayEndToEndBundleCatalog(new ThrowingAssemblyLoader())
            .Inspect(stage.Candidate, BaseActivePackages);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo("failed"));
            Assert.That(result.Failure!.Code, Is.EqualTo("assembly_byte_load_failed"));
            Assert.That(result.Failure.Message, Does.Not.Contain("SECRET_FROM_ARBITRARY_EXCEPTION"));
        });
    }

    private static string FixtureAssemblyPath() => Path.Combine(
        RepositoryRoot,
        "tests",
        "Fixtures",
        "EndToEndHost.ValidFixtures",
        "bin",
        Configuration,
        "net480",
        "EndToEndHost.ValidFixtures.dll");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ImmersiveChefs.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find repository root.");
    }

    private sealed class CatalogStage : IDisposable
    {
        public CatalogStage(Action<EndToEndBundleManifest>? mutate = null)
        {
            Root = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "e2e-catalog",
                Guid.NewGuid().ToString("N"));
            var directory = Path.Combine(Root, "alpha.mod", "1.6", "DevEndToEndTests");
            Directory.CreateDirectory(directory);
            var sourceAssembly = FixtureAssemblyPath();
            var assemblyBytes = File.ReadAllBytes(sourceAssembly);
            var assembly = Assembly.Load(assemblyBytes);
            var assemblyFileName = "EndToEndHost.ValidFixtures.dll";
            var manifestFileName = "EndToEndHost.ValidFixtures.e2etests.json";
            File.WriteAllBytes(Path.Combine(directory, assemblyFileName), assemblyBytes);

            Manifest = CreateManifest(assembly, assemblyBytes.LongLength, assemblyFileName);
            mutate?.Invoke(Manifest);
            var manifestPath = Path.Combine(directory, manifestFileName);
            File.WriteAllText(manifestPath, GatewayContractJson.Write(Manifest));
            File.WriteAllText(
                Path.Combine(directory, EndToEndStageMarker.FileName),
                GatewayContractJson.Write(new EndToEndStageMarker
                {
                    OwnerPackageId = "alpha.mod",
                    RimWorldVersion = "1.6",
                    TransactionId = Guid.NewGuid().ToString("N"),
                    Assemblies = new[] { assemblyFileName },
                    Manifests = new[] { manifestFileName }
                }));
            Candidate = new GatewayEndToEndManifestCandidate("alpha.mod", manifestPath);
        }

        public string Root { get; }

        public EndToEndBundleManifest Manifest { get; }

        public GatewayEndToEndManifestCandidate Candidate { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private static EndToEndBundleManifest CreateManifest(
            Assembly assembly,
            long length,
            string assemblyFileName)
        {
            return new EndToEndBundleManifest
            {
                OwnerPackageId = "alpha.mod",
                Assembly = assemblyFileName,
                AssemblyIdentity = GatewayEndToEndAssemblyIdentity.FormatDefinition(assembly.GetName()),
                ModuleVersionId = assembly.ManifestModule.ModuleVersionId.ToString("D"),
                AssemblyLength = length,
                AssemblySha256 = ComputeSha256(File.ReadAllBytes(FixtureAssemblyPath())),
                Dependencies = assembly.GetReferencedAssemblies()
                    .OrderBy(reference => reference.Name, StringComparer.Ordinal)
                    .ThenBy(reference => reference.Version)
                    .Select(reference => new EndToEndBundleDependency
                    {
                        Name = reference.Name ?? string.Empty,
                        Version = reference.Version?.ToString() ?? "0.0.0.0",
                        Culture = string.IsNullOrWhiteSpace(reference.CultureName) ? "neutral" : reference.CultureName!,
                        KeyKind = "PublicKeyToken",
                        PublicKeyOrToken = GatewayEndToEndAssemblyIdentity.FormatKey(reference.GetPublicKeyToken()),
                        Identity = GatewayEndToEndAssemblyIdentity.FormatReference(reference)
                    })
                    .ToArray(),
                Tests = assembly.GetTypes()
                    .Where(type => type.GetCustomAttributes(typeof(RimWorldEndToEndTestAttribute), inherit: false).Length == 1)
                    .Select(EndToEndTestContract.Describe)
                    .OrderBy(test => test.Id, StringComparer.Ordinal)
                    .Select(test => new EndToEndBundleTest
                    {
                        Id = test.Id,
                        TypeName = test.TestType.FullName!,
                        OwnerPackageId = test.OwnerPackageId,
                        ActivePackageIds = test.ActivePackageIds.ToArray(),
                        MaxFrames = test.Deadline.MaxFrames,
                        MaxGameTicks = test.Deadline.MaxGameTicks,
                        MaxWallClockSeconds = (int)test.Deadline.MaxWallClock.TotalSeconds
                    })
                    .ToArray()
            };
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using var sha256 = SHA256.Create();
            return GatewayEndToEndAssemblyIdentity.FormatKey(sha256.ComputeHash(bytes));
        }
    }

    private sealed class RecordingAssemblyLoader : IGatewayEndToEndAssemblyLoader
    {
        public int LoadCount { get; private set; }

        public Assembly Load(byte[] assemblyBytes)
        {
            LoadCount++;
            return Assembly.Load(assemblyBytes);
        }
    }

    private sealed class ThrowingAssemblyLoader : IGatewayEndToEndAssemblyLoader
    {
        public Assembly Load(byte[] assemblyBytes) =>
            throw new SecretAssemblyLoadException("SECRET_FROM_ARBITRARY_EXCEPTION");
    }

    private sealed class SecretAssemblyLoadException : Exception
    {
        public SecretAssemblyLoadException(string message) : base(message)
        {
        }
    }
}
