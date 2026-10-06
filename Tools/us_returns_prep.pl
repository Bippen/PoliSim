#!/usr/bin/perl
# THE UNITED STATES' PRESIDENTIAL RETURNS BY JURISDICTION, 2012-2024, FOR THE RUNTIME (PS-6 US-3; COMPLETED.md s786), AND THE HOUSE VOTE BY STATE,
# 2016-2024, BESIDE THEM (US-5, s788). Reads only pages saved byte for byte under ElectionsData/usa/raw/ - each held to its group's SHA256SUMS.txt
# line before a byte of it is used - one READ transcription and two typed tables:
#   raw/returns/        the FEC's "Table 2. Electoral & Pop Vote" of Federal Elections 2012 (.xls), 2016 and 2020 (.xlsx), and its Official 2024
#                       Presidential General Election Results workbook: the popular vote by state and the electoral votes as cast; NARA's results
#                       pages for 2012 and 2016 (raw/executive/ holds 2020's and 2024's): the nominees, each state's electors and votes as cast,
#                       the national split, 2016's votes for other persons
#                       (US-5, s788) the Clerk of the House's election statistics, 2016-2024 (of the Presidential and Congressional Election, or of the Congressional Election in a midterm): its
#                       "Recapitulation of Votes Cast for United States Representatives" (text by pdftotext -raw), the House series; the FEC's
#                       House-by-party tables of Federal Elections 2016-2022 read beside it as the cross-check
#   raw/apportionment/  the Census Bureau's Table 1 of 2010 and 2020: the Representatives per state, so the electors in force each year
#   raw/district/       Nebraska's canvass books 2012-2024 (their text by pdftotext -raw), Maine's workbooks by congressional district, 2020
#                       and 2024, and the Governor's certificates of 2012 and 2016 (their OCR text layer, by pdftotext -raw)
#   ElectionsData/usa/maine_districts_read_2012_2016.tsv   Maine's districts in 2012 and 2016, READ by eye from those certificates (no
#                       district table of either year exists)
#   ElectionsData/usa/state_ev_2024.csv and returns_2024.md   2024's tables typed before these pages were saved (returns_2024.md read back against them on 2026-10-05), held to them
# and writes
#   ElectionsData/usa/president_by_year.csv       a row a year: the nominees and the electoral vote as NARA prints it
#   ElectionsData/usa/president_by_state.csv      a row a jurisdiction a year: its electors, its popular vote, its electoral votes as cast
#   ElectionsData/usa/president_by_district.csv   a row a congressional district of Maine and Nebraska a year: the two nominees' votes
#   ElectionsData/usa/president_by_candidate.csv  (US-5) a row a candidate column of the 2024 workbook: the national vote
#   ElectionsData/usa/house_by_state.csv          (US-5) a row a state a year, 2016-2024: the House vote by the Clerk's recapitulation
#   Assets/Scripts/Elections/Generated/UsPresidentialReturns.cs   the CSVs as C# literals, each CSV's SHA-256 and the SHA-256 of every page
#                                                                 the run read or relied on (RawSources)
# GeneratedCatalogCheck re-reads the CSVs against the catalog and re-hashes every page RawSources lists. Tools/us_returns_mutations.sh proves the
# failure paths below on a copy of the inputs (run it after any change here).
#
# The electors in force: 3 U.S.C. 3 counts them as the Senators and Representatives "to which the several States are by law entitled at the
# time when the President and Vice President to be chosen come into office", and 2 U.S.C. 2a(b) entitles a census's apportionment from the
# Congress elected two years after it (the 1950 census's from the Eighty-third) - so an election year's electors are its Representatives under
# the latest census at least two years older, plus two Senators; the District of Columbia three (the Twenty-third Amendment). Both statutes are
# saved in raw/apportionment/, the amendment in raw/executive/; NARA's table of each year's electors per state is held to the result.
#
# It writes nothing and dies on any mismatch, naming every one it has found - a page off its digest, or an input it cannot read, stops it at
# that point, the mismatches found before it named with it:
#   - a page whose bytes are not its SHA256SUMS.txt line's;
#   - an apportionment that is not 435 Representatives over 50 states; a year's electors not 538; a state's electors not NARA's for that year
#     (nor, in 2024, the FEC workbook's own ELECTORAL VOTES column);
#   - a Table 2 whose state rows do not sum to its Total row, or a state whose parts do not sum to its total; the same for the 2024 workbook,
#     each of whose rows is also held to the sum of every candidate column; a Total-row cell the tool reads that a formula fills (Excel's
#     arithmetic on the sheet's own cells) without a typed national figure to answer to - 2012's two are held to Table 1 of the same workbook;
#   - a state's electoral votes as cast that are not NARA's, or electors left unaccounted for; a vote for another person that NARA's notes and
#     the FEC's notes do not both name with the same count, or that sits under any ticket but the one that carried the state;
#   - a nominee's surname in an FEC header that is not NARA's (one declared exception: 2020's Table 2 prints "Trunp");
#   - Nebraska's districts, or Maine's in 2020 and 2024, not summing to the FEC's state figures exactly; a Maine 2020 or 2024 leader, in a
#     district or the state, holding no more than half of the sheet's total ballots cast (TBC) - on the READING that TBC is the statute's
#     denominator, a ranked-choice count that would go to rounds, not modelled; a TBC cell empty, not the sum of its candidate, Others and
#     Blank columns, or - the districts' together, less Blank - not the FEC's typed state total;
#   - a Maine 2012 or 2016 figure READ that is not the certificate's own text layer - figure, district, page - or a READ cell missing or
#     twice, the overseas count not the certificate's footnote, a slate's at-large figure not the FEC's state figure; and, those aside, the
#     districts exceeding the state or falling short of it by more than the overseas votes "not tallied according to a Congressional district";
#   - where NARA's notes say which ticket carried which district (every split state), a district whose figures give it to the other ticket, or
#     a district against its state that the notes do not name; where there is no note, any district against its state (a note naming two
#     districts for one ticket - Nebraska's First and Third - binds them as a pair, not which is which);
#   - a tie, or all other candidates together reaching the larger nominee in any state or district (comparing the two nominees would then not
#     be the plurality);
#   - the record's winners - every state's electors to its plurality winner, Maine's and Nebraska's districts' to theirs - not giving NARA's
#     table exactly, state by state and in total: each nominee's electoral votes plus those the nominee's own electors cast for other persons;
#   - ElectionsData/usa/state_ev_2024.csv (the 2024 table typed before these pages were saved) not holding each jurisdiction once, each row as
#     the 2024 rows give it;
#   - (US-5) a candidate column of the 2024 workbook whose typed Total-row figure is not its state rows' sum, or the columns' national figures
#     not summing to TOTAL VOTES; a column's label holding a comma, a quote, a backslash or a control character;
#   - (US-5) the Clerk's House recapitulation not on exactly one page, titled for another election, or headed by anything but nine columns
#     ending Total with Republican and Democratic among them (read by their names - the order differs from year to year); a state row whose
#     columns do not sum to its own Total, a row naming no state, a state missing or twice, a column of the states not summing to the Total row;
#   - (US-5) an FEC House table not titled for its year, without GENERAL ELECTION over Democratic, Republican and Other in columns E-G, its
#     rows not summing to its typed Total row (a formula there refused), a state missing or twice; the two publications agreeing exactly on
#     fewer than 35 of the 50 states - every year read differs in a few, the run prints which (among them the FEC adds a district's vote for an
#     unexpired term to its full-term vote, Louisiana's December runoff to November, and prints Maine's first ranked-choice round, where the Clerk
#     prints the last round - president_returns.md reading 13); a column misread makes almost every state differ;
#   - (US-5) 2024's House line (Republican, Democratic, the total) or the three presidential rows of returns_2024.md not the pages' figures;
#   - a perl warning during the checks (an undefined value reaching one), or a name bound for a C# literal holding a quote or backslash;
#   - a certificate's text layer without page breaks (the cited pages cannot then be checked - the extractor is named, not the transcription).
# All six outputs are built and tested before the first is written; a write that fails dies.
# Usage: perl Tools/us_returns_prep.pl   (from the project root, Git for Windows' perl; pdftotext at /mingw64/bin or on PATH)
# ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw($UnzipError);

my $usa = 'ElectionsData/usa';
my $raw = "$usa/raw";
my @years = (2012, 2016, 2020, 2024);
my $pdftotext = -x '/mingw64/bin/pdftotext' ? '/mingw64/bin/pdftotext' : 'pdftotext';
my @problems;
sub problem { push @problems, join('', @_); return; }
$SIG{__WARN__} = sub { problem('a perl warning: ', $_[0] =~ s/\s+$//r) };   # an undefined value reaching a check is a mismatch, not a passing 0
# a die - a page off its digest, an input it cannot read - stops the run at once; the mismatches queued before it are printed first, not lost
# (not inside a module's own eval)
$SIG{__DIE__} = sub { return if $^S; print STDERR "MISMATCH: $_\n" for splice @problems; };

# ---------------------------------------------------------------- the saved pages, each held to its SHA256SUMS.txt line before it is used
my %sums;
for my $g (qw(returns apportionment district executive)) {
    open my $s, '<', "$raw/$g/SHA256SUMS.txt" or die "$raw/$g/SHA256SUMS.txt: $!\n";
    while (my $l = <$s>) { $sums{"$g/$2"} = $1 if $l =~ /^([0-9a-f]{64}) \*(.+?)\s*$/; }
    close $s;
}
my %read;   # every page read or relied on, "raw/<group>/<file>" => SHA-256
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

# ---------------------------------------------------------------- readers: a sheet as { row => { col => value } }, rows and columns from 1
my %formula_at;   # "<page>|<sheet>" => { "row,col" => 1 } for every cell a formula fills - its cached value is read like any other, so a check that
                  # needs a TYPED figure asks here (2012's Table 2 fills two Total-row cells by formula: All Others over its state rows, Total Vote
                  # over that row's own vote cells)
sub ent { my $s = shift; $s =~ s/&lt;/</g; $s =~ s/&gt;/>/g; $s =~ s/&quot;/"/g; $s =~ s/&apos;/'/g; $s =~ s/&#(\d+);/chr($1)/ge; $s =~ s/&#x([0-9a-fA-F]+);/chr(hex $1)/ge; $s =~ s/&amp;/&/g; return $s; }
sub colnum { my $c = shift; my $n = 0; $n = $n * 26 + (ord($_) - 64) for split //, $c; return $n; }
sub xlsx_sheet {   # an Office Open XML workbook: the shared strings, the sheet by name through the workbook's relationships
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
        $formula_at{"$what|$want"}{"$row," . colnum($col)} = 1 if $body =~ m{<f[\s>/]};
        my $v = $body =~ m{<v>(.*?)</v>}s ? $1 : ($body =~ m{<is>.*?<t[^>]*>(.*?)</t>}s ? $1 : undef);
        next unless defined $v;
        $cells{$row}{colnum($col)} = $attr =~ /t="s"/ ? $ss[$v] : ent($v);
    }
    return \%cells;
}
sub xls_sheet {   # BIFF8 in an OLE2 compound file: SST, LABELSST, LABEL, NUMBER, RK, MULRK, FORMULA (its cached value)
    my ($d, $want, $what) = @_;
    die "$what: not an OLE2 compound file\n" unless substr($d, 0, 8) eq "\xD0\xCF\x11\xE0\xA1\xB1\x1A\xE1";
    my $ssz = 1 << unpack('v', substr($d, 0x1E, 2));
    my $mshift = unpack('v', substr($d, 0x20, 2));
    my ($nfat, $dirstart, $cutoff, $mfstart, $difstart, $ndif) = map { unpack('V', substr($d, $_, 4)) } (0x2C, 0x30, 0x38, 0x3C, 0x44, 0x48);
    my $sec = sub { substr($d, 512 + $_[0] * $ssz, $ssz) };
    my @dif = unpack('V109', substr($d, 0x4C, 436));
    my $ds = $difstart;
    for (1 .. $ndif) { last if $ds >= 0xFFFFFFFA; my @e = unpack('V*', $sec->($ds)); my $nx = pop @e; push @dif, @e; $ds = $nx; }
    @dif = grep { $_ < 0xFFFFFFFA } @dif;
    @dif = @dif[0 .. $nfat - 1] if @dif > $nfat;
    my @fat;
    push @fat, unpack('V*', $sec->($_)) for @dif;
    my $chain = sub { my $s = shift; my $o = ''; my $g = 0; while ($s < 0xFFFFFFFA) { $o .= $sec->($s); $s = $fat[$s]; die "$what: a FAT loop\n" if ++$g > 1e7; } $o };
    my $dir = $chain->($dirstart);
    my (%str, $rs);
    for (my $o = 0; $o + 128 <= length $dir; $o += 128) {
        my $e = substr($dir, $o, 128);
        my $nl = unpack('v', substr($e, 0x40, 2));
        next unless $nl >= 2;
        my $nm = substr($e, 0, $nl - 2);
        $nm =~ s/(.)\x00/$1/gs;
        my $ty = ord(substr($e, 0x42, 1));
        my ($s0, $sz) = (unpack('V', substr($e, 0x74, 4)), unpack('V', substr($e, 0x78, 4)));
        $rs = $s0 if $ty == 5;
        $str{$nm} = [$s0, $sz] if $ty == 2;
    }
    my ($ws, $wz) = @{$str{Workbook} // $str{Book} // die "$what: no Workbook stream\n"};
    my $wb;
    if ($wz >= $cutoff) { $wb = substr($chain->($ws), 0, $wz); }
    else {
        my $mini = $chain->($rs);
        my @mf = unpack('V*', $chain->($mfstart));
        my $ms = 1 << $mshift;
        my $s = $ws;
        $wb = '';
        while ($s < 0xFFFFFFFA) { $wb .= substr($mini, $s * $ms, $ms); $s = $mf[$s]; }
        $wb = substr($wb, 0, $wz);
    }
    my @r;
    for (my $p = 0; $p + 4 <= length $wb;) { my ($t, $l) = unpack('vv', substr($wb, $p, 4)); push @r, [$t, substr($wb, $p + 4, $l), $p]; $p += 4 + $l; }
    my $xlstr = sub {
        my ($b, $o, $short) = @_;
        my $cch = $short ? ord(substr($b, $o, 1)) : unpack('v', substr($b, $o, 2));
        $o += $short ? 1 : 2;
        my $fl = ord(substr($b, $o, 1));
        $o++;
        if (!$short) { $o += 2 if $fl & 8; $o += 4 if $fl & 4; }
        my $raw = substr($b, $o, $cch * (($fl & 1) ? 2 : 1));
        ($fl & 1) ? join('', map { chr } unpack('v*', $raw)) : join('', map { chr } unpack('C*', $raw));
    };
    my $rk = sub { my $v = shift; my $n; if ($v & 2) { my $i = $v >> 2; $i -= 2**30 if $i & 0x20000000; $n = $i; } else { $n = unpack('d', pack('VV', 0, $v & 0xFFFFFFFC)); } $n /= 100 if $v & 1; $n };
    my (@sheets, @sst);
    for (my $i = 0; $i < @r; $i++) {
        my ($t, $b) = @{$r[$i]};
        push @sheets, { pos => unpack('V', substr($b, 0, 4)), name => $xlstr->($b, 6, 1) } if $t == 0x0085;
        next unless $t == 0x00FC;
        my @seg = ($b);
        my $j = $i + 1;
        push(@seg, $r[$j++][1]) while $j < @r && $r[$j][0] == 0x003C;
        my ($si, $so) = (0, 8);
        my $total = unpack('V', substr($seg[0], 4, 4));
        my $rd = sub { my $n = shift; my $o = ''; while ($n > 0) { if ($so >= length $seg[$si]) { $si++; $so = 0; } my $k = length($seg[$si]) - $so; $k = $n if $k > $n; $o .= substr($seg[$si], $so, $k); $so += $k; $n -= $k; } $o };
        for (1 .. $total) {
            if ($so >= length $seg[$si]) { $si++; $so = 0; last if $si > $#seg; }
            my $cch = unpack('v', $rd->(2));
            my $fl = ord($rd->(1));
            my $hi = $fl & 1;
            my $runs = ($fl & 8) ? unpack('v', $rd->(2)) : 0;
            my $ext = ($fl & 4) ? unpack('V', $rd->(4)) : 0;
            my $s = '';
            my $left = $cch;
            while ($left > 0) {
                if ($so >= length $seg[$si]) { $si++; $hi = ord(substr($seg[$si], 0, 1)) & 1; $so = 1; }
                my $per = $hi ? 2 : 1;
                my $n = int((length($seg[$si]) - $so) / $per);
                $n = $left if $n > $left;
                my $raw = substr($seg[$si], $so, $n * $per);
                $so += $n * $per;
                $left -= $n;
                $s .= $hi ? join('', map { chr } unpack('v*', $raw)) : join('', map { chr } unpack('C*', $raw));
            }
            $rd->(4 * $runs) if $runs;
            $rd->($ext) if $ext;
            push @sst, $s;
        }
    }
    my ($sh) = grep { $_->{name} eq $want } @sheets;
    die "$what: no sheet '$want'\n" unless $sh;
    my %pos;
    $pos{$r[$_][2]} = $_ for 0 .. $#r;
    my $i = $pos{$sh->{pos}} // die "$what: sheet '$want' points nowhere\n";
    my (%cells, $pend);
    for ($i++; $i < @r; $i++) {
        my ($t, $b) = @{$r[$i]};
        last if $t == 0x000A;
        my ($row, $col) = length($b) >= 4 ? unpack('vv', $b) : (0, 0);
        $row++;
        $col++;
        if ($t == 0x00FD) { $cells{$row}{$col} = $sst[unpack('V', substr($b, 6, 4))]; }
        elsif ($t == 0x0204) { $cells{$row}{$col} = $xlstr->($b, 6, 0); }
        elsif ($t == 0x0203) { $cells{$row}{$col} = unpack('d', substr($b, 6, 8)); }
        elsif ($t == 0x027E) { $cells{$row}{$col} = $rk->(unpack('V', substr($b, 6, 4))); }
        elsif ($t == 0x00BD) { my $last = unpack('v', substr($b, -2)) + 1; my $o = 4; for my $cc ($col .. $last) { $cells{$row}{$cc} = $rk->(unpack('V', substr($b, $o + 2, 4))); $o += 6; } }
        elsif ($t == 0x0006) { $formula_at{"$what|$want"}{"$row,$col"} = 1; my $res = substr($b, 6, 8); if (substr($res, 6, 2) eq "\xFF\xFF") { $pend = [$row, $col] if ord(substr($res, 0, 1)) == 0; } else { $cells{$row}{$col} = unpack('d', $res); } }
        elsif ($t == 0x0207 && $pend) { $cells{$pend->[0]}{$pend->[1]} = $xlstr->($b, 0, 0); undef $pend; }
    }
    return \%cells;
}
sub trim { my $s = shift // ''; $s =~ s/^\s+|\s+$//g; return $s; }
sub count {   # a count as a sheet holds it - a number, or text with thousands commas and footnote asterisks; an empty cell is none
    my ($v, $what) = @_;
    my $s = $v // '';
    $s =~ s/[*\s,]//g;
    return 0 if $s eq '';
    die "$what: '$v' is not a count\n" unless $s =~ /^\d+(?:\.0+)?$/;
    return $s + 0;
}
sub surname { my @w = split ' ', shift; pop @w while @w > 1 && $w[-1] =~ /^(?:Jr\.?|Sr\.?|II|III|IV)$/; return $w[-1]; }

# ---------------------------------------------------------------- the jurisdictions: the Census tables' and NARA's names, the FEC's codes
my %code = ('Alabama' => 'AL', 'Alaska' => 'AK', 'Arizona' => 'AZ', 'Arkansas' => 'AR', 'California' => 'CA', 'Colorado' => 'CO', 'Connecticut' => 'CT',
    'Delaware' => 'DE', 'District of Columbia' => 'DC', 'Florida' => 'FL', 'Georgia' => 'GA', 'Hawaii' => 'HI', 'Idaho' => 'ID', 'Illinois' => 'IL',
    'Indiana' => 'IN', 'Iowa' => 'IA', 'Kansas' => 'KS', 'Kentucky' => 'KY', 'Louisiana' => 'LA', 'Maine' => 'ME', 'Maryland' => 'MD',
    'Massachusetts' => 'MA', 'Michigan' => 'MI', 'Minnesota' => 'MN', 'Mississippi' => 'MS', 'Missouri' => 'MO', 'Montana' => 'MT', 'Nebraska' => 'NE',
    'Nevada' => 'NV', 'New Hampshire' => 'NH', 'New Jersey' => 'NJ', 'New Mexico' => 'NM', 'New York' => 'NY', 'North Carolina' => 'NC',
    'North Dakota' => 'ND', 'Ohio' => 'OH', 'Oklahoma' => 'OK', 'Oregon' => 'OR', 'Pennsylvania' => 'PA', 'Rhode Island' => 'RI',
    'South Carolina' => 'SC', 'South Dakota' => 'SD', 'Tennessee' => 'TN', 'Texas' => 'TX', 'Utah' => 'UT', 'Vermont' => 'VT', 'Virginia' => 'VA',
    'Washington' => 'WA', 'West Virginia' => 'WV', 'Wisconsin' => 'WI', 'Wyoming' => 'WY');
my %name_of = reverse %code;
my @order = map { $code{$_} } sort keys %code;   # by name, as the FEC and NARA list them

# ---------------------------------------------------------------- the apportionment: Representatives per state under each census's Table 1
my %reps;
for my $spec ([2010, 'apportionment/census_apportionment_table01_2010.xls', 'Sheet1', \&xls_sheet],
              [2020, 'apportionment/census_apportionment_table01_2020.xlsx', 'Table 1', sub { xlsx_sheet(@_) }]) {
    my ($census, $file, $sheet, $reader) = @$spec;
    my $c = $reader->(page($file), $sheet, $file);
    # the seat column, found by its own heading (the 2010 table spreads it over three rows, the 2020 one holds it in one cell)
    my ($col) = grep { my $k = $_; join(' ', map { $c->{$_}{$k} // '' } 1 .. 10) =~ /NUMBER OF APPORTIONED\s+REPRESENTATIVES/ } 2 .. 6;
    die "$file: no 'NUMBER OF APPORTIONED REPRESENTATIVES' column\n" unless $col;
    for my $r (sort { $a <=> $b } keys %$c) {
        my $n = trim($c->{$r}{1});
        next unless $code{$n} && $n ne 'District of Columbia';
        problem("$file: $n twice") if exists $reps{$census}{$code{$n}};
        $reps{$census}{$code{$n}} = count($c->{$r}{$col}, "$file $n");
    }
    my $t = 0;
    $t += $_ for values %{$reps{$census}};
    problem("$file: $t Representatives over " . scalar(keys %{$reps{$census}}) . " states, not 435 over 50") unless $t == 435 && keys(%{$reps{$census}}) == 50;
}
sub census_of { my $year = shift; my ($c) = grep { $_ + 2 <= $year && $year < $_ + 12 } (2010, 2020); die "no apportionment governs $year\n" unless $c; return $c; }
sub electors { my ($year, $st) = @_; return 3 if $st eq 'DC'; return $reps{census_of($year)}{$st} + 2; }
sub representatives { my ($year, $st) = @_; return $st eq 'DC' ? 0 : $reps{census_of($year)}{$st}; }

# ---------------------------------------------------------------- NARA: the nominees, the split, each state's electors and votes as cast
my %nara;   # $nara{year} = { name => {D, R}, winner, opponent, wn, on, total, st => { XX => { electors, D, R, otherD, otherR } }, tot => {...}, others => { XX => [[recipient, n]] } }
sub cells_of {
    my $row = shift;
    my @c;
    while ($row =~ /<t[hd]\b([^>]*)>(.*?)<\/t[hd]>/gs) {
        my ($attr, $x) = ($1, $2);
        $x =~ s/<[^>]+>/ /g;
        $x =~ s/&nbsp;/ /g;
        $x = ent($x);
        $x =~ s/\s+/ /g;
        my ($span) = $attr =~ /colspan="?(\d+)/;
        push @c, [trim($x), $span // 1];
    }
    return @c;
}
for my $year (@years) {
    my $rel = $year <= 2016 ? "returns/archives_electoral_college_$year.html" : "executive/archives_electoral_college_$year.html";
    my $h = page($rel);
    utf8::decode($h);
    $h =~ s/<!--.*?-->//gs;   # the 2012 page carries a commented-out copy of its first columns in every row
    my %n;
    my @rows = map { [cells_of($_)] } $h =~ /(<tr\b.*?<\/tr>)/gs;
    my ($pcols, @head);
    for (my $i = 0; $i < @rows; $i++) {
        my @t = map { $_->[0] } @{$rows[$i]};
        next unless @t;
        if ($t[0] eq 'President' && ($t[1] // '') =~ /^(.+?) \[(D|R)\]$/) { $n{name}{$2} = $1; $n{winner} = $2; }
        elsif ($t[0] =~ /^Main Opponent$/ && ($t[1] // '') =~ /^(.+?) \[(D|R)\]$/) { $n{name}{$2} = $1; $n{opponent} = $2; }
        elsif ($t[0] =~ /^Electoral Vote\*?$/) {
            my $j = join(' ', @t);
            ($n{wn}) = $j =~ /Winner: (\d+)/;
            ($n{on}) = $j =~ /Main Opponent: (\d+)/;
            ($n{total}, $n{majority}) = $j =~ /Total\/Majority: (\d+)\/(\d+)/;
        }
        elsif (!defined $pcols && $t[0] eq 'State' && grep { $_ eq 'For President' } @t) {
            my ($fp) = grep { $_->[0] eq 'For President' } @{$rows[$i]};
            $pcols = $fp->[1];
            @head = map { $_->[0] } @{$rows[++$i]}[0 .. $pcols - 1];
        }
        # a state's name carries its footnote mark - asterisks on the 2016 page, a number on 2024's ("Maine 2")
        elsif (defined $pcols && ($t[0] =~ s/\s*(?:\*+|\d+)$//r) =~ /^(Total|[A-Z][A-Za-z. ]+)$/ && ($code{$1} || $1 eq 'Total') && @t >= 2 + $pcols) {
            my $key = $1 eq 'Total' ? 'Total' : $code{$1};
            my %v = (electors => count($t[1], "NARA $year $key electors"));
            my $slate;
            for my $k (0 .. $pcols - 1) {
                my $cell = $t[2 + $k] eq '-' ? 0 : count($t[2 + $k], "NARA $year $key column $k");
                if ($head[$k] eq 'Other') { die "NARA $year: an Other column before any nominee\n" unless $slate; $v{"other$slate"} += $cell; next; }
                my ($nm) = $head[$k] =~ /^(.+?),\s*of\s/ or die "NARA $year: column heading '$head[$k]'\n";
                my ($p) = grep { surname($n{name}{$_} // '') eq surname($nm) } qw(D R);
                die "NARA $year: the column '$head[$k]' is neither nominee's\n" unless $p;
                $v{$p} += $cell;
                $slate = $p;
            }
            $v{$_} //= 0 for qw(D R otherD otherR);
            if ($key eq 'Total') { $n{tot} = \%v; } else { problem("NARA $year: $key twice") if $n{st}{$key}; $n{st}{$key} = \%v; }
        }
    }
    die "NARA $year: the nominees, the split or the state table not found\n" unless $n{name}{D} && $n{name}{R} && $n{winner} && $n{opponent} && defined $n{wn} && defined $n{on} && $pcols && $n{tot};
    die "NARA $year: the winner and the main opponent are one party\n" if $n{winner} eq $n{opponent};
    # the votes for other persons, as the page's notes give them: "the electoral votes for Texas were: for President, Trump 36, Ron Paul 1, and John Kasich 1;"
    (my $text = $h) =~ s/<[^>]+>/ /g;
    $text =~ s/&nbsp;/ /g;
    $text = ent($text);
    $text =~ s/\s+/ /g;
    while ($text =~ /the electoral votes for ([A-Z][a-z]+(?: [A-Z][a-z]+)*) were: for President, (.*?); for Vice President/g) {
        my ($state, $list) = ($1, $2);
        my $st = $code{$state} or die "NARA $year: a note on '$state'\n";
        for my $item (split /\s*,\s*and\s+|\s*,\s*|\s+and\s+/, $list) {
            my ($who, $k) = $item =~ /^(.+?)\s+(\d+)$/ or die "NARA $year $st: the note's item '$item'\n";
            my ($p) = grep { surname($n{name}{$_}) eq $who } qw(D R);
            if ($p) { problem("NARA $year $st: the note gives $who $k, the table $n{st}{$st}{$p}") unless $n{st}{$st} && $n{st}{$st}{$p} == $k; next; }
            push @{$n{others}{$st}}, [$who, $k + 0];
        }
    }
    # which district each ticket carried, where the state split: "Maine appoints its electors proportionally. Clinton/Kaine won in the First
    # Congressional District and took the state; Trump/Pence won the Second Congressional District." - a ticket by its presidential surname
    my %ord = (First => 1, Second => 2, Third => 3);
    my $ords = '(?:First|Second|Third)(?: and (?:First|Second|Third))*';
    my $party_of = sub { my ($ticket) = @_; my ($first) = split m{[/-]}, $ticket; my ($p) = grep { surname($n{name}{$_}) eq $first } qw(D R); die "NARA $year: the ticket '$ticket' is neither nominee's\n" unless $p; $p };
    while ($text =~ /(Maine|Nebraska) appoints its electors proportionally\. (\S+) won in the ($ords) Congressional Districts? and took the state; (\S+) won the ($ords) Congressional Districts?\./g) {
        my ($st, $pa, $da, $pb, $db) = ($code{$1}, $party_of->($2), $3, $party_of->($4), $5);
        problem("NARA $year $st: two notes on its districts") if $n{split}{$st};
        problem("NARA $year $st: one ticket named for both sides") if $pa eq $pb;
        $n{split}{$st} = { took => $pa, $pa => [map { $ord{$_} } split / and /, $da], $pb => [map { $ord{$_} } split / and /, $db] };
    }
    $nara{$year} = \%n;
}

# ---------------------------------------------------------------- the FEC: each year's Table 2 (2012-2020), the 2024 workbook
my %y;   # $y{year}{st}{XX} = { vD, vR, other, total, evD, evR }; $y{year}{notes} = { XX => [[recipient, n]] }
my %typo = ('2020 R' => 'Trunp');   # the FEC's own misprint in 2020's Table 2 heading, Trump's column: declared, so a second one still fails
for my $spec ([2012, 'returns/fec_federalelections2012.xls', 'Table 2. Electoral &  Pop Vote', \&xls_sheet],
              [2016, 'returns/fec_federalelections2016.xlsx', 'Table 2. Electoral &  Pop Vote', sub { xlsx_sheet(@_) }],
              [2020, 'returns/fec_federalelections2020.xlsx', '3. Table 2 Electoral & Pop Vote', sub { xlsx_sheet(@_) }]) {
    my ($year, $file, $sheet, $reader) = @$spec;
    my $bytes = page($file);
    my $c = $reader->($bytes, $sheet, $file);
    # the heading row: "<Surname> (D|R)" over B, C (the electoral vote) and D, E (the popular vote), the winner first; F "All Others", G "Total Vote"
    my ($hr) = grep { ($c->{$_}{2} // '') =~ /\((D|R)\)\s*$/ } sort { $a <=> $b } keys %$c;
    die "$file: no heading row\n" unless $hr;
    my %party;
    for my $col (2 .. 5) {
        my ($nm, $p) = ($c->{$hr}{$col} // '') =~ /^\s*(.*?)\s*\((D|R)\)\s*$/ or die "$file: heading $col '" . ($c->{$hr}{$col} // '') . "'\n";
        $party{$col} = $p;
        my $want = surname($nara{$year}{name}{$p});
        problem("$file: column $col heads '$nm ($p)', NARA's nominee is $nara{$year}{name}{$p}") unless $nm eq $want || ($typo{"$year $p"} // '') eq $nm;
    }
    die "$file: the electoral and popular columns disagree on the parties\n" unless $party{2} eq $party{4} && $party{3} eq $party{5} && $party{2} ne $party{3};
    die "$file: F and G are not 'All Others' and 'Total Vote'\n" unless trim($c->{$hr}{6}) =~ /^All Others$/i && trim($c->{$hr}{7}) =~ /^Total Vote$/i;
    my (%tot, $notes, $totrow);
    for my $r (sort { $a <=> $b } keys %$c) {
        my $a = trim($c->{$r}{1});
        if ($a =~ /^Total:/) { %tot = map { $_ => count($c->{$r}{$_}, "$file Total col $_") } 2 .. 7; $totrow = $r; next; }
        if ($r > $hr && %tot && $a ne '') { $notes .= " $a"; next; }
        next unless $a =~ /^([A-Z]{2})\*{0,4}$/;
        my $st = $1;
        die "$file: '$st' is no jurisdiction\n" unless $name_of{$st};
        problem("$file: $st twice") if $y{$year}{st}{$st};
        $y{$year}{st}{$st} = { "ev$party{2}" => count($c->{$r}{2}, "$file $st B"), "ev$party{3}" => count($c->{$r}{3}, "$file $st C"),
            "v$party{4}" => count($c->{$r}{4}, "$file $st D"), "v$party{5}" => count($c->{$r}{5}, "$file $st E"),
            other => count($c->{$r}{6}, "$file $st F"), total => count($c->{$r}{7}, "$file $st G") };
    }
    die "$file: no Total row\n" unless %tot;
    my @st = keys %{$y{$year}{st}};
    problem("$file: " . scalar(@st) . " jurisdictions, not 51") unless @st == 51;
    my %sum;
    for my $s (@st) { $sum{$_} += $y{$year}{st}{$s}{$_} for qw(evD evR vD vR other total); }
    problem("$file: the state rows' electoral votes against the Total row") unless $sum{"ev$party{2}"} == $tot{2} && $sum{"ev$party{3}"} == $tot{3};
    problem("$file: the state rows' popular vote against the Total row") unless $sum{"v$party{4}"} == $tot{4} && $sum{"v$party{5}"} == $tot{5} && $sum{other} == $tot{6} && $sum{total} == $tot{7};
    for my $s (@st) { my $e = $y{$year}{st}{$s}; problem("$file $s: $e->{vD} + $e->{vR} + $e->{other} is not the total $e->{total}") unless $e->{vD} + $e->{vR} + $e->{other} == $e->{total}; }
    # A Total-row cell a formula fills is Excel's arithmetic on the sheet's own cells - holding the state rows to it proves the reader, not the
    # source. Such a cell must have a
    # TYPED national figure to answer to: 2012's Table 2 sums its All Others and Total Vote, and Table 1 of the same workbook types the nominees'
    # national votes and the national total; any other formula in a Total row fails.
    my %typed = $year == 2012 ? map { $_ => 1 } 4 .. 7 : ();
    for my $k (2 .. 7) { problem("$file: the Total row's column $k is a formula, with no typed national figure to hold the state rows to") if $formula_at{"$file|$sheet"}{"$totrow,$k"} && !$typed{$k}; }
    if ($year == 2012) {
        my $t1sheet = 'Table 1. 2012 Pres Popular Vote';
        my $t1 = xls_sheet($bytes, $t1sheet, $file);
        my %row;
        for my $r (keys %$t1) {
            my $a = trim($t1->{$r}{1});
            $row{total} = $r if $a =~ /^Total:/;
            for my $p ('D', 'R') { $row{$p} = $r if index($a, "$nara{2012}{name}{$p} (") == 0; }
        }
        die "$file $t1sheet: the nominees' rows or the Total row not found\n" unless defined $row{D} && defined $row{R} && defined $row{total};
        for my $k (sort keys %row) { problem("$file $t1sheet: the $k row's vote is a formula, not a typed figure") if $formula_at{"$file|$t1sheet"}{"$row{$k},2"}; }
        my ($t1d, $t1r, $t1t) = map { count($t1->{$row{$_}}{2}, "$file $t1sheet $_") } qw(D R total);
        problem("$file: Table 2's state rows give D $sum{vD} R $sum{vR} others $sum{other} total $sum{total}, Table 1 types D $t1d R $t1r total $t1t")
            unless $sum{vD} == $t1d && $sum{vR} == $t1r && $sum{total} == $t1t && $sum{other} == $t1t - $t1d - $t1r;
    }
    # the notes under the table: "* Texas has 38 Electoral Votes.  1 was cast for John Kasich and 1 was cast for Ron Paul."
    $notes //= '';
    while ($notes =~ /([A-Z][a-z]+(?: [A-Z][a-z]+)*) has (\d+) Electoral Votes(.*?)(?=[A-Z][a-z]+(?: [A-Z][a-z]+)* has \d+ Electoral Votes|$)/g) {
        my ($state, $rest) = ($1, $3);
        my $st = $code{$state} or die "$file: a note on '$state'\n";
        push @{$y{$year}{notes}{$st}}, [$2, $1 + 0] while $rest =~ /(\d+) (?:was|were) cast for (.+?)(?=\s+and\s+\d|\.\s|\.$|$)/g;
    }
}
my @candidates;   # US-5: [year, the column's label, the ticket (R, D, or - for neither nominee), the national vote] - 2024's workbook, a column a candidate
{   # 2024: one wide sheet - the electors (B), the electoral vote by party (C, D), a column a candidate by surname, the total (last)
    my $file = 'returns/fec_2024presgeresults.xlsx';
    my $c = xlsx_sheet(page($file), 'OFFICIAL 2024 PRES GE RESULTS', $file);
    my %col;
    $col{trim($c->{1}{$_})} = $_ for keys %{$c->{1}};
    my ($sR, $sD) = (uc surname($nara{2024}{name}{R}), uc surname($nara{2024}{name}{D}));
    my ($cE, $evR, $evD, $vR, $vD, $vT) = ($col{'ELECTORAL VOTES'}, $col{"ELECTORAL VOTE: $sR (R)"}, $col{"ELECTORAL VOTE: $sD (D)"}, $col{$sR}, $col{$sD}, $col{'TOTAL VOTES'});
    die "$file: the electors, NARA's nominees' columns ($sR, $sD) or the total not found\n" unless $cE && $evR && $evD && $vR && $vD && $vT;
    my @cand = grep { $_ > $evD && $_ < $vT } sort { $a <=> $b } values %col;   # every candidate column, None of These Candidates and the write-ins among them
    my (%tot, %ctot, %csum);
    for my $r (sort { $a <=> $b } keys %$c) {
        my $a = trim($c->{$r}{1});
        if ($a =~ /^Total:/) {
            %tot = (evR => count($c->{$r}{$evR}, "$file Total"), evD => count($c->{$r}{$evD}, "$file Total"), vR => count($c->{$r}{$vR}, "$file Total"), vD => count($c->{$r}{$vD}, "$file Total"), total => count($c->{$r}{$vT}, "$file Total"));
            $ctot{$_} = count($c->{$r}{$_}, "$file Total col $_") for @cand;
            for my $k ($evR, $evD, $vT, @cand) { problem("$file: the Total row's column $k is a formula, with no typed national figure to hold the state rows to") if $formula_at{"$file|OFFICIAL 2024 PRES GE RESULTS"}{"$r,$k"}; }
            next;
        }
        next unless $a =~ /^([A-Z]{2})$/ && $name_of{$1};
        my $st = $1;
        problem("$file: $st twice") if $y{2024}{st}{$st};
        my ($d, $re, $t) = (count($c->{$r}{$vD}, "$file $st"), count($c->{$r}{$vR}, "$file $st"), count($c->{$r}{$vT}, "$file $st"));
        my $all = 0;
        for my $k (@cand) { my $v = count($c->{$r}{$k}, "$file $st col $k"); $all += $v; $csum{$k} += $v; }
        problem("$file $st: its candidate columns sum to $all, its total is $t") unless $all == $t;
        problem("$file $st: the workbook's electors " . count($c->{$r}{$cE}, "$file $st") . ", the apportionment's " . electors(2024, $st)) unless count($c->{$r}{$cE}, "$file $st") == electors(2024, $st);
        $y{2024}{st}{$st} = { evR => count($c->{$r}{$evR}, "$file $st"), evD => count($c->{$r}{$evD}, "$file $st"), vD => $d, vR => $re, other => $t - $d - $re, total => $t };
    }
    my @st = keys %{$y{2024}{st}};
    problem("$file: " . scalar(@st) . " jurisdictions, not 51") unless @st == 51;
    my %sum;
    for my $s (@st) { $sum{$_} += $y{2024}{st}{$s}{$_} for qw(evD evR vD vR total); }
    problem("$file: the state rows against the Total row") unless %tot && $sum{evR} == $tot{evR} && $sum{evD} == $tot{evD} && $sum{vR} == $tot{vR} && $sum{vD} == $tot{vD} && $sum{total} == $tot{total};
    # US-5: each candidate's national vote - the Total row's typed figure, held to its column's state rows; the columns together to the total
    my $call = 0;
    for my $k (@cand) {
        my $label = trim($c->{1}{$k});
        problem("$file: candidate column '$label' sums to " . ($csum{$k} // 0) . " over the states, its Total-row figure is " . ($ctot{$k} // 'missing')) unless defined $ctot{$k} && ($csum{$k} // 0) == $ctot{$k};
        problem("$file: the candidate column '$label' holds a comma, a quote, a backslash or a control character") if $label =~ /[,"\\\x00-\x1f]/;   # a CSV cell and a C# literal
        $call += $ctot{$k} // 0;
        push @candidates, [2024, $label, $k == $vR ? 'R' : $k == $vD ? 'D' : '-', $ctot{$k} // 0];
    }
    problem("$file: the candidate columns' national figures sum to $call, its TOTAL VOTES to $tot{total}") unless %tot && $call == $tot{total};
}

# ---------------------------------------------------------------- the electors in force, the votes as cast and for other persons: three sources held together
for my $year (@years) {
    my $n = $nara{$year};
    my $t = 0;
    $t += electors($year, $_) for @order;
    problem("$year: the apportionment's electors total $t, not 538") unless $t == 538;
    problem("$year: NARA's total $n->{tot}{electors} electors, majority $n->{majority}") unless $n->{tot}{electors} == 538 && $n->{total} == 538 && $n->{majority} == 270;
    problem("$year: NARA's table has " . scalar(keys %{$n->{st}}) . " jurisdictions") unless keys(%{$n->{st}}) == 51;
    my %others_fec = map { my $s = $_; ($s => join('; ', map { "$_->[0] $_->[1]" } sort { $a->[0] cmp $b->[0] } @{$y{$year}{notes}{$s}})) } keys %{$y{$year}{notes} // {}};
    my %others_nara = map { my $s = $_; ($s => join('; ', map { "$_->[0] $_->[1]" } sort { $a->[0] cmp $b->[0] } @{$n->{others}{$s}})) } keys %{$n->{others} // {}};
    for my $s (sort keys %{{ map { ($_ => 1) } keys %others_fec, keys %others_nara }}) { problem("$year $s: the FEC's notes give '" . ($others_fec{$s} // 'none') . "', NARA's '" . ($others_nara{$s} // 'none') . "'") unless ($others_fec{$s} // '') eq ($others_nara{$s} // ''); }
    for my $s (@order) {
        my ($e, $ns) = ($y{$year}{st}{$s}, $n->{st}{$s});
        next unless $e && $ns;
        my $el = electors($year, $s);
        problem("$year $s: NARA gives $ns->{electors} electors, the apportionment $el") unless $ns->{electors} == $el;
        problem("$year $s: cast D $e->{evD} R $e->{evR} by the FEC, $ns->{D} and $ns->{R} by NARA") unless $e->{evD} == $ns->{D} && $e->{evR} == $ns->{R};
        my $oth = $el - $e->{evD} - $e->{evR};
        $e->{cast_other} = $oth;
        problem("$year $s: the FEC's cast votes exceed the electors") if $oth < 0;
        problem("$year $s: $oth elector(s) cast for neither nominee, NARA's Other columns give " . ($ns->{otherD} + $ns->{otherR})) unless $oth == $ns->{otherD} + $ns->{otherR};
        my $named = 0;
        $named += $_->[1] for @{$n->{others}{$s} // []};
        problem("$year $s: $oth elector(s) cast for neither nominee, the notes name $named") unless $oth == $named;
        $e->{cast_other_to} = join('; ', map { "$_->[0] $_->[1]" } @{$n->{others}{$s} // []});
        $e->{electors} = $el;
    }
}

# ---------------------------------------------------------------- the districts: Maine's and Nebraska's, by congressional district
my %dist;   # $dist{year}{XX}{n} = { D, R, read }
for my $year (@years) {   # Nebraska: the Secretary of State's canvass book; a district's first Total line after its heading is the president's
    my ($file) = grep { $sums{$_} } map { "district/$_" } ("nebraska_sos_general_canvass_$year.pdf", "nebraska_sos_general_canvass_book_$year.pdf");
    die "NE $year: no canvass book among the saved pages\n" unless $file;
    page($file);
    open my $p, '-|', $pdftotext, '-raw', "$raw/$file", '-' or die "$pdftotext: $!\n";
    my @l = <$p>;
    close $p or die "$pdftotext $file: exit " . ($? >> 8) . "\n";
    my $cd;
    my %w = (one => 1, two => 2, three => 3);
    for my $line (@l) {
        if ($line =~ /^Congressional District\s+(One|Two|Three|0?1|0?2|0?3)\b/i) { $cd = $w{lc $1} // ($1 + 0); next; }
        # R then D, the book's order; a swapped reading cannot pass the sum below unless the state's two nominees polled the same
        if (defined $cd && $line =~ /^Total\s+([\d,]+)\s+([\d,]+)/i) { $dist{$year}{NE}{$cd} //= { R => count($1, "NE $year"), D => count($2, "NE $year"), read => 0 }; undef $cd; }
    }
    my @k = sort keys %{$dist{$year}{NE} // {}};
    problem("NE $year: districts '@k', the apportionment gives " . representatives($year, 'NE')) unless @k == representatives($year, 'NE') && "@k" eq join(' ', 1 .. @k);
    my ($sr, $sd) = (0, 0);
    for my $k (@k) { $sr += $dist{$year}{NE}{$k}{R}; $sd += $dist{$year}{NE}{$k}{D}; }
    my $e = $y{$year}{st}{NE};
    problem("NE $year: the districts give R $sr D $sd, the FEC's state row R $e->{vR} D $e->{vD}") unless $sr == $e->{vR} && $sd == $e->{vD};
}
for my $year (2020, 2024) {   # Maine: the Secretary of State's workbook by district, a sheet a district; its "CG<n> Total" row holds the towns and the overseas votes
    my $file = "district/maine_sos_president_by_cd_$year.xlsx";
    my $bytes = page($file);
    for my $cd (1 .. representatives($year, 'ME')) {
        my $c = xlsx_sheet($bytes, "CG$cd", $file);
        my %pc;
        for my $col (keys %{$c->{3} // {}}) { my $v = trim($c->{3}{$col}); $pc{D} = $col if $v eq 'Democratic'; $pc{R} = $col if $v eq 'Republican'; }
        die "$file CG$cd: no Democratic and Republican columns in row 3\n" unless $pc{D} && $pc{R};
        my @tr = grep { trim($c->{$_}{3}) =~ /^CG\s*$cd\s+Total$/ } keys %$c;
        die "$file CG$cd: " . scalar(@tr) . " 'CG$cd Total' rows\n" unless @tr == 1;
        # TBC: the sheet's total ballots cast - a formula cell, held below to the columns it sums and, statewide, to the FEC's typed total
        my %head = map { (trim($c->{1}{$_}) => $_) } keys %{$c->{1} // {}};
        my ($muni, $tbc) = ($head{MUNICIPALITY}, $head{TBC});
        my ($blank) = grep { trim($c->{1}{$_}) =~ /^blank$/i } keys %{$c->{1} // {}};
        die "$file CG$cd: no MUNICIPALITY, Blank and TBC columns in row 1, in that order\n" unless $muni && $blank && $tbc && $muni < $blank && $blank < $tbc;
        die "$file CG$cd: the 'CG$cd Total' row's TBC cell is empty\n" unless defined $c->{$tr[0]}{$tbc} && $c->{$tr[0]}{$tbc} ne '';
        my $t = count($c->{$tr[0]}{$tbc}, "$file CG$cd TBC");
        my $sum = 0;
        $sum += count($c->{$tr[0]}{$_}, "$file CG$cd col $_") for $muni + 1 .. $tbc - 1;
        problem("$file CG$cd: the TBC $t is not the sum $sum of its candidate, Others and Blank columns") unless $t == $sum;
        $dist{$year}{ME}{$cd} = { D => count($c->{$tr[0]}{$pc{D}}, "$file CG$cd"), R => count($c->{$tr[0]}{$pc{R}}, "$file CG$cd"), read => 0,
                                  tbc => $t, blank => count($c->{$tr[0]}{$blank}, "$file CG$cd Blank") };
    }
    my $e = $y{$year}{st}{ME};
    my ($sd, $sr) = (0, 0);
    for my $k (keys %{$dist{$year}{ME}}) { $sd += $dist{$year}{ME}{$k}{D}; $sr += $dist{$year}{ME}{$k}{R}; }
    problem("ME $year: the districts give D $sd R $sr, the FEC's state row D $e->{vD} R $e->{vR}") unless $sd == $e->{vD} && $sr == $e->{vR};
    # Maine's count is ranked-choice under its statute as saved (21-A s1(27-C)(D), s723-A - current text): a leader holding more than half of all
    # ballots cast - blank and overvoted ballots counted - wins outright by s723-A(2). READING: the sheet's TBC is that denominator - TBC is the sum
    # of the candidate, Others and Blank columns (held above), there is no overvote column, and no saved page says whether "Blank" holds ballots
    # overvoted at the first rank. So the test below is the statute's outright majority on that reading; which ticket carried the state and each
    # district is held independently by NARA's notes and table. A contest that would go to further rounds fails here - rounds are not modelled.
    my ($tbc, $blank) = (0, 0);
    for my $k (sort keys %{$dist{$year}{ME}}) {
        my $d = $dist{$year}{ME}{$k};
        my $lead = $d->{D} > $d->{R} ? $d->{D} : $d->{R};
        problem("ME $year CG$k: the leader's $lead is not more than half of the $d->{tbc} ballots cast (TBC) - a ranked-choice count, its rounds not modelled") unless 2 * $lead > $d->{tbc};
        $tbc += $d->{tbc};
        $blank += $d->{blank};
    }
    problem("ME $year: the districts' TBC $tbc less their Blank $blank is not the FEC's typed state total $e->{total}") unless $tbc - $blank == $e->{total};
    my $lead = $e->{vD} > $e->{vR} ? $e->{vD} : $e->{vR};
    problem("ME $year: the statewide leader's $lead is not more than half of the $tbc ballots cast (TBC) - a ranked-choice count, its rounds not modelled") unless 2 * $lead > $tbc;
}
my $read_tsv = "$usa/maine_districts_read_2012_2016.tsv";
my $read_digest;
{   # Maine 2012, 2016: READ by eye from the Governor's certificates (no district table exists) - and each figure held to the certificate's own
    # OCR text layer: the two readings must agree figure for figure, district for district and page for page. The overseas votes "not tallied
    # according to a Congressional district" are in the at-large totals only, so the districts also fall short of the state by at most that many.
    open my $h, '<:raw', $read_tsv or die "$read_tsv: $!\n";
    my $b = do { local $/; <$h> };   # slurped here only: the certificates below are read a line at a time
    close $h;
    $read_digest = sha256_hex($b);
    my (%unalloc, %onpage, $header);
    for my $line (split /\r?\n/, $b) {
        next if $line eq '' || $line =~ /^#/;
        my @f = split /\t/, $line;
        if (!$header) { $header = 1; die "$read_tsv: heading '$line'\n" unless $line eq "year\tstate\tdistrict\tparty\tvotes\tcertificate\tpage"; next; }
        die "$read_tsv: a row of " . scalar(@f) . " fields: $line\n" unless @f == 7;
        my ($year, $st, $cd, $p, $v, $cert, $pg) = @f;
        die "$read_tsv: $year $st is not Maine 2012 or 2016\n" unless $st eq 'ME' && ($year == 2012 || $year == 2016);
        die "$read_tsv: a $year row cites '$cert', not that year's certificate\n" unless $cert eq "raw/district/archives_ascertainment_maine_$year.pdf";
        die "$read_tsv: a $year row's page is '$pg'\n" unless $pg =~ /^[1-9]\d*$/;
        if ($p eq 'unallocated') {
            die "$read_tsv: a $year unallocated row for district '$cd', not '-'\n" unless $cd eq '-';
            problem("$read_tsv: ME $year unallocated twice") if exists $unalloc{$year};
            $unalloc{$year} = count($v, $read_tsv);
            $onpage{$year}{unallocated} = $pg;
            next;
        }
        die "$read_tsv: '$p' / district '$cd'\n" unless ($p eq 'D' || $p eq 'R') && $cd =~ /^[12]$/;
        problem("$read_tsv: ME $year $cd $p twice") if exists $dist{$year}{ME}{$cd}{$p};
        $dist{$year}{ME}{$cd}{$p} = count($v, $read_tsv);
        $dist{$year}{ME}{$cd}{read} = 1;
        $onpage{$year}{"$cd$p"} = $pg;
    }
    for my $year (2012, 2016) {
        my @missing = grep { !defined $dist{$year}{ME}{$_->[0]}{$_->[1]} } ([1, 'D'], [1, 'R'], [2, 'D'], [2, 'R']);
        problem("$read_tsv: ME $year $_->[0] $_->[1] not READ") for @missing;
        problem("$read_tsv: ME $year: no unallocated count READ") unless defined $unalloc{$year};
        problem("ME $year: two districts READ, the apportionment gives " . representatives($year, 'ME')) unless representatives($year, 'ME') == 2;
        next if @missing || !defined $unalloc{$year};
        # the certificate's text layer: each slate's block - the headings First, Second, At-Large, At-Large, its four figures in that order
        # (2012 prints each beside its elector's name, 2016 after the four names; split digit groups such as "142,93 7" are joined)
        my $cert = "district/archives_ascertainment_maine_$year.pdf";
        page($cert);
        open my $pt, '-|', $pdftotext, '-raw', "$raw/$cert", '-' or die "$pdftotext: $!\n";
        my @l = <$pt>;
        close $pt or die "$pdftotext $cert: exit " . ($? >> 8) . "\n";
        # the page a figure stands on is counted from the extractor's page breaks: an extractor set to print none cannot check the cited pages,
        # and says so rather than blaming the transcription
        my $breaks = 0;
        $breaks += ($_ =~ tr/\f//) for @l;
        problem("$cert: the text layer from $pdftotext carries no page breaks (an xpdfrc with textPageBreaks no?) - the cited pages cannot be checked") unless $breaks;
        my ($party, %block, @footnote);
        my $pageno = 1;
        for my $line (@l) {
            $pageno += ($line =~ tr/\f//);
            $line =~ s/\f//g;
            $line =~ s/\s+$//;
            push @footnote, [$1, $pageno] if $line =~ /There were ([\d,]+) votes cast for President and Vice President by Uniformed Service or Overseas voters/;
            if ($line =~ /^The (DEMOCRATIC|REPUBLICAN) PARTY Electors/) {
                $party = $1 eq 'DEMOCRATIC' ? 'D' : 'R';
                problem("$cert: two $party slates in its text layer") if $block{$party};
                $block{$party} = { heads => [], figs => [] };
                next;
            }
            if ($line =~ /^The .*PARTY Electors/ || $line =~ /There were/) { undef $party; next; }
            next unless $party;
            if ($line =~ /^(First|Second) Congressional District/) { push @{$block{$party}{heads}}, $1; next; }
            if ($line =~ /^At-Large$/) { push @{$block{$party}{heads}}, 'At-Large'; next; }
            if ($line =~ /(\d[\d, ]*\d|\d)$/) { (my $n = $1) =~ s/[ ,]//g; push @{$block{$party}{figs}}, [$n + 0, $pageno]; }
        }
        for my $p ('D', 'R') {
            my $blk = $block{$p} or do { problem("$cert: no $p slate in its text layer"); next; };
            problem("$cert: the $p slate's headings are '@{$blk->{heads}}', not 'First Second At-Large At-Large'") unless "@{$blk->{heads}}" eq 'First Second At-Large At-Large';
            my @f = @{$blk->{figs}};
            if (@f != 4) { problem("$cert: the $p slate carries " . scalar(@f) . " figure(s) in its text layer, not four"); next; }
            for my $cd (1, 2) {
                my ($fig, $pg) = @{$f[$cd - 1]};
                problem("$read_tsv: ME $year $cd $p READ $dist{$year}{ME}{$cd}{$p}, the certificate's text layer $fig") unless $fig == $dist{$year}{ME}{$cd}{$p};
                problem("$read_tsv: ME $year $cd $p cites page $onpage{$year}{\"$cd$p\"}, the figure is on page $pg") unless !$breaks || $pg == $onpage{$year}{"$cd$p"};
            }
            # its at-large figures are the FEC's state figure: the right slate was read
            problem("$cert: the $p slate's at-large figures $f[2][0] and $f[3][0], the FEC's state row " . $y{$year}{st}{ME}{"v$p"}) unless $f[2][0] == $f[3][0] && $f[2][0] == $y{$year}{st}{ME}{"v$p"};
        }
        my @fn = map { count($_->[0], "$cert footnote") } @footnote;
        problem("$cert: the footnote's counts '@fn', the READ $unalloc{$year}") unless @fn && !grep { $_ != $unalloc{$year} } @fn;
        problem("$read_tsv: ME $year unallocated cites page $onpage{$year}{unallocated}, which carries no footnote") unless !$breaks || grep { $_->[1] == $onpage{$year}{unallocated} } @footnote;
        # and the bound the figures keep in any case: no party's districts above the state, the shortfall within the overseas votes
        my $e = $y{$year}{st}{ME};
        my $gap = 0;
        for my $p ('D', 'R') {
            my $g = $e->{"v$p"} - $dist{$year}{ME}{1}{$p} - $dist{$year}{ME}{2}{$p};
            problem("ME $year $p: the districts exceed the state by " . -$g) if $g < 0;
            $gap += $g;
        }
        problem("ME $year: the nominees' districts fall $gap short of the state, more than the certificate's $unalloc{$year} unallocated") if $gap > $unalloc{$year};
    }
}

# ---------------------------------------------------------------- the plurality: no tie, and no field of others able to reach the larger nominee
for my $year (@years) {
    for my $s (@order) {
        my $e = $y{$year}{st}{$s};
        problem("$year $s: a tie") if $e->{vD} == $e->{vR};
        my $big = $e->{vD} > $e->{vR} ? $e->{vD} : $e->{vR};
        problem("$year $s: all others together $e->{other}, the larger nominee $big") unless $e->{other} < $big;
        for my $cd (sort keys %{$dist{$year}{$s} // {}}) {
            my $d = $dist{$year}{$s}{$cd};
            problem("$year $s-$cd: a tie") if $d->{D} == $d->{R};
            my $db = $d->{D} > $d->{R} ? $d->{D} : $d->{R};
            # a minor candidate's district vote is at most his or her statewide vote, at most the state's others together
            problem("$year $s-$cd: the state's others together $e->{other} could reach the larger nominee's $db") unless $e->{other} < $db;
        }
    }
}

# ---------------------------------------------------------------- the districts against NARA's notes: where a state split, which ticket carried which district
# (where it did not, the per-state split below holds every district to the state's winner; which figures stand under which label then rests on
# the canvass's headings, the workbook's sheet names, or - for the READ rows - the certificate's text layer above; and where a note names two
# districts for one ticket - Nebraska's First and Third - which of the two stands under which label rests on the book's headings too)
for my $year (@years) {
    for my $s ('ME', 'NE') {
        my $e = $y{$year}{st}{$s};
        my $win = $e->{vD} > $e->{vR} ? 'D' : 'R';
        my %won = map { my $d = $dist{$year}{$s}{$_}; ($_ => ($d->{D} > $d->{R} ? 'D' : 'R')) } keys %{$dist{$year}{$s} // {}};
        my @against = sort grep { $won{$_} ne $win } keys %won;
        my $note = $nara{$year}{split}{$s};
        if (!$note) { problem("$year $s: districts '@against' went against the state, NARA's page names none") if @against; next; }
        problem("$year $s: NARA's note has " . ($note->{took} eq 'D' ? 'the Democrat' : 'the Republican') . " take the state, the FEC's state row the other") unless $note->{took} eq $win;
        for my $p ('D', 'R') {
            for my $cd (@{$note->{$p} // []}) { problem("$year $s-$cd: NARA's note gives it to $p, the record's figures to " . ($won{$cd} // 'no one')) unless ($won{$cd} // '') eq $p; }
        }
        my @named = sort @{$note->{$win eq 'D' ? 'R' : 'D'} // []};
        problem("$year $s: the districts against the state are '@against', NARA's note names '@named'") unless "@against" eq "@named";
    }
}

# ---------------------------------------------------------------- the split: the record's winners against NARA's table, state by state and in total
my %pledged;
for my $year (@years) {
    my $n = $nara{$year};
    my %tot = (D => 0, R => 0);
    for my $s (@order) {
        my $e = $y{$year}{st}{$s};
        my $win = $e->{vD} > $e->{vR} ? 'D' : 'R';
        my %got = (D => 0, R => 0);
        my $nd = keys %{$dist{$year}{$s} // {}};
        if ($nd) {
            problem("$year $s: $nd districts for " . representatives($year, $s) . " Representatives") unless $nd == representatives($year, $s);
            $got{$win} += $e->{electors} - $nd;   # at large: the Senators' two
            for my $cd (keys %{$dist{$year}{$s}}) { my $d = $dist{$year}{$s}{$cd}; $got{$d->{D} > $d->{R} ? 'D' : 'R'}++; }
        }
        else { $got{$win} += $e->{electors}; }
        my $ns = $n->{st}{$s};
        for my $p ('D', 'R') {
            problem("$year $s: the record's winners give $p $got{$p}, NARA's table " . ($ns->{$p} + $ns->{"other$p"}) . " ($ns->{$p} cast, " . $ns->{"other$p"} . " for other persons)") unless $got{$p} == $ns->{$p} + $ns->{"other$p"};
            $tot{$p} += $got{$p};
        }
    }
    my ($w, $o) = ($n->{winner}, $n->{opponent});
    my %cast = (D => 0, R => 0);
    $cast{D} += $y{$year}{st}{$_}{evD}, $cast{R} += $y{$year}{st}{$_}{evR} for @order;
    problem("$year: NARA prints $n->{wn} - $n->{on}, its table sums to $n->{tot}{$w} - $n->{tot}{$o}, the FEC's states $cast{$w} - $cast{$o}") unless $cast{$w} == $n->{wn} && $cast{$o} == $n->{on} && $n->{tot}{$w} == $n->{wn} && $n->{tot}{$o} == $n->{on};
    problem("$year: the record's winners give $w $tot{$w} - $o $tot{$o}, NARA's table " . ($n->{tot}{$w} + $n->{tot}{"other$w"}) . ' - ' . ($n->{tot}{$o} + $n->{tot}{"other$o"})) unless $tot{$w} == $n->{tot}{$w} + $n->{tot}{"other$w"} && $tot{$o} == $n->{tot}{$o} + $n->{tot}{"other$o"};
    $pledged{$year} = \%tot;
}

# ---------------------------------------------------------------- the 2024 table typed before these pages were saved, held to them
{
    my $old = "$usa/state_ev_2024.csv";
    open my $h, '<', $old or die "$old: $!\n";
    my ($rows, $header, %seen) = (0, 0);
    while (my $line = <$h>) {
        $line =~ s/\r?\n$//;
        next if $line eq '' || $line =~ /^#/;
        if (!$header) { $header = 1; problem("$old: heading '$line'") unless $line eq 'state;winner(R/D);winner_EV;other_EV'; next; }
        my ($st) = split /;/, $line;
        my $e = $y{2024}{st}{$st} or do { problem("$old: '$st' is no jurisdiction"); next; };
        problem("$old $st: a second row") if $seen{$st}++;
        my $win = $e->{evR} >= $e->{evD} ? 'R' : 'D';
        my ($we, $oe) = $win eq 'R' ? ($e->{evR}, $e->{evD}) : ($e->{evD}, $e->{evR});
        problem("$old $st: '$line', the saved pages give '$st;$win;$we;$oe'") unless $line eq "$st;$win;$we;$oe";
        $rows++;
    }
    close $h;
    problem("$old: no row for $_") for grep { !$seen{$_} } @order;
    problem("$old: $rows rows, not 51") unless $rows == 51;
}

# ---------------------------------------------------------------- the House (US-5): the Clerk's recapitulation by state, the series; the FEC's table beside it
# The Clerk of the House's election statistics (of the Presidential and Congressional Election; in a midterm of the Congressional Election) print on one page the "Recapitulation of Votes Cast for
# United States Representatives": a row a state, a column a ballot line as its own header names them (the order differs from year to year), and
# the Total row. It is the House series (DECLARED, docs/specs/USA_STAGE_PLAN.md US-5): one publication, 50 states, every year. The FEC's House
# table by party is read beside it, 2016-2022 (no FEC volume of 2024 is served); its general-election columns hold the same ballot lines.
my @house_years = (2016, 2018, 2020, 2022, 2024);
my @house_states = grep { $_ ne 'DC' } @order;
my (%house, %house_fec, %house_tot);   # {year}{XX} = { R, D, other, total }: the Clerk; {year}{XX} = { D, R, O }: the FEC; {year} = the Clerk's Total row
for my $year (@house_years) {
    my $file = "returns/clerk_statistics$year.pdf";
    page($file);
    open my $p, '-|', $pdftotext, '-raw', "$raw/$file", '-' or die "$pdftotext: $!\n";
    my $text = do { local $/; <$p> };
    close $p or die "$pdftotext $file: exit " . ($? >> 8) . "\n";
    my @pages = split /\f/, $text;
    my @hit = grep { $pages[$_] =~ /^Recapitulation of Votes Cast for United States Representatives, Election of /m } 0 .. $#pages;
    if (@hit != 1) { problem("$file: the House recapitulation's title on " . scalar(@hit) . " page(s), not one"); next; }
    my @lines = split /\n/, $pages[$hit[0]];
    my ($title) = grep { /^Recapitulation of Votes Cast for United States Representatives, Election of / } @lines;
    problem("$file: the recapitulation is titled '" . trim($title) . "', not an election of $year") unless $title =~ /, Election of [A-Z][a-z]+ \d{1,2}, $year\s*$/;
    my @hdr = grep { /^State / } @lines;
    if (@hdr != 1) { problem("$file: " . scalar(@hdr) . " header line(s) on the recapitulation's page, not one"); next; }
    (my $h = trim($hdr[0])) =~ s/^State //;
    $h =~ s/\bOther Parties\d\b/Other Parties/;   # the footnote mark the header carries ("See each State's recapitulation table")
    my @cols = $h =~ /(Other Parties|\S+)/g;
    my %ix;
    @ix{@cols} = 0 .. $#cols;
    unless (@cols == 9 && $cols[-1] eq 'Total' && keys(%ix) == 9 && defined $ix{Republican} && defined $ix{Democratic}) { problem("$file: the header '" . trim($hdr[0]) . "' - not nine columns ending Total, with Republican and Democratic"); next; }
    my (%sum, $total);
    for my $l (@lines) {
        # a name (words of capitals, dotted or with "of" between, so "District of Columbia" is named and refused, not skipped), perhaps a footnote
        # mark, then its figures with their dot leaders
        next unless $l =~ /^([A-Z][A-Za-z.]*(?: (?:of|[A-Z][A-Za-z.]*))*)\d* (.*)$/;
        my ($name, @tok) = ($1, split ' ', $2);
        next if $name eq 'State' || $name eq 'Recapitulation';
        shift @tok if @tok == 10 && $tok[0] =~ /^\.+$/;   # the leader after the name, apart from an empty first column's dots
        next unless @tok == 9 && !grep { !/^(?:\.+|[\d,]+)$/ } @tok;   # a row of figures, an empty cell its dots
        my @v = map { /^\.+$/ ? 0 : count($_, "$file $name") } @tok;
        if ($name eq 'Total') { problem("$file: a second Total row") if $total; $total = \@v; next; }
        my $st = $code{$name};
        if (!$st || $st eq 'DC') { problem("$file: a row '$name', no state"); next; }
        problem("$file $st: a second row") if $house{$year}{$st};
        my $parts = 0;
        $parts += $v[$_] for grep { $_ != $ix{Total} } 0 .. 8;
        problem("$file $st: its columns sum to $parts, its Total is $v[$ix{Total}]") unless $parts == $v[$ix{Total}];
        $sum{$_} += $v[$_] for 0 .. 8;
        my ($r, $d, $t) = ($v[$ix{Republican}], $v[$ix{Democratic}], $v[$ix{Total}]);
        $house{$year}{$st} = { R => $r, D => $d, other => $t - $r - $d, total => $t };
    }
    problem("$file: no row for $_") for grep { !$house{$year}{$_} } @house_states;
    if (!$total) { problem("$file: no Total row"); next; }
    for my $k (0 .. 8) { problem("$file: the states' $cols[$k] sums to " . ($sum{$k} // 0) . ", the Total row holds $total->[$k]") unless ($sum{$k} // 0) == $total->[$k]; }
    $house_tot{$year} = { R => $total->[$ix{Republican}], D => $total->[$ix{Democratic}], total => $total->[$ix{Total}] };
}
for my $spec ([2016, 'returns/fec_federalelections2016.xlsx', 'Table 7. House by Party'], [2018, 'returns/fec_federalelections2018.xlsx', 'Table 5. House by Party'],
              [2020, 'returns/fec_federalelections2020.xlsx', '8. Table 7 House by Party'], [2022, 'returns/fec_federalelections2022.xlsx', '6. Table 5 House by Party']) {
    my ($year, $file, $sheet) = @$spec;
    my $c = xlsx_sheet(page($file), $sheet, $file);
    problem("$file '$sheet': titled '" . trim($c->{1}{1}) . "', not ${year}'s House vote by party") unless trim($c->{1}{1}) =~ /^\Q$year VOTES CAST FOR THE U.S. HOUSE OF REPRESENTATIVES BY PARTY\E\b/;
    my ($hr) = grep { trim($c->{$_}{1}) eq 'State' } sort { $a <=> $b } keys %$c;
    unless ($hr && trim($c->{$hr}{5}) eq 'Democratic' && trim($c->{$hr}{6}) eq 'Republican' && trim($c->{$hr}{7}) eq 'Other' && !grep { trim($c->{$hr - 1}{$_}) !~ /^GENERAL ELECTION\b/ } 5 .. 7) {
        problem("$file '$sheet': no heading of GENERAL ELECTION over Democratic, Republican and Other in columns E-G"); next;
    }
    my (%sum, $tr);
    for my $r (grep { $_ > $hr } sort { $a <=> $b } keys %$c) {
        my $a = trim($c->{$r}{1});
        if ($a =~ /^Total:?$/) { $tr = $r; last; }
        next if $a eq '';
        problem("$file '$sheet' row $r: '$a', no state or territory") unless $a =~ /^[A-Z]{2}$/;
        my @v = map { count($c->{$r}{$_}, "$file $a") } 5 .. 7;
        $sum{$_} += $v[$_ - 5] for 5 .. 7;
        next unless $name_of{$a} && $a ne 'DC';
        problem("$file $a: a second row") if $house_fec{$year}{$a};
        $house_fec{$year}{$a} = { D => $v[0], R => $v[1], O => $v[2] };
    }
    if (!$tr) { problem("$file '$sheet': no Total row"); next; }
    for my $k (5 .. 7) {
        problem("$file '$sheet': the Total row's column $k is a formula, with no typed national figure to hold the rows to") if $formula_at{"$file|$sheet"}{"$tr,$k"};
        problem("$file '$sheet': the rows' column $k sums to " . ($sum{$k} // 0) . ", the Total row holds " . count($c->{$tr}{$k}, "$file Total")) unless ($sum{$k} // 0) == count($c->{$tr}{$k}, "$file Total");
    }
    problem("$file: no row for $_") for grep { !$house_fec{$year}{$_} } @house_states;
    # the two publications agree on most states exactly (every year read differs in a few, each printed below and read in president_returns.md's
    # reading 13); a header the reading mislabels makes almost every state differ
    my $same = grep { $house{$year}{$_} && $house_fec{$year}{$_} && $house{$year}{$_}{R} == $house_fec{$year}{$_}{R} && $house{$year}{$_}{D} == $house_fec{$year}{$_}{D} } @house_states;
    problem("House $year: the Clerk's and the FEC's tables agree exactly on $same of 50 states, under the 35 a column read right gives") unless $same >= 35;
}
{   # 2024's House line and the three presidential rows of ElectionsData/usa/returns_2024.md - typed before these pages were saved, read back against them on 2026-10-05 - held to them
    my $md = "$usa/returns_2024.md";
    open my $h, '<', $md or die "$md: $!\n";
    my $text = do { local $/; <$h> };
    close $h;
    my ($pres) = $text =~ /^### National result \S{1,3} President\r?\n(.*?)^### /ms;   # the dash between is the file's own (UTF-8, read as bytes)
    my ($hse) = $text =~ /^### National result \S{1,3} House\r?\n(.*?)^### /ms;
    problem("$md: no 'National result - House' section") unless $hse;
    problem("$md: no 'National result - President' section") unless $pres;
    my %typed;
    $typed{House}{$1} = count($2, "$md House $1") while ($hse // '') =~ /^\| (Republican|Democratic) \| [\d.]+ \(([\d,]+)\) \|/mg;
    $typed{House}{total} = count($1, "$md House total") if ($hse // '') =~ /^Total House votes ([\d,]+)/m;
    my $k = $house_tot{2024} // {};
    for my $p (['Republican', 'R'], ['Democratic', 'D'], ['total', 'total']) {
        problem("$md: the House's $p->[0] typed " . ($typed{House}{$p->[0]} // 'nowhere') . ", the Clerk's Total row " . ($k->{$p->[1]} // 'unread')) unless defined $typed{House}{$p->[0]} && defined $k->{$p->[1]} && $typed{House}{$p->[0]} == $k->{$p->[1]};
    }
    my %vote = map { $_->[1] => $_->[3] } @candidates;
    while (($pres // '') =~ /^\| [^|]*? ([A-Z][a-z]+) \((?:R|D|Green)\) \| [\d.]+ \(([\d,]+)\) \|/mg) {
        my ($who, $v) = (uc $1, count($2, "$md $1"));
        problem("$md: $who typed $v, the FEC workbook's Total row " . ($vote{$who} // 'nothing')) unless defined $vote{$who} && $vote{$who} == $v;
        $typed{President}{$who} = 1;
    }
    problem("$md: " . scalar(keys %{$typed{President} // {}}) . " presidential row(s) held, not the three it types") unless keys(%{$typed{President} // {}}) == 3;
}

# a name bound for a C# string literal holds no quote or backslash (a comma cannot reach it: the notes are split on commas, and a comma in a
# nominee's name fails the surname checks above)
for my $year (@years) {
    for my $name ($nara{$year}{name}{D}, $nara{$year}{name}{R}, map { $y{$year}{st}{$_}{cast_other_to} // '' } @order) { problem("$year: the name '$name' holds a quote or backslash") if $name =~ /["\\]/; }
}
$SIG{__WARN__} = 'DEFAULT';   # the checks are done: from here a warning is printed as warnings are, never counted as a mismatch
if (@problems) { my $n = @problems; print STDERR "MISMATCH: $_\n" for splice @problems; die "$n mismatch(es) - nothing written\n"; }

# ---------------------------------------------------------------- the CSVs and the catalog: all six texts built, and tested, before the first file opens
sub write_all {
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
my $gen ="# GENERATED by Tools/us_returns_prep.pl from the pages under ElectionsData/usa/raw/ - DO NOT EDIT; the record is ElectionsData/usa/president_returns.md.\n";
my $csv_y = $gen . "# A row a year. nominee_d / nominee_r: as NARA's results page names them. cast_d / cast_r: the electoral votes cast for each (NARA). others_d_slate /\n"
    . "# others_r_slate: the votes each nominee's own electors cast for other persons (NARA's Other columns). electors: the apportionment's, 538.\n"
    . "year,nominee_d,nominee_r,electors,cast_d,cast_r,others_d_slate,others_r_slate\n";
for my $year (@years) { my $n = $nara{$year}; $csv_y .= join(',', $year, $n->{name}{D}, $n->{name}{R}, 538, $n->{tot}{D}, $n->{tot}{R}, $n->{tot}{otherD}, $n->{tot}{otherR}) . "\n"; }
my $csv_s = $gen . "# A row a jurisdiction a year, by name. electors: Representatives under the apportionment in force plus two (DC three), held to NARA's. votes_*: the\n"
    . "# FEC's popular vote - the two nominees, all others together (2024: every other column, None of These Candidates and the write-ins among them), the\n"
    . "# total. cast_*: the electoral votes as cast (the FEC, held to NARA); cast_other_to: to whom, as NARA's notes name them.\n"
    . "year,state,electors,votes_d,votes_r,votes_other,votes_total,cast_d,cast_r,cast_other,cast_other_to\n";
for my $year (@years) { for my $s (@order) { my $e = $y{$year}{st}{$s}; $csv_s .= join(',', $year, $s, $e->{electors}, $e->{vD}, $e->{vR}, $e->{other}, $e->{total}, $e->{evD}, $e->{evR}, $e->{cast_other}, $e->{cast_other_to}) . "\n"; } }
my $csv_d = $gen . "# A row a congressional district of Maine and Nebraska a year: the two nominees' votes. basis: canvass (Nebraska's books, Maine's workbooks) or READ\n"
    . "# (Maine 2012 and 2016, from the Governor's certificates - ElectionsData/usa/maine_districts_read_2012_2016.tsv).\n"
    . "year,state,district,votes_d,votes_r,basis\n";
my @drows;
for my $year (@years) { for my $s ('ME', 'NE') { for my $cd (sort { $a <=> $b } keys %{$dist{$year}{$s}}) { my $d = $dist{$year}{$s}{$cd}; push @drows, [$year, $s, $cd, $d->{D}, $d->{R}, $d->{read}]; $csv_d .= join(',', $year, $s, $cd, $d->{D}, $d->{R}, $d->{read} ? 'READ' : 'canvass') . "\n"; } } }
my $csv_h = $gen . "# US-5: a row a state a year - the House of Representatives, by the Clerk of the House's \"Recapitulation of Votes Cast for United States\n"
    . "# Representatives\" (its election statistics): each party's own ballot line, each race as the Clerk prints it (president_returns.md reading 13 -\n"
    . "# Louisiana's runoff districts the finalists' December vote beside the others' November vote, Maine's finalists at their last ranked-choice\n"
    . "# round - in 2022 an eliminated candidate's first-round vote beside them - Alaska's first choices). votes_other: every other column - among them\n"
    . "# the non-votes some states report (blank, over and under votes) and Maine's ranked-choice lines, which count some ballots again. votes_total:\n"
    . "# the Clerk's.\n"
    . "year,state,votes_r,votes_d,votes_other,votes_total\n";
for my $year (@house_years) { for my $s (@house_states) { my $e = $house{$year}{$s}; $csv_h .= join(',', $year, $s, $e->{R}, $e->{D}, $e->{other}, $e->{total}) . "\n"; } }
my $csv_c = $gen . "# US-5: a row a candidate column of the FEC's Official 2024 Presidential General Election Results workbook - its Total row's national vote, held\n"
    . "# to the column's state rows. ticket: R or D for the nominee NARA names; - for every other column, None of These Candidates and the write-ins among them.\n"
    . "year,candidate,ticket,votes\n";
$csv_c .= join(',', @$_) . "\n" for @candidates;
my ($dy, $ds, $dd, $dh, $dc) = map { sha256_hex($_) } ($csv_y, $csv_s, $csv_d, $csv_h, $csv_c);
my $years_said = join(', ', @years[0 .. $#years - 1]) . " and $years[-1]";
my $house_said = join(', ', @house_years[0 .. $#house_years - 1]) . " and $house_years[-1]";

my $cs = "// GENERATED by Tools/us_returns_prep.pl. DO NOT EDIT BY HAND.\n//\n"
    . "// Sources: ElectionsData/usa/president_by_year.csv, president_by_state.csv, president_by_district.csv, president_by_candidate.csv and\n"
    . "// house_by_state.csv (their SHA-256 below), written by the same run from the saved pages listed in RawSources. GeneratedCatalogCheck re-reads\n"
    . "// the CSVs and compares every figure, and re-hashes every page RawSources lists.\n\nnamespace PoliSim.Elections.Generated\n{\n"
    . "    /// <summary>PS-6 US-3 (COMPLETED.md s786): the United States' presidential elections of $years_said by jurisdiction - SOURCED (the FEC,\n"
    . "    /// NARA, the Census Bureau's apportionment, Maine's and Nebraska's canvasses; `ElectionsData/usa/president_returns.md`). Beside them (US-5,\n"
    . "    /// s788): 2024's candidates by the FEC's workbook; the House vote by state, $house_said, by the Clerk of the House's\n"
    . "    /// statistics. Generated, never hand-edited. The class is partial: US-11's House districts are its other part, UsHouseDistricts.cs, written\n"
    . "    /// by Tools/us_house_prep.pl.</summary>\n"
    . "    public static partial class UsPresidentialReturns\n    {\n"
    . "        public const string YearSourceDigest = \"$dy\";\n        public const string StateSourceDigest = \"$ds\";\n        public const string DistrictSourceDigest = \"$dd\";\n"
    . "        public const string CandidateSourceDigest = \"$dc\";\n        public const string HouseSourceDigest = \"$dh\";\n"
    . "        public const string ReadTranscriptionDigest = \"$read_digest\";\n\n"
    . "        /// <summary>Every saved page the run read or relied on, by its path under `ElectionsData/usa/`, with its SHA-256.</summary>\n"
    . "        public static readonly (string Path, string Sha256)[] RawSources =\n        {\n"
    . join('', map { "            (\"$_\", \"$read{$_}\"),\n" } sort keys %read) . "        };\n\n"
    . "        /// <summary>A row a year: the nominees as NARA names them; the electoral votes cast for each; the votes each nominee's own electors cast for other\n"
    . "        /// persons.</summary>\n"
    . "        public static readonly (int Year, string NomineeD, string NomineeR, int Electors, int CastD, int CastR, int OthersDSlate, int OthersRSlate)[] Years =\n        {\n"
    . join('', map { my $n = $nara{$_}; "            ($_, \"$n->{name}{D}\", \"$n->{name}{R}\", 538, $n->{tot}{D}, $n->{tot}{R}, $n->{tot}{otherD}, $n->{tot}{otherR}),\n" } @years) . "        };\n\n"
    . "        /// <summary>A row a jurisdiction a year, by name: the electors in force; the popular vote (the two nominees, all others together, the total); the\n"
    . "        /// electoral votes as cast, and to whom the others went.</summary>\n"
    . "        public static readonly (int Year, string State, int Electors, long VotesD, long VotesR, long VotesOther, long VotesTotal, int CastD, int CastR, int CastOther, string CastOtherTo)[] States =\n        {\n";
for my $year (@years) { for my $s (@order) { my $e = $y{$year}{st}{$s}; $cs .= "            ($year, \"$s\", $e->{electors}, ${\ $e->{vD}}L, ${\ $e->{vR}}L, ${\ $e->{other}}L, ${\ $e->{total}}L, $e->{evD}, $e->{evR}, $e->{cast_other}, \"$e->{cast_other_to}\"),\n"; } }
$cs .= "        };\n\n"
    . "        /// <summary>Maine's and Nebraska's congressional districts: the two nominees' votes. Read: the figures were READ from the Governor's certificate (no\n"
    . "        /// district table exists), not parsed from a canvass.</summary>\n"
    . "        public static readonly (int Year, string State, int District, long VotesD, long VotesR, bool Read)[] Districts =\n        {\n"
    . join('', map { "            ($_->[0], \"$_->[1]\", $_->[2], $_->[3]L, $_->[4]L, " . ($_->[5] ? 'true' : 'false') . "),\n" } @drows) . "        };\n\n"
    . "        /// <summary>US-5: 2024's candidates, a row a column of the FEC's workbook - its national vote (the Total row, held to the column's states).\n"
    . "        /// Ticket: R or D for the nominee NARA names, a hyphen for every other column (Nevada's none-of-these line and the scattered write-ins among\n"
    . "        /// them). No quote in this comment: a source reader keeps a commented line that holds one.</summary>\n"
    . "        public static readonly (int Year, string Candidate, string Ticket, long Votes)[] Candidates =\n        {\n"
    . join('', map { "            ($_->[0], \"$_->[1]\", \"$_->[2]\", $_->[3]L),\n" } @candidates) . "        };\n\n"
    . "        /// <summary>US-5: the House of Representatives, a row a state a year, by the Clerk of the House's recapitulation - each party's own ballot\n"
    . "        /// line, each race as the Clerk prints it (Louisiana's runoff districts the finalists' December vote beside the others' November vote,\n"
    . "        /// Maine's finalists at their last ranked-choice round - in 2022 an eliminated candidate's first-round vote beside them - Alaska's first\n"
    . "        /// choices); VotesOther every other column - the non-votes some states report and Maine's ranked-choice lines, which count some ballots\n"
    . "        /// again, among them; VotesTotal the Clerk's.</summary>\n"
    . "        public static readonly (int Year, string State, long VotesR, long VotesD, long VotesOther, long VotesTotal)[] House =\n        {\n";
for my $year (@house_years) { for my $s (@house_states) { my $e = $house{$year}{$s}; $cs .= "            ($year, \"$s\", $e->{R}L, $e->{D}L, $e->{other}L, $e->{total}L),\n"; } }
$cs .= "        };\n    }\n}\n";
$cs =~ s/\n/\r\n/g;
write_all("$usa/president_by_year.csv" => $csv_y, "$usa/president_by_state.csv" => $csv_s, "$usa/president_by_district.csv" => $csv_d,
          "$usa/president_by_candidate.csv" => $csv_c, "$usa/house_by_state.csv" => $csv_h, 'Assets/Scripts/Elections/Generated/UsPresidentialReturns.cs' => $cs);

for my $year (@years) {
    my $n = $nara{$year};
    my ($w, $o) = ($n->{winner}, $n->{opponent});
    my %v = (D => 0, R => 0, O => 0, T => 0);
    for my $s (@order) { my $e = $y{$year}{st}{$s}; $v{D} += $e->{vD}; $v{R} += $e->{vR}; $v{O} += $e->{other}; $v{T} += $e->{total}; }
    my @to = map { "$_ $y{$year}{st}{$_}{cast_other_to}" } grep { $y{$year}{st}{$_}{cast_other} } @order;
    printf "%d: %s (%s) %d - %s (%s) %d as cast, %d - %d by the record's winners = NARA's table EXACT%s; votes D %d R %d others %d total %d\n", $year,
        $n->{name}{$w}, $w, $n->{wn}, $n->{name}{$o}, $o, $n->{on}, $pledged{$year}{$w}, $pledged{$year}{$o}, (@to ? ' (for other persons: ' . join('; ', @to) . ')' : ''), $v{D}, $v{R}, $v{O}, $v{T};
}
printf "2024's candidates: %s\n", join('; ', map { "$_->[1]" . ($_->[2] ne '-' ? " ($_->[2])" : '') . " $_->[3]" } @candidates);
for my $year (@house_years) {
    my $k = $house_tot{$year};
    my $line = sprintf "House %d by the Clerk: R %d D %d others %d total %d, two-party R %.4f%%", $year, $k->{R}, $k->{D}, $k->{total} - $k->{R} - $k->{D}, $k->{total}, 100 * $k->{R} / ($k->{R} + $k->{D});
    if (my $f = $house_fec{$year}) {
        my ($fr, $fd, $fo) = (0, 0, 0);
        for my $s (@house_states) { $fr += $f->{$s}{R}; $fd += $f->{$s}{D}; $fo += $f->{$s}{O}; }
        my @diff = map { sprintf '%s D %+d R %+d', $_, $f->{$_}{D} - $house{$year}{$_}{D}, $f->{$_}{R} - $house{$year}{$_}{R} } grep { $f->{$_}{D} != $house{$year}{$_}{D} || $f->{$_}{R} != $house{$year}{$_}{R} } @house_states;
        $line .= sprintf "; the FEC's table, 50 states: R %d D %d others %d, two-party R %.4f%% - %d of 50 states identical; the FEC less the Clerk: %s", $fr, $fd, $fo, 100 * $fr / ($fr + $fd), 50 - @diff, join('; ', @diff);
    }
    else { $line .= "; no FEC volume of $year is served" }
    print "$line\n";
}
my ($extractor) = qx("$pdftotext" -v 2>&1) =~ /^(pdftotext version \S+)/m;
printf "%d pages read and held to their sums (text by %s); %d state rows, %d district rows, %d candidate rows, %d House rows; wrote president_by_year.csv, president_by_state.csv, president_by_district.csv, president_by_candidate.csv, house_by_state.csv, UsPresidentialReturns.cs\n",
    scalar(keys %read), $extractor // "$pdftotext, version unread", 51 * @years, scalar(@drows), scalar(@candidates), 50 * @house_years;
