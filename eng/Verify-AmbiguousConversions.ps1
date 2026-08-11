param(
    [Parameter(Mandatory = $true)]
    [string] $ProjectPath,

    [Parameter(Mandatory = $true)]
    [string] $PackageVersion,

    [string] $DotNetPath = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$output = & $DotNetPath build $ProjectPath `
    --configuration Release `
    --no-restore `
    -p:ReunionPackageVersion=$PackageVersion `
    -p:VerifyAmbiguousConversions=true `
    --consoleLoggerParameters:NoSummary 2>&1 | Out-String
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    throw "$ProjectPath accepted ambiguous implicit conversions."
}

$expectedTargets = @(
    'Result<string, string>',
    'Result<Failure<string>>',
    'Result<Failure<string>, string>',
    'Result<int, Success<int>>',
    'UnitResult<Success>',
    'Option<None>')

foreach ($target in $expectedTargets) {
    if (-not $output.Contains("$target.implicit operator")) {
        throw "$ProjectPath did not report CS0457 for $target.`n$output"
    }
}

if (-not $output.Contains('error CS0457')) {
    throw "$ProjectPath failed without the expected CS0457 diagnostics.`n$output"
}

Write-Output "$ProjectPath rejects all ambiguous implicit conversions."
$global:LASTEXITCODE = 0
