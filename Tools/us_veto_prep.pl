#!/usr/bin/perl
# PS-6 US-10 - THE VETO RECORD: EVERY PUBLIC LAW AND EVERY VETOED MEASURE OF THE 115TH-119TH CONGRESSES, EACH CHAMBER'S FINAL AGREEMENT AND ITS
# PARTY SPLIT, EVERY OVERRIDE. Reads only files held to their digests:
#   - the GPO's BILLSTATUS bulk zips (hr, s, hjres, sjres; out of tree, ../PoliSim-captures/sources/usa_us10/billstatus/, each held to its line in
#     ElectionsData/usa/raw/vetoes/out_of_tree.txt) - a measure's own <actions> only (its related bills' and amendments' actions are not its own);
#   - the roll calls those actions name (out of tree, rolls/): the Clerk's XML for the House, the Senate's LIS XML through the Wayback Machine;
#   - the Senate's veto lists by president and the House Historian's counts (in tree, raw/vetoes/, held to SHA256SUMS.txt) - the cross-checks.
# Writes ElectionsData/usa/us_veto_measures.csv (one row a measure) - the extract Tools/us_veto_backtest.pl reads.
# Usage (from the project root):
#   perl Tools/us_veto_prep.pl rolls   - prints the roll calls the measures name, "House|Senate<TAB>url" a line (for Tools/us_veto_fetch.pl rolls)
#   perl Tools/us_veto_prep.pl         - builds the CSV; dies, listing every problem, on any mismatch
# A chamber's FINAL AGREEMENT is its last action, on or before the day the measure was presented, that agrees to the measure's text: a passage
# (by any method, suspension included), a concurrence in the other chamber's amendment, a conference report agreed to. Where that last agreement
# carries a roll call, the roll call's own question must be an agreement's, never a cloture, a motion to proceed, to table or to recommit; where it
# carries none, the method its words name (voice vote, unanimous consent) is recorded. THE PRESIDENT of a decision is the one in office on its day:
# the terms change at noon on 20 January (the 20th Amendment, as records_by_date.md derives it), a decision dated 20 January the incoming
# president's (DECLARED: every one is after noon); a decision before noon on 20 January 2017 is outside the window. The checks the item names:
# party sums from member votes equal each roll call's totals; each override's outcome is two thirds of those voting (yea and nay; CRS RS22654:
# "two-thirds of the Members voting, a quorum being present"); the vetoed measures are the Senate's lists, override by override.
use strict;
use warnings;
use utf8;
use IO::Uncompress::Unzip qw($UnzipError);
use Digest::SHA;
binmode STDOUT, ':encoding(UTF-8)';

my $mode = $ARGV[0] // 'build';
# the mutation suite (Tools/us_veto_mutations.sh) points these at its own copies; the zips may stay where they are (US10_BILLSTATUS)
my $RAW = $ENV{US10_RAW} // 'ElectionsData/usa/raw/vetoes';
my $OUT = $ENV{US10_OUT} // '../PoliSim-captures/sources/usa_us10';
my $CSV = $ENV{US10_CSV} // 'ElectionsData/usa/us_veto_measures.csv';
sub path_of { my $rel = shift; return $rel =~ m{^billstatus/(.+)$} && $ENV{US10_BILLSTATUS} ? "$ENV{US10_BILLSTATUS}/$1" : "$OUT/$rel"; }
die "run from the project root\n" unless -d $RAW;
my @problems;
sub problem { push @problems, $_[0]; }
sub sha_file { my $d = Digest::SHA->new(256); $d->addfile($_[0], 'b'); $d->hexdigest }

# ------------------------------------------------------------------ the digests
my %oot;
open my $m, '<', "$RAW/out_of_tree.txt" or die "$RAW/out_of_tree.txt: $!";
while (<$m>) { next if /^#/; chomp; my @f = split /\t/; $oot{$f[2]} = $f[0] if @f >= 3; }
close $m;
sub held_out { my $rel = shift; my $want = $oot{$rel} // die "$rel: not in out_of_tree.txt - fetch it first (Tools/us_veto_fetch.pl)\n"; my $got = sha_file(path_of($rel)); die "$rel: its digest $got is not out_of_tree.txt's $want\n" unless $got eq $want; }
my %sums;
open my $s, '<', "$RAW/SHA256SUMS.txt" or die;
while (<$s>) { chomp; my ($h, $f) = /^(\w{64}) \*(.+)$/ or next; $sums{$f} = $h; }
close $s;
sub held_in { my $f = shift; my $want = $sums{$f} // die "$f: not in SHA256SUMS.txt\n"; die "$f: digest differs from SHA256SUMS.txt\n" unless sha_file("$RAW/$f") eq $want; open my $h, '<:raw', "$RAW/$f" or die; local $/; my $t = <$h>; close $h; return $t; }
sub page_named { my $stem = shift; my @f = sort grep { /^\Q$stem\E/ } keys %sums; die "$stem: no page\n" unless @f == 1; return ($f[0], held_in($f[0])); }

# ------------------------------------------------------------------ the presidents
# the House Historian's table names each president by his Congresses; the terms' boundaries are noon, 20 January (the 20th Amendment)
my ($histFile, $hist) = page_named('history_house_presidential_vetoes');
(my $histText = $hist) =~ s/<[^>]+>/ /g; $histText =~ s/&nbsp;|&#160;/ /g; $histText =~ s/\s+/ /g;
my %histCount;   # president => [regular, pocket, total, overridden] as the Historian prints them
for my $p (['115th-116th', 'Donald J. Trump'], ['117th-118th', 'Joseph R. Biden, Jr.'], ['119th', 'Donald J. Trump']) {
    my ($cong, $name) = @$p;
    (my $congRe = quotemeta $cong) =~ s/\\-/(?:-|\\xe2\\x80\\x93)/;   # the page writes one range with an en dash (UTF-8 bytes), one with a hyphen
    $histText =~ /$congRe\s+\Q$name\E\s+(\d+|\.+)\s+(\d+|\.+)\s+(\d+)\s+(\d+|\.+)/ or die "$histFile: no row for $name ($cong)\n";
    my @cells = ($1, $2, $3, $4);   # copied before the map: a match inside it would reset $1..$4
    $histCount{$cong} = [map { /\d/ ? $_ : 0 } @cells];
}
my @PRESIDENTS = (
    { key => 'Trump 1', name => 'Donald J. Trump (first term)', party => 'R', from => '2017-01-20', to => '2021-01-20', hist => '115th-116th' },
    { key => 'Biden', name => 'Joseph R. Biden Jr.', party => 'D', from => '2021-01-20', to => '2025-01-20', hist => '117th-118th' },
    { key => 'Trump 2', name => 'Donald J. Trump (second term)', party => 'R', from => '2025-01-20', to => '9999-12-31', hist => '119th' },
);
sub president_on { my $d = shift; for my $p (@PRESIDENTS) { return $p if $d ge $p->{from} && $d lt $p->{to}; } return undef; }

# ------------------------------------------------------------------ a measure's record
sub tag { my ($x, $t) = @_; return $x =~ /<\Q$t\E>(.*?)<\/\Q$t\E>/s ? $1 : undef; }
sub unesc { my $t = shift // ''; $t =~ s/&quot;/"/g; $t =~ s/&apos;/'/g; $t =~ s/&lt;/</g; $t =~ s/&gt;/>/g; $t =~ s/&amp;/&/g; $t =~ s/\s+/ /g; $t =~ s/^ | $//g; return $t; }

my %TYPE = (HR => 'H.R.', S => 'S.', HJRES => 'H.J.Res.', SJRES => 'S.J.Res.');

# an action agreeing to the measure's text, by chamber; the prefixes the record repeats an action under are stripped first
sub norm { my $t = shift; $t =~ s/^(?:Passed\/agreed to in (?:House|Senate)|Resolving differences -- (?:House|Senate) actions|Conference report agreed to in (?:House|Senate)|Failed of passage\/not agreed to in (?:House|Senate)):\s*//; return $t; }
sub house_agrees {
    my $raw = shift;
    my $t = norm($raw);
    return 0 if $t =~ /objections of the President/;
    # the record's own summary of a passage - en bloc suspensions ("the following bills passed under suspension") carry no other marker
    return 1 if $raw =~ /^Passed\/agreed to in House:/;
    return $t =~ /^On passage\b.*?\b(?:Passed|Agreed to)\b/
        || $t =~ /^On motion to suspend the rules and (?:pass|agree to|concur in)\b.*?\bAgreed to\b/
        || ($t =~ /^On motion (?:that )?the House (?:suspend the rules and )?(?:agree|concur|recede)\b.*?\bAgreed to\b/ && $t !~ /\bdisagree\b/)
        || $t =~ /^On motion to (?:agree|concur) (?:in|to)\b.*?\bAgreed to\b/
        || $t =~ /^On motion to suspend the rules and recede\b.*?\bconcur\b.*?\bAgreed to\b/i
        || $t =~ /^On agreeing to the conference report\b.*?\bAgreed to\b/
        # s802 review: an agreement made by a special rule's adoption ("Pursuant to the provisions of H. Res. 1061, the House agreed to the Senate
        # amendment"; "House agreed to Senate amendment ... pursuant to H.Res. N") - no roll call on the measure itself (reading 9)
        || $t =~ /^Pursuant to the provisions of H\.\s?Res\.\s?\d+,?\s+(?:the )?House (?:agreed|concurred)\b/i
        # the Clerk's line garbled ("On motion that the House suspend the rules and Agreed to by the Yeas and Nays", H.R. 1917, 117th)
        || ($t =~ /^On motion that the House suspend the rules\b.*?\bAgreed to\b/ && $t !~ /\bdisagree\b/)
        || ($t =~ /^House agreed to (?:the )?Senate amendments?\b/i && $t !~ /\bdisagree\b/);
}
sub senate_agrees {
    my $raw = shift;
    my $t = norm($raw);
    return 0 if $t =~ /over veto|objections of the President/;
    return 1 if $raw =~ /^Passed\/agreed to in Senate:/;
    return $t =~ /^Passed Senate\b/
        || $t =~ /^Received in the Senate[^.]*?\bpassed\b/
        # any number of asides before the object ("Senate agreed, under the order of 3/22/2024, having achieved 60 votes in the affirmative, to the
        # House amendment"), and the record's capitals ("House Amendment") - s802 review
        || $t =~ /^Senate agreed\b[^.]*?\bto (?:the )?(?:House amendments?|conference report)\b/i
        || $t =~ /^Senate (?:receded from its amendment and )?concurred\b/i;
}
sub house_override { return $_[0] =~ /^(?:(?:Passed|Failed of passage) (?:in )?House over veto:?\s*)?(?:On passage, the objections of the President to the contrary notwithstanding|Two-thirds of the Members present having voted in the affirmative the bill is passed, the objections of the President)/; }
sub senate_override { return $_[0] =~ /^(?:Passed Senate over veto|Failed of passage in Senate over veto)\b/; }
sub method_of { my $t = shift; return $t =~ /voice vote/i ? 'voice vote' : $t =~ /unanimous consent|without objection/i ? 'unanimous consent' : 'no recorded vote'; }

sub kind_of {
    my ($type, $title) = @_;
    return 'CRA disapproval' if $title =~ /congressional disapproval under chapter 8 of title 5|\bdisapproving the rule submitted by/i;
    return 'arms-sale disapproval' if $title =~ /disapproval of the proposed (?:export|foreign military sale|sale|transfer|license|retransfer)|arms sales/i;
    return 'War Powers' if $title =~ /removal of United States Armed Forces from hostilities|War Powers/i;
    return 'national emergency' if $title =~ /national emergency declared by the President/i;
    return 'D.C. disapproval' if $title =~ /disapproving the action of the District of Columbia Council/i;
    return $type =~ /JRES/ ? 'joint resolution' : 'bill';
}

my @measures;
my %vetoedSeen;
for my $c (115 .. 119) {
    for my $t (qw(hr s hjres sjres)) {
        my $rel = "billstatus/BILLSTATUS-$c-$t.zip";
        held_out($rel);
        my $z = IO::Uncompress::Unzip->new(path_of($rel)) or die "$rel: $UnzipError\n";
        my $status = 1;
        while ($status > 0) {
            my $member = $z->getHeaderInfo()->{Name};
            my $x = do { local $/; <$z> } // '';
            $status = $z->nextStream();
            die "$rel: a stream failed after $member\n" if $status < 0;
            my ($acts) = $x =~ /\n    <actions>\n(.*?)\n    <\/actions>/s;
            next unless defined $acts;
            my ($lawsBlock) = $x =~ /\n    <laws>\n(.*?)\n    <\/laws>/s;
            my @laws = $lawsBlock ? ($lawsBlock =~ /<type>Public ?Law<\/type>\s*<number>([\d-]+)<\/number>/g) : ();
            my @items;
            while ($acts =~ /\n      <item>\n(.*?)\n      <\/item>/gs) {
                my $b = $1;
                my %a = (date => tag($b, 'actionDate'), time => tag($b, 'actionTime') // '', text => unesc(tag($b, 'text')), code => tag($b, 'actionCode') // '');
                if ($b =~ /<recordedVotes>(.*?)<\/recordedVotes>/s) {
                    my $rv = $1;
                    $a{roll} = tag($rv, 'rollNumber'); $a{url} = tag($rv, 'url'); $a{chamber} = tag($rv, 'chamber');
                    my @n = $rv =~ /<rollNumber>/g; problem("$member: an action names " . scalar(@n) . " roll calls") if @n > 1;
                }
                push @items, \%a;
            }
            my $vetoed = grep { $_->{text} =~ /^Vetoed by President/ } @items;
            my $pocket = grep { $_->{text} =~ /^Pocket Vetoed by President/ } @items;
            next unless @laws || $vetoed || $pocket;
            my ($num) = $x =~ /<bill>\s*<number>(\d+)<\/number>/s or die "$member: no number\n";
            my ($type) = $x =~ /<bill>.*?<type>(\w+)<\/type>/s or die "$member: no type\n";
            my ($cong) = $x =~ /<bill>.*?<congress>(\d+)<\/congress>/s;
            die "$member: congress $cong in the $c zip\n" unless $cong == $c;
            # the measure's own title: <bill>'s child at four spaces (the first <title> in the file can be a related bill's or a rule's)
            my ($ownTitle) = $x =~ /\n    <title>(.*?)<\/title>/s or die "$member: no title of its own\n";
            my $title = unesc($ownTitle);
            push @measures, { congress => $c, type => $type, number => $num, measure => "$TYPE{$type} $num", title => $title, laws => \@laws, items => \@items,
                              vetoed => $vetoed ? 1 : 0, pocket => $pocket ? 1 : 0, member => $member };
        }
    }
}

# ------------------------------------------------------------------ each measure's decision, final agreements, overrides
my @rolls;   # [chamber, url, rel]
sub roll_rel {
    my ($chamber, $url) = @_;
    if ($chamber eq 'House') { my ($y, $n) = $url =~ m{/evs/(\d{4})/(roll\d+\.xml)$} or die "$url: not a Clerk roll URL\n"; return "rolls/house/$y/$n"; }
    my ($cs, $n) = $url =~ m{/(vote\d{4})/(vote_\d+_\d_\d{5}\.xml)$} or die "$url: not a Senate roll URL\n"; return "rolls/senate/$cs/$n";
}
my %need;
for my $ms (@measures) {
    my @it = @{$ms->{items}};
    my ($signed) = sort map { $_->{date} } grep { $_->{text} =~ /^Signed by President/ } @it;
    my ($veto) = sort map { $_->{date} } grep { $_->{text} =~ /^(?:Pocket )?Vetoed by President/ } @it;
    my ($nosig) = sort map { $_->{date} } grep { $_->{text} =~ /without (?:his |the President's )?signature|^Sent to Archivist of the United States unsigned/i } @it;
    my ($outcome, $day);
    if ($veto) { $outcome = $ms->{pocket} ? 'pocket vetoed' : @{$ms->{laws}} ? 'vetoed, overridden' : 'vetoed'; $day = $veto; }
    elsif ($signed) { $outcome = 'signed'; $day = $signed; }
    elsif ($nosig) { $outcome = 'law without signature'; $day = $nosig; }
    else { problem("$ms->{member}: a law with no signature, veto or became-law action"); next; }
    $ms->{outcome} = $outcome; $ms->{day} = $day;
    my $pres = president_on($day);
    $ms->{president} = $pres;
    my @presented = sort map { $_->{date} } grep { $_->{text} =~ /^Presented to President/ } @it;
    my $cut = @presented ? $presented[-1] : $day;
    problem("$ms->{measure} ($ms->{congress}th): not presented before its decision") unless @presented;
    for my $ch ('House', 'Senate') {
        my @cand = grep { $_->{date} le $cut && (($ch eq 'House' && house_agrees($_->{text})) || ($ch eq 'Senate' && senate_agrees($_->{text}))) } @it;
        for (@cand) { problem("$ms->{measure} ($ms->{congress}th): a $ch agreement whose roll is the other chamber's") if $_->{roll} && $_->{chamber} ne $ch; }
        if (!@cand) { problem("$ms->{measure} ($ms->{congress}th): no $ch agreement before presentment"); next; }
        @cand = sort { $a->{date} cmp $b->{date} || ($a->{time} || '') cmp ($b->{time} || '') || ($a->{roll} // 0) <=> ($b->{roll} // 0) } @cand;
        my $last = $cand[-1];
        my @sameDay = grep { $_->{date} eq $last->{date} } @cand;
        my %rollsThatDay = map { ($_->{roll} // '-') => 1 } @sameDay;
        if (keys %rollsThatDay > 1 && grep { !$_->{roll} } @sameDay) { $ms->{ambiguous}{$ch} = 1; }   # a roll-call and an unrecorded agreement the same day: the roll call taken, listed
        $ms->{final}{$ch} = $last;
        if ($last->{roll}) { my $rel = roll_rel($ch, $last->{url}); $need{$rel} = [$ch, $last->{url}]; $last->{rel} = $rel; }
    }
    # s802 review, the structural guard: the agreement that acts last overall cannot be one "with an amendment" - the other chamber must agree
    # after it; where the last one found is, the other chamber's real final agreement is in words the matchers miss
    if ($ms->{final}{House} && $ms->{final}{Senate}) {
        my ($h, $s) = ($ms->{final}{House}, $ms->{final}{Senate});
        my $last = ($h->{date} cmp $s->{date} || ($h->{time} || '') cmp ($s->{time} || '')) >= 0 ? $h : $s;
        my $other = $last == $h ? $s : $h;
        my $lastCh = $last == $h ? 'House' : 'Senate';
        # the same day: the Senate's actions carry no time, so the chamber agreeing to the other's amendment is the later
        my $answered = $other->{date} eq $last->{date} && norm($other->{text}) =~ /\b$lastCh (?:amendments?|Amdts?\.?)\b/i;
        problem("$ms->{measure} ($ms->{congress}th): the $lastCh acts last ($last->{date}) with an amendment - the other chamber's final agreement is not read ('" . substr(norm($last->{text}), 0, 90) . "')")
            if norm($last->{text}) =~ /\bwith (?:an )?amendments?\b/i && norm($last->{text}) !~ /\bdisagree\b/ && !$answered;
    }
    for my $ch ('House', 'Senate') {
        my @ov = grep { ($ch eq 'House' && house_override($_->{text})) || ($ch eq 'Senate' && senate_override($_->{text})) } @it;
        next unless @ov;
        my %byRoll = map { ($_->{roll} // '-') => $_ } @ov;
        problem("$ms->{measure}: a $ch override with no roll call") if $byRoll{'-'};
        problem("$ms->{measure}: " . scalar(keys %byRoll) . " $ch override roll calls") if keys %byRoll > 1;
        my ($o) = values %byRoll;
        next unless $o->{roll};
        my $rel = roll_rel($ch, $o->{url}); $need{$rel} = [$ch, $o->{url}]; $o->{rel} = $rel;
        $ms->{override}{$ch} = $o;
    }
}

# the measures' own problems stop the run before any roll call is read - a final agreement misread names a roll call no list fetched
if (@problems) { print STDERR "PROBLEMS (", scalar(@problems), "):\n", map({ "  $_\n" } @problems); die "us_veto_prep: stopped before the roll calls\n"; }
if ($mode eq 'rolls') {
    print "$need{$_}[0]\t$need{$_}[1]\n" for sort keys %need;
    exit 0;
}

# ------------------------------------------------------------------ the roll calls
my %roll;
my %questions;
for my $rel (sort keys %need) {
    held_out($rel);
    open my $h, '<:raw:encoding(UTF-8)', "$OUT/$rel" or die "$rel: $!"; my $x = do { local $/; <$h> }; close $h;
    my %r = (rel => $rel, chamber => $need{$rel}[0]);
    if ($r{chamber} eq 'House') {
        $r{number} = tag($x, 'rollcall-num') + 0; $r{question} = unesc(tag($x, 'vote-question')); $r{result} = unesc(tag($x, 'vote-result'));
        $r{legis} = unesc(tag($x, 'legis-num')); $r{date} = unesc(tag($x, 'action-date'));
        my %tot;
        while ($x =~ /<totals-by-party>(.*?)<\/totals-by-party>/gs) {
            my $b = $1; my $p = tag($b, 'party');
            my $k = $p =~ /^Republican/ ? 'R' : $p =~ /^Democratic/ ? 'D' : $p =~ /^Independent/ ? 'I' : die "$rel: party '$p'\n";
            $tot{$k} = [map { tag($b, $_) + 0 } qw(yea-total nay-total present-total not-voting-total)];
        }
        my %sum;
        while ($x =~ /<recorded-vote>\s*<legislator\b([^>]*)>.*?<\/legislator>\s*<vote>(.*?)<\/vote>/gs) {
            my ($attr, $v) = ($1, $2);
            my ($p) = $attr =~ /party="(\w+)"/ or die "$rel: a member with no party\n";
            my $i = $v =~ /^(Yea|Aye)$/ ? 0 : $v =~ /^(Nay|No)$/ ? 1 : $v eq 'Present' ? 2 : $v eq 'Not Voting' ? 3 : die "$rel: a vote '$v'\n";
            $sum{$p}[$i]++;
        }
        for my $p (sort keys %tot) { my @a = map { $sum{$p}[$_] // 0 } 0 .. 3; problem("$rel: ${p}'s members sum to @a, its totals @{$tot{$p}}") unless "@a" eq "@{$tot{$p}}"; }
        for my $p (sort keys %sum) { problem("$rel: members of party $p with no total") unless $tot{$p}; }
        $r{party} = \%tot;
    } else {
        $r{number} = tag($x, 'vote_number') + 0; $r{question} = unesc(tag($x, 'question')); $r{result} = unesc(tag($x, 'vote_result'));
        my $doc = tag($x, 'document') // ''; $r{legis} = unesc((tag($doc, 'document_type') // '') . ' ' . (tag($doc, 'document_number') // ''));
        $r{date} = unesc(tag($x, 'vote_date'));
        $r{qtext} = unesc(tag($x, 'vote_question_text'));   # an en bloc vote's document is one measure; its question names them all
        my $cnt = tag($x, 'count') // die "$rel: no count\n";
        my @count = map { (tag($cnt, $_) // 0) + 0 } qw(yeas nays present absent);
        my %sum; my @all = (0, 0, 0, 0);
        while ($x =~ /<member>(.*?)<\/member>/gs) {
            my $b = $1; my $p = tag($b, 'party'); my $v = tag($b, 'vote_cast');
            my $i = $v =~ /^(Yea|Guilty)$/ ? 0 : $v =~ /^(Nay|Not Guilty)$/ ? 1 : $v eq 'Present' ? 2 : $v eq 'Not Voting' ? 3 : die "$rel: a vote '$v'\n";
            $sum{$p}[$i]++; $all[$i]++;
            push @{$r{members}}, [unesc(tag($b, 'last_name')), tag($b, 'state'), $p, $i];
        }
        problem("$rel: members sum to @all, the count @count") unless "@all[0..2]" eq "@count[0..2]" && $all[3] == $count[3];
        $r{party} = { map { my $p = $_; ($p => [map { $sum{$p}[$_] // 0 } 0 .. 3]) } keys %sum };
    }
    $roll{$rel} = \%r;
}

# each roll call agrees with the action that names it: its measure (or, an en bloc Senate vote, its question naming it), its kind of question
my $enBlocCount = 0;
sub legis_key { my $l = uc shift; $l =~ s/[.\s]//g; return $l; }
for my $ms (@measures) {
    next unless $ms->{outcome};
    my $want = legis_key($ms->{measure});
    for my $ch ('House', 'Senate') {
        for my $what ('final', 'override') {
            my $a = $ms->{$what}{$ch} or next;
            next unless $a->{rel};
            my $r = $roll{$a->{rel}};
            my $enBloc = $ch eq 'Senate' && ($r->{qtext} // '') =~ /(?:^|\band\s|\s)\Q$ms->{measure}\E(?!\d)/ && legis_key($r->{legis}) ne $want;
            $enBlocCount++ if $enBloc;
            problem("$ms->{measure} ($ms->{congress}th) $ch $what: roll $r->{number} is on '$r->{legis}'") unless legis_key($r->{legis}) eq $want || $enBloc;
            problem("$ms->{measure} ($ms->{congress}th) $ch $what: roll $r->{number} is numbered $a->{roll} in the actions") unless $r->{number} == $a->{roll};
            my $q = $r->{question};
            my $ok = $what eq 'override'
                ? ($ch eq 'House' ? $q =~ /^(?:On )?Passage, Objections of the President (?:To The Contrary )?Not ?Withstanding/i : $q =~ /^On Overriding the Veto/i)
                : ($ch eq 'House' ? $q =~ /^On (?:the )?(?:Passage|Motion to Suspend the Rules and (?:Pass|Agree|Concur)|Agreeing to the (?:Senate Amendments?|Conference Report)|Motion to (?:Concur|Agree)|Concurring)/i
                                  : $q =~ /^On (?:Passage of the Bill|the Joint Resolution|the Resolution|the Conference Report|the Motion|Concurring|the Bill)/i);
            # the Senate's "On the Motion" names its motion only in the question's text: it must be a concurrence
            $ok = 0 if $ch eq 'Senate' && $q =~ /^On the Motion$/i && ($r->{qtext} // '') !~ /^On the Motion \((?:\w+ )?(?:Motion to Concur|Motion to Recede\b[^)]*?\bConcur|Senate Concurs)\b/i;
            problem("$ms->{measure} ($ms->{congress}th) $ch $what: roll $r->{number}'s question '$q' ('" . ($r->{qtext} // '') . "') is not an agreement's") unless $ok;
            problem("$ms->{measure} ($ms->{congress}th) $ch $what: roll $r->{number} is a cloture, a motion to proceed, to table or to recommit ('$q')") if $q =~ /cloture|proceed|table|recommit|commit/i && $q !~ /^On the Motion \(Motion to Concur/i;
            $questions{"$ch $what: $q"}++;
            if ($what eq 'override') {
                my ($y, $n) = (0, 0); for my $p (values %{$r->{party}}) { $y += $p->[0]; $n += $p->[1]; }
                my $passes = 3 * $y >= 2 * ($y + $n);
                my $says = $r->{result} =~ /^(Passed|Veto Overridden)/i ? 1 : $r->{result} =~ /^(Failed|Veto Sustained)/i ? 0 : die "$a->{rel}: result '$r->{result}'\n";
                problem("$ms->{measure} $ch override: $y-$n gives " . ($passes ? 'passed' : 'failed') . " at two thirds of those voting, the roll call says '$r->{result}'") unless $passes == $says;
                $ms->{overrideOk}{$ch} = $passes;
            }
        }
    }
}

# ------------------------------------------------------------------ the Senate's lists: the vetoed measures, override by override
my @vetoedRecord;
for my $stem ('senate_vetoes_TrumpDJ_', 'senate_vetoes_BidenJR_', 'senate_vetoes_TrumpDJ2_') {
    my ($f, $pg) = page_named($stem);
    my $cong;
    while ($pg =~ /<tr[^>]*>(.*?)<\/tr>/gs) {
        my $row = $1; $row =~ s/<[^>]+>/ /g; $row =~ s/&nbsp;|&#160;/ /g; $row =~ s/\s+/ /g;
        if ($row =~ /(\d{3})th Congress, \d\w+ Session/) { $cong = $1; next; }
        next unless $row =~ /^\s*((?:H\.R\.|S\.|H\.J\.Res\.|S\.J\.Res\.)\s?\d+)\b/;
        (my $id = $1) =~ s/\s//g;
        my @votes = $row =~ /vote No\. (\d+) \((\d+)-(\d+)\)/g;
        my $how = $row =~ /overridden/ ? 'overridden' : $row =~ /sustained/ ? 'sustained' : $row =~ /unchallenged/ ? 'unchallenged' : $row =~ /[Pp]ocket/ ? 'pocket' : die "$f: '$row'\n";
        push @vetoedRecord, { cong => $cong // die("$f: a row before any Congress\n"), id => $id, how => $how, votes => \@votes, page => $f };
    }
}
my %vetoedBill = map { (($_->{congress}) . ' ' . legis_key($_->{measure})) => $_ } grep { $_->{outcome} && $_->{outcome} =~ /veto/ } @measures;
my %recordKeys;
for my $v (@vetoedRecord) {
    my $k = "$v->{cong} " . legis_key($v->{id});
    $recordKeys{$k} = 1;
    my $ms = $vetoedBill{$k};
    if (!$ms) { problem("$v->{page}: $v->{id} ($v->{cong}th) is vetoed on the Senate's list but not in the bills' record"); next; }
    my @ov = map { $ms->{override}{$_} ? ($ms->{override}{$_}) : () } 'House', 'Senate';
    my @onList = @{$v->{votes}};
    my @mine = map { my $r = $roll{$_->{rel}}; my ($y, $n) = (0, 0); for my $p (values %{$r->{party}}) { $y += $p->[0]; $n += $p->[1]; } ($r->{number}, $y, $n) } @ov;
    problem("$v->{id} ($v->{cong}th): the Senate's list gives votes (@onList), the record's override roll calls (@mine)") unless "@onList" eq "@mine";
}
for my $k (sort keys %vetoedBill) { problem("$k: vetoed in the bills' record but not on the Senate's lists") unless $recordKeys{$k}; }

# the House Historian's counts per president
for my $p (@PRESIDENTS) {
    my @v = grep { $_->{outcome} && $_->{outcome} =~ /veto/ && $_->{president} && $_->{president}{key} eq $p->{key} } @measures;
    my $regular = grep { $_->{outcome} ne 'pocket vetoed' } @v;
    my $pocket = grep { $_->{outcome} eq 'pocket vetoed' } @v;
    my $over = grep { $_->{outcome} eq 'vetoed, overridden' } @v;
    my ($hr, $hp, $ht, $ho) = @{$histCount{$p->{hist}}};
    problem("$p->{name}: the record has $regular regular, $pocket pocket, $over overridden; the House Historian $hr, $hp, $ho") unless $regular == $hr && $pocket == $hp && $over == $ho && $regular + $pocket == $ht;
}

if (@problems) { print STDERR "PROBLEMS (", scalar(@problems), "):\n", map({ "  $_\n" } @problems); die "us_veto_prep: stopped\n"; }

# ------------------------------------------------------------------ the CSV
my @cols = qw(congress measure kind outcome decision_date president president_party law
    house_final house_final_date house_question house_R_yea house_R_nay house_R_present house_R_not_voting house_D_yea house_D_nay house_D_present house_D_not_voting house_I_yea house_I_nay house_I_present house_I_not_voting
    senate_final senate_final_date senate_question senate_R_yea senate_R_nay senate_R_present senate_R_not_voting senate_D_yea senate_D_nay senate_D_present senate_D_not_voting senate_I_yea senate_I_nay senate_I_present senate_I_not_voting senate_I_caucus
    override_house override_house_yea override_house_nay override_house_result override_senate override_senate_yea override_senate_nay override_senate_result same_day title);
sub csvq { my $v = shift // ''; return $v =~ /[",\n]/ ? '"' . ($v =~ s/"/""/gr) . '"' : $v; }

# an independent senator's caucus, by surname and state from senate_seats.csv (R-US10 (a); Sinema's a READING, G3)
my %caucus;
open my $ss, '<:encoding(UTF-8)', 'ElectionsData/usa/senate_seats.csv' or die;
my $sh;
while (<$ss>) { next if /^#/; chomp; s/\r$//; my @f = split /,/; if (!$sh) { $sh = 1; next; } my ($st, $cl, $name, $party, $indFrom, $took, $left, $howIn, $howOut, $cau) = @f; next unless $party =~ /I/; my @w = split / /, $name; @w = grep { !/^(Jr\.?|Sr\.?|II|III|IV)$/ } @w; $caucus{"$w[-1] $st"} = $cau; }
close $ss;

my @out;
my $window = 0; my %count; my $ambiguous = 0;
for my $ms (sort { $a->{congress} <=> $b->{congress} || $a->{type} cmp $b->{type} || $a->{number} <=> $b->{number} } @measures) {
    next unless $ms->{outcome};
    unless ($ms->{president}) { $count{'outside the window (before noon, 20 January 2017)'}++; next; }
    my %row = (congress => $ms->{congress}, measure => $ms->{measure}, kind => kind_of($ms->{type}, $ms->{title}), outcome => $ms->{outcome}, decision_date => $ms->{day},
               president => $ms->{president}{name}, president_party => $ms->{president}{party}, law => join(' ', @{$ms->{laws}}) || '-', title => $ms->{title},
               same_day => join(' ', grep { $ms->{ambiguous}{$_} } 'House', 'Senate') || '-');
    $ambiguous++ if $row{same_day} ne '-';
    for my $ch ('House', 'Senate') {
        my $lc = lc $ch; my $a = $ms->{final}{$ch};
        $row{"${lc}_final_date"} = $a->{date};
        if ($a->{rel}) {
            my $r = $roll{$a->{rel}};
            $row{"${lc}_final"} = "roll $r->{number}"; $row{"${lc}_question"} = $r->{question};
            for my $p (qw(R D I)) { my $t = $r->{party}{$p} // [0, 0, 0, 0]; @row{map { "${lc}_${p}_$_" } qw(yea nay present not_voting)} = @$t; }
            if ($ch eq 'Senate') {
                my %cc; for my $mb (@{$r->{members} // []}) { next unless $mb->[2] eq 'I'; my $cau = $caucus{"$mb->[0] $mb->[1]"} // problem("$a->{rel}: no seat row for independent $mb->[0] ($mb->[1])"); $cc{$cau // '?'}++; }
                $row{senate_I_caucus} = join(' ', map { "$_:$cc{$_}" } sort keys %cc) || '-';
            }
        } else {
            $row{"${lc}_final"} = method_of($a->{text}); $row{"${lc}_question"} = '-';
            for my $p (qw(R D I)) { @row{map { "${lc}_${p}_$_" } qw(yea nay present not_voting)} = ('-') x 4; }
            $row{senate_I_caucus} = '-' if $ch eq 'Senate';
        }
        my $o = $ms->{override}{$ch};
        if ($o) { my $r = $roll{$o->{rel}}; my ($y, $n) = (0, 0); for my $p (values %{$r->{party}}) { $y += $p->[0]; $n += $p->[1]; } @row{"override_$lc", "override_${lc}_yea", "override_${lc}_nay", "override_${lc}_result"} = ("roll $r->{number}", $y, $n, $ms->{overrideOk}{$ch} ? 'overridden' : 'sustained'); }
        else { @row{"override_$lc", "override_${lc}_yea", "override_${lc}_nay", "override_${lc}_result"} = ('-') x 4; }
    }
    $count{"$ms->{president}{key}: $ms->{outcome}"}++;
    push @out, join(',', map { csvq($row{$_}) } @cols);
}
die "us_veto_prep: stopped on the senators' caucus\n" if @problems;
open my $o, '>:raw:encoding(UTF-8)', $CSV or die;
print $o "# GENERATED by Tools/us_veto_prep.pl from the GPO's BILLSTATUS records and the roll calls they name (out of tree, held to ElectionsData/usa/raw/vetoes/out_of_tree.txt) - DO NOT EDIT; the record is ElectionsData/usa/veto_record.md.\n";
print $o "# PS-6 US-10: a row a public law or vetoed measure of the 115th-119th Congresses decided by a president in office from noon on 20 January 2017. house_final / senate_final: the chamber's final agreement - 'roll N' (the Clerk's or the Senate's roll call, its question and its party split: yea, nay, present, not voting; I independents, senate_I_caucus their caucus by senate_seats.csv) or the method its words name. override_*: the override roll call, two thirds of those voting. same_day: a chamber whose last agreement day carries both a roll-call agreement and an unrecorded one - the later by the record's own time taken, an unrecorded one without a time sorting first (veto_record.md lists each).\n";
print $o join(',', @cols), "\n";
print $o "$_\n" for @out;
close $o;
print "$CSV: ", scalar(@out), " measures; ", join('; ', map { "$_ $count{$_}" } sort keys %count), "; same-day ambiguities $ambiguous; en bloc Senate roll calls taken $enBlocCount\n";
print "questions taken:\n", map({ "  $questions{$_}  $_\n" } sort keys %questions);
