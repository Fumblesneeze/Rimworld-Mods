using Verse;

namespace ThinWalls;

public sealed class ThinWallsSettings : ModSettings
{
    public bool ShowShrinkGizmo = true;
    public bool ShowOffsetGizmo = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref ShowShrinkGizmo, "showShrinkGizmo", true);
        Scribe_Values.Look(ref ShowOffsetGizmo, "showOffsetGizmo", true);
    }
}
