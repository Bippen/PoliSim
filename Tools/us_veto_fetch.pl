#!/usr/bin/perl
# PS-6 US-10 - THE VETO RECORD'S SOURCES, FETCHED AND LOGGED. Three modes (run from the project root):
#   perl Tools/us_veto_fetch.pl pages        - the pages read whole, kept IN TREE under ElectionsData/usa/raw/vetoes/ (the Senate's veto lists
#                                              by president through the Wayback Machine - senate.gov answers 403 - the House Historian's counts,
#                                              the statutes' Senate procedures from the GPO's United States Code, the D.C. Code section, the CRS
#                                              report on the override); each appended to fetch_log.txt, SHA256SUMS.txt rewritten
#   perl Tools/us_veto_fetch.pl zips         - the GPO's BILLSTATUS bulk zips for the 115th-119th Congresses (hr, s, hjres, sjres), fetched OUT
#                                              OF TREE to ../PoliSim-captures/sources/usa_us10/billstatus/ where not already there, each digested
#                                              into out_of_tree.txt
#   perl Tools/us_veto_fetch.pl rolls <list> - the roll calls the prep tool lists (perl Tools/us_veto_prep.pl rolls > <list>): the Clerk's XML
#                                              for the House, the Senate's LIS XML through the Wayback Machine; OUT OF TREE under
#                                              ../PoliSim-captures/sources/usa_us10/rolls/, each digested into out_of_tree.txt
# A Wayback page is stored decoded (--compressed) and named by its capture; every other file as received. A file already on disk is not
# fetched again. A status other than 200, or a page that is not what it should be (its marker absent), stops the run.
use strict;
use warnings;
use Digest::SHA;
use POSIX qw(strftime);

my $mode = shift @ARGV // die "usage: perl Tools/us_veto_fetch.pl pages|zips|rolls <list>\n";
my $RAW = 'ElectionsData/usa/raw/vetoes';
my $OUT = '../PoliSim-captures/sources/usa_us10';
my $UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36';
die "run from the project root\n" unless -d 'ElectionsData/usa/raw';
mkdir $RAW unless -d $RAW;

sub now { strftime('%Y-%m-%dT%H:%M:%SZ', gmtime) }
sub sha_file { my $d = Digest::SHA->new(256); $d->addfile($_[0], 'b'); $d->hexdigest }

# curl one URL to a file; returns (status, bytes, effective url)
sub fetch {
    my ($url, $file, $compressed) = @_;
    # the Wayback Machine rate-limits (HTTP 429): a pause before each of its requests, and up to six retries on 429, backing off
    for my $try (0 .. 6) {
        sleep($url =~ /web\.archive\.org/ ? 2 : 0) if $try == 0;
        my @r = fetch_once($url, $file, $compressed);
        return @r unless ($r[0] eq '429' || $r[0] eq '000') && $try < 6;
        sleep(20 * ($try + 1));
    }
}

sub fetch_once {
    my ($url, $file, $compressed) = @_;
    my $tmp = "$file.part";
    my @cmd = ('curl', '-sS', '-L', ($compressed ? ('--compressed') : ()), '-A', $UA, '-H', 'Accept: text/html,application/xhtml+xml,application/xml,application/pdf,*/*',
               '-o', $tmp, '-w', '%{http_code} %{size_download} %{url_effective}', $url);
    open my $p, '-|', @cmd or die "curl: $!";
    my $w = do { local $/; <$p> } // '';
    close $p;
    my ($code, $bytes, $eff) = split / /, $w, 3;
    die "$url: curl failed ($w)\n" unless defined $code && $code =~ /^\d+$/;
    if ($code eq '200') { rename $tmp, $file or die "$file: $!"; } else { unlink $tmp; }
    return ($code, $bytes, $eff // $url);
}

sub log_line {
    my ($line) = @_;
    open my $l, '>>', "$RAW/fetch_log.txt" or die $!;
    print $l $line, "\n";
    close $l;
}

if ($mode eq 'pages') {
    # [file stem, url, wayback?, a marker the page must carry]
    my @pages = (
        ['history_house_presidential_vetoes', 'https://history.house.gov/Institution/Presidential-Vetoes/Presidential-Vetoes/', 0, 'Joseph R. Biden'],
        ['senate_vetoes_counts', 'https://www.senate.gov/legislative/vetoes/vetoCounts.htm', 1, 'TrumpDJ2.htm'],
        ['senate_vetoes_TrumpDJ', 'https://www.senate.gov/legislative/vetoes/TrumpDJ.htm', 1, 'S.J.Res.68'],
        ['senate_vetoes_BidenJR', 'https://www.senate.gov/legislative/vetoes/BidenJR.htm', 1, 'S.4199'],
        ['senate_vetoes_TrumpDJ2', 'https://www.senate.gov/legislative/vetoes/TrumpDJ2.htm', 1, 'H.R.504'],
        ['govinfo_USCODE-2023-title5-sec802', 'https://www.govinfo.gov/content/pkg/USCODE-2023-title5/html/USCODE-2023-title5-partI-chap8-sec802.htm', 0, 'Congressional disapproval procedure'],
        ['govinfo_USCODE-2023-title50-sec1546a', 'https://www.govinfo.gov/content/pkg/USCODE-2023-title50/html/USCODE-2023-title50-chap33-sec1546a.htm', 0, '1546a'],
        ['govinfo_USCODE-2023-title50-sec1622', 'https://www.govinfo.gov/content/pkg/USCODE-2023-title50/html/USCODE-2023-title50-chap34-subchapII-sec1622.htm', 0, 'National emergencies'],
        ['govinfo_USCODE-2023-title22-sec2776', 'https://www.govinfo.gov/content/pkg/USCODE-2023-title22/html/USCODE-2023-title22-chap39-subchapIII-sec2776.htm', 0, '2776'],
        ['dccode_1-206.04', 'https://code.dccouncil.gov/us/dc/council/code/sections/1-206.04', 0, '1-206.04'],
        ['crs_RS22654.11', 'https://www.congress.gov/crs_external_products/RS/PDF/RS22654/RS22654.11.pdf', 0, '%PDF'],
    );
    for my $pg (@pages) {
        my ($stem, $url, $wb, $marker) = @$pg;
        my $ext = $url =~ /\.pdf$/ ? 'pdf' : $url =~ /\.htm$/ ? 'htm' : 'html';
        my ($have) = grep { /^\Q$stem\E(_\d{14})?\.$ext$/ } do { opendir my $d, $RAW or die; readdir $d };
        if ($have) { print "kept: $have\n"; next; }
        my $req = $wb ? "https://web.archive.org/web/20261006000000id_/$url" : $url;
        my $tmpfile = "$RAW/$stem.$ext";
        my ($code, $bytes, $eff) = fetch($req, $tmpfile, $wb);
        my $at = now();
        die "$url: HTTP $code\n" unless $code eq '200';
        my $file = "$stem.$ext";
        my $capture = '';
        if ($wb) {
            ($capture) = $eff =~ m{/web/(\d{14})id_/} or die "$url: no capture stamp in $eff\n";
            $file = "${stem}_$capture.$ext";
            rename $tmpfile, "$RAW/$file" or die $!;
        }
        open my $fh, '<:raw', "$RAW/$file" or die; my $body = do { local $/; <$fh> }; close $fh;
        die "$file: the marker '$marker' is not on the page\n" if index($body, $marker) < 0;
        log_line(join("\t", $at, $code, -s "$RAW/$file", $file, $req . ($capture ? " (capture $capture)" : '')));
        print "fetched: $file\n";
    }
    # SHA256SUMS over every kept file
    opendir my $d, $RAW or die; my @files = sort grep { -f "$RAW/$_" && $_ !~ /^(SHA256SUMS\.txt|fetch_log\.txt|out_of_tree\.txt)$/ } readdir $d; closedir $d;
    open my $s, '>', "$RAW/SHA256SUMS.txt" or die; print $s sha_file("$RAW/$_"), " *$_\n" for @files; close $s;
    print "SHA256SUMS.txt: ", scalar(@files), " files\n";
}
elsif ($mode eq 'zips' || $mode eq 'rolls') {
    my %have;
    unless (-e "$RAW/out_of_tree.txt") {
        open my $h, '>', "$RAW/out_of_tree.txt" or die;
        print $h "# PS-6 US-10: the sources kept OUT OF TREE under PoliSim-captures/sources/usa_us10/ - sha256, bytes, path, url (capture), fetched (UTC). Written by Tools/us_veto_fetch.pl; Tools/us_veto_prep.pl holds every file it reads to its line.\n";
        close $h;
    }
    if (open my $m, '<', "$RAW/out_of_tree.txt") { while (<$m>) { next if /^#/; my @f = split /\t/; $have{$f[2]} = 1 if @f >= 3; } close $m; }
    my @todo;
    if ($mode eq 'zips') {
        for my $c (115 .. 119) { for my $t (qw(hr s hjres sjres)) { push @todo, ["billstatus/BILLSTATUS-$c-$t.zip", "https://www.govinfo.gov/bulkdata/BILLSTATUS/$c/$t/BILLSTATUS-$c-$t.zip", 0]; } }
        # P.L. 94-329 as enacted (90 Stat. 729): its section 601(b), the Senate's expedited procedure the War Powers (50 U.S.C. 1546a) and
        # arms-sale (22 U.S.C. 2776) disapprovals are considered under - not classified to the Code, so read from the Statutes at Large
        push @todo, ['statutes/STATUTE-90-Pg729.pdf', 'https://www.govinfo.gov/content/pkg/STATUTE-90/pdf/STATUTE-90-Pg729.pdf', 0];
    } else {
        my $list = shift @ARGV // die "rolls needs the list the prep tool writes\n";
        open my $l, '<', $list or die "$list: $!";
        while (<$l>) {
            chomp; next unless /\S/;
            my ($chamber, $url) = split /\t/;
            my ($name) = $url =~ m{/([^/]+\.xml)$} or die "$url: no file name\n";
            if ($chamber eq 'House') { my ($y) = $url =~ m{/evs/(\d{4})/} or die "$url\n"; push @todo, ["rolls/house/$y/$name", $url, 0]; }
            else { my ($cs) = $url =~ m{/(vote\d{4})/} or die "$url\n"; push @todo, ["rolls/senate/$cs/$name", "https://web.archive.org/web/20261006000000id_/$url", 1]; }
        }
        close $l;
    }
    my $added = 0;
    for my $t (@todo) {
        my ($rel, $url, $wb) = @$t;
        next if $have{$rel};
        my $path = "$OUT/$rel";
        (my $dir = $path) =~ s{/[^/]+$}{};
        system('mkdir', '-p', $dir) == 0 or die "mkdir $dir\n";
        my ($code, $bytes, $eff, $capture) = ('200', -s $path, $url, '');
        unless (-e $path) {
            ($code, $bytes, $eff) = fetch($url, $path, $wb);
            die "$url: HTTP $code\n" unless $code eq '200';
        }
        if ($wb) { ($capture) = $eff =~ m{/web/(\d{14})id_/}; die "$url: no capture stamp ($eff)\n" unless $capture; }
        open my $fh, '<:raw', $path or die; my $head = ''; read $fh, $head, 400; close $fh;
        die "$rel: not a roll call ($head)\n" if $mode eq 'rolls' && $head !~ /<(rollcall-vote|roll_call_vote)>/;
        open my $m, '>>', "$RAW/out_of_tree.txt" or die;
        print $m join("\t", sha_file($path), -s $path, $rel, $url . ($capture ? " (capture $capture)" : ''), now()), "\n";
        close $m;
        $have{$rel} = 1; $added++;
        print "$rel\n" if $added % 50 == 0;
    }
    print "out_of_tree.txt: $added added\n";
}
else { die "unknown mode $mode\n"; }
