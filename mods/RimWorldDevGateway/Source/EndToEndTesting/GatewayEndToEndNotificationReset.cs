namespace RimWorldDevGateway;

internal interface IGatewayEndToEndNotificationOperations
{
    bool MessagesEmpty { get; }

    bool LettersEmpty { get; }

    bool AlertsEmpty { get; }

    void ClearLetters();

    void ClearMessages();

    void ClearAlerts();
}

internal sealed class GatewayEndToEndNotificationReset
{
    private readonly IGatewayEndToEndNotificationOperations operations;

    internal GatewayEndToEndNotificationReset(IGatewayEndToEndNotificationOperations operations) =>
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));

    internal void Clear()
    {
        operations.ClearLetters();
        operations.ClearMessages();
        operations.ClearAlerts();
    }

    internal bool IsEmpty() =>
        operations.LettersEmpty &&
        operations.MessagesEmpty &&
        operations.AlertsEmpty;
}
