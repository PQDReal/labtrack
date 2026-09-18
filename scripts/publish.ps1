$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    dotnet test tests/LabTrack.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    dotnet publish src/LabTrack.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o artifacts/win-x64
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    & ./scripts/smoke.ps1
    Copy-Item -LiteralPath README.md -Destination artifacts/win-x64/README.md
    Compress-Archive -Path artifacts/win-x64/* -DestinationPath artifacts/LabTrack-win-x64.zip -Force
} finally { Pop-Location }
