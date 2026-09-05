using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace PersonalBugfixes.Exploration;

public static class ExplorationReadPatch
{
    public static readonly MethodInfo SafeRead = typeof(DiscoveryFlags).GetMethod(nameof(DiscoveryFlags.Read))!;
    public static readonly MethodInfo ListRead = typeof(List<bool>).GetProperty("Item")!.GetGetMethod()!;
    private static readonly Dictionary<MethodBase, FieldInfo> targets = new();

    public static int FindRead(IReadOnlyList<CodeInstruction> code, FieldInfo field)
    {
        int found = -1;
        for (int i = 2; i < code.Count; i++)
        {
            if (!code[i].Calls(ListRead) || code[i - 2].opcode != OpCodes.Ldfld ||
                !Equals(code[i - 2].operand, field) || !code[i - 1].IsLdloc()) continue;
            if (found != -1) return -1;
            found = i;
        }
        return found;
    }

    public static void Register(MethodBase method, FieldInfo field) => targets.Add(method, field);
    public static void Forget(MethodBase method) => targets.Remove(method);
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var code = instructions.Select(i => new CodeInstruction(i)).ToList();
        int index = FindRead(code, targets[__originalMethod]);
        if (index < 0) throw new InvalidOperationException("Expected exactly one learnedFeatures indexed read.");
        code[index].opcode = OpCodes.Call;
        code[index].operand = SafeRead;
        return code;
    }
}
