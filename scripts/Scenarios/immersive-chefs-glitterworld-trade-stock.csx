new System.Func<string>(() =>
{
    var stage = "resolve powered trade fixture";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The glitterworld trade fixture has no current map.");
        }

        var negotiator = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Glitter Cookware Buyer");
        var console = Find.Selector.SingleSelectedThing;
        if (negotiator == null || console == null || console.def.defName != "CommsConsole")
        {
            throw new System.InvalidOperationException(
                "The exact trade negotiator or selected comms console is unavailable.");
        }

        Building_OrbitalTradeBeacon beacon = null;
        var nearestBeaconDistance = int.MaxValue;
        foreach (var candidateThing in map.listerThings
                     .ThingsOfDef(DefDatabase<ThingDef>.GetNamed("OrbitalTradeBeacon")))
        {
            var candidate = candidateThing as Building_OrbitalTradeBeacon;
            if (candidate == null)
            {
                continue;
            }

            var distance = candidate.Position.DistanceToSquared(console.Position);
            if (distance < nearestBeaconDistance)
            {
                beacon = candidate;
                nearestBeaconDistance = distance;
            }
        }

        if (beacon == null || !beacon.TryGetComp<CompPowerTrader>().PowerOn)
        {
            throw new System.InvalidOperationException("The powered orbital trade beacon is unavailable.");
        }

        stage = "place colony trade silver under the beacon";
        var silverCells = new System.Collections.Generic.List<IntVec3>();
        foreach (var cell in beacon.TradeableCells)
        {
            if (cell.InBounds(map) && cell.Standable(map) && cell.GetFirstItem(map) == null)
            {
                silverCells.Add(cell);
                if (silverCells.Count == 10)
                {
                    break;
                }
            }
        }
        if (silverCells.Count != 10)
        {
            throw new System.InvalidOperationException(
                "The orbital trade beacon exposed fewer than ten free trade cells.");
        }

        foreach (var silverCell in silverCells)
        {
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = ThingDefOf.Silver.stackLimit;
            GenSpawn.Spawn(silver, silverCell, map);
        }

        stage = "seed trader-owned glitterworld cookware";
        var ship = new TradeShip(
            DefDatabase<TraderKindDef>.GetNamed("Orbital_Exotic"),
            null);
        ship.GetDirectlyHeldThings().ClearAndDestroyContents();
        var glitterworld = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_GlitterworldCookware"));
        glitterworld.GetComp<CompQuality>().SetQuality(QualityCategory.Awful, ArtGenerationContext.Colony);
        QualityCategory generatedQuality;
        if (!QualityUtility.TryGetQuality(glitterworld, out generatedQuality) ||
            generatedQuality < QualityCategory.Good)
        {
            throw new System.InvalidOperationException(
                "Glitterworld cookware did not apply its Good-quality generation floor.");
        }

        if (!ship.GetDirectlyHeldThings().TryAdd(glitterworld))
        {
            throw new System.InvalidOperationException(
                "The orbital trader did not accept the glitterworld cookware stock item.");
        }

        map.passingShipManager.AddShip(ship);
        if (!map.passingShipManager.passingShips.Contains(ship) ||
            !ship.Goods.Contains(glitterworld))
        {
            throw new System.InvalidOperationException(
                "The glitterworld cookware is not owned by the active orbital trader.");
        }

        Find.TickManager.Pause();
        return negotiator.ThingID + "|" + ship.FullTitle + "|" + generatedQuality;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Glitterworld trader stocking failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
