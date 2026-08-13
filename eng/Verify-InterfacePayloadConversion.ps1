param(
    [Parameter(Mandatory = $true)]
    [string] $ProjectPath,

    [Parameter(Mandatory = $true)]
    [string] $Framework,

    [string] $DotNetPath = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$output = & $DotNetPath build $ProjectPath `
    --configuration Release `
    --framework $Framework `
    --no-restore `
    -p:VerifyInterfacePayloadConversion=true `
    --consoleLoggerParameters:NoSummary 2>&1 | Out-String
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    throw "$ProjectPath accepted the direct interface payload conversion for $Framework."
}

if (-not $output.Contains('error CS0029')) {
    throw "$ProjectPath failed without the expected CS0029 diagnostic for $Framework.`n$output"
}

if (-not $output.Contains('IReadOnlyList')) {
    throw "$ProjectPath did not report the interface payload conversion for $Framework.`n$output"
}

Write-Output "$ProjectPath reproduces the direct interface payload conversion failure for $Framework."
$global:LASTEXITCODE = 0
