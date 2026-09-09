# Validate a PNG structurally: every chunk's CRC32 must match, and it must end with IEND.
# A transcription error anywhere in the file breaks a CRC, so this is proof the bytes are the
# delivered ones rather than something that merely decodes.
use strict; use warnings;
sub crc32 { my $b = shift; my $c = 0xFFFFFFFF;
  for my $ch (unpack("C*", $b)) { $c ^= $ch; for (1..8) { $c = ($c & 1) ? (($c >> 1) ^ 0xEDB88320) : ($c >> 1); } }
  return $c ^ 0xFFFFFFFF; }
my $bad = 0;
for my $p (@ARGV) {
  open(my $h, '<:raw', $p) or do { print "MISSING $p\n"; $bad++; next; };
  local $/; my $b = <$h>; close $h;
  my $name = $p; $name =~ s{.*/}{};
  if (substr($b,0,8) ne "\x89PNG\r\n\x1a\n") { printf("%-40s NOT A PNG\n", $name); $bad++; next; }
  my $i = 8; my $ok = 1; my $chunks = 0; my $end = 0;
  while ($i + 8 <= length($b)) {
    my $len = unpack("N", substr($b,$i,4));
    my $type = substr($b,$i+4,4);
    last if $i + 12 + $len > length($b);
    my $data = substr($b,$i+8,$len);
    my $crc = unpack("N", substr($b,$i+8+$len,4));
    if (crc32($type.$data) != $crc) { $ok = 0; printf("%-40s CRC FAIL in %s at %d\n", $name, $type, $i); last; }
    $chunks++; $end = 1 if $type eq "IEND";
    $i += 12 + $len;
  }
  if ($ok && $end && $i == length($b)) { printf("%-40s ok  %d chunks, %d bytes\n", $name, $chunks, length($b)); }
  else { printf("%-40s BROKEN (chunks %d, iend %d, stopped at %d of %d)\n", $name, $chunks, $end, $i, length($b)); $bad++; }
}
print "broken: $bad\n";
