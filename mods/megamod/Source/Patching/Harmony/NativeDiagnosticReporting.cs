namespace Megamod.Patching;

// One physical Warning route serves S035 and S094; S094 publishes only with both severities ready.
public sealed class NativeDiagnosticReporting
{
    private readonly AdmissionCoordinator admission;
    private readonly NativeLogPatch warning;
    private readonly NativeLogPatch error;
    private bool warningEnabled;
    private bool errorEnabled;
    private bool captureRequested;
    private volatile bool contextAdmitted;

    public NativeDiagnosticReporting(AdmissionCoordinator admission, NativeLogPatch warning, NativeLogPatch error)
    {
        this.admission = admission;
        this.warning = warning;
        this.error = error;
    }
    public bool ContextReady => contextAdmitted && warning.Available && error.Available;

    // Serialized composition owner only. No metadata operations occur in the log callback.
    public void Admit(bool warningRequired, bool contextRequired, string? contextUnavailable = null)
    {
        captureRequested = warningRequired;
        if (!warningRequired && !contextRequired) return;
        bool warningAdmitted = admission.Apply(new PatchGroup("diagnostics.warning-channel", AdmissionPhase.Early,
            new[] { warning.Candidate }), AdmissionPhase.Early);
        if (warningAdmitted && !warningEnabled) { warning.Enable(); warningEnabled = true; }
        warning.ReportCaptureUnavailable(captureRequested);
        if (!contextRequired) return;
        bool errorAdmitted = admission.Apply(new PatchGroup("diagnostics.definition-error", AdmissionPhase.Early,
            new[] { error.Candidate }), AdmissionPhase.Early,
            unavailableReason: contextUnavailable ?? (!warningAdmitted ? "The shared warning route is unavailable; complete definition context cannot be published." : null));
        if (!warningAdmitted || !errorAdmitted) return;
        if (!errorEnabled) { error.Enable(); errorEnabled = true; }
        contextAdmitted = true;
    }
    public void Refresh()
    {
        if (warningEnabled && !warning.Available) warning.RefreshPublication();
        if (errorEnabled && !error.Available) error.RefreshPublication();
        warning.ReportCaptureUnavailable(captureRequested);
    }
}
