using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Patching;
using UnityEngine;

namespace Megamod.Features.SettingsSearch.Runtime;

// Acquired only by the complete neutral reader; no production callbacks are registered here.
public sealed class SettingsReadListingBoundsGuard : IDisposable
{
    private const string Spec = "SS-READ-LISTING-BOUNDS";
    private static SettingsReadListingBoundsGuard? active;
    private readonly Thread thread = Thread.CurrentThread;
    private readonly SettingsReadControlGuards controls;
    private readonly SettingsReadListingGuards caches;
    private readonly AdmissionCoordinator admission;
    private SettingsReadControlGuard? guard;
    private MethodInfo? getter;
    private SettingsListingBoundsAdmission.Certificate? certificate;
    private PatchGroup? group;
    private bool attempted, initialized, disposed;

    public SettingsReadListingBoundsGuard(SettingsReadControlGuards controls,
        SettingsReadListingGuards caches, Action<string> warning)
    {
        this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
        this.caches = caches ?? throw new ArgumentNullException(nameof(caches));
        admission = new AdmissionCoordinator(warning);
    }

    public bool Initialize()
    {
        RequireThread();
        if (disposed || attempted) return Available;
        attempted = true;
        var candidates = new List<PatchCandidate>();
        if (!SettingsListingBoundsTargets.TryBind(out var catalog, out var failure))
        {
            foreach (string suffix in new[] { ".prefix", ".finalizer" })
                candidates.Add(new(Spec + suffix, Spec, "Verse.Listing_Standard.get_BoundingRectCached",
                    "automatic settings discovery", "ordinary settings pages",
                    () => failure ?? "Native listing consumers are unavailable.", () => { }, () => { }));
        }
        else
        {
            getter = catalog!.Getter;
            var callbacks = typeof(BoundsCallbacks);
            guard = new SettingsReadControlGuard(catalog!.Getter, Spec,
                callbacks.GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static)!,
                callbacks.GetMethod("Finalizer", BindingFlags.NonPublic | BindingFlags.Static)!);
            for (int index = 0; index < guard.Candidates.Count; index++)
            {
                var candidate = guard.Candidates[index] with { TargetSpecId = Spec };
                if (index == 0)
                    candidate = candidate with { Validate = () => CaptureConsumers(catalog) ?? guard.Candidates[0].Validate() };
                if (index == guard.Candidates.Count - 1)
                {
                    var last = candidate;
                    candidate = candidate with
                    {
                        Install = () =>
                        {
                            last.Install();
                            if (active != null || certificate?.IsCurrent() != true)
                                throw new InvalidOperationException("Listing consumer prerequisites changed during acquisition.");
                            guard.Enable();
                            active = this;
                        },
                        Rollback = () =>
                        {
                            if (ReferenceEquals(active, this)) active = null;
                            last.Rollback();
                        }
                    };
                }
                candidates.Add(candidate);
            }
        }
        group = new PatchGroup("settings-search.read-listing-bounds", AdmissionPhase.GameReady, candidates.ToArray());
        initialized = admission.Apply(group, AdmissionPhase.GameReady);
        return Available;
    }

    private string? CaptureConsumers(SettingsListingBoundsTargets.Catalog catalog)
    {
        if (active != null || !controls.Available || !caches.Available)
            return "Native listing value/cache preservation is unavailable or another bounds reader owns the getter.";
        string? getterFailure = GetterExceptionFailure();
        if (getterFailure != null) return getterFailure;
        var consumers = catalog.Consumers.Select(consumer => new SettingsListingBoundsAdmission.Consumer(
            consumer.Method, consumer.Checker,
            controls.ListingPreservationFor(consumer.Method) ?? caches.ListingPreservationFor(consumer.Method))).ToArray();
        return SettingsListingBoundsAdmission.TryCapture(consumers, out certificate, out var failure)
            ? null : failure ?? "Native listing consumer composition is unproved.";
    }

    public bool Available
    {
        get
        {
            RequireThread();
            if (!initialized || disposed || !ReferenceEquals(active, this)) return false;
            try { return certificate?.IsCurrent() == true && guard?.Available == true &&
                GetterExceptionFailure() == null && controls.Available && caches.Available; }
            catch (Exception) { return false; }
        }
    }

    private string? GetterExceptionFailure()
    {
        if (getter == null) return "The listing bounds getter is unavailable.";
        string? failure = null;
        bool inspected = HarmonyPublication.Inspect(getter, snapshot =>
        {
            foreach (var callback in snapshot.Finalizers)
            {
                // A last finalizer cannot observe an exception that an earlier one cleared.
                // Allow void observers and result restoration, but require the exception itself
                // to survive every preceding finalizer. Attribute aliases have the same effect.
                if (callback.IsFactory || callback.UncheckedReferenceBinding || callback.Method.ReturnType != typeof(void) ||
                    callback.Method.GetParameters().Any(parameter =>
                        (parameter.ParameterType.IsByRef || parameter.IsOut) &&
                        HarmonyArgumentBinding.Resolve(getter, parameter).Name == "__exception"))
                {
                    failure = "A listing getter finalizer can suppress an unproved callback exception.";
                    return false;
                }
            }
            return true;
        });
        return inspected ? null : failure ?? "Listing getter exception metadata is busy or unavailable.";
    }

    public void Dispose()
    {
        RequireThread();
        if (disposed) return;
        disposed = true;
        initialized = false;
        if (ReferenceEquals(active, this)) active = null;
        List<Exception>? failures = null;
        if (group != null)
            for (int index = group.Patches.Count - 1; index >= 0; index--)
                try { group.Patches[index].Rollback(); }
                catch (Exception error) { (failures ??= new()).Add(error); }
        if (failures != null) throw new AggregateException("Settings-read listing bounds cleanup failed.", failures);
    }

    private void RequireThread()
    {
        if (!ReferenceEquals(Thread.CurrentThread, thread))
            throw new InvalidOperationException("Settings-read listing bounds require their initializing thread.");
    }

    private static class BoundsCallbacks
    {
        [HarmonyPriority(int.MaxValue)]
        private static bool Prefix(MethodBase __originalMethod, ref Rect? __result, out SettingsReadScope? __state)
        {
            __state = null;
            if (!SettingsReadControlGuard.ShouldCapture(__originalMethod)) return true;
            var scope = SettingsReadScope.Current!;
            if (active?.Available != true) throw scope.Refuse("settings-read-listing-bounds-unavailable");
            __state = scope;
            __result = null;
            return false;
        }

        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref Rect? __result, Exception? __exception, ref SettingsReadScope? __state)
        {
            var acquired = __state;
            if (acquired == null) return;
            __state = null;
            // The acquired call owns restoration even if a later callback changes availability.
            __result = null;
            if (__exception != null && ReferenceEquals(SettingsReadScope.Current, acquired))
                acquired.Refuse("settings-read-listing-bounds-callback-failed");
        }
    }
}
