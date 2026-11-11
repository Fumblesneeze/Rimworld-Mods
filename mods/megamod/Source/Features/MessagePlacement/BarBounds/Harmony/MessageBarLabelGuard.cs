using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.MessagePlacement;

public enum MessageBarLabelStage { Colonist, Position, Rectangle, ColonistGeometry, PortraitTexture, Icons, Icon, AdornmentExclusions, PortraitPaint }

// Native portrait, label and icon members, supplied without loading a game in host tests.
public sealed record MessageBarLabelMembers(IReadOnlyDictionary<string, MemberInfo> Values, IReadOnlyCollection<Type>? DrawingTypes = null);

public sealed class MessageBarLabelGuard
{
    private readonly MessageBarLabelMembers members;
    private readonly MessageBarColonistExclusionGuard? exclusions;
    public MessageBarLabelGuard(MessageBarLabelMembers members, MessageBarColonistExclusionGuard? exclusions = null)
    { this.members = members; this.exclusions = exclusions; }
    public (ISet<int>? Required, string? Failure) Describe(MessageBarLabelStage stage, IReadOnlyList<CodeInstruction> body)
    {
        if (stage == MessageBarLabelStage.AdornmentExclusions)
        {
            var exclusion = exclusions?.Describe(body) ?? (null, "Native outward-adornment eligibility has not been proved.");
            return exclusion.Item1 != null && HasUnprovedDrawing(body, exclusion.Item1) ?
                (null, "An outward drawing call is outside the proved native exclusions.") : exclusion;
        }
        if (stage == MessageBarLabelStage.ColonistGeometry)
        {
            var all = new HashSet<int>();
            var exclusion = exclusions?.Describe(body) ?? (Required: (ISet<int>?)null, Failure: "Native outward-adornment eligibility has not been proved.");
            foreach (var part in new[] { DescribeCore(MessageBarLabelStage.Colonist, body, false, true),
                DescribeCore(MessageBarLabelStage.PortraitPaint, body, false, true), exclusion })
            {
                if (part.Failure != null || part.Required == null) return part;
                all.UnionWith(part.Required);
            }
            return HasUnprovedDrawing(body, all) ? (null, "An outward drawing call is outside the measured native portrait paths.") : (all, null);
        }
        var result = DescribeCore(stage, body, false);
        return result.Failure != null && stage == MessageBarLabelStage.PortraitTexture ? DescribeCore(stage, body, true) : result;
    }
    private (ISet<int>? Required, string? Failure) DescribeCore(MessageBarLabelStage stage, IReadOnlyList<CodeInstruction> body,
        bool textureXOnStack, bool partial = false)
    {
        try
        {
            var patterns = Patterns(stage, textureXOnStack).Select(ResolveMarkers).ToArray();
            var meaningful = Enumerable.Range(0, body.Count).Where(index => body[index].opcode != OpCodes.Nop).ToArray();
            var required = new HashSet<int>();
            var starts = new HashSet<int>();
            var locals = new Dictionary<string, int>();
            var definitions = new Dictionary<string, HashSet<int>>();
            foreach (var pattern in patterns)
            {
                var matches = new List<(int Start, Dictionary<string, int> Locals)>();
                for (int start = 0; start + pattern.Length <= meaningful.Length; start++)
                {
                    var candidate = new Dictionary<string, int>(locals);
                    if (Matches(pattern, body, meaningful, start, candidate)) matches.Add((start, candidate));
                }
                if (matches.Count != 1) return (null, "Native label projection is missing or ambiguous.");
                locals = matches[0].Locals;
                starts.Add(meaningful[matches[0].Start]);
                foreach (int index in meaningful.Skip(matches[0].Start).Take(pattern.Length)) required.Add(index);
                (string Name, int Index)? constructorAddress = null;
                for (int offset = 0; offset < pattern.Length; offset++)
                {
                    var atom = pattern[offset];
                    int index = meaningful[matches[0].Start + offset];
                    void Define(string name, int at)
                    { if (!definitions.TryGetValue(name, out var writes)) definitions[name] = writes = new(); writes.Add(at); }
                    if (atom.Code == OpCodes.Stloc && atom.Operand is string stored) Define(stored, index);
                    if (atom.Code == OpCodes.Ldloca && atom.Operand is string addressed) constructorAddress = (addressed, index);
                    if (atom.Code == OpCodes.Call && atom.Operand is ConstructorInfo && constructorAddress is { } address)
                    { Define(address.Name, address.Index); constructorAddress = null; }
                }
            }
            if (stage == MessageBarLabelStage.PortraitPaint)
            {
                foreach (string key in new[] { "box", "boxSolid" })
                {
                    int consumer = Enumerable.Range(0, body.Count).Single(index => body[index].Calls((MethodInfo)members.Values[key]));
                    int argument = consumer - 1;
                    for (int remaining = ((MethodInfo)members.Values[key]).GetParameters().Length - 1; remaining > 0; remaining--)
                        argument = SkipExpression(body, argument, required);
                    while (argument >= 0 && body[argument].opcode == OpCodes.Nop) argument--;
                    if (argument < 0 || Normalize(body[argument]) != A(1)) return (null, "The native portrait box uses another rectangle.");
                    required.Add(argument); required.Add(consumer); starts.Add(argument);
                }
                int texture = Enumerable.Range(0, body.Count).Single(index => body[index].Calls((MethodInfo)members.Values["textureRect"]));
                int depth = 1, cursor = texture + 1;
                for (; cursor < body.Count && !body[cursor].Calls((MethodInfo)members.Values["drawTexture"]); cursor++)
                {
                    if (body[cursor].opcode == OpCodes.Nop) continue;
                    if (body[cursor].opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw)
                        return (null, "A branch changes the pending native portrait rectangle.");
                    var effect = StackEffect(body[cursor]);
                    if (effect.Pop >= depth) return (null, "The pending native portrait rectangle is consumed before drawing.");
                    depth += effect.Push - effect.Pop; required.Add(cursor);
                }
                if (cursor == body.Count || depth != 2 || body.Count(instruction => instruction.Calls((MethodInfo)members.Values["drawTexture"])) != 5)
                    return (null, "The actual native portrait texture consumers changed.");
                required.Add(cursor);
            }
            // New incoming jumps may not enter the middle of a protected expression. Native label
            // branches explicitly modeled by a pattern remain legal; NOPs/local numbers are irrelevant.
            foreach (int index in Enumerable.Range(0, body.Count))
            {
                var normalized = Normalize(body[index]);
                if (normalized.Code == OpCodes.Ldarga)
                {
                    int next = meaningful.FirstOrDefault(value => value > index, -1);
                    bool NativeRectangleRead() => Convert.ToInt32(normalized.Operand) == 1 && stage != MessageBarLabelStage.Position &&
                        next >= 0 && new[] { "center", "bottom", "width", "rectX", "rectY", "height", "position" }.Any(key =>
                            members.Values.TryGetValue(key, out var member) && member is MethodInfo method && body[next].Calls(method));
                    if (!NativeRectangleRead()) return (null, "The supplied native geometry address escapes its read-only rectangle getters.");
                    required.Add(index); required.Add(next);
                }
                if (!required.Contains(index) && normalized.Code == OpCodes.Stloc &&
                    locals.Where(pair => !SourceLocal(pair.Key)).Any(pair => pair.Value == Convert.ToInt32(normalized.Operand)))
                    return (null, "A native label measurement local can be overwritten before drawing.");
                if (!required.Contains(index) && normalized.Code == OpCodes.Ldloca &&
                    locals.Where(pair => !SourceLocal(pair.Key)).Any(pair => pair.Value == Convert.ToInt32(normalized.Operand)))
                    return (null, "An unproved address use can change native label measurements.");
                if (body[index].opcode == OpCodes.Starg || body[index].opcode == OpCodes.Starg_S)
                    return (null, "The supplied native label arguments are overwritten.");
                if (body[index].blocks.Count != 0 && stage != MessageBarLabelStage.Icons)
                    return (null, "Native label projection has an unproved exception region.");
                if (body[index].operand is Label label)
                {
                    int destination = Enumerable.Range(0, body.Count).SingleOrDefault(i => body[i].labels.Contains(label), -1);
                    if (destination < 0 || required.Contains(destination) && !starts.Contains(destination) && !required.Contains(index) &&
                        required.Contains(PreviousMeaningful(meaningful, destination)))
                        return (null, "A branch can bypass the native label geometry producer.");
                    required.Add(index);
                }
                else if (body[index].operand is Label[]) return (null, "A switch may bypass the native label projection.");
            }
            if (stage == MessageBarLabelStage.Icons)
            {
                var blocks = body.SelectMany((instruction, index) => instruction.blocks.Select(block => (block.blockType, index))).ToArray();
                if (blocks.Length != 3 || blocks[0].blockType != ExceptionBlockType.BeginExceptionBlock ||
                    blocks[1].blockType != ExceptionBlockType.BeginFinallyBlock || blocks[2].blockType != ExceptionBlockType.EndExceptionBlock)
                    return (null, "The icon enumerator has an unproved exception handler.");
                var final = meaningful.Where(index => index >= blocks[1].index && index <= blocks[2].index).ToArray();
                if (final.Length != 4 || Normalize(body[final[0]]).Code != OpCodes.Ldloca || body[final[1]].opcode != OpCodes.Constrained ||
                    !body[final[2]].Calls(typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))!) || body[final[3]].opcode != OpCodes.Endfinally)
                    return (null, "The icon finally handler does more than dispose its enumerator.");
                foreach (int index in final) required.Add(index);
                if (body.Count(instruction => instruction.Calls((MethodInfo)members.Values["drawIcon"])) != 1)
                    return (null, "An extra icon consumer has unproved geometry.");
            }
            foreach (var definition in definitions)
            {
                int local = locals[definition.Key];
                var reads = required.Where(index =>
                {
                    var op = Normalize(body[index]);
                    return (op.Code == OpCodes.Ldloc || op.Code == OpCodes.Ldloca) && Convert.ToInt32(op.Operand) == local;
                }).ToHashSet();
                if (CanReach(body, definition.Value, reads, false))
                    return (null, "A native label measurement can be consumed without its proven definition.");
            }
            string? mandatory = stage == MessageBarLabelStage.Colonist ? "positionLabel" :
                stage == MessageBarLabelStage.Position ? "rectangleLabel" : stage == MessageBarLabelStage.Rectangle ? "label" : null;
            if (mandatory != null)
            {
                var calls = Enumerable.Range(0, body.Count).Where(index => body[index].Calls((MethodInfo)members.Values[mandatory])).ToHashSet();
                if (calls.Count != 1 || !calls.IsSubsetOf(required) || CanReach(body, calls, new HashSet<int>(), true))
                    return (null, "A normal return can bypass the actual native label consumer.");
            }
            if (stage == MessageBarLabelStage.Icon && Enumerable.Range(0, body.Count).Any(index =>
                !required.Contains(index) && Normalize(body[index]) is { Code: var code, Operand: var operand } &&
                code == OpCodes.Ldarg && Convert.ToInt32(operand) == 2))
                return (null, "The icon position reference escapes its horizontal-only cursor update.");
            if (stage == MessageBarLabelStage.Rectangle)
            {
                var fonts = Enumerable.Range(0, body.Count).Where(index => body[index].Calls((MethodInfo)members.Values["font"])).ToArray();
                int initialFont = fonts.Single(required.Contains);
                var consumers = Enumerable.Range(0, body.Count).Where(index => body[index].Calls((MethodInfo)members.Values["label"]) ||
                    body[index].Calls((MethodInfo)members.Values["calcSize"])).ToHashSet();
                var initial = new HashSet<int> { initialFont };
                if (CanReach(body, initial, consumers, false) || fonts.Where(index => index != initialFont).Any(write =>
                    CanReach(body, new HashSet<int>(), new HashSet<int> { write }, false) &&
                    CanReach(body, initial, consumers, false, write + 1)))
                    return (null, "A font-sensitive label consumer can use a different font from its proved measurement.");
            }
            if (!partial && HasUnprovedDrawing(body, required))
                return (null, "An unproved native drawing, clip ending or GUI transform changes helper geometry.");
            return (required, null);
        }
        catch (Exception error) { return (null, error.GetBaseException().Message); }
    }

    private static bool SourceLocal(string name) => name is "alpha" or "health" or "iconItem" or "gradientColor" or "normalColor";

    private Atom[][] Patterns(MessageBarLabelStage stage, bool textureXOnStack = false)
    {
        if (stage == MessageBarLabelStage.PortraitPaint) return new[]
        {
            new[] { A(1), SF("backgroundTexture"), C("drawTexture") },
            new[] { A(1), K(2), C("contract"), S("moodFill"), LA("moodFill"), C("height"), A(2), F("needs"), F("mood"), C("level"),
                O(OpCodes.Mul), S("moodHeight"), LA("moodFill"), LA("moodFill"), C("bottom"), L("moodHeight"), O(OpCodes.Sub), C("setYMin"),
                LA("moodFill"), L("moodHeight"), C("setHeight"), L("moodFill"), SF("moodTexture"), C("drawTexture") },
            new[] { A(0), AA(1), C("position"), C("textureRect") },
            new[] { A(1), K(1), C("contract"), O(OpCodes.Dup), C("beginGroup"), C("atZero"), S("gradient"),
                LA("gradient"), LA("gradient"), C("bottom"), K(35), O(OpCodes.Sub), C("setYMin"), L("gradientColor"), C("setColor"),
                L("gradient"), SF("gradientTexture"), C("drawTexture"), L("normalColor"), C("setColor"), C("endGroup") },
            new[] { A(0), A(1), A(2), C("drawIcons") },
            new[] { A(1), SF("deadTexture"), C("drawTexture") }
        };
        if (stage == MessageBarLabelStage.Icons) return new[]
        {
            new[] { SF("iconArea"), SF("iconList"), C("iconCount"), O(OpCodes.Conv_R4), O(OpCodes.Div), SF("iconMax"), C("min"),
                A(0), C("bar"), C("scale"), O(OpCodes.Mul), S("iconSize"), LA("iconPosition"), AA(1), C("rectX"), K(1), O(OpCodes.Add),
                AA(1), C("bottom"), L("iconSize"), O(OpCodes.Sub), K(1), O(OpCodes.Sub), C("vector") },
            new[] { A(0), L("iconItem"), F("iconTexture"), LA("iconPosition"), L("iconSize"), L("iconItem"), F("iconTooltip"), C("drawIcon") }
        };
        if (stage == MessageBarLabelStage.PortraitTexture)
        {
            var pattern = new[]
        {
            A(1), F("x"), S("textureX"), A(1), F("y"), S("textureY"), SF("textureSize"),
            A(0), C("bar"), C("scale"), C("multiplyVector"), S("textureSize"),
            L("textureX"), K(1), O(OpCodes.Add), L("textureY"), L("textureSize"), F("y"),
            A(0), C("bar"), C("size"), F("y"), O(OpCodes.Sub), O(OpCodes.Sub), K(1), O(OpCodes.Sub),
            L("textureSize"), F("x"), L("textureSize"), F("y"), N("rectangle"), K(1), C("contract"), O(OpCodes.Ret)
        };
            return new[] { textureXOnStack ? pattern.Where(atom => !(atom.Operand is string name && name == "textureX" &&
                (atom.Code == OpCodes.Stloc || atom.Code == OpCodes.Ldloc))).ToArray() : pattern };
        }
        if (stage == MessageBarLabelStage.Icon) return new[] { new[]
        {
            LA("icon"), A(2), F("x"), A(2), F("y"), A(3), A(3), C("rectangle"),
            L("icon"), A(1), C("drawTexture"), A(4), B(OpCodes.Brfalse, "advance"),
            L("icon"), A(4), C("tip"), C("tipRegion"), M("advance"),
            A(2), FA("x"), O(OpCodes.Dup), O(OpCodes.Ldind_R4), A(3), O(OpCodes.Add), O(OpCodes.Stind_R4), O(OpCodes.Ret)
        }};
        if (stage == MessageBarLabelStage.Colonist) return new[] { new[]
        {
            K(4f), A(0), C("bar"), C("scale"), O(OpCodes.Mul), S("offset"), LA("position"), AA(1),
            C("center"), F("x"), AA(1), C("bottom"), L("offset"), O(OpCodes.Sub), C("vector"),
            A(2), L("position"), L("alpha"), AA(1), C("width"), A(0), C("bar"), C("space"),
            O(OpCodes.Add), K(2f), O(OpCodes.Sub), A(0), F("cache"), I(0), I(1), I(1), C("positionLabel")
        }};
        if (stage == MessageBarLabelStage.Position) return new[] { new[]
        {
            A(0), A(3), A(4), A(5), C("nameWidth"), S("nameWidth"),
            C("disableTiny"), B(OpCodes.Brtrue, "largePadding"), K(4), B(OpCodes.Br, "padding"),
            M("largePadding"), K(6), M("padding"), S("padding"),
            C("disableTiny"), B(OpCodes.Brtrue, "largeHeight"), K(12), B(OpCodes.Br, "height"),
            M("largeHeight"), K(16), M("height"), S("height"),
            LA("background"), A(1), F("x"), L("nameWidth"), K(2), O(OpCodes.Div), O(OpCodes.Sub), L("padding"), O(OpCodes.Sub),
            A(1), F("y"), L("nameWidth"), L("padding"), K(2), O(OpCodes.Mul), O(OpCodes.Add), L("height"), C("rectangle"),
            A(0), L("background"), A(2), A(3), A(4), A(5), A(6), A(7), C("rectangleLabel")
        }};
        if (stage == MessageBarLabelStage.Rectangle) return new[]
        {
            new[] { A(5), C("font"), A(0), A(3), A(4), A(5), C("getLabel"), S("label"),
                A(0), A(3), A(4), A(5), C("nameWidth"), S("nameWidth") },
            new[]
            {
                A(6), B(OpCodes.Brtrue, "background"), L("health"), K(0.999f), B(OpCodes.Bge_Un, "healthCheck"),
                M("background"), A(1), SF("textBackground"), C("drawTexture"), M("healthCheck"),
                L("health"), K(0.999f), B(OpCodes.Bge_Un, "afterHealth"), A(1), K(1), C("contract"), L("health"),
                SF("healthTexture"), SF("clearTexture"), I(0), C("fill"), O(OpCodes.Pop), M("afterHealth")
            },
            new[]
            {
                A(7), B(OpCodes.Brfalse, "left"), I(1), C("anchor"), LA("text"), AA(1), C("center"), F("x"),
                L("nameWidth"), K(2), O(OpCodes.Div), O(OpCodes.Sub), AA(1), C("rectY"), K(2), O(OpCodes.Sub),
                L("nameWidth"), K(100), C("rectangle"), B(OpCodes.Br, "draw"),
                M("left"), I(0), C("anchor"), LA("text"), AA(1), C("rectX"), K(2), O(OpCodes.Add),
                AA(1), C("center"), F("y"), L("label"), C("calcSize"), F("y"), K(2), O(OpCodes.Div), O(OpCodes.Sub),
                L("nameWidth"), K(100), C("rectangle"), M("draw"), L("text"), L("label"), C("label")
            },
            new[]
            {
                AA(1), C("center"), F("x"), L("nameWidth"), K(2), O(OpCodes.Div), O(OpCodes.Sub),
                AA(1), C("rectY"), K(11), O(OpCodes.Add), C("tiny"), B(OpCodes.Brtrue, "tiny"),
                I(3), B(OpCodes.Br, "line"), M("tiny"), I(0), M("line"), O(OpCodes.Conv_R4), O(OpCodes.Add), L("nameWidth"), C("line")
            }
        };
        throw new InvalidOperationException("This native label projection is not admitted.");
    }

    private sealed record Atom(OpCode Code, object? Operand = null);
    private sealed record Marker(string Name);
    private sealed record Destination(string Name);
    private sealed record Jump(int Index);
    private static Atom M(string label) => new(OpCodes.Nop, new Marker(label));
    private static Atom B(OpCode code, string label) => new(code, new Destination(label));
    private static Atom[] ResolveMarkers(Atom[] pattern)
    {
        var labels = new Dictionary<string, int>();
        var result = new List<Atom>();
        foreach (var atom in pattern)
            if (atom.Operand is Marker marker) labels.Add(marker.Name, result.Count); else result.Add(atom);
        return result.Select(atom => atom.Operand is Destination target ? atom with { Operand = new Jump(labels[target.Name]) } : atom).ToArray();
    }
    private static Atom O(OpCode code) => new(code);
    private static Atom K(float value) => new(OpCodes.Ldc_R4, value);
    private static Atom I(int value) => new(OpCodes.Ldc_I4, value);
    private static Atom A(int index) => new(OpCodes.Ldarg, index);
    private static Atom AA(int index) => new(OpCodes.Ldarga, index);
    private static Atom L(string name) => new(OpCodes.Ldloc, name);
    private static Atom LA(string name) => new(OpCodes.Ldloca, name);
    private static Atom S(string name) => new(OpCodes.Stloc, name);
    private Atom C(string name) => new(OpCodes.Call, members.Values[name]);
    private Atom N(string name) => new(OpCodes.Newobj, members.Values[name]);
    private Atom F(string name) => new(OpCodes.Ldfld, members.Values[name]);
    private Atom FA(string name) => new(OpCodes.Ldflda, members.Values[name]);
    private Atom SF(string name) => new(OpCodes.Ldsfld, members.Values[name]);
    private static int PreviousMeaningful(int[] indexes, int value) => indexes.LastOrDefault(index => index < value, -1);
    private bool HasUnprovedDrawing(IReadOnlyList<CodeInstruction> body, ISet<int> required) =>
        Enumerable.Range(0, body.Count).Any(index => !required.Contains(index) && body[index].operand is MethodInfo method &&
            members.DrawingTypes?.Contains(method.DeclaringType!) == true &&
            (method.Name.StartsWith("Draw", StringComparison.Ordinal) || method.Name is "Label" or "FillableBar" or "ThingIcon" or "BeginGroup" or "EndGroup" or "EndClip" or "EndScrollView" or
                "set_matrix" or "ScaleAroundPivot" or "RotateAroundPivot" || method.GetParameters().Any(parameter =>
                    parameter.ParameterType == members.Values["rectangle"].DeclaringType ||
                    parameter.ParameterType.IsByRef && parameter.ParameterType.GetElementType() == members.Values["rectangle"].DeclaringType)));
    private static int SkipExpression(IReadOnlyList<CodeInstruction> body, int end, ISet<int> required)
    {
        int needed = 1;
        while (needed > 0 && end >= 0)
        {
            var instruction = body[end];
            if (instruction.opcode == OpCodes.Nop) { end--; continue; }
            if (instruction.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw)
                throw new InvalidOperationException("An unproved branch supplies native portrait drawing arguments.");
            var effect = StackEffect(instruction);
            if (effect.Push > needed) throw new InvalidOperationException("An aliased stack value supplies native portrait drawing arguments.");
            needed += effect.Pop - effect.Push; required.Add(end--);
        }
        if (needed != 0) throw new InvalidOperationException("The native portrait drawing argument stack is incomplete.");
        return end;
    }
    private static (int Pop, int Push) StackEffect(CodeInstruction instruction)
    {
        if (instruction.operand is MethodBase method && instruction.opcode is var code &&
            (code == OpCodes.Call || code == OpCodes.Callvirt || code == OpCodes.Newobj))
            return (method.GetParameters().Length + (method.IsStatic || code == OpCodes.Newobj ? 0 : 1),
                code == OpCodes.Newobj || method is MethodInfo { ReturnType: var type } && type != typeof(void) ? 1 : 0);
        int Count(StackBehaviour behaviour) => behaviour switch
        {
            StackBehaviour.Pop0 or StackBehaviour.Push0 => 0,
            StackBehaviour.Pop1 or StackBehaviour.Popi or StackBehaviour.Popref or StackBehaviour.Push1 or StackBehaviour.Pushi or
                StackBehaviour.Pushi8 or StackBehaviour.Pushr4 or StackBehaviour.Pushr8 or StackBehaviour.Pushref => 1,
            StackBehaviour.Pop1_pop1 or StackBehaviour.Popi_pop1 or StackBehaviour.Popi_popi or StackBehaviour.Popi_popi8 or
                StackBehaviour.Popi_popr4 or StackBehaviour.Popi_popr8 or StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi or StackBehaviour.Push1_push1 => 2,
            _ => throw new InvalidOperationException("The native portrait expression has an unproved stack operation.")
        };
        return (Count(instruction.opcode.StackBehaviourPop), Count(instruction.opcode.StackBehaviourPush));
    }
    private static bool CanReach(IReadOnlyList<CodeInstruction> body, ISet<int> blocked, ISet<int> targets, bool normalReturn, int start = 0)
    {
        var labels = new Dictionary<Label, int>();
        for (int index = 0; index < body.Count; index++) foreach (var label in body[index].labels) labels.Add(label, index);
        var pending = new Stack<int>(); var visited = new HashSet<int>(); pending.Push(start);
        while (pending.Count != 0)
        {
            int index = pending.Pop();
            if (blocked.Contains(index) || !visited.Add(index)) continue;
            if (index == body.Count) { if (normalReturn) return true; continue; }
            if (targets.Contains(index)) return true;
            var op = body[index];
            if (op.opcode.FlowControl == FlowControl.Return) { if (normalReturn) return true; continue; }
            if (op.opcode.FlowControl == FlowControl.Throw) continue;
            if (op.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch)
            {
                if (op.operand is not Label label || !labels.TryGetValue(label, out int destination))
                    throw new InvalidOperationException("The native label branch graph is unproved.");
                pending.Push(destination);
                if (op.opcode.FlowControl == FlowControl.Branch) continue;
            }
            pending.Push(index + 1);
        }
        return false;
    }
    private static bool Matches(Atom[] pattern, IReadOnlyList<CodeInstruction> body, int[] meaningful,
        int start, Dictionary<string, int> locals)
    {
        for (int offset = 0; offset < pattern.Length; offset++)
        {
            var actual = Normalize(body[meaningful[start + offset]]);
            var expected = pattern[offset];
            if (actual.Code != expected.Code) return false;
            if (expected.Operand is Jump jump)
            {
                if (actual.Operand is not Label label || !body[meaningful[start + jump.Index]].labels.Contains(label)) return false;
            }
            else if (expected.Operand is string local)
            {
                int index = Convert.ToInt32(actual.Operand);
                if (locals.TryGetValue(local, out int old) && old != index) return false;
                if (!locals.ContainsKey(local) && locals.Values.Contains(index)) return false;
                locals[local] = index;
            }
            else if (!Equals(actual.Operand, expected.Operand)) return false;
        }
        return true;
    }
    private static Atom Normalize(CodeInstruction instruction)
    {
        OpCode code = instruction.opcode;
        object? operand = instruction.operand;
        if (code == OpCodes.Callvirt) code = OpCodes.Call;
        if (code == OpCodes.Brtrue_S) code = OpCodes.Brtrue;
        if (code == OpCodes.Brfalse_S) code = OpCodes.Brfalse;
        if (code == OpCodes.Br_S) code = OpCodes.Br;
        if (code == OpCodes.Bge_Un_S) code = OpCodes.Bge_Un;
        if (code.Value >= OpCodes.Ldarg_0.Value && code.Value <= OpCodes.Ldarg_3.Value)
            return new(OpCodes.Ldarg, code.Value - OpCodes.Ldarg_0.Value);
        if (code == OpCodes.Ldarg_S) code = OpCodes.Ldarg;
        if (code == OpCodes.Ldarga_S) code = OpCodes.Ldarga;
        if (code == OpCodes.Ldarg || code == OpCodes.Ldarga)
            return new(code, operand is ParameterInfo parameter ? parameter.Position + (parameter.Member is MethodInfo m && m.IsStatic ? 0 : 1) : Convert.ToInt32(operand));
        if (code.Value >= OpCodes.Ldloc_0.Value && code.Value <= OpCodes.Ldloc_3.Value)
            return new(OpCodes.Ldloc, code.Value - OpCodes.Ldloc_0.Value);
        if (code.Value >= OpCodes.Stloc_0.Value && code.Value <= OpCodes.Stloc_3.Value)
            return new(OpCodes.Stloc, code.Value - OpCodes.Stloc_0.Value);
        if (code == OpCodes.Ldloc_S) code = OpCodes.Ldloc;
        if (code == OpCodes.Ldloca_S) code = OpCodes.Ldloca;
        if (code == OpCodes.Stloc_S) code = OpCodes.Stloc;
        if (code == OpCodes.Ldloc || code == OpCodes.Ldloca || code == OpCodes.Stloc)
            return new(code, operand is LocalBuilder local ? local.LocalIndex : Convert.ToInt32(operand));
        if (code.Value >= OpCodes.Ldc_I4_0.Value && code.Value <= OpCodes.Ldc_I4_8.Value)
            return new(OpCodes.Ldc_I4, code.Value - OpCodes.Ldc_I4_0.Value);
        if (code == OpCodes.Ldc_I4_S) return new(OpCodes.Ldc_I4, Convert.ToInt32(operand));
        return new(code, operand);
    }
}
