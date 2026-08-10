using System.Collections.Generic;
using RimWorldDevGateway.EndToEndTesting;

namespace EndToEndHost.InvalidFixtures;

[RimWorldEndToEndTest(
    "invalid.owner",
    "alpha.mod",
    "brrainz.harmony",
    "ludeon.rimworld",
    "another.mod")]
public sealed class MissingOwnerTest : NoOpTest
{
}

[RimWorldEndToEndTest(
    "invalid.harmony-order",
    "alpha.mod",
    "ludeon.rimworld",
    "brrainz.harmony",
    "alpha.mod")]
public sealed class CoreBeforeHarmonyTest : NoOpTest
{
}

[RimWorldEndToEndTest(
    "invalid.duplicate-package",
    "alpha.mod",
    "ludeon.rimworld",
    "alpha.mod",
    "ALPHA.MOD")]
public sealed class DuplicatePackageTest : NoOpTest
{
}

[RimWorldEndToEndTest(
    "invalid.explicit-gateway",
    "alpha.mod",
    "ludeon.rimworld",
    "alpha.mod",
    "fumblesneeze.rimworlddevgateway")]
public sealed class ExplicitGatewayTest : NoOpTest
{
}

[RimWorldEndToEndTest(
    "invalid.abstract",
    "alpha.mod",
    "ludeon.rimworld",
    "alpha.mod")]
public abstract class AbstractTest : NoOpTest
{
}

[RimWorldEndToEndTest(
    "invalid.interface",
    "alpha.mod",
    "ludeon.rimworld",
    "alpha.mod")]
public sealed class MissingInterfaceTest
{
}

[RimWorldEndToEndTest(
    "invalid.deadline",
    "alpha.mod",
    "ludeon.rimworld",
    "alpha.mod",
    MaxFrames = 0,
    MaxGameTicks = 0,
    MaxWallClockSeconds = 0)]
public sealed class InvalidDeadlineTest : NoOpTest
{
}

[RimWorldEndToEndTest(
    "invalid.duplicate-id",
    "alpha.mod",
    "ludeon.rimworld",
    "alpha.mod")]
public sealed class DuplicateIdOneTest : NoOpTest
{
}

[RimWorldEndToEndTest(
    "invalid.duplicate-id",
    "alpha.mod",
    "ludeon.rimworld",
    "alpha.mod")]
public sealed class DuplicateIdTwoTest : NoOpTest
{
}

public abstract class NoOpTest : IRimWorldEndToEndTest
{
    public void Arrange(IEndToEndContext context)
    {
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield break;
    }
}
