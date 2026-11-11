using Megamod.Features.MessagePlacement;
using NUnit.Framework;

namespace Megamod.Unit.Features.MessagePlacement.BarBounds;

[TestFixture]
public sealed class MessageBarLayoutPreparationTests
{
    [Test]
    public void FirstPreparationRefreshesAnOtherwiseCleanLayoutForTheActualViewport()
    {
        var preparation = new MessageBarLayoutPreparation();
        var context = new MessageBarLayoutContext(new object(), new object(), 1200, 800, new object());
        bool dirty = false;
        double preparedWidth = 1600;

        var receipt = preparation.Prepare(context, () => dirty = true, () =>
        {
            if (dirty) { preparedWidth = context.Width; dirty = false; }
        }, () => !dirty && preparedWidth == context.Width);

        Assert.That(preparedWidth, Is.EqualTo(1200));
        Assert.That(receipt, Is.Not.Null);
        Assert.That(receipt!.IsCurrent(), Is.True);
    }

    [Test]
    public void UnchangedContextUsesNativeDirtyStateWithoutRepeatedForcedInvalidation()
    {
        var preparation = new MessageBarLayoutPreparation();
        var context = new MessageBarLayoutContext(new object(), new object(), 1200, 800, new object());
        int invalidations = 0, preparations = 0, refreshes = 0;
        bool dirty = false;
        void Invalidate() { invalidations++; dirty = true; }
        void Prepare() { preparations++; if (dirty) { refreshes++; dirty = false; } }

        preparation.Prepare(context, Invalidate, Prepare, () => !dirty);
        preparation.Prepare(context with { }, Invalidate, Prepare, () => !dirty);
        dirty = true; // Native membership invalidation, independent of viewport changes.
        var latest = preparation.Prepare(context with { }, Invalidate, Prepare, () => !dirty);

        Assert.That(invalidations, Is.EqualTo(1));
        Assert.That(preparations, Is.EqualTo(3));
        Assert.That(refreshes, Is.EqualTo(2));
        Assert.That(latest!.IsCurrent(), Is.True);
    }

    [TestCase("width")]
    [TestCase("height")]
    [TestCase("root")]
    [TestCase("bar")]
    [TestCase("admission")]
    public void ChangedLayoutAuthorityRefreshesImmediatelyAndRevokesPreviousReceipt(string change)
    {
        var preparation = new MessageBarLayoutPreparation();
        var context = new MessageBarLayoutContext(new object(), new object(), 1200, 800, new object());
        int refreshes = 0;
        bool dirty = false;
        double preparedWidth = 0, preparedHeight = 0;
        void Invalidate() => dirty = true;
        void Prepare()
        {
            if (dirty) { refreshes++; preparedWidth = context.Width; preparedHeight = context.Height; dirty = false; }
        }
        var first = preparation.Prepare(context, Invalidate, Prepare, () => !dirty);
        context = change switch
        {
            "width" => context with { Width = 900 },
            "height" => context with { Height = 600 },
            "root" => context with { Root = new object() },
            "bar" => context with { Bar = new object() },
            _ => context with { AdmissionGeneration = new object() }
        };

        var current = preparation.Prepare(context, Invalidate, Prepare, () => !dirty);

        Assert.That(first!.IsCurrent(), Is.False);
        Assert.That(current!.IsCurrent(), Is.True);
        Assert.That(refreshes, Is.EqualTo(2));
        Assert.That(preparedWidth, Is.EqualTo(context.Width));
        Assert.That(preparedHeight, Is.EqualTo(context.Height));
    }

    [TestCase("invalidate")]
    [TestCase("prepare")]
    [TestCase("verify")]
    [TestCase("reject")]
    public void FailedPreparationRequiresFreshNativeInvalidationBeforeRecovery(string failure)
    {
        var preparation = new MessageBarLayoutPreparation();
        var context = new MessageBarLayoutContext(new object(), new object(), 1200, 800, new object());
        var first = preparation.Prepare(context, () => { }, () => { }, () => true);
        var fault = new InvalidOperationException("Native preparation failure");
        MessageBarLayoutReceipt? Attempt() => preparation.Prepare(
            failure == "invalidate" ? context with { Width = 900 } : context,
            () => { if (failure == "invalidate") throw fault; },
            () => { if (failure == "prepare") throw fault; },
            () => failure == "verify" ? throw fault : failure != "reject");
        if (failure == "reject") Assert.That(Attempt(), Is.Null);
        else Assert.That(Assert.Throws<InvalidOperationException>(() => Attempt()), Is.SameAs(fault));

        bool recoveryInvalidated = false;
        var recovered = preparation.Prepare(context, () => recoveryInvalidated = true, () => { }, () => true);

        Assert.That(recoveryInvalidated, Is.True);
        Assert.That(first!.IsCurrent(), Is.False);
        Assert.That(recovered!.IsCurrent(), Is.True);
    }

    [Test]
    public void ReentrantPreparationPublishesNeitherReceiptAndDoesNotRunNestedNativeWork()
    {
        var preparation = new MessageBarLayoutPreparation();
        var context = new MessageBarLayoutContext(new object(), new object(), 1200, 800, new object());
        MessageBarLayoutReceipt? nested = null;
        int nestedWork = 0;

        var outer = preparation.Prepare(context, () => { }, () =>
            nested = preparation.Prepare(context, () => nestedWork++, () => nestedWork++, () => true), () => true);

        Assert.That(outer, Is.Null);
        Assert.That(nested, Is.Null);
        Assert.That(nestedWork, Is.Zero);
        bool recoveryInvalidated = false;
        var recovered = preparation.Prepare(context, () => recoveryInvalidated = true, () => { }, () => true);
        Assert.That(recoveryInvalidated, Is.True);
        Assert.That(recovered!.IsCurrent(), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReceiptValidationLossIsFinalAndRequiresFreshPreparation(bool validationThrows)
    {
        var preparation = new MessageBarLayoutPreparation();
        var context = new MessageBarLayoutContext(new object(), new object(), 1200, 800, new object());
        bool ready = true, throws = false;
        int invalidations = 0, preparations = 0, reads = 0;
        var fault = new InvalidOperationException("Native read failed");
        bool Current() { reads++; if (throws) throw fault; return ready; }
        var receipt = preparation.Prepare(context, () => invalidations++, () => preparations++, Current);
        ready = false;
        throws = validationThrows;
        if (validationThrows) Assert.That(Assert.Throws<InvalidOperationException>(() => receipt!.IsCurrent()), Is.SameAs(fault));
        else Assert.That(receipt!.IsCurrent(), Is.False);

        ready = true; throws = false;
        Assert.That(receipt!.IsCurrent(), Is.False);
        Assert.That(reads, Is.EqualTo(2));
        Assert.That(preparations, Is.EqualTo(1), "Validation must not prepare native layout.");
        Assert.That(invalidations, Is.EqualTo(1), "Validation must not dirty native layout.");
        var recovered = preparation.Prepare(context, () => invalidations++, () => preparations++, Current);
        Assert.That(invalidations, Is.EqualTo(2));
        Assert.That(recovered!.IsCurrent(), Is.True);
    }

    [TestCase("null")]
    [TestCase("root")]
    [TestCase("bar")]
    [TestCase("admission")]
    [TestCase("zero-width")]
    [TestCase("negative-height")]
    [TestCase("nan-width")]
    [TestCase("infinite-height")]
    public void InvalidContextRevokesAuthorityWithoutTouchingNativeState(string invalid)
    {
        var preparation = new MessageBarLayoutPreparation();
        var context = new MessageBarLayoutContext(new object(), new object(), 1200, 800, new object());
        var first = preparation.Prepare(context, () => { }, () => { }, () => true);
        var bad = invalid switch
        {
            "null" => null,
            "root" => context with { Root = null! },
            "bar" => context with { Bar = null! },
            "admission" => context with { AdmissionGeneration = null! },
            "zero-width" => context with { Width = 0 },
            "negative-height" => context with { Height = -1 },
            "nan-width" => context with { Width = double.NaN },
            _ => context with { Height = double.PositiveInfinity }
        };
        int nativeCalls = 0;

        var rejected = preparation.Prepare(bad!, () => nativeCalls++, () => nativeCalls++, () => { nativeCalls++; return true; });

        Assert.That(rejected, Is.Null);
        Assert.That(nativeCalls, Is.Zero);
        Assert.That(first!.IsCurrent(), Is.False);
        bool recoveryInvalidated = false;
        preparation.Prepare(context, () => recoveryInvalidated = true, () => { }, () => true);
        Assert.That(recoveryInvalidated, Is.True);
    }

    [Test]
    public void ReentrantInvalidationStopsBeforeNativePreparation()
    {
        var preparation = new MessageBarLayoutPreparation();
        var context = new MessageBarLayoutContext(new object(), new object(), 1200, 800, new object());
        int nativePreparations = 0;
        var receipt = preparation.Prepare(context,
            () => preparation.Prepare(context, () => { }, () => { }, () => true),
            () => nativePreparations++, () => true);

        Assert.That(receipt, Is.Null);
        Assert.That(nativePreparations, Is.Zero);
    }
}
