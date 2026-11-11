using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.MessagePlacement;

public sealed record MessageBarColonistExclusionTargets(MethodInfo CurrentMood, MethodInfo EdgeExpansion,
    MethodInfo ExpandedBy, MethodInfo DrawAtlas, FieldInfo MoodAtlas, MethodInfo Dead, MethodInfo Corpse,
    MethodInfo Selector, MethodInfo SelectedObjects, MethodInfo Contains, MethodInfo WorldSelected,
    MethodInfo DrawSelection, MethodInfo GetCaravan, MethodInfo DrawCaravanSelection, MethodInfo ContractedBy,
    MethodInfo ColonistBar, MethodInfo Scale);

// Describes the native absence gates; it never installs a patch or renders a replacement.
public sealed class MessageBarColonistExclusionGuard
{
    private readonly MessageBarColonistExclusionTargets targets;
    public MessageBarColonistExclusionGuard(MessageBarColonistExclusionTargets targets) => this.targets = targets;
    public (ISet<int>? Required, string? Failure) Describe(IReadOnlyList<CodeInstruction> body)
    {
        try
        {
            var stream = new Stream(body);
            int mood = stream.Unique(targets.CurrentMood), edge = stream.Unique(targets.EdgeExpansion);
            int atlas = stream.Unique(targets.DrawAtlas), expand = stream.Unique(targets.ExpandedBy);
            int threshold = Stored(stream[mood + 1]), expansion = Stored(stream[edge + 1]);
            Require(Argument(stream[mood - 1]) == 2 && threshold >= 0 && Loaded(stream[edge - 1]) == threshold &&
                expansion >= 0 && threshold != expansion, "The native expansion no longer derives from the supplied pawn's current mood.");
            stream.Protect(mood - 1, mood + 1);
            Require(Loaded(stream[edge + 2]) == expansion && Float(stream[edge + 3], 0) &&
                LessOrEqualUnordered(stream[edge + 4]), "Native mood drawing no longer requires strictly positive expansion.");
            stream.Protect(edge - 1, edge + 4);
            Require(expand == atlas - 2 && Argument(stream[atlas - 4]) == 1 && Loaded(stream[atlas - 3]) == expansion &&
                stream[atlas - 1].opcode == OpCodes.Ldsfld && Equals(stream[atlas - 1].operand, targets.MoodAtlas),
                "The mood atlas does not consume the supplied rectangle and actual expansion.");
            stream.Protect(atlas - 4, atlas);
            Stable(stream, threshold, mood + 1); Stable(stream, expansion, edge + 1);
            Require(!stream.Reaches(edge - 1, mood + 1) && !stream.Reaches(atlas, edge + 1),
                "A mood value can reach drawing without its proved producer.");
            int moodExit = stream.Destination(edge + 4);
            Require(moodExit > atlas && !stream.Reaches(atlas, edge + 4, moodExit),
                "A branch can draw a mood edge without positive native expansion.");

            int selection = stream.Unique(targets.DrawSelection), start = selection - 19;
            Require(Argument(stream[start]) == 2 && stream[start + 1].Calls(targets.Dead) && TrueBranch(stream[start + 2]) &&
                stream.Destination(start + 2) == start + 8 && stream[start + 3].Calls(targets.Selector) &&
                stream[start + 4].Calls(targets.SelectedObjects) && Argument(stream[start + 5]) == 2 &&
                stream[start + 6].Calls(targets.Contains) && Jump(stream[start + 7]) && stream.Destination(start + 7) == start + 13 &&
                stream[start + 8].Calls(targets.Selector) && stream[start + 9].Calls(targets.SelectedObjects) &&
                Argument(stream[start + 10]) == 2 && stream[start + 11].Calls(targets.Corpse) && stream[start + 12].Calls(targets.Contains) &&
                FalseBranch(stream[start + 13]) && stream[start + 14].Calls(targets.WorldSelected) && TrueBranch(stream[start + 15]) &&
                Argument(stream[start + 16]) == 0 && Argument(stream[start + 17]) == 2,
                "Native selection no longer tests the supplied pawn or its corpse and current world view.");
            int rectangle = Loaded(stream[selection - 1]);
            Require(rectangle >= 0 && rectangle != threshold && rectangle != expansion, "The selection rectangle local is ambiguous.");
            stream.Protect(start, selection);
            int world = selection + 2, worldGate = selection + 3;
            Require(Jump(stream[selection + 1]) && stream[world].Calls(targets.WorldSelected) && FalseBranch(stream[worldGate]) &&
                stream.Destination(start + 13) == world && stream.Destination(start + 15) == world,
                "The unselected native path does not proceed through the world-view gate.");
            stream.Protect(world, worldGate);
            int caravan = stream.Unique(targets.DrawCaravanSelection);
            Require(Argument(stream[caravan - 4]) == 0 && Argument(stream[caravan - 3]) == 2 &&
                stream[caravan - 2].Calls(targets.GetCaravan) && Loaded(stream[caravan - 1]) == rectangle &&
                stream.Destination(worldGate) > caravan && stream.Destination(selection + 1) == stream.Destination(worldGate),
                "The caravan overlay does not use the native pawn-derived caravan and shared selection rectangle.");
            stream.Protect(caravan - 4, caravan);
            Require(!stream.Reaches(selection, start + 13, world) && !stream.Reaches(caravan, worldGate, stream.Destination(worldGate)),
                "A branch bypasses a native selection exclusion.");

            int rectangleStore = stream.Single(stream.All.Where(index => Stored(stream[index]) == rectangle));
            Require(Argument(stream[rectangleStore - 7]) == 1 && Float(stream[rectangleStore - 6], -2) &&
                Argument(stream[rectangleStore - 5]) == 0 && stream[rectangleStore - 4].Calls(targets.ColonistBar) &&
                stream[rectangleStore - 3].Calls(targets.Scale) && stream[rectangleStore - 2].opcode == OpCodes.Mul &&
                stream[rectangleStore - 1].Calls(targets.ContractedBy),
                "The native selection rectangle no longer derives from the supplied portrait and bar scale.");
            stream.Protect(rectangleStore - 7, rectangleStore);
            Stable(stream, rectangle, rectangleStore);
            Require(!stream.Reaches(selection, rectangleStore) && !stream.Reaches(caravan, rectangleStore),
                "A selection overlay can consume an unproved rectangle.");
            foreach (int index in stream.All)
            {
                var instruction = stream[index];
                Require(instruction.opcode != OpCodes.Starg && instruction.opcode != OpCodes.Starg_S,
                    "A native drawing argument is overwritten.");
                if (instruction.opcode != OpCodes.Ldarga && instruction.opcode != OpCodes.Ldarga_S) continue;
                var next = stream[index + 1];
                Require(ArgumentNumber(instruction.operand) == 1 && next.operand is MethodInfo getter && next.Calls(getter) &&
                    getter.DeclaringType == targets.ExpandedBy.GetParameters()[0].ParameterType && getter.IsSpecialName &&
                    getter.Name.StartsWith("get_", StringComparison.Ordinal) && !getter.ReturnType.IsByRef && getter.GetParameters().Length == 0,
                    "A supplied pawn or rectangle address escapes its native read.");
                stream.Required.Add(stream.Original(index)); stream.Required.Add(stream.Original(index + 1));
            }
            return (stream.Required, null);
        }
        catch (Exception error) { return (null, error.GetBaseException().Message); }
    }

    private static void Stable(Stream stream, int local, int write)
    {
        Require(stream.All.All(index => (Stored(stream[index]) != local || index == write) && Address(stream[index]) != local),
            "A native exclusion local can be overwritten or escape through an address.");
    }
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    private static bool Float(CodeInstruction instruction, float value) => instruction.opcode == OpCodes.Ldc_R4 && Equals(instruction.operand, value);
    private static bool FalseBranch(CodeInstruction instruction) => instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S;
    private static bool TrueBranch(CodeInstruction instruction) => instruction.opcode == OpCodes.Brtrue || instruction.opcode == OpCodes.Brtrue_S;
    private static bool Jump(CodeInstruction instruction) => instruction.opcode == OpCodes.Br || instruction.opcode == OpCodes.Br_S;
    private static bool LessOrEqualUnordered(CodeInstruction instruction) => instruction.opcode == OpCodes.Ble_Un || instruction.opcode == OpCodes.Ble_Un_S;
    private static int Argument(CodeInstruction instruction) => instruction.opcode == OpCodes.Ldarg || instruction.opcode == OpCodes.Ldarg_S ?
        ArgumentNumber(instruction.operand) : ShortLocal(instruction, "ldarg");
    private static int Loaded(CodeInstruction instruction) => instruction.opcode == OpCodes.Ldloc || instruction.opcode == OpCodes.Ldloc_S ? Number(instruction.operand) : ShortLocal(instruction, "ldloc");
    private static int Stored(CodeInstruction instruction) => instruction.opcode == OpCodes.Stloc || instruction.opcode == OpCodes.Stloc_S ? Number(instruction.operand) : ShortLocal(instruction, "stloc");
    private static int Address(CodeInstruction instruction) => instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S ? Number(instruction.operand) : -1;
    private static int ShortLocal(CodeInstruction instruction, string prefix) => instruction.opcode.Name is { } name &&
        name.Length == 7 && name.StartsWith(prefix + ".", StringComparison.Ordinal) && name[6] >= '0' && name[6] <= '3' ? name[6] - '0' : -1;
    private static int Number(object? operand) => operand is LocalBuilder local ? local.LocalIndex : Convert.ToInt32(operand);
    private static int ArgumentNumber(object? operand) => operand is ParameterInfo parameter ? parameter.Position + 1 : Convert.ToInt32(operand);

    private sealed class Stream
    {
        private readonly IReadOnlyList<CodeInstruction> body;
        private readonly int[] significant;
        private readonly Dictionary<Label, int> labels = new();
        private readonly List<int>[] edges;
        internal Stream(IReadOnlyList<CodeInstruction> body)
        {
            Require(body.Count <= 10000 && body.All(instruction => instruction.blocks.Count == 0 && instruction.opcode != OpCodes.Jmp),
                "Native exclusion has an unproved exception or transfer graph.");
            this.body = body;
            significant = Enumerable.Range(0, body.Count).Where(index => body[index].opcode != OpCodes.Nop).ToArray();
            edges = Enumerable.Range(0, significant.Length).Select(_ => new List<int>()).ToArray();
            int position = 0;
            for (int index = 0; index < body.Count; index++)
            {
                while (position < significant.Length && significant[position] < index) position++;
                foreach (var label in body[index].labels) Require(labels.TryAdd(label, position), "An exclusion branch label is duplicated.");
            }
            foreach (int index in All)
            {
                var instruction = this[index];
                if (instruction.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch)
                {
                    var targets = instruction.operand is Label label ? new[] { label } : instruction.operand as Label[];
                    Require(targets != null, "An exclusion branch is unreadable.");
                    foreach (var target in targets!)
                    {
                        Require(labels.TryGetValue(target, out int destination), "An exclusion branch target is missing.");
                        edges[index].Add(destination);
                    }
                    if (instruction.opcode.FlowControl == FlowControl.Branch) continue;
                }
                if (instruction.opcode.FlowControl is not (FlowControl.Return or FlowControl.Throw)) edges[index].Add(index + 1);
            }
        }
        internal IEnumerable<int> All => Enumerable.Range(0, significant.Length);
        internal ISet<int> Required { get; } = new HashSet<int>();
        internal CodeInstruction this[int index] => index >= 0 && index < significant.Length ? body[significant[index]] : throw new InvalidOperationException("A native exclusion expression is truncated.");
        internal int Original(int index) => significant[index];
        internal int Unique(MethodInfo method) => Single(All.Where(index => this[index].Calls(method)));
        internal int Single(IEnumerable<int> values) { var matches = values.ToArray(); Require(matches.Length == 1, "A native exclusion producer or consumer is missing or ambiguous."); return matches[0]; }
        internal int Destination(int branch) => this[branch].operand is Label label && labels.TryGetValue(label, out int destination) ? destination : throw new InvalidOperationException("A native exclusion branch is unreadable.");
        internal void Protect(int start, int end)
        {
            Require(start >= 0 && end < significant.Length && start <= end, "A native exclusion interval is invalid.");
            foreach (int from in All)
                if (from < start || from > end)
                    Require(edges[from].All(to => to <= start || to > end), "A branch enters the middle of a native exclusion producer or consumer.");
            for (int index = start; index <= end; index++) Required.Add(significant[index]);
        }
        internal bool Reaches(int target, int removed, int? replacement = null)
        {
            var pending = new Stack<int>(); var visited = new HashSet<int>(); pending.Push(0);
            while (pending.Count != 0)
            {
                int index = pending.Pop();
                if (index == significant.Length || !visited.Add(index)) continue;
                if (index == removed) { if (replacement.HasValue) pending.Push(replacement.Value); continue; }
                if (index == target) return true;
                foreach (int next in edges[index]) pending.Push(next);
            }
            return false;
        }
    }
}
