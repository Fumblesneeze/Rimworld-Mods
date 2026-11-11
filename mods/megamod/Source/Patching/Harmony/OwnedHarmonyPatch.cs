using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HarmonyInstance = HarmonyLib.Harmony;

namespace Megamod.Patching;

public sealed class OwnedHarmonyPatch
{
    public const string OwnerId = "fumblesneeze.megamod";

    private readonly MethodBase target;
    private readonly HarmonyPatchType kind;
    private readonly MethodInfo callback;
    private readonly Func<IReadOnlyList<Patch>>? readOwnedSlot;
    private readonly HarmonyInstance harmony = new(OwnerId);
    private bool attempted;

    public OwnedHarmonyPatch(MethodBase target, HarmonyPatchType kind, MethodInfo callback,
        Func<IReadOnlyList<Patch>>? readOwnedSlot = null)
    {
        this.target = target;
        this.kind = kind;
        this.callback = callback;
        this.readOwnedSlot = readOwnedSlot;
    }

    public string? ValidateOwnership() => CurrentSlot().Any(patch => patch.owner == OwnerId)
        ? "The owned target/kind slot already contains a patch; safe rollback cannot be acquired."
        : null;

    public void Install()
    {
        string? unavailable = ValidateOwnership();
        if (unavailable != null) throw new InvalidOperationException(unavailable);
        var processor = harmony.CreateProcessor(target);
        switch (kind)
        {
            case HarmonyPatchType.Prefix: processor.AddPrefix(callback); break;
            case HarmonyPatchType.Postfix: processor.AddPostfix(callback); break;
            case HarmonyPatchType.Transpiler: processor.AddTranspiler(callback); break;
            case HarmonyPatchType.Finalizer: processor.AddFinalizer(callback); break;
            default: throw new NotSupportedException($"Unsupported owned patch kind: {kind}");
        }
        attempted = true;
        processor.Patch();
    }

    public void Rollback()
    {
        if (!attempted) return;
        var owned = CurrentSlot().Where(patch => patch.owner == OwnerId).ToArray();
        if (owned.Length > 1 || (owned.Length == 1 && owned[0].PatchMethod != callback))
            throw new InvalidOperationException("The acquired owned target/kind slot changed; safe rollback cannot remove other members.");
        harmony.Unpatch(target, kind, OwnerId);
        attempted = false;
    }

    private IEnumerable<Patch> CurrentSlot()
    {
        if (readOwnedSlot != null) return readOwnedSlot();
        var patches = HarmonyInstance.GetPatchInfo(target);
        if (patches == null) return Array.Empty<Patch>();
        return kind switch
        {
            HarmonyPatchType.Prefix => patches.Prefixes,
            HarmonyPatchType.Postfix => patches.Postfixes,
            HarmonyPatchType.Transpiler => patches.Transpilers,
            HarmonyPatchType.Finalizer => patches.Finalizers,
            _ => throw new NotSupportedException($"Unsupported owned patch kind: {kind}")
        };
    }
}
