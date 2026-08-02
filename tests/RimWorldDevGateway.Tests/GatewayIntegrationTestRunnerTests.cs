using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using RimWorldDevGateway.IntegrationTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayIntegrationTestRunnerTests
{
    [Test]
    public void Disabled_runner_never_accesses_the_active_mod_assembly_catalog()
    {
        var catalog = new RecordingAssemblyCatalog();
        var runner = new GatewayIntegrationTestRunner(
            enabled: false,
            catalog,
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        DriveUntilDiscoveryTerminal(runner);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.DiscoveryCount, Is.Zero);
            Assert.That(runner.Snapshot.Enabled, Is.False);
            Assert.That(runner.Snapshot.DiscoveryState, Is.EqualTo("disabled"));
        });
    }

    [Test]
    public void Enabled_runner_reports_zero_discovery_as_a_failed_suite()
    {
        var catalog = new RecordingAssemblyCatalog();
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            catalog,
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        DriveUntilDiscoveryTerminal(runner);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.DiscoveryCount, Is.EqualTo(1));
            Assert.That(runner.Snapshot.DiscoveryState, Is.EqualTo("failed"));
            Assert.That(runner.Snapshot.Failures, Has.Count.EqualTo(1));
            Assert.That(runner.Snapshot.Failures[0].Code, Is.EqualTo("no_integration_tests_discovered"));
        });
    }

    [Test]
    public void Discovery_waits_for_the_declared_native_lifecycle_readiness()
    {
        var catalog = new RecordingAssemblyCatalog();
        var readiness = new FixedReadiness(isMainThread: true, ready: false);
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            catalog,
            new RecordingArtifactStore(),
            readiness);

        runner.Observe(RunAt.MainMenuLoaded);
        Assert.That(catalog.DiscoveryCount, Is.Zero);
        readiness.Ready = true;
        DriveUntilDiscoveryTerminal(runner);

        Assert.That(catalog.DiscoveryCount, Is.EqualTo(1));
        Assert.That(runner.Snapshot.DiscoveryState, Is.EqualTo("failed"));
    }

    [Test]
    public void Valid_tests_run_in_deterministic_method_order_for_the_observed_stage()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("Zeta", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass),
            ("Alpha", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass),
            ("MapOnly", RunAt.PlayableMapLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var catalog = new RecordingAssemblyCatalog((source, () => fixture.Assembly));
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            catalog,
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        DriveUntilDiscoveryTerminal(runner);
        DriveUntilResultCount(runner, RunAt.MainMenuLoaded, 1);
        Assert.That(fixture.Calls, Is.EqualTo(new[] { "Alpha" }), "Only one test may start in a frame.");
        Assert.That(
            runner.Snapshot.LifecyclePoints.Single(point => point.RunAt == RunAt.MainMenuLoaded).State,
            Is.EqualTo("running"));
        DriveUntilStageTerminal(runner, RunAt.MainMenuLoaded);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Calls, Is.EqualTo(new[] { "Alpha", "Zeta" }));
            Assert.That(runner.Snapshot.DiscoveredAssemblyCount, Is.EqualTo(1));
            Assert.That(runner.Snapshot.DiscoveredTestCount, Is.EqualTo(3));
            Assert.That(
                runner.Snapshot.DiscoveredTests.Select(test => (test.TestName, test.RunAt)),
                Is.EqualTo(new[]
                {
                    ("Fixture.Alpha", RunAt.MainMenuLoaded),
                    ("Fixture.MapOnly", RunAt.PlayableMapLoaded),
                    ("Fixture.Zeta", RunAt.MainMenuLoaded)
                }));
            Assert.That(
                runner.Snapshot.Results.Select(result => result.TestName),
                Is.EqualTo(new[] { "Fixture.Alpha", "Fixture.Zeta" }));
            Assert.That(runner.Snapshot.Results.Select(result => result.State), Is.All.EqualTo("passed"));
        });
    }

    [Test]
    public void Each_lifecycle_suite_executes_at_most_once_per_process()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("AtMenu", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass),
            ("OnMap", RunAt.PlayableMapLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => fixture.Assembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        DriveUntilDiscoveryTerminal(runner);
        DriveUntilStageTerminal(runner, RunAt.MainMenuLoaded);
        DriveUntilStageTerminal(runner, RunAt.PlayableMapLoaded);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Calls, Is.EqualTo(new[] { "AtMenu", "OnMap" }));
            Assert.That(runner.Snapshot.Results, Has.Count.EqualTo(2));
            Assert.That(
                runner.Snapshot.LifecyclePoints.Select(point => (point.RunAt, point.State)),
                Is.EqualTo(new[]
                {
                    (RunAt.MainMenuLoaded, "completed"),
                    (RunAt.PlayableMapLoaded, "completed")
                }));
        });
    }

    [Test]
    public void Load_and_invalid_signature_failures_do_not_hide_later_valid_tests()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("BrokenSignature", RunAt.MainMenuLoaded, EmittedMethodBehavior.InvalidReturn),
            ("StillRuns", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var bad = new GatewayIntegrationTestAssemblySource("a.bad", "Bad.IntegrationTests.dll");
        var good = new GatewayIntegrationTestAssemblySource("b.good", "Good.IntegrationTests.dll");
        var catalog = new RecordingAssemblyCatalog(
            (good, () => fixture.Assembly),
            (bad, () => throw new BadImageFormatException("not an assembly")));
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            catalog,
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        DriveUntilDiscoveryTerminal(runner);
        DriveUntilStageTerminal(runner, RunAt.MainMenuLoaded);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.LoadedIdentities, Is.EqualTo(new[]
            {
                "Bad.IntegrationTests.dll",
                "Good.IntegrationTests.dll"
            }));
            Assert.That(fixture.Calls, Is.EqualTo(new[] { "StillRuns" }));
            Assert.That(runner.Snapshot.DiscoveryState, Is.EqualTo("completed"));
            Assert.That(
                runner.Snapshot.Failures.Select(failure => failure.Code),
                Is.EquivalentTo(new[] { "assembly_load_failed", "invalid_test_signature" }));
            Assert.That(runner.Snapshot.Results.Single().State, Is.EqualTo("passed"));
        });
    }

    [Test]
    public void Async_void_tests_are_rejected_without_instantiating_the_marker_attribute()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("AsyncVoid", RunAt.MainMenuLoaded, EmittedMethodBehavior.InvalidAsyncVoid),
            ("StillRuns", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => fixture.Assembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        DriveUntilDiscoveryTerminal(runner);
        DriveUntilStageTerminal(runner, RunAt.MainMenuLoaded);

        Assert.Multiple(() =>
        {
            Assert.That(
                runner.Snapshot.Failures.Select(failure => failure.Code),
                Does.Contain("invalid_async_void_test"));
            Assert.That(fixture.Calls, Is.EqualTo(new[] { "StillRuns" }));
        });
    }

    [Test]
    public void Failed_assembly_loads_count_toward_the_attempt_bound()
    {
        var first = new GatewayIntegrationTestAssemblySource("a.fixture", "A.IntegrationTests.dll");
        var second = new GatewayIntegrationTestAssemblySource("b.fixture", "B.IntegrationTests.dll");
        var third = new GatewayIntegrationTestAssemblySource("c.fixture", "C.IntegrationTests.dll");
        var catalog = new RecordingAssemblyCatalog(
            (first, () => throw new BadImageFormatException("first")),
            (second, () => throw new BadImageFormatException("second")),
            (third, () => throw new BadImageFormatException("third")));
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            catalog,
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true),
            maximumAssemblies: 2);

        DriveUntilDiscoveryTerminal(runner);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.LoadedIdentities, Is.EqualTo(new[]
            {
                "A.IntegrationTests.dll",
                "B.IntegrationTests.dll"
            }));
            Assert.That(runner.Snapshot.DiscoveredAssemblyCount, Is.Zero);
            Assert.That(
                runner.Snapshot.Failures.Select(failure => failure.Code),
                Does.Contain("assembly_limit_reached"));
        });
    }

    [Test]
    public void Target_invocation_failures_are_unwrapped_and_later_tests_continue()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("AlphaThrows", RunAt.MainMenuLoaded, EmittedMethodBehavior.Throw),
            ("BetaPasses", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => fixture.Assembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        DriveUntilDiscoveryTerminal(runner);
        DriveUntilStageTerminal(runner, RunAt.MainMenuLoaded);

        var failed = runner.Snapshot.Results[0];
        Assert.Multiple(() =>
        {
            Assert.That(failed.State, Is.EqualTo("failed"));
            Assert.That(failed.ExceptionType, Is.EqualTo(typeof(InvalidOperationException).FullName));
            Assert.That(failed.ExceptionType, Is.Not.EqualTo(typeof(TargetInvocationException).FullName));
            Assert.That(failed.Message, Does.Contain("AlphaThrows failed"));
            Assert.That(runner.Snapshot.Results[1].State, Is.EqualTo("passed"));
            Assert.That(fixture.Calls, Is.EqualTo(new[] { "BetaPasses" }));
            Assert.That(
                runner.Snapshot.LifecyclePoints.Single(point => point.RunAt == RunAt.MainMenuLoaded).State,
                Is.EqualTo("failed"));
        });
    }

    [Test]
    public void Hostile_exception_formatting_is_isolated_and_later_tests_continue()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("AlphaHostile", RunAt.MainMenuLoaded, EmittedMethodBehavior.ThrowHostileException),
            ("BetaPasses", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => fixture.Assembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        Assert.DoesNotThrow(() => DriveUntilDiscoveryTerminal(runner));
        Assert.DoesNotThrow(() => DriveUntilStageTerminal(runner, RunAt.MainMenuLoaded));

        Assert.Multiple(() =>
        {
            Assert.That(runner.Snapshot.Results[0].State, Is.EqualTo("failed"));
            Assert.That(runner.Snapshot.Results[0].Message, Does.Contain("suppressed"));
            Assert.That(runner.Snapshot.Results[1].State, Is.EqualTo("passed"));
            Assert.That(fixture.Calls, Is.EqualTo(new[] { "BetaPasses" }));
        });
    }

    [Test]
    public void Current_session_credential_is_redacted_from_the_status_endpoint_and_durable_artifact()
    {
        const string token = "CURRENT_SESSION_CREDENTIAL_987654321";
        using var directory = new TemporaryDirectory();
        var fixture = EmittedIntegrationAssembly.Create(
            (token, RunAt.MainMenuLoaded, EmittedMethodBehavior.Throw));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var store = new GatewayIntegrationTestSessionArtifactStore(directory.Path);
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => fixture.Assembly)),
            store,
            new FixedReadiness(isMainThread: true, ready: true));
        runner.AttachSessionCredential(token);
        WaitForPersistence(store.BeginAttachSession("run-redaction", runner.Snapshot));

        DriveUntilDiscoveryTerminal(runner);
        DriveUntilStageTerminal(runner, RunAt.MainMenuLoaded);

        var router = new GatewayApiRouter(
            new GatewayDispatcher(),
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(integrationTestSnapshot: () => runner.Snapshot));
        var response = router.Handle(
            new GatewayHttpRequest(
                "GET",
                "/api/v1/integration-tests",
                "/api/v1/integration-tests",
                string.Empty,
                new Dictionary<string, string>(),
                Array.Empty<byte>()),
            "credential-redaction");
        var endpointJson = Encoding.UTF8.GetString(response.Body);
        var artifactJson = File.ReadAllText(store.ArtifactPath!);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(endpointJson, Does.Not.Contain(token));
            Assert.That(artifactJson, Does.Not.Contain(token));
            Assert.That(endpointJson, Does.Contain("[REDACTED]"));
            Assert.That(artifactJson, Does.Contain("[REDACTED]"));
        });
    }

    [Test]
    public void A_running_snapshot_is_persisted_before_invocation_and_remains_immutable()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("Probe", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var store = new RecordingArtifactStore();
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => fixture.Assembly)),
            store,
            new FixedReadiness(isMainThread: true, ready: true));

        DriveUntilDiscoveryTerminal(runner);
        DriveUntilStageTerminal(runner, RunAt.MainMenuLoaded);

        var runningIndex = store.Snapshots.FindIndex(snapshot =>
            snapshot.Results.Count == 1 && snapshot.Results[0].State == "running");
        var passedIndex = store.Snapshots.FindIndex(snapshot =>
            snapshot.Results.Count == 1 && snapshot.Results[0].State == "passed");
        Assert.Multiple(() =>
        {
            Assert.That(runningIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(passedIndex, Is.GreaterThan(runningIndex));
            Assert.That(store.Snapshots[runningIndex].Results[0].State, Is.EqualTo("running"));
            Assert.That(runner.Snapshot.Results[0].State, Is.EqualTo("passed"));
        });
    }

    [Test]
    public void A_failed_running_snapshot_commit_retries_the_same_candidate_without_rerunning_the_test()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("MustNotRun", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var store = new FailFirstRunningArtifactStore();
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => fixture.Assembly)),
            store,
            new FixedReadiness(isMainThread: true, ready: true));

        DriveUntilDiscoveryTerminal(runner);
        var discoverySnapshot = runner.PublishedSnapshot;
        runner.Observe(RunAt.MainMenuLoaded);
        runner.Observe(RunAt.MainMenuLoaded);
        runner.Observe(RunAt.MainMenuLoaded);
        runner.Observe(RunAt.MainMenuLoaded);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Calls, Is.Empty);
            Assert.That(store.RunningAttempts, Has.Count.EqualTo(2));
            Assert.That(store.RunningAttempts[1], Is.SameAs(store.RunningAttempts[0]));
            Assert.That(runner.PublishedSnapshot, Is.SameAs(discoverySnapshot));
        });

        runner.Observe(RunAt.MainMenuLoaded);
        Assert.That(fixture.Calls, Is.Empty, "The commit-completion polling frame must not also invoke.");
        Assert.That(runner.PublishedSnapshot, Is.SameAs(store.RunningAttempts[0]));
        runner.Observe(RunAt.MainMenuLoaded);
        Assert.That(fixture.Calls, Is.EqualTo(new[] { "MustNotRun" }));
    }

    [Test]
    public void A_test_is_never_invoked_before_the_artifact_store_is_attached_to_a_session()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("MustNotRun", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => fixture.Assembly)),
            new UnattachedArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        runner.Observe(RunAt.MainMenuLoaded);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Calls, Is.Empty);
            Assert.That(runner.Snapshot.DiscoveryState, Is.EqualTo("not-started"));
            Assert.That(runner.Snapshot.Results, Is.Empty);
        });
    }

    [Test]
    public void Tests_run_synchronously_on_the_observing_game_main_thread()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("CaptureThread", RunAt.MainMenuLoaded, EmittedMethodBehavior.CaptureThread));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => fixture.Assembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        var observingThread = Thread.CurrentThread.ManagedThreadId;
        DriveUntilDiscoveryTerminal(runner);
        DriveUntilStageTerminal(runner, RunAt.MainMenuLoaded);

        Assert.That(fixture.InvocationThreadId, Is.EqualTo(observingThread));
    }

    [Test]
    public void A_ready_lifecycle_observed_off_the_game_thread_is_rejected_before_discovery()
    {
        var catalog = new RecordingAssemblyCatalog();
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            catalog,
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: false, ready: true));

        Assert.Throws<InvalidOperationException>(() => runner.Observe(RunAt.MainMenuLoaded));
        Assert.That(catalog.DiscoveryCount, Is.Zero);
    }

    [Test]
    public void Off_thread_observation_is_rejected_before_lifecycle_readiness_is_probed()
    {
        var catalog = new RecordingAssemblyCatalog();
        var readiness = new RecordingReadiness(isMainThread: false, ready: true);
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            catalog,
            new RecordingArtifactStore(),
            readiness);

        Assert.Throws<InvalidOperationException>(() => runner.Observe(RunAt.MainMenuLoaded));
        Assert.Multiple(() =>
        {
            Assert.That(readiness.ReadyProbeCount, Is.Zero);
            Assert.That(catalog.DiscoveryCount, Is.Zero);
        });
    }

    [Test]
    public void Discovery_and_result_snapshots_respect_the_configured_test_bound()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("Charlie", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass),
            ("Alpha", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass),
            ("Bravo", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => fixture.Assembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true),
            maximumTests: 1);

        DriveUntilDiscoveryTerminal(runner);
        DriveUntilStageTerminal(runner, RunAt.MainMenuLoaded);

        Assert.Multiple(() =>
        {
            Assert.That(runner.Snapshot.DiscoveredTestCount, Is.EqualTo(1));
            Assert.That(runner.Snapshot.Results, Has.Count.EqualTo(1));
            Assert.That(runner.Snapshot.OmittedResultCount, Is.EqualTo(2));
            Assert.That(fixture.Calls, Is.EqualTo(new[] { "Alpha" }));
        });
    }

    [Test]
    public void Discovery_schedules_one_source_worker_and_never_invokes_tests_during_discovery()
    {
        var fixture = EmittedIntegrationAssembly.Create(
            ("Probe", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var catalog = new RecordingAssemblyCatalog((source, () => fixture.Assembly));
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            catalog,
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        runner.Observe(RunAt.MainMenuLoaded);
        Assert.Multiple(() =>
        {
            Assert.That(catalog.DiscoveryCount, Is.EqualTo(1));
            Assert.That(catalog.DiscoveryAdvanceCount, Is.Zero);
            Assert.That(catalog.LoadedIdentities, Is.Empty);
        });

        runner.Observe(RunAt.MainMenuLoaded);
        Assert.Multiple(() =>
        {
            Assert.That(catalog.DiscoveryAdvanceCount, Is.EqualTo(1));
            Assert.That(fixture.Calls, Is.Empty);
            Assert.That(runner.Snapshot.Results, Is.Empty);
        });

        DriveUntilDiscoveryTerminal(runner);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.LoadedIdentities, Is.EqualTo(new[] { "Fixture.IntegrationTests.dll" }));
            Assert.That(fixture.Calls, Is.Empty);
            Assert.That(runner.Snapshot.Results, Is.Empty);
        });
    }

    [Test]
    [Timeout(10_000)]
    public void Blocking_assembly_load_runs_on_a_polled_background_worker()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var fixture = EmittedIntegrationAssembly.Create(
            ("Probe", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () =>
            {
                entered.Set();
                release.Wait();
                return fixture.Assembly;
            })),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        runner.Observe(RunAt.MainMenuLoaded);
        try
        {
            AssertObserveReturnsPromptly(runner);
            Assert.That(entered.Wait(TimeSpan.FromSeconds(2)), Is.True);
            AssertObserveReturnsPromptly(runner);
            Assert.That(runner.Snapshot.DiscoveryState, Is.EqualTo("running"));
        }
        finally
        {
            release.Set();
        }

        DriveUntilDiscoveryTerminal(runner);
        Assert.That(runner.Snapshot.DiscoveredTestCount, Is.EqualTo(1));
    }

    [Test]
    [Timeout(10_000)]
    public void Blocking_get_types_runs_on_a_polled_background_worker()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var fixture = EmittedIntegrationAssembly.Create(
            ("Probe", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var assembly = new BlockingTypesAssembly(fixture.Assembly.GetTypes(), entered, release);
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => assembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        runner.Observe(RunAt.MainMenuLoaded);
        try
        {
            AssertObserveReturnsPromptly(runner);
            Assert.That(entered.Wait(TimeSpan.FromSeconds(2)), Is.True);
            AssertObserveReturnsPromptly(runner);
        }
        finally
        {
            release.Set();
        }

        DriveUntilDiscoveryTerminal(runner);
        Assert.That(runner.Snapshot.DiscoveredTestCount, Is.EqualTo(1));
    }

    [Test]
    [Timeout(10_000)]
    public void Blocking_get_methods_runs_on_a_polled_background_worker()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var fixture = EmittedIntegrationAssembly.Create(
            ("Probe", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var blockingType = new BlockingMethodsType(
            fixture.Assembly.GetTypes().Single(),
            entered,
            release);
        var assembly = new BlockingTypesAssembly(new Type[] { blockingType });
        var source = new GatewayIntegrationTestAssemblySource("example.fixture", "Fixture.IntegrationTests.dll");
        var runner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((source, () => assembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true));

        runner.Observe(RunAt.MainMenuLoaded);
        try
        {
            AssertObserveReturnsPromptly(runner);
            Assert.That(entered.Wait(TimeSpan.FromSeconds(2)), Is.True);
            AssertObserveReturnsPromptly(runner);
        }
        finally
        {
            release.Set();
        }

        DriveUntilDiscoveryTerminal(runner);
        Assert.That(runner.Snapshot.DiscoveredTestCount, Is.EqualTo(1));
    }

    [Test]
    public void Discovery_enforces_type_method_and_total_work_limits()
    {
        var multiTypeAssembly = EmitMultiTypeIntegrationAssembly(typeCount: 3);
        var typeSource = new GatewayIntegrationTestAssemblySource("types.fixture", "Types.IntegrationTests.dll");
        var typeRunner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((typeSource, () => multiTypeAssembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true),
            maximumTypesPerAssembly: 1);
        DriveUntilDiscoveryTerminal(typeRunner);

        var methodsFixture = EmittedIntegrationAssembly.Create(
            ("Alpha", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass),
            ("Bravo", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass),
            ("Charlie", RunAt.MainMenuLoaded, EmittedMethodBehavior.Pass));
        var methodSource = new GatewayIntegrationTestAssemblySource("methods.fixture", "Methods.IntegrationTests.dll");
        var methodRunner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((methodSource, () => methodsFixture.Assembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true),
            maximumMethodsPerType: 2);
        DriveUntilDiscoveryTerminal(methodRunner);

        var workSource = new GatewayIntegrationTestAssemblySource("work.fixture", "Work.IntegrationTests.dll");
        var workRunner = new GatewayIntegrationTestRunner(
            enabled: true,
            new RecordingAssemblyCatalog((workSource, () => methodsFixture.Assembly)),
            new RecordingArtifactStore(),
            new FixedReadiness(isMainThread: true, ready: true),
            maximumDiscoveryWork: 3);
        DriveUntilDiscoveryTerminal(workRunner);

        Assert.Multiple(() =>
        {
            Assert.That(typeRunner.Snapshot.DiscoveredTestCount, Is.EqualTo(1));
            Assert.That(typeRunner.Snapshot.Failures.Select(failure => failure.Code), Does.Contain("type_limit_reached"));
            Assert.That(methodRunner.Snapshot.DiscoveredTestCount, Is.EqualTo(2));
            Assert.That(methodRunner.Snapshot.Failures.Select(failure => failure.Code), Does.Contain("method_limit_reached"));
            Assert.That(workRunner.Snapshot.Failures.Select(failure => failure.Code), Does.Contain("discovery_work_limit_reached"));
        });
    }

    private static void DriveUntilDiscoveryTerminal(GatewayIntegrationTestRunner runner)
    {
        for (var frame = 0; frame < 10_000; frame++)
        {
            var state = runner.Snapshot.DiscoveryState;
            if (state == "disabled" ||
                (state != "not-started" &&
                 state != "running" &&
                 ReferenceEquals(runner.PublishedSnapshot, runner.Snapshot) &&
                 runner.IsPersistenceIdle))
            {
                break;
            }

            runner.Observe(RunAt.MainMenuLoaded);
            Thread.Sleep(1);
        }

        Assert.That(runner.Snapshot.DiscoveryState, Is.Not.EqualTo("running"));
    }

    private static void AssertObserveReturnsPromptly(GatewayIntegrationTestRunner runner)
    {
        var stopwatch = Stopwatch.StartNew();
        runner.Observe(RunAt.MainMenuLoaded);
        stopwatch.Stop();
        Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromMilliseconds(500)));
    }

    private static void DriveUntilStageTerminal(GatewayIntegrationTestRunner runner, RunAt runAt)
    {
        for (var frame = 0; frame < 10_000; frame++)
        {
            var lifecycle = runner.Snapshot.LifecyclePoints.Single(point => point.RunAt == runAt);
            if (lifecycle.State != "pending" &&
                lifecycle.State != "running" &&
                ReferenceEquals(runner.PublishedSnapshot, runner.Snapshot) &&
                runner.IsPersistenceIdle)
            {
                return;
            }

            runner.Observe(runAt);
            Thread.Sleep(1);
        }

        Assert.Fail("The integration-test lifecycle did not reach a terminal state within its frame bound.");
    }

    private static void DriveUntilResultCount(
        GatewayIntegrationTestRunner runner,
        RunAt runAt,
        int expectedCount)
    {
        for (var frame = 0; frame < 10_000; frame++)
        {
            if (runner.Snapshot.Results.Count >= expectedCount &&
                runner.Snapshot.Results.Take(expectedCount).All(result => result.State != "running"))
            {
                break;
            }

            runner.Observe(runAt);
            Thread.Sleep(1);
        }

        Assert.That(runner.Snapshot.Results, Has.Count.GreaterThanOrEqualTo(expectedCount));
        Assert.That(
            runner.Snapshot.Results.Take(expectedCount).Select(result => result.State),
            Has.None.EqualTo("running"));
    }

    private static GatewayIntegrationTestPersistenceOutcome WaitForPersistence(
        IGatewayIntegrationTestPersistenceOperation operation)
    {
        Assert.That(SpinWait.SpinUntil(() => operation.IsCompleted, TimeSpan.FromSeconds(10)), Is.True);
        var outcome = operation.GetOutcome();
        Assert.That(outcome.Succeeded, Is.True, outcome.Failure?.Message);
        return outcome;
    }

    private static Assembly EmitMultiTypeIntegrationAssembly(int typeCount)
    {
        var assemblyName = new AssemblyName("GatewayIntegrationMultiType_" + Guid.NewGuid().ToString("N"));
        var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule(assemblyName.Name!);
        var attributeConstructor = typeof(IntegrationTestAttribute).GetConstructor(new[] { typeof(RunAt) })!;
        for (var index = 0; index < typeCount; index++)
        {
            var type = module.DefineType(
                "Fixture" + index.ToString("D3"),
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            var method = type.DefineMethod(
                "Probe",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(void),
                Type.EmptyTypes);
            method.SetCustomAttribute(new CustomAttributeBuilder(
                attributeConstructor,
                new object[] { RunAt.MainMenuLoaded }));
            method.GetILGenerator().Emit(OpCodes.Ret);
            type.CreateType();
        }

        return assembly;
    }

    private sealed class BlockingTypesAssembly : Assembly
    {
        private readonly Type[] types;
        private readonly ManualResetEventSlim? entered;
        private readonly ManualResetEventSlim? release;

        public BlockingTypesAssembly(
            Type[] types,
            ManualResetEventSlim? entered = null,
            ManualResetEventSlim? release = null)
        {
            this.types = types;
            this.entered = entered;
            this.release = release;
        }

        public override string FullName => "Blocking.IntegrationTests, Version=1.0.0.0";

        public override Type[] GetTypes()
        {
            entered?.Set();
            release?.Wait();
            return types;
        }
    }

    private sealed class BlockingMethodsType : TypeDelegator
    {
        private readonly ManualResetEventSlim entered;
        private readonly ManualResetEventSlim release;

        public BlockingMethodsType(
            Type delegatingType,
            ManualResetEventSlim entered,
            ManualResetEventSlim release)
            : base(delegatingType)
        {
            this.entered = entered;
            this.release = release;
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            entered.Set();
            release.Wait();
            return base.GetMethods(bindingAttr);
        }
    }

    private sealed class RecordingAssemblyCatalog : IGatewayIntegrationTestAssemblyCatalog
    {
        private readonly IReadOnlyList<(GatewayIntegrationTestAssemblySource Source, Func<Assembly> Load)> entries;

        public RecordingAssemblyCatalog(
            params (GatewayIntegrationTestAssemblySource Source, Func<Assembly> Load)[] entries)
        {
            this.entries = entries;
        }

        public int DiscoveryCount { get; private set; }

        public int DiscoveryAdvanceCount { get; private set; }

        public List<string> LoadedIdentities { get; } = new();

        public IGatewayIntegrationTestAssemblyDiscoveryCursor BeginDiscovery()
        {
            DiscoveryCount++;
            return new RecordingDiscoveryCursor(
                entries.Select(entry => entry.Source).ToArray(),
                () => DiscoveryAdvanceCount++);
        }

        public Assembly Load(GatewayIntegrationTestAssemblySource source)
        {
            LoadedIdentities.Add(source.Identity);
            return entries.Single(entry => ReferenceEquals(entry.Source, source)).Load();
        }

        private sealed class RecordingDiscoveryCursor : IGatewayIntegrationTestAssemblyDiscoveryCursor
        {
            private readonly IReadOnlyList<GatewayIntegrationTestAssemblySource> sources;
            private readonly Action advanced;
            private int index;

            public RecordingDiscoveryCursor(
                IReadOnlyList<GatewayIntegrationTestAssemblySource> sources,
                Action advanced)
            {
                this.sources = sources
                    .OrderBy(source => source.OwningPackageId, StringComparer.Ordinal)
                    .ThenBy(source => source.Identity, StringComparer.Ordinal)
                    .ToArray();
                this.advanced = advanced;
            }

            public GatewayIntegrationTestAssemblyDiscoveryStep Advance()
            {
                advanced();
                return index < sources.Count
                    ? GatewayIntegrationTestAssemblyDiscoveryStep.Found(sources[index++])
                    : GatewayIntegrationTestAssemblyDiscoveryStep.Complete();
            }

            public void Dispose()
            {
            }
        }
    }

    private sealed class RecordingArtifactStore : IGatewayIntegrationTestArtifactStore
    {
        public bool IsAttached => true;

        public GatewayIntegrationTestSnapshot? CommittedSnapshot { get; private set; }

        public List<GatewayIntegrationTestSnapshot> Snapshots { get; } = new();

        public IGatewayIntegrationTestPersistenceOperation BeginPersist(
            GatewayIntegrationTestSnapshot snapshot)
        {
            Snapshots.Add(snapshot);
            CommittedSnapshot = snapshot;
            return new GatewayIntegrationTestCompletedPersistenceOperation(
                GatewayIntegrationTestPersistenceOutcome.Success(snapshot));
        }
    }

    private sealed class FailFirstRunningArtifactStore : IGatewayIntegrationTestArtifactStore
    {
        public bool IsAttached => true;

        public GatewayIntegrationTestSnapshot? CommittedSnapshot { get; private set; }

        public List<GatewayIntegrationTestSnapshot> RunningAttempts { get; } = new();

        private bool failedRunningSnapshot;

        public IGatewayIntegrationTestPersistenceOperation BeginPersist(
            GatewayIntegrationTestSnapshot snapshot)
        {
            var isRunning = snapshot.Results.Count == 1 && snapshot.Results[0].State == "running";
            if (isRunning)
            {
                RunningAttempts.Add(snapshot);
                if (!failedRunningSnapshot)
                {
                    failedRunningSnapshot = true;
                    return new GatewayIntegrationTestCompletedPersistenceOperation(
                        GatewayIntegrationTestPersistenceOutcome.Failed(
                            snapshot,
                            new IOException("The integration-test artifact is temporarily unavailable.")));
                }
            }

            CommittedSnapshot = snapshot;
            return new GatewayIntegrationTestCompletedPersistenceOperation(
                GatewayIntegrationTestPersistenceOutcome.Success(snapshot));
        }
    }

    private sealed class UnattachedArtifactStore : IGatewayIntegrationTestArtifactStore
    {
        public bool IsAttached => false;

        public GatewayIntegrationTestSnapshot? CommittedSnapshot => null;

        public IGatewayIntegrationTestPersistenceOperation BeginPersist(
            GatewayIntegrationTestSnapshot snapshot) =>
            throw new AssertionException("An unattached artifact store must not be written.");
    }

    private sealed class FixedReadiness : IGatewayIntegrationTestReadiness
    {
        public FixedReadiness(bool isMainThread, bool ready)
        {
            IsMainThread = isMainThread;
            Ready = ready;
        }

        public bool IsMainThread { get; }

        public bool Ready { get; set; }

        public bool IsReady(RunAt lifecyclePoint) => Ready;
    }

    private sealed class RecordingReadiness : IGatewayIntegrationTestReadiness
    {
        private readonly bool isMainThread;
        private readonly bool ready;

        public RecordingReadiness(bool isMainThread, bool ready)
        {
            this.isMainThread = isMainThread;
            this.ready = ready;
        }

        public bool IsMainThread => isMainThread;

        public int ReadyProbeCount { get; private set; }

        public bool IsReady(RunAt lifecyclePoint)
        {
            ReadyProbeCount++;
            return ready;
        }
    }

    private enum EmittedMethodBehavior
    {
        Pass,
        Throw,
        ThrowHostileException,
        InvalidReturn,
        CaptureThread,
        InvalidAsyncVoid
    }

    private sealed class EmittedIntegrationAssembly
    {
        private readonly FieldInfo callsField;
        private readonly FieldInfo invocationThreadField;

        private EmittedIntegrationAssembly(
            Assembly assembly,
            FieldInfo callsField,
            FieldInfo invocationThreadField)
        {
            Assembly = assembly;
            this.callsField = callsField;
            this.invocationThreadField = invocationThreadField;
        }

        public Assembly Assembly { get; }

        public IReadOnlyList<string> Calls => ((List<string>)callsField.GetValue(null)!).ToArray();

        public int InvocationThreadId => (int)invocationThreadField.GetValue(null)!;

        public static EmittedIntegrationAssembly Create(
            params (string Name, RunAt RunAt, EmittedMethodBehavior Behavior)[] methods)
        {
            var assemblyName = new AssemblyName("GatewayIntegrationFixture_" + Guid.NewGuid().ToString("N"));
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule(assemblyName.Name!);
            var type = module.DefineType(
                "Fixture",
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            var calls = type.DefineField(
                "Calls",
                typeof(List<string>),
                FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly);
            var invocationThread = type.DefineField(
                "InvocationThreadId",
                typeof(int),
                FieldAttributes.Public | FieldAttributes.Static);
            var initializer = type.DefineTypeInitializer().GetILGenerator();
            initializer.Emit(OpCodes.Newobj, typeof(List<string>).GetConstructor(Type.EmptyTypes)!);
            initializer.Emit(OpCodes.Stsfld, calls);
            initializer.Emit(OpCodes.Ret);

            var attributeConstructor = typeof(IntegrationTestAttribute).GetConstructor(new[] { typeof(RunAt) })!;
            foreach (var specification in methods)
            {
                var returnType = specification.Behavior == EmittedMethodBehavior.InvalidReturn
                    ? typeof(int)
                    : typeof(void);
                var method = type.DefineMethod(
                    specification.Name,
                    MethodAttributes.Public | MethodAttributes.Static,
                    returnType,
                    Type.EmptyTypes);
                method.SetCustomAttribute(new CustomAttributeBuilder(
                    attributeConstructor,
                    new object[] { specification.RunAt }));
                if (specification.Behavior == EmittedMethodBehavior.InvalidAsyncVoid)
                {
                    method.SetCustomAttribute(new CustomAttributeBuilder(
                        typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute)
                            .GetConstructor(new[] { typeof(Type) })!,
                        new object[] { typeof(object) }));
                }
                var il = method.GetILGenerator();
                if (specification.Behavior == EmittedMethodBehavior.Throw)
                {
                    il.Emit(OpCodes.Ldstr, specification.Name + " failed");
                    il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!);
                    il.Emit(OpCodes.Throw);
                }
                else if (specification.Behavior == EmittedMethodBehavior.ThrowHostileException)
                {
                    il.Emit(OpCodes.Newobj, typeof(HostileFormattingException).GetConstructor(Type.EmptyTypes)!);
                    il.Emit(OpCodes.Throw);
                }
                else if (specification.Behavior == EmittedMethodBehavior.InvalidReturn)
                {
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    il.Emit(OpCodes.Ldsfld, calls);
                    il.Emit(OpCodes.Ldstr, specification.Name);
                    il.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod(nameof(List<string>.Add))!);
                    if (specification.Behavior == EmittedMethodBehavior.CaptureThread)
                    {
                        il.Emit(OpCodes.Call, typeof(Thread).GetProperty(nameof(Thread.CurrentThread))!.GetMethod!);
                        il.Emit(OpCodes.Callvirt, typeof(Thread).GetProperty(nameof(Thread.ManagedThreadId))!.GetMethod!);
                        il.Emit(OpCodes.Stsfld, invocationThread);
                    }

                    il.Emit(OpCodes.Ret);
                }
            }

            var createdType = type.CreateType();
            return new EmittedIntegrationAssembly(
                assembly,
                createdType.GetField("Calls")!,
                createdType.GetField("InvocationThreadId")!);
        }
    }

    public sealed class HostileFormattingException : Exception
    {
        public override string Message => throw new InvalidOperationException("Message formatting failed.");

        public override string ToString() => throw new InvalidOperationException("ToString formatting failed.");
    }

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };

        public object CaptureUiState() => new { };
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "RimWorldDevGateway.RunnerTests",
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
