using System.Reflection;
using System.Threading;
using Verse;

namespace ImmersiveChefs;

public enum RimatomicsRadiationOwner
{
    CoreToxicBuildup,
    Rimatomics
}

public sealed class RimatomicsRadiationShape
{
    public RimatomicsRadiationShape(
        string assemblyName,
        string typeName,
        string methodName,
        bool isPublic,
        bool isStatic,
        string returnTypeName,
        IEnumerable<string> parameterTypeNames)
    {
        AssemblyName = assemblyName ?? string.Empty;
        TypeName = typeName ?? string.Empty;
        MethodName = methodName ?? string.Empty;
        IsPublic = isPublic;
        IsStatic = isStatic;
        ReturnTypeName = returnTypeName ?? string.Empty;
        ParameterTypeNames = (parameterTypeNames ?? Array.Empty<string>()).ToArray();
    }

    public string AssemblyName { get; }
    public string TypeName { get; }
    public string MethodName { get; }
    public bool IsPublic { get; }
    public bool IsStatic { get; }
    public string ReturnTypeName { get; }
    public IReadOnlyList<string> ParameterTypeNames { get; }
}

public static class RimatomicsRadiationPolicy
{
    public const string PackageId = "Dubwise.Rimatomics";
    public const string AssemblyName = "Rimatomics";
    public const string TypeName = "Rimatomics.DubUtils";
    public const string MethodName = "applyRads";
    public const float SeverityPerStrengthAtDefault = 0.0028758333f;

    public static RimatomicsRadiationOwner Decide(
        bool packageActive,
        bool integrationEnabled,
        RimatomicsRadiationShape shape)
    {
        if (!packageActive || !integrationEnabled || shape is null)
        {
            return RimatomicsRadiationOwner.CoreToxicBuildup;
        }

        return string.Equals(shape.AssemblyName, AssemblyName, StringComparison.Ordinal) &&
               string.Equals(shape.TypeName, TypeName, StringComparison.Ordinal) &&
               string.Equals(shape.MethodName, MethodName, StringComparison.Ordinal) &&
               shape.IsPublic &&
               shape.IsStatic &&
               string.Equals(shape.ReturnTypeName, typeof(void).FullName, StringComparison.Ordinal) &&
               shape.ParameterTypeNames.SequenceEqual(
                   new[] { typeof(Pawn).FullName!, typeof(float).FullName! },
                   StringComparer.Ordinal)
            ? RimatomicsRadiationOwner.Rimatomics
            : RimatomicsRadiationOwner.CoreToxicBuildup;
    }

    public static float StrengthForSeverity(float severityEquivalentDose)
    {
        if (float.IsNaN(severityEquivalentDose) || float.IsInfinity(severityEquivalentDose) ||
            severityEquivalentDose <= 0f)
        {
            return 0f;
        }

        return severityEquivalentDose / SeverityPerStrengthAtDefault;
    }

    public static bool TryClaimWarning(ref int state) =>
        Interlocked.CompareExchange(ref state, 1, 0) == 0;
}

internal enum RimatomicsRadiationApplication
{
    UseCoreFallback,
    Applied,
    FailedAfterInvocation
}

internal static class RimatomicsRadiationAdapter
{
    private static readonly object Sync = new();
    private static bool resolved;
    private static MethodInfo? applyRads;
    private static RimatomicsRadiationOwner owner;
    private static int warningState;

    internal static void ValidateActiveProviderShape()
    {
        Resolve();
    }

    internal static RimatomicsRadiationApplication Apply(Pawn pawn, float severityEquivalentDose)
    {
        if (pawn is null)
        {
            throw new ArgumentNullException(nameof(pawn));
        }

        Resolve();
        if (owner != RimatomicsRadiationOwner.Rimatomics || applyRads is null)
        {
            return RimatomicsRadiationApplication.UseCoreFallback;
        }

        try
        {
            applyRads.Invoke(
                null,
                new object[]
                {
                    pawn,
                    RimatomicsRadiationPolicy.StrengthForSeverity(severityEquivalentDose)
                });
            return RimatomicsRadiationApplication.Applied;
        }
        catch (Exception exception)
        {
            WarnOnce(
                "Rimatomics radiation invocation failed after admission; Core fallback was not applied " +
                "to avoid a possible duplicate dose. " + RootMessage(exception));
            return RimatomicsRadiationApplication.FailedAfterInvocation;
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

            var packageActive = ImmersiveChefsMod.Integrations?.IsActive(OptionalIntegration.Rimatomics) == true;
            var integrationEnabled = ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.Rimatomics);
            if (!packageActive || !integrationEnabled)
            {
                owner = RimatomicsRadiationOwner.CoreToxicBuildup;
                resolved = true;
                return;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => string.Equals(
                    assembly.GetName().Name,
                    RimatomicsRadiationPolicy.AssemblyName,
                    StringComparison.Ordinal))
                .ToArray();
            MethodInfo? candidate = null;
            RimatomicsRadiationShape shape;
            if (assemblies.Length == 1)
            {
                var type = assemblies[0].GetType(
                    RimatomicsRadiationPolicy.TypeName,
                    throwOnError: false,
                    ignoreCase: false);
                candidate = type?.GetMethod(
                    RimatomicsRadiationPolicy.MethodName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                    binder: null,
                    types: new[] { typeof(Pawn), typeof(float) },
                    modifiers: null);
                shape = ShapeFor(assemblies[0], type, candidate);
            }
            else
            {
                shape = new RimatomicsRadiationShape(
                    assemblies.Length == 0 ? string.Empty : "multiple",
                    string.Empty,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    Array.Empty<string>());
            }

            owner = RimatomicsRadiationPolicy.Decide(packageActive, integrationEnabled, shape);
            applyRads = owner == RimatomicsRadiationOwner.Rimatomics ? candidate : null;
            resolved = true;
            if (owner != RimatomicsRadiationOwner.Rimatomics)
            {
                WarnOnce(
                    "Dubs Rimatomics is active but its supported public radiation API changed. " +
                    "Uranium kitchenware will use another supported provider or Core toxic buildup for this session.");
            }
        }
    }

    private static RimatomicsRadiationShape ShapeFor(Assembly assembly, Type? type, MethodInfo? method) =>
        new(
            assembly.GetName().Name ?? string.Empty,
            type?.FullName ?? string.Empty,
            method?.Name ?? string.Empty,
            method?.IsPublic == true,
            method?.IsStatic == true,
            method?.ReturnType.FullName ?? string.Empty,
            method?.GetParameters().Select(parameter => parameter.ParameterType.FullName ?? string.Empty) ??
            Array.Empty<string>());

    private static void WarnOnce(string message)
    {
        if (RimatomicsRadiationPolicy.TryClaimWarning(ref warningState))
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
