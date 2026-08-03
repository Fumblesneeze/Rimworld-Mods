using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using NUnit.Framework;
using RimWorldDevGateway.IntegrationTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayIntegrationTestAdaptersTests
{
    [Test]
    public void Manifest_catalog_discovers_an_active_mods_exact_sibling_test_assembly()
    {
        using var directory = new TemporaryDirectory();
        var assemblyName = "Alpha.IntegrationTests.dll";
        var assemblyPath = Path.Combine(directory.Path, assemblyName);
        File.Copy(typeof(GatewayIntegrationTestRunner).Assembly.Location, assemblyPath);
        var manifestPath = Path.Combine(directory.Path, "Alpha.IntegrationTests.integrationtests.json");
        File.WriteAllText(
            manifestPath,
            "{\"ownerPackageId\":\"alpha.mod\",\"assembly\":\"Alpha.IntegrationTests.dll\"}");

        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "ludeon.rimworld", "alpha.mod" },
                new GatewayIntegrationTestManifestCandidate("alpha.mod", manifestPath)));

        var source = catalog.DiscoverActiveModAssemblies().Single();

        Assert.Multiple(() =>
        {
            Assert.That(source.OwningPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(source.Identity, Is.EqualTo("alpha.mod/Alpha.IntegrationTests.integrationtests.json"));
            Assert.That(catalog.Load(source), Is.Not.Null);
        });
    }

    [Test]
    public void Resolver_identity_matching_requires_exact_normalized_version_culture_and_public_key_token()
    {
        var requested = AssemblyIdentity(
            "Resolver.Target",
            new Version(1, 2, 3, 4),
            culture: null,
            publicKeyToken: Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(
                GatewayIntegrationTestAssemblyIdentity.Matches(
                    requested,
                    AssemblyIdentity("resolver.target", new Version(1, 2, 3, 4), "neutral", Array.Empty<byte>())),
                Is.True);
            Assert.That(
                GatewayIntegrationTestAssemblyIdentity.Matches(
                    requested,
                    AssemblyIdentity("Resolver.Target", new Version(1, 2, 3, 5), null, Array.Empty<byte>())),
                Is.False);
            Assert.That(
                GatewayIntegrationTestAssemblyIdentity.Matches(
                    requested,
                    AssemblyIdentity("Resolver.Target", new Version(1, 2, 3, 4), "de-DE", Array.Empty<byte>())),
                Is.False);
            Assert.That(
                GatewayIntegrationTestAssemblyIdentity.Matches(
                    requested,
                    AssemblyIdentity("Resolver.Target", new Version(1, 2, 3, 4), null, new byte[] { 1, 2, 3, 4 })),
                Is.False);
            Assert.That(
                GatewayIntegrationTestAssemblyIdentity.Matches(
                    AssemblyIdentity("Resolver.Zero", null, null, Array.Empty<byte>()),
                    AssemblyIdentity("Resolver.Zero", new Version(0, 0, 0, 0), "neutral", Array.Empty<byte>())),
                Is.True);
        });
    }

    [Test]
    public void Invalid_manifest_is_a_load_failure_and_does_not_hide_a_healthy_manifest()
    {
        using var directory = new TemporaryDirectory();
        var healthyManifest = WriteBundle(directory.Path, "Healthy.IntegrationTests", "alpha.mod");
        var invalidManifest = WriteBundle(
            directory.Path,
            "Invalid.IntegrationTests",
            "someone.else");
        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "alpha.mod" },
                new GatewayIntegrationTestManifestCandidate("alpha.mod", invalidManifest),
                new GatewayIntegrationTestManifestCandidate("alpha.mod", healthyManifest)));

        var sources = catalog.DiscoverActiveModAssemblies();
        var invalid = sources.Single(item => item.Identity.Contains("Invalid"));
        var healthy = sources.Single(item => item.Identity.Contains("Healthy"));

        Assert.Multiple(() =>
        {
            var failure = Assert.Throws<GatewayIntegrationTestManifestException>(() => catalog.Load(invalid));
            Assert.That(failure!.Code, Is.EqualTo("manifest_owner_mismatch"));
            Assert.That(catalog.Load(healthy), Is.Not.Null);
        });
    }

    [TestCase(",\"unexpected\":true")]
    [TestCase(",\"ownerPackageId\":\"alpha.mod\"")]
    public void Manifest_schema_rejects_unknown_and_duplicate_top_level_properties(string additionalJson)
    {
        using var directory = new TemporaryDirectory();
        var manifestPath = WriteBundle(
            directory.Path,
            "Strict.IntegrationTests",
            "alpha.mod",
            additionalJson);
        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "alpha.mod" },
                new GatewayIntegrationTestManifestCandidate("alpha.mod", manifestPath)));

        var source = catalog.DiscoverActiveModAssemblies().Single();
        var failure = Assert.Throws<GatewayIntegrationTestManifestException>(() => catalog.Load(source));

        Assert.That(failure!.Code, Is.EqualTo("invalid_manifest_schema"));
    }

    [Test]
    public void Manifest_package_matrix_selects_only_matching_active_package_combinations()
    {
        using var directory = new TemporaryDirectory();
        var missingRequired = WriteBundle(
            directory.Path,
            "MissingRequired.IntegrationTests",
            "alpha.mod",
            ",\"requiredPackageIds\":[\"needed.mod\"]");
        var forbiddenActive = WriteBundle(
            directory.Path,
            "ForbiddenActive.IntegrationTests",
            "alpha.mod",
            ",\"forbiddenPackageIds\":[\"conflict.mod\"]");
        var eligible = WriteBundle(
            directory.Path,
            "Eligible.IntegrationTests",
            "alpha.mod",
            ",\"requiredPackageIds\":[\"alpha.mod\"],\"forbiddenPackageIds\":[\"absent.mod\"]");
        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "alpha.mod", "conflict.mod" },
                new GatewayIntegrationTestManifestCandidate("alpha.mod", missingRequired),
                new GatewayIntegrationTestManifestCandidate("alpha.mod", forbiddenActive),
                new GatewayIntegrationTestManifestCandidate("alpha.mod", eligible)));

        var sources = catalog.DiscoverActiveModAssemblies();

        Assert.Multiple(() =>
        {
            Assert.That(sources, Has.Count.EqualTo(1));
            Assert.That(sources.Single().Identity, Does.Contain("Eligible"));
            Assert.That(catalog.Load(sources.Single()), Is.Not.Null);
        });
    }

    [Test]
    public void Exact_active_package_set_excludes_an_unexpected_extra_before_assembly_loading()
    {
        using var directory = new TemporaryDirectory();
        var exact = WriteBundle(
            directory.Path,
            "Exact.IntegrationTests",
            "alpha.mod",
            ",\"activePackageSetMode\":\"exact\",\"activePackageIds\":[\"ludeon.rimworld\",\"alpha.mod\",\"gateway.mod\"]");
        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "ludeon.rimworld", "alpha.mod", "gateway.mod", "unexpected.mod" },
                new GatewayIntegrationTestManifestCandidate("alpha.mod", exact)));

        var sources = catalog.DiscoverActiveModAssemblies();

        Assert.That(
            sources,
            Is.Empty,
            "A mismatched exact-set manifest must not yield a source that the runner could load or invoke.");
    }

    [Test]
    public void Exact_active_package_set_excludes_the_same_packages_in_the_wrong_order_before_assembly_loading()
    {
        using var directory = new TemporaryDirectory();
        var exact = WriteBundle(
            directory.Path,
            "ExactOrder.IntegrationTests",
            "alpha.mod",
            ",\"activePackageSetMode\":\"exact\",\"activePackageIds\":[\"ludeon.rimworld\",\"alpha.mod\",\"gateway.mod\"]");
        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "ludeon.rimworld", "gateway.mod", "alpha.mod" },
                new GatewayIntegrationTestManifestCandidate("alpha.mod", exact)));

        var sources = catalog.DiscoverActiveModAssemblies();

        Assert.That(
            sources,
            Is.Empty,
            "Exact mode must match the loaded package sequence before exposing an assembly source.");
    }

    [Test]
    public void Exact_active_package_sequence_loads_when_every_package_and_position_match()
    {
        using var directory = new TemporaryDirectory();
        var exact = WriteBundle(
            directory.Path,
            "ExactMatch.IntegrationTests",
            "alpha.mod",
            ",\"activePackageSetMode\":\"exact\",\"activePackageIds\":[\"ludeon.rimworld\",\"alpha.mod\",\"gateway.mod\"]");
        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "LUDEON.RIMWORLD", "ALPHA.MOD", "GATEWAY.MOD" },
                new GatewayIntegrationTestManifestCandidate("alpha.mod", exact)));

        var source = catalog.DiscoverActiveModAssemblies().Single();

        Assert.That(catalog.Load(source), Is.Not.Null);
    }

    [TestCase(",\"activePackageSetMode\":\"exact\"", "invalid_active_package_set")]
    [TestCase(",\"activePackageIds\":[\"alpha.mod\"]", "invalid_active_package_set")]
    [TestCase(",\"activePackageSetMode\":\"subset\",\"activePackageIds\":[\"alpha.mod\"]", "invalid_active_package_set")]
    [TestCase(",\"activePackageSetMode\":\"exact\",\"activePackageIds\":[\"alpha.mod\"],\"requiredPackageIds\":[\"alpha.mod\"]", "contradictory_package_matrix")]
    public void Exact_active_package_schema_requires_one_unambiguous_mode_and_value(
        string additionalJson,
        string expectedCode)
    {
        using var directory = new TemporaryDirectory();
        var manifestPath = WriteBundle(
            directory.Path,
            "ExactSchema.IntegrationTests",
            "alpha.mod",
            additionalJson);
        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "alpha.mod" },
                new GatewayIntegrationTestManifestCandidate("alpha.mod", manifestPath)));

        var source = catalog.DiscoverActiveModAssemblies().Single();
        var failure = Assert.Throws<GatewayIntegrationTestManifestException>(() => catalog.Load(source));

        Assert.That(failure!.Code, Is.EqualTo(expectedCode));
    }

    [TestCase("_leading.mod")]
    [TestCase("möd.unicode")]
    [TestCase(".dot-segment")]
    public void Manifest_catalog_rejects_package_ids_that_are_unsafe_as_host_path_segments(string packageId)
    {
        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(new[] { packageId }));

        var failure = Assert.Throws<GatewayIntegrationTestManifestException>(
            () => catalog.DiscoverActiveModAssemblies());

        Assert.That(failure!.Code, Is.EqualTo("invalid_active_package_list"));
    }

    [Test]
    public void Manifest_catalog_matches_package_identity_with_ordinal_ignore_case_semantics()
    {
        using var directory = new TemporaryDirectory();
        var manifestPath = WriteBundle(directory.Path, "Case.IntegrationTests", "alpha.mod");
        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "ALPHA.MOD" },
                new GatewayIntegrationTestManifestCandidate("Alpha.Mod", manifestPath)));

        var source = catalog.DiscoverActiveModAssemblies().Single();

        Assert.That(catalog.Load(source), Is.Not.Null);
    }

    [Test]
    public void Manifest_catalog_bounds_candidates_and_surfaces_the_truncation_as_an_isolated_failure()
    {
        using var directory = new TemporaryDirectory();
        var manifests = Enumerable.Range(0, 4)
            .Select(index => new GatewayIntegrationTestManifestCandidate(
                "alpha.mod",
                WriteBundle(directory.Path, $"Test{index}.IntegrationTests", "alpha.mod")))
            .ToArray();
        using var catalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(new[] { "alpha.mod" }, manifests),
            maximumManifests: 3);

        var sources = catalog.DiscoverActiveModAssemblies();
        var limitSource = sources.Single(item => item.Identity.Contains("limit"));
        var failure = Assert.Throws<GatewayIntegrationTestManifestException>(
            () => catalog.Load(limitSource));

        Assert.Multiple(() =>
        {
            Assert.That(sources, Has.Count.EqualTo(3));
            Assert.That(failure!.Code, Is.EqualTo("manifest_limit_reached"));
        });
    }

    [Test]
    public void Lifecycle_readiness_requires_a_complete_condition_to_survive_into_a_later_frame()
    {
        var probe = new MutableLifecycleProbe
        {
            IsMainThread = true,
            FrameCount = 10,
            PlayDataLoaded = true,
            NoLongEvent = true,
            EntryProgramState = true,
            EntryRootPresent = true,
            UiRootPresent = true,
            WindowStackPresent = true,
            PlayingProgramState = true,
            CurrentMapPresent = true
        };
        var readiness = new GatewayIntegrationTestStableReadiness(probe);

        Assert.That(readiness.IsReady(RunAt.MainMenuLoaded), Is.False);
        Assert.That(readiness.IsReady(RunAt.PlayableMapLoaded), Is.False);
        probe.FrameCount = 11;

        Assert.Multiple(() =>
        {
            Assert.That(readiness.IsReady(RunAt.MainMenuLoaded), Is.True);
            Assert.That(readiness.IsReady(RunAt.PlayableMapLoaded), Is.True);
        });
    }

    [Test]
    public void Session_artifact_store_attaches_latest_snapshot_atomically_without_credentials()
    {
        using var directory = new TemporaryDirectory();
        var store = new GatewayIntegrationTestSessionArtifactStore(directory.Path);
        var disabledRunner = new GatewayIntegrationTestRunner(
            enabled: false,
            new ThrowingAssemblyCatalog(),
            store,
            new NeverReady());
        var attachment = store.BeginAttachSession("run-123", disabledRunner.Snapshot);
        Assert.That(SpinWait.SpinUntil(() => attachment.IsCompleted, TimeSpan.FromSeconds(10)), Is.True);
        Assert.That(attachment.GetOutcome().Succeeded, Is.True);

        var artifactPath = Path.Combine(
            directory.Path,
            "DevGateway",
            "Sessions",
            "run-123",
            "integration-tests.json");
        var json = File.ReadAllText(artifactPath);
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(artifactPath), Is.True);
            Assert.That(json, Does.Contain("\"Enabled\":false"));
            Assert.That(json, Does.Not.Contain("token").IgnoreCase);
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(artifactPath)!, "*.tmp"), Is.Empty);
        });
    }

    [Test]
    public void Session_artifact_store_retries_a_transient_destination_reader_before_failing()
    {
        using var directory = new TemporaryDirectory();
        var store = new GatewayIntegrationTestSessionArtifactStore(directory.Path);
        var disabledRunner = new GatewayIntegrationTestRunner(
            enabled: false,
            new ThrowingAssemblyCatalog(),
            store,
            new NeverReady());
        var attachment = store.BeginAttachSession("run-transient-reader", disabledRunner.Snapshot);
        Assert.That(SpinWait.SpinUntil(() => attachment.IsCompleted, TimeSpan.FromSeconds(10)), Is.True);
        Assert.That(attachment.GetOutcome().Succeeded, Is.True);

        var artifactPath = Path.Combine(
            directory.Path,
            "DevGateway",
            "Sessions",
            "run-transient-reader",
            "integration-tests.json");
        var reader = new FileStream(
            artifactPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        var releaseReader = new Thread(() =>
        {
            Thread.Sleep(100);
            reader.Dispose();
        });
        releaseReader.Start();

        var persistence = store.BeginPersist(disabledRunner.Snapshot);
        Assert.That(SpinWait.SpinUntil(() => persistence.IsCompleted, TimeSpan.FromSeconds(10)), Is.True);
        releaseReader.Join(TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(persistence.GetOutcome().Succeeded, Is.True);
            Assert.That(File.Exists(artifactPath), Is.True);
            Assert.That(
                Directory.GetFiles(Path.GetDirectoryName(artifactPath)!, "*.tmp"),
                Is.Empty);
        });
    }

    [Test]
    public void Session_artifact_store_bounds_a_persistent_reader_and_keeps_the_prior_commit()
    {
        using var directory = new TemporaryDirectory();
        var store = new GatewayIntegrationTestSessionArtifactStore(directory.Path);
        var initial = new GatewayIntegrationTestSnapshot(enabled: false, discoveryState: "initial");
        var candidate = new GatewayIntegrationTestSnapshot(enabled: false, discoveryState: "candidate");
        var attachment = store.BeginAttachSession("run-persistent-reader", initial);
        Assert.That(SpinWait.SpinUntil(() => attachment.IsCompleted, TimeSpan.FromSeconds(10)), Is.True);
        Assert.That(attachment.GetOutcome().Succeeded, Is.True);

        var artifactPath = Path.Combine(
            directory.Path,
            "DevGateway",
            "Sessions",
            "run-persistent-reader",
            "integration-tests.json");
        using (new FileStream(
                   artifactPath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.Read))
        {
            var persistence = store.BeginPersist(candidate);
            Assert.That(SpinWait.SpinUntil(() => persistence.IsCompleted, TimeSpan.FromSeconds(5)), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(persistence.GetOutcome().Succeeded, Is.False);
                Assert.That(store.CommittedSnapshot, Is.SameAs(initial));
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(artifactPath), Does.Contain("\"DiscoveryState\":\"initial\""));
            Assert.That(File.ReadAllText(artifactPath), Does.Not.Contain("candidate"));
            Assert.That(
                Directory.GetFiles(Path.GetDirectoryName(artifactPath)!, "*.tmp"),
                Is.Empty);
        });
    }

    [Test]
    public void Session_artifact_attachment_is_not_committed_when_the_initial_snapshot_cannot_be_written()
    {
        using var directory = new TemporaryDirectory();
        var blockedSaveDataFolder = Path.Combine(directory.Path, "not-a-directory");
        File.WriteAllText(blockedSaveDataFolder, "occupied");
        var store = new GatewayIntegrationTestSessionArtifactStore(blockedSaveDataFolder);
        var disabledRunner = new GatewayIntegrationTestRunner(
            enabled: false,
            new ThrowingAssemblyCatalog(),
            store,
            new NeverReady());
        var attachment = store.BeginAttachSession("run-write-fails", disabledRunner.Snapshot);
        Assert.That(SpinWait.SpinUntil(() => attachment.IsCompleted, TimeSpan.FromSeconds(10)), Is.True);
        Assert.That(attachment.GetOutcome().Succeeded, Is.False);

        Assert.Multiple(() =>
        {
            Assert.That(store.IsAttached, Is.False);
            Assert.That(store.ArtifactPath, Is.Null);
        });
    }

    [Test]
    public void Coordinator_retries_the_same_attachment_candidate_without_discovery_or_credential_leakage()
    {
        const string token = "CURRENT_SESSION_CREDENTIAL_123456";
        var catalog = new StaticAssemblyCatalog(EmitSingleIntegrationTestAssembly());
        var store = new RetryingSessionArtifactStore(token);
        var diagnosticMessages = new List<string>();
        using var coordinator = GatewayIntegrationTestCoordinator.CreateEnabledWithStore(
            catalog,
            new NeverReady(),
            store,
            (_, exception) => diagnosticMessages.Add(exception?.Message ?? string.Empty));

        coordinator.AttachSession("run-attach-fails", token);
        coordinator.Tick();
        coordinator.Tick();
        coordinator.Tick();

        var json = Encoding.UTF8.GetString(GatewayJsonWriter.Write(coordinator.PublishedSnapshot!));
        Assert.Multiple(() =>
        {
            Assert.That(catalog.DiscoveryCalls, Is.Zero);
            Assert.That(store.AttachmentAttempts, Has.Count.EqualTo(2));
            Assert.That(store.AttachmentAttempts[1], Is.SameAs(store.AttachmentAttempts[0]));
            Assert.That(coordinator.PublishedSnapshot, Is.SameAs(store.AttachmentAttempts[0]));
            Assert.That(json, Does.Not.Contain(token));
            Assert.That(diagnosticMessages, Has.None.Contains(token));
        });
    }

    [Test]
    public void Coordinator_rejects_a_mismatched_success_and_retries_the_exact_initial_snapshot()
    {
        var catalog = new StaticAssemblyCatalog(EmitSingleIntegrationTestAssembly());
        var store = new MismatchedSuccessSessionArtifactStore();
        using var coordinator = GatewayIntegrationTestCoordinator.CreateEnabledWithStore(
            catalog,
            new AlwaysReady(),
            store);

        coordinator.AttachSession("run-mismatched-success");
        coordinator.Tick();
        var submittedSnapshot = store.AttachmentAttempts.Single();

        Assert.Multiple(() =>
        {
            Assert.That(store.AttachmentAttempts, Has.Count.EqualTo(1));
            Assert.That(coordinator.PublishedSnapshot, Is.Null);
            Assert.That(catalog.DiscoveryCalls, Is.Zero);
        });

        coordinator.Tick();
        Assert.That(store.AttachmentAttempts, Has.Count.EqualTo(2));
        Assert.That(store.AttachmentAttempts[1], Is.SameAs(submittedSnapshot));
        Assert.That(catalog.DiscoveryCalls, Is.Zero);

        coordinator.Tick();
        Assert.That(coordinator.PublishedSnapshot, Is.SameAs(submittedSnapshot));
        Assert.That(catalog.DiscoveryCalls, Is.Zero);

        coordinator.Tick();
        Assert.That(catalog.DiscoveryCalls, Is.EqualTo(1));
    }

    [Test]
    public void Disabled_coordinator_never_constructs_or_scans_enabled_runner_dependencies()
    {
        var factoryCalls = 0;
        using var coordinator = GatewayIntegrationTestCoordinator.Create(
            enabled: false,
            () =>
            {
                factoryCalls++;
                throw new AssertionException("Disabled mode must stay lazy.");
            });

        coordinator.AttachSession("run-disabled");
        coordinator.Tick();

        Assert.Multiple(() =>
        {
            Assert.That(factoryCalls, Is.Zero);
            Assert.That(coordinator.Snapshot.Enabled, Is.False);
            Assert.That(coordinator.Snapshot.DiscoveryState, Is.EqualTo("disabled"));
        });
    }

    [Test]
    public void Manifest_and_assembly_byte_bounds_fail_individually_without_unbounded_reads()
    {
        using var directory = new TemporaryDirectory();
        var oversizedManifest = WriteBundle(
            directory.Path,
            "Oversized.IntegrationTests",
            "alpha.mod",
            ",\"padding\":\"" + new string('x', 256) + "\"");
        using var manifestCatalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "alpha.mod" },
                new GatewayIntegrationTestManifestCandidate("alpha.mod", oversizedManifest)),
            maximumManifestBytes: 128);
        var manifestSource = manifestCatalog.DiscoverActiveModAssemblies().Single();
        var manifestFailure = Assert.Throws<GatewayIntegrationTestManifestException>(
            () => manifestCatalog.Load(manifestSource));

        var boundedAssemblyManifest = WriteBundle(
            directory.Path,
            "BoundedAssembly.IntegrationTests",
            "alpha.mod");
        using var assemblyCatalog = new GatewayIntegrationTestManifestCatalog(
            new FixedManifestSource(
                new[] { "alpha.mod" },
                new GatewayIntegrationTestManifestCandidate("alpha.mod", boundedAssemblyManifest)),
            maximumAssemblyBytes: 16);
        var assemblyFailure = Assert.Throws<GatewayIntegrationTestManifestException>(
            () => assemblyCatalog.Load(assemblyCatalog.DiscoverActiveModAssemblies().Single()));

        Assert.Multiple(() =>
        {
            Assert.That(manifestFailure!.Code, Is.EqualTo("manifest_size_invalid"));
            Assert.That(assemblyFailure!.Code, Is.EqualTo("assembly_byte_load_failed"));
        });
    }

    [Test]
    public void Enabled_coordinator_attaches_the_session_and_advances_one_test_per_ready_tick()
    {
        using var directory = new TemporaryDirectory();
        var probe = new MutableLifecycleProbe
        {
            IsMainThread = true,
            FrameCount = 20,
            PlayDataLoaded = true,
            NoLongEvent = true,
            EntryProgramState = true,
            EntryRootPresent = true,
            UiRootPresent = true,
            WindowStackPresent = true
        };
        var catalog = new StaticAssemblyCatalog(EmitSingleIntegrationTestAssembly());
        using var coordinator = GatewayIntegrationTestCoordinator.CreateEnabled(
            directory.Path,
            catalog,
            new GatewayIntegrationTestStableReadiness(probe));
        coordinator.AttachSession("run-enabled");

        for (var frame = 0;
             frame < 10_000 &&
             coordinator.Snapshot.Results.All(result => result.State == "running");
             frame++)
        {
            coordinator.Tick();
            probe.FrameCount++;
            Thread.Sleep(1);
        }

        Assert.Multiple(() =>
        {
            Assert.That(catalog.DiscoveryCalls, Is.EqualTo(1));
            Assert.That(coordinator.Snapshot.DiscoveredTestCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(coordinator.Snapshot.Results, Has.Some.Matches<GatewayIntegrationTestResult>(
                result => result.State == "passed"));
            Assert.That(File.Exists(coordinator.ArtifactPath), Is.True);
        });
    }

    [Test]
    public void Enabled_coordinator_factory_releases_catalog_when_construction_fails()
    {
        var catalog = new StaticAssemblyCatalog(Assembly.GetExecutingAssembly());

        Assert.Throws<ArgumentException>(() => GatewayIntegrationTestCoordinator.CreateEnabled(
            "invalid\0path",
            catalog,
            new NeverReady()));

        Assert.That(catalog.Disposed, Is.True);
    }

    [Test]
    public void Resolved_folder_scanner_uses_descending_load_priority_for_manifest_overrides()
    {
        using var directory = new TemporaryDirectory();
        var high = Path.Combine(directory.Path, "1.6");
        var low = Path.Combine(directory.Path, "Common");
        var highTests = Directory.CreateDirectory(Path.Combine(high, "DevIntegrationTests")).FullName;
        var lowTests = Directory.CreateDirectory(Path.Combine(low, "DevIntegrationTests")).FullName;
        var highOverride = Path.Combine(highTests, "Same.IntegrationTests.integrationtests.json");
        var lowOverride = Path.Combine(lowTests, "Same.IntegrationTests.integrationtests.json");
        var lowOnly = Path.Combine(lowTests, "LowOnly.IntegrationTests.integrationtests.json");
        File.WriteAllText(highOverride, "high");
        File.WriteAllText(lowOverride, "low");
        File.WriteAllText(lowOnly, "low-only");
        var scanner = new GatewayIntegrationTestResolvedFolderScanner();

        var manifests = scanner.Scan(directory.Path, new[] { high, low });

        Assert.That(manifests, Is.EqualTo(new[] { lowOnly, highOverride }));
    }

    [Test]
    public void Resolved_folder_scanner_counts_subdirectories_against_its_bounded_work_limit()
    {
        using var directory = new TemporaryDirectory();
        var version = Path.Combine(directory.Path, "1.6");
        var testDirectory = Directory.CreateDirectory(
            Path.Combine(version, "DevIntegrationTests")).FullName;
        Directory.CreateDirectory(Path.Combine(testDirectory, "one"));
        Directory.CreateDirectory(Path.Combine(testDirectory, "two"));
        Directory.CreateDirectory(Path.Combine(testDirectory, "three"));
        var scanner = new GatewayIntegrationTestResolvedFolderScanner(
            maximumDirectoryEntries: 2);

        var failure = Assert.Throws<GatewayIntegrationTestManifestException>(
            () => scanner.Scan(directory.Path, new[] { version }));

        Assert.That(failure!.Code, Is.EqualTo("manifest_scan_work_limit_reached"));
    }

    [Test]
    public void Resolved_folder_scanner_fails_the_mod_instead_of_returning_a_manifest_subset()
    {
        using var directory = new TemporaryDirectory();
        var version = Path.Combine(directory.Path, "1.6");
        var testDirectory = Directory.CreateDirectory(
            Path.Combine(version, "DevIntegrationTests")).FullName;
        for (var index = 0; index < 3; index++)
        {
            File.WriteAllText(
                Path.Combine(testDirectory, $"Test{index}.IntegrationTests.integrationtests.json"),
                "{}");
        }

        var scanner = new GatewayIntegrationTestResolvedFolderScanner(maximumManifests: 2);

        var failure = Assert.Throws<GatewayIntegrationTestManifestException>(
            () => scanner.Scan(directory.Path, new[] { version }));

        Assert.That(failure!.Code, Is.EqualTo("per_mod_manifest_limit_reached"));
    }

    [Test]
    public void Resolved_folder_scanner_rejects_a_load_folder_outside_the_mod_root()
    {
        using var root = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(outside.Path, "DevIntegrationTests"));
        var scanner = new GatewayIntegrationTestResolvedFolderScanner();

        var failure = Assert.Throws<GatewayIntegrationTestManifestException>(
            () => scanner.Scan(root.Path, new[] { outside.Path }));

        Assert.That(failure!.Code, Is.EqualTo("unsafe_resolved_folder"));
    }

    private static AssemblyName AssemblyIdentity(
        string name,
        Version? version,
        string? culture,
        byte[] publicKeyToken)
    {
        var identity = new AssemblyName
        {
            Name = name,
            Version = version,
            CultureName = string.Equals(culture, "neutral", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : culture
        };
        identity.SetPublicKeyToken(publicKeyToken);
        return identity;
    }

    private static Assembly EmitSingleIntegrationTestAssembly()
    {
        var assemblyName = new AssemblyName("GatewayCoordinatorFixture_" + Guid.NewGuid().ToString("N"));
        var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule(assemblyName.Name!);
        var type = module.DefineType(
            "CoordinatorFixture",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        var method = type.DefineMethod(
            "Probe",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(void),
            Type.EmptyTypes);
        method.SetCustomAttribute(new CustomAttributeBuilder(
            typeof(IntegrationTestAttribute).GetConstructor(new[] { typeof(RunAt) })!,
            new object[] { RunAt.MainMenuLoaded }));
        method.GetILGenerator().Emit(OpCodes.Ret);
        type.CreateType();
        return assembly;
    }

    private static string WriteBundle(
        string directory,
        string baseName,
        string manifestOwner,
        string additionalJson = "")
    {
        var assemblyName = baseName + ".dll";
        File.Copy(
            typeof(GatewayIntegrationTestRunner).Assembly.Location,
            Path.Combine(directory, assemblyName));
        var manifestPath = Path.Combine(directory, baseName + ".integrationtests.json");
        File.WriteAllText(
            manifestPath,
            "{\"ownerPackageId\":\"" + manifestOwner + "\",\"assembly\":\"" + assemblyName + "\"" +
            additionalJson + "}");
        return manifestPath;
    }

    private sealed class FixedManifestSource : IGatewayIntegrationTestManifestSource
    {
        private readonly GatewayIntegrationTestManifestDiscovery discovery;

        public FixedManifestSource(
            IEnumerable<string> activePackageIds,
            params GatewayIntegrationTestManifestCandidate[] manifests)
        {
            discovery = new GatewayIntegrationTestManifestDiscovery(activePackageIds, manifests);
        }

        public GatewayIntegrationTestManifestDiscovery Capture() => discovery;

        public IGatewayIntegrationTestManifestDiscoveryCursor BeginDiscovery() =>
            new GatewayIntegrationTestManifestDiscoveryCursor(discovery);
    }

    private sealed class MutableLifecycleProbe : IGatewayIntegrationTestLifecycleProbe
    {
        public bool IsMainThread { get; set; }
        public int FrameCount { get; set; }
        public bool PlayDataLoaded { get; set; }
        public bool NoLongEvent { get; set; }
        public bool EntryProgramState { get; set; }
        public bool EntryRootPresent { get; set; }
        public bool UiRootPresent { get; set; }
        public bool WindowStackPresent { get; set; }
        public bool PlayingProgramState { get; set; }
        public bool CurrentMapPresent { get; set; }
    }

    private sealed class ThrowingAssemblyCatalog : IGatewayIntegrationTestAssemblyCatalog
    {
        public IGatewayIntegrationTestAssemblyDiscoveryCursor BeginDiscovery() =>
            throw new AssertionException("Disabled runners must not scan staged tests.");

        public System.Reflection.Assembly Load(GatewayIntegrationTestAssemblySource source) =>
            throw new AssertionException("Disabled runners must not load staged tests.");
    }

    private sealed class NeverReady : IGatewayIntegrationTestReadiness
    {
        public bool IsMainThread => true;

        public bool IsReady(RunAt lifecyclePoint) => false;
    }

    private sealed class AlwaysReady : IGatewayIntegrationTestReadiness
    {
        public bool IsMainThread => true;

        public bool IsReady(RunAt lifecyclePoint) => true;
    }

    private sealed class RetryingSessionArtifactStore : IGatewayIntegrationTestSessionArtifactStore
    {
        private readonly string token;

        public RetryingSessionArtifactStore(string token)
        {
            this.token = token;
        }

        public bool IsAttached { get; private set; }

        public GatewayIntegrationTestSnapshot? CommittedSnapshot { get; private set; }

        public List<GatewayIntegrationTestSnapshot> AttachmentAttempts { get; } = new();

        public string? ArtifactPath => null;

        public IGatewayIntegrationTestPersistenceOperation BeginPersist(
            GatewayIntegrationTestSnapshot snapshot) =>
            throw new AssertionException("An unattached store must not persist runner transitions.");

        public IGatewayIntegrationTestPersistenceOperation BeginAttachSession(
            string runId,
            GatewayIntegrationTestSnapshot initialSnapshot)
        {
            AttachmentAttempts.Add(initialSnapshot);
            if (AttachmentAttempts.Count == 1)
            {
                return new GatewayIntegrationTestCompletedPersistenceOperation(
                    GatewayIntegrationTestPersistenceOutcome.Failed(
                        initialSnapshot,
                        new IOException("Could not attach credential " + token + ".")));
            }

            IsAttached = true;
            CommittedSnapshot = initialSnapshot;
            return new GatewayIntegrationTestCompletedPersistenceOperation(
                GatewayIntegrationTestPersistenceOutcome.Success(initialSnapshot));
        }
    }

    private sealed class MismatchedSuccessSessionArtifactStore :
        IGatewayIntegrationTestSessionArtifactStore
    {
        private readonly GatewayIntegrationTestSnapshot wrongSnapshot = new(
            enabled: true,
            discoveryState: "wrong-snapshot");

        public bool IsAttached { get; private set; }

        public GatewayIntegrationTestSnapshot? CommittedSnapshot { get; private set; }

        public string? ArtifactPath => null;

        public List<GatewayIntegrationTestSnapshot> AttachmentAttempts { get; } = new();

        public IGatewayIntegrationTestPersistenceOperation BeginPersist(
            GatewayIntegrationTestSnapshot snapshot) =>
            throw new AssertionException("No runner transition is expected in this attachment-gate test.");

        public IGatewayIntegrationTestPersistenceOperation BeginAttachSession(
            string runId,
            GatewayIntegrationTestSnapshot initialSnapshot)
        {
            AttachmentAttempts.Add(initialSnapshot);
            IsAttached = true;
            var committed = AttachmentAttempts.Count == 1 ? wrongSnapshot : initialSnapshot;
            CommittedSnapshot = committed;
            return new GatewayIntegrationTestCompletedPersistenceOperation(
                GatewayIntegrationTestPersistenceOutcome.Success(committed));
        }
    }

    private sealed class StaticAssemblyCatalog : IGatewayIntegrationTestAssemblyCatalog, IDisposable
    {
        private readonly Assembly assembly;
        private readonly GatewayIntegrationTestAssemblySource source =
            new("test.mod", "test.mod/Test.IntegrationTests.integrationtests.json");

        public StaticAssemblyCatalog(Assembly assembly)
        {
            this.assembly = assembly;
        }

        public int DiscoveryCalls { get; private set; }

        public bool Disposed { get; private set; }

        public IGatewayIntegrationTestAssemblyDiscoveryCursor BeginDiscovery()
        {
            DiscoveryCalls++;
            return new GatewayIntegrationTestAssemblyDiscoveryCursor(new[] { source });
        }

        public Assembly Load(GatewayIntegrationTestAssemblySource requestedSource)
        {
            Assert.That(requestedSource, Is.SameAs(source));
            return assembly;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "RimWorldDevGateway.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
