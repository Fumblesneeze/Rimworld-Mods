using NUnit.Framework;
using RimWorldDevGateway.Contracts;
using Verse;
using System.Runtime.Serialization;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndTestIsolationTests
{
    [Test]
    public void Map_reset_removes_every_roof_before_clearing_any_map_content()
    {
        Assert.That(
            VerseGatewayEndToEndIsolationOperations.ResetPhaseOrder,
            Is.EqualTo(new[]
            {
                GatewayEndToEndMapResetPhase.Roofs,
                GatewayEndToEndMapResetPhase.Designations,
                GatewayEndToEndMapResetPhase.Zones,
                GatewayEndToEndMapResetPhase.Things,
                GatewayEndToEndMapResetPhase.Notifications
            }));
    }

    [Test]
    public void Notification_reset_clears_every_surface_after_map_removal_and_verifies_empty()
    {
        var operations = new RecordingNotificationOperations();
        var reset = new GatewayEndToEndNotificationReset(operations);

        reset.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(operations.Calls, Is.EqualTo(new[]
            {
                "letters", "messages", "alerts"
            }));
            Assert.That(reset.IsEmpty(), Is.True);
        });
    }

    [Test]
    public void Notification_reset_requires_every_surface_to_be_empty()
    {
        foreach (var remaining in new[] { "messages", "letters", "alerts" })
        {
            var operations = new RecordingNotificationOperations { Remaining = remaining };

            Assert.That(
                new GatewayEndToEndNotificationReset(operations).IsEmpty(),
                Is.False,
                remaining);
        }
    }

    [Test]
    public void Notification_reset_pins_the_current_delayed_letter_and_active_alert_collection_shapes()
    {
        Assert.That(
            VerseGatewayEndToEndNotificationOperations.PrivateShapesAvailable,
            Is.True);
    }

    [Test]
    public void Roof_reset_policy_includes_constructed_and_overhead_mountain_roofs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(VerseGatewayEndToEndIsolationOperations.ShouldRemoveRoof(null), Is.False);
            Assert.That(
                VerseGatewayEndToEndIsolationOperations.ShouldRemoveRoof(
                    (RoofDef)FormatterServices.GetUninitializedObject(typeof(RoofDef))),
                Is.True);
        });
    }

    [Test]
    public void Permanent_map_features_are_baseline_environment_not_disposable_test_state()
    {
        var permanent = UninitializedThing(destroyable: false);
        var disposable = UninitializedThing(destroyable: true);

        Assert.Multiple(() =>
        {
            Assert.That(VerseGatewayEndToEndIsolationOperations.IsDisposable(permanent), Is.False);
            Assert.That(VerseGatewayEndToEndIsolationOperations.IsDisposable(disposable), Is.True);
        });
    }

    [Test]
    public void Isolation_pause_uses_pause_semantics_without_the_player_control_speed_setter()
    {
        GatewayGameStateMutationRequest request =
            VerseGatewayEndToEndIsolationOperations.CreatePauseRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.Paused, Is.True);
            Assert.That(request.Speed, Is.Null);
        });
    }

    private static Thing UninitializedThing(bool destroyable)
    {
        var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        def.destroyable = destroyable;
        var thing = (Thing)FormatterServices.GetUninitializedObject(typeof(Thing));
        thing.def = def;
        return thing;
    }

    [Test]
    public void Prepare_captures_baseline_then_pauses_resets_and_proves_empty()
    {
        var operations = new RecordingIsolationOperations();
        var isolation = new GatewayEndToEndTestIsolation(operations);

        isolation.Prepare(Context());

        Assert.That(operations.Calls, Is.EqualTo(new[]
        {
            "capture", "pause", "reset", "verify"
        }));
    }

    [Test]
    public void Prepare_throws_when_the_disposable_map_cannot_be_proved_empty()
    {
        var operations = new RecordingIsolationOperations { Empty = false };
        var isolation = new GatewayEndToEndTestIsolation(operations);

        var error = Assert.Throws<InvalidOperationException>(() => isolation.Prepare(Context()));

        Assert.That(error!.Message, Does.Contain("empty baseline"));
    }

    [Test]
    public void Cleanup_resets_before_restoring_and_verifies_the_restored_baseline()
    {
        var operations = new RecordingIsolationOperations();
        var isolation = new GatewayEndToEndTestIsolation(operations);
        isolation.Prepare(Context());
        operations.Calls.Clear();

        var trustworthy = isolation.Cleanup(Context());

        Assert.Multiple(() =>
        {
            Assert.That(trustworthy, Is.True);
            Assert.That(operations.Calls, Is.EqualTo(new[]
            {
                "pause", "reset", "verify", "restore", "verify"
            }));
            Assert.That(operations.Restored, Is.SameAs(operations.Baseline));
        });
    }

    [Test]
    public void Cleanup_returns_false_for_any_reset_restore_or_verification_failure()
    {
        foreach (var failure in new[] { "reset", "verify", "restore" })
        {
            var operations = new RecordingIsolationOperations { Failure = failure };
            var isolation = new GatewayEndToEndTestIsolation(operations);
            operations.Failure = null;
            isolation.Prepare(Context());
            operations.Failure = failure;

            Assert.That(isolation.Cleanup(Context()), Is.False, failure);
            Assert.That(operations.Calls, Does.Contain("restore"), failure + " must still restore");
        }
    }

    [Test]
    public void Cleanup_without_a_prepared_baseline_is_untrustworthy()
    {
        var isolation = new GatewayEndToEndTestIsolation(new RecordingIsolationOperations());

        Assert.That(isolation.Cleanup(Context()), Is.False);
    }

    private static GatewayEndToEndTestContext Context() => new(() => 0, () => 0, _ => null);

    private sealed class RecordingIsolationOperations : IGatewayEndToEndIsolationOperations
    {
        public bool IsReady => true;

        public GatewayEndToEndIsolationBaseline Baseline { get; } = new(new object());

        public List<string> Calls { get; } = new();

        public bool Empty { get; set; } = true;

        public string? Failure { get; set; }

        public GatewayEndToEndIsolationBaseline? Restored { get; private set; }

        public GatewayEndToEndIsolationBaseline CaptureBaseline()
        {
            Calls.Add("capture");
            return Baseline;
        }

        public void Pause()
        {
            Calls.Add("pause");
        }

        public void ResetTransientState()
        {
            Calls.Add("reset");
            if (Failure == "reset")
            {
                throw new InvalidOperationException("reset failed");
            }
        }

        public bool VerifyEmpty()
        {
            Calls.Add("verify");
            if (Failure == "verify")
            {
                return false;
            }

            return Empty;
        }

        public bool RestoreBaseline(GatewayEndToEndIsolationBaseline baseline)
        {
            Calls.Add("restore");
            Restored = baseline;
            return Failure != "restore";
        }
    }

    private sealed class RecordingNotificationOperations : IGatewayEndToEndNotificationOperations
    {
        public List<string> Calls { get; } = new();

        public string? Remaining { get; set; }

        public bool MessagesEmpty => Remaining != "messages";

        public bool LettersEmpty => Remaining != "letters";

        public bool AlertsEmpty => Remaining != "alerts";

        public void ClearMessages() => Calls.Add("messages");

        public void ClearLetters() => Calls.Add("letters");

        public void ClearAlerts() => Calls.Add("alerts");
    }
}
