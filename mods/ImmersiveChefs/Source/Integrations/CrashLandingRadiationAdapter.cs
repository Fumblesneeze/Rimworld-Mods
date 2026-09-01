using System.Threading;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

public enum CrashLandingRadiationOwner
{
    CoreToxicBuildup,
    CrashLanding
}

public enum UraniumRadiationOwner
{
    CoreToxicBuildup,
    CrashLanding,
    Rimatomics
}

public sealed class CrashLandingRadiationShape
{
    public CrashLandingRadiationShape(
        string owningPackageId,
        string defName,
        string hediffClassName,
        float lethalSeverity,
        float firstVisibleSeverity,
        bool initialStageHidden)
    {
        OwningPackageId = owningPackageId ?? string.Empty;
        DefName = defName ?? string.Empty;
        HediffClassName = hediffClassName ?? string.Empty;
        LethalSeverity = lethalSeverity;
        FirstVisibleSeverity = firstVisibleSeverity;
        InitialStageHidden = initialStageHidden;
    }

    public string OwningPackageId { get; }
    public string DefName { get; }
    public string HediffClassName { get; }
    public float LethalSeverity { get; }
    public float FirstVisibleSeverity { get; }
    public bool InitialStageHidden { get; }
}

public static class CrashLandingRadiationPolicy
{
    public const string PackageId = "Katavrik.CrashLanding";
    public const string DefName = "Rad";
    public const float LethalSeverity = 1f;
    public const float FirstVisibleSeverity = 0.1f;

    public static CrashLandingRadiationOwner Decide(
        bool packageActive,
        bool integrationEnabled,
        CrashLandingRadiationShape shape)
    {
        if (!packageActive || !integrationEnabled || shape is null)
        {
            return CrashLandingRadiationOwner.CoreToxicBuildup;
        }

        return string.Equals(shape.OwningPackageId, PackageId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(shape.DefName, DefName, StringComparison.Ordinal) &&
               string.Equals(shape.HediffClassName, typeof(HediffWithComps).FullName, StringComparison.Ordinal) &&
               Math.Abs(shape.LethalSeverity - LethalSeverity) <= 0.000001f &&
               Math.Abs(shape.FirstVisibleSeverity - FirstVisibleSeverity) <= 0.000001f &&
               shape.InitialStageHidden
            ? CrashLandingRadiationOwner.CrashLanding
            : CrashLandingRadiationOwner.CoreToxicBuildup;
    }

    public static bool TryClaimWarning(ref int state) =>
        Interlocked.CompareExchange(ref state, 1, 0) == 0;
}

public static class UraniumRadiationProviderPolicy
{
    public static UraniumRadiationOwner Decide(bool rimatomicsSupported, bool crashLandingSupported)
    {
        if (rimatomicsSupported)
        {
            return UraniumRadiationOwner.Rimatomics;
        }

        return crashLandingSupported
            ? UraniumRadiationOwner.CrashLanding
            : UraniumRadiationOwner.CoreToxicBuildup;
    }
}

internal enum CrashLandingRadiationApplication
{
    UseNextProvider,
    Applied,
    FailedAfterInvocation
}

internal static class CrashLandingRadiationAdapter
{
    private static readonly object Sync = new();
    private static bool resolved;
    private static HediffDef? radiationDef;
    private static CrashLandingRadiationOwner owner;
    private static int warningState;

    internal static void ValidateActiveProviderShape()
    {
        Resolve();
    }

    internal static CrashLandingRadiationApplication Apply(Pawn pawn, float severityEquivalentDose)
    {
        if (pawn is null)
        {
            throw new ArgumentNullException(nameof(pawn));
        }

        Resolve();
        if (owner != CrashLandingRadiationOwner.CrashLanding || radiationDef is null)
        {
            return CrashLandingRadiationApplication.UseNextProvider;
        }

        try
        {
            HealthUtility.AdjustSeverity(pawn, radiationDef, severityEquivalentDose);
            return CrashLandingRadiationApplication.Applied;
        }
        catch (Exception exception)
        {
            WarnOnce(
                "Crash Landing radiation application failed after admission; Core fallback was not applied " +
                "to avoid a possible duplicate dose. " + RootMessage(exception));
            return CrashLandingRadiationApplication.FailedAfterInvocation;
        }
    }

    private static void Resolve()
    {
        if (resolved)
        {
            return;
        }

        lock (Sync)
        {
            if (resolved)
            {
                return;
            }

            var packageActive =
                ImmersiveChefsMod.Integrations?.IsActive(OptionalIntegration.CrashLanding) == true;
            var integrationEnabled =
                ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CrashLanding);
            var candidate = packageActive && integrationEnabled
                ? DefDatabase<HediffDef>.GetNamedSilentFail(CrashLandingRadiationPolicy.DefName)
                : null;
            var shape = ShapeFor(candidate);
            owner = CrashLandingRadiationPolicy.Decide(packageActive, integrationEnabled, shape);
            radiationDef = owner == CrashLandingRadiationOwner.CrashLanding ? candidate : null;
            resolved = true;
            if (packageActive && integrationEnabled && owner != CrashLandingRadiationOwner.CrashLanding)
            {
                WarnOnce(
                    "Crash Landing is active but its supported radiation Hediff changed. " +
                    "Uranium kitchenware will use another supported provider or Core toxic buildup for this session.");
            }
        }
    }

    private static CrashLandingRadiationShape ShapeFor(HediffDef? def)
    {
        var stages = def?.stages ?? new List<HediffStage>();
        var firstVisible = stages.FirstOrDefault(stage => stage.becomeVisible);
        return new CrashLandingRadiationShape(
            def?.modContentPack?.PackageId ?? string.Empty,
            def?.defName ?? string.Empty,
            def?.hediffClass?.FullName ?? string.Empty,
            def?.lethalSeverity ?? float.NaN,
            firstVisible?.minSeverity ?? float.NaN,
            stages.Count > 0 && !stages[0].becomeVisible);
    }

    private static void WarnOnce(string message)
    {
        if (CrashLandingRadiationPolicy.TryClaimWarning(ref warningState))
        {
            Log.Warning("[ImmersiveChefs] " + message);
        }
    }

    private static string RootMessage(Exception exception)
    {
        var root = exception;
        while (root.InnerException is not null)
        {
            root = root.InnerException;
        }

        return root.GetType().Name + ": " + root.Message;
    }
}
