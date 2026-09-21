param([string]$Path = '', [string]$Baseline = '', [string]$Digest = '', [string]$Record = '', [string]$Evidence = '', [string]$Note = '', [string]$Date = '', [string]$AtCommit = '', [switch]$Grandfather, [switch]$Print, [string]$Root = '')
# ONE ROW FOR THE REVIEW LEDGER (2026-09-21; COMPLETED.md s546; read by Assets/Editor/ReviewLedgerCheck.cs and by Tools/bar_tier.ps1).
#   powershell -NoProfile -File Tools/review_row.ps1 -Path Assets/Scripts/Simulation/EnergyLedger.cs -Record s545 -Evidence Reviews/2026-09-21_ft10_pa.md
#   powershell -NoProfile -File Tools/review_row.ps1 -Baseline 777 -Digest <sha256> -Record s545 -Evidence Reviews/2026-09-21_ft10_pa.md
#   -AtCommit X   the file's state as commit X left it (the history pass's rows), not the working tree's
#   -Grandfather  the state as the guard found it, unreviewed (the landing day only: the count is a ratchet and only falls)
#   -Print        print the row and the state's name, write nothing
# A file's state is named by the SHA-256 of its bytes with every carriage return removed, so the working tree (CRLF)
# and the repository's blob (LF) are one state. A row says a review RAN: the evidence file is the reviewer's report,
# committed under Reviews/, and it must name the file it read. There is no waiver row.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.

$ErrorActionPreference = 'Stop'
if (-not $Root) { $Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path) }
Push-Location $Root
try {
  function StateSha([byte[]]$bytes) {
    $latin = [Text.Encoding]::GetEncoding(28591)
    $lf = $latin.GetBytes($latin.GetString($bytes).Replace("`r", ''))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return -join ($sha.ComputeHash($lf) | ForEach-Object { $_.ToString('x2') }) } finally { $sha.Dispose() }
  }
  $ledger = Join-Path $Root 'Tools/review_ledger.tsv'
  if (-not $Date) { $Date = Get-Date -Format 'yyyy-MM-dd' }
  if ($Path) {
    $Path = $Path -replace '\\', '/'
    if ($AtCommit) {
      $tmp = [IO.Path]::GetTempFileName()
      try { cmd /c "git show `"${AtCommit}:$Path`" > `"$tmp`" 2>nul"; if ($LASTEXITCODE -ne 0) { throw "commit $AtCommit holds no $Path" }; $bytes = [IO.File]::ReadAllBytes($tmp) } finally { Remove-Item -Force $tmp -ErrorAction SilentlyContinue }
    }
    else { $bytes = [IO.File]::ReadAllBytes((Join-Path $Root $Path)) }
    $kind = 'file'; $key = $Path; $sha = StateSha $bytes
  }
  elseif ($Baseline -and $Digest) { $kind = 'baseline'; $key = $Baseline; $sha = $Digest.ToLowerInvariant(); if ($sha.Length -ne 64) { throw 'a digest is 64 hex characters' } }
  else { throw 'give -Path, or -Baseline with -Digest' }

  if ($Grandfather) { if ($kind -ne 'file') { throw 'only a file state can be grandfathered' }; $status = 'grandfathered'; $Evidence = '-'; if (-not $Record) { $Record = '-' } }
  else {
    $status = 'reviewed'
    if (-not $Record) { throw 'a reviewed row names its record (-Record)' }
    if (-not $Evidence -or -not (Test-Path (Join-Path $Root $Evidence))) { throw "the evidence file '$Evidence' does not exist: the report is committed beside the row" }
  }
  if (-not $Note) { $Note = '-' }
  $row = @($kind, $key, $sha, $status, $Date, $Record, ($Evidence -replace '\\', '/'), $Note) -join "`t"
  if ($Print) { $row; return }
  if (Test-Path $ledger) {
    foreach ($line in [IO.File]::ReadAllLines($ledger)) { $c = $line.Split("`t"); if ($c.Length -ge 3 -and $c[0] -eq $kind -and $c[1] -eq $key -and $c[2] -eq $sha) { "ALREADY ON THE LEDGER: $kind $key at $($sha.Substring(0, 8))"; return } }
  }
  else { throw "no ledger at $ledger" }
  [IO.File]::AppendAllText($ledger, $row + "`r`n", (New-Object Text.UTF8Encoding($false)))
  "ROW ADDED: $kind $key at $($sha.Substring(0, 8)) - $status, $Record"
}
finally { Pop-Location }
