#!/usr/bin/perl
# q29_longrun_prep.pl [--dry]   (run from the project root)
#
# MF q29 (docs/RULINGS_2026-10-18.md q29; Elias's K139/K140, docs/RULINGS_2026-10-23.md): life expectancy derived from mortality, the output
# gap closing at a sourced persistence, and the no-policy growth held against the EC Ageing Report 2024, the IMF WEO and (beyond the WEO's
# horizon, the USA) CBO's Long-Term Budget Outlook. This tool DERIVES every figure the model and its diagnostic read from the files saved
# byte-exact under ElectionsData/macro/raw/q29_longrun/ (each held against that folder's SHA256SUMS.txt, which it checks) and writes
#   Assets/Scripts/Data/Generated/LongRunAnchors.cs
# never typing a figure. What it reads, and how:
#   - The seed mortality schedule, the EU five: Eurostat demo_mlifetable, indic_de DEATHRATE (the life table's Mx), sex T, 2024, single ages
#     0..94 and the open interval 95+ (Y_GE95). The age dimension MIXES Y85 and Y_GE85 - only the single years and Y_GE95 are read.
#   - The seed mortality schedule, the USA: Census Bureau 2023 National Population Projections, np2023_a2.csv (projected age-specific death
#     rates), NATIVITY 0 (total), SEX 0, GROUP 0 (total), 2024, ages 0..99 and 100+, collapsed to 95+ (below). The layout's key names SEX 1 and 2
#     only; the file carries SEX 0 rows, read as both sexes - np2023_a3.csv's E0 for SEX 0 lies between its SEX 1 and SEX 2 rows every year.
#     Census prints the rates to four decimals, so a young age's ratio is coarse: each side of a USA ratio is floored at half that unit.
#     EUROPOP2023 prints five decimals: each sex's projected rate is floored at half that unit (0.000005) before the sexes are combined.
#   - The mortality trend: the projection each population path already follows. The EU five: Eurostat proj_23naasmr (EUROPOP2023, the
#     Ageing Report 2024's population basis), projection BSL, the two sexes, ages 0..99 and 100+; the USA: np2023_a2 as above. At each anchor
#     year the ratio m(x, anchor) / m(x, 2024); the EU's rates are the two sexes' combined on each sex's STATIONARY population at that year
#     (its own life table, equal births - the sex ratio at birth left out, stated): the substrate is sex-blind, and women outnumber men where the
#     rates are highest, so a plain mean of the two sexes' ratios would weight the men's faster improvement at the oldest ages too heavily.
#     The anchors open at 2022 (EUROPOP2023's base year), so a start before the seed year reads its own year's mortality; the Census projection
#     opens in 2023, and an earlier anchor of the USA's holds 2023's rates.
#   - 95+ from single years and an open 100+: the stationary life table from 95 (q = m / (1 + m/2), L = l - d/2, the open L = l / m) gives
#     m(95+) = l(95) / T(95), the rate whose stationary population reproduces those deaths.
#   - The gap's persistence: Holston, Laubach and Williams' current estimates (NY Fed, the "Parameters" sheet: a_y,1 and a_y,2 of the
#     output-gap equation, quarterly), for the United States and the Euro Area. The ANNUAL persistence is the share of a standing gap (equal in
#     the two quarters before) left four quarters on, the real-rate term at zero: g(t) = a1 g(t-1) + a2 g(t-2). The euro area's stands for the
#     EU five (HLW estimate no equation for Sweden or Poland - stated).
#   - The life-expectancy band: EC 2024 Ageing Report statistical annex, cross-country tables II.1.2 (men) and II.1.3 (women), 2022-2070;
#     the USA: np2023_a3.csv E0, SEX 0, 2023-2100.
#   - Potential growth: the same annex's table II.1.14 (potential GDP, growth rate), 2022-2070; the USA: CBO's March 2025 LTBO economic file
#     (57054-2025-03-LTBO-econ.xlsx, sheet 1, "Real potential GDP"), 2025-2055.
#   - K210 (docs/RULINGS_2026-10-23.md): the two parts of that potential growth. Productivity: the annex's table II.1.21 (potential GDP per
#     worker, growth rate), 2022-2070; the USA: the same CBO sheet's "Potential labor force productivity" (real potential GDP over the potential
#     labour force), 2025-2055 - both per WORKER, the unit the model's labour input counts (PotentialOutput.LabourInput, persons employed).
#     Labour: the annex's table II.1.15 (employment 15-74, growth rate); the USA: CBO's "Potential labor force". Each source's potential
#     growth is held to (1 + productivity)(1 + labour) - 1 within PART_TOLERANCE every year, or the script stops.
#   - Real GDP growth to the WEO's horizon: the IMF DataMapper's NGDP_RPCH as the Internet Archive kept it on 5 October 2026 (its horizon,
#     2031, is the April 2026 edition's), 2024 to the last year it carries.
# A pattern that does not match, a figure absent, or a digest that is not SHA256SUMS.txt's stops the script. ASCII only.
use strict;
use warnings;
use JSON::PP;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw(unzip $UnzipError);
use IO::Uncompress::Gunzip qw(gunzip $GunzipError);

my $dry = @ARGV && $ARGV[0] eq '--dry';
die "run from the project root\n" unless -d 'Assets/Scripts/Data/Generated';
my $raw = 'ElectionsData/macro/raw/q29_longrun';
my $out = 'Assets/Scripts/Data/Generated/LongRunAnchors.cs';
my $SEED_YEAR = 2024;
my @ANCHORS = (2022, 2024, 2030, 2040, 2050, 2060, 2070, 2080, 2090, 2100);
my $AGES = 96;   # 0..94 single years and the open 95+
my $CENSUS_HALF_UNIT = 0.00005;   # half the unit np2023_a2 prints its rates to
my $EUROPOP_HALF_UNIT = 0.000005;   # half the unit proj_23naasmr prints its rates to
sub max { return $_[0] > $_[1] ? $_[0] : $_[1]; }
my @EU = (['SE', 'SWE', 'Sweden'], ['DE', 'DEU', 'Germany'], ['FR', 'FRA', 'France'], ['IT', 'ITA', 'Italy'], ['PL', 'POL', 'Poland']);

sub slurp { my ($p) = @_; open my $fh, '<:raw', $p or die "$p: $!\n"; local $/; my $s = <$fh>; close $fh; return $s; }
sub must { my ($v, $what) = @_; die "q29_longrun_prep: $what not found\n" unless defined $v; return $v; }

my %sums;
for (split /\n/, slurp("$raw/SHA256SUMS.txt")) { s/\r$//; $sums{$2} = $1 if /^([0-9a-f]{64}) \*?(\S+)$/; }
my %F = (
    life => 'eurostat_demo_mlifetable_DEATHRATE_T_2023-2024.json',
    lexp => 'eurostat_demo_mlexpec_T_Y_LT1.json',
    proj => 'eurostat_proj_23naasmr_BSL.json',
    a2 => 'census_np2023_a2.csv',
    a3 => 'census_np2023_a3.csv',
    hlw => 'nyfed_Holston_Laubach_Williams_current_estimates.xlsx',
    ar => 'ec_2024_Ageing_Report-Statistical_annex_all_horizontal_tables.xlsx',
    cbo => 'wayback_20250404235041_cbo_57054-2025-03-LTBO-econ.xlsx',
    weo => 'wayback_20261005135329_imf_datamapper_NGDP_RPCH.json.gz',
);
my %digest;
for my $k (sort keys %F) {
    my $f = $F{$k};
    $digest{$k} = sha256_hex(slurp("$raw/$f"));
    die "$f: its SHA-256 $digest{$k} is not the one SHA256SUMS.txt lists\n" unless ($sums{$f} // '') eq $digest{$k};
}

# ---- JSON-stat 2.0: the value at named members of every dimension (a dimension of one member may be omitted)
sub jstat {
    my ($j, %at) = @_;
    my @ids = @{ $j->{id} }; my @size = @{ $j->{size} };
    my $i = 0;
    for my $k (0 .. $#ids) {
        my $d = $ids[$k];
        my $p;
        if (exists $at{$d}) { $p = $j->{dimension}{$d}{category}{index}{ $at{$d} }; return undef unless defined $p; }
        else { die "dimension $d has $size[$k] members and was not named\n" if $size[$k] != 1; $p = 0; }
        $i = $i * $size[$k] + $p;
    }
    return $j->{value}{$i};
}
sub ageKey { my ($x) = @_; return $x == 0 ? 'Y_LT1' : "Y$x"; }

# ---- the stationary collapse of single years 95..99 and an open 100+ into one open 95+
sub collapse95 {
    my (@m) = @_;   # m[0..4] = 95..99, m[5] = 100+
    my ($l, $T) = (1.0, 0.0);
    for my $x (0 .. 4) {
        my $q = $m[$x] / (1 + 0.5 * $m[$x]);
        my $d = $l * $q;
        $T += $l - 0.5 * $d;
        $l -= $d;
    }
    $T += $l / $m[5];
    return 1.0 / $T;
}

# ---- e0 by the same arithmetic MortalitySchedule.LifeExpectancyAtBirth carries (q = m / (1 + m/2) at every closed age, the open L = l / m)
sub e0 {
    my (@m) = @_;
    my ($l, $T) = (1.0, 0.0);
    for my $x (0 .. $AGES - 2) {
        my $q = $m[$x] / (1 + 0.5 * $m[$x]);
        $q = 1 if $q > 1;
        my $d = $l * $q;
        $T += $l - 0.5 * $d;
        $l -= $d;
    }
    $T += $l / $m[$AGES - 1];
    return $T;
}

my (%seed, %ratio, %flags, %eurostatE0);

# ---- the EU five's seed: demo_mlifetable 2024
{
    my $j = decode_json(slurp("$raw/$F{life}"));
    for my $c (@EU) {
        my ($geo, undef, $name) = @$c;
        my @m;
        for my $x (0 .. 94) { push @m, must(jstat($j, indic_de => 'DEATHRATE', sex => 'T', age => ageKey($x), geo => $geo, time => $SEED_YEAR), "$name Mx $x"); }
        push @m, must(jstat($j, indic_de => 'DEATHRATE', sex => 'T', age => 'Y_GE95', geo => $geo, time => $SEED_YEAR), "$name Mx 95+");
        $seed{$name} = \@m;
    }
    my $e = decode_json(slurp("$raw/$F{lexp}"));
    for my $c (@EU) {
        my ($geo, undef, $name) = @$c;
        $eurostatE0{$name} = must(jstat($e, sex => 'T', age => 'Y_LT1', geo => $geo, time => $SEED_YEAR, unit => 'YR'), "$name e0 $SEED_YEAR") if grep { $_ eq 'unit' } @{ $e->{id} };
        $eurostatE0{$name} //= must(jstat($e, sex => 'T', age => 'Y_LT1', geo => $geo, time => $SEED_YEAR), "$name e0 $SEED_YEAR");
    }
}

# ---- the two sexes combined: m_T(x) = the sum over the sexes of L_s(x) m_s(x) over the sum of L_s(x), L_s each sex's own stationary life table
sub both {
    my ($mm, $mf) = @_;
    my @L;
    for my $m ($mm, $mf) {
        my ($l, @Ls) = (1.0);
        for my $x (0 .. $AGES - 2) { my $q = $m->[$x] / (1 + 0.5 * $m->[$x]); $q = 1 if $q > 1; my $d = $l * $q; push @Ls, $l - 0.5 * $d; $l -= $d; }
        push @Ls, $l / $m->[$AGES - 1];
        push @L, \@Ls;
    }
    return [ map { my $w = $L[0][$_] + $L[1][$_]; $w > 0 ? ($L[0][$_] * $mm->[$_] + $L[1][$_] * $mf->[$_]) / $w : 0.5 * ($mm->[$_] + $mf->[$_]) } 0 .. $AGES - 1 ];
}

# ---- the EU five's trend: EUROPOP2023 BSL, the two sexes combined at each anchor (both) and the ratio to 2024
{
    my $j = decode_json(slurp("$raw/$F{proj}"));
    my $at = sub {
        my ($geo, $sex, $year) = @_;
        my @m;
        for my $x (0 .. 99) { push @m, must(jstat($j, projection => 'BSL', sex => $sex, age => ageKey($x), geo => $geo, time => $year), "proj $geo $sex $year $x"); }
        push @m, must(jstat($j, projection => 'BSL', sex => $sex, age => 'Y_GE100', geo => $geo, time => $year), "proj $geo $sex $year 100+");
        @m = map { max($_, $EUROPOP_HALF_UNIT) } @m;   # L-1: Eurostat prints five decimals; a rate read as 0.00000 is floored at half that unit
        return [ @m[0 .. 94], collapse95(@m[95 .. 100]) ];
    };
    for my $c (@EU) {
        my ($geo, undef, $name) = @$c;
        my $base = both($at->($geo, 'M', $SEED_YEAR), $at->($geo, 'F', $SEED_YEAR));
        for my $year (@ANCHORS) {
            my $now = both($at->($geo, 'M', $year), $at->($geo, 'F', $year));
            push @{ $ratio{$name} }, [ map { $base->[$_] > 0 ? $now->[$_] / $base->[$_] : 1 } 0 .. $AGES - 1 ];
        }
    }
}

# ---- the USA: np2023_a2, NATIVITY 0, SEX 0, GROUP 0 - the seed at 2024 and the ratios at the anchors
{
    my @lines = split /\r?\n/, slurp("$raw/$F{a2}");
    my @head = split /,/, shift @lines;
    die "np2023_a2: header\n" unless $head[0] eq 'NATIVITY' && $head[3] eq 'YEAR' && $head[4] eq 'ASDR_0' && $head[104] eq 'ASDR_100';
    my %row;
    for (@lines) { my @f = split /,/; next unless @f > 104 && $f[0] eq '0' && $f[1] eq '0' && $f[2] eq '0'; $row{ $f[3] } = [ @f[4 .. 104] ]; }
    my $at = sub { my ($y) = @_; $y = 2023 if $y < 2023; my $r = must($row{$y}, "np2023_a2 total $y");   # the projection opens in 2023: an earlier anchor holds 2023's
    return [ @$r[0 .. 94], collapse95(@$r[95 .. 100]) ]; };
    $seed{USA} = $at->($SEED_YEAR);
    for my $year (@ANCHORS) {
        my $now = $at->($year);
        # L-1: Census prints four decimals, so a rate may read 0.0000 or step by a whole unit; each side of the ratio is floored at half the
        # rounding unit (0.00005), so no age carries a zero ratio and a printed zero reads as at most half a unit
        push @{ $ratio{USA} }, [ map { my ($b, $n) = (max($seed{USA}[$_], $CENSUS_HALF_UNIT), max($now->[$_], $CENSUS_HALF_UNIT)); $n / $b } 0 .. $AGES - 1 ];
    }
}

# ---- xlsx: shared strings and one sheet's cells
sub member { my ($zip, $name) = @_; my $o; unzip($zip => \$o, Name => $name) or die "$zip $name: $UnzipError\n"; return $o; }
sub sheet {
    my ($zip, $n) = @_;
    my $sst = member($zip, 'xl/sharedStrings.xml');
    my @ss = map { my $t = $_; $t =~ s/<[^>]+>//g; $t } ($sst =~ m{<si>(.*?)</si>}gs);
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
sub colIndex { my ($c) = @_; my $n = 0; $n = $n * 26 + ord($_) - 64 for split //, $c; return $n; }
sub colName { my ($n) = @_; my $s = ''; while ($n > 0) { my $r = ($n - 1) % 26; $s = chr(65 + $r) . $s; $n = int(($n - 1) / 26); } return $s; }

# ---- HLW: a_y,1 and a_y,2 for the United States and the Euro Area
my %rho;
{
    my $c = sheet("$raw/$F{hlw}", 3);
    my ($r1) = grep { ($c->{$_}{A} // '') eq 'a_y,1' } keys %$c;
    my ($r2) = grep { ($c->{$_}{A} // '') eq 'a_y,2' } keys %$c;
    my ($rh) = grep { ($c->{$_}{A} // '') eq 'Parameter' } keys %$c;
    die "HLW: rows\n" unless $r1 && $r2 && $rh;
    die "HLW: columns\n" unless ($c->{$rh}{B} // '') =~ /^United States/ && ($c->{$rh}{D} // '') =~ /^Euro Area/;
    for ([B => 'US'], [D => 'EA']) {
        my ($col, $k) = @$_;
        my ($a1, $a2) = ($c->{$r1}{$col}, $c->{$r2}{$col});
        my @g = (1, 1);
        push @g, $a1 * $g[-1] + $a2 * $g[-2] for 1 .. 4;
        $rho{$k} = { a1 => $a1, a2 => $a2, annual => $g[-1] };
    }
}

# ---- the Ageing Report's cross-country sheet: a table's row per country code, its year columns
my $ar = sheet("$raw/$F{ar}", 4);
sub arTable {
    my ($title) = @_;
    my ($rt) = grep { ($ar->{$_}{B} // '') =~ /^\Q$title\E/ } keys %$ar;
    die "Ageing Report: $title\n" unless $rt;
    my $hr = $rt + 2;
    my %yearCol;
    for my $col (keys %{ $ar->{$hr} }) { $yearCol{ $ar->{$hr}{$col} } = $col if $ar->{$hr}{$col} =~ /^20\d\d$/; }
    die "Ageing Report: $title years\n" unless $yearCol{2022} && $yearCol{2070};
    # the table's rows run from the year header to the next table's title, and each country's row is read once: a second match dies
    # (the review's H-1 - a fixed window ran five rows into the next table and Germany's row was overwritten by it)
    my %out;
    my $lastRow = (sort { $b <=> $a } keys %$ar)[0];
    for my $r ($hr + 1 .. $lastRow) {
        last if index($ar->{$r}{B} // '', 'Table ') == 0;
        my $code = $ar->{$r}{B} // next;
        next unless grep { $_->[0] eq $code } @EU;
        my ($name) = map { $_->[2] } grep { $_->[0] eq $code } @EU;
        die "Ageing Report: $title carries $code twice\n" if exists $out{$name};
        $out{$name} = [ map { must($ar->{$r}{ $yearCol{$_} }, "$title $code $_") } 2022 .. 2070 ];
    }
    die "Ageing Report: $title has " . scalar(keys %out) . " of the five\n" unless keys %out == 5;
    return \%out;
}
sub plausible {
    my ($what, $rows, $lo, $hi) = @_;
    for my $n (sort keys %$rows) { for my $v (@{ $rows->{$n} }) { die "$what: $n reads $v, outside $lo..$hi\n" if $v < $lo || $v > $hi; } }
}
my $leMen = arTable('Table II.1.2: Life expectancy at birth - Men');
my $leWomen = arTable('Table II.1.3: Life expectancy at birth - Women');
my $potAr = arTable('Table II.1.14: Potential GDP (growth rate)');
plausible('life expectancy at birth, men', $leMen, 70, 95);
plausible('life expectancy at birth, women', $leWomen, 70, 95);
plausible('potential growth', $potAr, -3, 6);
my $pwAr = arTable('Table II.1.21: Potential GDP per worker (growth rate)');
my $empAr = arTable('Table II.1.15: Employment (15-74y; growth rate)');
plausible('potential growth per worker', $pwAr, -3, 6);
plausible('employment growth', $empAr, -4, 4);
for my $n (sort keys %$leMen) { for my $i (0 .. $#{ $leMen->{$n} }) { die "Ageing Report: $n men's e0 not below women's at " . (2022 + $i) . "\n" unless $leMen->{$n}[$i] < $leWomen->{$n}[$i]; } }

# ---- Census a3: E0, both sexes
my @usE0;
{
    my @lines = split /\r?\n/, slurp("$raw/$F{a3}");
    die "np2023_a3: header\n" unless shift(@lines) eq 'NATIVITY,SEX,GROUP,YEAR,E0';
    my %e;
    for (@lines) { my @f = split /,/; $e{ $f[3] } = $f[4] if @f == 5 && $f[0] eq '0' && $f[1] eq '0' && $f[2] eq '0'; }
    @usE0 = map { must($e{$_}, "np2023_a3 E0 $_") } 2023 .. 2100;
}

# ---- CBO: real potential GDP growth, 2025-2055
my (@cboPot, @cboPw, @cboLab);
{
    my $c = sheet("$raw/$F{cbo}", 2);
    my ($rp) = grep { ($c->{$_}{A} // '') =~ /^Real potential GDP/ } keys %$c;
    die "CBO: the real potential GDP row\n" unless $rp;
    my ($ry) = grep { ($c->{$_}{B} // '') eq '1995' } keys %$c;
    die "CBO: the year row\n" unless $ry;
    my %yc; for my $col (keys %{ $c->{$ry} }) { $yc{ $c->{$ry}{$col} } = $col if $c->{$ry}{$col} =~ /^\d{4}$/; }
    @cboPot = map { must($c->{$rp}{ must($yc{$_}, "CBO year $_") }, "CBO potential $_") } 2025 .. 2055;
    # K210: the two parts (the footnote letter rides the label: "Potential labor forceb", "Potential labor force productivityc")
    # each label must match exactly one row: a second match stops the script (the review's L7, as arTable since q29's H-1)
    my @rw = grep { ($c->{$_}{A} // '') =~ /^Potential labor force productivity[a-z]?$/ } keys %$c;
    my @rl = grep { ($c->{$_}{A} // '') =~ /^Potential labor force[a-z]?$/ } keys %$c;
    die "CBO: the potential labor force productivity row matches " . scalar(@rw) . " rows, not one\n" unless @rw == 1;
    die "CBO: the potential labor force row matches " . scalar(@rl) . " rows, not one\n" unless @rl == 1;
    my ($rw, $rl) = ($rw[0], $rl[0]);
    @cboPw = map { must($c->{$rw}{ $yc{$_} }, "CBO productivity $_") } 2025 .. 2055;
    @cboLab = map { must($c->{$rl}{ $yc{$_} }, "CBO labour force $_") } 2025 .. 2055;
}

# ---- K210: each source's potential growth is its two parts compounded, every year (the published figures are rounded; CBO's to two decimals)
my $PART_TOLERANCE = 0.05;   # percentage points
{
    my $worst = 0;
    my %pot = (%$potAr, USA => \@cboPot); my %pw = (%$pwAr, USA => \@cboPw); my %lab = (%$empAr, USA => \@cboLab);
    for my $n (sort keys %pot) {
        for my $i (0 .. $#{ $pot{$n} }) {
            my $d = abs(((1 + $pw{$n}[$i] / 100) * (1 + $lab{$n}[$i] / 100) - 1) * 100 - $pot{$n}[$i]);
            die "K210: $n year index $i - potential $pot{$n}[$i] is not productivity $pw{$n}[$i] with labour $lab{$n}[$i] (off $d pp)\n" if $d > $PART_TOLERANCE;
            $worst = $d if $d > $worst;
        }
    }
    printf "K210: potential = productivity x labour, worst year off by %.4f pp (tolerance %.2f)\n", $worst, $PART_TOLERANCE;
}

# ---- WEO: NGDP_RPCH from 2024 to the last year every one of the six carries
my (%weo, $weoLast);
{
    my $gz = slurp("$raw/$F{weo}");
    my $txt; gunzip(\$gz => \$txt) or die "WEO: $GunzipError\n";
    my $j = decode_json($txt);
    my %iso = (map { $_->[2] => $_->[1] } @EU); $iso{USA} = 'USA';
    for my $name (keys %iso) {
        my $v = must($j->{values}{NGDP_RPCH}{ $iso{$name} }, "WEO $name");
        my $last = (sort { $b <=> $a } keys %$v)[0];
        $weoLast = $last if !defined $weoLast || $last < $weoLast;
        $weo{$name} = $v;
    }
    for my $name (keys %weo) { $weo{$name} = [ map { must($weo{$name}{$_}, "WEO $name $_") } 2024 .. $weoLast ]; }
}

# ---- the verification line: the method's e0 on each seed schedule against the publisher's own
my @order = ('Sweden', 'Germany', 'France', 'Italy', 'Poland', 'USA');
for my $n (@order) {
    my $mine = e0(@{ $seed{$n} });
    printf "%-8s e0 on the 2024 schedule %.3f  publisher %s\n", $n, $mine, $n eq 'USA' ? "(Census a3 E0 2024) $usE0[1]" : $eurostatE0{$n};
}
printf "gap persistence, annual: US %.4f (a1 %.4f a2 %.4f)  EA %.4f (a1 %.4f a2 %.4f)\n",
    $rho{US}{annual}, $rho{US}{a1}, $rho{US}{a2}, $rho{EA}{annual}, $rho{EA}{a1}, $rho{EA}{a2};
print "WEO horizon $weoLast\n";
exit 0 if $dry;

# ---- the C#
sub f { my ($v) = @_; my $s = sprintf('%.7g', $v); $s .= '.0' unless $s =~ /[.eE]/; return $s . 'f'; }
sub arr { return 'new float[] { ' . join(', ', map { f($_) } @_) . ' }'; }
my $cs = '';
$cs .= "// GENERATED by Tools/q29_longrun_prep.pl. DO NOT EDIT BY HAND.\n//\n";
$cs .= "// MF q29: the mortality schedules life expectancy is derived from, the output gap's annual persistence, and the sourced paths the no-policy\n";
$cs .= "// run is held against. Every figure below is read from ElectionsData/macro/raw/q29_longrun/ by that tool, whose header names each file,\n";
$cs .= "// series and transformation.\n//\n";
for my $k (sort keys %F) { $cs .= "// SHA-256 ($F{$k}): $digest{$k}\n"; }
$cs .= "\nusing System.Collections.Generic;\n\nnamespace PoliSim.Data.Generated\n{\n    public static class LongRunAnchors\n    {\n";
$cs .= "        /// <summary>Ages carried: single years 0..94 and the open interval 95+.</summary>\n        public const int AgeCount = $AGES;\n";
$cs .= "        /// <summary>The year of the seed schedules and the base of every ratio.</summary>\n        public const int SeedYear = $SEED_YEAR;\n";
$cs .= "        /// <summary>The years at which the trend's ratio is carried; between two the log-ratio is interpolated, after the last it holds.</summary>\n";
$cs .= "        public static readonly int[] AnchorYears = { " . join(', ', @ANCHORS) . " };\n\n";
$cs .= "        /// <summary>The seed's age-specific death rates, per person-year (Eurostat demo_mlifetable Mx 2024; the USA Census np2023_a2 2024).</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float[]> SeedMortality = new Dictionary<CountryId, float[]>\n        {\n";
$cs .= "            { CountryId.$_, " . arr(@{ $seed{$_} }) . " },\n" for @order;
$cs .= "        };\n\n";
$cs .= "        /// <summary>[anchor][age]: the projection's m(x, anchor) / m(x, 2024) (EUROPOP2023 BSL, the two sexes combined on their stationary populations; the USA np2023_a2, each side floored at half Census's rounding unit).</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float[][]> MortalityRatio = new Dictionary<CountryId, float[][]>\n        {\n";
for my $n (@order) {
    $cs .= "            { CountryId.$n, new float[][]\n            {\n";
    $cs .= "                " . arr(@$_) . ",\n" for @{ $ratio{$n} };
    $cs .= "            } },\n";
}
$cs .= "        };\n\n";
$cs .= "        /// <summary>The output-gap equation's two lags, quarterly (Holston-Laubach-Williams current estimates, the Parameters sheet).</summary>\n";
$cs .= "        public const float HlwUsAy1 = " . f($rho{US}{a1}) . ", HlwUsAy2 = " . f($rho{US}{a2}) . ", HlwEaAy1 = " . f($rho{EA}{a1}) . ", HlwEaAy2 = " . f($rho{EA}{a2}) . ";\n";
$cs .= "        /// <summary>The ANNUAL persistence: the share of a standing gap left four quarters on, the real-rate term at zero. The USA reads the US\n";
$cs .= "        /// estimate, the EU five the euro area's.</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float> GapPersistence = new Dictionary<CountryId, float>\n        {\n";
$cs .= "            { CountryId.$_, " . f($_ eq 'USA' ? $rho{US}{annual} : $rho{EA}{annual}) . " },\n" for @order;
$cs .= "        };\n\n";
$cs .= "        /// <summary>The first year of the life-expectancy band and of the potential-growth path, per country.</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, int> LifeExpectancyFirstYear = new Dictionary<CountryId, int>\n        {\n";
$cs .= "            { CountryId.$_, " . ($_ eq 'USA' ? 2023 : 2022) . " },\n" for @order;
$cs .= "        };\n";
$cs .= "        /// <summary>Life expectancy at birth, the band's low and high edge by year: the EU five the Ageing Report 2024's men (low) and women (high),\n";
$cs .= "        /// tables II.1.2 and II.1.3; the USA Census np2023_a3 E0 for both sexes on both edges.</summary>\n";
for my $edge (['LifeExpectancyLow', $leMen], ['LifeExpectancyHigh', $leWomen]) {
    $cs .= "        public static readonly Dictionary<CountryId, float[]> $edge->[0] = new Dictionary<CountryId, float[]>\n        {\n";
    $cs .= "            { CountryId.$_, " . arr($_ eq 'USA' ? @usE0 : @{ $edge->[1]{$_} }) . " },\n" for @order;
    $cs .= "        };\n";
}
$cs .= "\n        public static readonly Dictionary<CountryId, int> PotentialGrowthFirstYear = new Dictionary<CountryId, int>\n        {\n";
$cs .= "            { CountryId.$_, " . ($_ eq 'USA' ? 2025 : 2022) . " },\n" for @order;
$cs .= "        };\n";
$cs .= "        /// <summary>Potential GDP growth, per cent a year: the EU five the Ageing Report 2024's table II.1.14; the USA CBO's real potential GDP (LTBO, March 2025).</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float[]> PotentialGrowth = new Dictionary<CountryId, float[]>\n        {\n";
$cs .= "            { CountryId.$_, " . arr($_ eq 'USA' ? @cboPot : @{ $potAr->{$_} }) . " },\n" for @order;
$cs .= "        };\n";
$cs .= "        /// <summary>K210: the productivity part of that potential growth, per cent a year, per WORKER - the EU five the Ageing Report 2024's table II.1.21\n";
$cs .= "        /// (potential GDP per worker); the USA CBO's potential labor force productivity (LTBO, March 2025). Same first years as PotentialGrowth.</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float[]> ProductivityGrowth = new Dictionary<CountryId, float[]>\n        {\n";
$cs .= "            { CountryId.$_, " . arr($_ eq 'USA' ? @cboPw : @{ $pwAr->{$_} }) . " },\n" for @order;
$cs .= "        };\n";
$cs .= "        /// <summary>K210: the labour part, per cent a year - the EU five the Ageing Report 2024's table II.1.15 (employment 15-74); the USA CBO's potential\n";
$cs .= "        /// labor force. Same first years as PotentialGrowth.</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float[]> LabourGrowth = new Dictionary<CountryId, float[]>\n        {\n";
$cs .= "            { CountryId.$_, " . arr($_ eq 'USA' ? @cboLab : @{ $empAr->{$_} }) . " },\n" for @order;
$cs .= "        };\n\n";
$cs .= "        /// <summary>Real GDP growth, per cent a year, from the first year to the WEO's horizon (the IMF DataMapper's NGDP_RPCH).</summary>\n";
$cs .= "        public const int WeoFirstYear = 2024, WeoLastYear = $weoLast;\n";
$cs .= "        public static readonly Dictionary<CountryId, float[]> WeoRealGrowth = new Dictionary<CountryId, float[]>\n        {\n";
$cs .= "            { CountryId.$_, " . arr(@{ $weo{$_} }) . " },\n" for @order;
$cs .= "        };\n    }\n}\n";
$cs =~ s/\n/\r\n/g;
open my $o, '>:raw', $out or die "$out: $!\n"; print $o $cs; close $o;
print "wrote $out\n";
