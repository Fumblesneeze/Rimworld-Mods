using NUnit.Framework;
using RimWorld;
using Verse;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class TestHostIsolationTests
{
    [Test]
    public void Default_suite_does_not_bootstrap_mods_harmony_or_defs()
    {
        var loadedAssemblyNames = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetName().Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(ImmersiveChefsMod.Integrations, Is.Null);
            Assert.That(LoadedModManager.RunningModsListForReading, Is.Empty);
            Assert.That(loadedAssemblyNames, Does.Not.Contain("0Harmony"));
            Assert.That(DefDatabase<BillRepeatModeDef>.DefCount, Is.Zero);
        });
    }
}
