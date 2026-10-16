#!/usr/bin/perl
# us_factor_patch.pl <unity log> [--dry] - PS-6 US-6: patches E1's US factors into Assets/Scripts/Elections/PresidencyOfRecord.cs from the FIT lines
# UsPresidentialFactorCheck printed ("      FIT USA TRUMP REP 1.01839...  (poll ..., record ...)"), so a refit is patched in from the log and never retyped
# (CLAUDE.md: a figure is generated, not transcribed, §419).
# The log must be a CheckSuite run of the check - a named run, the cheap bar or the documents bar: a "CHECKS: running ..." header naming
# UsPresidentialFactorCheck - and the FIT lines are read only inside the check's own block (from its "=== UsPresidentialFactorCheck (PS-6 US-6)"
# header to its closing "=== UsPresidentialFactorCheck:" line), the last such block in the log; both parties' lines must be in it.
# It rewrites only the two entries of Usa2024Factors - ("R", "REP", <factor>) and ("D", "DEM", <factor>) - each matched on its text exactly once,
# keeps every other byte (line endings included), and dies on a missing party, an unparsable figure or a pattern that is not there exactly once.
# The second argument is --dry or nothing. Then run the check again: it must print "E1 (US): the stored factors are the fit".
use strict;
use warnings;

die "usage: perl Tools/us_factor_patch.pl <unity log> [--dry]\n" unless @ARGV == 1 || (@ARGV == 2 && $ARGV[1] eq '--dry');
my ($log, $flag) = @ARGV;
die "usage: perl Tools/us_factor_patch.pl <unity log> [--dry] - the log is required and comes first\n" if $log eq '--dry';
die "run from the project root\n" unless -d 'Assets/Scripts/Elections';
my $dry = defined $flag;
my $target = 'Assets/Scripts/Elections/PresidencyOfRecord.cs';
my %ticket = (REP => 'R', DEM => 'D');

open my $l, '<:raw', $log or die "$log: $!\n";
my ($header, $inBlock, %block, %last) = (0, 0);
while (my $line = <$l>) {
    $line =~ s/\r?\n$//;
    $header = 1 if $line =~ /CHECKS: running .*\bUsPresidentialFactorCheck\b/;
    if ($line =~ /^=== UsPresidentialFactorCheck \(PS-6 US-6\)/) { $inBlock = 1; %block = (); next; }
    if ($inBlock && $line =~ /^=== UsPresidentialFactorCheck:/) { $inBlock = 0; %last = %block if %block; next; }
    next unless $inBlock && $line =~ /^\s*FIT USA (\S+) (REP|DEM) (\S+)\s/;
    my ($surname, $party, $value) = ($1, $2, $3);
    die "$log: $surname ($party) carries '$value', not a finite number\n" unless $value =~ /^-?\d+(?:\.\d+)?(?:[Ee][-+]?\d+)?$/;
    $block{$party} = [$surname, $value];
}
close $l;
die "$log: no CheckSuite header running UsPresidentialFactorCheck (\"CHECKS: running ...\") - not a run of the check through the suite\n" unless $header;
die "$log: no complete UsPresidentialFactorCheck block (its header and its closing line) holding FIT USA lines\n" unless %last;
for my $party (sort keys %ticket) { die "$log: the check's last block holds no FIT USA line for $party\n" unless $last{$party}; }

open my $h, '<:raw', $target or die "$target: $!\n";
my $src = do { local $/; <$h> };
close $h;
for my $party (sort keys %ticket) {
    my ($surname, $value) = @{ $last{$party} };
    my $t = $ticket{$party};
    my $n = () = $src =~ /\("\Q$t\E", "\Q$party\E", [^)\r\n]*\)/g;
    die "$target: (\"$t\", \"$party\", ...) found $n times, not once\n" unless $n == 1;
    $src =~ /\("\Q$t\E", "\Q$party\E", ([^)\r\n]*)\)/;
    my $old = $1;
    $src =~ s/\("\Q$t\E", "\Q$party\E", [^)\r\n]*\)/("$t", "$party", $value)/;
    print "$party ($surname): $old -> $value\n";
}
if ($dry) { print "dry run - $target not written\n"; exit 0; }
open my $w, '>:raw', $target or die "$target: $!\n";
print $w $src or die "$target: $!\n";
close $w or die "$target: $!\n";
print "patched $target\n";
