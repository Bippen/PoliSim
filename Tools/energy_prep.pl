#!/usr/bin/perl
# energy_prep.pl <sources-dir> <out-dir>
#
# Stage 2 of the energy track (POLISIM_ENERGY_SPECLET.md, ruled §454; ENERGY_LAYER_SPINE.md §455): the physical layer's
# data files, DERIVED from the sources on disk - never typed. Every input is a file the spine lists by digest under
# PoliSim-captures/sources/energy/. The outputs under EnergyData/ are what EnergyCatalogGenerator reads into the runtime
# assembly; GeneratedCatalogCheck holds the catalog to these files' digests.
#
#   fleet_2023.csv        capacity and generation by the family's seven labels, the office of record beside Ember
#   load_blocks_2023.csv  the spec-let's three blocks and the peak, from the hourly load (S3: the P90 cut)
#   zones_2023.csv        Sweden's four bidding zones: settlement consumption and production by type (eSett)
#   links.csv             the six directed links between SE1..SE4 (Svenska kraftnät, 2024-09-30 report, Table 1)
#   combustion_2023.csv   CO2 of the fuel burned for electricity and heat, by label and plant class (IPCC 2006 defaults)
#   country_2023.csv      population, the CHP electricity share of output, inland demand
#
# Sources of record per country (the definition, named - spine §2): Eurostat nrg_inf_epc CAP_NET_ELC by main fuel group
# and nrg_bal_peh GEP by fuel for the five; EIA EPA Table 4.3 (2023 edition, net summer) and EPM Table 1.1 / 1.1.A for the USA.
use strict; use warnings; use utf8;
use JSON::PP; use POSIX qw(floor);
use IO::Uncompress::Unzip qw(unzip $UnzipError);
binmode STDOUT, ':encoding(UTF-8)';

my ($src, $out) = @ARGV; die "usage: energy_prep.pl <sources-dir> <out-dir>\n" unless $src && $out;
mkdir $out unless -d $out;
my @labels = qw(coal gas nuclear hydro wind solar other);
my @eu = qw(DE FR IT PL SE);
my %country = (DE => 'Germany', FR => 'France', IT => 'Italy', PL => 'Poland', SE => 'Sweden', US => 'United States');
# World Bank SP.POP.TOTL 2023, millions - the divisor of the family's per-head seeds (ENVIRONMENT_FAMILY_SPINE.md:7).
my %population = (DE => 83.29, FR => 68.37, IT => 58.98, PL => 36.69, SE => 10.54, US => 336.76);

# ---------------------------------------------------------------- readers
sub slurp { my ($p) = @_; open my $f, '<:raw', $p or die "open $p: $!"; local $/; my $t = <$f>; close $f; return $t; }
sub jsonstat_rows {   # Eurostat JSON-stat 2.0 -> list of { dim => code, ..., value => v }
    my ($path) = @_;
    my $j = JSON::PP->new->utf8->decode(slurp($path));
    my @ids = @{ $j->{id} }; my @size = @{ $j->{size} };
    my @codes;
    for my $i (0..$#ids) { my $idx = $j->{dimension}{$ids[$i]}{category}{index}; $codes[$i] = ref $idx eq 'HASH' ? [ sort { $idx->{$a} <=> $idx->{$b} } keys %$idx ] : $idx; }
    my @stride; my $s = 1; for (my $i = $#ids; $i >= 0; $i--) { $stride[$i] = $s; $s *= $size[$i]; }
    my @rows;
    for my $lin (keys %{ $j->{value} }) {
        my %r = (value => $j->{value}{$lin});
        for my $i (0..$#ids) { $r{$ids[$i]} = $codes[$i][ int($lin / $stride[$i]) % $size[$i] ]; }
        push @rows, \%r;
    }
    return @rows;
}
sub csv_split { my ($s) = @_; my @o; my $c = ''; my $q = 0; for my $ch (split //, $s) { if ($ch eq '"') { $q = !$q; next; } if ($ch eq ',' && !$q) { push @o, $c; $c = ''; next; } $c .= $ch; } push @o, $c; return @o; }
sub xlsx_sheet_rows {   # rows of cell values from sheet N of an xlsx given as bytes
    my ($bytes, $sheet) = @_;
    my $ss = ''; unzip \$bytes => \$ss, Name => 'xl/sharedStrings.xml' or $ss = '';
    utf8::decode($ss);
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
            $v =~ s/[\t\r\n]+/ /g;
            my $n = 0; for my $ch (split //, $col) { $n = $n * 26 + ord($ch) - 64; }
            $cells[$n - 1] = $v; $max = $n if $n > $max;
        }
        push @rows, [ map { defined $_ ? $_ : '' } @cells[0..$max-1] ];
    }
    return @rows;
}
sub num { my $s = shift // ''; $s =~ s/,//g; return ($s =~ /^-?\d+(\.\d+)?([eE][-+]?\d+)?$/) ? $s + 0 : undef; }
sub f1 { sprintf('%.1f', $_[0]) } sub f0 { sprintf('%.0f', $_[0]) } sub f4 { sprintf('%.4f', $_[0]) }

# ---------------------------------------------------------------- 1. the fleet
my (%cap, %gen);   # {cc}{label}
# Eurostat capacities: CAP_NET_ELC, operator TOTAL. coal = C0000 (coal AND manufactured gases - the group is not split), gas = G3000,
# nuclear = N9000, hydro = RA100 (pumped included), wind = RA300, solar = RA410 + RA420 (AC), other = TOTAL - the six.
{
    my %v;
    for my $r (jsonstat_rows("$src/eurostat_nrg_inf_epc_2023.json")) { next unless $r->{plant_tec} eq 'CAP_NET_ELC' && $r->{operator} eq 'TOTAL' && $r->{time} eq '2023'; $v{$r->{geo}}{$r->{siec}} = $r->{value}; }
    for my $cc (@eu) {
        my $t = $v{$cc}; my %c = (coal => $t->{C0000} // 0, gas => $t->{G3000} // 0, nuclear => $t->{N9000} // 0, hydro => $t->{RA100} // 0, wind => $t->{RA300} // 0, solar => ($t->{RA410} // 0) + ($t->{RA420} // 0));
        my $six = 0; $six += $c{$_} for keys %c; $c{other} = ($t->{TOTAL} // 0) - $six;
        $cap{$cc} = \%c;
    }
}
# Eurostat generation: GEP by fuel, GWh. coal = solid fossil fuels (C0000X0350-0370), gas = G3000, nuclear = N900H, hydro = RA100,
# wind = RA300, solar = RA410 + RA420, other = TOTAL - the six (bioenergy, wastes, oil, manufactured gases, geothermal, tide).
my %gepByProducer;   # for the CHP split
{
    my %v;
    for my $r (jsonstat_rows("$src/eurostat_nrg_bal_peh_2023.json")) {
        next unless $r->{unit} eq 'GWH' && $r->{time} eq '2023';
        $v{$r->{geo}}{$r->{siec}} = $r->{value} if $r->{nrg_bal} eq 'GEP';
        $gepByProducer{$r->{geo}}{$r->{nrg_bal}} = $r->{value} if $r->{siec} eq 'TOTAL';
    }
    for my $cc (@eu) {
        my $t = $v{$cc}; my %g = (coal => $t->{'C0000X0350-0370'} // 0, gas => $t->{G3000} // 0, nuclear => $t->{N900H} // 0, hydro => $t->{RA100} // 0, wind => $t->{RA300} // 0, solar => ($t->{RA410} // 0) + ($t->{RA420} // 0));
        my $six = 0; $six += $g{$_} for keys %g; $g{other} = ($t->{TOTAL} // 0) - $six;
        $gen{$cc} = \%g;
    }
}
# The USA. Capacity: EPA Table 4.3, 2023 edition, net summer MW (hydro = conventional + pumped storage, the Eurostat RA100 basis;
# solar = utility PV + thermal + small-scale PV, the table's own "Estimated Total Solar"; other = the rest of the utility total).
{
    my @rows = xlsx_sheet_rows(slurp("$src/eia_epa_04_03_2023edition.xlsx"), 1);
    my %t; for my $r (@rows) { my $v = num($r->[4]); $t{$r->[0]} = $v if defined $v && $r->[0]; }
    my %c = (coal => $t{'Coal'}, gas => $t{'Natural Gas'}, nuclear => $t{'Nuclear'}, hydro => $t{'Hydroelectric Conventional'} + $t{'Hydroelectric Pumped Storage'}, wind => $t{'Wind'}, solar => $t{'Estimated Total Solar'});
    my $sixCap = 0; $sixCap += $c{$_} for keys %c; $c{other} = $t{'Total'} + $t{'Small Scale Photovoltaic'} - $sixCap;
    $cap{US} = \%c;
    # Generation: EPM Table 1.1 (2023 annual, thousand MWh = GWh) and 1.1.A for the renewable detail; hydro net of pumped storage;
    # solar = utility PV + thermal + small-scale; total = utility total + small-scale PV.
    my @t11 = xlsx_sheet_rows(slurp("$src/eia_epm_table_1_01.xlsx"), 1);
    my @t11a = xlsx_sheet_rows(slurp("$src/eia_epm_table_1_01_a.xlsx"), 1);
    my ($r11) = grep { ($_->[0] // '') eq '2023' } @t11; my ($r11a) = grep { ($_->[0] // '') eq '2023' } @t11a;
    die "EPM 1.1 2023 row" unless $r11 && $r11a;
    # 1.1 columns: Coal, Petroleum Liquids, Petroleum Coke, Natural Gas, Other Fossil Gas, Nuclear, Hydro Conventional, Solar, Renewables excl hydro & solar, Pumped Storage, Other, Utility total, Small-scale PV, ...
    my %g = (coal => num($r11->[1]), gas => num($r11->[4]), nuclear => num($r11->[6]), hydro => num($r11->[7]) + num($r11->[10]), wind => num($r11a->[1]), solar => num($r11a->[2]) + num($r11a->[3]) + num($r11a->[11]));
    my $total = num($r11->[12]) + num($r11->[13]);
    my $sixGen = 0; $sixGen += $g{$_} for keys %g; $g{other} = $total - $sixGen;
    $gen{US} = \%g;
}
# Ember beside it (the release's capacity GW and generation TWh; other = bioenergy + other renewables + other fossil).
my (%emberCap, %emberGen);
{
    open my $f, '<:encoding(UTF-8)', "$src/ember_2023_six_countries.csv" or die $!; my $h = <$f>; $h =~ s/^\x{FEFF}//; my @hd = csv_split($h); chomp @hd;
    my %ix; $ix{$hd[$_]} = $_ for 0..$#hd;
    my %map = (Coal => 'coal', Gas => 'gas', Nuclear => 'nuclear', Hydro => 'hydro', Wind => 'wind', Solar => 'solar', Bioenergy => 'other', 'Other renewables' => 'other', 'Other fossil' => 'other');
    my %cc = reverse %country;
    while (my $l = <$f>) { chomp $l; my @c = csv_split($l); my $cc = $cc{$c[$ix{Area}]} or next; my $lab = $map{$c[$ix{'Electricity source'}]} or next;
        $emberCap{$cc}{$lab} += (num($c[$ix{'Capacity (GW)'}]) // 0) * 1000; $emberGen{$cc}{$lab} += (num($c[$ix{'Generation (TWh)'}]) // 0) * 1000; }
    close $f;
}
{
    open my $o, '>:encoding(UTF-8)', "$out/fleet_2023.csv" or die $!;
    print $o "# fleet_2023.csv - GENERATED by Tools/energy_prep.pl from PoliSim-captures/sources/energy (ENERGY_LAYER_SPINE.md §1-§2). Do not edit by hand.\n";
    print $o "# capacity_record_mw: DE FR IT PL SE = Eurostat nrg_inf_epc CAP_NET_ELC 2023, all operators, by main fuel group (coal = 'coal and manufactured gases', hydro incl. pumped, solar AC; multi-fuel groups in other);\n";
    print $o "#   US = EIA Electric Power Annual Table 4.3 (2023 edition), net summer capacity (hydro = conventional + pumped; solar = utility PV + thermal + small-scale). THE DEFINITION IS THE OFFICE OF RECORD'S; Ember's nameplate stands beside it, never averaged in.\n";
    print $o "# generation_record_gwh: DE FR IT PL SE = Eurostat nrg_bal_peh GEP 2023 (coal = solid fossil fuels; solar = thermal + PV; other = the remainder); US = EIA EPM Table 1.1 / 1.1.A, 2023 annual (hydro net of pumping; solar incl. small-scale).\n";
    print $o "# capacity_ember_mw / generation_ember_gwh: Ember yearly release, 2023 (other = bioenergy + other renewables + other fossil).\n";
    print $o "country;label;capacity_record_mw;capacity_ember_mw;generation_record_gwh;generation_ember_gwh\n";
    for my $cc (@eu, 'US') { for my $l (@labels) { print $o join(';', $cc, $l, f1($cap{$cc}{$l}), f1($emberCap{$cc}{$l} // 0), f1($gen{$cc}{$l}), f1($emberGen{$cc}{$l} // 0)), "\n"; } }
    close $o;
}

# ---------------------------------------------------------------- 2. the load blocks
sub fold {   # the spec-let's S3: base = the minimum every hour; peak = the periods above the P90 value; mid = the rest
    my ($mw, $step) = @_; my $n = @$mw; die "empty" unless $n;
    my @s = sort { $b <=> $a } @$mw; my $sum = 0; $sum += $_ for @s;
    my $p90 = $s[ floor(0.10 * $n) ]; my @peak = grep { $_ > $p90 } @s; my @rest = grep { $_ <= $p90 } @s;
    my $pm = 0; $pm += $_ for @peak; $pm /= (@peak || 1); my $rm = 0; $rm += $_ for @rest; $rm /= (@rest || 1);
    return { periods => $n, step => $step, hours => $n * $step, energy_gwh => $sum * $step / 1000, min => $s[-1], mean => $sum / $n, max => $s[0], p90 => $p90,
             peak_h => @peak * $step, peak_mean => $pm, mid_h => @rest * $step, mid_mean => $rm };
}
my %inland;   # Eurostat nrg_cb_e inland demand, GWh
for my $r (jsonstat_rows("$src/eurostat_nrg_cb_e_2023.json")) { $inland{$r->{geo}} = $r->{value} if $r->{nrg_bal} eq 'ID' && $r->{unit} eq 'GWH' && $r->{time} eq '2023'; }
my @blocks;
for my $cc (@eu) {   # energy-charts.info public_power, Load, calendar 2023 in CET (+1)
    my $j = JSON::PP->new->utf8->decode(slurp("$src/energycharts_public_power_" . lc($cc) . "_2023.json"));
    my @t = @{ $j->{unix_seconds} }; my ($load) = grep { $_->{name} eq 'Load' } @{ $j->{production_types} };
    my $step = ($t[1] - $t[0]) / 3600; my @mw;
    for my $i (0..$#t) { my @g = gmtime($t[$i] + 3600); next unless $g[5] + 1900 == 2023; my $v = $load->{data}[$i]; push @mw, $v + 0 if defined $v; }
    my $f = fold(\@mw, $step); $f->{zone} = $cc; $f->{cc} = $cc; $f->{inland} = $inland{$cc}; push @blocks, $f;
}
{   # EIA-930: Demand (MW) (Adjusted) summed over the balancing authorities per UTC hour; hours with fewer than 45 reporting dropped (the file edges)
    my (%sum, %n);
    for my $file ("$src/EIA930_BALANCE_2023_Jan_Jun.csv", "$src/EIA930_BALANCE_2023_Jul_Dec.csv") {
        open my $fh, '<', $file or die $!; my $head = <$fh>; $head =~ s/\r?\n$//; my @h = csv_split($head);
        my ($iUtc) = grep { $h[$_] eq 'UTC Time at End of Hour' } 0..$#h; my ($iDem) = grep { $h[$_] eq 'Demand (MW) (Adjusted)' } 0..$#h;
        while (my $line = <$fh>) { $line =~ s/\r?\n$//; my @f = csv_split($line); my $d = num($f[$iDem]); next unless defined $d; $sum{$f[$iUtc]} += $d; $n{$f[$iUtc]}++; }
        close $fh;
    }
    my @mw = map { $sum{$_} } grep { $n{$_} >= 45 && $_ =~ m{/2023 } } keys %sum;
    my $f = fold(\@mw, 1); $f->{zone} = 'US'; $f->{cc} = 'US'; $f->{inland} = undef; push @blocks, $f;
}
my %esett = (SE1 => '10Y1001A1001A44P', SE2 => '10Y1001A1001A45N', SE3 => '10Y1001A1001A46L', SE4 => '10Y1001A1001A47J');
my (%zoneCons, %zoneProd);
for my $z (sort keys %esett) {   # eSett EXP15 settlement consumption (negative sign convention), hourly; EXP16 production by type
    my $j = JSON::PP->new->utf8->decode(slurp("$src/esett_EXP15_${z}_2023.json"));
    my @mw = map { abs($_->{total} + 0) } grep { defined $_->{total} } @$j;
    my $f = fold(\@mw, 1); $f->{zone} = $z; $f->{cc} = 'SE'; $f->{inland} = undef; push @blocks, $f;
    $zoneCons{$z} += $_ for @mw;
    my $p = JSON::PP->new->utf8->decode(slurp("$src/esett_EXP16_${z}_2023.json"));
    for my $r (@$p) { for my $k (qw(hydro nuclear wind windOffshore solar thermal energyStorage other total)) { $zoneProd{$z}{$k} += ($r->{$k} // 0); } }
}
{
    open my $o, '>:encoding(UTF-8)', "$out/load_blocks_2023.csv" or die $!;
    print $o "# load_blocks_2023.csv - GENERATED by Tools/energy_prep.pl (ENERGY_LAYER_SPINE.md §3). The spec-let's S3 fold: base = the minimum every hour; peak = the periods above the P90 value (the value exceeded by ten per cent of them), at their mean; mid = the rest.\n";
    print $o "# series: DE FR IT PL SE = ENTSO-E actual total load republished by energy-charts.info (public_power, Load), calendar 2023 CET; US = EIA-930 Demand (MW) (Adjusted) summed over the balancing authorities per UTC hour of 2023, edge hours (<45 authorities) dropped; SE1..SE4 = eSett EXP15 settlement consumption.\n";
    print $o "# inland_demand_gwh = Eurostat nrg_cb_e ID 2023; scale = min(1, inland demand / the series' energy) - the factor the build applies where the load series exceeds the balance (Poland).\n";
    print $o "zone;country;periods;step_h;hours;energy_gwh;min_mw;mean_mw;max_mw;p90_mw;peak_h;peak_mean_mw;mid_h;mid_mean_mw;inland_demand_gwh;scale\n";
    for my $f (@blocks) {
        my $scale = defined $f->{inland} && $f->{energy_gwh} > $f->{inland} ? $f->{inland} / $f->{energy_gwh} : 1;
        print $o join(';', $f->{zone}, $f->{cc}, $f->{periods}, $f->{step}, f0($f->{hours}), f1($f->{energy_gwh}), f0($f->{min}), f0($f->{mean}), f0($f->{max}), f0($f->{p90}), f0($f->{peak_h}), f0($f->{peak_mean}), f0($f->{mid_h}), f0($f->{mid_mean}), defined $f->{inland} ? f1($f->{inland}) : '', f4($scale)), "\n";
    }
    close $o;
    open my $z, '>:encoding(UTF-8)', "$out/zones_2023.csv" or die $!;
    print $z "# zones_2023.csv - GENERATED by Tools/energy_prep.pl (ENERGY_LAYER_SPINE.md §4). eSett Open Data 2023: EXP15 settlement consumption and EXP16 settlement production by type per market balance area, GWh. thermal is eSett's own class; other = energyStorage + other.\n";
    print $z "zone;country;consumption_gwh;hydro_gwh;nuclear_gwh;wind_gwh;solar_gwh;thermal_gwh;other_gwh;total_gwh\n";
    for my $zn (sort keys %esett) { my $p = $zoneProd{$zn}; print $z join(';', $zn, 'SE', f1($zoneCons{$zn} / 1000), f1($p->{hydro} / 1000), f1($p->{nuclear} / 1000), f1(($p->{wind} + $p->{windOffshore}) / 1000), f1($p->{solar} / 1000), f1($p->{thermal} / 1000), f1(($p->{energyStorage} + $p->{other}) / 1000), f1($p->{total} / 1000)), "\n"; }
    close $z;
}

# ---------------------------------------------------------------- 3. the links (read off the two Svenska kraftnät documents, spine §4)
{
    open my $o, '>:encoding(UTF-8)', "$out/links.csv" or die $!;
    print $o "# links.csv - the six directed links between Sweden's bidding zones. ntc_mw = 'Maximal tilldelad NTC 2021-2023', Svenska kraftnät, 'Mål för ökning av överföringskapaciteten mellan Sveriges elområden', Svk 2023/2801, 2024-09-30, Table 1 (svk_mal_overforingskapacitet_slutrapport_2024.pdf, sha256 79c9000cb924b450...).\n";
    print $o "# map_max_mw = the network's maximum with no operational-security limit, Svk 'Information karta överföringskapacitet' (svk_textforklaring_karta_overforingskapacitet.pdf, sha256 5b5241379b9659b2...). The dated report's column seeds; the map's is the ceiling. Both documents agree on snitt 1 and 2.\n";
    print $o "from;to;ntc_mw;map_max_mw\n";
    print $o "SE1;SE2;3300;3300\nSE2;SE1;3300;3300\nSE2;SE3;7300;7300\nSE3;SE2;7300;7300\nSE3;SE4;5600;6200\nSE4;SE3;2800;2500\n";
    close $o;
}

# ---------------------------------------------------------------- 4. combustion CO2 (IPCC 2006 Vol. 2 Ch. 2 Table 2.2 defaults, kg CO2 per TJ, NCV)
my %factor = (C0110 => 98300, C0121 => 94600, C0129 => 94600, C0210 => 96100, C0220 => 101000, C0311 => 107000, C0312 => 107000, C0320 => 97500, C0330 => 97500, C0340 => 80700,
              C0350 => 44400, C0360 => 44400, C0371 => 260000, C0379 => 260000, G3000 => 56100, O4100_TOT => 73300, O4200 => 64200, O4610 => 57600, O4630 => 63100, O4640 => 73300,
              O4661XR5230B => 71500, O4669 => 71900, O4671XR5220B => 74100, O4680 => 77400, O4694 => 97500, O4695 => 80700, O4699 => 73300, P1100 => 106000, P1200 => 106000, S2000 => 107000,
              W6100 => 143000, W6220 => 91700);
my %labelOf = map { $_ => 'other' } keys %factor;
$labelOf{$_} = 'coal' for qw(C0110 C0121 C0129 C0210 C0220 C0311 C0312 C0320 C0330 C0340);
$labelOf{G3000} = 'gas';
my %co2; my %tj;   # {cc}{label}{class}
{
    my %class = (TI_EHG_MAPE_E => 'MAPE', TI_EHG_MAPCHP_E => 'MAPCHP', TI_EHG_MAPH_E => 'MAPH', TI_EHG_APE_E => 'APE', TI_EHG_APCHP_E => 'APCHP', TI_EHG_APH_E => 'APH');
    for my $r (jsonstat_rows("$src/eurostat_nrg_bal_c_2023_TI_EHG.json")) {
        next unless $r->{unit} eq 'TJ' && $r->{time} eq '2023'; my $cl = $class{$r->{nrg_bal}} or next; my $f = $factor{$r->{siec}} or next;
        $tj{$r->{geo}}{$labelOf{$r->{siec}}}{$cl} += $r->{value}; $co2{$r->{geo}}{$labelOf{$r->{siec}}}{$cl} += $r->{value} * $f / 1e6;   # kt
    }
}
{   # the USA: EIA-923 Page 1, 'Elec Fuel Consumption MMBtu' (the fuel EIA attributes to electricity) and 'Total Fuel Consumption MMBtu' (CHP heat in the difference), by fuel code
    my $zip = slurp("$src/f923_2023.zip"); my $xlsx; unzip \$zip => \$xlsx, Name => 'EIA923_Schedules_2_3_4_5_M_12_2023_Final_Revision.xlsx' or die "923: $UnzipError";
    my @rows = xlsx_sheet_rows($xlsx, 1);
    my ($hi) = grep { join(' ', @{ $rows[$_] }) =~ /Reported\s*Fuel\s*Type\s*Code/ } 0..$#rows; die "923 header" unless defined $hi;
    my @h = @{ $rows[$hi] }; my ($iFuel) = grep { ($h[$_] // '') =~ /Reported\s*Fuel\s*Type\s*Code/ } 0..$#h; my ($iElec) = grep { ($h[$_] // '') =~ /Elec\s*Fuel\s*Consumption\s*MMBtu/i } 0..$#h; my ($iTot) = grep { ($h[$_] // '') =~ /Total\s*Fuel\s*Consumption\s*MMBtu/i } 0..$#h;
    my ($iSec) = grep { ($h[$_] // '') =~ /Sector\s*Number/i } 0..$#h; die "923 columns" unless defined $iFuel && defined $iElec && defined $iTot && defined $iSec;
    # fuel codes -> IPCC factor (kg/TJ) and label; biogenic and non-combustion codes excluded
    my %us = (BIT => [94600, 'coal'], SUB => [96100, 'coal'], LIG => [101000, 'coal'], ANT => [98300, 'coal'], WC => [94600, 'coal'], RC => [94600, 'coal'], SGC => [94600, 'coal'],
              NG => [56100, 'gas'], DFO => [74100, 'other'], RFO => [77400, 'other'], JF => [71500, 'other'], KER => [71900, 'other'], PC => [97500, 'other'], WO => [73300, 'other'],
              OG => [57600, 'other'], BFG => [260000, 'other'], SGP => [57600, 'other'], PG => [63100, 'other'], TDF => [73300, 'other'], MSN => [91700, 'other']);
    for my $r (@rows[$hi+1..$#rows]) { my $code = $r->[$iFuel] // ''; my $u = $us{$code} or next; my $e = num($r->[$iElec]) // 0; my $t = num($r->[$iTot]) // 0;
        my $eTJ = $e * 1.055056e-3; my $hTJ = ($t - $e) * 1.055056e-3; $hTJ = 0 if $hTJ < 0;
        my $sec = int(num($r->[$iSec]) // 0); my $main = $sec >= 1 && $sec <= 3; my $chp = ($sec == 3 || $sec == 5 || $sec == 7);
        my $eCl = $main ? ($chp ? 'MAPCHP' : 'MAPE') : ($chp ? 'APCHP' : 'APE'); my $hCl = $main ? 'MAPH' : 'APH';
        $tj{US}{$u->[1]}{$eCl} += $eTJ; $co2{US}{$u->[1]}{$eCl} += $eTJ * $u->[0] / 1e6; $tj{US}{$u->[1]}{$hCl} += $hTJ; $co2{US}{$u->[1]}{$hCl} += $hTJ * $u->[0] / 1e6; }
}
{
    open my $o, '>:encoding(UTF-8)', "$out/combustion_2023.csv" or die $!;
    print $o "# combustion_2023.csv - GENERATED by Tools/energy_prep.pl (ENERGY_LAYER_SPINE.md §9). CO2 of the fossil fuel burned for electricity and heat, 2023, kt, by the family's label (coal = solid fossil fuels; gas = natural gas; other = manufactured gases, oil products, peat, oil shale, non-renewable wastes) and plant class.\n";
    print $o "# DE FR IT PL SE: Eurostat nrg_bal_c transformation inputs by fuel, TJ, in six classes - MAPE main-activity electricity-only, MAPCHP main-activity CHP, MAPH main-activity heat-only, APE / APCHP / APH the autoproducers' three; x IPCC 2006 Vol.2 Ch.2 Table 2.2 default CO2 factors (kg/TJ, NCV). Biogenic fuels excluded (EDGAR's basis).\n";
    print $o "# WHY THE OPERATOR SPLIT: EDGAR's Power Industry is IPCC 1A1 - the MAIN-ACTIVITY producers (plus refineries and other energy industries); autoproducers' emissions sit in industry's and services' own categories. So the derived part of the family's figure is MAPE + MAPCHP's electricity share; MAPH and MAPCHP's heat share are the known part of the residual; the autoproducer classes are computed and printed but belong to a book this model does not keep.\n";
    print $o "# US: EIA-923 2023 Page 1 by 'Sector Number' - 1 electric utility, 2 IPP non-CHP, 3 IPP CHP are main activity (MAPE, MAPCHP), 4-7 commercial and industrial are autoproducers (APE, APCHP); 'Elec Fuel Consumption MMBtu' is the fuel EIA itself attributes to electricity, so the CHP electricity share is already applied (MAPH / APH carry 'Total' minus 'Elec', the heat); by reported fuel type code x the same factors (TDF at the waste-oil default).\n";
    print $o "country;label;class;fuel_tj;co2_kt\n";
    for my $cc (@eu, 'US') { for my $l (qw(coal gas other)) { for my $cl (qw(MAPE MAPCHP MAPH APE APCHP APH)) { $tj{$cc}{$l}{$cl} //= 0; $co2{$cc}{$l}{$cl} //= 0; print $o join(';', $cc, $l, $cl, f0($tj{$cc}{$l}{$cl}), f1($co2{$cc}{$l}{$cl})), "\n"; } } }
    close $o;
    open my $c, '>:encoding(UTF-8)', "$out/country_2023.csv" or die $!;
    print $c "# country_2023.csv - GENERATED by Tools/energy_prep.pl. population_m = World Bank SP.POP.TOTL 2023, millions (the divisor of EnvironmentSeeds' per-head figures, ENVIRONMENT_FAMILY_SPINE.md:7);\n";
    print $c "# chp_share_main / chp_share_auto = CHP plants' gross electricity over their gross electricity + heat output, main-activity and autoproducer separately, Eurostat nrg_bal_peh 2023 (GEP_MAPCHP / (GEP_MAPCHP + GHP_MAPCHP); the same for AP) - the key that splits CHP fuel between electricity and heat (the USA's split is EIA's own in EIA-923, so both shares are 1);\n";
    print $c "# inland_demand_gwh = Eurostat nrg_cb_e ID 2023 (the USA: EIA EPM Table 1.1 utility + small-scale net generation, the only national total on disk).\n";
    print $c "country;population_m;chp_share_main;chp_share_auto;inland_demand_gwh\n";
    for my $cc (@eu, 'US') {
        my ($main, $auto) = (1, 1);
        if ($cc ne 'US') { my $g = $gepByProducer{$cc}; my $e = $g->{GEP_MAPCHP} // 0; my $h = $g->{GHP_MAPCHP} // 0; $main = $e / (($e + $h) || 1); my $ea = $g->{GEP_APCHP} // 0; my $ha = $g->{GHP_APCHP} // 0; $auto = $ea / (($ea + $ha) || 1); }
        my $id = $cc eq 'US' ? do { my $t = 0; $t += $gen{US}{$_} for @labels; $t } : $inland{$cc};
        print $c join(';', $cc, sprintf('%.2f', $population{$cc}), f4($main), f4($auto), f1($id)), "\n";
    }
    close $c;
}
print "energy_prep: wrote fleet_2023.csv, load_blocks_2023.csv, zones_2023.csv, links.csv, combustion_2023.csv, country_2023.csv to $out\n";
