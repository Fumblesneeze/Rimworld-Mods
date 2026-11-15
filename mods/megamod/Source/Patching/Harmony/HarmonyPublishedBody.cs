using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;

namespace Megamod.Patching;

// Copies an already published nongeneric managed body. Never composes patches,
// acquires an observer, resolves a foreign module token, or claims native execution.
internal static class HarmonyPublishedBody
{
    internal enum Failure
    {
        None, InvalidRoot, Busy, NotPublished, UnsupportedProvider, UnsupportedRuntime,
        InactiveDetour, AbiGlue, UnbakedTarget, UnsupportedBody, ExceptionRegions,
        Limit, MalformedBody, ChangedDuringRead
    }
    private const int MaximumCode = 65536, MaximumSignature = 8192;
    private static readonly Assembly Provider = typeof(PatchProcessor).Assembly;
    private static readonly Assembly Runtime = typeof(DynamicMethod).Assembly;
    private static readonly Type RuntimeMethod = typeof(object).GetMethod(nameof(ToString))!.GetType();
    private sealed class CaptureFailure : Exception
    {
        internal Failure Reason { get; }
        internal CaptureFailure(Failure reason) { Reason = reason; }
    }
    internal readonly record struct Operation(int Offset, OpCode Code, object? Operand);
    internal enum TokenKind { TypeHandle, MethodHandle, FieldHandle, DynamicMethod, String, Signature }
    internal sealed record TokenBinding(TokenKind Kind, object Value, RuntimeTypeHandle? TypeContext);
    internal sealed class Snapshot
    {
        private readonly Func<bool> current;
        internal MethodInfo Original { get; }
        internal MethodInfo PublishedTarget { get; }
        internal IReadOnlyList<byte> Code { get; }
        internal IReadOnlyList<Operation> Operations { get; }
        internal IReadOnlyDictionary<int, TokenBinding> Tokens { get; }
        internal IReadOnlyList<byte> LocalSignature { get; }
        internal int MaxStack { get; }
        internal bool InitLocals { get; }
        internal bool IsPublicationCurrent() => current();
        internal Snapshot(MethodInfo original, MethodInfo target, byte[] code, byte[] locals, int stack, bool init,
            List<Operation> operations, Dictionary<int, TokenBinding> tokens, Func<bool> current)
        {
            Original = original; PublishedTarget = target; Code = Array.AsReadOnly((byte[])code.Clone()); this.current = current;
            LocalSignature = Array.AsReadOnly((byte[])locals.Clone()); MaxStack = stack; InitLocals = init;
            Operations = operations.AsReadOnly(); Tokens = new ReadOnlyDictionary<int, TokenBinding>(tokens);
        }
    }
    internal static bool TryCapture(MethodInfo original, out Snapshot? body, out Failure failure)
    {
        body = null; failure = Failure.InvalidRoot;
        if (original == null || original.GetType() != RuntimeMethod || original.IsGenericMethod ||
            original.ContainsGenericParameters || original.DeclaringType == null || original.DeclaringType.IsGenericType ||
            original.ReflectedType != original.DeclaringType) return false;
        Snapshot? captured = null;
        Failure reason = Failure.Busy;
        bool callbackEntered = false;
        try
        {
            HarmonyPublication.InspectIdentity(original, identity =>
            {
                callbackEntered = true;
                if (!identity.HasPublication) { reason = Failure.NotPublished; return true; }
                WithDetour(original, (detour, box, target) => captured = ReadBaked(original, identity, detour, box, target));
                reason = Failure.None;
                return true;
            });
        }
        catch (CaptureFailure error) { reason = error.Reason; }
        catch (InvalidOperationException) when (!callbackEntered) { reason = Failure.UnsupportedProvider; }
        body = captured; failure = reason;
        return body != null;
    }

    // This is a managed publication read, not proof of an active native entry point.
    private static void WithDetour(MethodInfo original, Action<object, object, MethodInfo> inspect)
    {
        Type tools = ProviderType("HarmonyLib.PatchTools");
        FieldInfo? field = tools.GetField("detours", BindingFlags.NonPublic | BindingFlags.Static);
        Type detourInterface = ProviderType("MonoMod.Core.ICoreDetour");
        Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(MethodBase), detourInterface);
        if (field == null || !field.IsInitOnly || field.FieldType != dictionaryType ||
            field.GetValue(null) is not IDictionary detours || detours.GetType() != dictionaryType)
            throw new CaptureFailure(Failure.UnsupportedProvider);
        Locked(detours, () =>
        {
            object? detour = detours[original];
            Type concrete = ProviderType("MonoMod.Core.Platforms.PlatformTripleDetourFactory+Detour");
            Type baseType = ProviderType("MonoMod.Core.Platforms.PlatformTripleDetourFactory+DetourBase");
            if (detour == null || detour.GetType() != concrete) throw new CaptureFailure(Failure.UnsupportedProvider);
            if (Read<bool>(detour, baseType, "disposedValue")) throw new CaptureFailure(Failure.InactiveDetour);
            object source = Read(detour, concrete, "<Source>k__BackingField", typeof(MethodBase), true)!;
            object target = Read(detour, concrete, "<Target>k__BackingField", typeof(MethodBase), true)!;
            object realTarget = Read(detour, concrete, "realTarget", typeof(MethodBase), true)!;
            if (!KnownMethod(source) || !Equals(source, original) || !KnownMethod(target) || !KnownMethod(realTarget))
                throw new CaptureFailure(Failure.UnsupportedProvider);
            if (!Equals(target, realTarget)) throw new CaptureFailure(Failure.AbiGlue);
            Type boxBase = ProviderType("MonoMod.Core.Platforms.PlatformTripleDetourFactory+DetourBase+DetourBoxBase");
            Type boxType = ProviderType("MonoMod.Core.Platforms.PlatformTripleDetourFactory+Detour+ManagedDetourBox");
            object? box = Read(detour, baseType, "DetourBox", boxBase);
            if (box == null || box.GetType() != boxType) throw new CaptureFailure(Failure.UnsupportedProvider);
            Locked(box, () =>
            {
                object sync = Read(box, boxBase, "Sync", typeof(object), true) ?? throw new CaptureFailure(Failure.UnsupportedProvider);
                Locked(sync, () =>
                {
                    if (!Read<bool>(box, boxBase, "applyDetours") || Read<bool>(box, boxBase, "isApplying") ||
                        Read(box, boxBase, "Detour", ProviderType("MonoMod.Core.Platforms.SimpleNativeDetour")) == null)
                        throw new CaptureFailure(Failure.InactiveDetour);
                    if (!Equals(Read(box, boxType, "src", typeof(MethodBase), true), source) ||
                        !Equals(Read(box, boxType, "target", typeof(MethodBase), true), realTarget))
                        throw new CaptureFailure(Failure.UnsupportedProvider);
                    inspect(detour, box, (MethodInfo)target);
                });
            });
        });
    }
    private static Snapshot ReadBaked(MethodInfo original, HarmonyPublication.Identity identity, object detour, object box, MethodInfo target)
    {
        if (target is not DynamicMethod dynamic) throw new CaptureFailure(Failure.UnsupportedBody);
        Type dynamicType = typeof(DynamicMethod);
        Type resolverType = RuntimeType("System.Reflection.Emit.DynamicResolver");
        if (Read(dynamic, dynamicType, "_methodHandle", RuntimeType("System.IRuntimeMethodInfo")) == null ||
            Read(dynamic, dynamicType, "_resolver", resolverType) is not object resolver)
            throw new CaptureFailure(Failure.UnbakedTarget);
        if (resolver.GetType() != resolverType || !ReferenceEquals(Read(resolver, resolverType, "m_method", dynamicType, true), dynamic))
            throw new CaptureFailure(Failure.UnsupportedRuntime);
        byte[] code = Bytes(Read(resolver, resolverType, "m_code", typeof(byte[]), true), 1, MaximumCode);
        byte[] locals = Bytes(Read(resolver, resolverType, "m_localSignature", typeof(byte[]), true), 0, MaximumSignature);
        var header = (byte[]?)Read(resolver, resolverType, "m_exceptionHeader", typeof(byte[]), true);
        var regions = (Array?)Read(resolver, resolverType, "m_exceptions", RuntimeType("System.Reflection.Emit.__ExceptionInfo").MakeArrayType(), true);
        if ((header?.Length ?? 0) != 0 || (regions?.Length ?? 0) != 0) throw new CaptureFailure(Failure.ExceptionRegions);
        int stack = (int)Read(resolver, resolverType, "m_stackSize", typeof(int), true)!;
        if (stack < 0 || stack > ushort.MaxValue) throw new CaptureFailure(Failure.Limit);
        bool init = Read<bool>(dynamic, dynamicType, "_initLocals");
        Type scopeType = RuntimeType("System.Reflection.Emit.DynamicScope");
        object scope = Read(resolver, resolverType, "m_scope", scopeType, true) ?? throw new CaptureFailure(Failure.UnsupportedRuntime);
        var scopeCopy = new ScopeCopy(scope, scopeType);
        byte[] copiedCode = (byte[])code.Clone();
        var tokens = new Dictionary<int, TokenBinding>();
        List<Operation> operations = Decode(copiedCode, (token, kind) => tokens[token] = scopeCopy.Bind(token, kind));
        bool SameBody() => ReferenceEquals(Read(dynamic, dynamicType, "_resolver", resolverType), resolver) &&
            ReferenceEquals(Read(resolver, resolverType, "m_code", typeof(byte[]), true), code) &&
            ReferenceEquals(Read(resolver, resolverType, "m_localSignature", typeof(byte[]), true), locals) &&
            ReferenceEquals(Read(resolver, resolverType, "m_scope", scopeType, true), scope) &&
            (int)Read(resolver, resolverType, "m_stackSize", typeof(int), true)! == stack && Read<bool>(dynamic, dynamicType, "_initLocals") == init && scopeCopy.Unchanged();
        if (!SameBody()) throw new CaptureFailure(Failure.ChangedDuringRead);
        return new Snapshot(original, target, copiedCode, locals, stack, init, operations, tokens,
            () => Current(original, identity, detour, box, target, SameBody));
    }
    private static bool Current(MethodInfo original, HarmonyPublication.Identity identity, object detour, object box,
        MethodInfo target, Func<bool> sameBody)
    {
        bool current = false, callbackEntered = false;
        try
        {
            HarmonyPublication.InspectIdentity(original, now =>
            {
                callbackEntered = true;
                if (!identity.SamePublication(now)) return true;
                WithDetour(original, (currentDetour, currentBox, currentTarget) => current =
                    ReferenceEquals(currentDetour, detour) && ReferenceEquals(currentBox, box) &&
                    ReferenceEquals(currentTarget, target) && sameBody());
                return true;
            });
        }
        catch (CaptureFailure) { return false; }
        catch (InvalidOperationException) when (!callbackEntered) { return false; }
        return current;
    }
    private sealed class ScopeCopy
    {
        private readonly object list;
        private readonly object[] items;
        private readonly int size, version;
        internal ScopeCopy(object scope, Type scopeType)
        {
            if (scope.GetType() != scopeType) throw new CaptureFailure(Failure.UnsupportedRuntime);
            FieldInfo? forwarded = scopeType.GetField("m_bodyTokens", BindingFlags.NonPublic | BindingFlags.Instance);
            if (forwarded != null && (forwarded.FieldType != scopeType || !forwarded.IsInitOnly || forwarded.GetValue(scope) != null))
                throw new CaptureFailure(Failure.UnsupportedBody);
            list = Read(scope, scopeType, "m_tokens", typeof(List<object>), true) ?? throw new CaptureFailure(Failure.UnsupportedRuntime);
            if (list.GetType() != typeof(List<object>)) throw new CaptureFailure(Failure.UnsupportedRuntime);
            items = Read<object[]>(list, typeof(List<object>), "_items");
            size = Read<int>(list, typeof(List<object>), "_size"); version = Read<int>(list, typeof(List<object>), "_version");
            if (size < 0 || size > 4096 || items == null || size > items.Length) throw new CaptureFailure(Failure.Limit);
        }
        internal bool Unchanged() => ReferenceEquals(items, Read<object[]>(list, typeof(List<object>), "_items")) &&
            size == Read<int>(list, typeof(List<object>), "_size") && version == Read<int>(list, typeof(List<object>), "_version");
        // Preserve existing runtime handles/context; resolving/approving their effects
        // belongs to a future consumer. Token methods are never invoked or compiled.
        internal TokenBinding Bind(int token, OperandType operand)
        {
            int index = token & 0xffffff;
            int table = (int)((uint)token >> 24);
            if (index <= 0 || index >= size) throw new CaptureFailure(Failure.MalformedBody);
            object? value = items[index];
            TokenBinding binding;
            if (value is RuntimeMethodHandle method) binding = new(TokenKind.MethodHandle, method, null);
            else if (value is RuntimeFieldHandle field) binding = new(TokenKind.FieldHandle, field, null);
            else if (value is RuntimeTypeHandle type) binding = new(TokenKind.TypeHandle, type, null);
            else if (value is DynamicMethod dynamic) binding = new(TokenKind.DynamicMethod, dynamic, null);
            else if (value is string text) binding = new(TokenKind.String, text, null);
            else if (value is byte[] signature) binding = new(TokenKind.Signature, Array.AsReadOnly((byte[])Bytes(signature, 0, MaximumSignature).Clone()), null);
            else if (value?.GetType() == Runtime.GetType("System.Reflection.Emit.GenericMethodInfo"))
                binding = new(TokenKind.MethodHandle, Read<RuntimeMethodHandle>(value!, value!.GetType(), "m_methodHandle"), Read<RuntimeTypeHandle>(value, value.GetType(), "m_context"));
            else if (value?.GetType() == Runtime.GetType("System.Reflection.Emit.GenericFieldInfo"))
                binding = new(TokenKind.FieldHandle, Read<RuntimeFieldHandle>(value!, value!.GetType(), "m_fieldHandle"), Read<RuntimeTypeHandle>(value, value.GetType(), "m_context"));
            else throw new CaptureFailure(Failure.UnsupportedBody);
            bool valid = operand switch
            {
                OperandType.InlineMethod => table == 6 && binding.Kind is TokenKind.MethodHandle or TokenKind.DynamicMethod,
                OperandType.InlineField => table == 4 && binding.Kind == TokenKind.FieldHandle,
                OperandType.InlineType => table == 2 && binding.Kind == TokenKind.TypeHandle,
                OperandType.InlineString => table == 0x70 && binding.Kind == TokenKind.String,
                OperandType.InlineSig => table == 0x11 && binding.Kind == TokenKind.Signature,
                OperandType.InlineTok => (table == 2 && binding.Kind == TokenKind.TypeHandle) ||
                    (table == 4 && binding.Kind == TokenKind.FieldHandle) || (table == 6 && binding.Kind is TokenKind.MethodHandle or TokenKind.DynamicMethod),
                _ => false
            };
            if (!valid) throw new CaptureFailure(Failure.MalformedBody);
            return binding;
        }
    }
    private static readonly Dictionary<short, OpCode> Opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)!)
        .Where(code => code.OpCodeType != OpCodeType.Nternal).ToDictionary(code => code.Value);
    // Structural preflight shared with the original metadata reader. This does not resolve
    // tokens, inspect a publication or relax this class's captured-body restrictions.
    internal static bool TryDecode(byte[] code, out IReadOnlyList<Operation>? operations, out Failure failure)
    {
        operations = null;
        failure = Failure.None;
        try { operations = Decode(code, (_, _) => { }).AsReadOnly(); return true; }
        catch (CaptureFailure error) { failure = error.Reason; return false; }
    }
    private static List<Operation> Decode(byte[] code, Action<int, OperandType> bind)
    {
        var result = new List<Operation>();
        int position = 0;
        while (position < code.Length)
        {
            int offset = position;
            byte first = (byte)Number(1);
            short key = first == 0xfe ? unchecked((short)(0xfe00 | Number(1))) : first;
            if (!Opcodes.TryGetValue(key, out OpCode opcode)) throw new CaptureFailure(Failure.MalformedBody);
            object? operand = null;
            switch (opcode.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineI: operand = unchecked((sbyte)Number(1)); break;
                case OperandType.InlineI: operand = unchecked((int)Number(4)); break;
                case OperandType.InlineI8: operand = unchecked((long)Number(8)); break;
                case OperandType.ShortInlineR: operand = BitConverter.Int32BitsToSingle(unchecked((int)Number(4))); break;
                case OperandType.InlineR: operand = BitConverter.Int64BitsToDouble(unchecked((long)Number(8))); break;
                case OperandType.ShortInlineVar: operand = (int)Number(1); break;
                case OperandType.InlineVar: operand = (int)Number(2); break;
                case OperandType.ShortInlineBrTarget:
                    int shortDelta = unchecked((sbyte)Number(1)); operand = Branch(shortDelta); break;
                case OperandType.InlineBrTarget:
                    int delta = unchecked((int)Number(4)); operand = Branch(delta); break;
                case OperandType.InlineSwitch:
                    uint count = (uint)Number(4);
                    if (count > 256) throw new CaptureFailure(Failure.Limit);
                    int end = position + 4 * (int)count;
                    if (end > code.Length) throw new CaptureFailure(Failure.MalformedBody);
                    var targets = new int[count];
                    for (int i = 0; i < targets.Length; i++) targets[i] = Destination(end, unchecked((int)Number(4)));
                    operand = Array.AsReadOnly(targets); break;
                case OperandType.InlineMethod: case OperandType.InlineField: case OperandType.InlineType:
                case OperandType.InlineTok: case OperandType.InlineString: case OperandType.InlineSig:
                    int token = unchecked((int)Number(4)); bind(token, opcode.OperandType); operand = token; break;
                default: throw new CaptureFailure(Failure.MalformedBody);
            }
            result.Add(new Operation(offset, opcode, operand));
        }
        var starts = new HashSet<int>(result.Select(operation => operation.Offset));
        foreach (var operation in result)
        {
            if (operation.Code.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget && !starts.Contains((int)operation.Operand!))
                throw new CaptureFailure(Failure.MalformedBody);
            if (operation.Operand is IReadOnlyList<int> targets && targets.Any(target => !starts.Contains(target)))
                throw new CaptureFailure(Failure.MalformedBody);
        }
        return result;
        ulong Number(int width)
        {
            if (width > code.Length - position) throw new CaptureFailure(Failure.MalformedBody);
            ulong value = 0;
            for (int i = 0; i < width; i++) value |= (ulong)code[position++] << (8 * i);
            return value;
        }
        int Branch(int delta) => Destination(position, delta);
        int Destination(int start, int delta)
        {
            long target = (long)start + delta;
            if (target < 0 || target >= code.Length) throw new CaptureFailure(Failure.MalformedBody);
            return (int)target;
        }
    }
    private static byte[] Bytes(object? value, int minimum, int maximum)
    {
        if (value is not byte[] bytes) throw new CaptureFailure(Failure.UnsupportedRuntime);
        if (bytes.Length < minimum || bytes.Length > maximum) throw new CaptureFailure(Failure.Limit);
        return bytes;
    }
    private static bool KnownMethod(object? value) => value is DynamicMethod || value?.GetType() == RuntimeMethod;
    private static Type ProviderType(string name) => Provider.GetType(name) ?? throw new CaptureFailure(Failure.UnsupportedProvider);
    private static Type RuntimeType(string name) => Runtime.GetType(name) ?? throw new CaptureFailure(Failure.UnsupportedRuntime);
    private static T Read<T>(object instance, Type declaring, string name) => (T)Read(instance, declaring, name, typeof(T))!;
    private static object? Read(object instance, Type declaring, string name, Type expected, bool readOnly = false)
    {
        FieldInfo? field = declaring.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        if (field == null || field.FieldType != expected || (readOnly && !field.IsInitOnly) || !declaring.IsInstanceOfType(instance))
            throw new CaptureFailure(declaring.Assembly == Provider ? Failure.UnsupportedProvider : Failure.UnsupportedRuntime);
        return field.GetValue(instance);
    }
    private static void Locked(object value, Action inspect)
    {
        if (Monitor.IsEntered(value) || !Monitor.TryEnter(value)) throw new CaptureFailure(Failure.Busy);
        try { inspect(); }
        finally { Monitor.Exit(value); }
    }
}
