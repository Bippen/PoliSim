#!/usr/bin/perl
# ITALY'S 2020 COLLEGES: THE 2022 RESULTS PER COLLEGE AND 2018'S COMUNE RETURNS RE-AGGREGATED ONTO THEM (PS-7; ruling K150, docs/RULINGS_2026-10-23.md).
#
# Reads only files in the tree, byte-exact as fetched (their digests in each folder's SHA256SUMS.txt):
#   ElectionsData/italy/raw/opendata2022/{camera,senato}-20220925.zip   the Ministry of the Interior's open data, results per comune (and per piece
#                                                                       of a comune split between colleges) with the 2020 college each piece voted in
#   ElectionsData/italy/raw/opendata2018/{camera,senato}-20180304.zip   the same open data for 2018, per comune (on the 2017 colleges)
#   ElectionsData/italy/raw/collegi2020/elenco-collegi-comuni-{camera,senato}.csv   the Ministry's comune-to-college table for the 2020 colleges
#   ElectionsData/italy/raw/collegi2020/Elenco-comuni-soppressi.zip, Variazioni-amministrative-e-territoriali-dal-1991.zip
#                                                                       ISTAT's per-comune successions (mergers, renamings, province transfers)
# and writes, per chamber (camera, senato), into ElectionsData/italy/collegi2020/:
#   <ch>2022_liste.csv        per 2020 uninominal college: electorate, voters, blank ballots, each list's votes, and its cifra elettorale di
#                             collegio uninominale (DPR 361/1957 art. 77 c.1 lett. c: list votes plus the candidate-only votes split by Hare largest
#                             remainders among the candidate's lists)
#   <ch>2022_candidati.csv    per college, each candidate's votes and linked lists (the uninominal result; lett. a/b)
#   <ch>2018_su_collegi2020.csv  2018's comune returns re-aggregated onto the 2020 colleges: electorate, voters, blank ballots, each 2018 list's
#                             votes, and the candidate-only votes; a comune split between 2020 colleges is apportioned by each piece's 2022
#                             electorate (integer largest remainders, so every comune's totals are kept exactly)
#   <ch>_comuni_2018_2022.csv the join actually used: each 2018 comune, the 2022 comune it counts in, and the route (same name / encoding
#                             wildcard / ISTAT suppression / ISTAT renaming or province transfer)
# The colleges that vote on candidates only (Valle d'Aosta; Trentino-Alto Adige's Senato colleges) carry no 2022 list votes: their result is the
# candidates file, and their row says so in its `voto` column.
#
# Usage (from the project root):  perl Tools/it_collegi_prep.pl            writes the files
#                                 perl Tools/it_collegi_prep.pl --check    regenerates in memory and compares byte for byte with the files on disk
#                                 perl Tools/it_collegi_prep.pl --istat <Collegi_Elettorali_BasiGeografiche.zip>   also cross-checks the 2022
#                                 returns' comune-to-college assignment against ISTAT's UT_Collegi2020 table (the zip is kept out of tree)
# ASCII only. Output LF.
use strict; use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw($UnzipError);
use POSIX qw(floor);

my $check = 0; my $istatZip;
while (@ARGV) { my $a = shift; if ($a eq '--check') { $check = 1; } elsif ($a eq '--istat') { $istatZip = shift; } else { die "unknown argument $a\n"; } }

my $R = 'ElectionsData/italy/raw';
my $OUT = 'ElectionsData/italy/collegi2020';
my %digest;

sub slurp { my $f = shift; open my $h, '<:raw', $f or die "$f: $!\n"; local $/; my $b = <$h>; close $h; $digest{$f} = sha256_hex($b); return $b; }
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
sub rows { my $b = shift; my @r; for my $l (split /\r?\n/, $b) { next if $l eq ''; push @r, [map { my $x = $_; $x =~ s/^"//; $x =~ s/"$//; $x } split /;/, $l, -1]; } return @r; }
# ISTAT's files quote fields that hold semicolons (the act's citation), so split them on the quoting
sub qrows {
    my $b = shift; my @r;
    for my $l (split /\r?\n/, $b) {
        next if $l eq ''; my @f;
        while ($l =~ /\G(?:"((?:[^"]|"")*)"|([^;]*))(;|$)/g) { my $v = defined $1 ? $1 : $2; $v =~ s/""/"/g; push @f, $v; last if $3 eq ''; }
        push @r, \@f;
    }
    return @r;
}
sub wild18 { my $s = shift; $s =~ s/[\x80-\xFF]/_/g; return $s; }   # the 2018 files lost their accented letters to stray bytes: each one a wildcard

# A name key: upper case, diacritics folded (Latin-1 and UTF-8), apostrophes, spaces and punctuation dropped.
sub nk {
    my $s = shift;
    $s =~ s/\xC3([\x80-\xBF])/chr(ord($1) + 0x40)/ge;                       # UTF-8 two-byte Latin-1 letters to Latin-1
    $s =~ s/\xDF/SS/g;                                                       # Latin-1 sharp s (ISTAT's 'Weinstrasse')
    $s =~ tr/\xC0-\xC5\xE0-\xE5\xC8-\xCB\xE8-\xEB\xCC-\xCF\xEC-\xEF\xD2-\xD6\xF2-\xF6\xD9-\xDC\xF9-\xFC\xC7\xE7\xD1\xF1/AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuCcNn/;
    $s = uc $s;
    $s =~ s/[\x80-\xFF]/_/g;                                                 # a byte the file's encoding lost: a wildcard
    $s =~ s/[^A-Z0-9_+]//g;
    return $s;
}
sub fold7 {   # Latin-1 accents to their letters, sharp s to ss, any other high byte to ?
    my $s = shift;
    $s =~ tr/\xC0-\xC5\xE0-\xE5\xC8-\xCB\xE8-\xEB\xCC-\xCF\xEC-\xEF\xD2-\xD6\xF2-\xF6\xD9-\xDC\xF9-\xFC\xC7\xE7\xD1\xF1/AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuCcNn/;
    $s =~ s/\xDF/ss/g; $s =~ s/[\x80-\xFF]/?/g;
    return $s;
}
sub wildre { my $k = shift; my $q = join '', map { ($_ eq '_' || $_ eq '+') ? '.{1,2}' : quotemeta($_) } split //, $k; return qr/^$q$/; }
sub ascii { my $s = shift; $s =~ s/\xC3\x9C/U/g; $s =~ s/\xC3\xBC/u/g; die "non-ASCII name: $s\n" if $s =~ /[^\x00-\x7f]/; return $s; }
sub ucode { my $s = shift; return $s =~ /\b(U\d\d)\b/ ? $1 : die "no college code in '$s'\n"; }

# Integer largest remainders: split $q over weights @w, ties to the earlier piece.
sub apportion {
    my ($q, @w) = @_;
    my $W = 0; $W += $_ for @w;
    return map { 0 } @w if $q == 0;
    die "zero weight for a non-zero quantity\n" if $W == 0;
    my @base = map { floor($q * $_ / $W) } @w;
    my $left = $q; $left -= $_ for @base;
    my @order = sort { ($q * $w[$b] / $W - $base[$b]) <=> ($q * $w[$a] / $W - $base[$a]) or $a <=> $b } 0 .. $#w;
    $base[$order[$_]]++ for 0 .. $left - 1;
    return @base;
}

# ---- ISTAT's successions dated 2017-01-01 .. 2022-09-25 (from 2017 because the 2018 file still spells some comuni merged before polling
# day by their old names; a name the 2018 file already uses is joined by name first) ----
my (%succ, %rename, %scorporo);   # nk(old name) -> [ new name, ... ]; nk(new comune) -> the comune it was carved out of
{
    my $b = member("$R/collegi2020/Elenco-comuni-soppressi.zip", 'Elenco comuni soppressi/Elenco comuni soppressi.csv');
    my @r = qrows($b); shift @r;
    for my $f (@r) {
        my ($d, $m, $y) = split m{/}, $f->[6]; next unless $y;
        my $k = sprintf('%04d%02d%02d', $y, $m, $d);
        next unless $k gt '20161231' && $k le '20220925';
        push @{$succ{nk($f->[4])}}, $f->[8];
    }
    $b = member("$R/collegi2020/Variazioni-amministrative-e-territoriali-dal-1991.zip", 'Variazioni amministrative e territoriali dal 1991/Variazioni_amministrative_territoriali_dal_01011991.csv');
    @r = qrows($b); shift @r;
    for my $f (@r) {
        next unless $f->[0] =~ /^\d+$/ && $f->[0] >= 2017 && $f->[0] <= 2022;
        if ($f->[1] eq 'CS' && $f->[11] =~ /scorporo/i) { push @{$scorporo{nk($f->[5])}}, $f->[9]; next; }
        next unless $f->[1] eq 'CD' || $f->[1] eq 'AP';
        my $new = ($f->[1] eq 'CD' && $f->[9] ne '') ? $f->[9] : $f->[5];
        push @{$rename{nk($f->[5])}}, $new;
    }
}

# The joins ISTAT's files record but no name rule reaches, each with the ISTAT record it rests on.
my %declared = (
    "camera|TRENTINO-ALTO ADIGE/S_DTIROL|S+N JAN DI FASSA" => ['SAN GIOVANNI DI FASSA', "ISTAT 022250: Sen Jan di Fassa-Sen Jan (fusion 01/01/2018) renamed San Giovanni di Fassa-Sen Jan 07/12/2018"],
    "senato|TRENTINO-ALTO ADIGE|S+N JAN DI FASSA" => ['SAN GIOVANNI DI FASSA', "ISTAT 022250: Sen Jan di Fassa-Sen Jan (fusion 01/01/2018) renamed San Giovanni di Fassa-Sen Jan 07/12/2018"],
);
my @report;
my %retSetG;   # per chamber: circoscrizione key | comune key | college code, as the 2022 returns assign them
sub nk1 { my @p = split m{[/-]}, shift; return nk($p[0]); }   # the first form of a bilingual or hyphenated name (ISTAT and the Ministry spell South Tyrol's differently)
sub ckey { my $k = nk(shift); $k = 'VALLEDAOSTA' if $k =~ /^VALLEDAOSTA/; $k = 'TRENTINOALTOADIGE' if $k =~ /^TRENTINOALTOADIGE/; return $k; }
sub say { my $l = join('', @_); push @report, $l; print $l, "\n"; }   # printed as it is measured, so a die still shows what led to it

for my $ch ('camera', 'senato') {
    my $C = $ch eq 'camera';
    # ---- the 2020 colleges, from the Ministry's table ----
    my $el = slurp("$R/collegi2020/elenco-collegi-comuni-$ch.csv");
    my (%collName, %collPluri, %collCirc, %elencoSet, @collOrder);
    my @er = rows($el); shift @er;
    for my $f (@er) {
        my ($circ, $pluri, $unin) = map { ascii($_) } @$f[0..2]; my $com = $f->[3];
        my $id = "$circ - " . ucode($unin);
        if (!exists $collName{$id}) { push @collOrder, $id; $collName{$id} = $unin; $collPluri{$id} = $pluri; $collCirc{$id} = $circ; }
        $elencoSet{nk($circ) . '|' . nk($com) . '|' . ucode($unin)} = 1;
    }
    my %circByKey; $circByKey{nk($collCirc{$_})} = $collCirc{$_} for @collOrder;
    say "$ch: ", scalar(@collOrder), " uninominal colleges in the Ministry's 2020 table";

    # ---- 2022 ----
    my $z22 = "$R/opendata2022/$ch-20220925.zip";
    my $main22 = member($z22, $C ? 'camera2022_Italia_LivComune.csv' : 'Senato_Italia_LivComune.csv');
    my (%piece, %pieceOrder, %coll22, %lists22, %cand22, %candLists, %candSeen, %pieceTot, %listTot22, %voto);
    my @r = rows($main22); shift @r;
    for my $f (@r) {
        my ($circ, $unin, $com) = ($f->[2], $f->[4], $f->[5]);
        my $cid = ($circByKey{nk($circ)} // die "2022 circoscrizione '$circ' not in the 2020 table\n") . ' - ' . ucode($unin);
        die "2022 college $cid not in the 2020 table\n" unless exists $collName{$cid};
        my $pk = "$circ|$com|$cid";
        if (!exists $piece{$pk}) {
            $piece{$pk} = $f->[6] + 0; push @{$pieceOrder{"$circ|$com"}}, $cid;
            $pieceTot{$cid}{elettori} += $f->[6]; $pieceTot{$cid}{votanti} += $f->[8]; $pieceTot{$cid}{bianche} += $f->[10];
        }
        my $list = $f->[12]; my $lv = $f->[11] eq '' ? 0 : $f->[11];
        $lists22{$cid}{$list} += $lv; $listTot22{$list} += $lv;
        my $cand = "$f->[13] $f->[14] ($f->[16])";
        $candLists{$cid}{$cand}{$list} = 1;
        $cand22{$cid}{$cand} += $f->[18] unless $candSeen{"$pk|$cand"}++;
        $voto{$cid} = 'liste';
    }
    # the colleges voting on candidates only, and their electorate
    my ($spec, $specScr) = $C ? ('camera2022_VAosta_LivComune.csv', 'camera2022_VAosta_LivComune_Scrutini.csv')
                              : ('Senato_VAosta&Trentino_LivComune.csv', 'Senato_VAosta&Trentino_livComune_Scrutini.csv');
    my %specEl;
    @r = rows(member($z22, $specScr)); my $h = shift @r;
    for my $f (@r) {
        if ($C) { $specEl{$f->[1]} = [$f->[2], $f->[4], $f->[6]]; }
        else { $specEl{$f->[8]} = [$f->[9], $f->[11], $f->[13]]; }
    }
    @r = rows(member($z22, $spec)); shift @r;
    for my $f (@r) {
        my ($reg, $coll, $com) = ($f->[2], $f->[3], $f->[4]);
        my ($circ) = grep { index(nk($_), nk($reg)) == 0 } values %circByKey; die "no circoscrizione for '$reg'\n" unless $circ;
        my $cid = "$circ - " . ucode($coll); die "special college $cid not in the 2020 table\n" unless exists $collName{$cid};
        my $pk = "$reg|$com|$cid";
        if (!exists $piece{$pk}) {
            my $e = $specEl{$com} // die "no electorate for $reg $com\n";
            $piece{$pk} = $e->[0] + 0; push @{$pieceOrder{"$reg|$com"}}, $cid;
            $pieceTot{$cid}{elettori} += $e->[0]; $pieceTot{$cid}{votanti} += $e->[1]; $pieceTot{$cid}{bianche} += $e->[2];
        }
        my $cand = "$f->[5] $f->[6] ($f->[7])"; $cand =~ s/ 00:00:00\)/)/;
        $candLists{$cid}{$cand}{$f->[11]} = 1;
        $cand22{$cid}{$cand} += $f->[10];
        $voto{$cid} = 'solo candidati';
    }
    my @miss = grep { !exists $voto{$_} } @collOrder; die "$ch 2022: colleges with no returns: @miss\n" if @miss;
    my $nPieces = keys %piece; my $nCom = keys %pieceOrder; my $nSplit = grep { @{$pieceOrder{$_}} > 1 } keys %pieceOrder;
    say "$ch 2022: $nCom comuni in $nPieces college pieces; ", $nSplit, " comuni split between colleges: ",
        join(', ', map { (split /\|/)[1] . ' (' . scalar(@{$pieceOrder{$_}}) . ')' } sort grep { @{$pieceOrder{$_}} > 1 } keys %pieceOrder);

    # cross-check: the returns' comune-to-college assignment against the Ministry's table
    my %retSet; for my $pk (keys %piece) { my ($c, $m, $cid) = split /\|/, $pk; $retSet{nk($collCirc{$cid}) . '|' . nk($m) . '|' . ucode($cid)} = 1; $retSetG{$ch}{ckey($collCirc{$cid}) . '|' . nk1($m) . '|' . ucode($cid)} = 1; }
    my @notEl = grep { !$elencoSet{$_} } sort keys %retSet; my @notRet = grep { !$retSet{$_} } sort keys %elencoSet;
    say "$ch cross-check, returns vs the Ministry's table: ", scalar(keys %retSet), " comune-college pairs in the returns, ", scalar(keys %elencoSet),
        " in the table; in the returns only: ", scalar(@notEl), (@notEl ? " (@notEl[0..($#notEl < 9 ? $#notEl : 9)])" : ''),
        "; in the table only: ", scalar(@notRet), (@notRet ? " (@notRet[0..($#notRet < 9 ? $#notRet : 9)])" : '');

    # the cifra elettorale di collegio uninominale (art. 77 c.1 lett. c)
    my (%cifra, %solo22);
    for my $cid (@collOrder) {
        next unless $voto{$cid} eq 'liste';
        for my $cand (sort keys %{$cand22{$cid}}) {
            my @ls = sort keys %{$candLists{$cid}{$cand}};
            my $sum = 0; $sum += $lists22{$cid}{$_} for @ls;
            my $solo = $cand22{$cid}{$cand} - $sum; die "$cid $cand: candidate votes below its lists' ($cand22{$cid}{$cand} < $sum)\n" if $solo < 0;
            $solo22{$cid} += $solo;
            $cifra{$cid}{$_} = $lists22{$cid}{$_} for @ls;
            next if $solo == 0;
            if (@ls == 1) { $cifra{$cid}{$ls[0]} += $solo; next; }
            die "$cid $cand: candidate-only votes with no list votes\n" if $sum == 0;
            my $quot = $sum / $solo; my @base; my $given = 0;
            for my $i (0..$#ls) { $base[$i] = floor($lists22{$cid}{$ls[$i]} / $quot); $given += $base[$i]; }
            my @ord = sort { ($lists22{$cid}{$ls[$b]} / $quot - $base[$b]) <=> ($lists22{$cid}{$ls[$a]} / $quot - $base[$a]) or $a <=> $b } 0..$#ls;
            $base[$ord[$_]]++ for 0 .. $solo - $given - 1;
            $cifra{$cid}{$ls[$_]} += $base[$_] for 0..$#ls;
        }
    }
    my @lists22 = sort { $listTot22{$b} <=> $listTot22{$a} or $a cmp $b } grep { $listTot22{$_} > 0 } keys %listTot22;
    my %natCifra; for my $cid (@collOrder) { $natCifra{$_} += $cifra{$cid}{$_} // 0 for @lists22; }
    say "$ch 2022 national sums over the colleges voting on lists (list votes / cifre di collegio):";
    say "  $_: $listTot22{$_} / $natCifra{$_}" for @lists22;

    # ---- 2018, per comune ----
    my $z18 = "$R/opendata2018/$ch-20180304.zip";
    my $m18 = member($z18, $C ? 'Camera2018_livComune.txt' : 'Senato2018_livComune.txt');
    my (%c18, %c18seenPiece, %c18seenCand, %c18cand, %c18candLists, %listTot18, @c18order);
    @r = rows($m18); shift @r;
    for my $f (@r) {
        my ($circ, $unin, $com) = ($f->[0], $f->[2], $f->[3]);
        my $ck = "$circ|$com"; push @c18order, $ck unless exists $c18{$ck};
        my $pk = "$ck|$unin";
        if (!$c18seenPiece{$pk}++) { $c18{$ck}{elettori} += $f->[4]; $c18{$ck}{votanti} += $f->[6]; $c18{$ck}{bianche} += $f->[8]; }
        my $lv = $f->[16] eq '' ? 0 : $f->[16];
        $c18{$ck}{L}{$f->[15]} += $lv; $listTot18{$f->[15]} += $lv;
        my $cand = "$pk|$f->[9] $f->[10] $f->[11]";
        $c18cand{$cand} = $f->[14] unless $c18seenCand{$cand}++;
        $c18candLists{$cand}{$f->[15]} += $lv;
    }
    for my $cand (keys %c18cand) {
        my ($circ, $com) = split /\|/, $cand;
        next if $cand =~ /\|CANDIDATO NON PRESENTE /;   # the 2018 file's row for a list whose college candidate was not on the ballot: list votes, no candidate
        my $s = 0; $s += $_ for values %{$c18candLists{$cand}};
        my $solo = $c18cand{$cand} - $s; die "2018 $cand: candidate votes below its lists'\n" if $solo < 0;
        $c18{"$circ|$com"}{solo} += $solo;
    }
    my @lists18 = sort { $listTot18{$b} <=> $listTot18{$a} or $a cmp $b } grep { $listTot18{$_} > 0 } keys %listTot18;

    # ---- the join, 2018 comune -> 2022 comune ----
    my %c22ByCirc; for my $k (keys %pieceOrder) { my ($c, $m) = split /\|/, $k; $c22ByCirc{nk($c)}{nk($m)} = $k; }
    my %c22All; for my $k (keys %pieceOrder) { my (undef, $m) = split /\|/, $k; push @{$c22All{nk($m)}}, $k; }
    my (%map, %route, %covered, @unres);
    my $circOf = sub {   # 2018 circoscrizione/region -> the 2022 returns' own spelling of it
        my $c = shift; my $k = nk($c);
        return "VALLE D'AOSTA" if $k eq 'AOSTA';
        return (grep { $_ eq $k } keys %c22ByCirc) ? $k : ((grep { $_ =~ wildre($k) || index($_, $k) == 0 } keys %c22ByCirc)[0] // die "2018 '$c' matches no 2022 circoscrizione\n");
    };
    my %circMemo;
    for my $ck (@c18order) {
        my ($c, $m) = split /\|/, $ck;
        my $c22k = $circMemo{$c} //= ($circOf->($c) eq "VALLE D'AOSTA" ? nk("VALLE D'AOSTA") : $circOf->($c));
        my $pool = $c22ByCirc{$c22k} // {};
        my $mk = nk(wild18($m)); my ($hit, $how);
        if (exists $pool->{$mk}) { ($hit, $how) = ($pool->{$mk}, 'same name'); }
        elsif ($mk =~ /[_+]/) { my $re = wildre($mk); my @h = grep { $_ =~ $re } keys %$pool; ($hit, $how) = ($pool->{$h[0]}, 'encoding wildcard') if @h == 1; }
        if (!$hit) {
            my $k = $mk; my %seen;
            while (!$hit && !$seen{$k}++) {
                my @keys = ($k);
                if (!$succ{$k} && !$rename{$k} && $k =~ /[_+]/) { my $re = wildre($k); @keys = grep { $_ =~ $re } (keys %succ, keys %rename); }
                my @next = map { (@{$succ{$_} // []}, @{$rename{$_} // []}) } @keys;
                my %u; @next = grep { !$u{nk($_)}++ } @next;
                last unless @next;
                my @cands = grep { exists $pool->{nk($_)} || exists $c22All{nk($_)} } @next;
                my @here = grep { exists $pool->{nk($_)} } @next;
                if (@here == 1) { ($hit, $how) = ($pool->{nk($here[0])}, ((grep { $succ{$_} } @keys) ? 'ISTAT suppression' : 'ISTAT renaming') . " -> $here[0]"); last; }
                if (!@here && @cands == 1 && @{$c22All{nk($cands[0])}} == 1) { ($hit, $how) = ($c22All{nk($cands[0])}[0], 'ISTAT province transfer -> ' . $cands[0]); last; }
                if (@next == 1) { $k = nk($next[0]); next; }
                last;
            }
        }
        if (!$hit && exists $declared{"$ch|$c|$m"}) { my ($n22, $why) = @{$declared{"$ch|$c|$m"}}; ($hit, $how) = ($pool->{nk($n22)}, "declared: $why") if exists $pool->{nk($n22)}; }
        if ($hit) { $map{$ck} = [$hit]; $route{$ck} = $how; $covered{$hit}++; } else { push @unres, $ck; }
    }
    # a 2022 comune carved out of a 2018 one (ISTAT 'CS ... scorporo'): the parent's 2018 returns cover both, apportioned by 2022 electorate
    for my $k22 (sort keys %pieceOrder) {
        next if $covered{$k22};
        my ($c22, $m22) = split /\|/, $k22;
        my @par = @{$scorporo{nk($m22)} // []}; next unless @par == 1;
        my $pk = $c22ByCirc{nk($c22)}{nk($par[0])} // next;
        my @kids = grep { $map{$_} && $map{$_}[0] eq $pk } keys %map; next unless @kids == 1;
        push @{$map{$kids[0]}}, $k22; $route{$kids[0]} .= " + ISTAT scorporo -> $m22"; $covered{$k22}++;
    }
    my @uncov = grep { !$covered{$_} } sort keys %pieceOrder;
    my %routeCount; $routeCount{($route{$_} =~ /^(ISTAT [a-z]+(?: [a-z]+)?|same name|encoding wildcard|declared)/)[0] . ($route{$_} =~ /scorporo/ ? ' + scorporo' : '')}++ for keys %route;
    say "$ch join 2018 -> 2022: ", scalar(@c18order), " comuni in 2018, ", scalar(keys %map), " joined (",
        join(', ', map { "$_ $routeCount{$_}" } sort keys %routeCount), "), unresolved: ", scalar(@unres), (@unres ? " (@unres)" : ''),
        "; 2022 comuni with no 2018 predecessor: ", scalar(@uncov), (@uncov ? " (@uncov)" : '');
    die "$ch: the join is incomplete - see above\n" if @unres || @uncov;

    # ---- 2018 on the 2020 colleges ----
    my (%on, %elettori18Tot);
    my @cols = ('elettori', 'votanti', 'bianche', 'solo', map { "L:$_" } @lists18);
    for my $ck (@c18order) {
        my (@cids, @w);
        for my $k22 (@{$map{$ck}}) { my ($c22, $m22) = split /\|/, $k22; for my $cid (@{$pieceOrder{$k22}}) { push @cids, $cid; push @w, $piece{"$c22|$m22|$cid"}; } }
        for my $col (@cols) {
            my $q = $col =~ /^L:(.*)/ ? ($c18{$ck}{L}{$1} // 0) : ($c18{$ck}{$col} // 0);
            my @parts = @cids == 1 ? ($q) : apportion($q, @w);
            $on{$cids[$_]}{$col} += $parts[$_] for 0..$#cids;
        }
    }
    my %tot18; for my $ck (@c18order) { $tot18{$_} += ($c18{$ck}{$_} // 0) for qw(elettori votanti bianche solo); $tot18{"L:$_"} += $c18{$ck}{L}{$_} // 0 for @lists18; }
    my %totOn; for my $cid (@collOrder) { $totOn{$_} += $on{$cid}{$_} // 0 for @cols; }
    my @bad = grep { $tot18{$_} != $totOn{$_} } @cols;
    say "$ch 2018 re-aggregation: national totals kept exactly on ", scalar(@cols) - scalar(@bad), " of ", scalar(@cols), " columns",
        (@bad ? " - FAILED: @bad" : ''), "; electorate $tot18{elettori}, voters $tot18{votanti}";
    die "$ch: totals not kept\n" if @bad;

    # ---- write ----
    my @srcs = ($z22, $z18, "$R/collegi2020/elenco-collegi-comuni-$ch.csv", "$R/collegi2020/Elenco-comuni-soppressi.zip", "$R/collegi2020/Variazioni-amministrative-e-territoriali-dal-1991.zip");
    my $hdr = "# GENERATED by Tools/it_collegi_prep.pl - do not edit by hand. Sources (SHA-256):\n" . join('', map { "#   $_ $digest{$_}\n" } @srcs);
    my %files;
    my $t = $hdr . "# $ch 2022 per 2020 uninominal college; V: = the list's votes, C: = its cifra elettorale di collegio uninominale (DPR 361/1957 art. 77 c.1 lett. c)\n";
    $t .= join(';', 'circoscrizione', 'collegio_plurinominale', 'collegio_uninominale', 'nome', 'voto', 'elettori', 'votanti', 'schede_bianche', 'voti_solo_candidato', (map { "V:$_" } @lists22), (map { "C:$_" } @lists22)) . "\n";
    for my $cid (@collOrder) {
        my $L = $voto{$cid} eq 'liste';
        $t .= join(';', $collCirc{$cid}, $collPluri{$cid}, $cid, $collName{$cid}, $voto{$cid}, map({ $pieceTot{$cid}{$_} } qw(elettori votanti bianche)),
            ($L ? $solo22{$cid} // 0 : ''), (map { $L ? $lists22{$cid}{$_} // 0 : '' } @lists22), (map { $L ? $cifra{$cid}{$_} // 0 : '' } @lists22)) . "\n";
    }
    $files{"$OUT/${ch}2022_liste.csv"} = $t;
    $t = $hdr . "# $ch 2022 candidates per 2020 uninominal college (art. 77 c.1 lett. a: the candidate's votes; lists joined by +)\n";
    $t .= "collegio_uninominale;candidato;liste;voti\n";
    for my $cid (@collOrder) {
        for my $cand (sort { $cand22{$cid}{$b} <=> $cand22{$cid}{$a} or $a cmp $b } keys %{$cand22{$cid}}) {
            $t .= join(';', $cid, $cand, join('+', sort keys %{$candLists{$cid}{$cand}}), $cand22{$cid}{$cand}) . "\n";
        }
    }
    $files{"$OUT/${ch}2022_candidati.csv"} = $t;
    $t = $hdr . "# $ch 2018 comune returns re-aggregated onto the 2020 uninominal colleges (ruling K150); a comune split between 2020 colleges is apportioned by its pieces' 2022 electorate, integer largest remainders\n";
    $t .= join(';', 'circoscrizione', 'collegio_plurinominale', 'collegio_uninominale', 'elettori', 'votanti', 'schede_bianche', 'voti_solo_candidato', @lists18) . "\n";
    for my $cid (@collOrder) { $t .= join(';', $collCirc{$cid}, $collPluri{$cid}, $cid, map { $on{$cid}{$_} // 0 } @cols) . "\n"; }
    $files{"$OUT/${ch}2018_su_collegi2020.csv"} = $t;
    $t = $hdr . "# $ch: each 2018 comune (as the 2018 file spells it, a byte its encoding lost written ?), the 2022 comune(s) it is counted in, and how the join was made (ISTAT's names with their accents folded)\n";
    $t .= "circoscrizione_2018;comune_2018;circoscrizione_2022;comune_2022;via\n";
    for my $ck (@c18order) { my ($c, $m) = split /\|/, $ck; my @t = map { [split /\|/] } @{$map{$ck}}; $t .= join(';', map { fold7($_) } $c, $m, $t[0][0], join('+', map { $_->[1] } @t), $route{$ck}) . "\n"; }
    $files{"$OUT/${ch}_comuni_2018_2022.csv"} = $t;

    for my $f (sort keys %files) {
        my $b = $files{$f}; die "$f: non-ASCII output\n" if $b =~ /[^\x00-\x7f]/;
        if ($check) {
            my $disk = -e $f ? do { open my $hh, '<:raw', $f or die; local $/; <$hh> } : '';
            say "check $f: ", ($disk eq $b ? 'IDENTICAL' : 'DIFFERS'), ' sha256 ', sha256_hex($b);
        } else {
            mkdir $OUT unless -d $OUT;
            open my $w, '>:raw', $f or die "$f: $!\n"; print $w $b; close $w or die "$f: $!\n";
            say "wrote $f sha256 ", sha256_hex($b);
        }
    }
}

# ---- optional: ISTAT's own comune-to-college table (out of tree) ----
if ($istatZip) {
    my $dbf = member($istatZip, 'Collegi_Elettorali_BasiGeografiche/UT_Collegi2020/UT_Collegi2020.dbf');
    my ($nrec, $hlen, $rlen) = unpack('x4 V v v', substr($dbf, 0, 12));
    my @fl; my $p = 32;
    while (substr($dbf, $p, 1) ne "\r") { my ($n, $ty, $len) = unpack('A11 a x4 C', substr($dbf, $p, 32)); $n =~ s/\0.*//; push @fl, [$n, $len]; $p += 32; }
    my %istat;
    for my $i (0 .. $nrec - 1) {
        my $rec = substr($dbf, $hlen + $i * $rlen, $rlen); my $q = 1; my %v;
        for my $x (@fl) { my $s = substr($rec, $q, $x->[1]); $q += $x->[1]; $s =~ s/^\s+|\s+$//g; $v{$x->[0]} = $s; }
        $istat{camera}{ckey($v{CIRC_DEN}) . '|' . nk1($v{DEN_COM}) . '|' . $v{CU20_C1}} = 1;
        $istat{senato}{ckey($v{DEN_REG}) . '|' . nk1($v{DEN_COM}) . '|' . $v{SU20_C1}} = 1;
    }
    say "ISTAT UT_Collegi2020.dbf ($digest{$istatZip} of the zip): $nrec territorial units";
    for my $ch ('camera', 'senato') {
        # the college codes U01.. are numbered within the circoscrizione (Camera) or region (Senato), so each key carries both
        my %m = %{$retSetG{$ch}}; my %ist = %{$istat{$ch}};
        my @onlyM = grep { !$ist{$_} } sort keys %m; my @onlyI = grep { !$m{$_} } sort keys %ist;
        say "ISTAT vs the 2022 returns ($ch, circoscrizione|comune|college code): ", scalar(keys %m), " pairs in the returns, ", scalar(keys %ist), " in ISTAT's table; returns only: ",
            scalar(@onlyM), (@onlyM ? " (@onlyM)" : ''), "; ISTAT only: ", scalar(@onlyI), (@onlyI ? " (@onlyI)" : '');
    }
}
