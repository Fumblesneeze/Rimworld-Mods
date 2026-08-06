using System.Xml;
using Verse;

namespace ImmersiveChefs;

/// <summary>
/// Applies optional texture ownership only after the exact VTEX/VEF integration
/// and the inspected VEF building-component shape have both been validated.
/// </summary>
public sealed class PatchOperationTextureVariations : PatchOperation
{
    private PatchOperation? match = null!;

    protected override bool ApplyWorker(XmlDocument xml)
    {
        if (!ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.TextureVariations))
        {
            return true;
        }

        if (!TextureVariationAdapter.TryValidateBuildingShape(out var reason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(
                OptionalIntegration.TextureVariations,
                reason);
            return true;
        }

        if (match is null)
        {
            Log.Error(
                "[ImmersiveChefs] Texture variation patch is missing its nested match operation.");
            return false;
        }

        return match.Apply(xml);
    }
}
