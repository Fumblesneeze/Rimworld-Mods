using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using NUnit.Framework;

namespace ThinWalls.Defs.Tests;

[TestFixture]
public sealed class ThinWallPresentationContractTests
{
    private static readonly IReadOnlyDictionary<string, string> AcceptedRuns =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["20260820T013908908Z"] = "489b6eb405ee428fa2473995e8da2e31",
            ["20260820T020606902Z"] = "3f7b3b5426ae495899118400e2612571",
            ["20260820T021022450Z"] = "6609d360a92d43a7b4ec6a7637bea1fc",
        };

    [Test]
    public void PresentationUsesOnlyHashBoundAcceptedInGameCaptures()
    {
        string root = RepositoryRoot();
        PresentationManifest manifest = ReadManifest(root);

        Assert.Multiple(() =>
        {
            Assert.That(manifest.Schema, Is.EqualTo("ThinWalls/WorkshopPresentation/v3"));
            Assert.That(manifest.ImageGenerationUsed, Is.False);
            Assert.That(manifest.AcceptedRuns, Has.Count.EqualTo(AcceptedRuns.Count));
            Assert.That(manifest.AcceptedRuns.ToDictionary(run => run.RunId, run => run.SessionId),
                Is.EqualTo(AcceptedRuns));
            Assert.That(manifest.AcceptedRuns.All(run =>
                run.LaunchWindowStyle == "Minimized" && !run.VisibleWindow && run.ReviewVerdict == "passed"),
                Is.True);
            Assert.That(manifest.Sources, Has.Count.EqualTo(14));
            Assert.That(manifest.MeasurementInputs, Has.Count.EqualTo(5));
            Assert.That(manifest.Cards, Has.Count.GreaterThanOrEqualTo(3));
        });

        foreach (PresentationSource source in manifest.Sources)
        {
            string path = Path.Combine(root, Normalize(source.Path));
            Assert.That(AcceptedRuns.TryGetValue(source.RunId, out string acceptedSession), Is.True,
                $"Source {source.Token} is not bound to an accepted current-build run.");
            Assert.Multiple(() =>
            {
                Assert.That(source.Path, Does.StartWith(
                    $"mods/ThinWalls/Release/workshop/sources/in-game/accepted-{source.RunId}/"));
                Assert.That(source.OriginalScreenshot, Does.Match("^e2e-screenshot-[0-9]{6}\\.png$"));
                Assert.That(source.SessionId, Is.EqualTo(acceptedSession));
                Assert.That(source.LaunchWindowStyle, Is.EqualTo("Minimized"));
                Assert.That(source.VisibleWindow, Is.False);
                Assert.That(source.ReviewVerdict, Is.EqualTo("passed"));
                Assert.That(File.Exists(path), Is.True, $"Missing promoted in-game capture {source.Token}");
                Assert.That(Sha256(path), Is.EqualTo(source.Sha256));
            });

            using var bitmap = new Bitmap(path);
            Assert.Multiple(() =>
            {
                Assert.That(bitmap.Width, Is.EqualTo(source.Width));
                Assert.That(bitmap.Height, Is.EqualTo(source.Height));
            });
        }

        foreach (PresentationMeasurementInput input in manifest.MeasurementInputs)
        {
            string path = Path.Combine(root, Normalize(input.Path));
            Assert.That(AcceptedRuns.TryGetValue(input.RunId, out string acceptedSession), Is.True,
                $"Measurement input {input.Token} is not bound to an accepted current-build run.");
            Assert.Multiple(() =>
            {
                Assert.That(input.Path, Does.StartWith(
                    $"mods/ThinWalls/Release/workshop/measurements/in-game/accepted-{input.RunId}/"));
                Assert.That(input.OriginalScreenshot, Does.Match("^e2e-screenshot-[0-9]{6}\\.png$"));
                Assert.That(input.SessionId, Is.EqualTo(acceptedSession));
                Assert.That(File.Exists(path), Is.True, input.Token);
                Assert.That(Sha256(path), Is.EqualTo(input.Sha256), input.Token);
            });
            using var bitmap = new Bitmap(path);
            Assert.That((bitmap.Width, bitmap.Height), Is.EqualTo((input.Width, input.Height)), input.Token);
        }
    }

    [Test]
    public void AcceptedSourceDirectoryContainsExactlyTheManifestCaptures()
    {
        string root = RepositoryRoot();
        PresentationManifest manifest = ReadManifest(root);
        string sourceRoot = Path.Combine(root, "mods", "ThinWalls", "Release", "workshop", "sources", "in-game");
        string[] expected = manifest.Sources
            .Select(source => Path.GetFullPath(Path.Combine(root, Normalize(source.Path))))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] actual = Directory.GetFiles(sourceRoot, "*.png", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected),
            "Superseded or unmanifested in-game publishing captures must not remain in the accepted source tree.");
    }

    [Test]
    public void AcceptedMeasurementDirectoryContainsExactlyTheManifestSupportingCaptures()
    {
        string root = RepositoryRoot();
        PresentationManifest manifest = ReadManifest(root);
        string measurementRoot = Path.Combine(root, "mods", "ThinWalls", "Release", "workshop", "measurements", "in-game");
        string[] expected = manifest.MeasurementInputs
            .Select(input => Path.GetFullPath(Path.Combine(root, Normalize(input.Path))))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] actual = Directory.Exists(measurementRoot)
            ? Directory.GetFiles(measurementRoot, "*.png", SearchOption.AllDirectories)
                .Select(Path.GetFullPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();

        Assert.That(actual, Is.EqualTo(expected),
            "Superseded or unmanifested same-camera measurement captures must not remain in the supporting input tree.");
    }

    [Test]
    public void RendererFontAndApprovedOutputsAreHashBound()
    {
        string root = RepositoryRoot();
        PresentationManifest manifest = ReadManifest(root);
        string renderer = Path.Combine(root, Normalize(manifest.Renderer.Path));
        string font = Path.Combine(root, Normalize(manifest.Renderer.FontPath));

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(renderer), Is.True);
            Assert.That(Sha256(renderer), Is.EqualTo(manifest.Renderer.Sha256));
            Assert.That(manifest.Renderer.ImageMagickVersion, Is.EqualTo("7.1.2-18 Q16-HDRI x64"));
            Assert.That(File.Exists(font), Is.True);
            Assert.That(Sha256(font), Is.EqualTo(manifest.Renderer.FontSha256));
            Assert.That(manifest.Outputs.Select(output => output.Token), Does.Contain("title"));
            Assert.That(manifest.Outputs.Select(output => output.Token), Does.Contain("about"));
            Assert.That(manifest.Outputs.Count(output => output.Role == "workshop-card"), Is.EqualTo(manifest.Cards.Count));
        });

        foreach (PresentationOutput output in manifest.Outputs)
        {
            AssertOutput(root, output);
            if (output.Role == "workshop-card")
            {
                Assert.That(output.Bytes, Is.LessThan(1024 * 1024), output.Token);
            }
        }

        PresentationOutput title = manifest.Outputs.Single(output => output.Token == "title");
        PresentationOutput about = manifest.Outputs.Single(output => output.Token == "about");
        Assert.Multiple(() =>
        {
            Assert.That((title.Width, title.Height), Is.EqualTo((1280, 720)));
            Assert.That((about.Width, about.Height), Is.EqualTo((640, 360)));
        });
    }

    [Test]
    public void CleanOfflineRerenderMatchesEveryApprovedOutputByteForByte()
    {
        string root = RepositoryRoot();
        PresentationManifest manifest = ReadManifest(root);
        string renderer = Path.Combine(root, Normalize(manifest.Renderer.Path));
        string temporaryRoot = Path.Combine(Path.GetTempPath(), "thin-walls-presentation-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);

        try
        {
            var start = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File " + Quote(renderer) +
                    " -OutputRoot " + Quote(temporaryRoot) + " -Output json",
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using Process process = Process.Start(start)!;
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.That(process.WaitForExit(120000), Is.True, "Presentation renderer timed out.");
            Assert.That(process.ExitCode, Is.EqualTo(0), stderr + Environment.NewLine + stdout);
            Assert.That(stdout, Does.Contain("\"verified\":true"));

            foreach (PresentationOutput output in manifest.Outputs)
            {
                string rerendered = Path.Combine(temporaryRoot, Normalize(output.Path));
                Assert.That(File.Exists(rerendered), Is.True, output.Token);
                Assert.That(Sha256(rerendered), Is.EqualTo(output.Sha256), output.Token);
            }
        }
        finally
        {
            if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, true);
        }
    }

    [Test]
    public void EdgePlacementCellBoundaryOverlaysBisectTheMeasuredGhostSilhouette()
    {
        string root = RepositoryRoot();
        PresentationManifest manifest = ReadManifest(root);
        PresentationSource southSource = manifest.Sources.Single(source => source.Token == "preview-south");
        PresentationSource eastSource = manifest.Sources.Single(source => source.Token == "preview-east");
        Assert.That(southSource.SharedEdgeProjection, Is.Not.Null);
        Assert.That(eastSource.SharedEdgeProjection, Is.Not.Null);
        PresentationSharedEdgeProjection southSharedEdge = southSource.SharedEdgeProjection!;
        PresentationSharedEdgeProjection eastSharedEdge = eastSource.SharedEdgeProjection!;
        PointF southStart = ProjectSourcePointToPanel(
            southSharedEdge.StartScreenX,
            southSharedEdge.StartScreenYTop,
            new Rectangle(445, 195, 260, 260),
            new Size(350, 330));
        PointF southEnd = ProjectSourcePointToPanel(
            southSharedEdge.EndScreenX,
            southSharedEdge.EndScreenYTop,
            new Rectangle(445, 195, 260, 260),
            new Size(350, 330));
        PointF southOwner = ProjectSourcePointToPanel(
            southSharedEdge.OwnerCellCenterScreenX,
            southSharedEdge.OwnerCellCenterScreenYTop,
            new Rectangle(445, 195, 260, 260),
            new Size(350, 330));
        PointF southOpposite = ProjectSourcePointToPanel(
            southSharedEdge.OppositeCellCenterScreenX,
            southSharedEdge.OppositeCellCenterScreenYTop,
            new Rectangle(445, 195, 260, 260),
            new Size(350, 330));
        PointF eastStart = ProjectSourcePointToPanel(
            eastSharedEdge.StartScreenX,
            eastSharedEdge.StartScreenYTop,
            new Rectangle(445, 195, 260, 260),
            new Size(350, 330));
        PointF eastEnd = ProjectSourcePointToPanel(
            eastSharedEdge.EndScreenX,
            eastSharedEdge.EndScreenYTop,
            new Rectangle(445, 195, 260, 260),
            new Size(350, 330));
        PointF eastOwner = ProjectSourcePointToPanel(
            eastSharedEdge.OwnerCellCenterScreenX,
            eastSharedEdge.OwnerCellCenterScreenYTop,
            new Rectangle(445, 195, 260, 260),
            new Size(350, 330));
        PointF eastOpposite = ProjectSourcePointToPanel(
            eastSharedEdge.OppositeCellCenterScreenX,
            eastSharedEdge.OppositeCellCenterScreenYTop,
            new Rectangle(445, 195, 260, 260),
            new Size(350, 330));
        PresentationMeasurementInput southBackground = MeasurementInput(
            manifest,
            southSource.BackgroundMeasurementToken);
        PresentationMeasurementInput eastBackground = MeasurementInput(
            manifest,
            eastSource.BackgroundMeasurementToken);
        AssertCameraIdentity(southSharedEdge, southBackground, "south-edge preview");
        AssertCameraIdentity(eastSharedEdge, eastBackground, "east-edge preview");
        using var southBitmap = new Bitmap(Path.Combine(root, Normalize(southSource.Path)));
        using var eastBitmap = new Bitmap(Path.Combine(root, Normalize(eastSource.Path)));
        using var southBaseline = new Bitmap(Path.Combine(root, Normalize(southBackground.Path)));
        using var eastBaseline = new Bitmap(Path.Combine(root, Normalize(eastBackground.Path)));
        Rectangle previewCrop = new(445, 195, 260, 260);
        DifferenceMask southSourceMask = MeasureRelevantDifference(
            southBaseline,
            southBitmap,
            previewCrop,
            ((southSharedEdge.StartScreenYTop + southSharedEdge.EndScreenYTop) / 2d),
            normalAxisIsY: true,
            "south-edge source preview");
        DifferenceMask eastSourceMask = MeasureRelevantDifference(
            eastBaseline,
            eastBitmap,
            previewCrop,
            ((eastSharedEdge.StartScreenX + eastSharedEdge.EndScreenX) / 2d),
            normalAxisIsY: false,
            "east-edge source preview");
        AssertMaskInsideProjectionEnvelope(southSourceMask, southSharedEdge, normalAxisIsY: true, "south-edge source preview");
        AssertMaskInsideProjectionEnvelope(eastSourceMask, eastSharedEdge, normalAxisIsY: false, "east-edge source preview");
        Assert.Multiple(() =>
        {
            Assert.That(Math.Abs(southSourceMask.MidpointY - ((southSharedEdge.StartScreenYTop + southSharedEdge.EndScreenYTop) / 2d)), Is.LessThanOrEqualTo(1d),
                "The complete source south ghost silhouette must be centered on the independently Gateway-recorded shared edge.");
            Assert.That(Math.Abs(eastSourceMask.MidpointX - ((eastSharedEdge.StartScreenX + eastSharedEdge.EndScreenX) / 2d)), Is.LessThanOrEqualTo(1d),
                "The complete source east ghost silhouette must be centered on the independently Gateway-recorded shared edge.");
        });
        int southBoundary = 180 + (int)Math.Floor(((southStart.Y + southEnd.Y) / 2d) - 0.5d);
        int southCellSpan = RoundAway(Math.Abs(southOwner.Y - southOpposite.Y));
        Rectangle southExpectedBounds = Rectangle.FromLTRB(
            25 + RoundAway(Math.Min(southStart.X, southEnd.X)),
            southBoundary - southCellSpan,
            25 + RoundAway(Math.Max(southStart.X, southEnd.X)) + 1,
            southBoundary + 1 + southCellSpan + 1);
        int eastBoundary = 407 + (int)Math.Floor((eastStart.X + eastEnd.X) / 2d);
        int eastCellSpan = RoundAway(Math.Abs(eastOwner.X - eastOpposite.X));
        Rectangle eastExpectedBounds = Rectangle.FromLTRB(
            eastBoundary - eastCellSpan,
            180 + RoundAway(Math.Min(eastStart.Y, eastEnd.Y)),
            eastBoundary + 1 + eastCellSpan + 1,
            180 + RoundAway(Math.Max(eastStart.Y, eastEnd.Y)) + 1);
        string path = Path.Combine(
            root,
            "mods",
            "ThinWalls",
            "Release",
            "workshop",
            "assets",
            "feature-edge-placement.png");
        using var bitmap = new Bitmap(path);
        using Bitmap southPanel = TransformToPanel(southBitmap, previewCrop, new Size(350, 330));
        using Bitmap southBaselinePanel = TransformToPanel(southBaseline, previewCrop, new Size(350, 330));
        using Bitmap eastPanel = TransformToPanel(eastBitmap, previewCrop, new Size(350, 330));
        using Bitmap eastBaselinePanel = TransformToPanel(eastBaseline, previewCrop, new Size(350, 330));
        DifferenceMask southPanelMask = MeasureRelevantDifference(
            southBaselinePanel,
            southPanel,
            new Rectangle(Point.Empty, southPanel.Size),
            (southStart.Y + southEnd.Y) / 2d,
            normalAxisIsY: true,
            "south-edge final panel preview");
        DifferenceMask eastPanelMask = MeasureRelevantDifference(
            eastBaselinePanel,
            eastPanel,
            new Rectangle(Point.Empty, eastPanel.Size),
            (eastStart.X + eastEnd.X) / 2d,
            normalAxisIsY: false,
            "east-edge final panel preview");
        AssertPanelPreservesPreviewExceptGold(bitmap, new Rectangle(25, 180, 350, 330), southPanel, "south-edge final panel");
        AssertPanelPreservesPreviewExceptGold(bitmap, new Rectangle(407, 180, 350, 330), eastPanel, "east-edge final panel");

        AssertOverlayBisectsDifference(
            bitmap,
            new Rectangle(185, 185, 90, 135),
            normalAxisIsY: true,
            structuralMidpoint: 180 + southPanelMask.MidpointY,
            expectedBoundary: southBoundary + 0.5d,
            expectedBounds: southExpectedBounds,
            "south-edge preview");
        AssertOverlayBisectsDifference(
            bitmap,
            new Rectangle(550, 190, 145, 90),
            normalAxisIsY: false,
            structuralMidpoint: 407 + eastPanelMask.MidpointX,
            expectedBoundary: eastBoundary + 0.5d,
            expectedBounds: eastExpectedBounds,
            "east-edge preview");
    }

    [Test]
    public void TwoSidedWorkbenchSourceAndFinalCardPreserveThreeCompleteDisjointStructuralMasks()
    {
        string root = RepositoryRoot();
        PresentationManifest manifest = ReadManifest(root);
        PresentationSource source = manifest.Sources.Single(item => item.Token == "workbenches");
        using WorkbenchStages sourceStages = OpenWorkbenchStages(root, manifest, source);
        AssertWorkbenchStages(sourceStages, "promoted in-game workbench source");

        string cardPath = Path.Combine(
            root,
            "mods",
            "ThinWalls",
            "Release",
            "workshop",
            "assets",
            "feature-edge-placement.png");
        using var card = new Bitmap(cardPath);
        using WorkbenchStages panelStages = TransformWorkbenchStagesToPanel(sourceStages, new Size(350, 330));
        using Bitmap cardPanel = card.Clone(new Rectangle(789, 180, 350, 330), PixelFormat.Format24bppRgb);
        using WorkbenchStages cardStages = panelStages.WithPublic(cardPanel);
        AssertWorkbenchStages(cardStages, "final edge-placement card panel");
    }

    [Test]
    public void WorkbenchPixelGateRejectsAOneLevelLowContrastBridgeThatDetoursOutsideTheSharedWidth()
    {
        using var empty = NewSolidBitmap(300, 240, Color.FromArgb(110, 70, 35));
        using var wallOnly = (Bitmap)empty.Clone();
        using (Graphics graphics = Graphics.FromImage(wallOnly))
        {
            using var structural = new SolidBrush(Color.FromArgb(90, 90, 90));
            graphics.FillRectangle(structural, 55, 100, 190, 15);
        }
        using var northAndWall = (Bitmap)wallOnly.Clone();
        using (Graphics graphics = Graphics.FromImage(northAndWall))
        {
            using var structural = new SolidBrush(Color.FromArgb(90, 90, 90));
            graphics.FillRectangle(structural, 60, 40, 180, 40);
            using var oneLevelDifference = new SolidBrush(Color.FromArgb(111, 70, 35));
            graphics.FillRectangle(oneLevelDifference, 20, 80, 40, 1);
            graphics.FillRectangle(oneLevelDifference, 20, 80, 1, 20);
            graphics.FillRectangle(oneLevelDifference, 20, 99, 35, 1);
        }
        using var final = (Bitmap)northAndWall.Clone();
        using (Graphics graphics = Graphics.FromImage(final))
        {
            using var structural = new SolidBrush(Color.FromArgb(90, 90, 90));
            graphics.FillRectangle(structural, 60, 117, 180, 40);
        }

        var stages = new WorkbenchStages(empty, wallOnly, northAndWall, final, final);

        Exception exception = Assert.Catch<Exception>(() =>
            AssertWorkbenchStages(stages, "synthetic detouring-bridge workbench fixture"))!;
        Assert.That(exception.Message, Does.Contain("north bench must not overlap or fuse"));
    }

    [Test]
    public void StagedDifferenceMaskIncludesEveryOneLevelRgbChange()
    {
        using var before = NewSolidBitmap(12, 12, Color.FromArgb(110, 70, 35));
        using var after = (Bitmap)before.Clone();
        using (Graphics graphics = Graphics.FromImage(after))
        {
            using var oneLevelDifference = new SolidBrush(Color.FromArgb(111, 70, 35));
            graphics.FillRectangle(oneLevelDifference, 2, 2, 9, 9);
        }

        DifferenceMask mask = MeasureDifference(
            before,
            after,
            new Rectangle(Point.Empty, before.Size),
            "one-level RGB fixture");
        Assert.That(mask.Count, Is.EqualTo(81));
    }

    [Test]
    public void WorkbenchPixelGateAcceptsCompleteSeparatedStagedStructuralMasks()
    {
        using var empty = NewSolidBitmap(300, 240, Color.FromArgb(110, 70, 35));
        using var wallOnly = (Bitmap)empty.Clone();
        using (Graphics graphics = Graphics.FromImage(wallOnly))
        {
            using var structural = new SolidBrush(Color.FromArgb(90, 90, 90));
            graphics.FillRectangle(structural, 55, 100, 190, 15);
        }
        using var northAndWall = (Bitmap)wallOnly.Clone();
        using (Graphics graphics = Graphics.FromImage(northAndWall))
        {
            using var structural = new SolidBrush(Color.FromArgb(90, 90, 90));
            graphics.FillRectangle(structural, 60, 58, 180, 40);
        }
        using var final = (Bitmap)northAndWall.Clone();
        using (Graphics graphics = Graphics.FromImage(final))
        {
            using var structural = new SolidBrush(Color.FromArgb(90, 90, 90));
            graphics.FillRectangle(structural, 60, 117, 180, 40);
        }

        var stages = new WorkbenchStages(empty, wallOnly, northAndWall, final, final);
        Assert.DoesNotThrow(() => AssertWorkbenchStages(stages, "synthetic separated workbench fixture"));
    }

    [Test]
    public void EveryPublicationSourceHasAnExplicitPlacementScopeAndItemizedAuditsFailClosed()
    {
        PresentationManifest manifest = ReadManifest(RepositoryRoot());
        IReadOnlyDictionary<string, string[]> requiredCategories = RequiredPlacementAuditCategories();
        IReadOnlyDictionary<string, string> requiredScopes = RequiredPlacementScopes();

        Assert.That(manifest.Schema, Is.EqualTo("ThinWalls/WorkshopPresentation/v3"));
        var observedScopes = manifest.Sources.ToDictionary(
            source => "source:" + source.Token,
            source => source.PlacementReviewScope,
            StringComparer.Ordinal);
        foreach (PresentationOutput output in manifest.Outputs)
        {
            observedScopes.Add("output:" + output.Token, output.PlacementReviewScope);
        }
        Assert.That(observedScopes, Is.EqualTo(requiredScopes),
            "Placement review scopes are contract data, not author-selected declarations. Every furnished/door-bearing target must remain itemized and every technical catalog must remain explicit.");

        string[] itemizedTargets = requiredCategories.Keys.OrderBy(target => target, StringComparer.Ordinal).ToArray();
        string[] auditedTargets = manifest.Reviews.PlacementAudits
            .Select(audit => audit.TargetKind + ":" + audit.TargetToken)
            .OrderBy(target => target, StringComparer.Ordinal)
            .ToArray();
        Assert.That(auditedTargets, Is.EqualTo(itemizedTargets),
            "Every and only the itemized publication sources and final outputs require a retained placement audit.");

        foreach (PresentationPlacementAudit audit in manifest.Reviews.PlacementAudits)
        {
            string target = audit.TargetKind + ":" + audit.TargetToken;
            Assert.Multiple(() =>
            {
                Assert.That(audit.ReviewerTask, Is.Not.Empty, audit.TargetToken);
                Assert.That(audit.Verdict, Is.EqualTo("passed"), audit.TargetToken);
                Assert.That(audit.Items, Is.Not.Empty, audit.TargetToken);
                Assert.That(audit.Items.Select(item => item.Category).ToArray(),
                    Is.EqualTo(requiredCategories[target]),
                    target + " must enumerate exactly the placement classes visibly present in that target");
            });
            foreach (PresentationPlacementAuditItem item in audit.Items)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(item.VisibleCount, Is.GreaterThan(0), audit.TargetToken + ":" + item.Category);
                    Assert.That(item.ReviewedCount, Is.EqualTo(item.VisibleCount), audit.TargetToken + ":" + item.Category);
                    Assert.That(item.UnresolvedFindingCount, Is.Zero, audit.TargetToken + ":" + item.Category);
                    Assert.That(item.Observation, Is.Not.Empty, audit.TargetToken + ":" + item.Category);
                });
            }
        }
    }

    private static void AssertOverlayBisectsDifference(
        Bitmap bitmap,
        Rectangle search,
        bool normalAxisIsY,
        double structuralMidpoint,
        double expectedBoundary,
        Rectangle expectedBounds,
        string label)
    {
        var goldAxisCounts = new Dictionary<int, int>();
        var goldPoints = new List<Point>();
        for (int y = search.Top; y < search.Bottom; y++)
        {
            for (int x = search.Left; x < search.Right; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                if (pixel.R <= 200 || pixel.G <= 160 || pixel.B <= 100 || pixel.R - pixel.B <= 20)
                {
                    continue;
                }

                int axis = normalAxisIsY ? y : x;
                goldAxisCounts.TryGetValue(axis, out int count);
                goldAxisCounts[axis] = count + 1;
                goldPoints.Add(new Point(x, y));
            }
        }

        int requiredSpan = normalAxisIsY ? 45 : 45;
        int[] sharedBoundaryPixels = goldAxisCounts
            .Where(pair => pair.Value >= requiredSpan)
            .OrderBy(pair => Math.Abs(pair.Key - expectedBoundary))
            .Take(2)
            .Select(pair => pair.Key)
            .OrderBy(value => value)
            .ToArray();
        Assert.That(sharedBoundaryPixels, Has.Length.EqualTo(2), label + " shared overlay line was not measurable");
        Assert.That(sharedBoundaryPixels[1] - sharedBoundaryPixels[0], Is.LessThanOrEqualTo(1),
            label + " shared boundary must be one contiguous two-pixel line");

        double overlayMidpoint = sharedBoundaryPixels.Average();
        Rectangle actualBounds = Rectangle.FromLTRB(
            goldPoints.Min(point => point.X),
            goldPoints.Min(point => point.Y),
            goldPoints.Max(point => point.X) + 1,
            goldPoints.Max(point => point.Y) + 1);
        Assert.Multiple(() =>
        {
            Assert.That(Math.Abs(overlayMidpoint - expectedBoundary), Is.LessThanOrEqualTo(1d),
                $"{label} overlay midpoint {overlayMidpoint:0.0} does not match Gateway-recorded boundary {expectedBoundary:0.0}");
            Assert.That(Math.Abs(overlayMidpoint - structuralMidpoint), Is.LessThanOrEqualTo(1d),
                $"{label} overlay midpoint {overlayMidpoint:0.0} does not bisect the complete staged-difference ghost midpoint {structuralMidpoint:0.0}");
            Assert.That(Math.Abs(actualBounds.Left - expectedBounds.Left), Is.LessThanOrEqualTo(1), label + " left extent");
            Assert.That(Math.Abs(actualBounds.Top - expectedBounds.Top), Is.LessThanOrEqualTo(1), label + " top extent");
            Assert.That(Math.Abs(actualBounds.Right - expectedBounds.Right), Is.LessThanOrEqualTo(1), label + " right extent");
            Assert.That(Math.Abs(actualBounds.Bottom - expectedBounds.Bottom), Is.LessThanOrEqualTo(1), label + " bottom extent");
        });
    }

    private static void AssertPanelPreservesPreviewExceptGold(
        Bitmap card,
        Rectangle panel,
        Bitmap expectedPreview,
        string label)
    {
        Assert.That(expectedPreview.Size, Is.EqualTo(panel.Size));
        int compared = 0;
        for (int y = 0; y < panel.Height; y++)
        {
            for (int x = 0; x < panel.Width; x++)
            {
                Color actual = card.GetPixel(panel.X + x, panel.Y + y);
                if (IsGold(actual))
                {
                    continue;
                }
                compared++;
                Assert.That(actual.ToArgb(), Is.EqualTo(expectedPreview.GetPixel(x, y).ToArgb()),
                    $"{label} changed a non-overlay source pixel at ({x},{y})");
            }
        }
        Assert.That(compared, Is.GreaterThan(panel.Width * panel.Height * 0.95),
            label + " overlay unexpectedly obscures too much of the in-game render");
    }

    private static bool IsGold(Color pixel) =>
        pixel.R > 200 && pixel.G > 160 && pixel.B > 100 && pixel.R - pixel.B > 20;

    private static void AssertCameraIdentity(
        PresentationSharedEdgeProjection preview,
        PresentationMeasurementInput baseline,
        string label)
    {
        Assert.Multiple(() =>
        {
            Assert.That(preview.MapId, Is.Not.Empty, label + " preview map identity");
            Assert.That(baseline.MapId, Is.Not.Empty, label + " baseline map identity");
            Assert.That(preview.CameraRootSize, Is.GreaterThan(0d), label + " preview camera root size");
            Assert.That(baseline.CameraRootSize, Is.GreaterThan(0d), label + " baseline camera root size");
            Assert.That(preview.MapId, Is.EqualTo(baseline.MapId), label + " map");
            Assert.That(preview.CameraWorldX, Is.EqualTo(baseline.CameraWorldX), label + " camera X");
            Assert.That(preview.CameraWorldZ, Is.EqualTo(baseline.CameraWorldZ), label + " camera Z");
            Assert.That(preview.CameraRootSize, Is.EqualTo(baseline.CameraRootSize), label + " camera root size");
            Assert.That((preview.ViewportX, preview.ViewportY, preview.ScreenshotWidth, preview.ScreenshotHeight),
                Is.EqualTo((baseline.ViewportX, baseline.ViewportY, baseline.Width, baseline.Height)),
                label + " viewport");
        });
    }

    private static void AssertMaskInsideProjectionEnvelope(
        DifferenceMask mask,
        PresentationSharedEdgeProjection projection,
        bool normalAxisIsY,
        string label)
    {
        double tangentMinimum = normalAxisIsY
            ? Math.Min(projection.StartScreenX, projection.EndScreenX)
            : Math.Min(projection.StartScreenYTop, projection.EndScreenYTop);
        double tangentMaximum = normalAxisIsY
            ? Math.Max(projection.StartScreenX, projection.EndScreenX)
            : Math.Max(projection.StartScreenYTop, projection.EndScreenYTop);
        double normalMinimum = normalAxisIsY
            ? Math.Min(projection.OwnerCellCenterScreenYTop, projection.OppositeCellCenterScreenYTop)
            : Math.Min(projection.OwnerCellCenterScreenX, projection.OppositeCellCenterScreenX);
        double normalMaximum = normalAxisIsY
            ? Math.Max(projection.OwnerCellCenterScreenYTop, projection.OppositeCellCenterScreenYTop)
            : Math.Max(projection.OwnerCellCenterScreenX, projection.OppositeCellCenterScreenX);
        Rectangle envelope = normalAxisIsY
            ? Rectangle.FromLTRB(
                (int)Math.Floor(tangentMinimum) - 4,
                (int)Math.Floor(normalMinimum) - 4,
                (int)Math.Ceiling(tangentMaximum) + 5,
                (int)Math.Ceiling(normalMaximum) + 5)
            : Rectangle.FromLTRB(
                (int)Math.Floor(normalMinimum) - 4,
                (int)Math.Floor(tangentMinimum) - 4,
                (int)Math.Ceiling(normalMaximum) + 5,
                (int)Math.Ceiling(tangentMaximum) + 5);
        Assert.That(envelope.Contains(mask.Bounds), Is.True,
            $"{label} complete difference bounds {mask.Bounds} escape the independently projected one-edge envelope {envelope}");
    }

    private static PointF ProjectSourcePointToPanel(
        double sourceX,
        double sourceY,
        Rectangle crop,
        Size panel)
    {
        double scale = Math.Max((double)panel.Width / crop.Width, (double)panel.Height / crop.Height);
        double resizedWidth = Math.Round(crop.Width * scale, MidpointRounding.AwayFromZero);
        double resizedHeight = Math.Round(crop.Height * scale, MidpointRounding.AwayFromZero);
        double offsetX = (resizedWidth - panel.Width) / 2d;
        double offsetY = (resizedHeight - panel.Height) / 2d;
        return new PointF(
            (float)(((sourceX - crop.X) * scale) - offsetX),
            (float)(((sourceY - crop.Y) * scale) - offsetY));
    }

    private static int RoundAway(double value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static DifferenceMask MeasureRelevantDifference(
        Bitmap before,
        Bitmap after,
        Rectangle search,
        double expectedBoundary,
        bool normalAxisIsY,
        string label)
    {
        DifferenceMask complete = MeasureDifference(before, after, search, label);
        var remaining = new HashSet<Point>(complete.Pixels);
        var components = new List<DifferenceMask>();
        var queue = new Queue<Point>();
        while (remaining.Count > 0)
        {
            Point seed = remaining.First();
            remaining.Remove(seed);
            queue.Enqueue(seed);
            var pixels = new HashSet<Point> { seed };
            while (queue.Count > 0)
            {
                Point point = queue.Dequeue();
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        Point neighbor = new(point.X + dx, point.Y + dy);
                        if (remaining.Remove(neighbor))
                        {
                            pixels.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
            if (pixels.Count >= 64)
            {
                components.Add(new DifferenceMask(pixels));
            }
        }
        Assert.That(components, Is.Not.Empty, label + " did not contain a complete staged structural component");
        DifferenceMask selected = components
            .OrderBy(component => Math.Abs((normalAxisIsY ? component.MidpointY : component.MidpointX) - expectedBoundary))
            .ThenByDescending(component => component.Count)
            .First();
        Assert.That(selected.Count, Is.GreaterThan(500), label + " structural component is too small");
        Assert.That(complete.Count - selected.Count, Is.LessThanOrEqualTo(16),
            label + " contains unrelated staged drift outside the expected structural component");
        return selected;
    }

    private static DifferenceMask MeasureDifference(Bitmap before, Bitmap after, Rectangle search, string label)
    {
        Assert.That(after.Size, Is.EqualTo(before.Size), label + " staged screenshots must share one exact camera and viewport");
        Assert.That(new Rectangle(Point.Empty, before.Size).Contains(search), Is.True, label + " search bounds escaped the staged screenshots");
        var pixels = new HashSet<Point>();
        for (int y = search.Top; y < search.Bottom; y++)
        {
            for (int x = search.Left; x < search.Right; x++)
            {
                Color a = before.GetPixel(x, y);
                Color b = after.GetPixel(x, y);
                if (a.R != b.R || a.G != b.G || a.B != b.B)
                {
                    pixels.Add(new Point(x, y));
                }
            }
        }
        Assert.That(pixels.Count, Is.GreaterThan(64), label + " did not produce a measurable staged structural difference");
        return new DifferenceMask(pixels);
    }

    private static void AssertWorkbenchStages(WorkbenchStages stages, string label)
    {
        Rectangle viewport = new(Point.Empty, stages.Empty.Size);
        DifferenceMask wall = MeasureDifference(stages.Empty, stages.WallOnly, viewport, label + " wall stage");
        DifferenceMask north = MeasureDifference(stages.WallOnly, stages.NorthAndWall, viewport, label + " north-workbench stage");
        DifferenceMask south = MeasureDifference(stages.NorthAndWall, stages.NoShadowFinal, viewport, label + " south-workbench stage");
        Assert.That(north.Pixels.Overlaps(wall.Pixels), Is.False, label + " north workbench must not overlap or fuse with the complete Thin Wall mask");
        Assert.That(south.Pixels.Overlaps(wall.Pixels), Is.False, label + " south workbench must not overlap or fuse with the complete Thin Wall mask");
        Assert.That(north.Pixels.Overlaps(south.Pixels), Is.False, label + " opposing workbench masks must be disjoint");

        int northGap = wall.Bounds.Top - north.Bounds.Bottom;
        int southGap = south.Bounds.Top - wall.Bounds.Bottom;
        double screenPixelsPerCell = wall.Bounds.Width / 3d;
        int maximumAllowedGap = (int)Math.Ceiling((2d / 60d) * screenPixelsPerCell);
        Assert.Multiple(() =>
        {
            Assert.That(northGap, Is.GreaterThanOrEqualTo(1),
                label + " north bench must not overlap or fuse with the wall");
            Assert.That(southGap, Is.GreaterThanOrEqualTo(1),
                label + " south bench must not overlap or fuse with the wall");
            Assert.That(northGap, Is.LessThanOrEqualTo(maximumAllowedGap),
                label + " north clearance exceeds the one-pixel safety band plus one 1/60-cell quantization allowance");
            Assert.That(southGap, Is.LessThanOrEqualTo(maximumAllowedGap),
                label + " south clearance exceeds the one-pixel safety band plus one 1/60-cell quantization allowance");
        });

        foreach (Point point in wall.Pixels.Concat(north.Pixels).Concat(south.Pixels).Distinct())
        {
            Color structural = stages.NoShadowFinal.GetPixel(point.X, point.Y);
            Color published = stages.Public.GetPixel(point.X, point.Y);
            int delta = Math.Max(Math.Abs(structural.R - published.R), Math.Max(Math.Abs(structural.G - published.G), Math.Abs(structural.B - published.B)));
            Assert.That(delta, Is.LessThanOrEqualTo(12), $"{label} public render clips or repaints a structural pixel at ({point.X},{point.Y})");
        }
    }

    private static WorkbenchStages OpenWorkbenchStages(
        string root,
        PresentationManifest manifest,
        PresentationSource source)
    {
        return new WorkbenchStages(
            OpenMeasurement(root, manifest, source.EmptyMeasurementToken),
            OpenMeasurement(root, manifest, source.WallOnlyMeasurementToken),
            OpenMeasurement(root, manifest, source.NorthAndWallMeasurementToken),
            OpenMeasurement(root, manifest, source.NoShadowFinalMeasurementToken),
            new Bitmap(Path.Combine(root, Normalize(source.Path))));
    }

    private static Bitmap OpenMeasurement(string root, PresentationManifest manifest, string token) =>
        new(Path.Combine(root, Normalize(MeasurementInput(manifest, token).Path)));

    private static PresentationMeasurementInput MeasurementInput(PresentationManifest manifest, string token)
    {
        Assert.That(token, Is.Not.Empty, "The publication source is missing a required staged-measurement token.");
        PresentationMeasurementInput[] matches = manifest.MeasurementInputs.Where(item => item.Token == token).ToArray();
        Assert.That(matches, Has.Length.EqualTo(1), "Measurement input token must resolve exactly once: " + token);
        return matches[0];
    }

    private static WorkbenchStages TransformWorkbenchStagesToPanel(WorkbenchStages source, Size panel) => new(
        TransformToPanel(source.Empty, new Rectangle(Point.Empty, source.Empty.Size), panel),
        TransformToPanel(source.WallOnly, new Rectangle(Point.Empty, source.WallOnly.Size), panel),
        TransformToPanel(source.NorthAndWall, new Rectangle(Point.Empty, source.NorthAndWall.Size), panel),
        TransformToPanel(source.NoShadowFinal, new Rectangle(Point.Empty, source.NoShadowFinal.Size), panel),
        TransformToPanel(source.Public, new Rectangle(Point.Empty, source.Public.Size), panel));

    private static Bitmap TransformToPanel(Bitmap source, Rectangle crop, Size panel)
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), "thin-walls-panel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        string input = Path.Combine(temporaryRoot, "input.png");
        string output = Path.Combine(temporaryRoot, "output.png");
        source.Save(input, ImageFormat.Png);
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = "magick",
                Arguments = Quote(input) +
                    $" -crop {crop.Width}x{crop.Height}+{crop.X}+{crop.Y} +repage -filter Lanczos -resize {panel.Width}x{panel.Height}^ -gravity center -extent {panel.Width}x{panel.Height} -strip " + Quote(output),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using Process process = Process.Start(start)!;
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.That(process.WaitForExit(30000), Is.True, "ImageMagick panel transform timed out.");
            Assert.That(process.ExitCode, Is.Zero, stderr + Environment.NewLine + stdout);
            using var rendered = new Bitmap(output);
            return new Bitmap(rendered);
        }
        finally
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    private static Bitmap NewSolidBitmap(int width, int height, Color color)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        return bitmap;
    }

    private sealed class WorkbenchStages : IDisposable
    {
        public WorkbenchStages(Bitmap empty, Bitmap wallOnly, Bitmap northAndWall, Bitmap noShadowFinal, Bitmap @public)
        {
            Empty = empty;
            WallOnly = wallOnly;
            NorthAndWall = northAndWall;
            NoShadowFinal = noShadowFinal;
            Public = @public;
        }

        public Bitmap Empty { get; }
        public Bitmap WallOnly { get; }
        public Bitmap NorthAndWall { get; }
        public Bitmap NoShadowFinal { get; }
        public Bitmap Public { get; }

        public WorkbenchStages WithPublic(Bitmap @public) => new(
            (Bitmap)Empty.Clone(),
            (Bitmap)WallOnly.Clone(),
            (Bitmap)NorthAndWall.Clone(),
            (Bitmap)NoShadowFinal.Clone(),
            (Bitmap)@public.Clone());

        public void Dispose()
        {
            Empty.Dispose();
            WallOnly.Dispose();
            NorthAndWall.Dispose();
            NoShadowFinal.Dispose();
            Public.Dispose();
        }
    }

    private sealed class DifferenceMask
    {
        public DifferenceMask(HashSet<Point> pixels)
        {
            Pixels = pixels;
            Bounds = Rectangle.FromLTRB(
                pixels.Min(point => point.X),
                pixels.Min(point => point.Y),
                pixels.Max(point => point.X) + 1,
                pixels.Max(point => point.Y) + 1);
        }

        public HashSet<Point> Pixels { get; }
        public Rectangle Bounds { get; }
        public int Count => Pixels.Count;
        public double MidpointX => (Bounds.Left + Bounds.Right - 1) / 2d;
        public double MidpointY => (Bounds.Top + Bounds.Bottom - 1) / 2d;
    }

    private static IReadOnlyDictionary<string, string> RequiredPlacementScopes()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string token in new[]
                 {
                     "workbenches", "room-closed", "room-presentation-clean", "door-detail-closed", "door-detail-open",
                 })
        {
            result.Add("source:" + token, "itemized-realistic-scene");
        }
        foreach (string token in new[]
                 {
                     "preview-south", "preview-east", "junction-l", "junction-t", "junction-plus", "regular-union",
                     "damage-stone", "damage-wood", "damage-steel",
                 })
        {
            result.Add("source:" + token, "technical-catalog-no-realistic-fixtures");
        }
        foreach (string token in new[] { "title", "about", "edge-placement", "thin-doors", "contact-sheet" })
        {
            result.Add("output:" + token, "itemized-realistic-scene");
        }
        foreach (string token in new[] { "continuous-joins", "materials-damage" })
        {
            result.Add("output:" + token, "technical-catalog-no-realistic-fixtures");
        }
        return result;
    }

    private static IReadOnlyDictionary<string, string[]> RequiredPlacementAuditCategories() =>
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["source:workbenches"] = new[] { "workbench", "primary-aisle" },
            ["source:room-closed"] = new[] { "bed", "chair", "table-counter", "door", "primary-aisle" },
            ["source:room-presentation-clean"] = new[] { "bed", "chair", "table-counter", "door", "primary-aisle" },
            ["source:door-detail-closed"] = new[] { "door" },
            ["source:door-detail-open"] = new[] { "door" },
            ["output:title"] = new[] { "bed", "chair", "table-counter", "door", "primary-aisle" },
            ["output:about"] = new[] { "bed", "chair", "table-counter", "door", "primary-aisle" },
            ["output:edge-placement"] = new[] { "workbench", "primary-aisle" },
            ["output:thin-doors"] = new[] { "bed", "chair", "table-counter", "door", "primary-aisle" },
            ["output:contact-sheet"] = new[] { "bed", "chair", "table-counter", "workbench", "door", "primary-aisle" },
        };

    private static void AssertOutput(string root, PresentationOutput output)
    {
        string path = Path.Combine(root, Normalize(output.Path));
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(path), Is.True, output.Token);
            Assert.That(new FileInfo(path).Length, Is.EqualTo(output.Bytes), output.Token);
            Assert.That(Sha256(path), Is.EqualTo(output.Sha256), output.Token);
            Assert.That(output.Alt, Is.Not.Empty, output.Token);
            Assert.That(output.SourceTokens, Is.Not.Empty, output.Token);
        });
        using var bitmap = new Bitmap(path);
        Assert.Multiple(() =>
        {
            Assert.That(bitmap.Width, Is.EqualTo(output.Width), output.Token);
            Assert.That(bitmap.Height, Is.EqualTo(output.Height), output.Token);
            Assert.That(bitmap.PixelFormat, Is.EqualTo(PixelFormat.Format24bppRgb),
                output.Token + " must remain a lossless truecolor PNG rather than a palette-quantized capture");
            Assert.That(bitmap.GetPixel(0, 0).ToArgb(), Is.EqualTo(ColorTranslator.FromHtml("#1b2838").ToArgb()), output.Token);
            Assert.That(bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1).ToArgb(), Is.EqualTo(ColorTranslator.FromHtml("#1b2838").ToArgb()), output.Token);
        });
    }

    private static PresentationManifest ReadManifest(string root)
    {
        string path = Path.Combine(root, "mods", "ThinWalls", "Release", "workshop", "presentation.json");
        Assert.That(File.Exists(path), Is.True, "The accepted presentation manifest is missing.");
        using FileStream stream = File.OpenRead(path);
        var serializer = new DataContractJsonSerializer(typeof(PresentationManifest));
        return (PresentationManifest)serializer.ReadObject(stream)!;
    }

    private static string Sha256(string path)
    {
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value => value.ToString("X2")));
    }

    private static string Normalize(string path) => path.Replace('/', Path.DirectorySeparatorChar);
    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private static string RepositoryRoot() => typeof(ThinWallPresentationContractTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == "RepositoryRoot")
        .Value;

    [DataContract]
    private sealed class PresentationManifest
    {
        [DataMember(Name = "schema")] public string Schema { get; set; } = string.Empty;
        [DataMember(Name = "imageGenerationUsed")] public bool ImageGenerationUsed { get; set; }
        [DataMember(Name = "acceptedRuns")] public List<PresentationAcceptedRun> AcceptedRuns { get; set; } = new();
        [DataMember(Name = "renderer")] public PresentationRenderer Renderer { get; set; } = new();
        [DataMember(Name = "sources")] public List<PresentationSource> Sources { get; set; } = new();
        [DataMember(Name = "measurementInputs")] public List<PresentationMeasurementInput> MeasurementInputs { get; set; } = new();
        [DataMember(Name = "cards")] public List<PresentationCard> Cards { get; set; } = new();
        [DataMember(Name = "outputs")] public List<PresentationOutput> Outputs { get; set; } = new();
        [DataMember(Name = "reviews")] public PresentationReviews Reviews { get; set; } = new();
    }

    [DataContract]
    private sealed class PresentationAcceptedRun
    {
        [DataMember(Name = "runId")] public string RunId { get; set; } = string.Empty;
        [DataMember(Name = "sessionId")] public string SessionId { get; set; } = string.Empty;
        [DataMember(Name = "launchWindowStyle")] public string LaunchWindowStyle { get; set; } = string.Empty;
        [DataMember(Name = "visibleWindow")] public bool VisibleWindow { get; set; }
        [DataMember(Name = "reviewVerdict")] public string ReviewVerdict { get; set; } = string.Empty;
    }

    [DataContract]
    private sealed class PresentationRenderer
    {
        [DataMember(Name = "path")] public string Path { get; set; } = string.Empty;
        [DataMember(Name = "sha256")] public string Sha256 { get; set; } = string.Empty;
        [DataMember(Name = "imageMagickVersion")] public string ImageMagickVersion { get; set; } = string.Empty;
        [DataMember(Name = "fontPath")] public string FontPath { get; set; } = string.Empty;
        [DataMember(Name = "fontSha256")] public string FontSha256 { get; set; } = string.Empty;
    }

    [DataContract]
    private sealed class PresentationSource
    {
        [DataMember(Name = "token")] public string Token { get; set; } = string.Empty;
        [DataMember(Name = "path")] public string Path { get; set; } = string.Empty;
        [DataMember(Name = "originalScreenshot")] public string OriginalScreenshot { get; set; } = string.Empty;
        [DataMember(Name = "runId")] public string RunId { get; set; } = string.Empty;
        [DataMember(Name = "sessionId")] public string SessionId { get; set; } = string.Empty;
        [DataMember(Name = "launchWindowStyle")] public string LaunchWindowStyle { get; set; } = string.Empty;
        [DataMember(Name = "visibleWindow")] public bool VisibleWindow { get; set; }
        [DataMember(Name = "reviewVerdict")] public string ReviewVerdict { get; set; } = string.Empty;
        [DataMember(Name = "width")] public int Width { get; set; }
        [DataMember(Name = "height")] public int Height { get; set; }
        [DataMember(Name = "sha256")] public string Sha256 { get; set; } = string.Empty;
        [DataMember(Name = "placementReviewScope")] public string PlacementReviewScope { get; set; } = string.Empty;
        [DataMember(Name = "backgroundMeasurementToken", EmitDefaultValue = false)] public string BackgroundMeasurementToken { get; set; } = string.Empty;
        [DataMember(Name = "emptyMeasurementToken", EmitDefaultValue = false)] public string EmptyMeasurementToken { get; set; } = string.Empty;
        [DataMember(Name = "wallOnlyMeasurementToken", EmitDefaultValue = false)] public string WallOnlyMeasurementToken { get; set; } = string.Empty;
        [DataMember(Name = "northAndWallMeasurementToken", EmitDefaultValue = false)] public string NorthAndWallMeasurementToken { get; set; } = string.Empty;
        [DataMember(Name = "noShadowFinalMeasurementToken", EmitDefaultValue = false)] public string NoShadowFinalMeasurementToken { get; set; } = string.Empty;
        [DataMember(Name = "sharedEdgeProjection", EmitDefaultValue = false)]
        public PresentationSharedEdgeProjection? SharedEdgeProjection { get; set; }
    }

    [DataContract]
    private sealed class PresentationMeasurementInput
    {
        [DataMember(Name = "token")] public string Token { get; set; } = string.Empty;
        [DataMember(Name = "path")] public string Path { get; set; } = string.Empty;
        [DataMember(Name = "originalScreenshot")] public string OriginalScreenshot { get; set; } = string.Empty;
        [DataMember(Name = "runId")] public string RunId { get; set; } = string.Empty;
        [DataMember(Name = "sessionId")] public string SessionId { get; set; } = string.Empty;
        [DataMember(Name = "width")] public int Width { get; set; }
        [DataMember(Name = "height")] public int Height { get; set; }
        [DataMember(Name = "sha256")] public string Sha256 { get; set; } = string.Empty;
        [DataMember(Name = "mapId", EmitDefaultValue = false)] public string MapId { get; set; } = string.Empty;
        [DataMember(Name = "cameraWorldX", EmitDefaultValue = false)] public double CameraWorldX { get; set; }
        [DataMember(Name = "cameraWorldZ", EmitDefaultValue = false)] public double CameraWorldZ { get; set; }
        [DataMember(Name = "cameraRootSize", EmitDefaultValue = false)] public double CameraRootSize { get; set; }
        [DataMember(Name = "viewportX", EmitDefaultValue = false)] public int ViewportX { get; set; }
        [DataMember(Name = "viewportY", EmitDefaultValue = false)] public int ViewportY { get; set; }
    }

    [DataContract]
    private sealed class PresentationSharedEdgeProjection
    {
        [DataMember(Name = "mapId")] public string MapId { get; set; } = string.Empty;
        [DataMember(Name = "cameraWorldX")] public double CameraWorldX { get; set; }
        [DataMember(Name = "cameraWorldZ")] public double CameraWorldZ { get; set; }
        [DataMember(Name = "cameraRootSize")] public double CameraRootSize { get; set; }
        [DataMember(Name = "viewportX")] public int ViewportX { get; set; }
        [DataMember(Name = "viewportY")] public int ViewportY { get; set; }
        [DataMember(Name = "screenX")] public double ScreenX { get; set; }
        [DataMember(Name = "screenYTop")] public double ScreenYTop { get; set; }
        [DataMember(Name = "startScreenX")] public double StartScreenX { get; set; }
        [DataMember(Name = "startScreenYTop")] public double StartScreenYTop { get; set; }
        [DataMember(Name = "endScreenX")] public double EndScreenX { get; set; }
        [DataMember(Name = "endScreenYTop")] public double EndScreenYTop { get; set; }
        [DataMember(Name = "ownerCellCenterScreenX")] public double OwnerCellCenterScreenX { get; set; }
        [DataMember(Name = "ownerCellCenterScreenYTop")] public double OwnerCellCenterScreenYTop { get; set; }
        [DataMember(Name = "oppositeCellCenterScreenX")] public double OppositeCellCenterScreenX { get; set; }
        [DataMember(Name = "oppositeCellCenterScreenYTop")] public double OppositeCellCenterScreenYTop { get; set; }
        [DataMember(Name = "screenshotWidth")] public int ScreenshotWidth { get; set; }
        [DataMember(Name = "screenshotHeight")] public int ScreenshotHeight { get; set; }
        [DataMember(Name = "ownedSide")] public string OwnedSide { get; set; } = string.Empty;
    }

    [DataContract]
    private sealed class PresentationReviews
    {
        [DataMember(Name = "placementAudits")]
        public List<PresentationPlacementAudit> PlacementAudits { get; set; } = new();
    }

    [DataContract]
    private sealed class PresentationPlacementAudit
    {
        [DataMember(Name = "targetKind")] public string TargetKind { get; set; } = string.Empty;
        [DataMember(Name = "targetToken")] public string TargetToken { get; set; } = string.Empty;
        [DataMember(Name = "reviewerTask")] public string ReviewerTask { get; set; } = string.Empty;
        [DataMember(Name = "verdict")] public string Verdict { get; set; } = string.Empty;
        [DataMember(Name = "items")] public List<PresentationPlacementAuditItem> Items { get; set; } = new();
    }

    [DataContract]
    private sealed class PresentationPlacementAuditItem
    {
        [DataMember(Name = "category")] public string Category { get; set; } = string.Empty;
        [DataMember(Name = "visibleCount")] public int VisibleCount { get; set; }
        [DataMember(Name = "reviewedCount")] public int ReviewedCount { get; set; }
        [DataMember(Name = "unresolvedFindingCount")] public int UnresolvedFindingCount { get; set; }
        [DataMember(Name = "observation")] public string Observation { get; set; } = string.Empty;
    }

    [DataContract]
    private sealed class PresentationCard
    {
        [DataMember(Name = "token")] public string Token { get; set; } = string.Empty;
    }

    [DataContract]
    private sealed class PresentationOutput
    {
        [DataMember(Name = "token")] public string Token { get; set; } = string.Empty;
        [DataMember(Name = "role")] public string Role { get; set; } = string.Empty;
        [DataMember(Name = "path")] public string Path { get; set; } = string.Empty;
        [DataMember(Name = "alt")] public string Alt { get; set; } = string.Empty;
        [DataMember(Name = "width")] public int Width { get; set; }
        [DataMember(Name = "height")] public int Height { get; set; }
        [DataMember(Name = "bytes")] public long Bytes { get; set; }
        [DataMember(Name = "sha256")] public string Sha256 { get; set; } = string.Empty;
        [DataMember(Name = "sourceTokens")] public List<string> SourceTokens { get; set; } = new();
        [DataMember(Name = "placementReviewScope")] public string PlacementReviewScope { get; set; } = string.Empty;
    }
}
