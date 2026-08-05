using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndTestIsolationTests
{
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
}
