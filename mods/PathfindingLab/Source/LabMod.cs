using System;
using System.Collections.Generic;
using HarmonyLib;
using PathfindingLab.Core;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PathfindingLab;

public sealed class LabSettings : ModSettings
{
    public bool Experimental;
    public int MapSize;
    public int RefinementCells = 50;
    public override void ExposeData()
    {
        Scribe_Values.Look(ref Experimental, "experimental", false);
        Scribe_Values.Look(ref MapSize, "mapSize", 0);
        Scribe_Values.Look(ref RefinementCells, "refinementCells", 50);
        RefinementCells = LabPolicy.RefinementCells(RefinementCells);
        MapSize = LabPolicy.SettlementSize(MapSize, true, false);
    }
}

public sealed class LabMod : Mod
{
    public static LabSettings Settings { get; private set; } = new();
    public static bool Ready { get; private set; }
    public LabMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<LabSettings>();
        var harmony = new Harmony("fumblesneeze.pathfindinglab");
        try
        {
            PathRuntime.ValidateShape();
            WildlifeWander.Install(harmony);
            Patch(typeof(Pawn_PathFollower), "GenerateNewPathRequest", Type.EmptyTypes, nameof(PathRuntime.GeneratePostfix), true);
            Patch(typeof(Pawn_PathFollower), "NeedNewPath", Type.EmptyTypes, nameof(PathRuntime.NeedNewPathPostfix), true);
            Patch(typeof(Pawn_PathFollower), "StopDead", Type.EmptyTypes, nameof(PathRuntime.StopPostfix), true);
            harmony.Patch(AccessTools.DeclaredMethod(typeof(PathFinder), "ParameterizePathJob"),
                postfix: new HarmonyMethod(typeof(PathRuntime), nameof(PathRuntime.AssociatePostfix)));
            Patch(typeof(PathFinderJob), nameof(PathFinderJob.Execute), Type.EmptyTypes, nameof(PathRuntime.ExecutePrefix), false);
            Patch(typeof(PathFinderMapData), "ParameterizePathJob", new[] { typeof(PathFinderJob).MakeByRefType() }, nameof(PathRuntime.SnapshotPostfix), true);
            Patch(typeof(PathFinderMapData), "Notify_CellDelta", new[] { typeof(IntVec3) }, nameof(PathRuntime.CellDirtyPostfix), true);
            Patch(typeof(PathFinderMapData), "Notify_CellDelta", new[] { typeof(CellRect) }, nameof(PathRuntime.RectDirtyPostfix), true);
            Patch(typeof(PathFinderMapData), "Notify_MapDirtied", Type.EmptyTypes, nameof(PathRuntime.AllDirtyPostfix), true);
            Patch(typeof(PathRequest), nameof(PathRequest.Resolve), new[] { typeof(PawnPath) }, nameof(PathRuntime.ResolvePostfix), true);
            Patch(typeof(PawnPath), "ReleaseToPool", Type.EmptyTypes, nameof(PathRuntime.ReleasePostfix), true);
            Patch(typeof(PathFinder), nameof(PathFinder.Dispose), Type.EmptyTypes, nameof(PathRuntime.DisposePrefix), false);
            var gizmos = AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos), Type.EmptyTypes)
                ?? throw new MissingMethodException("Pawn.GetGizmos");
            harmony.Patch(gizmos, postfix: new HarmonyMethod(typeof(LabMod), nameof(GizmosPostfix)));
            var generate = AccessTools.Method(typeof(MapGenerator), nameof(MapGenerator.GenerateMap),
                new[] { typeof(IntVec3), typeof(MapParent), typeof(MapGeneratorDef), typeof(IEnumerable<GenStepWithParams>), typeof(Action<Map>), typeof(bool), typeof(bool) })
                ?? throw new MissingMethodException("MapGenerator.GenerateMap");
            harmony.Patch(generate, prefix: new HarmonyMethod(typeof(LabMod), nameof(MapSizePrefix)));
            Ready = true;
        }
        catch (Exception exception)
        {
            harmony.UnpatchAll(harmony.Id);
            Log.Warning("[Pathfinding Lab] Unsupported game shape; experimental hooks disabled: " + exception.GetType().Name + ": " + exception.Message);
        }
        void Patch(Type type, string method, Type[] arguments, string patch, bool postfix)
        {
            var original = AccessTools.Method(type, method, arguments) ?? throw new MissingMethodException(type.FullName, method);
            var replacement = new HarmonyMethod(typeof(PathRuntime), patch);
            harmony.Patch(original, prefix: postfix ? null : replacement, postfix: postfix ? replacement : null);
        }
    }

    public override string SettingsCategory() => "PL.Title".Translate();
    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.Label("PL.Warning".Translate());
        listing.CheckboxLabeled("PL.Experimental".Translate(), ref Settings.Experimental);
        foreach (int cells in new[] { 25, 50, 100, 200 })
            if (listing.RadioButton("PL.RefinementCells".Translate(cells), Settings.RefinementCells == cells)) Settings.RefinementCells = cells;
        listing.Gap();
        listing.Label("PL.MapWarning".Translate());
        if (listing.RadioButton("PL.VanillaSize".Translate(), Settings.MapSize == 0)) Settings.MapSize = 0;
        foreach (int size in new[] { 500, 750, 1000 })
            if (listing.RadioButton("PL.MapSize".Translate(size), Settings.MapSize == size)) Settings.MapSize = size;
        listing.End();
    }

    private static void GizmosPostfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
    {
        if (!__instance.IsColonistPlayerControlled) return;
        __result = Append(__result);
        static IEnumerable<Gizmo> Append(IEnumerable<Gizmo> original)
        {
            foreach (var gizmo in original) yield return gizmo;
            bool requested = !Settings.Experimental;
            yield return new Command_Toggle
            {
                defaultLabel = "PL.Experimental".Translate(),
                defaultDesc = "PL.Warning".Translate(),
                isActive = () => Settings.Experimental,
                // RimWorld invokes all grouped colonist gizmos: apply one desired state, not N inversions.
                toggleAction = () => Settings.Experimental = requested
            };
        }
    }

    private static void MapSizePrefix(ref IntVec3 mapSize, MapParent parent, bool isPocketMap)
    {
        int size = LabPolicy.SettlementSize(Settings.MapSize, parent is Settlement && parent.Faction == Faction.OfPlayer, isPocketMap);
        if (size > 0) mapSize = new IntVec3(size, 1, size);
    }
}
