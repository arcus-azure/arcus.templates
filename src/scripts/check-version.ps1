param(
    [string]$packageName,
    [string]$packageVersion
)

Write-Host "Looking for an update..."

$currentVersion = New-Object -TypeName System.Version -ArgumentList $packageVersion

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$json = Invoke-RestMethod -Uri "https://api.github.com/repos/arcus-azure/arcus.templates/releases" -Method Get
$availableVersions = $json | % { New-Object -TypeName System.Version -ArgumentList $_.name.Trim('v') }

$update = $availableVersions | 
    where { $_ -gt $currentVersion } | 
    Measure-Object -Maximum
if ($update.Count -ne 0) {
    $max = $update.Maximum
    Write-Warning "An update is available for the $packageName : $packageVersion -> $max (https://github.com/arcus-azure/arcus.templates/releases/tag/v$max)" }