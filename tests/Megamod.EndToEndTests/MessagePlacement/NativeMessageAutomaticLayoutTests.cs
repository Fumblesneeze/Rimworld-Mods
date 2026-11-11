using System.Reflection;
using LudeonTK;
using Megamod.Features.MessagePlacement;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.MessagePlacement;

// Pawns/message are disposable preconditions. Native UI scale and selection actions cause the result.
// Budget: 8*30s screenshots + 22*15s native window/pointer/log operations + 330s shared reserve = 900s.
// Escape, float-menu, cancel and selection actions are synchronous within that reserve. Each scale
// confirmation has its own native 10s countdown: capture and click Keep immediately, without a shot.
// Paused UI has no established frame-rate ceiling; launcher allowance must be at least 1500s.
[RimWorldEndToEndTest("megamod.messages.native-automatic-layout", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 4800, MaxWallClockSeconds = 900)]
public sealed class NativeMessageAutomaticLayoutTests : IRimWorldEndToEndTest
{
    // At the isolated 1600px viewport, native 0.95 scale steps put 32 portraits in two rows
    // at 1x but one row at 0.75x. Forty-eight stays on the same two-row scale at both widths.
    private const int FixturePawnCount = 32;
    private readonly List<Pawn> pawns = new();
    private readonly List<Window> ownedWindows = new();
    private SettingsRuntime settings = null!;
    private Message message = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private Rect before, resized;
    private float beforeBottom, resizedBottom;
    private int beforeRows, resizedRows;
    private Dictionary<string, string> beforeLayout = null!;
    private EditWindow_Log? ownedLog;

    public void Arrange(IEndToEndContext context)
    {
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings are unavailable.");
        var map = Find.CurrentMap ?? throw new InvalidOperationException("A disposable loaded map is required.");
        EndToEndAssert.True(Prefs.DevMode && !Find.WindowStack.IsOpen<EditWindow_Log>());
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && Find.Selector.SelectedObjects.Count == 0);
        EndToEndAssert.True(map.mapPawns.FreeColonists.Count == 0, "Use the empty disposable test map.");
        EndToEndAssert.True(settings.Store.UsesGlobal(MessagePlacementSettings.Enabled) &&
            settings.Store.UsesGlobal(MessagePlacementSettings.PositionOffset));
        var placement = ReadNativeField<MessagePlacementRuntime>(typeof(MegamodMod), "messagePlacement", LoadedModManager.GetMod<MegamodMod>());
        EndToEndAssert.True(placement?.Available == true && placement.AutomaticClearanceIntegrated,
            "This workflow requires the reviewed, admitted native automatic path.");
        var globals = settings.Store.ExportGlobals();
        var speed = Find.TickManager.CurTimeSpeed;
        float scale = Prefs.UIScalePreference;
        bool showBar = Find.PlaySettings.showColonistBar;
        var weapons = Prefs.ShowWeaponsUnderPortraitMode;
        bool mood = Prefs.VisibleMood;
        var live = ReadNativeField<List<Message>>(typeof(Messages), "liveMessages", null);
        EndToEndAssert.Equal(0, live.Count, "The fixture requires an otherwise empty notification stack.");
        context.DeferCleanup(() =>
        {
            var failures = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { failures.Add(error); } }
            foreach (var owned in ownedWindows.Distinct()) Attempt(() => { if (Find.WindowStack.Windows.Contains(owned)) owned.Close(); });
            Attempt(() => { if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(); });
            Attempt(() => Find.Selector.ClearSelection());
            Attempt(() => live.Remove(message));
            foreach (var pawn in pawns) Attempt(() => { if (!pawn.Destroyed) pawn.Destroy(DestroyMode.Vanish); });
            Attempt(() => settings.Store.LoadGlobals(globals));
            Attempt(() => Prefs.ShowWeaponsUnderPortraitMode = weapons);
            Attempt(() => Prefs.VisibleMood = mood);
            Attempt(() => Find.PlaySettings.showColonistBar = showBar);
            // Cleanup restores the precondition without creating an unowned resolution-confirm dialog.
            Attempt(() => { Prefs.UIScalePreference = scale; GenUI.ClearLabelWidthCache(); });
            Attempt(() => Find.ColonistBar.MarkColonistsDirty());
            Attempt(() => Find.TickManager.CurTimeSpeed = speed);
            if (failures.Count != 0) throw new AggregateException("Automatic message fixture cleanup failed.", failures);
        });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        Prefs.UIScalePreference = 1; // Disposable baseline; both claimed transitions below use native UI.
        GenUI.ClearLabelWidthCache();
        Find.PlaySettings.showColonistBar = true;
        Prefs.VisibleMood = false; // This preference is fixture setup, never the claimed player action.
        EndToEndAssert.True(settings.Store.SetGlobal(MessagePlacementSettings.Enabled, true).Succeeded);
        EndToEndAssert.True(settings.Store.SetGlobal(MessagePlacementSettings.PositionOffset, default).Succeeded);
        var cells = CellRect.CenteredOn(map.Center, 16).Where(cell => cell.InBounds(map) && cell.Walkable(map)).Take(FixturePawnCount).ToArray();
        EndToEndAssert.Equal(FixturePawnCount, cells.Length);
        for (int index = 0; index < cells.Length; index++)
        {
            var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            pawns.Add(pawn);
            pawn.Name = new NameTriple("Clearance", "Clearance " + (index + 1), "Fixture");
            pawn.equipment.DestroyAllEquipment();
            if (pawn.needs?.mood != null) pawn.needs.mood.CurLevel = 1;
            EndToEndAssert.True(MoodThresholdExtensions.CurrentMoodThresholdFor(pawn).EdgeExpansion() == 0,
                "Disposable fixture pawns must begin in the supported zero-edge presentation.");
            GenSpawn.Spawn(pawn, cells[index], map);
        }
        Find.ColonistBar.MarkColonistsDirty();
        message = new Message("Automatic message clearance verification", MessageTypeDefOf.NeutralEvent, new LookTargets(TargetInfo.Invalid));
        RequiredNativeField(typeof(Message), "startingTime", typeof(float), false).SetValue(message, RealTime.LastRealTime + 1200f);
        live.Add(message);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return Shot("current native portraits and message before changing UI scale");
        yield return new CheckpointStep("retain complete native baseline before assertions", _ => beforeLayout = LayoutEvidence());
        yield return new AssertionStep("retain actual native baseline layout and clear message", _ =>
        {
            before = ObserveClearance(out beforeBottom, out beforeRows);
            EndToEndAssert.Equal(2, beforeRows, "The 32-pawn baseline must actually display two rows; see the retained native layout.");
        });
        foreach (var step in ChangeScale(0.75f)) yield return step;
        yield return Shot("native bar layout and message after player selected 0.75x UI scale");
        yield return new CheckpointStep("retain complete before and after native layouts before causal assertions", _ => ComparedLayoutEvidence());
        yield return new AssertionStep("native UI scale changed current layout and causal message clearance", _ =>
        {
            AssertScale(0.75f);
            resized = ObserveClearance(out resizedBottom, out resizedRows);
            EndToEndAssert.Equal(1, resizedRows, "Native 0.75x must put these same 32 pawns on one logical row; see both retained layouts.");
            EndToEndAssert.True(Math.Abs(resizedBottom - beforeBottom) > 1,
                "This fixture must exercise a real current bar extent change, not merely a scale setting.");
            EndToEndAssert.True(Math.Abs(resized.y - before.y) > 1, "The same message must follow the changed current bar extent.");
        });
        yield return new SelectionActionStep("select one native colonist and expose the pending selection-outline path", new[] { pawns[0].ThingID }, false);
        yield return Shot("selected native colonist with explicit manual-only placement fallback");
        yield return new AssertionStep("selected adornment is not silently guessed as automatic geometry", _ =>
        {
            EndToEndAssert.True(Find.Selector.SelectedObjects.Count == 1 && Find.Selector.SelectedObjects.Contains(pawns[0]),
                "The native selector must contain only the requested fixture colonist.");
            AssertScale(0.75f);
            EndToEndAssert.True(Messages.IsLive(message) && !message.Expired && message.lastDrawRect.height > 0,
                "The same live message must still be drawn during the selection fallback.");
            float expected = NativeMessageBaselineY();
            EndToEndAssert.True(Math.Abs(message.lastDrawRect.y - expected) < 0.001f,
                "Selected adornment must use native manual-only baseline including lesson offset and 2-unit padding: actual=" +
                RectangleEvidence(message.lastDrawRect) + ", expectedY=" + expected.ToString("R"));
        });
        yield return new SelectionActionStep("deselect through the native selector and restore supported automatic geometry", Array.Empty<string>(), false);
        yield return Shot("automatic message clearance returns after native deselection");
        yield return new AssertionStep("fresh current geometry resumes after native deselection", context =>
        {
            EndToEndAssert.Equal(0, Find.Selector.SelectedObjects.Count, "The native deselection must clear the selector.");
            var restored = ObserveClearance(out _, out _);
            EndToEndAssert.True(Math.Abs(restored.y - resized.y) < 2,
                "Deselecting must restore the preceding automatic message y: actual=" + RectangleEvidence(restored) +
                ", preceding=" + RectangleEvidence(resized));
        });
        yield return new CheckpointStep("retain layout-action-clearance evidence", _ => new Dictionary<string, string>
        {
            ["beforeMessage"] = before.ToString(), ["resizedMessage"] = resized.ToString(),
            ["beforePortraitBottom"] = beforeBottom.ToString(), ["resizedPortraitBottom"] = resizedBottom.ToString(),
            ["beforeRows"] = beforeRows.ToString(), ["resizedRows"] = resizedRows.ToString(),
            ["scope"] = "current unarmed portraits/labels; expanded mood, selected overlays, weapons, frames, world/S001 remain pending"
        });
        foreach (var step in ChangeScale(1)) yield return step;
        yield return Shot("original current native bar extent after restoring UI scale through Options");
        yield return new CheckpointStep("retain original and restored native layouts before restoration assertions", _ => ComparedLayoutEvidence());
        yield return new AssertionStep("second native scale transition restores original geometry and clearance", context =>
        {
            AssertScale(1);
            var restored = ObserveClearance(out float bottom, out int rows);
            EndToEndAssert.Equal(beforeRows, rows, "Returning to 1x must restore the original portrait-row count.");
            EndToEndAssert.True(Math.Abs(bottom - beforeBottom) < 0.001f && Math.Abs(restored.y - before.y) < 2,
                "Returning to 1x must restore both current portrait extent and message clearance: portraitBottom=" +
                bottom.ToString("R") + ", originalBottom=" + beforeBottom.ToString("R") + ", message=" +
                RectangleEvidence(restored) + ", originalMessage=" + RectangleEvidence(before));
        });
        yield return new NativeDeveloperLogActionStep("open native developer console after automatic layout actions", true);
        yield return new AssertionStep("retain scenario-opened native console", _ =>
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Native console did not open."));
        yield return Shot("native console after current-layout and selection actions");
        yield return new NativeDeveloperLogActionStep("close native developer console after evidence", false);
    }

    private IEnumerable<EndToEndStep> ChangeScale(float scale)
    {
        string choiceLabel = null!;
        yield return new AssertionStep("require an isolated native scale transition", _ =>
        {
            EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_Options>() &&
                !Find.WindowStack.IsOpen<FloatMenu>() && !Find.WindowStack.IsOpen<Dialog_ResolutionConfirm>(),
                "A scale transition must start without a menu, Options or resolution-confirmation window.");
            var scales = ReadNativeField<float[]>(typeof(Dialog_Options), "UIScales", null);
            EndToEndAssert.Equal(1, scales.Count(value => value == scale), "The requested scale must occur once in native UIScales.");
            EndToEndAssert.True(scale <= 1 && Prefs.UIScalePreference != scale,
                "This fixture exercises distinct native 0.75x/1x transitions, which bypass the >1 resolution rejection.");
            choiceLabel = scales.Single(value => value == scale) + "x"; // Native DoUIOptions uses current culture.
        });
        yield return new EscapeMenuActionStep("open Escape menu for ordinary UI scale options", true);
        MainTabWindow_Menu menu = null!;
        yield return Capture<MainTabWindow_Menu>("capture native Escape menu", value => menu = value);
        yield return new NativePointerScopeBeginStep("retain pointer during native scale choice", value => pointer = value);
        Rect options = default;
        yield return new AssertionStep("measure native Options button", _ => options = OptionsButton(menu));
        yield return Click("open Options through its native button", options.center);
        Dialog_Options dialog = null!;
        yield return Capture<Dialog_Options>("capture native Options", value => dialog = value);
        Vector2 category = default;
        yield return new AssertionStep("measure native Interface category from visible category order", _ =>
        {
            EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<MainTabWindow_Menu>(),
                "The native Options command must have closed the Escape menu.");
            bool hasModSettings = ReadNativeField<bool>(typeof(Dialog_Options), "hasModSettings", dialog);
            var categories = DefDatabase<OptionCategoryDef>.AllDefs.Where(item => (Prefs.DevMode || !item.isDev) &&
                item.modContentPack.IsOfficialMod && (item != OptionCategoryDefOf.Mods || hasModSettings)).ToList();
            int index = categories.IndexOf(OptionCategoryDefOf.Interface);
            EndToEndAssert.True(index >= 0, "Interface must be present in the native category list.");
            category = new(80, index * 50 + 24);
        });
        yield return Click("choose the ordinary Interface category", category);
        // Observe a rendered Interface frame before reading its retained listing height/scrollbar.
        yield return Shot("native Interface options before changing UI scale");
        yield return Capture<Dialog_Options>("recapture Interface options", value => dialog = value);
        Vector2 scaleButton = default;
        yield return new AssertionStep("measure first Interface option using current scroll and viewport", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(ReadNativeField<OptionCategoryDef>(typeof(Dialog_Options), "selectedCategory", dialog),
                OptionCategoryDefOf.Interface) && dialog.selectedMod == null, "The native category click must select Interface.");
            float height = ReadNativeField<float>(typeof(Dialog_Options), "optionsViewRectHeight", dialog);
            var scroll = ReadNativeField<Vector2>(typeof(Dialog_Options), "optionsScrollPosition", dialog);
            EndToEndAssert.True(Finite(height) && height > 0 && Finite(scroll.x) && Finite(scroll.y),
                "The rendered native Interface listing must have finite positive height and finite scroll position.");
            float availableWidth = checked((float)(window.Width - 36 - 177 - (height > window.Height - 36 - 60 ? 26 : 0)));
            scaleButton = new(177 + availableWidth * 0.8f - scroll.x, 12 + 15 - scroll.y);
            var viewport = new Rect(177, 0, checked((float)window.Width - 36 - 177), checked((float)window.Height - 36 - 60));
            EndToEndAssert.True(availableWidth > 0 && viewport.Contains(scaleButton),
                "The first native Interface button must be visible in its current scroll viewport: point=" + scaleButton +
                ", viewport=" + RectangleEvidence(viewport) + ", height=" + height.ToString("R") + ", scroll=" + scroll);
        });
        yield return Click("open native UI scale choices", scaleButton);
        yield return new AssertionStep("retain and validate the actual native scale menu", _ =>
        {
            var menus = Find.WindowStack.Windows.OfType<FloatMenu>().ToArray();
            EndToEndAssert.Equal(1, menus.Length, "Only the scale menu produced by this native click may be present.");
            ownedWindows.Add(menus[0]);
            var menuOptions = ReadNativeField<List<FloatMenuOption>>(typeof(FloatMenu), "options", menus[0]);
            var scales = ReadNativeField<float[]>(typeof(Dialog_Options), "UIScales", null);
            EndToEndAssert.True(menuOptions.Count == scales.Length && scales.All(value => menuOptions.Count(option => option.Label == value + "x") == 1),
                "The visible menu labels must match native UIScales in the current culture: " + string.Join("; ", menuOptions.Select(option => option.Label)));
            EndToEndAssert.Equal(1, menuOptions.Count(option => option.Label == choiceLabel && !option.Disabled),
                "The exact native scale choice must be present and enabled: " + choiceLabel);
        });
        yield return new CurrentFloatMenuActionStep("choose native " + choiceLabel + " UI scale", choiceLabel, captureSoleUnownedMenu: true);
        Vector2 keep = default;
        yield return Capture<Dialog_ResolutionConfirm>("capture native ten-second scale confirmation immediately", _ =>
            keep = new(checked(((float)window.Width - 36) / 4 - 10), checked((float)window.Height - 36 - 17.5f)));
        yield return Click("keep the changed scale through its native confirmation button", keep);
        yield return new AssertionStep("native Keep accepted the scale without a pending menu or confirmation", _ =>
        {
            AssertScale(scale);
            EndToEndAssert.True(!Find.WindowStack.IsOpen<FloatMenu>() && !Find.WindowStack.IsOpen<Dialog_ResolutionConfirm>() &&
                Find.WindowStack.Windows.Contains(dialog), "Native Keep must close only the confirmation and leave Options open.");
        });
        yield return new NativePointerScopeEndStep("restore pointer after scale choice", pointer);
        yield return new WindowCancelActionStep("close Options through native cancel", typeof(Dialog_Options).FullName!);
        yield return new AssertionStep("native cancel returned to the ordinary play view", _ =>
        {
            AssertScale(scale);
            EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_Options>() &&
                !Find.WindowStack.IsOpen<MainTabWindow_Menu>() && !Find.WindowStack.IsOpen<Dialog_ResolutionConfirm>(),
                "Options cancel must leave no scale workflow window or main tab open.");
        });
    }

    private NativeWindowCaptureStep Capture<T>(string label, Action<T> retain) where T : Window => new(label, windows =>
    {
        var actuals = Find.WindowStack.Windows.OfType<T>().ToArray();
        EndToEndAssert.Equal(1, actuals.Length, "Expected one native " + typeof(T).FullName + " window.");
        var actual = actuals[0];
        ownedWindows.Add(actual);
        var captures = windows.Where(value => value.RuntimeType == typeof(T).FullName && value.NativeId == actual.ID).ToArray();
        EndToEndAssert.Equal(1, captures.Length, "Expected the same native window ID in the fresh capture.");
        window = captures[0];
        EndToEndAssert.True(string.IsNullOrEmpty(actual.optionalTitle) && window.Width > 36 && window.Height > 36 &&
            window.UiScale > 0 && Math.Abs(window.UiScale - Prefs.UIScale) < 0.001,
            "This native window must use an untitled 18-unit margin and the current GUI scale.");
        retain(actual);
    });
    private NativeWindowClickActionStep Click(string label, Vector2 point)
    {
        double x = (window.X + 18 + point.x) * window.UiScale;
        double y = (window.Y + 18 + point.y) * window.UiScale;
        EndToEndAssert.True(Finite(point.x) && Finite(point.y) && point.x >= 0 && point.x < window.Width - 36 &&
            point.y >= 0 && point.y < window.Height - 36 && x >= 0 && x < window.PixelWidth && y >= 0 && y < window.PixelHeight,
            "Native button center must remain within captured content and rendered pixels: " + label + ", point=" + point +
            ", window=" + window.RuntimeType + ", bounds=" + window.Width + "x" + window.Height + ", scale=" + window.UiScale);
        return new(label, window.Handle, x, y, null, pointer);
    }
    private Rect ObserveClearance(out float bottom, out int rows)
    {
        EndToEndAssert.True(Messages.IsLive(message), "The fixture notification must remain in the native live-message stack.");
        EndToEndAssert.True(!message.Expired, "The pinned fixture notification expired before layout observation.");
        EndToEndAssert.True(message.lastDrawRect.height > 0,
            "The native message has no positive drawn height: " + RectangleEvidence(message.lastDrawRect));
        var bar = Find.ColonistBar;
        EndToEndAssert.Equal(pawns.Count, bar.Entries.Count, "The current bar must contain every disposable fixture colonist.");
        rows = bar.DrawLocs.Select(point => point.y).Distinct().Count();
        bottom = bar.DrawLocs.Max(point => point.y) + bar.Size.y;
        EndToEndAssert.True(message.lastDrawRect.y >= bottom + 4,
            "Native message must clear current portrait rows and labels: message=" + RectangleEvidence(message.lastDrawRect) +
            ", portraitBottom=" + bottom.ToString("R") + ", rows=" + rows + ", uiScale=" + Prefs.UIScale.ToString("R"));
        int nativeId = ReadNativeField<int>(typeof(Message), "ID", message);
        int windowId = -Math.Abs(Gen.HashCombineInt(nativeId, 45574281));
        var immediate = Find.WindowStack.Windows.OfType<ImmediateWindow>().ToArray();
        var matching = immediate.Where(value => value.ID == windowId).ToArray();
        EndToEndAssert.Equal(1, matching.Length,
            "The live message must retain its exact native immediate window ID=" + windowId + "; present=" +
            string.Join("; ", immediate.Select(value => value.ID + ":" + RectangleEvidence(value.windowRect))));
        // Native WindowOnGUI rounds its screen rectangle immediately before GUI.Window; Message keeps
        // the unrounded CalculateRect value. Observe that native projection, never an epsilon guess.
        Rect expectedWindow = message.lastDrawRect.Rounded();
        EndToEndAssert.True(matching[0].windowRect == expectedWindow,
            "The exact message window must use native rounded geometry: actual=" + RectangleEvidence(matching[0].windowRect) +
            ", expected=" + RectangleEvidence(expectedWindow) + ", message=" + RectangleEvidence(message.lastDrawRect));
        return message.lastDrawRect;
    }
    private Dictionary<string, string> ComparedLayoutEvidence()
    {
        var evidence = beforeLayout.ToDictionary(pair => "before." + pair.Key, pair => pair.Value);
        foreach (var pair in LayoutEvidence()) evidence.Add("after." + pair.Key, pair.Value);
        return evidence;
    }
    private Dictionary<string, string> LayoutEvidence()
    {
        var bar = Find.ColonistBar;
        var entries = bar.Entries.ToArray();
        var locations = bar.DrawLocs.ToArray();
        Vector2 size = bar.Size;
        Rect extent = locations.Length == 0 ? default : Rect.MinMaxRect(locations.Min(point => point.x),
            locations.Min(point => point.y), locations.Max(point => point.x) + size.x, locations.Max(point => point.y) + size.y);
        return new Dictionary<string, string>
        {
            ["entryCount"] = entries.Length.ToString(), ["locationCount"] = locations.Length.ToString(),
            ["entryIdentities"] = string.Join("; ", entries.Select(entry => entry.group + ":" + entry.pawn?.ThingID)),
            ["rows"] = locations.Select(point => point.y).Distinct().Count().ToString(),
            ["rowCounts"] = string.Join("; ", locations.GroupBy(point => point.y).Select(row => row.Key.ToString("R") + ":" + row.Count())),
            ["barScale"] = bar.Scale.ToString("R"), ["portraitSize"] = $"({size.x:R}, {size.y:R})",
            ["portraitExtent"] = RectangleEvidence(extent), ["portraitBottom"] = extent.yMax.ToString("R"),
            ["drawLocations"] = string.Join("; ", locations.Select((point, index) => $"{index}=({point.x:R}, {point.y:R})")),
            ["message"] = RectangleEvidence(message.lastDrawRect),
            ["uiScalePreference"] = Prefs.UIScalePreference.ToString("R"), ["effectiveUiScale"] = Prefs.UIScale.ToString("R"),
            ["logicalViewport"] = UI.screenWidth + "x" + UI.screenHeight, ["pixelViewport"] = Screen.width + "x" + Screen.height,
            ["nativeMessageBaselineY"] = NativeMessageBaselineY().ToString("R"), ["frame"] = Time.frameCount.ToString()
        };
    }
    private static string RectangleEvidence(Rect rect) => $"({rect.x:R}, {rect.y:R}, {rect.width:R}, {rect.height:R})";
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static void AssertScale(float expected) => EndToEndAssert.True(Math.Abs(Prefs.UIScalePreference - expected) < 0.001f,
        "Native UI scale preference mismatch: expected=" + expected.ToString("R") + ", actual=" + Prefs.UIScalePreference.ToString("R"));
    private static float NativeMessageBaselineY()
    {
        int y = (int)Messages.MessagesTopLeftStandard.y;
        if (Current.Game != null && Find.ActiveLesson.ActiveLessonVisible) y += (int)Find.ActiveLesson.Current.MessagesYOffset;
        return y - 2; // Message.CalculateRect expands its actual drawn rectangle vertically by 2.
    }
    private static FieldInfo RequiredNativeField(Type owner, string name, Type valueType, bool isStatic)
    {
        for (Type? type = owner; type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (field == null) continue;
            if (field.FieldType != valueType || field.IsStatic != isStatic)
                throw new InvalidOperationException("Native field contract changed: " + owner.FullName + "." + name +
                    " expected " + (isStatic ? "static " : "instance ") + valueType.FullName + "; actual " +
                    (field.IsStatic ? "static " : "instance ") + field.FieldType.FullName + ".");
            return field;
        }
        throw new MissingFieldException("Required native field absent (including inherited public/nonpublic fields): " + owner.FullName + "." + name);
    }
    private static T ReadNativeField<T>(Type owner, string name, object? instance)
    {
        var value = RequiredNativeField(owner, name, typeof(T), instance == null).GetValue(instance);
        return value is T typed ? typed : throw new InvalidOperationException("Required native field is null or has an unexpected value: " + owner.FullName + "." + name);
    }
    private Rect OptionsButton(MainTabWindow_Menu menu)
    {
        EndToEndAssert.True(string.IsNullOrEmpty(menu.optionalTitle));
        bool saved = ReadNativeField<bool>(typeof(MainTabWindow_Menu), "anyGameFiles", menu);
        var labels = new List<string>();
        if (!GameDataSaveLoader.SavingIsTemporarilyDisabled && !Current.Game.Info.permadeathMode) labels.Add("Save");
        if (saved && !Current.Game.Info.permadeathMode) labels.Add("LoadGame");
        labels.Add("ReviewScenario");
        var font = Text.Font;
        try
        {
            Text.Font = GameFont.Small;
            float y = labels.Sum(label => Math.Max(45, Text.CalcHeight(label.Translate(), 170)) + 7);
            return new(0, y, 170, Math.Max(45, Text.CalcHeight("Options".Translate(), 170)));
        }
        finally { Text.Font = font; }
    }
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
}
