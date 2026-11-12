using System.Globalization;
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

// Explicit disposable window sizes arrange two layouts; only scale changes and manager
// actions are native player workflows. Windowed Waylith offers no alternate resolution choices.
// Budget:55 native operations*15 +6 screenshots*30 +35s waits +180s reserve =1220s.
// Attribute1500s; launcher1800s. No screenshot/wait between confirmation capture and native Keep.
[RimWorldEndToEndTest("megamod.drug-tolerance.native-layout-unavailable", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 1500)]
public sealed class NativeToleranceLayoutTests : IRimWorldEndToEndTest
{
    private readonly NativeDrugPolicyView view = new();
    private readonly List<Window> ownedWindows = new();
    private readonly List<Message> ownedNotices = new();
    private readonly string marker = "ToleranceLayout-" + Guid.NewGuid().ToString("N")[..8];
    private Game game = null!;
    private NativeDrugToleranceRuntime runtime = null!;
    private DrugPolicy policy = null!;
    private DrugPolicyEntry beer = null!;
    private DrugToleranceChoice retained = null!;
    private List<Message> messages = null!;
    private EndToEndNativeWindow window = null!;
    private MainTabWindow_Assign assign = null!;
    private Dialog_ManageDrugPolicies manager = null!;
    private string pointer = null!;
    private string phase = "setup";
    private HashSet<Window>? windowsBeforeChoice;
    private int nativeOperations, screenshots, waitSeconds;

    public void Arrange(IEndToEndContext context)
    {
        game = Current.Game ?? throw new InvalidOperationException("A disposable loaded Game is required.");
        EndToEndAssert.True(game.PlayerHasControl && Current.ProgramState == ProgramState.Playing &&
            !LongEventHandler.AnyEventNowOrWaiting && Prefs.DevMode);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null &&
            !Find.WindowStack.Windows.Any(w => w is Dialog_Options or Dialog_ResolutionConfirm or FloatMenu or
                Dialog_ManageDrugPolicies or EditWindow_Log));
        runtime = Read<NativeDrugToleranceRuntime>(typeof(MegamodMod), LoadedModManager.GetMod<MegamodMod>(), "drugTolerance");
        messages = Read<List<Message>>(typeof(Messages), null, "liveMessages");
        EndToEndAssert.Equal(0, messages.Count, "Start with an empty disposable notification surface.");
        EndToEndAssert.True(Screen.width == 1600 && Screen.height == 900 && Prefs.UIScalePreference == 1 &&
            Screen.fullScreenMode == FullScreenMode.Windowed &&
            Math.Abs(ResolutionUtility.DpiScale - 1) < .001f, "This bounded native counterexample requires1600x900,1x,DPI1.");
        EndToEndAssert.True(runtime.Available, "Require initially admitted tolerance controls: " + runtime.StatusKey);
        var scales = Read<float[]>(typeof(Dialog_Options), null, "UIScales");
        EndToEndAssert.True(scales.Count(s => s == 1) == 1 && scales.Count(s => s == 1.25f) == 1 &&
            ResolutionUtility.UIScaleSafeWithResolution(1.25f, 1280, 1024));

        var speed = Find.TickManager.CurTimeSpeed;
        int originalWidth = Screen.width, originalHeight = Screen.height;
        int preferenceWidth = Prefs.ScreenWidth, preferenceHeight = Prefs.ScreenHeight;
        float originalScale = Prefs.UIScalePreference;
        var mode = Screen.fullScreenMode; var preferenceMode = Prefs.FullScreenMode;
        // Registered first so exact owned windows close before process-level display restoration.
        // Screen dimensions publish on a later native frame; the runner owns the existing30s bound.
        bool restoreRequested = false;
        context.DeferCleanupUntil(() =>
        {
            if (!restoreRequested)
            {
                EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ResolutionConfirm>());
                Prefs.UIScalePreference = originalScale; GenUI.ClearLabelWidthCache();
                ResolutionUtility.SetResolutionRaw(originalWidth, originalHeight, mode);
                Prefs.ScreenWidth = preferenceWidth; Prefs.ScreenHeight = preferenceHeight; Prefs.FullScreenMode = preferenceMode;
                restoreRequested = true;
                return false;
            }
            return Screen.width == originalWidth && Screen.height == originalHeight && Screen.fullScreenMode == mode &&
                Prefs.UIScalePreference == originalScale && Prefs.ScreenWidth == preferenceWidth &&
                Prefs.ScreenHeight == preferenceHeight && Prefs.FullScreenMode == preferenceMode;
        });
        // Independent object/control cleanup still runs when a display request or observation fails.
        context.DeferCleanup(() =>
        {
            var errors = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
            // The native action may have opened its confirmation before a failed step result
            // prevented the iterator's next capture. Claim only its unique newly created native
            // confirmation in the same Game; never close a departed game's replacement window.
            Attempt(() =>
            {
                if (windowsBeforeChoice == null || !ReferenceEquals(Current.Game, game)) return;
                var pending = Find.WindowStack.Windows.OfType<Dialog_ResolutionConfirm>().Where(w => !windowsBeforeChoice.Contains(w)).ToArray();
                EndToEndAssert.True(pending.Length <= 1, "Ambiguous pending native display confirmation.");
                if (pending.Length == 1) ownedWindows.Add(pending[0]);
            });
            foreach (var owned in ownedWindows.AsEnumerable().Reverse()) Attempt(() =>
            {
                if (!Find.WindowStack.Windows.Contains(owned)) return;
                if (owned is MainTabWindow_Assign) Find.MainTabsRoot.EscapeCurrentTab(false);
                else owned.Close(false);
            });
            // An owned confirmation must be closed before restoring preferences: its WindowUpdate
            // can otherwise revert them again on the native ten-second timer.
            Attempt(() => EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ResolutionConfirm>()));
            foreach (var notice in ownedNotices) Attempt(() => messages.Remove(notice));
            Attempt(() => { RequireGame(); Find.TickManager.CurTimeSpeed = speed; });
            Attempt(() => { RequireGame(); if (policy != null && policy.label == marker) game.drugPolicyDatabase.AllPolicies.Remove(policy); });
            if (errors.Count != 0) throw new AggregateException("Tolerance layout cleanup failed.", errors);
        });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); policy.label = marker;
        beer = policy[DefDatabase<ThingDef>.GetNamed("Beer")];
        beer.allowedForJoy = true; beer.allowScheduled = true;
        retained = DrugToleranceChoice.Numeric(.125f);
        NativeDrugToleranceState.Write(beer, retained);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in DisplayPrecondition(1024, 768)) yield return step;
        yield return NoManager("narrow fixture allocation fails before manager paint", false);
        foreach (var step in ObserveUnavailable("narrow fixture allocation", true)) yield return step;
        foreach (var step in DisplayPrecondition(1280, 1024)) yield return step;
        yield return NoManager("larger fixture viewport fits before native scale change", true);
        foreach (var step in ChangeScale(1.25f)) yield return step;
        yield return NoManager("scaled viewport fails before manager paint", false);
        foreach (var step in ObserveUnavailable("scaled viewport", false)) yield return step;
        foreach (var step in ChangeScale(1f)) yield return step;
        yield return NoManager("native1x restores prospective fit before any manager paint", true);
        foreach (var step in OpenManager()) yield return step;
        yield return Click("select the retained disposable policy through the native list", view.PolicyRow(manager, policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture recovered policy controls", value => manager = value);
        yield return new AssertionStep("fresh supported layout exposes retained numeric controls", _ =>
        {
            RequireChoice(); EndToEndAssert.True(runtime.Available && ReferenceEquals(view.Selected(manager), policy));
            EndToEndAssert.True(WindowFits());
        });
        yield return Shot("native tolerance controls recovered after native1x scale choice");
        foreach (var step in CloseManager()) yield return step;
        foreach (var step in DisplayPrecondition(1600, 900)) yield return step;
        yield return NoManager("original fixture resolution restored", true);
        yield return N(new NativeDeveloperLogActionStep("open native console after layout transitions", true));
        yield return Capture<EditWindow_Log>("observe the exact native console", _ => { });
        yield return Shot("native console after the combined layout workflow");
        yield return N(new NativeDeveloperLogActionStep("close native console", false));
        yield return new CheckpointStep("final bounded layout evidence; no routine-use claim", _ =>
        {
            RequireChoice(); EndToEndAssert.Equal(55, nativeOperations); EndToEndAssert.Equal(6, screenshots);
            EndToEndAssert.Equal(35, waitSeconds); return Evidence();
        });
    }

    private CheckpointStep NoManager(string name, bool available) => new(name, _ =>
    {
        phase = name; RequireChoice(); EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>());
        EndToEndAssert.Equal(available, runtime.Available);
        EndToEndAssert.Equal(available ? null : "Megamod.DrugTolerance.LayoutUnavailable", runtime.StatusKey);
        return Evidence();
    });
    private IEnumerable<EndToEndStep> ObserveUnavailable(string name, bool windowFits)
    {
        phase = name;
        foreach (var old in ownedNotices) messages.Remove(old); // Only this fixture's earlier notice.
        foreach (var step in OpenManager()) yield return step;
        yield return Wait("native workflow displays its fresh layout-unavailable explanation", () =>
        {
            var notice = messages.FirstOrDefault(m => m.text == "Megamod.DrugTolerance.LayoutUnavailable".Translate().ToString().CapitalizeFirst());
            if (notice == null) return false;
            if (!ownedNotices.Contains(notice)) ownedNotices.Add(notice);
            return true;
        }, 5);
        yield return new CheckpointStep("observe current " + name + " bounds and preserved state", _ =>
        {
            RequireChoice(); EndToEndAssert.False(runtime.Available);
            EndToEndAssert.Equal(windowFits, WindowFits());
            return Evidence();
        });
        yield return Shot(name + " preserves native controls and explains unavailable tolerance");
        foreach (var step in CloseManager()) yield return step;
    }
    private IEnumerable<EndToEndStep> OpenManager()
    {
        yield return N(new NativeMainTabActionStep("open native Assign", "Assign", true));
        yield return Capture<MainTabWindow_Assign>("capture native Manage entry", value => assign = value);
        yield return N(new NativePointerScopeBeginStep("own manager pointer", value => pointer = value));
        yield return Click("open native drug-policy manager", view.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("observe actual manager including unsupported bounds", value => manager = value);
    }
    private IEnumerable<EndToEndStep> CloseManager()
    {
        yield return N(new WindowCancelActionStep("close native drug-policy manager", typeof(Dialog_ManageDrugPolicies).FullName!));
        yield return N(new NativePointerScopeEndStep("restore manager pointer", pointer));
        yield return N(new NativeMainTabActionStep("close native Assign", "Assign", false));
    }

    private IEnumerable<EndToEndStep> DisplayPrecondition(int width, int height)
    {
        // Persist the setup intent before the bounded iterator-side fixture mutation.
        yield return new CheckpointStep("arrange disposable window size; not a native resolution action", _ =>
            new Dictionary<string, string> { ["setupWidth"] = width.ToString(), ["setupHeight"] = height.ToString() });
        RequireGame();
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && Screen.fullScreenMode == FullScreenMode.Windowed &&
            !Find.WindowStack.Windows.Any(w => w is Dialog_Options or FloatMenu or Dialog_ResolutionConfirm or Dialog_ManageDrugPolicies));
        EndToEndAssert.True(ResolutionUtility.UIScaleSafeWithResolution(Prefs.UIScalePreference, width, height));
        ResolutionUtility.SetResolutionRaw(width, height, FullScreenMode.Windowed);
        yield return Wait("observe the disposable window-size precondition on a later frame", () =>
            Screen.width == width && Screen.height == height && Screen.fullScreenMode == FullScreenMode.Windowed, 5);
    }

    // Actual native Interface-scale selection and confirmation; no drawing/callback invocation.
    private IEnumerable<EndToEndStep> ChangeScale(float scale)
    {
        string choice = scale + "x";
        var category = OptionCategoryDefOf.Interface;
        yield return new AssertionStep("require a distinct safe native display transition", _ =>
        {
            RequireGame(); EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null &&
                !Find.WindowStack.Windows.Any(w => w is Dialog_Options or FloatMenu or Dialog_ResolutionConfirm));
            EndToEndAssert.True(ResolutionUtility.UIScaleSafeWithResolution(scale, Screen.width, Screen.height));
            EndToEndAssert.True(Prefs.UIScalePreference != scale);
        });
        yield return N(new EscapeMenuActionStep("open native Escape for display options", true));
        MainTabWindow_Menu menu = null!;
        yield return Capture<MainTabWindow_Menu>("capture native Escape menu", value => menu = value);
        yield return N(new NativePointerScopeBeginStep("own display-options pointer", value => pointer = value));
        yield return Click("open native Options", OptionsPoint(menu));
        Dialog_Options options = null!;
        yield return Capture<Dialog_Options>("capture native Options", value => options = value);
        var categories = DefDatabase<OptionCategoryDef>.AllDefs.Where(d => (Prefs.DevMode || !d.isDev) &&
            d.modContentPack.IsOfficialMod && (d != OptionCategoryDefOf.Mods || Read<bool>(typeof(Dialog_Options), options, "hasModSettings"))).ToList();
        int categoryIndex = categories.IndexOf(category);
        EndToEndAssert.True(categoryIndex >= 0);
        yield return Click("choose native display category", new(18 + 80, 18 + categoryIndex * 50 + 24));
        yield return Shot("native " + category.defName + " options before " + choice);
        yield return Capture<Dialog_Options>("recapture rendered native display options", value => options = value);
        EndToEndAssert.True(ReferenceEquals(Read<OptionCategoryDef>(typeof(Dialog_Options), options, "selectedCategory"), category) && options.selectedMod == null);
        float listingHeight = Read<float>(typeof(Dialog_Options), options, "optionsViewRectHeight");
        var scroll = Read<Vector2>(typeof(Dialog_Options), options, "optionsScrollPosition");
        EndToEndAssert.True(float.IsFinite(listingHeight) && listingHeight > 0 && scroll == Vector2.zero);
        float listWidth = (float)window.Width - 36 - 177 - (listingHeight > window.Height - 36 - 60 ? 26 : 0);
        yield return Click("open native display choices", new(18 + 177 + listWidth * .8f, 18 + 27));
        yield return new AssertionStep("bind the actual enabled target and restore choices", _ =>
        {
            var actual = Find.WindowStack.Windows.OfType<FloatMenu>().Single(); ownedWindows.Add(actual);
            var choices = Read<List<FloatMenuOption>>(typeof(FloatMenu), actual, "options");
            EndToEndAssert.Equal(1, choices.Count(o => o.Label == choice && !o.Disabled));
            string restore = 1f + "x";
            EndToEndAssert.Equal(1, choices.Count(o => o.Label == restore && !o.Disabled));
        });
        windowsBeforeChoice = Find.WindowStack.Windows.ToHashSet();
        yield return N(new CurrentFloatMenuActionStep("choose native " + choice, choice, captureSoleUnownedMenu: true));
        var confirmation = Find.WindowStack.Windows.OfType<Dialog_ResolutionConfirm>().Single();
        EndToEndAssert.False(windowsBeforeChoice.Contains(confirmation));
        ownedWindows.Add(confirmation);
        windowsBeforeChoice = null;
        // Capture registers this exact confirmation before any screenshot or delay can expire it.
        yield return Capture<Dialog_ResolutionConfirm>("capture native ten-second confirmation immediately", _ => { });
        yield return Click("keep native display change", new(18 + ((float)window.Width - 36) / 4 - 10, (float)window.Height - 18 - 17.5f));
        yield return Wait("observe native display change and confirmation closure", () =>
            !Find.WindowStack.IsOpen<Dialog_ResolutionConfirm>() && !Find.WindowStack.IsOpen<FloatMenu>() &&
            Prefs.UIScalePreference == scale, 5);
        yield return N(new NativePointerScopeEndStep("restore display-options pointer", pointer));
        yield return N(new WindowCancelActionStep("close native Options", typeof(Dialog_Options).FullName!));
    }
    private Vector2 OptionsPoint(MainTabWindow_Menu menu)
    {
        var labels = new List<string>();
        if (!GameDataSaveLoader.SavingIsTemporarilyDisabled && !game.Info.permadeathMode) labels.Add("Save");
        if (Read<bool>(typeof(MainTabWindow_Menu), menu, "anyGameFiles") && !game.Info.permadeathMode) labels.Add("LoadGame");
        labels.Add("ReviewScenario");
        var font = Text.Font;
        try
        {
            Text.Font = GameFont.Small;
            float y = labels.Sum(key => Math.Max(45, Text.CalcHeight(key.Translate(), 170)) + 7);
            return new(18 + 85, 18 + y + Math.Max(45, Text.CalcHeight("Options".Translate(), 170)) / 2);
        }
        finally { Text.Font = font; }
    }
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> receive) where T : Window => N(new NativeWindowCaptureStep(name, captures =>
    {
        RequireGame(); var actual = Find.WindowStack.Windows.OfType<T>().Single(); ownedWindows.Add(actual);
        window = captures.Single(w => w.NativeId == actual.ID && w.RuntimeType == actual.GetType().FullName);
        EndToEndAssert.True(window.Width > 0 && window.Height > 0 && Math.Abs(window.UiScale - Prefs.UIScale) < .001);
        receive(actual); // Observation may include a titled console or partially offscreen manager.
    }));
    private NativeWindowClickActionStep Click(string name, Vector2 point)
    {
        double x = (window.X + point.x) * window.UiScale, y = (window.Y + point.y) * window.UiScale;
        var actual = Find.WindowStack.Windows.Single(w => w.ID == window.NativeId && w.GetType().FullName == window.RuntimeType);
        EndToEndAssert.True(string.IsNullOrEmpty(actual.optionalTitle) && float.IsFinite(point.x) && float.IsFinite(point.y) &&
            point.x >= 18 && point.x < window.Width - 18 && point.y >= 18 && point.y < window.Height - 18 &&
            x >= 0 && x < window.PixelWidth && y >= 0 && y < window.PixelHeight, "Require a visible exact captured native control.");
        return N(new NativeWindowClickActionStep(name, window.Handle, x, y, null, pointer));
    }
    private T N<T>(T step) where T : EndToEndStep { nativeOperations++; return step; }
    private ScreenshotStep Shot(string name) { screenshots++; return new(name, Array.Empty<string>(), 0); }
    private WaitUntilStep Wait(string name, Func<bool> predicate, int seconds)
    {
        waitSeconds += seconds;
        return new(name, _ => predicate(), new EndToEndDeadline(int.MaxValue, 1200, TimeSpan.FromSeconds(seconds)));
    }
    private static T Read<T>(Type type, object? instance, string name)
    {
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            ?? throw new MissingFieldException(type.FullName, name);
        EndToEndAssert.True(field.FieldType == typeof(T) && field.IsStatic == (instance == null));
        return (T)field.GetValue(instance)!;
    }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game));
    private bool WindowFits() => manager.windowRect.x >= 0 && manager.windowRect.y >= 0 &&
        manager.windowRect.xMax <= UI.screenWidth && manager.windowRect.yMax <= UI.screenHeight;
    private void RequireChoice()
    {
        RequireGame(); EndToEndAssert.True(game.drugPolicyDatabase.AllPolicies.Contains(policy) &&
            ReferenceEquals(policy[beer.drug], beer) && ReferenceEquals(NativeDrugToleranceState.Read(beer), retained));
        EndToEndAssert.True(beer.allowedForJoy && beer.allowScheduled);
    }
    private IReadOnlyDictionary<string, string> Evidence() => new Dictionary<string, string>
    {
        ["phase"] = phase, ["scope"] = "native scale and manager layout only; resolution is disposable setup; no routine-use, numeric-edit or full S138 claim",
        ["pixels"] = Screen.width + "x" + Screen.height, ["logicalViewport"] = UI.screenWidth + "x" + UI.screenHeight,
        ["scalePreference"] = Prefs.UIScalePreference.ToString("R"), ["effectiveScale"] = Prefs.UIScale.ToString("R"),
        ["dpi"] = ResolutionUtility.DpiScale.ToString("R"), ["available"] = runtime.Available.ToString(),
        ["status"] = runtime.StatusKey ?? "none", ["manager"] = manager?.windowRect.ToString() ?? "not opened",
        ["managerOpen"] = (manager?.IsOpen == true).ToString(),
        ["choice"] = NativeDrugToleranceState.ReadEncoded(beer) ?? "absent", ["nativeOperations"] = nativeOperations.ToString(),
        ["screenshots"] = screenshots.ToString(), ["waitSeconds"] = waitSeconds.ToString()
    };
}
