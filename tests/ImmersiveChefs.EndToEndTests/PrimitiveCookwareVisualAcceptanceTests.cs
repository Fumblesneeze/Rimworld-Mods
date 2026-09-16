using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.primitive-cookware-visual-base", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 10000, MaxGameTicks = 30000, MaxWallClockSeconds = 330)]
public sealed class PrimitiveCookwareVisualBaseTest : IRimWorldEndToEndTest
{
    private readonly CookwareVisualFixture fixture = new(false, true);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

[RimWorldEndToEndTest("immersive-chefs.primitive-cookware-visual-vtex", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions",
    "OskarPotocki.VanillaFactionsExpanded.Core", "VanillaExpanded.VTEXVariations", "fumblesneeze.immersivechefs",
    MaxFrames = 10000, MaxGameTicks = 30000, MaxWallClockSeconds = 330)]
public sealed class PrimitiveCookwareVisualVtexTest : IRimWorldEndToEndTest
{
    private readonly CookwareVisualFixture fixture = new(true, true);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}
