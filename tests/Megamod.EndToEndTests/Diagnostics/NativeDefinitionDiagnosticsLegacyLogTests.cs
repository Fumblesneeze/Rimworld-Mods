using System.Collections.Generic;
using RimWorldDevGateway.EndToEndTesting;

namespace Megamod.EndToEndTests.Diagnostics;

[RimWorldEndToEndTest("megamod.diagnostics.definition-initial-legacy", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod", "fumblesneeze.megamoddiagnosticsfixture",
    LegacyXmlDeserializer = true, MaxFrames = 2400, MaxGameTicks = 2400, MaxWallClockSeconds = 120)]
public sealed class NativeDefinitionDiagnosticsLegacyLogTests : IRimWorldEndToEndTest
{
    private readonly NativeDefinitionDiagnosticsLogScenario scenario = new(expectedLegacy: true);

    public void Arrange(IEndToEndContext context) => scenario.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => scenario.Execute(context);
}
