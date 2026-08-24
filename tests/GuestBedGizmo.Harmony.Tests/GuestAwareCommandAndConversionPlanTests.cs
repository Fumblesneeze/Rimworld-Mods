using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using GuestBedGizmo.Beds;
using GuestBedGizmo.Compatibility.Hospitality;
using NUnit.Framework;
using Verse;

namespace GuestBedGizmo.Harmony.Tests;

[TestFixture]
public sealed class GuestAwareCommandAndConversionPlanTests
{
    [Test]
    public void OwnerCommandIsReplacedAndOnlyBedsWithTheWrongGuestStateAreSwapped()
    {
        Assembly hospitality = Assembly.LoadFile(Metadata("HospitalityAssemblyPath"));
        Assert.That(HospitalityRuntimeAdapter.TryResolve(
            new[] { hospitality },
            out HospitalityRuntimeAdapter? adapter,
            out string? failure), Is.True, failure);

        var ownerCommand = (Command_SetBedOwnerType)FormatterServices.GetUninitializedObject(
            typeof(Command_SetBedOwnerType));
        var unrelated = (Command_Toggle)FormatterServices.GetUninitializedObject(typeof(Command_Toggle));
        bool guestsAllowed = BedSelectionConversionPlan.TryCreate(
            new[] { false, true, false },
            new[] { true, true, false },
            new[] { true, true, false },
            BedOwnerChoiceKind.Guest,
            preflightAllowed: true,
            out BedSelectionConversionPlan toGuests);
        bool prisonersAllowed = BedSelectionConversionPlan.TryCreate(
            new[] { false, true, false },
            new[] { true, true, false },
            new[] { true, true, false },
            BedOwnerChoiceKind.Prisoner,
            preflightAllowed: true,
            out BedSelectionConversionPlan toPrisoners);

        Assert.Multiple(() =>
        {
            Assert.That(
                GuestBedGizmoGizmoProjection.Decide(ownerCommand, adapter!),
                Is.EqualTo(GizmoProjectionDecision.ReplaceOwnerCommand));
            Assert.That(
                GuestBedGizmoGizmoProjection.Decide(unrelated, adapter!),
                Is.EqualTo(GizmoProjectionDecision.Preserve));
            Assert.That(
                typeof(Command_GuestAwareBedOwnerType).IsSubclassOf(typeof(Command_Action)),
                Is.True,
                "The unified owner command must remain on the Gateway-supported native action path.");
            Assert.That(guestsAllowed, Is.True);
            Assert.That(prisonersAllowed, Is.True);
            Assert.That(toGuests.SwapIndexes, Is.EqualTo(new[] { 0 }));
            Assert.That(toGuests.ApplyVanillaOwnerType, Is.False);
            Assert.That(toPrisoners.SwapIndexes, Is.EqualTo(new[] { 1 }));
            Assert.That(toPrisoners.ApplyVanillaOwnerType, Is.True);
        });
    }

    [Test]
    public void ConversionPreflightRejectsBeforeAnySwapAndExcludesUnsupportedSelectedBeds()
    {
        bool invalidPrisonRoom = BedSelectionConversionPlan.TryCreate(
            new[] { true, false },
            new[] { true, true },
            new[] { true, true },
            BedOwnerChoiceKind.Prisoner,
            preflightAllowed: false,
            out BedSelectionConversionPlan prisonerPlan);
        bool missingReplacement = BedSelectionConversionPlan.TryCreate(
            new[] { false, false, true },
            new[] { true, false, true },
            new[] { false, false, true },
            BedOwnerChoiceKind.Guest,
            preflightAllowed: true,
            out BedSelectionConversionPlan guestPlan);

        Assert.Multiple(() =>
        {
            Assert.That(invalidPrisonRoom, Is.False);
            Assert.That(prisonerPlan.SwapIndexes, Is.Empty);
            Assert.That(missingReplacement, Is.False);
            Assert.That(guestPlan.SwapIndexes, Is.Empty);
        });
    }

    [Test]
    public void ConversionFailureRollsBackEveryAttemptInReverseOrder()
    {
        var items = new List<string> { "ordinary-0", "ordinary-1", "untouched" };
        var runtimeState = new[] { "ordinary", "ordinary", "ordinary" };
        var events = new List<string>();

        bool converted = ConversionTransactionExecutor.TryExecute(
            items,
            new[] { 0, 1 },
            (index, original) =>
            {
                runtimeState[index] = "guest";
                events.Add($"convert:{index}");
                if (index == 1) throw new InvalidOperationException("second swap failed after mutation");
                return $"guest-{index}";
            },
            (index, original) =>
            {
                runtimeState[index] = "ordinary";
                events.Add($"rollback:{index}");
                return original;
            },
            out IReadOnlyDictionary<string, string> replacements,
            out string? failure);

        Assert.Multiple(() =>
        {
            Assert.That(converted, Is.False);
            Assert.That(failure, Does.Contain("second swap failed after mutation"));
            Assert.That(runtimeState, Is.EqualTo(new[] { "ordinary", "ordinary", "ordinary" }));
            Assert.That(items, Is.EqualTo(new[] { "ordinary-0", "ordinary-1", "untouched" }));
            Assert.That(events, Is.EqualTo(new[] { "convert:0", "convert:1", "rollback:1", "rollback:0" }));
            Assert.That(replacements["ordinary-0"], Is.EqualTo("ordinary-0"));
            Assert.That(replacements["ordinary-1"], Is.EqualTo("ordinary-1"));
        });
    }

    private static string Metadata(string key) =>
        typeof(GuestAwareCommandAndConversionPlanTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == key)
            .Value;
}
