using System.Collections.Generic;
using System.Text;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway;

public sealed class GatewayAssemblyRuntimeExtensions : IGatewayAssemblyRuntimeExtensions
{
    private const int MaximumAutomationArgumentsUtf8Bytes = 1024 * 1024;
    private readonly GatewayAutomationRegistry automations;

    public GatewayAssemblyRuntimeExtensions(GatewayAutomationRegistry automations)
    {
        this.automations = automations ?? throw new ArgumentNullException(nameof(automations));
    }

    public void RegisterSessionAutomation(
        GatewayAssemblyAutomationDescriptor descriptor,
        GatewayAssemblyAutomationHandler handler)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var schema = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var entry in descriptor.ArgumentSchema)
        {
            schema.Add(entry.Key, entry.Value);
        }

        automations.RegisterSession(
            new GatewayAutomationDescriptor(
                descriptor.Name,
                descriptor.Version,
                descriptor.Description,
                schema,
                descriptor.Prerequisites,
                descriptor.Mutating),
            (context, arguments) =>
            {
                var argumentsJson = Encoding.UTF8.GetString(GatewayJsonWriter.Write(
                    arguments,
                    maxDepth: 16,
                    maxNodes: 16 * 1024,
                    maxUtf8Bytes: MaximumAutomationArgumentsUtf8Bytes));
                var value = handler(argumentsJson, context.CancellationToken) ?? string.Empty;
                return GatewayAssemblyExecutor.NormalizeResult(
                    value,
                    GatewayAssemblyExecutor.DefaultMaximumResultUtf8Bytes).Value;
            });
    }
}
