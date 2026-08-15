<#
.SYNOPSIS
Builds and stages an immutable Immersive Chefs RimWorld 1.6 Workshop candidate.

.DESCRIPTION
Requires a clean committed repository, builds Release, copies only the shipping allowlist, omits
symbols and development-only integration probes, hashes every staged byte, and writes a
mutation-free Workshop publication plan. This command never invokes Steam.

.EXAMPLE
.\scripts\Build-ImmersiveChefsRelease.ps1 -Output json
#>
[CmdletBinding()]
param(
    [string]$ArtifactsPath,

    [string]$RimWorldPath = 'F:\Steam\steamapps\common\RimWorld',

    [ValidateSet('table', 'json')]
    [string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Exit-InvalidInput {
    param([Parameter(Mandatory)][string]$Message)
    [Console]::Error.WriteLine($Message)
    exit 2
}

function Get-RelativePath {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Path)
    $rootUri = [Uri]::new($Root.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar)
    return [Uri]::UnescapeDataString($rootUri.MakeRelativeUri([Uri]::new($Path)).ToString()).Replace('/', '\')
}

function Get-Sha256Text {
    param([Parameter(Mandatory)][string]$Text)
    $bytes = [Text.Encoding]::UTF8.GetBytes($Text)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Get-CanonicalJson([object]$Value) {
    return $Value | ConvertTo-Json -Depth 20 -Compress
}

function Get-WorkshopShowcaseDesignEvidence {
    param(
        [Parameter(Mandatory)][object]$Showcase,
        [Parameter(Mandatory)][string]$ReleaseRoot,
        [Parameter(Mandatory)][string]$SourceCatalogPath
    )

    $showcaseId = [string]$Showcase.id
    $relativePath = [string]$Showcase.designRecord
    if ($showcaseId -notmatch '^[a-z0-9-]{1,48}$' -or
        $relativePath -cne "designs/$showcaseId.md" -or
        [IO.Path]::IsPathRooted($relativePath) -or
        $relativePath -match '(^|[\/])\.\.([\/]|$)') {
        throw "Workshop showcase '$showcaseId' has an invalid design-record path."
    }
    $workshopRoot = [IO.Path]::GetFullPath((Join-Path $ReleaseRoot 'workshop')).TrimEnd('\') + '\'
    $path = [IO.Path]::GetFullPath((Join-Path $workshopRoot $relativePath.Replace('/', '\')))
    if (-not $path.StartsWith($workshopRoot, [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Workshop showcase '$showcaseId' design record is missing or outside the release root."
    }
    $item = Get-Item -LiteralPath $path
    if ($item.Length -le 0 -or $item.Length -gt 256KB) {
        throw "Workshop showcase '$showcaseId' design record must be nonempty and at most 256 KiB."
    }
    $text = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    if ($text.IndexOf([char]0) -ge 0) { throw "Workshop showcase '$showcaseId' design record contains a NUL character." }
    function Read-DesignField([string]$Name) {
        $match = [regex]::Match($text, '(?m)^- ' + [regex]::Escape($Name) + ': `([^`]+)`\s*$')
        if (-not $match.Success -or [string]::IsNullOrWhiteSpace($match.Groups[1].Value)) {
            throw "Workshop showcase '$showcaseId' design record is missing '$Name'."
        }
        return $match.Groups[1].Value.Trim()
    }
    $recordId = Read-DesignField 'Showcase ID'
    $version = Read-DesignField 'Design-record version'
    $owner = Read-DesignField 'Owner'
    $status = Read-DesignField 'Status'
    $packages = Read-DesignField 'Exact presentation packages'
    $nativeWorkflow = Read-DesignField 'Native workflow'
    $expectedPackages = @($Showcase.requiredPackageIds | ForEach-Object { [string]$_ }) -join ' -> '
    if ($recordId -cne $showcaseId -or $version -cne '1' -or
        $owner -cne 'fumblesneeze.immersivechefs' -or $status -cne 'live-reviewed' -or
        $packages -cne $expectedPackages) {
        throw "Workshop showcase '$showcaseId' design record is not an exact live-reviewed package-bound record."
    }
    $sectionPattern = '(?ms)^## {0}\s*$(.*?)(?=^## |\z)'
    function Read-DesignSection([string]$Name) {
        $match = [regex]::Match($text, ($sectionPattern -f [regex]::Escape($Name)))
        if (-not $match.Success) { throw "Workshop showcase '$showcaseId' design record is missing section '$Name'." }
        return $match.Groups[1].Value
    }
    $references = Read-DesignSection 'References'
    $referenceIds = @([regex]::Matches($references, '(?m)^\|\s*(R\d{2})\s*\|') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    if ($referenceIds.Count -lt 3 -or -not (Test-Path -LiteralPath $SourceCatalogPath -PathType Leaf)) {
        throw "Workshop showcase '$showcaseId' design record needs at least three catalogued references."
    }
    $catalog = Get-Content -LiteralPath $SourceCatalogPath -Raw -Encoding UTF8
    foreach ($referenceId in $referenceIds) {
        if ($catalog -notmatch ('(?m)^\|\s*' + [regex]::Escape($referenceId) + '\s*\|')) {
            throw "Workshop showcase '$showcaseId' design record cites unknown reference '$referenceId'."
        }
    }
    $rules = Read-DesignSection 'Applied rules'
    $ruleRows = @([regex]::Matches($rules, '(?m)^\|\s*([a-z0-9][a-z0-9-]{0,63})\s*\|') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -cne 'rule-id' })
    $placements = Read-DesignSection 'Planned zones and placements'
    $placementRows = @([regex]::Matches($placements, '(?m)^\|\s*([^|]+?)\s*\|') | ForEach-Object { $_.Groups[1].Value.Trim() } | Where-Object { $_ -cne 'Zone/object' -and $_ -notmatch '^-+$' })
    $adjacency = Read-DesignSection 'Adjacency graph'
    if ($ruleRows.Count -lt 3 -or $placementRows.Count -lt 3 -or $adjacency -notmatch '--[a-z0-9 -]+-->') {
        throw "Workshop showcase '$showcaseId' design record needs at least three applied rules, three placements, and a directed adjacency graph."
    }
    $liveReview = Read-DesignSection 'Live review'
    foreach ($field in @('Exact build/package identity','Exact process/evidence directory','Player action observed','Visible result observed','Geometry/material/traffic observations at final crop size','Remaining caveats')) {
        $match = [regex]::Match($liveReview, '(?m)^- ' + [regex]::Escape($field) + ':\s*(.+?)\s*$')
        if (-not $match.Success -or [string]::IsNullOrWhiteSpace($match.Groups[1].Value)) {
            throw "Workshop showcase '$showcaseId' design record is not live-reviewed; '$field' is empty."
        }
    }
    return [pscustomobject][ordered]@{
        showcaseId = $showcaseId
        path = $path
        bytes = [long]$item.Length
        sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        status = $status
        exactPresentationPackages = @($Showcase.requiredPackageIds | ForEach-Object { [string]$_ })
        nativeWorkflow = $nativeWorkflow
        referenceIds = $referenceIds
        ruleCount = $ruleRows.Count
        placementCount = $placementRows.Count
    }
}

function Invoke-ShowcaseMediaTool {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [Parameter(Mandatory)][int]$TimeoutMilliseconds
    )
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $FilePath
    $start.Arguments = ($Arguments | ForEach-Object { '"' + ([string]$_).Replace('"', '\"') + '"' }) -join ' '
    $start.WorkingDirectory = $WorkingDirectory
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw "Could not start showcase media tool: $FilePath" }
        $standardOutput = $process.StandardOutput.ReadToEndAsync()
        $standardError = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutMilliseconds)) {
            $process.Kill()
            if (-not $process.WaitForExit(10000)) { throw "Showcase media tool did not exit after bounded termination: $FilePath" }
            throw "Showcase media tool exceeded its bounded timeout: $FilePath"
        }
        return [pscustomobject]@{
            ExitCode = [int]$process.ExitCode
            StandardOutput = $standardOutput.GetAwaiter().GetResult()
            StandardError = $standardError.GetAwaiter().GetResult()
        }
    }
    finally { $process.Dispose() }
}

function Get-RasterImageInfo([string]$Path) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    Add-Type -AssemblyName System.Drawing
    if ($bytes.Length -ge 24 -and
        $bytes[0] -eq 0x89 -and $bytes[1] -eq 0x50 -and $bytes[2] -eq 0x4E -and $bytes[3] -eq 0x47 -and
        $bytes[4] -eq 0x0D -and $bytes[5] -eq 0x0A -and $bytes[6] -eq 0x1A -and $bytes[7] -eq 0x0A -and
        [Text.Encoding]::ASCII.GetString($bytes, 12, 4) -ceq 'IHDR') {
        $width = ($bytes[16] -shl 24) -bor ($bytes[17] -shl 16) -bor ($bytes[18] -shl 8) -bor $bytes[19]
        $height = ($bytes[20] -shl 24) -bor ($bytes[21] -shl 16) -bor ($bytes[22] -shl 8) -bor $bytes[23]
        if ($width -le 0 -or $height -le 0) { throw "PNG has invalid dimensions: $Path" }
        $image = $null
        try {
            $image = [Drawing.Image]::FromFile($Path)
            if ($image.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid -or
                $image.Width -ne $width -or $image.Height -ne $height) {
                throw "PNG decoder rejected the recorded format or dimensions: $Path"
            }
        }
        finally { if ($null -ne $image) { $image.Dispose() } }
        return [pscustomobject][ordered]@{ format = 'png'; width = [int]$width; height = [int]$height; frameCount = 1; durationSeconds = $null }
    }
    if ($bytes.Length -ge 13 -and
        ([Text.Encoding]::ASCII.GetString($bytes, 0, 6) -in @('GIF87a', 'GIF89a'))) {
        $width = [int]$bytes[6] -bor ([int]$bytes[7] -shl 8)
        $height = [int]$bytes[8] -bor ([int]$bytes[9] -shl 8)
        if ($width -le 0 -or $height -le 0) { throw "GIF has invalid dimensions: $Path" }
        $image = $null
        try {
            $image = [Drawing.Image]::FromFile($Path)
            if ($image.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Gif.Guid -or
                $image.Width -ne $width -or $image.Height -ne $height) {
                throw "GIF decoder rejected the recorded format or dimensions: $Path"
            }
            $frameCount = $image.GetFrameCount([Drawing.Imaging.FrameDimension]::Time)
            $delayProperty = $image.GetPropertyItem(0x5100)
            $delayCentiseconds = 0L
            for ($index = 0; $index -lt $frameCount; $index++) {
                $delayCentiseconds += [BitConverter]::ToInt32($delayProperty.Value, $index * 4)
            }
        }
        finally { if ($null -ne $image) { $image.Dispose() } }
        if ($frameCount -lt 1 -or $delayCentiseconds -le 0) { throw "GIF has no timed image frames: $Path" }
        return [pscustomobject][ordered]@{ format = 'gif'; width = $width; height = $height; frameCount = $frameCount; durationSeconds = $delayCentiseconds / 100.0 }
    }
    throw "Showcase media has an unsupported or invalid image signature: $Path"
}

function Get-WorkshopShowcaseEvidence {
    param(
        [Parameter(Mandatory)][object]$Showcase,
        [Parameter(Mandatory)][string]$ReleaseRoot,
        [Parameter(Mandatory)][string]$ReviewedProductRoot
    )

    $showcaseId = [string]$Showcase.id
    $formats = @($Showcase.formats | ForEach-Object { [string]$_ })
    $screenshotRelativePath = [string]$Showcase.outputs.screenshot
    $declared = @([pscustomobject]@{ format = 'screenshot'; relativePath = $screenshotRelativePath })
    if ($formats -ccontains 'gif') {
        $declared += [pscustomobject]@{ format = 'gif'; relativePath = [string]$Showcase.outputs.gif }
    }
    $outputs = @($declared | ForEach-Object {
        if ([IO.Path]::IsPathRooted([string]$_.relativePath) -or
            [string]$_.relativePath -match '(^|[\\/])\.\.([\\/]|$)') {
            throw "Workshop showcase '$showcaseId' has an unsafe output path."
        }
        $path = [IO.Path]::GetFullPath((Join-Path $ReleaseRoot ('workshop\' + ([string]$_.relativePath).Replace('/', '\'))))
        $expectedRoot = [IO.Path]::GetFullPath((Join-Path $ReleaseRoot 'workshop')).TrimEnd('\') + '\'
        if (-not $path.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Workshop showcase output is missing or outside the release root: $path"
        }
        $item = Get-Item -LiteralPath $path
        if ($item.Length -le 0 -or $item.Length -ge 1MB) {
            throw "Workshop showcase output must be nonempty and under Steam's 1 MiB limit: $path"
        }
        $image = Get-RasterImageInfo $path
        [pscustomobject][ordered]@{
            format = [string]$_.format
            path = $path
            bytes = [long]$item.Length
            sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
            width = [int]$image.width
            height = [int]$image.height
            frameCount = [int]$image.frameCount
            durationSeconds = $image.durationSeconds
        }
    })

    $screenshot = @($outputs | Where-Object format -CEQ 'screenshot')[0]
    $evidencePath = [IO.Path]::ChangeExtension([string]$screenshot.path, '.capture.json')
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "Workshop showcase capture evidence is missing: $evidencePath"
    }
    try { $evidence = Get-Content -LiteralPath $evidencePath -Raw -Encoding UTF8 | ConvertFrom-Json }
    catch { throw "Workshop showcase capture evidence is unreadable: $evidencePath" }
    $isAssembly = [string]$evidence.schema -ceq 'RimWorldDevGateway/ShowcaseCaptureAssemblyEvidence/v1'
    [datetimeoffset]$capturedUtc = [datetimeoffset]::MinValue
    [datetimeoffset]$processStartUtc = [datetimeoffset]::MinValue
    $stateBeforeSelection = (Get-CanonicalJson @($evidence.stateBefore.selectedThings)) + '|' + (Get-CanonicalJson @($evidence.stateBefore.uiSelection))
    $stateAfterSelection = (Get-CanonicalJson @($evidence.stateAfter.selectedThings)) + '|' + (Get-CanonicalJson @($evidence.stateAfter.uiSelection))
    $declaredPackages = @($Showcase.requiredPackageIds | ForEach-Object { [string]$_ })
    $capturePackages = @($declaredPackages) + 'fumblesneeze.rimworlddevgateway'
    $declaredBeats = @($Showcase.beats | ForEach-Object { [string]$_ })
    if ((-not $isAssembly -and [string]$evidence.schema -cne 'RimWorldDevGateway/ShowcaseCaptureEvidence/v1') -or
        [string]$evidence.showcaseId -cne $showcaseId -or
        -not [datetimeoffset]::TryParse([string]$evidence.capturedUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind, [ref]$capturedUtc) -or
        [int]$evidence.processId -le 0 -or
        -not [datetimeoffset]::TryParse([string]$evidence.processStartUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind, [ref]$processStartUtc) -or
        [string]$evidence.runId -notmatch '^[a-f0-9]{32}$' -or
        $declaredPackages -ccontains 'fumblesneeze.rimworlddevgateway' -or
        (Get-CanonicalJson @($evidence.orderedPackageIds)) -cne (Get-CanonicalJson $capturePackages) -or
        @($evidence.orderedPackageIdentities).Count -ne $capturePackages.Count -or
        (Get-CanonicalJson @($evidence.observedBeats)) -cne (Get-CanonicalJson $declaredBeats) -or
        [string]::IsNullOrWhiteSpace([string]$evidence.reviewObservation) -or
        [bool]$evidence.bearerTokenRetained -or
        (-not $isAssembly -and ([bool]$evidence.selectionMutated -or [bool]$evidence.cameraMutated -or
            $stateBeforeSelection -cne $stateAfterSelection -or
            (Get-CanonicalJson $evidence.stateBefore.camera) -cne (Get-CanonicalJson $evidence.stateAfter.camera))) -or
        [string]$evidence.plan.schema -cne $(if ($isAssembly) { 'RimWorldDevGateway/ShowcaseCaptureAssemblyPlan/v1' } else { 'RimWorldDevGateway/ShowcaseCapturePlan/v1' }) -or
        [bool]$evidence.plan.mutatesGameState -or [bool]$evidence.plan.selectsThings -or
        ([bool]$evidence.plan.stillOnly -ne ($formats -cnotcontains 'gif')) -or
        [int]$evidence.plan.width -ne [int]$Showcase.crop.width -or
        [int]$evidence.plan.height -ne [int]$Showcase.crop.height -or
        [int]$evidence.plan.offsetX -ne [int]$Showcase.crop.offsetX -or
        [int]$evidence.plan.offsetY -ne [int]$Showcase.crop.offsetY -or
        [int]$evidence.plan.frameCount -lt 1 -or [int]$evidence.plan.framesPerSecond -lt 1 -or
        [double]$evidence.plan.durationSeconds -le 0 -or [double]$evidence.plan.durationSeconds -gt 5 -or
        [long]$evidence.actualCaptureDurationMilliseconds -le 0 -or [long]$evidence.actualCaptureDurationMilliseconds -gt 5000 -or
        @($evidence.frames).Count -ne [int]$evidence.plan.frameCount) {
        throw "Workshop showcase '$showcaseId' has invalid or mutated capture evidence."
    }
    $evidenceDirectory = [IO.Path]::GetFullPath((Split-Path -Parent $evidencePath))
    $expectedOptimizerArguments = @('-strip','-colors','256','-define','png:compression-level=9','-define','png:compression-filter=5')
    if (-not $isAssembly) {
        $optimizerCommand = Get-Command ([string]$evidence.pngOptimizer.name) -ErrorAction SilentlyContinue
        $optimizerItem = if ($null -eq $optimizerCommand) { $null } else { Get-Item -LiteralPath $optimizerCommand.Source -ErrorAction SilentlyContinue }
        if ($null -eq $optimizerItem -or
            [string]$evidence.pngOptimizer.sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
            (Get-FileHash -LiteralPath $optimizerItem.FullName -Algorithm SHA256).Hash -cne [string]$evidence.pngOptimizer.sha256 -or
            [string]::IsNullOrWhiteSpace([string]$evidence.pngOptimizer.version) -or
            (Get-CanonicalJson @($evidence.pngOptimizer.arguments)) -cne (Get-CanonicalJson $expectedOptimizerArguments)) {
            throw "Workshop showcase '$showcaseId' PNG optimizer provenance is invalid."
        }
        $optimizerVersion = Invoke-ShowcaseMediaTool -FilePath $optimizerItem.FullName -Arguments @('-version') -WorkingDirectory $evidenceDirectory -TimeoutMilliseconds 10000
        if ($optimizerVersion.ExitCode -ne 0 -or (([string]$optimizerVersion.StandardOutput -split "`r?`n")[0]) -cne [string]$evidence.pngOptimizer.version) {
            throw "Workshop showcase '$showcaseId' PNG optimizer version is not the recorded reviewed tool."
        }
    }
    else {
        $segments = @($evidence.sourceSegments)
        $segmentRoot = [IO.Path]::GetFullPath((Join-Path $evidenceDirectory 'segments')).TrimEnd('\') + '\'
        $segmentBeats = [System.Collections.Generic.List[string]]::new()
        if ($segments.Count -lt 2 -or $segments.Count -gt 12) { throw "Workshop showcase '$showcaseId' has an invalid hard-cut segment count." }
        for ($segmentIndex = 0; $segmentIndex -lt $segments.Count; $segmentIndex++) {
            $segment = $segments[$segmentIndex]
            $segmentPath = [IO.Path]::GetFullPath((Join-Path $evidenceDirectory ([string]$segment.capturePath).Replace('/', '\')))
            if ([int]$segment.index -ne ($segmentIndex + 1) -or
                -not $segmentPath.StartsWith($segmentRoot, [StringComparison]::OrdinalIgnoreCase) -or
                -not (Test-Path -LiteralPath $segmentPath -PathType Leaf) -or
                [string]$segment.segmentCaptureSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
                (Get-FileHash -LiteralPath $segmentPath -Algorithm SHA256).Hash -cne [string]$segment.segmentCaptureSha256) {
                throw "Workshop showcase '$showcaseId' has invalid hard-cut source evidence."
            }
            $source = Get-Content -LiteralPath $segmentPath -Raw -Encoding UTF8 | ConvertFrom-Json
            if ([string]$source.schema -cne 'RimWorldDevGateway/ShowcaseCaptureEvidence/v1' -or
                [string]$source.showcaseId -cne $showcaseId -or
                [int]$source.processId -ne [int]$evidence.processId -or
                [string]$source.processStartUtc -cne [string]$evidence.processStartUtc -or
                [string]$source.runId -cne [string]$evidence.runId -or
                (Get-CanonicalJson @($source.orderedPackageIds)) -cne (Get-CanonicalJson @($evidence.orderedPackageIds)) -or
                (Get-CanonicalJson @($source.orderedPackageIdentities)) -cne (Get-CanonicalJson @($evidence.orderedPackageIdentities)) -or
                [bool]$source.selectionMutated -or [bool]$source.cameraMutated -or [bool]$source.bearerTokenRetained -or
                (Get-CanonicalJson @($source.observedBeats)) -cne (Get-CanonicalJson @($segment.observedBeats))) {
                throw "Workshop showcase '$showcaseId' hard-cut segment is not one immutable same-process capture."
            }
            foreach ($beat in @($source.observedBeats)) { $segmentBeats.Add([string]$beat) }
        }
        if ((Get-CanonicalJson @($segmentBeats)) -cne (Get-CanonicalJson $declaredBeats)) {
            throw "Workshop showcase '$showcaseId' hard-cut segment beats do not match the declaration."
        }
    }
    for ($identityIndex = 0; $identityIndex -lt $capturePackages.Count; $identityIndex++) {
        $identity = @($evidence.orderedPackageIdentities)[$identityIndex]
        $identityFiles = @($identity.files)
        $seenIdentityPaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        if ([int]$identity.index -ne $identityIndex -or
            [string]$identity.packageId -cne $capturePackages[$identityIndex] -or
            [string]::IsNullOrWhiteSpace([string]$identity.name) -or
            [string]::IsNullOrWhiteSpace([string]$identity.rootDir) -or
            $identityFiles.Count -lt 1 -or
            ($capturePackages[$identityIndex] -ceq 'fumblesneeze.immersivechefs' -and $identityFiles.Count -gt 8192) -or
            ($capturePackages[$identityIndex] -cne 'fumblesneeze.immersivechefs' -and $identityFiles.Count -gt 515)) {
            throw "Workshop showcase '$showcaseId' has an invalid package identity at position $identityIndex."
        }
        foreach ($identityFile in $identityFiles) {
            $identityPath = ([string]$identityFile.path).Replace('\', '/')
            if ([string]::IsNullOrWhiteSpace($identityPath) -or [IO.Path]::IsPathRooted($identityPath) -or
                $identityPath -match '(^|/)\.\.(/|$)' -or
                -not $seenIdentityPaths.Add($identityPath) -or
                [long]$identityFile.bytes -le 0 -or [string]$identityFile.sha256 -notmatch '^[A-Fa-f0-9]{64}$') {
                throw "Workshop showcase '$showcaseId' has invalid package identity-file evidence."
            }
        }
    }
    $productIdentity = @($evidence.orderedPackageIdentities | Where-Object { [string]$_.packageId -ceq 'fumblesneeze.immersivechefs' })
    if ($productIdentity.Count -ne 1 -or -not (Test-Path -LiteralPath $ReviewedProductRoot -PathType Container)) {
        throw "Workshop showcase '$showcaseId' was not captured from a reviewed Immersive Chefs package."
    }
    $reviewedProductRoot = [IO.Path]::GetFullPath($ReviewedProductRoot)
    $reviewedProductFiles = @(Get-ChildItem -LiteralPath $reviewedProductRoot -File -Recurse -ErrorAction Stop | Where-Object {
        $relative = (Get-RelativePath -Root $reviewedProductRoot -Path $_.FullName).Replace('\', '/')
        $relative -ceq 'About/About.xml' -or
            $relative -ceq 'About/PublishedFileId.txt' -or
            $relative -ceq 'LoadFolders.xml' -or
            ($relative.StartsWith('1.6/', [StringComparison]::Ordinal) -and
                -not $relative.EndsWith('.pdb', [StringComparison]::OrdinalIgnoreCase) -and
                $relative -cne '1.6/Patches/ImmersiveChefsIntegrationProbePatch.xml')
    } | Sort-Object { (Get-RelativePath -Root $reviewedProductRoot -Path $_.FullName).Replace('\', '/') } | ForEach-Object {
        [pscustomobject][ordered]@{
            path = (Get-RelativePath -Root $reviewedProductRoot -Path $_.FullName).Replace('\', '/')
            bytes = [long]$_.Length
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    })
    $capturedProductFiles = @($productIdentity[0].files | ForEach-Object {
        [pscustomobject][ordered]@{
            path = ([string]$_.path).Replace('\', '/')
            bytes = [long]$_.bytes
            sha256 = ([string]$_.sha256).ToUpperInvariant()
        }
    } | Sort-Object path)
    if ($reviewedProductFiles.Count -lt 1 -or
        (Get-CanonicalJson $capturedProductFiles) -cne (Get-CanonicalJson $reviewedProductFiles)) {
        throw "Workshop showcase '$showcaseId' was not captured from the complete reviewed Immersive Chefs package."
    }
    $previousElapsed = -1L
    $frameHashes = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $frameRoot = [IO.Path]::GetFullPath((Join-Path $evidenceDirectory 'frames')).TrimEnd('\') + '\'
    for ($index = 0; $index -lt @($evidence.frames).Count; $index++) {
        $frame = @($evidence.frames)[$index]
        $framePath = [IO.Path]::GetFullPath((Join-Path $evidenceDirectory ([string]$frame.path).Replace('/', '\')))
        $expectedFrameWidth = [int]$frame.crop.frameWidth
        $expectedFrameHeight = [int]$frame.crop.frameHeight
        $expectedX = [int][Math]::Min([Math]::Max(0, [Math]::Floor(($expectedFrameWidth - [int]$Showcase.crop.width) / 2.0) + [int]$Showcase.crop.offsetX), $expectedFrameWidth - [int]$Showcase.crop.width)
        $expectedY = [int][Math]::Min([Math]::Max(0, [Math]::Floor(($expectedFrameHeight - [int]$Showcase.crop.height) / 2.0) + [int]$Showcase.crop.offsetY), $expectedFrameHeight - [int]$Showcase.crop.height)
        if ([int]$frame.index -ne ($index + 1) -or
            [long]$frame.elapsedMilliseconds -le $previousElapsed -or [long]$frame.elapsedMilliseconds -gt 5000 -or
            [long]$frame.bytes -le 0 -or [string]$frame.sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
            [int]$frame.crop.width -ne [int]$Showcase.crop.width -or
            [int]$frame.crop.height -ne [int]$Showcase.crop.height -or
            $expectedFrameWidth -lt [int]$Showcase.crop.width -or $expectedFrameHeight -lt [int]$Showcase.crop.height -or
            [int]$frame.crop.x -ne $expectedX -or [int]$frame.crop.y -ne $expectedY -or
            -not $framePath.StartsWith($frameRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not (Test-Path -LiteralPath $framePath -PathType Leaf) -or
            (Get-Item -LiteralPath $framePath).Length -ne [long]$frame.bytes -or
            (Get-FileHash -LiteralPath $framePath -Algorithm SHA256).Hash -cne [string]$frame.sha256) {
            throw "Workshop showcase '$showcaseId' has invalid bounded source-frame evidence."
        }
        $frameImage = Get-RasterImageInfo $framePath
        if ([string]$frameImage.format -cne 'png' -or
            [int]$frameImage.width -ne [int]$Showcase.crop.width -or
            [int]$frameImage.height -ne [int]$Showcase.crop.height) {
            throw "Workshop showcase '$showcaseId' has a non-PNG or dimensionally inconsistent source frame."
        }
        $null = $frameHashes.Add([string]$frame.sha256)
        $previousElapsed = [long]$frame.elapsedMilliseconds
    }
    if ([string]$screenshot.format -cne 'screenshot' -or [int]$screenshot.width -ne [int]$Showcase.crop.width -or
        [int]$screenshot.height -ne [int]$Showcase.crop.height -or
        [IO.Path]::GetFileName([string]$evidence.still.path) -cne [IO.Path]::GetFileName([string]$screenshot.path) -or
        [long]$evidence.still.bytes -ne [long]$screenshot.bytes -or
        [string]$evidence.still.sha256 -cne [string]$screenshot.sha256 -or
        -not $frameHashes.Contains([string]$screenshot.sha256)) {
        throw "Workshop showcase '$showcaseId' screenshot does not match its capture evidence."
    }
    if ($formats -ccontains 'gif') {
        $gif = @($outputs | Where-Object format -CEQ 'gif')[0]
        if ([IO.Path]::GetFileName([string]$evidence.gif.path) -cne [IO.Path]::GetFileName([string]$gif.path) -or
            [long]$evidence.gif.bytes -ne [long]$gif.bytes -or
            [string]$evidence.gif.sha256 -cne [string]$gif.sha256 -or
            [string]$gif.format -cne 'gif' -or [int]$gif.width -ne [int]$evidence.plan.gifWidth -or
            [int]$gif.frameCount -ne [int]$evidence.plan.frameCount -or
            [double]$gif.durationSeconds -le 0 -or [double]$gif.durationSeconds -gt 5 -or
            [Math]::Abs([double]$gif.durationSeconds - [double]$evidence.gif.actualDurationSeconds) -gt 0.011 -or
            [double]$evidence.gif.actualDurationSeconds -le 0 -or [double]$evidence.gif.actualDurationSeconds -gt 5 -or
            [string]$evidence.gif.encoderName -notmatch '^[A-Za-z0-9._-]{1,64}$' -or
            [string]$evidence.gif.encoderSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
            [string]::IsNullOrWhiteSpace([string]$evidence.gif.encoderVersion) -or
            @($evidence.gif.encoderArguments).Count -lt 1 -or
            [string]::IsNullOrWhiteSpace([string]$evidence.gif.encoderFilter) -or
            [string]$evidence.gif.probeName -notmatch '^[A-Za-z0-9._-]{1,64}$' -or
            [string]$evidence.gif.probeSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
            [string]::IsNullOrWhiteSpace([string]$evidence.gif.probeVersion) -or
            @($evidence.gif.probeArguments).Count -lt 1) {
            throw "Workshop showcase '$showcaseId' GIF does not match its capture evidence."
        }
        $expectedInputPattern = 'frames/frame-%04d.png'
        $expectedFilter = "fps=$([int]$evidence.plan.framesPerSecond),scale=$([int]$evidence.plan.gifWidth)`:-1:flags=lanczos,split[s0][s1];[s0]palettegen=max_colors=$([int]$evidence.plan.gifColors)`:stats_mode=diff[p];[s1][p]paletteuse=dither=bayer`:bayer_scale=5`:diff_mode=rectangle"
        $expectedEncoderArguments = @('-hide_banner','-loglevel','error','-y','-framerate',[string][int]$evidence.plan.framesPerSecond,'-i',$expectedInputPattern,'-filter_complex',$expectedFilter,'-loop','0',[IO.Path]::GetFileName([string]$gif.path))
        if ((Get-CanonicalJson @($evidence.gif.encoderArguments)) -cne (Get-CanonicalJson $expectedEncoderArguments) -or
            [string]$evidence.gif.encoderFilter -cne $expectedFilter) {
            throw "Workshop showcase '$showcaseId' GIF encoder recipe does not bind the retained source frames."
        }
        $encoderCommand = Get-Command ([string]$evidence.gif.encoderName) -ErrorAction SilentlyContinue
        $encoderItem = if ($null -eq $encoderCommand) { $null } else { Get-Item -LiteralPath $encoderCommand.Source -ErrorAction SilentlyContinue }
        if ($null -eq $encoderItem -or @($evidence.gif.encoderArguments | Where-Object { [string]$_ -ceq $expectedInputPattern }).Count -ne 1) {
            throw "Workshop showcase '$showcaseId' GIF encoder identity or retained-frame input is invalid."
        }
        $versionResult = Invoke-ShowcaseMediaTool -FilePath $encoderItem.FullName -Arguments @('-version') -WorkingDirectory $evidenceDirectory -TimeoutMilliseconds 10000
        $currentEncoderVersion = ([string]$versionResult.StandardOutput -split "`r?`n")[0]
        if ($versionResult.ExitCode -ne 0 -or
            (Get-FileHash -LiteralPath $encoderItem.FullName -Algorithm SHA256).Hash -cne [string]$evidence.gif.encoderSha256 -or
            $currentEncoderVersion -cne [string]$evidence.gif.encoderVersion) {
            throw "Workshop showcase '$showcaseId' GIF encoder version is not the recorded reviewed tool."
        }
        $reencodedPath = Join-Path ([IO.Path]::GetTempPath()) ("immersive-chefs-showcase-" + [guid]::NewGuid().ToString('N') + '.gif')
        try {
            $reencode = Invoke-ShowcaseMediaTool -FilePath $encoderItem.FullName -Arguments (@($expectedEncoderArguments[0..($expectedEncoderArguments.Count - 2)]) + $reencodedPath) -WorkingDirectory $evidenceDirectory -TimeoutMilliseconds 60000
            if ($reencode.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $reencodedPath -PathType Leaf) -or
                (Get-FileHash -LiteralPath $reencodedPath -Algorithm SHA256).Hash -cne [string]$gif.sha256) {
                throw "Workshop showcase '$showcaseId' GIF cannot be reproduced from its retained source frames."
            }
        }
        finally {
            if (Test-Path -LiteralPath $reencodedPath -PathType Leaf) { Remove-Item -LiteralPath $reencodedPath -Force }
        }
        $probeCommand = Get-Command ([string]$evidence.gif.probeName) -ErrorAction SilentlyContinue
        $probeItem = if ($null -eq $probeCommand) { $null } else { Get-Item -LiteralPath $probeCommand.Source -ErrorAction SilentlyContinue }
        $expectedProbeArguments = @('-v','error','-show_entries','format=duration','-of','default=noprint_wrappers=1:nokey=1',[IO.Path]::GetFileName([string]$gif.path))
        if ($null -eq $probeItem -or
            (Get-FileHash -LiteralPath $probeItem.FullName -Algorithm SHA256).Hash -cne [string]$evidence.gif.probeSha256 -or
            (Get-CanonicalJson @($evidence.gif.probeArguments)) -cne (Get-CanonicalJson $expectedProbeArguments)) {
            throw "Workshop showcase '$showcaseId' GIF probe identity or recipe is invalid."
        }
        $probeVersion = Invoke-ShowcaseMediaTool -FilePath $probeItem.FullName -Arguments @('-version') -WorkingDirectory $evidenceDirectory -TimeoutMilliseconds 10000
        $probeDuration = Invoke-ShowcaseMediaTool -FilePath $probeItem.FullName -Arguments $expectedProbeArguments -WorkingDirectory $evidenceDirectory -TimeoutMilliseconds 10000
        [double]$observedProbeDuration = 0
        if ($probeVersion.ExitCode -ne 0 -or (([string]$probeVersion.StandardOutput -split "`r?`n")[0]) -cne [string]$evidence.gif.probeVersion -or
            $probeDuration.ExitCode -ne 0 -or
            -not [double]::TryParse(([string]$probeDuration.StandardOutput).Trim(), [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$observedProbeDuration) -or
            [Math]::Abs($observedProbeDuration - [double]$evidence.gif.actualDurationSeconds) -gt 0.011) {
            throw "Workshop showcase '$showcaseId' GIF probe result does not match the recorded reviewed media."
        }
    }
    $evidenceItem = Get-Item -LiteralPath $evidencePath
    $sourceSegmentEvidence = @()
    if ($isAssembly) {
        $sourceSegmentEvidence = @($evidence.sourceSegments | ForEach-Object {
            $segmentPath = [IO.Path]::GetFullPath((Join-Path $evidenceDirectory ([string]$_.capturePath).Replace('/', '\')))
            [pscustomobject][ordered]@{
                path = $segmentPath
                bytes = (Get-Item -LiteralPath $segmentPath).Length
                sha256 = (Get-FileHash -LiteralPath $segmentPath -Algorithm SHA256).Hash
            }
        })
    }
    return [pscustomobject][ordered]@{
        showcaseId = $showcaseId
        outputs = $outputs
        sourceSegments = $sourceSegmentEvidence
        provenance = [pscustomobject][ordered]@{
            path = $evidencePath
            bytes = [long]$evidenceItem.Length
            sha256 = (Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash
            processId = [int]$evidence.processId
            processStartUtc = [string]$evidence.processStartUtc
            runId = [string]$evidence.runId
            capturedUtc = [string]$evidence.capturedUtc
        }
    }
}

function Write-JsonUtf8 {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][object]$Value)
    $json = $Value | ConvertTo-Json -Depth 12
    [IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}

function Resolve-ExistingWorkshopIdentity {
    param(
        [object]$DeclaredId,
        [Parameter(Mandatory)][string]$StateRoot,
        [string]$SourceIdentityPath
    )
    $identities = [System.Collections.Generic.List[System.UInt64]]::new()
    if ($null -ne $DeclaredId) {
        [System.UInt64]$parsed = 0
        if (-not [ulong]::TryParse([string]$DeclaredId, [ref]$parsed) -or $parsed -eq 0) {
            throw 'The release descriptor Workshop identity is invalid.'
        }
        $identities.Add($parsed)
    }
    if (-not [string]::IsNullOrWhiteSpace($SourceIdentityPath)) {
        if (-not (Test-Path -LiteralPath $SourceIdentityPath -PathType Leaf)) {
            if ($null -ne $DeclaredId) {
                throw 'The checked-in About/PublishedFileId.txt identity is missing for this published mod.'
            }
        }
        else {
            [System.UInt64]$parsed = 0
            if (-not [ulong]::TryParse((Get-Content -LiteralPath $SourceIdentityPath -Raw).Trim(), [ref]$parsed) -or $parsed -eq 0) {
                throw 'The checked-in About/PublishedFileId.txt identity is invalid.'
            }
            $identities.Add($parsed)
        }
    }
    $identityPath = Join-Path $StateRoot 'PublishedFileId.txt'
    if (Test-Path -LiteralPath $identityPath -PathType Leaf) {
        [System.UInt64]$parsed = 0
        if (-not [ulong]::TryParse((Get-Content -LiteralPath $identityPath -Raw).Trim(), [ref]$parsed) -or $parsed -eq 0) {
            throw 'The durable Workshop identity is invalid.'
        }
        $identities.Add($parsed)
    }
    $publicationRoot = Join-Path $StateRoot 'publication'
    foreach ($receiptPath in @(Get-ChildItem -LiteralPath $publicationRoot -Recurse -Filter publication-receipt.json -File -ErrorAction SilentlyContinue)) {
        try { $receipt = Get-Content -LiteralPath $receiptPath.FullName -Raw -Encoding UTF8 | ConvertFrom-Json }
        catch { throw "A Workshop publication receipt is unreadable: $($receiptPath.FullName)" }
        [System.UInt64]$parsed = 0
        if ([string]$receipt.schema -cne 'ImmersiveChefs/WorkshopPublicationReceipt/v1' -or
            -not [ulong]::TryParse([string]$receipt.publishedFileId, [ref]$parsed) -or $parsed -eq 0) {
            throw "A Workshop publication receipt has an invalid identity: $($receiptPath.FullName)"
        }
        $identities.Add($parsed)
    }
    $distinct = @($identities | Sort-Object -Unique)
    if ($distinct.Count -gt 1) { throw 'Workshop identity evidence conflicts; refusing to stage a release.' }
    return $(if ($distinct.Count -eq 1) { [System.UInt64]$distinct[0] } else { [System.UInt64]0 })
}

function Test-WorkshopPreviewProvenance {
    param(
        [Parameter(Mandatory)][object[]]$CurrentPreviews,
        [Parameter(Mandatory)][object[]]$ProvenancePreviews
    )
    if ($CurrentPreviews.Count -ne $ProvenancePreviews.Count) { return $false }
    for ($index = 0; $index -lt $CurrentPreviews.Count; $index++) {
        $current = $CurrentPreviews[$index]
        $provenance = $ProvenancePreviews[$index]
        if ([string]$provenance.token -cne [string]$current.token -or
            [int]$provenance.remoteIndex -ne $index -or
            [string]::IsNullOrWhiteSpace([string]$provenance.localPath) -or
            [IO.Path]::GetFileName([string]$provenance.localPath) -cne [IO.Path]::GetFileName([string]$current.path) -or
            [string]$provenance.localSha256 -cne [string]$current.sha256 -or
            [string]$provenance.remoteSha256 -cne [string]$current.sha256 -or
            [string]$provenance.remoteType -cne 'k_EItemPreviewType_Image') {
            return $false
        }
    }
    return $true
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releaseRoot = Join-Path $repositoryRoot 'mods\ImmersiveChefs\Release'
$descriptorPath = Join-Path $releaseRoot 'release.json'
if (-not (Test-Path -LiteralPath $descriptorPath -PathType Leaf)) {
    Exit-InvalidInput "Release descriptor does not exist: $descriptorPath"
}

$dirty = @(& git -C $repositoryRoot status --porcelain)
if ($LASTEXITCODE -ne 0) { throw 'git status --porcelain failed.' }
if ($dirty.Count -ne 0) {
    Exit-InvalidInput 'A publishable release requires a clean committed repository.'
}

$revision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $revision -notmatch '^[0-9a-f]{40}$') { throw 'Could not resolve the source revision.' }
$release = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
if ($release.schema -cne 'ImmersiveChefs/Release/v1' -or
    $release.packageId -cne 'fumblesneeze.immersivechefs' -or
    $release.rimWorldVersion -cne '1.6' -or
    [int]$release.steamAppId -ne 294100) {
    Exit-InvalidInput 'The release descriptor identity is invalid.'
}
$resolvedRimWorldPath = [IO.Path]::GetFullPath($RimWorldPath)
$versionPath = Join-Path $resolvedRimWorldPath 'Version.txt'
$managedAssemblyPath = Join-Path $resolvedRimWorldPath 'RimWorldWin64_Data\Managed\Assembly-CSharp.dll'
$steamManifestPath = Join-Path ([IO.DirectoryInfo]::new($resolvedRimWorldPath).Parent.Parent.FullName) 'appmanifest_294100.acf'
if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $managedAssemblyPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $steamManifestPath -PathType Leaf)) {
    Exit-InvalidInput 'The exact RimWorld release inputs are incomplete.'
}
$rawRimWorldBuild = (Get-Content -LiteralPath $versionPath -Raw).Trim()
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($managedAssemblyPath).Version
$actualRimWorldBuild = '{0}.{1}.{2} rev{3}' -f `
    $assemblyVersion.Major,
    $assemblyVersion.Minor,
    ($assemblyVersion.Build - 4805),
    [int]($assemblyVersion.Revision * 2 / 60)
$actualManagedHash = (Get-FileHash -LiteralPath $managedAssemblyPath -Algorithm SHA256).Hash
$steamManifestText = Get-Content -LiteralPath $steamManifestPath -Raw
$buildMatch = [regex]::Match($steamManifestText, '"buildid"\s+"(?<id>\d+)"')
if ($rawRimWorldBuild -cne [string]$release.rimWorldBuild -or
    $actualRimWorldBuild -cne [string]$release.rimWorldRuntimeBuild -or
    -not $buildMatch.Success -or
    $buildMatch.Groups['id'].Value -cne [string]$release.steamBuildId -or
    $actualManagedHash -cne [string]$release.managedAssemblySha256) {
    Exit-InvalidInput 'The installed RimWorld build does not match the pinned release inputs.'
}
$releaseStateRoot = Join-Path $repositoryRoot 'artifacts\Releases\fumblesneeze.immersivechefs'
$sourcePublishedFileIdPath = Join-Path $repositoryRoot 'mods\ImmersiveChefs\About\PublishedFileId.txt'
$effectivePublishedFileId = Resolve-ExistingWorkshopIdentity `
    -DeclaredId $release.publishedFileId `
    -StateRoot $releaseStateRoot `
    -SourceIdentityPath $sourcePublishedFileIdPath
if ($effectivePublishedFileId -eq 0 -and -not [bool]$release.allowFirstPublication) {
    Exit-InvalidInput 'The descriptor has no Workshop item and does not allow first publication.'
}
$changeNote = [string]$release.changeNote
$previousChangeNote = [string]$release.previousChangeNote
if ($effectivePublishedFileId -ne 0 -and
    ([string]::IsNullOrWhiteSpace($changeNote) -or
     $changeNote.Length -lt 24 -or
     $changeNote.Length -gt 8000 -or
     $changeNote.Trim() -match '^(update|updated|fix|fixes|bug fixes|various changes|miscellaneous changes)[.!]?$' -or
     (-not [string]::IsNullOrWhiteSpace($previousChangeNote) -and
      $changeNote.Trim() -ceq $previousChangeNote.Trim()))) {
    Exit-InvalidInput 'An existing Workshop item requires one new, specific player-facing change note.'
}
if ($effectivePublishedFileId -eq 0 -and $changeNote.Length -gt 8000) {
    Exit-InvalidInput 'The optional first-publication change note exceeds Steam limits.'
}

if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    $runId = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ', [Globalization.CultureInfo]::InvariantCulture)
    $ArtifactsPath = Join-Path $repositoryRoot "artifacts\Releases\fumblesneeze.immersivechefs\$runId"
}
$runRoot = [IO.Path]::GetFullPath($ArtifactsPath)
if (Test-Path -LiteralPath $runRoot) { Exit-InvalidInput "Release output already exists: $runRoot" }

$isolatedArtifactsRoot = Join-Path $runRoot 'build'
$buildOutput = @(& dotnet build (Join-Path $repositoryRoot 'mods\ImmersiveChefs\ImmersiveChefs.csproj') -c Release --nologo `
    "-p:RimWorldPath=$resolvedRimWorldPath" `
    "-p:ArtifactsRoot=$isolatedArtifactsRoot\" `
    '-p:UseArtifactsOutput=true' `
    "-p:ArtifactsPath=$(Join-Path $isolatedArtifactsRoot 'sdk')\" 2>&1)
if ($LASTEXITCODE -ne 0) { throw "Release build failed.`n$($buildOutput -join [Environment]::NewLine)" }

$builtRoot = Join-Path $isolatedArtifactsRoot 'Mods\fumblesneeze.immersivechefs'
$packageRoot = Join-Path $runRoot 'package'
$presentationRoot = Join-Path $runRoot 'presentation'
$evidenceRoot = Join-Path $runRoot 'evidence'
$null = New-Item -ItemType Directory -Path $packageRoot,$presentationRoot,$evidenceRoot
$null = New-Item -ItemType Directory -Path (Join-Path $packageRoot 'About'),(Join-Path $packageRoot '1.6\Assemblies') -Force

Copy-Item -LiteralPath (Join-Path $builtRoot 'About\About.xml') -Destination (Join-Path $packageRoot 'About\About.xml')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'mods\ImmersiveChefs\About\Preview.png') -Destination (Join-Path $packageRoot 'About\Preview.png')
if ($effectivePublishedFileId -ne 0) {
    Copy-Item -LiteralPath $sourcePublishedFileIdPath -Destination (Join-Path $packageRoot 'About\PublishedFileId.txt')
}
Copy-Item -LiteralPath (Join-Path $builtRoot '1.6\Assemblies\ImmersiveChefs.dll') -Destination (Join-Path $packageRoot '1.6\Assemblies\ImmersiveChefs.dll')
foreach ($directory in @('Defs', 'Languages', 'Patches', 'Textures')) {
    Copy-Item -LiteralPath (Join-Path $builtRoot "1.6\$directory") -Destination (Join-Path $packageRoot '1.6') -Recurse
}

$probePatch = Join-Path $packageRoot '1.6\Patches\ImmersiveChefsIntegrationProbePatch.xml'
if (Test-Path -LiteralPath $probePatch) { Remove-Item -LiteralPath $probePatch -Force }
if (Test-Path -LiteralPath (Join-Path $packageRoot '1.6\Assemblies\ImmersiveChefs.pdb')) {
    throw 'ImmersiveChefs.pdb entered the positive-allowlist package.'
}

$descriptionSource = Join-Path $releaseRoot ([string]$release.description)
$descriptionProvenanceSource = [IO.Path]::ChangeExtension($descriptionSource, '.provenance.json')
$previewSource = Join-Path $releaseRoot ([string]$release.preview)
Copy-Item -LiteralPath $descriptionSource -Destination (Join-Path $presentationRoot 'description.bbcode')
if (Test-Path -LiteralPath $descriptionProvenanceSource -PathType Leaf) {
    Copy-Item -LiteralPath $descriptionProvenanceSource -Destination (Join-Path $presentationRoot 'description.provenance.json')
}
Copy-Item -LiteralPath $previewSource -Destination (Join-Path $presentationRoot 'preview-main.png')
$presentationDefinitionPath = Join-Path $releaseRoot 'workshop\presentation.json'
$presentationDefinition = Get-Content -LiteralPath $presentationDefinitionPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ([string]$presentationDefinition.schema -cne 'ImmersiveChefs/WorkshopPresentation/v1') {
    Exit-InvalidInput 'The Workshop presentation definition is invalid.'
}
$additionalPreviewRoot = Join-Path $presentationRoot 'additional-previews'
$null = New-Item -ItemType Directory -Path $additionalPreviewRoot
$showcaseDefinitionPath = Join-Path $releaseRoot 'workshop\showcases.json'
$showcaseDefinition = Get-Content -LiteralPath $showcaseDefinitionPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ([string]$showcaseDefinition.schema -cne 'ImmersiveChefs/WorkshopShowcases/v1' -or
    [string]$showcaseDefinition.publicationStatus -cne 'human-deferred' -or
    @($showcaseDefinition.showcases).Count -ne 5 -or
    @($showcaseDefinition.showcases.id | Sort-Object -Unique).Count -ne 5) {
    Exit-InvalidInput 'The Workshop showcase definition is invalid.'
}
$declaredShowcasePreviews = @($showcaseDefinition.showcases | ForEach-Object {
    $showcase = $_
    $formats = @($showcase.formats | ForEach-Object { [string]$_ })
    $carouselFormat = [string]$showcase.carousel.format
    $relativePath = if ($carouselFormat -ceq 'gif') { [string]$showcase.outputs.gif } else { [string]$showcase.outputs.screenshot }
    if ([string]$showcase.id -notmatch '^[a-z0-9-]{1,48}$' -or
        [string]$showcase.carousel.token -notmatch '^[a-z0-9-]{1,32}$' -or
        [string]$showcase.designRecord -cne "designs/$($showcase.id).md" -or
        [int]$showcase.carousel.slot -lt 5 -or [int]$showcase.carousel.slot -gt 9 -or
        $formats -cnotcontains 'screenshot' -or
        $carouselFormat -notin @('gif', 'screenshot') -or
        ($carouselFormat -ceq 'gif' -and $formats -cnotcontains 'gif') -or
        [int]$showcase.crop.width -lt 1 -or [int]$showcase.crop.width -gt 16384 -or
        [int]$showcase.crop.height -lt 1 -or [int]$showcase.crop.height -gt 16384 -or
        @($showcase.beats).Count -lt 1 -or
        @($showcase.requiredPackageIds | Where-Object { [string]$_ -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$' }).Count -ne 0 -or
        [IO.Path]::IsPathRooted($relativePath) -or
        $relativePath -match '(^|[\\/])\.\.([\\/]|$)' -or
        ($carouselFormat -ceq 'gif' -and [IO.Path]::GetExtension($relativePath) -cne '.gif') -or
        ($carouselFormat -ceq 'screenshot' -and [IO.Path]::GetExtension($relativePath) -cne '.png')) {
        Exit-InvalidInput "Workshop showcase '$($showcase.id)' has an invalid capture/publication contract."
    }
    [pscustomobject][ordered]@{
        token = [string]$showcase.carousel.token
        slot = [int]$showcase.carousel.slot
        relativePath = $relativePath
        screenshotRelativePath = [string]$showcase.outputs.screenshot
        gifRelativePath = if ($formats -ccontains 'gif') { [string]$showcase.outputs.gif } else { $null }
        alt = [string]$showcase.title
        showcaseId = [string]$showcase.id
        format = $carouselFormat
    }
})
if (@($declaredShowcasePreviews.slot | Sort-Object) -join ',' -cne '5,6,7,8,9' -or
    @($declaredShowcasePreviews.token | Sort-Object -Unique).Count -ne 5) {
    Exit-InvalidInput 'The five Workshop showcases must own exact unique carousel slots 5 through 9.'
}
$showcasePreviews = @()
$showcaseEvidence = @()
$showcaseDesignEvidence = @()
$cardTokens = @($presentationDefinition.carouselCards | ForEach-Object { [string]$_ })
if ($cardTokens.Count -ne 6 -or @($cardTokens | Sort-Object -Unique).Count -ne 6) {
    Exit-InvalidInput 'The Workshop presentation must select exactly six unique illustrated carousel cards.'
}
$selectedCards = @($cardTokens | ForEach-Object {
    $token = $_
    $matches = @($presentationDefinition.cards | Where-Object { [string]$_.token -ceq $token })
    if ($matches.Count -ne 1) { Exit-InvalidInput "Illustrated carousel card '$token' is missing or duplicated." }
    $matches[0]
})
$previewDeclarations = @()
$previewDeclarations += @($selectedCards | ForEach-Object {
    [pscustomobject][ordered]@{ token = [string]$_.token; slot = [array]::IndexOf($cardTokens, [string]$_.token); relativePath = [string]$_.path; alt = [string]$_.alt; showcaseId = $null; format = 'screenshot' }
})
$previewDeclarations += $showcasePreviews
$additionalPreviews = @($previewDeclarations | Sort-Object slot | ForEach-Object {
    $token = [string]$_.token
    if ($token -notmatch '^[a-z0-9-]{1,32}$') { Exit-InvalidInput "Workshop preview token is invalid: $token" }
    $sourcePath = Join-Path $releaseRoot ('workshop\' + ([string]$_.relativePath).Replace('/', '\'))
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { Exit-InvalidInput "Workshop preview is missing: $sourcePath" }
    $destinationPath = Join-Path $additionalPreviewRoot ([IO.Path]::GetFileName($sourcePath))
    Copy-Item -LiteralPath $sourcePath -Destination $destinationPath
    $item = Get-Item -LiteralPath $destinationPath
    if ($item.Length -le 0 -or $item.Length -ge 1MB) { Exit-InvalidInput "Workshop preview must be nonempty and under Steam's 1 MiB limit: $token" }
    [pscustomobject][ordered]@{
        token = $token
        path = $destinationPath
        bytes = [long]$item.Length
        sha256 = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash
        width = if ([string]$_.format -ceq 'screenshot' -and $null -eq $_.showcaseId) { 1164 } else { $null }
        height = if ([string]$_.format -ceq 'screenshot' -and $null -eq $_.showcaseId) { 655 } else { $null }
        alt = [string]$_.alt
        showcaseId = $_.showcaseId
        format = [string]$_.format
    }
})
if ($additionalPreviews.Count -ne 6 -or @($additionalPreviews.token | Sort-Object -Unique).Count -ne 6 -or
    @($additionalPreviews | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.showcaseId) }).Count -ne 0) {
    Exit-InvalidInput 'The human-deferred Workshop presentation must declare exactly six non-showcase additional previews.'
}

$files = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -File | Sort-Object FullName | ForEach-Object {
    [pscustomobject][ordered]@{
        path = Get-RelativePath -Root $packageRoot -Path $_.FullName
        bytes = [long]$_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
})
$canonical = ($files | ForEach-Object { '{0}|{1}|{2}' -f $_.path,$_.bytes,$_.sha256 }) -join "`n"
$candidateDigest = Get-Sha256Text $canonical
$manifestPath = Join-Path $evidenceRoot 'package-files.json'
Write-JsonUtf8 -Path $manifestPath -Value ([pscustomobject][ordered]@{
    schema = 'ImmersiveChefs/PackageFiles/v1'
    packageId = [string]$release.packageId
    sourceRevision = $revision
    candidateSha256 = $candidateDigest
    files = $files
})

$descriptionPath = Join-Path $presentationRoot 'description.bbcode'
$previewPath = Join-Path $presentationRoot 'preview-main.png'
$descriptionText = Get-Content -LiteralPath $descriptionPath -Raw -Encoding UTF8
$hasUnresolvedImageTokens = $descriptionText -match '\{\{image:[^}]+\}\}'
$resolvedImageCount = [regex]::Matches($descriptionText, '\[img\]https://images\.steamusercontent\.com/.+?\[/img\]').Count
$presentationResolved = $resolvedImageCount -eq 6 -and -not $hasUnresolvedImageTokens
$descriptionProvenancePath = Join-Path $presentationRoot 'description.provenance.json'
$descriptionProvenance = $null
if ($presentationResolved -and (Test-Path -LiteralPath $descriptionProvenancePath -PathType Leaf)) {
    $descriptionProvenance = Get-Content -LiteralPath $descriptionProvenancePath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ([string]$descriptionProvenance.schema -cne 'ImmersiveChefs/WorkshopDescriptionProvenance/v1' -or
        [string]$descriptionProvenance.publishedFileId -cne [string]$effectivePublishedFileId -or
        [string]$descriptionProvenance.descriptionSha256 -cne (Get-FileHash -LiteralPath $descriptionPath -Algorithm SHA256).Hash -or
        -not (Test-WorkshopPreviewProvenance -CurrentPreviews $additionalPreviews -ProvenancePreviews @($descriptionProvenance.previews))) {
        $presentationResolved = $false
        $descriptionProvenance = $null
    }
}
elseif ($presentationResolved) { $presentationResolved = $false }
$plan = [pscustomobject][ordered]@{
    schema = 'ImmersiveChefs/WorkshopPublicationPlan/v1'
    sourceRevision = $revision
    releaseDescriptorSha256 = (Get-FileHash -LiteralPath $descriptorPath -Algorithm SHA256).Hash
    steamAppId = 294100
    steamUserId = [string]$release.steamUserId
    publishedFileId = if ($effectivePublishedFileId -eq 0) { $null } else { [string]$effectivePublishedFileId }
    allowFirstPublication = [bool]$release.allowFirstPublication -and $effectivePublishedFileId -eq 0
    packageId = [string]$release.packageId
    title = [string]$release.title
    author = [string]$release.author
    rimWorldVersion = [string]$release.rimWorldVersion
    rimWorldBuild = [string]$release.rimWorldBuild
    rimWorldRuntimeBuild = [string]$release.rimWorldRuntimeBuild
    steamBuildId = [string]$release.steamBuildId
    managedAssemblySha256 = [string]$release.managedAssemblySha256
    visibility = if ($effectivePublishedFileId -eq 0) { 'Private' } else { [string]$release.visibility }
    tags = @($release.tags)
    requiredWorkshopItems = @($release.requiredWorkshopItems)
    previousChangeNote = if ($effectivePublishedFileId -eq 0) { $null } else { $previousChangeNote }
    changeNote = $changeNote
    packagePath = $packageRoot
    packageManifestPath = $manifestPath
    candidateSha256 = $candidateDigest
    packageFileCount = $files.Count
    packageBytes = [long](($files | Measure-Object bytes -Sum).Sum)
    generatedAtPublication = if ($effectivePublishedFileId -eq 0) { @('About\PublishedFileId.txt') } else { @() }
    descriptionPath = $descriptionPath
    descriptionBytes = (Get-Item -LiteralPath $descriptionPath).Length
    descriptionSha256 = (Get-FileHash -LiteralPath $descriptionPath -Algorithm SHA256).Hash
    presentationResolved = $presentationResolved
    hasUnresolvedImageTokens = $hasUnresolvedImageTokens
    descriptionProvenancePath = if ($presentationResolved) { $descriptionProvenancePath } else { $null }
    previewPublicationPlanSha256 = if ($presentationResolved) { [string]$descriptionProvenance.previewPublicationPlanSha256 } else { $null }
    resolvedPreviews = if ($presentationResolved) { @($descriptionProvenance.previews) } else { @() }
    previewPath = $previewPath
    previewBytes = (Get-Item -LiteralPath $previewPath).Length
    previewSha256 = (Get-FileHash -LiteralPath $previewPath -Algorithm SHA256).Hash
    additionalPreviews = $additionalPreviews
    showcasePublicationStatus = [string]$showcaseDefinition.publicationStatus
    showcaseEvidence = $showcaseEvidence
    showcaseDesignEvidence = $showcaseDesignEvidence
    mutatesSteam = $false
}
$planPath = Join-Path $evidenceRoot 'publication-plan.json'
Write-JsonUtf8 -Path $planPath -Value $plan

$result = [pscustomobject][ordered]@{
    status = 'dry-run'
    runRoot = $runRoot
    packagePath = $packageRoot
    publicationPlan = $planPath
    candidateSha256 = $candidateDigest
    sourceRevision = $revision
    fileCount = $files.Count
    bytes = $plan.packageBytes
    steamMutation = $false
}
if ($Output -eq 'json') { $result | ConvertTo-Json -Compress } else { $result | Format-List }
