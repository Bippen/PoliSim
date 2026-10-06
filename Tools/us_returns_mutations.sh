#!/bin/bash
# THE US RETURNS GENERATOR'S FAILURE PATHS, PROVED (PS-6 US-3, COMPLETED.md s786; the House and 2024's candidates, US-5, s788). Each case mutates a fresh copy of Tools/us_returns_prep.pl's
# inputs - or, where it says TOOL, the copy's own tool; a case reaching a PDF's text rewrites the copy's extractor output instead (wrap), the
# input-cases' way to a page's text, said in their section - in a temporary directory, runs the tool there, and requires it to exit non-zero, write
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
  cp -r "$ROOT/ElectionsData/usa" "$T/ElectionsData/"; rm -f "$T"/ElectionsData/usa/president_by_*.csv "$T"/ElectionsData/usa/house_by_*.csv; cp "$ROOT/Tools/us_returns_prep.pl" "$T/Tools/"
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
  wrote=$(ls "$T"/ElectionsData/usa/president_by_*.csv "$T"/ElectionsData/usa/house_by_*.csv "$T"/Assets/Scripts/Elections/Generated/*.cs 2>/dev/null | wc -l)
  while IFS= read -r w; do grep -qF -- "$w" <<<"$out" || ok=0; done <<< "${2//@@/$'\n'}"
  if [ $rc -ne 0 ] && [ "$wrote" = 0 ] && [ $ok = 1 ]; then echo "CAUGHT  $1"; caught=$((caught+1))
  else echo "MISSED  $1 (exit $rc, $wrote file(s) written)"; tail -4 <<<"$out"; bad=$((bad+1)); fi
}

# --- the control
fresh
out=$(cd "$T" && perl Tools/us_returns_prep.pl 2>&1); rc=$?
same=$(cd "$T" && sha256sum ElectionsData/usa/president_by_*.csv ElectionsData/usa/house_by_*.csv Assets/Scripts/Elections/Generated/UsPresidentialReturns.cs | sed 's/ \*/  /' | sort)
mine=$(cd "$ROOT" && sha256sum ElectionsData/usa/president_by_*.csv ElectionsData/usa/house_by_*.csv Assets/Scripts/Elections/Generated/UsPresidentialReturns.cs | sed 's/ \*/  /' | sort)
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

# --- the House and 2024's candidates (US-5, s788); a Clerk's page is reached through its text, the extractor's output rewritten (wrap)
h01() { wrap 'BEGIN { undef $/ } s{^(Alabama \.+ 678,687 975,737 .*?) 5,471 1,659,895(\r?)$}{$1 5,472 1,659,895$2}m'; }   # the extractor's lines may end CRLF
h02() { wrap 'BEGIN { undef $/ } s{(United States Representatives, Election of November 8,) 2022}{$1 2021}'; }
h03() { wrap 'BEGIN { undef $/ } s{^Wyoming \.+ 132,206 47,250 [^\n]*\n}{}m'; }
h04() { wrap 'BEGIN { undef $/ } s{\fState Democratic Republican (Libertarian[^\n]*\n[^\f]*?Representatives, Election of November 3, 2020)}{\fState Republican Democratic $1}'; }   # a page's first line follows its form feed
h05() { wrap 'BEGIN { undef $/ } s{\fState Republican Democratic (Independent[^\n]*\n[^\f]*?Representatives, Election of November 5, 2024)}{\fState Democratic Republican $1}'; }
h06() { local f=ElectionsData/usa/raw/returns/fec_federalelections2022.xlsx; perl "$POKE" $f '<v>942393</v>' '<v>942394</v>'; resum $f; }
h07() { local f=ElectionsData/usa/raw/returns/fec_2024presgeresults.xlsx; perl "$POKE" $f '<v>862049</v>' '<v>862050</v>'; resum $f; }
h08() { sed -i 's/^| Republican | 49.75 (74,390,864) | 220 |/| Republican | 49.75 (74,390,865) | 220 |/' ElectionsData/usa/returns_2024.md; }
h09() { wrap 'BEGIN { undef $/ } s{(Recapitulation of Votes Cast for United States Representatives, Election of November 8, 2022)}{$1\n\f$1}'; }
case_ "a Clerk row's cell changed, its Total kept (2018, Alabama)" "clerk_statistics2018.pdf AL: its columns sum to 1659896, its Total is 1659895" h01
case_ "the Clerk's 2022 House page titled for another year" "not an election of 2022" h02
case_ "a state's row gone from the Clerk's 2022 House page" "clerk_statistics2022.pdf: no row for WY" h03
case_ "the Clerk's 2020 header naming Republican and Democratic in each other's place" "House 2020: the Clerk's and the FEC's tables agree exactly on 0 of 50 states" h04
case_ "the Clerk's 2024 header crossed (no FEC volume to answer to)" "returns_2024.md: the House's Republican typed 74390864, the Clerk's Total row 70571330" h05
case_ "an FEC House figure changed, its Total kept (2022, Alabama's Republicans)" "'6. Table 5 House by Party': the rows' column 6 sums to 54298206, the Total row holds 54298205" h06
case_ "a 2024 candidate's national figure changed (Stein)" "candidate column 'STEIN' sums to 862049 over the states, its Total-row figure is 862050@@the candidate columns' national figures sum to 155238303" h07
case_ "the typed 2024 House line off by one vote" "returns_2024.md: the House's Republican typed 74390865, the Clerk's Total row 74390864" h08
case_ "the Clerk's 2022 House title on two pages" "clerk_statistics2022.pdf: the House recapitulation's title on 2 page(s), not one" h09
# the review's first pass (s788): every refusal the readers added, proved - the Clerk's page through its text, the workbooks by poke, the md by edit
h10() { wrap 'BEGIN { undef $/ } s{^(Alabama \.+ )1,508,754( .*? )2,048,663(\r?)$}{${1}1,508,755${2}2,048,664$3}m'; }
h11() { wrap 'BEGIN { undef $/ } s{^(Total \.+ 54,227,992 [^\n]*\n)}{$1$1}m'; }
h12() { wrap 'BEGIN { undef $/ } s{^Total \.+ 54,227,992 [^\n]*\n}{}m'; }
h13() { wrap 'BEGIN { undef $/ } s{^(Wyoming \.+ 132,206 47,250 [^\n]*\n)}{$1$1}m'; }
h14() { wrap 'BEGIN { undef $/ } s{^Wyoming (\.+ 132,206 47,250 )}{Guam $1}m'; }
h15() { wrap 'BEGIN { undef $/ } s{\f(State Republican Democratic Independent Libertarian Green Constitution Other Parties1) Write-in (Total[^\n]*\n[^\f]*?Representatives, Election of November 5, 2024)}{\f$1 $2}'; }
h16() { wrap 'BEGIN { undef $/ } s{(\f(State Republican Democratic Libertarian[^\n]*\n))([^\f]*?Representatives, Election of November 8, 2022)}{$1$2$3}'; }
h17() { local f=ElectionsData/usa/raw/returns/fec_federalelections2022.xlsx; perl "$POKE" $f '<c r="A1" s="47" t="s"><v>11114</v>' '<c r="A1" s="47" t="s"><v>11113</v>'; resum $f; }
h18() { local f=ElectionsData/usa/raw/returns/fec_federalelections2022.xlsx; perl "$POKE" $f '<c r="E3" s="50" t="s"><v>3166</v></c><c r="F3"' '<c r="E3" s="50" t="s"><v>2811</v></c><c r="F3"'; resum $f; }
h19() { local f=ElectionsData/usa/raw/returns/fec_federalelections2022.xlsx; perl "$POKE" $f '<c r="F60" s="52"><v>54298205</v></c>' '<c r="F60" s="52"><f>SUM(F5:F59)</f><v>54298205</v></c>'; resum $f; }
h20() { local f=ElectionsData/usa/raw/returns/fec_federalelections2022.xlsx; perl "$POKE" $f '<c r="A6" s="49" t="s"><v>1184</v></c><c r="B6" s="35"><v>70295</v>' '<c r="A6" s="49" t="s"><v>1164</v></c><c r="B6" s="35"><v>70295</v>'; resum $f; }
h21() { local f=ElectionsData/usa/raw/returns/fec_federalelections2022.xlsx; perl "$POKE" $f '<c r="A60" s="49" t="s"><v>2796</v></c><c r="B60"' '<c r="A60" s="49" t="s"><v>552</v></c><c r="B60"'; resum $f; }
h22() { local f=ElectionsData/usa/raw/returns/fec_2024presgeresults.xlsx; perl "$POKE" $f '<c r="G53" s="49"><v>166175</v></c>' '<c r="G53" s="49"><f>SUM(G2:G52)</f><v>166175</v></c>'; resum $f; }
h23() { local f=ElectionsData/usa/raw/returns/fec_2024presgeresults.xlsx; perl "$POKE" $f '<c r="G1" s="2" t="s"><v>68</v></c>' '<c r="G1" s="2" t="inlineStr"><is><t>DE LA, CRUZ</t></is></c>'; resum $f; }
h24() { sed -i 's/^| Jill Stein (Green) | 0.56 (862,049) | 0 |/| Jill Stein (Green) | 0.56 (862,050) | 0 |/' ElectionsData/usa/returns_2024.md; }
h25() { sed -i '/^| Jill Stein (Green) |/d' ElectionsData/usa/returns_2024.md; }
h26() { perl -i -pe 's/^### National result \S+ House/### The House, nationally/' ElectionsData/usa/returns_2024.md; }
h27() { perl -i -pe 's/^### National result \S+ President/### The President, nationally/' ElectionsData/usa/returns_2024.md; }
h28() { wrap 'BEGIN { undef $/ } s{^(Delaware \.+ 209,606 287,830 [^\n]*\n)}{$1District of Columbia ...... 1,000 2,000 ..... ..... ..... ..... ..... ..... 3,000\n}m'; }
case_ "a state's Republicans and its Total raised together (2024, Alabama)" "clerk_statistics2024.pdf: the states' Republican sums to 74390865, the Total row holds 74390864" h10
case_ "the Clerk's 2022 Total row twice" "clerk_statistics2022.pdf: a second Total row" h11
case_ "the Clerk's 2022 Total row gone" "clerk_statistics2022.pdf: no Total row" h12
case_ "a state's row twice on the Clerk's 2022 House page" "clerk_statistics2022.pdf WY: a second row" h13
case_ "a row naming no state on the Clerk's 2022 House page" "clerk_statistics2022.pdf: a row 'Guam', no state" h14
case_ "the Clerk's 2024 header short of a column" "clerk_statistics2024.pdf: the header 'State Republican Democratic Independent Libertarian Green Constitution Other Parties1 Total' - not nine columns" h15
case_ "the Clerk's 2022 header line twice" "clerk_statistics2022.pdf: 2 header line(s) on the recapitulation's page, not one" h16
case_ "the FEC's 2022 House table titled as the Senate's" "'6. Table 5 House by Party': titled '2022 VOTES CAST FOR THE U.S. SENATE BY PARTY', not 2022's House vote by party" h17
case_ "the FEC's 2022 heading not GENERAL ELECTION over the columns read" "'6. Table 5 House by Party': no heading of GENERAL ELECTION over Democratic, Republican and Other in columns E-G" h18
case_ "a formula in the FEC's 2022 House Total row" "'6. Table 5 House by Party': the Total row's column 6 is a formula" h19
case_ "a state twice in the FEC's 2022 House table (Alaska read as Alabama)" "fec_federalelections2022.xlsx AL: a second row@@fec_federalelections2022.xlsx: no row for AK" h20
case_ "the FEC's 2022 House Total row unlabelled" "'6. Table 5 House by Party': no Total row" h21
case_ "a formula in a 2024 candidate's Total cell (De la Cruz)" "fec_2024presgeresults.xlsx: the Total row's column 7 is a formula" h22
case_ "a 2024 candidate's label with a comma" "the candidate column 'DE LA, CRUZ' holds a comma" h23
case_ "returns_2024.md's Stein off by one vote" "returns_2024.md: STEIN typed 862050, the FEC workbook's Total row 862049" h24
case_ "returns_2024.md's Stein row gone" "returns_2024.md: 2 presidential row(s) held, not the three it types" h25
case_ "returns_2024.md's House section renamed" "returns_2024.md: no 'National result - House' section" h26
case_ "returns_2024.md's President section renamed" "returns_2024.md: no 'National result - President' section" h27
case_ "a District of Columbia row on the Clerk's 2024 House page" "clerk_statistics2024.pdf: a row 'District of Columbia', no state" h28

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
