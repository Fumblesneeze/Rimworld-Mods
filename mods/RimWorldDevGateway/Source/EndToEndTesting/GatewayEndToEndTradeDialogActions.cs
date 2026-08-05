using System.Globalization;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway;

public sealed class GatewayTradeTransferAdjustment
{
    public GatewayTradeTransferAdjustment(
        string thingRuntimeId,
        int countBefore,
        int countAfter)
    {
        ThingRuntimeId = string.IsNullOrWhiteSpace(thingRuntimeId)
            ? throw new ArgumentException("A Thing runtime ID is required.", nameof(thingRuntimeId))
            : thingRuntimeId;
        CountBefore = countBefore;
        CountAfter = countAfter;
    }

    public string ThingRuntimeId { get; }

    public int CountBefore { get; }

    public int CountAfter { get; }
}

public sealed class GatewayTradeDialogException : Exception
{
    public GatewayTradeDialogException(string code, string message)
        : base(message)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("A failure code is required.", nameof(code))
            : code;
    }

    public GatewayTradeDialogException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("A failure code is required.", nameof(code))
            : code;
    }

    public string Code { get; }
}

public interface IGatewayEndToEndTradeDialogOperations
{
    GatewayTradeTransferAdjustment AdjustTransfer(string thingRuntimeId, int countDelta);

    void Accept();
}

public interface IGatewayEndToEndTradeDialogActions
{
    GatewayEndToEndStepOutcome Apply(TradeDialogActionStep step);
}

public sealed class GatewayEndToEndTradeDialogActions : IGatewayEndToEndTradeDialogActions
{
    private readonly IGatewayEndToEndTradeDialogOperations operations;

    public GatewayEndToEndTradeDialogActions(IGatewayEndToEndTradeDialogOperations operations) =>
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));

    public GatewayEndToEndStepOutcome Apply(TradeDialogActionStep step)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        try
        {
            switch (step.Action)
            {
                case EndToEndTradeDialogAction.AdjustTransfer:
                    var adjustment = operations.AdjustTransfer(
                        step.ThingRuntimeId ?? throw new InvalidOperationException(
                            "A transfer adjustment is missing its Thing runtime ID."),
                        step.CountDelta);
                    return GatewayEndToEndStepOutcome.Pass(new Dictionary<string, string>
                    {
                        ["thingRuntimeId"] = adjustment.ThingRuntimeId,
                        ["countBefore"] = adjustment.CountBefore.ToString(CultureInfo.InvariantCulture),
                        ["countAfter"] = adjustment.CountAfter.ToString(CultureInfo.InvariantCulture)
                    });
                case EndToEndTradeDialogAction.Accept:
                    operations.Accept();
                    return GatewayEndToEndStepOutcome.Pass(new Dictionary<string, string>
                    {
                        ["nativeCallback"] = "Dialog_Trade.Accept"
                    });
                default:
                    return GatewayEndToEndStepOutcome.Fail(
                        "unsupported_trade_dialog_action",
                        $"Trade-dialog action '{step.Action}' is unsupported.");
            }
        }
        catch (GatewayTradeDialogException exception)
        {
            return GatewayEndToEndStepOutcome.Fail(exception.Code, exception.Message);
        }
    }
}

public sealed class VerseGatewayEndToEndTradeDialogOperations :
    IGatewayEndToEndTradeDialogOperations
{
    private static readonly MethodInfo? CountSetter = typeof(Tradeable)
        .GetProperty(nameof(Tradeable.CountToTransfer),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?.GetSetMethod(nonPublic: true);

    private static readonly MethodInfo? CountChanged = typeof(Dialog_Trade).GetMethod(
        "CountToTransferChanged",
        BindingFlags.Instance | BindingFlags.NonPublic,
        binder: null,
        types: Type.EmptyTypes,
        modifiers: null);

    private static readonly MethodInfo? AcceptCallback = typeof(Dialog_Trade).GetMethod(
        "<DoWindowContents>b__65_2",
        BindingFlags.Instance | BindingFlags.NonPublic,
        binder: null,
        types: Type.EmptyTypes,
        modifiers: null);

    public GatewayTradeTransferAdjustment AdjustTransfer(
        string thingRuntimeId,
        int countDelta)
    {
        if (string.IsNullOrWhiteSpace(thingRuntimeId))
        {
            throw Error("invalid_trade_thing", "A nonempty Thing runtime ID is required.");
        }

        var dialog = ResolveDialog();
        var matches = TradeSession.deal.AllTradeables
            .Where(tradeable => tradeable.thingsColony
                .Concat(tradeable.thingsTrader)
                .Any(thing => string.Equals(
                    thing.ThingID,
                    thingRuntimeId,
                    StringComparison.Ordinal)))
            .ToArray();
        if (matches.Length != 1)
        {
            throw Error(
                matches.Length == 0 ? "trade_thing_missing" : "trade_thing_ambiguous",
                $"Expected one current Tradeable containing Thing '{thingRuntimeId}', found {matches.Length}.");
        }

        var tradeable = matches[0];
        var before = tradeable.CountToTransfer;
        int after;
        try
        {
            after = checked(before + countDelta);
        }
        catch (OverflowException exception)
        {
            throw new GatewayTradeDialogException(
                "trade_transfer_out_of_range",
                "The requested transfer count overflowed.",
                exception);
        }

        var minimum = tradeable.GetMinimumToTransfer();
        var maximum = tradeable.GetMaximumToTransfer();
        if (after < minimum || after > maximum)
        {
            throw Error(
                "trade_transfer_out_of_range",
                $"Transfer count {after} is outside the native range {minimum}..{maximum}.");
        }

        InvokeRequired(
            CountSetter,
            tradeable,
            new object[] { after },
            "trade_dialog_shape_changed",
            "RimWorld's native Tradeable count setter is unavailable.");
        InvokeRequired(
            CountChanged,
            dialog,
            Array.Empty<object>(),
            "trade_dialog_shape_changed",
            "RimWorld's native trade-dialog count refresh is unavailable.");
        if (tradeable.CountToTransfer != after)
        {
            throw Error(
                "trade_transfer_not_applied",
                "The native Tradeable count setter did not retain the requested count.");
        }

        return new GatewayTradeTransferAdjustment(thingRuntimeId, before, after);
    }

    public void Accept()
    {
        var dialog = ResolveDialog();
        InvokeRequired(
            AcceptCallback,
            dialog,
            Array.Empty<object>(),
            "trade_dialog_shape_changed",
            "RimWorld's exact native trade Accept callback is unavailable.");
    }

    private static Dialog_Trade ResolveDialog()
    {
        var matches = Find.WindowStack?.Windows.OfType<Dialog_Trade>().ToArray() ??
                      Array.Empty<Dialog_Trade>();
        if (matches.Length != 1)
        {
            throw Error(
                matches.Length == 0 ? "trade_dialog_missing" : "trade_dialog_ambiguous",
                $"Expected exactly one open native trade dialog, found {matches.Length}.");
        }

        if (TradeSession.deal is null)
        {
            throw Error("trade_deal_missing", "The native trade session has no active deal.");
        }

        return matches[0];
    }

    private static void InvokeRequired(
        MethodInfo? method,
        object target,
        object[] arguments,
        string code,
        string unavailableMessage)
    {
        if (method is null)
        {
            throw Error(code, unavailableMessage);
        }

        try
        {
            method.Invoke(target, arguments);
        }
        catch (TargetInvocationException exception)
        {
            throw new GatewayTradeDialogException(
                "trade_dialog_native_failure",
                "RimWorld's native trade-dialog action threw.",
                exception.InnerException ?? exception);
        }
        catch (Exception exception)
        {
            throw new GatewayTradeDialogException(
                "trade_dialog_native_failure",
                "RimWorld's native trade-dialog action could not be invoked.",
                exception);
        }
    }

    private static GatewayTradeDialogException Error(string code, string message) =>
        new(code, message);
}
