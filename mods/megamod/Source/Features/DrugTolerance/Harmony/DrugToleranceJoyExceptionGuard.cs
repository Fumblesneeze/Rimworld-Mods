using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugTolerance;

public sealed record DrugToleranceJoyExceptionMembers(FieldInfo Story, FieldInfo Traits, FieldInfo DrugDesire, MethodInfo Degree, MethodInfo MentalState);

public sealed class DrugToleranceJoyExceptionGuard
{
    private readonly DrugToleranceJoyExceptionMembers members;
    public DrugToleranceJoyExceptionGuard(DrugToleranceJoyExceptionMembers members) => this.members = members;
    public string? Validate(IReadOnlyList<CodeInstruction> body)
    {
        if (body.Count == 0 || body.Count > 4096 || body.Any(i => i.blocks.Count != 0)) return "The direct-joy exception flow is unproved.";
        if (members.Story.IsStatic || members.Traits.IsStatic || !members.DrugDesire.IsStatic ||
            members.Traits.DeclaringType != members.Story.FieldType || members.Degree.DeclaringType != members.Traits.FieldType ||
            members.Degree.IsStatic || members.Degree.ReturnType != typeof(int) ||
            !members.Degree.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { members.DrugDesire.FieldType }) ||
            members.MentalState.IsStatic || members.MentalState.ReturnType != typeof(bool) || members.MentalState.GetParameters().Length != 0)
            return "The direct-joy exception members changed.";
        var live = body.Select((instruction, index) => (instruction, index)).Where(x => x.instruction.opcode != OpCodes.Nop).ToArray();
        if (live.Length < 17) return "The native direct-joy exception is absent.";
        int start = live.Length - 17;
        var code = live.Skip(start).Select(x => x.instruction).ToArray();
        bool Op(int i, OpCode op) => code[i].opcode == op;
        bool Field(int i, OpCode op, FieldInfo field) => Op(i, op) && Equals(code[i].operand, field);
        bool Branch(int i, OpCode shortCode, OpCode longCode) => Op(i, shortCode) || Op(i, longCode);
        if (!Op(0, OpCodes.Ldarg_1) || !Field(1, OpCodes.Ldfld, members.Story) || !Branch(2, OpCodes.Brfalse_S, OpCodes.Brfalse) ||
            !Op(3, OpCodes.Ldarg_1) || !Field(4, OpCodes.Ldfld, members.Story) || !Field(5, OpCodes.Ldfld, members.Traits) ||
            !Field(6, OpCodes.Ldsfld, members.DrugDesire) || !code[7].Calls(members.Degree) || !Op(8, OpCodes.Ldc_I4_0) ||
            !Branch(9, OpCodes.Bgt_S, OpCodes.Bgt) || !Op(10, OpCodes.Ldarg_1) || !code[11].Calls(members.MentalState) ||
            !Branch(12, OpCodes.Brtrue_S, OpCodes.Brtrue) || !Op(13, OpCodes.Ldc_I4_0) || !Op(14, OpCodes.Ret) ||
            !Op(15, OpCodes.Ldc_I4_1) || !Op(16, OpCodes.Ret)) return "The native positive-trait/mental-state exception changed.";
        var labels = new Dictionary<Label, int>();
        for (int i = 0; i < body.Count; i++)
            foreach (var label in body[i].labels)
            {
                int next = i;
                while (next < body.Count && body[next].opcode == OpCodes.Nop) next++;
                if (labels.ContainsKey(label)) return "The direct-joy branch destination is ambiguous.";
                labels[label] = next;
            }
        foreach (int branch in new[] { 2, 9, 12 })
            if (code[branch].operand is not Label label || !labels.TryGetValue(label, out int destination) || destination != live[start + 15].index)
                return "A native exception no longer permits the same successful decision.";
        for (int i = 0; i < body.Count; i++)
        {
            var instruction = body[i];
            if (instruction.opcode == OpCodes.Starg || instruction.opcode == OpCodes.Starg_S || instruction.opcode == OpCodes.Ldarga || instruction.opcode == OpCodes.Ldarga_S)
                return "Direct-joy argument identity can be reassigned or escaped.";
            var targets = instruction.operand is Label label ? new[] { label } : instruction.operand as Label[] ?? Array.Empty<Label>();
            foreach (var target in targets)
                if (!labels.TryGetValue(target, out int destination) || destination > live[start].index && destination < live[start + 15].index)
                    return "A branch enters a partial native exception producer.";
        }
        return null;
    }
}
