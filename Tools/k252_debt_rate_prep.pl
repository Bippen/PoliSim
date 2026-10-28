#!/usr/bin/perl
# K252 (ruled 2026-10-27: "seed it at the source's implied rate; it is the largest term in France's, Italy's and Germany's registered misses"): THE
# DEBT STOCK'S EFFECTIVE INTEREST RATE AT THE SEED - the interest a government paid in a year over the AVERAGE of the debt it owed at the end of the
# year before and at the end of the year (the standard implied-rate convention; the K252 review's M1: the model's stock opens at the end of the
# interest year, so the end-before stock alone overstated day-one interest by the year's debt growth), both in the national accounts' own figures,
# the newest year the accounts hold.
# Reads the stored sources (ElectionsData/macro/raw/k252_debt_rate/, fetched byte-exact by curl, their digests in that folder's SHA256SUMS.txt) and
# writes Assets/Scripts/Data/Generated/DebtRateSeeds.cs:
#  - the EU five: Eurostat gov_10dd_edpt1 (the EDP notification tables), sector S13 (general government), million units of national currency -
#    na_item D41PAY ("Interest, expenditure") in the interest year over na_item GD (gross debt, the Maastricht stock) averaged over the ends of the year before and the year;
#  - the USA: OMB's Historical Tables of the FY 2027 Budget - Table 3.2's line "901 Interest on Treasury debt securities (gross)" in the fiscal year
#    over Table 7.1's "Gross Federal Debt" averaged over the ends of the fiscal year before and the fiscal year: the GROSS perimeter, because the model's US debt is gross federal debt
#    (the DebtPathHoldDiagnostic's perimeter note) and the model charges its rate on that stock.
# The interest year is the newest year every source carries as an outturn (the tool dies unless all six carry it, and unless OMB's Table 7.1 - actuals
# only - ends there), and unless both stocks are present. The rate is interest / ((stock before + stock at the end) / 2) x 100, written to six decimals.
# Usage (from the project root): perl Tools/k252_debt_rate_prep.pl [--check] [cs]
# Writes LF line ends; the output is byte-identical on every run from the same sources.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw(unzip $UnzipError);
use JSON::PP;

my $check = @ARGV && $ARGV[0] eq '--check' ? shift @ARGV : 0;
my $cs = $ARGV[0] // 'Assets/Scripts/Data/Generated/DebtRateSeeds.cs';
my $dir = 'ElectionsData/macro/raw/k252_debt_rate/';
my $d41File = 'eurostat_gov_10dd_edpt1_D41PAY_S13_MIO_NAC.json';
my $gdFile = 'eurostat_gov_10dd_edpt1_GD_S13_MIO_NAC.json';
my $omb32File = 'omb_hist03z2_fy2027.xlsx';
my $omb71File = 'omb_hist07z1_fy2027.xlsx';

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

# ---------------------------------------------------------------- Eurostat
# a JSON-stat answer of one na_item, one unit, one sector: geo -> year -> value
sub eurostat {
    my ($file, $item) = @_;
    my ($bytes, $digest) = verified($file);
    my $j = decode_json($bytes);
    my $dim = $j->{dimension};
    die "$file: not gov_10dd_edpt1's dataset\n" unless ($j->{label} // '') eq 'Government deficit/surplus, debt and associated data';
    for my $one ([na_item => $item], [unit => 'MIO_NAC'], [sector => 'S13'], [freq => 'A']) {
        my ($d, $code) = @$one; my $idx = $dim->{$d}{category}{index};
        die "$file: the $d dimension is not $code alone\n" unless keys %$idx == 1 && exists $idx->{$code};
    }
    my @ids = @{ $j->{id} };
    die "$file: the dimensions are not freq,unit,sector,na_item,geo,time\n" unless join(',', @ids) eq 'freq,unit,sector,na_item,geo,time';
    my $geo = $dim->{geo}{category}{index}; my $time = $dim->{time}{category}{index};
    my $nt = scalar keys %$time;
    my %v;
    for my $g (keys %$geo) { for my $y (keys %$time) {
        my $x = $j->{value}{ $geo->{$g} * $nt + $time->{$y} };
        $v{val}{$g}{$y} = $x if defined $x;
        $v{flag}{$g}{$y} = 1 if defined $j->{status} && defined $j->{status}{ $geo->{$g} * $nt + $time->{$y} };
    } }
    return (\%v, $digest, $j->{updated} // '');
}

# ---------------------------------------------------------------- OMB (the q32 tool's reader)
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

sub isnum { return defined $_[0] && $_[0] =~ /^-?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?$/; }
sub lit { my $s = sprintf('%.6f', $_[0]); $s =~ s/0+$//; $s =~ s/\.$//; return $s . 'f'; }

my ($d41All, $d41Digest, $d41Updated) = eurostat($d41File, 'D41PAY');
my ($gdAll, $gdDigest, $gdUpdated) = eurostat($gdFile, 'GD');
my ($d41, $gd) = ($d41All->{val}, $gdAll->{val});
# L4 (the K252 review): a status flag refuses only a cell the tool reads, not every year fetched
sub plain { my ($all, $file, $g, $y) = @_; die "$file: $g $y carries a status flag - not a plain outturn\n" if $all->{flag}{$g}{$y}; return $all->{val}{$g}{$y}; }

# Table 7.1: the fiscal years in column A, "Gross Federal Debt" (millions) in column B - the header row says so
my ($b71, $omb71Digest) = verified($omb71File);
my %s71 = sheets($b71); die "$omb71File: not one sheet named Table\n" unless $s71{Table};
my %t71 = cells($b71, $s71{Table});
die "$omb71File: row 1 is not Table 7.1's title\n" unless ($t71{1}{A} // '') =~ /^Table 7\.1 - FEDERAL DEBT AT THE END OF YEAR: 1940 - (\d{4})$/;
my $omb71Last = $1;
die "$omb71File: B2 is not 'In Millions of Dollars'\n" unless ($t71{2}{B} // '') eq 'In Millions of Dollars';
die "$omb71File: B3 is not 'Gross Federal Debt'\n" unless ($t71{3}{B} // '') eq 'Gross Federal Debt';
my %grossDebt;
for my $r (keys %t71) { my $y = $t71{$r}{A} // ''; next unless $y =~ /^\d{4}$/; die "$omb71File: ${y}'s gross debt is not a number\n" unless isnum($t71{$r}{B}); $grossDebt{$y} = $t71{$r}{B}; }

# Table 3.2: the years in row 3's header, line 901 its own row
my ($b32, $omb32Digest) = verified($omb32File);
my %s32 = sheets($b32); die "$omb32File: not one sheet named Table\n" unless $s32{Table};
my %t32 = cells($b32, $s32{Table});
die "$omb32File: row 1 is not Table 3.2's title\n" unless ($t32{1}{A} // '') =~ /^Table 3\.2 - OUTLAYS BY FUNCTION AND SUBFUNCTION/;
die "$omb32File: A2 is not '(in millions of dollars)'\n" unless ($t32{2}{A} // '') eq '(in millions of dollars)';
my ($row901) = grep { ($t32{$_}{A} // '') eq '901 Interest on Treasury debt securities (gross)' } keys %t32;
die "$omb32File: no line '901 Interest on Treasury debt securities (gross)'\n" unless $row901;
my %grossInterest;
for my $c (keys %{ $t32{3} }) { my $y = $t32{3}{$c}; next unless $y =~ /^\d{4}$/; $grossInterest{$y} = $t32{$row901}{$c} if isnum($t32{$row901}{$c}); }

# ---------------------------------------------------------------- the interest year
my @eu = ([Sweden => 'SE'], [Germany => 'DE'], [France => 'FR'], [Italy => 'IT'], [Poland => 'PL']);
my $year;
for my $y (sort { $b <=> $a } keys %{ $d41->{FR} }) {
    next unless grep({ defined $d41->{$_->[1]}{$y} && defined $gd->{$_->[1]}{$y - 1} && defined $gd->{$_->[1]}{$y} } @eu) == @eu;
    $year = $y; last;
}
die "no year in which all five carry interest and the stocks at the ends of the year before and the year\n" unless $year;
die "the interest year $year is not the last year of OMB's actuals (Table 7.1 ends $omb71Last)\n" unless $year == $omb71Last;
die "OMB: no gross interest for $year or gross debt for " . ($year - 1) . " and $year\n" unless isnum($grossInterest{$year}) && isnum($grossDebt{$year - 1}) && isnum($grossDebt{$year});

my (%interest, %before, %end, %stock, %rate);
for my $e (@eu) { my ($n, $c) = @$e; $interest{$n} = plain($d41All, $d41File, $c, $year); $before{$n} = plain($gdAll, $gdFile, $c, $year - 1); $end{$n} = plain($gdAll, $gdFile, $c, $year); }
$interest{USA} = $grossInterest{$year}; $before{USA} = $grossDebt{$year - 1}; $end{USA} = $grossDebt{$year};
$stock{$_} = ($before{$_} + $end{$_}) / 2.0 for keys %interest;
my @order = ((map { $_->[0] } @eu), 'USA');
for my $n (@order) {
    $rate{$n} = 100.0 * $interest{$n} / $stock{$n};
    die "$n: the implied rate $rate{$n} lies outside 0.5-8 % - the wrong rows?\n" unless $rate{$n} > 0.5 && $rate{$n} < 8;
}

# ---------------------------------------------------------------- the table
my $o = '';
$o .= "// GENERATED by Tools/k252_debt_rate_prep.pl. DO NOT EDIT BY HAND.\n//\n";
$o .= "// Source : ElectionsData/macro/raw/k252_debt_rate/$d41File (Eurostat, updated $d41Updated)\n// SHA-256: $d41Digest\n";
$o .= "// Source : ElectionsData/macro/raw/k252_debt_rate/$gdFile (Eurostat, updated $gdUpdated)\n// SHA-256: $gdDigest\n";
$o .= "// Source : ElectionsData/macro/raw/k252_debt_rate/$omb32File (OMB, Historical Tables, FY 2027 Budget, Table 3.2)\n// SHA-256: $omb32Digest\n";
$o .= "// Source : ElectionsData/macro/raw/k252_debt_rate/$omb71File (OMB, Historical Tables, FY 2027 Budget, Table 7.1)\n// SHA-256: $omb71Digest\n//\n";
$o .= "// perl Tools/k252_debt_rate_prep.pl --check re-reads the four sources and proves this file is theirs.\n\n";
$o .= "using System.Collections.Generic;\n\nnamespace PoliSim.Data.Generated\n{\n";
$o .= "    /// <summary>K252 (ruled 2026-10-27): the debt stock's effective interest rate at the seed - SOURCED, the national accounts' own: the interest paid in\n";
$o .= "    /// <see cref=\"InterestYear\"/> over the average of the debt owed at the ends of the year before and the year (the review's M1). The EU five from Eurostat gov_10dd_edpt1 (S13, D41PAY over GD, the\n";
$o .= "    /// Maastricht perimeter); the USA from OMB's Historical Tables (Table 3.2 line 901, interest on Treasury debt securities gross, over Table 7.1's gross\n";
$o .= "    /// federal debt - the gross perimeter the model's US debt is). Per cent a year, DERIVED as interest / stock x 100 to six decimals. Generated, never\n";
$o .= "    /// hand-edited.</summary>\n";
$o .= "    public static class DebtRateSeeds\n    {\n";
$o .= "        /// <summary>SOURCED: the year whose interest is read (each source's newest outturn; the stock is averaged over the ends of the year before and this year).</summary>\n";
$o .= "        public const int InterestYear = $year;\n\n";
$o .= "        /// <summary>SOURCED: the interest paid in <see cref=\"InterestYear\"/>, millions of national currency (the USA's in fiscal years).</summary>\n";
$o .= "        public static readonly Dictionary<CountryId, double> InterestMillions = new Dictionary<CountryId, double>\n        {\n";
$o .= "            { CountryId.$_, " . ($interest{$_} + 0) . " },\n" for @order;
$o .= "        };\n\n";
$o .= "        /// <summary>SOURCED: the gross debt at the end of the year before <see cref=\"InterestYear\"/>, millions of national currency.</summary>\n";
$o .= "        public static readonly Dictionary<CountryId, double> StockBeforeMillions = new Dictionary<CountryId, double>\n        {\n";
$o .= "            { CountryId.$_, " . ($before{$_} + 0) . " },\n" for @order;
$o .= "        };\n\n";
$o .= "        /// <summary>SOURCED: the gross debt at the end of <see cref=\"InterestYear\"/>, millions of national currency.</summary>\n";
$o .= "        public static readonly Dictionary<CountryId, double> StockEndMillions = new Dictionary<CountryId, double>\n        {\n";
$o .= "            { CountryId.$_, " . ($end{$_} + 0) . " },\n" for @order;
$o .= "        };\n\n";
$o .= "        /// <summary>DERIVED: the average stock the interest was paid on - (<see cref=\"StockBeforeMillions\"/> + <see cref=\"StockEndMillions\"/>) / 2.</summary>\n";
$o .= "        public static readonly Dictionary<CountryId, double> StockMillions = new Dictionary<CountryId, double>\n        {\n";
$o .= "            { CountryId.$_, " . ($stock{$_} + 0) . " },\n" for @order;
$o .= "        };\n\n";
$o .= "        /// <summary>DERIVED: the implied rate, per cent a year - <see cref=\"InterestMillions\"/> over <see cref=\"StockMillions\"/> x 100. WorldFactory seeds\n";
$o .= "        /// Country.EffectiveDebtInterestRate with it.</summary>\n";
$o .= "        public static readonly Dictionary<CountryId, float> ImpliedRatePercent = new Dictionary<CountryId, float>\n        {\n";
$o .= "            { CountryId.$_, " . lit($rate{$_}) . " },\n" for @order;
$o .= "        };\n";
$o .= "    }\n}\n";

if ($check) {
    my $have = slurp($cs);
    $have =~ s/\r\n/\n/g;
    if ($have eq $o) { print "k252_debt_rate_prep: $cs is byte-identical to the sources' table\n"; exit 0; }
    print "k252_debt_rate_prep: $cs DIFFERS from the sources' table - regenerate it\n"; exit 1;
}
open my $out, '>:raw', $cs or die "$cs: $!";
print $out $o;
close $out;
print "k252_debt_rate_prep: wrote $cs (interest year $year: " . join(', ', map { sprintf('%s %.2f %%', $_, $rate{$_}) } @order) . ")\n";
