using System;
using System.Collections.Generic;
using RimWorldDevGateway.EndToEndTesting;

namespace EndToEndHost.ValidFixtures;

[RimWorldEndToEndTest("alpha.legacy", "alpha.mod", "brrainz.harmony", "ludeon.rimworld", "alpha.mod",
    LegacyXmlDeserializer = true)]
public sealed class LegacyLoaderTest : IRimWorldEndToEndTest
{
    static LegacyLoaderTest() => throw new InvalidOperationException("Metadata inspection executed the legacy fixture.");
    public void Arrange(IEndToEndContext context) { }
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) { yield break; }
}
