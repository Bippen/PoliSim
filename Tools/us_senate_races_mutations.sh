#!/bin/bash
# THE US SENATE RACES GENERATOR'S FAILURE PATHS, PROVED (PS-6 US-12, COMPLETED.md s793 and s796). Each case mutates a fresh copy of
# Tools/us_senate_races_prep.pl's inputs - or, where it says TOOL, the copy's own tool; a case reaching a PDF's text rewrites the copy's
# extractor output instead (wrap), in both readings or in the one it names - in a temporary directory, runs the tool there, and requires it to
# exit non-zero, write nothing, and print the exact mismatch the case means. The control, no mutation, must pass and reproduce the
# repository's own bytes. Nothing under the repository is written. Run it after any change to the tool or to the pages it reads.
# Usage: bash Tools/us_senate_races_mutations.sh   (Git Bash; perl and pdftotext as the tool needs them). Exits 0 only when every case is caught.
set -u
ROOT=$(cd "$(dirname "$0")/.." && pwd)
PDFT=/mingw64/bin/pdftotext; [ -x "$PDFT" ] || PDFT=pdftotext
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT
T="$WORK/copy"
OUTS="ElectionsData/usa/senate_races.csv Assets/Scripts/Elections/Generated/UsSenateRaces.cs"
caught=0; bad=0
G=ElectionsData/usa/raw/returns

fresh() {
  rm -rf "$T"; mkdir -p "$T/ElectionsData/usa/raw" "$T/Tools" "$T/Assets/Scripts/Elections/Generated"
  cp -r "$ROOT/$G" "$T/ElectionsData/usa/raw/"; cp "$ROOT/ElectionsData/usa/senate_seats.csv" "$T/ElectionsData/usa/"
  cp "$ROOT/Tools/us_senate_races_prep.pl" "$T/Tools/"
}
resum() { local f=$1 g b h; g=$(dirname "$f"); b=$(basename "$f"); h=$(sha256sum "$f" | cut -d' ' -f1); sed -i "s/^[0-9a-f]* \*$b\$/$h *$b/" "$g/SHA256SUMS.txt"; }
wrap() {   # TOOL: the copy's tool reads pdftotext's text through the perl -pe program $1 - in both readings, or only in the one $2 names
  local only=${2:-both}
  printf '#!/bin/bash\nmode=raw; for a in "$@"; do [ "$a" = -table ] && mode=table; done\nif [ %q = both ] || [ %q = "$mode" ]; then %s "$@" | perl -pe %q; else %s "$@"; fi\n' \
    "$only" "$only" "$PDFT" "$1" "$PDFT" > "$T/pdft.sh"; chmod +x "$T/pdft.sh"
  sed -i "s#^my \$pdftotext = .*#my \$pdftotext = '$T/pdft.sh';#" Tools/us_senate_races_prep.pl
}
case_() {
  fresh
  ( cd "$T" && $3 ) >/dev/null 2>&1
  local out rc wrote ok=1 w
  out=$(cd "$T" && perl Tools/us_senate_races_prep.pl 2>&1); rc=$?
  wrote=$(cd "$T" && ls $OUTS 2>/dev/null | wc -l)
  while IFS= read -r w; do grep -qF -- "$w" <<<"$out" || ok=0; done <<< "${2//@@/$'\n'}"
  if [ $rc -ne 0 ] && [ "$wrote" = 0 ] && [ $ok = 1 ]; then echo "CAUGHT  $1"; caught=$((caught+1))
  else echo "MISSED  $1 (exit $rc, $wrote file(s) written)"; tail -4 <<<"$out"; bad=$((bad+1)); fi
}

# --- the control
fresh
out=$(cd "$T" && perl Tools/us_senate_races_prep.pl 2>&1); rc=$?
same=$(cd "$T" && sha256sum $OUTS | sed 's/ \*/  /' | sort)
mine=$(cd "$ROOT" && sha256sum $OUTS | sed 's/ \*/  /' | sort)
if [ $rc -eq 0 ] && [ "$same" = "$mine" ]; then echo "PASSES  the control: no mutation - exit 0, the repository's bytes"
else echo "BROKEN  the control (exit $rc)"; tail -4 <<<"$out"; bad=$((bad+1)); fi

# --- the pages
p01() { printf ' ' >> $G/clerk_statistics2024.pdf; }
p02() { sed -i '/fec_federalelections2018\.xlsx/d' $G/SHA256SUMS.txt; }
case_ "a volume changed by one byte" "clerk_statistics2024.pdf: SHA-256" p01
case_ "the FEC's workbook not in its sums" "fec_federalelections2018.xlsx: in no SHA256SUMS.txt" p02

# --- the listings (through the extractor's text)
r01() { wrap 's/^(Kyrsten Sinema, Democrat \.+\s*)1,191,100/${1}1,191,000/' raw; }
r02() { wrap 's/^(Kyrsten Sinema, Democrat \.+\s*)1,191,100/${1}1,191,101/'; }
r03() { wrap 's/^(\s*Martha McSally, Republican \.+\s*)1,135,200/${1}1135,200/'; }
r04() { wrap 's/^(\s*Martha McSally), Republican /$1, Republican Party /'; }
r05() { sed -i "s/^my %unnamed_ok = .*/my %unnamed_ok = ();/" Tools/us_senate_races_prep.pl; }   # TOOL
r06() { sed -i "s/^my %fusion_of = (CT => /my %fusion_of = (XX => /" Tools/us_senate_races_prep.pl; }   # TOOL
r07() { wrap 's/^(\s*Martha McSally, Republican \.+\s*)1,135,200/${1}1,191,100/'; }
r08() { sed -i "/^    '2024 CA' => /s/, '2024 NE' => { class => 2, ends => 'January 3, 2027' }//" Tools/us_senate_races_prep.pl; }   # TOOL
r09() { sed -i "s/'2018 MN' => { class => 2, ends => 'January 3, 2021' }/'2018 MN' => { class => 2, ends => 'January 3, 2023' }/" Tools/us_senate_races_prep.pl; }   # TOOL
r10() { sed -i "s/^my %marked = .*/my %marked = ();/" Tools/us_senate_races_prep.pl; }   # TOOL
case_ "a figure changed in the -raw reading alone (Sinema)" "2018 AZ: the -table reading's entries are not the -raw reading's - entry 1: 'Kyrsten Sinema, Democrat=1,191,000' against 'Kyrsten Sinema, Democrat=1,191,100'" r01
case_ "a figure changed in both readings (Sinema), against the recapitulation" "2018 AZ: the Senator row's Total 2384308, the races' entries 2384309" r02
case_ "a figure not grouped by thousands (McSally)" "2018 AZ: the figure '1135,200' of 'Martha McSally, Republican', not grouped by thousands" r03
case_ "a label naming a party in a form not declared" "2018 AZ: the label 'Republican Party' (Martha McSally) names a party in a form not declared" r04
case_ "TOOL: Arizona's unnamed entry undeclared (a fusion label elsewhere, so refused as a line)" "2018 AZ: the party line 'Green' outside a fusion state" r05
case_ "TOOL: Connecticut not a fusion state" "2016 CT: the party line 'Working Families' outside a fusion state" r06
case_ "a tie for first (McSally at Sinema's figure)" "2018 AZ class 1: a tie for first" r07
case_ "TOOL: Nebraska's 2024 special undeclared" "2024 NE: two races, no special declared" r08
case_ "TOOL: Minnesota's 2018 special declared to another term" "2018 MN: an unexpired term ending January 3, 2021, declared January 3, 2023" r09
r11() { wrap 's/^(\s*Martha McSally, Republican) \.+\s*1,135,200/$1 1,135,200/'; }
r12() { wrap 's/^\(For full term beginning January 3, 2019\)/(For full term beginning January 3, 2020)/'; }
r13() { wrap 's/^2This vote count (.*?)Cindy Hyde-Smith received/2This vote count ${1}Mike Espy received/' raw; }
r14() { wrap 's/^(Senator \.+ 1,191,100 1,135,200 )57,442/${1}57,443/'; }
case_ "TOOL: Mississippi's footnote marks undeclared" "2018 MS: the figure '1420,819' of 'Mike Espy, Democrat', not grouped by thousands" r10
case_ "an entry's dots lost, its line no entry (McSally)" "a line in the Senate listing that is no entry: 'Martha McSally, Republican 1,135,200'" r11
case_ "a full term beginning a year late" "2018 MN: a full term beginning January 3, 2020, not January 3, 2019" r12
case_ "a mark's footnote naming the other finalist (the mark then no mark)" "2018 MS: the figure '2486,769' of 'Cindy Hyde-Smith, Republican', not grouped by thousands" r13
case_ "a recapitulation cell changed (Arizona's Green)" "2018 AZ: the Senator row's cells sum to 2384309, its Total 2384308" r14

# --- the races the item holds, the FEC, the winners
n01() { sed -i "s/2020 => 2, 2022 => 3/2020 => 1, 2022 => 3/" Tools/us_senate_races_prep.pl; }   # TOOL: 2020's regular class mistaken
g01() { sed -i "s/'2020 AZ' => { class => 3, ends => 'January 3, 2023', alone => 1 }/'2020 AZ' => { class => 3, ends => 'January 3, 2023' }/" Tools/us_senate_races_prep.pl; }   # TOOL
g02() { sed -i "s/'2020 GA 3' => 2, //" Tools/us_senate_races_prep.pl; }   # TOOL: Georgia's 2020 special's marks undeclared
g03() { wrap 's/Raphael Warnock received 1,617,035/Raphael Smith received 1,617,035/' raw; }
g04() { sed -i "s/^    SC => { map { \$_ => 1 } ('Working Families', 'Green', 'Libertarian') });/    );/" Tools/us_senate_races_prep.pl; }   # TOOL
g05() { sed -i "s/2020 => \['NJ full'\]/2020 => []/" Tools/us_senate_races_prep.pl; }   # TOOL
g06() { sed -i "s/^    my %fec_last_slip = .*/    my %fec_last_slip = ();/" Tools/us_senate_races_prep.pl; }   # TOOL
g07() { sed -i "s/^ME,1,Angus S\. King Jr\.,I,-,\([^,]*\),-,before the window,-,D,.*/ME,1,Angus S. King Jr.,I,-,\1,-,before the window,-,D,-/" ElectionsData/usa/senate_seats.csv; }
f01() { sed -i "s/^    my %votes_differ = .*/    my %votes_differ = ();/" Tools/us_senate_races_prep.pl; }   # TOOL
f02() { sed -i "s/my %fec_class = (D => 'D', DFL => 'D', /my %fec_class = (D => 'D', /" Tools/us_senate_races_prep.pl; }   # TOOL
w01() { sed -i 's/^AZ,1,Ruben Gallego,/AZ,1,Ruben Galleg,/' ElectionsData/usa/senate_seats.csv; }
w02() { sed -i 's/Joe Manchin III/Joe Smith III/g' ElectionsData/usa/senate_seats.csv; }   # the surname under a numeral suffix
f03() { sed -i "s#('AZ full' => 'W(GRE)/GRE') : ();#('AZ full' => 'GRE') : ();#" Tools/us_senate_races_prep.pl; }   # TOOL
case_ "TOOL: 2020's regular class mistaken" "2020: the Class 1 states without a full-term race" n01
case_ "TOOL: Arizona's 2020 special not declared alone" "2020 AZ: one race, a special declared" g01
case_ "TOOL: Georgia's 2020 special's marks undeclared (its figures read whole)" "2020 GA: the Senator row's Total 11109014" g02
case_ "Georgia's 2020 special footnote naming another (the mark not read)" "2020 GA class 3: 1 footnote mark(s) declared and not found" g03
case_ "TOOL: South Carolina's 2016 fusion undeclared" "2016 SC: the party line 'Working Families' outside a fusion state" g04
case_ "TOOL: New Jersey's 2020 FEC difference undeclared" "FEC 2020 NJ full: the W's votes 2541239, the Clerk's winner Cory A. Booker 2541178" g05
case_ "TOOL: the FEC's Tilllis slip undeclared" "FEC 2020 NC full: the W Tilllis, the Clerk's winner Thom Tillis" g06
x01() { wrap 's/^(\s*)Spoiled Votes /${1}Spoiled Ballots /'; }
x02() { wrap 's/\(For unexpired term ending January 3, 2023\)/(For full term beginning January 3, 2021)/'; }
x03() { wrap 's/Raphael Warnock received 1,617,035/Raphael Warnock received 1617,035/' raw; }
x04() { wrap 's/Raphael Warnock received 1,617,035/Raphael Warnock received 1,617,036/' raw; }
x05() { sed -i "s/^    'No Party' => 'I', /    /" Tools/us_senate_races_prep.pl; }   # TOOL
case_ "a nameless entry undeclared outside a fusion state (Vermont's spoiled ballots)" "2016 VT: the entry 'Spoiled Ballots' - no name, outside a fusion state, not declared" x01
case_ "Arizona's lone 2020 special under a full-term heading" "2020 AZ: its one race, declared a special alone, not under an unexpired term's heading" x02
case_ "a runoff footnote's first-round figure not grouped by thousands" "footnote 1 gives warnock '1617,035', not grouped by thousands" x03
case_ "a runoff finalist's first round off the FEC's (Warnock)" "FEC 2020 GA unexpired: Raphael Warnock's first round 1617035, his footnote's 1617036" x04
case_ "TOOL: the label No Party undeclared" "the label 'No Party' (" x05
case_ "an independent winner's caucus unsourced (King)" "2018 ME class 1: the independent winner Angus S. King, Jr., his caucus 'D' by '-'" g07
case_ "TOOL: Ohio's FEC difference undeclared" "FEC 2018 OH full: the W's votes 2358508, the Clerk's winner Sherrod Brown 2355923" f01
case_ "TOOL: the FEC's DFL code undeclared" "FEC 2018 MN full: the W's party DFL" f02
case_ "a winner not his seat's holder by s792's catalog (Gallego)" "2024 AZ class 1 (full): the winner Ruben Gallego, the seat's holder on 2025-01-21 Ruben Galleg" w01
case_ "a holder another man of the same suffix (Manchin III)" "2018 WV class 1 (full): the winner Joe Manchin III, the seat's holder on 2019-01-09 Joe Smith III" w02
case_ "TOOL: Arizona's unnamed entry held to a code the FEC never prints" "FEC 2018 AZ full: 0 GRE lines, not one" f03

# --- the copy's own tool
t01() { perl -i -pe 's/^(my %class_states = )/my \$u; my \$warn = "x" . \$u; $1/' Tools/us_senate_races_prep.pl; }   # TOOL: an undefined value, after the warning hook
t02() { sed -i 's/^my \$csv = \$gen \. "# US-12: a row a Senate race/my $csv = $gen . "# US-12: a row a Senate r\xC3\xA4ce/' Tools/us_senate_races_prep.pl; }
t03() { sed -i 's/(my \$wn = \$x->{winner}{name}) =~ s\/,\/\/g;/my $wn = $x->{winner}{name} . ", x";/' Tools/us_senate_races_prep.pl; }
case_ "TOOL: a perl warning" "a perl warning: Use of uninitialized value" t01
case_ "TOOL: a non-ASCII byte in an output" "ElectionsData/usa/senate_races.csv: not ASCII at its line" t02
case_ "TOOL: a comma in a winner's name" "has 24 fields, its heading 23 - nothing written" t03

echo "$caught caught, $bad missed or broken"
[ $bad -eq 0 ]
