param([Parameter(Mandatory=$true)][string]$StatusPath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[Windows.Forms.Application]::EnableVisualStyles()
$form=New-Object Windows.Forms.Form
$form.Text='GRID.AI // TRANSFER'
$form.Size=New-Object Drawing.Size(850,520)
$form.MinimumSize=$form.Size
$form.StartPosition='CenterScreen'
$form.BackColor=[Drawing.Color]::FromArgb(12,18,26)
$form.ForeColor=[Drawing.Color]::LightSkyBlue
$form.Font=New-Object Drawing.Font('Consolas',11)
$layout=New-Object Windows.Forms.TableLayoutPanel
$layout.Dock='Fill';$layout.Padding=New-Object Windows.Forms.Padding(18);$layout.ColumnCount=1;$layout.RowCount=7
$form.Controls.Add($layout)
$heading=New-Object Windows.Forms.Label;$heading.Text='GRID.AI // TRANSFER';$heading.AutoSize=$true
$destination=New-Object Windows.Forms.Label;$destination.AutoSize=$true;$destination.MaximumSize=New-Object Drawing.Size(780,70)
$phase=New-Object Windows.Forms.Label;$phase.AutoSize=$true;$phase.ForeColor=[Drawing.Color]::Gold
$bar=New-Object Windows.Forms.ProgressBar;$bar.Dock='Top';$bar.Height=26
$times=New-Object Windows.Forms.Label;$times.AutoSize=$true
$feed=New-Object Windows.Forms.TextBox;$feed.Multiline=$true;$feed.ReadOnly=$true;$feed.Dock='Fill';$feed.Height=180;$feed.ScrollBars='Vertical';$feed.BackColor=$form.BackColor;$feed.ForeColor=[Drawing.Color]::LightSkyBlue
$footer=New-Object Windows.Forms.Label;$footer.AutoSize=$true;$footer.Text='Closing this window does not stop the transfer. Keep the drive connected.'
foreach($control in @($heading,$destination,$phase,$bar,$times,$feed,$footer)) { $layout.Controls.Add($control) }
$layout.RowStyles.Add((New-Object Windows.Forms.RowStyle('Absolute',32)))|Out-Null
$layout.RowStyles.Add((New-Object Windows.Forms.RowStyle('Absolute',60)))|Out-Null
$layout.RowStyles.Add((New-Object Windows.Forms.RowStyle('Absolute',32)))|Out-Null
$layout.RowStyles.Add((New-Object Windows.Forms.RowStyle('Absolute',36)))|Out-Null
$layout.RowStyles.Add((New-Object Windows.Forms.RowStyle('Absolute',34)))|Out-Null
$layout.RowStyles.Add((New-Object Windows.Forms.RowStyle('Percent',100)))|Out-Null
$layout.RowStyles.Add((New-Object Windows.Forms.RowStyle('Absolute',40)))|Out-Null
function Duration([double]$seconds) { $t=[TimeSpan]::FromSeconds([Math]::Max(0,$seconds)); '{0:00}:{1:00}:{2:00}' -f [int][Math]::Floor($t.TotalHours),$t.Minutes,$t.Seconds }
$timer=New-Object Windows.Forms.Timer;$timer.Interval=250
$script:last=$null
$timer.Add_Tick({
 try {
  if(Test-Path -LiteralPath $StatusPath) { $script:last=Get-Content -LiteralPath $StatusPath -Raw | ConvertFrom-Json }
  $state=$script:last;if(!$state){return}
  $u=$state.Update;$elapsed=([DateTimeOffset]::UtcNow-[DateTimeOffset]::Parse($state.StartedUtc)).TotalSeconds
  $pct=[double]$state.Percent;$status=[string]$u.Phase;$done=[bool]$u.Finished;$failed=[bool]$u.Failed
  if($u.CleanupPending) {
   $receipt=Join-Path $state.Destination 'transfer-status.txt'
   if(Test-Path -LiteralPath $receipt) {
    $text=Get-Content -LiteralPath $receipt -Raw
    if($text.StartsWith('Move complete.')) { $status='Move complete';$done=$true;$pct=100 }
    elseif($text -match '(?i)incomplete|refused') { $status=$text.Trim();$done=$true;$failed=$true }
   }
  }
  if(!$done -and !$u.CleanupPending) {
   $owner=Get-Process -Id $state.ParentId -ErrorAction SilentlyContinue
   if(!$owner -or $owner.StartTime.ToUniversalTime() -ne [DateTime]::Parse($state.ParentStarted).ToUniversalTime()) { $status='Transfer host ended without a completion report. Check the original terminal and staging folder.';$done=$true;$failed=$true }
  }
  $bar.Value=[Math]::Min(100,[Math]::Max(0,[int]$pct))
  $destination.Text= 'Destination: '+$state.Destination
  $phase.Text=('{0}  |  {1:N0}%  |  {2:N0}/{3:N0} files' -f $status,$pct,$u.FilesDone,$u.TotalFiles)
  $stale=([DateTimeOffset]::UtcNow-[DateTimeOffset]::Parse($state.UpdatedUtc)).TotalSeconds
  $remaining=if($done){'00:00:00'}elseif($stale -gt 10){'Waiting for progress update ('+[int]$stale+'s)'}elseif($null -ne $state.RemainingCopySeconds){'~'+(Duration $state.RemainingCopySeconds)+' + final checks'}else{'Measuring current phase...'}
  $times.Text='Elapsed: '+(Duration $elapsed)+'   |   Remaining: '+$remaining
  $feed.Text=($state.Feed -join [Environment]::NewLine)
  if($done){$timer.Stop();$footer.Text=if($failed){'Transfer did not complete. Review the error; source/staging retained where possible.'}else{'Transfer complete. You can close this window.'};if($failed){$phase.ForeColor=[Drawing.Color]::Tomato}}
 } catch { $phase.Text='Waiting for transfer status... '+$_.Exception.Message }
})
$form.Add_Shown({[IO.File]::WriteAllText($StatusPath+'.ready','ready');$timer.Start()})
$form.Add_FormClosed({$timer.Stop();$timer.Dispose()})
[void]$form.ShowDialog()
