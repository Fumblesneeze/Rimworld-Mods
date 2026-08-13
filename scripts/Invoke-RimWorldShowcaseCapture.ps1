<#
.SYNOPSIS
Captures a bounded, camera-centered RimWorld gameplay still and GIF candidate.

.DESCRIPTION
Uses the exact live Dev Gateway session to capture a fixed camera rectangle without selecting
Things. Raw PNG frames, the chosen still, a <=5 second GIF candidate, and a token-free provenance
manifest are retained together. The caller owns scene setup and native gameplay timing.

.EXAMPLE
.\scripts\Invoke-RimWorldShowcaseCapture.ps1 -Manifest C:\run\current.json -ProcessId 1234 `
  -OutputDirectory .\artifacts\Showcases\gastronomy -Width 1280 -Height 720 `
  -FrameCount 20 -FramesPerSecond 5 -BaseName gastronomy -Output json
#>
[CmdletBinding()]
param(
    [string]$Manifest,
    [int]$ProcessId,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateRange(1, 16384)][int]$Width = 1280,
    [ValidateRange(1, 16384)][int]$Height = 720,
    [int]$OffsetX = 0,
    [int]$OffsetY = 0,
    [ValidateRange(1, 60)][int]$FrameCount = 20,
    [ValidateRange(1, 12)][int]$FramesPerSecond = 5,
    [ValidateRange(0, 10000)][int]$StartDelayMilliseconds = 0,
    [ValidateRange(0, 60)][int]$StillFrameIndex = 0,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$')][string]$BaseName = 'showcase',
    [string]$GatewayClient,
    [string]$FfmpegPath = 'ffmpeg',
    [string]$FfprobePath = 'ffprobe',
    [ValidateRange(2, 256)][int]$GifColors = 96,
    [ValidateRange(64, 1280)][int]$GifWidth = 960,
    [ValidateRange(1024, 1048575)][int]$MaximumGifBytes = 1048575,
    [ValidateRange(5, 300)][int]$EncoderTimeoutSeconds = 60,
    [string]$PngOptimizerPath = 'magick',
    [switch]$StillOnly,
    [ValidatePattern('^[a-z0-9][a-z0-9._-]{0,63}$')][string]$ShowcaseId,
    [string[]]$ExpectedPackageIds = @(),
    [string[]]$ObservedBeats = @(),
    [string]$ReviewObservation,
    [switch]$DryRun,
    [ValidateSet('table', 'json')][string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Exit-InvalidInput([string]$Message) {
    [Console]::Error.WriteLine($Message)
    exit 2
}

function Get-RelativePath([string]$Root, [string]$Path) {
    $rootUri = [Uri]::new($Root.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar)
    return [Uri]::UnescapeDataString($rootUri.MakeRelativeUri([Uri]::new($Path)).ToString())
}

function ConvertTo-QuotedProcessArgument([string]$Value) {
    if ($null -eq $Value -or $Value.Length -eq 0) { return '""' }
    if ($Value -notmatch '[\s"]') { return $Value }

    $builder = [Text.StringBuilder]::new()
    $null = $builder.Append('"')
    $slashes = 0
    foreach ($character in $Value.ToCharArray()) {
        if ($character -eq '\') {
            $slashes++
            continue
        }
        if ($character -eq '"') {
            $null = $builder.Append(('\' * (($slashes * 2) + 1)))
            $null = $builder.Append('"')
        } else {
            if ($slashes -gt 0) { $null = $builder.Append(('\' * $slashes)) }
            $null = $builder.Append($character)
        }
        $slashes = 0
    }
    if ($slashes -gt 0) { $null = $builder.Append(('\' * ($slashes * 2))) }
    $null = $builder.Append('"')
    return $builder.ToString()
}

function Invoke-BoundedChildProcess {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][int]$TimeoutMilliseconds,
        [Parameter(Mandatory)][string]$Label,
        [string]$WorkingDirectory
    )

    if ($TimeoutMilliseconds -le 0) { throw "$Label has no remaining execution budget." }
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.Arguments = (($Arguments | ForEach-Object { ConvertTo-QuotedProcessArgument ([string]$_) }) -join ' ')
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        $startInfo.WorkingDirectory = [IO.Path]::GetFullPath($WorkingDirectory)
    }
    $child = [Diagnostics.Process]::new()
    $child.StartInfo = $startInfo
    try {
        if (-not $child.Start()) { throw "Could not start $Label." }
        $standardOutput = $child.StandardOutput.ReadToEndAsync()
        $standardError = $child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit($TimeoutMilliseconds)) {
            $ownedProcessId = [int]$child.Id
            $ownedProcessStartUtc = $child.StartTime.ToUniversalTime().ToString('O', [Globalization.CultureInfo]::InvariantCulture)
            $killFailure = $null
            try { $child.Kill() } catch { $killFailure = $_.Exception.Message }
            if (-not $child.WaitForExit(10000)) {
                $killDetail = if ([string]::IsNullOrWhiteSpace([string]$killFailure)) { 'Kill returned without a terminal exit.' } else { $killFailure }
                throw "$Label exceeded its $TimeoutMilliseconds-millisecond execution budget, and owned PID $ownedProcessId ($ownedProcessStartUtc) could not be terminated: $killDetail"
            }
            throw "$Label exceeded its $TimeoutMilliseconds-millisecond execution budget and terminated owned PID $ownedProcessId ($ownedProcessStartUtc)."
        }
        $child.WaitForExit()
        return [pscustomobject][ordered]@{
            ProcessId = [int]$child.Id
            ExitCode = [int]$child.ExitCode
            StandardOutput = $standardOutput.GetAwaiter().GetResult()
            StandardError = $standardError.GetAwaiter().GetResult()
        }
    }
    finally {
        $child.Dispose()
    }
}

function Get-LiveGatewayState([object]$Session, [string]$Suffix) {
    $headers = @{
        Authorization = 'Bearer ' + [string]$Session.token
        'X-Request-Id' = 'showcase-capture-' + $Suffix + '-' + [guid]::NewGuid().ToString('N')
    }
    $gameState = Invoke-RestMethod -Method Get -Uri ([string]$Session.baseUrl + '/game-state') -Headers $headers -TimeoutSec 15
    $headers['X-Request-Id'] = 'showcase-capture-ui-' + $Suffix + '-' + [guid]::NewGuid().ToString('N')
    $uiState = Invoke-RestMethod -Method Get -Uri ([string]$Session.baseUrl + '/ui-state') -Headers $headers -TimeoutSec 15
    if (-not [bool]$gameState.ok -or -not [bool]$uiState.ok) { throw "Gateway $Suffix state capture failed." }
    return [pscustomobject][ordered]@{
        camera = $gameState.result.Camera
        selectedThings = @($gameState.result.Selection)
        uiSelection = @($uiState.result.selection)
    }
}

function Get-LiveGatewayPackages([object]$Session) {
    $headers = @{
        Authorization = 'Bearer ' + [string]$Session.token
        'X-Request-Id' = 'showcase-capture-status-' + [guid]::NewGuid().ToString('N')
    }
    $source = @'
string.Join("\n", Verse.LoadedModManager.RunningModsListForReading
    .Select((mod, index) => index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\t" + mod.PackageId + "\t" + mod.Name.Replace("\t", " ") + "\t" + mod.RootDir));
'@
    $headers['Content-Type'] = 'text/plain; charset=utf-8'
    $response = Invoke-RestMethod -Method Post -Uri ([string]$Session.baseUrl + '/executions/csharp') -Headers $headers -Body ([Text.Encoding]::UTF8.GetBytes($source)) -TimeoutSec 30
    if (-not [bool]$response.ok -or -not [bool]$response.result.Succeeded) { throw 'Gateway active-package capture failed.' }
    $parsed = @(([string]$response.result.Value -split "`n") | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object {
        $parts = $_.TrimEnd("`r").Split("`t")
        if ($parts.Count -ne 4) { throw 'Gateway returned a malformed active-package identity.' }
        [pscustomobject][ordered]@{ Index = [int]$parts[0]; PackageId = $parts[1]; Name = $parts[2]; RootDir = $parts[3] }
    })
    return @($parsed | Sort-Object Index)
}

function Get-LivePackageIdentities([object[]]$Packages) {
    return @($Packages | ForEach-Object {
        $root = [IO.Path]::GetFullPath([string]$_.RootDir)
        $identityFiles = [System.Collections.Generic.List[IO.FileInfo]]::new()
        if ([string]$_.PackageId -ceq 'fumblesneeze.immersivechefs') {
            $loadableProductFiles = @(Get-ChildItem -LiteralPath $root -File -Recurse -ErrorAction Stop | Where-Object {
                $relative = (Get-RelativePath -Root $root -Path $_.FullName).Replace('\', '/')
                $relative -ceq 'About/About.xml' -or
                    $relative -ceq 'LoadFolders.xml' -or
                    ($relative.StartsWith('1.6/', [StringComparison]::Ordinal) -and
                        -not $relative.EndsWith('.pdb', [StringComparison]::OrdinalIgnoreCase) -and
                        $relative -cne '1.6/Patches/ImmersiveChefsIntegrationProbePatch.xml')
            } | Sort-Object { (Get-RelativePath -Root $root -Path $_.FullName).Replace('\', '/') })
            if ($loadableProductFiles.Count -lt 1 -or $loadableProductFiles.Count -gt 8192) {
                throw "Loaded Immersive Chefs package has an invalid loadable-file inventory."
            }
            foreach ($file in $loadableProductFiles) { $identityFiles.Add($file) }
        }
        else {
            foreach ($relative in @('About\About.xml','About\Manifest.xml','LoadFolders.xml')) {
                $candidate = Join-Path $root $relative
                if (Test-Path -LiteralPath $candidate -PathType Leaf) { $identityFiles.Add([IO.FileInfo]::new($candidate)) }
            }
            $assemblies = @(Get-ChildItem -LiteralPath $root -Filter '*.dll' -File -Recurse -ErrorAction Stop | Sort-Object FullName)
            if ($assemblies.Count -gt 512) { throw "Loaded package '$([string]$_.PackageId)' exceeds the 512-assembly identity ceiling." }
            foreach ($assembly in $assemblies) { $identityFiles.Add($assembly) }
        }
        if ($identityFiles.Count -lt 1) { throw "Loaded package '$([string]$_.PackageId)' exposes no identity file." }
        [pscustomobject][ordered]@{
            index = [int]$_.Index
            packageId = [string]$_.PackageId
            name = [string]$_.Name
            rootDir = $root
            files = @($identityFiles | ForEach-Object {
                [pscustomobject][ordered]@{ path = Get-RelativePath -Root $root -Path $_.FullName; bytes = [long]$_.Length; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
            })
        }
    })
}

function Get-CanonicalJson([object]$Value) {
    return $Value | ConvertTo-Json -Depth 12 -Compress
}

$durationSeconds = $FrameCount / [double]$FramesPerSecond
if ($durationSeconds -gt 5.0) {
    Exit-InvalidInput 'The requested frame count and rate exceed the five-second showcase limit.'
}
if ($StillFrameIndex -eq 0) {
    $StillFrameIndex = [Math]::Max(1, [int][Math]::Ceiling($FrameCount / 2.0))
}
if ($StillFrameIndex -gt $FrameCount) {
    Exit-InvalidInput '-StillFrameIndex cannot exceed -FrameCount.'
}
if (-not $DryRun) {
    if ([string]::IsNullOrWhiteSpace($ShowcaseId)) { Exit-InvalidInput '-ShowcaseId is required for a retained capture.' }
    if ($ExpectedPackageIds.Count -lt 2 -or @($ExpectedPackageIds | Where-Object { $_ -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$' }).Count -ne 0) {
        Exit-InvalidInput '-ExpectedPackageIds must contain the exact ordered, valid loaded package IDs.'
    }
    if ($ObservedBeats.Count -lt 1 -or @($ObservedBeats | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -ne 0) {
        Exit-InvalidInput '-ObservedBeats must name the exact native actions and visible beats present in this capture.'
    }
    if ([string]::IsNullOrWhiteSpace($ReviewObservation)) {
        Exit-InvalidInput '-ReviewObservation must record the acting agent''s live visual review.'
    }
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($GatewayClient)) {
    $GatewayClient = Join-Path $repositoryRoot 'artifacts\HostTools\Release\net480\RimWorldDevGateway.Client.exe'
}
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$intervalMilliseconds = [int][Math]::Round(1000.0 / $FramesPerSecond)
$plan = [pscustomobject][ordered]@{
    schema = 'RimWorldDevGateway/ShowcaseCapturePlan/v1'
    processId = $ProcessId
    outputDirectory = $outputRoot
    frameCount = $FrameCount
    framesPerSecond = $FramesPerSecond
    durationSeconds = $durationSeconds
    width = $Width
    height = $Height
    offsetX = $OffsetX
    offsetY = $OffsetY
    stillFrameIndex = $StillFrameIndex
    gifWidth = $GifWidth
    gifColors = $GifColors
    maximumGifBytes = $MaximumGifBytes
    stillOnly = [bool]$StillOnly
    mutatesGameState = $false
    selectsThings = $false
}
if ($DryRun) {
    if ($Output -eq 'json') { $plan | ConvertTo-Json -Compress } else { $plan | Format-List }
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Manifest) -or -not (Test-Path -LiteralPath $Manifest -PathType Leaf)) {
    Exit-InvalidInput '-Manifest must identify an existing exact Gateway session manifest.'
}
if ($ProcessId -le 0) { Exit-InvalidInput '-ProcessId must identify the exact live RimWorld process.' }
if (-not (Test-Path -LiteralPath $GatewayClient -PathType Leaf)) {
    Exit-InvalidInput "The Gateway client is missing: $GatewayClient"
}
if (Test-Path -LiteralPath $outputRoot) {
    if ((Get-ChildItem -LiteralPath $outputRoot -Force | Measure-Object).Count -ne 0) {
        Exit-InvalidInput "The showcase output directory must be absent or empty: $outputRoot"
    }
} else {
    $null = New-Item -ItemType Directory -Path $outputRoot
}
$framesRoot = Join-Path $outputRoot 'frames'
$null = New-Item -ItemType Directory -Path $framesRoot

$sessionRaw = Get-Content -LiteralPath $Manifest -Raw -Encoding UTF8
$session = $sessionRaw | ConvertFrom-Json
$processStartMatch = [regex]::Match($sessionRaw, '"processStartUtc"\s*:\s*"(?<value>[^"]+)"')
if ([int]$session.processId -ne $ProcessId -or
    [string]$session.runId -notmatch '^[A-Fa-f0-9]{32}$' -or
    -not $processStartMatch.Success) {
    Exit-InvalidInput 'The Gateway manifest does not match the exact requested process identity.'
}
$processStartUtc = $processStartMatch.Groups['value'].Value
$liveProcess = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
if ($null -eq $liveProcess -or
    $liveProcess.StartTime.ToUniversalTime().Ticks -ne ([datetimeoffset]::Parse($processStartUtc)).UtcTicks) {
    Exit-InvalidInput 'The retained RimWorld PID/start identity is no longer live.'
}
$stateBefore = Get-LiveGatewayState -Session $session -Suffix 'before'
$activePackages = @(Get-LiveGatewayPackages -Session $session)
$loadedPackageIds = @($activePackages | ForEach-Object { [string]$_.PackageId })
$capturePackageIds = @($ExpectedPackageIds) + 'fumblesneeze.rimworlddevgateway'
if ($loadedPackageIds.Count -ne $capturePackageIds.Count -or
    (Get-CanonicalJson $loadedPackageIds) -cne (Get-CanonicalJson $capturePackageIds)) {
    throw "The live ordered package list does not match the declared showcase group."
}
$packageIdentities = @(Get-LivePackageIdentities -Packages $activePackages)

if ($StartDelayMilliseconds -gt 0) { Start-Sleep -Milliseconds $StartDelayMilliseconds }
$captureClock = [Diagnostics.Stopwatch]::StartNew()
$captureDeadlineMilliseconds = 5000
$frameEvidence = [System.Collections.Generic.List[object]]::new()
for ($index = 1; $index -le $FrameCount; $index++) {
    $targetElapsed = ($index - 1) * $intervalMilliseconds
    $remaining = $targetElapsed - $captureClock.ElapsedMilliseconds
    if ($remaining -gt 0) { Start-Sleep -Milliseconds ([int]$remaining) }

    $frameName = 'frame-{0:D4}.png' -f $index
    $framePath = Join-Path $framesRoot $frameName
    $arguments = @(
        'screenshot', '--file', $framePath,
        '--width', [string]$Width,
        '--height', [string]$Height,
        '--offset-x', [string]$OffsetX,
        '--offset-y', [string]$OffsetY,
        '--manifest', [IO.Path]::GetFullPath($Manifest),
        '--pid', [string]$ProcessId,
        '-o', 'json')
    $remainingBudget = $captureDeadlineMilliseconds - [int]$captureClock.ElapsedMilliseconds
    $clientRun = Invoke-BoundedChildProcess -FilePath $GatewayClient -Arguments $arguments -TimeoutMilliseconds $remainingBudget -Label "Gateway frame $index capture"
    if ($clientRun.ExitCode -ne 0) {
        throw "Gateway frame capture failed at frame $index ($($clientRun.ExitCode)): $($clientRun.StandardError)"
    }
    if (-not (Test-Path -LiteralPath $framePath -PathType Leaf)) {
        throw "Gateway frame capture did not create $frameName."
    }
    $clientResultText = ([string]$clientRun.StandardOutput).Trim()
    try { $clientResult = $clientResultText | ConvertFrom-Json }
    catch { throw "Gateway frame capture returned invalid JSON at frame $index`: $clientResultText" }
    if ($null -eq $clientResult.crop -or
        [int]$clientResult.crop.width -le 0 -or
        [int]$clientResult.crop.height -le 0) {
        throw "Gateway frame capture omitted the exact applied crop at frame $index."
    }
    $frameEvidence.Add([pscustomobject][ordered]@{
        index = $index
        elapsedMilliseconds = $captureClock.ElapsedMilliseconds
        path = Get-RelativePath -Root $outputRoot -Path $framePath
        bytes = (Get-Item -LiteralPath $framePath).Length
        sha256 = (Get-FileHash -LiteralPath $framePath -Algorithm SHA256).Hash
        crop = $clientResult.crop
    })
}
$captureClock.Stop()
if ($captureClock.ElapsedMilliseconds -gt $captureDeadlineMilliseconds) {
    throw "The raw showcase sequence exceeded the five-second capture deadline ($($captureClock.ElapsedMilliseconds) ms)."
}
$stateAfter = Get-LiveGatewayState -Session $session -Suffix 'after'
if ((Get-CanonicalJson $stateBefore.camera) -cne (Get-CanonicalJson $stateAfter.camera) -or
    (Get-CanonicalJson $stateBefore.selectedThings) -cne (Get-CanonicalJson $stateAfter.selectedThings) -or
    (Get-CanonicalJson $stateBefore.uiSelection) -cne (Get-CanonicalJson $stateAfter.uiSelection)) {
    throw 'Showcase capture changed the live camera or selection state.'
}

$stillPath = Join-Path $outputRoot ($BaseName + '.png')
Copy-Item -LiteralPath (Join-Path $framesRoot ('frame-{0:D4}.png' -f $StillFrameIndex)) -Destination $stillPath
$optimizerCommand = Get-Command $PngOptimizerPath -ErrorAction SilentlyContinue
if ($null -eq $optimizerCommand) { Exit-InvalidInput "PNG optimizer is unavailable: $PngOptimizerPath" }
$optimizerVersionRun = Invoke-BoundedChildProcess -FilePath $optimizerCommand.Source -Arguments @('-version') -TimeoutMilliseconds 10000 -Label 'PNG optimizer version query'
if ($optimizerVersionRun.ExitCode -ne 0) { Exit-InvalidInput "PNG optimizer version query failed: $($optimizerVersionRun.StandardError)" }
$optimizerArguments = @('-strip','-colors','256','-define','png:compression-level=9','-define','png:compression-filter=5')
foreach ($frame in $frameEvidence) {
    $framePath = Join-Path $outputRoot ([string]$frame.path).Replace('/', '\')
    $optimizeFrame = Invoke-BoundedChildProcess -FilePath $optimizerCommand.Source -Arguments (@($framePath) + $optimizerArguments + @($framePath)) -TimeoutMilliseconds 30000 -Label "PNG frame $([int]$frame.index) optimization"
    if ($optimizeFrame.ExitCode -ne 0) { throw "PNG optimizer rejected retained frame $([int]$frame.index): $($optimizeFrame.StandardError)" }
    $frame.bytes = (Get-Item -LiteralPath $framePath).Length
    $frame.sha256 = (Get-FileHash -LiteralPath $framePath -Algorithm SHA256).Hash
}
Copy-Item -LiteralPath (Join-Path $framesRoot ('frame-{0:D4}.png' -f $StillFrameIndex)) -Destination $stillPath -Force
$gifPath = $null
$gifBytes = $null
$actualGifDurationSeconds = $null
$gifEvidence = $null
if (-not $StillOnly) {
$ffmpegCommand = Get-Command $FfmpegPath -ErrorAction SilentlyContinue
if ($null -eq $ffmpegCommand) { Exit-InvalidInput "ffmpeg is unavailable: $FfmpegPath" }
$ffprobeCommand = Get-Command $FfprobePath -ErrorAction SilentlyContinue
if ($null -eq $ffprobeCommand) { Exit-InvalidInput "ffprobe is unavailable: $FfprobePath" }
$versionRun = Invoke-BoundedChildProcess -FilePath $ffmpegCommand.Source -Arguments @('-version') -TimeoutMilliseconds 10000 -Label 'ffmpeg version query'
if ($versionRun.ExitCode -ne 0) { Exit-InvalidInput "ffmpeg version query failed: $($versionRun.StandardError)" }
$ffmpegVersion = ([string]$versionRun.StandardOutput -split "`r?`n")[0]
$ffmpegHash = (Get-FileHash -LiteralPath $ffmpegCommand.Source -Algorithm SHA256).Hash
$probeVersionRun = Invoke-BoundedChildProcess -FilePath $ffprobeCommand.Source -Arguments @('-version') -TimeoutMilliseconds 10000 -Label 'ffprobe version query'
if ($probeVersionRun.ExitCode -ne 0) { Exit-InvalidInput "ffprobe version query failed: $($probeVersionRun.StandardError)" }
$ffprobeVersion = ([string]$probeVersionRun.StandardOutput -split "`r?`n")[0]
$ffprobeHash = (Get-FileHash -LiteralPath $ffprobeCommand.Source -Algorithm SHA256).Hash
$gifPath = Join-Path $outputRoot ($BaseName + '.gif')
$inputPattern = 'frames/frame-%04d.png'
$filter = "fps=$FramesPerSecond,scale=$GifWidth`:-1:flags=lanczos,split[s0][s1];[s0]palettegen=max_colors=$GifColors`:stats_mode=diff[p];[s1][p]paletteuse=dither=bayer`:bayer_scale=5`:diff_mode=rectangle"
$ffmpegArguments = @(
    '-hide_banner', '-loglevel', 'error', '-y',
    '-framerate', [string]$FramesPerSecond,
    '-i', $inputPattern,
    '-filter_complex', $filter,
    '-loop', '0',
    ($BaseName + '.gif'))
$ffmpegRun = Invoke-BoundedChildProcess -FilePath $ffmpegCommand.Source -Arguments $ffmpegArguments -TimeoutMilliseconds ($EncoderTimeoutSeconds * 1000) -Label 'ffmpeg showcase encoding' -WorkingDirectory $outputRoot
if ($ffmpegRun.ExitCode -ne 0) {
    throw "ffmpeg failed ($($ffmpegRun.ExitCode)): $($ffmpegRun.StandardError)"
}
$gifBytes = (Get-Item -LiteralPath $gifPath).Length
if ($gifBytes -gt $MaximumGifBytes) {
    throw "The GIF candidate is $gifBytes bytes and exceeds the reviewed $MaximumGifBytes-byte limit. Reduce dimensions, colors, or frame rate."
}
$ffprobeArguments = @('-v', 'error', '-show_entries', 'format=duration', '-of', 'default=noprint_wrappers=1:nokey=1', ($BaseName + '.gif'))
$ffprobeRun = Invoke-BoundedChildProcess -FilePath $ffprobeCommand.Source -Arguments $ffprobeArguments -TimeoutMilliseconds 10000 -Label 'ffprobe duration query' -WorkingDirectory $outputRoot
if ($ffprobeRun.ExitCode -ne 0) { throw "ffprobe failed ($($ffprobeRun.ExitCode)): $($ffprobeRun.StandardError)" }
[double]$actualGifDurationSeconds = 0
if (-not [double]::TryParse(
    ([string]$ffprobeRun.StandardOutput).Trim(),
    [Globalization.NumberStyles]::Float,
    [Globalization.CultureInfo]::InvariantCulture,
    [ref]$actualGifDurationSeconds) -or
    $actualGifDurationSeconds -le 0 -or $actualGifDurationSeconds -gt 5.0) {
    throw "The encoded GIF has an invalid or over-limit duration: $($ffprobeRun.StandardOutput)"
}
$gifEvidence = [pscustomobject][ordered]@{
    path = Get-RelativePath -Root $outputRoot -Path $gifPath
    bytes = $gifBytes
    sha256 = (Get-FileHash -LiteralPath $gifPath -Algorithm SHA256).Hash
    nominalDurationSeconds = $durationSeconds
    actualDurationSeconds = $actualGifDurationSeconds
    encoderName = [IO.Path]::GetFileName($ffmpegCommand.Source)
    encoderSha256 = $ffmpegHash
    encoderVersion = $ffmpegVersion
    encoderArguments = $ffmpegArguments
    encoderFilter = $filter
    encoderTimeoutSeconds = $EncoderTimeoutSeconds
    probeName = [IO.Path]::GetFileName($ffprobeCommand.Source)
    probeSha256 = $ffprobeHash
    probeVersion = $ffprobeVersion
    probeArguments = $ffprobeArguments
}
}

$provenance = [pscustomobject][ordered]@{
    schema = 'RimWorldDevGateway/ShowcaseCaptureEvidence/v1'
    showcaseId = $ShowcaseId
    capturedUtc = [datetime]::UtcNow.ToString('O')
    processId = $ProcessId
    processStartUtc = $processStartUtc
    runId = [string]$session.runId
    orderedPackageIds = $loadedPackageIds
    orderedPackageIdentities = $packageIdentities
    observedBeats = @($ObservedBeats)
    reviewObservation = $ReviewObservation.Trim()
    manifestPath = [IO.Path]::GetFullPath($Manifest)
    stateBefore = $stateBefore
    stateAfter = $stateAfter
    selectionMutated = (Get-CanonicalJson $stateBefore.selectedThings) -cne (Get-CanonicalJson $stateAfter.selectedThings) -or
        (Get-CanonicalJson $stateBefore.uiSelection) -cne (Get-CanonicalJson $stateAfter.uiSelection)
    cameraMutated = (Get-CanonicalJson $stateBefore.camera) -cne (Get-CanonicalJson $stateAfter.camera)
    plan = $plan
    actualCaptureDurationMilliseconds = $captureClock.ElapsedMilliseconds
    frames = $frameEvidence
    pngOptimizer = [pscustomobject][ordered]@{
        name = [IO.Path]::GetFileName($optimizerCommand.Source)
        sha256 = (Get-FileHash -LiteralPath $optimizerCommand.Source -Algorithm SHA256).Hash
        version = ([string]$optimizerVersionRun.StandardOutput -split "`r?`n")[0]
        arguments = $optimizerArguments
    }
    still = [pscustomobject][ordered]@{
        path = Get-RelativePath -Root $outputRoot -Path $stillPath
        bytes = (Get-Item -LiteralPath $stillPath).Length
        sha256 = (Get-FileHash -LiteralPath $stillPath -Algorithm SHA256).Hash
    }
    gif = $gifEvidence
    bearerTokenRetained = $false
}
$provenancePath = Join-Path $outputRoot ($BaseName + '.capture.json')
[IO.File]::WriteAllText(
    $provenancePath,
    ($provenance | ConvertTo-Json -Depth 8) + [Environment]::NewLine,
    [Text.UTF8Encoding]::new($false))
$result = [pscustomobject][ordered]@{
    status = 'captured'
    still = $stillPath
    gif = $gifPath
    provenance = $provenancePath
    gifBytes = $gifBytes
    durationSeconds = $durationSeconds
}
if ($Output -eq 'json') { $result | ConvertTo-Json -Compress } else { $result | Format-List }
