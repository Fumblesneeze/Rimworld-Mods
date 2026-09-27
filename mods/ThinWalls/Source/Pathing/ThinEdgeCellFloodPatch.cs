using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace ThinWalls.Pathing;

/// <summary>Preserves the engine's cell flood, predicates and scratch grid; only constrains each traversed edge.</summary>
[HarmonyPatch(typeof(Reachability), "CheckCellBasedReachability")]
public static class ThinEdgeCellFloodPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo flood = AccessTools.Method(typeof(FloodFiller), nameof(FloodFiller.FloodFill), new[]
        {
            typeof(IntVec3), typeof(Predicate<IntVec3>), typeof(Func<IntVec3, bool>), typeof(int), typeof(bool), typeof(IEnumerable<IntVec3>)
        });
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (!instruction.Calls(flood)) { yield return instruction; continue; }
            replaced++;
            yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Reachability), "map"));
            yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThinEdgeCellFloodPatch), nameof(Flood)));
        }
        if (replaced != 1) throw new InvalidOperationException("Thin Walls expected exactly one native reachability cell-flood call, got " + replaced);
    }

    public static void Flood(FloodFiller filler, IntVec3 root, Predicate<IntVec3> nativePass,
        Func<IntVec3, bool> nativeProcess, int maximum, bool parents, IEnumerable<IntVec3> extraRoots,
        Map map, TraverseParms parms)
    {
        if (!ThinWallMapComponent.TryGet(map, out var component) || !component.HasCompletedEdgeStructures)
        {
            filler.FloodFill(root, nativePass, nativeProcess, maximum, parents, extraRoots);
            return;
        }
        IntVec3 current = IntVec3.Invalid;
        filler.FloodFill(root,
            cell => nativePass(cell) && (!current.IsValid || component.AllowsStep(current, cell, parms)),
            cell => { current = cell; return nativeProcess(cell); }, maximum, parents, extraRoots);
    }
}
