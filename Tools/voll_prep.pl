#!/usr/bin/perl
# MF q26 (Elias's rulings q26, 2026-10-18, and K145, 2026-10-27): THE VALUE OF LOST LOAD PER COUNTRY - unserved energy costs output at ACER's VoLL
# (CEPA 2018) for the EU five and at LBNL's Interruption Cost Estimate (Sullivan, Schellenberg and Blundell 2015) for the USA.
# Reads the stored sources in EnergyData/raw/voll/ (fetched byte-exact by curl, each digest checked against that folder's SHA256SUMS.txt; the fetch log
# beside them) and writes Assets/Scripts/Data/Generated/ValueOfLostLoadData.cs:
#   - CEPA, "Study on the estimation of the value of lost load of electricity supply in Europe", Final Report July 2018 (2015 euros): Annex G, Table G.1
#     (domestic VoLL, the annual average) and Tables G.2-G.3 (non-domestic VoLL by sector, 13 sectors), read out of the PDF's own content streams;
#   - Eurostat nrg_bal_c, electricity (E7000) final energy consumption by sector in 2015, GWh - the weights of CEPA's thirteen sectors (the study's own
#     year), each sector mapped to the balance's rows below; a sector CEPA marks "*" (unable to estimate) is left out of both sums;
#   - LBNL-6941E (2013 dollars), Table ES-1: the cost per unserved kWh at the longest duration the study estimates (16 hours), medium and large C&I and
#     residential;
#   - the price bridges to 2023, the energy layer's catalog year: Eurostat prc_hicp_aind (CP00, the annual average index, 2015 = 100) per country, and
#     BLS CPI-U (CUUR0000SA0) annual averages for 2013 and 2023;
#   - THREE UNITS (the review's M2): a HICP measures prices in the country's OWN currency, so Sweden's and Poland's 2015 euros go to krona and zloty at the
#     ECB's 2015 annual reference rate before their HICP carries them (ECB EXR A.SEK/PLN.EUR.SP00.A); the row carries that rate and the 2023 one, and
#     EnergyReliability converts back to the market's euro at the energy layer's own 2023 rate (EnergyLayerData.NationalPerMarketCurrency).
# Usage (from the project root): perl Tools/voll_prep.pl [cs]
# Writes LF line ends; the output is byte-identical on every run from the same files.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use Compress::Zlib;
use JSON::PP;

my $cs = $ARGV[0] // 'Assets/Scripts/Data/Generated/ValueOfLostLoadData.cs';
my $dir = 'EnergyData/raw/voll/';
my %file = (
    cepa => 'cepa_voll_study_2018.pdf',
    lbnl => 'lbnl-6941e.pdf',
    bal => 'eurostat_nrg_bal_c_electricity_2015.json',
    hicp => 'eurostat_prc_hicp_aind_2015_2023.json',
    cpi13 => 'bls_CUUR0000SA0_2013_2022.json',
    cpi23 => 'bls_CUUR0000SA0_2023.json',
    fx => 'ecb_exr_A_PLN_SEK_EUR_SP00_A_2015_2023.csv',
);

open my $sf, '<', $dir . 'SHA256SUMS.txt' or die "SHA256SUMS.txt: $!";
my %listed = map { s/\r?\n\z//; my ($d, $n) = /^([0-9a-f]{64}) \*(.+)$/ or die "SHA256SUMS.txt: unreadable line $_\n"; ($n => $d) } grep { /\S/ } <$sf>;
close $sf;
my (%bytes, %digest);
for my $k (sort keys %file) {
    open my $fh, '<:raw', $dir . $file{$k} or die "$dir$file{$k}: $!";
    $bytes{$k} = do { local $/; <$fh> };
    close $fh;
    $digest{$k} = sha256_hex($bytes{$k});
    die "$file{$k}: digest $digest{$k} is not the one SHA256SUMS.txt lists\n" unless ($listed{$file{$k}} // '') eq $digest{$k};
}

# ---- a PDF's text: every Flate stream inflated, the strings of its Tj / TJ operators joined, whitespace collapsed --------------------------------
sub pdf_text {
    my ($d) = @_;
    my $out = '';
    while ($d =~ /stream\r?\n(.*?)endstream/sg) {
        my $s = $1;
        my $o = uncompress($s);
        if (!defined $o) { my ($i) = inflateInit(); ($o) = $i->inflate($s) if $i; }
        next unless defined $o && $o =~ /T[Jj]/;
        while ($o =~ /(\[(?:[^\]\\]|\\.)*\]\s*TJ|\((?:[^)\\]|\\.)*\)\s*Tj|T\*|Td|TD|ET)/sg) {
            my $t = $1;
            if ($t eq 'ET' || $t =~ /^T[\*dD]$/) { $out .= ' '; next; }
            while ($t =~ /\(((?:[^)\\]|\\.)*)\)|(-?\d+\.?\d*)/g) {
                if (defined $1) { my $x = $1; $x =~ s/\\(\d{3})/chr(oct $1)/ge; $x =~ s/\\(.)/$1/g; $out .= $x; }
                elsif ($2 < -200) { $out .= ' '; }
            }
        }
        $out .= ' ';
    }
    $out =~ s/\s+/ /g;
    return $out;
}

my @countries = (['Germany', 'DE'], ['France', 'FR'], ['Italy', 'IT'], ['Poland', 'PL'], ['Sweden', 'SE']);

# ---- CEPA: Annex G ----------------------------------------------------------------------------------------------------------------------------
my $cepa = pdf_text($bytes{cepa});
my @starts;
for my $t (1 .. 4) { $cepa =~ /Table G ?\. ?$t ?:/g or die "CEPA: Table G.$t not found\n"; push @starts, pos($cepa); pos($cepa) = undef; }
my %seg = (1 => substr($cepa, $starts[0], $starts[1] - $starts[0]), 2 => substr($cepa, $starts[1], $starts[2] - $starts[1]), 3 => substr($cepa, $starts[2], $starts[3] - $starts[2]));
die "CEPA: Table G.1 does not say its values are in 2015 euros\n" unless $seg{1} =~ /All values are in 2015 Euros/;
sub row {
    my ($text, $name, $n) = @_;
    $text =~ /\b\Q$name\E((?: (?:\d+\.\d\d|\*)){$n})(?![\d.])/ or die "CEPA: no row of $n values for $name\n";
    my @v = split ' ', $1;
    return @v;
}
my @sectors = ('Basic Metals', 'Chemical and Petrochemical', 'Non-Metallic Minerals', 'Food and Tobacco', 'Textile and Leather', 'Paper, Pulp and Print',
    'Wood and Wood Products', 'Transport Equipment', 'Machinery', 'Construction', 'Transport', 'Agriculture, Forestry and Fishing', 'Services');
# the energy balance's rows per CEPA sector (Eurostat's final-consumption breakdown, energy use)
my @rows = (['FC_IND_IS_E', 'FC_IND_NFM_E'], ['FC_IND_CPC_E'], ['FC_IND_NMM_E'], ['FC_IND_FBT_E'], ['FC_IND_TL_E'], ['FC_IND_PPP_E'], ['FC_IND_WP_E'],
    ['FC_IND_TE_E'], ['FC_IND_MAC_E'], ['FC_IND_CON_E'], ['FC_TRA_E'], ['FC_OTH_AF_E', 'FC_OTH_FISH_E'], ['FC_OTH_CP_E']);

# ---- Eurostat: the 2015 electricity consumption by sector -------------------------------------------------------------------------------------
sub jstat {
    my ($json) = @_;
    my $j = decode_json($json);
    my @id = @{$j->{id}}; my @size = @{$j->{size}};
    return sub {
        my (%at) = @_;
        my $flat = 0;
        for my $i (0 .. $#id) {
            my $idx = $j->{dimension}{$id[$i]}{category}{index};
            my $code = $at{$id[$i]} // (keys %$idx)[0];
            die "Eurostat: $id[$i] has no $code\n" unless defined $idx->{$code};
            $flat = $flat * $size[$i] + $idx->{$code};
        }
        return $j->{value}{$flat};
    };
}
my $bal = jstat($bytes{bal});
my $hicp = jstat($bytes{hicp});

# ---- the ECB's annual reference rates, national currency per euro ------------------------------------------------------------------------------
my %fx;
for my $line (split /\r?\n/, $bytes{fx}) {
    my @c = split /,/, $line;
    next unless @c > 7 && $c[0] =~ /^EXR\.A\.(PLN|SEK)\.EUR\.SP00\.A$/;
    die "ECB: row $c[0] is not annual, euro-denominated, spot\n" unless $c[1] eq 'A' && $c[3] eq 'EUR' && $c[4] eq 'SP00' && $c[5] eq 'A';
    $fx{$c[2]}{$c[6]} = $c[7];
}
my %currency = (PL => 'PLN', SE => 'SEK');

my %out;
for my $c (@countries) {
    my ($name, $geo) = @$c;
    my ($domestic) = row($seg{1}, $name, 2);
    my @nd = (row($seg{2}, $name, 7), row($seg{3}, $name, 6));
    my ($sum, $gwh, $kept) = (0, 0, 0);
    my @detail;
    for my $s (0 .. $#sectors) {
        my $g = 0;
        for my $r (@{$rows[$s]}) { my $v = $bal->(nrg_bal => $r, geo => $geo); die "Eurostat: $geo $r has no value\n" unless defined $v; $g += $v; }
        if ($nd[$s] eq '*') { push @detail, "$sectors[$s] * (left out, $g GWh)"; next; }
        $sum += $nd[$s] * $g; $gwh += $g; $kept++;
        push @detail, "$sectors[$s] $nd[$s] x $g";
    }
    my $h15 = $hicp->(geo => $geo, time => '2015'); my $h23 = $hicp->(geo => $geo, time => '2023');
    die "HICP: $geo lacks 2015 or 2023\n" unless defined $h15 && defined $h23;
    my ($fx15, $fx23) = (1, 1);
    if (my $cur = $currency{$geo}) { $fx15 = $fx{$cur}{2015}; $fx23 = $fx{$cur}{2023}; die "ECB: $cur lacks 2015 or 2023\n" unless defined $fx15 && defined $fx23; }
    $out{$name} = { domestic => $domestic, nondomestic => $sum / $gwh, gwh => $gwh, kept => $kept, from => $h15, to => $h23, fx15 => $fx15, fx23 => $fx23, detail => join('; ', @detail) };
}

# ---- LBNL: Table ES-1 ---------------------------------------------------------------------------------------------------------------------------
my $lbnl = pdf_text($bytes{lbnl});
$lbnl =~ /Table ES ?- ?1: Estimated Interruption Cost per Event, Average kW and Unserved kWh \( ?U\.S\. ?2013\$\) by Duration and Customer Class Interruption Cost Interruption Duration Momentary 30 Minutes 1 Hour 4 Hours 8 Hours 16 Hours (.*?) Table ES ?- ?2/
    or die "LBNL: Table ES-1 not found\n";
my $es1 = $1;
sub per_kwh {
    my ($class) = @_;
    $es1 =~ /\Q$class\E.*?Cost per Unserved kWh((?: \$[\d,]+\.\d){6})/ or die "LBNL: no cost-per-unserved-kWh row for $class\n";
    my @v = map { s/[\$,]//g; $_ } split ' ', $1;
    return $v[5];   # 16 hours, the longest duration the study estimates
}
my $ci = per_kwh('Medium and Large C&I');
my $res = per_kwh('Residential');
sub m13 {
    my ($json, $year) = @_;
    my $j = decode_json($json);
    for my $s (@{$j->{Results}{series}}) { for my $r (@{$s->{data}}) { return $r->{value} if $r->{year} eq $year && $r->{period} eq 'M13'; } }
    die "BLS: no annual average for $year\n";
}
my $cpi13 = m13($bytes{cpi13}, '2013');
my $cpi23 = m13($bytes{cpi23}, '2023');

# ---- the file -----------------------------------------------------------------------------------------------------------------------------------
my $o = '';
$o .= "// GENERATED by Tools/voll_prep.pl. DO NOT EDIT BY HAND.\n//\n";
for my $k (sort keys %file) { $o .= sprintf "// %-6s %s%s  sha256 %s\n", $k, $dir, $file{$k}, $digest{$k}; }
$o .= "//\n// EnergyReliabilityDiagnostic re-runs this tool's arithmetic on the generated figures (the bridge and the weighting).\n\nusing System.Collections.Generic;\n\nnamespace PoliSim.Data.Generated\n{\n";
$o .= "    /// <summary>MF q26 (rulings q26 and K145): the value of lost load per country, per kWh in the study's currency and prices, and carried to 2023 - the energy\n";
$o .= "    /// layer's catalog year, the seed's prices (price level 1). The EU five: CEPA for ACER (2018), the NON-DOMESTIC VoLL - the thirteen sectors of Tables G.2-G.3\n";
$o .= "    /// weighted by Eurostat's 2015 electricity consumption of each (nrg_bal_c) - and the domestic VoLL of Table G.1 beside it; 2015 euros, carried by the\n";
$o .= "    /// country's HICP in its own currency (krona and zloty through the ECB annual rates). The USA: LBNL's ICE (Sullivan et al. 2015), Table ES-1, medium and large C&amp;I at 16 hours, and residential beside it; 2013 dollars,\n";
$o .= "    /// carried by the CPI-U. Generated, never hand-edited.</summary>\n";
$o .= "    public static class ValueOfLostLoadData\n    {\n";
$o .= "        public const string SourceFolder = \"$dir\";\n";
$o .= "        /// <summary>One country's figures: the output VoLL (non-domestic; C&amp;I for the USA) and the domestic one, per kWh in the study's currency and price year;\n";
$o .= "        /// the price index of that year and of 2023 (the country's own currency's); the ECB's annual rate, national currency per euro, in that year and in 2023 (1 where the\n";
$o .= "        /// country's currency is the market's); the consumption the EU weighting summed, GWh, and how many of CEPA's thirteen sectors it kept (0 for the USA).</summary>\n";
$o .= "        public static readonly Dictionary<CountryId, (double OutputPerKwh, double DomesticPerKwh, string Currency, int PriceYear, double IndexThen, double Index2023, double FxThen, double Fx2023, double WeightGwh, int SectorsKept)> Rows =\n";
$o .= "            new Dictionary<CountryId, (double OutputPerKwh, double DomesticPerKwh, string Currency, int PriceYear, double IndexThen, double Index2023, double FxThen, double Fx2023, double WeightGwh, int SectorsKept)>\n        {\n";
for my $c (@countries) {
    my $r = $out{$c->[0]};
    $o .= "            // $c->[0]: $r->{detail}\n";
    $o .= sprintf "            { CountryId.%s, (%.6f, %s, \"EUR\", 2015, %s, %s, %s, %s, %s, %d) },\n", $c->[0], $r->{nondomestic}, $r->{domestic}, $r->{from}, $r->{to}, $r->{fx15}, $r->{fx23}, $r->{gwh}, $r->{kept};
}
$o .= sprintf "            { CountryId.USA, (%s, %s, \"USD\", 2013, %s, %s, 1, 1, 0, 0) },\n", $ci, $res, $cpi13, $cpi23;
$o .= "        };\n    }\n}\n";
open my $w, '>:raw', $cs or die "$cs: $!";
print $w $o;
close $w;
print "wrote $cs\n";
for my $c (@countries) { my $r = $out{$c->[0]}; printf "%-8s output %.4f EUR/kWh (2015; %d sectors, %.0f GWh), domestic %s; HICP %s -> %s; ECB %s -> %s; output in 2023 prices %.4f EUR/kWh (back at the ECB 2023 rate)\n", $c->[0], $r->{nondomestic}, $r->{kept}, $r->{gwh}, $r->{domestic}, $r->{from}, $r->{to}, $r->{fx15}, $r->{fx23}, $r->{nondomestic} * $r->{fx15} * $r->{to} / $r->{from} / $r->{fx23}; }
printf "USA      output %s USD/kWh (2013, M&L C&I 16 h), residential %s; CPI-U %s -> %s; output in 2023 prices %.4f\n", $ci, $res, $cpi13, $cpi23, $ci * $cpi23 / $cpi13;
