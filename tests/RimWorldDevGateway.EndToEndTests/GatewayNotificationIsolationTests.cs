using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway.EndToEndTests;

[RimWorldEndToEndTest(
    "gateway.notification-cleanup.seed",
    EndToEndTestContract.GatewayPackageId,
    EndToEndTestContract.CorePackageId,
    MaxFrames = 900,
    MaxGameTicks = 2_000,
    MaxWallClockSeconds = 45)]
public sealed class GatewayNotificationCleanupSeedTest : IRimWorldEndToEndTest
{
    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        GenSpawn.Spawn(pawn, map.Center, map);

        Messages.Message(
            "Gateway cleanup probe live message",
            MessageTypeDefOf.NeutralEvent,
            historical: false);
        Find.LetterStack.ReceiveLetter(
            "Gateway cleanup probe visible letter",
            "This visible letter must not reach the next E2E test.",
            LetterDefOf.NeutralEvent,
            playSound: false);
        Find.LetterStack.ReceiveLetter(
            "Gateway cleanup probe delayed letter",
            "This delayed letter must not reach the next E2E test.",
            LetterDefOf.NeutralEvent,
            delayTicks: 60_000,
            playSound: false);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "advance through RimWorld's native alert startup delay",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "native player notifications become visible",
            _ => Find.TickManager.TicksGame >= 650 &&
                 GatewayNotificationProbe.LiveMessageCount > 0 &&
                 Find.LetterStack.LettersListForReading.Count > 0 &&
                 GatewayNotificationProbe.DelayedLetterCount > 0 &&
                 GatewayNotificationProbe.ActiveAlertCount > 0,
            new EndToEndDeadline(360, 1_200, TimeSpan.FromSeconds(20)));
        yield return new TimeControlActionStep(
            "pause with the seeded notification slate visible",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "seeded live message letter and active alert before cleanup",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "seeded notification counts",
            _ => new Dictionary<string, string>
            {
                ["messages"] = GatewayNotificationProbe.LiveMessageCount.ToString(),
                ["visibleLetters"] = Find.LetterStack.LettersListForReading.Count.ToString(),
                ["delayedLetters"] = GatewayNotificationProbe.DelayedLetterCount.ToString(),
                ["activeAlerts"] = GatewayNotificationProbe.ActiveAlertCount.ToString()
            });
    }
}

[RimWorldEndToEndTest(
    "gateway.notification-cleanup.verify",
    EndToEndTestContract.GatewayPackageId,
    EndToEndTestContract.CorePackageId,
    MaxFrames = 600,
    MaxGameTicks = 1_000,
    MaxWallClockSeconds = 30)]
public sealed class GatewayNotificationCleanupVerifyTest : IRimWorldEndToEndTest
{
    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.Equal(0, GatewayNotificationProbe.LiveMessageCount,
            "The next E2E test must start without live messages.");
        EndToEndAssert.Equal(0, Find.LetterStack.LettersListForReading.Count,
            "The next E2E test must start without visible letters.");
        EndToEndAssert.Equal(0, GatewayNotificationProbe.DelayedLetterCount,
            "The next E2E test must start without delayed letters.");
        EndToEndAssert.Equal(0, GatewayNotificationProbe.ActiveAlertCount,
            "The next E2E test must start without cached active alerts.");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new ScreenshotStep(
            "clean notification slate after sequential cleanup",
            Array.Empty<string>(),
            paddingPixels: 0);
    }
}

internal static class GatewayNotificationProbe
{
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    private static readonly FieldInfo LiveMessagesField =
        RequireField(typeof(Messages), "liveMessages", StaticPrivate, typeof(List<Message>));
    private static readonly FieldInfo DelayedLettersField =
        RequireField(typeof(LetterStack), "letterQueue", InstancePrivate, typeof(List<Letter>));
    private static readonly FieldInfo ActiveAlertsField =
        RequireField(typeof(AlertsReadout), "activeAlerts", InstancePrivate, typeof(List<Alert>));

    internal static int LiveMessageCount =>
        RequireList<Message>(LiveMessagesField, null).Count;

    internal static int DelayedLetterCount =>
        RequireList<Letter>(DelayedLettersField, Find.LetterStack).Count;

    internal static int ActiveAlertCount =>
        RequireList<Alert>(ActiveAlertsField, Find.Alerts).Count;

    private static FieldInfo RequireField(
        Type owner,
        string name,
        BindingFlags flags,
        Type exactType)
    {
        var field = owner.GetField(name, flags);
        if (field is null || field.FieldType != exactType)
        {
            throw new InvalidOperationException(
                "The exact RimWorld notification field is unavailable: " + owner.FullName + "." + name + ".");
        }

        return field;
    }

    private static List<T> RequireList<T>(FieldInfo field, object? owner) =>
        field.GetValue(owner) as List<T> ?? throw new InvalidOperationException(
            "The exact RimWorld notification collection is unavailable: " + field.DeclaringType?.FullName + "." + field.Name + ".");
}
