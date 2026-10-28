#!/usr/bin/perl
# record_factor_refit.pl - GATE-18's record-factor refit: writes the fit Gate18Diagnostic printed into Assets/Scripts/Elections/RecordFactors.cs, or
# confirms the stored factors are that fit. The refit of every election of record the table carries, in one step, from one log.
#
#   perl Tools/record_factor_refit.pl <gate18 log> --record s<NNN> [--label <name>] [--dry]   rewrite the table's rows from the log's fit
#   perl Tools/record_factor_refit.pl <gate18 log> --check                                    exit 0 where every stored factor is the log's fit, 1 where not
#
# What it reads: the FIT lines ("      FIT Sweden2026 S 0.99933090719699225  (handed ..., record ...)") inside the check's own block - from its
# "=== Gate18Diagnostic (" header to its closing "=== Gate18Diagnostic:" line, the last such block in the log - each figure as the check printed it
# (round-trip "R"), copied byte for byte and never re-formatted, so a figure is generated, not transcribed (CLAUDE.md, §419). The play-time
# "FACTORS:" lines are not read: they print four decimals.
# What it writes: each table row `(ElectionVintage.<election>, "<party>", <factor>),` of an election the block fitted gets the block's figure and the
# trailing comment "// [FITTED] refit s<NNN> from <label>, the fit's fixed point" (the label defaults to the log's file name without its extension);
# every other byte - the line endings, the comment lines between rows, the rows' order - is kept, and the same log with the same arguments writes the
# same bytes. A row whose trailing comment is anything but this tool's own is never overwritten.
# It dies, writing nothing, on: no closed block; no FIT line; an unparsable or non-finite figure; an (election, party) printed twice; an election the
# table carries that the block printed no fit for; a row of a fitted election left unprinted; a FIT line with no row, or a row there more than once.
# Then run Gate18Diagnostic again: every election must print "ok ... the stored factors are the fit", and --check on that log exits 0.
# The first form of this tool is Tools/chamber_factor_patch.pl (GATE-18 piece two), kept for its record.
use strict; use warnings;
use Digest::SHA qw(sha256_hex);

my ($log, $record, $label, $dry, $check);
while (@ARGV) {
    my $a = shift @ARGV;
    if    ($a eq '--record') { $record = shift @ARGV; }
    elsif ($a eq '--label')  { $label  = shift @ARGV; }
    elsif ($a eq '--dry')    { $dry    = 1; }
    elsif ($a eq '--check')  { $check  = 1; }
    elsif ($a =~ /^--/)      { die "unknown option $a\n"; }
    elsif (!defined $log)    { $log    = $a; }
    else                     { die "one log only (also given: $a)\n"; }
}
my $usage = "usage: perl Tools/record_factor_refit.pl <gate18 log> --record s<NNN> [--label <name>] [--dry]\n"
          . "       perl Tools/record_factor_refit.pl <gate18 log> --check\n";
die $usage unless defined $log;
die "--check takes no other option\n$usage" if $check && (defined $record || defined $label || $dry);
die "--record s<NNN> is required to write (the record section the refit is made under)\n$usage" if !$check && !(defined $record && $record =~ /^s[0-9]+$/);
if (!defined $label) { ($label = $log) =~ s{^.*[/\\]}{}; $label =~ s/\.[^.]*$//; }
die "the label '$label' is not one word of letters, digits, '.', '_' or '-'\n" unless $label =~ /^[A-Za-z0-9._-]+$/;
die "run from the project root\n" unless -d 'Assets/Scripts/Elections';
my $target = 'Assets/Scripts/Elections/RecordFactors.cs';
my $num = qr/[0-9]+(?:\.[0-9]+)?(?:[eE][-+]?[0-9]+)?/;

# ---- the log: the last closed Gate18Diagnostic block, its FIT lines
open my $lh, '<:raw', $log or die "$log: $!";
my (@block, $in, @last);
while (my $l = <$lh>) {
    if ($l =~ /^=== Gate18Diagnostic \(/) { $in = 1; @block = (); next; }
    if ($in && $l =~ /^=== Gate18Diagnostic:/) { $in = 0; @last = @block; next; }
    push @block, $l if $in;
}
close $lh;
die "no closed Gate18Diagnostic block in $log\n" unless @last;
my (%fit, @order);
for my $l (@last) {
    next unless $l =~ /^\s+FIT (\S+) (\S+) (\S+)\s/;
    my ($e, $p, $f) = ($1, $2, $3);
    die "$e $p: the figure '$f' is not a finite number - nothing written\n" unless $f =~ /^$num$/;
    die "$e $p: printed more than once in the block - nothing written\n" if exists $fit{$e}{$p};
    $fit{$e}{$p} = $f;
    push @order, [$e, $p];
}
die "no FIT lines in the block - nothing written\n" unless @order;

# ---- the table: every row, matched on its text
open my $th, '<:raw', $target or die "$target: $!";
my $t = do { local $/; <$th> }; close $th;
my $row = qr/^([ \t]*\(ElectionVintage\.(\w+), "([^"]+)", )(double\.NaN|$num)(\),)([^\r\n]*)(?=\r?\n|\z)/m;
my (%rows, %tableElections, @rowOrder);
while ($t =~ /$row/g) {
    my ($e, $p) = ($2, $3);
    die "$e $p: the table holds the row more than once - nothing written\n" if exists $rows{$e}{$p};
    $rows{$e}{$p} = { stored => $4, tail => $6 };
    $tableElections{$e} = 1;
    push @rowOrder, [$e, $p];
}
die "no table rows found in $target\n" unless @rowOrder;
for my $e (sort keys %tableElections) { die "$e: the table carries it but the block printed no fit for it - nothing written\n" unless $fit{$e}; }
for my $e (sort keys %fit) {
    die "$e: the block fitted it but the table carries no row of it - nothing written\n" unless $tableElections{$e};
    for my $p (sort keys %{ $fit{$e} }) { die "$e $p: the block printed a fit the table holds no row for - nothing written\n" unless $rows{$e}{$p}; }
    for my $p (sort keys %{ $rows{$e} }) { die "$e $p: the table holds it but the block printed no FIT line for it - nothing written\n" unless exists $fit{$e}{$p}; }
}

# the relative move as Gate18Diagnostic measures it: |stored - fit| / max(1, |fit|)
sub moved { my ($s, $f) = @_; return 'unfitted' if $s eq 'double.NaN'; my $m = abs($s - $f) / (abs($f) > 1 ? abs($f) : 1); return sprintf('%.1e', $m); }

# ---- --check
if ($check) {
    my $bad = 0;
    for my $e (sort keys %fit) {
        my @miss = grep { $rows{$e}{$_}{stored} ne $fit{$e}{$_} } map { $_->[1] } grep { $_->[0] eq $e } @rowOrder;
        if (!@miss) { printf "%s: %d row(s) - the stored factors are the fit\n", $e, scalar keys %{ $rows{$e} }; next; }
        $bad += @miss;
        printf "%s: %d of %d row(s) DIFFER from the fit\n", $e, scalar @miss, scalar keys %{ $rows{$e} };
        printf "    %s stored %s, fit %s (relative move %s)\n", $_, $rows{$e}{$_}{stored}, $fit{$e}{$_}, moved($rows{$e}{$_}{stored}, $fit{$e}{$_}) for @miss;
    }
    if ($bad) { print "CHECK FAILED: $bad stored factor(s) are not the fit of $log\n"; exit 1; }
    printf "CHECK OK: every stored factor is the fit of %s (%d row(s), %d election(s)); sha256 %s %s\n", $log, scalar @rowOrder, scalar keys %fit, sha256_hex($t), $target;
    exit 0;
}

# ---- write
my $ours = qr/^\s*\/\/ \[FITTED\] refit s[0-9]+ from [A-Za-z0-9._-]+, the fit's fixed point\s*$/;
for my $r (@rowOrder) {
    my $tail = $rows{$r->[0]}{$r->[1]}{tail};
    die "$r->[0] $r->[1]: the row carries a trailing comment that is not this tool's ('$tail') - nothing written\n" unless $tail eq '' || $tail =~ $ours;
}
my $comment = "   // [FITTED] refit $record from $label, the fit's fixed point";
my ($changed, $same) = (0, 0);
$t =~ s{$row}{
    my ($head, $e, $p, $old, $close) = ($1, $2, $3, $4, $5);
    my $new = $fit{$e}{$p};
    if ($old eq $new) { $same++; } else { $changed++; printf "%s %s: %s -> %s (relative move %s)\n", $e, $p, $old, $new, moved($old, $new); }
    $head . $new . $close . $comment;
}ge;
# the result re-read: every row the fit, the row count unchanged
my $n = 0;
while ($t =~ /$row/g) { $n++; die "$2 $3: the rewritten row is not the fit - nothing written\n" unless $4 eq $fit{$2}{$3} && $6 eq $comment; }
die "the rewrite left $n row(s) of " . scalar(@rowOrder) . " - nothing written\n" unless $n == @rowOrder;
printf "%d factor(s) moved, %d already the fit, over %d election(s); every row's comment: %s\n", $changed, $same, scalar keys %fit, $comment =~ s/^\s+//r;
if ($dry) { print "dry run - nothing written\n"; exit 0; }
open $th, '>:raw', $target or die "$target: $!"; print $th $t; close $th;
printf "patched %s; sha256 %s\n", $target, sha256_hex($t);
