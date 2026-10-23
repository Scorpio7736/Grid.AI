param(
    [string]$OllamaSource = (Join-Path $env:LOCALAPPDATA 'Programs\Ollama'),
    [string]$ModelCache = (Join-Path $env:USERPROFILE '.ollama\models'),
    [string]$DotnetSource = (Split-Path (Get-Command dotnet -ErrorAction Stop).Source),
    [string]$NugetCache = (Join-Path $env:USERPROFILE ".nuget\packages"),
    [string]$Destination = (Join-Path $PSScriptRoot '.grid-runtime')
)
$ErrorActionPreference = 'Stop'
# One-time provisioning from explicit installed/downloaded assets, not a runtime dependency.
if (Test-Path -LiteralPath $Destination) { throw "Destination already exists: $Destination. Keep it or choose a new destination; nothing overwritten." }
$manifestRelative = 'manifests\registry.ollama.ai\library\qwen3\4b'
$manifestPath = Join-Path $ModelCache $manifestRelative
if (!(Test-Path -LiteralPath (Join-Path $OllamaSource 'ollama.exe'))) { throw 'Ollama runtime missing. Supply -OllamaSource with an extracted Windows runtime directory.' }
if (!(Test-Path -LiteralPath $manifestPath)) { throw 'qwen3:4b missing. Provision its Ollama model cache once, then supply -ModelCache. Inference never downloads models.' }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$blobs = @($manifest.config) + @($manifest.layers)
foreach ($blob in $blobs) {
    if ($blob.digest -notmatch '^sha256:[0-9a-f]{64}$') { throw 'Invalid model blob digest.' }
    $source = Join-Path $ModelCache ('blobs\' + $blob.digest.Replace(':','-'))
    if (!(Test-Path -LiteralPath $source) -or (Get-Item -LiteralPath $source).Length -ne $blob.size) { throw "Missing/wrong-sized model blob: $source" }
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $blob.digest.Substring(7)) { throw "Model blob failed SHA256: $source" }
}
if (!(Test-Path -LiteralPath (Join-Path $DotnetSource 'sdk'))) { throw 'A .NET SDK directory is required for bundled MAXIS tooling.' }
$runtimeTarget = Join-Path $Destination 'ollama'
$modelTarget = Join-Path $Destination 'models'
New-Item -ItemType Directory -Path $runtimeTarget, (Join-Path $modelTarget 'blobs'), (Split-Path (Join-Path $modelTarget $manifestRelative)) -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $OllamaSource 'ollama.exe') -Destination $runtimeTarget
Copy-Item -LiteralPath (Join-Path $OllamaSource 'lib') -Destination $runtimeTarget -Recurse
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ThirdParty\Ollama-LICENSE') -Destination $runtimeTarget
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $modelTarget $manifestRelative)
foreach ($blob in $blobs) {
    $name = $blob.digest.Replace(':','-')
    Copy-Item -LiteralPath (Join-Path $ModelCache ('blobs\' + $name)) -Destination (Join-Path $modelTarget ('blobs\' + $name))
}
Copy-Item -LiteralPath $DotnetSource -Destination (Join-Path $Destination 'dotnet') -Recurse
if (Test-Path -LiteralPath $NugetCache) { Copy-Item -LiteralPath $NugetCache -Destination (Join-Path $Destination 'nuget') -Recurse }
@{ model='qwen3:4b'; manifestSha256=(Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash; runtimeVersion=(& (Join-Path $runtimeTarget 'ollama.exe') --version | Out-String).Trim(); preparedUtc=[DateTime]::UtcNow.ToString('O') } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Destination 'assets.json') -Encoding UTF8
Write-Output "GRID runtime provisioned inside $Destination. Normal GRID launch now starts and stops its own bundled AI server."
