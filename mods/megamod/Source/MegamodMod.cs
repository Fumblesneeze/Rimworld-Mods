using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Megamod.Features.ContentSelection.Harmony;
using Megamod.Features.ContentSelection.Core;
using Megamod.Features.ContentSelection.Runtime;
using Megamod.Features.Diagnostics;
using Megamod.Features.DrugPolicyMaintenance;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugPolicyMaintenance.Harmony;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using Megamod.Features.ReadableNumbers;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using Megamod.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace Megamod;

public sealed class MegamodMod : Mod
{
    private static MegamodMod? initialOwner;
    private readonly SettingsRuntime settings;
    private readonly SettingsRenderer renderer;
    private readonly CurrentGameObserverPatch gameObserver;
    private readonly SettingsLifecyclePumpPatch settingsPump;
    private readonly ProtectedDiagnosticsRuntime diagnostics;
    private readonly WarningCaptureRuntime warningCapture;
    private readonly DefinitionDiagnosticsRuntime definitionDiagnostics;
    private readonly AdmissionCoordinator admission;
    private readonly string[] activePackageIds;
    private readonly bool drugPolicyEnabled;
    private MessagePlacementRuntime? messagePlacement;
    private readonly NativeContentInvalidationPatch definitionLifecycle = new(typeof(PlayDataLoader));
    private DrugPresetEntriesPatch? drugPresets;
    private PresetEntriesRead? drugBaseline;
    private bool initialDefinitionsAttempted;
    private IncidentContentRuntime? incidents;
    private bool contentInitializationAttempted;
    private bool incidentPatchesAdmitted;

    public MegamodMod(ModContentPack content) : base(content)
    {
        diagnostics = new ProtectedDiagnosticsRuntime(GenFilePaths.SaveDataFolderPath);
        admission = new AdmissionCoordinator(diagnostics.Write);
        activePackageIds = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageIdPlayerFacing.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal).ToArray();
        var originalDiagnostics = DiagnosticOriginalFeatures.FromActivePackages(activePackageIds);
        var originalContent = ContentProviderPolicy.FromActivePackages(activePackageIds);
        var originalDrugPolicy = DrugPolicyProviderPolicy.FromActivePackages(activePackageIds);
        var store = new SettingsStore(SettingsEffectiveOff.Combine(originalDiagnostics.EffectiveOff,
            originalContent.EffectiveOff, originalDrugPolicy.EffectiveOff,
            new SettingsEffectiveOff(MessagePlacementProviderPolicy.Reasons(activePackageIds))));
        ReadableNumbersSettings.Register(store);
        store.Register(ReadableNumbersSettings.Punctuation);
        DiagnosticSettings.Register(store);
        WarningCaptureSettings.Register(store);
        DrugPolicySettings.Register(store);
        MessagePlacementSettings.Register(store);
        settings = SettingsRuntime.Initialize(store, GetSettings<MegamodGlobalSettings>(), exception =>
            diagnostics.Write("[fumblesneeze.megamod] common-settings-lifecycle observation failed; " +
                "current-save settings are unavailable; global settings and stored overrides remain. " +
                exception.GetBaseException().GetType().Name));
        // Mod construction precedes XML conversion. Load global choices first so the
        // admitted process-lifetime owners observe the initial loader's real diagnostics.
        drugPolicyEnabled = DrugPolicySettings.CaptureStartupEnabled(store);
        foreach (var warning in originalDiagnostics.StartupWarnings.Concat(originalContent.StartupWarnings)
            .Concat(originalDrugPolicy.StartupWarnings).Concat(MessagePlacementProviderPolicy.StartupWarnings(activePackageIds)))
            diagnostics.Write(warning);
        warningCapture = new WarningCaptureRuntime(store);
        definitionDiagnostics = new DefinitionDiagnosticsRuntime(
            admission, store, diagnostics.Write, warningCapture);
        renderer = new SettingsRenderer(settings);
        ReadableNumbersSettingsUi.Register(renderer);
        ReadableNumbersSettingsUi.RegisterPunctuation(renderer);
        DiagnosticSettingsUi.Register(renderer);
        WarningCaptureSettingsUi.Register(renderer, warningCapture);
        DrugPolicySettingsUi.Register(renderer);
        MessagePlacementSettingsUi.Register(renderer);
        gameObserver = new CurrentGameObserverPatch(typeof(Current), typeof(Game),
            actual => settings.OnGameChanged((Game?)actual), settings.ReportLifecycleFailure);
        settingsPump = new SettingsLifecyclePumpPatch(typeof(LongEventHandler),
            ProcessPendingLifecycle, settings.ReportLifecycleFailure);
        if (admission.Apply(new PatchGroup("common-settings-lifecycle", AdmissionPhase.Early,
                new[] { gameObserver.Candidate, settingsPump.Candidate }), AdmissionPhase.Early))
        {
            settings.SetSaveLifecycleAvailable(true);
            gameObserver.Enable();
            settingsPump.Enable();
            gameObserver.ObserveCurrent();
        }
        // Only the untouched startup trigger may publish the first finalized generation.
        // Reconstructed mod instances do not establish a new post-reload generation.
        Interlocked.CompareExchange(ref initialOwner, this, null);
    }

    internal static void PublishInitialDefinitions() => initialOwner?.InitializeDefinitions();

    private void InitializeDefinitions()
    {
        if (initialDefinitionsAttempted) return;
        initialDefinitionsAttempted = true;
        if (!UnityData.IsInMainThread || !PlayDataLoader.Loaded)
        {
            diagnostics.Write("[fumblesneeze.megamod] Initial finalized definitions are unavailable; " +
                "drug preset filtering and incident exclusions remain unavailable; native behavior remains.");
            return;
        }
        // One shared guard remains useful when either original feature provider is active.
        if (!admission.Apply(definitionLifecycle.Group, AdmissionPhase.DefsReady)) return;
        InitializeDrugPresets();
    }

    private void InitializeDrugPresets()
    {
        if (!drugPolicyEnabled) return;
        try
        {
            const BindingFlags instance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            MethodInfo Method(Type owner, string name, params Type[] arguments) =>
                owner.GetMethod(name, instance, null, arguments, null) ?? throw new MissingMethodException(owner.FullName, name);
            FieldInfo Field(Type owner, string name) =>
                owner.GetField(name, instance) ?? throw new MissingFieldException(owner.FullName, name);
            var patch = new DrugPresetEntriesPatch(
                Method(typeof(DrugPolicyDatabase), nameof(DrugPolicyDatabase.NewDrugPolicyFromDef), typeof(DrugPolicyDef)),
                Method(typeof(DrugPolicyDatabase), nameof(DrugPolicyDatabase.MakeNewDrugPolicy)),
                Field(typeof(DrugPolicyDef), nameof(DrugPolicyDef.entries)),
                Field(typeof(DrugPolicyEntry), nameof(DrugPolicyEntry.drug)),
                Method(typeof(DrugPolicy), "get_Item", typeof(ThingDef)),
                Method(typeof(DrugPolicyEntry), nameof(DrugPolicyEntry.CopyFrom), typeof(DrugPolicyEntry)),
                () => Volatile.Read(ref drugBaseline), diagnostics.Write);
            drugPresets = patch;
            var group = new PatchGroup("drug-policy.preset-entries", AdmissionPhase.DefsReady, new[] { patch.Candidate });
            if (!admission.Apply(group, AdmissionPhase.DefsReady)) return;
            bool IsCurrent() => definitionLifecycle.Available && definitionLifecycle.InvalidatedBy == null && patch.Available;
            // No save has authority here. Capture the complete native content baseline on the
            // main thread, then let worker factories read only detached membership and receipts.
            var baseline = NativeDrugPresetBaseline.Capture(IsCurrent);
            if (!baseline.IsCurrent) throw new InvalidOperationException("Initial drug baseline admission expired before publication.");
            Volatile.Write(ref drugBaseline, baseline);
            patch.Enable();
        }
        catch (Exception exception)
        {
            Volatile.Write(ref drugBaseline, null);
            drugPresets?.Disable();
            diagnostics.Write("[fumblesneeze.megamod] Patch MM.DRUG.PresetEntries unavailable; " +
                "target=RimWorld.DrugPolicyDatabase.NewDrugPolicyFromDef; " +
                "targetSpec=MM.DRUG.PresetEntries; unavailable=Unavailable-drug preset entries; fallback=Native preset behavior; " +
                "reason=" + exception.GetBaseException().Message);
        }
    }

    private void ProcessPendingLifecycle()
    {
        settings.ProcessPendingLifecycle();
        if (!UnityData.IsInMainThread) return;
        if (messagePlacement == null)
        {
            messagePlacement = new MessagePlacementRuntime(settings, activePackageIds, diagnostics.Write);
            admission.Apply(messagePlacement.Group, AdmissionPhase.Early);
        }
        // Also maintain the independent warning route when definition context or content
        // selection is inactive. Never inspect Harmony metadata from a log callback.
        definitionDiagnostics.RefreshReporting();
        if (drugPresets is { NeedsPublicationRefresh: true } preset && definitionLifecycle.Available &&
            definitionLifecycle.InvalidatedBy == null && Volatile.Read(ref drugBaseline) != null &&
            !preset.RefreshPublication())
            diagnostics.Write("[fumblesneeze.megamod] Patch MM.DRUG.PresetEntries unavailable after recomposition; " +
                "target=RimWorld.DrugPolicyDatabase.NewDrugPolicyFromDef; targetSpec=MM.DRUG.PresetEntries; " +
                "unavailable=Unavailable-drug preset entries; fallback=Native preset behavior; reason=" +
                (preset.Failure ?? "The current preset compilation could not be admitted."));
        if (!contentInitializationAttempted && PlayDataLoader.Loaded && !LongEventHandler.AnyEventNowOrWaiting)
        {
            // Initial finalized definitions only. A later idle transition is not proof of a
            // successful hot reload, and must not re-register settings or consume pending choices.
            contentInitializationAttempted = true;
            try
            {
                if (!definitionLifecycle.Available || definitionLifecycle.InvalidatedBy != null)
                    throw new InvalidOperationException("The initial shared Def lifecycle guard is unavailable.");
                var catalog = NativeContentCatalog.Capture().Catalog;
                var runtime = new IncidentContentRuntime(settings, catalog, activePackageIds, diagnostics.Write, definitionLifecycle);
                IncidentContentSettingsUi.Register(runtime, settings, renderer, catalog);
                incidentPatchesAdmitted = admission.Apply(runtime.Bridge.Group, AdmissionPhase.DefsReady);
                incidents = runtime;
            }
            catch (Exception exception)
            {
                // Content setup cannot take down the common settings pump or repeatedly retry
                // partially registered controls. Admission owns rollback of attempted patches.
                diagnostics.Write("[fumblesneeze.megamod] Content settings initialization failed; " +
                    "incident exclusions remain unavailable; native incidents and other settings remain. " +
                    exception.GetBaseException().GetType().Name);
            }
        }
        if (incidents == null || !incidentPatchesAdmitted) return;
        if (incidents.Bridge.NeedsPublicationRefresh && incidents.DefinitionLifecycle.InvalidatedBy == null &&
            !incidents.RefreshPatchPublication() && incidents.Bridge.Failure is { } reason)
            diagnostics.Write("[fumblesneeze.megamod] Incident content patches could not be revalidated; " +
                "incident exclusions remain unavailable and requested choices are retained; native incidents remain. " + reason);
        incidents.ProcessPendingLifecycle();
    }

    public override string SettingsCategory() => "Megamod_Name".Translate();

    public override void DoSettingsWindowContents(Rect inRect) => renderer.Draw(inRect);

    public override void WriteSettings()
    {
        settings.CaptureGlobals();
        base.WriteSettings();
    }
}
