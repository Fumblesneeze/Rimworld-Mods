new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    if (map == null)
    {
        throw new System.InvalidOperationException("The chef's knife fixture has no current map.");
    }

    var machinist = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Chef Knife Machinist");
    if (machinist == null)
    {
        throw new System.InvalidOperationException("The chef's knife machinist is unavailable.");
    }

    Thing machiningTable = null;
    foreach (var candidate in map.listerThings.ThingsInGroup(ThingRequestGroup.PotentialBillGiver))
    {
        var giver = candidate as IBillGiver;
        if (giver == null)
        {
            continue;
        }

        for (var billIndex = 0; billIndex < giver.BillStack.Count; billIndex++)
        {
            var bill = giver.BillStack[billIndex];
            if (bill.recipe.defName == "ImmersiveChefs_MakeChefsKnife" &&
                bill.PawnRestriction == machinist)
            {
                machiningTable = candidate;
                break;
            }
        }

        if (machiningTable != null)
        {
            break;
        }
    }

    if (machiningTable == null)
    {
        throw new System.InvalidOperationException("The exact chef's knife bill giver is unavailable.");
    }

    WorkGiver_DoBill workGiver = null;
    foreach (var def in DefDatabase<WorkGiverDef>.AllDefsListForReading)
    {
        if (def.giverClass == typeof(WorkGiver_DoBill) &&
            def.fixedBillGiverDefs != null &&
            def.fixedBillGiverDefs.Contains(machiningTable.def))
        {
            workGiver = (WorkGiver_DoBill)def.Worker;
            break;
        }
    }

    if (workGiver == null)
    {
        throw new System.InvalidOperationException("No finalized machining WorkGiver_DoBill exists.");
    }

    machinist.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
    var job = workGiver.JobOnThing(machinist, machiningTable, true);
    if (job == null || job.def != JobDefOf.DoBill)
    {
        throw new System.InvalidOperationException(
            "Vanilla could not create the chef's knife fabrication job.");
    }

    job.playerForced = true;
    machinist.jobs.StartJob(job, Verse.AI.JobCondition.InterruptForced);
    if (machinist.CurJobDef != JobDefOf.DoBill)
    {
        throw new System.InvalidOperationException("Vanilla rejected the chef's knife fabrication job.");
    }

    Find.Selector.ClearSelection();
    Find.Selector.Select(machinist);
    Find.TickManager.Pause();
    return machinist.ThingID + "|" + job.loadID;
})()
