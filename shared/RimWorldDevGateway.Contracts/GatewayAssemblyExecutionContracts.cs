using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;

namespace RimWorldDevGateway.Contracts;

public delegate string GatewayAssemblyAutomationHandler(
    string argumentsJson,
    CancellationToken cancellationToken);

public interface IGatewayAssemblyRuntimeExtensions
{
    void RegisterSessionAutomation(
        GatewayAssemblyAutomationDescriptor descriptor,
        GatewayAssemblyAutomationHandler handler);
}

public sealed class GatewayAssemblyAutomationDescriptor
{
    public const int MaximumArgumentSchemaEntries = 64;
    public const int MaximumArgumentSchemaKeyUtf8Bytes = 256;
    public const int MaximumArgumentSchemaValueUtf8Bytes = 4 * 1024;
    public const int MaximumPrerequisites = 64;
    public const int MaximumPrerequisiteUtf8Bytes = 4 * 1024;
    public const int MaximumMetadataUtf8Bytes = 512 * 1024;

    public GatewayAssemblyAutomationDescriptor(
        string name,
        string version,
        string description,
        bool mutating,
        IReadOnlyDictionary<string, string>? argumentSchema = null,
        IEnumerable<string>? prerequisites = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128)
        {
            throw new ArgumentException(
                "An automation name of at most 128 characters is required.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(version) || version.Length > 64)
        {
            throw new ArgumentException(
                "An automation version of at most 64 characters is required.",
                nameof(version));
        }

        if (string.IsNullOrWhiteSpace(description) || description.Length > 2_000)
        {
            throw new ArgumentException(
                "An automation description of at most 2,000 characters is required.",
                nameof(description));
        }

        Name = name;
        Version = version;
        Description = description;
        Mutating = mutating;
        var metadataUtf8Bytes = Utf8Bytes(name) + Utf8Bytes(version) + Utf8Bytes(description);
        var schemaCopy = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in argumentSchema ?? new Dictionary<string, string>())
        {
            if (schemaCopy.Count == MaximumArgumentSchemaEntries)
            {
                throw new ArgumentException(
                    $"An uploaded automation may declare at most {MaximumArgumentSchemaEntries} argument-schema entries.",
                    nameof(argumentSchema));
            }

            var keyBytes = RequireBoundedUtf8(
                entry.Key,
                MaximumArgumentSchemaKeyUtf8Bytes,
                "An argument-schema key",
                nameof(argumentSchema));
            var valueBytes = RequireBoundedUtf8(
                entry.Value,
                MaximumArgumentSchemaValueUtf8Bytes,
                "An argument-schema value",
                nameof(argumentSchema));
            metadataUtf8Bytes = CheckedMetadataTotal(
                metadataUtf8Bytes,
                keyBytes + valueBytes,
                nameof(argumentSchema));
            schemaCopy.Add(entry.Key, entry.Value);
        }

        ArgumentSchema = new ReadOnlyDictionary<string, string>(schemaCopy);
        var prerequisiteCopy = new List<string>();
        foreach (var prerequisite in prerequisites ?? Array.Empty<string>())
        {
            if (prerequisiteCopy.Count == MaximumPrerequisites)
            {
                throw new ArgumentException(
                    $"An uploaded automation may declare at most {MaximumPrerequisites} prerequisites.",
                    nameof(prerequisites));
            }

            var prerequisiteBytes = RequireBoundedUtf8(
                prerequisite,
                MaximumPrerequisiteUtf8Bytes,
                "A prerequisite",
                nameof(prerequisites));
            metadataUtf8Bytes = CheckedMetadataTotal(
                metadataUtf8Bytes,
                prerequisiteBytes,
                nameof(prerequisites));
            prerequisiteCopy.Add(prerequisite);
        }

        Prerequisites = new ReadOnlyCollection<string>(prerequisiteCopy);
    }

    public string Name { get; }

    public string Version { get; }

    public string Description { get; }

    public bool Mutating { get; }

    public IReadOnlyDictionary<string, string> ArgumentSchema { get; }

    public IReadOnlyList<string> Prerequisites { get; }

    private static int RequireBoundedUtf8(
        string? value,
        int maximumUtf8Bytes,
        string label,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(label + " must not be empty.", parameterName);
        }

        var bytes = Utf8Bytes(value!);
        if (bytes > maximumUtf8Bytes)
        {
            throw new ArgumentException(
                $"{label} exceeds the {maximumUtf8Bytes}-byte UTF-8 limit.",
                parameterName);
        }

        return bytes;
    }

    private static int CheckedMetadataTotal(
        int current,
        int added,
        string parameterName)
    {
        var total = checked(current + added);
        if (total > MaximumMetadataUtf8Bytes)
        {
            throw new ArgumentException(
                $"Uploaded automation metadata exceeds the {MaximumMetadataUtf8Bytes}-byte aggregate UTF-8 limit.",
                parameterName);
        }

        return total;
    }

    private static int Utf8Bytes(string value) => Encoding.UTF8.GetByteCount(value);
}

public sealed class GatewayAssemblyExecutionContext
{
    public GatewayAssemblyExecutionContext(
        string requestId,
        CancellationToken cancellationToken,
        IGatewayAssemblyRuntimeExtensions runtimeExtensions)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("A request ID is required.", nameof(requestId));
        }

        RequestId = requestId;
        CancellationToken = cancellationToken;
        RuntimeExtensions = runtimeExtensions ??
            throw new ArgumentNullException(nameof(runtimeExtensions));
    }

    public string RequestId { get; }

    public CancellationToken CancellationToken { get; }

    public IGatewayAssemblyRuntimeExtensions RuntimeExtensions { get; }
}
