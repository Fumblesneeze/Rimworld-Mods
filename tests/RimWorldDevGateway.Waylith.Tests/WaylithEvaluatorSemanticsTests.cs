using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class WaylithEvaluatorSemanticsTests
{
    [Test]
    public void AwaitCannotEscapeTheSynchronousDispatcherOrChangeState()
    {
        var evaluator = new GatewayCSharpEvaluator();
        evaluator.Evaluate("var affinityCounter = 0;");
        var result = evaluator.Evaluate("affinityCounter++; await Task.Delay(1); affinityCounter++;");
        Assert.Multiple(() => {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Diagnostics, Does.Contain("await"));
            Assert.That(evaluator.Evaluate("affinityCounter").Value, Is.EqualTo("0"));
            Assert.That(evaluator.Evaluate("Environment.CurrentManagedThreadId").Value,
                Is.EqualTo(Environment.CurrentManagedThreadId.ToString()));
        });
    }

    [TestCase("if (true) return 42;", "42", true)]
    [TestCase("{ return 43; }", "43", true)]
    [TestCase("return null;", null, true)]
    [TestCase("(string)null", null, true)]
    [TestCase("System.GC.KeepAlive(null)", null, false)]
    [TestCase("int Helper() { return 1; }", null, false)]
    [TestCase("int HelperProperty { get { return 1; } }", null, false)]
    public void ReportsSubmissionResultsWithoutMistakingHelperReturns(string source, string? value, bool resultSet)
    {
        var result = new GatewayCSharpEvaluator().Evaluate(source);
        Assert.Multiple(() => {
            Assert.That(result.Succeeded, Is.True, result.Diagnostics);
            Assert.That(result.ResultSet, Is.EqualTo(resultSet));
            Assert.That(result.Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void LoadedAwaitIsRejectedBeforeLoadedSourceSideEffects()
    {
        var path = Path.Combine(Path.GetTempPath(), "gateway-loaded-" + Guid.NewGuid().ToString("N") + ".csx");
        try
        {
            File.WriteAllText(path, "loadedCounter++; await System.Threading.Tasks.Task.Delay(1); loadedCounter++;");
            var evaluator = new GatewayCSharpEvaluator();
            evaluator.Evaluate("var loadedCounter = 0;");
            var result = evaluator.Evaluate("#load \"" + path.Replace("\\", "/") + "\"\nloadedCounter");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Diagnostics, Does.Contain("await"));
            Assert.That(evaluator.Evaluate("loadedCounter").Value, Is.EqualTo("0"));
        }
        finally { File.Delete(path); }
    }
}
