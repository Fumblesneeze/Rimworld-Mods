using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.SettingsSearch.Harmony;

// A certificate for original instructions plus their inspected publication effects.
// Runtime getter acquisition and native call-edge acceptance remain separate responsibilities.
public static class SettingsListingBoundsAdmission
{
    private const int MaximumConsumers = 64, MaximumMethods = 1024;
    private static readonly Type RuntimeMethod = typeof(object).GetMethod(nameof(ToString))!.GetType();
    public sealed record Preservation(MethodInfo Prefix, MethodInfo Finalizer, Func<bool> IsCurrent);
    public sealed record Consumer(MethodInfo Method, SettingsListingBoundsSemantics Semantics,
        Preservation? Preservation = null);

    public sealed class Certificate
    {
        private readonly Func<bool>[] current;
        private int invalidated;
        internal Certificate(IEnumerable<Func<bool>> current) => this.current = current.ToArray();
        public bool IsCurrent()
        {
            if (Volatile.Read(ref invalidated) != 0) return false;
            try
            {
                foreach (var check in current)
                    if (!check()) { Interlocked.Exchange(ref invalidated, 1); return false; }
            }
            catch (Exception) { Interlocked.Exchange(ref invalidated, 1); return false; }
            return Volatile.Read(ref invalidated) == 0;
        }
    }

    public static bool TryCapture(IReadOnlyList<Consumer> consumers, out Certificate? certificate, out string? failure)
    {
        certificate = null;
        failure = "The listing consumer declarations are unavailable or exceed the inspection bound.";
        if (consumers == null || consumers.Count == 0 || consumers.Count > MaximumConsumers) return false;
        try
        {
            var declarations = consumers.ToArray();
            var conditions = new List<Func<bool>>();
            var originals = new Dictionary<MethodInfo, HarmonyOriginalBody.Snapshot>();
            var families = new Dictionary<MethodInfo, HarmonyPublication.FamilyIdentity>();
            var values = new ValueShapes();
            int inspectedMethods = 0;
            foreach (var consumer in declarations)
            {
                if (consumer == null || consumer.Method == null || consumer.Semantics == null) return false;
                if (consumer.Preservation != null)
                {
                    // Trusted owned guard inspection may acquire provider monitors itself. It
                    // must run outside every HarmonyPublication callback, including revalidation.
                    if (!consumer.Preservation.IsCurrent())
                    { failure = "An owned listing preservation guard is unavailable."; return false; }
                    conditions.Add(consumer.Preservation.IsCurrent);
                }
                if (!ReadOriginal(consumer.Method, out _))
                { failure = "The original listing consumer body is unavailable."; return false; }
                IEnumerable<MethodInfo> members = new[] { consumer.Method };
                if (consumer.Method.IsGenericMethod)
                {
                    MethodInfo definition = consumer.Method.GetGenericMethodDefinition();
                    if (!families.TryGetValue(definition, out var family))
                    {
                        if (!HarmonyPublication.TryCaptureFamily(definition, out family))
                        { failure = "The listing generic publication family is unavailable."; return false; }
                        families.Add(definition, family!);
                        conditions.Add(family!.IsCurrent);
                    }
                    members = new[] { definition, consumer.Method }.Concat(family!.PublishedMembers).Distinct();
                }
                foreach (var member in members)
                {
                    if (++inspectedMethods > MaximumMethods)
                    { failure = "The listing publication inspection exceeds its aggregate method bound."; return false; }
                    if (!ReadOriginal(member, out var original))
                    { failure = "An original listing family member body is unavailable."; return false; }
                    var instructions = original!.CopyInstructions();
                    var proof = consumer.Semantics.Describe(member, instructions);
                    if (proof.Failure != null || proof.Required == null)
                    { failure = proof.Failure ?? "The original listing culling relationship is unproved."; return false; }
                    var operations = instructions.Select(code => new HarmonyPublication.Operation(code.opcode, code.operand)).ToArray();
                    var arguments = proof.Required.Where(index => IsArgument(instructions[index]))
                        .Select(index => instructions[index].ArgumentIndex() - (member.IsStatic ? 0 : 1)).Where(index => index >= 0).ToHashSet();
                    string? compositionFailure = null;
                    bool inspected = HarmonyPublication.Inspect(member, snapshot =>
                    {
                        compositionFailure = CompositionFailure(snapshot, operations, arguments, member, values,
                            member == consumer.Method ? consumer.Preservation : null);
                        if (compositionFailure != null) return false;
                        conditions.Add(snapshot.Identity.IsCurrent);
                        return true;
                    });
                    if (!inspected)
                    { failure = compositionFailure ?? "The listing publication is busy or unavailable."; return false; }
                }
            }
            var result = new Certificate(conditions);
            if (!result.IsCurrent())
            { failure = "The listing composition changed during inspection."; return false; }
            certificate = result;
            failure = null;
            return true;

            bool ReadOriginal(MethodInfo method, out HarmonyOriginalBody.Snapshot? original)
            {
                if (method.GetType() != RuntimeMethod) { original = null; return false; }
                if (originals.TryGetValue(method, out original)) return true;
                if (originals.Count >= MaximumMethods || !HarmonyOriginalBody.TryRead(method, out original, out _)) return false;
                originals.Add(method, original!);
                return true;
            }
        }
        catch (Exception)
        { failure = "The listing composition metadata is unavailable or has an unproved shape."; return false; }
    }

    private static string? CompositionFailure(HarmonyPublication.Snapshot snapshot,
        IReadOnlyList<HarmonyPublication.Operation> operations, ISet<int> arguments, MethodInfo consumer, ValueShapes values,
        Preservation? preservation)
    {
        if (snapshot.Transpilers.Length != 0)
            return "A transformed listing consumer requires an effective-body proof.";
        if (preservation != null &&
            (snapshot.Prefixes.Count(patch => Known(patch, preservation.Prefix)) != 1 ||
             snapshot.Finalizers.Count(patch => Known(patch, preservation.Finalizer)) != 1))
            return "The exact owned listing preservation registrations are missing or ambiguous.";
        if (snapshot.Prefixes.Any(patch => !Accepted(patch, preservation?.Prefix, consumer, arguments, values)) ||
            snapshot.Postfixes.Any(patch => !Accepted(patch, null, consumer, arguments, values)) ||
            snapshot.Finalizers.Any(patch => !Accepted(patch, preservation?.Finalizer, consumer, arguments, values)))
            return "An outer listing callback can change an unproved consumed effect.";
        foreach (var callback in snapshot.InnerCallbacks(operations))
        {
            if (callback.IsFactory || callback.OuterBinding || !callback.HasMatches || !ConcreteObserver(callback.Method))
                return "An inner listing callback can skip, suppress or replace an unproved consumed effect.";
            foreach (int index in callback.SelectedOperations)
            {
                MethodInfo context = operations[index].Operand as MethodInfo ?? consumer;
                if (callback.Method.GetParameters().Any(parameter => !AcceptedParameter(context, parameter, null, values)))
                    return "An inner listing callback can mutate a consumed value or an unproved receiver alias.";
            }
        }
        return null;
    }
    private static bool Known(HarmonyPublication.OuterRegistration patch, MethodInfo expected) =>
        patch.Owner == OwnedHarmonyPatch.OwnerId && patch.Method == expected && !patch.IsFactory && !patch.UncheckedReferenceBinding;
    private static bool Accepted(HarmonyPublication.OuterRegistration patch, MethodInfo? owned, MethodInfo target, ISet<int> arguments,
        ValueShapes values) =>
        !patch.IsFactory && !patch.UncheckedReferenceBinding &&
        (owned != null && Known(patch, owned) || ConcreteObserver(patch.Method) &&
            patch.Method.GetParameters().All(parameter => AcceptedParameter(target, parameter, arguments, values)));
    private static bool ConcreteObserver(MethodInfo method) => method.GetType() == RuntimeMethod && method.IsStatic &&
        !method.IsGenericMethod && method.DeclaringType != null && !method.DeclaringType.IsGenericType && method.ReturnType == typeof(void);
    private static bool IsArgument(CodeInstruction code) => code.opcode == OpCodes.Ldarg || code.opcode == OpCodes.Ldarg_S ||
        code.opcode == OpCodes.Ldarga || code.opcode == OpCodes.Ldarga_S || code.opcode == OpCodes.Starg || code.opcode == OpCodes.Starg_S ||
        code.opcode == OpCodes.Ldarg_0 || code.opcode == OpCodes.Ldarg_1 || code.opcode == OpCodes.Ldarg_2 || code.opcode == OpCodes.Ldarg_3;

    private static bool AcceptedParameter(MethodInfo context, ParameterInfo parameter, ISet<int>? outerArguments, ValueShapes values)
    {
        var binding = HarmonyArgumentBinding.Resolve(context, parameter);
        bool writable = parameter.ParameterType.IsByRef || parameter.IsOut;
        if (binding.Name == "__state") return true; // Provider-private patch state is not an injected target writer.
        if (binding.Name == "__args") return false; // The payload is mutable even when declared object/interface.
        if (binding.Name is "__originalMethod" or "__originalMember" or "__runOriginal" or "__exception") return !writable;
        if (binding.Name == "__instance") return !writable && context.DeclaringType != null &&
            values.Accepts(context.DeclaringType);
        if (binding.Name is "__result" or "__resultRef") return !writable &&
            values.Accepts(context.ReturnType);
        if (binding.Index >= 0)
        {
            bool consumed = outerArguments == null || outerArguments.Contains(binding.Index);
            if (!consumed) return true;
            Type type = context.GetParameters()[binding.Index].ParameterType;
            if (type.IsByRef) type = type.GetElementType()!;
            return !writable && values.Accepts(type);
        }
        if (binding.Name.StartsWith("___", StringComparison.Ordinal))
            return !writable && values.Accepts(parameter.ParameterType);
        return false;
    }
    private sealed class ValueShapes
    {
        private const int MaximumFields = 1024, MaximumTypes = 1024, MaximumDepth = 16, MaximumFieldsPerType = 64;
        private readonly Dictionary<Type, int> heights = new();
        private readonly HashSet<Type> inspected = new();
        private int fieldsRead;
        private bool exhausted;
        internal bool Accepts(Type type) => !exhausted && Visit(type, new HashSet<Type>(), 0, out _);
        private bool Visit(Type type, ISet<Type> path, int depth, out int height)
        {
            height = 0;
            if (type == typeof(string) || type.IsPrimitive || type.IsEnum) return true;
            if (!type.IsValueType || type.IsByRef || type.ContainsGenericParameters || depth >= MaximumDepth) return false;
            // A reused subtype still contributes its complete height at this new position.
            if (heights.TryGetValue(type, out height)) return depth + height <= MaximumDepth;
            if (!path.Add(type)) return false;
            try
            {
                if (inspected.Add(type) && inspected.Count > MaximumTypes) { exhausted = true; return false; }
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fields.Length > MaximumFieldsPerType) return false;
                if (fields.Length > MaximumFields - fieldsRead) { exhausted = true; return false; }
                fieldsRead += fields.Length;
                int childHeight = 0;
                foreach (var field in fields)
                {
                    if (!Visit(field.FieldType, path, depth + 1, out int child)) return false;
                    childHeight = Math.Max(childHeight, child);
                }
                height = childHeight + 1;
                if (depth + height > MaximumDepth) return false;
                heights.Add(type, height);
                return true;
            }
            finally { path.Remove(type); }
        }
    }
}
