#!/usr/bin/perl
# income_prep.pl <sources-dir> <out-dir>
#
# S1 of the backlog plan (POLISIM_BACKLOG_PLAN.md, ruled COMPLETED.md §474): the income-by-age data files for F4-1, the income
# dimension of the cohort substrate - DERIVED from the sources on disk, never typed. Every input is a file the README lists by digest
# under PoliSim-captures/sources/income/. Nothing in the runtime reads these files until F4-1's generator does.
#
#   income_by_age_2024.csv          mean and median income per age class: the five from Eurostat ilc_di03 (mean and median
#                                   EQUIVALISED net income, EUR, survey year 2024 = income year 2023, sex T), the USA from the
#                                   CPS ASEC 2024 table PINC-01 (persons 15+ by TOTAL MONEY INCOME in 2023, both sexes, all
#                                   races - a PERSON concept, not an equivalised household one: the README states the difference)
#   income_deciles_2024.csv         the national decile cut-off points (Eurostat ilc_di01, statinfo TC, EUR, 2024) - DS-2's
#                                   cross-check, printed beside the per-band shape and never fitted to
#   us_income_classes_by_age_2023.csv  PINC-01's full class table by age band (counts in thousands per $2,500 class to $100,000+)
#                                   - the validation of the per-band shape for the USA, as SCB's class table is for Sweden
use strict; use warnings; use utf8;
use JSON::PP;
use IO::Uncompress::Unzip;
binmode STDOUT, ':encoding(UTF-8)';

my ($src, $out) = @ARGV; die "usage: income_prep.pl <sources-dir> <out-dir>\n" unless $src && $out;
mkdir $out unless -d $out;

sub slurp { my ($p) = @_; open my $in, '<:raw', $p or die "$p: $!"; local $/; my $t = <$in>; close $in; return $t; }
sub writecsv { my ($p, @lines) = @_; open my $o, '>:raw:encoding(UTF-8)', $p or die "$p: $!"; print $o map { "$_\n" } @lines; close $o; print "wrote $p (", scalar(@lines), " lines)\n"; }

# ---- Eurostat JSON-stat: value index = the row-major position over the dimensions in "id" order
sub eurostat {
    my ($file) = @_; my $j = decode_json(slurp($file));
    my @ids = @{ $j->{id} }; my @size = @{ $j->{size} };
    my %index; for my $d (@ids) { my $cat = $j->{dimension}{$d}{category}{index}; $index{$d} = { map { $_ => $cat->{$_} } keys %$cat }; }
    my $lookup = sub {
        my (%want) = @_; my $pos = 0;
        for my $k (0..$#ids) { my $d = $ids[$k]; my $i = exists $want{$d} ? $index{$d}{$want{$d}} : 0; die "no $d=$want{$d}" unless defined $i; $pos = $pos * $size[$k] + $i; }
        return $j->{value}{$pos};
    };
    return ($j, \%index, $lookup);
}

my @geo = qw(DE FR IT PL SE);
my %country = (DE => 'Germany', FR => 'France', IT => 'Italy', PL => 'Poland', SE => 'Sweden', US => 'United States');

# ---- 1. income_by_age_2024.csv
my ($j3, $ix3, $get3) = eurostat("$src/ilc_di03_2024.json");
my @ages = sort { $ix3->{age}{$a} <=> $ix3->{age}{$b} } keys %{ $ix3->{age} };
my @rows = ('geo,country,age_class,mean,median,unit,concept,source');
for my $g (@geo) {
    for my $a (@ages) {
        my $mean = $get3->(geo => $g, age => $a, statinfo => 'MEAN_EI'); my $med = $get3->(geo => $g, age => $a, statinfo => 'MED_EI');
        next unless defined $mean || defined $med;
        push @rows, join(',', $g, $country{$g}, $a, $mean // '', $med // '', 'EUR', 'equivalised net income per person (household income over the OECD-modified scale)', 'Eurostat ilc_di03 2024 (income year 2023) sex T');
    }
}
# the USA: PINC-01's Age block - the band rows carry the median (column AS) and the mean (AU) in 2023 dollars
my ($pinc_rows, $pinc_classes) = pinc01("$src/pinc01_1_1_1.xlsx");
my %us_age = ('Total, 15 years and over' => 'Y_GE15', 'Under 65 years' => 'Y15-64', '15 to 24 years' => 'Y15-24', '25 to 34 years' => 'Y25-34', '25 to 29 years' => 'Y25-29', '30 to 34 years' => 'Y30-34',
    '35 to 44 years' => 'Y35-44', '35 to 39 years' => 'Y35-39', '40 to 44 years' => 'Y40-44', '45 to 54 years' => 'Y45-54', '45 to 49 years' => 'Y45-49', '50 to 54 years' => 'Y50-54',
    '55 to 64 years' => 'Y55-64', '55 to 59 years' => 'Y55-59', '60 to 64 years' => 'Y60-64', '65 years and over' => 'Y_GE65', '65 to 74 years' => 'Y65-74', '65 to 69 years' => 'Y65-69', '70 to 74 years' => 'Y70-74', '75 years and over' => 'Y_GE75');
for my $r (@$pinc_rows) {
    my $code = $us_age{ $r->{band} } or die "PINC-01 band not mapped: $r->{band}";
    push @rows, join(',', 'US', $country{US}, $code, $r->{mean}, $r->{median}, 'USD', 'total money income per person 15+ with income (not equivalised; the household table HINC-02 is the other concept)', 'US Census CPS ASEC 2024 PINC-01 (income year 2023) both sexes all races');
}
writecsv("$out/income_by_age_2024.csv", @rows);

# ---- 2. income_deciles_2024.csv - the national cut-offs, the cross-check
my ($j1, $ix1, $get1) = eurostat("$src/ilc_di01_2024.json");
my @dec = ('geo,country,quantile,cutoff_eur,source');
for my $g (@geo) {
    for my $q (qw(D1 D2 D3 D4 D5 D6 D7 D8 D9 P95 P99)) {
        next unless exists $ix1->{quant_inc}{$q};
        my $v = $get1->(geo => $g, quant_inc => $q, statinfo => 'TC', unit => 'EUR'); next unless defined $v;
        push @dec, join(',', $g, $country{$g}, $q, $v, 'Eurostat ilc_di01 2024 statinfo TC (top cut-off point) EUR');
    }
}
writecsv("$out/income_deciles_2024.csv", @dec);

# ---- 3. us_income_classes_by_age_2023.csv - the class table, the USA's shape validation
my @cls = ('band,age_class,total_thousands,with_income_thousands,' . join(',', map { s/[\$, ]//g; $_ } @$pinc_classes) . ',median_usd,mean_usd,gini');
for my $r (@$pinc_rows) { push @cls, join(',', "\"$r->{band}\"", $us_age{$r->{band}}, $r->{total}, $r->{with_income}, @{ $r->{counts} }, $r->{median}, $r->{mean}, $r->{gini}); }
writecsv("$out/us_income_classes_by_age_2023.csv", @cls);

# ---- PINC-01: the workbook's Age block (rows between the "Age" header and "Mean age"); columns D..AR the classes, AS median, AU mean, AW Gini
sub pinc01 {
    my ($file) = @_; my %part;
    my $z = IO::Uncompress::Unzip->new($file) or die "$file: $!";
    do { my $n = $z->getHeaderInfo->{Name}; if ($n eq 'xl/sharedStrings.xml' || $n eq 'xl/worksheets/sheet1.xml') { local $/; $part{$n} = <$z>; } } while ($z->nextStream > 0);
    my @ss; while ($part{'xl/sharedStrings.xml'} =~ /<si>(.*?)<\/si>/gs) { my $si = $1; my $t = join('', $si =~ /<t[^>]*>([^<]*)<\/t>/gs); $t =~ s/&amp;/&/g; push @ss, $t; }
    my (@rows, @classes, $in, $n);
    while ($part{'xl/worksheets/sheet1.xml'} =~ /<row[^>]*>(.*?)<\/row>/gs) {
        my $row = $1; my %c;
        while ($row =~ /<c r="([A-Z]+)(\d+)"([^>]*?)(?:\/>|>(.*?)<\/c>)/gs) { my ($col, $attrs, $inner) = ($1, $3, $4 // ''); my $v = ''; if ($inner =~ /<v>([^<]*)<\/v>/) { $v = $1; $v = $ss[$v] if $attrs =~ /t="s"/; } $c{$col} = $v if $v ne ''; }
        next unless %c;
        if (($c{A} // '') eq 'Age') { $in = 1; $n = 0; next; }
        next unless $in; $n++;
        if ($n == 1) { @classes = map { $c{$_} } grep { defined $c{$_} } cols('D', 'AR'); next; }
        next if $n == 2;
        last if ($c{A} // '') =~ /^Mean age/;
        next unless defined $c{B} && $c{B} =~ /^\d/;
        (my $band = $c{A}) =~ s/^\.+//;
        push @rows, { band => $band, total => $c{B}, with_income => $c{C}, counts => [ map { $c{$_} // '' } cols('D', 'AR') ], median => $c{AS}, mean => $c{AU}, gini => $c{AW} };
    }
    die "PINC-01: no age rows read" unless @rows; die "PINC-01: ", scalar(@classes), " classes, not 41" unless @classes == 41;
    return (\@rows, \@classes);
}
sub cols { my ($from, $to) = @_; my @c; my $i = colnum($from); while ($i <= colnum($to)) { push @c, colname($i); $i++ } return @c; }
sub colnum { my ($s) = @_; my $n = 0; $n = $n * 26 + (ord($_) - 64) for split //, $s; return $n; }
sub colname { my ($n) = @_; my $s = ''; while ($n > 0) { my $r = ($n - 1) % 26; $s = chr(65 + $r) . $s; $n = int(($n - 1) / 26); } return $s; }
