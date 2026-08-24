[CmdletBinding()]
param(
    [string]$RepositoryRoot,

    [ValidateRange(1, 70)]
    [int]$BuildTimeoutSeconds = 70
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Import-Module Microsoft.PowerShell.Utility -ErrorAction Stop

function Write-Diagnostic {
    param([string]$Message)

    [Console]::Error.WriteLine($Message)
}

function Get-CanonicalRoot {
    param([string]$Candidate)

    $selected = if ([string]::IsNullOrWhiteSpace($Candidate)) {
        Join-Path $PSScriptRoot '..'
    }
    else {
        $Candidate
    }

    $resolved = [IO.Path]::GetFullPath($selected).TrimEnd([IO.Path]::DirectorySeparatorChar)
    foreach ($marker in @('AGENTS.md', 'global.json', 'tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj')) {
        if (-not (Test-Path -LiteralPath (Join-Path $resolved $marker))) {
            throw "Repository root is missing required marker '$marker': $resolved"
        }
    }

    return $resolved
}

function Get-InputFiles {
    param([string]$Root)

    $files = [Collections.Generic.List[IO.FileInfo]]::new()
    foreach ($name in @(
        'global.json',
        'Directory.Build.props',
        'Directory.Build.targets',
        'Directory.Packages.props',
        'NuGet.config',
        '.codex\Start-RimWorldModdingMcp.ps1')) {
        $path = Join-Path $Root $name
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            $files.Add([IO.FileInfo]::new($path))
        }
    }

    $projectRoot = Join-Path $Root 'tools\RimWorldModding.Mcp'
    foreach ($file in Get-ChildItem -LiteralPath $projectRoot -Recurse -Force -File) {
        $projectRelative = $file.FullName.Substring($projectRoot.Length).TrimStart('\', '/')
        if ($projectRelative -match '(^|[\\/])(bin|obj)([\\/]|$)') {
            continue
        }

        if ($file.Extension -in @('.cs', '.csproj', '.props', '.targets', '.json')) {
            $files.Add($file)
        }
    }

    return @($files | Sort-Object FullName -Unique)
}

function Get-SourceSnapshot {
    param([string]$Root)

    $sdkVersion = (& dotnet --version 2>&1 | Select-Object -Last 1).ToString().Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sdkVersion)) {
        throw 'Could not resolve the repository-pinned dotnet SDK.'
    }

    $entries = [Collections.Generic.List[object]]::new()
    $seed = [Text.StringBuilder]::new()
    [void]$seed.Append("sdk=").Append($sdkVersion).Append("`n")
    foreach ($file in Get-InputFiles -Root $Root) {
        $relative = $file.FullName.Substring($Root.Length).TrimStart('\', '/').Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
        $entries.Add([pscustomobject]@{
            path = $relative
            length = $file.Length
            sha256 = $hash
        })
        [void]$seed.Append($relative).Append(':').Append($file.Length).Append(':').Append($hash).Append("`n")
    }

    if ($entries.Count -eq 0) {
        throw 'No MCP source inputs were discovered.'
    }

    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($seed.ToString())
        $fingerprint = ([BitConverter]::ToString($algorithm.ComputeHash($bytes))).Replace('-', '')
    }
    finally {
        $algorithm.Dispose()
    }

    return [pscustomobject]@{
        sdkVersion = $sdkVersion
        fingerprint = $fingerprint
        inputs = @($entries)
    }
}

function Get-OutputInventory {
    param([string]$Directory)

    return @(Get-ChildItem -LiteralPath $Directory -Recurse -Force -File |
        Where-Object Name -NE 'bootstrap-manifest.json' |
        Sort-Object FullName |
        ForEach-Object {
            [pscustomobject]@{
                path = $_.FullName.Substring($Directory.Length).TrimStart('\', '/').Replace('\', '/')
                length = $_.Length
                sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
            }
        })
}

function Test-PublishedEntry {
    param(
        [string]$Directory,
        [string]$Fingerprint
    )

    try {
        $manifestPath = Join-Path $Directory 'bootstrap-manifest.json'
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
            return $false
        }

        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        if ($manifest.schemaVersion -ne 1 -or $manifest.fingerprint -ne $Fingerprint) {
            return $false
        }

        $expected = @($manifest.files)
        $actual = @(Get-OutputInventory -Directory $Directory)
        if ($expected.Count -eq 0 -or $actual.Count -ne $expected.Count) {
            return $false
        }

        $actualByPath = @{}
        foreach ($item in $actual) {
            $actualByPath[$item.path] = $item
        }

        foreach ($item in $expected) {
            if (-not $actualByPath.ContainsKey($item.path)) {
                return $false
            }

            $observed = $actualByPath[$item.path]
            if ($observed.length -ne $item.length -or $observed.sha256 -ne $item.sha256) {
                return $false
            }
        }

        foreach ($required in @('RimWorldModding.Mcp.dll', 'RimWorldModding.Mcp.deps.json', 'RimWorldModding.Mcp.runtimeconfig.json')) {
            if (-not (Test-Path -LiteralPath (Join-Path $Directory $required) -PathType Leaf)) {
                return $false
            }
        }

        return $true
    }
    catch {
        return $false
    }
}

function Enter-BuildLease {
    param(
        [string]$Path,
        [TimeSpan]$Timeout
    )

    $deadline = [DateTime]::UtcNow.Add($Timeout)
    do {
        try {
            return [IO.File]::Open($Path, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        }
        catch [IO.IOException] {
            if ([DateTime]::UtcNow -ge $deadline) {
                throw "Timed out waiting for the MCP source-build lease: $Path"
            }

            Start-Sleep -Milliseconds 100
        }
    } while ($true)
}

function Invoke-BoundedDotNetBuild {
    param(
        [string[]]$Arguments,
        [TimeSpan]$Timeout
    )

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw 'Could not start the bounded MCP source build.'
        }

        $standardOutput = $process.StandardOutput.ReadToEndAsync()
        $standardError = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit([int]$Timeout.TotalMilliseconds)) {
            try { $process.Kill($true) } catch { Write-Diagnostic $_.Exception.Message }
            if (-not $process.WaitForExit(5000)) {
                throw "The MCP source build exceeded its $([int]$Timeout.TotalSeconds)-second bound and could not be stopped within 5 seconds."
            }
            throw "The MCP source build exceeded its $([int]$Timeout.TotalSeconds)-second bound."
        }

        $process.WaitForExit()
        foreach ($line in @($standardOutput.GetAwaiter().GetResult(), $standardError.GetAwaiter().GetResult())) {
            foreach ($part in $line -split '\r?\n') {
                if (-not [string]::IsNullOrWhiteSpace($part)) {
                    Write-Diagnostic $part
                }
            }
        }

        return $process.ExitCode
    }
    finally {
        $process.Dispose()
    }
}

function Remove-StagingDirectory {
    param(
        [string]$StagingRoot,
        [string]$Candidate
    )

    if ([string]::IsNullOrWhiteSpace($Candidate) -or -not (Test-Path -LiteralPath $Candidate)) {
        return
    }

    $resolvedStaging = [IO.Path]::GetFullPath($StagingRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedCandidate = [IO.Path]::GetFullPath($Candidate)
    if (-not $resolvedCandidate.StartsWith($resolvedStaging, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove an MCP staging directory outside its exact root: $resolvedCandidate"
    }

    foreach ($entry in @(Get-Item -LiteralPath $resolvedCandidate -Force) +
        @(Get-ChildItem -LiteralPath $resolvedCandidate -Recurse -Force)) {
        if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to recursively remove an MCP staging tree containing a reparse point: $($entry.FullName)"
        }
    }

    [IO.Directory]::Delete($resolvedCandidate, $true)
}

function Assert-SafeCachePath {
    param(
        [string]$Root,
        [string]$Candidate
    )

    $resolvedRoot = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $resolvedCandidate = [IO.Path]::GetFullPath($Candidate)
    $prefix = $resolvedRoot + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedCandidate.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "MCP bootstrap cache path escapes the exact repository: $resolvedCandidate"
    }

    $relative = $resolvedCandidate.Substring($prefix.Length)
    $cursor = $resolvedRoot
    foreach ($segment in $relative.Split([char[]]@('\', '/'), [StringSplitOptions]::RemoveEmptyEntries)) {
        $cursor = Join-Path $cursor $segment
        if (-not (Test-Path -LiteralPath $cursor)) {
            break
        }

        $item = Get-Item -LiteralPath $cursor -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "MCP bootstrap cache path contains a reparse point: $($item.FullName)"
        }
    }
}

function Clear-OrphanedStagingDirectories {
    param([string]$StagingRoot)

    foreach ($candidate in Get-ChildItem -LiteralPath $StagingRoot -Force -Directory) {
        if ($candidate.Name -notmatch '^\d+-[0-9a-f]{32}$') {
            Write-Diagnostic "Ignoring unrecognized MCP staging entry: $($candidate.FullName)"
            continue
        }

        if (($candidate.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to remove a reparse point from MCP staging: $($candidate.FullName)"
        }

        Remove-StagingDirectory -StagingRoot $StagingRoot -Candidate $candidate.FullName
    }
}

function Publish-SourceBuild {
    param(
        [string]$Root,
        [string]$CacheRoot
    )

    $cacheDirectory = Join-Path $CacheRoot 'cache'
    $stagingRoot = Join-Path $CacheRoot 'staging'
    Assert-SafeCachePath -Root $Root -Candidate $cacheDirectory
    Assert-SafeCachePath -Root $Root -Candidate $stagingRoot
    [IO.Directory]::CreateDirectory($cacheDirectory) | Out-Null
    [IO.Directory]::CreateDirectory($stagingRoot) | Out-Null
    Assert-SafeCachePath -Root $Root -Candidate $cacheDirectory
    Assert-SafeCachePath -Root $Root -Candidate $stagingRoot

    $lease = Enter-BuildLease -Path (Join-Path $CacheRoot 'source-build.lock') -Timeout ([TimeSpan]::FromSeconds(80))
    $stage = $null
    try {
        Clear-OrphanedStagingDirectories -StagingRoot $stagingRoot
        $snapshot = Get-SourceSnapshot -Root $Root
        $published = Join-Path $cacheDirectory $snapshot.fingerprint
        Assert-SafeCachePath -Root $Root -Candidate $published
        if (Test-PublishedEntry -Directory $published -Fingerprint $snapshot.fingerprint) {
            return $published
        }

        if (Test-Path -LiteralPath $published) {
            $quarantine = Join-Path $CacheRoot ("invalid-{0}-{1}" -f $snapshot.fingerprint, [Guid]::NewGuid().ToString('N'))
            [IO.Directory]::Move($published, $quarantine)
        }

        $stage = Join-Path $stagingRoot ("{0}-{1}" -f $PID, [Guid]::NewGuid().ToString('N'))
        $outputBase = Join-Path $stage 'out'
        $intermediateBase = Join-Path $stage 'obj'
        [IO.Directory]::CreateDirectory($stage) | Out-Null

        $project = Join-Path $Root 'tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'
        $buildArguments = @(
            'build', $project,
            '--configuration', 'Release',
            '--nologo',
            '--verbosity', 'minimal',
            ('-p:BaseOutputPath={0}{1}' -f $outputBase, [IO.Path]::DirectorySeparatorChar),
            ('-p:BaseIntermediateOutputPath={0}{1}' -f $intermediateBase, [IO.Path]::DirectorySeparatorChar)
        )
        $buildExitCode = Invoke-BoundedDotNetBuild -Arguments $buildArguments -Timeout ([TimeSpan]::FromSeconds($BuildTimeoutSeconds))
        if ($buildExitCode -ne 0) {
            throw "The MCP source build failed with exit code $buildExitCode."
        }

        $assemblies = @(Get-ChildItem -LiteralPath $outputBase -Recurse -File -Filter 'RimWorldModding.Mcp.dll')
        if ($assemblies.Count -ne 1) {
            throw "The MCP source build produced $($assemblies.Count) candidate assemblies; expected exactly one."
        }

        $binaryRoot = $assemblies[0].DirectoryName
        $afterBuild = Get-SourceSnapshot -Root $Root
        if ($afterBuild.fingerprint -ne $snapshot.fingerprint) {
            throw 'MCP source/build inputs changed while the bootstrap was building; retry from the new fingerprint.'
        }

        $files = @(Get-OutputInventory -Directory $binaryRoot)
        $manifest = [pscustomobject]@{
            schemaVersion = 1
            fingerprint = $snapshot.fingerprint
            sdkVersion = $snapshot.sdkVersion
            inputs = $snapshot.inputs
            files = $files
        }
        $manifestJson = $manifest | ConvertTo-Json -Depth 8 -Compress
        [IO.File]::WriteAllText(
            (Join-Path $binaryRoot 'bootstrap-manifest.json'),
            $manifestJson,
            [Text.UTF8Encoding]::new($false))

        if (-not (Test-PublishedEntry -Directory $binaryRoot -Fingerprint $snapshot.fingerprint)) {
            throw 'The staged MCP source build failed its exact manifest verification.'
        }

        [IO.Directory]::Move($binaryRoot, $published)
        Assert-SafeCachePath -Root $Root -Candidate $published
        if (-not (Test-PublishedEntry -Directory $published -Fingerprint $snapshot.fingerprint)) {
            throw 'The published MCP source build failed its exact manifest verification.'
        }

        return $published
    }
    finally {
        if ($null -ne $stage) {
            try { Remove-StagingDirectory -StagingRoot $stagingRoot -Candidate $stage } catch { Write-Diagnostic $_.Exception.Message }
        }

        $lease.Dispose()
    }
}

try {
    $root = Get-CanonicalRoot -Candidate $RepositoryRoot
    $cacheRoot = Join-Path $root 'artifacts\HostTools\RimWorldModding.Mcp\SourceBootstrap'
    Assert-SafeCachePath -Root $root -Candidate $cacheRoot
    [IO.Directory]::CreateDirectory($cacheRoot) | Out-Null
    Assert-SafeCachePath -Root $root -Candidate $cacheRoot
    $binaryRoot = Publish-SourceBuild -Root $root -CacheRoot $cacheRoot
    $assembly = Join-Path $binaryRoot 'RimWorldModding.Mcp.dll'

    & dotnet $assembly serve --repository-root $root
    exit $LASTEXITCODE
}
catch {
    Write-Diagnostic ("RimWorldModding MCP bootstrap failed: {0}" -f $_.Exception.GetBaseException().Message)
    exit 1
}
