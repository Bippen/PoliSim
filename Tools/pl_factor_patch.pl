#!/usr/bin/perl
# pl_factor_patch.pl <unity log> [--dry] - E1 (§772): patches Poland's candidate factors into Assets/Scripts/Elections/PresidencyOfRecord.cs from the
# FIT lines PresidentialElectionLiveDiagnostic printed ("      FIT NAWROCKI PiS 0.86341393941290423  (poll ..., record ...)"), so a refit is
# patched in from the log and never retyped (CLAUDE.md: a figure is generated, not transcribed, §419) - the Polish twin of us_factor_patch.pl (§912).
# The FIT lines are read only inside the check's own block (from its "=== PresidentialElectionLiveDiagnostic (" header to its closing
# "=== PresidentialElectionLiveDiagnostic:" line), the last such block in the log. Each fitted name rewrites exactly one entry,
# `new CandidateOfRecord("<NAME>", "<party>", ..., <factor>,` - matched on its name and party exactly once; every other byte (line endings
# included) is kept, and it dies on a missing block, an unparsable figure or a pattern that is not there exactly once. The second argument is
# --dry or nothing. Then run the check again: it must print the stored factors as the fit.
use strict; use warnings;
my ($log, $dry) = @ARGV;
die "usage: pl_factor_patch.pl <unity log> [--dry]\n" unless defined $log && (!defined $dry || $dry eq '--dry');
my $target = 'Assets/Scripts/Elections/PresidencyOfRecord.cs';
open my $lh, '<:raw', $log or die "$log: $!";
my (@block, $in, @last);
while (my $l = <$lh>) {
    if ($l =~ /^=== PresidentialElectionLiveDiagnostic \(/) { $in = 1; @block = (); next; }
    if ($in && $l =~ /^=== PresidentialElectionLiveDiagnostic:/) { $in = 0; @last = @block; next; }
    push @block, $l if $in;
}
close $lh;
die "no closed PresidentialElectionLiveDiagnostic block in $log\n" unless @last;
my @fits;
for my $l (@last) {
    next unless $l =~ /^\s+FIT (\S+) (\S+) ([0-9]+\.[0-9]+(?:[eE][-+]?[0-9]+)?)\s/;
    next if $1 eq 'USA';
    push @fits, [$1, $2, $3];
}
die "no Polish FIT lines in the block\n" unless @fits;
open my $th, '<:raw', $target or die "$target: $!";
local $/; my $t = <$th>; close $th;
for my $f (@fits) {
    my ($name, $party, $fit) = @$f;
    my $re = qr/(new CandidateOfRecord\("\Q$name\E", "\Q$party\E", [^,]+, )([0-9]+\.[0-9]+(?:[eE][-+]?[0-9]+)?)(,)/;
    my $n = 0; $n++ while $t =~ /$re/g;
    die "$name ($party): the entry is there $n time(s), not once\n" unless $n == 1;
    $t =~ /$re/; my $old = $2;
    $t =~ s/$re/$1$fit$3/;
    print "$name ($party): $old -> $fit\n";
}
if (defined $dry) { print "dry run - nothing written\n"; exit 0; }
open $th, '>:raw', $target or die "$target: $!"; print $th $t; close $th;
print "patched $target\n";
