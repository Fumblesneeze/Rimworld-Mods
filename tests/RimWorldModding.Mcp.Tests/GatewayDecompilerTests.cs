using System.Reflection;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GatewayDecompilerTests
{
    [Test]
    public void DiagnosticDeadline_ExpiresAndPropagatesCallerCancellation()
    {
        using var deadline = GatewayDiagnostics.CreateDeadline(CancellationToken.None, TimeSpan.Zero);
        Assert.That(SpinWait.SpinUntil(() => deadline.IsCancellationRequested, 1000), Is.True);
        using var caller = new CancellationTokenSource();
        using var linked = GatewayDiagnostics.CreateDeadline(caller.Token);
        caller.Cancel();
        Assert.That(linked.IsCancellationRequested, Is.True);
        Assert.Throws<OperationCanceledException>(() => GatewayMethodDecompiler.Decompile(new byte[1], null,
            0x06000001, "unused.dll", linked.Token));
    }

    public static string Overload(int number) => "integer-overload-marker" + number;
    public static string Overload(string text) => "string-overload-marker" + text;

    [Test]
    public void Decompile_UsesExactTokenAndRejectsOtherModuleIdentity()
    {
        var method = typeof(GatewayDecompilerTests).GetMethod(nameof(Overload), new[] { typeof(int) })!;
        var bytes = File.ReadAllBytes(method.Module.FullyQualifiedName);
        var result = GatewayMethodDecompiler.Decompile(bytes, method.Module.ModuleVersionId,
            method.MetadataToken, method.Module.FullyQualifiedName, CancellationToken.None);
        Assert.That(result.Code, Does.Contain("integer-overload-marker").And.Not.Contain("string-overload-marker"));
        Assert.Throws<ArgumentException>(() => GatewayMethodDecompiler.Decompile(bytes, Guid.NewGuid(),
            method.MetadataToken, method.Module.FullyQualifiedName, CancellationToken.None));
    }
}
