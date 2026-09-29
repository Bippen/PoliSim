#!/usr/bin/perl
# THE AT-REST TEXT-DRAW BASELINE (D24 item 5, 2026-09-29; COMPLETED.md s648). Reads a dry film's label table (the
# DryGuiPass record: one row per text draw per capture; PoliSim-captures/labels/<label>_<country>_<w>_labels.tsv) and
# prints what a screen AT REST asks the reader to read: the text draws inside the clip of each rail screen's own
# unscrolled frame. The screens are composed for UI v3.3 in this order - most text first - so the ranking is emitted,
# never typed.
#
#   perl Tools/text_baseline.pl rank   <labels.tsv>           the rail screens by visible text draws at rest, most first
#   perl Tools/text_baseline.pl screen <labels.tsv> <stem>     one frame's visible draws: font size, mode, width, text
#
# A rail screen's at-rest frame is the capture the sweep takes on arriving at it (the stems below, the sweep's own);
# a draw flagged OUTSIDE its clip is not on the screen and is not counted. The Desk is 01c_desk (the running desk).
use strict; use warnings;
binmode STDOUT, ':encoding(UTF-8)';
my ($mode, $tsv, $stem) = @ARGV;
die "usage: text_baseline.pl rank <labels.tsv> | screen <labels.tsv> <stem>\n" unless $mode && $tsv && ($mode eq 'rank' || ($mode eq 'screen' && $stem));
my %rail = (
    '01c_desk' => 'DESK', '02_statistics' => 'STATS', '03_decisions' => 'DOCKET', '04_demographics' => 'PEOPLE',
    '05_budget' => 'BUDGET', '06_policylaws' => 'LAWS', '07_politics' => 'POLITICS', '08_energy' => 'ENERGY',
);
open my $in, '<:encoding(UTF-8)', $tsv or die "$tsv: $!\n";
my $head = <$in>; chomp $head; $head =~ s/\r$//;
my @cols = split /\t/, $head; my %at; @at{@cols} = 0 .. $#cols;
for my $c (qw(capture font mode width flags text)) { die "the table has no '$c' column\n" unless exists $at{$c}; }
my (%count, @rows);
while (my $line = <$in>) {
    chomp $line; $line =~ s/\r$//;
    my @f = split /\t/, $line, -1;
    next if ($f[$at{flags}] // '') =~ /OUTSIDE/;
    my $cap = $f[$at{capture}];
    $count{$cap}++;
    push @rows, [ $f[$at{font}], $f[$at{mode}], $f[$at{width}], $f[$at{text}] ] if $mode eq 'screen' && $cap eq $stem;
}
close $in;
if ($mode eq 'rank') {
    my @present = grep { exists $count{$_} } keys %rail;
    my @missing = grep { !exists $count{$_} } sort keys %rail;
    print "rank\tscreen\tvisible text draws at rest\tframe\n";
    my $i = 0;
    for my $s (sort { $count{$b} <=> $count{$a} || $a cmp $b } @present) { $i++; print "$i\t$rail{$s}\t$count{$s}\t$s\n"; }
    print "MISSING\t$rail{$_}\t-\t$_ (not in this table)\n" for @missing;
    exit(@missing ? 1 : 0);
}
die "no visible draws for '$stem' in $tsv\n" unless @rows;
print "font\tmode\twidth\ttext\n";
print join("\t", @$_), "\n" for @rows;
printf STDERR "%s: %d visible text draws at rest\n", $stem, scalar @rows;
