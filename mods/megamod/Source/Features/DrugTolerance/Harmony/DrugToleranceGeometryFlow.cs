using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugTolerance;

// Width-only abstract execution. Unknown vertical coordinates and ordinary native drawing are
// irrelevant; an unknown write/escape of a rectangle which can feed a consumer is not.
internal sealed class DrugToleranceGeometryFlow
{
    private sealed record Value(string Kind, double Number = 0, Value? X = null, Value? Width = null, string? Address = null, int Origin = -1)
    {
        internal static readonly Value Unknown = new("?"), This = new("this");
        internal static Value Constant(double value) => new("number", value);
        internal static Value WidthOf(double offset = 0) => new("width", offset);
        internal static Value Rect(Value x, Value width) => new("rect", X: x, Width: width);
        internal static Value Ref(string address) => new("ref", Address: address);
    }
    private sealed class State
    {
        internal readonly List<Value> Stack = new();
        internal readonly Dictionary<string, Value> Slots = new();
        internal State Copy() { var s = new State(); s.Stack.AddRange(Stack); foreach (var p in Slots) s.Slots.Add(p.Key, p.Value); return s; }
        internal Value Read(string key) => Slots.TryGetValue(key, out var value) ? value : Value.Unknown;
        internal Value Pop() { if (Stack.Count == 0) throw new InvalidOperationException("Unproved evaluation stack."); var value = Stack[Stack.Count - 1]; Stack.RemoveAt(Stack.Count - 1); return value; }
    }
    private readonly IReadOnlyDictionary<string, MemberInfo> members;
    private readonly DrugToleranceGeometryStage stage;
    private readonly IReadOnlyList<CodeInstruction> body;
    private readonly Dictionary<Label, int> labels = new();
    private readonly HashSet<int> required = new();
    private readonly HashSet<int> consumers = new();
    private readonly HashSet<int> geometryLocals = new();
    private readonly Dictionary<int, HashSet<int>> dependencies = new();
    private int currentIndex;
    private int executions;
    internal DrugToleranceGeometryFlow(IReadOnlyDictionary<string, MemberInfo> members, DrugToleranceGeometryStage stage, IReadOnlyList<CodeInstruction> body)
    { this.members = members; this.stage = stage; this.body = body; }
    internal (ISet<int>? Required, string? Failure) Describe()
    {
        try
        {
            if (body.Count == 0 || body.Count > 12000) throw new InvalidOperationException("Native geometry body is outside the bounded proof.");
            for (int i = 0; i < body.Count; i++) foreach (var label in body[i].labels) labels.Add(label, i);
            var initial = new State();
            initial.Slots["a0"] = stage == DrugToleranceGeometryStage.Scroll ? Value.Rect(Value.Constant(0), Value.WidthOf()) : Value.This;
            initial.Slots["a1"] = stage == DrugToleranceGeometryStage.Scroll ? Value.Ref("out") : Value.Rect(stage == DrugToleranceGeometryStage.Manager ? Value.Constant(0) : new Value("coordinate", Address: "input"), Value.WidthOf());
            initial.Slots["a2"] = Value.Ref("view");
            initial.Slots["out"] = Value.Rect(new Value("coordinate", Address: "outer"), Value.WidthOf());
            initial.Slots["view"] = Value.Rect(Value.Constant(0), Value.WidthOf());
            initial.Slots["window"] = stage == DrugToleranceGeometryStage.SetWindow ? Value.Unknown : Value.Rect(Value.Unknown, Value.WidthOf());
            int consumerSites = body.Count(i => i.operand is MethodBase method && Consumer(method));
            if ((stage is DrugToleranceGeometryStage.InnerWindow or DrugToleranceGeometryStage.Manager or DrugToleranceGeometryStage.ForwardConfig) && consumerSites != 1 ||
                stage == DrugToleranceGeometryStage.Config && consumerSites != 2)
                throw new InvalidOperationException("Native width handoffs are missing or ambiguous.");
            var pending = new Queue<(int Index, State State)>(); pending.Enqueue((0, initial));
            var seen = new Dictionary<int, List<State>>();
            while (pending.Count > 0)
            {
                var item = pending.Dequeue();
                if (++executions > 25000) throw new InvalidOperationException("Native geometry flow did not converge.");
                if (item.Index < 0 || item.Index >= body.Count) throw new InvalidOperationException("Native geometry control flow escapes its body.");
                if (!seen.TryGetValue(item.Index, out var states)) seen[item.Index] = states = new();
                if (states.Any(s => Same(s, item.State))) continue;
                // Widen unrelated loop counters, retaining every distinct consumed rectangle width.
                if (states.Count >= 16) throw new InvalidOperationException("Native geometry has too many distinct paths.");
                states.Add(item.State.Copy());
                int index = item.Index; var state = item.State; var instruction = body[index];
                if (instruction.blocks.Any(b => b.blockType is ExceptionBlockType.BeginCatchBlock or ExceptionBlockType.BeginExceptFilterBlock or ExceptionBlockType.BeginFaultBlock))
                    throw new InvalidOperationException("An exception handler became an ordinary geometry producer.");
                bool stop = Step(index, state);
                if (stop) continue;
                var code = instruction.opcode;
                if (code == OpCodes.Switch)
                { foreach (var label in (Label[])instruction.operand) pending.Enqueue((Target(label), state.Copy())); pending.Enqueue((index + 1, state)); }
                else if (code.FlowControl == FlowControl.Branch)
                { if (code == OpCodes.Leave || code == OpCodes.Leave_S) state.Stack.Clear(); int target = Target((Label)instruction.operand); WidenLoop(state, index, target); pending.Enqueue((target, state)); }
                else if (code.FlowControl == FlowControl.Cond_Branch)
                { int target = Target((Label)instruction.operand); var branch = state.Copy(); WidenLoop(branch, index, target); pending.Enqueue((target, branch)); pending.Enqueue((index + 1, state)); }
                else pending.Enqueue((index + 1, state));
            }
            if (consumers.Count == 0) throw new InvalidOperationException("No reachable native width consumer was proved.");
            // EH cannot secretly rewrite a geometry local and return to its consumer. Ordinary
            // finally disposal has no such stores; handlers which only run after the consumer are irrelevant.
            int lastConsumer = consumers.Max();
            bool handler = false;
            for (int i = 0; i < lastConsumer; i++)
            {
                foreach (var block in body[i].blocks)
                { if (block.blockType is ExceptionBlockType.BeginCatchBlock or ExceptionBlockType.BeginFinallyBlock or ExceptionBlockType.BeginFaultBlock) handler = true; if (block.blockType == ExceptionBlockType.EndExceptionBlock) handler = false; }
                if (handler && (geometryLocals.Contains(Local(body[i].opcode, body[i].operand)) || Is(body[i].operand, "windowRect") || body[i].opcode == OpCodes.Stobj))
                    throw new InvalidOperationException("An exception handler can replace geometry before its consumer.");
            }
            var pendingDependencies = new Queue<int>(required);
            while (pendingDependencies.Count != 0)
                if (dependencies.TryGetValue(pendingDependencies.Dequeue(), out var inputs))
                    foreach (int input in inputs) if (required.Add(input)) pendingDependencies.Enqueue(input);
            return (required, null);
        }
        catch (Exception error) { return (null, error.GetBaseException().Message); }
    }
    private bool Step(int index, State s)
    {
        var i = body[index]; var c = i.opcode;
        if (c == OpCodes.Nop) return false;
        currentIndex = index;
        int local = Local(c, i.operand);
        if (local >= 0)
        {
            string key = "l" + local;
            if (c.Name!.StartsWith("stloc", StringComparison.Ordinal)) s.Slots[key] = Track(s.Pop());
            else s.Stack.Add(Track(c == OpCodes.Ldloca || c == OpCodes.Ldloca_S ? Value.Ref(key) : s.Read(key)));
            if (Read(s, s.Read(key)).Kind is "rect" or "vector") geometryLocals.Add(local);
            return false;
        }
        int arg = Argument(c, i.operand);
        if (arg >= 0)
        {
            string key = "a" + arg;
            if (c == OpCodes.Starg || c == OpCodes.Starg_S) s.Slots[key] = Track(s.Pop());
            else s.Stack.Add(Track(c == OpCodes.Ldarga || c == OpCodes.Ldarga_S ? Value.Ref(key) : s.Read(key)));
            return false;
        }
        if (c == OpCodes.Ldc_R4 || c == OpCodes.Ldc_R8) { s.Stack.Add(Track(Value.Constant(Convert.ToDouble(i.operand)))); return false; }
        if (c == OpCodes.Ldc_I4 || c == OpCodes.Ldc_I4_S) { s.Stack.Add(Track(Value.Constant(Convert.ToInt32(i.operand)))); return false; }
        if (c.Value >= OpCodes.Ldc_I4_M1.Value && c.Value <= OpCodes.Ldc_I4_8.Value) { s.Stack.Add(Track(Value.Constant(c.Value - OpCodes.Ldc_I4_0.Value))); return false; }
        if (c == OpCodes.Dup) { var value = Track(s.Pop()); s.Stack.Add(value); s.Stack.Add(value); return false; }
        if (c == OpCodes.Pop) { s.Pop(); return false; }
        if (c == OpCodes.Add || c == OpCodes.Sub || c == OpCodes.Mul || c == OpCodes.Div)
        { var right = s.Pop(); var left = s.Pop(); s.Stack.Add(MathValue(c, left, right)); return false; }
        if (c == OpCodes.Conv_R4 || c == OpCodes.Conv_R8)
        { var value = s.Pop(); s.Stack.Add(Track(value.Kind == "number" && c == OpCodes.Conv_R4 ? value with { Number = (float)value.Number } : value)); return false; }
        if (c == OpCodes.Ldfld || c == OpCodes.Ldflda)
        {
            var receiver = Read(s, s.Pop());
            if (Is(i.operand, "windowRect") && receiver == Value.This) s.Stack.Add(Track(c == OpCodes.Ldflda ? Value.Ref("window") : s.Read("window")));
            else if (Is(i.operand, "vectorX") && receiver.Kind == "vector") s.Stack.Add(Track(receiver.Width!));
            else s.Stack.Add(Value.Unknown);
            return false;
        }
        if (c == OpCodes.Stfld)
        {
            var value = s.Pop(); var receiver = s.Pop();
            if (Is(i.operand, "windowRect"))
            {
                if (receiver != Value.This || stage != DrugToleranceGeometryStage.SetWindow) throw new InvalidOperationException("The native window width is overwritten before consumption.");
                RequireWidth(value, 0); s.Slots["window"] = Track(value); Protect(value.Width!); required.Add(index); consumers.Add(index);
            }
            else if (receiver.Kind == "ref" && Read(s, receiver).Kind is "rect" or "vector") throw new InvalidOperationException("A tracked geometry value field is written directly.");
            return false;
        }
        if (c == OpCodes.Ldobj) { s.Stack.Add(Read(s, s.Pop())); return false; }
        if (c == OpCodes.Stobj) { var value = s.Pop(); Write(s, s.Pop(), value); return false; }
        if (c == OpCodes.Initobj) { Write(s, s.Pop(), Value.Unknown); return false; }
        if (c.Name!.StartsWith("stind", StringComparison.Ordinal) || c == OpCodes.Cpobj || c == OpCodes.Cpblk || c == OpCodes.Initblk || c == OpCodes.Calli)
            throw new InvalidOperationException("An indirect geometry write is outside the bounded proof.");
        if (c == OpCodes.Call || c == OpCodes.Callvirt || c == OpCodes.Newobj) return Call(index, s, (MethodBase)i.operand, c == OpCodes.Newobj);
        if (c == OpCodes.Ret)
        {
            if (stage == DrugToleranceGeometryStage.SetWindow) { RequireWidth(s.Read("window"), 0); Protect(s.Read("window").Width!); }
            if (stage == DrugToleranceGeometryStage.Margin) { var value = s.Pop(); if (value.Kind != "number" || value.Number != 18) throw new InvalidOperationException("The native margin is no longer eighteen GUI units."); Protect(value); required.Add(index); consumers.Add(index); }
            if (stage == DrugToleranceGeometryStage.Scroll)
            { RequireWidth(s.Read("view"), 0, -20); RequireWidth(s.Read("out"), 0, -4); Protect(s.Read("view").Width!); Protect(s.Read("out").Width!); required.Add(index); consumers.Add(index); }
            return true;
        }
        if (c == OpCodes.Throw || c == OpCodes.Rethrow || c == OpCodes.Endfinally) return true;
        if (c == OpCodes.Leave || c == OpCodes.Leave_S || c == OpCodes.Br || c == OpCodes.Br_S) return false;
        int pops = PopCount(c.StackBehaviourPop); for (int p = 0; p < pops; p++) s.Pop();
        int pushes = PushCount(c.StackBehaviourPush); for (int p = 0; p < pushes; p++) s.Stack.Add(Value.Unknown);
        return false;
    }
    private bool Call(int index, State s, MethodBase method, bool create)
    {
        int count = method.GetParameters().Length + (!create && !method.IsStatic ? 1 : 0);
        var args = new Value[count]; for (int a = count - 1; a >= 0; a--) args[a] = s.Pop();
        Value result = Value.Unknown;
        if (Is(method, "rectangle"))
        {
            int start = create ? 0 : 1; var value = Value.Rect(args[start], args[start + 2]);
            if (create) result = value; else Write(s, args[0], value);
        }
        else if (Is(method, "atZero")) { var value = Read(s, args[0]); result = Value.Rect(Value.Constant(0), Width(value)); }
        else if (Is(method, "rounded")) { result = Read(s, args[0]); }
        else if (Is(method, "contract"))
        { var value = Read(s, args[0]); result = Value.Rect(MathValue(OpCodes.Add, X(value), args[1]), MathValue(OpCodes.Sub, Width(value), MathValue(OpCodes.Mul, args[1], Value.Constant(2)))); }
        else if (Is(method, "margin")) { result = args[0] == Value.This ? Value.Constant(18) : Value.Unknown; }
        else if (Is(method, "initial")) { result = args[0] == Value.This ? new("vector", Width: Value.WidthOf()) : Value.Unknown; }
        else if (Is(method, "width")) result = Width(Read(s, args[0]));
        else if (Is(method, "x")) result = X(Read(s, args[0]));
        else if (Is(method, "xMax")) { var value = Read(s, args[0]); result = MathValue(OpCodes.Add, X(value), Width(value)); }
        else if (Is(method, "setWidth") || Is(method, "setX") || Is(method, "setXMax"))
        {
            var value = Read(s, args[0]); var x = X(value); var width = Width(value);
            if (Is(method, "setWidth")) width = args[1]; else if (Is(method, "setX")) x = args[1]; else width = MathValue(OpCodes.Sub, args[1], x);
            Write(s, args[0], Value.Rect(x, width));
        }
        else if (Vertical(method)) { /* Vertical Rect accessors cannot change x/width. */ }
        else if (Is(method, "adjust") && stage == DrugToleranceGeometryStage.Config)
        { var value = Read(s, args[2]); Write(s, args[2], Value.Rect(X(value), MathValue(OpCodes.Sub, Width(value), Value.Constant(20)))); }
        else if (Consumer(method))
        {
            var rect = args[method.IsStatic ? 0 : 1];
            double expected = stage switch { DrugToleranceGeometryStage.InnerWindow => -36, DrugToleranceGeometryStage.Manager => -210, DrugToleranceGeometryStage.Config when Is(method, "row") => -22, _ => 0 };
            RequireWidth(rect, expected); Protect(rect.Width!); required.Add(index); consumers.Add(index);
            // Window/manager forwarding is complete at its one checked handoff. Config continues
            // through the loop, proving later iterations cannot consume a mutated view width.
            if (stage is DrugToleranceGeometryStage.InnerWindow or DrugToleranceGeometryStage.Manager or DrugToleranceGeometryStage.ForwardConfig) return true;
        }
        else
        {
            if (args.Contains(Value.This)) s.Slots["window"] = Value.Unknown;
            foreach (var argument in args)
                if (argument.Kind == "ref" && Read(s, argument).Kind is "rect" or "vector")
                    throw new InvalidOperationException("An unknown call can mutate a geometry source: " + method.Name);
        }
        if (create || method is MethodInfo info && info.ReturnType != typeof(void)) s.Stack.Add(Track(result));
        return false;
    }
    private bool Consumer(MethodBase method) => stage switch
    {
        DrugToleranceGeometryStage.InnerWindow => Is(method, "contents"),
        DrugToleranceGeometryStage.Manager => Is(method, "rightContents"),
        DrugToleranceGeometryStage.ForwardConfig => Is(method, "config"),
        DrugToleranceGeometryStage.Config => Is(method, "header") || Is(method, "row"),
        _ => false
    };
    private bool Vertical(MethodBase method) => new[] { "y", "setY", "yMin", "setYMin", "yMax", "setYMax", "height", "setHeight" }.Any(key => Is(method, key));
    private bool Is(object member, string key) => members.TryGetValue(key, out var expected) && Equals(member, expected);
    private int Target(Label label) => labels.TryGetValue(label, out int target) ? target : throw new InvalidOperationException("Unknown native branch target.");
    private Value Read(State s, Value value) => value.Kind == "ref" ? Track(s.Read(value.Address!), value) : value;
    private void Write(State s, Value address, Value value)
    { if (address.Kind != "ref") throw new InvalidOperationException("Unproved native rectangle address."); s.Slots[address.Address!] = Track(value, address); }
    private static Value Width(Value value) => value.Kind == "rect" ? value.Width! : Value.Unknown;
    private static Value X(Value value) => value.Kind == "rect" ? value.X! : Value.Unknown;
    private static void RequireWidth(Value value, params double[] offsets)
    { if (value.Kind != "rect" || value.Width!.Kind != "width" || !offsets.Contains(value.Width.Number)) throw new InvalidOperationException("A native consumer no longer receives the proved width."); }
    private Value MathValue(OpCode code, Value left, Value right) => Track(Calculate(code, left, right), left, right);
    private static Value Calculate(OpCode code, Value left, Value right)
    {
        if (left.Kind == "number" && right.Kind == "number")
        {
            // Only exact bounded integer arithmetic is folded. Fractional intermediates can
            // round at conv.r4 or a typed store even when a Double calculation cancels to18.
            if (!ExactInteger(left.Number) || !ExactInteger(right.Number)) return Value.Unknown;
            double value = code == OpCodes.Add ? left.Number + right.Number : code == OpCodes.Sub ? left.Number - right.Number : code == OpCodes.Mul ? left.Number * right.Number : right.Number == 0 ? double.NaN : left.Number / right.Number;
            return ExactInteger(value) ? Value.Constant(value) : Value.Unknown;
        }
        // The admitted deductions are small integers, exactly representable across the native
        // width range. Do not turn arbitrary floating-point cancellation into an affine proof.
        if (left.Kind == "width" && right.Kind == "number" && IntegerOffset(right.Number) && (code == OpCodes.Add || code == OpCodes.Sub))
        { double offset = left.Number + (code == OpCodes.Add ? right.Number : -right.Number); return IntegerOffset(offset) ? Value.WidthOf(offset) : Value.Unknown; }
        if (left.Kind == "number" && right.Kind == "width" && IntegerOffset(left.Number) && code == OpCodes.Add)
        { double offset = right.Number + left.Number; return IntegerOffset(offset) ? Value.WidthOf(offset) : Value.Unknown; }
        if ((left.Kind is "coordinate" or "extent") && right.Kind == "number" && (code == OpCodes.Add || code == OpCodes.Sub)) return left with { Number = left.Number + (code == OpCodes.Add ? right.Number : -right.Number) };
        if (left.Kind == "coordinate" && right.Kind == "width" && code == OpCodes.Add) return new("extent", left.Number + right.Number, Address: left.Address);
        if (left.Kind == "extent" && right.Kind == "coordinate" && left.Address == right.Address && code == OpCodes.Sub) return Value.WidthOf(left.Number - right.Number);
        return Value.Unknown;
    }
    private static bool IntegerOffset(double value) => value == Math.Truncate(value) && Math.Abs(value) <= 4096;
    private static bool ExactInteger(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value == Math.Truncate(value) && Math.Abs(value) <= 100000;
    private Value Track(Value value, params Value[] inputs)
    {
        if (value.Kind is "?" or "this") return value;
        if (value.Kind == "rect") return value with { X = Track(value.X!, inputs), Width = Track(value.Width!, inputs) };
        if (value.Kind == "vector") return value with { Width = Track(value.Width!, inputs) };
        if (!dependencies.TryGetValue(currentIndex, out var sources)) dependencies[currentIndex] = sources = new();
        if (value.Origin >= 0 && value.Origin != currentIndex) sources.Add(value.Origin);
        foreach (var input in inputs) if (input.Origin >= 0 && input.Origin != currentIndex) sources.Add(input.Origin);
        return value with { Origin = currentIndex };
    }
    private void Protect(Value value) { if (value.Origin >= 0) required.Add(value.Origin); }
    private static bool Same(State left, State right) => left.Stack.SequenceEqual(right.Stack) && left.Slots.Count == right.Slots.Count && left.Slots.All(p => right.Slots.TryGetValue(p.Key, out var value) && p.Value == value);
    private static void WidenLoop(State state, int from, int to)
    {
        if (to > from) return;
        foreach (var key in state.Slots.Keys.ToArray())
            if (key.StartsWith("l", StringComparison.Ordinal) && state.Slots[key].Kind == "number") state.Slots[key] = Value.Unknown;
    }
    private static int Local(OpCode c, object operand)
    {
        if (c.Value >= OpCodes.Ldloc_0.Value && c.Value <= OpCodes.Ldloc_3.Value) return c.Value - OpCodes.Ldloc_0.Value;
        if (c.Value >= OpCodes.Stloc_0.Value && c.Value <= OpCodes.Stloc_3.Value) return c.Value - OpCodes.Stloc_0.Value;
        if (c == OpCodes.Ldloc || c == OpCodes.Ldloc_S || c == OpCodes.Ldloca || c == OpCodes.Ldloca_S || c == OpCodes.Stloc || c == OpCodes.Stloc_S) return operand is LocalBuilder l ? l.LocalIndex : Convert.ToInt32(operand);
        return -1;
    }
    private static int Argument(OpCode c, object operand)
    {
        if (c.Value >= OpCodes.Ldarg_0.Value && c.Value <= OpCodes.Ldarg_3.Value) return c.Value - OpCodes.Ldarg_0.Value;
        if (c == OpCodes.Ldarg || c == OpCodes.Ldarg_S || c == OpCodes.Ldarga || c == OpCodes.Ldarga_S || c == OpCodes.Starg || c == OpCodes.Starg_S) return operand is ParameterInfo p ? p.Position + 1 : Convert.ToInt32(operand);
        return -1;
    }
    private static int PopCount(StackBehaviour value) => value switch
    {
        StackBehaviour.Pop0 => 0,
        StackBehaviour.Pop1 or StackBehaviour.Popi or StackBehaviour.Popref => 1,
        StackBehaviour.Pop1_pop1 or StackBehaviour.Popi_pop1 or StackBehaviour.Popi_popi or StackBehaviour.Popi_popi8 or StackBehaviour.Popi_popr4 or StackBehaviour.Popi_popr8 or StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi => 2,
        StackBehaviour.Popi_popi_popi or StackBehaviour.Popref_popi_popi or StackBehaviour.Popref_popi_popi8 or StackBehaviour.Popref_popi_popr4 or StackBehaviour.Popref_popi_popr8 or StackBehaviour.Popref_popi_popref or StackBehaviour.Popref_popi_pop1 => 3,
        _ => throw new InvalidOperationException("Unsupported native geometry stack operation.")
    };
    private static int PushCount(StackBehaviour value) => value switch { StackBehaviour.Push0 => 0, StackBehaviour.Push1 or StackBehaviour.Pushi or StackBehaviour.Pushi8 or StackBehaviour.Pushr4 or StackBehaviour.Pushr8 or StackBehaviour.Pushref => 1, _ => throw new InvalidOperationException("Unsupported native geometry stack result.") };
}
