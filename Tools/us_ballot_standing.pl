#!/usr/bin/perl
# us_ballot_standing.pl - PS-6 US-6: WHERE THE FEC'S 2024 WORKBOOK SHOWS EACH MINOR CANDIDATE WITH VOTES, by jurisdiction. Prints only; writes nothing.
# The plan's US-6 says "the minor candidates standing only where the FEC shows them on the ballot"; US-6 stands every minor column nationally
# instead, a DEPARTURE put to Elias (the record's US-6 section), and this is the measurement it is put with: per candidate column, the jurisdictions
# whose cell holds a vote and the ones whose cell is empty or zero. Run from the project root:  perl Tools/us_ballot_standing.pl [SURNAME ...]
# (default: STEIN KENNEDY OLIVER). Reads the saved workbook only after holding it to its line in raw/returns/SHA256SUMS.txt.
# Read as the workbook prints it: a vote in a jurisdiction's cell is not a ballot listing - a write-in count lands there too, and the FEC's ballot
# listings per state are not saved as their own page (the plan's sources table) - so a jurisdiction with votes is an upper bound on where a
# candidate was listed, and an empty cell the lower bound on where he or she was not.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw($UnzipError);

die "run from the project root\n" unless -d 'ElectionsData/usa/raw/returns';
my @want = @ARGV ? @ARGV : qw(STEIN KENNEDY OLIVER);
my $dir = 'ElectionsData/usa/raw/returns';
my $name = 'fec_2024presgeresults.xlsx';
my $sheet = 'OFFICIAL 2024 PRES GE RESULTS';

open my $s, '<', "$dir/SHA256SUMS.txt" or die "$dir/SHA256SUMS.txt: $!\n";
my $want_sha;
while (<$s>) { s/\r?\n$//; $want_sha = $1 if /^([0-9a-f]{64}) \*\Q$name\E$/; }
close $s;
die "$name: in no line of SHA256SUMS.txt - the tool reads saved pages only\n" unless $want_sha;
open my $h, '<:raw', "$dir/$name" or die "$dir/$name: $!\n";
my $bytes = do { local $/; <$h> };
close $h;
my $got = sha256_hex($bytes);
die "$name: SHA-256 $got, SHA256SUMS.txt holds $want_sha\n" unless $got eq $want_sha;

sub ent { my $t = shift; $t =~ s/&lt;/</g; $t =~ s/&gt;/>/g; $t =~ s/&quot;/"/g; $t =~ s/&apos;/'/g; $t =~ s/&#(\d+);/chr($1)/ge; $t =~ s/&amp;/&/g; return $t; }
sub colnum { my $c = shift; my $n = 0; $n = $n * 26 + (ord($_) - 64) for split //, $c; return $n; }
my %part;
my $u = IO::Uncompress::Unzip->new(\$bytes) or die "$name: $UnzipError\n";
for (my $st = 1; $st > 0; $st = $u->nextStream()) { my $n = $u->getHeaderInfo()->{Name}; my ($b, $c) = (''); while ($u->read($c) > 0) { $b .= $c; } $part{$n} = $b; }
my @ss;
if (my $x = $part{'xl/sharedStrings.xml'}) { while ($x =~ m{<si>(.*?)</si>}gs) { my $si = $1; my $t = ''; $t .= $1 while $si =~ m{<t[^>]*>(.*?)</t>}gs; push @ss, ent($t); } }
my $wb = $part{'xl/workbook.xml'} // die "$name: no workbook part\n";
my $rels = $part{'xl/_rels/workbook.xml.rels'} // die "$name: no workbook relationships\n";
my %tg;
while ($rels =~ m{<Relationship\b([^>]*)/?>}g) { my $a = $1; my ($id) = $a =~ /Id="([^"]+)"/; my ($t) = $a =~ /Target="([^"]+)"/; $tg{$id} = $t; }
my $path;
while ($wb =~ m{<sheet\b([^>]*)/?>}g) { my $a = $1; my ($nm) = $a =~ /name="([^"]+)"/; my ($id) = $a =~ /r:id="([^"]+)"/; $path = $tg{$id} if ent($nm) eq $sheet; }
die "$name: no sheet '$sheet'\n" unless $path;
$path =~ s{^/?xl/}{};
my $xml = $part{"xl/$path"} // die "$name: no part $path\n";
my %cell;
while ($xml =~ m{<c r="([A-Z]+)(\d+)"([^>]*?)(?:/>|>(.*?)</c>)}gs) {
    my ($col, $row, $attr, $body) = ($1, $2, $3, $4 // '');
    my $v = $body =~ m{<v>(.*?)</v>}s ? $1 : undef;
    next unless defined $v;
    $cell{$row}{colnum($col)} = $attr =~ /t="s"/ ? $ss[$v] : ent($v);
}
my %col;
for my $k (keys %{ $cell{1} }) { my $l = $cell{1}{$k}; $l =~ s/^\s+|\s+$//g; $col{$l} = $k; }
my @rows = grep { my $a = $cell{$_}{1} // ''; $a =~ s/\s//g; $a =~ /^[A-Z]{2}$/ } sort { $a <=> $b } keys %cell;
die "$name: " . scalar(@rows) . " jurisdiction rows, not 51\n" unless @rows == 51;
print "the FEC's Official 2024 Presidential General Election Results ($name, SHA-256 held): a jurisdiction row a state and the District of Columbia\n";
for my $w (@want) {
    my $k = $col{$w} // die "$name: no column '$w'\n";
    my (@with, @without);
    for my $r (@rows) {
        my $st = $cell{$r}{1}; $st =~ s/\s//g;
        my $v = $cell{$r}{$k} // ''; $v =~ s/[*,\s]//g;
        die "$name $st col $w: '$v' is not a count\n" unless $v eq '' || $v =~ /^\d+(?:\.0+)?$/;
        if ($v ne '' && $v > 0) { push @with, $st; } else { push @without, $st; }
    }
    printf "%-10s votes in %2d of 51; none in %2d: %s\n", $w, scalar(@with), scalar(@without), join(' ', sort @without);
}
