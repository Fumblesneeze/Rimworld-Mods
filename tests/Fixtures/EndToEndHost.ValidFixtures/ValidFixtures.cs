using System;
using System.Collections.Generic;
using RimWorldDevGateway.EndToEndTesting;

namespace EndToEndHost.ValidFixtures;

[RimWorldEndToEndTest(
    "alpha.base-a",
    "alpha.mod",
    "ludeon.rimworld",
    "brrainz.harmony",
    "alpha.mod",
    MaxFrames = 300,
    MaxGameTicks = 1_200,
    MaxWallClockSeconds = 30)]
public sealed class ExplosiveStaticConstructorTest : IRimWorldEndToEndTest
{
    static ExplosiveStaticConstructorTest()
    {
        throw new InvalidOperationException("Metadata discovery executed test code.");
    }

    public void Arrange(IEndToEndContext context)
    {
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield break;
    }
}

[RimWorldEndToEndTest(
    "alpha.base-b",
    "alpha.mod",
    "ludeon.rimworld",
    "brrainz.harmony",
    "alpha.mod")]
public sealed class SecondBaseTest : IRimWorldEndToEndTest
{
    public void Arrange(IEndToEndContext context)
    {
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield break;
    }
}

[RimWorldEndToEndTest(
    "alpha.optional",
    "alpha.mod",
    "ludeon.rimworld",
    "brrainz.harmony",
    "alpha.mod",
    "optional.mod")]
public sealed class OptionalTest : IRimWorldEndToEndTest
{
    public void Arrange(IEndToEndContext context)
    {
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield break;
    }
}
