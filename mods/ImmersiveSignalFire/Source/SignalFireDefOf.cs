using Blues;
using RimWorld;
using Verse;

namespace ImmersiveSignalFire;

[DefOf]
public static class SignalFireDefOf
{
    public static ThingDef ImmersiveSignalFire_SignalFire = null!;
    public static JobDef ImmersiveSignalFire_Approach = null!;
    public static JobDef ImmersiveSignalFire_Signal = null!;
    public static SingleTakeDef ImmersiveSignalFire_Flame = null!;
    public static SingleTakeDef ImmersiveSignalFire_DarkSmokePulse = null!;

    static SignalFireDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(SignalFireDefOf));
    }
}
