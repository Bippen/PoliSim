#!/usr/bin/perl
# q16'S SOURCED SCALE (K72, docs/RULINGS_2026-10-23.md: "q16's dial-to-rate scale is sourced (party platforms or roll calls), US only").
# It reads ONLY the saved files under ElectionsData/usa/raw/q16_scale/, each held to its SHA256SUMS.txt line first:
#   - govinfo_USCODE-2023-title26-sec1.htm: 26 U.S.C. 1 - its subsections (a)-(e) top bracket (the rate in force before the Tax Cuts and Jobs Act) and
#     1(j), the TCJA's rates for taxable years 2018 through 2025 (the rates in force on the US start, 12 Mar 2024);
#   - govinfo_USCODE-2023-title26-sec11.htm: 26 U.S.C. 11(b), the corporate rate;
#   - clerk_house_roll699_2017.xml: the House's final vote on the TCJA (H R 1, on concurring in the Senate amendment, 20 Dec 2017) by party;
#   - wayback_senate_vote_115_1_00323_*.xml: the Senate's final vote on it (recede and concur, 20 Dec 2017) by party;
#   - ucsb_presidency_2024-democratic-party-platform.html: the 2024 Democratic platform, the American Presidency Project's copy (the party's own PDF BILLED);
#   - gop_RNC2024-Platform.pdf: the 2024 Republican platform (by pdftotext -raw).
# THE ANCHORS (DECLARED reading of K72): a party's own rate on a line is its 2024 platform's where the platform states one (the Democrats' corporate
# 28 percent); else the rate it enacted or defended on the TCJA's final roll calls, which split the parties whole - the Republicans' (every Republican
# yea in the Senate, the House's Republicans for it, every Democrat against) the TCJA's own rates, 1(j)'s top 37 % and 11(b)'s 21 %, which their 2024
# platform would "make permanent"; the Democrats' the top rate the TCJA replaced, 39.6 %. The rate in force is the TCJA's (the US start, 2024).
# It writes ElectionsData/usa/q16_rate_anchors.csv and Assets/Scripts/Elections/Generated/UsRateAnchors.cs, both built and tested before either is written.
# It dies, writing nothing, on: a file off its digest or in no SHA256SUMS.txt; a sentence or a figure not found where it is read; a roll call that is
# not H R 1's final passage or does not split the parties whole (no Democrat for it; the Republicans for it by a majority); a non-ASCII output byte.
# Usage: perl Tools/us_rate_anchors_prep.pl   (from the project root, Git for Windows' perl; pdftotext at /mingw64/bin or on PATH). ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);

my $raw = 'ElectionsData/usa/raw/q16_scale';
my $pdftotext = -x '/mingw64/bin/pdftotext' ? '/mingw64/bin/pdftotext' : 'pdftotext';
my @problems;
sub problem { push @problems, join('', @_); return; }
$SIG{__WARN__} = sub { problem('a perl warning: ', $_[0] =~ s/\s+$//r) };

my %sums;
{
    open my $s, '<', "$raw/SHA256SUMS.txt" or die "$raw/SHA256SUMS.txt: $!\n";
    while (my $l = <$s>) { $sums{$2} = $1 if $l =~ /^([0-9a-f]{64}) \*(.+?)\s*$/; }
    close $s;
}
my %read;
sub bytes_of {
    my $name = shift;
    my $f = "$raw/$name";
    open my $h, '<:raw', $f or die "$f: $!\n";
    local $/;
    my $b = <$h>;
    close $h;
    my $d = sha256_hex($b);
    my $want = $sums{$name} or die "$f: in no SHA256SUMS.txt - the tool reads saved files only\n";
    die "$f: SHA-256 $d, its SHA256SUMS.txt holds $want\n" unless $d eq $want;
    $read{"raw/q16_scale/$name"} = $d;
    return $b;
}
sub one { my $re = shift; my @f = sort grep { /^$re$/ } keys %sums; die "$re: " . scalar(@f) . " saved files match, not one\n" unless @f == 1; return $f[0]; }
sub words { my $s = shift; $s =~ s/<script\b.*?<\/script>//gis; $s =~ s/<[^>]*>/ /g; $s =~ s/&nbsp;|&#160;/ /g; $s =~ s/&amp;/&/g; $s =~ s/\s+/ /g; return $s; }
sub pdf_words {
    my $name = shift;
    bytes_of($name);
    open my $p, '-|', $pdftotext, '-raw', "$raw/$name", '-' or die "$pdftotext: $!\n";
    my $t = do { local $/; <$p> };
    close $p or die "$pdftotext $name: exit " . ($? >> 8) . "\n";
    $t =~ s/\xE2\x80\x99/'/g; $t =~ s/[^\x00-\x7F]/ /g; $t =~ s/-\n//g; $t =~ s/\s+/ /g;
    return $t;
}

# ---- the Code: 26 U.S.C. 1 and 11
my $sec1 = words(bytes_of('govinfo_USCODE-2023-title26-sec1.htm'));
my ($before, $j) = (undef, undef);
if ($sec1 =~ /^(.*?)\(f\) Phaseout of marriage penalty/s) { $before = $1; } else { problem('26 U.S.C. 1: subsections (a)-(e) not found before (f)'); }
if ($sec1 =~ /\(j\) Modifications for taxable years 2018 through 2025(.*?)Editorial Notes/s) { $j = $1; } else { problem('26 U.S.C. 1(j), "Modifications for taxable years 2018 through 2025", not found'); }
sub top { my ($t, $what) = @_; my @r = ($t // '') =~ /plus (\d+(?:\.\d+)?)% of the excess over/g; problem("$what: no bracket read") unless @r; my $m = 0; for (@r) { $m = $_ if $_ > $m; } return $m; }
my $pre = top($before, '26 U.S.C. 1(a)-(e)');
my $tcja = top($j, '26 U.S.C. 1(j)');
my $sec11 = words(bytes_of('govinfo_USCODE-2023-title26-sec11.htm'));
my ($corp) = $sec11 =~ /The amount of the tax imposed by subsection \(a\) shall be (\d+) percent of taxable income/;
problem('26 U.S.C. 11(b): "shall be N percent of taxable income" not found') unless defined $corp;

# ---- the roll calls
my $roll = bytes_of('clerk_house_roll699_2017.xml');
problem('House roll 699 of 2017: not H R 1, the Tax Cuts and Jobs Act, on concurring in the Senate amendment')
    unless $roll =~ /<legis-num>H R 1<\/legis-num>/ && $roll =~ /<vote-desc>Tax Cuts and Jobs Act<\/vote-desc>/ && $roll =~ /<vote-question>On Motion to Concur in the Senate Amendment<\/vote-question>/ && $roll =~ /<vote-result>Passed<\/vote-result>/;
my %house;
while ($roll =~ /<totals-by-party>\s*<party>(\w+)<\/party>\s*<yea-total>(\d+)<\/yea-total>\s*<nay-total>(\d+)<\/nay-total>/g) { $house{$1} = [$2, $3]; }
problem('House roll 699: the parties do not split whole (' . join('; ', map { "$_ $house{$_}[0]-$house{$_}[1]" } sort keys %house) . ')')
    unless $house{Republican} && $house{Democratic} && $house{Democratic}[0] == 0 && $house{Republican}[0] > $house{Republican}[1];
my %senate;
my @sen = grep { /^wayback_senate_vote_115_1_00323_\d{14}\.xml$/ } keys %sums;
my $senate_line = 'the Senate vote 323 not saved (BILLED: the archive answered 429 when fetched)';
if (@sen == 1) {
    my $x = bytes_of($sen[0]);
    problem("$sen[0]: not the Senate's final vote on H.R. 1 (recede and concur)") unless $x =~ /<document_name>H\.R\. 1<\/document_name>/ && $x =~ /Motion to Recede from the Senate Amendment to H\.R\. 1 and Concur with Further Amendment/;
    while ($x =~ /<member>.*?<party>(\w)<\/party>.*?<vote_cast>(\w[\w ]*)<\/vote_cast>.*?<\/member>/gs) { $senate{$1}{$2}++; }
    problem("$sen[0]: the parties do not split whole") unless ($senate{R}{Yea} // 0) > 0 && !($senate{R}{Nay}) && !($senate{D}{Yea}) && ($senate{D}{Nay} // 0) > 0;
    $senate_line = sprintf('the Senate (vote 323): R %d-%d, D %d-%d', $senate{R}{Yea} // 0, $senate{R}{Nay} // 0, $senate{D}{Yea} // 0, $senate{D}{Nay} // 0);
}

# ---- the platforms
my $dnc = words(bytes_of('ucsb_presidency_2024-democratic-party-platform.html'));   # the party's own PDF is BILLED (fetch_log.txt): the American Presidency Project's copy
my ($dcorp_from, $dcorp) = $dnc =~ /slashed the corporate tax rate to \d+ percent, down from (\d+) percent\. President Biden will raise that rate back to (\d+) percent/;
problem('the 2024 Democratic platform: "slashed the corporate tax rate to N percent, down from N percent. President Biden will raise that rate back to N percent" not found') unless defined $dcorp;
my $rnc = pdf_words('gop_RNC2024-Platform.pdf');
problem('the 2024 Republican platform: "make permanent the provisions of the Trump Tax Cuts" not found') unless $rnc =~ /make permanent the provisions of the Trump Tax Cuts/;

$SIG{__WARN__} = 'DEFAULT';
if (@problems) { my $n = @problems; print STDERR "MISMATCH: $_\n" for @problems; die "$n mismatch(es) - nothing written\n"; }

my @lines = (
    ['IncomeTax', $pre, $tcja, $tcja, "the top rate the TCJA replaced (26 U.S.C. 1(a)-(e)); every Democrat against the TCJA (House roll 699 of 2017)",
        "the TCJA's top rate (26 U.S.C. 1(j)); the Republicans for it (House roll 699 of 2017); their 2024 platform: make permanent the provisions of the Trump Tax Cuts"],
    ['CorporateTax', $dcorp, $corp, $corp, "the 2024 Democratic platform: raise that rate back to $dcorp percent",
        "26 U.S.C. 11(b) - the TCJA's; the Republicans for it (House roll 699 of 2017); their 2024 platform: make permanent the provisions of the Trump Tax Cuts"],
);
my $csv = "# GENERATED by Tools/us_rate_anchors_prep.pl from the files under ElectionsData/usa/raw/q16_scale/ - DO NOT EDIT.\n"
    . "# K72: q16's sourced scale - each major party's own statutory rate on a US tax line (dem, rep) and the statutory rate in force on the US start\n"
    . "# (in_force), percent; dem_by / rep_by: what each rests on. The game's line carries each in proportion: seeded rate x party / in force (DERIVED).\n"
    . "tax,dem,rep,in_force,dem_by,rep_by\n"
    . join('', map { join(',', @$_) . "\n" } @lines);
die "a field carries a comma\n" if grep { $_ =~ /,/ } map { @$_[4, 5] } @lines;
my $digest = sha256_hex($csv);
sub cq { my $s = shift; $s =~ s/\\/\\\\/g; $s =~ s/"/\\"/g; return "\"$s\""; }
my $cs = "// GENERATED by Tools/us_rate_anchors_prep.pl. DO NOT EDIT BY HAND.\n//\n"
    . "// Source: ElectionsData/usa/q16_rate_anchors.csv (its SHA-256 below), written by the same run from the files listed in RawSources.\n"
    . "// UsRateAnchorCheck re-reads the CSV and the files and finds every figure again.\n\n"
    . "namespace PoliSim.Elections.Generated\n{\n"
    . "    /// <summary>K72 (Elias's ruling of 2026-10-23): q16's sourced scale - each major party's own statutory rate on a US tax line and the rate in force\n"
    . "    /// on the US start, percent (SOURCED; the reading DECLARED in the tool). Generated, never hand-edited.</summary>\n"
    . "    public static class UsRateAnchors\n    {\n"
    . "        public const string SourceDigest = \"$digest\";\n\n"
    . "        /// <summary>Every saved file the run read, by its path under `ElectionsData/usa/`, with its SHA-256.</summary>\n"
    . "        public static readonly (string Path, string Sha256)[] RawSources = BuildRawSources();\n\n"
    . "        /// <summary>The CSV's rows: the tax line (a TaxType name), the Democrats' rate, the Republicans' and the rate in force, and what each party's rests on.</summary>\n"
    . "        public static readonly (string Tax, double Dem, double Rep, double InForce, string DemBy, string RepBy)[] Lines = BuildLines();\n\n"
    . "        private static (string Path, string Sha256)[] BuildRawSources() => new (string, string)[]\n        {\n"
    . join('', map { "            (\"$_\", \"$read{$_}\"),\n" } sort keys %read) . "        };\n\n"
    . "        private static (string Tax, double Dem, double Rep, double InForce, string DemBy, string RepBy)[] BuildLines() => new (string, double, double, double, string, string)[]\n        {\n"
    . join('', map { my $l = $_; "            (" . join(', ', cq($l->[0]), (map { /\./ ? $_ : "$_.0" } @$l[1 .. 3]), cq($l->[4]), cq($l->[5])) . "),\n" } @lines) . "        };\n    }\n}\n";
$cs =~ s/\n/\r\n/g;
for my $o ([ 'ElectionsData/usa/q16_rate_anchors.csv', $csv ], [ 'Assets/Scripts/Elections/Generated/UsRateAnchors.cs', $cs ]) { die "$o->[0]: not ASCII - nothing written\n" if $o->[1] =~ /[^\x00-\x7F]/; }
for my $o ([ 'ElectionsData/usa/q16_rate_anchors.csv', $csv ], [ 'Assets/Scripts/Elections/Generated/UsRateAnchors.cs', $cs ]) { open my $w, '>:raw', $o->[0] or die "$o->[0]: $!\n"; print $w $o->[1]; close $w or die "$o->[0]: $!\n"; }
printf "income: DEM %s, REP %s, in force %s; corporate: DEM %s (the platform, from %s), REP %s, in force %s\n", $pre, $tcja, $tcja, $dcorp, $dcorp_from, $corp, $corp;
printf "the House (roll 699): R %d-%d, D %d-%d; %s\n", @{$house{Republican}}, @{$house{Democratic}}, $senate_line;
print "wrote ElectionsData/usa/q16_rate_anchors.csv, Assets/Scripts/Elections/Generated/UsRateAnchors.cs\n";
