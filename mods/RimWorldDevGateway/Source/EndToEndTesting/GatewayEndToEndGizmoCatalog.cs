using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndGizmoCatalog : IEndToEndGizmoCatalog
{
    private readonly GatewayGizmoRegistry gizmos;

    public GatewayEndToEndGizmoCatalog(GatewayGizmoRegistry gizmos)
    {
        this.gizmos = gizmos ?? throw new ArgumentNullException(nameof(gizmos));
    }

    public IReadOnlyList<EndToEndGizmoOption> Query(
        IReadOnlyList<string> targetRuntimeIds,
        IReadOnlyList<string> architectCategoryDefNames)
    {
        if (targetRuntimeIds is null)
        {
            throw new ArgumentNullException(nameof(targetRuntimeIds));
        }

        if (architectCategoryDefNames is null)
        {
            throw new ArgumentNullException(nameof(architectCategoryDefNames));
        }

        var descriptors = gizmos.Query(GatewayGizmoQuery.ForOwners(
            targetRuntimeIds,
            architectCategoryDefNames: architectCategoryDefNames)).Items;
        return Project(descriptors);
    }

    internal static IReadOnlyList<EndToEndGizmoOption> Project(
        IEnumerable<GatewayGizmoDescriptor> descriptors)
    {
        if (descriptors is null)
        {
            throw new ArgumentNullException(nameof(descriptors));
        }

        return descriptors.Select(descriptor => new EndToEndGizmoOption(
                descriptor.Identity,
                descriptor.RuntimeType,
                descriptor.Label,
                descriptor.Disabled,
                MapInteraction(descriptor.InteractionKind),
                descriptor.BuildableDefName,
                descriptor.ToggleState,
                descriptor.HotKey))
            .ToArray();
    }

    private static EndToEndGizmoInteraction? MapInteraction(GatewayGizmoInteractionKind interaction) =>
        interaction switch
        {
            GatewayGizmoInteractionKind.Immediate => EndToEndGizmoInteraction.Invoke,
            GatewayGizmoInteractionKind.Toggle => EndToEndGizmoInteraction.Toggle,
            GatewayGizmoInteractionKind.Placement => EndToEndGizmoInteraction.Place,
            GatewayGizmoInteractionKind.Drag => EndToEndGizmoInteraction.Drag,
            _ => null
        };
}
