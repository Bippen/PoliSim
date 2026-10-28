#!/usr/bin/perl
# ITALY'S COUNT: THE CATALOG THE GAME'S CAMERA AND SENATO ARE COUNTED ON (PS-7; rulings K150, K151, K152, K158, docs/RULINGS_2026-10-23.md).
#
# Reads only files in the tree, byte-exact as fetched or generated (their digests are written into the catalog's header and constants):
#   ElectionsData/italy/collegi2020/{camera,senato}2018_su_collegi2020.csv  2018's comune returns on the 2020 colleges (Tools/it_collegi_prep.pl, K150)
#   ElectionsData/italy/collegi2020/{camera,senato}2022_liste.csv           2022 per college: each list's cifra elettorale di collegio (art. 77 lett. c)
#   ElectionsData/italy/collegi2020/{camera,senato}2022_candidati.csv       2022 per college: each candidate's votes and linked lists (lett. a/b)
#   ElectionsData/italy/raw/opendata2018/camera-20180304.zip                the Ministry's national 2018 list votes (Camera2018_Liste_Italia_Liv_Naz.txt)
#   ElectionsData/italy/raw/opendata2022/{camera,senato}-20220925.zip       the 2022 Estero list votes per country (the *EsteroListe_livNaz.xlsx)
#   ElectionsData/italy/raw/seats_2022/gu_2022-07-21_169_sg.pdf             the Gazzetta Ufficiale of 21 Jul 2022: the two DPR assigning the seats
#                                                                           (Tabella A - each circoscrizione's / region's seats; Tabella C - the Estero
#                                                                           ripartizioni's citizens, from which L. 459/2001 art. 6 c.2 gives their seats)
#   ElectionsData/italy/raw/reseat_2022/eligendo_camera_2022_area-italia.html, raw/records/eligendo_senato_2022_area-italia.html
#                                                                           Eligendo's published 2022 national cifre and seats (the backtest's reference)
#   ElectionsData/italy/raw/reseat_2022/dati_senato_leg19_{mandati,adesioni_gruppi}.csv, dati_senato_gruppi_denominazioni.csv
#                                                                           the senators for life of the XIX legislature and their groups (K154); the
#                                                                           Senato's uninominal winners by college and their groups (reading (6))
#   ElectionsData/italy/raw/reseat_2022/dati_camera_leg19_{mandati,adesioni_gruppi}.csv
#                                                                           the Camera's "maggioritario" winners and their groups (reading (6))
#   the opendata2022 zips' candidatilista files (camera2022_candidatilista.csv, Senato_2022_candlista.csv) - the list candidacies (reading (6))
# and writes Assets/Scripts/Elections/Generated/ItalianCount2022.cs.
#
# DECLARED readings (each stated once, here; the catalog's comments point back to this header):
#  (1) LINEAGE, 2018 list -> the 2022 roster's key: M5S, PD, LEGA -> Lega, FORZA ITALIA -> FI, FDI -> FdI, LIBERI E UGUALI -> AVS (SEL -> LeU -> AVS, the
#      partial lineage `ElectionsData/priors/previous_elections.md` names), +EUROPA -> PlusE, NOI CON L'ITALIA - UDC -> NM, SVP - PATT -> SVP. Every
#      other 2018 list maps to no key and is left out of the swing's base (Poland's reading: the base is the mapped lists' own sum). AzIV, IC and ScN
#      have no 2018 lineage.
#  (2) 2022 list -> key by the list's name (the roster's rows are the 2022 lists); a 2022 list the roster does not carry is summed into OTHER.
#  (3) A college's 2022 LINKS (K152) are its candidates' linked lists, read from the candidates file: each candidate's lists, mapped to keys, is one
#      group. The Trentino-Alto Adige and Valle d'Aosta joint lists are mapped to their members by name (a table below). A 2022 list that ran no
#      candidate of its own in a college is a group of its own there.
#  (4) The Valle d'Aosta college of each chamber votes on candidates only in both years (no list votes on disk): its seat is not counted from the
#      shares; the catalog carries the record's 2022 winner's group, which the count holds.
#  (5) An Estero joint list (LEGA - FORZA ITALIA - FRATELLI D'ITALIA) is carried as its members' group.
#  (6) Each 2022 college winner's own party (the coalition's candidacy agreement): the one list of its group; else the winner's list candidacy; else the
#      winner's parliamentary group on 27 Oct 2022 - a post-election fact read as a proxy for the candidacy (DECLARED). Unresolved colleges are left empty.
#
# Usage (from the project root):  perl Tools/it_count_prep.pl           writes the catalog
#                                 perl Tools/it_count_prep.pl --check   regenerates in memory and compares byte for byte
# ASCII only. Output LF (the catalog is marked -text in .gitattributes, so --check compares the bytes a checkout holds).
use strict; use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw($UnzipError);
use Compress::Zlib;

my $check = (@ARGV && $ARGV[0] eq '--check');
my $C = 'ElectionsData/italy/collegi2020';
my $R = 'ElectionsData/italy/raw';
my $OUT = 'Assets/Scripts/Elections/Generated/ItalianCount2022.cs';
my (%digest, @sources);

sub slurp { my $f = shift; open my $h, '<:raw', $f or die "$f: $!\n"; local $/; my $b = <$h>; close $h; unless (exists $digest{$f}) { $digest{$f} = sha256_hex($b); push @sources, $f; } return $b; }
sub member {
    my ($zip, $name) = @_;
    my $zb = slurp($zip);
    my $u = IO::Uncompress::Unzip->new(\$zb, Name => $name) or die "$zip: $name: $UnzipError\n";
    my ($b, $c) = ('');
    while ($u->read($c) > 0) { $b .= $c; }
    $u->close();
    die "$zip: $name is empty\n" if $b eq '';
    return $b;
}
sub table { my $f = shift; my @r; for my $l (split /\r?\n/, slurp($f)) { next if $l eq '' || $l =~ /^#/; push @r, [split /;/, $l, -1]; } return @r; }
sub norm { my $s = uc shift; $s =~ s{/.*$}{}; $s =~ s/[^A-Z0-9]//g; return $s; }

# ---- the keys: the 2022 roster's lists that the count carries, in the catalog's column order
my @KEYS = qw(FdI PD Lega M5S FI AzIV AVS NM SVP PlusE IC ScN MAIE UV);
my %KI; @KI{@KEYS} = (0 .. $#KEYS);

my %L2018 = ('MOVIMENTO 5 STELLE' => 'M5S', 'PARTITO DEMOCRATICO' => 'PD', 'LEGA' => 'Lega', 'FORZA ITALIA' => 'FI',
    "FRATELLI D'ITALIA CON GIORGIA MELONI" => 'FdI', 'LIBERI E UGUALI' => 'AVS', '+EUROPA' => 'PlusE', "NOI CON L'ITALIA - UDC" => 'NM', 'SVP - PATT' => 'SVP');
my %L2022 = ("FRATELLI D'ITALIA CON GIORGIA MELONI" => 'FdI', 'PARTITO DEMOCRATICO - ITALIA DEMOCRATICA E PROGRESSISTA' => 'PD', 'MOVIMENTO 5 STELLE' => 'M5S',
    'LEGA PER SALVINI PREMIER' => 'Lega', 'FORZA ITALIA' => 'FI', 'AZIONE - ITALIA VIVA - CALENDA' => 'AzIV', 'ALLEANZA VERDI E SINISTRA' => 'AVS', '+EUROPA' => 'PlusE',
    'NOI MODERATI/LUPI - TOTI - BRUGNARO - UDC' => 'NM', 'SUDTIROLER VOLKSPARTEI (SVP) - PATT' => 'SVP', 'IMPEGNO CIVICO LUIGI DI MAIO - CENTRO DEMOCRATICO' => 'IC',
    'SUD CHIAMA NORD' => 'ScN', "MOVIMENTO ASSOCIATIVO ITALIANI ALL'ESTERO - MAIE" => 'MAIE');
# (3) the joint lists of the candidate-only and special colleges, by their members; matched before the single names
my @JOINT = (
    ["LEGA PER SALVINI PREM.-FORZA IT.-NOI MODERATI-FRAT. D'ITALIA", [qw(FdI Lega FI NM)]],
    ['CAMPOBASE +EUROPA-ALL.VERDI E SIN.', [qw(PD AVS PlusE AzIV)]],
    ['PARTITO DEMOCRATICO - +EUROPA - ALLEANZA VERDI E SINISTRA', [qw(PD PlusE AVS)]],
    ["VALLEE D?AOSTE ? AUTONOMIE PROGRES FEDERALISME", [qw(UV)]],
    ["LEGA PER SALVINI PREMIER - FORZA ITALIA - FRATELLI D'ITALIA", [qw(Lega FI FdI)]],
);

sub keysOfCandidate {
    my $lists = shift; my %k; my $rest = $lists;
    for my $j (@JOINT) { my $i = index($rest, $j->[0]); if ($i >= 0) { $k{$_} = 1 for @{$j->[1]}; substr($rest, $i, length $j->[0]) = ''; } }
    for my $name (sort { length $b <=> length $a } keys %L2022) { my $i = index($rest, $name); if ($i >= 0) { $k{$L2022{$name}} = 1; substr($rest, $i, length $name) = ''; } }
    return join '+', sort { $KI{$a} <=> $KI{$b} } keys %k;
}

# ---- per chamber: colleges, 2018 by lineage, 2022 cifre by key, the 2022 candidates' groups and the winner
my %ch;
for my $chamber (qw(camera senato)) {
    my @l22 = table("$C/${chamber}2022_liste.csv"); my $h22 = shift @l22;
    my @l18 = table("$C/${chamber}2018_su_collegi2020.csv"); my $h18 = shift @l18;
    my @cand = table("$C/${chamber}2022_candidati.csv"); shift @cand;
    my %i22; for my $i (0 .. $#$h22) { $i22{$h22->[$i]} = $i; }
    my %i18; for my $i (0 .. $#$h18) { $i18{$h18->[$i]} = $i; }
    my %row18; for my $r (@l18) { $row18{$r->[2]} = $r; }
    my %cands; for my $r (@cand) { push @{$cands{$r->[0]}}, [$r->[1], $r->[2], $r->[3]]; }
    my (@circ, %circIndex, @colleges);
    for my $r (@l22) {
        my ($circ, $college, $voto) = ($r->[0], $r->[2], $r->[4]);
        unless (exists $circIndex{$circ}) { $circIndex{$circ} = scalar @circ; push @circ, $circ; }
        my $r18 = $row18{$college} or die "$chamber: no 2018 row for $college\n";
        my @v18 = (0) x @KEYS; my $other18 = 0;
        for my $i (7 .. $#$h18) { my $k = $L2018{$h18->[$i]}; my $v = $r18->[$i] eq '' ? 0 : $r18->[$i]; if (defined $k) { $v18[$KI{$k}] += $v; } else { $other18 += $v; } }
        my @c22 = (0) x @KEYS; my $other22 = 0; my %present;
        for my $i (0 .. $#$h22) {
            next unless $h22->[$i] =~ /^C:(.*)$/; my $name = $1; my $v = $r->[$i]; next if !defined $v || $v eq '';
            my $k = $L2022{$name}; if (defined $k) { $c22[$KI{$k}] += $v; $present{$k} = 1 if $v > 0; } else { $other22 += $v; }
        }
        my $list = $cands{$college} or die "$chamber: no candidates for $college\n";
        my (@groups, %grouped); my ($best, $bestVotes, $bestName) = (-1, -1, q{});
        for my $c (@$list) {
            my $g = keysOfCandidate($c->[1]);
            if ($g ne '') { push @groups, $g; $grouped{$_} = 1 for split /\+/, $g; }
            if ($c->[2] > $bestVotes) { $bestVotes = $c->[2]; $best = $g; $bestName = $c->[0]; }
        }
        for my $k (@KEYS) { if (($present{$k} || $v18[$KI{$k}] > 0) && !$grouped{$k}) { push @groups, $k; } }   # (3): a key with votes and no candidacy stands alone
        die "$chamber: ${college}'s winner is no key's candidate\n" if $best eq '';
        push @colleges, { name => $college, circ => $circIndex{$circ}, v18 => \@v18, other18 => $other18, c22 => \@c22, other22 => $other22,
            groups => join('|', @groups), winner => $best, winnerName => $bestName, listless => ($voto ne 'liste' ? 1 : 0) };
    }
    $ch{$chamber} = { circ => \@circ, colleges => \@colleges };
}

# ---- the Gazzetta of 21 Jul 2022: Tabella A of each DPR (the seats each circoscrizione / region is assigned), Tabella C (the Estero ripartizioni)
my $pdf = slurp("$R/seats_2022/gu_2022-07-21_169_sg.pdf");
my @objects;
while ($pdf =~ /(\d+) 0 obj(.*?)stream\r?\n(.*?)endstream/gs) {
    my ($hdr, $s) = ($2, $3); next unless $hdr =~ /FlateDecode/;
    my $o = uncompress($s); next unless defined $o && $o =~ /\bT[Jj]\b/;
    my $t = '';
    while ($o =~ /\[(.*?)\]\s*TJ|\(((?:[^()\\]|\\.)*)\)\s*Tj|(T\*|Td|TD|ET)/gs) {
        if (defined $1) { my $a = $1; while ($a =~ /\(((?:[^()\\]|\\.)*)\)|(-?\d+\.?\d*)/g) { if (defined $1) { $t .= $1; } elsif ($2 < -200) { $t .= ' '; } } }
        elsif (defined $2) { $t .= $2; } else { $t .= "\n"; }
    }
    push @objects, $t;
}
sub objectWith { my $needle = shift; for my $t (@objects) { return $t if index($t, $needle) >= 0; } die "the Gazzetta's text holds no '$needle'\n"; }
sub num { my $s = shift; $s =~ s/\.//g; return $s + 0; }
# Tabella A rows: [index] NAME POPULATION QUOTIENT [REMAINDER] [*] TOTAL - a name the PDF broke over two lines is joined first
sub tabellaA {
    my ($text, $expectTotal) = @_;
    my @lines = grep { /\S/ } split /\n/, $text; my %seats;
    for (my $i = 0; $i < @lines; $i++) {
        my $l = $lines[$i]; $l =~ s/^\s+|\s+$//g;
        if ($l =~ /^(\d+\s+)?[A-Z][A-Z' \-]*\/[A-Z]*$/ &&$i + 1 < @lines && $lines[$i + 1] =~ /^[A-Z' \-\/]+\s[\d.]{5,}/) { $l .= $lines[++$i]; $l =~ s/\s+$//; }
        next unless $l =~ /^(?:\d+\s*)?([A-Z][A-Z0-9' \-\/]*?)\s+([\d.]{6,})\s+(\d+)\s+(.*)$/;
        my ($name, $rest) = ($1, $4); next if $name =~ /^ITALIA$/;
        my @n = ($rest =~ /([\d.]+)/g); my $total = $n[-1];
        $seats{norm($name)} = $total + 0;
    }
    my $sum = 0; $sum += $_ for values %seats;
    die "Tabella A: the seats sum to $sum, not $expectTotal\n" unless $sum == $expectTotal;
    return \%seats;
}
my $camA = tabellaA(objectWith('TABELLA A - ELEZIONE DELLA CAMERA DEI DEPUTATI'), 392);
die "Tabella A (Camera): the circoscrizione quotient is not printed as 151.616\n" unless objectWith('TABELLA A - ELEZIONE DELLA CAMERA DEI DEPUTATI') =~ /151\.616/;
my $senA = tabellaA(objectWith('TABELLA A - ELEZIONE DEL SENATO DELLA REPUBBLICA'), 196);
for my $chamber (qw(camera senato)) {
    my $A = $chamber eq 'camera' ? $camA : $senA; my @seats;
    for my $c (@{$ch{$chamber}{circ}}) { my $s = $A->{norm($c)}; die "$chamber: Tabella A names no seats for $c\n" unless defined $s; push @seats, $s; }
    die "$chamber: Tabella A carries a circoscrizione the colleges do not\n" unless scalar(@seats) == scalar(keys %$A);
    $ch{$chamber}{seats} = \@seats;
}
# Tabella C: the citizens resident abroad per ripartizione; L. 459/2001 art. 6 c.2 - one seat each, the rest by those numbers, quotient and remainders
my @RIP = ('EUROPA', 'AMERICA MERIDIONALE', 'AMERICA SETTENTRIONALE E CENTRALE', 'AFRICA ASIA OCEANIA ANTARTIDE');
my $tabC = objectWith('TABELLA C - ELEZIONE DELLA CAMERA DEI DEPUTATI');
my @citizens = (objectWith('TABELLA C - ELEZIONE DEL SENATO DELLA REPUBBLICA') =~ /([\d]{1,3}(?:\.\d{3})+)/g);
my %seen; @citizens = grep { !$seen{$_}++ } @citizens;   # each figure once, in the page's order
my @cit = map { num($_) } @citizens[0 .. 4];
die "Tabella C: the four ripartizioni's citizens do not sum to the printed total\n" unless $cit[0] + $cit[1] + $cit[2] + $cit[3] == $cit[4];
sub esteroSeats {
    my ($total) = @_; my @s = (1) x 4; my $rest = $total - 4; return \@s if $rest <= 0;
    my $q = int($cit[4] / $rest); my @rem; my $given = 0;
    for my $i (0 .. 3) { my $n = int($cit[$i] / $q); $s[$i] += $n; $given += $n; $rem[$i] = $cit[$i] - $n * $q; }
    for my $i (sort { $rem[$b] <=> $rem[$a] } 0 .. 3) { last if $given >= $rest; $s[$i]++; $given++; }
    return \@s;
}
my $camC = esteroSeats(8); my $senC = esteroSeats(4);
die "Tabella C: the Camera quotient is not printed as 1.451.517\n" unless $tabC =~ /1\.451\.517/;

# ---- the Estero's 2022 list votes per ripartizione (the xlsx, its shared strings and first sheet)
sub estero {
    my ($zip, $name) = @_;
    my $x = member($zip, $name);
    my $ss = do { my $z = IO::Uncompress::Unzip->new(\$x, Name => 'xl/sharedStrings.xml') or die "$name: $UnzipError\n"; my ($b, $c) = (''); while ($z->read($c) > 0) { $b .= $c; } $b };
    my $sh = do { my $z = IO::Uncompress::Unzip->new(\$x, Name => 'xl/worksheets/sheet1.xml') or die "$name: $UnzipError\n"; my ($b, $c) = (''); while ($z->read($c) > 0) { $b .= $c; } $b };
    my @s = ($ss =~ /<si>(?:<t[^>]*>)?(.*?)(?:<\/t>)?<\/si>/gs);
    my %v;
    while ($sh =~ /<row [^>]*>(.*?)<\/row>/gs) {
        my $r = $1; my @c;
        while ($r =~ /<c r="[A-Z]+\d+"([^>]*)>(?:<v>(.*?)<\/v>)?<\/c>/g) { my ($a, $x2) = ($1, $2); push @c, ($a =~ /t="s"/ ? $s[$x2] : $x2); }
        next if !@c || $c[0] eq 'DATAELEZIONE';
        $v{$c[1]}{$c[3]} += $c[4];
    }
    return \%v;
}
my %est;
$est{camera} = estero("$R/opendata2022/camera-20220925.zip", 'camera2022_EsteroListe_livNaz.xlsx');
$est{senato} = estero("$R/opendata2022/senato-20220925.zip", 'Senato2022_EsteroListe_livNaz.xlsx');
my %esteroLists;   # every Estero list of either chamber, by name
for my $chamber (qw(camera senato)) { for my $rip (keys %{$est{$chamber}}) { $esteroLists{$_} = 1 for keys %{$est{$chamber}{$rip}}; } }
for my $rip (keys %{$est{camera}}, keys %{$est{senato}}) { die "Estero: an unknown ripartizione '$rip'\n" unless grep { $_ eq $rip } @RIP; }
my @EL = sort keys %esteroLists;

# ---- 2018's national list votes (Area Italia), the history's prior
my %nat18; my $nat18Valid = 0;
for my $l (split /\r?\n/, member("$R/opendata2018/camera-20180304.zip", 'Camera2018_Liste_Italia_Liv_Naz.txt')) {
    my @f = map { my $x = $_; $x =~ s/^"|"$//g; $x } split /;/, $l; next if $f[0] eq 'DENOMINAZIONE';
    (my $v = $f[2]) =~ s/,\d+$//; $nat18Valid += $v; my $k = $L2018{$f[1]}; $nat18{$k} += $v if defined $k;
}

# ---- Eligendo's published 2022 national figures (Area Italia): each list's votes and seats; each coalition's uninominal seats
sub eligendo {
    my $f = shift; my $t = slurp($f); $t =~ s/<script.*?<\/script>//gs; $t =~ s/<[^>]+>/|/g; $t =~ s/&Uuml;/U/g; $t =~ s/\xc3\x9c/U/g; $t =~ s/\s+/ /g; $t =~ s/(\| ?)+/|/g;
    my ($body) = ($t =~ /Seggi\|(CANDIDATI UNINOMINALI .*?)\|TOTALE\|/) or die "$f: no results table\n";
    my @cells = split /\|/, $body; my (@votes, @seats, @uni); my ($other, $valid, $group) = (0, 0, -1);
    @votes = (0) x @KEYS; @seats = (0) x @KEYS;
    for (my $i = 0; $i < @cells; ) {
        my $c = $cells[$i]; $c =~ s/^\s+|\s+$//g;
        if ($c eq 'CANDIDATI UNINOMINALI') {
            my $n = 3; my $s = ($i + 3 < @cells && $cells[$i + 3] =~ /^\s*\d+\s*$/ && $cells[$i + 4] =~ /[A-Z]/) ? $cells[$i + 3] + 0 : 0;
            $n = 4 if $s > 0 || ($i + 3 < @cells && $cells[$i + 3] =~ /^\s*\d+\s*$/ && $cells[$i + 4] =~ /[A-Z]/);
            push @uni, [$s, []]; $i += $n; next;
        }
        my $name = $c; my $v = num($cells[$i + 1]); my $j = $i + 3; my $s = 0;
        if ($j < @cells && $cells[$j] =~ /^\s*\d+\s*$/) { $s = $cells[$j] + 0; $j++; }
        my $k = $L2022{$name}; if (defined $k) { $votes[$KI{$k}] = $v; $seats[$KI{$k}] = $s; push @{$uni[-1][1]}, $k; } else { $other += $v; }
        $valid += $v; $i = $j;
    }
    my @uniOut = map { [ $_->[0], join('+', @{$_->[1]}) ] } grep { $_->[0] > 0 } @uni;
    return { votes => \@votes, seats => \@seats, other => $other, valid => $valid, uni => \@uniOut };
}
my %pub;
$pub{camera} = eligendo("$R/reseat_2022/eligendo_camera_2022_area-italia.html");
$pub{senato} = eligendo("$R/records/eligendo_senato_2022_area-italia.html");
die "Eligendo Camera: the valid votes are not 28,098,196\n" unless $pub{camera}{valid} == 28098196;

# ---- the senators for life of the XIX legislature (K154): each mandate's dates and each group adhesion's, the group by its short title then
my @lifeMandates; my @lifeGroups;
{
    my $RS = "$R/reseat_2022";
    sub csv {
        my $f = shift; my @r;
        for my $l (split /\r?\n/, slurp($f)) {
            next if $l eq ''; my @x;
            while ($l =~ /\G(?:"((?:[^"]|"")*)"|([^,]*))(,|$)/g) { my ($q, $p, $sep) = ($1, $2, $3); my $v = defined $q ? $q : $p; $v =~ s/""/"/g; push @x, $v; last if $sep eq ''; }
            push @r, \@x;
        }
        return @r;
    }
    my @den = csv("$RS/dati_senato_gruppi_denominazioni.csv"); shift @den;
    my @man = csv("$RS/dati_senato_leg19_mandati.csv"); shift @man;
    my @ade = csv("$RS/dati_senato_leg19_adesioni_gruppi.csv"); shift @ade;
    sub unq { my $x = shift; $x = '' unless defined $x; $x =~ s/^"|"$//g; return $x; }
    sub ymd { my $x = unq(shift); return 0 if $x eq ''; $x =~ s/-//g; return $x + 0; }
    my %life;
    for my $m (@man) {
        my $tipo = unq($m->[7]); next unless $tipo =~ /a vita/;
        my $who = unq($m->[0]); my $name = unq($m->[2]) . ' ' . unq($m->[1]);
        $life{$who} = $name; push @lifeMandates, [$name, ymd($m->[5]), ymd($m->[6])];
    }
    for my $a (@ade) {
        my $who = unq($a->[0]); next unless exists $life{$who};
        my ($from, $until, $g) = (ymd($a->[4]), ymd($a->[5]), unq($a->[7]));
        my $short;
        for my $d (@den) { next unless unq($d->[0]) eq $g; my ($i, $f) = (ymd($d->[3]), ymd($d->[4])); if ($i <= $from && ($f == 0 || $f >= $from)) { $short = unq($d->[2]); } }
        die "no title for $g on $from\n" unless defined $short;
        $short =~ s/\s*\(.*$//;
        push @lifeGroups, [$life{$who}, $short, $from, $until];
    }
    @lifeMandates = sort { $a->[0] cmp $b->[0] } @lifeMandates;
    @lifeGroups = sort { $a->[0] cmp $b->[0] || $a->[2] <=> $b->[2] } @lifeGroups;
    die "the senators for life: " . scalar(@lifeMandates) . " mandates, expected the six of 18 Oct 2022\n" unless @lifeMandates == 6;
}

# ---- (6) each 2022 college winner's own party (the coalition's candidacy agreement), by the first route that names exactly one key of the winning group:
#   a. the winning group holds one key;  b. the winner also stood on party lists (the Ministry's candidatilista files) - the one list of the group;
#   c. the winner's parliamentary group as the chamber's open data has it on 27 Oct 2022 (the first constitution settled - the Camera's Noi Moderati group
#      formed that day), by the group's name (FdI, Lega, FI, M5S, PD, Noi Moderati, AVS, Azione-IV; the Senato's short titles); the Camera's winners are
#      found by name (surname, first names) among its "maggioritario" mandates, the Senato's by the college its mandate names.
#   A college none of these resolves is left unnamed ("") and the count shares it by the declared Hare reading.
{
    my $RS = "$R/reseat_2022";
    sub csvq { my $b = shift; my @r; for my $l (split /\r?\n/, $b) { next if $l eq ''; my @x; while ($l =~ /\G(?:"((?:[^"]|"")*)"|([^,]*))(,|$)/g) { my ($q, $p, $sep) = ($1, $2, $3); my $v = defined $q ? $q : $p; $v =~ s/""/"/g; push @x, $v; last if $sep eq ''; } push @r, \@x; } shift @r; return @r; }
    sub foldname { my $s = uc shift; $s =~ s/\s*\(.*$//; $s =~ s/[^A-Z ]//g; $s =~ s/\s+/ /g; $s =~ s/^ | $//g; return $s; }
    sub onDay { my ($from, $until, $day) = @_; return $from <= $day && ($until == 0 || $until > $day); }
    my $DAY = 20221027;
    my %labelKey = ("FRATELLI D'ITALIA" => 'FdI', 'LEGA' => 'Lega', 'FORZA ITALIA' => 'FI', 'MOVIMENTO 5 STELLE' => 'M5S', 'PARTITO DEMOCRATICO' => 'PD', 'NOI MODERATI' => 'NM',
        'ALLEANZA VERDI' => 'AVS', 'AZIONE' => 'AzIV', 'FdI' => 'FdI', 'LSP' => 'Lega', 'PD-IDP' => 'PD', 'FI-BP' => 'FI', 'FIBP' => 'FI', 'M5S' => 'M5S', 'Az-IV' => 'AzIV', "Cd'I-NM" => 'NM');
    my $keyOfLabel = sub { my $l = shift; for my $p (sort { length $b <=> length $a } keys %labelKey) { return $labelKey{$p} if index($l, $p) == 0; } return undef; };
    my $dobKey = sub { my $c = shift; my ($d, $m, $y) = $c =~ m{\((\d+)/(\d+)/(\d+)\)}; return foldname($c) . " " . (defined $y ? ($d + 0) . "/" . ($m + 0) . "/$y" : ''); };
    # b. list candidacies, by name and birth date
    my %lists;
    for my $spec (['camera', "$R/opendata2022/camera-20220925.zip", 'camera2022_candidatilista.csv', 4, 5, 6, 7], ['senato', "$R/opendata2022/senato-20220925.zip", 'Senato_2022_candlista.csv', 5, 6, 7, 8]) {
        my ($chamber, $zip, $name, $iList, $iSur, $iFirst, $iDob) = @$spec;
        for my $l (split /\r?\n/, member($zip, $name)) {
            my @f = map { my $x = $_; $x =~ s/^"|"$//g; $x } split /;/, $l; next if $f[0] =~ /DATA/;
            next unless defined $f[$iDob];
            my ($d, $m, $y) = $f[$iDob] =~ m{(\d+)/(\d+)/(\d+)}; next unless defined $y;
            my $k = $L2022{$f[$iList]}; next unless defined $k;
            $lists{$chamber}{foldname("$f[$iSur] $f[$iFirst]") . " " . ($d + 0) . "/" . ($m + 0) . "/$y"}{$k} = 1;
        }
    }
    # c. the Camera: the winners' mandates by name, their groups on the day
    my %cameraGroup;
    {
        my %dep; for my $r (csvq(slurp("$RS/dati_camera_leg19_mandati.csv"))) { next unless ($r->[6] // '') eq 'maggioritario'; $dep{$r->[1]} = foldname("$r->[2] $r->[3]"); }
        my %first;
        for my $r (csvq(slurp("$RS/dati_camera_leg19_adesioni_gruppi.csv"))) {
            next unless exists $dep{$r->[0]}; my ($from, $until) = ($r->[3] + 0, ($r->[4] // '') eq '' ? 0 : $r->[4] + 0);
            my $label = $r->[6]; $label =~ s/\s*\(\d.*$//;
            if (onDay($from, $until, $DAY)) { $cameraGroup{$dep{$r->[0]}} = $label; }
            if (!exists $first{$r->[0]} || $from < $first{$r->[0]}[0]) { $first{$r->[0]} = [$from, $label]; }
        }
        for my $d (keys %first) { $cameraGroup{$dep{$d}} //= $first{$d}[1]; }
    }
    # c. the Senato: the winners' mandates by college, their groups' short titles on the day
    my %senatoGroup;
    {
        my %title; for my $r (csvq(slurp("$RS/dati_senato_gruppi_denominazioni.csv"))) { next unless $r->[0] =~ m{/gruppo/}; push @{$title{$r->[0]}}, [ymd($r->[3]), ymd($r->[4]), $r->[2]]; }
        my %college; for my $r (csvq(slurp("$RS/dati_senato_leg19_mandati.csv"))) { next unless ($r->[8] // '') =~ /uninominale/; (my $c = $r->[10]) =~ s/\s*\(.*$//; $c =~ s/\xC3[\x9C\xBC]/U/g; $college{$r->[0]} = uc $c; }   # the open data spells SUDTIROL with its U-umlaut, the catalog without
        for my $r (csvq(slurp("$RS/dati_senato_leg19_adesioni_gruppi.csv"))) {
            next unless exists $college{$r->[0]}; my ($from, $until) = (ymd($r->[4]), ymd($r->[5]));
            next unless onDay($from, $until, $DAY) || !exists $senatoGroup{$college{$r->[0]}};
            my $short; for my $t (@{$title{$r->[7]} || []}) { $short = $t->[2] if onDay($t->[0], $t->[1], $DAY); }
            $senatoGroup{$college{$r->[0]}} = $short if defined $short;
        }
    }
    for my $chamber (qw(camera senato)) {
        my %route;
        for my $c (@{$ch{$chamber}{colleges}}) {
            my @members = split /\+/, $c->{winner}; my %in; @in{@members} = ();
            my ($party, $via) = ('', 'unresolved');
            if (@members == 1) { ($party, $via) = ($members[0], 'one list'); }
            if ($party eq '') { my @k = grep { exists $in{$_} } keys %{$lists{$chamber}{$dobKey->($c->{winnerName})} || {}}; ($party, $via) = ($k[0], 'list candidacy') if @k == 1; }
            if ($party eq '') {
                my $label = $chamber eq 'camera' ? $cameraGroup{foldname($c->{winnerName})} : $senatoGroup{(split / \(/, uc $c->{name})[0]};
                my $k = defined $label ? $keyOfLabel->($label) : undef;
                ($party, $via) = ($k, 'parliamentary group') if defined $k && exists $in{$k};
            }
            $c->{party} = $party; $route{$via}++;
        }
        $ch{$chamber}{routes} = join(', ', map { "$_ $route{$_}" } sort keys %route);
    }
}

# ================================================================== write the catalog
my @o;
sub w { push @o, @_; }
sub longs { return join(', ', map { $_ + 0 } @_); }
sub str { my $s = shift; $s =~ s/\\/\\\\/g; $s =~ s/"/\\"/g; return "\"$s\""; }
w('// GENERATED by Tools/it_count_prep.pl. DO NOT EDIT BY HAND.', '//');
for my $f (@sources) { w("// Source : $f", "// SHA-256: $digest{$f}"); }
w('//', '// ItalianCountDiagnostic re-reads the derived tables\' digests against SourceDigests and runs the count on this catalog.', '',
  'namespace PoliSim.Elections.Generated', '{',
  '    /// <summary>PS-7 (rulings K150-K158): what Italy\'s Camera and Senato are counted on - the 2020 colleges with 2018\'s returns by lineage and 2022\'s cifre,',
  '    /// each college\'s 2022 links, the seats the DPR of 21 Jul 2022 assigned each circoscrizione and region, the Estero\'s 2022 lists, 2018\'s national list votes',
  '    /// and Eligendo\'s published 2022 figures. Generated, never hand-edited; the readings it rests on are declared in the generator\'s header.</summary>',
  '    public static class ItalianCount2022', '    {');
w('        /// <summary>Each source file and its SHA-256, in the order read.</summary>', '        public static readonly (string Path, string Sha256)[] SourceDigests =', '        {');
for my $f (@sources) { w("            (" . str($f) . ", \"$digest{$f}\"),"); }
w('        };', '');
w('        /// <summary>The roster keys the count carries - the columns of every per-key array here.</summary>', '        public static readonly string[] Keys = { ' . join(', ', map { str($_) } @KEYS) . ' };', '');
for my $chamber (qw(camera senato)) {
    my $P = $chamber eq 'camera' ? 'Camera' : 'Senato';
    my $d = $ch{$chamber};
    w("        /// <summary>$P: the circoscrizioni" . ($chamber eq 'senato' ? ' (regions)' : '') . " in the derived tables' order, and the seats Tabella A of the DPR of 21 Jul 2022 assigns each.</summary>");
    w("        public static readonly string[] ${P}Circoscrizioni = { " . join(', ', map { str($_) } @{$d->{circ}}) . ' };');
    w("        public static readonly int[] ${P}CircoscrizioneSeats = { " . longs(@{$d->{seats}}) . ' };', '');
    w("        /// <summary>$P: the 2020 uninominal colleges, each with its circoscrizione's index.</summary>");
    w("        public static readonly string[] ${P}Colleges =", '        {');
    w('            ' . str($_->{name}) . ',') for @{$d->{colleges}};
    w('        };');
    w("        public static readonly int[] ${P}CollegeCircoscrizione = { " . longs(map { $_->{circ} } @{$d->{colleges}}) . ' };', '');
    w("        /// <summary>$P: true for a college that voted on candidates only in 2022 (no list votes on disk).</summary>");
    w("        public static readonly bool[] ${P}CollegeCandidatesOnly = { " . join(', ', map { $_->{listless} ? 'true' : 'false' } @{$d->{colleges}}) . ' };', '');
    w("        /// <summary>$P: 2018's list votes on each college (K150), by lineage onto <see cref=\"Keys\"/>; and the 2018 lists no key carries.</summary>");
    w("        public static readonly long[][] ${P}2018 =", '        {');
    w('            new long[] { ' . longs(@{$_->{v18}}) . ' },') for @{$d->{colleges}};
    w('        };');
    w("        public static readonly long[] ${P}2018Other = { " . longs(map { $_->{other18} } @{$d->{colleges}}) . ' };', '');
    w("        /// <summary>$P: 2022's cifre elettorali di collegio (art. 77 c.1 lett. c) by <see cref=\"Keys\"/>; and the 2022 lists no key carries.</summary>");
    w("        public static readonly long[][] ${P}2022 =", '        {');
    w('            new long[] { ' . longs(@{$_->{c22}}) . ' },') for @{$d->{colleges}};
    w('        };');
    w("        public static readonly long[] ${P}2022Other = { " . longs(map { $_->{other22} } @{$d->{colleges}}) . ' };', '');
    w("        /// <summary>$P: each college's 2022 links (K152) - its candidates' lists as keys, '+' within a group, '|' between groups.</summary>");
    w("        public static readonly string[] ${P}Links =", '        {');
    w('            ' . str($_->{groups}) . ',') for @{$d->{colleges}};
    w('        };', '');
    w("        /// <summary>$P: each college's 2022 winner's own party (the generator's reading (6): the one list of the group, the winner's list candidacy, or the");
    w("        /// winner's parliamentary group on 27 Oct 2022); empty where none resolves - routes: $ch{$chamber}{routes}.</summary>");
    w("        public static readonly string[] ${P}WinnerParty2022 =", '        {');
    w('            ' . str($_->{party}) . ',') for @{$d->{colleges}};
    w('        };', '');
    w("        /// <summary>$P: the group of each college's 2022 winner (the candidate with the most votes, lett. a/b).</summary>");
    w("        public static readonly string[] ${P}Winner2022 =", '        {');
    w('            ' . str($_->{winner}) . ',') for @{$d->{colleges}};
    w('        };', '');
    my $p = $pub{$chamber};
    w("        /// <summary>$P: Eligendo's published 2022 national figures, Area Italia - each key's votes and proportional seats, the lists no key carries, the valid");
    w('        /// votes of all lists, and each coalition\'s or list\'s uninominal seats with its lists.</summary>');
    w("        public static readonly long[] ${P}Published2022Votes = { " . longs(@{$p->{votes}}) . ' };');
    w("        public static readonly int[] ${P}Published2022Seats = { " . longs(@{$p->{seats}}) . ' };');
    w("        public const long ${P}Published2022Other = $p->{other};");
    w("        public const long ${P}Published2022Valid = $p->{valid};");
    w("        public static readonly (int Seats, string Lists)[] ${P}Published2022Uninominal = { " . join(', ', map { "($_->[0], " . str($_->[1]) . ')' } @{$p->{uni}}) . ' };', '');
}
w('        /// <summary>The Estero\'s ripartizioni, the seats each elects (L. 459/2001 art. 6 c.2 on Tabella C\'s citizens), its 2022 lists and their votes.</summary>');
w('        public static readonly string[] EsteroRipartizioni = { ' . join(', ', map { str($_) } @RIP) . ' };');
w('        public static readonly int[] CameraEsteroSeats = { ' . longs(@$camC) . ' };');
w('        public static readonly int[] SenatoEsteroSeats = { ' . longs(@$senC) . ' };');
w('        public static readonly string[] EsteroLists = { ' . join(', ', map { str($_) } @EL) . ' };');
w('        /// <summary>Each Estero list\'s keys (\'+\' for a joint list\'s members), empty for a list no key carries.</summary>');
w('        public static readonly string[] EsteroListKeys = { ' . join(', ', map { str(keysOfCandidate($_)) } @EL) . ' };');
for my $chamber (qw(camera senato)) {
    my $P = $chamber eq 'camera' ? 'Camera' : 'Senato';
    w("        public static readonly long[][] ${P}Estero2022 =", '        {');
    for my $rip (@RIP) { w('            new long[] { ' . longs(map { $est{$chamber}{$rip}{$_} || 0 } @EL) . ' },'); }
    w('        };');
}
w('', '        /// <summary>2018\'s national list votes, Area Italia (the Ministry\'s Liv_Naz file), by lineage onto <see cref="Keys"/>, and all lists\' valid votes.</summary>');
w('        public static readonly long[] National2018Votes = { ' . longs(map { $nat18{$_} || 0 } @KEYS) . ' };');
w("        public const long National2018Valid = $nat18Valid;");
w('', '        /// <summary>K154: the senators for life of the XIX legislature - each mandate (from, until; dates as yyyymmdd, 0 while open), and each group adhesion');
w('        /// with the group\'s short title at its start (the Senato\'s open data).</summary>');
w('        public static readonly (string Name, int From, int Until)[] SenatorsForLife = { ' . join(', ', map { '(' . str($_->[0]) . ", $_->[1], $_->[2])" } @lifeMandates) . ' };');
w('        public static readonly (string Name, string Group, int From, int Until)[] SenatorsForLifeGroups = { ' . join(', ', map { '(' . str($_->[0]) . ', ' . str($_->[1]) . ", $_->[2], $_->[3])" } @lifeGroups) . ' };');
w('    }', '}');
my $text = join("\n", @o) . "\n";

if ($check) {
    my $have = do { open my $h, '<:raw', $OUT or die "$OUT: $!\n"; local $/; <$h> };
    if ($have eq $text) { print "ok: $OUT is current\n"; exit 0; }
    print "STALE: $OUT differs from a fresh generation\n"; exit 1;
}
open my $oh, '>:raw', $OUT or die "$OUT: $!\n"; print $oh $text; close $oh;
my $nc = scalar @{$ch{camera}{colleges}}; my $ns = scalar @{$ch{senato}{colleges}};
print "wrote $OUT: Camera $nc colleges / " . scalar(@{$ch{camera}{circ}}) . " circoscrizioni, Senato $ns colleges / " . scalar(@{$ch{senato}{circ}}) . " regions\n";
print "Camera Estero seats @$camC; Senato Estero seats @$senC\n";
print "Camera circoscrizione seats @{$ch{camera}{seats}}\nSenato region seats @{$ch{senato}{seats}}\n";
print "published Camera seats @{$pub{camera}{seats}} uni " . join(' ', map { "$_->[0]:$_->[1]" } @{$pub{camera}{uni}}) . "\n";
print "published Senato seats @{$pub{senato}{seats}} uni " . join(' ', map { "$_->[0]:$_->[1]" } @{$pub{senato}{uni}}) . "\n";
print "college winners' parties: Camera $ch{camera}{routes}; Senato $ch{senato}{routes}
";
print "Estero lists: " . join(' | ', map { $_ . ' => ' . keysOfCandidate($_) } @EL) . "\n";
