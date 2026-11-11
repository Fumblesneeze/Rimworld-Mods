using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.MessagePlacement;

public sealed record MessageBarAdornmentTargets(FieldInfo Drawer, FieldInfo EntryPawn, FieldInfo EntryGroup,
    FieldInfo Equipment, MethodInfo Primary, MethodInfo WeaponMode, MethodInfo Drafted, FieldInfo Definition,
    MethodInfo IsWeapon, MethodInfo DrawGroupFrame, MethodInfo HandleGroupFrameClicks, MethodInfo ThingIcon,
    MethodInfo RectangleX, MethodInfo RectangleY, MethodInfo RectangleWidth, MethodInfo RectangleHeight,
    ConstructorInfo RectangleConstructor, MethodInfo ScaleRectangle);

// Participates in the prepared owner's geometry receipt; it never installs another bar patch.
public sealed class MessageBarAdornmentGuard
{
    private readonly MessageBarAdornmentTargets targets;
    public MessageBarAdornmentGuard(MessageBarAdornmentTargets targets) => this.targets = targets;

    internal bool IsDrawingFamily(Type? type) => type == targets.ThingIcon.DeclaringType;
    internal bool IsProvedCall(MethodInfo method) => method == targets.DrawGroupFrame ||
        method == targets.HandleGroupFrameClicks || method == targets.ThingIcon || method == targets.ScaleRectangle;

    public (string? Failure, ISet<int> Required) Describe(IReadOnlyList<CodeInstruction> body,
        int handoffEnd, int entriesLocal, int entryLocal, int rectangleLocal, int drawAt)
    {
        try
        {
            var scan = new Scan(body);
            int handoff = scan.Position(handoffEnd), draw = scan.Position(drawAt);
            int frames = Store(scan[handoff]);
            Need(frames >= 0 && frames != entriesLocal && frames != entryLocal && frames != rectangleLocal,
                "The captured group-frame flag has no separate local.");
            Need(scan.All.Count(index => Store(scan[index]) == frames) == 1 &&
                scan.All.All(index => Address(scan[index]) != frames), "The captured group-frame flag can change.");

            int frame = scan.Unique(targets.DrawGroupFrame), clicks = scan.Unique(targets.HandleGroupFrameClicks);
            GroupArguments(scan, frame, entryLocal);
            // The other AND operand may be any native group-change flag: false captured frames
            // must still make this outward draw unreachable, including across loop backedges.
            Need(Load(scan[frame - 7]) == frames && scan[frame - 6].opcode == OpCodes.And && FalseBranch(scan[frame - 5]),
                "Group drawing no longer requires the captured frame flag.");
            scan.Protect(frame - 7, frame);
            Need(!scan.Reachable(0, frame, frame - 5, scan.Destination(frame - 5)), "A branch bypasses group-frame eligibility.");
            Need(frame < draw, "Group drawing moved beyond the proved entry lifetime.");

            int frameGate = scan.Single(scan.All.Where(index => index > frame && index + 1 < scan.Count &&
                Load(scan[index]) == frames && FalseBranch(scan[index + 1])));
            scan.Protect(frameGate, frameGate + 1);
            Need(!scan.Reachable(0, clicks, frameGate + 1, scan.Destination(frameGate + 1)), "Frame interaction bypasses the captured frame flag.");
            int clickEntry = Load(scan[clicks - 2]);
            Need(clickEntry >= 0, "Frame interaction has no entry local.");
            GroupArguments(scan, clicks, clickEntry);
            int clickStore = scan.Single(scan.All.Where(index => Store(scan[index]) == clickEntry));
            Need(Load(scan[clickStore - 3]) == entriesLocal && Load(scan[clickStore - 2]) >= 0 &&
                Item(scan[clickStore - 1], targets.EntryGroup.DeclaringType!), "Frame interaction does not use the prepared entries.");
            scan.Protect(clickStore - 3, clickStore);
            scan.Protect(clicks - 4, clicks);
            Need(!scan.Reachable(0, clicks, clickStore), "A branch substitutes the frame-click entry.");
            Need(scan.All.All(index => Address(scan[index]) != clickEntry ||
                index + 1 < scan.Count && scan[index + 1].opcode == OpCodes.Ldfld), "A frame-click entry address escapes.");

            int primaryCall = scan.Unique(targets.Primary), start = primaryCall - 8;
            Need(Load(scan[start]) == entryLocal && Field(scan[start + 1], targets.EntryPawn) &&
                Field(scan[start + 2], targets.Equipment) && scan[start + 3].opcode == OpCodes.Dup &&
                TrueBranch(scan[start + 4]) && scan.Destination(start + 4) == primaryCall &&
                scan[start + 5].opcode == OpCodes.Pop && scan[start + 6].opcode == OpCodes.Ldnull &&
                Jump(scan[start + 7]) && scan.Destination(start + 7) == primaryCall + 1,
                "The weapon is no longer the current entry pawn's optional primary equipment.");
            int primary = Store(scan[primaryCall + 1]);
            Need(primary >= 0 && primary != entryLocal && primary != rectangleLocal && primary != entriesLocal && primary != frames,
                "The primary equipment local aliases prepared state.");
            int mode = primaryCall + 2;
            Need(scan[mode].Calls(targets.WeaponMode) && Integer(scan[mode + 1], 2) && EqualBranch(scan[mode + 2]) &&
                scan[mode + 3].Calls(targets.WeaponMode) && Integer(scan[mode + 4], 1) && NotEqualBranch(scan[mode + 5]) &&
                Load(scan[mode + 6]) == entryLocal && Field(scan[mode + 7], targets.EntryPawn) && scan[mode + 8].Calls(targets.Drafted),
                "Weapon eligibility no longer uses Always or WhileDrafted with the current pawn.");
            int nonnull;
            var exits = new List<int>();
            if (FalseBranch(scan[mode + 9]))
            {
                // Optimized host compiler: the short-circuit boolean branches directly.
                nonnull = mode + 10;
                Need(scan.Destination(mode + 2) == nonnull, "Always no longer reaches the ordinary equipment checks.");
                exits.Add(mode + 5); exits.Add(mode + 9);
            }
            else
            {
                // Inspected 1.7 native compiler: merge false/true values before brfalse.
                Need(Jump(scan[mode + 9]) && Integer(scan[mode + 10], 0) && Jump(scan[mode + 11]) &&
                    Integer(scan[mode + 12], 1) && FalseBranch(scan[mode + 13]) &&
                    scan.Destination(mode + 2) == mode + 12 && scan.Destination(mode + 5) == mode + 10 &&
                    scan.Destination(mode + 9) == mode + 13 && scan.Destination(mode + 11) == mode + 13,
                    "The native weapon-visibility merge changed.");
                nonnull = mode + 14; exits.Add(mode + 13);
            }
            Need(Load(scan[nonnull]) == primary && FalseBranch(scan[nonnull + 1]) &&
                Load(scan[nonnull + 2]) == primary && Field(scan[nonnull + 3], targets.Definition) &&
                scan[nonnull + 4].Calls(targets.IsWeapon) && FalseBranch(scan[nonnull + 5]),
                "Weapon drawing no longer requires nonnull primary equipment with a weapon definition.");
            exits.Add(nonnull + 1); exits.Add(nonnull + 5);
            int rectangle = nonnull + 6;
            Need(RectangleRead(scan, rectangle, rectangleLocal, targets.RectangleX) &&
                RectangleRead(scan, rectangle + 2, rectangleLocal, targets.RectangleY) &&
                RectangleRead(scan, rectangle + 4, rectangleLocal, targets.RectangleHeight) && Float(scan[rectangle + 6], 1.05f) &&
                scan[rectangle + 7].opcode == OpCodes.Mul && scan[rectangle + 8].opcode == OpCodes.Add &&
                RectangleRead(scan, rectangle + 9, rectangleLocal, targets.RectangleWidth) &&
                RectangleRead(scan, rectangle + 11, rectangleLocal, targets.RectangleHeight) &&
                scan[rectangle + 13].opcode == OpCodes.Newobj && Equals(scan[rectangle + 13].operand, targets.RectangleConstructor) &&
                Float(scan[rectangle + 14], .75f) && scan[rectangle + 15].Calls(targets.ScaleRectangle) &&
                Load(scan[rectangle + 16]) == primary, "Weapon drawing no longer consumes the native portrait-relative rectangle and equipment.");
            int icon = scan.Unique(targets.ThingIcon);
            Need(IconTail(scan, rectangle + 17, icon), "The native weapon icon argument tail changed.");
            Need(exits.All(branch => scan.Destination(branch) > icon), "A failed eligibility check can still enter weapon drawing.");
            scan.Protect(start, icon);
            Need(!scan.Reachable(0, start, draw), "Weapon drawing can bypass the proved portrait entry.");
            Need(scan.All.Where(index => index > draw && index <= icon).All(index =>
                Store(scan[index]) != entryLocal && Store(scan[index]) != rectangleLocal &&
                (Store(scan[index]) != primary || index == primaryCall + 1)), "A consumed weapon local changes after portrait drawing.");
            Need(scan.All.All(index => Address(scan[index]) != primary), "The primary equipment local address escapes.");
            foreach (int branch in exits)
                Need(!scan.Reachable(scan.Destination(branch), icon, start), "An ineligible weapon path can re-enter drawing without renewed checks.");
            return (null, scan.Required);
        }
        catch (InvalidOperationException error) { return (error.Message, new HashSet<int>()); }
    }

    private void GroupArguments(Scan scan, int call, int entry) => Need(scan[call - 4].opcode == OpCodes.Ldarg_0 &&
        Field(scan[call - 3], targets.Drawer) && Load(scan[call - 2]) == entry && Field(scan[call - 1], targets.EntryGroup),
        "A group consumer no longer uses this drawer and its prepared entry group.");
    private bool IconTail(Scan scan, int start, int icon)
    {
        var parameters = targets.ThingIcon.GetParameters();
        if (!targets.ThingIcon.IsStatic || targets.ThingIcon.ReturnType != typeof(void)) return false;
        if (parameters.Length != 7 || icon != start + 7 || parameters[2].ParameterType != typeof(float) ||
            Nullable.GetUnderlyingType(parameters[3].ParameterType) == null || parameters[4].ParameterType != typeof(bool) ||
            parameters[5].ParameterType != typeof(float) || parameters[6].ParameterType != typeof(bool)) return false;
        int nullable = Address(scan[start + 1]);
        return Float(scan[start], 1f) && nullable >= 0 && scan[start + 2].opcode == OpCodes.Initobj &&
            Equals(scan[start + 2].operand, parameters[3].ParameterType) && Load(scan[start + 3]) == nullable &&
            Integer(scan[start + 4], 1) && Float(scan[start + 5], 1f) && Integer(scan[start + 6], 0);
    }
    private static bool RectangleRead(Scan scan, int at, int local, MethodInfo getter) => Address(scan[at]) == local && scan[at + 1].Calls(getter);
    private static bool Field(CodeInstruction instruction, FieldInfo field) => instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, field);
    private static bool Item(CodeInstruction instruction, Type entry) => instruction.operand is MethodInfo method && instruction.Calls(method) &&
        method.DeclaringType == typeof(List<>).MakeGenericType(entry) && method.Name == "get_Item" && method.ReturnType == entry &&
        method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(int) });
    private static bool Integer(CodeInstruction instruction, int value) => instruction.LoadsConstant(value);
    private static bool Float(CodeInstruction instruction, float value) => instruction.opcode == OpCodes.Ldc_R4 && Equals(instruction.operand, value);
    private static bool FalseBranch(CodeInstruction instruction) => instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S;
    private static bool TrueBranch(CodeInstruction instruction) => instruction.opcode == OpCodes.Brtrue || instruction.opcode == OpCodes.Brtrue_S;
    private static bool EqualBranch(CodeInstruction instruction) => instruction.opcode == OpCodes.Beq || instruction.opcode == OpCodes.Beq_S;
    private static bool NotEqualBranch(CodeInstruction instruction) => instruction.opcode == OpCodes.Bne_Un || instruction.opcode == OpCodes.Bne_Un_S;
    private static bool Jump(CodeInstruction instruction) => instruction.opcode == OpCodes.Br || instruction.opcode == OpCodes.Br_S;
    private static void Need(bool condition, string failure) { if (!condition) throw new InvalidOperationException(failure); }
    private static int Load(CodeInstruction instruction) => Local(instruction, "ldloc", OpCodes.Ldloc, OpCodes.Ldloc_S);
    private static int Store(CodeInstruction instruction) => Local(instruction, "stloc", OpCodes.Stloc, OpCodes.Stloc_S);
    private static int Address(CodeInstruction instruction) => instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S ? Number(instruction.operand) : -1;
    private static int Local(CodeInstruction instruction, string prefix, OpCode full, OpCode shortCode) =>
        instruction.opcode == full || instruction.opcode == shortCode ? Number(instruction.operand) :
        instruction.opcode.Name is { } name && name.Length == 7 && name.StartsWith(prefix + ".", StringComparison.Ordinal) &&
        name[6] >= '0' && name[6] <= '3' ? name[6] - '0' : -1;
    private static int Number(object? operand) => operand is LocalBuilder local ? local.LocalIndex : Convert.ToInt32(operand);

    // This graph only proves entry/exclusion for the matched native blocks. It does not interpret IL.
    private sealed class Scan
    {
        private readonly IReadOnlyList<CodeInstruction> body;
        private readonly int[] meaningful;
        private readonly Dictionary<Label, int> labels = new();
        private readonly List<int>[] successors;
        internal Scan(IReadOnlyList<CodeInstruction> body)
        {
            Need(body.Count <= 10000 && body.All(instruction => instruction.blocks.Count == 0 && instruction.opcode != OpCodes.Jmp),
                "The adornment graph is not bounded ordinary control flow.");
            this.body = body;
            meaningful = Enumerable.Range(0, body.Count).Where(index => body[index].opcode != OpCodes.Nop).ToArray();
            successors = Enumerable.Range(0, meaningful.Length).Select(_ => new List<int>()).ToArray();
            int position = 0;
            for (int index = 0; index < body.Count; index++)
            {
                while (position < meaningful.Length && meaningful[position] < index) position++;
                foreach (var label in body[index].labels) Need(labels.TryAdd(label, position), "An adornment branch label is ambiguous.");
            }
            for (int index = 0; index < Count; index++)
            {
                var instruction = this[index];
                if (instruction.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch)
                {
                    var branches = instruction.operand is Label label ? new[] { label } : instruction.operand as Label[];
                    Need(branches != null, "An adornment branch destination is unreadable.");
                    foreach (var branch in branches!)
                    {
                        Need(labels.TryGetValue(branch, out int destination), "An adornment branch destination is missing.");
                        successors[index].Add(destination);
                    }
                    if (instruction.opcode.FlowControl == FlowControl.Branch) continue;
                }
                if (instruction.opcode.FlowControl is not (FlowControl.Return or FlowControl.Throw)) successors[index].Add(index + 1);
            }
        }
        internal int Count => meaningful.Length;
        internal IEnumerable<int> All => Enumerable.Range(0, Count);
        internal ISet<int> Required { get; } = new HashSet<int>();
        internal CodeInstruction this[int index] => index >= 0 && index < Count ? body[meaningful[index]] : throw new InvalidOperationException("An adornment argument chain is truncated.");
        internal int Position(int original) { int value = Array.IndexOf(meaningful, original); Need(value >= 0, "An adornment boundary is missing."); return value; }
        internal int Single(IEnumerable<int> matches) { var result = matches.ToArray(); Need(result.Length == 1, "An adornment producer or consumer is missing or ambiguous."); return result[0]; }
        internal int Unique(MethodInfo method) => Single(All.Where(index => this[index].Calls(method)));
        internal int Destination(int branch) => this[branch].operand is Label label && labels.TryGetValue(label, out int destination) ? destination : throw new InvalidOperationException("An adornment branch is unreadable.");
        internal void Protect(int start, int end)
        {
            Need(start >= 0 && end < Count && start <= end, "An adornment argument interval is invalid.");
            foreach (int from in All)
                if (from < start || from > end)
                    Need(successors[from].All(to => to <= start || to > end), "A branch enters the middle of an adornment producer or consumer.");
            for (int index = start; index <= end; index++) Required.Add(meaningful[index]);
        }
        internal bool Reachable(int start, int target, int excluded, int? replacement = null)
        {
            var pending = new Stack<int>(); var seen = new HashSet<int>(); pending.Push(start);
            while (pending.Count != 0)
            {
                int index = pending.Pop();
                if (index == Count || !seen.Add(index)) continue;
                if (index == excluded) { if (replacement.HasValue) pending.Push(replacement.Value); continue; }
                if (index == target) return true;
                foreach (int next in successors[index]) pending.Push(next);
            }
            return false;
        }
    }
}
