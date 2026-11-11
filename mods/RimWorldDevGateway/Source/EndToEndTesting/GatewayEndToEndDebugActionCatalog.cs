using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndDebugActionCatalog : IEndToEndDebugActionCatalog
{
    private readonly GatewayDebugActionRegistry registry;
    public GatewayEndToEndDebugActionCatalog(GatewayDebugActionRegistry registry)
        => this.registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public EndToEndDebugActionPage Query(string? search = null, IReadOnlyList<string>? categories = null,
        string? after = null, int limit = 100)
    {
        var page = registry.Query(new GatewayDebugActionQuery(search, categories,
            new[] { GatewayDebugActionMode.Immediate }, after, limit));
        return new(page.Items.Select(item => new EndToEndDebugAction(item.Handle, item.Path, item.Label,
            item.Category, item.SourceType, item.AllowedGameStates, item.Visible, item.Active, item.DiscoveryError)),
            page.PageTruncated);
    }
}
