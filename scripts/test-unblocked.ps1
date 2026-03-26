param(
    [string[]]$Projects = @(
        "tests\TraceQ.Core.Tests\TraceQ.Core.Tests.csproj",
        "tests\TraceQ.Infrastructure.Tests\TraceQ.Infrastructure.Tests.csproj",
        "tests\TraceQ.Api.Tests\TraceQ.Api.Tests.csproj"
    ),
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$baseOutputDir = Join-Path $env:TEMP "TraceQTestRuns"

if (Test-Path $baseOutputDir) {
    Remove-Item $baseOutputDir -Recurse -Force
}

New-Item -ItemType Directory -Path $baseOutputDir | Out-Null

foreach ($project in $Projects) {
    $projectPath = Join-Path $projectRoot $project
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
    $outputDir = Join-Path $baseOutputDir $projectName

    Write-Host "Running $projectName from temp output path $outputDir"
    dotnet test $projectPath -c $Configuration -o $outputDir
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
