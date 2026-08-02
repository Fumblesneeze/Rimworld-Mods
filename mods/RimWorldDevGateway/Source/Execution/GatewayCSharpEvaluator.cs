using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Mono.CSharp;

namespace RimWorldDevGateway;

public sealed class GatewayCSharpEvaluationResult
{
    internal GatewayCSharpEvaluationResult(
        bool succeeded,
        bool resultSet,
        string? value,
        string? type,
        string diagnostics)
    {
        Succeeded = succeeded;
        ResultSet = resultSet;
        Value = value;
        Type = type;
        Diagnostics = diagnostics ?? string.Empty;
    }

    public bool Succeeded { get; }

    public bool ResultSet { get; }

    public string? Value { get; }

    public string? Type { get; }

    public string Diagnostics { get; }
}

public sealed class GatewayCSharpEvaluator
{
    private const string TruncatedMarker = "[truncated]";
    public const int DefaultMaximumSourceCharacters = 64 * 1024;
    public const int DefaultMaximumDiagnosticCharacters = 16 * 1024;
    public const int DefaultMaximumResultCharacters = 64 * 1024;
    public const int DefaultMaximumTypeCharacters = 1024;

    private readonly object sync = new();
    private readonly int maximumSourceCharacters;
    private readonly int maximumResultCharacters;
    private readonly int maximumTypeCharacters;
    private readonly BoundedTextWriter diagnosticWriter;
    private readonly CompilerContext context;
    private readonly Evaluator evaluator;

    public GatewayCSharpEvaluator(
        int maximumSourceCharacters = DefaultMaximumSourceCharacters,
        int maximumDiagnosticCharacters = DefaultMaximumDiagnosticCharacters,
        int maximumResultCharacters = DefaultMaximumResultCharacters,
        int maximumTypeCharacters = DefaultMaximumTypeCharacters)
    {
        if (maximumSourceCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSourceCharacters));
        }

        if (maximumDiagnosticCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDiagnosticCharacters));
        }

        if (maximumResultCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumResultCharacters));
        }

        if (maximumTypeCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumTypeCharacters));
        }

        this.maximumSourceCharacters = maximumSourceCharacters;
        this.maximumResultCharacters = maximumResultCharacters;
        this.maximumTypeCharacters = maximumTypeCharacters;
        diagnosticWriter = new BoundedTextWriter(maximumDiagnosticCharacters);
        context = new CompilerContext(
            new CompilerSettings(),
            new StreamReportPrinter(diagnosticWriter));
        evaluator = new Evaluator(context);
        if (!evaluator.Run(
                "using System; " +
                "using System.Collections.Generic; " +
                "using System.Linq; " +
                "using System.Reflection; " +
                "using System.Threading; " +
                "using System.Threading.Tasks;"))
        {
            throw new GatewayExecutionException(
                "csharp_initialization_failed",
                "Mono.CSharp could not initialize the gateway REPL framework namespaces: " +
                diagnosticWriter.GetText());
        }

        diagnosticWriter.Reset();
        context.Report.Printer.Reset();
        var gameAssemblyAlreadyImported = evaluator.Evaluate(
            "typeof(Verse.Current)",
            out _,
            out _) is null && context.Report.Errors == 0;
        diagnosticWriter.Reset();
        context.Report.Printer.Reset();

        ReferenceLoadedAssemblies(gameAssemblyAlreadyImported);
        if (!evaluator.Run(
                "using RimWorld; " +
                "using Verse; " +
                "using UnityEngine; " +
                "using RimWorldDevGateway;"))
        {
            throw new GatewayExecutionException(
                "csharp_initialization_failed",
                "Mono.CSharp could not initialize the gateway REPL namespaces: " +
                diagnosticWriter.GetText());
        }

        diagnosticWriter.Reset();
    }

    public GatewayCSharpEvaluationResult Evaluate(string source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (source.Length > maximumSourceCharacters)
        {
            throw new GatewayExecutionException(
                "csharp_source_too_large",
                $"C# source exceeds the {maximumSourceCharacters}-character limit.");
        }

        lock (sync)
        {
            diagnosticWriter.Reset();
            context.Report.Printer.Reset();
            var incomplete = evaluator.Evaluate(source, out var value, out var resultSet);
            var succeeded = incomplete is null && context.Report.Errors == 0;
            var resultText = succeeded && resultSet
                ? Convert.ToString(value, CultureInfo.InvariantCulture)
                : null;
            return new GatewayCSharpEvaluationResult(
                succeeded,
                succeeded && resultSet,
                Bound(resultText, maximumResultCharacters),
                Bound(succeeded && resultSet ? value?.GetType().FullName : null, maximumTypeCharacters),
                diagnosticWriter.GetText());
        }
    }

    private static string? Bound(string? value, int maximumCharacters)
    {
        if (value is null || value.Length <= maximumCharacters)
        {
            return value;
        }

        if (maximumCharacters <= TruncatedMarker.Length)
        {
            return TruncatedMarker.Substring(0, maximumCharacters);
        }

        return value.Substring(0, maximumCharacters - TruncatedMarker.Length) + TruncatedMarker;
    }

    private void ReferenceLoadedAssemblies(bool gameAssemblyAlreadyImported)
    {
        var gameAssembly = typeof(Verse.Current).Assembly;
        var required = new List<Assembly>();
        if (!gameAssemblyAlreadyImported)
        {
            required.Add(gameAssembly);
        }

        required.Add(typeof(UnityEngine.Vector3).Assembly);
        required.Add(typeof(GatewayCSharpEvaluator).Assembly);
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            typeof(object).Assembly.GetName().Name,
            typeof(Uri).Assembly.GetName().Name,
            typeof(Enumerable).Assembly.GetName().Name
        };
        if (gameAssemblyAlreadyImported)
        {
            referenced.Add(gameAssembly.GetName().Name);
        }

        foreach (var assembly in required)
        {
            if (referenced.Add(assembly.GetName().Name))
            {
                evaluator.ReferenceAssembly(assembly);
            }
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (assembly.IsDynamic ||
                assembly.GlobalAssemblyCache ||
                string.IsNullOrEmpty(name) ||
                !referenced.Add(name))
            {
                continue;
            }

            try
            {
                evaluator.ReferenceAssembly(assembly);
            }
            catch (Exception)
            {
                // Optional runtime-only assemblies can be unreferenceable; required assemblies above are strict.
            }
        }
    }

    private sealed class BoundedTextWriter : TextWriter
    {
        private readonly StringBuilder builder = new();
        private readonly int maximumCharacters;
        private bool truncated;

        public BoundedTextWriter(int maximumCharacters)
            : base(CultureInfo.InvariantCulture)
        {
            this.maximumCharacters = maximumCharacters;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (builder.Length < maximumCharacters)
            {
                builder.Append(value);
            }
            else
            {
                truncated = true;
            }
        }

        public override void Write(string? value)
        {
            if (value is null || value.Length == 0)
            {
                return;
            }

            var remaining = maximumCharacters - builder.Length;
            if (remaining > 0)
            {
                builder.Append(value, 0, Math.Min(remaining, value.Length));
            }

            if (value.Length > remaining)
            {
                truncated = true;
            }
        }

        public void Reset()
        {
            builder.Clear();
            truncated = false;
        }

        public string GetText()
        {
            if (truncated && maximumCharacters >= TruncatedMarker.Length)
            {
                builder.Length = Math.Min(builder.Length, maximumCharacters - TruncatedMarker.Length);
                builder.Append(TruncatedMarker);
            }

            return builder.ToString().Trim();
        }
    }
}
