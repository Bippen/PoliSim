#!/usr/bin/perl
# THE PRESIDENT'S VETO ON THE 10TH SEJM'S RECORD - F3'S WIDENED RULE AND THE RATES THE GAME DRAWS AT, AND B1 AS FIRST RULED, EVERY MISS LISTED
# (Elias's rulings E4, COMPLETED.md s775, and F3). Reads ElectionsData/poland/third_readings_term10.csv (every whole-bill third reading of the term,
# the PiS club's votes, the outcome) and ElectionsData/poland/veto_record.csv (the vetoes, the presidents as the record names them, the full titles),
# and writes two files:
#   docs/generated/VETO_B1_BACKTEST.md - F3: the statutes at risk per president (vetoed / signed / sent to the Tribunal), the rate each president
#     vetoed at among them and the pooled rate, every veto outside the widened base; then B1 as first ruled: the confusion matrix, the hit rate, the
#     precision, every veto it misses and every act it names that was not vetoed (E4);
#   Assets/Scripts/Elections/Generated/PolishVetoRates.cs - the runtime table the game draws at: each president's statutes at risk and vetoes, with
#     both CSVs' digests (PresidentialVetoDiagnostic re-reads the CSVs and recomputes every figure).
# F3 (ruled 2026-10-05): a statute is AT RISK when the President's backing party did not vote for it - a majority of its VOTING members (yes, no,
# abstain; the absent outside the count) voted no or abstained; a seeded draw then decides at the president's own rate refitted on that base, the
# pooled rate for a president with no record. READING, stated: a referral to the Tribunal is a decided act and not a veto, so it counts in the
# widened base as not vetoed. B1 (ruled 2026-10-02, s757): the President
# vetoes when MORE THAN HALF of the backing party's club MEMBERS (absent included) voted NO. The budget act (veto_exempt) is outside both. The
# president is the one on the day of the DECISION (outcome_date): the term boundary is the day Nawrocki took the oath; each term's name is the one
# veto_record.csv gives its vetoes, and the tool dies unless a term's vetoes carry one name. PiS backs both. An undecided act is outside every count.
# Usage (from the project root): perl Tools/veto_b1_backtest.pl [md] [cs]
# The CSV reader is RFC 4180 (quoted fields, doubled quotes) - the term's titles carry commas and quotes. Dies on any parse anomaly.
use strict;
use warnings;
use utf8;
use Digest::SHA qw(sha256_hex);

my $out = $ARGV[0] // 'docs/generated/VETO_B1_BACKTEST.md';
my $cs = $ARGV[1] // 'Assets/Scripts/Elections/Generated/PolishVetoRates.cs';
my $D = 'ElectionsData/poland/';

sub read_csv {
    my ($path) = @_;
    open my $fh, '<:encoding(UTF-8)', $path or die "$path: $!";
    local $/;
    my $s = <$fh>;
    close $fh;
    my (@rows, @row);
    my ($field, $inq, $i, $n) = ('', 0, 0, length $s);
    while ($i < $n) {
        my $c = substr($s, $i, 1);
        if ($inq) {
            if ($c eq '"') {
                if ($i + 1 < $n && substr($s, $i + 1, 1) eq '"') { $field .= '"'; $i += 2; next; }
                $inq = 0; $i++; next;
            }
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
    my $head = shift @rows;
    my @recs;
    for my $r (@rows) {
        next if @$r == 1 && $r->[0] eq '';
        next if $r->[0] =~ /^#/;
        die "$path: a row of " . scalar(@$r) . " fields against a header of " . scalar(@$head) . "\n" unless @$r == @$head;
        my %x;
        @x{@$head} = @$r;
        push @recs, \%x;
    }
    return \@recs;
}

sub digest_of { my ($path) = @_; open my $fh, '<:raw', $path or die "$path: $!"; local $/; my $b = <$fh>; close $fh; return sha256_hex($b); }

my $readings = read_csv($D . 'third_readings_term10.csv');
my $vetoes = read_csv($D . 'veto_record.csv');

sub outcome { my $o = shift; return 'vetoed' if $o eq 'vetoed'; return 'signed' if $o =~ /^signed/; return 'Tribunal' if $o =~ /^referred to Tribunal/; return undef; }
sub term { $_[0] lt '2025-08-06' ? 'Duda' : 'Nawrocki' }
sub fires { my $x = shift; 2 * $x->{pis_no} > $x->{pis_members} }                      # B1 as first ruled (s757)
sub at_risk { my $x = shift; $x->{pis_no} + $x->{pis_abstain} > $x->{pis_yes} }        # F3: a majority of the club's VOTING members voted no or abstained

# each term's president as the record names them - one name per term, or the tool dies
my %name;
for my $v (@$vetoes) {
    my $t = term($v->{veto_date});
    die "veto_record.csv names two presidents in the $t term: $name{$t} and $v->{president}\n" if defined $name{$t} && $name{$t} ne $v->{president};
    $name{$t} = $v->{president};
}
for my $t (qw(Duda Nawrocki)) { die "veto_record.csv names no president in the $t term\n" unless defined $name{$t}; }

my @decided = grep { defined outcome($_->{outcome}) && $_->{veto_exempt} eq '' } @$readings;
die "no decided act read\n" unless @decided;
for my $x (@decided) { die "PiS's counts do not sum to its members at $x->{sitting}/$x->{vote_no}\n" unless $x->{pis_yes} + $x->{pis_no} + $x->{pis_abstain} + $x->{pis_absent} == $x->{pis_members}; }

my (%cell, %risk);
for my $x (@decided) {
    $cell{term($x->{outcome_date})}{fires($x) ? 'yes' : 'no'}{outcome($x->{outcome})}++;
    $risk{term($x->{outcome_date})}{at_risk($x) ? 'risk' : 'safe'}{outcome($x->{outcome})}++;
}
sub c { my ($p, $f, $o) = @_; my $n = 0; for my $pp ($p eq 'term' ? qw(Duda Nawrocki) : ($p)) { $n += $cell{$pp}{$f}{$o} // 0; } return $n; }
sub r { my ($p, $f, $o) = @_; my $n = 0; for my $pp ($p eq 'term' ? qw(Duda Nawrocki) : ($p)) { $n += $risk{$pp}{$f}{$o} // 0; } return $n; }
sub rn { my ($p, $f) = @_; return r($p, $f, 'vetoed') + r($p, $f, 'signed') + r($p, $f, 'Tribunal'); }
sub pct { my ($a, $b) = @_; return $b ? sprintf('%.1f %%', 100 * $a / $b) : '-'; }

my %vetoTitle;
for my $v (@$vetoes) { $vetoTitle{"$v->{third_reading_sitting}/$v->{third_reading_vote_no}"} = $v->{act_title}; }
sub title_of { my $x = shift; return $vetoTitle{"$x->{sitting}/$x->{vote_no}"} // $x->{title}; }
sub split_of { my $x = shift; return "$x->{pis_yes}/$x->{pis_no}/$x->{pis_abstain}/$x->{pis_absent} of $x->{pis_members}"
    . ($x->{rozwojplus_yes} ne '' ? " (RozwojPlus $x->{rozwojplus_yes}/$x->{rozwojplus_no}/$x->{rozwojplus_abstain}/$x->{rozwojplus_absent})" : ''); }

open my $o, '>:encoding(UTF-8)', $out or die "$out: $!";
print $o "<!-- GENERATED by Tools/veto_b1_backtest.pl. DO NOT EDIT BY HAND. Re-derive: perl Tools/veto_b1_backtest.pl -->\n";
print $o "# The President's veto on the 10th Sejm's record - F3's widened rule and the rates the game draws at, and B1 as first ruled, every miss\n\n";
print $o "**F3** (Elias's ruling, widening B1): a statute is at risk when the President's backing party did not vote for it - a majority of its voting\n";
print $o "members (the absent outside the count) voted no or abstained; a seeded draw then decides, at each president's own rate on that base, the pooled\n";
print $o "rate for a president with no record. **B1** (Elias's ruling, COMPLETED.md s757, as first ruled): the President vetoes an ordinary statute when more\n";
print $o "than half of the backing party's club members at the vote (absent included) voted NO. Read on `ElectionsData/poland/third_readings_term10.csv`\n";
print $o "and `veto_record.csv` (the Sejm API's own record); the president is the one on the decision's day; budget acts and undecided acts are outside\n";
print $o "every count; READING, stated: a referral to the Tribunal is a decided act and not a veto, so it counts in the widened base. The rates below are the runtime table\n";
print $o "`Assets/Scripts/Elections/Generated/PolishVetoRates.cs`, written by the same run.\n\n";
print $o "## F3: the statutes at risk, and the rate each president vetoed at among them\n\n";
print $o "| President | at risk | vetoed | signed | to the Tribunal | not at risk: vetoed / signed / to the Tribunal | the rate the game draws at |\n|---|---:|---:|---:|---:|---|---:|\n";
for my $p (qw(Duda Nawrocki term)) {
    printf $o "| %s | %d | %d | %d | %d | %d / %d / %d | %s |\n", ($p eq 'term' ? 'pooled - a president with no record' : $name{$p}), rn($p, 'risk'),
        r($p, 'risk', 'vetoed'), r($p, 'risk', 'signed'), r($p, 'risk', 'Tribunal'), r($p, 'safe', 'vetoed'), r($p, 'safe', 'signed'), r($p, 'safe', 'Tribunal'),
        pct(r($p, 'risk', 'vetoed'), rn($p, 'risk'));
}
print $o "\n## F3: every veto outside the widened base\n\n";
my @outside = sort { $a->{outcome_date} cmp $b->{outcome_date} } grep { outcome($_->{outcome}) eq 'vetoed' && !at_risk($_) } @decided;
if (!@outside) { print $o "None: every veto on the record falls on a statute at risk.\n"; }
my $k = 0;
for my $x (@outside) { $k++; printf $o "%d. **%s, vetoed %s** - PiS yes/no/abstain/absent %s - %s\n", $k, $name{term($x->{outcome_date})}, $x->{outcome_date}, split_of($x), title_of($x); }
print $o "\n## B1 as first ruled (s757) - superseded by F3\n\n### The confusion matrix\n\n| President | B1 fires | vetoed | signed | to the Tribunal | total |\n|---|---|---:|---:|---:|---:|\n";
for my $p (qw(Duda Nawrocki term)) {
    for my $f (qw(yes no)) {
        my ($v, $s, $t) = (c($p, $f, 'vetoed'), c($p, $f, 'signed'), c($p, $f, 'Tribunal'));
        print $o "| " . ($p eq 'term' ? 'the whole term' : $name{$p}) . " | $f | $v | $s | $t | " . ($v + $s + $t) . " |\n";
    }
}
print $o "\n### The rates\n\n";
for my $p (qw(Duda Nawrocki term)) {
    my $vet = c($p, 'yes', 'vetoed') + c($p, 'no', 'vetoed');
    my $named = c($p, 'yes', 'vetoed') + c($p, 'yes', 'signed') + c($p, 'yes', 'Tribunal');
    my $all = $named + c($p, 'no', 'vetoed') + c($p, 'no', 'signed') + c($p, 'no', 'Tribunal');
    printf $o "- **%s**: hit rate %d of %d vetoes (%s); precision %d of the %d acts B1 names (%s); %d of %d decided acts vetoed (%s).\n",
        ($p eq 'term' ? 'The whole term' : $name{$p}), c($p, 'yes', 'vetoed'), $vet, pct(c($p, 'yes', 'vetoed'), $vet), c($p, 'yes', 'vetoed'), $named, pct(c($p, 'yes', 'vetoed'), $named),
        $vet, $all, pct($vet, $all);
}
print $o "\n### Every veto B1 misses - and whether F3 puts it at risk\n\n";
$k = 0;
for my $x (sort { $a->{outcome_date} cmp $b->{outcome_date} } grep { outcome($_->{outcome}) eq 'vetoed' && !fires($_) } @decided) {
    $k++;
    printf $o "%d. **%s, vetoed %s** - PiS yes/no/abstain/absent %s - %s - F3: %s\n", $k, $name{term($x->{outcome_date})}, $x->{outcome_date}, split_of($x),
        title_of($x), (at_risk($x) ? 'at risk' : 'NOT at risk');
}
print $o "\n### Every act B1 names that was not vetoed\n\n";
for my $p (qw(Duda Nawrocki)) {
    my @l = sort { $a->{outcome_date} cmp $b->{outcome_date} || $a->{sitting} <=> $b->{sitting} || $a->{vote_no} <=> $b->{vote_no} }
        grep { term($_->{outcome_date}) eq $p && fires($_) && outcome($_->{outcome}) ne 'vetoed' } @decided;
    print $o "#### $name{$p} - " . scalar(@l) . " acts\n\n| decided | outcome | Sejm vote | PiS yes/no/abstain/absent of members | the Sejm's agenda title |\n|---|---|---|---|---|\n";
    for my $x (@l) {
        (my $t = $x->{title}) =~ s/\|/\//g;
        printf $o "| %s | %s | %s/%s (%s) | %s/%s/%s/%s of %s | %s |\n", $x->{outcome_date}, outcome($x->{outcome}), $x->{sitting}, $x->{vote_no}, $x->{date},
            $x->{pis_yes}, $x->{pis_no}, $x->{pis_abstain}, $x->{pis_absent}, $x->{pis_members}, $t;
    }
    print $o "\n";
}
close $o;

my ($d1, $d2) = (digest_of($D . 'third_readings_term10.csv'), digest_of($D . 'veto_record.csv'));
open my $g, '>:encoding(UTF-8)', $cs or die "$cs: $!";
print $g "// GENERATED by Tools/veto_b1_backtest.pl. DO NOT EDIT BY HAND.\n//\n";
print $g "// Source : ElectionsData/poland/third_readings_term10.csv\n// SHA-256: $d1\n";
print $g "// Source : ElectionsData/poland/veto_record.csv\n// SHA-256: $d2\n//\n";
print $g "// PresidentialVetoDiagnostic re-reads both CSVs and recomputes every figure.\n\n";
print $g "namespace PoliSim.Elections.Generated\n{\n";
print $g "    /// <summary>Elias's ruling F3: the rates the President's veto is drawn at - for each president of the 10th Sejm's term, the ordinary statutes\n";
print $g "    /// AT RISK (the backing party did not vote for them) decided in that term, and those the President vetoed - SOURCED (the Sejm API's own record,\n";
print $g "    /// `ElectionsData/poland/veto_record.md`). Generated, never hand-edited.</summary>\n";
print $g "    public static class PolishVetoRates\n    {\n";
print $g "        public const string SourceDigest = \"$d1\";\n";
print $g "        public const string VetoRecordDigest = \"$d2\";\n\n";
print $g "        /// <summary>Each president as the record names them: the statutes at risk decided in the term, and those vetoed.</summary>\n";
print $g "        public static readonly (string President, int AtRisk, int Vetoed)[] OfRecord =\n        {\n";
for my $p (qw(Duda Nawrocki)) { printf $g "            (\"%s\", %d, %d),\n", $name{$p}, rn($p, 'risk'), r($p, 'risk', 'vetoed'); }
print $g "        };\n    }\n}\n";
close $g;

print "WROTE $out and $cs - " . scalar(@decided) . " decided ordinary statutes; F3 puts " . rn('term', 'risk') . " at risk, "
    . r('term', 'risk', 'vetoed') . " of them vetoed, " . scalar(@outside) . " vetoes outside; B1 named " . (c('term', 'yes', 'vetoed') + c('term', 'yes', 'signed') + c('term', 'yes', 'Tribunal'))
    . ", caught " . c('term', 'yes', 'vetoed') . " of " . (c('term', 'yes', 'vetoed') + c('term', 'no', 'vetoed')) . " vetoes\n";
