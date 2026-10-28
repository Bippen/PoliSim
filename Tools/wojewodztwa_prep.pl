#!/usr/bin/perl
# POLAND'S WOJEWODZTWA AS PRESIDENTIAL CAMPAIGN REGIONS (PL-C, ruling K169 of 2026-10-27: "the presidential campaign runs on the 16 wojewodztwa").
# Reads the per-wojewodztwo presidential returns already on disk, SOURCED from the KBW's own open-data files (their digests in the CSV's header and in
# ElectionsData/poland/raw/president/SHA256SUMS.txt):
#   ElectionsData/poland/presidential_votes_by_voivodeship.csv   - per round and unit: __VALID__ (the valid votes) and __ELIGIBLE__ (the electorate)
#   ElectionsData/poland/presidential_votes.csv                  - the national totals of the PKW's notices, the same four totals rows
#   ElectionsData/poland/raw/wojewodztwa_abroad_2020/kbw_2020_1_wyniki_gl_na_kand_po_powiatach_csv.zip - the 2020 first round per powiat,
#                                                              whose 'zagranica' and 'statki' rows (the voters abroad and on ships) the KBW
#                                                              counts inside mazowieckie; they are subtracted from it, so both vintages hold
#                                                              the voters of the 16 wojewodztwa alone (2025's files carry them as rows of their own)
# and writes
#   Assets/Scripts/Elections/Generated/PolishWojewodztwa.cs   the 16 names, and each first round's valid votes and electorate per wojewodztwo, as C#
#                                                              literals, both CSVs' SHA-256 in its header.
# It refuses to write unless, for each first round, the 16 wojewodztwa plus the units the voivodeship file carries beside them (2025's 'zagranica'
# and 'statki', the voters abroad and on ships) sum to the notice's national valid votes and electorate exactly, and the 16 names are the same in both.
# Usage: perl Tools/wojewodztwa_prep.pl [out]   (from the project root; out defaults to the Generated path). Writes CRLF.
use strict; use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw(unzip $UnzipError);

my $byUnit = 'ElectionsData/poland/presidential_votes_by_voivodeship.csv';
my $national = 'ElectionsData/poland/presidential_votes.csv';
my $out = shift // 'Assets/Scripts/Elections/Generated/PolishWojewodztwa.cs';
my %abroad = map { $_ => 1 } ('zagranica', 'statki');

sub slurp { my ($f) = @_; open my $h, '<:raw', $f or die "$f: $!\n"; local $/; my $b = <$h>; close $h; return $b; }

my $unitBytes = slurp($byUnit);
my $nationalBytes = slurp($national);
my $unitDigest = sha256_hex($unitBytes);
my $nationalDigest = sha256_hex($nationalBytes);

# national: election,round,date,candidate,(empty),votes for the totals rows
my %nat;
for my $line (split /\r?\n/, $nationalBytes) {
    next if $line =~ /^#/ || $line eq '';
    my @f = split /,/, $line, -1;
    next unless @f >= 6 && $f[3] =~ /^__(VALID|ELIGIBLE)__$/;
    $nat{"$f[0],$f[1],$1"} = $f[5];
}

# by unit: election,round,date,unit,candidate,votes
my (%valid, %eligible, %order);
my $seq = 0;
for my $line (split /\r?\n/, $unitBytes) {
    next if $line =~ /^#/ || $line eq '' || $line =~ /^election,/;
    my @f = split /,/, $line, -1;
    die "a row of six fields expected: $line\n" unless @f == 6;
    my ($year, $round, $unit, $what, $votes) = ($f[0], $f[1], $f[3], $f[4], $f[5]);
    next unless $round == 1;
    die "not a whole number: $votes\n" unless $votes =~ /^\d+$/;
    $order{"$year,$unit"} //= $seq++;
    $valid{$year}{$unit} = $votes if $what eq '__VALID__';
    $eligible{$year}{$unit} = $votes if $what eq '__ELIGIBLE__';
}

# the 2020 powiat file: its abroad and ship rows, which must be mazowieckie's, and mazowieckie's own sum, which must be the voivodeship file's
my $powiatZip = 'ElectionsData/poland/raw/wojewodztwa_abroad_2020/kbw_2020_1_wyniki_gl_na_kand_po_powiatach_csv.zip';
my $powiatBytes = slurp($powiatZip);
my $powiatDigest = sha256_hex($powiatBytes);
my $powiatText;
unzip(\$powiatBytes => \$powiatText, Name => 'wyniki_gl_na_kand_po_powiatach_utf8.csv') or die "$powiatZip: $UnzipError\n";
$powiatText =~ s/^\xEF\xBB\xBF//;
my @prows = map { [ map { my $v = $_; $v =~ s/^"//; $v =~ s/"$//; $v } split /;/, $_, -1 ] } grep { $_ ne '' } split /\r?\n/, $powiatText;
my $phead = shift @prows;
my ($iPowiat, $iWoj, $iElig, $iValid) = (-1, -1, -1, -1);
for my $i (0..$#$phead) {
    $iPowiat = $i if $phead->[$i] eq 'Powiat';
    $iWoj = $i if $phead->[$i] =~ /^Wojew\S+dztwo$/;
    $iElig = $i if $phead->[$i] =~ /^Liczba wyborc\S+ uprawnionych do g\S+osowania$/;
    $iValid = $i if $phead->[$i] =~ /^Liczba g\S+os\S+w wa\S+nych oddanych \S+cznie na wszystkich kandydat\S+w$/;
}
die "the powiat file's columns were not found\n" if grep { $_ < 0 } ($iPowiat, $iWoj, $iElig, $iValid);
my ($abroadE, $abroadV, $abroadRows, $mazE, $mazV) = (0, 0, 0, 0, 0);
for my $r (@prows) {
    next unless @$r > $iValid;
    if ($r->[$iWoj] eq 'mazowieckie') { $mazE += $r->[$iElig]; $mazV += $r->[$iValid]; }
    next unless $r->[$iPowiat] eq 'zagranica' || $r->[$iPowiat] eq 'statki';
    die "the 2020 '$r->[$iPowiat]' row is not mazowieckie's ($r->[$iWoj])\n" unless $r->[$iWoj] eq 'mazowieckie';
    $abroadE += $r->[$iElig]; $abroadV += $r->[$iValid]; $abroadRows++;
}
die "two 2020 abroad rows expected, $abroadRows read\n" unless $abroadRows == 2;
die "the powiat file's mazowieckie ($mazV valid, $mazE electorate) is not the voivodeship file's\n"
    unless $mazV == $valid{2020}{'mazowieckie'} && $mazE == $eligible{2020}{'mazowieckie'};
print "2020: the abroad and ship rows inside mazowieckie - valid $abroadV, electorate $abroadE - subtracted\n";

my %names;
for my $year (2020, 2025) {
    die "no first round of $year in $byUnit\n" unless $valid{$year};
    my @units = sort { $order{"$year,$a"} <=> $order{"$year,$b"} } grep { !$abroad{$_} } keys %{$valid{$year}};
    die "16 wojewodztwa expected in $year, " . scalar(@units) . " read\n" unless @units == 16;
    my ($sumV, $sumE) = (0, 0);
    for my $u (keys %{$valid{$year}}) {
        die "$year $u has no electorate\n" unless defined $eligible{$year}{$u};
        $sumV += $valid{$year}{$u}; $sumE += $eligible{$year}{$u};
    }
    my ($nv, $ne) = ($nat{"$year,1,VALID"}, $nat{"$year,1,ELIGIBLE"});
    die "no national totals for $year in $national\n" unless defined $nv && defined $ne;
    die "$year: the units' valid votes sum to $sumV, not the notice's $nv\n" unless $sumV == $nv;
    die "$year: the units' electorates sum to $sumE, not the notice's $ne\n" unless $sumE == $ne;
    $names{$year} = \@units;
    print "$year first round: 16 wojewodztwa + " . (scalar(keys %{$valid{$year}}) - 16) . " other unit(s) = valid $sumV, electorate $sumE (the notice's)\n";
}
die "the 16 names differ between 2020 and 2025\n" unless join('|', @{$names{2020}}) eq join('|', @{$names{2025}});
my @w = @{$names{2025}};

my $n = join(', ', map { "\"$_\"" } @w);
my $v20 = join(', ', map { $valid{2020}{$_} - ($_ eq 'mazowieckie' ? $abroadV : 0) } @w);
my $e20 = join(', ', map { $eligible{2020}{$_} - ($_ eq 'mazowieckie' ? $abroadE : 0) } @w);
my $v25 = join(', ', map { $valid{2025}{$_} } @w);
my $e25 = join(', ', map { $eligible{2025}{$_} } @w);
my $text = <<"EOT";
// GENERATED by Tools/wojewodztwa_prep.pl. DO NOT EDIT BY HAND.
//
// Source : ElectionsData/poland/presidential_votes_by_voivodeship.csv
// SHA-256: $unitDigest
// Source : ElectionsData/poland/presidential_votes.csv
// SHA-256: $nationalDigest
// Source : ElectionsData/poland/raw/wojewodztwa_abroad_2020/kbw_2020_1_wyniki_gl_na_kand_po_powiatach_csv.zip
// SHA-256: $powiatDigest
//
// The generator refuses to write unless each first round's units sum to the PKW notice's national valid votes and electorate, and the powiat file's mazowieckie is the voivodeship file's.

namespace PoliSim.Elections.Generated
{
    /// <summary>PL-C (K169): Poland's 16 wojewodztwa as the presidential campaign's regions - each one's name as the KBW prints it, and each presidential
    /// first round's valid votes and electorate there - SOURCED (the KBW's per-wojewodztwo files, `ElectionsData/poland/presidential_votes_by_voivodeship.csv`).
    /// Both vintages hold the voters of the 16 wojewodztwa alone: the voters abroad and on ships are no region - 2025's files carry them as units of their
    /// own, left out here, and 2020's voivodeship file counts them inside mazowieckie, so its 2020 figures are the KBW's less the powiat file's 'zagranica'
    /// and 'statki' rows ($abroadV valid votes, $abroadE electors). Generated, never hand-edited.</summary>
    public static class PolishWojewodztwa
    {
        public const string UnitsDigest = "$unitDigest";
        public const string NationalDigest = "$nationalDigest";
        public const string PowiatDigest2020 = "$powiatDigest";

        /// <summary>The 2020 first round's voters abroad and on ships, subtracted from mazowieckie (the powiat file's two rows): valid votes, electorate.</summary>
        public const long AbroadValid2020 = $abroadV, AbroadEligible2020 = $abroadE;

        /// <summary>The 16 wojewodztwa, in the KBW files' order.</summary>
        public static readonly string[] Names = { $n };

        /// <summary>The first round of 28 Jun 2020: each wojewodztwo's valid votes (mazowieckie's without the voters abroad and on ships).</summary>
        public static readonly long[] Valid2020 = { $v20 };

        /// <summary>The first round of 28 Jun 2020: each wojewodztwo's electorate (mazowieckie's without the voters abroad and on ships).</summary>
        public static readonly long[] Eligible2020 = { $e20 };

        /// <summary>The first round of 18 May 2025: each wojewodztwo's valid votes.</summary>
        public static readonly long[] Valid2025 = { $v25 };

        /// <summary>The first round of 18 May 2025: each wojewodztwo's electorate.</summary>
        public static readonly long[] Eligible2025 = { $e25 };
    }
}
EOT
$text =~ s/\r?\n/\r\n/g;
open my $o, '>:raw', $out or die "$out: $!\n";
print $o $text;
close $o;
print "wrote $out\n";
