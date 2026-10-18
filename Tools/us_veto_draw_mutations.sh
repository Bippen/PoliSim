#!/bin/bash
# R-US17 RULE (c)'S INSTRUMENT, ITS FAILURE PATHS PROVED (COMPLETED.md s834; the block's THE MUTATION SUITE). Each case puts ONE mutation in place
# in the project - a byte of the record, the plan's block or its mirror line, the catalog, or (CODE, s830's form) one line of the instrument or of
# UsVeto - runs the named check UsVetoDrawCheck alone (Tools/unity_run.ps1, CheckSuite.RunNamedBatch), and requires it to FAIL and to print the
# case's own message; a mutation that lands anywhere else prints something else and is MISSED, never CAUGHT. The file is restored from its copy
# before the next case, and a trap restores every file on any exit. The control, no mutation, runs first and last: the check passes, so the
# document and the catalog are what the run generates, byte for byte. A CODE case recompiles on its run and again on the next.
# Nothing else is written; each run's log is PoliSim-captures/logs/usvd_<case>.log. Refuses to start while a Unity runs (unity_run.ps1's own check).
# Usage: bash Tools/us_veto_draw_mutations.sh   (Git Bash, perl, PowerShell 5.1). Exits 0 only when both controls pass and every case is caught.
set -u
ROOT=$(cd "$(dirname "$0")/.." && pwd)
LOGS="$ROOT/../PoliSim-captures/logs"
WORK=$(mktemp -d)
CSV=ElectionsData/usa/us_veto_measures.csv
PLAN=docs/specs/USA_STAGE_PLAN.md
CAT=Assets/Scripts/Elections/Generated/UsVetoDefections.cs
CHECK=Assets/Editor/UsVetoDrawCheck.cs
RUNTIME=Assets/Scripts/Elections/UsVeto.cs
FILES=("$CSV" "$PLAN" "$CAT" "$CHECK" "$RUNTIME")
for f in "${FILES[@]}"; do mkdir -p "$WORK/$(dirname "$f")"; cp -p "$ROOT/$f" "$WORK/$f" || { echo "BROKEN  cannot copy $f"; exit 2; }; done
restore() { for f in "${FILES[@]}"; do cmp -s "$WORK/$f" "$ROOT/$f" || cp -p "$WORK/$f" "$ROOT/$f"; done; }
trap 'restore; rm -rf "$WORK"' EXIT
caught=0; bad=0

run() {   # $1 the run's label; prints the exit code, or "refused" where unity_run.ps1 launched nothing (a Unity running, the lockfile held)
  rm -f "$LOGS/$1.log"   # a stale log never stands in for this run's
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$ROOT/Tools/unity_run.ps1")" -Method PoliSim.EditorTools.CheckSuite.RunNamedBatch \
    -Label "$1" -Extra "-checks=UsVetoDrawCheck" > "$WORK/run.out" 2>&1
  local rc=$?
  if grep -q '^LAUNCHED:' "$WORK/run.out" && [ -f "$LOGS/$1.log" ]; then echo $rc; else echo refused; fi
}
control() {   # $1 label; the first control refused ends the suite
  restore
  local rc; rc=$(run "usvd_$1")
  if [ "$rc" = refused ]; then echo "BROKEN  the control ($1): Unity was not launched"; tail -3 "$WORK/run.out"; bad=$((bad+1)); [ "$1" = first ] && { echo "== stopped: no case runs without its control"; exit 2; }; return; fi
  if [ "$rc" = 0 ] && grep -q "CHECKS: 1 of 1 clean." "$LOGS/usvd_$1.log" && grep -q "the document docs/generated/US_VETO_DRAW.md is what this run writes, byte for byte" "$LOGS/usvd_$1.log" \
     && grep -q "the catalog $CAT is what this run generates, byte for byte" "$LOGS/usvd_$1.log"; then echo "PASSES  the control ($1): no mutation - the check clean, the document and the catalog byte for byte"
  else echo "BROKEN  the control ($1): exit $rc"; tail -5 "$WORK/run.out"; bad=$((bad+1)); fi
}
case_() {   # $1 label, $2 the mutation (a function run in the project root), then one or more extended regexes the failing run's log must match
  local label=$1 mut=$2; shift 2
  restore
  ( cd "$ROOT" && $mut )
  local changed=0 f
  for f in "${FILES[@]}"; do cmp -s "$WORK/$f" "$ROOT/$f" || changed=1; done
  if [ $changed = 0 ]; then echo "BROKEN  $label: the mutation changed nothing"; bad=$((bad+1)); return; fi
  local rc; rc=$(run "usvd_$label")
  if [ "$rc" = refused ]; then echo "BROKEN  $label: Unity was not launched"; bad=$((bad+1)); restore; return; fi
  local log="$LOGS/usvd_$label.log" ok=1 rx
  [ "$rc" != 0 ] || ok=0
  for rx in "$@"; do grep -qE -- "$rx" "$log" || { ok=0; echo "        not found: $rx"; }; done
  if [ $ok = 1 ]; then echo "CAUGHT  $label (exit $rc)"; caught=$((caught+1)); else echo "MISSED  $label (exit $rc)"; bad=$((bad+1)); fi
  restore
}

control first

# --- the record (each also fails the source pin, which is the first case's own message)
m_csv()   { printf '\n' >> "$CSV"; }   # a trailing empty line: the rows read the same, the bytes do not
m_third() { perl -i -pe 'if (!$d && /^116,S\. 906,/) { s/,roll \d+,/,voice vote,/; $d = 1 }' "$CSV"; }   # S. 906's House roll call taken away
m_hr131() { perl -i -pe 'if (!$d && /^119,H\.R\. 131,/) { s/,voice vote,/,roll 999,/; $d = 1 }' "$CSV"; }   # H.R. 131 given a roll call
case_ csv_byte m_csv 'FAIL +the SOURCE PIN: the record'"'"'s SHA-256'
case_ third_vetoed m_third 'FAIL +the UNPREDICTABLE set is \{[^}]*116\|S\. 906'
case_ hr131_roll m_hr131 'FAIL +the UNPREDICTABLE set is \{119\|H\.R\. 504\}'

# --- the plan
m_block()  { perl -i -pe 's/(N_MIN = 12 observations): a floor/$1 : a floor/ if /N_MIN = 12 observations: a floor/' "$PLAN"; }   # one space inside the block
m_mirror() { perl -i -pe 'if ($e && /\S/) { s/\.(\r?)$/ x.$1/; $e = 0 } $e = 1 if /<!-- R-US17 RULE END -->/' "$PLAN"; }   # the first non-blank line after END, whatever its state
case_ block_edit m_block 'FAIL +the block'"'"'s digest [0-9a-f]{8} is not the RuleDigests list'"'"'s last'
case_ mirror_edit m_mirror 'FAIL +the plan'"'"'s line after the block mirrors the ruling record'

# --- the catalog (recompiles)
m_cat() { perl -i -pe 'if (!$d && /new Entry\("[^"]+", \d+, (\d+), \d+\),/) { my $x = $1 + 1; s/(new Entry\("[^"]+", \d+, )\d+/$1$x/; $d = 1 }' "$CAT"; }
case_ catalog_edit m_cat 'FAIL +the catalog Assets/Scripts/Elections/Generated/UsVetoDefections\.cs differs from the generated one'

# --- CODE (s830's form: one line of the instrument or the runtime, in place for one run)
m_share()  { perl -i -pe 's/AddObs\(fit, rc, '"'"'P'"'"', pp, sp\.Yea, /AddObs(fit, rc, '"'"'P'"'"', pp, sp.Nay, /' "$CHECK"; }   # the PP's crossers read as its nays
m_budget() { perl -i -pe 's/PpCell = !budget && sPp > 0 \?/PpCell = sPp > 0 ?/' "$CHECK"; }   # a RECORD BUDGET CLASS measure made to draw
m_thin()   { perl -i -pe 's/public const int MinimumObservations = 12;/public const int MinimumObservations = 100;/' "$RUNTIME"; }   # every cell too thin
m_s2empty(){ perl -i -pe 's/m\.Expedited = ExpeditedKinds\.Contains\(m\.Kind\);/m.Expedited = true;/' "$CHECK"; }   # no RULE XXII measure left
m_s2never(){ perl -i -pe 's/case Procedure\.SenateCloture: return y >= \(3 \* v\.Sworn \+ 4\) \/ 5;/case Procedure.SenateCloture: return false;/' "$RUNTIME"; }   # cloture never reached
m_s1()     { perl -i -pe 's/if \(j\.Vetoed\) \{ t\.EHits \+= s\.Named; \}/if (j.Vetoed \&\& false) { t.EHits += s.Named; }/' "$CHECK"; }   # no named measure counted vetoed
case_ share_above_half m_share 'FAIL +every share d/m <= 1/2 - violated by'
case_ budget_draws m_budget 'FAIL +no RECORD BUDGET CLASS measure draws on the record - drawing:'
case_ planted_unfitted m_thin 'UNFITTED n_own=[0-9]+, pooled with [HS][PO] n=[0-9]+ - fewer than 100 observations' 'THE OUTCOME: S3$'
case_ planted_s2_empty m_s2empty 'S2, NEVER SENDS - case "no Rule XXII measure on the veto path"' 'THE OUTCOME: (S2|S1 \+ S2)$'
case_ planted_s2_never m_s2never 'S2, NEVER SENDS - case "never sends"' 'THE OUTCOME: (S2|S1 \+ S2)$'
case_ planted_s1 m_s1 'S1, FAR MORE: E\[hits\]' 'THE OUTCOME: (S1|S1 \+ S2)$'

control last

echo "== $caught caught, $bad missed or broken"
[ $bad = 0 ]
