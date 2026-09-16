namespace RimWorldDevGateway;

internal static class GatewayExceptionCorrelation
{
    internal static bool CanNormalize(Exception exception)
    {
        for (var depth = 0; depth < 9; depth++)
        {
            if (!GatewayIntegrationTestExceptionFormatter.IsTrustedExactExceptionType(exception)) return false;
            if (exception.InnerException is null) return true;
            exception = exception.InnerException;
        }
        return false;
    }

    internal static string CanonicalMessage(string rendered, string message, string canonical) =>
        message.Replace(rendered, canonical);

    internal static string SafeMessage(Exception exception) =>
        GatewayIntegrationTestExceptionFormatter.MessageWithoutVirtualUserCode(exception);

    // Only a whole rendered exception (including its stack) associates live CLR frames with a log.
    // Merely inspecting StackTrace or sharing a word with another message is not an association.
    internal static bool Matches(string rendered, string message) =>
        rendered.Length is > 0 and <= 65536 && rendered.IndexOf('\n') >= 0 &&
        message.IndexOf(rendered, StringComparison.Ordinal) >= 0;
}
