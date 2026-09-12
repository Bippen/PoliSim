#!/usr/bin/perl
# pension_prep.pl <sources-dir> <out-dir>
#
# S1 of the backlog plan (POLISIM_BACKLOG_PLAN.md, ruled COMPLETED.md §474): the pension-age and pension-payment data files for PN-1 and PN-2 -
# DERIVED from the sources on disk, never typed. Every input is a file the README lists by digest under PoliSim-captures/sources/pensions/.
# Nothing in the runtime reads these files until PN-1's statute class does.
#
#   statutory_ages.csv        six countries: the statutory retirement age's rule and its current figure, each EXTRACTED by pattern from the
#                             statute's own page (or the authority's, where the code article is not reachable) - a pattern that does not match
#                             stops this script; nothing is filled from memory
#   oecd_pag_2024.csv         OECD Pensions at a Glance (DSD_PAG@DF_PAG, SDMX): current and future normal retirement age for a labour-market
#                             entrant at 22 (CRPLF22, FRPLF22), the gross and net replacement rates at the average wage (GPRR100, NPRR100),
#                             the effective labour-market exit age (ELMEA), public pension expenditure as a share of GDP (PEP) - by sex, latest year
#   replacement_ratio_2024.csv  Eurostat ilc_pnp3: the aggregate replacement ratio (the median pension of 65–74 over the median earnings of
#                             50–59, excluding other social benefits) for the five, 2024 - PN-2's seed gate for the five; the USA's gate is OECD's
use strict; use warnings; use utf8;
use JSON::PP;
binmode STDOUT, ':encoding(UTF-8)';

my ($src, $out) = @ARGV; die "usage: pension_prep.pl <sources-dir> <out-dir>\n" unless $src && $out;
mkdir $out unless -d $out;
sub slurp { my ($p) = @_; open my $in, '<:raw', $p or die "$p: $!"; local $/; my $t = <$in>; close $in; return $t; }
sub writecsv { my ($p, @lines) = @_; open my $o, '>:raw:encoding(UTF-8)', $p or die "$p: $!"; print $o map { "$_\n" } @lines; close $o; print "wrote $p (", scalar(@lines), " lines)\n"; }
# a page's text: tags stripped, the entities the six pages use decoded, whitespace folded
sub text {
    my ($file) = @_; my $t = slurp($file); utf8::decode($t);
    $t =~ s/<script.*?<\/script>//gs; $t =~ s/<style.*?<\/style>//gs; $t =~ s/<[^>]+>/ /g;
    my %ent = ('&#167;' => '§', '&#252;' => 'ü', '&#246;' => 'ö', '&#228;' => 'ä', '&#223;' => 'ß', '&#160;' => ' ', '&nbsp;' => ' ', '&#39;' => "'", '&#8211;' => '-', '&amp;' => '&', '&#233;' => 'é', '&#224;' => 'à');
    $t =~ s/(&#?\w+;)/exists $ent{$1} ? $ent{$1} : $1/ge; $t =~ s/\s+/ /g; $t =~ s/ ([,.;])/$1/g; return $t;
}
# Eurostat JSON-stat: the value index is the row-major position over the dimensions in "id" order; unnamed dimensions take index 0
sub eurostat {
    my ($file) = @_; my $j = decode_json(slurp($file)); my @ids = @{ $j->{id} }; my @size = @{ $j->{size} };
    return sub {
        my (%want) = @_; my $pos = 0;
        for my $k (0..$#ids) { my $d = $ids[$k]; my $i = exists $want{$d} ? $j->{dimension}{$d}{category}{index}{ $want{$d} } : 0; die "no $d=$want{$d}\n" unless defined $i; $pos = $pos * $size[$k] + $i; }
        return $j->{value}{$pos};
    };
}
sub must { my ($text, $re, $what) = @_; my @m = $text =~ $re; die "$what: the pattern did not match - nothing is filled from memory; re-read the source\n" unless @m; return @m; }
sub csvq { my ($s) = @_; $s =~ s/"/""/g; return "\"$s\""; }

# ---- 1. statutory_ages.csv
my @st = ('country,rule,current_age_years,path,quote,source_file,fetched');
{   # Sweden - riktålder: the formula (SFB 2 kap. 10 b §), the six-year lag (10 c §), the figure in force (Pensionsmyndigheten)
    my $law = text("$src/pensions/se_sfb_2010_110.html");
    my ($formula) = must($law, qr/(Riktålder för pension räknas fram genom att det till bastalet 65 läggs 2\/3 av differensen mellan.*?avrundas till närmaste helår\.)/, 'SE 10 b §');
    my ($lag) = must($law, qr/(Den beräknade riktåldern för pension ska gälla för det sjätte året efter beräkningsåret\.)/, 'SE 10 c §');
    my $pm = text("$src/pensions/se_pensionsmyndigheten_riktalder.html");
    my ($from, $to, $age) = must($pm, qr/Riktåldern som gäller från (\d{4}) till (\d{4}) är (\d+) år/, 'SE riktålder in force');
    push @st, join(',', 'Sweden', 'LifeExpectancyIndexed', $age, csvq("riktålder $age from $from to $to (Pensionsmyndigheten); recalculated by SFB 2 kap. 10 b § - 65 + 2/3 of the gain in remaining life expectancy at 65 since 1994, rounded to whole years - and in force the sixth year after its calculation (10 c §)"), csvq("$formula $lag"), 'se_sfb_2010_110.html; se_pensionsmyndigheten_riktalder.html', '2026-09-12');
}
{   # Germany - SGB VI § 35 (the rule: 67) and § 235 (the transition by birth year to 1963)
    my $s35 = text("$src/pensions/de_sgb6_35.html");
    my ($rule) = must($s35, qr/(Die Regelaltersgrenze wird mit Vollendung des 67\. Lebensjahres erreicht\.)/, 'DE § 35');
    my $s235 = text("$src/pensions/de_sgb6_235.html");
    my ($trans) = must($s235, qr/(Versicherte, die vor dem 1\. Januar 1964 geboren sind, haben Anspruch auf Regelaltersrente, wenn sie.*?Wartezeit erfüllt haben\.)/, 'DE § 235 Abs. 1');
    my @tbl = $s235 =~ /(196[0-3]) (\d+) (66) (\d+)/g;   # the table's last rows: Geburtsjahr, Anhebung um Monate, auf Alter Jahr, Monat
    die "DE § 235 table not read\n" unless @tbl >= 4;
    push @st, join(',', 'Germany', 'Scheduled', 67, csvq("67 for those born 1964 or later (§ 35); § 235 Abs. 2 raises it by birth year from 65 (1946) to 66 years 10 months (1963) - the last cohort reaches 67 in 2031"), csvq("$rule $trans"), 'de_sgb6_35.html; de_sgb6_235.html', '2026-09-12');
}
{   # France - CSS L161-17-2 as the authority states the schedule (service-public.gouv.fr; the code article itself is not reachable from this machine)
    my $fr = text("$src/pensions/fr_service_public_F14043.html");
    my ($final) = must($fr, qr/À partir du 1\s*er\s*janvier 1969 (64 ans)/, 'FR final step');
    my @steps = $fr =~ /(196[5-8]) (63 ans(?: et \d+ mois)?)/g; die "FR schedule not read\n" unless @steps >= 6;
    my ($basis) = must($fr, qr/(Code de la sécurité sociale : article L161-17-2)/, 'FR legal basis');
    push @st, join(',', 'France', 'Scheduled', 64, csvq("64 for those born from 1 January 1969; from 62 years 9 months (born 1963 to March 1965) rising three months a birth-year: 63 (April-December 1965), 63 y 3 m (1966), 63 y 6 m (1967), 63 y 9 m (1968) - loi n° 2023-270, CSS L161-17-2"), csvq("$basis; $final; " . join(' ', @steps)), 'fr_service_public_F14043.html', '2026-09-12');
}
{   # Italy - DL 201/2011 art. 24 as INPS states the life-expectancy adjustment for 2027-2028 (the code text itself is not reachable from this machine)
    my $it = text("$src/pensions/it_inps_2027_2028.html");
    my ($q) = must($it, qr/(La pensione di vecchiaia slitterà a 67 anni e un mese nel 2027 e a 67 anni e 3 mesi nel 2028)/, 'IT 2027-2028');
    push @st, join(',', 'Italy', 'LifeExpectancyIndexed', 67, csvq("67 through 2026; 67 years 1 month in 2027, 67 years 3 months in 2028 (the ISTAT life-expectancy adjustment under art. 24 DL 201/2011, attenuated by decree); indexed every two years"), csvq($q), 'it_inps_2027_2028.html', '2026-09-12');
}
{   # Poland - art. 24 ust. 1 of the ustawa o emeryturach i rentach z FUS: 60 women, 65 men, fixed
    my $pl = text("$src/pensions/pl_arslege_art24.html");
    my ($q) = must($pl, qr/(Ubezpieczonym urodzonym po dniu 31 grudnia 1948 r\. przysługuje emerytura po osiągnięciu wieku emerytalnego wynoszącego co najmniej 60 lat dla kobiet i co najmniej 65 lat dla mężczyzn)/, 'PL art. 24');
    push @st, join(',', 'Poland', 'Fixed', 65, csvq("65 for men and 60 for women, fixed (art. 24 ust. 1; the model's one age reads the men's, the women's stated as the deviation)"), csvq($q), 'pl_arslege_art24.html', '2026-09-12');
}
{   # the USA - Social Security Act § 216(l), 42 U.S.C. 416(l): 67 for those attaining early retirement age after 2021
    my $us = text("$src/pensions/us_42usc416.html");
    my ($q) = must($us, qr/(who attains early retirement age after December 31, 2021\s*,\s*67 years of age)/, 'US 416(l)(1)(E)');
    push @st, join(',', 'United States', 'Scheduled', 67, csvq("67 for those attaining early retirement age (62) after 2021 - born 1960 or later, the full cohort from 2027; 66 plus two months a year for 2017-2021 (42 U.S.C. 416(l)(1))"), csvq($q), 'us_42usc416.html', '2026-09-12');
}
writecsv("$out/statutory_ages.csv", @st);

# ---- 2. oecd_pag_2024.csv - the latest observation per country × measure × sex
my %iso = (DEU => 'Germany', FRA => 'France', ITA => 'Italy', POL => 'Poland', SWE => 'Sweden', USA => 'United States');
my (%last, %val, %unit);
open my $pag, '<:raw:encoding(UTF-8)', "$src/pensions/oecd_pag_2023_six.csv" or die;
my $head = <$pag>;
while (my $line = <$pag>) {
    chomp $line; my @f = map { s/^"|"$//g; $_ } split /,(?=(?:[^"]*"[^"]*")*[^"]*$)/, $line;
    my ($area, $measure, $unitm, $sex, $year, $value) = @f[4, 8, 10, 12, 18, 20];
    next unless $iso{$area} && $measure =~ /^(CRPLF22|FRPLF22|GPRR100|NPRR100|ELMEA|PEP)$/ && $value ne '';
    my $k = "$area|$measure|$sex"; if (!defined $last{$k} || $year > $last{$k}) { $last{$k} = $year; $val{$k} = $value; $unit{$k} = $unitm; }
}
close $pag;
my @pg = ('country,measure,sex,year,value,unit,source');
for my $k (sort keys %last) { my ($a, $m, $s) = split /\|/, $k; push @pg, join(',', $iso{$a}, $m, $s, $last{$k}, $val{$k}, $unit{$k}, 'OECD Pensions at a Glance DSD_PAG@DF_PAG (SDMX, fetched 2026-09-12)'); }
die "PAG: fewer than 60 rows read\n" unless @pg > 60;
writecsv("$out/oecd_pag_2024.csv", @pg);

# ---- 3. replacement_ratio_2024.csv - Eurostat ilc_pnp3, sex T
my $pnp3 = eurostat("$src/pensions/ilc_pnp3_2024.json");
my @rr = ('geo,country,aggregate_replacement_ratio,source');
my %country = (DE => 'Germany', FR => 'France', IT => 'Italy', PL => 'Poland', SE => 'Sweden');
my @geo = qw(DE FR IT PL SE);
for my $g (@geo) {
    my $v = $pnp3->(geo => $g); die "ilc_pnp3: no value for $g\n" unless defined $v;
    push @rr, join(',', $g, $country{$g}, $v, 'Eurostat ilc_pnp3 2024 sex T (the median individual gross pension of 65-74 over the median gross earnings of 50-59)');
}
writecsv("$out/replacement_ratio_2024.csv", @rr);

# ---- 4. average_pension_2022.csv - ESSPROS: expenditure (spr_exp_pens, MIO_EUR, all schemes) over beneficiaries (spr_pns_ben, persons, sex T),
#         for old-age pensions alone (OLD) and for every pension type together (TOTAL) - the DERIVED average benefit per beneficiary per year,
#         PN-2's seed gate for the five in the unit the readout prints (the line over its headcount). ESSPROS counts a person once in TOTAL.
my $exp = eurostat("$src/pensions/spr_exp_pens_2022.json"); my $ben = eurostat("$src/pensions/spr_pns_ben_2022.json");
my @ap = ('geo,country,pension_type,expenditure_mio_eur,beneficiaries,average_eur_per_year,source');
for my $g (@geo) {
    # the beneficiary file's "total old age" (OLD_TOT) is old-age + anticipated old-age + partial pensions, counted once; the expenditure file has the three parts
    for my $type (qw(OLD_TOT TOTAL)) {
        my @parts = $type eq 'OLD_TOT' ? qw(OLD AOLD PART) : qw(TOTAL); my $e = 0; my (@summed, @absent);
        for my $p (@parts) { my $v = $exp->(geo => $g, spdepb => $p, spdepm => 'TOTAL', unit => 'MIO_EUR'); if (defined $v) { $e += $v; push @summed, $p } else { push @absent, $p } }
        die "ESSPROS $g expenditure $type: nothing reported\n" unless @summed; die "ESSPROS $g: the old-age line itself is missing\n" if $type eq 'OLD_TOT' && !grep { $_ eq 'OLD' } @summed;
        my $b = $ben->(geo => $g, spdepb => $type, spdepm => 'TOTAL', sex => 'T'); die "ESSPROS $g beneficiaries $type missing\n" unless defined $b && $b > 0;
        my $parts = join('+', @summed) . (@absent ? ' (' . join('+', @absent) . ' not reported)' : '');
        push @ap, join(',', $g, $country{$g}, $type eq 'OLD_TOT' ? 'old-age pensions (old-age + anticipated + partial)' : 'all pension types', sprintf('%.2f', $e), $b, sprintf('%.0f', $e * 1e6 / $b), "\"Eurostat spr_exp_pens (MIO_EUR, means-tested and not; parts $parts) over spr_pns_ben (persons, sex T, all schemes), 2022\"");
    }
}
writecsv("$out/average_pension_2022.csv", @ap);
