using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Patching;

// Original metadata only. This reader acquires no patches and confers no publication or
// effective-composition admission; callers must prove those independently.
internal static class HarmonyOriginalBody
{
    internal enum Failure { None, UnsupportedMethod, UnsupportedProvider, UnsupportedBody, Limit, MalformedBody }
    private const int MaximumCode = 65536, MaximumOperations = 4096, MaximumLocals = 256;
    private static readonly Type RuntimeMethod = typeof(object).GetMethod(nameof(ToString))!.GetType();
    private static readonly Assembly Provider = typeof(PatchProcessor).Assembly;

    internal sealed class Snapshot
    {
        internal MethodInfo Original { get; }
        internal IReadOnlyList<Type> LocalTypes { get; }
        private readonly IReadOnlyList<CodeInstruction> instructions;
        internal Snapshot(MethodInfo original, IReadOnlyList<Type> localTypes, IReadOnlyList<CodeInstruction> instructions)
        {
            Original = original;
            LocalTypes = Array.AsReadOnly(localTypes.ToArray());
            this.instructions = Copy(instructions);
        }
        internal IReadOnlyList<CodeInstruction> CopyInstructions() => Copy(instructions);
        private static IReadOnlyList<CodeInstruction> Copy(IReadOnlyList<CodeInstruction> source) =>
            Array.AsReadOnly(source.Select(code => new CodeInstruction(code.opcode,
                code.operand is Label[] labels ? (Label[])labels.Clone() : code.operand)
            { labels = new List<Label>(code.labels), blocks = new List<ExceptionBlock>(code.blocks) }).ToArray());
    }

    internal static bool TryRead(MethodInfo original, out Snapshot? body, out Failure failure)
    {
        body = null;
        failure = Failure.UnsupportedMethod;
        if (original == null || original.GetType() != RuntimeMethod || original.DeclaringType == null ||
            original.DeclaringType.IsGenericType || original.ReflectedType != original.DeclaringType) return false;
        try
        {
            failure = Failure.UnsupportedBody;
            MethodBody? metadata = original.GetMethodBody();
            byte[]? code = metadata?.GetILAsByteArray();
            if (metadata == null || code == null || code.Length == 0 || metadata.ExceptionHandlingClauses.Count != 0) return false;
            failure = Failure.Limit;
            if (code.Length > MaximumCode || metadata.LocalVariables.Count > MaximumLocals) return false;
            if (!HarmonyPublishedBody.TryDecode(code, out var structural, out var decodeFailure))
            {
                failure = decodeFailure == HarmonyPublishedBody.Failure.Limit ? Failure.Limit : Failure.MalformedBody;
                return false;
            }
            if (structural!.Count > MaximumOperations) return false;
            failure = Failure.UnsupportedBody;
            if (structural.Any(operation => operation.Code.OperandType == OperandType.InlineSig)) return false;

            failure = Failure.UnsupportedProvider;
            Type? reader = Provider.GetType("HarmonyLib.MethodBodyReader"), instruction = Provider.GetType("HarmonyLib.ILInstruction");
            if (reader == null || instruction == null) return false;
            Type listType = typeof(List<>).MakeGenericType(instruction);
            MethodInfo? read = reader.GetMethod("GetInstructions", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null, new[] { typeof(ILGenerator), typeof(MethodBase) }, null);
            if (read == null || read.IsGenericMethod || read.ReturnType != listType) return false;
            FieldInfo Offset = Field("offset", typeof(int)), Opcode = Field("opcode", typeof(OpCode)),
                Operand = Field("operand", typeof(object)), Argument = Field("argument", typeof(object)),
                Blocks = Field("blocks", typeof(List<ExceptionBlock>));
            // Null generator avoids declaring generic locals or emitting the inspected method.
            // The exact declared method shape above excludes HandleNativeMethod's native-import
            // branch, which would instantiate attributes for a method without a reflected type.
            object? result = read.Invoke(null, new object?[] { null, original });
            if (result == null || result.GetType() != listType || result is not IList parsed || parsed.Count != structural.Count) return false;
            var items = parsed.Cast<object>().ToArray();
            var positions = new Dictionary<object, int>();
            for (int index = 0; index < items.Length; index++)
            {
                object item = items[index];
                if (item == null || item.GetType() != instruction || !positions.TryAdd(item, index) ||
                    (int)Offset.GetValue(item)! != structural[index].Offset ||
                    !Equals(Opcode.GetValue(item), structural[index].Code) ||
                    Blocks.GetValue(item) is not List<ExceptionBlock> blocks || blocks.Count != 0) return false;
            }
            var labels = new Dictionary<int, Label>();
            var generator = new DynamicMethod("MegamodOriginalLabels", typeof(void), Type.EmptyTypes).GetILGenerator();
            Label LabelFor(object target)
            {
                if (!positions.TryGetValue(target, out int index)) throw new InvalidOperationException("Unknown original branch target.");
                if (!labels.TryGetValue(index, out Label label)) labels.Add(index, label = generator.DefineLabel());
                return label;
            }
            var projected = new CodeInstruction[items.Length];
            var localTypes = metadata.LocalVariables.Select(local => local.LocalType).ToArray();
            var localShapes = new HashSet<Type>(metadata.LocalVariables.Select(local => local.GetType()));
            for (int index = 0; index < items.Length; index++)
            {
                object item = items[index];
                OpCode opcode = structural[index].Code;
                object? argument = Argument.GetValue(item);
                switch (opcode.OperandType)
                {
                    case OperandType.InlineNone: argument = null; break;
                    case OperandType.ShortInlineBrTarget: case OperandType.InlineBrTarget:
                        argument = LabelFor(Operand.GetValue(item)!); break;
                    case OperandType.InlineSwitch:
                        if (Operand.GetValue(item) is not Array targets || targets.GetType() != instruction.MakeArrayType() || targets.Length > 256)
                            return false;
                        argument = targets.Cast<object>().Select(LabelFor).ToArray(); break;
                    case OperandType.ShortInlineVar: case OperandType.InlineVar:
                        if (argument is LocalVariableInfo local)
                        {
                            if (!localShapes.Contains(local.GetType()) || local.LocalIndex < 0 || local.LocalIndex >= localTypes.Length ||
                                local.LocalType != localTypes[local.LocalIndex]) return false;
                            argument = local.LocalIndex;
                        }
                        break;
                }
                projected[index] = new CodeInstruction(opcode, argument);
            }
            foreach (var label in labels) projected[label.Key].labels.Add(label.Value);
            body = new Snapshot(original, localTypes, projected);
            failure = Failure.None;
            return true;

            FieldInfo Field(string name, Type type)
            {
                var field = instruction.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null || field.FieldType != type) throw new InvalidOperationException("Original reader provider shape changed.");
                return field;
            }
        }
        catch (Exception) { return false; }
    }
}
