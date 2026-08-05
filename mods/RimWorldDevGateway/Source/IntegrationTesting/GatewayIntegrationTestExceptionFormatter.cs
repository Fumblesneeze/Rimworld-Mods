using System;
using System.IO;
using System.Reflection;
using RimWorldDevGateway.IntegrationTesting;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

internal readonly struct GatewayIntegrationTestExceptionDetails
{
    public GatewayIntegrationTestExceptionDetails(string type, string message, string stackTrace)
    {
        Type = type;
        Message = message;
        StackTrace = stackTrace;
    }

    public string Type { get; }

    public string Message { get; }

    public string StackTrace { get; }
}

internal static class GatewayIntegrationTestExceptionFormatter
{
    private const string SuppressedMessage = "Exception details suppressed for an untrusted exception type.";

    public static GatewayIntegrationTestExceptionDetails Format(
        Exception exception,
        string? sessionCredential = null,
        int maximumTypeCharacters = 256,
        int maximumMessageCharacters = 256,
        int maximumStackCharacters = 1024)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        ValidateMaximum(maximumTypeCharacters, nameof(maximumTypeCharacters));
        ValidateMaximum(maximumMessageCharacters, nameof(maximumMessageCharacters));
        ValidateMaximum(maximumStackCharacters, nameof(maximumStackCharacters));

        var type = BoundRaw(SafeTypeName(exception), maximumTypeCharacters);
        var trusted = IsTrustedExactExceptionType(exception);
        var message = trusted
            ? BoundRaw(SafeMessage(exception), maximumMessageCharacters)
            : BoundRaw(SuppressedMessage, maximumMessageCharacters);
        var stackTrace = trusted
            ? BoundRaw(SafeStackTrace(exception), maximumStackCharacters)
            : string.Empty;

        return new GatewayIntegrationTestExceptionDetails(
            Redact(type, sessionCredential),
            Redact(message, sessionCredential),
            Redact(stackTrace, sessionCredential));
    }

    public static string Redact(string? value, string? sessionCredential)
    {
        var safeValue = value ?? string.Empty;
        return string.IsNullOrEmpty(sessionCredential)
            ? safeValue
            : safeValue.Replace(sessionCredential, "[REDACTED]");
    }

    private static string SafeTypeName(Exception exception)
    {
        try
        {
            var type = exception.GetType();
            if (!ReferenceEquals(type.GetType(), typeof(Exception).GetType()))
            {
                return "System.Exception";
            }

            return type.FullName ?? type.Name ?? "System.Exception";
        }
        catch
        {
            return "System.Exception";
        }
    }

    private static bool IsTrustedExactExceptionType(Exception exception)
    {
        var type = exception.GetType();
        return type == typeof(IntegrationTestAssertionException) ||
               type == typeof(EndToEndAssertionException) ||
               type == typeof(EndToEndContractException) ||
               type == typeof(GatewayGameControlException) ||
               type == typeof(InvalidOperationException) ||
               type == typeof(ArgumentException) ||
               type == typeof(ArgumentNullException) ||
               type == typeof(ArgumentOutOfRangeException) ||
               type == typeof(IOException) ||
               type == typeof(FileNotFoundException) ||
               type == typeof(FileLoadException) ||
               type == typeof(BadImageFormatException) ||
               type == typeof(TypeLoadException) ||
               type == typeof(ReflectionTypeLoadException);
    }

    private static string SafeMessage(Exception exception)
    {
        try
        {
            var message = exception.Message;
            return string.IsNullOrWhiteSpace(message)
                ? "Exception message unavailable."
                : message;
        }
        catch
        {
            return "Exception message unavailable.";
        }
    }

    private static string SafeStackTrace(Exception exception)
    {
        try
        {
            return exception.StackTrace ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string BoundRaw(string? value, int maximumCharacters)
    {
        var safeValue = value ?? string.Empty;
        return safeValue.Length <= maximumCharacters
            ? safeValue
            : safeValue.Substring(0, maximumCharacters - 3) + "...";
    }

    private static void ValidateMaximum(int value, string parameterName)
    {
        if (value < 4)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
