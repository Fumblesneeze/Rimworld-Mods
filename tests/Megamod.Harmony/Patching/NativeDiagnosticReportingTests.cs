using Megamod.Patching;
using Megamod.Features.Diagnostics;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class NativeDiagnosticReportingTests
{
    [Test]
    public void AutomaticOffKeepsNormalWarningStatusWhileRetainingTheRequest()
    {
        var store = new SettingsStore(new SettingsEffectiveOff(new Dictionary<SettingDefinition<bool>, string>
        { [WarningCaptureSettings.MuteWarnings] = "Megamod_Settings_OriginalProviderOff" }));
        WarningCaptureSettings.Register(store);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, true);
        var capture = new WarningCaptureRuntime(store);
        Assert.That(capture.StatusKey, Is.EqualTo("Megamod_Diagnostics_WarningCaptureOff"));
        Assert.That(capture.NeedsWarningChannel, Is.False);
        Assert.That(capture.AllowWarning("ordinary warning"), Is.True);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, false);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, true);
        Assert.That(capture.NeedsWarningChannel, Is.False);
        Assert.That(store.GetGlobal(WarningCaptureSettings.MuteWarnings), Is.True);
    }

    [Test]
    public void CaptureExitAvailabilityTracksRebuildsWhileContextAndSavedChoiceSurvive()
    {
        NativeDiagnosticReporting? reporting = null;
        var notices = new List<string>();
        var store = new SettingsStore();
        WarningCaptureSettings.Register(store);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, true);
        var capture = new WarningCaptureRuntime(store);
        var warning = new NativeLogPatch(typeof(NativeLogPatchTests.LockedWarningLog), "Warning",
            text => reporting!.ContextReady ? text + " [context]" : text, notices.Add, capture.AllowWarning);
        capture.Bind(warning);
        var error = new NativeLogPatch(typeof(NativeLogPatchTests.SampleLog), "Error", text => text);
        var foreign = new HarmonyLib.Harmony("tests.megamod.reporting.capture-rebuild");
        var target = typeof(NativeLogPatchTests.LockedWarningLog).GetMethod("Warning")!;
        var epilogue = new HarmonyLib.HarmonyMethod(typeof(NativeLogPatchTests), nameof(NativeLogPatchTests.AddSuppressedEpilogue));
        try
        {
            NativeLogPatchTests.LockedWarningLog.Reset();
            foreign.Patch(target, transpiler: epilogue);
            reporting = new NativeDiagnosticReporting(new AdmissionCoordinator(notices.Add), warning, error);
            reporting.Admit(capture.NeedsWarningChannel, contextRequired: true);
            NativeLogPatchTests.LockedWarningLog.Warning("initial unavailable capture");
            Assert.That(warning.CaptureFailure, Does.Contain("suppressed exit"));

            foreign.Unpatch(target, HarmonyLib.HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(warning.CaptureAvailable, Is.False);
            NativeLogPatchTests.LockedWarningLog.Warning("awaiting safe refresh");
            reporting.Refresh();
            Assert.That(warning.CaptureAvailable, Is.True);
            Assert.That(warning.CaptureFailure, Is.Null);
            Assert.That(capture.StatusKey, Is.EqualTo("Megamod_Diagnostics_WarningCaptureOn"));
            NativeLogPatchTests.LockedWarningLog.Warning("muted after repair");

            store.SetGlobal(WarningCaptureSettings.MuteWarnings, false);
            reporting.Admit(capture.NeedsWarningChannel, contextRequired: true);
            NativeLogPatchTests.LockedWarningLog.Warning("unmuted by player");
            store.SetGlobal(WarningCaptureSettings.MuteWarnings, true);
            reporting.Admit(capture.NeedsWarningChannel, contextRequired: true);
            foreign.Patch(target, transpiler: epilogue);
            reporting.Refresh();
            NativeLogPatchTests.LockedWarningLog.Warning("later unavailable capture");
            Assert.That(warning.CaptureAvailable, Is.False);
            Assert.That(reporting.ContextReady, Is.True);
            Assert.That(store.GetGlobal(WarningCaptureSettings.MuteWarnings), Is.True);
            Assert.That(notices, Has.Count.EqualTo(1));
            Assert.That(NativeLogPatchTests.LockedWarningLog.Captured, Is.EqualTo(new[] {
                "initial unavailable capture [context]", "awaiting safe refresh", "unmuted by player [context]", "later unavailable capture [context]" }));
        }
        finally
        {
            try { warning.Candidate.Rollback(); error.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyLib.HarmonyPatchType.Transpiler, foreign.Id); NativeLogPatchTests.LockedWarningLog.Reset(); }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SharedCaptureCapabilityPreservesContextWhileMuteIsManuallyOrAutomaticallyOffAndAfterEdits(bool automaticOff)
    {
        NativeDiagnosticReporting? reporting = null;
        var notices = new List<string>();
        var store = new SettingsStore(automaticOff ? new SettingsEffectiveOff(new Dictionary<SettingDefinition<bool>, string>
        { [WarningCaptureSettings.MuteWarnings] = "original-active" }) : null);
        WarningCaptureSettings.Register(store);
        store.SetGlobal(WarningCaptureSettings.MuteWarnings, automaticOff);
        var capture = new WarningCaptureRuntime(store);
        var warning = new NativeLogPatch(typeof(NativeLogPatchTests.LockedWarningLog), "Warning",
            text => reporting!.ContextReady ? text + " [context]" : text, notices.Add, capture.AllowWarning);
        capture.Bind(warning);
        var error = new NativeLogPatch(typeof(NativeLogPatchTests.SampleLog), "Error", text => text);
        var foreign = new HarmonyLib.Harmony("tests.megamod.reporting.capture-exit");
        var target = typeof(NativeLogPatchTests.LockedWarningLog).GetMethod("Warning")!;
        try
        {
            NativeLogPatchTests.LockedWarningLog.Reset();
            foreign.Patch(target, transpiler: new HarmonyLib.HarmonyMethod(typeof(NativeLogPatchTests), nameof(NativeLogPatchTests.AddSuppressedEpilogue)));
            reporting = new NativeDiagnosticReporting(new AdmissionCoordinator(notices.Add), warning, error);
            reporting.Admit(capture.NeedsWarningChannel, contextRequired: true);
            Assert.That(reporting.ContextReady, Is.True, "An effectively off mute capability cannot poison shared S094 admission.");
            Assert.That(warning.CaptureAvailable, Is.False);
            NativeLogPatchTests.LockedWarningLog.Warning("before edits");
            Assert.That(notices, Is.Empty);

            store.SetGlobal(WarningCaptureSettings.MuteWarnings, false);
            reporting.Admit(capture.NeedsWarningChannel, contextRequired: true);
            store.SetGlobal(WarningCaptureSettings.MuteWarnings, true);
            reporting.Admit(capture.NeedsWarningChannel, contextRequired: true);
            reporting.Admit(capture.NeedsWarningChannel, contextRequired: true);
            NativeLogPatchTests.LockedWarningLog.Warning("after edits");

            Assert.That(reporting.ContextReady, Is.True);
            Assert.That(NativeLogPatchTests.LockedWarningLog.Captured, Is.EqualTo(new[] { "before edits [context]", "after edits [context]" }));
            Assert.That(store.GetGlobal(WarningCaptureSettings.MuteWarnings), Is.True);
            Assert.That(capture.StatusKey, Is.EqualTo(automaticOff ? "Megamod_Diagnostics_WarningCaptureOff" : "Megamod_Diagnostics_WarningCaptureUnavailable"));
            Assert.That(notices, Has.Count.EqualTo(automaticOff ? 0 : 1));
            if (!automaticOff)
                Assert.That(notices[0], Does.Contain("suppressed exit").And.Contain("LOG-CAPTURE-WARNING")
                    .And.Contain("target=").And.Contain("unavailable=warning muting").And.Contain("definition context remains"));
        }
        finally
        {
            try { warning.Candidate.Rollback(); error.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyLib.HarmonyPatchType.Transpiler, foreign.Id); NativeLogPatchTests.LockedWarningLog.Reset(); }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void WarningOnlyAndContextOwnersRecoverBenignRebuildOnlyAtSafeRefresh(bool withContext)
    {
        NativeDiagnosticReporting? reporting = null;
        var warning = new NativeLogPatch(typeof(NativeLogPatchTests.SampleLog), "Warning",
            text => reporting!.ContextReady ? text + " [context]" : text, capture: _ => false);
        var error = new NativeLogPatch(typeof(NativeLogPatchTests.SampleLog), "Error", text => text);
        var foreign = new HarmonyLib.Harmony("tests.megamod.reporting.refresh");
        var target = typeof(NativeLogPatchTests.SampleLog).GetMethod("Warning")!;
        try
        {
            NativeLogPatchTests.SampleLog.Reset();
            reporting = new NativeDiagnosticReporting(new AdmissionCoordinator(_ => { }), warning, error);
            reporting.Refresh();
            Assert.That(warning.CaptureAvailable, Is.False);
            reporting.Admit(warningRequired: true, contextRequired: withContext);
            Assert.That(reporting.ContextReady, Is.EqualTo(withContext));
            Assert.That(error.Available, Is.EqualTo(withContext));
            foreign.Patch(target, transpiler: new HarmonyLib.HarmonyMethod(typeof(NativeDiagnosticReportingTests), nameof(Benign)));
            NativeLogPatchTests.SampleLog.Warning("awaiting refresh");
            Assert.That(warning.CaptureAvailable, Is.False);
            reporting.Refresh();
            NativeLogPatchTests.SampleLog.Warning("muted again");
            Assert.That(warning.CaptureAvailable, Is.True);
            Assert.That(reporting.ContextReady, Is.EqualTo(withContext));
            Assert.That(NativeLogPatchTests.SampleLog.Captured, Is.EqualTo(new[] { "awaiting refresh" }));
        }
        finally
        {
            try { warning.Candidate.Rollback(); error.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyLib.HarmonyPatchType.Transpiler, foreign.Id); NativeLogPatchTests.SampleLog.Reset(); }
        }
    }

    public static IEnumerable<HarmonyLib.CodeInstruction> Benign(IEnumerable<HarmonyLib.CodeInstruction> incoming) => incoming;

    [Test]
    public void ErrorRejectionCannotDisableAdmittedWarningMuteOrPartiallyPublishContext()
    {
        NativeDiagnosticReporting? reporting = null;
        bool mute = true;
        var warning = new NativeLogPatch(typeof(NativeLogPatchTests.SampleLog), "Warning",
            text => reporting!.ContextReady ? text + " [context]" : text, capture: _ => !mute);
        var error = new NativeLogPatch(typeof(object), "Error", text => text);
        var notices = new List<string>();
        try
        {
            NativeLogPatchTests.SampleLog.Reset();
            reporting = new NativeDiagnosticReporting(new AdmissionCoordinator(notices.Add), warning, error);
            reporting.Admit(warningRequired: true, contextRequired: true);
            NativeLogPatchTests.SampleLog.Warning("muted");
            Assert.That(NativeLogPatchTests.SampleLog.Captured, Is.Empty);
            Assert.That(warning.CaptureAvailable, Is.True);
            Assert.That(reporting.ContextReady, Is.False);
            mute = false;
            NativeLogPatchTests.SampleLog.Warning("ordinary");
            Assert.That(NativeLogPatchTests.SampleLog.Captured, Is.EqualTo(new[] { "ordinary" }));
            Assert.That(notices, Has.Count.EqualTo(1));
            Assert.That(notices[0], Does.Contain("DDI-REPORT-ERROR"));
        }
        finally { warning.Candidate.Rollback(); error.Candidate.Rollback(); NativeLogPatchTests.SampleLog.Reset(); }
    }
}
