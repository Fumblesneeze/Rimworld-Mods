using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;
using System.Web.Script.Serialization;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
[NonParallelizable]
public sealed class ImmersiveChefsReleaseScriptBehaviorTests
{
    [Test]
    public void Published_identity_is_checked_in_and_matches_the_release_descriptor()
    {
        using var fixture = Fixture.Create();
        var root = fixture.RepositoryRoot;
        var aboutIdentityPath = Path.Combine(root, "mods", "ImmersiveChefs", "About", "PublishedFileId.txt");
        var releasePath = Path.Combine(root, "mods", "ImmersiveChefs", "Release", "release.json");
        var release = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(releasePath));

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(aboutIdentityPath), Is.True,
                "An already published mod must retain its Workshop identity in the checked-in About directory.");
            Assert.That(File.ReadAllText(aboutIdentityPath).Trim(), Is.EqualTo("3782589902"));
            Assert.That(Convert.ToString(release["publishedFileId"]), Is.EqualTo("3782589902"));
        });
    }

    [Test]
    public void Release_builder_decodes_the_real_title_preview_dimensions_without_byte_overflow()
    {
        using var fixture = Fixture.Create();
        var previewPath = Path.Combine(
            fixture.RepositoryRoot, "mods", "ImmersiveChefs", "Release", "workshop", "preview-main.png");
        var run = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Get-RasterImageInfo" },
            "$image=Get-RasterImageInfo " + Ps(previewPath) + ";Write-Output ($image.format+'|'+$image.width+'|'+$image.height)");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("png|1280|720"));
        });
    }

    [Test]
    public void Release_tests_do_not_launch_unrelated_visible_desktop_applications()
    {
        using var fixture = Fixture.Create();
        var source = File.ReadAllText(Path.Combine(
            fixture.RepositoryRoot,
            "tests", "ImmersiveChefs.Tests", "ImmersiveChefsReleaseScriptBehaviorTests.cs"));

        var visibleEditorExecutable = "note" + "pad.exe";
        Assert.That(source, Does.Not.Contain(visibleEditorExecutable).IgnoreCase,
            "Process-lease fixtures must use an owned hidden noninteractive child, not flash Notepad on the desktop.");
    }

    [Test]
    public void Staged_inventory_accepts_an_identity_already_in_the_manifest_without_double_counting_it()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Read-Json", "Get-RelativePath", "Assert-StagedCandidate" },
            $"Assert-StagedCandidate ([pscustomobject]@{{ packageManifestPath = {Ps(fixture.ManifestPath)}; packagePath = {Ps(fixture.PackageRoot)}; generatedAtPublication = @('About\\PublishedFileId.txt') }}); Write-Output 'accepted'");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("accepted"));
        });
    }

    [Test]
    public void Staged_inventory_rejects_an_unreviewed_extra_file()
    {
        using var fixture = Fixture.Create();
        File.WriteAllText(Path.Combine(fixture.PackageRoot, "unreviewed.txt"), "x", new UTF8Encoding(false));
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Read-Json", "Get-RelativePath", "Assert-StagedCandidate" },
            $"Assert-StagedCandidate ([pscustomobject]@{{ packageManifestPath = {Ps(fixture.ManifestPath)}; packagePath = {Ps(fixture.PackageRoot)}; generatedAtPublication = @() }})");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("Unexpected staged file"));
        });
    }

    [Test]
    public void Workshop_identity_is_recovered_from_one_receipt_and_conflicting_receipts_fail_closed()
    {
        using var fixture = Fixture.Create();
        var stateRoot = Path.Combine(fixture.Root, "state");
        var receiptRoot = Path.Combine(stateRoot, "publication", "one");
        Directory.CreateDirectory(receiptRoot);
        File.WriteAllText(Path.Combine(receiptRoot, "publication-receipt.json"),
            "{\"schema\":\"ImmersiveChefs/WorkshopPublicationReceipt/v1\",\"publishedFileId\":\"123456789\"}",
            new UTF8Encoding(false));
        var recovered = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Resolve-ExistingWorkshopIdentity" },
            $"Write-Output (Resolve-ExistingWorkshopIdentity -DeclaredId $null -StateRoot {Ps(stateRoot)})");
        Assert.Multiple(() =>
        {
            Assert.That(recovered.ExitCode, Is.Zero, recovered.StandardError);
            Assert.That(recovered.StandardOutput.Trim(), Is.EqualTo("123456789"));
        });

        var second = Path.Combine(stateRoot, "publication", "two");
        Directory.CreateDirectory(second);
        File.WriteAllText(Path.Combine(second, "publication-receipt.json"),
            "{\"schema\":\"ImmersiveChefs/WorkshopPublicationReceipt/v1\",\"publishedFileId\":\"987654321\"}",
            new UTF8Encoding(false));
        var conflict = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Resolve-ExistingWorkshopIdentity" },
            $"Write-Output (Resolve-ExistingWorkshopIdentity -DeclaredId $null -StateRoot {Ps(stateRoot)})");
        Assert.Multiple(() =>
        {
            Assert.That(conflict.ExitCode, Is.EqualTo(1));
            Assert.That(conflict.StandardError, Does.Contain("identity evidence conflicts"));
        });
    }

    [Test]
    public void Checked_in_Workshop_identity_participates_in_conflict_detection()
    {
        using var fixture = Fixture.Create();
        var stateRoot = Path.Combine(fixture.Root, "state");
        Directory.CreateDirectory(stateRoot);
        var checkedInIdentity = Path.Combine(fixture.Root, "PublishedFileId.txt");
        File.WriteAllText(checkedInIdentity, "123456789", new UTF8Encoding(false));

        var accepted = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Resolve-ExistingWorkshopIdentity" },
            $"Write-Output (Resolve-ExistingWorkshopIdentity -DeclaredId '123456789' -StateRoot {Ps(stateRoot)} -SourceIdentityPath {Ps(checkedInIdentity)})");
        var rejected = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Resolve-ExistingWorkshopIdentity" },
            $"Write-Output (Resolve-ExistingWorkshopIdentity -DeclaredId '987654321' -StateRoot {Ps(stateRoot)} -SourceIdentityPath {Ps(checkedInIdentity)})");

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(accepted.StandardOutput.Trim(), Is.EqualTo("123456789"));
            Assert.That(rejected.ExitCode, Is.EqualTo(1));
            Assert.That(rejected.StandardError, Does.Contain("identity evidence conflicts"));
        });
    }

    [Test]
    public void Release_scripts_parse_under_the_installed_Windows_PowerShell_host()
    {
        using var fixture = Fixture.Create();
        foreach (var script in new[] { "Build-ImmersiveChefsRelease.ps1", "Invoke-ImmersiveChefsWorkshopRelease.ps1", "Invoke-RimWorldShowcaseCapture.ps1", "Invoke-RimWorldShowcaseAssembly.ps1" })
        {
            var parsed = fixture.ParseWithWindowsPowerShell(script);
            Assert.That(parsed.ExitCode, Is.Zero, script + Environment.NewLine + parsed.StandardError);
        }
    }

    [Test]
    public void Showcase_capture_owns_a_hard_five_second_deadline_and_exact_encoder_provenance()
    {
        using var fixture = Fixture.Create();
        var source = File.ReadAllText(Path.Combine(fixture.RepositoryRoot, "scripts", "Invoke-RimWorldShowcaseCapture.ps1"));
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("$captureDeadlineMilliseconds = 5000"));
            Assert.That(source, Does.Contain("Invoke-BoundedChildProcess"));
            Assert.That(source, Does.Contain("actualCaptureDurationMilliseconds"));
            Assert.That(source, Does.Contain("actualDurationSeconds"));
            Assert.That(source, Does.Contain("processStartUtc"));
            Assert.That(source, Does.Contain("stateBefore"));
            Assert.That(source, Does.Contain("stateAfter"));
            Assert.That(source, Does.Contain("encoderArguments"));
            Assert.That(source, Does.Contain("encoderVersion"));
            Assert.That(source, Does.Contain("ffprobe duration query"));
        });
    }

    [Test]
    public void Showcase_capture_terminates_the_exact_owned_child_after_a_timeout()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-RimWorldShowcaseCapture.ps1",
            new[] { "ConvertTo-QuotedProcessArgument", "Invoke-BoundedChildProcess" },
            "try { $null=Invoke-BoundedChildProcess -FilePath 'pwsh.exe' -Arguments @('-NoProfile','-NonInteractive','-Command','Start-Sleep -Seconds 30') -TimeoutMilliseconds 100 -Label 'hung fixture'; throw 'expected timeout' } " +
            "catch { Write-Output $_.Exception.Message }");
        Assert.That(run.ExitCode, Is.Zero, run.StandardError);
        var match = Regex.Match(run.StandardOutput, @"terminated owned PID (?<pid>\d+)");
        Assert.That(match.Success, Is.True, run.StandardOutput);
        Assert.That(Process.GetProcesses().Any(process => process.Id == int.Parse(match.Groups["pid"].Value)), Is.False,
            "The timed-out exact child remained alive after the capture command returned.");
    }

    [Test]
    public void Showcase_hard_cut_assembly_requires_same_process_and_preserves_every_segment_beat()
    {
        using var fixture = Fixture.Create();
        var source = File.ReadAllText(Path.Combine(fixture.RepositoryRoot, "scripts", "Invoke-RimWorldShowcaseAssembly.ps1"));
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("RimWorldDevGateway/ShowcaseCaptureAssemblyEvidence/v1"));
            Assert.That(source, Does.Contain("processStartUtc"));
            Assert.That(source, Does.Contain("orderedPackageIdentities"));
            Assert.That(source, Does.Contain("observedBeats"));
            Assert.That(source, Does.Contain("segmentCaptureSha256"));
            Assert.That(source, Does.Contain("maximumDurationSeconds = 5"));
        });
    }

    [Test]
    public void Change_history_is_required_for_updates_but_not_for_first_publication_or_preview_sync()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-WorkshopChangeHistoryRequired" },
            "$update=Test-WorkshopChangeHistoryRequired $false $false;" +
            "$first=Test-WorkshopChangeHistoryRequired $true $false;" +
            "$preview=Test-WorkshopChangeHistoryRequired $false $true;" +
            "Write-Output ($update.ToString()+'|'+$first.ToString()+'|'+$preview.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False|False"));
        });
    }

    [Test]
    public void Showcase_release_evidence_binds_every_declared_output_and_rejects_tampering()
    {
        using var fixture = Fixture.Create();
        var releaseRoot = Path.Combine(fixture.Root, "release");
        var assetRoot = Path.Combine(releaseRoot, "workshop", "assets", "showcases");
        var frameRoot = Path.Combine(assetRoot, "frames");
        Directory.CreateDirectory(assetRoot);
        Directory.CreateDirectory(frameRoot);
        var screenshotPath = Path.Combine(assetRoot, "service.png");
        var gifPath = Path.Combine(assetRoot, "service.gif");
        var framePath = Path.Combine(frameRoot, "frame-0001.png");
        var reviewedProductRoot = Path.Combine(fixture.Root, "reviewed-product");
        var reviewedProductAssembly = Path.Combine(reviewedProductRoot, "1.6", "Assemblies", "ImmersiveChefs.dll");
        var reviewedProductDef = Path.Combine(reviewedProductRoot, "1.6", "Defs", "ThingDefs.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(reviewedProductAssembly));
        Directory.CreateDirectory(Path.GetDirectoryName(reviewedProductDef));
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        File.WriteAllBytes(screenshotPath, png);
        File.WriteAllBytes(framePath, png);
        File.WriteAllBytes(reviewedProductAssembly, new byte[] { 1, 2, 3, 4 });
        File.WriteAllText(reviewedProductDef, "<Defs />", new UTF8Encoding(false));
        string Sha(string path)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }
        var screenshotHash = Sha(screenshotPath);
        var ffmpegPath = fixture.ResolveCommand("ffmpeg.exe");
        var ffprobePath = fixture.ResolveCommand("ffprobe.exe");
        var magickPath = fixture.ResolveCommand("magick.exe");
        var ffmpegVersion = Fixture.RunProcess(ffmpegPath, "-version").StandardOutput.Split('\n')[0].TrimEnd('\r');
        var ffprobeVersion = Fixture.RunProcess(ffprobePath, "-version").StandardOutput.Split('\n')[0].TrimEnd('\r');
        var filter = "fps=1,scale=1:-1:flags=lanczos,split[s0][s1];[s0]palettegen=max_colors=96:stats_mode=diff[p];[s1][p]paletteuse=dither=bayer:bayer_scale=5:diff_mode=rectangle";
        var encode = Fixture.RunProcess(ffmpegPath,
            "-hide_banner -loglevel error -y -framerate 1 -i \"" + Path.Combine(assetRoot, "frames", "frame-%04d.png") + "\" -filter_complex \"" + filter + "\" -loop 0 \"" + gifPath + "\"");
        Assert.That(encode.ExitCode, Is.Zero, encode.StandardError);
        var gifHash = Sha(gifPath);
        var evidencePath = Path.Combine(assetRoot, "service.capture.json");
        File.WriteAllText(evidencePath,
            "{\"schema\":\"RimWorldDevGateway/ShowcaseCaptureEvidence/v1\",\"showcaseId\":\"service\",\"capturedUtc\":\"2026-08-13T12:00:00Z\"," +
            "\"processId\":123,\"processStartUtc\":\"2026-08-13T11:59:00Z\",\"runId\":\"0123456789abcdef0123456789abcdef\"," +
            "\"orderedPackageIds\":[\"ludeon.rimworld\",\"fumblesneeze.immersivechefs\",\"fumblesneeze.rimworlddevgateway\"]," +
            "\"orderedPackageIdentities\":[" +
            "{\"index\":0,\"packageId\":\"ludeon.rimworld\",\"name\":\"Core\",\"rootDir\":\"C:/Core\",\"files\":[{\"path\":\"About/About.xml\",\"bytes\":1,\"sha256\":\"" + new string('A', 64) + "\"}]}," +
            "{\"index\":1,\"packageId\":\"fumblesneeze.immersivechefs\",\"name\":\"Immersive Chefs\",\"rootDir\":\"C:/Product\",\"files\":[" +
            "{\"path\":\"1.6/Assemblies/ImmersiveChefs.dll\",\"bytes\":4,\"sha256\":\"" + Sha(reviewedProductAssembly) + "\"}," +
            "{\"path\":\"1.6/Defs/ThingDefs.xml\",\"bytes\":" + new FileInfo(reviewedProductDef).Length + ",\"sha256\":\"" + Sha(reviewedProductDef) + "\"}]}," +
            "{\"index\":2,\"packageId\":\"fumblesneeze.rimworlddevgateway\",\"name\":\"Gateway\",\"rootDir\":\"C:/Gateway\",\"files\":[{\"path\":\"About/About.xml\",\"bytes\":1,\"sha256\":\"" + new string('B', 64) + "\"}]}]," +
            "\"observedBeats\":[\"order\"],\"reviewObservation\":\"The native order is visible.\"," +
            "\"stateBefore\":{\"camera\":{\"x\":1},\"selectedThings\":[],\"uiSelection\":[]}," +
            "\"stateAfter\":{\"camera\":{\"x\":1},\"selectedThings\":[],\"uiSelection\":[]}," +
            "\"selectionMutated\":false,\"cameraMutated\":false," +
            "\"plan\":{\"schema\":\"RimWorldDevGateway/ShowcaseCapturePlan/v1\",\"frameCount\":1,\"framesPerSecond\":1,\"durationSeconds\":1," +
            "\"width\":1,\"height\":1,\"offsetX\":0,\"offsetY\":0,\"gifWidth\":1,\"gifColors\":96,\"mutatesGameState\":false,\"selectsThings\":false}," +
            "\"actualCaptureDurationMilliseconds\":900," +
            "\"frames\":[{\"index\":1,\"elapsedMilliseconds\":700,\"path\":\"frames/frame-0001.png\",\"bytes\":" + png.Length + "," +
            "\"sha256\":\"" + screenshotHash + "\",\"crop\":{\"frameWidth\":1,\"frameHeight\":1,\"x\":0,\"y\":0,\"height\":1,\"width\":1}}]," +
            "\"pngOptimizer\":{\"name\":\"magick.exe\",\"sha256\":\"" + Sha(magickPath) + "\",\"version\":" + new JavaScriptSerializer().Serialize(Fixture.RunProcess(magickPath, "-version").StandardOutput.Split('\n')[0].TrimEnd('\r')) + ",\"arguments\":[\"-strip\",\"-colors\",\"256\",\"-define\",\"png:compression-level=9\",\"-define\",\"png:compression-filter=5\"]}," +
            "\"still\":{\"path\":\"service.png\",\"bytes\":" + new FileInfo(screenshotPath).Length + ",\"sha256\":\"" + screenshotHash + "\"}," +
            "\"gif\":{\"path\":\"service.gif\",\"bytes\":" + new FileInfo(gifPath).Length + ",\"sha256\":\"" + gifHash +
            "\",\"actualDurationSeconds\":1,\"encoderName\":\"ffmpeg.exe\",\"encoderSha256\":\"" + Sha(ffmpegPath) + "\",\"encoderVersion\":" + new JavaScriptSerializer().Serialize(ffmpegVersion) + "," +
            "\"encoderArguments\":[],\"encoderFilter\":\"\",\"probeName\":\"ffprobe.exe\",\"probeSha256\":\"" + Sha(ffprobePath) + "\",\"probeVersion\":" + new JavaScriptSerializer().Serialize(ffprobeVersion) + ",\"probeArguments\":[]}," +
            "\"bearerTokenRetained\":false}", new UTF8Encoding(false));
        var showcasePath = Path.Combine(fixture.Root, "showcase.json");
        File.WriteAllText(showcasePath,
            "{\"id\":\"service\",\"formats\":[\"screenshot\",\"gif\"],\"requiredPackageIds\":[\"ludeon.rimworld\",\"fumblesneeze.immersivechefs\"],\"beats\":[\"order\"],\"crop\":{\"width\":1,\"height\":1,\"offsetX\":0,\"offsetY\":0}," +
            "\"outputs\":{\"screenshot\":\"assets/showcases/service.png\",\"gif\":\"assets/showcases/service.gif\"}}",
            new UTF8Encoding(false));

        var encoderArgs = new[] { "-hide_banner", "-loglevel", "error", "-y", "-framerate", "1", "-i", "frames/frame-%04d.png", "-filter_complex", filter, "-loop", "0", "service.gif" };
        var probeArgs = new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", "service.gif" };
        var evidenceText = File.ReadAllText(evidencePath)
            .Replace("\"encoderArguments\":[]", "\"encoderArguments\":" + new JavaScriptSerializer().Serialize(encoderArgs))
            .Replace("\"encoderFilter\":\"\"", "\"encoderFilter\":" + new JavaScriptSerializer().Serialize(filter))
            .Replace("\"probeArguments\":[]", "\"probeArguments\":" + new JavaScriptSerializer().Serialize(probeArgs));
        File.WriteAllText(evidencePath, evidenceText, new UTF8Encoding(false));
        var operation = "$s=Get-Content -LiteralPath " + Ps(showcasePath) + " -Raw | ConvertFrom-Json;" +
                        "$e=Get-WorkshopShowcaseEvidence -Showcase $s -ReleaseRoot " + Ps(releaseRoot) + " -ReviewedProductRoot " + Ps(reviewedProductRoot) + ";" +
                        "Write-Output ($e.showcaseId+'|'+$e.outputs.Count+'|'+$e.provenance.sha256.Length)";
        var accepted = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Get-RelativePath", "Get-CanonicalJson", "Invoke-ShowcaseMediaTool", "Get-RasterImageInfo", "Get-WorkshopShowcaseEvidence" }, operation);
        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(accepted.StandardOutput.Trim(), Is.EqualTo("service|2|64"));
        });

        File.AppendAllText(reviewedProductDef, "<!-- stale -->", new UTF8Encoding(false));
        var staleProductRejected = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Get-RelativePath", "Get-CanonicalJson", "Invoke-ShowcaseMediaTool", "Get-RasterImageInfo", "Get-WorkshopShowcaseEvidence" }, operation);
        Assert.Multiple(() =>
        {
            Assert.That(staleProductRejected.ExitCode, Is.EqualTo(1));
            Assert.That(staleProductRejected.StandardError, Does.Contain("complete reviewed Immersive Chefs package"));
        });
        File.WriteAllText(reviewedProductDef, "<Defs />", new UTF8Encoding(false));

        File.AppendAllText(gifPath, "tampered", new UTF8Encoding(false));
        var rejected = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Get-RelativePath", "Get-CanonicalJson", "Invoke-ShowcaseMediaTool", "Get-RasterImageInfo", "Get-WorkshopShowcaseEvidence" }, operation);
        Assert.Multiple(() =>
        {
            Assert.That(rejected.ExitCode, Is.EqualTo(1));
            Assert.That(rejected.StandardError, Does.Contain("does not match its capture evidence"));
        });
    }

    [Test]
    public void Showcase_design_evidence_binds_live_reviewed_references_rules_placements_and_packages()
    {
        using var fixture = Fixture.Create();
        var releaseRoot = Path.Combine(fixture.Root, "mods", "ImmersiveChefs", "Release");
        var designRoot = Path.Combine(releaseRoot, "workshop", "designs");
        Directory.CreateDirectory(designRoot);
        var designPath = Path.Combine(designRoot, "service.md");
        File.WriteAllText(designPath,
            "# Restaurant service\n\n" +
            "- Showcase ID: `service`\n" +
            "- Design-record version: `1`\n" +
            "- Owner: `fumblesneeze.immersivechefs`\n" +
            "- Status: `live-reviewed`\n" +
            "- Intended crop: `1280x720, fixed view`\n" +
            "- Colony brief: `Established temperate restaurant beside its working kitchen.`\n" +
            "- Exact presentation packages: `ludeon.rimworld -> fumblesneeze.immersivechefs`\n" +
            "- Native workflow: `guest orders -> cooks work -> waiter serves`\n\n" +
            "## References\n\n" +
            "| ID | Role in this scene | Inspected evidence | Eligible class |\n" +
            "|---|---|---|---|\n" +
            "| R01 | dining | formal hall | recurrent |\n" +
            "| R02 | kitchen | worker island | recurrent |\n" +
            "| R07 | restaurant | service rhythm | recurrent |\n\n" +
            "## Applied rules\n\n" +
            "| Rule ID | Class | Sources/contract | Decision in this scene |\n" +
            "|---|---|---|---|\n" +
            "| public-edge | recurrent | R01/R07 | dining faces the approach |\n" +
            "| kitchen-buffer | mechanical | Core jobs | service shelf borders kitchen |\n" +
            "| clear-lane | mechanical | work cells | interaction cells stay open |\n\n" +
            "## Adjacency graph\n\n" +
            "`freezer --high-frequency--> stove --service--> dining`\n\n" +
            "## Planned zones and placements\n\n" +
            "| Zone/object | Bounds or relation | Exact Def/rotation/material | Rule IDs | Rationale |\n" +
            "|---|---|---|---|---|\n" +
            "| kitchen | west room | Stove_Electric/south/steel | clear-lane | readable cooking |\n" +
            "| pass | shared wall | Shelf/east/wood | kitchen-buffer | short waiter path |\n" +
            "| dining | east room | Table2x2c/wood | public-edge | guest context |\n\n" +
            "## Variation and history\n\nMixed stone and wood show an expanded colony.\n\n" +
            "## Rejected drafts and deviations\n\n" +
            "| Draft/evidence | Rejection or deviation | Resulting rule/change |\n" +
            "|---|---|---|\n" +
            "| first frame | pass was hidden | moved shelf beside doorway |\n\n" +
            "## Live review\n\n" +
            "- Exact build/package identity: `ABC123`\n" +
            "- Exact process/evidence directory: `run/service`\n" +
            "- Player action observed: `A guest placed an order.`\n" +
            "- Visible result observed: `The waiter delivered the cooked meal.`\n" +
            "- Geometry/material/traffic observations at final crop size: `Kitchen and dining room remained legible.`\n" +
            "- Remaining caveats: `No camera cut was needed.`\n",
            new UTF8Encoding(false));
        var catalogPath = Path.Combine(fixture.Root, "source-catalog.md");
        File.WriteAllText(catalogPath, "| R01 | one |\n| R02 | two |\n| R07 | seven |\n", new UTF8Encoding(false));
        var showcasePath = Path.Combine(fixture.Root, "showcase.json");
        File.WriteAllText(showcasePath,
            "{\"id\":\"service\",\"designRecord\":\"designs/service.md\",\"requiredPackageIds\":[\"ludeon.rimworld\",\"fumblesneeze.immersivechefs\"]}",
            new UTF8Encoding(false));
        var operation = "$s=Get-Content -LiteralPath " + Ps(showcasePath) + " -Raw | ConvertFrom-Json;" +
                        "$e=Get-WorkshopShowcaseDesignEvidence -Showcase $s -ReleaseRoot " + Ps(releaseRoot) + " -SourceCatalogPath " + Ps(catalogPath) + ";" +
                        "Write-Output ($e.showcaseId+'|'+$e.referenceIds.Count+'|'+$e.ruleCount+'|'+$e.placementCount+'|'+$e.sha256.Length)";
        var accepted = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Get-WorkshopShowcaseDesignEvidence" }, operation);
        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(accepted.StandardOutput.Trim(), Is.EqualTo("service|3|3|3|64"));
        });

        File.WriteAllText(designPath, File.ReadAllText(designPath).Replace("`live-reviewed`", "`draft`"), new UTF8Encoding(false));
        var rejected = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Get-WorkshopShowcaseDesignEvidence" }, operation);
        Assert.Multiple(() =>
        {
            Assert.That(rejected.ExitCode, Is.EqualTo(1));
            Assert.That(rejected.StandardError, Does.Contain("live-reviewed"));
        });
    }

    [Test]
    public void Workshop_description_resolver_replaces_each_reviewed_image_token_and_rejects_unresolved_copy()
    {
        using var fixture = Fixture.Create();
        var repositoryRoot = fixture.RepositoryRoot;
        var inventoryPath = Path.Combine(fixture.Root, "preview-inventory.json");
        var outputPath = Path.Combine(fixture.Root, "description.bbcode");
        var provenancePath = Path.Combine(fixture.Root, "description.provenance.json");
        var tokens = new[]
        {
            "kitchenware", "teamwork", "meals", "colony", "compatibility",
            "showcase-gastronomy", "dishwashing", "showcase-nutrient-paste", "showcase-professional-prep", "showcase-memories"
        };
        var previewPlan = new string('A', 64);
        var previewEntries = tokens.Select((token, index) =>
        {
            var localPath = Path.Combine(fixture.Root, token + ".png");
            File.WriteAllText(localPath, "card-" + token, new UTF8Encoding(false));
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(localPath))).Replace("-", string.Empty);
            return "{\"token\":\"" + token + "\",\"localPath\":\"" + localPath.Replace("\\", "\\\\") +
                   "\",\"localSha256\":\"" + hash + "\",\"remoteSha256\":\"" + hash +
                   "\",\"remoteIndex\":" + index + ",\"remoteUrl\":\"https://images.steamusercontent.com/ugc/" +
                   (1000 + index) + "/card.png\",\"remoteType\":\"k_EItemPreviewType_Image\"}";
        }).ToArray();
        var authorizationTree = new string('B', 64);
        var authorizationIntent = new string('C', 64);
        File.WriteAllText(
            inventoryPath,
            "{\"schema\":\"ImmersiveChefs/WorkshopRemotePreviewInventory/v1\",\"publishedFileId\":\"3782589902\",\"publicationPlanSha256\":\"" +
            previewPlan + "\",\"authorizationSourceTreeSha256\":\"" + authorizationTree +
            "\",\"authorizationIntentSha256\":\"" + authorizationIntent + "\",\"previews\":[" + string.Join(",", previewEntries) +
            "]}",
            new UTF8Encoding(false));
        var rimWorldPath = typeof(ImmersiveChefsReleaseScriptBehaviorTests).Assembly
            .GetCustomAttributes<System.Reflection.AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RimWorldPath")
            .Value!;
        var script = Path.Combine(repositoryRoot, "scripts", "Resolve-ImmersiveChefsWorkshopDescription.ps1");
        var template = Path.Combine(repositoryRoot, "mods", "ImmersiveChefs", "Release", "workshop", "description.template.bbcode");
        var run = Fixture.RunProcess(
            "pwsh.exe",
            "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + script + "\"" +
            " -InventoryPath \"" + inventoryPath + "\"" +
            " -TemplatePath \"" + template + "\"" +
            " -DestinationPath \"" + outputPath + "\"" +
            " -RimWorldPath \"" + rimWorldPath + "\" -Output json");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(File.Exists(provenancePath), Is.True);
            var resolved = File.ReadAllText(outputPath);
            Assert.That(resolved, Does.Not.Contain("{{image:"));
            Assert.That(Regex.Matches(resolved, @"\[img\]https://images\.steamusercontent\.com/.+?\[/img\]").Count, Is.EqualTo(6));
            Assert.That(new FileInfo(outputPath).Length + 1, Is.LessThanOrEqualTo(8000));
            var provenance = File.ReadAllText(provenancePath);
            Assert.That(provenance, Does.Contain("3782589902"));
            Assert.That(provenance, Does.Contain(previewPlan));
            Assert.That(provenance, Does.Contain(authorizationTree));
            Assert.That(provenance, Does.Contain(authorizationIntent));
        });
    }

    [Test]
    public void Remote_visibility_mapping_accepts_private_first_publication_and_public_updates()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Get-ExpectedRemoteVisibility" },
            "$private=Get-ExpectedRemoteVisibility 'Private';$public=Get-ExpectedRemoteVisibility 'Public';Write-Output ($private+'|'+$public)");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("k_ERemoteStoragePublishedFileVisibilityPrivate|k_ERemoteStoragePublishedFileVisibilityPublic"));
        });
    }

    [Test]
    public void Final_publication_requires_the_exact_terminal_preview_state_identity()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-PresentationPreviewStateAllowsPublication" },
            "$ok=Test-PresentationPreviewStateAllowsPublication 'succeeded' 'PREVIEW' 3782589902 'PREVIEW' 3782589902;" +
            "$pending=Test-PresentationPreviewStateAllowsPublication 'preview-submit-admitted' 'PREVIEW' 3782589902 'PREVIEW' 3782589902;" +
            "$wrong=Test-PresentationPreviewStateAllowsPublication 'succeeded' 'OTHER' 3782589902 'PREVIEW' 3782589902;" +
            "Write-Output ($ok.ToString()+'|'+$pending.ToString()+'|'+$wrong.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False|False"));
        });
    }

    [Test]
    public void First_publication_may_bootstrap_only_private_copy_without_unresolved_image_tokens()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-WorkshopPresentationAllowsPublication" },
            "$resolved=Test-WorkshopPresentationAllowsPublication $true $false 3782589902 $false 'Public';" +
            "$bootstrap=Test-WorkshopPresentationAllowsPublication $false $false 0 $true 'Private';" +
            "$tokens=Test-WorkshopPresentationAllowsPublication $false $true 0 $true 'Private';" +
            "$public=Test-WorkshopPresentationAllowsPublication $false $false 0 $true 'Public';" +
            "Write-Output ($resolved.ToString()+'|'+$bootstrap.ToString()+'|'+$tokens.ToString()+'|'+$public.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|True|False|False"));
        });
    }

    [Test]
    public void Resolved_description_provenance_must_match_every_current_preview_hash_and_slot()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Test-WorkshopPreviewProvenance", "Test-WorkshopAuthorizationProvenance" },
            "$current=@([pscustomobject]@{token='hero';path='hero.png';sha256='AAAA'},[pscustomobject]@{token='meals';path='meals.png';sha256='BBBB'});" +
            "$exact=@([pscustomobject]@{token='hero';remoteIndex=0;remoteUrl='https://images.steamusercontent.com/ugc/1/A/';localPath='hero.png';localSha256='AAAA';remoteSha256='AAAA';remoteType='k_EItemPreviewType_Image'}," +
            "[pscustomobject]@{token='meals';remoteIndex=1;remoteUrl='https://images.steamusercontent.com/ugc/2/B/';localPath='meals.png';localSha256='BBBB';remoteSha256='BBBB';remoteType='k_EItemPreviewType_Image'});" +
            "$drift=@([pscustomobject]@{token='hero';remoteIndex=0;remoteUrl='https://images.steamusercontent.com/ugc/1/A/';localPath='hero.png';localSha256='AAAA';remoteSha256='AAAA';remoteType='k_EItemPreviewType_Image'}," +
            "[pscustomobject]@{token='meals';remoteIndex=1;remoteUrl='https://images.steamusercontent.com/ugc/2/B/';localPath='meals.png';localSha256='CCCC';remoteSha256='CCCC';remoteType='k_EItemPreviewType_Image'});" +
            "$exactProvenance=[pscustomobject]@{authorizationSourceTreeSha256=('E'*64);authorizationIntentSha256=('D'*64);previews=$exact};" +
            "$driftedIntent=[pscustomobject]@{authorizationSourceTreeSha256=('E'*64);authorizationIntentSha256=('C'*64);previews=$exact};" +
            "$ok=Test-WorkshopAuthorizationProvenance -AuthorizationSourceTreeSha256 ('E'*64) -AuthorizationIntentSha256 ('D'*64) -CurrentPreviews $current -Provenance $exactProvenance;" +
            "$previewDrift=Test-WorkshopAuthorizationProvenance -AuthorizationSourceTreeSha256 ('E'*64) -AuthorizationIntentSha256 ('D'*64) -CurrentPreviews $current -Provenance ([pscustomobject]@{authorizationSourceTreeSha256=('E'*64);authorizationIntentSha256=('D'*64);previews=$drift});" +
            "$intentDrift=Test-WorkshopAuthorizationProvenance -AuthorizationSourceTreeSha256 ('E'*64) -AuthorizationIntentSha256 ('D'*64) -CurrentPreviews $current -Provenance $driftedIntent;" +
            "$treeDrift=Test-WorkshopAuthorizationProvenance -AuthorizationSourceTreeSha256 ('E'*64) -AuthorizationIntentSha256 ('D'*64) -CurrentPreviews $current -Provenance ([pscustomobject]@{authorizationSourceTreeSha256=('F'*64);authorizationIntentSha256=('D'*64);previews=$exact});" +
            "$badHost=@($exact[0].PSObject.Copy(),$exact[1].PSObject.Copy());$badHost[1].remoteUrl='https://example.invalid/not-steam/';" +
            "$hostDrift=Test-WorkshopAuthorizationProvenance -AuthorizationSourceTreeSha256 ('E'*64) -AuthorizationIntentSha256 ('D'*64) -CurrentPreviews $current -Provenance ([pscustomobject]@{authorizationSourceTreeSha256=('E'*64);authorizationIntentSha256=('D'*64);previews=$badHost});" +
            "Write-Output ($ok.ToString()+'|'+$previewDrift.ToString()+'|'+$intentDrift.ToString()+'|'+$treeDrift.ToString()+'|'+$hostDrift.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False|False|False|False"));
        });
    }

    [Test]
    public void Resolved_description_must_be_the_exact_template_projection_of_verified_Steam_image_urls()
    {
        using var fixture = Fixture.Create();
        var template = Path.Combine(fixture.Root, "description.template.bbcode");
        var description = Path.Combine(fixture.Root, "description.bbcode");
        File.WriteAllText(template, "top [img]{{image:kitchenware}}[/img] middle [img]{{image:meals}}[/img] end", new UTF8Encoding(false));
        File.WriteAllText(description, "top [img]https://images.steamusercontent.com/ugc/1/A/[/img] middle [img]https://images.steamusercontent.com/ugc/2/B/[/img] end", new UTF8Encoding(false));
        var run = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Test-WorkshopResolvedDescriptionMatchesTemplate" },
            "$previews=@([pscustomobject]@{token='hero';remoteUrl='https://images.steamusercontent.com/ugc/0/H/'}," +
            "[pscustomobject]@{token='kitchenware';remoteUrl='https://images.steamusercontent.com/ugc/1/A/'}," +
            "[pscustomobject]@{token='meals';remoteUrl='https://images.steamusercontent.com/ugc/2/B/'});" +
            "$ok=Test-WorkshopResolvedDescriptionMatchesTemplate -TemplatePath " + Ps(template) + " -DescriptionPath " + Ps(description) + " -ProvenancePreviews $previews;" +
            "$previews[1].remoteUrl='https://example.invalid/not-steam/';$badHost=Test-WorkshopResolvedDescriptionMatchesTemplate -TemplatePath " + Ps(template) + " -DescriptionPath " + Ps(description) + " -ProvenancePreviews $previews;$previews[1].remoteUrl='https://images.steamusercontent.com/ugc/1/A/';" +
            "Set-Content -LiteralPath " + Ps(description) + " -Value 'tampered copy with a self-consistent hash' -NoNewline -Encoding utf8;" +
            "$tampered=Test-WorkshopResolvedDescriptionMatchesTemplate -TemplatePath " + Ps(template) + " -DescriptionPath " + Ps(description) + " -ProvenancePreviews $previews;" +
            "Write-Output ($ok.ToString()+'|'+$badHost.ToString()+'|'+$tampered.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False|False"));
        });
    }

    [Test]
    public void Release_authorization_tree_ignores_only_Steam_URL_outputs_and_detects_authored_or_product_drift()
    {
        using var fixture = Fixture.Create();
        var repository = Path.Combine(fixture.Root, "authorization-repository");
        var workshop = Path.Combine(repository, "mods", "ImmersiveChefs", "Release", "workshop");
        var source = Path.Combine(repository, "mods", "ImmersiveChefs", "Source");
        Directory.CreateDirectory(workshop);
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(workshop, "description.bbcode"), "old Steam URL", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(workshop, "description.provenance.json"), "old provenance", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(workshop, "description.template.bbcode"), "authored copy", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(source, "Cookware.cs"), "product source", new UTF8Encoding(false));
        Assert.That(Fixture.RunProcess("git", $"-C \"{repository}\" init").ExitCode, Is.Zero);
        Assert.That(Fixture.RunProcess("git", $"-C \"{repository}\" config user.email test@example.invalid").ExitCode, Is.Zero);
        Assert.That(Fixture.RunProcess("git", $"-C \"{repository}\" config user.name test").ExitCode, Is.Zero);
        Assert.That(Fixture.RunProcess("git", $"-C \"{repository}\" add .").ExitCode, Is.Zero);
        Assert.That(Fixture.RunProcess("git", $"-C \"{repository}\" commit -m initial").ExitCode, Is.Zero);

        string Digest()
        {
            var invocation = fixture.InvokeFunctions(
                "Build-ImmersiveChefsRelease.ps1",
                new[] { "Get-Sha256Text", "Get-ReleaseAuthorizationSourceTreeSha256" },
                "Write-Output (Get-ReleaseAuthorizationSourceTreeSha256 " + Ps(repository) + ")");
            Assert.That(invocation.ExitCode, Is.Zero, invocation.StandardError);
            return invocation.StandardOutput.Trim();
        }

        var initial = Digest();
        File.WriteAllText(Path.Combine(workshop, "description.bbcode"), "new Steam URL", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(workshop, "description.provenance.json"), "new provenance", new UTF8Encoding(false));
        Assert.That(Fixture.RunProcess("git", $"-C \"{repository}\" add .").ExitCode, Is.Zero);
        Assert.That(Fixture.RunProcess("git", $"-C \"{repository}\" commit -m resolved").ExitCode, Is.Zero);
        var urlOnly = Digest();
        File.WriteAllText(Path.Combine(workshop, "description.template.bbcode"), "changed authored copy", new UTF8Encoding(false));
        Assert.That(Fixture.RunProcess("git", $"-C \"{repository}\" add .").ExitCode, Is.Zero);
        Assert.That(Fixture.RunProcess("git", $"-C \"{repository}\" commit -m drift").ExitCode, Is.Zero);
        var authoredDrift = Digest();

        Assert.Multiple(() =>
        {
            Assert.That(initial, Does.Match("^[A-F0-9]{64}$"));
            Assert.That(urlOnly, Is.EqualTo(initial));
            Assert.That(authoredDrift, Is.Not.EqualTo(initial));
        });
        foreach (var path in Directory.GetFiles(repository, "*", SearchOption.AllDirectories))
            File.SetAttributes(path, FileAttributes.Normal);
    }

    [Test]
    public void Workshop_change_history_is_parsed_in_remote_order_and_rejects_reused_notes()
    {
        using var fixture = Fixture.Create();
        var html = "<div class=\"detailBox changeLogCtn\"><div class=\"changelog headline\">Update: now</div>" +
                   "<p id=\"22\">Newest &amp; specific.</p></div>" +
                   "<div class=\"detailBox changeLogCtn\"><div class=\"changelog headline\">Update: before</div>" +
                   "<p id=\"11\">Previous note.</p></div>";
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "ConvertFrom-WorkshopChangeHistoryHtml", "Test-WorkshopChangeNotePreflight" },
            "$notes=@(ConvertFrom-WorkshopChangeHistoryHtml -Html " + Ps(html) + " -Uri 'https://steam/changelog/7');" +
            "$ok=Test-WorkshopChangeNotePreflight -RemoteNotes $notes -ExpectedPreviousNote 'Newest & specific.' -NewNote 'A different player-facing note.';" +
            "$reuse=Test-WorkshopChangeNotePreflight -RemoteNotes $notes -ExpectedPreviousNote 'Newest & specific.' -NewNote 'Previous note.';" +
            "Write-Output ($notes[0].id+'|'+$notes[0].note+'|'+$ok.ToString()+'|'+$reuse.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("22|Newest & specific.|True|False"));
        });
    }

    [Test]
    public void Workshop_change_history_uses_a_normal_bounded_browser_request_shape()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Initialize-WorkshopChangeHistoryRequest" },
            "$request=[Net.HttpWebRequest]::CreateHttp('https://steamcommunity.com/');" +
            "Initialize-WorkshopChangeHistoryRequest $request;" +
            "Write-Output ($request.UserAgent+'|'+$request.Accept+'|'+$request.Headers['Accept-Language']+'|'+$request.Timeout+'|'+$request.ReadWriteTimeout)");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Does.StartWith("Mozilla/5.0 (Windows NT 10.0; Win64; x64)"));
            Assert.That(run.StandardOutput, Does.Contain("|text/html,"));
            Assert.That(run.StandardOutput, Does.Contain("|en-US,en;q=0.9|30000|30000"));
        });
    }

    [Test]
    public void Workshop_change_history_allows_one_first_update_after_a_noted_or_unnoted_private_bootstrap()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-WorkshopChangeNotePreflight" },
            "$empty=Test-WorkshopChangeNotePreflight -RemoteNotes @() -ExpectedPreviousNote '' -NewNote 'A specific first public update note.';" +
            "$unexpected=Test-WorkshopChangeNotePreflight -RemoteNotes @([pscustomobject]@{note='unexpected'}) -ExpectedPreviousNote '' -NewNote 'A specific first public update note.';" +
            "$tracked=Test-WorkshopChangeNotePreflight -RemoteNotes @([pscustomobject]@{note='private bootstrap'}) -ExpectedPreviousNote 'private bootstrap' -NewNote 'A specific first public update note.';" +
            "Write-Output ($empty.ToString()+'|'+$unexpected.ToString()+'|'+$tracked.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False|True"));
        });
    }

    [Test]
    public void Workshop_change_history_retries_one_explicit_rate_limit_before_succeeding()
    {
        using var fixture = Fixture.Create();
        const string html =
            "<div class=\"detailBox changeLogCtn\"><div class=\"changelog headline\">Update: recovered</div>" +
            "<p id=\"22\">Recovered note.</p></div>";
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "ConvertFrom-WorkshopChangeHistoryHtml", "Get-WorkshopChangeNotes" },
            "$script:attempt=0;$script:delays=[System.Collections.Generic.List[int]]::new();" +
            "$request={param($uri)$script:attempt++;if($script:attempt -eq 1){return [pscustomobject]@{Succeeded=$false;StatusCode=429;RetryAfterMilliseconds=7}};" +
            "return [pscustomobject]@{Succeeded=$true;StatusCode=200;RetryAfterMilliseconds=0;Html=" + Ps(html) + "}};" +
            "$delay={param($milliseconds)$script:delays.Add([int]$milliseconds)};" +
            "$notes=@(Get-WorkshopChangeNotes -PublishedFileId 7 -RequestOperation $request -DelayOperation $delay);" +
            "Write-Output ($script:attempt.ToString()+'|'+$script:delays[0].ToString()+'|'+$notes[0].id+'|'+$notes[0].note)");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("2|7|22|Recovered note."));
        });
    }

    [Test]
    public void Workshop_retry_after_parser_honors_numeric_and_http_date_bounds()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Resolve-WorkshopRateLimitDelayMilliseconds" },
            "$now=[datetimeoffset]::Parse('2026-08-16T15:00:00Z');" +
            "$numeric=Resolve-WorkshopRateLimitDelayMilliseconds -RetryAfter '2' -Now $now;" +
            "$numericClamp=Resolve-WorkshopRateLimitDelayMilliseconds -RetryAfter '999' -Now $now;" +
            "$dateClamp=Resolve-WorkshopRateLimitDelayMilliseconds -RetryAfter 'Sun, 16 Aug 2026 15:02:00 GMT' -Now $now;" +
            "$fallback=Resolve-WorkshopRateLimitDelayMilliseconds -RetryAfter '' -Now $now;" +
            "Write-Output ($numeric.ToString()+'|'+$numericClamp.ToString()+'|'+$dateClamp.ToString()+'|'+$fallback.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("2000|30000|30000|30000"));
        });
    }

    [Test]
    public void Workshop_change_history_does_not_retry_other_failures_and_stops_after_third_429()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "ConvertFrom-WorkshopChangeHistoryHtml", "Get-WorkshopChangeNotes" },
            "$script:attempt=0;$script:delays=[System.Collections.Generic.List[int]]::new();" +
            "$delay={param($milliseconds)$script:delays.Add([int]$milliseconds)};" +
            "$nonRate={param($uri)$script:attempt++;return [pscustomobject]@{Succeeded=$false;StatusCode=500;RetryAfterMilliseconds=7}};" +
            "$nonRateError='';try{$null=Get-WorkshopChangeNotes -PublishedFileId 7 -RequestOperation $nonRate -DelayOperation $delay}catch{$nonRateError=$_.Exception.Message};" +
            "$nonRateAttempts=$script:attempt;$nonRateDelays=$script:delays.Count;" +
            "$script:attempt=0;$script:delays.Clear();" +
            "$rate={param($uri)$script:attempt++;return [pscustomobject]@{Succeeded=$false;StatusCode=429;RetryAfterMilliseconds=7}};" +
            "$rateError='';try{$null=Get-WorkshopChangeNotes -PublishedFileId 7 -RequestOperation $rate -DelayOperation $delay}catch{$rateError=$_.Exception.Message};" +
            "Write-Output ($nonRateAttempts.ToString()+'|'+$nonRateDelays.ToString()+'|'+$nonRateError+'|'+$script:attempt.ToString()+'|'+$script:delays.Count.ToString()+'|'+$rateError)");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(
                run.StandardOutput.Trim(),
                Is.EqualTo("1|0|The Steam change-history request failed with HTTP 500 after 1 attempt(s).|3|2|The Steam change-history request failed with HTTP 429 after 3 attempt(s)."));
        });
    }

    [Test]
    public void Retained_memories_showcase_matches_its_declared_crop_and_native_workflow()
    {
        using var fixture = Fixture.Create();
        var root = fixture.RepositoryRoot;
        var releaseRoot = Path.Combine(root, "mods", "ImmersiveChefs", "Release", "workshop");
        var serializer = new JavaScriptSerializer();
        var manifest = (Dictionary<string, object>)serializer.DeserializeObject(
            File.ReadAllText(Path.Combine(releaseRoot, "showcases.json")));
        var declared = ((object[])manifest["showcases"])
            .Cast<Dictionary<string, object>>()
            .Single(item => (string)item["id"] == "dining-memories");
        var crop = (Dictionary<string, object>)declared["crop"];
        var evidencePath = Path.Combine(releaseRoot, "assets", "showcases", "dining-memories", "dining-memories.capture.json");
        var evidence = (Dictionary<string, object>)serializer.DeserializeObject(File.ReadAllText(evidencePath));
        var plan = (Dictionary<string, object>)evidence["plan"];
        var firstFrame = (Dictionary<string, object>)((object[])evidence["frames"]).Single();
        var applied = (Dictionary<string, object>)firstFrame["crop"];
        var framePath = Path.Combine(Path.GetDirectoryName(evidencePath)!, ((string)firstFrame["path"]).Replace('/', Path.DirectorySeparatorChar));
        var screenshotPath = Path.Combine(Path.GetDirectoryName(evidencePath)!, "dining-memories.png");
        string Sha(string path)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }

        Assert.Multiple(() =>
        {
            foreach (var field in new[] { "width", "height", "offsetX", "offsetY" })
            {
                Assert.That(Convert.ToInt32(plan[field]), Is.EqualTo(Convert.ToInt32(crop[field])), field);
            }
            Assert.That(Convert.ToInt32(applied["width"]), Is.EqualTo(Convert.ToInt32(crop["width"])));
            Assert.That(Convert.ToInt32(applied["height"]), Is.EqualTo(Convert.ToInt32(crop["height"])));
            Assert.That(File.Exists(framePath), Is.True);
            Assert.That(Sha(framePath), Is.EqualTo(((string)firstFrame["sha256"]).ToUpperInvariant()));
            Assert.That(Sha(screenshotPath), Is.EqualTo(Sha(framePath)));
            Assert.That(File.ReadAllText(Path.Combine(root, "scripts", "Scenarios", "immersive-chefs-showcase-memories-setup.csx")), Does.Not.Contain("TryGainMemory"));
            Assert.That(File.ReadAllText(Path.Combine(root, "scripts", "Scenarios", "immersive-chefs-showcase-memories-arm.csx")), Does.Contain("consume.Chosen(true, null)"));
        });
    }

    [Test]
    public void Publisher_keeps_the_title_art_first_then_six_feature_cards_when_showcases_are_human_deferred()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-WorkshopShowcasePlanEvidence" },
            "$tokens=@('immersive-chefs','kitchenware','teamwork','dishwashing','meals','colony','compatibility');$p=$tokens|ForEach-Object{[pscustomobject]@{token=$_;showcaseId=$null;format='screenshot';sha256=('preview-'+$_);width=$(if($_ -ceq 'immersive-chefs'){1280}else{1164});height=$(if($_ -ceq 'immersive-chefs'){720}else{655})}};" +
            "$ok=Test-WorkshopShowcasePlanEvidence -PublicationStatus 'human-deferred' -Evidence @() -DesignEvidence @() -AdditionalPreviews @($p);" +
            "$leaked=Test-WorkshopShowcasePlanEvidence -PublicationStatus 'human-deferred' -Evidence @([pscustomobject]@{showcaseId='dishwasher-turnaround'}) -DesignEvidence @() -AdditionalPreviews @($p);" +
            "$missing=Test-WorkshopShowcasePlanEvidence -PublicationStatus 'human-deferred' -Evidence @() -DesignEvidence @() -AdditionalPreviews @($p|Select-Object -Skip 1);" +
            "$wrongSize=@($p|ForEach-Object{[pscustomobject]@{token=$_.token;showcaseId=$_.showcaseId;format=$_.format;sha256=$_.sha256;width=$_.width;height=$_.height}});$wrongSize[0].width=1164;" +
            "$badSize=Test-WorkshopShowcasePlanEvidence -PublicationStatus 'human-deferred' -Evidence @() -DesignEvidence @() -AdditionalPreviews $wrongSize;" +
            "Write-Output ($ok.ToString()+'|'+$leaked.ToString()+'|'+$missing.ToString()+'|'+$badSize.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False|False|False"));
        });
    }

    [Test]
    public void Final_remote_preview_slots_and_bytes_must_match_before_publication()
    {
        using var fixture = Fixture.Create();
        var downloadRoot = Path.Combine(fixture.Root, "preview-downloads");
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-RemoteWorkshopPreviewsMatchResolvedDescription", "Test-ResolvedWorkshopPreviewsReadyForPublication" },
            "$resolved=@([pscustomobject]@{remoteIndex=0;remoteUrl='https://images.steamusercontent.com/a.png';remoteType='k_EItemPreviewType_Image';remoteSha256='BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD'}," +
            "[pscustomobject]@{remoteIndex=1;remoteUrl='https://images.steamusercontent.com/b.png';remoteType='k_EItemPreviewType_Image';remoteSha256='BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD'});" +
            "$remote=@([pscustomobject]@{Index=0;Url='https://images.steamusercontent.com/a.png';Type='k_EItemPreviewType_Image'}," +
            "[pscustomobject]@{Index=1;Url='https://images.steamusercontent.com/b.png';Type='k_EItemPreviewType_Image'});" +
            "$download={param($uri,$path)[IO.File]::WriteAllText($path,'abc',[Text.UTF8Encoding]::new($false))};" +
            "$ok=Test-ResolvedWorkshopPreviewsReadyForPublication -RemotePreviews $remote -ResolvedPreviews $resolved -DownloadRoot " + Ps(downloadRoot) + " -DownloadOperation $download;" +
            "$sameHostDrift=@($resolved[0].PSObject.Copy(),$resolved[1].PSObject.Copy());$sameHostDrift[1].remoteUrl='https://images.steamusercontent.com/different.png';" +
            "$urlDrift=Test-ResolvedWorkshopPreviewsReadyForPublication -RemotePreviews $remote -ResolvedPreviews $sameHostDrift -DownloadRoot " + Ps(downloadRoot) + " -DownloadOperation $download;" +
            "$wrongBytes={param($uri,$path)[IO.File]::WriteAllText($path,'abd',[Text.UTF8Encoding]::new($false))};" +
            "$byteDrift=Test-ResolvedWorkshopPreviewsReadyForPublication -RemotePreviews $remote -ResolvedPreviews $resolved -DownloadRoot " + Ps(downloadRoot) + " -DownloadOperation $wrongBytes;" +
            "Write-Output ($ok.ToString()+'|'+$urlDrift.ToString()+'|'+$byteDrift.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False|False"));
        });
    }

    [Test]
    public void Publisher_process_lease_is_recovered_before_the_interactive_hold_exists()
    {
        using var fixture = Fixture.Create();
        var gatewayRoot = Path.Combine(fixture.Root, "gateway");
        var manifestDirectory = Path.Combine(gatewayRoot, "one", "SavedData", "DevGateway");
        Directory.CreateDirectory(manifestDirectory);
        using var retained = StartHiddenRetainedProcess()
            ?? throw new InvalidOperationException("Could not start the retained-process fixture.");
        try
        {
            var started = retained.StartTime.ToUniversalTime().ToString("O");
            var leasePath = Path.Combine(gatewayRoot, "publisher-process-lease.json");
            File.WriteAllText(leasePath,
                "{\"schema\":\"RimWorldDevGateway/ProcessLease/v1\",\"processId\":" + retained.Id + ",\"processStartUtc\":\"" + started + "\"}",
                new UTF8Encoding(false));

            var run = fixture.InvokeFunctions(
                "Invoke-ImmersiveChefsWorkshopRelease.ps1",
                new[] { "Read-Json", "Assert-RetainedProcessIdentity", "Get-ExactProcessStartUtcFromManifest", "Get-PublisherProcessLease" },
                "$lease=Get-PublisherProcessLease -GatewayRoot " + Ps(gatewayRoot) + " -ProcessLeasePath " + Ps(leasePath) +
                "; Write-Output ($lease.ProcessId.ToString() + '|' + [IO.Path]::GetFileName($lease.ManifestPath))");
            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.Zero, run.StandardError);
                Assert.That(run.StandardOutput.Trim(), Is.EqualTo(retained.Id + "|publisher-process-lease.json"));
            });
        }
        finally
        {
            if (!retained.HasExited)
            {
                retained.Kill();
                retained.WaitForExit();
            }
        }
    }

    [Test]
    public void Publisher_process_lease_rejects_even_a_subsecond_start_identity_mismatch()
    {
        using var fixture = Fixture.Create();
        using var retained = StartHiddenRetainedProcess()
            ?? throw new InvalidOperationException("Could not start the retained-process fixture.");
        try
        {
            var expected = retained.StartTime.ToUniversalTime().AddMilliseconds(1).ToString("O");
            var run = fixture.InvokeFunctions(
                "Invoke-ImmersiveChefsWorkshopRelease.ps1",
                new[] { "Assert-RetainedProcessIdentity" },
                "Assert-RetainedProcessIdentity -Process (Get-Process -Id " + retained.Id + ") -ExpectedStartUtc ([datetimeoffset]::ParseExact(" +
                Ps(expected) + ",'O',[Globalization.CultureInfo]::InvariantCulture))");
            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.EqualTo(1));
                Assert.That(run.StandardError, Does.Contain("PID was reused"));
            });
        }
        finally
        {
            if (!retained.HasExited)
            {
                retained.Kill();
                retained.WaitForExit();
            }
        }
    }

    [Test]
    public void Publisher_process_lease_is_null_while_the_launcher_has_not_created_any_artifact()
    {
        using var fixture = Fixture.Create();
        var gatewayRoot = Path.Combine(fixture.Root, "not-created-yet");
        var leasePath = Path.Combine(gatewayRoot, "publisher-process-lease.json");
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Read-Json", "Assert-RetainedProcessIdentity", "Get-ExactProcessStartUtcFromManifest", "Get-PublisherProcessLease" },
            "Set-StrictMode -Version Latest; $lease=Get-PublisherProcessLease -GatewayRoot " + Ps(gatewayRoot) + " -ProcessLeasePath " + Ps(leasePath) +
            "; if($null -ne $lease){throw 'unexpected lease'}; Write-Output 'waiting'");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("waiting"));
        });
    }

    [Test]
    public void Atomic_text_writer_replaces_an_existing_durable_publication_state()
    {
        using var fixture = Fixture.Create();
        var statePath = Path.Combine(fixture.Root, "publication-state.txt");
        var sentinelBackup = statePath + ".bak";
        File.WriteAllText(statePath, "submitted|plan|123", new UTF8Encoding(false));
        File.WriteAllText(sentinelBackup, "retained recovery evidence", new UTF8Encoding(false));
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Write-TextAtomically" },
            "Write-TextAtomically -Path " + Ps(statePath) + " -Value 'succeeded|plan|123'; Write-Output (Get-Content -LiteralPath " + Ps(statePath) + " -Raw)");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("succeeded|plan|123"));
            Assert.That(File.ReadAllText(sentinelBackup), Is.EqualTo("retained recovery evidence"));
            Assert.That(
                Directory.GetFiles(fixture.Root).Where(path =>
                    !string.Equals(path, statePath, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(path, sentinelBackup, StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileName(path).StartsWith("publication-state.txt.", StringComparison.OrdinalIgnoreCase)),
                Is.Empty);
        });
    }

    [Test]
    public void Newer_release_tool_revision_is_allowed_only_for_exact_post_submit_reconciliation()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-ReleaseSourceRevisionAllowed" },
            "$same=Test-ReleaseSourceRevisionAllowed 'candidate' 'candidate' $false 0 $false; " +
            "$recovery=Test-ReleaseSourceRevisionAllowed 'tool-fix' 'candidate' $true 3782589902 $true; " +
            "$differentPlan=Test-ReleaseSourceRevisionAllowed 'tool-fix' 'candidate' $false 3782589902 $true; " +
            "$idless=Test-ReleaseSourceRevisionAllowed 'tool-fix' 'candidate' $true 0 $true; " +
            "$divergent=Test-ReleaseSourceRevisionAllowed 'divergent' 'candidate' $true 3782589902 $false; " +
            "Write-Output ($same.ToString()+'|'+$recovery.ToString()+'|'+$differentPlan.ToString()+'|'+$idless.ToString()+'|'+$divergent.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|True|False|False|False"));
        });
    }

    [Test]
    public void Reconciliation_requires_the_exact_nonzero_item_identity_from_durable_state()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-ReconciliationStateIdentity" },
            "$exact=Test-ReconciliationStateIdentity 'submitted' 3782589902 3782589902; " +
            "$conflict=Test-ReconciliationStateIdentity 'submitted' 111 222; " +
            "$missing=Test-ReconciliationStateIdentity 'submitted' 0 3782589902; " +
            "$preSubmit=Test-ReconciliationStateIdentity 'create-admitted' 0 0; " +
            "Write-Output ($exact.ToString()+'|'+$conflict.ToString()+'|'+$missing.ToString()+'|'+$preSubmit.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False|False|True"));
        });
    }

    [Test]
    public void Subscribed_smoke_uses_a_short_path_and_rejects_an_unsafe_projected_gateway_session_path()
    {
        using var fixture = Fixture.Create();
        const string attemptId = "20260819T143503061Z";
        var safeRepositoryRoot = Path.Combine(Path.GetPathRoot(fixture.Root)!, "rimmods");
        var expected = Path.Combine(safeRepositoryRoot, "artifacts", "ReleaseSmoke", attemptId);
        var unsafeRepositoryRoot = Path.Combine(Path.GetPathRoot(fixture.Root)!, new string('x', 220));
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsSubscribedSmoke.ps1",
            new[] { "Assert-SubscribedSmokeArtifactsPath", "Get-SubscribedSmokeArtifactsRoot" },
            "$safe=Get-SubscribedSmokeArtifactsRoot " + Ps(safeRepositoryRoot) + " '" + attemptId + "'; " +
            "$unsafe=''; try {$null=Get-SubscribedSmokeArtifactsRoot " + Ps(unsafeRepositoryRoot) + " '" + attemptId + "'} catch {$unsafe=$_.Exception.Message}; " +
            "Write-Output ($safe+'|'+$unsafe)");
        var parts = run.StandardOutput.Trim().Split(new[] { '|' }, 2);
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(parts[0], Is.EqualTo(expected));
            Assert.That(parts[0], Does.Not.Contain("fumblesneeze.immersivechefs"));
            Assert.That(parts[0], Does.Not.Contain("publication"));
            Assert.That(parts[1], Does.Contain("legacy Windows path limit"));
        });
    }

    [Test]
    public void Subscribed_smoke_selects_the_session_copy_that_owns_the_screenshots()
    {
        using var fixture = Fixture.Create();
        var runnerEvidence = Path.Combine(fixture.Root, "smoke-001", "run", "end-to-end-tests.json");
        var persistedEvidence = Path.Combine(fixture.Root, "smoke-001", "run", "SavedData", "DevGateway", "Sessions", "one", "end-to-end-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(runnerEvidence)!);
        Directory.CreateDirectory(Path.GetDirectoryName(persistedEvidence)!);
        File.WriteAllText(runnerEvidence, "{}", new UTF8Encoding(false));
        File.WriteAllText(persistedEvidence, "{}", new UTF8Encoding(false));
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsSubscribedSmoke.ps1",
            new[] { "Get-SubscribedSmokeEvidenceFile" },
            "Write-Output ((Get-SubscribedSmokeEvidenceFile -RunRoot " + Ps(fixture.Root) + ").FullName)");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo(persistedEvidence));
        });
    }

    [Test]
    public void Subscribed_smoke_normalizes_host_envelope_and_persisted_session_evidence()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsSubscribedSmoke.ps1",
            new[] { "Get-SubscribedSmokeEvidencePayload" },
            "$hostPayload=Get-SubscribedSmokeEvidencePayload ([pscustomobject]@{result=[pscustomobject]@{Execution='host'}}); " +
            "$session=Get-SubscribedSmokeEvidencePayload ([pscustomobject]@{Execution='session'}); " +
            "Write-Output ($hostPayload.Execution+'|'+$session.Execution)");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("host|session"));
        });
    }

    private static string Ps(string value) => "'" + value.Replace("'", "''") + "'";

    private static Process StartHiddenRetainedProcess()
    {
        var start = new ProcessStartInfo(
            "powershell.exe",
            "-NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -Command \"Start-Sleep -Seconds 120\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        return Process.Start(start)
            ?? throw new InvalidOperationException("Could not start the hidden retained-process fixture.");
    }

    private sealed class Fixture : IDisposable
    {
        private Fixture(string root, string repositoryRoot)
        {
            Root = root;
            RepositoryRoot = repositoryRoot;
            PackageRoot = Path.Combine(root, "package");
            Directory.CreateDirectory(Path.Combine(PackageRoot, "About"));
            var identity = Path.Combine(PackageRoot, "About", "PublishedFileId.txt");
            File.WriteAllText(identity, "123456789", new UTF8Encoding(false));
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(identity))).Replace("-", string.Empty);
            ManifestPath = Path.Combine(root, "package-files.json");
            File.WriteAllText(ManifestPath,
                "{\"files\":[{\"path\":\"About\\\\PublishedFileId.txt\",\"bytes\":9,\"sha256\":\"" + hash + "\"}]}",
                new UTF8Encoding(false));
        }

        public string Root { get; }
        public string RepositoryRoot { get; }
        public string PackageRoot { get; }
        public string ManifestPath { get; }

        public static Fixture Create()
        {
            var root = Path.Combine(Path.GetTempPath(), nameof(ImmersiveChefsReleaseScriptBehaviorTests), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new Fixture(root, FindRepositoryRoot());
        }

        public string ResolveCommand(string name)
        {
            var invocation = Run("pwsh.exe", "-NoProfile -NonInteractive -Command \"(Get-Command '" + name + "').Source\"");
            if (invocation.ExitCode != 0) throw new InvalidOperationException(invocation.StandardError);
            return invocation.StandardOutput.Trim();
        }

        public Invocation InvokeFunctions(string scriptName, IReadOnlyList<string> functionNames, string operation)
        {
            var target = Path.Combine(RepositoryRoot, "scripts", scriptName);
            var invocation = Path.Combine(Root, "invoke-" + Guid.NewGuid().ToString("N") + ".ps1");
            var script =
                "$ErrorActionPreference='Stop'\n$tokens=$null;$errors=$null\n" +
                $"$ast=[System.Management.Automation.Language.Parser]::ParseFile({Ps(target)},[ref]$tokens,[ref]$errors)\n" +
                $"foreach($name in @({string.Join(",", functionNames.Select(Ps))})){{ $node=$ast.Find({{param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq $name}},$true); if($null -eq $node){{throw \"missing $name\"}}; Invoke-Expression $node.Extent.Text }}\n" +
                "try {\n" + operation + "\nexit 0\n} catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
            File.WriteAllText(invocation, script, new UTF8Encoding(false));
            return Run("pwsh.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{invocation}\"");
        }

        public Invocation ParseWithWindowsPowerShell(string scriptName)
        {
            var target = Path.Combine(RepositoryRoot, "scripts", scriptName);
            var invocation = Path.Combine(Root, "parse-" + Guid.NewGuid().ToString("N") + ".ps1");
            var script =
                "$errors=$null;$tokens=$null\n" +
                $"$null=[System.Management.Automation.Language.Parser]::ParseFile({Ps(target)},[ref]$tokens,[ref]$errors)\n" +
                "if($errors.Count -ne 0){$errors | ForEach-Object {[Console]::Error.WriteLine($_.Message)};exit 1}\nexit 0\n";
            File.WriteAllText(invocation, script, new UTF8Encoding(false));
            return Run("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{invocation}\"");
        }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }

        private static Invocation Run(string file, string arguments)
        {
            var start = new ProcessStartInfo(file, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start PowerShell.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30_000)) { process.Kill(); throw new TimeoutException("PowerShell fixture timed out."); }
            Task.WaitAll(stdout, stderr);
            return new Invocation(process.ExitCode, stdout.Result, stderr.Result);
        }

        public static Invocation RunProcess(string file, string arguments) => Run(file, arguments);

        private static string FindRepositoryRoot()
        {
            for (var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); current is not null; current = current.Parent)
                if (File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln"))) return current.FullName;
            throw new DirectoryNotFoundException("Could not find repository root.");
        }
    }

    private sealed class Invocation
    {
        public Invocation(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }
        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
    }
}
