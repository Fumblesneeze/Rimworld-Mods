using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using NUnit.Framework;

namespace ImmersiveSignalFire.Defs.Tests;

[TestFixture]
public sealed class SignalFireSourceContractTests
{
    [Test]
    public void BuildingIsColdTwoByTwoAndResearchGated()
    {
        XElement def = XDocument.Load(SourcePath("Defs", "ThingDefs", "SignalFire.xml"))
            .Root!.Elements("ThingDef")
            .Single(candidate => (string?)candidate.Element("defName") == "ImmersiveSignalFire_SignalFire");
        string[] compClasses = def.Element("comps")?.Elements("li")
            .Select(element => (string?)element.Attribute("Class") ?? (string?)element.Element("compClass") ?? string.Empty)
            .ToArray() ?? System.Array.Empty<string>();

        Assert.Multiple(() =>
        {
            Assert.That((string?)def.Element("thingClass"), Is.EqualTo("ImmersiveSignalFire.Buildings.Building_SignalFire"));
            Assert.That((string?)def.Element("size"), Is.EqualTo("(2,2)"));
            Assert.That((string?)def.Element("graphicData")?.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)def.Element("graphicData")?.Element("texPath"),
                Is.EqualTo("ImmersiveSignalFire/Things/Building/SignalFire/SignalFire"));
            Assert.That((string?)def.Element("tickerType"), Is.EqualTo("Normal"));
            Assert.That((string?)def.Element("passability"), Is.EqualTo("PassThroughOnly"));
            Assert.That(def.Element("costStuffCount"), Is.Null);
            Assert.That(def.Element("stuffCategories"), Is.Null);
            Assert.That((string?)def.Element("costList")?.Element("WoodLog"), Is.EqualTo("60"));
            Assert.That((string?)def.Element("statBases")?.Element("WorkToBuild"), Is.EqualTo("400"));
            Assert.That((string?)def.Element("statBases")?.Element("MaxHitPoints"), Is.EqualTo("120"));
            Assert.That(def.Element("researchPrerequisites")?.Elements("li").Select(item => item.Value),
                Is.EqualTo(new[] { "ImmersiveSignalFire_SmokeSignals" }));
            Assert.That(compClasses, Is.EqualTo(new[] { "ImmersiveSignalFire.Buildings.CompProperties_SignalFire" }));
            Assert.That(compClasses.Any(value => value.Contains("Glower") ||
                                                 value.Contains("Refuelable") ||
                                                 value.Contains("HeatPusher") ||
                                                 value.Contains("FireOverlay")), Is.False);
        });
    }

    [Test]
    public void ResearchAndJobsPinRequestedTimingAndTechnology()
    {
        XElement research = XDocument.Load(SourcePath("Defs", "ResearchProjectDefs", "SmokeSignals.xml"))
            .Root!.Element("ResearchProjectDef")!;
        XElement[] jobs = XDocument.Load(SourcePath("Defs", "JobDefs", "SignalFireJobs.xml"))
            .Root!.Elements("JobDef").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That((string?)research.Element("defName"), Is.EqualTo("ImmersiveSignalFire_SmokeSignals"));
            Assert.That((string?)research.Element("baseCost"), Is.EqualTo("500"));
            Assert.That((string?)research.Element("techLevel"), Is.EqualTo("Neolithic"));
            Assert.That(jobs.Select(job => (string?)job.Element("defName")), Is.EqualTo(new[]
            {
                "ImmersiveSignalFire_Approach",
                "ImmersiveSignalFire_Signal",
            }));
            Assert.That(jobs.Select(job => (string?)job.Element("driverClass")), Is.EqualTo(new[]
            {
                "ImmersiveSignalFire.Jobs.JobDriver_ApproachSignalFire",
                "ImmersiveSignalFire.Jobs.JobDriver_SignalFire",
            }));
        });
    }

    [Test]
    public void AboutDeclaresHardDependenciesAndOptionalOrderingOnly()
    {
        XDocument about = XDocument.Load(SourcePath("Build", "ReviewedAbout.xml"));
        string[] dependencies = about.Descendants("modDependencies").Elements("li")
            .Select(item => (string?)item.Element("packageId") ?? string.Empty)
            .ToArray();
        string[] loadAfter = about.Descendants("loadAfter").Elements("li").Select(item => item.Value).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(dependencies, Is.EqualTo(new[] { "blues.forge", "brrainz.harmony" }));
            Assert.That(loadAfter, Does.Contain("meathax.showmeyourtools").IgnoreCase);
            Assert.That(dependencies, Does.Not.Contain("meathax.showmeyourtools").IgnoreCase);
            Assert.That(dependencies.Any(value => value.Contains("rimworlddevgateway")), Is.False);
        });
    }

    [Test]
    public void EnglishTextContainsExactNoContactAndOutcomeMessages()
    {
        XElement root = XDocument.Load(SourcePath("Languages", "English", "Keyed", "ImmersiveSignalFire.xml")).Root!;

        Assert.Multiple(() =>
        {
            Assert.That(root.Element("ImmersiveSignalFire_NoContacts")?.Value,
                Is.EqualTo("no allied neolithic or medieval settlements nearby"));
            Assert.That(root.Element("ImmersiveSignalFire_Unnoticed")?.Value,
                Does.Contain("failed to be noticed"));
            Assert.That(root.Element("ImmersiveSignalFire_Misunderstood")?.Value,
                Does.Contain("misunderstood"));
        });
    }

    [Test]
    public void ForgeEffectsRestoreTheOriginalParticleAndOriginalEmissionRate()
    {
        XElement root = XDocument.Load(SourcePath("Defs", "EffectDefs", "SignalFireEffects.xml")).Root!;
        XElement smoke = root.Elements("Blues.SingleTakeDef")
            .Single(def => (string?)def.Element("defName") == "ImmersiveSignalFire_DarkSmokePulse");
        XElement instruction = smoke.Element("fleckInstructions")!.Element("li")!;
        XElement fleck = root.Elements("FleckDef")
            .Single(def => (string?)def.Element("defName") == "ImmersiveSignalFire_DarkSmoke");

        Assert.Multiple(() =>
        {
            Assert.That((string?)fleck.Element("solidTime"), Is.EqualTo("0.7"));
            Assert.That((string?)fleck.Element("fadeInTime"), Is.EqualTo("0.12"));
            Assert.That((string?)fleck.Element("fadeOutTime"), Is.EqualTo("1.2"));
            Assert.That((string?)instruction.Element("ActiveTicks"), Is.EqualTo("0~20"));
            Assert.That((string?)instruction.Element("visuals")?.Element("scale"), Is.EqualTo("0.55~1.15"));
            Assert.That(instruction.Element("visuals")?.Element("colors")?.Elements("li").Select(item => item.Value),
                Is.EqualTo(new[] { "(0.09, 0.085, 0.075, 0.88)", "(0.17, 0.16, 0.14, 0.82)" }));
            Assert.That((string?)instruction.Element("spawner")?.Element("spawnPerTick"), Is.EqualTo("0.72"),
                "Periodic scheduling must not restyle the reference plume by changing its original emission rate.");
            Assert.That((string?)instruction.Element("spawner")?.Element("radius"), Is.EqualTo("(0.5, 0.2)"));
            Assert.That((string?)instruction.Element("spawner")?.Element("offset"), Is.EqualTo("(0, 0.55)"));
            Assert.That((string?)instruction.Element("physics")?.Element("speed"), Is.EqualTo("0.25~0.75"));
            Assert.That((string?)instruction.Element("physics")?.Element("range"), Is.EqualTo("1.7~2.6"));
            Assert.That((string?)instruction.Element("physics")?.Element("rotation"), Is.EqualTo("-10~10"));
            Assert.That(instruction.Element("fleckDefs")?.Elements("li").Select(item => item.Value),
                Is.EqualTo(new[] { "ImmersiveSignalFire_DarkSmoke" }));
        });
    }

    [Test]
    public void FlameEffectCannotImitateSmokeWhileBlanketsAreLowered()
    {
        XElement root = XDocument.Load(SourcePath("Defs", "EffectDefs", "SignalFireEffects.xml")).Root!;
        XElement flameFleck = root.Elements("FleckDef")
            .Single(def => (string?)def.Element("defName") == "ImmersiveSignalFire_FlameFleck");
        XElement flame = root.Elements("Blues.SingleTakeDef")
            .Single(def => (string?)def.Element("defName") == "ImmersiveSignalFire_Flame");
        XElement instruction = flame.Element("fleckInstructions")!.Element("li")!;
        using var flameTexture = new Bitmap(SourcePath(
            "Textures", "ImmersiveSignalFire", "Effects", "Flame.png"));

        Assert.Multiple(() =>
        {
            Assert.That((string?)flameFleck.Element("fleckSystemClass"), Is.EqualTo("Blues.FleckSystemForged"));
            Assert.That((string?)flameFleck.Element("graphicData")?.Element("texPath"),
                Is.EqualTo("ImmersiveSignalFire/Effects/Flame"));
            Assert.That((string?)flameFleck.Element("graphicData")?.Element("shaderType"), Is.EqualTo("MoteGlow"));
            Assert.That(instruction.Element("fleckDefs")?.Elements("li").Select(item => item.Value),
                Is.EqualTo(new[] { "ImmersiveSignalFire_FlameFleck" }),
                "The steady Forge layer must use only the product's white-alpha flame silhouette; stock grayscale fire flecks rendered dark source lobes during lowered-blanket gaps.");
            Assert.That(File.Exists(SourcePath("Textures", "ImmersiveSignalFire", "Effects", "Flame.png")), Is.True);
            for (int y = 0; y < flameTexture.Height; y++)
            {
                for (int x = 0; x < flameTexture.Width; x++)
                {
                    Color pixel = flameTexture.GetPixel(x, y);
                    if (pixel.A > 0)
                    {
                        Assert.That((pixel.R, pixel.G, pixel.B), Is.EqualTo((255, 255, 255)),
                            $"Visible flame RGB must remain white for tint-only rendering at ({x},{y}).");
                    }
                }
            }
        });
    }

    [Test]
    public void SmokeControllerUsesTheOrderedLifecycleAndKillsOwnedForgeRunners()
    {
        string source = File.ReadAllText(SourcePath("Source", "Effects", "SignalEffectController.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("smokeLifecycle.Advance(elapsedTicks, StopSmokeRunner, StartSmokeRunner, TickSmokeRunner)"));
            Assert.That(source, Does.Contain("smokeLifecycle.Stop(StopSmokeRunner)"));
            Assert.That(source, Does.Contain("smokeRunner.Kill()"));
            Assert.That(source, Does.Not.Contain("smokeRunnerStartTick"));
            Assert.That(source, Does.Not.Contain("smokeRunners"));
        });
    }

    [Test]
    public void TerminalActiveResolutionConsumesThePreparedBuilding()
    {
        string source = File.ReadAllText(SourcePath("Source", "Buildings", "CompSignalFire.cs"));
        int completeActive = source.IndexOf("private void CompleteActive", System.StringComparison.Ordinal);
        int terminalCommit = source.IndexOf("phase = SignalSessionPhase.Idle;", completeActive, System.StringComparison.Ordinal);
        int protectedResolution = source.IndexOf("try", completeActive, System.StringComparison.Ordinal);
        int resetSession = source.IndexOf(
            "ResetSession(interruptJobs: true, reportingPawn: reportingPawn);",
            completeActive,
            System.StringComparison.Ordinal);
        int protectedCleanup = source.IndexOf("finally", protectedResolution, System.StringComparison.Ordinal);
        int stopEffects = source.IndexOf("StopEffects();", protectedCleanup, System.StringComparison.Ordinal);
        int destroySpentFire = source.IndexOf("DestroySpentFire();", completeActive, System.StringComparison.Ordinal);
        int makeSoot = source.IndexOf("MakeSoot(map);", completeActive, System.StringComparison.Ordinal);
        int exactDeltaCheck = source.IndexOf("afterThickness - beforeThickness != 16", System.StringComparison.Ordinal);
        int cleanupCommit = source.IndexOf("cleanupDone = true", exactDeltaCheck, System.StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("DestroySpentFire"));
            Assert.That(source, Does.Contain("Fire.Destroy(DestroyMode.Vanish)"));
            Assert.That(source, Does.Contain("destroySpentFire: false"));
            Assert.That(terminalCommit, Is.LessThan(protectedResolution),
                "Terminal resolution must commit phase state before effect or job cleanup can throw.");
            Assert.That(resetSession, Is.GreaterThan(protectedResolution).And.LessThan(protectedCleanup),
                "Participant job cleanup must remain inside the outer terminal protection.");
            Assert.That(stopEffects, Is.GreaterThan(protectedCleanup).And.LessThan(destroySpentFire),
                "Forge cleanup must be protected by the nested destruction/soot finally path.");
            Assert.That(makeSoot, Is.GreaterThan(destroySpentFire),
                "Owned terminal cleanup must destroy the spent fire before placing and verifying final ash.");
            Assert.That(source, Does.Contain("Fire.OccupiedRect().ExpandedBy(1)"));
            Assert.That(source, Does.Contain("shouldPropagate: false"));
            Assert.That(source, Does.Contain("afterThickness - beforeThickness != 16"));
            Assert.That(cleanupCommit, Is.GreaterThan(exactDeltaCheck),
                "Cleanup idempotence must commit only after the exact 16-thickness delta is verified.");
        });
    }

    [TestCase("Things/Building/SignalFire/SignalFire.png", 512, 512)]
    [TestCase("Effects/DarkSmoke.png", 128, 128)]
    [TestCase("Effects/Flame.png", 128, 128)]
    [TestCase("Effects/SignalBlanket.png", 128, 256)]
    public void PackagedSpritesAreStrictEightBitRgba(string relativePath, int width, int height)
    {
        string[] pathParts = new[] { "Textures", "ImmersiveSignalFire" }
            .Concat(relativePath.Split('/'))
            .ToArray();
        using Image image = Image.FromFile(SourcePath(pathParts));

        Assert.Multiple(() =>
        {
            Assert.That(image.Width, Is.EqualTo(width));
            Assert.That(image.Height, Is.EqualTo(height));
            Assert.That(image.PixelFormat, Is.EqualTo(PixelFormat.Format32bppArgb));
        });
    }

    [TestCase("Things/Building/SignalFire/SignalFire.png")]
    [TestCase("Effects/DarkSmoke.png")]
    [TestCase("Effects/Flame.png")]
    [TestCase("Effects/SignalBlanket.png")]
    public void PackagedSpritesUseBlackRgbUnderFullyTransparentPixels(string relativePath)
    {
        string[] pathParts = new[] { "Textures", "ImmersiveSignalFire" }
            .Concat(relativePath.Split('/'))
            .ToArray();
        using var image = new Bitmap(SourcePath(pathParts));

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Color pixel = image.GetPixel(x, y);
                if (pixel.A == 0)
                {
                    Assert.That((pixel.R, pixel.G, pixel.B), Is.EqualTo((0, 0, 0)),
                        $"Invisible RGB must be black at ({x},{y}) in {relativePath}.");
                }
            }
        }
    }

    [Test]
    public void WorkshopCopyDescribesTheWoodOnlySingleUseStack()
    {
        string description = File.ReadAllText(SourcePath("Release", "workshop", "description.bbcode.txt"));

        Assert.Multiple(() =>
        {
            Assert.That(description, Does.Contain("fresh-and-seasoned wood signal stack"));
            Assert.That(description, Does.Not.Contain("stone-and-wood"));
        });
    }

    [Test]
    public void SignalFireSpriteUsesARestrainedFreshWoodMajorityPalette()
    {
        using var image = new Bitmap(SourcePath(
            "Textures", "ImmersiveSignalFire", "Things", "Building", "SignalFire", "SignalFire.png"));
        var colors = new System.Collections.Generic.HashSet<int>();
        int visible = 0;
        int freshWood = 0;
        int brownWood = 0;
        int minX = image.Width;
        int minY = image.Height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Color pixel = image.GetPixel(x, y);
                if (pixel.A < 32)
                {
                    continue;
                }

                visible++;
                colors.Add((pixel.R << 16) | (pixel.G << 8) | pixel.B);
                minX = System.Math.Min(minX, x);
                minY = System.Math.Min(minY, y);
                maxX = System.Math.Max(maxX, x);
                maxY = System.Math.Max(maxY, y);
                if (pixel.G >= pixel.R * 0.90 && pixel.G > pixel.B * 1.12)
                {
                    freshWood++;
                }

                if (pixel.R > pixel.G * 1.08 && pixel.G > pixel.B * 1.05)
                {
                    brownWood++;
                }
            }
        }

        double freshShare = freshWood / (double)System.Math.Max(1, freshWood + brownWood);
        Assert.Multiple(() =>
        {
            Assert.That(visible, Is.GreaterThan(0));
            Assert.That(colors.Count, Is.LessThanOrEqualTo(48));
            Assert.That(maxX - minX + 1, Is.InRange(396, 430));
            Assert.That(maxY - minY + 1, Is.InRange(320, 370));
            Assert.That(freshShare, Is.GreaterThanOrEqualTo(0.65));
        });
    }

    private static string SourcePath(params string[] parts)
    {
        string repositoryRoot = typeof(SignalFireSourceContractTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RepositoryRoot")
            .Value;
        return Path.Combine(new[] { repositoryRoot, "mods", "ImmersiveSignalFire" }.Concat(parts).ToArray());
    }
}
