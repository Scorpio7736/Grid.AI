# Compatibility entry point. GRID now owns its bundled runtime lifecycle.
$ErrorActionPreference = 'Stop'
Write-Output 'GRID starts its bundled AI automatically on normal launch. No separate global Ollama server is needed.'
Write-Output 'Fresh source clone: run Prepare-GridRuntime.ps1 once, then launch GRID.'
Write-Output 'Portable release: run launch.cmd. Verify with launch.cmd --verify-runtime.'
