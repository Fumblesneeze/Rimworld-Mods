using Verse;

namespace ImmersiveChefs;

/// <summary>
/// Harmless marker used only to prove that RimWorld applied this mod's XML patch pipeline.
/// It has no dependency on, or reference to, the separate development gateway.
/// </summary>
public sealed class ImmersiveChefsIntegrationProbeExtension : DefModExtension
{
    public string marker = string.Empty;
}
