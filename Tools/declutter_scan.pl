#!/usr/bin/perl
# THE DECLUTTER SCAN (K321, Code's recommendation on Elias's K320 of 2026-10-30; docs/RULINGS_2026-10-23.md, docs/specs/UI_V33_SPEC.md s7).
# Reads the label tables the films write (DryGuiPass: PoliSim-captures/labels/<label>_<country>_<w>_labels.tsv for a dry film, <label>_labels.tsv
# for a real one) and finds the draws that break K320's caps.
#
#   perl Tools/declutter_scan.pl scan     [--legacy] [--out=<prefix>] <table>...   the breaches, by screen, most first; --out writes <prefix>.tsv
#   perl Tools/declutter_scan.pl baseline [--grow] <table>...                       writes docs/generated/DECLUTTER_BASELINE.tsv (K321 (c))
#   perl Tools/declutter_scan.pl judge    [--baseline=<file>] <table>...            each table against the baseline, as the film judges it (K321 (d))
#   perl Tools/declutter_scan.pl keys     [--legacy] <table>...                     every breach key, sorted - what parity compares
#   perl Tools/declutter_scan.pl selftest                                           the fixture (Tools/declutter_fixture/) against its expected keys
#   perl Tools/declutter_scan.pl parity   [--legacy] <table>...                     these keys against the film's own rules (Assets/Scripts/Testing/
#                                                                                   DeclutterRules.cs, run out of the engine by Tools/textcheck)
#
# A TABLE is a label table's path. A film since K321 writes its own facts beside it (<label>_film.tsv: country, width, real or dry, and its MODE -
# the choreography that decides what a frame shows), and they are read from there. A table without them (written before K321) is a dry film's by
# its name (<label>_<country>_<w>_labels.tsv, mode sweep), or a real film's named as <Country>=<path> or <Country>:<mode>=<path> (mode sweep by
# default); a real film can see Canvas text, so its captures cover the Canvas too. --legacy reads a table written before K321's flags: WRAP by the
# old height rule (a wrapped draw whose needed height over 1.25 type sizes exceeds 1.6), no CONTROL, CANVAS or slip draw.
#
# THE RULES (K321 (b), (e), (f)). The exemptions and the caps are Tools/declutter_rules.tsv - ONE file, read here and by the film's judge. A visible
# draw (not OUTSIDE), not a slip's own (SLIPHEAD / SLIPLINE / SLIPHEAD2 / SLIPLINE2), on a capture no exemption covers, is a breach when it is
#   CUT   ends in an ellipsis;               WRAP  runs past one line (MULTILINE) with at least wrap_words counted words;
#   LONG  more than long_words counted words; STOP  ends in a sentence stop with at least stop_words counted words (and is not CUT);
#   LABEL a CONTROL draw over label_words counted words.
# A counted word is a token (split at white space, the middle dot and the slash) holding a letter, so figures are uncounted. On a capture whose
# exemption is not 'all', a level-1 slip with more than slip_lines lines under its head, or a line over slip_line_words counted words, and a
# level-2 slip with more than slip2_lines lines, is a SLIP breach. A key is the capture stem and the table's text with every run of digits folded
# to '#', plus the kind; the baseline scopes it by country, width, mode and source (imgui, or canvas - a real film's Canvas text). One stem can
# show other words in another mode (the Desk at 01c in the sweep and in the interrupts film, months apart), so a film is judged by its own mode's rows.
#
# Exit code: 0 clean; 1 a breach judged NEW or STALE, a baseline that would grow without --grow, a selftest or parity mismatch; 2 a usage fault.
# Every mode prints one summary line beginning 'DECLUTTER: '.
use strict; use warnings;
use File::Basename qw(dirname);
use File::Spec;
use Encode ();
binmode STDOUT, ':encoding(UTF-8)';
binmode STDERR, ':encoding(UTF-8)';

my $ROOT = File::Spec->rel2abs(File::Spec->catdir(dirname(__FILE__), '..'));
my $RULES = "$ROOT/Tools/declutter_rules.tsv";
my $BASELINE = "$ROOT/docs/generated/DECLUTTER_BASELINE.tsv";
my $FIXTURE = "$ROOT/Tools/declutter_fixture";
my @CAPS = qw(label_words long_words wrap_words stop_words slip_lines slip_line_words slip2_lines);
my @KINDS = qw(CUT WRAP LONG STOP LABEL SLIP);
my @K321 = qw(MULTILINE CONTROL CANVAS SLIPHEAD SLIPLINE SLIPHEAD2 SLIPLINE2);
my $BASEHEAD = "country\twidth\tmode\tsource\tcapture\tkind\ttext";

my $mode = shift @ARGV // '';
my %opt; my @tables;
for my $a (@ARGV) { if ($a =~ /^--([a-z]+)(?:=(.*))?$/) { $opt{$1} = $2 // 1 } else { push @tables, $a } }
usage() unless $mode =~ /^(scan|baseline|judge|keys|selftest|parity)$/;
usage() if $mode ne 'selftest' && !@tables;

my $rules = load_rules($RULES);

if ($mode eq 'selftest') { exit selftest() }
if ($mode eq 'keys')     { my @k = map { keys_of(read_named($_), $opt{legacy}) } @tables; print "$_\n" for sort @k; print STDERR 'DECLUTTER: ' . scalar(@k) . " key(s) from " . scalar(@tables) . " table(s)\n"; exit 0 }
if ($mode eq 'scan')     { exit scan() }
if ($mode eq 'baseline') { exit baseline() }
if ($mode eq 'judge')    { exit judge() }
if ($mode eq 'parity')   { exit parity() }

sub usage {
    print STDERR "usage: declutter_scan.pl scan|baseline|judge|keys|parity [--legacy] [--out=<prefix>] [--grow] [--baseline=<file>] <table>... | selftest\n";
    print STDERR "       a table is a dry film's path, or <Country>=<path> for a real film's\n";
    print "DECLUTTER: usage fault - nothing read\n";
    exit 2;
}

# ------------------------------------------------------------------ the rules file

sub glob_re {
    my ($g) = @_;
    my $re = '';
    my @c = split //, $g;
    for (my $i = 0; $i < @c; $i++) {
        my $ch = $c[$i];
        if ($ch eq '*') { $re .= '.*' }
        elsif ($ch eq '?') { $re .= '.' }
        elsif ($ch eq '[') {
            my $close = index($g, ']', $i + 1);
            if ($close < 0) { $re .= quotemeta('['); next }
            $re .= substr($g, $i, $close - $i + 1);
            $i = $close;
        }
        else { $re .= quotemeta($ch) }
    }
    return qr/^$re$/;
}

sub load_rules {
    my ($path) = @_;
    open my $h, '<:encoding(UTF-8)', $path or die "DECLUTTER: $path: $!\n";
    my (%r, $header, @errors); $r{exempt} = [];
    my $n = 0;
    while (my $line = <$h>) {
        $n++; chomp $line; $line =~ s/\r$//;
        next if $line eq '' || $line =~ /^#/;
        unless ($header) { $header = 1; next }
        my @c = split /\t/, $line, -1;
        if (@c != 4 || $c[3] !~ /\S/) { push @errors, "line $n: four columns, the last a reason"; next }
        if ($c[0] eq 'exempt') {
            if ($c[2] ne 'all' && $c[2] ne 'rest') { push @errors, "line $n: scope is all or rest"; next }
            push @{ $r{exempt} }, { glob => $c[1], all => $c[2] eq 'all', reason => $c[3], re => glob_re($c[1]) };
        } elsif ($c[0] eq 'cap') {
            if ($c[2] !~ /^\d+$/) { push @errors, "line $n: a cap is a whole number"; next }
            if (!grep { $_ eq $c[1] } @CAPS) { push @errors, "line $n: no cap is named '$c[1]'"; next }
            $r{cap}{ $c[1] } = $c[2] + 0;
        } else { push @errors, "line $n: a row is exempt or cap" }
    }
    close $h;
    for my $c (@CAPS) { push @errors, "the cap '$c' is not set" unless defined $r{cap}{$c} }
    push @errors, 'no exemption read' unless @{ $r{exempt} };
    die "DECLUTTER: $path is faulty - " . join('; ', @errors) . "\n" if @errors;
    return \%r;
}

sub exemption_of {
    my ($cap) = @_;
    for my $e (@{ $rules->{exempt} }) { return $e if $cap =~ $e->{re} }
    return undef;
}

# ------------------------------------------------------------------ the label table

sub unescape {
    my ($t) = @_;
    $t =~ s/\\(.)/$1 eq 'n' ? "\n" : $1 eq 't' ? "\t" : $1 eq 'r' ? "\r" : $1/ge;
    return $t;
}

sub words { my ($p) = @_; return scalar grep { length($_) && /\p{L}/ } split /[\s\x{00B7}\/]+/, $p }
sub fold  { my ($t) = @_; $t =~ s/[0-9]+(?:[.,][0-9]+)*/#/g; return $t }

# The film's own facts beside a table (DeclutterRules.WriteFacts): X_labels.tsv beside X_film.tsv; undef when there are none.
sub read_facts {
    my ($path) = @_;
    (my $facts = $path) =~ s/_labels\.tsv$/_film.tsv/;
    return undef if $facts eq $path || !-e $facts;
    open my $h, '<:encoding(UTF-8)', $facts or die "DECLUTTER: $facts: $!\n";
    my %f;
    while (my $l = <$h>) { chomp $l; $l =~ s/\r$//; next if $l eq '' || $l =~ /^#/; my ($k, $v) = split /\t/, $l, 2; $f{$k} = $v if defined $v }
    close $h;
    die "DECLUTTER: $facts names no country, film or mode\n" unless $f{country} && $f{film} && $f{mode};
    return \%f;
}

# A table argument: { path, country, real, mode } - from the film's own facts, else from the name (a dry table) or the argument (a real one).
sub read_named {
    my ($arg) = @_;
    my ($country, $path, $real, $mode);
    if ($arg =~ /^([A-Za-z]+)(?::([A-Za-z0-9+_.-]+))?=(.+)$/ && !-e $arg) { ($country, $mode, $path, $real) = (lc $1, $2 // 'sweep', $3, 1) }
    else { $path = $arg }
    if (my $f = read_facts($path)) { ($country, $real, $mode) = ($f->{country}, $f->{film} eq 'real' ? 1 : 0, $f->{mode}) }
    elsif (!defined $country) {
        ($country) = $path =~ /_([a-z]+)_\d+_labels\.tsv$/;
        die "DECLUTTER: '$path' has no facts beside it and is not a dry film's table (<label>_<country>_<w>_labels.tsv) - name a real film's: <Country>=<path>\n" unless $country;
        ($real, $mode) = (0, 'sweep');
    }
    my $t = read_table($path);
    $t->{country} = $country; $t->{real} = $real; $t->{path} = $path; $t->{mode} = $mode;
    return $t;
}

sub read_table {
    my ($path) = @_;
    open my $h, '<:encoding(UTF-8)', $path or die "DECLUTTER: $path: $!\n";
    my $hd = <$h> // die "DECLUTTER: $path is empty\n"; chomp $hd; $hd =~ s/\r$//;
    my @cols = split /\t/, $hd; my %at; @at{@cols} = 0 .. $#cols;
    for my $c (qw(capture screen font mode needs_height flags text)) { die "DECLUTTER: $path: the table has no '$c' column\n" unless exists $at{$c} }
    my @rows; my $n = 0;
    while (my $line = <$h>) {
        $n++; chomp $line; $line =~ s/\r$//;
        next if $line eq '';
        my @f = split /\t/, $line, -1;
        my $get = sub { my $v = $f[ $at{ $_[0] } ]; defined $v ? $v : '' };
        my ($w) = $get->('screen') =~ /^(\d+)/;
        my %flags = map { $_ => 1 } grep { length } split / /, $get->('flags');
        my $text = $get->('text');
        my $font = $get->('font'); $font = 0 unless $font =~ /^-?[\d.]+(?:[eE][-+]?\d+)?$/;
        my $nh = $get->('needs_height'); $nh = 0 unless $nh =~ /^-?[\d.]+(?:[eE][-+]?\d+)?$/;
        push @rows, { capture => $get->('capture'), width => ($w // 0) + 0, font => $font + 0, mode => $get->('mode'), nh => $nh + 0,
                      flags => \%flags, text => $text, plain => unescape($text) };
    }
    close $h;
    return { rows => \@rows, width => (@rows ? $rows[0]{width} : 0) };
}

sub is_slip { my ($r) = @_; return $r->{flags}{SLIPHEAD} || $r->{flags}{SLIPLINE} || $r->{flags}{SLIPHEAD2} || $r->{flags}{SLIPLINE2} }

# ------------------------------------------------------------------ the classification (DeclutterRules.Read, line for line)

sub classify {
    my ($t, $legacy) = @_;
    my (@atrest, @slips, @captures, %seencap, %seen, %exempt, %sliprows, $flagged);
    my $c = $rules->{cap};
    my $add = sub { my ($into, $b) = @_; my $k = keyline($b); return if $seen{$k}++; push @$into, $b };
    for my $r (@{ $t->{rows} }) {
        push @captures, $r->{capture} unless $seencap{ $r->{capture} }++;
        $flagged ||= grep { $r->{flags}{$_} } @K321;
        my $ex = exemption_of($r->{capture});
        $exempt{ $r->{capture} } = 1 if $ex;
        next if $r->{text} eq '';
        if (is_slip($r)) { push @{ $sliprows{ $r->{capture} } }, $r if !$ex || !$ex->{all}; next }
        next if $r->{flags}{OUTSIDE} || $ex;
        my $w = words($r->{plain});
        my $cut = $r->{plain} =~ /(\x{2026}|\.\.\.)\s*$/ ? 1 : 0;
        my $multi = $legacy ? ($r->{mode} eq 'wrap' && $r->{font} > 0 && $r->{nh} / ($r->{font} * 1.25) > 1.6) : $r->{flags}{MULTILINE};
        my $src = $r->{flags}{CANVAS} ? 'canvas' : 'imgui';
        my %b = (capture => $r->{capture}, source => $src, text => $r->{text}, key => fold($r->{text}), words => $w);
        $add->(\@atrest, { %b, kind => 'CUT' }) if $cut;
        $add->(\@atrest, { %b, kind => 'WRAP' }) if $multi && $w >= $c->{wrap_words};
        $add->(\@atrest, { %b, kind => 'LONG' }) if $w > $c->{long_words};
        $add->(\@atrest, { %b, kind => 'STOP' }) if $w >= $c->{stop_words} && !$cut && $r->{plain} =~ /[.!?]["'\x{2019}\x{201D}\x{00BB})\]]?\s*$/;
        $add->(\@atrest, { %b, kind => 'LABEL' }) if $r->{flags}{CONTROL} && $w > $c->{label_words};
    }
    for my $cap (@captures) {
        next unless $sliprows{$cap};
        for my $s (group_slips($sliprows{$cap})) {
            my $limit = $s->{level} == 1 ? $c->{slip_lines} : $c->{slip2_lines};
            my $n = scalar @{ $s->{lines} };
            $add->(\@slips, { capture => $cap, source => 'imgui', kind => 'SLIP', text => $s->{head}{text}, key => fold($s->{head}{text}),
                              words => words($s->{head}{plain}), detail => "level $s->{level}: $n lines under the head, the cap $limit" }) if $n > $limit;
            next unless $s->{level} == 1;
            for my $l (@{ $s->{lines} }) {
                my $w = words($l->{plain});
                next if $w <= $c->{slip_line_words};
                my $text = $s->{head}{text} . " \x{00BB} " . $l->{text};
                $add->(\@slips, { capture => $cap, source => 'imgui', kind => 'SLIP', text => $text, key => fold($text), words => $w,
                                  detail => "level 1: a line of $w counted words, the cap $c->{slip_line_words}" });
            }
        }
    }
    return { atrest => \@atrest, slips => \@slips, captures => \@captures, exempt => \%exempt, flagged => $flagged ? 1 : 0 };
}

sub group_slips {
    my ($rows) = @_;
    my (@slips, $open);
    for my $r (@$rows) {
        if ($r->{flags}{SLIPHEAD}) { push @slips, { head => $r, level => 1, lines => [] }; $open = $slips[-1] }
        elsif ($r->{flags}{SLIPHEAD2}) { push @slips, { head => $r, level => 2, lines => [] }; $open = $slips[-1] }
        elsif ($open && (($r->{flags}{SLIPLINE} && $open->{level} == 1) || ($r->{flags}{SLIPLINE2} && $open->{level} == 2))) { push @{ $open->{lines} }, $r }
    }
    return @slips;
}

sub keyline { my ($b) = @_; return join "\t", $b->{source}, $b->{capture}, $b->{kind}, $b->{key} }

sub keys_of {
    my ($t, $legacy) = @_;
    my $rd = classify($t, $legacy);
    warn_unflagged($t, $rd) unless $legacy;
    return map { join "\t", $t->{country}, $t->{width}, keyline($_) } @{ $rd->{atrest} }, @{ $rd->{slips} };
}

sub warn_unflagged {
    my ($t, $rd) = @_;
    print STDERR "DECLUTTER: $t->{path} carries none of K321's flags (MULTILINE, CONTROL, CANVAS, the slip tags) - a table written before them? read it with --legacy\n" unless $rd->{flagged};
}

# ------------------------------------------------------------------ scan (the report)

sub scan {
    my (%byScreen, @inv, %distinct, $atrest, $slips);
    for my $arg (@tables) {
        my $t = read_named($arg);
        my $rd = classify($t, $opt{legacy});
        warn_unflagged($t, $rd) unless $opt{legacy};
        for my $b (@{ $rd->{atrest} }, @{ $rd->{slips} }) {
            my ($screen) = $b->{capture} =~ /^([a-z]*\d+[a-z]?\d*)/; $screen //= $b->{capture};
            $byScreen{$screen}{n}++;
            $byScreen{$screen}{caps}{ $b->{capture} }++;
            $distinct{"$screen\t$b->{text}"} = 1;
            push @inv, join "\t", $screen, $t->{country}, $t->{width}, $b->{source}, $b->{capture}, $b->{kind}, $b->{words}, ($b->{detail} // ''), $b->{text};
        }
        $atrest += @{ $rd->{atrest} }; $slips += @{ $rd->{slips} };
        my %kinds; $kinds{ $_->{kind} }++ for @{ $rd->{atrest} }, @{ $rd->{slips} };
        printf "%s %s %d: %d at rest, %d slip; %s; %d capture(s), %d exempt\n", $t->{path}, $t->{country}, $t->{width}, scalar @{ $rd->{atrest} },
            scalar @{ $rd->{slips} }, join(' ', map { "$_ " . ($kinds{$_} // 0) } @KINDS), scalar @{ $rd->{captures} }, scalar keys %{ $rd->{exempt} };
    }
    if ($opt{out}) {
        open my $o, '>:encoding(UTF-8)', "$opt{out}.tsv" or die "DECLUTTER: $opt{out}.tsv: $!\n";
        print $o "screen\tcountry\twidth\tsource\tcapture\tkind\twords\tdetail\ttext\n";
        print $o "$_\n" for sort @inv;
        close $o;
    }
    printf "%-8s %5s %8s  %s\n", 'screen', 'draws', 'distinct', 'captures (most breaches first)';
    my %dist; $dist{ (split /\t/, $_)[0] }++ for keys %distinct;
    for my $s (sort { $byScreen{$b}{n} <=> $byScreen{$a}{n} || $a cmp $b } keys %byScreen) {
        my $c = $byScreen{$s}{caps};
        my @top = (sort { $c->{$b} <=> $c->{$a} || $a cmp $b } keys %$c)[0 .. 2];
        printf "%-8s %5d %8d  %s\n", $s, $byScreen{$s}{n}, $dist{$s}, join(', ', map { "$_ ($c->{$_})" } grep { defined } @top);
    }
    printf "DECLUTTER: %d breach(es) at rest, new and stale not judged (scan); %d slip breach(es); %d table(s), %d distinct string(s) by screen\n",
        $atrest // 0, $slips // 0, scalar @tables, scalar keys %distinct;
    return 0;
}

# ------------------------------------------------------------------ the baseline

sub read_baseline {
    my ($path) = @_;
    open my $h, '<:encoding(UTF-8)', $path or die "DECLUTTER: $path: $!\n";
    my (@rows, $header, $generated); my $n = 0;
    while (my $line = <$h>) {
        $n++; chomp $line; $line =~ s/\r$//;
        if ($line =~ /^#/) { $generated = 1 if index($line, 'GENERATED by Tools/declutter_scan.pl') >= 0; next }
        next if $line eq '';
        unless ($header) { $header = 1; die "DECLUTTER: $path line $n: the header is country, width, mode, source, capture, kind, text\n" unless $line eq $BASEHEAD; next }
        my @c = split /\t/, $line, -1;
        die "DECLUTTER: $path line $n: not a baseline row\n"
            unless @c == 7 && $c[1] =~ /^\d+$/ && length $c[2] && $c[3] =~ /^(imgui|canvas)$/ && ($c[5] eq 'TAKEN' || grep { $_ eq $c[5] } @KINDS);
        push @rows, { country => $c[0], width => $c[1] + 0, mode => $c[2], source => $c[3], capture => $c[4], kind => $c[5], key => $c[6] };
    }
    close $h;
    die "DECLUTTER: $path does not say it is GENERATED by Tools/declutter_scan.pl - it is never hand-edited\n" unless $generated;
    return \@rows;
}

sub row_line { my ($r) = @_; return join "\t", $r->{country}, $r->{width}, $r->{mode}, $r->{source}, $r->{capture}, $r->{kind}, $r->{key} }
sub kind_of { return (split /\t/, $_[0])[5] }

sub baseline {
    my $out = $opt{out} // $BASELINE;
    my $exists = -e $out;
    my @old = $exists ? @{ read_baseline($out) } : ();
    my %old = map { row_line($_) => 1 } @old;
    my %replace; my %fresh; my @from;
    for my $arg (@tables) {
        my $t = read_named($arg);
        my $rd = classify($t, 0);
        warn_unflagged($t, $rd);
        my $scope = join "\t", $t->{country}, $t->{width}, $t->{mode};
        push @from, "$t->{path} ($t->{country}, $t->{width}, " . ($t->{real} ? 'real' : 'dry') . ", mode $t->{mode})";
        my @sources = $t->{real} ? qw(imgui canvas) : qw(imgui);
        for my $src (@sources) {
            for my $cap (@{ $rd->{captures} }) {
                $replace{"$scope\t$src\t$cap"} = 1;
                $fresh{"$scope\t$src\t$cap\tTAKEN\t-"} = 1;
            }
        }
        for my $b (@{ $rd->{atrest} }, @{ $rd->{slips} }) { $fresh{ "$scope\t" . keyline($b) } = 1 }
    }
    my %new = %fresh;
    for my $r (@old) { $new{ row_line($r) } = 1 unless $replace{ join "\t", @$r{qw(country width mode source capture)} } }
    my @grows = grep { !$old{$_} && kind_of($_) ne 'TAKEN' } sort keys %new;
    my @shrinks = grep { !$new{$_} && kind_of($_) ne 'TAKEN' } sort keys %old;
    # a grown row on a frame the old baseline never took is an existing screen filmed for the first time (or a renamed or new frame); one on a frame
    # it took is a breach that was not there - the integrator reads the two apart before choosing --grow
    my %seenFrame = map { (join("\t", @$_{qw(country width mode source capture)}), 1) } grep { $_->{kind} eq 'TAKEN' } @old;
    my $unseen = sub { my @c = split /\t/, $_[0]; return !$seenFrame{ join "\t", @c[0 .. 4] } };
    if ($exists && @grows && !$opt{grow}) {
        print "GROWS\t$_" . ($unseen->($_) ? "\t[a frame the baseline never took]" : "\t[a frame the baseline took: a NEW breach]") . "\n" for @grows;
        printf "DECLUTTER: the baseline would GROW by %d row(s), %d of them on frames it never took - it only shrinks (K321 (d)); nothing written. "
             . "A new breach on a known frame is decluttered, not grandfathered; an existing screen filmed for the first time, or a renamed frame, is "
             . "written with --grow and recorded\n", scalar @grows, scalar(grep { $unseen->($_) } @grows);
        return 1;
    }
    my $breaches = grep { kind_of($_) ne 'TAKEN' } keys %new;
    open my $o, '>:raw:encoding(UTF-8)', $out or die "DECLUTTER: $out: $!\n";
    print $o "# GENERATED by Tools/declutter_scan.pl baseline - never hand-edited (K321 (c), docs/RULINGS_2026-10-23.md). The breaches of K320's caps\n";
    print $o "# grandfathered when the declutter check landed: every film and dry film judges its own label table against these rows (K321 (d)). A breach\n";
    print $o "# not here is NEW and fails the film; a row of a capture the film took that no longer breaches is STALE and fails until this file is\n";
    print $o "# regenerated, so it only shrinks (a regeneration that would add a row refuses without --grow). A TAKEN row (its text a dash)\n";
    print $o "# says the generating films took that capture. The key: the capture stem and the drawn text with every run of digits folded to '#',\n";
    print $o "# plus the kind; scoped by country, width, film mode (one stem shows other words in another mode) and source (imgui, or canvas: a\n";
    print $o "# real film's Canvas text). Regenerate: perl Tools/declutter_scan.pl baseline <the films' label tables> - each film's facts beside it.\n";
    print $o "# generated from: $_\n" for @from;
    print $o "$BASEHEAD\n";
    print $o "$_\n" for sort keys %new;
    close $o;
    if (!$exists) {
        printf "DECLUTTER: %d breach row(s) written to %s - the first generation, every breach grandfathered; %d table(s) read\n", $breaches, $out, scalar @tables;
        return 0;
    }
    printf "DECLUTTER: %d breach row(s) written to %s (%d grown%s, %d shrunk); %d table(s) read\n", $breaches, $out, scalar @grows, (@grows ? ' by --grow' : ''), scalar @shrinks, scalar @tables;
    print "GREW\t$_\n" for @grows;
    print "SHRANK\t$_\n" for @shrinks;
    return 0;
}

# ------------------------------------------------------------------ judge (DeclutterRules.Judge, without the moved words: those need the film's slip books)

sub judge { return judge_tables($opt{baseline} // $BASELINE, [@tables], \*STDOUT) }

sub judge_tables {
    my ($path, $tabs, $fh) = @_;
    unless (-e $path) {
        print $fh "DECLUTTER: no baseline at $path - nothing can be judged; generate it: perl Tools/declutter_scan.pl baseline <the films' tables>\n";
        return 1;
    }
    my $base = read_baseline($path);
    my $worst = 0;
    for my $arg (@$tabs) {
        my $t = read_named($arg);
        my $rd = classify($t, 0);
        warn_unflagged($t, $rd);
        my ($country, $width, $fmode) = ($t->{country}, $t->{width}, $t->{mode});
        my @mine = grep { $_->{country} eq $country && $_->{width} == $width && $_->{mode} eq $fmode } @$base;
        my $tag = "$country $width " . ($t->{real} ? 'real' : 'dry') . " $fmode";
        unless (grep { $_->{source} eq 'imgui' } @mine) {
            printf $fh "DECLUTTER: %d breach(es) at rest, 0 new, 0 stale; %d slip breach(es); %s - FAILED: the baseline holds nothing of %s at %d px in the film mode '%s'\n",
                scalar @{ $rd->{atrest} }, scalar @{ $rd->{slips} }, $tag, $country, $width, $fmode;
            $worst = 1; next;
        }
        my %sources = map { $_ => 1 } ($t->{real} ? qw(imgui canvas) : qw(imgui));
        my %grand = map { keyline($_) => 1 } grep { $_->{kind} ne 'TAKEN' } @mine;
        my %covered = map { ("$_->{source}\t$_->{capture}", 1) } grep { $_->{kind} eq 'TAKEN' } @mine;
        my @all = (@{ $rd->{atrest} }, @{ $rd->{slips} });
        my %now = map { keyline($_) => 1 } @all;
        my %taken = map { $_ => 1 } @{ $rd->{captures} };
        my ($new, $stale) = (0, 0);
        for my $b (@all) {
            next if $grand{ keyline($b) };
            $new++;
            printf $fh "DECLUTTER NEW %s %s (%s, %d counted words%s): %s%s\n", $b->{capture}, $b->{kind}, $b->{source}, $b->{words},
                ($b->{detail} ? "; $b->{detail}" : ''), $b->{text}, ($covered{"$b->{source}\t$b->{capture}"} ? '' : '  [a frame the baseline never saw]');
        }
        for my $r (@mine) {
            next if $r->{kind} eq 'TAKEN' || !$sources{ $r->{source} } || !$taken{ $r->{capture} } || $now{ keyline($r) };
            $stale++;
            printf $fh "DECLUTTER STALE %s %s (%s): %s - it no longer breaches on a frame this film took; regenerate the baseline (it only shrinks)\n", $r->{capture}, $r->{kind}, $r->{source}, $r->{key};
        }
        my $code = $new || $stale ? 1 : 0;
        printf $fh "DECLUTTER: %d breach(es) at rest, %d new, %d stale; %d slip breach(es); %d capture(s) read, %d exempt; %s - %s\n",
            scalar @{ $rd->{atrest} }, $new, $stale, scalar @{ $rd->{slips} }, scalar @{ $rd->{captures} }, scalar keys %{ $rd->{exempt} }, $tag, $code ? 'FAILED' : 'PASS';
        $worst = $code if $code > $worst;
    }
    return $worst;
}

# ------------------------------------------------------------------ selftest and parity: the two implementations held equal

sub file_lines {
    my ($path) = @_;
    open my $h, '<:encoding(UTF-8)', $path or die "DECLUTTER: $path: $!\n";
    my @l = grep { $_ ne '' && !/^#/ } map { s/\r?\n$//r } <$h>;
    close $h;
    return @l;
}

sub compare {
    my ($what, $want, $got) = @_;
    my %w = map { $_ => 1 } @$want; my %g = map { $_ => 1 } @$got;
    my @missing = grep { !$g{$_} } @$want; my @extra = grep { !$w{$_} } @$got;
    print "  $what MISSING\t$_\n" for @missing;
    print "  $what EXTRA\t$_\n" for @extra;
    return @missing + @extra;
}

sub selftest {
    my $bad = 0;
    my $t = read_named("$FIXTURE/fixture_sweden_1280_labels.tsv");
    $bad += compare('keys', [ file_lines("$FIXTURE/expected_keys.tsv") ], [ sort(keys_of($t, 0)) ]);
    $bad += compare('legacy keys', [ file_lines("$FIXTURE/expected_keys_legacy.tsv") ], [ sort(keys_of($t, 1)) ]);
    # the judge's teeth: the fixture against its planted baseline - exactly the planted NEW and the planted STALE
    my @want = file_lines("$FIXTURE/expected_judge.tsv");
    my @got;
    {
        my $buf = '';
        open my $mem, '>:encoding(UTF-8)', \$buf or die "DECLUTTER: an in-memory handle: $!\n";
        my $code = judge_tables("$FIXTURE/fixture_baseline.tsv", [ "$FIXTURE/fixture_sweden_1280_labels.tsv" ], $mem);
        close $mem;
        my $s = Encode::decode('UTF-8', $buf);
        @got = map { s/ \(.*//r } grep { /^DECLUTTER (NEW|STALE) / } split /\n/, $s;
        push @got, "exit $code";
    }
    $bad += compare('judge', \@want, \@got);
    printf "DECLUTTER: selftest %s - the fixture's keys (flagged and legacy) and the judge's planted NEW and STALE, against Tools/declutter_fixture/'s expected files%s\n",
        $bad ? 'FAILED' : 'PASS', $bad ? " ($bad difference(s) above)" : '';
    return $bad ? 1 : 0;
}

# A path as a native Windows program reads it: Git Bash's perl spells G:\x as /g/x, which it converts on a command line but not inside a file.
sub native {
    my ($p) = @_;
    return $p unless $^O eq 'msys' || $^O eq 'cygwin';
    return "$1:/$2" if $p =~ m{^/([A-Za-z])/(.*)$};
    my $w = `cygpath -m "$p"`; chomp $w;
    return $w || $p;
}

sub parity {
    my @args = map { /^([A-Za-z]+(?::[A-Za-z0-9+_.-]+)?)=(.+)$/ && !-e $_ ? "$1=" . native(File::Spec->rel2abs($2)) : native(File::Spec->rel2abs($_)) } @tables;
    my @perl = sort map { keys_of(read_named($_), $opt{legacy}) } @tables;
    my $proj = "$ROOT/Tools/textcheck";
    # the tables go in a list file (one a line): hundreds of paths overrun a command line
    my $list = File::Spec->catfile(File::Spec->tmpdir, "declutter_parity_$$.txt");
    open my $lf, '>:encoding(UTF-8)', $list or die "DECLUTTER: $list: $!\n";
    print $lf "$_\n" for @args;
    close $lf;
    my @cmd = ('dotnet', 'run', '--project', native($proj), '-c', 'Release', '--', 'declutter', native($ROOT), ($opt{legacy} ? '--legacy' : ()), '@' . native($list));
    open my $p, '-|:encoding(UTF-8)', @cmd or die "DECLUTTER: cannot run dotnet: $!\n";
    my @cs = sort grep { /\t/ } map { s/\r?\n$//r } <$p>;
    close $p;
    my $exit = $? >> 8;
    unlink $list;
    die "DECLUTTER: the C# runner failed (exit $exit)\n" if $exit != 0;
    my $bad = compare('C#-vs-perl', \@perl, \@cs);
    printf "DECLUTTER: parity %s - %d key(s) from perl, %d from DeclutterRules.cs, over %d table(s)%s\n", $bad ? 'FAILED' : 'PASS', scalar @perl, scalar @cs, scalar @tables, $opt{legacy} ? ' (legacy)' : '';
    return $bad ? 1 : 0;
}

