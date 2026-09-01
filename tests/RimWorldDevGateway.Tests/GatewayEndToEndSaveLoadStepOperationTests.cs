using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndSaveLoadStepOperationTests
{
    [Test]
    public void Saves_once_then_completes_only_after_a_new_playable_game_is_loaded()
    {
        var runtime = new RecordingRuntime();
        var operation = new GatewayEndToEndSaveLoadStepOperation(runtime, "FtvPersistence", _ => { });

        Assert.That(operation.IsCompleted, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(runtime.SaveCalls, Is.EqualTo(1));
            Assert.That(runtime.LoadCalls, Is.EqualTo(1));
            Assert.That(runtime.LastSaveName, Is.EqualTo("FtvPersistence"));
        });

        runtime.IsPlayable = false;
        runtime.CurrentGame = new object();
        Assert.That(operation.IsCompleted, Is.False, "A replacement game is not enough before player control returns.");
        runtime.IsPlayable = true;
        Assert.That(operation.IsCompleted, Is.True);

        var outcome = operation.GetOutcome();
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(outcome.Artifacts["saveName"], Is.EqualTo("FtvPersistence"));
            Assert.That(outcome.Artifacts["pausedStateRestored"], Is.EqualTo("False"));
            Assert.That(runtime.SaveCalls, Is.EqualTo(1));
            Assert.That(runtime.LoadCalls, Is.EqualTo(1));
            Assert.That(runtime.PauseCalls, Is.Zero,
                "An originally running workflow must remain running after replacement.");
            Assert.That(runtime.PauseOnLoad, Is.False,
                "An originally running workflow must not alter RimWorld's pause-on-load preference.");
        });
    }

    [Test]
    public void Fails_closed_without_overwriting_an_existing_save()
    {
        var runtime = new RecordingRuntime { SaveFileExists = true };
        var operation = new GatewayEndToEndSaveLoadStepOperation(runtime, "Existing", _ => { });

        Assert.That(operation.IsCompleted, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(operation.GetOutcome().Passed, Is.False);
            Assert.That(operation.GetOutcome().FailureCode, Is.EqualTo("e2e_save_already_exists"));
            Assert.That(runtime.SaveCalls, Is.Zero);
            Assert.That(runtime.LoadCalls, Is.Zero);
        });
    }

    [Test]
    public void Fails_closed_when_native_save_does_not_create_a_nonempty_file()
    {
        var runtime = new RecordingRuntime { CreateSaveOnSave = false };
        var operation = new GatewayEndToEndSaveLoadStepOperation(runtime, "Missing", _ => { });

        Assert.That(operation.IsCompleted, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(operation.GetOutcome().Passed, Is.False);
            Assert.That(operation.GetOutcome().FailureCode, Is.EqualTo("e2e_save_failed"));
            Assert.That(runtime.LoadCalls, Is.Zero);
        });
    }

    [Test]
    public void Paused_action_uses_native_pause_on_load_and_restores_the_preference()
    {
        var runtime = new RecordingRuntime { IsPaused = true };
        var deferred = new List<Action>();
        var operation = new GatewayEndToEndSaveLoadStepOperation(
            runtime,
            "PausedPersistence",
            deferred.Add);

        Assert.That(operation.IsCompleted, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(runtime.PauseOnLoad, Is.True,
                "RimWorld's native load callback must own pause restoration before play resumes.");
            Assert.That(runtime.Operations, Is.EqualTo(new[] { "save", "enable-pause-on-load", "load" }),
                "The native pause-on-load preference must be enabled immediately before requesting load.");
            Assert.That(deferred, Has.Count.EqualTo(1),
                "The temporary preference override needs guaranteed E2E cleanup before asynchronous loading begins.");
        });
        runtime.CurrentGame = new object();
        runtime.IsPaused = true;
        runtime.IsPlayable = true;
        Assert.That(operation.IsCompleted, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(runtime.PauseCalls, Is.Zero,
                "The native pause-on-load callback must make a polling pause unnecessary.");
            Assert.That(operation.GetOutcome().Artifacts["pausedStateRestored"], Is.EqualTo("True"));
            Assert.That(runtime.PauseOnLoad, Is.False,
                "The action must restore the caller's original pause-on-load preference.");
            Assert.That(runtime.Operations[runtime.Operations.Count - 1],
                Is.EqualTo("disable-pause-on-load"));
        });
    }

    [Test]
    public void Paused_action_uses_a_safe_playable_fallback_when_native_pause_was_not_observed()
    {
        var runtime = new RecordingRuntime { IsPaused = true };
        var operation = new GatewayEndToEndSaveLoadStepOperation(runtime, "PausedFallback", _ => { });

        Assert.That(operation.IsCompleted, Is.False);
        runtime.CurrentGame = new object();
        runtime.IsPaused = false;
        runtime.IsPlayable = false;

        Assert.That(operation.IsCompleted, Is.False);
        Assert.That(runtime.PauseCalls, Is.Zero,
            "TickManager.Pause is unsafe before the replacement play root is initialized.");

        runtime.IsPlayable = true;
        Assert.That(operation.IsCompleted, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(runtime.PauseCalls, Is.EqualTo(1));
            Assert.That(runtime.IsPaused, Is.True);
            Assert.That(runtime.PauseOnLoad, Is.False);
        });
    }

    [Test]
    public void Deferred_cleanup_restores_pause_on_load_when_async_loading_is_abandoned()
    {
        var runtime = new RecordingRuntime { IsPaused = true };
        var deferred = new List<Action>();
        var operation = new GatewayEndToEndSaveLoadStepOperation(
            runtime,
            "AbandonedLoad",
            deferred.Add);

        Assert.That(operation.IsCompleted, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(runtime.PauseOnLoad, Is.True);
            Assert.That(deferred, Has.Count.EqualTo(1));
        });

        deferred[0]();
        Assert.That(runtime.PauseOnLoad, Is.False);
    }

    [Test]
    public void Originally_running_action_temporarily_disables_a_preexisting_pause_on_load_preference()
    {
        var runtime = new RecordingRuntime
        {
            IsPaused = false,
            PauseOnLoad = true,
        };
        runtime.Operations.Clear();
        var deferred = new List<Action>();
        var operation = new GatewayEndToEndSaveLoadStepOperation(
            runtime,
            "RunningWithPreference",
            deferred.Add);

        Assert.That(operation.IsCompleted, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(runtime.PauseOnLoad, Is.False);
            Assert.That(runtime.Operations, Is.EqualTo(new[] { "save", "disable-pause-on-load", "load" }));
            Assert.That(deferred, Has.Count.EqualTo(1));
        });

        runtime.CurrentGame = new object();
        runtime.IsPaused = false;
        runtime.IsPlayable = true;
        Assert.That(operation.IsCompleted, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(runtime.PauseOnLoad, Is.True);
            Assert.That(operation.GetOutcome().Artifacts["pausedStateRestored"], Is.EqualTo("False"));
        });
    }

    private sealed class RecordingRuntime : IGatewayEndToEndSaveLoadRuntime
    {
        private readonly object initialGame = new();

        public object? CurrentGame { get; set; }

        public bool IsPlayable { get; set; } = true;

        public bool SavingTemporarilyDisabled { get; set; }

        public bool IsPaused { get; set; }

        public bool PauseOnLoad
        {
            get => pauseOnLoad;
            set
            {
                if (pauseOnLoad == value)
                {
                    return;
                }

                pauseOnLoad = value;
                Operations.Add(value ? "enable-pause-on-load" : "disable-pause-on-load");
            }
        }

        public bool SaveFileExists { get; set; }

        public bool CreateSaveOnSave { get; set; } = true;

        public int SaveCalls { get; private set; }

        public int LoadCalls { get; private set; }

        public int PauseCalls { get; private set; }

        public List<string> Operations { get; } = new();

        public string? LastSaveName { get; private set; }

        private bool pauseOnLoad;

        public RecordingRuntime()
        {
            CurrentGame = initialGame;
        }

        public bool SaveExists(string saveName) => SaveFileExists;

        public bool SaveIsNonEmpty(string saveName) => SaveFileExists;

        public void Save(string saveName)
        {
            SaveCalls++;
            Operations.Add("save");
            LastSaveName = saveName;
            SaveFileExists = CreateSaveOnSave;
        }

        public void Load(string saveName)
        {
            LoadCalls++;
            Operations.Add("load");
        }

        public void Pause()
        {
            PauseCalls++;
            IsPaused = true;
        }
    }
}
