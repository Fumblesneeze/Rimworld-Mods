using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;

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
        "brrainz.harmony",
        "ludeon.rimworld",
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
    public void Exact_performance_bundle_is_admitted_as_a_lazy_E2E_adapter_with_full_descriptor()
    {
        GC.KeepAlive(typeof(UnityEngine.Vector3).Assembly);
        using var stage = new PerformanceCatalogStage();
        var result = new GatewayEndToEndBundleCatalog().Inspect(
            stage.Candidate,
            new[]
            {
                "brrainz.harmony",
                "ludeon.rimworld",
                PerformanceTestContract.CircinusPackageId,
                EndToEndTestContract.GatewayPackageId
            });

        Assert.That(result.State, Is.EqualTo("loaded"), result.Failure?.Message);
        var tests = result.Source!.Tests;
        var instrumented = tests.Single(test => test.Id.EndsWith(".instrumented", StringComparison.Ordinal));
        Assert.Multiple(() =>
        {
            Assert.That(tests, Has.Count.EqualTo(3));
            Assert.That(tests, Has.All.Property(nameof(GatewayEndToEndRuntimeTestDescriptor.IsPerformance)).True);
            Assert.That(instrumented.PerformanceDescriptor, Is.Not.Null);
            Assert.That(instrumented.PerformanceDescriptor!.MeasuredSubjectPackageId,
                Is.EqualTo(EndToEndTestContract.GatewayPackageId));
            Assert.That(instrumented.PerformanceDescriptor.EvidenceLens,
                Is.EqualTo(PerformanceEvidenceLens.ProductInstrumented));
            Assert.That(instrumented.CreateTest(), Is.AssignableTo<IRimWorldEndToEndTest>());
        });
    }

    [Test]
    public void Filtered_performance_manifest_admits_only_listed_compiled_benchmarks()
    {
        GC.KeepAlive(typeof(UnityEngine.Vector3).Assembly);
        using var stage = new PerformanceCatalogStage(manifestFilter: test =>
            test.Id.StartsWith("gateway.immersive-chefs-neutral-", StringComparison.Ordinal));
        var result = new GatewayEndToEndBundleCatalog().Inspect(
            stage.Candidate,
            new[]
            {
                "brrainz.harmony",
                "ludeon.rimworld",
                PerformanceTestContract.CircinusPackageId,
                "imranfish.xmlextensions",
                EndToEndTestContract.GatewayPackageId
            });

        Assert.That(result.State, Is.EqualTo("loaded"), result.Failure?.Message);
        Assert.Multiple(() =>
        {
            Assert.That(result.Source!.Tests.Select(test => test.Id), Is.EqualTo(new[]
            {
                "gateway.immersive-chefs-neutral-control"
            }));
            Assert.That(result.Source.Tests, Has.All.Property(nameof(GatewayEndToEndRuntimeTestDescriptor.IsPerformance)).True);
        });
    }

    [Test]
    public void Filtered_performance_manifest_rejects_an_incomplete_evidence_lens_family()
    {
        GC.KeepAlive(typeof(UnityEngine.Vector3).Assembly);
        using var stage = new PerformanceCatalogStage(manifestFilter: test =>
            test.Id.StartsWith("gateway.immersive-chefs-neutral-", StringComparison.Ordinal) &&
            !test.Id.EndsWith(".armed-disabled", StringComparison.Ordinal));

        var result = new GatewayEndToEndBundleCatalog().Inspect(
            stage.Candidate,
            new[]
            {
                "brrainz.harmony",
                "ludeon.rimworld",
                PerformanceTestContract.CircinusPackageId,
                "imranfish.xmlextensions",
                EndToEndTestContract.GatewayPackageId
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo("failed"));
            Assert.That(result.Failure!.Code, Is.EqualTo("compiled_test_manifest_mismatch"));
            Assert.That(result.Source, Is.Null);
        });
    }

    [Test]
    public void Filtered_ordinary_product_family_does_not_require_the_explicit_absent_diagnostic()
    {
        GC.KeepAlive(typeof(UnityEngine.Vector3).Assembly);
        using var stage = new PerformanceCatalogStage(manifestFilter: test =>
            test.Id.StartsWith("gateway.immersive-chefs-neutral-present", StringComparison.Ordinal));

        var result = new GatewayEndToEndBundleCatalog().Inspect(
            stage.Candidate,
            new[]
            {
                "brrainz.harmony",
                "ludeon.rimworld",
                PerformanceTestContract.CircinusPackageId,
                "imranfish.xmlextensions",
                "fumblesneeze.immersivechefs",
                EndToEndTestContract.GatewayPackageId
            });

        Assert.That(result.State, Is.EqualTo("loaded"), result.Failure?.Message);
        Assert.Multiple(() =>
        {
            Assert.That(result.Source, Is.Not.Null);
            Assert.That(result.Source!.Tests.Select(test => test.Id), Is.EqualTo(new[]
            {
                "gateway.immersive-chefs-neutral-present.armed-disabled",
                "gateway.immersive-chefs-neutral-present.disarmed",
                "gateway.immersive-chefs-neutral-present.instrumented"
            }));
            Assert.That(result.Source.Tests, Has.All.Property(
                nameof(GatewayEndToEndRuntimeTestDescriptor.IsPerformance)).True);
        });
    }

    [Test]
    public void Exact_Dpa_performance_bundle_is_admitted_only_as_a_noncanonical_diagnostic()
    {
        GC.KeepAlive(typeof(UnityEngine.Vector3).Assembly);
        const string selector = "Verse.Map::MapPreTick()";
        using var stage = new PerformanceCatalogStage(dpaDiagnostic: true, selector);
        var result = new GatewayEndToEndBundleCatalog().Inspect(
            stage.Candidate,
            new[]
            {
                "brrainz.harmony",
                "ludeon.rimworld",
                PerformanceTestContract.DpaPackageId,
                EndToEndTestContract.GatewayPackageId
            });

        Assert.That(result.State, Is.EqualTo("loaded"), result.Failure?.Message);
        Assert.Multiple(() =>
        {
            Assert.That(result.Source!.Tests, Has.Count.EqualTo(3));
            Assert.That(result.Source.Tests.Select(test => test.PerformanceDescriptor!.Profiler),
                Is.All.EqualTo(PerformanceProfilerKind.DpaDiagnostic));
            Assert.That(result.Source.Tests.SelectMany(test => test.PerformanceDescriptor!.ActivePackageIds),
                Does.Not.Contain(PerformanceTestContract.CircinusPackageId));
            Assert.That(result.Source.Tests.Select(test => test.PerformanceDescriptor!.MethodSelectors.Single().Value),
                Is.All.EqualTo(selector));
            Assert.That(result.Source.Tests.Select(test => test.PerformanceDescriptor!.Repetitions),
                Is.All.EqualTo(1));
        });
    }

    [Test]
    public void Core_before_declared_harmony_is_rejected_before_loading_the_assembly()
    {
        using var stage = new CatalogStage(manifest =>
        {
            foreach (var test in manifest.Tests)
            {
                test.ActivePackageIds = new[] { "ludeon.rimworld", "brrainz.harmony", "alpha.mod" };
            }
        });
        var loader = new RecordingAssemblyLoader();

        var result = new GatewayEndToEndBundleCatalog(loader).Inspect(
            stage.Candidate,
            new[] { "ludeon.rimworld", "brrainz.harmony", "alpha.mod", EndToEndTestContract.GatewayPackageId });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo("failed"));
            Assert.That(result.Failure!.Code, Is.EqualTo("manifest_tests_invalid"));
            Assert.That(loader.LoadCount, Is.Zero);
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
    public void Ordinary_E2E_manifest_cannot_omit_a_compiled_test()
    {
        using var stage = new CatalogStage(manifest =>
            manifest.Tests = manifest.Tests.Take(manifest.Tests.Length - 1).ToArray());
        var loader = new RecordingAssemblyLoader();

        var result = new GatewayEndToEndBundleCatalog(loader).Inspect(stage.Candidate, BaseActivePackages);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo("failed"));
            Assert.That(result.Failure!.Code, Is.EqualTo("compiled_test_manifest_mismatch"));
            Assert.That(result.Source, Is.Null);
            Assert.That(result.AdmittedTestInstanceCount, Is.Zero);
        });
    }

    [Test]
    public void Compiled_test_discovery_enforces_the_published_exact_and_over_ceiling()
    {
        var attributedType = typeof(CatalogPerformanceBenchmark);
        var exact = Enumerable.Repeat(attributedType, PerformanceTestContract.MaximumBenchmarks);
        var over = Enumerable.Repeat(attributedType, PerformanceTestContract.MaximumBenchmarks + 1);

        Assert.Multiple(() =>
        {
            Assert.That(GatewayEndToEndBundleCatalog.BoundedCompiledTestTypes(exact),
                Has.Count.EqualTo(PerformanceTestContract.MaximumBenchmarks));
            Assert.That(() => GatewayEndToEndBundleCatalog.BoundedCompiledTestTypes(over),
                Throws.Exception.With.Message.Contains(PerformanceTestContract.MaximumBenchmarks.ToString()));
        });
    }

    [Test]
    public void Compiled_performance_ids_reject_case_only_duplicates_without_changing_ordinary_ids()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => GatewayEndToEndBundleCatalog.ValidateCompiledTestIds(
                new[] { "Ordinary", "ordinary" },
                Array.Empty<string>()), Throws.Nothing);
            Assert.That(() => GatewayEndToEndBundleCatalog.ValidateCompiledTestIds(
                    new[] { "Benchmark", "benchmark" },
                    new[] { "Benchmark", "benchmark" }),
                Throws.Exception.With.Message.Contains("case-insensitive duplicate"));
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

    private static string PerformanceFixtureAssemblyPath() => Path.Combine(
        RepositoryRoot,
        "tests",
        "RimWorldDevGateway.PerformanceTests",
        "bin",
        Configuration,
        "net480",
        "RimWorldDevGateway.PerformanceTests.dll");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln")))
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

    private sealed class PerformanceCatalogStage : IDisposable
    {
        public PerformanceCatalogStage(
            bool dpaDiagnostic = false,
            string? diagnosticSelector = null,
            Func<EndToEndBundleTest, bool>? manifestFilter = null)
        {
            Root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-catalog", Guid.NewGuid().ToString("N"));
            var owner = EndToEndTestContract.GatewayPackageId;
            var directory = Path.Combine(Root, owner, "1.6", "DevEndToEndTests");
            Directory.CreateDirectory(directory);
            var bytes = File.ReadAllBytes(PerformanceFixtureAssemblyPath());
            var assembly = Assembly.Load(bytes);
            const string assemblyFile = "RimWorldDevGateway.PerformanceTests.dll";
            const string manifestFile = "RimWorldDevGateway.PerformanceTests.e2etests.json";
            File.WriteAllBytes(Path.Combine(directory, assemblyFile), bytes);
            var descriptors = assembly.GetTypes()
                .Where(type => type.GetCustomAttributes(typeof(RimWorldPerformanceTestAttribute), false).Length == 1)
                .Select(PerformanceTestContract.Describe)
                .OrderBy(item => item.Id, StringComparer.Ordinal)
                .ToArray();
            var manifest = new EndToEndBundleManifest
            {
                OwnerPackageId = owner,
                Assembly = assemblyFile,
                AssemblyIdentity = GatewayEndToEndAssemblyIdentity.FormatDefinition(assembly.GetName()),
                ModuleVersionId = assembly.ManifestModule.ModuleVersionId.ToString("D"),
                AssemblyLength = bytes.LongLength,
                AssemblySha256 = ComputeSha256(bytes),
                Dependencies = assembly.GetReferencedAssemblies().OrderBy(reference => reference.Name, StringComparer.Ordinal)
                    .ThenBy(reference => reference.Version)
                    .Select(reference => new EndToEndBundleDependency
                    {
                        Name = reference.Name ?? string.Empty,
                        Version = reference.Version?.ToString() ?? "0.0.0.0",
                        Culture = string.IsNullOrWhiteSpace(reference.CultureName) ? "neutral" : reference.CultureName!,
                        KeyKind = "PublicKeyToken",
                        PublicKeyOrToken = GatewayEndToEndAssemblyIdentity.FormatKey(reference.GetPublicKeyToken()),
                        Identity = GatewayEndToEndAssemblyIdentity.FormatReference(reference)
                    }).ToArray(),
                Tests = descriptors.Select(descriptor => ToManifest(
                        descriptor, dpaDiagnostic, diagnosticSelector))
                    .Where(manifestFilter ?? (_ => true))
                    .ToArray()
            };
            var manifestPath = Path.Combine(directory, manifestFile);
            File.WriteAllText(manifestPath, GatewayContractJson.Write(manifest));
            File.WriteAllText(Path.Combine(directory, EndToEndStageMarker.FileName), GatewayContractJson.Write(
                new EndToEndStageMarker
                {
                    OwnerPackageId = owner,
                    RimWorldVersion = "1.6",
                    TransactionId = Guid.NewGuid().ToString("N"),
                    Assemblies = new[] { assemblyFile },
                    Manifests = new[] { manifestFile }
                }));
            Candidate = new GatewayEndToEndManifestCandidate(owner, manifestPath);
        }

        public string Root { get; }
        public GatewayEndToEndManifestCandidate Candidate { get; }
        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }

        private static EndToEndBundleTest ToManifest(
            PerformanceTestDescriptor descriptor,
            bool dpaDiagnostic,
            string? diagnosticSelector)
        {
            var deadline = PerformanceBundleDeadline.Calculate(
                descriptor.WarmUpTicks,
                descriptor.SampleTicks);
            return new EndToEndBundleTest
            {
                Id = descriptor.Id,
                TypeName = descriptor.TestType.FullName!,
                OwnerPackageId = descriptor.StagingOwnerPackageId,
                ActivePackageIds = (dpaDiagnostic
                    ? PerformanceTestContract.ForDpaDiagnostic(descriptor, diagnosticSelector!).ActivePackageIds
                    : descriptor.ActivePackageIds).ToArray(),
                MaxFrames = deadline.MaxFrames,
                MaxGameTicks = deadline.MaxGameTicks,
                MaxWallClockSeconds = deadline.MaxWallClockSeconds,
                Kind = EndToEndBundleTest.PerformanceKind,
                MeasuredSubjectPackageId = descriptor.MeasuredSubjectPackageId,
                WorkloadVersion = descriptor.WorkloadVersion,
                ComparisonId = descriptor.ComparisonId,
                WarmUpTicks = descriptor.WarmUpTicks,
                SampleTicks = descriptor.SampleTicks,
                GameSpeed = (int)descriptor.GameSpeed,
                Repetitions = dpaDiagnostic ? 1 : descriptor.Repetitions,
                EvidenceLens = (int)descriptor.EvidenceLens,
                ProductAbsentControlId = descriptor.ProductAbsentControlId,
                MethodSelectors = (dpaDiagnostic
                    ? PerformanceTestContract.ForDpaDiagnostic(descriptor, diagnosticSelector!).MethodSelectors
                    : descriptor.MethodSelectors).Select(selector => new PerformanceBundleMethodSelector
                {
                    Kind = (int)selector.Kind,
                    Value = selector.Value,
                    Category = selector.Category
                }).ToArray(),
                ThroughputCheckpoints = descriptor.ThroughputCheckpoints.Select(checkpoint =>
                    new PerformanceBundleThroughputCheckpoint
                    {
                        Id = checkpoint.Id,
                        MinimumCount = checkpoint.MinimumCount
                    }).ToArray(),
                PerformanceProfiler = dpaDiagnostic ? "dpa" : "circinus",
                DiagnosticSelector = dpaDiagnostic ? diagnosticSelector : null
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

    [RimWorldPerformanceTest(
        "gateway.catalog-boundary",
        EndToEndTestContract.GatewayPackageId,
        EndToEndTestContract.GatewayPackageId,
        "brrainz.harmony",
        "ludeon.rimworld",
        PerformanceTestContract.CircinusPackageId)]
    private sealed class CatalogPerformanceBenchmark : IRimWorldPerformanceTest
    {
        public void Arrange(IEndToEndContext context) { }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield break;
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
