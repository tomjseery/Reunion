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
    $expectedAssets = @(
        'lib/net10.0/Reunion.AspNetCore.dll',
        'lib/net11.0/Reunion.AspNetCore.dll')
    $actualAssets = @($entryNames | Where-Object { $_ -match '^lib/.+/Reunion\.AspNetCore\.dll$' })

    if (@($expectedAssets | Where-Object { $_ -notin $actualAssets }).Count -ne 0 -or
        @($actualAssets | Where-Object { $_ -notin $expectedAssets }).Count -ne 0) {
        throw "Unexpected Reunion.AspNetCore library assets: $($actualAssets -join ', ')"
    }

    if ('README.md' -notin $entryNames) {
        throw 'The package does not contain its declared README.md.'
    }

    $nuspecEntry = @($archive.Entries | Where-Object FullName -Like '*.nuspec')
    if ($nuspecEntry.Count -ne 1) {
        throw "Expected exactly one nuspec but found $($nuspecEntry.Count)."
    }

    $reader = [System.IO.StreamReader]::new($nuspecEntry[0].Open())
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

    if ($packageId -ne 'Reunion.AspNetCore') {
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

    $dependencyGroups = @($nuspec.SelectNodes("//*[local-name()='dependencies']/*[local-name()='group']"))
    $expectedFrameworks = @('net10.0', 'net11.0')
    $actualFrameworks = @($dependencyGroups | ForEach-Object targetFramework)
    if (@($expectedFrameworks | Where-Object { $_ -notin $actualFrameworks }).Count -ne 0 -or
        @($actualFrameworks | Where-Object { $_ -notin $expectedFrameworks }).Count -ne 0) {
        throw "Unexpected dependency groups: $($actualFrameworks -join ', ')"
    }

    foreach ($group in $dependencyGroups) {
        $dependencies = @($group.SelectNodes("*[local-name()='dependency']"))
        $expectedDependencies = @('Reunion', 'Reunion.Errors')
        $actualDependencies = @($dependencies | ForEach-Object id)
        if ($dependencies.Count -ne $expectedDependencies.Count -or
            @($expectedDependencies | Where-Object { $_ -notin $actualDependencies }).Count -ne 0 -or
            @($actualDependencies | Where-Object { $_ -notin $expectedDependencies }).Count -ne 0) {
            throw "The $($group.targetFramework) group must contain only Reunion and Reunion.Errors dependencies."
        }

        if ($ExpectedVersion -and @($dependencies | Where-Object version -NE $ExpectedVersion).Count -ne 0) {
            throw "The Reunion dependencies for $($group.targetFramework) must be $ExpectedVersion."
        }
    }

    $frameworkGroups = @($nuspec.SelectNodes("//*[local-name()='frameworkReferences']/*[local-name()='group']"))
    $frameworkTargets = @($frameworkGroups | ForEach-Object targetFramework)
    if (@($expectedFrameworks | Where-Object { $_ -notin $frameworkTargets }).Count -ne 0 -or
        @($frameworkTargets | Where-Object { $_ -notin $expectedFrameworks }).Count -ne 0) {
        throw "Unexpected framework-reference groups: $($frameworkTargets -join ', ')"
    }

    foreach ($group in $frameworkGroups) {
        $references = @($group.SelectNodes("*[local-name()='frameworkReference']"))
        if ($references.Count -ne 1 -or $references[0].name -ne 'Microsoft.AspNetCore.App') {
            throw "The $($group.targetFramework) group must contain only Microsoft.AspNetCore.App."
        }
    }

    Write-Host "Package $packageId $packageVersion contains the expected metadata, assets, Reunion package dependencies, and ASP.NET Core framework references."
}
finally {
    $archive.Dispose()
}
