#!/usr/bin/env perl
# FIRST-TIME PARTIES IN RIKSDAG ELECTIONS, GENERATED FROM VALMYNDIGHETEN'S OWN FILES (2026-09-30, COMPLETED.md s681).
#   perl Tools/first_time_parties.pl > ElectionsData/sweden/first_time_parties.tsv
# Reads only the raw files under ElectionsData/sweden/raw/first_time/ and ElectionsData/sweden/2026/raw/ (fetched byte-exact from
# historik.val.se and resultat.val.se; the register is ElectionsData/sweden/first_time_parties.md). A party is FIRST-TIME at the first
# election 2006-2026 whose list names it, when no earlier list since 2002 names it - by abbreviation or by its name case- and
# punctuation-folded. Valmyndigheten's own "previous election" column is NOT used for newness: it marks a party new when its
# registration changed (Direktdemokraterna is "new" on the 2018 page and listed on the 2014 one). A renamed party can still read as
# new where no page carries the old name; on the 2010-2018 pages a previous-election figure also rules a party out (Liberalerna, formerly Folkpartiet). The gate reads the range, not one party.
use strict; use warnings; use Encode; use HTML::Entities; use JSON::PP;
binmode STDOUT, ':encoding(UTF-8)';
my $root = $0; $root =~ s{Tools[/\\]first_time_parties\.pl$}{}; $root = '.' if $root eq '';
my $raw = "$root/ElectionsData/sweden/raw/first_time";

sub slurp { my $f = shift; local $/; open my $h, '<:raw', $f or die "$f: $!"; my $d = <$h>; close $h; return $d; }
sub text_of { my $d = shift; return $d =~ /\xc3[\x80-\xbf]/ ? decode('UTF-8', $d) : decode('latin-1', $d); }
sub fold { my $n = lc shift; $n =~ s/\(.*?\)//g; $n =~ s/[^\p{L}\p{N}]+//g; return $n; }
sub cells { my $tr = shift; return map { my $x = decode_entities($_); $x =~ s/<[^>]*>//g; $x =~ s/\x{a0}/ /g; $x =~ s/^\s+|\s+$//g; $x } ($tr =~ /<t[dh][^>]*>(.*?)<\/t[dh]>/gis); }
sub pct { my $s = shift // ''; $s =~ s/%//; $s =~ s/,/./; return $s eq '' ? '' : $s; }
my $skip = qr/^(Giltiga|Ogiltiga|Antal r|Valdeltagande|Totalt|R\x{f6}st|\x{d6}vriga|Summa|Partibeteckning|Parti$)/i;

# year -> list of [abbrev, name, votes, share%]
my %lists;
{   # 2002: every row naming a party with its vote count (write-ins included - the base of "seen before", never a first-timer here)
    my $t = text_of(slurp("$raw/val2002_R_00.html"));
    for my $tr ($t =~ /<tr.*?<\/tr>/gis) { my @c = cells($tr); for my $i (0 .. $#c - 1) { if ($c[$i] =~ /\p{L}{3}/ && $c[$i + 1] =~ /^\d+$/ && $c[$i] !~ $skip) { push @{$lists{2002}}, ['', $c[$i], $c[$i + 1], '']; last; } } }
}
{   # 2006: Valmyndigheten's national tables - the parliamentary parties and the small-party page
    for my $f ("$raw/val2006_R_rike_roster.html", "$raw/val2006_R_rike_ovriga.html") {
        my $t = text_of(slurp($f));
        for my $tr ($t =~ /<tr.*?<\/tr>/gis) { my @c = cells($tr); next unless @c >= 3; my ($i) = grep { $c[$_] =~ /^\d+$/ } 0 .. $#c; next unless defined $i && $i > 0; my $name = $c[$i - 1]; next if $name =~ $skip || $name !~ /\p{L}{2}/;
            # the roster page sets 2002's figure beside 2006's: a party with one stood in 2002 and joins the base
            my @later = grep { $c[$_] =~ /^\d+$/ } $i + 1 .. $#c; push @{$lists{2002}}, ['', $name, $c[$later[0]], ''] if @later;
            push @{$lists{2006}}, ['', $name, $c[$i], pct($c[$i + 1])]; }
    }
}
for my $y (2010, 2014, 2018) {   # the full national page: abbreviation, name, votes, share, change, change, previous votes, previous share
    my $t = text_of(slurp("$raw/val${y}_R_rike.html"));
    for my $tr ($t =~ /<tr.*?<\/tr>/gis) { my @c = cells($tr); next unless @c >= 4 && $c[2] =~ /^\d+$/ && $c[1] ne '' && $c[1] !~ /%$/ && $c[1] !~ $skip && ($c[0] // '') !~ /^(\x{d6}VR|BLANK|OG|VDT)$/; push @{$lists{$y}}, [$c[0], $c[1], $c[2], pct($c[3]), ($c[6] // '') =~ /^\d+$/ ? 1 : 0]; }
}
for my $pair ([2022, "$raw/resultat_val2022_RD_S.json"], [2026, "$root/ElectionsData/sweden/2026/raw/resultat_val2026_RD_S.json"]) {
    my ($y, $f) = @$pair; my $j = decode_json(slurp($f)); my %once;
    my $walk; $walk = sub { my $n = shift; if (ref $n eq 'HASH') { if (exists $n->{partibeteckning} && exists $n->{antalRoster} && !$once{$n->{partibeteckning}}++) { my $ab = $n->{partiforkortning} // ''; push @{$lists{$y}}, [$ab, $n->{partibeteckning}, $n->{antalRoster}, $n->{andelRoster} // ''] unless $ab eq "\x{d6}VR"; } $walk->($_) for values %$n; } elsif (ref $n eq 'ARRAY') { $walk->($_) for @$n; } };
    $walk->($j);
}

my (%seenName, %seenAbbrev);
my $note = sub { my ($name) = @_; return $name =~ /Liberalerna/ ? 'renamed (Folkpartiet)' : ''; };
print "year\tabbrev\tparty\tvotes\tshare_pct\tsource\tnote\n";
for my $y (sort { $a <=> $b } keys %lists) {
    my @first;
    for my $r (@{$lists{$y}}) {
        my ($ab, $name, $votes, $share) = @$r; my $k = fold($name);
        my $seen = $seenName{$k} || ($ab ne '' && $seenAbbrev{$ab});
        push @first, $r if $y > 2002 && !$seen && !$r->[4];   # [4]: the page itself gives a previous-election figure (a renamed registration)
    }
    for my $r (@{$lists{$y}}) { $seenName{fold($r->[1])} = 1; $seenAbbrev{$r->[0]} = 1 if $r->[0] ne ''; }
    my $src = $y <= 2018 ? "historik.val.se ($y)" : "resultat.val.se ($y)";
    for my $r (sort { $b->[2] <=> $a->[2] } @first) { printf "%d\t%s\t%s\t%s\t%s\t%s\t%s\n", $y, $r->[0], $r->[1], $r->[2], $r->[3], $src, $note->($r->[1]); }
}
