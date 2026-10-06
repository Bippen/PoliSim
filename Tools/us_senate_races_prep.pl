#!/usr/bin/perl
# THE SENATE RACES (PS-6 US-12 part two, COMPLETED.md s793): every Senate race of the general elections of 2018 and 2024 - the Class I seats
# and the specials printed beside them - and Nebraska's Class II race of 2020 (the base of its 2024 special), read from the Clerk of the
# House's election statistics; the record is ElectionsData/usa/senate_record.md (its race half). The catalog only: no seat method, no
# measurement (R-US19's details are declared before any measurement, as R-US18's were at s791).
# It reads ONLY saved pages, each held to its SHA256SUMS.txt line first:
#   - raw/returns/clerk_statistics<year>.pdf, 2018, 2020 and 2024, twice: by pdftotext -raw (the text in reading order) and by pdftotext
#     -table (each entry's label and figure on one line, by where they stand) - the two must agree, state by state, entry for entry;
#   - raw/returns/fec_federalelections2018.xlsx (the FEC's 2018 Senate sheet: the cross-check of 2018);
# and ElectionsData/usa/senate_seats.csv (s792's catalog), which every winner is held to.
# It writes ElectionsData/usa/senate_races.csv and Assets/Scripts/Elections/Generated/UsSenateRaces.cs (a part of the partial class
# UsPresidentialReturns). It dies, writing nothing, on:
#   - a page off its digest, or in no SHA256SUMS.txt;
#   - (the listings) a state's Senate listing not found, or twice; an entry without a figure, or a figure not grouped by thousands but where
#     a footnote mark is declared; a line in the listing neither an entry nor a heading; a mark whose runoff footnote is missing or names
#     another candidate; a term heading not of the two forms, or a state of one race carrying one; a full term not beginning on 3 January
#     after the election; a race of a term the year does not hold; an entry with no name outside a fusion state but the one declared; a fusion
#     line in a state with none, or with no candidate before it; a label not declared to a class; a tie for first;
#   - the -table reading's entries not the -raw reading's, state by state, in order;
#   - (the recapitulations) a state's Senator row whose cells do not sum to its Total, or whose Total is not the state's races' entries;
#   - the races not the declared ones: 2018 and 2024 a race in each of the 33 Class I states, the specials declared, 2020 Nebraska alone;
#   - (Maine) a winner without half the candidates' votes - its single count would then be a first round;
#   - (the FEC, 2018) a race without one W, the W's party not the Clerk's winner's, or the W's votes (the winner's: the fused total, the
#     runoff, or the general) not the Clerk's but where declared - no loser's figure is compared; the unnamed entry declared not the FEC's
#     one line of her code, her name and her votes;
#   - (the winners) a winner not the holder of his seat by s792's catalog on the day it is held;
#   - a perl warning during the checks; a non-ASCII byte in an output, or a CSV row whose fields are not its heading's.
# All outputs are built and tested before the first is written.
# Usage: perl Tools/us_senate_races_prep.pl   (from the project root, Git for Windows' perl; pdftotext at /mingw64/bin or on PATH)
# ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw($UnzipError);

my $usa = 'ElectionsData/usa';
my $raw = "$usa/raw";
my @years = (2018, 2020, 2024);
my $pdftotext = -x '/mingw64/bin/pdftotext' ? '/mingw64/bin/pdftotext' : 'pdftotext';
my @problems;
sub problem { push @problems, join('', @_); return; }
$SIG{__WARN__} = sub { problem('a perl warning: ', $_[0] =~ s/\s+$//r) };
$SIG{__DIE__} = sub { return if $^S; print STDERR "MISMATCH: $_\n" for splice @problems; };

# ---------------------------------------------------------------- the saved pages, each held to its SHA256SUMS.txt line before it is used
my %sums;
{
    open my $s, '<', "$raw/returns/SHA256SUMS.txt" or die "$raw/returns/SHA256SUMS.txt: $!\n";
    while (my $l = <$s>) { $sums{"returns/$2"} = $1 if $l =~ /^([0-9a-f]{64}) \*(.+?)\s*$/; }
    close $s;
}
my %read;
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
sub pdf_text {
    my ($rel, $mode) = @_;
    page($rel);
    open my $p, '-|', $pdftotext, $mode, "$raw/$rel", '-' or die "$pdftotext: $!\n";
    my $text = do { local $/; <$p> };
    close $p or die "$pdftotext $rel: exit " . ($? >> 8) . "\n";
    $text =~ s/\r//g;
    return $text;
}
sub ent { my $s = shift; $s =~ s/&lt;/</g; $s =~ s/&gt;/>/g; $s =~ s/&quot;/"/g; $s =~ s/&apos;/'/g; $s =~ s/&#(\d+);/chr($1)/ge; $s =~ s/&#x([0-9a-fA-F]+);/chr(hex $1)/ge; $s =~ s/&amp;/&/g; return $s; }
sub colnum { my $c = shift; my $n = 0; $n = $n * 26 + (ord($_) - 64) for split //, $c; return $n; }
sub xlsx_sheet {   # us_house_prep.pl's reader: a sheet as { row => { col => value } }
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
sub valid_count { return shift =~ /^\d{1,3}(?:,\d{3})*$/; }
sub num { (my $s = shift) =~ s/,//g; return $s + 0; }

my %code = ('Alabama' => 'AL', 'Alaska' => 'AK', 'Arizona' => 'AZ', 'Arkansas' => 'AR', 'California' => 'CA', 'Colorado' => 'CO', 'Connecticut' => 'CT',
    'Delaware' => 'DE', 'Florida' => 'FL', 'Georgia' => 'GA', 'Hawaii' => 'HI', 'Idaho' => 'ID', 'Illinois' => 'IL',
    'Indiana' => 'IN', 'Iowa' => 'IA', 'Kansas' => 'KS', 'Kentucky' => 'KY', 'Louisiana' => 'LA', 'Maine' => 'ME', 'Maryland' => 'MD',
    'Massachusetts' => 'MA', 'Michigan' => 'MI', 'Minnesota' => 'MN', 'Mississippi' => 'MS', 'Missouri' => 'MO', 'Montana' => 'MT', 'Nebraska' => 'NE',
    'Nevada' => 'NV', 'New Hampshire' => 'NH', 'New Jersey' => 'NJ', 'New Mexico' => 'NM', 'New York' => 'NY', 'North Carolina' => 'NC',
    'North Dakota' => 'ND', 'Ohio' => 'OH', 'Oklahoma' => 'OK', 'Oregon' => 'OR', 'Pennsylvania' => 'PA', 'Rhode Island' => 'RI',
    'South Carolina' => 'SC', 'South Dakota' => 'SD', 'Tennessee' => 'TN', 'Texas' => 'TX', 'Utah' => 'UT', 'Vermont' => 'VT', 'Virginia' => 'VA',
    'Washington' => 'WA', 'West Virginia' => 'WV', 'Wisconsin' => 'WI', 'Wyoming' => 'WY');
my %upper = map { uc($_) => $code{$_} } keys %code;
my %territory = map { $_ => 1 } ('AMERICAN SAMOA', 'DISTRICT OF COLUMBIA', 'GUAM', 'NORTHERN MARIANA ISLANDS', 'PUERTO RICO', 'VIRGIN ISLANDS');

# ---------------------------------------------------------------- the declared tables
# the 33 Class I states (s792's catalog gives each state's classes; held below to it)
# the races of each year beyond one full term a state: the specials, their class and term as the volumes print them
my %special = (
    '2018 MN' => { class => 2, ends => 'January 3, 2021' }, '2018 MS' => { class => 2, ends => 'January 3, 2021' },
    '2024 CA' => { class => 1, ends => 'January 3, 2025' }, '2024 NE' => { class => 2, ends => 'January 3, 2027' },
);
my %only = (2020 => { NE => 2 });   # 2020: Nebraska's Class II race alone - the base of its 2024 special
# each label to its class: D, R, I (an independent, no party), O (another party, or a write-in whatever party it names)
my %label_class = (
    'Democrat' => 'D', 'Democratic' => 'D', 'Democratic-Nonpartisan League' => 'D', 'Democratic-Farmer-Labor' => 'D',
    'Republican' => 'R',
    'Independent' => 'I', 'Unaffiliated' => 'I', 'No Political Party' => 'I', 'No Party Affiliation' => 'I', 'By Petition' => 'I',
    'Independent American' => 'O', 'Independence-Alliance' => 'O',
);
# a label that names a major party or an independent in a form not declared above stops the run (us_house_prep.pl's "GOP" lesson)
sub label_class {
    my $l = shift;
    return $label_class{$l} if exists $label_class{$l};
    return 'O' if $l =~ /^Write-in(?: \(.*\))?$/;   # a write-in candidate's line, whatever party it names
    return undef if $l =~ /democrat|republic|independ|nonpartisan|gop\b/i;   # New York's nameless "Independence" line is fusion (below), never this
    return 'O';
}
my %fusion_label = map { $_ => 1 } ('Working Families', 'Independence', "Women's Equality", 'Conservative', 'Reform');
my %fusion_state = map { $_ => 1 } qw(CT NY);
my %noncand = map { lc($_) => 1 } ('Blank Votes', 'Blank', 'Blanks', 'Over Votes', 'Under Votes', 'Void', 'Others', 'All Others', 'Scattering',
    'Scatterings', 'Write-in', 'Write-in (Scattering)', 'Write-in, Scatterings', 'Other Write-ins', 'None of These Candidates', 'Write-ins',
    'Scattered', 'Miscellaneous');
my %unnamed_ok = ('2018 AZ' => 'Green');   # the FEC names her: Angela Green, W(GRE)/GRE
my %unnamed_used;
my %suffix = map { $_ => 1 } ('Jr.', 'Sr.', 'II', 'III', 'IV');
# footnote marks glued to figures: declared race by race - Mississippi's 2018 special, its two finalists at the 27 Nov runoff
my %marked = ('2018 MS 2' => 2);

# ---------------------------------------------------------------- the listings, read twice
sub read_volume {   # { XX => { races => [ { term => 'full'|'unexpired', ends, ent => [ {label, fig, mark} ] } ], recap => [cells] } }
    my ($year, $mode) = @_;
    my $file = "returns/clerk_statistics$year.pdf";
    my @L = split /\n/, pdf_text($file, $mode), -1;
    my (%out, $state, $sect, $race);
    for my $i (0 .. $#L) {
        my $t = $L[$i];
        $t =~ s/^\f+//;
        $t = trim($t);
        next if $t eq '' || $t =~ /^\(?\d{1,3}\)?$/;   # a page number: n, or (n) on the first page
        if ($t =~ /^([A-Z][A-Z. ]*[A-Z])(--Continued)?$/ && ($upper{$1} || $territory{$1})) {
            my $st = $upper{$1} // "-$1";
            if (($state // '') ne $st) { $state = $st; $sect = ''; $race = undef; }
            next;
        }
        next unless defined $state && $state !~ /^-/;
        # a runoff footnote (Mississippi 2018): its mark, the runoff's day, the candidate it names (the -raw reading: one line each)
        if ($mode eq '-raw' && $t =~ /^(\d)This vote count is from the runoff election held on ([A-Z][a-z]+ \d+, \d{4})\. (.+?) received ([\d,]+) votes in the general election/) {
            $out{$state}{runoff_note}{$1} = { day => $2, name => $3, general => $4 };
            next;
        }
        if ($t =~ /^FOR UNITED STATES SENATOR(--Continued)?$/) {
            if ($1) { problem("$file ($mode) $state line " . ($i + 1) . ": a Senate listing continued with none begun") unless $out{$state}{races}; $sect = 'SENATE'; next; }
            problem("$file ($mode) $state: two Senate listings") if $out{$state}{races};
            $sect = 'SENATE'; $out{$state}{races} = []; $race = undef;
            next;
        }
        if ($t =~ /^FOR /) { $sect = ''; next; }
        if ($t =~ /^Recapitulation of Votes Cast in (.+?)(?:--Continued)?$/) { $sect = 'RECAP'; next; }
        if ($sect eq 'SENATE') {
            if ($t =~ /^\(For (unexpired|full) term (?:ending|beginning) ([A-Z][a-z]+ \d+, \d{4})\)$/) {
                $race = { term => $1, when => $2, ent => [] }; push @{$out{$state}{races}}, $race; next;
            }
            if ($t =~ /^\(For /) { problem("$file ($mode) $state: a term heading '$t'"); next; }
            if ($t =~ /^(.*?) ?\.{2,}\s*([\d,]+)$/) {
                my ($label, $fig) = ($1, $2);
                if (!$race) { $race = { term => 'full', when => '', ent => [] }; push @{$out{$state}{races}}, $race; }
                push @{$race->{ent}}, { label => $label, fig => $fig, line => $i + 1 };
                next;
            }
            if ($t =~ /\.{2,}\s*$/) { problem("$file ($mode) $state line " . ($i + 1) . ": the entry '$t' without a figure"); next; }
            problem("$file ($mode) $state line " . ($i + 1) . ": a line in the Senate listing that is no entry: '$t'");   # a wrapped entry would land here
            next;
        }
        if ($sect eq 'RECAP' && $t =~ /^Senator\b(.*)$/) { push @{$out{$state}{recap}}, grep { /^[\d,]+$/ } split / /, $1; }
    }
    return \%out;
}

my @fec_note;
my %race;   # "year XX class" => { year, st, class, term, cands => [ {name, label, class, total} ], non, winner, ... }
for my $year (@years) {
    my $rawv = read_volume($year, '-raw');
    my $tabv = read_volume($year, '-table');
    for my $st (sort keys %$rawv) {
        next unless $rawv->{$st}{races};
        my $a = join '|', map { my $r = $_; ("(" . $r->{term} . ")", map { "$_->{label}=$_->{fig}" } @{$r->{ent}}) } @{$rawv->{$st}{races}};
        my $b = join '|', map { my $r = $_; ("(" . $r->{term} . ")", map { "$_->{label}=$_->{fig}" } @{$r->{ent}}) } @{($tabv->{$st} // {})->{races} // []};
        $a =~ s/\s+/ /g; $b =~ s/\s+/ /g;
        if ($a ne $b) {
            my @x = split /\|/, $a; my @y = split /\|/, $b;
            my ($k) = grep { ($x[$_] // '') ne ($y[$_] // '') } 0 .. ($#x > $#y ? $#x : $#y);
            problem("$year $st: the -table reading's entries are not the -raw reading's - entry $k: '" . ($x[$k] // 'none') . "' against '" . ($y[$k] // 'none') . "'");
        }
    }
    for my $st (sort keys %$tabv) { problem("$year $st: a Senate listing in the -table reading alone") if $tabv->{$st}{races} && !$rawv->{$st}{races}; }
    my @states = grep { $rawv->{$_}{races} } sort keys %$rawv;
    @states = grep { $only{$year}{$_} } @states if $only{$year};
    for my $st (@states) {
        my @rs = @{$rawv->{$st}{races}};
        my $sp = $special{"$year $st"};
        if (@rs == 2) {
            problem("$year $st: two races, no special declared") unless $sp;
        } elsif (@rs == 1) {
            problem("$year $st: one race, a special declared") if $sp;
            problem("$year $st: one race under a term heading") if $rs[0]{when} ne '';
        } else { problem("$year $st: " . scalar(@rs) . " races"); next; }
        for my $r (@rs) {
            my $class;
            if ($only{$year}) { $class = $only{$year}{$st}; }
            elsif ($r->{term} eq 'unexpired') {
                $class = $sp ? $sp->{class} : 0;
                problem("$year $st: an unexpired term ending $r->{when}, declared $sp->{ends}") if $sp && $r->{when} ne $sp->{ends};
            } else {
                $class = 1;
                problem("$year $st: a full term beginning $r->{when}, not January 3, " . ($year + 1)) if $r->{when} ne '' && $r->{when} ne 'January 3, ' . ($year + 1);
            }
            my $key = "$year $st $class" . ($r->{term} eq 'unexpired' && $class == 1 ? 'u' : '');
            my $x = $race{$key} = { year => $year, st => $st, class => $class, term => $r->{term}, cands => [], non => 0, flags => {}, sum => 0 };
            my $marks = $marked{"$year $st $class"} // 0;
            for my $e (@{$r->{ent}}) {
                my $fig = $e->{fig};
                if (!valid_count($fig)) {
                    if ($marks && $fig =~ /^(\d)(\d{1,3}(?:,\d{3})+)$/ && valid_count($2)) { $fig = $2; $e->{mark} = $1; $marks--; $x->{flags}{M} = 1; }
                    else { problem("$year $st: the figure '$e->{fig}' of '$e->{label}', not grouped by thousands"); next; }
                }
                my $v = num($fig);
                $x->{sum} += $v;
                my $label = trim($e->{label});
                my @parts = split /, /, $label;
                if ($noncand{lc $label}) { $x->{non} += $v; next; }
                if ($fusion_label{$label} && @parts == 1) {
                    if (!$fusion_state{$st}) { problem("$year $st: the party line '$label' outside a fusion state"); next; }
                    my $of = $x->{cands}[-1] or do { problem("$year $st: the party line '$label' with no candidate before it"); next; };
                    $of->{total} += $v; $of->{lines}++; $x->{flags}{F} = 1;
                    next;
                }
                if (@parts == 1) {
                    if (($unnamed_ok{"$year $st"} // '') eq $label) { push @{$x->{cands}}, { name => "(unnamed, $label)", label => $label, class => 'O', total => $v }; $unnamed_used{"$year $st"} = 1; $x->{flags}{U} = 1; next; }
                    problem("$year $st: the entry '$label' - no name, outside a fusion state, not declared");
                    next;
                }
                my @nm = (shift @parts);
                push @nm, shift @parts while @parts > 1 && $suffix{$parts[0]};
                my $lab = join ', ', @parts;
                my $cl = label_class($lab);
                if (!defined $cl) { problem("$year $st: the label '$lab' ($nm[0]) names a party in a form not declared"); next; }
                push @{$x->{cands}}, { name => join(', ', @nm), label => $lab, class => $cl, total => $v };
                if (defined $e->{mark}) {   # a mark is its footnote's: a runoff's count, the footnote naming this candidate
                    my $n = $rawv->{$st}{runoff_note}{$e->{mark}};
                    problem("$year $st: the mark $e->{mark} on $nm[0], " . ($n ? "its footnote naming $n->{name}" : 'no runoff footnote with that mark'))
                        unless $n && last_word($n->{name}) eq last_word($nm[0]);
                }
            }
            problem("$year $st class $class: $marks footnote mark(s) declared and not found") if $marks;
            my @c = sort { $b->{total} <=> $a->{total} } @{$x->{cands}};
            if (@c < 1) { problem("$year $st: a race with no candidate"); next; }
            problem("$year $st class $class: a tie for first") if @c > 1 && $c[0]{total} == $c[1]{total};
            $x->{winner} = $c[0];
            my %n; $n{$_->{class}}++ for @{$x->{cands}};
            $x->{flags}{S} = 1 if @c > 1 && $c[0]{class} eq $c[1]{class};   # the first two of one party (California's top two)
            $x->{flags}{N} = 1 unless $n{D};   # no Democrat on the ballot
        }
        # the state's recapitulation: its Senator row's cells sum to its last cell, the Total, and the Total is the state's races' entries
        my @cells = map { num($_) } @{$rawv->{$st}{recap} // []};
        if (@cells < 2) { problem("$year $st: no Senator row in the state's recapitulation"); }
        else {
            my $tot = pop @cells;
            my $s = 0; $s += $_ for @cells;
            my $races = 0; $races += $race{$_}{sum} for grep { $race{$_}{year} == $year && $race{$_}{st} eq $st } keys %race;
            problem("$year $st: the Senator row's cells sum to $s, its Total $tot") unless $s == $tot;
            problem("$year $st: the Senator row's Total $tot, the races' entries $races") unless $tot == $races;
        }
    }
    for my $k (sort keys %unnamed_ok) { problem("$k: the unnamed '$unnamed_ok{$k}' declared and not found") if $k =~ /^$year / && !$unnamed_used{$k}; }
}

# ---------------------------------------------------------------- the races the item holds: 33 Class I states each year, the specials, 2020 Nebraska
my %class_of;   # by s792's catalog: XX => { classes }
my @seat_rows;
{
    open my $h, '<', "$usa/senate_seats.csv" or die "$usa/senate_seats.csv: $!\n";
    my @hdr;
    while (<$h>) { s/\r?\n$//; next if /^#/; my @c = split /,/; if (!@hdr) { @hdr = @c; next; } my %r; @r{@hdr} = @c; push @seat_rows, \%r; $class_of{$r{state}}{$r{class}} = 1; }
    close $h;
    $read{"$usa/senate_seats.csv"} = do { open my $b, '<:raw', "$usa/senate_seats.csv" or die; local $/; sha256_hex(<$b>) };
}
my @class1 = sort grep { $class_of{$_}{1} } keys %class_of;
problem("s792's catalog: " . scalar(@class1) . " Class I states, not 33") unless @class1 == 33;
for my $year (2018, 2024) {
    my %have = map { $_->{st} => 1 } grep { $_->{year} == $year && $_->{class} == 1 && $_->{term} eq 'full' } values %race;
    my @miss = grep { !$have{$_} } @class1;
    my @extra = grep { !$class_of{$_}{1} } sort keys %have;
    problem("$year: the Class I states without a full-term race: @miss; races outside Class I: @extra") if @miss || @extra;
}
my @y2020 = grep { $_->{year} == 2020 } values %race;
problem("2020: " . scalar(@y2020) . " races read, not Nebraska's alone") unless @y2020 == 1 && $y2020[0]{st} eq 'NE';

# ---------------------------------------------------------------- Maine: a single count is a first round only if its winner has half the candidates' votes
for my $x (grep { $_->{st} eq 'ME' } values %race) {
    my $c = 0; $c += $_->{total} for @{$x->{cands}};
    problem("$x->{year} ME: its winner $x->{winner}{total} of the candidates' $c - no majority, so its single count may not be a final round") unless 2 * $x->{winner}{total} > $c;
}

# ---------------------------------------------------------------- the FEC's 2018 Senate sheet: one W a race, its party the Clerk's winner's
{
    my $c = xlsx_sheet(page('returns/fec_federalelections2018.xlsx'), '2018 US Senate Results by State', 'fec_federalelections2018.xlsx');
    my ($hr) = grep { my $r = $_; grep { ($c->{$r}{$_} // '') eq 'GE WINNER INDICATOR' } keys %{$c->{$r}} } sort { $a <=> $b } keys %$c;
    die "the FEC's 2018 Senate sheet: no heading row\n" unless $hr;
    my %col; for my $k (keys %{$c->{$hr}}) { $col{trim($c->{$hr}{$k})} = $k; }
    $col{D} //= $col{DISTRICT};   # the term: 'S', 'S-FULL TERM', 'S-UNEXPIRED TERM' (headed DISTRICT on the Senate sheet)
    for my $need ('STATE ABBREVIATION', 'D', 'PARTY', 'GENERAL VOTES', 'GE WINNER INDICATOR') { die "the FEC's 2018 Senate sheet: no column '$need'\n" unless $col{$need}; }
    my %w;   # "XX full|unexpired" => [ [party code, the votes that decided: the fusion total, the runoff, or the general] ]
    my ($fus, $run) = ($col{'COMBINED GE PARTY TOTALS (CT, NY)'}, $col{'GE RUNOFF ELECTION VOTES (MS Senate)'});
    die "the FEC's 2018 Senate sheet: no fusion-total or runoff column\n" unless $fus && $run;
    # the unnamed entry declared above, held to the FEC's line of her party in that race: its votes the Clerk's entry's
    my %unnamed_fec = ('AZ full' => 'W(GRE)/GRE');   # the FEC's code: a write-in in the primary, the Green line in the general
    my %unnamed_seen;
    for my $r (sort { $a <=> $b } keys %$c) {
        next if $r <= $hr;
        {
            my ($st, $d, $p) = map { trim($c->{$r}{$col{$_}} // '') } ('STATE ABBREVIATION', 'D', 'PARTY');
            my $k = $st . ($d =~ /UNEXPIRED/ ? ' unexpired' : ' full');
            if (exists $unnamed_fec{$k} && $unnamed_fec{$k} eq $p && trim($c->{$r}{$col{'GENERAL VOTES'}} // '') ne '') {
                my ($x) = grep { $_->{year} == 2018 && "$_->{st} $_->{term}" eq $k } values %race;
                my ($u) = $x ? grep { $_->{name} =~ /^\(unnamed/ } @{$x->{cands}} : ();
                my $fv = num(trim($c->{$r}{$col{'GENERAL VOTES'}}));
                my $ln = trim($c->{$r}{$col{'CANDIDATE NAME (Last)'} // ''} // '');
                problem("FEC 2018 $k: the $p line, $ln, $fv votes; the Clerk's unnamed entry " . ($u ? "$u->{label}, $u->{total}" : 'none')) unless $u && $u->{total} == $fv && $ln eq $u->{label};
                $unnamed_seen{$k}++;
            }
        }
        next unless trim($c->{$r}{$col{'GE WINNER INDICATOR'}} // '') eq 'W';
        my ($st, $d, $p) = map { trim($c->{$r}{$col{$_}} // '') } ('STATE ABBREVIATION', 'D', 'PARTY');
        my $term = $d =~ /UNEXPIRED/ ? 'unexpired' : 'full';
        my $v = trim($c->{$r}{$run} // '') ne '' ? $c->{$r}{$run} : trim($c->{$r}{$fus} // '') ne '' ? $c->{$r}{$fus} : $c->{$r}{$col{'GENERAL VOTES'}};
        push @{$w{"$st $term"}}, [$p, num(trim($v // 0))];
    }
    my %fec_class = (D => 'D', DFL => 'D', R => 'R', REP => 'R', DEM => 'D', IND => 'I', N => '?');
    # the one race whose winner's votes the FEC prints larger than the Clerk, no saved page saying why: Ohio - both printed, the Clerk's used
    my %votes_differ = ('OH full' => 1);
    for my $x (grep { $_->{year} == 2018 } values %race) {
        my $k = "$x->{st} $x->{term}";
        my @p = @{$w{$k} // []};
        if (@p != 1) { problem("FEC 2018 $k: " . scalar(@p) . " W marks, not one"); next; }
        my ($code, $v) = @{$p[0]};
        my $fc = $fec_class{$code} // 'O';
        # Mississippi's special: the FEC codes its candidates Nonpartisan (N); its W is held to the Clerk's winner by the runoff's votes
        problem("FEC 2018 $k: the W's party $code, the Clerk's winner $x->{winner}{name} ($x->{winner}{class})") unless $fc eq $x->{winner}{class} || ($fc eq '?' && $k eq 'MS unexpired');
        if ($votes_differ{$k}) { push @fec_note, "$k: the FEC's winner $v, the Clerk's $x->{winner}{total} (DECLARED: no saved page explains it; the Clerk's figure used)"; $votes_differ{$k} = 2; next; }
        problem("FEC 2018 $k: the W's votes $v, the Clerk's winner $x->{winner}{name} $x->{winner}{total}") unless $v == $x->{winner}{total};
    }
    for my $k (sort keys %votes_differ) { problem("FEC 2018 $k: a difference declared and not met") unless $votes_differ{$k} == 2; }
    for my $k (sort keys %unnamed_fec) { problem("FEC 2018 $k: " . ($unnamed_seen{$k} // 0) . " $unnamed_fec{$k} lines, not one") unless ($unnamed_seen{$k} // 0) == 1; }
}

# ---------------------------------------------------------------- the winners against s792's catalog: each the holder of his seat on the day
sub holder_on {   # the seat's holder (state, class) at the end of a day, by s792's catalog
    my ($st, $cl, $d) = @_;
    my @h = grep { $_->{state} eq $st && $_->{class} == $cl && $_->{took} le $d && ($_->{left} eq '-' || $_->{left} gt $d) } @seat_rows;
    return @h == 1 ? $h[0] : undef;
}
sub last_word { my $n = shift; $n =~ s/\([^)]*\)//g; $n =~ s/,? (?:Jr|Sr|II|III|IV)\.?\s*$//; $n =~ s/[.,]//g; $n =~ s/\s+$//; return lc((split / /, $n)[-1]); }
for my $x (sort { $a->{year} <=> $b->{year} || $a->{st} cmp $b->{st} } values %race) {
    # the day each race's winner is held to: 2018 and 2020 winners on the window's first day (s792's catalog starts on 3 Jan 2023); 2024's
    # full terms after the January 2025 oaths; Nebraska's 2024 special (Class II) on the 119th's first day; California's 2024 special on the
    # day its winner was sworn to the old term
    my $d = $x->{year} < 2024 ? '2023-01-03' : $x->{class} == 2 ? '2025-01-03' : $x->{term} eq 'unexpired' ? '2024-12-09' : '2025-01-21';
    my $h = holder_on($x->{st}, $x->{class}, $d);
    my $who = $h ? $h->{senator} : 'no one';
    problem("$x->{year} $x->{st} class $x->{class} ($x->{term}): the winner $x->{winner}{name}, the seat's holder on $d $who")
        unless $h && last_word($h->{senator}) eq last_word($x->{winner}{name});
}

$SIG{__WARN__} = 'DEFAULT';
if (@problems) { my $n = @problems; print STDERR "MISMATCH: $_\n" for splice @problems; die "$n mismatch(es) - nothing written\n"; }

# ---------------------------------------------------------------- the CSV and the catalog's part
sub write_all {
    my @out = @_;
    for (my $i = 0; $i < @out; $i += 2) {
        my ($f, $t) = @out[$i, $i + 1];
        if ($t =~ /[^\x00-\x7F]/) { my $line = 1 + (substr($t, 0, $-[0]) =~ tr/\n//); die "$f: not ASCII at its line $line - nothing written\n"; }
        next unless $f =~ /\.csv$/;
        my ($head, @rows) = grep { !/^#/ } split /\n/, $t;
        my $n = () = $head =~ /,/g;
        for my $r (@rows) { my $k = () = $r =~ /,/g; die "$f: '$r' has " . ($k + 1) . " fields, its heading " . ($n + 1) . " - nothing written\n" unless $k == $n; }
    }
    for (my $i = 0; $i < @out; $i += 2) { my ($f, $t) = @out[$i, $i + 1]; open my $w, '>:raw', $f or die "$f: $!\n"; print $w $t or die "$f: $!\n"; close $w or die "$f: $!\n"; }
}
my %flag_said = (F => "fusion: a candidate on several ballot lines, his lines summed", M => 'a footnote mark read off a figure (Mississippi\'s 2018 special: its two finalists at the 27 Nov runoff, their figures beside the others\' 6 Nov ones as the Clerk prints them)',
    N => 'no Democrat on the ballot', S => "the first two of one party (California's top two)", U => 'an entry with no name, a candidate as the FEC names it');
my $gen = "# GENERATED by Tools/us_senate_races_prep.pl from the pages under ElectionsData/usa/raw/ - DO NOT EDIT; the record is ElectionsData/usa/senate_record.md.\n";
my $csv = $gen . "# US-12: a row a Senate race - every race of the 2018 and 2024 general elections (the Class I seats and the specials printed beside them) and\n"
    . "# Nebraska's Class II race of 2020 - by the Clerk of the House's statistics, each state's listing FOR UNITED STATES SENATOR. term: full, or\n"
    . "# unexpired (a special). votes_r / votes_d / votes_i / votes_other: the candidates' votes by class - D a Democrat (Democratic-Nonpartisan\n"
    . "# League and Democratic-Farmer-Labor included), R a Republican, I an independent with no party (Independent, Unaffiliated, No Political\n"
    . "# Party, No Party Affiliation, By Petition), other any other party and every write-in candidate; a candidate's fusion lines summed to him.\n"
    . "# votes_non: the lines that are not candidates (blanks, over and under votes, void, scatterings, write-in totals with\n"
    . "# no name, None of These Candidates). A label naming a major party, an independent or Nonpartisan in a form not declared stops the run.\n"
    . "# cands_r / cands_d / cands_i: how many of each the Clerk printed. winner: the candidates' plurality (Mississippi's special at its runoff), his name and class.\n"
    . "# flags, a letter each (- for none):\n" . join('', map { "#   $_  $flag_said{$_}\n" } sort keys %flag_said)
    . "year,state,class,term,votes_r,votes_d,votes_i,votes_other,votes_non,cands_r,cands_d,cands_i,winner,winner_class,flags\n";
my @rows;
for my $x (sort { $a->{year} <=> $b->{year} || $a->{st} cmp $b->{st} || $a->{class} <=> $b->{class} || $a->{term} cmp $b->{term} } values %race) {
    my (%v, %n); $v{$_} = 0 for qw(R D I O); $n{$_} = 0 for qw(R D I O);
    for my $c (@{$x->{cands}}) { $v{$c->{class}} += $c->{total}; $n{$c->{class}}++; }
    my $flags = join '', sort keys %{$x->{flags}};
    (my $wn = $x->{winner}{name}) =~ s/,//g;
    my @row = ($x->{year}, $x->{st}, $x->{class}, $x->{term}, $v{R}, $v{D}, $v{I}, $v{O}, $x->{non}, $n{R}, $n{D}, $n{I}, $wn, $x->{winner}{class}, $flags eq '' ? '-' : $flags);
    push @rows, \@row;
    $csv .= join(',', @row) . "\n";
}
my $dg = sha256_hex($csv);
sub cq { my $s = shift; $s =~ s/\\/\\\\/g; $s =~ s/"/\\"/g; return "\"$s\""; }
my $cs = "// GENERATED by Tools/us_senate_races_prep.pl. DO NOT EDIT BY HAND.\n//\n"
    . "// Source: ElectionsData/usa/senate_races.csv (its SHA-256 below), written by the same run from the saved pages listed in SenateRaceRawSources.\n"
    . "// GeneratedCatalogCheck re-reads the CSV and compares every figure, and re-hashes every page listed.\n\n"
    . "namespace PoliSim.Elections.Generated\n{\n"
    . "    // PS-6 US-12 (COMPLETED.md s793): the Senate races of 2018 and 2024 and Nebraska's of 2020 - a part of the partial class\n"
    . "    // UsPresidentialReturns (UsPresidentialReturns.cs, written by Tools/us_returns_prep.pl). Generated, never hand-edited; built by a method.\n"
    . "    public static partial class UsPresidentialReturns\n    {\n"
    . "        public const string SenateRaceSourceDigest = \"$dg\";\n\n"
    . "        /// <summary>Every saved page the Senate races run read, by its path under `ElectionsData/usa/` (s792's seats catalog among them), with its SHA-256.</summary>\n"
    . "        public static readonly (string Path, string Sha256)[] SenateRaceRawSources = BuildSenateRaceRawSources();\n\n"
    . "        /// <summary>A row a Senate race: year, state, class, term (full or unexpired), the candidates' votes by class (R, D, I an independent,\n"
    . "        /// other), the lines that are not candidates, how many of each class the Clerk printed, the winner's name and class, and the flags\n"
    . "        /// (senate_races.csv's header says each).</summary>\n"
    . "        public static readonly (int Year, string State, int Class, string Term, long VotesR, long VotesD, long VotesI, long VotesOther, long VotesNon, int CandsR, int CandsD, int CandsI, string Winner, string WinnerClass, string Flags)[] SenateRaces = BuildSenateRaces();\n\n"
    . "        private static (string Path, string Sha256)[] BuildSenateRaceRawSources() => new (string, string)[]\n        {\n"
    . join('', map { my $p = $_; $p =~ s{^\Q$usa\E/}{}; $p =~ s{^raw/}{raw/}; "            (\"$p\", \"$read{$_}\"),\n" } sort keys %read) . "        };\n\n"
    . "        private static (int Year, string State, int Class, string Term, long VotesR, long VotesD, long VotesI, long VotesOther, long VotesNon, int CandsR, int CandsD, int CandsI, string Winner, string WinnerClass, string Flags)[] BuildSenateRaces() => new (int, string, int, string, long, long, long, long, long, int, int, int, string, string, string)[]\n        {\n"
    . join('', map { "            ($_->[0], " . cq($_->[1]) . ", $_->[2], " . cq($_->[3]) . ", $_->[4]L, $_->[5]L, $_->[6]L, $_->[7]L, $_->[8]L, $_->[9], $_->[10], $_->[11], " . cq($_->[12]) . ', ' . cq($_->[13]) . ', ' . cq($_->[14]) . "),\n" } @rows)
    . "        };\n    }\n}\n";
$cs =~ s/\n/\r\n/g;
write_all("$usa/senate_races.csv" => $csv, 'Assets/Scripts/Elections/Generated/UsSenateRaces.cs' => $cs);
my %count; $count{$_->[0]}++ for @rows;
print "races: " . join(', ', map { "$_ $count{$_}" } sort keys %count) . "\n";
print "FEC: $_
" for @fec_note;
print "flags: " . join('; ', map { "$_->[0] $_->[1] class $_->[2] $_->[3]: $_->[14]" } grep { $_->[14] ne '-' } @rows) . "\n";
print "wrote $usa/senate_races.csv (" . scalar(@rows) . " races), Assets/Scripts/Elections/Generated/UsSenateRaces.cs\n";
