param([string]$Label = '', [string]$Sessions = '', [string]$Record = '', [string]$Why = '', [string]$Date = '', [switch]$Print, [string]$Root = '')
# ONE ROW FOR THE DRY-FILM SCOPE LEDGER (2026-09-22; COMPLETED.md s574; read by Assets/Editor/DryFilmScopeCheck.cs and by Tools/bar_tier.ps1).
#   powershell -NoProfile -File Tools/film_scope.ps1 -Label dryt6 -Sessions "Sweden@1280x720,Germany@2560x1440" -Record s575 -Why "the two the item moves"
#   -Print   print the row, write nothing
# A UI item NAMES the countries and widths its dry film runs, before it runs one. The check then reads the film's own plan line and
# fails the bar if the film ran a session the row does not name - the cost of a twelve-session sweep becomes a decision, not a habit.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.

$ErrorActionPreference = 'Stop'
if (-not $Root) { $Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path) }
Push-Location $Root
try {
  if (-not $Label) { throw 'a row names the dry film it covers (-Label)' }
  if (-not $Sessions) { throw 'a row names the sessions the item asks for (-Sessions "Country@WxH,...")' }
  if (-not $Record) { throw 'a row names its record (-Record)' }
  if (-not $Why) { throw 'a row says WHY these and no others (-Why)' }
  if (-not $Date) { $Date = Get-Date -Format 'yyyy-MM-dd' }

  $clean = @()
  foreach ($s in $Sessions.Split(',')) {
    $t = $s.Trim()
    if (-not $t) { continue }
    if ($t -notmatch '^[A-Za-z]+@\d{3,4}x\d{3,4}$') { throw "session '$t' is not Country@WidthxHeight (e.g. Sweden@1280x720)" }
    $clean += $t
  }
  if (-not $clean.Count) { throw 'no sessions given' }

  $ledger = Join-Path $Root 'Tools/film_scope.tsv'
  if (-not (Test-Path $ledger)) { throw "no scope ledger at $ledger" }
  $row = @('scope', $Label, ($clean -join ','), $Date, $Record, $Why) -join "`t"
  if ($Print) { $row; return }
  foreach ($line in [IO.File]::ReadAllLines($ledger)) {
    $c = $line.Split("`t")
    if ($c.Length -ge 2 -and $c[0] -eq 'scope' -and $c[1] -eq $Label) { "ROW EXISTS: $Label is already declared - a label is filmed once"; return }
  }

  [IO.File]::AppendAllText($ledger, $row + "`r`n", (New-Object Text.UTF8Encoding($false)))
  "ROW ADDED: $Label runs $($clean.Count) session(s) - $($clean -join ', ') - $Record"
}
finally { Pop-Location }
