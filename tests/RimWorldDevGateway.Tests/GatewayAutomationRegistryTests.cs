using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayAutomationRegistryTests
{
    [Test]
    public void Built_in_automation_is_discoverable_and_runs_synchronously_on_the_caller_thread()
    {
        var registry = Registry();
        var callerThread = Environment.CurrentManagedThreadId;
        var invokedThread = 0;
        registry.RegisterBuiltIn(
            Descriptor("verify.scene", mutating: false),
            (context, arguments) =>
            {
                invokedThread = Environment.CurrentManagedThreadId;
                return "verified:" + arguments["name"];
            });

        var descriptors = registry.Describe();
        var run = registry.StartRun(
            "verify.scene",
            "request-1",
            new Dictionary<string, object?> { ["name"] = "kitchen" });

        Assert.Multiple(() =>
        {
            Assert.That(descriptors, Has.Count.EqualTo(1));
            Assert.That(descriptors[0].Name, Is.EqualTo("verify.scene"));
            Assert.That(descriptors[0].Version, Is.EqualTo("1.0"));
            Assert.That(descriptors[0].Availability.Available, Is.True);
            Assert.That(invokedThread, Is.EqualTo(callerThread));
            Assert.That(run.State, Is.EqualTo("succeeded"));
            Assert.That(run.Result, Is.EqualTo("verified:kitchen"));
        });
    }

    [Test]
    public void Session_registration_exposes_dynamic_availability_and_invocation_is_case_sensitive()
    {
        var registry = Registry();
        var available = false;
        registry.RegisterSession(
            Descriptor("scene.uploaded", mutating: true),
            (_, _) => "ok",
            () => new GatewayAutomationAvailability(available, "A playable map is required."));

        var descriptor = registry.Describe().Single();
        var unavailable = Assert.Throws<GatewayAutomationException>(() => registry.StartRun(
            "scene.uploaded",
            "request-unavailable",
            EmptyArguments()));
        var wrongCase = Assert.Throws<GatewayAutomationException>(() => registry.StartRun(
            "Scene.Uploaded",
            "request-case",
            EmptyArguments()));

        available = true;
        var run = registry.StartRun("scene.uploaded", "request-ready", EmptyArguments());

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.SessionScoped, Is.True);
            Assert.That(descriptor.Mutating, Is.True);
            Assert.That(descriptor.Prerequisites, Is.EqualTo(new[] { "gateway-ready" }));
            Assert.That(descriptor.Availability.Available, Is.False);
            Assert.That(descriptor.Availability.Reason, Is.EqualTo("A playable map is required."));
            Assert.That(unavailable!.Code, Is.EqualTo("automation_unavailable"));
            Assert.That(wrongCase!.Code, Is.EqualTo("automation_not_found"));
            Assert.That(run.State, Is.EqualTo("succeeded"));
        });
    }

    [Test]
    public void Descriptor_availability_is_preserved_when_no_dynamic_probe_is_registered()
    {
        var registry = Registry();
        var descriptor = new GatewayAutomationDescriptor(
            "scene.requires-map",
            "1.0",
            "Test automation",
            new Dictionary<string, object?> { ["type"] = "object" },
            new[] { "playable-map" },
            mutating: false,
            availability: new GatewayAutomationAvailability(false, "No playable map."));
        registry.RegisterBuiltIn(descriptor, (_, _) => null);

        var discovered = registry.Describe().Single();
        var error = Assert.Throws<GatewayAutomationException>(() => registry.StartRun(
            "scene.requires-map",
            "request-no-map",
            EmptyArguments()));

        Assert.Multiple(() =>
        {
            Assert.That(discovered.Availability.Available, Is.False);
            Assert.That(discovered.Availability.Reason, Is.EqualTo("No playable map."));
            Assert.That(error!.Code, Is.EqualTo("automation_unavailable"));
        });
    }

    [Test]
    public void Cancellation_between_steps_retains_bounded_progress_and_artifacts()
    {
        var registry = Registry(maximumProgress: 2, maximumArtifacts: 1);
        var secondMutationRan = false;
        var stateAfterCancellationRequest = string.Empty;
        registry.RegisterBuiltIn(
            Descriptor("scene.cancel", mutating: true),
            (context, _) =>
            {
                context.ReportProgress("preflight", "resolved", 0.1);
                context.ReportProgress("spawn", "first", 0.5);
                context.ReportProgress("spawn", "second", 0.75);
                context.AddArtifact("before", "text", "old");
                context.AddArtifact("after", "screenshot", "new");

                context.RunStep("first-mutation", () =>
                {
                    Assert.That(registry.Cancel(context.RunId), Is.True);
                    stateAfterCancellationRequest = registry.GetRun(context.RunId).State;
                });
                context.RunStep("second-mutation", () => secondMutationRan = true);
                return null;
            });

        var run = registry.StartRun("scene.cancel", "request-cancel", EmptyArguments());

        Assert.Multiple(() =>
        {
            Assert.That(stateAfterCancellationRequest, Is.EqualTo("cancel-requested"));
            Assert.That(secondMutationRan, Is.False);
            Assert.That(run.State, Is.EqualTo("cancelled"));
            Assert.That(run.Error?.Code, Is.EqualTo("automation_cancelled"));
            Assert.That(run.Progress.Select(item => item.Sequence), Is.EqualTo(new long[] { 2, 3 }));
            Assert.That(run.ProgressEvicted, Is.EqualTo(1));
            Assert.That(run.Artifacts.Select(item => item.Name), Is.EqualTo(new[] { "after" }));
            Assert.That(run.ArtifactsEvicted, Is.EqualTo(1));
        });
    }

    [Test]
    public void Equivalent_idempotent_retry_replays_original_run_even_if_availability_changes()
    {
        var runIds = new Queue<string>(new[] { "run-1", "run-2" });
        var registry = Registry(runIdFactory: () => runIds.Dequeue());
        var available = true;
        var invocationCount = 0;
        registry.RegisterBuiltIn(
            Descriptor("scene.idempotent", mutating: true),
            (_, _) => ++invocationCount,
            () => new GatewayAutomationAvailability(available, "Map closed."));

        var first = registry.StartRun(
            "scene.idempotent",
            "request-first",
            new Dictionary<string, object?> { ["z"] = 2, ["a"] = 1 },
            "retry-key");
        available = false;
        var replay = registry.StartRun(
            "scene.idempotent",
            "request-retry",
            new Dictionary<string, object?> { ["a"] = 1, ["z"] = 2 },
            "retry-key");

        Assert.Multiple(() =>
        {
            Assert.That(replay, Is.SameAs(first));
            Assert.That(replay.RunId, Is.EqualTo("run-1"));
            Assert.That(replay.RequestId, Is.EqualTo("request-first"));
            Assert.That(invocationCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Equivalent_read_only_dictionary_arguments_have_a_canonical_idempotency_fingerprint()
    {
        var registry = Registry();
        var invocationCount = 0;
        registry.RegisterBuiltIn(
            Descriptor("scene.read-only-arguments", mutating: true),
            (_, arguments) =>
            {
                invocationCount++;
                return arguments["a"];
            });

        var first = registry.StartRun(
            "scene.read-only-arguments",
            "request-first",
            new ReadOnlyArguments(("z", 2), ("a", 1)),
            "read-only-key");
        var replay = registry.StartRun(
            "scene.read-only-arguments",
            "request-retry",
            new ReadOnlyArguments(("a", 1), ("z", 2)),
            "read-only-key");

        Assert.Multiple(() =>
        {
            Assert.That(replay, Is.SameAs(first));
            Assert.That(first.Result, Is.EqualTo(1));
            Assert.That(invocationCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Reusing_an_idempotency_key_with_different_arguments_fails_before_new_work()
    {
        var registry = Registry();
        var invocationCount = 0;
        registry.RegisterBuiltIn(
            Descriptor("scene.conflict", mutating: true),
            (_, _) => ++invocationCount);
        registry.StartRun(
            "scene.conflict",
            "request-first",
            new Dictionary<string, object?> { ["count"] = 1 },
            "same-key");

        var error = Assert.Throws<GatewayAutomationException>(() => registry.StartRun(
            "scene.conflict",
            "request-second",
            new Dictionary<string, object?> { ["count"] = 2 },
            "same-key"));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo("idempotency_conflict"));
            Assert.That(invocationCount, Is.EqualTo(1));
            Assert.That(registry.ListRuns(), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Completed_run_history_is_bounded_and_evicted_runs_are_not_addressable()
    {
        var runIds = new Queue<string>(new[] { "run-1", "run-2", "run-3" });
        var registry = Registry(historyCapacity: 2, runIdFactory: () => runIds.Dequeue());
        registry.RegisterBuiltIn(Descriptor("scene.history", mutating: false), (_, _) => null);

        registry.StartRun("scene.history", "request-1", EmptyArguments());
        var second = registry.StartRun("scene.history", "request-2", EmptyArguments());
        var third = registry.StartRun("scene.history", "request-3", EmptyArguments());
        var missing = Assert.Throws<GatewayAutomationException>(() => registry.GetRun("run-1"));

        Assert.Multiple(() =>
        {
            Assert.That(registry.ListRuns(), Is.EqualTo(new[] { second, third }));
            Assert.That(missing!.Code, Is.EqualTo("automation_run_not_found"));
        });
    }

    [Test]
    public void Handler_failures_finish_the_run_with_stable_error_codes()
    {
        var logs = new GatewayLogBuffer();
        var registry = Registry(diagnostics: logs);
        registry.RegisterBuiltIn(
            Descriptor("scene.known-failure", mutating: false),
            (_, _) => throw new GatewayAutomationException("def_not_found", "Missing ThingDef."));
        registry.RegisterBuiltIn(
            Descriptor("scene.unknown-failure", mutating: false),
            (_, _) => throw new InvalidOperationException("Broken test automation."));

        var known = registry.StartRun("scene.known-failure", "request-known", EmptyArguments());
        var unknown = registry.StartRun("scene.unknown-failure", "request-unknown", EmptyArguments());
        var diagnostic = logs.Read(0, 10).Entries.Single();

        Assert.Multiple(() =>
        {
            Assert.That(known.State, Is.EqualTo("failed"));
            Assert.That(known.Error?.Code, Is.EqualTo("def_not_found"));
            Assert.That(unknown.State, Is.EqualTo("failed"));
            Assert.That(unknown.Error?.Code, Is.EqualTo("automation_failed"));
            Assert.That(diagnostic.Severity, Is.EqualTo("Error"));
            Assert.That(diagnostic.RequestId, Is.EqualTo("request-unknown"));
            Assert.That(diagnostic.Message, Does.Contain("scene.unknown-failure"));
            Assert.That(diagnostic.Message, Does.Contain(unknown.RunId));
            Assert.That(diagnostic.Stack, Does.Contain("InvalidOperationException"));
        });
    }

    [Test]
    public void Timeout_is_cooperatively_observed_between_steps()
    {
        var now = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var registry = Registry(utcNow: () => now);
        var secondMutationRan = false;
        registry.RegisterBuiltIn(
            Descriptor("scene.timeout", mutating: true),
            (context, _) =>
            {
                context.RunStep("first-mutation", () => now = now.AddSeconds(6));
                context.RunStep("second-mutation", () => secondMutationRan = true);
                return null;
            });

        var run = registry.StartRun(
            "scene.timeout",
            "request-timeout",
            EmptyArguments(),
            timeout: TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(secondMutationRan, Is.False);
            Assert.That(run.State, Is.EqualTo("timed-out"));
            Assert.That(run.Error?.Code, Is.EqualTo("automation_timed_out"));
            Assert.That(run.CompletedUtc, Is.EqualTo(now));
        });
    }

    private static GatewayAutomationRegistry Registry(
        int historyCapacity = 4,
        int maximumProgress = 256,
        int maximumArtifacts = 64,
        Func<DateTimeOffset>? utcNow = null,
        Func<string>? runIdFactory = null,
        GatewayLogBuffer? diagnostics = null)
    {
        return new GatewayAutomationRegistry(
            historyCapacity,
            maximumProgress,
            maximumArtifacts,
            utcNow ?? (() => new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero)),
            runIdFactory ?? (() => Guid.NewGuid().ToString("N")),
            diagnostics);
    }

    private static IReadOnlyDictionary<string, object?> EmptyArguments() =>
        new Dictionary<string, object?>();

    private static GatewayAutomationDescriptor Descriptor(string name, bool mutating)
    {
        return new GatewayAutomationDescriptor(
            name,
            "1.0",
            "Test automation",
            new Dictionary<string, object?> { ["type"] = "object" },
            new[] { "gateway-ready" },
            mutating);
    }

    private sealed class ReadOnlyArguments : IReadOnlyDictionary<string, object?>
    {
        private readonly IReadOnlyList<KeyValuePair<string, object?>> entries;

        public ReadOnlyArguments(params (string Key, object? Value)[] entries)
        {
            this.entries = entries
                .Select(entry => new KeyValuePair<string, object?>(entry.Key, entry.Value))
                .ToArray();
        }

        public int Count => entries.Count;

        public IEnumerable<string> Keys => entries.Select(entry => entry.Key);

        public IEnumerable<object?> Values => entries.Select(entry => entry.Value);

        public object? this[string key] => entries.Single(entry => entry.Key == key).Value;

        public bool ContainsKey(string key) => entries.Any(entry => entry.Key == key);

        public bool TryGetValue(string key, out object? value)
        {
            foreach (var entry in entries)
            {
                if (entry.Key == key)
                {
                    value = entry.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => entries.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
