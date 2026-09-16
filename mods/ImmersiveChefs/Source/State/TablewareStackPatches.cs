using HarmonyLib;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

[HarmonyPatch(typeof(DamageWorker), nameof(DamageWorker.Apply))]
internal static class TablewareDamagePatch
{
    private static void Prefix(DamageInfo dinfo, Thing victim, out (CompTablewareStack? Pile, int Maximum) __state)
    {
        __state = default;
        if (dinfo.Def.harmsHealth && victim.def.useHitPoints && victim.stackCount > 0 &&
            CompTablewareStack.For(victim) is { } pile)
            __state = (pile, pile.BeginNativeDamage());
    }

    // Finalizer restores the representative even if a third-party worker hook throws.
    private static void Finalizer((CompTablewareStack? Pile, int Maximum) __state) =>
        __state.Pile?.FinishNativeDamage(__state.Maximum);
}

[HarmonyPatch(typeof(Thing), nameof(Thing.CanStackWith))]
internal static class TablewareMaterialStackPatch
{
    private static void Postfix(Thing __instance, Thing other, ref bool __result)
    {
        // The enclosing ThingWithComps method still checks every comp, including
        // quality, sanitation/provenance, ownership and optional comp restrictions.
        if (!__result && CompTablewareStack.For(__instance) is not null && CompTablewareStack.For(other) is not null &&
            !__instance.Destroyed && !other.Destroyed && __instance.def == other.def &&
            __instance.def.category == ThingCategory.Item && !__instance.IsRelic() && !other.IsRelic())
            __result = true;
    }
}

[HarmonyPatch(typeof(Thing), nameof(Thing.TryAbsorbStack))]
internal static class TablewareAbsorbStackPatch
{
    internal sealed class Transfer
    {
        internal readonly CompTablewareStack Destination;
        internal readonly CompTablewareStack Source;
        internal readonly TablewareStackLedger BeforeDestination;
        internal readonly TablewareStackLedger BeforeSource;
        internal Transfer(CompTablewareStack destination, CompTablewareStack source)
        {
            Destination = destination; Source = source;
            BeforeDestination = destination.Snapshot(); BeforeSource = source.Snapshot();
        }
    }

    private static bool Prefix(Thing __instance, Thing other, ref bool __result, out Transfer? __state)
    {
        __state = null;
        if (CompTablewareStack.For(__instance) is not { } destination || CompTablewareStack.For(other) is not { } source)
            return true;
        if (ReferenceEquals(__instance, other)) { __result = false; return false; }
        __state = new Transfer(destination, source);
        return true;
    }

    private static void Postfix(Thing __instance, Transfer? __state)
    {
        if (__state is null) return;
        var moved = __instance.stackCount - __state.BeforeDestination.Units.Count;
        if (moved <= 0) return;
        __state.BeforeDestination.Absorb(__state.BeforeSource, __instance.stackCount);
        __state.Destination.Restore(__state.BeforeDestination);
        __state.Source.Restore(__state.BeforeSource);
    }
}

[HarmonyPatch(typeof(Thing), nameof(Thing.SplitOff))]
internal static class TablewareSplitStackPatch
{
    private static void Prefix(Thing __instance, int count, out TablewareStackLedger? __state) =>
        __state = count > 0 && count < __instance.stackCount ? CompTablewareStack.For(__instance)?.Snapshot() : null;

    private static void Postfix(Thing __instance, Thing __result, TablewareStackLedger? __state)
    {
        if (__state is null || ReferenceEquals(__instance, __result) || CompTablewareStack.For(__result) is not { } piece)
            return;
        var taken = __state.Take(__result.stackCount);
        piece.Restore(taken);
        CompTablewareStack.For(__instance)!.Restore(__state);
    }
}
