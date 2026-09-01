using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;
using System.Text;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndExecutionStateMachineTests
{
    [SetUp]
    public void ResetFixtures()
    {
        DurableWorkflowTest.Reset();
        AssertionFailureTest.Reset();
        PassingTest.Reset();
        NeverCompletesTest.Reset();
        PendingCleanupTest.Reset();
        NeverCompletingCleanupTest.Reset();
        ThrowingAbovePendingCleanupTest.Reset();
    }

    [Test]
    public void Arrange_and_each_step_wait_for_their_exact_pre_invocation_snapshot_to_be_durable()
    {
        var clock = new FakeClock();
        var driver = new RecordingDriver();
        var machine = Machine(clock, driver, Descriptor<DurableWorkflowTest>("durable"));

        machine.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("arranging"));
            Assert.That(machine.PersistencePending, Is.True);
            Assert.That(DurableWorkflowTest.ConstructorCount, Is.Zero);
        });

        machine.Advance();
        Assert.That(DurableWorkflowTest.ConstructorCount, Is.Zero, "Unpersisted arranging state invoked test code.");

        machine.ConfirmPersisted(machine.Snapshot);
        clock.NextFrame();
        machine.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(DurableWorkflowTest.ConstructorCount, Is.EqualTo(1));
            Assert.That(DurableWorkflowTest.ArrangeCount, Is.EqualTo(1));
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("running"));
            Assert.That(DurableWorkflowTest.ExecuteCount, Is.Zero);
        });

        machine.ConfirmPersisted(machine.Snapshot);
        clock.NextFrame();
        machine.Advance();
        Assert.That(DurableWorkflowTest.ExecuteCount, Is.Zero,
            "Iterator bodies do not execute until the first MoveNext transition.");
        Assert.That(machine.PersistencePending, Is.False);

        clock.NextFrame();
        machine.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(machine.Snapshot.CurrentStep!.Name, Is.EqualTo("native action"));
            Assert.That(machine.Snapshot.CurrentStep.Status, Is.EqualTo("running"));
            Assert.That(DurableWorkflowTest.ExecuteCount, Is.EqualTo(1));
            Assert.That(driver.BeginCount, Is.Zero);
            Assert.That(machine.PersistencePending, Is.True);
        });

        machine.ConfirmPersisted(machine.Snapshot);
        clock.NextFrame();
        machine.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(driver.BeginCount, Is.EqualTo(1));
            Assert.That(machine.Snapshot.CurrentStep!.Status, Is.EqualTo("passed"));
            Assert.That(machine.PersistencePending, Is.True);
        });
    }

    [Test]
    public void Wait_predicate_advances_once_per_frame_and_passes_only_when_observed_true()
    {
        var clock = new FakeClock();
        var machine = Machine(clock, new RecordingDriver(), Descriptor<DurableWorkflowTest>("wait"));
        AdvanceUntil(machine, clock, snapshot => snapshot.CurrentStep?.Name == "observable wait");
        machine.ConfirmPersisted(machine.Snapshot);

        for (var frame = 0; frame < 2; frame++)
        {
            clock.NextFrame();
            machine.Advance();
            Assert.That(DurableWorkflowTest.PredicateCount, Is.EqualTo(frame + 1));
            Assert.That(machine.Snapshot.CurrentStep!.Status, Is.EqualTo("running"));
            Assert.That(machine.PersistencePending, Is.False);
        }

        clock.NextFrame();
        machine.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(DurableWorkflowTest.PredicateCount, Is.EqualTo(3));
            Assert.That(machine.Snapshot.CurrentStep!.Status, Is.EqualTo("passed"));
            Assert.That(machine.PersistencePending, Is.True);
        });
    }

    [Test]
    public void Scheduled_time_control_waits_then_dispatches_the_native_action_on_the_exact_game_tick()
    {
        var clock = new FakeClock();
        var driver = new RecordingDriver();
        var machine = Machine(clock, driver, Descriptor<ScheduledTimeControlTest>("scheduled-time"));
        AdvanceUntil(machine, clock, snapshot => snapshot.CurrentStep?.Name == "pause on the exact capture frame");
        machine.ConfirmPersisted(machine.Snapshot);

        while (clock.GameTick < 99)
        {
            clock.NextFrame(gameTicks: Math.Min(10, 99 - clock.GameTick));
            machine.Advance();
        }

        Assert.Multiple(() =>
        {
            Assert.That(clock.GameTick, Is.EqualTo(99));
            Assert.That(driver.BeginCount, Is.Zero);
            Assert.That(machine.Snapshot.CurrentStep!.Status, Is.EqualTo("running"));
        });

        clock.NextFrame();
        machine.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(driver.BeginCount, Is.EqualTo(1));
            Assert.That(driver.LastStep, Is.TypeOf<TimeControlActionStep>());
            Assert.That(machine.Snapshot.CurrentStep!.Status, Is.EqualTo("passed"));
        });
    }

    [Test]
    public void Scheduled_time_control_times_out_without_dispatch_when_the_target_tick_is_not_reached()
    {
        var clock = new FakeClock();
        var driver = new RecordingDriver();
        var machine = Machine(clock, driver, Descriptor<ScheduledTimeControlTimeoutTest>("scheduled-timeout"));
        AdvanceUntil(machine, clock, snapshot => snapshot.CurrentStep?.Status == "running");
        machine.ConfirmPersisted(machine.Snapshot);

        for (var frame = 0; frame < 4 && machine.Snapshot.CurrentTest!.Status == "running"; frame++)
        {
            clock.NextFrame(gameTicks: 0, wallClock: TimeSpan.FromMilliseconds(1));
            machine.Advance();
        }

        Assert.Multiple(() =>
        {
            Assert.That(driver.BeginCount, Is.Zero);
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("cleaning"));
            Assert.That(machine.Snapshot.CurrentTest.Failure!.Kind, Is.EqualTo("timeout"));
            Assert.That(machine.Snapshot.CurrentStep!.Status, Is.EqualTo("timed_out"));
        });
    }

    [Test]
    public void Scheduled_time_control_does_not_dispatch_when_the_target_is_first_seen_after_the_deadline()
    {
        var clock = new FakeClock();
        var driver = new RecordingDriver();
        var machine = Machine(
            clock,
            driver,
            Descriptor<ScheduledTimeControlCrossedAfterDeadlineTest>("scheduled-crossed-after-deadline"));
        AdvanceUntil(machine, clock, snapshot => snapshot.CurrentStep?.Status == "running");
        machine.ConfirmPersisted(machine.Snapshot);

        clock.NextFrame(gameTicks: 0);
        machine.Advance();
        clock.NextFrame(gameTicks: 0);
        machine.Advance();
        clock.NextFrame(gameTicks: 100);
        machine.Advance();

        Assert.Multiple(() =>
        {
            Assert.That(driver.BeginCount, Is.Zero);
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("cleaning"));
            Assert.That(machine.Snapshot.CurrentStep!.Status, Is.EqualTo("timed_out"));
        });
    }

    [Test]
    public void Wait_timeout_uses_frame_tick_or_wall_deadline_and_enters_durable_cleanup()
    {
        var clock = new FakeClock();
        var machine = Machine(clock, new RecordingDriver(), Descriptor<NeverCompletesTest>("timeout"));
        AdvanceUntil(machine, clock, snapshot => snapshot.CurrentStep?.Status == "running");
        machine.ConfirmPersisted(machine.Snapshot);

        for (var frame = 0; frame < 4 && machine.Snapshot.CurrentTest!.Status == "running"; frame++)
        {
            clock.NextFrame(gameTicks: 0, wallClock: TimeSpan.FromMilliseconds(1));
            machine.Advance();
        }

        Assert.Multiple(() =>
        {
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("cleaning"));
            Assert.That(machine.Snapshot.CurrentTest.Failure!.Kind, Is.EqualTo("timeout"));
            Assert.That(machine.Snapshot.CurrentStep!.Status, Is.EqualTo("timed_out"));
            Assert.That(machine.PersistencePending, Is.True);
        });
    }

    [Test]
    public void Assertion_failure_isolated_by_successful_cleanup_and_later_test_still_passes()
    {
        var clock = new FakeClock();
        var isolation = new RecordingIsolation();
        var machine = Machine(
            clock,
            new RecordingDriver(),
            isolation,
            Descriptor<AssertionFailureTest>("a-fails"),
            Descriptor<PassingTest>("b-passes"));

        AdvanceToTerminal(machine, clock);
        var firstFailure = machine.Snapshot.Results[0].Failure!;

        Assert.Multiple(() =>
        {
            Assert.That(machine.Snapshot.GroupState, Is.EqualTo("completed_with_failures"));
            Assert.That(machine.Snapshot.Results.Select(result => result.Id),
                Is.EqualTo(new[] { "a-fails", "b-passes" }));
            Assert.That(machine.Snapshot.Results.Select(result => result.Status),
                Is.EqualTo(new[] { "failed", "passed" }));
            Assert.That(firstFailure.Kind, Is.EqualTo("assertion"));
            Assert.That(firstFailure.Message, Is.EqualTo("expected failure"));
            Assert.That(PassingTest.ArrangeCount, Is.EqualTo(1));
            Assert.That(isolation.CleanupCount, Is.EqualTo(2));
            Assert.That(machine.Snapshot.Results.Select(result => result.CleanupState),
                Is.EqualTo(new[] { "passed", "passed" }));
            Assert.That(machine.Snapshot.ProcessTainted, Is.False);
        });
    }

    [Test]
    public void Deferred_cleanup_is_polled_across_frames_before_isolation_and_reuse()
    {
        var clock = new FakeClock();
        var isolation = new RecordingIsolation();
        var machine = Machine(
            clock,
            new RecordingDriver(),
            isolation,
            Descriptor<PendingCleanupTest>("pending-cleanup"));
        AdvanceUntil(machine, clock, snapshot => snapshot.CurrentTest?.Status == "cleaning");
        machine.ConfirmPersisted(machine.Snapshot);

        clock.NextFrame();
        machine.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(PendingCleanupTest.PollCount, Is.EqualTo(1));
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("cleaning"));
            Assert.That(isolation.CleanupCount, Is.Zero);
        });
        clock.NextFrame();
        machine.Advance();
        Assert.That(PendingCleanupTest.PollCount, Is.EqualTo(2));
        clock.NextFrame();
        machine.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(PendingCleanupTest.PollCount, Is.EqualTo(3));
            Assert.That(isolation.CleanupCount, Is.EqualTo(1));
            Assert.That(machine.Snapshot.CurrentTest!.CleanupState, Is.EqualTo("passed"));
        });
    }

    [Test]
    public void Deferred_cleanup_timeout_taints_the_process_instead_of_claiming_reusable_state()
    {
        var clock = new FakeClock();
        var isolation = new RecordingIsolation();
        var machine = Machine(
            clock,
            new RecordingDriver(),
            isolation,
            Descriptor<NeverCompletingCleanupTest>("cleanup-timeout"));
        AdvanceUntil(machine, clock, snapshot => snapshot.CurrentTest?.Status == "cleaning");
        machine.ConfirmPersisted(machine.Snapshot);
        clock.NextFrame(wallClock: TimeSpan.FromMilliseconds(1));
        machine.Advance();
        clock.NextFrame(wallClock: TimeSpan.FromSeconds(31));
        machine.Advance();

        Assert.Multiple(() =>
        {
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("infrastructure_failed"));
            Assert.That(machine.Snapshot.CurrentTest.CleanupState, Is.EqualTo("unverifiable"));
            Assert.That(machine.Snapshot.CurrentTest.Failure!.Code, Is.EqualTo("cleanup_unverifiable"));
            Assert.That(NeverCompletingCleanupTest.PollCount, Is.EqualTo(2));
            Assert.That(isolation.CleanupCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Throwing_later_cleanup_does_not_erase_an_earlier_pending_owner()
    {
        var clock = new FakeClock();
        var isolation = new RecordingIsolation();
        var machine = Machine(
            clock,
            new RecordingDriver(),
            isolation,
            Descriptor<ThrowingAbovePendingCleanupTest>("throwing-above-pending"));
        AdvanceUntil(machine, clock, snapshot => snapshot.CurrentTest?.Status == "cleaning");
        machine.ConfirmPersisted(machine.Snapshot);
        clock.NextFrame();
        machine.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(ThrowingAbovePendingCleanupTest.ThrowCount, Is.EqualTo(1));
            Assert.That(ThrowingAbovePendingCleanupTest.PollCount, Is.EqualTo(1));
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("cleaning"));
        });
        clock.NextFrame();
        machine.Advance();
        Assert.Multiple(() =>
        {
            Assert.That(ThrowingAbovePendingCleanupTest.PollCount, Is.EqualTo(2));
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("infrastructure_failed"));
            Assert.That(machine.Snapshot.CurrentTest.CleanupState, Is.EqualTo("unverifiable"));
            Assert.That(isolation.CleanupCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Cleanup_failure_taints_the_process_and_skips_every_remaining_test()
    {
        var clock = new FakeClock();
        var isolation = new RecordingIsolation { CleanupIsTrustworthy = false };
        var machine = Machine(
            clock,
            new RecordingDriver(),
            isolation,
            Descriptor<PassingTest>("a-taints"),
            Descriptor<DurableWorkflowTest>("b-skipped"));

        AdvanceToTerminal(machine, clock);

        Assert.Multiple(() =>
        {
            Assert.That(machine.Snapshot.ProcessTainted, Is.True);
            Assert.That(machine.Snapshot.GroupState, Is.EqualTo("infrastructure_failed"));
            Assert.That(machine.Snapshot.Results.Select(result => result.Status),
                Is.EqualTo(new[] { "infrastructure_failed", "skipped" }));
            Assert.That(machine.Snapshot.Results[0].CleanupState, Is.EqualTo("unverifiable"));
            Assert.That(DurableWorkflowTest.ConstructorCount, Is.Zero);
        });
    }

    [Test]
    public void Only_the_exact_snapshot_instance_can_release_a_persistence_barrier()
    {
        var machine = Machine(new FakeClock(), new RecordingDriver(), Descriptor<PassingTest>("exact"));
        machine.Advance();
        var different = new GatewayEndToEndExecutionSnapshot("running");

        Assert.Throws<InvalidOperationException>(() => machine.ConfirmPersisted(different));
        Assert.That(machine.PersistencePending, Is.True);
        machine.ConfirmPersisted(machine.Snapshot);
        Assert.That(machine.PersistencePending, Is.False);
    }

    [Test]
    public void Arrange_waits_for_player_control_before_capturing_the_isolation_baseline()
    {
        var clock = new FakeClock();
        var isolation = new RecordingIsolation { IsReady = false };
        var machine = Machine(
            clock,
            new RecordingDriver(),
            isolation,
            Descriptor<PassingTest>("wait-for-player-control"));

        machine.Advance();
        machine.ConfirmPersisted(machine.Snapshot);
        for (var frame = 0; frame < 3; frame++)
        {
            clock.NextFrame();
            machine.Advance();
        }

        Assert.Multiple(() =>
        {
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("arranging"));
            Assert.That(isolation.PrepareCount, Is.Zero);
            Assert.That(PassingTest.ArrangeCount, Is.Zero);
            Assert.That(machine.Snapshot.ProcessTainted, Is.False);
        });

        isolation.IsReady = true;
        clock.NextFrame();
        machine.Advance();

        Assert.Multiple(() =>
        {
            Assert.That(isolation.PrepareCount, Is.EqualTo(1));
            Assert.That(PassingTest.ArrangeCount, Is.EqualTo(1));
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("running"));
        });
    }

    [Test]
    public void Player_control_readiness_wait_is_bounded_by_the_test_deadline()
    {
        var clock = new FakeClock();
        var isolation = new RecordingIsolation { IsReady = false };
        var machine = Machine(
            clock,
            new RecordingDriver(),
            isolation,
            Descriptor<PassingTest>("player-control-timeout"));

        machine.Advance();
        machine.ConfirmPersisted(machine.Snapshot);
        clock.NextFrame(gameTicks: 0, wallClock: TimeSpan.FromSeconds(31));
        machine.Advance();

        Assert.Multiple(() =>
        {
            Assert.That(machine.Snapshot.CurrentTest!.Status, Is.EqualTo("infrastructure_failed"));
            Assert.That(machine.Snapshot.CurrentTest.Failure!.Code,
                Is.EqualTo("player_control_not_ready"));
            Assert.That(machine.Snapshot.CurrentTest.CleanupState, Is.EqualTo("not_run"));
            Assert.That(machine.Snapshot.ProcessTainted, Is.True);
            Assert.That(isolation.PrepareCount, Is.Zero);
        });
    }

    [Test]
    public void Failed_step_diagnostics_are_credential_free_and_utf8_bounded_before_persistence()
    {
        const string credential = "SESSION-CREDENTIAL-DO-NOT-RETAIN";
        var clock = new FakeClock();
        var driver = new RecordingDriver
        {
            Operation = GatewayEndToEndCompletedStepOperation.Failed(
                "native_rejection",
                credential + new string('\u20ac', 10_000))
        };
        var machine = new GatewayEndToEndExecutionStateMachine(
            new[] { Descriptor<DurableWorkflowTest>("bounded-failure") },
            clock,
            () => new GatewayEndToEndTestContext(
                () => clock.FrameCount,
                () => clock.GameTick,
                _ => null),
            driver,
            new RecordingIsolation(),
            credential);

        AdvanceUntil(machine, clock, snapshot => snapshot.CurrentTest?.Failure is not null);
        var failure = machine.Snapshot.CurrentTest!.Failure!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Not.Contain(credential));
            Assert.That(
                Encoding.UTF8.GetByteCount(failure.Message),
                Is.LessThanOrEqualTo(GatewayEndToEndExecutionStateMachine.MaximumFailureMessageUtf8Bytes));
            Assert.That(failure.Message, Does.EndWith("..."));
        });
    }

    [Test]
    public void Readiness_cleanup_and_skipped_failures_all_use_the_central_credential_sanitizer()
    {
        const string credential = "process";
        var clock = new FakeClock();
        var isolation = new RecordingIsolation { CleanupIsTrustworthy = false };
        var machine = new GatewayEndToEndExecutionStateMachine(
            new[]
            {
                Descriptor<PassingTest>("a-cleanup-fails"),
                Descriptor<DurableWorkflowTest>("b-skipped")
            },
            clock,
            () => new GatewayEndToEndTestContext(
                () => clock.FrameCount,
                () => clock.GameTick,
                _ => null),
            new RecordingDriver(),
            isolation,
            credential);

        AdvanceToTerminal(machine, clock);

        Assert.That(
            machine.Snapshot.Results
                .SelectMany(result => new[] { result.Failure, result.CleanupFailure })
                .Where(failure => failure is not null)
                .Select(failure => failure!.Message),
            Has.None.Contains(credential));

        const string readinessCredential = "RimWorld";
        var readinessClock = new FakeClock();
        var readinessMachine = new GatewayEndToEndExecutionStateMachine(
            new[] { Descriptor<PassingTest>("readiness-fails") },
            readinessClock,
            () => new GatewayEndToEndTestContext(
                () => readinessClock.FrameCount,
                () => readinessClock.GameTick,
                _ => null),
            new RecordingDriver(),
            new RecordingIsolation { IsReady = false },
            readinessCredential);
        readinessMachine.Advance();
        readinessMachine.ConfirmPersisted(readinessMachine.Snapshot);
        readinessClock.NextFrame(gameTicks: 0, wallClock: TimeSpan.FromSeconds(31));
        readinessMachine.Advance();

        Assert.That(
            readinessMachine.Snapshot.CurrentTest!.Failure!.Message,
            Does.Not.Contain(readinessCredential));
    }

    private static GatewayEndToEndExecutionStateMachine Machine(
        FakeClock clock,
        RecordingDriver driver,
        params GatewayEndToEndRuntimeTestDescriptor[] tests) =>
        Machine(clock, driver, new RecordingIsolation(), tests);

    private static GatewayEndToEndExecutionStateMachine Machine(
        FakeClock clock,
        RecordingDriver driver,
        RecordingIsolation isolation,
        params GatewayEndToEndRuntimeTestDescriptor[] tests) => new(
        tests,
        clock,
        () => new GatewayEndToEndTestContext(
            () => clock.FrameCount,
            () => clock.GameTick,
            _ => null),
        driver,
        isolation);

    private static GatewayEndToEndRuntimeTestDescriptor Descriptor<T>(string id)
        where T : IRimWorldEndToEndTest => new(
        id,
        "alpha.mod",
        new[] { "ludeon.rimworld", "alpha.mod" },
        typeof(T).FullName!,
        100,
        200,
        30,
        typeof(T));

    private static void AdvanceUntil(
        GatewayEndToEndExecutionStateMachine machine,
        FakeClock clock,
        Func<GatewayEndToEndExecutionSnapshot, bool> reached)
    {
        for (var attempt = 0; attempt < 64 && !reached(machine.Snapshot); attempt++)
        {
            if (machine.PersistencePending)
            {
                machine.ConfirmPersisted(machine.Snapshot);
            }

            clock.NextFrame();
            machine.Advance();
        }

        Assert.That(reached(machine.Snapshot), Is.True, "The expected E2E state was not reached.");
    }

    private static void AdvanceToTerminal(GatewayEndToEndExecutionStateMachine machine, FakeClock clock)
    {
        for (var attempt = 0; attempt < 256 && !machine.Snapshot.IsTerminal; attempt++)
        {
            if (machine.PersistencePending)
            {
                machine.ConfirmPersisted(machine.Snapshot);
            }

            clock.NextFrame();
            machine.Advance();
        }

        Assert.That(machine.Snapshot.IsTerminal, Is.True, "The E2E group did not terminate.");
    }

    private sealed class FakeClock : IGatewayEndToEndClock
    {
        public long FrameCount { get; private set; }

        public int GameTick { get; private set; }

        public DateTimeOffset UtcNow { get; private set; } = DateTimeOffset.Parse("2026-08-05T00:00:00Z");

        public void NextFrame(int gameTicks = 1, TimeSpan? wallClock = null)
        {
            FrameCount++;
            GameTick += gameTicks;
            UtcNow += wallClock ?? TimeSpan.FromMilliseconds(16);
        }
    }

    private sealed class RecordingDriver : IGatewayEndToEndStepDriver
    {
        public int BeginCount { get; private set; }

        public IGatewayEndToEndStepOperation? Operation { get; set; }

        public EndToEndStep? LastStep { get; private set; }

        public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context)
        {
            BeginCount++;
            LastStep = step;
            return Operation ?? GatewayEndToEndCompletedStepOperation.Passed();
        }
    }

    private sealed class RecordingIsolation : IGatewayEndToEndTestIsolation
    {
        public bool IsReady { get; set; } = true;

        public bool CleanupIsTrustworthy { get; set; } = true;

        public int PrepareCount { get; private set; }

        public int CleanupCount { get; private set; }

        public void Prepare(IEndToEndContext context) => PrepareCount++;

        public bool Cleanup(IEndToEndContext context)
        {
            CleanupCount++;
            return CleanupIsTrustworthy;
        }
    }

    private sealed class DurableWorkflowTest : IRimWorldEndToEndTest
    {
        public DurableWorkflowTest() => ConstructorCount++;

        public static int ConstructorCount { get; private set; }
        public static int ArrangeCount { get; private set; }
        public static int ExecuteCount { get; private set; }
        public static int PredicateCount { get; private set; }

        public void Arrange(IEndToEndContext context) => ArrangeCount++;

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            ExecuteCount++;
            yield return new TimeControlActionStep("native action", paused: false, EndToEndGameSpeed.Normal);
            yield return new WaitUntilStep(
                "observable wait",
                _ => ++PredicateCount >= 3,
                new EndToEndDeadline(10, 10, TimeSpan.FromSeconds(1)));
            yield return new AssertionStep("final assertion", _ => EndToEndAssert.True(true));
        }

        public static void Reset()
        {
            ConstructorCount = 0;
            ArrangeCount = 0;
            ExecuteCount = 0;
            PredicateCount = 0;
        }
    }

    private sealed class NeverCompletesTest : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context)
        {
        }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield return new WaitUntilStep(
                "never",
                _ => false,
                new EndToEndDeadline(2, 100, TimeSpan.FromMinutes(1)));
        }

        public static void Reset()
        {
        }
    }

    private sealed class ScheduledTimeControlTest : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context)
        {
        }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield return new TimeControlActionStep(
                "pause on the exact capture frame",
                paused: true,
                speed: EndToEndGameSpeed.Normal,
                atGameTick: 100,
                deadline: new EndToEndDeadline(100, 200, TimeSpan.FromSeconds(2)));
        }
    }

    private sealed class ScheduledTimeControlTimeoutTest : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context)
        {
        }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield return new TimeControlActionStep(
                "never reached scheduled pause",
                paused: true,
                speed: EndToEndGameSpeed.Normal,
                atGameTick: 1_000,
                deadline: new EndToEndDeadline(2, 100, TimeSpan.FromSeconds(1)));
        }
    }

    private sealed class ScheduledTimeControlCrossedAfterDeadlineTest : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context)
        {
        }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield return new TimeControlActionStep(
                "expired before reaching scheduled pause",
                paused: true,
                speed: EndToEndGameSpeed.Normal,
                atGameTick: 100,
                deadline: new EndToEndDeadline(2, 200, TimeSpan.FromSeconds(1)));
        }
    }

    private sealed class AssertionFailureTest : IRimWorldEndToEndTest
    {
        public void Arrange(IEndToEndContext context)
        {
        }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield return new AssertionStep("fails", _ => EndToEndAssert.Fail("expected failure"));
        }

        public static void Reset()
        {
        }
    }

    private sealed class PassingTest : IRimWorldEndToEndTest
    {
        public static int ArrangeCount { get; private set; }

        public void Arrange(IEndToEndContext context) => ArrangeCount++;

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield return new AssertionStep("passes", _ => EndToEndAssert.True(true));
        }

        public static void Reset() => ArrangeCount = 0;
    }

    private sealed class PendingCleanupTest : IRimWorldEndToEndTest
    {
        public static int PollCount { get; private set; }

        public void Arrange(IEndToEndContext context) =>
            ((GatewayEndToEndTestContext)context).DeferCleanup(() => ++PollCount >= 3);

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield return new AssertionStep("passes", _ => EndToEndAssert.True(true));
        }

        public static void Reset() => PollCount = 0;
    }

    private sealed class NeverCompletingCleanupTest : IRimWorldEndToEndTest
    {
        public static int PollCount { get; private set; }

        public void Arrange(IEndToEndContext context) =>
            ((GatewayEndToEndTestContext)context).DeferCleanup(() =>
            {
                PollCount++;
                return false;
            });

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield return new AssertionStep("passes", _ => EndToEndAssert.True(true));
        }

        public static void Reset() => PollCount = 0;
    }

    private sealed class ThrowingAbovePendingCleanupTest : IRimWorldEndToEndTest
    {
        public static int PollCount { get; private set; }
        public static int ThrowCount { get; private set; }

        public void Arrange(IEndToEndContext context)
        {
            var gateway = (GatewayEndToEndTestContext)context;
            gateway.DeferCleanup(() => ++PollCount >= 2);
            gateway.DeferCleanup(() =>
            {
                ThrowCount++;
                throw new InvalidOperationException("expected cleanup failure");
            });
        }

        public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
        {
            yield return new AssertionStep("passes", _ => EndToEndAssert.True(true));
        }

        public static void Reset()
        {
            PollCount = 0;
            ThrowCount = 0;
        }
    }

}
