using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using GuestBedGizmo.Beds;
using GuestBedGizmo.Compatibility.Hospitality;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace GuestBedGizmo.Harmony.Tests;

[TestFixture]
public sealed class GuestBedGizmoPatchProjectionTests
{
    [Test]
    public void ProjectionPreservesHospitalitysLegacyToggleWithoutAVanillaOwnerCommand()
    {
        Assembly hospitality = Assembly.LoadFile(Metadata("HospitalityAssemblyPath"));
        Assert.That(HospitalityRuntimeAdapter.TryResolve(
            new[] { hospitality },
            out HospitalityRuntimeAdapter? adapter,
            out string? failure), Is.True, failure);

        Type actionType = hospitality.GetType(HospitalityRuntimeContract.LegacyActionTypeName, true)!;
        object closure = FormatterServices.GetUninitializedObject(actionType);
        MethodInfo callback = actionType.GetMethod(
            HospitalityRuntimeContract.LegacyActionMethodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var legacy = (Command_Toggle)FormatterServices.GetUninitializedObject(typeof(Command_Toggle));
        legacy.toggleAction = (Action)Delegate.CreateDelegate(typeof(Action), closure, callback);
        var unrelated = (Command_Toggle)FormatterServices.GetUninitializedObject(typeof(Command_Toggle));
        unrelated.toggleAction = UnrelatedAction;

        var bed = (Building_Bed)FormatterServices.GetUninitializedObject(typeof(Building_Bed));
        Gizmo[] noOwnerCommand = GuestBedGizmoGizmoProjection
            .Project(bed, new Gizmo[] { unrelated, legacy }, adapter!)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                GuestBedGizmoGizmoProjection.Decide(legacy, adapter!),
                Is.EqualTo(GizmoProjectionDecision.RemoveLegacyToggle));
            Assert.That(noOwnerCommand, Is.EqualTo(new Gizmo[] { unrelated, legacy }),
                "Cribs and other beds without a vanilla owner command must keep Hospitality's legacy toggle.");
        });
    }

    private static void UnrelatedAction()
    {
    }

    private static string Metadata(string key) =>
        typeof(GuestBedGizmoPatchProjectionTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == key)
            .Value;
}
