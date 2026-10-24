#!/usr/bin/perl
# K62 (Elias's ruling of 2026-10-23, docs/RULINGS_2026-10-23.md): THE PRE-RUN FISCAL YEARS ARE SEEDED FROM A SOURCED ANNUAL BALANCE SERIES, so the
# first count of a start carries Brender & Drazen's fiscal term (A-LOOP q13, docs/specs/A_LOOP_PLAN.md section 4). Reads the two files saved
# byte-exact under ElectionsData/macro/raw/fiscal_balance/ (each held against that folder's SHA256SUMS.txt, which this tool checks) and writes:
#   ElectionsData/macro/fiscal_balance.csv                         country,year,opens,balance_pct_gdp,flag,source
#   Assets/Scripts/Elections/Generated/FiscalBalanceRecord.cs     the same figures as C# literals, the CSV's SHA-256 in its header
# Sources:
#   - Sweden, Germany, France, Italy, Poland: Eurostat gov_10dd_edpt1 (the EDP notification tables), na_item B9 (net lending (+) / net borrowing (-)),
#     sector S13 (general government), unit PC_GDP, annual - the calendar year, opening 1 January. Eurostat's status flag is carried (b = a break).
#   - USA: OMB, Budget of the U.S. Government FY 2027, Historical Tables, Table 1.2 "SUMMARY OF RECEIPTS, OUTLAYS, AND SURPLUSES OR DEFICITS ( - )
#     AS PERCENTAGES OF GDP", the column Total / Surplus or Deficit (-) - the unified federal budget, by FEDERAL fiscal year, which opens on
#     1 October of the calendar year before its number (31 U.S.C. 1102; the table's own "TQ" row is the 1976 transition quarter, left out).
#     Its figures are printed to one decimal; the workbook stores binary doubles (-4.0999999999999996), read back to the one decimal printed.
# A cell OMB prints as "*" or "-*" ("0.05 percent or less") is left out, never read as zero; none falls inside the years kept.
# Years kept: from FIRST_YEAR on (every start's four pre-run years, the earliest start being Italy's of 21 July 2022).
# Usage (from the project root): perl Tools/fiscal_balance_prep.pl [--dry]
# ASCII only.
use strict;
use warnings;
use JSON::PP;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw(unzip $UnzipError);

my $dry = @ARGV && $ARGV[0] eq '--dry';
die "run from the project root\n" unless -d 'Assets/Scripts/Elections';
my $raw = 'ElectionsData/macro/raw/fiscal_balance';
my $FIRST_YEAR = 2010;
my $EUROSTAT = 'eurostat_gov_10dd_edpt1_B9_S13_PC_GDP.json';
my $OMB = 'omb_hist01z2_fy2027.xlsx';

sub slurp { my ($p) = @_; open my $fh, '<:raw', $p or die "$p: $!\n"; local $/; my $s = <$fh>; close $fh; return $s; }

my %sums;
for (split /\n/, slurp("$raw/SHA256SUMS.txt")) { s/\r$//; $sums{$2} = $1 if /^([0-9a-f]{64}) \*?(\S+)$/; }
my %digest;
for my $f ($EUROSTAT, $OMB) {
    $digest{$f} = sha256_hex(slurp("$raw/$f"));
    die "$f: its SHA-256 $digest{$f} is not the one SHA256SUMS.txt lists\n" unless ($sums{$f} // '') eq $digest{$f};
}

# ---- Eurostat JSON-stat 2.0: every dimension but geo and time a single member, asserted
my $j = decode_json(slurp("$raw/$EUROSTAT"));
my @ids = @{$j->{id}}; my @size = @{$j->{size}};
my %want = (unit => 'PC_GDP', sector => 'S13', na_item => 'B9', freq => 'A');
for my $k (0 .. $#ids) {
    my $d = $ids[$k];
    next if $d eq 'geo' || $d eq 'time';
    die "dimension $d has $size[$k] members\n" if $size[$k] != 1;
    my ($member) = keys %{$j->{dimension}{$d}{category}{index}};
    die "dimension $d is $member, not $want{$d}\n" if defined $want{$d} && $member ne $want{$d};
}
my %country = (SE => 'Sweden', DE => 'Germany', FR => 'France', IT => 'Italy', PL => 'Poland');
my (%pct, %flag, %opens, %source);
for my $geo (keys %{$j->{dimension}{geo}{category}{index}}) {
    my $c = $country{$geo} or die "an unexpected geo $geo\n";
    for my $time (keys %{$j->{dimension}{time}{category}{index}}) {
        my $i = 0;
        for my $k (0 .. $#ids) {
            my $d = $ids[$k];
            my $p = $d eq 'geo' ? $j->{dimension}{geo}{category}{index}{$geo} : $d eq 'time' ? $j->{dimension}{time}{category}{index}{$time} : 0;
            $i = $i * $size[$k] + $p;
        }
        next unless defined $j->{value}{$i} && $time >= $FIRST_YEAR;
        $pct{$c}{$time} = $j->{value}{$i};
        $flag{$c}{$time} = $j->{status}{$i} // '';
        $opens{$c}{$time} = sprintf('%04d-01-01', $time);
        $source{$c} = "Eurostat gov_10dd_edpt1 B9 S13 PC_GDP (updated $j->{updated})";
    }
}

# ---- OMB Table 1.2: the workbook's shared strings and its one sheet
sub member { my ($zip, $name) = @_; my $out; unzip($zip => \$out, Name => $name) or die "$name: $UnzipError\n"; return $out; }
my $zip = "$raw/$OMB";
my $sst = member($zip, 'xl/sharedStrings.xml');
my @ss = map { my $t = $_; $t =~ s/<[^>]+>//g; $t } ($sst =~ m{<si>(.*?)</si>}gs);
my $sheet = member($zip, 'xl/worksheets/sheet1.xml');
my %cell;
while ($sheet =~ m{<row r="(\d+)"[^>]*>(.*?)</row>}gs) {
    my ($r, $x) = ($1, $2);
    while ($x =~ m{<c r="([A-Z]+)\d+"([^>]*?)(?:/>|>(?:<f>.*?</f>)?(?:<v>(.*?)</v>)?</c>)}gs) {
        my ($col, $at, $v) = ($1, $2, $3);
        next unless defined $v;
        $v = $ss[$v] if $at =~ /t="s"/;
        $cell{$r}{$col} = $v;
    }
}
die "A1 is not Table 1.2: $cell{1}{A}\n" unless ($cell{1}{A} // '') =~ /^Table 1\.2 - SUMMARY OF RECEIPTS, OUTLAYS, AND SURPLUSES OR DEFICITS/;
die "the columns are not Year / Total / Surplus or Deficit (-)\n"
    unless ($cell{2}{A} // '') eq 'Year' && ($cell{2}{C} // '') eq 'Total' && ($cell{3}{E} // '') eq 'Surplus or Deficit (-)' && ($cell{3}{C} // '') eq 'Receipts' && ($cell{3}{D} // '') eq 'Outlays';
for my $r (sort { $a <=> $b } keys %cell) {
    my ($y, $v) = ($cell{$r}{A}, $cell{$r}{E});
    next unless defined $y && $y =~ /^\d{4}$/ && $y >= $FIRST_YEAR && defined $v;
    next unless $v =~ /^-?\d+(\.\d+)?(E-?\d+)?$/;   # "*" / "-*": 0.05 percent or less - left out
    my $one = sprintf('%.1f', $v + ($v >= 0 ? 1e-9 : -1e-9));
    $one = '0.0' if $one eq '-0.0';
    $pct{USA}{$y} = $one + 0;
    $flag{USA}{$y} = '';
    $opens{USA}{$y} = sprintf('%04d-10-01', $y - 1);
    $source{USA} = 'OMB Historical Tables FY 2027 Table 1.2 Total Surplus or Deficit (-)';
}

my @countries = qw(USA Sweden Germany France Italy Poland);
for my $c (@countries) { die "$c: no year read\n" unless $pct{$c} && keys %{$pct{$c}}; }

my $csv = "country,year,opens,balance_pct_gdp,flag,source\n";
for my $c (@countries) {
    for my $y (sort { $a <=> $b } keys %{$pct{$c}}) { $csv .= join(',', $c, $y, $opens{$c}{$y}, $pct{$c}{$y}, $flag{$c}{$y}, $source{$c}) . "\n"; }
}
my $digestCsv = sha256_hex($csv);
for my $c (@countries) {
    my @y = sort { $a <=> $b } keys %{$pct{$c}};
    printf "FISCAL BALANCE: %-8s %d-%d (%d years) %s\n", $c, $y[0], $y[-1], scalar(@y), join(' ', map { "$_:$pct{$c}{$_}" . ($flag{$c}{$_} ? "($flag{$c}{$_})" : '') } @y);
}
printf "  %s %s\n  %s %s\n  csv %s\n", $EUROSTAT, $digest{$EUROSTAT}, $OMB, $digest{$OMB}, $digestCsv;
exit 0 if $dry;

my $csvPath = 'ElectionsData/macro/fiscal_balance.csv';
open my $o, '>:raw', $csvPath or die "$csvPath: $!\n"; print $o $csv; close $o;

my $cs = "// GENERATED by Tools/fiscal_balance_prep.pl. DO NOT EDIT BY HAND.\n//\n// Source : $csvPath\n// SHA-256: $digestCsv\n"
    . "// Raw    : $raw/$EUROSTAT\n// SHA-256: $digest{$EUROSTAT}\n// Raw    : $raw/$OMB\n// SHA-256: $digest{$OMB}\n//\n"
    . "// FiscalBalanceRecordCheck re-reads both raw files and the CSV and compares every figure.\n\n"
    . "namespace PoliSim.Elections.Generated\n{\n"
    . "    /// <summary>K62 (2026-10-23): the annual government balance, a percentage of GDP, surplus positive - SOURCED: Eurostat gov_10dd_edpt1 (B9, S13,\n"
    . "    /// PC_GDP; the calendar year) for the EU five, OMB's Historical Table 1.2 (the unified federal budget's surplus or deficit; the federal fiscal\n"
    . "    /// year, opening 1 October) for the USA (the record `ElectionsData/macro/fiscal_balance.md`). Each row is a year, the day it opens and its\n"
    . "    /// balance; a year runs twelve months from its opening. Generated, never hand-edited.</summary>\n"
    . "    public static class FiscalBalanceRecord\n    {\n        public const string SourceDigest = \"$digestCsv\";\n"
    . "        public const string EurostatDigest = \"$digest{$EUROSTAT}\";\n        public const string OmbDigest = \"$digest{$OMB}\";\n\n";
for my $c (@countries) {
    $cs .= "        /// <summary>$c: the year, the day it opens (year, month, day) and its balance, % of GDP.</summary>\n";
    $cs .= "        public static readonly (int Year, int OpensYear, int OpensMonth, int OpensDay, double Percent)[] $c =\n        {\n";
    for my $y (sort { $a <=> $b } keys %{$pct{$c}}) {
        my ($oy, $om, $od) = split /-/, $opens{$c}{$y};
        my $v = sprintf('%.1f', $pct{$c}{$y});
        $cs .= sprintf("            (%d, %d, %d, %d, %s),\n", $y, $oy, $om + 0, $od + 0, $v);
    }
    $cs .= "        };\n\n";
}
$cs .= "        /// <summary>The country's years, oldest first; none for a country the record does not hold.</summary>\n"
    . "        public static (int Year, int OpensYear, int OpensMonth, int OpensDay, double Percent)[] For(PoliSim.Data.CountryId id)\n        {\n"
    . "            switch (id)\n            {\n";
$cs .= "                case PoliSim.Data.CountryId.$_: return $_;\n" for @countries;
$cs .= "                default: return new (int, int, int, int, double)[0];\n            }\n        }\n    }\n}\n";
$cs =~ s/\r?\n/\r\n/g;
my $csPath = 'Assets/Scripts/Elections/Generated/FiscalBalanceRecord.cs';
open my $p, '>:raw', $csPath or die "$csPath: $!\n"; print $p $cs; close $p;
print "WROTE $csvPath and $csPath\n";
