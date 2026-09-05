using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HarmonyLib;

namespace PersonalBugfixes.Exploration;

// Executes a copy of real target IL. No process-global game property is swapped or patched.
public static class DetachedProbe
{
    private static readonly object sync = new();
    private static Scope? active;
    public static string LastDetail { get; private set; } = "not run";

    private sealed class Scope
    {
        public Scope(ExplorationTarget target) { Target = target; Fixture = target.CreateFixture(); }
        public readonly ExplorationTarget Target;
        public readonly ProbeFixture Fixture;
        public int Update = 3, Steps, Reads, Dirty;
        public bool ExpectedBounds;
    }

    public static ProbeOutcome Run(ExplorationTarget target)
    {
        lock (sync)
        {
            if (active != null) throw new InvalidOperationException("Nested probe rejected.");
            active = new Scope(target);
            try
            {
                // Snapshot includes the currently installed transpiler on the second run.
                var standin = new HarmonyMethod(Method(nameof(Standin)))
                {
                    // Harmony 2.4.1 Snapshot dereferences absent patch metadata.
                    reversePatchType = Harmony.GetPatchInfo(target.Update) == null
                        ? HarmonyReversePatchType.Original : HarmonyReversePatchType.Snapshot
                };
                var copy = Harmony.ReversePatch(target.Update, standin, Method(nameof(Sanitize)));
                copy.Invoke(null, Array.Empty<object>());
                var flags = target.Learned.GetValue(active.Fixture.Component) as List<bool>;
                if (active.Update != 0 || flags == null || flags.Count < 2 || !flags[0] || flags[1])
                {
                    LastDetail = "fixture completion or existing-flag assertion failed";
                    return ProbeOutcome.Inconclusive;
                }
                LastDetail = $"fixture passed; reads={active.Reads}; dirty={active.Dirty}; flags={flags.Count}";
                return ProbeOutcome.Healthy;
            }
            catch (Exception exception)
            {
                if (exception.GetBaseException() is ArgumentOutOfRangeException && active.ExpectedBounds)
                {
                    LastDetail = "expected learnedFeatures[2] bounds failure with 2 flags / 3 features";
                    return ProbeOutcome.BugPresent;
                }
                LastDetail = "probe rejected: " + exception.GetBaseException();
                return ProbeOutcome.Inconclusive;
            }
            finally { active = null; }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Standin() => throw new InvalidOperationException("Probe copy is not prepared.");

    private static MethodInfo Method(string name) => typeof(DetachedProbe).GetMethod(name,
        BindingFlags.Static | BindingFlags.NonPublic)!;

    private static IEnumerable<CodeInstruction> Sanitize(IEnumerable<CodeInstruction> instructions)
    {
        var scope = active ?? throw new InvalidOperationException("No probe scope.");
        var target = scope.Target;
        CodeInstruction? previous = null;
        foreach (var original in instructions)
        {
            if (original.blocks.Any(b => b.blockType == ExceptionBlockType.BeginCatchBlock ||
                b.blockType == ExceptionBlockType.BeginExceptFilterBlock || b.blockType == ExceptionBlockType.BeginFaultBlock))
                throw new InvalidOperationException("Probe catch/filter/fault handlers are unsupported; safety failures must escape.");
            var instruction = new CodeInstruction(original);
            var replacement = Rewrite(instruction, target, previous);
            previous = original;
            // Bound all loops in the copied method, including future changed control flow.
            if (instruction.opcode.FlowControl == FlowControl.Branch || instruction.opcode.FlowControl == FlowControl.Cond_Branch)
            {
                var step = new CodeInstruction(OpCodes.Call, Method(nameof(Step)));
                step.labels.AddRange(instruction.labels);
                step.blocks.AddRange(instruction.blocks);
                instruction.labels.Clear(); instruction.blocks.Clear();
                yield return step;
            }
            foreach (var item in replacement) yield return item;
        }
    }

    private static IReadOnlyList<CodeInstruction> Rewrite(CodeInstruction code, ExplorationTarget target, CodeInstruction? previous)
    {
        var op = code.opcode;
        if (op == OpCodes.Calli || op == OpCodes.Jmp || op == OpCodes.Localloc || op == OpCodes.Newarr ||
            op.Name.StartsWith("stind", StringComparison.Ordinal) || op.Name.StartsWith("ldind", StringComparison.Ordinal) ||
            op == OpCodes.Cpblk || op == OpCodes.Initblk || op == OpCodes.Throw)
            throw new InvalidOperationException("Unsupported probe opcode: " + op);

        if (code.operand is FieldInfo field)
        {
            if (field.IsStatic)
            {
                if (field == target.UpdateType && op == OpCodes.Ldsfld) return Replace(code, Method(nameof(UpdateValue)));
                if (field == target.UpdateType && op == OpCodes.Stsfld) return Replace(code, Method(nameof(SetUpdate)));
                if (field == target.RevealAll && op == OpCodes.Ldsfld) { code.opcode = OpCodes.Ldc_I4_0; code.operand = null; }
                else if (field == target.Layer && op == OpCodes.Ldsfld) { code.opcode = OpCodes.Ldnull; code.operand = null; }
                else throw new InvalidOperationException("Unknown static field in probe: " + field);
            }
            else
            {
                bool fixtureField = field == target.Learned || field == target.WorldFeatures || field == target.Features || field == target.Renderer;
                bool closureField = IsClosure(field.DeclaringType!, target) && field.FieldType == target.Feature;
                if ((!fixtureField && !closureField) || (op != OpCodes.Ldfld && !(op == OpCodes.Stfld && (field == target.Learned || closureField))))
                    throw new InvalidOperationException("Unsupported fixture field access: " + field);
            }
        }
        else if (code.operand is MethodBase method)
        {
            if (method == target.WorldGetter) return Replace(code, Method(nameof(WorldValue)).MakeGenericMethod(target.World));
            if (method == target.StateGetter)
            {
                if (!target.StateGetter.ReturnType.IsEnum || !Enum.IsDefined(target.StateGetter.ReturnType, "Playing") ||
                    Convert.ToInt32(Enum.Parse(target.StateGetter.ReturnType, "Playing")) != 2)
                    throw new InvalidOperationException("ProgramState.Playing changed.");
                code.opcode = OpCodes.Ldc_I4_2; code.operand = null;
            }
            else if (method == ExplorationReadPatch.ListRead) return Replace(code, Method(nameof(ObservedRead)));
            else if (method == ExplorationReadPatch.SafeRead) return Replace(code, Method(nameof(ObservedSafeRead)));
            else if (method == target.Tiles) return Replace(code, Method(nameof(EmptyTiles)).MakeGenericMethod(target.Feature));
            else if (method is MethodInfo info && info.DeclaringType == target.World && info.Name == "GetComponent" &&
                !info.IsStatic && info.IsGenericMethod && info.GetGenericArguments().SequenceEqual(new[] { target.Component }) &&
                info.GetParameters().Length == 0 && info.ReturnType == target.Component)
                return Replace(code, Method(nameof(ComponentValue)).MakeGenericMethod(target.World, target.Component));
            else if (method is MethodInfo dirty && dirty.DeclaringType == target.Renderer.FieldType && dirty.Name == "SetDirty" &&
                !dirty.IsStatic && dirty.ReturnType == typeof(void) && dirty.IsGenericMethod && dirty.GetParameters().Length == 1 &&
                dirty.GetParameters()[0].ParameterType == target.Layer.FieldType)
                return Replace(code, Method(nameof(IgnoreDirty)).MakeGenericMethod(target.Renderer.FieldType, target.Layer.FieldType));
            else if (op == OpCodes.Newobj && IsClosure(method.DeclaringType!, target) && method.GetParameters().Length == 0)
                return Replace(code, Method(nameof(Uninitialized)).MakeGenericMethod(method.DeclaringType!));
            else if (op == OpCodes.Ldftn && IsClosure(method.DeclaringType!, target) && method is MethodInfo lambda &&
                lambda.ReturnType == typeof(bool) && lambda.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(int) }))
            { /* Only passed to FindAll over the substituted empty Tiles list, never invoked. */ }
            else if (method.DeclaringType == typeof(Predicate<int>) && op == OpCodes.Newobj) { }
            else if (method.DeclaringType == typeof(Enumerable) && method.Name == "Repeat" && method is MethodInfo repeat &&
                repeat.GetGenericArguments().SequenceEqual(new[] { typeof(bool) }))
                return Replace(code, Method(nameof(BoundedRepeat)));
            else if (method.DeclaringType == typeof(Enumerable) && method.Name == "ToList" && method is MethodInfo toList &&
                toList.GetGenericArguments().All(t => t == typeof(int) || t == typeof(bool)))
                return Replace(code, Method(nameof(BoundedToList)).MakeGenericMethod(toList.GetGenericArguments()));
            else if (method.DeclaringType == typeof(List<bool>) && method.Name == "Add")
                return Replace(code, Method(nameof(BoundedAdd)));
            else if (method.DeclaringType == typeof(IDisposable) && method.Name == "Dispose" &&
                op == OpCodes.Callvirt && previous?.opcode == OpCodes.Constrained &&
                previous.operand is Type receiver && receiver == typeof(List<>.Enumerator).MakeGenericType(target.Feature))
            { /* Only the fixture feature-list enumerator, never arbitrary interface dispatch. */ }
            else if (!SafeCollectionCall(method, target))
                throw new InvalidOperationException("Unknown call in disposable probe: " + method.DeclaringType?.FullName + "." + method.Name);
        }
        return new[] { code };
    }

    private static bool IsClosure(Type type, ExplorationTarget target) => type.DeclaringType == target.Update.DeclaringType &&
        type.IsDefined(typeof(CompilerGeneratedAttribute), false) && type.TypeInitializer == null &&
        type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).All(f => f.FieldType == target.Feature);

    private static bool SafeCollectionCall(MethodBase method, ExplorationTarget target)
    {
        var type = method.DeclaringType!;
        if (!type.IsGenericType || !type.GetGenericArguments().All(t => t == typeof(bool) || t == typeof(int) || t == target.Feature)) return false;
        var definition = type.GetGenericTypeDefinition();
        if (definition == typeof(List<>)) return new[] { "get_Count", "set_Item", "GetEnumerator", "FindAll" }.Contains(method.Name);
        return definition == typeof(List<>.Enumerator) && new[] { "MoveNext", "get_Current", "Dispose" }.Contains(method.Name);
    }

    private static IReadOnlyList<CodeInstruction> Replace(CodeInstruction instruction, MethodInfo method)
    { instruction.opcode = OpCodes.Call; instruction.operand = method; return new[] { instruction }; }

    private static void Step() { if (++active!.Steps > 4096) throw new InvalidOperationException("Probe instruction budget exhausted."); }
    private static int UpdateValue() => active!.Update;
    private static void SetUpdate(int value) => active!.Update = value;
    private static T WorldValue<T>() => (T)active!.Fixture.World;
    private static TComponent ComponentValue<TWorld, TComponent>(TWorld world) => (TComponent)active!.Fixture.Component;
    private static T Uninitialized<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static IEnumerable<int> EmptyTiles<T>(T feature) => Array.Empty<int>();
    private static void IgnoreDirty<TRenderer, TLayer>(TRenderer renderer, TLayer layer) => active!.Dirty++;
    private static IEnumerable<bool> BoundedRepeat(bool value, int count)
    {
        if (count < 0 || count > 32) throw new InvalidOperationException("Probe allocation budget exceeded.");
        return Enumerable.Repeat(value, count).ToArray();
    }
    private static List<T> BoundedToList<T>(IEnumerable<T> source)
    {
        // Do not dispatch an arbitrary external IEnumerable implementation on an uninitialized fixture.
        if (source == null || (source.GetType() != typeof(T[]) && source.GetType() != typeof(List<T>)))
            throw new InvalidOperationException("Unknown probe enumerable rejected.");
        var collection = (ICollection<T>)source;
        if (collection.Count > 32) throw new InvalidOperationException("Probe list budget exceeded.");
        return source.ToList();
    }
    private static void BoundedAdd(List<bool> flags, bool value)
    {
        if (flags.Count >= 32) throw new InvalidOperationException("Probe list budget exceeded.");
        flags.Add(value);
    }
    private static bool ObservedRead(List<bool> flags, int index)
    {
        active!.Reads++;
        active.ExpectedBounds = ReferenceEquals(flags, active.Target.Learned.GetValue(active.Fixture.Component)) && index == 2 && flags.Count == 2;
        return flags[index];
    }
    private static bool ObservedSafeRead(List<bool> flags, int index)
    {
        active!.Reads++;
        if (index < 0 || index > 31) throw new InvalidOperationException("Probe index budget exceeded.");
        return DiscoveryFlags.Read(flags, index);
    }
}
