#!/usr/bin/perl
# govcons_prep.pl <sources-dir> <out-dir>
#
# T-3, form A (ruled 2026-09-15, COMPLETED.md §503 measured §505, ruled after): the identity's G is the sourced final-consumption share
# of GDP at the seed, grown with the lines. This tool DERIVES the six shares from the files under PoliSim-captures/sources/govcons/,
# never typing a figure:
#
#   government_consumption_2023.csv   general government final consumption expenditure over GDP, 2023, in current prices:
#                                     the five from Eurostat nama_10_gdp (P3_S13 and B1GQ, CP_MNAC - the levels, so the share is
#                                     unrounded), the USA from BEA's NIPA flat file (A955RC government consumption expenditures,
#                                     T30905 line 2, over A191RC GDP, T10105 line 1). Beside each, the figure the publisher prints as a
#                                     share (Eurostat PC_GDP to one decimal; the World Bank's NE.CON.GOVT.ZS for the USA - which is
#                                     BEA's own ratio, so the USA's reading is ONE source in two copies) and BEA's federal-only share
#                                     for the USA (A957RC), the perimeter the model's federal lines sit on.
#
# A pattern that does not match, a figure absent or two readings of one figure disagreeing past their rounding stop the script.
use strict; use warnings; use utf8;
use JSON::PP;
binmode STDOUT, ':encoding(UTF-8)';

my ($src, $out) = @ARGV; die "usage: govcons_prep.pl <sources-dir> <out-dir>\n" unless $src && $out;
mkdir $out unless -d $out;

sub slurp { my ($p) = @_; open my $in, '<:raw', $p or die "$p: $!"; local $/; my $t = <$in>; close $in; return $t; }
sub must { my ($v, $what) = @_; die "govcons_prep: $what not found\n" unless defined $v; return $v; }

# ---- Eurostat JSON-stat: the value at the row-major position over the dimensions in "id" order
sub eurostat {
    my ($file) = @_; my $j = decode_json(slurp($file));
    my @ids = @{ $j->{id} }; my @size = @{ $j->{size} };
    my %index; for my $d (@ids) { my $cat = $j->{dimension}{$d}{category}{index}; $index{$d} = { map { $_ => $cat->{$_} } keys %$cat }; }
    my $pos = sub {
        my (%want) = @_; my $p = 0;
        for my $k (0..$#ids) { my $d = $ids[$k]; my $i = exists $want{$d} ? $index{$d}{$want{$d}} : 0; die "govcons_prep: no $d=$want{$d} in $file\n" unless defined $i; $p = $p * $size[$k] + $i; }
        return $p;
    };
    return ($j, $pos);
}

my ($levels, $lpos) = eurostat("$src/eurostat_nama_10_gdp_P3_S13_B1GQ_CP_MNAC_2023_2024.json");
my ($pc, $ppos) = eurostat("$src/eurostat_nama_10_gdp_P3_S13_PC_GDP_2023_2024.json");
my %name = (SE => 'Sweden', DE => 'Germany', FR => 'France', IT => 'Italy', PL => 'Poland', US => 'USA');
my @rows = ('geo,country,source,consumption_series,gdp_series,year,consumption,gdp,unit,share_pct,published_pct,published_source,federal_share_pct,flag');

for my $g (qw(SE DE FR IT PL)) {
    my $pp = $lpos->(na_item => 'P3_S13', geo => $g, time => '2023'); my $bp = $lpos->(na_item => 'B1GQ', geo => $g, time => '2023');
    my $cons = must($levels->{value}{$pp}, "Eurostat P3_S13 CP_MNAC $g 2023"); my $gdp = must($levels->{value}{$bp}, "Eurostat B1GQ CP_MNAC $g 2023");
    my $flag = join('', grep { defined } ($levels->{status}{$pp}, $levels->{status}{$bp})); $flag = $flag =~ /p/ ? 'p' : '';
    my $share = 100.0 * $cons / $gdp;
    my $published = must($pc->{value}{ $ppos->(geo => $g, time => '2023') }, "Eurostat P3_S13 PC_GDP $g 2023");
    die "govcons_prep: ${g}'s share from the levels (" . sprintf('%.4f', $share) . ") is not the published one-decimal share ($published)\n" if abs($share - $published) > 0.05 + 1e-9;
    push @rows, join(',', $g, $name{$g}, 'Eurostat nama_10_gdp CP_MNAC', 'P3_S13', 'B1GQ', 2023, $cons, $gdp, 'national currency millions', sprintf('%.6f', $share), $published, 'Eurostat nama_10_gdp PC_GDP', '', $flag);
}

# ---- the USA: BEA's NIPA flat file, "%SeriesCode,Period,Value" with thousands separators inside quotes
my $nipa = slurp("$src/bea_NipaDataA.txt");
sub bea { my ($series, $year) = @_; my ($v) = $nipa =~ /^\Q$series\E,\Q$year\E,"?([0-9,.]+)"?\r?$/m; die "govcons_prep: BEA $series $year not found\n" unless defined $v; $v =~ s/,//g; return $v; }
my $usCons = bea('A955RC', 2023); my $usGdp = bea('A191RC', 2023); my $usFed = bea('A957RC', 2023);
my $usShare = 100.0 * $usCons / $usGdp;
my $wb = decode_json(slurp("$src/worldbank_NE.CON.GOVT.ZS_2023_2024.json"));
my ($wbUs) = map { $_->{value} } grep { $_->{countryiso3code} eq 'USA' && $_->{date} eq '2023' } @{ $wb->[1] };
must($wbUs, 'World Bank NE.CON.GOVT.ZS USA 2023');
die "govcons_prep: BEA's ratio ($usShare) and the World Bank's ($wbUs) disagree - they were one figure in two copies when this tool was written\n" if abs($usShare - $wbUs) > 1e-6;
push @rows, join(',', 'US', 'USA', 'BEA NIPA flat file', 'A955RC', 'A191RC', 2023, $usCons, $usGdp, 'USD millions', sprintf('%.6f', $usShare), sprintf('%.6f', $wbUs), 'World Bank NE.CON.GOVT.ZS (BEA\'s ratio)', sprintf('%.6f', 100.0 * $usFed / $usGdp), '');

open my $o, '>:raw:encoding(UTF-8)', "$out/government_consumption_2023.csv" or die $!;
print $o map { "$_\n" } @rows; close $o;
print "wrote $out/government_consumption_2023.csv (", scalar(@rows) - 1, " rows)\n";
print "$_\n" for @rows;
