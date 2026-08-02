using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

public sealed class RimWorldDevGatewayMod : Mod
{
    public const string PackageId = "fumblesneeze.rimworlddevgateway";

    public const string DangerWarning =
        "DEVELOPER-ONLY: authenticated loopback callers can execute unrestricted code in the RimWorld process.";

    public RimWorldDevGatewayMod(ModContentPack content) : base(content)
    {
        Log.Warning($"[RimWorldDevGateway] {DangerWarning}");
        GatewayRuntimeBootstrap.Schedule(
            content,
            (bearerToken, handler) => new GatewayLoopbackServer(bearerToken, handler));
    }

    public override string SettingsCategory() => "RimWorld Dev Gateway — DANGER";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.Label(DangerWarning);
        listing.Gap();
        listing.Label(
            "Unrestricted execution is always enabled while this mod is loaded. " +
            "The listener is restricted to authenticated IPv4 loopback requests; there is intentionally no safety toggle.");
        listing.End();
    }
}
