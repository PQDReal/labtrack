param([string]$Executable = 'artifacts/win-x64/LabTrack.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$exePath = (Resolve-Path (Join-Path $projectRoot $Executable)).Path
$smokePath = Join-Path $projectRoot ('artifacts/smoke-' + [guid]::NewGuid().ToString('N'))
$process = Start-Process -FilePath $exePath -ArgumentList @('--smoke-test', ('"' + $smokePath + '"')) -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(60000)) { Stop-Process -Id $process.Id; throw 'UI smoke test timed out.' }
if (Test-Path -LiteralPath (Join-Path $smokePath 'FAIL.txt')) { throw (Get-Content -Raw -LiteralPath (Join-Path $smokePath 'FAIL.txt')) }
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $smokePath 'PASS.txt'))) { throw 'UI smoke test failed.' }
Get-Content -LiteralPath (Join-Path $smokePath 'PASS.txt')
Write-Output "Evidence: $smokePath"
