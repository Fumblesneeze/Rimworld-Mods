using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;

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

    [Test]
    public void Context_entry_receives_correlation_cancellation_and_session_extension_hook()
    {
        var extensions = new RecordingRuntimeExtensions();
        var executor = new GatewayAssemblyExecutor(
            maximumAssemblyBytes: 4 * 1024 * 1024,
            maximumResultUtf8Bytes: 1024,
            runtimeExtensions: extensions);
        var assemblyBytes = File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location);
        using var cancellation = new CancellationTokenSource();

        var result = executor.Execute(
            assemblyBytes,
            typeof(UploadedAssemblyFixture).FullName!,
            nameof(UploadedAssemblyFixture.ExecuteWithContext),
            "{\"value\":42}",
            requestId: "assembly-context-test",
            cancellation.Token);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo("assembly-context-test:True:{\"value\":42}"));
            Assert.That(result.Truncated, Is.False);
            Assert.That(result.OriginalUtf8Bytes, Is.EqualTo(result.Value.Length));
            Assert.That(extensions.Descriptor, Is.Not.Null);
            Assert.That(extensions.Descriptor!.Name, Is.EqualTo("fixture.echo"));
            Assert.That(extensions.Descriptor.Version, Is.EqualTo("1"));
            Assert.That(extensions.Descriptor.Mutating, Is.False);
            Assert.That(extensions.Handler, Is.Not.Null);
            Assert.That(
                extensions.Handler!("{\"message\":\"hello\"}", cancellation.Token),
                Is.EqualTo("automation:{\"message\":\"hello\"}"));
        });
    }

    [Test]
    public void Context_entry_observes_cooperative_cancellation_without_wrapping_it()
    {
        var executor = new GatewayAssemblyExecutor(
            maximumAssemblyBytes: 4 * 1024 * 1024,
            maximumResultUtf8Bytes: 1024,
            runtimeExtensions: new RecordingRuntimeExtensions());
        var assemblyBytes = File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location);
        using var cancellation = new CancellationTokenSource();

        var execution = Task.Run(() => executor.Execute(
            assemblyBytes,
            typeof(UploadedAssemblyFixture).FullName!,
            nameof(UploadedAssemblyFixture.WaitForCancellation),
            "{}",
            requestId: "assembly-cancellation-test",
            cancellation.Token));
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

        Assert.That(
            () => execution.GetAwaiter().GetResult(),
            Throws.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void Result_is_normalized_to_a_deterministic_utf8_safe_bound()
    {
        const int maximumResultUtf8Bytes = 24;
        var executor = new GatewayAssemblyExecutor(
            maximumAssemblyBytes: 4 * 1024 * 1024,
            maximumResultUtf8Bytes,
            runtimeExtensions: new RecordingRuntimeExtensions());
        var assemblyBytes = File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location);

        var first = executor.Execute(
            assemblyBytes,
            typeof(UploadedAssemblyFixture).FullName!,
            nameof(UploadedAssemblyFixture.LargeUnicodeResult),
            "{}",
            requestId: "assembly-result-test",
            CancellationToken.None);
        var second = executor.Execute(
            assemblyBytes,
            typeof(UploadedAssemblyFixture).FullName!,
            nameof(UploadedAssemblyFixture.LargeUnicodeResult),
            "{}",
            requestId: "assembly-result-test-2",
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Value, Is.EqualTo(second.Value));
            Assert.That(first.Truncated, Is.True);
            Assert.That(first.OriginalUtf8Bytes, Is.GreaterThan(maximumResultUtf8Bytes));
            Assert.That(System.Text.Encoding.UTF8.GetByteCount(first.Value), Is.LessThanOrEqualTo(maximumResultUtf8Bytes));
            Assert.That(first.Value, Does.EndWith(GatewayAssemblyExecutionResult.TruncationMarker));
            Assert.That(
                System.Text.Encoding.UTF8.GetString(
                    System.Text.Encoding.UTF8.GetBytes(first.Value)),
                Is.EqualTo(first.Value));
        });
    }

    [Test]
    public void Supported_legacy_and_context_overloads_are_rejected_as_ambiguous()
    {
        var executor = new GatewayAssemblyExecutor(
            maximumAssemblyBytes: 4 * 1024 * 1024,
            maximumResultUtf8Bytes: 1024,
            runtimeExtensions: new RecordingRuntimeExtensions());
        var assemblyBytes = File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location);

        var exception = Assert.Throws<GatewayExecutionException>(() => executor.Execute(
            assemblyBytes,
            typeof(UploadedAssemblyFixture).FullName!,
            nameof(UploadedAssemblyFixture.Ambiguous),
            "{}",
            requestId: "assembly-ambiguous-test",
            CancellationToken.None));

        Assert.That(exception!.Code, Is.EqualTo("invalid_entry_point"));
    }

    [Test]
    public void Generic_entry_point_is_rejected_without_invocation()
    {
        var executor = new GatewayAssemblyExecutor(4 * 1024 * 1024);
        var assemblyBytes = File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location);

        var exception = Assert.Throws<GatewayExecutionException>(() => executor.Execute(
            assemblyBytes,
            typeof(UploadedAssemblyFixture).FullName!,
            nameof(UploadedAssemblyFixture.Generic),
            "{}"));

        Assert.That(exception!.Code, Is.EqualTo("invalid_entry_point"));
    }

    [Test]
    public void Cancellation_before_load_leaves_upload_count_unchanged()
    {
        var executor = new GatewayAssemblyExecutor(4 * 1024 * 1024);
        var assemblyBytes = File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => executor.Execute(
                    assemblyBytes,
                    typeof(UploadedAssemblyFixture).FullName!,
                    nameof(UploadedAssemblyFixture.ExecuteWithContext),
                    "{}",
                    "cancel-before-load",
                    cancellation.Token),
                Throws.TypeOf<OperationCanceledException>());
            Assert.That(executor.UploadCount, Is.Zero);
        });
    }

    [Test]
    public void Public_instance_overload_makes_name_based_entry_point_ambiguous()
    {
        var executor = new GatewayAssemblyExecutor(4 * 1024 * 1024);
        var assemblyBytes = File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location);

        var exception = Assert.Throws<GatewayExecutionException>(() => executor.Execute(
            assemblyBytes,
            typeof(UploadedAssemblyMixedOverloadFixture).FullName!,
            nameof(UploadedAssemblyMixedOverloadFixture.Execute),
            "{}"));

        Assert.That(exception!.Code, Is.EqualTo("invalid_entry_point"));
    }

    private sealed class RecordingRuntimeExtensions : IGatewayAssemblyRuntimeExtensions
    {
        public GatewayAssemblyAutomationDescriptor? Descriptor { get; private set; }

        public GatewayAssemblyAutomationHandler? Handler { get; private set; }

        public void RegisterSessionAutomation(
            GatewayAssemblyAutomationDescriptor descriptor,
            GatewayAssemblyAutomationHandler handler)
        {
            Descriptor = descriptor;
            Handler = handler;
        }
    }
}

public static class UploadedAssemblyFixture
{
    public static string Execute(string requestJson) =>
        Thread.CurrentThread.ManagedThreadId + ":" + requestJson;

    public static int Invalid(string requestJson) => requestJson.Length;

    public static string Throw(string requestJson) =>
        throw new InvalidOperationException("fixture failure");

    public static string ExecuteWithContext(
        string requestJson,
        GatewayAssemblyExecutionContext context)
    {
        context.RuntimeExtensions.RegisterSessionAutomation(
            new GatewayAssemblyAutomationDescriptor(
                "fixture.echo",
                "1",
                "Echoes the normalized automation arguments.",
                mutating: false),
            (argumentsJson, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return "automation:" + argumentsJson;
            });
        return context.RequestId + ":" + context.CancellationToken.CanBeCanceled + ":" + requestJson;
    }

    public static string WaitForCancellation(
        string requestJson,
        GatewayAssemblyExecutionContext context)
    {
        context.CancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
        context.CancellationToken.ThrowIfCancellationRequested();
        return requestJson;
    }

    public static string LargeUnicodeResult(
        string requestJson,
        GatewayAssemblyExecutionContext context) =>
        string.Concat(Enumerable.Repeat("🍳\u0001", 64));

    public static string Ambiguous(string requestJson) => requestJson;

    public static string Ambiguous(
        string requestJson,
        GatewayAssemblyExecutionContext context) => requestJson + context.RequestId;

    public static string Generic<T>(string requestJson) => typeof(T).Name + requestJson;
}

public sealed class UploadedAssemblyMixedOverloadFixture
{
    public static string Execute(string requestJson) => requestJson;

    public string Execute(string requestJson, int suffix) => requestJson + suffix;
}
