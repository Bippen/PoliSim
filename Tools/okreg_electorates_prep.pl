#!/usr/bin/perl
# POLAND'S OKREGI AS CAMPAIGN REGIONS (Regional 5a, ruling K146 of 2026-10-27: "a region is ... Poland's okreg (each okreg's electorate fetched
# from the PKW)"). Reads the two zips the Krajowe Biuro Wyborcze's election-data service publishes for the 2023 Sejm, byte-exact as fetched into
# ElectionsData/poland/raw/okreg_electorates_2023/ (their digests in that folder's SHA256SUMS.txt):
#   wyniki_gl_na_listy_po_okregach_sejm_csv.zip  - per okreg: "Liczba wyborcow uprawnionych do glosowania" (the electorate on polling day) and
#                                                  "Liczba glosow waznych oddanych lacznie na wszystkie listy kandydatow" (the valid list votes)
#   okregi_sejm_csv.zip                          - per okreg: "Siedziba OKW" (the seat of the district electoral commission, the okreg's name)
# and writes
#   Assets/Scripts/Elections/Generated/PolishOkregElectorates2023.cs   names, electorates and valid votes as C# literals, both zips' SHA-256 in its header.
# It refuses to write unless the electorates sum to the notice's national figure and the valid votes to its valid total (Dz.U. 2023 poz. 2234,
# Dzial I rozdz. 2, as ElectionsData/poland/returns_2023.md quotes it: 29,532,595 eligible; 21,596,674 valid).
# Usage: perl Tools/okreg_electorates_prep.pl [out]   (from the project root; out defaults to the Generated path). Writes CRLF.
use strict; use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw(unzip $UnzipError);

my $dir = 'ElectionsData/poland/raw/okreg_electorates_2023';
my $out = shift // 'Assets/Scripts/Elections/Generated/PolishOkregElectorates2023.cs';
my $NationalEligible = 29532595;   # Dz.U. 2023 poz. 2234, Dzial I rozdz. 2 pkt 8 (returns_2023.md's "Turnout" line)
my $NationalValid = 21596674;      # the same notice's valid votes (returns_2023.md's "Votes (valid)" line)

sub slurp_zip {
    my ($zip, $member) = @_;
    open my $h, '<:raw', $zip or die "$zip: $!\n"; local $/; my $bytes = <$h>; close $h;
    my $text;
    unzip(\$bytes => \$text, Name => $member) or die "$zip/$member: $UnzipError\n";
    return (sha256_hex($bytes), $text);
}

sub rows {
    my ($text) = @_;
    $text =~ s/^\xEF\xBB\xBF//;   # the files' UTF-8 byte-order mark
    my @rows;
    for my $line (split /\r?\n/, $text) {
        next if $line eq '';
        my @f = map { my $v = $_; $v =~ s/^"//; $v =~ s/"$//; $v } split /;/, $line;
        push @rows, \@f;
    }
    return @rows;
}

my ($resultsDigest, $results) = slurp_zip("$dir/wyniki_gl_na_listy_po_okregach_sejm_csv.zip", 'wyniki_gl_na_listy_po_okregach_sejm_utf8.csv');
my ($districtsDigest, $districts) = slurp_zip("$dir/okregi_sejm_csv.zip", 'okregi_sejm_utf8.csv');

my @r = rows($results);
my $head = shift @r;
my ($iEligible, $iValid) = (-1, -1);
for my $i (0..$#$head) {
    $iEligible = $i if $head->[$i] =~ /^Liczba wyborc\S+ uprawnionych do g\S+osowania$/;
    $iValid = $i if $head->[$i] =~ /^Liczba g\S+os\S+w wa\S+nych oddanych \S+cznie na wszystkie listy kandydat\S+w$/;
}
die "the results file's electorate column was not found\n" if $iEligible < 0;
die "the results file's valid-votes column was not found\n" if $iValid < 0;
die "41 okregi expected in the results, " . scalar(@r) . " read\n" unless @r == 41;
my (@eligible, @valid);
my ($sumE, $sumV) = (0, 0);
for my $k (0..40) {
    die "okreg " . ($k + 1) . " out of order in the results\n" unless $r[$k][0] == $k + 1;
    for my $v ($r[$k][$iEligible], $r[$k][$iValid]) { die "not a whole number: $v\n" unless $v =~ /^\d+$/; }
    push @eligible, $r[$k][$iEligible]; push @valid, $r[$k][$iValid];
    $sumE += $r[$k][$iEligible]; $sumV += $r[$k][$iValid];
}
die "the electorates sum to $sumE, not the notice's $NationalEligible\n" unless $sumE == $NationalEligible;
die "the valid votes sum to $sumV, not the notice's $NationalValid\n" unless $sumV == $NationalValid;

my @d = rows($districts);
my $dHead = shift @d;
my $iSeat = -1;
for my $i (0..$#$dHead) { $iSeat = $i if $dHead->[$i] eq 'Siedziba OKW'; }
die "the districts file's 'Siedziba OKW' column was not found\n" if $iSeat < 0;
die "41 okregi expected in the districts file, " . scalar(@d) . " read\n" unless @d == 41;
my @names;
for my $k (0..40) {
    die "okreg " . ($k + 1) . " out of order in the districts file\n" unless $d[$k][0] == $k + 1;
    push @names, $d[$k][$iSeat];
}
# Review L8 (2026-10-28): "Siedziba OKW" is the seat of the district electoral COMMISSION, not the district's name - the seat repeats for districts that
# are not the seat city's (Krakow 12 and 13; Warszawa 19 and 20; Katowice 29, 31 and 32, where 29 is the Gliwice/Bytom/Zabrze district and 32 the
# Sosnowiec one; Bielsko-Biala 27 and 30, where 30 is the Rybnik district). The PKW's files name a district by its number and its commission's seat
# only (its "Opis granic" lists its powiaty), so the key is the number alone and the seat is a second field; a common name from outside the files is
# not used. Okreg 19's electorate includes the voters abroad and on ships ("Warszawa; zagranica; statki" in the district file).
my @keys = map { 'okreg ' . ($_ + 1) } 0..40;
my $s = join(', ', map { "\"$_\"" } @names);

my $n = join(', ', map { "\"$_\"" } @keys);
my $e = join(', ', @eligible);
my $v = join(', ', @valid);
my $text = <<"EOT";
// GENERATED by Tools/okreg_electorates_prep.pl. DO NOT EDIT BY HAND.
//
// Source : ElectionsData/poland/raw/okreg_electorates_2023/wyniki_gl_na_listy_po_okregach_sejm_csv.zip
// SHA-256: $resultsDigest
// Source : ElectionsData/poland/raw/okreg_electorates_2023/okregi_sejm_csv.zip
// SHA-256: $districtsDigest
//
// The generator refuses to write unless the electorates and the valid votes sum to the PKW notice's national figures (Dz.U. 2023 poz. 2234).

namespace PoliSim.Elections.Generated
{
    /// <summary>Regional 5a (K146): the 2023 Sejm's 41 okregi as regions - each one's key (its number), the seat of its district electoral commission,
    /// its electorate on polling day and its valid list votes - SOURCED (the PKW's own per-district files, Krajowe Biuro Wyborcze). The commission's seat
    /// is not the district's name: it repeats (Katowice for okregi 29, 31 and 32), and the files carry no other name. Okreg 19's electorate includes the
    /// voters abroad and on ships. Generated, never hand-edited.</summary>
    public static class PolishOkregElectorates2023
    {
        public const string ResultsDigest = "$resultsDigest";
        public const string DistrictsDigest = "$districtsDigest";

        /// <summary>Each okreg's key, okreg 1 to 41: "okreg N", the number the PKW gives it.</summary>
        public static readonly string[] Names = { $n };

        /// <summary>Each okreg's district electoral commission's seat ("Siedziba OKW"), okreg 1 to 41 - a second field, not a name: it repeats.</summary>
        public static readonly string[] CommissionSeats = { $s };

        /// <summary>Each okreg's electorate on polling day ("Liczba wyborcow uprawnionych do glosowania"), okreg 1 to 41.</summary>
        public static readonly long[] Eligible = { $e };

        /// <summary>Each okreg's valid list votes ("Liczba glosow waznych oddanych lacznie na wszystkie listy kandydatow"), okreg 1 to 41.</summary>
        public static readonly long[] Valid = { $v };
    }
}
EOT
$text =~ s/\r?\n/\r\n/g;
open my $o, '>:raw', $out or die "$out: $!\n";
print $o $text;
close $o;
print "wrote $out: 41 okregi, electorate $sumE, valid $sumV\n";
