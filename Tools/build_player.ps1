param([string]$Out = 'G:\UNITY\Builds\Incumbent-dev', [string]$Save = 'playtest_4_precampaign_day1', [switch]$SmokeOnly, [switch]$Windowed, [int]$SmokeTimeoutSec = 600)
# THE WINDOWS PLAYER, BUILT AND SMOKED (2026-09-29, COMPLETED.md s668).
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build_player.ps1               build into G:\UNITY\Builds\Incumbent-dev, then smoke
#   ... -SmokeOnly                                                                            smoke the build already there
#   ... -Save <name>                                                                          the save the smoke loads (default: the protocol's first)
#   ... -SmokeOnly -Windowed                                                                  the smoke with a real graphics device and window
# 1. The build: one Unity job through Tools/unity_run.ps1 (the pre-launch check, the launch register), PlayerBuild.BuildWindows. The folder is
#    outside the repository and never committed; productName and companyName are untouched (s651).
# 2. The smoke: the built Incumbent.exe, -batchmode -nographics -smoke=<save>: PoliSim.Testing.PlayerSmoke loads the save through the game's own
#    load path, runs 30 game days and quits 0. PASSED needs exit 0 AND the log's 'SMOKE: PASSED' line - an exit alone proves nothing (s656).
# 3. The icon: the exe's own icon, extracted to <Out>\..\Incumbent-dev_icon.png for reading.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$logs = Join-Path (Split-Path -Parent $root) 'PoliSim-captures\logs'
$exe = Join-Path $Out 'Incumbent.exe'

if (-not $SmokeOnly) {
  & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'Tools\unity_run.ps1') -Method PoliSim.EditorTools.PlayerBuild.BuildWindows -Label player_build -Extra "-buildout=$Out" -TimeoutSec 3600
  $code = $LASTEXITCODE
  Select-String -Path (Join-Path $logs 'player_build.log') -Pattern '^BUILD:|error CS' -CaseSensitive | ForEach-Object { $_.Line }
  if ($code -ne 0 -or -not (Test-Path $exe)) { "BUILD PLAYER: FAILED - Unity exit $code, exe present: $(Test-Path $exe)"; exit 1 }
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
'BUILD PLAYER: PASSED'
exit 0
