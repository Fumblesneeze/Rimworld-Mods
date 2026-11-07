#if RIMWORLD_1_7
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;

namespace RimWorldDevGateway;

// CoreCLR removed the AppDomain emission API used by Mono.CSharp. The protocol,
// dispatch thread and per-session declaration lifetime remain the same on Waylith.
public sealed class GatewayCSharpEvaluator
{
    public const int DefaultMaximumSourceCharacters = 64 * 1024;
    public const int DefaultMaximumDiagnosticCharacters = 16 * 1024;
    public const int DefaultMaximumResultCharacters = 64 * 1024;
    public const int DefaultMaximumTypeCharacters = 1024;
    private readonly object sync = new();
    private readonly int maximumSourceCharacters;
    private readonly int maximumDiagnosticCharacters;
    private readonly int maximumResultCharacters;
    private readonly int maximumTypeCharacters;
    private readonly ScriptOptions options;
    private ScriptState<object?>? state;

    public GatewayCSharpEvaluator(
        int maximumSourceCharacters = DefaultMaximumSourceCharacters,
        int maximumDiagnosticCharacters = DefaultMaximumDiagnosticCharacters,
        int maximumResultCharacters = DefaultMaximumResultCharacters,
        int maximumTypeCharacters = DefaultMaximumTypeCharacters)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumSourceCharacters);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumDiagnosticCharacters);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumResultCharacters);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumTypeCharacters);
        this.maximumSourceCharacters = maximumSourceCharacters;
        this.maximumDiagnosticCharacters = maximumDiagnosticCharacters;
        this.maximumResultCharacters = maximumResultCharacters;
        this.maximumTypeCharacters = maximumTypeCharacters;
        var required = new[] { typeof(Verse.Current).Assembly, typeof(UnityEngine.Vector3).Assembly, typeof(GatewayCSharpEvaluator).Assembly };
        var assemblies = required.Concat(AppDomain.CurrentDomain.GetAssemblies())
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .GroupBy(a => a.GetName().Name, StringComparer.OrdinalIgnoreCase).Select(g => g.First());
        options = ScriptOptions.Default.WithReferences(assemblies).WithImports(
            "System", "System.Collections.Generic", "System.Linq", "System.Reflection",
            "System.Threading", "System.Threading.Tasks", "RimWorld", "Verse", "UnityEngine", "RimWorldDevGateway");
    }

    public GatewayCSharpEvaluationResult Evaluate(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Length > maximumSourceCharacters)
            throw new GatewayExecutionException("csharp_source_too_large", $"C# source exceeds the {maximumSourceCharacters}-character limit.");
        lock (sync)
        {
            var script = state is null
                ? CSharpScript.Create<object?>(source, options)
                : state.Script.ContinueWith<object?>(source, options);
            var compilation = script.GetCompilation();
            var tree = compilation.SyntaxTrees.Last();
            var root = (CompilationUnitSyntax)tree.GetRoot();
            // This endpoint has a synchronous dispatcher contract. Awaiting a script
            // would either resume off-thread or deadlock a captured game context.
            // Reject before running any part of the submission, not after side effects.
            if (compilation.SyntaxTrees.Any(syntax => syntax.GetRoot().DescendantTokens()
                    .Any(token => token.IsKind(SyntaxKind.AwaitKeyword))))
                return new GatewayCSharpEvaluationResult(false, false, null, null,
                    Bound("await is not supported by the synchronous game-thread REPL; use an explicit dispatcher-backed automation for multi-frame work.", maximumDiagnosticCharacters)!);
            try
            {
                // Run synchronously on the dispatching game thread, as with the legacy evaluator.
                var next = state is null
                    ? script.RunAsync().GetAwaiter().GetResult()
                    : script.RunFromAsync(state).GetAwaiter().GetResult();
                state = next;
                var last = root.Members.LastOrDefault() as GlobalStatementSyntax;
                var resultSet = root.Members.OfType<GlobalStatementSyntax>()
                        .SelectMany(global => global.DescendantNodes(node => node is not AnonymousFunctionExpressionSyntax
                            and not LocalFunctionStatementSyntax))
                        .OfType<ReturnStatementSyntax>().Any(statement => statement.Expression is not null) ||
                    (last?.Statement is ExpressionStatementSyntax expression && expression.SemicolonToken.IsMissing &&
                     compilation.GetSemanticModel(tree).GetTypeInfo(expression.Expression).Type?.SpecialType != SpecialType.System_Void);
                return new GatewayCSharpEvaluationResult(true, resultSet,
                    resultSet && next.ReturnValue is not null
                        ? Bound(Convert.ToString(next.ReturnValue, CultureInfo.InvariantCulture), maximumResultCharacters) : null,
                    resultSet ? Bound(next.ReturnValue?.GetType().FullName, maximumTypeCharacters) : null, string.Empty);
            }
            catch (CompilationErrorException exception)
            {
                return new GatewayCSharpEvaluationResult(false, false, null, null,
                    Bound(string.Join(Environment.NewLine, exception.Diagnostics), maximumDiagnosticCharacters)!);
            }
        }
    }

    private static string? Bound(string? value, int limit)
    {
        const string marker = "[truncated]";
        if (value is null || value.Length <= limit) return value;
        return limit <= marker.Length ? marker.Substring(0, limit) : value.Substring(0, limit - marker.Length) + marker;
    }
}
#endif
