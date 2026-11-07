using NUnit.Framework;
using Verse;

// Assembly-wide guard: this is a CoreCLR metadata/unit process, never a mod host.
[SetUpFixture]
public sealed class WaylithUnitEnvironment
{
    [OneTimeSetUp]
    public void Before() => Verify();

    [OneTimeTearDown]
    public void After() => Verify();

    private static void Verify()
    {
        Assert.Multiple(() => {
            Assert.That(AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "0Harmony"), Is.False);
            Assert.That(LoadedModManager.RunningModsListForReading, Is.Empty);
            Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        });
    }
}
