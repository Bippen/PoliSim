#!/usr/bin/perl
# energy_fiscal_prep.pl <sources-dir> <out-dir>
#
# Stage 4 of the energy track (EN-4, the fiscal layer; POLISIM_ENERGY_SPECLET.md S7): the retail stack's seed, DERIVED from the
# sources on disk - never typed. Adds to EnergyData/:
#
#   retail_2023.csv   per country × customer class: the 2023 consumption and the retail price's components per kWh in the country's
#                     currency - energy and supply, network, policy levies (renewable + capacity + other charges), environmental and
#                     nuclear taxes, VAT - Eurostat's price-component tables for the five, EIA's annual retail price for the USA.
#
# The runtime splits "energy and supply" into the dispatch's wholesale price and a FITTED supply margin at the seed (EnergyLedger).
use strict; use warnings; use utf8;
use JSON::PP;
use IO::Uncompress::Unzip qw(unzip $UnzipError);
binmode STDOUT, ':encoding(UTF-8)';

my ($src, $out) = @ARGV; die "usage: energy_fiscal_prep.pl <sources-dir> <out-dir>\n" unless $src && $out;
my @eu = qw(DE FR IT PL SE);

sub slurp { my ($p) = @_; open my $f, '<:raw', $p or die "open $p: $!"; local $/; my $t = <$f>; close $f; return $t; }
sub jsonstat_rows {
    my ($path) = @_; my $j = JSON::PP->new->utf8->decode(slurp($path));
    my @ids = @{ $j->{id} }; my @size = @{ $j->{size} }; my @codes;
    for my $i (0..$#ids) { my $idx = $j->{dimension}{$ids[$i]}{category}{index}; $codes[$i] = ref $idx eq 'HASH' ? [ sort { $idx->{$a} <=> $idx->{$b} } keys %$idx ] : $idx; }
    my @stride; my $s = 1; for (my $i = $#ids; $i >= 0; $i--) { $stride[$i] = $s; $s *= $size[$i]; }
    my @rows; for my $lin (keys %{ $j->{value} }) { my %r = (value => $j->{value}{$lin}); for my $i (0..$#ids) { $r{$ids[$i]} = $codes[$i][ int($lin / $stride[$i]) % $size[$i] ]; } push @rows, \%r; }
    return @rows;
}
sub xlsx_sheet_rows {
    my ($bytes, $sheet) = @_; my $ss = ''; unzip \$bytes => \$ss, Name => 'xl/sharedStrings.xml' or $ss = ''; utf8::decode($ss);
    my @strings; while ($ss =~ m{<si>(.*?)</si>}gs) { my $si = $1; my $t = join('', $si =~ m{<t[^>]*>(.*?)</t>}gs); $t =~ s/&amp;/&/g; $t =~ s/&lt;/</g; $t =~ s/&gt;/>/g; push @strings, $t; }
    my $xml; unzip \$bytes => \$xml, Name => "xl/worksheets/sheet$sheet.xml" or die "sheet$sheet: $UnzipError"; utf8::decode($xml);
    my @rows;
    while ($xml =~ m{<row[^>]*>(.*?)</row>}gs) {
        my $row = $1; my @cells; my $max = 0;
        while ($row =~ m{<c r="([A-Z]+)\d+"([^>]*?)(?:/>|>(.*?)</c>)}gs) {
            my ($col, $attr, $body) = ($1, $2, $3 // ''); my $v = '';
            if ($attr =~ /t="s"/) { my ($i) = $body =~ m{<v>(\d+)</v>}; $v = defined $i ? $strings[$i] : ''; }
            elsif ($attr =~ /t="inlineStr"/) { ($v) = $body =~ m{<t[^>]*>(.*?)</t>}s; $v //= ''; }
            else { ($v) = $body =~ m{<v>(.*?)</v>}s; $v //= ''; }
            $v =~ s/[\t\r\n]+/ /g; my $n = 0; for my $ch (split //, $col) { $n = $n * 26 + ord($ch) - 64; } $cells[$n - 1] = $v; $max = $n if $n > $max;
        }
        push @rows, [ map { defined $_ ? $_ : '' } @cells[0..$max-1] ];
    }
    return @rows;
}
sub num { my $s = shift // ''; $s =~ s/,//g; return ($s =~ /^-?\d+(\.\d+)?([eE][-+]?\d+)?$/) ? $s + 0 : undef; }
sub f4 { sprintf('%.4f', $_[0]) } sub f1 { sprintf('%.1f', $_[0]) }

# consumption by class, GWh: households = FC_OTH_HH_E; non-households = final consumption less households (industry, services, transport, agriculture, other)
my (%fc, %hh);
for my $r (jsonstat_rows("$src/eurostat_nrg_cb_e_2023.json")) { next unless $r->{unit} eq 'GWH' && $r->{time} eq '2023'; $fc{$r->{geo}} = $r->{value} if $r->{nrg_bal} eq 'FC'; $hh{$r->{geo}} = $r->{value} if $r->{nrg_bal} eq 'FC_OTH_HH_E'; }
# the components, EUR per kWh, annual 2023: households band DC (2 500-4 999 kWh), non-households band IC (500-1 999 MWh)
my %comp;   # {cc}{class}{code}
for my $r (jsonstat_rows("$src/eurostat_nrg_pc_204_c_2023.json")) { next unless $r->{currency} eq 'EUR' && $r->{time} eq '2023' && $r->{nrg_cons} eq 'KWH2500-4999'; $comp{$r->{geo}}{households}{$r->{nrg_prc}} = $r->{value}; }
for my $r (jsonstat_rows("$src/eurostat_nrg_pc_205_c_2023.json")) { next unless $r->{currency} eq 'EUR' && $r->{time} eq '2023' && $r->{nrg_cons} eq 'MWH500-1999'; $comp{$r->{geo}}{nonhousehold}{$r->{nrg_prc}} = $r->{value}; }
# the USA: EPA Table 2.4 (cents per kWh by sector, 2023) and 2.2 (sales by sector, MWh); households = residential, non-households = commercial + industrial + transportation, sales-weighted
my (%usPrice, %usSales);
{
    my @p = xlsx_sheet_rows(slurp("$src/eia_epa_02_04.xlsx"), 1); my ($r) = grep { ($_->[0] // '') eq '2023' } @p; die "EPA 2.4 2023" unless $r; @usPrice{qw(res com ind tra)} = map { num($_) } @$r[1..4];
    my @s = xlsx_sheet_rows(slurp("$src/eia_epa_02_02.xlsx"), 1); my ($q) = grep { ($_->[0] // '') eq '2023' } @s; die "EPA 2.2 2023" unless $q; @usSales{qw(res com ind tra)} = map { num($_) } @$q[1..4];
}
open my $o, '>:encoding(UTF-8)', "$out/retail_2023.csv" or die $!;
print $o "# retail_2023.csv - GENERATED by Tools/energy_fiscal_prep.pl (EN-4). Per country and customer class: 2023 consumption (GWh) and the retail price's components per kWh in the country's currency.\n";
print $o "# DE FR IT PL SE: Eurostat nrg_pc_204_c (households, band DC 2 500-4 999 kWh) and nrg_pc_205_c (non-households, band IC 500-1 999 MWh), EUR/kWh, annual 2023 - energy_supply = NRG_SUP; network = NETC; policy = TAX_RNW + TAX_CAP + OTH (the levies and charges that fund schemes); tax_env = TAX_ENV + TAX_NUC; vat = VAT; total = NRG_SUP + NETC + TAX_FEE_LEV_CHRG. Consumption from nrg_cb_e: households = FC_OTH_HH_E, non-households = FC - households.\n";
print $o "# US: EIA Electric Power Annual Table 2.4 (average price by sector, cents/kWh, 2023) and Table 2.2 (sales by sector, MWh) - households = residential; non-households = commercial + industrial + transportation, sales-weighted; the price is all-in under energy_supply and its components are BILLED (EIA-861 by class); taxes 0 (state and local, unsourced).\n";
print $o "country;class;consumption_gwh;energy_supply;network;policy;tax_env;vat;total\n";
for my $cc (@eu) {
    for my $class (qw(households nonhousehold)) {
        my $c = $comp{$cc}{$class}; my $cons = $class eq 'households' ? $hh{$cc} : $fc{$cc} - $hh{$cc};
        my $policy = ($c->{TAX_RNW} // 0) + ($c->{TAX_CAP} // 0) + ($c->{OTH} // 0); my $env = ($c->{TAX_ENV} // 0) + ($c->{TAX_NUC} // 0); my $vat = $c->{VAT} // 0;
        my $total = ($c->{NRG_SUP} // 0) + ($c->{NETC} // 0) + ($c->{TAX_FEE_LEV_CHRG} // 0);
        my $parts = ($c->{NRG_SUP} // 0) + ($c->{NETC} // 0) + $policy + $env + $vat;
        die "$cc $class: the tax components ($parts) do not sum to the total ($total)" if abs($parts - $total) > 0.0002;
        print $o join(';', $cc, $class, f1($cons), f4($c->{NRG_SUP} // 0), f4($c->{NETC} // 0), f4($policy), f4($env), f4($vat), f4($total)), "\n";
    }
}
{
    my $hhCons = $usSales{res} / 1000; my $nhCons = ($usSales{com} + $usSales{ind} + $usSales{tra}) / 1000;
    my $nhPrice = ($usPrice{com} * $usSales{com} + $usPrice{ind} * $usSales{ind} + $usPrice{tra} * $usSales{tra}) / ($usSales{com} + $usSales{ind} + $usSales{tra}) / 100;
    print $o join(';', 'US', 'households', f1($hhCons), f4($usPrice{res} / 100), f4(0), f4(0), f4(0), f4(0), f4($usPrice{res} / 100)), "\n";
    print $o join(';', 'US', 'nonhousehold', f1($nhCons), f4($nhPrice), f4(0), f4(0), f4(0), f4(0), f4($nhPrice)), "\n";
}
close $o;
print "energy_fiscal_prep: wrote retail_2023.csv to $out\n";
