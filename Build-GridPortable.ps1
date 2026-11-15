param([string]$Destination = (Join-Path $PSScriptRoot 'portable\GRID7736'), [string]$RuntimeSource = (Join-Path $PSScriptRoot '.grid-runtime'))
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $Destination) { throw "Destination exists: $Destination. Select a new folder; existing worlds/packages are never overwritten." }
if (!(Test-Path -LiteralPath (Join-Path $RuntimeSource 'assets.json'))) { throw 'Run Prepare-GridRuntime.ps1 first or supply a prepared -RuntimeSource.' }
$runtimeBytes = (Get-ChildItem -LiteralPath $RuntimeSource -File -Recurse | Measure-Object -Property Length -Sum).Sum
if ($runtimeBytes -gt 127744000000L) { throw 'Runtime assets exceed the 128 GB managed target with reserve.' }
$modelBytes = (Get-ChildItem -LiteralPath $RuntimeSource -File -Recurse | Where-Object { !$_.FullName.StartsWith((Join-Path ([IO.Path]::GetFullPath($RuntimeSource)) 'dotnet') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) } | Measure-Object -Property Length -Sum).Sum
if ($modelBytes -gt 75000000000L) { throw 'AI runtime, models and caches exceed the 75 GB sub-limit.' }
$targetDrive = [IO.DriveInfo]::new([IO.Path]::GetPathRoot([IO.Path]::GetFullPath($Destination)))
if ($targetDrive.AvailableFreeSpace -lt $runtimeBytes + 1000000000L) { throw 'Insufficient staging space for runtime plus application/source headroom.' }
$publishDestination = [IO.Path]::GetFullPath($Destination)
$Destination = $publishDestination + '.staging-' + [Guid]::NewGuid().ToString('N')
New-Item -ItemType Directory -Path $Destination | Out-Null
Set-Content -LiteralPath (Join-Path $Destination 'grid-package.json') -Value '{"version":2,"selfContained":true,"runtime":"runtime","platform":"win-x64"}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $Destination 'transfer-status.txt') -Value 'Package creation pending. Do not launch until provisioning completes.' -Encoding UTF8
$app = Join-Path $Destination 'app' 
dotnet publish (Join-Path $PSScriptRoot 'Grid.Terminal\Grid.Terminal.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:ContinuousIntegrationBuild=true -p:Deterministic=true -o $app
if ($LASTEXITCODE -ne 0) { throw 'Self-contained publish failed; this is not a completed package.' }
$runtimeTarget = Join-Path $Destination 'runtime'
New-Item -ItemType Directory -Path $runtimeTarget -Force | Out-Null
foreach ($entry in Get-ChildItem -LiteralPath $RuntimeSource) {
    if ($entry.Name -notin @('temp','home','ai-runtime.log')) { Copy-Item -LiteralPath $entry.FullName -Destination $runtimeTarget -Recurse }
}
# Runtime transient state is not required and is excluded from release copies.
New-Item -ItemType Directory -Path (Join-Path $Destination 'data') -Force | Out-Null
Set-Content -LiteralPath (Join-Path $Destination 'grid-package.json') -Value '{"version":2,"selfContained":true,"runtime":"runtime","platform":"win-x64"}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $Destination 'launch.cmd') -Encoding ASCII -Value '@echo off', 'setlocal DisableDelayedExpansion', 'set "DOTNET_ROOT=%~dp0runtime\dotnet"', 'set "DOTNET_ROOT_X64=%~dp0runtime\dotnet"', 'set "DOTNET_MULTILEVEL_LOOKUP=0"', '"%~dp0app\Grid.Terminal.exe" %*', 'exit /b %errorlevel%'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'THIRD_PARTY_RUNTIME_NOTICES.md') -Destination $Destination
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Test-GridPortable.ps1') -Destination $Destination
# Keep the shipped source/code in the same measured base budget. Never recurse into generated packages or user state.
$sourceTarget = Join-Path $Destination 'source'
function Copy-GridSource([string]$From, [string]$To) {
    New-Item -ItemType Directory -Path $To -Force | Out-Null
    foreach ($entry in Get-ChildItem -LiteralPath $From) {
        if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Source link unsupported: $($entry.FullName)" }
        if ($entry.FullName -eq [IO.Path]::GetFullPath($Destination)) { continue }
        if ($entry.PSIsContainer) {
            if ($entry.Name -notlike '.grid-*' -and $entry.Name -notin @('.git','.vs','.grid-runtime','portable','bin','obj','TestResults','data','runtime','app','node_modules')) { Copy-GridSource $entry.FullName (Join-Path $To $entry.Name) }
        } elseif (($entry.Extension -in @('.cs','.csproj','.slnx','.props','.targets','.ps1','.md','.json') -or $entry.Name -in @('PdfPig-LICENSE','llama.cpp-LICENSE')) -and $entry.Name -notmatch '^(appsettings|secrets)') { Copy-Item -LiteralPath $entry.FullName -Destination $To }
    }
}
Copy-GridSource $PSScriptRoot $sourceTarget
& (Join-Path $app 'Grid.Terminal.exe') --seal-portable-assets $Destination
if ($LASTEXITCODE -ne 0) { throw 'Package exceeds the base budget or has invalid model assets; package remains incomplete.' }
Set-Content -LiteralPath (Join-Path $Destination 'transfer-status.txt') -Value 'Package ready.' -Encoding UTF8
if (([IO.DriveInfo]::new([IO.Path]::GetPathRoot($Destination))).AvailableFreeSpace -lt 256000000L) { throw 'Package staging consumed the required free-space reserve.' }
[IO.Directory]::Move($Destination, $publishDestination)
Write-Output "Portable Windows x64 package: $publishDestination. Start launch.cmd. No installed .NET/Ollama or user model cache is required; Windows and supported hardware drivers are still required."
