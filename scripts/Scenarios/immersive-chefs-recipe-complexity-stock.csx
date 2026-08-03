new System.Func<string>(() =>
{
    var stage = "resolve matched cooks";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The recipe-complexity fixture has no map.");
        }

        Pawn simpleCook = null;
        Pawn fineCook = null;
        Pawn lavishCook = null;
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.LabelShort == "Simple Recipe Cook") simpleCook = pawn;
            else if (pawn.LabelShort == "Fine Recipe Cook") fineCook = pawn;
            else if (pawn.LabelShort == "Lavish Recipe Cook") lavishCook = pawn;
        }
        if (simpleCook == null || fineCook == null || lavishCook == null)
        {
            throw new System.InvalidOperationException("The exact three matched cooks are unavailable.");
        }

        var cookwareDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware");
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var rawRice = DefDatabase<ThingDef>.GetNamed("RawRice");
        var milk = DefDatabase<ThingDef>.GetNamed("Milk");
        var cooks = new[] { simpleCook, fineCook, lavishCook };
        for (var index = 0; index < cooks.Length; index++)
        {
            stage = "stock kitchen " + index;
            var cook = cooks[index];
            var roomCenter = new IntVec3(cook.Position.x, 0, cook.Position.z + 3);
            var cookware = (ThingWithComps)ThingMaker.MakeThing(cookwareDef, ThingDefOf.Steel);
            cookware.GetComp<CompQuality>()
                .SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            cookware.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            GenSpawn.Spawn(cookware, new IntVec3(roomCenter.x - 4, 0, roomCenter.z + 2), map);

            var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
            plate.GetComp<CompQuality>()
                .SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            plate.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            GenSpawn.Spawn(plate, new IntVec3(roomCenter.x - 4, 0, roomCenter.z - 2), map);

            var rice = ThingMaker.MakeThing(rawRice);
            rice.stackCount = 20;
            GenSpawn.Spawn(rice, new IntVec3(roomCenter.x + 3, 0, roomCenter.z + 2), map);
            if (index > 0)
            {
                var milkStack = ThingMaker.MakeThing(milk);
                milkStack.stackCount = 20;
                GenSpawn.Spawn(milkStack, new IntVec3(roomCenter.x + 3, 0, roomCenter.z - 2), map);
            }
        }

        Find.TickManager.Pause();
        return simpleCook.ThingID + "|" + fineCook.ThingID + "|" + lavishCook.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Recipe-complexity stock failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
