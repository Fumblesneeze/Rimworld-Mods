using NUnit.Framework;

[SetUpFixture]
public sealed class EnvironmentGuard
{
    [OneTimeSetUp, OneTimeTearDown]
    public void No_Harmony_or_active_mods_or_defs_in_ordinary_tests()
    {
        Assert.That(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name), Does.Not.Contain("0Harmony"));
        Assert.That(Verse.LoadedModManager.RunningModsListForReading, Is.Empty);
        Assert.That(Verse.DefDatabase<Verse.ThingDef>.AllDefsListForReading, Is.Empty);
    }
}
