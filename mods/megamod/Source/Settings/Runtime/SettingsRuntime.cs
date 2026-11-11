using System.Threading;
using Verse;

namespace Megamod.Settings;

// Loading hooks publish requests only. The admitted main-thread pump owns mutable store access.
public sealed class SettingsRuntime
{
    private readonly MegamodGlobalSettings persistedGlobals;
    private readonly Action<Exception> reportFailure;
    private readonly SettingsLifecycleMailbox<Game> lifecycle = new();
    private readonly SettingsLifecycleEntry lifecycleEntry = new();
    private MegamodSaveSettings? boundComponent;
    private SaveSettingsBinding? binding;
    private GlobalSettingsReadSnapshot publishedGlobals;

    private SettingsRuntime(SettingsStore store, MegamodGlobalSettings persisted, Action<Exception>? reportFailure)
    {
        Store = store;
        persistedGlobals = persisted;
        this.reportFailure = reportFailure ?? (_ => { });
        Store.LoadGlobals(persisted.Snapshot);
        publishedGlobals = new(Store.ExportGlobals(), Store.Revision.Global);
        Store.Changed += CaptureChangedValues;
    }

    public static SettingsRuntime? Instance { get; private set; }
    public SettingsStore Store { get; }
    public GlobalSettingsReadSnapshot PublishedGlobals => Volatile.Read(ref publishedGlobals);
    public SettingsLifecycleRequest<Game> LifecycleReceipt => lifecycle.Read();
    public SaveSettingsBinding? CurrentBinding
    {
        get { ProcessPendingLifecycle(); return binding is { IsRevoked: false } ? binding : null; }
    }
    public bool SaveLifecycleAvailable => lifecycle.Read().Available;

    public static SettingsRuntime Initialize(SettingsStore store, MegamodGlobalSettings persisted,
        Action<Exception>? reportFailure = null)
    {
        if (Instance != null)
            throw new InvalidOperationException("The common settings runtime is already initialized.");
        return Instance = new SettingsRuntime(store, persisted, reportFailure);
    }

    public void CaptureGlobals()
    {
        RequireMainThread();
        var snapshot = Store.ExportGlobals();
        persistedGlobals.Capture(snapshot);
        if (PublishedGlobals.GlobalRevision != Store.Revision.Global)
            Volatile.Write(ref publishedGlobals, new(snapshot, Store.Revision.Global));
    }

    private void CaptureChangedValues()
    {
        RequireMainThread();
        if (PublishedGlobals.GlobalRevision != Store.Revision.Global) CaptureGlobals();
        if (boundComponent != null && binding is { IsRevoked: false })
            boundComponent.Capture(Store.ExportSave(binding));
    }

    // These entry points may run on the native loading worker. They never access Store.
    public void SetSaveLifecycleAvailable(bool available) => lifecycle.SetAvailable(available);
    public void ReportLifecycleFailure(Exception exception) => lifecycle.ReportFailure(exception, reportFailure);
    public void OnGameChanged(Game? actualGame) => lifecycle.Observe(actualGame);
    public void NotifyGameFinalized(Game owner, SaveSettingsSnapshot snapshot)
    {
        var request = lifecycle.Read();
        lifecycle.NotifyFinalized(owner, request.Generation, snapshot);
    }

    // Called by the admitted LongEventsUpdate postfix, before Root resumes ordinary UI work.
    public void ProcessPendingLifecycle()
    {
        RequireMainThread();
        lifecycleEntry.Run(ReconcilePendingLifecycle);
    }

    private void ReconcilePendingLifecycle()
    {
        bool readable = lifecycle.TryReconcileOwner(() => Current.Game, reportFailure, out var actual);
        if (binding != null && (binding.IsRevoked || !readable))
        {
            boundComponent?.Capture(Store.ExportSave(binding));
            binding = null;
            boundComponent = null;
            Store.DeactivateSave();
        }
        if (!readable || binding != null) return;
        bool finished = !LongEventHandler.AnyEventNowOrWaiting && Current.ProgramState == ProgramState.Playing;
        if (!lifecycle.TryGetReady(actual, finished, out var ready)) return;
        var component = ready.Owner!.GetComponent<MegamodSaveSettings>();
        if (component == null) return;
        var candidate = Store.ActivateSave(ready.Save!);
        if (!lifecycle.TryReadOwner(() => Current.Game, reportFailure, out var confirmed) ||
            !ReferenceEquals(confirmed, ready.Owner) || !lifecycle.TryBind(ready, candidate))
        {
            Store.DeactivateSave();
            return;
        }
        boundComponent = component;
        binding = candidate;
        component.Capture(Store.ExportSave(candidate));
    }

    public SettingEditResult SetUseGlobal<T>(SaveSettingsBinding token, SettingDefinition<T> definition, bool useGlobal)
    {
        ProcessPendingLifecycle();
        return Store.SetUseGlobal(token, definition, useGlobal);
    }

    public SettingEditResult SetSaveOverride<T>(SaveSettingsBinding token, SettingDefinition<T> definition, T value)
    {
        ProcessPendingLifecycle();
        return Store.SetSaveOverride(token, definition, value);
    }

    private static void RequireMainThread()
    {
        if (!UnityData.IsInMainThread)
            throw new InvalidOperationException("Mutable settings runtime access requires the native main thread.");
    }
}
