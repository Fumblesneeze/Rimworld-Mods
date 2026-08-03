new System.Func<string>(() =>
{
    var stage = "resolve cooking rooms";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The ware-selection fixture has no current map.");
        }

        var cleanCook = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Clean Ware Cook");
        var urgentCook = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Urgent Dirty Ware Cook");
        if (cleanCook == null || urgentCook == null)
        {
            throw new System.InvalidOperationException("The exact cooking workers are unavailable.");
        }

        var cleanCenter = new IntVec3(cleanCook.Position.x, 0, cleanCook.Position.z + 3);
        var urgentCenter = new IntVec3(urgentCook.Position.x, 0, urgentCook.Position.z + 3);
        var cookwareDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware");
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");

        stage = "place inferior clean cookware";
        var cleanCookware = (ThingWithComps)ThingMaker.MakeThing(cookwareDef, ThingDefOf.Steel);
        cleanCookware.GetComp<CompQuality>().SetQuality(QualityCategory.Awful, ArtGenerationContext.Colony);
        cleanCookware.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        GenSpawn.Spawn(cleanCookware, new IntVec3(cleanCenter.x - 4, 0, cleanCenter.z + 2), map);

        var cleanPlate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        cleanPlate.GetComp<CompQuality>().SetQuality(QualityCategory.Awful, ArtGenerationContext.Colony);
        cleanPlate.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        GenSpawn.Spawn(cleanPlate, new IntVec3(cleanCenter.x - 4, 0, cleanCenter.z - 2), map);

        stage = "place superior dirty controls";
        var rejectedCookware = (ThingWithComps)ThingMaker.MakeThing(cookwareDef, ThingDefOf.Gold);
        rejectedCookware.GetComp<CompQuality>()
            .SetQuality(QualityCategory.Legendary, ArtGenerationContext.Colony);
        rejectedCookware.GetComp<ImmersiveChefs.CompSanitation>().MarkDirty();
        GenSpawn.Spawn(rejectedCookware, new IntVec3(cleanCenter.x + 4, 0, cleanCenter.z + 2), map);

        var rejectedPlate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Gold);
        rejectedPlate.GetComp<CompQuality>()
            .SetQuality(QualityCategory.Legendary, ArtGenerationContext.Colony);
        rejectedPlate.GetComp<ImmersiveChefs.CompSanitation>().MarkDirty();
        GenSpawn.Spawn(rejectedPlate, new IntVec3(cleanCenter.x + 4, 0, cleanCenter.z - 2), map);

        stage = "place urgent-only dirty ware";
        var urgentCookware = (ThingWithComps)ThingMaker.MakeThing(cookwareDef, ThingDefOf.Steel);
        urgentCookware.GetComp<CompQuality>()
            .SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        urgentCookware.GetComp<ImmersiveChefs.CompSanitation>().MarkDirty();
        GenSpawn.Spawn(urgentCookware, new IntVec3(urgentCenter.x - 3, 0, urgentCenter.z + 2), map);

        var urgentPlate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        urgentPlate.GetComp<CompQuality>()
            .SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        urgentPlate.GetComp<ImmersiveChefs.CompSanitation>().MarkDirty();
        GenSpawn.Spawn(urgentPlate, new IntVec3(urgentCenter.x - 3, 0, urgentCenter.z - 2), map);

        stage = "place exact raw ingredients";
        var rawRice = DefDatabase<ThingDef>.GetNamed("RawRice");
        var cleanRice = ThingMaker.MakeThing(rawRice);
        cleanRice.stackCount = 12;
        GenSpawn.Spawn(cleanRice, new IntVec3(cleanCenter.x + 3, 0, cleanCenter.z + 3), map);
        var urgentRice = ThingMaker.MakeThing(rawRice);
        urgentRice.stackCount = 12;
        GenSpawn.Spawn(urgentRice, new IntVec3(urgentCenter.x + 3, 0, urgentCenter.z + 3), map);

        ImmersiveChefs.ImmersiveChefsMod.Settings.WareRequirementMode =
            ImmersiveChefs.WareRequirementMode.Strict;
        ImmersiveChefs.ImmersiveChefsMod.Settings.DirtyWareFallback =
            ImmersiveChefs.DirtyWareFallback.UrgentOnly;
        Find.TickManager.Pause();
        return cleanCookware.ThingID + "|" + cleanPlate.ThingID + "|" +
               rejectedCookware.ThingID + "|" + rejectedPlate.ThingID + "|" +
               urgentCookware.ThingID + "|" + urgentPlate.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Cooking ware stock failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
