using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using LudeonTK;
using Megamod.EndToEndTests.DrugPolicyMaintenance;
using Megamod.Features.DrugTolerance;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugTolerance;

[RimWorldEndToEndTest("megamod.drug-tolerance.native-editor", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 1200)]
public sealed class NativeToleranceEditorTests : IRimWorldEndToEndTest
{
    private readonly NativeDrugPolicyView view = new();
    private readonly HashSet<Window> ownedWindows = new();
    private readonly string marker = "ToleranceEditor-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private IEndToEndContext context = null!;
    private Game game = null!;
    private DrugPolicy policy = null!;
    private DrugPolicyEntry beer = null!;
    private MainTabWindow_Assign assign = null!;
    private Dialog_ManageDrugPolicies manager = null!;
    private Dialog_DrugTolerance editor = null!;
    private EndToEndNativeWindow window = null!;
    private string pointerScope = null!;

    public void Arrange(IEndToEndContext context)
    {
        this.context = context; game = Current.Game;
        EndToEndAssert.True(game.PlayerHasControl && Current.ProgramState == ProgramState.Playing && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>() &&
            !Find.WindowStack.IsOpen<Dialog_DrugTolerance>() && !Find.WindowStack.IsOpen<EditWindow_Log>());
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() => { RequireGame(); Find.TickManager.CurTimeSpeed = speed; });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        // Disposable native policy setup; every accepted choice below is made through its UI.
        policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); policy.label = marker;
        context.DeferCleanup(() =>
        {
            RequireGame();
            if (policy.label == marker) game.drugPolicyDatabase.AllPolicies.Remove(policy);
        });
        beer = policy[DefDatabase<ThingDef>.GetNamed("Beer")];
        beer.allowScheduled = true; beer.allowedForJoy = false;
        EndToEndAssert.Equal(DrugToleranceChoiceKind.Any, NativeDrugToleranceState.Read(beer).Kind);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativeMainTabActionStep("open Assign for native tolerance editing", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture native policy manager entry", current => assign = current);
        yield return new NativePointerScopeBeginStep("own pointer during tolerance editing", value => pointerScope = value);
        yield return Click("open the native drug-policy manager", view.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture native policy list", current => manager = current);
        yield return Click("select the disposable policy", view.PolicyRow(manager, policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture selected policy and tolerance column", current => manager = current);
        yield return Shot("new policy shows Any beside native drug controls");
        foreach (var step in OpenEditor()) yield return step;
        yield return Click("switch Any off in the native contextual editor", AnyPoint());
        foreach (var step in Replace("NaN")) yield return step;
        yield return Shot("invalid native text leaves Apply disabled and policy unchanged");
        yield return Click("attempt the visibly disabled Apply button", ApplyPoint());
        yield return new AssertionStep("invalid input leaves the exact editor open and policy Any", _ =>
        {
            EndToEndAssert.True(editor.IsOpen && Find.WindowStack.Windows.Contains(editor));
            EndToEndAssert.Equal("NaN", editor.Percentage);
            EndToEndAssert.Equal(DrugToleranceChoiceKind.Any, NativeDrugToleranceState.Read(beer).Kind);
        });
        foreach (var step in Replace("0")) yield return step;
        yield return Shot("valid zero draft before native Apply");
        foreach (var step in Apply(0f)) yield return step;
        yield return Shot("native policy row shows numeric zero after Apply");

        foreach (var step in OpenEditor()) yield return step;
        foreach (var step in Replace("100")) yield return step;
        yield return Shot("edited hundred-percent draft before native Cancel");
        yield return Click("cancel the edited hundred-percent draft", CancelPoint());
        yield return Closed("native Cancel discards the draft");
        yield return Capture<Dialog_ManageDrugPolicies>("capture unchanged zero after Cancel", current => manager = current);
        yield return new AssertionStep("Cancel retains numeric zero", _ => AssertChoice(0f));
        yield return Shot("policy still shows zero after native Cancel");

        foreach (var step in OpenEditor()) yield return step;
        foreach (var step in Replace("100")) yield return step;
        foreach (var step in Apply(1f)) yield return step;
        yield return Shot("numeric hundred is a displayed limit distinct from Any");

        foreach (var step in OpenEditor()) yield return step;
        foreach (var step in Replace(12.5f.ToString(CultureInfo.CurrentCulture))) yield return step;
        foreach (var step in Apply(.125f)) yield return step;
        yield return Shot("native locale decimal is applied to the policy row");
        foreach (var step in OpenEditor()) yield return step;
        yield return Click("choose unrestricted Any in the contextual editor", AnyPoint());
        yield return Click("apply Any through the native button", ApplyPoint());
        yield return Closed("native Apply closes the Any draft");
        yield return Capture<Dialog_ManageDrugPolicies>("capture restored Any policy row", current => manager = current);
        yield return new AssertionStep("Any preserves the surrounding native policy choices", _ =>
        {
            EndToEndAssert.Equal(DrugToleranceChoiceKind.Any, NativeDrugToleranceState.Read(beer).Kind);
            EndToEndAssert.True(beer.allowScheduled && !beer.allowedForJoy && ReferenceEquals(view.Selected(manager), policy));
        });
        yield return Shot("Any is restored while native scheduling remains selected");
        yield return new CheckpointStep("native editor final owned choice", _ => new Dictionary<string, string>
        {
            ["policy"] = policy.id + ":" + policy.label,
            ["choice"] = NativeDrugToleranceState.ReadEncoded(beer) ?? "absent",
            ["workflow"] = "native tolerance button, Any checkbox, focused text replacement, Apply and Cancel",
            ["scope"] = "editor only; routine job acceptance is a separate workflow"
        });
        yield return new WindowCancelActionStep("close the native policy manager", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore pointer after tolerance editing", pointerScope);
        yield return new NativeMainTabActionStep("close Assign after tolerance editing", "Assign", false);
        yield return new NativeDeveloperLogActionStep("open native console after tolerance editor actions", true);
        yield return Capture<EditWindow_Log>("retain the owned native console", _ => { });
        yield return Shot("native console after the tolerance editor workflow");
        yield return new NativeDeveloperLogActionStep("close native console after tolerance editing", false);
    }

    private IEnumerable<EndToEndStep> OpenEditor()
    {
        yield return Click("open Beer tolerance through its native row button", RowPoint());
        yield return Capture<Dialog_DrugTolerance>("capture the contextual tolerance editor", current => editor = current);
    }
    private IEnumerable<EndToEndStep> Replace(string text)
    {
        var before = NativeDrugToleranceState.Read(beer);
        yield return Click("focus the native percentage text field", PercentagePoint());
        yield return Capture<Dialog_DrugTolerance>("bind the exact focused percentage field", current => editor = current);
        EndToEndAssert.True(!string.IsNullOrEmpty(window.FocusedTextControlHandle), "Native percentage field did not acquire an exact focused-control lease.");
        yield return new NativeWindowReplaceTextActionStep("type percentage through native focused input", window.Handle, window.FocusedTextControlHandle!, text);
        yield return new AssertionStep("native text changes only the unapplied draft", _ =>
        {
            EndToEndAssert.Equal(text, editor.Percentage);
            EndToEndAssert.True(ReferenceEquals(before, NativeDrugToleranceState.Read(beer)));
        });
    }
    private IEnumerable<EndToEndStep> Apply(float limit)
    {
        yield return Click("apply the numeric draft with the native button", ApplyPoint());
        yield return Closed("native Apply closes its exact contextual window");
        yield return Capture<Dialog_ManageDrugPolicies>("capture the applied native policy row", current => manager = current);
        yield return new AssertionStep("native Apply commits the exact reviewed percentage", _ => AssertChoice(limit));
    }
    private void AssertChoice(float limit)
    {
        var choice = NativeDrugToleranceState.Read(beer);
        EndToEndAssert.Equal(DrugToleranceChoiceKind.Limit, choice.Kind);
        EndToEndAssert.Equal(limit, choice.Limit);
    }
    private WaitUntilStep Closed(string name) => new(name, _ => !editor.IsOpen && !Find.WindowStack.Windows.Contains(editor),
        new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(5)));
    private Vector2 RowPoint()
    {
        RequireGame(); EndToEndAssert.True(ReferenceEquals(view.Selected(manager), policy));
        var scroll = (Vector2)typeof(Dialog_ManageDrugPolicies).GetField("scrollPosition", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
        EndToEndAssert.True(scroll == Vector2.zero);
        int index = Enumerable.Range(0, policy.Count).Single(i => ReferenceEquals(policy[i], beer));
        float rowY = 18 + 147 + index * 35;
        EndToEndAssert.True(147 + policy.Count * 35 < manager.windowRect.height - 36 - Window.CloseButSize.y - 11,
            "This bounded editor fixture needs native rows visible without scrolling.");
        // The admitted column occupies the right edge and has at least120 GUI units.
        return new(manager.windowRect.width - 18 - 1 - 60, rowY + 17.5f);
    }
    private float HelpHeight()
    {
        var font = Text.Font;
        try { Text.Font = GameFont.Small; return Text.CalcHeight("Megamod.DrugTolerance.RuleHelp".Translate(), editor.windowRect.width - 36); }
        finally { Text.Font = font; }
    }
    private Vector2 AnyPoint() => new(editor.windowRect.width - 18 - 12, 18 + 50 + HelpHeight() + 14);
    private Vector2 PercentagePoint() => new(editor.windowRect.width - 18 - 70, 18 + 86 + HelpHeight() + 15);
    private Vector2 ApplyPoint() => new(18 + (editor.windowRect.width - 36) * .75f + 2.5f, editor.windowRect.height - 18 - 17.5f);
    private Vector2 CancelPoint() => new(18 + (editor.windowRect.width - 36) * .25f - 2.5f, editor.windowRect.height - 18 - 17.5f);
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game), "Refuse an unrelated game during editor cleanup or input.");
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
