using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

public static class BuildingAppearanceControls
{
    private sealed class Entry { public BuildingAppearance Value; }
    private static readonly ConditionalWeakTable<Building, Entry> Entries = new();

    public static BuildingAppearance Get(Thing? thing) =>
        thing is Building building && Entries.TryGetValue(building, out Entry entry) ? entry.Value : default;

    // A packed inner Thing may be drawn far from its old Position. Keep its choice, not its map transform.
    public static BuildingAppearance ForRendering(Thing? thing) => thing?.Spawned == true ? Get(thing) : default;

    public static bool IsEligible(Building building) => building.Spawned &&
        building.Faction == Faction.OfPlayer && building is not Frame &&
        !ThinWallUtility.IsThinEdgeDef(building.def) && building.def.category == ThingCategory.Building &&
        building.def.building?.isWall != true && building is not Building_Door &&
        building.def.graphicData != null && !building.def.graphicData.Linked &&
        building.def.graphicData.linkFlags == LinkFlags.None;

    public static void CycleScale(Building building) => Set(building, Get(building).NextScale());
    public static void CycleOffset(Building building) => Set(building, Get(building).NextOffset());

    private static void Set(Building building, BuildingAppearance appearance)
    {
        if (!IsEligible(building)) return;
        Store(building, appearance);
        Dirty(building);
    }

    // Lifecycle assignment only. Drawing and gizmos both read this same state, without another shift.
    public static void RefreshAutomaticOffset(Building building)
    {
        if (!building.Spawned) return;
        BuildingAppearance current = Get(building);
        if (current.OffsetIsManual) return;
        int step = IsEligible(building) ? BuildingAutomaticOffset.Resolve(building.OccupiedRect(),
            edge => ThinWallUtility.HasEdgeStructure(building.Map, edge, completedOnly: false)) : 0;
        if (step == current.OffsetStep) return;
        Store(building, current.WithAutomaticOffset(step));
        Dirty(building);
    }

    private static void Dirty(Building building)
    {
        if (building.Map is Map map)
        {
            foreach (IntVec3 cell in building.OccupiedRect())
                map.mapDrawer.MapMeshDirty(cell, (ulong)MapMeshFlagDefOf.Things |
                    (ulong)MapMeshFlagDefOf.BuildingsDamage | (ulong)MapMeshFlagDefOf.Buildings);
        }
    }

    private static void Store(Building building, BuildingAppearance appearance)
    {
        if (appearance.IsDefault && !appearance.OffsetIsManual) Entries.Remove(building);
        else Entries.GetOrCreateValue(building).Value = appearance;
    }

    public static void Expose(Building building)
    {
        BuildingAppearance appearance = Get(building);
        int scale = appearance.ScaleStep;
        int offset = appearance.OffsetStep;
        Scribe_Values.Look(ref scale, "thinWallsVisualScale", 0);
        Scribe_Values.Look(ref offset, "thinWallsVisualOffset", 0);
        bool manual = appearance.OffsetIsManual;
        // Old saves only persisted two integers; nonzero offsets were explicit gizmo choices.
        Scribe_Values.Look(ref manual, "thinWallsVisualOffsetManual", offset != 0);
        if (Scribe.mode == LoadSaveMode.LoadingVars) Store(building, new BuildingAppearance(scale, offset, manual));
    }
}

[HarmonyPatch(typeof(Building), nameof(Building.SetFaction), new[] { typeof(Faction), typeof(Pawn) })]
public static class BuildingAppearanceFactionPatch
{
    [HarmonyPostfix]
    private static void Postfix(Building __instance) => BuildingAppearanceControls.RefreshAutomaticOffset(__instance);
}

[HarmonyPatch(typeof(Building), nameof(Building.ExposeData))]
public static class BuildingAppearanceSavePatch
{
    [HarmonyPostfix]
    private static void Postfix(Building __instance) => BuildingAppearanceControls.Expose(__instance);
}

[HarmonyPatch(typeof(Building), nameof(Building.GetGizmos))]
public static class BuildingAppearanceGizmoPatch
{
    [HarmonyPostfix]
    private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building __instance)
    {
        foreach (Gizmo gizmo in __result) yield return gizmo;
        if (!BuildingAppearanceControls.IsEligible(__instance)) yield break;
        if (ThinWallsMod.Settings.ShowShrinkGizmo) yield return new Command_ShrinkBuilding(__instance);
        if (ThinWallsMod.Settings.ShowOffsetGizmo) yield return new Command_OffsetBuilding(__instance);
    }
}

public sealed class Command_ShrinkBuilding : Command_Action
{
    private readonly Building building;
    public Command_ShrinkBuilding(Building building)
    {
        this.building = building;
        defaultLabel = "TW_ShrinkBuilding".Translate();
        defaultDesc = "TW_ShrinkBuildingDesc".Translate();
        icon = TexUI.ArrowTexRight;
        groupable = false;
        action = () => BuildingAppearanceControls.CycleScale(building);
    }
    public override string TopRightLabel => BuildingAppearanceControls.Get(building).ScalePercent + "%";
    public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms) =>
        BuildingAppearanceIcons.Draw(rect, true, disabled, buttonMat, parms,
            Text.CalcHeight(LabelCap, rect.width + 0.1f), Text.LineHeight);
}

public sealed class Command_OffsetBuilding : Command_Action
{
    private readonly Building building;
    private static readonly string[] DirectionKeys =
    {
        "TW_OffsetCenter", "TW_OffsetN", "TW_OffsetNE", "TW_OffsetE", "TW_OffsetSE",
        "TW_OffsetS", "TW_OffsetSW", "TW_OffsetW", "TW_OffsetNW"
    };
    public Command_OffsetBuilding(Building building)
    {
        this.building = building;
        defaultLabel = "TW_OffsetBuilding".Translate();
        defaultDesc = "TW_OffsetBuildingDesc".Translate();
        icon = TexUI.ArrowTexRight;
        groupable = false;
        action = () => BuildingAppearanceControls.CycleOffset(building);
    }
    public override string TopRightLabel => DirectionKeys[BuildingAppearanceControls.Get(building).OffsetStep].Translate();
    public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms) =>
        BuildingAppearanceIcons.Draw(rect, false, disabled, buttonMat, parms,
            Text.CalcHeight(LabelCap, rect.width + 0.1f), Text.LineHeight);
}
