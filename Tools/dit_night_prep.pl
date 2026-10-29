#!/usr/bin/perl
# ITALY'S ELECTION NIGHT, THE SEAT CELLS (D-IT; Design's boards 34a-34d). Reads Design's night table as the channel returned it, decoded byte-exact and
# kept out of tree with its digest (../PoliSim-captures/design/RETURN_2026-10-29_DIT/night.json): per chamber one cell per seat in Italy [x, y, block,
# index in block] at the board's pitch, the blocks (a circoscrizione of the Camera, a region of the Senato) with their colleges in U order, the block
# labels, the paper borders (segs) and the board's seeded declaration order (each college's rank) - and writes
#   Assets/Scripts/Elections/Generated/ItalianNightCartogram.cs   the positions as the board's, never re-derived, the source's SHA-256 in its header.
# Design's own figures in the file (its 2022 votes, winners, list seats, inks, the call points, the life senators' names) are NOT carried: the night counts
# the game's (ItalianNight), and the senators for life are the record's (ItalianCount2022.SenatorsForLife).
# Every block is matched to the game's catalog by its colleges' names (ItalianCount2022's CameraColleges / SenatoColleges), never by its label.
# Dies unless: every college of the catalog sits in exactly one block, in the catalog's own order and in U order inside its block; each block's colleges
# are one circoscrizione's (region's) and all of them; each block's cells are its magnitude (the catalog's Tabella A seats), indices 0..seats-1 once each;
# the Camera's cells number 392 and the Senato's 196, none on another's position; each chamber's declaration ranks are 0..colleges-1 once each.
# ItalianNightDiagnostic, in the cheap bar, holds the generated table to the game's count.
# Usage: perl Tools/dit_night_prep.pl [night.json] [catalog .cs] [out]   (from the project root)
use strict; use warnings;
use Digest::SHA qw(sha256_hex);
use JSON::PP;

my $src = shift // '../PoliSim-captures/design/RETURN_2026-10-29_DIT/night.json';
my $catalog = shift // 'Assets/Scripts/Elections/Generated/ItalianCount2022.cs';
my $out = shift // 'Assets/Scripts/Elections/Generated/ItalianNightCartogram.cs';

open my $h, '<:raw', $src or die "$src: $!\n"; my $bytes = do { local $/; <$h> }; close $h;
my $digest = sha256_hex($bytes);
my $j = JSON::PP->new->utf8->decode($bytes);

open my $c, '<:raw', $catalog or die "$catalog: $!\n"; my $cat = do { local $/; <$c> }; close $c;
sub strings { my $name = shift; $cat =~ /public static readonly string\[\] $name =\s*\{(.*?)\};/s or die "$name not in the catalog\n"; my $b = $1; return [ $b =~ /"([^"]*)"/g ]; }
sub ints { my $name = shift; $cat =~ /public static readonly int\[\] $name = \{([^}]*)\};/ or die "$name not in the catalog\n"; return [ map { $_ + 0 } split /\s*,\s*/, $1 =~ s/^\s+|\s+$//gr ]; }

my %chamberOut;
for my $ch (qw(Camera Senato)) {
    my $X = $j->{$ch} or die "$ch missing\n";
    my $colleges = strings("${ch}Colleges");
    my $collegeCirc = ints("${ch}CollegeCircoscrizione");
    my $circSeats = ints("${ch}CircoscrizioneSeats");
    die "$ch: the catalog's colleges and their circoscrizioni disagree in length\n" unless @$colleges == @$collegeCirc;
    my %catalogIndex; $catalogIndex{$colleges->[$_]} = $_ for 0 .. $#$colleges;
    my @jc = @{$X->{colleges}};
    die "$ch: " . scalar(@jc) . " colleges in the table, " . scalar(@$colleges) . " in the catalog\n" unless @jc == @$colleges;
    # each table college to the catalog's, by name; the table's order is the catalog's
    my @toCatalog;
    for my $i (0 .. $#jc) {
        my $n = $jc[$i]{n};
        die "$ch: college '$n' is not in the catalog\n" unless exists $catalogIndex{$n};
        die "$ch: college '$n' sits at $i in the table and $catalogIndex{$n} in the catalog\n" unless $catalogIndex{$n} == $i;
        push @toCatalog, $catalogIndex{$n};
    }
    # the declaration ranks
    my %rank; my @rank;
    for my $i (0 .. $#jc) { my $r = $jc[$i]{r}; die "$ch: rank $r twice\n" if $rank{$r}++; die "$ch: rank $r out of range\n" if $r < 0 || $r > $#jc; $rank[$i] = $r; }
    # the blocks
    my @blocks = @{$X->{blocks}};
    die "$ch: " . scalar(@blocks) . " blocks, " . scalar(@$circSeats) . " circoscrizioni in the catalog\n" unless @blocks == @$circSeats;
    my (%usedCollege, @blockCirc, @blockCols, @blockSeats, @blockCode);
    for my $b (0 .. $#blocks) {
        my $B = $blocks[$b];
        my @cols = @{$B->{cols}};
        my $circ;
        my $lastU = 0;
        for my $ci (@cols) {
            die "$ch block $B->{code}: college $ci in two blocks\n" if $usedCollege{$ci}++;
            my $cc = $collegeCirc->[$toCatalog[$ci]];
            $circ //= $cc;
            die "$ch block $B->{code}: its colleges are in two circoscrizioni\n" unless $cc == $circ;
            my $u = $jc[$ci]{u}; die "$ch block $B->{code}: colleges not in U order\n" unless $u > $lastU; $lastU = $u;
        }
        die "$ch block $B->{code}: no college\n" unless defined $circ;
        my $inCirc = grep { $_ == $circ } @$collegeCirc;
        die "$ch block $B->{code}: " . scalar(@cols) . " colleges of the circoscrizione's $inCirc\n" unless @cols == $inCirc;
        die "$ch block $B->{code}: block $b is circoscrizione $circ - the table's block order must be the catalog's\n" unless $circ == $b;
        die "$ch block $B->{code}: seats $B->{seats} against the catalog's $circSeats->[$circ]\n" unless $B->{seats} == $circSeats->[$circ];
        push @blockCirc, $circ; push @blockCols, [ map { $toCatalog[$_] } @cols ]; push @blockSeats, $B->{seats};
        push @blockCode, $B->{code};
    }
    die "$ch: a college sits in no block\n" unless keys(%usedCollege) == @jc;
    # the cells
    my (@cells, %pos, %seen);
    my $italy = 0; $italy += $_ for @$circSeats;
    for my $cell (@{$X->{cells}}) {
        my ($x, $y, $b, $i) = @$cell;
        die "$ch: cell at $x,$y twice\n" if $pos{"$x,$y"}++;
        die "$ch: cell in block $b out of range\n" if $b < 0 || $b > $#blocks;
        die "$ch: block $blockCode[$b] index $i out of range\n" if $i < 0 || $i >= $blockSeats[$b];
        die "$ch: block $blockCode[$b] index $i twice\n" if $seen{"$b,$i"}++;
        push @cells, [$x, $y, $b, $i];
    }
    die "$ch: " . scalar(@cells) . " cells, the catalog's Italy seats $italy\n" unless @cells == $italy;
    my $want = $ch eq 'Camera' ? 392 : 196;
    die "$ch: $italy seats in Italy, $want expected\n" unless $italy == $want;
    my @labels = map { [ $_->{x}, $_->{y}, $_->{g}, $_->{t}, $_->{inside} ? 1 : 0 ] } @{$X->{labels}};
    die "$ch: " . scalar(@labels) . " labels for " . scalar(@blocks) . " blocks\n" unless @labels == @blocks;
    my @segs = map { [ @$_[0 .. 3] ] } @{$X->{segs}};
    $chamberOut{$ch} = { pitch => $X->{p}, mapH => $X->{mapH}, cells => \@cells, codes => \@blockCode, cols => \@blockCols,
        labels => \@labels, segs => \@segs, rank => \@rank };
}

sub str { my $s = shift; $s =~ s/\\/\\\\/g; $s =~ s/"/\\"/g; return "\"$s\""; }
sub flat { return join(', ', @{$_[0]}); }
sub block {
    my ($ch, $o) = @_;
    my $cells = join(",\n", map { "                { $_->[0], $_->[1], $_->[2], $_->[3] }" } @{$o->{cells}});
    my $cols = join(",\n", map { "                new[] { " . flat($_) . " }" } @{$o->{cols}});
    my $labels = join(",\n", map { "                new Label($_->[0], $_->[1], $_->[2], " . str($_->[3]) . ", " . ($_->[4] ? 'true' : 'false') . ")" } @{$o->{labels}});
    my $segs = join(",\n", map { "                { $_->[0], $_->[1], $_->[2], $_->[3] }" } @{$o->{segs}});
    my $codes = join(', ', map { str($_) } @{$o->{codes}});
    return <<"CS";
        /// <summary>The $ch: its pitch and map height in board px, its cells, blocks, labels, borders and the board's declaration order.</summary>
        public static readonly Chamber $ch = new Chamber($o->{pitch}, $o->{mapH},
            new[,]
            {
$cells
            },
            new[] { $codes },
            new[]
            {
$cols
            },
            new[]
            {
$labels
            },
            new[,]
            {
$segs
            },
            new[] { ${\ flat($o->{rank})} });
CS
}

my $camera = block('Camera', $chamberOut{Camera});
my $senato = block('Senato', $chamberOut{Senato});
my $text = <<"CS";
// GENERATED by Tools/dit_night_prep.pl. DO NOT EDIT BY HAND.
//
// Source : ../PoliSim-captures/design/RETURN_2026-10-29_DIT/night.json (Design's board 34a night table, out of tree)
// SHA-256: $digest
//
// ItalianNightDiagnostic (the cheap bar) holds this table to the game's count (the magnitudes, the colleges, the order inside each block) and its own invariants.

namespace PoliSim.Elections.Generated
{
    /// <summary>D-IT (Design's boards 34a-34d): ITALY'S CHAMBERS AS SEAT CELLS - one cell per seat in Italy at the board's pitch, each circoscrizione
    /// (Camera) or region (Senato) a contiguous block of its own magnitude, its colleges first in U order then its list seats, the blocks' labels and the
    /// paper borders between them - positions as the board's, never re-derived - and the board's seeded declaration order [AUTHORED]. Blocks are in the
    /// catalog's circoscrizione order and colleges are the catalog's indices (`ItalianCount2022`). The night reads its counts from the model, never from here.</summary>
    public static class ItalianNightCartogram
    {
        public const string SourceDigest = "$digest";

        /// <summary>A block's label: where it is printed (board px, the map's own origin), the block it names, its text, and whether it sits inside the block.</summary>
        public sealed class Label
        {
            public readonly int X, Y, Block;
            public readonly string Text;
            public readonly bool Inside;

            public Label(int x, int y, int block, string text, bool inside) { X = x; Y = y; Block = block; Text = text; Inside = inside; }
        }

        /// <summary>One chamber's table. <see cref="Cells"/> rows are [x, y, block, index in block] - an index below the block's college count is that college
        /// (<see cref="BlockColleges"/>, U order), the rest the block's list seats in the night's fixed order. <see cref="Segs"/> rows are [x, y, width, height],
        /// the paper borders. <see cref="DeclareRank"/> is each catalog college's place in the board's declaration order, 0 first.</summary>
        public sealed class Chamber
        {
            public readonly int Pitch, MapHeight;
            public readonly int[,] Cells, Segs;
            public readonly string[] BlockCode;
            public readonly int[][] BlockColleges;
            public readonly Label[] Labels;
            public readonly int[] DeclareRank;

            public Chamber(int pitch, int mapHeight, int[,] cells, string[] blockCode, int[][] blockColleges, Label[] labels, int[,] segs, int[] declareRank)
            {
                Pitch = pitch; MapHeight = mapHeight; Cells = cells; BlockCode = blockCode; BlockColleges = blockColleges; Labels = labels; Segs = segs; DeclareRank = declareRank;
            }
        }

$camera
$senato    }
}
CS
$text =~ s/\r?\n/\r\n/g;
open my $o, '>:raw:encoding(UTF-8)', $out or die "$out: $!\n"; print $o $text; close $o;   # characters out as UTF-8 (a raw handle writes a Latin-1 byte for one below 256)
printf "%s written - Camera %d cells in %d blocks, Senato %d cells in %d blocks; source %s\n", $out, scalar(@{$chamberOut{Camera}{cells}}), scalar(@{$chamberOut{Camera}{codes}}),
    scalar(@{$chamberOut{Senato}{cells}}), scalar(@{$chamberOut{Senato}{codes}}), substr($digest, 0, 12);
