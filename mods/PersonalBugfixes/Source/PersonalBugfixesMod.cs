using System;
using System.Collections.Generic;
using System.Linq;
using PersonalBugfixes.Exploration;
using RimWorld.Planet;
using Verse;

namespace PersonalBugfixes;

public sealed class PersonalBugfixesMod : Mod
{
    public const string PackageId = "fumblesneeze.personalbugfixes";
    private static readonly FixLifecycle lifecycle = new();
    public static IReadOnlyList<FixResult> StartupResults { get; private set; } = Array.Empty<FixResult>();

    public PersonalBugfixesMod(ModContentPack content) : base(content) => LongEventHandler.ExecuteWhenFinished(Initialize);

    private static void Initialize()
    {
        if (StartupResults.Count != 0) return;
        var contents = LoadedModManager.RunningModsListForReading.Where(m =>
            StringComparer.OrdinalIgnoreCase.Equals(m.PackageIdPlayerFacing, ExplorationFix.PackageId)).ToArray();
        var fix = new ExplorationFix(contents.Length != 0, () =>
        {
            var assembly = contents.Single().assemblies.loadedAssemblies.Single(a => a.GetName().Name == "RimworldExplorationMode");
            return new ExplorationTarget(assembly.GetType("RimworldExploration.VisibilityManager", true),
                assembly.GetType("RimworldExploration.WorldFeatureManager", true), typeof(World), typeof(WorldFeature),
                typeof(Find), typeof(Current));
        });
        var result = lifecycle.Evaluate(fix);
        StartupResults = new[] { result };
        if (result.Warning) Log.Warning(result.ToString());
        else Log.Message(result.ToString());
    }
}
