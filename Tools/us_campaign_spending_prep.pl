#!/usr/bin/perl
# THE US WAR CHEST FROM FEC SPENDING (K19, docs/RULINGS_2026-10-23.md: "Source the war chest's price table from FEC spending"; US-30's BILLED table).
# It reads ONLY the saved OpenFEC responses under ElectionsData/usa/raw/fec_spending/, each held to its SHA256SUMS.txt line first:
#   - fec_committee_<id>_reports_2024.json: every report the committee filed for the 2024 cycle - its coverage, its form, whether it is the most
#     recent version, and its total disbursements for the period;
#   - fec_committee_<id>_totals_2024.json: the committee's cycle totals (its disbursements, the cycle's coverage);
#   - fec_schedule_b_by_purpose_<id>_2024.json: the cycle's itemized disbursements by the FEC's purpose category;
#   - fec_committee_<id>_history_2024.json: the committee's candidate ids (the nominee it was authorized by).
# The committees: Harris for President (C00703975, DEM) and Donald J. Trump for President 2024 (C00828541, REP; the FEC lists it under its later
# name, NEVER SURRENDER, INC., and as a Leadership PAC since its conversion - DECLARED by its reports: every report covering the window is a Form 3P,
# the presidential candidate committee's report, and its history names candidate P80001571).
# DERIVED, the war chest: each committee's disbursements over the game's 2024 campaign window (WINDOW below - the C# check holds it to the game's
# CampaignCalendar), each most-recent Form 3P report's total disbursements counted by the share of its coverage days inside the window (DECLARED: a
# report's spending spread evenly over its days); the war chest the two nominees' mean (DECLARED: D-1 (c)'s equal chest for every party).
# It writes ElectionsData/usa/campaign_spending.csv and Assets/Scripts/Elections/Generated/UsCampaignSpending.cs, both built and tested before
# either is written. It dies, writing nothing, on: a file off its digest or in no SHA256SUMS.txt; a response whose pagination is not whole (more
# pages than read); a committee whose history does not name its candidate; a window day covered by no report or by two; a report covering the
# window that is not a Form 3P; a cycle total, a purpose total or a report total that is not a number; a non-ASCII byte in an output.
# Usage: perl Tools/us_campaign_spending_prep.pl   (from the project root, Git for Windows' perl; JSON::PP is core). ASCII only.
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use JSON::PP;
use Time::Local qw(timegm);

my $raw = 'ElectionsData/usa/raw/fec_spending';
my @WINDOW = ('2024-09-10', '2024-11-04');   # DECLARED: the game's 2024 campaign, its first day to the day before the polling day (UsCampaignSpendingCheck holds it)
my @COMMITTEES = (['C00703975', 'DEM', 'P00009423', 'HARRIS FOR PRESIDENT', undef], ['C00828541', 'REP', 'P80001571', 'NEVER SURRENDER, INC.', 'DONALD J. TRUMP FOR PRESIDENT 2024, INC.']);   # [id, the party key, the nominee's candidate id, the name the FEC lists, the former name (the review's L-3) or none]
my @problems;
sub problem { push @problems, join('', @_); return; }
$SIG{__WARN__} = sub { problem('a perl warning: ', $_[0] =~ s/\s+$//r) };

my %sums;
{
    open my $s, '<', "$raw/SHA256SUMS.txt" or die "$raw/SHA256SUMS.txt: $!\n";
    while (my $l = <$s>) { $sums{$2} = $1 if $l =~ /^([0-9a-f]{64}) \*(.+?)\s*$/; }
    close $s;
}
my %read;
sub json_of {
    my $name = shift;
    my $f = "$raw/$name";
    open my $h, '<:raw', $f or die "$f: $!\n";
    local $/;
    my $b = <$h>;
    close $h;
    my $d = sha256_hex($b);
    my $want = $sums{$name} or die "$f: in no SHA256SUMS.txt - the tool reads saved responses only\n";
    die "$f: SHA-256 $d, its SHA256SUMS.txt holds $want\n" unless $d eq $want;
    $read{"raw/fec_spending/$name"} = $d;
    my $j = decode_json($b);
    my $p = $j->{pagination};
    problem("$name: page $p->{page} of $p->{pages} - not the whole response") if $p && ($p->{pages} // 1) > 1;
    return $j;
}
sub epoch { my ($y, $m, $d) = $_[0] =~ /^(\d{4})-(\d\d)-(\d\d)/ or die "not a day: $_[0]\n"; return timegm(0, 0, 12, $d, $m - 1, $y); }
sub days { return int((epoch($_[1]) - epoch($_[0])) / 86400 + 0.5) + 1; }   # inclusive
sub num { my ($v, $what) = @_; problem("$what: not a number ('" . ($v // 'none') . "')") unless defined $v && $v =~ /^-?\d+(\.\d+)?$/; return $v // 0; }

my @rows;    # [kind, committee, party, label, from, to, amount, days_in_window, days]
my %window;  # party -> window disbursements
for my $c (@COMMITTEES) {
    my ($id, $party, $cand, $name, $former) = @$c;
    my $hist = json_of("fec_committee_${id}_history_2024.json");
    my @ids = map { @{$_->{candidate_ids} // []} } @{$hist->{results}};
    problem("$id: its 2024 history names candidates " . join(',', @ids) . ", not $cand") unless grep { $_ eq $cand } @ids;
    my $h = $hist->{results}[0] // {};
    problem("$id: its 2024 history names the committee '" . ($h->{name} // 'none') . "', not '$name'") unless ($h->{name} // '') eq $name;
    if (defined $former) {   # the review's L-3: a converted committee is the nominee's by its former name and former candidate, not by a candidate id a sponsored PAC would also list
        problem("$id: its former name '" . ($h->{former_committee_name} // 'none') . "' is not '$former'") unless ($h->{former_committee_name} // '') eq $former;
        problem("$id: its former candidate '" . ($h->{former_candidate_id} // 'none') . "' is not $cand") unless ($h->{former_candidate_id} // '') eq $cand;
    } else {
        problem("$id: not its candidate's principal campaign committee in 2024") unless ($h->{designation_full} // '') eq 'Principal campaign committee';
    }
    my $tot = json_of("fec_committee_${id}_totals_2024.json")->{results}[0];
    my ($cf, $ct) = (substr($tot->{coverage_start_date}, 0, 10), substr($tot->{coverage_end_date}, 0, 10));
    push @rows, ['cycle', $id, $party, 'total disbursements', $cf, $ct, num($tot->{disbursements}, "$id cycle disbursements"), '-', days($cf, $ct)];
    for my $p (sort { $a->{purpose} cmp $b->{purpose} } @{json_of("fec_schedule_b_by_purpose_${id}_2024.json")->{results}}) {
        push @rows, ['purpose', $id, $party, $p->{purpose}, $cf, $ct, num($p->{total}, "$id purpose $p->{purpose}"), '-', '-'];
    }
    my @reports = grep { $_->{most_recent} } @{json_of("fec_committee_${id}_reports_2024.json")->{results}};
    my %covered;
    my $sum = 0;
    for my $r (sort { $a->{coverage_start_date} cmp $b->{coverage_start_date} } @reports) {
        my ($f, $t) = (substr($r->{coverage_start_date}, 0, 10), substr($r->{coverage_end_date}, 0, 10));
        next if $t lt $WINDOW[0] || $f gt $WINDOW[1];
        problem("$id: the report $r->{report_type_full} ($f to $t) covers the window and is a $r->{report_form}, not a Form 3P") unless ($r->{report_form} // '') eq 'Form 3P';
        my ($in_f, $in_t) = ($f lt $WINDOW[0] ? $WINDOW[0] : $f, $t gt $WINDOW[1] ? $WINDOW[1] : $t);
        for (my $e = epoch($in_f); $e <= epoch($in_t); $e += 86400) { my @g = gmtime($e); my $d = sprintf('%04d-%02d-%02d', $g[5] + 1900, $g[4] + 1, $g[3]); problem("$id: $d covered twice") if $covered{$d}++; }
        my $amount = num($r->{total_disbursements_period}, "$id $r->{report_type_full} disbursements");
        my ($din, $dall) = (days($in_f, $in_t), days($f, $t));
        push @rows, ['report', $id, $party, $r->{report_type_full}, $f, $t, $amount, $din, $dall];
        $sum += $amount * $din / $dall;
    }
    for (my $e = epoch($WINDOW[0]); $e <= epoch($WINDOW[1]); $e += 86400) { my @g = gmtime($e); my $d = sprintf('%04d-%02d-%02d', $g[5] + 1900, $g[4] + 1, $g[3]); problem("$id: $d covered by no report") unless $covered{$d}; }
    $window{$party} = $sum;
}
my $chest = ($window{DEM} + $window{REP}) / 2.0;
$SIG{__WARN__} = 'DEFAULT';
if (@problems) { my $n = @problems; print STDERR "MISMATCH: $_\n" for @problems; die "$n mismatch(es) - nothing written\n"; }

my $csv = "# GENERATED by Tools/us_campaign_spending_prep.pl from the OpenFEC responses under ElectionsData/usa/raw/fec_spending/ - DO NOT EDIT.\n"
    . "# K19: the 2024 nominees' principal campaign committees (DEM C00703975, REP C00828541). kind cycle: the committee's cycle disbursements; purpose:\n"
    . "# its cycle's itemized disbursements by the FEC's purpose category; report: each most-recent Form 3P report covering the game's 2024 campaign\n"
    . "# window ($WINDOW[0] to $WINDOW[1]), its total disbursements, its days inside the window and its days - counted by that share (DECLARED).\n"
    . "kind,committee,party,label,from,to,amount,days_in_window,days\n"
    . join('', map { join(',', @$_) . "\n" } @rows);
my $digest = sha256_hex($csv);
sub cq { my $s = shift; $s =~ s/\\/\\\\/g; $s =~ s/"/\\"/g; return "\"$s\""; }
my $cs = "// GENERATED by Tools/us_campaign_spending_prep.pl. DO NOT EDIT BY HAND.\n//\n"
    . "// Source: ElectionsData/usa/campaign_spending.csv (its SHA-256 below), written by the same run from the OpenFEC responses listed in RawSources.\n"
    . "// UsCampaignSpendingCheck re-reads the CSV and the responses and derives every figure again.\n\n"
    . "namespace PoliSim.Elections.Generated\n{\n"
    . "    /// <summary>K19 (Elias's ruling of 2026-10-23): the US war chest from FEC spending - the 2024 nominees' committees' disbursements over the game's\n"
    . "    /// campaign window and their mean (DERIVED from SOURCED), and the cycle's spending by purpose (SOURCED). Generated, never hand-edited.</summary>\n"
    . "    public static class UsCampaignSpending\n    {\n"
    . "        public const string SourceDigest = \"$digest\";\n"
    . "        /// <summary>The game's 2024 campaign window the reports are counted over, its first and last day (DECLARED; the check holds it to the calendar).</summary>\n"
    . "        public const string WindowFrom = \"$WINDOW[0]\";\n        public const string WindowTo = \"$WINDOW[1]\";\n"
    . sprintf("        /// <summary>DERIVED: the Democratic nominee's committee's disbursements over the window, dollars.</summary>\n        public const double WindowDisbursementsDem = %.2f;\n", $window{DEM})
    . sprintf("        /// <summary>DERIVED: the Republican nominee's committee's disbursements over the window, dollars.</summary>\n        public const double WindowDisbursementsRep = %.2f;\n", $window{REP})
    . sprintf("        /// <summary>DERIVED: the war chest - the two nominees' mean over the window, dollars (D-1 (c)'s equal chest).</summary>\n        public const double WarChestUsd = %.2f;\n\n", $chest)
    . "        /// <summary>Every saved response the run read, by its path under `ElectionsData/usa/`, with its SHA-256.</summary>\n"
    . "        public static readonly (string Path, string Sha256)[] RawSources = BuildRawSources();\n\n"
    . "        /// <summary>The CSV's rows: kind (cycle, purpose, report), committee, party key, label, coverage, dollars, days inside the window, days.</summary>\n"
    . "        public static readonly (string Kind, string Committee, string Party, string Label, string From, string To, double Amount, string DaysInWindow, string Days)[] Rows = BuildRows();\n\n"
    . "        private static (string Path, string Sha256)[] BuildRawSources() => new (string, string)[]\n        {\n"
    . join('', map { "            (\"$_\", \"$read{$_}\"),\n" } sort keys %read) . "        };\n\n"
    . "        private static (string Kind, string Committee, string Party, string Label, string From, string To, double Amount, string DaysInWindow, string Days)[] BuildRows() => new (string, string, string, string, string, string, double, string, string)[]\n        {\n"
    . join('', map { "            (" . join(', ', (map { cq($_) } @$_[0 .. 5]), $_->[6] =~ /\./ ? $_->[6] : "$_->[6].0", cq($_->[7]), cq($_->[8])) . "),\n" } @rows) . "        };\n    }\n}\n";
$cs =~ s/\n/\r\n/g;
for my $o ([ 'ElectionsData/usa/campaign_spending.csv', $csv ], [ 'Assets/Scripts/Elections/Generated/UsCampaignSpending.cs', $cs ]) {
    die "$o->[0]: not ASCII - nothing written\n" if $o->[1] =~ /[^\x00-\x7F]/;
}
for my $o ([ 'ElectionsData/usa/campaign_spending.csv', $csv ], [ 'Assets/Scripts/Elections/Generated/UsCampaignSpending.cs', $cs ]) {
    open my $w, '>:raw', $o->[0] or die "$o->[0]: $!\n"; print $w $o->[1]; close $w or die "$o->[0]: $!\n";
}
printf "window %s to %s: DEM %.2f, REP %.2f; the war chest (their mean) %.2f dollars\n", @WINDOW, $window{DEM}, $window{REP}, $chest;
print "report: " . join(' ', @$_) . "\n" for grep { $_->[0] eq 'report' } @rows;
for my $party ('DEM', 'REP') {
    my ($all) = map { $_->[6] } grep { $_->[0] eq 'cycle' && $_->[2] eq $party } @rows;
    printf "%s cycle %.2f; by purpose: %s\n", $party, $all, join(', ', map { sprintf('%s %.1f %%', $_->[3], 100 * $_->[6] / $all) } grep { $_->[0] eq 'purpose' && $_->[2] eq $party } @rows);
}
print "wrote ElectionsData/usa/campaign_spending.csv (" . scalar(@rows) . " rows), Assets/Scripts/Elections/Generated/UsCampaignSpending.cs\n";
