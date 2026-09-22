param([string]$Command = '', [int]$TimeoutSec = 1800, [switch]$Start, [switch]$Stop, [switch]$Status)
# THE WARM EDITOR'S CLIENT (2026-09-22; COMPLETED.md s574, moved into the repository s575). One Editor, held warm, talked to
# through two files in PoliSim-captures/bridge/ (no port). The host is Assets/Editor/WarmEditor.cs.
#   powershell -NoProfile -File Tools/warm.ps1 -Start               start the host (one Unity launch; it holds the project)
#   powershell -NoProfile -File Tools/warm.ps1 -Command "bar all"   the cheap bar, in a host that SURVIVES it (s575)
#   powershell -NoProfile -File Tools/warm.ps1 -Command "bar sim"   the simulation bar, likewise
#   powershell -NoProfile -File Tools/warm.ps1 -Command "checks MetaTextCheck,DryFilmScopeCheck"
#   powershell -NoProfile -File Tools/warm.ps1 -Command "run PoliSim.EditorTools.EnergyMinistryDiagnostic.Run"
#   powershell -NoProfile -File Tools/warm.ps1 -Stop                tell it to quit and free the project
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
  if (Get-Process -Name 'Unity' -ErrorAction SilentlyContinue) { 'WARM: a Unity is already running - stop it first'; exit 3 }
  New-Item -ItemType Directory -Force -Path $bridge | Out-Null
  foreach ($f in @($cmdFile, $doneFile, $readyFile)) { if (Test-Path $f) { Remove-Item -Force $f } }
  $a = @('-batchmode', '-nographics', '-projectPath', $root, '-executeMethod', 'PoliSim.EditorTools.WarmEditor.Host', '-logFile', (Join-Path $logs 'warm_host.log'))
  Start-Process -FilePath $unity -ArgumentList $a | Out-Null
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

if ($Stop) {
  if (-not (Get-Process -Name 'Unity' -ErrorAction SilentlyContinue)) { 'WARM: no host'; exit 0 }
  $null = Send 'quit' 60
  $t0 = Get-Date
  while (Get-Process -Name 'Unity' -ErrorAction SilentlyContinue) {
    if (((Get-Date) - $t0).TotalSeconds -gt 120) { 'WARM: the host did not exit in 120 s - it will on its idle timer; never kill it'; exit 1 }
    Start-Sleep -Milliseconds 500
  }
  foreach ($f in @($cmdFile, $doneFile, $readyFile)) { if (Test-Path $f) { Remove-Item -Force $f } }
  'WARM: host down, the project is free'
  exit 0
}

if ($Command) { $out = @(Send $Command $TimeoutSec); if ($out.Count -gt 1) { $out[0..($out.Count - 2)] }; exit [int]$out[-1] }
'WARM: give -Start, -Command "<cmd>", -Stop or -Status'
exit 2
