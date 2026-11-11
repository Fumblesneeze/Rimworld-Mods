using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;

namespace Megamod.Patching;

// Provider metadata only. Never call from a native output callback or invoke foreign patch methods.
internal static class HarmonyPublication
{
    private static readonly FieldInfo? GateField = typeof(PatchProcessor).GetField("locker", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly Type? SharedState = typeof(PatchProcessor).Assembly.GetType("HarmonyLib.HarmonySharedState");
    private static readonly Type? Serialization = typeof(PatchProcessor).Assembly.GetType("HarmonyLib.PatchInfoSerialization");
    private static readonly FieldInfo? StateField = SharedState?.GetField("state", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? HeaderField = Serialization?.GetField("infixHeader", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo? ValidateEnvelope = Serialization?.GetMethod("ValidateJsonEnvelope", BindingFlags.Static | BindingFlags.NonPublic,
        null, new[] { typeof(byte[]), typeof(int) }, null);
    private static readonly MethodInfo? ReadPayload = Serialization?.GetMethod("DeserializePayload", BindingFlags.Static | BindingFlags.NonPublic,
        null, new[] { typeof(byte[]), typeof(byte) }, null);
    private const int MaximumStateBytes = 4 * 1024 * 1024;
    private static readonly byte[] Envelope = System.Text.Encoding.ASCII.GetBytes("HARMONY-INFIX\0");
    private sealed class StateBusyException : InvalidOperationException
    {
        internal StateBusyException() : base("The provider's published metadata state is busy.") { }
    }

    private static object Gate => GateField?.FieldType == typeof(object) && GateField.IsInitOnly &&
        StateField?.FieldType == typeof(Dictionary<MethodBase, byte[]>) && StateField.IsInitOnly &&
        HeaderField?.FieldType == typeof(byte[]) && HeaderField.IsInitOnly &&
        ValidateEnvelope?.ReturnType == typeof(void) && !ValidateEnvelope.IsGenericMethod &&
        ReadPayload?.ReturnType == typeof(PatchInfo) && !ReadPayload.IsGenericMethod && GateField.GetValue(null) is object gate
        ? gate : throw new InvalidOperationException("The provider's published-generation metadata boundary changed.");
    private static PatchInfo? Read(MethodBase target) => Read(target, out _);
    private static PatchInfo? Read(MethodBase target, out byte[]? identity)
    {
        identity = null;
        if (!Monitor.IsEntered(Gate)) throw new InvalidOperationException("Published metadata requires the provider composition monitor.");
        if (StateField!.GetValue(null) is not Dictionary<MethodBase, byte[]> state ||
            HeaderField!.GetValue(null) is not byte[] header || !header.SequenceEqual(Envelope))
            throw new InvalidOperationException("The provider's published metadata state or envelope changed.");
        if (!Monitor.TryEnter(state)) throw new StateBusyException();
        byte[] bytes;
        try
        {
            if (!state.TryGetValue(target, out byte[]? stored)) return null;
            if (stored == null || stored.Length == 0 || stored.Length > MaximumStateBytes)
                throw new InvalidOperationException("Published metadata is empty or exceeds the 4 MiB inspection limit.");
            identity = stored;
            bytes = (byte[])stored.Clone();
        }
        finally { Monitor.Exit(state); }

        int version = 1;
        byte backend = 1;
        if (bytes[0] == Envelope[0])
        {
            if (bytes.Length <= Envelope.Length + 2 || !bytes.Take(Envelope.Length).SequenceEqual(Envelope))
                throw new InvalidOperationException("Published metadata has an unknown or truncated envelope.");
            version = bytes[Envelope.Length];
            backend = bytes[Envelope.Length + 1];
            if (version < 1 || version > 5 || backend != 1)
                throw new InvalidOperationException("Published metadata uses an unsupported envelope version or backend.");
            bytes = bytes.Skip(Envelope.Length + 2).ToArray();
        }
        else if (bytes[0] != (byte)'{') throw new InvalidOperationException("Published metadata is not the inspected JSON format.");
        // Provider-owned JSON validation/converters only. GetPatchInfo/Deserialize also infer an
        // infix version by constructing foreign parameter attributes, so cannot be used here.
        ValidateEnvelope!.Invoke(null, new object[] { bytes, version });
        return (PatchInfo?)ReadPayload!.Invoke(null, new object[] { bytes, backend }) ??
            throw new InvalidOperationException("Published metadata payload was null.");
    }

    internal static int ExpectedVersion(MethodBase target)
    {
        object gate = Gate;
        if (!Monitor.IsEntered(gate)) throw new InvalidOperationException("A diagnostic stream was not composed under the provider publication monitor.");
        return unchecked((Read(target)?.VersionCount ?? 0) + 1);
    }

    internal static bool Inspect(MethodBase target, Func<Snapshot, bool> inspect)
    {
        object gate = Gate;
        if (Monitor.IsEntered(gate) || !Monitor.TryEnter(gate)) return false;
        try
        {
            var metadata = Read(target, out var identity);
            return inspect(new Snapshot(metadata, new Identity(target, identity)));
        }
        catch (StateBusyException) { return false; }
        finally { Monitor.Exit(gate); }
    }
    internal static bool InspectPair(MethodBase first, MethodBase second, Func<Snapshot, Snapshot, bool> inspect)
    {
        object gate = Gate;
        if (Monitor.IsEntered(gate) || !Monitor.TryEnter(gate)) return false;
        try
        {
            var firstMetadata = Read(first, out var firstIdentity);
            var secondMetadata = Read(second, out var secondIdentity);
            return inspect(new Snapshot(firstMetadata, new Identity(first, firstIdentity)),
                new Snapshot(secondMetadata, new Identity(second, secondIdentity)));
        }
        catch (StateBusyException) { return false; }
        finally { Monitor.Exit(gate); }
    }

    internal readonly record struct Operation(OpCode Code, object? Operand);

    internal sealed class Identity
    {
        private readonly MethodBase target;
        private readonly byte[]? bytes;
        internal Identity(MethodBase target, byte[]? bytes) { this.target = target; this.bytes = bytes; }
        // The inspected provider serializes into a fresh array and replaces this dictionary entry
        // only after publishing the detour. This identity supplements a validated snapshot; it
        // never proves effective semantics and never executes/deserializes foreign metadata.
        internal bool IsCurrent()
        {
            try
            {
                object gate = Gate;
                if (Monitor.IsEntered(gate) || !Monitor.TryEnter(gate)) return false;
                try
                {
                    if (StateField!.GetValue(null) is not Dictionary<MethodBase, byte[]> state || !Monitor.TryEnter(state)) return false;
                    try
                    {
                        return state.TryGetValue(target, out var current)
                            ? bytes != null && ReferenceEquals(current, bytes)
                            : bytes == null;
                    }
                    finally { Monitor.Exit(state); }
                }
                finally { Monitor.Exit(gate); }
            }
            catch (Exception) { return false; }
        }
    }

    internal sealed class OuterRegistration
    {
        private readonly string[] before, after;
        internal MethodInfo Method { get; }
        internal string Owner { get; }
        internal int Index { get; }
        internal int Priority { get; }
        internal IReadOnlyList<string> Before { get; }
        internal IReadOnlyList<string> After { get; }
        internal bool Debug { get; }
        internal bool UncheckedReferenceBinding { get; }
        internal bool IsFactory { get; }
        // The parameterless HarmonyMethod constructor does not import callback attributes.
        // This disposable projection is for ordering/receipt checks, never registration.
        internal Patch ToOrderingPatch()
        {
            if (IsFactory) throw new InvalidOperationException("A published outer patch factory cannot be resolved during metadata inspection.");
            return new Patch(new HarmonyMethod
            {
                method = Method, priority = Priority, before = (string[])before.Clone(), after = (string[])after.Clone(),
                debug = Debug, uncheckedReferenceBinding = UncheckedReferenceBinding
            }, Index, Owner);
        }
        internal OuterRegistration(Patch patch)
        {
            Method = patch.PatchMethod;
            Owner = patch.owner;
            Index = patch.index;
            Priority = patch.priority;
            before = (string[])patch.before.Clone();
            after = (string[])patch.after.Clone();
            Before = Array.AsReadOnly(before);
            After = Array.AsReadOnly(after);
            Debug = patch.debug;
            UncheckedReferenceBinding = patch.uncheckedReferenceBinding;
            var parameters = Method.GetParameters();
            IsFactory = Method.IsStatic && (Method.ReturnType == typeof(MethodInfo) || Method.ReturnType == typeof(DynamicMethod)) &&
                parameters.Length == 1 && parameters[0].ParameterType == typeof(MethodBase);
        }
    }

    internal sealed class Snapshot
    {
        internal Identity Identity { get; }
        internal int Version { get; }
        internal Patch[] Transpilers { get; }
        internal IReadOnlyList<OuterRegistration> Prefixes { get; }
        internal IReadOnlyList<OuterRegistration> Postfixes { get; }
        internal IReadOnlyList<OuterRegistration> Finalizers { get; }
        private readonly Effect[] effects;
        internal Snapshot(PatchInfo? info, Identity identity)
        {
            Identity = identity;
            Version = info?.VersionCount ?? 0;
            Transpilers = info?.transpilers.ToArray() ?? Array.Empty<Patch>();
            Prefixes = Array.AsReadOnly((info?.prefixes ?? Array.Empty<Patch>()).Select(patch => new OuterRegistration(patch)).ToArray());
            Postfixes = Array.AsReadOnly((info?.postfixes ?? Array.Empty<Patch>()).Select(patch => new OuterRegistration(patch)).ToArray());
            Finalizers = Array.AsReadOnly((info?.finalizers ?? Array.Empty<Patch>()).Select(patch => new OuterRegistration(patch)).ToArray());
            effects = (info?.innerprefixes ?? Array.Empty<Patch>()).Select(patch => new Effect(patch, HarmonyPatchType.InnerPrefix))
                .Concat((info?.innerpostfixes ?? Array.Empty<Patch>()).Select(patch => new Effect(patch, HarmonyPatchType.InnerPostfix)))
                .Concat((info?.innerfinalizers ?? Array.Empty<Patch>()).Select(patch => new Effect(patch, HarmonyPatchType.InnerFinalizer))).ToArray();
        }
        internal string? FailureFor(IReadOnlyList<Operation> body, ISet<int> protectedOperations)
        {
            foreach (var effect in effects)
            {
                if (effect.OuterBinding) return "An inner patch binds outer reporting state; its effects are unproven.";
                int[] selected = effect.SelectedOperations(body, out bool hasMatches);
                if (!hasMatches) return "A published inner selector has no match in the inspected reporting stream.";
                if (selected.Any(protectedOperations.Contains) && !effect.Observation)
                    return "An inner patch can change a required native suppression, message or owned reporting operation.";
            }
            return null;
        }
        internal string? FailureForConversion(IReadOnlyList<Operation> body, ISet<int> requiredOperations,
            int enterIndex, int exitIndex)
        {
            string? failure = FailureFor(body, requiredOperations);
            if (failure != null) return failure;
            foreach (var effect in effects)
            {
                var selected = effect.SelectedOperations(body, out _);
                if (selected.Contains(exitIndex) && effect.Role != HarmonyPatchType.InnerPostfix)
                    return "An inner patch can interrupt conversion disposal before context restoration.";
                if (selected.Contains(enterIndex) && effect.Role != HarmonyPatchType.InnerPrefix)
                    return "An inner patch can interrupt conversion acquisition before its token reaches cleanup.";
            }
            return null;
        }
    }

    private sealed class Effect
    {
        internal readonly int[] Positions;
        internal readonly bool OuterBinding;
        internal readonly bool Observation;
        internal readonly HarmonyPatchType Role;
        private readonly InnerTargetKind kind;
        private readonly MemberInfo? member;
        private readonly object? constant;
        internal Effect(Patch patch, HarmonyPatchType role)
        {
            Role = role;
            MethodInfo callback = patch.PatchMethod; // Resolves metadata, unlike GetMethod(original).
            var parameters = callback.GetParameters();
            OuterBinding = patch.uncheckedReferenceBinding || parameters.Any(parameter => parameter.CustomAttributes.Any(
                attribute => attribute.AttributeType.FullName == "HarmonyLib.HarmonyOuter"));
            Observation = callback.ReturnType == typeof(void) && (parameters.Length == 0 ||
                (role == HarmonyPatchType.InnerPostfix && parameters.Length == 1 && parameters[0].Name == "__result" && !parameters[0].IsOut &&
                    (parameters[0].ParameterType.IsPrimitive || parameters[0].ParameterType.IsEnum)));
            if (patch.innerMethod != null && patch.innerTarget == null)
            {
                kind = InnerTargetKind.Method;
                member = patch.innerMethod.Method;
                Positions = (int[])patch.innerMethod.positions.Clone();
            }
            else if (patch.innerTarget != null && patch.innerMethod == null)
            {
                kind = patch.innerTarget.Kind;
                member = patch.innerTarget.Member;
                constant = patch.innerTarget.ConstantValue;
                Positions = (int[])patch.innerTarget.positions.Clone();
            }
            else throw new InvalidOperationException("A published inner selector has no unambiguous representation.");
        }
        internal int[] SelectedOperations(IReadOnlyList<Operation> body, out bool hasMatches)
        {
            int[] matches = Enumerable.Range(0, body.Count).Where(i => Matches(body[i])).ToArray();
            hasMatches = matches.Length != 0;
            return Positions.Length == 0 ? matches : Positions.Select(position =>
            {
                long index = position > 0 ? (long)position - 1 : (long)matches.Length + position;
                if (position == 0 || index < 0 || index >= matches.Length)
                    throw new InvalidOperationException("Published inner positions do not match the inspected reporting stream.");
                return matches[(int)index];
            }).ToArray();
        }
        internal bool Matches(Operation operation)
        {
            if (kind == InnerTargetKind.Method)
                return (operation.Code == OpCodes.Call || operation.Code == OpCodes.Callvirt) && operation.Operand is MethodInfo actual &&
                    member is MethodInfo selected && SameMember(selected, actual) &&
                    (selected.IsGenericMethodDefinition || selected.GetGenericArguments().SequenceEqual(actual.GetGenericArguments()));
            if (kind == InnerTargetKind.Constructor || kind == InnerTargetKind.FieldRead || kind == InnerTargetKind.FieldWrite)
            {
                bool opcode = kind == InnerTargetKind.Constructor ? operation.Code == OpCodes.Newobj :
                    kind == InnerTargetKind.FieldRead ? operation.Code == OpCodes.Ldfld || operation.Code == OpCodes.Ldsfld :
                    operation.Code == OpCodes.Stfld || operation.Code == OpCodes.Stsfld;
                return opcode && member != null && operation.Operand is MemberInfo actual && SameMember(member, actual);
            }
            if (kind == InnerTargetKind.Constant)
            {
                object? value = operation.Code == OpCodes.Ldstr || operation.Code == OpCodes.Ldc_I8 ||
                    operation.Code == OpCodes.Ldc_R4 || operation.Code == OpCodes.Ldc_R8 ? operation.Operand :
                    operation.Code == OpCodes.Ldc_I4_S && operation.Operand is byte unsigned ? (int)unchecked((sbyte)unsigned) :
                    operation.Code == OpCodes.Ldc_I4 || operation.Code == OpCodes.Ldc_I4_S ? Convert.ToInt32(operation.Operand) :
                    operation.Code == OpCodes.Ldc_I4_M1 ? -1 :
                    operation.Code.Value >= OpCodes.Ldc_I4_0.Value && operation.Code.Value <= OpCodes.Ldc_I4_8.Value
                        ? operation.Code.Value - OpCodes.Ldc_I4_0.Value : null;
                if (constant is float single) return value is float other && BitConverter.SingleToInt32Bits(single) == BitConverter.SingleToInt32Bits(other);
                if (constant is double real) return value is double other && BitConverter.DoubleToInt64Bits(real) == BitConverter.DoubleToInt64Bits(other);
                return Equals(constant, value);
            }
            throw new InvalidOperationException("The published inner selector kind has no reporting effect proof yet: " + kind);
        }
        private static bool SameMember(MemberInfo selected, MemberInfo actual) => selected.Module == actual.Module &&
            selected.MetadataToken == actual.MetadataToken &&
            (selected.DeclaringType?.IsGenericTypeDefinition == true || selected.DeclaringType == actual.DeclaringType);
    }
}
