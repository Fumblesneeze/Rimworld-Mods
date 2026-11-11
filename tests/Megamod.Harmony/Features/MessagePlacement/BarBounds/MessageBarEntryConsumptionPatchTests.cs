using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement.BarBounds;

[TestFixture, NonParallelizable]
public sealed class MessageBarEntryConsumptionPatchTests
{
    private static readonly MethodInfo Target = typeof(Bar).GetMethod(nameof(Bar.OnGUI))!;
    private static readonly MethodInfo Clicks = typeof(Bar).GetMethod(nameof(Bar.HandleClicks))!;
    private static readonly MethodInfo Draw = typeof(Bar).GetMethod(nameof(Bar.DrawColonist))!;

    [TestCase(false)]
    [TestCase(true)]
    public void InnerRectangleArgumentRewriteRevokesOnlyEntryAuthorityAndPreservesNativeEffects(bool rewriteDraw)
    {
        var foreign = new HarmonyLib.Harmony("tests.message-bar.entry-arguments");
        int handoffs = 0;
        var failures = new List<Exception>();
        var patch = Create((_, _, _, authority) => { Assert.That(authority(), Is.True); handoffs++; }, failures.Add);
        var bar = new Bar();
        try
        {
            Admit(patch);
            Assert.That(patch.EntryArgumentsAvailable, Is.True);
            var oldAuthority = patch.CaptureEntryArgumentsAuthority();
            bar.OnGUI();
            Assert.That(bar.Clicked, Is.EqualTo(new[] { 10f, 20f }));
            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(typeof(MessageBarEntryConsumptionPatchTests), nameof(RewriteRectangle))
                { innerMethod = new InnerMethod(rewriteDraw ? Draw : Clicks) }).Patch();
            Assert.That(patch.RefreshPublication(), Is.True, "The timing handoff remains valid.");
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(bar.Clicked, Is.EqualTo(rewriteDraw ? new[] { 10f, 20f, 10f, 20f } : new[] { 10f, 20f, 60f, 70f }), "The actual foreign rewrite still reaches native handling.");
                Assert.That(bar.Drawn, Is.EqualTo(rewriteDraw ? new[] { 10f, 20f, 60f, 70f } : new[] { 10f, 20f, 10f, 20f }));
                Assert.That(handoffs, Is.EqualTo(2));
                Assert.That(patch.EntryArgumentsAvailable, Is.False, "Selected entry arguments were rewritten after the incoming stream.");
                Assert.That(patch.EntryArgumentsFailure, Is.Not.Empty);
                Assert.That(oldAuthority(), Is.False);
                Assert.That(failures, Is.Empty);
            });
            var deniedAuthority = patch.CaptureEntryArgumentsAuthority();
            foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id);
            Assert.That(patch.RefreshPublication(), Is.True);
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(patch.EntryArgumentsAvailable, Is.True);
                Assert.That(patch.EntryArgumentsFailure, Is.Null);
                Assert.That(patch.CaptureEntryArgumentsAuthority()(), Is.True);
                Assert.That(oldAuthority(), Is.False, "A new publication must not revive the old body's authority.");
                Assert.That(deniedAuthority(), Is.False);
                Assert.That(bar.Clicked.TakeLast(2), Is.EqualTo(new[] { 10f, 20f }));
                Assert.That(bar.Drawn.TakeLast(2), Is.EqualTo(new[] { 10f, 20f }));
                Assert.That(handoffs, Is.EqualTo(3));
                Assert.That(failures, Is.Empty);
            });
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    public static void RewriteRectangle(ref Rectangle __0) => __0.X += 50;

    [Test]
    public void UnrelatedResultRewriteAndRetainedGeometryKeepSelectedEntryArgumentsAvailable()
    {
        var foreign = new HarmonyLib.Harmony("tests.message-bar.unrelated-result");
        var failures = new List<Exception>();
        var patch = Create((_, _, _, _) => { }, failures.Add);
        var bar = new Bar();
        try
        {
            Admit(patch);
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(typeof(MessageBarEntryConsumptionPatchTests), nameof(RewriteProbe))
                { innerMethod = new InnerMethod(typeof(Bar).GetMethod(nameof(Bar.Probe))!) }).Patch();
            Assert.That(patch.RefreshPublication(), Is.True);
            var authority = patch.CaptureEntryArgumentsAuthority();
            bar.OnGUI(); bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(bar.ProbeResult, Is.EqualTo(91));
                Assert.That(authority(), Is.True);
                Assert.That(bar.Clicked, Is.EqualTo(new[] { 10f, 20f, 10f, 20f }));
                Assert.That(bar.Drawn, Is.EqualTo(bar.Clicked));
                Assert.That(failures, Is.Empty);
            });
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    public static void RewriteProbe(ref int __result) => __result = 91;

    [TestCase(false)]
    [TestCase(true)]
    public void UnavailableEntryDescriptionKeepsTimingAndNativeCalls(bool unreadable)
    {
        int handoffs = 0;
        var failures = new List<Exception>();
        var participant = unreadable ? new MessageBarEntryConsumptionPatch(null!, Clicks, Draw) :
            new(typeof(Rectangle), Clicks, typeof(Bar).GetMethod(nameof(Bar.UnusedDraw))!);
        var patch = Create((_, _, _, authority) => { Assert.That(authority(), Is.True); handoffs++; }, failures.Add, participant);
        var bar = new Bar();
        try
        {
            Admit(patch);
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(handoffs, Is.EqualTo(1));
                Assert.That(patch.EntryArgumentsAvailable, Is.False);
                Assert.That(patch.CaptureEntryArgumentsAuthority()(), Is.False);
                Assert.That(patch.EntryArgumentsFailure, Is.Not.Empty);
                Assert.That(bar.Clicked, Is.EqualTo(new[] { 10f, 20f }));
                Assert.That(bar.Drawn, Is.EqualTo(bar.Clicked));
                Assert.That(failures, Is.Empty);
            });
        }
        finally { patch.Candidate.Rollback(); }
    }

    private static MessageBarPreparedPatch Create(Action<object, object, bool, Func<bool>> handoff, Action<Exception> failure,
        MessageBarEntryConsumptionPatch? participant = null) =>
        new(new(Target, typeof(Bar).GetProperty(nameof(Bar.Entries))!.GetMethod!,
            typeof(Bar).GetProperty(nameof(Bar.ShowGroupFrames))!.GetMethod!), handoff, failure,
            participant ?? new(typeof(Rectangle), Clicks, Draw));

    private static void Admit(MessageBarPreparedPatch patch)
    {
        var warnings = new List<string>();
        Assert.That(new AdmissionCoordinator(warnings.Add).Apply(
            new("bar-entry-arguments", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True,
            string.Join("; ", warnings));
        patch.Enable();
    }

    public struct Rectangle
    {
        public float X, Y, Width, Height;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Rectangle(float x, float y, float width, float height)
        { X = x; Y = y; Width = width; Height = height; }
    }

    public sealed class Bar
    {
        private readonly List<float> entries = new() { 10, 20 };
        public readonly List<float> Clicked = new(), Drawn = new();
        public int ProbeResult;
        public List<float> Entries { [MethodImpl(MethodImplOptions.NoInlining)] get => entries; }
        public bool ShowGroupFrames { [MethodImpl(MethodImplOptions.NoInlining)] get => false; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OnGUI()
        {
            var current = Entries;
            bool frames = ShowGroupFrames;
            ProbeResult = Probe();
            for (int i = 0; i < current.Count; i++)
            {
                Rectangle rect = default;
                rect = new Rectangle(current[i], 0, 48, 48);
                HandleClicks(rect, frames);
                DrawColonist(rect, frames);
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public void HandleClicks(Rectangle rect, bool frames) => Clicked.Add(rect.X);
        [MethodImpl(MethodImplOptions.NoInlining)] public void DrawColonist(Rectangle rect, bool frames) => Drawn.Add(rect.X);
        [MethodImpl(MethodImplOptions.NoInlining)] public void UnusedDraw(Rectangle rect, bool frames) => Drawn.Add(-1);
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Probe() => 1;
    }
}
