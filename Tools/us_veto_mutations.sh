#!/bin/bash
# THE US VETO RECORD'S FAILURE PATHS, PROVED (PS-6 US-10, COMPLETED.md s802). Each case mutates a fresh copy of Tools/us_veto_prep.pl's inputs -
# the in-tree pages (raw/vetoes/), the out-of-tree roll calls, or, where it says TOOL, the copy's own tool - in a temporary directory, runs the
# tool there, and requires it to exit non-zero, write no CSV, and print the exact mismatch the case means (a mutation that lands anywhere else
# prints something else and is MISSED, never CAUGHT). A mutated file is re-pinned in its digest list unless the case is the digest check itself.
# The BILLSTATUS zips are read in place (US10_BILLSTATUS), never mutated. The control, no mutation, must pass and reproduce the repository's CSV.
# Nothing under the repository or the capture folder is written. Each run reads the five Congresses' zips (about a minute a case).
# Usage: bash Tools/us_veto_mutations.sh   (Git Bash, perl). Exits 0 only when every case is caught.
set -u
ROOT=$(cd "$(dirname "$0")/.." && pwd)
SRC="$ROOT/../PoliSim-captures/sources/usa_us10"
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT
T="$WORK/copy"; O="$WORK/out"
CSV=ElectionsData/usa/us_veto_measures.csv
caught=0; bad=0

fresh() {
  rm -rf "$T" "$O"; mkdir -p "$T/ElectionsData/usa/raw" "$T/Tools" "$O"
  cp -r "$ROOT/ElectionsData/usa/raw/vetoes" "$T/ElectionsData/usa/raw/"
  cp "$ROOT/ElectionsData/usa/senate_seats.csv" "$T/ElectionsData/usa/"
  cp "$ROOT/Tools/us_veto_prep.pl" "$T/Tools/"
  cp -r "$SRC/rolls" "$O/"
}
run() { (cd "$T" && US10_OUT="$O" US10_BILLSTATUS="$SRC/billstatus" perl Tools/us_veto_prep.pl 2>&1); }
repin_out() {   # re-pin a mutated out-of-tree file: its line's digest and bytes
  local rel=$1 h n; h=$(sha256sum "$O/$rel" | cut -d' ' -f1); n=$(stat -c %s "$O/$rel")
  perl -i -pe "s{^[0-9a-f]{64}\t\d+\t\Q$rel\E\t}{$h\t$n\t$rel\t}" "$T/ElectionsData/usa/raw/vetoes/out_of_tree.txt"
}
resum() {   # re-pin a mutated in-tree page in SHA256SUMS.txt
  local f=$1 b h; b=$(basename "$f"); h=$(sha256sum "$f" | cut -d' ' -f1)
  sed -i "s/^[0-9a-f]* \*$b\$/$h *$b/" "$T/ElectionsData/usa/raw/vetoes/SHA256SUMS.txt"
}
case_() {   # $1 label, $2 the text the run must print, $3 the mutation (a function run inside the copy)
  fresh
  ( cd "$T" && $3 ) >/dev/null 2>&1
  local out rc wrote
  out=$(run); rc=$?
  wrote=$(ls "$T/$CSV" 2>/dev/null | wc -l)
  if [ $rc -ne 0 ] && [ "$wrote" = 0 ] && grep -qF -- "$2" <<<"$out"; then echo "CAUGHT  $1"; caught=$((caught+1))
  else echo "MISSED  $1 (exit $rc, $wrote CSV written)"; tail -4 <<<"$out"; bad=$((bad+1)); fi
}

# --- the control
fresh
out=$(run); rc=$?
if [ $rc -eq 0 ] && cmp -s "$T/$CSV" "$ROOT/$CSV"; then echo "PASSES  the control: no mutation - exit 0, the repository's CSV"
else echo "BROKEN  the control (exit $rc)"; tail -4 <<<"$out"; bad=$((bad+1)); fi

V=ElectionsData/usa/raw/vetoes
# --- the roll calls
r01() { perl -0 -i -pe 's{(party="R"[^>]*>[^<]*</legislator>\s*<vote>)Yea(</vote>)}{$1Nay$2}' "$O/rolls/house/2024/roll010.xml"; repin_out rolls/house/2024/roll010.xml; }
r02() { perl -0 -i -pe 's{<vote_cast>Yea</vote_cast>}{<vote_cast>Nay</vote_cast>}' "$O/rolls/senate/vote1182/vote_118_2_00122.xml"; repin_out rolls/senate/vote1182/vote_118_2_00122.xml; }
r03() { perl -0 -i -pe 's{<vote-result>Failed</vote-result>}{<vote-result>Passed</vote-result>}' "$O/rolls/house/2024/roll185.xml"; repin_out rolls/house/2024/roll185.xml; }
r04() { perl -0 -i -pe 's{<vote-question>On Passage</vote-question>}{<vote-question>On Motion to Table</vote-question>}' "$O/rolls/house/2024/roll010.xml"; repin_out rolls/house/2024/roll010.xml; }
r05() { printf ' ' >> "$O/rolls/house/2024/roll010.xml"; }
case_ "a House member's vote flipped (H.J.Res. 98's passage, 2024 roll 10)" "rolls/house/2024/roll010.xml: R's members sum to" r01
case_ "a senator's vote flipped (H.J.Res. 98's passage, 118-2 vote 122)" "rolls/senate/vote1182/vote_118_2_00122.xml: members sum to" r02
case_ "an override's result reversed (H.J.Res. 98, 2024 roll 185)" "H.J.Res. 98 House override: 214-191 gives failed at two thirds of those voting" r03
case_ "a final roll call's question made a motion to table" "roll 10's question 'On Motion to Table'" r04
case_ "a roll call changed by one byte, not re-pinned" "rolls/house/2024/roll010.xml: its digest" r05

# --- the pages
p01() { local f=$(ls $V/senate_vetoes_BidenJR_*.htm); perl -0 -i -pe 's{S\.4199}{S.4198}g' "$f"; resum "$f"; }
p02() { local f=$V/history_house_presidential_vetoes.html; perl -0 -i -pe 's{(Joseph R\. Biden, Jr\.</td><td colspan="1">)13(</td><td colspan="1">\.+</td><td colspan="1">)13}{${1}14${2}14}' "$f"; resum "$f"; }
p03() { local f=$(ls $V/senate_vetoes_BidenJR_*.htm); perl -0 -i -pe 's{No\. 185}{No. 186}' "$f"; resum "$f"; }
p04() { printf ' ' >> $V/history_house_presidential_vetoes.html; }
case_ "a veto missing from the Senate's list (S. 4199 renamed)" "118 S4199: vetoed in the bills' record but not on the Senate's lists" p01
case_ "the House Historian's Biden count raised (13 to 14)" "Joseph R. Biden Jr.: the record has 13 regular, 0 pocket, 0 overridden; the House Historian 14, 0, 0" p02
case_ "an override's vote number off on the Senate's list (H.J.Res. 98: No. 186)" "the Senate's list gives votes (186" p03
case_ "a page changed by one byte, not re-summed" "history_house_presidential_vetoes.html: digest differs from SHA256SUMS.txt" p04

# --- TOOL
t01() { sed -i 's/^            my \$enBloc = .*/            my $enBloc = 0;/' Tools/us_veto_prep.pl; }
t02() { sed -i "s/^    return 1 if \$raw =~ \/\^Passed\\\\\/agreed to in House:\/;/    0;/" Tools/us_veto_prep.pl; }
case_ "TOOL the en bloc Senate vote no longer read as its measures' (S.J.Res. 37)" "S.J.Res. 37 (116th) Senate final: roll 179 is on 'S.J.Res. 48'" t01
case_ "TOOL the record's own passage summary no longer an agreement (the en bloc House suspensions lost)" "no House agreement before presentment" t02
t03() { perl -i -ne 'print unless /^\s+\|\| \$t =~ \/\^Senate agreed\\b/' Tools/us_veto_prep.pl; }   # the Senate's concurrence wording dropped
case_ "TOOL the Senate's concurrence in a House amendment no longer read (the guard)" "with an amendment - the other chamber's final agreement is not read" t03

echo "$caught caught, $bad missed or broken"
[ $bad -eq 0 ]
