Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Shared read-only preflight for grouped dry-runs and the launcher's disposable copy.
function Get-RimWorldPrelaunchConfigFiles {
    param([string]$SourceDirectory)
    if ([string]::IsNullOrWhiteSpace($SourceDirectory)) { return @() }
    $sourceRoot = [IO.Path]::GetFullPath($SourceDirectory)
    if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
        throw "Prelaunch config directory does not exist: $sourceRoot"
    }
    $ancestor = Get-Item -LiteralPath $sourceRoot -Force
    while ($null -ne $ancestor) {
        if (($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'Prelaunch config ancestors cannot be reparse points.'
        }
        $ancestor = $ancestor.Parent
    }
    $entries = @(Get-ChildItem -LiteralPath $sourceRoot -Force | Sort-Object Name)
    if ($entries.Count -gt 64) { throw 'Prelaunch config exceeds 64 files.' }
    $totalBytes = [long]0
    foreach ($entry in $entries) {
        if ($entry.PSIsContainer -or ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Prelaunch config accepts only direct ordinary XML files: $($entry.Name)"
        }
        if ($entry.Name -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}\.xml$') {
            throw "Invalid prelaunch config file name: $($entry.Name)"
        }
        if ($entry.Name -ieq 'Prefs.xml' -or $entry.Name -ieq 'ModsConfig.xml') {
            throw "Prelaunch config file '$($entry.Name)' is reserved by the isolated launcher."
        }
        if ([long]$entry.Length -gt 16MB) { throw 'Prelaunch config file exceeds 16 MiB.' }
        $totalBytes += [long]$entry.Length
        if ($totalBytes -gt 64MB) { throw 'Prelaunch config exceeds 64 MiB aggregate.' }
        $xmlSettings = [Xml.XmlReaderSettings]::new()
        $xmlSettings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
        $xmlSettings.XmlResolver = $null
        $xmlSettings.MaxCharactersInDocument = 16MB
        $reader = [Xml.XmlReader]::Create($entry.FullName, $xmlSettings)
        try { while ($reader.Read()) { } } finally { $reader.Dispose() }
    }
    return $entries
}

Export-ModuleMember -Function Get-RimWorldPrelaunchConfigFiles
