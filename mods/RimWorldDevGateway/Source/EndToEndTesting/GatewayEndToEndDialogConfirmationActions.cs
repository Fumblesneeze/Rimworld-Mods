using System.Reflection;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway;

internal sealed class GatewayDialogConfirmationShape
{
    internal GatewayDialogConfirmationShape(
        string windowTypeName,
        string assemblyFullName,
        string callbackFieldName,
        params string[] argumentFieldNames)
    {
        WindowTypeName = windowTypeName;
        AssemblyFullName = assemblyFullName;
        CallbackFieldName = callbackFieldName;
        ArgumentFieldNames = argumentFieldNames;
    }

    internal string WindowTypeName { get; }

    internal string AssemblyFullName { get; }

    internal string CallbackFieldName { get; }

    internal IReadOnlyList<string> ArgumentFieldNames { get; }
}

internal static class VerseGatewayEndToEndDialogConfirmationActions
{
    private static readonly GatewayDialogConfirmationShape ReplimatSurvivalBatch = new(
        "Replimat.Dialog_BatchMakeSurvivalMeals",
        "Replimat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        "confirmAction",
        "curValue",
        "selectedSurvivalMealType");

    internal static GatewayEndToEndStepOutcome Apply(DialogConfirmationActionStep step)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        var matches = Find.WindowStack?.Windows
            .Where(window => string.Equals(
                window.GetType().FullName,
                step.ExpectedWindowTypeName,
                StringComparison.Ordinal))
            .ToArray() ?? Array.Empty<Window>();
        if (matches.Length == 0)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "dialog_not_found",
                "No open window matched the declared exact runtime type.");
        }

        if (matches.Length != 1)
        {
            return GatewayEndToEndStepOutcome.Fail(
                "ambiguous_dialog",
                "More than one open window matched the declared exact runtime type.");
        }

        if (!TryResolveShape(matches[0].GetType(), out var shape))
        {
            return GatewayEndToEndStepOutcome.Fail(
                "dialog_confirmation_unsupported",
                "The exact open window does not have a registered semantic confirmation adapter.");
        }

        if (!TryPrepare(
                matches[0],
                shape!,
                out var callback,
                out var arguments,
                out var failureMessage))
        {
            return GatewayEndToEndStepOutcome.Fail(
                "dialog_confirmation_shape_mismatch",
                failureMessage);
        }

        try
        {
            // Match the normal button path: the window closes before its validated callback runs.
            matches[0].Close(doCloseSound: true);
            callback!.DynamicInvoke(arguments);
            return GatewayEndToEndStepOutcome.Pass(
                new Dictionary<string, string>
                {
                    ["windowType"] = step.ExpectedWindowTypeName
                });
        }
        catch (Exception exception)
        {
            var cause = exception is TargetInvocationException { InnerException: not null }
                ? exception.InnerException
                : exception;
            return GatewayEndToEndStepOutcome.Fail(
                "dialog_confirmation_failed",
                "The validated dialog confirmation path threw " + cause!.GetType().Name + ".");
        }
    }

    internal static bool TryResolveShape(
        Type runtimeType,
        out GatewayDialogConfirmationShape? shape)
    {
        if (runtimeType is null)
        {
            throw new ArgumentNullException(nameof(runtimeType));
        }

        shape = ReplimatSurvivalBatch;
        if (string.Equals(runtimeType.FullName, shape.WindowTypeName, StringComparison.Ordinal) &&
            string.Equals(runtimeType.Assembly.FullName, shape.AssemblyFullName, StringComparison.Ordinal))
        {
            return true;
        }

        shape = null;
        return false;
    }

    internal static bool TryPrepare(
        object window,
        GatewayDialogConfirmationShape shape,
        out Delegate? callback,
        out object?[] arguments,
        out string failureMessage)
    {
        if (window is null)
        {
            throw new ArgumentNullException(nameof(window));
        }

        if (shape is null)
        {
            throw new ArgumentNullException(nameof(shape));
        }

        callback = null;
        arguments = Array.Empty<object?>();
        failureMessage = string.Empty;
        var runtimeType = window.GetType();
        if (!string.Equals(runtimeType.FullName, shape.WindowTypeName, StringComparison.Ordinal) ||
            !string.Equals(runtimeType.Assembly.FullName, shape.AssemblyFullName, StringComparison.Ordinal))
        {
            failureMessage = "The candidate window no longer has the registered exact type and assembly identity.";
            return false;
        }

        var callbackField = FindInstanceField(runtimeType, shape.CallbackFieldName);
        callback = callbackField?.GetValue(window) as Delegate;
        if (callback is null)
        {
            failureMessage = "The registered confirmation field is missing or does not contain a delegate.";
            return false;
        }

        var invoke = callback.GetType().GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);
        if (invoke?.ReturnType != typeof(void))
        {
            failureMessage = "The registered confirmation field does not contain a void-returning callback.";
            return false;
        }

        var parameters = invoke.GetParameters();
        if (parameters.Length != shape.ArgumentFieldNames.Count)
        {
            failureMessage = "The confirmation delegate parameter count does not match the registered argument fields.";
            return false;
        }

        arguments = new object?[parameters.Length];
        for (var index = 0; index < parameters.Length; index++)
        {
            var argumentField = FindInstanceField(runtimeType, shape.ArgumentFieldNames[index]);
            if (argumentField is null)
            {
                failureMessage = "A registered confirmation argument field is missing.";
                return false;
            }

            var value = argumentField.GetValue(window);
            var parameterType = parameters[index].ParameterType;
            if (value is null)
            {
                if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
                {
                    failureMessage = "A registered confirmation argument is null for a non-nullable parameter.";
                    return false;
                }
            }
            else if (!parameterType.IsInstanceOfType(value))
            {
                failureMessage = "A registered confirmation argument does not match its delegate parameter type.";
                return false;
            }

            arguments[index] = value;
        }

        return true;
    }

    private static FieldInfo? FindInstanceField(Type runtimeType, string fieldName)
    {
        for (var current = runtimeType; current is not null; current = current.BaseType)
        {
            var field = current.GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (field is not null)
            {
                return field;
            }
        }

        return null;
    }
}
