using System.Reflection;
using HarmonyLib;

namespace ImmersiveChefs.Harmony.Tests;

internal sealed class HarmonyPatchScope : IDisposable
{
    private readonly Action cleanup;
    private bool disposed;

    private HarmonyPatchScope(HarmonyLib.Harmony harmony)
        : this(() => harmony.UnpatchAll(harmony.Id))
    {
    }

    private HarmonyPatchScope(Action cleanup)
    {
        this.cleanup = cleanup;
    }

    internal static HarmonyPatchScope CreateCleanupProbe(Action cleanup)
    {
        return new HarmonyPatchScope(cleanup ?? throw new ArgumentNullException(nameof(cleanup)));
    }

    public static HarmonyPatchScope ApplyPostfix(string ownerId, MethodBase target, MethodInfo postfix)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException("A non-empty Harmony owner ID is required.", nameof(ownerId));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (postfix is null)
        {
            throw new ArgumentNullException(nameof(postfix));
        }

        if (HarmonyLib.Harmony.HasAnyPatches(ownerId))
        {
            throw new InvalidOperationException($"Harmony owner '{ownerId}' already has active patches.");
        }

        var harmony = new HarmonyLib.Harmony(ownerId);
        try
        {
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            return new HarmonyPatchScope(harmony);
        }
        catch (Exception patchFailure)
        {
            try
            {
                harmony.UnpatchAll(ownerId);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(
                    $"Applying Harmony owner '{ownerId}' failed and rollback also failed.",
                    patchFailure,
                    cleanupFailure);
            }

            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        cleanup();
        disposed = true;
    }
}
