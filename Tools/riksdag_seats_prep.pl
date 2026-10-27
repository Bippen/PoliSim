#!/usr/bin/perl
# SWEDEN'S FIXED CONSTITUENCY SEATS AND THE 2022 SEAT TABLE FOR THE LIVE COUNT (GATE-18 piece three, q3: "wire Sweden's 12 % route and per-valkrets
# seats into the live count"). Reads, byte-exact as saved under ElectionsData/sweden/2026/raw/ (each checked against its line in the folder's SHA256SUMS.results.txt):
#   fordelning-av-mandat-2022-och-2026.xlsx   Valmyndigheten's workbook of the fixed seats per valkrets, "Fasta mandat 2022" and "Fasta mandat 2026"
#                                             (the rows "Val till Riksdagen"; vallagen 4 kap. 3 par.: decided by 30 April of the election year)
#   resultat_val2026_RD_NN_S.json, NN 01..29   each valkrets's seats per party: partiMandat.antalMandat (2026) and antalMandatForegaendeVal (2022)
# and cross-checks them against ElectionsData/sweden/2026/valkrets_seats_2026.csv (the 2026 seat table of the decision Dnr VAL-735-2026, its fixed and
# party columns) - every cell, or it dies. Writes
#   Assets/Scripts/Elections/Generated/SwedishFixedSeats.cs   the 29 valkretsar (valkrets_votes_2026.csv's row order) and their fixed seats of each
#                                                              decision, the workbook's SHA-256 in its header
#   ElectionsData/sweden/valkrets_seats_2022.csv               the 2022 Riksdag's seats per valkrets and party (fixed and adjustment together - the
#                                                              JSON carries no split for the previous election) with the 2022 fixed seats
# SwedishRiksdagAllocationDiagnostic re-reads every source and compares every figure.
# Usage: perl Tools/riksdag_seats_prep.pl   (from the project root). ASCII only; the names are carried as the sources' own UTF-8 bytes.
use strict; use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw(unzip $UnzipError);

my $raw = 'ElectionsData/sweden/2026/raw';
my $xlsx = "$raw/fordelning-av-mandat-2022-och-2026.xlsx";
my $votes2026 = 'ElectionsData/sweden/2026/valkrets_votes_2026.csv';
my $seats2026 = 'ElectionsData/sweden/2026/valkrets_seats_2026.csv';
my $outCs = 'Assets/Scripts/Elections/Generated/SwedishFixedSeats.cs';
my $outCsv = 'ElectionsData/sweden/valkrets_seats_2022.csv';
my @parties = qw(S M SD C V KD L MP);   # the order of every Swedish valkrets file in the tree

sub slurp { my ($p) = @_; open my $h, '<:raw', $p or die "$p: $!\n"; local $/; my $b = <$h>; close $h; return $b; }

# --- the workbook: the shared strings and the sheet whose header carries both columns
my $xbytes = slurp($xlsx);
my $xDigest = sha256_hex($xbytes);
sub member { my ($name) = @_; my $out; unzip(\$xbytes => \$out, Name => $name) or die "$xlsx: $name: $UnzipError\n"; return $out; }
my $sst = member('xl/sharedStrings.xml');
my @ss;
while ($sst =~ m{<si>(.*?)</si>}sg) { my $x = $1; my $t = ''; while ($x =~ m{<t[^>]*>(.*?)</t>}sg) { $t .= $1; } $t =~ s/&amp;/&/g; push @ss, $t; }
my (%fixed2022, %fixed2026, $sheetFound);
for my $n (1 .. 9) {
    my $sheet; eval { $sheet = member("xl/worksheets/sheet$n.xml"); 1 } or last;
    my (%col, @rows);
    while ($sheet =~ m{<row r="(\d+)"[^>]*>(.*?)</row>}sg) {
        my ($r, $cells) = ($1, $2);
        my %row;
        while ($cells =~ m{<c r="([A-Z]+)\d+"([^>]*?)(?:/>|>(.*?)</c>)}sg) {
            my ($c, $attr, $in) = ($1, $2, defined $3 ? $3 : '');
            my $v = $in =~ m{<v>(.*?)</v>} ? $1 : '';
            $v = $ss[$v] if $attr =~ /t="s"/;
            $row{$c} = $v;
        }
        push @rows, \%row;
    }
    next unless @rows;
    my %head = map { $rows[0]{$_} => $_ } keys %{ $rows[0] };
    next unless exists $head{'Fasta mandat 2022'} && exists $head{'Fasta mandat 2026'} && exists $head{'Valkrets'} && exists $head{'Valtyp'};
    $sheetFound = 1;
    for my $row (@rows[1 .. $#rows]) {
        next unless ($row->{ $head{'Valtyp'} } // '') eq 'Val till Riksdagen';
        my $name = $row->{ $head{'Valkrets'} };
        my ($a, $b) = ($row->{ $head{'Fasta mandat 2022'} }, $row->{ $head{'Fasta mandat 2026'} });
        die "not a whole number for $name: $a / $b\n" unless $a =~ /^\d+$/ && $b =~ /^\d+$/;
        die "$name twice in the workbook\n" if exists $fixed2022{$name};
        ($fixed2022{$name}, $fixed2026{$name}) = ($a, $b);
    }
}
die "$xlsx: no sheet carries Valtyp, Valkrets, Fasta mandat 2022 and Fasta mandat 2026\n" unless $sheetFound;
die "29 Riksdag valkretsar expected in the workbook, " . scalar(keys %fixed2022) . " read\n" unless keys(%fixed2022) == 29;
for my $y ([2022, \%fixed2022], [2026, \%fixed2026]) { my $s = 0; $s += $_ for values %{ $y->[1] }; die "the $y->[0] fixed seats sum to $s, not 310\n" unless $s == 310; }

# --- the count's order: valkrets_votes_2026.csv's rows
my @names;
for my $line (split /\r?\n/, slurp($votes2026)) { next if $line eq '' || $line =~ /^#/ || $line =~ /^valkrets;/; push @names, (split /;/, $line)[0]; }
die "29 valkretsar expected in $votes2026, " . scalar(@names) . " read\n" unless @names == 29;
for my $n (@names) { die "$n is not in the workbook\n" unless exists $fixed2022{$n}; }

# --- the JSONs: each valkrets's seats per party, 2026 and 2022
my (%seat2026, %seat2022, @jsonDigests);
for my $i (1 .. 29) {
    my $f = sprintf('%s/resultat_val2026_RD_%02d_S.json', $raw, $i);
    my $b = slurp($f);
    push @jsonDigests, sha256_hex($b);
    $b =~ /"valdatum":"2026-09-13","tidigareValdatum":"2022-09-11","namn":"([^"]+)"/ or die "$f: no valkrets name after the two polling days\n";
    my $name = $1;
    die "$f: $name is not in the workbook\n" unless exists $fixed2022{$name};
    my $i0 = index($b, '"partiMandat":[') ; die "$f: no partiMandat\n" if $i0 < 0;
    my $block = substr($b, $i0, index($b, ']', $i0) - $i0);
    my %got;
    while ($block =~ /"partiforkortning":"([^"]+)","partikod":"[^"]*","antalMandat":(\d+),"antalMandatForegaendeVal":(\d+)/g) { $got{$1} = [$2, $3]; }
    for my $p (@parties) { die "$f: no seats for $p\n" unless $got{$p}; $seat2026{$name}{$p} = $got{$p}[0]; $seat2022{$name}{$p} = $got{$p}[1]; }
    die "$f: a party outside the eight holds a seat\n" if grep { my $k = $_; !grep { $_ eq $k } @parties } grep { $got{$_}[0] + $got{$_}[1] > 0 } keys %got;
}
die "29 JSONs, " . scalar(keys %seat2026) . " valkretsar\n" unless keys(%seat2026) == 29;

# --- every source against the folder's own digest sheet (the review's L-6)
my %sums;
for my $line (split /\r?\n/, slurp("$raw/SHA256SUMS.results.txt")) { $sums{$2} = lc $1 if $line =~ /^([0-9a-fA-F]{64}) \*?(\S+)$/; }
my $wb = 'fordelning-av-mandat-2022-och-2026.xlsx';
die "$raw/SHA256SUMS.results.txt: $wb is $xDigest, the sheet says " . ($sums{$wb} // 'nothing') . "\n" unless ($sums{$wb} // '') eq $xDigest;
for my $i (1 .. 29) {
    my $name = sprintf('resultat_val2026_RD_%02d_S.json', $i);
    die "$raw/SHA256SUMS.results.txt: $name is $jsonDigests[$i - 1], the sheet says " . ($sums{$name} // 'nothing') . "\n" unless ($sums{$name} // '') eq $jsonDigests[$i - 1];
}

# --- the cross-check against the 2026 seat table of the decision
my (@sHead, $checked);
for my $line (split /\r?\n/, slurp($seats2026)) {
    next if $line eq '' || $line =~ /^#/;
    my @f = split /;/, $line;
    if (!@sHead) { @sHead = @f; next; }
    my %c; @c{@sHead} = @f;
    my $n = $c{valkrets};
    die "$seats2026: $n fixed $c{fixed}, the workbook's 2026 column $fixed2026{$n}\n" unless $c{fixed} == $fixed2026{$n};
    for my $p (@parties) { die "$seats2026: $n $p $c{$p}, the JSON's $seat2026{$n}{$p}\n" unless $c{$p} == $seat2026{$n}{$p}; }
    $checked++;
}
die "$seats2026: $checked of 29 valkretsar checked\n" unless $checked == 29;
my %nat2022; for my $n (@names) { $nat2022{$_} += $seat2022{$n}{$_} for @parties; }
my $sum2022 = 0; $sum2022 += $nat2022{$_} for @parties;
die "the 2022 seats sum to $sum2022, not 349\n" unless $sum2022 == 349;

# --- the 2022 seat table
my $natLine = join('; ', map { "$_ $nat2022{$_}" } @parties);
my @csv = (
    "# Sweden Riksdag 2022 - seats per valkrets and party, fixed + adjustment together [SOURCED]",
    "# source: Valmyndigheten's 2026 result files ElectionsData/sweden/2026/raw/resultat_val2026_RD_NN_S.json (NN 01..29, byte-exact, digests in",
    "#   raw/SHA256SUMS.results.txt): partiMandat.antalMandatForegaendeVal, each file's seats per party at its previous election (tidigareValdatum",
    "#   2022-09-11); 'fixed' = the column 'Fasta mandat 2022' of raw/fordelning-av-mandat-2022-och-2026.xlsx (Valmyndigheten, the rows 'Val till Riksdagen').",
    "# WRITTEN BY Tools/riksdag_seats_prep.pl, never by hand. The same 29 units and row order as valkrets_votes_2022.csv / valkrets_votes_2026.csv.",
    "# VERIFIED by the tool: fixed sums to 310 and the seats to 349; per party $natLine.",
    "# The previous-election fields carry no split into fixed and adjustment seats, so none is given here.",
    'valkrets;fixed;total;' . join(';', @parties),
);
for my $n (@names) { my $t = 0; $t += $seat2022{$n}{$_} for @parties; push @csv, join(';', $n, $fixed2022{$n}, $t, map { $seat2022{$n}{$_} } @parties); }
open my $o, '>:raw', $outCsv or die "$outCsv: $!\n"; print $o join("\r\n", @csv), "\r\n"; close $o;

# --- the runtime table
my $nameList = join("\r\n", map { "            \"$_\"," } @names);
my $f22 = join(', ', map { $fixed2022{$_} } @names);
my $f26 = join(', ', map { $fixed2026{$_} } @names);
my @cs = (
    "// GENERATED by Tools/riksdag_seats_prep.pl. DO NOT EDIT BY HAND.",
    "//",
    "// Source : ElectionsData/sweden/2026/raw/fordelning-av-mandat-2022-och-2026.xlsx (Valmyndigheten; the rows 'Val till Riksdagen')",
    "// SHA-256: $xDigest",
    "//",
    "// SwedishRiksdagAllocationDiagnostic re-reads the workbook and compares every figure.",
    "",
    "namespace PoliSim.Elections.Generated",
    "{",
    "    /// <summary>GATE-18 piece three (q3): the fixed constituency seats of each of the 29 valkretsar as Valmyndigheten decided them for the Riksdag",
    "    /// elections of 2022 and 2026 (vallagen 4 kap. 3 \xC2\xA7: decided by 30 April of the election year, one seat per 310th part of the electorate) -",
    "    /// SOURCED, generated, never hand-edited. Indexed as <c>Names</c>.</summary>",
    "    public static class SwedishFixedSeats",
    "    {",
    "        public const string SourceDigest = \"$xDigest\";",
    "",
    "        /// <summary>The valkretsar, in the row order of the count's catalog file.</summary>",
    "        public static readonly string[] Names =",
    "        {",
    $nameList,
    "        };",
    "",
    "        /// <summary>The decision for the election of 2022 ('Fasta mandat 2022').</summary>",
    "        public static readonly int[] Fixed2022 = { $f22 };",
    "",
    "        /// <summary>The decision for the election of 2026 ('Fasta mandat 2026').</summary>",
    "        public static readonly int[] Fixed2026 = { $f26 };",
    "    }",
    "}",
);
open $o, '>:raw', $outCs or die "$outCs: $!\n"; print $o join("\r\n", @cs), "\r\n"; close $o;
print "wrote $outCs (workbook $xDigest) and $outCsv (2022: $natLine); 29 JSONs read, the 2026 table cross-checked cell by cell\n";
