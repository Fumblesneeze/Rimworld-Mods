<#
.SYNOPSIS
Verifies Immersive Chefs in RimWorld with an isolated minimal mod list.

.DESCRIPTION
Builds and deploys only Immersive Chefs, launches RimWorld with an isolated saved-data folder and
dedicated log, verifies its window through FlaUI, captures a screenshot, checks the startup marker,
and closes only the process it launched. Use -DryRun to create and inspect the isolated configuration
without building, deploying, starting FlaUI, or launching RimWorld.

Exit code 0 means success; 1 means build/runtime/verification failure; 2 means a path or semantic
input rejected after parameter binding. PowerShell rejects invalid ValidateRange or ValidateSet
values before the script runs and reports its own nonzero parameter-binding exit (normally 1).

.EXAMPLE
.\scripts\Invoke-RimWorldSmoke.ps1 -DryRun -Output json

.EXAMPLE
.\scripts\Invoke-RimWorldSmoke.ps1 -TimeoutSeconds 180
#>
[CmdletBinding()]
param(
    [string]$RimWorldPath = 'F:\Steam\steamapps\common\RimWorld',

    [string]$SteamModContentFolder = 'F:\Steam\steamapps\workshop\content\294100',

    [string]$ArtifactsPath,

    [ValidateRange(30, 600)]
    [int]$TimeoutSeconds = 180,

    [switch]$DryRun,

    [ValidateSet('table', 'json')]
    [string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$activeModIds = @(
    'brrainz.harmony',
    'ludeon.rimworld',
    'imranfish.xmlextensions',
    'fumblesneeze.immersivechefs'
)

$knownExpansionIds = @(
    'ludeon.rimworld.royalty',
    'ludeon.rimworld.ideology',
    'ludeon.rimworld.biotech',
    'ludeon.rimworld.anomaly',
    'ludeon.rimworld.odyssey'
)

function Write-Result {
    param([pscustomobject]$Result)

    if ($Output -eq 'json') {
        $Result | ConvertTo-Json -Compress
        return
    }

    $Result | Format-List
}

function Exit-InvalidInput {
    param([string]$Message)

    [Console]::Error.WriteLine($Message)
    exit 2
}

function Get-OptionalFileHash {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Write-MinimalModsConfig {
    param(
        [string]$Path,
        [string]$Version
    )

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)

    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $writer.WriteStartDocument()
        $writer.WriteStartElement('ModsConfigData')
        $writer.WriteElementString('version', $Version)
        $writer.WriteStartElement('activeMods')
        foreach ($packageId in $activeModIds) {
            $writer.WriteElementString('li', $packageId)
        }
        $writer.WriteEndElement()
        $writer.WriteStartElement('knownExpansions')
        foreach ($packageId in $knownExpansionIds) {
            $writer.WriteElementString('li', $packageId)
        }
        $writer.WriteEndElement()
        $writer.WriteEndElement()
        $writer.WriteEndDocument()
    }
    finally {
        $writer.Dispose()
    }
}

function Invoke-FlaUiJson {
    param([string[]]$Arguments)

    $lines = @(& flaui @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    $text = ($lines -join [Environment]::NewLine).Trim()

    if ($exitCode -ne 0) {
        throw "FlaUI command failed (exit $exitCode): flaui $($Arguments -join ' ')`n$text"
    }

    try {
        $payload = $text | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "FlaUI returned invalid JSON for 'flaui $($Arguments -join ' ')': $text"
    }

    if ($payload.PSObject.Properties.Name -contains 'success' -and -not $payload.success) {
        $errorText = if ($payload.PSObject.Properties.Name -contains 'error') { $payload.error } else { $text }
        throw "FlaUI command reported failure: $errorText"
    }

    return $payload
}

function Stop-OwnedDumpHelper {
    param(
        [Parameter(Mandatory)][System.Diagnostics.Process]$Helper,
        [Parameter(Mandatory)][string]$PartialDumpPath
    )

    $helperProcessId = $Helper.Id
    $helperProcessStartUtc = $Helper.StartTime.ToUniversalTime().ToString(
        'O',
        [Globalization.CultureInfo]::InvariantCulture)
    try {
        $Helper.Kill($true)
    }
    catch [System.InvalidOperationException] {
        $Helper.Refresh()
        if (-not $Helper.HasExited) { throw }
    }
    catch {
        return [pscustomobject]@{
            Status = 'cleanup-failed'
            Path = $PartialDumpPath
            Detail = "Native dump helper termination failed: $($_.Exception.Message)"
            HelperProcessId = $helperProcessId
            HelperProcessStartUtc = $helperProcessStartUtc
            HelperAlive = $true
        }
    }
    if (-not $Helper.HasExited -and -not $Helper.WaitForExit(5000)) {
        return [pscustomobject]@{
            Status = 'cleanup-failed'
            Path = $PartialDumpPath
            Detail = 'Native dump helper did not exit after bounded termination.'
            HelperProcessId = $helperProcessId
            HelperProcessStartUtc = $helperProcessStartUtc
            HelperAlive = $true
        }
    }
    try {
        if (Test-Path -LiteralPath $PartialDumpPath -PathType Leaf) {
            Remove-Item -LiteralPath $PartialDumpPath -Force
        }
    }
    catch {
        return [pscustomobject]@{
            Status = 'cleanup-failed'
            Path = $PartialDumpPath
            Detail = "Timed-out native dump helper partial cleanup failed: $($_.Exception.Message)"
            HelperProcessId = $helperProcessId
            HelperProcessStartUtc = $helperProcessStartUtc
            HelperAlive = $false
        }
    }
    if (Test-Path -LiteralPath $PartialDumpPath -PathType Leaf) {
        return [pscustomobject]@{
            Status = 'cleanup-failed'
            Path = $PartialDumpPath
            Detail = 'Timed-out native dump helper left a partial artifact.'
            HelperProcessId = $helperProcessId
            HelperProcessStartUtc = $helperProcessStartUtc
            HelperAlive = $false
        }
    }

    return [pscustomobject]@{
        Status = 'timed-out'
        Path = $null
        Detail = 'Native dump helper exceeded 20 seconds.'
        HelperProcessId = $helperProcessId
        HelperProcessStartUtc = $helperProcessStartUtc
        HelperAlive = $false
    }
}

function Capture-OwnedProcessDump {
    param(
        [Parameter(Mandatory)][System.Diagnostics.Process]$Process,
        [Parameter(Mandatory)][string]$DumpPath
    )

    $helperPath = $null
    $temporaryDumpPath = $null
    $helper = $null
    try {
        $dumpDirectory = Split-Path -Parent $DumpPath
        if ([string]::IsNullOrWhiteSpace($dumpDirectory) -or
            -not (Test-Path -LiteralPath $dumpDirectory -PathType Container)) {
            return [pscustomobject]@{ Status = 'failed'; Path = $null; Detail = 'The dump destination directory does not exist.' }
        }
        if (Test-Path -LiteralPath $DumpPath -PathType Leaf) {
            Remove-Item -LiteralPath $DumpPath -Force
        }

        $helperPath = Join-Path $dumpDirectory ('.rimworld-minidump-' + [guid]::NewGuid().ToString('N') + '.ps1')
        $temporaryDumpPath = Join-Path $dumpDirectory ('.rimworld-minidump-' + [guid]::NewGuid().ToString('N') + '.tmp')
        $helperSource = @'
param(
    [Parameter(Mandatory)][int]$TargetProcessId,
    [Parameter(Mandatory)][long]$TargetProcessStartUtcTicks,
    [Parameter(Mandatory)][string]$TargetDumpPath
)
$ErrorActionPreference = 'Stop'
$nativeSource = @"
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RimWorldDevGateway.HostDiagnostics
{
    [Flags]
    public enum MiniDumpType : uint
    {
        WithHandleData = 0x00000004,
        WithUnloadedModules = 0x00000020,
        WithProcessThreadData = 0x00000100,
        WithFullMemoryInfo = 0x00000800,
        WithThreadInfo = 0x00001000
    }

    public static class NativeMiniDump
    {
        [DllImport("Dbghelp.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MiniDumpWriteDump(
            IntPtr processHandle,
            uint processId,
            SafeFileHandle fileHandle,
            MiniDumpType dumpType,
            IntPtr exceptionParam,
            IntPtr userStreamParam,
            IntPtr callbackParam);
    }
}
"@
Add-Type -TypeDefinition $nativeSource -Language CSharp
$target = [Diagnostics.Process]::GetProcessById($TargetProcessId)
$stream = $null
try {
    if ($target.StartTime.ToUniversalTime().Ticks -ne $TargetProcessStartUtcTicks) {
        [Console]::Error.WriteLine('Target process start identity changed before dump capture.')
        exit 87
    }
    $stream = [IO.File]::Open(
        $TargetDumpPath,
        [IO.FileMode]::CreateNew,
        [IO.FileAccess]::Write,
        [IO.FileShare]::None)
    $dumpType = [RimWorldDevGateway.HostDiagnostics.MiniDumpType]::WithHandleData -bor
        [RimWorldDevGateway.HostDiagnostics.MiniDumpType]::WithUnloadedModules -bor
        [RimWorldDevGateway.HostDiagnostics.MiniDumpType]::WithProcessThreadData -bor
        [RimWorldDevGateway.HostDiagnostics.MiniDumpType]::WithFullMemoryInfo -bor
        [RimWorldDevGateway.HostDiagnostics.MiniDumpType]::WithThreadInfo
    $captured = [RimWorldDevGateway.HostDiagnostics.NativeMiniDump]::MiniDumpWriteDump(
        $target.Handle,
        [uint32]$TargetProcessId,
        $stream.SafeFileHandle,
        $dumpType,
        [IntPtr]::Zero,
        [IntPtr]::Zero,
        [IntPtr]::Zero)
    if (-not $captured) {
        $errorCode = [Runtime.InteropServices.Marshal]::GetLastWin32Error()
        [Console]::Error.WriteLine("MiniDumpWriteDump failed with Win32 error $errorCode.")
        exit 86
    }
    $stream.Flush($true)
}
finally {
    if ($null -ne $stream) { $stream.Dispose() }
    $target.Dispose()
}
exit 0
'@
        [IO.File]::WriteAllText(
            $helperPath,
            $helperSource,
            [Text.UTF8Encoding]::new($false))

        $pwsh = Join-Path $PSHOME 'pwsh.exe'
        if (-not (Test-Path -LiteralPath $pwsh -PathType Leaf)) {
            return [pscustomobject]@{ Status = 'unavailable'; Path = $null; Detail = "pwsh.exe was not found at $pwsh" }
        }

        $startInfo = [Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = $pwsh
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.ArgumentList.Add('-NoProfile')
        $startInfo.ArgumentList.Add('-NonInteractive')
        $startInfo.ArgumentList.Add('-ExecutionPolicy')
        $startInfo.ArgumentList.Add('Bypass')
        $startInfo.ArgumentList.Add('-File')
        $startInfo.ArgumentList.Add($helperPath)
        $startInfo.ArgumentList.Add('-TargetProcessId')
        $startInfo.ArgumentList.Add([string]$Process.Id)
        $startInfo.ArgumentList.Add('-TargetProcessStartUtcTicks')
        $startInfo.ArgumentList.Add([string]$Process.StartTime.ToUniversalTime().Ticks)
        $startInfo.ArgumentList.Add('-TargetDumpPath')
        $startInfo.ArgumentList.Add($temporaryDumpPath)
        $helper = [Diagnostics.Process]::Start($startInfo)
        if ($null -eq $helper) {
            return [pscustomobject]@{ Status = 'failed'; Path = $null; Detail = 'The native dump helper did not start.' }
        }
        if (-not $helper.WaitForExit(20000)) {
            return Stop-OwnedDumpHelper -Helper $helper -PartialDumpPath $temporaryDumpPath
        }

        $helperError = $helper.StandardError.ReadToEnd().Trim()
        $hasMiniDumpSignature = $false
        if (Test-Path -LiteralPath $temporaryDumpPath -PathType Leaf) {
            $signatureStream = [System.IO.File]::OpenRead($temporaryDumpPath)
            try {
                $signatureBytes = [byte[]]::new(4)
                $hasMiniDumpSignature =
                    $signatureStream.Read($signatureBytes, 0, $signatureBytes.Length) -eq 4 -and
                    [System.Text.Encoding]::ASCII.GetString($signatureBytes) -ceq 'MDMP'
            }
            finally {
                $signatureStream.Dispose()
            }
        }
        if ($helper.ExitCode -eq 0 -and
            (Test-Path -LiteralPath $temporaryDumpPath -PathType Leaf) -and
            (Get-Item -LiteralPath $temporaryDumpPath).Length -ge 4 -and
            $hasMiniDumpSignature) {
            [System.IO.File]::Move($temporaryDumpPath, $DumpPath)
            $temporaryDumpPath = $null
            return [pscustomobject]@{
                Status = 'captured'
                Path = $DumpPath
                Detail = 'Native MiniDumpWriteDump completed.'
                TargetProcessId = [int]$Process.Id
                TargetProcessStartUtc = $Process.StartTime.ToUniversalTime().ToString(
                    'O',
                    [Globalization.CultureInfo]::InvariantCulture)
            }
        }

        if (Test-Path -LiteralPath $temporaryDumpPath -PathType Leaf) {
            Remove-Item -LiteralPath $temporaryDumpPath -Force
        }
        return [pscustomobject]@{
            Status = 'failed'
            Path = $null
            Detail = if ([string]::IsNullOrWhiteSpace($helperError)) {
                "Native dump helper exited with code $($helper.ExitCode)."
            } else {
                "Native dump helper exited with code $($helper.ExitCode): $helperError"
            }
        }
    }
    catch {
        if ($null -ne $temporaryDumpPath -and
            (Test-Path -LiteralPath $temporaryDumpPath -PathType Leaf)) {
            Remove-Item -LiteralPath $temporaryDumpPath -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path -LiteralPath $DumpPath -PathType Leaf) {
            Remove-Item -LiteralPath $DumpPath -Force -ErrorAction SilentlyContinue
        }
        return [pscustomobject]@{ Status = 'failed'; Path = $null; Detail = $_.Exception.Message }
    }
    finally {
        if ($null -ne $helper) {
            $helper.Dispose()
        }
        if ($null -ne $helperPath -and (Test-Path -LiteralPath $helperPath -PathType Leaf)) {
            Remove-Item -LiteralPath $helperPath -Force -ErrorAction SilentlyContinue
        }
        if ($null -ne $temporaryDumpPath -and
            (Test-Path -LiteralPath $temporaryDumpPath -PathType Leaf)) {
            Remove-Item -LiteralPath $temporaryDumpPath -Force -ErrorAction SilentlyContinue
        }
    }
}

function Stop-OwnedProcessGracefully {
    param(
        [Parameter(Mandatory)][System.Diagnostics.Process]$Process,
        [Parameter(Mandatory)][string]$DumpPath
    )

    $Process.Refresh()
    if ($Process.HasExited) {
        return [pscustomobject]@{
            Method = 'already-exited'
            CloseRequested = $false
            Forced = $false
            ExitCode = $Process.ExitCode
            Dump = $null
        }
    }

    $closeRequested = $Process.CloseMainWindow()
    if ($Process.WaitForExit(15000)) {
        return [pscustomobject]@{
            Method = 'window-close'
            CloseRequested = $closeRequested
            Forced = $false
            ExitCode = $Process.ExitCode
            Dump = $null
        }
    }

    $dump = Capture-OwnedProcessDump -Process $Process -DumpPath $DumpPath
    $Process.Refresh()
    if ($Process.HasExited) {
        return [pscustomobject]@{
            Method = 'exited-during-dump'
            CloseRequested = $closeRequested
            Forced = $false
            ExitCode = $Process.ExitCode
            Dump = $dump
        }
    }

    try {
        $Process.Kill()
    }
    catch [System.InvalidOperationException] {
        $Process.Refresh()
        if (-not $Process.HasExited) {
            throw
        }
    }
    if (-not $Process.HasExited) {
        $null = $Process.WaitForExit(15000)
    }
    $Process.Refresh()
    if (-not $Process.HasExited) {
        throw "Owned RimWorld PID $($Process.Id) remained alive after the exact-PID force fallback."
    }

    return [pscustomobject]@{
        Method = 'force-fallback'
        CloseRequested = $closeRequested
        Forced = $true
        ExitCode = $Process.ExitCode
        Dump = $dump
    }
}

if (-not (Test-Path -LiteralPath $RimWorldPath -PathType Container)) {
    Exit-InvalidInput "RimWorld path does not exist: $RimWorldPath"
}

if (-not (Test-Path -LiteralPath $SteamModContentFolder -PathType Container)) {
    Exit-InvalidInput "Steam Workshop content path does not exist: $SteamModContentFolder"
}

$resolvedRimWorldPath = (Resolve-Path -LiteralPath $RimWorldPath).Path
$resolvedWorkshopPath = (Resolve-Path -LiteralPath $SteamModContentFolder).Path
$rimWorldExecutable = Join-Path $resolvedRimWorldPath 'RimWorldWin64.exe'
$versionPath = Join-Path $resolvedRimWorldPath 'Version.txt'
$managedPath = Join-Path $resolvedRimWorldPath 'RimWorldWin64_Data\Managed'

if (-not (Test-Path -LiteralPath $rimWorldExecutable -PathType Leaf)) {
    Exit-InvalidInput "RimWorld executable does not exist: $rimWorldExecutable"
}

if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
    Exit-InvalidInput "RimWorld version file does not exist: $versionPath"
}

$rimWorldVersion = (Get-Content -LiteralPath $versionPath -Raw).Trim()
if (-not $rimWorldVersion.StartsWith('1.6.', [StringComparison]::Ordinal)) {
    Exit-InvalidInput "Immersive Chefs baseline supports RimWorld 1.6; installed version is '$rimWorldVersion'."
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    $ArtifactsPath = Join-Path $repositoryRoot 'artifacts\RimWorldSmoke'
}

$artifactRoot = [System.IO.Path]::GetFullPath($ArtifactsPath)
$runId = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ', [Globalization.CultureInfo]::InvariantCulture)
$runDirectory = Join-Path $artifactRoot $runId
$savedDataPath = Join-Path $runDirectory 'SavedData'
$configDirectory = Join-Path $savedDataPath 'Config'
$modsConfigPath = Join-Path $configDirectory 'ModsConfig.xml'
$playerLogPath = Join-Path $runDirectory 'Player.log'
$screenshotPath = Join-Path $runDirectory 'main-menu.png'
$buildLogPath = Join-Path $runDirectory 'dotnet-build.log'
$processCleanupPath = Join-Path $runDirectory 'process-cleanup.json'
$hangDumpPath = Join-Path $runDirectory 'RimWorldWin64-hang.dmp'
$null = New-Item -Path $configDirectory -ItemType Directory -Force
Write-MinimalModsConfig -Path $modsConfigPath -Version $rimWorldVersion

$normalModsConfigPath = Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml'
$normalConfigHashBefore = Get-OptionalFileHash -Path $normalModsConfigPath
$launchArguments = @(
    "-savedatafolder=`"$savedDataPath`"",
    '-logFile',
    "`"$playerLogPath`""
)

if ($DryRun) {
    $normalConfigHashAfter = Get-OptionalFileHash -Path $normalModsConfigPath
    Write-Result ([pscustomobject]@{
        Status = 'dry-run'
        Version = $rimWorldVersion
        ActiveMods = $activeModIds
        ModsConfig = $modsConfigPath
        PlayerLog = $playerLogPath
        Screenshot = $screenshotPath
        ProcessCleanup = $processCleanupPath
        HangDump = $hangDumpPath
        LaunchArguments = $launchArguments
        NormalConfigHashBefore = $normalConfigHashBefore
        NormalConfigHashAfter = $normalConfigHashAfter
    })
    exit 0
}

if ($null -eq (Get-Command flaui -ErrorAction SilentlyContinue)) {
    Exit-InvalidInput 'FlaUI CLI is not installed or not available on PATH.'
}

$existingRimWorld = @(Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue)
if ($existingRimWorld.Count -gt 0) {
    Exit-InvalidInput "RimWorld is already running (PID(s): $($existingRimWorld.Id -join ', ')). Close it before smoke verification."
}

$launchedProcess = $null
$connected = $false
$serviceStartedByScript = $false
$result = $null
$failureMessage = $null

try {
    $projectPath = Join-Path $repositoryRoot 'mods\ImmersiveChefs\ImmersiveChefs.csproj'
    $buildArguments = @(
        'build', $projectPath,
        '--configuration', 'Release',
        "-p:DeployToGame=true",
        "-p:RimWorldPath=$resolvedRimWorldPath",
        "-p:RimWorldManagedPath=$managedPath",
        "-p:SteamModContentFolder=$resolvedWorkshopPath"
    )
    $buildOutput = @(& dotnet @buildArguments 2>&1)
    $buildExitCode = $LASTEXITCODE
    $buildOutput | Set-Content -LiteralPath $buildLogPath -Encoding UTF8
    if ($buildExitCode -ne 0) {
        throw "Mod build/deploy failed with exit $buildExitCode. See $buildLogPath"
    }

    $deployedAbout = Join-Path $resolvedRimWorldPath 'Mods\fumblesneeze.immersivechefs\About\About.xml'
    if (-not (Test-Path -LiteralPath $deployedAbout -PathType Leaf)) {
        throw "ModSdk did not deploy Immersive Chefs to $deployedAbout"
    }

    $launchedProcess = Start-Process -FilePath $rimWorldExecutable -ArgumentList $launchArguments -PassThru
    $deadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
    $startupMarker = '[ImmersiveChefs] Initialized fumblesneeze.immersivechefs'
    $markerFound = $false

    while ([datetime]::UtcNow -lt $deadline) {
        $launchedProcess.Refresh()
        if ($launchedProcess.HasExited) {
            throw "RimWorld exited before verification with code $($launchedProcess.ExitCode). See $playerLogPath"
        }

        if (Test-Path -LiteralPath $playerLogPath -PathType Leaf) {
            $markerFound = $null -ne (Select-String -LiteralPath $playerLogPath -SimpleMatch $startupMarker -Quiet)
        }

        if ($markerFound -and $launchedProcess.MainWindowHandle -ne 0) {
            break
        }

        Start-Sleep -Seconds 1
    }

    if (-not $markerFound) {
        throw "Timed out after $TimeoutSeconds seconds waiting for the Immersive Chefs startup marker. See $playerLogPath"
    }

    Start-Sleep -Seconds 8

    $serviceStart = Invoke-FlaUiJson -Arguments @('service', 'start')
    $serviceStartedByScript = $serviceStart.message -eq 'Service started successfully'
    $statusBefore = Invoke-FlaUiJson -Arguments @('status')
    if ($statusBefore.data.connected) {
        throw "FlaUI is already connected to PID $($statusBefore.data.processId); refusing to disrupt that session."
    }

    $null = Invoke-FlaUiJson -Arguments @('connect', '--pid', $launchedProcess.Id.ToString([Globalization.CultureInfo]::InvariantCulture))
    $connected = $true
    $status = Invoke-FlaUiJson -Arguments @('status')
    if (-not $status.data.connected -or [int]$status.data.processId -ne $launchedProcess.Id) {
        throw 'FlaUI did not remain connected to the launched RimWorld process.'
    }

    $windows = Invoke-FlaUiJson -Arguments @('window', 'list')
    $windowRows = @($windows.data)
    $launchedProcess.Refresh()
    if ($launchedProcess.MainWindowHandle -eq 0 -or [string]::IsNullOrWhiteSpace($launchedProcess.MainWindowTitle)) {
        throw 'The launched RimWorld process does not expose a visible native main window.'
    }

    $null = Invoke-FlaUiJson -Arguments @('screenshot', '--output', $screenshotPath)
    if (-not (Test-Path -LiteralPath $screenshotPath -PathType Leaf) -or (Get-Item -LiteralPath $screenshotPath).Length -eq 0) {
        throw "FlaUI did not create a non-empty screenshot at $screenshotPath"
    }

    $markerCount = @(Select-String -LiteralPath $playerLogPath -SimpleMatch $startupMarker).Count
    if ($markerCount -ne 1) {
        throw "Expected one startup marker, found $markerCount. See $playerLogPath"
    }

    $modErrors = @(Select-String -LiteralPath $playerLogPath -Pattern '(?i)\[ImmersiveChefs\].*(error|exception)')
    if ($modErrors.Count -gt 0) {
        throw "Player log contains an Immersive Chefs error. See $playerLogPath"
    }

    $result = [pscustomobject]@{
        Status = 'passed'
        Version = $rimWorldVersion
        ProcessId = $launchedProcess.Id
        ActiveMods = $activeModIds
        WindowTitle = $launchedProcess.MainWindowTitle
        FlaUiWindowCount = $windowRows.Count
        MarkerCount = $markerCount
        ModsConfig = $modsConfigPath
        PlayerLog = $playerLogPath
        Screenshot = $screenshotPath
        BuildLog = $buildLogPath
        ProcessCleanup = $processCleanupPath
        HangDump = $hangDumpPath
        NormalConfigHashBefore = $normalConfigHashBefore
        NormalConfigHashAfter = $null
    }
}
catch {
    $failureMessage = $_.Exception.Message
}
finally {
    if ($connected) {
        $null = @(& flaui disconnect 2>&1)
    }

    if ($serviceStartedByScript) {
        $null = @(& flaui service stop 2>&1)
        Start-Sleep -Milliseconds 250
        $null = @(& flaui service stop 2>&1)
    }

    if ($null -ne $launchedProcess) {
        try {
            $processCleanup = Stop-OwnedProcessGracefully `
                -Process $launchedProcess `
                -DumpPath $hangDumpPath
            $processCleanup | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $processCleanupPath -Encoding UTF8
            if ($null -ne $processCleanup.Dump -and
                [string]$processCleanup.Dump.Status -ceq 'cleanup-failed' -and
                $null -eq $failureMessage) {
                $failureMessage = "Native dump helper cleanup failed for PID $($processCleanup.Dump.HelperProcessId) started $($processCleanup.Dump.HelperProcessStartUtc): $($processCleanup.Dump.Detail)"
            }
        }
        catch {
            if ($null -eq $failureMessage) {
                $failureMessage = "Owned RimWorld process cleanup failed: $($_.Exception.Message)"
            }

            [pscustomobject]@{
                Method = 'cleanup-failed'
                CloseRequested = $null
                Forced = $null
                ExitCode = $null
                Error = $_.Exception.ToString()
            } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $processCleanupPath -Encoding UTF8
        }
    }
}

$normalConfigHashAfter = Get-OptionalFileHash -Path $normalModsConfigPath
if ($normalConfigHashBefore -ne $normalConfigHashAfter) {
    $failureMessage = "Normal RimWorld ModsConfig.xml changed during isolated verification. Before=$normalConfigHashBefore After=$normalConfigHashAfter"
}

if ($null -ne $failureMessage) {
    [Console]::Error.WriteLine($failureMessage)
    [Console]::Error.WriteLine("Smoke artifacts: $runDirectory")
    exit 1
}

$result.NormalConfigHashAfter = $normalConfigHashAfter
Write-Result $result
exit 0
