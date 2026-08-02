using System.IO;
using System.Threading;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayAssemblyExecutorTests
{
    [Test]
    public void Execution_is_unrestricted_by_default_and_invokes_the_exact_static_contract()
    {
        var executor = new GatewayAssemblyExecutor(maximumAssemblyBytes: 4 * 1024 * 1024);
        var assemblyBytes = File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location);

        var result = executor.Execute(
            assemblyBytes,
            typeof(UploadedAssemblyFixture).FullName!,
            nameof(UploadedAssemblyFixture.Execute),
            "{\"value\":42}");

        Assert.Multiple(() =>
        {
            Assert.That(executor.UnrestrictedExecutionEnabled, Is.True);
            Assert.That(executor.UploadCount, Is.EqualTo(1));
            Assert.That(result, Is.EqualTo(Thread.CurrentThread.ManagedThreadId + ":{\"value\":42}"));
        });
    }

    [Test]
    public void Oversized_assemblies_are_rejected_before_load()
    {
        var executor = new GatewayAssemblyExecutor(maximumAssemblyBytes: 3);

        var exception = Assert.Throws<GatewayExecutionException>(() =>
            executor.Execute(new byte[4], "Anything", "Execute", "{}"));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("assembly_too_large"));
            Assert.That(executor.UploadCount, Is.Zero);
        });
    }

    [Test]
    public void Entry_point_must_be_public_static_string_with_one_string_parameter()
    {
        var executor = new GatewayAssemblyExecutor(maximumAssemblyBytes: 4 * 1024 * 1024);
        var assemblyBytes = File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location);

        var exception = Assert.Throws<GatewayExecutionException>(() => executor.Execute(
            assemblyBytes,
            typeof(UploadedAssemblyFixture).FullName!,
            nameof(UploadedAssemblyFixture.Invalid),
            "{}"));

        Assert.That(exception!.Code, Is.EqualTo("invalid_entry_point"));
    }

    [Test]
    public void Entry_point_exception_is_unwrapped_with_a_stable_error_code()
    {
        var executor = new GatewayAssemblyExecutor(maximumAssemblyBytes: 4 * 1024 * 1024);
        var assemblyBytes = File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location);

        var exception = Assert.Throws<GatewayExecutionException>(() => executor.Execute(
            assemblyBytes,
            typeof(UploadedAssemblyFixture).FullName!,
            nameof(UploadedAssemblyFixture.Throw),
            "{}"));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("entry_point_failed"));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(exception.InnerException!.Message, Is.EqualTo("fixture failure"));
        });
    }
}

public static class UploadedAssemblyFixture
{
    public static string Execute(string requestJson) =>
        Thread.CurrentThread.ManagedThreadId + ":" + requestJson;

    public static int Invalid(string requestJson) => requestJson.Length;

    public static string Throw(string requestJson) =>
        throw new InvalidOperationException("fixture failure");
}
