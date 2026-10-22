#!/usr/bin/perl
# PS-6 US-27 (docs/specs/USA_STAGE_PLAN.md US-27; R-US20 RULED (b), Elias's ruling I4, 2026-10-18: "a FITTED midterm term on the president's party
# from the record since 1946, with n and its interval printed"): THE MIDTERM TERM, FITTED on US-13's record.
# Reads ElectionsData/usa/house_midterms.csv (written by Tools/us_midterm_record.pl from the saved pages; its digest is taken over its text with LF
# line ends, as that tool writes it, and carried into the generated file, which UsMidtermFitCheck holds against the CSV).
# THE FIT (DECLARED, PROVISIONAL under I16 / G5 - put to Elias with US-27):
#   - one term, on the president's party, in points of the two-party House vote (the unit R-US18 (b)'s count swings in, so its seat change is the
#     count's own): the intercept of the president's party's midterm vote swing - the mean - with its 95 % interval of the mean (Student's t on
#     n - 1 degrees of freedom, the quantile computed here by bisection on the regularised incomplete beta, DERIVED);
#   - the record since 1946: every midterm of the CSV from 1946 to 2022 (kind "midterm"). The only vote swing the record carries for every one of
#     them is the SECONDARY column (Vital Statistics on Congress, Table 2-2: its change of the major-party vote from the last election, to the
#     decimals it prints, signed for the president's party - vs_vote_change, vs_vote_direction). The Clerk's own vote reaches only 2002-2022
#     (its 1944-1996 statistics are page images, BILLED), so the Clerk's fit is printed beside on its n; and the seat change of every midterm
#     (seats_* of the row and the row before) is printed beside, never fitted;
#   - unified and divided midterms (the CSV's senate_majority with the House's largest party, as US_MIDTERM_RECORD.md reads them) are printed
#     beside, never fitted apart: I4 rules one term.
# Writes Assets/Scripts/Elections/Generated/UsMidtermTerm.cs. UsMidtermFitCheck re-reads the CSV, refits and fails while the stored constants are
# not the fit; UsCongressElection reads Generated.UsMidtermTerm.MeanPoints on a midterm polling day.
# Usage (from the project root): perl Tools/us_midterm_fit.pl [--dry]
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);

my $dry = @ARGV && $ARGV[0] eq '--dry';
die "run from the project root\n" unless -d 'Assets/Scripts/Elections';
my $csvPath = 'ElectionsData/usa/house_midterms.csv';
my $cs = 'Assets/Scripts/Elections/Generated/UsMidtermTerm.cs';
my ($from, $to) = (1946, 2022);

open my $fh, '<:raw', $csvPath or die "$csvPath: $!\n";
my $text = do { local $/; <$fh> };
close $fh;
$text =~ s/\r\n/\n/g;
my $digest = sha256_hex($text);

my (@head, %row, @years);
for my $line (split /\n/, $text) {
    next if $line =~ /^#/ || $line eq '';
    my @f = split /,/, $line, -1;
    if (!@head) { @head = @f; next; }
    die "a row of " . scalar(@f) . " fields under a header of " . scalar(@head) . "\n" unless @f == @head;
    my %h; @h{@head} = @f;
    $row{$h{year}} = \%h;
    push @years, $h{year};
}

sub house_major { my $r = shift; return $r->{seats_r} > $r->{seats_d} ? 'R' : $r->{seats_d} > $r->{seats_r} ? 'D' : '-'; }

my (@secondary, @clerk, @seats, @rows);
for my $y (@years) {
    my $r = $row{$y};
    next unless $r->{kind} eq 'midterm' && $y >= $from && $y <= $to;
    my $p = $r->{president_party};
    die "$y: the president's party '$p' is neither D nor R\n" unless $p eq 'D' || $p eq 'R';
    die "$y: no SECONDARY swing\n" if $r->{vs_vote_change} eq '-' || $r->{vs_vote_direction} eq '-';
    my $vs = ($r->{vs_vote_direction} eq $p ? 1 : -1) * $r->{vs_vote_change};
    my $b = $row{$y - 2} or die "$y: no row for the election two years before\n";
    # the government on the day: the House sitting is the one the election two years before chose ($b's), the Senate the row's own senate_majority
    my $unified = house_major($b) eq $p && $r->{senate_majority} eq $p ? 'unified' : 'divided';
    my $seat = ($p eq 'R' ? $r->{seats_r} - $b->{seats_r} : $r->{seats_d} - $b->{seats_d});
    my $clerkSwing;
    if ($r->{clerk_r} ne '-' && $b->{clerk_r} ne '-') {
        my $after = ($p eq 'R' ? $r->{clerk_r} : $r->{clerk_d}) / ($r->{clerk_r} + $r->{clerk_d});
        my $before = ($p eq 'R' ? $b->{clerk_r} : $b->{clerk_d}) / ($b->{clerk_r} + $b->{clerk_d});
        $clerkSwing = 100 * ($after - $before);
        push @clerk, $clerkSwing;
    }
    push @secondary, $vs;
    push @seats, $seat;
    push @rows, [$y, $p, $unified, $vs, $clerkSwing, $seat];
}

# ---- Student's t: the regularised incomplete beta by its continued fraction (Lentz), ln Gamma by Lanczos (g = 7, nine terms), the quantile by bisection
my @lanczos = (0.99999999999980993, 676.5203681218851, -1259.1392167224028, 771.32342877765313, -176.61502916214059,
               12.507343278686905, -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7);
sub lngamma {
    my ($x) = @_;
    my $pi = 4 * atan2(1, 1);
    return log($pi / abs(sin($pi * $x))) - lngamma(1 - $x) if $x < 0.5;
    $x -= 1;
    my $a = $lanczos[0];
    my $t = $x + 7.5;
    $a += $lanczos[$_] / ($x + $_) for 1 .. 8;
    return 0.5 * log(2 * $pi) + ($x + 0.5) * log($t) - $t + log($a);
}
sub betacf {
    my ($a, $b, $x) = @_;
    my ($tiny, $eps) = (1e-300, 1e-16);
    my ($qab, $qap, $qam) = ($a + $b, $a + 1, $a - 1);
    my $c = 1;
    my $d = 1 - $qab * $x / $qap; $d = $tiny if abs($d) < $tiny; $d = 1 / $d;
    my $h = $d;
    for my $m (1 .. 10000) {
        my $m2 = 2 * $m;
        my $aa = $m * ($b - $m) * $x / (($qam + $m2) * ($a + $m2));
        $d = 1 + $aa * $d; $d = $tiny if abs($d) < $tiny; $c = 1 + $aa / $c; $c = $tiny if abs($c) < $tiny; $d = 1 / $d; $h *= $d * $c;
        $aa = -($a + $m) * ($qab + $m) * $x / (($a + $m2) * ($qap + $m2));
        $d = 1 + $aa * $d; $d = $tiny if abs($d) < $tiny; $c = 1 + $aa / $c; $c = $tiny if abs($c) < $tiny; $d = 1 / $d;
        my $del = $d * $c; $h *= $del;
        return $h if abs($del - 1) < $eps;
    }
    die "the incomplete beta's continued fraction did not converge\n";
}
sub ibeta {
    my ($a, $b, $x) = @_;
    return 0 if $x <= 0;
    return 1 if $x >= 1;
    my $bt = exp(lngamma($a + $b) - lngamma($a) - lngamma($b) + $a * log($x) + $b * log(1 - $x));
    return $x < ($a + 1) / ($a + $b + 2) ? $bt * betacf($a, $b, $x) / $a : 1 - $bt * betacf($b, $a, 1 - $x) / $b;
}
sub tcdf { my ($t, $nu) = @_; my $tail = 0.5 * ibeta($nu / 2, 0.5, $nu / ($nu + $t * $t)); return $t >= 0 ? 1 - $tail : $tail; }
sub tinv {
    my ($p, $nu) = @_;
    my ($lo, $hi) = (0, 1000);
    for (1 .. 200) { my $mid = 0.5 * ($lo + $hi); if (tcdf($mid, $nu) < $p) { $lo = $mid; } else { $hi = $mid; } }
    return 0.5 * ($lo + $hi);
}
sub fit {
    my @v = @_;
    my $n = @v;
    die "a fit of fewer than two observations\n" if $n < 2;
    my $sum = 0; $sum += $_ for @v;
    my $mean = $sum / $n;
    my $ss = 0; $ss += ($_ - $mean) ** 2 for @v;
    my $sd = sqrt($ss / ($n - 1));
    my $t = tinv(0.975, $n - 1);
    my $half = $t * $sd / sqrt($n);
    return ($n, $mean, $sd, $t, $mean - $half, $mean + $half);
}

my ($n, $mean, $sd, $t, $lo, $hi) = fit(@secondary);
my ($cn, $cmean, $csd, $ct, $clo, $chi) = fit(@clerk);
my @uni = map { $_->[3] } grep { $_->[2] eq 'unified' } @rows;
my @div = map { $_->[3] } grep { $_->[2] eq 'divided' } @rows;
my ($un, $umean) = (scalar(@uni), 0); $umean += $_ / $un for @uni;
my ($dn, $dmean) = (scalar(@div), 0); $dmean += $_ / $dn for @div;
my ($sn, $smean) = (scalar(@seats), 0); $smean += $_ / $sn for @seats;

printf "US-27 MIDTERM TERM: n %d midterms %d-%d (SECONDARY); mean %.6f pp, sd %.6f, t(0.975, %d) %.9f, 95%% interval of the mean %.6f to %.6f pp\n", $n, $from, $to, $mean, $sd, $n - 1, $t, $lo, $hi;
printf "  beside: the Clerk n %d, mean %.6f pp, 95%% interval %.6f to %.6f (t %.9f); unified n %d mean %.4f, divided n %d mean %.4f; seat change n %d mean %.4f\n",
    $cn, $cmean, $clo, $chi, $ct, $un, $umean, $dn, $dmean, $sn, $smean;
printf "  %d %s %s SECONDARY %+.1f Clerk %s seats %+d\n", $_->[0], $_->[1], $_->[2], $_->[3], defined $_->[4] ? sprintf('%+.4f', $_->[4]) : '-', $_->[5] for @rows;
print "  $csvPath sha256 $digest (its text with LF line ends)\n";
exit 0 if $dry;

sub g { return sprintf('%.17g', shift); }
my $yearList = join(', ', map { $_->[0] } @rows);
my $swingList = join(', ', map { g($_->[3]) } @rows);
my $out = <<"CS";
// GENERATED by Tools/us_midterm_fit.pl. DO NOT EDIT BY HAND.
//
// Source : $csvPath (written by Tools/us_midterm_record.pl from the saved pages)
// SHA-256: $digest (its text with LF line ends)
//
// UsMidtermFitCheck re-reads the CSV and refits every figure.

namespace PoliSim.Elections.Generated
{
    /// <summary>PS-6 US-27 - R-US20 RULED (b) (Elias's ruling I4, `COMPLETED.md` §856): the midterm term on the president's party - FITTED on US-13's
    /// record since 1946, the mean of the president's party's midterm vote swing with its 95 % interval of the mean (Student's t, DERIVED), in points of
    /// the two-party House vote. The swings are the SECONDARY compilation's (Vital Statistics on Congress, Table 2-2, as the record prints it), the one
    /// vote swing the record carries for every midterm since 1946 (DECLARED, PROVISIONAL - put to Elias); the Clerk's own, 2002-2022, beside.
    /// Generated, never hand-edited.</summary>
    public static class UsMidtermTerm
    {
        public const string SourceDigest = "$digest";
        public const int From = $from, To = $to;

        /// <summary>The midterms fitted, in year order, and each one's swing of the president's party (SECONDARY), in points.</summary>
        public static readonly int[] Years = { $yearList };
        public static readonly double[] SwingPoints = { $swingList };

        /// <summary>FITTED (one parameter, the intercept): n, the mean swing, its sample sd, Student's t at 0.975 on n - 1 degrees of freedom and the
        /// 95 % interval of the mean, in points.</summary>
        public const int N = $n;
        public const double MeanPoints = ${\ g($mean)};
        public const double SdPoints = ${\ g($sd)};
        public const double TQuantile = ${\ g($t)};
        public const double LowerPoints = ${\ g($lo)};
        public const double UpperPoints = ${\ g($hi)};

        /// <summary>Printed beside, never read by the count: the Clerk's own vote swing at the midterms its text pages read (2002-2022) - n, the mean and
        /// its 95 % interval, in points.</summary>
        public const int ClerkN = $cn;
        public const double ClerkMeanPoints = ${\ g($cmean)};
        public const double ClerkLowerPoints = ${\ g($clo)};
        public const double ClerkUpperPoints = ${\ g($chi)};

        /// <summary>Printed beside, never fitted apart (I4 rules one term): the unified and the divided midterms' mean swings (SECONDARY), and the mean
        /// seat change of the president's party ([HH-DIV]), with their n.</summary>
        public const int UnifiedN = $un, DividedN = $dn, SeatN = $sn;
        public const double UnifiedMeanPoints = ${\ g($umean)};
        public const double DividedMeanPoints = ${\ g($dmean)};
        public const double SeatChangeMean = ${\ g($smean)};
    }
}
CS
$out =~ s/\r?\n/\r\n/g;
open my $o, '>:raw', $cs or die "$cs: $!\n";
print $o $out;
close $o;
print "wrote $cs\n";
