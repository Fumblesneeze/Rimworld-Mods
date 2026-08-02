using Verse;

namespace RimWorldDevGateway;

/// <summary>
/// A harmless marker attached to Core Steel by the Gateway's XML patch. The marker gives the
/// startup integration fixture a real finalized PatchOperation result to verify.
/// </summary>
public sealed class GatewayIntegrationProbeExtension : DefModExtension
{
    public string marker = string.Empty;
}
