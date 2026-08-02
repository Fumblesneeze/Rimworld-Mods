using System.Reflection;
using System.Threading;
using HarmonyLib;
using Verse;

namespace ImmersiveChefs;

public sealed class ImmersiveChefsMod : Mod
{
    public const string PackageId = "fumblesneeze.immersivechefs";

    private static int initialized;
    private readonly ImmersiveChefsSettingsUi settingsUi = new();

    public ImmersiveChefsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<ImmersiveChefsSettings>();
        Settings.ClampToAllowedRanges();

        if (Interlocked.Exchange(ref initialized, 1) != 0)
        {
            return;
        }

        Integrations = IntegrationCatalog.Detect(
            LoadedModManager.RunningModsListForReading.Select(mod => mod.PackageId));

        ImmersiveChefsDefBootstrap.Apply();

        HarmonyInstance = new Harmony(PackageId);
        HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

        var activeIntegrations = Integrations.Active.Count == 0
            ? "none"
            : string.Join(", ", Integrations.Active);

        Log.Message($"[ImmersiveChefs] Initialized {PackageId}. Optional integrations: {activeIntegrations}");
    }

    public static IntegrationSnapshot? Integrations { get; private set; }

    public static ImmersiveChefsSettings Settings { get; private set; } = new();

    internal static Harmony? HarmonyInstance { get; private set; }

    internal static bool IsIntegrationEnabled(OptionalIntegration integration)
    {
        return Integrations is { } snapshot &&
               OptionalIntegrationPolicy.IsEnabled(integration, snapshot, Settings);
    }

    public override string SettingsCategory()
    {
        return "Immersive Chefs";
    }

    public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
    {
        settingsUi.Draw(inRect, Settings);
    }
}
