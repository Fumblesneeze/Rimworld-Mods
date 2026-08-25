using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class VerseGatewayEndToEndEscapeMenuActionsTests
{
    [Test]
    public void Open_uses_the_native_menu_action_and_verifies_the_result()
    {
        var runtime = new RecordingRuntime { PlayerControlledPlayableGame = true };

        var outcome = VerseGatewayEndToEndEscapeMenuActions.Apply(
            new EscapeMenuActionStep("open", open: true),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(runtime.OpenCalls, Is.EqualTo(1));
            Assert.That(runtime.CloseCalls, Is.Zero);
            Assert.That(runtime.EscapeMenuOpen, Is.True);
        });
    }

    [Test]
    public void Close_uses_the_native_escape_path_and_verifies_the_result()
    {
        var runtime = new RecordingRuntime
        {
            PlayerControlledPlayableGame = true,
            EscapeMenuOpen = true
        };

        var outcome = VerseGatewayEndToEndEscapeMenuActions.Apply(
            new EscapeMenuActionStep("close", open: false),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(runtime.OpenCalls, Is.Zero);
            Assert.That(runtime.CloseCalls, Is.EqualTo(1));
            Assert.That(runtime.EscapeMenuOpen, Is.False);
        });
    }

    [Test]
    public void Unavailable_player_game_fails_without_touching_the_menu()
    {
        var runtime = new RecordingRuntime();

        var outcome = VerseGatewayEndToEndEscapeMenuActions.Apply(
            new EscapeMenuActionStep("open", open: true),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("escape_menu_player_control_required"));
            Assert.That(runtime.OpenCalls, Is.Zero);
            Assert.That(runtime.CloseCalls, Is.Zero);
        });
    }

    [Test]
    public void Close_requires_the_escape_menu_to_be_current()
    {
        var runtime = new RecordingRuntime { PlayerControlledPlayableGame = true };

        var outcome = VerseGatewayEndToEndEscapeMenuActions.Apply(
            new EscapeMenuActionStep("close", open: false),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("escape_menu_not_open"));
            Assert.That(runtime.CloseCalls, Is.Zero);
        });
    }

    [TestCase(true, "escape_menu_open_failed")]
    [TestCase(false, "escape_menu_close_failed")]
    public void Native_action_must_reach_the_requested_postcondition(bool open, string failureCode)
    {
        var runtime = new RecordingRuntime
        {
            PlayerControlledPlayableGame = true,
            EscapeMenuOpen = !open,
            IgnoreMutation = true
        };

        var outcome = VerseGatewayEndToEndEscapeMenuActions.Apply(
            new EscapeMenuActionStep("action", open),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo(failureCode));
        });
    }

    private sealed class RecordingRuntime : IGatewayEndToEndEscapeMenuRuntime
    {
        public bool PlayerControlledPlayableGame { get; set; }

        public bool EscapeMenuOpen { get; set; }

        public bool IgnoreMutation { get; set; }

        public int OpenCalls { get; private set; }

        public int CloseCalls { get; private set; }

        public void OpenEscapeMenu()
        {
            OpenCalls++;
            if (!IgnoreMutation)
            {
                EscapeMenuOpen = true;
            }
        }

        public void CloseEscapeMenu()
        {
            CloseCalls++;
            if (!IgnoreMutation)
            {
                EscapeMenuOpen = false;
            }
        }
    }
}
