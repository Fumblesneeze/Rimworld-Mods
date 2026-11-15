using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using HarmonyLib;
using LudeonTK;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

// XML Extensions emits both Mod objects from actual loaded fixture Defs at its normal startup.
// The addon requests catalog reads only. Every page open/edit/close remains the native UI path.
[RimWorldEndToEndTest("megamod.settings-search.native-catalog", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "imranfish.xmlextensions", "fumblesneeze.megamod",
    "fumblesneeze.megamodsettingscatalogfixture",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 500)]
public sealed class NativeSettingsCatalogTests : IRimWorldEndToEndTest
{
    private const string FixturePackage = "fumblesneeze.megamodsettingscatalogfixture";
    private const string Owner = "fumblesneeze.megamod.e2e.settings-catalog";
    private const string Title = "Catalog twins";
    private const string ProviderWindowName = "XmlExtensions.ModSettingsWindow";
    private static readonly string[] Names = { "amber", "cobalt" };
    private static NativeSettingsCatalogTests? active;
    private readonly List<Window> owned = new();
    private readonly Dictionary<string, Mod> mods = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> baseline = new(StringComparer.Ordinal);
    private readonly SettingsNativeCatalog catalog = new();
    private readonly SettingsPageRecorder chooser = new(new("fixture-chooser", "fixture-chooser", null, null, null), "ordinary");
    private readonly SettingsPageRecorder provider = new(new("fixture-provider", "fixture-provider", null, null, null), "ordinary");
    private SettingsNeutralReader reader = null!;
    private NativeSettingsCatalogProbes? probes;
    private MethodInfo getSetting = null!, setSetting = null!, drawMenu = null!, drawChooser = null!;
    private FieldInfo cachedPages = null!, activeMenu = null!;
    private Mod providerMod = null!;
    private Game game = null!;
    private Dialog_Options options = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private string? failure, lastReadReason;
    private SettingsNeutralReadStatus lastReadStatus;
    private int reads;
    private bool consoleRequested, staleInvalidated;
    private SettingsPage[] priorPages = Array.Empty<SettingsPage>();

    public void Arrange(IEndToEndContext context)
    {
        game = Current.Game;
        EndToEndAssert.True(active == null && game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            Find.MainTabsRoot.OpenTab == null &&
            LanguageDatabase.activeLanguage.folderName == "English" &&
            !Find.WindowStack.Windows.Any(w => w is Dialog_Options or Dialog_ModSettings or MainTabWindow_Menu or EditWindow_Log ||
                w.GetType().FullName == ProviderWindowName));
        var providerType = RequiredType("XmlExtensions.XmlMod");
        providerMod = LoadedModManager.ModHandles.Single(m => m.GetType() == providerType);
        getSetting = RequiredMethod(RequiredType("XmlExtensions.SettingsManager"), "GetSetting", true,
            typeof(string), typeof(string));
        setSetting = RequiredMethod(RequiredType("XmlExtensions.SettingsManager"), "SetSetting", true,
            typeof(string), typeof(string), typeof(string));
        drawMenu = RequiredMethod(RequiredType("XmlExtensions.SettingsMenuDef"), "DrawSettingsMenu", false, typeof(Rect));
        drawChooser = RequiredMethod(typeof(Dialog_Options), "DoModOptions", false, typeof(Listing_Standard));
        cachedPages = AccessTools.Field(typeof(Dialog_Options), "cachedModsWithSettings")
            ?? throw new MissingFieldException(typeof(Dialog_Options).FullName, "cachedModsWithSettings");
        EndToEndAssert.Equal(typeof(IEnumerable<Mod>), cachedPages.FieldType);
        activeMenu = AccessTools.Field(RequiredType("XmlExtensions.BaseSettingsWindow"), "activeMenu")
            ?? throw new MissingFieldException("XmlExtensions.BaseSettingsWindow", "activeMenu");
        foreach (string name in Names)
        {
            string defName = "MegamodCatalog" + char.ToUpperInvariant(name[0]) + name[1..];
            var matches = LoadedModManager.ModHandles.Where(m => m.Content?.PackageId == FixturePackage &&
                (m.GetType().GetField("settingsMenuDef", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as Def)?.defName == defName).ToArray();
            EndToEndAssert.Equal(1, matches.Length, "The actual XML Extensions startup must emit the " + name + " fixture page.");
            mods.Add(name, matches[0]);
            baseline.Add(name, GetValue(name));
            EndToEndAssert.Equal(false, bool.Parse(baseline[name]), "Disposable fixture options must start false.");
        }
        EndToEndAssert.True(!ReferenceEquals(mods["amber"], mods["cobalt"]) &&
            ReferenceEquals(mods["amber"].Content, mods["cobalt"].Content));
        reader = new(message => failure ??= message);
        var patches = new Harmony(Owner);
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() =>
        {
            active = null;
            var failures = new List<Exception>();
            void Restore(Action action) { try { action(); } catch (Exception error) { failures.Add(error); } }
            Restore(catalog.Clear);
            Restore(() => probes?.Clear());
            Restore(reader.Dispose);
            Restore(() => patches.UnpatchAll(Owner));
            foreach (var pair in baseline)
                Restore(() => setSetting.Invoke(null, new object[] { ModId(pair.Key), "enabled", pair.Value }));
            if (consoleRequested) owned.AddRange(Find.WindowStack.Windows.OfType<EditWindow_Log>());
            // A provider constructor replaces the native Dialog_ModSettings while opening its own
            // window. Include a still-open matching window if an interrupted capture never retained it.
            owned.AddRange(Find.WindowStack.Windows.Where(w => w is Dialog_Options or Dialog_ModSettings or MainTabWindow_Menu ||
                w.GetType().FullName == ProviderWindowName));
            Restore(() =>
            {
                if (Find.MainTabsRoot.OpenTab?.TabWindow is MainTabWindow_Menu)
                    Find.MainTabsRoot.EscapeCurrentTab(false);
            });
            foreach (var item in owned.Distinct().ToArray())
                Restore(() => { if (Find.WindowStack.Windows.Contains(item)) item.Close(false); });
            Restore(providerMod.WriteSettings);
            Restore(() => Find.TickManager.CurTimeSpeed = speed);
            if (failures.Count != 0) throw new AggregateException("Catalog fixture cleanup failed.", failures);
        });
        patches.Patch(RequiredMethod(typeof(Dialog_Options), "DoOptions", false, typeof(OptionCategoryDef), typeof(Rect)),
            transpiler: new HarmonyMethod(typeof(NativeSettingsCatalogTests), nameof(SurroundChooser)));
        patches.Patch(RequiredMethod(typeof(Dialog_Options), nameof(Dialog_Options.DoWindowContents), false, typeof(Rect)),
            postfix: new HarmonyMethod(typeof(NativeSettingsCatalogTests), nameof(DrawAddon)));
        patches.Patch(RequiredMethod(RequiredType(ProviderWindowName), "DoWindowContents", false, typeof(Rect)),
            transpiler: new HarmonyMethod(typeof(NativeSettingsCatalogTests), nameof(SurroundProvider)));
        active = this;
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own native catalog pointer", value => pointer = value);
        foreach (var step in OpenOptions()) yield return step;
        yield return Shot("native Mod options before catalog capture");
        yield return Click("request completed native page catalog", new Vector2(100, (float)window.Height - 35));
        yield return Wait("complete native catalog request", () => reads == 1 || failure != null);
        yield return new CheckpointStep("catalog retains both genuine same-title provider pages", _ => VerifyCatalog());
        yield return Shot("captured catalog retains two independent provider pages");

        yield return new AssertionStep("schedule bounded isolated catalog diagnostics", stepContext =>
        {
            var expected = catalog.Pages.Select(page =>
            {
                EndToEndAssert.True(catalog.TryResolve(page, out var native));
                return native!;
            }).ToArray();
            probes = new(expected, mods["amber"], mods["cobalt"]);
        });
        yield return Wait("complete isolated startup, enumeration and publication diagnostics", () => probes!.Complete || failure != null);
        yield return new CheckpointStep("retain bounded catalog diagnostic results", stepContext =>
        {
            EndToEndAssert.True(failure == null, failure ?? "");
            EndToEndAssert.True(probes!.Complete);
            AssertValues(false, false);
            return probes.Evidence;
        });

        // Both original cards are already visible. Opening them directly keeps this catalog
        // workflow independent of the chooser's separate text-search/input lifecycle.
        yield return Wait("observe both original native cards", () => ChooserLabels().Length == 2);
        yield return Shot("original chooser exposes both same-title cards");

        foreach (string name in Names)
        {
            foreach (var step in OpenProvider(name)) yield return step;
            yield return Shot("original " + name + " provider option before editing");
            yield return ClickProviderOption("edit the original " + name + " checkbox", name);
            yield return new AssertionStep("only the intended provider option changed", _ =>
                AssertValues(true, name == "cobalt"));
            yield return Shot("native " + name + " provider checkbox changed");
            foreach (var step in CloseProvider()) yield return step;
            yield return new CheckpointStep("native close persisted " + name + " option", _ => PersistedValues());
            foreach (var step in OpenProvider(name)) yield return step;
            yield return new AssertionStep("native reopen retains " + name + " value", _ =>
                EndToEndAssert.Equal(true, bool.Parse(GetValue(name))));
            yield return Shot("native reopen retains " + name + " checkbox");
            foreach (var step in CloseProvider()) yield return step;
        }
        yield return new AssertionStep("independent native edits retained both options", _ => AssertValues(true, true));
        foreach (string name in Names)
        {
            foreach (var step in OpenProvider(name)) yield return step;
            yield return ClickProviderOption("restore original " + name + " checkbox", name);
            yield return new AssertionStep("native edit restores " + name + " default", _ =>
                EndToEndAssert.Equal(false, bool.Parse(GetValue(name))));
            foreach (var step in CloseProvider()) yield return step;
        }
        yield return new CheckpointStep("ordinary close persisted both restored defaults", _ =>
        { AssertValues(false, false); return PersistedValues(); });

        priorPages = catalog.Pages.ToArray();
        yield return new WindowCancelActionStep("close the original native Options", typeof(Dialog_Options).FullName!);
        foreach (var step in OpenOptions()) yield return step;
        yield return Click("refresh from a newly opened native Options collection", new Vector2(100, (float)window.Height - 35));
        yield return Wait("complete the replacement catalog generation", () => reads == 2 || failure != null);
        yield return new CheckpointStep("refresh invalidates stale descriptors and retains exact new identities", stepContext =>
        {
            EndToEndAssert.True(staleInvalidated, "Refresh must invalidate every old descriptor before its pending callback.");
            foreach (var page in priorPages) EndToEndAssert.True(!catalog.TryResolve(page, out _));
            return VerifyCatalog();
        });
        yield return Shot("refreshed catalog and unchanged native access");
        yield return new AssertionStep("clear invalidates the retained catalog", stepContext =>
        {
            var pages = catalog.Pages.ToArray(); catalog.Clear();
            EndToEndAssert.Equal(0, catalog.Pages.Count);
            EndToEndAssert.True(!catalog.Pending);
            foreach (var page in pages) EndToEndAssert.True(!catalog.TryResolve(page, out _));
        });
        yield return new WindowCancelActionStep("close refreshed native Options", typeof(Dialog_Options).FullName!);
        yield return new NativePointerScopeEndStep("restore native catalog pointer", pointer);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open catalog workflow native console", true);
        yield return Shot("native console after catalog and provider workflow");
        yield return new NativeDeveloperLogActionStep("close catalog workflow native console", false);
    }

    private IEnumerable<EndToEndStep> OpenOptions()
    {
        yield return new EscapeMenuActionStep("open the native Escape menu", true);
        MainTabWindow_Menu menu = null!;
        yield return Capture<MainTabWindow_Menu>("capture actual Escape menu", actual => menu = actual);
        yield return Click("open Options through the native menu button", OptionsPoint(menu));
        yield return CaptureOptions();
        Vector2 category = default;
        yield return new AssertionStep("locate the actual Mod options category", _ =>
        {
            EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null &&
                ReadBooleanField(typeof(Dialog_Options), "hasModSettings", options));
            var categories = DefDatabase<OptionCategoryDef>.AllDefs.Where(d => (Prefs.DevMode || !d.isDev) && d.modContentPack.IsOfficialMod).ToList();
            int index = categories.IndexOf(OptionCategoryDefOf.Mods);
            EndToEndAssert.True(index >= 0);
            category = new(18 + 80, 18 + index * 50 + 24);
        });
        yield return Click("choose native Mod options", category);
        yield return Wait("observe the native Mod options category", () => options.selectedCategory == OptionCategoryDefOf.Mods);
        yield return CaptureOptions();
    }

    private IEnumerable<EndToEndStep> OpenProvider(string name)
    {
        yield return CaptureOptions();
        yield return Wait("observe both native cards before opening " + name, () => ChooserLabels().Length == 2);
        yield return ClickProviderCard(name);
        yield return Wait("observe the provider's own " + name + " settings window", () =>
            Find.WindowStack.Windows.Any(w => w.GetType().FullName == ProviderWindowName) &&
            (activeMenu.GetValue(null) as Def)?.defName == "MegamodCatalog" + char.ToUpperInvariant(name[0]) + name[1..]);
        yield return new NativeWindowCaptureStep("capture the actual provider settings window", captured =>
        {
            var actual = Find.WindowStack.Windows.Single(w => w.GetType().FullName == ProviderWindowName);
            RetainWindow(actual, captured);
            EndToEndAssert.True(!Find.WindowStack.IsOpen<Dialog_ModSettings>(), "Provider must replace the native wrapper through its own constructor.");
        });
        yield return Wait("observe original " + name + " option text", () => ProviderLabel(name) != null);
    }

    private IEnumerable<EndToEndStep> CloseProvider()
    {
        yield return new WindowCancelActionStep("close original provider settings through native cancel", ProviderWindowName);
        yield return Wait("observe provider close returned to original Options", () =>
            Find.WindowStack.Windows.Contains(options) && !Find.WindowStack.Windows.Any(w => w.GetType().FullName == ProviderWindowName));
        yield return CaptureOptions();
    }

    private NativeWindowCaptureStep CaptureOptions() => Capture<Dialog_Options>("capture actual native Options", value => options = value);
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> receive) where T : Window => new(name, captured =>
    {
        RequireGame(); var actual = Find.WindowStack.Windows.OfType<T>().Single();
        RetainWindow(actual, captured); receive(actual);
    });
    private void RetainWindow(Window actual, IReadOnlyList<EndToEndNativeWindow> captured)
    {
        if (!owned.Contains(actual)) owned.Add(actual);
        window = captured.Single(w => w.NativeId == actual.ID && w.RuntimeType == actual.GetType().FullName);
    }
    private NativeWindowClickActionStep Click(string name, Vector2 point) => new(name, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointer);
    private NativeWindowClickActionStep ClickObserved(string name, SettingsCaptureRect bounds)
    {
        EndToEndAssert.True(bounds.X >= window.X + 18 && bounds.Y >= window.Y + 18 &&
            bounds.X + bounds.Width <= window.X + window.Width - 18 &&
            bounds.Y + bounds.Height <= window.Y + window.Height - 18,
            "The observed native control must be inside its exact current window.");
        return new(name, window.Handle, (bounds.X + bounds.Width / 2) * window.UiScale,
            (bounds.Y + bounds.Height / 2) * window.UiScale, null, pointer);
    }
    private NativeWindowClickActionStep ClickProviderCard(string name)
    {
        var expected = ((IEnumerable<Mod>)cachedPages.GetValue(options)!).Where(m => m.SettingsCategory() == Title).ToArray();
        EndToEndAssert.Equal(2, expected.Length);
        int ordinal = Array.FindIndex(expected, m => ReferenceEquals(m, mods[name]));
        EndToEndAssert.True(ordinal >= 0);
        return ClickObserved("open original " + name + " native provider card", ChooserLabels()[ordinal].Bounds);
    }
    private NativeWindowClickActionStep ClickProviderOption(string action, string name) =>
        ClickObserved(action, ProviderLabel(name)!.Bounds);
    private SettingsCaptureRecord[] ChooserLabels() => chooser.LastCapture?.Records
        .Where(r => r.Kind == SettingsCaptureKind.Text && r.Text == Title).ToArray() ?? Array.Empty<SettingsCaptureRecord>();
    private SettingsCaptureRecord? ProviderLabel(string name) => provider.LastCapture?.Records
        .SingleOrDefault(r => r.Kind == SettingsCaptureKind.Text && r.Text == "Catalog " + name + " option");

    private IReadOnlyDictionary<string, string> VerifyCatalog()
    {
        RequireGame(); EndToEndAssert.True(failure == null, failure ?? "");
        EndToEndAssert.Equal(SettingsNeutralReadStatus.Captured, lastReadStatus, lastReadReason ?? "");
        EndToEndAssert.True(!catalog.Pending);
        var actual = ((IEnumerable<Mod>)cachedPages.GetValue(options)!).ToArray();
        EndToEndAssert.Equal(actual.Length, catalog.Pages.Count);
        for (int index = 0; index < actual.Length; index++)
        {
            var page = catalog.Pages[index];
            EndToEndAssert.True(catalog.TryResolve(page, out var resolved) && ReferenceEquals(resolved, actual[index]));
            EndToEndAssert.True(!catalog.TryResolve(page with { }, out _), "A copied descriptor must not forge a current opening token.");
        }
        var twins = catalog.Pages.Where(p => p.Title == Title && p.PackageIdentity == FixturePackage).ToArray();
        EndToEndAssert.Equal(2, twins.Length);
        EndToEndAssert.True(twins.All(p => p.RawAuthors == "Fumblesneeze"));
        EndToEndAssert.True(twins[0].PageIdentity != twins[1].PageIdentity && twins[0].OpenToken != twins[1].OpenToken);
        foreach (string name in Names)
            EndToEndAssert.Equal(1, twins.Count(p => catalog.TryResolve(p, out var resolved) && ReferenceEquals(resolved, mods[name])));
        AssertValues(false, false);
        return new Dictionary<string, string> { ["pages"] = catalog.Pages.Count.ToString(), ["providerTwins"] = twins.Length.ToString(),
            ["provider"] = providerMod.Content.PackageId, ["request"] = reads.ToString(),
            ["scope"] = "completed native catalog and ordinary provider editing; automatic provider option discovery remains incomplete" };
    }
    private string GetValue(string name) => (string)getSetting.Invoke(null, new object[] { ModId(name), "enabled" })!;
    private static string ModId(string name) => "megamod.catalog.fixture." + name;
    private void AssertValues(bool amber, bool cobalt)
    {
        EndToEndAssert.Equal(amber, bool.Parse(GetValue("amber")), "Amber option changed unexpectedly.");
        EndToEndAssert.Equal(cobalt, bool.Parse(GetValue("cobalt")), "Cobalt option changed unexpectedly.");
    }
    private IReadOnlyDictionary<string, string> PersistedValues()
    {
        var files = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*.xml")
            .Where(p => Path.GetFileName(p).EndsWith("_XmlMod.xml", StringComparison.OrdinalIgnoreCase)).ToArray();
        EndToEndAssert.Equal(1, files.Length, "Provider close must persist its actual own settings file.");
        var data = XDocument.Load(files[0]).Descendants("dataDict").Single();
        var keys = data.Element("keys")!.Elements("li").Select(e => e.Value).ToArray();
        var values = data.Element("values")!.Elements("li").Select(e => e.Value).ToArray();
        EndToEndAssert.Equal(keys.Length, values.Length);
        var persisted = keys.Zip(values, (key, value) => (key, value)).ToDictionary(p => p.key, p => p.value);
        foreach (string name in Names) EndToEndAssert.Equal(bool.Parse(GetValue(name)), bool.Parse(persisted[ModId(name) + ";enabled"]));
        return new Dictionary<string, string> { ["nativeSettingsFile"] = Path.GetFileName(files[0]),
            ["amber"] = GetValue("amber"), ["cobalt"] = GetValue("cobalt") };
    }
    private Vector2 OptionsPoint(MainTabWindow_Menu menu)
    {
        var labels = new List<string>();
        if (!GameDataSaveLoader.SavingIsTemporarilyDisabled && !game.Info.permadeathMode) labels.Add("Save");
        if (ReadBooleanField(typeof(MainTabWindow_Menu), "anyGameFiles", menu) && !game.Info.permadeathMode) labels.Add("LoadGame");
        labels.Add("ReviewScenario");
        var font = Text.Font;
        try
        {
            Text.Font = GameFont.Small;
            float y = labels.Sum(key => Math.Max(45, Text.CalcHeight(key.Translate(), 170)) + 7);
            return new(103, 18 + y + Math.Max(45, Text.CalcHeight("Options".Translate(), 170)) / 2);
        }
        finally { Text.Font = font; }
    }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting);
    private static Type RequiredType(string name) => AccessTools.TypeByName(name) ?? throw new TypeLoadException(name);
    private static bool ReadBooleanField(Type type, string name, object instance)
    {
        var field = AccessTools.Field(type, name) ?? throw new MissingFieldException(type.FullName, name);
        if (field.IsStatic || field.FieldType != typeof(bool) || field.GetValue(instance) is not bool value)
            throw new InvalidOperationException("Expected native instance Boolean field: " + type.FullName + "." + name);
        return value;
    }
    private static MethodInfo RequiredMethod(Type type, string name, bool isStatic, params Type[] parameters)
    {
        var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance),
            null, parameters, null) ?? throw new MissingMethodException(type.FullName, name);
        EndToEndAssert.Equal(isStatic, method.IsStatic); return method;
    }
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static WaitUntilStep Wait(string name, Func<bool> condition) => new(name, _ => condition(),
        new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(20)));

    private static IEnumerable<CodeInstruction> SurroundChooser(IEnumerable<CodeInstruction> instructions) =>
        ReplaceCall(instructions, RequiredMethod(typeof(Dialog_Options), "DoModOptions", false, typeof(Listing_Standard)), nameof(DrawChooser));
    private static IEnumerable<CodeInstruction> SurroundProvider(IEnumerable<CodeInstruction> instructions) =>
        ReplaceCall(instructions, RequiredMethod(RequiredType("XmlExtensions.SettingsMenuDef"), "DrawSettingsMenu", false, typeof(Rect)), nameof(DrawProvider));
    private static IEnumerable<CodeInstruction> ReplaceCall(IEnumerable<CodeInstruction> instructions, MethodInfo original, string replacement)
    {
        int replaced = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(original))
            {
                replaced++;
                yield return new CodeInstruction(instruction) { opcode = OpCodes.Call,
                    operand = AccessTools.Method(typeof(NativeSettingsCatalogTests), replacement) };
            }
            else yield return instruction;
        }
        if (replaced != 1) throw new InvalidOperationException("Expected exactly one ordinary draw edge: " + original);
    }
    private static void DrawChooser(Dialog_Options instance, Listing_Standard listing)
    {
        var owner = active ?? throw new InvalidOperationException("Catalog fixture is inactive.");
        owner.chooser.Observe(() => owner.drawChooser.Invoke(instance, new object[] { listing }));
    }
    private static void DrawProvider(object menu, Rect rect)
    {
        var owner = active ?? throw new InvalidOperationException("Catalog fixture is inactive.");
        owner.provider.Observe(() => owner.drawMenu.Invoke(menu, new object[] { rect }));
    }
    private static void DrawAddon(Dialog_Options __instance, Rect inRect)
    {
        var owner = active;
        if (owner == null || __instance.selectedCategory != OptionCategoryDefOf.Mods) return;
        try
        {
            if (Widgets.ButtonText(new Rect(0, inRect.yMax - 34, 190, 32), "Capture pages"))
            {
                owner.catalog.Refresh(__instance);
                if (owner.priorPages.Length != 0)
                    owner.staleInvalidated = owner.catalog.Pages.Count == 0 && owner.priorPages.All(p => !owner.catalog.TryResolve(p, out _));
            }
            Widgets.Label(new Rect(0, inRect.yMax - 59, 200, 22), owner.failure ??
                (owner.catalog.Pending ? "Catalog pending" : owner.reads == 0 ? "Catalog not captured" :
                    "Pages: " + owner.catalog.Pages.Count + "; twins: " + owner.catalog.Pages.Count(p => p.Title == Title)));
            // Native DoWindowContents and every addon widget have returned before the isolated read.
            if (owner.catalog.Pending)
            {
                owner.lastReadStatus = owner.catalog.ReadPending(owner.reader, out owner.lastReadReason);
                if (owner.lastReadStatus != SettingsNeutralReadStatus.Deferred)
                {
                    owner.reads++;
                    if (owner.lastReadStatus != SettingsNeutralReadStatus.Captured)
                        owner.failure ??= owner.lastReadReason ?? owner.lastReadStatus.ToString();
                }
            }
            else owner.probes?.Tick(owner.reader);
        }
        catch (Exception error) { owner.failure ??= error.ToString(); }
    }
}
