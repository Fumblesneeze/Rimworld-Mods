using System.Globalization;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway;

public sealed class GatewaySettlementTradeOpening
{
    public GatewaySettlementTradeOpening(
        int settlementWorldObjectId,
        int caravanWorldObjectId,
        string nativeCommand)
    {
        SettlementWorldObjectId = settlementWorldObjectId;
        CaravanWorldObjectId = caravanWorldObjectId;
        NativeCommand = string.IsNullOrWhiteSpace(nativeCommand)
            ? throw new ArgumentException("A native command identity is required.", nameof(nativeCommand))
            : nativeCommand;
    }

    public int SettlementWorldObjectId { get; }

    public int CaravanWorldObjectId { get; }

    public string NativeCommand { get; }
}

public sealed class GatewaySettlementTradeException : Exception
{
    public GatewaySettlementTradeException(string code, string message)
        : base(message)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("A failure code is required.", nameof(code))
            : code;
    }

    public GatewaySettlementTradeException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("A failure code is required.", nameof(code))
            : code;
    }

    public string Code { get; }
}

public interface IGatewayEndToEndSettlementTradeOperations
{
    GatewaySettlementTradeOpening Open(int settlementWorldObjectId, int caravanWorldObjectId);
}

public interface IGatewayEndToEndSettlementTradeActions
{
    GatewayEndToEndStepOutcome Apply(SettlementTradeActionStep step);
}

public sealed class GatewayEndToEndSettlementTradeActions : IGatewayEndToEndSettlementTradeActions
{
    private readonly IGatewayEndToEndSettlementTradeOperations operations;

    public GatewayEndToEndSettlementTradeActions(
        IGatewayEndToEndSettlementTradeOperations operations) =>
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));

    public GatewayEndToEndStepOutcome Apply(SettlementTradeActionStep step)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        try
        {
            var opened = operations.Open(
                step.SettlementWorldObjectId,
                step.CaravanWorldObjectId);
            if (step.ExpectedFailureCode is not null)
            {
                return GatewayEndToEndStepOutcome.Fail(
                    "settlement_trade_expected_failure_missing",
                    $"Expected native settlement trade to fail with '{step.ExpectedFailureCode}', but it opened successfully.");
            }

            return GatewayEndToEndStepOutcome.Pass(new Dictionary<string, string>
            {
                ["settlementWorldObjectId"] = opened.SettlementWorldObjectId.ToString(
                    CultureInfo.InvariantCulture),
                ["caravanWorldObjectId"] = opened.CaravanWorldObjectId.ToString(
                    CultureInfo.InvariantCulture),
                ["nativeCommand"] = opened.NativeCommand
            });
        }
        catch (GatewaySettlementTradeException exception)
        {
            if (step.ExpectedFailureCode is not null)
            {
                if (!string.Equals(
                        exception.Code,
                        step.ExpectedFailureCode,
                        StringComparison.Ordinal))
                {
                    return GatewayEndToEndStepOutcome.Fail(
                        "settlement_trade_unexpected_failure",
                        $"Expected native failure '{step.ExpectedFailureCode}', observed '{exception.Code}'.");
                }

                return GatewayEndToEndStepOutcome.Pass(new Dictionary<string, string>
                {
                    ["expectedFailureCode"] = step.ExpectedFailureCode,
                    ["observedFailureCode"] = exception.Code
                });
            }

            return GatewayEndToEndStepOutcome.Fail(exception.Code, exception.Message);
        }
    }
}

public sealed class VerseGatewayEndToEndSettlementTradeOperations :
    IGatewayEndToEndSettlementTradeOperations
{
    private const string NativeCommandIdentity = "CaravanVisitUtility.TradeCommand";

    public GatewaySettlementTradeOpening Open(
        int settlementWorldObjectId,
        int caravanWorldObjectId)
    {
        var worldObjects = Find.WorldObjects?.AllWorldObjects ??
                           throw Error(
                               "world_objects_unavailable",
                               "RimWorld's live world-object collection is unavailable.");
        var settlement = ResolveExact<Settlement>(
            worldObjects,
            settlementWorldObjectId,
            "settlement");
        var caravan = ResolveExact<Caravan>(
            worldObjects,
            caravanWorldObjectId,
            "caravan");
        if (caravan.Faction != Faction.OfPlayer)
        {
            throw Error(
                "settlement_trade_wrong_caravan_faction",
                "Settlement trade requires a player caravan.");
        }

        if (settlement.Tile != caravan.Tile)
        {
            throw Error(
                "settlement_trade_wrong_tile",
                "The player caravan is not visiting the requested settlement tile.");
        }

        if (!ReferenceEquals(CaravanVisitUtility.SettlementVisitedNow(caravan), settlement))
        {
            throw Error(
                "settlement_trade_target_mismatch",
                "RimWorld's native visit lookup does not resolve to the requested settlement.");
        }

        if (settlement.Faction is null || settlement.TraderKind is null)
        {
            throw Error(
                "settlement_trade_unavailable",
                "The requested settlement has no native faction trader.");
        }

        var dialogsBefore = CurrentTradeDialogs();
        if (dialogsBefore.Length != 0)
        {
            throw Error(
                "trade_dialog_already_open",
                $"Expected no open native trade dialog, found {dialogsBefore.Length}.");
        }

        Command command;
        try
        {
            command = CaravanVisitUtility.TradeCommand(
                caravan,
                settlement.Faction,
                settlement.TraderKind);
        }
        catch (Exception exception)
        {
            throw new GatewaySettlementTradeException(
                "settlement_trade_native_failure",
                "RimWorld could not create its native settlement trade command.",
                exception);
        }

        if (command is not Command_Action action || action.action is null)
        {
            throw Error(
                "settlement_trade_shape_changed",
                "RimWorld's settlement trade command is not an invokable Command_Action.");
        }

        if (command.Disabled)
        {
            throw Error(
                "settlement_trade_disabled",
                "RimWorld's native settlement trade command is disabled for this caravan.");
        }

        try
        {
            action.action();
        }
        catch (Exception exception)
        {
            throw new GatewaySettlementTradeException(
                "settlement_trade_native_failure",
                "RimWorld's native settlement trade command threw.",
                exception);
        }

        var dialogsAfter = CurrentTradeDialogs();
        if (dialogsAfter.Length != 1 || TradeSession.deal is null)
        {
            throw Error(
                "settlement_trade_not_opened",
                $"The native command produced {dialogsAfter.Length} trade dialogs instead of one active deal.");
        }

        return new GatewaySettlementTradeOpening(
            settlementWorldObjectId,
            caravanWorldObjectId,
            NativeCommandIdentity);
    }

    private static T ResolveExact<T>(
        IEnumerable<WorldObject> worldObjects,
        int worldObjectId,
        string kind)
        where T : WorldObject
    {
        var matches = worldObjects
            .Where(worldObject => worldObject.ID == worldObjectId)
            .ToArray();
        if (matches.Length != 1)
        {
            throw Error(
                kind + "_world_object_missing",
                $"Expected one live world object with ID {worldObjectId}, found {matches.Length}.");
        }

        if (matches[0] is not T typed)
        {
            throw Error(
                kind + "_world_object_wrong_type",
                $"World object {worldObjectId} is not a {typeof(T).Name}.");
        }

        return typed;
    }

    private static Dialog_Trade[] CurrentTradeDialogs() =>
        Find.WindowStack?.Windows.OfType<Dialog_Trade>().ToArray() ??
        Array.Empty<Dialog_Trade>();

    private static GatewaySettlementTradeException Error(string code, string message) =>
        new(code, message);
}
