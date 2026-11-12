using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugPolicyMaintenance;

// Initial native Game construction consumes the XML preset before Arrange. No policy is created here.
[RimWorldEndToEndTest("megamod.drug-policy.initial-preset-native-controls", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod", "fumblesneeze.megamoddrugpresetsfixture",
    MaxFrames = int.MaxValue, MaxGameTicks = 3600, MaxWallClockSeconds = 900)]
public sealed class NativeDrugPresetTests : IRimWorldEndToEndTest
{
    private DrugPolicy policy = null!;
    private DrugPolicy originalPawnPolicy = null!;
    private DrugPolicy[] originalPolicies = null!;
    private DrugPolicyEntry beer = null!;
    private Pawn pawn = null!;
    private Dialog_ManageDrugPolicies? dialog;
    private MainTabWindow_Assign? assign;
    private FloatMenu? menu;
    private EditWindow_Log? ownedLog;
    private EndToEndNativeWindow window = null!;
    private string pointerScope = null!;
    private NativeDrugPolicyView view = null!;
    private string initialPolicyValues = null!;

    public void Arrange(IEndToEndContext context)
    {
        var game = Current.Game ?? throw new InvalidOperationException("A disposable loaded game is required.");
        var map = Find.CurrentMap ?? throw new InvalidOperationException("A disposable loaded map is required.");
        EndToEndAssert.True(game.PlayerHasControl);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null, "Start with no native main tab open.");
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>() || Find.WindowStack.IsOpen<FloatMenu>());
        EndToEndAssert.True(Prefs.DevMode && !Find.WindowStack.IsOpen<EditWindow_Log>(),
            "The isolated console workflow requires developer mode and an initially closed log.");
        var definition = DefDatabase<DrugPolicyDef>.GetNamed("MegamodDrugPresetFixtureRecovery");
        originalPolicies = game.drugPolicyDatabase.AllPolicies.ToArray();
        policy = originalPolicies.Single(item => ReferenceEquals(item.sourceDef, definition));
        beer = Enumerable.Range(0, policy.Count).Select(i => policy[i]).Single(row => row.drug.defName == "Beer");
        AssertPreset(false);
        initialPolicyValues = AllPolicyValues();
        view = new NativeDrugPolicyView();
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() => Find.TickManager.CurTimeSpeed = speed);
        context.DeferCleanup(() =>
        {
            // Cleanup only. Successful execution restores both changes through native controls.
            if (beer.allowedForJoy) beer.allowedForJoy = false;
            if (pawn != null && !pawn.Destroyed)
            {
                if (ReferenceEquals(pawn.drugs.CurrentPolicy, policy)) pawn.drugs.CurrentPolicy = originalPawnPolicy;
                pawn.Destroy(DestroyMode.Vanish);
            }
        });
        context.DeferCleanup(() => { if (dialog != null && Find.WindowStack.Windows.Contains(dialog)) dialog.Close(false); });
        context.DeferCleanup(() => { if (menu != null && Find.WindowStack.Windows.Contains(menu)) menu.Close(false); });
        context.DeferCleanup(() => { if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(false); });
        context.DeferCleanup(() =>
        {
            if (assign != null && ReferenceEquals(Find.MainTabsRoot.OpenTab?.TabWindow, assign))
                Find.MainTabsRoot.EscapeCurrentTab(false);
        });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        originalPawnPolicy = pawn.drugs.CurrentPolicy;
        EndToEndAssert.False(ReferenceEquals(originalPawnPolicy, policy));
        var cell = CellRect.CenteredOn(map.Center, 20).First(candidate => candidate.InBounds(map) && candidate.Walkable(map));
        GenSpawn.Spawn(pawn, cell, map); // Disposable assignment target, not preset/result construction.
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep("initial native constructor policy before any UI action", _ => Evidence());
        yield return Shot("before opening the native Assign tab");
        yield return new NativeMainTabActionStep("open ordinary Assign through its native button", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture the native Assign table", value => assign = value);
        yield return Shot("Assign before opening Manage drug policies");
        yield return new NativePointerScopeBeginStep("own native pointer during drug-policy controls", value => pointerScope = value);
        yield return Click("open Manage drug policies through the native column header", view.ManageButton(assign!).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture the native drug-policy manager", value => dialog = value);
        yield return Shot("policy manager before choosing the constructor-created fixture preset");
        yield return Click("select the constructor-created fixture policy", view.PolicyRow(dialog!, policy).center);
        yield return new AssertionStep("the native policy list selected the original object", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(view.Selected(dialog!), policy));
            AssertPreset(false);
        });
        yield return Capture<Dialog_ManageDrugPolicies>("capture visible valid preset controls", value => dialog = value);
        yield return Shot("preserved Beer and Smokeleaf schedules after the skipped Steel row");
        yield return new CheckpointStep("native selected policy and following choices", _ => Evidence());
        yield return Click("enable Beer recreation through its native checkbox", view.BeerJoyCheckbox(dialog!, policy).center);
        yield return Shot("Beer recreation checkbox visibly enabled");
        yield return new AssertionStep("native checkbox changes only the selected permission", _ => AssertPreset(true));
        yield return Capture<Dialog_ManageDrugPolicies>("recapture the same checkbox for native restoration", value => dialog = value);
        yield return Click("restore Beer recreation through the native checkbox", view.BeerJoyCheckbox(dialog!, policy).center);
        yield return new AssertionStep("native control restored all original policy values", _ =>
        {
            AssertPreset(false);
            EndToEndAssert.Equal(initialPolicyValues, AllPolicyValues());
        });
        yield return Shot("Beer permission restored while other preset controls remain unchanged");
        yield return new WindowCancelActionStep("close drug policies through native cancel", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return Capture<MainTabWindow_Assign>("recapture Assign before choosing a pawn policy", value => assign = value);
        yield return Click("open the generated colonist's native policy dropdown", view.PawnPolicyCell(assign!, pawn).center);
        yield return Capture<FloatMenu>("capture native policy choices", value => menu = value);
        yield return Shot("native pawn policy choices before assigning the fixture preset");
        yield return Click("assign the recovered fixture policy through its native menu option", view.MenuOption(menu!, policy.label).center);
        yield return new AssertionStep("native assignment refers to the original recovered policy", _ =>
            EndToEndAssert.True(ReferenceEquals(pawn.drugs.CurrentPolicy, policy)));
        yield return Capture<MainTabWindow_Assign>("capture the visible assignment result", value => assign = value);
        yield return Shot("Assign shows the recovered fixture policy on the colonist");
        yield return new CheckpointStep("native policy assignment result", _ => Evidence());
        yield return Click("reopen the native assignment dropdown for restoration", view.PawnPolicyCell(assign!, pawn).center);
        yield return Capture<FloatMenu>("capture original policy restoration choice", value => menu = value);
        yield return Click("restore the colonist's original policy through the native menu", view.MenuOption(menu!, originalPawnPolicy.label).center);
        yield return new AssertionStep("native restoration preserved policies and default status", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(pawn.drugs.CurrentPolicy, originalPawnPolicy));
            EndToEndAssert.True(originalPolicies.SequenceEqual(Current.Game.drugPolicyDatabase.AllPolicies));
            EndToEndAssert.Equal(initialPolicyValues, AllPolicyValues());
        });
        yield return Shot("original pawn assignment restored");
        yield return new NativePointerScopeEndStep("restore pointer after native policy workflow", pointerScope);
        yield return new NativeMainTabActionStep("close the ordinary Assign tab", "Assign", false);
        yield return new AssertionStep("native log is still closed before acquiring the console window", _ =>
            EndToEndAssert.False(Find.WindowStack.IsOpen<EditWindow_Log>()));
        yield return new NativeDeveloperLogActionStep("open the native developer console after drug-policy actions", true);
        yield return new AssertionStep("retain the exact newly opened native console", _ =>
        {
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>()
                ?? throw new InvalidOperationException("The native developer console did not open.");
        });
        yield return Shot("native developer console after preset recovery editing and assignment");
        yield return new NativeDeveloperLogActionStep("restore the closed native developer console", false);
    }

    private void AssertPreset(bool beerJoy)
    {
        EndToEndAssert.True(originalPolicies.SequenceEqual(Current.Game.drugPolicyDatabase.AllPolicies));
        EndToEndAssert.True(policy.sourceDef.defName == "MegamodDrugPresetFixtureRecovery" && policy.label == policy.sourceDef.LabelCap.ToString());
        var rows = Enumerable.Range(0, policy.Count).Select(i => policy[i]).ToArray();
        EndToEndAssert.False(rows.Any(row => row.drug == null || row.drug.defName == "Steel" || !row.drug.IsDrug));
        EndToEndAssert.True(rows.Select(row => row.drug).Distinct().Count() == rows.Length);
        EndToEndAssert.True(!beer.allowedForAddiction && beer.allowedForJoy == beerJoy && beer.allowScheduled &&
            beer.daysFrequency == 2 && beer.onlyIfMoodBelow == 0.65f && beer.onlyIfJoyBelow == 0.75f && beer.takeToInventory == 2);
        var smokeleaf = rows.Single(row => row.drug.defName == "SmokeleafJoint");
        EndToEndAssert.True(smokeleaf.allowedForAddiction && smokeleaf.allowedForJoy && smokeleaf.allowScheduled &&
            smokeleaf.daysFrequency == 3 && smokeleaf.onlyIfMoodBelow == 0.45f && smokeleaf.onlyIfJoyBelow == 0.55f && smokeleaf.takeToInventory == 1);
        EndToEndAssert.True(rows.Single(row => row.drug.defName == "Yayo").allowedForJoy,
            "Broad pleasure permission must survive independently of the following explicit rows.");
    }

    private string AllPolicyValues() => string.Join("\n", Current.Game.drugPolicyDatabase.AllPolicies.Select(item =>
        item.id + ":" + item.label + ":" + item.sourceDef?.defName + ":" + string.Join("|", Enumerable.Range(0, item.Count).Select(index =>
        {
            var row = item[index];
            return FormattableString.Invariant($"{row.drug.defName},{row.allowedForAddiction},{row.allowedForJoy},{row.allowScheduled},{row.daysFrequency},{row.onlyIfMoodBelow},{row.onlyIfJoyBelow},{row.takeToInventory}");
        }))));

    private Dictionary<string, string> Evidence() => new()
    {
        ["fixture"] = policy.sourceDef.defName, ["policyId"] = policy.id.ToString(),
        ["nativeFactoryTrigger"] = "initial Game.drugPolicyDatabase constructor, before Arrange",
        ["policyValues"] = AllPolicyValues(), ["assignment"] = pawn?.drugs.CurrentPolicy?.label ?? "not arranged"
    };
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private NativeWindowClickActionStep Click(string name, Vector2 point) => new(name, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointerScope);
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> retain) where T : Window => new(name, windows =>
    {
        T current = Find.WindowStack.WindowOfType<T>() ?? throw new InvalidOperationException("Expected native window is missing: " + typeof(T));
        window = windows.Single(item => item.NativeId == current.ID && item.RuntimeType == current.GetType().FullName);
        EndToEndAssert.True(window.Width > 0 && window.Height > 0 && window.UiScale > 0);
        retain(current);
    });
}
