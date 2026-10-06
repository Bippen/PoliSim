#!/bin/bash
# THE US SENATE GENERATOR'S FAILURE PATHS, PROVED (PS-6 US-12, COMPLETED.md s792). Each case mutates a fresh copy of Tools/us_senate_prep.pl's
# inputs - or, where it says TOOL, the copy's own tool; a case reaching a PDF's text rewrites the copy's extractor output instead (wrap) - in a
# temporary directory, runs the tool there, and requires it to exit non-zero, write nothing, and print the exact mismatch the case means (a
# mutation that lands anywhere else prints something else and is MISSED, never CAUGHT). The control, no mutation, must pass and reproduce the
# repository's own bytes. Nothing under the repository is written. Run it after any change to the tool or to the pages it reads.
# Usage: bash Tools/us_senate_mutations.sh   (Git Bash; perl and pdftotext as the tool needs them). Exits 0 only when every case is caught.
set -u
ROOT=$(cd "$(dirname "$0")/.." && pwd)
PDFT=/mingw64/bin/pdftotext; [ -x "$PDFT" ] || PDFT=pdftotext
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT
T="$WORK/copy"
OUTS="ElectionsData/usa/senate_seats.csv ElectionsData/usa/senate_changes.csv ElectionsData/usa/senate_on.csv ElectionsData/usa/senate_division.csv Assets/Scripts/Elections/Generated/UsSenateRecord.cs"
caught=0; bad=0
S=ElectionsData/usa/raw/senate
R=ElectionsData/usa/raw/records

fresh() {
  rm -rf "$T"; mkdir -p "$T/ElectionsData/usa/raw" "$T/Tools" "$T/Assets/Scripts/Elections/Generated"
  cp -r "$ROOT/$S" "$ROOT/$R" "$T/ElectionsData/usa/raw/"
  cp "$ROOT/Tools/us_senate_prep.pl" "$T/Tools/"
}
resum() {   # re-pin a mutated page in its group's sums, so the mutation reaches the parser and not the digest check
  local f=$1 g b h; g=$(dirname "$f"); b=$(basename "$f"); h=$(sha256sum "$f" | cut -d' ' -f1)
  sed -i "s/^[0-9a-f]* \*$b\$/$h *$b/" "$g/SHA256SUMS.txt"
}
pg() { ls $S/$1 | head -1; }   # a page of the senate group by its pattern
wrap() {   # TOOL: the copy's tool reads pdftotext's text through the perl -pe program $1
  printf '#!/bin/bash\n%s "$@" | perl -pe %q\n' "$PDFT" "$1" > "$T/pdft.sh"; chmod +x "$T/pdft.sh"
  sed -i "s#^my \$pdftotext = .*#my \$pdftotext = '$T/pdft.sh';#" Tools/us_senate_prep.pl
}
case_() {   # $1 label, $2 the text(s) the run must print (several joined by @@), $3 the mutation (a function run inside the copy)
  fresh
  ( cd "$T" && $3 ) >/dev/null 2>&1
  local out rc wrote ok=1 w
  out=$(cd "$T" && perl Tools/us_senate_prep.pl 2>&1); rc=$?
  wrote=$(cd "$T" && ls $OUTS 2>/dev/null | wc -l)
  while IFS= read -r w; do grep -qF -- "$w" <<<"$out" || ok=0; done <<< "${2//@@/$'\n'}"
  if [ $rc -ne 0 ] && [ "$wrote" = 0 ] && [ $ok = 1 ]; then echo "CAUGHT  $1"; caught=$((caught+1))
  else echo "MISSED  $1 (exit $rc, $wrote file(s) written)"; tail -4 <<<"$out"; bad=$((bad+1)); fi
}

# --- the control (fresh copies no Assets/, so the C# part exists in the copy only if the run writes it)
fresh
mkdir -p "$T/ElectionsData/usa"
out=$(cd "$T" && perl Tools/us_senate_prep.pl 2>&1); rc=$?
same=$(cd "$T" && sha256sum $OUTS | sed 's/ \*/  /' | sort)
mine=$(cd "$ROOT" && sha256sum $OUTS | sed 's/ \*/  /' | sort)
if [ $rc -eq 0 ] && [ "$same" = "$mine" ]; then echo "PASSES  the control: no mutation - exit 0, the repository's bytes"
else echo "BROKEN  the control (exit $rc)"; tail -4 <<<"$out"; bad=$((bad+1)); fi

# --- the pages and their sums
p01() { printf ' ' >> "$(pg 'wayback_senate_NewSenators_*.html')"; }
p02() { sed -i '/wayback_senate_SenatorsDiedinOffice_/d' $S/SHA256SUMS.txt; }
p03() { local f; f=$(pg 'wayback_senate_state_WV_*.html'); rm "$f"; sed -i "/$(basename "$f")/d" $S/SHA256SUMS.txt; }
case_ "a page changed by one byte" "wayback_senate_NewSenators_20260906053851.html: SHA-256" p01
case_ "a page its sums do not list is never read" "senate/wayback_senate_SenatorsDiedinOffice_\d{14}\.html: 0 saved pages match, not one" p02
case_ "a state's page missing" "wayback_senate_state_WV_(\d{14})\.html: 0 saved pages match, not one" p03
p04() { sed -i '/govinfo_cdir_2024-04-25\.pdf/d' $S/SHA256SUMS.txt; }
case_ "a page read by its name and not in its sums (the Directory of 2024)" "govinfo_cdir_2024-04-25.pdf: in no SHA256SUMS.txt - the tool reads saved pages only" p04

# --- the state pages
s01() { local f; f=$(pg 'wayback_senate_state_NJ_*.html'); perl -0777 -i -pe 's{Andy  Kim \(D\)}{Andy  Kim (DFL)}' "$f"; resum "$f"; }
s02() { local f; f=$(pg 'wayback_senate_state_NJ_*.html'); perl -0777 -i -pe 's{(George S\. Helmy \(D\)</a></td><td data-label="TERM BEGAN">\s*Aug\.\s*)23, 2024}{${1}19, 2024}' "$f"; resum "$f"; }
s03() { local f; f=$(pg 'wayback_senate_state_NJ_*.html'); perl -0777 -i -pe 's{(Andy  Kim \(D\)</a><sup><a href="#note7">7</a></sup></td><td data-label="TERM BEGAN">\s*Dec\.\s*8, 2024</td><td data-label="TERM ENDED">)\s*Present\s*}{${1}Jan. 3, 2025}' "$f"; resum "$f"; }
s04() { local f; f=$(pg 'wayback_senate_state_NJ_*.html'); perl -0777 -i -pe 's{<a name="note7">&nbsp;</a>7\.}{<a name="note8">&nbsp;</a>8.}' "$f"; resum "$f"; }
case_ "a party label not declared (Kim DFL)" "NJ Andy Kim: party label 'DFL' not declared" s01
case_ "a holder beginning before his predecessor ended (Helmy 19 Aug)" "NJ class 1: Robert Menendez ended 2024-08-20, after George S. Helmy began 2024-08-19" s02
case_ "a class's last holder ended (Kim)" "NJ class 1: its last holder has ended" s03
case_ "a footnote mark without its note" "NJ Andy Kim: footnote 7 has no note" s04
s05() { local f; f=$(pg 'wayback_senate_state_NJ_*.html'); perl -0777 -i -pe 's{Andy  Kim \(D\)}{Andy  Kim}' "$f"; resum "$f"; }
case_ "a row of the window without its party (Kim)" "NJ: 'Andy Kim' has no party in parentheses" s05

# --- the oaths
o01() { local f; f=$(pg 'wayback_senate_NewSenators_*.html'); sed -i 's|<td>Pete Ricketts</td>|<td>Pete Rickets</td>|' "$f"; resum "$f"; }
o02() { local f; f=$(pg 'wayback_senate_NewSenators_*.html'); perl -0777 -i -pe 's{<tr[^>]*>\s*<td>Alan Armstrong</td>.*?</tr>}{}s' "$f"; resum "$f"; }
o03() { local f; f=$(pg 'wayback_senate_NewSenators_*.html'); perl -0777 -i -pe 's{(<td>Jim Justice</td>.*?)January 14, 2025}{${1}January 13, 2025}s' "$f"; resum "$f"; }
case_ "a new senator matching no row of his state's page" "New Senators 118: Pete Rickets (R-NE) matches no row of NE's page" o01
case_ "a row of the window with no oath (Armstrong)" "OK Alan Armstrong: began 2026-03-24, inside the window, with no oath on the New Senators page" o02
case_ "Justice's oath a day early, against [SEN-DIV]'s note" "[SEN-DIV] note: James Justice sworn 2025-01-14, the roster 2025-01-13" o03
o04() { local f; f=$(pg 'wayback_senate_NewSenators_*.html'); perl -0777 -i -pe 's{(<td>Peter Welch</td>\s*<td>)D-VT}{${1}R-VT}' "$f"; resum "$f"; }
case_ "a New Senators entry of another party (Welch R)" "New Senators: Peter Welch is R, VT's page D" o04

# --- the appointments, the deaths
a01() { local f; f=$(pg 'wayback_senate_AppointedSenators_*.html'); sed -i 's|<span style="display:none">20240909</span>Sep 9, 2024|<span style="display:none">20240910</span>Sep 10, 2024|' "$f"; resum "$f"; }
a02() { local f; f=$(pg 'wayback_senate_AppointedSenators_*.html'); sed -i 's|<span style="display:none">20240823</span>Aug 23, 2024|<span style="display:none">20240912</span>Sep 12, 2024|' "$f"; resum "$f"; }
x01() { local f; f=$(pg 'wayback_senate_SenatorsDiedinOffice_*.html'); sed -i 's|<td>09/29/2023</td>|<td>09/28/2023</td>|' "$f"; resum "$f"; }
x02() { local f; f=$(pg 'wayback_senate_SenatorsDiedinOffice_*.html'); perl -0777 -i -pe 's{<tr valign="top">\s*<td><a href="[^"]*">Graham, Lindsey </a> \(SC\)</td>.*?</tr>}{}s' "$f"; resum "$f"; }
case_ "an appointee's oath off the New Senators page's (Helmy)" "Appointed Senators: George S. Helmy sworn 2024-09-10, the New Senators page 2024-09-09" a01
case_ "an appointment after its oath (Helmy)" "Appointed Senators: George S. Helmy appointed 2024-09-12 after his oath 2024-09-09" a02
case_ "a death a day off the state page's (Feinstein)" "Died in Office: Feinstein, Dianne 2023-09-28, CA's page ended 2023-09-29" x01
case_ "a death the Died page does not list (Lindsey Graham)" "SC Lindsey Graham: ended 'Died', not on the Died in Office page" x02
a03() { local f; f=$(pg 'wayback_senate_AppointedSenators_*.html'); perl -0777 -i -pe 's{<tr>\s*<td><span style="display:none">Helmy</span>.*?</tr>}{}s' "$f"; resum "$f"; }
case_ "an appointee by the New Senators page off the Appointed Senators page (Helmy)" "NJ George S. Helmy: appointed on the New Senators page, not on the Appointed Senators page" a03

# --- the early rows (s795): the state pages' days back to 3 Jan 2017
e01() { local f; f=$(pg 'wayback_senate_state_AZ_*.html'); perl -0777 -i -pe 's{(McSally</a>&nbsp;\(R\)</td><td data-label="TERM BEGAN">)Jan\. 3, 2019}{${1}Dec. 30, 2018}' "$f"; resum "$f"; }
e02() { local f; f=$(pg 'wayback_senate_AppointedSenators_*.html'); sed -i 's|<span style="display:none">20200101</span>Jan 1, 2020|<span style="display:none">20200107</span>Jan 7, 2020|' "$f"; resum "$f"; }
e03() { local f; f=$(pg 'wayback_senate_SenatorsDiedinOffice_*.html'); sed -i 's|<td>08/25/2018</td>|<td>08/24/2018</td>|' "$f"; resum "$f"; }
e04() { sed -i "s/^my %appointed_mark = .*/my %appointed_mark = ();/" Tools/us_senate_prep.pl; }   # TOOL: Smith's mark undeclared
e05() { sed -i "s/^        my %slip = .*/        my %slip = ();/" Tools/us_senate_prep.pl; }   # TOOL: the 117th note's slip undeclared
e06() { local f; f=$(pg 'wayback_senate_state_CA_*.html'); perl -0777 -i -pe 's{(Kamala D\. Harris(?:</a>)?(?:&nbsp;| ))\(D\)}{$1(I)}' "$f"; resum "$f"; }
e07() { local f=$R/wayback_senate_party_division_20260919.html; perl -0777 -i -pe 's{(117th Congress.*?Democrats )\((48) seats\)}{$1(49 seats)}s' $f; resum $f; }
case_ "an early holder beginning before his predecessor ended (McSally 30 Dec 2018)" "AZ class 3: Jon L. Kyl ended 2018-12-31, after Martha McSally began 2018-12-30" e01
case_ "an early appointee's row day outside his appointment and oath (Loeffler)" "Appointed Senators: Kelly Loeffler appointed 2020-01-07, sworn 2020-01-06; GA's page has him from 2020-01-06" e02
case_ "an early death a day off the state page's (McCain)" "Died in Office: McCain, John S., III 2018-08-24, AZ's page ended 2018-08-25" e03
case_ "TOOL: Smith's footnote mark undeclared" "Appointed Senators: Tina Smith (MN) appointed 'Jan 3, 2018 1', a footnote mark not declared" e04
case_ "TOOL: the 117th note's slip undeclared (Loeffler)" "[SEN-DIV] 117th note: R 51 D 46 I 2 vacant 1 on 2021-01-19" e05
case_ "an early row an independent (Harris I)" "CA Kamala D. Harris: an early row labelled I - the early rows are read for D and R alone" e06
case_ "[SEN-DIV]'s 117th line holding on no day (D 49)" "[SEN-DIV] 117: D 49 R 50 I 2 holds on no day from 2021-01-03 to 2023-01-02" e07

# --- the party changes and the independents
c01() { local f; f=$(pg 'wayback_senate_SenatorsWhoChangedPartiesDuringSenateService_*.html'); sed -i 's/On June 5, 2024, he switched/On June 6, 2024, he switched/' "$f"; resum "$f"; }
c02() { sed -i "/^    'AZ sinema'  => /d" Tools/us_senate_prep.pl; }   # TOOL: Sinema's change undeclared
c03() { local f; f=$(pg 'wayback_senate_SenatorsRepresentingThirdorMinorParties_*.html'); perl -0777 -i -pe 's{<tr[^>]*>(?:(?!</tr>).)*?Angus S\. King, Jr\.(?:(?!</tr>).)*?</tr>}{}s' "$f"; resum "$f"; }
case_ "Manchin's change a day off its words" "Changed Parties: the words for WV manchin not found" c01
case_ "TOOL: Sinema's change undeclared" "AZ Kyrsten Sinema: labelled D, I with no declared change@@Changed Parties: Kyrsten Sinema (AZ) changed party, not declared" c02
case_ "an independent off the Third or Minor Parties page (King)" "ME Angus S. King Jr.: an independent here, not on the Third or Minor Parties page" c03
c04() { local f; f=$(pg 'wayback_senate_SenatorsRepresentingThirdorMinorParties_*.html'); perl -0777 -i -pe 's{(<span style="font-weight:bold">)Angus S\. King, Jr\.(</span>) \(ME\)}{${1}Peter Welch$2 (VT)}' "$f"; resum "$f"; }
c05() { local f; f=$(pg 'wayback_senate_SenatorsRepresentingThirdorMinorParties_*.html'); perl -0777 -i -pe 's{(Sinema.*?2023)&#150;2025}{${1}&#150;2024}s' "$f"; resum "$f"; }
case_ "a Democrat named an independent on the Third or Minor Parties page (Welch)" "Third or Minor Parties: Peter Welch (VT) an independent in 2013-present, here D on 2023-01-03" c04
case_ "the Third or Minor Parties page ending an independent's span early (Sinema 2024)" "Third or Minor Parties: Kyrsten Sinema (AZ) independent to 2024, his service here ending 2025-01-03" c05

# --- the caucus
k01() { local f; for f in $S/wayback_democrats_senate_gov_members_*.html; do perl -0777 -i -pe 's{(<div class="MemberBox__state">)Vermont(</div>(?:(?!MemberBox__state).)*?Sanders)}{${1}Montana$2}s' "$f"; resum "$f"; done; }
k02() { local f; f=$(ls $S/wayback_democrats_senate_gov_members_2026*.html); perl -0777 -i -pe 's{(<div class="MemberBox__state">)Hawaii(</div>(?:(?!MemberBox__state).)*?Schatz)}{${1}Ohio$2}s' "$f"; resum "$f"; }
case_ "Sanders listed under another state on both lists" "VT Bernard Sanders: an independent of the window on no Democrats' list, his caucus unsourced" k01
case_ "a Democrat off the 2026 list (Schatz under Ohio)" "Democrats' list 2026-09-05: the Democrat Brian E. Schatz (HI) not on it@@Democrats' list 2026-09-05: Brian Schatz (OH schatz) holds no Democratic or independent seat that day" k02

# --- the anchors
n01() { local f=$R/wayback_senate_class_I_20260830.html; sed -i 's|Baldwin, Tammy</a> (D-WI)|Baldwin, Tammy</a> (R-WI)|' $f; resum $f; }
n02() { wrap 's/Democrats in roman \(48\)/Democrats in roman (47)/'; }
n03() { wrap 's/(Pete Ricketts.*\[1-8-23\]\s+)1-23-23/${1}1-24-23/'; }
n04() { wrap '$_ = "" if /^Tammy Baldwin \.{3,}/'; }
n05() { sed -i "s/^my %cdir_omits = .*/my %cdir_omits = ();/" Tools/us_senate_prep.pl; }   # TOOL: the Budd omission undeclared
n06() { wrap 's/compilation of the Congressional Directory was April 25, 2024/compilation of the Congressional Directory was April 26, 2024/'; }
case_ "the Class I page's party off the roster's (Baldwin R)" "Class I (2025-01-14): WI baldwin R on the page, the roster D" n01
case_ "the Directory's Senate count off the roster's (D 47)" "CDIR 2024-04-25 (2024-04-25): D 47 R 49 I 3 total 100, the roster D 48 R 49 I 3 vacant 0" n02
case_ "the Directory's oath off the New Senators page's (Ricketts)" "CDIR 2024-04-25: Pete Ricketts sworn 2023-01-24, the New Senators page 2023-01-23" n03
case_ "a senator gone from the Directory's class listing (Baldwin)" "CDIR 2024-04-25: class 1: the roster's [WI baldwin] not listed" n04
case_ "TOOL: the Directory's Budd omission undeclared" "CDIR 2024-04-25: class 3: the roster's [NC budd] not listed" n05
case_ "the Directory's closing date not the declared one" "CDIR 2024-04-25: closing date 2024-04-26, declared 2024-04-25" n06
n07() { wrap 's/^of April 25, 2024\. Democrats in roman/of April 24, 2024. Democrats in roman/'; }
n08() { wrap 's/ Died Laphonza/ Deceased Laphonza/'; }
case_ "the Directory's count as of another day than its closing" "CDIR 2024-04-25: its count is as of 2024-04-24, its closing day 2024-04-25" n07
case_ "the Directory's table: a change for another reason" "CDIR 2024-04-25: a change for the reason 'Deceased', not a resignation or a death" n08

# --- [SEN-DIV]
v01() { local f=$R/wayback_senate_party_division_20260919.html; perl -0777 -i -pe 's{(118th Congress.*?Democrats )\((47) seats\)}{$1(50 seats)}s' $f; resum $f; }
v02() { local f=$R/wayback_senate_party_division_20260919.html; perl -0777 -i -pe 's{With the resignation of JD Vance effective January 10, 2025}{With the resignation of JD Vance effective January 9, 2025}' $f; resum $f; }
case_ "[SEN-DIV]'s 118th line holding on no day (D 50)" "[SEN-DIV] 118: D 50 R 49 I 4 holds on no day from 2023-01-03 to 2025-01-02" v01
case_ "[SEN-DIV]'s note: Vance's resignation a day early" "[SEN-DIV] note: JD Vance resigned 2025-01-09 from class 3, the roster 2025-01-10, class 3" v02
v03() { local f=$R/wayback_senate_party_division_20260919.html; perl -0777 -i -pe 's{119th Congress \(2025}{119th Congresses (2025}' $f; resum $f; }
case_ "[SEN-DIV] without its 119th line" "[SEN-DIV]: lines for 117 118, not the 117th-119th" v03

# --- the copy's own tool
t01() { sed -i 's/^my \$reach = .*/my $reach = undef . "2026-09-06";/' Tools/us_senate_prep.pl; }   # TOOL: an undefined value
t02() { sed -i 's/^my \$csv_s = \$gen \. "# US-12: a row a senator/my $csv_s = $gen . "# US-12: a r\xC3\xB6w a senator/' Tools/us_senate_prep.pl; }   # TOOL
t03() { perl -i -pe 's/name => \$name =~ s\/,\/\/gr,/name => \$name,/; s/\$x->\{name\} =~ s\/,\/\/gr,/\$x->{name},/' Tools/us_senate_prep.pl; }   # TOOL: the commas left in the names
case_ "TOOL: a perl warning" "a perl warning: Use of uninitialized value in concatenation (.) or string" t01
case_ "TOOL: a non-ASCII byte in an output" "ElectionsData/usa/senate_seats.csv: not ASCII at its line" t02
case_ "TOOL: a comma left in a senator's name (Casey Jr.)" "has 12 fields, its heading 11 - nothing written" t03

echo "$caught caught, $bad missed or broken"
[ $bad -eq 0 ]
