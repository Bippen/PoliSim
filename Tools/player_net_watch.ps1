# THE PLAYER'S OUTBOUND CONNECTIONS, WATCHED (2026-09-30, COMPLETED.md s678; Elias's ruling: Unity analytics OFF).
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/player_net_watch.ps1 [-Exe <path>] [-PlayerArgs "<args>"] [-TimeoutSec 600]
# Launches the player (by default the build's own smoke, -batchmode -nographics -smoke=<save>), polls every 100 ms the TCP connections owned by the
# player AND every process it started (the crash handler), and names each remote endpoint. Two witnesses, because an address alone can be a shared CDN:
#   1. ADDRESS - the endpoint is one the watched hosts resolve to (A and AAAA, through their CNAME chains, resolved before AND after the run);
#   2. NAME    - the DNS client cache holds a watched host after the run that it did not hold before (the cache is flushed first when the OS allows).
# FAILS (exit 7) when either witness sees a watched host; exit 0 means the player ran and contacted neither. A player that did not run is exit 2.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.
param(
  [string]$Exe = 'G:\UNITY\Builds\Incumbent-dev\Incumbent.exe',
  [string]$PlayerArgs = '',
  [string]$Save = 'playtest_4_precampaign_day1',
  [int]$TimeoutSec = 600,
  [string[]]$Watched = @('cdp.cloud.unity3d.com', 'config.uca.cloud.unity3d.com')
)
$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
$logs = Join-Path (Split-Path -Parent $root) 'PoliSim-captures\logs'
if (-not (Test-Path $Exe)) { "WATCH: no player at $Exe"; exit 2 }

function Resolve-All([string]$name) {
  $ips = @()
  foreach ($t in @('A', 'AAAA')) {
    try { $ips += @(Resolve-DnsName -Name $name -Type $t -ErrorAction Stop | Where-Object { $_.IPAddress } | ForEach-Object { $_.IPAddress }) } catch { }
  }
  $ips | Sort-Object -Unique
}
function Cached([string]$name) { @(Get-DnsClientCache -ErrorAction SilentlyContinue | Where-Object { $_.Entry -eq $name -or $_.Name -eq $name }).Count -gt 0 }

$addr = @{}
foreach ($h in $Watched) { foreach ($ip in (Resolve-All $h)) { $addr[$ip] = $h } }
$flushed = $false
try { Clear-DnsClientCache -ErrorAction Stop; $flushed = $true } catch { }
$before = @{}; foreach ($h in $Watched) { $before[$h] = Cached $h }
"WATCH: watched hosts $($Watched -join ', ') -> $($addr.Count) address(es); DNS cache flushed: $flushed; cached before: $(($Watched | ForEach-Object { "$_=$($before[$_])" }) -join ' ')"

if ($PlayerArgs -eq '') { $PlayerArgs = "-batchmode -nographics -smoke=$Save -logFile `"$(Join-Path $logs 'player_netwatch.log')`"" }
# connections to a watched address that exist BEFORE the launch (an earlier run's TIME_WAIT lingers up to two minutes) are not this run's
$prior = @{}
foreach ($c in @(Get-NetTCPConnection -ErrorAction SilentlyContinue | Where-Object { $addr.ContainsKey($_.RemoteAddress) })) { $prior["$($c.RemoteAddress):$($c.RemotePort)/$($c.LocalPort)"] = $true }
"WATCH: $($prior.Count) connection(s) to a watched address already open or closing before the launch - excluded"
$p = Start-Process -FilePath $Exe -ArgumentList $PlayerArgs -PassThru
Add-Content -Path (Join-Path $logs 'unity_launched.tsv') -Value ("{0}`t{1}`t{2}`t{3}" -f $p.Id, $p.StartTime.ToString('o'), 'player_netwatch', $Exe)
$seen = @{}
$deadline = (Get-Date).AddSeconds($TimeoutSec)
while (-not $p.HasExited -and (Get-Date) -lt $deadline) {
  # THE FAST WITNESS: the TCP table read directly (a millisecond, no PID) every 5 ms for ~100 ms - any connect() sits in SYN_SENT for at least
  # one round trip, so a connection cannot open and close between two reads (the slow per-PID read above can miss one the server closes first)
  $until = (Get-Date).AddMilliseconds(100)
  while ((Get-Date) -lt $until -and -not $p.HasExited) {
    foreach ($c in [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpConnections()) {
      $ra = $c.RemoteEndPoint.Address.ToString()
      if ($c.RemoteEndPoint.Address.IsIPv4MappedToIPv6) { $ra = $c.RemoteEndPoint.Address.MapToIPv4().ToString() }
      if (-not $addr.ContainsKey($ra) -or $prior.ContainsKey("${ra}:$($c.RemoteEndPoint.Port)/$($c.LocalEndPoint.Port)")) { continue }
      $k = "${ra}:$($c.RemoteEndPoint.Port)"
      if (-not $seen.ContainsKey($k)) { $seen[$k] = "{0} {1} (fast witness)" -f (Get-Date).ToString('HH:mm:ss.fff'), $c.State }
    }
    Start-Sleep -Milliseconds 5
  }
  $pids = @($p.Id) + @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$($p.Id)" -ErrorAction SilentlyContinue | ForEach-Object { $_.ProcessId })
  foreach ($c in @(Get-NetTCPConnection -ErrorAction SilentlyContinue | Where-Object { $pids -contains $_.OwningProcess -and $_.RemoteAddress -notin @('0.0.0.0', '::', '127.0.0.1', '::1') })) {
    $k = "$($c.RemoteAddress):$($c.RemotePort)"
    if (-not $seen.ContainsKey($k)) { $seen[$k] = "{0} pid {1} {2}" -f (Get-Date).ToString('HH:mm:ss.fff'), $c.OwningProcess, $c.State }
  }
  # a closed connection lingers in TIME_WAIT owned by pid 0 - so any connection to a WATCHED address during the run counts, whatever its owner (run with no Editor open)
  foreach ($c in @(Get-NetTCPConnection -ErrorAction SilentlyContinue | Where-Object { $addr.ContainsKey($_.RemoteAddress) })) {
    if ($prior.ContainsKey("$($c.RemoteAddress):$($c.RemotePort)/$($c.LocalPort)")) { continue }
    $k = "$($c.RemoteAddress):$($c.RemotePort)"
    if (-not $seen.ContainsKey($k)) { $seen[$k] = "{0} pid {1} {2} (any owner)" -f (Get-Date).ToString('HH:mm:ss.fff'), $c.OwningProcess, $c.State }
  }
}
if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force; "WATCH: TIMEOUT - the player ran past $TimeoutSec s and was stopped" }
$p.WaitForExit()
$after = @{}; foreach ($h in $Watched) { $after[$h] = Cached $h }   # read FIRST: the resolution below would put the names in the cache itself

foreach ($h in $Watched) { foreach ($ip in (Resolve-All $h)) { $addr[$ip] = $h } }   # CDN answers rotate: the union of both resolutions
$hits = 0
"WATCH: the player (pid $($p.Id)) exited $($p.ExitCode); $($seen.Count) remote endpoint(s) seen"
foreach ($k in ($seen.Keys | Sort-Object)) {
  $ip = $k.Substring(0, $k.LastIndexOf(':'))
  $name = if ($addr.ContainsKey($ip)) { $hits++; "WATCHED $($addr[$ip])" } else { try { (Resolve-DnsName -Name $ip -Type PTR -ErrorAction Stop | Select-Object -First 1).NameHost } catch { '(no PTR)' } }
  "  $k  $($seen[$k])  $name"
}
foreach ($h in $Watched) {
  if ($after[$h] -and -not $before[$h]) { $hits++; "  NAME: $h entered the DNS cache during the run" }
  elseif ($after[$h]) { "  NAME: $h was already cached before the run - this witness is blind to it" }
  else { "  NAME: $h not looked up" }
}
if ($hits -gt 0) { "WATCH: FAILED - the player contacted a watched host ($hits sighting(s))"; exit 7 }
"WATCH: CLEAN - the player contacted neither watched host"
exit 0
