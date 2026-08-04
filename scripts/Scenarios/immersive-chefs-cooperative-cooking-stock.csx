new System.Func<string>(() =>
{
    var stage = "resolve named cooks";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The cooperative-cooking fixture has no map.");
        }

        var assistedLead = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Assisted Lead");
        var controlLead = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Control Lead");
        if (assistedLead == null || controlLead == null)
        {
            throw new System.InvalidOperationException("The matched lead cooks are unavailable.");
        }

        var assistedCenter = new IntVec3(assistedLead.Position.x + 5, 0, assistedLead.Position.z + 2);
        var controlCenter = new IntVec3(controlLead.Position.x + 5, 0, controlLead.Position.z + 2);
        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var rawRice = DefDatabase<ThingDef>.GetNamed("RawRice");
        var cookwareDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware");
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        Thing assistedStove = null;
        Thing controlStove = null;
        var roomCenters = new[] { assistedCenter, controlCenter };
        var leads = new[] { assistedLead, controlLead };
        stage = "create matched native bills and supplies";
        for (var index = 0; index < roomCenters.Length; index++)
        {
            var roomCenter = roomCenters[index];
            var lead = leads[index];
            var stove = ThingMaker.MakeThing(
                stoveDef,
                stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
            stove.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(stove, new IntVec3(roomCenter.x, 0, roomCenter.z + 2), map, Rot4.North);
            var fuel = stove.TryGetComp<CompRefuelable>();
            fuel.Refuel(fuel.Props.fuelCapacity);

            var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"));
            bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
            bill.repeatCount = 1;
            bill.ingredientSearchRadius = 12f;
            bill.ingredientFilter.SetDisallowAll();
            bill.ingredientFilter.SetAllow(rawRice, true);
            bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
            bill.SetPawnRestriction(lead);
            ((IBillGiver)stove).BillStack.AddBill(bill);

            var cookware = (ThingWithComps)ThingMaker.MakeThing(cookwareDef, ThingDefOf.Steel);
            cookware.GetComp<CompQuality>()
                .SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            cookware.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            GenSpawn.Spawn(
                cookware,
                new IntVec3(roomCenter.x - 5, 0, roomCenter.z - 4),
                map);

            var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
            plate.GetComp<CompQuality>()
                .SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            plate.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            GenSpawn.Spawn(
                plate,
                new IntVec3(roomCenter.x - 4, 0, roomCenter.z - 4),
                map);

            var rice = ThingMaker.MakeThing(rawRice);
            rice.stackCount = 20;
            GenSpawn.Spawn(rice, new IntVec3(roomCenter.x - 3, 0, roomCenter.z - 4), map);
            if (index == 0)
            {
                assistedStove = stove;
            }
            else
            {
                controlStove = stove;
            }
        }

        return assistedStove.ThingID + "|" + controlStove.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Cooperative-cooking stock failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
