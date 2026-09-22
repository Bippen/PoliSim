#!/usr/bin/perl
# energy_tax_prep.pl <captures-dir> <out-dir>      e.g.  perl Tools/energy_tax_prep.pl ../PoliSim-captures EnergyData
#
# EN-7b's sourcing (energy stage 5's law category, COMPLETED.md §474; the S1 pattern of §475): the statutory electricity tax per country and
# customer class in the seed year, DERIVED from the files on disk - never typed. Every input is a file docs/data/ENERGY_LAYER_SPINE.md §15 lists by digest
# under PoliSim-captures/sources/energy_tax/. A pattern that does not match stops this script; nothing is filled from memory. Nothing in the
# runtime reads the output until EN-7b's law category does.
#
#   electricity_tax_2023.csv   per country x class (households = Eurostat band DC, non-households = band IC): the 2023 statutory rate the class's
#                              Eurostat band pays in its own currency and unit, the paragraph, the rate in EUR per kWh (the ECB 2023 reference rate
#                              for the krona and the zloty), the seed's environmental-tax component (EnergyData/retail_2023.csv, Eurostat TAX_ENV +
#                              TAX_NUC), and the component over the statute - the COVERAGE a change of the statute can move - with the EU floor
#                              (Council Directive 2003/96/EC, Annex I Table C) and the source file. The USA: no federal electricity excise, not a row.
use strict; use warnings; use utf8;
use JSON::PP;
use Encode ();
binmode STDOUT, ':encoding(UTF-8)';

my ($cap, $out) = @ARGV; die "usage: energy_tax_prep.pl <captures-dir> <out-dir>\n" unless $cap && $out;
my $src = "$cap/sources/energy_tax";

sub slurp { my ($p) = @_; open my $f, '<:raw', $p or die "open $p: $!"; local $/; my $t = <$f>; close $f; return $t; }
# a page's text: scripts, styles and tags stripped, entities decoded, whitespace folded (the checkers' own normalisation)
sub text {
    my ($file, $enc) = @_; my $t = slurp("$src/$file");
    $t = ($enc // 'utf8') eq 'latin1' ? Encode::decode('iso-8859-1', $t) : Encode::decode('UTF-8', $t);
    $t =~ s/<script.*?<\/script>//gsi; $t =~ s/<style.*?<\/style>//gsi; $t =~ s/<!--.*?-->//gs; $t =~ s/<[^>]+>/ /g;
    $t =~ s/&#(\d+);/chr($1)/ge; $t =~ s/&#x([0-9a-f]+);/chr(hex $1)/gei;
    my %e = (nbsp => ' ', amp => '&', lt => '<', gt => '>', quot => '"', apos => "'", sect => "\x{00A7}", rsquo => "'", euro => "\x{20AC}",
             auml => "\x{00E4}", ouml => "\x{00F6}", aring => "\x{00E5}", uuml => "\x{00FC}", szlig => "\x{00DF}", eacute => "\x{00E9}", egrave => "\x{00E8}", agrave => "\x{00E0}");
    $t =~ s/&(\w+);/exists $e{$1} ? $e{$1} : "&$1;"/ge;
    $t =~ s/[\x{00A0}\x{202F}]/ /g; $t =~ s/[\x{2010}\x{2011}]/-/g; $t =~ s/\x{2019}/'/g; $t =~ s/\s+/ /g;
    return $t;
}
sub must { my ($file, $re, $what) = @_; my @m = text($file)  =~ $re; die "$what: the pattern did not match in $file - nothing is filled from memory; re-read the source\n" unless @m; return @m; }
sub num { my ($s) = @_; $s =~ s/\s//g; $s =~ s/,/./; return $s + 0; }
# TEDB's rate JSON (the Commission's Taxes in Europe Database, the REST API's plain JSON): the electricity rows - product key => [rate strings]
sub tedb_electricity {
    my ($file) = @_; my $j = JSON::PP->new->utf8->decode(slurp("$src/$file")); my %rows;
    for my $block (@{ $j->{rateEnergyStructure} || [] }) {
        next unless grep { /\.elec$/ } @{ $block->{subHeadersList} || [] };
        for my $r (@{ $block->{rateEnergy} || [] }) { $rows{$r->{product}} = [ map { $_->{rate} // '' } @{ $r->{energyActiveValues} || [] } ]; }
    }
    die "$file: no electricity rows in the TEDB JSON\n" unless %rows; return \%rows;
}

# ---- the EU floor: Directive 2003/96/EC Annex I Table C, the electricity row (business, non-business), EUR per MWh ---------------------------------
my ($floorBusiness, $floorNonBusiness) = must('eu_2003_96_consol20230110.html', qr/Electricity \(in euro per MWh\) CN code 2716 ([0-9]+,[0-9]+) ([0-9]+,[0-9]+)/, 'EU floor');
$floorBusiness = num($floorBusiness); $floorNonBusiness = num($floorNonBusiness);

# ---- the currency bridge: ECB 2023 reference rates, national currency per euro ------------------------------------------------------------------
my %perEur = (EUR => 1);
for my $line (split /\n/, slurp("$cap/sources/energy/ecb_sek_pln_eur_annual.csv")) { my @f = split /,/, $line; $perEur{$f[2]} = $f[7] if @f > 7 && $f[1] eq 'A' && $f[6] eq '2023' && $f[3] eq 'EUR'; }
die "ECB rates: SEK or PLN missing\n" unless $perEur{SEK} && $perEur{PLN};

# ---- the seed's component: EnergyData/retail_2023.csv, tax_env (Eurostat TAX_ENV + TAX_NUC), EUR per kWh ---------------------------------------
my %component;
for my $line (split /\n/, slurp("$out/retail_2023.csv")) { next if $line =~ /^#|^country;/; my @f = split /;/, $line; $component{$f[0]}{$f[1]} = $f[6] if @f >= 9; }

my @rows;   # country, class, rate, unit, currency, per kWh in EUR, the paragraph, the source, a note
sub row { my (%r) = @_; push @rows, \%r; }

# ---- Germany: StromStG § 3 - one rate for both bands (the reliefs for manufacturing are refunds on application; the 2023 IC band carries § 3)
{
    my ($rate) = must('de_stromstg_3.html', qr/Die Steuer betr\x{00E4}gt ([0-9]+,[0-9]+) Euro f\x{00FC}r eine Megawattstunde/, 'DE § 3');
    my $tedb = tedb_electricity('eu_tedb_de_rate_20230701.json');
    my ($nonBusiness) = map { @{ $tedb->{$_} } } grep { /non_business/ } keys %$tedb; die "DE TEDB non-business missing\n" unless defined $nonBusiness && $nonBusiness =~ /^\s*20\.5\b/;
    my ($relief2011) = must('de_buzer_9b_hfing2024_synopse.html', qr/Die Steuerentlastung betr\x{00E4}gt ([0-9]+,[0-9]+) Euro f\x{00FC}r eine Megawattstunde/, 'DE § 9b relief a.F.');
    for my $class (qw(households nonhousehold)) {
        row(country => 'DE', class => $class, rate => num($rate), unit => 'per MWh', currency => 'EUR', kwh => num($rate) / 1000.0, paragraph => 'StromStG § 3',
            source => 'de_stromstg_3.html; eu_tedb_de_rate_20230701.json (2023 non-business ' . $nonBusiness . ')',
            note => $class eq 'households' ? 'the standard rate since 1 January 2003' : "the standard rate; manufacturing's relief of $relief2011 EUR/MWh (§ 9b a.F.) is a refund on application, outside the band's figure");
    }
}

# ---- Sweden: LSE 11 kap. 3 § as fixed for 2023 (SFS 2022:1590); non-households' band carries the industrial rate of 11 kap. 9 § (0,6 öre) ------
{
    my ($rate) = must('se_sfs_2022_1590_riksdagen.html', qr/betalas med ([0-9]+,[0-9]+) \x{00F6}re per f\x{00F6}rbrukad kilowattimme elektrisk kraft f\x{00F6}r kalender\x{00E5}ret 2023/, 'SE 2023 rate');
    my $t = text('se_lse_lagennu_kons_2023_203.html');
    my ($industrial) = $t =~ /Avdrag enligt f\x{00F6}rsta stycket 6(?: och 7)? medges med skillnaden mellan den skattesats som g\x{00E4}llde vid skattskyldighetens intr\x{00E4}de och ([0-9]+,[0-9]+) \x{00F6}re per f\x{00F6}rbrukad kilowattimme/ or die "SE industrial floor: the pattern did not match\n";
    my ($north) = $t =~ /Avdrag enligt f\x{00F6}rsta stycket 8 medges med ([0-9]+,[0-9]+) \x{00F6}re per f\x{00F6}rbrukad kilowattimme/ ? ($1) : (undef);
    die "SE northern deduction: the pattern did not match\n" unless defined $north;
    row(country => 'SE', class => 'households', rate => num($rate), unit => 'ore per kWh', currency => 'SEK', kwh => num($rate) / 100.0 / $perEur{SEK}, paragraph => 'LSE (1994:1776) 11 kap. 3 §; SFS 2022:1590',
        source => 'se_sfs_2022_1590_riksdagen.html; se_lse_lagennu_kons_2023_203.html', note => "the northern municipalities pay " . num($north) . " ore less (11 kap. 9 §); the band averages both");
    row(country => 'SE', class => 'nonhousehold', rate => num($industrial), unit => 'ore per kWh', currency => 'SEK', kwh => num($industrial) / 100.0 / $perEur{SEK}, paragraph => 'LSE (1994:1776) 11 kap. 9 § 1 st. 6 and 2 st.',
        source => 'se_lse_lagennu_kons_2023_203.html', note => 'the industrial rate; the service sector pays the full rate - the band is read as manufacturing (an inference)');
}

# ---- France: the accise at the shield's floors for 2023 (loi 2022-1726 art. 64); the unshielded tariff from the Assemblée's avis 285 -------------
{
    my $t = text('fr_plf2023_an_avis285.html');
    my ($hh, $unshielded, $other) = $t =~ /fixer \x{00E0} : \x{2013} ([0-9]+) euro par m\x{00E9}gawattheure le tarif normal de l'accise applicable aux m\x{00E9}nages et assimil\x{00E9}s, au lieu de ([0-9]+,[0-9]+) euros ; \x{2013} ([0-9]+,[0-9]+) euro par m\x{00E9}gawattheure le tarif applicable aux autres consommations/
        or die "FR shield floors: the pattern did not match\n";
    my $law = text('fr_lf2023_art64_an_ta51.html'); die "FR art. 64: the shield's text not found\n" unless $law =~ /L\. 312-37/;
    row(country => 'FR', class => 'households', rate => num($hh), unit => 'per MWh', currency => 'EUR', kwh => num($hh) / 1000.0, paragraph => 'CIBS L312-37 at the floor, loi 2022-1726 art. 64',
        source => 'fr_plf2023_an_avis285.html; fr_lf2023_art64_an_ta51.html', note => 'the price shield; the tariff it replaced ' . num($unshielded) . ' EUR/MWh');
    row(country => 'FR', class => 'nonhousehold', rate => num($other), unit => 'per MWh', currency => 'EUR', kwh => num($other) / 1000.0, paragraph => 'CIBS L312-37 at the floor, loi 2022-1726 art. 64',
        source => 'fr_plf2023_an_avis285.html; fr_lf2023_art64_an_ta51.html', note => 'the price shield');
}

# ---- Italy: TUA Allegato I - households 0,0227 (D.M. 30 December 2011), other uses 0,0125 on the first 200 000 kWh a month (TEDB 2023) ----------
{
    my ($hh) = must('it_gu_dm_2011-12-30_11A16869_art1.html', qr/impiegata per qualsiasi applicazione nelle abitazioni, e' determinata in euro ([0-9]+,[0-9]+) per ogni chilowattora/, 'IT households');
    my $tedb = tedb_electricity('eu_tedb_it_rate_20230701.json');
    my ($business) = map { @{ $tedb->{$_} } } grep { /_business_use$/ && !/non_business/ } keys %$tedb;
    die "IT TEDB business missing\n" unless defined $business; my ($b) = $business =~ /([0-9]+(?:\.[0-9]+)?)\s*EUR/ or die "IT TEDB business rate unreadable: $business\n";
    row(country => 'IT', class => 'households', rate => num($hh), unit => 'per kWh', currency => 'EUR', kwh => num($hh), paragraph => 'D.Lgs. 504/1995 Allegato I; D.M. 30.12.2011 art. 1',
        source => 'it_gu_dm_2011-12-30_11A16869_art1.html', note => 'the residence exemption of art. 52 c.3 e) (150 kWh a month up to 3 kW) lowers the band\'s average - consistent with, not proven');
    row(country => 'IT', class => 'nonhousehold', rate => $b, unit => 'per MWh', currency => 'EUR', kwh => $b / 1000.0, paragraph => 'D.Lgs. 504/1995 Allegato I (first 200 000 kWh a month)',
        source => 'eu_tedb_it_rate_20230701.json', note => 'the first tier; the band\'s shortfall against it is not explained by the saved files');
}

# ---- Poland: the excise, ustawa o podatku akcyzowym art. 89 ust. 3 - one rate (TEDB 2023's national-currency row) --------------------------------
{
    my $tedb = tedb_electricity('eu_tedb_pl_rate_20230701.json');
    my @rates = map { @{ $tedb->{$_} } } grep { /\.elec\.fuel_for_(non_)?business_use$/ } sort keys %$tedb; my ($pln) = grep { /PLN/ } @rates; die "PL TEDB rate missing\n" unless defined $pln;
    my ($r) = $pln =~ /([0-9]+(?:\.[0-9]+)?)\s*PLN/ or die "PL rate unreadable: $pln\n";
    for my $class (qw(households nonhousehold)) {
        row(country => 'PL', class => $class, rate => $r, unit => 'per MWh', currency => 'PLN', kwh => $r / 1000.0 / $perEur{PLN}, paragraph => 'ustawa o podatku akcyzowym art. 89 ust. 3',
            source => 'eu_tedb_pl_rate_20230701.json (the statute: pl_dzu_2023_1542_akcyza_tj.pdf)', note => 'the excise is a small part of the band\'s environmental-tax figure; the rest is named by no saved document');
    }
}

# ---- the output ---------------------------------------------------------------------------------------------------------------------------------
mkdir $out unless -d $out;
my $path = "$out/electricity_tax_2023.csv";
open my $o, '>:raw:encoding(UTF-8)', $path or die "$path: $!";
print $o "# electricity_tax_2023.csv - GENERATED by Tools/energy_tax_prep.pl (EN-7b sourcing). The statutory electricity tax per country and class, 2023, from the files docs/data/ENERGY_LAYER_SPINE.md §15 lists by digest.\n";
print $o "# rate is the statute's own figure in its unit and currency; eur_per_kwh converts it at the ECB 2023 reference rate (SEK $perEur{SEK}, PLN $perEur{PLN}); component is the seed's tax_env (EnergyData/retail_2023.csv, Eurostat TAX_ENV + TAX_NUC, EUR/kWh); coverage = component / eur_per_kwh, printed, never capped here.\n";
print $o "# the EU floor (Directive 2003/96/EC Annex I Table C): business $floorBusiness EUR/MWh, non-business $floorNonBusiness EUR/MWh - households may be exempted (Art. 15(1)(h)). The USA: no federal electricity excise; no row.\n";
print $o "country;class;rate;unit;currency;eur_per_kwh;component;coverage;floor_eur_per_kwh;paragraph;source;note\n";
for my $r (@rows) {
    s/; / + /g for @{$r}{qw(paragraph source)}; $r->{note} =~ s/; / - /g; s/;/,/g for @{$r}{qw(paragraph source note)};   # the separator never inside a field
    my $c = $component{$r->{country}}{$r->{class}}; die "no seed component for $r->{country} $r->{class}\n" unless defined $c;
    my $floor = ($r->{class} eq 'households' ? $floorNonBusiness : $floorBusiness) / 1000.0;
    printf $o "%s;%s;%s;%s;%s;%.6f;%s;%.3f;%.4f;%s;%s;%s\n", $r->{country}, $r->{class}, $r->{rate}, $r->{unit}, $r->{currency}, $r->{kwh}, $c, $r->{kwh} > 0 ? $c / $r->{kwh} : 0, $floor, $r->{paragraph}, $r->{source}, $r->{note};
}
close $o;
print "wrote $path (", scalar(@rows), " rows)\n";
printf "  %s %-12s rate %s %s %s = %.6f EUR/kWh, component %s, coverage %.3f\n", $_->{country}, $_->{class}, $_->{rate}, $_->{currency}, $_->{unit}, $_->{kwh}, $component{$_->{country}}{$_->{class}}, $_->{kwh} > 0 ? $component{$_->{country}}{$_->{class}} / $_->{kwh} : 0 for @rows;
