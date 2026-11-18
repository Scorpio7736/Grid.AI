[CmdletBinding()]
param([switch]$Download, [switch]$Plan, [switch]$RepairRuntime)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = [IO.Path]::GetFullPath($PSScriptRoot)
$runtime = Join-Path $root '.grid-runtime'
$app = Join-Path $root '.grid-app'
$data = Join-Path $root '.grid-data'
$lock = Get-Content -LiteralPath (Join-Path $root 'Grid-Clone-Dependencies.json') -Raw | ConvertFrom-Json
function Assert-LocalPath([string]$Path) {
    $current = [IO.Path]::GetFullPath($Path)
    if (!$current.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and $current -ne $root) { throw 'Installation path escaped this clone.' }
    while ($current) {
        if ((Test-Path -LiteralPath $current) -and ((Get-Item -Force -LiteralPath $current).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Linked installation path refused: $current" }
        $current = [IO.Path]::GetDirectoryName($current)
    }
}
foreach ($path in @($root,$runtime,$app,$data)) { Assert-LocalPath $path }
if ($Plan) {
    [ordered]@{ Mode='Fresh clone'; Runtime=$runtime; Application=$app; World=(Join-Path $data 'world.db'); Network=[bool]$Download; ExistingWorld='Preserved; never import shared LocalAppData world'; Ollama=$lock.Ollama.Url; Sdk=$lock.Sdk.Url; Model=$lock.Model.ManifestSha256 } | ConvertTo-Json
    return
}
if (![Environment]::Is64BitProcess -or $env:OS -ne 'Windows_NT' -or $env:PROCESSOR_ARCHITECTURE -ne 'AMD64') { throw 'Use 64-bit PowerShell on Windows x64.' }
if ($env:GRID_AI_DISABLED -eq '1') { throw 'GRID_AI_DISABLED=1 disables inference. Review that setting before full clone setup; it was not changed.' }
$drive = [IO.DriveInfo]::new([IO.Path]::GetPathRoot($root))
if ($RepairRuntime) {
    if (!$Download) { throw '-RepairRuntime requires -Download to stage verified replacement dependencies. Existing files retained.' }
    if ($drive.AvailableFreeSpace -lt 20000000000L) { throw 'At least 20 GB free staging space is required. Existing runtime retained in place.' }
    if (Test-Path -LiteralPath $runtime) {
        $retained = Join-Path $root ('.grid-install-retained-' + [Guid]::NewGuid().ToString('N'))
        Assert-LocalPath $runtime; Assert-LocalPath $retained
        Move-Item -LiteralPath $runtime -Destination $retained
        Write-Output "Previous runtime retained at $retained. Worlds and application data are unchanged."
    }
}
if (!(Test-Path -LiteralPath $runtime)) {
    if (!$Download) { throw 'Dependencies are absent. Run .\Setup-GridClone.ps1 -Download for explicit online installation, or provision verified local assets with Prepare-GridRuntime.ps1 first. No files downloaded.' }
    if ($drive.AvailableFreeSpace -lt 20000000000L) { throw 'At least 20 GB free staging space is required for this baseline. Nothing downloaded.' }
    $stage = Join-Path $root ('.grid-install-' + [Guid]::NewGuid().ToString('N'))
    Assert-LocalPath $stage
    New-Item -ItemType Directory -Path $stage | Out-Null
    # Failed staging is retained for inspection; never recursively delete an inferred path.
    function Fetch([string]$Url,[string]$Path,[string]$Hash,[string]$Algorithm='SHA256') {
        Invoke-WebRequest -UseBasicParsing -TimeoutSec 3600 -Uri $Url -OutFile $Path
        if ((Get-FileHash -LiteralPath $Path -Algorithm $Algorithm).Hash -ne $Hash) { throw "Downloaded asset hash mismatch: $Path. Staging retained; no runtime published." }
    }
    Write-Output 'Downloading pinned runtime, SDK and Qwen assets. No global services/accounts/worlds are copied.'
    $bundle = Join-Path $stage 'runtime'
    New-Item -ItemType Directory -Path $bundle | Out-Null
    $zip = Join-Path $stage 'ollama.zip'
    Fetch $lock.Ollama.Url $zip $lock.Ollama.Sha256
    Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $bundle 'ollama')
    $zip = Join-Path $stage 'sdk.zip'
    Fetch $lock.Sdk.Url $zip $lock.Sdk.Sha512 'SHA512'
    Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $bundle 'dotnet')
    $manifest = Join-Path $bundle 'models\manifests\registry.ollama.ai\library\qwen3\4b'
    New-Item -ItemType Directory -Path (Split-Path $manifest) | Out-Null
        $pinnedManifest=Join-Path $root $lock.Model.Manifest
    if ((Get-FileHash -LiteralPath $pinnedManifest).Hash -ne $lock.Model.ManifestSha256) { throw 'Repository model manifest hash mismatch.' }
    Copy-Item -LiteralPath $pinnedManifest -Destination $manifest
    $model = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
    $blobs = @(@($model.config) + @($model.layers) | Sort-Object digest -Unique)
    $blobRoot = Join-Path $bundle 'models\blobs'
    New-Item -ItemType Directory -Path $blobRoot | Out-Null
    foreach ($blob in $blobs) {
        if ($blob.digest -notmatch '^sha256:[0-9a-f]{64}$' -or $blob.size -le 0 -or $blob.size -gt 75000000000L) { throw 'Invalid pinned model blob metadata.' }
        $target = Join-Path $blobRoot $blob.digest.Replace(':','-')
                if ($blob.digest -eq 'sha256:359d7dd4bcdab3d86b87d73ac27966f4dbb9f5efdfcc75d34a8764a09474fae7') {
            $metadata=Join-Path $root $lock.Model.MetadataBlob
            if ((Get-FileHash -LiteralPath $metadata).Hash -ne $blob.digest.Substring(7)) { throw 'Pinned metadata blob mismatch.' }
            Copy-Item -LiteralPath $metadata -Destination $target
        } else { Fetch ('https://registry.ollama.ai/v2/library/qwen3/blobs/' + $blob.digest) $target $blob.digest.Substring(7) }
        if ((Get-Item -LiteralPath $target).Length -ne $blob.size) { throw 'Model blob size mismatch.' }
    }
    Copy-Item -LiteralPath (Join-Path $root 'ThirdParty\Ollama-LICENSE') -Destination (Join-Path $bundle 'ollama')
    @{model='qwen3:4b'; runtimeVersion='ollama version is 0.40.9'; manifestSha256=$lock.Model.ManifestSha256; preparedUtc=[DateTime]::UtcNow.ToString('O')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $bundle 'assets.json') -Encoding UTF8
    Assert-LocalPath $bundle; Assert-LocalPath $runtime
    if (Test-Path -LiteralPath $runtime) { throw 'Runtime destination appeared during setup; no overwrite.' }
    Move-Item -LiteralPath $bundle -Destination $runtime
    Write-Output "Verified dependencies published. Download archives retained at $stage."
}
$dotnet = Join-Path $runtime 'dotnet\dotnet.exe'
foreach ($required in @($dotnet,(Join-Path $runtime 'assets.json'),(Join-Path $runtime 'ollama\ollama.exe'))) {
    if (!(Test-Path -LiteralPath $required -PathType Leaf)) { throw "Incomplete runtime: $required. Existing files retained. Close GRID, then run .\Setup-GridClone.ps1 -Download -RepairRuntime to retain and replace this runtime, or provision verified local assets." }
}
$manifest = Join-Path $runtime 'models\manifests\registry.ollama.ai\library\qwen3\4b'
if ((Get-FileHash -LiteralPath $manifest).Hash -ne $lock.Model.ManifestSha256) { throw 'Pinned model manifest verification failed.' }
$model = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
foreach ($blob in @($model.config) + @($model.layers)) {
    if ($blob.digest -notmatch '^sha256:[0-9a-f]{64}$') { throw 'Invalid model digest.' }
    $path = Join-Path $runtime ('models\blobs\' + $blob.digest.Replace(':','-')); Assert-LocalPath $path
    if ((Get-Item -LiteralPath $path).Length -ne $blob.size -or (Get-FileHash -LiteralPath $path).Hash -ne $blob.digest.Substring(7)) { throw 'Pinned model blob verification failed.' }
}
# Child process environment only; never set machine/user settings or import personal caches.
$names = @('DOTNET_ROOT','DOTNET_ROOT_X64','DOTNET_MULTILEVEL_LOOKUP','DOTNET_CLI_HOME','NUGET_PACKAGES','DOTNET_CLI_TELEMETRY_OPTOUT','DOTNET_SKIP_FIRST_TIME_EXPERIENCE','DOTNET_GENERATE_ASPNET_CERTIFICATE')
$previous = @{}; foreach ($name in $names) { $previous[$name]=[Environment]::GetEnvironmentVariable($name) }
try {
    $env:DOTNET_ROOT=$env:DOTNET_ROOT_X64=Join-Path $runtime 'dotnet'
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'; $env:DOTNET_MULTILEVEL_LOOKUP='0'; $env:DOTNET_CLI_TELEMETRY_OPTOUT='1'; $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
    $env:DOTNET_CLI_HOME=Join-Path $runtime 'sdk-home'; $env:NUGET_PACKAGES=Join-Path $runtime 'nuget'
    New-Item -ItemType Directory -Path $app -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $app 'clone-ready.json') -Value '{"version":1,"ready":false}' -Encoding UTF8
    $config=Join-Path $app 'Clone.NuGet.Config'
    $sources=if($Download) {'<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />'} else {''}
    Set-Content -LiteralPath $config -Value ('<configuration><packageSources><clear />'+$sources+'</packageSources></configuration>') -Encoding UTF8
    & $dotnet restore (Join-Path $root 'Grid.Terminal\Grid.Terminal.csproj') --configfile $config -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed. Offline setup needs a populated runtime/nuget cache; use -Download to allow package restore.' }
    & $dotnet build (Join-Path $root 'Grid.Terminal\Grid.Terminal.csproj') -c Release --no-restore -o $app
    if ($LASTEXITCODE -ne 0) { throw 'GRID build failed; installation not marked ready.' }
    & (Join-Path $app 'Grid.Terminal.exe') --verify-chat-runtime
    if ($LASTEXITCODE -ne 0) { throw 'Local-model conversation verification failed; installation not marked ready. Preserve the printed runtime error.' }
    Set-Content -LiteralPath (Join-Path $app 'clone-ready.json') -Value (@{version=1; ready=$true; verifiedUtc=[DateTime]::UtcNow.ToString('O'); world='.grid-data/world.db'} | ConvertTo-Json) -Encoding UTF8
    Write-Output 'Grid.AI clean clone ready. Run .\Start-GridClone.ps1 for this clone-local world and credential setup. Existing worlds remain untouched.'
} finally { foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$previous[$name],'Process') } }
