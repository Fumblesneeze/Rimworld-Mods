using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.ContentSelection.Harmony;

public sealed class IncidentPatchBridge
{
    private static readonly ConcurrentDictionary<MethodBase, IncidentPatchBridge> Routes = new();
    private readonly Type workerType;
    private readonly Type parmsType;
    private readonly Type defType;
    private readonly Func<object, bool> excluded;
    private readonly Action<object>? refusal;
    private readonly Action<string>? unavailable;
    private readonly Registration[] registrations;
    private FieldInfo? identity;
    private object compilation = new();
    private object? attemptedCompilation;
    private object? publication;
    private volatile bool enabled;
    private volatile bool faulted;
    private int failureReported;
    public string? Failure { get; private set; }

    public IncidentPatchBridge(Type workerType, Type parmsType, Type defType, Func<object, bool> excluded,
        Action<object>? refusal = null, Action<string>? unavailable = null)
    {
        this.workerType = workerType;
        this.parmsType = parmsType;
        this.defType = defType;
        this.excluded = excluded;
        this.refusal = refusal;
        this.unavailable = unavailable;
        registrations = new[]
        {
            new Registration("MM.CONTENT.IncidentEligibility", "CanFireNow", HarmonyPatchType.Postfix, Callback(nameof(VetoPostfix))),
            new Registration("MM.CONTENT.IncidentExecute", "TryExecute", HarmonyPatchType.Prefix, Callback(nameof(RefusePrefix))),
            new Registration("MM.CONTENT.IncidentExecuteResult", "TryExecute", HarmonyPatchType.Postfix, Callback(nameof(ExecuteVetoPostfix))),
            new Registration("MM.CONTENT.IncidentEligibilityGeneration", "CanFireNow", HarmonyPatchType.Transpiler, Callback(nameof(ObserveCompilation))),
            new Registration("MM.CONTENT.IncidentExecuteGeneration", "TryExecute", HarmonyPatchType.Transpiler, Callback(nameof(ObserveCompilation)))
        };
        Group = new PatchGroup("MM.CONTENT.Incidents", AdmissionPhase.DefsReady,
            registrations.Select(registration => new PatchCandidate(registration.Id, registration.Id,
                workerType.FullName + "." + registration.MethodName, "Incident exclusions", "Native incident behavior",
                () => Validate(registration), () => Install(registration), () => Rollback(registration))).ToArray());
    }
    public PatchGroup Group { get; }
    public bool Available => !faulted && ReferenceEquals(Volatile.Read(ref compilation), Volatile.Read(ref publication));
    // A rejected generation waits for a later compilation, rather than rescanning every frame.
    public bool NeedsPublicationRefresh => !faulted && registrations.All(registration => registration.Installed) &&
        !ReferenceEquals(Volatile.Read(ref compilation), Volatile.Read(ref attemptedCompilation));
    // Composition owner only, outside foreign locks and incident callbacks.
    public bool RefreshPublication()
    {
        Volatile.Write(ref publication, null);
        object generation = Volatile.Read(ref compilation);
        if (faulted || registrations.Any(registration => !registration.Installed)) return false;
        Volatile.Write(ref attemptedCompilation, generation);
        Failure = null;
        foreach (var registration in registrations)
        {
            if (IncidentPatchOrder.HasFactory(registration.Target!))
            {
                Failure = "Incident patch factories prevent safe metadata inspection.";
                return false;
            }
            string? failure = registration.Kind switch
            {
                HarmonyPatchType.Prefix => IncidentPatchOrder.ValidatePrefix(registration.Target!, registration.Callback),
                HarmonyPatchType.Postfix => IncidentPatchOrder.ValidatePostfix(registration.Target!, registration.Callback),
                _ => HasOwnedGeneration(registration) ? null : "The owned compilation observer is absent or duplicated."
            };
            if (failure != null) { Failure = registration.Id + ": " + failure; return false; }
        }
        if (!ReferenceEquals(generation, Volatile.Read(ref compilation))) return false;
        Volatile.Write(ref publication, generation);
        return Available;
    }

    private static bool HasOwnedGeneration(Registration registration)
    {
        var patches = HarmonyLib.Harmony.GetPatchInfo(registration.Target!)?.Transpilers.ToArray() ?? Array.Empty<Patch>();
        return patches.Count(patch => patch.owner == OwnedHarmonyPatch.OwnerId) == 1 &&
            patches.Count(patch => patch.PatchMethod == registration.Callback) == 1 &&
            patches.Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId && patch.PatchMethod == registration.Callback);
    }

    private static IEnumerable<CodeInstruction> ObserveCompilation(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        if (Routes.TryGetValue(__originalMethod, out var route))
        {
            Volatile.Write(ref route.publication, null);
            Volatile.Write(ref route.compilation, new object());
        }
        return instructions;
    }

    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("Incident patch group is unavailable.");
        enabled = true;
    }
    public void Disable() => enabled = false;

    private void Fault(Exception exception)
    {
        enabled = false;
        faulted = true;
        Volatile.Write(ref publication, null);
        string reason = exception.GetBaseException().Message;
        Failure = new string(reason.Take(256).Select(character => char.IsControl(character) ? ' ' : character).ToArray());
        if (Interlocked.Exchange(ref failureReported, 1) != 0) return;
        try { unavailable?.Invoke(Failure); }
        catch (Exception) { /* Failure reporting cannot replace the native outcome. */ }
    }

    private string? Validate(Registration registration)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        if (workerType.ContainsGenericParameters || workerType.IsGenericType) return "Generic worker types are unsupported.";
        identity = workerType.GetField("def", flags);
        registration.Target = workerType.GetMethod(registration.MethodName, flags, null, new[] { parmsType }, null);
        if (identity == null || identity.FieldType != defType || registration.Target == null ||
            registration.Target.ReturnType != typeof(bool) || registration.Target.IsGenericMethod)
            return "The native incident method or Def identity field shape changed.";
        if (IncidentPatchOrder.HasFactory(registration.Target)) return "Incident patch factories prevent safe admission.";
        registration.Receipt ??= new OwnedHarmonyPatch(registration.Target, registration.Kind, registration.Callback);
        return registration.Receipt.ValidateOwnership();
    }

    private void Install(Registration registration)
    {
        if (registration.Target == null || registration.Receipt == null) throw new InvalidOperationException("Incident target was not validated.");
        if (IncidentPatchOrder.HasFactory(registration.Target)) throw new InvalidOperationException("Incident patch factories prevent safe admission.");
        if (!Routes.TryAdd(registration.Target, this) && !ReferenceEquals(Routes[registration.Target], this))
            throw new InvalidOperationException("Incident target already has an owned bridge.");
        registration.Receipt.Install();
        registration.Installed = true;
        if (registrations.All(member => member.Installed) && !RefreshPublication())
            throw new InvalidOperationException(Failure ?? "Incident patch metadata changed during admission.");
    }

    private void Rollback(Registration registration)
    {
        enabled = false;
        Volatile.Write(ref publication, null);
        registration.Installed = false;
        try { registration.Receipt?.Rollback(); }
        finally
        {
            if (registration.Target != null && Routes.TryGetValue(registration.Target, out var owner) && ReferenceEquals(owner, this))
                Routes.TryRemove(registration.Target, out _);
        }
    }

    [HarmonyPriority(int.MinValue)]
    private static bool RefusePrefix(object __instance, MethodBase __originalMethod, ref bool __result, out bool __state)
    {
        __state = false;
        if (!Routes.TryGetValue(__originalMethod, out var route) || !route.enabled || !route.Available) return true;
        object? def;
        try
        {
            def = route.identity!.GetValue(__instance);
            if (def == null || !route.excluded(def)) return true;
        }
        catch (Exception exception) { route.Fault(exception); return true; }
        __result = false;
        __state = true;
        try { route.refusal?.Invoke(def); }
        catch (Exception exception) { route.Fault(exception); }
        return false;
    }

    [HarmonyPriority(int.MinValue)]
    private static void ExecuteVetoPostfix(object __instance, MethodBase __originalMethod, ref bool __result, bool __state)
    {
        // This invocation already refused before a notification failure invalidated later calls.
        if (__state) { __result = false; return; }
        VetoPostfix(__instance, __originalMethod, ref __result);
    }

    [HarmonyPriority(int.MinValue)]
    private static void VetoPostfix(object __instance, MethodBase __originalMethod, ref bool __result)
    {
        if (!__result || !Routes.TryGetValue(__originalMethod, out var route) || !route.enabled || !route.Available) return;
        try
        {
            object? def = route.identity!.GetValue(__instance);
            if (def != null && route.excluded(def)) __result = false;
        }
        catch (Exception exception) { route.Fault(exception); }
    }

    private static MethodInfo Callback(string name) => typeof(IncidentPatchBridge).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;

    private sealed class Registration
    {
        public Registration(string id, string methodName, HarmonyPatchType kind, MethodInfo callback)
        {
            Id = id;
            MethodName = methodName;
            Kind = kind;
            Callback = callback;
        }
        public string Id { get; }
        public string MethodName { get; }
        public HarmonyPatchType Kind { get; }
        public MethodInfo Callback { get; }
        public MethodInfo? Target;
        public OwnedHarmonyPatch? Receipt;
        public bool Installed;
    }
}
