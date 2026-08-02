using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayCSharpEvaluatorTests
{
    [Test]
    public void Evaluate_returns_the_expression_value_and_runtime_type()
    {
        var evaluator = new GatewayCSharpEvaluator();

        var result = evaluator.Evaluate("1 + 2");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ResultSet, Is.True);
            Assert.That(result.Value, Is.EqualTo("3"));
            Assert.That(result.Type, Is.EqualTo("System.Int32"));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Evaluate_preserves_variables_between_submissions()
    {
        var evaluator = new GatewayCSharpEvaluator();

        var declaration = evaluator.Evaluate("var gatewayCounter = 40;");
        var result = evaluator.Evaluate("gatewayCounter + 2");

        Assert.Multiple(() =>
        {
            Assert.That(declaration.Succeeded, Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value, Is.EqualTo("42"));
            Assert.That(result.Type, Is.EqualTo("System.Int32"));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Compile_error_does_not_discard_prior_repl_state()
    {
        var evaluator = new GatewayCSharpEvaluator();

        var declaration = evaluator.Evaluate("var stateBeforeError = 40;");
        var failed = evaluator.Evaluate("missingAfterDeclaration + 1");
        var recovered = evaluator.Evaluate("stateBeforeError + 2");

        Assert.Multiple(() =>
        {
            Assert.That(declaration.Succeeded, Is.True);
            Assert.That(failed.Succeeded, Is.False);
            Assert.That(failed.Diagnostics, Is.Not.Empty);
            Assert.That(recovered.Succeeded, Is.True, recovered.Diagnostics);
            Assert.That(recovered.Value, Is.EqualTo("42"));
            Assert.That(recovered.Type, Is.EqualTo("System.Int32"));
        });
    }

    [Test]
    public void Evaluate_returns_bounded_compiler_diagnostics_without_a_value()
    {
        var evaluator = new GatewayCSharpEvaluator(maximumDiagnosticCharacters: 80);

        var result = evaluator.Evaluate("missingSymbol + anotherMissingSymbol");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ResultSet, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Type, Is.Null);
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(result.Diagnostics.Length, Is.LessThanOrEqualTo(80));
        });
    }

    [Test]
    public void Evaluate_rejects_source_above_the_configured_limit()
    {
        var evaluator = new GatewayCSharpEvaluator(maximumSourceCharacters: 4);

        var exception = Assert.Throws<GatewayExecutionException>(() => evaluator.Evaluate("12345"));

        Assert.That(exception!.Code, Is.EqualTo("csharp_source_too_large"));
    }

    [Test]
    public void Evaluate_truncates_an_oversized_value_with_an_explicit_marker()
    {
        var evaluator = new GatewayCSharpEvaluator(maximumResultCharacters: 24);

        var result = evaluator.Evaluate("new string('x', 100)");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value, Has.Length.LessThanOrEqualTo(24));
            Assert.That(result.Value, Does.EndWith("[truncated]"));
            Assert.That(result.Type, Is.EqualTo("System.String"));
        });
    }

    [Test]
    public void Evaluate_preloads_gateway_game_and_unity_namespaces()
    {
        var evaluator = new GatewayCSharpEvaluator();

        var result = evaluator.Evaluate(
            "typeof(GatewayDispatcher).FullName + \"|\" + " +
            "typeof(Current).FullName + \"|\" + typeof(Vector3).FullName");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.Diagnostics);
            Assert.That(
                result.Value,
                Is.EqualTo(
                    "RimWorldDevGateway.GatewayDispatcher|" +
                    "Verse.Current|UnityEngine.Vector3"));
        });
    }

    [Test]
    public void Evaluate_preloads_compatible_assemblies_already_loaded_in_the_app_domain()
    {
        var evaluator = new GatewayCSharpEvaluator();

        var result = evaluator.Evaluate(
            "typeof(RimWorldDevGateway.Tests.GatewayCSharpEvaluatorTests).FullName");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.Diagnostics);
            Assert.That(
                result.Value,
                Is.EqualTo("RimWorldDevGateway.Tests.GatewayCSharpEvaluatorTests"));
        });
    }

    [Test]
    public void Evaluate_bounds_the_runtime_type_name()
    {
        var evaluator = new GatewayCSharpEvaluator(maximumTypeCharacters: 11);

        var result = evaluator.Evaluate("1");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Type, Has.Length.LessThanOrEqualTo(11));
            Assert.That(result.Type, Does.EndWith("[truncated]"));
        });
    }
}
