param([int]$Commits = 12, [int]$Sections = 6)
# THE SESSION BRIEF (2026-09-17; COMPLETED.md s525; the reading order at the head of CLAUDE.md).
#   powershell -NoProfile -File Tools/session_brief.ps1
# Generated at the moment it is read, never stored: the tree and its upstream, the last commits, Unity, the last bars
# with their exits, the newest record sections, the open residue rows the last bar printed, the errands still open,
# the trajectory sentinel's baseline, the newest memory file. Every figure is read from where it lives; a source that
# cannot be read is named, never guessed. It decides nothing: it is where a session starts reading, not what it reads.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.

$ErrorActionPreference = 'Continue'
# The records carry non-ASCII (section signs, dashes, multiplication signs): print UTF-8, or a pipe reads '?' for them.
$previousEncoding = $null
try { $previousEncoding = [Console]::OutputEncoding; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$invariant = [System.Globalization.CultureInfo]::InvariantCulture
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$captures = Join-Path (Split-Path -Parent $root) 'PoliSim-captures'
Push-Location $root
function Head([string]$t) { ''; "== $t" }
function Clip([string]$s, [int]$n) { if ($null -eq $s) { return '' }; $s = $s -replace '\s+', ' '; if ($s.Length -gt $n) { $s.Substring(0, $n) + '...' } else { $s } }
try {
  "SESSION BRIEF - PoliSim - generated $(Get-Date -Format s) from the repo as it stands"

  Head 'the tree'
  & git fetch origin --quiet 2>$null
  $branch = (& git rev-parse --abbrev-ref HEAD).Trim()
  $counts = (& git rev-list --left-right --count "origin/$branch...HEAD" 2>$null)
  if ($counts) { $parts = $counts -split '\s+'; "  $branch - $($parts[1]) ahead of origin, $($parts[0]) behind" } else { "  $branch - no upstream read" }
  $dirty = @(& git status --porcelain | Where-Object { $_ -notmatch 'PoliSim\.slnx$' })
  if ($dirty.Count) { "  $($dirty.Count) uncommitted path(s) besides PoliSim.slnx:"; $dirty | Select-Object -First 12 | ForEach-Object { "    $_" } } else { '  clean (PoliSim.slnx aside - Unity rewrites it)' }
  $unity = @(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue)
  if ($unity.Count) { "  UNITY IS RUNNING ($($unity.Count) process(es)) - a batch run is refused while it holds the project" } else { '  no Unity process' }

  Head "the last $Commits commits"
  & git log --format='%h %ad %s' --date=format:'%m-%d %H:%M' -$Commits | ForEach-Object { '  ' + (Clip $_ 150) }

  Head 'the last bars (Logs/bar_timing.tsv)'
  $timing = Join-Path $root 'Logs/bar_timing.tsv'
  if (Test-Path $timing) {
    Get-Content $timing -Tail 400 | Where-Object { $_ -match "`tTOTAL`t" } | Select-Object -Last 8 | ForEach-Object {
      $f = $_ -split "`t"; [string]::Format($invariant, "  {0}  {1,-10} exit {2}  {3:F1} s", $f[0], $f[1], $f[4], ([double]$f[3] / 1000))
    }
  } else { '  Logs/bar_timing.tsv is absent - no bar has run on this clone' }

  Head "the newest $Sections record sections (COMPLETED.md - read by section, never whole)"
  $headings = @(Select-String -Path (Join-Path $root 'COMPLETED.md') -Pattern '^## [0-9]+\. ' -Encoding UTF8 | Select-Object -Last $Sections)
  foreach ($h in $headings) { '  line {0,6}: {1}' -f $h.LineNumber, (Clip $h.Line 150) }

  Head 'the residue the last bar printed (ResidueCheck)'
  $logs = Join-Path $captures 'logs'
  $barLog = $null
  if (Test-Path $logs) {
    $barLog = Get-ChildItem $logs -Filter '*.log' | Where-Object { $_.Name -match '^(bar|docbar)' -and $_.Length -lt 50MB } | Sort-Object LastWriteTime -Descending |
      Where-Object { Select-String -Path $_.FullName -Pattern 'THE RESIDUE:' -SimpleMatch -Quiet } | Select-Object -First 1
  }
  if ($barLog) {
    "  from $($barLog.Name) ($($barLog.LastWriteTime.ToString('s')))"
    $lines = Get-Content $barLog.FullName -Encoding UTF8
    $i = ($lines | Select-String -Pattern 'THE RESIDUE:' -SimpleMatch | Select-Object -Last 1).LineNumber
    if ($i) { $lines[($i - 1)..([Math]::Min($lines.Count - 1, $i + 14))] | Where-Object { $_ -match '\S' -and $_ -notmatch '^(UnityEngine|PoliSim)\.|^\(Filename' } | ForEach-Object { '  ' + (Clip $_ 150) } }
  } else { "  no bar log carrying the residue under $logs" }

  Head 'the errands still open (the live table of ERRANDS.md: rows with no done mark anywhere and no closing word in their opening words)'
  $mark = [string][char]0x2705
  $errands = @(Get-Content (Join-Path $root 'ERRANDS.md') -Encoding UTF8)
  $end = [Array]::FindIndex([string[]]$errands, [Predicate[string]]{ param($l) $l -match '^## ' })
  if ($end -lt 0) { $end = $errands.Count }
  $open = @($errands[0..($end - 1)] | Where-Object { $_ -match '^\| \*\*E-[0-9]+\*\*' } | ForEach-Object { [pscustomobject]@{ Line = $_ } } | Where-Object { $head = $_.Line.Substring(0, [Math]::Min(140, $_.Line.Length)); -not ($_.Line.Contains($mark) -or $head -match 'DONE|WITHDRAWN|RULED|SENT|ANSWERED') })
  if ($open.Count) { $open | ForEach-Object { '  ' + (Clip $_.Line 150) } } else { '  none read as open' }

  Head 'the trajectory sentinel'
  $sentinel = Join-Path $root 'Assets/Editor/TrajectorySentinelCheck.cs'
  $label = Select-String -Path $sentinel -Pattern 'public const string BaselineLabel = "([^"]+)"' -Encoding UTF8 | Select-Object -First 1
  if ($label) { "  baseline '$($label.Matches[0].Groups[1].Value)' - a simulation change that moves the no-policy path sets it and its digests in the same commit" } else { '  BaselineLabel not found in TrajectorySentinelCheck.cs' }

  Head 'the newest memory file'
  $memory = Join-Path $env:USERPROFILE '.claude\projects\C--Users-elias\memory'
  if (Test-Path $memory) {
    Get-ChildItem $memory -Filter 'polisim-*.md' | Sort-Object LastWriteTime -Descending | Select-Object -First 2 | ForEach-Object { "  $($_.Name) ($($_.LastWriteTime.ToString('s')))" }
  } else { "  no memory directory at $memory" }

  ''
  'THEN: the reading order at the head of CLAUDE.md; before every commit, Tools/bar_tier.ps1.'
}
finally {
  Pop-Location
  if ($null -ne $previousEncoding) { try { [Console]::OutputEncoding = $previousEncoding } catch { } }
}
