param([Parameter(Mandatory = $true)][string]$Method, [Parameter(Mandatory = $true)][string]$Label, [switch]$Window, [string]$Extra = '', [int]$TimeoutSec = 1800, [switch]$Check)
# ONE UNITY JOB, LAUNCHED CLEAN (ruled 2026-09-29, COMPLETED.md s656's pass; the one-job rule is s648).
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/unity_run.ps1 -Method PoliSim.EditorTools.CheckSuite.RunDocumentBatch -Label docs656
#   ... -Window                         a real (windowed) film: no -batchmode
#   ... -Extra "-shotcountry=Sweden -shotwidth=1280"   ONE string, split on spaces: powershell -File passes a comma list as a
#                                       single argument, and Unity then reads none of it (s656: a dry film ran USA@1600 exit 0)
#   ... -Check                          the pre-launch check alone, launching nothing
# BEFORE a launch it lists every Unity.exe and refuses if one runs, and refuses if Temp/UnityLockfile is HELD (a file nothing
# holds is a leftover and is fine - Unity reopens it). A job that starts on top of a leftover fails quietly (s656: a warm host
# that never exited held the lock and the next three launches filmed nothing).
# Every launch is registered in PoliSim-captures/logs/unity_launched.tsv (pid, start time, label) - Tools/unity_end_own.ps1
# ends only a process registered there. The log is PoliSim-captures/logs/<Label>.log; the exit code is Unity's.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.
$ErrorActionPreference = 'Stop'
$unity = 'G:\UNITY\Unity Hub\6000.5.6f1\Editor\Unity.exe'
if (-not (Test-Path $unity)) { $unity = 'G:\UNITY\Unity Hub\6000.5.6f1\Editor\Editor\Unity.exe' }
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$logs = Join-Path (Split-Path -Parent $root) 'PoliSim-captures\logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null

$running = @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'")
foreach ($p in $running) { "PRELAUNCH: Unity.exe pid $($p.ProcessId) started $($p.CreationDate): $($p.CommandLine)" }
if ($running.Count -gt 0) { 'PRELAUNCH: REFUSED - a Unity is running; one job at a time'; exit 3 }
$lock = Join-Path $root 'Temp\UnityLockfile'
if (Test-Path $lock) {
  try { $fs = [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None'); $fs.Close(); 'PRELAUNCH: no Unity running; the lockfile is a leftover nothing holds' }
  catch { 'PRELAUNCH: REFUSED - Temp/UnityLockfile is held with no Unity.exe listed'; exit 3 }
} else { 'PRELAUNCH: no Unity running; no lockfile' }
if ($Check) { exit 0 }

$log = Join-Path $logs "$Label.log"
$a = @()
if (-not $Window) { $a += '-batchmode' }
$a += @('-projectPath', $root, '-executeMethod', $Method) + @($Extra -split '\s+' | Where-Object { $_ }) + @('-logFile', $log)
"ARGS: $($a -join ' | ')"
$p = Start-Process -FilePath $unity -ArgumentList $a -PassThru
Add-Content -Path (Join-Path $logs 'unity_launched.tsv') -Value ("{0}`t{1}`t{2}`t{3}" -f $p.Id, $p.StartTime.ToString('o'), $Label, $Method)
"LAUNCHED: pid $($p.Id) $Label ($Method) -> $log"

# THE HANG WATCHDOG (ruled 2026-09-30, round 4 follow-up 3; COMPLETED.md s702). Four batch runs printed their result and never left - each in
# teardown, after 'CodeReloadManager destroyed' (Tools/unity_hangs.tsv). The log is read as it grows; once it carries the run's RESULT LINE -
#   CHECKS: ... exiting N.        the suite, the simulation group, a named subset, the document group
#   SHOT: DRY done - ... exiting N  a dry film
#   SHOT: exiting N.               a real film, after its before-exit guards
#   EXITDIAG: quitting             any other method: the Editor has begun to quit (Assets/Editor/ExitDiagnostics.cs); its code is not known
# - a process not gone 90 s later is ended by Tools/unity_end_own.ps1, recorded in the hang ledger as hung at teardown with the result kept,
# and this script exits with the KEPT code (124 where the line carries none), so a chain continues on the result the run printed.
$WatchdogSec = 90
$resultRx = '^(CHECKS: .*exiting (?<code>\d+)\.|SHOT: DRY done - .*exiting (?<code>\d+)|SHOT: exiting (?<code>\d+)\.|EXITDIAG: quitting)'
$deadline = (Get-Date).AddSeconds($TimeoutSec)
$pos = 0L; $carry = ''; $resultAt = $null; $resultLine = $null; $resultCode = $null; $watchdogCode = $null
while (-not $p.WaitForExit(2000)) {
  if ((Get-Date) -gt $deadline) { "TIMEOUT: pid $($p.Id) still running after $TimeoutSec s - left running, not ended"; exit 124 }
  if ($null -eq $resultAt -and (Test-Path $log)) {
    try {
      $fs = [IO.File]::Open($log, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
      try {
        if ($fs.Length -gt $pos) {
          [void]$fs.Seek($pos, [IO.SeekOrigin]::Begin)
          $sr = New-Object IO.StreamReader($fs)
          $chunk = $carry + $sr.ReadToEnd(); $pos = $fs.Position
          $lines = $chunk -split "`r?`n"; $carry = $lines[$lines.Count - 1]
          for ($i = 0; $i -lt $lines.Count - 1; $i++) {
            if ($lines[$i] -cmatch $resultRx) { $resultAt = Get-Date; $resultLine = $lines[$i].Trim(); if ($Matches['code']) { $resultCode = [int]$Matches['code'] }; break }
          }
        }
      } finally { $fs.Dispose() }
    } catch { }
  }
  if ($null -ne $resultAt -and ((Get-Date) - $resultAt).TotalSeconds -ge $WatchdogSec) {
    "WATCHDOG: pid $($p.Id) printed its result $WatchdogSec s ago and has not exited - '$resultLine'"
    $kept = if ($null -ne $resultCode) { "code $resultCode - $resultLine" } else { "code unknown - $resultLine" }
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'Tools\unity_end_own.ps1') -ProcessId $p.Id -Kept $kept
    $code = if ($null -ne $resultCode) { $resultCode } else { 124 }
    "EXITED: pid $($p.Id) ended by the watchdog; the result kept: code $code"
    if ($Method -notmatch 'UiScreenshotCapture\.(Run|RunDry)$') { exit $code }
    $watchdogCode = $code
    break
  }
}
if ($null -eq $watchdogCode) { "EXITED: pid $($p.Id) code $($p.ExitCode)" }

# s661: A FILM'S OWN HEADER MUST MATCH ITS REQUEST. The harness logs 'SHOT: HEADER country=<c> width=<w>' for every session it runs, from what it
# found (the real film's width is the Game View's own). The request is read from -Extra WITHOUT regard to case - what was meant - so an argument
# Unity cannot read (a comma list, a mis-cased switch) shows as a header that differs. No header at all is a failure too. Exit 5 on either.
if ($Method -match 'UiScreenshotCapture\.(Run|RunDry)$') {
  $want = @{}
  foreach ($t in ($Extra -split '\s+')) {
    if ($t -match '^(?i)-shot(countries|country)=(.+)$') { $want['country'] = @($Matches[2] -split ',') }
    if ($t -match '^(?i)-shotwidth=(\d+)$') { $want['width'] = @($Matches[1]) }
    if ($t -match '^(?i)-shotgeometries=(.+)$') { $want['width'] = @(($Matches[1] -split ',') | ForEach-Object { ($_ -split 'x')[0] }) }
  }
  $headers = @(Select-String -Path $log -Pattern '^SHOT: HEADER country=(\S+) width=(\d+)' -CaseSensitive)
  if ($headers.Count -eq 0) { "FILM HEADER: FAILED - the log carries no 'SHOT: HEADER' line; what ran is unknown"; exit 5 }
  $bad = 0
  foreach ($h in $headers) {
    $c = $h.Matches[0].Groups[1].Value; $w = $h.Matches[0].Groups[2].Value
    $okC = -not $want.ContainsKey('country') -or ($want['country'] | Where-Object { $_ -ieq $c })
    $okW = -not $want.ContainsKey('width') -or ($want['width'] -contains $w)
    if ($okC -and $okW) { "FILM HEADER: ok - country=$c width=$w" } else { "FILM HEADER: MISMATCH - the film ran country=$c width=$w; asked country=$($want['country'] -join ',') width=$($want['width'] -join ',')"; $bad++ }
  }
  if ($bad -gt 0) { "FILM HEADER: FAILED - $bad session(s) differ from the request"; exit 5 }
}
if ($null -ne $watchdogCode) { exit $watchdogCode }
exit $p.ExitCode
