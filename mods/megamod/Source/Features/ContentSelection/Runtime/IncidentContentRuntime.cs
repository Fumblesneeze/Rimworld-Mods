using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Megamod.Features.ContentSelection.Core;
using Megamod.Features.ContentSelection.Harmony;
using Megamod.Settings;
using RimWorld;
using Verse;

namespace Megamod.Features.ContentSelection.Runtime;

// Root constructs this after finalized Defs and calls the pump after the common settings pump.
// This is the finished-main-thread incident family, not the still-required early loading boundary.
public sealed class IncidentContentRuntime
{
    private readonly IncidentPolicyLifecycle<Game> lifecycle;
    private readonly Action<string> warning;
    private int failureReported;
    private int definitionFailureReported;
    private volatile bool active;

    public IncidentContentRuntime(SettingsRuntime settings, ContentCatalog catalog, IEnumerable<string> activePackageIds,
        Action<string> warning, NativeContentInvalidationPatch definitionLifecycle)
    {
        RequireMainThread();
        this.warning = warning ?? throw new ArgumentNullException(nameof(warning));
        Catalog = catalog;
        DefinitionLifecycle = definitionLifecycle ?? throw new ArgumentNullException(nameof(definitionLifecycle));
        Definitions = new ContentSettings(settings.Store);
        foreach (var entry in catalog.Entries) Definitions.Individual(entry.Key);
        lifecycle = new IncidentPolicyLifecycle<Game>(Definitions, new ContentApplication(settings.Store), catalog,
            catalog.Entries.Where(entry => entry.Identity is IncidentDef).Select(entry => entry.Key).ToArray(),
            () => settings.LifecycleReceipt, () => Current.Game);
        Bridge = new IncidentPatchBridge(typeof(IncidentWorker), typeof(IncidentParms), typeof(IncidentDef),
            identity => CurrentAppliedPolicy is { } policy && !policy.IsAllowed(identity),
            NotifyRefusal, ReportUnavailable);
    }

    public ContentSettings Definitions { get; }
    public ContentCatalog Catalog { get; }
    public IncidentPatchBridge Bridge { get; }
    public NativeContentInvalidationPatch DefinitionLifecycle { get; }
    // Retained immutable evidence for the future reconciliation boundary, never an availability claim.
    public AppliedContentPolicy? RetainedAppliedPolicy { get; private set; }
    public AppliedContentPolicy? CurrentAppliedPolicy
    {
        get
        {
            if (!active || !Bridge.Available || !DefinitionLifecycle.Available ||
                DefinitionLifecycle.InvalidatedBy != null) return null;
            var policy = lifecycle.ReadApplied();
            return active && Bridge.Available && DefinitionLifecycle.Available && DefinitionLifecycle.InvalidatedBy == null ? policy : null;
        }
    }
    public bool PolicyAvailable => CurrentAppliedPolicy != null;

    // Main-thread composition owner only. Revalidation never captures pending settings again.
    public bool RefreshPatchPublication()
    {
        RequireMainThread();
        active = false;
        Bridge.Disable();
        if (DefinitionLifecycle.InvalidatedBy != null) return false;
        try { return Bridge.RefreshPublication(); }
        catch (Exception exception) { ReportUnavailable(exception.GetBaseException().Message); return false; }
    }

    public void ProcessPendingLifecycle()
    {
        RequireMainThread();
        active = false;
        Bridge.Disable();
        try
        {
            if (DefinitionLifecycle.InvalidatedBy is { } entry)
            {
                if (Interlocked.Exchange(ref definitionFailureReported, 1) == 0)
                    Warn("[fumblesneeze.megamod] Incident policy unavailable after native " + entry +
                        "; requested choices and prior applied decisions are retained. Def generation reconciliation is not implemented.");
                return;
            }
            if (!DefinitionLifecycle.Available) return;
            bool ready = Current.ProgramState == ProgramState.Playing &&
                !LongEventHandler.AnyEventNowOrWaiting;
            if (lifecycle.Process(ready, Bridge.Available) && DefinitionLifecycle.Available && DefinitionLifecycle.InvalidatedBy == null)
            {
                if (lifecycle.ReadApplied() is { } applied) RetainedAppliedPolicy = applied;
                Bridge.Enable(); active = true;
            }
        }
        catch (Exception exception) { ReportUnavailable(exception.GetBaseException().Message); }
    }

    private static void NotifyRefusal(object identity)
    {
        if (UnityData.IsInMainThread && identity is IncidentDef definition)
            Messages.Message("Megamod_Content_IncidentExcluded".Translate(definition.LabelCap), MessageTypeDefOf.RejectInput);
    }

    private void ReportUnavailable(string reason)
    {
        active = false;
        if (Interlocked.Exchange(ref failureReported, 1) != 0) return;
        string bounded = new string(reason.Take(256).Select(character => char.IsControl(character) ? ' ' : character).ToArray());
        Warn("[fumblesneeze.megamod] Incident content policy unavailable; requested choices remain stored; " +
            "native incident behavior remains. " + bounded);
    }

    private void Warn(string message)
    {
        try { warning(message); }
        catch (Exception) { /* An unavailable feature cannot break the common lifecycle through diagnostics. */ }
    }

    private static void RequireMainThread()
    {
        if (!UnityData.IsInMainThread) throw new InvalidOperationException("Incident policy composition requires the native main thread.");
    }
}
