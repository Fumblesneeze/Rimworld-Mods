using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using Megamod.Patching;
using Megamod.Settings;
using Verse;

namespace Megamod.Features.Diagnostics;

// One process-lifetime owner. Construct after registering/loading common globals, before XML loading.
public sealed class DefinitionDiagnosticsRuntime
{
    private readonly AdmissionCoordinator admission;
    private readonly DiagnosticOptionsSource options;
    private readonly Action<string> protectedWarning;
    private readonly DefinitionContextScope scope = new();
    private readonly ThreadLocal<int> protectedDepth = new();
    private readonly NativeLogPatch warning;
    private readonly NativeLogPatch error;
    private readonly NativeDiagnosticReporting reporting;
    private readonly WarningCaptureRuntime? warningCapture;
    private readonly NativeDefinitionConversionPatch current;
    private readonly NativeDefinitionConversionPatch legacy;
    private readonly NativeDefinitionConversionGuard coverage;
    private readonly FieldInfo? nativeLogLock;
    private int captureFailureReported;
    private int initializationFailureReported;

    public DefinitionDiagnosticsRuntime(AdmissionCoordinator admission, SettingsStore store,
        Action<string> protectedWarning, WarningCaptureRuntime? warningCapture = null)
    {
        this.admission = admission;
        this.protectedWarning = protectedWarning;
        this.warningCapture = warningCapture;
        options = new DiagnosticOptionsSource(store);
        nativeLogLock = typeof(Log).GetField("logLock", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        warning = new NativeLogPatch(typeof(Log), nameof(Log.Warning), Project, WriteProtectedWarning,
            warningCapture == null ? null : warningCapture.AllowWarning);
        error = new NativeLogPatch(typeof(Log), nameof(Log.Error), Project, WriteProtectedWarning);
        reporting = new NativeDiagnosticReporting(admission, warning, error);
        warningCapture?.Bind(warning);
        MethodInfo resolve = typeof(XmlInheritance).GetMethod(nameof(XmlInheritance.GetResolvedNodeFor),
            BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(XmlNode) }, null)!;
        current = new NativeDefinitionConversionPatch(typeof(DirectXmlToObjectNew), nameof(DirectXmlToObjectNew.DefFromNodeNew),
            typeof(LoadableXmlAsset), typeof(Def), resolve,
            typeof(DirectXmlToObjectNew.ParseValueAndReturnDefDelegate).GetMethod("Invoke")!, Enter, SafeRefreshPhase, scope.InvalidateCurrent);
        legacy = new NativeDefinitionConversionPatch(typeof(DirectXmlLoader), nameof(DirectXmlLoader.DefFromNode),
            typeof(LoadableXmlAsset), typeof(Def), resolve,
            typeof(Func<XmlNode, bool, object>).GetMethod("Invoke")!, Enter, SafeRefreshPhase, scope.InvalidateCurrent);
        coverage = new NativeDefinitionConversionGuard(current, legacy, () => scope.HasActiveFrame, scope.InvalidateCurrent, SafeRefreshPhase);
        // Changed is raised only by the serialized settings owner, after the options subscriber.
        store.Changed += AdmitIfEnabled;
        AdmitIfEnabled();
    }

    private void AdmitIfEnabled()
    {
        try
        {
            bool contextRequired = options.Current.DefinitionContextEnabled;
            string? unavailable = nativeLogLock?.FieldType != typeof(object) || nativeLogLock.GetValue(null) == null
                ? "The native logging lock cannot be identified for safe context refresh." : null;
            reporting.Admit(warningCapture?.NeedsWarningChannel == true, contextRequired, unavailable);
            RefreshReporting();
            if (!contextRequired || !reporting.ContextReady) return;
            if (admission.Apply(new PatchGroup("diagnostics.definition-family", AdmissionPhase.Early,
                new[] { current.Candidate, legacy.Candidate }.Concat(coverage.Candidates).ToArray()), AdmissionPhase.Early))
            {
                current.Enable(); legacy.Enable(); coverage.Enable();
            }
        }
        catch (Exception exception)
        {
            if (Interlocked.Exchange(ref initializationFailureReported, 1) == 0)
                WriteProtectedWarning("[fumblesneeze.megamod] Definition-context initialization failed; ordinary native diagnostics remain. " + exception.GetType().Name);
        }
    }

    private bool SafeRefreshPhase() => nativeLogLock?.GetValue(null) is object nativeLock && !Monitor.IsEntered(nativeLock);

    // Main-thread maintenance or actual converter entry, always outside the inspected native log lock.
    // This also restores a benign rebuilt warning-only route while S094 is effectively off.
    public void RefreshReporting()
    {
        try { if (SafeRefreshPhase()) reporting.Refresh(); }
        catch (Exception exception)
        {
            if (Interlocked.Exchange(ref initializationFailureReported, 1) == 0)
                WriteProtectedWarning("[fumblesneeze.megamod] Diagnostic publication refresh unavailable; ordinary diagnostics remain. " + exception.GetType().Name);
        }
    }

    private IDisposable? Enter(XmlNode node, object? source)
    {
        if (!options.Current.DefinitionContextEnabled) return null;
        var remainsValid = coverage.CaptureValidity();
        if (remainsValid == null) return null;
        // These checks occur in the actual converter body, before its diagnostic scope or native Log lock.
        RefreshReporting();
        if (!reporting.ContextReady) return null;
        var asset = source as LoadableXmlAsset;
        var metadata = new DefinitionSource(Read(() => asset?.mod?.Name), Read(() => asset?.mod?.PackageIdPlayerFacing),
            Read(() => asset?.mod?.RootDir), Read(() => asset?.fullFolderPath), Read(() => asset?.name));
        return scope.Enter(DefinitionContextCapture.Read(node, metadata, CaptureFailure), remainsValid);
    }

    private string? Read(Func<string?> read)
    {
        try { return read(); }
        catch (Exception exception) { CaptureFailure(exception); return null; }
    }
    private void CaptureFailure(Exception exception)
    {
        if (Interlocked.Exchange(ref captureFailureReported, 1) == 0)
            WriteProtectedWarning("[fumblesneeze.megamod] DDI source metadata could not be read; unavailable fields remain unknown. " + exception.GetType().Name);
    }
    private string Project(string original)
    {
        if (protectedDepth.Value != 0 || !options.Current.DefinitionContextEnabled || !reporting.ContextReady) return original;
        var context = scope.Current;
        return context == null ? original : DefinitionContextFormatter.Append(original, context);
    }

    // The supplied sink must still write a durable warning if S035 later suppresses ordinary capture.
    public void WriteProtectedWarning(string message)
    {
        protectedDepth.Value++;
        try { protectedWarning(message); }
        catch (Exception) { /* Shared sink owns its bounded durable fallback. */ }
        finally { protectedDepth.Value--; }
    }
}
