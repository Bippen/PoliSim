#!/usr/bin/perl
# THE USA'S ELECTORAL-COLLEGE TILE CARTOGRAM FOR THE NIGHT (US-34; Design's boards 29a-29b, drawn by Code through the Design channel as a PROPOSAL).
# Reads the board's cartogram table as the channel returned it, decoded and kept out of tree with its digest
# (../PoliSim-captures/design/dus/tiles.json, SHA256SUMS.txt beside it): a 32 x 20 grid, one cell per elector [col,row,code] - the code a postal
# code, or ME-1, ME-2, NE-1, NE-2, NE-3 for the five district electors - and a label cell per state; and the board file itself
# (../PoliSim-captures/design/dus/Election Night US v3.5.dc.html) for the 51 names the slips print (its NAMES table: 29b's For Code 8 - the game
# holds postal codes only). Writes
#   Assets/Scripts/Elections/Generated/UsTileCartogram.cs   the positions as the board's, never re-derived; both sources' SHA-256 in its header.
# The board's own figures (its illustrative shares, its Senate not up, its declaration order and clock) are NOT carried: the night counts the game's.
# Dies unless the cells are 538, none twice, each inside the grid; the jurisdictions 51, each one contiguous block, its label one of its own cells;
# Maine's districts 1-2 and Nebraska's 1-3 present once each with two at-large cells beside them; every state named.
# The state borders (between two jurisdictions' cells) and each block's outline (for the dashed edge of a state not yet declared) are derived
# here from the cells - geometry of the board's own positions, not positions.
# UsNightDiagnostic holds the generated table to the catalog's electors and House seats on every run.
# Usage: perl Tools/dus_cartogram_prep.pl [tiles.json] [board.html] [out]   (from the project root)
use strict; use warnings;
use Digest::SHA qw(sha256_hex);
use JSON::PP;

my $src = shift // '../PoliSim-captures/design/dus/tiles.json';
my $board = shift // '../PoliSim-captures/design/dus/Election Night US v3.5.dc.html';
my $out = shift // 'Assets/Scripts/Elections/Generated/UsTileCartogram.cs';

open my $h, '<:raw', $src or die "$src: $!\n"; my $bytes = do { local $/; <$h> }; close $h;
my $digest = sha256_hex($bytes);
my $j = JSON::PP->new->utf8->decode($bytes);
my ($cols, $rows) = @{$j->{grid}};
die "the grid must be 32 x 20, read $cols x $rows\n" unless $cols == 32 && $rows == 20;
die "the pitch must be 18 and the cell 17, read $j->{pitch} / $j->{cell}\n" unless $j->{pitch} == 18 && $j->{cell} == 17;

open my $bf, '<:raw', $board or die "$board: $!\n"; my $boardBytes = do { local $/; <$bf> }; close $bf;
my $boardDigest = sha256_hex($boardBytes);
my ($namesText) = $boardBytes =~ /NAMES = \{([^}]*)\}/ or die "the board holds no NAMES table\n";
my %name;
while ($namesText =~ /([A-Z]{2}): '([^']*)'/g) { $name{$1} = $2; }

my (%seen, %cellsOf, %owner);
my $total = 0;
for my $c (@{$j->{cells}}) {
    my ($x, $y, $code) = @$c;
    die "cell $x,$y outside the grid\n" if $x < 0 || $x >= $cols || $y < 0 || $y >= $rows;
    die "cell $x,$y taken twice\n" if $seen{"$x,$y"}++;
    my ($state, $district) = $code =~ /^([A-Z]{2})(?:-(\d))?$/ or die "cell $x,$y carries the code '$code'\n";
    push @{$cellsOf{$state}}, [$x, $y, $district // 0];
    $owner{"$x,$y"} = $state;
    $total++;
}
die "538 cells expected, $total read\n" unless $total == 538;
my @states = sort keys %cellsOf;
die "51 jurisdictions expected, " . scalar(@states) . " read\n" unless @states == 51;

for my $s (@states) {
    die "$s is not named on the board\n" unless defined $name{$s};
    my $label = $j->{labels}{$s} or die "$s has no label cell\n";
    die "$s: its label is not one of its cells\n" unless ($owner{"$label->[0],$label->[1]"} // '') eq $s;
    my @cells = sort { $a->[1] <=> $b->[1] || $a->[0] <=> $b->[0] } @{$cellsOf{$s}};   # reading order: row, then column
    $cellsOf{$s} = \@cells;
    my %in = map { ("$_->[0],$_->[1]" => 1) } @cells;
    my @queue = ("$cells[0][0],$cells[0][1]"); my %reached = ($queue[0] => 1);
    while (@queue) {
        my ($x, $y) = split /,/, shift @queue;
        for my $d ([1, 0], [-1, 0], [0, 1], [0, -1]) {
            my $k = ($x + $d->[0]) . ',' . ($y + $d->[1]);
            if ($in{$k} && !$reached{$k}) { $reached{$k} = 1; push @queue, $k; }
        }
    }
    die "$s is not contiguous\n" unless keys(%reached) == @cells;
    my @districts = sort { $a <=> $b } grep { $_ > 0 } map { $_->[2] } @cells;
    my $atLarge = grep { $_->[2] == 0 } @cells;
    if ($s eq 'ME') { die "Maine must carry districts 1-2 and two at-large cells\n" unless "@districts" eq '1 2' && $atLarge == 2; }
    elsif ($s eq 'NE') { die "Nebraska must carry districts 1-3 and two at-large cells\n" unless "@districts" eq '1 2 3' && $atLarge == 2; }
    else { die "$s carries district cells\n" if @districts; }
}

# the borders: a segment on the grid's lines wherever two jurisdictions' cells meet; each block's outline, wherever its cell meets another or nothing
my @borders;
my %outline;
for my $s (@states) {
    for my $c (@{$cellsOf{$s}}) {
        my ($x, $y) = @$c;
        my $right = $owner{($x + 1) . ",$y"}; my $below = $owner{"$x," . ($y + 1)};
        push @borders, [$x + 1, $y, $x + 1, $y + 1] if defined $right && $right ne $s;
        push @borders, [$x, $y + 1, $x + 1, $y + 1] if defined $below && $below ne $s;
        push @{$outline{$s}}, [$x + 1, $y, $x + 1, $y + 1] unless ($right // '') eq $s;
        push @{$outline{$s}}, [$x, $y + 1, $x + 1, $y + 1] unless ($below // '') eq $s;
        push @{$outline{$s}}, [$x, $y, $x, $y + 1] unless ($owner{($x - 1) . ",$y"} // '') eq $s;
        push @{$outline{$s}}, [$x, $y, $x + 1, $y] unless ($owner{"$x," . ($y - 1)} // '') eq $s;
    }
}

sub str { my $s = shift; $s =~ s/\\/\\\\/g; $s =~ s/"/\\"/g; return "\"$s\""; }
sub triples { return join(', ', map { "{ $_->[0], $_->[1], $_->[2] }" } @{$_[0]}); }
sub segs { return join(', ', map { "{ $_->[0], $_->[1], $_->[2], $_->[3] }" } @{$_[0]}); }

my @lines;
for my $s (@states) {
    my $l = $j->{labels}{$s};
    push @lines, "            new Block(" . str($s) . ", " . str($name{$s}) . ", $l->[0], $l->[1],\n"
        . "                new[,] { " . triples($cellsOf{$s}) . " },\n"
        . "                new[,] { " . segs($outline{$s}) . " }),";
}

open my $o, '>:raw:encoding(UTF-8)', $out or die "$out: $!\n";
print $o <<"EOT";
// GENERATED by Tools/dus_cartogram_prep.pl. DO NOT EDIT BY HAND.
//
// Source : ../PoliSim-captures/design/dus/tiles.json (board 29a's cartogram table, out of tree; SHA256SUMS.txt beside it)
// SHA-256: $digest
// Names  : ../PoliSim-captures/design/dus/Election Night US v3.5.dc.html (boards 29a-29b, its NAMES table)
// SHA-256: $boardDigest
//
// UsNightDiagnostic holds this table to the catalog's electors and House seats and to its own invariants.

namespace PoliSim.Elections.Generated
{
    /// <summary>US-34 (boards 29a-29b, a PROPOSAL): THE ELECTORAL COLLEGE AS TILES - one cell per elector on a $cols x $rows grid, each of the 51
    /// jurisdictions one contiguous block of its own electors, Maine's and Nebraska's district electors their own numbered cells, the borders as segments
    /// on the grid's lines - positions as the board's, never re-derived; the names the board's. The night reads its counts from the model, never from here.</summary>
    public static class UsTileCartogram
    {
        public const string SourceDigest = "$digest";
        public const string NamesDigest = "$boardDigest";

        /// <summary>The grid, in cells.</summary>
        public const int Columns = $cols, Rows = $rows;

        /// <summary>One jurisdiction: its postal code and name, the cell its code is printed in, its cells [col, row, district] in reading order (row,
        /// then column; the district 0 for an elector at large or statewide), and its outline's segments [x1, y1, x2, y2] on the grid's lines.</summary>
        public sealed class Block
        {
            public readonly string Code, Name;
            public readonly int LabelColumn, LabelRow;
            public readonly int[,] Cells, Outline;

            public Block(string code, string name, int labelColumn, int labelRow, int[,] cells, int[,] outline)
            {
                Code = code; Name = name; LabelColumn = labelColumn; LabelRow = labelRow; Cells = cells; Outline = outline;
            }
        }

        /// <summary>The 51, by postal code.</summary>
        public static readonly Block[] States =
        {
@{[join("\n", @lines)]}
        };

        /// <summary>The borders between two jurisdictions' cells.</summary>
        public static readonly int[,] Borders = { @{[segs(\@borders)]} };
    }
}
EOT
close $o;
print "wrote $out: 51 jurisdictions, $total cells, " . scalar(@borders) . " border segments; tiles $digest; names $boardDigest\n";
