#!/usr/bin/perl
# MF q32 (ruled 2026-10-18 q32: "the no-policy debt path is held against CBO's long-term outlook and the EC Debt Sustainability Monitor"): THE TWO
# BASELINE DEBT PATHS THE NO-POLICY RUN IS HELD AGAINST.
# Reads the two stored workbooks (ElectionsData/macro/raw/q32_debt/, fetched byte-exact by curl, their digests in that folder's SHA256SUMS.txt) and
# writes Assets/Editor/Generated/DebtPathAnchors.cs:
#  - CBO, February 2026, the extended baseline (Supplemental Table 1, "Summary Data for the Extended Baseline", percentage of GDP): federal debt held
#    by the public at the end of each fiscal year, 2026 to 2056 - the column whose header reads "Federal debt held by the public" (the tool dies unless it does);
#  - the European Commission's Debt Sustainability Monitor 2024, country fiches (one sheet per member state, named by its code): the baseline gross
#    debt ratio, per cent of GDP, from the row under "<country> - baseline scenario (% of GDP)" (its "Gross debt ratio" row), and the stochastic
#    projections' 10th and 90th percentiles (the rows the sheet labels p10 and p90 beside "stochastic"), each year the sheet's header gives.
# K218 (2026-10-28): it also writes each source's own decomposition of the debt change, which the hold prints beside the model's - CBO's revenues, total
#    noninterest spending, revenues minus total noninterest spending (the primary balance), net interest and GDP in billions of dollars (the same table's
#    columns, matched by header); the DSM's baseline block rows (1) Primary balance, (1.1.2) Cost of ageing, (2.1) Interest expenditure, (2.2) Growth effect
#    (real), (2.3) Inflation effect and (3) Stock-flow adjustments (matched by label; a blank cell - no cost of ageing before the projection's first year -
#    is 0), the DSM's own identity (the change in the ratio = -(1) + (2) + (3)) checked every year to 0.1 point.
# DebtPathHoldDiagnostic reads the generated table; this tool re-reads the workbooks and with --check proves the table is theirs, byte for byte.
# Usage (from the project root): perl Tools/q32_debt_prep.pl [--check] [cs]
# Writes LF line ends; the output is byte-identical on every run from the same workbooks.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw(unzip $UnzipError);

my $check = @ARGV && $ARGV[0] eq '--check' ? shift @ARGV : 0;
my $cs = $ARGV[0] // 'Assets/Editor/Generated/DebtPathAnchors.cs';
my $dir = 'ElectionsData/macro/raw/q32_debt/';
my $cboFile = 'wayback_20260211152810_cbo_51119-2026-02-LTBO-Budget.xlsx';
my $dsmFile = 'ec_dsm2024_country_fiches_tables_and_graphs_final.xlsx';

sub slurp { my ($p) = @_; open my $f, '<:raw', $p or die "open $p: $!"; local $/; my $t = <$f>; close $f; return $t; }

sub verified {
    my ($file) = @_;
    my $bytes = slurp($dir . $file);
    my $digest = sha256_hex($bytes);
    open my $sf, '<', $dir . 'SHA256SUMS.txt' or die "SHA256SUMS.txt: $!";
    my $listed = grep { s/\r?\n\z//; $_ eq "$digest *$file" } <$sf>;
    close $sf;
    die "$file: digest $digest is not the one SHA256SUMS.txt lists\n" unless $listed;
    return ($bytes, $digest);
}

# the workbook's sheets by name -> their part paths
sub sheets {
    my ($bytes) = @_;
    my ($wb, $rels);
    unzip \$bytes => \$wb, Name => 'xl/workbook.xml' or die "workbook.xml: $UnzipError";
    unzip \$bytes => \$rels, Name => 'xl/_rels/workbook.xml.rels' or die "workbook.xml.rels: $UnzipError";
    my %target;
    while ($rels =~ m{<Relationship\b([^>]*)/?>}g) {
        my $a = $1; my ($id) = $a =~ /\bId="([^"]+)"/; my ($t) = $a =~ /\bTarget="([^"]+)"/;
        $t =~ s{^/?xl/}{}; $target{$id} = $t if defined $id && defined $t;
    }
    my %sheet;
    while ($wb =~ m{<sheet\b([^>]*)/?>}g) {
        my $a = $1; my ($n) = $a =~ /\bname="([^"]+)"/; my ($id) = $a =~ /\br:id="([^"]+)"/;
        $sheet{$n} = 'xl/' . $target{$id} if defined $n && defined $id && $target{$id};
    }
    return %sheet;
}

# a sheet's cells: row number -> { column letters -> text }
sub cells {
    my ($bytes, $part) = @_;
    my $ss = '';
    unzip \$bytes => \$ss, Name => 'xl/sharedStrings.xml' or $ss = '';
    my @strings;
    while ($ss =~ m{<si>(.*?)</si>}gs) { my $si = $1; my $t = join('', $si =~ m{<t[^>]*>(.*?)</t>}gs); $t =~ s/&amp;/&/g; $t =~ s/&lt;/</g; $t =~ s/&gt;/>/g; push @strings, $t; }
    my $xml;
    unzip \$bytes => \$xml, Name => $part or die "$part: $UnzipError";
    my %rows;
    while ($xml =~ m{<row r="(\d+)"[^>]*>(.*?)</row>}gs) {
        my ($r, $row) = ($1, $2);
        while ($row =~ m{<c r="([A-Z]+)\d+"([^>]*?)(?:/>|>(.*?)</c>)}gs) {
            my ($col, $attr, $body) = ($1, $2, $3 // ''); my $v;
            if ($attr =~ /t="s"/) { my ($i) = $body =~ m{<v>(\d+)</v>}; $v = defined $i ? $strings[$i] : undef; }
            elsif ($attr =~ /t="inlineStr"/) { ($v) = $body =~ m{<t[^>]*>(.*?)</t>}s; }
            else { ($v) = $body =~ m{<v>(.*?)</v>}s; }
            next unless defined $v && $v ne '';
            $v =~ s/\s+/ /g; $v =~ s/^ | $//g;
            $rows{$r}{$col} = $v;
        }
    }
    return %rows;
}

sub colnum { my $n = 0; $n = $n * 26 + ord($_) - 64 for split //, shift; return $n; }
sub isnum { return defined $_[0] && $_[0] =~ /^-?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?$/; }
# a figure as a C# float literal: the workbook's double, rounded to 6 decimals (the table's precision; the workbooks carry binary noise past it)
sub lit { my $s = sprintf('%.6f', $_[0]); $s =~ s/0+$//; $s =~ s/\.$//; return $s . 'f'; }

# ---------------------------------------------------------------- CBO
my ($cboBytes, $cboDigest) = verified($cboFile);
my %cboSheets = sheets($cboBytes);
die "$cboFile: no sheet 'Supplemental Table 1'\n" unless $cboSheets{'Supplemental Table 1'};
my %cbo = cells($cboBytes, $cboSheets{'Supplemental Table 1'});
my ($headRow, $debtCol, %cboCol);
for my $r (sort { $a <=> $b } keys %cbo) {
    next unless ($cbo{$r}{A} // '') eq 'Fiscal year';
    $headRow = $r;
    for my $c (keys %{ $cbo{$r} }) { $debtCol = $c if $cbo{$r}{$c} =~ /^Federal debt held by the public/; }
    # K218: the decomposition's columns, each matched by its header's own words
    for my $c (keys %{ $cbo{$r} }) {
        my $hd = $cbo{$r}{$c};
        $cboCol{Revenues} = $c if $hd eq 'Revenues';
        $cboCol{Noninterest} = $c if $hd =~ /^Total noninterest spending/;
        $cboCol{Primary} = $c if $hd =~ /^Revenues minus total noninterest spending/;
        $cboCol{NetInterest} = $c if $hd eq 'Net interest';
        $cboCol{Gdp} = $c if $hd eq 'GDP (billions of dollars)';
    }
    last;
}
die "$cboFile: no 'Fiscal year' header row\n" unless $headRow;
die "$cboFile: no 'Federal debt held by the public' column in the header row\n" unless $debtCol;
for my $k (qw(Revenues Noninterest Primary NetInterest Gdp)) { die "$cboFile: no '$k' column in the header row\n" unless $cboCol{$k}; }
my %cboSeries;
my %cboDebt;
for my $r (sort { $a <=> $b } keys %cbo) {
    next unless $r > $headRow;
    my $y = $cbo{$r}{A} // '';
    last unless $y =~ /^\d{4}$/;
    die "$cboFile: ${y}'s debt '" . ($cbo{$r}{$debtCol} // '') . "' is not a number\n" unless isnum($cbo{$r}{$debtCol});
    $cboDebt{$y} = $cbo{$r}{$debtCol};
    for my $k (keys %cboCol) { die "$cboFile: ${y}'s $k '" . ($cbo{$r}{$cboCol{$k}} // '') . "' is not a number\n" unless isnum($cbo{$r}{$cboCol{$k}}); $cboSeries{$k}{$y} = $cbo{$r}{$cboCol{$k}}; }
    die "$cboFile: ${y}'s primary balance is not revenues less noninterest spending\n" if abs($cboSeries{Revenues}{$y} - $cboSeries{Noninterest}{$y} - $cboSeries{Primary}{$y}) > 0.01;
}
my @cboYears = sort { $a <=> $b } keys %cboDebt;
die "$cboFile: the years do not run 2026 to 2056 (" . join(',', @cboYears) . ")\n" unless @cboYears == 31 && $cboYears[0] == 2026 && $cboYears[-1] == 2056;
die "$cboFile: 2026's debt held by the public $cboDebt{2026} is not near 100 % of GDP - the wrong column?\n" unless $cboDebt{2026} > 90 && $cboDebt{2026} < 110;

# ---------------------------------------------------------------- the DSM
my ($dsmBytes, $dsmDigest) = verified($dsmFile);
my %dsmSheets = sheets($dsmBytes);
my @dsmOrder = ([SE => 'Sweden'], [DE => 'Germany'], [FR => 'France'], [IT => 'Italy'], [PL => 'Poland']);
my (%baseFirst, %base, %fanFirst, %p10, %p90, %dec);
# K218: the decomposition rows, by the label the sheet's column D carries, and the name each is written under
my @decRows = (['(1) Primary balance' => 'PrimaryBalance'], ['(1.1.2) Cost of ageing' => 'CostOfAgeing'], ['(2.1) Interest expenditure' => 'Interest'],
    ['(2.2) Growth effect (real)' => 'GrowthEffect'], ['(2.3) Inflation effect' => 'InflationEffect'], ['(3) Stock-flow adjustments' => 'StockFlow'], ['Change in the ratio' => 'Change']);
for my $pair (@dsmOrder) {
    my ($code, $name) = @$pair;
    die "$dsmFile: no sheet $code\n" unless $dsmSheets{$code};
    my %s = cells($dsmBytes, $dsmSheets{$code});
    # the baseline: the header row "<name> - baseline scenario (% of GDP)" carries the years; the next row "Gross debt ratio" the figures
    my $h;
    for my $r (sort { $a <=> $b } keys %s) { if (grep { $_ eq "$name - baseline scenario (% of GDP)" } values %{ $s{$r} }) { $h = $r; last; } }
    die "$dsmFile $code: no '$name - baseline scenario (% of GDP)' row\n" unless $h;
    my $g = $h + 1;
    die "$dsmFile $code: the row under the baseline header is not 'Gross debt ratio'\n" unless ($s{$g}{D} // '') eq 'Gross debt ratio';
    my @cols = sort { colnum($a) <=> colnum($b) } grep { ($s{$h}{$_} // '') =~ /^\d{4}$/ } keys %{ $s{$h} };
    die "$dsmFile $code: the baseline header carries no years\n" unless @cols;
    my @years = map { $s{$h}{$_} } @cols;
    for my $i (1 .. $#years) { die "$dsmFile $code: the baseline's years do not run on\n" unless $years[$i] == $years[$i - 1] + 1; }
    die "$dsmFile $code: the baseline does not reach 2035\n" unless $years[-1] == 2035;
    $baseFirst{$name} = $years[0];
    $base{$name} = [ map { die "$dsmFile $code: a baseline figure '" . ($s{$g}{$_} // '') . "' is not a number\n" unless isnum($s{$g}{$_}); $s{$g}{$_} } @cols ];
    # K218: each decomposition row - the first row below the header whose label opens with the row's words; a blank cell is 0 (no cost of ageing before 2026)
    for my $dr (@decRows) {
        my ($label, $key) = @$dr; my $row;
        for my $r (sort { $a <=> $b } keys %s) { next unless $r > $h; if (index($s{$r}{D} // '', $label) == 0) { $row = $r; last; } }
        die "$dsmFile $code: no '$label' row in the baseline block\n" unless $row && $row < $h + 30;
        $dec{$key}{$name} = [ map { my $v = $s{$row}{$_}; die "$dsmFile $code: a '$label' figure '$v' is not a number\n" if defined $v && !isnum($v); $v // 0 } @cols ];
    }
    for my $i (0 .. $#cols) {
        my $sum = -$dec{PrimaryBalance}{$name}[$i] + $dec{Interest}{$name}[$i] + $dec{GrowthEffect}{$name}[$i] + $dec{InflationEffect}{$name}[$i] + $dec{StockFlow}{$name}[$i];
        die "$dsmFile $code: $years[$i]'s decomposition does not sum to its change in the ratio\n" if abs($sum - $dec{Change}{$name}[$i]) > 0.1;
    }
    # the fan: the rows labelled p10 and p90 (column X); their years are the header row just above the p10 row whose X... reads the years in Y..
    my ($r10, $r90);
    for my $r (sort { $a <=> $b } keys %s) { $r10 //= $r if ($s{$r}{X} // '') eq 'p10'; $r90 //= $r if ($s{$r}{X} // '') eq 'p90'; }
    die "$dsmFile $code: no p10 or p90 row\n" unless $r10 && $r90 && $r90 > $r10;
    my $fh = $r10 - 1;
    my @fcols = sort { colnum($a) <=> colnum($b) } grep { colnum($_) > colnum('X') && ($s{$fh}{$_} // '') =~ /^\d{4}$/ } keys %{ $s{$fh} };
    die "$dsmFile $code: no year header above the p10 row\n" unless @fcols;
    my @fyears = map { $s{$fh}{$_} } @fcols;
    for my $i (1 .. $#fyears) { die "$dsmFile $code: the fan's years do not run on\n" unless $fyears[$i] == $fyears[$i - 1] + 1; }
    $fanFirst{$name} = $fyears[0];
    $p10{$name} = [ map { die "$dsmFile $code: a p10 figure is not a number\n" unless isnum($s{$r10}{$_}); $s{$r10}{$_} } @fcols ];
    $p90{$name} = [ map { die "$dsmFile $code: a p90 figure is not a number\n" unless isnum($s{$r90}{$_}); $s{$r90}{$_} } @fcols ];
    for my $i (0 .. $#fcols) { die "$dsmFile $code: p10 above p90 in $fyears[$i]\n" if $p10{$name}[$i] > $p90{$name}[$i]; }
}
my %one = map { $_ => 1 } values %baseFirst; die "the DSM's baselines open in different years\n" unless keys %one == 1;
my %oneF = map { $_ => 1 } values %fanFirst; die "the DSM's fans open in different years\n" unless keys %oneF == 1;
my ($dsmFirst) = keys %one; my ($fanFirstYear) = keys %oneF;

# ---------------------------------------------------------------- the table
my $o = '';
$o .= "// GENERATED by Tools/q32_debt_prep.pl. DO NOT EDIT BY HAND.\n//\n";
$o .= "// Source : ElectionsData/macro/raw/q32_debt/$cboFile\n// SHA-256: $cboDigest\n";
$o .= "// Source : ElectionsData/macro/raw/q32_debt/$dsmFile\n// SHA-256: $dsmDigest\n//\n";
$o .= "// perl Tools/q32_debt_prep.pl --check re-reads both workbooks and proves this file is theirs.\n\n";
$o .= "using System.Collections.Generic;\n\nnamespace PoliSim.Data.Generated\n{\n";
$o .= "    /// <summary>MF q32 (ruled 2026-10-18 q32): the baseline debt paths the no-policy run is held against (`DebtPathHoldDiagnostic`) - CBO's February 2026\n";
$o .= "    /// extended baseline for the USA (federal debt held by the public, per cent of GDP, end of fiscal year) and the European Commission's Debt\n";
$o .= "    /// Sustainability Monitor 2024 for the EU five (general government gross debt, per cent of GDP: the baseline, and the stochastic projections' 10th\n";
$o .= "    /// and 90th percentiles). Each figure is the workbook's, to six decimals. Generated, never hand-edited.</summary>\n";
$o .= "    public static class DebtPathAnchors\n    {\n";
$o .= "        public const string CboSourcePath = \"ElectionsData/macro/raw/q32_debt/$cboFile\";\n";
$o .= "        public const string CboSourceDigest = \"$cboDigest\";\n";
$o .= "        public const string DsmSourcePath = \"ElectionsData/macro/raw/q32_debt/$dsmFile\";\n";
$o .= "        public const string DsmSourceDigest = \"$dsmDigest\";\n\n";
$o .= "        /// <summary>The first fiscal year of <see cref=\"CboDebtHeldByPublic\"/>; element i is year CboFirstYear + i.</summary>\n";
$o .= "        public const int CboFirstYear = $cboYears[0];\n";
$o .= "        public static readonly float[] CboDebtHeldByPublic = { " . join(', ', map { lit($cboDebt{$_}) } @cboYears) . " };\n\n";
$o .= "        /// <summary>K218: CBO's own decomposition, the same table, the same years - revenues, total noninterest spending, their difference (the primary balance,\n";
$o .= "        /// positive a surplus) and net interest, per cent of GDP; GDP in billions of dollars (its growth is the growth effect's).</summary>\n";
for my $k (qw(Revenues Noninterest Primary NetInterest Gdp)) {
    my %name = (Revenues => 'CboRevenues', Noninterest => 'CboNoninterestSpending', Primary => 'CboPrimaryBalance', NetInterest => 'CboNetInterest', Gdp => 'CboGdpBillions');
    $o .= "        public static readonly float[] $name{$k} = { " . join(', ', map { lit($cboSeries{$k}{$_}) } @cboYears) . " };\n";
}
$o .= "\n";
$o .= "        /// <summary>The first year of each DSM baseline; element i is year DsmFirstYear + i.</summary>\n";
$o .= "        public const int DsmFirstYear = $dsmFirst;\n";
$o .= "        public static readonly Dictionary<CountryId, float[]> DsmBaseline = new Dictionary<CountryId, float[]>\n        {\n";
$o .= "            { CountryId.$_->[1], new[] { " . join(', ', map { lit($_) } @{ $base{$_->[1]} }) . " } },\n" for @dsmOrder;
$o .= "        };\n\n";
$o .= "        /// <summary>The first year of each DSM fan; element i is year DsmFanFirstYear + i.</summary>\n";
$o .= "        public const int DsmFanFirstYear = $fanFirstYear;\n";
$o .= "        public static readonly Dictionary<CountryId, float[]> DsmP10 = new Dictionary<CountryId, float[]>\n        {\n";
$o .= "            { CountryId.$_->[1], new[] { " . join(', ', map { lit($_) } @{ $p10{$_->[1]} }) . " } },\n" for @dsmOrder;
$o .= "        };\n";
$o .= "        public static readonly Dictionary<CountryId, float[]> DsmP90 = new Dictionary<CountryId, float[]>\n        {\n";
$o .= "            { CountryId.$_->[1], new[] { " . join(', ', map { lit($_) } @{ $p90{$_->[1]} }) . " } },\n" for @dsmOrder;
$o .= "        };\n\n";
$o .= "        /// <summary>K218: the DSM's own decomposition of each year's change in the baseline's ratio, per cent of GDP, the baseline's years (element i is DsmFirstYear + i):\n";
$o .= "        /// the primary balance (positive a surplus; it subtracts), of which the cost of ageing, the interest expenditure, the real growth and inflation effects and the\n";
$o .= "        /// stock-flow adjustments - the change is -PrimaryBalance + Interest + GrowthEffect + InflationEffect + StockFlow (the tool checks it every year).</summary>\n";
for my $dr (grep { $_->[1] ne 'Change' } @decRows) {
    my $key = $dr->[1];
    $o .= "        public static readonly Dictionary<CountryId, float[]> Dsm$key = new Dictionary<CountryId, float[]>\n        {\n";
    $o .= "            { CountryId.$_->[1], new[] { " . join(', ', map { lit($_) } @{ $dec{$key}{$_->[1]} }) . " } },\n" for @dsmOrder;
    $o .= "        };\n";
}
$o .= "    }\n}\n";

if ($check) {
    my $have = slurp($cs);
    $have =~ s/\r\n/\n/g;
    if ($have eq $o) { print "q32_debt_prep: $cs is byte-identical to the workbooks' table\n"; exit 0; }
    print "q32_debt_prep: $cs DIFFERS from the workbooks' table - regenerate it\n"; exit 1;
}
open my $out, '>:raw', $cs or die "$cs: $!";
print $out $o;
close $out;
print "q32_debt_prep: wrote $cs (CBO $cboYears[0]-$cboYears[-1]; DSM baselines from $dsmFirst, fans from $fanFirstYear)\n";
