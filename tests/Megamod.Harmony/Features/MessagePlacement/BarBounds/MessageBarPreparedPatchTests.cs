using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement.BarBounds;

[TestFixture, NonParallelizable]
public sealed class MessageBarPreparedPatchTests
{
    private static readonly MethodInfo Target = typeof(Bar).GetMethod(nameof(Bar.OnGUI))!;
    private static readonly MethodInfo Entries = typeof(Bar).GetProperty(nameof(Bar.Entries))!.GetMethod!;
    private static readonly MethodInfo Frames = typeof(Bar).GetProperty(nameof(Bar.ShowGroupFrames))!.GetMethod!;
    private static int observations;
    private static readonly ManualResetEventSlim CompositionEntered = new(false);
    private static readonly ManualResetEventSlim ReleaseComposition = new(false);

    [Test]
    public void BranchBypassingPreparationCannotAuthorizePlacementBeforeNativeConsumption()
    {
        var type = typeof(BypassBar);
        int handoffs = 0;
        var patch = new MessageBarPreparedPatch(new(type.GetMethod(nameof(BypassBar.OnGUI))!,
            type.GetProperty(nameof(BypassBar.Entries))!.GetMethod!,
            type.GetProperty(nameof(BypassBar.ShowGroupFrames))!.GetMethod!,
            new[] { type.GetMethod(nameof(BypassBar.Consume))! }), (_, _, _, _) => handoffs++, _ => { });
        var bar = new BypassBar();
        try
        {
            var warnings = new List<string>();
            bool admitted = new AdmissionCoordinator(warnings.Add).Apply(
                new("bar-flow", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early);
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(admitted, Is.False, "The configured consumer can execute without the prepared handoff.");
                Assert.That(bar.Consumed, Is.EqualTo(new[] { 7 }));
                Assert.That(handoffs, Is.Zero);
            });
        }
        finally { patch.Candidate.Rollback(); }
    }

    public sealed class BypassBar
    {
        private readonly List<int> values = new() { 7 };
        public readonly List<int> Consumed = new();
        public bool Bypass = true;
        public List<int> Entries { [MethodImpl(MethodImplOptions.NoInlining)] get => values; }
        public bool ShowGroupFrames { [MethodImpl(MethodImplOptions.NoInlining)] get => true; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OnGUI()
        {
            var current = values;
            bool frames = false;
            if (!Bypass) { current = Entries; frames = ShowGroupFrames; }
            Consume(current, frames);
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public void Consume(List<int> current, bool frames) => Consumed.AddRange(current);
    }

    [Test]
    public void DominatedLoopConsumersObserveOneHandoffAndRetainNativeValues()
    {
        int handoffs = 0;
        var patch = new MessageBarPreparedPatch(new(Target, Entries, Frames,
            new[] { typeof(Bar).GetMethod("Consume", BindingFlags.NonPublic | BindingFlags.Instance)! }),
            (_, _, _, authority) => { Assert.That(authority(), Is.True); handoffs++; }, _ => { });
        var bar = new Bar();
        try
        {
            Admit(patch);
            bar.OnGUI();
            Assert.That(handoffs, Is.EqualTo(1));
            Assert.That(bar.Consumed, Is.EqualTo(new[] { 1, 2 }));
        }
        finally { patch.Candidate.Rollback(); }
    }

    [Test]
    public void OversizedPublishedMetadataCannotPreventForeignRemovalAndLaterRecovery()
    {
        var foreign = new HarmonyLib.Harmony(new string('p', 4 * 1024 * 1024));
        int handoffs = 0;
        Func<bool>? current = null;
        var failures = new List<Exception>();
        var patch = new MessageBarPreparedPatch(new(Target, Entries, Frames), (_, _, _, valid) =>
        { handoffs++; current = valid; }, failures.Add);
        var bar = new Bar();
        try
        {
            Admit(patch);
            bar.OnGUI();
            Assert.That(handoffs, Is.EqualTo(1));
            Assert.That(current!(), Is.True);
            foreign.Patch(Target, transpiler: new(typeof(MessageBarPreparedPatchTests), nameof(KeepIncoming)));
            Assert.That(patch.Available, Is.False);
            Assert.That(current(), Is.False, "The old handoff cannot retain authority through a rebuild.");
            Assert.DoesNotThrow(() => foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id));
            bar.Consumed.Clear(); bar.ConsumedFrames.Clear();
            bar.OnGUI();
            Assert.That(handoffs, Is.EqualTo(1), "An uninspectable rebuild must preserve native consumption without an owned handoff.");
            Assert.That(bar.Consumed, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(bar.ConsumedFrames, Is.EqualTo(new[] { true, true }));
            Assert.That(patch.Available, Is.False);

            new HarmonyLib.Harmony("tests.message-bar.prepared.metadata-recovery").CreateProcessor(Target).Patch();
            bar.Entries[0] = 5;
            bar.Consumed.Clear(); bar.ConsumedFrames.Clear();
            bar.OnGUI();
            Assert.That(handoffs, Is.EqualTo(2));
            Assert.That(current!(), Is.True);
            Assert.That(patch.Available, Is.True);
            Assert.That(bar.Consumed, Is.EqualTo(new[] { 5, 2 }));
            Assert.That(bar.ConsumedFrames, Is.EqualTo(new[] { true, true }));
            Assert.That(failures, Is.Empty);
        }
        finally
        {
            // The regression can prevent removal while metadata is oversized. Remove only
            // the acquired observer first so failed RED cleanup does not repeat that defect.
            try { new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(Target, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId); }
            finally
            {
                try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
                finally { patch.Candidate.Rollback(); }
            }
        }
    }
    public static IEnumerable<CodeInstruction> KeepIncoming(IEnumerable<CodeInstruction> instructions) => instructions;

    [Test]
    public void UnchangedRendersRetainAdmissionDuringUnrelatedCompositionAndRepatchStillRevokesIt()
    {
        var foreign = new HarmonyLib.Harmony("tests.message-bar.prepared.cached-admission");
        var unrelated = typeof(MessageBarPreparedPatchTests).GetMethod(nameof(UnrelatedTarget))!;
        var values = new List<int>();
        var failures = new List<Exception>();
        var patch = new MessageBarPreparedPatch(new(Target, Entries, Frames), (_, entries, _, valid) =>
        { if (valid()) values.Add(((List<int>)entries)[0]); }, failures.Add);
        Task? composition = null;
        CompositionEntered.Reset(); ReleaseComposition.Reset();
        try
        {
            Admit(patch);
            composition = Task.Run(() => foreign.Patch(unrelated, transpiler:
                new HarmonyMethod(typeof(MessageBarPreparedPatchTests), nameof(BlockUnrelatedComposition))));
            Assert.That(CompositionEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            var bar = new Bar();
            bar.OnGUI();
            bar.Entries[0] = 4;
            bar.OnGUI();
            ReleaseComposition.Set();
            composition.GetAwaiter().GetResult();
            Assert.That(values, Is.EqualTo(new[] { 1, 4 }), "Stable admitted code must not depend on rereading busy provider metadata; each handoff still observes current native values.");

            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(typeof(MessageBarPreparedPatchTests), nameof(Skip))
                { innerMethod = new InnerMethod(typeof(MessageBarPreparedPatch).GetMethod("Observe", BindingFlags.NonPublic | BindingFlags.Static)!) }).Patch();
            Assert.That(patch.Available, Is.False);
            Assert.That(patch.RefreshPublication(), Is.False);
            bar.OnGUI();
            Assert.That(values, Has.Count.EqualTo(2));
            foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id);
            Assert.That(patch.Available, Is.False);
            foreign.Unpatch(unrelated, HarmonyPatchType.Transpiler, foreign.Id);
            CompositionEntered.Reset(); ReleaseComposition.Reset();
            composition = Task.Run(() => foreign.Patch(unrelated, transpiler:
                new HarmonyMethod(typeof(MessageBarPreparedPatchTests), nameof(BlockUnrelatedComposition))));
            Assert.That(CompositionEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            bar.OnGUI(); // This generation has never been admitted: busy metadata must still refuse.
            ReleaseComposition.Set();
            composition.GetAwaiter().GetResult();
            Assert.That(values, Has.Count.EqualTo(2));
            Assert.That(patch.Available, Is.False);
            bar.OnGUI(); // A fresh compatible generation refreshes on its first invocation.
            Assert.That(values, Is.EqualTo(new[] { 1, 4, 4 }));
            Assert.That(failures, Is.Empty);
        }
        finally
        {
            ReleaseComposition.Set();
            try { composition?.GetAwaiter().GetResult(); }
            finally
            {
                try
                {
                    foreign.Unpatch(unrelated, HarmonyPatchType.Transpiler, foreign.Id);
                    foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id);
                }
                finally { patch.Candidate.Rollback(); }
            }
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void UnrelatedTarget() { }
    public static IEnumerable<CodeInstruction> BlockUnrelatedComposition(IEnumerable<CodeInstruction> instructions)
    {
        CompositionEntered.Set();
        if (!ReleaseComposition.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Owned composition fixture was not released.");
        return instructions.ToArray();
    }

    [Test]
    public void PublishedInnerSkipOfOwnedHandoffDeclinesThenRemovalRestoresFreshHandoff()
    {
        var foreign = new HarmonyLib.Harmony("tests.message-bar.prepared.skip");
        int handoffs = 0;
        var failures = new List<Exception>();
        var patch = new MessageBarPreparedPatch(new(Target, Entries, Frames), (_, _, _, _) => handoffs++, failures.Add);
        var bar = new Bar();
        try
        {
            Admit(patch);
            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(typeof(MessageBarPreparedPatchTests), nameof(Skip))
            { innerMethod = new InnerMethod(typeof(MessageBarPreparedPatch).GetMethod("Observe", BindingFlags.NonPublic | BindingFlags.Static)!) }).Patch();
            Assert.That(patch.RefreshPublication(), Is.False);
            Assert.That(patch.Available, Is.False);
            bar.OnGUI();
            Assert.That(handoffs, Is.Zero);
            Assert.That(bar.Consumed, Is.EqualTo(new[] { 1, 2 }));
            foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id);
            Assert.That(patch.RefreshPublication(), Is.True);
            bar.OnGUI();
            Assert.That(handoffs, Is.EqualTo(1));
            Assert.That(failures, Is.Empty);
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }
    public static bool Skip() => false;

    [Test]
    public void CompatibleEffectiveGetterResultIsTheSameListHandedToNativeConsumption()
    {
        var foreign = new HarmonyLib.Harmony("tests.message-bar.prepared.result");
        object? actualBar = null, actualEntries = null;
        bool? actualFrames = null;
        Func<bool>? current = null;
        var failures = new List<Exception>();
        var patch = new MessageBarPreparedPatch(new(Target, Entries, Frames), (bar, entries, frames, valid) =>
        { actualBar = bar; actualEntries = entries; actualFrames = frames; current = valid; }, failures.Add);
        var target = new Bar();
        try
        {
            Admit(patch);
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(typeof(MessageBarPreparedPatchTests), nameof(ReplaceEntries))
                { innerMethod = new InnerMethod(Entries) }).Patch();
            Assert.That(patch.RefreshPublication(), Is.True);
            target.OnGUI();
            Assert.That(actualBar, Is.SameAs(target));
            Assert.That(actualEntries, Is.SameAs(Replacement));
            Assert.That(target.Consumed, Is.EqualTo(Replacement));
            Assert.That(actualFrames, Is.True);
            Assert.That(target.ConsumedFrames, Is.EqualTo(new[] { true }));
            Assert.That(current!(), Is.True);
            foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id);
            Assert.That(current(), Is.False, "An old in-flight handoff cannot borrow the replacement generation.");
            Assert.That(failures, Is.Empty);
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }
    private static readonly List<int> Replacement = new() { 7 };
    public static void ReplaceEntries(ref List<int> __result) => __result = Replacement;

    [TestCase(false)]
    [TestCase(true)]
    public void UnrelatedSkipAndReadonlyHandoffObservationRemainCompatibleWithoutInspectionExecution(bool observeHandoff)
    {
        var foreign = new HarmonyLib.Harmony("tests.message-bar.prepared.compatible");
        int handoffs = 0;
        var failures = new List<Exception>();
        var patch = new MessageBarPreparedPatch(new(Target, Entries, Frames), (_, _, _, _) => handoffs++, failures.Add);
        try
        {
            Admit(patch);
            MethodInfo selected = observeHandoff
                ? typeof(MessageBarPreparedPatch).GetMethod("Observe", BindingFlags.NonPublic | BindingFlags.Static)!
                : typeof(Bar).GetMethod(nameof(Bar.Probe))!;
            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(typeof(MessageBarPreparedPatchTests),
                observeHandoff ? nameof(ObserveNoArguments) : nameof(Skip)) { innerMethod = new InnerMethod(selected) }).Patch();
            observations = 0;
            Assert.That(patch.RefreshPublication(), Is.True);
            Assert.That(observations, Is.Zero);
            new Bar().OnGUI();
            Assert.That(handoffs, Is.EqualTo(1));
            Assert.That(observations, Is.EqualTo(observeHandoff ? 2 : 0));
            Assert.That(failures, Is.Empty);
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }
    public static void ObserveNoArguments() => observations++;

    [Test]
    public void InheritedGettersPreserveActualDerivedRendererReceiverAndConsumedLocals()
    {
        var target = typeof(InheritedBar).GetMethod(nameof(InheritedBar.RenderInherited))!;
        object? observed = null, entries = null;
        bool? frames = null;
        var patch = new MessageBarPreparedPatch(new(target, Entries, Frames), (actual, returned, grouped, _) =>
        { observed = actual; entries = returned; frames = grouped; }, _ => { });
        var bar = new InheritedBar();
        try
        {
            Admit(patch);
            bar.RenderInherited();
            Assert.That(observed, Is.SameAs(bar));
            Assert.That(entries, Is.SameAs(bar.Entries));
            Assert.That(frames, Is.True);
            Assert.That(bar.Consumed, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(bar.ConsumedFrames, Is.EqualTo(new[] { true, true }));
        }
        finally { patch.Candidate.Rollback(); }
    }

    [TestCase(nameof(Bar.MissingPreparation))]
    [TestCase(nameof(Bar.DuplicateEntries))]
    [TestCase(nameof(Bar.OtherReceiver))]
    public void MissingAmbiguousOrOtherReceiverPreparationKeepsTheNativeTargetUnmodified(string method)
    {
        var target = typeof(Bar).GetMethod(method)!;
        int handoffs = 0;
        var warnings = new List<string>();
        var patch = new MessageBarPreparedPatch(new(target, Entries, Frames), (_, _, _, _) => handoffs++, _ => { });
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(
                new("bar-prepared", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.False);
            var bar = new Bar();
            target.CreateDelegate<Action>(bar)();
            Assert.That(bar.Consumed, Has.Count.EqualTo(1));
            Assert.That(handoffs, Is.Zero);
            Assert.That(patch.Available, Is.False);
        }
        finally { patch.Candidate.Rollback(); }
    }

    [Test]
    public void MutableOuterPrefixCannotMakeHandoffReportThePrePrefixReceiver()
    {
        var foreign = new HarmonyLib.Harmony("tests.message-bar.prepared.receiver");
        object? observed = null;
        var patch = new MessageBarPreparedPatch(new(Target, Entries, Frames), (bar, _, _, _) => observed = bar, _ => { });
        var original = new Bar();
        Bar.Other = new Bar();
        try
        {
            Admit(patch);
            foreign.Patch(Target, prefix: new HarmonyMethod(typeof(MessageBarPreparedPatchTests), nameof(SubstituteReceiver)));
            Assert.That(patch.RefreshPublication(), Is.True);
            original.OnGUI();
            Assert.That(observed, Is.SameAs(Bar.Other));
            Assert.That(original.Consumed, Is.Empty);
            Assert.That(Bar.Other.Consumed, Is.EqualTo(new[] { 1, 2 }));
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.Prefix, foreign.Id); }
            finally { patch.Candidate.Rollback(); Bar.Other = new Bar(); }
        }
    }
    public static void SubstituteReceiver(ref Bar __instance) => __instance = Bar.Other;

    private static void Admit(MessageBarPreparedPatch patch)
    {
        var warnings = new List<string>();
        Assert.That(new AdmissionCoordinator(warnings.Add).Apply(
            new("bar-prepared", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True,
            string.Join("; ", warnings));
        patch.Enable();
        Assert.That(patch.EntryArgumentsAvailable, Is.False, "A handoff-only caller has not selected entry consumers.");
        Assert.That(patch.CaptureEntryArgumentsAuthority()(), Is.False);
    }
    public class Bar
    {
        private readonly List<int> entries = new() { 1, 2 };
        public readonly List<int> Consumed = new();
        public readonly List<bool> ConsumedFrames = new();
        public List<int> Entries { [MethodImpl(MethodImplOptions.NoInlining)] get => entries; }
        public bool ShowGroupFrames { [MethodImpl(MethodImplOptions.NoInlining)] get => Entries.Count > 1; }
        [MethodImpl(MethodImplOptions.NoInlining)] public void OnGUI()
        {
            Probe();
            var current = Entries;
            bool frames = ShowGroupFrames;
            for (int index = 0; index < current.Count; index++) Consume(current[index], frames);
        }
        [MethodImpl(MethodImplOptions.NoInlining)] private void Consume(int value, bool frames)
        { Consumed.Add(value); ConsumedFrames.Add(frames); }
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Probe() { observations++; }
        [MethodImpl(MethodImplOptions.NoInlining)] public void MissingPreparation() => Consume(1, false);
        [MethodImpl(MethodImplOptions.NoInlining)] public void DuplicateEntries()
        {
            var current = Entries;
            bool frames = ShowGroupFrames;
            var second = Entries;
            Consume(current[0] + second[0], frames);
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public void OtherReceiver()
        {
            var current = Other.Entries;
            bool frames = ShowGroupFrames;
            Consume(current[0], frames);
        }
        public static Bar Other = new();
    }
    public sealed class InheritedBar : Bar
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public void RenderInherited()
        {
            var current = Entries;
            bool frames = ShowGroupFrames;
            for (int index = 0; index < current.Count; index++)
            { Consumed.Add(current[index]); ConsumedFrames.Add(frames); }
        }
    }
}
