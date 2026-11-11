using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement;

[TestFixture, NonParallelizable]
public sealed class MessagePlacementBridgeTests
{
    private static MessagePlacementBridge? bridge;
    private static Root root = null!;
    private static bool repaint;
    private static Action? afterDraw;
    private static int contextRevision;
    private static Exception? registrationFailure;
    private static bool consumeOnRevalidation;
    private static bool consumeOnAuthorization;
    private static bool placementEnabled;
    private readonly List<string> warnings = new();

    [Test]
    public void OrdinaryDisableRetainsNativeMessagesAndReenableUsesExistingOffsets()
    {
        root = new Play(); repaint = true; bridge = Create(); root.AfterBar = root.Stack.OnGUI;
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            placementEnabled = false;
            root.OnGUI();
            Assert.That(root.Stack.FirstObserved, Is.EqualTo(new Rectangle(19, 37, 100, 25)));
            Assert.That(root.Message.LastRectangle, Is.EqualTo(root.Message.DrawnRectangle));
            Assert.That(warnings, Is.Empty, "A requested disable is not an admission warning.");
            placementEnabled = true;
            root.OnGUI();
            Assert.That(root.Stack.FirstObserved, Is.EqualTo(new Rectangle(119, 37, 100, 25)));
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(2));
        }
        finally { Cleanup(); }
    }

    [Test]
    public void WindowStackEntryCommitsSealedMessagesBeforeFirstWindowConsumer()
    {
        root = new Entry(); repaint = true; bridge = Create();
        root.Bar = null;
        root.AfterBar = root.Stack.OnGUI;
        Rectangle beforeBarrier = default;
        root.BetweenCollectionAndBar = () => beforeBarrier = root.Message.LastRectangle;
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            root.OnGUI();
            Assert.That(beforeBarrier, Is.EqualTo(new Rectangle(19, 37, 100, 25)));
            Assert.That(root.Stack.FirstObserved, Is.EqualTo(new Rectangle(119, 37, 100, 25)));
            var window = root.Stack.Windows.Single();
            Assert.That(window.Rectangle, Is.EqualTo(root.Message.LastRectangle));
            Assert.That(root.Message.DrawnRectangle, Is.EqualTo(window.Rectangle));
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(1));
            Assert.That(warnings, Is.Empty);
        }
        finally { Cleanup(); }
    }

    [Test]
    public void RepeatedWindowEntryAfterCommitPreservesNativeCallsWithoutSecondTranslation()
    {
        root = new Entry(); repaint = true; bridge = Create(); root.Bar = null;
        root.AfterBar = () => { root.Stack.OnGUI(); root.Stack.OnGUI(); };
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            root.OnGUI();
            Assert.That(root.Stack.WindowPasses, Is.EqualTo(2));
            Assert.That(root.Stack.FirstObserved, Is.EqualTo(new Rectangle(119, 37, 100, 25)));
            Assert.That(root.Message.LastRectangle, Is.EqualTo(root.Message.DrawnRectangle));
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(1));
            Assert.That(warnings, Is.Empty, "A completed collection needs no second fallback commit.");
        }
        finally { Cleanup(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EarlyWindowConsumptionVetoesLaterMovementAndNextEnvelopeRecovers(bool beforeCollection)
    {
        root = new Entry(); repaint = true;
        root.OnGUI(); // Native retained window exists before any owned observer.
        bridge = Create(); root.Bar = null; root.AfterBar = root.Stack.OnGUI;
        var foreign = new HarmonyLib.Harmony("tests.messages.windows-before-collection");
        var target = typeof(Entry).GetMethod(nameof(Entry.OnGUI))!;
        Rectangle earlyObserved = default;
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            if (beforeCollection)
                foreign.Patch(target, prefix: new HarmonyMethod(typeof(MessagePlacementBridgeTests), nameof(RenderWindowsBeforeCollection))
                    { after = [OwnedHarmonyPatch.OwnerId] });
            else afterDraw = root.Stack.OnGUI;
            root.BetweenCollectionAndBar = () => earlyObserved = root.Stack.FirstObserved;
            root.OnGUI();
            Assert.That(earlyObserved, Is.EqualTo(new Rectangle(19, 37, 100, 25)));
            Assert.That(root.Stack.Windows.Single().Rectangle, Is.EqualTo(earlyObserved));
            Assert.That(root.Message.DrawnRectangle, Is.EqualTo(earlyObserved));
            Assert.That(root.Stack.WindowPasses, Is.EqualTo(2));
            foreign.Unpatch(target, HarmonyPatchType.Prefix, foreign.Id); afterDraw = null;
            root.Stack.OnGUI(); // Outside the envelope must not invalidate the next invocation.
            root.OnGUI();
            Assert.That(root.Stack.FirstObserved, Is.EqualTo(new Rectangle(119, 37, 100, 25)));
            Assert.That(root.Message.DrawnRectangle, Is.EqualTo(root.Stack.FirstObserved));
        }
        finally
        {
            try { foreign.Unpatch(target, HarmonyPatchType.Prefix, foreign.Id); }
            finally { Cleanup(); }
        }
    }
    public static void RenderWindowsBeforeCollection() => root.Stack.OnGUI();

    [TestCase("stale-frame")]
    [TestCase("wrong-stack")]
    [TestCase("play-without-bar")]
    public void UnavailableWindowFallbackPreservesNativeRendering(string reason)
    {
        root = reason == "play-without-bar" ? new Play() : new Entry();
        repaint = true; bridge = Create(); root.Bar = null; root.AfterBar = root.Stack.OnGUI;
        root.BetweenCollectionAndBar = reason == "stale-frame" ? () => contextRevision++ :
            reason == "wrong-stack" ? () => new Stack().OnGUI() : null;
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            root.OnGUI();
            Assert.That(root.Stack.FirstObserved, Is.EqualTo(new Rectangle(19, 37, 100, 25)));
            Assert.That(root.Message.LastRectangle, Is.EqualTo(root.Stack.FirstObserved));
            Assert.That(root.Message.DrawnRectangle, Is.EqualTo(root.Stack.FirstObserved));
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(1));
        }
        finally { Cleanup(); }
    }

    [Test]
    public void PreparedBarThenWindowEntryDoesNotTranslateTwiceAndInputRetainsItsCallback()
    {
        root = new Play(); repaint = true; bridge = Create(); root.AfterBar = root.Stack.OnGUI;
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            root.OnGUI();
            var window = root.Stack.Windows.Single();
            var callback = window.Callback;
            Assert.That(root.Stack.FirstObserved, Is.EqualTo(new Rectangle(119, 37, 100, 25)));
            repaint = false;
            root.OnGUI();
            Assert.That(window.Callback, Is.SameAs(callback));
            Assert.That(root.Message.LastRectangle, Is.EqualTo(root.Stack.FirstObserved));
            Assert.That(root.Message.DrawnRectangle, Is.EqualTo(root.Stack.FirstObserved));
            Assert.That(warnings, Is.Empty);
        }
        finally { Cleanup(); }
    }

    [Test]
    public void ActualPreparedBarBarrierUsesInterveningLayoutBeforeFirstConsumer()
    {
        root = new Play { Bar = new Bar() }; repaint = true; bridge = Create();
        Rectangle beforeBarrier = default;
        root.BetweenCollectionAndBar = () =>
        {
            beforeBarrier = root.Stack.Windows.Single().Rectangle;
            root.Bar.Locations[0] = 220; // Same list, current values prepared later in this invocation.
        };
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            root.OnGUI();
            Assert.That(beforeBarrier, Is.EqualTo(new Rectangle(19, 37, 100, 25)));
            Assert.That(root.Bar.ConsumedMessage, Is.EqualTo(new Rectangle(119, 264, 100, 25)));
            var window = root.Stack.Windows.Single();
            Assert.That(root.Message.LastRectangle, Is.EqualTo(window.Rectangle));
            window.Callback();
            Assert.That(root.Message.DrawnRectangle, Is.EqualTo(window.Rectangle));
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(1));
            Assert.That(warnings, Is.Empty);
        }
        finally { Cleanup(); }
    }

    [Test]
    public void BarHandoffBeforeCollectionCompletesRejectsLaterMovementEvenWithoutCollisionQuery()
    {
        root = new Play { Bar = new Bar { QueryCollision = false } }; repaint = true; bridge = Create();
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            afterDraw = root.Bar.OnGUI;
            root.OnGUI();
            Assert.That(root.Stack.Windows.Single().Rectangle, Is.EqualTo(new Rectangle(19, 37, 100, 25)));
            afterDraw = null;
            root.OnGUI();
            Assert.That(root.Stack.Windows.Single().Rectangle, Is.EqualTo(new Rectangle(119, 164, 100, 25)));
        }
        finally { Cleanup(); }
    }

    [Test]
    public void BarHandoffBeforeMessageCollectionStartsRejectsLaterMovementWithoutCollisionQuery()
    {
        root = new Play { Bar = new Bar { QueryCollision = false } }; repaint = true;
        root.OnGUI(); // Seed a native retained window before installing the owned observers.
        bridge = Create();
        root.Bar.ConsumedMessage = default; // The next assertion must observe this invocation's early bar call.
        Rectangle earlierConsumption = default;
        root.BetweenCollectionAndBar = () => earlierConsumption = root.Bar.ConsumedMessage;
        var foreign = new HarmonyLib.Harmony("tests.messages.bar-before-collection");
        var target = typeof(Play).GetMethod(nameof(Play.OnGUI))!;
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(MessagePlacementBridgeTests), nameof(RenderBarBeforeMessageCollection))
                { after = [OwnedHarmonyPatch.OwnerId] });
            root.OnGUI();
            Assert.That(earlierConsumption, Is.EqualTo(new Rectangle(19, 37, 100, 25)), "The early bar consumed native geometry inside the active outer invocation.");
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(2));
            Assert.That(root.Stack.Windows.Single().Rectangle, Is.EqualTo(earlierConsumption), "A later handoff must not move messages after the early bar already consumed their geometry.");
            foreign.Unpatch(target, HarmonyPatchType.Prefix, foreign.Id);
            root.Bar.OnGUI(); // Outside an active envelope: no history may leak into the next invocation.
            root.OnGUI();
            Assert.That(root.Stack.Windows.Single().Rectangle, Is.EqualTo(new Rectangle(119, 164, 100, 25)));
        }
        finally
        {
            try { foreign.Unpatch(target, HarmonyPatchType.Prefix, foreign.Id); }
            finally { Cleanup(); }
        }
    }
    public static void RenderBarBeforeMessageCollection() => root.Bar!.OnGUI();

    [Test]
    public void OuterNativeExceptionAfterCollectionDiscardsPendingPlacementAndNextInvocationRecovers()
    {
        root = new Play(); repaint = true; bridge = Create();
        var native = new InvalidOperationException("intervening native GUI failed");
        root.BetweenCollectionAndBar = () => throw native;
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            Assert.That(Assert.Throws<InvalidOperationException>(root.OnGUI), Is.SameAs(native));
            Assert.That(bridge.CommitPrepared(root, MessageBarBoundsReceipt.Visible(260, () => true), () => true), Is.False);
            Assert.That(root.Stack.Windows.Single().Rectangle, Is.EqualTo(new Rectangle(19, 37, 100, 25)));
            root.BetweenCollectionAndBar = null;
            root.OnGUI();
            Assert.That(root.Stack.Windows.Single().Rectangle, Is.EqualTo(new Rectangle(119, 37, 100, 25)));
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(2));
            Assert.That(warnings, Is.Empty);
        }
        finally { Cleanup(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ConsumptionDuringFinalWindowValidationCannotAuthorizeLaterWrites(bool duringAuthorization)
    {
        root = new Entry(); repaint = true; bridge = Create();
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            consumeOnRevalidation = !duringAuthorization;
            consumeOnAuthorization = duringAuthorization;
            root.OnGUI();
            Assert.That(root.Stack.Windows[0].Rectangle.X, Is.EqualTo(19));
            Assert.That(root.Message.LastRectangle.X, Is.EqualTo(19));
            Assert.That(warnings, Has.Count.EqualTo(1));
            consumeOnRevalidation = consumeOnAuthorization = false;
            root.OnGUI();
            Assert.That(root.Stack.Windows[0].Rectangle.X, Is.EqualTo(119));
        }
        finally { Cleanup(); }
    }

    [Test]
    public void AdmissionLostBetweenPassesWarnsOnceAndReturnsToNativePlacement()
    {
        root = new Entry(); repaint = true; bridge = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.bridge-lost-order");
        var target = typeof(Stack).GetMethod(nameof(Stack.Register))!;
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            root.OnGUI();
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(MessageRegistrationGuardTests), nameof(MessageRegistrationGuardTests.AddNops))
                { after = [OwnedHarmonyPatch.OwnerId] });
            root.OnGUI(); root.OnGUI();
            Assert.That(root.Stack.Windows[0].Rectangle.X, Is.EqualTo(19));
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(bridge.Available, Is.False);
        }
        finally { Cleanup(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    [TestCase("draw")]
    [TestCase("queue")]
    [TestCase("context")]
    [TestCase("consumer")]
    public void ChangedPassAuthorityDeclinesThatPassAndACompatibleLaterPassRecovers(string change)
    {
        root = new Play(); repaint = true; bridge = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.bridge-repatch");
        MethodInfo target = change == "draw" ? typeof(Message).GetMethod(nameof(Message.Draw))! : typeof(Stack).GetMethod(nameof(Stack.Register))!;
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            afterDraw = () =>
            {
                afterDraw = null;
                if (change == "context") contextRevision++;
                else if (change == "consumer") root.Stack.Windows[0].Callback();
                else foreign.Patch(target, transpiler: new(typeof(MessageRegistrationGuardTests), nameof(MessageRegistrationGuardTests.AddNops)));
            };
            root.OnGUI();
            Assert.That(root.Stack.Windows[0].Rectangle.X, Is.EqualTo(19));
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(bridge.Available, Is.True);
            root.OnGUI();
            Assert.That(root.Stack.Windows[0].Rectangle.X, Is.EqualTo(119));
            Assert.That(warnings, Has.Count.EqualTo(1));
        }
        finally { Cleanup(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    [Test]
    public void DeferredConsumerFailureRollsBackEarlierReceiptsAndNeverPublishes()
    {
        root = new Entry(); repaint = true; bridge = Create(true);
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.False);
            Assert.That(bridge.Available, Is.False);
            foreach (var target in new[] { typeof(Message).GetMethod(nameof(Message.Draw))!,
                typeof(Stack).GetMethod(nameof(Stack.Register))!, typeof(Entry).GetMethod(nameof(Entry.OnGUI))! })
                Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)?.Owners.Contains(OwnedHarmonyPatch.OwnerId) ?? false, Is.False);
            root.OnGUI();
            Assert.That(root.Stack.Windows[0].Rectangle.X, Is.EqualTo(19));
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(1));
        }
        finally { Cleanup(); }
    }

    [Test]
    public void NativeRegistrationExceptionIsNotReplayedOrSwallowedAndNextPassRecovers()
    {
        root = new Entry(); repaint = true; bridge = Create();
        var native = new InvalidOperationException("registration failed");
        try
        {
            root.OnGUI(); // No admission: ordinary native placement.
            Assert.That(root.Stack.Windows[0].Rectangle.X, Is.EqualTo(19));
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True);
            registrationFailure = native;
            Assert.That(Assert.Throws<InvalidOperationException>(root.OnGUI), Is.SameAs(native));
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(2));
            registrationFailure = null;
            root.OnGUI();
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(3));
            Assert.That(root.Stack.Windows[0].Rectangle.X, Is.EqualTo(119));
            Assert.That(warnings, Is.Empty);
        }
        finally { registrationFailure = null; Cleanup(); }
    }

    [Test]
    public void StaleSecondWindowRejectsTheWholeBatchWithOneWarningAndPreservesForeignMutation()
    {
        root = new Entry { Second = new Message { Id = 8 } }; repaint = true;
        bridge = Create();
        var foreignBounds = new Rectangle(50, 90, 70, 20);
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True, string.Join("; ", warnings));
            afterDraw = () => root.Stack.Windows[1].Rectangle = foreignBounds;
            root.OnGUI();
            Assert.That(root.Stack.Windows[0].Rectangle, Is.EqualTo(new Rectangle(19, 37, 100, 25)));
            Assert.That(root.Stack.Windows[1].Rectangle, Is.EqualTo(foreignBounds));
            Assert.That(warnings, Has.Count.EqualTo(1));
            afterDraw = null;
            root.OnGUI();
            Assert.That(root.Stack.Windows[0].Rectangle.X, Is.EqualTo(119));
            Assert.That(root.Stack.Windows[1].Rectangle.X, Is.EqualTo(119));
            Assert.That(warnings, Has.Count.EqualTo(1));
        }
        finally { Cleanup(); }
    }

    [Test]
    public void ActualOwnedCompositionMovesTheRegisteredWindowAndRetainedInputCallbackTogether()
    {
        root = new Entry(); repaint = true;
        bridge = Create();
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(bridge.Group, AdmissionPhase.Early), Is.True,
                string.Join("; ", warnings));
            root.OnGUI();
            var window = root.Stack.Windows.Single();
            Assert.That(window.Rectangle, Is.EqualTo(new Rectangle(119, 37, 100, 25)));
            Assert.That(root.Message.LastRectangle, Is.EqualTo(window.Rectangle));
            Action retained = window.Callback;
            repaint = false;
            root.OnGUI();
            Assert.That(window.Callback, Is.SameAs(retained));
            Assert.That(root.Message.LastRectangle, Is.EqualTo(window.Rectangle));
            window.Callback();
            Assert.That(root.Message.DrawnRectangle, Is.EqualTo(window.Rectangle));
            Assert.That(root.Stack.RegisterCalls, Is.EqualTo(2));
            Assert.That(warnings, Is.Empty);
        }
        finally { Cleanup(); }
    }

    private MessagePlacementBridge Create(bool badCollision = false)
    {
        warnings.Clear(); afterDraw = null; contextRevision = 0; registrationFailure = null;
        consumeOnRevalidation = consumeOnAuthorization = false;
        placementEnabled = true;
        if (root.Bar == null) { root.Bar = new(); root.Bar.Locations[0] = -40; }
        const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        return new(new(typeof(Message).GetMethod(nameof(Message.Draw))!, typeof(Message).GetMethod(nameof(Message.CalculateRect))!,
            typeof(Stack).GetMethod(nameof(Stack.Register))!, typeof(Stack).GetMethod("AddNew", hidden)!,
            typeof(MessagePlacementBridgeTests).GetMethod(nameof(MessagesGui))!, typeof(Root).GetMethod(nameof(Root.OnGUI))!,
            typeof(Entry).GetMethod(nameof(Entry.OnGUI))!, typeof(Play).GetMethod(nameof(Play.OnGUI))!,
            typeof(MessagePlacementBridgeTests).GetMethod(badCollision ? nameof(WrongCollides) : nameof(Collides))!, typeof(Message).GetField(nameof(Message.LastRectangle))!,
            typeof(Window).GetField(nameof(Window.Rectangle))!, typeof(Window).GetField(nameof(Window.Callback))!),
            typeof(MessagePlacementBridgeTests).GetMethod(nameof(Forward))!, value => ReferenceEquals(value, root),
            () =>
            {
                int revision = contextRevision;
                int enumerations = 0;
                int validations = 0;
                IEnumerable<object> CurrentWindows()
                {
                    if (++enumerations == 2 && consumeOnRevalidation) Collides(default, out _);
                    foreach (var window in root.Stack.Windows) yield return window;
                }
                return new(root, root.Stack, CurrentWindows(), 1000, 800, repaint, new(10, 0), () =>
                {
                    if (++validations == 2 && consumeOnAuthorization) Collides(default, out _);
                    return revision == contextRevision;
                });
            },
            bounds => new Rectangle((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height), warnings.Add,
            new(typeof(Bar).GetMethod(nameof(Bar.OnGUI))!, typeof(Bar).GetProperty(nameof(Bar.Entries))!.GetMethod!,
                typeof(Bar).GetProperty(nameof(Bar.ShowGroupFrames))!.GetMethod!),
            (actual, entries, frames) =>
            {
                var bar = (Bar)actual;
                double location = bar.Locations[0];
                return MessageBarBoundsReceipt.Visible(location + 40, () => ReferenceEquals(root.Bar, bar) &&
                    ReferenceEquals(bar.RawEntries, entries) && bar.Locations[0] == location);
            }, typeof(Stack).GetMethod(nameof(Stack.OnGUI))!,
            (actualRoot, actualStack) => actualRoot is Entry && ReferenceEquals(actualRoot, root) &&
                ReferenceEquals(root.Stack, actualStack) && root.Bar == null
                ? MessageBarBoundsReceipt.Absent(() => ReferenceEquals(actualRoot, root) && ReferenceEquals(root.Stack, actualStack) && root.Bar == null)
                : null, () => placementEnabled);
    }
    private static void Cleanup()
    {
        if (bridge != null)
            foreach (var candidate in bridge.Group.Patches.Reverse()) candidate.Rollback();
        bridge = null; afterDraw = null;
    }
    public static void Forward(Stack stack, int id, Rectangle rectangle, int layer, Action callback,
        bool background, bool absorb, float shadow, Action? outside, bool ignore, Message message)
    {
        stack.Register(id, rectangle, layer, callback, background, absorb, shadow, outside, ignore);
        bridge?.Observe(stack, message, rectangle, callback, new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void MessagesGui() { root.Message.Draw(19, 37); root.Second?.Draw(19, 63); afterDraw?.Invoke(); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Collides(Rectangle rectangle, out float alpha) { alpha = 1; return true; }
    public static bool WrongCollides(Rectangle rectangle, ref float alpha) => true;
    public readonly record struct Rectangle(float X, float Y, float Width, float Height);
    public sealed class Window { public int Id; public Rectangle Rectangle; public Action Callback = null!; }
    public sealed class Stack
    {
        public readonly List<Window> Windows = new();
        public int RegisterCalls;
        public Rectangle FirstObserved;
        public int WindowPasses;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OnGUI()
        {
            WindowPasses++;
            FirstObserved = Windows.Count == 0 ? default : Windows[0].Rectangle;
            foreach (var window in Windows) window.Callback();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Register(int id, Rectangle rectangle, int layer, Action callback, bool background, bool absorb,
            float shadow, Action? outside, bool ignore)
        {
            RegisterCalls++;
            if (registrationFailure != null) throw registrationFailure;
            if (!repaint) return;
            for (int index = 0; index < Windows.Count; index++)
                if (Windows[index].Id == id)
                {
                    var window = Windows[index];
                    window.Rectangle = rectangle; window.Callback = callback;
                    return;
                }
            AddNew(id, rectangle, layer, callback, background, absorb, shadow, outside, ignore);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddNew(int id, Rectangle rectangle, int layer, Action callback, bool background, bool absorb,
            float shadow, Action? outside, bool ignore)
        { var window = new Window { Id = id }; window.Rectangle = rectangle; window.Callback = callback; Windows.Add(window); }
    }
    public sealed class Message
    {
        public int Id = 7;
        public Rectangle LastRectangle, DrawnRectangle;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Rectangle CalculateRect(float x, float y) => LastRectangle = new(x, y, 100, 25);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Draw(int x, int y)
        {
            var rectangle = CalculateRect(x, y);
            root.Stack.Register(Id, rectangle, 3, () => DrawnRectangle = rectangle, true, false, 1, null, false);
        }
    }
    public class Root
    {
        public Stack Stack = new();
        public Message Message = new();
        public Message? Second;
        public Bar? Bar;
        public Action? BetweenCollectionAndBar;
        public Action? AfterBar;
        [MethodImpl(MethodImplOptions.NoInlining)] public virtual void OnGUI() => MessagesGui();
    }
    public sealed class Entry : Root
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public override void OnGUI()
        { base.OnGUI(); BetweenCollectionAndBar?.Invoke(); Bar?.OnGUI(); AfterBar?.Invoke(); }
    }
    public sealed class Play : Root
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public override void OnGUI()
        { base.OnGUI(); BetweenCollectionAndBar?.Invoke(); Bar?.OnGUI(); AfterBar?.Invoke(); }
    }
    public sealed class Bar
    {
        private readonly List<int> entries = new() { 1 };
        public readonly List<double> Locations = new() { 120 };
        public Rectangle ConsumedMessage;
        public bool QueryCollision = true;
        public object RawEntries => entries;
        public List<int> Entries { [MethodImpl(MethodImplOptions.NoInlining)] get => entries; }
        public bool ShowGroupFrames { [MethodImpl(MethodImplOptions.NoInlining)] get => Entries.Count > 1; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OnGUI()
        {
            var current = Entries;
            bool frames = ShowGroupFrames;
            for (int index = 0; index < Locations.Count; index++) Consume(current[index], frames);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Consume(int entry, bool frames)
        {
            ConsumedMessage = root.Stack.Windows[0].Rectangle;
            if (QueryCollision) Collides(ConsumedMessage, out _);
        }
    }
}
