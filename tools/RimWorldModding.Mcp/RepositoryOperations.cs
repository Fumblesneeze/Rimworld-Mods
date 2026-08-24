namespace RimWorldModding.Mcp;

public sealed record AdapterOperationResult(
    string Operation,
    int ExitCode,
    double DurationSeconds,
    string StandardOutput,
    string StandardError,
    string? EvidenceRoot);

public static class RepositoryOperations
{
    public static async Task<AdapterOperationResult> ExecuteAsync(
        AdapterCommand command,
        CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync(
            command.FileName,
            command.Arguments,
            command.WorkingDirectory,
            command.Timeout,
            cancellationToken);
        var operationResult = new AdapterOperationResult(
            command.Kind,
            result.ExitCode,
            result.Duration.TotalSeconds,
            Bound(result.StandardOutput),
            Bound(result.StandardError),
            command.EvidenceRoot);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"{command.Kind} failed with exit code {result.ExitCode}: {Bound(result.StandardError + result.StandardOutput)}");
        return operationResult;
    }

    private static string Bound(string value) =>
        value.Length <= 65536 ? value.Trim() : value[^65536..].Trim();
}
