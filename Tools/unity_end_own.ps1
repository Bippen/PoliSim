param([Parameter(Mandatory = $true)][int]$ProcessId)
# END ONE UNITY THIS REPO'S TOOLS LAUNCHED - AND NOTHING ELSE (2026-09-29, COMPLETED.md s659).
#   powershell -NoProfile -ExecutionPolicy Bypass -File G:\UNITY\Projects\PoliSim\Tools\unity_end_own.ps1 -ProcessId 11496
# It ends the process only when ALL of these hold, and refuses otherwise:
#   - the pid is a Unity.exe now;
#   - PoliSim-captures/logs/unity_launched.tsv (written by Tools/unity_run.ps1 and Tools/warm.ps1 -Start at launch) has a row for that pid
#     whose start time equals the live process's start time to the second - so a reused pid, or Elias's own Editor, is never touched;
#   - its command line carries -projectPath for this project.
# The processes it started (AssetImportWorker, UnityShaderCompiler, the package server - children by parent pid) end with it.
# Every ending is appended to PoliSim-captures/logs/unity_ended.tsv. WHY: a warm host can hang in Unity's native teardown after
# 'Cleanup mono' (s656), where no code of the host's own runs and no timer inside it can end it.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$logs = Join-Path (Split-Path -Parent $root) 'PoliSim-captures\logs'
$register = Join-Path $logs 'unity_launched.tsv'

$p = Get-CimInstance Win32_Process -Filter "ProcessId=$ProcessId"
if ($null -eq $p) { "END: REFUSED - no process $ProcessId"; exit 2 }
if ($p.Name -ne 'Unity.exe') { "END: REFUSED - pid $ProcessId is $($p.Name), not Unity.exe"; exit 2 }
if ($p.CommandLine -notlike "*-projectPath*$root*") { "END: REFUSED - pid $ProcessId does not open $root"; exit 2 }
if (-not (Test-Path $register)) { "END: REFUSED - no launch register at $register"; exit 2 }
$live = (Get-Process -Id $ProcessId).StartTime
$row = $null
foreach ($line in Get-Content $register) {
  $f = $line -split "`t"
  if ($f.Count -ge 3 -and $f[0] -eq "$ProcessId") {
    $t = [DateTime]::Parse($f[1], [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind)
    if ([Math]::Abs(($t - $live).TotalSeconds) -lt 1) { $row = $f }
  }
}
if ($null -eq $row) { "END: REFUSED - pid $ProcessId (started $($live.ToString('o'))) is not in the launch register with that start time"; exit 2 }

$children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$ProcessId")
Stop-Process -Id $ProcessId -Force
foreach ($c in $children) { try { Stop-Process -Id $c.ProcessId -Force } catch {} }
Add-Content -Path (Join-Path $logs 'unity_ended.tsv') -Value ("{0}`t{1}`t{2}`t{3}`t{4}" -f (Get-Date).ToString('o'), $ProcessId, $row[2], $row[3], (($children | ForEach-Object { "$($_.Name):$($_.ProcessId)" }) -join ','))
"END: ended pid $ProcessId ($($row[2])) and $($children.Count) child process(es)"
exit 0
