use strict; use warnings;
# Part 1: decompose a session's wall time from its transcript. For each entry we take the timestamp and the role;
# a TOOL RESULT's timestamp minus the assistant message that called it is TOOL time; the gap from a tool result to the
# next assistant message is MODEL time. Bash calls are classified by their command text.
my ($file, $from, $to) = @ARGV;   # from/to as ISO strings, optional
my (@ev);
open my $h, '<:raw', $file or die "$file: $!";
while (my $line = <$h>) {
  next unless $line =~ /"timestamp":"([^"]+)"/;
  my $ts = $1;
  next if defined $from && $ts lt $from;
  next if defined $to && $ts gt $to;
  my $type = $line =~ /"type":"assistant"/ ? 'a' : $line =~ /"type":"user"/ ? 'u' : '?';
  my $isTool = $line =~ /"tool_use_id"/ ? 1 : 0;
  my $cmd = '';
  if ($line =~ /"name":"Bash","input":\{"command":"((?:[^"\\]|\\.){0,400})/) { $cmd = $1; }
  elsif ($line =~ /"name":"(Read|Write|Edit|Glob|Grep|Task|TodoWrite|Agent)"/) { $cmd = "\L$1"; }
  push @ev, [$ts, $type, $isTool, $cmd];
}
close $h;
die "no events\n" unless @ev;
sub secs { my $t = shift; my ($Y,$M,$D,$h,$m,$s) = $t =~ /(\d+)-(\d+)-(\d+)T(\d+):(\d+):(\d+)/ or return 0;
  return ((($D * 24 + $h) * 60 + $m) * 60) + $s + ($M - 1) * 2678400; }
my $t0 = secs($ev[0][0]); my $t1 = secs($ev[-1][0]);
my ($model, $tool, %bucket) = (0, 0);
for my $i (1 .. $#ev) {
  my $dt = secs($ev[$i][0]) - secs($ev[$i - 1][0]);
  if ($dt < 0) { next; } if ($dt > 3600) { $bucket{"gap over an hour"} += $dt; next; }
  my ($prev, $cur) = ($ev[$i - 1], $ev[$i]);
  if ($cur->[1] eq 'u' && $cur->[2]) {           # a tool RESULT: the time since the call is the tool's
    $tool += $dt;
    my $c = $prev->[3] // '';
    my $b = $c =~ /chain_ui|chain_bars|chain_full|chain_matrix|step\.ps1|film\.ps1|dry\.ps1|Start-Process/ ? 'unity launch (fire)'
          : $c =~ /^until |sleep \d/ ? 'waiting on a run'
          : $c =~ /dotnet build/ ? 'dotnet compile'
          : $c =~ /^(read|write|edit|glob|grep|agent|task)$/ ? "file $c"
          : $c =~ /\bgit\b/ ? 'git'
          : $c =~ /perl |sed |awk / ? 'scripting'
          : 'other bash';
    $bucket{$b} += $dt;
  }
  elsif ($cur->[1] eq "a") { $model += $dt; $bucket{"model thinking/writing"} += $dt; }
  else { $bucket{"user turn"} += $dt; }
}
printf "window: %s .. %s\n", $ev[0][0], $ev[-1][0];
printf "wall: %.1f min over %d transcript events\n", ($t1 - $t0) / 60, scalar @ev;
printf "model (between tool results): %.1f min\n", $model / 60;
printf "tool  (call -> result):       %.1f min\n", $tool / 60;
print "\nby bucket (minutes):\n";
for my $b (sort { $bucket{$b} <=> $bucket{$a} } keys %bucket) { printf "  %-22s %6.1f\n", $b, $bucket{$b} / 60; }
