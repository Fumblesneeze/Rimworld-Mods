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

function Write-JsonUtf8 {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][object]$Value)
    $json = $Value | ConvertTo-Json -Depth 12
    [IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
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
if ($actualRimWorldBuild -cne [string]$release.rimWorldBuild -or
    -not $buildMatch.Success -or
    $buildMatch.Groups['id'].Value -cne [string]$release.steamBuildId -or
    $actualManagedHash -cne [string]$release.managedAssemblySha256) {
    Exit-InvalidInput 'The installed RimWorld build does not match the pinned release inputs.'
}
if (-not $rawRimWorldBuild.StartsWith('1.6.4871 rev', [StringComparison]::Ordinal)) {
    Exit-InvalidInput 'Version.txt does not identify the pinned RimWorld 1.6.4871 build line.'
}
if ($null -eq $release.publishedFileId -and -not [bool]$release.allowFirstPublication) {
    Exit-InvalidInput 'The descriptor has no Workshop item and does not allow first publication.'
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
if ($null -ne $release.publishedFileId) {
    $publishedIdentity = [string]$release.publishedFileId
    $parsedIdentity = 0UL
    if (-not [ulong]::TryParse($publishedIdentity, [ref]$parsedIdentity) -or $parsedIdentity -eq 0) {
        Exit-InvalidInput 'The release descriptor Workshop identity is invalid.'
    }
    [IO.File]::WriteAllText(
        (Join-Path $packageRoot 'About\PublishedFileId.txt'),
        $publishedIdentity,
        [Text.UTF8Encoding]::new($false))
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
$previewSource = Join-Path $releaseRoot ([string]$release.preview)
Copy-Item -LiteralPath $descriptionSource -Destination (Join-Path $presentationRoot 'description.bbcode')
Copy-Item -LiteralPath $previewSource -Destination (Join-Path $presentationRoot 'preview-main.png')

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
$plan = [pscustomobject][ordered]@{
    schema = 'ImmersiveChefs/WorkshopPublicationPlan/v1'
    sourceRevision = $revision
    releaseDescriptorSha256 = (Get-FileHash -LiteralPath $descriptorPath -Algorithm SHA256).Hash
    steamAppId = 294100
    steamUserId = [string]$release.steamUserId
    publishedFileId = $release.publishedFileId
    allowFirstPublication = [bool]$release.allowFirstPublication
    packageId = [string]$release.packageId
    title = [string]$release.title
    author = [string]$release.author
    rimWorldVersion = [string]$release.rimWorldVersion
    rimWorldBuild = [string]$release.rimWorldBuild
    steamBuildId = [string]$release.steamBuildId
    managedAssemblySha256 = [string]$release.managedAssemblySha256
    visibility = [string]$release.visibility
    tags = @($release.tags)
    requiredWorkshopItems = @($release.requiredWorkshopItems)
    changeNote = [string]$release.changeNote
    packagePath = $packageRoot
    packageManifestPath = $manifestPath
    candidateSha256 = $candidateDigest
    packageFileCount = $files.Count
    packageBytes = [long](($files | Measure-Object bytes -Sum).Sum)
    generatedAtPublication = @('About\PublishedFileId.txt')
    descriptionPath = $descriptionPath
    descriptionBytes = (Get-Item -LiteralPath $descriptionPath).Length
    descriptionSha256 = (Get-FileHash -LiteralPath $descriptionPath -Algorithm SHA256).Hash
    previewPath = $previewPath
    previewBytes = (Get-Item -LiteralPath $previewPath).Length
    previewSha256 = (Get-FileHash -LiteralPath $previewPath -Algorithm SHA256).Hash
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
