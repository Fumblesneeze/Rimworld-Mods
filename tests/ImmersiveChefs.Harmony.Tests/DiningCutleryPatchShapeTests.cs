using HarmonyLib;
using NUnit.Framework;
using RimWorld;
using Verse.AI;

namespace ImmersiveChefs.Harmony.Tests;

[TestFixture]
public sealed class DiningCutleryPatchShapeTests
{
    [Test]
    public void JobDriver_ingest_exposes_the_exact_chewing_toil_landmark()
    {
        var field = AccessTools.Field(typeof(JobDriver_Ingest), "chewing");

        Assert.Multiple(() =>
        {
            Assert.That(field, Is.Not.Null);
            Assert.That(field?.FieldType, Is.EqualTo(typeof(Toil)));
        });
    }
}
