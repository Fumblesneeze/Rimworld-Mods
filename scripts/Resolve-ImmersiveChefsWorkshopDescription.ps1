<#
.SYNOPSIS
Resolves reviewed Steam additional-preview URLs into the Immersive Chefs Workshop template.

.DESCRIPTION
Requires the exact six-token Steam inventory, rejects unexpected hosts/tokens/indexes, writes a
new UTF-8 BBCode file, and enforces Steamworks' installed exact description byte ceiling.

.EXAMPLE
.\scripts\Resolve-ImmersiveChefsWorkshopDescription.ps1 `
  -InventoryPath .\artifacts\...\presentation-preview-inventory.json `
  -DestinationPath .\mods\ImmersiveChefs\Release\workshop\description.bbcode -Output json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$InventoryPath,
    [string]$TemplatePath,
    [Parameter(Mandatory)][string]$DestinationPath,
    [string]$RimWorldPath = 'F:\Steam\steamapps\common\RimWorld',
    [ValidateSet('table', 'json')][string]$Output = 'table'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Exit-InvalidInput([string]$Message) {
    [Console]::Error.WriteLine($Message)
    exit 2
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($TemplatePath)) {
    $TemplatePath = Join-Path $repositoryRoot 'mods\ImmersiveChefs\Release\workshop\description.template.bbcode'
}
foreach ($path in @($InventoryPath, $TemplatePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Exit-InvalidInput "Required input does not exist: $path" }
}

try { $inventory = Get-Content -LiteralPath $InventoryPath -Raw -Encoding UTF8 | ConvertFrom-Json }
catch { Exit-InvalidInput "The remote preview inventory is invalid JSON: $($_.Exception.Message)" }
if ([string]$inventory.schema -cne 'ImmersiveChefs/WorkshopRemotePreviewInventory/v1') {
    Exit-InvalidInput 'The remote preview inventory schema is invalid.'
}
[ulong]$publishedFileId = 0
$previewPlanSha256 = [string]$inventory.publicationPlanSha256
if (-not [ulong]::TryParse([string]$inventory.publishedFileId, [ref]$publishedFileId) -or $publishedFileId -eq 0 -or
    $previewPlanSha256 -notmatch '^[A-Fa-f0-9]{64}$') {
    Exit-InvalidInput 'The remote preview inventory item/plan identity is invalid.'
}
$expectedTokens = @('kitchenware', 'teamwork', 'dishwashing', 'meals', 'colony', 'compatibility')
$previews = @($inventory.previews)
if ($previews.Count -ne $expectedTokens.Count) { Exit-InvalidInput 'The remote preview inventory must contain exactly six images.' }

$template = Get-Content -LiteralPath $TemplatePath -Raw -Encoding UTF8
for ($index = 0; $index -lt $expectedTokens.Count; $index++) {
    $preview = $previews[$index]
    $token = [string]$preview.token
    $urlText = [string]$preview.remoteUrl
    $localPath = [IO.Path]::GetFullPath([string]$preview.localPath)
    $localHash = [string]$preview.localSha256
    $remoteHash = [string]$preview.remoteSha256
    [Uri]$url = $null
    if ($token -cne $expectedTokens[$index] -or [int]$preview.remoteIndex -ne $index -or
        -not [Uri]::TryCreate($urlText, [UriKind]::Absolute, [ref]$url) -or
        $url.Scheme -cne 'https' -or $url.Host -cne 'images.steamusercontent.com' -or
        [string]$preview.remoteType -cne 'k_EItemPreviewType_Image' -or
        -not (Test-Path -LiteralPath $localPath -PathType Leaf) -or
        $localHash -notmatch '^[A-Fa-f0-9]{64}$' -or $remoteHash -cne $localHash -or
        (Get-FileHash -LiteralPath $localPath -Algorithm SHA256).Hash -cne $localHash) {
        Exit-InvalidInput "Remote preview slot $index does not match its reviewed token and Steam URL."
    }
    $placeholder = '{{image:' + $token + '}}'
    if ([regex]::Matches($template, [regex]::Escape($placeholder)).Count -ne 1) {
        Exit-InvalidInput "The Workshop template must contain token '$token' exactly once."
    }
    $template = $template.Replace($placeholder, $url.AbsoluteUri)
}
if ($template -match '\{\{image:[^}]+\}\}') { Exit-InvalidInput 'The resolved Workshop description still contains an image token.' }

$steamworksPath = Join-Path ([IO.Path]::GetFullPath($RimWorldPath)) 'RimWorldWin64_Data\Managed\com.rlabrecque.steamworks.net.dll'
if (-not (Test-Path -LiteralPath $steamworksPath -PathType Leaf)) { Exit-InvalidInput 'The installed Steamworks assembly is missing.' }
$steamworks = [Reflection.Assembly]::LoadFrom($steamworksPath)
$constants = $steamworks.GetType('Steamworks.Constants', $true)
$limit = [int]$constants.GetField('k_cchPublishedDocumentDescriptionMax', [Reflection.BindingFlags]'Public,Static').GetValue($null)
$bytes = [Text.Encoding]::UTF8.GetBytes($template)
if ($template.IndexOf([char]0) -ge 0 -or $bytes.Length + 1 -gt $limit) {
    Exit-InvalidInput "The resolved Workshop description uses $($bytes.Length + 1) bytes including Steam's terminator; limit is $limit."
}

$destination = [IO.Path]::GetFullPath($DestinationPath)
$destinationDirectory = Split-Path -Parent $destination
if (-not (Test-Path -LiteralPath $destinationDirectory -PathType Container)) { Exit-InvalidInput 'The destination directory does not exist.' }
$temporary = Join-Path $destinationDirectory ('.' + [IO.Path]::GetFileName($destination) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
try {
    [IO.File]::WriteAllText($temporary, $template, [Text.UTF8Encoding]::new($false))
    if (Test-Path -LiteralPath $destination -PathType Leaf) {
        $backup = $temporary + '.bak'
        try { [IO.File]::Replace($temporary, $destination, $backup) }
        finally { if (Test-Path -LiteralPath $backup) { Remove-Item -LiteralPath $backup -Force } }
    }
    else { [IO.File]::Move($temporary, $destination) }
}
finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force } }

$result = [pscustomobject][ordered]@{
    status = 'resolved'
    destinationPath = $destination
    imageCount = $expectedTokens.Count
    utf8Bytes = $bytes.Length
    steamLimitIncludingTerminator = $limit
    sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
}
$provenancePath = [IO.Path]::ChangeExtension($destination, '.provenance.json')
$provenance = [pscustomobject][ordered]@{
    schema = 'ImmersiveChefs/WorkshopDescriptionProvenance/v1'
    publishedFileId = [string]$publishedFileId
    previewPublicationPlanSha256 = $previewPlanSha256.ToUpperInvariant()
    descriptionSha256 = [string]$result.sha256
    previews = @($previews | ForEach-Object {
        [pscustomobject][ordered]@{
            token = [string]$_.token
            remoteIndex = [int]$_.remoteIndex
            remoteUrl = [string]$_.remoteUrl
            localPath = [IO.Path]::GetFileName([string]$_.localPath)
            localSha256 = [string]$_.localSha256
            remoteSha256 = [string]$_.remoteSha256
            remoteType = [string]$_.remoteType
        }
    })
}
[IO.File]::WriteAllText($provenancePath, ($provenance | ConvertTo-Json -Depth 6) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
$result | Add-Member -NotePropertyName provenancePath -NotePropertyValue $provenancePath
if ($Output -eq 'json') { $result | ConvertTo-Json -Compress } else { $result | Format-List }
