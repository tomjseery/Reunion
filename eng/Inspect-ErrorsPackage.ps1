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
        'lib/net10.0/Reunion.Errors.dll',
        'lib/net11.0/Reunion.Errors.dll')
    $actualAssets = @($entryNames | Where-Object { $_ -match '^lib/.+/Reunion\.Errors\.dll$' })

    if (@($expectedAssets | Where-Object { $_ -notin $actualAssets }).Count -ne 0 -or
        @($actualAssets | Where-Object { $_ -notin $expectedAssets }).Count -ne 0) {
        throw "Unexpected Reunion.Errors library assets: $($actualAssets -join ', ')"
    }

    $expectedDerivedFactories = @(
        'Conflict:0',
        'Conflict:1',
        'Forbidden:0',
        'Forbidden:1',
        'Invalid:0',
        'Invalid:1',
        'NotFound:0',
        'NotFound:1',
        'PaymentRequired:0',
        'PaymentRequired:1',
        'Unauthenticated:0',
        'Unauthenticated:1',
        'Validation:1',
        'Validation:2')

    foreach ($asset in $expectedAssets) {
        $assemblyStream = [System.IO.MemoryStream]::new()
        $assetStream = $archive.GetEntry($asset).Open()
        $peReader = $null

        try {
            $assetStream.CopyTo($assemblyStream)
            $assemblyStream.Position = 0
            $peReader = [System.Reflection.PortableExecutable.PEReader]::new($assemblyStream)
            $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader(
                $peReader)
            $types = @{}

            foreach ($typeHandle in $metadata.TypeDefinitions) {
                $type = $metadata.GetTypeDefinition($typeHandle)
                $name = $metadata.GetString($type.Name)
                $namespace = $metadata.GetString($type.Namespace)
                $fullName = if ($namespace) { "$namespace.$name" } else { $name }
                $types[$fullName] = $type
            }

            if ($types.ContainsKey('Reunion.Errors.ErrorDefinitions`1')) {
                throw "$asset still exposes the removed generic error-definition builder."
            }

            if (-not $types.ContainsKey('Reunion.Errors.ErrorDefinition')) {
                throw "$asset does not expose Reunion.Errors.ErrorDefinition."
            }

            $publicStaticMethods = @()
            $definitionType = $types['Reunion.Errors.ErrorDefinition']

            foreach ($methodHandle in $definitionType.GetMethods()) {
                $method = $metadata.GetMethodDefinition($methodHandle)
                $isPublic = ($method.Attributes -band
                    [System.Reflection.MethodAttributes]::Public) -ne 0
                $isStatic = ($method.Attributes -band
                    [System.Reflection.MethodAttributes]::Static) -ne 0

                if ($isPublic -and $isStatic) {
                    $publicStaticMethods += $method
                }
            }

            if (@($publicStaticMethods | Where-Object {
                        $metadata.GetString($_.Name) -eq 'For'
                    }).Count -ne 0) {
                throw "$asset still exposes the removed owner-builder factory."
            }

            $derivedFactories = @($publicStaticMethods | Where-Object {
                    $_.GetGenericParameters().Count -eq 1
                })
            $actualDerivedFactories = @(
                $derivedFactories | ForEach-Object {
                    "$($metadata.GetString($_.Name)):$($_.GetParameters().Count)"
                })

            if (@($expectedDerivedFactories | Where-Object {
                        $_ -notin $actualDerivedFactories
                    }).Count -ne 0 -or
                @($actualDerivedFactories | Where-Object {
                        $_ -notin $expectedDerivedFactories
                    }).Count -ne 0) {
                throw "$asset does not expose the expected direct generic factory surface."
            }
        }
        finally {
            if ($null -ne $peReader) {
                $peReader.Dispose()
            }

            $assetStream.Dispose()
            $assemblyStream.Dispose()
        }
    }

    if ('README.md' -notin $entryNames) {
        throw 'The package does not contain its declared README.md.'
    }

    $nuspecEntries = @($archive.Entries | Where-Object FullName -Like '*.nuspec')
    if ($nuspecEntries.Count -ne 1) {
        throw "Expected exactly one nuspec but found $($nuspecEntries.Count)."
    }

    $reader = [System.IO.StreamReader]::new($nuspecEntries[0].Open())
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

    if ($packageId -ne 'Reunion.Errors') {
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
        throw 'Reunion.Errors must not contain NuGet dependencies.'
    }

    $groups = @($nuspec.SelectNodes("//*[local-name()='dependencies']/*[local-name()='group']") |
        ForEach-Object { $_.targetFramework })
    $expectedGroups = @('net10.0', 'net11.0')
    if (@($expectedGroups | Where-Object { $_ -notin $groups }).Count -ne 0 -or
        @($groups | Where-Object { $_ -notin $expectedGroups }).Count -ne 0) {
        throw "Unexpected dependency groups: $($groups -join ', ')"
    }

    Write-Host "Package $packageId $packageVersion contains the expected metadata, direct factory APIs, assets, and empty dependency groups."
}
finally {
    $archive.Dispose()
}
