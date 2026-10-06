#!/usr/bin/perl
# THE SENATE BY STATE AND BY DATE (PS-6 US-12, COMPLETED.md s792): every Senate seat of the 118th and 119th Congresses, its holders and the dates
# they held it, read from the Senate's own pages; and (s795, R-US19's catalog step) every holder back to 3 Jan 2017, the 115th Congress's
# opening - the early rows, on the state pages' own days, no oath read (the saved New Senators page lists the 118th and 119th alone). The record
# is ElectionsData/usa/senate_record.md.
# It reads ONLY saved pages, each held to its SHA256SUMS.txt line first:
#   - raw/senate/wayback_senate_state_<XX>_<capture>.html, the 50 "States in the Senate" pages: each seat's holders by class, their party,
#     the day each began and ended and how (the base);
#   - raw/senate/wayback_senate_NewSenators_*.html: every new senator of the 118th and 119th Congresses and the day of his oath (a seat is
#     held from the oath - DECLARED, the plan's US-12);
#   - raw/senate/wayback_senate_AppointedSenators_*.html, _SenatorsDiedinOffice_*.html, _SenatorsWhoChangedPartiesDuringSenateService_*.html,
#     _SenatorsRepresentingThirdorMinorParties_*.html: the appointments with their oaths, the deaths, the party changes, the independents;
#   - raw/senate/govinfo_cdir_<edition>.pdf, the Congressional Directory of 25 Apr 2024 and of 1 Oct 2025 (by pdftotext -raw): the Senate's
#     party count on its closing day, each class's in 2024, and its table of changes - the anchors;
#   - raw/senate/wayback_democrats_senate_gov_members_<capture>.html: the Senate Democrats' own list of their members (the caucus, R-US10 (a));
#   - raw/records/wayback_senate_class_{I,II,III}_*.html ([SEN-C1..3]): each class as the page's own date gives it;
#   - raw/records/wayback_senate_party_division_*.html ([SEN-DIV]): the party division of the 117th-119th Congresses.
# It writes ElectionsData/usa/senate_seats.csv, senate_changes.csv, senate_on.csv, senate_division.csv and
# Assets/Scripts/Elections/Generated/UsSenateRecord.cs (a part of the partial class UsPresidentialReturns). It dies, writing nothing, on:
#   - a page off its digest, or in no SHA256SUMS.txt; a page sought by its pattern missing, or two (a state's, a list's, an edition's);
#   - (the state pages) a row read (holding a seat on or after 3 Jan 2017) without a class, a name, a party or a day; a state not holding
#     exactly two classes, the classes not 33, 33 and 34 states; a class's holders overlapping, or its last holder ended; a party label not
#     declared; a footnote mark without its note; an early row (one whose service ended before the window's first day's end) neither a
#     Democrat nor a Republican - the Third or Minor Parties page is read for the window alone;
#   - (the oaths) a New Senators entry of the 118th or 119th Congress not read, matching no row of its state's page or two, of another
#     party, or sworn twice; a row of the window that began inside it with no oath on that page;
#   - (the appointments) an appointment sworn in the window matching no holder, whose oath is not the New Senators page's, or which follows
#     its oath; one sworn from 4 Jan 2017 to the window matching no row, or whose row's first day is not from its appointment to its oath; an
#     appointee by the New Senators page whom the Appointed Senators page does not list, but the two DECLARED (Schiff, Kim); a footnote mark
#     after an appointment's day not declared, or its note not naming him (Smith's DECLARED);
#   - (the deaths) a death in the 115th-119th Congresses whose day is not the state page's, or a row ended "Died" the page does not list;
#   - (the party changes) the Changed Parties page's summaries not one for each chapter of its contents; a senator of the window whose party
#     changed there other than the two declared, or a declared change's day not the page's own words; a senator the Third or Minor Parties
#     page names an independent in the window who is not one here on the first and last day of its span, or whose service does not end in
#     its last year, or the reverse;
#   - (the anchors) the class pages not the roster on their own dates, senator for senator; the Congressional Directory's closing day, its
#     count's own day, its counts or its class listings (the one omission DECLARED apart) not the roster's; its table of changes not read
#     whole, a reason other than a resignation or a death, or a successor's oath not the New Senators page's;
#   - (the caucus) the Senate Democrats' list on its capture day not exactly the roster's Democrats and the independents it lists; an
#     independent of the window on neither list nor named by the Senate's own words;
#   - [SEN-DIV] without its 117th-119th lines, a line holding on no day of its Congress the record reaches, its 119th notes (a delayed
#     oath, a resignation) not found or not the roster's days, or its 117th note (the division before the January 2021 oaths) not the
#     roster's on every day it names but its last - the one slip DECLARED (Georgia's page ends Loeffler's service on 19 Jan 2021);
#   - a day's roster not 100 seats; the record's reach not the New Senators page's capture;
#   - a perl warning during the checks; a non-ASCII byte in an output, or a CSV row whose fields are not its heading's.
# The record's reach is the New Senators page's capture: a row that began after it is not read, and a service ending after it is written as
# serving (-); both are printed.
# All outputs are built and tested before the first is written.
# Usage: perl Tools/us_senate_prep.pl   (from the project root, Git for Windows' perl; pdftotext at /mingw64/bin or on PATH)
# ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use Time::Local qw(timegm);

my $usa = 'ElectionsData/usa';
my $raw = "$usa/raw";
my $pdftotext = -x '/mingw64/bin/pdftotext' ? '/mingw64/bin/pdftotext' : 'pdftotext';
my @problems;
sub problem { push @problems, join('', @_); return; }
$SIG{__WARN__} = sub { problem('a perl warning: ', $_[0] =~ s/\s+$//r) };
$SIG{__DIE__} = sub { return if $^S; print STDERR "MISMATCH: $_\n" for splice @problems; };

# ---------------------------------------------------------------- the saved pages, each held to its SHA256SUMS.txt line before it is used
my %sums;
for my $g (qw(records senate)) {
    open my $s, '<', "$raw/$g/SHA256SUMS.txt" or die "$raw/$g/SHA256SUMS.txt: $!\n";
    while (my $l = <$s>) { $sums{"$g/$2"} = $1 if $l =~ /^([0-9a-f]{64}) \*(.+?)\s*$/; }
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
    $b =~ s/\r//g;
    return $b;
}
sub one {   # the one saved page of a group matching a pattern
    my ($g, $re) = @_;
    my @f = sort grep { /^$g\/$re$/ } keys %sums;
    die "$g/$re: " . scalar(@f) . " saved pages match, not one\n" unless @f == 1;
    return $f[0] =~ s/^$g\///r;
}
sub pdf_text {
    my $rel = shift;
    page($rel);
    open my $p, '-|', $pdftotext, '-raw', "$raw/$rel", '-' or die "$pdftotext: $!\n";
    my $text = do { local $/; <$p> };
    close $p or die "$pdftotext $rel: exit " . ($? >> 8) . "\n";
    $text =~ s/\r//g;
    $text =~ tr/\xE1\xE9\xED\xF3\xFA\xF1/aeioun/;   # pdftotext's Latin-1: the accented letters these volumes print, to ASCII
    $text =~ s/([a-z])\n\xB4([a-z])/$1$2/g;          # a separate acute accent set on the next line splits a name ("Luja" / "\xB4n"): rejoined
    return $text;
}
sub words {   # tags to spaces, the entities these pages use decoded to ASCII, white space collapsed
    my $s = shift;
    $s =~ s/<script\b.*?<\/script>//gis;
    $s =~ s/<style\b.*?<\/style>//gis;
    $s =~ s/<[^>]*>/ /g;
    return ascii($s);
}
sub ascii {
    my $s = shift;
    $s =~ s/&nbsp;|&#160;|\xC2\xA0/ /g;
    $s =~ s/&aacute;|\xC3\xA1/a/g; $s =~ s/&eacute;|\xC3\xA9/e/g; $s =~ s/&iacute;|\xC3\xAD/i/g; $s =~ s/&oacute;|\xC3\xB3/o/g; $s =~ s/&uacute;|\xC3\xBA/u/g; $s =~ s/&ntilde;|\xC3\xB1/n/g;
    $s =~ s/&ndash;|&#150;|&#8211;|\xE2\x80\x93/-/g; $s =~ s/&rsquo;|&#039;|&#39;|\xE2\x80\x99/'/g; $s =~ s/&quot;|``|''/"/g; $s =~ s/&amp;/&/g;
    $s =~ s/\s+/ /g; $s =~ s/^ | $//g;
    return $s;
}

# ---------------------------------------------------------------- dates: a day is YYYY-MM-DD; the roster on a day is the Senate at its end
my %mon = (jan => 1, feb => 2, mar => 3, apr => 4, may => 5, jun => 6, jul => 7, aug => 8, sep => 9, sept => 9, oct => 10, nov => 11, dec => 12,
    january => 1, february => 2, march => 3, april => 4, june => 6, july => 7, august => 8, september => 9, october => 10, november => 11, december => 12);
sub day {   # "Sept. 29, 2023", "January 3, 2025", "09/29/2023", "1-23-23" -> 2023-09-29
    my ($t, $what) = @_;
    $t = ascii($t);
    return sprintf('%04d-%02d-%02d', $3, $mon{lc $1}, $2) if $t =~ /^([A-Za-z]+)\.? (\d{1,2}), (\d{4})$/ && $mon{lc $1};
    return sprintf('%04d-%02d-%02d', $3, $1, $2) if $t =~ m{^(\d{1,2})/(\d{1,2})/(\d{4})\*?$};
    return sprintf('20%02d-%02d-%02d', $3, $1, $2) if $t =~ /^(\d{1,2})-(\d{1,2})-(\d{2})$/;
    die "$what: not a day: '$t'\n";
}
sub next_day { my ($y, $m, $d) = split /-/, shift; my @t = gmtime(timegm(0, 0, 12, $d, $m - 1, $y) + 86400); return sprintf('%04d-%02d-%02d', $t[5] + 1900, $t[4] + 1, $t[3]); }
sub prev_day { my ($y, $m, $d) = split /-/, shift; my @t = gmtime(timegm(0, 0, 12, $d, $m - 1, $y) - 86400); return sprintf('%04d-%02d-%02d', $t[5] + 1900, $t[4] + 1, $t[3]); }
my $window = '2023-01-03';   # the 118th Congress opens
my $early = '2017-01-03';    # the 115th Congress opens: the holder rows reach back to it (s795), on the state pages' own days - the saved New
                             # Senators page lists the 118th and 119th Congresses alone, so no oath before the window is read
my @beyond;   # what the record's reach clips, printed
my @early_appointed;   # the appointees sworn before the window: the Appointed Senators page's days beside the state page's, printed
my $reach = '2026-09-06';   # the record's reach, the New Senators page's capture (held to it below): a row that began after it is not read,
                             # a service ending after it is written as serving

my %code = ('Alabama' => 'AL', 'Alaska' => 'AK', 'Arizona' => 'AZ', 'Arkansas' => 'AR', 'California' => 'CA', 'Colorado' => 'CO', 'Connecticut' => 'CT',
    'Delaware' => 'DE', 'Florida' => 'FL', 'Georgia' => 'GA', 'Hawaii' => 'HI', 'Idaho' => 'ID', 'Illinois' => 'IL', 'Indiana' => 'IN', 'Iowa' => 'IA',
    'Kansas' => 'KS', 'Kentucky' => 'KY', 'Louisiana' => 'LA', 'Maine' => 'ME', 'Maryland' => 'MD', 'Massachusetts' => 'MA', 'Michigan' => 'MI',
    'Minnesota' => 'MN', 'Mississippi' => 'MS', 'Missouri' => 'MO', 'Montana' => 'MT', 'Nebraska' => 'NE', 'Nevada' => 'NV', 'New Hampshire' => 'NH',
    'New Jersey' => 'NJ', 'New Mexico' => 'NM', 'New York' => 'NY', 'North Carolina' => 'NC', 'North Dakota' => 'ND', 'Ohio' => 'OH', 'Oklahoma' => 'OK',
    'Oregon' => 'OR', 'Pennsylvania' => 'PA', 'Rhode Island' => 'RI', 'South Carolina' => 'SC', 'South Dakota' => 'SD', 'Tennessee' => 'TN', 'Texas' => 'TX',
    'Utah' => 'UT', 'Vermont' => 'VT', 'Virginia' => 'VA', 'Washington' => 'WA', 'West Virginia' => 'WV', 'Wisconsin' => 'WI', 'Wyoming' => 'WY');
my @order = sort values %code;   # by postal code
my %is_state = map { $_ => 1 } @order;

# A senator is matched across pages by his state and the last word of his surname (suffixes, quoted nicknames and initials dropped), and
# where one state has two of a surname in the window, by his first given name's initial as well (South Carolina's Grahams) - in
# find_holder alone: the class pages, the Directory's listings and the Democrats' lists are keyed on state and surname, none naming both.
sub surname_key {
    my $n = ascii(shift);
    $n =~ s/"[^"]*"//g; $n =~ s/\([^)]*\)//g; $n =~ s/\s+$//; $n =~ s/,? (?:Jr|Sr)\.?$//; $n =~ s/ (?:II|III|IV)$//; $n =~ s/[.,]//g; $n =~ s/\s+$//;
    my @w = split / /, $n;
    return lc $w[-1];
}
sub first_initial { my $n = ascii(shift); $n =~ s/^(?:[A-Z]\. )+//; return uc substr($n, 0, 1); }

# ---------------------------------------------------------------- the base: the 50 state pages
my (%seat, %capture);   # $seat{$st}{$class} = [ { name, party_label, began, ended, comments, notes } ... ] of the window, in order
my %party_declared = ('D' => 'D', 'R' => 'R', 'I' => 'I', 'D, I' => 'D/I');   # D/I: a Democrat who became an independent, dated below
for my $st (@order) {
    my $rel = 'senate/' . one('senate', "wayback_senate_state_${st}_(\\d{14})\\.html");
    ($capture{$st}) = $rel =~ /_(\d{8})\d{6}\.html$/;
    $capture{$st} =~ s/^(\d{4})(\d\d)(\d\d)$/$1-$2-$3/;
    my $h = page($rel);
    my %note;
    while ($h =~ m{<a name="note(\d+)">&nbsp;</a>\s*\d+\.\s*(.*?)</p>}gs) { $note{$1} = ascii($2); }
    my $class;
    while ($h =~ m{<tr>(.*?)</tr>}gs) {
        my $r = $1;
        if ($r !~ /data-label/) { $class = $1 if $r =~ /Class (\d)/; next; }
        my %c;
        while ($r =~ m{<td data-label="([A-Z ]+)">(.*?)</td>}gs) { $c{$1} = $2; }
        next unless defined $c{SENATOR};
        my @marks = map { /note(\d+)/ ? $1 : () } ($c{SENATOR} . $c{'TERM BEGAN'} . $c{'TERM ENDED'}) =~ /(<a href="#note\d+">)/g;
        my ($b, $e) = map { my $x = $_; $x =~ s/<sup>.*?<\/sup>//gs; words($x) } ($c{'TERM BEGAN'}, $c{'TERM ENDED'});
        next if $e =~ /(\d{4})\s*$/ && $1 < 2017;   # ended before the early rows' year: not read (some old rows give a month alone)
        my $ended = $e =~ /^Present$/ ? '-' : day($e, "$st $c{SENATOR} ended");
        next if $ended ne '-' && $ended le $early;   # held no seat on or after the 115th Congress's first day's end
        defined $class or problem("$st: a row of the window outside any class");
        my $who = $c{SENATOR}; $who =~ s/<sup>.*?<\/sup>//gs; $who = words($who);
        my ($name, $label) = $who =~ /^(.*?) \(([^)]*)\)$/ or do { problem("$st: '$who' has no party in parentheses"); next; };
        my $party = $party_declared{$label} // do { problem("$st $name: party label '$label' not declared"); 'X' };
        for my $m (@marks) { problem("$st $name: footnote $m has no note") unless defined $note{$m}; }
        push @{$seat{$st}{$class // 0}}, { st => $st, class => $class // 0, name => $name =~ s/,//gr, label => $label, party => $party, began => day($b, "$st $name began"),
            ended => $ended, comments => words($c{COMMENTS} // ''), notes => [ map { $note{$_} // '' } @marks ],
            early => ($ended ne '-' && $ended le $window ? 1 : 0) };   # an early row: his service ended before the window's first day's end
    }
    my @cl = sort keys %{$seat{$st}};
    problem("$st: holds classes @cl in the window, not two") unless @cl == 2 && !grep { $_ !~ /^[123]$/ } @cl;
    for my $cl (@cl) {
        my $s = $seat{$st}{$cl};
        for my $i (1 .. $#$s) { problem("$st class $cl: $s->[$i - 1]{name} ended $s->[$i - 1]{ended}, after $s->[$i]{name} began $s->[$i]{began}")
            if $s->[$i - 1]{ended} eq '-' || $s->[$i - 1]{ended} gt $s->[$i]{began}; }
        problem("$st class $cl: its last holder has ended") unless $s->[-1]{ended} eq '-';
    }
}
my %by_class; for my $st (@order) { $by_class{$_}++ for keys %{$seat{$st}}; }
problem("the classes hold $by_class{1}, $by_class{2} and $by_class{3} states, not 33, 33 and 34")
    unless ($by_class{1} // 0) == 33 && ($by_class{2} // 0) == 33 && ($by_class{3} // 0) == 34;
my @all = map { my $st = $_; map { @{$seat{$st}{$_}} } sort keys %{$seat{$st}} } @order;   # every row read, early rows among them
my @holders = grep { !$_->{early} } @all;   # the window's: every check of the 118th and 119th Congresses reads these alone
my @early = grep { $_->{early} } @all;
# an early row is a Democrat or a Republican: the Third or Minor Parties page is read for the window alone, so an early independent stops the run
for my $x (@early) { problem("$x->{st} $x->{name}: an early row labelled $x->{label} - the early rows are read for D and R alone") unless $x->{party} eq 'D' || $x->{party} eq 'R'; }
sub find_in {   # a senator by state and name among the rows given, or undef; dies on two
    my ($st, $name, $what, $rows, $of) = @_;
    my ($k, $fi) = (surname_key($name), first_initial($name));
    my @m = grep { $_->{st} eq $st && surname_key($_->{name}) eq $k } @$rows;
    @m = grep { first_initial($_->{name}) eq $fi } @m if @m > 1;
    problem("$what: '$name' ($st) matches " . scalar(@m) . " holders of $of") if @m > 1;
    return $m[0];
}
sub find_holder { my ($st, $name, $what) = @_; return find_in($st, $name, $what, \@holders, 'the window'); }   # a senator of the window
sub find_any { my ($st, $name, $what) = @_; return find_in($st, $name, $what, \@all, 'the rows read'); }        # of the window or early

# ---------------------------------------------------------------- the oaths: the New Senators page, its 118th and 119th Congresses
{
    my $rel = 'senate/' . one('senate', 'wayback_senate_NewSenators_\d{14}\.html');
    my $h = page($rel);
    my $congress;
    my %seen;
    while ($h =~ m{(<tr[^>]*>.*?</tr>)}gs) {
        my $t = words($1);
        next if $t eq '';
        if ($t =~ /^(\d+) th Congress/) { $congress = $1; next; }
        next unless defined $congress && ($congress == 118 || $congress == 119);
        my ($name, $pp, $st, $how, $sworn) = $t =~ /^(.+?) (?:\d+ )?([DRI]) ?-([A-Z]{2}) (.+?) (?:\d+ )?([A-Z][a-z]+ \d{1,2}, \d{4})$/
            or do { problem("New Senators $congress: an entry not read: '$t'"); next; };
        my $x = find_holder($st, $name, "New Senators $congress") or do { problem("New Senators $congress: $name ($pp-$st) matches no row of ${st}'s page"); next; };
        problem("New Senators: $name sworn twice") if $seen{$x}++;
        $x->{sworn} = day($sworn, "New Senators $name");
        $x->{how_in} = $how =~ /Appointed/ ? 'appointed' : $how =~ /Special election/ ? 'special election' : 'general election';
        $x->{how_said} = $how;
        problem("New Senators: $name is $pp, ${st}'s page $x->{label}") unless $x->{party} eq $pp || ($x->{party} eq 'D/I' && $pp eq 'D');
    }
    for my $x (@holders) {
        if ($x->{began} gt $reach && !defined $x->{sworn}) { $x->{beyond} = 1; push @beyond, "$x->{st} $x->{name}: began $x->{began}, after the reach - not read"; next; }
        if ($x->{began} gt $window && !defined $x->{sworn}) { problem("$x->{st} $x->{name}: began $x->{began}, inside the window, with no oath on the New Senators page"); }
        $x->{took} = $x->{sworn} // $x->{began};
        $x->{how_in} //= 'before the window';
    }
    for my $x (@early) { $x->{took} = $x->{began}; $x->{how_in} = 'before the window'; }   # the state page's day: no oath before the window is read
    @holders = grep { !$_->{beyond} } @holders;
    @all = grep { !$_->{beyond} } @all;
    for my $st (@order) { for my $cl (keys %{$seat{$st}}) { @{$seat{$st}{$cl}} = grep { !$_->{beyond} } @{$seat{$st}{$cl}}; } }
}
# ---------------------------------------------------------------- the appointments, the deaths
my %appointed_mark = ('MN smith' => 1);   # the Appointed Senators page's footnote marks after an appointment's day, by senator
{
    my $h = page('senate/' . one('senate', 'wayback_senate_AppointedSenators_\d{14}\.html'));
    my $words = words($h);
    while ($h =~ m{<tr>(.*?)</tr>}gs) {
        my $r = $1;
        my @td = map { my $x = $_; $x =~ s{<span style="display:none">.*?</span>}{}gs; words($x) } $r =~ m{<td>(.*?)</td>}gs;
        next unless @td >= 3 && $td[0] =~ /^(.+) \(([DRI])-([A-Z]{2})\)$/;
        my ($name, $p, $st) = ($1, $2, $3);
        next unless $td[1] =~ /(\d{4})/ && $1 >= 2016;   # appointed before the early rows' years: not read (old rows print "--" or footnoted days)
        # a footnote mark after the appointment's day: declared senator by senator, its note naming him (Smith's: appointed on 2 Jan 2018,
        # her lieutenant governorship resigned at 11:59 p.m. that night - the table's day, 3 Jan, is the one read)
        if ($td[1] =~ /^(.*\d{4}) (\d)$/) {
            my ($d, $m) = ($1, $2);
            my $k = "$st " . surname_key($name);
            problem("Appointed Senators: $name ($st) appointed '$td[1]', a footnote mark not declared") unless ($appointed_mark{$k} // '') eq $m;
            problem("Appointed Senators: footnote $m does not name $name") unless $words =~ /(?:^|\s)$m\. \Q$name\E was appointed on /;
            $appointed_mark{$k} = 'read' if exists $appointed_mark{$k};
            $td[1] = $d;
        }
        my ($appointed, $sworn) = (day($td[1], "Appointed $name"), day($td[2], "Appointed $name sworn"));
        next if $sworn le $early;
        if ($sworn lt $window) {   # sworn before the window: no oath page to hold him to; his row's day must lie from his appointment to his oath
            my $x = find_any($st, $name, 'Appointed Senators') or do { problem("Appointed Senators: $name ($st), sworn $sworn, matches no row"); next; };
            problem("Appointed Senators: $name appointed $appointed, sworn $sworn; ${st}'s page has him from $x->{took}") unless $x->{took} ge $appointed && $x->{took} le $sworn;
            $x->{appointed} = $appointed;
            push @early_appointed, "$st $name: appointed $appointed, sworn $sworn by the Appointed Senators page; ${st}'s page from $x->{took}";
            next;
        }
        my $x = find_holder($st, $name, 'Appointed Senators') or do { problem("Appointed Senators: $name ($st) of the window matches no holder"); next; };
        problem("Appointed Senators: $name sworn $sworn, the New Senators page " . ($x->{sworn} // 'none')) unless ($x->{sworn} // '') eq $sworn;
        problem("Appointed Senators: $name appointed $appointed after his oath $sworn") if $appointed gt $sworn;
        $x->{appointed} = $appointed;
    }
    # the reverse: every appointee of the window by the New Senators page is on the Appointed Senators page - but the two DECLARED, elected to
    # the next term and then appointed to the last weeks of the old one, whom that page does not list
    my %elected_then_appointed = ('CA schiff' => 1, 'NJ kim' => 1);
    for my $x (@holders) {
        next unless ($x->{how_in} // '') eq 'appointed' && $x->{took} gt $window;
        my $k = "$x->{st} " . surname_key($x->{name});
        if ($elected_then_appointed{$k}) {
            problem("$x->{st} $x->{name}: DECLARED elected then appointed, yet on the Appointed Senators page") if $x->{appointed};
            problem("$x->{st} $x->{name}: DECLARED elected then appointed, the New Senators page says '$x->{how_said}'") unless $x->{how_said} =~ /^Appointed & .*election$/;
            $elected_then_appointed{$k} = 2;
        } elsif (!$x->{appointed}) { problem("$x->{st} $x->{name}: appointed on the New Senators page, not on the Appointed Senators page"); }
    }
    for my $k (sort keys %elected_then_appointed) { problem("$k: DECLARED elected then appointed, no such appointee") unless $elected_then_appointed{$k} == 2; }
    for my $k (sort keys %appointed_mark) { problem("Appointed Senators: ${k}'s footnote mark declared and not found") unless $appointed_mark{$k} eq 'read'; }
    $h = page('senate/' . one('senate', 'wayback_senate_SenatorsDiedinOffice_\d{14}\.html'));
    my %died;
    while ($h =~ m{<tr valign="top">(.*?)</tr>}gs) {
        my @td = map { words($_) } $1 =~ m{<td>(.*?)</td>}gs;
        next unless @td >= 3 && $td[1] =~ /^(\d+)\/\d$/ && $1 >= 115;
        my ($name, $st) = $td[0] =~ /^(.+?) \(([A-Z]{2})\)$/ or do { problem("Died in Office: '$td[0]' not read"); next; };
        my $x = find_any($st, $name =~ s/^(.+?), (.+)$/$2 $1/r, 'Died in Office') or do { problem("Died in Office: $name ($st) matches no holder"); next; };
        my $d = day($td[2], "Died $name");
        problem("Died in Office: $name $d, ${st}'s page ended $x->{ended}") unless $x->{ended} eq $d && $x->{comments} =~ /Died/;
        $died{$x} = 1;
    }
    for my $x (@all) { problem("$x->{st} $x->{name}: ended 'Died', not on the Died in Office page") if $x->{comments} =~ /Died/ && !$died{$x}; }
}

for my $x (@all) {
    $x->{left} = $x->{ended};
    $x->{how_out} = $x->{ended} eq '-' ? '-' : $x->{comments} =~ /Died/ ? 'died' : $x->{comments} =~ /[Rr]esigned/ ? 'resigned' : 'term ended';
    if ($x->{left} ne '-' && $x->{left} gt $reach) { push @beyond, "$x->{st} $x->{name}: ended $x->{left}, after the reach - written as serving"; ($x->{left}, $x->{how_out}) = ('-', '-'); }
}

# ---------------------------------------------------------------- the party changes: declared from the Changed Parties page, held to it
my %change = (   # a senator of the window whose party changed: the page's words, whose captured day is the first he is an independent
    # Sinema: the page's party line is "Independent, 2023-present", after her announcement of 9 Dec 2022; the day it gives, the 118th's opening,
    # is the window's first day, so no earlier day would count differently
    'AZ sinema'  => { said => qr/Effective (January 3, 2023), at the beginning of the 118th Congress, she received her seniority/ },
    'WV manchin' => { said => qr/On (June 5, 2024), he switched his affiliation to Independent, but he continued to caucus with the Democrats/,
        caucus => 'the Changed Parties page ("he continued to caucus with the Democrats")' },
);
{
    my $t = words(page('senate/' . one('senate', 'wayback_senate_SenatorsWhoChangedPartiesDuringSenateService_\d{14}\.html')));
    for my $k (sort keys %change) {
        if ($t =~ $change{$k}{said}) { $change{$k}{day} = day($1, "Changed Parties $k"); } else { problem("Changed Parties: the words for $k not found"); }
    }
    # every chapter of the page's contents: a senator of the window among them must be a declared change, and every declared change a chapter
    my @chapters;
    while ($t =~ /Chapter (\d+): (.+?) of ([A-Z][a-z]+(?: [A-Z][a-z]+)?)(?= Chapter \d+:| [A-Z])/g) {
        my ($num, $name, $state) = ($1, $2, $3);   # copied first: a substitution resets the captures
        $name =~ s/,$//;
        push @chapters, [$num, $name, $state];
    }
    problem("Changed Parties: its contents number " . join(',', map { $_->[0] } @chapters) . ", not 1 to " . scalar(@chapters)) if grep { $chapters[$_ - 1][0] != $_ } 1 .. @chapters;
    problem("Changed Parties: no chapter read") unless @chapters;
    for my $c (@chapters) {
        my (undef, $name, $state) = @$c;
        my $st = $code{$state} or do { problem("Changed Parties: chapter '$name of $state' - not a state"); next; };
        my $x = find_holder($st, $name, 'Changed Parties') or next;   # a senator who held no seat in the window
        my $k = "$st " . surname_key($x->{name});
        problem("Changed Parties: $name ($st) changed party, not declared") unless $change{$k};
        $change{$k}{seen} = 1 if $change{$k};   # never autovivify an undeclared change
    }
    for my $k (sort keys %change) { problem("Changed Parties: $k is no chapter of the page's contents") unless $change{$k}{seen}; }
}
for my $x (@all) {
    my $k = "$x->{st} " . surname_key($x->{name});
    if ($x->{party} eq 'D/I') {
        my $c = $change{$k} or do { problem("$x->{st} $x->{name}: labelled D, I with no declared change"); $x->{i_from} = $window; next; };
        $x->{i_from} = $c->{day} // $window;
    } elsif ($change{$k}) { problem("$x->{st} $x->{name}: a declared change, labelled $x->{label}"); }
}
sub party_on { my ($x, $d) = @_; return $x->{party} ne 'D/I' ? $x->{party} : $d ge $x->{i_from} ? 'I' : 'D'; }
{   # the independents of the window by the Third or Minor Parties page, held to the roster's
    my $h = page('senate/' . one('senate', 'wayback_senate_SenatorsRepresentingThirdorMinorParties_\d{14}\.html'));
    my %named;
    while ($h =~ m{<tr[^>]*>(.*?)</tr>}gs) {
        my $t = words($1);
        next unless $t =~ /^\S+ (.+?) \(([A-Z]{2})\) (Independent(?: Democrat)?) \S+ (\d{4})-(\d{4}|present)/;
        my ($name, $st, $from, $to) = ($1, $2, $4, $5);
        next if $to ne 'present' && $to < 2023;
        my $x = find_holder($st, $name, 'Third or Minor Parties') or do { problem("Third or Minor Parties: $name ($st) matches no holder"); next; };
        $named{$x} = 1;
        my $first = $x->{party} eq 'D/I' ? $x->{i_from} : $x->{took};
        problem("Third or Minor Parties: $name independent from $from, here from $first") unless substr($first, 0, 4) == $from || ($from < 2023 && $first le $window);
        # an independent here on the span's first and last day inside the window, his service ending in its last year
        my $d1 = $first lt $window ? $window : $first;
        my $d2 = $x->{left} eq '-' ? $reach : prev_day($x->{left});
        for my $d ($d1, $d2) { problem("Third or Minor Parties: $name ($st) an independent in $from-$to, here " . party_on($x, $d) . " on $d") unless party_on($x, $d) eq 'I'; }
        problem("Third or Minor Parties: $name ($st) independent to $to, his service here ending " . ($x->{left} eq '-' ? 'never' : $x->{left}))
            unless ($to eq 'present') == ($x->{left} eq '-') && ($to eq 'present' || substr($x->{left}, 0, 4) == $to);
    }
    for my $x (@holders) { problem("$x->{st} $x->{name}: an independent here, not on the Third or Minor Parties page") if ($x->{party} eq 'I' || $x->{party} eq 'D/I') && !$named{$x}; }
}

# ---------------------------------------------------------------- the roster on a day
sub roster {   # [ { st, class, holder or undef, party (D/R/I/-) } ] of the 100 seats at the end of the day
    my $d = shift;
    my @r;
    for my $st (@order) {
        for my $cl (sort keys %{$seat{$st}}) {
            my ($x) = grep { $_->{took} le $d && ($_->{left} eq '-' || $_->{left} gt $d) } @{$seat{$st}{$cl}};
            push @r, { st => $st, class => $cl, x => $x, party => $x ? party_on($x, $d) : '-' };
        }
    }
    return \@r;
}
sub count { my $r = shift; my %c = (D => 0, R => 0, I => 0, '-' => 0); $c{$_->{party}}++ for @$r; return \%c; }
{
    my ($nscap) = one('senate', 'wayback_senate_NewSenators_(\d{14})\.html') =~ /_(\d{8})/;
    problem("the record's reach $reach is not the New Senators page's capture $nscap") unless $reach eq ($nscap =~ s/^(\d{4})(\d\d)(\d\d)$/$1-$2-$3/r);
}

# ---------------------------------------------------------------- the caucus: the Senate Democrats' own lists
my %caucus_seen;   # independent => [ capture days that list him ]
for my $rel (sort grep { /^senate\/wayback_democrats_senate_gov_members_\d{14}\.html$/ } keys %sums) {
    my ($cap) = $rel =~ /_(\d{8})\d{6}\.html$/;
    $cap =~ s/^(\d{4})(\d\d)(\d\d)$/$1-$2-$3/;
    my $h = page($rel);
    my %listed;
    while ($h =~ m{<div class="MemberBox__state">(.*?)</div>.*?<div class="MemberBox__name">(.*?)</div>}gs) {
        my ($state, $nm) = (words($1), words($2));
        my $st = $code{$state} or do { problem("Democrats' list $cap: state '$state'"); next; };
        $nm =~ s/^Senator //;
        $listed{"$st " . surname_key($nm)} = $nm;
    }
    my $r = roster($cap);
    my %want;
    for my $s (@$r) { next unless $s->{x} && ($s->{party} eq 'D' || $s->{party} eq 'I'); $want{"$s->{st} " . surname_key($s->{x}{name})} = $s; }
    for my $k (sort keys %listed) { problem("Democrats' list $cap: $listed{$k} ($k) holds no Democratic or independent seat that day") unless $want{$k}; }
    for my $k (sort keys %want) {
        my $s = $want{$k};
        if ($listed{$k}) { push @{$caucus_seen{$s->{x}}}, $cap if $s->{party} eq 'I'; }
        elsif ($s->{party} eq 'D') { problem("Democrats' list $cap: the Democrat $s->{x}{name} ($s->{st}) not on it"); }
    }
}
for my $x (@holders) {
    next unless $x->{party} eq 'I' || $x->{party} eq 'D/I';
    my $k = "$x->{st} " . surname_key($x->{name});
    my $said = $change{$k} ? $change{$k}{caucus} : undef;
    if ($caucus_seen{$x}) { $x->{caucus_by} = "the Senate Democrats' list of " . join(' and ', @{$caucus_seen{$x}}); }
    elsif ($said) { $x->{caucus_by} = $said; }
    else { problem("$x->{st} $x->{name}: an independent of the window on no Democrats' list, his caucus unsourced"); next; }
    # applied over his whole service in the window (R-US10 (a) as read here); Sinema's is a reading put to Elias, and says so where it travels
    $x->{caucus_by} .= ' - a reading put to Elias (R-US10): the Senate\'s page says she "would not participate in either party caucus"' if $k eq 'AZ sinema';
    $x->{caucus} = 'D';
}

# ---------------------------------------------------------------- the anchors: the class pages, the Congressional Directory
for my $c ([1, 'I'], [2, 'II'], [3, 'III']) {
    my ($cl, $roman) = @$c;
    my $rel = 'records/' . one('records', "wayback_senate_class_${roman}_\\d{8}\\.html");
    my $h = page($rel);
    my ($meta) = $h =~ /<meta name="date" content="[A-Za-z]+, ([A-Za-z]+ \d+, \d{4})">/ or do { problem("$rel: no page date"); next; };
    my $d = day($meta, "$rel date");
    my %page;
    while ($h =~ m{<td><a href="[^"]*">([^<]+)</a>\s*\(([DRI])-([A-Z]{2})\)</td>}g) { my ($nm, $p, $st) = ($1, $2, $3); $nm = ascii($nm); $nm =~ s/^(.+?), (.+)$/$2 $1/; $page{"$st " . surname_key($nm)} = $p; }
    my %roster = map { ("$_->{st} " . surname_key($_->{x} ? $_->{x}{name} : '-') => $_->{party}) } grep { $_->{class} == $cl } @{roster($d)};
    for my $k (sort keys %page) { problem("Class $roman ($d): $k $page{$k} on the page, the roster " . ($roster{$k} // 'no such senator')) unless ($roster{$k} // '') eq $page{$k}; }
    for my $k (sort keys %roster) { problem("Class $roman ($d): the roster's $k ($roster{$k}) not on the page") unless exists $page{$k}; }
    print "Class $roman, its page's date $d: " . scalar(keys %page) . " senators, the roster's the same\n" unless grep { /Class $roman/ } @problems;
}
my %cdir_omits = ('2024-04-25 class 3' => 'NC budd');   # the Directory of 2024 lists Class III without Ted Budd (NC), whom its own totals count
my %cdir_said;
for my $ed (['2024-04-25', '2024-04-25'], ['2026-02-20', '2025-10-01']) {
    my ($edition, $closing) = @$ed;
    my $t = pdf_text("senate/govinfo_cdir_$edition.pdf");
    my ($cd) = $t =~ /Closing date for compilation of the Congressional Directory was ([A-Za-z]+ \d+, \d{4})\./ or do { problem("CDIR $edition: no closing date"); next; };
    problem("CDIR $edition: closing date " . day($cd, 'CDIR') . ", declared $closing") unless day($cd, 'CDIR') eq $closing;
    (my $flat = $t) =~ s/\s+/ /g;
    my ($asof, $a1, $n1, $a2, $n2, $n3, $tot) = $flat =~ /membership of the Senate as of the end of the legislative day of ([A-Za-z]+ \d+, \d{4})\. (\w+) in roman \((\d+)\); (\w+) in italic \((\d+)\); Independents in SMALL CAPS \((\d+)\); total \((\d+)\)/
        or do { problem("CDIR $edition: the Senate's count not found"); next; };
    problem("CDIR $edition: its count is as of " . day($asof, 'CDIR') . ", its closing day $closing") unless day($asof, 'CDIR') eq $closing;
    my %said = (substr($a1, 0, 1) => $n1, substr($a2, 0, 1) => $n2, I => $n3);
    my $c = count(roster($closing));
    problem("CDIR $edition ($closing): D $said{D} R $said{R} I $said{I} total $tot, the roster D $c->{D} R $c->{R} I $c->{I} vacant $c->{'-'}")
        unless $said{D} == $c->{D} && $said{R} == $c->{R} && $said{I} == $c->{I} && $tot == 100 && $c->{'-'} == 0;
    $cdir_said{$closing} = \%said;
    if ($edition eq '2024-04-25') {   # its Terms of Service: each class's listing on the closing day, counted entry by entry
        my @lines = split /\n/, $t;
        my @at = grep { $lines[$_] =~ /^CLASS (I|II|III)\.--SENATORS WHOSE TERMS OF SERVICE EXPIRE IN/ } 0 .. $#lines;
        problem("CDIR $edition: " . scalar(@at) . " class listings, not 3") unless @at == 3;
        for my $i (0 .. $#at) {
            my ($bracket) = $lines[$at[$i] + 1] =~ /^\[(3\d) Senators in this group: ([^\]]+)\]$/ ? ($2) : ();
            my %listed = (D => 0, R => 0, I => 0);
            my %entry;   # "XX surname" => party, as listed
            for (my $j = $at[$i] + 2; $j <= $#lines && $lines[$j] !~ /^CLASS [IV]+\.--/ && $lines[$j] !~ /^\d+ Senator /; $j++) {
                next unless $lines[$j] =~ /^(.+?)\s*\d?\s*\.{3,}\s*(D|R|I)\.\s.*?, ([A-Z]{2})\.$/;
                $listed{$2}++; $entry{"$3 " . surname_key($1)} = $2;
            }
            my %roster_class = map { ("$_->{st} " . surname_key($_->{x}{name}) => $_->{party}) } grep { $_->{class} == $i + 1 && $_->{x} } @{roster($closing)};
            my @missing = grep { !exists $entry{$_} } sort keys %roster_class;
            my @extra = grep { !exists $roster_class{$_} } sort keys %entry;
            my @wrong = grep { exists $roster_class{$_} && $roster_class{$_} ne $entry{$_} } sort keys %entry;
            my $declared = $cdir_omits{"$edition class " . ($i + 1)} // '';
            problem("CDIR $edition: class " . ($i + 1) . ": the roster's [@missing] not listed, [@extra] listed and not the roster's, [@wrong] another party")
                unless join(' ', @missing) eq $declared && !@extra && !@wrong;
            push @{$cdir_said{slips}}, "class " . ($i + 1) . "'s listing omits $declared (DECLARED)" if $declared ne '';
            my %p = map { /^(\w)\w+, (\d+)$/ ? ($1 => $2) : () } split /; /, $bracket // '';
            push @{$cdir_said{slips}}, "class " . ($i + 1) . "'s bracket says " . ($bracket // 'nothing') . ", its own listing D $listed{D} R $listed{R} I $listed{I}"
                unless ($p{D} // 0) == $listed{D} && ($p{R} // 0) == $listed{R} && ($p{I} // 0) == $listed{I};
        }
    }
    # its table of changes: each successor's oath must be the New Senators page's (the change days it gives are printed, not used)
    my ($tab) = $t =~ /CHANGES IN THE SENATE FOR THE 11\dTH CONGRESS\n(.*?)\nCHANGES IN THE HOUSE/s or do { problem("CDIR $edition: no Senate changes table"); next; };
    $tab =~ s/^.*?Sworn in\n//s or problem("CDIR $edition: its Senate changes table has no 'Sworn in' heading");
    $tab =~ s/\n\d .*\z//s;   # its footnote
    $tab =~ s/\n/ /g;          # a name may wrap ("James David ``JD''" / "Vance.")
    my $rows = () = $tab =~ /\s[A-Z]{2} \d{1,2}-\d{1,2}-\d\d \S/g;   # a row by its shape: a state and a change day, whatever its reason
    while ($tab =~ /\s[A-Z]{2} \d{1,2}-\d{1,2}-\d\d (\S+)/g) { problem("CDIR $edition: a change for the reason '$1', not a resignation or a death") unless $1 eq 'Resigned' || $1 eq 'Died'; }
    my $n = 0;
    while ($tab =~ /\G\s*(.+?) \.*\s*([A-Z]{2}) (\d{1,2}-\d{1,2}-\d\d) (Resigned|Died) (.+?) ?\d? \.+ \[?(\d{1,2}-\d{1,2}-\d\d)\]? (\d{1,2}-\d{1,2}-\d\d)(?= |$)/g) {
        my ($out, $st, $when, $why, $succ, $chosen, $sworn) = ($1, $2, $3, $4, $5, $6, $7);
        $n++;
        $succ =~ s/\s*\d$//;
        my $x = find_holder($st, $succ, "CDIR $edition") or do { problem("CDIR $edition: successor $succ ($st) matches no holder"); next; };
        problem("CDIR $edition: $succ sworn " . day($sworn, 'CDIR') . ", the New Senators page " . ($x->{sworn} // 'none')) unless ($x->{sworn} // '') eq day($sworn, 'CDIR');
        my $o = find_holder($st, $out =~ s/\s*``[^']*''\s*/ /r, "CDIR $edition");
        push @{$cdir_said{changes}}, "$st $out: $why " . day($when, 'CDIR') . " by the Directory, " . ($o ? $o->{left} : '?') . " by ${st}'s page; $succ chosen " . day($chosen, 'CDIR') . " by the Directory, " . ($x->{appointed} // '-') . " by the Appointed Senators page, sworn " . day($sworn, 'CDIR');
    }
    problem("CDIR $edition: its changes table read $n of its $rows rows") unless $n && $n == $rows;
}

# ---------------------------------------------------------------- [SEN-DIV]: each Congress's line, and the days of its Congress it holds on
my %div;
my $note117;   # [SEN-DIV]'s 117th note as the roster holds it, printed
{
    my $t = words(page('records/' . one('records', 'wayback_senate_party_division_\d{8}\.html')));
    while ($t =~ /(11[789])th Congress \(\d{4}-\d{4}\) Majority Party: (\w+) \((\d+) seats\) Minority Party: (\w+) \((\d+) seats\) Other Parties: (.*?) Total Seats: (\d+)/g) {
        my ($c, $ma, $man, $mi, $min, $oth, $tot) = ($1, $2, $3, $4, $5, $6, $7);
        my ($ni) = $oth =~ /(\d+) Independents?/;
        $div{$c} = { substr($ma, 0, 1) => $man, substr($mi, 0, 1) => $min, I => $ni // 0, total => $tot, other => $oth };
    }
    problem("[SEN-DIV]: lines for " . join(' ', sort keys %div) . ", not the 117th-119th") unless keys %div == 3;
    # its 119th note: a senator-elect's delayed oath and a resignation, each held to the roster's day
    if ($t =~ /includes Senator-elect (.+?) of ([A-Z][a-z]+(?: [A-Z][a-z]+)?), who chose to delay his swearing-in to ([A-Z][a-z]+ \d+, \d{4})/) {
        my ($nm, $state, $d) = ($1, $2, day($3, '[SEN-DIV] note'));
        my $x = $code{$state} ? find_holder($code{$state}, $nm, '[SEN-DIV] note') : undef;
        problem("[SEN-DIV] note: $nm sworn $d, the roster " . ($x ? $x->{took} : 'no such senator')) unless $x && $x->{took} eq $d;
    } else { problem("[SEN-DIV]: its 119th note on a senator-elect's delayed oath not found"); }
    if ($t =~ /With the resignation of (.+?) effective ([A-Z][a-z]+ \d+, \d{4}), ([A-Z][a-z]+(?: [A-Z][a-z]+)?)'s Class (\d) seat/) {
        my ($nm, $d, $state, $cl) = ($1, day($2, '[SEN-DIV] note'), $3, $4);
        my $x = $code{$state} ? find_holder($code{$state}, $nm, '[SEN-DIV] note') : undef;
        problem("[SEN-DIV] note: $nm resigned $d from class $cl, the roster " . ($x ? "$x->{left}, class $x->{class}" : 'no such senator')) unless $x && $x->{left} eq $d && $x->{class} == $cl;
    } else { problem("[SEN-DIV]: its 119th note on a resignation not found"); }
    # its 117th note: the division from the 117th's opening to the January 2021 oaths - held on every day it names but its last (whose end
    # already holds the new senators), the early rows on their state pages' days
    if ($t =~ /Note: From ([A-Z][a-z]+ \d+, \d{4}), to ([A-Z][a-z]+ \d+, \d{4}), party division stood at (\d+) Republicans, (\d+) Democrats, (\d+) Independents \(who caucused with the Democrats\), and (\d+) vacancy\./) {
        my ($from, $to, $r, $d, $i, $v) = (day($1, '[SEN-DIV] 117th note'), day($2, '[SEN-DIV] 117th note'), $3, $4, $5, $6);
        # the named slip, DECLARED: Georgia's page ends Loeffler's service on 19 Jan 2021, the day before Warnock's oath, so at that day's end
        # her seat reads vacant; the note counts her to 20 Jan. On the slip's day the roster must be the note's less her seat, and hers its last
        my %slip = ('2021-01-19' => 'GA loeffler');
        my @slips;
        for (my $day = $from; $day lt $to; $day = next_day($day)) {
            my $k = count(roster($day));
            my ($wr, $wv) = ($r, $v);
            if (my $who = $slip{$day}) {
                my ($sst, $sk) = split / /, $who;
                my ($x) = grep { $_->{st} eq $sst && surname_key($_->{name}) eq $sk } @all;
                problem("[SEN-DIV] 117th note: the slip $who on $day, the row " . ($x ? "left $x->{left}, party $x->{party}" : 'not found')) unless $x && $x->{left} eq $day && $x->{party} eq 'R';
                ($wr, $wv) = ($r - 1, $v + 1);
                $slip{$day} = '';
                push @slips, "$day: ${who}'s last day by ${sst}'s page, R $wr vacant $wv";
            }
            if ($k->{R} != $wr || $k->{D} != $d || $k->{I} != $i || $k->{'-'} != $wv) { problem("[SEN-DIV] 117th note: R $wr D $d I $i vacant $wv on $day (the note's R $r vacant $v from $from to $to); the roster R $k->{R} D $k->{D} I $k->{I} vacant $k->{'-'}"); last; }
        }
        for my $s (sort keys %slip) { problem("[SEN-DIV] 117th note: the slip on $s declared and not met") if $slip{$s} ne ''; }
        $note117 = "R $r D $d I $i vacant $v on every day from $from to " . prev_day($to) . (@slips ? ' but the slip DECLARED - ' . join('; ', @slips) : '');
    } else { problem("[SEN-DIV]: its 117th note on the division before the January 2021 oaths not found"); }
}
my %congress =(117 => ['2021-01-03', '2023-01-02'], 118 => ['2023-01-03', '2025-01-02'], 119 => ['2025-01-03', $reach]);
my %holds;   # congress => [ [from, to] ... ] the stretches of days its line holds on
for my $c (117, 118, 119) {
    my ($from, $to) = @{$congress{$c}};
    my $want = $div{$c} or next;
    my $run;
    for (my $d = $from; $d le $to; $d = next_day($d)) {
        my $k = count(roster($d));
        my $ok = $k->{D} == $want->{D} && $k->{R} == $want->{R} && $k->{I} == $want->{I} && $k->{'-'} == 0;
        if ($ok) { $run //= [$d, $d]; $run->[1] = $d; } elsif ($run) { push @{$holds{$c}}, $run; undef $run; }
    }
    push @{$holds{$c}}, $run if $run;
    problem("[SEN-DIV] $c: D $want->{D} R $want->{R} I $want->{I} holds on no day from $from to $to") unless $holds{$c};
}

# ---------------------------------------------------------------- the named days, the seats and the changes
my @named = (['2017-01-03', 'the 115th Congress opens'], ['2018-11-06', 'the general election of 2018'], ['2019-01-03', 'the 116th Congress opens'],
    ['2020-11-03', 'the general election of 2020'], ['2021-01-03', 'the 117th Congress opens'], ['2022-11-08', 'the general election of 2022'],
    ['2023-01-03', 'the 118th Congress opens'],['2024-03-12', 'the US start (WorldClock.StartDate)'], ['2024-03-18', "the Senate Democrats' list captured"],
    ['2024-04-25', 'the Congressional Directory of 2024 closes'], ['2025-01-02', "the 118th Congress's last day"], ['2025-01-03', 'the 119th Congress opens'],
    ['2025-01-14', "the Class I page's date"], ['2025-01-21', "the Class III page's date"], ['2025-10-01', 'the Congressional Directory of 2026 closes'],
    ['2026-07-14', "the Class II page's date"], ['2026-09-05', "the Senate Democrats' list captured"], [$reach, "the record's reach (the New Senators page captured)"]);
my @onrows;
for my $n (@named) {
    my ($d, $what) = @$n;
    my $r = roster($d);
    problem("$d: " . scalar(@$r) . ' seats, not 100') unless @$r == 100;
    my %byc;
    for my $s (@$r) { $byc{$s->{class}}{$s->{party}}++; }
    my $k = count($r);
    my $dem_caucus = $k->{D} + scalar(grep { $_->{party} eq 'I' && ($_->{x}{caucus} // '') eq 'D' } @$r);
    push @onrows, [$d, $what, (map { my $cl = $_; map { $byc{$cl}{$_} // 0 } qw(D R I -) } 1 .. 3), $k->{D}, $k->{R}, $k->{I}, $k->{'-'}, $dem_caucus, $k->{R}];
}
my @changes;   # day, state, class, kind, senator, detail - the window's seat changes other than a Congress's opening
for my $x (@holders) {
    if ($x->{left} ne '-' && $x->{left} ne '2025-01-03') { push @changes, [$x->{left}, $x->{st}, $x->{class}, $x->{how_out}, $x->{name}, "$x->{st}'s page"]; }
    if ($x->{took} gt $window && $x->{took} ne '2025-01-03') {
        my $det = $x->{how_in} . ($x->{appointed} ? " $x->{appointed}" : '') . ($x->{began} ne $x->{took} ? "; $x->{st}'s page has $x->{began}" : '');
        push @changes, [$x->{took}, $x->{st}, $x->{class}, 'sworn', $x->{name}, $det];
    }
    push @changes, [$x->{i_from}, $x->{st}, $x->{class}, 'independent', $x->{name}, 'the Changed Parties page'] if $x->{i_from} && $x->{i_from} gt $window;
}
@changes = sort { $a->[0] cmp $b->[0] || $a->[1] cmp $b->[1] || $a->[3] cmp $b->[3] } @changes;

$SIG{__WARN__} = 'DEFAULT';
if (@problems) { my $n = @problems; print STDERR "MISMATCH: $_\n" for splice @problems; die "$n mismatch(es) - nothing written\n"; }

# ---------------------------------------------------------------- the CSVs and the catalog's part
sub write_all {
    my @out = @_;
    for (my $i = 0; $i < @out; $i += 2) {
        my ($f, $t) = @out[$i, $i + 1];
        if ($t =~ /[^\x00-\x7F]/) { my $line = 1 + (substr($t, 0, $-[0]) =~ tr/\n//); die "$f: not ASCII at its line $line - nothing written\n"; }
        next unless $f =~ /\.csv$/;   # a CSV's every row has its heading's fields: no value carries a comma
        my ($head, @rows) = grep { !/^#/ } split /\n/, $t;
        my $n = () = $head =~ /,/g;
        for my $r (@rows) { my $k = () = $r =~ /,/g; die "$f: '$r' has " . ($k + 1) . " fields, its heading " . ($n + 1) . " - nothing written\n" unless $k == $n; }
    }
    for (my $i = 0; $i < @out; $i += 2) {
        my ($f, $t) = @out[$i, $i + 1];
        open my $w, '>:raw', $f or die "$f: $!\n";
        print $w $t or die "$f: $!\n";
        close $w or die "$f: $!\n";
    }
}
my $gen = "# GENERATED by Tools/us_senate_prep.pl from the pages under ElectionsData/usa/raw/ - DO NOT EDIT; the record is ElectionsData/usa/senate_record.md.\n";
my $csv_s = $gen . "# US-12: a row a senator who held a seat on or after 3 Jan 2023 (the 118th Congress's opening, the window), by his state's \"States in\n"
    . "# the Senate\" page - and (s795) every senator whose service ended from 4 Jan 2017 to that day, the early rows, on his state page's own days.\n"
    . "# party: D, R or I as that page prints it, D/I for a Democrat who became an independent (independent_from: the day, by the Changed Parties\n"
    . "# page). took: the day of his oath by the New Senators page for a senator new in the 118th or 119th Congress, else the day his state's page\n"
    . "# gives (before the window - which may be an appointment's day, not an oath's). left: the day his service ended, by his state's page (-\n"
    . "# while he serves, or when it ends after the record's reach). how_in: general election, special election or appointed (the New Senators\n"
    . "# page), or before the window; how_out: term ended, resigned or died. caucus: an independent's, applied over his whole service in the\n"
    . "# window (R-US10 (a) as read at US-12), by the Senate Democrats' own list or the Senate's own words (caucus_by), else -. The roster on a\n"
    . "# day is the state at its end: a senator counts from his oath's day and not on his last.\n"
    . "state,class,senator,party,independent_from,took,left,how_in,how_out,caucus,caucus_by\n";
my @srows;
for my $x (@all) { my @row = ($x->{st}, $x->{class}, $x->{name} =~ s/,//gr, $x->{party}, $x->{i_from} // '-', $x->{took}, $x->{left}, $x->{how_in}, $x->{how_out}, $x->{caucus} // '-', $x->{caucus_by} // '-'); push @srows, \@row; $csv_s .= join(',', @row) . "\n"; }
my $csv_c = $gen . "# US-12: a row a seat change of the 118th and 119th Congresses to the record's reach, a Congress's opening apart (its retiring senators'\n"
    . "# term ends and its new senators' oaths on 3 Jan): kind resigned, died, sworn (an oath on another day - an appointee's, or a late one) or\n"
    . "# independent (a party change); detail: how and by which page, and the state page's own day where the oath differs from it.\n"
    . "day,state,class,kind,senator,detail\n" . join('', map { join(',', @$_) . "\n" } @changes);
my $csv_o = $gen . "# US-12: the Senate at the end of named days - by class and party (D, R, I, - vacant), then the whole, and with the independents\n"
    . "# folded into the conference they caucus with (R-US10 (a)): dem_caucus, rep_caucus. The record reaches $reach.\n"
    . "day,what,c1_d,c1_r,c1_i,c1_vacant,c2_d,c2_r,c2_i,c2_vacant,c3_d,c3_r,c3_i,c3_vacant,d,r,i,vacant,dem_caucus,rep_caucus\n"
    . join('', map { join(',', @$_) . "\n" } @onrows);
my $csv_v = $gen . "# US-12: the party division of each Congress as the Senate's Party Division page gives it ([SEN-DIV]: one undated line a Congress),\n"
    . "# and each stretch of the Congress's days, to the record's reach, on which the roster gives that line exactly with no seat vacant.\n"
    . "congress,d,r,i,holds_from,holds_to\n";
my @vrows;
for my $c (117, 118, 119) { for my $h (@{$holds{$c}}) { my @row = ($c, $div{$c}{D}, $div{$c}{R}, $div{$c}{I}, @$h); push @vrows, \@row; $csv_v .= join(',', @row) . "\n"; } }
my ($ds, $dc, $do, $dv) = map { sha256_hex($_) } ($csv_s, $csv_c, $csv_o, $csv_v);
sub cq { my $s = shift; $s =~ s/\\/\\\\/g; $s =~ s/"/\\"/g; return "\"$s\""; }
my $cs = "// GENERATED by Tools/us_senate_prep.pl. DO NOT EDIT BY HAND.\n//\n"
    . "// Sources: ElectionsData/usa/senate_seats.csv, senate_changes.csv, senate_on.csv and senate_division.csv (their SHA-256 below), written by\n"
    . "// the same run from the saved pages listed in SenateRawSources. GeneratedCatalogCheck re-reads the CSVs and compares every figure, and\n"
    . "// re-hashes every page listed.\n\n"
    . "namespace PoliSim.Elections.Generated\n{\n"
    . "    // PS-6 US-12 (COMPLETED.md s792): the Senate by state and by date, the 118th and 119th Congresses - a part of the partial class\n"
    . "    // UsPresidentialReturns (UsPresidentialReturns.cs, written by Tools/us_returns_prep.pl). Generated, never hand-edited. Its tables are built\n"
    . "    // by methods: the partial class has one static initializer, which Mono's JIT overflowed once already (UsHouseDistricts.cs).\n"
    . "    public static partial class UsPresidentialReturns\n    {\n"
    . "        public const string SenateSeatSourceDigest = \"$ds\";\n        public const string SenateChangeSourceDigest = \"$dc\";\n"
    . "        public const string SenateOnSourceDigest = \"$do\";\n        public const string SenateDivisionSourceDigest = \"$dv\";\n\n"
    . "        /// <summary>Every saved page the Senate run read, by its path under `ElectionsData/usa/`, with its SHA-256.</summary>\n"
    . "        public static readonly (string Path, string Sha256)[] SenateRawSources = BuildSenateRawSources();\n\n"
    . "        /// <summary>A row a senator who held a seat on or after 3 Jan 2023, or whose service ended from 4 Jan 2017 to that day (s795, on his
        /// state page's days): state, class (1-3), name, party (D, R, I; D/I a Democrat who became an\n"
    . "        /// independent on IndependentFrom), the day he took the seat (Took: his oath for a senator new in the 118th or 119th\n"
    . "        /// Congress, else his state page's day, which may be an appointment's), the day his service ended (Left, a hyphen while he serves),\n"
    . "        /// how he came and went, and an independent's caucus with its source (senate_seats.csv's header says each).</summary>\n"
    . "        public static readonly (string State, int Class, string Senator, string Party, string IndependentFrom, string Took, string Left, string HowIn, string HowOut, string Caucus, string CaucusBy)[] SenateSeats = BuildSenateSeats();\n\n"
    . "        /// <summary>A row a seat change of the 118th and 119th Congresses, a Congress's opening apart: day, state, class, kind, senator, detail.</summary>\n"
    . "        public static readonly (string Day, string State, int Class, string Kind, string Senator, string Detail)[] SenateChanges = BuildSenateChanges();\n\n"
    . "        /// <summary>The Senate at the end of named days, by class and party (vacant seats apart), and with the independents in their caucus.</summary>\n"
    . "        public static readonly (string Day, string What, int C1D, int C1R, int C1I, int C1Vacant, int C2D, int C2R, int C2I, int C2Vacant, int C3D, int C3R, int C3I, int C3Vacant, int D, int R, int I, int Vacant, int DemCaucus, int RepCaucus)[] SenateOn = BuildSenateOn();\n\n"
    . "        /// <summary>Each Congress's party division by the Senate's page ([SEN-DIV]) and each stretch of its days on which the roster gives it.</summary>\n"
    . "        public static readonly (int Congress, int D, int R, int I, string HoldsFrom, string HoldsTo)[] SenateDivision = BuildSenateDivision();\n\n"
    . "        private static (string Path, string Sha256)[] BuildSenateRawSources() => new (string, string)[]\n        {\n"
    . join('', map { "            (\"$_\", \"$read{$_}\"),\n" } sort keys %read) . "        };\n\n"
    . "        private static (string State, int Class, string Senator, string Party, string IndependentFrom, string Took, string Left, string HowIn, string HowOut, string Caucus, string CaucusBy)[] BuildSenateSeats() => new (string, int, string, string, string, string, string, string, string, string, string)[]\n        {\n"
    . join('', map { "            (" . join(', ', cq($_->[0]), $_->[1], map { cq($_) } @$_[2 .. 10]) . "),\n" } @srows) . "        };\n\n"
    . "        private static (string Day, string State, int Class, string Kind, string Senator, string Detail)[] BuildSenateChanges() => new (string, string, int, string, string, string)[]\n        {\n"
    . join('', map { "            (" . join(', ', cq($_->[0]), cq($_->[1]), $_->[2], cq($_->[3]), cq($_->[4]), cq($_->[5])) . "),\n" } @changes) . "        };\n\n"
    . "        private static (string Day, string What, int C1D, int C1R, int C1I, int C1Vacant, int C2D, int C2R, int C2I, int C2Vacant, int C3D, int C3R, int C3I, int C3Vacant, int D, int R, int I, int Vacant, int DemCaucus, int RepCaucus)[] BuildSenateOn() => new (string, string, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int)[]\n        {\n"
    . join('', map { "            (" . join(', ', cq($_->[0]), cq($_->[1]), @$_[2 .. 19]) . "),\n" } @onrows) . "        };\n\n"
    . "        private static (int Congress, int D, int R, int I, string HoldsFrom, string HoldsTo)[] BuildSenateDivision() => new (int, int, int, int, string, string)[]\n        {\n"
    . join('', map { "            ($_->[0], $_->[1], $_->[2], $_->[3], " . cq($_->[4]) . ', ' . cq($_->[5]) . "),\n" } @vrows) . "        };\n    }\n}\n";
$cs =~ s/\n/\r\n/g;
write_all("$usa/senate_seats.csv" => $csv_s, "$usa/senate_changes.csv" => $csv_c, "$usa/senate_on.csv" => $csv_o, "$usa/senate_division.csv" => $csv_v,
          'Assets/Scripts/Elections/Generated/UsSenateRecord.cs' => $cs);

# ---------------------------------------------------------------- the run's report (the record pastes it)
for my $o (@onrows) { printf "%s %-55s D %2d R %2d I %d vacant %d - with the caucus DEM %d REP %d\n", $o->[0], $o->[1], @$o[14 .. 19]; }
for my $c (117, 118, 119) { printf "[SEN-DIV] %dth: D %d R %d I %d - holds %s\n", $c, $div{$c}{D}, $div{$c}{R}, $div{$c}{I}, join(', ', map { "$_->[0] to $_->[1]" } @{$holds{$c}}); }
print "change: " . join(' ', @$_) . "\n" for @changes;
print "the Directory's table: $_\n" for @{$cdir_said{changes} // []};
print "the Directory's slip: $_\n" for @{$cdir_said{slips} // []};
print "[SEN-DIV] 117th note: $note117
";
print "early appointee: $_
" for @early_appointed;
print "the record's reach $reach: " . (@beyond ? join('; ', @beyond) : 'nothing beyond it on the pages read') . "\n";
print "caucus: $_->{st} $_->{name} ($_->{party}) with the Democrats, by $_->{caucus_by}\n" for grep { $_->{caucus} } @holders;
print "wrote $usa/senate_seats.csv (" . scalar(@srows) . " senators), senate_changes.csv (" . scalar(@changes) . "), senate_on.csv (" . scalar(@onrows) . " days), senate_division.csv, Assets/Scripts/Elections/Generated/UsSenateRecord.cs\n";
