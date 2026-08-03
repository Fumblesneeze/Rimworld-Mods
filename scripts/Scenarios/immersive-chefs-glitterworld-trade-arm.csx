new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    if (map == null)
    {
        throw new System.InvalidOperationException("The glitterworld trade fixture has no current map.");
    }

    var negotiator = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Glitter Cookware Buyer");
    if (negotiator == null)
    {
        throw new System.InvalidOperationException("The glitterworld trade negotiator is unavailable.");
    }

    var ship = map.passingShipManager.passingShips
        .OfType<TradeShip>()
        .FirstOrDefault(candidate => candidate.Goods.Any(thing =>
            thing.def.defName == "ImmersiveChefs_GlitterworldCookware"));
    if (ship == null)
    {
        throw new System.InvalidOperationException("The glitterworld cookware trade ship is unavailable.");
    }

    ship.TryOpenComms(negotiator);
    if (!Find.WindowStack.Windows.Any(window => window is Dialog_Trade))
    {
        throw new System.InvalidOperationException("RimWorld did not open its native trade dialog.");
    }

    Find.TickManager.Pause();
    return negotiator.ThingID + "|" + ship.FullTitle;
})()
