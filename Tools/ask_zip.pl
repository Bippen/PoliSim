#!/usr/bin/perl
# The standard form for a Design ask: MANIFEST.sha256 over the loose files (ASK.md first), then ONE stored zip (Method 0) holding the
# manifest first and every listed file after it. Usage: perl Tools/ask_zip.pl <dir> <zipname> <file>...
# Moved into Tools/ with the D-DE ask (COMPLETED.md s700) - it had cut D-ST's and D-CP's asks from a session scratchpad; a send's tooling is committed.
# A stored zip survives the Design store byte-exact (s610); loose PNGs do not (the store injects a C2PA chunk) - read them from the zip.
use strict; use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Compress::Zip qw(zip $ZipError);
my ($dir, $zipname, @files) = @ARGV;
die "usage: askzip.pl <dir> <zipname> <file>...\n" unless $dir && $zipname && @files;
chdir $dir or die "$dir: $!\n";
open my $m, '>:raw', 'MANIFEST.sha256' or die;
for my $f (@files) {
    open my $h, '<:raw', $f or die "$f: $!\n"; local $/; my $b = <$h>; close $h;
    print $m sha256_hex($b), "  ", $f, "\n";
}
close $m;
unlink $zipname;
zip [ 'MANIFEST.sha256', @files ] => $zipname, Method => 0, BinModeIn => 1 or die "zip: $ZipError\n";
print "WROTE $dir/$zipname with MANIFEST.sha256 + ", scalar(@files), " file(s)\n";
