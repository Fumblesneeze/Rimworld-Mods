new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    Pawn primitiveCrafter = null;
    Pawn modernCrafter = null;
    foreach (var pawn in map.mapPawns.AllPawnsSpawned)
    {
        if (pawn.LabelShort == "Primitive Cookware Crafter")
        {
            primitiveCrafter = pawn;
        }
        else if (pawn.LabelShort == "Modern Cookware Machinist")
        {
            modernCrafter = pawn;
        }
    }

    if (primitiveCrafter == null || modernCrafter == null)
    {
        throw new System.InvalidOperationException("The fabrication fixture crafters are unavailable.");
    }

    var primitiveCell = new IntVec3(primitiveCrafter.Position.x + 3, 0, primitiveCrafter.Position.z);
    var modernCell = new IntVec3(modernCrafter.Position.x + 4, 0, modernCrafter.Position.z);
    var craftingSpot = primitiveCell.GetThingList(map)
        .FirstOrDefault(thing => thing.def.defName == "CraftingSpot");
    var machiningTable = modernCell.GetThingList(map)
        .FirstOrDefault(thing => thing.def.defName == "TableMachining");
    if (craftingSpot == null || machiningTable == null)
    {
        throw new System.InvalidOperationException("The exact fabrication work tables are unavailable.");
    }

    System.Func<Thing, WorkGiver_DoBill> resolveWorkGiver = table =>
    {
        foreach (var def in DefDatabase<WorkGiverDef>.AllDefsListForReading)
        {
            if (def.giverClass == typeof(WorkGiver_DoBill) &&
                def.fixedBillGiverDefs != null &&
                def.fixedBillGiverDefs.Contains(table.def))
            {
                return (WorkGiver_DoBill)def.Worker;
            }
        }

        throw new System.InvalidOperationException(
            "No finalized WorkGiver_DoBill owns " + table.def.defName + ".");
    };

    System.Func<Pawn, Thing, WorkGiver_DoBill, Verse.AI.Job> arm = (pawn, table, giver) =>
    {
        pawn.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
        var job = giver.JobOnThing(pawn, table, true);
        if (job == null || job.def != JobDefOf.DoBill)
        {
            throw new System.InvalidOperationException(
                "Vanilla could not create the expected DoBill job for " + pawn.LabelShort +
                " at " + table.LabelShort + ".");
        }

        job.playerForced = true;
        pawn.jobs.StartJob(job, Verse.AI.JobCondition.InterruptForced);
        if (pawn.CurJobDef != JobDefOf.DoBill)
        {
            throw new System.InvalidOperationException(
                "Vanilla rejected the fabrication job for " + pawn.LabelShort + ".");
        }

        return job;
    };

    var primitiveJob = arm(primitiveCrafter, craftingSpot, resolveWorkGiver(craftingSpot));
    var modernJob = arm(modernCrafter, machiningTable, resolveWorkGiver(machiningTable));
    Find.Selector.ClearSelection();
    Find.Selector.Select(primitiveCrafter);
    Find.Selector.Select(modernCrafter);
    Find.TickManager.Pause();
    return primitiveCrafter.ThingID + "|" + modernCrafter.ThingID + "|" +
           primitiveJob.loadID + "|" + modernJob.loadID;
})()
