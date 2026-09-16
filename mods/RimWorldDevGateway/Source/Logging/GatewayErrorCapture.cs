using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace RimWorldDevGateway;

// Logging hooks retain only CLR snapshots; mod/Verse ownership is resolved on the main thread.
internal sealed class GatewayErrorCapture : IDisposable
{
    private const string Owner = "fumblesneeze.rimworlddevgateway";
    private readonly Harmony harmony = new(Owner);
    private readonly List<(MethodBase Target, MethodInfo Patch)> installed = new();
    [ThreadStatic] private static Capture? currentLog;
    [ThreadStatic] private static Capture? currentException;
    [ThreadStatic] private static bool capturing;

    internal GatewayErrorCapture()
    {
        try
        {
            Patch(AccessTools.Method(typeof(Verse.Log), nameof(Verse.Log.Error), new[] { typeof(string) }),
                nameof(BeforeLog), nameof(AfterLog));
            var target = AccessTools.Method(typeof(Exception), nameof(Exception.ToString), Type.EmptyTypes);
            var postfix = AccessTools.Method(typeof(GatewayErrorCapture), nameof(AfterException));
            installed.Add((target, postfix));
            harmony.Patch(target, postfix: new HarmonyMethod(postfix) { priority = Priority.Last });
        }
        catch { Dispose(); throw; }
    }

    private void Patch(MethodBase target, string prefix, string? finalizer)
    {
        if (target is null) throw new MissingMethodException("Required diagnostic logging hook is unavailable.");
        var before = AccessTools.Method(typeof(GatewayErrorCapture), prefix);
        var after = finalizer is null ? null : AccessTools.Method(typeof(GatewayErrorCapture), finalizer);
        installed.Add((target, before));
        if (after is not null) installed.Add((target, after));
        harmony.Patch(target, new HarmonyMethod(before) { priority = Priority.First,
            before = new[] { "net.pardeike.rimworld.lib.harmony", "Alexey.BetterStacktraces" } },
            finalizer: after is null ? null : new HarmonyMethod(after));
    }

    private static void BeforeLog(string text, out Capture? __state)
    {
        __state = currentLog;
        if (capturing) return;
        try
        {
            capturing = true;
            currentLog = new Capture("Error", text, new StackTrace(2, false), Array.Empty<GatewayErrorCause>());
        }
        catch { currentLog = null; }
        finally { capturing = false; }
    }

    private static void AfterLog(Capture? __state) => currentLog = __state;

    private static void AfterException(Exception __instance, string __result)
    {
        var e = __instance;
        if (e is null || capturing) return;
        try
        {
            capturing = true;
            if (__result is null || __result.Length > 65536) { currentException = null; return; }
            var causes = new List<GatewayErrorCause>();
            var canNormalize = GatewayExceptionCorrelation.CanNormalize(e);
            var truncated = !canNormalize;
            var inner = e.InnerException;
            while (inner is not null && causes.Count < 8)
            {
                var trace = new StackTrace(inner, false);
                truncated |= trace.FrameCount > 64;
                causes.Add(new GatewayErrorCause(inner.GetType().FullName ?? "Exception", GatewayExceptionCorrelation.SafeMessage(inner),
                    trace.ToString(), Frames(trace)));
                inner = inner.InnerException;
            }
            currentException = new Capture(e.GetType().FullName ?? "Exception", __result,
                new StackTrace(e, false), causes.AsReadOnly()) { Truncated = truncated || inner is not null,
                CanonicalMessage = canNormalize ? (e.GetType().FullName ?? "Exception") + ": " +
                    GatewayExceptionCorrelation.SafeMessage(e) : __result };
        }
        catch { currentException = null; }
        finally { capturing = false; }
    }

    internal static void Observe(GatewayErrorStore store, string message, string stack, string? requestId)
    {
        if (capturing) return;
        try
        {
            capturing = true;
            var exception = currentException;
            currentException = null;
            message = currentLog?.Message ?? message;
            var captured = exception is not null && DateTime.UtcNow - exception.At < TimeSpan.FromSeconds(1) &&
                GatewayExceptionCorrelation.Matches(exception.Message, message) ? exception : currentLog;
            if (captured == exception && exception is not null)
                message = GatewayExceptionCorrelation.CanonicalMessage(exception.Message, message, exception.CanonicalMessage);
            store.Add(captured?.Type ?? "Error", message, captured?.Trace.ToString() ?? stack,
                captured is null ? null : Frames(captured.Trace), captured?.Causes, requestId,
                (captured?.Truncated ?? false) || (captured?.Trace.FrameCount ?? 0) > 64);
        }
        finally { capturing = false; }
    }

    private static IReadOnlyList<GatewayCapturedFrame> Frames(StackTrace trace)
    {
        var result = new List<GatewayCapturedFrame>();
        foreach (var frame in (trace.GetFrames() ?? Array.Empty<StackFrame>()).Take(64))
        {
            try
            {
                var method = Harmony.GetOriginalMethodFromStackframe(frame) ?? frame.GetMethod();
                if (method is null) continue;
                var offset = frame.GetILOffset();
                result.Add(new GatewayCapturedFrame(GatewayMethodIdentity.TryHandle(method) ?? "",
                    GatewayMethodIdentity.Signature(method), offset < 0 ? null : offset));
            }
            catch { result.Add(new GatewayCapturedFrame("", "Unresolved dynamic frame")); }
        }
        return result.AsReadOnly();
    }

    public void Dispose()
    {
        foreach (var patch in installed) harmony.Unpatch(patch.Target, patch.Patch);
        installed.Clear(); currentLog = null; currentException = null;
    }

    private sealed class Capture
    {
        internal Capture(string type, string message, StackTrace trace, IReadOnlyList<GatewayErrorCause> causes)
        { Type = type; Message = message; Trace = trace; Causes = causes; }
        internal string Type { get; }
        internal string Message { get; }
        internal StackTrace Trace { get; }
        internal IReadOnlyList<GatewayErrorCause> Causes { get; }
        internal DateTime At { get; } = DateTime.UtcNow;
        internal bool Truncated { get; set; }
        internal string CanonicalMessage { get; set; } = "";
    }
}
