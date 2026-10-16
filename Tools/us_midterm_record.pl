#!/usr/bin/perl
# THE MIDTERM RECORD (PS-6 US-13; R-US20's rule is the fenced block in docs/specs/USA_STAGE_PLAN.md): the president's party in the House at every
# midterm from 1946 to 2022 and in every presidential year from 1948 to 2024 - its seat change by [HH-DIV], and its two-party vote swing where the
# Clerk of the House's statistics of both elections carry text. UsMidtermCheck reads what this writes and prints the model's midterm beside it.
# It reads ONLY saved pages, each held to its SHA256SUMS.txt line first:
#   - raw/executive/history_house_party_divisions.html ([HH-DIV], the House Historian's party divisions as of each Congress's initial election
#     results): the 79th-119th Congresses, the ones the elections of 1944-2024 chose;
#   - NARA's Electoral College results pages, 1944-2024 (raw/midterms/ 1944-2008, raw/returns/ 2012 and 2016, raw/executive/ 2020 and 2024):
#     each election's winner and his party, so the president's party at each House election (the last presidential election before it);
#   - the Clerk's election statistics, 1998-2024 (raw/midterms/ 1998-2014, raw/returns/ 2016-2024), by pdftotext -raw: the "Recapitulation of
#     Votes Cast for United States Representatives", a row a state, its Total row (the statistics of 1944-1996 are page images with no text
#     layer - raw/midterms/fetch_log.txt - so their vote is BILLED);
#   - raw/midterms/vitalstats_table_2-2.csv: a SECONDARY compilation (Vital Statistics on Congress, Table 2-2), printed beside, never read by the
#     rule;
# and ElectionsData/usa/house_by_state.csv (US-5's catalog), which the Clerk's rows of 2016-2024 must equal state by state.
# It writes ElectionsData/usa/house_midterms.csv and docs/generated/US_MIDTERM_RECORD.md. It dies, writing nothing, on:
#   - a page off its digest, or in no SHA256SUMS.txt;
#   - [HH-DIV] without a row for one of the 79th-119th Congresses; a row whose parties and declared vacancies do not sum to its seats; a footnote
#     mark on a Congress other than the four declared, or a declared mark whose footnote is not the one declared;
#   - a NARA page titled for another year, without its winner and main opponent each with a party, the two of one party, the winner short of the
#     majority it prints, or a party other than D or R;
#   - (the Clerk) the recapitulation's title on no page or naming another year; its header not opening on the two major parties; a state row
#     whose cells do not close on its Total under exactly one reading of its leader and footnote marks; a state twice, a state missing, a row
#     that names no state; a column of the states not summing to the Total row, the Total row not closing; 2016-2024 not house_by_state.csv's
#     figures, state by state;
#   - the secondary's heading not the one read, a year missing or twice, a direction other than D or R;
#   - a largest party tied in a House the record reads; a perl warning during the checks; a non-ASCII byte in an output.
# All outputs are built and tested before the first is written.
# Usage: perl Tools/us_midterm_record.pl   (from the project root, Git for Windows' perl; pdftotext at /mingw64/bin or on PATH)
# ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);

my $usa = 'ElectionsData/usa';
my $raw = "$usa/raw";
my $pdftotext = -x '/mingw64/bin/pdftotext' ? '/mingw64/bin/pdftotext' : 'pdftotext';
my @problems;
sub problem { push @problems, join('', @_); return; }
$SIG{__WARN__} = sub { problem('a perl warning: ', $_[0] =~ s/\s+$//r) };
$SIG{__DIE__} = sub { return if $^S; print STDERR "MISMATCH: $_\n" for splice @problems; };

# ---------------------------------------------------------------- the saved pages, each held to its SHA256SUMS.txt line before it is used
# (us_house_prep.pl's gate, with this item's group)
my %sums;
for my $g (qw(returns executive midterms)) {
    open my $s, '<', "$raw/$g/SHA256SUMS.txt" or die "$raw/$g/SHA256SUMS.txt: $!\n";
    while (my $l = <$s>) { $sums{"$g/$2"} = $1 if $l =~ /^([0-9a-f]{64}) \*(.+?)\s*$/; }
    close $s;
}
my %read;   # every page read, "<group>/<file>" => SHA-256
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
    $read{$rel} = $d;
    return $b;
}
sub html_text {   # a page's words: scripts and styles dropped, every tag a space, the entities this tool meets decoded, white space collapsed
    my $s = page(shift);
    $s =~ s/<script\b.*?<\/script>//gis;
    $s =~ s/<style\b.*?<\/style>//gis;
    $s =~ s/<[^>]*>/ /g;
    $s =~ s/&nbsp;|&#160;|\xC2\xA0/ /g;
    $s =~ s/&amp;/&/g;
    $s =~ s/\s+/ /g;
    return $s;
}
sub trim { my $s = shift // ''; $s =~ s/^\s+|\s+$//g; return $s; }
sub num { (my $s = shift) =~ s/,//g; return $s + 0; }

my %code = ('Alabama' => 'AL', 'Alaska' => 'AK', 'Arizona' => 'AZ', 'Arkansas' => 'AR', 'California' => 'CA', 'Colorado' => 'CO', 'Connecticut' => 'CT',
    'Delaware' => 'DE', 'Florida' => 'FL', 'Georgia' => 'GA', 'Hawaii' => 'HI', 'Idaho' => 'ID', 'Illinois' => 'IL',
    'Indiana' => 'IN', 'Iowa' => 'IA', 'Kansas' => 'KS', 'Kentucky' => 'KY', 'Louisiana' => 'LA', 'Maine' => 'ME', 'Maryland' => 'MD',
    'Massachusetts' => 'MA', 'Michigan' => 'MI', 'Minnesota' => 'MN', 'Mississippi' => 'MS', 'Missouri' => 'MO', 'Montana' => 'MT', 'Nebraska' => 'NE',
    'Nevada' => 'NV', 'New Hampshire' => 'NH', 'New Jersey' => 'NJ', 'New Mexico' => 'NM', 'New York' => 'NY', 'North Carolina' => 'NC',
    'North Dakota' => 'ND', 'Ohio' => 'OH', 'Oklahoma' => 'OK', 'Oregon' => 'OR', 'Pennsylvania' => 'PA', 'Rhode Island' => 'RI',
    'South Carolina' => 'SC', 'South Dakota' => 'SD', 'Tennessee' => 'TN', 'Texas' => 'TX', 'Utah' => 'UT', 'Vermont' => 'VT', 'Virginia' => 'VA',
    'Washington' => 'WA', 'West Virginia' => 'WV', 'Wisconsin' => 'WI', 'Wyoming' => 'WY');

my @house_years = map { 1944 + 2 * $_ } 0 .. 40;           # the House elections the record reads, 1944-2024
my @pres_years = grep { $_ % 4 == 0 } @house_years;        # the presidential elections, 1944-2024
my @clerk_years = grep { $_ >= 1998 } @house_years;        # the Clerk's statistics with a text layer
sub congress_of { return (shift() - 1786) / 2; }            # the nth Congress is chosen at the election of 1786 + 2n

# ---------------------------------------------------------------- [HH-DIV]: the 79th-119th Congresses
# A row a Congress: the seats (a footnote mark after the figure where the page has one), the Democrats, the Republicans, the others (each party's
# count in brackets, or 0), the delegates and resident commissioners.
my %hh;   # {year} = { D, R, other, vacant, seats, mark }
{
    my $file = 'executive/history_house_party_divisions.html';
    my $h = page($file);
    # the footnotes: each mark's LAST paragraph (the first "<sup>1</sup>" is a mark inside the 40th Congress's row, its paragraph the table)
    my %foot;
    while ($h =~ /<sup>(\d)<\/sup>(.*?)<\/p>/gs) { my ($n, $t) = ($1, $2); $t =~ s/<[^>]*>//g; $t =~ s/\s+/ /g; $foot{$n} = trim($t); }
    # the marks this record meets, each with its footnote's words and the vacancies the row's total holds by them
    my %declared = (86 => [3, qr/^Alaska entered Congress at the beginning of the 86th Congress/, 0],
                    99 => [4, qr/^This total includes one vacancy\.$/, 1],
                    116 => [5, qr/North Carolina did not submit an election certificate for the Ninth U\.S\. Congressional District prior to the opening day of the 116th Congress/, 1],
                    117 => [6, qr/New York did not submit an election certificate for the Twenty-Second U\.S\. Congressional District prior to the opening day of the 117th Congress/, 1]);
    for my $n (79 .. 119) {
        my ($row) = $h =~ />\s*${n}(?:st|nd|rd|th) \(\d{4}\S{1,3}\d{4}\)\s*(<.*?)<\/tr>/s;
        if (!$row) { problem("$file: no row for the ${n}th Congress"); next; }
        $row =~ s/&nbsp;|&#160;|\xC2\xA0/ /g;   # a no-break space, as an entity or as its UTF-8 bytes (the 113th's first cell)
        my @c = map { trim($_) } grep { /\S/ } split /<[^>]*>/, $row;
        my $mark = '';
        if (@c >= 5 && $c[0] =~ /^\d{3}$/ && $c[1] =~ /^\d$/ && $c[2] =~ /^\d+$/ && $c[3] =~ /^\d+$/) { $mark = splice(@c, 1, 1); }
        unless (@c >= 4 && $c[0] =~ /^\d{3}$/ && $c[1] =~ /^\d+$/ && $c[2] =~ /^\d+$/ && $c[-1] =~ /^\d+\/\d+$/) { problem("$file: the ${n}th Congress's row reads '" . join('|', @c) . "'"); next; }
        my ($seats, $d, $r) = @c[0 .. 2];
        my @oc = @c[3 .. $#c - 1];
        my $other = 0;
        for my $cell (@oc) {
            if ($cell eq '0') { next; }
            my @k = $cell =~ /\((\d+)\)/g;
            problem("$file: the ${n}th Congress's other cell '$cell' carries no count") unless @k;
            $other += $_ for @k;
        }
        my $vacant = 0;
        if ($mark ne '' || $declared{$n}) {
            my $want = $declared{$n};
            if (!$want) { problem("$file: the ${n}th Congress carries footnote $mark, not declared here"); next; }
            problem("$file: the ${n}th Congress's footnote " . ($mark || 'none') . ", not the $want->[0] declared, or its words not the ones declared")
                unless $mark eq $want->[0] && ($foot{$mark} // '') =~ $want->[1];
            $vacant = $want->[2];
        }
        problem("$file: the ${n}th Congress - $d D, $r R, $other other, $vacant vacant: not its $seats seats") unless $d + $r + $other + $vacant == $seats;
        $hh{1786 + 2 * $n} = { D => $d, R => $r, other => $other, vacant => $vacant, seats => $seats, mark => $mark };
    }
}

# ---------------------------------------------------------------- NARA: each presidential election's winner and his party
my %nara_file = map { $_ => "midterms/archives_electoral_college_$_.html" } grep { $_ <= 2008 } @pres_years;
$nara_file{2012} = 'returns/archives_electoral_college_2012.html';
$nara_file{2016} = 'returns/archives_electoral_college_2016.html';
$nara_file{2020} = 'executive/archives_electoral_college_2020.html';
$nara_file{2024} = 'executive/archives_electoral_college_2024.html';
my %won;   # {year} = { name, party, opponent, opponent_party, ev, ev_opp, majority }
for my $y (@pres_years) {
    my $file = $nara_file{$y};
    my $t = html_text($file);
    my ($title) = $t =~ /(\d{4}) Electoral College Results \| National Archives/;
    problem("$file: titled for " . ($title // 'no year') . ", not $y") unless ($title // 0) == $y;
    if ($t =~ /\bPresident (\S.{1,60}?) \[([A-Z]+)\] Main Opponent (\S.{1,60}?) \[([A-Z]+)\] Electoral Vote\*? Winner: (\d+)\*? Main Opponent: (\d+)\*? Total\/Majority: (\d+)\/(\d+)/) {
        my %w = (name => $1, party => $2, opponent => $3, opponent_party => $4, ev => $5, ev_opp => $6, total => $7, majority => $8);
        problem("$file: the winner's party '$w{party}' or the opponent's '$w{opponent_party}' is not D or R") unless $w{party} =~ /^[DR]$/ && $w{opponent_party} =~ /^[DR]$/;
        problem("$file: the winner and the main opponent are both of $w{party}") if $w{party} eq $w{opponent_party};
        problem("$file: the winner's $w{ev} electoral votes short of the majority $w{majority}") unless $w{ev} >= $w{majority} && $w{ev} > $w{ev_opp};
        $won{$y} = \%w;
    } else { problem("$file: no President, Main Opponent and Electoral Vote lines read"); }
}
# the president's party at a House election: the party of the winner of the last presidential election before it
sub pres_from { my $y = shift; return $y % 4 == 0 ? $y - 4 : $y - 2; }

# ---------------------------------------------------------------- the Clerk: the national recapitulation, 1998-2024
my %clerk;   # {year} = { R, D, total, states => { XX => { R, D, total } } }
# Where the Clerk's own Total row and the sum of its state rows disagree in a column of neither major party: {year} = { column (from 1) => the
# Total row less the states' sum }. Each is the page's own difference, read off it and printed in the record; never the Republican, the
# Democratic or the Total column, which must agree exactly.
my %col_gap = (2002 => { 4 => -2668, 10 => 2668 },    # the Libertarian and the first Other Parties column: the same votes counted in a different column
               2006 => { 11 => -1202, 14 => -1202 });  # the first Other Parties column and the Total: the states hold 1,202 votes the Total row does not
for my $y (@clerk_years) {
    my $file = $y <= 2014 ? "midterms/clerk_statistics$y.pdf" : "returns/clerk_statistics$y.pdf";
    page($file);
    open my $p, '-|', $pdftotext, '-raw', "$raw/$file", '-' or die "$pdftotext: $!\n";
    my $text = do { local $/; <$p> };
    close $p or die "$pdftotext $file: exit " . ($? >> 8) . "\n";
    $text =~ s/\r//g;
    my @pages = split /\f/, $text;
    my $title_re = qr/^Recapitulation of Votes Cast for United States Representatives, Election of (.+?)(?:--Continued)?\s*$/m;
    my @hit = grep { $pages[$_] =~ $title_re } 0 .. $#pages;
    if (!@hit) { problem("$file: the House recapitulation's title on no page"); next; }
    for my $i (@hit) { my ($when) = $pages[$i] =~ $title_re; problem("$file: the recapitulation's title names '$when', not an election of $y") unless $when =~ /\b$y$/; }
    # the header: from its "State" line to the line that ends "Total", a word broken at a line's end joined again
    my @lines = split /\n/, $pages[$hit[0]];
    my ($at) = grep { $lines[$_] =~ /^State / } 0 .. $#lines;
    if (!defined $at) { problem("$file: no header on the recapitulation's first page"); next; }
    my $hdr = '';
    for my $k ($at .. $#lines) {
        my $l = trim($lines[$k]);
        $hdr = $hdr eq '' ? $l : ($hdr =~ /-$/ ? substr($hdr, 0, -1) . $l : "$hdr $l");
        last if $hdr =~ /\bTotal$/;
    }
    my ($first, $second) = $hdr =~ /^State (\S+) (\S+)/;
    my $order = ($first // '') eq 'Republican' && ($second // '') eq 'Democratic' ? 'RD' : ($first // '') eq 'Democratic' && ($second // '') eq 'Republican' ? 'DR' : '';
    if (!$order) { problem("$file: the header '$hdr' does not open on the Republican and Democratic columns"); next; }
    # the Total row first: its cells give the columns
    my @total;
    for my $i (@hit) {
        for my $l (split /\n/, $pages[$i]) {
            next unless $l =~ /^Total (.*)$/;
            my @tok = split ' ', $1;
            shift @tok if @tok && $tok[0] =~ /^\.+$/;
            next unless @tok && !grep { !/^\d{1,3}(?:,\d{3})*$/ } @tok;
            problem("$file: a second Total row") if @total;
            @total = map { num($_) } @tok;
        }
    }
    if (!@total) { problem("$file: no Total row"); next; }
    my $nc = @total;
    my $tparts = 0; $tparts += $total[$_] for 0 .. $nc - 2;
    problem("$file: the Total row's columns sum to $tparts, its Total is $total[-1]") unless $tparts == $total[-1];
    my (%cand, %line_of);   # {XX} = [ each distinct reading of the row that closes on its Total ]
    for my $i (@hit) {
        for my $l (split /\n/, $pages[$i]) {
            next unless $l =~ /^([A-Z][A-Za-z]*(?: (?:of|[A-Z][A-Za-z]*))*?)(?: ?(\d{1,2}))? (\.+ .*|[\d,.].*)$/;
            my ($name, $mark, $rest) = ($1, $2, $3);
            next if $name eq 'Total' || $name eq 'State';
            my @tok = split ' ', $rest;
            next if grep { !/^(?:\.+|\d{1,3}(?:,\d{3})*)$/ } @tok;   # not a row of figures (a header line, a footnote)
            my $st = $code{$name};
            if (!$st) { problem("$file: a row '$name', no state"); next; }
            # the reading: the leader's dots (first, if any) and footnote marks (bare figures of one or two digits) dropped, as many as the row
            # carries beyond the columns - the one reading whose cells close on the row's Total
            my @drop = grep { ($_ == 0 && $tok[$_] =~ /^\.+$/) || $tok[$_] =~ /^\d{1,2}$/ } 0 .. $#tok;
            my $extra = @tok - $nc;
            my @readings;
            if ($extra == 0) { push @readings, [@tok]; }
            elsif ($extra > 0 && $extra <= 3 && @drop >= $extra) {
                my @pick = (0 .. $extra - 1);
                while (1) {
                    my %gone = map { $drop[$_] => 1 } @pick;
                    push @readings, [map { $tok[$_] } grep { !$gone{$_} } 0 .. $#tok];
                    my $k = $extra - 1;
                    $k-- while $k >= 0 && $pick[$k] == @drop - $extra + $k;
                    last if $k < 0;
                    $pick[$k]++;
                    $pick[$_] = $pick[$_ - 1] + 1 for $k + 1 .. $extra - 1;
                }
            }
            my @closing;
            for my $r (@readings) {
                next if grep { !/^(?:\.+|\d{1,3}(?:,\d{3})*)$/ } @$r;
                my @v = map { /^\.+$/ ? 0 : num($_) } @$r;
                my $parts = 0; $parts += $v[$_] for 0 .. $nc - 2;
                push @closing, \@v if $parts == $v[-1];
            }
            my %distinct = map { join(',', @$_) => $_ } @closing;
            if (!%distinct) { problem("$file $st: no reading of '" . trim($rest) . "' closes on its Total over $nc columns"); next; }
            problem("$file $st: a second row") if $cand{$st};
            $cand{$st} = [map { $distinct{$_} } sort keys %distinct];
            $line_of{$st} = trim($rest);
        }
    }
    problem("$file: no row for $_") for grep { !$cand{$code{$_}} } sort keys %code;
    # a row read more than one way (its leader and a mark, or a mark and a small figure, interchangeable on its own Total): the one combination of
    # the rows' readings whose columns give the Total row - every column but those this tool declares a gap in, by the gap declared
    my @amb = sort grep { @{$cand{$_}} > 1 } keys %cand;
    my $gap = $col_gap{$y} // {};
    problem("$file: a gap declared in the Republican or the Democratic column") if grep { $_ == 1 || $_ == 2 } keys %$gap;
    my @base = (0) x $nc;
    for my $st (grep { @{$cand{$_}} == 1 } keys %cand) { $base[$_] += $cand{$st}[0][$_] for 0 .. $nc - 1; }
    my (@fits, $best);
    my @pick = (0) x @amb;
    my $combos = 1; $combos *= scalar(@{$cand{$_}}) for @amb;
    if ($combos > 4096) { problem("$file: $combos combinations of ambiguous rows (" . join(' ', @amb) . ") - too many to try"); next; }
    for my $c (0 .. $combos - 1) {
        my ($r, @s) = ($c, @base);
        for my $a (0 .. $#amb) { my $n = @{$cand{$amb[$a]}}; $pick[$a] = $r % $n; $r = int($r / $n); my $v = $cand{$amb[$a]}[$pick[$a]]; $s[$_] += $v->[$_] for 0 .. $nc - 1; }
        my @off = grep { $s[$_] + ($gap->{$_ + 1} // 0) != $total[$_] } 0 .. $nc - 1;
        if (!defined $best || @off < $best) { $best = @off; @fits = (); }
        push @fits, { pick => [@pick], sums => [@s], off => [@off] } if @off == $best;
    }
    if (@fits != 1) { problem("$file: " . scalar(@fits) . " combinations of the rows' readings come equally close to the Total row's columns (read more than one way: " . join('; ', map { "$_ '$line_of{$_}'" } @amb) . ")"); next; }
    # the columns that still differ: each must be a gap declared above, by its amount - and never the Republican or the Democratic column
    for my $k (@{$fits[0]{off}}) { problem("$file: the states' column " . ($k + 1) . " sums to $fits[0]{sums}[$k], the Total row holds $total[$k] (the Total row less the states: " . ($total[$k] - $fits[0]{sums}[$k]) . ")" . (($gap->{$k + 1} // 0) ? ", not the gap declared, $gap->{$k + 1}" : '')); }
    next if @{$fits[0]{off}};
    my %rows = map { $_ => $cand{$_}[0] } grep { @{$cand{$_}} == 1 } keys %cand;
    $rows{$amb[$_]} = $cand{$amb[$_]}[$fits[0]{pick}[$_]] for 0 .. $#amb;
    my ($ri, $di) = $order eq 'RD' ? (0, 1) : (1, 0);
    $clerk{$y} = { R => $total[$ri], D => $total[$di], total => $total[-1], order => $order, columns => $nc, header => $hdr,
                   states => { map { $_ => { R => $rows{$_}[$ri], D => $rows{$_}[$di], total => $rows{$_}[-1] } } keys %rows } };
}

# US-5's catalog, 2016-2024: the Clerk read here must give it state by state
{
    my $f = "$usa/house_by_state.csv";
    open my $h, '<', $f or die "$f: $!\n";
    my %seen;
    while (my $l = <$h>) {
        next if $l =~ /^#/ || $l =~ /^year,/;
        $l =~ s/\s+$//;
        my ($y, $st, $r, $d, $o, $t) = split /,/, $l;
        $seen{$y}++;
        my $c = $clerk{$y} && $clerk{$y}{states}{$st};
        problem("$f $y $st: R $r D $d total $t, the Clerk read here " . ($c ? "R $c->{R} D $c->{D} total $c->{total}" : 'no row')) unless $c && $c->{R} == $r && $c->{D} == $d && $c->{total} == $t;
    }
    close $h;
    problem("$f: $_ - " . ($seen{$_} // 0) . " rows, not 50") for grep { ($seen{$_} // 0) != 50 } grep { $_ >= 2016 } @clerk_years;
}

# ---------------------------------------------------------------- the secondary compilation: Vital Statistics on Congress, Table 2-2
my %vs;   # {year} = { dem, rep, change, dir }
{
    my $file = 'midterms/vitalstats_table_2-2.csv';
    my $b = page($file);
    $b =~ s/^\xEF\xBB\xBF//;
    my @l = split /\r?\n/, $b;
    my $want = 'Year,DemPctVotes,DemPctSeats,RepPctVotes,RepPctSeats,ChangePctVotes,DirectionVotesChange,ChangePctSeatsWon,DirectionSeatsChange,DemSeatsAndVotesDifference';
    problem("$file: headed '" . ($l[0] // '') . "', not the heading read") unless ($l[0] // '') eq $want;
    for my $line (@l[1 .. $#l]) {
        next if $line eq '';
        my @c = split /,/, $line;
        unless (@c == 10 && $c[0] =~ /^\d{4}$/ && $c[1] =~ /^\d+(?:\.\d+)?$/ && $c[3] =~ /^\d+(?:\.\d+)?$/ && $c[5] =~ /^\d*\.?\d+$/ && $c[6] =~ /^[DR]$/) { problem("$file: a row '$line'"); next; }
        problem("$file: $c[0] twice") if $vs{$c[0]};
        $vs{$c[0]} = { dem => $c[1], rep => $c[3], change => $c[5], dir => $c[6] };
    }
    problem("$file: no row for $_") for grep { !$vs{$_} } grep { $_ >= 1946 } @house_years;
}

die "\n" if @problems;

# ---------------------------------------------------------------- the record: a row an election, 1944-2024, and the 2026 midterm (its result BILLED)
my @rows;
for my $y (@house_years, 2026) {
    my $pf = pres_from($y);
    my $party = $won{$pf} ? $won{$pf}{party} : undef;
    problem("no presidential election read for $y ($pf)") unless defined $party || $y == 1944;
    my $x = { year => $y, kind => $y % 4 == 0 ? 'presidential' : 'midterm', congress => congress_of($y), party => $party // '-', from => $pf,
              elected => $y % 4 == 0 && $won{$y} ? $won{$y}{party} : '-', hh => $hh{$y}, clerk => $clerk{$y}, vs => $vs{$y} };
    push @rows, $x;
}
my %row = map { $_->{year} => $_ } @rows;
sub largest { my $h = shift; return '' unless $h; return $h->{R} > $h->{D} ? 'R' : $h->{D} > $h->{R} ? 'D' : 'tie'; }
sub pres_seats { my ($h, $p) = @_; return $h->{$p}; }
sub share { my ($c, $p) = @_; return $c->{$p} / ($c->{R} + $c->{D}); }
my @rec;   # the derived rows, 1946-2026
for my $x (grep { $_->{year} >= 1946 } @rows) {
    my $prev = $row{$x->{year} - 2};
    my $sit = largest($prev->{hh});
    problem("the House sitting at $x->{year}: its largest party tied") if $sit eq 'tie';
    my $p = $x->{party};
    my %d = (row => $x, unified => $sit eq $p ? 'unified' : 'divided');
    if ($x->{hh} && $prev->{hh}) { $d{seats_before} = $prev->{hh}{$p}; $d{seats_after} = $x->{hh}{$p}; $d{seat_change} = $x->{hh}{$p} - $prev->{hh}{$p}; }
    if ($x->{clerk} && $prev->{clerk}) { $d{share_before} = share($prev->{clerk}, $p); $d{share_after} = share($x->{clerk}, $p); $d{swing} = 100 * ($d{share_after} - $d{share_before}); }
    if ($x->{vs}) { $d{vs_swing} = ($x->{vs}{dir} eq $p ? 1 : -1) * $x->{vs}{change}; }
    push @rec, \%d;
}
die "\n" if @problems;

# ---------------------------------------------------------------- the CSV
my $sources = join('', map { "#   $_ sha256 $read{$_}\n" } sort keys %read);
my $csv = "# GENERATED by Tools/us_midterm_record.pl from the pages under ElectionsData/usa/raw/ - DO NOT EDIT; the record is docs/generated/US_MIDTERM_RECORD.md.\n"
    . "# US-13: a row a House general election, 1944-2024, and the 2026 midterm (its result BILLED). congress: the Congress the election chose.\n"
    . "# president_party: the party of the winner of the last presidential election before it (president_from, NARA's Electoral College results\n"
    . "# page). elected_party: in a presidential year, that year's winner's party. seats_*: [HH-DIV]'s division of the Congress chosen, as of its\n"
    . "# initial election results (seats_vacant: the vacancies its footnote puts in the row's total; seats_total: the row's own). clerk_*: the Clerk\n"
    . "# of the House's recapitulation, its Total row - each party's own ballot line, and the row's Total - where the statistics carry text, else -.\n"
    . "# vs_*: SECONDARY (Vital Statistics on Congress, Table 2-2), each party's percentage of all votes and the change of the major-party vote\n"
    . "# from the last election with its direction, as printed; never read by R-US20's rule. The pages read, each with its SHA-256:\n"
    . $sources
    . "year,kind,congress,president_party,president_from,elected_party,seats_d,seats_r,seats_other,seats_vacant,seats_total,clerk_r,clerk_d,clerk_total,vs_dem_pct,vs_rep_pct,vs_vote_change,vs_vote_direction\n";
for my $x (@rows) {
    my $h = $x->{hh}; my $c = $x->{clerk}; my $v = $x->{vs};
    $csv .= join(',', $x->{year}, $x->{kind}, $x->{congress}, $x->{party}, $x->{from}, $x->{elected},
        $h ? ($h->{D}, $h->{R}, $h->{other}, $h->{vacant}, $h->{seats}) : ('-') x 5,
        $c ? ($c->{R}, $c->{D}, $c->{total}) : ('-') x 3,
        $v ? ($v->{dem}, $v->{rep}, $v->{change}, $v->{dir}) : ('-') x 4) . "\n";
}
my $csv_sha = sha256_hex($csv);

# ---------------------------------------------------------------- the document
sub pp { my $v = shift; return '-' unless defined $v; my $s = sprintf('%+.2f', $v); $s = '+0.00' if $s eq '-0.00'; return $s; }
sub pct { my $v = shift; return defined $v ? sprintf('%.2f %%', 100 * $v) : '-'; }
sub sc { my $v = shift; return defined $v ? sprintf('%+d', $v) : '-'; }
sub pname { my $p = shift; return $p eq 'R' ? 'REP' : $p eq 'D' ? 'DEM' : $p; }
sub vsfmt { my $v = shift; return '-' unless defined $v; my $s = ($v < 0 ? '-' : '+') . sprintf('%s', abs($v)); return $s; }
my $doc = "<!-- GENERATED by Tools/us_midterm_record.pl from ElectionsData/usa/house_midterms.csv (sha256 $csv_sha). DO NOT EDIT BY HAND. Re-derive: perl Tools/us_midterm_record.pl -->\n"
    . "# The midterm record - the president's party in the House at every midterm since 1946, and in the presidential years beside (US-13, R-US20)\n\n"
    . "Read on `ElectionsData/usa/house_midterms.csv` (`Tools/us_midterm_record.pl`, from saved pages only: [HH-DIV]'s party divisions for the seats, "
    . "NARA's Electoral College results pages for each president's party, the Clerk of the House's recapitulations for the vote where they carry text, "
    . "and a secondary compilation printed beside). R-US20's rule is the fenced block in `docs/specs/USA_STAGE_PLAN.md`; `UsMidtermCheck` re-reads this "
    . "record and prints the model's midterm beside it on the US card (`docs/reference/US_ELECTIONS.md`).\n\n"
    . "- **The president's party** at an election is the party of the winner of the last presidential election before it (NARA): a midterm reads the "
    . "election two years before it, a presidential year the one four years before it - the sitting president's party. The column names the election read and its winner.\n"
    . "- **Unified** where the president's party is the largest party in the House sitting on the election's day (the Congress the election two years before chose, by [HH-DIV]); **divided** otherwise.\n"
    . "- **A seat change** is the president's party's seats in the Congress the election chose less its seats in the Congress before, as [HH-DIV] prints them - "
    . "the divisions as of each Congress's initial election results, so deaths, specials, vacancies and party switches inside a Congress are not in them, and the chamber's size is each row's own.\n"
    . "- **A vote swing** is the president's party's share of the two-party House vote less its share at the election two years before, in points - each party's own ballot line, "
    . "the Clerk's recapitulation's Total row (US-5's basis). The Clerk's statistics of 1944-1996 are page images with no text layer (`ElectionsData/usa/raw/midterms/fetch_log.txt`), "
    . "so every vote swing that needs one of them is BILLED (-).\n"
    . "- **SECONDARY**: Vital Statistics on Congress, Table 2-2 - its change of the major-party vote from the last election, to the decimals it prints, signed for the president's party. "
    . "Its own notes: each party's percentage of all votes excludes districts whose unopposed candidates had no vote recorded; its sources, as its notes list them, are the Biographical Directory of the United States Congress, Congressional Quarterly's Almanac, National Journal, the Almanac of American Politics and the FEC, its latest update MIT's Election Data and Science Lab. "
    . "Printed beside, never read by R-US20's rule.\n\n";
my $table_head = "| year | president's party (the election read) | government | seats: the Congress before -> the one chosen | seat change | two-party share (the Clerk): before -> after | vote swing (the Clerk) | vote swing (SECONDARY) |\n"
    . "|---|---|---|---:|---:|---:|---:|---:|\n";
sub line_of {
    my $d = shift; my $x = $d->{row};
    my $w = $won{$x->{from}};
    return sprintf("| %d | %s (%d: %s) | %s | %s | %s | %s | %s | %s |\n", $x->{year}, pname($x->{party}), $x->{from}, $w->{name}, $d->{unified},
        defined $d->{seat_change} ? "$d->{seats_before} -> $d->{seats_after}" : '-', sc($d->{seat_change}),
        defined $d->{swing} ? pct($d->{share_before}) . ' -> ' . pct($d->{share_after}) : '-', pp($d->{swing}), vsfmt($d->{vs_swing}));
}
my @mid = grep { $_->{row}{kind} eq 'midterm' && $_->{row}{year} <= 2022 } @rec;
my @pres = grep { $_->{row}{kind} eq 'presidential' } @rec;
$doc .= "## The midterms\n\n" . $table_head . join('', map { line_of($_) } @mid) . "\n";
$doc .= "## The presidential years\n\nThe president's party is the sitting president's; the winner of the year's own election is named after the table.\n\n" . $table_head . join('', map { line_of($_) } @pres) . "\n";
$doc .= "Each presidential year's winner: " . join('; ', map { my $w = $won{$_->{row}{year}}; "$_->{row}{year} $w->{name} (" . pname($w->{party}) . ")" } @pres) . ".\n\n";

# the ranges by set
sub range_of {
    my ($key, @set) = @_;
    my @v = map { $_->{$key} } grep { defined $_->{$key} } @set;
    return (scalar(@v), undef, undef) unless @v;
    my ($lo, $hi) = ($v[0], $v[0]);
    for (@v) { $lo = $_ if $_ < $lo; $hi = $_ if $_ > $hi; }
    return (scalar(@v), $lo, $hi);
}
my @sets = (['every midterm, 1946-2022', @mid], ['unified midterms', grep { $_->{unified} eq 'unified' } @mid], ['divided midterms', grep { $_->{unified} eq 'divided' } @mid],
            ['every presidential year, 1948-2024', @pres], ['unified presidential years', grep { $_->{unified} eq 'unified' } @pres], ['divided presidential years', grep { $_->{unified} eq 'divided' } @pres]);
$doc .= "## The ranges, by government type and in the presidential years\n\nEach set's least and greatest; n counts the elections the column reads.\n\n"
    . "| set | n (seats) | seat change: least / greatest | n (the Clerk) | vote swing (the Clerk): least / greatest | n (SECONDARY) | vote swing (SECONDARY): least / greatest |\n"
    . "|---|---:|---:|---:|---:|---:|---:|\n";
for my $s (@sets) {
    my ($name, @set) = @$s;
    my ($ns, $sl, $sh) = range_of('seat_change', @set);
    my ($nv, $vl, $vh) = range_of('swing', @set);
    my ($nx, $xl, $xh) = range_of('vs_swing', @set);
    $doc .= sprintf("| %s | %d | %s / %s | %d | %s / %s | %d | %s / %s |\n", $name, $ns, sc($sl), sc($sh), $nv, pp($vl), pp($vh), $nx, vsfmt($xl), vsfmt($xh));
}
my @band = grep { defined $_->{swing} } @mid;
my ($nb, $L, $U) = range_of('swing', @band);
die "the rule's record is empty\n" unless $nb;
$doc .= "\n**R-US20's band** - the rule's record, the midterms whose two elections the Clerk's text pages both read (" . join(', ', map { $_->{row}{year} } @band) . "): "
    . sprintf("[L, U] = [%s, %s] points", pp($L), pp($U)) . ", the least and the greatest of their vote swings, both government types together. "
    . "The vote swing of every earlier midterm is BILLED. The rule compares at full precision; the two decimals here are for reading.\n\n";

# the Clerk's own differences, declared above
$doc .= "The Clerk's own Total row differs from the sum of its states' rows in columns of neither major party (the tool declares each; the Republican and Democratic columns agree exactly every year): "
    . join('; ', map { my $y = $_; "$y, " . join(' and ', map { "column $_ (" . sprintf('%+d', $col_gap{$y}{$_}) . ", the Total row less the states)" } sort { $a <=> $b } keys %{$col_gap{$y}}) . " of the header '$clerk{$y}{header}'" } sort keys %col_gap) . ".

";

# the Clerk against the secondary
$doc .= "## The Clerk and the secondary compilation, where both read a year\n\n| year | vote swing (the Clerk) | vote swing (SECONDARY) | SECONDARY less the Clerk |\n|---|---:|---:|---:|\n";
for my $d (grep { defined $_->{swing} && defined $_->{vs_swing} } @rec) {
    $doc .= sprintf("| %d | %s | %s | %s |\n", $d->{row}{year}, pp($d->{swing}), vsfmt($d->{vs_swing}), pp($d->{vs_swing} - $d->{swing}));
}

# the 2026 midterm
my ($m26) = grep { $_->{row}{year} == 2026 } @rec;
my $h24 = $hh{2024};
$doc .= "\n## The 2026 midterm\n\n- The president's party: " . pname($m26->{row}{party}) . " (2024: $won{2024}{name}); the House sitting: the " . congress_of(2024) . "th, "
    . "REP $h24->{R} / DEM $h24->{D} - $m26->{unified}.\n"
    . "- Its result is BILLED until the Clerk's 2026 statistics are saved (expected 2027): never estimated. Once read, it is printed beside the model's midterm, never read by R-US20's rule.\n";

die "\n" if @problems;

# ---------------------------------------------------------------- write: all built and tested, then each file once
my @out = ("$usa/house_midterms.csv" => $csv, 'docs/generated/US_MIDTERM_RECORD.md' => $doc);
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

# ---------------------------------------------------------------- the run's report
for my $y (@clerk_years) { my $c = $clerk{$y}; printf "the Clerk %d: %s first, %d columns, REP %d DEM %d total %d\n", $y, $c->{order} eq 'RD' ? 'REP' : 'DEM', $c->{columns}, $c->{R}, $c->{D}, $c->{total}; }
printf "R-US20's band over %d midterms (%s): [%s, %s]\n", $nb, join(' ', map { $_->{row}{year} } @band), pp($L), pp($U);
print "wrote $usa/house_midterms.csv (" . scalar(@rows) . " rows, sha256 $csv_sha) and docs/generated/US_MIDTERM_RECORD.md\n";
