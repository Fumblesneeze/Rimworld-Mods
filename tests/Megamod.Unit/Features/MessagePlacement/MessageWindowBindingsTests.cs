using Megamod.Features.MessagePlacement;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.MessagePlacement;

[TestFixture]
public sealed class MessageWindowBindingsTests
{
    [TestCase("removed")]
    [TestCase("replaced")]
    [TestCase("callback")]
    [TestCase("window-rectangle")]
    [TestCase("message-rectangle")]
    [TestCase("capture-rectangle")]
    [TestCase("incoming-rectangle")]
    [TestCase("capture-message")]
    [TestCase("duplicate")]
    [TestCase("combined")]
    public void CaptureRevalidationRejectsChangedIdentityOrStateBeforeAnyMutation(string change)
    {
        var baseline = new NativeRectangle(13, 17, 100, 25);
        var previous = new NativeRectangle(33, 47, 100, 25);
        var message = new NativeMessage { LastRectangle = baseline };
        var retained = new NativeCallback { Message = message, Rectangle = previous };
        var incoming = new NativeCallback { Message = message, Rectangle = baseline };
        Action callback = retained.Draw;
        var window = new NativeWindow { Rectangle = previous, Callback = callback };
        var windows = new List<object> { window };
        Assert.That(Bindings().Capture(message, baseline, incoming.Draw, false, windows, out var capture),
            Is.EqualTo(MessageWindowCaptureStatus.Bound));
        Assert.That(capture!.IsCurrent(windows), Is.True);
        switch (change)
        {
            case "removed": windows.Clear(); break;
            case "replaced": windows[0] = new NativeWindow { Rectangle = previous, Callback = callback }; break;
            case "callback": window.Callback = retained.Draw; break;
            case "window-rectangle": window.Rectangle = baseline; break;
            case "message-rectangle": message.LastRectangle = previous; break;
            case "capture-rectangle": retained.Rectangle = baseline; break;
            case "incoming-rectangle": incoming.Rectangle = previous; break;
            case "capture-message": retained.Message = new(); break;
            case "duplicate": windows.Add(new NativeWindow { Rectangle = previous, Callback = callback }); break;
            case "combined":
                Action combined = () => {};
                combined += callback;
                windows.Add(new NativeWindow { Rectangle = previous, Callback = combined }); break;
        }
        Assert.That(capture.IsCurrent(windows), Is.False);
        Assert.That(retained.Draws, Is.Zero);
        Assert.That(incoming.Draws, Is.Zero);
    }

    [Test]
    public void RepaintMovesOnlyExactMessageWindowAndPreservesNativeCallbackInstance()
    {
        var message = new NativeMessage { LastRectangle = new(13, 17, 100, 25) };
        var target = new NativeCallback { Message = message, Rectangle = message.LastRectangle };
        Action callback = target.Draw;
        var own = new NativeWindow { Rectangle = message.LastRectangle, Callback = callback };
        var unrelated = new NativeWindow { Rectangle = new(1, 2, 3, 4), Callback = () => {} };
        Assert.That(Bindings().TryCapture(message, message.LastRectangle, callback, true, [unrelated, own], out var capture), Is.True);
        var shifted = new NativeRectangle(31, 47, 100, 25);
        capture!.Apply(shifted);
        Assert.That(own.Rectangle, Is.EqualTo(shifted));
        Assert.That(message.LastRectangle, Is.EqualTo(shifted));
        Assert.That(target.Rectangle, Is.EqualTo(shifted));
        Assert.That(own.Callback, Is.SameAs(callback));
        Assert.That(target.Draws, Is.Zero);
        Assert.That(unrelated.Rectangle, Is.EqualTo(new NativeRectangle(1, 2, 3, 4)));
        own.Callback();
        Assert.That(target.Draws, Is.EqualTo(1));
    }

    [Test]
    public void InputPassUpdatesExistingCallbackAndRestoresItsPreviouslyTranslatedRectangleExactly()
    {
        var baseline = new NativeRectangle(13, 17, 100, 25);
        var previous = new NativeRectangle(33, 47, 100, 25);
        var message = new NativeMessage { LastRectangle = baseline };
        var oldTarget = new NativeCallback { Message = message, Rectangle = previous };
        var incomingTarget = new NativeCallback { Message = message, Rectangle = baseline };
        Action existing = oldTarget.Draw;
        var window = new NativeWindow { Rectangle = previous, Callback = existing };
        Assert.That(Bindings().TryCapture(message, baseline, incomingTarget.Draw, false, [window], out var capture), Is.True);
        var moved = new NativeRectangle(53, 67, 100, 25);
        capture!.Apply(moved);
        Assert.That(window.Rectangle, Is.EqualTo(moved));
        Assert.That(oldTarget.Rectangle, Is.EqualTo(moved));
        Assert.That(message.LastRectangle, Is.EqualTo(moved));
        Assert.That(incomingTarget.Rectangle, Is.EqualTo(baseline));
        Assert.That(window.Callback, Is.SameAs(existing));
        capture.Restore();
        Assert.That(window.Rectangle, Is.EqualTo(previous));
        Assert.That(oldTarget.Rectangle, Is.EqualTo(previous));
        Assert.That(message.LastRectangle, Is.EqualTo(baseline));
        Assert.That(oldTarget.Draws, Is.Zero);
    }

    [Test]
    public void RepaintDoesNotAcceptAWindowStillBoundToThePreviousCallback()
    {
        var baseline = new NativeRectangle(13, 17, 100, 25);
        var message = new NativeMessage { LastRectangle = baseline };
        var oldTarget = new NativeCallback { Message = message, Rectangle = baseline };
        var incoming = new NativeCallback { Message = message, Rectangle = baseline };
        var window = new NativeWindow { Rectangle = baseline, Callback = oldTarget.Draw };
        Assert.That(Bindings().TryCapture(message, baseline, incoming.Draw, true, [window], out var capture), Is.False);
        Assert.That(capture, Is.Null);
    }

    [Test]
    public void ACombinedCallbackCannotMasqueradeAsTheSingleNativeMessageCallback()
    {
        var baseline = new NativeRectangle(13, 17, 100, 25);
        var message = new NativeMessage { LastRectangle = baseline };
        var target = new NativeCallback { Message = message, Rectangle = baseline };
        Action callback = () => {};
        callback += target.Draw;
        var window = new NativeWindow { Rectangle = baseline, Callback = callback };
        Assert.That(Bindings().TryCapture(message, baseline, callback, true, [window], out _), Is.False);
    }

    [Test]
    public void AmbiguousWindowsAreRejectedWithoutMovingEither()
    {
        var baseline = new NativeRectangle(13, 17, 100, 25);
        var message = new NativeMessage { LastRectangle = baseline };
        var target = new NativeCallback { Message = message, Rectangle = baseline };
        Action callback = target.Draw;
        var first = new NativeWindow { Rectangle = baseline, Callback = callback };
        var second = new NativeWindow { Rectangle = baseline, Callback = callback };
        Assert.That(Bindings().TryCapture(message, baseline, callback, true, [first, second], out _), Is.False);
        Assert.That(first.Rectangle, Is.EqualTo(baseline));
        Assert.That(second.Rectangle, Is.EqualTo(baseline));
    }

    [Test]
    public void ChangedNativeFieldContractIsRejectedBeforeCapture()
    {
        Assert.Throws<ArgumentException>(() => new MessageWindowBindings(
            typeof(NativeMessage).GetField(nameof(NativeMessage.LastRectangle))!,
            typeof(NativeWindow).GetField(nameof(NativeWindow.Rectangle))!,
            typeof(NativeCallback).GetField(nameof(NativeCallback.Draws))!,
            typeof(NativeCallback).GetField(nameof(NativeCallback.Message))!,
            typeof(NativeCallback).GetField(nameof(NativeCallback.Rectangle))!,
            typeof(NativeCallback).GetMethod(nameof(NativeCallback.Draw))!));
    }

    [TestCase(false, MessageWindowCaptureStatus.NotRenderedYet)]
    [TestCase(true, MessageWindowCaptureStatus.Incompatible)]
    public void MissingWindowIsOnlyAnOrdinaryInputPassBeforeFirstRepaint(bool repaint, MessageWindowCaptureStatus expected)
    {
        var baseline = new NativeRectangle(13, 17, 100, 25);
        var message = new NativeMessage { LastRectangle = baseline };
        var target = new NativeCallback { Message = message, Rectangle = baseline };
        Assert.That(Bindings().Capture(message, baseline, target.Draw, repaint, [], out var capture), Is.EqualTo(expected));
        Assert.That(capture, Is.Null);
        Assert.That(target.Draws, Is.Zero);
        Assert.That(message.LastRectangle, Is.EqualTo(baseline));
    }

    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, true)]
    public void ExistingCombinedMessageCallbackIsIncompatibleEvenBesideAValidSingleWindow(
        bool includeSingle, bool combinedFirst, bool nativeFirst)
    {
        var baseline = new NativeRectangle(13, 17, 100, 25);
        var previous = new NativeRectangle(33, 47, 100, 25);
        var message = new NativeMessage { LastRectangle = baseline };
        var oldTarget = new NativeCallback { Message = message, Rectangle = previous };
        var incoming = new NativeCallback { Message = message, Rectangle = baseline };
        int foreignCalls = 0;
        Action native = oldTarget.Draw;
        Action foreign = () => foreignCalls++;
        Action combined = nativeFirst ? native + foreign : foreign + native;
        var combinedWindow = new NativeWindow { Rectangle = previous, Callback = combined };
        var singleWindow = new NativeWindow { Rectangle = previous, Callback = native };
        object[] windows = !includeSingle ? [combinedWindow]
            : combinedFirst ? [combinedWindow, singleWindow] : [singleWindow, combinedWindow];
        Assert.That(Bindings().Capture(message, baseline, incoming.Draw, false, windows, out var capture),
            Is.EqualTo(MessageWindowCaptureStatus.Incompatible));
        Assert.That(capture, Is.Null);
        Assert.That(combinedWindow.Rectangle, Is.EqualTo(previous));
        Assert.That(combinedWindow.Callback, Is.SameAs(combined));
        Assert.That(singleWindow.Rectangle, Is.EqualTo(previous));
        Assert.That(message.LastRectangle, Is.EqualTo(baseline));
        Assert.That(oldTarget.Draws, Is.Zero);
        Assert.That(foreignCalls, Is.Zero);
    }

    [Test]
    public void CombinedCallbackForAnotherMessageDoesNotBlockTheExactMessageWindow()
    {
        var baseline = new NativeRectangle(13, 17, 100, 25);
        var message = new NativeMessage { LastRectangle = baseline };
        var target = new NativeCallback { Message = message, Rectangle = baseline };
        var other = new NativeCallback { Message = new NativeMessage(), Rectangle = baseline };
        Action combined = () => {};
        combined += other.Draw;
        var unrelated = new NativeWindow { Rectangle = baseline, Callback = combined };
        Action native = target.Draw;
        var own = new NativeWindow { Rectangle = baseline, Callback = native };
        Assert.That(Bindings().Capture(message, baseline, native, false, [unrelated, own], out var capture),
            Is.EqualTo(MessageWindowCaptureStatus.Bound));
        Assert.That(capture, Is.Not.Null);
        Assert.That(unrelated.Callback, Is.SameAs(combined));
        Assert.That(other.Draws, Is.Zero);
    }

    private static MessageWindowBindings Bindings() => new(
        typeof(NativeMessage).GetField(nameof(NativeMessage.LastRectangle))!,
        typeof(NativeWindow).GetField(nameof(NativeWindow.Rectangle))!,
        typeof(NativeWindow).GetField(nameof(NativeWindow.Callback))!,
        typeof(NativeCallback).GetField(nameof(NativeCallback.Message))!,
        typeof(NativeCallback).GetField(nameof(NativeCallback.Rectangle))!,
        typeof(NativeCallback).GetMethod(nameof(NativeCallback.Draw))!);

    public readonly record struct NativeRectangle(float X, float Y, float Width, float Height);
    public sealed class NativeMessage { public NativeRectangle LastRectangle; }
    public sealed class NativeWindow { public NativeRectangle Rectangle; public Action Callback = null!; }
    public sealed class NativeCallback
    {
        public NativeMessage Message = null!;
        public NativeRectangle Rectangle;
        public int Draws;
        public void Draw() => Draws++;
    }
}
