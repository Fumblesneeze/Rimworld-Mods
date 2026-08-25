using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RimWorldDevGateway.Performance;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class CircinusRuntimeAdapterTests
{
    private static readonly Lazy<Assembly> Fixture = new(() =>
        Assembly.LoadFrom(FixtureAssemblyPath()));

    [SetUp]
    public void ResetFixture()
    {
        InvokeStatic("Circinus.Bootstrap.CircinusMod", "ResetForTests");
        InvokeInstance(CurrentRecorder(), "ResetForTests");
        InvokeStatic("Circinus.Profiling.Instrumenter", "ResetForTests");
        InvokeStatic("Circinus.Profiling.ProfilerRegistry", "ResetForTests");
        InvokeStatic("Circinus.Identity.HarmonyIndex", "ResetForTests");
    }

    [Test]
    public void Binding_is_inactive_and_absent_safe_and_rejects_duplicate_assemblies()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CircinusRuntimeAdapter.TryBind(
                packageActive: false,
                new[] { Fixture.Value },
                out var inactive,
                out var inactiveReason), Is.False);
            Assert.That(inactive, Is.Null);
            Assert.That(inactiveReason, Does.Contain("inactive"));

            Assert.That(CircinusRuntimeAdapter.TryBind(
                packageActive: true,
                Array.Empty<Assembly>(),
                out var absent,
                out var absentReason), Is.False);
            Assert.That(absent, Is.Null);
            Assert.That(absentReason, Does.Contain("not loaded"));

            Assert.That(CircinusRuntimeAdapter.TryBind(
                packageActive: true,
                new[] { Fixture.Value, Fixture.Value },
                out var duplicate,
                out var duplicateReason), Is.False);
            Assert.That(duplicate, Is.Null);
            Assert.That(duplicateReason, Does.Contain("exactly one"));
        });
    }

    [Test]
    public void Binding_records_identity_and_validates_the_exact_supported_shape()
    {
        var binding = Bind();

        Assert.Multiple(() =>
        {
            Assert.That(binding.Identity.AssemblyName, Is.EqualTo("Circinus"));
            Assert.That(binding.Identity.AssemblyIdentity, Does.StartWith("Circinus,"));
            Assert.That(binding.Identity.ModuleVersionId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(binding.Identity.Length, Is.GreaterThan(0));
            Assert.That(binding.Identity.Sha256, Has.Length.EqualTo(64));
            Assert.That(binding.SchemaMajor, Is.EqualTo(1));
            Assert.That(binding.SchemaMinor, Is.EqualTo(15));
            Assert.That(binding.ValidateLocalOnlySettings(out var reason), Is.True, reason);
        });
    }

    [Test]
    public void Shape_binding_rejects_a_changed_exact_member_signature()
    {
        var source = new ReplacingTypeSource(
            new AssemblyCircinusTypeSource(Fixture.Value),
            "Circinus.Contract.RunDocument",
            typeof(ChangedRunDocument));

        Assert.That(CircinusShapeBinder.TryBind(source, out var shape, out var reason), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(shape, Is.Null);
            Assert.That(reason, Does.Contain("RunDocument.ToJson").And.Contain("String"));
        });
    }

    [Test]
    [TestCase("autoStartProfiler")]
    [TestCase("autoArmProfiler")]
    [TestCase("autoProfile")]
    [TestCase("showWarmupWindow")]
    [TestCase("ingestEnabled")]
    [TestCase("autoRecord")]
    public void Unsafe_boolean_automatic_or_sharing_settings_fail_before_starting_a_run(string fieldName)
    {
        var binding = Bind();
        SetSettingsField(fieldName, true);

        Assert.That(binding.TryBeginRun("unsafe-run", out var run, out var reason), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(run, Is.Null);
            Assert.That(reason, Does.Contain(fieldName));
            Assert.That(GetProperty<bool>(CurrentRecorder(), "Recording"), Is.False);
        });
    }

    [Test]
    public void Unsafe_prompt_warmup_or_armed_target_settings_fail_before_starting_a_run()
    {
        var binding = Bind();
        SetSettingsField("autoProfileAsked", 0);
        Assert.That(binding.TryBeginRun("prompt-run", out _, out var promptReason), Is.False);
        Assert.That(promptReason, Does.Contain("autoProfileAsked"));

        ResetFixture();
        binding = Bind();
        SetSettingsField("warmupSeconds", 1);
        Assert.That(binding.TryBeginRun("warmup-run", out _, out var warmupReason), Is.False);
        Assert.That(warmupReason, Does.Contain("warmupSeconds"));

        ResetFixture();
        binding = Bind();
        SetSettingsField("consentVersion", 0);
        Assert.That(binding.TryBeginRun("consent-run", out _, out var consentReason), Is.False);
        Assert.That(consentReason, Does.Contain("consentVersion"));

        ResetFixture();
        binding = Bind();
        var settings = GetStaticField<object>("Circinus.Bootstrap.CircinusMod", "Settings");
        var armed = (System.Collections.IList)settings.GetType().GetField("armedTargetKeys")!.GetValue(settings)!;
        armed.Add("stale.target");
        Assert.That(binding.TryBeginRun("armed-run", out _, out var armedReason), Is.False);
        Assert.That(armedReason, Does.Contain("armedTargetKeys"));
    }

    [Test]
    public void Run_rejects_duplicate_stale_and_overflow_registration_and_cleans_owned_state()
    {
        var binding = Bind();
        Assert.That(binding.TryBeginRun("registration-run", out var run, out var startReason), Is.True, startReason);
        var method = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(ProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;

        using (var activeRun = run!)
        {
            Assert.That(activeRun.RunId, Is.Not.EqualTo("registration-run").And.Not.Empty);
            Assert.That(activeRun.TryArmMethod(method, "fixture", out var armReason), Is.True, armReason);
            Assert.That(activeRun.TryArmMethod(method, "fixture", out var duplicateReason), Is.False);
            Assert.That(duplicateReason, Does.Contain("already registered"));
            Assert.That(activeRun.TryArmTarget("missing.target", out var staleReason), Is.False);
            Assert.That(staleReason, Does.Contain("did not resolve"));
            Assert.That(activeRun.TryArmTarget("fixture.overflow", out var overflowReason), Is.False);
            Assert.That(overflowReason, Does.Contain("1000"));
            Assert.That(activeRun.TryArmTarget("fixture.valid", out var targetReason), Is.True, targetReason);
            activeRun.SetSampling(enabled: true);
        }

        Assert.Multiple(() =>
        {
            Assert.That(InvokeStatic<bool>("Circinus.Profiling.Instrumenter", "IsPatched", method), Is.False);
            Assert.That(GetStaticField<bool>("Circinus.Profiling.ProfilerRegistry", "Enabled"), Is.False);
            Assert.That(GetStaticField<bool>("Circinus.Profiling.ProfilerRegistry", "Recording"), Is.False);
            Assert.That(GetProperty<bool>(CurrentRecorder(), "Recording"), Is.False);
        });
    }

    [Test]
    public void Complete_resolved_selection_is_hand_armed_and_owned_by_one_run()
    {
        var method = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(ProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var selection = PerformanceMethodSelectorResolver.Resolve(
            new[] { typeof(CircinusRuntimeAdapterTests).Assembly },
            new EmptyHarmonyCatalog(),
            new[]
            {
                new PerformanceSelectionRequest(
                    PerformanceMethodSelectorKind.Method,
                    typeof(CircinusRuntimeAdapterTests).FullName + "::" + nameof(ProfiledFixtureMethod),
                    "fixture"),
                new PerformanceSelectionRequest(
                    PerformanceMethodSelectorKind.CircinusTarget,
                    "fixture.valid",
                    "target")
            });
        var binding = Bind();
        Assert.That(binding.TryBeginRun("resolved-selection", out var run, out var startReason),
            Is.True, startReason);

        using (var activeRun = run!)
        {
            Assert.That(activeRun.TryArmSelection(selection, out var armReason), Is.True, armReason);
            Assert.That(InvokeStatic<bool>("Circinus.Profiling.Instrumenter", "IsPatched", method), Is.True);
            var profiler = InvokeStatic<object>("Circinus.Profiling.ProfilerRegistry", "Find", method);
            Assert.That((bool)profiler.GetType().GetField("HandArmed")!.GetValue(profiler)!, Is.True);
        }

        Assert.That(InvokeStatic<bool>("Circinus.Profiling.Instrumenter", "IsPatched", method), Is.False);
    }

    [Test]
    public void Failed_post_start_identity_acquisition_stops_the_native_recorder()
    {
        var binding = Bind();
        SetRecorderField("ThrowOnActiveId", true);

        Assert.That(binding.TryBeginRun("identity-failure", out var run, out var reason), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(run, Is.Null);
            Assert.That(reason, Does.Contain("active id failure"));
            Assert.That(GetProperty<bool>(CurrentRecorder(), "Recording"), Is.False);
            Assert.That((int)CurrentRecorder().GetType().GetField("StopCount")!.GetValue(CurrentRecorder())!,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void Failed_registration_releases_only_native_state_that_the_run_may_have_acquired()
    {
        var binding = Bind();
        Assert.That(binding.TryBeginRun("registration-failure", out var run, out var startReason),
            Is.True, startReason);
        var externalMethod = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(ProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var partialMethod = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(SecondProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;

        using (var activeRun = run!)
        {
            Assert.That(InvokeStatic<bool>(
                "Circinus.Profiling.Instrumenter",
                "ArmMethod",
                externalMethod,
                "external"), Is.True);
            SetStaticField("Circinus.Profiling.Instrumenter", "ThrowBeforeArmMethodForTests", true);
            Assert.That(activeRun.TryArmMethod(externalMethod, "run", out var beforeReason), Is.False);
            Assert.That(beforeReason, Does.Contain("already armed outside this run"));
            Assert.That(InvokeStatic<bool>(
                "Circinus.Profiling.Instrumenter",
                "IsPatched",
                externalMethod), Is.True,
                "a failure before native acquisition must not remove unrelated state");

            SetStaticField("Circinus.Profiling.Instrumenter", "ThrowBeforeArmMethodForTests", false);
            SetStaticField("Circinus.Profiling.Instrumenter", "ThrowAfterArmMethodForTests", true);
            Assert.That(activeRun.TryArmMethod(partialMethod, "run", out var afterReason), Is.False);
            Assert.That(afterReason, Does.Contain("after failure"));
            Assert.That(InvokeStatic<bool>(
                "Circinus.Profiling.Instrumenter",
                "IsPatched",
                partialMethod), Is.False,
                "a failure after native acquisition must release the partial method arm");

            SetStaticField("Circinus.Profiling.Instrumenter", "ThrowAfterArmMethodForTests", false);
            SetStaticField("Circinus.Profiling.Instrumenter", "ThrowAfterArmTargetForTests", true);
            Assert.That(activeRun.TryArmTarget("fixture.valid", out var targetReason), Is.False);
            Assert.That(targetReason, Does.Contain("target after failure"));
            Assert.That(GetStaticProperty<int>(
                "Circinus.Profiling.Instrumenter",
                "ActiveTargetCountForTests"), Is.Zero,
                "a throwing native target arm must release its partial target state");
        }
    }

    [Test]
    public void Settings_are_revalidated_before_arming_or_enabling_sampling()
    {
        var binding = Bind();
        Assert.That(binding.TryBeginRun("settings-revalidation", out var run, out var startReason),
            Is.True, startReason);
        var method = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(ProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;

        using (var activeRun = run!)
        {
            SetSettingsField("ingestEnabled", true);
            Assert.That(activeRun.TryArmMethod(method, "fixture", out var armReason), Is.False);
            Assert.That(armReason, Does.Contain("ingestEnabled"));
            Assert.That(InvokeStatic<bool>(
                "Circinus.Profiling.Instrumenter",
                "IsPatched",
                method), Is.False);
            Assert.That(() => activeRun.SetSampling(enabled: true),
                Throws.InvalidOperationException.With.Message.Contains("ingestEnabled"));
            Assert.That(GetStaticField<bool>("Circinus.Profiling.ProfilerRegistry", "Enabled"), Is.False);
            Assert.That(GetStaticField<bool>("Circinus.Profiling.ProfilerRegistry", "Recording"), Is.False);
        }
    }

    [Test]
    public void Curated_target_rejects_a_native_arm_that_is_not_hand_armed_and_cleans_it()
    {
        var binding = Bind();
        Assert.That(binding.TryBeginRun("target-hand-arm", out var run, out var startReason),
            Is.True, startReason);
        SetStaticField("Circinus.Profiling.Instrumenter", "IgnoreTargetHandArmedForTests", true);

        using (var activeRun = run!)
        {
            Assert.That(activeRun.TryArmTarget("fixture.valid", out var armReason), Is.False);
            Assert.That(armReason, Does.Contain("HandArmed"));
            Assert.That(GetStaticProperty<int>(
                "Circinus.Profiling.Instrumenter",
                "ActiveTargetCountForTests"), Is.Zero);
        }
    }

    [Test]
    public void Stop_rejects_schema_or_persisted_identity_drift_and_accepts_one_exact_capture()
    {
        var binding = Bind();
        Assert.That(binding.TryBeginRun("capture-run", out var run, out var startReason), Is.True, startReason);
        using (var activeRun = run!)
        {
            activeRun.AddMarker("sample", "start");
            Assert.That(activeRun.TryStopAndCapture(
                _ => "{\"id\":\"other-run\",\"schemaMajor\":1,\"schemaMinor\":15}",
                out var mismatch,
                out var mismatchReason), Is.False);
            Assert.That(mismatch, Is.Null);
            Assert.That(mismatchReason, Does.Contain("persisted run identity"));
        }

        ResetFixture();
        binding = Bind();
        SetRecorderField("NextSchemaMajor", 2);
        Assert.That(binding.TryBeginRun("schema-run", out run, out startReason), Is.False);
        Assert.That(run, Is.Null);
        Assert.That(startReason, Does.Contain("unsupported schema major"));
        Assert.That(GetProperty<bool>(CurrentRecorder(), "Recording"), Is.False);

        ResetFixture();
        binding = Bind();
        Assert.That(binding.TryBeginRun("raw-identity-run", out run, out startReason), Is.True, startReason);
        using (var activeRun = run!)
        {
            SetDocumentField("JsonIdOverride", "other-raw-id");
            Assert.That(activeRun.TryStopAndCapture(
                Persisted(activeRun),
                out var changed,
                out var changedReason), Is.False);
            Assert.That(changed, Is.Null);
            Assert.That(changedReason, Does.Contain("in-memory JSON identity"));
        }

        ResetFixture();
        binding = Bind();
        Assert.That(binding.TryBeginRun("accepted-run", out run, out startReason), Is.True, startReason);
        using (var activeRun = run!)
        {
            var acceptedId = activeRun.RunId;
            var persisted = "{\"id\":\"" + acceptedId + "\",\"schemaMajor\":1,\"schemaMinor\":15}";
            Assert.That(activeRun.TryStopAndCapture(id =>
            {
                Assert.That(id, Is.EqualTo(acceptedId));
                Assert.That(GetProperty<bool>(CurrentRecorder(), "Recording"), Is.False);
                return persisted;
            }, out var capture, out var captureReason), Is.True, captureReason);
            Assert.Multiple(() =>
            {
                Assert.That(capture!.RunId, Is.EqualTo(acceptedId));
                Assert.That(capture.SchemaMajor, Is.EqualTo(1));
                Assert.That(capture.SchemaMinor, Is.EqualTo(15));
                Assert.That(capture.InMemoryJson, Does.Contain(acceptedId));
                Assert.That(capture.PersistedJson, Is.EqualTo(persisted));
            });
        }
    }

    [Test]
    public void Capture_correlates_one_nonempty_method_sidecar_and_retains_one_empty_sidecar()
    {
        var method = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(ProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var binding = Bind();
        Assert.That(binding.TryBeginRun("correlation-run", out var run, out var startReason), Is.True, startReason);
        using (var activeRun = run!)
        {
            Assert.That(activeRun.TryArmMethod(method, "fixture", out var armReason), Is.True, armReason);
            var key = MethodKey(method);
            SeedProfiler(method, empty: false);
            SetDocumentRows(MethodRow(key), string.Empty);

            Assert.That(activeRun.TryStopAndCapture(Persisted(activeRun), out var capture, out var reason), Is.True, reason);
            Assert.Multiple(() =>
            {
                Assert.That(capture!.Sidecars, Has.Count.EqualTo(1));
                Assert.That(capture.Sidecars[0].RowKind, Is.EqualTo(CircinusRowKind.Method));
                Assert.That(capture.Sidecars[0].RowKey, Is.EqualTo(key));
                Assert.That(capture.Sidecars[0].NoRowReason, Is.Null);
                Assert.That(capture.Sidecars[0].TotalCalls, Is.EqualTo(12));
                Assert.That(capture.Sidecars[0].TotalTimedCalls, Is.EqualTo(7));
            });
        }

        ResetFixture();
        binding = Bind();
        Assert.That(binding.TryBeginRun("empty-run", out run, out startReason), Is.True, startReason);
        using (var activeRun = run!)
        {
            Assert.That(activeRun.TryArmMethod(method, "fixture", out var armReason), Is.True, armReason);
            Assert.That(activeRun.TryStopAndCapture(Persisted(activeRun), out var capture, out var reason), Is.True, reason);
            Assert.Multiple(() =>
            {
                Assert.That(capture!.Sidecars, Has.Count.EqualTo(1));
                Assert.That(capture.Sidecars[0].Empty, Is.True);
                Assert.That(capture.Sidecars[0].NoRowReason, Is.EqualTo("empty-or-uninvoked"));
            });
        }
    }

    [TestCase("nonempty-missing")]
    [TestCase("empty-with-row")]
    [TestCase("duplicate-row")]
    public void Capture_rejects_missing_unexpected_or_duplicate_method_rows(string failure)
    {
        var method = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(ProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var binding = Bind();
        Assert.That(binding.TryBeginRun("invalid-correlation", out var run, out var startReason), Is.True, startReason);
        using (var activeRun = run!)
        {
            Assert.That(activeRun.TryArmMethod(method, "fixture", out var armReason), Is.True, armReason);
            var key = MethodKey(method);
            switch (failure)
            {
                case "nonempty-missing":
                    SeedProfiler(method, empty: false);
                    break;
                case "empty-with-row":
                    SetDocumentRows(MethodRow(key), string.Empty);
                    break;
                case "duplicate-row":
                    SeedProfiler(method, empty: false);
                    SetDocumentRows(MethodRow(key) + "," + MethodRow(key), string.Empty);
                    break;
            }

            Assert.That(activeRun.TryStopAndCapture(Persisted(activeRun), out var capture, out var reason), Is.False);
            Assert.That(capture, Is.Null);
            Assert.That(reason, Does.Contain("correlation").And.Contain(key));
        }
    }

    [Test]
    public void Capture_uses_the_exact_native_patch_key_for_a_patch_method_sidecar()
    {
        var method = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(ProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        const string patchKey = "fixture-patch-key";
        InvokeStatic("Circinus.Identity.HarmonyIndex", "RegisterPatchForTests", method, patchKey);
        var binding = Bind();
        Assert.That(binding.TryBeginRun("patch-correlation", out var run, out var startReason), Is.True, startReason);
        using (var activeRun = run!)
        {
            Assert.That(activeRun.TryArmMethod(method, "fixture", out var armReason), Is.True, armReason);
            SeedProfiler(method, empty: false);
            SetDocumentRows(string.Empty, PatchRow(patchKey));

            Assert.That(activeRun.TryStopAndCapture(Persisted(activeRun), out var capture, out var reason), Is.True, reason);
            Assert.Multiple(() =>
            {
                Assert.That(capture!.Sidecars.Single().RowKind, Is.EqualTo(CircinusRowKind.Patch));
                Assert.That(capture.Sidecars.Single().RowKey, Is.EqualTo(patchKey));
                Assert.That(capture.Sidecars.Single().CanSkip, Is.True);
            });
        }
    }

    [Test]
    public void Shared_patch_sidecar_uses_Circinus_first_row_and_retains_native_ambiguity()
    {
        var method = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(ProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        const string firstPatchKey = "fixture-first-patch-key";
        InvokeStatic(
            "Circinus.Identity.HarmonyIndex",
            "RegisterPatchPairForTests",
            method,
            firstPatchKey,
            "fixture-second-patch-key");
        var binding = Bind();
        Assert.That(binding.TryBeginRun("shared-patch-correlation", out var run, out var startReason),
            Is.True, startReason);
        using (var activeRun = run!)
        {
            Assert.That(activeRun.TryArmMethod(method, "fixture", out var armReason), Is.True, armReason);
            SeedProfiler(method, empty: false);
            SetDocumentRows(string.Empty, PatchRow(firstPatchKey, ambiguousTargets: true));

            Assert.That(activeRun.TryStopAndCapture(
                Persisted(activeRun),
                out var capture,
                out var reason), Is.True, reason);
            Assert.Multiple(() =>
            {
                Assert.That(capture!.Sidecars, Has.Count.EqualTo(1));
                Assert.That(capture.Sidecars.Single().RowKey, Is.EqualTo(firstPatchKey));
                Assert.That(capture.Sidecars.Single().AmbiguousTargetCount, Is.EqualTo(2));
            });
        }
    }

    [Test]
    public void Capture_retains_every_profiler_owned_by_a_curated_target()
    {
        var binding = Bind();
        Assert.That(binding.TryBeginRun("target-sidecars", out var run, out var startReason), Is.True, startReason);
        using (var activeRun = run!)
        {
            Assert.That(activeRun.TryArmTarget("fixture.valid", out var armReason), Is.True, armReason);
            Assert.That(activeRun.TryStopAndCapture(Persisted(activeRun), out var capture, out var reason), Is.True, reason);
            Assert.Multiple(() =>
            {
                Assert.That(capture!.Sidecars, Has.Count.EqualTo(2));
                Assert.That(capture.Sidecars, Has.All.Property(nameof(CircinusProfilerSidecar.Empty)).True);
                Assert.That(capture.Sidecars, Has.All.Property(nameof(CircinusProfilerSidecar.NoRowReason))
                    .EqualTo("empty-or-uninvoked"));
            });
        }
    }

    [Test]
    public void Successful_backend_completion_releases_the_exact_owned_profiler_session()
    {
        var method = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(ProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var binding = Bind();
        Assert.That(binding.TryBeginRun("backend-success", out var run, out var startReason),
            Is.True, startReason);
        Assert.That(run!.TryArmMethod(method, "fixture", out var armReason), Is.True, armReason);
        Assert.That(InvokeStatic<bool>(
            "Circinus.Profiling.Instrumenter",
            "IsPatched",
            method), Is.True);

        var result = GatewayCircinusPerformanceBackend.CompleteOwnedRun(ref run, _ => "complete");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("complete"));
            Assert.That(run, Is.Null);
            Assert.That(InvokeStatic<bool>(
                "Circinus.Profiling.Instrumenter",
                "IsPatched",
                method), Is.False);
            Assert.That(GetProperty<bool>(CurrentRecorder(), "Recording"), Is.False);
        });
    }

    [Test]
    public void Dispose_retries_failed_recorder_method_and_target_cleanup_until_all_are_inactive()
    {
        var method = typeof(CircinusRuntimeAdapterTests).GetMethod(
            nameof(ProfiledFixtureMethod),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var binding = Bind();
        Assert.That(binding.TryBeginRun("retry-cleanup", out var run, out var startReason),
            Is.True, startReason);
        Assert.That(run!.TryArmMethod(method, "fixture", out var methodReason), Is.True, methodReason);
        Assert.That(run.TryArmTarget("fixture.valid", out var targetReason), Is.True, targetReason);
        SetRecorderField("ThrowOnStopForTests", true);
        SetStaticField("Circinus.Profiling.Instrumenter", "ThrowOnDisarmMethodForTests", true);
        SetStaticField("Circinus.Profiling.Instrumenter", "ThrowOnDisarmTargetForTests", true);

        var failure = Assert.Throws<AggregateException>(() => run.Dispose());
        Assert.Multiple(() =>
        {
            Assert.That(failure!.InnerExceptions, Has.Count.GreaterThanOrEqualTo(3));
            Assert.That(GetProperty<bool>(CurrentRecorder(), "Recording"), Is.True);
            Assert.That(InvokeStatic<bool>("Circinus.Profiling.Instrumenter", "IsPatched", method), Is.True);
            Assert.That(GetStaticProperty<int>("Circinus.Profiling.Instrumenter", "ActiveTargetCountForTests"),
                Is.EqualTo(1));
        });

        SetRecorderField("ThrowOnStopForTests", false);
        SetStaticField("Circinus.Profiling.Instrumenter", "ThrowOnDisarmMethodForTests", false);
        SetStaticField("Circinus.Profiling.Instrumenter", "ThrowOnDisarmTargetForTests", false);
        Assert.DoesNotThrow(() => run.Dispose());
        Assert.Multiple(() =>
        {
            Assert.That(GetProperty<bool>(CurrentRecorder(), "Recording"), Is.False);
            Assert.That(InvokeStatic<bool>("Circinus.Profiling.Instrumenter", "IsPatched", method), Is.False);
            Assert.That(GetStaticProperty<int>("Circinus.Profiling.Instrumenter", "ActiveTargetCountForTests"),
                Is.Zero);
            Assert.That(GetStaticField<bool>("Circinus.Profiling.ProfilerRegistry", "Enabled"), Is.False);
            Assert.That(GetStaticField<bool>("Circinus.Profiling.ProfilerRegistry", "Recording"), Is.False);
        });
    }

    private static CircinusRuntimeBinding Bind()
    {
        Assert.That(CircinusRuntimeAdapter.TryBind(
            packageActive: true,
            new[] { Fixture.Value },
            out var binding,
            out var reason), Is.True, reason);
        return binding!;
    }

    private static void ProfiledFixtureMethod() { }

    private static void SecondProfiledFixtureMethod() { }

    private static Func<string, string?> Persisted(CircinusRuntimeRun run)
    {
        var expected = run.RunId;
        return id =>
        {
            Assert.That(id, Is.EqualTo(expected));
            return "{\"id\":\"" + id + "\",\"schemaMajor\":1,\"schemaMinor\":15}";
        };
    }

    private static string MethodKey(MethodBase method)
    {
        var methodRef = InvokeStatic<object>("Circinus.Identity.HarmonyIndex", "RefOf", method);
        return (string)methodRef.GetType().GetField("Key")!.GetValue(methodRef)!;
    }

    private static void SeedProfiler(MethodBase method, bool empty)
    {
        var profiler = InvokeStatic<object>("Circinus.Profiling.ProfilerRegistry", "Find", method);
        InvokeInstance(profiler, "SeedForTests", 2, 12L, 7L, empty, 4);
    }

    private static void SetDocumentRows(string methodsJson, string patchesJson)
    {
        var document = GetProperty<object>(CurrentRecorder(), "Document");
        document.GetType().GetField("MethodsJson")!.SetValue(document, methodsJson);
        document.GetType().GetField("PatchesJson")!.SetValue(document, patchesJson);
    }

    private static void SetDocumentField(string name, object value)
    {
        var document = GetProperty<object>(CurrentRecorder(), "Document");
        document.GetType().GetField(name)!.SetValue(document, value);
    }

    private static string MethodRow(string key) =>
        "{\"method\":{\"key\":\"" + key + "\"},\"totalMs\":1,\"calls\":12}";

    private static string PatchRow(string key, bool ambiguousTargets = false) =>
        "{\"patch\":{\"key\":\"" + key + "\"},\"totalMs\":1,\"calls\":12,\"timedCalls\":7" +
        (ambiguousTargets ? ",\"ambiguousTargets\":true" : string.Empty) + "}";

    private static object CurrentRecorder() =>
        GetStaticProperty<object>("Circinus.Session.RunRecorder", "Current");

    private static void SetSettingsField(string name, object value)
    {
        var settings = GetStaticField<object>("Circinus.Bootstrap.CircinusMod", "Settings");
        settings.GetType().GetField(name)!.SetValue(settings, value);
    }

    private static void SetRecorderField(string name, object value) =>
        CurrentRecorder().GetType().GetField(name)!.SetValue(CurrentRecorder(), value);

    private static void InvokeStatic(string typeName, string methodName, params object[] arguments) =>
        InvokeStatic<object?>(typeName, methodName, arguments);

    private static T InvokeStatic<T>(string typeName, string methodName, params object[] arguments) =>
        (T)Fixture.Value.GetType(typeName)!.GetMethod(methodName)!.Invoke(null, arguments)!;

    private static void InvokeInstance(object instance, string methodName, params object[] arguments) =>
        instance.GetType().GetMethod(methodName)!.Invoke(instance, arguments);

    private static T GetStaticProperty<T>(string typeName, string propertyName) =>
        (T)Fixture.Value.GetType(typeName)!.GetProperty(propertyName)!.GetValue(null)!;

    private static T GetStaticField<T>(string typeName, string fieldName) =>
        (T)Fixture.Value.GetType(typeName)!.GetField(fieldName)!.GetValue(null)!;

    private static void SetStaticField(string typeName, string fieldName, object value) =>
        Fixture.Value.GetType(typeName)!.GetField(fieldName)!.SetValue(null, value);

    private static T GetProperty<T>(object instance, string propertyName) =>
        (T)instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;

    private static string FixtureAssemblyPath()
    {
        var root = FindRepositoryRoot();
        var configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif
        return Path.Combine(root, "tests", "Fixtures", "Circinus.ValidFixtures", "bin", configuration, "net48", "Circinus.dll");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private sealed class ReplacingTypeSource : ICircinusTypeSource
    {
        private readonly ICircinusTypeSource inner;
        private readonly string replacedName;
        private readonly Type replacement;

        public ReplacingTypeSource(ICircinusTypeSource inner, string replacedName, Type replacement)
        {
            this.inner = inner;
            this.replacedName = replacedName;
            this.replacement = replacement;
        }

        public Type? Resolve(string fullName) => fullName == replacedName ? replacement : inner.Resolve(fullName);
    }

    private sealed class EmptyHarmonyCatalog : IPerformanceHarmonyCatalog
    {
        public IReadOnlyList<PerformanceHarmonyPatch> ResolveOwner(string exactOwnerId) =>
            Array.Empty<PerformanceHarmonyPatch>();
    }

    private sealed class ChangedRunDocument
    {
        public string Id = string.Empty;
        public int SchemaMajor = 1;
        public int SchemaMinor = 15;
        public object ToJson() => new object();
    }
}
