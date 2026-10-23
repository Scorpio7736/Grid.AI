param([string]$Destination = (Join-Path $PSScriptRoot 'portable\GRID7736'), [string]$RuntimeSource = (Join-Path $PSScriptRoot '.grid-runtime'))
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $Destination) { throw "Destination exists: $Destination. Select a new folder; existing worlds/packages are never overwritten." }
if (!(Test-Path -LiteralPath (Join-Path $RuntimeSource 'assets.json'))) { throw 'Run Prepare-GridRuntime.ps1 first or supply a prepared -RuntimeSource.' }
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
Set-Content -LiteralPath (Join-Path $Destination 'grid-package.json') -Value '{"version":1,"selfContained":true,"runtime":"runtime"}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $Destination 'transfer-status.txt') -Value 'Package creation pending. Do not launch until provisioning completes.' -Encoding UTF8
$app = Join-Path $Destination 'app' 
dotnet publish (Join-Path $PSScriptRoot 'Grid.Terminal\Grid.Terminal.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $app
if ($LASTEXITCODE -ne 0) { throw 'Self-contained publish failed; this is not a completed package.' }
$runtimeTarget = Join-Path $Destination 'runtime'
New-Item -ItemType Directory -Path $runtimeTarget -Force | Out-Null
foreach ($entry in Get-ChildItem -LiteralPath $RuntimeSource) {
    if ($entry.Name -notin @('temp','home','ai-runtime.log')) { Copy-Item -LiteralPath $entry.FullName -Destination $runtimeTarget -Recurse }
}
# Runtime transient state is not required and is excluded from release copies.
New-Item -ItemType Directory -Path (Join-Path $Destination 'data') -Force | Out-Null
Set-Content -LiteralPath (Join-Path $Destination 'grid-package.json') -Value '{"version":1,"selfContained":true,"runtime":"runtime"}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $Destination 'launch.cmd') -Encoding ASCII -Value '@echo off', 'setlocal DisableDelayedExpansion', '"%~dp0app\Grid.Terminal.exe" %*', 'exit /b %errorlevel%'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'THIRD_PARTY_RUNTIME_NOTICES.md') -Destination $Destination
Set-Content -LiteralPath (Join-Path $Destination 'transfer-status.txt') -Value 'Package ready.' -Encoding UTF8
Write-Output "Portable Windows x64 package: $Destination. Start launch.cmd. No installed .NET/Ollama or user model cache is required; Windows and supported hardware drivers are still required."
