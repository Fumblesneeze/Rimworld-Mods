<#
.SYNOPSIS
Publishes one explicitly confirmed, immutable Immersive Chefs release plan to Steam Workshop.

.DESCRIPTION
Revalidates the clean committed source revision and every staged byte, launches one isolated
RimWorld Dev Gateway process, compiles the checked-in Steamworks publisher, creates or updates
the item, queries its remote metadata, subscribes/downloads it, verifies the installed bytes,
and writes a token-free receipt. The exact confirmation is intentionally awkward and is accepted
only together with the reviewed publication-plan SHA-256.

.EXAMPLE
.\scripts\Invoke-ImmersiveChefsWorkshopRelease.ps1 `
  -PublicationPlan C:\...\publication-plan.json `
  -PublicationPlanSha256 ABCD... `
  -Confirmation 'publish immersive chefs to steam workshop'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PublicationPlan,
    [Parameter(Mandatory)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$PublicationPlanSha256,
    [Parameter(Mandatory)][string]$Confirmation,
    [string]$RimWorldPath = 'F:\Steam\steamapps\common\RimWorld',
    [string]$SteamModContentFolder = 'F:\Steam\steamapps\workshop\content\294100',
    [ValidateRange(120, 1800)][int]$TimeoutSeconds = 900,
    [switch]$PreviewSyncOnly,
    [ValidateSet('table', 'json')][string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$exactConfirmation = 'publish immersive chefs to steam workshop'

function Exit-InvalidInput([string]$Message) {
    [Console]::Error.WriteLine($Message)
    exit 2
}

function Test-ReleaseSourceRevisionAllowed([string]$CurrentRevision, [string]$PlanRevision, [bool]$ReconcileOnly, [System.UInt64]$PublishedFileId, [bool]$PlanIsAncestor) {
    return $CurrentRevision -ceq $PlanRevision -or ($ReconcileOnly -and $PublishedFileId -ne 0 -and $PlanIsAncestor)
}

function Test-ReconciliationStateIdentity([string]$State, [System.UInt64]$StateItemId, [System.UInt64]$PublishedFileId) {
    $reconciliationStates = @('submit-admitted', 'submitted', 'submit-indeterminate', 'dependency-indeterminate', 'succeeded')
    return $reconciliationStates -cnotcontains $State -or
        ($StateItemId -ne 0 -and $PublishedFileId -ne 0 -and $StateItemId -eq $PublishedFileId)
}

function Get-ExpectedRemoteVisibility([string]$PlanVisibility) {
    switch ($PlanVisibility) {
        'Private' { return 'k_ERemoteStoragePublishedFileVisibilityPrivate' }
        'Public' { return 'k_ERemoteStoragePublishedFileVisibilityPublic' }
        'Unlisted' { return 'k_ERemoteStoragePublishedFileVisibilityUnlisted' }
        default { throw "Unsupported reviewed Workshop visibility: $PlanVisibility" }
    }
}

function Test-PresentationPreviewStateAllowsPublication(
    [string]$State,
    [string]$StatePlan,
    [System.UInt64]$StateItemId,
    [string]$ExpectedPreviewPlan,
    [System.UInt64]$ExpectedItemId) {
    return $State -ceq 'succeeded' -and $StatePlan -ceq $ExpectedPreviewPlan -and
        $StateItemId -ne 0 -and $StateItemId -eq $ExpectedItemId
}

function Test-WorkshopPresentationAllowsPublication(
    [bool]$PresentationResolved,
    [bool]$HasUnresolvedImageTokens,
    [System.UInt64]$PlannedPublishedFileId,
    [bool]$AllowFirstPublication,
    [string]$Visibility) {
    return $PresentationResolved -or
        ($PlannedPublishedFileId -eq 0 -and $AllowFirstPublication -and
         $Visibility -ceq 'Private' -and -not $HasUnresolvedImageTokens)
}

function Test-RemoteWorkshopPreviewsMatchResolvedDescription(
    [object[]]$RemotePreviews,
    [object[]]$ResolvedPreviews) {
    if ($RemotePreviews.Count -ne $ResolvedPreviews.Count) { return $false }
    for ($index = 0; $index -lt $ResolvedPreviews.Count; $index++) {
        $remote = $RemotePreviews[$index]
        $resolved = $ResolvedPreviews[$index]
        if ([int]$remote.Index -ne $index -or [int]$resolved.remoteIndex -ne $index -or
            [string]$remote.Url -cne [string]$resolved.remoteUrl -or
            [string]$remote.Type -cne [string]$resolved.remoteType) {
            return $false
        }
    }
    return $true
}

function Read-Json([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "JSON file does not exist: $Path" }
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Write-JsonUtf8([string]$Path, [object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20
    [IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}

function Write-TextAtomically([string]$Path, [string]$Value) {
    $temporary = $Path + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    try {
        [IO.File]::WriteAllText($temporary, $Value, [Text.UTF8Encoding]::new($false))
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            $backup = $Path + '.' + [guid]::NewGuid().ToString('N') + '.bak'
            try { [IO.File]::Replace($temporary, $Path, $backup) }
            finally { if (Test-Path -LiteralPath $backup -PathType Leaf) { Remove-Item -LiteralPath $backup -Force } }
        } else {
            [IO.File]::Move($temporary, $Path)
        }
    } finally {
        if (Test-Path -LiteralPath $temporary -PathType Leaf) { Remove-Item -LiteralPath $temporary -Force }
    }
}

function Save-RemoteFile([string]$Uri, [string]$Path) {
    $request = [Net.HttpWebRequest]::CreateHttp($Uri)
    $request.Timeout = 30000
    $request.ReadWriteTimeout = 30000
    $response = $request.GetResponse()
    try {
        $input = $response.GetResponseStream()
        try {
            $output = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
            try { $input.CopyTo($output) }
            finally { $output.Dispose() }
        }
        finally { $input.Dispose() }
    }
    finally { $response.Dispose() }
}

function ConvertFrom-WorkshopChangeHistoryHtml([string]$Html, [string]$Uri) {
    if ([Text.Encoding]::UTF8.GetByteCount($Html) -gt 2MB) { throw 'The Steam change-history response exceeds the reviewed limit.' }
    $matches = [regex]::Matches(
        $Html,
        '<div class="changelog headline">(?<headline>.*?)</div>.*?<p id="(?<id>\d+)">(?<note>.*?)</p>',
        [Text.RegularExpressions.RegexOptions]::Singleline -bor [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    $notes = [System.Collections.Generic.List[object]]::new()
    foreach ($match in $matches) {
        $headline = [Net.WebUtility]::HtmlDecode(([regex]::Replace($match.Groups['headline'].Value, '<[^>]+>', ' '))).Trim()
        $note = [Net.WebUtility]::HtmlDecode(([regex]::Replace($match.Groups['note'].Value, '<br\s*/?>', "`n", [Text.RegularExpressions.RegexOptions]::IgnoreCase)))
        $note = [Net.WebUtility]::HtmlDecode(([regex]::Replace($note, '<[^>]+>', ''))).Trim()
        if ($note.Length -gt 8000) { throw 'Steam returned an over-limit Workshop change note.' }
        $notes.Add([pscustomobject][ordered]@{
            id = $match.Groups['id'].Value
            headline = $headline
            note = $note
            url = $uri + '?snr=1_5_9__408#' + $match.Groups['id'].Value
        })
        if ($notes.Count -ge 100) { break }
    }
    return @($notes)
}

function Get-WorkshopChangeNotes([System.UInt64]$PublishedFileId) {
    if ($PublishedFileId -eq 0) { return @() }
    $uri = "https://steamcommunity.com/sharedfiles/filedetails/changelog/$PublishedFileId"
    $request = [Net.HttpWebRequest]::CreateHttp($uri)
    $request.Timeout = 30000
    $request.ReadWriteTimeout = 30000
    $request.UserAgent = 'ImmersiveChefsReleaseVerifier/1.0'
    $response = $request.GetResponse()
    try {
        if ([long]$response.ContentLength -gt 2MB) { throw 'The Steam change-history response exceeds the reviewed limit.' }
        $stream = $response.GetResponseStream()
        try {
            $reader = [IO.StreamReader]::new($stream, [Text.Encoding]::UTF8, $true, 4096, $true)
            try { $html = $reader.ReadToEnd() }
            finally { $reader.Dispose() }
        }
        finally { $stream.Dispose() }
    }
    finally { $response.Dispose() }
    return @(ConvertFrom-WorkshopChangeHistoryHtml -Html $html -Uri $uri)
}

function Test-WorkshopChangeHistoryRequired([bool]$FirstPublicationPlan, [bool]$PreviewOnly) {
    return -not $FirstPublicationPlan -and -not $PreviewOnly
}

function Test-WorkshopChangeNotePreflight(
    [object[]]$RemoteNotes,
    [string]$ExpectedPreviousNote,
    [string]$NewNote) {
    if ([string]::IsNullOrWhiteSpace($NewNote)) { return $false }
    if ([string]::IsNullOrWhiteSpace($ExpectedPreviousNote)) {
        return $RemoteNotes.Count -eq 0
    }
    if ($RemoteNotes.Count -eq 0) { return $false }
    return [string]$RemoteNotes[0].note -ceq $ExpectedPreviousNote.Trim() -and
        @($RemoteNotes | Where-Object { [string]$_.note -ceq $NewNote.Trim() }).Count -eq 0
}

function Test-WorkshopShowcasePlanEvidence([string]$PublicationStatus, [object[]]$Evidence, [object[]]$DesignEvidence, [object[]]$AdditionalPreviews) {
    if ($PublicationStatus -cne 'human-deferred') { return $false }
    $cardPreviews = @($AdditionalPreviews | Where-Object { [string]::IsNullOrWhiteSpace([string]$_.showcaseId) })
    $expectedTokens = @('kitchenware','teamwork','dishwashing','meals','colony','compatibility')
    if ($Evidence.Count -ne 0 -or $DesignEvidence.Count -ne 0 -or
        $AdditionalPreviews.Count -ne 6 -or $cardPreviews.Count -ne 6) { return $false }
    for ($index = 0; $index -lt $expectedTokens.Count; $index++) {
        if ([string]$cardPreviews[$index].token -cne $expectedTokens[$index] -or
            [string]$cardPreviews[$index].format -cne 'screenshot') { return $false }
    }
    return $true
}

function Stop-RetainedProcess([Diagnostics.Process]$Process, [int]$GraceMilliseconds = 15000) {
    if ($null -eq $Process) { return $false }
    $Process.Refresh()
    if ($Process.HasExited) { return $false }
    $null = $Process.CloseMainWindow()
    if (-not $Process.WaitForExit($GraceMilliseconds)) {
        $Process.Kill()
        $Process.WaitForExit()
        return $true
    }
    return $false
}

function Assert-RetainedProcessIdentity([Diagnostics.Process]$Process, [datetimeoffset]$ExpectedStartUtc) {
    if ($null -eq $Process) { throw 'The retained publisher process is missing.' }
    $Process.Refresh()
    if ($Process.HasExited) { return }
    $actual = [datetimeoffset]::new($Process.StartTime.ToUniversalTime(), [timespan]::Zero)
    if ($actual.ToUniversalTime().Ticks -ne $ExpectedStartUtc.ToUniversalTime().Ticks) {
        throw "The retained publisher PID was reused by a different process (expected $($ExpectedStartUtc.ToString('O')); actual $($actual.ToString('O')))."
    }
}

function Get-ExactProcessStartUtcFromManifest([string]$ManifestPath) {
    $raw = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8
    $match = [regex]::Match($raw, '"processStartUtc"\s*:\s*"(?<value>[^"]+)"', [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $match.Success) { throw 'The Gateway manifest has no exact process-start timestamp.' }
    [datetimeoffset]$parsed = [datetimeoffset]::MinValue
    if (-not [datetimeoffset]::TryParseExact(
        $match.Groups['value'].Value,
        'O',
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal -bor [Globalization.DateTimeStyles]::AdjustToUniversal,
        [ref]$parsed)) {
        throw 'The Gateway manifest process-start timestamp is not exact round-trip UTC.'
    }
    return $parsed
}

function Get-PublisherProcessLease([string]$GatewayRoot, [string]$ProcessLeasePath) {
    $leaseFiles = [System.Collections.Generic.List[IO.FileInfo]]::new()
    if (Test-Path -LiteralPath $ProcessLeasePath -PathType Leaf) {
        $leaseFiles.Add([IO.FileInfo]::new([IO.Path]::GetFullPath($ProcessLeasePath)))
    } else {
        foreach ($manifestFile in @(Get-ChildItem -LiteralPath $GatewayRoot -Recurse -Filter 'current.json' -File -ErrorAction SilentlyContinue)) {
            $leaseFiles.Add($manifestFile)
        }
    }
    if ($leaseFiles.Count -eq 0) { return $null }
    if ($leaseFiles.Count -ne 1) { throw 'More than one Gateway process lease appeared for this release.' }

    $manifest = Read-Json $leaseFiles[0].FullName
    if ($leaseFiles[0].Name -cne 'current.json' -and
        [string]$manifest.schema -cne 'RimWorldDevGateway/ProcessLease/v1') {
        throw 'The Gateway launcher process lease has the wrong schema.'
    }
    $processId = [int]$manifest.processId
    $processStartUtc = Get-ExactProcessStartUtcFromManifest -ManifestPath $leaseFiles[0].FullName
    $process = Get-Process -Id $processId -ErrorAction Stop
    Assert-RetainedProcessIdentity -Process $process -ExpectedStartUtc $processStartUtc
    return [pscustomobject][ordered]@{
        ManifestPath = $leaseFiles[0].FullName
        Process = $process
        ProcessId = $processId
        ProcessStartUtc = $processStartUtc
    }
}

function Get-RelativePath([string]$Root, [string]$Path) {
    $rootUri = [Uri]::new($Root.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar)
    return [Uri]::UnescapeDataString($rootUri.MakeRelativeUri([Uri]::new($Path)).ToString()).Replace('/', '\')
}

function Invoke-GatewayClient {
    param(
        [Parameter(Mandatory)][string]$Client,
        [Parameter(Mandatory)][string]$Manifest,
        [Parameter(Mandatory)][int]$ProcessId,
        [Parameter(Mandatory)][string[]]$Arguments
    )
    $commandArguments = @($Arguments) + @('--manifest', $Manifest, '--pid', [string]$ProcessId, '-o', 'json')
    $lines = @(& $Client @commandArguments 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Gateway client failed ($LASTEXITCODE): $($lines -join [Environment]::NewLine)" }
    $text = ($lines -join [Environment]::NewLine).Trim()
    try { return $text | ConvertFrom-Json }
    catch { throw "Gateway client returned invalid JSON: $text" }
}

function Invoke-WorkshopAutomation {
    param(
        [string]$Client,
        [string]$Manifest,
        [int]$ProcessId,
        [hashtable]$Arguments
    )
    $argumentsJson = $Arguments | ConvertTo-Json -Depth 8 -Compress
    $response = Invoke-GatewayClient -Client $Client -Manifest $Manifest -ProcessId $ProcessId `
        -Arguments @('run', 'release.workshop', '--arguments', $argumentsJson)
    if (-not [bool]$response.ok -or [string]$response.result.State -cne 'succeeded') {
        throw "Workshop automation dispatch failed: $($response | ConvertTo-Json -Depth 8 -Compress)"
    }
    try { return ([string]$response.result.Result | ConvertFrom-Json) }
    catch { throw "Workshop automation returned invalid result JSON: $($response.result.Result)" }
}

function Wait-WorkshopTerminal {
    param(
        [string]$Client,
        [string]$Manifest,
        [int]$ProcessId,
        [string]$PlanSha256,
        [datetime]$Deadline,
        [string[]]$TerminalStatuses
    )
    do {
        $state = Invoke-WorkshopAutomation -Client $Client -Manifest $Manifest -ProcessId $ProcessId `
            -Arguments @{ operation = 'status' }
        if ($state.PlanSha256 -and [string]$state.PlanSha256 -cne $PlanSha256) {
            throw 'Workshop status belongs to a different publication plan.'
        }
        if ($TerminalStatuses -ccontains [string]$state.Status) { return $state }
        Start-Sleep -Milliseconds 500
    } while ([datetime]::UtcNow -lt $Deadline)
    throw "Workshop operation did not finish before the bounded deadline; last stage: $($state.Stage)"
}

function Assert-StagedCandidate([object]$Plan) {
    $manifest = Read-Json ([string]$Plan.packageManifestPath)
    $root = [IO.Path]::GetFullPath([string]$Plan.packagePath)
    $expected = @{}
    foreach ($entry in @($manifest.files)) {
        $path = Join-Path $root ([string]$entry.path)
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Staged file is missing: $path" }
        $item = Get-Item -LiteralPath $path
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        if ([long]$item.Length -ne [long]$entry.bytes -or $hash -cne [string]$entry.sha256) {
            throw "Staged file differs from the reviewed package manifest: $path"
        }
        $expected[[string]$entry.path] = $true
    }
    $generated = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @($Plan.generatedAtPublication)) { $null = $generated.Add([string]$entry) }
    $actual = @(Get-ChildItem -LiteralPath $root -Recurse -File)
    foreach ($item in $actual) {
        $relative = Get-RelativePath -Root $root -Path $item.FullName
        if (-not $expected.ContainsKey($relative) -and -not $generated.Contains($relative)) {
            throw "Unexpected staged file: $($item.FullName)"
        }
    }
    $generatedPresent = @($actual | Where-Object {
        $relative = Get-RelativePath -Root $root -Path $_.FullName
        -not $expected.ContainsKey($relative) -and $generated.Contains($relative)
    }).Count
    if ($actual.Count -ne $expected.Count + $generatedPresent) {
        throw 'Staged package inventory changed after its reviewed plan was written.'
    }
}

if ($Confirmation -cne $exactConfirmation) { Exit-InvalidInput 'The exact Steam publication confirmation phrase is required.' }
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$planPath = [IO.Path]::GetFullPath($PublicationPlan)
$actualPlanHash = (Get-FileHash -LiteralPath $planPath -Algorithm SHA256).Hash
if ($actualPlanHash -cne $PublicationPlanSha256.ToUpperInvariant()) { Exit-InvalidInput 'The supplied publication-plan hash does not match the exact file.' }
$plan = Read-Json $planPath
$firstPublicationPlan = $null -eq $plan.publishedFileId -and [bool]$plan.allowFirstPublication
$changeHistoryRequired = Test-WorkshopChangeHistoryRequired -FirstPublicationPlan $firstPublicationPlan -PreviewOnly ([bool]$PreviewSyncOnly)
$expectedPlanVisibility = if ($null -eq $plan.publishedFileId) { 'Private' } else { 'Public' }
if ([string]$plan.schema -cne 'ImmersiveChefs/WorkshopPublicationPlan/v1' -or
    [string]$plan.packageId -cne 'fumblesneeze.immersivechefs' -or
    [string]$plan.title -cne 'Immersive Chefs' -or
    [string]$plan.author -cne 'Fumblesneeze' -or
    [string]$plan.rimWorldVersion -cne '1.6' -or
    [string]$plan.rimWorldBuild -cne '1.6.4871 rev590' -or
    [string]$plan.rimWorldRuntimeBuild -cne '1.6.4871 rev591' -or
    [string]$plan.steamBuildId -cne '23969874' -or
    [string]$plan.managedAssemblySha256 -cne '5CF1B5BE399D5B1C9C56CA72C9D35B4ECF307FEACF5859D04AC5A1AA5926356A' -or
    [int]$plan.steamAppId -ne 294100 -or
    [string]$plan.steamUserId -cne '76561198077136238' -or
    [string]$plan.visibility -cne $expectedPlanVisibility -or
    [bool]$plan.mutatesSteam) {
    Exit-InvalidInput 'The publication plan identity or dry-run contract is invalid.'
}
if (-not (Test-WorkshopShowcasePlanEvidence -PublicationStatus ([string]$plan.showcasePublicationStatus) -Evidence @($plan.showcaseEvidence) -DesignEvidence @($plan.showcaseDesignEvidence) -AdditionalPreviews @($plan.additionalPreviews))) {
    Exit-InvalidInput 'The publication plan showcase policy and presentation evidence are inconsistent.'
}
$dirty = @(& git -C $repositoryRoot status --porcelain)
if ($LASTEXITCODE -ne 0 -or $dirty.Count -ne 0) { Exit-InvalidInput 'Steam publication requires the exact clean committed release revision.' }
$revision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
$descriptorPath = Join-Path $repositoryRoot 'mods\ImmersiveChefs\Release\release.json'
if ((Get-FileHash -LiteralPath $descriptorPath -Algorithm SHA256).Hash -cne [string]$plan.releaseDescriptorSha256) {
    Exit-InvalidInput 'The release descriptor changed after the publication plan was reviewed.'
}
if ((Get-FileHash -LiteralPath ([string]$plan.descriptionPath) -Algorithm SHA256).Hash -cne [string]$plan.descriptionSha256 -or
    (Get-FileHash -LiteralPath ([string]$plan.previewPath) -Algorithm SHA256).Hash -cne [string]$plan.previewSha256) {
    Exit-InvalidInput 'The Workshop presentation changed after the publication plan was reviewed.'
}
foreach ($additionalPreview in @($plan.additionalPreviews)) {
    $path = [IO.Path]::GetFullPath([string]$additionalPreview.path)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
        (Get-Item -LiteralPath $path).Length -ne [long]$additionalPreview.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne [string]$additionalPreview.sha256) {
        Exit-InvalidInput "An additional Workshop preview changed after review: $path"
    }
}
foreach ($showcaseEvidence in @($plan.showcaseEvidence)) {
    foreach ($segment in @($showcaseEvidence.sourceSegments)) {
        $segmentPath = [IO.Path]::GetFullPath([string]$segment.path)
        if (-not (Test-Path -LiteralPath $segmentPath -PathType Leaf) -or
            (Get-Item -LiteralPath $segmentPath).Length -ne [long]$segment.bytes -or
            (Get-FileHash -LiteralPath $segmentPath -Algorithm SHA256).Hash -cne [string]$segment.sha256) {
            Exit-InvalidInput "A hard-cut Workshop showcase segment changed after review: $segmentPath"
        }
    }
    foreach ($output in @($showcaseEvidence.outputs)) {
        $path = [IO.Path]::GetFullPath([string]$output.path)
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
            (Get-Item -LiteralPath $path).Length -ne [long]$output.bytes -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne [string]$output.sha256) {
            Exit-InvalidInput "A declared Workshop showcase output changed after review: $path"
        }
    }
    $provenancePath = [IO.Path]::GetFullPath([string]$showcaseEvidence.provenance.path)
    if (-not (Test-Path -LiteralPath $provenancePath -PathType Leaf) -or
        (Get-Item -LiteralPath $provenancePath).Length -ne [long]$showcaseEvidence.provenance.bytes -or
        (Get-FileHash -LiteralPath $provenancePath -Algorithm SHA256).Hash -cne [string]$showcaseEvidence.provenance.sha256) {
        Exit-InvalidInput "Workshop showcase capture evidence changed after review: $provenancePath"
    }
}
foreach ($designEvidence in @($plan.showcaseDesignEvidence)) {
    $designPath = [IO.Path]::GetFullPath([string]$designEvidence.path)
    if (-not (Test-Path -LiteralPath $designPath -PathType Leaf) -or
        (Get-Item -LiteralPath $designPath).Length -ne [long]$designEvidence.bytes -or
        (Get-FileHash -LiteralPath $designPath -Algorithm SHA256).Hash -cne [string]$designEvidence.sha256) {
        Exit-InvalidInput "Workshop showcase design evidence changed after review: $designPath"
    }
}
$plannedPublishedFileId = if ($null -eq $plan.publishedFileId) { [System.UInt64]0 } else { [System.UInt64]$plan.publishedFileId }
$bootstrapPresentation = -not [bool]$plan.presentationResolved -and
    (Test-WorkshopPresentationAllowsPublication `
        -PresentationResolved ([bool]$plan.presentationResolved) `
        -HasUnresolvedImageTokens ([bool]$plan.hasUnresolvedImageTokens) `
        -PlannedPublishedFileId $plannedPublishedFileId `
        -AllowFirstPublication ([bool]$plan.allowFirstPublication) `
        -Visibility ([string]$plan.visibility))
if (-not $PreviewSyncOnly -and -not $bootstrapPresentation -and -not [bool]$plan.presentationResolved) {
    Exit-InvalidInput 'The final Workshop description has not been resolved against the reviewed Steam image inventory.'
}
$releaseStateRoot = Join-Path $repositoryRoot 'artifacts\Releases\fumblesneeze.immersivechefs'
$null = New-Item -ItemType Directory -Path $releaseStateRoot -Force
$leaseName = 'Local\Fumblesneeze.ImmersiveChefs.SteamWorkshopRelease'
$releaseLease = [Threading.Mutex]::new($false, $leaseName)
try { $leaseAcquired = $releaseLease.WaitOne(0) }
catch [Threading.AbandonedMutexException] { $leaseAcquired = $true }
if (-not $leaseAcquired) { Exit-InvalidInput 'Another Immersive Chefs Workshop release operation owns the exclusive lease.' }
$releaseIdentityPath = Join-Path $releaseStateRoot 'PublishedFileId.txt'
$repositoryIdentityPath = Join-Path $repositoryRoot 'mods\ImmersiveChefs\About\PublishedFileId.txt'
$packageIdentityPath = Join-Path ([string]$plan.packagePath) 'About\PublishedFileId.txt'
$statePath = Join-Path $releaseStateRoot 'publication-state.txt'
$presentationStatePath = Join-Path $releaseStateRoot 'presentation-preview-state.txt'
$publishedFileId = if ($null -eq $plan.publishedFileId) { [System.UInt64]0 } else { [System.UInt64]$plan.publishedFileId }
if (Test-Path -LiteralPath $releaseIdentityPath -PathType Leaf) {
    $persistedIdentity = (Get-Content -LiteralPath $releaseIdentityPath -Raw).Trim()
    [System.UInt64]$durableId = 0
    if (-not [ulong]::TryParse($persistedIdentity, [ref]$durableId) -or $durableId -eq 0 -or
        ($publishedFileId -ne 0 -and $publishedFileId -ne $durableId)) {
        Exit-InvalidInput 'The durable release Workshop identity is invalid or conflicts with the plan.'
    }
    $publishedFileId = $durableId
}
if ($publishedFileId -eq 0 -and (Test-Path -LiteralPath $packageIdentityPath -PathType Leaf)) {
    $persistedIdentity = (Get-Content -LiteralPath $packageIdentityPath -Raw).Trim()
    if (-not [ulong]::TryParse($persistedIdentity, [ref]$publishedFileId) -or $publishedFileId -eq 0) {
        Exit-InvalidInput 'The staged recovery identity is invalid.'
    }
}
if ($publishedFileId -ne 0) {
    if (-not (Test-Path -LiteralPath $repositoryIdentityPath -PathType Leaf) -or
        (Get-Content -LiteralPath $repositoryIdentityPath -Raw).Trim() -cne [string]$publishedFileId) {
        Exit-InvalidInput "The checked-in Workshop identity is missing or conflicts with the release plan: $repositoryIdentityPath"
    }
    if (-not (Test-Path -LiteralPath $releaseIdentityPath -PathType Leaf)) {
        [IO.File]::WriteAllText($releaseIdentityPath, [string]$publishedFileId, [Text.UTF8Encoding]::new($false))
    }
    foreach ($path in @($releaseIdentityPath, $packageIdentityPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
            (Get-Content -LiteralPath $path -Raw).Trim() -cne [string]$publishedFileId) {
            Exit-InvalidInput "Workshop identity is missing or inconsistent: $path"
        }
    }
}
if (-not $PreviewSyncOnly -and -not $bootstrapPresentation) {
    if (-not (Test-Path -LiteralPath $presentationStatePath -PathType Leaf)) { Exit-InvalidInput 'The reviewed Steam preview synchronization has no durable terminal state.' }
    $finalPreviewParts = (Get-Content -LiteralPath $presentationStatePath -Raw).Trim().Split('|')
    [ulong]$finalPreviewItemId = 0
    if ($finalPreviewParts.Count -ne 3 -or -not [ulong]::TryParse($finalPreviewParts[2], [ref]$finalPreviewItemId) -or
        -not (Test-PresentationPreviewStateAllowsPublication `
            -State $finalPreviewParts[0] -StatePlan $finalPreviewParts[1] -StateItemId $finalPreviewItemId `
            -ExpectedPreviewPlan ([string]$plan.previewPublicationPlanSha256) -ExpectedItemId $publishedFileId)) {
        Exit-InvalidInput 'The final Workshop update is not bound to one terminal preview synchronization for this item.'
    }
}
if ($publishedFileId -eq 0 -and -not [bool]$plan.allowFirstPublication) {
    Exit-InvalidInput 'This plan is not eligible to create exactly one new Workshop item.'
}
Assert-StagedCandidate $plan

$publicationRoot = Join-Path $releaseStateRoot 'publication'
$null = New-Item -ItemType Directory -Path $publicationRoot -Force
$existingReceipts = @(Get-ChildItem -LiteralPath $publicationRoot -Recurse -Filter 'publication-receipt.json' -File -ErrorAction SilentlyContinue)
$receiptIds = [System.Collections.Generic.HashSet[System.UInt64]]::new()
$completedReceiptPlans = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($receiptPath in $existingReceipts) {
    try { $existingReceipt = Read-Json $receiptPath.FullName }
    catch { Exit-InvalidInput "An existing publication receipt is unreadable: $($receiptPath.FullName)" }
    if ([string]$existingReceipt.publicationPlanSha256 -ceq $actualPlanHash) {
        Exit-InvalidInput 'This reviewed plan already has a successful publication receipt.'
    }
    [System.UInt64]$receiptId = 0
    if ([string]$existingReceipt.schema -cne 'ImmersiveChefs/WorkshopPublicationReceipt/v1' -or
        -not [ulong]::TryParse([string]$existingReceipt.publishedFileId, [ref]$receiptId) -or $receiptId -eq 0) {
        Exit-InvalidInput "An existing publication receipt has an invalid Workshop identity: $($receiptPath.FullName)"
    }
    $null = $receiptIds.Add($receiptId)
    if ([string]$existingReceipt.publicationPlanSha256 -match '^[A-Fa-f0-9]{64}$') {
        $null = $completedReceiptPlans.Add([string]$existingReceipt.publicationPlanSha256)
    }
}
if ($receiptIds.Count -gt 1 -or ($receiptIds.Count -eq 1 -and $publishedFileId -ne 0 -and -not $receiptIds.Contains($publishedFileId))) {
    Exit-InvalidInput 'Workshop receipt identities conflict with the reviewed candidate.'
}
if ($publishedFileId -eq 0 -and $receiptIds.Count -eq 1) {
    Exit-InvalidInput 'A prior successful receipt proves an existing Workshop item; rebuild the candidate so its immutable package includes that identity.'
}
$resumeState = $null
$priorState = $null
$priorStatePlan = $null
$priorStateItemId = [System.UInt64]0
if (Test-Path -LiteralPath $statePath -PathType Leaf) {
    $stateParts = @((Get-Content -LiteralPath $statePath -Raw).Trim().Split([char]'|'))
    if ($stateParts.Count -ne 3 -or $stateParts[1] -notmatch '^[A-Fa-f0-9]{64}$') {
        Exit-InvalidInput 'The durable publication state is invalid.'
    }
    $priorState = $stateParts[0]
    $priorStatePlan = $stateParts[1]
    if (-not [ulong]::TryParse($stateParts[2], [ref]$priorStateItemId)) {
        Exit-InvalidInput 'The durable publication state has an invalid item identity.'
    }
    if ($stateParts[1] -ceq $actualPlanHash) { $resumeState = $priorState }
}
if ($publishedFileId -eq 0 -and $null -ne $priorState -and $priorState -notin @('create-failed-definite')) {
    Exit-InvalidInput 'A prior first-publication attempt is indeterminate and has no durable item ID; refusing to create another item.'
}
$indeterminateStates = @('create-admitted', 'create-indeterminate', 'created', 'submit-admitted', 'submitted', 'submit-indeterminate', 'dependency-indeterminate', 'succeeded')
if ($null -ne $priorStatePlan -and $priorStatePlan -cne $actualPlanHash -and
    $indeterminateStates -ccontains [string]$priorState -and
    -not $completedReceiptPlans.Contains([string]$priorStatePlan)) {
    Exit-InvalidInput "Publication plan $priorStatePlan remains indeterminate at '$priorState'; reconcile that exact plan before any new Steam mutation."
}
$reconcileOnly = @('submit-admitted', 'submitted', 'submit-indeterminate', 'dependency-indeterminate', 'succeeded') -ccontains [string]$resumeState
$changeNotesBefore = @()
if ($publishedFileId -ne 0 -and $changeHistoryRequired) {
    $changeNotesBefore = @(Get-WorkshopChangeNotes -PublishedFileId $publishedFileId)
    if ($reconcileOnly) {
        $previousMatches = if ([string]::IsNullOrWhiteSpace([string]$plan.previousChangeNote)) {
            $changeNotesBefore.Count -eq 0
        }
        else {
            $changeNotesBefore.Count -gt 0 -and [string]$changeNotesBefore[0].note -ceq ([string]$plan.previousChangeNote).Trim()
        }
        $newMatches = $changeNotesBefore.Count -gt 0 -and [string]$changeNotesBefore[0].note -ceq ([string]$plan.changeNote).Trim()
        if (-not $previousMatches -and -not $newMatches) {
            Exit-InvalidInput 'Steam change history cannot reconcile this admitted publication.'
        }
    }
    elseif (-not (Test-WorkshopChangeNotePreflight `
        -RemoteNotes $changeNotesBefore `
        -ExpectedPreviousNote ([string]$plan.previousChangeNote) `
        -NewNote ([string]$plan.changeNote))) {
        Exit-InvalidInput 'Steam change history does not match the reviewed previous note, or the new note was already used.'
    }
}
$stateIdentityMatches = Test-ReconciliationStateIdentity -State ([string]$resumeState) `
    -StateItemId $priorStateItemId -PublishedFileId $publishedFileId
if (-not $stateIdentityMatches) {
    Exit-InvalidInput 'The durable publication state item identity conflicts with the recovered Workshop identity.'
}
$planIsAncestor = $revision -ceq [string]$plan.sourceRevision
if (-not $planIsAncestor) {
    & git -C $repositoryRoot merge-base --is-ancestor ([string]$plan.sourceRevision) $revision
    $ancestorExitCode = $LASTEXITCODE
    if ($ancestorExitCode -gt 1) { Exit-InvalidInput 'Could not validate the publication plan ancestry.' }
    $planIsAncestor = $ancestorExitCode -eq 0
}
if (-not (Test-ReleaseSourceRevisionAllowed -CurrentRevision $revision -PlanRevision ([string]$plan.sourceRevision) `
        -ReconcileOnly $reconcileOnly -PublishedFileId $publishedFileId -PlanIsAncestor $planIsAncestor)) {
    Exit-InvalidInput 'The publication plan was not built from the current committed revision, and no exact descendant post-submit reconciliation is eligible.'
}
$attemptId = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ', [Globalization.CultureInfo]::InvariantCulture)
$runRoot = Join-Path $publicationRoot $attemptId
$null = New-Item -ItemType Directory -Path $runRoot
Write-JsonUtf8 -Path (Join-Path $runRoot 'attempt-plan.json') -Value ([pscustomobject][ordered]@{
    schema = 'ImmersiveChefs/WorkshopPublicationAttempt/v1'
    planSha256 = $actualPlanHash
    publishedFileId = if ($publishedFileId -eq 0) { $null } else { [string]$publishedFileId }
    createEligible = $publishedFileId -eq 0
    reconcileOnly = $reconcileOnly
    startedUtc = [datetime]::UtcNow.ToString('O')
})
$gatewayRoot = Join-Path $runRoot 'gateway'
$publisherProcessLeaseFile = Join-Path $gatewayRoot 'publisher-process-lease.json'
$completionFile = Join-Path $runRoot 'publisher-complete.signal'
$launcherOut = Join-Path $runRoot 'gateway-launcher.out.log'
$launcherErr = Join-Path $runRoot 'gateway-launcher.err.log'
$launcher = Join-Path $repositoryRoot 'scripts\Invoke-GatewaySmoke.ps1'
$shell = (Get-Process -Id $PID).Path
$launcherArguments = @(
    '-NoProfile', '-File', $launcher,
    '-RimWorldPath', $RimWorldPath,
    '-SteamModContentFolder', $SteamModContentFolder,
    '-ArtifactsPath', $gatewayRoot,
    '-InteractiveHoldSeconds', [string]$TimeoutSeconds,
    '-InteractiveCompletionFile', $completionFile,
    '-ProcessLeaseFile', $publisherProcessLeaseFile,
    '-TimeoutSeconds', '300',
    '-Output', 'json'
)
$launcherProcess = Start-Process -FilePath $shell -ArgumentList $launcherArguments -PassThru -WindowStyle Hidden `
    -RedirectStandardOutput $launcherOut -RedirectStandardError $launcherErr
$publisherLauncherCompleted = $false
$publisherGameProcess = $null
$publisherGameProcessStartUtc = $null

$primaryError = $null
$cleanupError = $null
try {
    $deadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
    $hold = $null
    do {
        $launcherProcess.Refresh()
        if ($launcherProcess.HasExited) {
            throw "Gateway launcher exited before the publisher hold: $((Get-Content $launcherErr -Raw -ErrorAction SilentlyContinue))"
        }
        if ($null -eq $publisherGameProcess) {
            $publisherLease = Get-PublisherProcessLease -GatewayRoot $gatewayRoot -ProcessLeasePath $publisherProcessLeaseFile
            if ($null -ne $publisherLease) {
                $publisherGameProcess = $publisherLease.Process
                $publisherGameProcessStartUtc = $publisherLease.ProcessStartUtc
            }
        }
        $holdFiles = @(Get-ChildItem -LiteralPath $gatewayRoot -Recurse -Filter 'interactive-hold.json' -File -ErrorAction SilentlyContinue)
        if ($holdFiles.Count -gt 1) { throw 'More than one Gateway interactive hold appeared for this release.' }
        if ($holdFiles.Count -eq 1) {
            $candidate = Read-Json $holdFiles[0].FullName
            if ([string]$candidate.Status -ceq 'active') { $hold = $candidate }
        }
        if ($null -eq $hold) { Start-Sleep -Milliseconds 500 }
    } while ($null -eq $hold -and [datetime]::UtcNow -lt $deadline)
    if ($null -eq $hold) { throw 'The isolated Gateway publisher process did not become ready.' }

    $manifestPath = [string]$hold.Manifest
    $gameProcessId = [int]$hold.ProcessId
    $publisherManifest = Read-Json $manifestPath
    $holdProcessStartUtc = Get-ExactProcessStartUtcFromManifest -ManifestPath $manifestPath
    if ($null -eq $publisherGameProcess) {
        $publisherGameProcess = Get-Process -Id $gameProcessId -ErrorAction Stop
        $publisherGameProcessStartUtc = $holdProcessStartUtc
    }
    if ($publisherGameProcess.Id -ne $gameProcessId -or $publisherGameProcessStartUtc -ne $holdProcessStartUtc) {
        throw 'The interactive hold does not match the process identity retained from this exact Gateway run.'
    }
    Assert-RetainedProcessIdentity -Process $publisherGameProcess -ExpectedStartUtc $publisherGameProcessStartUtc
    $client = Join-Path $repositoryRoot 'artifacts\HostTools\Release\net480\RimWorldDevGateway.Client.exe'
    $managed = Join-Path $RimWorldPath 'RimWorldWin64_Data\Managed'
    $contract = Join-Path $repositoryRoot 'artifacts\HostTools\Release\net480\RimWorldDevGateway.Contracts.dll'
    $source = Join-Path $repositoryRoot 'scripts\Fixtures\GatewaySteamWorkshopPublisher.cs'
    $registration = Invoke-GatewayClient -Client $client -Manifest $manifestPath -ProcessId $gameProcessId -Arguments @(
        'execute-source', $source, '--managed', $managed, '--contract', $contract,
        '--entry-type', 'GatewaySteamWorkshopPublisher.Entry', '--entry-method', 'Execute', '--request-json', '{}')
    if (-not [bool]$registration.ok) { throw 'The checked-in Workshop publisher did not register.' }

    $status = Invoke-WorkshopAutomation -Client $client -Manifest $manifestPath -ProcessId $gameProcessId -Arguments @{ operation = 'status' }
    if ([string]$status.CurrentUserSteamId -cne [string]$plan.steamUserId) {
        throw "RimWorld is logged into Steam user $($status.CurrentUserSteamId), not the reviewed owner $($plan.steamUserId)."
    }

    if ($publishedFileId -eq 0) {
        $null = Invoke-WorkshopAutomation -Client $client -Manifest $manifestPath -ProcessId $gameProcessId `
            -Arguments @{ operation = 'owner-scan'; planSha256 = $actualPlanHash; title = [string]$plan.title }
        $ownerScan = Wait-WorkshopTerminal -Client $client -Manifest $manifestPath -ProcessId $gameProcessId `
            -PlanSha256 $actualPlanHash -Deadline $deadline -TerminalStatuses @('owner-scan-complete', 'owner-scan-found', 'failed')
        if ([string]$ownerScan.Status -eq 'owner-scan-found') {
            throw "The owning Steam account already publishes an exact-title Immersive Chefs item ($($ownerScan.PublishedFileId)); record that ID before any update."
        }
        if ([string]$ownerScan.Status -ne 'owner-scan-complete') {
            throw "Steam did not prove exact-title absence before first publication: $($ownerScan | ConvertTo-Json -Compress)"
        }
    }

    $queryRemote = {
        $null = Invoke-WorkshopAutomation -Client $client -Manifest $manifestPath -ProcessId $gameProcessId `
            -Arguments @{ operation = 'query'; planSha256 = $actualPlanHash; publishedFileId = [string]$publishedFileId }
        return Wait-WorkshopTerminal -Client $client -Manifest $manifestPath -ProcessId $gameProcessId `
            -PlanSha256 $actualPlanHash -Deadline $deadline -TerminalStatuses @('queried', 'failed')
    }

    $preflightRemote = $null
    if ($publishedFileId -ne 0) {
        $preflightRemote = & $queryRemote
        if ([string]$preflightRemote.Status -cne 'queried' -or
            [string]$preflightRemote.PublishedFileId -cne [string]$publishedFileId -or
            [string]$preflightRemote.RemoteOwnerSteamId -cne [string]$plan.steamUserId -or
            [int]$preflightRemote.RemoteConsumerAppId -ne 294100 -or
            [string]$preflightRemote.RemoteTitle -cne [string]$plan.title) {
            throw 'The existing Workshop item failed exact ID/owner/app/title preflight before mutation.'
        }
    }

    if ($PreviewSyncOnly) {
        if ($publishedFileId -eq 0) { throw 'Additional previews may be synchronized only after a private first publication has a retained identity.' }
        $previewResumeState = ''
        $previewResumePlan = ''
        $previewResumeItemId = [ulong]0
        if (Test-Path -LiteralPath $presentationStatePath -PathType Leaf) {
            $previewParts = (Get-Content -LiteralPath $presentationStatePath -Raw).Trim().Split('|')
            if ($previewParts.Count -ne 3 -or -not [ulong]::TryParse($previewParts[2], [ref]$previewResumeItemId)) {
                throw 'The durable presentation-preview state is malformed.'
            }
            $previewResumeState = $previewParts[0]
            $previewResumePlan = $previewParts[1]
            if ($previewResumeItemId -ne $publishedFileId) { throw 'The durable presentation-preview state targets a different Workshop item.' }
            if ($previewResumePlan -cne $actualPlanHash) {
                if ($previewResumeState -cne 'succeeded') { throw 'A different preview plan may not bypass an admitted Workshop preview update.' }
                $previewResumeState = ''
                $previewResumePlan = ''
            }
        }
        $previewNeedsMutation = $previewResumeState -notin @('preview-submit-admitted', 'preview-submitted', 'preview-submit-indeterminate', 'succeeded')
        if (-not $previewNeedsMutation) {
            $alreadyConverged = @($preflightRemote.RemoteAdditionalPreviews).Count -eq @($plan.additionalPreviews).Count -and
                @($preflightRemote.RemoteAdditionalPreviews | Where-Object { [string]::IsNullOrWhiteSpace([string]$_.Url) }).Count -eq 0
            if (-not $alreadyConverged) { throw 'The admitted Workshop preview update is indeterminate and has not converged; refusing to resubmit.' }
        }
        if ($previewNeedsMutation) {
        $null = Invoke-WorkshopAutomation -Client $client -Manifest $manifestPath -ProcessId $gameProcessId -Arguments @{
            operation = 'preview-sync'
            planSha256 = $actualPlanHash
            confirmation = $exactConfirmation
            publishedFileId = [string]$publishedFileId
            identityPath = $releaseIdentityPath
            statePath = $presentationStatePath
            title = [string]$plan.title
            changeNote = ''
            additionalPreviewPaths = @($plan.additionalPreviews | ForEach-Object { [string]$_.path })
        }
        $previewSync = Wait-WorkshopTerminal -Client $client -Manifest $manifestPath -ProcessId $gameProcessId `
            -PlanSha256 $actualPlanHash -Deadline $deadline -TerminalStatuses @('succeeded', 'failed', 'legal-agreement-required')
        if ([string]$previewSync.Status -cne 'succeeded') { throw "Workshop preview synchronization failed: $($previewSync | ConvertTo-Json -Compress)" }
        }

        $remotePreviewInventory = $null
        do {
            $candidate = & $queryRemote
            $candidatePreviews = @($candidate.RemoteAdditionalPreviews)
            if ([string]$candidate.Status -ceq 'queried' -and
                $candidatePreviews.Count -eq @($plan.additionalPreviews).Count -and
                @($candidatePreviews | Where-Object { [string]::IsNullOrWhiteSpace([string]$_.Url) }).Count -eq 0) {
                $remotePreviewInventory = $candidatePreviews
                break
            }
            Start-Sleep -Seconds 2
        } while ([datetime]::UtcNow -lt $deadline)
        if ($null -eq $remotePreviewInventory) { throw 'Steam did not expose the complete additional-preview inventory before the bounded deadline.' }

        $resolved = @(
            for ($index = 0; $index -lt @($plan.additionalPreviews).Count; $index++) {
                $remotePreview = $remotePreviewInventory[$index]
                $remoteUrl = [string]$remotePreview.Url
                if ([int]$remotePreview.Index -ne $index -or
                    [string]$remotePreview.Type -cne 'k_EItemPreviewType_Image' -or
                    -not [Uri]::IsWellFormedUriString($remoteUrl, [UriKind]::Absolute) -or
                    [Uri]::new($remoteUrl).Scheme -cne 'https' -or
                    [Uri]::new($remoteUrl).Host -cne 'images.steamusercontent.com') {
                    throw "Steam returned an unexpected additional-preview identity at slot $index."
                }
                $downloadPath = Join-Path $runRoot ("remote-additional-preview-{0:D2}.png" -f $index)
                Save-RemoteFile -Uri $remoteUrl -Path $downloadPath
                $remoteHash = (Get-FileHash -LiteralPath $downloadPath -Algorithm SHA256).Hash
                if ($remoteHash -cne [string]$plan.additionalPreviews[$index].sha256) {
                    throw "Steam additional preview $index differs from the reviewed local image."
                }
                [pscustomobject][ordered]@{
                    token = [string]$plan.additionalPreviews[$index].token
                    localPath = [string]$plan.additionalPreviews[$index].path
                    localSha256 = [string]$plan.additionalPreviews[$index].sha256
                    remoteIndex = [int]$remotePreview.Index
                    remoteUrl = $remoteUrl
                    remoteOriginalFileName = [string]$remotePreview.OriginalFileName
                    remoteType = [string]$remotePreview.Type
                    remoteDownloadedPath = $downloadPath
                    remoteSha256 = $remoteHash
                }
            }
        )
        $inventoryPath = Join-Path $runRoot 'presentation-preview-inventory.json'
        Write-JsonUtf8 -Path $inventoryPath -Value ([pscustomobject][ordered]@{
            schema = 'ImmersiveChefs/WorkshopRemotePreviewInventory/v1'
            publishedFileId = [string]$publishedFileId
            publicationPlanSha256 = $actualPlanHash
            previews = $resolved
        })
        Write-TextAtomically -Path $presentationStatePath -Value ("succeeded|$actualPlanHash|$publishedFileId")
        $previewOnlyResult = [pscustomobject][ordered]@{
            status = 'additional-previews-synchronized'
            publishedFileId = [string]$publishedFileId
            workshopUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=$publishedFileId"
            inventory = $inventoryPath
            previewCount = $resolved.Count
        }
        if ($Output -eq 'json') { $previewOnlyResult | ConvertTo-Json -Compress } else { $previewOnlyResult | Format-List }
        return
    }

    $remote = $null
    if ($reconcileOnly) {
        if ($publishedFileId -eq 0) { throw 'An indeterminate submit cannot be reconciled without a durable Workshop identity.' }
    }
    else {
        $request = @{
            operation = 'publish'
            planSha256 = $actualPlanHash
            confirmation = $exactConfirmation
            publishedFileId = if ($publishedFileId -eq 0) { '' } else { [string]$publishedFileId }
            allowFirstPublication = [bool]$plan.allowFirstPublication -and $publishedFileId -eq 0
            identityPath = $releaseIdentityPath
            repositoryIdentityPath = $repositoryIdentityPath
            packageIdentityPath = $packageIdentityPath
            statePath = $statePath
            title = [string]$plan.title
            packagePath = [string]$plan.packagePath
            descriptionPath = [string]$plan.descriptionPath
            previewPath = [string]$plan.previewPath
            changeNote = [string]$plan.changeNote
            requiredWorkshopItemId = [string]@($plan.requiredWorkshopItems)[0]
            tags = @($plan.tags)
            visibility = [string]$plan.visibility
        }
        $null = Invoke-WorkshopAutomation -Client $client -Manifest $manifestPath -ProcessId $gameProcessId -Arguments $request
        $publish = Wait-WorkshopTerminal -Client $client -Manifest $manifestPath -ProcessId $gameProcessId `
            -PlanSha256 $actualPlanHash -Deadline $deadline -TerminalStatuses @('succeeded', 'failed', 'legal-agreement-required')
        if ([string]$publish.Status -cne 'succeeded') { throw "Steam publication did not complete: $($publish | ConvertTo-Json -Compress)" }
        $publishedFileId = [ulong]$publish.PublishedFileId
        foreach ($path in @($repositoryIdentityPath, $releaseIdentityPath, $packageIdentityPath)) {
            if ((Get-Content -LiteralPath $path -Raw).Trim() -cne [string]$publishedFileId) {
                throw "The Workshop identity was not persisted before publication continued: $path"
            }
        }
    }

    do {
        $remoteCandidate = & $queryRemote
        $remoteTags = @(([string]$remoteCandidate.RemoteTags).Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        $expectedTags = @($plan.tags | ForEach-Object { [string]$_ })
        $tagsMatch = $remoteTags.Count -eq $expectedTags.Count -and
            @($remoteTags | Where-Object { $expectedTags -cnotcontains $_ }).Count -eq 0
        if ([string]$remoteCandidate.Status -ceq 'queried' -and
            [string]$remoteCandidate.RemoteTitle -ceq [string]$plan.title -and
            [string]$remoteCandidate.RemoteDescriptionSha256 -ceq [string]$plan.descriptionSha256 -and
            [string]$remoteCandidate.RemoteMetadata -ceq $actualPlanHash -and
            [string]$remoteCandidate.RemoteOwnerSteamId -ceq [string]$plan.steamUserId -and
            [string]$remoteCandidate.RemoteVisibility -ceq (Get-ExpectedRemoteVisibility ([string]$plan.visibility)) -and
            -not [string]::IsNullOrWhiteSpace([string]$remoteCandidate.RemotePreviewUrl) -and
            ($bootstrapPresentation -or
             (Test-RemoteWorkshopPreviewsMatchResolvedDescription `
                 -RemotePreviews @($remoteCandidate.RemoteAdditionalPreviews) `
                 -ResolvedPreviews @($plan.resolvedPreviews))) -and
            $tagsMatch) {
            $remote = $remoteCandidate
            break
        }
        Start-Sleep -Seconds 2
    } while ([datetime]::UtcNow -lt $deadline)
    if ($null -eq $remote) {
        throw "Remote Workshop metadata did not converge to the reviewed plan: $($remoteCandidate | ConvertTo-Json -Depth 8 -Compress)"
    }

    $verifiedChangeNote = $null
    if (-not $changeHistoryRequired) {
        $verifiedChangeNote = $null
    }
    elseif ($reconcileOnly -and $changeNotesBefore.Count -gt 0 -and
            [string]$changeNotesBefore[0].note -ceq ([string]$plan.changeNote).Trim()) {
        $verifiedChangeNote = $changeNotesBefore[0]
    }
    else {
        do {
            $currentNotes = @(Get-WorkshopChangeNotes -PublishedFileId $publishedFileId)
            if ($currentNotes.Count -gt 0 -and
                [string]$currentNotes[0].note -ceq ([string]$plan.changeNote).Trim() -and
                ($reconcileOnly -or
                 @($changeNotesBefore | Where-Object { [string]$_.id -ceq [string]$currentNotes[0].id }).Count -eq 0)) {
                $verifiedChangeNote = $currentNotes[0]
                break
            }
            Start-Sleep -Seconds 2
        } while ([datetime]::UtcNow -lt $deadline)
        if ($null -eq $verifiedChangeNote) { throw 'Steam change history did not expose the exact new player-facing note.' }
    }

    $expectedDependencies = @($plan.requiredWorkshopItems | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $actualDependencies = @($remote.RemoteDependencies | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $dependencyOperations = @()
    $dependencyOperations += @($actualDependencies | Where-Object { $expectedDependencies -cnotcontains $_ } | ForEach-Object {
        [pscustomobject]@{ Operation = 'dependency-remove'; ItemId = $_ }
    })
    $dependencyOperations += @($expectedDependencies | Where-Object { $actualDependencies -cnotcontains $_ } | ForEach-Object {
        [pscustomobject]@{ Operation = 'dependency-add'; ItemId = $_ }
    })
    foreach ($dependencyOperation in $dependencyOperations) {
        # The immediately preceding exact query owns the in-process ID/owner/app/title preflight.
        $dependencyPreflight = & $queryRemote
        if ([string]$dependencyPreflight.Status -cne 'queried' -or
            [string]$dependencyPreflight.PublishedFileId -cne [string]$publishedFileId -or
            [string]$dependencyPreflight.RemoteOwnerSteamId -cne [string]$plan.steamUserId -or
            [int]$dependencyPreflight.RemoteConsumerAppId -ne 294100 -or
            [string]$dependencyPreflight.RemoteTitle -cne [string]$plan.title) {
            throw 'The Workshop item failed exact preflight immediately before dependency mutation.'
        }
        $null = Invoke-WorkshopAutomation -Client $client -Manifest $manifestPath -ProcessId $gameProcessId -Arguments @{
            operation = [string]$dependencyOperation.Operation
            planSha256 = $actualPlanHash
            publishedFileId = [string]$publishedFileId
            requiredWorkshopItemId = [string]$dependencyOperation.ItemId
            title = [string]$plan.title
            statePath = $statePath
        }
        $dependency = Wait-WorkshopTerminal -Client $client -Manifest $manifestPath -ProcessId $gameProcessId `
            -PlanSha256 $actualPlanHash -Deadline $deadline -TerminalStatuses @('succeeded', 'failed')
        if ([string]$dependency.Status -cne 'succeeded') { throw "Workshop dependency operation failed: $($dependencyOperation.Operation) $($dependencyOperation.ItemId)" }
    }
    if ($dependencyOperations.Count -gt 0) {
        $remote = $null
        do {
            $remoteCandidate = & $queryRemote
            $candidateDependencies = @($remoteCandidate.RemoteDependencies | ForEach-Object { [string]$_ } | Sort-Object -Unique)
            $remoteTags = @(([string]$remoteCandidate.RemoteTags).Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
            $tagsMatch = $remoteTags.Count -eq $expectedTags.Count -and
                @($remoteTags | Where-Object { $expectedTags -cnotcontains $_ }).Count -eq 0
            if ([string]$remoteCandidate.Status -ceq 'queried' -and
                [string]$remoteCandidate.RemoteTitle -ceq [string]$plan.title -and
                [string]$remoteCandidate.RemoteDescriptionSha256 -ceq [string]$plan.descriptionSha256 -and
                [string]$remoteCandidate.RemoteMetadata -ceq $actualPlanHash -and
                [string]$remoteCandidate.RemoteOwnerSteamId -ceq [string]$plan.steamUserId -and
                [string]$remoteCandidate.RemoteVisibility -ceq (Get-ExpectedRemoteVisibility ([string]$plan.visibility)) -and
                -not [string]::IsNullOrWhiteSpace([string]$remoteCandidate.RemotePreviewUrl) -and
                $tagsMatch -and
                $candidateDependencies.Count -eq $expectedDependencies.Count -and
                @($candidateDependencies | Where-Object { $expectedDependencies -cnotcontains $_ }).Count -eq 0) {
                $remote = $remoteCandidate
                break
            }
            Start-Sleep -Seconds 2
        } while ([datetime]::UtcNow -lt $deadline)
        if ($null -eq $remote) { throw 'The exact Workshop dependency graph did not propagate.' }
    } elseif ($actualDependencies.Count -ne $expectedDependencies.Count -or
              @($actualDependencies | Where-Object { $expectedDependencies -cnotcontains $_ }).Count -ne 0) {
        throw 'The exact Workshop dependency graph was not established.'
    }

    $remotePreviewPath = Join-Path $runRoot 'remote-preview.png'
    Save-RemoteFile -Uri ([string]$remote.RemotePreviewUrl) -Path $remotePreviewPath
    if ((Get-FileHash -LiteralPath $remotePreviewPath -Algorithm SHA256).Hash -cne [string]$plan.previewSha256) {
        throw 'Steam serves a preview image that differs from the reviewed preview bytes.'
    }
    Write-TextAtomically -Path $statePath -Value ("succeeded|$actualPlanHash|$publishedFileId")

    $null = Invoke-WorkshopAutomation -Client $client -Manifest $manifestPath -ProcessId $gameProcessId `
        -Arguments @{ operation = 'subscribe'; planSha256 = $actualPlanHash; publishedFileId = [string]$publishedFileId }
    $installed = Wait-WorkshopTerminal -Client $client -Manifest $manifestPath -ProcessId $gameProcessId `
        -PlanSha256 $actualPlanHash -Deadline $deadline -TerminalStatuses @('installed', 'failed')
    if ([string]$installed.Status -cne 'installed') { throw "The subscribed Workshop item did not install: $($installed | ConvertTo-Json -Compress)" }
    $expectedInstallPath = Join-Path ([IO.Path]::GetFullPath($SteamModContentFolder)) ([string]$publishedFileId)
    if (-not [string]::Equals([IO.Path]::GetFullPath([string]$installed.InstallFolder), $expectedInstallPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Steam reported an unexpected Workshop install folder.'
    }
    $manifest = Read-Json ([string]$plan.packageManifestPath)
    foreach ($entry in @($manifest.files)) {
        $installedPath = Join-Path $expectedInstallPath ([string]$entry.path)
        if (-not (Test-Path -LiteralPath $installedPath -PathType Leaf) -or
            (Get-FileHash -LiteralPath $installedPath -Algorithm SHA256).Hash -cne [string]$entry.sha256) {
            throw "Subscribed Workshop package differs from the candidate: $installedPath"
        }
    }
    if ((Get-Content -LiteralPath (Join-Path $expectedInstallPath 'About\PublishedFileId.txt') -Raw).Trim() -cne [string]$publishedFileId) {
        throw 'Subscribed Workshop package has the wrong identity file.'
    }
    $expectedInstalledPaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @($manifest.files)) { $null = $expectedInstalledPaths.Add([string]$entry.path) }
    $null = $expectedInstalledPaths.Add('About\PublishedFileId.txt')
    $actualInstalledPaths = @(Get-ChildItem -LiteralPath $expectedInstallPath -Recurse -File | ForEach-Object {
        Get-RelativePath -Root $expectedInstallPath -Path $_.FullName
    })
    if ($actualInstalledPaths.Count -ne $expectedInstalledPaths.Count -or
        @($actualInstalledPaths | Where-Object { -not $expectedInstalledPaths.Contains($_) }).Count -ne 0) {
        throw 'Subscribed Workshop package contains an unexpected or missing file.'
    }

    [IO.File]::WriteAllText($completionFile, [datetime]::UtcNow.ToString('O'), [Text.UTF8Encoding]::new($false))
    if (-not $launcherProcess.WaitForExit(120000)) {
        $launcherProcess.Kill()
        $launcherProcess.WaitForExit()
        Assert-RetainedProcessIdentity -Process $publisherGameProcess -ExpectedStartUtc $publisherGameProcessStartUtc
        $null = Stop-RetainedProcess -Process $publisherGameProcess
        throw 'The exact Gateway publisher launcher did not clean up before subscribed verification.'
    }
    if ($launcherProcess.ExitCode -ne 0) {
        throw "Gateway publisher cleanup failed before subscribed verification: $((Get-Content $launcherErr -Raw -ErrorAction SilentlyContinue))"
    }
    Assert-RetainedProcessIdentity -Process $publisherGameProcess -ExpectedStartUtc $publisherGameProcessStartUtc
    if (Stop-RetainedProcess -Process $publisherGameProcess) {
        throw 'The exact Gateway publisher RimWorld process required force cleanup before subscribed verification.'
    }
    $publisherLauncherCompleted = $true

    $subscribedSmokeScript = Join-Path $repositoryRoot 'scripts\Invoke-ImmersiveChefsSubscribedSmoke.ps1'
    $subscribedSmokeRoot = Join-Path $runRoot 'subscribed-smoke'
    $subscribedSmokeOutput = @(& $subscribedSmokeScript `
        -PublishedFileId ([string]$publishedFileId) `
        -ExpectedInstallPath $expectedInstallPath `
        -RimWorldPath $RimWorldPath `
        -SteamModContentFolder $SteamModContentFolder `
        -ArtifactsPath $subscribedSmokeRoot `
        -TimeoutSeconds 420 `
        -Output json 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "Subscribed Workshop player smoke failed: $($subscribedSmokeOutput -join [Environment]::NewLine)"
    }
    try { $subscribedSmoke = ($subscribedSmokeOutput -join [Environment]::NewLine).Trim() | ConvertFrom-Json }
    catch { throw "Subscribed Workshop player smoke returned invalid JSON: $($subscribedSmokeOutput -join [Environment]::NewLine)" }
    if ([string]$subscribedSmoke.status -cne 'passed' -or
        -not [bool]$subscribedSmoke.localProductRestored -or
        -not [string]::Equals(
            [IO.Path]::GetFullPath([string]$subscribedSmoke.loadedPackagePath),
            $expectedInstallPath,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Subscribed Workshop player smoke did not prove exact-path native behavior and restoration.'
    }

    $receipt = [pscustomobject][ordered]@{
        schema = 'ImmersiveChefs/WorkshopPublicationReceipt/v1'
        publishedUtc = [datetime]::UtcNow.ToString('O')
        sourceRevision = [string]$plan.sourceRevision
        publisherSourceRevision = $revision
        publicationPlan = $planPath
        publicationPlanSha256 = $actualPlanHash
        candidateSha256 = [string]$plan.candidateSha256
        changeNote = [string]$plan.changeNote
        remoteChangeNote = $verifiedChangeNote
        publishedFileId = [string]$publishedFileId
        workshopUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=$publishedFileId"
        remote = $remote
        installed = $installed
        installedPackagePath = $expectedInstallPath
        subscribedSmoke = $subscribedSmoke
        gatewayEvidence = [IO.DirectoryInfo]::new([string]$holdFiles[0].FullName).Parent.FullName
        tokenRetained = $false
    }
    $receiptPath = Join-Path $runRoot 'publication-receipt.json'
    Write-JsonUtf8 -Path $receiptPath -Value $receipt
    $result = [pscustomobject][ordered]@{
        status = 'published-and-subscribed'
        publishedFileId = [string]$publishedFileId
        workshopUrl = $receipt.workshopUrl
        receipt = $receiptPath
        installedPackagePath = $expectedInstallPath
        sourceRevision = [string]$plan.sourceRevision
        publisherSourceRevision = $revision
        candidateSha256 = [string]$plan.candidateSha256
    }
}
catch {
    $primaryError = $_
}
finally {
    try {
        if (-not $publisherLauncherCompleted) {
            $cleanupSignalError = $null
            if ($null -eq $publisherGameProcess) {
                $publisherLease = Get-PublisherProcessLease -GatewayRoot $gatewayRoot -ProcessLeasePath $publisherProcessLeaseFile
                if ($null -ne $publisherLease) {
                    $publisherGameProcess = $publisherLease.Process
                    $publisherGameProcessStartUtc = $publisherLease.ProcessStartUtc
                }
            }
            try { [IO.File]::WriteAllText($completionFile, [datetime]::UtcNow.ToString('O'), [Text.UTF8Encoding]::new($false)) }
            catch { $cleanupSignalError = $_ }
            if (-not $launcherProcess.WaitForExit(120000)) {
                $launcherProcess.Kill()
                $launcherProcess.WaitForExit()
                Assert-RetainedProcessIdentity -Process $publisherGameProcess -ExpectedStartUtc $publisherGameProcessStartUtc
                $null = Stop-RetainedProcess -Process $publisherGameProcess
                throw 'The exact Gateway publisher launcher did not clean up within two minutes.'
            }
            Assert-RetainedProcessIdentity -Process $publisherGameProcess -ExpectedStartUtc $publisherGameProcessStartUtc
            if (Stop-RetainedProcess -Process $publisherGameProcess) {
                throw 'The exact Gateway publisher RimWorld process required force cleanup.'
            }
            if ($launcherProcess.ExitCode -ne 0) {
                throw "Gateway publisher cleanup failed: $((Get-Content $launcherErr -Raw -ErrorAction SilentlyContinue))"
            }
            $publisherLauncherCompleted = $true
            if ($null -ne $cleanupSignalError) { throw $cleanupSignalError }
        }
    }
    catch {
        $cleanupError = $_
    }
    try {
        if ($leaseAcquired) { $releaseLease.ReleaseMutex() }
        $releaseLease.Dispose()
    }
    catch {
        if ($null -eq $cleanupError) { $cleanupError = $_ }
    }
}

if ($null -ne $primaryError) {
    if ($null -ne $cleanupError) {
        throw "$($primaryError.Exception.Message) Cleanup also failed: $($cleanupError.Exception.Message)"
    }
    throw $primaryError
}
if ($null -ne $cleanupError) { throw $cleanupError }

if ($Output -eq 'json') { $result | ConvertTo-Json -Compress } else { $result | Format-List }
