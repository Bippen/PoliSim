#!/bin/bash
# THE US HOUSE GENERATOR'S FAILURE PATHS, PROVED (PS-6 US-11, COMPLETED.md s790). Each case mutates a fresh copy of Tools/us_house_prep.pl's
# inputs - or, where it says TOOL, the copy's own tool; a case reaching a PDF's text rewrites the copy's extractor output instead (wrap) - in a
# temporary directory, runs the tool there, and requires it to exit non-zero, write nothing, and print the exact mismatch the case means (a
# mutation that lands anywhere else prints something else and is MISSED, never CAUGHT). The control, no mutation, must pass and reproduce the
# repository's own bytes. Nothing under the repository is written. Run it after any change to the tool or to the pages it reads.
# Usage: bash Tools/us_house_mutations.sh   (Git Bash; perl and pdftotext as the tool needs them). Exits 0 only when every case is caught.
set -u
ROOT=$(cd "$(dirname "$0")/.." && pwd)
POKE="$ROOT/Tools/xlsx_poke.pl"
PDFT=/mingw64/bin/pdftotext; [ -x "$PDFT" ] || PDFT=pdftotext
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT
T="$WORK/copy"
OUTS="ElectionsData/usa/house_districts.csv ElectionsData/usa/house_maps.csv ElectionsData/usa/house_years.csv Assets/Scripts/Elections/Generated/UsHouseDistricts.cs"
caught=0; bad=0

fresh() {
  rm -rf "$T"; mkdir -p "$T/ElectionsData" "$T/Tools" "$T/Assets/Scripts/Elections/Generated"
  cp -r "$ROOT/ElectionsData/usa" "$T/ElectionsData/"; rm -f "$T"/ElectionsData/usa/house_districts.csv "$T"/ElectionsData/usa/house_maps.csv "$T"/ElectionsData/usa/house_years.csv
  cp "$ROOT/Tools/us_house_prep.pl" "$T/Tools/"
}
resum() {   # re-pin a mutated page in its group's sums, so the mutation reaches the parser and not the digest check
  local f=$1 g b h; g=$(dirname "$f"); b=$(basename "$f"); h=$(sha256sum "$f" | cut -d' ' -f1)
  sed -i "s/^[0-9a-f]* \*$b\$/$h *$b/" "$g/SHA256SUMS.txt"
}
wrap() {   # TOOL: the copy's tool reads pdftotext's text through the perl -pe program $1 - in both of its readings, or only in the one $2 names
  # (raw or table: the tool runs pdftotext -raw and -table, and a case that means to break one reading against the other rewrites one)
  local only=${2:-both}
  printf '#!/bin/bash\nmode=raw; for a in "$@"; do [ "$a" = -table ] && mode=table; done\nif [ %q = both ] || [ %q = "$mode" ]; then %s "$@" | perl -pe %q; else %s "$@"; fi\n' \
    "$only" "$only" "$PDFT" "$1" "$PDFT" > "$T/pdft.sh"; chmod +x "$T/pdft.sh"
  sed -i "s#^my \$pdftotext = .*#my \$pdftotext = '$T/pdft.sh';#" Tools/us_house_prep.pl
}
case_() {   # $1 label, $2 the text(s) the run must print (several joined by @@), $3 the mutation (a function run inside the copy)
  fresh
  ( cd "$T" && $3 ) >/dev/null 2>&1
  local out rc wrote ok=1 w
  out=$(cd "$T" && perl Tools/us_house_prep.pl 2>&1); rc=$?
  wrote=$(cd "$T" && ls $OUTS 2>/dev/null | wc -l)
  while IFS= read -r w; do grep -qF -- "$w" <<<"$out" || ok=0; done <<< "${2//@@/$'\n'}"
  if [ $rc -ne 0 ] && [ "$wrote" = 0 ] && [ $ok = 1 ]; then echo "CAUGHT  $1"; caught=$((caught+1))
  else echo "MISSED  $1 (exit $rc, $wrote file(s) written)"; tail -4 <<<"$out"; bad=$((bad+1)); fi
}

# --- the control (fresh copies no Assets/, so the C# part exists in the copy only if the run writes it)
fresh
out=$(cd "$T" && perl Tools/us_house_prep.pl 2>&1); rc=$?
same=$(cd "$T" && sha256sum $OUTS | sed 's/ \*/  /' | sort)
mine=$(cd "$ROOT" && sha256sum $OUTS | sed 's/ \*/  /' | sort)
if [ $rc -eq 0 ] && [ "$same" = "$mine" ]; then echo "PASSES  the control: no mutation - exit 0, the repository's bytes"
else echo "BROKEN  the control (exit $rc)"; tail -4 <<<"$out"; bad=$((bad+1)); fi

M=ElectionsData/usa/raw/maps
# --- the pages and their sums
p01() { printf ' ' >> $M/census_rdo_congressional_districts_119th.html; }
p02() { sed -i '/census_rdo_congressional_districts_117th.html/d' $M/SHA256SUMS.txt; }
case_ "a map page changed by one byte" "census_rdo_congressional_districts_119th.html: SHA-256" p01
case_ "a map page its sums do not list" "census_rdo_congressional_districts_117th.html: in no SHA256SUMS.txt" p02

# --- the apportionment (Table 1, through the extractor's text)
a01() { wrap 's{^Alabama 4,802,982 7 0(\r?)$}{Alabama 4,802,982 8 0$1}'; }
a02() { wrap 's{^Texas 29,183,290 38 2(\r?)$}{Texas 29,183,290 38 1$1}'; }
case_ "a 2010 seat added (Alabama 8)" "436 Representatives over 50 states, not 435 over 50" a01
case_ "the 2020 change column off the tables' difference (Texas +1)" "Table 1 2020: TX's change 1, the tables differ by 2" a02

# --- [HH-DIV]
d01() { local f=ElectionsData/usa/raw/executive/history_house_party_divisions.html; perl -0777 -i -pe 's{(119th \(2025.{1,3}2027\)</td><td colspan="1" style="text-align: center;">435</td><td colspan="1" style="text-align: center;">)215}{${1}216}s' $f; resum $f; }
d02() { local f=ElectionsData/usa/raw/executive/history_house_party_divisions.html; sed -i 's/for the Twenty-Second U.S. Congressional District/for the Twenty-First U.S. Congressional District/' $f; resum $f; }
case_ "the 119th's Democrats raised by one" "the 119th Congress - 216 D, 220 R, 0 other: not 435" d01
case_ "the vacancy's footnote naming another district" "the 117th Congress's footnote 6 - not the vacancy this tool declares" d02

# --- the maps
m01() { local f=$M/census_rdo_congressional_districts_119th.html; sed -i 's/Five states (Alabama, Georgia, Louisiana, New York, and North Carolina)/Five states (Alabama, Georgia, Louisiana, and New York)/' $f; resum $f; }
m02() { local f=$M/census_rdo_congressional_districts_116th.html; sed -i 's/delineated new boundaries for the 2018 election cycle/drew new boundaries for the 2018 election cycle/' $f; resum $f; }
m03() { local f=$M/census_geography_congressional_districts.html; sed -i 's/South Dakota, Vermont, and Wyoming)/South Dakota, and Vermont)/; s/the six single-member states/the five single-member states/' $f; resum $f; }
m04() { local f=$M/census_rdo_congressional_districts_120th.html; sed -i 's/(Alabama, California, Florida, Louisiana, Missouri\*, North Carolina, Ohio, Tennessee, Texas, and Utah)/(Alabama, California, Florida, Louisiana, Missouri*, North Carolina, Ohio, Ohio, Texas, and Utah)/' $f; resum $f; }
m05() { local f=$M/census_rdo_congressional_districts_117th.html; perl -i -pe 's/North Carolina was the only state that had changes/North Carolina was the one state that had changes/' $f; resum $f; }
m06() { local f=$M/census_rdo_congressional_districts_120th.html; sed -i 's/\*IMPORTANT NOTE:/*NOTE:/g' $f; resum $f; }   # (a no-break space follows the colon on the page)
case_ "the 2024 list one state short of its count" "'five' states, 4 named" m01
case_ "the 2018 sentence gone" "no sentence naming the states that delineated new boundaries for the 2018 election cycle" m02
case_ "the 2022 exceptions not the one-seat states" "the exceptions AK DE ND SD VT are not the states the 2020 census gives one seat" m03
case_ "a state named twice for 2026" "OH named twice" m04
case_ "the 2020 sentence gone" "no sentence naming the only state that changed between the 116th and 117th Congresses" m05
case_ "Missouri's note gone" "no note on Missouri's plan beside its asterisk" m06

# --- the Clerk's listings (through the extractor's text; a case naming 'raw' rewrites the -raw reading alone, so the -table reading must catch it)
c01() { wrap 'BEGIN { undef $/ } s{(\n2\. Barry Moore, Republican [^\n]*\n(?:[^\n]*\n){3})137,460(\r?\n)}{${1}137,461$2}'; }
c02() { wrap 's{^(2d district \.+ )137,460( 58,014 )}{${1}137,461$2}'; }
c03() { wrap 'BEGIN { undef $/ } s{\n2\. (Barry Moore, Republican)}{\n$1}'; }
c04() { wrap 'BEGIN { undef $/ } s{(\n4\. Anthony P\. D.Esposito, Republican [^\n]*\n)Conservative( \.+\r?\n)}{${1}Pat Doe, Conservative$2}'; }
c05() { wrap 's{^Independent( \.+\r?)$}{Green$1} if $. > 4670 && $. < 4680' raw; }
c06() { wrap 's{^(2This vote count is from Louisiana.s December 10, 2016, general) \(runoff\) election}{$1 (second) election}' raw; }
c07() { wrap 's{^(1\. Jerry L\. Carl, Republican \.+)(\r?)$}{$1$2\nPat Doe, Democrat ......... (1)$2}' raw; }
c08() { wrap 'BEGIN { undef $/ } s{\n26(\r?\n)\fMAINE}{\n27$1\fMAINE}' raw; }
c09() { wrap 's{^FOR UNITED STATES REPRESENTATIVE(\r?)$}{FOR UNITED STATES REPRESENTATIVE$1\nFOR UNITED STATES REPRESENTATIVE$1} if $. < 30' raw; }
c10() { wrap 'BEGIN { undef $/ } s{(\n200,802\r?\n)25,483(\r?\n181,647\r?\n1,893\r?\n)19,984(\r?\n)}{${1}19,984${2}25,483$3}' raw; }
c11() { wrap 's{\b137,460\b}{97,737}g, s{\b58,014\b}{97,737}g if $. < 120'; }
c12() { wrap 's{^(3d district \.+ )220,621( 56,215 .* )282,443(\r?)$}{${1}420,621${2}482,443$3}' raw; }
c13() { wrap 's{^1(This vote count is from round 2 of Maine.s ranked-choice general election)}{3$1}' raw; }
c14() { wrap 's{^(1According to Florida law, the names of those with no opposition are not printed on the ballot\.)(\r?)$}{$1$2\n$1$2}' raw; }
c15() { wrap 's{ordered a new election in this Congressional District}{called a recount in this Congressional District}' raw; }
c16() { wrap 's{^1According to Florida law, the names of those with no opposition are not printed on the ballot\.}{1According to Florida law, these names are printed.}' raw; }
c17() { wrap 's{\b143,291\b}{143,091}g, s{\b138,898\b}{139,098}g'; }
case_ "a district's figure raised by one (2022 AL-2)" "clerk_statistics2022.pdf AL-2: the listing sums to 198962, its row's Total is 198961 - 0 reading(s)" c01
case_ "a recapitulation cell raised, its Total kept (2022 AL-2)" "clerk_statistics2022.pdf AL-2: its row's cells sum to 198962, its Total is 198961" c02
case_ "a district's number lost (2022 AL-2 runs into AL-1)" "clerk_statistics2022.pdf AL: the districts 1,3,4,5,6,7, the apportionment gives 7" c03
case_ "a fusion line given a name (2022 NY-4: its Conservative line a candidate)" "House 2022: the districts give R 221 D 214@@2022 NY-4: the FEC's winner R, the Clerk's D" c04
case_ "an unnamed entry outside the declared one (2020 VA-2)" "the entry 'Green' - no name, outside a fusion state, not declared" c05
case_ "a runoff footnote that no longer says runoff (2016 LA-3)" "clerk_statistics2016.pdf LA-3: footnote 2 glued to a figure" c06
case_ "an unopposed entry beside a candidate (2022 AL-1)" "an unopposed entry beside" c07
case_ "a page number out of sequence (2016)" "the page numbered 27 follows" c08
case_ "a state's House listed twice (the first state)" "AL: listed FOR UNITED STATES REPRESENTATIVE 2 time(s), not once" c09
case_ "two figures transposed in the -raw reading alone (2024 NY-1: Conservative and Blank)" "clerk_statistics2024.pdf NY: the -table reading differs from the -raw reading" c10
case_ "a tie for first (2022 AL-2, its listing and row)" "AL-2: a tie for first" c11
case_ "two readings of the footnote marks close the listing (2016 LA-3)" "LA-3: the listing sums to 682443, its row's Total is 482443 - 2 reading(s) of its footnote marks close it" c12
case_ "a figure no reading groups (2022 ME-2, its footnote renumbered)" "the figure '1146,142' is grouped by no reading" c13
case_ "a footnote number twice in a state (Florida)" "FL: footnote 1 twice" c14
case_ "the uncertified race's footnote ordering no new election (2018 NC-9)" "does not order a new election" c15
case_ "an unopposed seat's footnote no no-opposition law (Florida)" "is no no-opposition law" c16
case_ "the vacant seat's Republican made the loser (2020 NY-22)" "House 2020: NY-22's winner D, the seat [HH-DIV] leaves out" c17

# --- the checks against the other sources, and TOOL
x01() { sed -i 's/^2024,VT,0,/2024,VT,1,/' ElectionsData/usa/house_by_state.csv; }
x02() { sed -i '/^2018,WY,/d' ElectionsData/usa/house_by_state.csv; }
x03() { sed -i "s/^my %fec_exception = ('2022 IN-2' => [^)]*);/my %fec_exception = ();/" Tools/us_house_prep.pl; }
x04() { sed -i "s/'2024 ME-2' => 'C', //" Tools/us_house_prep.pl; }
x05() { sed -i "s/^my %unnamed_ok = ('2020 VA-2' => 'Independent');/my %unnamed_ok = ();/" Tools/us_house_prep.pl; }
x06() { local f=ElectionsData/usa/raw/returns/fec_federalelections2022.xlsx; perl "$POKE" $f '<c r="P59" s="35"><v>128553</v>' '<c r="P59" s="35"><v>128554</v>'; resum $f; }   # the House sheet's GENERAL VOTES cell (the figure is in the by-party table too)
x07() { local f=ElectionsData/usa/raw/returns/fec_federalelections2018.xlsx; perl "$POKE" $f '<c r="V2991" s="262" t="s"><v>10240</v>' '<c r="V2991" s="262" t="s"><v>5</v>'; resum $f; }   # the Democrat's general row (2991): its ** made the W (shared string 5)
x08() { local w=$(grep '^2018,WV,' ElectionsData/usa/house_by_state.csv); sed -i "s/^2018,WY,.*/$w/" ElectionsData/usa/house_by_state.csv; }
x09() { sed -i "s/'GOP' => 'R', //" Tools/us_house_prep.pl; }
x10() { sed -i "s/'Common Sense Suffolk', //" Tools/us_house_prep.pl; }
x11() { sed -i "s/'2022 AK-AL' => 'A', //" Tools/us_house_prep.pl; }
x12() { sed -i "s/^my %ranked = (/my %ranked = ('2020 ME-2' => 'C', /" Tools/us_house_prep.pl; }
x13() { sed -i "s/'2024 ME-2' => 'C'/'2024 ME-2' => 'A'/" Tools/us_house_prep.pl; }
x14() { sed -i "s/^my %unnamed_ok = ('2020 VA-2' => 'Independent');/my %unnamed_ok = ('2020 VA-2' => 'Independent', '2018 VA-3' => 'Independent');/" Tools/us_house_prep.pl; }
x15() { sed -i 's/^my %vacant = /{ my $u; my $w = $u + 1; }\nmy %vacant = /' Tools/us_house_prep.pl; }
x16() { perl -i -pe 's/a letter each/a lett\x{e9}r each/' Tools/us_house_prep.pl; }
x17() { local f=ElectionsData/usa/raw/returns/fec_federalelections2022.xlsx; perl "$POKE" $f '<c r="W1399" s="107"/>' '<c r="W1399" s="107" t="inlineStr"><is><t>W</t></is></c>'; resum $f; }   # a W on IN-2's full-term Democrat
x18() { local f=ElectionsData/usa/raw/returns/fec_federalelections2022.xlsx; perl "$POKE" $f '<v>2.1984029264355016E-2</v></c><c r="R29" s="37"/><c r="S29" s="105"/><c r="V29" s="106"/><c r="W29" s="107"/>' '<v>2.1984029264355016E-2</v></c><c r="R29" s="37"/><c r="S29" s="105"/><c r="V29" s="106"/><c r="W29" s="107" t="inlineStr"><is><t>W</t></is></c>'; resum $f; }   # a W on AL-4's Libertarian, beside the winner's (the empty cell is in two sheets; the row's own share pins this one)
x19() { local f=ElectionsData/usa/raw/returns/fec_federalelections2018.xlsx; perl "$POKE" $f '<c r="V1" s="278" t="s"><v>2018</v>' '<c r="V1" s="278" t="s"><v>32</v>'; resum $f; }   # the winner column's heading made the party's
case_ "US-5's Vermont 2024 Republican vote raised" "2024 VT: the districts' own-line R 0, D " x01
case_ "a state-year gone from US-5's table" "house_by_state.csv: no row for 2018 WY" x02
case_ "TOOL: the FEC's IN-2 exception withdrawn" "2022 IN-2: the FEC's winner none, the Clerk's R" x03
case_ "TOOL: Maine's 2024 ranked count undeclared" "ME-2: a ranked-choice line (Continuing Ballots, Exhausted Ballots) in a race not declared ranked" x04
case_ "TOOL: Virginia's unnamed candidate undeclared" "VA-2 line 4675: the entry 'Independent' - no name, outside a fusion state, not declared" x05
case_ "the FEC's Alaska 2022 general vote off by one" "Alaska's D candidates' general vote 128554, the Clerk's first choices 128553" x06
case_ "the FEC marking a winner in the uncertified race (2018 NC-9)" "North Carolina's 9th - the FEC's marks '**' x2, 'W' x1@@2018 NC-9: the FEC's winner D, the Clerk's none" x07
case_ "a state-year of US-5's table replaced by a second copy of another (2018 WY by WV)" "house_by_state.csv: 2018 WV twice@@house_by_state.csv: no row for 2018 WY" x08
case_ "TOOL: Washington's GOP label undeclared" "the label 'GOP' names a major party in a form not declared" x09
case_ "TOOL: a fusion label undeclared (Common Sense Suffolk)" "the party line 'Common Sense Suffolk' - a label this tool does not declare" x10
case_ "TOOL: Alaska's 2022 ranked count undeclared" "2022 AK-0: the FEC's footnote gives the general election's later rounds, the race is not declared ranked" x11
case_ "TOOL: a plurality race declared ranked (2020 ME-2)" "2020 ME-2: declared 'C', but the FEC's ranked-choice footnote gives no later round of the general election" x12
case_ "TOOL: a Maine race declared as Alaska's kind" "an Alaska race declared outside Alaska" x13
case_ "TOOL: an unnamed candidate declared that no race prints" "2018 VA-3: an unnamed candidate declared, no such entry read" x14
case_ "TOOL: a perl warning during the checks" "a perl warning: Use of uninitialized value" x15
case_ "TOOL: a non-ASCII byte in an output" "house_districts.csv: not ASCII" x16
case_ "the FEC marking IN-2's full-term Democrat the winner (the exception's reverse)" "2022 IN-2: declared an exception (the FEC marks the winner on the unexpired-term row only), but the full term's mark is D" x17
case_ "the FEC marking two candidates' rows in one district (2022 AL-4)" "2022 AL-4: the FEC marks winners with 2 FEC ids" x18
case_ "the FEC's 2018 winner column without its heading" "no heading for w" x19

# --- the pages: [HH-DIV] and the Census Bureau's geography page
d03() { local f=ElectionsData/usa/raw/executive/history_house_party_divisions.html; sed -i 's/115th (2017/105th (2017/g' $f; resum $f; }
d04() { local f=ElectionsData/usa/raw/executive/history_house_party_divisions.html; perl -0777 -i -pe 's{(>118th \(2023.{1,3}2025\)</a></td><td[^>]*>435)}{$1<sup>4</sup>}s' $f; resum $f; }   # (the 115th-119th rows carry no <center>)
m07() { local f=$M/census_geography_congressional_districts.html; sed -i 's/North Carolina was the only state to make changes/Ohio was the only state to make changes/' $f; resum $f; }
m08() { local f=$M/census_geography_congressional_districts.html; sed -i 's/All states established new congressional districts in 2022/All states drew new districts in 2022/' $f; resum $f; }
m09() { local f=$M/census_geography_congressional_districts.html; sed -i 's/the six single-member states/the seven single-member states/' $f; resum $f; }
case_ "the 115th Congress's row gone" "no row for the 115th Congress" d03
case_ "a footnote on a Congress not declared (the 118th)" "the 118th Congress carries footnote 4, not declared here" d04
case_ "the geography page naming another state for the 117th" "names OH the only state to change for the 117th Congress, the 117th tab NC" m07
case_ "the geography page's 2022 sentence gone" "no sentence on the states' new districts of 2022" m08
case_ "the 2022 exceptions' count word not their number" "'seven' single-member states, 6 named" m09

echo "--- $caught caught, $bad missed or broken"
[ $bad -eq 0 ]
