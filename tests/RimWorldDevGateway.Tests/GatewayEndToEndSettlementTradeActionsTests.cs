using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndSettlementTradeActionsTests
{
    [Test]
    public void Exact_world_object_ids_open_the_native_command_and_are_reported()
    {
        var operations = new RecordingOperations();
        var actions = new GatewayEndToEndSettlementTradeActions(operations);

        var outcome = actions.Apply(new SettlementTradeActionStep("trade", 41, 42));

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(operations.SettlementWorldObjectId, Is.EqualTo(41));
            Assert.That(operations.CaravanWorldObjectId, Is.EqualTo(42));
            Assert.That(outcome.Artifacts["settlementWorldObjectId"], Is.EqualTo("41"));
            Assert.That(outcome.Artifacts["caravanWorldObjectId"], Is.EqualTo("42"));
            Assert.That(outcome.Artifacts["nativeCommand"],
                Is.EqualTo("CaravanVisitUtility.TradeCommand"));
        });
    }

    [Test]
    public void Native_resolution_failure_is_preserved_as_a_failed_step()
    {
        var actions = new GatewayEndToEndSettlementTradeActions(
            new RecordingOperations
            {
                Failure = new GatewaySettlementTradeException(
                    "settlement_trade_wrong_tile",
                    "The caravan is elsewhere.")
            });

        var outcome = actions.Apply(new SettlementTradeActionStep("trade", 41, 42));

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("settlement_trade_wrong_tile"));
            Assert.That(outcome.FailureMessage, Is.EqualTo("The caravan is elsewhere."));
        });
    }

    [Test]
    public void Exact_expected_native_failure_passes_with_a_durable_artifact()
    {
        var actions = new GatewayEndToEndSettlementTradeActions(
            new RecordingOperations
            {
                Failure = new GatewaySettlementTradeException(
                    "settlement_trade_target_mismatch",
                    "The requested settlement is not the native visit target.")
            });

        var outcome = actions.Apply(new SettlementTradeActionStep(
            "reject wrong target",
            41,
            42,
            "settlement_trade_target_mismatch"));

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(outcome.Artifacts["expectedFailureCode"],
                Is.EqualTo("settlement_trade_target_mismatch"));
            Assert.That(outcome.Artifacts["observedFailureCode"],
                Is.EqualTo("settlement_trade_target_mismatch"));
        });
    }

    [Test]
    public void Unexpected_native_failure_does_not_satisfy_the_expected_failure()
    {
        var actions = new GatewayEndToEndSettlementTradeActions(
            new RecordingOperations
            {
                Failure = new GatewaySettlementTradeException(
                    "settlement_trade_wrong_tile",
                    "The caravan is elsewhere.")
            });

        var outcome = actions.Apply(new SettlementTradeActionStep(
            "reject wrong target",
            41,
            42,
            "settlement_trade_target_mismatch"));

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("settlement_trade_unexpected_failure"));
            Assert.That(outcome.FailureMessage, Does.Contain("settlement_trade_wrong_tile"));
        });
    }

    [Test]
    public void Successful_open_does_not_satisfy_an_expected_failure()
    {
        var actions = new GatewayEndToEndSettlementTradeActions(new RecordingOperations());

        var outcome = actions.Apply(new SettlementTradeActionStep(
            "reject wrong target",
            41,
            42,
            "settlement_trade_target_mismatch"));

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("settlement_trade_expected_failure_missing"));
        });
    }

    private sealed class RecordingOperations : IGatewayEndToEndSettlementTradeOperations
    {
        public int SettlementWorldObjectId { get; private set; }

        public int CaravanWorldObjectId { get; private set; }

        public GatewaySettlementTradeException? Failure { get; set; }

        public GatewaySettlementTradeOpening Open(
            int settlementWorldObjectId,
            int caravanWorldObjectId)
        {
            SettlementWorldObjectId = settlementWorldObjectId;
            CaravanWorldObjectId = caravanWorldObjectId;
            if (Failure is not null)
            {
                throw Failure;
            }

            return new GatewaySettlementTradeOpening(
                settlementWorldObjectId,
                caravanWorldObjectId,
                "CaravanVisitUtility.TradeCommand");
        }
    }
}
