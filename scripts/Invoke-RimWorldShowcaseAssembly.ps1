<#
.SYNOPSIS
Assembles reviewed fixed-camera showcase segments into one bounded hard-cut GIF.

.DESCRIPTION
Each source is a complete v1 ShowcaseCaptureEvidence directory captured from the same exact
RimWorld process and package build. The command copies the retained PNG frames in declared order,
encodes a sub-five-second hard-cut GIF, and writes a v1 assembly evidence document that hashes
every source capture. It never contacts or mutates the game.

.EXAMPLE
.\scripts\Invoke-RimWorldShowcaseAssembly.ps1 -SegmentCapturePaths @('.\segments\order\order.capture.json','.\segments\delivery\delivery.capture.json') `
  -OutputDirectory .\final -ShowcaseId gastronomy-service -BaseName gastronomy-service `
  -ObservedBeats @('order','cutlery','full-kitchen-roster','meal-delivery') -FramesPerSecond 5 -Output json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]]$SegmentCapturePaths,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidatePattern('^[a-z0-9][a-z0-9-]{0,47}$')][string]$ShowcaseId,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$')][string]$BaseName,
    [Parameter(Mandatory)][string[]]$ObservedBeats,
    [Parameter(Mandatory)][string]$ReviewObservation,
    [ValidateRange(1,12)][int]$FramesPerSecond = 5,
    [ValidateRange(1,60)][int]$StillFrameIndex = 1,
    [ValidateRange(64,1280)][int]$GifWidth = 960,
    [ValidateRange(2,256)][int]$GifColors = 96,
    [ValidateRange(1024,1048575)][int]$MaximumGifBytes = 1048575,
    [string]$FfmpegPath = 'ffmpeg',
    [string]$FfprobePath = 'ffprobe',
    [ValidateSet('table','json')][string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$maximumDurationSeconds = 5

function Exit-InvalidInput([string]$Message) { [Console]::Error.WriteLine($Message); exit 2 }
function Get-CanonicalJson([object]$Value) { $Value | ConvertTo-Json -Depth 16 -Compress }
function Get-RelativePath([string]$Root,[string]$Path) {
    $rootUri=[Uri]::new($Root.TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar)
    [Uri]::UnescapeDataString($rootUri.MakeRelativeUri([Uri]::new($Path)).ToString())
}
function Quote([string]$Value) {
    if ($Value -notmatch '[\s"]') { return $Value }
    return '"' + $Value.Replace('"','\"') + '"'
}
function Invoke-Child([string]$FilePath,[string[]]$Arguments,[string]$WorkingDirectory,[int]$TimeoutMilliseconds) {
    $info=[Diagnostics.ProcessStartInfo]::new()
    $info.FileName=$FilePath
    $info.Arguments=(($Arguments|ForEach-Object { Quote ([string]$_) }) -join ' ')
    $info.WorkingDirectory=$WorkingDirectory
    $info.UseShellExecute=$false
    $info.CreateNoWindow=$true
    $info.RedirectStandardOutput=$true
    $info.RedirectStandardError=$true
    $process=[Diagnostics.Process]::new(); $process.StartInfo=$info
    try {
        if(-not $process.Start()){throw "Could not start $FilePath."}
        $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
        if(-not $process.WaitForExit($TimeoutMilliseconds)){
            $id=$process.Id; $start=$process.StartTime.ToUniversalTime().ToString('O')
            $process.Kill()
            if(-not $process.WaitForExit(10000)){throw "Timed-out owned child $id ($start) could not be terminated."}
            throw "Timed-out owned child $id ($start) was terminated."
        }
        [pscustomobject]@{ExitCode=$process.ExitCode;StandardOutput=$stdout.GetAwaiter().GetResult();StandardError=$stderr.GetAwaiter().GetResult()}
    } finally { $process.Dispose() }
}

if ($SegmentCapturePaths.Count -lt 2 -or $SegmentCapturePaths.Count -gt 12) { Exit-InvalidInput 'Two to twelve segment captures are required.' }
if ($ObservedBeats.Count -lt 2 -or @($ObservedBeats|Where-Object {[string]::IsNullOrWhiteSpace($_)}).Count -ne 0) { Exit-InvalidInput 'Observed beats must be a nonempty ordered sequence.' }
if ([string]::IsNullOrWhiteSpace($ReviewObservation)) { Exit-InvalidInput 'A live visual review observation is required.' }

$outputRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $outputRoot){
    $existing=@(Get-ChildItem -LiteralPath $outputRoot -Force)
    if($existing.Count -ne 1 -or -not $existing[0].PSIsContainer -or $existing[0].Name -cne 'segments'){
        Exit-InvalidInput "Output directory must be absent, empty, or contain only its reviewed segments directory: $outputRoot"
    }
} else { $null=New-Item -ItemType Directory -Path $outputRoot }
$framesRoot=Join-Path $outputRoot 'frames'; $null=New-Item -ItemType Directory -Path $framesRoot

$segments=[System.Collections.Generic.List[object]]::new()
$frames=[System.Collections.Generic.List[object]]::new()
$identity=$null; $orderedBeatUnion=[System.Collections.Generic.List[string]]::new(); $frameIndex=0
foreach($captureInput in $SegmentCapturePaths){
    $capturePath=[IO.Path]::GetFullPath($captureInput)
    $segmentRootBoundary=[IO.Path]::GetFullPath((Join-Path $outputRoot 'segments')).TrimEnd('\')+'\'
    if(-not $capturePath.StartsWith($segmentRootBoundary,[StringComparison]::OrdinalIgnoreCase)){throw "Segment capture must live under the final evidence segments directory: $capturePath"}
    if(-not(Test-Path -LiteralPath $capturePath -PathType Leaf)){throw "Segment capture is missing: $capturePath"}
    $capture=Get-Content -LiteralPath $capturePath -Raw -Encoding UTF8|ConvertFrom-Json
    if([string]$capture.schema -cne 'RimWorldDevGateway/ShowcaseCaptureEvidence/v1' -or
       [string]$capture.showcaseId -cne $ShowcaseId -or [bool]$capture.selectionMutated -or [bool]$capture.cameraMutated -or
       [bool]$capture.bearerTokenRetained -or @($capture.frames).Count -lt 1){throw "Invalid source segment: $capturePath"}
    $segmentIdentity=[pscustomobject][ordered]@{
        processId=[int]$capture.processId; processStartUtc=[string]$capture.processStartUtc; runId=[string]$capture.runId
        orderedPackageIds=@($capture.orderedPackageIds); orderedPackageIdentities=@($capture.orderedPackageIdentities)
    }
    if($null -eq $identity){$identity=$segmentIdentity}
    elseif((Get-CanonicalJson $segmentIdentity) -cne (Get-CanonicalJson $identity)){throw 'All hard-cut segments must come from the same exact process and package build.'}
    foreach($beat in @($capture.observedBeats)){ $orderedBeatUnion.Add([string]$beat) }
    $segmentRoot=Split-Path -Parent $capturePath; $first=$frameIndex+1
    foreach($sourceFrame in @($capture.frames)){
        $sourcePath=[IO.Path]::GetFullPath((Join-Path $segmentRoot ([string]$sourceFrame.path).Replace('/','\')))
        if(-not(Test-Path -LiteralPath $sourcePath -PathType Leaf) -or
           (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -cne [string]$sourceFrame.sha256 -or
           (Get-Item -LiteralPath $sourcePath).Length -ne [long]$sourceFrame.bytes){throw "Segment frame identity failed: $sourcePath"}
        $frameIndex++
        $target=Join-Path $framesRoot ('frame-{0:D4}.png' -f $frameIndex)
        Copy-Item -LiteralPath $sourcePath -Destination $target
        $frames.Add([pscustomobject][ordered]@{
            index=$frameIndex; elapsedMilliseconds=[long][Math]::Round((($frameIndex-1)*1000.0)/$FramesPerSecond)+1
            path=Get-RelativePath $outputRoot $target; bytes=(Get-Item $target).Length
            sha256=(Get-FileHash $target -Algorithm SHA256).Hash; crop=$sourceFrame.crop
        })
    }
    $segments.Add([pscustomobject][ordered]@{
        index=$segments.Count+1; capturePath=Get-RelativePath $outputRoot $capturePath; segmentCaptureSha256=(Get-FileHash $capturePath -Algorithm SHA256).Hash
        observedBeats=@($capture.observedBeats); firstOutputFrame=$first; lastOutputFrame=$frameIndex
        stateBefore=$capture.stateBefore; stateAfter=$capture.stateAfter
    })
}

if((Get-CanonicalJson @($orderedBeatUnion)) -cne (Get-CanonicalJson @($ObservedBeats))){throw 'Segment beats do not form the exact declared ordered beat sequence.'}
$duration=$frameIndex/[double]$FramesPerSecond
if($duration -gt $maximumDurationSeconds){throw "Hard-cut sequence is $duration seconds and exceeds $maximumDurationSeconds seconds."}
if($StillFrameIndex -gt $frameIndex){Exit-InvalidInput '-StillFrameIndex exceeds the assembled frame count.'}

$stillPath=Join-Path $outputRoot ($BaseName+'.png')
Copy-Item (Join-Path $framesRoot ('frame-{0:D4}.png' -f $StillFrameIndex)) $stillPath
$ffmpeg=(Get-Command $FfmpegPath -ErrorAction Stop).Source; $ffprobe=(Get-Command $FfprobePath -ErrorAction Stop).Source
$ffmpegVersion=((Invoke-Child $ffmpeg @('-version') $outputRoot 10000).StandardOutput -split "`r?`n")[0]
$ffprobeVersion=((Invoke-Child $ffprobe @('-version') $outputRoot 10000).StandardOutput -split "`r?`n")[0]
$gifPath=Join-Path $outputRoot ($BaseName+'.gif')
$filter="fps=$FramesPerSecond,scale=$GifWidth`:-1:flags=lanczos,split[s0][s1];[s0]palettegen=max_colors=$GifColors`:stats_mode=diff[p];[s1][p]paletteuse=dither=bayer`:bayer_scale=5`:diff_mode=rectangle"
$encoderArguments=@('-hide_banner','-loglevel','error','-y','-framerate',[string]$FramesPerSecond,'-i','frames/frame-%04d.png','-filter_complex',$filter,'-loop','0',($BaseName+'.gif'))
$encode=Invoke-Child $ffmpeg $encoderArguments $outputRoot 60000
if($encode.ExitCode -ne 0){throw "ffmpeg failed: $($encode.StandardError)"}
if((Get-Item $gifPath).Length -gt $MaximumGifBytes){throw 'Assembled GIF exceeds the reviewed Steam byte limit.'}
$probeArguments=@('-v','error','-show_entries','format=duration','-of','default=noprint_wrappers=1:nokey=1',($BaseName+'.gif'))
$probe=Invoke-Child $ffprobe $probeArguments $outputRoot 10000
[double]$actual=0
if($probe.ExitCode -ne 0 -or -not [double]::TryParse($probe.StandardOutput.Trim(),[Globalization.NumberStyles]::Float,[Globalization.CultureInfo]::InvariantCulture,[ref]$actual) -or $actual -gt 5 -or $actual -le 0){throw 'Assembled GIF duration is invalid.'}

$firstSegment=$segments[0]; $lastSegment=$segments[$segments.Count-1]
$evidence=[pscustomobject][ordered]@{
    schema='RimWorldDevGateway/ShowcaseCaptureAssemblyEvidence/v1'; showcaseId=$ShowcaseId; capturedUtc=[datetime]::UtcNow.ToString('O')
    processId=$identity.processId; processStartUtc=$identity.processStartUtc; runId=$identity.runId
    orderedPackageIds=$identity.orderedPackageIds; orderedPackageIdentities=$identity.orderedPackageIdentities
    observedBeats=@($ObservedBeats); reviewObservation=$ReviewObservation.Trim(); sourceSegments=$segments
    stateBefore=$firstSegment.stateBefore; stateAfter=$lastSegment.stateAfter
    selectionMutated=(Get-CanonicalJson $firstSegment.stateBefore.selectedThings) -cne (Get-CanonicalJson $lastSegment.stateAfter.selectedThings)
    cameraMutated=(Get-CanonicalJson $firstSegment.stateBefore.camera) -cne (Get-CanonicalJson $lastSegment.stateAfter.camera)
    plan=[pscustomobject][ordered]@{schema='RimWorldDevGateway/ShowcaseCaptureAssemblyPlan/v1';frameCount=$frameIndex;framesPerSecond=$FramesPerSecond;durationSeconds=$duration;width=[int]$frames[0].crop.width;height=[int]$frames[0].crop.height;offsetX=0;offsetY=0;stillFrameIndex=$StillFrameIndex;gifWidth=$GifWidth;gifColors=$GifColors;maximumDurationSeconds=$maximumDurationSeconds;stillOnly=$false;mutatesGameState=$false;selectsThings=$false}
    actualCaptureDurationMilliseconds=[long][Math]::Round($actual*1000); frames=$frames
    pngOptimizer=$null
    still=[pscustomobject][ordered]@{path=Get-RelativePath $outputRoot $stillPath;bytes=(Get-Item $stillPath).Length;sha256=(Get-FileHash $stillPath -Algorithm SHA256).Hash}
    gif=[pscustomobject][ordered]@{path=Get-RelativePath $outputRoot $gifPath;bytes=(Get-Item $gifPath).Length;sha256=(Get-FileHash $gifPath -Algorithm SHA256).Hash;nominalDurationSeconds=$duration;actualDurationSeconds=$actual;encoderName=[IO.Path]::GetFileName($ffmpeg);encoderSha256=(Get-FileHash $ffmpeg -Algorithm SHA256).Hash;encoderVersion=$ffmpegVersion;encoderArguments=$encoderArguments;encoderFilter=$filter;encoderTimeoutSeconds=60;probeName=[IO.Path]::GetFileName($ffprobe);probeSha256=(Get-FileHash $ffprobe -Algorithm SHA256).Hash;probeVersion=$ffprobeVersion;probeArguments=$probeArguments}
    bearerTokenRetained=$false
}
$evidencePath=Join-Path $outputRoot ($BaseName+'.capture.json')
[IO.File]::WriteAllText($evidencePath,($evidence|ConvertTo-Json -Depth 16)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
$result=[pscustomobject]@{status='assembled';still=$stillPath;gif=$gifPath;provenance=$evidencePath;segments=$segments.Count;frames=$frameIndex;durationSeconds=$actual}
if($Output -eq 'json'){$result|ConvertTo-Json -Compress}else{$result|Format-List}
