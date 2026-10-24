#!/usr/bin/perl
# A-LOOP's registered passes on an audit run's own log (Elias's rulings K60 and K61 of 2026-10-23, docs/RULINGS_2026-10-23.md), recomputed from
# what each RECORD line prints - so a line that misstates its own term fails here while the line is false.
#   K60 (q15, docs/specs/A_LOOP_PLAN.md section 3 (4) (b) as restated): every RECORD line names the approval term WITH its economy stock, and the
#       size it prints for the government side equals b x (net approval now - net approval when the government took office), within 0.01 pp.
#       The governing side's seats on the night (the 175-of-349 line in Sweden) are a READING, printed, never a pass or a fail.
#   q13 (section 4 (4)): the fiscal term the line prints equals sigma_v x delta-z recomputed from the BAL-i the line prints - the two variables
#       ([BD06] Appendix I, the two-year fallback, each held at +/-10 % of GDP), the index at 11.339 / 22.073, the spread the generated fit
#       carries (Assets/Scripts/Elections/Generated/IncumbentVoteSwing.cs, SigmaPoints = the MAD-based scale since K64), the cap at +/-10 pp (K64) -
#       within 0.01 pp.
#   K98 (2026-10-24): while every year a line reads is sourced, its BAL0 must be the source's too (the line names each source year); once a
#       game year is read, BAL0 is the game's own.
#   K61 (q13's surplus clause under the change reading): "a pole whose balance improves across the window gains" - where both variables the line
#       prints improve (neither negative, one positive) the government side's term is positive; where both worsen it is negative; a window that
#       improves on one and worsens on the other is printed as a reading (its sign is the coefficients' weighing, not the rule's).
# A line printed before K62-K64 (no "fiscal years read") carries no BAL-i and its fiscal term is reported as not checkable, never passed.
# Usage (from the project root): perl Tools/audit/aloop_record_check.pl <audit log> [<the run's night folder, holding <id>.night<n>.txt>]
# Prints one line per RECORD line, one per night bound, and "ALOOP RECORD CHECK: PASS" (exit 0), "PASS ON K60 ALONE" where no line carried a
# checkable fiscal term (exit 0), or "ALOOP RECORD CHECK: FAIL - n" (exit 1). ASCII only.
use strict;
use warnings;

my ($log, $nights) = @ARGV;
die "usage: perl Tools/audit/aloop_record_check.pl <audit log> [<night folder>]\n" unless defined $log;
my $gen = 'Assets/Scripts/Elections/Generated/IncumbentVoteSwing.cs';
open my $g, '<', $gen or die "$gen: $!\n";
my ($sigma, $mad);
while (<$g>) { $mad = $1 if /MadScalePoints = ([-0-9.eE]+);/; $sigma = $1 if /SigmaPoints = ([-0-9.eE]+);/; }
close $g;
$sigma = $mad unless defined $sigma;   # since K64 SigmaPoints is the MAD-based scale, written as its name
die "$gen: no sigma_v\n" unless defined $sigma;
my ($BT, $BE, $BOUND, $CAP) = (11.339, 22.073, 10.0, 10.0);   # SOURCED [BD06] Table 2 col. 2; the bound in pp of GDP and the cap in pp of share, [AUTHORED-DRAFT] (FiscalVote)

my ($fail, $checked, $fiscalChecked, $fiscalOld, $fourYear) = (0, 0, 0, 0, 0);
my $run = '(before any run)';
my (%held, $pending);
sub bad { my ($what) = @_; $fail++; print "  FAIL $what\n"; }
sub held { my ($x) = @_; return $x > $BOUND ? $BOUND : $x < -$BOUND ? -$BOUND : $x; }

open my $in, '<', $log or die "$log: $!\n";
while (my $line = <$in>) {
    $line =~ s/\r?\n$//;
    if ($line =~ /^AUDIT: RUN \d+ of \d+ - (\S+):/) { $run = $1; $pending = undef; next; }
    if ($line =~ /^ELECTION: held for \S+ on /) { night($pending, ++$held{$run}) if defined $pending; $pending = undef; next; }
    next unless $line =~ /^RECORD: the government's record judged (for the count on (\d{4}-\d\d-\d\d)|at the campaign's opening) for (\S+)/;
    my ($count, $day, $country) = (defined $2, $2 // '', $3);
    my $tag = sprintf('%s %s %s', $run, $count ? "the count on $day" : 'the opening', $country);
    $checked++;

    # ---- K60: the approval term
    my ($approvalPart) = $line =~ /; approval: (.*?); fiscal: /;
    my $approvalSize = 'n/a';
    if (!defined $approvalPart) { bad("$tag: the line carries no approval part"); }
    elsif ($approvalPart =~ /approval net of its economy terms (-?[0-9.]+) \(approval (-?[0-9.]+), its economy stock ([-+][0-9.]+)\) against (-?[0-9.]+) when .*?: b ([0-9.]+).*?-> (?:the government side ([-+][0-9.]+) pp|no governing party carries it)/) {
        my ($net, $appr, $stock, $ref, $b, $size) = ($1, $2, $3, $4, $5, $6);
        my $fromPrinted = $b * ($net - $ref);
        my $netOk = abs($net - (($appr - $stock) < 0 ? 0 : ($appr - $stock) > 100 ? 100 : ($appr - $stock))) <= 0.02;
        if (defined $size) {
            $approvalSize = sprintf('%+.2f pp = %.2f x (%.2f - %.2f) %s', $size, $b, $net, $ref, abs($size - $fromPrinted) <= 0.01 ? 'ok' : 'MISMATCH');
            bad("$tag: the approval term prints $size pp, b x the net change is " . sprintf('%+.4f', $fromPrinted)) unless abs($size - $fromPrinted) <= 0.01;
            bad("$tag: K60 - b prints $b, not 0.16") unless $b == 0.16;   # K60: the coefficient is 0.16 (SOURCED for the USA, transferred elsewhere: ApprovalVote)
        } else { $approvalSize = 'no governing party carries it'; }
        bad("$tag: net approval $net is not approval $appr less its stock $stock") unless $netOk;
    }
    elsif ($approvalPart =~ /no reference marked|no government stored/) { $approvalSize = 'no approval term (no reference)'; }
    else { bad("$tag: the approval term is not named with its economy stock: $approvalPart"); }

    # ---- q13, K64, K61: the fiscal term
    my ($fiscalPart) = $line =~ /; fiscal: (.*)\)$/;
    my $fiscal = 'n/a';
    if (!defined $fiscalPart) { bad("$tag: the line carries no fiscal part"); }
    elsif ($fiscalPart !~ /fiscal years read/) { $fiscal = 'printed before K62-K64: no BAL-i on the line, not checkable'; $fiscalOld++; }
    elsif ($fiscalPart =~ /fiscal years read (\d+) \((\d+) (?:sourced before the start|from the source)(?:; source years:([^)]*))?\), (.*?); BAL-4 (\S+) BAL-3 (\S+) BAL-2 (\S+) BAL-1 (\S+) BAL0 (\S+) pp of GDP(.*)$/) {
        my ($years, $sourced, $sources, $tenure, $b4, $b3, $b2, $b1, $b0, $rest) = ($1, $2, $3 // '', $4, $5, $6, $7, $8, $9, $10);
        $fiscalChecked++;
        # K98 (2026-10-24): while every year read is a sourced one (no game year has closed under the tenure), BAL0 is the source's too
        bad("$tag: K98 - every year read is sourced, but BAL0 is not the source's ($sources)") if $years >= 1 && $sourced == $years && $b0 ne 'n/a' && $sources !~ /BAL0 \d{4}/;
        bad("$tag: K98 - a game year read, yet BAL0 is printed as the source's ($sources)") if $sourced < $years && $sources =~ /BAL0 \d{4}/;
        # K100 / K105 (the s-fisc review 2's L3): on a first count the line names the election's year E; BAL0 is E, every BAL-i older than E, and on
        # a count line E is the count's own year
        if ($sources =~ /election year (\d{4})/) {
            my $e = $1;
            bad("$tag: K100 - BAL0's source year is not the election's $e ($sources)") if $sources =~ /BAL0 (\d{4})/ && $1 != $e;
            for my $y ($sources =~ /BAL-\d (\d{4})/g) { bad("$tag: K100 - a year read ($y) is not older than the election's $e") unless $y < $e; }
            my %seen; for my $y ($sources =~ /BAL-?\d (\d{4})/g) { bad("$tag: H1 - the source year $y is read twice ($sources)") if $seen{$y}++; }
            bad("$tag: K100 - the count on $day reads the election year $e") if $count && substr($day, 0, 4) != $e;
            bad("$tag: K100 - the opening reads an election year $e before its own day") if !$count && $line =~ /judged (\d{4})-\d\d-\d\d:/ && $e < $1;
        }
        bad("$tag: four years read, yet the line prints the two-year fallback") if $years >= 4 && $rest =~ /\(two years\)/;
        $fourYear++ if $years >= 4;
        my $na = sub { $_[0] eq 'n/a' ? undef : $_[0] + 0 };
        ($b4, $b3, $b2, $b1, $b0) = map { $na->($_) } ($b4, $b3, $b2, $b1, $b0);
        my ($term, $ey);
        if ($years >= 4) { $term = 0.5 * ($b1 + $b2) - 0.5 * ($b3 + $b4); }
        elsif ($years >= 2) { $term = $b1 - $b2; }
        $ey = $b0 - $b1 if defined $b0 && $years >= 1;
        if ($rest =~ /^ - no fiscal term/) {
            bad("$tag: the line prints no fiscal term, but its BAL-i form one") if defined $term || defined $ey;
            $fiscal = "no fiscal term ($years year(s) read; $tenure)";
        } elsif ($rest =~ /index ([-+][0-9.]+) x spread ([0-9.]+) pp = ([-+][0-9.]+) pp(, capped at \+\/-[0-9.]+ pp)? -> (?:the government side ([-+][0-9.]+) pp|no governing party carries it)/) {
            my ($index, $spread, $uncapped, $capped, $side) = ($1, $2, $3, $4, $5);
            my $idx = $BT * held($term // 0) / 100 + $BE * held($ey // 0) / 100;
            my $u = $sigma * $idx;
            my $z = $u > $CAP ? $CAP : $u < -$CAP ? -$CAP : $u;
            bad(sprintf('%s: the printed index %s is not %.6f from the printed BAL-i', $tag, $index, $idx)) unless abs($index - $idx) <= 1e-4;
            bad(sprintf('%s: the printed spread %s pp is not sigma_v %.6f', $tag, $spread, $sigma)) unless abs($spread - $sigma) <= 1e-6;
            bad(sprintf('%s: sigma_v x delta-z prints %s pp, recomputed %+.4f', $tag, $uncapped, $u)) unless abs($uncapped - $u) <= 0.01;
            bad("$tag: the cap is printed where it does not bind, or not where it does") unless (defined $capped) == (abs($u) > $CAP);
            if (defined $side) {
                bad(sprintf('%s: the government side prints %s pp, recomputed %+.4f', $tag, $side, $z)) unless abs($side - $z) <= 0.01;
                my ($t, $e) = ($term // 0, $ey // 0);
                my $improves = $t >= 0 && $e >= 0 && ($t > 0 || $e > 0);
                my $worsens = $t <= 0 && $e <= 0 && ($t < 0 || $e < 0);
                my $rule = $improves ? 'improves: gains' : $worsens ? 'worsens: loses' : 'mixed: a reading';
                # K61's sign, on the game's own printed term (the s-fisc review 2's L2: a test the recomputation cannot satisfy by construction) - a
                # wrong sign beyond the print's rounding fails; a term too small to print (|z| < 1e-4 pp) is a reading
                bad("$tag: K61 - the balance improves across the window and the game prints $side pp") if $improves && ($side < 0 || (abs($z) >= 1e-4 && !($side > 0)));
                bad("$tag: K61 - the balance worsens across the window and the game prints $side pp") if $worsens && ($side > 0 || (abs($z) >= 1e-4 && !($side < 0)));
                $fiscal = sprintf('%+.2f pp (term %s, election year %s; %d year(s) read, %d sourced; %s%s) K61 %s', $side,
                    defined $term ? sprintf('%+.3f', $term) : 'none', defined $ey ? sprintf('%+.3f', $ey) : 'none', $years, $sourced, $tenure, defined $capped ? '; capped' : '', $rule);
            } else { $fiscal = 'no governing party carries it'; }
        } else { bad("$tag: the fiscal part does not parse: $rest"); }
    } else { bad("$tag: the fiscal part does not parse: $fiscalPart"); }

    # the parties the record's total names - the governing side, whose seats on the night are K60's reading
    if ($count) {
        my ($total) = $line =~ /judged \d{4}-\d\d-\d\d: .*?: (.*?) \(economy: /;
        $pending = { tag => $tag, parties => [defined $total ? map { /^(\S+) [-+]/ ? $1 : () } split /, /, $total : ()] };
    }
    print "  $tag: approval $approvalSize; fiscal $fiscal\n";
}

# A count line followed by the played world's own count ("ELECTION: held for") is that election's: its night file is the run's next. A count line
# no held election follows (a fork's world judging its own record: the counterfactual's elections, K50) is checked above and binds no night.
sub night {
    my ($p, $k) = @_;
    return unless defined $nights;
    my $file = "$nights/$run.night$k.txt";
    my $seats;
    if (open my $nf, '<', $file) {
        my ($name, %won, $of);
        while (<$nf>) { s/\r?\n$//; if (/^Name\t(.+)$/) { $name = $1; } elsif (/^Figure\t.*?(\d+) of (\d+) seats/) { $of = $2; } elsif (/^Figure\t.*?(\d+) seats/ && defined $name) { $won{$name} = $1; } }
        close $nf;
        my $side = 0; $side += $won{$_} // 0 for @{$p->{parties}};
        my $majority = defined $of ? int($of / 2) + 1 : undef;
        $seats = sprintf('READING: the governing side (%s) holds %d%s', join(' ', @{$p->{parties}}), $side,
            defined $of ? sprintf(' of %d - %s the majority line %d', $of, $side >= $majority ? 'at or above' : 'below', $majority) : '');
    } else { $seats = "READING: no night file $file"; }
    print "  $p->{tag} -> night $k: $seats\n";
}
close $in;
die "no RECORD line in $log\n" unless $checked;
print "  $checked RECORD line(s); the fiscal term recomputed on $fiscalChecked, $fiscalOld printed before K62-K64 (not checkable); [BD06]'s four-year form on $fourYear; sigma_v $sigma pp\n";
print $fail ? "ALOOP RECORD CHECK: FAIL - $fail\n"
    : $fiscalChecked ? "ALOOP RECORD CHECK: PASS\n"
    : "ALOOP RECORD CHECK: PASS ON K60 ALONE - no line carried a checkable fiscal term\n";
exit($fail ? 1 : 0);
