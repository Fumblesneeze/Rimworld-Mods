using System.Reflection;
using NUnit.Framework;
using Verse;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PersonalDiningOwnershipTests
{
    [Test]
    public void Personal_cutlery_owner_survives_split_and_blocks_cross_owner_stacking()
    {
        const string owner = "OwnershipFixturePawn101";
        const string otherOwner = "OwnershipFixturePawn202";
        var original = Ware(out var originalSanitation);
        var split = Ware(out var splitSanitation);
        var sameOwner = Ware(out var sameOwnerSanitation);
        var other = Ware(out var otherSanitation);

        originalSanitation.MarkPersonalDiningOwner(owner);
        sameOwnerSanitation.MarkPersonalDiningOwner(owner);
        otherSanitation.MarkPersonalDiningOwner(otherOwner);
        originalSanitation.PostSplitOff(split);

        Assert.Multiple(() =>
        {
            Assert.That(originalSanitation.IsPersonalDiningWareFor(owner), Is.True);
            Assert.That(splitSanitation.IsPersonalDiningWareFor(owner), Is.True);
            Assert.That(originalSanitation.AllowStackWith(sameOwner), Is.True);
            Assert.That(originalSanitation.AllowStackWith(other), Is.False);
        });
    }

    [Test]
    public void Personal_plate_owner_survives_meal_split_and_blocks_cross_owner_stacking()
    {
        const string owner = "OwnershipFixturePawn303";
        const string otherOwner = "OwnershipFixturePawn404";
        var original = Meal(out var originalEmbedded);
        var split = Meal(out var splitEmbedded);
        var sameOwner = Meal(out var sameOwnerEmbedded);
        var other = Meal(out var otherEmbedded);

        originalEmbedded.MarkPersonalPlateOwner(owner);
        sameOwnerEmbedded.MarkPersonalPlateOwner(owner);
        otherEmbedded.MarkPersonalPlateOwner(otherOwner);
        originalEmbedded.PostSplitOff(split);

        Assert.Multiple(() =>
        {
            Assert.That(originalEmbedded.IsPersonalPlateFor(owner), Is.True);
            Assert.That(splitEmbedded.IsPersonalPlateFor(owner), Is.True);
            Assert.That(originalEmbedded.AllowStackWith(sameOwner), Is.True);
            Assert.That(originalEmbedded.AllowStackWith(other), Is.False);
        });
    }

    [Test]
    public void Session_transfer_marker_survives_split_and_blocks_unmarked_stacking()
    {
        var original = Ware(out var originalSanitation);
        var split = Ware(out var splitSanitation);
        var sameSessionState = Ware(out var sameSessionSanitation);
        var unrelated = Ware(out _);

        originalSanitation.MarkSessionTransferredWare();
        sameSessionSanitation.MarkSessionTransferredWare();
        originalSanitation.PostSplitOff(split);

        Assert.Multiple(() =>
        {
            Assert.That(originalSanitation.ReturnToMapAfterInterruptedSession, Is.True);
            Assert.That(splitSanitation.ReturnToMapAfterInterruptedSession, Is.True);
            Assert.That(originalSanitation.AllowStackWith(sameSessionState), Is.True);
            Assert.That(originalSanitation.AllowStackWith(unrelated), Is.False);
        });
    }

    [Test]
    public void Personal_and_session_transfer_provenance_are_mutually_exclusive_and_clearable()
    {
        const string owner = "OwnershipFixturePawn505";
        _ = Ware(out var sanitation);

        sanitation.MarkPersonalDiningOwner(owner);
        sanitation.MarkSessionTransferredWare();

        Assert.Multiple(() =>
        {
            Assert.That(sanitation.IsPersonalDiningWareFor(owner), Is.False);
            Assert.That(sanitation.ReturnToMapAfterInterruptedSession, Is.True);
        });

        sanitation.MarkPersonalDiningOwner(owner);
        Assert.Multiple(() =>
        {
            Assert.That(sanitation.IsPersonalDiningWareFor(owner), Is.True);
            Assert.That(sanitation.ReturnToMapAfterInterruptedSession, Is.False);
        });

        sanitation.ClearPersonalDiningOwner();
        sanitation.ClearSessionTransfer();
        Assert.Multiple(() =>
        {
            Assert.That(sanitation.IsPersonalDiningWareFor(owner), Is.False);
            Assert.That(sanitation.ReturnToMapAfterInterruptedSession, Is.False);
        });
    }

    private static ThingWithComps Ware(out CompSanitation sanitation)
    {
        var thing = new ThingWithComps();
        sanitation = new CompSanitation { parent = thing };
        SetComps(thing, sanitation);
        return thing;
    }

    private static ThingWithComps Meal(out CompEmbeddedWare embedded)
    {
        var thing = new ThingWithComps { stackCount = 1 };
        embedded = new CompEmbeddedWare { parent = thing };
        SetComps(thing, embedded);
        return thing;
    }

    private static void SetComps(ThingWithComps thing, params ThingComp[] comps)
    {
        typeof(ThingWithComps)
            .GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(thing, comps.ToList());
    }
}
