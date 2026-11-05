param([Parameter(Mandatory=$true)][string]$PackageRoot, [switch]$AssistanceOnly, [switch]$IntelligenceOnly, [switch]$TransferAcceptance)
$ErrorActionPreference = 'Stop'
if ($AssistanceOnly -and $IntelligenceOnly) { throw 'Choose either AssistanceOnly or IntelligenceOnly.' }
$package = (Resolve-Path -LiteralPath $PackageRoot).Path
$exe = Join-Path $package 'app\Grid.Terminal.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Portable apphost missing.' }
$verificationHome = Join-Path $package ('verification-home-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $verificationHome | Out-Null
$start = New-Object System.Diagnostics.ProcessStartInfo
$start.FileName=$exe; $start.Arguments=if($TransferAcceptance){'--verify-portable'}elseif($IntelligenceOnly){'--verify-intelligence'}elseif($AssistanceOnly){'--verify-assistance'}else{'--verify-runtime'}; $start.WorkingDirectory=$package
$start.UseShellExecute=$false; $start.CreateNoWindow=$true
$start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
$start.EnvironmentVariables['PATH']=Join-Path $env:SystemRoot 'System32'
$start.EnvironmentVariables['USERPROFILE']=$verificationHome
$start.EnvironmentVariables['HOME']=$verificationHome
$start.EnvironmentVariables['LOCALAPPDATA']=$verificationHome
$start.EnvironmentVariables['DOTNET_ROOT']=Join-Path $package 'runtime\dotnet'
$start.EnvironmentVariables['DOTNET_ROOT_X64']=Join-Path $package 'runtime\dotnet'
$start.EnvironmentVariables['DOTNET_MULTILEVEL_LOOKUP']='0'
$start.EnvironmentVariables['OLLAMA_MODELS']=Join-Path $verificationHome 'no-global-models'
$start.EnvironmentVariables.Remove('GRID_AI_DISABLED')
$process=[System.Diagnostics.Process]::Start($start)
try {
    $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
    if (!$process.WaitForExit($(if($TransferAcceptance){900000}elseif($IntelligenceOnly){480000}else{240000}))) { if (!$process.HasExited) { & (Join-Path $env:SystemRoot 'System32\taskkill.exe') /PID $process.Id /T /F | Out-Null }; throw 'Portable runtime verification timed out.' }
    Write-Output $stdout.GetAwaiter().GetResult()
    $errors=$stderr.GetAwaiter().GetResult(); if ($errors) { Write-Output $errors }
    if ($process.ExitCode -ne 0) { throw "Portable verification failed: $($process.ExitCode)" }
    Write-Output 'PASS: packaged apphost and destination-local .NET/AI worked without global PATH or user model cache. This is not a clean-machine or firewall-isolation test.'
} finally { $process.Dispose() }
