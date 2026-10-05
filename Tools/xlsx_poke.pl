#!/usr/bin/perl
# xlsx_poke.pl <xlsx> <old text> <new text> - rewrites one value in whichever worksheet part of a workbook holds it exactly once, keeping every
# other part and the members' order (Tools/us_returns_mutations.sh's way to reach a workbook's figures in its copy; never run on ElectionsData).
# Dies unless the old text is in exactly one worksheet part, exactly once.
use strict;
use warnings;
use IO::Uncompress::Unzip qw($UnzipError);
use IO::Compress::Zip qw($ZipError :zip_method);
my ($file, $old, $new) = @ARGV;
die "usage: xlsx_poke.pl <xlsx> <old text> <new text>\n" unless defined $new;
my $u = IO::Uncompress::Unzip->new($file) or die "$file: $UnzipError\n";
my (@names, %part);
for (my $st = 1; $st > 0; $st = $u->nextStream()) { my $n = $u->getHeaderInfo()->{Name}; my ($b, $c) = (''); while ($u->read($c) > 0) { $b .= $c; } push @names, $n; $part{$n} = $b; }
$u->close();
my @hits = grep { $_ =~ m{^xl/worksheets/} && index($part{$_}, $old) >= 0 } @names;
die "'$old' in " . scalar(@hits) . " worksheet parts, not one\n" unless @hits == 1;
my $n = () = $part{$hits[0]} =~ /\Q$old\E/g;
die "'$old' $n times in $hits[0], not once\n" unless $n == 1;
$part{$hits[0]} =~ s/\Q$old\E/$new/;
my $out = '';
my $z = IO::Compress::Zip->new(\$out, Name => $names[0], Method => ZIP_CM_DEFLATE) or die "zip: $ZipError\n";
$z->print($part{$names[0]});
for my $i (1 .. $#names) { $z->newStream(Name => $names[$i], Method => ZIP_CM_DEFLATE); $z->print($part{$names[$i]}); }
$z->close();
open my $w, '>:raw', $file or die "$file: $!\n";
print $w $out or die "$file: $!\n";
close $w or die "$file: $!\n";
print "poked $hits[0]\n";
