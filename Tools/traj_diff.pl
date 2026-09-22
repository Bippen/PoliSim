use strict; use warnings;
# THE FAMILY DIFF (2026-09-22, COMPLETED.md s577; under Tools/ by the standing rule of s576 - every BASELINE pass needs it).
#   perl Tools/traj_diff.pl <old-label> <new-label> [seed] [turns]      e.g. perl Tools/traj_diff.pl p6pap2 pn3b 777 100
# Reads ../PoliSim-captures/trajectories/traj_<label>_s<seed>_t<turns>.csv (turn,country,field,value) and prints, per country:
# how many of the fields moved, and every moved field's last-turn figure old -> new with the per-cent change, largest first.
# It states what it cannot know: a field absent from either file is listed as absent, never as unmoved.
my ($oldLabel, $newLabel, $seed, $turns) = @ARGV;
die "usage: traj_diff.pl <old-label> <new-label> [seed] [turns]\n" unless $oldLabel && $newLabel;
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
