#!/usr/bin/perl
# PS-6 US-14 - THE US FEDERAL DEBT LIMIT AS DATED DATA: the statute's steps read from the acts and from Treasury's own tables, the limit P.L. 118-5
# s401(b) reinstated on 2 January 2025 reproduced from the record's own debt, the record's last extraordinary-measures episode dated, and Treasury's
# perimeters at the seed's vintage. Writes docs/generated/US_DEBT_LIMIT.md; UsDebtLimitCheck re-reads it (the cheap bar), recomputes from the same
# Treasury files every figure it can, and writes the document's engine block (the bridge to the game's seed) with its WriteBridge entry.
# Usage (from the project root): perl Tools/us_debt_limit_prep.pl [md]
#
# READ, NOT TYPED: every amount comes from a Treasury file (MSPD tables 1 and 2, the Daily Treasury Statement's table III-C, Treasury's daily table of
# the 2025 measures) and every date from an act's own "Approved ..." line, the act's text or Treasury's report - with ONE exception, stated where it is
# used: the month Treasury projected the 2025 measures would run out ("in August", the Secretary's letter of 9 May 2025) is read from the letter's page
# image, which carries no text layer, so it is a constant below with its citation.
# The two PDFs with a text layer are read through `pdftotext -raw` (Git for Windows' mingw64 build carries it).
use strict;
use warnings;
use utf8;
use Digest::SHA qw(sha256_hex);
use Math::BigFloat;
use Time::Local qw(timegm);
binmode STDOUT, ':encoding(UTF-8)';

my $out = $ARGV[0] // 'docs/generated/US_DEBT_LIMIT.md';
my $R = 'ElectionsData/usa/raw';
# the inputs, in the order the stamp lists them - the key is what the document and the check call each file
my @IN = (
    [mspd2a   => "$R/treasury_debt/treasury_mspd_table_2_2021-11_to_2024-11.json"],
    [mspd2b   => "$R/debt_limit/treasury_mspd_table_2_2024-12_to_2026-08.json"],
    [mspd1    => "$R/treasury_debt/treasury_mspd_table_1_2024-12-31_and_2026-06-30.json"],
    [dts2301  => "$R/treasury_debt/treasury_dts_table_3c_2023-01-10_to_2023-01-24.json"],
    [dts2306  => "$R/treasury_debt/treasury_dts_table_3c_2023-05-31_to_2023-06-07.json"],
    [dts2501  => "$R/treasury_debt/treasury_dts_table_3c_2024-12-31_to_2025-01-24.json"],
    [dts2507  => "$R/treasury_debt/treasury_dts_table_3c_2025-07-01_to_2025-07-08.json"],
    [dts2606  => "$R/treasury_debt/treasury_dts_table_3c_2026-06-26_to_2026-07-02.json"],
    [pl117_73 => "$R/budget_statutes/govinfo_PLAW-117publ73.htm"],
    [pl118_5  => "$R/budget_statutes/govinfo_PLAW-118publ5.htm"],
    [pl119_21 => "$R/debt_limit/govinfo_PLAW-119publ21.htm"],
    [csrdf    => "$R/treasury_debt/treasury_2025_csrdf_report.pdf"],
    [emtable  => "$R/treasury_debt/treasury_dsl_activity_2025-07-03.pdf"],
);
my %P = map { $_->[0] => $_->[1] } @IN;

sub slurp { my ($p) = @_; open my $fh, '<:raw', $p or die "$p: $!"; local $/; my $b = <$fh>; close $fh; return $b; }
sub digest_of { return sha256_hex(slurp($_[0])); }
sub pdf_text { my ($p) = @_; my $t = `pdftotext -raw "$p" - 2>/dev/null`; die "$p: pdftotext read nothing\n" unless defined $t && length $t > 100; $t =~ s/\s+/ /g; return $t; }

# ------------------------------------------------------------------ the Treasury API's JSON: one flat object per row, every value a string
sub rows_of {
    my ($p) = @_;
    my $s = slurp($p);
    my @rows;
    while ($s =~ /\{("record_date":[^{}]*)\}/g) { my $o = $1; my %r; $r{$1} = $2 while $o =~ /"([a-z0-9_]+)":"([^"]*)"/g; push @rows, \%r if $r{record_date} =~ /^\d{4}-\d\d-\d\d$/; }
    die "$p: no rows\n" unless @rows;
    return \@rows;
}
# MSPD table 2: the amount of one class (Statutory Debt Limit, Total Public Debt Subject to Limit) on one month-end, USD millions as given
sub mspd2 { my ($rows, $date, $class) = @_; for (@$rows) { return $_->{total_mil_amt} if $_->{record_date} eq $date && $_->{debt_limit_class1_desc} eq $class; } die "MSPD table 2 has no $class on $date\n"; }
# DTS table III-C: debt subject to limit = held by the public + intragovernmental - the three not subject + guaranteed debt (the table's lines 1, 2, 4-7;
# the API serves no total line), at the day's close or opening; and the limit line - whole USD millions as published
sub dts {
    my ($rows, $date, $which) = @_;
    my %v; for (@$rows) { next unless $_->{record_date} eq $date; $v{"$_->{debt_catg}|$_->{debt_catg_desc}"} = $_->{$which eq 'open' ? 'open_today_bal' : 'close_today_bal'}; }
    die "DTS III-C has no rows on $date\n" unless %v;
    my @need = ('Debt Held by the Public|null', 'Intragovernmental Holdings|null', 'Debt Not Subject to Limit|Other Debt (-)', 'Debt Not Subject to Limit|Unamortized Discount (-)',
        'Debt Not Subject to Limit|Federal Financing Bank (-)', 'Other Debt Subject to Limit|Guaranteed Debt of Government Agencies', 'Statutory Debt Limit|null');
    for (@need) { die "DTS III-C on $date lacks $_\n" unless defined $v{$_}; }
    my $dsl = $v{$need[0]} + $v{$need[1]} - $v{$need[2]} - $v{$need[3]} - $v{$need[4]} + $v{$need[5]};
    return ($dsl, $v{$need[6]});
}
sub dts_dates { my ($rows) = @_; my %d = map { $_->{record_date} => 1 } @$rows; return sort keys %d; }

sub usd_of_mil { my ($mil) = @_; my $x = Math::BigFloat->new($mil); $x->bmul(1_000_000); return $x; }
sub group { my ($n) = @_; my ($i, $f) = split /\./, "$n"; my $neg = $i =~ s/^-//; 1 while $i =~ s/^(\d+)(\d{3})/$1,$2/; return ($neg ? '-' : '') . $i . (defined $f && length $f ? ".$f" : ''); }
my %MON = (January => 1, February => 2, March => 3, April => 4, May => 5, June => 6, July => 7, August => 8, September => 9, October => 10, November => 11, December => 12);
sub iso { my ($m, $d, $y) = @_; die "no month $m\n" unless $MON{$m}; return sprintf('%04d-%02d-%02d', $y, $MON{$m}, $d); }
sub days_between { my ($a, $b) = @_; my @a = split /-/, $a; my @b = split /-/, $b; return (timegm(0, 0, 0, $b[2], $b[1] - 1, $b[0]) - timegm(0, 0, 0, $a[2], $a[1] - 1, $a[0])) / 86400; }
sub approved { my ($p) = @_; my $t = slurp($p); die "$p: no \"Approved ...\" line\n" unless $t =~ /Approved ([A-Z][a-z]+) (\d{1,2}), (\d{4})\./; return iso($1, $2, $3); }
sub dollars_in { my ($p, $re) = @_; my $t = slurp($p); $t =~ s/\s+/ /g; die "$p: the clause is not there\n" unless $t =~ $re; (my $n = $1) =~ s/,//g; return $n; }

# ------------------------------------------------------------------ the acts: their days and their amounts
my $d117 = approved($P{pl117_73});
my $add117 = dollars_in($P{pl117_73}, qr/is increased by \$([\d,]+)\./);
my $d118 = approved($P{pl118_5});
my $fra = slurp($P{pl118_5}); $fra =~ s/\s+/ /g;
die "P.L. 118-5 s401(a): the suspension's last day is not there\n" unless $fra =~ /shall not apply for the period beginning on the date of the enactment of this Act and ending on ([A-Z][a-z]+) (\d{1,2}), (\d{4})\./;
my $suspendedThrough = iso($1, $2, $3);
die "P.L. 118-5 s401(b): the reinstatement's day is not there\n" unless $fra =~ /Effective on ([A-Z][a-z]+) (\d{1,2}), (\d{4}), the limitation in effect under section 3101\(b\)/;
my $reinstated = iso($1, $2, $3);
my $d119 = approved($P{pl119_21});
my $add119 = dollars_in($P{pl119_21}, qr/as most recently increased by section 401\(b\) of Public Law 118-5 \(31 U\.S\.C\. 3101 note\), is increased by \$([\d,]+)\./);

# ------------------------------------------------------------------ Treasury's limit as it carried it (MSPD table 2) - every month-end read
my $m2a = rows_of($P{mspd2a});
my $m2b = rows_of($P{mspd2b});
my @months = sort { $a->[0] cmp $b->[0] } map { [$_->{record_date}, $_->{total_mil_amt}] } grep { $_->{debt_limit_class1_desc} eq 'Statutory Debt Limit' } (@$m2a, @$m2b);
my (@steps, $prev);
for my $m (@months) { if (!defined $prev || $m->[1] ne $prev) { push @steps, [$m->[0], $m->[1]]; $prev = $m->[1]; } }
# the steps must be: the pre-P.L. 117-73 limit, P.L. 117-73's, the suspension (0), the reinstated limit, P.L. 119-21's - and nothing else
die "MSPD table 2: " . scalar(@steps) . " steps in the limit, not the five the acts give: " . join(', ', map { "$_->[0] $_->[1]" } @steps) . "\n" unless @steps == 5 && $steps[2][1] eq '0';
my ($L0, $L1, $L2, $L3) = map { usd_of_mil($steps[$_][1]) } (0, 1, 3, 4);
die "P.L. 117-73: " . group($L0) . " + " . group($add117) . " is not Treasury's " . group($L1) . "\n" unless $L0->copy->badd($add117)->bcmp($L1) == 0;
die "P.L. 119-21: " . group($L2) . " + " . group($add119) . " is not Treasury's " . group($L3) . "\n" unless $L2->copy->badd($add119)->bcmp($L3) == 0;

# ------------------------------------------------------------------ the reinstatement, reproduced from the record's own debt (P.L. 118-5 s401(b))
# the limit in force + the face amount outstanding on 2 Jan 2025 - the face amount outstanding on the date of enactment (3 June 2023, a Saturday: the
# Friday's close, which is the Monday's opening). "On 2 Jan" is the end of the previous day - Treasury's letter of 27 Dec 2024, page image: "the new debt
# limit will be established at the amount of outstanding debt subject to the statutory limit at the end of the previous day" - so 2 Jan's OPENING.
my $dts06 = rows_of($P{dts2306});
my $dts01 = rows_of($P{dts2501});
my ($dslEnact, $limEnact) = dts($dts06, '2023-06-02', 'close');
my ($dslEnactMon) = dts($dts06, '2023-06-05', 'open');
die "DTS: the close of 2 June 2023 ($dslEnact) is not the opening of 5 June ($dslEnactMon)\n" unless $dslEnact == $dslEnactMon;
die "DTS: the limit line on 2 June 2023 ($limEnact) is not Treasury's P.L. 117-73 limit\n" unless usd_of_mil($limEnact)->bcmp($L1) == 0;
my ($dslJan2Open, $limJan2) = dts($dts01, '2025-01-02', 'open');
my ($dslJan2Close) = dts($dts01, '2025-01-02', 'close');
my ($dslDec31Close) = dts($dts01, '2024-12-31', 'close');
die "DTS: the opening of 2 Jan 2025 ($dslJan2Open) is not the close of 31 Dec 2024 ($dslDec31Close)\n" unless $dslJan2Open == $dslDec31Close;
my $reproducedMil = $limEnact + $dslJan2Open - $dslEnact;   # whole millions, as the DTS publishes
my $gapMil = Math::BigFloat->new($reproducedMil)->bsub(scalar Math::BigFloat->new($L2)->bdiv(1_000_000));   # scalar: bdiv in list context is a floored quotient and its remainder
die "the reproduction " . group($reproducedMil) . " mil is " . "$gapMil" . " mil from Treasury's figure - beyond the three whole-million roundings\n" unless $gapMil->copy->babs->bcmp(1.5) <= 0;
my $closeWould = $limEnact + $dslJan2Close - $dslEnact;
# to the dollar: MSPD table 2's 31 Dec 2024 debt subject to limit is the same close (2 Jan's opening) to a fraction of a dollar
my $dslDec31Usd = usd_of_mil(mspd2($m2b, '2024-12-31', 'Total Public Debt Subject to Limit'));
my $headroomUsd = $L2->copy->bsub($dslDec31Usd);                          # what s401(b) carried: the limit in force less the debt on enactment
my $impliedEnactUsd = $L1->copy->bsub($headroomUsd);
die "the implied enactment-day debt " . group($impliedEnactUsd) . " is outside the DTS's whole million $dslEnact\n" unless $impliedEnactUsd->copy->bdiv(1_000_000)->bsub($dslEnact)->babs->bcmp(0.5) <= 0;

# ------------------------------------------------------------------ the record's last episode of extraordinary measures (2025)
my $csrdf = pdf_text($P{csrdf});
die "the CSRDF report does not date the episode's start\n" unless $csrdf =~ /On ([A-Z][a-z]+) (\d{1,2}), (\d{4}), the outstanding debt subject to the limit reached the statutory debt limit\./;
my $emReached = iso($1, $2, $3);
die "the CSRDF report does not date the episode's end\n" unless $csrdf =~ /On ([A-Z][a-z]+) (\d{1,2}), (\d{4}), Public Law 119-21 increased the statutory debt limit by \$5 trillion/;
my $emEnded = iso($1, $2, $3);
die "the CSRDF report's end ($emEnded) is not P.L. 119-21's approval ($d119)\n" unless $emEnded eq $d119;
my $emDays = days_between($emReached, $emEnded);
# the DTS agrees: debt subject to limit within the measures' working margin of the limit from that day, well below it on the business day before
my @d01 = dts_dates($dts01);
my ($before) = grep { $_ lt $emReached } reverse @d01;
my ($dslBefore, $limBefore) = dts($dts01, $before, 'close');
my ($dslAt, $limAt) = dts($dts01, $emReached, 'close');
my $dts07 = rows_of($P{dts2507});
my @d07 = dts_dates($dts07);
my ($lastIn) = grep { $_ lt $emEnded } reverse @d07;
my ($firstAfter) = grep { $_ gt $emEnded } @d07;
my ($dslLast, $limLast) = dts($dts07, $lastIn, 'close');
my ($dslAfter, $limAfter) = dts($dts07, $firstAfter, 'close');
die "DTS: on $emReached the debt is " . ($limAt - $dslAt) . " mil under the limit - not at it\n" unless $limAt - $dslAt < 100;
die "DTS: on $before the debt is already at the limit\n" unless $limBefore - $dslBefore > 1000;
die "DTS: on $lastIn the debt is not at the limit\n" unless $limLast - $dslLast < 100;
die "DTS: on $firstAfter the limit is not P.L. 119-21's\n" unless usd_of_mil($limAfter)->copy->bsub($L3)->babs->bcmp(1_000_000) < 0;
# the projected end: [IMAGE] the Secretary's letter of 9 May 2025 - "there is a reasonable probability that the federal government's cash and extraordinary
# measures will be exhausted in August while Congress is scheduled to be in recess" - read from the page (no text layer); the month is the reading
my ($exFrom, $exTo) = ('2025-08-01', '2025-08-31');
my ($exDaysFrom, $exDaysTo) = (days_between($emReached, $exFrom), days_between($emReached, $exTo));
# the measures' size on the first day: Treasury's daily table, the episode's first row, "Totals: Total Measures" ($ billions)
my $em = pdf_text($P{emtable});
my %NUM = reverse %MON;
my @er = split /-/, $emReached;
my $first = sprintf('%s %d, %d', $NUM{$er[1] + 0}, $er[2], $er[0]);
die "the measures' table has no row for $emReached\n" unless $em =~ /\Q$first\E((?: \(?\d+\)?){12})/;
my @cells = map { (my $c = $_) =~ s/[()]//g; $c } grep { length } split / /, $1;
my $emTotalBn = $cells[9];
die "the measures' table's first row does not add up: CSRDF $cells[0] + G Fund $cells[3] + ESF $cells[6] against $emTotalBn\n" unless abs($cells[0] + $cells[3] + $cells[6] - $emTotalBn) <= 1;

# ------------------------------------------------------------------ Treasury's perimeters (MSPD table 1 and table 2) at the reinstatement and at the seed's vintage
my $m1 = rows_of($P{mspd1});
sub mspd1 { my ($date, $col) = @_; for (@$m1) { return $_->{$col} if $_->{record_date} eq $date && $_->{security_type_desc} eq 'Total Public Debt Outstanding'; } die "MSPD table 1 has no total on $date\n"; }
my $VINTAGE = '2026-06-30';   # WorldClock.SeedVintage is 1 July 2026; the month-end before it is the last statement the seed's season reads
my %per;
for my $d ('2024-12-31', $VINTAGE) {
    $per{$d} = {
        public => usd_of_mil(mspd1($d, 'debt_held_public_mil_amt')), intragov => usd_of_mil(mspd1($d, 'intragov_hold_mil_amt')),
        tpdo => usd_of_mil(mspd1($d, 'total_mil_amt')), dsl => usd_of_mil(mspd2($m2b, $d, 'Total Public Debt Subject to Limit')),
    };
}
my ($dslVintageDts) = dts(rows_of($P{dts2606}), $VINTAGE, 'close');
die "DTS's close of $VINTAGE ($dslVintageDts mil) is not MSPD's " . group($per{$VINTAGE}{dsl}) . "\n" unless $per{$VINTAGE}{dsl}->copy->bdiv(1_000_000)->bsub($dslVintageDts)->babs->bcmp(0.5) <= 0;
# the 2023 episode, for the span's range only: the day the DTS first shows the debt at the limit
my $dts23 = rows_of($P{dts2301});
my ($em23) = grep { my ($x, $l) = dts($dts23, $_, 'close'); $l - $x < 100 } dts_dates($dts23);
my $em23Days = days_between($em23, $d118);

# ------------------------------------------------------------------ the document
my $stamp = join(' ', map { "$_->[0]=" . digest_of($_->[1]) } @IN);
my $md = "<!-- GENERATED by Tools/us_debt_limit_prep.pl. DO NOT EDIT BY HAND. Re-derive: perl Tools/us_debt_limit_prep.pl. inputs-sha256: $stamp -->\n";
$md .= "# The US federal debt limit as dated data - the statute's steps, the 2025 reinstatement reproduced, the record's last extraordinary measures, Treasury's perimeters (PS-6 US-14)\n\n";
$md .= "Read on the files the stamp names (`ElectionsData/usa/raw/treasury_debt/`, `raw/debt_limit/`, `raw/budget_statutes/`; the reading is `ElectionsData/usa/debt_limit.md`). Treasury's tables give USD millions; a figure here is that figure times a million, nothing rounded. `UsDebtLimitCheck` (the cheap bar) recomputes every Treasury figure below from the same files and fails while this document differs.\n\n";
$md .= "## The limit by date\n\n| in force from | limit (USD) | the act | as Treasury carried it (MSPD table 2) |\n|---|---:|---|---|\n";
$md .= sprintf("| %s | %s | P.L. 117-73 (approved %s): the limit then in force \"is increased by \$%s\" | %s from %s (%s on %s) |\n", $d117, group($L1), $d117, group($add117), group($steps[1][1]), $steps[1][0], group($steps[0][1]), $steps[0][0]);
$md .= sprintf("| %s | suspended through %s | P.L. 118-5 s401(a) (approved %s) | 0 from %s |\n", $d118, $suspendedThrough, $d118, $steps[2][0]);
$md .= sprintf("| %s | %s | P.L. 118-5 s401(b): the limit in force raised by the debt issued while suspended - Treasury's figure, reproduced below | %s from %s |\n", $reinstated, group($L2), group($steps[3][1]), $steps[3][0]);
$md .= sprintf("| %s | %s | P.L. 119-21 s72001 (approved %s): \"is increased by \$%s\" | %s from %s |\n\n", $d119, group($L3), $d119, group($add119), group($steps[4][1]), $steps[4][0]);
$md .= "## The reinstatement of 2 January 2025, reproduced from the record's own debt\n\n";
$md .= "P.L. 118-5 s401(b): the limit in force, raised by the face amount outstanding on 2 January 2025 less that outstanding on the date of enactment. Debt subject to limit from the Daily Treasury Statement's table III-C (whole USD millions as published; held by the public + intragovernmental - the three lines not subject + guaranteed debt):\n\n";
$md .= sprintf("- the limit in force on the date of enactment: **%s** mil (the DTS's limit line on 2 June 2023);\n", group($limEnact));
$md .= sprintf("- outstanding on the date of enactment (3 June 2023, a Saturday - the Friday's close, the Monday's opening): **%s** mil;\n", group($dslEnact));
$md .= sprintf("- outstanding on 2 January 2025, read as Treasury read it - \"at the end of the previous day\" (the Secretary's letter of 27 December 2024, read from the page image): 2 January's opening, **%s** mil (31 December's close);\n", group($dslJan2Open));
$md .= sprintf("- reproduced: %s + %s - %s = **%s mil**, against Treasury's **%s** - within the three whole-million roundings (%s mil apart).\n", group($limEnact), group($dslJan2Open), group($dslEnact), group($reproducedMil), group($L2), "$gapMil");
$md .= sprintf("- 2 January's CLOSE (%s mil, after the redemption the letter names) would have given %s mil - %s mil lower: the formula reads the day's opening.\n", group($dslJan2Close), group($closeWould), group($reproducedMil - $closeWould));
$md .= sprintf("- To the dollar: MSPD table 2's debt subject to limit on 31 December 2024 is %s, the same close to a fraction of a dollar; so s401(b) carried a headroom of **%s** (Treasury's limit less that debt), and the enactment-day debt Treasury's figure implies is %s, inside the DTS's whole million.\n", group($dslDec31Usd), group($headroomUsd), group($impliedEnactUsd));
$md .= sprintf("- With P.L. 119-21's \$%s: **%s**.\n\n", group($add119), group($L3));
$md .= "## The record's last extraordinary measures (R-US21 (iii))\n\n";
$md .= sprintf("- **Reached %s, ended %s: %d days.** Treasury's report on the CSRDF (30 January 2026): \"On %s, the outstanding debt subject to the limit reached the statutory debt limit\"; the episode ended with P.L. 119-21. The DTS agrees: %s mil under the limit on %s, %s under on %s and %s under on %s; on %s the limit line reads %s.\n",
    $emReached, $emEnded, $emDays, $first, group($limBefore - $dslBefore), $before, group($limAt - $dslAt), $emReached, group($limLast - $dslLast), $lastIn, $firstAfter, group($limAfter));
$md .= sprintf("- **Projected to run out in August 2025: %d to %d days from %s.** The Secretary's letter of 9 May 2025 (page image, no text layer): \"there is a reasonable probability that the federal government's cash and extraordinary measures will be exhausted in August\". The month is the reading; its first and last days bound it.\n", $exDaysFrom, $exDaysTo, $emReached);
$md .= sprintf("- **The measures' size on the first day: \$%d billion** - Treasury's daily table (\"Debt Subject to Limit - Extraordinary Measures\", 3 July 2025), the %s row, Totals \"Total Measures\" (CSRDF/PSRHBF %d + G Fund %d + ESF %d).\n", $emTotalBn, $first, $cells[0], $cells[3], $cells[6]);
$md .= sprintf("- The 2023 episode, for the range: the DTS first shows the debt at the limit on %s; P.L. 118-5 suspended it on %s - %d days.\n\n", $em23, $d118, $em23Days);
$md .= "## Treasury's perimeters (MSPD tables 1 and 2), USD\n\n| | 31 Dec 2024 (2 January 2025's opening) | $VINTAGE (the seed's vintage) |\n|---|---:|---:|\n";
for (['debt held by the public', 'public'], ['intragovernmental holdings', 'intragov'], ['total public debt outstanding', 'tpdo'], ['debt subject to limit', 'dsl']) {
    $md .= sprintf("| %s | %s | %s |\n", $_->[0], group($per{'2024-12-31'}{$_->[1]}), group($per{$VINTAGE}{$_->[1]}));
}
$md .= "\n## Data (read by `UsDebtLimitCheck`)\n\n<!-- US14-DATA BEGIN -->\n";
$md .= "law.1 = $d117 | " . "$L1" . " | P.L. 117-73\n";
$md .= "law.2 = $d118 | suspended | P.L. 118-5 s401(a), through $suspendedThrough\n";
$md .= "law.3 = $reinstated | " . "$L2" . " | P.L. 118-5 s401(b)\n";
$md .= "law.4 = $d119 | " . "$L3" . " | P.L. 119-21 s72001\n";
$md .= "reinstatement.reproduced_mil = $reproducedMil\n";
$md .= "reinstatement.headroom_usd = " . "$headroomUsd" . "\n";
$md .= "vintage.date = $VINTAGE\n";
$md .= "vintage.$_\_usd = " . $per{$VINTAGE}{$_} . "\n" for qw(dsl tpdo public intragov);
$md .= "em.reached = $emReached\nem.ended = $emEnded\nem.days_used = $emDays\nem.exhaustion = $exFrom..$exTo\nem.days_to_exhaustion = $exDaysFrom..$exDaysTo\n";
$md .= "em.total_measures_usd = " . ($emTotalBn * 1_000_000_000) . "\n";
$md .= "<!-- US14-DATA END -->\n\n";
$md .= "## The bridge to the game's seed (PN-4: same year, same perimeter)\n\n";
$md .= "Written by the engine, which alone holds the seed: `-executeMethod PoliSim.EditorTools.UsDebtLimitCheck.WriteBridge`. The cheap bar fails while it differs from what `UsDebtLimitCheck` measures.\n\n";
# the engine's block survives a re-derivation of the rest
my ($engine) = (-e $out ? slurp($out) : '') =~ /(<!-- GENERATED by PoliSim\.EditorTools\.UsDebtLimitCheck\.WriteBridge.*?<!-- END GENERATED -->)/s;
$engine //= "<!-- GENERATED by PoliSim.EditorTools.UsDebtLimitCheck.WriteBridge. DO NOT EDIT BY HAND. -->\nNOT YET WRITTEN - run the entry above.\n<!-- END GENERATED -->";
$engine =~ s/\r\n/\n/g;
$md .= "$engine\n";

open my $o, '>:raw:encoding(UTF-8)', $out or die "$out: $!";
print $o $md;
close $o;
print "$out written: limits ", group($L1), " / suspended / ", group($L2), " / ", group($L3), "; reproduced ", group($reproducedMil), " mil (", "$gapMil", " from Treasury's); measures $emReached..$emEnded = $emDays days, exhaustion $exDaysFrom..$exDaysTo, \$$emTotalBn bn; vintage DSL ", group($per{$VINTAGE}{dsl}), "\n";
