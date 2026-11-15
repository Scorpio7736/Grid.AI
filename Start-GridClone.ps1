[CmdletBinding()]
param([switch]$Plan)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($PSScriptRoot)
$world=Join-Path $root '.grid-data\world.db'
$app=Join-Path $root '.grid-app\Grid.Terminal.exe'
if($Plan) { @{ Application=$app; World=$world; SharedWorldImported=$false } | ConvertTo-Json; return }
foreach($path in @($root,(Join-Path $root '.grid-app'),(Join-Path $root '.grid-data'),$world)) {
    $current=$path
    while($current) {
        if((Test-Path -LiteralPath $current) -and ((Get-Item -Force -LiteralPath $current).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Linked clone path refused: $current" }
        $current=[IO.Path]::GetDirectoryName($current)
    }
}
if(!(Test-Path -LiteralPath (Join-Path $root '.grid-app\clone-ready.json'))) { throw 'Run Setup-GridClone.ps1 first; a successful model conversation check is required.' }
$readiness=Get-Content -LiteralPath (Join-Path $root '.grid-app\clone-ready.json') -Raw | ConvertFrom-Json
if($readiness.version -ne 1 -or $readiness.ready -ne $true) { throw 'Clone setup is incomplete. Rerun Setup-GridClone.ps1; no world opened.' }
$before=[Environment]::GetEnvironmentVariable('DOTNET_ROOT_X64')
try {
    $env:DOTNET_ROOT_X64=Join-Path $root '.grid-runtime\dotnet'
    New-Item -ItemType Directory -Path (Split-Path $world) -Force | Out-Null
    & $app --world $world
    if($LASTEXITCODE -ne 0) { throw "GRID exited with code $LASTEXITCODE" }
} finally { [Environment]::SetEnvironmentVariable('DOTNET_ROOT_X64',$before,'Process') }
