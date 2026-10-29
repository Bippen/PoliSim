#!/usr/bin/perl
# uspop_prep.pl [--dry | --check]   (run from the project root)
#
# K299 (K290's follow-up, docs/RULINGS_2026-10-23.md): the US population path on CBO's 2025 demographics. This tool DERIVES every figure the model
# and its diagnostics read from files saved byte-exact under ElectionsData/usa/raw/uspop_cbo_demographics/ (each held against that folder's
# SHA256SUMS.txt, which it checks) and writes
#   Assets/Scripts/Data/Generated/UsDemographicOutlook.cs
# never typing a figure. --dry prints the summary and writes nothing; --check writes nothing and says whether the file on disk is what this tool
# writes (exit 1 where it is not). What it reads, and how:
#   - CBO, "The Demographic Outlook: 2025 to 2055" (January 2025, publication 60875), the data underlying its figures (60875-Data.xlsx, reposted by
#     CBO on 2 May 2025 - the file's own Contents sheet says the repost changed Figures 9, 10 and 15 only), as the Internet Archive kept cbo.gov's
#     own file (cbo.gov answers a JavaScript challenge; the fetch log beside the file):
#       Figure 2, "Population Size, by Age Group", millions, the Social Security area population: 24 or younger, 25 to 54, 55 to 64, 65 or older and
#       the total, 1975-2055. Each year's four groups must sum to its total within the file's own rounding (0.0025 million), and the total must
#       equal CBO's March 2025 Long-Term Budget Outlook's population row ("Population (millions of people)", sheet "1. Econ Vars_Annual Rates",
#       the same projection, from ElectionsData/macro/raw/q29_longrun/, held against that folder's SHA256SUMS.txt) to the file's 0.001 million at
#       every year both carry - the two publications are one projection, and a misread sheet or a shifted column cannot pass both.
#       Figure 10, "BLS Through 2024 Plus CBO Projection of the Civilian Noninstitutionalized Population, by Age Group", millions: 16 to 24, 25 to 54,
#       55 to 64, 65 or older and the total (age 16 or older), 1975-2055 - the population CBO's labor force participation rate applies to. It must
#       equal the LTBO's own actual labor force over its participation rate (the same file's rows "Labor force growth" and "Labor force
#       participation rate") within 0.05 % of the index at every year 2026-2055 - CBO's two files, one population.
#   - (K299's prereg, amendment r3) CBO's "Demographic Projections" for the same report (57059-2025-01-Demographic-Projections.zip, reposted by CBO
#     on 2 May 2025; its README: "This data supplements CBO's January 2025 report The Demographic Outlook: 2025 to 2055"), as the Internet Archive
#     kept cbo.gov's own file: the member "CSV files/ssArea_pop_byYearAgeMaritalSex.csv" (year, age 0..99 and "100+", marital status, sex, number of
#     people), read in memory from the zip. Both sexes and every marital status are summed and the single years folded into the model's 21 bands for
#     2024-2098 (the model's catalog opens on 2024), with the 15+ and 16+ from the single years. Every year must carry all 101 ages x 4 statuses x 2
#     sexes once; through 2055 each year's total and its four Figure 2 groups must equal Figure 2's to the figure file's 0.001 million.
# Each sheet's header row must name its columns exactly as listed; a year absent, carried twice, or a figure outside its bounds stops the script
# (q29's H-1 / L7 discipline). The generated file carries CBO's figures as published and the table's bands as summed; what the model does with
# them (CohortDemographics.Projection: the bands are the USA's path, carried past the table's last year) is reviewed with the code. ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw(unzip $UnzipError);

my $mode = $ARGV[0] // '';
die "usage: uspop_prep.pl [--dry | --check]\n" unless $mode eq '' || $mode eq '--dry' || $mode eq '--check';
die "run from the project root\n" unless -d 'Assets/Scripts/Data/Generated';
my $raw = 'ElectionsData/usa/raw/uspop_cbo_demographics';
my $q29 = 'ElectionsData/macro/raw/q29_longrun';
my $out = 'Assets/Scripts/Data/Generated/UsDemographicOutlook.cs';
my ($FIRST, $LAST) = (1975, 2055);

sub slurp { my ($p) = @_; open my $fh, '<:raw', $p or die "$p: $!\n"; local $/; my $s = <$fh>; close $fh; return $s; }
sub must { my ($v, $what) = @_; die "uspop_prep: $what not found\n" unless defined $v; return $v; }
sub sums { my ($dir) = @_; my %s; for (split /\n/, slurp("$dir/SHA256SUMS.txt")) { s/\r$//; $s{$2} = $1 if /^([0-9a-f]{64}) \*?(\S+)$/; } return \%s; }

my %F = (
    cbo => [$raw, 'wayback_20250811192155_cbo_60875-Data.xlsx'],
    age => [$raw, 'wayback_20250508101602_cbo_57059-2025-01-Demographic-Projections.zip'],
    ltbo => [$q29, 'wayback_20250404235041_cbo_57054-2025-03-LTBO-econ.xlsx'],
);
my (%digest, %sumsOf);
for my $k (sort keys %F) {
    my ($dir, $f) = @{ $F{$k} };
    $sumsOf{$dir} //= sums($dir);
    $digest{$k} = sha256_hex(slurp("$dir/$f"));
    die "$dir/$f: its SHA-256 $digest{$k} is not the one $dir/SHA256SUMS.txt lists\n" unless ($sumsOf{$dir}{$f} // '') eq $digest{$k};
}

# ---- xlsx: shared strings, the sheet named, its cells (k276_participation_prep.pl's reader; whitespace inside a shared string collapsed to one space)
sub member { my ($zip, $name) = @_; my $o; unzip($zip => \$o, Name => $name) or die "$zip $name: $UnzipError\n"; return $o; }
sub sheetNamed {
    my ($zip, $name) = @_;
    my $wb = member($zip, 'xl/workbook.xml');
    my @hit = $wb =~ m{<sheet [^>]*name="\Q$name\E"[^>]*r:id="([^"]+)"}g;
    die "$zip: the sheet '$name' is named " . scalar(@hit) . " times, not once\n" unless @hit == 1;
    my $rels = member($zip, 'xl/_rels/workbook.xml.rels');
    my ($target) = $rels =~ m{<Relationship [^>]*Id="\Q$hit[0]\E"[^>]*Target="([^"]+)"};
    ($target) = $rels =~ m{<Relationship [^>]*Target="([^"]+)"[^>]*Id="\Q$hit[0]\E"} unless defined $target;
    die "$zip: the sheet '$name' has no target\n" unless defined $target;
    $target =~ s{^/?xl/}{}; $target =~ s{^/}{};
    my $sst = member($zip, 'xl/sharedStrings.xml');
    my @ss = map { my $t = $_; $t =~ s/<[^>]+>//g; $t =~ s/&amp;/&/g; $t =~ s/&#8217;|\x{2019}/'/g; $t =~ s/\s+/ /g; $t =~ s/^ | $//g; $t } ($sst =~ m{<si>(.*?)</si>}gs);
    my $x = member($zip, "xl/$target");
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

# A figure's table: the header row naming exactly @cols in B.., then one row a year in A from $FIRST to $LAST, each figure inside [$lo, $hi].
sub figure {
    my ($zip, $sheet, $title, $cols, $lo, $hi) = @_;
    my $c = sheetNamed($zip, $sheet);
    my @t = grep { index($c->{$_}{A} // '', $title) == 0 } keys %$c;
    die "$sheet: the title '$title' heads " . scalar(@t) . " rows, not one\n" unless @t == 1;
    my @letters = map { chr(ord('B') + $_) } 0 .. $#$cols;
    my @h = grep { my $r = $_; $r > $t[0] && !grep { ($c->{$r}{ $letters[$_] } // '') ne $cols->[$_] } 0 .. $#$cols } keys %$c;
    die "$sheet: the header row (" . join(' | ', @$cols) . ") matches " . scalar(@h) . " rows, not one\n" unless @h == 1;
    my %byYear;
    for my $r (keys %$c) {
        next unless $r > $h[0] && ($c->{$r}{A} // '') =~ /^(\d{4})$/;
        my $y = $1;
        next if $y < $FIRST || $y > $LAST;
        die "$sheet: $y carried twice\n" if $byYear{$y};
        $byYear{$y} = [ map { my $v = must($c->{$r}{ $letters[$_] }, "$sheet $y $cols->[$_]"); die "$sheet $y $cols->[$_] reads $v, outside $lo..$hi\n" if $v !~ /^[-\d.Ee+]+$/ || $v < $lo || $v > $hi; $v + 0 } 0 .. $#$cols ];
    }
    for my $y ($FIRST .. $LAST) { die "$sheet: no $y row\n" unless $byYear{$y}; }
    return \%byYear;
}

my $cboZip = "$F{cbo}[0]/$F{cbo}[1]";
my $ss = figure($cboZip, 'Figure 2', 'Figure 2. Population Size, by Age Group', ['24 or younger', '25 to 54', '55 to 64', '65 or older', 'Total'], 0.5, 1000);
my $cnp = figure($cboZip, 'Figure 10', 'Figure 10. BLS Through 2024 Plus CBO Projection of the Civilian Noninstitutionalized Population, by Age Group', ['16 to 24', '25 to 54', '55 to 64', '65 or older', 'Total'], 0.5, 1000);
for my $y ($FIRST .. $LAST) {
    for ([$ss, 'Figure 2'], [$cnp, 'Figure 10']) {
        my ($t, $n) = @$_;
        my $sum = 0; $sum += $t->{$y}[$_] for 0 .. 3;
        die sprintf("%s %d: the four groups sum to %.3f against the total %.3f\n", $n, $y, $sum, $t->{$y}[4]) if abs($sum - $t->{$y}[4]) > 0.0025 + 1e-9;
    }
}

# ---- the LTBO's sheet 1: the population row and the labor force rows, by year
my (%ltboPop, %ltboLfGrowth, %ltboLfpr);
{
    my $c = sheetNamed("$F{ltbo}[0]/$F{ltbo}[1]", '1. Econ Vars_Annual Rates');
    my @yr = grep { ($c->{$_}{B} // '') eq '1995' } keys %$c;
    die "LTBO: the year row (1995 in B) matches " . scalar(@yr) . " rows, not one\n" unless @yr == 1;
    my %colYear; for my $col (keys %{ $c->{ $yr[0] } }) { $colYear{$col} = $c->{ $yr[0] }{$col} if $c->{ $yr[0] }{$col} =~ /^\d{4}$/; }
    my %rowNamed;
    for my $r (keys %$c) { my $a = $c->{$r}{A} // next; for my $want ('Population (millions of people)', 'Labor force growth', 'Labor force participation rate') { push @{ $rowNamed{$want} }, $r if $a =~ /^\Q$want\E[a-z]?$/; } }
    for my $want (keys %rowNamed) { die "LTBO: '$want' heads " . scalar(@{ $rowNamed{$want} }) . " rows, not one\n" unless @{ $rowNamed{$want} } == 1; }
    for my $col (keys %colYear) {
        my $y = $colYear{$col};
        $ltboPop{$y} = $c->{ $rowNamed{'Population (millions of people)'}[0] }{$col};
        $ltboLfGrowth{$y} = $c->{ $rowNamed{'Labor force growth'}[0] }{$col};
        $ltboLfpr{$y} = $c->{ $rowNamed{'Labor force participation rate'}[0] }{$col};
    }
}
my $both = 0;
for my $y (sort keys %ltboPop) {
    next if $y < $FIRST || $y > $LAST || !defined $ltboPop{$y};
    $both++;
    die sprintf("Figure 2 %d: the total %.3f is not the LTBO's population %.3f\n", $y, $ss->{$y}[4], $ltboPop{$y}) if abs($ss->{$y}[4] - $ltboPop{$y}) > 0.0011;
}
die "the LTBO carries only $both of Figure 2's years\n" if $both < 50;
{
    my ($lf, $worst, $at) = (1.0, 0.0, 0);
    for my $y (2026 .. $LAST) {
        $lf *= 1.0 + must($ltboLfGrowth{$y}, "LTBO labor force growth $y") / 100.0;
        my $implied = $lf / (must($ltboLfpr{$y}, "LTBO LFPR $y") / $ltboLfpr{2025});
        my $fig = $cnp->{$y}[4] / $cnp->{2025}[4];
        my $d = abs($implied / $fig - 1.0);
        ($worst, $at) = ($d, $y) if $d > $worst;
    }
    die sprintf("Figure 10 against the LTBO: the civilian noninstitutionalized population index differs from the LTBO's labor force over its rate by %.4f %% at %d\n", 100 * $worst, $at) if $worst > 0.0005;
    printf "Figure 10 is the LTBO's labor force over its participation rate within %.4f %% of the index (worst %d), 2026-%d\n", 100 * $worst, $at, $LAST;
}
printf "Figure 2's total is the LTBO's population at its %d shared years (to 0.001 million)\n", $both;
for my $y (2024, 2025, 2030, 2040, 2055) {
    printf "%d  SS area: <=24 %.3f 25-54 %.3f 55-64 %.3f 65+ %.3f total %.3f | CNP 16+: %.3f\n", $y, @{ $ss->{$y} }, $cnp->{$y}[4];
}
# ---- K299 amendment r3: the by-age table - the zip's ssArea_pop_byYearAgeMaritalSex.csv, folded into the model's 21 bands for BAND_FIRST..BAND_LAST
my ($BAND_FIRST, $BAND_LAST) = (2024, 2098);
my $member = '57059-2025-01-Demographic-Projections/CSV files/ssArea_pop_byYearAgeMaritalSex.csv';
my (%band, %p15, %p16, %seen);
{
    my $csv = member("$F{age}[0]/$F{age}[1]", $member);
    $csv =~ s/\r//g;
    my @lines = split /\n/, $csv;
    die "the table's header is '$lines[0]', not 'year,age,marital_status,sex,number_of_people'\n" unless $lines[0] eq 'year,age,marital_status,sex,number_of_people';
    my %ms = map { $_ => 1 } qw(single married divorced widowed);
    for my $l (@lines[1 .. $#lines]) {
        next if $l eq '';
        my ($y, $age, $m, $s, $n) = split /,/, $l, -1;
        die "the table: '$l' is not five fields\n" unless defined $n && $n ne '';
        die "the table: '$l' - year, age, status, sex or count unreadable\n" unless $y =~ /^\d{4}$/ && ($age =~ /^\d{1,2}$/ || $age eq '100+') && $ms{$m} && ($s eq 'male' || $s eq 'female') && $n =~ /^\d+(?:\.\d+)?$/;
        die "the table: $y $age $m $s carried twice\n" if $seen{"$y $age $m $s"}++;
        next if $y < $BAND_FIRST || $y > $BAND_LAST;
        my $a = $age eq '100+' ? 100 : $age;
        my $b = int($a / 5); $b = 20 if $b > 20;
        $band{$y}[$b] += $n;
        $p15{$y} += $n if $a >= 15;
        $p16{$y} += $n if $a >= 16;
    }
    for my $y ($BAND_FIRST .. $BAND_LAST) {
        die "the table has no $y\n" unless $band{$y};
        for my $b (0 .. 20) { die "the table: $y band $b is empty\n" unless ($band{$y}[$b] // 0) > 0; }
        die "the table: $y does not carry all 101 ages x 4 statuses x 2 sexes\n" unless 808 == grep { /^$y / } keys %seen;
        next if $y > $LAST;
        # the table against Figure 2 (one projection, two files): the total and the four groups, to Figure 2's 0.001 million
        my $tot = 0; $tot += $_ for @{ $band{$y} };
        die sprintf("the table %d: %.4f million against Figure 2's total %.3f\n", $y, $tot / 1e6, $ss->{$y}[4]) if abs($tot / 1e6 - $ss->{$y}[4]) > 0.0006;
        my @g = ([0, 4], [5, 10], [11, 12], [13, 20]);
        for my $k (0 .. 3) {
            my $sum = 0; $sum += $band{$y}[$_] for $g[$k][0] .. $g[$k][1];
            die sprintf("the table %d: group %d %.4f million against Figure 2's %.3f\n", $y, $k, $sum / 1e6, $ss->{$y}[$k]) if abs($sum / 1e6 - $ss->{$y}[$k]) > 0.0006;
        }
    }
}
printf "the table's bands, %d-%d: each year's total and four groups equal Figure 2's to 0.001 million through %d\n", $BAND_FIRST, $BAND_LAST, $LAST;
for my $y (2024, 2025, 2040, 2055, 2098) { printf "%d  the table: 15+ %.3f 16+ %.3f, bands %s\n", $y, $p15{$y} / 1e6, $p16{$y} / 1e6, join(' ', map { sprintf '%.3f', $_ / 1e6 } @{ $band{$y} }); }

exit 0 if $mode eq '--dry';

sub f { my ($v) = @_; my $s = sprintf('%.7g', $v); $s .= '.0' unless $s =~ /[.eE]/; return $s . 'f'; }
sub arr { return 'new float[] { ' . join(', ', map { f($_) } @_) . ' }'; }
my $cs = '';
$cs .= "// GENERATED by Tools/uspop_prep.pl. DO NOT EDIT BY HAND.\n//\n";
$cs .= "// K299 (K290's follow-up): CBO's 2025 demographics - \"The Demographic Outlook: 2025 to 2055\" (January 2025), the data underlying its\n";
$cs .= "// figures and its \"Demographic Projections\" (the Social Security area population by single year of age, 2022-2098), both as CBO reposted them\n";
$cs .= "// on 2 May 2025, from ElectionsData/usa/raw/uspop_cbo_demographics/ (the Internet Archive's captures of cbo.gov's own files). K299's prereg,\n";
$cs .= "// amendment r3: the by-age table is the USA's path, band by band. Every figure below is read by that tool, whose header names each file and the\n";
$cs .= "// cross-checks (the table against Figure 2, Figure 2 and Figure 10 against CBO's March 2025 Long-Term Budget Outlook, ElectionsData/macro/raw/q29_longrun/).\n//\n";
for my $k (sort keys %F) { $cs .= "// SHA-256 ($F{$k}[1]): $digest{$k}\n"; }
$cs .= "\nnamespace PoliSim.Data.Generated\n{\n    public static class UsDemographicOutlook\n    {\n";
$cs .= "        /// <summary>The years: one figure a year from the first to the last, both included.</summary>\n";
$cs .= "        public const int FirstYear = $FIRST, LastYear = $LAST;\n\n";
$cs .= "        /// <summary>The source files' paths and their SHA-256, as this table was generated - GeneratedCatalogCheck re-derives each.</summary>\n";
$cs .= "        public static readonly string[] SourcePath = { " . join(', ', map { "\"$F{$_}[0]/$F{$_}[1]\"" } sort keys %F) . " };\n";
$cs .= "        public static readonly string[] SourceDigest = { " . join(', ', map { "\"$digest{$_}\"" } sort keys %F) . " };\n\n";
$cs .= "        /// <summary>CBO's age groups of the Social Security area population (Figure 2), each the first and last age of the model's five-year bands it\n";
$cs .= "        /// covers: 24 or younger, 25 to 54, 55 to 64, 65 or older (the open band).</summary>\n";
$cs .= "        public static readonly int[][] GroupAges = { new[] { 0, 24 }, new[] { 25, 54 }, new[] { 55, 64 }, new[] { 65, 999 } };\n";
my @gn = ('24 or younger', '25 to 54', '55 to 64', '65 or older');
$cs .= "        /// <summary>Figure 2, the Social Security area population by age group, millions: [group][year - FirstYear], groups in GroupAges' order.</summary>\n";
$cs .= "        public static readonly float[][] Groups =\n        {\n";
for my $g (0 .. 3) { $cs .= "            " . arr(map { $ss->{$_}[$g] } $FIRST .. $LAST) . ",   // $gn[$g]\n"; }
$cs .= "        };\n";
$cs .= "        /// <summary>Figure 2, the Social Security area population, all ages, millions, [year - FirstYear] - equal to the LTBO's population row.</summary>\n";
$cs .= "        public static readonly float[] Total = " . arr(map { $ss->{$_}[4] } $FIRST .. $LAST) . ";\n";
$cs .= "        /// <summary>Figure 10, the civilian noninstitutionalized population age 16 or older (BLS through 2024 plus CBO's projection), millions,\n";
$cs .= "        /// [year - FirstYear] - the population CBO's labor force participation rate applies to (the LTBO's labor force over its rate).</summary>\n";
$cs .= "        public static readonly float[] Civilian16Plus = " . arr(map { $cnp->{$_}[4] } $FIRST .. $LAST) . ";\n";
$cs .= "\n";
$cs .= "        /// <summary>A Figure 2 group's population in a year, millions, the year held inside FirstYear..LastYear.</summary>\n";
$cs .= "        public static float Group(int group, int year) => Groups[group][System.Math.Max(0, System.Math.Min(LastYear - FirstYear, year - FirstYear))];\n\n";
$cs .= "        /// <summary>Amendment r3: the by-age table's years, one pyramid a year from the first to the last, both included.</summary>\n";
$cs .= "        public const int BandsFirstYear = $BAND_FIRST, BandsLastYear = $BAND_LAST;\n";
$cs .= "        /// <summary>Amendment r3: the Social Security area population by the model's 21 bands (0-4 .. 95-99, 100+), both sexes, every marital status, millions:\n";
$cs .= "        /// [year - BandsFirstYear][band] - the table's single years summed, each year's total and Figure 2 groups asserted equal to Figure 2's.</summary>\n";
$cs .= "        public static readonly float[][] Bands =\n        {\n";
for my $y ($BAND_FIRST .. $BAND_LAST) { $cs .= "            " . arr(map { $_ / 1e6 } @{ $band{$y} }) . ",   // $y\n"; }
$cs .= "        };\n";
$cs .= "        /// <summary>Amendment r3: the table's population aged 15 or older and 16 or older, from its single years, millions, [year - BandsFirstYear].</summary>\n";
$cs .= "        public static readonly float[] Population15Plus = " . arr(map { $p15{$_} / 1e6 } $BAND_FIRST .. $BAND_LAST) . ";\n";
$cs .= "        public static readonly float[] Population16Plus = " . arr(map { $p16{$_} / 1e6 } $BAND_FIRST .. $BAND_LAST) . ";\n\n";
$cs .= "        /// <summary>The table's pyramid in a year, millions by band, the year held inside BandsFirstYear..BandsLastYear.</summary>\n";
$cs .= "        public static float[] For(int year) => Bands[System.Math.Max(0, System.Math.Min(BandsLastYear - BandsFirstYear, year - BandsFirstYear))];\n";
$cs .= "    }\n}\n";
$cs =~ s/\n/\r\n/g;
if ($mode eq '--check') {
    my $disk = -e $out ? slurp($out) : '';
    if ($disk eq $cs) { print "$out is what this tool writes\n"; exit 0; }
    print "$out DIFFERS from what this tool writes\n"; exit 1;
}
open my $o, '>:raw', $out or die "$out: $!\n"; print $o $cs; close $o;
print "wrote $out\n";
