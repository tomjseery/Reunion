param(
    [Parameter(Mandatory = $true)]
    [string] $PackagePath,

    [string] $ExpectedVersion
)

$ErrorActionPreference = 'Stop'
$resolvedPackage = (Resolve-Path -LiteralPath $PackagePath).Path
$archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedPackage)

try {
    $entryNames = @($archive.Entries | ForEach-Object FullName)
    $expectedAssets = @('lib/net10.0/Reunion.dll', 'lib/net11.0/Reunion.dll')
    $actualAssets = @($entryNames | Where-Object { $_ -match '^lib/.+/Reunion\.dll$' })

    if (@($expectedAssets | Where-Object { $_ -notin $actualAssets }).Count -ne 0 -or
        @($actualAssets | Where-Object { $_ -notin $expectedAssets }).Count -ne 0) {
        throw "Unexpected Reunion library assets: $($actualAssets -join ', ')"
    }

    foreach ($documentationAsset in @('lib/net10.0/Reunion.xml', 'lib/net11.0/Reunion.xml')) {
        $entry = $archive.GetEntry($documentationAsset)
        if ($null -eq $entry) {
            throw "The package does not contain $documentationAsset."
        }

        $documentationReader = [System.IO.StreamReader]::new($entry.Open())
        try {
            $documentation = $documentationReader.ReadToEnd()
        }
        finally {
            $documentationReader.Dispose()
        }

        if (-not $documentation.Contains('M:Reunion.UnitResult`1.Map``1')) {
            throw "$documentationAsset does not describe UnitResult.Map."
        }
    }

    if ('README.md' -notin $entryNames) {
        throw 'The package does not contain its declared README.md.'
    }

    $nuspecEntries = @($archive.Entries | Where-Object FullName -Like '*.nuspec')
    if ($nuspecEntries.Count -ne 1) {
        throw "Expected exactly one nuspec but found $($nuspecEntries.Count)."
    }

    $nuspecEntry = $nuspecEntries[0]
    $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
    try {
        [xml] $nuspec = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }

    $metadata = $nuspec.SelectSingleNode("//*[local-name()='metadata']")
    $packageId = $metadata.SelectSingleNode("*[local-name()='id']").InnerText
    $packageVersion = $metadata.SelectSingleNode("*[local-name()='version']").InnerText
    $license = $metadata.SelectSingleNode("*[local-name()='license']")
    $readme = $metadata.SelectSingleNode("*[local-name()='readme']").InnerText
    $repository = $metadata.SelectSingleNode("*[local-name()='repository']")

    if ($packageId -ne 'Reunion') {
        throw "Unexpected package ID: $packageId"
    }

    if ($ExpectedVersion -and $packageVersion -ne $ExpectedVersion) {
        throw "Expected package version $ExpectedVersion but found $packageVersion."
    }

    if ($license.type -ne 'expression' -or $license.InnerText -ne 'MIT') {
        throw 'The package must declare the MIT license expression.'
    }

    if ($readme -ne 'README.md') {
        throw "Unexpected package readme path: $readme"
    }

    if ($repository.type -ne 'git' -or $repository.url -ne 'https://github.com/ThomasSeery/Reunion') {
        throw 'The package repository metadata is missing or incorrect.'
    }

    $dependencies = @($nuspec.SelectNodes("//*[local-name()='dependency']"))
    if ($dependencies.Count -ne 0) {
        throw "The consumer package has NuGet dependencies."
    }

    $groups = @($nuspec.SelectNodes("//*[local-name()='dependencies']/*[local-name()='group']") |
        ForEach-Object { $_.targetFramework })
    $expectedGroups = @('net10.0', 'net11.0')
    if (@($expectedGroups | Where-Object { $_ -notin $groups }).Count -ne 0 -or
        @($groups | Where-Object { $_ -notin $expectedGroups }).Count -ne 0) {
        throw "Unexpected dependency groups: $($groups -join ', ')"
    }

    Write-Host "Package $packageId $packageVersion contains the expected metadata, assets, and empty dependency groups."
}
finally {
    $archive.Dispose()
}
