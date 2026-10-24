#!/usr/bin/perl
# A-LOOP q13 (docs/specs/A_LOOP_PLAN.md section 4 (3), Elias's ruling J1 Q4): SIGMA_V, FITTED - the spread of incumbent vote swings that converts
# Brender & Drazen's probit index into points of the government's share (shift_gov = sigma_v x delta-z).
# The plan's definition: "the standard deviation of the PM's party's vote-share change between consecutive elections, over the estimating
# sample's frame (developed democracies, 1960-2003)", from the ParlGov election table.
# Reads, byte-exact as fetched (their digests in docs/reference/raw/aloop/parlgov/SHA256SUMS.txt, which this tool checks):
#   docs/reference/raw/aloop/parlgov/view_election.csv - every party's vote share at every election, with the cabinet in office at the
#     election (previous_cabinet_id) and the election before (previous_parliament_election_id);
#   docs/reference/raw/aloop/parlgov/view_cabinet.csv - every cabinet's parties, the prime minister's flagged (prime_minister = 1).
# THE FRAME (each a reading of [BD06], docs/reference/raw/aloop/tau_foerder_13-2006_brender_drazen.pdf):
#   - developed: the countries Table A2 marks "Developed Economy" (pdftotext -table of pages 36-37; Appendix I: "OECD economies that were
#     members of the organization during the entire sample period", the paper's "23 OECD countries"). ParlGov holds 22 of them - not the
#     United States, whose leader is the president - so the fit runs over the 22;
#   - 1960-2003: a parliamentary election dated 1960-01-01 to 2003-12-31 (the paper's sample period); the election before may fall earlier.
# ONE OBSERVATION per such election: the party flagged prime minister in the cabinet in office at the election, its vote share at the election
# minus its share at the previous parliamentary election, in points. An election is left out - each reason counted and printed - where the
# cabinet in office names no prime minister's party, where that party has no share at either election under its own id (a merger, a split, a
# joint list listed under another id), or where a share is empty. Since K64 sigma_v is the MAD-based robust scale (below); the sample
# standard deviation (n - 1) stays printed, and its 95 % interval, under normality, is the chi-square interval of a normal sd, the quantiles by Wilson-Hilferty (z = 1.959963984540054), DERIVED.
# Writes Assets/Scripts/Elections/Generated/IncumbentVoteSwing.cs (the constants and both digests); FiscalVoteFitCheck re-reads both CSVs,
# refits and fails while the stored constants are not the fit.
# Usage (from the project root): perl Tools/aloop_sigma_fit.pl [--dry]
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);

my $dry = @ARGV && $ARGV[0] eq '--dry';
die "run from the project root\n" unless -d 'Assets/Scripts/Elections';
my $D = 'docs/reference/raw/aloop/parlgov/';
my $cs = 'Assets/Scripts/Elections/Generated/IncumbentVoteSwing.cs';
my @developed = qw(AUS AUT BEL CAN DNK FIN FRA DEU GRC ISL IRL ITA JPN LUX NLD NZL NOR PRT ESP SWE TUR GBR);   # Table A2's 23 less the USA (not in ParlGov)
my ($from, $to) = ('1960-01-01', '2003-12-31');
my %dev = map { $_ => 1 } @developed;

sub slurp { my ($p) = @_; open my $fh, '<:raw', $p or die "$p: $!\n"; local $/; my $s = <$fh>; close $fh; return $s; }

# the digests the folder's SHA256SUMS.txt lists, held against the bytes
my %sums;
for (split /\n/, slurp($D . 'SHA256SUMS.txt')) { s/\r$//; $sums{$2} = $1 if /^([0-9a-f]{64}) \*?(\S+)$/; }
my %digest;
for my $f (qw(view_election.csv view_cabinet.csv)) {
    $digest{$f} = sha256_hex(slurp($D . $f));
    die "$f: its SHA-256 $digest{$f} is not the one SHA256SUMS.txt lists\n" unless ($sums{$f} // '') eq $digest{$f};
}

sub read_csv {
    my ($s) = @_;
    my (@rows, @row);
    my ($field, $inq, $i, $n) = ('', 0, 0, length $s);
    while ($i < $n) {
        my $c = substr($s, $i, 1);
        if ($inq) {
            if ($c eq '"') {
                if ($i + 1 < $n && substr($s, $i + 1, 1) eq '"') { $field .= '"'; $i += 2; next; }
                $inq = 0; $i++; next;
            }
            $field .= $c; $i++; next;
        }
        if ($c eq '"') { die "a quote inside an unquoted field\n" if length $field; $inq = 1; $i++; next; }
        if ($c eq ',') { push @row, $field; $field = ''; $i++; next; }
        if ($c eq "\r") { $i++; next; }
        if ($c eq "\n") { push @row, $field; push @rows, [@row]; @row = (); $field = ''; $i++; next; }
        $field .= $c; $i++;
    }
    push @row, $field if length($field) || @row;
    push @rows, [@row] if @row;
    die "an unterminated quote\n" if $inq;
    my $head = shift @rows;
    my @recs;
    for my $r (@rows) {
        die "a row of " . scalar(@$r) . " fields under a header of " . scalar(@$head) . "\n" unless @$r == @$head;
        my %h; @h{@$head} = @$r; push @recs, \%h;
    }
    return \@recs;
}

my $elections = read_csv(slurp($D . 'view_election.csv'));
my $cabinets = read_csv(slurp($D . 'view_cabinet.csv'));

my (%pmParty, %share, %elec);
for my $c (@$cabinets) { $pmParty{$c->{cabinet_id}}{$c->{party_id}} = 1 if $c->{prime_minister} eq '1'; }
for my $e (@$elections) {
    next unless $e->{election_type} eq 'parliament';
    $share{$e->{election_id}}{$e->{party_id}} = $e->{vote_share};
    $elec{$e->{election_id}} //= { country => $e->{country_name_short}, date => $e->{election_date}, prev => $e->{previous_parliament_election_id}, cabinet => $e->{previous_cabinet_id} };
}

my (@swing, %perCountry, %out);
for my $id (sort { $elec{$a}{country} cmp $elec{$b}{country} || $elec{$a}{date} cmp $elec{$b}{date} || $a <=> $b } keys %elec) {
    my $e = $elec{$id};
    next unless $dev{$e->{country}} && $e->{date} ge $from && $e->{date} le $to;
    my @pm = sort { $a <=> $b } keys %{ $pmParty{$e->{cabinet}} // {} };
    if (@pm != 1) { $out{@pm ? 'more than one prime minister\'s party' : 'no prime minister\'s party in the cabinet in office'}++; next; }
    my ($now, $then) = ($share{$id}{$pm[0]}, ($e->{prev} ne '' ? $share{$e->{prev}}{$pm[0]} : undef));
    if (!defined $now || !defined $then) { $out{'the party has no share at one of the two elections under its own id'}++; next; }
    if ($now eq '' || $then eq '') { $out{'an empty share'}++; next; }
    push @swing, $now - $then;
    $perCountry{$e->{country}}++;
}

my $n = @swing;
die "no observation\n" if $n < 2;
my $sum = 0; $sum += $_ for @swing;
my $mean = $sum / $n;
my $ss = 0; $ss += ($_ - $mean) ** 2 for @swing;
my $sd = sqrt($ss / ($n - 1));
my $k = $n - 1;
my $z = 1.959963984540054;
sub chi2q { my ($zp) = @_; my $a = 2 / (9 * $k); return $k * (1 - $a + $zp * sqrt($a)) ** 3; }
my ($lo, $hi) = ($sd * sqrt($k / chi2q($z)), $sd * sqrt($k / chi2q(-$z)));

# K64 (Elias's ruling of 2026-10-23: "sigma_v takes a robust scale"): the swings are fat-tailed, so the vote reads a ROBUST scale - the MAD-based
# one, 1.482602218505602 x the median absolute deviation from the median (the factor 1 / the normal's 75 % point, DERIVED, so the scale is a
# normal's sd where the swings are normal; breakdown point 50 %). Printed beside it: the IQR-based scale (the interquartile range, Hyndman-Fan
# type 7 quantiles - linear between order statistics - over 1.3489795003921634, twice the normal's 75 % point, DERIVED; breakdown 25 %) and the
# kurtosis (m4 / m2^2, population moments; a normal's is 3). The standard deviation and its interval stay printed, the interval under normality.
my @sorted = sort { $a <=> $b } @swing;
sub q7 { my ($s, $p) = @_; my $h = ($#$s) * $p; my $f = int($h); return $f >= $#$s ? $s->[-1] : $s->[$f] + ($h - $f) * ($s->[$f + 1] - $s->[$f]); }
my $median = q7(\@sorted, 0.5);
my @dev = sort { $a <=> $b } map { abs($_ - $median) } @swing;
my $mad = q7(\@dev, 0.5);
my $madScale = 1.482602218505602 * $mad;
my $iqrScale = (q7(\@sorted, 0.75) - q7(\@sorted, 0.25)) / 1.3489795003921634;
my ($m2, $m4) = (0, 0);
for (@swing) { $m2 += ($_ - $mean) ** 2; $m4 += ($_ - $mean) ** 4; }
$m2 /= $n; $m4 /= $n;
my $kurtosis = $m4 / ($m2 * $m2);

printf "SIGMA_V: n %d elections in %d countries (%s to %s); mean swing %.4f pp; sigma_v (MAD-based, read) %.6f pp; IQR-based %.6f pp; median %.4f pp, MAD %.4f pp; kurtosis %.3f; sd %.6f pp, 95%% interval under normality %.6f - %.6f pp\n",
    $n, scalar(keys %perCountry), $from, $to, $mean, $madScale, $iqrScale, $median, $mad, $kurtosis, $sd, $lo, $hi;
printf "  %s %d\n", $_, $perCountry{$_} for sort keys %perCountry;
printf "  left out: %s %d\n", $_, $out{$_} for sort keys %out;
printf "  view_election.csv %s\n  view_cabinet.csv %s\n", $digest{'view_election.csv'}, $digest{'view_cabinet.csv'};
exit 0 if $dry;

my $countries = join(', ', map { "\"$_\"" } @developed);
my $text = <<"CS";
// GENERATED by Tools/aloop_sigma_fit.pl. DO NOT EDIT BY HAND.
//
// Source : docs/reference/raw/aloop/parlgov/view_election.csv
// SHA-256: $digest{'view_election.csv'}
// Source : docs/reference/raw/aloop/parlgov/view_cabinet.csv
// SHA-256: $digest{'view_cabinet.csv'}
//
// FiscalVoteFitCheck re-reads both CSVs and refits every figure.

namespace PoliSim.Elections.Generated
{
    /// <summary>A-LOOP q13 (`docs/specs/A_LOOP_PLAN.md` section 4 (3), Elias's ruling J1 Q4): sigma_v, the spread of incumbent vote swings - FITTED on the
    /// ParlGov election table, the prime minister's party's vote-share change between consecutive parliamentary elections over Brender &amp; Drazen's
    /// developed countries (their Table A2) in ParlGov, 1960-2003. Generated, never hand-edited.</summary>
    public static class IncumbentVoteSwing
    {
        public const string ElectionDigest = "$digest{'view_election.csv'}";
        public const string CabinetDigest = "$digest{'view_cabinet.csv'}";
        public const string From = "$from", To = "$to";

        /// <summary>The frame's countries, ParlGov's short names.</summary>
        public static readonly string[] Countries = { $countries };

        /// <summary>FITTED (one free parameter, the spread; the mean beside it): the observations, the mean swing, and the standard deviation with its
        /// 95 % interval UNDER NORMALITY (the chi-square interval of a normal sd, DERIVED), in vote-share points - printed, not read since K64.</summary>
        public const int N = $n;
        public const double MeanPoints = ${\ sprintf('%.17g', $mean)};
        public const double SdPoints = ${\ sprintf('%.17g', $sd)};
        public const double SdLowerPoints = ${\ sprintf('%.17g', $lo)};
        public const double SdUpperPoints = ${\ sprintf('%.17g', $hi)};

        /// <summary>K64 (2026-10-23, "sigma_v takes a robust scale"): FITTED - the median swing, the median absolute deviation from it, and the
        /// MAD-based scale (1.482602218505602 x MAD, DERIVED factor), the one the vote reads; the IQR-based scale (type 7 quantiles, over 1.3489795003921634,
        /// DERIVED factor) and the kurtosis (population moments) printed beside it; vote-share points.</summary>
        public const double MedianPoints = ${\ sprintf('%.17g', $median)};
        public const double MadPoints = ${\ sprintf('%.17g', $mad)};
        public const double MadScalePoints = ${\ sprintf('%.17g', $madScale)};
        public const double IqrScalePoints = ${\ sprintf('%.17g', $iqrScale)};
        public const double Kurtosis = ${\ sprintf('%.17g', $kurtosis)};

        /// <summary>sigma_v as the vote reads it: the MAD-based scale (K64).</summary>
        public const double SigmaPoints = MadScalePoints;
    }
}
CS
$text =~ s/\r?\n/\r\n/g;
open my $o, '>:raw', $cs or die "$cs: $!\n";
print $o $text;
close $o;
print "wrote $cs\n";
