#!/usr/bin/perl
# A-LOOP q5 (docs/specs/A_LOOP_PLAN.md section 6 (3)): THE FITTED JUMP AND DECAY of an event's salience, from Eurobarometer's QA3 series.
#
# Reads, byte-exact as fetched by Tools/aloop_eb_fetch.sh (digests in docs/reference/raw/aloop/eb/SHA256SUMS.txt, which this tool checks),
# each Standard Eurobarometer wave's "Volume A" table workbook: QA3 (QA3a in the older waves), "What do you think are the two most important
# issues facing (OUR COUNTRY) at the moment? (MAX. 2 ANSWERS)", the weighted share of each country's respondents naming each item.
# The older waves are BIFF8 .xls inside a zip (read here through their OLE2 container), the newer .xlsx (a zip of XML), so nothing is
# converted or re-saved: the cells are read out of the stored bytes.
#
# THE DESIGN (fixed here before the first fit was run; the plan's own examples - "immigration in 2015 (DE), rising prices 2022, the pandemic
# 2020" - and one dated real event of each class the pool carries or adds). A UNIT is one country around one dated event: its pre-wave (the
# last wave fielded wholly before the event), its post-wave (the first fielded wholly after) and its decay waves (the ones after that).
#   class      issue slot   QA3 item read                                      units: country, pre -> post, decay waves
#   Economy    Economy      max(rising prices/inflation, economic situation)   every EU member printed in both: EB96 -> EB97; EB98 EB99 EB100 EB101
#   Health     Healthcare   health (and social security)                       every EU member printed in both: EB92 -> EB93; EB94 EB95 EB96
#   Migration  Immigration  immigration                                        every EU member printed in both: EB83 -> EB84; EB85 EB86 EB87 EB88
#   Security   Defense      terrorism                                          BE EB84 -> EB85; EB86 EB87 / FR EB85 -> EB86; EB87 EB88 /
#                                                                              DE EB86 -> EB87; EB88 EB89 / SE EB86 -> EB87; EB88 EB89 /
#                                                                              ES EB87 -> EB88; EB89 EB90
#   Crime      Crime        crime                                              SE EB99 -> EB100; EB101 EB102 EB103
#   Natural    Climate      environment, climate and energy issues             SE EB89 -> EB90; EB91 EB92
#  - Economy: Russia's invasion of Ukraine (24 Feb 2022) and the energy-price shock it set off; EB96's fieldwork closed on 14 Feb 2022. The
#    slot takes the larger of its two items per country and wave, the convention StanceModel's EB105 table states ("where two items map to
#    one slot the larger stands"); EB98 on prints the second as "the economic situation".
#  - Health: the pandemic (WHO, 11 March 2020); EB92 was fielded in November 2019. The item's wording changes at EB93 - "health and social
#    security" (EB92, both split samples) becomes "health" (EB93, marked (M) on the sheet) - so the jump crosses a wording change, stated.
#    EB92 asked QA3 of split samples; split A (QA3a, the earlier waves' list) is read.
#  - Migration: the 2015 arrivals (the summer and autumn of 2015); EB83 was fielded in May 2015, EB84 in November. The unit's event date
#    (for the back-cast, below) is 5 September 2015, when Germany and Austria admitted the people held in Hungary.
#  - Security: QA3 carried no defence item before EB103, and "the international situation" first appears in EB97, after the invasion, so the
#    war has no pre-wave on any item that measures it. The class is fitted on the dated terror attacks the series brackets cleanly, on its
#    TERRORISM item, read as the Defense slot's analogue (EB105's "security and defence" seeds the slot) - a mismatch stated: Brussels
#    (22 Mar 2016), Nice (14 Jul 2016), Berlin (19 Dec 2016), Stockholm (7 Apr 2017), Barcelona (17 Aug 2017). Manchester (22 May 2017) is
#    left out: EB87's fieldwork (20-30 May 2017) straddles it. Brussels's pre-wave EB84 was fielded 7-17 Nov 2015, across the Paris attacks
#    of 13 November, so its pre-level may already be raised, stated.
#  - Crime: Sweden's gang-violence autumn of 2023 (September 2023's shootings and bombings and the Prime Minister's address of 28 September);
#    EB99 was fielded in May-June 2023, EB100 in October-November.
#  - Natural: Sweden's summer of 2018, its drought and its forest fires (July-August 2018); EB89 March 2018, EB90 November 2018. Its event
#    date is 15 July 2018, the fires' peak month's middle (a reading: the summer has no single day).
#  - Each unit's event date is the day named above (the class table in the code carries them); the post-wave is fielded wholly after it.
#  - Item-list changes inside a class's waves (two answers at most, so a new item competes with the old ones): EB97, Economy's post-wave,
#    adds "the international situation" (it can only have lowered the economy items' rise); EB103, Crime's third decay wave, adds three
#    items ("security and defence", "Russia's invasion of Ukraine", "threats to democracy") and Sweden's crime share halves there (Crime
#    is inert before it).
# A wave is read off its own sheet: the fieldwork dates printed on it (their midpoint is the wave's date) and its item labels.
#
# THE FIT, per class (n = its units; a unit enters where its item is printed for its country in its pre- and post-wave):
#   - the jump J = the mean over the units of share(post) - share(pre), in share units (0..1). Its 95 % interval: with two or more units the
#     jackknife over units (leave one out; the standard error from the n leave-one-out means; the t quantile at n-1 degrees of freedom); with
#     one, the sampling interval of a difference of two independent proportions, 1.96 sqrt(p1(1-p1)/n1 + p0(1-p0)/n0), each n the weighted
#     total the sheet prints for that country (simple random sampling - Eurobarometer's design effect is not on the sheet, so this interval is
#     narrower than the truth, stated);
#   - the decay: at each decay step k, the excess E_k = the mean over the units of share(wave) - share(pre), at the time dt_k = the mean over
#     the units of that wave's date less the post-wave's (a unit missing a decay wave's item leaves that step); lambda (a day) minimises
#     sum_k (E_k - J exp(-lambda dt_k))^2 - one free parameter, J held at the measured jump; the half-life is ln 2 / lambda, in days. Its 95 %
#     interval: with two or more units the jackknife over units again; with one unit, none.
#   - THE JUMP THE GAME LANDS is the SURVEY jump J, HELD flat for the class's hold - the units' mean days from their event to their post-wave
#     (EventToSurveyDays, rounded to whole days: DERIVED) - and decaying at lambda after it. So the game reads J on the survey's own date and
#     the fitted curve after it, and never a level the series did not show. (Since amendment (6) below, J, the hold and the decay are read
#     at the excess's peak wave; the lines above describe the case where the peak is the post-wave.)
#   - Reported only, never generated: the jump BACK-CAST to the event's day, J0 = the mean over the units of (share(post) - share(pre)) x
#     exp(lambda x the days from the unit's event to its post-wave), with its jackknife interval (the one-unit interval scales the sampling
#     interval and carries no uncertainty in lambda; a leave-one-out whose decay sits at the grid's edge back-casts with a factor of 1, so its
#     pseudo-values jump). It is an EXTRAPOLATION above the series' own maximum (Economy's event-day level some 8 points above any wave; for
#     20 of 27 units above each unit's own maximum), and Economy's factor depends on the decay's shape (1.16 to 1.76 as decay waves are
#     dropped), so it is printed for the record and lands nowhere.
#   - A class is generated where lambda is resolved - not at the search's low end (no decay the series shows: the excess held or grew) and not
#     at its high end (the excess gone before the first decay wave, faster than the series resolves) - AND J's interval excludes zero.
#     Otherwise it is printed and NOT generated: it stays inert (a jump of zero), and the reason is printed.
#   - Reported only, never generated: lambda's jackknife takes every leave-one-out fit as it comes, an edge return (a leave-one-out whose
#     jump is not positive, or whose fit sits at the grid's end) included, so a class carried by one unit prints a meaningless lambda interval.
#   ⚠ AMENDED after the first run (2026-10-19), the units, waves and items untouched: (1) the decay was first a log-linear regression over the
#     steps whose excess stayed positive; a step crossing zero then entered or left the fit as a unit was dropped, so the jackknife was not a
#     smooth function of the data (the first run's Economy interval spanned -0.077 to +0.087 a day). The least squares on the levels above
#     replaced it, and with it the resolution rule (Resolution, below). (2) The first run counted the United Kingdom as a member after its
#     withdrawal; it is a member in a wave fielded before 31 January 2020 only. The first run's table, for the record (log-linear decay, the
#     UK counted throughout; the jump then as now the plain mean): Economy n 28, jump +0.0975, lambda 0.004661 (half-life 148.7 days); Health
#     n 28, +0.0618, lambda -0.000206 (not generated); Migration n 28, +0.1464, lambda 0.005223 (132.7 days); Security n 5, +0.0580, lambda
#     0.006395 (108.4 days); Crime n 1, +0.0400, no positive decay step (not generated); Natural n 1, +0.1500, lambda -0.000049 (not
#     generated) - the same classes generated as the amended estimator's.
#   ⚠ AMENDED a second time after review 882 (2026-10-19), the units, waves and items untouched again: (3) the jump is back-cast to the event's
#     day (the game lands it there; the first form landed the survey's figure on the event's day, so at the survey's date the game held some
#     57 % of what the survey measured for Economy); (4) a class whose (back-cast) jump's interval does not exclude zero is no longer
#     generated - the Security class, carried by Brussels alone, became inert under it.
#   ⚠ AMENDED a third time after review 882b (2026-10-19): (5) the back-cast is no longer landed - it put a level on the event's day the series
#     never showed - and the game lands the survey jump held flat until the survey's date (the hold, above); the generate rule reads the survey
#     jump's interval. The same classes are generated (Economy, Migration) and the same are inert.
#   ⚠ AMENDED a fourth time by Elias's ruling K69 (2026-10-23, docs/RULINGS_2026-10-23.md), the units, waves and items untouched again: (6) the
#     jump is read at the excess's PEAK - the step (the post-wave or a decay wave) whose mean excess over the pre-wave is the largest, the
#     earliest on a tie - not at the post-wave: J is that step's mean excess, its interval the jackknife over units with the peak step held at
#     the full sample's (one unit: the sampling interval of that wave against the pre-wave), the hold the units' mean days from their event to
#     the peak's wave, and the decay is fitted on the steps after the peak with their times counted from it. Where the peak is the post-wave
#     (Economy, Migration, Security, Crime) every figure is the earlier design's to the bit; Health (the peak at EB94) and Natural (EB91) are
#     generated under it. (7) The USA's conversion (K69): the bases section below the fit. (8) K94 (Elias, 2026-10-24): where the peak is
#     later than the post-wave, the game lands the post-wave's measured level (FirstWaveJump), holds it from the event to the post-wave
#     (FirstHoldDays, the units' mean days, rounded half up) and ramps it LINEARLY to the peak's level over the measured interval to the
#     peak's wave (RampDays = HoldDays - FirstHoldDays), then decays at lambda - DECLARED: linear between the two measured waves, the
#     series having nothing between them. Where the peak is the post-wave, RampDays is 0 and the hold is the earlier design's.
#   ⚠ K102 (Elias, 2026-10-24): THE OUT-OF-SAMPLE TEST OF THE USA'S CONVERSION - its criterion DECLARED here and committed BEFORE any of
#     its figures was computed (the review's M-2). The data: Gallup's own "most important problem" trend table, from the same source family
#     as the stored 2024 CSV (the Datawrapper chart vEKjO's published datasets, and the trends page's archived replays), fetched byte-exact
#     into ElectionsData/usa/raw/salience/k102/ (SHA256SUMS.txt, fetch_log.md); a year whose table is not on any such page as text is
#     BILLED, never read off an image. Per tested year:
#       (i) the BASE: the mean of the year's Total row over the year's columns (each column's Total / 100);
#       (ii) the SURGING ISSUE's observed US rise: the item's share at its largest over the columns fielded after the event, less its share in
#            the last column fielded wholly before it - 2020: "Coronavirus/Diseases" against the Health class (event 11 March 2020; the last
#            column before it February 2020); 2022: "High cost of living/Inflation" against the Economy class (event 24 February 2022; the last
#            column before it February 2022, fielded 1-17 February); 2015: "Immigration" against the Migration class (event 5 September 2015);
#       (iii) the share ratio: the observed US rise over the class's fitted Eurobarometer jump (EventSalienceFit's SurveyJump), set beside
#            the conversion.
#     THE CRITERION: (a) the base is instrument-stable when every tested year's base lies within +-15 % of the stored 2024 base (GallupBase);
#     (b) the conversion is preferred over no conversion when, in a majority of the tested years, the observed US rise is nearer - in
#     |ln(observed / predicted)| - to the Eurobarometer jump times UsConversion than to the Eurobarometer jump times 1. The conversion
#     (0.7134) FAILS, and is put back to Elias, if (a) fails or (b) fails; otherwise it is KEPT. Stated limit: the US and the EU rises are
#     different populations' responses to different events of a class, so (b) tests the scale against the only US rises on record, not
#     an identity.
# The t quantile: the two-sided 95 % point of Student's t, by bisection on its exact distribution function for integer degrees of freedom
# (Abramowitz & Stegun 26.7.3-4), DERIVED - SalienceFitCheck evaluates the same.
# Writes docs/generated/EB_QA3_SERIES.tsv (every share the fit read, unit by unit, each wave's date and each source's digest) and
# Assets/Scripts/Elections/Generated/EventSalienceFit.cs (the constants and the digests); SalienceFitCheck re-reads the TSV, holds it against
# the stored files' digests, refits from it and fails while the constants are not the fit.
# Usage (from the project root): perl Tools/aloop_salience_fit.pl [--dry]                              the fit
#                                perl Tools/aloop_salience_fit.pl --dump <file> [sheet regex [member]]  a workbook's sheets, or a sheet's cells
#                                perl Tools/aloop_salience_fit.pl --labels [wave ...]                   each wave's QA3 sheet, fieldwork and items
use strict;
use warnings;
use Digest::SHA qw(sha256_hex);
use IO::Uncompress::Unzip qw($UnzipError);
use POSIX qw(floor);

sub slurp { my ($p) = @_; open my $fh, '<:raw', $p or die "$p: $!\n"; local $/; my $s = <$fh>; close $fh; return $s; }

# ---------------------------------------------------------------- zip members, in memory
sub unzip_all {
    my ($bytes) = @_;
    my $u = IO::Uncompress::Unzip->new(\$bytes) or die "unzip: $UnzipError\n";
    my (%part, @order);
    for (my $st = 1; $st > 0; $st = $u->nextStream()) {
        my $n = $u->getHeaderInfo()->{Name}; my ($b, $c) = ('');
        while ($u->read($c) > 0) { $b .= $c; }
        $part{$n} = $b; push @order, $n;
    }
    return (\%part, \@order);
}

# ---------------------------------------------------------------- .xlsx: sheets as { name => [ [row, col, text] ... ] }
sub xml_text { my ($s) = @_; $s =~ s/&lt;/</g; $s =~ s/&gt;/>/g; $s =~ s/&quot;/"/g; $s =~ s/&apos;/'/g; $s =~ s/&#(\d+);/chr($1)/ge; $s =~ s/&amp;/&/g; return $s; }
sub col_index { my ($l) = @_; my $n = 0; $n = $n * 26 + (ord($_) - 64) for split //, $l; return $n - 1; }

sub xlsx_sheets {
    my ($part) = @_;
    my @ss;
    if (my $s = $part->{'xl/sharedStrings.xml'}) {
        while ($s =~ m{<si>(.*?)</si>}gs) { my $si = $1; my $t = ''; while ($si =~ m{<t(?:\s[^>]*)?>(.*?)</t>}gs) { $t .= $1; } push @ss, xml_text($t); }
    }
    my %rid;
    while ($part->{'xl/_rels/workbook.xml.rels'} =~ m{<Relationship ([^>]*?)/?>}g) { my $a = $1; my ($id) = $a =~ /Id="([^"]+)"/; my ($t) = $a =~ /Target="([^"]+)"/; $rid{$id} = $t; }
    my @names;
    my %sheets;
    while ($part->{'xl/workbook.xml'} =~ m{<sheet ([^>]*?)/>}g) {
        my $a = $1; my ($n) = $a =~ /name="([^"]*)"/; my ($r) = $a =~ /r:id="([^"]+)"/;
        $n = xml_text($n);
        my $t = $rid{$r}; $t =~ s{^/?xl/}{};
        my $x = $part->{"xl/$t"} // die "no part xl/$t\n";
        my @cells;
        while ($x =~ m{<c r="([A-Z]+)(\d+)"([^>]*?)(?:/>|>(.*?)</c>)}gs) {
            my ($col, $row, $attr, $in) = ($1, $2, $3, $4 // '');
            my ($v) = $in =~ m{<v>(.*?)</v>}s;
            my $txt;
            if ($attr =~ /t="s"/ && defined $v) { $txt = $ss[$v]; }
            elsif ($attr =~ /t="inlineStr"/) { my $t2 = ''; while ($in =~ m{<t(?:\s[^>]*)?>(.*?)</t>}gs) { $t2 .= $1; } $txt = xml_text($t2); }
            elsif (defined $v) { $txt = xml_text($v); }
            next unless defined $txt && length $txt;
            push @cells, [$row - 1, col_index($col), $txt];
        }
        push @names, $n; $sheets{$n} = \@cells;
    }
    return (\@names, \%sheets);
}

# ---------------------------------------------------------------- .xls: OLE2 compound file -> the Workbook stream -> BIFF8 cells
sub ole_stream {
    my ($f, $want) = @_;
    die "not an OLE2 compound file\n" unless substr($f, 0, 8) eq "\xD0\xCF\x11\xE0\xA1\xB1\x1A\xE1";
    my $ssz = 1 << unpack('v', substr($f, 0x1E, 2));
    my $mssz = 1 << unpack('v', substr($f, 0x20, 2));
    my ($nfat, $dir0) = unpack('V V', substr($f, 0x2C, 8));
    my ($cutoff, $mfat0, $nmfat, $difat0, $ndifat) = unpack('V V V V V', substr($f, 0x38, 20));
    my @difat = unpack('V109', substr($f, 0x4C, 436));
    my $sec = sub { my ($n) = @_; return substr($f, ($n + 1) * $ssz, $ssz); };
    my $d = $difat0;
    for (1 .. $ndifat) { my @e = unpack('V*', $sec->($d)); $d = pop @e; push @difat, @e; }
    @difat = grep { $_ < 0xFFFFFFFA } @difat;
    splice(@difat, $nfat) if @difat > $nfat;
    my @fat = map { unpack('V*', $sec->($_)) } @difat;
    my $chain = sub { my ($s) = @_; my $b = ''; my %seen; while ($s < 0xFFFFFFFA) { die "a FAT loop\n" if $seen{$s}++; $b .= $sec->($s); $s = $fat[$s]; } return $b; };
    my $dir = $chain->($dir0);
    my ($root, $hit);
    for (my $o = 0; $o + 128 <= length $dir; $o += 128) {
        my $e = substr($dir, $o, 128);
        my $nl = unpack('v', substr($e, 64, 2)); next unless $nl >= 2;
        my $name = substr($e, 0, $nl - 2); $name =~ s/(.)\x00/$1/gs;
        my $type = ord(substr($e, 66, 1));
        my ($start, $size) = unpack('V V', substr($e, 116, 8));
        $root = [$start, $size] if $type == 5;
        $hit = [$start, $size] if $type == 2 && $name eq $want;
    }
    die "no '$want' stream\n" unless $hit;
    if ($hit->[1] >= $cutoff) { return substr($chain->($hit->[0]), 0, $hit->[1]); }
    my $mini = $chain->($root->[0]);
    my @mfat = $nmfat ? unpack('V*', $chain->($mfat0)) : ();
    my ($b, $s) = ('', $hit->[0]);
    while ($s < 0xFFFFFFFA) { $b .= substr($mini, $s * $mssz, $mssz); $s = $mfat[$s]; }
    return substr($b, 0, $hit->[1]);
}

sub rk_value {
    my ($rk) = @_;
    my $v;
    if ($rk & 2) { my $i = $rk >> 2; $i -= (1 << 30) if $i & (1 << 29); $v = $i; }
    else { $v = unpack('d<', pack('V V', 0, $rk & 0xFFFFFFFC)); }
    return ($rk & 1) ? $v / 100 : $v;
}

# A BIFF8 unicode string read across record boundaries: the SST's chunks each restart the character run with a fresh option byte.
sub sst_strings {
    my ($chunks) = @_;
    my ($ci, $pos) = (0, 8);
    my $total = unpack('V', substr($chunks->[0], 4, 4));
    my $take = sub {
        my ($n) = @_; my $out = '';
        while ($n > 0) {
            if ($pos >= length $chunks->[$ci]) { $ci++; $pos = 0; die "SST ran out\n" if $ci > $#$chunks; }
            my $k = length($chunks->[$ci]) - $pos; $k = $n if $k > $n;
            $out .= substr($chunks->[$ci], $pos, $k); $pos += $k; $n -= $k;
        }
        return $out;
    };
    my @s;
    for (1 .. $total) {
        my $cch = unpack('v', $take->(2));
        my $flags = ord($take->(1));
        my $runs = ($flags & 8) ? unpack('v', $take->(2)) : 0;
        my $ext = ($flags & 4) ? unpack('V', $take->(4)) : 0;
        my ($str, $wide, $left) = ('', $flags & 1, $cch);
        while ($left > 0) {
            if ($pos >= length $chunks->[$ci]) { $ci++; $pos = 0; die "SST ran out\n" if $ci > $#$chunks; $wide = ord(substr($chunks->[$ci], 0, 1)) & 1; $pos = 1; }
            my $avail = length($chunks->[$ci]) - $pos;
            my $can = $wide ? int($avail / 2) : $avail; $can = $left if $can > $left;
            my $raw = substr($chunks->[$ci], $pos, $can * ($wide ? 2 : 1)); $pos += length $raw;
            $str .= $wide ? join('', map { chr } unpack('v*', $raw)) : join('', map { chr } unpack('C*', $raw));
            $left -= $can;
        }
        $take->(4 * $runs) if $runs; $take->($ext) if $ext;
        push @s, $str;
    }
    return \@s;
}

sub xls_sheets {
    my ($xls) = @_;
    my $w = eval { ole_stream($xls, 'Workbook') } // ole_stream($xls, 'Book');
    my (@bound, @sstchunks, $insst);
    my $o = 0;
    my @recs;
    while ($o + 4 <= length $w) {
        my ($t, $l) = unpack('v v', substr($w, $o, 4));
        push @recs, [$o, $t, substr($w, $o + 4, $l)];
        $o += 4 + $l;
    }
    for my $i (0 .. $#recs) {
        my ($at, $t, $d) = @{$recs[$i]};
        if ($t == 0x0085) {
            my $pos = unpack('V', substr($d, 0, 4)); my $cch = ord(substr($d, 6, 1)); my $fl = ord(substr($d, 7, 1));
            my $name = ($fl & 1) ? join('', map { chr } unpack('v*', substr($d, 8, 2 * $cch))) : substr($d, 8, $cch);
            push @bound, [$name, $pos];
        }
        elsif ($t == 0x00FC) { @sstchunks = ($d); $insst = 1; }
        elsif ($t == 0x003C && $insst) { push @sstchunks, $d; }
        else { $insst = 0 if $t != 0x003C; }
    }
    my $sst = @sstchunks ? sst_strings(\@sstchunks) : [];
    my %byoffset = map { $_->[0] => $_ } @recs;
    my (@names, %sheets);
    for my $b (@bound) {
        my ($name, $pos) = @$b;
        my @cells;
        my $started = 0;
        for my $r (@recs) {
            my ($at, $t, $d) = @$r;
            next if $at < $pos;
            if ($t == 0x0809) { last if $started; $started = 1; next; }
            last if $t == 0x000A;
            if ($t == 0x00FD) { my ($row, $col, $xf, $k) = unpack('v v v V', $d); push @cells, [$row, $col, $sst->[$k]]; }
            elsif ($t == 0x0203) { my ($row, $col) = unpack('v v', $d); push @cells, [$row, $col, unpack('d<', substr($d, 6, 8))]; }
            elsif ($t == 0x027E) { my ($row, $col, $xf, $rk) = unpack('v v v V', $d); push @cells, [$row, $col, rk_value($rk)]; }
            elsif ($t == 0x00BD) {
                my ($row, $c0) = unpack('v v', $d); my $n = (length($d) - 6) / 6;
                for my $j (0 .. $n - 1) { my ($xf, $rk) = unpack('v V', substr($d, 4 + 6 * $j, 6)); push @cells, [$row, $c0 + $j, rk_value($rk)]; }
            }
            elsif ($t == 0x0204) {
                my ($row, $col, $xf, $cch) = unpack('v v v v', $d); my $fl = ord(substr($d, 8, 1));
                push @cells, [$row, $col, ($fl & 1) ? join('', map { chr } unpack('v*', substr($d, 9, 2 * $cch))) : substr($d, 9, $cch)];
            }
            elsif ($t == 0x0006) {
                my ($row, $col) = unpack('v v', $d); my $res = substr($d, 6, 8);
                push @cells, [$row, $col, unpack('d<', $res)] unless unpack('v', substr($res, 6, 2)) == 0xFFFF;
            }
        }
        @cells = grep { defined $_->[2] && length $_->[2] } @cells;
        push @names, $name; $sheets{$name} = \@cells;
    }
    return (\@names, \%sheets);
}

# a stored file -> its workbook's sheets
sub workbook {
    my ($path, $member) = @_;
    my $bytes = slurp($path);
    my ($part, $order) = unzip_all($bytes);
    return xlsx_sheets($part) if exists $part->{'xl/workbook.xml'};
    my @inner = defined $member && length $member ? grep { $_ eq $member } @$order : grep { /\.xlsx?$/i } @$order;
    die "$path: " . scalar(@inner) . " workbooks inside, not one (" . join(', ', @$order) . ")\n" unless @inner == 1;
    my $in = $part->{$inner[0]};
    if (substr($in, 0, 2) eq 'PK') { my ($p2) = unzip_all($in); return xlsx_sheets($p2); }
    return xls_sheets($in);
}

sub grid { my ($cells) = @_; my %g; $g{$_->[0]}{$_->[1]} = $_->[2] for @$cells; return \%g; }

if (@ARGV && $ARGV[0] eq '--dump') {
    my (undef, $file, $re, $member) = @ARGV;
    my ($names, $sheets) = workbook($file, $member);
    unless (defined $re) { print "$_\n" for @$names; exit 0; }
    for my $n (grep { /$re/ } @$names) {
        print "=== $n\n";
        my $g = grid($sheets->{$n});
        for my $r (sort { $a <=> $b } keys %$g) { print "r$r\t", join("\t", map { "c$_=$g->{$r}{$_}" } sort { $a <=> $b } keys %{$g->{$r}}), "\n"; }
    }
    exit 0;
}

# ---------------------------------------------------------------- the waves, and the QA3 table of each
my $D = 'docs/reference/raw/aloop/eb/';
# wave => [stored file, the workbook inside a zip that holds several (else undef)]
my %wavefile = (
    83 => ['eb83_VolumeAEB833COMMcomplet20150619_xls.zip'], 84 => ['eb84_VolumeAEB843COMMstandard20151119_xls.zip'],
    85 => ['eb85_EB85.2_Volume_A_xls.zip'], 86 => ['eb86_ST86_Volume_A_xls.zip'], 87 => ['eb87_Volume_A_xls.zip'], 88 => ['eb88_Volume_A_xlsl.zip'],
    89 => ['eb89_vol_A_xls.zip'], 90 => ['eb90_volume_A_xls.zip'], 91 => ['eb91_volume_A_xls.zip'], 92 => ['eb92_volume_A.zip', 'eb92_Volume_A.xls'],
    93 => ['eb93_volume_A.zip'], 94 => ['eb94_volume_A.xlsx'], 95 => ['eb95_vol_A.xlsx'], 96 => ['eb96_Volume_A.xlsx'], 97 => ['eb97_Volume_A.xlsx'],
    98 => ['eb98_volume_A.xlsx'], 99 => ['eb99_volume_A.xlsx'], 100 => ['eb100_Volume_A.xlsx'], 101 => ['eb101_VOL_A.xlsx'], 102 => ['eb102_volume_A.xlsx'],
    103 => ['eb103_volume_A.xlsx'],
);
# the EU's members as the sheets code them (Germany's whole column, not its two halves); the United Kingdom while a member
my @eu = qw(BE BG CZ DK DE EE IE EL ES FR HR IT CY LV LT LU HU MT NL AT PL PT RO SI SK FI SE UK);
my %member = map { $_ => 1 } @eu;

sub trim { my ($s) = @_; $s //= ''; $s =~ s/\s+/ /g; $s =~ s/^ //; $s =~ s/ $//; return $s; }

# One wave's QA3: { date => midpoint (days since 1970-01-01), fieldwork => text, sheet => name, n => {country => n}, share => {label => {country => share}} }
sub qa3 {
    my ($wave) = @_;
    my ($file, $member) = @{$wavefile{$wave}};
    my ($names, $sheets) = workbook($D . $file, $member);
    for my $name (@$names) {
        my $cells = $sheets->{$name};
        my ($question, $fieldwork);
        for my $c (@$cells) {
            my $t = trim($c->[2]);
            $question //= $t if $t =~ /^QA3a?\.? What do you think are the two most important issues facing \(OUR COUNTRY\) at the moment\?/;
            $fieldwork //= $t if $t =~ /Fieldwork\s*:/;
        }
        next unless defined $question && defined $fieldwork;
        my $g = grid($cells);
        my ($head, %colof);
        for my $r (sort { $a <=> $b } keys %$g) {
            my %have = map { trim($g->{$r}{$_}) => $_ } keys %{$g->{$r}};
            if (exists $have{BE} && exists $have{DE} && exists $have{SE}) { $head = $r; %colof = map { $_ => $have{$_} } grep { exists $have{$_} } @eu; last; }
        }
        next unless defined $head;   # a contents sheet names the question and the fieldwork but holds no table
        my ($last, %n, %share);
        for my $r (sort { $a <=> $b } grep { $_ > $head } keys %$g) {
            my @lab = grep { $_ < $colof{BE} && trim($g->{$r}{$_}) =~ /[A-Za-z]/ && trim($g->{$r}{$_}) !~ /^-?[0-9.]+(?:E[-+]?[0-9]+)?$/i } keys %{$g->{$r}};
            my $label;
            if (@lab) { my @lines = grep { /[A-Za-z]/ } split /\n/, $g->{$r}{(sort { $a <=> $b } @lab)[-1]}; $label = trim($lines[-1]); $last = $label; }
            else { $label = $last; }   # a share row printed under its counts row, the label on the counts row (EB93's layout)
            next unless defined $label;
            my %row = map { $_ => trim($g->{$r}{$colof{$_}}) } grep { defined $g->{$r}{$colof{$_}} } keys %colof;
            if ($label =~ /^total$/i && !%n) { %n = %row; next; }
            next unless %row;
            my $shares = 1;
            for my $v (values %row) { $shares = 0 unless $v eq '-' || ($v =~ /^[0-9.eE+-]+$/ && $v <= 1.0000001); }
            next unless $shares;
            $share{$label} = { map { $_ => ($row{$_} eq '-' ? 0 : 0 + $row{$_}) } keys %row };
        }
        my ($d1, $m1, $y1, $d2, $m2, $y2);
        if ($fieldwork =~ m{(\d{1,2})/(\d{1,2})\s*-\s*(\d{1,2})/(\d{1,2})/(\d{4})}) { ($d1, $m1, $d2, $m2, $y2) = ($1, $2, $3, $4, $5); }
        elsif ($fieldwork =~ m{(\d{1,2})\s*-\s*(\d{1,2})/(\d{1,2})/(\d{4})}) { ($d1, $d2, $m2, $y2) = ($1, $2, $3, $4); $m1 = $m2; }
        else { die "wave $wave: fieldwork '$fieldwork' unread\n"; }
        $y1 = $m1 > $m2 ? $y2 - 1 : $y2;
        my $mid = (days($y1, $m1, $d1) + days($y2, $m2, $d2)) / 2;
        return { date => $mid, fieldwork => $fieldwork, sheet => $name, n => \%n, share => \%share, question => $question };
    }
    die "wave $wave: no sheet carries QA3 with its fieldwork\n";
}

# days since 1970-01-01 of a civil date (proleptic Gregorian)
sub days {
    my ($y, $m, $d) = @_;
    $y -= $m <= 2; my $era = floor($y / 400); my $yoe = $y - $era * 400;
    my $doy = floor((153 * ($m + ($m > 2 ? -3 : 9)) + 2) / 5) + $d - 1;
    my $doe = $yoe * 365 + floor($yoe / 4) - floor($yoe / 100) + $doy;
    return $era * 146097 + $doe - 719468;
}

if (@ARGV && $ARGV[0] eq '--labels') {
    for my $w (@ARGV > 1 ? @ARGV[1 .. $#ARGV] : sort { $a <=> $b } keys %wavefile) {
        my $q = qa3($w);
        printf "EB%d  %s  sheet %s  date %.1f  n(DE) %s\n    %s\n", $w, $q->{fieldwork}, $q->{sheet}, $q->{date}, $q->{n}{DE} // '?', join(' | ', sort keys %{$q->{share}});
    }
    exit 0;
}

# ---------------------------------------------------------------- the design as data (the header's table)
my @classes = (
    # class, issue slot, item labels (a unit's share is the largest of them printed), units [country (or EU), the event's date, pre, post, decay waves...]
    ['Economy',   'Economy',     [qr/^Rising prices/i, qr/^(?:The )?economic situation$/i],   [['EU', '2022-02-24', 96, 97, 98, 99, 100, 101]]],
    ['Health',    'Healthcare',  [qr/^Health(?: and social security)?(?: \(M\))?$/i],          [['EU', '2020-03-11', 92, 93, 94, 95, 96]]],
    ['Migration', 'Immigration', [qr/^Immigration$/i],                                          [['EU', '2015-09-05', 83, 84, 85, 86, 87, 88]]],
    ['Security',  'Defense',     [qr/^Terrorism$/i],                                            [['BE', '2016-03-22', 84, 85, 86, 87], ['FR', '2016-07-14', 85, 86, 87, 88], ['DE', '2016-12-19', 86, 87, 88, 89],
                                                                                                 ['SE', '2017-04-07', 86, 87, 88, 89], ['ES', '2017-08-17', 87, 88, 89, 90]]],
    ['Crime',     'Crime',       [qr/^Crime$/i],                                                [['SE', '2023-09-28', 99, 100, 101, 102, 103]]],
    ['Natural',   'Climate',     [qr/^The environment, climate and energy issues$/i],           [['SE', '2018-07-15', 89, 90, 91, 92]]],
);

my $dry = @ARGV && $ARGV[0] eq '--dry';
die "run from the project root\n" unless -d 'Assets/Scripts/Elections';

# the digests the folder's SHA256SUMS.txt lists, held against the bytes
my %sums;
for (split /\n/, slurp($D . 'SHA256SUMS.txt')) { s/\r$//; $sums{$2} = $1 if /^([0-9a-f]{64}) \*?(\S+)$/; }
my %digest;
for my $w (sort { $a <=> $b } keys %wavefile) {
    my $f = $wavefile{$w}[0];
    $digest{$w} = sha256_hex(slurp($D . $f));
    die "$f: its SHA-256 $digest{$w} is not the one SHA256SUMS.txt lists\n" unless ($sums{$f} // '') eq $digest{$w};
}

my %q;
sub wave { my ($w) = @_; $q{$w} //= qa3($w); return $q{$w}; }

# a country's share of a class's item in a wave (the largest of the class's labels printed for it), or undef
sub share_of {
    my ($w, $country, $items) = @_;
    my $best;
    for my $label (keys %{wave($w)->{share}}) {
        next unless grep { $label =~ $_ } @$items;
        my $v = wave($w)->{share}{$label}{$country};
        $best = $v if defined $v && (!defined $best || $v > $best);
    }
    return $best;
}

# ---- the series rows: class, unit, step (-1 pre, 0 post, k decay), wave, wave date, share, n, the unit's event date (days since 1970-01-01).
# The United Kingdom is a member in a wave fielded before 31 January 2020 (its withdrawal), and an "EU" unit is a member in both its pre-
# and its post-wave.
my $Brexit = days(2020, 1, 31);
sub member { my ($country, $w) = @_; return $member{$country} && ($country ne 'UK' || wave($w)->{date} < $Brexit); }
my @rows;
for my $c (@classes) {
    my ($class, $slot, $items, $units) = @$c;
    for my $u (@$units) {
        my ($who, $event, @waves) = @$u;
        my ($ey, $em, $ed) = split /-/, $event;
        my $eday = days($ey, $em, $ed);
        die "$class $who: the event ($event) is not before its post-wave's fieldwork\n" unless $eday < wave($waves[1])->{date};
        for my $country ($who eq 'EU' ? grep { member($_, $waves[0]) && member($_, $waves[1]) } @eu : ($who)) {
            next unless defined share_of($waves[0], $country, $items) && defined share_of($waves[1], $country, $items);
            for my $i (0 .. $#waves) {
                my $s = share_of($waves[$i], $country, $items);
                next unless defined $s;
                push @rows, [$class, $country, $i - 1, $waves[$i], wave($waves[$i])->{date}, $s, wave($waves[$i])->{n}{$country} // '', $eday];
            }
        }
    }
}

# ---------------------------------------------------------------- the fit (SalienceFitCheck repeats it on the TSV's rows)
my $PI = 3.141592653589793;
sub tcdf_two_sided {   # P(|T| < t), Student's t with integer nu degrees of freedom (Abramowitz & Stegun 26.7.3, 26.7.4)
    my ($t, $nu) = @_;
    my $th = atan2($t, sqrt($nu)); my ($s, $c) = (sin($th), cos($th));
    my ($sum, $term) = (1, 1);
    if ($nu % 2) {
        return 2 * $th / $PI if $nu == 1;
        for (my $k = 2; $k <= $nu - 3; $k += 2) { $term *= $k / ($k + 1) * $c * $c; $sum += $term; }
        return 2 / $PI * ($th + $s * $c * $sum);
    }
    for (my $k = 1; $k <= $nu - 3; $k += 2) { $term *= $k / ($k + 1) * $c * $c; $sum += $term; }
    return $s * $sum;
}
# the two-sided 95 % point of Student's t, by bisection on its distribution function
sub tq { my ($nu) = @_; my ($lo, $hi) = (0, 1000); for (1 .. 200) { my $m = ($lo + $hi) / 2; if (tcdf_two_sided($m, $nu) < 0.95) { $lo = $m; } else { $hi = $m; } } return ($lo + $hi) / 2; }
my $Z = 1.959963984540054;

# lambda minimising sum_k (E_k - J exp(-lambda dt_k))^2 over the decay steps: the best of GridPoints + 1 log-spaced rates from LambdaLow to
# LambdaHigh a day, refined by golden section between its two neighbours. Returns (lambda, edge): edge -1 or +1 where the best grid point is
# the grid's own end (no decay the series resolves; the excess gone before the first decay wave), 0 inside - and +1 also where the fitted
# excess at the first decay wave is under Resolution, half a point: the sheets print whole per cents, so such a decay cannot be told from
# one complete before that wave (the fit is flat out to the grid's end).
my ($LambdaLow, $LambdaHigh, $GridPoints, $Resolution) = (1e-6, 1.0, 2000, 0.005);
sub decay {
    my ($j, $steps) = @_;
    return (0, -1) unless $j > 0 && @$steps;
    my $sse = sub { my ($l) = @_; my $s = 0; $s += ($_->[1] - $j * exp(-$l * $_->[2])) ** 2 for @$steps; return $s; };
    my $grid = sub { my ($i) = @_; return $LambdaLow * ($LambdaHigh / $LambdaLow) ** ($i / $GridPoints); };
    my ($best, $bi) = (undef, 0);
    for my $i (0 .. $GridPoints) { my $s = $sse->($grid->($i)); if (!defined $best || $s < $best) { ($best, $bi) = ($s, $i); } }
    return ($grid->($bi), $bi == 0 ? -1 : 1) if $bi == 0 || $bi == $GridPoints;
    my ($a, $b) = ($grid->($bi - 1), $grid->($bi + 1));
    my $g = (sqrt(5) - 1) / 2;
    my ($c, $d) = ($b - $g * ($b - $a), $a + $g * ($b - $a));
    my ($fc, $fd) = ($sse->($c), $sse->($d));
    for (1 .. 100) {
        if ($fc < $fd) { ($b, $d, $fd) = ($d, $c, $fc); $c = $b - $g * ($b - $a); $fc = $sse->($c); }
        else { ($a, $c, $fc) = ($c, $d, $fd); $d = $a + $g * ($b - $a); $fd = $sse->($d); }
    }
    my $l = ($a + $b) / 2;
    my $first = $steps->[0][2]; for (@$steps) { $first = $_->[2] if $_->[2] < $first; }
    return ($l, $j * exp(-$l * $first) < $Resolution ? 1 : 0);
}

# one class's jump at the survey's PEAK, its decay after the peak, and the jump BACK-CAST to the event's day (each unit's rise at the peak wave
# times exp(lambda x the days from its event to that wave), averaged; lambda taken as 0 where the decay is unresolved, so the back-cast is the
# measured rise). K69 (Elias, 2026-10-23, docs/RULINGS_2026-10-23.md): the jump is read at the excess's peak - the step (0 the post-wave, k the
# k-th decay wave) whose mean excess over the pre-wave is the largest, the earliest on a tie - and the decay is fitted on the steps after it,
# their times counted from it. A leave-one-out refit keeps the full sample's peak step ($peak), so the jackknife is a smooth function of the
# data (a peak that hopped between steps would make the interval a mixture of two estimands).
sub fit_rows {
    my ($rows, $peak) = @_;
    my (%pre, %post, %at);
    for my $r (@$rows) {
        my ($class, $unit, $step, $w, $date, $s, $n, $event) = @$r;
        if ($step == -1) { $pre{$unit} = [$s, $n, $date]; } elsif ($step == 0) { $post{$unit} = [$s, $n, $date, $event]; } else { $at{$step}{$unit} = [$s, $date, $n]; }
    }
    my @units = sort grep { exists $post{$_} } keys %pre;
    my $first = 0; $first += $post{$_}[0] - $pre{$_}[0] for @units; $first /= @units;
    my @steps;
    for my $k (sort { $a <=> $b } keys %at) {
        my @in = grep { exists $at{$k}{$_} } @units;
        next unless @in;
        my ($e, $dt) = (0, 0);
        for (@in) { $e += $at{$k}{$_}[0] - $pre{$_}[0]; $dt += $at{$k}{$_}[1] - $post{$_}[2]; }
        push @steps, [$k, $e / @in, $dt / @in, scalar @in];
    }
    unless (defined $peak) { $peak = 0; my $best = $first; for (@steps) { ($peak, $best) = ($_->[0], $_->[1]) if $_->[1] > $best; } }
    # the peak's own wave per unit: [share, date, n] (the post-wave at step 0)
    my %atpeak = $peak == 0 ? (map { $_ => [$post{$_}[0], $post{$_}[2], $post{$_}[1]] } @units) : (map { $_ => $at{$peak}{$_} } grep { exists $at{$peak}{$_} } @units);
    my @inpeak = grep { exists $atpeak{$_} } @units;
    my ($jump, $dtp) = ($first, 0);
    if ($peak != 0) { my ($s) = grep { $_->[0] == $peak } @steps; ($jump, $dtp) = ($s->[1], $s->[2]); }
    my @after = map { [$_->[0], $_->[1], $_->[2] - $dtp, $_->[3]] } grep { $_->[0] > $peak } @steps;
    my ($lambda, $edge) = decay($jump, \@after);
    my $lb = $edge == 0 ? $lambda : 0;
    my ($j0, $lag) = (0, 0);
    for (@inpeak) { my $d = $atpeak{$_}[1] - $post{$_}[3]; $j0 += ($atpeak{$_}[0] - $pre{$_}[0]) * exp($lb * $d); $lag += $d; }
    $j0 /= @inpeak; $lag /= @inpeak;
    my $firstlag = 0; $firstlag += $post{$_}[2] - $post{$_}[3] for @units; $firstlag /= @units;   # K94: the event to the first wave after it, where the ramp starts
    return { units => \@units, jump => $jump, first => $first, firstlag => $firstlag, peak => $peak, steps => \@steps, after => \@after, lambda => $lambda, edge => $edge, j0 => $j0, lag => $lag, lb => $lb,
             pre => \%pre, post => \%post, atpeak => \%atpeak };
}

my @out;
my $report = '';
for my $c (@classes) {
    my ($class, $slot) = @$c;
    my @mine = grep { $_->[0] eq $class } @rows;
    my $f = fit_rows(\@mine);
    my $n = @{$f->{units}};
    my ($jlo, $jhi, $llo, $lhi, $j0lo, $j0hi);
    if ($n >= 2) {
        my (@jj, @ll, @j0);
        for my $drop (@{$f->{units}}) { my $g = fit_rows([grep { $_->[1] ne $drop } @mine], $f->{peak}); push @jj, $g->{jump}; push @ll, $g->{lambda}; push @j0, $g->{j0}; }
        my $se = sub { my ($v) = @_; my $m = 0; $m += $_ for @$v; $m /= @$v; my $ss = 0; $ss += ($_ - $m) ** 2 for @$v; return sqrt(($n - 1) / $n * $ss); };
        my $t = tq($n - 1);
        ($jlo, $jhi) = ($f->{jump} - $t * $se->(\@jj), $f->{jump} + $t * $se->(\@jj));
        ($llo, $lhi) = ($f->{lambda} - $t * $se->(\@ll), $f->{lambda} + $t * $se->(\@ll));
        ($j0lo, $j0hi) = ($f->{j0} - $t * $se->(\@j0), $f->{j0} + $t * $se->(\@j0));
    } else {
        my $u = $f->{units}[0];
        my ($p0, $n0) = @{$f->{pre}{$u}}; my ($p1, $n1) = ($f->{atpeak}{$u}[0], $f->{atpeak}{$u}[2]);   # the peak's wave against the pre-wave
        my $se = sqrt($p1 * (1 - $p1) / $n1 + $p0 * (1 - $p0) / $n0);
        ($jlo, $jhi) = ($f->{jump} - $Z * $se, $f->{jump} + $Z * $se);
        my $k = exp($f->{lb} * $f->{lag});
        ($j0lo, $j0hi) = ($jlo * $k, $jhi * $k);
    }
    my $why = $f->{edge} < 0 ? 'no decay the series resolves' : $f->{edge} > 0 ? 'its excess was gone before the first decay wave' : !($jlo > 0) ? 'its jump\'s interval does not exclude zero' : '';
    my $generate = $why eq '';
    my $hl = $f->{edge} == 0 ? log(2) / $f->{lambda} : undef;
    my $line = sprintf "%-9s -> %-11s n %2d unit(s); jump at the survey %+.4f (95%% %+.4f .. %+.4f); lambda %.6f /day%s (95%% %s .. %s); half-life %s days (95%% %s .. %s)\n",
        $class, $slot, $n, $f->{jump}, $jlo, $jhi, $f->{lambda}, ($f->{edge} < 0 ? ' [the grid\'s low end: no decay resolved]' : $f->{edge} > 0 ? ' [the grid\'s high end: gone before the first decay wave]' : ''),
        defined $llo ? sprintf('%.6f', $llo) : 'none', defined $lhi ? sprintf('%.6f', $lhi) : 'none',
        defined $hl ? sprintf('%.1f', $hl) : 'none', (defined $lhi && $lhi > 0 ? sprintf('%.1f', log(2) / $lhi) : 'none'), (defined $llo ? ($llo > 0 ? sprintf('%.1f', log(2) / $llo) : 'unbounded') : 'none');
    $line .= sprintf "            read at the excess's peak: step %d (%s), %+.4f; the post-wave's own rise %+.4f; decay fitted on %d step(s) after the peak\n",
        $f->{peak}, (@{$c->[3]} == 1 ? 'EB' . $c->[3][0][3 + $f->{peak}] : 'each unit\'s own wave'), $f->{jump}, $f->{first}, scalar @{$f->{after}};
    $line .= sprintf "            held %d days (the event to the peak's wave, %.1f days on the units' mean); reported only, the back-cast to the event's day: %+.4f (95%% %+.4f .. %+.4f)\n", int($f->{lag} + 0.5), $f->{lag}, $f->{j0}, $j0lo, $j0hi;
    $line .= sprintf "            units' rise at the peak: %s\n", join(' ', map { sprintf '%s %+.2f', $_, $f->{atpeak}{$_}[0] - $f->{pre}{$_}[0] } grep { exists $f->{atpeak}{$_} } @{$f->{units}});
    $line .= sprintf "            excess by step: E0 %+.4f; %s\n", $f->{first}, join('; ', map { sprintf 'step %d: E %+.4f at %.1f days (%d units)', @$_[0 .. 3] } @{$f->{steps}});
    $line .= "            NOT GENERATED: $why - the class stays inert\n" unless $generate;
    $report .= $line;
    push @out, { class => $class, slot => $slot, n => $n, jump => $f->{jump}, jlo => $jlo, jhi => $jhi, lambda => $f->{lambda}, llo => $llo, lhi => $lhi,
                 j0 => $f->{j0}, j0lo => $j0lo, j0hi => $j0hi, lag => $f->{lag}, edge => $f->{edge}, generate => $generate, why => $why, peak => $f->{peak}, first => $f->{first}, firstlag => $f->{firstlag} };
}

# ---------------------------------------------------------------- K69's USA: the two instruments' bases
# A share on either instrument is the respondents naming the issue; its BASE is the mentions a respondent gives on the instrument, the sum of
# every response category the table prints (each instrument's "no opinion" / "don't know" among them). Gallup's table prints that sum itself
# (its "Total" row: the question is open-ended and a respondent may name more than one problem, so it exceeds 100 %); Eurobarometer's QA3
# allows two answers, and its sum is read here off every stored sheet, per member and wave (the French duplicate of the Cyprus item on the
# newer sheets, "La question chypriote", is left out: it repeats "Cyprus issue"). THE CONVERSION (DECLARED): a jump fitted on Eurobarometer's
# shares lands on a Gallup seed scaled by Gallup's base over Eurobarometer's - for an issue holding a given part of the mentions, its share
# scales with the mentions a respondent gives.
my $G = 'ElectionsData/usa/raw/salience/';
my $gallupFile = 'gallup_mip_vEKjO_47_dataset.csv';
my %gsums;
for (split /\n/, slurp($G . 'SHA256SUMS.txt')) { s/\r$//; $gsums{$2} = $1 if /^([0-9a-f]{64}) \*?(\S+)$/; }
my $gallupDigest = sha256_hex(slurp($G . $gallupFile));
die "$gallupFile: its SHA-256 $gallupDigest is not the one its SHA256SUMS.txt lists\n" unless ($gsums{$gallupFile} // '') eq $gallupDigest;
my (@gmonths, @gtotals);
for (split /\n/, slurp($G . $gallupFile)) {
    s/\r$//; my @f = split /\t/;
    @gmonths = @f[1 .. $#f] if !@gmonths && @f > 1 && $f[0] eq '' && $f[1] =~ /^[A-Z][a-z]{2}-\d\d$/;
    if (@f > 1 && $f[0] eq 'Total') { @gtotals = map { my $v = $_; $v =~ s/%$//; die "Gallup's Total '$_' unread\n" unless $v =~ /^\d+$/; $v / 100 } @f[1 .. $#f]; }
}
die "Gallup's table: " . scalar(@gmonths) . " months, " . scalar(@gtotals) . " totals\n" unless @gmonths && @gmonths == @gtotals;
my $gallupBase = 0; $gallupBase += $_ for @gtotals; $gallupBase /= @gtotals;
my (@base, $ebBase);
for my $w (sort { $a <=> $b } keys %wavefile) {
    my $q = wave($w);
    for my $country (grep { member($_, $w) } @eu) {
        my ($sum, $items) = (0, 0);
        for my $label (sort keys %{$q->{share}}) {
            next if $label =~ /^La question chypriote$/;
            my $v = $q->{share}{$label}{$country};
            next unless defined $v;
            $sum += $v; $items++;
        }
        next unless $items;
        push @base, [$w, $country, $sum, $items];
    }
}
$ebBase = 0; $ebBase += $_->[2] for @base; $ebBase /= @base;
my ($ebLo, $ebHi) = (9, 0); for (@base) { $ebLo = $_->[2] if $_->[2] < $ebLo; $ebHi = $_->[2] if $_->[2] > $ebHi; }
my $usConversion = $gallupBase / $ebBase;
$report .= sprintf "K69 the USA: Gallup's base %.4f mentions a respondent (the mean of its Total row over %d months, %s .. %s); Eurobarometer's QA3 base %.4f (%d member-waves, %.2f .. %.2f); K69's conversion x %.4f (dropped by K103: the USA lands at scale one)\n",
    $gallupBase, scalar @gtotals, $gmonths[-1], $gmonths[0], $ebBase, scalar @base, $ebLo, $ebHi, $usConversion;

# ---------------------------------------------------------------- K102: the conversion out of sample (the header's declared criterion)
my $K = 'ElectionsData/usa/raw/salience/k102/';
my %ksums;
for (split /\n/, slurp($K . 'SHA256SUMS.txt')) { s/\r$//; $ksums{$2} = $1 if /^([0-9a-f]{64}) \*?(\S+)$/; }
my %kdigest;
sub kbytes { my ($n) = @_; my $b = slurp($K . $n); my $d = sha256_hex($b); die "$n: its SHA-256 $d is not the one k102/SHA256SUMS.txt lists\n" unless ($ksums{$n} // '') eq $d; $kdigest{$n} = $d; return $b; }
# a cell as a share (0..1): a whole per cent, or undef for "*" (under half a per cent) and "--" (not mentioned)
sub kval { my ($v) = @_; $v = trim($v); $v =~ s/%$//; return $v =~ /^\d+$/ ? $v / 100 : undef; }
# a table: [ [months...], { label => [shares...] } ] - the Datawrapper dataset (tab-separated) or the trends page's FIRST table (its months in
# the header's scope="col" cells, each row's label in its scope="row" cell)
sub ktable {
    my ($n) = @_;
    my $b = kbytes($n);
    my (@m, %r);
    if ($n =~ /\.csv$/) {
        for (split /\n/, $b) {
            s/\r$//; my @f = split /\t/; next unless @f > 1;
            if (!@m && $f[0] eq '' && $f[1] =~ /^[A-Z][a-z]{2}[- ]\d/) { @m = @f[1 .. $#f]; next; }
            my $l = $f[0]; $l =~ s/<[^>]+>//g; $l = trim($l); next if $l eq '';
            $r{$l} = [map { kval($_) } @f[1 .. $#f]];
        }
    } else {
        my ($t) = $b =~ /(<table.*?<\/table>)/s or die "$n: no table\n";
        @m = map { trim($_) } $t =~ /<th colspan="1" scope="col">(.*?)<\/th>/gs;
        while ($t =~ /<tr>(.*?)<\/tr>/gs) {
            my $tr = $1; my ($l) = $tr =~ /<th scope="row">(.*?)<\/th>/s or next;
            $l =~ s/<[^>]+>//g; $l = xml_text(trim($l));
            $r{$l} = [map { kval($_) } $tr =~ /<td[^>]*>(.*?)<\/td>/gs];
        }
    }
    return [\@m, \%r];
}
# the tested years (the header's design): the item, the class, the pre column, the post columns, the tables they are read from
my @k102 = (
    { year => 2020, item => 'Coronavirus/Diseases', class => 'Health', pre => 'Feb 2020', post => ['Apr 2020', 'May 2020', 'Jun 2020', 'Jul 2020', 'Aug 2020', 'Sep 2020'],
      files => ['wayback_gallup_mip_page_20201028085700.html'], months => qr/ 2020$/ },
    { year => 2022, item => 'High cost of living/Inflation', class => 'Economy', pre => 'Feb 2022', post => ['Mar 2022', 'Apr 2022', 'May 2022', 'Jun 2022', 'Jul 2022', 'Aug 2022'],
      files => ['wayback_gallup_mip_page_20220425021115.html', 'gallup_mip_vEKjO_1_dataset.csv'], months => qr/ 2022$/ },
);
# 2015: the trends page of the time prints its table as an image, so the year is BILLED - held here: the stored page carries no text table
{ my $b = kbytes('wayback_gallup_mip_page_20150915083712.html'); die "2015: the stored page carries a text table - read it, do not bill it\n" if $b =~ /<table/i; }
my $surveyJump = { map { $_->{class} => $_->{jump} } @out };
my @krows; my ($kNear, $kBaseOk) = (0, 1);
for my $y (@k102) {
    my (%total, %item);
    for my $f (@{$y->{files}}) {
        my ($m, $r) = @{ktable($f)};
        die "$f: no Total row or no '$y->{item}' row\n" unless $r->{Total} && $r->{$y->{item}};
        for my $i (0 .. $#$m) { next unless $m->[$i] =~ $y->{months}; $total{$m->[$i]} //= $r->{Total}[$i]; $item{$m->[$i]} //= $r->{$y->{item}}[$i]; }
    }
    my @cols = sort keys %total;
    my $base = 0; $base += $total{$_} for @cols; $base /= @cols;
    die "$y->{year}: no '$y->{pre}' column\n" unless defined $item{$y->{pre}};
    my ($peak, $at) = (undef, ''); for (@{$y->{post}}) { die "$y->{year}: no '$_' column\n" unless exists $item{$_}; my $v = $item{$_} // 0; ($peak, $at) = ($v, $_) if !defined $peak || $v > $peak; }
    my $rise = $peak - $item{$y->{pre}};
    my $eb = $surveyJump->{$y->{class}};
    my ($dConv, $dNone) = (abs(log($rise / ($eb * $usConversion))), abs(log($rise / $eb)));
    my $baseOk = abs($base / $gallupBase - 1) <= 0.15;
    $kBaseOk &&= $baseOk; $kNear++ if $dConv < $dNone;
    push @krows, [$y->{year}, scalar @cols, $base, $y->{item}, $y->{pre}, $item{$y->{pre}}, $at, $peak, $rise, $y->{class}, $eb, $rise / $eb];
    $report .= sprintf "K102 %d: Gallup's base %.4f over %d columns (%s the 2024 base's +-15 %%); '%s' %s %.2f -> %s %.2f, a rise of %+.4f against the %s class's EB jump %+.4f: the share ratio %.4f (the conversion %.4f); |ln| to the converted %.3f, to the unconverted %.3f\n",
        $y->{year}, $base, scalar @cols, $baseOk ? 'inside' : 'OUTSIDE', $y->{item}, $y->{pre}, $item{$y->{pre}}, $at, $peak, $rise, $y->{class}, $eb, $rise / $eb, $usConversion, $dConv, $dNone;
}
my $kKeep = $kBaseOk && $kNear * 2 > @k102;
$report .= sprintf "K102 verdict: (a) the base %s; (b) the converted jump nearer in %d of %d years - the conversion %s; 2015 BILLED (its table is an image)\n",
    $kBaseOk ? 'is instrument-stable' : 'FAILS', $kNear, scalar @k102, $kKeep ? 'is KEPT' : 'FAILS and goes back to Elias';
print $report;
exit 0 if $dry;

# ---------------------------------------------------------------- the outputs
sub g17 { my ($v) = @_; return 'double.NaN' unless defined $v; my $s = sprintf('%.17g', $v); $s .= '.0' unless $s =~ /[.eE]/; return $s; }

my $tsv = "# GENERATED by Tools/aloop_salience_fit.pl - never hand-edited. A-LOOP q5 (docs/specs/A_LOOP_PLAN.md section 6 (3)): every Eurobarometer\n"
        . "# QA3 share the salience fit reads, read out of the stored Volume A workbooks (docs/reference/raw/aloop/eb/, digests in its SHA256SUMS.txt).\n"
        . "# source rows: wave, stored file, its SHA-256, the fieldwork its sheet prints, the sheet, the fieldwork's midpoint (days since 1970-01-01).\n"
        . "# row rows: class, unit (the country), step (-1 the pre-wave, 0 the post-wave, k the k-th decay wave), wave, the wave's date, the share\n"
        . "# (0..1, the sheet's figure; the larger of the class's items where it reads two), the weighted total the sheet prints for the country,\n"
        . "# the unit's event date (days since 1970-01-01).\n"
        . "# base rows (K69, the USA's conversion): wave, member, the sum of every QA3 share its sheet prints for the member (the mentions a\n"
        . "# respondent gives; the French duplicate of the Cyprus item left out), the count of items summed.\n"
        . "# gallupsource row: the stored Gallup table (ElectionsData/usa/raw/salience/), its SHA-256; gallup rows: a month, its Total row (0..).\n"
        . "# SalienceFitCheck refits from these rows; regenerate with: perl Tools/aloop_salience_fit.pl\n";
my %used; $used{$_->[3]} = 1 for @rows;
for my $w (sort { $a <=> $b } keys %used) {
    my $fw = wave($w)->{fieldwork}; $fw =~ s/\t/ /g;
    $tsv .= join("\t", 'source', $w, $wavefile{$w}[0], $digest{$w}, $fw, wave($w)->{sheet}, g17(wave($w)->{date})) . "\n";
}
for my $r (@rows) { $tsv .= join("\t", 'row', $r->[0], $r->[1], $r->[2], $r->[3], g17($r->[4]), g17($r->[5]), $r->[6] eq '' ? '' : g17($r->[6]), g17($r->[7])) . "\n"; }
for my $b (@base) { $tsv .= join("\t", 'base', $b->[0], $b->[1], g17($b->[2]), $b->[3]) . "\n"; }
$tsv .= join("\t", 'gallupsource', $G . $gallupFile, $gallupDigest) . "\n";
for my $i (0 .. $#gmonths) { $tsv .= join("\t", 'gallup', $gmonths[$i], g17($gtotals[$i])) . "\n"; }
# the series' digest, over its text with LF line ends (a checkout's CRLF does not change it); SalienceFitCheck takes it the same way
my $seriesDigest = sha256_hex($tsv);

my $cs = "// GENERATED by Tools/aloop_salience_fit.pl. DO NOT EDIT BY HAND.\n//\n"
       . "// Source : docs/generated/EB_QA3_SERIES.tsv (the shares read out of the stored Eurobarometer Volume A workbooks,\n"
       . "//          docs/reference/raw/aloop/eb/, each workbook's SHA-256 on its source row and in that folder's SHA256SUMS.txt)\n"
       . "// SHA-256: $seriesDigest (its text with LF line ends)\n//\n"
       . "// SalienceFitCheck re-reads the series, holds it and every workbook against its digest and refits every figure.\n\n"
       . "namespace PoliSim.Elections.Generated\n{\n"
       . "    /// <summary>A-LOOP q5 (`docs/specs/A_LOOP_PLAN.md` section 6 (3)): the jump an event of each class gives the issue it names, and the rate it\n"
       . "    /// decays at, FITTED on Eurobarometer's QA3 (\"the two most important issues facing (OUR COUNTRY)\") around dated real events - the design,\n"
       . "    /// the units and the estimator are `Tools/aloop_salience_fit.pl`'s header. A jump is in share units (0..1, the survey's two-mention\n"
       . "    /// share) read at the excess's PEAK (K69: the post-wave or a later wave, the class's PeakStep), which the game lands held flat for the\n"
       . "    /// class's hold (the event to the peak's wave) and then decays at the rate fitted on the waves after the peak; the back-cast to the event's\n"
       . "    /// day is reported only. A rate is a day's; an interval is 95 %. A class is generated where its decay is resolved and its jump's interval\n"
       . "    /// excludes zero; otherwise its jump is zero (inert) and its measured figures stand beside it for the record. The USA's conversion\n"
       . "    /// (K69) is Gallup's base over Eurobarometer's, each the mentions a respondent gives on its instrument. Generated, never hand-edited.</summary>\n"
       . "    public static class EventSalienceFit\n    {\n"
       . "        /// <summary>DERIVED: the series' digest (its text with LF line ends), the decay search's range (a day) and grid, and the series' resolution\n"
       . "        /// (half the sheets' whole per cent) - the tool's.</summary>\n"
       . "        public const string SeriesDigest = \"$seriesDigest\";\n"
       . "        public const double LambdaLow = " . g17($LambdaLow) . ", LambdaHigh = " . g17($LambdaHigh) . ", Resolution = " . g17($Resolution) . ";\n"
       . "        public const int GridPoints = $GridPoints;\n        public const int SeriesRows = " . scalar(@rows) . ";\n"
       . "\n        /// <summary>DERIVED from SOURCED tables (K69, the USA's conversion): Gallup's base - the mean of its Total row over the stored months\n"
       . "        /// (`$G$gallupFile`, SHA-256 below) - and Eurobarometer's QA3 base - the mean over the series' base rows of a member's summed shares in a\n"
       . "        /// wave - each the mentions a respondent gives on its instrument; their ratio was K69's conversion for the USA, which failed its K102 test; since K103 the game applies none (the record).</summary>\n"
       . "        public const string GallupDigest = \"$gallupDigest\";\n"
       . "        public const int GallupMonths = " . scalar(@gtotals) . ", BaseRows = " . scalar(@base) . ";\n"
       . "        public const double GallupBase = " . g17($gallupBase) . ", EurobarometerBase = " . g17($ebBase) . ";\n"
       . "        public const double UsConversion = GallupBase / EurobarometerBase;\n";
$cs .= "\n        /// <summary>DERIVED from SOURCED tables (K102, the conversion out of sample; the criterion is the tool's header, declared before the\n"
     . "        /// figures): per tested year, Gallup's base (its Total row's mean), the surging item's rise from the last column before the event to its\n"
     . "        /// largest after, the class's Eurobarometer jump, and the share ratio (rise over jump) - read from `ElectionsData/usa/raw/salience/k102/`.\n"
     . "        /// 2015 is BILLED (the trends page printed its table as an image). The verdict: BaseStable (every base within 15 % of GallupBase),\n"
     . "        /// ConvertedNearer (the years the converted jump is nearer the rise in |ln|) and Kept. Not read by the game: the test failed and K103 (Elias,\n"
     . "        /// 2026-10-24) dropped the conversion - the USA lands the jump at scale one; these figures are the record of why.</summary>\n";
for my $k (@krows) {
    my ($yr, $ncol, $base, $item, $pre, $preS, $at, $peak, $rise, $class, $eb, $ratio) = @$k;
    $cs .= "        public const int K102Columns$yr = $ncol;\n"
         . "        public const double K102Base$yr = " . g17($base) . ", K102Pre$yr = " . g17($preS) . ", K102Peak$yr = " . g17($peak) . ", K102Rise$yr = " . g17($rise)
         . ", K102EbJump$yr = " . g17($eb) . ", K102Ratio$yr = " . g17($ratio) . ";   // '$item' $pre -> $at, the $class class\n";
}
$cs .= "        public const bool K102BaseStable = " . ($kBaseOk ? 'true' : 'false') . ", K102Kept = " . ($kKeep ? 'true' : 'false') . ";\n"
     . "        public const int K102ConvertedNearer = $kNear, K102Years = " . scalar(@k102) . ";\n";
for my $o (@out) {
    my $k = $o->{class};
    $cs .= "\n        /// <summary>FITTED: $k (the $o->{slot} slot) - $o->{n} unit(s); " . ($o->{generate} ? 'generated' : "NOT generated: $o->{why} - inert") . ".</summary>\n";
    $cs .= "        public const bool ${k}Generated = " . ($o->{generate} ? 'true' : 'false') . ";\n";
    $cs .= "        public const int ${k}Units = $o->{n};\n";
    $cs .= "        public const double ${k}SurveyJump = " . g17($o->{jump}) . ", ${k}SurveyJumpLower = " . g17($o->{jlo}) . ", ${k}SurveyJumpUpper = " . g17($o->{jhi}) . ";\n";
    $cs .= "        public const double ${k}EventToSurveyDays = " . g17($o->{lag}) . ";\n";
    $cs .= "        public const double ${k}EventDayJump = " . g17($o->{j0}) . ", ${k}EventDayJumpLower = " . g17($o->{j0lo}) . ", ${k}EventDayJumpUpper = " . g17($o->{j0hi}) . ";\n";
    $cs .= "        public const double ${k}MeasuredLambda = " . g17($o->{lambda}) . ", ${k}LambdaLower = " . g17($o->{llo}) . ", ${k}LambdaUpper = " . g17($o->{lhi}) . ";\n";
    $cs .= "        public const double ${k}Jump = " . ($o->{generate} ? "${k}SurveyJump" : '0.0') . ", ${k}LambdaPerDay = " . ($o->{generate} ? "${k}MeasuredLambda" : '0.0') . ";\n";
    $cs .= "        public const int ${k}HoldDays = " . ($o->{generate} ? int($o->{lag} + 0.5) : 0) . ";\n";
    $cs .= "        public const int ${k}PeakStep = $o->{peak};\n";
    $cs .= "        public const double ${k}FirstWaveJump = " . g17($o->{first}) . ";\n";
    # K94 (Elias, 2026-10-24): the ramp - the first wave's level held from the event to the first wave, then rising linearly to the peak by the
    # peak's wave (no ramp where the peak is the first wave); both day counts the units' mean, rounded half up, as the hold is
    {
        my $hold = $o->{generate} ? int($o->{lag} + 0.5) : 0;
        my $fh = !$o->{generate} ? 0 : $o->{peak} == 0 ? $hold : int($o->{firstlag} + 0.5);
        $cs .= "        public const int ${k}FirstHoldDays = $fh, ${k}RampDays = " . ($hold - $fh) . ";\n";
    }
}
$cs .= "    }\n}\n";

sub spew { my ($p, $s) = @_; open my $fh, '>:raw', $p or die "$p: $!\n"; print $fh $s or die "$p: $!\n"; close $fh or die "$p: $!\n"; }
spew('docs/generated/EB_QA3_SERIES.tsv', $tsv);
spew('Assets/Scripts/Elections/Generated/EventSalienceFit.cs', $cs);
printf "wrote docs/generated/EB_QA3_SERIES.tsv (%d rows from %d waves, SHA-256 %s) and Assets/Scripts/Elections/Generated/EventSalienceFit.cs\n", scalar @rows, scalar keys %used, $seriesDigest;
