#!/usr/bin/perl
# SP-8 (2026-10-30; docs/specs/FRANCE_PRESIDENTIAL_START_PLAN.md, prereg P1-P4): FRANCE'S 2022 PRESIDENTIAL RECORD FOR THE RUNTIME - generated, never
# hand-edited. Reads, byte for byte as fetched (each file's SHA-256 is written into the output, so a re-run against a newer file shows in the diff):
#   ElectionsData/france/raw/presidential2022/resultats-par-niveau-{dpt,fe}-t{1,2}-france-entiere.txt   the Interior Ministry's definitive count
#        (data.gouv.fr; ISO-8859-1, ';'), by departement and for France entiere
#   ElectionsData/france/raw/executive/cc_2022-195PDR_premier-tour.html      [CC-195] the Conseil constitutionnel's first-round declaration
#   ElectionsData/france/raw/records/cc_2022-197PDR_proclamation.html        [CC-197] its proclamation of the run-off
#   ElectionsData/france/raw/presidential2022/cc_2022-187PDR_liste-des-candidats.html   [CC-187] the list of candidates (the start's day)
#   ElectionsData/france/raw/presidential2022/cc_2017-169PDR_premier-tour.html, cc_2017-171PDR_proclamation.html   [CC-169] [CC-171] the 2017 rounds (the first term)
#   ElectionsData/france/raw/presidential2022/elysee_20220507_ceremonie-investiture.html [EL-INV] the investiture
#   ElectionsData/france/raw/presidential2022/ipsos_20220506_estimations-second-tour.html [IPSOS-T2] the run-off's 20:00 estimate
# and writes Assets/Scripts/Elections/Generated/FrenchPresidentialReturns.cs. Every figure and every date is read from those pages by the patterns
# below; a pattern that finds nothing stops the tool, which then writes nothing. It applies P1 (a) itself: the departement rows summed must equal the
# France-entiere row, field by field, or nothing is written. P1 (b) - the Conseil against the Interior - is printed, not enforced (the prereg's rule).
# FrenchPresidentialRecordCheck re-reads the same files and compares every figure with the catalog.
# Usage: perl Tools/s8_fr_prep.pl   (from the project root)
use strict; use warnings;
use Digest::SHA qw(sha256_hex);
use Encode qw(decode encode);

my $raw = 'ElectionsData/france/raw';
my $out = 'Assets/Scripts/Elections/Generated/FrenchPresidentialReturns.cs';
my %digest;

sub slurp {
    my ($path) = @_;
    open my $h, '<:raw', $path or die "$path: $!\n";
    local $/; my $b = <$h>; close $h;
    $digest{$path} = sha256_hex($b);
    return $b;
}

# a page's text as the record files quote it: tags stripped, entities decoded, whitespace runs collapsed
sub text_of {
    my ($bytes) = @_;
    my $s = decode('UTF-8', $bytes);
    $s =~ s/<script.*?<\/script>//gs; $s =~ s/<style.*?<\/style>//gs; $s =~ s/<[^>]+>/ /g;
    my %ent = (nbsp => ' ', '#160' => ' ', '#039' => "'", '#39' => "'", rsquo => "'", eacute => "\x{e9}", Eacute => "\x{c9}", egrave => "\x{e8}", ecirc => "\x{ea}", agrave => "\x{e0}", ccedil => "\x{e7}", amp => '&');
    $s =~ s/&(#?\w+);/exists $ent{$1} ? $ent{$1} : "&$1;"/ge;
    $s =~ s/\x{a0}|\x{202f}/ /g; $s =~ s/\s+/ /g;
    return $s;
}

my %month = (janvier => 1, "f\x{e9}vrier" => 2, mars => 3, avril => 4, mai => 5, juin => 6, juillet => 7, "ao\x{fb}t" => 8, septembre => 9, octobre => 10, novembre => 11, "d\x{e9}cembre" => 12);
sub fr_date {
    my ($d, $m, $y, $what) = @_;
    $d =~ s/er$//;
    die "$what: no month '$m'\n" unless $month{$m};
    return [$y + 0, $month{$m}, $d + 0];
}
sub need { my ($ok, $what) = @_; die "pattern not found: $what\n" unless $ok; }
sub num { my ($s) = @_; $s =~ s/\s//g; die "not a number: $s\n" unless $s =~ /^\d+$/; return $s + 0; }

# the display surname: each part of the Interior's upper-case surname capitalised ("LE PEN" Le Pen, "DUPONT-AIGNAN" Dupont-Aignan)
sub display_surname {
    my ($u) = @_;
    my $s = lc $u;
    $s =~ s/(^|[ \-])(\w)/$1\u$2/g;
    return $s;
}

# ---- the Interior's count: (round => { fe => {...}, dpt => [ {...}, ... ] })
my %count;
for my $round (1, 2) {
    for my $level ('fe', 'dpt') {
        my $path = "$raw/presidential2022/resultats-par-niveau-$level-t$round-france-entiere.txt";
        my $bytes = slurp($path);
        my @lines = split /\r?\n/, decode('ISO-8859-1', $bytes);
        my $head = shift @lines;
        my $panel = $head =~ /N\x{b0}Panneau/ ? 1 : 0;
        my $width = $panel ? 7 : 6;
        for my $line (@lines) {
            next if $line eq '';
            my @f = split /;/, $line, -1;
            my $row = { code => $f[0], name => $f[1], registered => num($f[3]), voters => num($f[6]), blank => num($f[8]), null => num($f[11]), cast => num($f[14]), votes => [] };
            my $i = 17;
            while ($i + $width - 1 <= $#f) {
                my @g = @f[$i .. $i + $width - 1];
                my ($p, $sexe, $nom, $prenom, $voix) = $panel ? @g[0 .. 4] : (0, @g[0 .. 3]);
                push @{ $row->{votes} }, { panel => $p + 0, surname => $nom, given => $prenom, votes => num($voix) };
                $i += $width;
            }
            die "$path: $row->{code} carries no candidate\n" unless @{ $row->{votes} };
            if ($level eq 'fe') { $count{$round}{fe} = $row; } else { push @{ $count{$round}{dpt} }, $row; }
        }
    }
    # P1 (a): the departement rows summed equal the France-entiere row, field by field, or nothing is written
    my $fe = $count{$round}{fe};
    my %sum;
    for my $r (@{ $count{$round}{dpt} }) {
        $sum{$_} += $r->{$_} for qw(registered voters blank null cast);
        $sum{"v:" . $_->{surname}} += $_->{votes} for @{ $r->{votes} };
    }
    for my $k (qw(registered voters blank null cast)) { die "P1 (a) FAILS: round $round $k - departements $sum{$k}, France entiere $fe->{$k}\n" unless $sum{$k} == $fe->{$k}; }
    for my $v (@{ $fe->{votes} }) { die "P1 (a) FAILS: round $round $v->{surname} - departements " . ($sum{"v:$v->{surname}"} // 0) . ", France entiere $v->{votes}\n" unless ($sum{"v:$v->{surname}"} // -1) == $v->{votes}; }
    printf "P1 (a) round %d: %d departement rows sum to France entiere on registered, voters, blank, null, cast and %d candidates - HOLDS\n", $round, scalar @{ $count{$round}{dpt} }, scalar @{ $fe->{votes} };
}

# ---- the Conseil's declarations: CC-195 (the first round) and CC-197 (the run-off)
my %cc;
for my $spec ([1, "$raw/executive/cc_2022-195PDR_premier-tour.html", 'CC-195'], [2, "$raw/records/cc_2022-197PDR_proclamation.html", 'CC-197']) {
    my ($round, $path, $id) = @$spec;
    my $t = text_of(slurp($path));
    my %r;
    need($t =~ /\x{c9}lecteurs inscrits : ([\d ]+?) Votants : ([\d ]+?) Bulletins blancs : ([\d ]+?) Bulletins nuls : ([\d ]+?) Suffrages exprim\x{e9}s : ([\d ]+?) Majorit\x{e9} absolue : ([\d ]+?) Ont obtenu : (.*?)(?: Article 2| Ainsi,)/, "$id totals");
    @r{qw(registered voters blank null cast majority)} = map { num($_) } ($1, $2, $3, $4, $5, $6);
    my $list = $7;
    while ($list =~ /(?:M\.|Mme) (.+?) ([\p{Lu}][\p{Lu}\- ]*[\p{Lu}]) : ([\d ]+?)(?= (?:M\.|Mme) |$)/g) { push @{ $r{votes} }, { given => $1, surname => $2, votes => num($3) }; }
    need(@{ $r{votes} // [] } >= 2, "$id candidates");
    need($t =~ /il a \x{e9}t\x{e9} proc\x{e9}d\x{e9} les (\d+) et (\d+) (\w+) (\d{4})/ || $t =~ /les (\d+) et (\d+) (\w+) (\d{4})/, "$id polling days");
    $r{polled} = fr_date($2, $3, $4, "$id polling day");   # the second of the two days: metropolitan France voted on it (the first is the overseas' and the Americas')
    need($t =~ /Rendu public le (\d+(?:er)?) (\w+) (\d{4})/, "$id rendu public");
    $r{proclaimed} = fr_date($1, $2, $3, "$id rendu public");
    $cc{$round} = \%r;
    printf "%s: round %d polled %04d-%02d-%02d, made public %04d-%02d-%02d, %d candidates, cast %d\n", $id, $round, @{ $r{polled} }, @{ $r{proclaimed} }, scalar @{ $r{votes} }, $r{cast};
}

# P1 (b): the Conseil against the Interior, printed (a difference is not a failure)
my $differences = 0;
for my $round (1, 2) {
    my ($fe, $c) = ($count{$round}{fe}, $cc{$round});
    for my $k (qw(registered voters blank null cast)) { if ($fe->{$k} != $c->{$k}) { $differences++; printf "P1 (b) round %d %s: the Interior %d, the Conseil %d\n", $round, $k, $fe->{$k}, $c->{$k}; } }
    my %byName = map { $_->{surname} => $_->{votes} } @{ $c->{votes} };
    for my $v (@{ $fe->{votes} }) {
        my $s = $v->{surname};
        die "P1 (b): round $round - the Conseil names no $s\n" unless exists $byName{$s};
        if ($byName{$s} != $v->{votes}) { $differences++; printf "P1 (b) round %d %s: the Interior %d, the Conseil %d\n", $round, $s, $v->{votes}, $byName{$s}; }
    }
    die "P1 (b): round $round - the Conseil names " . scalar(@{ $c->{votes} }) . " candidates, the Interior " . scalar(@{ $fe->{votes} }) . "\n" unless @{ $c->{votes} } == @{ $fe->{votes} };
}
printf "P1 (b): %d field(s) differ between the Conseil's proclamations and the Interior's France entiere\n", $differences;

# ---- the dates: the list of candidates, the 2017 term, the investiture, the 2022 term
my $t187 = text_of(slurp("$raw/presidential2022/cc_2022-187PDR_liste-des-candidats.html"));
need($t187 =~ /Liste des candidats \x{e0} l'\x{e9}lection pr\x{e9}sidentielle/ && $t187 =~ /Rendu public le (\d+(?:er)?) (\w+) (\d{4})/, 'CC-187 list of candidates, rendu public');
my $listed = fr_date($1, $2, $3, 'CC-187');
my $t171 = text_of(slurp("$raw/presidential2022/cc_2017-171PDR_proclamation.html"));
need($t171 =~ /aura lieu, au plus tard, le (\d+) (\w+) (\d{4}) \x{e0} 24 heures/, 'CC-171 the 2017 handover');
my $firstTermLatest = fr_date($1, $2, $3, 'CC-171');
need($t171 =~ /les (\d+) et (\d+) (\w+) (\d{4}), sont les suivants/, 'CC-171 the 2017 run-off days');
my $firstTermRunOff = fr_date($2, $3, $4, 'CC-171 run-off');
my $t169 = text_of(slurp("$raw/presidential2022/cc_2017-169PDR_premier-tour.html"));
need($t169 =~ /premier tour de scrutin/ && $t169 =~ /les (\d+) et (\d+) (\w+) (\d{4}), sont les suivants/, 'CC-169 the 2017 first round days');
my $firstTermFirstRound = fr_date($2, $3, $4, 'CC-169');
my $t197 = text_of(slurp("$raw/records/cc_2022-197PDR_proclamation.html"));
need($t197 =~ /Pr\x{e9}sident de la R\x{e9}publique fran\x{e7}aise \x{e0} compter du (\d+) (\w+) (\d{4}) \x{e0} 0 heure/, 'CC-197 the term from');
my $termFrom = fr_date($1, $2, $3, 'CC-197 term');
my $tinv = text_of(slurp("$raw/presidential2022/elysee_20220507_ceremonie-investiture.html"));
need($tinv =~ /Publi\x{e9} le (\d+) (\w+) (\d{4}) Emmanuel Macron a \x{e9}t\x{e9} officiellement investi Pr\x{e9}sident de la R\x{e9}publique/, 'EL-INV investiture');
my $invested = fr_date($1, $2, $3, 'EL-INV');
my $tips = text_of(slurp("$raw/presidential2022/ipsos_20220506_estimations-second-tour.html"));
need($tips =~ /estimations les plus justes \x{e0} 20h en annon\x{e7}ant une victoire d\x{2019}? ?Emmanuel Macron avec (\d+),(\d)% des suffrages/ || $tips =~ /20h en annon\x{e7}ant une victoire d.{0,3}Emmanuel Macron avec (\d+),(\d)% des suffrages/, 'IPSOS-T2 the 20:00 estimate');
my $estimate = "$1.$2";
printf "dates: listed %04d-%02d-%02d; first term from (latest) %04d-%02d-%02d; invested %04d-%02d-%02d; second term from %04d-%02d-%02d; Ipsos 20:00 run-off MACRON %s\n", @$listed, @$firstTermLatest, @$invested, @$termFrom, $estimate;

# ---- the C#
sub cq { my ($s) = @_; $s =~ s/\\/\\\\/g; $s =~ s/"/\\"/g; return '"' . $s . '"'; }
my $nl = "\r\n";
my $cs = '';
$cs .= "// GENERATED by Tools/s8_fr_prep.pl. DO NOT EDIT BY HAND.$nl//$nl";
for my $p (sort keys %digest) { $cs .= "// Source : $p$nl// SHA-256: $digest{$p}$nl"; }
$cs .= "//$nl// FrenchPresidentialRecordCheck re-reads these files and compares every figure.$nl$nl";
$cs .= "namespace PoliSim.Elections.Generated$nl\{$nl";
$cs .= "    /// <summary>SP-8 (prereg P1-P4, `docs/specs/FRANCE_PRESIDENTIAL_START_PLAN.md`): France's 2022 presidential election of record - SOURCED. The Interior Ministry's$nl";
$cs .= "    /// definitive count by departement and for France entiere (data.gouv.fr), the Conseil constitutionnel's two declarations (CC-195, CC-197), the list of$nl";
$cs .= "    /// candidates (CC-187), the 2017 proclamation (CC-171), the investiture (the Elysee) and the run-off's 20:00 estimate (Ipsos's own page). Generated, never hand-edited.</summary>$nl";
$cs .= "    public static class FrenchPresidentialReturns$nl    \{$nl";
$cs .= "        /// <summary>A round's totals: registered, voters, blank, null and cast (\"suffrages exprim\x{e9}s\").</summary>$nl";
$cs .= "        public readonly struct Totals$nl        \{$nl            public readonly int Round; public readonly long Registered, Voters, Blank, Null, Cast;$nl";
$cs .= "            public Totals(int round, long registered, long voters, long blank, long nulls, long cast) { Round = round; Registered = registered; Voters = voters; Blank = blank; Null = nulls; Cast = cast; \}$nl        \}$nl$nl";
$cs .= "        /// <summary>One candidate's votes in a round: the Interior's panel number (the Conseil's drawn order), the surname as the Interior writes it (the key),$nl";
$cs .= "        /// the surname for display, the given name, the votes.</summary>$nl";
$cs .= "        public readonly struct Vote$nl        \{$nl            public readonly int Round, Panel; public readonly string Surname, DisplaySurname, Given; public readonly long Votes;$nl";
$cs .= "            public Vote(int round, int panel, string surname, string displaySurname, string given, long votes) \{ Round = round; Panel = panel; Surname = surname; DisplaySurname = displaySurname; Given = given; Votes = votes; \}$nl        \}$nl$nl";
$cs .= "        /// <summary>One departement's (or the abroad's, or an overseas unit's) row: the Interior's code and name and its totals.</summary>$nl";
$cs .= "        public readonly struct Unit$nl        \{$nl            public readonly int Round; public readonly string Code, Name; public readonly long Registered, Cast;$nl";
$cs .= "            public Unit(int round, string code, string name, long registered, long cast) \{ Round = round; Code = code; Name = name; Registered = registered; Cast = cast; \}$nl        \}$nl$nl";
my $date = sub { my ($d) = @_; return sprintf('new System.DateTime(%d, %d, %d)', @$d); };
$cs .= "        /// <summary>[CC-187] the list of candidates, made public (\"Rendu public le\") - SP-8's start day (K174).</summary>$nl        public static readonly System.DateTime CandidatesListed = " . $date->($listed) . ";$nl";
$cs .= "        /// <summary>[CC-195] the first round's metropolitan polling day (the second of \"les 9 et 10 avril\") and the declaration made public.</summary>$nl";
$cs .= "        public static readonly System.DateTime FirstRound = " . $date->($cc{1}{polled}) . ", FirstRoundProclaimed = " . $date->($cc{1}{proclaimed}) . ";$nl";
$cs .= "        /// <summary>[CC-197] the run-off's metropolitan polling day and the proclamation made public.</summary>$nl";
$cs .= "        public static readonly System.DateTime RunOff = " . $date->($cc{2}{polled}) . ", RunOffProclaimed = " . $date->($cc{2}{proclaimed}) . ";$nl";
$cs .= "        /// <summary>[EL-INV] the investiture; [CC-197] the second term \"\x{e0} compter du ... \x{e0} 0 heure\"; [CC-171] the first term's start, \"au plus tard, le ... \x{e0} 24 heures\".</summary>$nl";
$cs .= "        public static readonly System.DateTime Invested = " . $date->($invested) . ", SecondTermFrom = " . $date->($termFrom) . ", FirstTermFromLatest = " . $date->($firstTermLatest) . ";$nl";
$cs .= "        /// <summary>[CC-169] the 2017 first round's metropolitan polling day; [CC-171] the 2017 run-off's - the first term's election.</summary>$nl";
$cs .= "        public static readonly System.DateTime FirstTermFirstRound = " . $date->($firstTermFirstRound) . ", FirstTermRunOff = " . $date->($firstTermRunOff) . ";$nl$nl";
$cs .= "        /// <summary>[IPSOS-T2] the run-off's 20:00 estimate for the first-named finalist (%), Ipsos-Sopra Steria's for France T\x{e9}l\x{e9}visions, as Ipsos's own page states it.$nl";
$cs .= "        /// The first round's 20:00 estimate is BILLED (no institute's own page reached; prereg P4): none is held.</summary>$nl";
$cs .= "        public const string RunOffEstimateSurname = \"MACRON\";$nl        public const double RunOffEstimate = $estimate;$nl        public const string RunOffEstimateBy = \"Ipsos-Sopra Steria\";$nl$nl";
for my $who (['Proclaimed', \%cc, 'the Conseil constitutionnel\'s declarations (CC-195, CC-197)'], ['Counted', undef, 'the Interior Ministry\'s France entiere rows']) {
    my ($name, $src, $words) = @$who;
    $cs .= "        /// <summary>Each round's totals, $words.</summary>$nl        public static readonly Totals[] ${name}Totals =$nl        \{$nl";
    for my $round (1, 2) {
        my $r = $src ? $src->{$round} : $count{$round}{fe};
        $cs .= "            new Totals($round, $r->{registered}L, $r->{voters}L, $r->{blank}L, $r->{null}L, $r->{cast}L),$nl";
    }
    $cs .= "        \};$nl$nl";
    $cs .= "        /// <summary>Each candidate's votes, $words; in the Interior's panel order.</summary>$nl        public static readonly Vote[] ${name}Votes =$nl        \{$nl";
    for my $round (1, 2) {
        my %ccv = $src ? (map { $_->{surname} => $_->{votes} } @{ $src->{$round}{votes} }) : ();
        for my $v (@{ $count{$round}{fe}{votes} }) {
            my $votes = $src ? $ccv{ $v->{surname} } : $v->{votes};
            $cs .= "            new Vote($round, $v->{panel}, " . cq($v->{surname}) . ", " . cq(display_surname($v->{surname})) . ", " . cq($v->{given}) . ", ${votes}L),$nl";
        }
    }
    $cs .= "        \};$nl$nl";
}
$cs .= "        /// <summary>The Interior's units by round: every departement, the overseas and the abroad, in the file's order.</summary>$nl        public static readonly Unit[] Units =$nl        \{$nl";
for my $round (1, 2) { for my $r (@{ $count{$round}{dpt} }) { $cs .= "            new Unit($round, " . cq($r->{code}) . ", " . cq($r->{name}) . ", $r->{registered}L, $r->{cast}L),$nl"; } }
$cs .= "        \};$nl$nl";
$cs .= "        /// <summary>Each unit's votes by round: (round, code, surname, votes).</summary>$nl        public static readonly (int Round, string Code, string Surname, long Votes)[] UnitVotes =$nl        \{$nl";
for my $round (1, 2) { for my $r (@{ $count{$round}{dpt} }) { for my $v (@{ $r->{votes} }) { $cs .= "            ($round, " . cq($r->{code}) . ", " . cq($v->{surname}) . ", $v->{votes}L),$nl"; } } }
$cs .= "        \};$nl    \}$nl\}$nl";

open my $o, '>:raw', $out or die "$out: $!\n";
print $o encode('UTF-8', $cs);
close $o;
printf "%s: %d bytes\n", $out, length(encode('UTF-8', $cs));
