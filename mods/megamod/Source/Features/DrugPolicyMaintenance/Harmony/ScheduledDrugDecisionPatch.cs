using System;
using System.Collections.Generic;
using System.Reflection;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugTolerance;
using Megamod.Patching;

namespace Megamod.Features.DrugPolicyMaintenance.Harmony;

// One native owner, two fixed consumers. S138 depends on this receipt without owning its rollback.
public sealed class ScheduledDrugDecisionPatch
{
    private readonly DrugToleranceDecisionPatch decision;
    private readonly HashSet<string> reported = new(StringComparer.Ordinal);

    public ScheduledDrugDecisionPatch(MethodInfo target,
        Func<object, object, ScheduledDrugReadResult> potentialMood,
        Func<object, object, ScheduledDrugReadResult> tolerance,
        Func<bool> contextAvailable, Action<string> report,
        Func<object, object, Func<bool>, bool>? inCurrentContext = null)
    {
        void Report(string reason)
        {
            string bounded = reason.Replace('\r', ' ').Replace('\n', ' ');
            if (bounded.Length > 256) bounded = bounded.Substring(0, 253) + "...";
            lock (reported)
                if (reported.Count >= 16 || !reported.Add(bounded)) return;
            report("[fumblesneeze.megamod] patch=DRUG-SCHEDULED; target=" + target.DeclaringType?.FullName + "." + target.Name +
                "; targetSpec=DRUG-SCHEDULED-COMPOSITION; reason=" + bounded +
                "; unavailable=scheduled mood restrictions and atomic tolerance editing/enforcement; " +
                "fallback=native scheduled permission; retained feature and policy choices");
        }
        bool Evaluate(object actor, object drug)
        {
            var moodResult = potentialMood(actor, drug);
            var toleranceResult = tolerance(actor, drug);
            return moodResult != ScheduledDrugReadResult.Rejected &&
                toleranceResult != ScheduledDrugReadResult.Rejected;
        }
        decision = new("DRUG-SCHEDULED", target, true,
            (actor, drug) => inCurrentContext == null ? Evaluate(actor, drug) :
                inCurrentContext(actor, drug, () => Evaluate(actor, drug)),
            () => Available && contextAvailable(), Report, targetSpecId: "DRUG-SCHEDULED-COMPOSITION",
            unavailableBehavior: "scheduled mood restrictions and atomic tolerance editing/enforcement",
            fallback: "native scheduled permission; retained feature and policy choices");
        S138Dependency = new("DT-SCHEDULED-DEPENDENCY", "DRUG-SCHEDULED-COMPOSITION", target.ToString()!,
            "atomic tolerance editing/enforcement", "native permission; independent supported mood restriction",
            ValidateDependency, () =>
            {
                var failure = ValidateDependency();
                if (failure != null) throw new InvalidOperationException(failure);
            }, () => { });
    }

    public PatchCandidate Candidate => decision.Candidate;
    public PatchCandidate S138Dependency { get; }
    public bool Available => decision.Available && decision.Failure == null;
    public string? Failure => decision.Failure;
    private string? ValidateDependency() => Available ? null : Failure ?? "The shared scheduled boundary is not currently admitted.";
}
