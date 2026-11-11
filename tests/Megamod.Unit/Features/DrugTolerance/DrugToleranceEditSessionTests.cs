using System.Globalization;
using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugTolerance;

[TestFixture]
public sealed class DrugToleranceEditSessionTests
{
    [Test]
    public void ApplyRechecksTheExactCurrentContextBeforeWriting()
    {
        var saved = DrugToleranceChoice.Any;
        bool current = true;
        using var editor = new DrugToleranceEditSession(saved, CultureInfo.InvariantCulture, () => current, choice => saved = choice);
        editor.Draft.Any = false; editor.Draft.Percentage = "25";
        current = false;
        Assert.That(editor.TryApply(), Is.False);
        Assert.That(saved, Is.SameAs(DrugToleranceChoice.Any));
    }

    [Test]
    public void CancelRetainsInvalidRawChoiceAndClosesTheMutationCallback()
    {
        var saved = DrugToleranceChoice.Decode("future-version:kept");
        var original = saved;
        var editor = new DrugToleranceEditSession(saved, CultureInfo.InvariantCulture, () => true, choice => saved = choice);
        editor.Draft.Any = true;
        editor.Dispose();
        Assert.That(editor.TryApply(), Is.False);
        Assert.That(editor.CanApply, Is.False);
        Assert.That(saved, Is.SameAs(original));
    }

    [Test]
    public void ExplicitRepairAppliesOnceAndRejectsReentry()
    {
        var saved = DrugToleranceChoice.Decode("invalid");
        DrugToleranceEditSession? editor = null;
        int writes = 0; bool reentered = true;
        editor = new(saved, CultureInfo.GetCultureInfo("de-DE"), () => true, choice =>
        { writes++; saved = choice; reentered = editor!.TryApply(); });
        using (editor)
        {
            editor.Draft.Percentage = "25,5";
            Assert.That(editor.TryApply(), Is.True);
            Assert.That(saved.Limit, Is.EqualTo(0.255f));
            Assert.That(reentered, Is.False); Assert.That(writes, Is.EqualTo(1));
            Assert.That(editor.TryApply(), Is.False);
        }
    }

    [Test]
    public void InvalidDraftStaysOpenForCorrectionWithoutWriting()
    {
        var saved = DrugToleranceChoice.Numeric(0.2f);
        using var editor = new DrugToleranceEditSession(saved, CultureInfo.InvariantCulture, () => true, choice => saved = choice);
        editor.Draft.Percentage = "NaN";
        Assert.That(editor.CanApply, Is.False); Assert.That(editor.TryApply(), Is.False);
        Assert.That(saved.Limit, Is.EqualTo(0.2f));
        editor.Draft.Percentage = "100";
        Assert.That(editor.TryApply(), Is.True);
        Assert.That(saved.Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit)); Assert.That(saved.Limit, Is.EqualTo(1));
    }
}
