#!/usr/bin/perl
# THE ELECTIONS-BACKTESTS REPORT, FILTERED FOR A DIFF (PS-6 US-5, COMPLETED.md s788). An item that touches the vote or seat models owes an
# `elec<N>` diff: the launch (`Tools/unity_run.ps1 -Method PoliSim.EditorTools.ElectionsBacktests.RunBatch -Label elec<N>a|b`) run before
# and after it, the two reports compared line by line. This prints a log's report lines: from the first harness line (VOTEBACKTEST:) to the
# batch's result line (ELECTIONS BACKTESTS:), less what moves from run to run without the models moving - Unity's stack-trace lines (they
# carry source line numbers, so any edit to a harness moves them), the (Filename:) lines, blank lines, and the per-run timing, source,
# epoch, remap, import and memory lines.
# Usage: perl Tools/elec_report.pl <log> > <report>; then diff the two reports. The sessions before this file used their own filters,
# whose saved .report extracts held no GateReRun line - a diff through them could not see a change there.
# Proved on 2026-10-06: elec709b (2026-10-01) against elec788a moved only s786's USA Electoral College lines (SeatAllocationBacktest).
use strict;
use warnings;
my $on = 0;
while (my $line = <>) {
    $line =~ s/\r?\n$//;
    $on = 1 if $line =~ /^VOTEBACKTEST: /;
    next unless $on;
    next if $line =~ /^\s*$/;
    next if $line =~ /^(?:UnityEngine\.|PoliSim\.|System\.|Mono\.|\(Filename: )/;
    next if $line =~ /^(?:TIMING:|SOURCE:|EPOCH:|MonoPathRemapper:|\[MODES\]|Unloading |Memory consumption|Total: |Asset Pipeline|Refresh|Worker ready|Begin MonoManager|Domain Reload)/;
    print "$line\n";
    last if $line =~ /^ELECTIONS BACKTESTS: /;
}
die "no report: no VOTEBACKTEST: line in the input\n" unless $on;
