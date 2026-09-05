using System;
using System.Collections.Generic;

namespace PersonalBugfixes;

public enum ProbeOutcome { BugPresent, Healthy, Inconclusive }
public enum FixState { Absent, Incompatible, NotRequired, Applied, Failed }
public sealed class FixResult
{
    public FixResult(string id, FixState state, string stage, string detail)
    { Id = id; State = state; Stage = stage; Detail = detail; }
    public string Id { get; }
    public FixState State { get; }
    public string Stage { get; }
    public string Detail { get; }
    public bool Warning => State is FixState.Incompatible or FixState.Failed;
    public override string ToString() => $"[Personal Bugfixes] {Id}: {State}; stage={Stage}; {Detail}";
}

public interface IPersonalFix
{
    string Id { get; }
    bool TargetPresent { get; }
    string TargetDescription { get; }
    string? Inspect(); // null means supported; otherwise a diagnostic reason.
    ProbeOutcome Probe();
    void Apply();
    bool VerifyInstalled();
    void Remove();
}

public sealed class FixLifecycle
{
    private readonly Dictionary<string, FixResult> results = new(StringComparer.Ordinal);
    public FixResult Evaluate(IPersonalFix fix)
    {
        if (results.TryGetValue(fix.Id, out var previous)) return previous;
        return results[fix.Id] = EvaluateOnce(fix);
    }

    private static FixResult EvaluateOnce(IPersonalFix fix)
    {
        string stage = "presence";
        bool attempted = false;
        try
        {
            if (!fix.TargetPresent) return Result(FixState.Absent, "optional target package is absent");
            stage = "structure";
            var error = fix.Inspect();
            if (error != null) return Result(FixState.Incompatible, error);
            stage = "original probe";
            var before = fix.Probe();
            if (before == ProbeOutcome.Healthy) return Result(FixState.NotRequired, "unpatched assertion passed");
            if (before != ProbeOutcome.BugPresent) return Result(FixState.Incompatible, "probe inconclusive");
            stage = "apply";
            attempted = true;
            fix.Apply();
            stage = "installed IL";
            if (!fix.VerifyInstalled()) throw new InvalidOperationException("Installed patch postcondition failed.");
            stage = "patched probe";
            if (fix.Probe() != ProbeOutcome.Healthy) throw new InvalidOperationException("Patched assertion did not pass.");
            return Result(FixState.Applied, "local IL verified; original failed and patched assertion passed");
        }
        catch (Exception exception)
        {
            string detail = exception.GetBaseException().Message;
            if (attempted)
            {
                try { fix.Remove(); detail += "; own patch removed"; }
                catch (Exception cleanup) { detail += "; ROLLBACK FAILED: " + cleanup.GetBaseException().Message; }
            }
            return Result(FixState.Failed, detail);
        }

        FixResult Result(FixState state, string detail) => new(fix.Id, state, stage, fix.TargetDescription + "; " + detail);
    }
}
