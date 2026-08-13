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
    public void Release_scripts_parse_under_the_installed_Windows_PowerShell_host()
    {
        using var fixture = Fixture.Create();
        foreach (var script in new[] { "Build-ImmersiveChefsRelease.ps1", "Invoke-ImmersiveChefsWorkshopRelease.ps1", "Invoke-RimWorldShowcaseCapture.ps1" })
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
        var reviewedProductAssembly = Path.Combine(assetRoot, "ImmersiveChefs.dll");
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        File.WriteAllBytes(screenshotPath, png);
        File.WriteAllBytes(framePath, png);
        File.WriteAllBytes(reviewedProductAssembly, new byte[] { 1, 2, 3, 4 });
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
            "{\"index\":1,\"packageId\":\"fumblesneeze.immersivechefs\",\"name\":\"Immersive Chefs\",\"rootDir\":\"C:/Product\",\"files\":[{\"path\":\"1.6/Assemblies/ImmersiveChefs.dll\",\"bytes\":4,\"sha256\":\"" + Sha(reviewedProductAssembly) + "\"}]}," +
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
                        "$e=Get-WorkshopShowcaseEvidence -Showcase $s -ReleaseRoot " + Ps(releaseRoot) + " -ReviewedProductAssemblyPath " + Ps(reviewedProductAssembly) + ";" +
                        "Write-Output ($e.showcaseId+'|'+$e.outputs.Count+'|'+$e.provenance.sha256.Length)";
        var accepted = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Get-CanonicalJson", "Invoke-ShowcaseMediaTool", "Get-RasterImageInfo", "Get-WorkshopShowcaseEvidence" }, operation);
        Assert.Multiple(() =>
        {
            Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
            Assert.That(accepted.StandardOutput.Trim(), Is.EqualTo("service|2|64"));
        });

        File.AppendAllText(gifPath, "tampered", new UTF8Encoding(false));
        var rejected = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Get-CanonicalJson", "Invoke-ShowcaseMediaTool", "Get-RasterImageInfo", "Get-WorkshopShowcaseEvidence" }, operation);
        Assert.Multiple(() =>
        {
            Assert.That(rejected.ExitCode, Is.EqualTo(1));
            Assert.That(rejected.StandardError, Does.Contain("does not match its capture evidence"));
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
        File.WriteAllText(
            inventoryPath,
            "{\"schema\":\"ImmersiveChefs/WorkshopRemotePreviewInventory/v1\",\"publishedFileId\":\"3782589902\",\"publicationPlanSha256\":\"" +
            previewPlan + "\",\"previews\":[" + string.Join(",", previewEntries) +
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
            new[] { "Test-WorkshopPreviewProvenance" },
            "$current=@([pscustomobject]@{token='hero';path='hero.png';sha256='AAAA'},[pscustomobject]@{token='meals';path='meals.png';sha256='BBBB'});" +
            "$exact=@([pscustomobject]@{token='hero';remoteIndex=0;localPath='hero.png';localSha256='AAAA';remoteSha256='AAAA';remoteType='k_EItemPreviewType_Image'}," +
            "[pscustomobject]@{token='meals';remoteIndex=1;localPath='meals.png';localSha256='BBBB';remoteSha256='BBBB';remoteType='k_EItemPreviewType_Image'});" +
            "$drift=@([pscustomobject]@{token='hero';remoteIndex=0;localPath='hero.png';localSha256='AAAA';remoteSha256='AAAA';remoteType='k_EItemPreviewType_Image'}," +
            "[pscustomobject]@{token='meals';remoteIndex=1;localPath='meals.png';localSha256='CCCC';remoteSha256='CCCC';remoteType='k_EItemPreviewType_Image'});" +
            "$ok=Test-WorkshopPreviewProvenance $current $exact; $bad=Test-WorkshopPreviewProvenance $current $drift;" +
            "Write-Output ($ok.ToString()+'|'+$bad.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False"));
        });
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
    public void Publisher_requires_five_complete_showcase_evidence_records_cross_bound_to_the_carousel()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-WorkshopShowcasePlanEvidence" },
            "$ids=@('gastronomy-service','dishwasher-turnaround','nutrient-paste-prison-line','professional-prep-line','dining-memories');" +
            "$e=@();$p=@();$slot=5;foreach($id in $ids){$fmt=if($id -ceq 'dining-memories'){'screenshot'}else{'gif'};" +
            "$a='A'.PadRight(64,'A');$b='B'.PadRight(64,'B');$c='C'.PadRight(64,'C');" +
            "$outs=@([pscustomobject]@{format='screenshot';sha256=$a});if($fmt -ceq 'gif'){$outs+=([pscustomobject]@{format='gif';sha256=$b})};" +
            "$e+=([pscustomobject]@{showcaseId=$id;outputs=$outs;provenance=[pscustomobject]@{sha256=$c}});" +
            "$carouselHash=if($fmt -ceq 'gif'){$b}else{$a};$p+=([pscustomobject]@{showcaseId=$id;format=$fmt;sha256=$carouselHash});$slot++};" +
            "$p=@([pscustomobject]@{showcaseId=$null;format='screenshot';sha256='card1'},[pscustomobject]@{showcaseId=$null;format='screenshot';sha256='card2'},[pscustomobject]@{showcaseId=$null;format='screenshot';sha256='card3'},[pscustomobject]@{showcaseId=$null;format='screenshot';sha256='card4'},[pscustomobject]@{showcaseId=$null;format='screenshot';sha256='card5'})+$p;" +
            "$ok=Test-WorkshopShowcasePlanEvidence -Evidence @($e) -AdditionalPreviews @($p);$missing=Test-WorkshopShowcasePlanEvidence -Evidence @($e|Select-Object -First 4) -AdditionalPreviews @($p);" +
            "Write-Output ($ok.ToString()+'|'+$missing.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False"));
        });
    }

    [Test]
    public void Final_remote_preview_slots_must_match_the_urls_embedded_in_the_description()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-RemoteWorkshopPreviewsMatchResolvedDescription" },
            "$resolved=@([pscustomobject]@{remoteIndex=0;remoteUrl='https://images.steamusercontent.com/a.png';remoteType='k_EItemPreviewType_Image'}," +
            "[pscustomobject]@{remoteIndex=1;remoteUrl='https://images.steamusercontent.com/b.png';remoteType='k_EItemPreviewType_Image'});" +
            "$remote=@([pscustomobject]@{Index=0;Url='https://images.steamusercontent.com/a.png';Type='k_EItemPreviewType_Image'}," +
            "[pscustomobject]@{Index=1;Url='https://images.steamusercontent.com/b.png';Type='k_EItemPreviewType_Image'});" +
            "$reordered=@($remote[1],$remote[0]);" +
            "$ok=Test-RemoteWorkshopPreviewsMatchResolvedDescription $remote $resolved;" +
            "$bad=Test-RemoteWorkshopPreviewsMatchResolvedDescription $reordered $resolved;" +
            "Write-Output ($ok.ToString()+'|'+$bad.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False"));
        });
    }

    [Test]
    public void Publisher_process_lease_is_recovered_before_the_interactive_hold_exists()
    {
        using var fixture = Fixture.Create();
        var gatewayRoot = Path.Combine(fixture.Root, "gateway");
        var manifestDirectory = Path.Combine(gatewayRoot, "one", "SavedData", "DevGateway");
        Directory.CreateDirectory(manifestDirectory);
        using var retained = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = false })
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
        using var retained = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = false })
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
