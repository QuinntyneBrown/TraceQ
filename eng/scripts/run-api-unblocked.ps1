param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$projectPath = Join-Path $projectRoot "src\TraceQ.Api\TraceQ.Api.csproj"
$outputDir = Join-Path $env:TEMP "TraceQApiBuildOut"
$entryAssembly = Join-Path $outputDir "TraceQ.Api.dll"

if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}

dotnet build $projectPath -c $Configuration -o $outputDir
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet $entryAssembly
exit $LASTEXITCODE
