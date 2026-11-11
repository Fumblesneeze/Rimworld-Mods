using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class ProtectedNativeWarningTests
{
    [Test]
    public void OwnedWarningMuteCannotSuppressProtectedCategoryOutput()
    {
        var patch = new NativeLogPatch(typeof(NativeLog), "Warning", text => text + " [context]", capture: _ => false);
        try
        {
            NativeLog.Records.Clear();
            NativeLog.UiEffects = 0;
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("warning", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            NativeLog.Warning("muted");
            var output = new ProtectedNativeWarning(typeof(NativeLog), typeof(NativeFile));
            Assert.That(output.TryWrite("required startup warning"), Is.True);
            Assert.That(NativeLog.Records, Is.EqualTo(new[] { "required startup warning" }));
            Assert.That(NativeLog.UiEffects, Is.Zero);
        }
        finally { patch.Candidate.Rollback(); NativeLog.Records.Clear(); }
    }
    [Test]
    public void UnknownBackendShapeIsUnavailableWithoutCallingTheCategory()
    {
        NativeLog.Records.Clear();
        var output = new ProtectedNativeWarning(typeof(NativeLog), typeof(object));
        Assert.That(output.UnavailableReason, Does.Contain("file availability"));
        Assert.That(output.TryWrite("must use fallback"), Is.False);
        Assert.That(NativeLog.Records, Is.Empty);
    }
    [TestCase("disabled")]
    [TestCase("unstarted")]
    [TestCase("shutdown")]
    [TestCase("category-disabled")]
    [TestCase("failed-during-write")]
    public void UnavailableOrFailedBackendCannotAcknowledgeDurableNativeDelivery(string mode)
    {
        try
        {
            NativeLog.Records.Clear();
            NativeFile.Disabled = mode == "disabled";
            NativeFile.StartedRoot = mode == "unstarted" ? null : "fixture";
            NativeFile.SetShutdown(mode == "shutdown");
            Category.Enabled = mode != "category-disabled";
            Category.FailDuringWrite = mode == "failed-during-write";
            var output = new ProtectedNativeWarning(typeof(NativeLog), typeof(NativeFile));
            Assert.That(output.TryWrite("required warning"), Is.False);
            Assert.That(NativeLog.Records.Count, Is.EqualTo(mode == "failed-during-write" ? 1 : 0));
        }
        finally
        {
            NativeFile.Disabled = false;
            NativeFile.StartedRoot = "fixture";
            NativeFile.SetShutdown(false);
            Category.Enabled = true;
            Category.FailDuringWrite = false;
            NativeLog.Records.Clear();
        }
    }
    [Test]
    public void ProtectedWarningReachesNativeCategoryWhenOrdinaryEntryIsSuppressedWithoutOpeningUi()
    {
        var suppressor = new HarmonyLib.Harmony("tests.megamod.protected-warning");
        var target = typeof(NativeLog).GetMethod(nameof(NativeLog.Warning))!;
        try
        {
            NativeLog.Records.Clear();
            NativeLog.UiEffects = 0;
            suppressor.Patch(target, prefix: new HarmonyMethod(typeof(ProtectedNativeWarningTests), nameof(Skip)));
            var output = new ProtectedNativeWarning(typeof(NativeLog), typeof(NativeFile));
            NativeLog.Warning("suppressed ordinary warning");
            Assert.That(output.TryWrite("Patch P startup warning"), Is.True);
            Assert.That(NativeLog.Records, Is.EqualTo(new[] { "Patch P startup warning" }));
            Assert.That(NativeLog.UiEffects, Is.Zero);
        }
        finally { suppressor.Unpatch(target, HarmonyPatchType.Prefix, suppressor.Id); NativeLog.Records.Clear(); }
    }
    public static bool Skip() => false;
    public enum Level { Information, Warning, Error }
    public readonly struct Warning;
    public readonly ref struct LogText<T>(string text)
    {
        public string Text { get; } = text;
        public static implicit operator LogText<T>(string text) => new(text);
    }
    public sealed class Category
    {
        public static bool Enabled = true;
        public static bool FailDuringWrite;
        public bool IsEnabled(Level level) => Enabled;
        public void Warning(LogText<Warning> text, string member, string file, int line)
        {
            NativeLog.Records.Add(text.Text);
            if (FailDuringWrite) NativeFile.Disabled = true;
        }
    }
    public static class NativeLog
    {
        private static readonly Category logCategory = new();
        private static bool PreventLogging => false;
        public static readonly List<string> Records = new();
        public static int UiEffects;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warning(string message)
        {
            if (PreventLogging) return;
            logCategory.Warning(message, nameof(Warning), "fixture", 1);
            UiEffects++;
        }
    }
    public static class NativeFile
    {
        public static bool Disabled { get; set; }
        public static string? StartedRoot { get; set; } = "fixture";
        private static bool _shutdown;
        public static void SetShutdown(bool value) => _shutdown = value;
    }
}
