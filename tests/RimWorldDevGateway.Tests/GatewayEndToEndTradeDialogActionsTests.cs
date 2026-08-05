using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndTradeDialogActionsTests
{
    [Test]
    public void Adjustment_resolves_the_exact_thing_and_reports_native_counts()
    {
        var operations = new RecordingOperations
        {
            Adjustment = new GatewayTradeTransferAdjustment("MealFine42", 0, -1)
        };
        var actions = new GatewayEndToEndTradeDialogActions(operations);

        var outcome = actions.Apply(
            TradeDialogActionStep.AdjustTransfer("buy", "MealFine42", -1));

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(operations.AdjustRequest, Is.EqualTo(("MealFine42", -1)));
            Assert.That(outcome.Artifacts["thingRuntimeId"], Is.EqualTo("MealFine42"));
            Assert.That(outcome.Artifacts["countBefore"], Is.EqualTo("0"));
            Assert.That(outcome.Artifacts["countAfter"], Is.EqualTo("-1"));
            Assert.That(operations.AcceptCount, Is.Zero);
        });
    }

    [Test]
    public void Acceptance_invokes_only_the_native_dialog_callback()
    {
        var operations = new RecordingOperations();
        var outcome = new GatewayEndToEndTradeDialogActions(operations).Apply(
            TradeDialogActionStep.Accept("accept"));

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(operations.AcceptCount, Is.EqualTo(1));
            Assert.That(operations.AdjustRequest, Is.Null);
            Assert.That(outcome.Artifacts["nativeCallback"], Is.EqualTo("Dialog_Trade.Accept"));
        });
    }

    [TestCase("trade_dialog_missing")]
    [TestCase("trade_thing_ambiguous")]
    [TestCase("trade_transfer_out_of_range")]
    public void Native_failures_are_retained_as_failed_action_outcomes(string failureCode)
    {
        var operations = new RecordingOperations
        {
            Failure = new GatewayTradeDialogException(failureCode, "native rejection")
        };

        var outcome = new GatewayEndToEndTradeDialogActions(operations).Apply(
            TradeDialogActionStep.AdjustTransfer("buy", "MealFine42", -1));

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo(failureCode));
            Assert.That(outcome.FailureMessage, Is.EqualTo("native rejection"));
        });
    }

    private sealed class RecordingOperations : IGatewayEndToEndTradeDialogOperations
    {
        public GatewayTradeTransferAdjustment Adjustment { get; set; } =
            new("thing", 0, 1);

        public GatewayTradeDialogException? Failure { get; set; }

        public (string RuntimeId, int Delta)? AdjustRequest { get; private set; }

        public int AcceptCount { get; private set; }

        public GatewayTradeTransferAdjustment AdjustTransfer(string thingRuntimeId, int countDelta)
        {
            AdjustRequest = (thingRuntimeId, countDelta);
            if (Failure is not null)
            {
                throw Failure;
            }

            return Adjustment;
        }

        public void Accept()
        {
            AcceptCount++;
            if (Failure is not null)
            {
                throw Failure;
            }
        }
    }
}
