using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using Megamod.Features.DrugPolicyMaintenance.Core;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugPolicyMaintenance;

// Run with the isolated prelaunch drug-policy.enabled=false configuration. No fixture Defs or
// forced preset factory: initial native policies already exist before Arrange.
[RimWorldEndToEndTest("megamod.drug-policy.shared-catalog-preset-off", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 3600, MaxWallClockSeconds = 900)]
public sealed class NativeDrugCatalogPresetOffTests : IRimWorldEndToEndTest
{
    private readonly NativeDrugPolicyView view = new();
    private Game game = null!;
    private MegamodMod mod = null!;
    private DrugCatalogRead catalog = null!;
    private DrugPolicy policy = null!;
    private DrugPolicyEntry beer = null!;
    private DrugPolicy[] initialPolicies = null!;
    private bool initialJoy;
    private string initialValues = null!, pointerScope = null!;
    private Dialog_ManageDrugPolicies? dialog;
    private MainTabWindow_Assign? assign;
    private EditWindow_Log? ownedLog;
    private EndToEndNativeWindow window = null!;

    public void Arrange(IEndToEndContext context)
    {
        game = Current.Game ?? throw new InvalidOperationException("A disposable native game is required.");
        EndToEndAssert.True(game.PlayerHasControl && Find.CurrentMap != null && Prefs.DevMode);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>() &&
            !Find.WindowStack.IsOpen<EditWindow_Log>());
        mod = LoadedModManager.GetMod<MegamodMod>();
        catalog = ReadRoot("initialDrugCatalog") as DrugCatalogRead
            ?? throw new InvalidOperationException("The finalized shared initial drug catalog was not published.");
        AssertCatalog();
        initialPolicies = game.drugPolicyDatabase.AllPolicies.ToArray();
        EndToEndAssert.True(initialPolicies.Length > 0);
        policy = initialPolicies[0];
        beer = Enumerable.Range(0, policy.Count).Select(index => policy[index]).Single(row => row.drug.defName == "Beer");
        initialJoy = beer.allowedForJoy;
        initialValues = Values();
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() =>
        {
            RequireGame();
            beer.allowedForJoy = initialJoy; // Fallback cleanup; successful path restores through the checkbox.
            Find.TickManager.CurTimeSpeed = speed;
            if (dialog != null && Find.WindowStack.Windows.Contains(dialog)) dialog.Close(false);
            if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(false);
            if (assign != null && ReferenceEquals(Find.MainTabsRoot.OpenTab?.TabWindow, assign))
                Find.MainTabsRoot.EscapeCurrentTab(false);
        });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        int operations = 0, screenshots = 0;
        foreach (var step in Workflow())
        {
            if (step is ScreenshotStep) screenshots++;
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep) operations++;
            yield return step;
        }
        yield return new AssertionStep("complete off-state operation and screenshot allowance", _ =>
        {
            EndToEndAssert.Equal(15, operations);
            EndToEndAssert.Equal(4, screenshots);
            EndToEndAssert.True(operations * 15 + screenshots * 30 + 180 <= 900);
        });
    }

    private IEnumerable<EndToEndStep> Workflow()
    {
        yield return new CheckpointStep("initial shared catalog with preset feature off; supporting observation", _ => Evidence());
        yield return new NativeMainTabActionStep("open native Assign with preset maintenance off", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture ordinary Assign", value => assign = value);
        yield return new NativePointerScopeBeginStep("own native pointer for off-state policy controls", value => pointerScope = value);
        yield return Click("open native Manage drug policies", view.ManageButton(assign!).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture native drug-policy manager", value => dialog = value);
        yield return Click("select the constructor-created ordinary native policy", view.PolicyRow(dialog!, policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture original native Beer permission", value => dialog = value);
        yield return new AssertionStep("confirm original selection and current off-state catalog", _ =>
        {
            AssertCatalog();
            EndToEndAssert.True(ReferenceEquals(view.Selected(dialog!), policy));
            EndToEndAssert.Equal(initialValues, Values());
        });
        yield return Shot("native policy controls before editing with preset maintenance off");
        yield return Click("toggle Beer recreation through the ordinary checkbox", view.BeerJoyCheckbox(dialog!, policy).center);
        yield return new AssertionStep("observe the native permission change with shared catalog retained", _ =>
        {
            AssertCatalog();
            EndToEndAssert.Equal(!initialJoy, beer.allowedForJoy);
            EndToEndAssert.True(initialPolicies.SequenceEqual(game.drugPolicyDatabase.AllPolicies));
        });
        yield return Shot("native Beer recreation permission visibly changed while preset maintenance is off");
        yield return Capture<Dialog_ManageDrugPolicies>("recapture checkbox for native restoration", value => dialog = value);
        yield return Click("restore Beer recreation through the same native checkbox", view.BeerJoyCheckbox(dialog!, policy).center);
        yield return new AssertionStep("observe all original native policy values restored", _ =>
        {
            AssertCatalog();
            EndToEndAssert.Equal(initialValues, Values());
            EndToEndAssert.True(initialPolicies.SequenceEqual(game.drugPolicyDatabase.AllPolicies));
        });
        yield return Shot("native permission restored without enabling preset filtering");
        yield return new WindowCancelActionStep("close native drug policies", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore pointer after off-state controls", pointerScope);
        yield return new NativeMainTabActionStep("close native Assign", "Assign", false);
        yield return new NativeDeveloperLogActionStep("open native console after off-state policy edits", true);
        yield return new AssertionStep("retain only the newly opened console", _ =>
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Native console did not open."));
        yield return Shot("native console after shared-catalog and off-state policy workflow");
        yield return new NativeDeveloperLogActionStep("close the native console", false);
        yield return new CheckpointStep("off-state native workflow complete; no generator authority claimed", _ => Evidence());
    }

    private void AssertCatalog()
    {
        RequireGame();
        EndToEndAssert.False((bool)ReadRoot("drugPolicyEnabled")!, "Requires isolated prelaunch drug-policy.enabled=false.");
        EndToEndAssert.True(ReadRoot("drugPresets") == null && ReadRoot("drugBaseline") == null);
        EndToEndAssert.True(ReferenceEquals(catalog, ReadRoot("initialDrugCatalog")) && catalog.IsCurrent);
        // Diagnostic comparison only: no production reader or new catalog publication is created.
        var native = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.category == ThingCategory.Item && def.IsDrug).ToArray();
        EndToEndAssert.True(catalog.Entries.Count > 0 && catalog.Entries.Select(entry => entry.Identity).SequenceEqual(native));
        EndToEndAssert.True(catalog.Entries.All(entry => entry.Identity is ThingDef def &&
            entry.Addictive == def.IsAddictiveDrug && entry.Pleasure == def.IsPleasureDrug));
    }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game), "Never read or restore another Game.");
    private object? ReadRoot(string name) => (typeof(MegamodMod).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(MegamodMod).FullName, name)).GetValue(mod);
    private string Values() => string.Join("|", game.drugPolicyDatabase.AllPolicies.Select(item =>
        item.id + ":" + item.label + ":" + item.sourceDef?.defName + ":" + string.Join(";", Enumerable.Range(0, item.Count).Select(index =>
        {
            var row = item[index];
            return FormattableString.Invariant($"{row.drug.defName},{row.allowedForAddiction},{row.allowedForJoy},{row.allowScheduled},{row.daysFrequency},{row.onlyIfMoodBelow},{row.onlyIfJoyBelow},{row.takeToInventory}");
        }))));
    private Dictionary<string, string> Evidence() => new()
    {
        ["scope"] = "initial shared catalog and ordinary native policy edits with preset maintenance off; no applied exclusions or generation",
        ["presetEnabled"] = ReadRoot("drugPolicyEnabled")!.ToString()!, ["catalogCurrent"] = catalog.IsCurrent.ToString(),
        ["catalog"] = string.Join(";", catalog.Entries.Select(entry => ((ThingDef)entry.Identity).defName + ":" + entry.Addictive + ":" + entry.Pleasure)),
        ["policyValues"] = Values()
    };
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private NativeWindowClickActionStep Click(string name, Vector2 point) => new(name, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointerScope);
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> retain) where T : Window => new(name, windows =>
    {
        T current = Find.WindowStack.WindowOfType<T>() ?? throw new InvalidOperationException("Expected native window missing: " + typeof(T));
        window = windows.Single(item => item.NativeId == current.ID && item.RuntimeType == current.GetType().FullName);
        EndToEndAssert.True(window.Width > 0 && window.Height > 0 && window.UiScale > 0);
        retain(current);
    });
}
