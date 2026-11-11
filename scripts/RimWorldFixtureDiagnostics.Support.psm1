Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-RimWorldFixtureExpectations {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Sha256,
        [Parameter(Mandatory)][string[]]$ActivePackageIds)
    $part = [IO.Path]::GetFullPath($Path)
    while ($null -ne $part) {
        if ([IO.Path]::Exists($part) -and ([IO.File]::GetAttributes($part) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Fixture expectations cross a reparse point.' }
        $part = [IO.Path]::GetDirectoryName($part)
    }
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ($stream.Length -gt 65536) { throw 'Fixture expectations exceed 64 KiB.' }
        $bytes = [byte[]]::new([int]$stream.Length)
        $stream.ReadExactly($bytes)
        if ($stream.ReadByte() -ne -1) { throw 'Fixture expectations changed during read.' }
    } finally { $stream.Dispose() }
    if ($Sha256 -cnotmatch '^[A-F0-9]{64}$' -or [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)) -cne $Sha256) { throw 'Fixture expectation hash mismatch.' }
    # This immutable file is emitted only from validated selected plans by fixture-stage.
    $document = [Text.Json.JsonDocument]::Parse([Text.Encoding]::UTF8.GetString($bytes))
    try {
        $root = $document.RootElement
        $names = @($root.EnumerateObject() | ForEach-Object { $_.Name })
        if ($names.Count -ne 3 -or @($names | Select-Object -Unique).Count -ne 3 -or
            $root.GetProperty('schema').GetString() -cne 'RimWorldDevGateway/XmlFixtureExpectations/v1') { throw 'Invalid fixture expectation schema.' }
        $active = @($root.GetProperty('activePackageIds').EnumerateArray() | ForEach-Object { $_.GetString() })
        # E2E metadata excludes the implicit Gateway; smoke appends it to the exact process list.
        $gateway = 'fumblesneeze.rimworlddevgateway'
        if ($active -icontains $gateway -or ((@($active) + @($gateway)) -join '|') -cne ($ActivePackageIds -join '|')) {
            throw 'Fixture expectations do not belong to this exact active group with its appended Gateway.'
        }
        $expected = [Collections.Generic.List[object]]::new()
        $messages = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $packages = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($fixture in $root.GetProperty('fixtures').EnumerateArray()) {
            $fields = @($fixture.EnumerateObject() | ForEach-Object { $_.Name })
            if (($fields | Sort-Object) -join '|' -cne 'expectedErrors|files|manifestSha256|ownerPackageId|packageId') { throw 'Invalid fixture provenance fields.' }
            $owner = $fixture.GetProperty('ownerPackageId').GetString()
            $package = $fixture.GetProperty('packageId').GetString()
            $manifestHash = $fixture.GetProperty('manifestSha256').GetString()
            if (-not $packages.Add($package) -or $active -cnotcontains $package -or $active -cnotcontains $owner -or
                [Array]::IndexOf($active, $owner) -ge [Array]::IndexOf($active, $package) -or
                $manifestHash -cnotmatch '^[A-F0-9]{64}$') { throw 'Invalid selected fixture provenance.' }
            $entries = @($fixture.GetProperty('expectedErrors').EnumerateArray())
            if ($entries.Count -gt 16) { throw 'Too many fixture diagnostic expectations.' }
            foreach ($entry in $entries) {
                $fields = @($entry.EnumerateObject() | ForEach-Object { $_.Name })
                $message = $entry.GetProperty('message').GetString()
                $count = $entry.GetProperty('count').GetInt32()
                if (($fields | Sort-Object) -join '|' -cne 'count|message' -or
                    [string]::IsNullOrWhiteSpace($message) -or $message.Length -gt 4096 -or $message -match '[\x00-\x1F\x7F]' -or
                    $message -cne $message.Trim() -or $count -lt 1 -or $count -gt 16 -or -not $messages.Add($message)) { throw 'Invalid exact fixture diagnostic expectation.' }
                $expected.Add([pscustomobject]@{ OwnerPackageId = $owner; PackageId = $package; ManifestSha256 = $manifestHash; Message = $message; Count = $count })
            }
        }
        return [pscustomobject]@{ File = [IO.Path]::GetFullPath($Path); Sha256 = $Sha256; Errors = @($expected) }
    } finally { $document.Dispose() }
}

function Get-RimWorldFixtureLogAccounting {
    param([Parameter(Mandatory)][string]$Path, [AllowNull()][object]$Expectations, [switch]$Final)
    $errors = if ($null -eq $Expectations) { @() } else { @($Expectations.Errors) }
    $expected = @($errors | ForEach-Object { [pscustomobject]@{
        OwnerPackageId = $_.OwnerPackageId; PackageId = $_.PackageId; ManifestSha256 = $_.ManifestSha256
        Message = $_.Message; RequiredCount = $_.Count; ObservedCount = 0; Matches = [Collections.Generic.List[object]]::new()
    } })
    $unexpected = [Collections.Generic.List[object]]::new()
    $riskPattern = '(?i)(FileNotFoundException|TypeLoadException|MissingMethodException|ReflectionTypeLoadException|Root level exception|Exception:|Error while instantiating a mod|Could not instantiate|Could not resolve cross-reference|XML error|not a Def type or could not be found|Patch operation .* failed|EmbedIO.*(error|exception)|Mono\.CSharp.*(error|exception)|Swan\.Lite.*(error|exception)|System\.ValueTuple.*(error|exception))'
    $current = $null
    $lineNumber = 0
    # Classify one stable snapshot. A native stream must use explicit ERROR record headers;
    # bare text inside any of its records must never impersonate a legacy error event.
    $share = if ($Final) { [IO.FileShare]::Read } else { [IO.FileShare]::ReadWrite }
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, $share)
    try {
        # Read only bytes already present at open; a live append-only writer cannot move EOF
        # indefinitely. A short read/truncation remains a failure, never an ignored diagnostic.
        $snapshotByteLength = $stream.Length
        $snapshotBytes = [byte[]]::new([int]$snapshotByteLength)
        $stream.ReadExactly($snapshotBytes)
    } finally { $stream.Dispose() }
    $decoder = [IO.StreamReader]::new([IO.MemoryStream]::new($snapshotBytes, $false))
    try { $snapshotText = $decoder.ReadToEnd() } finally { $decoder.Dispose() }
    $deferredTrailingCharacters = 0
    if (-not $Final) {
        # Appends may stop in the middle of a message or UTF-8 character. Only the final
        # post-shutdown scan classifies an unterminated last line; no complete line is dropped.
        $completeLength = $snapshotText.LastIndexOfAny([char[]]"`r`n") + 1
        $deferredTrailingCharacters = $snapshotText.Length - $completeLength
        $snapshotText = $snapshotText.Substring(0, $completeLength)
    }
    $reader = [IO.StringReader]::new($snapshotText)
    try {
        $snapshotLines = [Collections.Generic.List[string]]::new()
        while ($null -ne ($line = $reader.ReadLine())) { $snapshotLines.Add($line) }
        $lines = $snapshotLines.ToArray()
    } finally { $reader.Dispose() }
    # Padding belongs before the severity column. Consume exactly one separator after it,
    # preserving any leading spaces that are part of the actual diagnostic message.
    $severityHeader = '(?<severity>ERROR|FATAL|WARN|WARNING|INFO|DEBUG|TRACE) (?<message>.*)$'
    $timestampedHeader = [regex]::new('^\d{2}:\d{2}:\d{2}(?:\.\d+)? +' + $severityHeader)
    $hasTimestampedHeaders = @($lines | Where-Object { $timestampedHeader.IsMatch($_) }).Count -gt 0
    # In a timestamped stream, an unprefixed "ERROR ..." is continuation text, not
    # another native header. Untimestamped severity headers belong only to legacy logs.
    $headerPattern = if ($hasTimestampedHeaders) { $timestampedHeader } else { [regex]::new('^' + $severityHeader) }
    $hasNativeHeaders = @($lines | Where-Object { $headerPattern.IsMatch($_) }).Count -gt 0
    foreach ($line in $lines) {
        $lineNumber++
        $header = $headerPattern.Match($line)
        $nativeSeverity = $header.Success
        $severity = $header.Groups['severity'].Value
        $nativeError = $nativeSeverity -and $severity -cin @('ERROR', 'FATAL')
        $message = if ($nativeSeverity) { $header.Groups['message'].Value } else { $line -creplace '^\d{2}:\d{2}:\d{2}(?:\.\d+)? ', '' }
        # A continuation is retained but never exempted from independent error classification.
        $candidate = $null
        if (($nativeSeverity -and $severity -ceq 'ERROR') -or (-not $hasNativeHeaders -and -not $nativeSeverity)) {
            $candidate = @($expected | Where-Object { $_.Message -ceq $message }) | Select-Object -First 1
        }
        if ($nativeError -or $message -match $riskPattern -or $null -ne $candidate) {
            $current = [pscustomobject]@{ LineNumber = $lineNumber; Message = $message; Lines = [Collections.Generic.List[string]]::new() }
            $current.Lines.Add($line)
            if ($null -ne $candidate) {
                $candidate.ObservedCount++
                $candidate.Matches.Add($current)
            } else { $unexpected.Add($current) }
        } elseif ($nativeSeverity) { $current = $null }
        elseif ($null -ne $current) { $current.Lines.Add($line) }
    }
    $mismatches = @($expected | Where-Object { $_.ObservedCount -gt $_.RequiredCount -or ($Final -and $_.ObservedCount -ne $_.RequiredCount) })
    return [pscustomobject]@{
        Phase = $(if ($Final) { 'final' } else { 'intermediate' }); Passed = ($unexpected.Count -eq 0 -and $mismatches.Count -eq 0)
        SnapshotByteLength = $snapshotByteLength
        DeferredTrailingCharacters = $deferredTrailingCharacters
        ExpectationsFile = $(if ($null -eq $Expectations) { $null } else { $Expectations.File })
        ExpectationsSha256 = $(if ($null -eq $Expectations) { $null } else { $Expectations.Sha256 })
        Expected = $expected; Unexpected = @($unexpected); CountMismatches = $mismatches
    }
}

function Assert-RimWorldFixtureLog {
    param([Parameter(Mandatory)][string]$Path, [AllowNull()][object]$Expectations, [switch]$Final,
        [Parameter(Mandatory)][string]$EvidencePath)
    $result = Get-RimWorldFixtureLogAccounting -Path $Path -Expectations $Expectations -Final:$Final
    [IO.File]::WriteAllText($EvidencePath, ($result | ConvertTo-Json -Depth 16), [Text.UTF8Encoding]::new($false))
    if (-not $result.Passed) {
        throw "Player.log contains unexpected errors or fixture diagnostic count mismatches; see $EvidencePath"
    }
}

Export-ModuleMember -Function Read-RimWorldFixtureExpectations, Get-RimWorldFixtureLogAccounting, Assert-RimWorldFixtureLog
