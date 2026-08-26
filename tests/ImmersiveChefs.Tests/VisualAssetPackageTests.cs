using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class VisualAssetPackageTests
{
    private const string CookwareTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/Cookware/Cookware";
    private const string PrimitiveCookwareTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/PrimitiveCookware/PrimitiveCookware";
    private const string PlateTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/Plate/Plate";
    private const string CutleryTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/Cutlery/Cutlery";
    private const string ChefsKnifeTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/ChefsKnife/ChefsKnife";
    private const string GlitterCookwareTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/GlitterCookware/GlitterCookware";
    private const string PreparedFoodTexturePath =
        "ImmersiveChefs/Things/Item/Food/PreparedIngredients/PreparedIngredients";
    private const string DishwasherTexturePath =
        "ImmersiveChefs/Things/Building/Dishwasher/Dishwasher";
    private const string IndustrialDishwasherTexturePath =
        "ImmersiveChefs/Things/Building/Dishwasher/IndustrialDishwasher";
    private const string PrepStationTexturePath =
        "ImmersiveChefs/Things/Building/KitchenStation/PrepStation";
    private const string SauceStationTexturePath =
        "ImmersiveChefs/Things/Building/KitchenStation/SauceStation";
    private const string MeatStationTexturePath =
        "ImmersiveChefs/Things/Building/KitchenStation/MeatStation";
    private const string VegetableStationTexturePath =
        "ImmersiveChefs/Things/Building/KitchenStation/VegetableStation";
    private const string PastryStationTexturePath =
        "ImmersiveChefs/Things/Building/KitchenStation/PastryStation";
    private const string MicrowaveTexturePath =
        "ImmersiveChefs/Things/Building/Appliance/Microwave";

    [Test]
    public void Every_shipped_Thing_diffuse_has_a_reviewed_Core_relative_outline_approval()
    {
        var root = FindRepositoryRoot();
        var textureRoot = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            "ImmersiveChefs",
            "Things");
        var manifestPath = Path.Combine(root, "docs", "SpriteOutlineApprovals.xml");
        Assert.That(
            File.Exists(manifestPath),
            Is.True,
            "Every shipped Thing diffuse needs a reviewed Core-relative outline approval.");

        var manifest = XDocument.Load(manifestPath);
        var sprites = manifest.Root!.Elements("sprite").ToArray();
        var packagedDiffuses = Directory
            .GetFiles(textureRoot, "*.png", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith("_m.png", StringComparison.OrdinalIgnoreCase))
            .Select(path => path.Substring(textureRoot.Length + 1).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var approvedPaths = sprites
            .Select(sprite => (string?)sprite.Attribute("path") ?? string.Empty)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(approvedPaths, Is.EqualTo(packagedDiffuses));
            Assert.That(
                sprites.Select(sprite => (string?)sprite.Attribute("id")),
                Is.Unique,
                "Outline approval ids must be stable and unique.");
        });

        foreach (var sprite in sprites)
        {
            var relativePath = (string)sprite.Attribute("path")!;
            var diffusePath = Path.Combine(textureRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.Multiple(() =>
            {
                Assert.That((string?)sprite.Attribute("class"), Is.AnyOf("portable-fine", "portable-broad", "building"), relativePath);
                Assert.That((int?)sprite.Attribute("finalWidth"), Is.GreaterThan(0), relativePath);
                Assert.That((int?)sprite.Attribute("finalHeight"), Is.GreaterThan(0), relativePath);
                Assert.That((int?)sprite.Attribute("foregroundComponents"), Is.GreaterThanOrEqualTo(1), relativePath);
                Assert.That((int?)sprite.Attribute("backgroundHoles"), Is.GreaterThanOrEqualTo(0), relativePath);
                Assert.That((int?)sprite.Attribute("selectedStrokeSourcePixels"), Is.GreaterThan(0), relativePath);
                Assert.That((double?)sprite.Attribute("ring1"), Is.InRange(0.0, 1.0), relativePath);
                Assert.That((double?)sprite.Attribute("ring2"), Is.InRange(0.0, 1.0), relativePath);
                Assert.That((double?)sprite.Attribute("ring3"), Is.InRange(0.0, 1.0), relativePath);
                Assert.That((string?)sprite.Attribute("reviewEvidence"), Is.Not.Null.And.Not.Empty, relativePath);
                Assert.That((string?)sprite.Attribute("preOutlineSha256"), Does.Match("^[0-9A-F]{64}$"), relativePath);
                Assert.That((string?)sprite.Attribute("outputSha256"), Is.EqualTo(ComputeSha256(diffusePath)), relativePath);
            });
        }
    }

    [Test]
    public void Outline_processor_adds_only_exterior_contour_and_preserves_topology_gaps_and_mask_semantics()
    {
        var root = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            root,
            ".agents",
            "skills",
            "rimworld-asset-generation",
            "scripts",
            "Add-RimWorldSpriteOutline.ps1");
        var baselinePath = Path.Combine(root, "docs", "SpriteOutlineBaseline.xml");
        var temporaryRoot = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "outline-processor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        var inputPath = Path.Combine(temporaryRoot, "fixture.png");
        var maskInputPath = Path.Combine(temporaryRoot, "fixture_m.png");
        var outputPath = Path.Combine(temporaryRoot, "outlined.png");
        var maskOutputPath = Path.Combine(temporaryRoot, "outlined_m.png");
        var manifestPath = Path.Combine(temporaryRoot, "approvals.xml");

        try
        {
            using (var diffuse = new Bitmap(40, 40))
            using (var mask = new Bitmap(40, 40))
            {
                var diffuseColor = Color.FromArgb(255, 186, 123, 72);
                var maskColor = Color.FromArgb(255, 255, 0, 0);

                FillRectangle(diffuse, 5, 5, 14, 14, diffuseColor);
                FillRectangle(mask, 5, 5, 14, 14, maskColor);
                ClearRectangle(diffuse, 8, 8, 11, 11);
                ClearRectangle(mask, 8, 8, 11, 11);

                FillRectangle(diffuse, 22, 6, 24, 32, diffuseColor);
                FillRectangle(diffuse, 32, 6, 34, 32, diffuseColor);
                FillRectangle(diffuse, 22, 30, 34, 32, diffuseColor);
                FillRectangle(mask, 22, 6, 24, 32, maskColor);
                FillRectangle(mask, 32, 6, 34, 32, maskColor);
                FillRectangle(mask, 22, 30, 34, 32, maskColor);

                diffuse.Save(inputPath, System.Drawing.Imaging.ImageFormat.Png);
                mask.Save(maskInputPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            string sourceHash;
            using (var sha256 = SHA256.Create())
            {
                sourceHash = BitConverter
                    .ToString(sha256.ComputeHash(File.ReadAllBytes(inputPath)))
                    .Replace("-", string.Empty);
            }
            new XDocument(
                new XElement(
                    "spriteOutlineApprovals",
                    new XElement(
                        "sprite",
                        new XAttribute("id", "synthetic-fixture"),
                        new XAttribute("class", "portable-broad"),
                        new XAttribute("preOutlineSha256", sourceHash),
                        new XAttribute("finalWidth", "40"),
                        new XAttribute("finalHeight", "40"),
                        new XAttribute("foregroundComponents", "2"),
                        new XAttribute("backgroundHoles", "1"),
                        new XElement(
                            "protectedGap",
                            new XAttribute("id", "synthetic-opening"),
                            new XAttribute("orientation", "horizontal"),
                            new XAttribute("fixedCoordinate", "12"),
                            new XAttribute("start", "25"),
                            new XAttribute("end", "31"),
                            new XAttribute("minimumTransparentRunPixels", "3")),
                        new XElement(
                            "sourceContourExclusion",
                            new XAttribute("protectedGapId", "synthetic-opening"),
                            new XAttribute("x", "25"),
                            new XAttribute("y", "12"),
                            new XAttribute("width", "1"),
                            new XAttribute("height", "1"),
                            new XAttribute("reason", "preserve the synthetic U-shaped opening")))))
                .Save(manifestPath);

            var result = RunOutlineProcessor(
                scriptPath,
                inputPath,
                outputPath,
                2,
                baselinePath,
                manifestPath,
                "synthetic-fixture",
                maskInputPath,
                maskOutputPath);

            Assert.That(result.ExitCode, Is.Zero, result.StandardOutput + Environment.NewLine + result.StandardError);
            Assert.That(File.Exists(outputPath), Is.True, result.StandardOutput + Environment.NewLine + result.StandardError);
            Assert.That(File.Exists(maskOutputPath), Is.True, result.StandardOutput + Environment.NewLine + result.StandardError);

            var unrelatedManifestPath = Path.Combine(temporaryRoot, "unrelated-exclusion-approvals.xml");
            var unrelatedManifest = XDocument.Load(manifestPath);
            var unrelatedExclusion = unrelatedManifest.Root!
                .Element("sprite")!
                .Element("sourceContourExclusion")!;
            unrelatedExclusion.SetAttributeValue("x", "0");
            unrelatedExclusion.SetAttributeValue("y", "0");
            unrelatedManifest.Save(unrelatedManifestPath);
            var unrelatedOutputPath = Path.Combine(temporaryRoot, "unrelated-exclusion.png");
            var unrelatedMaskOutputPath = Path.Combine(temporaryRoot, "unrelated-exclusion_m.png");
            var unrelated = RunOutlineProcessor(
                scriptPath,
                inputPath,
                unrelatedOutputPath,
                2,
                baselinePath,
                unrelatedManifestPath,
                "synthetic-fixture",
                maskInputPath,
                unrelatedMaskOutputPath);
            Assert.Multiple(() =>
            {
                Assert.That(unrelated.ExitCode, Is.EqualTo(2), unrelated.StandardOutput + Environment.NewLine + unrelated.StandardError);
                Assert.That(unrelated.StandardError, Does.Contain("protected gap").IgnoreCase);
                Assert.That(File.Exists(unrelatedOutputPath), Is.False, "An unrelated exclusion published a diffuse.");
                Assert.That(File.Exists(unrelatedMaskOutputPath), Is.False, "An unrelated exclusion published a mask.");
            });

            var oversizedManifestPath = Path.Combine(temporaryRoot, "oversized-exclusion-approvals.xml");
            var oversizedManifest = XDocument.Load(manifestPath);
            var oversizedExclusion = oversizedManifest.Root!
                .Element("sprite")!
                .Element("sourceContourExclusion")!;
            oversizedExclusion.SetAttributeValue("x", "0");
            oversizedExclusion.SetAttributeValue("y", "0");
            oversizedExclusion.SetAttributeValue("width", "40");
            oversizedExclusion.SetAttributeValue("height", "40");
            oversizedManifest.Save(oversizedManifestPath);
            var oversizedOutputPath = Path.Combine(temporaryRoot, "oversized-exclusion.png");
            var oversizedMaskOutputPath = Path.Combine(temporaryRoot, "oversized-exclusion_m.png");
            var oversized = RunOutlineProcessor(
                scriptPath,
                inputPath,
                oversizedOutputPath,
                2,
                baselinePath,
                oversizedManifestPath,
                "synthetic-fixture",
                maskInputPath,
                oversizedMaskOutputPath);
            Assert.Multiple(() =>
            {
                Assert.That(oversized.ExitCode, Is.EqualTo(2), oversized.StandardOutput + Environment.NewLine + oversized.StandardError);
                Assert.That(oversized.StandardError, Does.Contain("local neighborhood").IgnoreCase);
                Assert.That(File.Exists(oversizedOutputPath), Is.False, "An oversized exclusion published a diffuse.");
                Assert.That(File.Exists(oversizedMaskOutputPath), Is.False, "An oversized exclusion published a mask.");
            });

            var rejectedOutputPath = Path.Combine(temporaryRoot, "rejected.png");
            var rejectedMaskOutputPath = Path.Combine(temporaryRoot, "rejected_m.png");
            var rejected = RunOutlineProcessor(
                scriptPath,
                inputPath,
                rejectedOutputPath,
                3,
                baselinePath,
                manifestPath,
                "synthetic-fixture",
                maskInputPath,
                rejectedMaskOutputPath);
            Assert.Multiple(() =>
            {
                Assert.That(rejected.ExitCode, Is.EqualTo(2), rejected.StandardOutput + Environment.NewLine + rejected.StandardError);
                Assert.That(File.Exists(rejectedOutputPath), Is.False, "A topology-invalid diffuse was written.");
                Assert.That(File.Exists(rejectedMaskOutputPath), Is.False, "A topology-invalid mask was written.");
            });

            var undersizedManifestPath = Path.Combine(temporaryRoot, "undersized-approvals.xml");
            var undersizedManifest = XDocument.Load(manifestPath);
            var undersizedSprite = undersizedManifest.Root!.Element("sprite")!;
            undersizedSprite.SetAttributeValue("finalWidth", "20");
            undersizedSprite.SetAttributeValue("finalHeight", "20");
            undersizedManifest.Save(undersizedManifestPath);
            var undersizedOutputPath = Path.Combine(temporaryRoot, "undersized.png");
            var undersizedMaskOutputPath = Path.Combine(temporaryRoot, "undersized_m.png");
            var undersized = RunOutlineProcessor(
                scriptPath,
                inputPath,
                undersizedOutputPath,
                1,
                baselinePath,
                undersizedManifestPath,
                "synthetic-fixture",
                maskInputPath,
                undersizedMaskOutputPath);
            Assert.Multiple(() =>
            {
                Assert.That(undersized.ExitCode, Is.EqualTo(2), undersized.StandardOutput + Environment.NewLine + undersized.StandardError);
                Assert.That(File.Exists(undersizedOutputPath), Is.False, "An undersized-stroke diffuse was written.");
                Assert.That(File.Exists(undersizedMaskOutputPath), Is.False, "An undersized-stroke mask was written.");
            });

            var brightOutputPath = Path.Combine(temporaryRoot, "bright.png");
            var brightMaskOutputPath = Path.Combine(temporaryRoot, "bright_m.png");
            var bright = RunOutlineProcessor(
                scriptPath,
                inputPath,
                brightOutputPath,
                2,
                baselinePath,
                manifestPath,
                "synthetic-fixture",
                maskInputPath,
                brightMaskOutputPath,
                "#FFFFFF");
            Assert.Multiple(() =>
            {
                Assert.That(bright.ExitCode, Is.EqualTo(1), bright.StandardOutput + Environment.NewLine + bright.StandardError);
                Assert.That(File.Exists(brightOutputPath), Is.False, "A below-threshold diffuse was written.");
                Assert.That(File.Exists(brightMaskOutputPath), Is.False, "A below-threshold mask was written.");
            });

            var strictBaselinePath = Path.Combine(temporaryRoot, "strict-baseline.xml");
            var strictBaseline = XDocument.Load(baselinePath);
            strictBaseline.Root!
                .Element("acceptanceClasses")!
                .Elements("class")
                .Single(element => (string?)element.Attribute("id") == "portable-broad")
                .SetAttributeValue("minimumTransparentEdgeClearance", "4");
            strictBaseline.Save(strictBaselinePath);
            var edgeOutputPath = Path.Combine(temporaryRoot, "edge.png");
            var edgeMaskOutputPath = Path.Combine(temporaryRoot, "edge_m.png");
            var edgeRejected = RunOutlineProcessor(
                scriptPath,
                inputPath,
                edgeOutputPath,
                2,
                strictBaselinePath,
                manifestPath,
                "synthetic-fixture",
                maskInputPath,
                edgeMaskOutputPath);
            Assert.Multiple(() =>
            {
                Assert.That(edgeRejected.ExitCode, Is.EqualTo(1), edgeRejected.StandardOutput + Environment.NewLine + edgeRejected.StandardError);
                Assert.That(File.Exists(edgeOutputPath), Is.False, "An edge-cropped diffuse was written.");
                Assert.That(File.Exists(edgeMaskOutputPath), Is.False, "An edge-cropped mask was written.");
            });

            var fringeInputPath = Path.Combine(temporaryRoot, "fringe.png");
            var fringeMaskInputPath = Path.Combine(temporaryRoot, "fringe_m.png");
            using (var fringe = new Bitmap(inputPath))
            using (var fringeMask = new Bitmap(maskInputPath))
            {
                fringe.SetPixel(0, 0, Color.FromArgb(1, 0, 0, 0));
                fringeMask.SetPixel(0, 0, Color.FromArgb(1, 0, 0, 0));
                fringe.Save(fringeInputPath, System.Drawing.Imaging.ImageFormat.Png);
                fringeMask.Save(fringeMaskInputPath, System.Drawing.Imaging.ImageFormat.Png);
            }
            string fringeHash;
            using (var sha256 = SHA256.Create())
            {
                fringeHash = BitConverter
                    .ToString(sha256.ComputeHash(File.ReadAllBytes(fringeInputPath)))
                    .Replace("-", string.Empty);
            }
            var fringeManifestPath = Path.Combine(temporaryRoot, "fringe-approvals.xml");
            var fringeManifest = XDocument.Load(manifestPath);
            fringeManifest.Root!.Element("sprite")!
                .SetAttributeValue("preOutlineSha256", fringeHash);
            fringeManifest.Save(fringeManifestPath);
            var fringeOutputPath = Path.Combine(temporaryRoot, "fringe-out.png");
            var fringeMaskOutputPath = Path.Combine(temporaryRoot, "fringe-out_m.png");
            var fringeRejected = RunOutlineProcessor(
                scriptPath,
                fringeInputPath,
                fringeOutputPath,
                2,
                baselinePath,
                fringeManifestPath,
                "synthetic-fixture",
                fringeMaskInputPath,
                fringeMaskOutputPath);
            Assert.Multiple(() =>
            {
                Assert.That(fringeRejected.ExitCode, Is.EqualTo(1), fringeRejected.StandardOutput + Environment.NewLine + fringeRejected.StandardError);
                Assert.That(File.Exists(fringeOutputPath), Is.False, "A low-alpha edge-fringe diffuse was written.");
                Assert.That(File.Exists(fringeMaskOutputPath), Is.False, "A low-alpha edge-fringe mask was written.");
            });

            var occupiedOutputPath = Path.Combine(temporaryRoot, "occupied.png");
            var sentinel = new byte[] { 17, 35, 53, 71 };
            File.WriteAllBytes(occupiedOutputPath, sentinel);
            var occupied = RunOutlineProcessor(
                scriptPath,
                inputPath,
                occupiedOutputPath,
                2,
                baselinePath,
                manifestPath,
                "synthetic-fixture",
                maskInputPath,
                Path.Combine(temporaryRoot, "occupied_m.png"));
            Assert.Multiple(() =>
            {
                Assert.That(occupied.ExitCode, Is.EqualTo(2), occupied.StandardOutput + Environment.NewLine + occupied.StandardError);
                Assert.That(File.ReadAllBytes(occupiedOutputPath), Is.EqualTo(sentinel), "An occupied destination was replaced.");
            });

            var collisionPath = Path.Combine(temporaryRoot, "collision.png");
            var collision = RunOutlineProcessor(
                scriptPath,
                inputPath,
                collisionPath,
                2,
                baselinePath,
                manifestPath,
                "synthetic-fixture",
                maskInputPath,
                collisionPath);
            Assert.Multiple(() =>
            {
                Assert.That(collision.ExitCode, Is.EqualTo(2), collision.StandardOutput + Environment.NewLine + collision.StandardError);
                Assert.That(collision.StandardError, Does.Contain("distinct").IgnoreCase);
                Assert.That(File.Exists(collisionPath), Is.False, "A cross-colliding destination was written.");
            });

            using var source = new Bitmap(inputPath);
            using var sourceMask = new Bitmap(maskInputPath);
            using var outlined = new Bitmap(outputPath);
            using var outlinedMask = new Bitmap(maskOutputPath);
            Assert.Multiple(() =>
            {
                Assert.That(outlined.GetPixel(4, 5).A, Is.EqualTo(255), "Expected a new exterior contour pixel.");
                Assert.That(outlined.GetPixel(9, 9).A, Is.Zero, "The enclosed hole must not receive contour.");
                Assert.That(outlined.GetPixel(28, 12).A, Is.LessThan(96), "The protected U-shaped opening closed.");
                Assert.That(outlined.GetPixel(25, 12).A, Is.Zero, "The approved source-space gap exclusion was ignored.");
            });

            for (var y = 0; y < source.Height; y++)
            {
                for (var x = 0; x < source.Width; x++)
                {
                    var before = source.GetPixel(x, y);
                    var after = outlined.GetPixel(x, y);
                    var beforeMask = sourceMask.GetPixel(x, y);
                    var afterMask = outlinedMask.GetPixel(x, y);
                    if (before.A > 0)
                    {
                        Assert.That(after.ToArgb(), Is.EqualTo(before.ToArgb()), $"Diffuse changed at ({x},{y}).");
                        Assert.That(afterMask.ToArgb(), Is.EqualTo(beforeMask.ToArgb()), $"Mask changed at ({x},{y}).");
                    }

                    Assert.That(afterMask.A, Is.EqualTo(after.A), $"Mask alpha differs at ({x},{y}).");
                    if (before.A == 0 && after.A > 0)
                    {
                        Assert.That(afterMask.R, Is.Zero, $"New mask contour is not fixed black at ({x},{y}).");
                        Assert.That(afterMask.G, Is.Zero, $"New mask contour is not fixed black at ({x},{y}).");
                        Assert.That(afterMask.B, Is.Zero, $"New mask contour is not fixed black at ({x},{y}).");
                    }
                }
            }
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }

    [Test]
    public void Outline_processor_accepts_the_measured_three_quarter_pixel_fine_item_boundary()
    {
        var root = FindRepositoryRoot();
        var scriptPath = Path.Combine(
            root,
            ".agents",
            "skills",
            "rimworld-asset-generation",
            "scripts",
            "Add-RimWorldSpriteOutline.ps1");
        var baselinePath = Path.Combine(root, "docs", "SpriteOutlineBaseline.xml");
        var temporaryRoot = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "outline-fine-boundary-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        var inputPath = Path.Combine(temporaryRoot, "fine.png");
        var maskInputPath = Path.Combine(temporaryRoot, "fine_m.png");
        var outputPath = Path.Combine(temporaryRoot, "fine-out.png");
        var maskOutputPath = Path.Combine(temporaryRoot, "fine-out_m.png");
        var manifestPath = Path.Combine(temporaryRoot, "approvals.xml");

        try
        {
            using (var diffuse = new Bitmap(40, 40))
            using (var mask = new Bitmap(40, 40))
            {
                FillRectangle(diffuse, 5, 5, 34, 34, Color.FromArgb(255, 23, 19, 15));
                FillRectangle(mask, 5, 5, 34, 34, Color.FromArgb(255, 255, 0, 0));
                diffuse.Save(inputPath, System.Drawing.Imaging.ImageFormat.Png);
                mask.Save(maskInputPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            new XDocument(
                new XElement(
                    "spriteOutlineApprovals",
                    new XElement(
                        "sprite",
                        new XAttribute("id", "fine-boundary"),
                        new XAttribute("class", "portable-fine"),
                        new XAttribute("preOutlineSha256", ComputeSha256(inputPath)),
                        new XAttribute("finalWidth", "30"),
                        new XAttribute("finalHeight", "30"),
                        new XAttribute("foregroundComponents", "1"),
                        new XAttribute("backgroundHoles", "0"))))
                .Save(manifestPath);

            var result = RunOutlineProcessor(
                scriptPath,
                inputPath,
                outputPath,
                1,
                baselinePath,
                manifestPath,
                "fine-boundary",
                maskInputPath,
                maskOutputPath);

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardOutput + Environment.NewLine + result.StandardError);
                Assert.That(File.Exists(outputPath), Is.True, "The accepted fine-item candidate was not published.");
                Assert.That(File.Exists(maskOutputPath), Is.True, "The accepted fine-item mask was not published.");
            });
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }

    [Test]
    public void Disposable_generator_comparison_assets_are_absent_from_the_package()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "mods", "ImmersiveChefs", "ImmersiveChefs.csproj");
        var packagedVersionRoot = Path.Combine(
            root,
            "artifacts",
            "Mods",
            "fumblesneeze.immersivechefs",
            "1.6");
        var project = XDocument.Load(projectPath);
        var cleanupTarget = project.Root!
            .Elements("Target")
            .Single(element =>
                (string?)element.Attribute("Name") == "RemoveDisposableVisualComparisonAssets");
        var scheduledTargets = ((string?)cleanupTarget.Attribute("BeforeTargets") ?? string.Empty)
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        Assert.That(scheduledTargets, Is.EquivalentTo(new[] { "Build", "CopyMod" }));

        Assert.Multiple(() =>
        {
            Assert.That(
                File.Exists(Path.Combine(
                    packagedVersionRoot,
                    "Defs",
                    "ThingDefs",
                    "VisualGeneratorComparison.xml")),
                Is.False,
                "The disposable visual-comparison Def must never survive incremental packaging.");
            Assert.That(
                Directory.Exists(Path.Combine(
                    packagedVersionRoot,
                    "Textures",
                    "ImmersiveChefs",
                    "Dev",
                    "GeneratorComparison")),
                Is.False,
                "Local and built-in generator drafts are evidence artifacts, not release textures.");
        });

        var isolatedRoot = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "visual-package-cleanup-" + Guid.NewGuid().ToString("N"));
        var isolatedOutputVersion = Path.Combine(isolatedRoot, "artifacts", "1.6");
        var isolatedLivePackage = Path.Combine(isolatedRoot, "live-package");
        var seededPaths = new[]
        {
            Path.Combine(
                isolatedOutputVersion,
                "Defs",
                "ThingDefs",
                "VisualGeneratorComparison.xml"),
            Path.Combine(
                isolatedOutputVersion,
                "Textures",
                "ImmersiveChefs",
                "Dev",
                "GeneratorComparison",
                "draft.png"),
            Path.Combine(
                isolatedLivePackage,
                "1.6",
                "Defs",
                "ThingDefs",
                "VisualGeneratorComparison.xml"),
            Path.Combine(
                isolatedLivePackage,
                "1.6",
                "Textures",
                "ImmersiveChefs",
                "Dev",
                "GeneratorComparison",
                "draft.png")
        };

        System.Diagnostics.Process? cleanupProcess = null;
        var cleanupProcessStarted = false;
        try
        {
            foreach (var seededPath in seededPaths)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(seededPath)!);
                File.WriteAllText(seededPath, "disposable visual-comparison sentinel");
            }

            var arguments =
                $"msbuild \"{projectPath}\" -nologo " +
                "-t:RemoveDisposableVisualComparisonAssets " +
                $"-p:OutputVersionFolder=\"{isolatedOutputVersion}\" " +
                $"-p:RimWorldModPackageFolder=\"{isolatedLivePackage}\"";
            var standardOutput = new System.Text.StringBuilder();
            var standardError = new System.Text.StringBuilder();
            cleanupProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            cleanupProcess.OutputDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is not null)
                {
                    standardOutput.AppendLine(eventArgs.Data);
                }
            };
            cleanupProcess.ErrorDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is not null)
                {
                    standardError.AppendLine(eventArgs.Data);
                }
            };
            cleanupProcessStarted = cleanupProcess.Start();
            Assert.That(cleanupProcessStarted, Is.True, "Failed to start the isolated cleanup target.");
            cleanupProcess.BeginOutputReadLine();
            cleanupProcess.BeginErrorReadLine();
            var exited = cleanupProcess.WaitForExit(30_000);
            if (!exited)
            {
                cleanupProcess.Kill();
                cleanupProcess.WaitForExit(5_000);
            }
            else
            {
                cleanupProcess.WaitForExit();
            }

            Assert.That(
                exited,
                Is.True,
                "The isolated cleanup target did not complete in 30 seconds.");
            Assert.That(
                cleanupProcess.ExitCode,
                Is.Zero,
                standardOutput + Environment.NewLine + standardError);
            Assert.That(
                seededPaths.Any(File.Exists),
                Is.False,
                "The cleanup target must remove stale comparison assets from both output roots.");
        }
        finally
        {
            if (cleanupProcessStarted && cleanupProcess is not null && !cleanupProcess.HasExited)
            {
                cleanupProcess.Kill();
                cleanupProcess.WaitForExit(5_000);
            }

            cleanupProcess?.Dispose();
            if (Directory.Exists(isolatedRoot))
            {
                Directory.Delete(isolatedRoot, recursive: true);
            }
        }
    }

    [Test]
    public void Portable_texture_variation_families_are_complete_masked_and_chroma_free()
    {
        var root = FindRepositoryRoot();
        var pairs = new[]
        {
            (CookwareTexturePath + "_Dirty", true),
            (CookwareTexturePath + "_Stone", true),
            (CookwareTexturePath + "_StoneDirty", true),
            (PlateTexturePath + "_Dirty", false),
            (PlateTexturePath + "_Wood", false),
            (PlateTexturePath + "_WoodDirty", false),
            (PlateTexturePath + "_Stone", false),
            (PlateTexturePath + "_StoneDirty", false),
            (CutleryTexturePath + "_Dirty", false),
            (CutleryTexturePath + "_Wood", false),
            (CutleryTexturePath + "_WoodDirty", false)
        };

        foreach (var (texturePath, requireFixedBlackRegion) in pairs)
        {
            var diffusePath = TextureFile(root, texturePath + ".png");
            var maskPath = TextureFile(root, texturePath + "_m.png");
            var packagedDiffuse = PackagedTextureFile(root, texturePath + ".png");
            var packagedMask = PackagedTextureFile(root, texturePath + "_m.png");

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(diffusePath), Is.True, texturePath + " diffuse");
                Assert.That(File.Exists(maskPath), Is.True, texturePath + " mask");
                Assert.That(File.Exists(packagedDiffuse), Is.True, texturePath + " packaged diffuse");
                Assert.That(File.Exists(packagedMask), Is.True, texturePath + " packaged mask");
            });
            AssertPackagedTextureMatchesSource(diffusePath, packagedDiffuse);
            AssertPackagedTextureMatchesSource(maskPath, packagedMask);
            AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion);
            AssertNoVividGreenChroma(diffusePath);
        }

        AssertSpritesDiffer(
            TextureFile(root, PlateTexturePath + ".png"),
            TextureFile(root, PlateTexturePath + "_Wood.png"));
        AssertSpritesDiffer(
            TextureFile(root, PlateTexturePath + ".png"),
            TextureFile(root, PlateTexturePath + "_Stone.png"));
        AssertSpritesDiffer(
            TextureFile(root, CookwareTexturePath + ".png"),
            TextureFile(root, CookwareTexturePath + "_Stone.png"));
        AssertSpritesDiffer(
            TextureFile(root, CutleryTexturePath + ".png"),
            TextureFile(root, CutleryTexturePath + "_Wood.png"));
        foreach (var (clean, dirty) in new[]
                 {
                     (CookwareTexturePath, CookwareTexturePath + "_Dirty"),
                     (CookwareTexturePath + "_Stone", CookwareTexturePath + "_StoneDirty"),
                     (PlateTexturePath, PlateTexturePath + "_Dirty"),
                     (PlateTexturePath + "_Wood", PlateTexturePath + "_WoodDirty"),
                     (PlateTexturePath + "_Stone", PlateTexturePath + "_StoneDirty"),
                     (CutleryTexturePath, CutleryTexturePath + "_Dirty"),
                     (CutleryTexturePath + "_Wood", CutleryTexturePath + "_WoodDirty")
                 })
        {
            AssertSpritesDiffer(
                TextureFile(root, clean + ".png"),
                TextureFile(root, dirty + ".png"));
        }
    }

    [Test]
    public void Dirty_portable_families_have_conspicuous_fixed_color_grime_after_steel_tint()
    {
        var root = FindRepositoryRoot();
        foreach (var (clean, dirty) in new[]
                 {
                     (CookwareTexturePath, CookwareTexturePath + "_Dirty"),
                     (CookwareTexturePath + "_Stone", CookwareTexturePath + "_StoneDirty"),
                     (PlateTexturePath, PlateTexturePath + "_Dirty"),
                     (PlateTexturePath + "_Wood", PlateTexturePath + "_WoodDirty"),
                     (PlateTexturePath + "_Stone", PlateTexturePath + "_StoneDirty"),
                     (CutleryTexturePath, CutleryTexturePath + "_Dirty"),
                     (CutleryTexturePath + "_Wood", CutleryTexturePath + "_WoodDirty"),
                     (PrimitiveCookwareTexturePath, PrimitiveCookwareTexturePath + "_Dirty")
                 })
        {
            AssertDirtySpriteHasConspicuousFixedGrime(
                TextureFile(root, clean + ".png"),
                TextureFile(root, dirty + ".png"),
                TextureFile(root, clean + "_m.png"),
                TextureFile(root, dirty + "_m.png"));
        }
    }

    [Test]
    public void Building_texture_variation_family_is_complete_distinct_and_chroma_free()
    {
        var root = FindRepositoryRoot();
        var texturePaths = new[]
        {
            DishwasherTexturePath,
            IndustrialDishwasherTexturePath,
            PrepStationTexturePath,
            SauceStationTexturePath,
            MeatStationTexturePath,
            VegetableStationTexturePath,
            PastryStationTexturePath,
            MicrowaveTexturePath
        };

        foreach (var texturePath in texturePaths)
        {
            var basePath = TextureFile(root, texturePath + "_north.png");
            var variantPath = TextureFile(root, texturePath + "_Variant01_north.png");
            var packagedVariantPath = PackagedTextureFile(root, texturePath + "_Variant01_north.png");

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(variantPath), Is.True, texturePath + " source variant");
                Assert.That(File.Exists(packagedVariantPath), Is.True, texturePath + " packaged variant");
            });
            AssertPackagedTextureMatchesSource(variantPath, packagedVariantPath);
            AssertTransparentSprite(variantPath);
            AssertSameCanvas(basePath, variantPath);
            AssertNoVividGreenChroma(variantPath);
            AssertNoBrightBlueEmission(variantPath);
            AssertSpritesDiffer(basePath, variantPath);
        }
    }

    [Test]
    public void Every_rotatable_custom_building_uses_complete_authored_directional_families()
    {
        var root = FindRepositoryRoot();
        var documents = new[]
        {
            Path.Combine(root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "KitchenBuildings.xml"),
            Path.Combine(root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "PreparedFoodAndStation.xml"),
            Path.Combine(root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "AssistantStations.xml"),
            Path.Combine(root, "mods", "ImmersiveChefs", "Patches", "Compatibility", "ThermodynamicsHotMeals.xml")
        }
            .Select(XDocument.Load)
            .ToArray();
        var defs = documents
            .SelectMany(document => document.Descendants("ThingDef"))
            .Where(element => element.Element("defName") is not null)
            .ToDictionary(element => (string)element.Element("defName")!);
        var buildings = new[]
        {
            ("ImmersiveChefs_Dishwasher", DishwasherTexturePath, 5, 3),
            ("ImmersiveChefs_IndustrialDishwasher", IndustrialDishwasherTexturePath, 7, 3),
            ("ImmersiveChefs_PrepStation", PrepStationTexturePath, 7, 3),
            ("ImmersiveChefs_SauceStation", SauceStationTexturePath, 5, 3),
            ("ImmersiveChefs_MeatStation", MeatStationTexturePath, 5, 3),
            ("ImmersiveChefs_VegetableStation", VegetableStationTexturePath, 5, 3),
            ("ImmersiveChefs_PastryStation", PastryStationTexturePath, 5, 3),
            ("ImmersiveChefs_Microwave", MicrowaveTexturePath, 1, 1)
        };
        var directions = new[] { "north", "east", "south", "west" };

        foreach (var (defName, texturePath, widthUnits, heightUnits) in buildings)
        {
            var graphicData = defs[defName].Element("graphicData")!;
            Assert.That(
                (string?)graphicData.Element("graphicClass"),
                Is.EqualTo("Graphic_Multi"),
                defName + " must use authored direction-specific building art.");

            foreach (var familyPath in new[] { texturePath, texturePath + "_Variant01" })
            {
                Assert.Multiple(() =>
                {
                    Assert.That(
                        File.Exists(TextureFile(root, familyPath + ".png")),
                        Is.False,
                        familyPath + " must not retain the rejected single-view source raster.");
                    Assert.That(
                        File.Exists(PackagedTextureFile(root, familyPath + ".png")),
                        Is.False,
                        familyPath + " must not package the rejected single-view raster.");
                    foreach (var direction in directions)
                    {
                        Assert.That(
                            File.Exists(TextureFile(root, familyPath + "_" + direction + ".png")),
                            Is.True,
                            familyPath + " source " + direction);
                        Assert.That(
                            File.Exists(PackagedTextureFile(root, familyPath + "_" + direction + ".png")),
                            Is.True,
                            familyPath + " packaged " + direction);
                    }
                });

                var north = TextureFile(root, familyPath + "_north.png");
                var east = TextureFile(root, familyPath + "_east.png");
                var south = TextureFile(root, familyPath + "_south.png");
                var west = TextureFile(root, familyPath + "_west.png");
                foreach (var direction in directions)
                {
                    var source = TextureFile(root, familyPath + "_" + direction + ".png");
                    var packaged = PackagedTextureFile(root, familyPath + "_" + direction + ".png");
                    AssertPackagedTextureMatchesSource(source, packaged);
                    AssertTransparentSprite(source);
                    AssertNoVividGreenChroma(source);
                    AssertNoBrightBlueEmission(source);
                }

                AssertCanvasAspect(north, widthUnits, heightUnits);
                AssertCanvasAspect(south, widthUnits, heightUnits);
                AssertCanvasAspect(east, heightUnits, widthUnits);
                AssertCanvasAspect(west, heightUnits, widthUnits);
                AssertSpritesDiffer(north, south);
                AssertSpritesDiffer(east, west);
                AssertNotExactHalfTurn(north, south);
                AssertNotExactHalfTurn(east, west);
            }
        }
    }

    [Test]
    public void Workbench_families_follow_the_measured_core_projection_templates()
    {
        var root = FindRepositoryRoot();
        var geometryPath = Path.Combine(root, "docs", "WorkbenchSpriteGeometry.xml");
        Assert.That(File.Exists(geometryPath), Is.True, "The measured Core projection must be durable.");

        var geometry = XDocument.Load(geometryPath).Root!;
        var baseline = geometry.Element("coreBaseline")!;
        var samples = baseline.Element("samples")!.Elements("texture").ToArray();
        Assert.Multiple(() =>
        {
            Assert.That((string?)geometry.Attribute("gameVersion"), Is.EqualTo("1.6.4871 rev590"));
            Assert.That((string?)baseline.Attribute("footprint"), Is.EqualTo("3x1"));
            Assert.That((string?)baseline.Attribute("drawSize"), Is.EqualTo("3.5,1.5"));
            Assert.That((string?)baseline.Attribute("pixelsPerCell"), Is.EqualTo("64"));
            Assert.That(samples.Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(
                samples.Single(element =>
                    (string?)element.Attribute("name") == "TableTailorHand")
                    .Attribute("canonicalBareTable")?.Value,
                Is.EqualTo("true"));
        });

        var templates = geometry.Element("immersiveChefsTemplates")!
            .Elements("template")
            .ToDictionary(
                element => (string)element.Attribute("footprint")!,
                element => element,
                StringComparer.Ordinal);
        var outlineStrokes = XDocument.Load(Path.Combine(root, "docs", "SpriteOutlineApprovals.xml"))
            .Root!
            .Elements("sprite")
            .ToDictionary(
                element => (string)element.Attribute("path")!,
                element => (int)element.Attribute("selectedStrokeSourcePixels")!,
                StringComparer.Ordinal);
        var documents = new[]
        {
            Path.Combine(root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "KitchenBuildings.xml"),
            Path.Combine(root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "PreparedFoodAndStation.xml"),
            Path.Combine(root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "AssistantStations.xml")
        }
            .Select(XDocument.Load)
            .ToArray();
        var defs = documents
            .SelectMany(document => document.Descendants("ThingDef"))
            .Where(element => element.Element("defName") is not null)
            .ToDictionary(element => (string)element.Element("defName")!);
        var families = new[]
        {
            ("ImmersiveChefs_Dishwasher", DishwasherTexturePath, "2x1"),
            ("ImmersiveChefs_IndustrialDishwasher", IndustrialDishwasherTexturePath, "3x1"),
            ("ImmersiveChefs_PrepStation", PrepStationTexturePath, "3x1"),
            ("ImmersiveChefs_SauceStation", SauceStationTexturePath, "2x1"),
            ("ImmersiveChefs_MeatStation", MeatStationTexturePath, "2x1"),
            ("ImmersiveChefs_VegetableStation", VegetableStationTexturePath, "2x1"),
            ("ImmersiveChefs_PastryStation", PastryStationTexturePath, "2x1")
        };

        foreach (var (defName, texturePath, footprint) in families)
        {
            var template = templates[footprint];
            var expectedDrawSize = "(" + (string)template.Attribute("drawSize")! + ")";
            var horizontal = ParseCanvas((string)template.Attribute("horizontalCanvas")!);
            var vertical = ParseCanvas((string)template.Attribute("verticalCanvas")!);
            var horizontalTabletop = ParseRectangle((string)template.Attribute("horizontalTabletop")!);
            var horizontalUnderframe = ParseRectangle(
                (string)template.Attribute("horizontalScreenSouthUnderframe")!);
            var verticalTabletop = ParseRectangle((string)template.Attribute("verticalTabletop")!);
            var verticalUnderframe = ParseRectangle(
                (string)template.Attribute("verticalScreenSouthUnderframe")!);
            Assert.That(
                (string?)defs[defName].Element("graphicData")!.Element("drawSize"),
                Is.EqualTo(expectedDrawSize),
                defName + " must use its measured Core-derived draw canvas.");

            foreach (var variant in new[] { string.Empty, "_Variant01" })
            {
                var paths = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var (direction, expectedCanvas) in new[]
                         {
                             ("north", horizontal),
                             ("east", vertical),
                             ("south", horizontal),
                             ("west", vertical)
                         })
                {
                    var path = TextureFile(root, texturePath + variant + "_" + direction + ".png");
                    var outlinePath = (texturePath + variant + "_" + direction + ".png")
                        .Substring("ImmersiveChefs/Things/".Length);
                    paths.Add(direction, path);
                    using var bitmap = new System.Drawing.Bitmap(path);
                    Assert.That(
                        (bitmap.Width, bitmap.Height),
                        Is.EqualTo(expectedCanvas),
                        path + " must use the exact normalized projection canvas.");
                    AssertMeasuredProjection(
                        bitmap,
                        direction is "north" or "south" ? horizontalTabletop : verticalTabletop,
                        direction is "north" or "south" ? horizontalUnderframe : verticalUnderframe,
                        outlineStrokes[outlinePath],
                        path);
                    AssertNoGreenSilhouetteFringe(path);
                }

                AssertOppositeUnderframesUseTheSameProjection(
                    paths["north"],
                    paths["south"],
                    horizontalUnderframe);
                AssertOppositeUnderframesUseTheSameProjection(
                    paths["east"],
                    paths["west"],
                    verticalUnderframe);
            }
        }
    }

    [Test]
    public void Reviewed_cardinal_sprites_are_pinned_to_fixed_camera_landmark_approvals()
    {
        var root = FindRepositoryRoot();
        var approvalPath = Path.Combine(root, "docs", "DirectionalSpriteApprovals.xml");
        Assert.That(
            File.Exists(approvalPath),
            Is.True,
            "Directional art needs a reviewed landmark manifest before it can be accepted.");

        var approvedFrames = XDocument.Load(approvalPath)
            .Root!
            .Elements("frame")
            .ToDictionary(
                element => (string)element.Attribute("path")!,
                element => element,
                StringComparer.Ordinal);
        var expectedPaths = new[]
        {
            ("Dishwasher", "Dishwasher"),
            ("Dishwasher", "IndustrialDishwasher"),
            ("KitchenStation", "PrepStation"),
            ("KitchenStation", "SauceStation"),
            ("KitchenStation", "MeatStation"),
            ("KitchenStation", "VegetableStation"),
            ("KitchenStation", "PastryStation"),
            ("Appliance", "Microwave")
        }
            .SelectMany(family => new[] { string.Empty, "_Variant01" }
                .SelectMany(variant => new[] { "north", "east", "south", "west" }
                    .Select(direction =>
                        family.Item1 + "/" + family.Item2 + variant + "_" + direction + ".png")))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.That(
            approvedFrames.Keys,
            Is.EquivalentTo(expectedPaths),
            "Every corrected cardinal frame needs one explicit reviewed landmark approval.");

        var textureRoot = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            "ImmersiveChefs",
            "Things",
            "Building");
        foreach (var path in expectedPaths)
        {
            var approval = approvedFrames[path];
            var landmarks = (string?)approval.Attribute("landmarks") ?? string.Empty;
            var equipmentOrder = (string?)approval.Attribute("equipmentOrder") ?? string.Empty;
            var isMicrowave = path.StartsWith("Appliance/Microwave", StringComparison.Ordinal);
            var expectedHash = (string?)approval.Attribute("sha256") ?? string.Empty;
            var texturePath = Path.Combine(
                textureRoot,
                path.Replace('/', Path.DirectorySeparatorChar));

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(texturePath), Is.True, path);
                Assert.That(
                    landmarks,
                    Does.Contain(isMicrowave ? "countertop body" : "screen-bottom underframe"),
                    path + " must record the fixed-camera body landmark.");
                if (!isMicrowave)
                {
                    Assert.That(
                        equipmentOrder.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Length,
                        Is.GreaterThanOrEqualTo(2),
                        path + " must record its direction-specific world-space equipment order.");
                }
                Assert.That(
                    expectedHash,
                    Does.Match("^[0-9a-f]{64}$"),
                    path + " must pin the exact visually reviewed sprite.");
            });
            if (!File.Exists(texturePath))
            {
                continue;
            }

            Assert.That(
                Sha256(texturePath),
                Is.EqualTo(expectedHash),
                path + " changed after its directional landmark review.");
        }

        foreach (var family in expectedPaths
                     .Select(path => path.Substring(0, path.LastIndexOf('_')))
                     .Distinct(StringComparer.Ordinal)
                     .Where(family => !family.StartsWith("Appliance/Microwave", StringComparison.Ordinal)))
        {
            var north = EquipmentOrder(approvedFrames[family + "_north.png"]);
            var east = EquipmentOrder(approvedFrames[family + "_east.png"]);
            var south = EquipmentOrder(approvedFrames[family + "_south.png"]);
            var west = EquipmentOrder(approvedFrames[family + "_west.png"]);
            Assert.Multiple(() =>
            {
                Assert.That(east, Is.EqualTo(north), family + " east must rotate north's order clockwise.");
                Assert.That(south, Is.EqualTo(north.Reverse()), family + " south must reverse north's order.");
                Assert.That(west, Is.EqualTo(north.Reverse()), family + " west must rotate north's order counter-clockwise.");
            });
        }

        foreach (var family in new[] { "Appliance/Microwave", "Appliance/Microwave_Variant01" })
        {
            Assert.Multiple(() =>
            {
                Assert.That((string?)approvedFrames[family + "_north.png"].Attribute("visibleFace"),
                    Is.EqualTo("rear-bottom-full"), family + " north must show the blank rear under RimWorld's fixed camera.");
                Assert.That((string?)approvedFrames[family + "_south.png"].Attribute("visibleFace"),
                    Is.EqualTo("door-bottom-full"), family + " south must show the local-south microwave front.");
                Assert.That((string?)approvedFrames[family + "_east.png"].Attribute("visibleFace"),
                    Is.EqualTo("door-edge-right"), family + " east must show the front terminal edge at screen right.");
                Assert.That((string?)approvedFrames[family + "_west.png"].Attribute("visibleFace"),
                    Is.EqualTo("door-edge-left"), family + " west must show the front terminal edge at screen left.");
            });
        }
    }

    [Test]
    public void Microwave_door_and_controls_follow_the_reviewed_cardinal_face_in_real_pixels()
    {
        var root = FindRepositoryRoot();
        var defPatch = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Patches",
            "Compatibility",
            "ThermodynamicsHotMeals.xml"));
        var microwaveDef = defPatch.Descendants("ThingDef")
            .Single(element => (string?)element.Element("defName") == "ImmersiveChefs_Microwave");
        Assert.That(
            (string?)microwaveDef.Element("interactionCellOffset"),
            Is.EqualTo("(0,0,-1)"),
            "The visual correction must preserve the published interaction convention and existing-save rotation semantics.");
        var textureRoot = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            "ImmersiveChefs",
            "Things",
            "Building",
            "Appliance");
        var approvals = XDocument.Load(Path.Combine(root, "docs", "DirectionalSpriteApprovals.xml"))
            .Root!
            .Elements("frame")
            .Where(element => ((string?)element.Attribute("path"))?.StartsWith("Appliance/Microwave", StringComparison.Ordinal) == true)
            .ToDictionary(element => (string)element.Attribute("path")!, StringComparer.Ordinal);
        var projectionContract = XDocument.Load(Path.Combine(root, "docs", "MicrowaveProjectionBaseline.xml"))
            .Root!
            .Element("projectionContract")!;
        var minimumCasingToTopRatio = double.Parse(
            (string)projectionContract.Attribute("minimumCasingToTopRatio")!,
            CultureInfo.InvariantCulture);
        var maximumCasingToTopRatio = double.Parse(
            (string)projectionContract.Attribute("maximumCasingToTopRatio")!,
            CultureInfo.InvariantCulture);
        var measuredFrames = new Dictionary<string, List<(string Direction, int TopDepth, int CasingDepth)>>(
            StringComparer.Ordinal);

        foreach (var family in new[] { "Microwave", "Microwave_Variant01" })
        {
            foreach (var direction in new[] { "north", "east", "south", "west" })
            {
                using var bitmap = new Bitmap(Path.Combine(textureRoot, family + "_" + direction + ".png"));
                var bounds = AlphaBounds(bitmap);
                var approvalPath = "Appliance/" + family + "_" + direction + ".png";
                var approval = approvals[approvalPath];
                var approvedBounds = ParseRectangle((string)approval.Attribute("alphaBounds")!);
                var topPlane = ParseRectangle((string)approval.Attribute("topPlane")!);
                var nearCasing = ParseRectangle((string)approval.Attribute("nearCasing")!);
                var bevelBand = ParseRectangle((string)approval.Attribute("bevelBand")!);
                var topDepth = int.Parse((string)approval.Attribute("topDepth")!);
                var casingDepth = int.Parse((string)approval.Attribute("casingDepth")!);
                var projection = (string)approval.Attribute("projection")!;
                if (!measuredFrames.TryGetValue(projection, out var projectionFrames))
                {
                    projectionFrames = new List<(string Direction, int TopDepth, int CasingDepth)>();
                    measuredFrames.Add(projection, projectionFrames);
                }

                projectionFrames.Add((family + "_" + direction, topDepth, casingDepth));
                Assert.Multiple(() =>
                {
                    Assert.That(bitmap.Width, Is.EqualTo(512), family + "_" + direction + " canvas width");
                    Assert.That(bitmap.Height, Is.EqualTo(512), family + "_" + direction + " canvas height");
                    Assert.That(bounds, Is.EqualTo(approvedBounds), family + "_" + direction + " approved alpha bounds");
                    Assert.That(bounds.Left, Is.GreaterThanOrEqualTo(24), family + "_" + direction + " left clearance");
                    Assert.That(bounds.Top, Is.GreaterThanOrEqualTo(24), family + "_" + direction + " top clearance");
                    Assert.That(bitmap.Width - bounds.Right, Is.GreaterThanOrEqualTo(24), family + "_" + direction + " right clearance");
                    Assert.That(bitmap.Height - bounds.Bottom, Is.GreaterThanOrEqualTo(24), family + "_" + direction + " bottom clearance");
                    Assert.That(AlphaCoverage(bitmap, topPlane), Is.GreaterThanOrEqualTo(0.98), family + "_" + direction + " measured top plane");
                    Assert.That(AlphaCoverage(bitmap, nearCasing), Is.GreaterThanOrEqualTo(0.98), family + "_" + direction + " measured near casing");
                    Assert.That(AlphaCoverage(bitmap, bevelBand), Is.GreaterThanOrEqualTo(0.90), family + "_" + direction + " measured bevel band");
                    Assert.That(Math.Max(bounds.Width, bounds.Height), Is.LessThanOrEqualTo(456), family + "_" + direction + " compact long axis");
                    Assert.That(
                        casingDepth / (double)topDepth,
                        Is.InRange(minimumCasingToTopRatio, maximumCasingToTopRatio),
                        family + "_" + direction + " must match the measured Thermodynamics countertop-microwave top/casing projection rather than becoming a tall cube or flat deck");
                    Assert.That(
                        direction is "north" or "south" ? bounds.Width / (double)bounds.Height : bounds.Height / (double)bounds.Width,
                        Is.GreaterThanOrEqualTo(1.10),
                        family + "_" + direction + " must use RimWorld's axis-aligned horizontal/vertical cardinal projection");
                    Assert.That(
                        projection,
                        Is.EqualTo(direction is "north" or "south" ? family + "-horizontal" : family + "-vertical"),
                        family + "_" + direction + " fixed-camera projection group");
                });

                if (direction is "east" or "west")
                {
                    var sidePlane = ParseRectangle((string)approval.Attribute("sidePlane")!);
                    Assert.That(
                        AlphaCoverage(bitmap, sidePlane),
                        Is.GreaterThanOrEqualTo(0.98),
                        family + "_" + direction + " measured axis-aligned side plane");
                }

                if (direction == "north")
                {
                    var rearFeature = ParseRectangle((string)approval.Attribute("rearFeature")!);
                    Assert.That(
                        DarkPixelFraction(bitmap, rearFeature),
                        Is.LessThan(0.18),
                        family + "_north must retain a blank rear field rather than painting the door onto it.");
                    continue;
                }

                var frontFeature = ParseRectangle((string)approval.Attribute("frontFeature")!);
                Assert.That(
                    DarkPixelFraction(bitmap, frontFeature),
                    Is.GreaterThan(direction == "south" ? 0.45 : 0.08),
                    family + "_" + direction + " must retain its readable door/control landmark on the approved front face.");
                if (direction == "south")
                {
                    Assert.That(
                        frontFeature.Width / (double)frontFeature.Height,
                        Is.InRange(2.0, 3.0),
                        family + "_south must retain a microwave-shaped cavity instead of a VHS/VCR letterbox slot.");
                }
            }
        }

        foreach (var (projection, frames) in measuredFrames)
        {
            Assert.That(frames, Has.Count.EqualTo(2), projection + " must contain one cardinal-opposite pair.");
            Assert.Multiple(() =>
            {
                Assert.That(
                    Math.Abs(frames[0].TopDepth - frames[1].TopDepth),
                    Is.LessThanOrEqualTo(25),
                    projection + " opposite frames must share one measured top projection.");
                Assert.That(
                    Math.Abs(frames[0].CasingDepth - frames[1].CasingDepth),
                    Is.LessThanOrEqualTo(25),
                    projection + " opposite frames must share one measured shallow casing projection.");
            });
        }
    }

    [Test]
    public void Ordinary_cookware_uses_owned_stuffable_art_with_a_matching_mask()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_Cookware");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, CookwareTexturePath + ".png");
        var maskPath = TextureFile(root, CookwareTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(CookwareTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("ImmersiveChefs.Graphic_PortableKitchenwareVariation"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That(File.Exists(diffusePath), Is.True, "The selected cookware source sprite must exist.");
            Assert.That(File.Exists(maskPath), Is.True, "Stuffable cookware needs a source RimWorld mask.");
            Assert.That(File.Exists(PackagedTextureFile(root, CookwareTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, CookwareTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: true);
    }

    [Test]
    public void Primitive_cookware_uses_distinct_stuffable_art_with_permanent_wood_accents()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_PrimitiveCookware");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, PrimitiveCookwareTexturePath + ".png");
        var maskPath = TextureFile(root, PrimitiveCookwareTexturePath + "_m.png");
        var dirtyPath = TextureFile(root, PrimitiveCookwareTexturePath + "_Dirty.png");
        var dirtyMaskPath = TextureFile(root, PrimitiveCookwareTexturePath + "_Dirty_m.png");
        var modernPath = TextureFile(root, CookwareTexturePath + ".png");
        var oldStoneVariantPath = TextureFile(root, CookwareTexturePath + "_Stone.png");
        var packagedPaths = new[]
        {
            (diffusePath, PackagedTextureFile(root, PrimitiveCookwareTexturePath + ".png")),
            (maskPath, PackagedTextureFile(root, PrimitiveCookwareTexturePath + "_m.png")),
            (dirtyPath, PackagedTextureFile(root, PrimitiveCookwareTexturePath + "_Dirty.png")),
            (dirtyMaskPath, PackagedTextureFile(root, PrimitiveCookwareTexturePath + "_Dirty_m.png"))
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)graphicData.Element("texPath"),
                Is.EqualTo(PrimitiveCookwareTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("ImmersiveChefs.Graphic_PortableKitchenwareVariation"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(maskPath), Is.True);
            Assert.That(File.Exists(dirtyPath), Is.True);
            Assert.That(File.Exists(dirtyMaskPath), Is.True);
            Assert.That(File.ReadAllBytes(diffusePath), Is.Not.EqualTo(File.ReadAllBytes(modernPath)));
            Assert.That(File.ReadAllBytes(diffusePath), Is.Not.EqualTo(File.ReadAllBytes(oldStoneVariantPath)));
            Assert.That(packagedPaths.Select(pair => pair.Item2), Is.All.Matches<string>(File.Exists));
        });

        foreach (var (sourcePath, packagedPath) in packagedPaths)
        {
            AssertPackagedTextureMatchesSource(sourcePath, packagedPath);
            using var packaged = new Bitmap(packagedPath);
            Assert.Multiple(() =>
            {
                Assert.That(packaged.Width, Is.EqualTo(256), packagedPath);
                Assert.That(packaged.Height, Is.EqualTo(256), packagedPath);
            });
        }

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: true);
        AssertTransparentMatchingPair(dirtyPath, dirtyMaskPath, requireFixedBlackRegion: true);
        AssertVisibleBounds(diffusePath, 170, 230, 170, 230);
    }

    [Test]
    public void Preview_pipeline_packages_exact_sixteen_by_nine_images_below_one_mebibyte()
    {
        var root = FindRepositoryRoot();
        var scriptPath = Path.Combine(root, "scripts", "Build-ImmersiveChefsPreview.ps1");
        var workshopPath = Path.Combine(
            root, "mods", "ImmersiveChefs", "Release", "workshop", "preview-main.png");
        var aboutPath = Path.Combine(root, "mods", "ImmersiveChefs", "About", "Preview.png");
        var packagedAboutPath = Path.Combine(
            root, "artifacts", "Mods", "fumblesneeze.immersivechefs", "About", "Preview.png");

        Assert.That(File.Exists(scriptPath), Is.True, "The preview must have a deterministic renderer.");
        var script = File.ReadAllText(scriptPath);
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("YOU DONKEY!"));
            Assert.That(script, Does.Contain("IMMERSIVE CHEFS"));
            Assert.That(script, Does.Contain("1280x720"));
            Assert.That(script, Does.Contain("640x360"));
            Assert.That(File.Exists(workshopPath), Is.True);
            Assert.That(File.Exists(aboutPath), Is.True);
            Assert.That(File.Exists(packagedAboutPath), Is.True);
        });

        AssertPreview(workshopPath, 1280, 720);
        AssertPreview(aboutPath, 640, 360);
        AssertSpeechBubbleTextCentered(
            workshopPath,
            new Rectangle(620, 40, 552, 162),
            maximumCenterOffset: 4.0);
        AssertSpeechBubbleTextCentered(
            aboutPath,
            new Rectangle(310, 20, 276, 81),
            maximumCenterOffset: 2.0);

        var temporaryRoot = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "immersive-chefs-preview-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var renderedWorkshop = Path.Combine(temporaryRoot, "workshop.png");
            var renderedAbout = Path.Combine(temporaryRoot, "about.png");
            var result = RunPreviewRenderer(scriptPath, renderedWorkshop, renderedAbout);

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardError);
                Assert.That(
                    File.ReadAllBytes(renderedWorkshop),
                    Is.EqualTo(File.ReadAllBytes(workshopPath)),
                    "The committed Workshop preview must be byte-for-byte reproducible.");
                Assert.That(
                    File.ReadAllBytes(renderedAbout),
                    Is.EqualTo(File.ReadAllBytes(aboutPath)),
                    "The committed About preview must be byte-for-byte reproducible.");
            });

            var sharedOutput = Path.Combine(temporaryRoot, "same.png");
            var invalid = RunPreviewRenderer(scriptPath, sharedOutput, sharedOutput);
            Assert.Multiple(() =>
            {
                Assert.That(invalid.ExitCode, Is.EqualTo(2));
                Assert.That(invalid.StandardError, Does.Contain("distinct"));
            });
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void Plate_family_uses_its_packaged_stuff_mask_in_base_and_optional_variation_graphics()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var defs = document.Root!.Elements("ThingDef")
            .Where(element => element.Element("defName") is not null)
            .ToDictionary(element => (string)element.Element("defName")!);
        var plateGraphic = defs["ImmersiveChefs_Plate"].Element("graphicData")!;
        var adobeGraphic = defs["ImmersiveChefs_AdobePlate"].Element("graphicData")!;
        var diffusePath = TextureFile(root, PlateTexturePath + ".png");
        var optionalVariationMaskPath = TextureFile(root, PlateTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)plateGraphic.Element("texPath"), Is.EqualTo(PlateTexturePath));
            Assert.That((string?)plateGraphic.Element("graphicClass"), Is.EqualTo("ImmersiveChefs.Graphic_PortableKitchenwareVariation"));
            Assert.That((string?)plateGraphic.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That((string?)adobeGraphic.Element("texPath"), Is.EqualTo(PlateTexturePath));
            Assert.That((string?)adobeGraphic.Element("graphicClass"), Is.EqualTo("ImmersiveChefs.Graphic_PortableKitchenwareVariation"));
            Assert.That((string?)adobeGraphic.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(
                File.Exists(optionalVariationMaskPath),
                Is.True,
                "Base and VTEX graphics share the packaged Stuff mask.");
            Assert.That(File.Exists(PackagedTextureFile(root, PlateTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, PlateTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, optionalVariationMaskPath, requireFixedBlackRegion: false);
    }

    [Test]
    public void Cutlery_setting_uses_owned_single_sprite_art_with_a_stuff_mask()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_Cutlery");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, CutleryTexturePath + ".png");
        var maskPath = TextureFile(root, CutleryTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(CutleryTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("ImmersiveChefs.Graphic_PortableKitchenwareVariation"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(maskPath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, CutleryTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, CutleryTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: false);
    }

    [Test]
    public void Chefs_knife_set_uses_owned_single_sprite_art_with_a_stuff_mask()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_ChefsKnife");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, ChefsKnifeTexturePath + ".png");
        var maskPath = TextureFile(root, ChefsKnifeTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(ChefsKnifeTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(maskPath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, ChefsKnifeTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, ChefsKnifeTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: true);
    }

    [Test]
    public void Glitterworld_cookware_uses_owned_fixed_color_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_GlitterworldCookware");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, GlitterCookwareTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(GlitterCookwareTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That(graphicData.Element("color"), Is.Null, "Fixed glitter art must retain its authored color.");
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, GlitterCookwareTexturePath + ".png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
    }

    [Test]
    public void Prepared_ingredients_use_owned_fixed_color_single_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "PreparedFoodAndStation.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_PreparedFood");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, PreparedFoodTexturePath + ".png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(PreparedFoodTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, PreparedFoodTexturePath + ".png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
    }

    [Test]
    public void Domestic_dishwasher_uses_owned_directional_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "KitchenBuildings.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_Dishwasher");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, DishwasherTexturePath + "_north.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(DishwasherTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Multi"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(2.5,1.5)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, DishwasherTexturePath + "_north.png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
    }

    [Test]
    public void Industrial_dishwasher_uses_owned_directional_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "KitchenBuildings.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_IndustrialDishwasher");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, IndustrialDishwasherTexturePath + "_north.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(IndustrialDishwasherTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Multi"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(3.5,1.5)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, IndustrialDishwasherTexturePath + "_north.png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
    }

    [Test]
    public void Ingredient_prep_station_uses_owned_directional_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "PreparedFoodAndStation.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_PrepStation");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, PrepStationTexturePath + "_north.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(PrepStationTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Multi"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(3.5,1.5)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, PrepStationTexturePath + "_north.png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
        if (File.Exists(diffusePath))
        {
            using var bitmap = new Bitmap(diffusePath);
            Assert.That(
                bitmap.Width * 15,
                Is.EqualTo(bitmap.Height * 35),
                "The texture canvas must match the 3.5:1.5 draw mesh without Unity stretching it.");
        }
    }

    [Test]
    public void Sauce_station_uses_owned_directional_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "AssistantStations.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_SauceStation");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, SauceStationTexturePath + "_north.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(SauceStationTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Multi"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(2.5,1.5)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, SauceStationTexturePath + "_north.png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
        AssertCanvasAspect(diffusePath, widthUnits: 5, heightUnits: 3);
        AssertNoBrightBlueEmission(diffusePath);
    }

    [Test]
    public void Meat_station_uses_owned_directional_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "AssistantStations.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_MeatStation");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, MeatStationTexturePath + "_north.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(MeatStationTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Multi"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(2.5,1.5)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, MeatStationTexturePath + "_north.png")), Is.True);
        });

        AssertTransparentSprite(diffusePath);
        AssertCanvasAspect(diffusePath, widthUnits: 5, heightUnits: 3);
        AssertNoBrightBlueEmission(diffusePath);
    }

    [Test]
    public void Vegetable_station_uses_owned_directional_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "AssistantStations.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_VegetableStation");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, VegetableStationTexturePath + "_north.png");
        var packagedPath = PackagedTextureFile(root, VegetableStationTexturePath + "_north.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(VegetableStationTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Multi"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(2.5,1.5)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(packagedPath), Is.True);
        });

        AssertPackagedTextureMatchesSource(diffusePath, packagedPath);
        AssertTransparentSprite(diffusePath);
        AssertCanvasAspect(diffusePath, widthUnits: 5, heightUnits: 3);
        AssertNoVividGreenChroma(diffusePath);
        AssertNoBrightBlueEmission(diffusePath);
    }

    [Test]
    public void Pastry_station_uses_owned_directional_sprite_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "AssistantStations.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_PastryStation");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, PastryStationTexturePath + "_north.png");
        var packagedPath = PackagedTextureFile(root, PastryStationTexturePath + "_north.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(PastryStationTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Multi"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(2.5,1.5)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(packagedPath), Is.True);
        });

        AssertPackagedTextureMatchesSource(diffusePath, packagedPath);
        AssertTransparentSprite(diffusePath);
        AssertCanvasAspect(diffusePath, widthUnits: 5, heightUnits: 3);
        AssertNoVividGreenChroma(diffusePath);
        AssertNoBrightBlueEmission(diffusePath);
    }

    [Test]
    public void Fallback_microwave_uses_compact_owned_countertop_art()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Patches",
            "Compatibility",
            "ThermodynamicsHotMeals.xml"));
        var def = document.Descendants("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_Microwave");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, MicrowaveTexturePath + "_north.png");
        var packagedPath = PackagedTextureFile(root, MicrowaveTexturePath + "_north.png");
        var obsoleteSourcePath = TextureFile(root, "Things/Building/Microwave/Microwave.png");
        var obsoletePackagedPath = PackagedTextureFile(root, "Things/Building/Microwave/Microwave.png");
        var outlineStroke = XDocument.Load(Path.Combine(root, "docs", "SpriteOutlineApprovals.xml"))
            .Root!
            .Elements("sprite")
            .Single(element =>
                (string?)element.Attribute("path") == "Building/Appliance/Microwave_north.png")
            .Attribute("selectedStrokeSourcePixels")!
            .Value;
        var addedDiameter = int.Parse(outlineStroke) * 2;

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(MicrowaveTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Multi"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("Cutout"));
            Assert.That((string?)graphicData.Element("drawSize"), Is.EqualTo("(1.25,1.35)"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(packagedPath), Is.True);
            Assert.That(File.Exists(obsoleteSourcePath), Is.False);
            Assert.That(
                File.Exists(obsoletePackagedPath),
                Is.False,
                "Rejected microwave art must not survive incremental package staging.");
        });

        AssertPackagedTextureMatchesSource(diffusePath, packagedPath);
        AssertTransparentSprite(diffusePath);
        AssertCanvasAspect(diffusePath, widthUnits: 1, heightUnits: 1);
        AssertVisibleBounds(
            diffusePath,
            minimumWidth: 280 + addedDiameter,
            maximumWidth: 430 + addedDiameter,
            minimumHeight: 180 + addedDiameter,
            maximumHeight: 430 + addedDiameter);
        AssertNoVividGreenChroma(diffusePath);
        AssertNoBrightBlueEmission(diffusePath);
    }

    private static string TextureFile(string root, string texturePath)
    {
        return Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            texturePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string PackagedTextureFile(string root, string texturePath)
    {
        return Path.Combine(
            root,
            "artifacts",
            "Mods",
            "fumblesneeze.immersivechefs",
            "1.6",
            "Textures",
            texturePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void AssertTransparentMatchingPair(
        string diffusePath,
        string maskPath,
        bool requireFixedBlackRegion)
    {
        if (!File.Exists(diffusePath) || !File.Exists(maskPath))
        {
            return;
        }

        using var diffuse = new Bitmap(diffusePath);
        using var mask = new Bitmap(maskPath);
        var visibleDiffusePixels = 0;
        var redStuffPixels = 0;
        var fixedBlackPixels = 0;
        var alphaMismatches = 0;
        for (var y = 0; y < diffuse.Height; y++)
        {
            for (var x = 0; x < diffuse.Width; x++)
            {
                var diffusePixel = diffuse.GetPixel(x, y);
                var maskPixel = mask.GetPixel(x, y);
                if (diffusePixel.A > 0)
                {
                    visibleDiffusePixels++;
                }

                if (maskPixel.A > 0 && maskPixel.R >= 240 && maskPixel.G <= 15 && maskPixel.B <= 15)
                {
                    redStuffPixels++;
                }

                if (maskPixel.A > 0 && maskPixel.R <= 15 && maskPixel.G <= 15 && maskPixel.B <= 15)
                {
                    fixedBlackPixels++;
                }

                if (diffusePixel.A != maskPixel.A)
                {
                    alphaMismatches++;
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(diffuse.Size, Is.EqualTo(mask.Size));
            Assert.That(diffuse.Width, Is.GreaterThanOrEqualTo(128));
            Assert.That(diffuse.Height, Is.GreaterThanOrEqualTo(128));
            Assert.That(diffuse.GetPixel(0, 0).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(diffuse.Width - 1, 0).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(0, diffuse.Height - 1).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(diffuse.Width - 1, diffuse.Height - 1).A, Is.EqualTo(0));
            Assert.That(mask.GetPixel(0, 0).A, Is.EqualTo(0));
            Assert.That(visibleDiffusePixels, Is.GreaterThan(100));
            Assert.That(redStuffPixels, Is.GreaterThan(100), "The mask needs a visible red Stuff region.");
            Assert.That(alphaMismatches, Is.Zero, "Diffuse and mask silhouettes must match exactly.");
            if (requireFixedBlackRegion)
            {
                Assert.That(
                    fixedBlackPixels,
                    Is.GreaterThan(100),
                    "Fixed handles need a visible black mask region.");
            }
        });
    }

    private static void AssertTransparentSprite(string diffusePath)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var diffuse = new Bitmap(diffusePath);
        var visiblePixels = 0;
        for (var y = 0; y < diffuse.Height; y++)
        {
            for (var x = 0; x < diffuse.Width; x++)
            {
                if (diffuse.GetPixel(x, y).A > 0)
                {
                    visiblePixels++;
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(diffuse.Width, Is.GreaterThanOrEqualTo(128));
            Assert.That(diffuse.Height, Is.GreaterThanOrEqualTo(128));
            Assert.That(diffuse.GetPixel(0, 0).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(diffuse.Width - 1, 0).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(0, diffuse.Height - 1).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(diffuse.Width - 1, diffuse.Height - 1).A, Is.EqualTo(0));
            Assert.That(visiblePixels, Is.GreaterThan(100));
        });
    }

    private static void AssertCanvasAspect(string diffusePath, int widthUnits, int heightUnits)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var bitmap = new Bitmap(diffusePath);
        Assert.That(
            bitmap.Width * heightUnits,
            Is.EqualTo(bitmap.Height * widthUnits),
            "The texture canvas must match its draw mesh without Unity stretching it.");
    }

    private static void AssertSameCanvas(string expectedPath, string actualPath)
    {
        if (!File.Exists(expectedPath) || !File.Exists(actualPath))
        {
            return;
        }

        using var expected = new Bitmap(expectedPath);
        using var actual = new Bitmap(actualPath);
        Assert.That(actual.Size, Is.EqualTo(expected.Size));
    }

    private static void AssertNoBrightBlueEmission(string diffusePath)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var bitmap = new Bitmap(diffusePath);
        var brightBluePixels = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A >= 64 &&
                    pixel.B >= 100 &&
                    pixel.B > pixel.R * 1.25 &&
                    pixel.B > pixel.G * 1.15)
                {
                    brightBluePixels++;
                }
            }
        }

        Assert.That(
            brightBluePixels,
            Is.Zero,
            "Unconditional base art must not look powered while the building is off or broken.");
    }

    private static void AssertNoVividGreenChroma(string diffusePath)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var bitmap = new Bitmap(diffusePath);
        var vividGreenPixels = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A > 0 &&
                    pixel.G >= 128 &&
                    pixel.G > pixel.R * 1.4 &&
                    pixel.G > pixel.B * 1.4)
                {
                    vividGreenPixels++;
                }

            }
        }

        Assert.That(
            vividGreenPixels,
            Is.Zero,
            "The selected sprite must not retain vivid green chroma-key pixels.");
    }

    private static void AssertNoGreenSilhouetteFringe(string diffusePath)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var bitmap = new Bitmap(diffusePath);
        var greenSilhouetteEdgePixels = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A > 0 &&
                    pixel.G > pixel.R * 1.2 &&
                    pixel.G > pixel.B * 1.2 &&
                    pixel.G - Math.Max(pixel.R, pixel.B) >= 3 &&
                    IsSilhouetteEdge(bitmap, x, y, radius: 2))
                {
                    greenSilhouetteEdgePixels++;
                }
            }
        }

        Assert.That(
            greenSilhouetteEdgePixels,
            Is.Zero,
            diffusePath + " must not retain dark or low-alpha green chroma fringe on its silhouette.");
    }

    private static bool IsSilhouetteEdge(Bitmap bitmap, int x, int y, int radius)
    {
        for (var offsetY = -radius; offsetY <= radius; offsetY++)
        {
            for (var offsetX = -radius; offsetX <= radius; offsetX++)
            {
                if (offsetX * offsetX + offsetY * offsetY > radius * radius)
                {
                    continue;
                }

                var sampleX = x + offsetX;
                var sampleY = y + offsetY;
                if (sampleX < 0 || sampleY < 0 ||
                    sampleX >= bitmap.Width || sampleY >= bitmap.Height ||
                    bitmap.GetPixel(sampleX, sampleY).A == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void AssertPackagedTextureMatchesSource(string sourcePath, string packagedPath)
    {
        if (!File.Exists(sourcePath) || !File.Exists(packagedPath))
        {
            return;
        }

        Assert.That(
            File.ReadAllBytes(packagedPath),
            Is.EqualTo(File.ReadAllBytes(sourcePath)),
            "The staged texture must be the exact reviewed source asset.");
    }

    private static void AssertSpritesDiffer(string leftPath, string rightPath)
    {
        if (!File.Exists(leftPath) || !File.Exists(rightPath))
        {
            return;
        }

        using var left = new Bitmap(leftPath);
        using var right = new Bitmap(rightPath);
        Assert.That(right.Size, Is.EqualTo(left.Size));

        var visiblePixels = 0;
        var mismatches = 0;
        for (var y = 0; y < left.Height; y++)
        {
            for (var x = 0; x < left.Width; x++)
            {
                var leftPixel = left.GetPixel(x, y);
                var rightPixel = right.GetPixel(x, y);
                if (leftPixel.A > 0 || rightPixel.A > 0)
                {
                    visiblePixels++;
                }

                if (!PremultipliedPixelsMatch(leftPixel, rightPixel))
                {
                    mismatches++;
                }
            }
        }

        Assert.That(
            mismatches,
            Is.GreaterThan(visiblePixels / 100),
            "A selected variation must differ in more than PNG encoding or transparent-pixel metadata.");
    }

    private static void AssertDirtySpriteHasConspicuousFixedGrime(
        string cleanDiffusePath,
        string dirtyDiffusePath,
        string cleanMaskPath,
        string dirtyMaskPath)
    {
        using var clean = new Bitmap(cleanDiffusePath);
        using var dirty = new Bitmap(dirtyDiffusePath);
        using var cleanMask = new Bitmap(cleanMaskPath);
        using var dirtyMask = new Bitmap(dirtyMaskPath);
        Assert.Multiple(() =>
        {
            Assert.That(dirty.Size, Is.EqualTo(clean.Size), dirtyDiffusePath);
            Assert.That(cleanMask.Size, Is.EqualTo(clean.Size), cleanMaskPath);
            Assert.That(dirtyMask.Size, Is.EqualTo(clean.Size), dirtyMaskPath);
        });

        var materialSurfacePixels = 0;
        var fixedGrimePixels = 0;
        var highContrastGrimePixels = 0;
        for (var y = 0; y < clean.Height; y++)
        for (var x = 0; x < clean.Width; x++)
        {
            var cleanPixel = clean.GetPixel(x, y);
            var dirtyPixel = dirty.GetPixel(x, y);
            var cleanMaskPixel = cleanMask.GetPixel(x, y);
            var dirtyMaskPixel = dirtyMask.GetPixel(x, y);
            if (cleanPixel.A < 96 || cleanMaskPixel.R < 240 || cleanMaskPixel.G > 15 || cleanMaskPixel.B > 15)
            {
                continue;
            }

            materialSurfacePixels++;
            if (dirtyPixel.A < 96 || dirtyMaskPixel.R > 15 || dirtyMaskPixel.G > 15 || dirtyMaskPixel.B > 15)
            {
                continue;
            }

            fixedGrimePixels++;
            var cleanAfterSteelTint = Color.FromArgb(
                cleanPixel.A,
                cleanPixel.R * 105 / 255,
                cleanPixel.G * 105 / 255,
                cleanPixel.B * 105 / 255);
            var maximumChannelDifference = Math.Max(
                Math.Abs(dirtyPixel.R - cleanAfterSteelTint.R),
                Math.Max(
                    Math.Abs(dirtyPixel.G - cleanAfterSteelTint.G),
                    Math.Abs(dirtyPixel.B - cleanAfterSteelTint.B)));
            if (maximumChannelDifference >= 45)
            {
                highContrastGrimePixels++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(materialSurfacePixels, Is.GreaterThan(100), cleanDiffusePath + " has no measurable Stuff surface.");
            Assert.That(
                fixedGrimePixels,
                Is.GreaterThanOrEqualTo((int)Math.Ceiling(materialSurfacePixels * 0.12)),
                dirtyDiffusePath + " must reserve at least 12% of its Stuff surface for fixed-color grime.");
            Assert.That(
                highContrastGrimePixels,
                Is.GreaterThanOrEqualTo((int)Math.Ceiling(materialSurfacePixels * 0.10)),
                dirtyDiffusePath + " must remain unmistakably dirty after the Core Steel tint is applied.");
        });
    }

    private static void AssertNotExactHalfTurn(string sourcePath, string oppositePath)
    {
        if (!File.Exists(sourcePath) || !File.Exists(oppositePath))
        {
            return;
        }

        using var expected = new Bitmap(sourcePath);
        using var actual = new Bitmap(oppositePath);
        expected.RotateFlip(RotateFlipType.Rotate180FlipNone);
        Assert.That(actual.Size, Is.EqualTo(expected.Size));

        var mismatches = 0;
        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                if (!PremultipliedPixelsMatch(actual.GetPixel(x, y), expected.GetPixel(x, y)))
                {
                    mismatches++;
                }
            }
        }

        Assert.That(
            mismatches,
            Is.GreaterThan(expected.Width * expected.Height / 100),
            "An opposite cardinal sprite must be authored from RimWorld's fixed map camera, not manufactured by rotating another raster.");
    }

    private static bool PremultipliedPixelsMatch(Color left, Color right)
    {
        return left.A == right.A &&
               left.R * left.A == right.R * right.A &&
               left.G * left.A == right.G * right.A &&
               left.B * left.A == right.B * right.A;
    }

    private static string Sha256(string path)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(path);
        return BitConverter.ToString(sha256.ComputeHash(stream))
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }

    private static (int Width, int Height) ParseCanvas(string value)
    {
        var parts = value.Split('x');
        Assert.That(parts.Length, Is.EqualTo(2), "Invalid canvas measurement: " + value);
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    private static Rectangle ParseRectangle(string value)
    {
        var parts = value.Split(',');
        Assert.That(parts.Length, Is.EqualTo(4), "Invalid measured rectangle: " + value);
        return new Rectangle(
            int.Parse(parts[0]),
            int.Parse(parts[1]),
            int.Parse(parts[2]),
            int.Parse(parts[3]));
    }

    private static void AssertMeasuredProjection(
        Bitmap bitmap,
        Rectangle tabletop,
        Rectangle screenSouthUnderframe,
        int addedOutlinePixels,
        string path)
    {
        var expectedVisibleBounds = Rectangle.FromLTRB(
            tabletop.Left - addedOutlinePixels,
            tabletop.Top - addedOutlinePixels,
            tabletop.Right + addedOutlinePixels,
            screenSouthUnderframe.Bottom + addedOutlinePixels);
        Assert.Multiple(() =>
        {
            Assert.That(
                AlphaBounds(bitmap),
                Is.EqualTo(expectedVisibleBounds),
                path + " must use the exact Core-derived tabletop plus underframe bounds.");
            Assert.That(
                AlphaCoverage(bitmap, screenSouthUnderframe),
                Is.GreaterThanOrEqualTo(0.70),
                path + " must retain a substantial underframe in the screen-south projection band.");
        });
    }

    private static void AssertOppositeUnderframesUseTheSameProjection(
        string firstPath,
        string oppositePath,
        Rectangle underframe)
    {
        using var first = new Bitmap(firstPath);
        using var opposite = new Bitmap(oppositePath);
        Assert.That(
            Math.Abs(AlphaCoverage(first, underframe) - AlphaCoverage(opposite, underframe)),
            Is.LessThanOrEqualTo(0.08),
            "Opposite frames must keep a common fixed-camera body projection instead of rotating the apron away from screen-south.");
    }

    private static Rectangle AlphaBounds(Bitmap bitmap)
    {
        var minimumX = bitmap.Width;
        var minimumY = bitmap.Height;
        var maximumX = -1;
        var maximumY = -1;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A == 0)
                {
                    continue;
                }

                minimumX = Math.Min(minimumX, x);
                minimumY = Math.Min(minimumY, y);
                maximumX = Math.Max(maximumX, x);
                maximumY = Math.Max(maximumY, y);
            }
        }

        return maximumX < minimumX
            ? Rectangle.Empty
            : Rectangle.FromLTRB(minimumX, minimumY, maximumX + 1, maximumY + 1);
    }

    private static double AlphaCoverage(Bitmap bitmap, Rectangle area)
    {
        long alpha = 0;
        for (var y = area.Top; y < area.Bottom; y++)
        {
            for (var x = area.Left; x < area.Right; x++)
            {
                alpha += bitmap.GetPixel(x, y).A;
            }
        }

        return alpha / (255d * area.Width * area.Height);
    }

    private static string[] EquipmentOrder(XElement approval)
    {
        return ((string?)approval.Attribute("equipmentOrder") ?? string.Empty)
            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .ToArray();
    }

    private static void AssertVisibleBounds(
        string diffusePath,
        int minimumWidth,
        int maximumWidth,
        int minimumHeight,
        int maximumHeight)
    {
        if (!File.Exists(diffusePath))
        {
            return;
        }

        using var bitmap = new Bitmap(diffusePath);
        var minimumX = bitmap.Width;
        var minimumY = bitmap.Height;
        var maximumX = -1;
        var maximumY = -1;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A == 0)
                {
                    continue;
                }

                minimumX = Math.Min(minimumX, x);
                minimumY = Math.Min(minimumY, y);
                maximumX = Math.Max(maximumX, x);
                maximumY = Math.Max(maximumY, y);
            }
        }

        var visibleWidth = maximumX >= minimumX ? maximumX - minimumX + 1 : 0;
        var visibleHeight = maximumY >= minimumY ? maximumY - minimumY + 1 : 0;
        Assert.Multiple(() =>
        {
            Assert.That(visibleWidth, Is.InRange(minimumWidth, maximumWidth));
            Assert.That(visibleHeight, Is.InRange(minimumHeight, maximumHeight));
        });
    }

    private static void AssertPreview(string path, int expectedWidth, int expectedHeight)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using var bitmap = new Bitmap(path);
        Assert.Multiple(() =>
        {
            Assert.That(bitmap.Width, Is.EqualTo(expectedWidth), path);
            Assert.That(bitmap.Height, Is.EqualTo(expectedHeight), path);
            Assert.That(new FileInfo(path).Length, Is.LessThan(1024 * 1024), path);
        });
    }

    private static void AssertSpeechBubbleTextCentered(
        string path,
        Rectangle bubbleInterior,
        double maximumCenterOffset)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using var bitmap = new Bitmap(path);
        var minimumX = int.MaxValue;
        var minimumY = int.MaxValue;
        var maximumX = int.MinValue;
        var maximumY = int.MinValue;
        for (var y = bubbleInterior.Top; y < bubbleInterior.Bottom; y++)
        {
            for (var x = bubbleInterior.Left; x < bubbleInterior.Right; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.R >= 180 || pixel.G >= 180 || pixel.B >= 180)
                {
                    continue;
                }

                minimumX = Math.Min(minimumX, x);
                minimumY = Math.Min(minimumY, y);
                maximumX = Math.Max(maximumX, x);
                maximumY = Math.Max(maximumY, y);
            }
        }

        Assert.That(maximumX, Is.GreaterThanOrEqualTo(minimumX), "No dark bubble text was found in " + path);
        Assert.That(maximumY, Is.GreaterThanOrEqualTo(minimumY), "No dark bubble text was found in " + path);
        var textCenterX = (minimumX + maximumX) / 2.0;
        var textCenterY = (minimumY + maximumY) / 2.0;
        var bubbleCenterX = bubbleInterior.Left + (bubbleInterior.Width - 1) / 2.0;
        var bubbleCenterY = bubbleInterior.Top + (bubbleInterior.Height - 1) / 2.0;

        Assert.Multiple(() =>
        {
            Assert.That(
                Math.Abs(textCenterX - bubbleCenterX),
                Is.LessThanOrEqualTo(maximumCenterOffset),
                $"Speech-bubble text is not horizontally centered in {path}.");
            Assert.That(
                Math.Abs(textCenterY - bubbleCenterY),
                Is.LessThanOrEqualTo(maximumCenterOffset),
                $"Speech-bubble text is not vertically centered in {path}.");
        });
    }

    private static void FillRectangle(
        Bitmap bitmap,
        int minimumX,
        int minimumY,
        int maximumX,
        int maximumY,
        Color color)
    {
        for (var y = minimumY; y <= maximumY; y++)
        {
            for (var x = minimumX; x <= maximumX; x++)
            {
                bitmap.SetPixel(x, y, color);
            }
        }
    }

    private static string ComputeSha256(string path)
    {
        using var sha256 = SHA256.Create();
        return BitConverter
            .ToString(sha256.ComputeHash(File.ReadAllBytes(path)))
            .Replace("-", string.Empty);
    }

    private static void ClearRectangle(
        Bitmap bitmap,
        int minimumX,
        int minimumY,
        int maximumX,
        int maximumY) =>
        FillRectangle(bitmap, minimumX, minimumY, maximumX, maximumY, Color.Transparent);

    private static (int ExitCode, string StandardOutput, string StandardError) RunOutlineProcessor(
        string scriptPath,
        string inputPath,
        string outputPath,
        int strokePixels,
        string baselinePath,
        string manifestPath,
        string assetId,
        string maskInputPath,
        string maskOutputPath,
        string outlineColor = "#17130F")
    {
        var start = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments =
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File " +
                Quote(scriptPath) +
                " -InputPath " +
                Quote(inputPath) +
                " -OutputPath " +
                Quote(outputPath) +
                " -StrokePixels " +
                strokePixels +
                " -OutlineColor " +
                Quote(outlineColor) +
                " -Baseline " +
                Quote(baselinePath) +
                " -TopologyManifest " +
                Quote(manifestPath) +
                " -AssetId " +
                Quote(assetId) +
                " -MaskInputPath " +
                Quote(maskInputPath) +
                " -MaskOutputPath " +
                Quote(maskOutputPath) +
                " -Output json",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(start)!;
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000))
        {
            using (var treeKill = Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill.exe",
                Arguments = $"/PID {process.Id} /T /F",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }))
            {
                if (treeKill is not null && !treeKill.WaitForExit(5_000))
                {
                    treeKill.Kill();
                }
            }

            if (!process.WaitForExit(5_000))
            {
                Assert.Fail("Outline processor tree did not exit after bounded termination.");
            }

            Assert.Fail("Outline processing exceeded 30 seconds.");
        }

        Assert.That(
            Task.WaitAll(new Task[] { standardOutput, standardError }, 5_000),
            Is.True,
            "Outline processor output streams did not close after process exit.");
        return (process.ExitCode, standardOutput.Result, standardError.Result);
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunPreviewRenderer(
        string scriptPath,
        string workshopOutput,
        string aboutOutput)
    {
        var start = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments =
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File " +
                Quote(scriptPath) +
                " -WorkshopOutput " +
                Quote(workshopOutput) +
                " -AboutOutput " +
                Quote(aboutOutput) +
                " -Output json",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(start)!;
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000))
        {
            process.Kill();
            process.WaitForExit();
            Assert.Fail("Preview rendering exceeded 30 seconds.");
        }

        Task.WaitAll(standardOutput, standardError);
        return (process.ExitCode, standardOutput.Result, standardError.Result);
    }

    private static double DarkPixelFraction(Bitmap bitmap, Rectangle rectangle)
    {
        var dark = 0;
        var visible = 0;
        for (var y = rectangle.Top; y < rectangle.Bottom; y++)
        for (var x = rectangle.Left; x < rectangle.Right; x++)
        {
            var pixel = bitmap.GetPixel(x, y);
            if (pixel.A <= 180)
            {
                continue;
            }

            visible++;
            if ((pixel.R + pixel.G + pixel.B) / 3 < 80)
            {
                dark++;
            }
        }

        return visible == 0 ? 0 : dark / (double)visible;
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RimWorldMods.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
