new System.Func<string>(() =>
{
    var stage = "resolve route workers";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The kitchenware route matrix requires a playable map.");
        }

        var workerNames = new[]
        {
            "Wood Plate Crafter",
            "Wood Cutlery Crafter",
            "Adobe Plate Crafter",
            "Silver Cookware Smith",
            "Silver Plate Smith",
            "Silver Cutlery Smith"
        };
        var recipeNames = new[]
        {
            "ImmersiveChefs_MakeSoftPlates",
            "ImmersiveChefs_MakeSoftCutlery",
            "ImmersiveChefs_MakeAdobePlates",
            "ImmersiveChefs_MakeMedievalCookware",
            "ImmersiveChefs_SmithPlates",
            "ImmersiveChefs_SmithCutlery"
        };
        var workers = new Pawn[workerNames.Length];
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            for (var index = 0; index < workerNames.Length; index++)
            {
                if (pawn.LabelShort == workerNames[index])
                {
                    workers[index] = pawn;
                }
            }
        }

        var jobs = new Verse.AI.Job[workers.Length];
        for (var index = 0; index < workers.Length; index++)
        {
            stage = "arm " + recipeNames[index];
            var worker = workers[index];
            if (worker == null)
            {
                throw new System.InvalidOperationException(
                    "Route worker is unavailable: " + workerNames[index] + ".");
            }

            Thing table = null;
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
                    if (bill.recipe.defName == recipeNames[index] && bill.PawnRestriction == worker)
                    {
                        table = candidate;
                        break;
                    }
                }

                if (table != null)
                {
                    break;
                }
            }

            if (table == null)
            {
                throw new System.InvalidOperationException(
                    "Could not resolve the exact bill giver for " + recipeNames[index] + ".");
            }

            WorkGiver_DoBill workGiver = null;
            foreach (var def in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            {
                if (def.giverClass == typeof(WorkGiver_DoBill) &&
                    def.fixedBillGiverDefs != null &&
                    def.fixedBillGiverDefs.Contains(table.def))
                {
                    workGiver = (WorkGiver_DoBill)def.Worker;
                    break;
                }
            }

            if (workGiver == null)
            {
                throw new System.InvalidOperationException(
                    "No finalized WorkGiver_DoBill owns " + table.def.defName + ".");
            }

            worker.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
            var job = workGiver.JobOnThing(worker, table, true);
            if (job == null || job.def != JobDefOf.DoBill)
            {
                var refuelable = table.TryGetComp<CompRefuelable>();
                var giver = (IBillGiver)table;
                var routeBill = giver.BillStack[0];
                throw new System.InvalidOperationException(
                    "Vanilla could not create DoBill for " + recipeNames[index] +
                    "; returned=" + (job == null ? "<null>" : job.def.defName) +
                    ", workGiver=" + workGiver.def.defName +
                    ", requiredWorkType=" + routeBill.recipe.requiredGiverWorkType.defName +
                    ", billReady=" + routeBill.ShouldDoNow() +
                    ", pawnAllowed=" + routeBill.PawnAllowedToStartAnew(worker) +
                    ", fuel=" + (refuelable == null ? "n/a" : refuelable.Fuel.ToString()) +
                    ", hasFuel=" + (refuelable == null ? "n/a" : refuelable.HasFuel.ToString()) + ".");
            }

            job.playerForced = true;
            worker.jobs.StartJob(job, Verse.AI.JobCondition.InterruptForced);
            if (worker.CurJobDef != JobDefOf.DoBill)
            {
                throw new System.InvalidOperationException(
                    "Vanilla rejected DoBill for " + recipeNames[index] + ".");
            }

            jobs[index] = job;
        }

        stage = "pause armed routes";
        Find.Selector.ClearSelection();
        for (var index = 0; index < workers.Length; index++)
        {
            Find.Selector.Select(workers[index]);
        }

        Find.TickManager.Pause();
        var jobIds = new string[jobs.Length];
        for (var index = 0; index < jobs.Length; index++)
        {
            jobIds[index] = jobs[index].loadID.ToString();
        }

        return string.Join("|", jobIds);
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
