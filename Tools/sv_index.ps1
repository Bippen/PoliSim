<#
.SYNOPSIS
  The section-V sitting package (R-D5, 2026-08-28; rebuilt 2026-09-01). One HTML page for Elias's
  visual review: the Canvas-capture AUDIT first, then the checklist grouped by era and by screen,
  every row carrying THE ONE QUESTION it exists to answer. Tooling in-tree, output OUT of tree
  beside the captures; nothing binary is committed.

.DESCRIPTION
  Reads MISSING_PREREQUISITES.md's section V. Two tables come out of it:

    1. THE AUDIT - the "| surface | verdict |" table inside the S-20 blockquote at the top of the
       section. It opens the page, and a VOID verdict sorts FIRST, because a row whose evidence was
       void is the one thing a sitting must not scroll past. The re-films are listed beside it.

    2. THE CHECKLIST - the "| surface | built | the capture | what to look for |" table. Each row's
       backticked capture tokens are expanded against the capture folder (a "*" is a glob, a bare
       token a single file, "_rows"/"_deep" the scrolled variants of the tokens before it, and an
       ellipsis the previous token's screen tail). Missing files are LISTED AS MISSING - the index
       says what it could not find rather than dropping it.

  Three things this rebuild adds, each because a sitting of ~50 rows in flat checklist order costs
  the reader a decision on every one of them:

    - ERA, derived from the capture token's own prefix, never authored. omni/cont/clear are the
      omnibus pass; v3a/v3desk/sitting are UI v3.0; v31/v3c/sp4/pa_sweep are UI v3.1; we/wf/
      pa_campaign are the elections era. A row whose prefix matches none of those is filed as
      UNFILED and says so, rather than being guessed into a group.

    - SCREEN, from the token's own screen-id segment ("..._07b_politics_compass" -> "07b politics
      compass"), so the rows for one screen sit together across sizes and countries.

    - THE QUESTION. The checklist's fourth cell IS the question, and it is promoted out of the body
      text to the top of the row. WARNING: where that cell does not read as a question - no "?",
      no interrogative opening - the row is FLAGGED rather than given one. Writing forty questions
      here would be authoring the review; naming the rows that lack one is reporting it.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File Tools\sv_index.ps1
#>
param(
  [string]$PrereqsPath = "",
  [string]$CapturesDir = "",
  [string]$OutPath = ""
)
$repo = Split-Path -Parent $PSScriptRoot
if ($PrereqsPath -eq "") { $PrereqsPath = Join-Path $repo "MISSING_PREREQUISITES.md" }
if ($CapturesDir -eq "") { $CapturesDir = Join-Path (Split-Path -Parent $repo) "PoliSim-captures" }
if ($OutPath -eq "") { $OutPath = Join-Path $CapturesDir "sv_index.html" }

$lines = [IO.File]::ReadAllLines($PrereqsPath, [Text.Encoding]::UTF8)
$inV = $false; $inTable = $false; $rows = @(); $audit = @()
foreach ($line in $lines) {
    if ($line -match '^# V\.') { $inV = $true; continue }
    if ($inV -and $line -match '^# ') { break }
    if (-not $inV) { continue }

    # The audit table lives inside the blockquote: "> | surface | verdict |".
    if ($line -match '^>\s*\|') {
        $cells = ($line -replace '^>\s*', '').Trim().TrimStart('|').TrimEnd('|') -split ' \| '
        if ($cells.Count -eq 2 -and $cells[0].Trim() -ne 'surface' -and $cells[0].Trim() -notmatch '^-+$') {
            $audit += ,@{ Surface = $cells[0].Trim(); Verdict = $cells[1].Trim() }
        }
        continue
    }

    if ($line -match '^\| surface \| built \| the capture \|') { $inTable = $true; continue }
    if ($inTable -and $line -match '^\|---') { continue }
    if ($inTable -and $line -match '^\|') {
        $cells = $line.Trim().TrimStart('|').TrimEnd('|') -split ' \| '
        if ($cells.Count -ge 4) { $rows += ,@{ Surface = $cells[0].Trim(); Built = $cells[1].Trim(); Capture = $cells[2].Trim(); Look = $cells[3].Trim() } }
    } elseif ($inTable -and $line.Trim() -eq '') { $inTable = $false }
}

function Html($s) { return [System.Net.WebUtility]::HtmlEncode($s) }
function StripMd($s) { return ($s -replace '\*\*', '' -replace '`', '') }

function ExpandTokens($captureCell) {
    # every backticked token, read with the checklist's own shorthand: a token starting with "_" and a
    # screen number ("_06i_...", "_90_...") continues the previous token's PREFIX up to its "*" (the size
    # slot); "_rows" / "_deep" are the scrolled variants of the previous token (replacing its "_rows" if
    # it has one, appended otherwise, skipped when it already ends in "*"); an ellipsis stands for
    # the previous token's screen tail after the country-size slot.
    $tokens = @(); $bases = @(); $m = [regex]::Matches($captureCell, '`([^`]+)`')
    $base = $null
    foreach ($x in $m) {
        $tok = $x.Groups[1].Value
        if ($tok -match '^_(rows|deep)$' -and $base) {
            foreach ($b in $bases) {
                if ($b.EndsWith('*')) { continue }
                if ($b -match '_rows$') { $tokens += ($b -replace '_rows$', $tok) } else { $tokens += $b + $tok }
            }
            continue
        }
        if ($tok -match '^_\d' -and $base) {
            $i = $base.IndexOf('*')
            $prefix = if ($i -ge 0) { $base.Substring(0, $i + 1) } else { $base }
            $tokens += $prefix + $tok; continue
        }
        if ($tok.Contains([char]0x2026) -and $base) {
            $segs = $base -split '_'
            $start = -1
            for ($k = 0; $k -lt $segs.Count; $k++) { if ($segs[$k] -match '^\d+[a-z]?$') { $start = $k; break } }
            if ($start -ge 0) { $tok = $tok.Replace([string][char]0x2026, (($segs[$start..($segs.Count - 1)]) -join '_')) }
        }
        $tokens += $tok; $bases += $tok; $base = $tok
    }
    return @($tokens | Select-Object -Unique)
}

# ERA from the capture token's own prefix. Nothing here is authored about a row: the prefix is the
# label the film run itself was given, so the grouping is the repo's and not this script's.
function EraOf($tok) {
    if (-not $tok) { return @{ Key = 'z-unfiled'; Name = 'UNFILED - no capture token names a film run' } }
    switch -regex ($tok) {
        '^(omni|cont|clear)'            { return @{ Key = 'a-omnibus'; Name = 'The omnibus pass and its continuation (2026-08-27/28)' } }
        '^(v3a|v3desk|sitting)'         { return @{ Key = 'b-v30';     Name = 'UI v3.0 - Screen 0, the rail, and the first sitting' } }
        '^(v31|v3c|sp4|pa_sweep)'       { return @{ Key = 'c-v31';     Name = 'UI v3.1 - density, contrast, instruments' } }
        '^(we\d|wf\d|pa_campaign|cd5b)' { return @{ Key = 'd-elect';   Name = 'The elections era - campaign, debate, election night' } }
        default                         { return @{ Key = 'z-unfiled'; Name = 'UNFILED - the prefix matches no known film run' } }
    }
}

# SCREEN from the token's own screen-id segment, so one screen's rows sit together.
function ScreenOf($tok) {
    if (-not $tok) { return '(no capture)' }
    $segs = $tok -split '_'
    for ($k = 0; $k -lt $segs.Count; $k++) {
        if ($segs[$k] -match '^\d+[a-z]?$') { return (($segs[$k..([Math]::Min($k + 2, $segs.Count - 1))]) -join ' ') }
    }
    return '(no screen id)'
}

# Does the fourth cell READ as a question? A "?" or an interrogative opening. Anything else is
# FLAGGED, never rewritten - the row is asking the reader to look without saying what for, and that
# is the finding rather than an invitation to invent one.
function IsQuestion($s) {
    if ($s -match '\?') { return $true }
    return ($s -match '^\s*(\*\*)?(Does|Do|Is|Are|Which|Whether|Can|Should|Has|Have|Will|What|Two questions)\b')
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<!doctype html><html><head><meta charset="utf-8"><title>PoliSim - the section V sitting</title>')
[void]$sb.AppendLine('<style>body{font:15px/1.45 Georgia,serif;margin:2rem;max-width:1100px;color:#2b2620;background:#f4efe4}h1{font-size:1.7rem}h2{font-size:1.3rem;margin-top:2.6rem;border-top:2px solid #8a7a5c;padding-top:.8rem}h3{font-size:1.05rem;margin-top:1.8rem;color:#5d564a}h4{font-size:1rem;margin:1.6rem 0 .2rem}.meta{color:#5d564a;font-size:.92rem}.q{margin:.5rem 0 .7rem;padding:.5rem .7rem;border-left:4px solid #8a7a5c;background:#efe7d6}.q b{letter-spacing:.06em;font-size:.78rem;color:#5d564a;display:block}.noq{border-left-color:#9c4238;background:#f3e3df}.files{margin:0;padding-left:1.2rem}.files li{margin:.15rem 0}.missing{color:#9c4238}code{font-family:Consolas,monospace;font-size:.9em}img.preview{max-width:100%;border:1px solid #c9ba9b;margin:.4rem 0}details summary{cursor:pointer;color:#5d564a}table.audit{border-collapse:collapse;margin:.6rem 0 1.4rem}table.audit td,table.audit th{border:1px solid #c9ba9b;padding:.35rem .6rem;text-align:left;vertical-align:top}tr.void td{background:#f3e3df}</style></head><body>')
[void]$sb.AppendLine('<h1>PoliSim &mdash; the section V sitting</h1>')
[void]$sb.AppendLine(('<p class="meta">Generated {0} from <code>{1}</code>; {2} checklist rows, {3} audit rows; captures under <code>{4}</code>.</p>' -f (Get-Date -Format 'yyyy-MM-dd HH:mm'), (Html (Split-Path -Leaf $PrereqsPath)), $rows.Count, $audit.Count, (Html $CapturesDir)))

# --- 1. THE AUDIT, first, void rows at the top -------------------------------------------------
[void]$sb.AppendLine('<h2>1. The capture audit &mdash; read this before anything below</h2>')
if ($audit.Count -eq 0) {
    [void]$sb.AppendLine('<p class="missing">No audit table was found in section V. That is a change to the source document, not an empty audit &mdash; this page will not invent one.</p>')
} else {
    [void]$sb.AppendLine('<p class="meta">S-20: every guard was green while a board filmed the wrong screen. A VOID row is evidence that did not exist; its re-films are named beside it.</p>')
    [void]$sb.AppendLine('<table class="audit"><tr><th>surface</th><th>verdict</th></tr>')
    foreach ($a in ($audit | Sort-Object @{ Expression = { if ($_.Verdict -match 'VOID') { 0 } else { 1 } } })) {
        $cls = if ($a.Verdict -match 'VOID') { ' class="void"' } else { '' }
        [void]$sb.AppendLine(('<tr{0}><td>{1}</td><td>{2}</td></tr>' -f $cls, (Html (StripMd $a.Surface)), (Html (StripMd $a.Verdict))))
    }
    [void]$sb.AppendLine('</table>')
    $refilms = @(Get-ChildItem -Path (Join-Path $CapturesDir '*e6_election_night*.png') -File -ErrorAction SilentlyContinue | Sort-Object Name)
    [void]$sb.AppendLine(('<p class="meta">The re-films on disk for the void row: {0} file(s).</p>' -f $refilms.Count))
    if ($refilms.Count -gt 0) {
        [void]$sb.Append('<ul class="files">')
        foreach ($f in $refilms) { [void]$sb.Append(('<li><a href="{0}">{0}</a></li>' -f (Html $f.Name))) }
        [void]$sb.AppendLine('</ul>')
    }
}

# --- 2. THE CHECKLIST, by era then by screen ---------------------------------------------------
$decorated = @()
foreach ($row in $rows) {
    # PowerShell UNROLLS a one-element array on return, so a row that expands to a single capture
    # token arrives here as a STRING - and $tokens[0] is then its first CHARACTER, "o" for
    # "omni_final_...". That misfiled 17 rows as UNFILED on the first run while the page rendered
    # perfectly, which is precisely the class this project keeps finding: evidence that looks fine
    # whatever the truth is. @() forces the array back.
    $tokens = @(ExpandTokens $row.Capture)
    $first = if ($tokens.Count -gt 0) { $tokens[0] } else { $null }
    $era = EraOf $first
    $decorated += ,@{ Row = $row; Tokens = $tokens; EraKey = $era.Key; EraName = $era.Name; Screen = (ScreenOf $first) }
}

$missingQuestion = 0
[void]$sb.AppendLine('<h2>2. The checklist, by era and by screen</h2>')
foreach ($eraKey in ($decorated | ForEach-Object { $_.EraKey } | Sort-Object -Unique)) {
    $inEra = @($decorated | Where-Object { $_.EraKey -eq $eraKey })
    [void]$sb.AppendLine(('<h3>{0} &mdash; {1} row(s)</h3>' -f (Html $inEra[0].EraName), $inEra.Count))

    foreach ($screen in ($inEra | ForEach-Object { $_.Screen } | Sort-Object -Unique)) {
        $inScreen = @($inEra | Where-Object { $_.Screen -eq $screen })
        [void]$sb.AppendLine(('<h4>{0}</h4>' -f (Html $screen)))

        foreach ($d in $inScreen) {
            $row = $d.Row
            [void]$sb.AppendLine(('<div class="meta"><b>{0}</b> &mdash; built {1}</div>' -f (Html (StripMd $row.Surface)), (Html (StripMd $row.Built))))
            if (IsQuestion $row.Look) {
                [void]$sb.AppendLine(('<div class="q"><b>THE QUESTION</b>{0}</div>' -f (Html (StripMd $row.Look))))
            } else {
                $missingQuestion++
                [void]$sb.AppendLine(('<div class="q noq"><b>NO QUESTION STATED &mdash; this row says what to look at, not what it is asking</b>{0}</div>' -f (Html (StripMd $row.Look))))
            }

            $preview = $null
            [void]$sb.AppendLine('<ul class="files">')
            foreach ($tok in @($d.Tokens)) {
                $pattern = $tok
                if ($pattern.EndsWith(']')) { $pattern = $pattern + '*' }
                if (-not $pattern.EndsWith('.png')) { $pattern = $pattern + '.png' }
                $files = @(Get-ChildItem -Path (Join-Path $CapturesDir $pattern) -File -ErrorAction SilentlyContinue | Sort-Object Name)
                if ($files.Count -eq 0) { [void]$sb.AppendLine(('<li class="missing"><code>{0}</code> &mdash; no file matches</li>' -f (Html $tok))); continue }
                [void]$sb.AppendLine(('<li><code>{0}</code> &mdash; {1} file(s):' -f (Html $tok), $files.Count))
                [void]$sb.Append('<ul class="files">')
                foreach ($f in $files) { [void]$sb.Append(('<li><a href="{0}">{0}</a></li>' -f (Html $f.Name))); if (-not $preview -and $f.Name -match '1600') { $preview = $f.Name } }
                if (-not $preview -and $files.Count -gt 0) { $preview = $files[0].Name }
                [void]$sb.AppendLine('</ul></li>')
            }
            [void]$sb.AppendLine('</ul>')
            if ($preview) { [void]$sb.AppendLine(('<details><summary>preview: {0}</summary><a href="{0}"><img class="preview" loading="lazy" src="{0}" alt="{0}"></a></details>' -f (Html $preview))) }
        }
    }
}

[void]$sb.AppendLine(('<h2>3. What this page could not do for you</h2><p class="meta">{0} of {1} rows carry no stated question. They are flagged in red above rather than given one: writing them here would be authoring the review instead of preparing it.</p>' -f $missingQuestion, $rows.Count))
[void]$sb.AppendLine('</body></html>')
[IO.File]::WriteAllText($OutPath, $sb.ToString(), (New-Object Text.UTF8Encoding($false)))
"sv_index: {0} checklist rows, {1} audit rows, {2} without a stated question -> {3}" -f $rows.Count, $audit.Count, $missingQuestion, $OutPath
