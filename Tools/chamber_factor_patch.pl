#!/usr/bin/perl
# chamber_factor_patch.pl <unity log> [--dry] - GATE-18 piece two (q18 with K-1, E1's form at the count's unit): patches the chamber elections' eve-fitted
# factors into Assets/Scripts/Elections/RecordFactors.cs from the FIT lines Gate18Diagnostic printed
# ("      FIT Sweden2026 S 1.0234567890123  (handed ..., record ...)"), so a fit is patched in from the log and never retyped (CLAUDE.md: a figure is
# generated, not transcribed, §419) - the chamber twin of pl_factor_patch.pl and us_factor_patch.pl.
# The FIT lines are read only inside the check's own block (from its "=== Gate18Diagnostic (" header to its closing "=== Gate18Diagnostic:" line), the
# last such block in the log. Each fitted (election, party) rewrites exactly one entry of the table, `(ElectionVintage.<election>, "<party>", <factor>),`
# - matched on its election and party exactly once; every other byte (line endings included) is kept. It dies on a missing block, an unparsable or
# non-finite figure, an (election, party) printed twice, a table entry of a fitted election left unprinted, or a pattern that is not there exactly once, and writes nothing then. The second argument is --dry or nothing. Then run the check
# again: it must print "the stored factors are the fit" for every election.
use strict; use warnings;
my ($log, $dry) = @ARGV;
die "usage: perl Tools/chamber_factor_patch.pl <unity log> [--dry]\n" unless defined $log && $log ne '--dry' && (!defined $dry || $dry eq '--dry');
die "run from the project root\n" unless -d 'Assets/Scripts/Elections';
my $target = 'Assets/Scripts/Elections/RecordFactors.cs';
open my $lh, '<:raw', $log or die "$log: $!";
my (@block, $in, @last);
while (my $l = <$lh>) {
    if ($l =~ /^=== Gate18Diagnostic \(/) { $in = 1; @block = (); next; }
    if ($in && $l =~ /^=== Gate18Diagnostic:/) { $in = 0; @last = @block; next; }
    push @block, $l if $in;
}
close $lh;
die "no closed Gate18Diagnostic block in $log\n" unless @last;
my @fits;
for my $l (@last) {
    next unless $l =~ /^\s+FIT (\S+) (\S+) (\S+)\s/;
    my ($election, $party, $fit) = ($1, $2, $3);
    die "$election $party: the figure '$fit' is not a finite number\n" unless $fit =~ /^[0-9]+(?:\.[0-9]+)?(?:[eE][-+]?[0-9]+)?$/;
    push @fits, [$election, $party, $fit];
}
die "no FIT lines in the block\n" unless @fits;
my %seen;
for my $f (@fits) { my $k = "$f->[0] $f->[1]"; die "$k: printed more than once in the block - nothing written\n" if $seen{$k}++; }
open my $th, '<:raw', $target or die "$target: $!";
my $t = do { local $/; <$th> }; close $th;
my %fitted = map { $_->[0] => 1 } @fits;
while ($t =~ /\(ElectionVintage\.(\w+), "([^"]+)", (?:double\.NaN|[0-9.eE+-]+)\),/g) {
    die "$1 $2: the table holds it but the block printed no FIT line for it - nothing written\n" if $fitted{$1} && !$seen{"$1 $2"};
}
for my $f (@fits) {
    my ($election, $party, $fit) = @$f;
    my $re = qr/(\(ElectionVintage\.\Q$election\E, "\Q$party\E", )(double\.NaN|[0-9]+(?:\.[0-9]+)?(?:[eE][-+]?[0-9]+)?)(\),)/;
    my $n = 0; $n++ while $t =~ /$re/g;
    die "$election $party: the entry is there $n time(s), not once - nothing written\n" unless $n == 1;
    $t =~ /$re/; my $old = $2;
    $t =~ s/$re/$1$fit$3/;
    print "$election $party: $old -> $fit\n";
}
if (defined $dry) { print "dry run - nothing written\n"; exit 0; }
open $th, '>:raw', $target or die "$target: $!"; print $th $t; close $th;
print "patched $target\n";
