using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace Megamod.EndToEndTests.Diagnostics;

// Supporting actual-loader evidence only. This deliberately does not claim native log UI/reload acceptance.
[RimWorldEndToEndTest("megamod.diagnostics.initial-xml-preflight", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod", "fumblesneeze.megamoddiagnosticsfixture",
    MaxFrames = 600, MaxGameTicks = 600, MaxWallClockSeconds = 60)]
public sealed class InitialDefinitionDiagnosticsEvidence : IRimWorldEndToEndTest
{
    private string[] records = Array.Empty<string>();
    public void Arrange(IEndToEndContext context)
    {
        // No Defs are manufactured and no diagnostic is emitted here: these must come from real XML loading.
        records = Log.Messages.Select(message => message.text)
            .Where(text => text.Contains("MegamodDiagnosticsFixture", StringComparison.Ordinal)).ToArray();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new AssertionStep("real XML loader retained distinct current-definition contexts", _ =>
        {
            foreach (string name in new[] { "MegamodDiagnosticsFixtureFirst", "MegamodDiagnosticsFixtureSecond", "MegamodDiagnosticsFixtureMissingType" })
                EndToEndAssert.True(records.Any(text => text.Contains("Definition: " + name, StringComparison.Ordinal) &&
                    text.Contains("Package: fumblesneeze.megamoddiagnosticsfixture", StringComparison.Ordinal)),
                    "Missing actual loader context for " + name);
        });
        yield return new CheckpointStep("supporting initial XML diagnostic records", _ =>
            records.Select((text, index) => new KeyValuePair<string, string>("record-" + index, text))
                .ToDictionary(pair => pair.Key, pair => pair.Value));
        yield return new ScreenshotStep("current native view after XML diagnostics; supporting evidence only", Array.Empty<string>(), 0);
    }
}
