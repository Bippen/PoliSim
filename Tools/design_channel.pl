#!/usr/bin/perl
# THE DESIGN CHANNEL'S VERIFIER (X-1, COMPLETED.md §610, 2026-09-24).
#
# The channel is the DesignSync tool (/design-sync) from the MAIN session: `write_files` pushes files from a
# local folder by path, `get_file` pulls one file back as base64 into the session. This script is the part a
# session cannot do in its head: it reads a pulled result OFF DISK (the harness persists any tool result over
# about 50 KB to tool-results/<id>.txt), decodes it, walks the PNG's chunks, strips the caBX chunk the store
# injects, and digests - so a binary never travels through a context (CLAUDE.md, §424/§427).
#
#   perl Tools/design_channel.pl manifest <folder>                 write <folder>/MANIFEST.sha256 (sha256sum -c form)
#   perl Tools/design_channel.pl decode <tool-result.txt> <out>    decode a persisted get_file result to <out>; prints truncated, bytes, sha256
#   perl Tools/design_channel.pl compare <pulled.png> <original>   strip caBX from the pulled PNG and compare against the original's prefix
#
# Measured on 2026-09-24 (§610): a pull is capped at 262 144 base64 characters = 196 608 bytes and says
# "truncated":true past it; every PNG the store holds carries a 5 758-byte caBX (C2PA) chunk after IHDR that the
# pushed file did not, so a PNG's digest never survives the store, while its IDAT stream does; a zip is stored
# byte-exact. Send binaries in a zip with the manifest inside; pull only what is under the cap.
use strict; use warnings;
use MIME::Base64 qw(decode_base64);
use Digest::SHA qw(sha256_hex);

my ($mode, @a) = @ARGV;
$mode ||= '';

sub slurp { my ($p) = @_; open my $h, '<:raw', $p or die "$p: $!"; local $/; my $s = <$h>; close $h; $s }
sub chunks {
  my ($bin) = @_; my $pos = 8; my @c;
  return () unless substr($bin, 0, 8) eq "\x89PNG\r\n\x1a\n";
  while ($pos + 8 <= length $bin) {
    my ($len, $type) = unpack('N a4', substr($bin, $pos, 8));
    push @c, [$type, $len, $pos]; $pos += 12 + $len; last if $type eq 'IEND';
  }
  @c
}
sub strip_cabx {
  my ($bin) = @_; my $out = substr($bin, 0, 8);
  for my $c (chunks($bin)) { my ($type, $len, $pos) = @$c; $out .= substr($bin, $pos, 12 + $len) unless $type eq 'caBX'; }
  $out
}

if ($mode eq 'manifest') {
  my ($dir) = @a; die "manifest <folder>\n" unless $dir && -d $dir;
  opendir my $d, $dir or die; my @files = sort grep { -f "$dir/$_" && $_ ne 'MANIFEST.sha256' } readdir $d; closedir $d;
  open my $m, '>:raw', "$dir/MANIFEST.sha256" or die;
  for my $f (@files) { my $s = slurp("$dir/$f"); printf $m "%s  %s\n", sha256_hex($s), $f; printf "%s  %s  (%d bytes)\n", sha256_hex($s), $f, length $s; }
  close $m; print scalar(@files), " files in $dir/MANIFEST.sha256\n";
}
elsif ($mode eq 'decode') {
  my ($src, $out) = @a; die "decode <tool-result.txt> <out>\n" unless $src && $out;
  my $j = slurp($src);
  my ($tr) = $j =~ /"truncated":(true|false)/;
  my ($b64flag) = $j =~ /"isBase64":(true|false)/;
  my ($content) = $j =~ /"content":"(.*?)","contentType"/s or die "no content field in $src\n";
  my $bin;
  if (($b64flag // 'false') eq 'true') { $bin = decode_base64($content); }
  else { $bin = $content; $bin =~ s/\\n/\n/g; $bin =~ s/\\t/\t/g; $bin =~ s/\\"/"/g; $bin =~ s/\\\\/\\/g; }
  open my $o, '>:raw', $out or die "$out: $!"; print $o $bin; close $o;
  printf "%s: truncated=%s base64=%s bytes=%d sha256=%s\n", $out, $tr // '?', $b64flag // '?', length $bin, sha256_hex($bin);
  my @c = chunks($bin);
  if (@c) { printf "  PNG chunks: %s%s\n", join(' ', map { "$_->[0]:$_->[1]" } @c[0 .. ($#c < 5 ? $#c : 5)]), (@c > 6 ? " ... " . scalar(@c) . " chunks, last $c[-1][0]" : ''); }
  print "  ⚠ TRUNCATED - the store holds more than the pull cap returns; verify this file by its prefix only\n" if ($tr // '') eq 'true';
}
elsif ($mode eq 'compare') {
  my ($pulled, $orig) = @a; die "compare <pulled.png> <original.png>\n" unless $pulled && $orig;
  my $p = slurp($pulled); my $o = slurp($orig);
  my $s = strip_cabx($p); my $L = length $s;
  my $cabx = grep { $_->[0] eq 'caBX' } chunks($p);
  printf "pulled %d bytes (%d caBX chunk%s), without caBX %d bytes; original %d bytes\n", length $p, $cabx, ($cabx == 1 ? '' : 's'), $L, length $o;
  printf "  without caBX: sha256 %s\n  original:     sha256 %s\n", sha256_hex($s), sha256_hex($o);
  if ($s eq $o) { print "  WHOLE AND IDENTICAL once the injected chunk is removed\n"; }
  elsif ($L <= length($o) && substr($o, 0, $L) eq $s) { print "  PREFIX IDENTICAL for $L of ", length($o), " bytes (a truncated pull)\n"; }
  else { print "  DIFFERS\n"; exit 1; }
}
elsif ($mode eq 'split') {
  # THE SPLIT ARCHIVE (§612): a pull cannot read by range, so a file over the cap goes as parts under it.
  # Writes <outdir>/<name>.partNNN and <outdir>/<name>.parts.sha256 - the whole's digest and byte count on the
  # first line, one line per part (sha256sum -c form) after it.
  my ($file, $outdir, $partbytes) = @a; die "split <file> <outdir> [partbytes]\n" unless $file && $outdir;
  $partbytes ||= 180 * 1024;   # under the measured 196 608-byte cap
  die "part size must be under 196608 bytes\n" if $partbytes >= 196608;
  mkdir $outdir unless -d $outdir;
  my $bin = slurp($file); (my $name = $file) =~ s{.*[\\/]}{};
  my $n = int((length($bin) + $partbytes - 1) / $partbytes);
  open my $m, '>:raw', "$outdir/$name.parts.sha256" or die;
  printf $m "# WHOLE %s  %s  %d bytes  %d parts of %d\n", sha256_hex($bin), $name, length $bin, $n, $partbytes;
  for my $i (0 .. $n - 1) {
    my $part = substr($bin, $i * $partbytes, $partbytes);
    my $pn = sprintf('%s.part%03d', $name, $i + 1);
    open my $o, '>:raw', "$outdir/$pn" or die; print $o $part; close $o;
    printf $m "%s  %s\n", sha256_hex($part), $pn;
  }
  close $m; printf "%s: %d bytes -> %d parts of %d in %s (whole %s)\n", $name, length $bin, $n, $partbytes, $outdir, sha256_hex($bin);
}
elsif ($mode eq 'join') {
  # Reassemble parts against their sheet: every part's digest checked as it is read, the whole's digest and byte
  # count checked at the end; a single mismatch writes nothing.
  my ($dir, $out) = @a; die "join <partsdir> [out]\n" unless $dir;
  opendir my $d, $dir or die; my ($sheet) = grep { /\.parts\.sha256$/ } readdir $d; closedir $d;
  die "no .parts.sha256 sheet in $dir\n" unless $sheet;
  open my $s, '<:raw', "$dir/$sheet" or die; my @lines = <$s>; close $s;
  # §626: the whole's name may carry spaces (Design's board file does) - the name is everything between the two double spaces.
  my $head = shift @lines; my ($whole, $name, $bytes, $count) = $head =~ /^# WHOLE ([0-9a-f]{64})  (.+?)  (\d+) bytes  (\d+) parts/ or die "bad sheet head: $head";
  $out ||= "$dir/$name";
  my $bin = ''; my $ok = 0; my @bad;
  for my $l (@lines) {
    next unless $l =~ /^([0-9a-f]{64})  (.+?)\s*$/; my ($sum, $pn) = ($1, $2);
    if (!-f "$dir/$pn") { push @bad, "MISSING $pn"; next; }
    my $part = slurp("$dir/$pn");
    if (sha256_hex($part) ne $sum) { push @bad, "DIFFERS $pn"; next; }
    $bin .= $part; $ok++;
  }
  if (@bad || $ok != $count) { print "join FAILED: $ok of $count parts ok; @bad\n"; exit 1; }
  if (length($bin) != $bytes || sha256_hex($bin) ne $whole) { printf "join FAILED: whole reads %d bytes %s against the sheet's %d %s\n", length $bin, sha256_hex($bin), $bytes, $whole; exit 1; }
  open my $o, '>:raw', $out or die "$out: $!"; print $o $bin; close $o;
  printf "%s: %d parts joined, %d bytes, sha256 %s - WHOLE MATCHES THE SHEET\n", $out, $ok, length $bin, $whole;
}
else { print STDERR "modes: manifest <folder> | decode <tool-result.txt> <out> | compare <pulled.png> <original.png> | split <file> <outdir> [partbytes] | join <partsdir> [out]\n"; exit 2; }
