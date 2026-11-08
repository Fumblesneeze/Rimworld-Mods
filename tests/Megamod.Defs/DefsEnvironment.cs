using NUnit.Framework;
using Verse;

namespace Megamod.Defs.Tests;

[SetUpFixture]
public sealed class DefsEnvironment
{
    [OneTimeSetUp]
    public void Before() => Verify();

    [OneTimeTearDown]
    public void After() => Verify();

    private static void Verify()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "0Harmony"), Is.False);
            Assert.That(LoadedModManager.RunningModsListForReading, Is.Empty);
            Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        });
    }
}
