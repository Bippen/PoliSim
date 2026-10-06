#!/usr/bin/perl
# THE HOUSE BY DISTRICT (PS-6 US-11, COMPLETED.md s790): every House race of the general elections of 2016, 2018, 2020, 2022 and 2024, read
# from the Clerk of the House's election statistics, and the maps in force each election, read from the Census Bureau's pages. US-5 read the
# same publications' national recapitulation (Tools/us_returns_prep.pl); this tool reads each state's listing "FOR UNITED STATES REPRESENTATIVE"
# and its "Recapitulation of Votes Cast in <State>" - the record is ElectionsData/usa/house_districts.md.
# It reads ONLY saved pages, each held to its SHA256SUMS.txt line first:
#   - raw/returns/clerk_statistics<year>.pdf, 2016-2024, twice: by pdftotext -raw (the text in reading order) and by pdftotext -table (each
#     entry's label and figure on one line, by where they stand on the page) - the second reading holds the first's pairing;
#   - raw/apportionment/census_apportionment_table01_<census>.pdf, 2010 and 2020 (the seats per state - us_returns_prep.pl reads the same
#     tables' workbooks);
#   - raw/executive/history_house_party_divisions.html (the House Historian's party divisions, [HH-DIV]);
#   - raw/maps/census_*.html (which states' lines changed for each election);
#   - raw/returns/fec_federalelections<year>.xlsx, 2016-2022 (the FEC's per-candidate House sheet: the winners, Alaska 2022's first choices and
#     last round, the ranked-choice footnotes);
# and ElectionsData/usa/house_by_state.csv (US-5's catalog), which the districts must sum to, state by state.
# It writes ElectionsData/usa/house_districts.csv, house_maps.csv, house_years.csv and Assets/Scripts/Elections/Generated/UsHouseDistricts.cs
# (a part of the partial class UsPresidentialReturns). It dies, writing nothing, on:
#   - a page off its digest, or in no SHA256SUMS.txt;
#   - a Table 1 not giving 435 Representatives over 50 states, or a 2020 change column not the two tables' difference;
#   - [HH-DIV] without a row for one of the 115th-119th Congresses, a row not summing to 435 with its footnoted vacancy, a footnote on a
#     Congress other than the two vacancies declared, or either vacancy's footnote not the one declared;
#   - a Census page without its sentence naming the states whose lines changed, a count word not the list's length, a state named twice or
#     unknown; the 2022 exceptions not exactly the states the 2020 census gives one seat; the geography page's state for the 117th not the
#     117th tab's; no note on Missouri's 2026 plan;
#   - (s799) an instrument's words not on its saved page, or an instrument or a plan declared not used for lines that did not change;
#   - (the Clerk) a page number out of sequence; a state not listed once FOR UNITED STATES REPRESENTATIVE; a state's districts not 1..n of its
#     apportionment (or AT LARGE for one seat); a district twice; an entry without a figure, a figure without an entry; a recapitulation row
#     whose cells do not sum to its Total, a district without a row, a row without a district; a district's listing not summing to its row's
#     Total once its footnote marks are read - a mark is a footnote number of the same state glued to a figure, the reading must be the only
#     one that closes the listing, and every figure must be grouped by thousands once read; a footnote number twice in a state; a mark on a
#     district's number other than the unprinted race's, whose footnote must say a new election was ordered; an unopposed seat beside another
#     entry, or its footnote not a no-opposition law's; a footnote glued to a figure of a kind not read (a runoff, a ranked round, a replaced
#     candidate) or marking anything but two finalists; a party line in a fusion state with no candidate before it or a label not declared; an
#     entry with no name outside a fusion state but the one declared (or a declared one unused); a label naming a major party in a form not
#     declared; a tie for first; the -table reading's labels and figures not the -raw reading's, state by state, in order;
#   - the winners not giving [HH-DIV] in every year, but for the two seats its footnotes leave unassigned (2018 NC-9, 2020 NY-22);
#   - the districts not summing, state by state, to house_by_state.csv's own-line Republican and Democratic votes and its total - each of the
#     250 state-years once;
#   - (the FEC) a sheet without its headings; a district's winner by party not the Clerk's but for the declared exception, which must itself
#     be what it says (no winner marked on the full term, the Clerk's on the unexpired term); a district whose winner's marks name two FEC ids;
#     North Carolina's uncertified 9th not '**' on every general candidate; Alaska 2022's general column not the Clerk's first choices, or its
#     rounds unread; a ranked-choice race declared without the FEC's footnote giving the general election's later rounds (2016-2022), those
#     rounds not the Clerk's figures where it prints the last, or such a footnote on a race not declared; an Alaska race declared outside
#     Alaska, a Maine one outside Maine, Maine 2024's Continuing Ballots line not its finalists' sum;
#   - a perl warning during the checks; a non-ASCII byte in an output.
# All outputs are built and tested before the first is written.
# Usage: perl Tools/us_house_prep.pl   (from the project root, Git for Windows' perl; pdftotext at /mingw64/bin or on PATH)
# ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw($UnzipError);

my $usa = 'ElectionsData/usa';
my $raw = "$usa/raw";
my @years = (2016, 2018, 2020, 2022, 2024);
my $pdftotext = -x '/mingw64/bin/pdftotext' ? '/mingw64/bin/pdftotext' : 'pdftotext';
my @problems;
sub problem { push @problems, join('', @_); return; }
$SIG{__WARN__} = sub { problem('a perl warning: ', $_[0] =~ s/\s+$//r) };   # an undefined value reaching a check is a mismatch, not a passing 0
$SIG{__DIE__} = sub { return if $^S; print STDERR "MISMATCH: $_\n" for splice @problems; };

# ---------------------------------------------------------------- the saved pages, each held to its SHA256SUMS.txt line before it is used
# (us_returns_prep.pl's gate, its lines 93-114, with the maps group)
my %sums;
for my $g (qw(returns apportionment executive maps)) {
    open my $s, '<', "$raw/$g/SHA256SUMS.txt" or die "$raw/$g/SHA256SUMS.txt: $!\n";
    while (my $l = <$s>) { $sums{"$g/$2"} = $1 if $l =~ /^([0-9a-f]{64}) \*(.+?)\s*$/; }
    close $s;
}
my %read;   # every page read, "raw/<group>/<file>" => SHA-256
sub page {
    my $rel = shift;
    my $f = "$raw/$rel";
    open my $h, '<:raw', $f or die "$f: $!\n";
    local $/;
    my $b = <$h>;
    close $h;
    my $d = sha256_hex($b);
    my $want = $sums{$rel} or die "$f: in no SHA256SUMS.txt - the tool reads saved pages only\n";
    die "$f: SHA-256 $d, its SHA256SUMS.txt holds $want\n" unless $d eq $want;
    $read{"raw/$rel"} = $d;
    return $b;
}
sub pdf_text {   # the page held to its digest, then its text by pdftotext in the mode given (-raw by default; lines end CRLF; a page's first line
                 # follows its form feed)
    my ($rel, $mode) = @_;
    page($rel);
    open my $p, '-|', $pdftotext, $mode // '-raw', "$raw/$rel", '-' or die "$pdftotext: $!\n";
    my $text = do { local $/; <$p> };
    close $p or die "$pdftotext $rel: exit " . ($? >> 8) . "\n";
    return $text;
}
sub html_text {   # a page's words: scripts and styles dropped, every tag a space, the entities this tool meets decoded, white space collapsed
    my $s = page(shift);
    $s =~ s/<script\b.*?<\/script>//gis;
    $s =~ s/<style\b.*?<\/style>//gis;
    $s =~ s/<[^>]*>/ /g;
    $s =~ s/&nbsp;|&#160;|\xC2\xA0/ /g;   # a no-break space, as an entity or as its UTF-8 bytes (the 117th's tab: "116th\xC2\xA0and")
    $s =~ s/&amp;/&/g;
    $s =~ s/\s+/ /g;
    return $s;
}

# ---------------------------------------------------------------- the FEC's workbooks: a sheet as { row => { col => value } }, rows and columns
# from 1 (us_returns_prep.pl's reader, its lines 120-149, without the formula map)
sub ent { my $s = shift; $s =~ s/&lt;/</g; $s =~ s/&gt;/>/g; $s =~ s/&quot;/"/g; $s =~ s/&apos;/'/g; $s =~ s/&#(\d+);/chr($1)/ge; $s =~ s/&#x([0-9a-fA-F]+);/chr(hex $1)/ge; $s =~ s/&amp;/&/g; return $s; }
sub colnum { my $c = shift; my $n = 0; $n = $n * 26 + (ord($_) - 64) for split //, $c; return $n; }
sub xlsx_sheet {
    my ($bytes, $want, $what) = @_;
    my %part;
    my $u = IO::Uncompress::Unzip->new(\$bytes) or die "$what: $UnzipError\n";
    for (my $st = 1; $st > 0; $st = $u->nextStream()) { my $n = $u->getHeaderInfo()->{Name}; my ($b, $c) = (''); while ($u->read($c) > 0) { $b .= $c; } $part{$n} = $b; }
    my @ss;
    if (my $x = $part{'xl/sharedStrings.xml'}) { utf8::decode($x); while ($x =~ m{<si>(.*?)</si>}gs) { my $si = $1; my $t = ''; $t .= $1 while $si =~ m{<t[^>]*>(.*?)</t>}gs; push @ss, ent($t); } }
    my $wb = $part{'xl/workbook.xml'} // die "$what: no workbook part\n";
    utf8::decode($wb);
    my $rels = $part{'xl/_rels/workbook.xml.rels'} // die "$what: no workbook relationships\n";
    my %tg;
    while ($rels =~ m{<Relationship\b([^>]*)/?>}g) { my $a = $1; my ($id) = $a =~ /Id="([^"]+)"/; my ($t) = $a =~ /Target="([^"]+)"/; $tg{$id} = $t; }
    my $path;
    while ($wb =~ m{<sheet\b([^>]*)/?>}g) { my $a = $1; my ($nm) = $a =~ /name="([^"]+)"/; my ($id) = $a =~ /r:id="([^"]+)"/; if (ent($nm) eq $want) { $path = $tg{$id}; last; } }
    die "$what: no sheet '$want'\n" unless $path;
    $path =~ s{^/?xl/}{};
    my $xml = $part{"xl/$path"} // die "$what: no part $path\n";
    utf8::decode($xml);
    my %cells;
    while ($xml =~ m{<c r="([A-Z]+)(\d+)"([^>]*?)(?:/>|>(.*?)</c>)}gs) {
        my ($col, $row, $attr, $body) = ($1, $2, $3, $4 // '');
        my $v = $body =~ m{<v>(.*?)</v>}s ? $1 : ($body =~ m{<is>.*?<t[^>]*>(.*?)</t>}s ? $1 : undef);
        next unless defined $v;
        $cells{$row}{colnum($col)} = $attr =~ /t="s"/ ? $ss[$v] : ent($v);
    }
    return \%cells;
}
sub trim { my $s = shift // ''; $s =~ s/^\s+|\s+$//g; return $s; }

# ---------------------------------------------------------------- the states (us_returns_prep.pl's table, its lines 272-281), DC left out
my %code = ('Alabama' => 'AL', 'Alaska' => 'AK', 'Arizona' => 'AZ', 'Arkansas' => 'AR', 'California' => 'CA', 'Colorado' => 'CO', 'Connecticut' => 'CT',
    'Delaware' => 'DE', 'Florida' => 'FL', 'Georgia' => 'GA', 'Hawaii' => 'HI', 'Idaho' => 'ID', 'Illinois' => 'IL',
    'Indiana' => 'IN', 'Iowa' => 'IA', 'Kansas' => 'KS', 'Kentucky' => 'KY', 'Louisiana' => 'LA', 'Maine' => 'ME', 'Maryland' => 'MD',
    'Massachusetts' => 'MA', 'Michigan' => 'MI', 'Minnesota' => 'MN', 'Mississippi' => 'MS', 'Missouri' => 'MO', 'Montana' => 'MT', 'Nebraska' => 'NE',
    'Nevada' => 'NV', 'New Hampshire' => 'NH', 'New Jersey' => 'NJ', 'New Mexico' => 'NM', 'New York' => 'NY', 'North Carolina' => 'NC',
    'North Dakota' => 'ND', 'Ohio' => 'OH', 'Oklahoma' => 'OK', 'Oregon' => 'OR', 'Pennsylvania' => 'PA', 'Rhode Island' => 'RI',
    'South Carolina' => 'SC', 'South Dakota' => 'SD', 'Tennessee' => 'TN', 'Texas' => 'TX', 'Utah' => 'UT', 'Vermont' => 'VT', 'Virginia' => 'VA',
    'Washington' => 'WA', 'West Virginia' => 'WV', 'Wisconsin' => 'WI', 'Wyoming' => 'WY');
my %name_of = reverse %code;
my @order = map { $code{$_} } sort keys %code;   # by name, as the Clerk lists them
my %upper = map { uc($_) => $code{$_} } keys %code;
my %territory = map { $_ => 1 } ('AMERICAN SAMOA', 'DISTRICT OF COLUMBIA', 'GUAM', 'NORTHERN MARIANA ISLANDS', 'PUERTO RICO', 'VIRGIN ISLANDS');

# ---------------------------------------------------------------- the apportionment: Representatives per state by each census's Table 1
my %reps;
for my $census (2010, 2020) {
    my $file = "apportionment/census_apportionment_table01_$census.pdf";
    my ($rows, $total) = (0, undef);
    for my $l (split /\r?\n/, pdf_text($file)) {
        if ($l =~ /^TOTAL\S*(?: APPORTIONMENT POPULATION\d?)? [\d,]+ (\d+)$/) { $total = $1; next; }
        next unless $l =~ /^([A-Z][a-z]+(?: [A-Z][a-z]+)*) [\d,]+ (\d+) ([+-]?\d+)$/;
        my ($name, $seats, $change) = ($1, $2, $3);
        my $st = $code{$name} or do { problem("$file: a row '$name', no state"); next; };
        problem("$file: $st twice") if exists $reps{$census}{$st};
        $reps{$census}{$st} = $seats;
        $reps{"$census change"}{$st} = $change;
        $rows++;
    }
    my $t = 0;
    $t += $_ for values %{$reps{$census}};
    problem("$file: $t Representatives over $rows states, not 435 over 50; its TOTAL row " . ($total // 'unread')) unless $t == 435 && $rows == 50 && ($total // 0) == 435;
}
for my $st (@order) { my $d = ($reps{2020}{$st} // 0) - ($reps{2010}{$st} // 0); problem("Table 1 2020: ${st}'s change $reps{'2020 change'}{$st}, the tables differ by $d") unless defined $reps{'2020 change'}{$st} && $reps{'2020 change'}{$st} == $d; }
sub census_of { my $year = shift; my ($c) = grep { $_ + 2 <= $year && $year < $_ + 12 } (2010, 2020); die "no apportionment governs $year\n" unless $c; return $c; }
sub seats { my ($year, $st) = @_; return $reps{census_of($year)}{$st}; }

# ---------------------------------------------------------------- [HH-DIV]: the House Historian's party divisions, the 115th-119th Congresses
# A row a Congress: the seats (a footnote mark after the figure where the page has one), the Democrats, the Republicans, the others.
my %hhdiv;   # {year} = { D, R, other, mark }: the Congress elected that year (the nth Congress is elected in 1786 + 2n)
{
    my $file = 'executive/history_house_party_divisions.html';
    my $h = page($file);
    for my $n (115 .. 119) {
        # the Congress's name as a cell's (or its link's) text - the 119th's row carries no link - then the row's cells
        my ($row) = $h =~ />\s*${n}th \(\d{4}\S{1,3}\d{4}\)\s*(<.*?)<\/tr>/s;
        if (!$row) { problem("$file: no row for the ${n}th Congress"); next; }
        my @c = map { trim($_) } grep { /\S/ } split /<[^>]*>/, $row;
        my ($seats, $mark, $d, $r, $o);
        if (@c >= 6 && $c[1] =~ /^\d$/ && $c[0] eq '435') { ($seats, $mark, $d, $r, $o) = @c[0 .. 4]; }   # a footnote mark: 435, n, D, R, other
        elsif (@c >= 5 && $c[0] eq '435') { ($seats, $d, $r, $o) = @c[0 .. 3]; }
        else { problem("$file: the ${n}th Congress's row reads '" . join('|', @c) . "'"); next; }
        problem("$file: the ${n}th Congress - $d D, $r R, $o other: not 435" . ($mark ? ' less one vacancy' : '')) unless $d + $r + $o == 435 - ($mark ? 1 : 0);
        $hhdiv{1786 + 2 * $n} = { D => $d, R => $r, other => $o, mark => $mark // '' };
    }
    # the two vacancies this item meets, by the page's own footnotes
    my %foot;
    $foot{$1} = $2 while $h =~ /<sup>(\d)<\/sup>(.*?)<\/p>/g;
    my %want = (2018 => qr/North Carolina did not submit an election certificate for the Ninth U\.S\. Congressional District prior to the opening day of the 116th Congress/,
                2020 => qr/New York did not submit an election certificate for the Twenty-Second U\.S\. Congressional District prior to the opening day of the 117th Congress/);
    for my $y (sort keys %want) {
        my $m = $hhdiv{$y} ? $hhdiv{$y}{mark} : '';
        problem("$file: the " . (($y - 1786) / 2) . "th Congress's footnote " . ($m || 'none') . " - not the vacancy this tool declares") unless $m && ($foot{$m} // '') =~ $want{$y};
    }
    for my $y (sort grep { !$want{$_} } keys %hhdiv) { problem("$file: the " . (($y - 1786) / 2) . "th Congress carries footnote $hhdiv{$y}{mark}, not declared here") if $hhdiv{$y}{mark}; }
}
my %vacant = (2018 => 'NC-9', 2020 => 'NY-22');   # [HH-DIV]'s footnotes 5 and 6, read above

# ---------------------------------------------------------------- the maps: which states' lines changed for each election
# The Census Bureau's Redistricting Data Program tabs (one per Congress) name the states that "delineated new boundaries"; the 117th's names the
# one state that changed, as the Bureau's geography page does; for 2022 that page names the exceptions to "all states". Each election's set,
# against the previous House election's lines.
my (%changed, %map_page);   # {year}{XX} = 1; {year} = the page that says so
{
    my %word = (one => 1, two => 2, three => 3, four => 4, five => 5, six => 6, seven => 7, eight => 8, nine => 9, ten => 10, eleven => 11, twelve => 12);
    my $names = sub {   # "Colorado, Minnesota, and Pennsylvania" -> codes; a trailing asterisk (the page's note mark) dropped
        my ($list, $what) = @_;
        my @n = map { s/\*$//r } split /, (?:and )?| and /, $list;
        my @c;
        for (@n) { my $st = $code{$_}; if (!$st) { problem("$what: '$_' is no state"); next; } problem("$what: $st named twice") if grep { $_ eq $st } @c; push @c, $st; }
        return @c;
    };
    for my $spec ([2016, 'maps/census_rdo_congressional_districts_115th.html'], [2018, 'maps/census_rdo_congressional_districts_116th.html'],
                  [2024, 'maps/census_rdo_congressional_districts_119th.html'], [2026, 'maps/census_rdo_congressional_districts_120th.html']) {
        my ($year, $file) = @$spec;
        my $t = html_text($file);
        my $prev = $year - 2;
        if ($t =~ /\b(\w+) states \(([^)]*)\) delineated new boundaries for the $year election cycle\. All other states, the District of Columbia, and Puerto Rico had no changes to their congressional district boundaries from the $prev cycle\./) {
            my ($w, $list) = (lc $1, $2);
            my @c = $names->($list, $file);
            problem("$file: '$w' states, " . scalar(@c) . " named") unless ($word{$w} // -1) == @c;
            $changed{$year}{$_} = 1 for @c;
            $map_page{$year} = $file;
        } else { problem("$file: no sentence naming the states that delineated new boundaries for the $year election cycle"); }
    }
    {
        my $file = 'maps/census_rdo_congressional_districts_117th.html';
        my $t = html_text($file);
        if ($t =~ /\b([A-Z][a-z]+(?: [A-Z][a-z]+)*) was the only state that had changes to their congressional districts between the 116th and 117th Congresses\./) {
            my @c = $names->($1, $file);
            $changed{2020}{$_} = 1 for @c;
            $map_page{2020} = $file;
        } else { problem("$file: no sentence naming the only state that changed between the 116th and 117th Congresses"); }
        page('maps/census_rdo_congressional_districts_118th.html');   # the 118th's tab names no state whose lines changed - 2022 is the geography page's
    }
    {
        my $file = 'maps/census_geography_congressional_districts.html';
        my $t = html_text($file);
        if ($t =~ /All states established new congressional districts in 2022, with the exception of the (\w+) single-member states \(([^)]*)\)\./) {
            my ($w, $list) = (lc $1, $2);
            my @ex = $names->($list, $file);
            problem("$file: '$w' single-member states, " . scalar(@ex) . " named") unless ($word{$w} // -1) == @ex;
            my @one = grep { seats(2022, $_) == 1 } @order;
            problem("$file: the exceptions " . join(' ', sort @ex) . " are not the states the 2020 census gives one seat, " . join(' ', sort @one)) unless join(' ', sort @ex) eq join(' ', sort @one);
            my %ex = map { $_ => 1 } @ex;
            $changed{2022}{$_} = 1 for grep { !$ex{$_} } @order;
            $map_page{2022} = $file;
        } else { problem("$file: no sentence on the states' new districts of 2022"); }
        # the same page on the 117th: its state must be the 117th tab's
        if ($t =~ /\b([A-Z][a-z]+(?: [A-Z][a-z]+)*) was the only state to make changes to their congressional districts for the 117 ?th congressional session\./) {
            my @c = $names->($1, $file);
            my $tab = join(' ', sort keys %{$changed{2020} // {}});
            problem("$file: names " . join(' ', @c) . " the only state to change for the 117th Congress, the 117th tab $tab") unless join(' ', @c) eq $tab;
        } else { problem("$file: no sentence naming the only state to change for the 117th Congress"); }
    }
}
my %missouri_note;   # the 120th tab's note on Missouri's plan, kept verbatim for the record (ASCII)
{
    my $t = html_text('maps/census_rdo_congressional_districts_120th.html');
    if ($t =~ /\*IMPORTANT NOTE: (On September 3, 2026, the Missouri Supreme Court ruled that the state's congressional district plan redrawn for the 2026 elections cannot be used[^.]*\.)/) { $missouri_note{text} = $1; }
    else { problem("maps/census_rdo_congressional_districts_120th.html: no note on Missouri's plan beside its asterisk"); }
}
# the instruments read for the redraws of 2020 and 2024 (named in the record; held here only to their digests)
page($_) for qw(maps/wayback_ncleg_SL2019-249_20200430.pdf maps/legis_la_SB8_2024_1ES.html maps/nyassembly_A09310_2023.html
                maps/alison_reapportionment_block_tract.html maps/ncsbe_uscongress_shapefiles_list.xml maps/supremecourt_docket_23a231.html
                maps/supremecourt_docket_23a1002.html);
# s799 (US-11's Design, R-US18): the instrument a redraw was made by, as reading 8 names it - printed only, never read by the rule - each held to
# words on its saved page; a change no saved page describes is '-'. Minnesota's 2016 and 2018 changes are the Census tabs' "cosmetic in nature".
my %instrument = (
    '2016 MN' => ['cosmetic (the 115th tab)', 'maps/census_rdo_congressional_districts_115th.html', 'cosmetic in nature'],
    '2018 MN' => ['cosmetic (the 116th tab)', 'maps/census_rdo_congressional_districts_116th.html', 'cosmetic in nature'],
    '2020 NC' => ['an act: Session Law 2019-249', 'maps/wayback_ncleg_SL2019-249_20200430.pdf', 'AN ACT TO REALIGN THE CONGRESSIONAL DISTRICTS'],
    '2024 AL' => ['a court-ordered plan (2023)', 'maps/alison_reapportionment_block_tract.html', '2023 Court Ordered Congressional Plan'],
    '2024 LA' => ['an act: Act No. 2 of the 2024 First Extraordinary Session', 'maps/legis_la_SB8_2024_1ES.html', 'Becomes Act No. 2'],
    '2024 NY' => ['an act: Chapter 92 of 2024', 'maps/nyassembly_A09310_2023.html', 'SIGNED CHAP.92'],
    '2024 NC' => ['an act: Session Law 2023-145 (its text BILLED)', 'maps/ncsbe_uscongress_shapefiles_list.xml', 'SL 2023-145'],
);
for my $k (sort keys %instrument) {
    my ($what, $rel, $words) = @{$instrument{$k}};
    my $t = $rel =~ /\.pdf$/ ? pdf_text($rel) : do { my $b = page($rel); $b };
    problem("the instrument of $k: '$words' not on $rel") unless index($t, $words) >= 0;
}
# s799: the lines IN FORCE - lines_changed, but where a saved page says a new plan was not used: Missouri 2026, by the 120th tab's note (the plan
# "cannot be used"; a referendum on the November ballot). The rule's held and redrawn read this column, and play's redrawn states with it.
my %not_used = ('2026 MO' => 1);
problem("2026 MO: declared not used, but the 120th tab carries no note on Missouri's plan") if $not_used{'2026 MO'} && !$missouri_note{text};
# both held here, before the mismatches are counted: an instrument or a plan declared not used names lines that changed
for my $k (sort keys %instrument) { my ($y, $s) = split / /, $k; problem("$k: an instrument declared for lines that did not change") unless $changed{$y}{$s}; }
for my $k (sort keys %not_used) { my ($y, $s) = split / /, $k; problem("$k: declared not used, but its lines did not change") unless $changed{$y}{$s}; }

# ---------------------------------------------------------------- the Clerk: each state's House listing and its recapitulation
# A candidate's party is the first of his labels that is Republican or Democratic. Every label naming a major party is declared here - a new
# form stops the run rather than falling to "other": Washington's 2018 "GOP" (the label its recapitulation heads a column of its own with) is
# Republican; the write-in candidates' "Write-in (Republican)" and "Write-in (Democratic)" are other, as US-5's columns count them.
my %major_label = ('Democrat' => 'D', 'Democratic' => 'D', 'Democratic-Farmer-Labor' => 'D', 'Democratic-Nonpartisan League' => 'D', 'Democratic-NPL' => 'D',
    'Republican' => 'R', 'Republican/Tax Revolt' => 'R', 'GOP' => 'R', 'Write-in (Republican)' => 'O', 'Write-in (Democratic)' => 'O');
sub party_of {
    my $l = shift // '';
    return $major_label{$l} if exists $major_label{$l};
    problem("the label '$l' names a major party in a form not declared") if $l =~ /republic|democrat|\bgop\b/i;
    return 'O';
}
# US-5's own line: the recapitulation's own Republican and Democratic columns - "GOP" stands in a column of its own there, so it is no own line
sub own_line_of { my $l = shift // ''; return '' if $l eq 'GOP'; my $p = party_of($l); return $p eq 'O' ? '' : $p; }
# lines that are not candidates: counted in the district's Total, never in a candidate's (the Clerk prints them among the entries)
my %noncand = map { lc($_) => 1 } ('Write-in', 'Write-ins', 'Scattering', 'Scatter', 'Blank Votes', 'Blank', 'Blanks', 'Void', 'All Others',
    'Other Write-ins', 'Over Votes', 'Overvotes', 'Under Votes', 'Undervotes', 'Miscellaneous', 'Others', 'Write-in (Other)', 'Write-in (No Party)',
    'Write-in (Miscellaneous)', 'Continuing Ballots', 'Exhausted Ballot', 'Exhausted Ballots', 'Spoiled Votes');
my %suffix = map { $_ => 1 } ('Jr.', 'Jr', 'Sr.', 'Sr', 'II', 'III', 'IV');
# a fusion state: an entry with no name is another ballot line of the candidate above it (New York and Connecticut; South Carolina in 2016) -
# its label one the volumes print, declared here; a comma entry is a line only when every part is one (a candidate's name has a comma too)
sub fusion_state { my ($year, $st) = @_; return $st eq 'NY' || $st eq 'CT' || ($st eq 'SC' && $year == 2016); }
my %fusion_label = map { $_ => 1 } ('Blue Lives Matter', 'Common Sense', 'Common Sense Suffolk', 'Conservative', 'Constitution', 'Green', 'Independence',
    'Independent', 'Libertarian', 'Medical Freedom', 'Moderate', 'Parent', 'Reform', 'Save America Movement', 'Save Our City', 'Serve America Movement',
    'Stop Iran Deal', 'Upstate Jobs', "Women's Equality", 'Working Families');
# outside the fusion states an entry with no name is a candidate the Clerk printed unnamed: declared one by one
my %unnamed_ok = ('2020 VA-2' => 'Independent');
my %unnamed_used;
# ranked choice, declared race by race and held below to what the pages say: Maine's 2nd district printed at the finalists' last round (C) -
# 2018 and 2022 by the FEC's footnotes, which give that round, 2024 beside a Continuing Ballots line equal to the finalists' sum; Alaska printed
# at its first choices (A) - 2022 by the FEC's footnote and rounds, 2024 by declaration alone (no saved page gives its rounds)
my %ranked = ('2018 ME-2' => 'C', '2022 ME-2' => 'C', '2024 ME-2' => 'C', '2022 AK-AL' => 'A', '2024 AK-AL' => 'A');
for my $k (sort keys %ranked) {
    my ($y, $st) = $k =~ /^(\d{4}) ([A-Z]{2})-/ or do { problem("$k: a declared ranked race this tool cannot name"); next; };
    problem("$k: declared '$ranked{$k}' - an Alaska race declared outside Alaska, or a Maine one outside Maine") unless ($ranked{$k} eq 'A' && $st eq 'AK') || ($ranked{$k} eq 'C' && $st eq 'ME');
}
sub valid_count { return shift =~ /^\d{1,3}(?:,\d{3})*$/; }
sub num { (my $s = shift) =~ s/,//g; return $s + 0; }
sub squash { (my $s = shift // '') =~ s/\s+/ /g; return trim($s); }

my %race;   # {year}{XX}{n} (0 = at large) = { cands => [ { party, total, own, own_party } ], noncand, total, winner, final => { R, D }, flags => {...}, ... }
my %footnotes;   # {year}{XX} = [ { n, text, line } ]
for my $year (@years) {
    my $file = "returns/clerk_statistics$year.pdf";
    my @L = split /\n/, pdf_text($file), -1;
    # lines with their page; a page's number is its last line that is not blank, when it reads n or (n) - and the numbers rise a page at a time
    my (@lines, @on_page);
    my $pg = 0;
    for my $i (0 .. $#L) {
        my $l = $L[$i];
        $l =~ s/\r$//;
        $pg++ while $l =~ s/^\f//;
        push @lines, { t => $l, pg => $pg, ln => $i + 1 };
        push @{$on_page[$pg]}, $#lines;
    }
    my @numbered;
    for my $p (0 .. $#on_page) {
        my @ix = grep { $lines[$_]{t} =~ /\S/ } @{$on_page[$p] // []};
        next unless @ix;
        if ($lines[$ix[-1]]{t} =~ /^\s*\(?(\d{1,3})\)?\s*$/) { $lines[$ix[-1]]{pagenum} = $1; push @numbered, [$p, $1]; }
    }
    for my $k (1 .. $#numbered) { problem("$file: the page numbered $numbered[$k][1] follows $numbered[$k - 1][1], " . ($numbered[$k][0] - $numbered[$k - 1][0]) . " page(s) on") unless $numbered[$k][1] - $numbered[$k - 1][1] == $numbered[$k][0] - $numbered[$k - 1][0]; }
    # the reading: states, their sections, the districts and their entries, the footnotes, the recapitulations
    my ($state, $sect, $cur, $in_hdr, $hdr, $has_total, $in_foot, $foot_page) = ('', '', undef, 0, '', 0, 0, -1);
    my (%dist, %recap, %house_sections, %listing);   # %listing: {XX} = the House listing's entries in the order printed, "label|figure"
    for my $Lr (@lines) {
        next if defined $Lr->{pagenum};
        my $t = $Lr->{t};
        $t =~ s/\s+$//;
        next if $t eq '';
        my ($ln, $lpg) = ($Lr->{ln}, $Lr->{pg});
        $in_foot = 0 if $lpg != $foot_page;
        # a footnote: its mark glued to its first word; it runs on over lines of prose (the raw text can carry more of the page after it)
        if ($t =~ /^(\d)([A-Z"].*)$/) { $in_foot = 1; $foot_page = $lpg; push @{$footnotes{$year}{$state}}, { n => $1, text => $2, line => $ln } if $state && $state !~ /^-/; next; }
        my $structural = ($t =~ /^([A-Z][A-Z. ]*[A-Z])(--Continued)?$/ && ($upper{$1} || $territory{$1})) || $t =~ /^FOR / || $t =~ /^Recapitulation / || $t =~ /^\(For /
            || $t =~ /^(?:\d )?\d+\. / || $t =~ /\.{2,}/ || $t =~ /^[\d,]+$/ || $t =~ /^(?:\d+(?:st|nd|rd|th|d) district|At large)\b/ || $t =~ /^Title of/
            || $t eq 'AT LARGE' || $t =~ /^Representa/;
        if ($in_foot && !$structural && ($t =~ /[a-z].* .* / || $t =~ /^[\d,]+\.$/)) { $footnotes{$year}{$state}[-1]{text} .= " $t" if $state && $state !~ /^-/ && $footnotes{$year}{$state}; next; }
        $in_foot = 0;
        if ($t =~ /^([A-Z][A-Z. ]*[A-Z])(--Continued)?$/ && ($upper{$1} || $territory{$1})) {
            my $st = $upper{$1} // "-$1";   # a territory: its lines are passed over
            if ($st ne $state) { $state = $st; $sect = ''; $cur = undef; }
            next;
        }
        if ($t =~ /^FOR (.+?)(?:--Continued)?$/) { $sect = $1; $cur = undef if $sect ne 'UNITED STATES REPRESENTATIVE'; $house_sections{$state}{$ln} = 1 if $1 eq 'UNITED STATES REPRESENTATIVE' && $t !~ /--Continued$/; next; }
        if ($t =~ /^Recapitulation of Votes Cast in (.+?)(?:--Continued)?$/) {
            my $rs = $1;
            problem("$file line $ln: a recapitulation of '$rs' inside $state") unless ($code{$rs} // '') eq $state || $state =~ /^-/;
            ($sect, $in_hdr, $hdr, $cur) = ('RECAP', 0, '', undef);
            next;
        }
        if ($t =~ /^\(For /) { problem("$file line $ln: '$t' among $state\'s House races - a House race for a part term, not read") if $sect eq 'UNITED STATES REPRESENTATIVE'; next; }
        next if $state eq '' || $state =~ /^-/;
        if ($sect eq 'UNITED STATES REPRESENTATIVE') {
            if ($t eq 'AT LARGE') { problem("$file $state: AT LARGE twice") if $dist{$state}{0}; $cur = $dist{$state}{0} = { n => 0, line => $ln, ent => [] }; next; }
            if ($t =~ /^(?:(\d) )?(\d+)\. (.*)$/) {
                my ($mark, $n, $rest) = ($1, $2, $3);
                problem("$file $state: district $n twice (line $ln)") if $dist{$state}{$n};
                $cur = $dist{$state}{$n} = { n => $n, line => $ln, ent => [], mark => $mark };
                if ($rest eq 'See explanation below') { $cur->{unprinted} = $ln; next; }
                $t = $rest;
            }
            if (!$cur) { problem("$file $state line $ln: '$t' before any district"); next; }
            if ($t =~ /^[\d,]+$/) {
                my ($e) = grep { !defined $_->{fig} && !$_->{unopposed} } @{$cur->{ent}};
                if (!$e) { problem("$file $state-$cur->{n} line $ln: the figure '$t' with no entry waiting for it"); next; }
                $e->{fig} = $t; $e->{figline} = $ln;
                next;
            }
            if ($t =~ /^(.*?) ?\.{2,} ?(.*)$/) {
                my ($label, $tail) = ($1, $2);
                my $e = { label => $label, line => $ln };
                if ($tail eq '') { }
                elsif ($tail =~ /^\((\d)\)$/) { $e->{unopposed} = $1; }
                elsif ($tail =~ /^[\d,]+$/) { $e->{fig} = $tail; $e->{figline} = $ln; }
                else { problem("$file $state-$cur->{n} line $ln: an entry ending '$tail'"); }
                my @parts = split /, /, $label;
                if ($noncand{lc $label}) { $e->{kind} = 'non'; }
                elsif (fusion_state($year, $state) && !grep { !$fusion_label{$_} } @parts) {
                    # another ballot line of the candidate above ("Conservative", "Blue Lives Matter", "Conservative, Common Sense"): its votes are his
                    my ($of) = grep { $_->{kind} eq 'cand' } reverse @{$cur->{ent}};
                    if (!$of) { problem("$file $state-$cur->{n} line $ln: the party line '$label' with no candidate before it"); $e->{kind} = 'non'; }
                    else { $e->{kind} = 'line'; $e->{of} = $of; }
                }
                elsif (@parts == 1) {
                    my $key = "$year $state-$cur->{n}";
                    if (fusion_state($year, $state)) { problem("$file $state-$cur->{n} line $ln: the party line '$label' - a label this tool does not declare"); $e->{kind} = 'non'; }
                    elsif (($unnamed_ok{$key} // '') eq $label) { $e->{kind} = 'cand'; $e->{name} = "(unnamed, $label)"; $e->{parties} = [$label]; $unnamed_used{$key} = 1; }
                    else { problem("$file $state-$cur->{n} line $ln: the entry '$label' - no name, outside a fusion state, not declared"); $e->{kind} = 'non'; }
                }
                else {
                    my @nm = (shift @parts);
                    push @nm, shift @parts while @parts > 1 && $suffix{$parts[0]};
                    $e->{kind} = 'cand'; $e->{name} = join(', ', @nm); $e->{parties} = [@parts];
                }
                push @{$cur->{ent}}, $e;
                next;
            }
            problem("$file $state-$cur->{n} line $ln: '$t' read as nothing");
            next;
        }
        if ($sect eq 'RECAP') {
            if ($t =~ /^Title of/) { ($in_hdr, $hdr) = (1, $t); next; }
            my $is_row = $t =~ /^(?:\d+(?:st|nd|rd|th|d) district|At large)\b/;
            if ($in_hdr && !$is_row) { $hdr .= " $t"; next; }
            if ($in_hdr && $is_row) { (my $h = $hdr) =~ s/\s*(?:Representative|Representa- tive)$//; $has_total = $h =~ /\bTotal$/ ? 1 : 0; $in_hdr = 0; }
            if ($t =~ /^(?:(\d+)(?:st|nd|rd|th|d) district|At large) ?(.*)$/) {
                my ($n, $rest) = ($1 // 0, $2);
                my @cells = split ' ', $rest;
                my $r = $recap{$state}{$n} //= { sum => 0, lines => [] };
                push @{$r->{lines}}, $ln;
                if ($has_total) { my $tot = pop @cells; problem("$file $state recap $n: a second Total (line $ln)") if defined $r->{total}; $r->{total} = $tot; }
                for (@cells) {
                    next if /^\.+$/;
                    if (/^\((\d)\)$/) { $r->{unopposed} = $1; next; }
                    if (valid_count($_)) { $r->{sum} += num($_); next; }
                    problem("$file $state recap $n line $ln: the cell '$_'");
                }
            }
            next;
        }
    }
    # the second reading: -table sets each entry's label and figure on one line, where they stand on the page; per state, the House listing's
    # entries in the order printed must be the first reading's, label for label and figure for figure (the first pairs them by order alone)
    {
        my (%tab, $tstate, $tsect) = ();
        for my $l (split /\n/, pdf_text($file, '-table')) {
            $l =~ s/\r$//;
            $l =~ s/^\f+//;
            my $t = trim($l);
            next if $t eq '';
            my $sq = squash($t);   # (-table spaces a title's words by where they stand: "Recapitulation       of  Votes Cast  in Arkansas")
            if ($sq =~ /^([A-Z][A-Z. ]*[A-Z])(--Continued)?$/ && ($upper{$1} || $territory{$1})) { $tstate = $upper{$1} // "-$1"; $tsect = ''; next; }
            if ($sq =~ /^FOR (.+?)(?:--Continued)?$/) { $tsect = $1; next; }
            if ($sq =~ /^Recapitulation of Votes Cast in /) { $tsect = 'RECAP'; next; }
            next unless $tstate && $tstate !~ /^-/ && ($tsect // '') eq 'UNITED STATES REPRESENTATIVE';
            next unless $t =~ /^(?:(?:\d\s+)?\d+\.\s+)?(.*?)\s*\.{2,}\s*(\S*)$/;
            push @{$tab{$tstate}}, squash($1) . '|' . $2;
        }
        for my $st (@order) {
            my @first;
            for my $n (sort { $dist{$st}{$a}{line} <=> $dist{$st}{$b}{line} } keys %{$dist{$st} // {}}) {
                push @first, map { squash($_->{label}) . '|' . ($_->{fig} // ($_->{unopposed} ? "($_->{unopposed})" : '')) } @{$dist{$st}{$n}{ent}};
            }
            my @second = @{$tab{$st} // []};
            next if join("\n", @first) eq join("\n", @second);
            my ($i) = grep { ($first[$_] // '') ne ($second[$_] // '') } 0 .. ($#first > $#second ? $#first : $#second);
            problem("$file $st: the -table reading differs from the -raw reading at its entry " . ($i + 1) . ": '" . ($second[$i] // 'none') . "' against '" . ($first[$i] // 'none') . "'");
        }
    }
    # ------------------------------------------------ each state's districts: complete, each held to its row, its marks read, its winner
    for my $st (@order) {
        my @hs = keys %{$house_sections{$st} // {}};
        problem("$file $st: listed FOR UNITED STATES REPRESENTATIVE " . scalar(@hs) . " time(s), not once") unless @hs == 1;
        my $n_seats = seats($year, $st);
        my @want = $n_seats == 1 ? (0) : (1 .. $n_seats);
        my @have = sort { $a <=> $b } keys %{$dist{$st} // {}};
        problem("$file $st: the districts " . join(',', @have) . ", the apportionment gives $n_seats") unless join(',', @have) eq join(',', @want);
        my @rows = sort { $a <=> $b } keys %{$recap{$st} // {}};
        problem("$file $st: recapitulation rows " . join(',', @rows) . ", districts " . join(',', @have)) unless join(',', @rows) eq join(',', @have);
        my %foot_of;
        for my $f (@{$footnotes{$year}{$st} // []}) {
            problem("$file $st: footnote $f->{n} twice (lines $foot_of{$f->{n}}{line} and $f->{line})") if $foot_of{$f->{n}};
            $foot_of{$f->{n}} = $f;
        }
        my $note = sub { my $m = shift; return defined $m && $foot_of{$m} ? $foot_of{$m}{text} : undef; };
        for my $n (@have) {
            my $D = $dist{$st}{$n};
            my $R = $recap{$st}{$n};
            my $key = "$st-" . ($n || 'AL');
            my $out = $race{$year}{$st}{$n} = { flags => {}, cands => [], noncand => 0, total => 0, final => { R => 0, D => 0 } };
            if (defined $D->{mark}) {
                # a mark on a district's number: only the race printed without votes, its footnote ordering a new election
                problem("$file $key: the district's number carries mark $D->{mark}, no footnote of $st") unless defined $note->($D->{mark});
                problem("$file $key: a mark on a printed district's number") unless $D->{unprinted};
                problem("$file $key: the mark on its number - '" . ($note->($D->{mark}) // '') . "' - does not order a new election") unless ($note->($D->{mark}) // '') =~ /ordered a new election/;
                $out->{note} = $note->($D->{mark});
            }
            if ($D->{unprinted}) {
                # no votes printed: the district's row must be empty, and its footnote say why
                problem("$file $key: no votes printed, but its row holds " . ($R ? ($R->{total} // 'no Total') : 'nothing')) unless $R && !$R->{sum} && ($R->{total} // '') =~ /^\.+$/;
                problem("$file $key: no votes printed and no footnote on its number") unless defined $D->{mark};
                $out->{flags}{N} = 1;   # no winner of record
                problem("$file $key: no winner, not the seat [HH-DIV] leaves vacant in $year") unless ($vacant{$year} // '') eq $key && $year == 2018;
                next;
            }
            my @ent = @{$D->{ent}};
            my @waiting = grep { !defined $_->{fig} && !$_->{unopposed} } @ent;
            problem("$file $key: " . join(' | ', map { $_->{label} } @waiting) . " without a figure") if @waiting;
            my @cands = grep { $_->{kind} eq 'cand' } @ent;
            my @unopp = grep { $_->{unopposed} } @ent;
            if (@unopp) {
                # an unopposed seat whose name was not printed on the ballot: its one name, no votes, the row's "(n)" the same footnote, a
                # no-opposition law
                problem("$file $key: an unopposed entry beside " . (@ent - 1) . " other(s)") unless @ent == 1 && @cands == 1;
                my $why = $note->($unopp[0]{unopposed});
                problem("$file $key: the unopposed mark ($unopp[0]{unopposed}) names no footnote of $st") unless defined $why;
                problem("$file $key: the unopposed mark's footnote - '" . ($why // '') . "' - is no no-opposition law") unless ($why // '') =~ /no opposition.*not printed on the ballot/;
                problem("$file $key: the row does not carry the unopposed mark") unless $R && ($R->{unopposed} // '') eq $unopp[0]{unopposed} && !$R->{sum};
                my $c = $cands[0] // $unopp[0];
                $c->{party} = (grep { $_ ne 'O' } map { party_of($_) } @{$c->{parties} // []})[0] // 'O';
                push @{$out->{cands}}, { party => $c->{party}, total => 0, own => 0, own_party => '' };
                $out->{winner} = $c->{party};
                $out->{n} = { R => $c->{party} eq 'R' ? 1 : 0, D => $c->{party} eq 'D' ? 1 : 0 };
                $out->{flags}{U} = 1;
                $out->{note} = $why;
                next;
            }
            # the figures: each grouped by thousands, or a footnote mark glued before a grouped figure
            my $sum = 0;
            for my $e (grep { defined $_->{fig} } @ent) { $e->{v} = num($e->{fig}); $sum += $e->{v}; }
            my $rt = $R ? $R->{total} : undef;
            if (!defined $rt || !valid_count($rt)) { problem("$file $key: its row's Total '" . ($rt // 'none') . "'"); next; }
            my $total = num($rt);
            problem("$file $key: its row's cells sum to $R->{sum}, its Total is $total (lines @{$R->{lines}})") unless $R->{sum} == $total;
            # the marks: a figure may be a footnote number of the state glued to a figure grouped by thousands; the reading must be the one
            # subset of such figures that closes the listing on the row's Total
            my @markable = grep { defined $_->{fig} && $_->{fig} =~ /^(\d)(\d{1,3}(?:,\d{3})*)$/ && defined $note->($1) } @ent;
            my @must = grep { defined $_->{fig} && !valid_count($_->{fig}) } @ent;
            for my $e (@must) { problem("$file $key line $e->{figline}: the figure '$e->{fig}' is grouped by no reading") unless grep { $_ == $e } @markable; }
            my @fits;
            for my $mask (0 .. (1 << @markable) - 1) {
                my @pick = map { $markable[$_] } grep { $mask & (1 << $_) } 0 .. $#markable;
                my %in = map { $_ => 1 } @pick;
                next if grep { !$in{$_} } @must;
                my $s = $sum;
                for my $e (@pick) { (my $rest = $e->{fig}) =~ s/^\d//; $s += num($rest) - $e->{v}; }
                push @fits, \@pick if $s == $total;
            }
            if (@fits != 1) { problem("$file $key: the listing sums to $sum, its row's Total is $total - " . scalar(@fits) . " reading(s) of its footnote marks close it"); next; }
            for my $e (@{$fits[0]}) { (my $rest = $e->{fig}) =~ s/^(\d)//; $e->{mark} = $1; $e->{v} = num($rest); $out->{flags}{M} = 1; }
            # the candidates: each his own line plus the party lines below it; his party the first of his labels that is R or D
            for my $c (@cands) {
                $c->{party} = (grep { $_ ne 'O' } map { party_of($_) } @{$c->{parties}})[0] // 'O';
                $c->{total} = $c->{v} // 0;
                $c->{own_party} = @{$c->{parties}} == 1 ? own_line_of($c->{parties}[0]) : '';   # US-5's own line: one label, its own column
            }
            for my $e (grep { $_->{kind} eq 'line' } @ent) { $e->{of}{total} += $e->{v} // 0; $e->{of}{lines}++; $out->{flags}{F} = 1; }
            $out->{noncand} += $_->{v} // 0 for grep { $_->{kind} eq 'non' } @ent;
            $out->{total} = $total;
            # the deciding count: where a footnote marks a runoff or a ranked-choice round, the finalists are the candidates its mark is glued to
            my @final = grep { defined $_->{mark} && ($note->($_->{mark}) // '') =~ /\b(?:runoff|round 2)\b/ } @cands;
            my %marked_note = map { $_->{mark} => $note->($_->{mark}) } grep { defined $_->{mark} } @ent;
            for my $m (sort keys %marked_note) {
                my $text = $marked_note{$m};
                my @on = grep { ($_->{mark} // '') eq $m } @ent;
                if ($text =~ /\b(?:runoff|round 2)\b/) {
                    problem("$file $key: footnote $m marks " . scalar(@on) . " figure(s), not two finalists") unless @on == 2 && !grep { $_->{kind} ne 'cand' } @on;
                    $out->{flags}{$text =~ /round 2/ ? 'C' : 'L'} = 1;
                    $out->{note} = $text;
                } elsif ($text =~ /^This candidate replaces /) { $out->{note} = $text; }
                else { problem("$file $key: footnote $m glued to a figure - '$text' - a kind this tool does not read"); }
            }
            my @pool = @final ? @final : @cands;
            my @rank = sort { $b->{total} <=> $a->{total} } @pool;
            if (!@rank) { problem("$file $key: no candidate"); next; }
            problem("$file $key: a tie for first, " . join(' and ', map { $_->{name} } @rank[0, 1])) if @rank > 1 && $rank[0]{total} == $rank[1]{total};
            $out->{winner} = $rank[0]{party};
            # the deciding count by party: the pool's votes (Louisiana's runoff districts: the two finalists alone)
            $out->{final}{$_->{party}} += $_->{total} for grep { $_->{party} ne 'O' } @pool;
            push @{$out->{cands}}, map { { party => $_->{party}, total => $_->{total}, own => ($_->{own_party} ne '' ? ($_->{v} // 0) : 0), own_party => $_->{own_party} } } @cands;
            # the fusion lines' weight: would the largest single ballot line, counted alone, belong to another candidate? (reported, record reading 3)
            my ($lmax) = sort { ($b->{v} // 0) <=> ($a->{v} // 0) } grep { $_->{kind} eq 'cand' || $_->{kind} eq 'line' } @ent;
            $out->{line_count_elsewhere} = 1 if $lmax && ($lmax->{kind} eq 'cand' ? $lmax : $lmax->{of}) != $rank[0] && $out->{flags}{F};
            # a same-party race: the deciding count's first two of one party (California's and Washington's top two, Louisiana's open
            # primary) - the other party may have stood, and lost, beside them
            $out->{flags}{S} = 1 if @rank > 1 && $rank[0]{party} ne 'O' && $rank[1]{party} eq $rank[0]{party};
            $out->{n} = { R => scalar(grep { $_->{party} eq 'R' } @cands), D => scalar(grep { $_->{party} eq 'D' } @cands) };
            # ranked choice, as the Clerk prints it - declared race by race, each held to what its page shows (and the FEC's, below)
            my $rk = $ranked{"$year $key"};
            my @tally = grep { $_->{kind} eq 'non' && $_->{label} =~ /^(?:Continuing|Exhausted) Ballots?$/ } @ent;
            if (!$rk) { problem("$file $key: a ranked-choice line (" . join(', ', map { $_->{label} } @tally) . ") in a race not declared ranked") if @tally; problem("$file $key: a round footnote in a race not declared ranked") if $out->{flags}{C}; }
            elsif ($rk eq 'C') {   # Maine's last round: two finalists, and the page (or the FEC's footnote, below) says it is a later round
                my @major = grep { $_->{party} ne 'O' } @cands;
                problem("$file $key: declared Maine's last round, but " . scalar(@major) . " Republican or Democratic candidates") unless @major == 2;
                my ($cont) = grep { $_->{label} eq 'Continuing Ballots' } @tally;
                if ($year == 2022) { problem("$file $key: declared round 2 by its footnote, which it lacks") unless $out->{flags}{C}; }
                elsif ($year == 2024) { problem("$file $key: its Continuing Ballots line is not the finalists' sum") unless $cont && @major == 2 && $cont->{v} == $major[0]{total} + $major[1]{total}; }
                elsif ($year == 2018) { problem("$file $key: declared its last round, " . scalar(@ent) . " entries printed, not the two finalists") unless @ent == 2; }
                else { problem("$file $key: declared Maine's last round in a year this tool has no rule for"); }
                $out->{flags}{C} = 1;
            }
            elsif ($rk eq 'A') { $out->{flags}{A} = 1; }   # Alaska's first choices - 2022 held to the FEC below
        }
    }
}
for my $k (sort keys %ranked) {
    my ($y, $st, $n) = $k =~ /^(\d{4}) ([A-Z]{2})-(\d+|AL)$/ or do { problem("$k: a declared ranked race this tool cannot name"); next; };
    $n = 0 if $n eq 'AL';
    problem("$k: declared ranked, no such race read") unless $race{$y} && $race{$y}{$st} && $race{$y}{$st}{$n};
}
for my $k (sort keys %unnamed_ok) { problem("$k: an unnamed candidate declared, no such entry read") unless $unnamed_used{$k}; }

# ---------------------------------------------------------------- the House of record: the winners against [HH-DIV]; the districts against US-5
for my $year (@years) {
    my %w;
    for my $st (@order) { for my $n (keys %{$race{$year}{$st} // {}}) { my $x = $race{$year}{$st}{$n}; $w{$x->{winner}}++ if $x->{winner}; } }
    my $h = $hhdiv{$year} or next;
    my ($r, $d, $o) = ($w{R} // 0, $w{D} // 0, $w{O} // 0);
    if ($year == 2020) {   # the Clerk prints NY-22's count (the Republican ahead); [HH-DIV] leaves the seat out (its footnote 6)
        my $ny22 = $race{2020}{NY}{22};
        problem("House 2020: NY-22's winner " . ($ny22->{winner} // 'none') . ", the seat [HH-DIV] leaves out") unless $ny22 && ($ny22->{winner} // '') eq 'R';
        $r--;
    }
    problem("House $year: the districts give R $r D $d other $o, [HH-DIV] R $h->{R} D $h->{D} other $h->{other}") unless $r == $h->{R} && $d == $h->{D} && $o == $h->{other};
}
{
    my $f = "$usa/house_by_state.csv";
    open my $h, '<', $f or die "$f: $!\n";
    my %seen;
    while (my $l = <$h>) {
        next if $l =~ /^#|^year,/;
        $l =~ s/\r?\n$//;
        my ($y, $st, $vr, $vd, $vo, $vt) = split /,/, $l;
        next unless $race{$y} && $race{$y}{$st};
        if ($seen{"$y $st"}++) { problem("$f: $y $st twice"); next; }
        my ($R, $D, $T) = (0, 0, 0);
        for my $x (values %{$race{$y}{$st}}) {
            $T += $x->{total};
            for my $c (@{$x->{cands}}) { $R += $c->{own} if $c->{own_party} eq 'R'; $D += $c->{own} if $c->{own_party} eq 'D'; }
        }
        problem("$y $st: the districts' own-line R $R, D $D, total $T; house_by_state.csv R $vr, D $vd, total $vt") unless $R == $vr && $D == $vd && $T == $vt;
    }
    for my $y (@years) { for my $st (@order) { problem("$f: no row for $y $st") unless $seen{"$y $st"}; } }
}

# ---------------------------------------------------------------- the FEC's per-candidate sheets, 2016-2022
# The FEC marks its general-election winner W (W* a runoff's winner) on every ballot line of his; his party is the R or D among those lines,
# all of them one FEC id. Its footnotes name the ranked-choice counts; Alaska's 2022 rows carry the rounds.
my %fec_exception = ('2022 IN-2' => 'the FEC marks the winner on the unexpired-term row only');   # (2018 NC-9: ** on every line, no W - no winner in both)
my %alaska_fec;   # 2022: { gen => { R, D }, round => { k => { R, D } }, last => the last round, final => its { R, D } }
my %nc9_fec;      # 2018 NC-9: the FEC's winner-column value => its rows (every general candidate's must be ** - not certified)
for my $spec ([2016, '2016 US House Results by State'], [2018, '2018 US House Results by State'], [2020, '13. US House Results by State'], [2022, '8. US House Results by State']) {
    my ($year, $sheet) = @$spec;
    my $file = "returns/fec_federalelections$year.xlsx";
    my $c = xlsx_sheet(page($file), $sheet, $file);
    my %col;
    for my $k (keys %{$c->{1}}) {
        my $h = trim($c->{1}{$k});
        $col{st} = $k if $h eq 'STATE ABBREVIATION';
        $col{cd} = $k if $h eq 'DISTRICT' || $h eq 'D';
        $col{party} = $k if $h eq 'PARTY';
        $col{w} = $k if $h eq 'GE WINNER INDICATOR';
        $col{gen} = $k if $h eq 'GENERAL VOTES';
        $col{id} = $k if $h =~ /^FEC ID#?$/;
        $col{fn} = $k if $h eq 'FOOTNOTES';
        $col{"rcv$1"} = $k if $h =~ /^(\d)(?:ST|ND|RD|TH) ROUND RCV VOTES$/;
    }
    if (grep { !defined $col{$_} } qw(st cd party w gen id fn)) { problem("$file '$sheet': no heading for " . join(', ', grep { !defined $col{$_} } qw(st cd party w gen id fn))); next; }
    my (%fw, %fwu, %wid, %rcv_note);   # "XX-n" => { R, D, O } (full term), the same on the unexpired term, the W rows' FEC ids, the ranked-choice footnote
    for my $r (sort { $a <=> $b } keys %$c) {
        next if $r == 1;
        my $row = $c->{$r};
        my $st = trim($row->{$col{st}});
        next unless $name_of{$st};
        my $cd = trim($row->{$col{cd}});
        my ($n) = $cd =~ /^(\d{1,2})\b/;
        my $unexpired = $cd =~ /UNEXPIRED/i;
        if (defined $n && !$unexpired) {
            my $fn = trim($row->{$col{fn}});
            $rcv_note{"$st-" . ($n + 0)} //= $fn if $fn =~ /ranked/i;
        }
        if ($year == 2022 && $st eq 'AK' && $cd eq '00') {   # Alaska's candidates: the general column by party, the rounds beside it
            my $p = trim($row->{$col{party}});
            my $party = $p eq 'R' ? 'R' : $p eq 'D' ? 'D' : '';
            my $g = trim($row->{$col{gen}});
            if ($party && $g =~ /^\d+$/) {   # (a candidate of the primary only has no general vote)
                $alaska_fec{gen}{$party} += $g;
                for my $k (1 .. 9) { my $v = defined $col{"rcv$k"} ? trim($row->{$col{"rcv$k"}}) : ''; $alaska_fec{round}{$k}{$party} += $v if $v =~ /^\d+$/; }
            }
        }
        my $w = trim($row->{$col{w}});
        if ($year == 2018 && $st eq 'NC' && $cd =~ /^0?9$/ && trim($row->{$col{party}}) ne '' && trim($row->{$col{gen}}) =~ /^\d+$/) {
            push @{$nc9_fec{$w}}, $r;   # the uncertified race: the FEC's mark on each of its general candidates
        }
        next unless $w =~ /^W\*?$/;
        if (!defined $n) { problem("$file row $r: a winner in district '$cd'"); next; }
        my $p = trim($row->{$col{party}});
        next if $p eq 'Combined Parties:';
        my ($major) = grep { /^(?:R|REP|D|DEM|DFL|DNL)$/ } split /[\/()*\s]+/, $p;
        my $key = "$st-" . ($n + 0);
        ($unexpired ? \%fwu : \%fw)->{$key}{defined $major ? ($major =~ /^R/ ? 'R' : 'D') : 'O'} = 1;
        $wid{$key}{trim($row->{$col{id}})} = 1 unless $unexpired;
    }
    my $fold = sub { my %f = %{shift // {}}; return $f{R} && $f{D} ? 'both R and D' : $f{R} ? 'R' : $f{D} ? 'D' : $f{O} ? 'O' : 'none'; };
    for my $st (@order) {
        for my $n (sort { $a <=> $b } keys %{$race{$year}{$st} // {}}) {
            my $key = "$st-$n";
            my $label = "$year $st-" . ($n || 0);
            my $cw = $race{$year}{$st}{$n}{winner} // 'none';
            # his minor lines carry the W too (a fusion winner): the winner's party is the R or D among them, other only when neither is
            my $fwin = $fold->($fw{$key});
            problem("$label: the FEC marks winners with " . scalar(keys %{$wid{$key}}) . " FEC ids (" . join(', ', sort keys %{$wid{$key}}) . ")") if keys(%{$wid{$key} // {}}) > 1;
            if (my $ex = $fec_exception{"$year $st-$n"}) {
                my $u = $fold->($fwu{$key});
                problem("$label: declared an exception ($ex), but the full term's mark is $fwin and the unexpired term's $u, the Clerk's winner $cw") unless $fwin eq 'none' && $u eq $cw;
                next;
            }
            problem("$label: the FEC's winner $fwin, the Clerk's $cw") unless $fwin eq $cw;
        }
    }
    delete $fw{$_} for map { my $st = $_; map { "$st-$_" } keys %{$race{$year}{$st} // {}} } @order;
    problem("$file: a winner in $_, a district the Clerk does not list") for sort keys %fw;
    # ranked choice: a race declared ranked must have the FEC's footnote giving the general election's later rounds - its figures the Clerk's
    # where the Clerk prints the last round (Maine) - and a footnote giving them must be on a declared race
    for my $key (sort keys %rcv_note) {
        my ($st, $n) = $key =~ /^([A-Z]{2})-(\d+)$/;
        my $rk = $ranked{"$year $st-" . ($n || 'AL')} // '';
        my $text = $rcv_note{$key};
        my $general = $text =~ /general election for Congressional District \d+ did not have majority winners|For the general election|Ranked Choice Voting for General Election/;
        if (!$general) { problem("$year $key: declared '$rk', but the FEC's ranked-choice footnote gives no later round of the general election") if $rk; next; }
        if (!$rk) { problem("$year $key: the FEC's footnote gives the general election's later rounds, the race is not declared ranked"); next; }
        next if $rk eq 'A';
        my $x = $race{$year}{$st}{$n} or next;
        my %v = map { num($_) => 1 } $text =~ /(\d{1,3}(?:,\d{3})+)/g;
        my %c;
        $c{$_->{party}} += $_->{total} for @{$x->{cands}};
        problem("$year $key: the FEC's footnote gives no later-round figure equal to the Clerk's R " . ($c{R} // 0) . " and D " . ($c{D} // 0)) unless $v{$c{R} // -1} && $v{$c{D} // -1};
    }
    for my $k (sort grep { /^$year / } keys %ranked) {
        my ($st, $n) = $k =~ /^\d{4} ([A-Z]{2})-(\d+|AL)$/;
        $n = 0 if $n eq 'AL';
        problem("$k: declared ranked, the FEC's sheet carries no ranked-choice footnote on it") unless $rcv_note{"$st-$n"};
    }
    if ($year == 2018) { problem("$file: North Carolina's 9th - the FEC's marks " . join(', ', map { "'$_' x" . scalar(@{$nc9_fec{$_}}) } sort keys %nc9_fec) . ", not ** on every general candidate") unless keys(%nc9_fec) == 1 && $nc9_fec{'**'} && @{$nc9_fec{'**'}} >= 2; }
    if ($year == 2022) {   # Alaska: the FEC's general column is the Clerk's first choices, party by party; its last round is the deciding count
        my %clerk;
        $clerk{$_->{party}} += $_->{total} for @{$race{2022}{AK}{0}{cands} // []};
        for my $p ('R', 'D') { problem("$file: Alaska's $p candidates' general vote " . ($alaska_fec{gen}{$p} // 'none') . ", the Clerk's first choices " . ($clerk{$p} // 'none')) unless ($alaska_fec{gen}{$p} // -1) == ($clerk{$p} // -2); }
        my ($last) = grep { $alaska_fec{round}{$_} } reverse 1 .. 9;
        if (!$last || !$alaska_fec{round}{$last}{R} || !$alaska_fec{round}{$last}{D}) { problem("$file: Alaska's ranked-choice rounds not read"); }
        else {
            $alaska_fec{last} = $last;
            $alaska_fec{final} = $alaska_fec{round}{$last};
            my $ak = $race{2022}{AK}{0};
            my $lead = $alaska_fec{final}{R} > $alaska_fec{final}{D} ? 'R' : 'D';
            problem("$file: Alaska's last round leads $lead, the Clerk's winner " . ($ak->{winner} // 'none')) unless ($ak->{winner} // '') eq $lead;
            $ak->{final} = { R => $alaska_fec{final}{R}, D => $alaska_fec{final}{D} };   # the deciding count: the FEC's last round (the Clerk prints no round)
        }
    }
}

# the deciding count names the winner in every race with one (the Clerk's figures, Louisiana's finalists, Alaska 2022's last round)
for my $year (@years) {
    for my $st (@order) {
        for my $n (sort { $a <=> $b } keys %{$race{$year}{$st} // {}}) {
            my $x = $race{$year}{$st}{$n};
            next unless $x->{winner} && $x->{winner} ne 'O' && !$x->{flags}{U};
            my ($r, $d) = ($x->{final}{R}, $x->{final}{D});
            my $lead = $r > $d ? 'R' : $d > $r ? 'D' : 'tie';
            problem("$year $st-$n: the deciding count R $r D $d leads $lead, the winner $x->{winner}") unless $lead eq $x->{winner};
        }
    }
}

$SIG{__WARN__} = 'DEFAULT';
if (@problems) { my $n = @problems; print STDERR "MISMATCH: $_\n" for splice @problems; die "$n mismatch(es) - nothing written\n"; }

# ---------------------------------------------------------------- the CSVs and the catalog's part: all built, and tested, before the first file opens
sub write_all {   # (us_returns_prep.pl's writer, its lines 915-927)
    my @out = @_;
    for (my $i = 0; $i < @out; $i += 2) {
        my ($f, $t) = @out[$i, $i + 1];
        if ($t =~ /[^\x00-\x7F]/) { my $line = 1 + (substr($t, 0, $-[0]) =~ tr/\n//); die "$f: not ASCII at its line $line - nothing written\n"; }
    }
    for (my $i = 0; $i < @out; $i += 2) {
        my ($f, $t) = @out[$i, $i + 1];
        open my $w, '>:raw', $f or die "$f: $!\n";
        print $w $t or die "$f: $!\n";
        close $w or die "$f: $!\n";
    }
}
my %flag_said = (A => "Alaska's ranked-choice count printed at its first choices (final_r / final_d: 2022 the FEC's last round, 2024 the first choices)",
    C => "Maine's ranked-choice count printed at the finalists' last round (2022: an eliminated candidate's first-round figure beside them)",
    F => 'fusion: a candidate on several ballot lines, his lines summed', L => "Louisiana's runoff: the finalists' December votes beside the others' November votes",
    M => 'a footnote mark read off a figure', N => 'no winner of record - no votes printed, the election not certified',
    S => "same party: the deciding count's first two of one party", U => 'unopposed, its name not printed on the ballot - no votes');
my $gen = "# GENERATED by Tools/us_house_prep.pl from the pages under ElectionsData/usa/raw/ - DO NOT EDIT; the record is ElectionsData/usa/house_districts.md.\n";
my $csv_d = $gen . "# US-11: a row a House race a year (the 435 of each general election), by the Clerk of the House's statistics - each state's listing\n"
    . "# FOR UNITED STATES REPRESENTATIVE, each race held to its row of the state's recapitulation. district: 0 at large. votes_r / votes_d /\n"
    . "# votes_other: the candidates' votes by party - a candidate's ballot lines summed (fusion), his party the first of his labels that is\n"
    . "# Republican or Democratic. votes_total: the row's Total, the lines that are not candidates (write-ins, blanks, Maine's ranked-choice\n"
    . "# tallies) among it. own_r / own_d: the candidates' own single Republican or Democratic line, US-5's basis (house_by_state.csv).\n"
    . "# cands_r / cands_d: how many of each the Clerk printed (none for a race printed without votes). final_r / final_d: the deciding count's\n"
    . "# Republican and Democratic votes - votes_r / votes_d, but in Louisiana's runoff districts the two finalists' December votes alone, and for\n"
    . "# Alaska 2022 the FEC's last ranked-choice round; 0 where no votes were printed. winner: R, D, O, or - for none. flags, a letter each (-\n"
    . "# for none):\n"
    . join('', map { "#   $_  $flag_said{$_}\n" } sort keys %flag_said)
    . "year,state,district,votes_r,votes_d,votes_other,votes_total,own_r,own_d,cands_r,cands_d,final_r,final_d,winner,flags\n";
my @drows;
for my $year (@years) {
    for my $st (@order) {
        for my $n (sort { $a <=> $b } keys %{$race{$year}{$st}}) {
            my $x = $race{$year}{$st}{$n};
            my %v = (R => 0, D => 0, O => 0);
            my %own = (R => 0, D => 0);
            for my $c (@{$x->{cands}}) { $v{$c->{party}} += $c->{total}; $own{$c->{own_party}} += $c->{own} if $c->{own_party}; }
            my $nr = $x->{n} ? $x->{n}{R} : 0;
            my $nd = $x->{n} ? $x->{n}{D} : 0;
            my $flags = join('', sort keys %{$x->{flags}});
            my @row = ($year, $st, $n, $v{R}, $v{D}, $v{O}, $x->{total}, $own{R}, $own{D}, $nr, $nd, $x->{final}{R}, $x->{final}{D}, $x->{winner} // '-', $flags eq '' ? '-' : $flags);
            push @drows, \@row;
            $csv_d .= join(',', @row) . "\n";
        }
    }
}
my @map_years = (2016, 2018, 2020, 2022, 2024, 2026);
my $csv_m = $gen . "# US-11: a row a state an election, 2016-2026 - the Representatives the apportionment in force gives it (the Census Bureau's Table 1), and\n"
    . "# whether its district lines changed since the previous House election (1) or not (0), as the Census Bureau's pages say - its Redistricting\n"
    . "# Data Program tabs, and for 2022 its geography page; source: that saved page, its path under ElectionsData/usa/. 2026: the plans as the\n"
    . "# Bureau collected them - Missouri's in doubt (the 120th tab's note, the record's reading 8). in_force (s799): lines_changed, but 0 where a\n"
    . "# saved page says the new plan was not used (Missouri 2026, the 120th tab's note) - the column R-US18's held and redrawn read. instrument\n"
    . "# (s799, printed only): what made the change, as reading 8 names it and held to words on its saved page; - where no saved page says.\n"
    . "year,state,seats,lines_changed,in_force,instrument,source\n";
my @mrows;
for my $year (@map_years) {
    for my $st (@order) {
        my $chg = $changed{$year}{$st} ? 1 : 0;
        my $ins = $instrument{"$year $st"};
        my @row = ($year, $st, seats($year, $st), $chg, ($chg && !$not_used{"$year $st"}) ? 1 : 0, $ins ? $ins->[0] : '-', "raw/$map_page{$year}");
        push @mrows, \@row; $csv_m .= join(',', @row) . "\n";
    }
}
my $csv_y = $gen . "# US-11: a row a House election - the Congress it elected and its party division as the House Historian's party-divisions page gives it\n"
    . "# ([HH-DIV]), its seats out of 435; vacant: the seat its footnote leaves unassigned (no certificate before the opening day), or -.\n"
    . "year,congress,seats_r,seats_d,seats_other,vacant\n";
my @yrows;
for my $year (@years) { my $h = $hhdiv{$year}; my @row = ($year, ($year - 1786) / 2, $h->{R}, $h->{D}, $h->{other}, $vacant{$year} // '-'); push @yrows, \@row; $csv_y .= join(',', @row) . "\n"; }
my ($dd, $dm, $dy) = map { sha256_hex($_) } ($csv_d, $csv_m, $csv_y);
my $cs = "// GENERATED by Tools/us_house_prep.pl. DO NOT EDIT BY HAND.\n//\n"
    . "// Sources: ElectionsData/usa/house_districts.csv, house_maps.csv and house_years.csv (their SHA-256 below), written by the same run from the\n"
    . "// saved pages listed in HouseRawSources. GeneratedCatalogCheck re-reads the CSVs and compares every figure, and re-hashes every page listed.\n\n"
    . "namespace PoliSim.Elections.Generated\n{\n"
    . "    // PS-6 US-11 (COMPLETED.md s790): the House of Representatives by district, 2016-2024, its maps and its record - the other part of the\n"
    . "    // partial class UsPresidentialReturns (UsPresidentialReturns.cs, written by Tools/us_returns_prep.pl). Generated, never hand-edited.\n"
    . "    public static partial class UsPresidentialReturns\n    {\n"
    . "        public const string HouseDistrictSourceDigest = \"$dd\";\n        public const string HouseMapSourceDigest = \"$dm\";\n"
    . "        public const string HouseYearSourceDigest = \"$dy\";\n\n"
    . "        /// <summary>Every saved page the House run read, by its path under `ElectionsData/usa/`, with its SHA-256.</summary>\n"
    . "        public static readonly (string Path, string Sha256)[] HouseRawSources =\n        {\n"
    . join('', map { "            (\"$_\", \"$read{$_}\"),\n" } sort keys %read) . "        };\n\n"
    . "        /// <summary>A row a House race a year, by the Clerk of the House's statistics: the candidates' votes by party (a candidate's ballot lines\n"
    . "        /// summed), the row's total, the own single Republican and Democratic lines (US-5's basis), how many of each the Clerk printed, the\n"
    . "        /// deciding count by party (Louisiana's runoff finalists alone; Alaska 2022 the FEC's last round), the winner (R, D, O, or a hyphen for\n"
    . "        /// none) and the flags (house_districts.csv's header says each). District 0 is at large. Built an election a method: the partial class\n"
    . "        /// has one static initializer, and one array literal of every race made it too large for Mono's JIT (a stack overflow on first use).</summary>\n"
    . "        public static readonly (int Year, string State, int District, long VotesR, long VotesD, long VotesOther, long VotesTotal, long OwnR, long OwnD, int CandsR, int CandsD, long FinalR, long FinalD, string Winner, string Flags)[] HouseDistricts =\n"
    . "            JoinHouseDistricts(" . join(', ', map { "HouseDistricts$_()" } @years) . ");\n\n"
    . join('', map { my $y = $_; "        private static (int Year, string State, int District, long VotesR, long VotesD, long VotesOther, long VotesTotal, long OwnR, long OwnD, int CandsR, int CandsD, long FinalR, long FinalD, string Winner, string Flags)[] HouseDistricts$y() => new (int, string, int, long, long, long, long, long, long, int, int, long, long, string, string)[]\n        {\n"
        . join('', map { "            ($_->[0], \"$_->[1]\", $_->[2], $_->[3]L, $_->[4]L, $_->[5]L, $_->[6]L, $_->[7]L, $_->[8]L, $_->[9], $_->[10], $_->[11]L, $_->[12]L, \"$_->[13]\", \"$_->[14]\"),\n" } grep { $_->[0] == $y } @drows) . "        };\n\n" } @years)
    . "        private static (int Year, string State, int District, long VotesR, long VotesD, long VotesOther, long VotesTotal, long OwnR, long OwnD, int CandsR, int CandsD, long FinalR, long FinalD, string Winner, string Flags)[] JoinHouseDistricts(params (int, string, int, long, long, long, long, long, long, int, int, long, long, string, string)[][] parts)\n"
    . "        {\n            int n = 0;\n            foreach (var p in parts) { n += p.Length; }\n"
    . "            var all = new (int Year, string State, int District, long VotesR, long VotesD, long VotesOther, long VotesTotal, long OwnR, long OwnD, int CandsR, int CandsD, long FinalR, long FinalD, string Winner, string Flags)[n];\n"
    . "            int at = 0;\n            foreach (var p in parts) { for (int i = 0; i < p.Length; i++) { all[at++] = p[i]; } }\n            return all;\n        }\n\n"
    . "        /// <summary>A row a state an election, 2016-2026: the seats the apportionment in force gives it, and whether its lines changed since the\n"
    . "        /// previous House election, as the Census Bureau's pages say (Source the saved page, under `ElectionsData/usa/`). 2026 is the plans as\n"
    . "        /// the Bureau collected them - Missouri's in doubt after a court ruling (house_districts.md, reading 8). InForce (s799): the lines in force,\n"
    . "        /// LinesChanged but where a saved page says the plan was not used (Missouri 2026); Instrument: what made the change, printed only.</summary>\n"
    . "        public static readonly (int Year, string State, int Seats, bool LinesChanged, bool InForce, string Instrument, string Source)[] HouseMaps =\n        {\n"
    . join('', map { "            ($_->[0], \"$_->[1]\", $_->[2], " . ($_->[3] ? 'true' : 'false') . ', ' . ($_->[4] ? 'true' : 'false') . ", \"$_->[5]\", \"$_->[6]\"),\n" } @mrows) . "        };\n\n"
    . "        /// <summary>A row a House election: the Congress it elected and its party division by the House Historian (its seats of 435), and the\n"
    . "        /// seat its footnote leaves unassigned - no certificate before the opening day (a hyphen for none).</summary>\n"
    . "        public static readonly (int Year, int Congress, int SeatsR, int SeatsD, int SeatsOther, string Vacant)[] HouseRecord =\n        {\n"
    . join('', map { "            ($_->[0], $_->[1], $_->[2], $_->[3], $_->[4], \"$_->[5]\"),\n" } @yrows) . "        };\n    }\n}\n";
$cs =~ s/\n/\r\n/g;
write_all("$usa/house_districts.csv" => $csv_d, "$usa/house_maps.csv" => $csv_m, "$usa/house_years.csv" => $csv_y,
          'Assets/Scripts/Elections/Generated/UsHouseDistricts.cs' => $cs);

# ---------------------------------------------------------------- the run's report (the record pastes it)
for my $year (@years) {
    my (%w, %fl);
    for my $st (@order) { for my $n (keys %{$race{$year}{$st}}) { my $x = $race{$year}{$st}{$n}; $w{$x->{winner} // '-'}++; $fl{$_}++ for keys %{$x->{flags}}; } }
    my $h = $hhdiv{$year};
    printf "House %d: the districts' winners R %d D %d (none %d) - [HH-DIV] R %d D %d%s; flags %s\n", $year, $w{R} // 0, $w{D} // 0, $w{'-'} // 0, $h->{R}, $h->{D},
        $vacant{$year} ? ", $vacant{$year} vacant" : '', join(' ', map { "$_ $fl{$_}" } sort keys %fl);
}
for my $year (@map_years) { my $n = keys %{$changed{$year}}; print "lines changed for $year ($n state" . ($n == 1 ? '' : 's') . ", $map_page{$year}): " . join(' ', sort keys %{$changed{$year}}) . "\n"; }
print "a fusion race whose largest single ballot line is another candidate's: "
    . join(', ', map { my $y = $_; map { my $st = $_; map { "$y $st-$_" } grep { $race{$y}{$st}{$_}{line_count_elsewhere} } sort { $a <=> $b } keys %{$race{$y}{$st}} } @order } @years) . "\n";
printf "Alaska 2022 by the FEC's workbook: its round %d R %d D %d (the Clerk prints the first choices, R %d D %d)\n", $alaska_fec{last}, $alaska_fec{final}{R}, $alaska_fec{final}{D},
    $alaska_fec{gen}{R}, $alaska_fec{gen}{D};
print "Missouri (the 120th's tab): $missouri_note{text}\n";
print "wrote $usa/house_districts.csv (" . scalar(@drows) . " races), house_maps.csv (" . scalar(@mrows) . " rows), house_years.csv, Assets/Scripts/Elections/Generated/UsHouseDistricts.cs\n";
