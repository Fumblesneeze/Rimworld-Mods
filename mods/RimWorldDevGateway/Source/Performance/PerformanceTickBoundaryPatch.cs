using System.Reflection;
using System.Threading;
using Verse;

namespace RimWorldDevGateway.Performance;

internal static class PerformanceTickBoundaryGate
{
    private const int Disabled = -1;
    private static int targetTick = Disabled;

    internal static void Arm(int gameTick)
    {
        if (gameTick < 0) throw new ArgumentOutOfRangeException(nameof(gameTick));
        if (Interlocked.CompareExchange(ref targetTick, gameTick, Disabled) != Disabled)
            throw new InvalidOperationException("An exact performance tick boundary is already armed.");
    }

    internal static void Disarm() => Interlocked.Exchange(ref targetTick, Disabled);

    internal static bool ShouldRunTick(int currentGameTick, int armedTargetTick) =>
        armedTargetTick == Disabled || currentGameTick < armedTargetTick;

    // Harmony prefix. This does not manufacture game ticks: it only pauses and rejects the first
    // native batched DoSingleTick call that would exceed the declared measurement boundary.
    internal static bool Prefix()
    {
        var target = Volatile.Read(ref targetTick);
        var manager = Find.TickManager;
        if (manager is null || ShouldRunTick(manager.TicksGame, target)) return true;
        manager.Pause();
        return false;
    }
}

internal sealed class PerformanceTickBoundaryPatch : IDisposable
{
    public const string OwnerId = "fumblesneeze.rimworlddevgateway";
    private readonly Action uninstall;
    private readonly Func<bool> ownsExactPatch;
    private bool disposed;
    private int armedTarget = -1;

    private PerformanceTickBoundaryPatch(
        Action uninstall,
        Func<bool> ownsExactPatch)
    {
        this.uninstall = uninstall;
        this.ownsExactPatch = ownsExactPatch;
    }

    public static bool TryInstall(
        IReadOnlyList<Assembly> assemblies,
        ReflectionPerformanceHarmonyCatalog catalog,
        out PerformanceTickBoundaryPatch? lease,
        out string reason)
    {
        lease = null;
        if (assemblies is null || catalog is null)
        {
            reason = "The Harmony tick-boundary inputs are unavailable.";
            return false;
        }

        try
        {
            if (catalog.ResolveOwner(OwnerId).Count != 0)
                throw new InvalidOperationException(
                    $"Harmony owner '{OwnerId}' already has patches before performance setup.");
            var harmonyAssembly = assemblies.Single(item =>
                string.Equals(item.GetName().Name, "0Harmony", StringComparison.Ordinal));
            var harmonyType = ExactType(harmonyAssembly, "HarmonyLib.Harmony");
            var harmonyMethodType = ExactType(harmonyAssembly, "HarmonyLib.HarmonyMethod");
            var harmonyConstructor = harmonyType.GetConstructor(new[] { typeof(string) }) ??
                                     throw new MissingMethodException(harmonyType.FullName, ".ctor(String)");
            var harmonyMethodConstructor = harmonyMethodType.GetConstructor(new[] { typeof(MethodInfo) }) ??
                                           throw new MissingMethodException(harmonyMethodType.FullName, ".ctor(MethodInfo)");
            var patch = harmonyType.GetMethod(
                "Patch",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[]
                {
                    typeof(MethodBase), harmonyMethodType, harmonyMethodType, harmonyMethodType, harmonyMethodType
                },
                modifiers: null);
            var unpatch = harmonyType.GetMethod(
                "Unpatch",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(MethodBase), typeof(MethodInfo) },
                modifiers: null);
            if (patch is null || patch.ReturnType != typeof(MethodInfo) || patch.DeclaringType != harmonyType)
                throw new MissingMethodException(harmonyType.FullName, "Patch(MethodBase,HarmonyMethod,...)");
            if (unpatch is null || unpatch.ReturnType != typeof(void) || unpatch.DeclaringType != harmonyType)
                throw new MissingMethodException(harmonyType.FullName, "Unpatch(MethodBase,MethodInfo)");

            var target = typeof(TickManager).GetMethod(
                "DoSingleTick",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            var prefix = typeof(PerformanceTickBoundaryGate).GetMethod(
                "Prefix",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (target is null || target.ReturnType != typeof(void) || target.DeclaringType != typeof(TickManager))
                throw new MissingMethodException(typeof(TickManager).FullName, "DoSingleTick()");
            if (prefix is null || prefix.ReturnType != typeof(bool) || prefix.DeclaringType != typeof(PerformanceTickBoundaryGate))
                throw new MissingMethodException(typeof(PerformanceTickBoundaryGate).FullName, "Prefix()");

            var harmony = harmonyConstructor.Invoke(new object[] { OwnerId });
            var prefixDescriptor = harmonyMethodConstructor.Invoke(new object[] { prefix });
            return TryAcquire(
                install: () =>
                {
                    var replacement = patch.Invoke(harmony, new[] { target, prefixDescriptor, null, null, null });
                    if (replacement is not MethodInfo)
                        throw new InvalidOperationException("Harmony did not return the patched TickManager replacement.");
                },
                uninstall: () => unpatch.Invoke(harmony, new object[] { target, prefix }),
                ownsExactPatch: () => OwnsExactPatch(catalog, target, prefix),
                out lease,
                out reason);
        }
        catch (Exception exception)
        {
            PerformanceTickBoundaryGate.Disarm();
            reason = "Could not install the exact performance tick boundary: " +
                     CircinusRuntimeAdapter.Describe(exception);
            return false;
        }
    }

    internal static bool TryAcquireForTests(
        Action install,
        Action uninstall,
        Func<bool> ownsExactPatch,
        out PerformanceTickBoundaryPatch? lease,
        out string reason) =>
        TryAcquire(install, uninstall, ownsExactPatch, out lease, out reason);

    private static bool TryAcquire(
        Action install,
        Action uninstall,
        Func<bool> ownsExactPatch,
        out PerformanceTickBoundaryPatch? lease,
        out string reason)
    {
        lease = null;
        try
        {
            install();
            if (!ownsExactPatch())
                throw new InvalidOperationException("Harmony did not expose one exact Gateway tick-boundary prefix.");
            lease = new PerformanceTickBoundaryPatch(uninstall, ownsExactPatch);
            reason = string.Empty;
            return true;
        }
        catch (Exception primary)
        {
            PerformanceTickBoundaryGate.Disarm();
            try
            {
                // Exact-method unpatch is idempotent. Always attempt it after the Patch call was
                // admitted: catalog verification itself may be the failing/false operation.
                uninstall();
                if (ownsExactPatch())
                    throw new InvalidOperationException("Harmony retained the Gateway performance tick-boundary prefix.");
            }
            catch (Exception cleanup)
            {
                reason = "Could not install or clean the exact performance tick boundary: " +
                         CircinusRuntimeAdapter.Describe(new AggregateException(primary, cleanup));
                return false;
            }

            reason = "Could not install the exact performance tick boundary: " +
                     CircinusRuntimeAdapter.Describe(primary);
            return false;
        }
    }

    public void Arm(int gameTick)
    {
        ThrowIfDisposed();
        PerformanceTickBoundaryGate.Arm(gameTick);
        armedTarget = gameTick;
    }

    public void Confirm(int gameTick)
    {
        ThrowIfDisposed();
        if (armedTarget < 0)
            throw new InvalidOperationException("No exact performance tick boundary is armed.");
        if (gameTick != armedTarget)
            throw new InvalidOperationException(
                $"Performance window expected exact tick boundary {armedTarget}, observed {gameTick}.");
    }

    public void Disarm()
    {
        PerformanceTickBoundaryGate.Disarm();
        armedTarget = -1;
    }

    public void Dispose()
    {
        if (disposed) return;
        Disarm();
        uninstall();
        if (ownsExactPatch())
            throw new InvalidOperationException("Harmony retained the Gateway performance tick-boundary prefix.");
        disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed) throw new ObjectDisposedException(nameof(PerformanceTickBoundaryPatch));
    }

    private static Type ExactType(Assembly assembly, string name) =>
        assembly.GetType(name, throwOnError: false, ignoreCase: false) ?? throw new MissingMemberException(name);

    private static bool OwnsExactPatch(
        ReflectionPerformanceHarmonyCatalog catalog,
        MethodBase target,
        MethodInfo prefix)
    {
        var installed = catalog.ResolveOwner(OwnerId);
        return installed.Count == 1 && installed[0].Kind == PerformanceHarmonyPatchKind.Prefix &&
               Equals(installed[0].PatchedTarget, target) && Equals(installed[0].PatchMethod, prefix);
    }
}
