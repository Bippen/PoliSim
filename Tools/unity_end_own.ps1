param([Parameter(Mandatory = $true)][int]$ProcessId)
# END ONE UNITY THIS REPO'S TOOLS LAUNCHED - AND NOTHING ELSE (2026-09-29, COMPLETED.md s659).
#   powershell -NoProfile -ExecutionPolicy Bypass -File G:\UNITY\Projects\PoliSim\Tools\unity_end_own.ps1 -ProcessId 11496
# It ends the process only when ALL of these hold, and refuses otherwise:
#   - the pid is a Unity.exe now;
#   - PoliSim-captures/logs/unity_launched.tsv (written by Tools/unity_run.ps1 and Tools/warm.ps1 -Start at launch) has a row for that pid
#     whose start time equals the live process's start time to the second - so a reused pid, or Elias's own Editor, is never touched;
#   - its command line carries -projectPath for this project.
# The processes it started (AssetImportWorker, UnityShaderCompiler, the package server - children by parent pid) end with it.
# Every ending is appended to PoliSim-captures/logs/unity_ended.tsv. Every ending is also a row of the committed hang ledger, Tools/unity_hangs.tsv (s692).
# WHY: a warm host can hang in Unity's native teardown after
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

# THE HANG LEDGER (ruled 2026-09-30, COMPLETED.md s692): every launch this script ends is a hang, and each is one row of Tools/unity_hangs.tsv -
# committed, so a pattern can show across sessions: when, the label and method, how long it had lived, the PHASE read from its own log (startup:
# the method never began - a domain reload, a compile error; running: the method's own lines, no end; after the run: its result line printed and
# the process never left; teardown: 'Cleanup mono' and then nothing, s656/s659), its working set, and the log's last line as the evidence.
$proc = Get-Process -Id $ProcessId
$workingSetMb = [Math]::Round($proc.WorkingSet64 / 1MB)
$minutes = [Math]::Round(((Get-Date) - $live).TotalMinutes, 1)
$phase = 'unknown (no log)'
$lastLine = ''
$launchLog = Join-Path $logs "$($row[2]).log"
if (Test-Path $launchLog) {
  # s698: the hung process still HOLDS its log open - read it through a stream that shares write access (ReadAllText and Get-Content were refused
  # with an IOException on the first real hang the ledger met, cheap698, and the end itself failed with them)
  $fs = [IO.File]::Open($launchLog, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
  try { $sr = New-Object IO.StreamReader($fs); $text = $sr.ReadToEnd() } finally { $fs.Dispose() }
  $tail = @(($text -split "`r?`n") | Select-Object -Last 60 | Where-Object { $_.Trim() -ne '' })
  if ($tail.Count -gt 0) { $lastLine = $tail[$tail.Count - 1].Trim() -replace "`t", ' ' }
  if ($lastLine.Length -gt 160) { $lastLine = $lastLine.Substring(0, 160) }
  if ($text -match 'Cleanup mono') { $phase = 'teardown' }
  elseif ($text -match 'CHECKS: [0-9]+ of [0-9]+|SHOT: done|SMOKE: (PASS|FAIL)') { $phase = 'after the run' }
  elseif ($text -match 'error CS[0-9]+') { $phase = 'startup (compile errors)' }
  elseif ($text -match 'CHECKS: running|SHOT: |SMOKE: armed') { $phase = 'running' }
  else { $phase = 'startup' }
}
$ledger = Join-Path $root 'Tools\unity_hangs.tsv'
if (-not (Test-Path $ledger)) { Set-Content -Path $ledger -Value "ended`tpid`tlabel`tmethod`tstarted`tminutes_alive`tphase`tworking_set_mb`tlast_log_line" -Encoding ASCII }

$children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$ProcessId")
Stop-Process -Id $ProcessId -Force
Add-Content -Path $ledger -Encoding UTF8 -Value ("{0}`t{1}`t{2}`t{3}`t{4}`t{5}`t{6}`t{7}`t{8}" -f (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss'), $ProcessId, $row[2], $row[3], $live.ToString('yyyy-MM-ddTHH:mm:ss'), $minutes.ToString([Globalization.CultureInfo]::InvariantCulture), $phase, $workingSetMb, $lastLine)
foreach ($c in $children) { try { Stop-Process -Id $c.ProcessId -Force } catch {} }
Add-Content -Path (Join-Path $logs 'unity_ended.tsv') -Value ("{0}`t{1}`t{2}`t{3}`t{4}" -f (Get-Date).ToString('o'), $ProcessId, $row[2], $row[3], (($children | ForEach-Object { "$($_.Name):$($_.ProcessId)" }) -join ','))
"END: ended pid $ProcessId ($($row[2])) and $($children.Count) child process(es); the hang ledger row: phase '$phase', $workingSetMb MB, $minutes min alive"
exit 0
