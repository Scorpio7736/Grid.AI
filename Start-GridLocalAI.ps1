# Start a GRID-dedicated, loopback-only Ollama server without changing the user's other Ollama instance.
$ErrorActionPreference = 'Stop'
$runtime = (Get-Command ollama -ErrorAction Stop).Source
$logDirectory = Join-Path $env:LOCALAPPDATA 'GRID\ai-runtime'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
try {
    $version = Invoke-RestMethod 'http://127.0.0.1:11435/api/version' -TimeoutSec 2
    Write-Output "Existing runtime on port 11435: $($version.version). Verify its offline configuration before use."
    exit 0
} catch { }
$settings = @{ OLLAMA_HOST='127.0.0.1:11435'; OLLAMA_NO_CLOUD='1'; OLLAMA_NUM_PARALLEL='1'; OLLAMA_MAX_LOADED_MODELS='1'; OLLAMA_CONTEXT_LENGTH='4096'; OLLAMA_KEEP_ALIVE='1m' }
$previous = @{}
try {
    foreach ($key in $settings.Keys) { $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process'); [Environment]::SetEnvironmentVariable($key, $settings[$key], 'Process') }
    $process = Start-Process -FilePath $runtime -ArgumentList 'serve' -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logDirectory 'stdout.log') -RedirectStandardError (Join-Path $logDirectory 'stderr.log')
    Set-Content -LiteralPath (Join-Path $logDirectory 'server.pid') -Value $process.Id
    Write-Output "GRID local AI server PID $($process.Id), loopback port 11435. Uses existing Ollama model cache; no model is downloaded."
} finally {
    foreach ($key in $settings.Keys) { [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process') }
}
