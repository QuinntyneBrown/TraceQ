<#
.SYNOPSIS
    Downloads the all-MiniLM-L6-v2 ONNX model and vocabulary files from Hugging Face.

.DESCRIPTION
    This is an offline-transfer script for air-gapped deployments.
    Run this script ONCE on a machine with internet access, then copy the
    entire 'models/' folder to the air-gapped system.

    Downloads:
    1. all-MiniLM-L6-v2 ONNX model (model.onnx)
    2. WordPiece vocabulary (vocab.txt)
    3. Tokenizer configuration (tokenizer.json)

.EXAMPLE
    .\scripts\download-model.ps1
#>

$ErrorActionPreference = "Stop"

$modelsDir = Join-Path $PSScriptRoot ".." "models"
$modelsDir = [System.IO.Path]::GetFullPath($modelsDir)

Write-Host "TraceQ Model Downloader" -ForegroundColor Cyan
Write-Host "=======================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Target directory: $modelsDir"
Write-Host ""

# Create models directory if it doesn't exist
if (-not (Test-Path $modelsDir)) {
    New-Item -ItemType Directory -Path $modelsDir -Force | Out-Null
    Write-Host "Created directory: $modelsDir"
}

$baseUrl = "https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main"

$files = @(
    @{
        Name = "ONNX Model"
        Url  = "$baseUrl/onnx/model.onnx"
        Path = Join-Path $modelsDir "all-MiniLM-L6-v2.onnx"
    },
    @{
        Name = "Vocabulary"
        Url  = "$baseUrl/vocab.txt"
        Path = Join-Path $modelsDir "vocab.txt"
    },
    @{
        Name = "Tokenizer Config"
        Url  = "$baseUrl/tokenizer.json"
        Path = Join-Path $modelsDir "tokenizer.json"
    }
)

$totalFiles = $files.Count
$currentFile = 0

foreach ($file in $files) {
    $currentFile++
    $fileName = [System.IO.Path]::GetFileName($file.Path)

    if (Test-Path $file.Path) {
        Write-Host "[$currentFile/$totalFiles] $($file.Name) ($fileName) - already exists, skipping." -ForegroundColor Yellow
        continue
    }

    Write-Host "[$currentFile/$totalFiles] Downloading $($file.Name) ($fileName)..." -ForegroundColor Green

    try {
        # Use .NET WebClient for progress reporting on larger files
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($file.Url, $file.Path)
        $webClient.Dispose()

        $fileSize = (Get-Item $file.Path).Length
        $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
        Write-Host "  Downloaded: $fileSizeMB MB" -ForegroundColor Gray
    }
    catch {
        Write-Host "  ERROR: Failed to download $($file.Name): $_" -ForegroundColor Red
        if (Test-Path $file.Path) {
            Remove-Item $file.Path -Force
        }
        throw
    }
}

Write-Host ""
Write-Host "Download complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Files in $modelsDir`:" -ForegroundColor Cyan
Get-ChildItem $modelsDir | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  $($_.Name) ($sizeMB MB)"
}
Write-Host ""
Write-Host "Next steps for air-gapped deployment:" -ForegroundColor Yellow
Write-Host "  1. Copy the entire 'models/' folder to the air-gapped system"
Write-Host "  2. Place it in the TraceQ project root directory"
Write-Host "  3. Verify appsettings.json paths point to the correct location"
