namespace ThinWalls.Designation;

public sealed class Designator_ThinDoor : Designator_ThinWall
{
    public Designator_ThinDoor() : base(
        ThinWallUtility.ThinDoorDefName,
        "TW_ThinDoorDesignatorLabel",
        "TW_ThinDoorDesignatorDescription")
    {
    }
}
