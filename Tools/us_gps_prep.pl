#!/usr/bin/perl
# PS-6 US-32 - THE GLOBAL PARTY SURVEY 2019'S TWO US PARTIES, READ FOR THE ROSTER. Reads only files kept OUT OF TREE under
# ../PoliSim-captures/sources/ (ruling G7, COMPLETED.md s797: the file fetched, its digest kept, never the raw file committed), each held to
# its line in ElectionsData/positions/raw/out_of_tree.txt before a byte of it is used:
#   usa_us32/gps2019_by_party_v2.tab       the by-party dataset (Harvard Dataverse doi:10.7910/DVN/WMGTNS, file 3788852, the table Dataverse
#                                          serves): the experts' mean per item, the file's own text
#   usa_us32/gps2019_by_expert.tab         the by-expert dataset (file 3679272): every expert's placement of each party - the spread, and every
#                                          mean recomputed from it
#   usa_us32/gps2019_codebook_6feb2020.pdf the Technical Note and Codebook (file 3679273): each item's question and endpoints, held as quoted
#                                          (its text by pdftotext -raw)
#   usa_us32/dataverse_files_listing.json  the dataset's file listing: the MD5 Dataverse publishes for each original upload, held against the
#                                          originals kept beside (the .sav files, the by-party CSV, the codebook, the questionnaire)
#   usa_us32/gps2019_by_party_v2-2.csv     the by-party CSV upload (file 3788854): every mean read is held to it as a second publication
# and writes
#   ElectionsData/positions/gps_2019_usa.csv         a row per US party per item read: the file's mean as printed, the two decimals the roster
#                                                    carries, the experts' spread (count, sd, min, quartiles, max)
#   Assets/Scripts/Data/Generated/UsGps2019.cs       the CSV as C# literals, its SHA-256, every source file's SHA-256 (RawSources), each item's
#                                                    two decimals as a constant the roster reads
# GeneratedCatalogCheck re-reads the CSV against the catalog, re-hashes every file RawSources lists, holds every mean to the by-party file's
# own cell and the roster to the constants. Tools/us_gps_mutations.sh proves the failure paths below on a copy of the inputs.
#
# THE READINGS (declared; the record is ElectionsData/positions/party_positions.md):
#   - the items: V4_Scale (economic left-right, Q3.1) and V6_Scale (social liberal-conservative, Q3.3) - the roster's LrEcon and Galtan since
#     2026-08-28; V8_Scale (populist rhetoric, Q3.5) - the roster's PeopleVsElite, the plan's mapping (US-32: "read V8 to replace the
#     [AUTHORED-DRAFT] people-versus-elite values"), a reading because V8's wording is not CHES people_v_elite's; V9 (populist saliency,
#     Q3.6), V10 (immigration, Q4.1) and V19 (people should decide, Q5.2) read and printed, carried by no roster field: V9 is the salience a
#     CHES anti_elite_salience reading would take and is HELD under G7 (it moves a cast); V10 has no field in the US rows; V19 is the item the
#     codebook itself marks as CHES PEOPLE_VS_ELITE, on the reversed scale - an alternative reading put to Elias, not installed;
#   - the two decimals: half away from zero on the file's own decimal digits (8.225 -> 8.23), the convention every survey figure in the roster
#     has carried;
#   - a placement is an expert's answer 0-10; 99 (DK/NA/MISSING, the codebook) and an empty cell are not placements; the spread is over the
#     placements: the sample standard deviation (n - 1), the quartiles by linear interpolation between order statistics (position (n-1)p).
#
# It writes nothing and dies on any mismatch, naming every one it has found:
#   - a file off its out_of_tree.txt line (bytes or SHA-256), or not on the list; an original whose MD5 is not the listing's;
#   - the by-party file without exactly one row for each US party (ISO "USA", its GPS id and name), or a column the tool reads missing;
#   - a mean that is not a decimal number on the 0-10 scale; a mean the by-party CSV prints differently (beyond 1e-12);
#   - the by-expert file's US rows not naming the two parties as its first two columns, a placement outside 0-10 that is not 99, an item
#     without a placement; a mean recomputed from the placements that is not the by-party file's (beyond 1e-12);
#   - the codebook's text without an item's question or its endpoints as quoted below, or without V19's CHES marker;
#   - a perl warning during the checks (an undefined value reaching one).
# Both outputs are built before the first is written; a write that fails dies.
# Usage: perl Tools/us_gps_prep.pl   (from the project root, Git for Windows' perl; pdftotext at /mingw64/bin or on PATH)
# ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use Digest::MD5 qw(md5_hex);
use Text::ParseWords qw(parse_line);

my $list = 'ElectionsData/positions/raw/out_of_tree.txt';
my $src = $ENV{US32_SRC} // '../PoliSim-captures/sources';   # the mutation suite points this at its own copies
my $csvOut = 'ElectionsData/positions/gps_2019_usa.csv';
my $csOut = 'Assets/Scripts/Data/Generated/UsGps2019.cs';
my $pdftotext = -x '/mingw64/bin/pdftotext' ? '/mingw64/bin/pdftotext' : 'pdftotext';
die "run from the project root\n" unless -f $list;
my @problems;
sub problem { push @problems, join('', @_); return; }
$SIG{__WARN__} = sub { problem('a perl warning: ', $_[0] =~ s/\s+$//r) };
$SIG{__DIE__} = sub { return if $^S; print STDERR "MISMATCH: $_\n" for splice @problems; };

# ---------------------------------------------------------------- the files, each held to its out_of_tree.txt line before it is used
my (%want, %bytes, %order);
open my $l, '<', $list or die "$list: $!\n";
while (my $line = <$l>) {
    next if $line =~ /^#/;
    $line =~ s/\r?\n$//;
    my @f = split /\t/, $line;
    die "$list: a line of " . scalar(@f) . " field(s), not 5 (sha256, bytes, path, url, fetched)\n" unless @f == 5;
    die "$list: '$f[0]' is not a SHA-256\n" unless $f[0] =~ /^[0-9a-f]{64}$/;
    $want{$f[2]} = $f[0]; $bytes{$f[2]} = $f[1]; $order{$f[2]} = scalar keys %order;
}
close $l;
my %read;   # every file read or relied on, its path under PoliSim-captures/sources/ => SHA-256
sub held {
    my $rel = shift;
    my $w = $want{$rel} // die "$rel: not in $list - the tool reads listed files only\n";
    my $f = "$src/$rel";
    open my $h, '<:raw', $f or die "$f: $!\n";
    local $/;
    my $b = <$h>;
    close $h;
    die "$f: " . length($b) . " bytes, $list holds $bytes{$rel}\n" unless length($b) == $bytes{$rel};
    my $d = sha256_hex($b);
    die "$f: SHA-256 $d, $list holds $w\n" unless $d eq $w;
    $read{$rel} = $d;
    return $b;
}

# the listing's MD5 for every original upload, held against the originals kept beside
my $listing = held('usa_us32/dataverse_files_listing.json');
my %md5;   # original file name => md5
while ($listing =~ /\{"(?:description":"[^"]*","label|label)":"[^"]*".*?"filename":"([^"]*)"(.*?)"md5":"([0-9a-f]{32})"/gs) {
    my ($name, $mid, $m) = ($1, $2, $3);
    my $orig = $mid =~ /"originalFileName":"([^"]*)"/ ? $1 : $name;
    $md5{$orig} = $m;
}
for my $p (['usa_us32/gps2019_by_party_v2.sav', 'Global Party Survey by Party SPSS V2_1_Apr_2020.sav'],
           ['usa_us32/gps2019_by_party_v2-2.csv', 'Global Party Survey by Party SPSS V2_1_Apr_2020-2.csv'],
           ['usa_us32/gps2019_by_expert.sav', 'Global Party Survey by Expert 29 Dec 2019.sav'],
           ['usa_us32/gps2019_codebook_6feb2020.pdf', 'Global Party Survey Technical Note and Codebook 6 Feb 2020.pdf'],
           ['usa_us32/gps2019_questionnaire.pdf', 'Global Party Survey Questionnaire.pdf']) {
    my ($rel, $orig) = @$p;
    my $m = $md5{$orig};
    unless (defined $m) { problem("the listing names no original '$orig'"); next; }
    my $got = md5_hex(held($rel));
    problem("$rel: MD5 $got, the Dataverse listing publishes $m for '$orig'") unless $got eq $m;
}

# ---------------------------------------------------------------- the items, the parties, the roster's fields
# item => [question in the by-expert file, roster field or 'none', the C# stem, the codebook's question, the codebook's endpoints]
my @items = (
    ['V4_Scale', 'Q3.1', 'LrEcon', 'EconomicV4',
     'Q3.1 Parties can be classified by their current stance on ECONOMIC ISSUES such as privatization, taxes, regulation, government spending, and the welfare state.',
     '0. Extreme economic left 10. Extreme economic right'],
    ['V6_Scale', 'Q3.3', 'Galtan', 'SocialV6',
     'Q3.3 Parties can also be classified by their current social values.',
     '0. Very liberal 10. Very conservative'],
    ['V8_Scale', 'Q3.5', 'PeopleVsElite', 'PopulistRhetoricV8',
     'Q3.5 Parties can also be classified by their current use of POPULIST OR PLURALIST rhetoric.',
     '0. Strongly favors pluralist rhetoric 10. Strongly favors populist rhetoric'],
    ['V9', 'Q3.6', 'none', 'PopulistSaliencyV9',
     'Q3.6 And how IMPORTANT is populist rhetoric currently for each of the following parties?',
     '0. No importance 10. Great importance'],
    ['V10', 'Q4.1', 'none', 'ImmigrationV10',
     'Q4.1 Turning now to party positions on specific political issues, where do parties currently stand on IMMIGRATION?',
     '0. Strongly favors liberal immigration policies 10. Strongly favors restrictive immigration policies'],
    ['V19', 'Q5.2', 'none', 'PeopleShouldDecideV19',
     'Q5.2 How would you characterize the rhetoric commonly used by various parties on whether the people or leaders should decide important issues?',
     '0. Strongly emphasizes that ordinary people should decide important issues 10. Strongly emphasizes that leaders should decide important issues'],
);
# the roster's order (PartySystem.cs, UsaParties): key, the GPS id, the GPS party name, the C# stem
my @parties = (['REP', '1001', 'Republican Party', 'Rep'], ['DEM', '1000', 'Democratic Party', 'Dem']);

# ---------------------------------------------------------------- the codebook: each item's question and endpoints as quoted above
{
    held('usa_us32/gps2019_codebook_6feb2020.pdf');
    my $pdf = "$src/usa_us32/gps2019_codebook_6feb2020.pdf";
    open my $p, '-|', $pdftotext, '-raw', '-enc', 'UTF-8', $pdf, '-' or die "$pdftotext: $!\n";
    local $/;
    my $text = <$p>;
    close $p;
    die "$pdftotext gave no text for $pdf\n" unless defined $text && length $text > 1000;
    $text =~ s/\s+/ /g;
    for my $it (@items) {
        my ($v, $q, $field, $stem, $question, $ends) = @$it;
        my $at = index($text, $question);
        if ($at < 0) { problem("the codebook's text has no question for $v as quoted: '$question'"); next; }
        my $window = substr($text, $at, 1500);
        problem("the codebook's text has no endpoints for $v after its question as quoted: '$ends'") unless index($window, $ends) >= 0;
        problem("the codebook's text does not mark V19 as CHES PEOPLE_VS_ELITE after its question") if $v eq 'V19' && $window !~ /\[\+In CHES 2017 and 2019 PEOPLE_VS_ELITE\]/;
    }
}

# ---------------------------------------------------------------- the by-party file: the experts' mean per item, the file's own text
my %mean;   # "KEY|item" => the file's text
{
    my $t = held('usa_us32/gps2019_by_party_v2.tab');
    my @lines = split /\r?\n/, $t;
    my @h = split /\t/, shift @lines;
    my %c; @c{@h} = (0 .. $#h);
    for my $need ('ID_GPS', 'ISO', 'Partyname', map { $_->[0] } @items) { die "the by-party file has no column $need\n" unless exists $c{$need}; }
    my %seen;
    for my $row (@lines) {
        my @f = split /\t/, $row, -1;
        next unless ($f[$c{ISO}] // '') eq '"USA"';
        my ($party) = grep { $_->[1] eq $f[$c{ID_GPS}] } @parties;
        unless ($party) { problem("the by-party file holds a US row with GPS id $f[$c{ID_GPS}], not one of the roster's two"); next; }
        problem("the by-party file names GPS id $party->[1] '$f[$c{Partyname}]', not \"$party->[2]\"") unless $f[$c{Partyname}] eq "\"$party->[2]\"";
        $seen{$party->[0]}++;
        for my $it (@items) {
            my $cell = $f[$c{$it->[0]}] // '';
            if ($cell !~ /^\d+(?:\.\d+)?$/ || $cell > 10) { problem("$party->[0] $it->[0]: the by-party file's cell '$cell' is not a decimal on 0-10"); next; }
            $mean{"$party->[0]|$it->[0]"} = $cell;
        }
    }
    for my $p (@parties) { problem("the by-party file holds " . ($seen{$p->[0]} // 0) . " row(s) for $p->[0], not one") unless ($seen{$p->[0]} // 0) == 1; }
}

# the by-party CSV, a second publication of the same means
{
    my $t = held('usa_us32/gps2019_by_party_v2-2.csv');
    $t =~ s/^\xEF\xBB\xBF//;
    my @lines = split /\r?\n/, $t;
    my @h = parse_line(',', 0, shift @lines);
    my %c; @c{@h} = (0 .. $#h);
    my %seen;
    for my $row (@lines) {
        next unless $row =~ /^\d+,USA,/;
        my @f = parse_line(',', 0, $row);
        my ($party) = grep { $_->[1] eq $f[$c{ID_GPS}] } @parties or next;
        $seen{$party->[0]}++;
        for my $it (@items) {
            my $a = $mean{"$party->[0]|$it->[0]"} // next;
            my $b = $f[$c{$it->[0]} // die "the by-party CSV has no column $it->[0]\n"];
            problem("$party->[0] $it->[0]: the by-party CSV prints '$b', the table '$a'") unless defined $b && $b =~ /^\d+(?:\.\d+)?$/ && abs($a - $b) < 1e-12;
        }
    }
    for my $p (@parties) { problem("the by-party CSV holds " . ($seen{$p->[0]} // 0) . " row(s) for $p->[0], not one") unless ($seen{$p->[0]} // 0) == 1; }
}

# ---------------------------------------------------------------- the by-expert file: every placement, the spread, the means recomputed
my %place;   # "KEY|item" => [placements]
{
    my $t = held('usa_us32/gps2019_by_expert.tab');
    my @lines = split /\r?\n/, $t;
    my @h = split /\t/, shift @lines;
    my %c; @c{@h} = (0 .. $#h);
    my $experts = 0;
    for my $row (@lines) {
        my @f = split /\t/, $row, -1;
        next unless ($f[$c{ISO} // die "the by-expert file has no ISO column\n"] // '') eq '"USA"';
        $experts++;
        my %slot = (REP => 1, DEM => 2);
        problem("a US expert row names '$f[$c{CPARTY1}]' and '$f[$c{CPARTY2}]' as its first two parties, not the Republican and the Democratic")
            unless $f[$c{CPARTY1}] eq '"USA_ Republican Party"' && $f[$c{CPARTY2}] eq '"USA_ Democratic Party"';
        for my $p (@parties) {
            for my $it (@items) {
                my $col = "$it->[1]_$slot{$p->[0]}";
                die "the by-expert file has no column $col\n" unless exists $c{$col};
                my $v = $f[$c{$col}] // '';
                next if $v eq '' || $v eq '99';
                if ($v !~ /^\d+(?:\.\d+)?$/ || $v > 10) { problem("$p->[0] $it->[0]: a placement '$v' that is neither 0-10 nor 99"); next; }
                push @{$place{"$p->[0]|$it->[0]"}}, $v;
            }
        }
    }
    problem("the by-expert file holds no US expert") unless $experts;
}

sub quantile {   # linear interpolation between order statistics at position (n-1)p
    my ($sorted, $p) = @_;
    my $pos = (@$sorted - 1) * $p;
    my $lo = int $pos;
    my $hi = $lo + 1 < @$sorted ? $lo + 1 : $lo;
    return $sorted->[$lo] + ($sorted->[$hi] - $sorted->[$lo]) * ($pos - $lo);
}
sub round2 {   # half away from zero on the decimal digits as printed
    my $s = shift;
    my ($int, $frac) = $s =~ /^(\d+)(?:\.(\d*))?$/ or die "round2: '$s'\n";
    $frac = ($frac // '') . '000';
    my $cents = $int * 100 + substr($frac, 0, 2);
    $cents++ if substr($frac, 2, 1) >= 5;
    return sprintf('%d.%02d', int($cents / 100), $cents % 100);
}

my @rows;
for my $p (@parties) {
    for my $it (@items) {
        my $key = "$p->[0]|$it->[0]";
        my $m = $mean{$key} // next;
        my @v = sort { $a <=> $b } @{$place{$key} // []};
        if (!@v) { problem("$p->[0] $it->[0]: no expert placed the party"); next; }
        my $n = @v;
        my $sum = 0; $sum += $_ for @v;
        my $avg = $sum / $n;
        problem(sprintf("%s %s: the %d placements' mean %.15g is not the by-party file's %s", $p->[0], $it->[0], $n, $avg, $m)) unless abs($avg - $m) < 1e-12;
        my $ss = 0; $ss += ($_ - $avg) ** 2 for @v;
        my $sd = $n > 1 ? sqrt($ss / ($n - 1)) : 0;
        push @rows, [$p->[0], $p->[1], $p->[2], $it->[0], $it->[1], $it->[2], $n, $m, round2($m), sprintf('%.4f', $sd),
                     map { sprintf('%.2f', $_) } ($v[0], quantile(\@v, 0.25), quantile(\@v, 0.5), quantile(\@v, 0.75), $v[-1])];
    }
}

if (@problems) { print STDERR "MISMATCH: $_\n" for @problems; die scalar(@problems) . " mismatch(es) - nothing written\n"; }
$SIG{__WARN__} = 'DEFAULT';

# ---------------------------------------------------------------- the CSV
my $heading = 'party,gps_id,gps_party,item,question,roster_field,experts,mean,mean_2dp,sd,min,q1,median,q3,max';
my $csv = "# GENERATED by Tools/us_gps_prep.pl from the Global Party Survey 2019 (Norris; Harvard Dataverse doi:10.7910/DVN/WMGTNS) kept out of tree, held to ElectionsData/positions/raw/out_of_tree.txt - DO NOT EDIT; the record is ElectionsData/positions/party_positions.md.\n"
        . "# PS-6 US-32: a row per US party per item. mean: the by-party file's own text (file 3788852), every one recomputed from the by-expert file's placements (file 3679272) and held to the by-party CSV (file 3788854). mean_2dp: half away from zero on those digits - the figure the roster carries. experts: the placements (0-10; 99 and empty are none). sd: sample (n-1). q1/median/q3: linear between order statistics. roster_field: the PoliticalParty field the item fills (none: read and printed only).\n"
        . "# The codebook's items (Technical Note and Codebook 6 Feb 2020): V4_Scale Q3.1 economic left-right (0 extreme economic left - 10 extreme economic right); V6_Scale Q3.3 social values (0 very liberal - 10 very conservative); V8_Scale Q3.5 populist or pluralist rhetoric (0 strongly favors pluralist rhetoric - 10 strongly favors populist rhetoric); V9 Q3.6 how important populist rhetoric is (0 no importance - 10 great importance); V10 Q4.1 immigration (0 strongly favors liberal - 10 strongly favors restrictive immigration policies); V19 Q5.2 the people or leaders should decide (0 ordinary people should decide - 10 leaders should decide; the codebook marks it as CHES PEOPLE_VS_ELITE).\n"
        . "$heading\n";
$csv .= join(',', @$_) . "\n" for @rows;
die "a CSV cell holds a comma, quote or backslash\n" if grep { /[",\\]/ } map { @$_ } @rows;
my $csvDigest = sha256_hex($csv);

# ---------------------------------------------------------------- the C# catalog
my $cs = "// GENERATED by Tools/us_gps_prep.pl. DO NOT EDIT BY HAND.\n//\n"
       . "// Source: ElectionsData/positions/gps_2019_usa.csv (its SHA-256 below), written by the same run from the Global Party Survey 2019 files kept\n"
       . "// out of tree (RawSources, each held to ElectionsData/positions/raw/out_of_tree.txt). GeneratedCatalogCheck re-reads the CSV and compares\n"
       . "// every cell, re-hashes every file RawSources lists, holds every mean to the by-party file's own cell, and holds the roster to the constants.\n\n"
       . "namespace PoliSim.Data.Generated\n{\n"
       . "    /// <summary>PS-6 US-32: the Global Party Survey 2019's two US parties - SOURCED (Norris, Harvard Dataverse doi:10.7910/DVN/WMGTNS;\n"
       . "    /// `ElectionsData/positions/party_positions.md`). Each item's experts' mean at the two decimals the roster carries, and the experts' spread\n"
       . "    /// row by row. Generated, never hand-edited.</summary>\n"
       . "    public static class UsGps2019\n    {\n"
       . "        public const string SourceDigest = \"$csvDigest\";\n\n"
       . "        /// <summary>Every file the run read or relied on, by its path under `PoliSim-captures/sources/` (out of tree, beside the project), with its SHA-256.</summary>\n"
       . "        public static readonly (string Path, string Sha256)[] RawSources =\n        {\n";
$cs .= "            (\"$_\", \"$read{$_}\"),\n" for sort { $order{$a} <=> $order{$b} } keys %read;
$cs .= "        };\n\n        // each item's experts' mean, half away from zero to two decimals - the roster reads these (PartySystem.cs, UsaParties)\n";
for my $r (@rows) {
    my ($party) = grep { $_->[0] eq $r->[0] } @parties;
    my ($it) = grep { $_->[0] eq $r->[3] } @items;
    $cs .= "        public const float $party->[3]$it->[3] = $r->[8]f;\n";
}
$cs .= "\n        /// <summary>A row of the CSV, every cell as its text.</summary>\n"
     . "        public readonly struct Row\n        {\n"
     . "            public readonly string Party, GpsId, GpsParty, Item, Question, RosterField, Experts, Mean, Mean2dp, Sd, Min, Q1, Median, Q3, Max;\n"
     . "            public Row(string party, string gpsId, string gpsParty, string item, string question, string rosterField, string experts, string mean,\n"
     . "                string mean2dp, string sd, string min, string q1, string median, string q3, string max)\n"
     . "            {\n"
     . "                Party = party; GpsId = gpsId; GpsParty = gpsParty; Item = item; Question = question; RosterField = rosterField; Experts = experts;\n"
     . "                Mean = mean; Mean2dp = mean2dp; Sd = sd; Min = min; Q1 = q1; Median = median; Q3 = q3; Max = max;\n"
     . "            }\n        }\n\n"
     . "        public static readonly Row[] Rows =\n        {\n";
$cs .= '            new Row(' . join(', ', map { "\"$_\"" } @$_) . "),\n" for @rows;
$cs .= "        };\n    }\n}\n";
die "a non-ASCII byte in the outputs\n" if ($csv . $cs) =~ /[^\x0A\x20-\x7E]/;

# ---------------------------------------------------------------- write both
for my $o ([$csvOut, $csv], [$csOut, $cs]) {
    open my $w, '>:raw', $o->[0] or die "$o->[0]: $!\n";
    print $w $o->[1] or die "$o->[0]: $!\n";
    close $w or die "$o->[0]: $!\n";
}
printf "us_gps_prep: %d file(s) held to %s; %d row(s) (%s); every mean the by-party file's, recomputed from the placements and held to the CSV upload\n",
    scalar(keys %read), $list, scalar(@rows), join(' ', map { "$_->[0] $_->[3] $_->[8] (n $_->[6], $_->[10]-$_->[14])" } @rows);
print "wrote $csvOut (SHA-256 $csvDigest) and $csOut\n";
