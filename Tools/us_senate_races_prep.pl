#!/usr/bin/perl
# THE SENATE RACES (PS-6 US-12, COMPLETED.md s793 and s796): every Senate race of the general elections of 2016, 2018, 2020, 2022 and 2024 -
# each year's regular class and the specials printed beside it - read from the Clerk of the House's election statistics, with the counts
# R-US19 reads (s796, R-US19's catalog step: each candidate's side, the general's ballot, the base count); the record is
# ElectionsData/usa/senate_record.md (its race half). The catalog only: no seat method, no measurement (R-US19's rule is preregistered, s794).
# It reads ONLY saved pages, each held to its SHA256SUMS.txt line first:
#   - raw/returns/clerk_statistics<year>.pdf, 2016-2024, twice: by pdftotext -raw (the text in reading order) and by pdftotext -table (each
#     entry's label and figure on one line, by where they stand) - the two must agree, state by state, entry for entry;
#   - raw/returns/fec_federalelections<year>.xlsx, 2016-2022 (the FEC's Senate sheets: the cross-check, and Alaska 2022's ranked rounds);
# and ElectionsData/usa/senate_seats.csv (s792's catalog, back to 2017 at s795), which every winner is held to and which gives an
# independent's caucus.
# It writes ElectionsData/usa/senate_races.csv and Assets/Scripts/Elections/Generated/UsSenateRaces.cs (a part of the partial class
# UsPresidentialReturns). It dies, writing nothing, on:
#   - a page off its digest, or in no SHA256SUMS.txt;
#   - (the listings) a state's Senate listing not found, or twice; an entry without a figure, or a figure not grouped by thousands but for a
#     declared race's footnote mark (a mark is read only where its runoff footnote names the candidate: one naming another leaves the figure
#     ungrouped, or the race's marks not found); a footnote's first-round figure not grouped by thousands; a line in the listing neither an
#     entry nor a heading; a declared race's marks not found, or its finalists not two; a term heading not of the two forms; a state of one race carrying
#     one but the special declared alone (Arizona 2020), or that one not under an unexpired heading; a full term not beginning on 3 January
#     after the election; a race of a term the year does not hold; an entry with no name outside a fusion state but the one declared; a fusion
#     line in a state not declared fusion for that label, with no candidate before it, or after a runoff finalist; a label not declared to a
#     class where it names a party or an independent's form; a tie for first;
#   - the -table reading's entries not the -raw reading's, state by state, in order;
#   - (the recapitulations) a state's Senator row whose cells do not sum to its Total, or whose Total is not the state's races' entries;
#   - the races not the declared ones: each year a full-term race in every state of its regular class and none outside it, the specials
#     declared;
#   - (Maine; Georgia and Louisiana where no runoff is printed) a winner without half the candidates' votes - its single count would then be
#     a first round;
#   - (the FEC, 2016-2022) a race whose W rows name not one candidate (a fusion candidate's lines grouped), the W's party not the Clerk's
#     winner's, the W's name not the winner's but the slip declared, or the W's votes (the runoff's, the fused total, or the general) not the
#     Clerk's but where declared - no loser's figure is compared; the unnamed entry declared not the FEC's one line of her code, her name and
#     her votes; a runoff finalist's first round by his footnote not the FEC's general figure for him; a ranked round whose votes are not all
#     the Clerk's candidates', or a ranked count with no Clerk race;
#   - (the counts) the deciding count naming another than the winner; an independent winner with no seat row, or his caucus unsourced;
#   - (the winners) a winner not the holder of his seat by s792's catalog on the day his year is held to;
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
my @years = (2016, 2018, 2020, 2022, 2024);
my %regular = (2016 => 3, 2018 => 1, 2020 => 2, 2022 => 3, 2024 => 1);   # the class each year's regular full terms fill (2 U.S.C. s1)
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
# a listing line folded to ASCII: pdftotext's UTF-8 accents to their letters (Lujan's a), as us_senate_prep.pl folds the Senate's pages
sub fold { my $s = shift; $s =~ s/\xC3\xA1/a/g; $s =~ s/\xC3\xA9/e/g; $s =~ s/\xC3\xAD/i/g; $s =~ s/\xC3\xB3/o/g; $s =~ s/\xC3\xBA/u/g; $s =~ s/\xC3\xB1/n/g;
    $s =~ tr/\xE1\xE9\xED\xF3\xFA\xF1/aeioun/; return $s; }   # and pdftotext's Latin-1 bytes, the same letters
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
# each year's regular class's states (the roster's seat rows give each state's classes; held below to them)
# the races of each year beyond one full term a state: the specials, their class and term as the volumes print them
# (alone: the state's one race that year, printed under its term heading - Arizona 2020, no Class II seat there)
my %special = (
    '2018 MN' => { class => 2, ends => 'January 3, 2021' }, '2018 MS' => { class => 2, ends => 'January 3, 2021' },
    '2020 AZ' => { class => 3, ends => 'January 3, 2023', alone => 1 }, '2020 GA' => { class => 3, ends => 'January 3, 2023' },
    '2022 CA' => { class => 3, ends => 'January 3, 2023' }, '2022 OK' => { class => 2, ends => 'January 3, 2027' },
    '2024 CA' => { class => 1, ends => 'January 3, 2025' }, '2024 NE' => { class => 2, ends => 'January 3, 2027' },
);
# each label to its class: D, R, I (an independent, no party), O (another party, or a write-in whatever party it names)
my %label_class = (
    'Democrat' => 'D', 'Democratic' => 'D', 'Democratic-Nonpartisan League' => 'D', 'Democratic-Farmer-Labor' => 'D',
    'Republican' => 'R',
    'Independent' => 'I', 'Unaffiliated' => 'I', 'No Political Party' => 'I', 'No Party Affiliation' => 'I', 'By Petition' => 'I',
    'No Party' => 'I', 'Nonaffiliated' => 'I', 'Nominated by Petition' => 'I',
    'Independent American' => 'O', 'Independence-Alliance' => 'O', 'New Independent Party Iowa' => 'O', 'Alaskan Independence' => 'O',
    'Independent Party of Delaware' => 'O',
);
# a label that names a major party or an independent in a form not declared above stops the run (us_house_prep.pl's "GOP" lesson)
sub label_class {
    my $l = shift;
    return $label_class{$l} if exists $label_class{$l};
    return 'O' if $l =~ /^Write-in(?: \(.*\))?$/;   # a write-in candidate's line, whatever party it names
    # a party's name, or an independent's in any of its forms (no party, unaffiliated, petition, unenrolled): New York's nameless "Independence"
    # line is fusion (below), never this
    return undef if $l =~ /democrat|republic|independ|nonpartisan|no party|nonaffil|unaffil|petition|unenrolled|gop\b/i;
    return 'O';
}
# fusion, state by state: the nameless party lines that follow a candidate and are his (South Carolina 2016: Dixon's Working Families and Green
# lines, Bledsoe's Libertarian line - the FEC's combined column names South Carolina that year)
my %fusion_of = (CT => { map { $_ => 1 } ('Working Families', 'Independence', "Women's Equality", 'Conservative', 'Reform') },
    NY => { map { $_ => 1 } ('Working Families', 'Independence', "Women's Equality", 'Conservative', 'Reform') },
    SC => { map { $_ => 1 } ('Working Families', 'Green', 'Libertarian') });
my %fusion_label = map { %$_ } values %fusion_of;
my %noncand = map { lc($_) => 1 } ('Blank Votes', 'Blank', 'Blanks', 'Over Votes', 'Under Votes', 'Void', 'Others', 'All Others', 'Scattering',
    'Scatterings', 'Write-in', 'Write-in (Scattering)', 'Write-in, Scatterings', 'Other Write-ins', 'None of These Candidates', 'Write-ins',
    'Scattered', 'Miscellaneous', 'Scatter', 'Spoiled Votes', 'Write-in (No Party)', 'Write-in (Miscellaneous)');
my %unnamed_ok = ('2018 AZ' => 'Green');   # the FEC names her: Angela Green, W(GRE)/GRE
my %unnamed_used;
my %suffix = map { $_ => 1 } ('Jr.', 'Sr.', 'II', 'III', 'IV');
# footnote marks glued to figures: declared race by race - each a runoff's two finalists, their figures the runoff's count beside the others'
# first-round figures as the Clerk prints them: Louisiana's 2016 Class III (10 Dec), Mississippi's 2018 special (27 Nov), Georgia's 2020
# Class II and Class III special (5 Jan 2021), Georgia's 2022 Class III (6 Dec)
my %marked = ('2016 LA 3' => 2, '2018 MS 2' => 2, '2020 GA 2' => 2, '2020 GA 3' => 2, '2022 GA 3' => 2);

# ---------------------------------------------------------------- the listings, read twice
sub read_volume {   # { XX => { races => [ { term => 'full'|'unexpired', ends, ent => [ {label, fig, mark} ] } ], recap => [cells] } }
    my ($year, $mode) = @_;
    my $file = "returns/clerk_statistics$year.pdf";
    my @L = split /\n/, pdf_text($file, $mode), -1;
    my (%out, $state, $sect, $race);
    for my $i (0 .. $#L) {
        my $t = $L[$i];
        $t =~ s/^\f+//;
        $t = trim(fold($t));
        next if $t eq '' || $t =~ /^\(?\d{1,3}\)?$/;   # a page number: n, or (n) on the first page
        if ($t =~ /^([A-Z][A-Z. ]*[A-Z])(--Continued)?$/ && ($upper{$1} || $territory{$1})) {
            my $st = $upper{$1} // "-$1";
            if (($state // '') ne $st) { $state = $st; $sect = ''; $race = undef; }
            next;
        }
        next unless defined $state && $state !~ /^-/;
        # a runoff footnote: its mark, the runoff's day, and each candidate it names with his first-round votes (the -raw reading, its
        # lines joined to the next note, heading or page number; a word hyphenated across a line joined whole)
        if ($mode eq '-raw' && $t =~ /^(\d)(This vote count is from .*)$/) {
            my ($m, $note) = ($1, $2);
            for (my $j = $i + 1; $j <= $#L; $j++) {
                my $u = trim($L[$j] =~ s/^\f+//r);
                last if $u eq '' || $u =~ /^\d+This vote count/ || $u =~ /^\(?\d{1,3}\)?$/ || $u =~ /^[A-Z][A-Z. ]*[A-Z](--Continued)?$/ || $u =~ /^FOR /;
                $note = $note =~ /[a-z]-$/ ? substr($note, 0, -1) . $u : "$note $u";
            }
            my ($day) = $note =~ /from (?:the runoff election held on |[A-Z][a-z]+'s )([A-Z][a-z]+ \d+, \d{4})/;
            my %named; while ($note =~ /(\S+) received ([\d,]+)/g) { my ($who, $v) = ($1, $2); $named{lc $who} = $v; }
            for my $w (sort keys %named) { problem("$file ($mode) $state: footnote $m gives $w '$named{$w}', not grouped by thousands") unless valid_count($named{$w}); }
            $out{$state}{runoff_note}{$m} = { day => $day // '', text => $note, named => \%named };
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
    for my $st (@states) {
        my @rs = @{$rawv->{$st}{races}};
        my $sp = $special{"$year $st"};
        if (@rs == 2) {
            problem("$year $st: two races, no special declared") unless $sp && !$sp->{alone};
        } elsif (@rs == 1) {
            if ($sp && $sp->{alone}) { problem("$year $st: its one race, declared a special alone, not under an unexpired term's heading") unless $rs[0]{term} eq 'unexpired'; }
            else {
                problem("$year $st: one race, a special declared") if $sp;
                problem("$year $st: one race under a term heading") if $rs[0]{when} ne '';
            }
        } else { problem("$year $st: " . scalar(@rs) . " races"); next; }
        for my $r (@rs) {
            my $class;
            if ($r->{term} eq 'unexpired') {
                $class = $sp ? $sp->{class} : 0;
                problem("$year $st: an unexpired term ending $r->{when}, declared $sp->{ends}") if $sp && $r->{when} ne $sp->{ends};
            } else {
                $class = $regular{$year};
                problem("$year $st: a full term beginning $r->{when}, not January 3, " . ($year + 1)) if $r->{when} ne '' && $r->{when} ne 'January 3, ' . ($year + 1);
            }
            my $key = "$year $st $class" . ($r->{term} eq 'unexpired' && $class == $regular{$year} ? 'u' : '');
            my $x = $race{$key} = { year => $year, st => $st, class => $class, term => $r->{term}, cands => [], non => 0, flags => {}, sum => 0 };
            my $marks = $marked{"$year $st $class"} // 0;
            for my $e (@{$r->{ent}}) {
                my $fig = $e->{fig};
                # a declared race's mark: a leading digit whose runoff footnote names this entry's candidate, the rest a figure grouped by
                # thousands - read off though the whole may look grouped too (Georgia's 12,289,113 is mark 1 and 2,289,113)
                if ($marks && $fig =~ /^(\d)(\d{1,3}(?:,\d{3})+)$/ && valid_count($2)) {
                    my ($m, $rest) = ($1, $2);
                    my $n = $rawv->{$st}{runoff_note}{$m};
                    if ($n && defined $n->{named}{last_word((split /, /, trim($e->{label}))[0])}) { $fig = $rest; $e->{mark} = $m; $marks--; $x->{flags}{M} = 1; }
                }
                if (!valid_count($fig)) { problem("$year $st: the figure '$e->{fig}' of '$e->{label}', not grouped by thousands"); next; }
                my $v = num($fig);
                $x->{sum} += $v;
                my $label = trim($e->{label});
                my @parts = split /, /, $label;
                if ($noncand{lc $label}) { $x->{non} += $v; next; }
                if (@parts == 1 && ($unnamed_ok{"$year $st"} // '') eq $label) { push @{$x->{cands}}, { name => "(unnamed, $label)", label => $label, class => 'O', total => $v }; $unnamed_used{"$year $st"} = 1; $x->{flags}{U} = 1; next; }
                if ($fusion_label{$label} && @parts == 1) {
                    if (!$fusion_of{$st} || !$fusion_of{$st}{$label}) { problem("$year $st: the party line '$label' outside a fusion state"); next; }
                    my $of = $x->{cands}[-1] or do { problem("$year $st: the party line '$label' with no candidate before it"); next; };
                    problem("$year $st: the party line '$label' after a runoff finalist, whose footnote's first round excludes it") if defined $of->{runoff};
                    $of->{total} += $v; $of->{lines}++; $x->{flags}{F} = 1;
                    next;
                }
                if (@parts == 1) {
                    problem("$year $st: the entry '$label' - no name, outside a fusion state, not declared");
                    next;
                }
                my @nm = (shift @parts);
                push @nm, shift @parts while @parts > 1 && $suffix{$parts[0]};
                my $lab = join ', ', @parts;
                my $cl = label_class($lab);
                if (!defined $cl) { problem("$year $st: the label '$lab' ($nm[0]) names a party in a form not declared"); next; }
                push @{$x->{cands}}, { name => join(', ', @nm), label => $lab, class => $cl, total => $v };
                if (defined $e->{mark}) {   # a mark is its footnote's: a runoff's count, the footnote naming this candidate with his first round
                    # (a mark is read only where its footnote names him, above: a footnote naming another leaves the figure ungrouped, or the
                    # race's marks not found)
                    my $n = $rawv->{$st}{runoff_note}{$e->{mark}};
                    $x->{cands}[-1]{runoff} = $v; $x->{cands}[-1]{first} = num($n->{named}{last_word($nm[0])}); $x->{runoff_day} = $n->{day};
                }
            }
            problem("$year $st class $class: $marks footnote mark(s) declared and not found") if $marks;
            # the winner: the deciding count's - a runoff's between its two marked finalists, else the candidates' plurality
            my @fin = grep { defined $_->{runoff} } @{$x->{cands}};
            problem("$year $st class $class: " . scalar(@fin) . " runoff finalists, not two") if @fin && @fin != 2;
            my @c = sort { $b->{total} <=> $a->{total} } (@fin ? @fin : @{$x->{cands}});
            if (@c < 1) { problem("$year $st: a race with no candidate"); next; }
            problem("$year $st class $class: a tie for first") if @c > 1 && $c[0]{total} == $c[1]{total};
            $x->{winner} = $c[0];
            @c = sort { ($b->{first} // $b->{total}) <=> ($a->{first} // $a->{total}) } @{$x->{cands}};   # the first round's order
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

# ---------------------------------------------------------------- the races the item holds: each year's regular class, the specials declared
my %class_of;   # by s792's catalog: XX => { classes }
my @seat_rows;
{
    open my $h, '<', "$usa/senate_seats.csv" or die "$usa/senate_seats.csv: $!\n";
    my @hdr;
    while (<$h>) { s/\r?\n$//; next if /^#/; my @c = split /,/; if (!@hdr) { @hdr = @c; next; } my %r; @r{@hdr} = @c; push @seat_rows, \%r; $class_of{$r{state}}{$r{class}} = 1; }
    close $h;
    $read{"$usa/senate_seats.csv"} = do { open my $b, '<:raw', "$usa/senate_seats.csv" or die; local $/; sha256_hex(<$b>) };
}
my %class_states = map { my $cl = $_; ($cl => [ sort grep { $class_of{$_}{$cl} } keys %class_of ]) } 1 .. 3;
problem("s792's catalog: the classes hold " . join(', ', map { scalar @{$class_states{$_}} } 1 .. 3) . " states, not 33, 33 and 34")
    unless @{$class_states{1}} == 33 && @{$class_states{2}} == 33 && @{$class_states{3}} == 34;
for my $year (@years) {   # each year's full-term races are its regular class's states, one a state
    my $cl = $regular{$year};
    my %have = map { $_->{st} => 1 } grep { $_->{year} == $year && $_->{class} == $cl && $_->{term} eq 'full' } values %race;
    my @miss = grep { !$have{$_} } @{$class_states{$cl}};
    my @extra = grep { !$class_of{$_}{$cl} } sort keys %have;
    problem("$year: the Class $cl states without a full-term race: @miss; races outside Class $cl: @extra") if @miss || @extra;
}

# ---------------------------------------------------------------- Maine: a single count is a first round only if its winner has half the candidates' votes
for my $x (grep { $_->{st} eq 'ME' } values %race) {
    my $c = 0; $c += $_->{total} for @{$x->{cands}};
    problem("$x->{year} ME: its winner $x->{winner}{total} of the candidates' $c - no majority, so its single count may not be a final round") unless 2 * $x->{winner}{total} > $c;
}
# Georgia and Louisiana: a race no runoff decided is final only if its winner has half the candidates' votes - else a runoff the Clerk does
# not print would have decided it
for my $x (grep { ($_->{st} eq 'GA' || $_->{st} eq 'LA') && !$_->{flags}{M} } values %race) {
    my $c = 0; $c += $_->{total} for @{$x->{cands}};
    problem("$x->{year} $x->{st}: its winner $x->{winner}{total} of the candidates' $c with no runoff printed - no majority") unless 2 * $x->{winner}{total} > $c;
}

# ---------------------------------------------------------------- the FEC's Senate sheets, 2016-2022: one W a race, its party the Clerk's winner's
# (the FEC's 2024 volume answered 403 at s786: 2024 rests on the Clerk alone)
my %fec_sheet = (2016 => '2016 US Senate Results by State', 2018 => '2018 US Senate Results by State', 2020 => '12. US Senate Results by State',
    2022 => '7. US Senate Results by State');
my %rcv;   # "year XX term" => [ { last, party, rounds => [r1, r2, ...] } ]: a ranked count's rounds as the FEC prints them (Alaska 2022)
for my $year (sort keys %fec_sheet) {
    my $file = "fec_federalelections$year.xlsx";
    my $c = xlsx_sheet(page("returns/$file"), $fec_sheet{$year}, $file);
    my ($hr) = grep { my $r = $_; grep { ($c->{$r}{$_} // '') eq 'GE WINNER INDICATOR' } keys %{$c->{$r}} } sort { $a <=> $b } keys %$c;
    die "the FEC's $year Senate sheet: no heading row\n" unless $hr;
    my %col; for my $k (keys %{$c->{$hr}}) { $col{trim($c->{$hr}{$k})} = $k; }
    $col{D} //= $col{DISTRICT};   # the term: 'S', 'S-FULL TERM', 'S-UNEXPIRED TERM' (headed D or DISTRICT by year)
    for my $need ('STATE ABBREVIATION', 'D', 'PARTY', 'GENERAL VOTES', 'GE WINNER INDICATOR', 'CANDIDATE NAME (Last)') { die "the FEC's $year Senate sheet: no column '$need'\n" unless $col{$need}; }
    my ($run) = map { $col{$_} } grep { /^GE RUNOFF ELECTION VOTES/ } keys %col;
    my ($fus) = map { $col{$_} } grep { /^COMBINED GE PARTY TOTALS/ } keys %col;
    die "the FEC's $year Senate sheet: no fusion-total or runoff column\n" unless $fus && $run;
    my %round = map { /^(\d)(?:ST|ND|RD|TH) ROUND RCV VOTES$/ ? ($1 => $col{$_}) : () } keys %col;
    my %w;   # "XX full|unexpired" => [ [party code, the votes that decided: the runoff, the fusion total, or the general] ]
    # the unnamed entry declared above, held to the FEC's line of her party in that race: its votes the Clerk's entry's
    my %unnamed_fec = $year == 2018 ? ('AZ full' => 'W(GRE)/GRE') : ();   # the FEC's code: a write-in in the primary, the Green line in the general
    my %unnamed_seen;
    for my $r (sort { $a <=> $b } keys %$c) {
        next if $r <= $hr;
        my ($st, $d, $p) = map { trim($c->{$r}{$col{$_}} // '') } ('STATE ABBREVIATION', 'D', 'PARTY');
        # the term as the FEC codes it: S-UNEXPIRED TERM / S-FULL TERM in any case; a plain S is the state's one race that year, whatever its
        # term (Arizona's 2020 special)
        my @mine = grep { $_->{year} == $year && $_->{st} eq $st } values %race;
        my $term = $d =~ /unexpired/i ? 'unexpired' : $d =~ /full/i ? 'full' : @mine == 1 ? $mine[0]{term} : 'full';
        my $k = "$st $term";
        if (exists $unnamed_fec{$k} && $unnamed_fec{$k} eq $p && trim($c->{$r}{$col{'GENERAL VOTES'}} // '') ne '') {
            my ($x) = grep { $_->{year} == $year && "$_->{st} $_->{term}" eq $k } values %race;
            my ($u) = $x ? grep { $_->{name} =~ /^\(unnamed/ } @{$x->{cands}} : ();
            my $fv = num(trim($c->{$r}{$col{'GENERAL VOTES'}}));
            my $ln = trim($c->{$r}{$col{'CANDIDATE NAME (Last)'}} // '');
            problem("FEC $year $k: the $p line, $ln, $fv votes; the Clerk's unnamed entry " . ($u ? "$u->{label}, $u->{total}" : 'none')) unless $u && $u->{total} == $fv && $ln eq $u->{label};
            $unnamed_seen{$k}++;
        }
        if (%round && trim($c->{$r}{$round{1}} // '') ne '' && trim($c->{$r}{$col{'CANDIDATE NAME (Last)'}} // '') ne '') {   # a candidate's row, never the sheet's total row
            push @{$rcv{"$year $k"}}, { last => trim($c->{$r}{$col{'CANDIDATE NAME (Last)'}} // ''), party => $p, rounds => [ map { num(trim($c->{$r}{$round{$_}} // 0)) } sort { $a <=> $b } keys %round ] };
        }
        next unless trim($c->{$r}{$col{'GE WINNER INDICATOR'}} // '') eq 'W';
        # a candidate on several fusion lines carries a W on each: grouped by his name, the decisive votes the runoff's, else the combined
        # total's, else the general's
        my $last = trim($c->{$r}{$col{'CANDIDATE NAME (Last)'}} // '');
        my $g = ($w{$k}{lc $last} //= { last => $last, rows => 0 });
        $g->{rows}++;
        $g->{party} //= $p unless $p =~ /^Combined/;   # a summary row "Combined Parties:" (2022) carries the total, never the party
        $g->{run} //= num(trim($c->{$r}{$run})) if trim($c->{$r}{$run} // '') ne '';
        $g->{fus} //= num(trim($c->{$r}{$fus})) if trim($c->{$r}{$fus} // '') ne '';
        $g->{gen} //= num(trim($c->{$r}{$col{'GENERAL VOTES'}} // 0));
    }
    # a runoff's finalists' first-round figures, as their footnotes give them, held to the FEC's general column for each
    for my $x (sort { $a->{st} cmp $b->{st} } grep { $_->{year} == $year && $_->{flags}{M} } values %race) {
        my $k = "$x->{st} $x->{term}";
        for my $f (grep { defined $_->{runoff} } @{$x->{cands}}) {
            my @rows = grep { trim($c->{$_}{$col{'STATE ABBREVIATION'}} // '') eq $x->{st} && lc trim($c->{$_}{$col{'CANDIDATE NAME (Last)'}} // '') eq last_word($f->{name})
                && trim($c->{$_}{$col{'GENERAL VOTES'}} // '') ne '' } grep { $_ > $hr } sort { $a <=> $b } keys %$c;
            my @terms = grep { my $d = trim($c->{$_}{$col{D}} // ''); $x->{term} eq 'unexpired' ? $d =~ /unexpired/i : $d !~ /unexpired/i } @rows;
            problem("FEC $year $k: $f->{name}'s first round " . (@terms ? num(trim($c->{$terms[0]}{$col{'GENERAL VOTES'}})) : 'not on the sheet') . ", his footnote's $f->{first}")
                unless @terms == 1 && num(trim($c->{$terms[0]}{$col{'GENERAL VOTES'}})) == $f->{first};
        }
    }
    my %fec_class = (D => 'D', DFL => 'D', R => 'R', REP => 'R', DEM => 'D', IND => 'I', N => '?');
    # the races whose winner's votes the FEC prints apart from the Clerk's, no saved page saying why - both printed, the Clerk's used
    my %votes_differ = map { $_ => 1 } @{ { 2016 => ['CA full', 'NY full', 'UT full'], 2018 => ['OH full'], 2020 => ['NJ full'], 2022 => ['IN full', 'NH full'] }->{$year} };
    # the FEC's own slip in a winner's name, declared: its 2020 sheet spells Tillis "Tilllis"
    my %fec_last_slip = $year == 2020 ? ('NC full' => 'tilllis') : ();
    for my $x (sort { $a->{st} cmp $b->{st} || $a->{term} cmp $b->{term} } grep { $_->{year} == $year } values %race) {
        my $k = "$x->{st} $x->{term}";
        my @p = values %{$w{$k} // {}};
        if (@p != 1) { problem("FEC $year $k: W marks for " . scalar(@p) . " candidates, not one"); next; }
        my $g = $p[0];
        my ($code, $last) = ($g->{party} // '-', $g->{last});
        my $v = $g->{run} // $g->{fus} // $g->{gen};
        (my $c1 = $code) =~ s/\*$//;   # a footnote star on the code (Utah 2022's R*)
        $c1 =~ s{/.*$}{};              # a fusion code names the candidate's own party first (Oregon's DEM/IP/WF, D/IP)
        my $fc = $fec_class{$c1} // 'O';
        # Mississippi's 2018 special: the FEC codes its candidates Nonpartisan (N); its W is held to the Clerk's winner by the runoff's votes
        problem("FEC $year $k: the W's party $code, the Clerk's winner $x->{winner}{name} ($x->{winner}{class})") unless $fc eq $x->{winner}{class} || ($fc eq '?' && "$year $k" eq '2018 MS unexpired');
        if (($fec_last_slip{$k} // '') eq lc $last) { push @fec_note, "$year $k: the FEC's W spelled '$last', the Clerk's winner $x->{winner}{name} (DECLARED: the FEC's slip)"; $fec_last_slip{$k} = 'met'; }
        else { problem("FEC $year $k: the W $last, the Clerk's winner $x->{winner}{name}") unless lc($last) =~ /\b\Q${\ last_word($x->{winner}{name})}\E$/; }
        if ($votes_differ{$k}) { push @fec_note, "$year $k: the FEC's winner $v, the Clerk's $x->{winner}{total} (DECLARED: no saved page explains it; the Clerk's figure used)"; $votes_differ{$k} = 2; next; }
        # a ranked count's W is printed at its first choices (the general's column): held to the Clerk's first choices
        problem("FEC $year $k: the W's votes $v, the Clerk's winner $x->{winner}{name} $x->{winner}{total}") unless $v == $x->{winner}{total};
    }
    for my $k (sort keys %votes_differ) { problem("FEC $year $k: a difference declared and not met") unless $votes_differ{$k} == 2; }
    for my $k (sort keys %fec_last_slip) { problem("FEC $year $k: a name slip declared and not met") unless $fec_last_slip{$k} eq 'met'; }
    for my $k (sort keys %unnamed_fec) { problem("FEC $year $k: " . ($unnamed_seen{$k} // 0) . " $unnamed_fec{$k} lines, not one") unless ($unnamed_seen{$k} // 0) == 1; }
}

# ---------------------------------------------------------------- the counts R-US19 reads (s796, US-12's Design): sides, the general's ballot, the base count
# A candidate's side: R gives REP and D gives DEM by the Clerk's label; I gives the conference his seat row names (s792/s795's caucus,
# matched by state, class and surname - R-US10 (a)), none if he holds no row of that seat or his caucus is N; any other party, a write-in and
# the unnamed entry have none. A race's counts in order: the general (its finalists at their first-round figures, as the runoff's footnote
# gives them), then the runoff (its two finalists) or the ranked count's rounds (the FEC's columns); the deciding count is the last, the base
# count the last in which both sides have a candidate with votes - where none has, the deciding count, flagged.
sub side_of {
    my ($x, $c) = @_;
    return 'REP' if $c->{class} eq 'R';
    return 'DEM' if $c->{class} eq 'D';
    return '' unless $c->{class} eq 'I';
    my @h = grep { $_->{state} eq $x->{st} && $_->{class} == $x->{class} && last_word($_->{senator}) eq last_word($c->{name}) } @seat_rows;
    return '' unless @h;
    my $cau = $h[0]{caucus} // '-';
    return $cau eq 'D' ? 'DEM' : $cau eq 'R' ? 'REP' : '';
}
for my $x (sort { $a->{year} <=> $b->{year} || $a->{st} cmp $b->{st} || $a->{class} <=> $b->{class} || $a->{term} cmp $b->{term} } values %race) {
    $_->{side} = side_of($x, $_) for @{$x->{cands}};
    # the truth's data gate: a winner of no party label holds a seat row whose caucus is sourced (D, R, or N for neither)
    if ($x->{winner}{class} eq 'I') {
        my @h = grep { $_->{state} eq $x->{st} && $_->{class} == $x->{class} && last_word($_->{senator}) eq last_word($x->{winner}{name}) } @seat_rows;
        problem("$x->{year} $x->{st} class $x->{class}: the independent winner $x->{winner}{name}, " . (@h ? "his caucus '$h[0]{caucus}' by '$h[0]{caucus_by}'" : 'no seat row'))
            unless @h && $h[0]{caucus} =~ /^[DRN]$/ && $h[0]{caucus_by} ne '-';
    }
    my @counts = ([ "general (the Clerk's)", { map { ($_ => ($_->{first} // $_->{total})) } @{$x->{cands}} } ]);
    my @fin = grep { defined $_->{runoff} } @{$x->{cands}};
    push @counts, [ "runoff (the Clerk's)", { map { ($_ => $_->{runoff}) } @fin } ] if @fin;
    if (my $rows = $rcv{"$x->{year} $x->{st} $x->{term}"}) {   # a ranked count: each FEC row matched to the Clerk's candidate by surname
        my %by = map { (lc($_->{last}) => $_) } @$rows;
        my $n = @{$rows->[0]{rounds}};
        for my $rd (1 .. $n) {
            my %v;
            for my $c (@{$x->{cands}}) { my $r = $by{last_word($c->{name})}; $v{$c} = $r ? $r->{rounds}[$rd - 1] : 0; }
            my $named = 0; $named += $_->{rounds}[$rd - 1] for @$rows;
            my $got = 0; $got += $_ for values %v;
            problem("$x->{year} $x->{st}: the FEC's ranked round $rd holds $named votes, $got of them the Clerk's candidates'") unless $got == $named;
            push @counts, [ "round $rd (the FEC's)", \%v ] if $got;
        }
        $x->{flags}{R} = 1;
    }
    my $sum = sub { my ($cnt, $side) = @_; my $s = 0; $s += $cnt->{$_} // 0 for grep { $_->{side} eq $side } @{$x->{cands}}; return $s; };
    my $top = sub { my ($cnt, $side) = @_; my $m = 0; for my $c (grep { $_->{side} eq $side } @{$x->{cands}}) { $m = $cnt->{$c} if ($cnt->{$c} // 0) > $m; } return $m; };
    my ($gen) = $counts[0];
    ($x->{gen_r}, $x->{gen_dc}) = ($sum->($gen->[1], 'REP'), $sum->($gen->[1], 'DEM'));
    my $decide = $counts[-1];
    # the deciding count's winner is the race's winner (a ranked count's last round is held to the Clerk's plurality of first choices)
    my ($lead) = sort { ($decide->[1]{$b} // 0) <=> ($decide->[1]{$a} // 0) } @{$x->{cands}};
    problem("$x->{year} $x->{st} class $x->{class}: the deciding count ($decide->[0]) names $lead->{name}, the winner $x->{winner}{name}") unless $lead == $x->{winner};
    my ($base) = grep { $sum->($_->[1], 'REP') > 0 && $sum->($_->[1], 'DEM') > 0 } reverse @counts;
    my $flag = '';
    if (!$base) { $base = $decide; $flag = '; no count two-sided'; }
    $x->{base_count} = $base->[0] . $flag;
    ($x->{final_r}, $x->{final_dc}, $x->{final_r_top}, $x->{final_dc_top}, $x->{final_none_top}) =
        ($sum->($base->[1], 'REP'), $sum->($base->[1], 'DEM'), $top->($base->[1], 'REP'), $top->($base->[1], 'DEM'), $top->($base->[1], ''));
}
for my $k (sort keys %rcv) { my ($y, $st, $term) = split / /, $k; problem("$k: a ranked count on the FEC's sheet, no Clerk race") unless grep { $_->{year} == $y && $_->{st} eq $st && $_->{term} eq $term } values %race; }

# ---------------------------------------------------------------- the winners against s792's catalog: each the holder of his seat on the day
sub holder_on {   # the seat's holder (state, class) at the end of a day, by s792's catalog
    my ($st, $cl, $d) = @_;
    my @h = grep { $_->{state} eq $st && $_->{class} == $cl && $_->{took} le $d && ($_->{left} eq '-' || $_->{left} gt $d) } @seat_rows;
    return @h == 1 ? $h[0] : undef;
}
sub held_day { my $x = shift; my %on = (2016 => '2017-01-03', 2018 => '2019-01-09', 2020 => '2021-01-21', 2022 => '2023-01-03');
    return $on{$x->{year}} // ($x->{class} == 2 ? '2025-01-03' : $x->{term} eq 'unexpired' ? '2024-12-09' : '2025-01-21'); }
sub last_word { my $n = shift; $n =~ s/\([^)]*\)//g; $n =~ s/,? (?:Jr|Sr|II|III|IV)\.?\s*$//; $n =~ s/[.,]//g; $n =~ s/\s+$//; return lc((split / /, $n)[-1]); }
for my $x (sort { $a->{year} <=> $b->{year} || $a->{st} cmp $b->{st} } values %race) {
    # the day each race's winner is held to (s795's catalog reaches 3 Jan 2017): 2016's on the 115th's first day; 2018's on 9 Jan 2019, after
    # Florida's late oath (8 Jan); 2020's on 21 Jan 2021, after Georgia's runoff winners' oaths (20 Jan); 2022's on the 118th's first day;
    # 2024's full terms after the January 2025 oaths, Nebraska's 2024 special (Class II) on the 119th's first day, California's 2024 special
    # on the day its winner was sworn to the old term
    my $d = held_day($x);
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
my %flag_said = (F => "fusion: a candidate on several ballot lines, his lines summed",
    M => "a runoff: its two finalists' figures carry footnote marks, read off - the runoff's count beside the others' first-round figures as the Clerk prints them",
    N => 'no Democrat on the ballot', R => "a ranked count: its rounds read from the FEC's sheet (Alaska 2022; the Clerk prints first choices)",
    S => "the first two of one party (a top two, or a ranked count's two Republicans)", U => 'an entry with no name, a candidate as the FEC names it');
my $gen = "# GENERATED by Tools/us_senate_races_prep.pl from the pages under ElectionsData/usa/raw/ - DO NOT EDIT; the record is ElectionsData/usa/senate_record.md.\n";
my $csv = $gen . "# US-12: a row a Senate race - every race of the general elections of 2016, 2018, 2020, 2022 and 2024, each year's regular class and the\n"
    . "# specials printed beside it - by the Clerk of the House's statistics, each state's listing FOR UNITED STATES SENATOR. term: full, or\n"
    . "# unexpired (a special). votes_r / votes_d / votes_i / votes_other: the candidates' printed votes by class - D a Democrat\n"
    . "# (Democratic-Nonpartisan League and Democratic-Farmer-Labor included), R a Republican, I an independent with no party (Independent,\n"
    . "# Unaffiliated, No Political Party, No Party Affiliation, By Petition), other any other party and every write-in candidate; a candidate's\n"
    . "# fusion lines summed to him; in a runoff race the finalists' figures are the runoff's, the others' the first round's (flag M).\n"
    . "# votes_non: the lines that are not candidates (blanks, over and under votes, void, spoiled, scatterings, write-in totals with no name,\n"
    . "# None of These Candidates). A label naming a major party, an independent or Nonpartisan in a form not declared stops the run.\n"
    . "# cands_r / cands_d / cands_i: how many of each the Clerk printed. winner: the deciding count's (a runoff's, a ranked count's last\n"
    . "# round's, else the candidates' plurality), his name and class.\n"
    . "# The counts R-US19 reads (s796), by side - REP a Republican, DEM a Democrat, and an independent the side his seat row's caucus names\n"
    . "# (R-US10 (a); none if he holds no row of that seat or caucuses with neither); other parties and write-ins no side: gen_r / gen_dc the\n"
    . "# general's ballot (a runoff's finalists at their first-round figures, by its footnote); base_count which count the share reads - the\n"
    . "# last of the general, the runoff and the ranked rounds in which both sides have a candidate with votes (where none has, the deciding\n"
    . "# count, '; no count two-sided'); final_r / final_dc that count's votes by side, final_r_top / final_dc_top each side's strongest\n"
    . "# candidate in it, final_none_top the strongest candidate of no side in it.\n"
    . "# flags, a letter each (- for none):\n" . join('', map { "#   $_  $flag_said{$_}\n" } sort keys %flag_said)
    . "year,state,class,term,votes_r,votes_d,votes_i,votes_other,votes_non,cands_r,cands_d,cands_i,winner,winner_class,flags,gen_r,gen_dc,final_r,final_dc,final_r_top,final_dc_top,final_none_top,base_count\n";
my @rows;
for my $x (sort { $a->{year} <=> $b->{year} || $a->{st} cmp $b->{st} || $a->{class} <=> $b->{class} || $a->{term} cmp $b->{term} } values %race) {
    my (%v, %n); $v{$_} = 0 for qw(R D I O); $n{$_} = 0 for qw(R D I O);
    for my $c (@{$x->{cands}}) { $v{$c->{class}} += $c->{total}; $n{$c->{class}}++; }
    my $flags = join '', sort keys %{$x->{flags}};
    (my $wn = $x->{winner}{name}) =~ s/,//g;
    my @row = ($x->{year}, $x->{st}, $x->{class}, $x->{term}, $v{R}, $v{D}, $v{I}, $v{O}, $x->{non}, $n{R}, $n{D}, $n{I}, $wn, $x->{winner}{class}, $flags eq '' ? '-' : $flags,
        $x->{gen_r}, $x->{gen_dc}, $x->{final_r}, $x->{final_dc}, $x->{final_r_top}, $x->{final_dc_top}, $x->{final_none_top}, $x->{base_count});
    push @rows, \@row;
    $csv .= join(',', @row) . "\n";
}
my $dg = sha256_hex($csv);
sub cq { my $s = shift; $s =~ s/\\/\\\\/g; $s =~ s/"/\\"/g; return "\"$s\""; }
my $tuple = "int Year, string State, int Class, string Term, long VotesR, long VotesD, long VotesI, long VotesOther, long VotesNon, int CandsR, int CandsD, int CandsI, string Winner, string WinnerClass, string Flags, long GenR, long GenDc, long FinalR, long FinalDc, long FinalRTop, long FinalDcTop, long FinalNoneTop, string BaseCount";
(my $types = $tuple) =~ s/ [A-Za-z]+(,|$)/$1/g;
my $cs = "// GENERATED by Tools/us_senate_races_prep.pl. DO NOT EDIT BY HAND.\n//\n"
    . "// Source: ElectionsData/usa/senate_races.csv (its SHA-256 below), written by the same run from the saved pages listed in SenateRaceRawSources.\n"
    . "// GeneratedCatalogCheck re-reads the CSV and compares every figure, and re-hashes every page listed.\n\n"
    . "namespace PoliSim.Elections.Generated\n{\n"
    . "    // PS-6 US-12 (COMPLETED.md s793, s796): the Senate races of 2016-2024 - a part of the partial class UsPresidentialReturns\n"
    . "    // (UsPresidentialReturns.cs, written by Tools/us_returns_prep.pl). Generated, never hand-edited; built by a method.\n"
    . "    public static partial class UsPresidentialReturns\n    {\n"
    . "        public const string SenateRaceSourceDigest = \"$dg\";\n\n"
    . "        /// <summary>Every saved page the Senate races run read, by its path under `ElectionsData/usa/` (s792's seats catalog among them), with its SHA-256.</summary>\n"
    . "        public static readonly (string Path, string Sha256)[] SenateRaceRawSources = BuildSenateRaceRawSources();\n\n"
    . "        /// <summary>A row a Senate race: year, state, class, term (full or unexpired), the candidates' printed votes by class (R, D, I an\n"
    . "        /// independent, other), the lines that are not candidates, how many of each class the Clerk printed, the winner's name and class, the\n"
    . "        /// flags, and the counts R-US19 reads by side - the general's ballot, the base count and which it is, each side's strongest candidate\n"
    . "        /// and the strongest of no side (senate_races.csv's header says each).</summary>\n"
    . "        public static readonly ($tuple)[] SenateRaces = BuildSenateRaces();\n\n"
    . "        private static (string Path, string Sha256)[] BuildSenateRaceRawSources() => new (string, string)[]\n        {\n"
    . join('', map { my $p = $_; $p =~ s{^\Q$usa\E/}{}; $p =~ s{^raw/}{raw/}; "            (\"$p\", \"$read{$_}\"),\n" } sort keys %read) . "        };\n\n"
    . "        private static ($tuple)[] BuildSenateRaces() => new ($types)[]\n        {\n"
    . join('', map { "            ($_->[0], " . cq($_->[1]) . ", $_->[2], " . cq($_->[3]) . ", $_->[4]L, $_->[5]L, $_->[6]L, $_->[7]L, $_->[8]L, $_->[9], $_->[10], $_->[11], " . cq($_->[12]) . ', ' . cq($_->[13]) . ', ' . cq($_->[14])
        . ", $_->[15]L, $_->[16]L, $_->[17]L, $_->[18]L, $_->[19]L, $_->[20]L, $_->[21]L, " . cq($_->[22]) . "),\n" } @rows)
    . "        };\n    }\n}\n";
$cs =~ s/\n/\r\n/g;
write_all("$usa/senate_races.csv" => $csv, 'Assets/Scripts/Elections/Generated/UsSenateRaces.cs' => $cs);
my %count; $count{$_->[0]}++ for @rows;
print "races: " . join(', ', map { "$_ $count{$_}" } sort keys %count) . "\n";
print "FEC: $_
" for @fec_note;
print "flags: " . join('; ', map { "$_->[0] $_->[1] class $_->[2] $_->[3]: $_->[14]" } grep { $_->[14] ne '-' } @rows) . "\n";
print "wrote $usa/senate_races.csv (" . scalar(@rows) . " races), Assets/Scripts/Elections/Generated/UsSenateRaces.cs\n";
