using System.Threading;
using ImmersiveSignalFire.Effects;
using ImmersiveSignalFire.Interactions;
using Verse;

namespace ImmersiveSignalFire;

public sealed class SignalFireMod : Mod
{
    private static int initialized;

    public SignalFireMod(ModContentPack content) : base(content)
    {
        if (Interlocked.Exchange(ref initialized, 1) != 0)
        {
            return;
        }

        NoPawnSignalFireMenuPatch.EnsureInstalled();
        LongEventHandler.ExecuteWhenFinished(InitializeAfterContentLoad);
    }

    private static void InitializeAfterContentLoad()
    {
        OptionalToolsAdapter.Initialize();
    }
}
