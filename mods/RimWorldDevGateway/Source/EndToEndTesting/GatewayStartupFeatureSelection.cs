namespace RimWorldDevGateway;

public sealed class GatewayStartupFeatureSelection
{
    public const string IntegrationFlag = "devGatewayRunIntegrationTests";
    public const string EndToEndFlag = "devGatewayRunEndToEndTests";
    public const string EndToEndTestIdsArgument = "devGatewayEndToEndTestIds";

    private GatewayStartupFeatureSelection(
        bool runIntegrationTests,
        bool runEndToEndTests,
        IReadOnlyList<string> selectedEndToEndTestIds)
    {
        RunIntegrationTests = runIntegrationTests;
        RunEndToEndTests = runEndToEndTests;
        SelectedEndToEndTestIds = selectedEndToEndTestIds;
    }

    public bool RunIntegrationTests { get; }

    public bool RunEndToEndTests { get; }

    public IReadOnlyList<string> SelectedEndToEndTestIds { get; }

    public static GatewayStartupFeatureSelection Capture(
        Func<string, bool> argumentPassed,
        Func<string, string?>? argumentValue = null)
    {
        if (argumentPassed is null)
        {
            throw new ArgumentNullException(nameof(argumentPassed));
        }

        var runEndToEndTests = argumentPassed(EndToEndFlag);
        var selectedIds = NormalizeSelectedTestIds(argumentValue?.Invoke(EndToEndTestIdsArgument));
        if (!runEndToEndTests && selectedIds.Count > 0)
        {
            throw new ArgumentException(
                "An E2E test selection requires the exact E2E startup flag.",
                nameof(argumentValue));
        }

        return new GatewayStartupFeatureSelection(
            argumentPassed(IntegrationFlag),
            runEndToEndTests,
            selectedIds);
    }

    private static IReadOnlyList<string> NormalizeSelectedTestIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        var parsed = value!.Split(',')
            .Select(item => item.Trim())
            .ToArray();
        if (parsed.Any(item => !IsValidTestId(item)))
        {
            throw new ArgumentException("The E2E test selection contains an invalid stable ID.", nameof(value));
        }
        if (parsed.Distinct(StringComparer.Ordinal).Count() != parsed.Length)
        {
            throw new ArgumentException("The E2E test selection contains a duplicate stable ID.", nameof(value));
        }

        return Array.AsReadOnly(parsed.OrderBy(item => item, StringComparer.Ordinal).ToArray());
    }

    private static bool IsValidTestId(string value) =>
        value.Length is > 0 and <= 160 &&
        char.IsLetterOrDigit(value[0]) &&
        value.All(character =>
            char.IsLetterOrDigit(character) ||
            character is '.' or '_' or '-');
}
