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

function Read-RimWorldSelectedTestExpectations {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Sha256,
        [Parameter(Mandatory)][string[]]$ActivePackageIds, [Parameter(Mandatory)][string[]]$SelectedTestIds,
        [Parameter(Mandatory)][string]$ModsRoot, [Parameter(Mandatory)][string]$RimWorldVersion,
        [Parameter(Mandatory)][string]$SavedDataPath)
    function Assert-OrdinaryPath([string]$Value) {
        $part = [IO.Path]::GetFullPath($Value)
        while ($null -ne $part) {
            if ([IO.Path]::Exists($part) -and ([IO.File]::GetAttributes($part) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Selected-test expectations cross a reparse point.' }
            $part = [IO.Path]::GetDirectoryName($part)
        }
    }
    function Read-BoundBytes([string]$File, [string]$Hash, [long]$Maximum) {
        Assert-OrdinaryPath $File
        $stream = [IO.File]::Open($File, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        try {
            if ($stream.Length -gt $Maximum) { throw 'Selected-test expectation input exceeds its size bound.' }
            $bytes = [byte[]]::new([int]$stream.Length)
            $stream.ReadExactly($bytes)
            if ($stream.ReadByte() -ne -1) { throw 'Selected-test expectation input changed during read.' }
        } finally { $stream.Dispose() }
        if ($Hash -cnotmatch '^[A-F0-9]{64}$' -or [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)) -cne $Hash) { throw 'Selected-test expectation artifact hash mismatch.' }
        return ,$bytes
    }
    function Assert-Fields($Element, [string]$Names) {
        $fields = @($Element.EnumerateObject() | ForEach-Object { $_.Name })
        if (($fields | Sort-Object) -join '|' -cne $Names) { throw 'Invalid selected-test expectation fields.' }
    }
    $bytes = Read-BoundBytes $Path $Sha256 65536
    $document = [Text.Json.JsonDocument]::Parse([Text.Encoding]::UTF8.GetString($bytes))
    $manifestDocument = $null
    try {
        $root = $document.RootElement
        Assert-Fields $root 'activePackageIds|assemblyFileName|assemblyIdentity|assemblySha256|expectedDiagnostics|manifestFileName|manifestSha256|moduleVersionId|ownerPackageId|schema|testId'
        $testId = $root.GetProperty('testId').GetString()
        if ($root.GetProperty('schema').GetString() -cne 'RimWorldDevGateway/SelectedTestExpectations/v1' -or
            $SelectedTestIds.Count -ne 1 -or $SelectedTestIds[0] -cne $testId -or [string]::IsNullOrWhiteSpace($testId)) { throw 'Selected-test expectations require their exact single selected test.' }
        $active = @($root.GetProperty('activePackageIds').EnumerateArray() | ForEach-Object { $_.GetString() })
        $gateway = 'fumblesneeze.rimworlddevgateway'
        $owner = $root.GetProperty('ownerPackageId').GetString()
        if ($active -icontains $gateway -or ((@($active) + @($gateway)) -join '|') -cne ($ActivePackageIds -join '|') -or
            @($active | Select-Object -Unique).Count -ne $active.Count -or $ActivePackageIds -cnotcontains $owner -or
            $owner -cnotmatch '^[a-z0-9][a-z0-9_.-]*$' -or $owner.Contains('..')) { throw 'Selected-test expectations do not belong to this exact active group.' }
        $assembly = $root.GetProperty('assemblyFileName').GetString()
        $manifest = $root.GetProperty('manifestFileName').GetString()
        if ($RimWorldVersion -cnotmatch '^\d+\.\d+$' -or $assembly -cnotmatch '^[A-Za-z0-9_.-]+\.dll$' -or
            $manifest -cnotmatch '^[A-Za-z0-9_.-]+\.json$' -or $assembly.Contains('..') -or $manifest.Contains('..')) { throw 'Invalid staged expectation artifact name.' }
        $stage = Join-Path (Join-Path (Join-Path $ModsRoot $owner) $RimWorldVersion) 'DevEndToEndTests'
        $null = Read-BoundBytes (Join-Path $stage $assembly) ($root.GetProperty('assemblySha256').GetString()) 67108864
        $manifestBytes = Read-BoundBytes (Join-Path $stage $manifest) ($root.GetProperty('manifestSha256').GetString()) 1048576
        $manifestDocument = [Text.Json.JsonDocument]::Parse([Text.Encoding]::UTF8.GetString($manifestBytes))
        $bundle = $manifestDocument.RootElement
        if ($bundle.GetProperty('schema').GetString() -cne 'RimWorldDevGateway.EndToEndTests/v3' -or
            $bundle.GetProperty('ownerPackageId').GetString() -cne $owner -or $bundle.GetProperty('assembly').GetString() -cne $assembly) { throw 'Selected-test expectation bundle provenance mismatch.' }
        if ($root.GetProperty('assemblySha256').GetString() -cne $bundle.GetProperty('assemblySha256').GetString().ToUpperInvariant()) { throw 'Selected-test expectation compiled hash mismatch.' }
        foreach ($field in @('assemblyIdentity', 'moduleVersionId')) {
            if ($root.GetProperty($field).GetString() -cne $bundle.GetProperty($field).GetString()) { throw 'Selected-test expectation compiled identity mismatch.' }
        }
        $tests = @($bundle.GetProperty('tests').EnumerateArray() | Where-Object { $_.GetProperty('id').GetString() -ceq $testId })
        if ($tests.Count -ne 1 -or $tests[0].GetProperty('ownerPackageId').GetString() -cne $owner -or
            (@($tests[0].GetProperty('activePackageIds').EnumerateArray() | ForEach-Object { $_.GetString() }) -join '|') -cne ($active -join '|')) { throw 'Selected-test expectation test provenance mismatch.' }
        $entries = @($root.GetProperty('expectedDiagnostics').EnumerateArray())
        $compiledEntries = @($tests[0].GetProperty('expectedDiagnostics').EnumerateArray())
        if ($entries.Count -lt 1 -or $entries.Count -gt 16 -or $entries.Count -ne $compiledEntries.Count) { throw 'Invalid selected-test expectation declaration count.' }
        $savedData = [IO.Path]::GetFullPath($SavedDataPath).TrimEnd([IO.Path]::DirectorySeparatorChar)
        if (-not [IO.Path]::IsPathFullyQualified($SavedDataPath) -or [IO.Path]::GetFileName($savedData) -cne 'SavedData') { throw 'Selected-test expectation binding requires the isolated SavedData directory.' }
        Assert-OrdinaryPath $savedData
        $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $messages = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $expected = [Collections.Generic.List[object]]::new()
        for ($index = 0; $index -lt $entries.Count; $index++) {
            $entry = $entries[$index]
            $compiled = $compiledEntries[$index]
            Assert-Fields $entry 'count|id|innerCauses|message|severity'
            $id = $entry.GetProperty('id').GetString()
            $severity = $entry.GetProperty('severity').GetString()
            $template = $entry.GetProperty('message').GetString()
            $count = $entry.GetProperty('count').GetInt32()
            $causes = @($entry.GetProperty('innerCauses').EnumerateArray() | ForEach-Object { $_.GetString() })
            if ($id.Length -gt 128 -or $id -cnotmatch '^[A-Za-z0-9_.:-]+$' -or -not $ids.Add($id) -or
                $severity -cnotin @('ERROR', 'WARN') -or $count -lt 1 -or $count -gt 16 -or
                [string]::IsNullOrWhiteSpace($template) -or $template.Length -gt 4096 -or $template -cne $template.Trim() -or
                $template -match '[\x00-\x1F\x7F]' -or -not $messages.Add($severity + '|' + $template) -or
                $causes.Count -gt 4 -or @($causes | Select-Object -Unique).Count -ne $causes.Count) { throw 'Invalid exact selected-test diagnostic declaration.' }
            foreach ($cause in $causes) {
                if ($cause.Length -gt 4096 -or -not $cause.StartsWith(' ---> ', [StringComparison]::Ordinal) -or
                    $cause.IndexOf(': ', [StringComparison]::Ordinal) -le 6 -or $cause -match '[\x00-\x1F\x7F{}]') { throw 'Invalid exact selected-test inner cause.' }
            }
            foreach ($field in @('id', 'severity', 'message')) {
                if ($entry.GetProperty($field).GetString() -cne $compiled.GetProperty($field).GetString()) { throw 'Selected-test expectation declaration drift.' }
            }
            if ($count -ne $compiled.GetProperty('count').GetInt32() -or
                ($causes -join "`n") -cne (@($compiled.GetProperty('innerCauses').EnumerateArray() | ForEach-Object { $_.GetString() }) -join "`n")) { throw 'Selected-test expectation declaration drift.' }
            $message = $template
            if ($template.Contains('{') -or $template.Contains('}')) {
                $binding = [regex]::Match($template, '"\{savedata\}[\\/]Saves[\\/](?<name>[A-Za-z0-9_.-]{1,128})"')
                if (-not $binding.Success -or $binding.Groups['name'].Value.Contains('..') -or
                    $binding.Groups['name'].Value -cnotmatch '\.rws(?:\.new)?$' -or
                    $template.Remove($binding.Index, $binding.Length) -match '[{}]') { throw 'Invalid closed savedata binding in selected-test expectation.' }
                $boundPath = Join-Path (Join-Path $savedData 'Saves') $binding.Groups['name'].Value
                Assert-OrdinaryPath $boundPath
                $message = $template.Substring(0, $binding.Index) + '"' + $boundPath + '"' + $template.Substring($binding.Index + $binding.Length)
            }
            $expected.Add([pscustomobject]@{ OwnerPackageId = $owner; PackageId = $null; TestId = $testId; Id = $id
                AssemblySha256 = $root.GetProperty('assemblySha256').GetString(); ModuleVersionId = $root.GetProperty('moduleVersionId').GetString()
                ManifestSha256 = $root.GetProperty('manifestSha256').GetString(); Severity = $severity; Template = $template
                Message = $message; Count = $count; InnerCauses = $causes })
        }
        return [pscustomobject]@{ File = [IO.Path]::GetFullPath($Path); Sha256 = $Sha256; SavedDataPath = $savedData; Errors = @($expected) }
    } finally { if ($null -ne $manifestDocument) { $manifestDocument.Dispose() }; $document.Dispose() }
}

function Merge-RimWorldDiagnosticExpectations {
    param([AllowNull()][object]$Fixture, [AllowNull()][object]$SelectedTest)
    if ($null -eq $Fixture) { return $SelectedTest }
    if ($null -eq $SelectedTest) { return $Fixture }
    $errors = @($Fixture.Errors) + @($SelectedTest.Errors)
    $keys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entry in $errors) {
        $severity = if ($null -eq $entry.PSObject.Properties['Severity']) { 'ERROR' } else { $entry.Severity }
        if (-not $keys.Add($severity + '|' + $entry.Message)) { throw 'Ambiguous XML and selected-test diagnostic expectations.' }
    }
    return [pscustomobject]@{ File = @($Fixture.File, $SelectedTest.File); Sha256 = @($Fixture.Sha256, $SelectedTest.Sha256); Errors = $errors }
}

function Get-RimWorldFixtureLogAccounting {
    param([Parameter(Mandatory)][string]$Path, [AllowNull()][object]$Expectations, [switch]$Final)
    $errors = if ($null -eq $Expectations) { @() } else { @($Expectations.Errors) }
    $expected = @($errors | ForEach-Object {
        $declaredSeverity = $_.PSObject.Properties['Severity']
        if ($null -ne $declaredSeverity -and $declaredSeverity.Value -cnotin @('ERROR', 'WARN')) {
            throw 'Expected runtime diagnostic severity must be ERROR or WARN.'
        }
        $innerCauses = @(if ($null -ne $_.PSObject.Properties['InnerCauses']) { $_.InnerCauses })
        if ($innerCauses.Count -gt 4 -or @($innerCauses | Select-Object -Unique).Count -ne $innerCauses.Count) {
            throw 'Expected runtime inner causes must contain at most four distinct exact lines.'
        }
        foreach ($cause in $innerCauses) {
            if ($null -eq $declaredSeverity -or $cause -isnot [string] -or $cause.Length -gt 4096 -or
                -not $cause.StartsWith(' ---> ', [StringComparison]::Ordinal) -or
                @($cause.ToCharArray() | Where-Object { [char]::IsControl($_) }).Count -gt 0) {
                throw 'Expected runtime inner cause must be one bounded native inner-exception line.'
            }
        }
        [pscustomobject]@{
        OwnerPackageId = $_.OwnerPackageId; PackageId = $_.PackageId; ManifestSha256 = $_.ManifestSha256
        TestId = $(if ($null -ne $_.PSObject.Properties['TestId']) { $_.TestId } else { $null })
        Id = $(if ($null -ne $_.PSObject.Properties['Id']) { $_.Id } else { $null })
        AssemblySha256 = $(if ($null -ne $_.PSObject.Properties['AssemblySha256']) { $_.AssemblySha256 } else { $null })
        ModuleVersionId = $(if ($null -ne $_.PSObject.Properties['ModuleVersionId']) { $_.ModuleVersionId } else { $null })
        Template = $(if ($null -ne $_.PSObject.Properties['Template']) { $_.Template } else { $null })
        Severity = $(if ($null -eq $declaredSeverity) { 'ERROR' } else { $declaredSeverity.Value })
        RequiresNativeSeverity = ($null -ne $declaredSeverity)
        InnerCauses = $innerCauses
        Message = $_.Message; RequiredCount = $_.Count; ObservedCount = 0; Matches = [Collections.Generic.List[object]]::new()
    } })
    $unexpected = [Collections.Generic.List[object]]::new()
    $causeMismatches = [Collections.Generic.List[object]]::new()
    $pendingCauseParent = $null
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
        if ($null -ne $pendingCauseParent) {
            if (-not $nativeSeverity -and $line -ceq $pendingCauseParent.RequiredInnerCauses[$pendingCauseParent.ObservedInnerCauseCount]) {
                $pendingCauseParent.Lines.Add($line)
                $pendingCauseParent.ObservedInnerCauseCount++
                if ($pendingCauseParent.ObservedInnerCauseCount -eq $pendingCauseParent.RequiredInnerCauses.Count) { $pendingCauseParent = $null }
                continue
            }
            $causeMismatches.Add($pendingCauseParent)
            $pendingCauseParent = $null
        }
        # A continuation is retained but never exempted from independent error classification.
        $candidate = $null
        if ($nativeSeverity -and $severity -cin @('ERROR', 'WARN')) {
            $candidate = @($expected | Where-Object { $_.Severity -ceq $severity -and $_.Message -ceq $message }) | Select-Object -First 1
        } elseif (-not $hasNativeHeaders -and -not $nativeSeverity) {
            $candidate = @($expected | Where-Object { -not $_.RequiresNativeSeverity -and $_.Message -ceq $message }) | Select-Object -First 1
        }
        if ($nativeError -or $message -match $riskPattern -or $message.StartsWith(' ---> ', [StringComparison]::Ordinal) -or $null -ne $candidate) {
            $current = [pscustomobject]@{ LineNumber = $lineNumber; Message = $message; Lines = [Collections.Generic.List[string]]::new()
                RequiredInnerCauses = @(); ObservedInnerCauseCount = 0 }
            $current.Lines.Add($line)
            if ($null -ne $candidate) {
                $candidate.ObservedCount++
                $candidate.Matches.Add($current)
                $current.RequiredInnerCauses = @($candidate.InnerCauses)
                if ($current.RequiredInnerCauses.Count -gt 0) { $pendingCauseParent = $current }
            } else { $unexpected.Add($current) }
        } elseif ($nativeSeverity) { $current = $null }
        elseif ($null -ne $current) { $current.Lines.Add($line) }
    }
    if ($Final -and $null -ne $pendingCauseParent) { $causeMismatches.Add($pendingCauseParent) }
    $mismatches = @($expected | Where-Object { $_.ObservedCount -gt $_.RequiredCount -or ($Final -and $_.ObservedCount -ne $_.RequiredCount) })
    return [pscustomobject]@{
        Phase = $(if ($Final) { 'final' } else { 'intermediate' }); Passed = ($unexpected.Count -eq 0 -and $mismatches.Count -eq 0 -and $causeMismatches.Count -eq 0)
        SnapshotByteLength = $snapshotByteLength
        DeferredTrailingCharacters = $deferredTrailingCharacters
        ExpectationsFile = $(if ($null -eq $Expectations) { $null } else { $Expectations.File })
        ExpectationsSha256 = $(if ($null -eq $Expectations) { $null } else { $Expectations.Sha256 })
        Expected = $expected; Unexpected = @($unexpected); CountMismatches = $mismatches; InnerCauseMismatches = @($causeMismatches)
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

Export-ModuleMember -Function Read-RimWorldFixtureExpectations, Read-RimWorldSelectedTestExpectations, Merge-RimWorldDiagnosticExpectations, Get-RimWorldFixtureLogAccounting, Assert-RimWorldFixtureLog
