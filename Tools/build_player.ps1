param([string]$KnownDiffs = '', [string]$Out = 'G:\UNITY\Builds\Incumbent-dev', [string]$Save = 'playtest_4_precampaign_day1', [switch]$SmokeOnly, [switch]$Windowed, [int]$SmokeTimeoutSec = 600)
# THE WINDOWS PLAYER, BUILT AND SMOKED (2026-09-29, COMPLETED.md s668).
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build_player.ps1               build into G:\UNITY\Builds\Incumbent-dev, then smoke
#   ... -SmokeOnly                                                                            smoke the build already there
#   ... -Save <name>                                                                          the save the smoke loads (default: the protocol's first)
#   ... -SmokeOnly -Windowed                                                                  the smoke with a real graphics device and window
# 1. The build: one Unity job through Tools/unity_run.ps1 (the pre-launch check, the launch register), PlayerBuild.BuildWindows. The folder is
#    outside the repository and never committed; productName and companyName are untouched (s651).
# 2. The smoke: the built Incumbent.exe, -batchmode -nographics -smoke=<save>: PoliSim.Testing.PlayerSmoke loads the save through the game's own
#    load path, runs 30 game days and quits 0. PASSED needs exit 0 AND the log's 'SMOKE: PASSED' line - an exit alone proves nothing (s656).
# Hygiene (s672): the five settings files a build dirties are restored when their diffs are the known ones (Tools/build_known_diffs.tsv), and the
#    run fails if the tree does not stand as it stood before. WARNING: the build bakes UnityConnectSettings m_Enabled = 1 into the player whatever the
#    committed value says - restoring the file keeps the repository's 0, not the exe's (s672; Elias's to decide).
# 3. The icon: the exe's own icon, extracted to <Out>\..\Incumbent-dev_icon.png for reading.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$logs = Join-Path (Split-Path -Parent $root) 'PoliSim-captures\logs'
$exe = Join-Path $Out 'Incumbent.exe'

# s672: BUILD HYGIENE. A build writes five settings files into the tree (Tools/build_known_diffs.tsv names each change it is known to make). They are
# refused if already dirty (a restore must never take Elias's own edit), restored after the build only when every changed line is a known one, and
# the tree must stand exactly as it stood before the build - anything else fails, left in place for inspection.
# git writes its line-ending warnings to stderr, which PowerShell 5.1 under 'Stop' turns into a terminating error: every git call runs under 'Continue'
function Invoke-RepoGit([string[]]$GitArgs) { $e = $ErrorActionPreference; $ErrorActionPreference = 'Continue'; try { @(& git.exe -C $root @GitArgs 2>$null) } finally { $ErrorActionPreference = $e } }
function GitStatus { Invoke-RepoGit @('status', '--porcelain', '--untracked-files=all') }
$known = @{}
if (-not $KnownDiffs) { $KnownDiffs = Join-Path $root 'Tools\build_known_diffs.tsv' }
foreach ($line in Get-Content $KnownDiffs) {
  if ($line -match '^#' -or $line.Trim().Length -eq 0) { continue }
  $f = $line -split "`t", 2
  if (-not $known.ContainsKey($f[0])) { $known[$f[0]] = @() }
  $known[$f[0]] += $f[1]
}
$pre = GitStatus
if (-not $SmokeOnly) {
  $held = @($pre | Where-Object { $known.ContainsKey($_.Substring(3)) })
  if ($held.Count -gt 0) { "HYGIENE: REFUSED - these carry uncommitted edits a build would overwrite: $($held -join '; ')"; exit 3 }
  & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'Tools\unity_run.ps1') -Method PoliSim.EditorTools.PlayerBuild.BuildWindows -Label player_build -Extra "-buildout=$Out" -TimeoutSec 3600
  $code = $LASTEXITCODE
  Select-String -Path (Join-Path $logs 'player_build.log') -Pattern '^BUILD:|error CS' -CaseSensitive | ForEach-Object { $_.Line }
  if ($code -ne 0 -or -not (Test-Path $exe)) { "BUILD PLAYER: FAILED - Unity exit $code, exe present: $(Test-Path $exe)"; exit 1 }
  $unknown = 0
  foreach ($line in (GitStatus | Where-Object { $pre -notcontains $_ })) {
    $path = $line.Substring(3)
    if (-not $known.ContainsKey($path)) { continue }   # judged by the final comparison below
    $changed = @(Invoke-RepoGit @('diff', '-U0', '--', $path) | ForEach-Object { $_ -replace "`r$", '' } | Where-Object { $_ -match '^[-+]' -and $_ -notmatch '^(\+\+\+|---)' } | ForEach-Object { $_.Substring(1) })
    $odd = @($changed | Where-Object { $l = $_; -not ($known[$path] | Where-Object { $l -match $_ }) })
    if ($odd.Count -gt 0) { "HYGIENE: UNKNOWN DIFF in $path - not restored: $($odd -join ' | ')"; $unknown++; continue }
    $null = Invoke-RepoGit @('checkout', '--', $path)
    "HYGIENE: restored $path - $($changed.Count) changed line(s), every one known"
  }
  if ($unknown -gt 0) { "BUILD PLAYER: FAILED - $unknown file(s) carry a diff the build is not known to write"; exit 6 }
}

$slog = Join-Path $logs 'player_smoke.log'
if (Test-Path $slog) { Remove-Item -Force $slog }
# -Windowed: the player with its graphics device and a window (a -nographics smoke renders nothing, so a shader or a UI failure only shows here)
$smokeArgs = @("-smoke=$Save", '-logFile', $slog)
if (-not $Windowed) { $smokeArgs = @('-batchmode', '-nographics') + $smokeArgs }
$p = Start-Process -FilePath $exe -ArgumentList $smokeArgs -PassThru
Add-Content -Path (Join-Path $logs 'unity_launched.tsv') -Value ("{0}`t{1}`t{2}`t{3}" -f $p.Id, $p.StartTime.ToString('o'), 'player_smoke', $exe)
if (-not $p.WaitForExit($SmokeTimeoutSec * 1000)) { "SMOKE: TIMEOUT - pid $($p.Id) still running after $SmokeTimeoutSec s"; exit 124 }
$lines = @(Select-String -Path $slog -Pattern '^SMOKE:|Exception' -CaseSensitive | ForEach-Object { $_.Line })
$lines | Select-Object -First 12
$passed = $lines | Where-Object { $_ -like 'SMOKE: PASSED*' }
"SMOKE: player exit $($p.ExitCode)"

Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Icon]::ExtractAssociatedIcon($exe)
$png = Join-Path (Split-Path -Parent $Out) 'Incumbent-dev_icon.png'
$icon.ToBitmap().Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
"ICON: extracted $($icon.Width)x$($icon.Height) -> $png"

if ($p.ExitCode -ne 0 -or -not $passed) { 'BUILD PLAYER: SMOKE FAILED'; exit 1 }
# s672: THE CHECK - the tree stands exactly as it stood before the build; the output folder is outside the repository, so any difference is a leak
$diff = @(Compare-Object -ReferenceObject $pre -DifferenceObject (GitStatus) | ForEach-Object { "$($_.SideIndicator) $($_.InputObject)" })
if ($diff.Count -gt 0) { "HYGIENE: FAILED - the build left the tree dirty outside its output folder: $($diff -join '; ')"; exit 6 }
'HYGIENE: the tree stands as it stood before the build'
'BUILD PLAYER: PASSED'
exit 0
