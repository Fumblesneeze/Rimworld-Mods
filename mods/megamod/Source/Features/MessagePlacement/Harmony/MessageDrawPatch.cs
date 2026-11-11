using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.MessagePlacement;

public sealed record MessageDrawShape(FieldInfo MessageField, FieldInfo RectangleField, MethodInfo Callback);

public sealed class MessageDrawPatch
{
    private static readonly ConcurrentDictionary<MethodBase, MessageDrawPatch> Routes = new();
    private static readonly MethodInfo Transpiler = typeof(MessageDrawPatch).GetMethod(nameof(Transpile), BindingFlags.NonPublic | BindingFlags.Static)!;
    private readonly MethodInfo draw, calculate, register, forward;
    private readonly OwnedHarmonyPatch receipt;
    private sealed record Incoming(int Version, MessageDrawShape Shape, IReadOnlyList<HarmonyPublication.Operation> Body, ISet<int> Protected);
    private Incoming? incoming, publication;
    private bool installed;

    public MessageDrawPatch(MethodInfo draw, MethodInfo calculateRectangle, MethodInfo registerWindow, MethodInfo forward)
    {
        this.draw = draw; calculate = calculateRectangle; register = registerWindow; this.forward = forward;
        receipt = new(draw, HarmonyPatchType.Transpiler, Transpiler,
            () => ReadTranspilers().Where(patch => patch.owner == OwnedHarmonyPatch.OwnerId).ToArray());
        Candidate = new("MSG-NATIVE-DRAW", "MSG-NATIVE-DRAW", draw.ToString()!, "message placement",
            "native placement", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public MessageDrawShape? Shape => Admitted()?.Shape;
    public bool Available => Admitted() != null;

    private Incoming? Admitted()
    {
        var generation = Volatile.Read(ref incoming);
        if (!installed || generation == null) return null;
        // Ordinary recomposition revokes this immutable generation before inspecting its stream.
        if (ReferenceEquals(generation, Volatile.Read(ref publication))) return generation;
        try
        {
            bool accepted = HarmonyPublication.Inspect(draw, snapshot =>
            {
                if (snapshot.Version != generation.Version || !IsLastObserver(snapshot.Transpilers) ||
                    snapshot.FailureFor(generation.Body, generation.Protected) != null ||
                    !installed || !ReferenceEquals(generation, Volatile.Read(ref incoming))) return false;
                Volatile.Write(ref publication, generation);
                return true;
            });
            return accepted ? generation : null;
        }
        catch (Exception) { return null; }
    }

    private string? Validate()
    {
        if (!draw.IsPublic || draw.IsStatic || draw.IsGenericMethod || draw.ReturnType != typeof(void) ||
            !Parameters(draw, typeof(int), typeof(int)) || calculate.DeclaringType != draw.DeclaringType ||
            !calculate.IsPublic || calculate.IsStatic || calculate.IsGenericMethod || !calculate.ReturnType.IsValueType ||
            !Parameters(calculate, typeof(float), typeof(float)) || register.IsStatic || !register.IsPublic ||
            register.IsGenericMethod || register.ReturnType != typeof(void) || !forward.IsStatic ||
            !forward.IsPublic || forward.IsGenericMethod || forward.ReturnType != typeof(void))
            return "The native message/calculate/window callable shape changed.";
        var arguments = new[] { register.DeclaringType! }.Concat(register.GetParameters().Select(p => p.ParameterType))
            .Concat(new[] { draw.DeclaringType! }).ToArray();
        if (!Parameters(forward, arguments)) return "The observation wrapper does not exactly forward the native window arguments.";
        string? failure = null;
        bool inspected = HarmonyPublication.Inspect(draw, snapshot =>
        {
            failure = snapshot.Transpilers.Any(Factory) ? "A foreign transpiler factory prevents safe ordering inspection." :
                snapshot.Transpilers.Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId)
                    ? "The owned draw target/kind slot already contains a patch; safe rollback cannot be acquired." : null;
            return true;
        });
        return inspected ? failure : "Published message draw metadata is busy or cannot be inspected during composition.";
    }

    private void Install()
    {
        if (!Routes.TryAdd(draw, this)) throw new InvalidOperationException("The message draw route already has an owner.");
        receipt.Install();
        installed = true;
        if (!Available) throw new InvalidOperationException("The incoming captured native window rectangle is missing, ambiguous or changed after observation.");
    }

    private void Rollback()
    {
        installed = false;
        Volatile.Write(ref publication, null);
        Volatile.Write(ref incoming, null);
        try { receipt.Rollback(); }
        finally
        {
            if (Routes.TryGetValue(draw, out var current) && ReferenceEquals(current, this)) Routes.TryRemove(draw, out _);
        }
    }

    private static bool Parameters(MethodInfo method, params Type[] arguments) =>
        method.GetParameters().Select(p => p.ParameterType).SequenceEqual(arguments);

    private static bool Factory(Patch patch) => patch.PatchMethod.IsStatic &&
        (patch.PatchMethod.ReturnType == typeof(MethodInfo) || patch.PatchMethod.ReturnType == typeof(DynamicMethod)) &&
        Parameters(patch.PatchMethod, typeof(MethodBase));

    private Patch[] ReadTranspilers()
    {
        Patch[]? result = null;
        if (!HarmonyPublication.Inspect(draw, snapshot => { result = snapshot.Transpilers; return true; }))
            throw new InvalidOperationException("Published message draw metadata is busy or cannot be inspected during composition.");
        return result!;
    }

    private bool IsLastObserver(Patch[] patches)
    {
        if (patches.Any(Factory) || patches.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
            patches.Count(p => p.owner == OwnedHarmonyPatch.OwnerId && p.PatchMethod == Transpiler) != 1)
            return false;
        var ordered = PatchProcessor.GetSortedPatchMethods(draw, patches);
        return ordered.Count > 0 && ordered[ordered.Count - 1] == Transpiler;
    }

    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var incoming = instructions.ToList();
        if (!Routes.TryGetValue(__originalMethod, out var route)) return incoming;
        Volatile.Write(ref route.publication, null);
        Volatile.Write(ref route.incoming, null);
        int version;
        try { version = HarmonyPublication.ExpectedVersion(__originalMethod); }
        catch (Exception)
        {
            // Inspection failure must not block native recomposition or foreign patch removal.
            // Keep the incoming stream unchanged and this generation unadmitted.
            return incoming;
        }
        var meaningful = incoming.Where(i => i.opcode != OpCodes.Nop).ToArray();
        int measure = Array.FindIndex(meaningful, i => i.Calls(route.calculate));
        int output = Array.FindIndex(meaningful, i => i.Calls(route.register));
        if (measure < 0 || output <= measure || meaningful.Count(i => i.Calls(route.calculate)) != 1 ||
            meaningful.Count(i => i.Calls(route.register)) != 1 || measure + 1 >= meaningful.Length ||
            meaningful[measure + 1].opcode != OpCodes.Stfld || meaningful[measure + 1].operand is not FieldInfo rectangle ||
            rectangle.FieldType != route.calculate.ReturnType) return incoming;
        var messages = meaningful.Where(i => i.opcode == OpCodes.Stfld && i.operand is FieldInfo field &&
            field.DeclaringType == rectangle.DeclaringType && field.FieldType == route.draw.DeclaringType).ToArray();
        if (messages.Length != 1 || messages[0].operand is not FieldInfo message) return incoming;
        int owner = Array.IndexOf(meaningful, messages[0]);
        if (owner < 2 || meaningful[owner - 1].opcode != OpCodes.Ldarg_0 || Local(meaningful[owner - 2]) is not int captureLocal) return incoming;
        var reads = meaningful.Select((instruction, index) => (instruction, index)).Where(pair =>
            pair.instruction.opcode == OpCodes.Ldfld && Equals(pair.instruction.operand, rectangle)).ToArray();
        if (reads.Length != 1 || reads[0].index <= measure || reads[0].index >= output ||
            Local(meaningful[reads[0].index - 1]) != captureLocal) return incoming;
        var callbacks = meaningful.Skip(reads[0].index + 1).Take(output - reads[0].index - 1)
            .Where(i => i.opcode == OpCodes.Ldftn && i.operand is MethodInfo method && method.DeclaringType == rectangle.DeclaringType &&
                method.ReturnType == typeof(void) && !method.IsStatic && Parameters(method)).ToArray();
        if (callbacks.Length != 1 || callbacks[0].operand is not MethodInfo callback) return incoming;
        int function = Array.IndexOf(meaningful, callbacks[0]);
        if (function + 1 >= output || meaningful[function + 1].opcode != OpCodes.Newobj ||
            meaningful[function + 1].operand is not ConstructorInfo constructor || constructor.DeclaringType != typeof(Action)) return incoming;
        // Prove argument positions, not merely the presence of an Action somewhere on the stack.
        var parameters = route.register.GetParameters().Select(p => p.ParameterType).ToArray();
        int rectangleArgument = Array.IndexOf(parameters, rectangle.FieldType);
        int drawingActionArgument = Array.IndexOf(parameters, typeof(Action));
        if (rectangleArgument < 0 || drawingActionArgument <= rectangleArgument ||
            parameters.Count(type => type == rectangle.FieldType) != 1) return incoming;
        int cursor = reads[0].index + 1;
        for (int argument = rectangleArgument + 1; argument < parameters.Length; argument++)
        {
            if (cursor >= output) return incoming;
            if (argument == drawingActionArgument)
            {
                if (cursor + 2 >= output || Local(meaningful[cursor]) != captureLocal || cursor + 1 != function)
                    return incoming;
                cursor += 3; // same closure local, ldftn, Action constructor
            }
            else
            {
                var instruction = meaningful[cursor++];
                Type type = parameters[argument];
                bool primitive = type.IsPrimitive || type.IsEnum;
                if (!(primitive && instruction.opcode.Name!.StartsWith("ldc.", StringComparison.Ordinal)) &&
                    !(!type.IsValueType && instruction.opcode == OpCodes.Ldnull) && Local(instruction) == null &&
                    !(instruction.opcode.Name!.StartsWith("ldarg", StringComparison.Ordinal) &&
                        !instruction.opcode.Name.StartsWith("ldarga", StringComparison.Ordinal))) return incoming;
            }
        }
        if (cursor != output) return incoming;
        for (int index = reads[0].index + 1; index < output; index++)
            if (meaningful[index].labels.Count != 0 || meaningful[index].blocks.Count != 0) return incoming;
        int callIndex = incoming.IndexOf(meaningful[output]);
        if (incoming[callIndex].blocks.Count != 0) return incoming;
        var changed = incoming.Select(i => new CodeInstruction(i)).ToList();
        var instance = new CodeInstruction(OpCodes.Ldarg_0);
        instance.labels.AddRange(changed[callIndex].labels);
        changed[callIndex].labels.Clear();
        changed[callIndex].opcode = OpCodes.Call;
        changed[callIndex].operand = route.forward;
        changed.Insert(callIndex, instance);
        // Only the consumed closure/rectangle/delegate relationship and native forwarding call.
        // Runtime binding checks still establish actual window/message identity and equal values.
        Volatile.Write(ref route.incoming, new Incoming(version, new(message, rectangle, callback),
            changed.Select(instruction => new HarmonyPublication.Operation(instruction.opcode, instruction.operand)).ToArray(),
            new HashSet<int>
            {
                incoming.IndexOf(meaningful[owner]), incoming.IndexOf(meaningful[measure + 1]),
                incoming.IndexOf(meaningful[reads[0].index]), incoming.IndexOf(meaningful[function + 1]), callIndex + 1
            }));
        return changed;
    }

    private static int? Local(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Ldloc_0) return 0;
        if (instruction.opcode == OpCodes.Ldloc_1) return 1;
        if (instruction.opcode == OpCodes.Ldloc_2) return 2;
        if (instruction.opcode == OpCodes.Ldloc_3) return 3;
        if (instruction.opcode != OpCodes.Ldloc && instruction.opcode != OpCodes.Ldloc_S) return null;
        return instruction.operand is LocalBuilder local ? local.LocalIndex : Convert.ToInt32(instruction.operand);
    }
}
