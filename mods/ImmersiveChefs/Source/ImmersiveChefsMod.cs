using System.Reflection;
using System.Threading;
using HarmonyLib;
using Verse;

namespace ImmersiveChefs;

public sealed class ImmersiveChefsMod : Mod
{
    public const string PackageId = "fumblesneeze.immersivechefs";

    private static int initialized;

    public ImmersiveChefsMod(ModContentPack content) : base(content)
    {
        if (Interlocked.Exchange(ref initialized, 1) != 0)
        {
            return;
        }

        Integrations = IntegrationCatalog.Detect(
            LoadedModManager.RunningModsListForReading.Select(mod => mod.PackageId));

        new Harmony(PackageId).PatchAll(Assembly.GetExecutingAssembly());

        var activeIntegrations = Integrations.Active.Count == 0
            ? "none"
            : string.Join(", ", Integrations.Active);

        Log.Message($"[ImmersiveChefs] Initialized {PackageId}. Optional integrations: {activeIntegrations}");
    }

    public static IntegrationSnapshot? Integrations { get; private set; }
}
