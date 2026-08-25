using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using NUnit.Framework;
using RimWorldDevGateway.Performance;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class DpaRuntimeAdapterTests
{
    private static readonly Lazy<Assembly> Fixture = new(() => Assembly.LoadFrom(FixtureAssemblyPath()));

    [SetUp]
    public void ResetFixture() => InvokeStatic("Analyzer.Profiling.Analyzer", "ResetForTests");

    [Test]
    public void Binding_is_absent_inactive_duplicate_and_changed_shape_safe()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DpaRuntimeAdapter.TryBind(false, new[] { Fixture.Value }, out _, out var inactive), Is.False);
            Assert.That(inactive, Does.Contain("inactive"));
            Assert.That(DpaRuntimeAdapter.TryBind(true, Array.Empty<Assembly>(), out _, out var absent), Is.False);
            Assert.That(absent, Does.Contain("not loaded"));
            Assert.That(DpaRuntimeAdapter.TryBind(true, new[] { Fixture.Value, Fixture.Value }, out _, out var duplicate), Is.False);
            Assert.That(duplicate, Does.Contain("exactly one"));
        });

        var source = new ReplacingDpaTypeSource(
            new AssemblyDpaTypeSource(Fixture.Value),
            "Analyzer.Profiling.Utility",
            typeof(ChangedUtility));
        Assert.That(DpaShapeBinder.TryBind(source, out _, out var changed), Is.False);
        Assert.That(changed, Does.Contain("PatchInternalMethod"));

        var oversized = Enumerable.Repeat(typeof(string).Assembly, DpaRuntimeAdapter.MaximumLoadedAssemblies + 1);
        Assert.That(DpaRuntimeAdapter.TryBind(true, oversized, out _, out var bounded), Is.False);
        Assert.That(bounded, Does.Contain(DpaRuntimeAdapter.MaximumLoadedAssemblies.ToString()));
    }

    [Test]
    public void Binding_retains_identity_and_uses_one_exact_native_internal_profile_lifecycle()
    {
        var binding = Bind();
        var method = typeof(DpaRuntimeAdapterTests).GetMethod(
            nameof(ProfiledTarget), BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.That(binding.TryBeginInternalCallDiagnostic(
            new[] { method }, DpaDiagnosticCategory.Tick, out var run, out var reason), Is.True, reason);
        Assert.That(GetStaticField<bool>("Analyzer.Modbase", "isPatched"), Is.True);

        var active = run!;
        try
        {
            Assert.That(active.TryStart(out var startReason), Is.True, startReason);
            InvokeInstance("Verse.TickManager", "DoSingleTick");
            Assert.That(active.TryStopAndCapture(
                "fixture-workload/v1",
                new[] { "DpaRuntimeAdapterTests::ProfiledTarget()" },
                new[] { "sample-start", "sample-end" },
                out var capture,
                out var stopReason),
                Is.True, stopReason);
            Assert.Multiple(() =>
            {
                Assert.That(capture!.Profiler, Is.EqualTo("dpa"));
                Assert.That(capture.Kind, Is.EqualTo("diagnostic"));
                Assert.That(capture.WorkloadVersion, Is.EqualTo("fixture-workload/v1"));
                Assert.That(capture.RequestedSelectors,
                    Is.EqualTo(new[] { "DpaRuntimeAdapterTests::ProfiledTarget()" }));
                Assert.That(capture.Selectors, Has.Length.EqualTo(1));
                Assert.That(capture.Entries, Has.Length.EqualTo(1));
                Assert.That(capture.Entries[0].Samples, Has.Length.EqualTo(1));
                Assert.That(capture.Entries[0].Samples[0].Calls, Is.EqualTo(4));
                Assert.That(capture.Entries[0].Samples[0].Milliseconds, Is.EqualTo(1.25d));
                Assert.That(capture.Entries[0].Method, Does.Contain("System.Math::Abs"));
                Assert.That(capture.Entries[0].Method, Is.Not.EqualTo(PerformanceMethodIdentity.Of(method)));
                Assert.That(capture.Entries[0].Type, Is.Empty);
            });
        }
        finally
        {
            CompleteCleanup(active);
        }

        Assert.Multiple(() =>
        {
            Assert.That(GetStaticProperty<bool>("Analyzer.Profiling.Analyzer", "CurrentlyProfiling"), Is.False);
            Assert.That(GetStaticCollectionCount("Analyzer.Profiling.ProfileController", "Profiles"), Is.Zero);
            Assert.That(GetStaticField<bool>("Analyzer.Settings", "disableThreadedPatching"), Is.False);
        });
    }

    [Test]
    public void Begin_rejects_dirty_native_state_and_bounded_or_unsupported_selectors()
    {
        var binding = Bind();
        InvokeStatic("Analyzer.Profiling.Analyzer", "BeginProfiling");
        Assert.That(binding.TryBeginInternalCallDiagnostic(
            new[] { Target() }, DpaDiagnosticCategory.Tick, out _, out var activeReason), Is.False);
        Assert.That(activeReason, Does.Contain("already profiling"));

        ResetFixture();
        binding = Bind();
        Assert.That(binding.TryBeginInternalCallDiagnostic(
            Enumerable.Repeat(Target(), DpaRuntimeBinding.MaximumSelectors + 1),
            DpaDiagnosticCategory.Tick, out _, out var boundedReason), Is.False);
        Assert.That(boundedReason, Does.Contain(DpaRuntimeBinding.MaximumSelectors.ToString()));

        Assert.That(binding.TryBeginInternalCallDiagnostic(
            new[] { typeof(GenericFixture<>).GetMethod(nameof(GenericFixture<int>.Call))! },
            DpaDiagnosticCategory.Tick, out _, out var genericReason), Is.False);
        Assert.That(genericReason, Does.Contain("generic"));
    }

    [Test]
    public void Snapshot_is_raw_bounded_and_dpa_diagnostic_only()
    {
        var binding = Bind();
        Assert.That(binding.TryBeginInternalCallDiagnostic(
            new[] { Target() }, DpaDiagnosticCategory.Update, out var run, out var reason), Is.True, reason);
        var active = run!;
        try
        {
            Assert.That(active.TryStart(out reason), Is.True, reason);
            InvokeInstance("Verse.Root_Play", "Update");
            Assert.That(active.TryStopAndCapture(
                    "fixture/v1", new[] { "DpaRuntimeAdapterTests::ProfiledTarget()" },
                    Array.Empty<string>(), out var capture, out reason),
                Is.True, reason);
            Assert.Multiple(() =>
            {
                Assert.That(capture!.BaselineEligible, Is.False);
                Assert.That(capture.Schema, Is.EqualTo(DpaDiagnosticCapture.SchemaValue));
                Assert.That(capture.Assembly.Sha256, Has.Length.EqualTo(64));
                Assert.That(capture.Entries.SelectMany(entry => entry.Samples), Has.All.Matches<DpaDiagnosticSample>(
                    sample => sample.Index >= 0 && sample.Index < 2000));
            });
            using var stream = new MemoryStream();
            new DataContractJsonSerializer(typeof(DpaDiagnosticCapture)).WriteObject(stream, capture);
            Assert.That(Encoding.UTF8.GetString(stream.ToArray()),
                Does.Contain("\"schema\":\"RimWorldDevGateway\\/DpaDiagnostic\\/v1\"")
                    .And.Contain("\"baselineEligible\":false"));
        }
        finally
        {
            CompleteCleanup(active);
        }
    }

    [Test]
    public void Snapshot_rejects_a_started_run_without_a_native_measurement_cycle()
    {
        var binding = Bind();
        Assert.That(binding.TryBeginInternalCallDiagnostic(
            new[] { Target() }, DpaDiagnosticCategory.Tick, out var run, out var reason), Is.True, reason);
        try
        {
            Assert.That(run!.TryStart(out reason), Is.True, reason);
            Assert.That(run.TryStopAndCapture(
                "fixture/v1", new[] { "DpaRuntimeAdapterTests::ProfiledTarget()" },
                Array.Empty<string>(), out _, out reason), Is.False);
            Assert.That(reason, Does.Contain("no invoked raw internal-callee entry"));
        }
        finally
        {
            CompleteCleanup(run!);
        }
    }

    [Test]
    public void Cleanup_failure_retains_a_retryable_run_and_second_dispose_clears_only_owned_state()
    {
        var binding = Bind();
        Assert.That(binding.TryBeginInternalCallDiagnostic(
            new[] { Target() }, DpaDiagnosticCategory.Tick, out var run, out var reason), Is.True, reason);
        Assert.That(run!.TryStart(out reason), Is.True, reason);
        SetStaticField("Analyzer.Profiling.Analyzer", "ThrowOnCleanupForTests", true);

        var failure = Assert.Throws<InvalidOperationException>(() => run.Dispose());
        Assert.That(failure!.Message, Does.Contain("pending").Or.Contain("incomplete"));
        Assert.That((bool)InvokeStaticResult("Analyzer.Profiling.Analyzer", "WaitForCleanupStartForTests"), Is.True);
        Assert.That(SpinWait.SpinUntil(
            () => !GetStaticProperty<bool>("Analyzer.Profiling.Analyzer", "CurrentlyCleaningUp"),
            TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(GetStaticCollectionCount("Analyzer.Profiling.ProfileController", "Profiles"), Is.EqualTo(1));

        SetStaticField("Analyzer.Profiling.Analyzer", "ThrowOnCleanupForTests", false);
        run.RequestCleanup();
        CompleteCleanup(run);
        Assert.Multiple(() =>
        {
            Assert.That(GetStaticCollectionCount("Analyzer.Profiling.ProfileController", "Profiles"), Is.Zero);
            Assert.That(GetStaticField<bool>("Analyzer.Settings", "disableThreadedPatching"), Is.False);
        });
    }

    [Test]
    public void Partial_native_registration_is_rejected_and_retains_owner_until_cleanup_is_observed()
    {
        var target = Target();
        SetStaticField("Analyzer.Profiling.Utility", "FailPatchForTests", target);
        SetStaticField("Analyzer.Profiling.Analyzer", "ThrowOnCleanupForTests", true);
        var binding = Bind();

        Assert.That(binding.TryBeginInternalCallDiagnostic(
            new[] { target }, DpaDiagnosticCategory.Tick, out var retained, out var reason), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(reason, Does.Contain("did not apply exactly one").And.Contain("cleanup"));
            Assert.That(retained, Is.Not.Null);
        });

        SetStaticField("Analyzer.Profiling.Analyzer", "ThrowOnCleanupForTests", false);
        Assert.That(SpinWait.SpinUntil(
            () => !GetStaticProperty<bool>("Analyzer.Profiling.Analyzer", "CurrentlyCleaningUp"),
            TimeSpan.FromSeconds(5)), Is.True);
        retained!.RequestCleanup();
        CompleteCleanup(retained);
    }

    [Test]
    public void Cleanup_cannot_confirm_before_the_native_worker_starts()
    {
        var binding = Bind();
        Assert.That(binding.TryBeginInternalCallDiagnostic(
            new[] { Target() }, DpaDiagnosticCategory.Tick, out var run, out var reason), Is.True, reason);
        InvokeStatic("Analyzer.Profiling.Analyzer", "HoldCleanupForTests");
        run!.RequestCleanup();

        Assert.Multiple(() =>
        {
            Assert.That(run.TryConfirmCleanup(out var pending), Is.False);
            Assert.That(pending, Does.Contain("cleaning"));
            Assert.That(GetStaticProperty<bool>("Analyzer.Profiling.Analyzer", "CurrentlyCleaningUp"), Is.True);
        });

        InvokeStatic("Analyzer.Profiling.Analyzer", "ReleaseCleanupForTests");
        CompleteCleanup(run);
    }

    [Test]
    public void Begin_rejects_multiple_outer_selectors_because_DPA_only_activates_the_last()
    {
        var methods = typeof(DpaRuntimeAdapterTests).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(method => method.Name == nameof(OverloadedTarget))
            .OrderBy(method => method.GetParameters().Length)
            .ToArray();
        var binding = Bind();
        Assert.That(binding.TryBeginInternalCallDiagnostic(
            methods, DpaDiagnosticCategory.Tick, out _, out var reason), Is.False);
        Assert.That(reason, Does.Contain("selector count").And.Contain("1"));
    }

    private static DpaRuntimeBinding Bind()
    {
        Assert.That(DpaRuntimeAdapter.TryBind(true, LoadedWithFixture(), out var binding, out var reason),
            Is.True, reason);
        return binding!;
    }

    private static Assembly[] LoadedWithFixture() => AppDomain.CurrentDomain.GetAssemblies()
        .Where(assembly => !string.Equals(assembly.GetName().Name, "PerformanceAnalyzer", StringComparison.Ordinal))
        .Concat(new[] { Fixture.Value })
        .ToArray();

    private static MethodInfo Target() => typeof(DpaRuntimeAdapterTests).GetMethod(
        nameof(ProfiledTarget), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static void ProfiledTarget() => Math.Abs(-1);
    private static void OverloadedTarget() => Math.Abs(-3);
    private static void OverloadedTarget(int value) => Math.Abs(value);

    private sealed class GenericFixture<T>
    {
        public static void Call() { }
    }

    private sealed class ChangedUtility
    {
        public static bool PatchInternalMethod(MethodInfo method, int category) => true;
    }

    private static string FixtureAssemblyPath()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln")))
            directory = directory.Parent;
        if (directory is null) throw new DirectoryNotFoundException("Could not locate the repository root.");
        return Path.Combine(directory.FullName, "tests", "Fixtures", "Dpa.ValidFixtures", "bin",
            "Release", "net48", "PerformanceAnalyzer.dll");
    }

    private static void InvokeStatic(string typeName, string methodName) =>
        Fixture.Value.GetType(typeName, true)!.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(null, null);

    private static object InvokeStaticResult(string typeName, string methodName) =>
        Fixture.Value.GetType(typeName, true)!.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(null, null)!;

    private static void InvokeInstance(string typeName, string methodName)
    {
        var type = Fixture.Value.GetType(typeName, true)!;
        type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(Activator.CreateInstance(type), null);
    }

    private static void CompleteCleanup(DpaRuntimeRun run)
    {
        run.RequestCleanup();
        var reason = string.Empty;
        Assert.That(SpinWait.SpinUntil(() => run.TryConfirmCleanup(out reason), TimeSpan.FromSeconds(5)),
            Is.True, reason);
        Assert.DoesNotThrow(() => run.Dispose());
    }

    private static T GetStaticProperty<T>(string typeName, string propertyName) =>
        (T)Fixture.Value.GetType(typeName, true)!.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null, null)!;

    private static T GetStaticField<T>(string typeName, string fieldName) =>
        (T)Fixture.Value.GetType(typeName, true)!.GetField(fieldName, BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null)!;

    private static void SetStaticField(string typeName, string fieldName, object value) =>
        Fixture.Value.GetType(typeName, true)!.GetField(fieldName, BindingFlags.Static | BindingFlags.Public)!
            .SetValue(null, value);

    private static int GetStaticCollectionCount(string typeName, string propertyName)
    {
        var value = Fixture.Value.GetType(typeName, true)!.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null, null)!;
        return (int)value.GetType().GetProperty("Count")!.GetValue(value, null)!;
    }
}

internal sealed class ReplacingDpaTypeSource : IDpaTypeSource
{
    private readonly IDpaTypeSource inner;
    private readonly string name;
    private readonly Type replacement;

    public ReplacingDpaTypeSource(IDpaTypeSource inner, string name, Type replacement)
    {
        this.inner = inner;
        this.name = name;
        this.replacement = replacement;
    }

    public Type? Resolve(string fullName) => fullName == name ? replacement : inner.Resolve(fullName);
}
