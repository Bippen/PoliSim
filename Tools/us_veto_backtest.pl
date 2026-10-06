#!/usr/bin/perl
# PS-6 US-10 - THE US VETO ON THE RECORD, IN E4'S FORM (VETO_B1_BACKTEST.md's): EACH CANDIDATE RULE FOR WHICH BILLS THE PRESIDENT VETOES, PER
# PRESIDENT - its base, the vetoes inside and outside it, the rate a draw would take - with the confusion matrix, the hit rate and the precision; and
# how many measures a party-bloc Congress under Rule XXII could have sent at all. Reads ElectionsData/usa/us_veto_measures.csv (Tools/us_veto_prep.pl)
# and, for B1's yardstick, ElectionsData/poland/third_readings_term10.csv; writes docs/generated/US_VETO_BACKTEST.md. R-US17 is asked on it.
# Usage (from the project root): perl Tools/us_veto_backtest.pl [md]
#
# THE RULES (each on a chamber's FINAL AGREEMENT; a final agreement with no roll call - voice vote, unanimous consent - names nothing):
#   (a) B1 transposed (R-US17 (a)): more than half the president's party's House MEMBERS on the roll call (not voting included) voted nay - B1's
#       own count (s757: the club's members, absent included);
#   F3 transposed: the president's party did not vote for it in the House - a majority of its VOTING members (yea, nay, present) voted nay or present
#       (F3's own count: the absent outside, an abstention against);
#   (b) either chamber (R-US17 (b)): F3's count in the House or in the Senate;
#   both chambers: F3's count in the House and in the Senate;
#   (c) fitted (R-US17 (c)): a family printed, never picked here - the House share of the president's party's voting members voting nay at or above t.
# The president's party is the party label (R for Trump, D for Biden); independents are outside it (a senator caucusing with it included in no count).
# A measure VETOED: vetoed, pocket vetoed, or vetoed and overridden; NOT VETOED: signed, or law without signature.
#
# THE BLOC CONGRESS (R-US17's risk, the plan's spec-risk 4): each chamber's two party blocs, R and D, each voting as its own majority voted on the
# final roll call (yea where its yeas exceed its nays and presents), each bloc carrying every member the roll call lists (not voting included);
# a senator of no party joins the bloc his caucus names (senate_seats.csv), a House independent stays outside both. A final agreement with no roll
# call passes under blocs (no one recorded against). The House passes on more bloc yeas than nays. The Senate: a measure under a statute's
# expedited procedure (5 U.S.C. 802 for a CRA disapproval; P.L. 94-329 s601(b) for War Powers and arms-sale disapprovals; 50 U.S.C. 1622 for a
# national emergency; D.C. Code 1-206.04 for a D.C. disapproval - each read, raw/vetoes/) passes on more bloc yeas than nays; any other measure
# needs cloture under Rule XXII, three fifths of the senators duly chosen and sworn (the roll call's members) voting yea. A tie is not broken (the
# Vice President is not modelled here). A VETOABLE measure is one the bloc Congress sends and the president's party bloc opposed in a chamber.
#
# THE READING OF "FAR MORE" (R-US17: "STOPS and re-asks if every rule names far more bills than the record vetoed"), DECLARED BEFORE THE FIRST RUN
# (G5, PROVISIONAL): a rule names far more bills than the record vetoed when its precision on the pooled US record is below B1's precision on
# Poland's whole term (recomputed here from the Sejm's CSV) - B1 was ruled at that precision, so a rule below it names more bills per veto caught.
use strict;
use warnings;
use utf8;
use Digest::SHA qw(sha256_hex);
binmode STDOUT, ':encoding(UTF-8)';

my $out = $ARGV[0] // 'docs/generated/US_VETO_BACKTEST.md';
my $CSV = 'ElectionsData/usa/us_veto_measures.csv';

sub read_csv {
    my ($path) = @_;
    open my $fh, '<:encoding(UTF-8)', $path or die "$path: $!";
    local $/;
    my $s = <$fh>;
    close $fh;
    my (@rows, @row);
    my @chars = split //, $s;   # an array, not substr: substr on a decoded UTF-8 string walks from the start each time
    my ($field, $inq, $i, $n) = ('', 0, 0, scalar @chars);
    while ($i < $n) {
        my $c = $chars[$i];
        if ($inq) {
            if ($c eq '"') { if ($i + 1 < $n && $chars[$i + 1] eq '"') { $field .= '"'; $i += 2; next; } $inq = 0; $i++; next; }
            die "$path: a field runs over a line\n" if $c eq "\n";
            $field .= $c; $i++; next;
        }
        if ($c eq '"') { die "$path: a quote inside an unquoted field\n" if length $field; $inq = 1; $i++; next; }
        if ($c eq ',') { push @row, $field; $field = ''; $i++; next; }
        if ($c eq "\r") { $i++; next; }
        if ($c eq "\n") { push @row, $field; push @rows, [@row]; @row = (); $field = ''; $i++; next; }
        $field .= $c; $i++;
    }
    push @row, $field if length($field) || @row;
    push @rows, [@row] if @row;
    die "$path: an unterminated quote\n" if $inq;
    @rows = grep { !(@$_ == 1 && $_->[0] eq '') && $_->[0] !~ /^#/ } @rows;
    my $head = shift @rows;
    my @recs;
    for my $r (@rows) { die "$path: a row of " . scalar(@$r) . " fields against " . scalar(@$head) . "\n" unless @$r == @$head; my %x; @x{@$head} = @$r; push @recs, \%x; }
    return \@recs;
}
sub digest_of { my ($path) = @_; open my $fh, '<:raw', $path or die "$path: $!"; local $/; my $b = <$fh>; close $fh; return sha256_hex($b); }
sub pct { my ($a, $b) = @_; return $b ? sprintf('%.1f %%', 100 * $a / $b) : '-'; }

# ------------------------------------------------------------------ B1's yardstick, from the Sejm's own CSV (s757, s775)
my ($b1Named, $b1Vetoed) = (0, 0);
for my $x (@{ read_csv('ElectionsData/poland/third_readings_term10.csv') }) {
    next if length $x->{veto_exempt};
    my $o = $x->{outcome};
    next unless $o eq 'vetoed' || $o =~ /^signed/ || $o =~ /^referred to Tribunal/;
    next unless 2 * $x->{pis_no} > $x->{pis_members};
    $b1Named++; $b1Vetoed++ if $o eq 'vetoed';
}
die "B1's yardstick: $b1Vetoed of $b1Named - VETO_B1_BACKTEST.md's whole term reads 45 of 166\n" unless $b1Named == 166 && $b1Vetoed == 45;
my $b1Precision = $b1Vetoed / $b1Named;

# ------------------------------------------------------------------ the measures and the rules
my $rows = read_csv($CSV);
my @PRES = ('Donald J. Trump (first term)', 'Joseph R. Biden Jr.', 'Donald J. Trump (second term)');
my %EXPEDITED = map { $_ => 1 } ('CRA disapproval', 'arms-sale disapproval', 'War Powers', 'national emergency', 'D.C. disapproval');

sub split_of { my ($x, $ch, $p) = @_; my @v = map { $x->{"${ch}_${p}_$_"} } qw(yea nay present not_voting); return undef if $v[0] eq '-'; return \@v; }
sub vetoed { $_[0]{outcome} =~ /veto/ ? 1 : 0 }
sub opposed_f3 { my ($x, $ch) = @_; my $s = split_of($x, $ch, $x->{president_party}) or return 0; return $s->[1] + $s->[2] > $s->[0] ? 1 : 0; }
my %RULE = (
    'a' => { name => '(a) B1 transposed - more than half the party\'s House members voted nay', named => sub { my $s = split_of($_[0], 'house', $_[0]{president_party}) or return 0; my $mem = $s->[0] + $s->[1] + $s->[2] + $s->[3]; return 2 * $s->[1] > $mem ? 1 : 0; } },
    'f3' => { name => 'F3 transposed - the party\'s House voting members mostly nay or present', named => sub { opposed_f3($_[0], 'house') } },
    'b' => { name => '(b) either chamber - F3\'s count in the House or the Senate', named => sub { opposed_f3($_[0], 'house') || opposed_f3($_[0], 'senate') } },
    'both' => { name => 'both chambers - F3\'s count in the House and the Senate', named => sub { opposed_f3($_[0], 'house') && opposed_f3($_[0], 'senate') } },
);
my @RULES = qw(a f3 b both);

# the bloc Congress
sub bloc_passes {
    my ($x, $ch) = @_;
    return (1, 'no roll call - unanimous under blocs') if $x->{"${ch}_final"} !~ /^roll /;
    my %seats = map { my $s = split_of($x, $ch, $_); ($_ => $s ? $s->[0] + $s->[1] + $s->[2] + $s->[3] : 0) } qw(R D I);
    my %dir = map { my $s = split_of($x, $ch, $_); ($_ => $s && $s->[0] > $s->[1] + $s->[2] ? 1 : 0) } qw(R D);
    my ($yes, $no) = (0, 0);
    for my $p (qw(R D)) { if ($dir{$p}) { $yes += $seats{$p}; } else { $no += $seats{$p}; } }
    if ($ch eq 'senate' && $x->{senate_I_caucus} ne '-') {
        for my $kv (split / /, $x->{senate_I_caucus}) { my ($c, $n) = split /:/, $kv; next unless $c eq 'R' || $c eq 'D'; if ($dir{$c}) { $yes += $n; } else { $no += $n; } }
    }
    my $sworn = $seats{R} + $seats{D} + $seats{I};
    if ($ch eq 'senate' && !$EXPEDITED{$x->{kind}}) {
        my $need = int((3 * $sworn + 4) / 5);
        return ($yes >= $need ? 1 : 0, "cloture: bloc yeas $yes of $sworn sworn, three fifths $need");
    }
    return ($yes > $no ? 1 : 0, sprintf('%s: bloc yeas %d, nays %d', $ch eq 'senate' ? 'expedited' : 'House', $yes, $no));
}
sub bloc_sends { my $x = shift; my ($h, $hw) = bloc_passes($x, 'house'); my ($s, $sw) = bloc_passes($x, 'senate'); return ($h && $s ? 1 : 0, "$hw; $sw"); }
sub party_bloc_opposed { my ($x, $ch) = @_; my $s = split_of($x, $ch, $x->{president_party}) or return 0; return $s->[0] > $s->[1] + $s->[2] ? 0 : 1; }

# ------------------------------------------------------------------ the counts
my (%n, %v, %named, %namedVetoed);
for my $x (@$rows) {
    my $p = $x->{president};
    die "$CSV: a president '$p'\n" unless grep { $_ eq $p } @PRES;
    for my $k ($p, 'pooled') {
        $n{$k}++; $v{$k} += vetoed($x);
        for my $r (@RULES) { if ($RULE{$r}{named}->($x)) { $named{$r}{$k}++; $namedVetoed{$r}{$k} += vetoed($x); } }
    }
}

my $md = '';
my $digest = digest_of($CSV);
$md .= "<!-- GENERATED by Tools/us_veto_backtest.pl from ElectionsData/usa/us_veto_measures.csv (sha256 $digest). DO NOT EDIT BY HAND. Re-derive: perl Tools/us_veto_backtest.pl -->\n";
$md .= "# The US veto on the record - which bills each president vetoed, each candidate rule's base and precision, and what a party-bloc Congress could send (R-US17)\n\n";
$md .= "Read on `ElectionsData/usa/us_veto_measures.csv` (`Tools/us_veto_prep.pl`: every public law and every vetoed measure of the 115th-119th Congresses decided from noon on 20 January 2017, each chamber's final agreement and its party split from the Clerk's and the Senate's roll calls; the record is `ElectionsData/usa/veto_record.md`). A rule reads the chamber's final agreement; one with no roll call (voice vote, unanimous consent) names nothing. VETOED: vetoed, pocket vetoed, or vetoed and overridden.\n\n";

$md .= "## The record\n\n| President | measures decided | vetoed | of them overridden | House final agreement by roll call | Senate final agreement by roll call |\n|---|---:|---:|---:|---:|---:|\n";
for my $k (@PRES, 'pooled') {
    my @r = grep { $k eq 'pooled' || $_->{president} eq $k } @$rows;
    my $ov = grep { $_->{outcome} eq 'vetoed, overridden' } @r;
    my $hr = grep { $_->{house_final} =~ /^roll / } @r; my $sr = grep { $_->{senate_final} =~ /^roll / } @r;
    $md .= sprintf("| %s | %d | %d | %d | %d | %d |\n", $k eq 'pooled' ? 'the three terms' : $k, scalar(@r), $v{$k} // 0, $ov, $hr, $sr);
}

$md .= "\n## Each rule: its base, the vetoes inside and outside it, the rate a draw would take\n\n";
$md .= "| Rule | President | named | vetoed among them (the rate) | vetoes outside it | named, not vetoed | hit rate | precision |\n|---|---|---:|---:|---:|---:|---:|---:|\n";
for my $r (@RULES) {
    for my $k (@PRES, 'pooled') {
        my $nm = $named{$r}{$k} // 0; my $nv = $namedVetoed{$r}{$k} // 0; my $vt = $v{$k} // 0;
        $md .= sprintf("| %s | %s | %d | %d (%s) | %d | %d | %s | %s |\n", $RULE{$r}{name}, $k eq 'pooled' ? 'the three terms' : $k, $nm, $nv, pct($nv, $nm), $vt - $nv, $nm - $nv, pct($nv, $vt), pct($nv, $nm));
    }
}

$md .= "\n## (c) a rule fitted on the record - the family, printed and not picked\n\nThe House share of the president's party's voting members voting nay at or above t, pooled over the three terms:\n\n| t | named | vetoed among them | hit rate | precision |\n|---:|---:|---:|---:|---:|\n";
for my $t (map { $_ / 10 } 1 .. 9) {
    my ($nm, $nv) = (0, 0);
    for my $x (@$rows) { my $s = split_of($x, 'house', $x->{president_party}) or next; my $vot = $s->[0] + $s->[1] + $s->[2]; next unless $vot && $s->[1] / $vot >= $t; $nm++; $nv += vetoed($x); }
    $md .= sprintf("| %.1f | %d | %d | %s | %s |\n", $t, $nm, $nv, pct($nv, $v{pooled}), pct($nv, $nm));
}

$md .= "\n## Every veto\n\n| President | measure | kind | outcome | the party's House split (yea-nay-present-not voting) | its Senate split | rules naming it | a bloc Congress sends it? |\n|---|---|---|---|---|---|---|---|\n";
my ($sendable, $vetoTotal) = (0, 0);
for my $x (grep { vetoed($_) } @$rows) {
    $vetoTotal++;
    my $hs = split_of($x, 'house', $x->{president_party}); my $ss = split_of($x, 'senate', $x->{president_party});
    my @by = map { $_ eq 'a' ? '(a)' : $_ eq 'f3' ? 'F3' : $_ eq 'b' ? '(b)' : 'both' } grep { $RULE{$_}{named}->($x) } @RULES;
    my ($ok, $why) = bloc_sends($x);
    $sendable += $ok;
    $md .= sprintf("| %s | %s (%dth) | %s | %s | %s | %s | %s | %s - %s |\n", $x->{president}, $x->{measure}, $x->{congress}, $x->{kind}, $x->{outcome},
        $hs ? join('-', @$hs) : $x->{house_final}, $ss ? join('-', @$ss) : $x->{senate_final}, @by ? join(', ', @by) : 'none', $ok ? 'yes' : 'no', $why);
}

$md .= "\n## The party-bloc Congress under Rule XXII\n\n";
# THE PLAN'S PREMISE (spec-risk 4, "each party voting as one"; s802 review): a party holds ONE stance on a measure, in both chambers. A measure the
# president's party opposes then reaches him only if the other party carries both chambers alone - a House majority, and in the Senate a majority
# under an expedited procedure or three fifths of the senators sworn under cloture, the senators of no party in their caucus. The seats are each
# Congress's last roll call's in each chamber (the Congress's latest final agreement by roll call).
my %seatsOf;   # "congress chamber" => {R, D, sworn} with the Senate's independents in their caucus
for my $x (sort { $a->{house_final_date} cmp $b->{house_final_date} } @$rows) {
    for my $ch ('house', 'senate') {
        next unless $x->{"${ch}_final"} =~ /^roll /;
        my %s = map { my $v = split_of($x, $ch, $_); ($_ => $v ? $v->[0] + $v->[1] + $v->[2] + $v->[3] : 0) } qw(R D I);
        my %t = (R => $s{R}, D => $s{D}, sworn => $s{R} + $s{D} + $s{I});
        if ($ch eq 'senate' && $x->{senate_I_caucus} ne '-') { for my $kv (split / /, $x->{senate_I_caucus}) { my ($c, $n) = split /:/, $kv; $t{$c} += $n if $c eq 'R' || $c eq 'D'; } }
        my $k = "$x->{congress} $ch";
        $seatsOf{$k} = \%t if !$seatsOf{$k} || $x->{"${ch}_final_date"} ge ($seatsOf{$k}{date} // '');
        $seatsOf{$k}{date} = $x->{"${ch}_final_date"};
    }
}
my ($oneStance, %oneStanceBy, $opposedAll) = (0, ());
for my $x (@$rows) {
    next unless opposed_f3($x, 'house') || opposed_f3($x, 'senate');
    $opposedAll++;
    my $other = $x->{president_party} eq 'R' ? 'D' : 'R';
    my ($h, $s) = ($seatsOf{"$x->{congress} house"}, $seatsOf{"$x->{congress} senate"});
    die "the $x->{congress}th: no roll call gives its seats\n" unless $h && $s;
    my $house = 2 * $h->{$other} > $h->{sworn};
    my $senate = $EXPEDITED{$x->{kind}} ? 2 * $s->{$other} > $s->{sworn} : $s->{$other} >= int((3 * $s->{sworn} + 4) / 5);
    if ($house && $senate) { $oneStance++; $oneStanceBy{$x->{president}}++; }
}
$md .= "**The plan's premise - each party one stance, in both chambers** (spec-risk 4, \"each party voting as one\"): a measure the president's party opposes reaches him only if the other party carries both chambers alone. The seats, each Congress's last roll call's:\n\n| Congress | House R - D (sworn) | Senate R - D, independents in their caucus (sworn) |\n|---|---|---|\n";
for my $c (sort { $a <=> $b } map { /^(\d+) house$/ ? $1 : () } keys %seatsOf) {
    my ($h, $s) = ($seatsOf{"$c house"}, $seatsOf{"$c senate"});
    $md .= sprintf("| %dth | %d - %d (%d) | %d - %d (%d) |\n", $c, $h->{R}, $h->{D}, $h->{sworn}, $s->{R}, $s->{D}, $s->{sworn});
}
$md .= sprintf("\n- Of the %d measures the president's party opposed in a chamber (F3's count), a one-stance bloc Congress sends **%d** (%s).\n\n", $opposedAll, $oneStance,
    join(', ', map { "$_ " . ($oneStanceBy{$_} // 0) } @PRES));
$md .= "**Chamber by chamber, as recorded** (each chamber's blocs voting as their members voted there - a party may stand one way in the House and another in the Senate; the reading this document declared before its first run):\n\n";
my ($allSend, $vetoable, %vetoableBy) = (0, 0);
for my $x (@$rows) {
    my ($ok) = bloc_sends($x);
    next unless $ok;
    $allSend++;
    my $opp = party_bloc_opposed($x, 'house') || party_bloc_opposed($x, 'senate');
    if ($opp) { $vetoable++; $vetoableBy{$x->{president}}++; }
}
$md .= sprintf("- Of the record's %d vetoed measures, it could have sent **%d** to the president.\n", $vetoTotal, $sendable);
$md .= sprintf("- Of all %d measures decided, it sends %d; of those, **%d** are VETOABLE - the president's party bloc opposed in a chamber (%s).\n", scalar(@$rows), $allSend, $vetoable,
    join(', ', map { "$_ " . ($vetoableBy{$_} // 0) } @PRES));

# ------------------------------------------------------------------ the verdict (R-US17)
$md .= "\n## The verdict (R-US17)\n\n";
$md .= sprintf("B1's precision on Poland's whole term, recomputed from the Sejm's CSV: **%d of %d (%s)**. \"Far more\" (declared before the first run, PROVISIONAL): a rule whose pooled precision is below it.\n\n", $b1Vetoed, $b1Named, pct($b1Vetoed, $b1Named));
my @far = grep { my $nm = $named{$_}{pooled} // 0; !$nm || ($namedVetoed{$_}{pooled} // 0) / $nm < $b1Precision } @RULES;
my $aNm = $named{a}{pooled} // 0; my $aPrec = $aNm ? ($namedVetoed{a}{pooled} // 0) / $aNm : 0;
my @stops;
push @stops, 'every rule names far more bills than the record vetoed (each precision below B1\'s)' if @far == @RULES;
push @stops, sprintf('a party-bloc Congress under Rule XXII never sends a vetoable measure under the plan\'s premise, each party one stance in both chambers (0 of %d opposed; chamber by chamber, as recorded, it sends %d)', $opposedAll, $vetoable) if $oneStance == 0;
if (@stops) { $md .= "**STOPPED - R-US17 is re-asked:** " . join('; ', @stops) . sprintf(". On the matrix alone (a) would be recommended: its pooled precision %s is at least B1's %s.\n", pct($namedVetoed{a}{pooled} // 0, $aNm), pct($b1Vetoed, $b1Named)); }
elsif ($aPrec >= $b1Precision) { $md .= sprintf("**Code recommends (a)**: its pooled precision %s is at least B1's %s.\n", pct($namedVetoed{a}{pooled}, $aNm), pct($b1Vetoed, $b1Named)); }
else { $md .= sprintf("**No recommendation - R-US17 is asked with the matrix**: (a)'s pooled precision %s is below B1's %s, and R-US17 names no other rule to recommend.\n", pct($namedVetoed{a}{pooled} // 0, $aNm), pct($b1Vetoed, $b1Named)); }
$md .= "\nThe rules below B1's precision: " . (@far ? join('; ', map { $RULE{$_}{name} } @far) : 'none') . ".\n";

open my $o, '>:raw:encoding(UTF-8)', $out or die "$out: $!";
print $o $md;
close $o;
print "$out written: ", scalar(@$rows), " measures, $vetoTotal vetoes, $sendable sendable under blocs, $vetoable vetoable chamber by chamber, $oneStance under one stance\n";
