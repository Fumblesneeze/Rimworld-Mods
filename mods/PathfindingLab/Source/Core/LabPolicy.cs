namespace PathfindingLab.Core;

public static class LabPolicy
{
    public static int RefinementCells(int requested) => requested is 25 or 50 or 100 or 200 ? requested : 50;
    public static int RenewalNodes(int requested) => RefinementCells(requested) / 2 + 1;
    public static int SettlementSize(int requested, bool playerSettlement, bool pocketMap) =>
        playerSettlement && !pocketMap && requested is 500 or 750 or 1000 ? requested : 0;
}
