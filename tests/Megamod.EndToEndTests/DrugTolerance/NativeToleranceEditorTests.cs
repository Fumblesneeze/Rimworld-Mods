using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Megamod.EndToEndTests.DrugPolicyMaintenance;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugTolerance;

[RimWorldEndToEndTest("megamod.drug-tolerance.native-editor", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 900)]
public sealed class NativeToleranceEditorTests : IRimWorldEndToEndTest
{
    private readonly NativeDrugPolicyView view = new();
    private readonly HashSet<Window> ownedWindows = new();
    private readonly string marker = "ToleranceSlider-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private IEndToEndContext context = null!;
    private Game game = null!;
    private DrugPolicy policy = null!;
    private DrugPolicyEntry beer = null!, smokeleaf = null!;
    private DrugToleranceChoice retained = null!, malformed = null!;
    private MainTabWindow_Assign assign = null!;
    private Dialog_ManageDrugPolicies manager = null!;
    private EndToEndNativeWindow window = null!;
    private string pointerScope = null!;
    private int nativeOperations, screenshots;

    public void Arrange(IEndToEndContext context)
    {
        this.context = context; game = Current.Game;
        EndToEndAssert.True(game.PlayerHasControl && Current.ProgramState == ProgramState.Playing && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>() &&
            !Find.WindowStack.IsOpen<EditWindow_Log>());
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() => { RequireGame(); Find.TickManager.CurTimeSpeed = speed; });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        // Disposable retained-value preconditions; every changed choice below uses the native slider.
        policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); policy.label = marker;
        context.DeferCleanup(() =>
        {
            RequireGame();
            if (policy.label == marker) game.drugPolicyDatabase.AllPolicies.Remove(policy);
        });
        beer = policy[DefDatabase<ThingDef>.GetNamed("Beer")];
        smokeleaf = policy[DefDatabase<ThingDef>.GetNamed("SmokeleafJoint")];
        beer.allowScheduled = smokeleaf.allowScheduled = true;
        beer.allowedForJoy = smokeleaf.allowedForJoy = false;
        beer.onlyIfMoodBelow = .65f; beer.onlyIfJoyBelow = .75f;
        retained = DrugToleranceChoice.Numeric(.125f);
        malformed = DrugToleranceChoice.Decode("unrecognized-retained-value");
        NativeDrugToleranceState.Write(beer, retained);
        NativeDrugToleranceState.Write(smokeleaf, malformed);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Workflow())
        {
            if (step is ScreenshotStep) screenshots++;
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep) nativeOperations++;
            yield return step;
        }
        yield return new AssertionStep("count the complete native slider workflow", _ =>
        {
            EndToEndAssert.Equal(27, nativeOperations);
            EndToEndAssert.Equal(9, screenshots);
            EndToEndAssert.True(nativeOperations * 15 + screenshots * 30 + 180 <= 900);
        });
    }

    private IEnumerable<EndToEndStep> Workflow()
    {
        yield return new NativeMainTabActionStep("open Assign for native tolerance editing", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture native policy manager entry", current => assign = current);
        yield return new NativePointerScopeBeginStep("own pointer during tolerance editing", value => pointerScope = value);
        yield return Click("open the native drug-policy manager", view.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture native policy list", current => manager = current);
        yield return Click("select the disposable policy", view.PolicyRow(manager, policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture selected policy and native sliders", current => manager = current);
        yield return Shot("retained 12.50 percent and needs-review values beside native mood and recreation sliders");
        yield return new AssertionStep("painting preserves exact fractional and malformed choices", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(retained, NativeDrugToleranceState.Read(beer)));
            EndToEndAssert.True(ReferenceEquals(malformed, NativeDrugToleranceState.Read(smokeleaf)));
        });
        yield return Click("press and release the native slider without dragging", NativeToleranceSliderView.Point(manager, policy, beer, 0));
        yield return Capture<Dialog_ManageDrugPolicies>("capture unchanged value after the ordinary native press", current => manager = current);
        yield return new AssertionStep("a press alone retains the exact fractional choice", _ =>
            EndToEndAssert.True(ReferenceEquals(retained, NativeDrugToleranceState.Read(beer))));
        yield return Shot("ordinary native slider press preserves 12.50 percent before dragging");
        foreach (var step in Set(beer, 0, 0f, "zero percent")) yield return step;
        foreach (var step in Set(beer, 100, 1f, "numeric hundred percent")) yield return step;
        foreach (var step in Set(beer, 20, .2f, "twenty percent")) yield return step;
        foreach (var step in Set(beer, 101, null, "unrestricted Any")) yield return step;
        foreach (var step in Set(smokeleaf, 101, null, "repair needs-review to Any at the unchanged handle")) yield return step;
        foreach (var step in Set(smokeleaf, 40, .4f, "forty percent")) yield return step;
        yield return new AssertionStep("slider edits preserve surrounding native policy controls", _ =>
        {
            EndToEndAssert.True(beer.allowScheduled && !beer.allowedForJoy && smokeleaf.allowScheduled && !smokeleaf.allowedForJoy);
            EndToEndAssert.Equal(.65f, beer.onlyIfMoodBelow); EndToEndAssert.Equal(.75f, beer.onlyIfJoyBelow);
            EndToEndAssert.True(ReferenceEquals(view.Selected(manager), policy));
            EndToEndAssert.Equal(DrugToleranceChoiceKind.Any, NativeDrugToleranceState.Read(beer).Kind);
        });
        yield return new CheckpointStep("native slider final owned choices", _ => new Dictionary<string, string>
        {
            ["policy"] = policy.id + ":" + policy.label,
            ["beerChoice"] = NativeDrugToleranceState.ReadEncoded(beer) ?? "absent",
            ["smokeleafChoice"] = NativeDrugToleranceState.ReadEncoded(smokeleaf) ?? "absent",
            ["workflow"] = "same native HorizontalSlider as mood/recreation; fractional paint/press retention, native drag to0, numeric100,20,Any,malformed repair",
            ["scope"] = "policy slider only; routine job acceptance is a separate workflow",
            ["maximumBudgetSeconds"] = "855 including 180 reserve; scenario900"
        });
        yield return new WindowCancelActionStep("close the native policy manager", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore pointer after tolerance editing", pointerScope);
        yield return new NativeMainTabActionStep("close Assign after tolerance editing", "Assign", false);
        yield return new NativeDeveloperLogActionStep("open native console after tolerance slider actions", true);
        yield return Capture<EditWindow_Log>("retain the owned native console", _ => { });
        yield return Shot("native console after the tolerance slider workflow");
        yield return new NativeDeveloperLogActionStep("close native console after tolerance editing", false);
    }

    private IEnumerable<EndToEndStep> Set(DrugPolicyEntry entry, float position, float? expected, string description)
    {
        EndToEndAssert.True(ReferenceEquals(view.Selected(manager), policy));
        var windowsBefore = Find.WindowStack.Windows.ToArray();
        yield return NativeToleranceSliderView.Drag("drag the native tolerance slider to " + description, window, pointerScope, manager, policy, entry, position);
        yield return Capture<Dialog_ManageDrugPolicies>("capture native policy after " + description, current => manager = current);
        yield return new AssertionStep("slider immediately commits " + description + " in the existing window", _ =>
        {
            var choice = NativeDrugToleranceState.Read(entry);
            EndToEndAssert.Equal(expected.HasValue ? DrugToleranceChoiceKind.Limit : DrugToleranceChoiceKind.Any, choice.Kind);
            if (expected.HasValue) EndToEndAssert.Equal(expected.Value, choice.Limit);
            EndToEndAssert.True(windowsBefore.SequenceEqual(Find.WindowStack.Windows));
        });
        yield return Shot("native policy slider displays " + description);
    }

    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game), "Refuse an unrelated game during slider cleanup or input.");
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private NativeWindowClickActionStep Click(string name, Vector2 local) => new(name, window.Handle,
        (window.X + local.x) * window.UiScale, (window.Y + local.y) * window.UiScale, null, pointerScope);
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> retain) where T : Window => new(name, windows =>
    {
        RequireGame();
        var current = Find.WindowStack.WindowOfType<T>() ?? throw new InvalidOperationException("Missing native window: " + typeof(T));
        if (ownedWindows.Add(current)) context.DeferCleanup(() =>
        {
            RequireGame();
            if (current is MainTabWindow_Assign && ReferenceEquals(Find.MainTabsRoot.OpenTab?.TabWindow, current)) Find.MainTabsRoot.EscapeCurrentTab(false);
            else if (Find.WindowStack.Windows.Contains(current)) current.Close(false);
        });
        retain(current);
        window = windows.Single(item => item.NativeId == current.ID && item.RuntimeType == current.GetType().FullName);
    });
}
