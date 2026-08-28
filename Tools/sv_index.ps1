<#
.SYNOPSIS
  The section-V contact index (R-D5, the clear-out kickoff of 2026-08-28): one HTML section per row of
  MISSING_PREREQUISITES.md's section V review checklist, linking that row's captures at every size, in
  checklist order. Tooling in-tree, output OUT of tree beside the captures; nothing binary is committed.

.DESCRIPTION
  Reads the "| surface | built | the capture | what to look for |" table under "# V." and, for each row,
  expands every backticked capture token in the third cell against the capture folder: a token with a
  "*" is a glob (one row of links per matching file, sorted), a token without one is a single file, and
  "(+`_rows`, `_deep`)" style suffix hints are expanded by trying the base pattern with each suffix.
  Missing files are listed as such - the index says what it could not find rather than dropping it.

.PARAMETER PrereqsPath   MISSING_PREREQUISITES.md (default: the repo root's).
.PARAMETER CapturesDir   the out-of-tree capture folder (default: ..\PoliSim-captures beside the repo).
.PARAMETER OutPath       the HTML to write (default: <CapturesDir>\sv_index.html).

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
$inV = $false; $inTable = $false; $rows = @()
foreach ($line in $lines) {
    if ($line -match '^# V\.') { $inV = $true; continue }
    if ($inV -and $line -match '^# ') { break }
    if (-not $inV) { continue }
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
    # screen number ("_06i_…", "_90_…") continues the previous token's PREFIX up to its "*" (the size
    # slot); "_rows" / "_deep" are the scrolled variants of the previous token (replacing its "_rows" if
    # it has one, appended otherwise, skipped when it already ends in "*"); an ellipsis "…" stands for
    # the previous token's screen tail after the country-size slot.
    $tokens = @(); $bases = @(); $m = [regex]::Matches($captureCell, '`([^`]+)`')
    $base = $null
    foreach ($x in $m) {
        $tok = $x.Groups[1].Value
        if ($tok -match '^_(rows|deep)$' -and $base) {
            # the scrolled variant of every base named so far in the row ("a, b, c (+_deep)")
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
        if ($tok.Contains('…') -and $base) {
            # the previous token's screen tail: from its first segment that reads as a screen id ("05b")
            $segs = $base -split '_'
            $start = -1
            for ($k = 0; $k -lt $segs.Count; $k++) { if ($segs[$k] -match '^\d+[a-z]?$') { $start = $k; break } }
            if ($start -ge 0) { $tok = $tok.Replace('…', (($segs[$start..($segs.Count - 1)]) -join '_')) }
        }
        $tokens += $tok; $bases += $tok; $base = $tok
    }
    return @($tokens | Select-Object -Unique)
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<!doctype html><html><head><meta charset="utf-8"><title>PoliSim - section V contact index</title>')
[void]$sb.AppendLine('<style>body{font:15px/1.45 Georgia,serif;margin:2rem;max-width:1100px;color:#2b2620;background:#f4efe4}h1{font-size:1.6rem}h2{font-size:1.15rem;margin-top:2.2rem;border-top:1px solid #c9ba9b;padding-top:1rem}.meta{color:#5d564a;font-size:.92rem}.look{margin:.4rem 0 .8rem}.files{margin:0;padding-left:1.2rem}.files li{margin:.15rem 0}.missing{color:#9c4238}code{font-family:Consolas,monospace;font-size:.9em}img.preview{max-width:100%;border:1px solid #c9ba9b;margin:.4rem 0}details summary{cursor:pointer;color:#5d564a}</style></head><body>')
[void]$sb.AppendLine('<h1>PoliSim &mdash; section V, the review checklist on film</h1>')
[void]$sb.AppendLine(('<p class="meta">Generated {0} from <code>{1}</code>; {2} rows; captures under <code>{3}</code>. Sit through it top to bottom: each section is one checklist row, its captures at every size linked, the 1600 one previewed.</p>' -f (Get-Date -Format 'yyyy-MM-dd HH:mm'), (Html (Split-Path -Leaf $PrereqsPath)), $rows.Count, (Html $CapturesDir)))
$n = 0
foreach ($row in $rows) {
    $n++
    [void]$sb.AppendLine(('<h2>{0}. {1}</h2>' -f $n, (Html (StripMd $row.Surface))))
    [void]$sb.AppendLine(('<div class="meta">built: {0}</div>' -f (Html (StripMd $row.Built))))
    [void]$sb.AppendLine(('<div class="look">{0}</div>' -f (Html (StripMd $row.Look))))
    $tokens = ExpandTokens $row.Capture
    $preview = $null
    [void]$sb.AppendLine('<ul class="files">')
    foreach ($tok in $tokens) {
        $pattern = $tok
        if ($pattern.EndsWith(']')) { $pattern = $pattern + '*' }   # a bracket class names a screen family
        if (-not $pattern.EndsWith('.png')) { $pattern = $pattern + '.png' }
        # -Path (not -Filter): PowerShell's own wildcards, so "[2-7]" character classes resolve
        $files = @(Get-ChildItem -Path (Join-Path $CapturesDir $pattern) -File -ErrorAction SilentlyContinue | Sort-Object Name)
        if ($files.Count -eq 0) { [void]$sb.AppendLine(('<li class="missing"><code>{0}</code> &mdash; no file matches</li>' -f (Html $tok))); continue }
        [void]$sb.AppendLine(('<li><code>{0}</code> &mdash; {1} file(s):' -f (Html $tok), $files.Count))
        [void]$sb.Append('<ul class="files">')
        foreach ($f in $files) { [void]$sb.Append(('<li><a href="{0}">{0}</a></li>' -f (Html $f.Name))); if (-not $preview -and $f.Name -match '1600') { $preview = $f.Name } }
        if (-not $preview -and $files.Count -gt 0) { $preview = $files[0].Name }
        [void]$sb.AppendLine('</ul></li>')
    }
    [void]$sb.AppendLine('</ul>')
    if ($preview) { [void]$sb.AppendLine(('<details open><summary>preview: {0}</summary><a href="{0}"><img class="preview" loading="lazy" src="{0}" alt="{0}"></a></details>' -f (Html $preview))) }
}
[void]$sb.AppendLine('</body></html>')
[IO.File]::WriteAllText($OutPath, $sb.ToString(), (New-Object Text.UTF8Encoding($false)))
"sv_index: {0} rows -> {1}" -f $rows.Count, $OutPath
