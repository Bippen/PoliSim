use strict; use warnings;
# Rewrite the last cell (the status) of named ledger rows, leaving every line ending as it was.
my ($file, @pairs) = @ARGV;
my %status = @pairs;
open my $in, '<:raw', $file or die "$file: $!";
my @lines = <$in>;
close $in;
my %done;
for my $line (@lines) {
  next unless $line =~ /^\| (GA-[0-9]+-[0-9]+) \|/;
  my $id = $1;
  next unless exists $status{$id};
  my ($body, $eol) = $line =~ /^(.*?)(\r?\n)?\z/s;
  $eol //= '';
  $body =~ s/\| [^|]*\|\s*\z/| $status{$id} |/ or die "no status cell in $id";
  $line = $body . $eol;
  $done{$id} = 1;
}
for my $id (keys %status) { die "row $id not found" unless $done{$id}; }
open my $out, '>:raw', $file or die;
print $out @lines;
close $out;
print "updated ", scalar(keys %done), " row(s)\n";
