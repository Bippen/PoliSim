#!/usr/bin/perl
# pbr_prep.pl [--dry | --check]   (run from the project root)
#
# pbr (K293's filing; Reviews/2026-10-30_pbr_prereg.md): THE PENSION BENEFIT RATIO, every country's path. This tool DERIVES every figure the model and
# its diagnostic read from files saved byte-exact (each held against its folder's SHA256SUMS.txt, which it checks) and writes
#   Assets/Scripts/Data/Generated/PensionBenefitRatioPath.cs     - B, the benefit ratio by calendar year (the model reads it: PensionBenefitRatio)
#   Assets/Editor/Generated/PensionBenefitRatioAnchors.cs         - the sources' expenditure and coverage paths ((A1)/(A2): PensionBenefitRatioDiagnostic)
# never typing a figure. --dry prints the summary and writes nothing; --check writes nothing and says whether the files on disk are what this tool
# writes (exit 1 where either differs).
# What it reads, and how:
#   - The EU five: the 2024 Ageing Report's statistical annex (ElectionsData/macro/raw/q29_longrun/), sheet "cross-country_tables" (xl/worksheets/
#     sheet4.xml), one figure a year 2022-2070, rows SE DE FR IT PL: Table II.1.79 "Benefit ratio (total public pensions, gross, %)" - B; Table II.1.64
#     "Public pensions, gross expenditure as % of GDP"; and the coverage ratio, derived as Table II.1.76 (pensioners, thousands) / (Table II.1.8
#     (population, millions) x Table II.1.11 (share 65+, %) x 10) x 100. Cross-checks, through `pdftotext -raw` (Git Bash's mingw64 build), against
#     Institutional Paper 279 (ElectionsData/macro/raw/k276_pension_age/): Table I.1.12 (coverage ratio, PDF page 53, 2022-2070 by decade) and Table
#     I.1.16 (benefit ratio, public pensions: total, PDF page 56, 2022 2045 2070) - each within its printed tenth (0.05 + 1e-6).
#   - The USA: CBO's 2026 Long-Term Projections for Social Security (publication 62556; ElectionsData/usa/raw/pbr_social_security/): the projections
#     workbook's sheet "3" (Table 3, outlays with scheduled benefits, % of GDP, calendar years) and sheet "2" (Table 2, GDP, trillions); the
#     additional-information workbook's "1. Covered Workers" (total, thousands), "2. Covered and Taxable Earnings" (covered earnings, trillions) and
#     "3. OASDI Beneficiaries" (total, thousands), 2026-2100. B is DERIVED: the average benefit (outlays x GDP / beneficiaries) over the average
#     covered wage (covered earnings / covered workers), per cent. The coverage-like ratio is the beneficiaries over CBO's 65 or older (LTBO 2026,
#     ElectionsData/usa/raw/k274_fiscal_reaction/, sheet "11. Pop, by Age Group", millions), 2026-2056. The expenditure path is Table 3's outlays,
#     2022-2100 (actual to 2025).
# A table title not matched exactly once, a country carried twice, a year missing, a header not where it is named, a B outside 5..100 or a cross-check
# outside its tenth stops the script (q29's and K276's discipline). ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw(unzip $UnzipError);

my $mode = $ARGV[0] // '';
die "usage: pbr_prep.pl [--dry | --check]\n" unless $mode eq '' || $mode eq '--dry' || $mode eq '--check';
die "run from the project root\n" unless -d 'Assets/Scripts/Data/Generated' && -d 'Assets/Editor/Generated';
my $outPath = 'Assets/Scripts/Data/Generated/PensionBenefitRatioPath.cs';
my $outAnchors = 'Assets/Editor/Generated/PensionBenefitRatioAnchors.cs';
my @EU = (['SE', 'Sweden'], ['DE', 'Germany'], ['FR', 'France'], ['IT', 'Italy'], ['PL', 'Poland']);
my ($EU_FIRST, $EU_LAST, $US_FIRST, $US_LAST, $US_EXP_FIRST, $US_COV_LAST) = (2022, 2070, 2026, 2100, 2022, 2056);

sub slurp { my ($p) = @_; open my $fh, '<:raw', $p or die "$p: $!\n"; local $/; my $s = <$fh>; close $fh; return $s; }
sub must { my ($v, $what) = @_; die "pbr_prep: $what not found\n" unless defined $v; return $v; }

# ---- the sources, each held against its folder's SHA256SUMS.txt
my @SRC = (
    ['annex', 'ElectionsData/macro/raw/q29_longrun', 'ec_2024_Ageing_Report-Statistical_annex_all_horizontal_tables.xlsx'],
    ['ip279', 'ElectionsData/macro/raw/k276_pension_age', 'ec_2024_Ageing_Report_ip279_en.pdf'],
    ['cbo', 'ElectionsData/usa/raw/pbr_social_security', 'wayback_20260921152701_cbo_62556-2026-Long-Term-Social-Security-Projections.xlsx'],
    ['cboadd', 'ElectionsData/usa/raw/pbr_social_security', 'wayback_20260919143154_cbo_62556-2026-Additional-Info.xlsx'],
    ['ltbo', 'ElectionsData/usa/raw/k274_fiscal_reaction', 'wayback_20260923101450_cbo_62044-2026-LTBO.xlsx'],
);
my (%path, %digest);
for my $s (@SRC) {
    my ($k, $dir, $f) = @$s;
    my %sums;
    for (split /\n/, slurp("$dir/SHA256SUMS.txt")) { s/\r$//; $sums{$2} = $1 if /^([0-9a-f]{64}) \*?(\S+)$/; }
    $path{$k} = "$dir/$f";
    $digest{$k} = sha256_hex(slurp($path{$k}));
    die "$f: its SHA-256 $digest{$k} is not the one $dir/SHA256SUMS.txt lists\n" unless ($sums{$f} // '') eq $digest{$k};
}

# ---- xlsx: shared strings and one sheet's cells (q29_longrun_prep.pl's reader); a sheet by its NAME through the workbook's relationships
sub member { my ($zip, $name) = @_; my $o; unzip($zip => \$o, Name => $name) or die "$zip $name: $UnzipError\n"; return $o; }
sub sheetFile {
    my ($zip, $name) = @_;
    my $wb = member($zip, 'xl/workbook.xml');
    my $rels = member($zip, 'xl/_rels/workbook.xml.rels');
    (my $esc = $name) =~ s/&/&amp;/g;
    my @m = $wb =~ m{<sheet name="\Q$esc\E"[^>]*r:id="(rId\d+)"}g;
    die "$zip: sheet '$name' found " . scalar(@m) . " times, not once\n" unless @m == 1;
    for my $rel ($rels =~ m{<Relationship [^>]*>}g) {
        next unless $rel =~ m{Id="\Q$m[0]\E"};
        return $1 if $rel =~ m{Target="(?:/xl/)?(worksheets/[^"]+)"};
    }
    die "$zip: no worksheet for $m[0]\n";
}
sub sheet {
    my ($zip, $file) = @_;
    my @ss;
    my $sst = eval { member($zip, 'xl/sharedStrings.xml') };
    @ss = map { my $t = $_; $t =~ s/<[^>]+>//g; $t =~ s/\s+/ /g; $t =~ s/^ | $//g; $t =~ s/&amp;/&/g; $t } ($sst =~ m{<si>(.*?)</si>}gs) if defined $sst;
    my $x = member($zip, "xl/$file");
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

# ---- the Ageing Report's cross-country sheet
my $ar = sheet($path{annex}, 'worksheets/sheet4.xml');
my $lastRow = (sort { $b <=> $a } keys %$ar)[0];
sub arTable {
    my ($title, $lo, $hi) = @_;
    my @rt = grep { ($ar->{$_}{B} // '') =~ /^\Q$title\E$/ } keys %$ar;
    die "Ageing Report: '$title' matches " . scalar(@rt) . " titles, not one\n" unless @rt == 1;
    my $hr = $rt[0] + 2;
    my %yearCol;
    for my $col (keys %{ $ar->{$hr} }) { $yearCol{ $ar->{$hr}{$col} } = $col if $ar->{$hr}{$col} =~ /^20\d\d$/; }
    die "Ageing Report: $title years\n" unless $yearCol{$EU_FIRST} && $yearCol{$EU_LAST};
    my %out;
    for my $r ($hr + 1 .. $lastRow) {
        last if index($ar->{$r}{B} // '', 'Table ') == 0;
        my $code = $ar->{$r}{B} // next;
        my ($row) = grep { $_->[0] eq $code } @EU;
        next unless $row;
        die "Ageing Report: $title carries $code twice\n" if exists $out{ $row->[1] };
        $out{ $row->[1] } = [ map { my $v = must($ar->{$r}{ must($yearCol{$_}, "$title year $_") }, "$title $code $_"); die "$title $code $_ reads $v, outside $lo..$hi\n" if $v < $lo || $v > $hi; $v } $EU_FIRST .. $EU_LAST ];
    }
    die "Ageing Report: $title has " . scalar(keys %out) . " of the five\n" unless keys %out == 5;
    return \%out;
}
my $brEu = arTable('Table II.1.79: Benefit ratio (total public pensions, gross, %)', 5, 100);
my $expEu = arTable('Table II.1.64: Public pensions, gross expenditure as % of GDP', 0.5, 50);
my $pens = arTable('Table II.1.76: Pensioners (public, in thousands)', 100, 1e6);
my $pop = arTable('Table II.1.8: Population (million)', 1, 1000);
my $s65 = arTable('Table II.1.11: Share of elderly population (+65y) as % of total population', 1, 100);
my %covEu;
for my $n (map { $_->[1] } @EU) { $covEu{$n} = [ map { 100 * $pens->{$n}[$_] / ($pop->{$n}[$_] * $s65->{$n}[$_] * 10) } 0 .. $EU_LAST - $EU_FIRST ]; }

# ---- the cross-checks: Institutional Paper 279, Tables I.1.12 and I.1.16
sub pdfPage {
    my ($page) = @_;
    open my $p, '-|', 'pdftotext', '-f', $page, '-l', $page, '-raw', $path{ip279}, '-' or die "pdftotext: $!\n";
    binmode $p; local $/; my $t = <$p>; close $p or die "pdftotext page $page failed (is Git Bash's mingw64 pdftotext on the PATH?)\n";
    $t =~ s/\r//g;
    return [ split /\n/, $t ];
}
my %code2name = map { $_->[0] => $_->[1] } @EU;
my $checked = 0;
{
    my $l = pdfPage(53);
    my @t = grep { $l->[$_] eq 'Table I.1.12: Coverage ratio (%)' } 0 .. $#$l;
    die "IP279 page 53: Table I.1.12's title found " . scalar(@t) . " times, not once\n" unless @t == 1;
    my @h = grep { $l->[$_] eq '2022 2030 2040 2050 2060 2070 2022-70' } 0 .. $#$l;
    die "IP279 page 53: Table I.1.12's year row found " . scalar(@h) . " times, not once\n" unless @h == 1;
    my %seen;
    for my $i ($h[0] + 1 .. $#$l) {
        last if $l->[$i] =~ /^2022-30 /;
        next unless $l->[$i] =~ /^(SE|DE|FR|IT|PL) ((?:-?\d+\.\d ){6})-?\d+\.\d$/;
        my ($c, @v) = ($1, split / /, $2);
        die "IP279 Table I.1.12 carries $c twice\n" if $seen{$c}++;
        my @y = (2022, 2030, 2040, 2050, 2060, 2070);
        for my $k (0 .. 5) {
            my $mine = $covEu{ $code2name{$c} }[ $y[$k] - $EU_FIRST ];
            die sprintf("coverage %s %d: derived %.3f, Table I.1.12 %s\n", $c, $y[$k], $mine, $v[$k]) if abs($mine - $v[$k]) > 0.05 + 1e-6;
            $checked++;
        }
    }
    die "IP279 Table I.1.12: " . scalar(keys %seen) . " of the five\n" unless keys %seen == 5;
}
{
    my $l = pdfPage(56);
    my @t = grep { $l->[$_] eq 'Table I.1.16: Benefit ratio: 2022, 2045 and 2070 (%)' } 0 .. $#$l;
    die "IP279 page 56: Table I.1.16's title found " . scalar(@t) . " times, not once\n" unless @t == 1;
    my %seen;
    for my $i ($t[0] + 1 .. $#$l) {
        next unless $l->[$i] =~ /^(SE|DE|FR|IT|PL) (.*) \1$/;
        my ($c, @v) = ($1, split / /, $2);
        die "IP279 Table I.1.16 carries $c twice\n" if $seen{$c}++;
        my @y = (2022, 2045, 2070);
        for my $k (0 .. 2) {   # "Public pensions: total" - the row's fifth to seventh figures
            my $mine = $brEu->{ $code2name{$c} }[ $y[$k] - $EU_FIRST ];
            die sprintf("benefit ratio %s %d: II.1.79 %.3f, Table I.1.16 %s\n", $c, $y[$k], $mine, $v[4 + $k]) if abs($mine - $v[4 + $k]) > 0.05 + 1e-6;
            $checked++;
        }
    }
    die "IP279 Table I.1.16: " . scalar(keys %seen) . " of the five\n" unless keys %seen == 5;
}

# ---- CBO: a sheet's title row, its header row, and one column by its header, by calendar year
sub cboColumn {
    my ($zip, $sheetName, $title, $header, $first, $last) = @_;
    my $c = sheet($zip, sheetFile($zip, $sheetName));
    my @tr = grep { ($c->{$_}{A} // '') eq $title } keys %$c;
    die "$sheetName: title '$title' found " . scalar(@tr) . " times, not once\n" unless @tr == 1;
    my ($hr, $col);
    for my $r ($tr[0] + 1 .. $tr[0] + 4) {
        next unless $c->{$r};
        my @hit = grep { ($c->{$r}{$_} // '') eq $header } keys %{ $c->{$r} };
        if (@hit == 1) { ($hr, $col) = ($r, $hit[0]); last; }
    }
    die "$sheetName: header '$header' not found once under its title\n" unless defined $hr;
    my %out;
    for my $r ($hr + 1 .. $hr + 200) {
        my $y = $c->{$r}{A} // next;
        next unless $y =~ /^\d{4}$/;
        die "$sheetName: year $y twice\n" if exists $out{$y};
        $out{$y} = $c->{$r}{$col};
    }
    for my $y ($first .. $last) { die "$sheetName: '$header' has no $y\n" unless defined $out{$y} && $out{$y} =~ /^-?\d+(\.\d+)?(E-?\d+)?$/; }
    return \%out;
}
my $usOut = cboColumn($path{cbo}, '3', "3. Social Security's Tax Revenues and Outlays, With Scheduled Benefits, Measured as a Percentage of Gross Domestic Product", 'Outlays', $US_EXP_FIRST, $US_LAST);
my $usGdp = cboColumn($path{cbo}, '2', '2. Projected Gross Domestic Product and Taxable Payroll', 'Gross domestic product', $US_FIRST, $US_LAST);
my $usWork = cboColumn($path{cboadd}, '1. Covered Workers', '1. Covered Workers', 'Total', $US_FIRST, $US_LAST);
my $usCov = cboColumn($path{cboadd}, '2. Covered and Taxable Earnings', '2. Covered and Taxable Earnings', 'Covered earnings (trillions of dollars)a', $US_FIRST, $US_LAST);
my $usBen = cboColumn($path{cboadd}, '3. OASDI Beneficiaries', '3. Social Security Beneficiaries', 'Total', $US_FIRST, $US_LAST);
my $us65 = cboColumn($path{ltbo}, '11. Pop, by Age Group', '11. Population, by Age Group', '65 or older', $US_FIRST, $US_COV_LAST);
my @brUs;
for my $y ($US_FIRST .. $US_LAST) {
    my $benefit = $usOut->{$y} / 100 * $usGdp->{$y} * 1e12 / ($usBen->{$y} * 1e3);
    my $wage = $usCov->{$y} * 1e12 / ($usWork->{$y} * 1e3);
    my $b = 100 * $benefit / $wage;
    die "USA $y: the derived benefit ratio $b is outside 5..100\n" if $b < 5 || $b > 100;
    push @brUs, $b;
}
my @covUs = map { 100 * $usBen->{$_} / 1e3 / $us65->{$_} } $US_FIRST .. $US_COV_LAST;
my @expUs = map { $usOut->{$_} } $US_EXP_FIRST .. $US_LAST;

# ---- the summary
for my $n (map { $_->[1] } @EU) {
    printf "%-8s B %s | exp %s | cov %s\n", $n,
        join(' ', map { sprintf('%d %.2f', $_, $brEu->{$n}[$_ - $EU_FIRST]) } 2022, 2030, 2040, 2050, 2070),
        join(' ', map { sprintf('%.2f', $expEu->{$n}[$_ - $EU_FIRST]) } 2022, 2030, 2040, 2050, 2070),
        join(' ', map { sprintf('%.1f', $covEu{$n}[$_ - $EU_FIRST]) } 2022, 2030, 2040, 2050, 2070);
}
printf "USA      B %s | outlays %s | ben/65+ %s\n",
    join(' ', map { sprintf('%d %.2f', $_, $brUs[$_ - $US_FIRST]) } 2026, 2030, 2040, 2050, 2070, 2100),
    join(' ', map { sprintf('%.3f', $usOut->{$_}) } 2024, 2026, 2030, 2040, 2050, 2070, 2100),
    join(' ', map { sprintf('%.1f', $covUs[$_ - $US_FIRST]) } 2026, 2030, 2040, 2050, 2056);
print "cross-checks against IP279 Tables I.1.12 and I.1.16: $checked figures within their printed tenth\n";

# ---- the two files
sub f { my ($v) = @_; my $s = sprintf('%.7g', $v); $s .= '.0' unless $s =~ /[.eE]/; return $s . 'f'; }
sub arr { return 'new float[] { ' . join(', ', map { f($_) } @_) . ' }'; }
my @order = sort keys %path;
my $head = '';
$head .= "// GENERATED by Tools/pbr_prep.pl. DO NOT EDIT BY HAND. perl Tools/pbr_prep.pl --check re-reads every source and proves this file is theirs.\n//\n";
$head .= "// pbr (K293's filing, Reviews/2026-10-30_pbr_prereg.md): the pension benefit ratio - the EU five the EC Ageing Report 2024 (statistical annex,\n";
$head .= "// cross-country tables), the USA derived from CBO's 2026 Long-Term Projections for Social Security (publication 62556). The tool's header names each\n";
$head .= "// file, table and transformation; its cross-checks hold the annex against Institutional Paper 279's Tables I.1.12 and I.1.16.\n//\n";
$head .= "// Source : $path{$_}\n// SHA-256: $digest{$_}\n" for @order;
$head .= "\nusing System.Collections.Generic;\n\n";

my $cs = $head;
$cs .= "namespace PoliSim.Data.Generated\n{\n";
$cs .= "    /// <summary>pbr (K314): B, the benefit ratio - the average public pension over the average wage, per cent - by calendar year. The model takes only its ratio\n";
$cs .= "    /// (PensionBenefitRatio.At): the EU five's own table, the USA's derived from CBO's five series. Generated, never hand-edited.</summary>\n";
$cs .= "    public static class PensionBenefitRatioPath\n    {\n";
$cs .= "        /// <summary>The Ageing Report's years, one figure a year, both included.</summary>\n";
$cs .= "        public const int EuFirstYear = $EU_FIRST, EuLastYear = $EU_LAST;\n";
$cs .= "        /// <summary>CBO's years for the USA's derived ratio, one figure a year, both included (the additional tables start in $US_FIRST).</summary>\n";
$cs .= "        public const int UsFirstYear = $US_FIRST, UsLastYear = $US_LAST;\n\n";
$cs .= "        /// <summary>The Ageing Report 2024, Table II.1.79 \"Benefit ratio (total public pensions, gross, %)\", $EU_FIRST-$EU_LAST; the USA, CBO 62556: (Table 3's outlays x\n";
$cs .= "        /// Table 2's GDP / OASDI beneficiaries) / (covered earnings / covered workers) x 100, $US_FIRST-$US_LAST. Per cent; element i is the first year + i.</summary>\n";
$cs .= "        public static readonly Dictionary<CountryId, float[]> Ratio = new Dictionary<CountryId, float[]>\n        {\n";
$cs .= "            { CountryId.$_, " . arr(@{ $brEu->{$_} }) . " },\n" for map { $_->[1] } @EU;
$cs .= "            { CountryId.USA, " . arr(@brUs) . " },\n";
$cs .= "        };\n\n";
$cs .= "        /// <summary>The first year of each country's <see cref=\"Ratio\"/>.</summary>\n";
$cs .= "        public static int FirstYear(CountryId id) => id == CountryId.USA ? UsFirstYear : EuFirstYear;\n\n";
$cs .= "        /// <summary>The files read, project-relative, and their SHA-256 when this file was written (GeneratedCatalogCheck's drift rule).</summary>\n";
$cs .= "        public static readonly string[] SourcePath = { " . join(', ', map { "\"$path{$_}\"" } @order) . " };\n";
$cs .= "        public static readonly string[] SourceDigest = { " . join(', ', map { "\"$digest{$_}\"" } @order) . " };\n";
$cs .= "    }\n}\n";
$cs =~ s/\n/\r\n/g;

my $an = $head;
$an .= "namespace PoliSim.Data.Generated\n{\n";
$an .= "    /// <summary>pbr: what the no-policy pension line is held against (PensionBenefitRatioDiagnostic, (A1) and (A2)) - each source's expenditure path and its\n";
$an .= "    /// coverage ratio. Read by the diagnostic only; the model reads PensionBenefitRatioPath. Generated, never hand-edited.</summary>\n";
$an .= "    public static class PensionBenefitRatioAnchors\n    {\n";
$an .= "        public const int EuFirstYear = $EU_FIRST, EuLastYear = $EU_LAST;\n";
$an .= "        /// <summary>The USA's expenditure years (CBO's Table 3, actual to 2025) and its coverage years (the beneficiaries over CBO's 65 or older, LTBO 2026 sheet 11).</summary>\n";
$an .= "        public const int UsExpenditureFirstYear = $US_EXP_FIRST, UsExpenditureLastYear = $US_LAST, UsCoverageFirstYear = $US_FIRST, UsCoverageLastYear = $US_COV_LAST;\n\n";
$an .= "        /// <summary>The Ageing Report 2024, Table II.1.64 \"Public pensions, gross expenditure as % of GDP\", $EU_FIRST-$EU_LAST; the USA, CBO 62556 Table 3, outlays with\n";
$an .= "        /// scheduled benefits, % of GDP, calendar years $US_EXP_FIRST-$US_LAST. Element i is the first year + i.</summary>\n";
$an .= "        public static readonly Dictionary<CountryId, float[]> Expenditure = new Dictionary<CountryId, float[]>\n        {\n";
$an .= "            { CountryId.$_, " . arr(@{ $expEu->{$_} }) . " },\n" for map { $_->[1] } @EU;
$an .= "            { CountryId.USA, " . arr(@expUs) . " },\n";
$an .= "        };\n\n";
$an .= "        /// <summary>The coverage ratio, per cent: the EU five Table II.1.76 / (Table II.1.8 x Table II.1.11 x 10) x 100, $EU_FIRST-$EU_LAST (= IP279 Table I.1.12 to its\n";
$an .= "        /// tenth); the USA OASDI beneficiaries / CBO's 65 or older x 100, $US_FIRST-$US_COV_LAST. Element i is the first year + i.</summary>\n";
$an .= "        public static readonly Dictionary<CountryId, float[]> Coverage = new Dictionary<CountryId, float[]>\n        {\n";
$an .= "            { CountryId.$_, " . arr(@{ $covEu{$_} }) . " },\n" for map { $_->[1] } @EU;
$an .= "            { CountryId.USA, " . arr(@covUs) . " },\n";
$an .= "        };\n\n";
$an .= "        public static int ExpenditureFirstYear(CountryId id) => id == CountryId.USA ? UsExpenditureFirstYear : EuFirstYear;\n";
$an .= "        public static int CoverageFirstYear(CountryId id) => id == CountryId.USA ? UsCoverageFirstYear : EuFirstYear;\n";
$an .= "    }\n}\n";
$an =~ s/\n/\r\n/g;

exit 0 if $mode eq '--dry';
if ($mode eq '--check') {
    my $bad = 0;
    for my $pair ([$outPath, $cs], [$outAnchors, $an]) {
        my $disk = -e $pair->[0] ? slurp($pair->[0]) : '';
        if ($disk eq $pair->[1]) { print "$pair->[0] is what this tool writes\n"; } else { print "$pair->[0] DIFFERS from what this tool writes\n"; $bad = 1; }
    }
    exit $bad;
}
for my $pair ([$outPath, $cs], [$outAnchors, $an]) { open my $o, '>:raw', $pair->[0] or die "$pair->[0]: $!\n"; print $o $pair->[1]; close $o; print "wrote $pair->[0]\n"; }
