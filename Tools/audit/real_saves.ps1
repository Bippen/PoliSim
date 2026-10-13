param([switch]$Aside, [switch]$Back, [string]$Label = 'autosave')
# THE PLAYER'S SAVES FOLDER, MOVED ASIDE FOR AN AUDIT PLAN AND PUT BACK BYTE FOR BYTE (the gameplay audit, docs/audit/AUDIT_PROTOCOL.md;
# Elias's answer of 2026-10-13: "move it aside" for one short autosave plan).
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/audit/real_saves.ps1 -Aside
#   ... run the plan (BatchSimulationRunner.RunAudit refuses an autosaving plan while the folder holds files) ...
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/audit/real_saves.ps1 -Back -Label <plan label>
# -Aside records every file's SHA-256 (PoliSim-captures/logs/audit/real_saves_manifest.tsv) and renames saves -> saves.audit-aside.
# -Back moves whatever the plan wrote (the harness's autosaves) to PoliSim-captures/logs/audit/<Label>_realsaves/ as evidence, renames the
# folder back, and checks every file against the manifest: exit 0 only when every file is back with its digest.
# ASCII only: PowerShell 5.1 reads a BOM-less script as ANSI.
$ErrorActionPreference = 'Stop'
$root = Join-Path $env:USERPROFILE 'AppData\LocalLow\DWELOP Games\Incumbent'
$savesDir = Join-Path $root 'saves'
$asideDir = Join-Path $root 'saves.audit-aside'
$repo = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
$logs = Join-Path (Split-Path -Parent $repo) 'PoliSim-captures\logs\audit'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$manifest = Join-Path $logs 'real_saves_manifest.tsv'

function Digest([string]$path) {
  $sha = [Security.Cryptography.SHA256]::Create()
  $fs = [IO.File]::OpenRead($path)
  try { return ([BitConverter]::ToString($sha.ComputeHash($fs))).Replace('-', '').ToLowerInvariant() } finally { $fs.Dispose(); $sha.Dispose() }
}

if ($Aside) {
  if (Test-Path $asideDir) { "REFUSED: $asideDir exists already - a previous plan was not put back (run -Back first)"; exit 3 }
  if (-not (Test-Path $savesDir)) { "NOTHING TO MOVE: $savesDir does not exist"; [IO.File]::WriteAllText($manifest, ''); exit 0 }
  $lines = New-Object System.Collections.Generic.List[string]
  foreach ($f in [IO.Directory]::GetFiles($savesDir, '*', [IO.SearchOption]::AllDirectories)) {
    $rel = $f.Substring($savesDir.Length + 1)
    $lines.Add($rel + "`t" + (New-Object IO.FileInfo $f).Length + "`t" + (Digest $f))
  }
  [IO.File]::WriteAllLines($manifest, $lines)
  [IO.Directory]::Move($savesDir, $asideDir)
  "ASIDE: $($lines.Count) file(s) recorded in $manifest; $savesDir -> $asideDir"
  exit 0
}

if ($Back) {
  if (-not (Test-Path $asideDir)) { "REFUSED: $asideDir does not exist - nothing to put back"; exit 3 }
  if (Test-Path $savesDir) {
    # The evidence folder is on another volume (Directory.Move cannot cross one): copy each file the plan wrote, check the copy's digest
    # against its source, and only then delete the plan's folder - the player's own folder is untouched beside it as saves.audit-aside.
    $evidence = Join-Path $logs ($Label + '_realsaves')
    if (Test-Path $evidence) { "REFUSED: $evidence exists - choose another -Label"; exit 3 }
    $written = [IO.Directory]::GetFiles($savesDir, '*', [IO.SearchOption]::AllDirectories)
    foreach ($f in $written) {
      $to = Join-Path $evidence $f.Substring($savesDir.Length + 1)
      New-Item -ItemType Directory -Force -Path (Split-Path -Parent $to) | Out-Null
      [IO.File]::Copy($f, $to)
      if ((Digest $f) -ne (Digest $to)) { "REFUSED: the copy of $f differs from its source - nothing deleted, nothing moved back"; exit 4 }
    }
    [IO.Directory]::Delete($savesDir, $true)
    "EVIDENCE: what the plan wrote ($($written.Count) file(s)) is now in $evidence; its folder deleted"
  }
  [IO.Directory]::Move($asideDir, $savesDir)
  $bad = 0; $count = 0
  foreach ($line in [IO.File]::ReadAllLines($manifest)) {
    if ($line.Length -eq 0) { continue }
    $cells = $line.Split("`t")
    $count++
    $f = Join-Path $savesDir $cells[0]
    if (-not (Test-Path $f)) { "MISSING: $($cells[0])"; $bad++; continue }
    if ((Digest $f) -ne $cells[2]) { "CHANGED: $($cells[0])"; $bad++ }
  }
  $now = ([IO.Directory]::GetFiles($savesDir, '*', [IO.SearchOption]::AllDirectories)).Count
  if ($now -ne $count) { "COUNT: $now file(s) in the folder against $count in the manifest"; $bad++ }
  if ($bad -eq 0) { "BACK: all $count file(s) restored with their digests"; exit 0 }
  "BACK: $bad problem(s) - the folder is back but differs from the manifest"; exit 1
}

'usage: -Aside | -Back [-Label <plan label>]'
exit 2
