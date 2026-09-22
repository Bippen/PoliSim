use strict; use warnings;
# Every Unity launch in a window, with its WALL (the runner's own EXIT line) and its WORK (what the method reported),
# so startup+shutdown is the difference. Sources: PoliSim-captures/logs/step_*.out, film_*.out, dry_*.out and the matching *.log.
my ($from, $to) = @ARGV;
my $dir = 'G:/UNITY/Projects/PoliSim-captures/logs';
opendir(my $d, $dir) or die; my @outs = grep { /^(step|film|dry)_.*\.out$/ } readdir $d; closedir $d;
my @rows;
for my $o (sort @outs) {
  my $path = "$dir/$o";
  my @st = stat $path; my $mtime = $st[9];
  my @t = localtime($mtime); my $iso = sprintf("%04d-%02d-%02dT%02d:%02d", $t[5]+1900, $t[4]+1, $t[3], $t[2], $t[1]);
  next if defined $from && $iso lt $from; next if defined $to && $iso gt $to;
  open my $h, '<:raw', $path or next; local $/; my $s = <$h>; close $h;
  my ($label) = $o =~ /^(?:step|film|dry)_(.+)\.out$/;
  # the runner prints: EXIT <label> <code> wall <n> s   (step.ps1) or FILM/DRY exit lines
  my @walls = $s =~ /EXIT \S+ \d+ wall (\d+) s/g;
  push @walls, $s =~ /wall (\d+) s/g unless @walls;
  my $wall = 0; $wall += $_ for @walls;
  # the work the method reported
  my $work = 0; my $kind = 'other';
  if ($s =~ /TIMING: cheap total ([\d.,]+) s/) { my $v = $1; $v =~ s/,/./; $work = $v; $kind = 'cheap bar'; }
  elsif ($s =~ /TIMING: simulation total ([\d.,]+) s/) { my $v = $1; $v =~ s/,/./; $work = $v; $kind = 'sim bar'; }
  elsif ($s =~ /DRY done|DRY \S+ done/) { $kind = 'dry film'; my @secs = $s =~ /\((\d+[.,]\d+) s\)/g; for (@secs) { my $v = $_; $v =~ s/,/./; $work += $v; } }
  elsif ($o =~ /^film_/) { $kind = 'film'; }
  elsif ($s =~ /CHECKS: \d+ of \d+ clean/ && $o =~ /doc/) { $kind = 'document bar'; }
  elsif ($s =~ /CHECKS: /) { $kind = 'a check alone'; }
  push @rows, [$iso, $label, $kind, $wall, $work, scalar @walls];
}
my (%n, %w, %k);
printf "%-16s %-22s %-14s %6s %6s\n", 'when', 'label', 'kind', 'wall', 'work';
for my $r (sort { $a->[0] cmp $b->[0] } @rows) {
  printf "%-16s %-22s %-14s %6d %6.1f\n", @$r[0 .. 4];
  $n{$r->[2]} += ($r->[5] || 1); $w{$r->[2]} += $r->[3]; $k{$r->[2]} += $r->[4];
}
print "\n-- by kind: launches, wall min, work min, startup+shutdown min --\n";
my ($N, $W, $K) = (0, 0, 0);
for my $kind (sort { $w{$b} <=> $w{$a} } keys %w) {
  printf "  %-14s %3d launches  wall %6.1f  work %6.1f  overhead %6.1f\n", $kind, $n{$kind}, $w{$kind}/60, $k{$kind}/60, ($w{$kind} - $k{$kind})/60;
  $N += $n{$kind}; $W += $w{$kind}; $K += $k{$kind};
}
printf "  TOTAL          %3d launches  wall %6.1f  work %6.1f  overhead %6.1f\n", $N, $W/60, $K/60, ($W-$K)/60;
