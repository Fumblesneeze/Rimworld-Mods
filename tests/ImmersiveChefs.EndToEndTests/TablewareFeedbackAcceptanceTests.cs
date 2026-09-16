using System.Collections.Generic;
using RimWorldDevGateway.EndToEndTesting;
namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.tableware-feedback-acceptance", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 30000, MaxGameTicks = 60000, MaxWallClockSeconds = 600)]
public sealed class TablewareFeedbackAcceptanceTest : IRimWorldEndToEndTest
{
    private readonly MixedTablewareScenario cutlery = new("ImmersiveChefs_Cutlery");
    public void Arrange(IEndToEndContext context) => cutlery.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in cutlery.Execute(context)) yield return step;
        var crafting = new UngradedTablewareCraftTest();
        crafting.Arrange(context);
        using (var steps = crafting.Execute(context)) while (steps.MoveNext()) yield return steps.Current;
        var dining = new MixedTablewareDiningTest();
        dining.Arrange(context);
        using (var steps = dining.Execute(context)) while (steps.MoveNext()) yield return steps.Current;
    }
}
