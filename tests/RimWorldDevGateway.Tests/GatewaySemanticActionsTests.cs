using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewaySemanticActionsTests
{
    [Test]
    public void Discovery_reports_versioned_actions_schemas_and_live_availability_on_the_dispatcher()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations
        {
            WindowAvailability = GatewaySemanticActionAvailability.Unavailable(
                "No active window is available.")
        };
        var registry = new GatewaySemanticActionRegistry(dispatcher, operations);

        var pending = registry.DiscoverAsync("discover-actions");

        Assert.That(pending.IsCompleted, Is.False);
        dispatcher.Drain(DispatchPhase.Update);
        var descriptors = pending.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(
                descriptors.Select(action => action.Name),
                Is.EqualTo(new[]
                {
                    "game.pause",
                    "game.speed",
                    "window.accept",
                    "window.cancel",
                    "debug.tool.cancel"
                }));
            Assert.That(descriptors.Select(action => action.Version), Is.All.EqualTo("1"));
            Assert.That(descriptors[0].ArgumentSchema.Single().Name, Is.EqualTo("paused"));
            Assert.That(descriptors[0].ArgumentSchema.Single().Type, Is.EqualTo("boolean"));
            Assert.That(descriptors[0].ArgumentSchema.Single().Required, Is.False);
            Assert.That(
                descriptors[1].ArgumentSchema.Single().AllowedValues,
                Is.EqualTo(new[] { "paused", "normal", "fast", "superfast", "ultrafast" }));
            Assert.That(descriptors[0].Available, Is.True);
            Assert.That(descriptors[0].UnavailableReason, Is.Null);
            Assert.That(descriptors[2].Available, Is.False);
            Assert.That(descriptors[2].UnavailableReason, Is.EqualTo("No active window is available."));
            Assert.That(operations.AvailabilityRequestIds, Is.All.EqualTo("discover-actions"));
        });
    }

    [Test]
    public void Pause_executes_on_the_dispatcher_and_returns_the_before_and_after_state()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations();
        var registry = new GatewaySemanticActionRegistry(dispatcher, operations);

        var pending = registry.InvokeAsync(
            "pause-request",
            "game.pause",
            new Dictionary<string, object?> { ["paused"] = true });

        Assert.Multiple(() =>
        {
            Assert.That(pending.IsCompleted, Is.False);
            Assert.That(operations.IsPaused, Is.False);
        });

        dispatcher.Drain(DispatchPhase.Update);
        var invocation = pending.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(invocation.Action, Is.EqualTo("game.pause"));
            Assert.That(invocation.Version, Is.EqualTo("1"));
            Assert.That(invocation.Ok, Is.True);
            Assert.That(invocation.Error, Is.Null);
            Assert.That(invocation.Result?.ResolvedAction, Is.EqualTo("game.pause.set"));
            Assert.That(invocation.Result?.Before, Is.EqualTo(false));
            Assert.That(invocation.Result?.After, Is.EqualTo(true));
            Assert.That(operations.IsPaused, Is.True);
            Assert.That(operations.MutationRequestIds, Is.EqualTo(new[] { "pause-request" }));
        });
    }

    [Test]
    public void Pause_action_refuses_to_claim_an_unpause_while_RimWorld_is_force_paused()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations
        {
            IsPaused = true,
            ForcePaused = true
        };
        var registry = new GatewaySemanticActionRegistry(dispatcher, operations);

        var pending = registry.InvokeAsync(
            "forced-pause-request",
            "game.pause",
            new Dictionary<string, object?> { ["paused"] = false });
        dispatcher.Drain(DispatchPhase.Update);
        var invocation = pending.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(invocation.Ok, Is.False);
            Assert.That(invocation.Error?.Code, Is.EqualTo("game_force_paused"));
            Assert.That(operations.IsPaused, Is.True);
            Assert.That(operations.MutationRequestIds, Is.Empty);
        });
    }

    [Test]
    public void Speed_rejects_missing_wrong_type_unknown_and_extra_arguments_before_dispatch()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations();
        var registry = new GatewaySemanticActionRegistry(dispatcher, operations);

        var invocations = new[]
        {
            registry.InvokeAsync("speed-missing", "game.speed").GetAwaiter().GetResult(),
            registry.InvokeAsync(
                "speed-type",
                "game.speed",
                new Dictionary<string, object?> { ["speed"] = 2 }).GetAwaiter().GetResult(),
            registry.InvokeAsync(
                "speed-value",
                "game.speed",
                new Dictionary<string, object?> { ["speed"] = "warp" }).GetAwaiter().GetResult(),
            registry.InvokeAsync(
                "speed-extra",
                "game.speed",
                new Dictionary<string, object?>
                {
                    ["speed"] = "fast",
                    ["unexpected"] = true
                }).GetAwaiter().GetResult()
        };

        Assert.Multiple(() =>
        {
            Assert.That(invocations.Select(result => result.Ok), Is.All.False);
            Assert.That(
                invocations.Select(result => result.Error?.Code),
                Is.All.EqualTo("invalid_argument"));
            Assert.That(invocations.Select(result => result.Error?.Retryable), Is.All.False);
            Assert.That(invocations.Select(result => result.Result), Is.All.Null);
            Assert.That(invocations.Select(result => result.Action), Is.All.EqualTo("game.speed"));
            Assert.That(dispatcher.PendingCount, Is.Zero);
            Assert.That(operations.MutationRequestIds, Is.Empty);
        });
    }

    [Test]
    public void Speed_executes_on_the_dispatcher_and_returns_stable_speed_names()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations();
        var registry = new GatewaySemanticActionRegistry(dispatcher, operations);

        var pending = registry.InvokeAsync(
            "speed-request",
            "game.speed",
            new Dictionary<string, object?> { ["speed"] = "superfast" });

        Assert.That(pending.IsCompleted, Is.False);
        dispatcher.Drain(DispatchPhase.Update);
        var invocation = pending.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(invocation.Ok, Is.True);
            Assert.That(invocation.Result?.ResolvedAction, Is.EqualTo("game.speed.set"));
            Assert.That(invocation.Result?.Before, Is.EqualTo("normal"));
            Assert.That(invocation.Result?.After, Is.EqualTo("superfast"));
            Assert.That(operations.CurrentSpeed, Is.EqualTo(GatewayGameSpeed.Superfast));
            Assert.That(operations.MutationRequestIds, Is.EqualTo(new[] { "speed-request" }));
        });
    }

    [TestCase("window.accept", "accept")]
    [TestCase("window.cancel", "cancel")]
    public void Window_actions_invoke_the_top_window_handler_on_the_dispatcher(
        string action,
        string expectedOperation)
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations();
        var registry = new GatewaySemanticActionRegistry(dispatcher, operations);

        var pending = registry.InvokeAsync("window-request", action);

        Assert.That(pending.IsCompleted, Is.False);
        dispatcher.Drain(DispatchPhase.Update);
        var invocation = pending.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(invocation.Ok, Is.True);
            Assert.That(invocation.Result?.ResolvedAction, Is.EqualTo(action));
            Assert.That(invocation.Result?.Before, Is.EqualTo("Dialog_MessageBox"));
            Assert.That(invocation.Result?.After, Is.Null);
            Assert.That(operations.LastWindowOperation, Is.EqualTo(expectedOperation));
            Assert.That(operations.MutationRequestIds, Is.EqualTo(new[] { "window-request" }));
        });
    }

    [Test]
    public void Debug_tool_cancel_is_discoverable_and_clears_only_an_active_native_tool()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations { DebugToolActive = true };
        var registry = new GatewaySemanticActionRegistry(dispatcher, operations);

        var discovery = registry.DiscoverAsync("discover-debug-tool-cancel");
        dispatcher.Drain(DispatchPhase.Update);
        var descriptor = discovery.GetAwaiter().GetResult()
            .Single(action => action.Name == "debug.tool.cancel");
        var invocationTask = registry.InvokeAsync(
            "cancel-debug-tool",
            "debug.tool.cancel");
        dispatcher.Drain(DispatchPhase.Update);
        var invocation = invocationTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Available, Is.True);
            Assert.That(invocation.Ok, Is.True);
            Assert.That(invocation.Result?.Before, Is.EqualTo(true));
            Assert.That(invocation.Result?.After, Is.EqualTo(false));
            Assert.That(operations.DebugToolActive, Is.False);
        });
    }

    [Test]
    public void Unknown_action_returns_a_stable_error_without_dispatching()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations();
        var registry = new GatewaySemanticActionRegistry(dispatcher, operations);

        var invocation = registry.InvokeAsync("unknown-request", "map.teleport")
            .GetAwaiter()
            .GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(invocation.Action, Is.EqualTo("map.teleport"));
            Assert.That(invocation.Version, Is.EqualTo("1"));
            Assert.That(invocation.Ok, Is.False);
            Assert.That(invocation.Result, Is.Null);
            Assert.That(invocation.Error?.Code, Is.EqualTo("action_not_found"));
            Assert.That(invocation.Error?.Retryable, Is.False);
            Assert.That(dispatcher.PendingCount, Is.Zero);
            Assert.That(operations.AvailabilityRequestIds, Is.Empty);
            Assert.That(operations.MutationRequestIds, Is.Empty);
        });
    }

    [Test]
    public void Unavailable_action_returns_its_live_reason_without_mutation()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations
        {
            GameAvailability = GatewaySemanticActionAvailability.Unavailable(
                "A playable game is required.")
        };
        var registry = new GatewaySemanticActionRegistry(dispatcher, operations);

        var pending = registry.InvokeAsync("unavailable-request", "game.pause");
        dispatcher.Drain(DispatchPhase.Update);
        var invocation = pending.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(invocation.Ok, Is.False);
            Assert.That(invocation.Result, Is.Null);
            Assert.That(invocation.Error?.Code, Is.EqualTo("action_unavailable"));
            Assert.That(invocation.Error?.Message, Is.EqualTo("A playable game is required."));
            Assert.That(invocation.Error?.Retryable, Is.False);
            Assert.That(operations.MutationRequestIds, Is.Empty);
        });
    }

    [Test]
    public void Live_operation_failure_returns_a_stable_action_error()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations
        {
            MutationException = new InvalidOperationException("RimWorld rejected the operation.")
        };
        var logs = new GatewayLogBuffer();
        var registry = new GatewaySemanticActionRegistry(
            dispatcher,
            operations,
            diagnostics: logs);

        var pending = registry.InvokeAsync(
            "failed-request",
            "game.pause",
            new Dictionary<string, object?> { ["paused"] = true });
        dispatcher.Drain(DispatchPhase.Update);
        var invocation = pending.GetAwaiter().GetResult();
        var diagnostic = logs.Read(0, 10).Entries.Single();

        Assert.Multiple(() =>
        {
            Assert.That(invocation.Ok, Is.False);
            Assert.That(invocation.Result, Is.Null);
            Assert.That(invocation.Error?.Code, Is.EqualTo("action_failed"));
            Assert.That(invocation.Error?.Message, Does.Contain("RimWorld rejected the operation."));
            Assert.That(invocation.Error?.Retryable, Is.False);
            Assert.That(diagnostic.Severity, Is.EqualTo("Error"));
            Assert.That(diagnostic.RequestId, Is.EqualTo("failed-request"));
            Assert.That(diagnostic.Message, Does.Contain("game.pause"));
            Assert.That(diagnostic.Stack, Does.Contain("InvalidOperationException"));
        });
    }

    [Test]
    public void Verse_adapter_reports_game_unavailable_without_dereferencing_game_services()
    {
        Assert.That(Verse.Current.Game, Is.Null, "The framework fixture must represent the entry state.");
        var operations = new VerseGatewaySemanticActionOperations();

        var availability = operations.GetGameAvailability();

        Assert.Multiple(() =>
        {
            Assert.That(availability.IsAvailable, Is.False);
            Assert.That(availability.UnavailableReason, Is.EqualTo("No active RimWorld game is available."));
        });
    }

    [Test]
    public void Pause_and_window_actions_reject_arguments_outside_their_schemas()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeSemanticActionOperations();
        var registry = new GatewaySemanticActionRegistry(dispatcher, operations);

        var invocations = new[]
        {
            registry.InvokeAsync(
                "pause-type",
                "game.pause",
                new Dictionary<string, object?> { ["paused"] = "true" }).GetAwaiter().GetResult(),
            registry.InvokeAsync(
                "accept-extra",
                "window.accept",
                new Dictionary<string, object?> { ["confirm"] = true }).GetAwaiter().GetResult(),
            registry.InvokeAsync(
                "cancel-extra",
                "window.cancel",
                new Dictionary<string, object?> { ["cancel"] = true }).GetAwaiter().GetResult()
        };

        Assert.Multiple(() =>
        {
            Assert.That(invocations.Select(result => result.Ok), Is.All.False);
            Assert.That(
                invocations.Select(result => result.Error?.Code),
                Is.All.EqualTo("invalid_argument"));
            Assert.That(dispatcher.PendingCount, Is.Zero);
            Assert.That(operations.MutationRequestIds, Is.Empty);
        });
    }

    private sealed class FakeSemanticActionOperations : IGatewaySemanticActionOperations
    {
        public GatewaySemanticActionAvailability GameAvailability { get; set; } =
            GatewaySemanticActionAvailability.Available();

        public GatewaySemanticActionAvailability WindowAvailability { get; set; } =
            GatewaySemanticActionAvailability.Available();

        public List<string?> AvailabilityRequestIds { get; } = new();

        public List<string?> MutationRequestIds { get; } = new();

        public string? LastWindowOperation { get; private set; }

        public Exception? MutationException { get; set; }

        public bool IsPaused { get; set; }

        public bool ForcePaused { get; set; }

        public GatewayGameSpeed CurrentSpeed { get; private set; } = GatewayGameSpeed.Normal;

        public string? ActiveWindowType { get; private set; } = "Dialog_MessageBox";

        public bool DebugToolActive { get; set; }

        public GatewaySemanticActionAvailability GetDebugToolAvailability()
        {
            AvailabilityRequestIds.Add(GatewayRequestScope.CurrentRequestId);
            return DebugToolActive
                ? GatewaySemanticActionAvailability.Available()
                : GatewaySemanticActionAvailability.Unavailable("No native debug tool is active.");
        }

        public GatewaySemanticActionAvailability GetGameAvailability()
        {
            AvailabilityRequestIds.Add(GatewayRequestScope.CurrentRequestId);
            return GameAvailability;
        }

        public GatewaySemanticActionAvailability GetWindowAvailability()
        {
            AvailabilityRequestIds.Add(GatewayRequestScope.CurrentRequestId);
            return WindowAvailability;
        }

        public void SetPaused(bool paused)
        {
            MutationRequestIds.Add(GatewayRequestScope.CurrentRequestId);
            ThrowIfConfigured();
            IsPaused = paused;
        }

        public void SetSpeed(GatewayGameSpeed speed)
        {
            MutationRequestIds.Add(GatewayRequestScope.CurrentRequestId);
            ThrowIfConfigured();
            CurrentSpeed = speed;
        }

        public void AcceptWindow()
        {
            MutationRequestIds.Add(GatewayRequestScope.CurrentRequestId);
            ThrowIfConfigured();
            LastWindowOperation = "accept";
            ActiveWindowType = null;
        }

        public void CancelWindow()
        {
            MutationRequestIds.Add(GatewayRequestScope.CurrentRequestId);
            ThrowIfConfigured();
            LastWindowOperation = "cancel";
            ActiveWindowType = null;
        }

        public void CancelDebugTool()
        {
            MutationRequestIds.Add(GatewayRequestScope.CurrentRequestId);
            ThrowIfConfigured();
            DebugToolActive = false;
        }

        private void ThrowIfConfigured()
        {
            if (MutationException is not null)
            {
                throw MutationException;
            }
        }
    }
}
