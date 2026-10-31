#!/usr/bin/perl
# S5 (Design's return of 2026-10-30, answer 5): THE CREATED PARTY'S MARK CELLS, CUT WHITE ON TRANSPARENT - a deterministic recolour of
# OUR OWN installed asset, never a redraw and never a recreation from an image.
#
#   perl Tools/s5_cell_whitecut.pl <cell.png>...        rewrites each file in place; prints one line per file
#   perl Tools/s5_cell_whitecut.pl --check <cell.png>... changes nothing; exits 1 unless every file is already white-cut
#
# The cells under Assets/Resources/Art/UI/Cells/ were authored in slate (95,102,114 measured, §676), so a tint multiplied them toward black and
# every ink read alike. Design's answer: cut them white on transparent and tint them in the party's ink. What this does to each file, and
# nothing else:
#   - every pixel's R, G and B become 255; its ALPHA IS KEPT, byte for byte (so the silhouette, the cut, the hatch's transparent stripes and the
#     anti-aliased edge are exactly the delivered file's); a fully transparent pixel goes to (255,255,255,0) too, so filtering at the edge
#     bleeds white, never black, under a tint;
#   - the file is re-encoded as IHDR (unchanged), one IDAT (filter 0 on every row, zlib level 9) and IEND. Every ancillary chunk is dropped and
#     named in the output: the cells carry a 5 758-byte caBX (the C2PA block the design store injects into a PNG it reads, CLAUDE.md's channel
#     note), which signs pixels this tool has changed, so keeping it would certify a file that no longer exists.
# Only 8-bit RGBA, non-interlaced input is accepted; anything else stops the run before a byte is written. Idempotent: a second run rewrites
# the same bytes. The alpha plane is compared before and after inside the run, and a mismatch stops it.
use strict; use warnings;
use Compress::Zlib;
use Digest::SHA qw(sha256_hex);

my $check = 0;
if (@ARGV && $ARGV[0] eq '--check') { $check = 1; shift @ARGV; }
die "usage: s5_cell_whitecut.pl [--check] <cell.png>...\n" unless @ARGV;

sub crc32_of { return crc32($_[0]); }
sub chunk { my ($type, $data) = @_; return pack('N', length $data) . $type . $data . pack('N', crc32($type . $data)); }

sub decode {
    my ($f, $b) = @_;
    substr($b, 0, 8) eq "\x89PNG\r\n\x1a\n" or die "$f: not a PNG\n";
    my ($p, $ihdr, $idat, @other) = (8, undef, '');
    while ($p < length $b) {
        my ($len, $type) = unpack('N a4', substr($b, $p, 8));
        my $data = substr($b, $p + 8, $len);
        my $crc = unpack('N', substr($b, $p + 8 + $len, 4));
        $crc == crc32($type . $data) or die "$f: CRC mismatch in $type\n";
        if ($type eq 'IHDR') { $ihdr = $data; } elsif ($type eq 'IDAT') { $idat .= $data; } elsif ($type ne 'IEND') { push @other, "$type($len)"; }
        $p += 12 + $len;
        last if $type eq 'IEND';
    }
    defined $ihdr or die "$f: no IHDR\n";
    my ($w, $h, $depth, $ct, $comp, $filt, $lace) = unpack('N N C C C C C', $ihdr);
    ($depth == 8 && $ct == 6 && $lace == 0) or die "$f: only 8-bit RGBA non-interlaced is handled (depth $depth, colour type $ct, interlace $lace)\n";
    my $raw = uncompress($idat); defined $raw or die "$f: IDAT does not inflate\n";
    my $stride = $w * 4;
    length($raw) == $h * ($stride + 1) or die "$f: IDAT holds " . length($raw) . " bytes, not " . ($h * ($stride + 1)) . "\n";
    my @rows; my @prev = (0) x $stride;
    for my $y (0 .. $h - 1) {
        my $ft = ord substr($raw, $y * ($stride + 1), 1);
        my @cur = unpack('C*', substr($raw, $y * ($stride + 1) + 1, $stride));
        for my $i (0 .. $stride - 1) {
            my $a = $i >= 4 ? $cur[$i - 4] : 0; my $up = $prev[$i]; my $c = $i >= 4 ? $prev[$i - 4] : 0;
            my $x = $cur[$i];
            if ($ft == 1) { $x += $a; } elsif ($ft == 2) { $x += $up; } elsif ($ft == 3) { $x += int(($a + $up) / 2); }
            elsif ($ft == 4) { my $pp = $a + $up - $c; my ($pa, $pb, $pc) = (abs($pp - $a), abs($pp - $up), abs($pp - $c)); $x += ($pa <= $pb && $pa <= $pc) ? $a : ($pb <= $pc ? $up : $c); }
            elsif ($ft != 0) { die "$f: row $y has filter type $ft\n"; }
            $cur[$i] = $x & 255;
        }
        push @rows, [@cur]; @prev = @cur;
    }
    return ($ihdr, $w, $h, \@rows, \@other);
}

sub alpha_of { my ($rows) = @_; my $s = ''; for my $r (@$rows) { for (my $i = 3; $i < @$r; $i += 4) { $s .= chr $r->[$i]; } } return $s; }

my $bad = 0;
for my $f (@ARGV) {
    open my $fh, '<:raw', $f or die "$f: $!"; local $/; my $b = <$fh>; close $fh;
    my $before = sha256_hex($b);
    my ($ihdr, $w, $h, $rows, $other) = decode($f, $b);
    my ($opaque, $partial, $clear, $coloured) = (0, 0, 0, 0);
    for my $r (@$rows) { for (my $i = 0; $i < @$r; $i += 4) { my $al = $r->[$i + 3]; $al == 255 ? $opaque++ : $al == 0 ? $clear++ : $partial++; $coloured++ if $r->[$i] != 255 || $r->[$i + 1] != 255 || $r->[$i + 2] != 255; } }
    (my $name = $f) =~ s{.*[/\\]}{};
    if ($check) {
        my $ok = $coloured == 0 && !@$other;
        printf "%-40s %s (%d px not white, ancillary: %s)\n", $name, $ok ? 'WHITE-CUT' : 'NOT WHITE-CUT', $coloured, @$other ? join(' ', @$other) : 'none';
        $bad++ unless $ok;
        next;
    }
    my $alphaBefore = alpha_of($rows);
    my $out = '';
    for my $r (@$rows) { for (my $i = 0; $i < @$r; $i += 4) { $r->[$i] = $r->[$i + 1] = $r->[$i + 2] = 255; } $out .= "\0" . pack('C*', @$r); }
    alpha_of($rows) eq $alphaBefore or die "$f: the alpha plane moved - nothing written\n";
    my $png = "\x89PNG\r\n\x1a\n" . chunk('IHDR', $ihdr) . chunk('IDAT', compress($out, 9)) . chunk('IEND', '');
    my (undef, undef, undef, $check_rows) = decode($f, $png);
    alpha_of($check_rows) eq $alphaBefore or die "$f: the re-encoded file does not decode to the same alpha - nothing written\n";
    open my $o, '>:raw', $f or die "$f: $!"; print $o $png; close $o;
    printf "%-40s %dx%d  opaque %d partial %d clear %d  %d px recoloured  dropped: %s  sha256 %s -> %s\n", $name, $w, $h, $opaque, $partial, $clear, $coloured,
        @$other ? join(' ', @$other) : 'none', substr($before, 0, 12), substr(sha256_hex($png), 0, 12);
}
exit($bad ? 1 : 0);
