param(
    [Parameter(Mandatory = $true)]
    [string] $ProjectPath,

    [Parameter(Mandatory = $true)]
    [string] $PackagesPath,

    [Parameter(Mandatory = $true)]
    [string] $ExpectedVersion,

    [Parameter(Mandatory = $true)]
    [string] $ExpectedPackageSource,

    [string] $PackageId = 'Reunion'
)

$ErrorActionPreference = 'Stop'
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$resolvedPackages = [System.IO.Path]::GetFullPath($PackagesPath)
$resolvedSource = (Resolve-Path -LiteralPath $ExpectedPackageSource).Path
$assetsPath = Join-Path (Split-Path -Parent $resolvedProject) 'obj/project.assets.json'
$assets = Get-Content -Raw -LiteralPath $assetsPath | ConvertFrom-Json -AsHashtable
$libraryKey = "$PackageId/$ExpectedVersion"

if (-not $assets.libraries.ContainsKey($libraryKey) -or $assets.libraries[$libraryKey].type -ne 'package') {
    throw "Restore assets do not contain the expected package $libraryKey."
}

$packageFolder = Join-Path $resolvedPackages "$($PackageId.ToLowerInvariant())/$ExpectedVersion"
$metadataPath = Join-Path $packageFolder '.nupkg.metadata'
$packageMetadata = Get-Content -Raw -LiteralPath $metadataPath | ConvertFrom-Json -AsHashtable
$actualSource = [System.IO.Path]::GetFullPath($packageMetadata.source)

if (-not $actualSource.Equals($resolvedSource, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "$PackageId was restored from '$actualSource' instead of '$resolvedSource'."
}

Write-Host "$libraryKey was restored from the isolated local package source into $resolvedPackages."
