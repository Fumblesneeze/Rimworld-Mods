using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LudeonTK;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugPolicyMaintenance;

// The fixture remains loaded while ordinary save/load constructs fresh native Games and databases.
[RimWorldEndToEndTest("megamod.drug-policy.preset-save-reload-controls", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod", "fumblesneeze.megamoddrugpresetsfixture",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 900)]
public sealed class NativeDrugPresetPersistenceTests : IRimWorldEndToEndTest
{
    private const string FixtureDefName = "MegamodDrugPresetFixtureRecovery";
    private readonly string saveName = "Megamod-Drug-Preset-" + Guid.NewGuid().ToString("N");
    private readonly string pawnMarker = "MegamodDrugPreset-" + Guid.NewGuid().ToString("N");
    private readonly NativeDrugPolicyView view = new();
    private readonly HashSet<Window> ownedWindows = new();
    private IEndToEndContext context = null!;
    private Game initialGame = null!;
    private Game? loadedGame;
    private DrugPolicy policy = null!;
    private Pawn pawn = null!;
    private Pawn? initialPawn;
    private string? pawnId;
    private int mapId;
    private int policyId;
    private int originalPawnPolicyId;
    private int defaultPolicyId;
    private string initialValues = null!;
    private string editedValues = null!;
    private string presetValues = null!;
    private bool saveRequested;
    private Dialog_ManageDrugPolicies? dialog;
    private MainTabWindow_Assign? assign;
    private FloatMenu? menu;
    private EndToEndNativeWindow window = null!;
    private string pointerScope = null!;

    public void Arrange(IEndToEndContext context)
    {
        this.context = context;
        initialGame = Current.Game ?? throw new InvalidOperationException("A disposable loaded game is required.");
        var map = initialGame.CurrentMap ?? throw new InvalidOperationException("A disposable loaded map is required.");
        EndToEndAssert.True(initialGame.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null);
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>() || Find.WindowStack.IsOpen<FloatMenu>());
        EndToEndAssert.True(Prefs.DevMode && !Find.WindowStack.IsOpen<EditWindow_Log>());
        EndToEndAssert.Equal(0, Find.Selector.SelectedObjectsListForReading.Count);
        mapId = map.uniqueID;
        policy = initialGame.drugPolicyDatabase.AllPolicies.Single(item => item.sourceDef?.defName == FixtureDefName);
        policyId = policy.id;
        defaultPolicyId = initialGame.drugPolicyDatabase.AllPolicies[0].id;
        AssertFixtureChoices(false);
        initialValues = PolicyValues(initialGame);
        editedValues = PolicyValues(initialGame, true);
        presetValues = PresetValues();

        string savePath = GenFilePaths.FilePathForSavedGame(saveName);
        EndToEndAssert.False(File.Exists(savePath), "Never overwrite an existing save.");
        var speed = Find.TickManager.CurTimeSpeed;
        var positionField = typeof(CameraDriver).GetField("rootPos", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Cannot retain the native camera position.");
        var position = (Vector3)positionField.GetValue(Find.CameraDriver)!;
        float size = Find.CameraDriver.RootSize;

        // Each action is independent; a failed restoration must not prevent another object's cleanup.
        // Registered first, removed last: its persisted marker remains available for every other guard.
        context.DeferCleanup(() =>
        {
            var owned = PawnForCleanup(CleanupGame());
            if (owned != null && !owned.Destroyed) owned.Destroy(DestroyMode.Vanish);
        });
        context.DeferCleanup(() =>
        {
            if (!saveRequested || !File.Exists(savePath)) return;
            _ = CleanupGame();
            File.Delete(savePath);
        });
        context.DeferCleanup(() => CleanupGame().tickManager.CurTimeSpeed = speed);
        context.DeferCleanup(() =>
        {
            _ = CleanupGame();
            Find.CameraDriver.SetRootPosAndSize(position, size);
        });
        context.DeferCleanup(() =>
        {
            var current = FixturePolicy(CleanupGame());
            Entry(current, "Beer").allowedForJoy = false; // Interrupted-run cleanup only.
        });
        context.DeferCleanup(() =>
        {
            var game = CleanupGame();
            var owned = PawnForCleanup(game);
            if (owned != null && !owned.Destroyed && ReferenceEquals(owned.drugs.CurrentPolicy, FixturePolicy(game)))
                owned.drugs.CurrentPolicy = game.drugPolicyDatabase.AllPolicies.Single(item => item.id == originalPawnPolicyId);
        });

        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        initialPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        pawn = initialPawn;
        pawn.Name = new NameSingle(pawnMarker); // Native saved fixture identity, not a product result.
        pawnId = pawn.ThingID;
        originalPawnPolicyId = pawn.drugs.CurrentPolicy.id;
        EndToEndAssert.True(originalPawnPolicyId != policyId);
        var cell = CellRect.CenteredOn(map.Center, 20).First(candidate => candidate.InBounds(map) && candidate.Walkable(map));
        GenSpawn.Spawn(pawn, cell, map); // Disposable assignment target; does not construct the policy result.
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return Shot("map before editing the preset for ordinary save reload");
        yield return new NativeMainTabActionStep("open Assign before the saved edit", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture Assign before Manage drug policies", value => assign = value);
        yield return new NativePointerScopeBeginStep("own the pointer for pre-save drug-policy controls", value => pointerScope = value);
        yield return Click("open the native drug-policy manager before saving", view.ManageButton(assign!).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture the manager before the saved edit", value => dialog = value);
        yield return Click("select the constructor-created fixture policy before saving", view.PolicyRow(dialog!, policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture the selected policy before its saved edit", value => dialog = value);
        yield return Shot("Beer recreation initially unchecked with preserved preset schedules");
        yield return Click("enable Beer recreation through the native checkbox for persistence", view.BeerJoyCheckbox(dialog!, policy).center);
        yield return new AssertionStep("the pre-save native edit changes only Beer recreation", _ => AssertState(true));
        yield return Shot("Beer recreation enabled before saving");
        yield return new WindowCancelActionStep("close drug policies before assigning the saved policy", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return Capture<MainTabWindow_Assign>("capture Assign before the saved assignment", value => assign = value);
        yield return Click("open the colonist's native policy choices before saving", view.PawnPolicyCell(assign!, pawn).center);
        yield return Capture<FloatMenu>("capture the policy choices before saving", value => menu = value);
        yield return Click("assign the recovered policy through its native option before saving", view.MenuOption(menu!, policy.label).center);
        yield return new AssertionStep("the native assignment points at the policy to be saved", _ =>
            EndToEndAssert.True(ReferenceEquals(pawn.drugs.CurrentPolicy, policy)));
        yield return Capture<MainTabWindow_Assign>("capture the assignment before saving", value => assign = value);
        yield return Shot("fixture policy assigned before ordinary save reload");
        yield return new CheckpointStep("native edited choices and assignment before saving", _ => Evidence());
        yield return new NativePointerScopeEndStep("restore the pointer before replacing the native game", pointerScope);
        yield return new NativeMainTabActionStep("close Assign before ordinary save reload", "Assign", false);
        yield return new AssertionStep("retain ownership of the unique save and retire old window handles", _ =>
        {
            AssertState(true);
            EndToEndAssert.False(File.Exists(GenFilePaths.FilePathForSavedGame(saveName)));
            EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null);
            dialog = null;
            assign = null;
            menu = null;
            saveRequested = true;
        });
        yield return new SaveLoadActionStep("save and reload the edited preset through native game commands", saveName);
        yield return new AssertionStep("reacquire the loaded policy and pawn by their persisted identities", _ =>
        {
            // Check the saved marker and identities before binding the replacement as owned.
            var game = CleanupGame();
            EndToEndAssert.False(ReferenceEquals(game, initialGame));
            var loadedPolicy = FixturePolicy(game);
            var loadedPawn = PawnForCleanup(game) ?? throw new InvalidOperationException("The saved assignment target was not restored.");
            EndToEndAssert.False(ReferenceEquals(loadedPolicy, policy));
            EndToEndAssert.False(ReferenceEquals(loadedPawn, initialPawn));
            loadedGame = game;
            policy = loadedPolicy;
            pawn = loadedPawn;
            AssertState(true);
            EndToEndAssert.True(ReferenceEquals(pawn.drugs.CurrentPolicy, policy));
            EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && Find.TickManager.Paused);
        });
        yield return Shot("reloaded map before reopening native drug-policy controls");
        yield return new NativeMainTabActionStep("open Assign after ordinary save reload", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture the loaded assignment", value => assign = value);
        yield return Shot("Assign retains the fixture policy after reload");
        yield return new NativePointerScopeBeginStep("own the pointer for loaded policy controls", value => pointerScope = value);
        yield return Click("open Manage drug policies in the loaded game", view.ManageButton(assign!).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture the loaded drug-policy manager", value => dialog = value);
        yield return Click("select the saved fixture policy in the loaded manager", view.PolicyRow(dialog!, policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture the saved checkbox and following preset choices", value => dialog = value);
        yield return new AssertionStep("native loaded controls expose the retained edit", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(view.Selected(dialog!), policy));
            AssertState(true);
        });
        yield return Shot("Beer recreation remains checked after reload with other preset choices intact");
        yield return new CheckpointStep("native loaded policy choices and assignment", _ => Evidence());
        yield return Click("restore Beer recreation through the loaded native checkbox", view.BeerJoyCheckbox(dialog!, policy).center);
        yield return new AssertionStep("the loaded checkbox still edits the intended policy", _ => AssertState(false));
        yield return Shot("loaded Beer checkbox restored through native UI");
        yield return new WindowCancelActionStep("close the loaded manager before assignment restoration", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return Capture<MainTabWindow_Assign>("capture Assign for native restoration after reload", value => assign = value);
        yield return Click("open the loaded colonist's native policy dropdown", view.PawnPolicyCell(assign!, pawn).center);
        yield return Capture<FloatMenu>("capture the saved original assignment choice", value => menu = value);
        var originalPolicy = CleanupGame().drugPolicyDatabase.AllPolicies.Single(item => item.id == originalPawnPolicyId);
        yield return Click("restore the original pawn policy through the loaded native menu", view.MenuOption(menu!, originalPolicy.label).center);
        yield return new AssertionStep("native restoration preserves saved policy identities and default", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(pawn.drugs.CurrentPolicy, originalPolicy));
            AssertState(false);
        });
        yield return Shot("original pawn assignment restored after reload");
        yield return new NativePointerScopeEndStep("restore the pointer after the loaded controls", pointerScope);
        yield return new NativeMainTabActionStep("close Assign after the persistence workflow", "Assign", false);
        yield return new AssertionStep("the native console is closed before scenario acquisition", _ =>
            EndToEndAssert.False(Find.WindowStack.IsOpen<EditWindow_Log>()));
        yield return new NativeDeveloperLogActionStep("open the native console after preset persistence", true);
        yield return Capture<EditWindow_Log>("retain the exact console opened by this scenario", _ => { });
        yield return Shot("native console after preset save reload and native restoration");
        yield return new NativeDeveloperLogActionStep("close the native console after persistence evidence", false);
    }

    private void AssertState(bool beerJoy)
    {
        var game = CleanupGame();
        AssertFixtureChoices(beerJoy);
        EndToEndAssert.Equal(beerJoy ? editedValues : initialValues, PolicyValues(game));
        EndToEndAssert.Equal(defaultPolicyId, game.drugPolicyDatabase.AllPolicies[0].id);
        EndToEndAssert.Equal(presetValues, PresetValues());
    }

    private void AssertFixtureChoices(bool beerJoy)
    {
        EndToEndAssert.True(policy.sourceDef.defName == FixtureDefName && policy.label == policy.sourceDef.LabelCap.ToString());
        var rows = Enumerable.Range(0, policy.Count).Select(i => policy[i]).ToArray();
        EndToEndAssert.True(rows.All(row => row.drug != null && row.drug.IsDrug && row.drug.defName != "Steel"));
        EndToEndAssert.Equal(rows.Length, rows.Select(row => row.drug).Distinct().Count());
        var beer = Entry(policy, "Beer");
        EndToEndAssert.True(!beer.allowedForAddiction && beer.allowedForJoy == beerJoy && beer.allowScheduled &&
            beer.daysFrequency == 2 && beer.onlyIfMoodBelow == 0.65f && beer.onlyIfJoyBelow == 0.75f && beer.takeToInventory == 2);
        var smokeleaf = Entry(policy, "SmokeleafJoint");
        EndToEndAssert.True(smokeleaf.allowedForAddiction && smokeleaf.allowedForJoy && smokeleaf.allowScheduled &&
            smokeleaf.daysFrequency == 3 && smokeleaf.onlyIfMoodBelow == 0.45f && smokeleaf.onlyIfJoyBelow == 0.55f && smokeleaf.takeToInventory == 1);
        EndToEndAssert.True(Entry(policy, "Yayo").allowedForJoy);
    }

    private Game CleanupGame()
    {
        var game = Current.Game ?? throw new InvalidOperationException("Cannot clean an absent game during a save transition.");
        if (!game.PlayerHasControl || LongEventHandler.AnyEventNowOrWaiting || game.CurrentMap?.uniqueID != mapId)
            throw new InvalidOperationException("The owned map is not playable outside a native transition.");
        if (ReferenceEquals(game, initialGame)) return game;
        // A completed load can precede the rebinding assertion. Map/Thing IDs alone can collide in
        // another game, so require the unique native-saved marker before accepting any replacement.
        if (!saveRequested || (loadedGame != null && !ReferenceEquals(game, loadedGame)))
            throw new InvalidOperationException("The current game is not the completed owned save transition.");
        if (PawnForCleanup(game) == null ||
            !game.drugPolicyDatabase.AllPolicies.Any(item => item.id == originalPawnPolicyId))
            throw new InvalidOperationException("The loaded game lacks this scenario's saved pawn marker or policy identity.");
        _ = FixturePolicy(game);
        return game;
    }

    private DrugPolicy FixturePolicy(Game game) => game.drugPolicyDatabase.AllPolicies.Single(item =>
        item.id == policyId && item.sourceDef?.defName == FixtureDefName);

    private Pawn? PawnForCleanup(Game game)
    {
        if (ReferenceEquals(game, initialGame)) return initialPawn;
        var map = game.CurrentMap ?? throw new InvalidOperationException("The owned loaded map is absent.");
        return map.mapPawns.AllPawnsSpawned.SingleOrDefault(item =>
            item.ThingID == pawnId && item.Name is NameSingle name && name.Name == pawnMarker);
    }

    private static DrugPolicyEntry Entry(DrugPolicy value, string defName) =>
        Enumerable.Range(0, value.Count).Select(i => value[i]).Single(row => row.drug.defName == defName);

    private string PolicyValues(Game game, bool? beerJoyOverride = null) => string.Join("\n",
        game.drugPolicyDatabase.AllPolicies.Select(item => item.id + ":" + item.label + ":" + item.sourceDef?.defName + ":" +
            string.Join("|", Enumerable.Range(0, item.Count).Select(index =>
            {
                var row = item[index];
                bool joy = item.id == policyId && row.drug.defName == "Beer" && beerJoyOverride.HasValue
                    ? beerJoyOverride.Value : row.allowedForJoy;
                return RowValues(row, joy);
            }))));

    private static string PresetValues()
    {
        var definition = DefDatabase<DrugPolicyDef>.GetNamed(FixtureDefName);
        return definition.label + ":" + definition.allowPleasureDrugs + ":" +
            string.Join("|", definition.entries.Select(row => RowValues(row, row.allowedForJoy)));
    }

    private static string RowValues(DrugPolicyEntry row, bool joy) => FormattableString.Invariant(
        $"{row.drug.defName},{row.allowedForAddiction},{joy},{row.allowScheduled},{row.daysFrequency},{row.onlyIfMoodBelow},{row.onlyIfJoyBelow},{row.takeToInventory}");

    private Dictionary<string, string> Evidence() => new()
    {
        ["fixture"] = FixtureDefName, ["policyId"] = policyId.ToString(), ["pawnId"] = pawnId!, ["pawnMarker"] = pawnMarker,
        ["saveName"] = saveName, ["gameReplaced"] = (!ReferenceEquals(Current.Game, initialGame)).ToString(),
        ["policyValues"] = PolicyValues(CleanupGame()), ["presetValues"] = PresetValues(),
        ["assignmentPolicyId"] = pawn.drugs.CurrentPolicy.id.ToString(), ["defaultPolicyId"] = defaultPolicyId.ToString()
    };

    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private NativeWindowClickActionStep Click(string name, Vector2 point) => new(name, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointerScope);

    private NativeWindowCaptureStep Capture<T>(string name, Action<T> retain) where T : Window => new(name, windows =>
    {
        T current = Find.WindowStack.WindowOfType<T>() ?? throw new InvalidOperationException("Expected native window is missing: " + typeof(T));
        // Register ownership before any later capture/geometry assertion can fail.
        if (ownedWindows.Add(current))
            context.DeferCleanup(() =>
            {
                if (current is MainTabWindow_Assign && ReferenceEquals(Find.MainTabsRoot.OpenTab?.TabWindow, current))
                {
                    _ = CleanupGame();
                    Find.MainTabsRoot.EscapeCurrentTab(false);
                }
                else if (Find.WindowStack.Windows.Contains(current))
                {
                    _ = CleanupGame();
                    current.Close(false);
                }
            });
        retain(current);
        window = windows.Single(item => item.NativeId == current.ID && item.RuntimeType == current.GetType().FullName);
        EndToEndAssert.True(window.Width > 0 && window.Height > 0 && window.UiScale > 0);
    });
}
