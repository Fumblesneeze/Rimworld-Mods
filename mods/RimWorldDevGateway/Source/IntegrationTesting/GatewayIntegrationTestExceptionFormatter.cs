using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
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

    internal static string MessageWithoutVirtualUserCode(Exception exception) =>
        IsTrustedExactExceptionType(exception) ? SafeMessage(exception) : SuppressedMessage;

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

        var type = BoundRaw(
            Redact(SafeTypeName(exception), sessionCredential),
            maximumTypeCharacters);
        var trusted = IsTrustedExactExceptionType(exception);
        var message = trusted
            ? BoundRaw(
                Redact(SafeMessage(exception), sessionCredential),
                maximumMessageCharacters)
            : BoundRaw(
                Redact(SuppressedMessage, sessionCredential),
                maximumMessageCharacters);
        var stackTrace = trusted
            ? BoundRaw(
                Redact(SafeStackTrace(exception), sessionCredential),
                maximumStackCharacters)
            : string.Empty;

        return new GatewayIntegrationTestExceptionDetails(type, message, stackTrace);
    }

    public static string Redact(string? value, string? sessionCredential)
    {
        var safeValue = value ?? string.Empty;
        return string.IsNullOrEmpty(sessionCredential)
            ? safeValue
            : safeValue.Replace(sessionCredential, "[REDACTED]");
    }

    public static string RedactAndBoundUtf8(
        string? value,
        string? sessionCredential,
        int maximumUtf8Bytes)
    {
        if (maximumUtf8Bytes < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumUtf8Bytes));
        }

        var source = value ?? string.Empty;
        var credential = sessionCredential ?? string.Empty;
        var output = new StringBuilder(Math.Min(source.Length, maximumUtf8Bytes));
        var appendedUnits = new List<(int CharacterCount, int Utf8Bytes)>();
        var outputBytes = 0;
        var sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            if (credential.Length > 0 &&
                sourceIndex <= source.Length - credential.Length &&
                string.CompareOrdinal(
                    source,
                    sourceIndex,
                    credential,
                    0,
                    credential.Length) == 0)
            {
                const string marker = "[REDACTED]";
                if (outputBytes + marker.Length > maximumUtf8Bytes)
                {
                    break;
                }

                output.Append(marker);
                appendedUnits.Add((marker.Length, marker.Length));
                outputBytes += marker.Length;
                sourceIndex += credential.Length;
                continue;
            }

            var first = source[sourceIndex];
            var validSurrogatePair = char.IsHighSurrogate(first) &&
                                     sourceIndex + 1 < source.Length &&
                                     char.IsLowSurrogate(source[sourceIndex + 1]);
            var characterCount = validSurrogatePair
                ? 2
                : 1;
            var characterBytes = validSurrogatePair
                ? 4
                : first <= 0x7f
                    ? 1
                    : first <= 0x7ff
                        ? 2
                        : 3;
            if (outputBytes + characterBytes > maximumUtf8Bytes)
            {
                break;
            }

            if (char.IsSurrogate(first) && !validSurrogatePair)
            {
                output.Append('\ufffd');
            }
            else
            {
                output.Append(source, sourceIndex, characterCount);
            }
            outputBytes += characterBytes;
            appendedUnits.Add((characterCount, characterBytes));
            sourceIndex += characterCount;
        }

        if (sourceIndex < source.Length)
        {
            while (outputBytes > maximumUtf8Bytes - 3 && appendedUnits.Count > 0)
            {
                var last = appendedUnits[appendedUnits.Count - 1];
                appendedUnits.RemoveAt(appendedUnits.Count - 1);
                output.Length -= last.CharacterCount;
                outputBytes -= last.Utf8Bytes;
            }

            output.Append("...");
        }

        return output.ToString();
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

    internal static bool IsTrustedExactExceptionType(Exception exception)
    {
        var type = exception.GetType();
        return type == typeof(IntegrationTestAssertionException) ||
               type == typeof(EndToEndAssertionException) ||
               type == typeof(EndToEndContractException) ||
               type == typeof(GatewayGameControlException) ||
               type == typeof(Performance.PerformanceNormalizationException) ||
               type == typeof(InvalidOperationException) ||
               type == typeof(ArgumentException) ||
               type == typeof(ArgumentNullException) ||
               type == typeof(ArgumentOutOfRangeException) ||
               type == typeof(IOException) ||
               type == typeof(DirectoryNotFoundException) ||
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
        if (safeValue.Length <= maximumCharacters)
        {
            return safeValue;
        }

        var prefixLength = maximumCharacters - 3;
        if (prefixLength > 0 &&
            prefixLength < safeValue.Length &&
            char.IsHighSurrogate(safeValue[prefixLength - 1]) &&
            char.IsLowSurrogate(safeValue[prefixLength]))
        {
            prefixLength--;
        }

        return safeValue.Substring(0, prefixLength) + "...";
    }

    private static void ValidateMaximum(int value, string parameterName)
    {
        if (value < 4)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
