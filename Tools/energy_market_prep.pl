#!/usr/bin/perl
# energy_market_prep.pl <sources-dir> <out-dir>
#
# Stage 3 of the energy track (EN-3, the market layer; POLISIM_ENERGY_SPECLET.md S5-S6, ruled §454): the clearing's data files,
# DERIVED from the sources on disk - never typed. Adds to stage 2's EnergyData/:
#
#   dispatch_levels_2023.csv   per zone × block × category, the 2023 output level (MW) in the block's hours, the block's demand
#                              (the total generation level), and for Sweden's zones the settlement consumption and the external export
#   variable_costs_2023.csv    per country × fossil category: fuel price, implied efficiency, emission factor, variable O&M, availability,
#                              the main-activity share, the fossil category's capacity share - the merit order's inputs
#   external_links_se.csv      Sweden's interconnectors by zone (Svenska kraftnät's capacity-map text), the apportionment key of the exchange
#
# The block hours are the SAME fold as stage 2's (the load sorted, base | mid | peak at the P90 cut) - for the countries their own load,
# for SE1..SE4 the NATIONAL load's hours, so the four zones clear on coincident hours.
use strict; use warnings; use utf8;
use JSON::PP; use POSIX qw(floor);
use IO::Uncompress::Unzip qw(unzip $UnzipError);
binmode STDOUT, ':encoding(UTF-8)';

my ($src, $out) = @ARGV; die "usage: energy_market_prep.pl <sources-dir> <out-dir>\n" unless $src && $out;
my @eu = qw(DE FR IT PL SE);
my @cats = qw(coal gas oil nuclear hydro wind solar firm);
my @blocks = qw(base mid peak);

sub slurp { my ($p) = @_; open my $f, '<:raw', $p or die "open $p: $!"; local $/; my $t = <$f>; close $f; return $t; }
sub jsonstat_rows {
    my ($path) = @_; my $j = JSON::PP->new->utf8->decode(slurp($path));
    my @ids = @{ $j->{id} }; my @size = @{ $j->{size} }; my @codes;
    for my $i (0..$#ids) { my $idx = $j->{dimension}{$ids[$i]}{category}{index}; $codes[$i] = ref $idx eq 'HASH' ? [ sort { $idx->{$a} <=> $idx->{$b} } keys %$idx ] : $idx; }
    my @stride; my $s = 1; for (my $i = $#ids; $i >= 0; $i--) { $stride[$i] = $s; $s *= $size[$i]; }
    my @rows; for my $lin (keys %{ $j->{value} }) { my %r = (value => $j->{value}{$lin}); for my $i (0..$#ids) { $r{$ids[$i]} = $codes[$i][ int($lin / $stride[$i]) % $size[$i] ]; } push @rows, \%r; }
    return @rows;
}
sub csv_split { my ($s) = @_; my @o; my $c = ''; my $q = 0; for my $ch (split //, $s) { if ($ch eq '"') { $q = !$q; next; } if ($ch eq ',' && !$q) { push @o, $c; $c = ''; next; } $c .= $ch; } push @o, $c; return @o; }
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
sub f1 { sprintf('%.1f', $_[0]) } sub f4 { sprintf('%.4f', $_[0]) } sub f2 { sprintf('%.2f', $_[0]) }

# ---- the clearing's blocks as HOURS: the load sorted; peak = the periods above the P90 value (S3's cut, exactly stage 2's peak block);
# base = the periods at or below the P10 value (the mirror cut - the trough hours, where surplus and curtailment show); mid = the rest.
# A partition of the year, so the three blocks' energies sum to the series. Returns a map period-index -> block.
sub blocks_of {
    my ($mw) = @_; my $n = @$mw; my @idx = sort { $mw->[$b] <=> $mw->[$a] } 0..$n-1;
    my $p90 = $mw->[ $idx[ floor(0.10 * $n) ] ]; my $p10 = $mw->[ $idx[ floor(0.90 * $n) ] ];
    my @b = map { $mw->[$_] > $p90 ? 'peak' : ($mw->[$_] <= $p10 ? 'base' : 'mid') } 0..$n-1;
    return (\@b, $p10, $p90);
}
# ---- levels per block: the mean of a series over the block's own periods
sub level_by_block {
    my ($series, $blockOf) = @_; my (%sum, %n);
    for my $i (0..$#$series) { my $v = $series->[$i]; next unless defined $v; my $b = $blockOf->[$i]; $sum{$b} += $v; $n{$b}++; }
    return map { $_ => ($n{$_} ? $sum{$_} / $n{$_} : 0) } @blocks;
}
sub hours_by_block { my ($blockOf, $step) = @_; my %h; $h{$_} += $step for @$blockOf; return map { $_ => ($h{$_} // 0) } @blocks; }

# ================================================================= 1. dispatch levels
my @levelRows;   # [zone, block, demand, {cat=>mw}, consumption, external_export]
# the five: energy-charts public_power types folded into the categories; demand = the sum of the generating types (no trading, no pumping load)
my %fold = (
    coal => ['Fossil brown coal / lignite', 'Fossil hard coal'], gas => ['Fossil gas'], oil => ['Fossil oil', 'Fossil coal-derived gas'], nuclear => ['Nuclear'],
    hydro => ['Hydro Run-of-River', 'Hydro water reservoir', 'Hydro pumped storage'], wind => ['Wind onshore', 'Wind offshore'], solar => ['Solar'],
    firm => ['Biomass', 'Waste', 'Geothermal', 'Others'],
);
my %seBlockOf; my @seUnix;   # Sweden's national hours, for the zones
for my $cc (@eu) {
    my $j = JSON::PP->new->utf8->decode(slurp("$src/energycharts_public_power_" . lc($cc) . "_2023.json"));
    my @t = @{ $j->{unix_seconds} }; my %type; for my $p (@{ $j->{production_types} }) { $type{$p->{name}} = $p->{data}; }
    my @keep = grep { my @g = gmtime($t[$_] + 3600); $g[5] + 1900 == 2023 } 0..$#t;   # the CET calendar year
    my @load = map { $type{Load}[$_] // 0 } @keep;
    my ($blockOf, $min, $p90) = blocks_of(\@load);
    my %cat;
    for my $c (@cats) { my @s = map { my $i = $_; my $v = 0; for my $name (@{ $fold{$c} }) { $v += ($type{$name}[$i] // 0) if exists $type{$name}; } $v } @keep; my %l = level_by_block(\@s, $blockOf); $cat{$c} = \%l; }
    my @gen = map { my $i = $_; my $v = 0; for my $c (@cats) { for my $name (@{ $fold{$c} }) { $v += ($type{$name}[$i] // 0) if exists $type{$name}; } } $v } @keep;
    my %dem = level_by_block(\@gen, $blockOf); my $step = ($t[1] - $t[0]) / 3600; my %hrs = hours_by_block($blockOf, $step);
    for my $b (@blocks) { push @levelRows, [$cc, $b, $hrs{$b}, $dem{$b}, { map { $_ => $cat{$_}{$b} } @cats }, undef, undef]; }
    if ($cc eq 'SE') { my %m; for my $k (0..$#keep) { $m{ $t[$keep[$k]] } = $blockOf->[$k]; } %seBlockOf = %m; }
}
# the USA: EIA-930 net generation by source (Adjusted), summed over the balancing authorities per UTC hour; blocks by the demand aggregate
{
    my %src930 = (coal => 'Net Generation (MW) from Coal (Adjusted)', gas => 'Net Generation (MW) from Natural Gas (Adjusted)', nuclear => 'Net Generation (MW) from Nuclear (Adjusted)',
        oil => 'Net Generation (MW) from All Petroleum Products (Adjusted)', hydro => 'Net Generation (MW) from Hydropower and Pumped Storage (Adjusted)', solar => 'Net Generation (MW) from Solar (Adjusted)',
        wind => 'Net Generation (MW) from Wind (Adjusted)');
    my @firmCols = ('Net Generation (MW) from Other Fuel Sources (Adjusted)', 'Net Generation (MW) from Unknown Fuel Sources (Adjusted)');
    my (%dem, %n, %g);
    for my $file ("$src/EIA930_BALANCE_2023_Jan_Jun.csv", "$src/EIA930_BALANCE_2023_Jul_Dec.csv") {
        open my $fh, '<', $file or die $!; my $head = <$fh>; $head =~ s/\r?\n$//; my @h = csv_split($head); my %ix; $ix{$h[$_]} = $_ for 0..$#h;
        my $iUtc = $ix{'UTC Time at End of Hour'}; my $iDem = $ix{'Demand (MW) (Adjusted)'};
        while (my $line = <$fh>) { $line =~ s/\r?\n$//; my @f = csv_split($line); my $d = num($f[$iDem]); next unless defined $d; my $t = $f[$iUtc]; next unless $t =~ m{/2023 };
            $dem{$t} += $d; $n{$t}++;
            for my $c (keys %src930) { $g{$t}{$c} += (num($f[$ix{$src930{$c}}]) // 0); }
            for my $col (@firmCols) { $g{$t}{firm} += (num($f[$ix{$col}]) // 0); } }
        close $fh;
    }
    my @hours = grep { $n{$_} >= 45 } keys %dem;
    my @load = map { $dem{$_} } @hours; my ($blockOf) = blocks_of(\@load);
    my %cat; for my $c (@cats) { my @s = map { $g{$_}{$c} // 0 } @hours; my %l = level_by_block(\@s, $blockOf); $cat{$c} = \%l; }
    my @gen = map { my $h = $_; my $v = 0; $v += ($g{$h}{$_} // 0) for @cats; $v } @hours; my %d = level_by_block(\@gen, $blockOf); my %hrs = hours_by_block($blockOf, 1);
    for my $b (@blocks) { push @levelRows, ['US', $b, $hrs{$b}, $d{$b}, { map { $_ => $cat{$_}{$b} } @cats }, undef, undef]; }
}
# Sweden's zones: eSett production by type and consumption per hour (UTC), the exchange by neighbour from energy-charts, all on the NATIONAL hours
my %extShare = (   # each neighbour's flow split over the zones it connects to, by the interconnector capacity out of Sweden (external_links_se.csv)
    Norway => { SE1 => 600, SE2 => 1300, SE3 => 2095 }, Finland => { SE1 => 1500, SE3 => 1200 }, Denmark => { SE3 => 715, SE4 => 1300 }, Germany => { SE4 => 615 }, Poland => { SE4 => 600 }, Lithuania => { SE4 => 700 },
);
my %extOut = (SE1 => {Finland => [1500,1100], Norway => [600,700]}, SE2 => {'Norway (NO4)' => [300,250], 'Norway (NO3)' => [1000,600]}, SE3 => {'Norway (NO1)' => [2095,2145], Finland => [1200,1200], 'Denmark (DK1)' => [715,715]}, SE4 => {'Denmark (DK2)' => [1300,1700], Germany => [615,600], Poland => [600,600], Lithuania => [700,700]});
{
    my $cb = JSON::PP->new->utf8->decode(slurp("$src/energycharts_cbpf_se_2023.json"));
    my @ct = @{ $cb->{unix_seconds} }; my %flow; for my $c (@{ $cb->{countries} }) { $flow{$c->{name}} = $c->{data}; }
    my %zoneExt;   # zone -> block -> sum MW, n
    for my $i (0..$#ct) { my $b = $seBlockOf{$ct[$i]}; next unless defined $b;
        for my $nb (keys %extShare) { my $gw = $flow{$nb}[$i]; next unless defined $gw; my $exportMw = -$gw * 1000;   # energy-charts: negative = export from Sweden; MW
            my $tot = 0; $tot += $_ for values %{ $extShare{$nb} };
            for my $z (keys %{ $extShare{$nb} }) { my $share = $extShare{$nb}{$z} / $tot; $zoneExt{$z}{$b}{sum} += $exportMw * $share; $zoneExt{$z}{all}{sum} += $exportMw * $share; }
        }
        $zoneExt{$_}{$b}{n}++, $zoneExt{$_}{all}{n}++ for qw(SE1 SE2 SE3 SE4);
    }
    my %mba = (SE1 => '10Y1001A1001A44P', SE2 => '10Y1001A1001A45N', SE3 => '10Y1001A1001A46L', SE4 => '10Y1001A1001A47J');
    for my $z (qw(SE1 SE2 SE3 SE4)) {
        my $p = JSON::PP->new->utf8->decode(slurp("$src/esett_EXP16_${z}_2023.json")); my $c = JSON::PP->new->utf8->decode(slurp("$src/esett_EXP15_${z}_2023.json"));
        my %cons; for my $r (@$c) { $cons{$r->{timestampUTC}} = abs($r->{total} // 0); }
        my (%sum, %n);
        for my $r (@$p) {
            my ($y, $mo, $d, $h) = $r->{timestampUTC} =~ /^(\d{4})-(\d\d)-(\d\d)T(\d\d)/;
            my $b = $seBlockOf{ utc_unix($y, $mo, $d, $h) }; next unless defined $b;
            my %v = (hydro => $r->{hydro} // 0, nuclear => $r->{nuclear} // 0, wind => ($r->{wind} // 0) + ($r->{windOffshore} // 0), solar => $r->{solar} // 0, firm => ($r->{thermal} // 0) + ($r->{energyStorage} // 0) + ($r->{other} // 0), coal => 0, gas => 0, oil => 0);
            for my $k (@cats) { $sum{$b}{$k} += $v{$k}; } $sum{$b}{cons} += $cons{$r->{timestampUTC}} // 0; $n{$b}++;
        }
        for my $b (@blocks) { my %cat = map { $_ => ($n{$b} ? $sum{$b}{$_} / $n{$b} : 0) } @cats; my $dem = 0; $dem += $cat{$_} for @cats;
            my $ext = $zoneExt{$z}{$b}{n} ? $zoneExt{$z}{$b}{sum} / $zoneExt{$z}{$b}{n} : 0;
            push @levelRows, [$z, $b, $n{$b} // 0, $dem, \%cat, ($n{$b} ? $sum{$b}{cons} / $n{$b} : 0), $ext]; }
    }
}
# an hour's unix time from its UTC fields (Time::Local's timegm, without the module: the days since the epoch by the civil calendar)
sub utc_unix { my ($y, $mo, $d, $h) = @_; my @cum = (0,31,59,90,120,151,181,212,243,273,304,334); my $leap = ($y % 4 == 0 && ($y % 100 != 0 || $y % 400 == 0)) ? 1 : 0;
    my $days = 0; for my $yy (1970..$y-1) { $days += (($yy % 4 == 0 && ($yy % 100 != 0 || $yy % 400 == 0)) ? 366 : 365); } $days += $cum[$mo-1] + ($mo > 2 ? $leap : 0) + $d - 1; return $days * 86400 + $h * 3600; }
{
    open my $o, '>:encoding(UTF-8)', "$out/dispatch_levels_2023.csv" or die $!;
    print $o "# dispatch_levels_2023.csv - GENERATED by Tools/energy_market_prep.pl (EN-3). Per zone and block: the 2023 output level of each category in the block's hours, MW, and the block's demand = the sum of the generating categories' levels (the balance: consumption + net export + losses + pumping). The base block's level is the year-round mean (the base is carried every hour); mid and peak are the means over their own hours (the load's P90 cut, as stage 2's fold).\n";
    print $o "# DE FR IT PL SE: energy-charts.info public_power by type, folded - coal = lignite + hard coal; gas; oil = fossil oil + coal-derived gas; nuclear; hydro = run-of-river + reservoir + pumped-storage generation; wind on- and offshore; solar; firm = biomass + waste + geothermal + others (must-run, not dispatched). US: EIA-930 net generation by source (Adjusted) summed over the balancing authorities per UTC hour - oil = all petroleum products; firm = other + unknown fuel sources.\n";
    print $o "# SE1..SE4: eSett EXP16 production by type and EXP15 consumption, on the NATIONAL load's block hours so the four zones clear on coincident hours; firm = eSett's thermal + storage + other (Sweden's fossil thermal is heat-led CHP inside it and is not dispatched); consumption_mw = the zone's settlement consumption; external_export_mw = Sweden's exchange with Norway, Finland, Denmark, Germany, Poland and Lithuania (energy-charts cbpf, positive = export) apportioned to the zones by the interconnector capacity out of each (external_links_se.csv).\n";
    print $o "zone;block;hours;demand_mw;" . join(';', map { "${_}_mw" } @cats) . ";consumption_mw;external_export_mw\n";
    for my $r (@levelRows) { my ($z, $b, $hrs, $dem, $cat, $cons, $ext) = @$r; print $o join(';', $z, $b, sprintf('%.0f', $hrs), f1($dem), (map { f1($cat->{$_}) } @cats), defined $cons ? f1($cons) : '', defined $ext ? f1($ext) : ''), "\n"; }
    close $o;
    open my $e, '>:encoding(UTF-8)', "$out/external_links_se.csv" or die $!;
    print $e "# external_links_se.csv - Sweden's interconnectors by zone, MW out of Sweden / into Sweden, read off Svenska kraftnät's 'Information karta överföringskapacitet' (svk_textforklaring_karta_overforingskapacitet.pdf, sha256 5b5241379b9659b2...). The apportionment key of the exchange in dispatch_levels_2023.csv; the exchange itself is exogenous (the neighbours are outside the six).\n";
    print $e "zone;neighbour;out_mw;in_mw\n";
    for my $z (qw(SE1 SE2 SE3 SE4)) { for my $nb (sort keys %{ $extOut{$z} }) { print $e join(';', $z, $nb, @{ $extOut{$z}{$nb} }), "\n"; } }
    close $e;
}

# ================================================================= 2. variable costs
# the fuel groups, by Eurostat siec leaf code -> category (the same map as energy_prep.pl's combustion, oil = oil products + manufactured gases; wastes and peat are firm)
my %factor = (C0110 => 98300, C0121 => 94600, C0129 => 94600, C0210 => 96100, C0220 => 101000, C0311 => 107000, C0312 => 107000, C0320 => 97500, C0330 => 97500, C0340 => 80700,
              C0350 => 44400, C0360 => 44400, C0371 => 260000, C0379 => 260000, G3000 => 56100, O4100_TOT => 73300, O4200 => 64200, O4610 => 57600, O4630 => 63100, O4640 => 73300,
              O4661XR5230B => 71500, O4669 => 71900, O4671XR5220B => 74100, O4680 => 77400, O4694 => 97500, O4695 => 80700, O4699 => 73300);
my %catOf = map { $_ => 'oil' } keys %factor; $catOf{$_} = 'coal' for qw(C0110 C0121 C0129 C0210 C0220 C0311 C0312 C0320 C0330 C0340); $catOf{G3000} = 'gas';
my %lignite = map { $_ => 1 } qw(C0220 C0330);   # lignite and brown-coal briquettes - the untraded coal
my (%tjElc, %co2Elc, %tjLig, %tjCoal);   # {cc}{cat}: fuel to ELECTRICITY (electricity-only + CHP's electricity share), all producers
my %chpShare;   # {cc}{MAP|AP}
{
    for my $r (jsonstat_rows("$src/eurostat_nrg_bal_peh_2023.json")) { next unless $r->{unit} eq 'GWH' && $r->{time} eq '2023' && $r->{siec} eq 'TOTAL'; $chpShare{$r->{geo}}{$r->{nrg_bal}} = $r->{value}; }
    for my $cc (@eu) { my $g = $chpShare{$cc}; my $e = $g->{GEP_MAPCHP} // 0; my $h = $g->{GHP_MAPCHP} // 0; my $ea = $g->{GEP_APCHP} // 0; my $ha = $g->{GHP_APCHP} // 0;
        $chpShare{$cc}{MAP} = $e / (($e + $h) || 1); $chpShare{$cc}{AP} = $ea / (($ea + $ha) || 1); }
    my %elecShare = (TI_EHG_MAPE_E => ['MAP', 1], TI_EHG_APE_E => ['AP', 1], TI_EHG_MAPCHP_E => ['MAP', 'chp'], TI_EHG_APCHP_E => ['AP', 'chp']);
    for my $r (jsonstat_rows("$src/eurostat_nrg_bal_c_2023_TI_EHG.json")) {
        next unless $r->{unit} eq 'TJ' && $r->{time} eq '2023'; my $cl = $elecShare{$r->{nrg_bal}} or next; my $f = $factor{$r->{siec}} or next; my $cc = $r->{geo};
        my $share = $cl->[1] eq 'chp' ? $chpShare{$cc}{$cl->[0]} : 1; my $tj = $r->{value} * $share; my $cat = $catOf{$r->{siec}};
        $tjElc{$cc}{$cat} += $tj; $co2Elc{$cc}{$cat} += $tj * $f / 1e6;
        if ($cat eq 'coal') { $tjCoal{$cc} += $tj; $tjLig{$cc} += $tj if $lignite{$r->{siec}}; }
    }
}
my (%gep, %gepMain);   # {cc}{cat}: gross electricity from the category's fuels, GWh, and the main-activity share of it
{
    my %catOfGep = ('C0000X0350-0370' => 'coal', G3000 => 'gas', O4000XBIO => 'oil', 'C0350-0370' => 'oil');
    for my $r (jsonstat_rows("$src/eurostat_nrg_bal_peh_2023.json")) { next unless $r->{unit} eq 'GWH' && $r->{time} eq '2023'; my $cat = $catOfGep{$r->{siec}} or next;
        $gep{$r->{geo}}{$cat} += $r->{value} if $r->{nrg_bal} eq 'GEP'; $gepMain{$r->{geo}}{$cat} += $r->{value} if $r->{nrg_bal} eq 'GEP_MAPE' || $r->{nrg_bal} eq 'GEP_MAPCHP'; }
}
# the USA: EIA-923 Page 1 - elec fuel MMBtu and net generation MWh by fuel code and sector
{
    my $zip = slurp("$src/f923_2023.zip"); my $xlsx; unzip \$zip => \$xlsx, Name => 'EIA923_Schedules_2_3_4_5_M_12_2023_Final_Revision.xlsx' or die "923: $UnzipError";
    my @rows = xlsx_sheet_rows($xlsx, 1); my ($hi) = grep { join(' ', @{ $rows[$_] }) =~ /Reported\s*Fuel\s*Type\s*Code/ } 0..$#rows; my @h = @{ $rows[$hi] };
    my ($iFuel) = grep { ($h[$_] // '') =~ /Reported\s*Fuel\s*Type\s*Code/ } 0..$#h; my ($iElec) = grep { ($h[$_] // '') =~ /Elec\s*Fuel\s*Consumption\s*MMBtu/i } 0..$#h; my ($iGen) = grep { ($h[$_] // '') =~ /^Net\s*Generation\s*\(?Megawatthours\)?/i } 0..$#h; my ($iSec) = grep { ($h[$_] // '') =~ /Sector\s*Number/i } 0..$#h;
    die "923 columns" unless defined $iFuel && defined $iElec && defined $iGen && defined $iSec;
    my %us = (BIT => [94600, 'coal'], SUB => [96100, 'coal'], LIG => [101000, 'coal'], ANT => [98300, 'coal'], WC => [94600, 'coal'], RC => [94600, 'coal'], SGC => [94600, 'coal'],
              NG => [56100, 'gas'], DFO => [74100, 'oil'], RFO => [77400, 'oil'], JF => [71500, 'oil'], KER => [71900, 'oil'], PC => [97500, 'oil'], WO => [73300, 'oil'], OG => [57600, 'oil'], BFG => [260000, 'oil'], SGP => [57600, 'oil'], PG => [63100, 'oil']);
    for my $r (@rows[$hi+1..$#rows]) { my $u = $us{$r->[$iFuel] // ''} or next; my $e = num($r->[$iElec]) // 0; my $gen = num($r->[$iGen]) // 0; my $sec = int(num($r->[$iSec]) // 0);
        my $tj = $e * 1.055056e-3; $tjElc{US}{$u->[1]} += $tj; $co2Elc{US}{$u->[1]} += $tj * $u->[0] / 1e6; $gep{US}{$u->[1]} += $gen / 1000; $gepMain{US}{$u->[1]} += $gen / 1000 if $sec >= 1 && $sec <= 3;
        if ($u->[1] eq 'coal') { $tjCoal{US} += $tj; $tjLig{US} += $tj if ($r->[$iFuel] // '') eq 'LIG'; } }
}
# prices, 2023, in the country's own currency per MWh of fuel (NCV)
my $fx = 0; { my @l = split /\n/, slurp("$src/ecb_usd_eur_annual.csv"); for (@l) { my @c = split /,/; $fx = $c[7] if ($c[6] // '') eq '2023'; } } die "ECB rate" unless $fx > 1;
my (%pink); { my @l = split /\n/, slurp("$src/wb_pink_sheet_monthly_prices.tsv"); my (%s, $n); for (@l) { my @c = split /\t/; next unless ($c[0] // '') =~ /^2023M/; $s{brent} += $c[2]; $s{coal} += $c[5]; $s{gasUs} += $c[7]; $s{gasEu} += $c[8]; $n++; } $pink{$_} = $s{$_} / $n for keys %s; }
my %aeoVom; { my $t = slurp("$src/eia_aeo2023_elec_cost_perf.txt");
    for my $row (['coal', qr/Ultra-supercritical coal \(USC\)/], ['gas', qr/Combined-cycle.multi-shaft/], ['oil', qr/Combustion turbine.industrial frame/], ['nuclear', qr/Nuclear.light water reactor/]) {
        my ($name, $re) = @$row; if ($t =~ /$re\s+\d{4}\s+[\d,]+\s+\d+\s+\$[\d,]+\s+[\d.]+\s+\$[\d,]+\s+\$([\d.]+)\s+\$[\d.]+/) { $aeoVom{$name} = $1 + 0; } else { die "AEO row $name not found" } } }
# EIA Electric Power Annual Table 7.4, weighted average cost of fossil fuels for the electric power industry, 2023, US$/MMBtu (GCV): all coal ranks, lignite, petroleum, natural gas
my %usFuel; { my @rows = xlsx_sheet_rows(slurp("$src/eia_epa_07_04.xlsx"), 1); my ($r2023) = grep { ($_->[0] // '') eq '2023' } @rows; die "EPA 7.4 2023 row" unless $r2023; $usFuel{coal} = num($r2023->[8]); $usFuel{lignite} = num($r2023->[6]); $usFuel{oil} = num($r2023->[10]); $usFuel{gas} = num($r2023->[12]); die "EPA 7.4 cells" unless $usFuel{coal} && $usFuel{gas} && $usFuel{oil} && $usFuel{lignite}; }
# €/MWh(NCV): hard coal from the Pink Sheet (US$/t, Australian) at the ECB rate over 25.12 GJ/t (6 000 kcal/kg NCV, the marker's basis) and 3.6 GJ/MWh; LIGNITE is untraded in Europe - priced at EIA's 2023 US lignite delivered cost
# (EPA Table 7.4) as a proxy for a comparable untraded fuel, [PROVISIONAL] until a European mine-mouth figure is sourced; gas from TTF (US$/mmbtu, GCV) at the rate, 0.29307 MWh/mmbtu, × 1.108 GCV/NCV; oil from Brent (US$/bbl) at 1.6998 MWh/bbl (5.8 mmbtu) × 1.06 GCV/NCV; manufactured gases at 0 (a byproduct).
my $hardCoalEur = $pink{coal} / $fx / (25.12 / 3.6); my $ligniteEur = $usFuel{lignite} / $fx * 3.412 * 1.05; my $gasEur = $pink{gasEu} / $fx / 0.29307 * 1.108; my $oilEur = $pink{brent} / $fx / 1.6998 * 1.06;
# the USA in US$/MWh(NCV): EPA Table 7.4's delivered costs × 3.412 MMBtu/MWh with the same GCV→NCV factors
my %usEur = (coal => $usFuel{coal} * 3.412 * 1.05, gas => $usFuel{gas} * 3.412 * 1.108, oil => $usFuel{oil} * 3.412 * 1.06);
my %ets = (DE => 85.51, FR => 85.51, IT => 85.51, PL => 85.51, SE => 85.51, US => 0);   # ICAP Allowance Price Explorer, EU ETS secondary market 2023 mean (ENERGY_LAYER_SPINE.md §5); no federal carbon price in the USA
my %avail = (coal => 0.85, gas => 0.85, oil => 0.85);   # [AUTHORED-DRAFT] technical availability of a thermal fleet net of outages; the ERAA de-rating row is BILLED
{
    open my $o, '>:encoding(UTF-8)', "$out/variable_costs_2023.csv" or die $!;
    print $o "# variable_costs_2023.csv - GENERATED by Tools/energy_market_prep.pl (EN-3). The merit order's inputs per country and fossil category, 2023 vintage, in the country's currency (EUR for the five, USD for the USA), carried nominal by the price level (P5-B6).\n";
    printf $o "# fuel_per_mwh_th: DE FR IT PL SE - hard coal from the World Bank Pink Sheet 2023 mean (Australian, %.2f US\$/t) at the ECB 2023 rate (%.4f US\$/EUR) over 25.12 GJ/t; LIGNITE at EIA's 2023 US lignite delivered cost (EPA Table 7.4, %.2f US\$/MMBtu) as a [PROVISIONAL] proxy for an untraded fuel, blended by each country's lignite share of coal input (Eurostat nrg_bal_c); gas from TTF (%.3f US\$/mmbtu, GCV -> NCV x1.108); oil from Brent (%.2f US\$/bbl), manufactured gases at 0, blended by input; US - EIA EPA Table 7.4 2023 delivered cost (all coal %.2f, gas %.2f, petroleum %.2f US\$/MMBtu).\n", $pink{coal}, $fx, $usFuel{lignite}, $pink{gasEu}, $pink{brent}, $usFuel{coal}, $usFuel{gas}, $usFuel{oil};
    print $o "# efficiency: DERIVED - gross electricity from the category's fuels (Eurostat nrg_bal_peh GEP by fuel; EIA-923 net generation by fuel) over the fuel burned for electricity (electricity-only plants + CHP's electricity share), NCV. ef_t_per_mwh: DERIVED - the same fuel's CO2 at the IPCC 2006 defaults over the same electricity. main_share: the main-activity producers' share of the category's generation (EDGAR's 1A1 basis).\n";
    printf $o "# vom_per_mwh: EIA AEO2023 'Cost and Performance Characteristics of New Generating Technologies', Table 1, variable O&M 2022 US\$/MWh - coal = ultra-supercritical (%.2f), gas = combined-cycle multi-shaft (%.2f), oil = combustion-turbine industrial frame (%.2f) - at the ECB rate for the five (a US series applied to Europe: the only one reached). availability: [AUTHORED-DRAFT] %.2f - the ERAA de-rating row is BILLED. ets_per_t: the EU ETS 2023 secondary mean for the five, 0 for the USA.\n", $aeoVom{coal}, $aeoVom{gas}, $aeoVom{oil}, $avail{coal};
    print $o "country;category;fuel_per_mwh_th;efficiency;ef_t_per_mwh;vom_per_mwh;availability;main_share;lignite_share;ets_per_t\n";
    for my $cc (@eu, 'US') { for my $cat (qw(coal gas oil)) {
        my $tj = $tjElc{$cc}{$cat} // 0; my $gwh = $gep{$cc}{$cat} // 0; my $eff = $tj > 0 ? $gwh / ($tj * 0.27778) : 0; my $ef = $gwh > 0 ? ($co2Elc{$cc}{$cat} // 0) / $gwh : 0;   # kt/GWh = t/MWh
        my $lig = ($tjCoal{$cc} // 0) > 0 ? ($tjLig{$cc} // 0) / $tjCoal{$cc} : 0;
        my $fuel; if ($cc eq 'US') { $fuel = $usEur{$cat}; } elsif ($cat eq 'coal') { $fuel = $lig * $ligniteEur + (1 - $lig) * $hardCoalEur; } elsif ($cat eq 'gas') { $fuel = $gasEur; } else { $fuel = $oilEur; }
        # oil: Brent for the products, 0 for the manufactured gases, blended by the category's fuel input
        if ($cat eq 'oil' && $cc ne 'US') { my ($oilOnly, $all) = (0, 0); for my $r (jsonstat_rows("$src/eurostat_nrg_bal_c_2023_TI_EHG.json")) { next unless $r->{geo} eq $cc && $r->{unit} eq 'TJ' && $r->{nrg_bal} eq 'TI_EHG_E' && ($catOf{$r->{siec}} // '') eq 'oil'; $all += $r->{value}; $oilOnly += $r->{value} if $r->{siec} =~ /^O4/; } $fuel = $all > 0 ? $oilEur * $oilOnly / $all : $oilEur; }
        my $vom = $aeoVom{$cat} / ($cc eq 'US' ? 1 : $fx); my $main = $gwh > 0 ? ($gepMain{$cc}{$cat} // 0) / $gwh : 1;
        print $o join(';', $cc, $cat, f2($fuel), f4($eff), f4($ef), f2($vom), f2($avail{$cat}), f4($main > 1 ? 1 : $main), f4($lig), f2($ets{$cc})), "\n"; } }
    close $o;
}
print "energy_market_prep: wrote dispatch_levels_2023.csv, variable_costs_2023.csv, external_links_se.csv to $out\n";
