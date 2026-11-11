using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugTolerance;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class DrugToleranceColumnWidthGuardTests
{
    public struct Rectangle { public float Width { get; set; } }
    public sealed class NativeColumns
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Calculate(Rectangle rect, out float addiction, out float joy, out float scheduled,
            out float icon, out float name, out float frequency, out float mood, out float recreation, out float inventory)
        {
            float variable = rect.Width - 27f - 108f;
            icon = 27f; name = variable * 0.25f;
            addiction = 36f; joy = 36f; scheduled = 36f;
            frequency = variable * 0.3f; mood = variable * 0.15f;
            recreation = variable * 0.15f; inventory = variable * 0.15f;
        }
    }
    private static readonly MethodInfo Target = typeof(NativeColumns).GetMethod(nameof(NativeColumns.Calculate))!;
    private static readonly DrugToleranceColumnWidthGuard Guard = new(typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!.GetMethod!);
    private static string? failure;
    private static string? mutation;

    [Test]
    public void NativeColumnMeaningsAreProvedFromTheActualIncomingWidthArithmetic()
    {
        var owner = new HarmonyLib.Harmony("tests.tolerance.column-width");
        mutation = null;
        failure = "Observer did not execute.";
        try
        {
            owner.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugToleranceColumnWidthGuardTests), nameof(Inspect)));
            new NativeColumns().Calculate(new Rectangle { Width = 935 }, out float a, out float b, out float c,
                out float d, out float e, out float f, out float g, out float h, out float i);
            Assert.That(new[] { a, b, c, d, e, f, g, h, i }, Is.EqualTo(new[] { 36f, 36f, 36f, 27f, 200f, 240.000015f, 120.0000076f, 120.0000076f, 120.0000076f }).Within(0.0001f));
            Assert.That(failure, Is.Null);
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }

    [TestCase("combined-subtraction", true)]
    [TestCase("forward-entry", true)]
    [TestCase("local-rectangle", true)]
    [TestCase("changed-fixed-width", false)]
    [TestCase("changed-variable-allocation", false)]
    [TestCase("output-address-swap", false)]
    [TestCase("late-alias-write", false)]
    [TestCase("branch-skips-output", false)]
    [TestCase("catastrophic-cancellation", false)]
    public void IncomingColumnArithmeticAdmitsOnlyProvedEquivalentAllocations(string change, bool expected)
    {
        var owner = new HarmonyLib.Harmony("tests.tolerance.column-width.variants");
        mutation = change; failure = "Observer did not execute.";
        try
        {
            owner.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugToleranceColumnWidthGuardTests), nameof(Inspect)));
            new NativeColumns().Calculate(new Rectangle { Width = 935 }, out float a, out float b, out float c,
                out float d, out float e, out float f, out float g, out float h, out float i);
            if (expected)
                Assert.That(new[] { a, b, c, d, e, f, g, h, i }, Is.EqualTo(new[] { 36f, 36f, 36f, 27f, 200f, 240.000015f, 120.0000076f, 120.0000076f, 120.0000076f }).Within(0.0001f));
            if (change == "catastrophic-cancellation") Assert.That(e, Is.EqualTo(-33.75f), "The real Single calculation must demonstrate the lost width.");
            Assert.That(failure == null, Is.EqualTo(expected), failure);
        }
        finally { mutation = null; owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }

    public static IEnumerable<CodeInstruction> Inspect(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var body = instructions.ToList();
        if (mutation == "combined-subtraction")
        {
            int start = body.FindIndex(i => i.opcode == OpCodes.Ldc_R4 && Equals(i.operand, 27f));
            body[start].operand = 135f;
            body.RemoveRange(start + 2, 2);
        }
        else if (mutation == "forward-entry")
        {
            Label entry = generator.DefineLabel(); body[0].labels.Add(entry);
            body.InsertRange(0, new[] { new CodeInstruction(OpCodes.Br, entry), new CodeInstruction(OpCodes.Nop) });
        }
        else if (mutation == "local-rectangle")
        {
            int getter = body.FindIndex(i => i.Calls(typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!.GetMethod!));
            var local = generator.DeclareLocal(typeof(Rectangle));
            body.RemoveAt(getter - 1);
            body.InsertRange(getter - 1, new[] { new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Stloc, local), new CodeInstruction(OpCodes.Ldloca, local) });
        }
        else if (mutation == "changed-fixed-width")
            body.First(i => i.opcode == OpCodes.Ldc_R4 && Equals(i.operand, 36f)).operand = 37f;
        else if (mutation == "changed-variable-allocation")
            body.First(i => i.opcode == OpCodes.Ldc_R4 && Equals(i.operand, 0.25f)).operand = 0.26f;
        else if (mutation == "output-address-swap")
        {
            foreach (var instruction in body)
            {
                if (instruction.opcode == OpCodes.Ldarg_2) { instruction.opcode = OpCodes.Ldarg_S; instruction.operand = (byte)5; }
                else if (instruction.opcode == OpCodes.Ldarg_S && Convert.ToInt32(instruction.operand) == 5)
                { instruction.opcode = OpCodes.Ldarg_2; instruction.operand = null; }
            }
        }
        else if (mutation == "late-alias-write")
        {
            var alias = generator.DeclareLocal(typeof(float).MakeByRefType());
            body.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldarg_2), new CodeInstruction(OpCodes.Stloc, alias) });
            body.InsertRange(body.Count - 1, new[] { new CodeInstruction(OpCodes.Ldloc, alias), new CodeInstruction(OpCodes.Ldc_R4, 99f), new CodeInstruction(OpCodes.Stind_R4) });
        }
        else if (mutation == "branch-skips-output")
        {
            Label exit = generator.DefineLabel(); body[^1].labels.Add(exit);
            body.Insert(0, new CodeInstruction(OpCodes.Br, exit));
        }
        else if (mutation == "catastrophic-cancellation")
        {
            int getter = body.FindIndex(i => i.Calls(typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!.GetMethod!));
            body.InsertRange(getter + 1, new[] { new CodeInstruction(OpCodes.Ldc_R4, 1e30f), new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Ldc_R4, 1e30f), new CodeInstruction(OpCodes.Sub) });
        }
        failure = Guard.Validate(body);
        return body;
    }
}
