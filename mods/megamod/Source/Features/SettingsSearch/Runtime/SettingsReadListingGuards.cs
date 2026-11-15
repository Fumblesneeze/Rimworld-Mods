using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Patching;
using UnityEngine;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

// Keeps only the two inspected native label-scroll collections detached during Label/End.
// No production reader callbacks are registered; the complete neutral reader owns initialization.
public sealed class SettingsReadListingGuards : IDisposable
{
    private const int MaximumCacheEntries = 65536;
    private static readonly CacheAccess? Access = CacheAccess.Resolve();
    private readonly Thread owner = Thread.CurrentThread;
    private readonly AdmissionCoordinator admission;
    private readonly List<SettingsReadControlGuard> guards = new();
    private readonly PatchGroup group;
    private bool initialized, disposed;
    private (MethodInfo Target, SettingsListingBoundsAdmission.Preservation Contract)? labelPreservation;

    internal SettingsListingBoundsAdmission.Preservation? ListingPreservationFor(MethodInfo method) =>
        labelPreservation is { } pair && pair.Target == method ? pair.Contract : null;

    public SettingsReadListingGuards(Action<string> warning)
    {
        admission = new AdmissionCoordinator(warning);
        var candidates = new List<PatchCandidate>();
        Bind(candidates, "SS-READ-LISTING-LABEL-CACHE", "Label", typeof(Rect), false, typeof(LabelCache),
            typeof(string), typeof(float), typeof(TipSignal?));
        Bind(candidates, "SS-READ-LISTING-END-CACHE", "End", typeof(void), true, typeof(EndCache));
        group = new PatchGroup("settings-search.read-listing-caches", AdmissionPhase.GameReady, candidates.ToArray());
    }

    public bool Initialize()
    {
        RequireOwner();
        if (disposed) return false;
        initialized = admission.Apply(group, AdmissionPhase.GameReady);
        return Available;
    }

    public bool Available
    {
        get
        {
            RequireOwner();
            if (!initialized || disposed || guards.Count != 2) return false;
            try { return guards.All(guard => guard.Available); }
            catch (Exception) { return false; }
        }
    }

    public void Dispose()
    {
        RequireOwner();
        if (disposed) return;
        disposed = true;
        initialized = false;
        List<Exception>? failures = null;
        for (int index = group.Patches.Count - 1; index >= 0; index--)
            try { group.Patches[index].Rollback(); }
            catch (Exception failure) { (failures ??= new()).Add(failure); }
        if (failures != null) throw new AggregateException("Settings-read listing guard cleanup failed.", failures);
    }

    private void Bind(List<PatchCandidate> candidates, string id, string name, Type result,
        bool isVirtual, Type callbacks, params Type[] arguments)
    {
        SettingsReadControlGuard? guard = null;
        try
        {
            var target = typeof(Listing_Standard).GetMethod(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, arguments, null);
            if (target != null && target.DeclaringType == typeof(Listing_Standard) && target.IsPublic &&
                !target.IsStatic && target.IsVirtual == isVirtual && !target.IsGenericMethod &&
                !target.ContainsGenericParameters && target.ReturnType == result &&
                target.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(arguments) &&
                Access != null)
            {
                var prefix = callbacks.GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
                var finalizer = callbacks.GetMethod("Finalizer", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
                guard = new SettingsReadControlGuard(target, id, prefix, finalizer);
            }
        }
        catch (Exception) { /* The ordinary rejected C01 candidate below owns shape failure. */ }

        if (guard == null)
        {
            candidates.Add(new PatchCandidate(id, "SS-READ-LISTING-CACHE", "Verse.Listing_Standard." + name,
                "automatic settings discovery", "ordinary settings pages",
                () => "The native listing callable or label-scroll field shape is unavailable.", () => { }, () => { }));
            return;
        }
        guards.Add(guard);
        if (callbacks == typeof(LabelCache))
            labelPreservation = (typeof(Listing_Standard).GetMethod(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, arguments, null)!,
                new(callbacks.GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!,
                    callbacks.GetMethod("Finalizer", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!,
                    () => initialized && !disposed && guard.Available));
        for (int index = 0; index < guard.Candidates.Count; index++)
        {
            var candidate = guard.Candidates[index] with { TargetSpecId = "SS-READ-LISTING-CACHE" };
            candidates.Add(index == guard.Candidates.Count - 1
                ? candidate with { Install = () => { candidate.Install(); guard.Enable(); } }
                : candidate);
        }
    }

    private void RequireOwner()
    {
        if (!ReferenceEquals(Thread.CurrentThread, owner))
            throw new InvalidOperationException("Settings-read listing guards require their initializing thread.");
    }

    // Capture both original references and both detached copies before assigning either native field.
    // Per-call isolation also protects nested/repeated native calls without retaining listing objects.
    private sealed class CacheState
    {
        private readonly Listing_Standard instance;
        internal readonly List<Pair<Vector2, Vector2>>? Positions;
        internal readonly List<Vector2>? Touched;
        internal readonly List<Pair<Vector2, Vector2>>? DetachedPositions;
        internal readonly List<Vector2>? DetachedTouched;

        internal CacheState(Listing_Standard instance)
        {
            this.instance = instance;
            var positions = Access!.ReadPositions(instance);
            var touched = Access.ReadTouched(instance);
            long count = (long)(positions?.Count ?? 0) + (touched?.Count ?? 0);
            if (count > MaximumCacheEntries)
                throw SettingsReadScope.Current!.Refuse("settings-read-listing-cache-limit");
            Positions = positions;
            Touched = touched;
            DetachedPositions = Copy(positions);
            DetachedTouched = Copy(touched);
        }

        internal void Isolate()
        {
            Access!.WritePositions(instance, DetachedPositions);
            Access.WriteTouched(instance, DetachedTouched);
        }

        internal void Restore()
        {
            // Always restore the acquired receiver, even if foreign code rewrites its instance arg.
            try { Access!.WritePositions(instance, Positions); }
            finally { Access!.WriteTouched(instance, Touched); }
        }

        // Direct List members avoid calling an unfamiliar IEnumerable implementation on a subclass.
        private static List<T>? Copy<T>(List<T>? original)
        {
            if (original == null) return null;
            var copy = new List<T>(original.Count);
            for (int index = 0; index < original.Count; index++) copy.Add(original[index]);
            return copy;
        }
    }

    private static void Enter(MethodBase target, Listing_Standard instance, out CacheState? state)
    {
        state = null;
        if (!SettingsReadControlGuard.ShouldCapture(target)) return;
        // The acquired state is published before either swap, so finalization owns partial entry.
        try { state = new CacheState(instance); }
        catch
        {
            SettingsReadScope.Current!.Refuse("settings-read-listing-cache-capture-failed");
            throw;
        }
        state.Isolate();
    }

    private static void Exit(ref CacheState? state)
    {
        var acquired = state;
        if (acquired == null) return;
        state = null;
        acquired.Restore();
    }

    private sealed class CacheAccess
    {
        internal readonly Func<Listing_Standard, List<Pair<Vector2, Vector2>>?> ReadPositions;
        internal readonly Action<Listing_Standard, List<Pair<Vector2, Vector2>>?> WritePositions;
        internal readonly Func<Listing_Standard, List<Vector2>?> ReadTouched;
        internal readonly Action<Listing_Standard, List<Vector2>?> WriteTouched;

        private CacheAccess()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<Pair<Vector2, Vector2>>() ||
                RuntimeHelpers.IsReferenceOrContainsReferences<Vector2>())
                throw new InvalidOperationException("Native listing cache values contain references.");
            var positions = Field("labelScrollbarPositions", typeof(List<Pair<Vector2, Vector2>>));
            var touched = Field("labelScrollbarPositionsSetThisFrame", typeof(List<Vector2>));
            ReadPositions = Reader<List<Pair<Vector2, Vector2>>?>(positions);
            WritePositions = Writer<List<Pair<Vector2, Vector2>>?>(positions);
            ReadTouched = Reader<List<Vector2>?>(touched);
            WriteTouched = Writer<List<Vector2>?>(touched);
        }

        internal static CacheAccess? Resolve()
        {
            try { return new CacheAccess(); }
            catch (Exception) { return null; }
        }

        private static FieldInfo Field(string name, Type type)
        {
            var field = typeof(Listing_Standard).GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (field == null || field.DeclaringType != typeof(Listing_Standard) || !field.IsPrivate ||
                field.IsStatic || field.IsInitOnly || field.IsLiteral || field.FieldType != type)
                throw new InvalidOperationException("Native listing cache field shape is unavailable.");
            return field;
        }

        private static Func<Listing_Standard, T> Reader<T>(FieldInfo field)
        {
            var instance = Expression.Parameter(typeof(Listing_Standard), "instance");
            return Expression.Lambda<Func<Listing_Standard, T>>(Expression.Field(instance, field), instance).Compile();
        }

        private static Action<Listing_Standard, T> Writer<T>(FieldInfo field)
        {
            var instance = Expression.Parameter(typeof(Listing_Standard), "instance");
            var value = Expression.Parameter(typeof(T), "value");
            return Expression.Lambda<Action<Listing_Standard, T>>(
                Expression.Assign(Expression.Field(instance, field), value), instance, value).Compile();
        }
    }

    // Separate declaring types give each paired target an independent Harmony __state identity.
    private static class LabelCache
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(MethodBase __originalMethod, Listing_Standard __instance, out CacheState? __state)
            => Enter(__originalMethod, __instance, out __state);

        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref CacheState? __state) => Exit(ref __state);
    }

    private static class EndCache
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(MethodBase __originalMethod, Listing_Standard __instance, out CacheState? __state)
            => Enter(__originalMethod, __instance, out __state);

        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref CacheState? __state) => Exit(ref __state);
    }
}
