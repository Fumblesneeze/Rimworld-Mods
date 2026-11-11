using Verse;

namespace Megamod;

// Do not reference this type from mod construction or ordinary runtime code. Native CallAll
// reaches its constructor after initial Def resolution; that worker queues this callback for
// the main-thread completion drain before the next queued new-game/load worker is dispatched.
[StaticConstructorOnStartup]
internal static class InitialDefinitionsStartup
{
    static InitialDefinitionsStartup() => LongEventHandler.ExecuteWhenFinished(MegamodMod.PublishInitialDefinitions);
}
