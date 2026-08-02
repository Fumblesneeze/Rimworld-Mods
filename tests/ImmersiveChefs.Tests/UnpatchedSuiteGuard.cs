using ImmersiveChefs;
using NUnit.Framework;
using RimWorld;
using Verse;

[SetUpFixture]
public sealed class UnpatchedSuiteGuard
{
    [OneTimeSetUp]
    public void Ordinary_suite_starts_without_bootstrapped_mod_patch_or_def_state()
    {
        AssertCleanState();
    }

    [OneTimeTearDown]
    public void Ordinary_suite_finished_without_bootstrapped_mod_patch_or_def_state()
    {
        AssertCleanState();
    }

    private static void AssertCleanState()
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
