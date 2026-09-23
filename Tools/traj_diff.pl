use strict; use warnings;
# THE FAMILY DIFF (2026-09-22, COMPLETED.md s577; under Tools/ by the standing rule of s576 - every BASELINE pass needs it).
#   perl Tools/traj_diff.pl <old-label> <new-label> [seed] [turns]      e.g. perl Tools/traj_diff.pl p6pap2 pn3b 777 100
# Reads ../PoliSim-captures/trajectories/traj_<label>_s<seed>_t<turns>.csv (turn,country,field,value) and prints, per country:
# how many of the fields moved, and every moved field's last-turn figure old -> new with the per-cent change, largest first.
# It states what it cannot know: a field absent from either file is listed as absent, never as unmoved.
# K-1 part (3) (2026-09-24, s604): a fifth argument 'first' prints, BEFORE the diff, where each country first diverges (the turn,
# and the fields that differ on it) and six headline fields at turns 1, 4, 20 and the last - the read a moved CALENDAR needs, where
# the last turn alone cannot say when a statute step landed. Without it the output is what it always was.
my ($oldLabel, $newLabel, $seed, $turns, $mode) = @ARGV;
die "usage: traj_diff.pl <old-label> <new-label> [seed] [turns] [first]\n" unless $oldLabel && $newLabel;
$seed ||= 777; $turns ||= 100;
my $dir = '../PoliSim-captures/trajectories';
sub read_last {
  my $label = shift;
  my $path = "$dir/traj_${label}_s${seed}_t${turns}.csv";
  open my $h, '<', $path or die "cannot read $path\n";
  my %last; my $maxTurn = 0;
  while (my $line = <$h>) {
    chomp $line;
    next if $line =~ /^turn,/;
    my ($t, $c, $f, $v) = split /,/, $line;
    next unless defined $v;
    $maxTurn = $t if $t > $maxTurn;
    $last{$c}{$f} = [$t, $v] if !exists $last{$c}{$f} || $t >= $last{$c}{$f}[0];
  }
  close $h;
  return (\%last, $maxTurn);
}
if (defined $mode && $mode eq 'first') {
  my %all;
  for my $label ($oldLabel, $newLabel) {
    my $path = "$dir/traj_${label}_s${seed}_t${turns}.csv";
    open my $h, '<', $path or die "cannot read $path\n";
    while (my $line = <$h>) { chomp $line; next if $line =~ /^turn,/; my ($t, $c, $f, $v) = split /,/, $line; next unless defined $v; $all{$label}{$c}{$f}{$t} = $v; }
    close $h;
  }
  my @headline = ('GDP', 'GovernmentDebt', 'Budget', 'LaborForceParticipationRate', 'Unemployment', 'Inflation');
  printf "FIRST DIVERGENCE  %s -> %s  (seed %s, %s turns)\n", $oldLabel, $newLabel, $seed, $turns;
  for my $c (sort keys %{$all{$newLabel}}) {
    my $first; my @fields; my @absent;
    for my $f (sort keys %{$all{$newLabel}{$c}}) {
      if (!exists $all{$oldLabel}{$c}{$f}) { push @absent, $f; next; }   # the tool's promise: absent is said, never counted as unmoved
      for my $t (sort { $a <=> $b } keys %{$all{$newLabel}{$c}{$f}}) {
        my $o = $all{$oldLabel}{$c}{$f}{$t}; my $n = $all{$newLabel}{$c}{$f}{$t};
        next if !defined $o || $o + 0 == $n + 0;
        if (!defined $first || $t < $first) { $first = $t; @fields = ($f); } elsif ($t == $first) { push @fields, $f; }
        last;
      }
    }
    printf "%-8s %s%s\n", $c, defined $first ? sprintf('first differs at turn %d in %d field(s): %s', $first, scalar @fields, join(', ', @fields[0 .. ($#fields < 5 ? $#fields : 5)]) . ($#fields > 5 ? ', ...' : '')) : 'identical at every turn',
      @absent ? sprintf(' (%d field(s) absent from %s: %s)', scalar @absent, $oldLabel, join(', ', @absent)) : '';
    for my $f (@headline) {
      next unless exists $all{$newLabel}{$c}{$f};
      my @cells;
      for my $t (1, 4, 20, $turns) {
        my $o = $all{$oldLabel}{$c}{$f}{$t}; my $n = $all{$newLabel}{$c}{$f}{$t};
        next unless defined $o && defined $n;
        push @cells, sprintf('t%d %s', $t, $o + 0 == $n + 0 ? '=' : sprintf('%.4f -> %.4f', $o, $n));
      }
      printf "    %-28s %s\n", $f, join('  ', @cells);
    }
  }
  print "\n";
}
my ($old, $oldTurn) = read_last($oldLabel);
my ($new, $newTurn) = read_last($newLabel);
printf "FAMILY DIFF  %s -> %s  (seed %s, %s turns; last turn read: %d and %d)\n\n", $oldLabel, $newLabel, $seed, $turns, $oldTurn, $newTurn;
my $movedTotal = 0; my $fieldsTotal = 0;
for my $c (sort keys %$new) {
  my @moved; my $fields = 0; my $absent = 0;
  for my $f (sort keys %{$new->{$c}}) {
    $fields++;
    if (!exists $old->{$c}{$f}) { $absent++; next; }
    my $a = $old->{$c}{$f}[1] + 0; my $b = $new->{$c}{$f}[1] + 0;
    next if $a == $b;
    my $pct = $a != 0 ? 100 * ($b - $a) / abs($a) : undef;
    push @moved, [$f, $a, $b, $pct];
  }
  $movedTotal += scalar @moved; $fieldsTotal += $fields;
  printf "%-8s %d of %d field(s) moved%s\n", $c, scalar @moved, $fields, $absent ? " ($absent absent from $oldLabel)" : '';
  for my $m (sort { (defined $b->[3] ? abs $b->[3] : 0) <=> (defined $a->[3] ? abs $a->[3] : 0) } @moved) {
    printf "    %-34s %14.4f -> %14.4f  %s\n", $m->[0], $m->[1], $m->[2], defined $m->[3] ? sprintf('%+.2f %%', $m->[3]) : 'from zero';
  }
  print "\n";
}
printf "TOTAL %d of %d field-readings moved.\n", $movedTotal, $fieldsTotal;
