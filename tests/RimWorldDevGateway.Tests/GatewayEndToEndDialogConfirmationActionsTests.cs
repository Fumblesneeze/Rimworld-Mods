using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndDialogConfirmationActionsTests
{
    [Test]
    public void Exact_private_callback_and_argument_fields_prepare_the_native_confirmation()
    {
        var capturedCount = 0;
        var capturedDefName = string.Empty;
        var dialog = new FakeConfirmationDialog((count, defName) =>
        {
            capturedCount = count;
            capturedDefName = defName;
        });
        var shape = Shape(
            typeof(FakeConfirmationDialog),
            "curValue",
            "selectedSurvivalMealType");

        var prepared = VerseGatewayEndToEndDialogConfirmationActions.TryPrepare(
            dialog,
            shape,
            out var callback,
            out var arguments,
            out var failureMessage);
        Assert.That(prepared, Is.True, failureMessage);
        callback!.DynamicInvoke(arguments);

        Assert.Multiple(() =>
        {
            Assert.That(capturedCount, Is.EqualTo(3));
            Assert.That(capturedDefName, Is.EqualTo("MealSurvivalPack"));
        });
    }

    [Test]
    public void Argument_shape_drift_fails_before_the_callback_can_run()
    {
        var invoked = false;
        var dialog = new FakeConfirmationDialog((_, _) => invoked = true);
        var shape = Shape(
            typeof(FakeConfirmationDialog),
            "selectedSurvivalMealType",
            "curValue");

        var prepared = VerseGatewayEndToEndDialogConfirmationActions.TryPrepare(
            dialog,
            shape,
            out _,
            out _,
            out var failureMessage);

        Assert.Multiple(() =>
        {
            Assert.That(prepared, Is.False);
            Assert.That(invoked, Is.False);
            Assert.That(failureMessage, Does.Contain("parameter type"));
        });
    }

    [Test]
    public void Result_returning_delegate_is_not_treated_as_a_confirmation_button()
    {
        var dialog = new FakeResultDialog(value => value.ToString());
        var shape = Shape(typeof(FakeResultDialog), "curValue");

        var prepared = VerseGatewayEndToEndDialogConfirmationActions.TryPrepare(
            dialog,
            shape,
            out _,
            out _,
            out var failureMessage);

        Assert.Multiple(() =>
        {
            Assert.That(prepared, Is.False);
            Assert.That(failureMessage, Does.Contain("void-returning"));
        });
    }

    [Test]
    public void Unregistered_window_type_has_no_generic_reflection_fallback()
    {
        var resolved = VerseGatewayEndToEndDialogConfirmationActions.TryResolveShape(
            typeof(FakeConfirmationDialog),
            out var shape);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.False);
            Assert.That(shape, Is.Null);
        });
    }

    private static GatewayDialogConfirmationShape Shape(Type type, params string[] arguments) =>
        new(
            type.FullName!,
            type.Assembly.FullName!,
            "confirmAction",
            arguments);

    private sealed class FakeConfirmationDialog
    {
        private readonly Action<int, string> confirmAction;
        private readonly int curValue = 3;
        private readonly string selectedSurvivalMealType = "MealSurvivalPack";

        public FakeConfirmationDialog(Action<int, string> confirmAction) =>
            this.confirmAction = confirmAction;
    }

    private sealed class FakeResultDialog
    {
        private readonly Func<int, string> confirmAction;
        private readonly int curValue = 3;

        public FakeResultDialog(Func<int, string> confirmAction) =>
            this.confirmAction = confirmAction;
    }
}
