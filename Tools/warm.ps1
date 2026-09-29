param([string]$Command = '', [int]$TimeoutSec = 1800, [switch]$Start, [switch]$Stop, [switch]$Status, [switch]$IdleProbe, [int]$IdleSeconds = 0)
# THE WARM EDITOR'S CLIENT (2026-09-22; COMPLETED.md s574, moved into the repository s575). One Editor, held warm, talked to
# through two files in PoliSim-captures/bridge/ (no port). The host is Assets/Editor/WarmEditor.cs.
#   powershell -NoProfile -File Tools/warm.ps1 -Start               start the host (one Unity launch; it holds the project)
#   powershell -NoProfile -File Tools/warm.ps1 -Command "bar all"   the cheap bar, in a host that SURVIVES it (s575)
#   powershell -NoProfile -File Tools/warm.ps1 -Command "bar sim"   the simulation bar, likewise
#   powershell -NoProfile -File Tools/warm.ps1 -Command "checks MetaTextCheck,DryFilmScopeCheck"
#   powershell -NoProfile -File Tools/warm.ps1 -Command "run PoliSim.EditorTools.EnergyMinistryDiagnostic.Run"
#   powershell -NoProfile -File Tools/warm.ps1 -Stop                tell it to quit and free the project; a host still running 90 s
#                                                                    after 'quit' is reported HUNG with the command that ends it (s659)
#   powershell -NoProfile -File Tools/warm.ps1 -IdleProbe           the check that a host exits on its idle timeout (a 20 s limit)
#   -Start -IdleSeconds N                                           a host with an N-second idle limit (the default is WarmEditor's)
# WHY IT IS HERE (s575): s574 wrote this client into the session's scratchpad directory, and the session ended mid-bar and
# took it along - the host kept running and answering, and nothing on disk could talk to it. A loop instrument lives in Tools/.
# A film needs the project: -Stop the host before any Unity film, -Start it again after.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.
$ErrorActionPreference = 'Stop'
$unity = 'G:\UNITY\Unity Hub\6000.5.6f1\Editor\Unity.exe'
if (-not (Test-Path $unity)) { $unity = 'G:\UNITY\Unity Hub\6000.5.6f1\Editor\Editor\Unity.exe' }
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$captures = Join-Path (Split-Path -Parent $root) 'PoliSim-captures'
$bridge = Join-Path $captures 'bridge'
$logs = Join-Path $captures 'logs'
$cmdFile = Join-Path $bridge 'cmd.txt'
$doneFile = Join-Path $bridge 'done.txt'
$readyFile = Join-Path $bridge 'ready.txt'
$pidFile = Join-Path $bridge 'host.pid'

function Send([string]$text, [int]$limit) {
  if (Test-Path $doneFile) { Remove-Item -Force $doneFile }
  $tmp = Join-Path $bridge 'cmd.tmp'
  [IO.File]::WriteAllText($tmp, $text)
  Move-Item -Force $tmp $cmdFile
  $t0 = Get-Date
  while (-not (Test-Path $doneFile)) {
    if (((Get-Date) - $t0).TotalSeconds -gt $limit) { "WARM: no answer in $limit s to '$text'"; return 1 }
    if (-not (Get-Process -Name 'Unity' -ErrorAction SilentlyContinue)) { "WARM: the host is GONE while '$text' was pending - read $logs\warm_host.log"; return 1 }
    Start-Sleep -Milliseconds 500
  }
  Start-Sleep -Milliseconds 200
  "WARM: answered in $([Math]::Round(((Get-Date) - $t0).TotalSeconds, 1)) s"
  $body = Get-Content -Raw $doneFile
  $body
  if ($body -match '(?m)^exit\t(\d+)') { return [int]$Matches[1] }
  return 1
}

if ($Start) {
  # s659: the same pre-launch check as every launch - no Unity.exe running, the lockfile not held.
  $pre = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'Tools\unity_run.ps1') -Method none -Label none -Check
  $pre
  if ($LASTEXITCODE -ne 0) { 'WARM: not started - the pre-launch check refused'; exit 3 }
  New-Item -ItemType Directory -Force -Path $bridge | Out-Null
  foreach ($f in @($cmdFile, $doneFile, $readyFile, $pidFile)) { if (Test-Path $f) { Remove-Item -Force $f } }
  $a = @('-batchmode', '-nographics', '-projectPath', $root, '-executeMethod', 'PoliSim.EditorTools.WarmEditor.Host', '-logFile', (Join-Path $logs 'warm_host.log'))
  if ($IdleSeconds -gt 0) { $a += "-warmidle=$IdleSeconds" }
  $hp = Start-Process -FilePath $unity -ArgumentList $a -PassThru
  [IO.File]::WriteAllText($pidFile, "$($hp.Id)")
  Add-Content -Path (Join-Path $logs 'unity_launched.tsv') -Value ("{0}`t{1}`t{2}`t{3}" -f $hp.Id, $hp.StartTime.ToString('o'), 'warm_host', 'PoliSim.EditorTools.WarmEditor.Host')
  $t0 = Get-Date
  while (-not (Test-Path $readyFile)) {
    if (((Get-Date) - $t0).TotalSeconds -gt 300) { 'WARM: the host did not come up in 300 s'; exit 1 }
    Start-Sleep -Milliseconds 500
  }
  "WARM: up in $([int]((Get-Date) - $t0).TotalSeconds) s"
  exit 0
}

if ($Status) {
  if ((Test-Path $readyFile) -and (Get-Process -Name 'Unity' -ErrorAction SilentlyContinue)) { "WARM: host ready since $(Get-Content $readyFile)" } else { 'WARM: no host' }
  exit 0
}

# s659: a host that has logged 'host down' and is still a process after the grace is HUNG IN UNITY'S NATIVE TEARDOWN (s656: after
# 'Cleanup mono', where no code of the host's runs - neither 'quit' nor the idle timer can end it). It is reported with the one
# command that ends it (Tools/unity_end_own.ps1 - only a process registered at launch); this script never ends a process itself.
function WaitHostGone([int]$hostPid, [int]$grace) {
  $t0 = Get-Date
  while (Get-Process -Id $hostPid -ErrorAction SilentlyContinue) {
    if (((Get-Date) - $t0).TotalSeconds -gt $grace) { return $false }
    Start-Sleep -Milliseconds 500
  }
  return $true
}
function HostPid { if (Test-Path $pidFile) { return [int](Get-Content $pidFile) } ; return 0 }
function Hung([int]$hostPid, [int]$grace) {
  "WARM: HUNG IN TEARDOWN - pid $hostPid logged 'host down' and is still running after $grace s; it holds the project until it is ended:"
  "  powershell -NoProfile -ExecutionPolicy Bypass -File $root\Tools\unity_end_own.ps1 -ProcessId $hostPid"
}

if ($Stop) {
  $hostPid = HostPid
  if ($hostPid -eq 0 -or -not (Get-Process -Id $hostPid -ErrorAction SilentlyContinue)) { 'WARM: no host'; exit 0 }
  $null = Send 'quit' 60
  if (-not (WaitHostGone $hostPid 90)) { Hung $hostPid 90; exit 4 }
  foreach ($f in @($cmdFile, $doneFile, $readyFile, $pidFile)) { if (Test-Path $f) { Remove-Item -Force $f } }
  'WARM: host down, the project is free'
  exit 0
}

# s659: THE CHECK THAT A HOST EXITS ON ITS IDLE TIMEOUT - a real host started with a short idle limit, left alone, and timed to its
# process's end. It launches Unity, so it runs outside the bar (the bar runs inside one); run it after any change to WarmEditor.
if ($IdleProbe) {
  $a = @('-Start', '-IdleSeconds', '20')
  & powershell -NoProfile -ExecutionPolicy Bypass -File $MyInvocation.MyCommand.Path @a
  if ($LASTEXITCODE -ne 0) { 'IDLE PROBE: the host did not start'; exit 1 }
  $hostPid = HostPid
  $t0 = Get-Date
  while (-not (Select-String -Path (Join-Path $logs 'warm_host.log') -Pattern 'WARM: idle past' -SimpleMatch -Quiet)) {
    if (((Get-Date) - $t0).TotalSeconds -gt 120) { 'IDLE PROBE: FAILED - no idle line in 120 s'; exit 1 }
    Start-Sleep -Seconds 1
  }
  $idle = Get-Date
  if (-not (WaitHostGone $hostPid 90)) { 'IDLE PROBE: FAILED'; Hung $hostPid 90; exit 4 }
  foreach ($f in @($cmdFile, $doneFile, $readyFile, $pidFile)) { if (Test-Path $f) { Remove-Item -Force $f } }
  "IDLE PROBE: PASSED - pid $hostPid logged its idle exit $([int]($idle - $t0).TotalSeconds) s after ready and was gone $([Math]::Round(((Get-Date) - $idle).TotalSeconds, 1)) s later"
  exit 0
}

if ($Command) { $out = @(Send $Command $TimeoutSec); if ($out.Count -gt 1) { $out[0..($out.Count - 2)] }; exit [int]$out[-1] }
'WARM: give -Start, -Command "<cmd>", -Stop, -Status or -IdleProbe'
exit 2
