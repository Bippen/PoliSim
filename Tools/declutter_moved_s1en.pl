#!/usr/bin/perl
# S1 (K320): writes docs/declutter/moved_s1en.tsv from the dry films' label tables and the sweep's mapping, and verifies every row:
# each segment of text_before (split as StatsSlipReachabilityCheck splits, figures folded) must be found in the slip's lines read together.
# Usage (from the project root): perl Tools/declutter_moved_s1en.pl docs/declutter/moved_s1en.tsv Assets/Scripts/UI/RangeCaptions.cs <labels.tsv>...
#   the labels are the six countries' 1280 dry-film tables the sweep was measured on (../PoliSim-captures/labels/, e.g. dryfisc2_sweden_1280_labels.tsv).
# It prints a verdict line; UNREACHED or LONG SLIP LINE rows are failures. The mapping below is the sweep's, kept beside EnergySlips by hand.
use strict; use warnings; use utf8;
binmode STDOUT, ':encoding(UTF-8)';
# RETIRED at §968 (K334): its output was converted to machine rows - moved_k334s1.tsv (§968) and moved_k334s1b.tsv (§971, scoped by country) - and moved_s1en.tsv is gone.
# Run again, this would write all 290 rows back without the marker - undoing the conversion and making the reader refuse the file. Kept for its
# record (§957); both files are maintained by hand from here, converted as the readers allow.
die "declutter_moved_s1en.pl is RETIRED (K334, \x{a7}968): moved_k334s1.tsv and moved_k334s1b.tsv are maintained by hand - see this file's header.\n";
my ($out, $captions, @tables) = @ARGV;
my $sep = ' | ';
# ---- the five Energy dials' bands, read off RangeCaptions.cs ----
my %band;   # line -> [[dial, name], ...]
{
    open my $h, '<:encoding(UTF-8)', $captions or die "$captions: $!";
    my $dial;
    while (my $l = <$h>) {
        if ($l =~ /\{\s*"([^"]+)",\s*new Dial\(/) { $dial = $1; next; }
        next unless defined $dial && $dial =~ /^(Retail intervention|Market liberalisation|Investment planning|State ownership|Subsidy)$/;
        if ($l =~ /B\("([^"]+)",\s*"([^"]+)",/) { push @{ $band{$2} }, [$dial, $1]; }
    }
}
sub counted0 { my @t = grep { /\p{L}/ } split /[ \x{00B7}\/]+/, shift; return scalar @t; }
sub frag
{
    # RangeCaptions.BandSlip's rule: each sentence a line without its stop; one past eight counted words split at its first ';' or ','
    my $line = shift; my @out;
    for my $s (split /\. /, $line) {
        $s =~ s/^\s+|\s+$//g; $s =~ s/\.$//; next unless length $s;
        my $cut = counted0($s) > 8 ? do { my @p = grep { $_ >= 0 } (index($s, ';'), index($s, ',')); @p ? (sort { $a <=> $b } @p)[0] : -1 } : -1;
        if ($cut > 0 && $cut < length($s) - 1) { my ($a, $b) = (substr($s, 0, $cut), substr($s, $cut + 1)); $a =~ s/^\s+|\s+$//g; $b =~ s/^\s+|\s+$//g; push @out, uc $a, uc $b; }
        else { push @out, uc $s; }
    }
    return @out;
}
# ---- the mapping: [regex on the at-rest text, slip id, sub returning the slip lines] ----
my @map = (
    [qr/^Billed · reached only through secondary…$/, 'en:target', sub { ('BILLED · REACHED ONLY THROUGH SECONDARY REPORTS') }],
    [qr/^Wind 145 gw · solar 215 gw in 2030 · coal…$/, 'en:target', sub { ('WIND 145 GW · SOLAR 215 GW IN 2030 · COAL OUT BY 2038') }],
    [qr/^(Wind 28\.1 gw · solar 79\.2 gw in 2030|Renewables 40 % of production in 2030|Fossil-free electricity by 2040|None · nothing to answer)$/, 'en:target', sub { (uc $1) }],
    [qr/^WHY THE PRICE IS WHAT IT IS$/, 'en:why', sub { ('WHY THE PRICE IS WHAT IT IS') }],
    [qr/^WHAT ELSE REACHES IT$/, 'en:reaches', sub { ('WHAT ELSE REACHES IT') }],
    [qr/^The rule on the price$/, 'en:why:rule', sub { ('THE RULE ON THE PRICE') }],
    [qr/^(ONE DEVICE · THE FORM IS IN THE PARTS' NAMES|AND IN THE CAPTION THAT SAYS WHAT PRICED IT)$/, 'en:why:rule', sub { ($1) }],
    [qr/^(\d+\.\d) = Σ PRICE × ENERGY ⁄ Σ ENERGY, (THREE|TWELVE) BLOCKS · ENERGY = SEED MW × HOURS PER BLOCK · THE PEAK BLOCK FILLS ITS LANE$/, 'en:why:derivation',
        sub { ("$1 = Σ PRICE × ENERGY ⁄ Σ ENERGY, $2 BLOCKS", 'ENERGY = SEED MW × HOURS PER BLOCK', 'THE PEAK BLOCK FILLS ITS LANE') }],
    [qr/^LINKS IN THE GUTTERS THEY JOIN · PEAK FLOW ⁄ NTC$/, 'en:why:zones', sub { ('LINKS IN THE GUTTERS THEY JOIN', 'PEAK FLOW ⁄ NTC') }],
    [qr/^ONE ZONE · THE REAL MARKET HAS SEVEN: NORD · CNOR · CSUD · SUD · CALA · SICI · SARD$/, 'en:why:zones', sub { ('ONE ZONE · THE REAL MARKET HAS SEVEN', 'NORD · CNOR · CSUD · SUD · CALA · SICI · SARD') }],
    [qr/^ONE ZONE · THE REAL GRID IS THREE INTERCONNECTIONS: EASTERN · WESTERN · ERCOT$/, 'en:why:zones', sub { ('ONE ZONE · THE REAL GRID IS THREE INTERCONNECTIONS', 'EASTERN · WESTERN · ERCOT') }],
    [qr/^ONE ZONE · THE NEIGHBOURS ARE EXOGENOUS$/, 'en:why:zones', sub { ('ONE ZONE · THE NEIGHBOURS ARE EXOGENOUS') }],
    [qr/^THE LOAD DOES NOT GROW WITH GDP OR ELECTRIFICATION · STATED, NOT MODELLED$/, 'en:why:load', sub { ('THE LOAD DOES NOT GROW WITH GDP OR ELECTRIFICATION', 'STATED, NOT MODELLED') }],
    [qr/^(EUR PER MWh) · PER BLOCK · UNDER THE RULE ROW'S THREE BLOCKS$/, 'en:why:water', sub { ("$1 · PER BLOCK", "UNDER THE RULE ROW'S THREE BLOCKS") }],
    [qr/^THE HYDRO SHIFT'S FOLD IS BILLED FOR (\w+) · NO RESERVOIR MODELLED$/, 'en:why:water', sub { ("THE HYDRO SHIFT'S FOLD IS BILLED FOR $1", 'NO RESERVOIR MODELLED') }],
    [qr/^NO RESERVOIR MODELLED FOR (\w+) · SWEDEN'S IS THE ONE$/, 'en:why:water', sub { ("NO RESERVOIR MODELLED FOR $1", "SWEDEN'S IS THE ONE") }],
    [qr/^Water value and reservoirs$/, 'en:why:water', sub { ('WATER VALUE AND RESERVOIRS') }],
    [qr/^(USD) BN · PAID, THEN RECEIVED · ONE SCALE WITH THE ROW ABOVE$/, 'en:why:incidence', sub { ("$1 BN · PAID, THEN RECEIVED", 'ONE SCALE WITH THE ROW ABOVE') }],
    [qr/^THE SAME ([\d.,]+) - WHAT THE MARKET COST IS WHAT THE GENERATORS RECEIVED$/, 'en:why:leader', sub { ("THE SAME $1", 'WHAT THE MARKET COST IS WHAT THE GENERATORS RECEIVED') }],
    [qr/^THE ROW IS ZERO BY THE CATALOG · A FEDERAL CARBON PRICE DOES NOT EXIST$/, 'en:reaches:ets', sub { ('THE ROW IS ZERO BY THE CATALOG', 'A FEDERAL CARBON PRICE DOES NOT EXIST') }],
    [qr/^THE CLIMATE FUND SITS OFF THE BUDGET · THE SUBSIDY DIAL'S COST STAYS WITH THE OTHER SECTORS' SUPPORT$/, 'en:reaches:retail', sub { ('THE CLIMATE FUND SITS OFF THE BUDGET', "THE SUBSIDY DIAL'S COST STAYS WITH THE OTHER SECTORS' SUPPORT") }],
    [qr/^THE RETAIL COMPONENTS ARE BILLED · THE SUBSIDY'S COST LANDS ON THE ENERGY LINE WITH NO RETAIL EFFECT$/, 'en:reaches:retail', sub { ('THE RETAIL COMPONENTS ARE BILLED', "THE SUBSIDY'S COST LANDS ON THE ENERGY LINE", 'WITH NO RETAIL EFFECT') }],
    [qr/^THE DIAL REACHES NO ORDER - THE FLEET MOVES BY THE CONNECTION QUEUE ABOVE, UNPRICED · THE DIAL'S COST STAYS WITH THE OTHER SECTORS' SUPPORT$/, 'en:reaches:planning',
        sub { ('THE DIAL REACHES NO ORDER', 'THE FLEET MOVES BY THE CONNECTION QUEUE ABOVE, UNPRICED', "THE DIAL'S COST STAYS WITH THE OTHER SECTORS' SUPPORT") }],
    [qr/^A STATE SHARE OF THE GENERATORS' RECEIPTS WOULD READ THE RENT THE PAGE PRINTS AND NOTHING READS · THE DIAL STILL MOVES THE SECTOR'S OUTPUT AND EMPLOYMENT$/, 'en:reaches:ownership',
        sub { ("A STATE SHARE OF THE GENERATORS' RECEIPTS", 'WOULD READ THE RENT THE PAGE PRINTS', 'AND NOTHING READS', "THE DIAL STILL MOVES THE SECTOR'S OUTPUT AND EMPLOYMENT") }],
    [qr/^THE STATES LEVY THEIR OWN GROSS-RECEIPTS TAXES · NO NATIONAL STATUTE FOR THIS HOUSE TO MOVE$/, 'en:reaches:electax', sub { ('THE STATES LEVY THEIR OWN GROSS-RECEIPTS TAXES', 'NO NATIONAL STATUTE FOR THIS HOUSE TO MOVE') }],
    [qr/^OPENS LAWS ON THIS CATEGORY · (\d+ OFFERED · \d+ ENACTED · \d+ BEFORE \w+) · THE ONE CATEGORY THAT REACHES THE ENERGY LAYER$/, 'en:lawslink',
        sub { ('OPENS LAWS ON THIS CATEGORY', $1, 'THE ONE CATEGORY THAT REACHES THE ENERGY LAYER') }],
    [qr/^NO NATIONAL ELECTRICITY EXCISE FOR (UNITED STATES) · THE LAWS ARE NOT OFFERED$/, 'en:lawslink', sub { ("NO NATIONAL ELECTRICITY EXCISE FOR $1", 'THE LAWS ARE NOT OFFERED') }],
    [qr/^THE ELECTRICITY TAX'S LAWS ▸$/, 'en:lawslink', sub { ("THE ELECTRICITY TAX'S LAWS ▸") }],
    [qr/^No Economic Sectors bill before (Parliament|Congress) - these dials travel in it, one draft with the Sectors page\.$/, 'en:sectorsbill:status',
        sub { ('NO ECONOMIC SECTORS BILL BEFORE ' . uc $1, 'THESE DIALS TRAVEL IN IT', 'ONE DRAFT WITH THE SECTORS PAGE') }],
    [qr/^(IN OPPOSITION|IN SUPPORT|JUNIOR PARTNER) · (THE GOVERNMENT INTRODUCES BILLS) · (YOUR PARTY VOTES ON THEM)$/, 'en:sectorsbill', sub { ($1, $2, $3) }],
);
sub lookup
{
    my $text = shift;
    for my $m (@map) { if ($text =~ $m->[0]) { return [$m->[1], [$m->[2]->()]]; } }
    return undef;
}
# ---- the label tables: every non-provenance Energy capture's draws (the Laws page the link reaches is the Laws screen's, S4) ----
my (%rows, @order, %whole);
my @draws;
for my $f (@tables) {
    open my $h, '<:encoding(UTF-8)', $f or die "$f: $!";
    <$h>;
    while (my $l = <$h>) {
        chomp $l; $l =~ s/\r$//;
        my ($cap, $scr, $x, $y, $w, $hg, $font, $mode, $nw, $nh, $flags, $text) = split /\t/, $l, 12;
        next unless defined $text && $cap =~ /^08_/ && $cap !~ /provenance|laws_link_arrived/;
        push @draws, [$cap, $flags // '', $text];
        $whole{$text} = 1 if lookup($text);
    }
}
for my $d (@draws) {
    my ($cap, $flags, $text) = @$d;
    my $hit = lookup($text);
    if (!$hit && $cap =~ /_dragged$/ && $band{$text}) {
        my @b = @{ $band{$text} };
        my %dials = map { $_->[0] => 1 } @b;
        die "band line in several Energy dials: $text" if keys %dials > 1;
        $hit = ["dial:$b[0][0]:band", [frag($text)], uc $b[0][1]];
    }
    if (!$hit && $flags =~ /OUTSIDE/ && length $text >= 20) {
        # an OUTSIDE draw's text is clipped in the table: matched on its prefix against the whole texts drawn elsewhere
        for my $full (sort keys %whole) { if (index($full, $text) == 0) { $hit = lookup($full); $text = $full; last; } }
    }
    next unless $hit;
    my $key = "$cap\t$text";
    next if $rows{$key};
    $rows{$key} = $hit; push @order, $key;
}
# ---- verify and write ----
sub norm { my $s = uc shift; $s =~ s/[–—−]/-/g; $s =~ s/\d+(?:[.,]\d+)*/#/g; $s =~ s/\s+/ /g; $s =~ s/^ | $//g; return $s; }
sub counted { my @t = grep { /\p{L}/ } split /[\s\x{00B7}\/]+/, shift; return scalar @t; }
my ($bad, $joined, $long) = (0, 0, 0);
my %texts;
open my $o, '>:encoding(UTF-8)', $out or die;
print $o "capture_stem\ttext_before\tnow_in\tslip_text\n";
for my $key (sort { $a cmp $b } @order) {
    my ($cap, $text) = split /\t/, $key, 2;
    $texts{$text} = 1;
    my ($now, $lines, $head) = @{ $rows{$key} };
    my @lines = @$lines;
    my @slip = defined $head ? ($head, @lines) : @lines;
    my $nl = join("\n", map { norm($_) } @slip);
    my $sp = join(' ', map { norm($_) } @slip);
    (my $t = $text) =~ s/(…|\.\.\.)\s*$//;   # a CUT draw: the slip holds the uncut text
    for my $seg (split / · | - |: |; |\. |, /, norm($t)) {
        $seg =~ s/^\s+|\s+$//g; $seg =~ s/[.,]$//;
        next if $seg eq '' || $seg =~ /^[#\s\-+%.,()·\/]*$/;
        if (index($nl, $seg) >= 0) { next; }
        if (index($sp, $seg) >= 0) { $joined++; print "JOINED     $cap  '$seg'\n"; next; }
        $bad++; print "UNREACHED  $cap  '$seg'  (from '$text')\n";
    }
    for my $line (@lines) { if (counted($line) > 10) { $long++; print "LONG SLIP LINE  $line\n"; } }
    if (@lines > 4) { $long++; print "MORE THAN 4 LINES  $now\n"; }
    print $o join("\t", $cap, $text, $now, join($sep, @slip)), "\n";
}
close $o;
printf "%d rows, %d distinct texts; %d segments unreached, %d found only with the slip's lines read together, %d slip-cap breaches\n",
    scalar @order, scalar(keys %texts), $bad, $joined, $long;
