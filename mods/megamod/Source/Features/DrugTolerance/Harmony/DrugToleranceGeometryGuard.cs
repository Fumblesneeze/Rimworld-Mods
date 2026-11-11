using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugTolerance;

public enum DrugToleranceGeometryStage { InitialSize, Margin, SetWindow, InnerWindow, Manager, ForwardConfig, Config, Scroll }
public sealed class DrugToleranceGeometryGuard
{
    private readonly IReadOnlyDictionary<string, MemberInfo> members;
    private readonly DrugToleranceGeometryStage stage;
    public DrugToleranceGeometryGuard(IReadOnlyDictionary<string, MemberInfo> members, DrugToleranceGeometryStage stage)
    { this.members = members; this.stage = stage; }
    internal bool HasGeometryReceiver(MemberInfo member) =>
        (member is MethodInfo method && !method.IsStatic || member is FieldInfo field && !field.IsStatic) &&
        new[] { "rectangle", "vector" }.Any(key => members.TryGetValue(key, out var constructor) && member.DeclaringType == constructor.DeclaringType);
    public (ISet<int>? Required, string? Failure) Describe(IReadOnlyList<CodeInstruction> body)
    {
        try
        {
            if (stage != DrugToleranceGeometryStage.InitialSize)
                return new DrugToleranceGeometryFlow(members, stage, body).Describe();
            var operations = Enumerable.Range(0, body.Count).Where(i => body[i].opcode != OpCodes.Nop).ToArray();
            var expected = new (OpCode Code, object? Operand)[]
            {
                (OpCodes.Call, members["screenWidth"]), (OpCodes.Ldc_I4, 50), (OpCodes.Sub, null),
                (OpCodes.Ldc_I4, 1300), (OpCodes.Call, members["minimum"]), (OpCodes.Conv_R4, null),
                (OpCodes.Ldc_R4, 720f), (OpCodes.Newobj, members["vector"]), (OpCodes.Ret, null)
            };
            var matches = Enumerable.Range(0, Math.Max(0, operations.Length - expected.Length + 1)).Where(start =>
                expected.Select((e, i) => { var actual = Normalize(body[operations[start + i]]); return actual.Code == e.Code && Equals(actual.Operand, e.Operand); }).All(value => value)).ToArray();
            if (matches.Length != 1 || body.Any(i => i.blocks.Count != 0 || i.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch) ||
                body.Count(i => i.opcode == OpCodes.Ret) != 1)
                return (null, "The physical-screen initial width no longer reaches the native size result.");
            return (operations.Skip(matches[0]).Take(expected.Length).ToHashSet(), null);
        }
        catch (Exception error) { return (null, error.GetBaseException().Message); }
    }
    private static (OpCode Code, object? Operand) Normalize(CodeInstruction instruction)
    {
        var code = instruction.opcode;
        if (code.Value >= OpCodes.Ldc_I4_0.Value && code.Value <= OpCodes.Ldc_I4_8.Value)
            return (OpCodes.Ldc_I4, code.Value - OpCodes.Ldc_I4_0.Value);
        if (code == OpCodes.Ldc_I4_S) return (OpCodes.Ldc_I4, Convert.ToInt32(instruction.operand));
        return (code, instruction.operand);
    }
}
