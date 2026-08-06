using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndSaveLoadStepOperationTests
{
    [Test]
    public void Saves_once_then_completes_only_after_a_new_playable_game_is_loaded()
    {
        var runtime = new RecordingRuntime();
        var operation = new GatewayEndToEndSaveLoadStepOperation(runtime, "FtvPersistence");

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
            Assert.That(runtime.SaveCalls, Is.EqualTo(1));
            Assert.That(runtime.LoadCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void Fails_closed_without_overwriting_an_existing_save()
    {
        var runtime = new RecordingRuntime { SaveFileExists = true };
        var operation = new GatewayEndToEndSaveLoadStepOperation(runtime, "Existing");

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
        var operation = new GatewayEndToEndSaveLoadStepOperation(runtime, "Missing");

        Assert.That(operation.IsCompleted, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(operation.GetOutcome().Passed, Is.False);
            Assert.That(operation.GetOutcome().FailureCode, Is.EqualTo("e2e_save_failed"));
            Assert.That(runtime.LoadCalls, Is.Zero);
        });
    }

    private sealed class RecordingRuntime : IGatewayEndToEndSaveLoadRuntime
    {
        private readonly object initialGame = new();

        public object? CurrentGame { get; set; }

        public bool IsPlayable { get; set; } = true;

        public bool SavingTemporarilyDisabled { get; set; }

        public bool SaveFileExists { get; set; }

        public bool CreateSaveOnSave { get; set; } = true;

        public int SaveCalls { get; private set; }

        public int LoadCalls { get; private set; }

        public string? LastSaveName { get; private set; }

        public RecordingRuntime()
        {
            CurrentGame = initialGame;
        }

        public bool SaveExists(string saveName) => SaveFileExists;

        public bool SaveIsNonEmpty(string saveName) => SaveFileExists;

        public void Save(string saveName)
        {
            SaveCalls++;
            LastSaveName = saveName;
            SaveFileExists = CreateSaveOnSave;
        }

        public void Load(string saveName)
        {
            LoadCalls++;
        }
    }
}
