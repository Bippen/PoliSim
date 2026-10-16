#!/bin/bash
# THE GPS 2019 EXTRACT'S FAILURE PATHS, PROVED (PS-6 US-32). Each case mutates a fresh copy of Tools/us_gps_prep.pl's inputs - the out-of-tree
# files (copied from ../PoliSim-captures/sources/usa_us32/, or from US32_SRC) and their list - or, where it says TOOL, the copy's own tool (the
# codebook's text is rewritten through a wrapper around pdftotext), in a temporary directory, runs the tool there, and requires it to exit
# non-zero, write nothing, and print the exact mismatch the case means (a mutation that lands anywhere else prints something else and is MISSED,
# never CAUGHT). The control, no mutation, must pass and reproduce the repository's own bytes (the C# catalog compared without carriage returns,
# which a checkout under core.autocrlf adds). Nothing under the repository or the sources is written.
# Usage: bash Tools/us_gps_mutations.sh   (Git Bash; perl and pdftotext as the tool needs them). Exits 0 only when every case is caught.
set -u
ROOT=$(cd "$(dirname "$0")/.." && pwd)
SRC=${US32_SRC:-$ROOT/../PoliSim-captures/sources}
PDFT=/mingw64/bin/pdftotext; [ -x "$PDFT" ] || PDFT=pdftotext
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT
T="$WORK/copy"
L=ElectionsData/positions/raw/out_of_tree.txt
CSV=ElectionsData/positions/gps_2019_usa.csv
CS=Assets/Scripts/Data/Generated/UsGps2019.cs
S=src/usa_us32
caught=0; bad=0
[ -d "$SRC/usa_us32" ] || { echo "BROKEN  no sources at $SRC/usa_us32 (set US32_SRC)"; exit 1; }

fresh() {
  rm -rf "$T"; mkdir -p "$T/ElectionsData/positions/raw" "$T/Tools" "$T/Assets/Scripts/Data/Generated" "$T/src"
  cp "$ROOT/$L" "$T/$L"
  cp -r "$SRC/usa_us32" "$T/src/"
  cp "$ROOT/Tools/us_gps_prep.pl" "$T/Tools/"
}
resum() {   # re-pin a mutated file's SHA-256 and bytes in the list, so the mutation reaches the reader and not the digest check
  local f=$1 h n; h=$(sha256sum "$f" | cut -d' ' -f1); n=$(stat -c %s "$f")
  perl -i -pe "s{^[0-9a-f]{64}\t\d+\tusa_us32/\Q$(basename "$f")\E\t}{$h\t$n\tusa_us32/$(basename "$f")\t}" "$L"
}
cell() {   # $1 file, $2 column name, $3 the row's selector (perl expression on @F, with %c the columns), $4 perl expression giving the new cell from $v
  perl -i -lne 'BEGIN { ($col, $sel, $new) = splice @ARGV, 0, 3; } my @F = split /\t/, $_, -1; if ($. == 1) { %c = map { $F[$_] => $_ } 0 .. $#F; print; next; }
    if (!$done && eval $sel) { my $v = $F[$c{$col}]; $F[$c{$col}] = eval $new; $done = 1; } print join("\t", @F);' "$2" "$3" "$4" "$1"
}
wrap() {   # TOOL: the copy's tool reads pdftotext's text through the perl -pe program $1
  printf '#!/bin/bash\n%s "$@" | perl -pe %q\n' "$PDFT" "$1" > "$T/pdft.sh"; chmod +x "$T/pdft.sh"
  sed -i "s#^my \$pdftotext = .*#my \$pdftotext = '$T/pdft.sh';#" Tools/us_gps_prep.pl
}
case_() {   # $1 label, $2 the text(s) the run must print (several joined by @@), $3 the mutation (a function run inside the copy)
  fresh
  ( cd "$T" && $3 ) >/dev/null 2>&1
  local out rc wrote ok=1 w
  out=$(cd "$T" && US32_SRC="$T/src" perl Tools/us_gps_prep.pl 2>&1); rc=$?
  wrote=$(cd "$T" && ls $CSV $CS 2>/dev/null | wc -l)
  while IFS= read -r w; do grep -qF -- "$w" <<<"$out" || ok=0; done <<< "${2//@@/$'\n'}"
  if [ $rc -ne 0 ] && [ "$wrote" = 0 ] && [ $ok = 1 ]; then echo "CAUGHT  $1"; caught=$((caught+1))
  else echo "MISSED  $1 (exit $rc, $wrote file(s) written)"; tail -4 <<<"$out"; bad=$((bad+1)); fi
}

# --- the control
fresh
out=$(cd "$T" && US32_SRC="$T/src" perl Tools/us_gps_prep.pl 2>&1); rc=$?
if [ $rc -eq 0 ] && cmp -s "$T/$CSV" "$ROOT/$CSV" && [ "$(tr -d '\r' < "$T/$CS" | sha256sum)" = "$(tr -d '\r' < "$ROOT/$CS" | sha256sum)" ]; then
  echo "PASSES  the control: no mutation - exit 0, the repository's bytes"
else echo "BROKEN  the control (exit $rc)"; tail -4 <<<"$out"; bad=$((bad+1)); fi

# --- the files and their list
m01() { perl -i -pe 's/\t7\.825\t/\t7.826\t/ if /^1001\t"USA"/' $S/gps2019_by_party_v2.tab; }
m02() { sed -i '/usa_us32\/gps2019_by_expert\.tab\t/d' $L; }
m03() { printf 'x' >> $S/gps2019_codebook_6feb2020.pdf; }
m04() { perl -i -pe 'BEGIN { binmode STDIN; binmode STDOUT; } $n++ or s/^(.)/chr(ord($1) ^ 1)/e' $S/gps2019_by_party_v2.sav; resum $S/gps2019_by_party_v2.sav; }
case_ "a source file changed in one byte" "usa_us32/gps2019_by_party_v2.tab: SHA-256" m01
case_ "a source file the list does not name is never read" "usa_us32/gps2019_by_expert.tab: not in $L" m02
case_ "a source file one byte longer" "gps2019_codebook_6feb2020.pdf: 1308052 bytes, $L holds 1308051" m03
case_ "an original whose MD5 is not the one Dataverse publishes" "usa_us32/gps2019_by_party_v2.sav: MD5" m04

# --- the by-party file and its CSV upload
m05() { perl -i -pe 's/\t7\.825\t/\t7.826\t/ if /^1001\t"USA"/' $S/gps2019_by_party_v2.tab; resum $S/gps2019_by_party_v2.tab; }
m06() { perl -i -ne 'print; print if /^1001\t"USA"/' $S/gps2019_by_party_v2.tab; resum $S/gps2019_by_party_v2.tab; }
m07() { perl -i -pe 's/\t"Republican Party"\t/\t"Grand Old Party"\t/ if /^1001\t"USA"/' $S/gps2019_by_party_v2.tab; resum $S/gps2019_by_party_v2.tab; }
m08() { perl -i -pe 's/,7\.825,/,7.835,/ if /^1001,USA,/' $S/gps2019_by_party_v2-2.csv; resum $S/gps2019_by_party_v2-2.csv; }
m09() { cell $S/gps2019_by_party_v2.tab V10 '$F[$c{ID_GPS}] eq "1000"' '"11.5"'; resum $S/gps2019_by_party_v2.tab; }
case_ "a mean the placements do not give" "REP V8_Scale: the 40 placements' mean 7.825 is not the by-party file's 7.826@@REP V8_Scale: the by-party CSV prints '7.825', the table '7.826'" m05
case_ "a party's row twice" "the by-party file holds 2 row(s) for REP, not one" m06
case_ "a GPS id under another party's name" "the by-party file names GPS id 1001 '\"Grand Old Party\"', not \"Republican Party\"" m07
case_ "the CSV upload printing another mean" "REP V8_Scale: the by-party CSV prints '7.835', the table '7.825'" m08
case_ "a mean off the 0-10 scale" "DEM V10: the by-party file's cell '11.5' is not a decimal on 0-10" m09

# --- the by-expert file
m10() { cell $S/gps2019_by_expert.tab 'Q3.5_1' '$F[$c{ISO}] eq q("USA") && $F[$c{"Q3.5_1"}] =~ /^[0-9]$/' '$v + 1'; resum $S/gps2019_by_expert.tab; }
m11() { cell $S/gps2019_by_expert.tab 'Q4.1_2' '$F[$c{ISO}] eq q("USA")' '"42"'; resum $S/gps2019_by_expert.tab; }
m12() { cell $S/gps2019_by_expert.tab CPARTY1 '$F[$c{ISO}] eq q("USA")' 'q("USA_ Democratic Party")'; resum $S/gps2019_by_expert.tab; }
case_ "a placement changed" "REP V8_Scale: the 40 placements' mean" m10
case_ "a placement neither 0-10 nor 99" "DEM V10: a placement '42' that is neither 0-10 nor 99" m11
case_ "an expert row naming the parties in another order" "a US expert row names '\"USA_ Democratic Party\"' and '\"USA_ Democratic Party\"' as its first two parties" m12

# --- the codebook's wording (TOOL: the text through a wrapper)
m13() { wrap 's/POPULIST OR PLURALIST rhetoric/POPULIST rhetoric/'; }
m14() { wrap 's/Extreme economic/Very economic/g'; }
m15() { wrap 's/PEOPLE_VS_ELITE/PEOPLE_V_ELITE/'; }
case_ "an item's question not the codebook's" "the codebook's text has no question for V8_Scale as quoted" m13
case_ "an item's endpoints not the codebook's" "the codebook's text has no endpoints for V4_Scale after its question as quoted" m14
case_ "V19 not marked as CHES PEOPLE_VS_ELITE" "the codebook's text does not mark V19 as CHES PEOPLE_VS_ELITE after its question" m15

echo "us_gps_mutations: $caught caught, $bad missed or broken"
[ $bad -eq 0 ]
