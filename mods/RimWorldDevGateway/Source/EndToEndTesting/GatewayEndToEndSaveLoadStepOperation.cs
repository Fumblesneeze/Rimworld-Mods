using System.IO;
using Verse;

namespace RimWorldDevGateway;

internal interface IGatewayEndToEndSaveLoadRuntime
{
    object? CurrentGame { get; }

    bool IsPlayable { get; }

    bool SavingTemporarilyDisabled { get; }

    bool SaveExists(string saveName);

    bool SaveIsNonEmpty(string saveName);

    void Save(string saveName);

    void Load(string saveName);
}

internal sealed class GatewayEndToEndSaveLoadStepOperation : IGatewayEndToEndStepOperation
{
    private readonly IGatewayEndToEndSaveLoadRuntime runtime;
    private readonly string saveName;
    private object? originalGame;
    private GatewayEndToEndStepOutcome? outcome;
    private bool loadRequested;

    internal GatewayEndToEndSaveLoadStepOperation(
        IGatewayEndToEndSaveLoadRuntime runtime,
        string saveName)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.saveName = string.IsNullOrWhiteSpace(saveName)
            ? throw new ArgumentException("A save name is required.", nameof(saveName))
            : saveName;
    }

    public bool IsCompleted
    {
        get
        {
            Advance();
            return outcome is not null;
        }
    }

    public GatewayEndToEndStepOutcome GetOutcome()
    {
        return outcome ?? throw new InvalidOperationException(
            "The save/load operation has not completed.");
    }

    private void Advance()
    {
        if (outcome is not null)
        {
            return;
        }

        try
        {
            if (!loadRequested)
            {
                BeginSaveAndLoad();
                return;
            }

            if (runtime.CurrentGame is not null &&
                !ReferenceEquals(runtime.CurrentGame, originalGame) &&
                runtime.IsPlayable)
            {
                outcome = GatewayEndToEndStepOutcome.Pass(
                    new Dictionary<string, string> { ["saveName"] = saveName });
            }
        }
        catch (Exception exception)
        {
            outcome = GatewayEndToEndStepOutcome.Fail(
                "e2e_save_load_failed",
                "RimWorld's native save/load workflow threw " + exception.GetType().Name + ".");
        }
    }

    private void BeginSaveAndLoad()
    {
        originalGame = runtime.CurrentGame;
        if (originalGame is null || !runtime.IsPlayable)
        {
            outcome = GatewayEndToEndStepOutcome.Fail(
                "e2e_game_not_playable",
                "A player-controlled game is required before native save/load.");
            return;
        }

        if (runtime.SavingTemporarilyDisabled)
        {
            outcome = GatewayEndToEndStepOutcome.Fail(
                "e2e_save_temporarily_disabled",
                "RimWorld currently prevents a native game save.");
            return;
        }

        if (runtime.SaveExists(saveName))
        {
            outcome = GatewayEndToEndStepOutcome.Fail(
                "e2e_save_already_exists",
                "The isolated E2E save name already exists and will not be overwritten.");
            return;
        }

        runtime.Save(saveName);
        if (!runtime.SaveExists(saveName) || !runtime.SaveIsNonEmpty(saveName))
        {
            outcome = GatewayEndToEndStepOutcome.Fail(
                "e2e_save_failed",
                "RimWorld's native save workflow did not create a nonempty save.");
            return;
        }

        loadRequested = true;
        runtime.Load(saveName);
    }
}

internal sealed class VerseGatewayEndToEndSaveLoadRuntime : IGatewayEndToEndSaveLoadRuntime
{
    public object? CurrentGame => Current.Game;

    public bool IsPlayable => Current.Game?.PlayerHasControl == true &&
                              Current.Game.CurrentMap is not null &&
                              !LongEventHandler.AnyEventNowOrWaiting;

    public bool SavingTemporarilyDisabled => GameDataSaveLoader.SavingIsTemporarilyDisabled;

    public bool SaveExists(string saveName) => File.Exists(SavePath(saveName));

    public bool SaveIsNonEmpty(string saveName)
    {
        var path = SavePath(saveName);
        return File.Exists(path) && new FileInfo(path).Length > 0;
    }

    public void Save(string saveName) => GameDataSaveLoader.SaveGame(saveName);

    public void Load(string saveName) => GameDataSaveLoader.LoadGame(saveName);

    private static string SavePath(string saveName) => GenFilePaths.FilePathForSavedGame(saveName);
}
