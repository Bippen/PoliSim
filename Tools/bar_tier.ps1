param([switch]$Staged, [string]$Commit = '', [string]$Root = '')
# THE BAR TIERS, READ OFF WHAT A COMMIT TOUCHES (2026-09-17; POLISIM_FEATURE_LIST.md, the working discipline's rule 1; COMPLETED.md s524).
#   powershell -NoProfile -File Tools/bar_tier.ps1            the working tree against HEAD, untracked files included
#   powershell -NoProfile -File Tools/bar_tier.ps1 -Staged    the index against HEAD (what the next commit carries)
#   powershell -NoProfile -File Tools/bar_tier.ps1 -Commit X  what commit X changed
# It prints every tier the paths touch, the paths that decided each, the runs owed per item (the union of the tiers
# touched) and at the track's close, and whether the adversarial review runs. It decides nothing by itself: the
# record states what the commit was barred at.
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
  $money = 'Fiscal|Budget|Tax|Spending|Debt|Ledger|SovereignWealthFund|Welfare|Pension|EnergyMarket|EnergyPassThrough|AiFinanceMinistry|TradeCosts|Transfer|CarbonRate|ProgramBill|PortfolioEffectiveness|EconomyState'

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
    $perItem += "the dry film of the touched screens (UiScreenshotCapture.RunDry under -batchmode, every width and country the item names, iterated until its guards are silent) + ONE filmed width at the item's end"
    $atClose += 'the four-width film matrix'
  }
  if ($touched -contains 'SIMULATION' -and -not ($touched -contains 'UI')) { $atClose += 'the four-width film matrix' }
  "  per item : $($perItem -join ' + ')"
  "  at close : $(if ($atClose.Count) { $atClose -join '; ' } else { '-' })"
  if ($touched -contains 'SIMULATION') {
    if ($moneyHits.Count) { "  review   : REQUIRED - money paths: $($moneyHits -join ', ')" }
    else { '  review   : REQUIRED if the sentinel moves (a BASELINE family) or the change books money; otherwise skipped - the record says which' }
  }
  else { "  review   : skipped ($(($touched | ForEach-Object { $_.ToLowerInvariant() }) -join ', '))" }
}
finally { Pop-Location }
