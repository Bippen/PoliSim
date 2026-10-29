#!/usr/bin/perl
# k276_participation_prep.pl [--dry]   (run from the project root)
#
# K276 (docs/RULINGS_2026-10-23.md, Elias 2026-10-29, answering K245): labour-force participation rises by cohort and with statutory pension
# ages, sourced from the EC Ageing Report 2024 (the EU five) and CBO's long-term projections (the USA). This tool DERIVES every figure the model
# and its diagnostic read from files already saved byte-exact under ElectionsData/macro/raw/q29_longrun/ (each held against that folder's
# SHA256SUMS.txt, which it checks) and writes
#   Assets/Scripts/Data/Generated/ParticipationPath.cs
# never typing a figure. What it reads, and how:
#   - The EU five: the 2024 Ageing Report's statistical annex, cross-country tables (sheet "cross-country_tables"), the projected participation
#     rate by age group, both sexes, 2022-2070, one figure a year: table II.1.29 young (20-24y), II.1.30 prime-age (25-54y), II.1.31 older
#     (55-64y), II.1.32 oldest (65-74y). The ruling names "II.1.15": in this annex II.1.15 is employment 15-74 (growth rate), the series K210
#     already reads as the labour part; the participation rates by cohort are II.1.29-II.1.32, the successor the ruling allows. Beside them,
#     table II.1.26 (labour force 20-64y, thousands), printed by the diagnostic as information; and (review 1, M2) tables II.1.53 (employment
#     20-74y, millions) and II.1.51 (unemployment rate 20-74y), from which the diagnostic derives the Ageing Report's labour force aged 20-74
#     (employment / (1 - the rate / 100)), the perimeter its labour-force hold reads the model's own labour force on.
#   - The USA: CBO's March 2025 Long-Term Budget Outlook economic file (57054-2025-03-LTBO-econ.xlsx, as the Internet Archive kept it - cbo.gov
#     refuses, K210's fetch log), sheet "2. Actual Labor Force Participation Rates, by Age Group and Sex", 2025-2099: males and females, each in
#     fifteen age groups (16-17, 18-19, 20-24, five-year groups to 55-59, 60-61, 62-64, 65-69, 70-79, 80-89). CBO publishes no rate by age for
#     both sexes together and no 2024 row; the model combines the sexes in ParticipationPath's reader (equal weights, DECLARED there) and reads
#     the path's change from 2025, the file's first year.
#   - (Review 1, M1) the pension age the Ageing Report projects the two indexed statutes under, from ElectionsData/macro/raw/k276_pension_age/
#     (held against that folder's SHA256SUMS.txt), read through `pdftotext -raw` (Git Bash's mingw64 build): the 2024 country fiches' Table 1,
#     "Qualifying conditions for retirement" - Sweden (page 7) the row "Earliest age for a guaranteed pension - men and women", 2023-2070 (the age
#     the riktalder sets, the game's Swedish statute); Italy (page 8) the two "Statutory retirement age" rows, men and women, which must agree,
#     2022-2070 ("67y+3m" read as 67 + 3/12). Each is cross-checked against the report's own Table I.1.3 (Institutional Paper 279, page 33,
#     rows SE* and IT*, 2030, 2050 and 2070, male and female alike) within 0.05 years - the table prints one decimal.
# Each table's rows run from its year header to the next table's title; a country carried twice, a group header not matched exactly once, a year
# absent or a figure outside 0..100 stops the script (q29's H-1 / L7 discipline). The prime-age row of each country is held between the young
# and the older rows' extremes in 2024 as a sanity bound on the table read. ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw(unzip $UnzipError);

my $dry = @ARGV && $ARGV[0] eq '--dry';
die "run from the project root\n" unless -d 'Assets/Scripts/Data/Generated';
my $raw = 'ElectionsData/macro/raw/q29_longrun';
my $out = 'Assets/Scripts/Data/Generated/ParticipationPath.cs';
my @EU = (['SE', 'Sweden'], ['DE', 'Germany'], ['FR', 'France'], ['IT', 'Italy'], ['PL', 'Poland']);
my ($EU_FIRST, $EU_LAST, $US_FIRST, $US_LAST) = (2022, 2070, 2025, 2099);

sub slurp { my ($p) = @_; open my $fh, '<:raw', $p or die "$p: $!\n"; local $/; my $s = <$fh>; close $fh; return $s; }
sub must { my ($v, $what) = @_; die "k276_participation_prep: $what not found\n" unless defined $v; return $v; }

my %sums;
for (split /\n/, slurp("$raw/SHA256SUMS.txt")) { s/\r$//; $sums{$2} = $1 if /^([0-9a-f]{64}) \*?(\S+)$/; }
my %F = (
    ar => 'ec_2024_Ageing_Report-Statistical_annex_all_horizontal_tables.xlsx',
    cbo => 'wayback_20250404235041_cbo_57054-2025-03-LTBO-econ.xlsx',
);
my %digest;
for my $k (sort keys %F) {
    my $f = $F{$k};
    $digest{$k} = sha256_hex(slurp("$raw/$f"));
    die "$f: its SHA-256 $digest{$k} is not the one SHA256SUMS.txt lists\n" unless ($sums{$f} // '') eq $digest{$k};
}

# ---- xlsx: shared strings and one sheet's cells (q29_longrun_prep.pl's reader; whitespace inside a shared string collapsed to one space)
sub member { my ($zip, $name) = @_; my $o; unzip($zip => \$o, Name => $name) or die "$zip $name: $UnzipError\n"; return $o; }
sub sheet {
    my ($zip, $n) = @_;
    my $sst = member($zip, 'xl/sharedStrings.xml');
    my @ss = map { my $t = $_; $t =~ s/<[^>]+>//g; $t =~ s/\s+/ /g; $t =~ s/^ | $//g; $t } ($sst =~ m{<si>(.*?)</si>}gs);
    my $x = member($zip, "xl/worksheets/sheet$n.xml");
    my %cell;
    while ($x =~ m{<row r="(\d+)"[^>]*>(.*?)</row>}gs) {
        my ($r, $body) = ($1, $2);
        while ($body =~ m{<c r="([A-Z]+)\d+"([^>]*?)(?:/>|>(.*?)</c>)}gs) {
            my ($col, $attr, $in) = ($1, $2, $3 // '');
            next unless $in =~ m{<v>(.*?)</v>}s;
            my $v = $1;
            $v = $ss[$v] if $attr =~ /t="s"/;
            $cell{$r}{$col} = $v;
        }
    }
    return \%cell;
}

# ---- the Ageing Report's cross-country sheet (sheet 4, as q29 reads it)
my $ar = sheet("$raw/$F{ar}", 4);
my $lastRow = (sort { $b <=> $a } keys %$ar)[0];
sub arTable {
    my ($title, $lo, $hi) = @_;
    my @rt = grep { ($ar->{$_}{B} // '') =~ /^\Q$title\E/ } keys %$ar;
    die "Ageing Report: '$title' matches " . scalar(@rt) . " titles, not one\n" unless @rt == 1;
    my $hr = $rt[0] + 2;
    my %yearCol;
    for my $col (keys %{ $ar->{$hr} }) { $yearCol{ $ar->{$hr}{$col} } = $col if $ar->{$hr}{$col} =~ /^20\d\d$/; }
    die "Ageing Report: $title years\n" unless $yearCol{$EU_FIRST} && $yearCol{$EU_LAST};
    my %out;
    for my $r ($hr + 1 .. $lastRow) {
        last if index($ar->{$r}{B} // '', 'Table ') == 0;
        my $code = $ar->{$r}{B} // next;
        my ($row) = grep { $_->[0] eq $code } @EU;
        next unless $row;
        die "Ageing Report: $title carries $code twice\n" if exists $out{ $row->[1] };
        $out{ $row->[1] } = [ map { my $v = must($ar->{$r}{ must($yearCol{$_}, "$title year $_") }, "$title $code $_"); die "$title $code $_ reads $v, outside $lo..$hi\n" if $v < $lo || $v > $hi; $v } $EU_FIRST .. $EU_LAST ];
    }
    die "Ageing Report: $title has " . scalar(keys %out) . " of the five\n" unless keys %out == 5;
    return \%out;
}
my %group = (
    Young => arTable('Table II.1.29: Participation rate young (20-24y)', 0, 100),
    Prime => arTable('Table II.1.30: Participation rate prime-age (25-54y)', 0, 100),
    Older => arTable('Table II.1.31: Participation rate older (55-64y)', 0, 100),
    Oldest => arTable('Table II.1.32: Participation rate oldest (65-74y)', 0, 100),
);
my $lf2064 = arTable('Table II.1.26: Labour force (20-64y; thousands)', 100, 1e6);
my $emp2074 = arTable('Table II.1.53: Employment (20-74y; millions)', 0.1, 1000);
my $une2074 = arTable('Table II.1.51: Unemployment rate (20-74y)', 0, 100);

# ---- review 1, M1: the pension age the Ageing Report projects Sweden and Italy under (the country fiches' Table 1; the report's Table I.1.3)
my $pa = 'ElectionsData/macro/raw/k276_pension_age';
my %paSums;
for (split /\n/, slurp("$pa/SHA256SUMS.txt")) { s/\r$//; $paSums{$2} = $1 if /^([0-9a-f]{64}) \*?(\S+)$/; }
my %PA = (
    se => 'ec_2024_Ageing_Report_country_fiche_Sweden.pdf',
    it => 'ec_2024_Ageing_Report_country_fiche_Italy.pdf',
    ip => 'ec_2024_Ageing_Report_ip279_en.pdf',
);
for my $k (sort keys %PA) {
    my $f = $PA{$k};
    $digest{"pa_$k"} = sha256_hex(slurp("$pa/$f"));
    die "$f: its SHA-256 " . $digest{"pa_$k"} . " is not the one $pa/SHA256SUMS.txt lists\n" unless ($paSums{$f} // '') eq $digest{"pa_$k"};
}
sub pdfPage {
    my ($f, $page) = @_;
    open my $p, '-|', 'pdftotext', '-f', $page, '-l', $page, '-raw', "$pa/$f", '-' or die "pdftotext $f: $!\n";
    binmode $p; local $/; my $t = <$p>; close $p or die "pdftotext $f page $page failed (is Git Bash's mingw64 pdftotext on the PATH?)\n";
    $t =~ s/\r//g;
    return [ split /\n/, $t ];
}
sub ym { my ($s) = @_; $s =~ /^(\d+)y(?:\+(\d+)m)?$/ or die "pension age '$s' is not NNy or NNy+Nm\n"; return $1 + ($2 // 0) / 12; }
my %paYears; my %paAges;
{
    my $l = pdfPage($PA{se}, 7);
    my @t = grep { $l->[$_] eq 'TABLE 1 - QUALIFYING CONDITIONS FOR RETIREMENT' } 0 .. $#$l;
    die "Sweden fiche page 7: Table 1's title found " . scalar(@t) . " times, not once\n" unless @t == 1;
    my @y = split / /, must(($l->[ $t[0] + 1 ] =~ /^((?:20\d\d ){5}20\d\d)$/)[0], 'Sweden fiche Table 1 year row');
    my @g = grep { /^Earliest age for a guaranteed pension - men and women ((?:\d\d ){5}\d\d)$/ } @$l;
    die "Sweden fiche: the guaranteed-pension row found " . scalar(@g) . " times, not once\n" unless @g == 1;
    my ($v) = $g[0] =~ /((?:\d\d ){5}\d\d)$/;
    ($paYears{Sweden}, $paAges{Sweden}) = ([@y], [split / /, $v]);
}
{
    my $l = pdfPage($PA{it}, 8);
    my @t = grep { $l->[$_] eq 'TABLE 1 - QUALIFYING CONDITIONS FOR RETIREMENT' } 0 .. $#$l;
    die "Italy fiche page 8: Table 1's title found " . scalar(@t) . " times, not once\n" unless @t == 1;
    die "Italy fiche page 8: the 'Statutory retirement age' labels are not there\n" unless (grep { /^Statutory retirement age - men/ } @$l) == 1 && (grep { /^Statutory retirement age - w ?omen/ } @$l) == 1;
    my @yr = grep { $_ > $t[0] && $l->[$_] =~ /^(?:20\d\d ){5}20\d\d$/ } 0 .. $#$l;
    die "Italy fiche: Table 1's year row\n" unless @yr;
    my $r = $yr[0];
    my ($men, $women) = ($l->[ $r + 1 ], $l->[ $r + 2 ]);
    my $cell = qr/\d+y(?:\+\d+m)?/;
    die "Italy fiche: the two rows after the year row are not six ages each ('$men' / '$women')\n" unless $men =~ /^$cell(?: $cell){5}$/ && $women =~ /^$cell(?: $cell){5}$/;
    die "Italy fiche: the men's and women's statutory ages differ ('$men' / '$women')\n" unless $men eq $women;
    ($paYears{Italy}, $paAges{Italy}) = ([split / /, $l->[$r]], [map { ym($_) } split / /, $men]);
}
{
    my $l = pdfPage($PA{ip}, 33);
    die "the report page 33: Table I.1.3's title is not there\n" unless (grep { /^Table I\.1\.3: Statutory retirement ages, early retirement ages and incentives/ } @$l) == 1;
    die "the report page 33: the year row is not 2022 2030 2050 2070 twice\n" unless (grep { /^2022 2030 2050 2070 2022 2030 2050 2070\b/ } @$l) == 1;
    for (['SE', 'Sweden'], ['IT', 'Italy']) {
        my ($code, $n) = @$_;
        my @r = grep { /^\Q$code\E\* / } @$l;
        die "Table I.1.3: the $code* row found " . scalar(@r) . " times, not once\n" unless @r == 1;
        my @a = $r[0] =~ /([\d.]+) \([\d.]+\)/g;
        die "Table I.1.3 $code*: " . scalar(@a) . " ages, not eight\n" unless @a == 8;
        die "Table I.1.3 $code*: the male and female ages differ\n" unless "@a[0..3]" eq "@a[4..7]";
        my %t = (2030 => $a[1], 2050 => $a[2], 2070 => $a[3]);
        for my $y (sort keys %t) {
            my ($i) = grep { $paYears{$n}[$_] == $y } 0 .. $#{ $paYears{$n} };
            die "$n: the fiche has no $y column\n" unless defined $i;
            die sprintf("%s %d: the fiche's %.3f and Table I.1.3's %s differ by more than 0.05 years\n", $n, $y, $paAges{$n}[$i], $t{$y}) if abs($paAges{$n}[$i] - $t{$y}) > 0.05 + 1e-9;
        }
    }
}
for my $n ('Sweden', 'Italy') { printf "%-8s the pension age the Ageing Report projects under: %s\n", $n, join(' ', map { sprintf('%d %.3f', $paYears{$n}[$_], $paAges{$n}[$_]) } 0 .. $#{ $paYears{$n} }); }
for my $n (map { $_->[1] } @EU) {
    my $i = 2024 - $EU_FIRST;
    my ($y, $p, $o) = ($group{Young}{$n}[$i], $group{Prime}{$n}[$i], $group{Older}{$n}[$i]);
    die "Ageing Report: ${n}'s 2024 prime-age rate $p is not above both the young ($y) and the older ($o) rates\n" unless $p > $y && $p > $o;
}

sub colIndex { my ($s) = @_; my $n = 0; $n = $n * 26 + ord($_) - 64 for split //, $s; return $n; }
# ---- CBO: sheet 3, the rates by age group and sex
my (@usCols, %us, @usAll);
{
    my $c = sheet("$raw/$F{cbo}", 3);
    my @rh = grep { ($c->{$_}{A} // '') eq 'Year' } keys %$c;
    die "CBO: the header row matches " . scalar(@rh) . " rows, not one\n" unless @rh == 1;
    my $rh = $rh[0];
    my @rs = grep { ($c->{$_}{D} // '') eq 'Males' && ($c->{$_}{U} // '') eq 'Females' } keys %$c;
    die "CBO: the sex row (Males in D, Females in U) matches " . scalar(@rs) . " rows, not one\n" unless @rs == 1;
    # the fifteen groups, in the file's own order on both sides: males E..R+S, females V..AJ
    my @want = ('16 to 17', '18 to 19', '20 to 24', '25 to 29', '30 to 34', '35 to 39', '40 to 44', '45 to 49', '50 to 54', '55 to 59', '60 to 61', '62 to 64', '65 to 69', '70 to 79', '80 to 89');
    my %colOf;
    for my $col (keys %{ $c->{$rh} }) { my $h = $c->{$rh}{$col}; $colOf{$1}{$col} = 1 if $h =~ /^Ages (\d+ to \d+)$/; }
    my %rowOf;
    for my $r (keys %$c) { $rowOf{ $c->{$r}{A} } = $r if ($c->{$r}{A} // '') =~ /^\d{4}$/ && $r > $rh; }
    for my $g (@want) {
        my @cols = sort { colIndex($a) <=> colIndex($b) } keys %{ $colOf{$g} // {} };
        die "CBO: 'Ages $g' heads " . scalar(@cols) . " columns, not two (males, females)\n" unless @cols == 2;
        die "CBO: 'Ages $g' - the male column $cols[0] is not left of U\n" unless colIndex($cols[0]) < colIndex('U') && colIndex($cols[1]) > colIndex('U');
        my ($lo, $hi) = $g =~ /^(\d+) to (\d+)$/;
        push @usCols, [$lo, $hi];
        for my $s (0, 1) {
            push @{ $us{ $s ? 'F' : 'M' } }, [ map { my $v = must($c->{ must($rowOf{$_}, "CBO year $_") }{ $cols[$s] }, "CBO $g $_"); die "CBO $g $_ reads $v\n" if $v < 0 || $v > 100; $v } $US_FIRST .. $US_LAST ];
        }
    }
    my @all = grep { ($c->{$rh}{$_} // '') eq 'All people age 16 or older' } keys %{ $c->{$rh} };
    die "CBO: the all-people column\n" unless @all == 1;
    # review 1 (M3): the all-people rate, printed by the diagnostic beside the USA's labour-force hold as an attribution only (no model reader)
    @usAll = map { my $v = must($c->{ must($rowOf{$_}, "CBO year $_") }{ $all[0] }, "CBO all people $_"); die "CBO all people $_ reads $v\n" if $v < 0 || $v > 100; $v } $US_FIRST .. $US_LAST;
}

# ---- the summary: each group's change from its base year at the years the diagnostic holds
my @order = ('Sweden', 'Germany', 'France', 'Italy', 'Poland');
for my $n (@order) {
    my $b = 2024 - $EU_FIRST;
    printf "%-8s", $n;
    for my $g (qw(Young Prime Older Oldest)) {
        printf "  %s %.1f", $g, $group{$g}{$n}[$b];
        printf " %+.1f@%d", $group{$g}{$n}[$_ - $EU_FIRST] - $group{$g}{$n}[$b], $_ for (2031, 2050, 2070);
    }
    print "\n";
}
{
    my $worst = 0; my $at = '';
    for my $k (0 .. $#usCols) {
        my $dm = $us{M}[$k][-1] - $us{M}[$k][0]; my $df = $us{F}[$k][-1] - $us{F}[$k][0];
        my $gap = abs($dm - $df);
        ($worst, $at) = ($gap, "$usCols[$k][0]-$usCols[$k][1]") if $gap > $worst;
        printf "USA %2d-%2d  males %.1f -> %.1f  females %.1f -> %.1f  (2025 -> %d)\n", @{ $usCols[$k] }, $us{M}[$k][0], $us{M}[$k][-1], $us{F}[$k][0], $us{F}[$k][-1], $US_LAST;
    }
    printf "USA: the widest gap between the sexes' changes 2025-%d is %.2f pts (%s); equal weights miss a population-weighted mean by that gap x the weight's distance from one half\n", $US_LAST, $worst, $at;
}
exit 0 if $dry;

# ---- the C#
sub f { my ($v) = @_; my $s = sprintf('%.7g', $v); $s .= '.0' unless $s =~ /[.eE]/; return $s . 'f'; }
sub arr { return 'new float[] { ' . join(', ', map { f($_) } @_) . ' }'; }
my $cs = '';
$cs .= "// GENERATED by Tools/k276_participation_prep.pl. DO NOT EDIT BY HAND.\n//\n";
$cs .= "// K276: the sourced participation paths by age group - the EU five the EC Ageing Report 2024 (statistical annex, cross-country tables\n";
$cs .= "// II.1.29-II.1.32 and II.1.26), the USA CBO's March 2025 Long-Term Budget Outlook (rates by age group and sex). Every figure below is read\n";
$cs .= "// from ElectionsData/macro/raw/q29_longrun/ by that tool, whose header names each file, table and transformation.\n";
$cs .= "// Review 1 (M1, M2): tables II.1.53 and II.1.51, and the pension age the Ageing Report projects Sweden and Italy under (the 2024 country\n";
$cs .= "// fiches' Table 1, cross-checked against the report's Table I.1.3), from ElectionsData/macro/raw/k276_pension_age/.\n//\n";
for my $k (sort keys %F) { $cs .= "// SHA-256 ($F{$k}): $digest{$k}\n"; }
for my $k (sort keys %PA) { $cs .= "// SHA-256 ($PA{$k}): " . $digest{"pa_$k"} . "\n"; }
$cs .= "\nusing System.Collections.Generic;\n\nnamespace PoliSim.Data.Generated\n{\n    public static class ParticipationPath\n    {\n";
$cs .= "        /// <summary>The Ageing Report's years: one figure a year from the first to the last, both included.</summary>\n";
$cs .= "        public const int EuFirstYear = $EU_FIRST, EuLastYear = $EU_LAST;\n";
$cs .= "        /// <summary>CBO's years: one figure a year from the first to the last, both included.</summary>\n";
$cs .= "        public const int UsFirstYear = $US_FIRST, UsLastYear = $US_LAST;\n\n";
my %what = (
    Young => 'table II.1.29, participation rate young (20-24y), per cent',
    Prime => 'table II.1.30, participation rate prime-age (25-54y), per cent',
    Older => 'table II.1.31, participation rate older (55-64y), per cent',
    Oldest => 'table II.1.32, participation rate oldest (65-74y), per cent',
);
for my $g (qw(Young Prime Older Oldest)) {
    $cs .= "        /// <summary>The Ageing Report 2024, $what{$g}, both sexes, $EU_FIRST-$EU_LAST.</summary>\n";
    $cs .= "        public static readonly Dictionary<CountryId, float[]> $g = new Dictionary<CountryId, float[]>\n        {\n";
    $cs .= "            { CountryId.$_, " . arr(@{ $group{$g}{$_} }) . " },\n" for @order;
    $cs .= "        };\n";
}
$cs .= "        /// <summary>The Ageing Report 2024, table II.1.26, labour force (20-64y), thousands, $EU_FIRST-$EU_LAST - printed by the diagnostic as information.</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float[]> LabourForce2064 = new Dictionary<CountryId, float[]>\n        {\n";
$cs .= "            { CountryId.$_, " . arr(@{ $lf2064->{$_} }) . " },\n" for @order;
$cs .= "        };\n";
$cs .= "        /// <summary>Review 1 (M2): the Ageing Report 2024, table II.1.53, employment (20-74y), millions, $EU_FIRST-$EU_LAST - with II.1.51, the labour force aged 20-74 the diagnostic's labour-force hold reads.</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float[]> Employment2074 = new Dictionary<CountryId, float[]>\n        {\n";
$cs .= "            { CountryId.$_, " . arr(@{ $emp2074->{$_} }) . " },\n" for @order;
$cs .= "        };\n";
$cs .= "        /// <summary>Review 1 (M2): the Ageing Report 2024, table II.1.51, unemployment rate (20-74y), per cent, $EU_FIRST-$EU_LAST.</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float[]> Unemployment2074 = new Dictionary<CountryId, float[]>\n        {\n";
$cs .= "            { CountryId.$_, " . arr(@{ $une2074->{$_} }) . " },\n" for @order;
$cs .= "        };\n\n";
$cs .= "        /// <summary>Review 1 (M1): the years of the pension age the Ageing Report 2024 projects the two indexed statutes under (the country fiches' Table 1).</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, int[]> ProjectedPensionAgeYears = new Dictionary<CountryId, int[]>\n        {\n";
$cs .= "            { CountryId.$_, new[] { " . join(', ', @{ $paYears{$_} }) . " } },\n" for ('Sweden', 'Italy');
$cs .= "        };\n";
$cs .= "        /// <summary>Review 1 (M1): that pension age, years (a month a twelfth) - Sweden the fiche's \"earliest age for a guaranteed pension\" (the age the riktalder sets), Italy its statutory retirement age, men and women alike.</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float[]> ProjectedPensionAge = new Dictionary<CountryId, float[]>\n        {\n";
$cs .= "            { CountryId.$_, " . arr(@{ $paAges{$_} }) . " },\n" for ('Sweden', 'Italy');
$cs .= "        };\n\n";
$cs .= "        /// <summary>Review 1 (M3): CBO, LTBO March 2025, the actual labor force participation rate of all people aged 16 or older, per cent, [year - UsFirstYear] - an attribution beside the diagnostic's labour-force hold, read by no model code.</summary>\n";
$cs .= "        public static readonly float[] UsAll16Plus = " . arr(@usAll) . ";\n";
$cs .= "        /// <summary>CBO's age groups, in the file's order: the first and last age of each, both included.</summary>\n";
$cs .= "        public static readonly int[][] UsGroups = { " . join(', ', map { "new[] { $_->[0], $_->[1] }" } @usCols) . " };\n";
for my $s (['M', 'UsMale', 'males'], ['F', 'UsFemale', 'females']) {
    $cs .= "        /// <summary>CBO, LTBO March 2025, actual labor force participation rates of $s->[2], per cent: [group][year - UsFirstYear].</summary>\n";
    $cs .= "        public static readonly float[][] $s->[1] =\n        {\n";
    $cs .= "            " . arr(@$_) . ",\n" for @{ $us{ $s->[0] } };
    $cs .= "        };\n";
}
$cs .= "    }\n}\n";
$cs =~ s/\n/\r\n/g;
open my $o, '>:raw', $out or die "$out: $!\n"; print $o $cs; close $o;
print "wrote $out\n";
