Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-RimWorldXml10Text {
    [CmdletBinding()]
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string]$Text
    )

    if ($null -eq $Text) {
        return ''
    }

    $builder = [System.Text.StringBuilder]::new($Text.Length)
    for ($index = 0; $index -lt $Text.Length; $index++) {
        $value = [int]$Text[$index]
        if ($value -ge 0xD800 -and $value -le 0xDBFF) {
            if ($index + 1 -lt $Text.Length) {
                $next = [int]$Text[$index + 1]
                if ($next -ge 0xDC00 -and $next -le 0xDFFF) {
                    $null = $builder.Append($Text[$index])
                    $null = $builder.Append($Text[$index + 1])
                    $index++
                    continue
                }
            }

            $null = $builder.Append([char]0xFFFD)
            continue
        }

        if (($value -ge 0xDC00 -and $value -le 0xDFFF) -or
            -not ($value -eq 0x09 -or $value -eq 0x0A -or $value -eq 0x0D -or
                  ($value -ge 0x20 -and $value -le 0xD7FF) -or
                  ($value -ge 0xE000 -and $value -le 0xFFFD))) {
            $null = $builder.Append([char]0xFFFD)
            continue
        }

        $null = $builder.Append($Text[$index])
    }

    return $builder.ToString()
}

function Write-RimWorldEndToEndJUnitReport {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$GroupResults
    )

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $failures = @($GroupResults | Where-Object { [string]$_.Status -ne 'passed' }).Count
        $writer.WriteStartDocument()
        $writer.WriteStartElement('testsuite')
        $writer.WriteAttributeString('name', 'RimWorld E2E exact mod groups')
        $writer.WriteAttributeString('tests', [string]$GroupResults.Count)
        $writer.WriteAttributeString('failures', [string]$failures)
        foreach ($groupResult in $GroupResults) {
            $writer.WriteStartElement('testcase')
            $writer.WriteAttributeString('classname', 'RimWorld.EndToEnd.ModGroup')
            $writer.WriteAttributeString(
                'name',
                (ConvertTo-RimWorldXml10Text -Text ([string]$groupResult.GroupId)))
            if ([string]$groupResult.Status -ne 'passed') {
                $writer.WriteStartElement('failure')
                $writer.WriteAttributeString(
                    'message',
                    (ConvertTo-RimWorldXml10Text -Text ([string]$groupResult.Message)))
                $failureDetail = if (Test-Path -LiteralPath ([string]$groupResult.StandardError) -PathType Leaf) {
                    Get-Content -LiteralPath ([string]$groupResult.StandardError) -Raw
                }
                else {
                    [string]$groupResult.Message
                }
                $writer.WriteString((ConvertTo-RimWorldXml10Text -Text $failureDetail))
                $writer.WriteEndElement()
            }
            $writer.WriteEndElement()
        }
        $writer.WriteEndElement()
        $writer.WriteEndDocument()
    }
    finally {
        $writer.Dispose()
    }
}

function Get-RimWorldDeployedProductEvidence {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PackageId,

        [Parameter(Mandatory)]
        [string]$ProjectPath,

        [Parameter(Mandatory)]
        [string]$RimWorldPath,

        [Parameter(Mandatory)]
        [string]$BuildLogPath
    )

    $resolvedProject = (Resolve-Path -LiteralPath $ProjectPath -ErrorAction Stop).Path
    $resolvedGame = (Resolve-Path -LiteralPath $RimWorldPath -ErrorAction Stop).Path
    $resolvedBuildLog = (Resolve-Path -LiteralPath $BuildLogPath -ErrorAction Stop).Path
    $packageRoot = Join-Path (Join-Path $resolvedGame 'Mods') $PackageId
    if (-not (Test-Path -LiteralPath $packageRoot -PathType Container)) {
        throw "Deployed product package '$PackageId' does not exist at '$packageRoot'."
    }

    $packageRoot = (Resolve-Path -LiteralPath $packageRoot -ErrorAction Stop).Path
    $packageRootInfo = Get-Item -LiteralPath $packageRoot -Force -ErrorAction Stop
    if (($packageRootInfo.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Deployed product package '$PackageId' must not be a reparse point."
    }

    $entries = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -Force -ErrorAction Stop)
    foreach ($entry in $entries) {
        if (($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Deployed product package '$PackageId' contains a reparse entry."
        }
    }

    $files = [System.Collections.Generic.List[object]]::new()
    foreach ($file in @($entries | Where-Object { -not $_.PSIsContainer })) {
        $relativePath = $file.FullName.Substring($packageRoot.Length).TrimStart('\', '/') -replace '\\', '/'
        $segments = @($relativePath -split '/')
        if (@($segments | Where-Object { $_ -ieq 'DevEndToEndTests' }).Count -gt 0) {
            continue
        }

        $files.Add([pscustomobject]@{
            RelativePath = $relativePath
            Length = [long]$file.Length
            Sha256 = (Get-FileHash `
                -LiteralPath $file.FullName `
                -Algorithm SHA256 `
                -ErrorAction Stop).Hash.ToUpperInvariant()
        })
    }

    $orderedFiles = @($files | Sort-Object -Property RelativePath -CaseSensitive)
    if ($orderedFiles.Count -eq 0) {
        throw "Deployed product package '$PackageId' contains no product files."
    }

    return [pscustomobject]@{
        PackageId = $PackageId
        Project = $resolvedProject
        PackageRoot = $packageRoot
        BuildLog = $resolvedBuildLog
        Files = $orderedFiles
    }
}

function ConvertTo-RimWorldProductEvidenceJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Evidence
    )

    return ConvertTo-Json -InputObject @($Evidence) -Depth 8 -Compress
}

function Get-RimWorldEndToEndXmlDeserializer {
    param([Parameter(Mandatory)][object]$Group)
    $present = if ($Group -is [Collections.IDictionary]) { $Group.Contains('xmlDeserializer') }
        else { $null -ne $Group.PSObject.Properties['xmlDeserializer'] }
    if (-not $present) { return 'default' }
    $value = [string]$Group.xmlDeserializer
    if ($value -cnotin @('default', 'legacy')) { throw 'Unknown E2E native XML deserializer mode.' }
    return $value
}

function New-RimWorldEndToEndSmokeLaunch {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$Arguments,

        [switch]$EnableAudio,

        [ValidateSet('default', 'legacy')][string]$XmlDeserializer = 'default',

        [AllowNull()][object]$FixtureExpectations
    )

    $projectedArguments = [System.Collections.Generic.List[string]]::new()
    foreach ($argument in $Arguments) {
        $projectedArguments.Add($argument)
    }
    if ($XmlDeserializer -ceq 'legacy') { $projectedArguments.Add('-LegacyXmlDeserializer') }
    if ($EnableAudio) {
        $projectedArguments.Add('-EnableAudio')
    }
    if ($null -ne $FixtureExpectations) {
        if ([string]::IsNullOrWhiteSpace([string]$FixtureExpectations.path) -or
            [string]$FixtureExpectations.sha256 -cnotmatch '^[A-F0-9]{64}$') { throw 'Invalid staged fixture expectation receipt.' }
        $projectedArguments.Add('-XmlFixtureExpectationsFile')
        $projectedArguments.Add([string]$FixtureExpectations.path)
        $projectedArguments.Add('-XmlFixtureExpectationsSha256')
        $projectedArguments.Add([string]$FixtureExpectations.sha256)
    }

    return [pscustomobject]@{
        AudioEnabled = [bool]$EnableAudio
        XmlDeserializer = $XmlDeserializer
        Arguments = @($projectedArguments)
    }
}

function Initialize-RimWorldEndToEndOwnedRun {
    param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$OwnedRunDirectory)
    $resolved = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($OwnedRunDirectory))
    $grouped = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'artifacts/EndToEndRuns/Grouped'))
    if ([IO.Path]::GetDirectoryName($resolved) -ine $grouped -or -not [IO.Directory]::Exists($resolved)) {
        throw 'Owned E2E run must be an existing immediate child of the repository grouped artifacts directory.'
    }
    $current = [IO.DirectoryInfo]::new($resolved)
    while ($null -ne $current) {
        if ($current.Exists -and ($current.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'Owned E2E run crosses a reparse point.'
        }
        $current = $current.Parent
    }
    $markerPath = Join-Path $resolved 'typed-run.json'
    $markerFile = Get-Item -LiteralPath $markerPath -Force -ErrorAction Stop
    if ($markerFile.Length -gt 65536 -or ($markerFile.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'Invalid typed E2E ownership marker.'
    }
    $marker = [IO.File]::ReadAllText($markerPath) | ConvertFrom-Json -AsHashtable -ErrorAction Stop
    if ($marker.Count -ne 2 -or $marker.schema -cne 'RimWorldModdingMcp/OwnedE2ERun/v1' -or
        $marker.runDirectory -cne $resolved) {
        throw 'Typed E2E ownership marker does not match this run.'
    }
    $receipt = Join-Path $resolved 'runner-process.json'
    $temporary = $receipt + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    try {
        $process = [Diagnostics.Process]::GetCurrentProcess()
        try {
            [IO.File]::WriteAllText($temporary, (@{
                schema = 'RimWorldDevGateway/ProcessLease/v1'
                processId = $PID
                processStartUtc = $process.StartTime.ToUniversalTime().ToString('O')
            } | ConvertTo-Json -Compress), [Text.UTF8Encoding]::new($false))
        }
        finally { $process.Dispose() }
        [IO.File]::Move($temporary, $receipt)
    }
    finally { if ([IO.File]::Exists($temporary)) { Remove-Item -LiteralPath $temporary -Force } }
    return $resolved
}

function Assert-RimWorldFixtureProcessStopped {
    param([Parameter(Mandatory)][string]$Identity)
    $part = [IO.Path]::GetFullPath($Identity)
    while ($null -ne $part) {
        if ([IO.Path]::Exists($part) -and ([IO.File]::GetAttributes($part) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Process identity crosses a reparse point.' }
        $part = [IO.Path]::GetDirectoryName($part)
    }
    $file = Get-Item -LiteralPath $Identity -Force -ErrorAction Stop
    if ($file.Length -gt 65536) { throw 'Invalid child process identity.' }
    $document = [Text.Json.JsonDocument]::Parse([IO.File]::ReadAllText($Identity))
    try {
        $value = $document.RootElement
        $names = @($value.EnumerateObject() | ForEach-Object { $_.Name })
        $started = [DateTimeOffset]::MinValue
        $childId = 0
        if ($names.Count -ne 3 -or @($names | Select-Object -Unique).Count -ne 3 -or
            $value.GetProperty('schema').GetString() -cne 'RimWorldDevGateway/ProcessLease/v1' -or
            -not $value.GetProperty('processId').TryGetInt32([ref]$childId) -or $childId -le 0 -or
            -not [DateTimeOffset]::TryParse($value.GetProperty('processStartUtc').GetString(), [ref]$started) -or $started.Offset -ne [TimeSpan]::Zero) { throw 'Invalid child process identity.' }
    }
    finally { $document.Dispose() }
    $child = $null
    try { $child = [Diagnostics.Process]::GetProcessById($childId) }
    catch [ArgumentException] { }
    if ($null -ne $child) {
        try {
            if ($child.StartTime.ToUniversalTime().Ticks -ne $started.UtcTicks) { throw 'Child PID has been reused; exit is uncertain.' }
            if (-not $child.HasExited) { throw 'Exact child is still running.' }
        }
        finally { $child.Dispose() }
    }
}

function Invoke-RimWorldEndToEndFixtureLaunch {
    param(
        [Parameter(Mandatory)][string]$RunDirectory,
        [Parameter(Mandatory)][string]$FixtureLeaseFile,
        [Parameter(Mandatory)][string]$ProcessLeaseFile,
        [switch]$ExternalStage,
        [Parameter(Mandatory)][scriptblock]$Stage,
        [Parameter(Mandatory)][scriptblock]$Launch,
        [Parameter(Mandatory)][scriptblock]$Clean
    )
    $root = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($RunDirectory))
    $lease = [IO.Path]::GetFullPath($FixtureLeaseFile)
    $identity = [IO.Path]::GetFullPath($ProcessLeaseFile)
    $stageIdentity = Join-Path ([IO.Path]::GetDirectoryName($lease)) 'fixture-stage-process.json'
    foreach ($path in @($root, $lease, $identity)) {
        $part = $path
        while ($null -ne $part) {
            if ([IO.Path]::Exists($part) -and ([IO.File]::GetAttributes($part) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'Fixture launch path crosses a reparse point.'
            }
            $part = [IO.Path]::GetDirectoryName($part)
        }
    }
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($lease)) -cne $root -or
        [IO.Path]::GetFileName($lease) -cne 'fixture.lease.json' -or
        [IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($identity)) -cne $root -or
        [IO.Path]::GetFileName($identity) -cne 'game-process.json' -or
        [IO.Path]::GetFileName([IO.Path]::GetDirectoryName($identity)) -cnotmatch '^smoke-[0-9]{3,}$') {
        throw 'Fixture launch requires exact immediate group/child lease paths.'
    }
    $catalogPath = Join-Path $root 'fixture-recovery.json'
    $groups = [Collections.Generic.List[object]]::new()
    if ([IO.File]::Exists($catalogPath)) {
        $file = Get-Item -LiteralPath $catalogPath -Force
        if ($file.Length -gt 65536 -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Invalid fixture catalog.' }
        $document = [Text.Json.JsonDocument]::Parse([IO.File]::ReadAllText($catalogPath))
        try {
            $value = $document.RootElement
            $fields = @($value.EnumerateObject() | ForEach-Object { $_.Name })
            if ($fields.Count -ne 2 -or @($fields | Select-Object -Unique).Count -ne 2 -or
                $value.GetProperty('schema').GetString() -cne 'RimWorldModdingMcp/E2EFixtureRecovery/v1') { throw 'Invalid fixture catalog.' }
            $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            foreach ($entry in $value.GetProperty('groups').EnumerateArray()) {
                $fields = @($entry.EnumerateObject() | ForEach-Object { $_.Name })
                if ($fields.Count -ne 5 -or @($fields | Select-Object -Unique).Count -ne 5) { throw 'Ambiguous fixture catalog fields.' }
                $fixturePath = $entry.GetProperty('fixtureLeaseFile').GetString()
                $processPath = $entry.GetProperty('processLeaseFile').GetString()
                $stagePath = $entry.GetProperty('stageProcessLeaseFile').GetString()
                if ($fixturePath -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]*/fixture\.lease\.json$' -or
                    $fixturePath.Split('/')[0].EndsWith('.') -or
                    $processPath -cnotmatch '^smoke-[0-9]{3,}/game-process\.json$' -or
                    $stagePath -cne ($fixturePath.Split('/')[0] + '/fixture-stage-process.json') -or
                    -not $paths.Add($fixturePath) -or -not $paths.Add($processPath) -or -not $paths.Add($stagePath)) { throw 'Invalid fixture catalog paths.' }
                $groups.Add(@{ fixtureLeaseFile=$fixturePath; processLeaseFile=$processPath; launchAttempted=$entry.GetProperty('launchAttempted').GetBoolean()
                    stageProcessLeaseFile=$stagePath; stageLaunchAttempted=$entry.GetProperty('stageLaunchAttempted').GetBoolean() })
            }
        }
        finally { $document.Dispose() }
    }
    $record = @{ fixtureLeaseFile = [IO.Path]::GetRelativePath($root, $lease) -replace '\\', '/'
        processLeaseFile = [IO.Path]::GetRelativePath($root, $identity) -replace '\\', '/'; launchAttempted = $false
        stageProcessLeaseFile = [IO.Path]::GetRelativePath($root, $stageIdentity) -replace '\\', '/'; stageLaunchAttempted = [bool]$ExternalStage }
    if (@($groups | Where-Object { $_.fixtureLeaseFile -ieq $record.fixtureLeaseFile -or $_.processLeaseFile -ieq $record.processLeaseFile }).Count -ne 0) {
        throw 'Fixture launch paths are already registered.'
    }
    $groups.Add($record)
    $writeCatalog = {
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes((@{ schema='RimWorldModdingMcp/E2EFixtureRecovery/v1'; groups=@($groups) } | ConvertTo-Json -Depth 8 -Compress))
        if ($bytes.Length -gt 65536) { throw 'Fixture recovery catalog exceeds 64 KiB.' }
        $temporary = $catalogPath + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
        try {
            $stream = [IO.FileStream]::new($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
            try { $stream.Write($bytes); $stream.Flush($true) } finally { $stream.Dispose() }
            [IO.File]::Move($temporary, $catalogPath, $true)
        }
        finally { if ([IO.File]::Exists($temporary)) { Remove-Item -LiteralPath $temporary -Force } }
    }
    & $writeCatalog
    $primary = $null
    $result = $null
    try {
        $null = & $Stage
        if ($ExternalStage) { Assert-RimWorldFixtureProcessStopped -Identity $stageIdentity }
        $record.launchAttempted = $true
        & $writeCatalog
        $result = & $Launch
    }
    catch { $primary = $_ }
    $cleanupFailure = $null
    try {
        if ($ExternalStage) { Assert-RimWorldFixtureProcessStopped -Identity $stageIdentity }
        elseif ([IO.Path]::Exists($stageIdentity)) { throw 'In-process publication has contradictory child evidence.' }
        if ([IO.Path]::Exists($lease)) {
            if ($record.launchAttempted) {
                Assert-RimWorldFixtureProcessStopped -Identity $identity
            }
            elseif ([IO.Path]::Exists($identity)) { throw 'Unattempted launch has contradictory process evidence.' }
            $null = & $Clean
            if ([IO.Path]::Exists($lease)) { throw 'Fixture cleanup retained its lease.' }
        }
    }
    catch { $cleanupFailure = $_.Exception.Message }
    if ($null -ne $cleanupFailure) {
        $message = "XML fixture cleanup incomplete: $cleanupFailure"
        if ($null -ne $primary) { $message = "$($primary.Exception.Message); $message" }
        $failure = [InvalidOperationException]::new($message)
        if ($null -ne $result) { $failure.Data['LaunchResult'] = $result }
        throw $failure
    }
    if ($null -ne $primary) { throw $primary }
    return $result
}

Export-ModuleMember -Function `
    Invoke-RimWorldEndToEndFixtureLaunch, `
    Initialize-RimWorldEndToEndOwnedRun, `
    ConvertTo-RimWorldXml10Text, `
    Write-RimWorldEndToEndJUnitReport, `
    Get-RimWorldDeployedProductEvidence, `
    ConvertTo-RimWorldProductEvidenceJson, `
    New-RimWorldEndToEndSmokeLaunch, `
    Get-RimWorldEndToEndXmlDeserializer
