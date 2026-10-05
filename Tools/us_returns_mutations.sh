#!/bin/bash
# THE US RETURNS GENERATOR'S FAILURE PATHS, PROVED (PS-6 US-3; COMPLETED.md s786). Each case mutates a fresh copy of Tools/us_returns_prep.pl's
# inputs - or, where it says TOOL, the copy's own tool - in a temporary directory, runs the tool there, and requires it to exit non-zero, write
# nothing, and print the exact mismatch the case means (a mutation that lands anywhere else prints something else and is MISSED, never
# CAUGHT). The control, no mutation, must pass and reproduce the repository's own bytes. Nothing under the repository is written.
# Run it after any change to the tool or to the pages it reads.
# Usage: bash Tools/us_returns_mutations.sh   (Git Bash; perl and pdftotext as the tool needs them). Exits 0 only when every case is caught.
set -u
ROOT=$(cd "$(dirname "$0")/.." && pwd)
POKE="$ROOT/Tools/xlsx_poke.pl"
PDFT=/mingw64/bin/pdftotext; [ -x "$PDFT" ] || PDFT=pdftotext
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT
T="$WORK/copy"
TSV=ElectionsData/usa/maine_districts_read_2012_2016.tsv
caught=0; bad=0

fresh() {
  rm -rf "$T"; mkdir -p "$T/ElectionsData" "$T/Tools" "$T/Assets/Scripts/Elections/Generated"
  cp -r "$ROOT/ElectionsData/usa" "$T/ElectionsData/"; rm -f "$T"/ElectionsData/usa/president_by_*.csv; cp "$ROOT/Tools/us_returns_prep.pl" "$T/Tools/"
}
resum() {   # re-pin a mutated page in its group's sums, so the mutation reaches the parser and not the digest check
  local f=$1 g b h; g=$(dirname "$f"); b=$(basename "$f"); h=$(sha256sum "$f" | cut -d' ' -f1)
  sed -i "s/^[0-9a-f]* \*$b\$/$h *$b/" "$g/SHA256SUMS.txt"
}
wrap() {   # TOOL: the copy's tool reads pdftotext's text through the perl -pe program $1
  printf '#!/bin/bash\n%s "$@" | perl -pe %q\n' "$PDFT" "$1" > "$T/pdft.sh"; chmod +x "$T/pdft.sh"
  sed -i "s#^my \$pdftotext = .*#my \$pdftotext = '$T/pdft.sh';#" Tools/us_returns_prep.pl
}
case_() {   # $1 label, $2 the text(s) the run must print (several joined by @@), $3 the mutation (a function run inside the copy)
  fresh
  ( cd "$T" && $3 ) >/dev/null 2>&1
  local out rc wrote ok=1 w
  out=$(cd "$T" && perl Tools/us_returns_prep.pl 2>&1); rc=$?
  wrote=$(ls "$T"/ElectionsData/usa/president_by_*.csv "$T"/Assets/Scripts/Elections/Generated/*.cs 2>/dev/null | wc -l)
  while IFS= read -r w; do grep -qF -- "$w" <<<"$out" || ok=0; done <<< "${2//@@/$'\n'}"
  if [ $rc -ne 0 ] && [ "$wrote" = 0 ] && [ $ok = 1 ]; then echo "CAUGHT  $1"; caught=$((caught+1))
  else echo "MISSED  $1 (exit $rc, $wrote file(s) written)"; tail -4 <<<"$out"; bad=$((bad+1)); fi
}

# --- the control
fresh
out=$(cd "$T" && perl Tools/us_returns_prep.pl 2>&1); rc=$?
same=$(cd "$T" && sha256sum ElectionsData/usa/president_by_*.csv Assets/Scripts/Elections/Generated/UsPresidentialReturns.cs | sed 's/ \*/  /' | sort)
mine=$(cd "$ROOT" && sha256sum ElectionsData/usa/president_by_*.csv Assets/Scripts/Elections/Generated/UsPresidentialReturns.cs | sed 's/ \*/  /' | sort)
if [ $rc -eq 0 ] && [ "$same" = "$mine" ]; then echo "PASSES  the control: no mutation - exit 0, the repository's bytes"
else echo "BROKEN  the control (exit $rc)"; tail -4 <<<"$out"; bad=$((bad+1)); fi

# --- the inputs
m01() { sed -i 's/^2012\tME\t1\tR\t142937/2012\tME\t1\tR\t152937/' $TSV; }
m02() { sed -i 's/^2016\tME\t2\tD\t144817/2016\tME\t2\tD\t144617/' $TSV; }
m03() { sed -i 's/^2016\tME\t2\tR\t181177/2016\tME\t2\tR\t131177/; s/^2016\tME\t-\tunallocated\t184/2016\tME\t-\tunallocated\t50184/' $TSV; }
m04() { sed -i 's/^ME;D;3;1/ME;D;4;0/' ElectionsData/usa/state_ev_2024.csv; }
m05() { printf ' ' >> ElectionsData/usa/raw/returns/archives_electoral_college_2016.html; }
m06() { sed -i '/fec_federalelections2016.xlsx/d' ElectionsData/usa/raw/returns/SHA256SUMS.txt; }
m08() { local f=ElectionsData/usa/raw/returns/archives_electoral_college_2016.html; sed -i 's/Ron Paul 1, and John Kasich 1/Ron Paul 1, and Ted Cruz 1/' $f; resum $f; }
m09() {   # one Maine elector moved from Harris to Trump on NARA's 2024 table: the row's third and fourth cells, 3 and 1, become 2 and 2
  local f=ElectionsData/usa/raw/executive/archives_electoral_college_2024.html
  perl -0777 -i -pe 's{(<tr>(?:(?!</tr>).)*?ascertainment-maine\.pdf.*?</tr>)}{my $r = $1; my $i = 0; $r =~ s/(<td[^>]*>)(.*?)(<\/td>)/my ($o, $c, $e) = ($1, $2, $3); $i++; $c =~ s{3}{2} if $i == 3; $c =~ s{1}{2} if $i == 4; "$o$c$e"/ges; $r}se' $f; resum $f; }
r01() { perl -i -pe 's/^2016\tME\t1\t/2016\tME\tX\t/; s/^2016\tME\t2\t/2016\tME\t1\t/; s/^2016\tME\tX\t/2016\tME\t2\t/' $TSV; }
r02() { perl -i -pe 's/^(2012\tME\t)1(\tD\t)223035/${1}2${2}223035/ or s/^(2012\tME\t)2(\tD\t)177998/${1}1${2}177998/' $TSV; }
r03() { sed -i 's/^2012\tME\t1\tD\t223035/2012\tME\t1\tD\t223235/' $TSV; }
r04() { sed -i 's/^2012\tME\t-\tunallocated\t415/2012\tME\t-\tunallocated\t50415/; s/^2012\tME\t1\tD\t223035/2012\tME\t1\tD\t173035/' $TSV; }
r05() { printf '2016\tME\t-\tunallocated\t1840\traw/district/archives_ascertainment_maine_2016.pdf\t2\n' >> $TSV; }
r06() { sed -i 's#^\(2016\tME\t2\tR\t181177\t\)raw/district/archives_ascertainment_maine_2016.pdf#\1raw/returns/fec_2024presgeresults.xlsx#' $TSV; }
r07() { sed -i '/^2012\tME\t1\tR\t/d' $TSV; }
r08() { sed -i 's/^\(2012\tME\t1\tD\t223035\t[^\t]*\t\)3/\12/' $TSV; }
r09() { sed -i 's/^AL;R;9;0/AK;R;3;0/' ElectionsData/usa/state_ev_2024.csv; }
r10() { sed -i 's/^AL;R;9;0/AL;R;9/' ElectionsData/usa/state_ev_2024.csv; }
r11() { sed -i 's/^state;winner(R\/D);winner_EV;other_EV/state;winner;ev;other/' ElectionsData/usa/state_ev_2024.csv; }
r13() { local f=ElectionsData/usa/raw/executive/archives_electoral_college_2024.html; sed -i 's/Harris-Walz won the Second Congressional District/Harris-Walz won the Third Congressional District/' $f; resum $f; }
r14() { local f=ElectionsData/usa/raw/district/maine_sos_president_by_cd_2020.xlsx; perl "$POKE" $f '<v>380324</v>' '<v>400000</v>'; resum $f; }
r17() { local f=ElectionsData/usa/raw/district/maine_sos_president_by_cd_2020.xlsx; perl "$POKE" $f '<v>380324</v>' ''; resum $f; }
r18() { local f=ElectionsData/usa/raw/district/maine_sos_president_by_cd_2020.xlsx; perl "$POKE" $f '<v>380324</v>' '<v>1000</v>'; resum $f; }
r19() { m08; r06; }
case_ "a READ district figure raised past the state" "ME 2012 R: the districts exceed the state" m01
case_ "a READ district figure lowered past the overseas count" "ME 2016: the nominees' districts fall 376 short of the state" m02
case_ "a district READ that flips its winner" "2016 ME: the record's winners give" m03
case_ "the 2024 typed table contradicting the pages" "state_ev_2024.csv ME" m04
case_ "a saved page changed by one byte" "archives_electoral_college_2016.html: SHA-256" m05
case_ "a page the sums do not list" "fec_federalelections2016.xlsx: in no SHA256SUMS.txt" m06
case_ "NARA's note disagreeing with the FEC's" "2016 TX: the FEC's notes give" m08
case_ "one Maine elector moved between parties on NARA's 2024 table" "2024 ME: cast D 3 R 1 by the FEC, 2 and 2 by NARA" m09
case_ "2016's READ districts swapped (the winners swap, the sums kept)" "ME 2016 1 D READ 144817, the certificate's text layer 212774" r01
case_ "2012's READ Democratic districts swapped (the winners kept)" "ME 2012 1 D READ 177998, the certificate's text layer 223035" r02
case_ "a READ figure changed by 200, inside the old bound" "ME 2012 1 D READ 223235, the certificate's text layer 223035" r03
case_ "the overseas count widened and a figure cut to match" "the footnote's counts" r04
case_ "a second unallocated row" "ME 2016 unallocated twice" r05
case_ "a row citing a page that is not its certificate" "not that year's certificate" r06
case_ "a READ cell deleted" "ME 2012 1 R not READ" r07
case_ "a READ row citing the wrong page" "ME 2012 1 D cites page 2, the figure is on page 3" r08
case_ "the 2024 table losing a state to a duplicate" "no row for AL@@AK: a second row" r09
case_ "the 2024 table's row truncated" "state_ev_2024.csv AL: 'AL;R;9'" r10
case_ "the 2024 table's heading lost" "state_ev_2024.csv: heading" r11
case_ "NARA's note naming the wrong district" "2024 NE-3: NARA's note gives it to D" r13
case_ "a Maine 2020 TBC raised past twice its leader's votes" "not more than half of the 400000 ballots cast" r14
case_ "a Maine 2020 TBC emptied" "the 'CG2 Total' row's TBC cell is empty" r17
case_ "a Maine 2020 TBC understated" "the TBC 1000 is not the sum 380324" r18
case_ "a mismatch found before an input stops the run is still named" "2016 TX: the FEC's notes give@@not that year's certificate" r19

# --- the tool's own copy
t07() { sed -i "s/my %typo = ('2020 R' => 'Trunp');/my %typo = ();/" Tools/us_returns_prep.pl; }
t12() { wrap 's/^Congressional District 1\b/Congressional District X/; s/^Congressional District 2\b/Congressional District 1/; s/^Congressional District X/Congressional District 2/'; }
t15() { perl -i -pe 's/my %typed = \$year == 2012 \? map \{ \$_ => 1 \} 4 \.\. 7 : \(\);/my %typed = ();/' Tools/us_returns_prep.pl; }
t16() { perl -i -pe 's/\$row\{\$p\} = \$r if index\(\$a, "\$nara\{2012\}\{name\}\{\$p\} \("\) == 0;/\$row{\$p eq q(D) ? q(R) : q(D)} = \$r if index(\$a, "\$nara{2012}{name}{\$p} (") == 0;/' Tools/us_returns_prep.pl; }
t20() { wrap 's/\f//g'; }
case_ "TOOL: the declared misprint withdrawn" "heads 'Trunp (R)'" t07
case_ "TOOL: Nebraska's district headings 1 and 2 exchanged by the text extractor (the 2020 and 2024 books)" "2020 NE-1: NARA's note gives it to R" t12
case_ "TOOL: 2012's formula Total cells without their typed counterpart" "is a formula, with no typed national figure" t15
case_ "TOOL: Table 1's nominee rows crossed" "Table 1 types" t16
case_ "TOOL: a text extractor printing no page breaks" "carries no page breaks" t20

echo "caught $caught, missed or broken $bad"
[ $bad -eq 0 ]
