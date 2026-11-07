using NUnit.Framework;
using RimWorldDevGateway.Client;

namespace RimWorldDevGateway.Tests;

public sealed class WaylithSourceCompilerTests
{
    [Test]
    public void CompilesGameTypedDiagnosticForSelectedRuntimeAndRemovesTemporaryWorkspace()
    {
        var root = Directory.CreateDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, "snippet-test-" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var source = Path.Combine(root, "Entry.cs");
            File.WriteAllText(source, "public static class Entry { public static string Execute(string input) => typeof(Verse.Current).FullName; }");
            var compiler = new DotNetGatewaySourceCompiler(temporaryRoot: root);
            var bytes = compiler.Compile(new GatewaySourceCompilationRequest(source,
                Path.GetDirectoryName(typeof(Verse.Current).Assembly.Location)!,
                typeof(Contracts.GatewayAssemblyExecutionContext).Assembly.Location, "net10.0"));
            Assert.That(bytes.Length, Is.GreaterThan(0));
            Assert.That(Directory.GetDirectories(root), Is.Empty);
        }
        finally { Directory.Delete(root, true); }
    }
}
