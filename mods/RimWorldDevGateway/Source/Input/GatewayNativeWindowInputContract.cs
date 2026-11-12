using System.Reflection;
using System.Reflection.Emit;

namespace RimWorldDevGateway;

internal readonly record struct GatewayNativeSliderState(int Slider, int Range, byte End)
{
    public bool Valid => Range == 0 ? End == 0 : Slider == 0 && End is 1 or 2;
}

// Pure callable-shape policy; inspecting it never initializes native input or Harmony.
internal static class GatewayNativeWindowInputContract
{
    public static bool CanRestoreSliderLatch(GatewayNativeSliderState owned, GatewayNativeSliderState current, bool exactWindow, bool foreignInput)
        => current == default || owned != default && owned.Valid && current == owned && exactWindow && !foreignInput;

    public static string? RejectSliderInput(bool ownsButton, GatewayNativeSliderState current, GatewayNativeSliderState owned)
        => !ownsButton ? current == default ? null : "native_slider_busy" : current.Valid && current == owned ? null : "native_slider_ownership_lost";

    public static GatewayNativeInputPoint ResolveDragPoint(GatewayNativeWindowGeometry geometry, double x, double y)
    {
        var point = geometry.ResolvePoint(x, y);
        if (point.Error != null) return point;
        float nativeX = (float)point.InputX, nativeY = (float)point.InputY;
        // Match the inspected native float ReadPhysMouse projection, including rounding at edges.
        float physicalY = (float)geometry.PixelHeight - (float)geometry.MouseOriginOffset - nativeY;
        string? error = geometry.ResolvePoint(nativeX, physicalY).Error;
        return new(error, nativeX, nativeY);
    }

    // The tiny native projection has two inspected forms: height-y and height-1-y.
    // Bind the actual mouse vector, its x/y fields and host height; unknown transforms refuse.
    public static double? MouseOriginOffset(IReadOnlyList<GatewayNativeInstructionShape> body,
        MethodInfo mousePosition, MethodInfo displayHeight, FieldInfo x, FieldInfo y, ConstructorInfo point)
    {
        if (body.Count is not (11 or 13) || body.Any(item => item.Targets.Count != 0 || item.Blocks.Count != 0)) return null;
        bool Is(int index, OpCode code, object? operand = null) => body[index].Opcode == code.Value && Equals(body[index].Operand, operand);
        object? local = body[1].Operand;
        if (local == null || !Is(0, OpCodes.Call, mousePosition) || !Is(1, OpCodes.Stloc, local) ||
            !Is(2, OpCodes.Ldloc, local) || !Is(3, OpCodes.Ldfld, x) || !Is(4, OpCodes.Ldarg, 0) ||
            !(Is(5, OpCodes.Callvirt, displayHeight) || Is(5, OpCodes.Call, displayHeight))) return null;
        int suffix = 6;
        double offset = 0;
        if (body.Count == 13)
        {
            if (!Is(6, OpCodes.Ldc_R4, 1f) || !Is(7, OpCodes.Sub)) return null;
            suffix = 8; offset = 1;
        }
        return Is(suffix, OpCodes.Ldloc, local) && Is(suffix + 1, OpCodes.Ldfld, y) &&
            Is(suffix + 2, OpCodes.Sub) && Is(suffix + 3, OpCodes.Newobj, point) && Is(suffix + 4, OpCodes.Ret)
            ? offset : null;
    }

    public static bool SupportsWindowIdentity(int nativeId, bool isImmediateWindow)
        // WindowStack.Add uses a zero-initialized counter; zero is an ordinary window ID.
        => nativeId >= 0 && !isImmediateWindow;

    public static bool PreserveReplacementHotControl(bool rejectedOwnedRelease, bool capturedWindowPresent,
        bool knownOtherWindowOwner) => rejectedOwnedRelease && !capturedWindowPresent && knownOtherWindowOwner;

    public static bool HasRequiredResetFlow(IReadOnlyList<GatewayNativeResetNode> body)
    {
        if (body.Count == 0 || body.Count(node => node.Resets) != 1 ||
            body.Count(node => node.PostReset) != 1) return false;
        var pending = new Queue<(int Index, bool Reset)>();
        var visited = new HashSet<(int, bool)>();
        pending.Enqueue((0, false));
        bool completed = false, reachedPostReset = false;
        while (pending.Count != 0)
        {
            var state = pending.Dequeue();
            if (state.Index < 0 || state.Index >= body.Count) return false;
            if (!visited.Add(state)) continue;
            GatewayNativeResetNode node = body[state.Index];
            if (node.Resets && state.Reset) return false;
            bool reset = state.Reset || node.Resets;
            if ((node.PostReset || node.Returns) && !reset) return false;
            reachedPostReset |= node.PostReset;
            completed |= node.Returns;
            if (node.Returns && node.Next.Count != 0) return false;
            foreach (int next in node.Next) pending.Enqueue((next, reset));
        }
        return completed && reachedPostReset;
    }

    public static bool SuppressDispatch(bool ownsButton, bool queuedEdge, bool rejected,
        bool stopRequested, bool injectionFailed)
        => injectionFailed || ((ownsButton || queuedEdge) && (rejected || stopRequested));

    public static string? RejectInput(bool ownsLeft, bool leftPressed, bool mouseActivity,
        bool keyboardActivity, bool scrollActivity, int hotControl, int ownedHotControl)
    {
        if (mouseActivity || keyboardActivity || scrollActivity ||
            hotControl != (ownsLeft ? ownedHotControl : 0)) return "native_input_busy";
        if (ownsLeft && !leftPressed) return "native_input_ownership_lost";
        return !ownsLeft && leftPressed ? "native_input_busy" : null;
    }

    public static bool HasQueues(Type inputSystem, Type vector)
        => Matches(inputSystem.GetMethod("QueueMouseMove", new[] { vector }), inputSystem, true,
               typeof(void), new[] { vector }) &&
           Matches(inputSystem.GetMethod("QueueMouseButton", new[] { typeof(int), typeof(bool), vector }),
               inputSystem, true, typeof(void), new[] { typeof(int), typeof(bool), vector });

    public static MethodInfo? ResolveFrameReset(Type inputSystem)
    {
        MethodInfo? reset = inputSystem.GetMethod("BeginFrame", BindingFlags.Public | BindingFlags.Static |
            BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
        return Matches(reset, inputSystem, true, typeof(void), Type.EmptyTypes) ? reset : null;
    }

    public static MethodInfo? ResolveFrameResetBoundary(Type bootstrap, Type application)
    {
        MethodInfo? boundary = bootstrap.GetMethod("OnPrePollEvents", BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.DeclaredOnly, null, new[] { application }, null);
        return boundary != null && boundary.DeclaringType == bootstrap && boundary.IsPrivate &&
            boundary.IsStatic && !boundary.IsGenericMethod && !boundary.ContainsGenericParameters &&
            boundary.ReturnType == typeof(void) && boundary.GetParameters().Select(p => p.ParameterType)
                .SequenceEqual(new[] { application }) ? boundary : null;
    }

    public static MethodInfo? ResolveDispatch(Type dispatcher, Type host)
    {
        MethodInfo? height = host.GetProperty("DisplayHeight", BindingFlags.Public | BindingFlags.Instance |
            BindingFlags.DeclaredOnly)?.GetGetMethod();
        if (!Matches(height, host, false, typeof(float), Type.EmptyTypes)) return null;
        MethodInfo? dispatch = dispatcher.GetMethod("DispatchInput", BindingFlags.Public | BindingFlags.Instance |
            BindingFlags.DeclaredOnly, null, new[] { host }, null);
        return Matches(dispatch, dispatcher, false, typeof(void), new[] { host }) ? dispatch : null;
    }

    public static MethodInfo? ResolveEventDispatch(Type dispatcher, Type vector)
    {
        MethodInfo? method = dispatcher.GetMethod("Dispatch", BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.DeclaredOnly, null, new[] { vector, typeof(bool) }, null);
        return method != null && method.IsPrivate && !method.IsStatic && !method.IsGenericMethod &&
            !method.ContainsGenericParameters && method.ReturnType == typeof(void) ? method : null;
    }

    internal static bool Matches(MethodInfo? method, Type owner, bool isStatic, Type result, Type[] parameters)
        => method != null && method.DeclaringType == owner && method.IsPublic && method.IsStatic == isStatic &&
           !method.IsGenericMethod && !method.ContainsGenericParameters && method.ReturnType == result &&
           method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameters);
}

internal sealed class GatewayNativeResetNode
{
    public GatewayNativeResetNode(IReadOnlyList<int> next, bool resets = false, bool postReset = false, bool returns = false)
    { Next = next; Resets = resets; PostReset = postReset; Returns = returns; }
    public IReadOnlyList<int> Next { get; }
    public bool Resets { get; }
    public bool PostReset { get; }
    public bool Returns { get; }
}

// Per-outer-dispatch receipt: mouse delivery is decided after layout, not by the outer call.
internal sealed class GatewayNativeMousePassGate
{
    private bool seen;
    private bool admitted;
    public bool Seen => seen;
    public bool Admitted => admitted;
    public bool OriginalObserved { get; private set; }
    public string? Rejection { get; private set; }
    public bool Dispatched { get; private set; }
    // True preserves an unrelated pass; false rejects this invocation; null requires owned validation.
    public static bool? BeforeProvenance(bool exactActiveFrame, bool provenAuthority, bool providerAvailable,
        GatewayNativeMouseProvenance? provenance)
    {
        if (exactActiveFrame && (!provenAuthority || !providerAvailable)) return false;
        return provenance?.Owned == true ? null : true;
    }
    public bool BeforePass(bool originalWillRun, string? rejection, bool stopRequested, bool terminalPrefix = true)
    {
        if (seen) return false;
        seen = true;
        Rejection = rejection;
        admitted = terminalPrefix && originalWillRun && rejection == null && !stopRequested;
        return admitted;
    }
    public void AfterPass(bool originalRan)
    {
        OriginalObserved |= originalRan;
        Dispatched |= admitted && originalRan;
    }
    public void ApplyReleaseTail(bool ownedRelease, int value, Action<int> nativeSetter)
    {
        if (ownedRelease && !Dispatched && value == 0) return;
        nativeSetter(value);
    }
}

internal sealed class GatewayNativeMouseProvenance
{
    private readonly object input;
    private readonly bool drawing;
    private readonly int button, type, rawType;
    private readonly double x, y;
    public bool Owned { get; }
    public GatewayNativeMouseProvenance(bool owned, object input, bool drawing, int button, int type, int rawType,
        double x, double y)
    { Owned = owned; this.input = input; this.drawing = drawing; this.button = button;
      this.type = type; this.rawType = rawType; this.x = x; this.y = y; }
    public bool Matches(object? currentInput, bool currentDrawing, int currentButton, int currentType,
        int currentRawType, double currentX, double currentY)
        => ReferenceEquals(input, currentInput) && drawing == currentDrawing && button == currentButton &&
           type == currentType && rawType == currentRawType && x == currentX && y == currentY;
}

internal sealed class GatewayNativeInstructionShape
{
    public GatewayNativeInstructionShape(short opcode, object? operand, IReadOnlyList<int> targets,
        IReadOnlyList<(int Kind, Type? CatchType)> blocks)
    { Opcode = opcode; Operand = operand; Targets = targets; Blocks = blocks; }
    public short Opcode { get; }
    public object? Operand { get; }
    public IReadOnlyList<int> Targets { get; }
    public IReadOnlyList<(int Kind, Type? CatchType)> Blocks { get; }
    public static bool SameBody(IReadOnlyList<GatewayNativeInstructionShape> original,
        IReadOnlyList<GatewayNativeInstructionShape> effective)
        => original.Count == effective.Count && original.Select((instruction, index) =>
            instruction.Opcode == effective[index].Opcode && Equals(instruction.Operand, effective[index].Operand) &&
            instruction.Targets.SequenceEqual(effective[index].Targets) &&
            instruction.Blocks.SequenceEqual(effective[index].Blocks)).All(equal => equal);
}

// A stream is not final merely because our observer ran. Bind its proof to the next immutable
// Harmony metadata publication and the provider's actual metadata-only sorted order.
internal sealed class GatewayNativeResetAdmission
{
    private readonly object? previousPublication;
    private readonly bool streamSupported;
    private object? acceptedPublication;

    public GatewayNativeResetAdmission(object? previousPublication, bool streamSupported)
    { this.previousPublication = previousPublication; this.streamSupported = streamSupported; }

    public bool IsAccepted(object? publication)
        => publication != null && ReferenceEquals(publication, acceptedPublication);

    public bool TryAccept(object? publication, IReadOnlyList<MethodInfo> orderedMethods, MethodInfo observer,
        int innerPrefixCount = 0, int innerPostfixCount = 0, int innerFinalizerCount = 0)
    {
        // Harmony 3 rewrites inner calls after all transpilers. Their effect is outside this
        // stream proof, including a bool inner prefix that skips the reset call itself.
        if (innerPrefixCount != 0 || innerPostfixCount != 0 || innerFinalizerCount != 0) return false;
        if (IsAccepted(publication)) return true;
        if (!streamSupported || publication == null || acceptedPublication != null ||
            ReferenceEquals(publication, previousPublication) || orderedMethods.Count == 0 ||
            orderedMethods.Any(IsFactory) ||
            orderedMethods[orderedMethods.Count - 1] != observer || orderedMethods.Count(method => method == observer) != 1)
            return false;
        acceptedPublication = publication;
        return true;
    }

    // Exact Harmony 3 factory declaration. Metadata inspection must never invoke this method.
    public static bool IsFactory(MethodInfo method)
        => method.IsStatic && (method.ReturnType == typeof(MethodInfo) ||
            method.ReturnType == typeof(System.Reflection.Emit.DynamicMethod)) &&
           method.GetParameters().Select(parameter => parameter.ParameterType)
               .SequenceEqual(new[] { typeof(MethodBase) });
}

// Receipt for the raw transient flags produced by our own queue calls. A later frame's edge,
// or an opposite/repeated edge that we did not queue, must never inherit that ownership.
internal sealed class GatewayNativeInputEdgeReceipt
{
    private int? generation;
    private bool ownedDown;
    private bool ownedUp;

    public void Invalidate()
    {
        generation = null;
        ownedDown = ownedUp = false;
    }

    public void Record(int frameGeneration, bool pressed)
    {
        if (generation != frameGeneration) ownedDown = ownedUp = false;
        generation = frameGeneration;
        if (pressed) ownedDown = true;
        else ownedUp = true;
    }

    public bool HasForeignEdges(int frameGeneration, bool down, bool up, bool repeat)
        => repeat || (down && (generation != frameGeneration || !ownedDown)) ||
           (up && (generation != frameGeneration || !ownedUp));
}

// Retain the click core's exact cleanup receipt while distinguishing a provider abort from
// successful dispatch. The provider polls this at native completion to freeze the outcome.
internal sealed class GatewayNativeInputResultOperation : IGatewayDeferredAutomationOperation
{
    private readonly IGatewayDeferredAutomationOperation inner;
    private readonly Func<string?> providerFailure;
    private GatewayDeferredAutomationResult? terminal;

    public GatewayNativeInputResultOperation(IGatewayDeferredAutomationOperation inner, Func<string?> providerFailure)
    {
        this.inner = inner;
        this.providerFailure = providerFailure;
    }

    public GatewayDeferredAutomationResult? Advance(bool stopRequested)
    {
        if (terminal != null) return terminal;
        GatewayDeferredAutomationResult? result = inner.Advance(stopRequested);
        if (result == null) return null;
        string? failure = result.Error == null ? providerFailure() : null;
        terminal = failure == null ? result : GatewayDeferredAutomationResult.Failure(failure,
            "Native input provider became unavailable before the gesture completed.", result.Result);
        return terminal;
    }
}
