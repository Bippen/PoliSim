#!/usr/bin/perl
# us_ballot_standing.pl - PS-6 US-6c: WHERE EACH OF 2024'S OTHER CANDIDATES STOOD - the jurisdictions whose ballot of record listed the column, the ones
# where it was only written in, and the ones where the FEC's workbook holds no vote for it. Prints only; writes nothing.
# Elias's ruling J3 (2026-10-19, COMPLETED.md s870): "a minor candidate stands only where on the ballot of record (the FEC's states)"; the count stands
# each column there (PresidencyOfRecord.StandingOfRecord). The ballot of record is read off the FEC's Official 2024 Presidential General Election Results
# workbook by its own note - "Italics indicate Write-In Votes" - so a roman cell is a listing and an italic one a write-in count (president_returns.md
# reading 15). This reads the generated table, ElectionsData/usa/president_by_candidate_state.csv (Tools/us_returns_prep.pl writes it from the saved
# workbook; GeneratedCatalogCheck holds it to the catalog), and the jurisdictions of 2024 from president_by_state.csv beside it.
# Run from the project root:  perl Tools/us_ballot_standing.pl [SURNAME ...]   (default: STEIN KENNEDY OLIVER; ALL for every column)
use strict;
use warnings;

my $usa = 'ElectionsData/usa';
die "run from the project root\n" unless -d $usa;
my @want = @ARGV ? @ARGV : qw(STEIN KENNEDY OLIVER);

sub rows {   # a generated CSV's data rows under its heading, the comment lines skipped
    my ($file, $heading) = @_;
    open my $h, '<', "$usa/$file" or die "$usa/$file: $!\n";
    my ($seen, @rows) = (0);
    while (my $l = <$h>) {
        $l =~ s/\r?\n$//;
        next if $l eq '' || $l =~ /^#/;
        if (!$seen) { die "$file: its heading '$l', not '$heading'\n" unless $l eq $heading; $seen = 1; next; }
        push @rows, [split /,/, $l, -1];
    }
    close $h;
    die "$file: no rows\n" unless @rows;
    return @rows;
}

my @states = map { $_->[1] } grep { $_->[0] == 2024 } rows('president_by_state.csv', 'year,state,electors,votes_d,votes_r,votes_other,votes_total,cast_d,cast_r,cast_other,cast_other_to');
die "president_by_state.csv: " . scalar(@states) . " jurisdictions of 2024, not 51\n" unless @states == 51;
my (%listed, %written, @order);
for my $r (rows('president_by_candidate_state.csv', 'year,candidate,state,votes,listed')) {
    my ($year, $candidate, $state, $votes, $listed) = @$r;
    next unless $year == 2024;
    die "president_by_candidate_state.csv $candidate $state: listed '$listed', neither ballot nor write-in\n" unless $listed eq 'ballot' || $listed eq 'write-in';
    push @order, $candidate unless exists $listed{$candidate};
    $listed{$candidate} //= {};
    $written{$candidate} //= {};
    if ($listed eq 'ballot') { $listed{$candidate}{$state} = $votes; } else { $written{$candidate}{$state} = $votes; }
}
@want = @order if @want == 1 && $want[0] eq 'ALL';
print "the ballot of record, 2024 (the FEC's workbook, a write-in in italics by its note; read from president_by_candidate_state.csv)\n";
for my $w (@want) {
    die "no column '$w' among 2024's other candidates\n" unless exists $listed{$w};
    my @on = grep { exists $listed{$w}{$_} } @states;
    my @in = grep { exists $written{$w}{$_} } @states;
    my @none = grep { !exists $listed{$w}{$_} && !exists $written{$w}{$_} } @states;
    printf "%-26s on the ballot in %2d of 51; written in %2d: %s; no vote in %2d: %s\n", $w, scalar(@on), scalar(@in), (join(' ', @in) || '-'), scalar(@none), (join(' ', @none) || '-');
}
