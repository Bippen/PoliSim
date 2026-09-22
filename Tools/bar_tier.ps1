param([switch]$Staged, [string]$Commit = '', [string]$Root = '')
# THE BAR TIERS, READ OFF WHAT A COMMIT TOUCHES (2026-09-17; POLISIM_FEATURE_LIST.md, the working discipline's rule 1; COMPLETED.md s524).
#   powershell -NoProfile -File Tools/bar_tier.ps1            the working tree against HEAD, untracked files included
#   powershell -NoProfile -File Tools/bar_tier.ps1 -Staged    the index against HEAD (what the next commit carries)
#   powershell -NoProfile -File Tools/bar_tier.ps1 -Commit X  what commit X changed
# It prints every tier the paths touch, the paths that decided each, the runs owed per item (the union of the tiers
# touched) and at the track's close, and whether the adversarial review runs. It decides nothing by itself: the
# record states what the commit was barred at.
# THE REVIEW IS ENFORCED SINCE 2026-09-21 (COMPLETED.md s546; c544620 touched a money path, this tool said REQUIRED,
# no review ran and a defect shipped): under `review : REQUIRED` it prints, per money path, whether
# Tools/review_ledger.tsv holds a row for the state this commit carries. The gate is the cheap bar's
# ReviewLedgerCheck, which reads `$money` and `$moneyRoots` OUT OF THIS FILE - the two lines below are the
# definition of a money path for both. Add a row with Tools/review_row.ps1.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.

$ErrorActionPreference = 'Stop'
if (-not $Root) { $Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path) }
Push-Location $Root
try {
  if ($Commit) { $paths = @(git diff-tree --no-commit-id --name-only -r $Commit) }
  elseif ($Staged) { $paths = @(git diff --cached --name-only) }
  else { $paths = @(git diff --name-only HEAD) + @(git ls-files --others --exclude-standard) }
  $paths = @($paths | Where-Object { $_ } | ForEach-Object { $_ -replace '\\', '/' } | Sort-Object -Unique)

  # The two registration tables, read off CheckSuite itself. A check in the simulation group ONLY is simulation;
  # one registered in both (the residue, the slack audit) is bookkeeping, and so is the suite's own machinery.
  $suite = [IO.File]::ReadAllText((Join-Path $Root 'Assets/Editor/CheckSuite.cs'))
  function Names([string]$block) { @([regex]::Matches($block, '\("([A-Za-z0-9_]+)",\s*[A-Za-z0-9_.]+\.Run\)') | ForEach-Object { $_.Groups[1].Value }) }
  $cheapChecks = Names ([regex]::Match($suite, 'Suite =\s*\{[\s\S]*?\n\s*\};').Value)
  $simChecks = Names ([regex]::Match($suite, 'Simulation = new \(string Name, Action Run\)\[\][\s\S]*?\n\s*\};').Value)
  $simOnly = @($simChecks | Where-Object { $cheapChecks -notcontains $_ })
  $machinery = @('CheckSuite', 'CheckExit', 'BarTiming', 'RatchetLedger', 'RatchetResidency', 'RatchetSlackCheck', 'ResidueCheck', 'SourceText')
  $simSources = ''
  foreach ($c in $simOnly) { $f = Join-Path $Root "Assets/Editor/$c.cs"; if (Test-Path $f) { $simSources += [IO.File]::ReadAllText($f) } }

  # The money paths: a file whose name says it books, taxes, spends, borrows, transfers or bills.
  $money = 'Fiscal|Budget|Tax|Spending|Debt|Ledger|SovereignWealthFund|Welfare|Pension|EnergyMarket|EnergyPassThrough|AiFinanceMinistry|TradeCosts|Transfer|CarbonRate|ProgramBill|PortfolioEffectiveness|EconomyState|EnergyFleet|EnergyConnectionQueue|AiEnergyMinistry|SimulationManager'   # s546: the fleet decides what is dispatched and so what is billed - s539's defect lived in EnergyFleet.cs. s549 (ruled 2026-09-21): SimulationManager - the fiscal path cannot sit outside a money-path audit because of its name. s551: EnergyConnectionQueue - every refusal in EnergyFleet.Place is decided there (its own review's finding)
  # The roots the ledger reaches: runtime source. Data files, prep scripts and editor diagnostics with a money name stay REQUIRED on the
  # line below and are reached by the ledger's BASELINE pass (a moved sentinel needs a reviewed digest), not by a row of their own.
  $moneyRoots = '^Assets/Scripts/(Simulation|Data|Elections|Persistence)/'

  $ignored = @(); $docs = @(); $ui = @(); $sim = @(); $tooling = @(); $moneyHits = @()
  foreach ($p in $paths) {
    $bare = $p -replace '\.meta$', ''
    $name = [IO.Path]::GetFileNameWithoutExtension($bare)
    if ($p -match '^(Library|Logs|Temp|UserSettings|obj)/' -or $p -match '\.(slnx|sln|csproj)$') { $ignored += $p; continue }
    if ($p -match '\.md$') { $docs += $p; continue }
    $isSim = $false
    if ($p -match '^Assets/Scripts/(Simulation|Data|Elections|Persistence)/' -or $p -match '^(ElectionsData|EnergyData|MacroData)/' -or $p -match '^Tools/.*_prep\.pl$' -or $p -match '^ProjectSettings/') { $isSim = $true }
    elseif ($p -match '^Assets/Editor/') {
      if ($machinery -contains $name) { $tooling += $p; continue }
      if ($simOnly -contains $name -or ($name.Length -gt 3 -and $cheapChecks -notcontains $name -and $simSources -match "\b$([regex]::Escape($name))\b")) { $isSim = $true }
      elseif ($name -match '^(UiScreenshotCapture|ScreenEdgeCheck|FilmDiffCheck)$') { $ui += $p; continue }
      else { $tooling += $p; continue }
    }
    elseif ($p -match '^Assets/(Scripts/UI|Scripts/Testing|Resources|Scenes|Settings)/' -or $p -match '\.(png|ttf|otf|wav|ogg|mp3|mat|shader|uss|uxml|prefab|unity)(\.meta)?$') { $ui += $p; continue }
    else { $tooling += $p; continue }
    if ($isSim) {
      $sim += $p
      if ($name -match $money -and $moneyHits -notcontains $bare) { $moneyHits += $bare }
    }
  }

  $touched = @()
  if ($sim.Count) { $touched += 'SIMULATION' }
  if ($ui.Count) { $touched += 'UI' }
  if ($tooling.Count -and -not $sim.Count -and -not $ui.Count) { $touched += 'TOOLING' }
  if (-not $touched.Count -and $docs.Count) { $touched += 'DOCUMENTS' }
  if (-not $touched.Count) { 'BAR TIER: NOTHING to bar'; return }

  "BAR TIER: $($touched -join ' + ') ($($paths.Count) path(s): $($sim.Count) simulation, $($ui.Count) UI, $($tooling.Count) tooling, $($docs.Count) document(s), $($ignored.Count) ignored)"
  foreach ($group in @(@('simulation', $sim), @('ui', $ui), @('tooling', $tooling), @('documents', $docs))) {
    foreach ($p in ($group[1] | Select-Object -First 12)) { "    $($group[0].PadRight(10)) $p" }
    if ($group[1].Count -gt 12) { "    $($group[0].PadRight(10)) ... and $($group[1].Count - 12) more" }
  }

  if ($touched -contains 'DOCUMENTS') {
    '  per item : CheckSuite.RunDocumentBatch (the checks that read documents; the slack audit waits for the next cheap bar)'
    '  at close : -'
    '  review   : skipped (documents)'
    return
  }

  $perItem = @('CheckSuite.RunAllBatch (the cheap bar)')
  $atClose = @()
  if ($touched -contains 'SIMULATION') {
    $perItem += 'CheckSuite.RunSimulationBatch (the simulation bar, TrajectorySentinelCheck first)'
    $atClose += 'the full trajectory dump with its old-beside-new diffs'
  }
  if ($touched -contains 'UI') {
    $perItem += "the dry film of the SESSIONS THE ITEM NAMES (UiScreenshotCapture.RunDry under -batchmode; declare them with Tools/film_scope.ps1 BEFORE the film and DryFilmScopeCheck holds the film to the row), iterated until its guards are silent) + ONE filmed width at the item's end"
    $atClose += 'the four-width film matrix'
  }
  if ($touched -contains 'SIMULATION' -and -not ($touched -contains 'UI')) { $atClose += 'the four-width film matrix' }
  if ($touched -contains 'UI') {
    $scopeFile = Join-Path $Root 'Tools/film_scope.tsv'
    $declared = @()
    if (Test-Path $scopeFile) {
      foreach ($line in [IO.File]::ReadAllLines($scopeFile)) { $c = $line.Split("`t"); if ($c.Length -ge 6 -and $c[0] -eq 'scope') { $declared += "$($c[1]) -> $($c[2])" } }
    }
    $lastScope = if ($declared.Count) { $declared[-1] } else { 'NONE' }
  }
  "  per item : $($perItem -join ' + ')"
  "  at close : $(if ($atClose.Count) { $atClose -join '; ' } else { '-' })"
  if ($touched -contains 'UI') {
    "  film scope: REQUIRED - declare the sessions with Tools/film_scope.ps1 before the dry film; DryFilmScopeCheck fails the cheap bar on a film that ran a session its row does not name"
    "    last row : $lastScope"
  }
  if ($touched -contains 'SIMULATION') {
    if ($moneyHits.Count) {
      "  review   : REQUIRED - money paths: $($moneyHits -join ', ')"
      $ledgerRows = @(); $ledgerFile = Join-Path $Root 'Tools/review_ledger.tsv'
      if (Test-Path $ledgerFile) { $ledgerRows = @([IO.File]::ReadAllLines($ledgerFile) | Where-Object { $_ -and $_[0] -ne '#' } | ForEach-Object { ,($_.Split("`t")) }) }
      foreach ($m in $moneyHits) {
        if ($m -notmatch $moneyRoots -or $m -notmatch '\.cs$') { "    ledger   : $m - outside the ledger's rows (not runtime source under the money roots); the line above stands, and the baseline pass reaches it"; continue }
        $bytes = $null; $tmp = [IO.Path]::GetTempFileName()
        try {
          if ($Commit) { cmd /c "git show `"${Commit}:$m`" > `"$tmp`" 2>nul" } elseif ($Staged) { cmd /c "git show `":$m`" > `"$tmp`" 2>nul" } else { $LASTEXITCODE = 0; if (Test-Path (Join-Path $Root $m)) { Copy-Item -Force (Join-Path $Root $m) $tmp } else { $LASTEXITCODE = 1 } }
          if ($LASTEXITCODE -eq 0) { $bytes = [IO.File]::ReadAllBytes($tmp) }
        } finally { Remove-Item -Force $tmp -ErrorAction SilentlyContinue }
        if ($null -eq $bytes) { "    ledger   : $m - deleted by this change; nothing left to review"; continue }
        $latin = [Text.Encoding]::GetEncoding(28591); $lf = $latin.GetBytes($latin.GetString($bytes).Replace("`r", ''))
        $hasher = [Security.Cryptography.SHA256]::Create(); try { $state = -join ($hasher.ComputeHash($lf) | ForEach-Object { $_.ToString('x2') }) } finally { $hasher.Dispose() }
        $row = $ledgerRows | Where-Object { $_.Length -ge 8 -and $_[0] -eq 'file' -and $_[1] -eq $m -and $_[2] -eq $state } | Select-Object -First 1
        if ($row) { "    ledger   : $m at $($state.Substring(0, 8)) - $($row[3]), $($row[5]), $($row[6])" }
        else { "    ledger   : $m at $($state.Substring(0, 8)) - NO ROW FOR THIS STATE. ReviewLedgerCheck fails the cheap bar until the review has run, its report is under Reviews/ and Tools/review_row.ps1 has added the row" }
      }
    }
    else { '  review   : REQUIRED if the sentinel moves (a BASELINE family) or the change books money; otherwise skipped - the record says which' }
  }
  else { "  review   : skipped ($(($touched | ForEach-Object { $_.ToLowerInvariant() }) -join ', '))" }
}
finally { Pop-Location }
