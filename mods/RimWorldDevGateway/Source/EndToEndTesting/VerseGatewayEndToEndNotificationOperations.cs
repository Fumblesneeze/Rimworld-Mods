using System.Reflection;
using RimWorld;
using Verse;

namespace RimWorldDevGateway;

internal sealed class VerseGatewayEndToEndNotificationOperations :
    IGatewayEndToEndNotificationOperations
{
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    private static readonly FieldInfo? LiveMessagesField =
        typeof(Messages).GetField("liveMessages", StaticPrivate);
    private static readonly FieldInfo? DelayedLettersField =
        typeof(LetterStack).GetField("letterQueue", InstancePrivate);
    private static readonly FieldInfo? ActiveAlertsField =
        typeof(AlertsReadout).GetField("activeAlerts", InstancePrivate);

    internal static bool PrivateShapesAvailable =>
        IsExactListField<Message>(LiveMessagesField, isStatic: true) &&
        IsExactListField<Letter>(DelayedLettersField, isStatic: false) &&
        IsExactListField<Alert>(ActiveAlertsField, isStatic: false);

    public bool MessagesEmpty =>
        RequireList<Message>(LiveMessagesField, null, "Verse.Messages.liveMessages").Count == 0;

    public bool LettersEmpty
    {
        get
        {
            var stack = RequireLetterStack();
            return stack.LettersListForReading.Count == 0 &&
                   RequireList<Letter>(DelayedLettersField, stack, "Verse.LetterStack.letterQueue").Count == 0;
        }
    }

    public bool AlertsEmpty =>
        RequireList<Alert>(ActiveAlertsField, RequireAlerts(), "RimWorld.AlertsReadout.activeAlerts").Count == 0;

    public void ClearLetters()
    {
        var stack = RequireLetterStack();
        foreach (var letter in stack.LettersListForReading.ToArray())
        {
            stack.RemoveLetter(letter);
        }

        RequireList<Letter>(DelayedLettersField, stack, "Verse.LetterStack.letterQueue").Clear();
    }

    public void ClearMessages() => Messages.Clear();

    public void ClearAlerts() =>
        RequireList<Alert>(ActiveAlertsField, RequireAlerts(), "RimWorld.AlertsReadout.activeAlerts").Clear();

    private static LetterStack RequireLetterStack() =>
        Find.LetterStack ?? throw new InvalidOperationException(
            "E2E notification reset requires RimWorld's current LetterStack.");

    private static AlertsReadout RequireAlerts() =>
        Find.Alerts ?? throw new InvalidOperationException(
            "E2E notification reset requires RimWorld's current AlertsReadout.");

    private static bool IsExactListField<T>(FieldInfo? field, bool isStatic) =>
        field is not null &&
        field.IsPrivate &&
        field.IsStatic == isStatic &&
        field.FieldType == typeof(List<T>);

    private static List<T> RequireList<T>(FieldInfo? field, object? owner, string exactShape)
    {
        if (!IsExactListField<T>(field, owner is null) || field!.GetValue(owner) is not List<T> list)
        {
            throw new InvalidOperationException(
                "The exact RimWorld notification collection is unavailable: " + exactShape + ".");
        }

        return list;
    }
}
