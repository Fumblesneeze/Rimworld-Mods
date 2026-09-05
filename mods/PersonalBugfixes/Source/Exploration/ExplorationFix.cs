using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace PersonalBugfixes.Exploration;

public sealed class ExplorationFix : IPersonalFix
{
    public const string PackageId = "thelastbulletbender.rwexploration";
    public const string Owner = "fumblesneeze.personalbugfixes.exploration-discovery-list";
    private readonly Func<ExplorationTarget> resolve;
    private ExplorationTarget? target;
    private string detail = "target not resolved";
    private readonly Harmony harmony = new(Owner);

    public ExplorationFix(bool present, Func<ExplorationTarget> resolve)
    { TargetPresent = present; this.resolve = resolve; }
    public string Id => "exploration-discovery-list";
    public bool TargetPresent { get; }
    public string TargetDescription => $"{PackageId}: VisibilityManager.UpdateGraphics; {detail}";

    public string? Inspect()
    {
        target = resolve();
        detail = $"{target.Update.DeclaringType!.Assembly.GetName().Name}; MVID={target.Update.Module.ModuleVersionId}";
        if (Harmony.GetPatchInfo(target.Update)?.Owners.Any() == true)
            return "target already has Harmony patches; cannot establish their behavior with a transpiler-only copy";
        var code = PatchProcessor.GetOriginalInstructions(target.Update);
        return ExplorationReadPatch.FindRead(code, target.Learned) < 0
            ? "expected exactly one local learnedFeatures/index/get_Item sequence" : null;
    }
    public ProbeOutcome Probe()
    {
        var result = DetachedProbe.Run(target!);
        detail = $"MVID={target!.Update.Module.ModuleVersionId}; {DetachedProbe.LastDetail}";
        return result;
    }
    public void Apply()
    {
        ExplorationReadPatch.Register(target!.Update, target.Learned);
        harmony.Patch(target.Update, transpiler: new HarmonyMethod(typeof(ExplorationReadPatch).GetMethod(nameof(ExplorationReadPatch.Transpiler))));
    }
    public bool VerifyInstalled()
    {
        var patches = Harmony.GetPatchInfo(target!.Update);
        if (patches == null || patches.Transpilers.Count(p => p.owner == Owner) != 1 || patches.Owners.Any(o => o != Owner)) return false;
        var code = PatchProcessor.GetCurrentInstructions(target.Update);
        return code.Count(i => i.Calls(ExplorationReadPatch.SafeRead)) == 1 &&
            ExplorationReadPatch.FindRead(code, target.Learned) == -1;
    }
    public void Remove()
    {
        if (target == null) return;
        harmony.Unpatch(target.Update, HarmonyPatchType.All, Owner);
        if (Harmony.GetPatchInfo(target.Update)?.Owners.Contains(Owner) == true)
            throw new InvalidOperationException("Personal patch is still installed after rollback.");
        ExplorationReadPatch.Forget(target.Update);
    }
}
