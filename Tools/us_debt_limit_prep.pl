#!/usr/bin/perl
# PS-6 US-14 - THE US FEDERAL DEBT LIMIT AS DATED DATA: the statute's steps read from the acts and from Treasury's own tables, the limit P.L. 118-5
# s401(b) reinstated on 2 January 2025 reproduced from the record's own debt, the record's last extraordinary-measures episode dated, and Treasury's
# perimeters at the seed's vintage. Writes docs/generated/US_DEBT_LIMIT.md; UsDebtLimitCheck re-reads it (the cheap bar): it recomputes every line of the
# document's Data and Figures blocks that comes from a Treasury JSON table or an act's text, finds each printed in the prose, and holds the prose by the
# body digest this script writes into the stamp; it writes the document's engine block (the bridge to the game's seed) with its WriteBridge entry.
# Usage (from the project root): perl Tools/us_debt_limit_prep.pl [md]
#
# READ, NOT TYPED: every amount comes from a Treasury file (MSPD tables 1 and 2, the Daily Treasury Statement's table III-C, Treasury's daily table of
# the 2025 measures) and every date from an act's "Approved ..." line or its text, Treasury's report, or the DTS's own calendar (the last or first day
# it publishes on either side of an act's date). THE TYPED CONSTANTS, each stated where it is used:
#   - the month Treasury projected its cash and the 2025 measures would run out ("in August", the Secretary's letter of 9 May 2025): read from the
#     letter's page image, which carries no text layer;
#   - the vintage month-end: the last month-end before WorldClock.SeedVintage, which UsDebtLimitCheck holds the two together on;
#   - two CONVENTIONS for reading the DTS: "at the limit" is a debt subject to limit within $100 million of it (Treasury's working margin in both
#     episodes is about $25 million), "well below" is more than $1,000 million under;
#   - the documents' own dates in citations (a letter's, the CSRDF report's, the daily table's), typed as each document prints its date.
# Every label the prose gives a derived day - a heading's date, a weekday, a month, a year - is formatted from that day.
# The two PDFs with a text layer are read through `pdftotext -raw` (Git for Windows' mingw64 build carries it); the check cannot re-read them, so their two
# readings (the 2025 episode's dates, also held by the DTS; the measures' size) are held by the files' digests and the body digest.
use strict;
use warnings;
use utf8;
use Digest::SHA qw(sha256_hex);
use Encode qw(encode_utf8);
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
my $AT_LIMIT_MIL = 100;      # CONVENTION (above): "at the limit"
my $WELL_BELOW_MIL = 1000;   # CONVENTION (above): "well below"
my $LINES_PER_DEBT = 6;      # the DTS lines one debt-subject-to-limit figure is computed from (see dts below), each published to the whole million

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
# the DTS's own calendar around a date: the last day it publishes on or before / strictly before it, the first strictly after it
sub last_on_or_before { my ($rows, $d) = @_; my ($x) = grep { $_ le $d } reverse dts_dates($rows); die "the DTS window has no day on or before $d\n" unless $x; return $x; }
sub last_before { my ($rows, $d) = @_; my ($x) = grep { $_ lt $d } reverse dts_dates($rows); die "the DTS window has no day before $d\n" unless $x; return $x; }
sub first_after { my ($rows, $d) = @_; my ($x) = grep { $_ gt $d } dts_dates($rows); die "the DTS window has no day after $d\n" unless $x; return $x; }

sub usd_of_mil { my ($mil) = @_; my $x = Math::BigFloat->new($mil); $x->bmul(1_000_000); return $x; }
sub group { my ($n) = @_; my ($i, $f) = split /\./, "$n"; my $neg = $i =~ s/^-//; 1 while $i =~ s/^(\d+)(\d{3})/$1,$2/; return ($neg ? '-' : '') . $i . (defined $f && length $f ? ".$f" : ''); }
my %MON = (January => 1, February => 2, March => 3, April => 4, May => 5, June => 6, July => 7, August => 8, September => 9, October => 10, November => 11, December => 12);
my %NUM = reverse %MON;
sub iso { my ($m, $d, $y) = @_; die "no month $m\n" unless $MON{$m}; return sprintf('%04d-%02d-%02d', $y, $MON{$m}, $d); }
sub words { my @x = split /-/, $_[0]; return sprintf('%s %d, %d', $NUM{$x[1] + 0}, $x[2], $x[0]); }
sub long_day { my @x = split /-/, $_[0]; return sprintf('%d %s %d', $x[2], $NUM{$x[1] + 0}, $x[0]); }
sub month_year { my @x = split /-/, $_[0]; return sprintf('%s %d', $NUM{$x[1] + 0}, $x[0]); }
sub weekday { my @x = split /-/, $_[0]; return (qw(Sunday Monday Tuesday Wednesday Thursday Friday Saturday))[(gmtime(timegm(0, 0, 0, $x[2], $x[1] - 1, $x[0])))[6]]; }
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
# the limit in force + the face amount outstanding on the reinstatement's day - the face amount outstanding on the date of enactment. The enactment day is
# P.L. 118-5's approval; the DTS publishes no Saturday, so its outstanding is the close of the last day the DTS publishes on or before it, which must be the
# opening of the first day after it. "On" the reinstatement's day is "at the end of the previous day" (Treasury's letter of 27 Dec 2024, page image) - the
# opening of that day, which must be the close of the last day the DTS publishes before it.
my $dts06 = rows_of($P{dts2306});
my $dts01 = rows_of($P{dts2501});
my $enactClose = last_on_or_before($dts06, $d118);
my $enactOpen = first_after($dts06, $d118);
my ($dslEnact, $limEnact) = dts($dts06, $enactClose, 'close');
my ($dslEnactNext) = dts($dts06, $enactOpen, 'open');
die "DTS: the close of $enactClose ($dslEnact) is not the opening of $enactOpen ($dslEnactNext)\n" unless $dslEnact == $dslEnactNext;
die "DTS: the limit line on $enactClose ($limEnact) is not Treasury's P.L. 117-73 limit\n" unless usd_of_mil($limEnact)->bcmp($L1) == 0;
my $priorClose = last_before($dts01, $reinstated);
my ($dslReinOpen) = dts($dts01, $reinstated, 'open');
my ($dslReinClose) = dts($dts01, $reinstated, 'close');
my ($dslPriorClose) = dts($dts01, $priorClose, 'close');
die "DTS: the opening of $reinstated ($dslReinOpen) is not the close of $priorClose ($dslPriorClose)\n" unless $dslReinOpen == $dslPriorClose;
my $reproducedMil = $limEnact + $dslReinOpen - $dslEnact;   # whole millions, as the DTS publishes
my $gapMil = Math::BigFloat->new($reproducedMil)->bsub(scalar Math::BigFloat->new($L2)->bdiv(1_000_000));   # scalar: bdiv in list context is a floored quotient and its remainder
# the rounding the reproduction can carry: half a million on each published line it combines - the limit line and the six lines of each of the two debts
my $boundMil = 0.5 * (1 + 2 * $LINES_PER_DEBT);
die "the reproduction " . group($reproducedMil) . " mil is $gapMil mil from Treasury's figure - beyond the rounding of the lines it combines ($boundMil)\n" unless $gapMil->copy->babs->bcmp($boundMil) <= 0;
my $closeWould = $limEnact + $dslReinClose - $dslEnact;
# to the dollar: MSPD table 2's month-end before the reinstatement is the same day's close as the DTS's prior close; the DTS publishes it to the million
my ($monthEnd) = grep { $_ lt $reinstated } reverse sort map { $_->{record_date} } grep { $_->{debt_limit_class1_desc} eq 'Total Public Debt Subject to Limit' } @$m2b;
die "MSPD's last month-end before $reinstated ($monthEnd) is not the DTS's prior close ($priorClose)\n" unless $monthEnd eq $priorClose;
my $dslMonthEndUsd = usd_of_mil(mspd2($m2b, $monthEnd, 'Total Public Debt Subject to Limit'));
die "MSPD's $monthEnd figure " . group($dslMonthEndUsd) . " is outside the rounding of the DTS's $dslPriorClose\n" unless $dslMonthEndUsd->copy->bdiv(1_000_000)->bsub($dslPriorClose)->babs->bcmp(0.5 * $LINES_PER_DEBT) <= 0;
my $headroomUsd = $L2->copy->bsub($dslMonthEndUsd);                          # what s401(b) carried: the limit in force less the debt on enactment
my $impliedEnactUsd = $L1->copy->bsub($headroomUsd);
my $impliedGapMil = $impliedEnactUsd->copy->bdiv(1_000_000)->bsub($dslEnact);
die "the implied enactment-day debt " . group($impliedEnactUsd) . " is outside the rounding of the DTS's $dslEnact\n" unless $impliedGapMil->copy->babs->bcmp(0.5 * $LINES_PER_DEBT) <= 0;

# ------------------------------------------------------------------ the record's last episode of extraordinary measures (2025)
my $csrdf = pdf_text($P{csrdf});
die "the CSRDF report does not date the episode's start\n" unless $csrdf =~ /On ([A-Z][a-z]+) (\d{1,2}), (\d{4}), the outstanding debt subject to the limit reached the statutory debt limit\./;
my $emReached = iso($1, $2, $3);
die "the CSRDF report does not date the episode's end\n" unless $csrdf =~ /On ([A-Z][a-z]+) (\d{1,2}), (\d{4}), Public Law 119-21 increased the statutory debt limit by \$5 trillion/;
my $emEnded = iso($1, $2, $3);
die "the CSRDF report's end ($emEnded) is not P.L. 119-21's approval ($d119)\n" unless $emEnded eq $d119;
my $emDays = days_between($emReached, $emEnded);
# the DTS agrees: debt subject to limit at the limit from that day, well below it on the day the DTS publishes before
my $before = last_before($dts01, $emReached);
my ($dslBefore, $limBefore) = dts($dts01, $before, 'close');
my ($dslAt, $limAt) = dts($dts01, $emReached, 'close');
my $dts07 = rows_of($P{dts2507});
my $lastIn = last_before($dts07, $emEnded);
my $firstAfter = first_after($dts07, $emEnded);
my ($dslLast, $limLast) = dts($dts07, $lastIn, 'close');
my ($dslAfter, $limAfter) = dts($dts07, $firstAfter, 'close');
die "DTS: on $emReached the debt is " . ($limAt - $dslAt) . " mil under the limit - not at it\n" unless $limAt - $dslAt < $AT_LIMIT_MIL;
die "DTS: on $before the debt is already at the limit\n" unless $limBefore - $dslBefore > $WELL_BELOW_MIL;
die "DTS: on $lastIn the debt is not at the limit\n" unless $limLast - $dslLast < $AT_LIMIT_MIL;
die "DTS: on $firstAfter the limit is not P.L. 119-21's\n" unless usd_of_mil($limAfter)->copy->bsub($L3)->babs->bcmp(1_000_000) < 0;
# TYPED, from a page image: the Secretary's letter of 9 May 2025 - "there is a reasonable probability that the federal government's cash and extraordinary
# measures will be exhausted in August while Congress is scheduled to be in recess". The projection is of the cash and the measures together, and a month,
# so its first and last days bound it
my ($exFrom, $exTo) = ('2025-08-01', '2025-08-31');
my ($exDaysFrom, $exDaysTo) = (days_between($emReached, $exFrom), days_between($emReached, $exTo));
# the measures' size on the first day: Treasury's daily table, the episode's first row, "Totals: Total Measures" ($ billions)
my $em = pdf_text($P{emtable});
my $first = words($emReached);
die "the measures' table has no row for $emReached\n" unless $em =~ /\Q$first\E((?: \(?\d+\)?){12})/;
my @cells = map { (my $c = $_) =~ s/[()]//g; $c } grep { length } split / /, $1;
my $emTotalBn = $cells[9];
die "the measures' table's first row does not add up: CSRDF $cells[0] + G Fund $cells[3] + ESF $cells[6] against $emTotalBn\n" unless abs($cells[0] + $cells[3] + $cells[6] - $emTotalBn) <= 1;

# ------------------------------------------------------------------ Treasury's perimeters (MSPD table 1 and table 2) at the reinstatement and at the seed's vintage
my $m1 = rows_of($P{mspd1});
sub mspd1 { my ($date, $col) = @_; for (@$m1) { return $_->{$col} if $_->{record_date} eq $date && $_->{security_type_desc} eq 'Total Public Debt Outstanding'; } die "MSPD table 1 has no total on $date\n"; }
my $VINTAGE = '2026-06-30';   # TYPED: the last month-end before WorldClock.SeedVintage - UsDebtLimitCheck asserts the two together
my %per;
for my $d ($monthEnd, $VINTAGE) {
    $per{$d} = {
        public => usd_of_mil(mspd1($d, 'debt_held_public_mil_amt')), intragov => usd_of_mil(mspd1($d, 'intragov_hold_mil_amt')),
        tpdo => usd_of_mil(mspd1($d, 'total_mil_amt')), dsl => usd_of_mil(mspd2($m2b, $d, 'Total Public Debt Subject to Limit')),
    };
}
my ($dslVintageDts) = dts(rows_of($P{dts2606}), $VINTAGE, 'close');
die "DTS's close of $VINTAGE ($dslVintageDts mil) is outside the rounding of MSPD's " . group($per{$VINTAGE}{dsl}) . "\n" unless $per{$VINTAGE}{dsl}->copy->bdiv(1_000_000)->bsub($dslVintageDts)->babs->bcmp(0.5 * $LINES_PER_DEBT) <= 0;
# the 2023 episode, for the span's range only: the day the DTS first shows the debt at the limit, to P.L. 118-5's suspension
my $dts23 = rows_of($P{dts2301});
my ($em23) = grep { my ($x, $l) = dts($dts23, $_, 'close'); $l - $x < $AT_LIMIT_MIL } dts_dates($dts23);
die "the 2023 window shows no day at the limit\n" unless $em23;
my $em23Days = days_between($em23, $d118);

# ------------------------------------------------------------------ the document: the body (held by its digest in the stamp), then the engine's block
my $md = "# The US federal debt limit as dated data - the statute's steps, the 2025 reinstatement reproduced, the record's last extraordinary measures, Treasury's perimeters (PS-6 US-14)\n\n";
$md .= "Read on the files the stamp names (`ElectionsData/usa/raw/treasury_debt/`, `raw/debt_limit/`, `raw/budget_statutes/`; the reading is `ElectionsData/usa/debt_limit.md`). Treasury's tables give USD millions; a figure here is that figure times a million, nothing rounded. `UsDebtLimitCheck` (the cheap bar) recomputes from the same JSON tables and acts every line of the Data and Figures blocks below that rests on them, finds each figure printed in the prose, and holds this body by the digest in the stamp. Two readings rest on PDFs the check cannot re-read - the 2025 episode's dates (which the DTS also holds) and the measures' size - and are held by those files' digests and the body digest.\n\n";
$md .= "## The limit by date\n\n| in force from | limit (USD) | the act | as Treasury carried it (MSPD table 2, USD millions) |\n|---|---:|---|---|\n";
$md .= sprintf("| %s | %s | P.L. 117-73 (approved %s): the limit then in force \"is increased by \$%s\" | %s from %s (%s on %s) |\n", $d117, group($L1), $d117, group($add117), group($steps[1][1]), $steps[1][0], group($steps[0][1]), $steps[0][0]);
$md .= sprintf("| %s | suspended through %s | P.L. 118-5 s401(a) (approved %s) | %s from %s |\n", $d118, $suspendedThrough, $d118, $steps[2][1], $steps[2][0]);
$md .= sprintf("| %s | %s | P.L. 118-5 s401(b): the limit in force raised by the debt issued while suspended - Treasury's figure, reproduced below | %s from %s |\n", $reinstated, group($L2), group($steps[3][1]), $steps[3][0]);
$md .= sprintf("| %s | %s | P.L. 119-21 s72001 (approved %s): \"is increased by \$%s\" | %s from %s |\n\n", $d119, group($L3), $d119, group($add119), group($steps[4][1]), $steps[4][0]);
$md .= "## The reinstatement of " . long_day($reinstated) . ", reproduced from the record's own debt\n\n";
$md .= "P.L. 118-5 s401(b): the limit in force, raised by the face amount outstanding on the reinstatement's day less that outstanding on the date of enactment. Debt subject to limit from the Daily Treasury Statement's table III-C (USD millions; held by the public + intragovernmental - the three lines not subject + guaranteed debt, each line published to the whole million):\n\n";
$md .= sprintf("- the limit in force on the date of enactment: **%s** mil (the DTS's limit line on %s);\n", group($limEnact), $enactClose);
$md .= sprintf("- outstanding on the date of enactment (%s, a %s - the close of %s, the last day the DTS publishes on or before it, which is the opening of %s): **%s** mil;\n", $d118, weekday($d118), $enactClose, $enactOpen, group($dslEnact));
$md .= sprintf("- outstanding on %s, read as Treasury read it - \"at the end of the previous day\" (the Secretary's letter of 27 December 2024, read from the page image): that day's opening, **%s** mil, the close of %s;\n", $reinstated, group($dslReinOpen), $priorClose);
$md .= sprintf("- reproduced: %s + %s - %s = **%s mil**, against Treasury's **%s**, a difference of %s mil, inside the rounding the reproduction can carry: half a million on each of the %d published lines it combines, at most %s mil.\n", group($limEnact), group($dslReinOpen), group($dslEnact), group($reproducedMil), group($L2), "$gapMil", 1 + 2 * $LINES_PER_DEBT, $boundMil);
$md .= sprintf("- %s's CLOSE (%s mil, after the redemption the letter names) would have given %s mil - %s mil lower: the formula reads the day's opening.\n", $reinstated, group($dslReinClose), group($closeWould), group($reproducedMil - $closeWould));
$md .= sprintf("- To the dollar: MSPD table 2's debt subject to limit on %s is %s - the same day's close as the DTS's %s mil, which it lies inside the rounding of (the DTS publishes no finer figure, so that the two are one close is the reading). On that reading s401(b) carried a headroom of **%s** (Treasury's limit less that debt), and the enactment-day debt Treasury's figure implies is %s, a difference of %s mil from the DTS's %s, inside its rounding (half a million on each of its %d lines).\n", $monthEnd, group($dslMonthEndUsd), group($dslPriorClose), group($headroomUsd), group($impliedEnactUsd), "$impliedGapMil", group($dslEnact), $LINES_PER_DEBT);
$md .= sprintf("- With P.L. 119-21's \$%s: **%s**.\n\n", group($add119), group($L3));
$md .= "## The record's last extraordinary measures (R-US21 (iii))\n\n";
$md .= sprintf("- **Reached %s, ended %s: %d days.** Treasury's report on the CSRDF (30 January 2026): \"On %s, the outstanding debt subject to the limit reached the statutory debt limit\"; the episode ended with P.L. 119-21. The DTS agrees: %s mil under the limit on %s (the day it publishes before), %s under on %s and %s under on %s (the last day before the act); on %s the limit line reads %s.\n",
    $emReached, $emEnded, $emDays, $first, group($limBefore - $dslBefore), $before, group($limAt - $dslAt), $emReached, group($limLast - $dslLast), $lastIn, $firstAfter, group($limAfter));
$md .= sprintf("- **Treasury's CASH AND the measures projected to run out in %s (%s to %s): %d to %d days from %s.** The Secretary's letter of 9 May 2025 (page image, no text layer): \"there is a reasonable probability that the federal government's cash and extraordinary measures will be exhausted in August\". The month is the reading; its first and last days bound it. The span counts days Treasury's cash balance bought as well as the measures; the game holds no cash balance.\n", month_year($exFrom), $exFrom, $exTo, $exDaysFrom, $exDaysTo, $emReached);
$md .= sprintf("- **The measures' size on the first day: \$%d billion (%s)** - Treasury's daily table (\"Debt Subject to Limit - Extraordinary Measures\", 3 July 2025), the %s row, Totals \"Total Measures\" (CSRDF/PSRHBF %d + G Fund %d + ESF %d).\n", $emTotalBn, group($emTotalBn * 1_000_000_000), $first, $cells[0], $cells[3], $cells[6]);
$md .= sprintf("- The %s episode, for the range: the DTS first shows the debt at the limit on %s; P.L. 118-5 suspended it on %s - %d days.\n\n", substr($em23, 0, 4), $em23, $d118, $em23Days);
$md .= "## Treasury's perimeters (MSPD tables 1 and 2), USD\n\n| | $monthEnd (the reinstatement's opening) | $VINTAGE (the seed's vintage) |\n|---|---:|---:|\n";
for (['debt held by the public', 'public'], ['intragovernmental holdings', 'intragov'], ['total public debt outstanding', 'tpdo'], ['debt subject to limit', 'dsl']) {
    $md .= sprintf("| %s | %s | %s |\n", $_->[0], group($per{$monthEnd}{$_->[1]}), group($per{$VINTAGE}{$_->[1]}));
}
# the Data block is read by the engine block's stamp too (its digest), so it stays as it was; the Figures block carries every other figure the prose prints
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
$md .= "<!-- US14-DATA END -->\n\n<!-- US14-FIGURES BEGIN -->\n";
$md .= "mspd.step.$_ = $steps[$_ - 1][0] | $steps[$_ - 1][1]\n" for 1 .. 5;
$md .= "law.1.increase_usd = $add117\nlaw.4.increase_usd = $add119\n";
$md .= "reinstatement.enactment_close = $enactClose\nreinstatement.enactment_open = $enactOpen\nreinstatement.limit_in_force_mil = $limEnact\nreinstatement.enactment_dsl_mil = $dslEnact\n";
$md .= "reinstatement.prior_close = $priorClose\nreinstatement.opening_dsl_mil = $dslReinOpen\nreinstatement.gap_mil = $gapMil\nreinstatement.rounding_bound_mil = $boundMil\n";
$md .= "reinstatement.close_dsl_mil = $dslReinClose\nreinstatement.close_would_mil = $closeWould\nreinstatement.close_lower_mil = " . ($reproducedMil - $closeWould) . "\n";
$md .= "reinstatement.month_end = $monthEnd\nreinstatement.implied_enactment_usd = $impliedEnactUsd\nreinstatement.implied_gap_mil = $impliedGapMil\n";
$md .= "reinstatement.$_\_usd = " . $per{$monthEnd}{$_} . "\n" for qw(dsl tpdo public intragov);
$md .= "em.before = $before\nem.margin_before_mil = " . ($limBefore - $dslBefore) . "\nem.margin_reached_mil = " . ($limAt - $dslAt) . "\n";
$md .= "em.last_in = $lastIn\nem.margin_last_mil = " . ($limLast - $dslLast) . "\nem.first_after = $firstAfter\nem.limit_after_mil = $limAfter\n";
$md .= "em.total_measures_parts_bn = $cells[0] | $cells[3] | $cells[6] | $emTotalBn\n";
$md .= "em2023.reached = $em23\nem2023.ended = $d118\nem2023.days = $em23Days\n";
$md .= "<!-- US14-FIGURES END -->\n\n";
$md .= "## The bridge to the game's seed (PN-4: same year, same perimeter)\n\n";
$md .= "Written by the engine, which alone holds the seed: `-executeMethod PoliSim.EditorTools.UsDebtLimitCheck.WriteBridge`. The cheap bar fails while it differs from what `UsDebtLimitCheck` measures.\n\n";
my $bodyDigest = sha256_hex(encode_utf8($md));
# the engine's block survives a re-derivation of the rest
my ($engine) = (-e $out ? slurp($out) : '') =~ /(<!-- GENERATED by PoliSim\.EditorTools\.UsDebtLimitCheck\.WriteBridge.*?<!-- END GENERATED -->)/s;
$engine //= "<!-- GENERATED by PoliSim.EditorTools.UsDebtLimitCheck.WriteBridge. DO NOT EDIT BY HAND. -->\nNOT YET WRITTEN - run the entry above.\n<!-- END GENERATED -->";
$engine =~ s/\r\n/\n/g;
my $stamp = join(' ', map { "$_->[0]=" . digest_of($_->[1]) } @IN);
my $doc = "<!-- GENERATED by Tools/us_debt_limit_prep.pl. DO NOT EDIT BY HAND. Re-derive: perl Tools/us_debt_limit_prep.pl. inputs-sha256: $stamp body-sha256: $bodyDigest -->\n" . $md . "$engine\n";

open my $o, '>:raw:encoding(UTF-8)', $out or die "$out: $!";
print $o $doc;
close $o;
print "$out written: limits ", group($L1), " / suspended / ", group($L2), " / ", group($L3), "; reproduced ", group($reproducedMil), " mil ($gapMil from Treasury's, bound $boundMil); measures $emReached..$emEnded = $emDays days, exhaustion $exDaysFrom..$exDaysTo, \$$emTotalBn bn; vintage DSL ", group($per{$VINTAGE}{dsl}), "; body $bodyDigest\n";
