# Review - §790, PS-6 US-11 part one: the House by district, 2016-2024, and the maps in force (2026-10-06)

A workflow review (`polisim-staged-review`) of the change that builds US-11's data half: the new raw group `raw/maps` (the Census Bureau's Redistricting Data Program tabs, its geography page, the 2020 and 2024 instruments that serve); `Tools/us_house_prep.pl`, which reads every House race of 2016-2024 from the Clerk of the House's statistics, holds each to its state's recapitulation, and checks the winners against the House Historian's divisions, the districts against US-5's state table and the FEC's sheets; the catalog it writes (`house_districts.csv`, `house_maps.csv`, `house_years.csv`, `UsHouseDistricts.cs` - the other part of the partial class `UsPresidentialReturns`); its mutation suite; `GeneratedCatalogCheck.CheckUsHouseDistricts`; `us_returns_prep.pl`'s partial class and corrected House texts; the record `house_districts.md`; `president_returns.md` reading 13 and the plan's corrections. Not required by the tier (`Tools/bar_tier.ps1`: SIMULATION, no money path, the sentinel unmoved); run because the catalog is the input R-US18 is measured on. One pass - one defect survived it; every finding was put to a refute-first skeptic, who re-graded it; every report below is verbatim.

## The first pass - confirmed (verbatim)

One pass over the staged tree, 46 agents: four lenses, every finding put to a refute-first skeptic.

### 1. 'GOP' is not read as Republican: 2018 WA-8's Republican finalist (Dino Rossi, 148,968 votes) is filed as other, and every check passes

- **Lens:** reader - **reviewer:** defect - **skeptic:** defect
- **Where:** Tools/us_house_prep.pl:263

**The scenario.** The Clerk prints 'Dino Rossi, GOP' (clerk_statistics2018.pdf -raw line 4045; Washington's recap header reads 'Democratic Republican GOP Libertarian Total'). party_of() only matches 'Republican' or 'Republican/Tax Revolt', so Rossi gets party O and own_party O (line 480). The staged house_districts.csv row is `2018,WA,8,0,164089,148968,313057,0,164089,0,1,D,-`: votes_r 0, cands_r 0, and his votes sit in votes_other. In the two-party data a top-two general between a Democrat and a Republican reads as an uncontested Democratic seat. Nothing catches it. The winner is the Democrat either way, so [HH-DIV] (line 546) and the FEC check (line 626) see no change. The house_by_state check (line 562) agrees because the Clerk's national recap puts the GOP column under its other column (WA 2018 R 899,744), and US-5 counts each party's own ballot line. Neither house_districts.md nor president_returns.md mentions GOP or Rossi. It is the only GOP label in the five volumes.

**The fix proposed.** Count 'GOP' as R when deciding the candidate's party (votes_r, cands_r, winner), but keep own_r on the Clerk's own Republican column, which is US-5's basis. That needs a separate predicate for own_party; otherwise own_r gains 148,968 and the WA 2018 house_by_state check fails. Alternatively, declare the reading in house_districts.md reading 3 and name the race.

**The skeptic's evidence.** I could not refute it. Each step of the scenario checks out against the source and the code, and running the tool on a scratch copy proves it.

THE SOURCE
- clerk_statistics2018.pdf, read with `pdftotext -raw`:
  - line 4045: `Dino Rossi, GOP ....`
  - line 4057 (Washington's recap header): `Title of candidate Democratic Republican GOP Libertarian Total`
  - the 8th district's recap row: `164,089 .... 148,968 .... 313,057`, so Rossi's votes sit in the GOP column.
- In the Clerk's national House recapitulation, Washington's row puts the 148,968 under "Other Parties1", whose footnote says "See each State's recapitulation table to determine the parties included in this category". That is why house_by_state.csv has WA 2018 R as 899,744.
- Grepping all five volumes finds this as the only GOP label.
- The FEC's 2018 sheet (row 4473) also prints Rossi's PARTY as "GOP". Its W is on Schrier (D).

THE CODE PATH (Tools/us_house_prep.pl)
- Line 263: `return 'R' if $l =~ /^(?:Republican|Republican\/Tax Revolt)$/;`, so "GOP" falls through to 'O'.
- Line 478: Rossi's party is 'O'.
- Line 480: his own_party is 'O'.
- Line 511: the Republican candidate count is 0.
- Line 679: `$v{O} += 148968`.

THE RUN
- `perl Tools/us_house_prep.pl` on a scratch copy exits 0.
- All four outputs are byte-identical to the staged ones, including `2018,WA,8,0,164089,148968,313057,0,164089,0,1,D,-`.

WHY NOTHING CATCHES IT
- Line 546 ([HH-DIV]) counts winners only, and the winner is D either way.
- Line 562 sums own-line R to 899,744, which is the Clerk's own Republican column, so it agrees.
- Line 626 (FEC) compares winners only.
- GeneratedCatalogCheck's `r.OwnR > r.VotesR` test sees 0 > 0, which is false, so it passes.
- The mutation suite has no case for this.

NOT DECLARED ANYWHERE
- house_districts.md reading 3 lists the variants it counts: DFL, Democratic-NPL and Republican/Tax Revolt.
- GOP, Rossi and WA-8 appear in no record, plan or check.
- The CSV header defines cands_r as "how many of each stood", which is false for this race.

THE PROPOSED FIX, TRIED ON THE COPY
- With GOP added to party_of and own_party kept 'O' for GOP, every check passes.
- house_maps.csv and house_years.csv are unchanged.
- Only one row changes, to `2018,WA,8,148968,164089,0,313057,0,164089,1,1,D,-`.
- Adding GOP to party_of alone stops the run with `2018 WA: the districts' own-line R 1048712, D 1888593, total 3021951; house_by_state.csv R 899744`. So a separate own-line test is needed, as the finding says.

SEVERITY
The finding's grade stands, but the defect is narrow:
- It touches one race out of 2,175.
- No winner, [HH-DIV] count or US-5 sum changes.
- Nothing reads HouseDistricts yet except GeneratedCatalogCheck.

It is still a defect because this catalog is the input to part two, and the error can change the measurement that decides R-US18:
- Under method (a), 2018 to 2020, the true WA-8 2018 Democratic two-party share is 0.5242. The national swing is about -2.8 pp (0.5437 to 0.5159 with Rossi counted). That gives about 0.496, which calls the seat R; the Democrat actually won in 2020 with 0.5178.
- As filed, no Republican stood, so the seat reads as an uncontested D seat and is called D.
- So method (a) would look one seat better than it is.

**The skeptic's corrected fix.** Treat 'GOP' as Republican when deciding a candidate's party, but keep the own-line columns on the Clerk's own Republican column, which is US-5's basis. Both edits below were tested on a scratch copy.

1. Line 263 becomes:
   `return 'R' if $l =~ /^(?:Republican|Republican\/Tax Revolt|GOP)$/;`

2. Line 480 becomes:
   `$c->{own_party} = @{$c->{parties}} == 1 ? ($c->{parties}[0] eq 'GOP' ? 'O' : party_of($c->{parties}[0])) : '';`
   The comment there should say: Washington's "GOP" is its own column in the Clerk's state recap, and the national recap counts it under "Other Parties".

3. Re-run the tool. The only change should be the 2018 WA-8 row, now `2018,WA,8,148968,164089,0,313057,0,164089,1,1,D,-`, plus the HouseDistrictSourceDigest in UsHouseDistricts.cs.

4. Declare the reading in house_districts.md reading 3, in the same form as the Vermont 2024 sentence. It should name the race (2018 Washington 8th, the label "GOP"), say the Clerk's national recap counts that column under Other Parties, and say that own_r is therefore 0 there, as house_by_state.csv has it. The CSV header's party sentence should say the same.

5. Recommended: add a guard that stops the run on any candidate label containing GOP, G.O.P., Republic or Democ that party_of leaves as 'O', unless the label is declared. Add one mutation case for it in Tools/us_house_mutations.sh. The guard will also fire on the 64 'Write-in (Republican)' and 'Write-in (Democratic)' labels in the five volumes, which are filed as other with no declaration either. Each of those needs its own declared reading.

If Elias rules that the literal label decides, the fix is the declaration alone: name 2018 WA-8 in reading 3, say that "GOP" is read as other and that cands_r is 0 there, and note that this matters for part two's handling of uncontested seats.

### 2. Within-district figure order is held by no check: transpositions pass, and two in 2024 flip NY-1 and NY-19 while [HH-DIV] still matches

- **Lens:** reader - **reviewer:** minor - **skeptic:** note
- **Where:** Tools/us_house_prep.pl:348

**The scenario.** Each bare figure goes to the first entry still waiting for one (lines 347-351). After that a race is held only to its row's Total (459/474), the state's own single-label R/D lines (562), the yearly [HH-DIV] totals (546) and, for 2016-2022 only, the FEC winner's party (626). The recap row's per-column cells are summed (line 400) but never matched to entries. So any transposition that leaves the single-label R/D own lines alone passes. Demonstrated on a copy using the suite's own wrap(). First, swapping 2024 -raw lines 2869/2872 (NY-1 Conservative 25,483 with Blank 19,984) gives exit 0, all four outputs written, and NY-1 votes_r 220,786 instead of 226,285. Second, swapping lines 2869/2871 (NY-1 Conservative 25,483 with Common Sense Suffolk 1,893) together with 3087/3088 (NY-19 Working Families 22,598 with Blank 11,862) gives exit 0 and 'House 2024: ... R 220 D 215 - [HH-DIV] R 220 D 215', but the written rows are `2024,NY,1,202695,207130,...,D,F` and `2024,NY,19,184290,181911,...,R,F`. Both winners are wrong and they cancel. Reading 1's DECLARED premise ('then their figures in the same order') is therefore checked by nothing. On the pinned pages the order is right: an independent pairing from `pdftotext -table`, which puts each label and its figure on one line, agrees with the tool's assignment in all 2,175 races.

**The fix proposed.** Hold each district's (label, figure) pairs to a second reading. Either require agreement with the -table text entry by entry (it agrees on all 2,175 races today), or map the recap header's columns and hold each line's figure to its column. Add a transposition case (for example the 2024 NY-1/NY-19 pair) to Tools/us_house_mutations.sh.

**The skeptic's evidence.** The gap is real, but no output row is wrong today. I'm downgrading it from minor to note.

THE CODE. Tools/us_house_prep.pl:348 gives each bare figure to the first entry still waiting for one: `my ($e) = grep { !defined $_->{fig} && !$_->{unopposed} } @{$cur->{ent}};`. On the clean run, 6,990 of the 7,022 figures are assigned this way; only 32 sit inline on their own entry's line (358). The recap row's cells are only summed (396-401: `$r->{sum} += num($_)`) and never matched to entries. After that a race is held only by aggregate checks:
- the cells against the Total (459) and the listing against the Total (474);
- [HH-DIV] winner counts (546);
- the state's own single-label R/D lines and total (562);
- the FEC winner's party, 2016-2022 only (626).
A few single races have extra checks (521 ME-2 2024, 635 AK 2022, 492 runoffs), so the finding's "only" is slightly too broad, but it doesn't matter here. GeneratedCatalogCheck.CheckUsHouseDistricts re-holds the same aggregates from the CSV.

REPRODUCED. I ran it on a scratch copy, with the 2024 -raw text swapped through a wrapper:
- Swapping lines 2869 and 2872 gives exit 0, all four outputs written, and `2024,NY,1,220786,183540,...,R,F` (the repo has 226285).
- Swapping 2869/2871 and 3087/3088 gives exit 0 and "House 2024: ... R 220 D 215 - [HH-DIV] R 220 D 215", with `2024,NY,1,202695,207130,...,D,F` and `2024,NY,19,184290,181911,...,R,F`. Both winners are wrong and the errors cancel.
The unmodified copy reproduces the repository's bytes.

WHY NOTE, NOT MINOR:
1. Today's pairing is right. I dumped the tool's entry-by-entry assignment (a copy of the tool, clean run) and paired it independently against `pdftotext -table`, which puts each label and its figure on one line by position. The result: 2,174 printed districts and 7,033 entries agree, 0 differ (NC-9 2018 prints no votes). The same check flags exactly the mutated NY-1 and NY-19 entries, so it does detect a swap.
2. The trigger can't arise from the inputs. The PDFs are pinned by SHA-256, and -raw follows the PDF's own content order, so a swapped extraction would need different pages, or a new volume such as the 2026 statistics.
3. If the extractor changes, the mutation suite's control flags the byte difference. Any single winner flip trips [HH-DIV] or the FEC check, and any swap that touches a single-label R/D line trips line 562.
4. Nothing over-claims. The tool header, the record's "What the tool proves", the plan text and GeneratedCatalogCheck never claim the pairing is checked. Reading 1 is labelled DECLARED, and its "held to its row's Total" sentence names only a figure misread, an entry dropped, or a page number taken for a figure.

What's left: a declared premise that is cheap to check isn't checked. That matters most when the tool reads a new volume, where a swap that changes only fusion-line, minor-candidate or non-candidate figures would pass silently.

**The skeptic's corrected fix.** Hardening, not a blocker for s790; worth doing before the tool reads a new volume.

1. Add a second, position-based reading of each volume. After the -raw reading, run `pdftotext -table` on each Clerk PDF. Parse the House sections the same way:
   - state headings, FOR sections, `N.` district starts and AT LARGE;
   - collapse the white space before matching "Recapitulation of Votes Cast in", because -table spreads that heading out;
   - collapse the white space inside labels (2020 IL-4 prints double spaces).
   Then require every entry's (label, figure, or "(n)" for unopposed) to equal the -table line's, the nth occurrence of a label against the nth. Any disagreement is a problem().
2. Add a mutation case to Tools/us_house_mutations.sh: a wrap() that swaps 2024 NY-1's Conservative and Common Sense Suffolk figures (25,483 and 1,893) and NY-19's Working Families and Blank figures (22,598 and 11,862). It must print both districts' disagreements.
3. Change reading 1's "held to its row's Total" sentence to name what the second reading now holds. Holding each line's figure to its recap column would also work, but the multi-line recap headers and party columns that add same-party candidates together make that harder than the -table pass.

### 3. In the fusion states, any unlisted bare label is silently added to the candidate above; elsewhere the same label stops the run

- **Lens:** reader - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_house_prep.pl:362

**The scenario.** In NY, CT and SC 2016, a single-part entry that is not in %noncand automatically becomes a ballot line of the nearest candidate above (lines 362-369). %fusion_label is consulted only for comma entries. Outside those states the same entry is a problem (line 373). Demonstrated on a copy: renaming 2024 NY-1's 'Blank' (-raw line 2865, 19,984) to 'Blank Ballots' gives exit 0, [HH-DIV] still matches, and the row becomes `2024,NY,1,226285,203524,...`: the Democrat's votes_d rises from 183,540 to 203,524. The Total, the own-line sums and the winner do not move, so nothing fires. In a close race (2022 CT-5, or 2024 NY-19) the same absorption could flip the winner, and in 2024 only [HH-DIV]'s yearly total would see it. On the pinned pages all 324 lines read as fusion lines are real party lines. Conservative lines attach only to Republicans and Working Families lines only to Democrats in all five years, and 2016-2022 fusion totals match the FEC's combined per-candidate totals where the two sources agree.

**The fix proposed.** In a fusion state, accept a bare label as a party line only if it is in a declared list. Extend %fusion_label to the 21 labels these volumes actually print (Blue Lives Matter, Save Our City, Medical Freedom, Moderate, Constitution, Common Sense Suffolk, and so on) and make any other bare label a problem, as outside the fusion states. Add a mutation case for it.

**The skeptic's evidence.** THE PATH IS REAL. Tools/us_house_prep.pl:361 `if ($noncand{lc $label}) { $e->{kind} = 'non'; }`, then :362 `elsif (fusion_state($year, $state) && (@parts == 1 || $fusion_label{$parts[0]}))`. In NY, CT and SC 2016, any bare label that is not in %noncand becomes kind 'line' of the nearest candidate above (:366-368). %fusion_label (:273) is read only for a comma entry. Outside those states the same bare label reaches :373 and is a problem.

NOTHING DOWNSTREAM SEES IT:
- The closing check adds every figure whatever its kind (:450-452, `$sum += $e->{v}`), so moving a line from noncand to a candidate leaves the sum equal to the Total.
- The house_by_state check (:538-544) holds only `own` and the Total.
- The FEC check holds only the winner's party, 2016-2022.
- [HH-DIV] holds only yearly seat counts.
- GeneratedCatalogCheck.CheckUsHouseDistricts holds `VotesR+VotesD+VotesOther <= VotesTotal` and own <= party votes, and both survive the absorption.

REPRODUCED on a temp copy, where the copy's pdftotext is wrapped to rename 2024 line 2865 `Blank` to `Blank Ballots`. The control matches the repo byte for byte. The mutated run exits 0, still reports `House 2024: R 220 D 215 - [HH-DIV] R 220 D 215`, and changes one row: `2024,NY,1,226285,183540,0,430460,200802,181647,1,1,R,F` becomes `...,226285,203524,...`. Avlon's 181,647 + 1,893 (Common Sense Suffolk) absorbs Blank's 19,984. The Total, own_d and the winner stay as they were.

TODAY'S OUTPUT IS CORRECT, and the trigger is realistic:
- Instrumented on the pinned pages, 329 entries are read as lines: 324 in NY and CT plus 5 in SC 2016 (the finding's 324 leaves SC out). They carry 21 labels, all party lines. Conservative attaches only to Republicans (98) and Working Families only to Democrats (104).
- The non-candidate lines in the fusion states are Scattering, Void, Blank Votes, Blank and Write-in, all in %noncand.
- The Clerk's NY volumes already changed spelling, from 'Blank Votes' (2016-2020, 81 lines) to 'Blank' (2022-2024, 52 lines). A new spelling in a later or re-fetched volume is therefore a plausible event, and in NY or CT it would be absorbed silently, while any other state stops.

WHY MINOR, NOT A DEFECT: the inputs are pinned by digest and readings 3-4 of the record declare the design. The finding's flip scenario (2022 CT-5, 2024 NY-19) is overstated:
- Non-candidate lines print after every candidate, so they attach to the candidate listed last.
- CT prints no non-candidate lines at all in these five volumes.
- A flip would still change [HH-DIV]'s count, and the FEC check for 2016-2022, unless another flip offset it.

The real harm is a silent shift of votes_r and votes_d, which are part two's inputs, with no change of winner.

**The skeptic's corrected fix.** Tested on a temp copy.

1. Line 362: change the test to `elsif (fusion_state($year, $state) && $fusion_label{$parts[0]})`. For a bare label, parts[0] is the label itself, so this covers both bare and comma entries.

2. Add a branch before the existing `elsif (@parts == 1)`:
`elsif (fusion_state($year, $state) && @parts == 1) { problem("$file $state-$cur->{n} line $ln: the entry '$label' - no name, in a fusion state, no party line declared"); $e->{kind} = 'non'; }`

3. Extend %fusion_label with the 9 bare labels these volumes print that it lacks: 'Save America Movement', 'Stop Iran Deal', 'Save Our City', 'Parent', 'Moderate', 'Medical Freedom', 'Constitution', 'Common Sense Suffolk', 'Blue Lives Matter'. The other 12 printed labels are already in the list, and 'Conservative, Common Sense' passes on its first part.

With the fix, the control exits 0 and all four outputs (house_districts.csv, house_maps.csv, house_years.csv, UsHouseDistricts.cs) are identical to the repo's. The Blank Ballots mutation then exits 255 with "returns/clerk_statistics2024.pdf NY-1 line 2865: the entry 'Blank Ballots' - no name, in a fusion state, no party line declared" and writes nothing.

Also:
- Add that as a case in Tools/us_house_mutations.sh: wrap 's{^Blank( \.+\r?)$}{Blank Ballots$1} if $. == 2865'.
- Add the condition to the header's die-list.
- In house_districts.md, extend the checks list (line 22) and reading 3: a bare line in a fusion state must be a declared party line.

### 4. Reading 3's 'Taking any one line as a candidate would elect the wrong one in six races' describes a different metric from the one the six come from

- **Lens:** reader - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/house_districts.md:52

**The scenario.** The six races (2018 NY-1/24/27, 2022 NY-4/17/22) are the tool's line_count_elsewhere test (line 506): the largest single ballot line belongs to a candidate other than the winner. Read literally, 'taking any one line as a candidate' means detaching one line, and that flips 13 races, as computed from the tool's own entries: 2016 NY-22; 2018 NY-1, NY-22, NY-24, NY-27; 2020 NY-22 (Tenney's Conservative 12,807 against a 109-vote margin); 2022 CT-5, NY-4, NY-17, NY-18, NY-19, NY-22; 2024 NY-19. Read that way, the sentence understates how many races depend on the fusion attachment that findings 2 and 3 show is unchecked.

**The fix proposed.** Reword to the metric's own terms ('in six races the largest single ballot line is another candidate's'), or state both counts.

**The skeptic's evidence.** The metric (Tools/us_house_prep.pl 504-506): `my ($lmax) = sort { ($b->{v} // 0) <=> ($a->{v} // 0) } grep { $_->{kind} eq 'cand' || $_->{kind} eq 'line' } @ent; $out->{line_count_elsewhere} = 1 if $lmax && ($lmax->{kind} eq 'cand' ? $lmax : $lmax->{of}) != $rank[0] && $out->{flags}{F};`. It finds the single largest ballot line and asks whether it belongs to someone other than the winner. That is the same as treating every line as its own candidate. In these races it also gives the same answer as counting each candidate by his own named line.

The prose (house_districts.md:52): "The tool adds each such line to the candidate above it. Taking any one line as a candidate would elect the wrong one in six races (2018 New York's 1st, 24th, 27th; 2022 New York's 4th, 17th, 22nd)." The words "any one line" read naturally as detaching one line while the others stay attached. That is a different test.

How I checked: I copied the inputs to the scratchpad and ran the tool with a few lines added, read-only for the repo. The run passed and printed the same six races. The added lines tested each fusion race (148 of them) two ways:
- Each candidate counted by his own named line: exactly the tool's six.
- One minor line detached from its candidate: exactly the 13 races the finding lists (2016 NY-22; 2018 NY-1, NY-22, NY-24, NY-27; 2020 NY-22; 2022 CT-5, NY-4, NY-17, NY-18, NY-19, NY-22; 2024 NY-19).

Example, from the Clerk's 2020 text (pdftotext lines 3280-3292): Tenney has Republican 143,291 + Conservative 12,807 = 156,098. Brindisi has Democratic 138,898 + Working Families 11,188 + Independence 5,903 = 155,989. Tenney's own line beats Brindisi's, so the tool does not flag the race. Detach her Conservative line alone and Brindisi wins. Another: 2022 CT-5, Hayes 123,818 + WF 4,020 against Logan 123,342 + Independent 2,492; detaching WF elects Logan.

Why only a note: the record's own pasted run line states the metric precisely ("a fusion race whose largest single ballot line is another candidate's (the line counted alone would elect him)"). The reading "each line its own candidate" is also open to a reader and gives exactly six. No data, code or check is affected; only the sentence's wording is.

**The skeptic's corrected fix.** Rewrite the reading 3 sentence in the tool's own terms, for example: "Counted line by line, with every ballot line standing as its own candidate, the largest line is another candidate's in six races (2018 New York's 1st, 24th, 27th; 2022 New York's 4th, 17th, 22nd: the run's line above)." Do not put the 13 into the prose by hand, because the tool does not print it and the repo's rule is that figures come from the run. If the record should also say how many races turn on a single minor line (2020 NY-22 is the sharpest case: Tenney's Conservative 12,807 against a 109-vote margin), add that check to the tool's printed report and cite its output line.

### 5. The IN-2 FEC exception accepts any disagreement, and the reason it states is never checked

- **Lens:** checks - **reviewer:** defect - **skeptic:** minor
- **Where:** Tools/us_house_prep.pl:625

**The scenario.** Lines 570/624-625 only refuse the 2022 IN-2 exception when the FEC's folded winner EQUALS the Clerk's ('declared an exception ... but the FEC's winner is the Clerk's'). Its stated reason, 'the FEC marks the winner on the unexpired-term row only', is never checked: line 609 drops every UNEXPIRED row before anything is recorded. PROBED on a scratch copy: in fec_federalelections2022.xlsx I set the full-term Democrat's row (cell W1399, Steury, D, 62,955) to W via xlsx_poke and re-pinned the digest. The fold gives 'D', the Clerk gives 'R', so the exception is satisfied: exit 0 and all four outputs written. A sheet with no IN-2 rows at all (fold 'none'), or with a W only on a minor party's row (fold 'O'), passes the same way. So for this district the FEC cross-check can fail only when the FEC agrees with the Clerk. x03 tests only that withdrawing the exception is caught; the reverse refusal has no case. I read the saved sheet: today the full-term rows 1396-1403 carry no W and the unexpired-term R row 1405 does, so the stated reason is true now. It is just not enforced.

**The fix proposed.** Keep the unexpired-term W rows for each district instead of discarding them at line 609. For each declared exception, require the full-term fold to be 'none' and the unexpired-term fold to equal the Clerk's winner, and refuse anything else. Add a case to us_house_mutations.sh: a W on the full-term Democrat (W1399) must be refused.

**The skeptic's evidence.** The code facts hold, and I reproduced the failing path. I re-grade it from defect to minor.

CODE (Tools/us_house_prep.pl; the working tree equals the index):
- Line 570: `my %fec_exception = ('2022 IN-2' => 'the FEC marks the winner on the unexpired-term row only');`
- Line 609: `next if $cd =~ /UNEXPIRED/i;` drops every unexpired-term W row before %fw. No other structure keeps it: %alaska_fec reads only AK-00 and %nc9_fec only 2018 NC-9.
- Lines 623-625: `my $fwin = ...` then `if ($ex) { problem("... declared an exception ($ex), but the FEC's winner $fwin is the Clerk's") if $fwin eq $cw; next; }`. Only agreement is refused. 'D', 'O', 'none' and 'both R and D' all pass.
- NC-9 is held differently. Line 631 checks its stated reason (** on every general candidate, no W), and suite case x07 proves it. IN-2 is the only declared exception whose reason nothing checks.

SAVED SHEET (sheet8.xml of fec_federalelections2022.xlsx; column 23, letter W, is 'GE WINNER INDICATOR'):
- Rows 1396-1403 ('02-Full Term') carry no GE W.
- Row 1405 ('02-Unexpired Term', R, Yakym) carries `<c r="W1405" ...><v>44</v>` (44 = 'W').
- The catalog row is `2022,IN,2,...,R,-`.
- So the stated reason is true today but not enforced.

PROBES on a scratch copy built like the suite's fresh(). The control exits 0 and reproduces all four outputs byte for byte.
1. W1399 (the full-term Democrat Steury) set to W, digest re-pinned: exit 0, 4 files written. On the same copy with the exception withdrawn, the run prints "MISMATCH: 2022 IN-2: the FEC's winner D, the Clerk's R". So the poke reaches the fold and the exception swallows it.
2. W1405's W removed, so the FEC marks no winner in IN-2 and the stated reason is false: exit 0, 4 files.
3. W1405 removed and W1406 (the unexpired-term Democrat) set to W: exit 0, 4 files.

SUITE: x03 is the only IN-2 case, and it only withdraws the exception. No case prints "declared an exception", so even line 625's stale-exception refusal is unproven.

WHY MINOR, NOT A DEFECT:
- No output is wrong, and none of the outputs depends on the FEC sheet for IN-2.
- The failing path needs a digest-pinned input replaced.
- A Clerk misread that flips IN-2's party is still caught by the [HH-DIV] 2022 check (lines 535-547: R 222 / D 213). An offsetting flip elsewhere in 2022 would be caught by the FEC check on that other district.
- The tool's header (line 34, 'but for the declared exceptions') and house_districts.md line 25 describe the exemption accurately. Reading 10 calls IN-2 'declared'. Unlike NC-9's sentence ('the tool holds that'), it never claims the tool holds IN-2's reason.
- What is lost is real: for this one district-year the FEC cross-check checks nothing about the winner's party, the record's stated reason is unchecked, and the stale-exception refusal itself has no case.

**The skeptic's corrected fix.** 1. Do not discard unexpired-term W rows at line 609. Fold them into their own hash (e.g. %fwu, keyed "XX-n" by the same `^(\d{1,2})` district number, with the same R/D/O fold), kept out of %fw.
2. For each declared exception, require all of these and refuse anything else:
   - the full-term fold is exactly 'none' (no W on any full-term row);
   - the unexpired-term fold equals the Clerk's winner.
   That way the declared reason, 'on the unexpired-term row only', is what the tool holds. This also covers the current 'agrees with the Clerk' refusal: a full-term W of the Clerk's party is not 'none'.
3. Optionally tie the mark to the candidate: the W'd unexpired-term row's FEC ID (H2IN02295) must appear on a full-term row of the Clerk's winner's party. Alternatively, require the FEC full-term GENERAL VOTES plurality party to equal the Clerk's, which cross-checks the full-term race itself rather than a different race won by the same man.
4. Add cases to Tools/us_house_mutations.sh, each needing its exact message:
   - a W on W1399, the full-term Democrat (poke `<c r="W1399" s="107"/>` to `<c r="W1399" s="107" t="s"><v>44</v></c>`, then resum), must be refused;
   - W1405's W removed (`<c r="W1405" s="107" t="s"><v>44</v></c>` to `<c r="W1405" s="107"/>`) must be refused;
   - a W on the full-term Republican W1396 must be refused, which exercises the stale-exception path.
5. Reading 10 of house_districts.md can then say the tool holds the IN-2 reason, as it already says for NC-9.

### 6. Ranked-choice declarations are not checked for Alaska or 2018 ME-2, and plurality races can be declared ranked

- **Lens:** checks - **reviewer:** defect - **skeptic:** minor
- **Where:** Tools/us_house_prep.pl:525

**The scenario.** Two failure directions, both PROBED by editing %ranked (line 280) in a copy of the tool:
(1) Removing '2018 ME-2', '2022 AK-AL' and '2024 AK-AL' gives exit 0. The C and A flags silently drop from house_districts.csv (2018 ME-2, 2022 AK-0, 2024 AK-0).
(2) Adding '2016 ME-2' => 'C' and '2020 AK-AL' => 'A' (both plurality races) also gives exit 0, and the false C and A flags are written.
Cause: the 'A' branch (line 525) only sets the flag and checks nothing. The 'C' branch checks something specific only in 2018, 2022 and 2024; in any other year it requires only two R/D candidates. When a race is not declared, line 515 refuses only Continuing/Exhausted lines or a round footnote, and only 2022 and 2024 ME-2 print those.
The record's 'What the tool proves' (house_districts.md line 22) says 'the ranked-choice races are the declared ones (reading 6), each as its page shows it'. The tool already reads evidence it could check against:
- the 2022 FEC sheet's Alaska rows carry 1ST/2ND/3RD ROUND RCV VOTES (read into %alaska_fec at lines 594-601 and 636-638, never tied to %ranked);
- the 2018 FEC sheet's ME-02 rows carry FOOTNOTES 'Ranked-choice voting was used in the 2018 primary and general elections'.
The Alaska general-column check (lines 632-635) runs whether or not Alaska is declared. Related: %unnamed_ok (line 276) is never refused when no race uses it.

**The fix proposed.** In the FEC loop:
- require ($ranked{'2022 AK-AL'} // '') eq 'A' exactly when Alaska's RCV rounds are read;
- read the FOOTNOTES column, require 2018 ME-2 to be declared exactly when its rows name ranked-choice voting, and refuse any other district whose footnote does;
- refuse a 'C' declaration in a year without its own per-year rule, and an 'A' declaration outside AK.
In the record, say that the 2024 AK declaration is not checked against any saved page. Add cases (Alaska undeclared; a plurality race declared ranked). Refuse an %unnamed_ok entry that no race uses.

**The skeptic's evidence.** The code is as the finding says (Tools/us_house_prep.pl as staged):
- L280 `my %ranked = ('2018 ME-2' => 'C', '2022 ME-2' => 'C', '2024 ME-2' => 'C', '2022 AK-AL' => 'A', '2024 AK-AL' => 'A');`
- L515: an undeclared race (`!$rk`) is refused only if it has Continuing/Exhausted Ballots lines (`@tally`) or a round footnote (`$out->{flags}{C}`).
- L518: the 'C' branch always requires two R/D candidates. Beyond that it checks only `if ($year == 2022)` (the footnote), `2024` (Continuing Ballots equal to the finalists' sum) and `2018` (`@ent == 2`, L520-522). Nothing more is checked in 2016 or 2020.
- L525 `elsif ($rk eq 'A') { $out->{flags}{A} = 1; }` checks nothing, not even that the state is AK.
- L529-533 only checks that a declared race exists.
- L594-601 and L632-638 read Alaska 2022's FEC general column and its RCV rounds without any condition. %ranked is never consulted there, so the comment on L525 ("held to the FEC's general column below") holds the figures, not the declaration.
- GeneratedCatalogCheck.CheckUsHouseDistricts compares Flags only as CSV against the C# literal, which are written by the same run.

Probes, run on a scratch copy. The control exits 0 and reproduces the repository's house_districts.csv byte for byte.
- Dropping 2018 ME-2, 2022 AK-AL or 2024 AK-AL, one at a time or all three: exit 0. Only the flags column changes (2018 ME-2 C to -, 2022 AK-0 A to -, 2024 AK-0 A to -).
- Adding '2016 ME-2'=>'C' and '2020 AK-AL'=>'A': exit 0, and the false C and A flags are written.
- Also exit 0: '2020 ME-2'=>'C', and '2024 CA-1'=>'A' (A is not even confined to Alaska).
- Caught: '2018 ME-1'=>'C' ("4 entries printed, not the two finalists").
- An unused %unnamed_ok entry ('2018 VA-2'=>'Green') gives exit 0.

The record overstates what the tool proves. house_districts.md L16/L22 says the tool "dies ... unless ... the ranked-choice races are the declared ones (reading 6), each as its page shows it". The tool comment at L512 says "each held to what its page shows". Both are false for the A declarations and for withdrawal of 2018 ME-2.

The evidence the finding cites exists:
- 2018 FEC sheet: the ME-02 rows carry the footnote "...the general election for Congressional District 2 ... went into ranked-choice voting rounds ... For the general election, the final-round vote totals were: Jared Golden, 142,440 ... and Bruce Poliquin, 138,931". These equal the Clerk's 2018 ME-2 row exactly. The ME-01 rows carry no footnote.
- 2022 FEC sheet: nonzero RCV rounds appear only for AK-00 (rounds 1-3) and ME-02 (round 2: 165,136 and 146,142, the Clerk's 2022 ME-2 row). AK's footnote reads "Votes shown in General Column are the official 1st Choice General Vote Summary Results".
- The Clerk's 2022 and 2024 Alaska sections carry no note. So 2024 AK can only be DECLARED; no saved page can hold it.

Why minor and not defect:
- Every shipped figure is right today: all five declarations are correct and the run reproduces the repository's bytes.
- Winners come from candidate plurality, so a dropped or false flag never changes a winner, the [HH-DIV] tally or the house_by_state sums. The probes confirm the House lines are unchanged.
- All pages are pinned by digest, so the failing path needs an edit to the tool's own table.
- What fails is the proof and the record's one-line claim about it. The flags themselves matter later: they are US-11's "exceptions as data", and part two's method (a) is where Alaska 2022's A matters (its first-choice party sums favour R; D won).

The finding's fix is wrong in one part. "Refuse any other district whose footnote names ranked-choice voting" would wrongly refuse 2020 ME-2. Its 2020 FEC rows read "Ranked-choice voting was used in the 2020 primary and general elections", but only the Republican primary went to rounds, and the race is correctly undeclared.

**The skeptic's corrected fix.** 1. Tie the declarations to the FEC pages the tool already opens, for every district rather than only AK 2022.
   - 2022: a district with a nonzero 2nd-or-later RCV round must be declared. AK-AL must be 'A' (its general column is already held to the Clerk; also require the footnote saying that column is the 1st-choice results). ME-2 must be 'C', with the Clerk's R and D figures equal to the FEC's last round, parsed from the sheet. Refuse a 2022 declaration whose district has no such rounds.
   - 2018: parse the FOOTNOTES sentence on the general election going to rounds ("the general election for Congressional District N ... went into ranked-choice voting rounds" / "For the general election, the final-round vote totals were: <name>, <n> ... and <name>, <n>"). Require 'C' exactly where it appears, with the Clerk's two finalists equal to the parsed totals.
   - Do NOT key on the phrase "Ranked-choice voting was used". The 2020 ME-02 rows say it, and 2020 ME-2 was decided in round one.
2. In the Clerk loop, refuse 'A' outside AK and 'C' outside ME. Make the per-year rules exhaustive: refuse a C or A declaration in a year that has no hold of its own (2016, 2020).
3. 2024 AK-AL: keep it, but mark it DECLARED. No saved page can hold it (the Clerk prints no note, no FEC 2024 volume exists, and the state's rounds are BILLED). Reword the comments at L512/L525 and house_districts.md's "What the tool proves" bullet to say what is actually proven:
   - undeclared races print no ranked line or round footnote;
   - each declared Maine race is held to its page, and in 2018 and 2022 also to the FEC's final round;
   - Alaska 2022 is held to the FEC's rounds;
   - Alaska 2024 rests on no saved page.
4. Add mutation cases to Tools/us_house_mutations.sh:
   - 2022 AK-AL withdrawn;
   - 2018 ME-2 withdrawn;
   - a plurality race declared (2016 ME-2 'C', or 2020 AK-AL 'A');
   - 2020 ME-2 declared 'C' (the footnote-phrase trap).
5. Hygiene, with no effect on outputs: refuse a %unnamed_ok entry that no race uses, the way %ranked's existence check at L529-533 does.

### 7. The geography page's 117th sentence is checked for a hardcoded 'North Carolina', not compared with the 117th tab

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_house_prep.pl:243

**The scenario.** Lines 223-226 take 2020's changed state from the 117th tab. Line 243 only checks that the geography page contains the literal sentence 'North Carolina was the only state to make changes ... 117th'. Nothing compares the two pages. PROBED: I rewrote the 117th tab to 'Ohio was the only state that had changes to their congressional districts between the 116th and 117th Congresses.' and re-pinned it. Result: exit 0, and house_maps.csv writes 2020,OH,16,1 and 2020,NC,13,0, while the geography page the tool read still names North Carolina. No mutation case covers line 243's refusal.

**The fix proposed.** Capture the state named in line 243's regex. Require it to equal the single key of %{$changed{2020}} from the 117th tab, and refuse a disagreement. Add a case that changes either page.

**The skeptic's evidence.** I tried to refute this and could not. The working tree matches the staged index (`git show :Tools/us_house_prep.pl` has the same SHA-256 as the file on disk).

**Where 2020's set comes from (Tools/us_house_prep.pl 220-226).** Only the 117th tab sets it:
`if ($t =~ /\b([A-Z][a-z]+(?: [A-Z][a-z]+)*) was the only state that had changes ... between the 116th and 117th Congresses\./) { my @c = $names->($1, $file); $changed{2020}{$_} = 1 for @c; $map_page{2020} = $file; }`

**What line 243 checks.** It holds the geography page to a literal and never looks at `$changed{2020}`:
`problem("$file: no sentence saying North Carolina was the only state to change for the 117th Congress") unless $t =~ /North Carolina was the only state to make changes to their congressional districts for the 117 ?th congressional session\./;`

**Nothing else ties the two pages together.**
- A grep for `changed{2020}` finds only lines 225 and 226. Line 695 writes house_maps.csv straight from `%changed`.
- GeneratedCatalogCheck.CheckUsHouseDistricts compares the CSVs with the catalog row by row and checks seats per state. It holds no expected changed state.
- The NC session-law PDF is only checked against its digest (`page()`).

**Probe 1 reproduced, on a scratch copy of ElectionsData/usa plus the tool.**
- Control: exit 0, 2020 = NC.
- I rewrote the 117th tab to "Ohio was the only state that had changes..." and re-pinned it in raw/maps/SHA256SUMS.txt.
- Result: exit 0, "lines changed for 2020 (1 state, ...117th.html): OH". house_maps.csv got `2020,NC,13,0` and `2020,OH,16,1`.
- The geography page the tool had just checked still contains "North Carolina was the only state to make changes" (grep count 1). So the tool's output contradicts a page it read and accepted, and the cheap bar would pass too.

**Probe 2, the reverse.** Changing the geography page's sentence to Ohio and re-pinning is refused, with exit 255, nothing written and the line-243 message. That refusal comes from the literal, not from comparing the pages, and no mutation case covers it: m03 changes only the geography page's 2022 sentence, and m05 changes only the 117th tab's wording.

**Why minor.**
- No current output is wrong: both pages say North Carolina today.
- No written claim is broken: the tool header and the record's "What the tool proves" never say the two pages are compared. The register's "Use" column for the geography page ("North Carolina alone for the 117th") is satisfied by the literal.
- The failing path needs a re-fetched, re-pinned 117th tab naming another single state. That is the same kind of input the mutation suite exists to refuse. If it happened, the 2020 map flag that part two's seat methods will read would be silently wrong.

**The skeptic's corrected fix.** **1. Compare the two pages in the geography-page block.** In Tools/us_house_prep.pl, replace line 243 with a reading of the geography page's own named state, checked against the 117th tab. The 117th block runs first, so `%changed{2020}` is already filled.

    if ($t =~ /\b([A-Z][a-z]+(?: [A-Z][a-z]+)*) was the only state to make changes to their congressional districts for the 117 ?th congressional session\./) {
        my @g = $names->($1, $file);
        my $g = join(' ', sort @g);
        my $tab = join(' ', sort keys %{$changed{2020} // {}});
        problem("$file: the 117th Congress's one changed state $g, the 117th tab's " . ($tab eq '' ? 'none' : $tab)) unless $g eq $tab;
    } else { problem("$file: no sentence naming the only state that changed for the 117th Congress"); }

Keep `@g` in its own variable. Writing `sort $names->(...)` directly risks Perl parsing `$names` as the sort comparator.

**2. Update the tool header's die-list** to name "the geography page's 117th state not the 117th tab's".

**3. Add two cases to Tools/us_house_mutations.sh:**

    m07() { local f=$M/census_rdo_congressional_districts_117th.html; perl -i -pe 's/North Carolina was the only state that had changes/Ohio was the only state that had changes/' $f; resum $f; }
    m08() { local f=$M/census_geography_congressional_districts.html; perl -i -pe 's/North Carolina was the only state to make changes/Ohio was the only state to make changes/' $f; resum $f; }
    case_ "the 117th tab naming another state than the geography page" "the 117th Congress's one changed state NC, the 117th tab's OH" m07
    case_ "the geography page naming another state for the 117th" "the 117th Congress's one changed state OH, the 117th tab's NC" m08

**4. Update house_districts.md** to the new count of cases and the new Census-page cases, after re-running the suite. Its counts come from the run, not from this note.

### 8. The US-5 tie counts matched lines, not distinct state-years

- **Lens:** checks - **reviewer:** minor - **skeptic:** note
- **Where:** Tools/us_house_prep.pl:565

**The scenario.** Lines 552-565 increment $n for every house_by_state.csv line whose state-year exists in %race, then require $n == 250. A repeated row therefore counts twice. PROBED: I replaced the 2018,WY row with a second copy of 2018,WV (337146,234568,6277,577991). $n stays 250, exit 0, and Wyoming 2018's districts are never held to US-5. The record's 'all 250 state-years' (house_districts.md line 24) is not what the code checks. x02 (a row deleted) is caught; a row replaced by a duplicate is not.

**The fix proposed.** Keep a %seen keyed by "$y $st" and refuse a repeat, or require every state-year in %race (5 x 50) to be matched exactly once.

**The skeptic's evidence.** The finding is real, but I'm downgrading it from minor to note.

WHAT THE CODE DOES. In the staged Tools/us_house_prep.pl, lines 551-565 do this for each house_by_state.csv line: `next unless $race{$y} && $race{$y}{$st};` (556), compare the sums (562), then `$n++` (563). At the end, `problem("$f: $n state-years held, not 250") unless $n == 250;` (565). So $n counts matched lines, not distinct (year, state) keys.

REPRODUCED. I copied the tree to the scratchpad the same way the suite's fresh() does. The working tree equals the index for every input.
- Control run: exit 0, and all four outputs match the repository byte for byte.
- Probe run: I replaced `2018,WY,127963,59903,17409,205275` with a second `2018,WV,337146,234568,6277,577991`. Exit 0, all four outputs written, and Wyoming 2018 is never compared.
- x02 (the row deleted) is still caught, as "249 state-years held, not 250".

Only one kind of edit gets through: an exact copy of another state-year's correct row put in place of a deleted row.
- A duplicate with different figures fails the comparison at line 562.
- A row with an unknown year or state is skipped at line 556, so the count drops and line 565 catches it.

WHY THIS IS A NOTE, NOT MINOR.
1. The real input is correct. house_by_state.csv has 250 data rows, 50 per year and no duplicate (year, state) pair, so the 2026-10-06 run did hold all 250 state-years. house_districts.md line 24 is true for that run. It only overstates what a passing run guarantees on a corrupted input.
2. The generator cannot write the probe's input. us_returns_prep.pl line 949 is `for my $year (@house_years) { for my $s (@house_states) { ... } }`: 5 fixed years times 50 distinct codes (sorted hash keys minus DC, lines 279 and 801). Line 844 reports any state without a row. So the only way in is a hand edit or a bad merge of a generated file.
3. The cheap bar rejects that hand edit in GeneratedCatalogCheck.cs:
   - line 579/801: the HouseSourceDigest check ("house_by_state.csv changed since generation");
   - line 591: the row-for-row comparison against the catalog ("House row ... (2018 WY) differs");
   - lines 598-613: each year must hold the 50 states once ("twice", "not the 50").
   - CheckUsHouseDistricts (lines 765-770) makes the same districts-against-US-5 comparison again, against the generated catalog's rows rather than the CSV.
4. house_by_state.csv is only a cross-check input. Under the probe, the tool's printed run and all four outputs are identical to the control.

The only real effect: the Perl tool's own proof, and the record's "all 250 state-years" as a statement about any passing run, are one step weaker than the code enforces, and the 28-case suite has no case for it. It's worth tightening because the fix is cheap, but it is not a defect.

**The skeptic's corrected fix.** In Tools/us_house_prep.pl (block at lines 548-566), count distinct keys and refuse a repeat. Keep x02's expected message unchanged:

    my %held;
    while (my $l = <$h>) {
        ...
        next unless $race{$y} && $race{$y}{$st};
        if ($held{"$y $st"}++) { problem("$f: $y $st twice"); next; }
        ... (the comparison as now)
    }
    my $n = keys %held;
    problem("$f: $n state-years held, not 250") unless $n == 250;

The tool's own Clerk checks already ensure %race holds each of the 50 states once per year. So 250 distinct matched keys means every state-year was held.

Add a 29th case to Tools/us_house_mutations.sh:

    x08() { sed -i 's/^2018,WY,.*/2018,WV,337146,234568,6277,577991/' ElectionsData/usa/house_by_state.csv; }
    case_ "a state-year replaced by another's row in US-5's table" "house_by_state.csv: 2018 WV twice@@249 state-years held, not 250" x08

Then update the record's case list and count to match. Following the claim convention, take the count from the suite's own output; don't type it in.

### 9. Case c04 also mutates 2022 NY-3, though its label and the record name only NY-4

- **Lens:** checks - **reviewer:** minor - **skeptic:** note
- **Where:** Tools/us_house_mutations.sh:88

**The scenario.** The window `$. > 2380 && $. < 2400` covers two 'Conservative ....' lines in 2022's pdftotext text (no other year has one there):
- line 2381, NY-3's Conservative line under Santos;
- line 2395, NY-4's under D'Esposito.
Both become a candidate named 'Pat Doe'. The case is caught for the right reason: the dumped output is exactly 'House 2022: ... R 221 D 214' plus '2022 NY-4: the FEC's winner R, the Clerk's D'. The NY-3 change only stays silent because Santos's Republican line alone (133,859) still beats Zimmerman's 125,404 (120,045 Democratic plus 5,359 Working Families). With closer figures the count would read R 220 D 215 and the case would be MISSED. The suite's header ('a mutation that lands anywhere else prints something else') does not cover a second landing that prints nothing.

**The fix proposed.** Narrow the condition to `$. == 2395`, or anchor on the preceding "\n4. Anthony P. D'Esposito" line the way c01 anchors on Barry Moore.

**The skeptic's evidence.** I couldn't refute the facts, but the failure the finding predicts can't happen, so I regraded it to a note.

What I confirmed:
- Tools/us_house_mutations.sh:88 is `c04() { wrap 's{^Conservative( \.+\r?)$}{Pat Doe, Conservative$1} if $. > 2380 && $. < 2400'; }`.
- I ran pdftotext -raw on all seven PDFs the tool feeds through pdftotext and applied the same predicate. Only clerk_statistics2022.pdf matches, on two lines:
  - line 2381 is NY-3's Conservative line (after "3. George A. D. Santos, Republican" at 2380);
  - line 2395 is NY-4's (after "4. Anthony P. D'Esposito, Republican" at 2394).
- The 2016, 2018, 2020 and 2024 texts and the two apportionment tables have no match in that window.
- In us_house_prep.pl:360-378, "Pat Doe, Conservative" splits into two parts. "Pat Doe" is not in %fusion_label, so the line becomes a candidate (kind 'cand'), and party_of('Conservative') gives 'O'.

I copied the tree to the scratchpad, rebuilt wrap() exactly, and added a debug print to the copied tool:
- NY-3 2022: winner R; R 133859 | O 11965 | D 125404.
- NY-4 2022: winner D; R 129353 | O 11269 | D 130871.
- The run exited 255, wrote nothing, and printed exactly two mismatches:
  - "House 2022: the districts give R 221 D 214 other 0, [HH-DIV] R 222 D 213 other 0"
  - "2022 NY-4: the FEC's winner R, the Clerk's D"

So the mutation does land twice, and the NY-3 landing is silent:
- Own-line R and D (lines 480 and 560) and the row Total don't change.
- The [HH-DIV] check (line 546) and the FEC check (line 626) compare only winners, and Santos still wins.

Meanwhile the case label "(2022 NY-4: the Conservative line a candidate)" and the record (house_districts.md:28, "a fusion line given a name - New York's 4th of 2022") each describe one landing. c05, the only other single-race window case, lands once (2020 line 4675). c09 lands in three years, but its label names no year.

Why it's only a note:
1. The second landing can't produce a false CAUGHT. Both required strings are specific to NY-4, and "R 221 D 214" needs exactly one net flip, so any regression that stops NY-4 being detected still reports MISSED.
2. The predicted MISSED needs different NY-3 figures. page() (lines 59-71) checks clerk_statistics2022.pdf against its SHA-256 before pdftotext reads it, so those figures can't change without the control breaking first. Even then the result would be a false alarm that shows, not a pass that hides a fault.
3. The header's promise still holds: a CAUGHT here still proves NY-4's named line was caught.

What remains is a test and a record sentence that each name one site while the mutation hits two.

**The skeptic's corrected fix.** Make the mutation land once by anchoring on the D'Esposito line the way c01 anchors on Barry Moore. Use `.` for the apostrophe because the program sits inside bash single quotes; c06 does the same with `Louisiana.s`:

c04() { wrap 'BEGIN { undef $/ } s{(\n4\. Anthony P\. D.Esposito, Republican [^\n]*\n)Conservative( \.+\r?\n)}{${1}Pat Doe, Conservative$2}'; }

I checked this version in a scratch copy:
- It changes one line in all seven PDFs (2022 line 2395, NY-4).
- The tool exits 255, writes nothing, and prints the same two expected mismatches, so the case's expected texts don't change.

`if $. == 2395` would also land once, but it keeps the line-number fragility.

The label and house_districts.md:28 then need no change.

### 10. Case c06 rewrites every 2016 Louisiana runoff footnote, not just LA-3's

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_house_mutations.sh:90

**The scenario.** The regex `^(\d)(This vote count is from Louisiana.s December 10, 2016, general) \(runoff\) election` matches footnotes 1, 2 and 3 of the 2016 volume (the Senate runoff, LA-3 and LA-4; text lines 1757/1760/1763). The run refuses both 'LA-3: footnote 2 ...' and 'LA-4: footnote 3 ...'. The expected text 'a kind this tool does not read' is not tied to LA-3, so the label '(2016 LA-3)' understates what the case mutates and checks.

**The fix proposed.** Anchor on `^2` and expect 'clerk_statistics2016.pdf LA-3: footnote 2 glued to a figure', or relabel the case to name both LA-3 and LA-4.

**The skeptic's evidence.** I could not refute the finding. I reproduced it by running the tool on a copy of the tree in the scratchpad. The repo was not written, and the working tree matches the index for every file involved.

1. The regex hits all three footnotes. Tools/us_house_mutations.sh:90 applies `s{^(\d)(This vote count is from Louisiana.s December 10, 2016, general) \(runoff\) election}{...(second)...}` through `perl -pe`, one line at a time. In `pdftotext -raw clerk_statistics2016.pdf`, lines 1757, 1760 and 1763 all match:
   - footnote 1 names Campbell and Kennedy (the Senate race);
   - footnote 2 names Angelle and Higgins (LA-3);
   - footnote 3 names Johnson and Jones (LA-4).
   The wrapped extractor printed "(second)" on all three lines.

2. Two House races reach the check. us_house_prep.pl:460-474 reads LA-3's 277,671 and 260,762 as mark 2, and LA-4's 387,370 and 346,579 as mark 3. Each is the only reading that closes its row (282,443 and 268,761). With "runoff" gone, :486 leaves @final empty, and the else branch at :496 fires for both races. The Senate footnote change has no effect: Senate figures are never read as House entries.

3. Running the case as staged: exit 255, nothing written, and two refusals:
   - `returns/clerk_statistics2016.pdf LA-3: footnote 2 glued to a figure - '...' - a kind this tool does not read`
   - `returns/clerk_statistics2016.pdf LA-4: footnote 3 glued to a figure - '...' - a kind this tool does not read`

4. Why this breaks the suite's contract. Line 99 expects only the generic text "a kind this tool does not read". case_ at line 37 checks it with a plain substring grep (`grep -qF -- "$w" <<<"$out"`), so either refusal satisfies it. The header at lines 4-5 promises "the exact mismatch the case means (a mutation that lands anywhere else prints something else and is MISSED, never CAUGHT)".
   - Test: I applied the same mutation to footnote 3 only (`^(3)`), leaving LA-3 untouched. The run printed one mismatch, for LA-4 only, with exit 255 and nothing written. Under case_'s criteria this reports "CAUGHT a runoff footnote that no longer says runoff (2016 LA-3)".
   - Limit: if line 496 broke for all races, neither line would print and the case would be MISSED. So the case still exercises the right code path.
   - What is wrong is the label. It names one footnote and LA-3, but the mutation rewrites three footnotes and two races. The expected text is also looser than the header's guarantee.

5. Severity is minor: this is a precision problem in a test and its label, not a fault in the generated data. c07 does not share it; its anchor `^1\. Jerry L\. Carl, Republican \.+$` matches only in 2022 (0 matches in 2016, 2018, 2020 and 2024).

**The skeptic's corrected fix.** Restrict the mutation to footnote 2 and expect the LA-3 refusal by name, as c01-c03 already do:

c06() { wrap 's{^(2)(This vote count is from Louisiana.s December 10, 2016, general) \(runoff\) election}{$1$2 (second) election}'; }
case_ "a runoff footnote that no longer says runoff (2016 LA-3)" "clerk_statistics2016.pdf LA-3: footnote 2 glued to a figure" c06

Checked by running both versions:
- `^(2)` version: one mismatch, for LA-3 only. Exit 255, nothing written, and the expected text is found, so the case is CAUGHT.
- LA-4-only (`^(3)`) run: that expected text is absent, so the case would be MISSED, as the header requires.

Alternative: keep `(\d)`, relabel the case "(2016 LA-3 and LA-4)", and expect both lines: "clerk_statistics2016.pdf LA-3: footnote 2 glued to a figure@@clerk_statistics2016.pdf LA-4: footnote 3 glued to a figure".

### 11. Several refusals the record lists as proved have no mutation case

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_house_mutations.sh:104

**The scenario.** The 28 cases leave these refusals in Tools/us_house_prep.pl without a case. Each is live on reading the code; the tie was PROBED (2022 AL-2 set to 97,737 each, listing and row, gives 'AL-2: a tie for first, Barry Moore and Phyllis Harvey-Hall').
- Winner rules: tie for first (501); a runoff or round footnote that does not mark two finalists (492).
- Footnote marks: two or more readings close the listing (474 - c01 only tests 0 readings); a figure no reading groups (464, the path 2022 ME-2's '1146,142' depends on).
- Vacancies: a race without a winner other than 2018 NC-9 (432); NY-22 2020's winner not R (543).
- FEC: the IN-2 exception's reverse (625); missing headings (587); Alaska's rounds not read (637); a winner in a district the Clerk does not list (630 - raised by c03 but never asserted).
- [HH-DIV]: a Congress's row missing (170) or unreadable (175); an undeclared footnote on another Congress (189).
- The 2022 geography page: count word (236), sentence gone (242), the 117th sentence (243).
- Ranked choice: a round footnote in an undeclared race (515, second clause); 518, 520, 521, 522.
- Listing structure: district twice (341); figure without entry (349); entry without figure (437); party line with no candidate (367); recapitulation rows against districts (416 - raised by c03, not asserted); the unopposed mark and row (443/444 - raised by c07, not asserted).
- A perl warning (47); a non-ASCII output (650).
If any of these were broken, the suite would still exit 0.

**The fix proposed.** Add cases at least for the refusals the record's 'What the tool proves' bullets name: tie, finalists, the 2+ readings branch, the ungrouped figure, both vacancies, the exception's reverse, the HH-DIV row and its undeclared footnote, the 2022 geography sentence, the round-footnote branch, a perl warning, and non-ASCII output.

**The skeptic's evidence.** I could not refute the gap. I demonstrated it on a scratch copy. The finding's framing needs three corrections, listed at the end.

1. What the suite asserts. `case_` (us_house_mutations.sh:31-40) counts a case as CAUGHT only when every string it lists is printed (`grep -qF`). The 28 cases assert the texts of these us_house_prep.pl lines:
   - 67 and 68 (the digest dies);
   - 155, 157, 176, 187, 204 (its "named twice" branch only), 215, 218, 227, 238, 250, 305, 373, 410, 414, 442, 459;
   - 474 (its 0-readings branch only), 496, 515 (its first clause only), 546, 562, 565, 626, 631, 635.

   That is 25 of the tool's 72 `problem()` call sites. No case expects "a tie for first", "not two finalists", "no row for the", "carries footnote", "NY-22's winner", "no heading for" or "rounds not read".

2. The failing path, run. On a scratch copy I commented out the refusals at prep lines 170, 189, 492, 501, 543 and 587, then ran the committed suite against that tool. It printed "PASSES the control ... --- 28 caught, 0 missed or broken" and exited 0.

3. Why it matters: two of these guards fall through to a silent skip.
   - Line 587 ends in `next`, which skips that year's FEC cross-check.
   - Line 539, `my $h = $hhdiv{$year} or next;`, would skip a year's [HH-DIV] check if line 170 regressed.
   - Probe: I poked fec_federalelections2018.xlsx `<c r="V1" s="278" t="s"><v>2018</v></c>` to `<v>5</v>` (the heading becomes "W") and re-summed it.
   - The committed tool printed "MISMATCH: returns/fec_federalelections2018.xlsx '2018 US House Results by State': no heading for w", exited 255 and wrote nothing.
   - With line 587's `problem()` removed, the tool exited 0 and wrote house_districts.csv, house_maps.csv and house_years.csv byte-identical to the repository's. 2018's winner check vanished silently, including the NC-9 hold at line 631.

4. Side messages printed but not asserted (I replayed both cases):
   - c03 prints line 416's "recapitulation rows 1,2,3,4,5,6,7, districts 1,3,4,5,6,7" and line 630's "a winner in AL-2, a district the Clerk does not list". It asserts only line 414.
   - c07 prints line 443's "the unopposed mark (1) names no footnote of AL" and line 444's "the row does not carry the unopposed mark". It asserts only line 442.

5. The standard. COMPLETED.md §788 says "twenty-eight new cases, every new refusal proved", and us_returns_mutations.sh:121 says "every refusal the readers added, proved". The s788 review graded the same class of finding "reviewer: note - skeptic: minor" and acted on it. house_districts.md:28 says this suite "proves the failure paths ... as us_returns_mutations.sh does for US-3 and US-5", and the suite's own header reads "FAILURE PATHS, PROVED".

Why minor and not a defect: no figure is wrong. Every refusal is present in the code and passes on the real data. The gap is regression coverage only.

Corrections to the finding:
- (a) The record does not falsely list these paths as mutation-proved. Its "What the tool proves" bullets (house_districts.md:14-26) describe the tool's checks, which all exist. Its mutation paragraph names exactly the 28 cases. The gap is against the s788 standard that the record invokes, not against a false sentence. Neither CLAUDE.md nor the plan's US-11 "Done when" requires per-refusal cases.
- (b) Line 650 (a non-ASCII output) cannot be reached by any input mutation. The outputs carry only numbers, state codes, flag letters and the tool's own literal paths. The sibling suite proves neither its perl-warning guard (us_returns_prep.pl:89) nor its ASCII guard (:919), so lines 47 and 650 sit outside the established standard.
- (c) The finding's list understates the gap. These are also unasserted: 147, 148, the "is no state" branch of 204, 331, 335, 338, 346, 359, 383, 396, 401, 424, 429, 430, 457, 500, 530, 532 and 610.

**The skeptic's corrected fix.** 1. Cheapest first: assert what the existing cases already print (all four strings verified printed).
   - c03: append `@@recapitulation rows 1,2,3,4,5,6,7, districts 1,3,4,5,6,7@@a winner in AL-2, a district the Clerk does not list`.
   - c07: append `@@the row does not carry the unopposed mark@@names no footnote of AL`.
2. Next, cases for the refusals whose fallthrough is a silent skip:
   - Line 587: poke ElectionsData/usa/raw/returns/fec_federalelections2018.xlsx `<c r="V1" s="278" t="s"><v>2018</v></c>` to `<c r="V1" s="278" t="s"><v>5</v></c>`, run resum, and expect "'2018 US House Results by State': no heading for w". This was probed: exit 255, nothing written, that text printed.
   - Line 170: sed the 118th row's label on history_house_party_divisions.html, resum, and expect "no row for the 118th Congress".
   - Line 637: poke one of the 2022 RCV round headings.
3. Then cases for the holds on the done-when's path: 189, 432, 543, 501 (the tie; the reviewer's 2022 AL-2 probe, listing and row), 492, 474 with two or more readings, and 464.
4. Then the listing structure (341, 349, 437, 367), the 2022 geography page (236, 242, 243), ranked choice (the second clause of 515, then 518, 520, 521, 522) and 625.
5. Either prove line 47 with an input case (a non-numeric [HH-DIV] cell) and line 650 with a TOOL case, or say in house_districts.md that those two are not proved.
6. Re-run `bash Tools/us_house_mutations.sh` on the staged tree. Require the control to pass and every case to be CAUGHT, then update the count and the list in house_districts.md:28.
7. If Elias instead rules that a selective suite is acceptable: say in house_districts.md:28 and in the suite's header that the 28 cases are a selection, and drop "as us_returns_mutations.sh does for US-3 and US-5", since after s788 that suite claims every new refusal is proved.

### 12. The control's comment describes a step that does nothing

- **Lens:** checks - **reviewer:** note - **skeptic:** minor
- **Where:** Tools/us_house_mutations.sh:42

**The scenario.** The comment says 'the copy's generated C# part is the repository's until the run rewrites it, so it is removed first'. But fresh() (lines 18-22) never copies Assets/, so the rm at line 44 is a no-op. The control is still sound: the .cs exists only if the run writes it. It compares against the working tree, not the index. Its C# comparison relies on core.autocrlf=true: the index holds LF, the tool writes CRLF, and the checkout is CRLF. That is the same convention as us_returns_mutations.sh.

**The fix proposed.** Correct the comment, or drop the rm. Optionally compare against `git show :<path>` (the C# part with CR stripped) so the control proves the index rather than whatever is in the working tree.

**The skeptic's evidence.** I could not refute it. The comment is false and line 44 does nothing.

1. fresh() at Tools/us_house_mutations.sh:18-22 runs `rm -rf "$T"; mkdir -p "$T/ElectionsData" "$T/Tools" "$T/Assets/Scripts/Elections/Generated"`, then `cp -r "$ROOT/ElectionsData/usa" ...`, removes the three house CSVs, and runs `cp "$ROOT/Tools/us_house_prep.pl" "$T/Tools/"`. It copies nothing into Assets/. ElectionsData/usa holds no file matching *UsHouseDistricts* and no Assets path (find returns 0).

2. I rebuilt fresh() word for word in a scratch directory. Afterwards the Generated directory had 0 entries and UsHouseDistricts.cs did not exist. So `rm -f "$T/Assets/Scripts/Elections/Generated/UsHouseDistricts.cs"` at :44 has nothing to remove. The premise in the :42 comment, "the copy's generated C# part is the repository's until the run rewrites it", is false.

3. I ran the control in a scratch copy without line 44: rc=0, and the four outputs' SHA-256 equal the working tree's, so it PASSES. Removing the rm changes nothing. The control is sound without it:
   - us_house_prep.pl:643 dies on @problems before write_all (:728-729). The C# part exists in the copy only if the run wrote it.
   - A run that fails exits non-zero and has a missing sha256sum line, so it reports BROKEN.

4. The CRLF claim is accurate:
   - us_house_prep.pl:727 does `$cs =~ s/\n/\r\n/g;` and :654 opens the file with `'>:raw'`.
   - `git ls-files --eol` shows `i/lf w/crlf attr/` for UsHouseDistricts.cs. core.autocrlf=true comes from C:/Program Files/Git/etc/gitconfig.
   - The CSVs are `usa/house_*.csv -text` (i/lf w/lf).
   - us_returns_prep.pl:999 writes CRLF the same way. The control in us_returns_mutations.sh compares UsPresidentialReturns.cs the same way and has no rm, so this comment and line look carried over by mistake.
   - On a checkout without autocrlf the control reports BROKEN, so that failure is loud, not a silent pass.

Severity: I raised it from note to minor because of the repo's own rule. The CLAUDE.md claim convention "governs every document AND every source comment" and outranks everything else, and it was ruled because of comments that were demonstrably false. This comment asserts a fact about the code, namely what fresh() copies, and that fact is false. No check catches it: CommentClaimCheck.cs:101 scans only `*.cs`. It has no runtime effect, which is why it is not a defect.

**The skeptic's corrected fix.** Delete line 44 and make the line-42 comment true, for example: `# --- the control (fresh() copies no C# part, so the one compared is the run's own)`.

If the author wants a guard, use a check that fails loudly instead of a silent rm: `[ -e "$T/Assets/Scripts/Elections/Generated/UsHouseDistricts.cs" ] && { echo "BROKEN  fresh() copied a C# part"; bad=$((bad+1)); }`. That also protects every case_, because case_'s `wrote` count would treat a copied .cs as written and mark every case MISSED.

I do not recommend the optional switch to `git show :<path>`. It would change the control from proving the repository's own working-tree bytes, the convention us_returns_mutations.sh shares, to proving the index, with the CRs stripped. The current dependency on autocrlf fails loudly and needs no fix.

### 13. The C# House tie never counts the rows it compared, so a missing year passes unnoticed

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:765

**The scenario.** CheckUsHouseDistricts compares the districts' own lines and totals only against those UsPresidentialReturns.House rows that exist for the year. If House carried no rows for an election, nothing would be compared, yet the success line would still say 'the districts sum to the House rows by state'. The perl tool requires 250; the C# check requires nothing.

**The fix proposed.** Count the House rows compared per election, require 50, and add a disagreement otherwise.

**The skeptic's evidence.** Upheld: the C# tie can compare nothing for an election and still pass. Nothing is wrong in the current tree.

**The C# check, G:/UNITY/Projects/PoliSim/Assets/Editor/GeneratedCatalogCheck.cs:**
- Lines 765-770: `foreach (var h in UsPresidentialReturns.House) { if (h.Year != y.Year) { continue; } ... }`. There is no counter. If no House row has that year, the loop adds nothing to `wrong`.
- Lines 783-786: the success line "the districts sum to the House rows by state" prints whenever `wrong` is empty.
- The same function guards emptiness elsewhere: line 689 ("so no page was compared"), line 775 ("so nothing was counted") and `ReadUsCsv` line 818 ("every comparison of it is vacuous"). This tie has no such guard.

**The presidential check does not cover it (lines 596-615, `CheckUsPresidentialReturns`):**
- It holds each House year present to the 50 states and forbids gaps between the first and last year.
- It never ties House's years to `HouseRecord`'s years. A House part regenerated with only 2018-2024 passes it.

**The tool's guard, G:/UNITY/Projects/PoliSim/Tools/us_house_prep.pl:**
- Line 556 skips House rows that have no districts (`next unless $race{$y} && $race{$y}{$st};`).
- Line 565 compares the matched count to a literal: `unless $n == 250`.
- The year lists are hard-coded separately: line 43 here (`@years = (2016..2024)`) and line 800 of `us_returns_prep.pl` (`@house_years`).

**What catches the scenario today:**
- Only 2016 or 2024 could drop out, because interior gaps are refused. Both would turn the cheap bar red through another check, `UsNationalVoteCheck`:
  - Its cases (lines 71-79, 214-215) read House for every year 2016-2024.
  - `SharesOf` (lines 517-519) throws "the catalog holds no House vote of {year}".
  - Its card stamp (line 340) includes `HouseSourceDigest`.
  - Both checks are in the cheap suite (`CheckSuite.cs` lines 275 and 277).
- In that case only `CheckUsHouseDistricts`'s own success line is wrong.

**The path nothing catches:**
- `house_maps.csv` already holds 50 rows for 2026.
- Suppose 2026 is added to the district tool's years before `us_returns_prep.pl` holds 2026 House rows:
  - The tool still counts 250 matched rows and passes.
  - `UsNationalVoteCheck` has no 2026 case and its stamp does not change.
  - The C# tie compares nothing for 2026 and prints the success line.

**The current tree:** recomputed from the CSVs, there are 250 House rows, every one with districts, 0 disagreeing, and 435 races in each year 2016-2024. Nothing is wrong now, so the grade stays a note.

**The skeptic's corrected fix.** In `CheckUsHouseDistricts`, count the House rows compared for each `HouseRecord` year and require all 50 states:

```csharp
int compared = 0;
foreach (var h in UsPresidentialReturns.House)
{
    if (h.Year != y.Year) { continue; }
    compared++;
    // ... the existing per-state comparison ...
}
if (compared != 50) { wrong.Add(F("{0}: {1} House row(s) by state compared with the districts, not 50", y.Year, compared)); }
```

Requiring 50 is enough. The presidential check already holds each House year present to the 50 states once, and a state with districts but no House row already fails as -1.

Alongside it, tie the tool's literal to its year list (line 565) so that adding a district year cannot pass the tool:

```perl
unless $n == 50 * @years
```

Optionally, name the year count in the success line.

### 14. UnwiredSubsystemCheck passes the House part only because it shares a type name; the partial split also stops the pair from ever being reported

- **Lens:** catalog - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/Generated/UsHouseDistricts.cs:10

**The scenario.** No game file reads HouseDistricts, HouseMaps, HouseRecord, HouseRawSources or the three digests; the only reader is Assets/Editor/GeneratedCatalogCheck.cs. I compiled the real UnwiredSubsystemCheck.cs, SourceText.cs and RatchetLedger.cs outside Unity (Debug and CheckExit stubbed). On the staged tree it measures UNWIRED 2/2 and UNREACHABLE 2 of 288 (ceiling 2), so it passes. HEAD measures 2/180 and 2/287, so nothing moved. The pass looks at type names only, and the House file is reached because it declares `UsPresidentialReturns`. That name appears in ElectoralCollege.cs, which is itself one of the two UNREACHABLE files, and in the twin file's own `partial class` line. (1) Same tree with the House part declared as its own class: '3 of 288 file(s) UNREACHABLE (ceiling 2)', FINISH 1. (2) Every code mention of the type removed from ElectoralCollege.cs: the HEAD one-part catalog reports UsPresidentialReturns.cs (3 > 2, fails); the staged two-part catalog reports neither file (2, passes). Each part's declaration counts as a mention of the other, so the pair can no longer be reported. This sidesteps the per-file rule that a new runtime file needs a runtime caller.

**The fix proposed.** Change the instrument and write the change down, which the check's own doc allows for an instrument fix. Judge a file that declares only part of a partial type by whether its own members are named in non-Editor code, not by the type name, and never count a sibling `partial` declaration of the same type as a mention. Then either give the House part a game-path reader or record it as waiting for part two. Do not raise the ceiling.

**The skeptic's evidence.** I could not refute it. I compiled the real UnwiredSubsystemCheck.cs, SourceText.cs and RatchetLedger.cs outside Unity, with Debug and CheckExit stubbed. I ran them on a HEAD tree (from git archive) and on the staged tree, both copied to the scratchpad. The working tree matches the index under Assets/Scripts and Assets/Editor, and there are no untracked files there.

How the check decides (UnwiredSubsystemCheck.cs:283-306):
- A file counts as reached if any public type it declares is named (CountWord) in any other non-harness file under Assets/Scripts.
- A sibling's `partial class` line counts as a naming.
- Neither generated part declares a `public static` method that the PublicStatic regex matches, because their fields are `static readonly (...)[]`. So the UNREACHABLE pass is the only one that ever looks at them.

The facts behind the finding:
- UsHouseDistricts.cs:10 and UsPresidentialReturns.cs:14 both read `public static partial class UsPresidentialReturns`.
- The only non-Editor code that names the type is ElectoralCollege.cs:63 and :67 (`UsPresidentialReturns.States` and `.Districts` in FromCatalog).
- HouseDistricts, HouseMaps, HouseRecord, HouseRawSources and the three House*SourceDigest names are read only in Assets/Editor/GeneratedCatalogCheck.cs:681-786.

Measured with the real check:
- HEAD: UNWIRED 2 of 180, UNREACHABLE 2 of 287 (ElectoralCollege.cs and Rosatellum.cs).
- Staged: UNWIRED 2 of 180, UNREACHABLE 2 of 288, the same two. The finding's "UNWIRED 2/2" is a typo for 2/180.
- Case (1), the House part as its own class: 3 of 288 UNREACHABLE, UsHouseDistricts.cs listed, FINISH 1.
- Case (2), every UsPresidentialReturns mention removed from ElectoralCollege.cs: HEAD gives 3 of 287 (UsPresidentialReturns.cs reported, FINISH 1); staged gives 2 of 288 (FINISH 0), with neither part reported.
- Caveat for case (2): the doc lines at ElectoralCollege.cs:13 and :52 hold `cref="..."`. SourceText keeps any line that contains a double quote, so those mentions must be removed too.

The rule this goes around:
- F1 item 1 (COMPLETED.md ~31907): "The generated catalog moves into the runtime assembly WHEN A RUNTIME CONSUMER EXISTS - not before."
- ElectionsDataCatalogGenerator.cs:45-50 and PopulationProjectionCatalogGenerator.cs:49-56 both record a catalog sent to the Editor assembly because this check caught it, and "a ceiling may not be raised to admit it".
- §788 says "no new runtime file, the unwired counts unmoved".
- §646 says "UnwiredSubsystemCheck admits no such file".
- s790 adds a new runtime file with no runtime reader (its consumer is US-16). It is admitted only because it shares the type name, and nothing staged records the wait.

Why minor and not a defect:
- Today's numbers do not move and nothing breaks.
- The partial blind spot already existed for GameController's 30 partial files. It costs nothing there because GameController is named elsewhere.
- US-5's `House` array already sits in this class with no runtime reader.
- The silenced tripwire in case (2) only fires after a future edit.

The proposed fix is inconsistent. Judging a partial file by its own members makes the measurement 3 today, so "record it as waiting" together with "do not raise the ceiling" still fails the bar. I tested a smaller instrument change: strip a sibling partial declaration before counting. It gives HEAD 2/287 and staged 2/288 (no GameController false positive), and case (2) staged 4/288 FINISH 1, with both parts reported.

**The skeptic's corrected fix.** 1. Fix the check. In the UNREACHABLE pass (UnwiredSubsystemCheck.cs:292), remove any `partial class`, `partial struct` or `partial interface` declaration of the same name from the other file before counting:
`CountWord(Regex.Replace(other.Value, @"\bpartial\s+(?:class|struct|interface)\s+" + Regex.Escape(name) + @"\b", " "), name)`
Write the change into the check's doc. Measured results:
- HEAD 2/287 and staged 2/288, so no ceiling moves and no GameController file is reported.
- With ElectoralCollege.cs no longer naming the catalog, staged reports both parts (4/288, FINISH 1) instead of neither.

2. Deal with the House part under F1's rule. Today it is read only by GeneratedCatalogCheck, and its runtime consumer will be US-16. Pick one:
- Emit it into the Editor assembly as its own class, which needs:
  - UsPresidentialReturns made non-partial again;
  - GeneratedCatalogCheck pointed at the new class;
  - us_house_prep.pl's output path and class name changed.
  Move it back into the runtime assembly in the commit that gives it a game-path reader.
- Or keep it where it is, and say in §790 and house_districts.md that:
  - it has no game-path reader and waits for US-16;
  - the check admits it only through the shared type name, which ElectoralCollege.cs supplies and which is itself unreachable.

Do not raise either ceiling. Do not adopt per-member judging plus "record it" plus "do not raise the ceiling" together: that measures 3 today and fails the bar.

### 15. The re-read never ties a race's Winner to its own votes; the vacant seat's winner can be anything

- **Lens:** catalog - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/GeneratedCatalogCheck.cs:758

**The scenario.** Line 758 subtracts whichever party the vacant race names, and line 759 only requires that the race exists. Mutation in a scratch copy: NY-22 2020 changed from Winner R to D in both the CSV and the catalog, digest updated, votes unchanged at R 156098 vs D 155989. The check gives RESULT 0 and still prints '2020 R 212 D 222 (NY-22 vacant)'. The generator asserts that NY-22's winner is R (Tools/us_house_prep.pl:541-544); the C# re-read does not. More generally, swapping two opposite-party winners within one year also passes, because only the totals are checked.

**The fix proposed.** For each race, assert Winner equals the party with the most votes across VotesR, VotesD and VotesOther, skipping U and N rows and treating flag-A races as named exceptions. On today's data this holds everywhere except 2022 AK-AL. Apply it to the vacant race as well.

**The skeptic's evidence.** I couldn't refute it. I traced the code, and the failing path is real.

1. Nothing ties Winner to the votes. In Assets/Editor/GeneratedCatalogCheck.cs, Winner is read on only six lines: 698 (SameRow against the CSV), 727, 743, 744, 758 and 759. Line 742 is the only per-race check on the votes, and it compares the parts with the total and the own lines with the party votes. It never compares Winner with VotesR, VotesD or VotesOther.
   L743 `if (r.Winner == "R") { winnersR++; } else if (r.Winner == "D") { winnersD++; } else if (r.Winner == "O") { winnersO++; } else { none++; unwon = name; }`
   L744 `if (name == y.Vacant) { vacantWinner = r.Winner; }`
   L758 `int r2 = winnersR - (vacantWinner == "R" ? 1 : 0), d2 = winnersD - (vacantWinner == "D" ? 1 : 0), o2 = winnersO - (vacantWinner == "O" ? 1 : 0);`
   L759 `bool vacancyRight = y.Vacant == "-" ? none == 0 : (vacantWinner != null && (none == 0 || (none == 1 && unwon == y.Vacant)));`

2. Trace of the mutation. Change 2020 NY-22 (CSV line 1165, catalog line 1200, votes R 156098 / D 155989) from R to D in both files and update HouseDistrictSourceDigest. ReadUsCsv (lines 800-801) only compares the SHA of the CSV with that constant, so it passes, and SameRow passes. The count gives winnersR 212 and winnersD 223. vacantWinner is "D", so r2 = 212 and d2 = 222, which equal the house_years row `2020,117,212,222,0,NY-22`. none is 0, so vacancyRight is true. The state sums only use OwnR, OwnD and VotesTotal (lines 764-768), which are unchanged. The check passes. Setting the winner to "O" or "-" passes too ("-" gives none 1 and unwon "NY-22"). Swapping two opposite-party winners in one year leaves every count unchanged, so it also passes.

3. The other vacancy has the same hole. Change 2018 NC-9 from "-" to R: winnersR is 200, vacantWinner is "R", r2 = 199, which matches [HH-DIV]. none is 0, so the check passes. The proposed fix skips N rows, so it leaves this hole open.

4. Nothing else catches it.
   - No other code reads HouseDistricts (grep of Assets).
   - No bar step re-runs the perl tool.
   - The generator does derive the winner from the votes (us_house_prep.pl:502 `$out->{winner} = $rank[0]{party}`) and asserts NY-22 is R (:540-544). It checks the FEC's winners for 2016-2022, but nothing checks the 2024 winners one by one.
   - The sibling check, CheckUsPresidentialReturns, does re-derive its result from the votes (the allocator, lines 637-645; its doc calls it "the one question a catalog read by an allocator owes").
   - CLAUDE.md FT-10 says "a digest certifies sameness, not sanity".
   - The repo treats coordinated hand edits as in scope for these invariants (line ~590: "a guard on a hand edit only").

5. Why minor and not a defect: the data is right today. A scan of house_districts.csv finds 2163 contested rows, and the winner has strictly the most votes in all of them except 2022 AK-0 (R 129379, D 128553, winner D, cands_r 2, flag A). 2024 AK-0 (flag A) agrees with its votes. All 11 U rows have zero votes and their winner is the party of their only candidate. 2018 NC-9 (flag N) has winner "-" and zero votes. There are no ties. The field has no reader until part two. Getting through requires a coordinated edit of the CSV, the catalog and the digest.

**The skeptic's corrected fix.** In CheckUsHouseDistricts, inside the per-race loop after line 743, check every race's stored Winner against the race's own figures. Run this on the vacant races as well. Then line 758 can no longer subtract whichever party the row happens to name.

(a) Flag N rows: Winner must be "-" and VotesR, VotesD and VotesOther must all be 0. A Winner of "-" without flag N is wrong.

(b) Flag U rows: the votes must all be 0, and Winner must be the party of the race's only candidate:
- R when CandsR == 1 and CandsD == 0;
- D when CandsD == 1 and CandsR == 0;
- anything else is wrong.

(c) Every other row: Winner must be the strict maximum of VotesR, VotesD and VotesOther. A tie is wrong. The one exception is a list of declared races keyed by name, which today holds only {"2022 AK-AL": Alaska's first choices, where the two Republicans together outpoll the Democrat who won (house_districts.md reading 6)}. Hold each listed exception to three things:
- its Winner still differs from the party with the most votes, so a stale exception fails;
- the party with the most votes fielded two or more candidates (CandsR or CandsD >= 2). That is the only way a plurality winner can be outpolled by a party's combined votes;
- it was found exactly once.

Do not exempt races by flag A, because 2024 AK-AL (flag A) agrees with its votes. Do not skip N and U rows; check them as in (a) and (b). Otherwise 2018 NC-9 can still be given to any party: a winner of R gives r2 199 and none 0, which passes.

On today's CSV this passes on every row. It pins 2020 NY-22 to R by its votes and 2018 NC-9 to "-" by its flag, and it catches a swap of two winners when the votes are left unchanged.

Optionally, mirror the presidential success line and print, per year, the division that the figures alone give, with the exception named. Keep the comments free of transcribed figures, per the claim convention.

### 16. cands_r / cands_d are 0/0 for NC-9 2018, though the header says 'how many of each stood'

- **Lens:** catalog - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/house_districts.csv:744

**The scenario.** Row 2018,NC,9 has cands 0/0 (flag N). The tool itself requires at least two general candidates marked '**' with votes in the FEC sheet for this race (us_house_prep.pl:604-606, 631), so candidates did stand. Anything counting races without a major-party candidate from cands (for example R-US18(a)'s rule for uncontested seats) would count NC-9 2018 as uncontested by both parties.

**The fix proposed.** Gloss the column as 'printed by the Clerk', or have the header say that N rows carry 0/0.

**The skeptic's evidence.** The mismatch is real, but only in the documentation. Nothing reads these columns yet.

How the 0/0 is produced:
- Tools/us_house_prep.pl:343 marks the Clerk's "1 9. See explanation below" as `unprinted`.
- The N branch (427-433) runs `next` before line 511 (`$out->{n} = {R=>..., D=>...}`), so `$out->{cands}` stays `[]` (from line 422).
- Lines 680-681 (`$nr = $x->{n} ? ... : scalar(grep { $_->{party} eq 'R' } @{$x->{cands}})`) then give 0 and 0.
- The Clerk's 2018 text (pdftotext -raw) prints no candidates at all: "1 9. See explanation below". The recap row is all dots, and the footnote reads "1On February 21, 2019, the North Carolina State Board of Elections ordered a new election in this Congressional District."

What the docs claim:
- house_districts.csv:7 "# cands_r / cands_d: how many of each stood." against :744 "2018,NC,9,0,0,0,0,0,0,0,0,-,N".
- The same words appear in UsHouseDistricts.cs:48 ("how many of each stood") and house_districts.md:7 ("how many of each party stood").
- The N gloss (prep.pl:661) says only "no votes printed". It does not say that no candidates are printed.

Candidates did stand, and the tool reads that:
- In fec_federalelections2018.xlsx, sheet '2018 US House Results by State', NC 09 has rows with general votes: row 2987 Harris R 139246 '**', row 2991 McCready D 138341 '**', row 2994 Scott LIB 5130 '**'.
- prep.pl:604-606 collects exactly these rows, and :631 requires at least 2 of them.
- house_districts.md:67 itself says "the FEC marks each of its general candidates '**'".
- So the truth is 1/1, and the catalog says 0/0. It is the only 0/0 row and the only N row (awk over the CSV).

Why the severity stays at note:
- CandsR/CandsD appear only in the generator, the CSV, UsHouseDistricts.cs and GeneratedCatalogCheck's SameRow row comparison. No logic reads them.
- R-US18's uncontested-seat rule is part two and is not in this commit, so the miscount scenario is only prospective.
- The row already carries winner '-' and flag N, with all votes 0, so any consumer must special-case it anyway.
- NC's lines changed for 2020 (it is the only state for the 117th), so method (a) for 2018->2020 would not swing NC's 2018 districts directly.

**The skeptic's corrected fix.** Fix the gloss, not the data. Keep the CSV "by the Clerk" rather than filling 1/1 from the FEC sheet.

In Tools/us_house_prep.pl:
- line 669: "# cands_r / cands_d: how many of each the Clerk lists (none where it prints no race - flag N)."
- line 661: extend N to "no winner of record - no candidates or votes printed, the election not certified".
- line 715 (the C# summary): replace "how many of each stood" with "how many of each the Clerk lists".

In ElectionsData/usa/house_districts.md:7, make the same wording change.

Then re-run the tool. The CSV digest and UsHouseDistricts.cs regenerate together. GeneratedCatalogCheck reads only the 'year,...' heading line, so a change to the comment lines is safe. Then re-run Tools/us_house_mutations.sh.

### 17. The C flag's gloss ('printed at its last round') does not hold for votes_other in 2022 ME-2

- **Lens:** catalog - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/house_districts.csv:9

**The scenario.** Row 2022,ME,2 (flags CM) has votes_other 21655, which is Bond's figure. The page's footnote mark 1 ('from round 2') sits only on Poliquin's and Golden's figures; Bond's is unmarked. The record's reading 6 calls it 'the eliminated Independent's first-round figure'. So the row mixes rounds, but the C gloss says only 'last round'. The L gloss spells out the equivalent mix ('beside the others' November votes').

**The fix proposed.** Gloss C as 'the finalists at the last round, beside any eliminated candidate's first-round figure'.

**The skeptic's evidence.** I tried to refute this and could not. The scenario happens exactly as the reviewer describes.

1. The gloss and the row.
   - house_districts.csv:9 reads `#   C  Maine's ranked-choice count printed at its last round`.
   - house_districts.csv:11 reads `#   L  Louisiana's runoff: the finalists' December votes beside the others' November votes`. This gloss states its mixed rounds; C does not.
   - house_districts.csv:1502 is `2022,ME,2,146142,165136,21655,656104,146142,165136,1,1,D,CM`, and UsHouseDistricts.cs:1537 holds the same tuple.
   - The C# doc for HouseDistricts defers to this header: "the flags (house_districts.csv's header says each)".

2. What the page prints. In `pdftotext -raw clerk_statistics2022.pdf`, ME-2 lists Poliquin, Golden, "Tiffany Bond, Independent", "Exhausted Ballot" and "Write-in", with the figures `1146,142 / 1165,136 / 21,655 / 322,778 / 393`.
   - Footnote 1 says: "This vote count is from round 2 of Maine's ranked-choice general election ... In round 1 ... Golden received 153,074 votes, and Bruce Poliquin received 141,260 votes."
   - The mark is glued only to the two finalists' figures. Bond's 21,655 is unmarked.
   - Bond was eliminated after round 1, so she has no round-2 count. 21,655 can only be her first-round figure.

3. The tool builds this mix and then labels it "last round".
   - us_house_prep.pl:486-494: the finalists are the candidates the "round 2" footnote is glued to. The tool reports a problem unless exactly two candidates carry the mark, so by construction any other candidate in a C row is not from round 2.
   - C is set at :493 and again at :523.
   - Bond (party O) is added into votes_other through `$v{$c->{party}} += $c->{total}`.
   - The gloss itself is hand-written in the generator, at :659 in `%flag_said`.

4. The project's own record agrees with the reviewer. house_districts.md reading 6 says: "2022 by its footnote ("from round 2"), the eliminated Independent's first-round figure beside the finalists' and an "Exhausted Ballot" line".

5. The mismatch is limited to one row's votes_other.
   - The 2018 page prints only Golden 142,440 and Poliquin 138,931.
   - The 2024 page prints only Theriault and Golden, plus the Continuing and Exhausted lines.
   - So the gloss is true for the other two C rows.

Why only a note:
   - No figure is wrong.
   - No code reads the gloss. A grep finds the wording only in the generator (:277, :659) and in the CSV.
   - GeneratedCatalogCheck holds nothing about what C means.
   - The record states the mix exactly.

The same omission appears in the texts this commit corrected:
   - house_by_state.csv's header
   - the UsPresidentialReturns.cs House doc
   - the plan's House-basis sentence
   - president_returns.md reading 13

All of them say "Maine's last ranked-choice round". Yet the 2022 recapitulation's Independent column (21,655) is the same first-round figure.

**The skeptic's corrected fix.** Fix the gloss in the generator, not in the CSV, because the CSV is generated and marked DO NOT EDIT. Change `%flag_said{C}` at Tools/us_house_prep.pl:659 to read: "Maine's ranked-choice count: the finalists at its last round, beside any eliminated candidate's first-round votes". Keep it general and name no year or figure, to stay inside the claim convention.

Optionally, bring the source comment at :277 into line with it.

Then re-run the tool. It rewrites house_districts.csv and UsHouseDistricts.cs together: the gloss is inside the hashed text ($dd = sha256_hex($csv_d), :701), so HouseDistrictSourceDigest changes and GeneratedCatalogCheck re-reads it. The mutation suite checks failure messages only, so it is unaffected.

Optionally, add the same words to the Maine clause in us_returns_prep.pl's house_by_state header and C# House doc (that also needs a re-run), in the plan's House-basis sentence, and in president_returns.md reading 13.

### 18. The C# HouseMaps summary drops the CSV header's 2026/Missouri caveat; 2022's rows are attributed to the wrong Census program

- **Lens:** catalog - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/Generated/UsHouseDistricts.cs:2229

**The scenario.** Row (2026, MO, 8, true): the same 120th page's note says the redrawn plan 'cannot be used' for 2026. house_maps.csv's header carries that caveat; the C# summary, which is what code reads, says only 'whether its lines changed ... as the Census Bureau says'. The CSV header also credits lines_changed to 'the Census Bureau's Redistricting Data Program', but the 50 rows for 2022 come from the Geography page. Source paths are written relative to raw/, while HouseRawSources is relative to usa/, so they never join.

**The fix proposed.** Carry the 2026 caveat into the C# summary, name the 2022 source correctly, and use one path root.

**The skeptic's evidence.** I could not refute the finding. All three parts hold as written, and all three affect only documentation: no code reads the affected fields.

1. The Missouri caveat is missing from the C# (real).
- The CSV header comes from Tools/us_house_prep.pl:692 (house_maps.csv line 4): "(2026: its plans as the Census Bureau collected them - Missouri's in doubt, the record's reading)".
- The C# summary comes from us_house_prep.pl:719-720 (UsHouseDistricts.cs:2229-2230). It says only "whether its lines changed since the previous House election, as the Census Bureau says (Source the saved page)".
- The row is UsHouseDistricts.cs:2507 `(2026, "MO", 8, true, "maps/census_rdo_congressional_districts_120th.html")`.
- The 120th page lists "Missouri*". Its note says the plan "cannot be used" and the Bureau's products "may not reflect the districts that will be in place for the November 2026 elections".
- `grep -i missouri` over Assets/*.cs finds nothing. The C# also drops the fact that the 2026 rows are plans as collected, not lines in force.
- The same commit already made the US-5 House C# summary repeat its CSV header's caveats (us_returns_prep.pl diff). So the generator's own practice is to keep the two in step, and HouseMaps breaks it.

2. 2022 is credited to the wrong Census program (real, but in the CSV header, not at the cited C# line).
- house_maps.csv:691 says "as the Census Bureau's Redistricting Data Program says".
- The 50 rows for 2022 (csv lines 156-205) cite maps/census_geography_congressional_districts.html. That page sits under programs-surveys/geography, and the record's register lists it as "U.S. Census Bureau, Geography - About Congressional Districts".
- The generator's comment at line 228 says the program's own 118th tab "names no state".
- The C# summary's wording, "as the Census Bureau says", is accurate. Only the CSV header has this problem.
- Each 2022 row's source column already names the right page.

3. The two path lists use different roots (true, but nothing joins them).
- Line 69 stores pages as `$read{"raw/$rel"}`, so HouseRawSources holds "raw/maps/..." and its summary says "by its path under `ElectionsData/usa/`".
- `$map_page` holds 'maps/...' (lines 207-241), so HouseMaps.Source is relative to raw/, and its summary does not say so.
- No code joins the two. CheckUsHouseDistricts compares m.Source only to the CSV's own column (SameRow, GeneratedCatalogCheck.cs:705) and re-hashes HouseRawSources separately.
- grep finds no other reader of HouseMaps, LinesChanged or Source. So there is no failing path today; the risk is only to a future reader who tries to match the two.

Impact: documentation only. LinesChanged is read only by the round-trip check, and part two's cycles (2018 to 2020, 2020 to 2022, 2022 to 2024) never read a 2026 row. The caveat survives in the CSV header, house_districts.md reading 8 and the plan's BILLED list. Severity stays at note.

**The skeptic's corrected fix.** All three changes go in Tools/us_house_prep.pl, followed by a re-run of the tool (it rewrites the CSVs, the digests and UsHouseDistricts.cs; GeneratedCatalogCheck re-reads).

(1) Lines 719-720: add the CSV's 2026 clause to the HouseMaps summary, for example: "... whether its lines changed since the previous House election, as the Census Bureau's pages say (Source: the saved page, its path under `ElectionsData/usa/raw/`; 2026: the plans as the Bureau collected them - Missouri's in doubt, the record's reading, house_districts.md reading 8)".

(2) Line 691: change the wording to "as the Census Bureau's Redistricting Data Program tabs say (2022: the Bureau's geography page, its own tab naming no state)".

(3) Pick one path root. The cheap option is to state the root in the summary, as in (1); the data does not change. The other is to emit "raw/$map_page{$year}" at line 695 (and in the run report at line 739), so each Source string matches its HouseRawSources path exactly. That changes house_maps.csv's bytes and HouseMapSourceDigest, which the re-run regenerates.

### 19. The 'vacant' gloss says 'without a member on the opening day', but the footnotes are about a missing certificate

- **Lens:** catalog - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/house_years.csv:3

**The scenario.** [HH-DIV] footnotes 5 and 6 say the state 'did not submit an election certificate'. The Clerk's division table is headed '[ALL FIGURES REFLECT IMMEDIATE RESULTS OF ELECTIONS]', and the division counts 212 R, which includes LA-5's winner. The column therefore means 'the seat the division leaves unassigned'. A seat whose member-elect could not be seated is counted by party and not listed here; LA-5 in 2020 is such a case, though that comes from public record, not a saved page. The C# summary repeats the same gloss.

**The fix proposed.** Gloss the column as 'the seat the division's footnote leaves unassigned (no certificate before the opening day)'.

**The skeptic's evidence.** I tried to refute this and could not. The wording does not match its source, but no figure, check or consumer is affected.

What the generated text says:
- Tools/us_house_prep.pl:697 (house_years.csv line 3): "vacant: the district its footnote leaves without a member on the opening day, or -."
- Tools/us_house_prep.pl:724 (the C# HouseRecord summary): "the district the page leaves vacant on the opening day (a hyphen for none)."
- The hand-written record, ElectionsData/usa/house_districts.md:9, uses the same framing: "the district its footnote leaves vacant on the opening day".

What the source says:
- raw/executive/history_house_party_divisions.html:872-873, footnotes 5 and 6: "The State of North Carolina / New York did not submit an election certificate for the Ninth / Twenty-Second U.S. Congressional District prior to the opening day of the 116th / 117th Congress." Neither footnote says "without a member".
- The same page's preamble, line 830: "The figures presented are the House party divisions as of the initial election results ... subsequent changes in House membership due to deaths, resignations, contested or special elections ... are not included."
- The Clerk's table in every clerk_statistics PDF is headed "[ALL FIGURES REFLECT IMMEDIATE RESULTS OF ELECTIONS]". Its 117th row reads "435 222 212 ... 1": one seat Vacant.
- So the division, and this column with it, is not a list of seats empty on the opening day.

What the code means by the column:
- The generator removes NY-22 from the count (lines 541-546: `$r--`) and keeps NC-9 with no winner.
- GeneratedCatalogCheck subtracts `vacantWinner` and describes it as "the seat its footnote leaves vacant being the one race without a winner or the one winner it does not count".
- So the column means "the seat the division leaves unassigned", as the finding says.

The failing path:
- house_districts.csv has `2020,LA,5,...,R,LMS`. That winner is inside the House Historian's (HH-DIV) 212 R: 213 district winners less NY-22 gives 212. By public record the member-elect died before the opening day.
- `2022,VA,4,...,D` (inside the 213 D) and `2024,FL,1,...,R` (inside the 220 R) are the same kind of case, also by public record.
- None of these is on a saved page; the 2020 Clerk text has no such note.
- Read as a criterion, the gloss gives an incomplete 2020 row. Under the C# "(a hyphen for none)", the hyphens for 2022 and 2024 read as false.
- Read literally, the words "its footnote" limit the claim, and both listed seats really had no member on the opening day. That is why this is a note, not a defect.

Wider context:
- The record's own reading 5 (house_districts.md:54) already has the exact form: "a seat the division leaves out (its footnote 6: no certificate before the opening day)". The generated headers disagree with it.
- No consumer exists yet. Part two is not in this commit.
- ReadUsCsv skips '#' lines, so the gloss reaches the check only through the CSV digest.

**The skeptic's corrected fix.** 1. Reword the gloss in Tools/us_house_prep.pl.
   - Line 697 (the house_years.csv header): "vacant: the district its footnote leaves out of the division - the state submitted no election certificate for it before the opening day - or -." Optionally add: "the division is as of the initial results, so a member-elect lost before the opening day is still counted by party."
   - Line 724 (the C# summary): "and the district its footnote leaves out of the division (no election certificate before the opening day; a hyphen for none)."
2. Make the same change by hand in ElectionsData/usa/house_districts.md:9.
3. Name no specific seats in the new wording (claim convention).
4. Re-run the tool from the root to regenerate the outputs. The header change moves the CSV's SHA-256, and HouseYearSourceDigest in UsHouseDistricts.cs is regenerated with it. GeneratedCatalogCheck and the mutation suite need no change.

### 20. us_house_prep.pl's header lists three outputs; it writes four

- **Lens:** catalog - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_house_prep.pl:15

**The scenario.** The header names house_districts.csv, house_maps.csv and UsHouseDistricts.cs. write_all (line 728) also writes house_years.csv, and the record says 'All four outputs'.

**The fix proposed.** Add house_years.csv to the header.

**The skeptic's evidence.** The finding is correct, and I could not refute it. The staged copy (`git show :Tools/us_house_prep.pl`) matches the working copy.

Tools/us_house_prep.pl:15-16, the header, names three outputs:
  "# It writes ElectionsData/usa/house_districts.csv, ElectionsData/usa/house_maps.csv and Assets/Scripts/Elections/Generated/UsHouseDistricts.cs
   # (a part of the partial class UsPresidentialReturns). It dies, writing nothing, on:"

The code writes four. Lines 728-729:
  write_all("$usa/house_districts.csv" => $csv_d, "$usa/house_maps.csv" => $csv_m, "$usa/house_years.csv" => $csv_y,
            'Assets/Scripts/Elections/Generated/UsHouseDistricts.cs' => $cs);
$csv_y is built at lines 696-700 (year,congress,seats_r,seats_d,seats_other,vacant).

Every other place names the fourth file:
- the tool's own end-of-run print (line 745: "... house_maps.csv (...rows), house_years.csv, Assets/...UsHouseDistricts.cs");
- the generated C# header (line 703: "house_districts.csv, house_maps.csv and house_years.csv");
- the mutation suite (Tools/us_house_mutations.sh:15, OUTS lists all four; line 20 deletes all three CSVs);
- the record (ElectionsData/usa/house_districts.md:9 describes house_years.csv; line 26 says "All four outputs are built and tested before the first is written");
- the plan (USA_STAGE_PLAN.md:292 names all three CSVs);
- GeneratedCatalogCheck.cs:693, which re-reads house_years.csv against HouseYearSourceDigest.

So the header is the only place that is wrong. It leaves out a file the tool overwrites.

Why only a note: this is a doc comment and nothing reads it. Nothing parses the header, and the mutation suite carries its own OUTS list. The output is generated deterministically and checked by the re-read, so no data, check or runtime path changes. The header's other claims still hold, including line 33 "All outputs are built and tested before the first is written", because the ASCII gate at lines 650 and 652-657 loops over all four. This is not a transcribed figure or an undeclared premise under the CLAUDE.md claim convention, so it does not rise to minor.

**The skeptic's corrected fix.** Name house_years.csv in the header. Write it the way line 703 does, so the line stays within the header's width (the widest header line is 144 characters, line 15 is 142, the new line is 141):

# It writes ElectionsData/usa/house_districts.csv, house_maps.csv, house_years.csv and Assets/Scripts/Elections/Generated/UsHouseDistricts.cs
# (a part of the partial class UsPresidentialReturns). It dies, writing nothing, on:

Optionally, also say what the file holds where the header lists the [HH-DIV] reads, for example "(the party division a year, with its footnoted vacancy, to house_years.csv)". The one-line change above is enough.

### 21. US-8's new Need says only the districts hold the 119th's delegations, but a saved roll call holds them, and US-5 says the presidency never waits on the districts

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** docs/specs/USA_STAGE_PLAN.md:258

**The scenario.** R-US3 (a) needs the House seated in January 2025 by state delegation. ElectionsData/usa/raw/records/clerk_house_vote_2025_roll001.html ([CLK-R1] in records_by_date.md, the 119th's opening-day quorum call) lists all 435 members with party and state: 220 R and 215 D over 50 states, with the same FL 20-8 and NY 7-19 delegations the 2024 district winners give. So 'and only the districts hold them' is false. The new Need also contradicts line 202 (US-5: 'the 435 districts are US-11's, so the presidency never waits on the district PDFs'), and it puts checkpoint 1 behind a Phase D item.

**The fix proposed.** Drop 'only the districts hold them' or name [CLK-R1] beside the districts and declare which one R-US3 reads. Then either take US-11 out of US-8's Needs or amend line 202 so the two lines agree.

**The skeptic's evidence.** The new clause is false, and it contradicts line 202 of the same plan.

1. The new line (staged docs/specs/USA_STAGE_PLAN.md:258): "*Needs:* US-4, US-6, US-7; US-11's district catalog (§790) - R-US3 reads the House's delegations by state, the record's until US-16 elects one, and only the districts hold them; R-US3; R-US14." Before this change the line was "*Needs:* US-4, US-6, US-7; R-US3; R-US14."

2. A saved roll call already holds the delegations.
- `ElectionsData/usa/raw/records/clerk_house_vote_2025_roll001.html` has been committed since 0aa2d8c6 (s617) and is in 14ee39b6.
- records_by_date.md:201 registers it as [CLK-R1], "quorum call of the 119th's first day". The record only uses its sidebar live count; the member table has never been extracted.
- That table has 435 rows, each with party, state and vote: Republican 220, Democratic 215, across 50 states. Gaetz (FL) is listed as "Not Voting".
- I counted it by state and compared it with the staged house_districts.csv winners for 2024. The two match in all 50 states (FL R20/D8, NY D19/R7, NC R10/D4, and so on).
- So "only the districts hold them" is false. A defensible version would be "only the districts are in a catalog the game reads", but the sentence does not say that.

3. The districts also do not give R-US3's own basis. R-US3 (a) at :499 reads "the House seated on 6 January". `clk_house_vote_2025_roll002.html` ([CLK-R2], the Speaker vote the same day) lists 434 members, with FL at R19/D8 because FL-1 was vacant. The districts give the House as elected (FL R20/D8, Gaetz included), not as seated. No state's majority differs between the two readings, so no outcome changes. Still, which basis R-US3 reads is an undeclared premise.

4. It contradicts line 202, which this change left alone: "National totals only: the 435 districts are US-11's, so the presidency never waits on the district PDFs." US-8, the presidency's checkpoint 1, now needs a catalog built from those PDFs.

5. A Phase C item now needs a Phase D item. The phase table at :152 puts US-6 to US-8 (checkpoint 1) in C and US-9 to US-14 in D. US-8 now needs US-11. This part is weak: the Need names only the half already built in §790 of this same commit, so nothing actually waits.

Severity: the problem is confined to the document. No code, bar or outcome changes, and the Need is already met. It is still a false existence claim and an internal contradiction, newly added to a document the claim convention governs. That makes it minor.

**The skeptic's corrected fix.** Change the text only, and transcribe no figures.

1. Rewrite line 258 so it names both holders and declares the basis. Suggested wording: "*Needs:* US-4, US-6, US-7; R-US3 - it reads the record's House by state delegation until US-16 elects one: the 2024 district winners (US-11's catalog, §790), checked state by state against the 119th's opening quorum call [CLK-R1]. DECLARED: the House as elected, [HH-DIV]'s basis and the one US-2 seats; the House as it sat at the Speaker vote [CLK-R2] is not modelled. R-US14."

2. Amend line 202 to match. Either say that the national vote (US-5) never waits on the district PDFs, or add that US-8 reads only US-11's data half, built at §790.

The other option is to take US-11 out of US-8's Needs and have R-US3 read [CLK-R1] directly. That needs a new extractor, so the text fix above is the cheaper route.

### 22. Maine 2022 is called 'the last ranked-choice round' in corrected reading 13, the plan and flag C, but the Clerk prints the eliminated Independent's round-1 figure

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/president_returns.md:58

**The scenario.** The Clerk's 2022 ME-2 listing prints Poliquin 146,142 and Golden 165,136 (round 2, footnote 1) beside Bond 21,655 and Write-in 393. The FEC 2022 House sheet's ME-2 footnote gives round 2 as 'Golden: 165,136; Poliquin: 146,142; Bond: 0; Write-In: 0', so 21,655 and 393 are round-1 figures. This is the same mix of counts the correction calls out for Louisiana, and house_districts.md reading 6 (line 55) describes it correctly. These places still say last round: reading 13 ('true of neither Louisiana nor Alaska'), the plan's House basis (USA_STAGE_PLAN.md:219) and US-11 (:292), the house_by_state.csv header (us_returns_prep.pl:945), the C# doc (:994), and flag C in the house_districts.csv header (us_house_prep.pl:659, 'printed at its last round'). As a result the 2022 ME-2 row (votes_other 21655, flags CM) and house_by_state's 2022 ME votes_other (353948) carry a round-1 figure under a 'last round' label.

**The fix proposed.** In reading 13, the plan's two sentences and both generators' header and doc texts, word Maine 2022 as reading 6 does: the finalists at round 2, beside the eliminated Independent's and the write-ins' round-1 figures. Or narrow flag C to the finalists' figures. Then regenerate.

**The skeptic's evidence.** I could not refute the finding. The facts check out on the saved pages, and the staged texts say what the finding quotes.

1. The Clerk's 2022 Maine listing (`pdftotext -raw clerk_statistics2022.pdf`, ME-2):
   - Entries in order: Poliquin, Golden, Bond (Independent), "Exhausted Ballot", "Write-in".
   - Figures in the same order: `1146,142`, `1165,136`, `21,655`, `322,778`, `393`.
   - Footnote: "1This vote count is from round 2 of Maine's ranked-choice general election ... In round 1 of the election, Jared F. Golden received 153,074 votes, and Bruce Poliquin received 141,260 votes."
   - Mark 1 is glued only to the two finalists' figures. Bond's 21,655 and the write-ins' 393 carry no mark.

2. The FEC's 2022 workbook (sheet8.xml, "8. US House Results by State"):
   - Row 1655, Bond: general column P = 21655, round-2 column AA = 0.
   - Row 1656, Scattered: P = 393, AA = 0.
   - Footnote X: "the results shown are for the first round of Ranked Choice Voting (RCV). Second round results were: Golden: 165,136; Poliquin: 146,142; Bond: 0; Write-In: 0".
   - So 21,655 and 393 are first-round figures that the Clerk prints beside the finalists' round-2 figures. This is the same mix that reading 13 now spells out for Louisiana.

3. The staged texts still call this race the last round:
   - `president_returns.md:58`: "this reading first said "each race's final count", true of neither Louisiana nor Alaska ... Maine's last ranked-choice round (2022's footnote: "from round 2" ...)". This implies the old wording was true of Maine, although Maine 2022 is the same mix as Louisiana.
   - `USA_STAGE_PLAN.md:219`: "Maine's last ranked-choice round".
   - `USA_STAGE_PLAN.md:292`: "(Maine's last round, Alaska's first choices)".
   - `us_returns_prep.pl:945` and `:994`: "Maine's last ranked-choice round". These are emitted into `house_by_state.csv:4` and `UsPresidentialReturns.cs:334`.
   - `us_house_prep.pl:277`, a comment: "Maine's 2nd district printed at its last round (C)".
   - `us_house_prep.pl:659`, flag C: "Maine's ranked-choice count printed at its last round". This is emitted as `house_districts.csv:9`.

4. The rows carry the first-round figures under that label:
   - `house_districts.csv:1502` is `2022,ME,2,146142,165136,21655,656104,...,D,CM`. Its votes_other is Bond's first-round figure.
   - `house_by_state.csv:176` is `2022,ME,275405,384889,353948,1014242`. The 353,948 is 8,962 + 322,778 + 21,655 + 160 + 393.

5. `house_districts.md:55` (reading 6, the same commit) states it correctly: "2022 by its footnote ("from round 2"), the eliminated Independent's first-round figure beside the finalists'". It leaves out the write-ins' 393. The record therefore contradicts itself within one commit.

Why minor rather than a defect: no figure, winner or check changes.
- The winner pool is the marked finalists (`us_house_prep.pl:486`, `@final` = candidates marked "round 2").
- R and D are the round-2 figures, and the two-party split is untouched.
- The Done-when (REP 222 / DEM 213) holds either way.
- What is wrong is the source reading and the flag label. In a correction made to fix exactly this kind of wording, that is a real record inaccuracy.

Related but not staged: `president_returns.md:60`, which this diff does not change, says the FEC's footnotes "give the final rounds - the Clerk's figures". That holds for the finalists only, since the FEC's final round gives Bond 0.

**The skeptic's corrected fix.** Describe Maine 2022 as a mix everywhere, as reading 6 does, and name the write-ins too:

1. `president_returns.md:58`:
   - Change "true of neither Louisiana nor Alaska" to "true of neither Louisiana, Maine's 2nd district in 2022, nor Alaska".
   - Reword Maine's clause along these lines: "Maine's 2nd district its two finalists at the last ranked-choice round - 2018 those two alone (the FEC's own footnote gives that final round); 2022 by its footnote ("from round 2", glued to the finalists only), beside the eliminated Independent's and the write-ins' first-round figures, as Louisiana's runoff districts print theirs; 2024 beside a "Continuing Ballots" line equal to their sum".
   - Optionally narrow line 60's "which give the final rounds - the Clerk's figures" to "the finalists' final round - the Clerk's figures for them".

2. `us_house_prep.pl`:
   - Flag C at :659 becomes something like "Maine's ranked-choice race: its finalists printed at the last round (2022 beside the others' first-round figures)".
   - Make the same change to the comment at :277.

3. `us_returns_prep.pl:945` and `:994`: "Maine's finalists at the last ranked-choice round (2022 beside the others' first-round figures)".

4. `USA_STAGE_PLAN.md:219` and `:292`: the same wording.

5. Regenerate:
   - Re-run `us_returns_prep.pl`. This moves `house_by_state.csv`, `HouseSourceDigest` and `UsPresidentialReturns.cs`.
   - Re-run `us_house_prep.pl`. This moves `house_districts.csv` and `HouseDistrictSourceDigest` in `UsHouseDistricts.cs`.
   - Both CSV digests cover the header comment lines, so both change.
   - Because `HouseSourceDigest` moves, the US card's readings stamp moves too: `UsNationalVoteCheck.cs:340` feeds `HouseSourceDigest` into its source-digest. Re-stamp `US_ELECTIONS.md` through the Editor's `WriteReadings`, then run the cheap bar (`GeneratedCatalogCheck`).

`us_house_mutations.sh` does not match on the "last round" wording (its one Maine case, x04, expects the "not declared ranked" message), so the suite needs no change. If the `problem()` texts at `us_house_prep.pl:518` and `:522` are reworded too, check that no mutation case expects them.

### 23. Risk 14 still says the Clerk's PDFs are inflated by perl and that CID fonts are a risk; the US-11 text edited in this commit says the opposite

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** docs/specs/USA_STAGE_PLAN.md:758

**The scenario.** Risk 14 reads 'the Clerk's PDFs are inflated by perl, and CID fonts have come out as glyph codes before'. Line 292, edited in this commit, now says the text is pdftotext -raw's and the CID trap does not arise. Both us_returns_prep.pl (since §788) and us_house_prep.pl read the Clerk PDFs through /mingw64/bin/pdftotext (xpdf 4.06). Inflating all five PDFs' streams shows only Type1 fonts and no CIDFont. A reader of §7 is told about a method and a risk the plan has retired.

**The fix proposed.** Rewrite the bullet: the Clerk's PDFs are read through pdftotext -raw and use Type1 fonts (US-11). Keep the perl and CID caution only for PDFs that have not been read yet.

**The skeptic's evidence.** I could not refute this finding. The contradiction is in the staged plan (git show :docs/specs/USA_STAGE_PLAN.md).

- Line 758, risk 14, is unchanged by this commit. It reads: "There is no PDF renderer: the Clerk's PDFs are inflated by perl, and CID fonts have come out as glyph codes before."
- Line 292, US-11's text, is rewritten by this commit. It now reads: "their text is `pdftotext -raw`'s - the fonts are Type1, so the CID trap first feared here does not arise".
- Line 721, the sources table row as corrected at s788, says "(PDF; text by `pdftotext -raw`)".

**History (`git log -S`).** s777 (cba1bd04) wrote "inflated by perl" in three places: the US-11 text, the sources row and risk 14. s788 (40583b01) corrected the sources row and s790 corrects the US-11 text. Risk 14 is the only stale copy left, in the same section 7 where this commit rewrote risk 9.

**The method claim is false.**
- Tools/us_house_prep.pl:44 sets `my $pdftotext = -x '/mingw64/bin/pdftotext' ? ...`. Its `pdf_text` at lines 72-77 opens `$pdftotext, '-raw'`.
- Tools/us_returns_prep.pl:547, 650 and 806 do the same.
- Tools/us_house_mutations.sh:11 and 27-29 wrap the pdftotext output.
- No file under Tools/ uses Compress::Zlib or inflates anything (grep finds none).
- The new record house_districts.md and president_returns.md reading 9 also say `pdftotext -raw`.

**The CID risk does not arise for these PDFs.** I inflated every stream of all five clerk_statistics PDFs with perl Compress::Zlib (all 78 to 110 streams per file inflated). Every font is `/Subtype /Type1`, with no Type0 or CIDFontType font:
- 2016: NewCenturySchlbk family and CenturySchL-Roma.
- 2018-2024: NewCenturySchlbk family, Helvetica and ZapfDingbats.

Each embedded font has a ToUnicode map. The CIDSystemInfo hits match the ToUnicode count one for one and belong to those maps. `pdftotext -raw` output contains no control-character lines and prints "FOR UNITED STATES REPRESENTATIVE" cleanly.

**What still holds.** "There is no PDF renderer" is still literally true: /mingw64/bin holds pdftotext.exe and no rasteriser. "CID fonts have come out as glyph codes before" is still true as history about other PDFs. But the bullet presents perl inflation as the Clerk method and keeps the glyph-code risk on the Clerk's PDFs, which line 292 now retires.

**Impact.** This is documentation only, with no effect on code or data. It is a half-finished correction inside a commit whose stated scope includes these plan corrections, so I grade it minor.

**Outside this finding.** CLAUDE.md:124 also says "no PDF renderer - inflate FlateDecode streams with perl Compress::Zlib" without naming pdftotext. That is a general environment rule this commit does not touch.

**The skeptic's corrected fix.** Rewrite the second bullet of risk 14 (docs/specs/USA_STAGE_PLAN.md, line 758) as:

"- There is no PDF renderer, only Git for Windows' `pdftotext` (xpdf): the Clerk's PDFs are read by `pdftotext -raw`, their fonts Type1 with ToUnicode maps (US-11, §790). A PDF not yet read may carry CID fonts without ToUnicode, which have come out as glyph codes before, so its text is checked before it is used."

This brings risk 14 into line with line 292 and the sources row at line 721. It keeps the caution for PDFs not yet read, such as the veto, Senate and court-order PDFs, and adds no figures.

### 24. The plan says the tool flags uncontested seats; only the 11 unopposed seats left off the ballot carry a flag

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** docs/specs/USA_STAGE_PLAN.md:292

**The scenario.** The built item lists 'the unopposed seats printed without votes' and, separately, 'uncontested seats flagged'. The catalog's only such flag is U ('unopposed, its name not printed on the ballot'), on 11 races. 66 races where one Republican or Democrat stood alone, with votes printed and no other candidate, carry no flag (64 have '-', 2 have 'F'). Example row: '2016,GA,9,256535,0,0,256535,256535,0,1,0,R,-'. R-US18 (a) handles uncontested seats by a DECLARED rule, so part two keyed on the plan's word would read GA-9 2016 as a contested 100-0 district unless it works contestedness out from cands_r, cands_d and votes_other.

**The fix proposed.** Either add a flag for single-candidate races (defined in the record and the CSV header) and regenerate, or reword the plan: uncontested seats can be derived from cands_r/cands_d/votes_other, and flag U marks only those not printed on the ballot.

**The skeptic's evidence.** The finding holds, and nothing in the code refutes it.

1. The plan claims the flag, and this change is what made that a claim about the build. Staged docs/specs/USA_STAGE_PLAN.md:292 rewrites US-11's list of exceptions held as data. The new list includes both "the unopposed seats printed without votes" and "uncontested seats flagged". The same paragraph then says "**Built, the data half: `COMPLETED.md` §790**". The phrase "uncontested seats flagged" was already in the item at 14ee39b6. Two things in this change turn it into a false tracking claim: the sentence was corrected to match the build and the phrase was kept, and the "Built" marker was added after it.

2. The tool has no uncontested flag. In Tools/us_house_prep.pl:
   - `%flag_said` (around line 659) defines exactly A, C, F, L, M, N, S and U. U is "unopposed, its name not printed on the ballot - no votes".
   - U is set only in the `if (@unopp)` branch (lines 439-451). That branch runs only when an entry carries the Clerk's "(n)" no-opposition mark, so a lone candidate whose votes are printed never gets it.
   - Nothing in UsHouseDistricts.cs or GeneratedCatalogCheck.cs mentions uncontested or unopposed races.

3. The staged house_districts.csv matches the finding's numbers. It has 77 races with cands_r + cands_d == 1 and votes_other == 0:
   - 11 are flagged U (FL, OK and LA, all with zero votes);
   - 64 are flagged '-';
   - 2 are flagged 'F' (2016 NY-17 and 2018 NY-16).

   The example row `2016,GA,9,256535,0,0,256535,256535,0,1,0,R,-` is in the CSV. The Clerk's 2016 text (pdftotext -raw) prints "9. Doug Collins, Republican ... 256,535" as the district's only entry.

4. Under the two-party sense that uniform swing (R-US18 (a)) usually uses, the gap is wider. 205 races have cands_r == 0 or cands_d == 0. Of these, 11 are flagged U and 37 are flagged S, and 157 carry neither flag.

5. Contestedness depends on a definition; votes_other alone cannot settle it. Named write-in candidates count as party-O candidates: `2016,GA,10,243725,0,1096,...` comes from "Leonard Ware, Write-in" and "Patrick A. Boggs, Write-in". So a reader cannot recover "uncontested" from votes_other without first choosing a rule. That is exactly what the plan says is already flagged as data.

Mitigation: cands_r and cands_d are in the catalog, so part two can derive two-party uncontested seats. No output is wrong. The part-two harm in the scenario is plausible but not shown. The defect is a plan/tracking claim that the build does not meet, so it stays minor.

**The skeptic's corrected fix.** Reword the plan rather than add a flag, because the definition of an uncontested seat belongs to R-US18 (a)'s DECLARED rule in part two. In USA_STAGE_PLAN.md US-11, replace "uncontested seats flagged" with wording along these lines: "each party's candidates counted (cands_r, cands_d), from which part two reads the uncontested seats by R-US18 (a)'s declared rule (flag U marks only the unopposed seats not printed on the ballot)". Do not transcribe any counts, per the claim convention.

If a flag is wanted as data in the data half instead, do all of the following:
- add a letter to `%flag_said` in Tools/us_house_prep.pl, with its definition stated (one candidate alone, or one major party with no candidate; and whether named write-ins count);
- define it in house_districts.md;
- regenerate the outputs;
- have GeneratedCatalogCheck hold the flag against cands_r, cands_d and votes_other;
- add a mutation case.

### 25. Risk 9 copies the tool's derived counts into the plan ('five states' for 2024 and ten for 2026'), from a Census page that is revised in place

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** docs/specs/USA_STAGE_PLAN.md:751

**The scenario.** The counts come from house_maps.csv and the run's 'lines changed for ...' lines. The 120th tab is revised in place: its Missouri note post-dates its own 'Page Last Revised - August 28, 2026' stamp and says the plan 'may not reflect the districts that will be in place for the November 2026 elections'. A re-fetch after Missouri's referendum, or after another state's plan is collected, makes the tool write 9 or 11 rows for 2026 while risk 9 still says 'ten'. No check scans docs/specs: DocumentClaimCheck reads only root *.md files, and only for Type.Member references.

**The fix proposed.** Point to the source instead of copying the numbers, e.g. 'North Carolina's lines changed for 2020, every multi-seat state's for 2022, and for 2024 and 2026 the states house_maps.csv names (the tool prints them)'.

**The skeptic's evidence.** I tried to refute this and could not. Every factual claim in the finding checks out against the staged tree.

1. **The line.** Staged docs/specs/USA_STAGE_PLAN.md:751 reads: "Read at US-11 (§790, `house_maps.csv`, by the Census Bureau's pages): North Carolina's lines changed for 2020, every multi-seat state's for 2022, five states' for 2024 and ten for 2026." The plan is a live spec, so it is not exempt; only COMPLETED.md is.

2. **The counts are copied from the generated file.** In the staged house_maps.csv, the rows with lines_changed=1 are AL GA LA NY NC for 2024 (5) and AL CA FL LA MO NC OH TN TX UT for 2026 (10). us_house_prep.pl:739 prints them as "lines changed for $year ($n states ...)", and house_districts.md:41-42 pastes those lines (5 and 10). The pages use the same count words:
   - 119th tab, line 2386: "Five states (Alabama, Georgia, Louisiana, New York, and North Carolina) delineated new boundaries for the 2024 election cycle".
   - 120th tab, line 2326: "Ten states (Alabama, California, Florida, Louisiana, Missouri*, ...)".

3. **The 120th tab is revised in place.** Its note at line 2327 says "On September 3, 2026, the Missouri Supreme Court ruled ... may not reflect the districts that will be in place for the November 2026 elections". The page's own stamp at line 3026 is "Page Last Revised - August 28, 2026", so the note post-dates it. Reading 8 of house_districts.md says the same: "That note is dated after the page's own revision stamp".

4. **The failing path exists.** The tool's regex at us_house_prep.pl:212, `(\w+) states \(([^)]*)\) delineated new boundaries`, reads count words up to twelve (`%word`, line 199). A re-fetched page saying "Nine states" or "Eleven states" therefore parses and writes 9 or 11 rows for 2026. If a re-fetch drops the Missouri note, the run stops on the pinned regex (lines 249-250) until someone edits the tool. In both cases nothing touches the plan.
   - The plan expects this re-read: its BILLED list at line 735 bills each post-2024 redistricting "once its enacted map is read".
   - That same BILLED line, in this same diff, states the fact by reference ("the Census Bureau's 120th tab names the states that redrew for 2026, Missouri's plan in doubt ... - `house_maps.csv`, §790"). Risk 9 transcribes the counts instead and drops the Missouri doubt.
   - So "ten for 2026" already overstates what the source says. The page itself warns that Missouri's plan may not be the one used in November 2026.

5. **No check catches it.** DocumentClaimCheck.cs:124 reads only `Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly)`, and its regex at :64 matches only a backticked `Type.Member`. Nothing in Assets/Editor or Tools reads the plan's risks.

6. **The rule covers it.**
   - CLAUDE.md:39-41: DERIVED includes "a count", and "Nobody transcribes, anywhere, in any file".
   - The long form says: "name the command that produces it instead of its result".
   - The discipline's rule 2 says "Every real-world number carries source, vintage and basis". The 2026 count carries neither the page's vintage nor its basis (the plans as collected, Missouri in doubt).
   - Rule 9 says "A status about the outside world is a cached value".

7. **Severity: minor.** Two things limit it:
   - The 2024 count is settled history and will not move.
   - Both counts match house_maps.csv today, and the issue is in a document only, with no runtime effect.

   The 2026 count is the live one: it sits on a page revised in place, and the plan itself bills a re-read of it. The precedent is Reviews/2026-10-06_s788_us5_house_returns.md, finding 18: derived figures transcribed into this same plan were graded minor by both the reviewer and the skeptic, and were acted on.

**The skeptic's corrected fix.** Change only the plan's text (document tier); no regeneration is needed. At docs/specs/USA_STAGE_PLAN.md:751, replace the counts with a reference and carry the source's basis for 2026, e.g.:

"9. **Boundaries move.** Read at US-11 (§790, by the Census Bureau's pages): North Carolina's lines changed for 2020, every multi-seat state's for 2022, and for 2024 and 2026 those of the states `house_maps.csv` names (the tool prints each year's). 2026's set is the plans as the Bureau collected them, and Missouri's is in doubt (the 120th tab's note, on a page revised in place). A district's number is a label, not a place, ..."

Leave the rest of the risk unchanged. This matches the wording the same diff already uses for the same fact in the BILLED list at line 735.

### 26. The 'Not reached' section cites refusals that the fetch log does not record

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/house_districts.md:74

**The scenario.** The section opens 'Each attempt is logged with its time and status in raw/maps/fetch_log.txt' (line 72). Line 74 adds '(the earlier attempts of the item's map pass were 429 too)'. raw/maps/fetch_log.txt holds a single Internet Archive attempt for SL 2023-145 (2026-10-06T03:27:47Z, 429) and nothing earlier, so the plural claim has no saved support. Line 77 cites elections.alaska.gov's 405 for Alaska's 2024 rounds. That 405 is raw/rules/fetch_log.txt's request for RCV.php (a rules page) at 2026-10-05T19:45:39Z, not a request for the 2024 results, and it is not in raw/maps.

**The fix proposed.** Drop the parenthesis or log the earlier attempts. Say that no Alaska 2024 results URL was requested and that the 405 was the host's answer to RCV.php. Limit the opening sentence to the attempts actually logged in raw/maps.

**The skeptic's evidence.** I could not refute the finding. Both halves hold, and the code already handles neither.

The record (ElectionsData/usa/house_districts.md) says:
- line 72: "Each attempt is logged with its time and status in `raw/maps/fetch_log.txt`."
- line 74: "...the Internet Archive's 2023-11-30 capture 429 (the earlier attempts of the item's map pass were 429 too)."
- line 77: "**Alaska's 2024 ranked-choice rounds** — elections.alaska.gov answered 405 (`raw/rules/fetch_log.txt`)..."

**1. The NC parenthetical (line 74).** raw/maps/fetch_log.txt has 18 lines:
- 13 lines answered 200 at 03:26:27–03:27:07Z;
- one Internet Archive line for this capture: `2026-10-06T03:27:47Z 429 620 wayback_ncleg_SL2023-145_20231130.pdf`;
- the 115th tab (200, 03:45:13Z);
- three refusals at 03:49:45–03:49:50Z (403, 401, 403).

Nothing in the log is earlier than 03:26.

The earlier attempts did happen. They are recorded only in the session scratchpad:
- us11map/probe_log.tsv has `429 620 wayback_nc_SL2023-145.pdf` at 02:34:01Z, 02:34:49Z, 02:38:10Z and 02:40:50Z;
- us11map/map_maps.md:65 lists the same four times.

Neither file is committed. The out-of-tree folder PoliSim-captures/sources/usa_us11/maps/ is empty, and no repo file points to the probe log. So the parenthetical is true but has no saved support. It also contradicts line 72, because those attempts are not in raw/maps/fetch_log.txt. The same goes for the map pass's other unlogged refusals: ncleg 403 at 02:26:17Z, GA 403/401 at 02:30:54–56Z, and the C2022C/C2023E captures 429. The GA and ncleg refusals were requested again at 03:49 so they could be logged; the four Internet Archive 429s were not.

**2. The Alaska bullet (line 77).** raw/rules/fetch_log.txt:4 reads `2026-10-05T19:45:39Z 405 2301 alaska_elections_rcv.html https://www.elections.alaska.gov/RCV.php`. That is a rules page, fetched a day earlier. It is the only Alaska request in any log:
- in-tree: every raw/*/fetch_log.txt;
- out-of-tree: the usa_us3/usa_us5 fetch logs, where usa_us3/cited2024/fetch_log.txt:8 holds the same RCV.php line;
- the map pass's probe_log.tsv, which has 0 Alaska lines.

president_returns.md:81 itself says "elections.alaska.gov/RCV.php ... HTTP 405 on 2026-10-05 (`raw/rules/`)". The scratch critic (us11map/critic.md:91, C8) corrected only the pointer; the bullet still presents a rules page's 405 as the reason the 2024 rounds were not reached. No URL for the 2024 results was ever requested. This conflicts with CLAUDE.md:112 ("where it cannot be reached, the row is BILLED with what is missing and why") and with the three-way test ("would a reader with no memory of the session reach the same verdict from what is written down").

**Why minor.** The damage is limited to the record's wording. raw/maps/SHA256SUMS.txt does not list fetch_log.txt, and neither Tools/us_house_prep.pl, Tools/us_house_mutations.sh nor GeneratedCatalogCheck.cs reads it. No generated figure, CSV or check is affected.

**The skeptic's corrected fix.** Edit only house_districts.md. No tool, CSV or digest changes, because the fetch log is neither hashed nor read.

- **Line 72:** scope the sentence to what is logged. For example: "This item's fetches and refusals are logged with their time and status in `raw/maps/fetch_log.txt`; the Alaska site's one refusal is in `raw/rules/fetch_log.txt`."
- **Line 74:** drop "(the earlier attempts of the item's map pass were 429 too)". The alternative is to add a dated NOTE line to raw/maps/fetch_log.txt, in the form raw/returns/fetch_log.txt:17 uses, saying the map pass also requested the same capture at 02:34:01Z, 02:34:49Z, 02:38:10Z and 02:40:50Z and each answer was 429. Then the claim has saved support.
- **Line 77:** state what was actually tried. For example: "Alaska's 2024 ranked-choice rounds — not requested; the Division of Elections' host answered 405 to its RCV page (RCV.php) on 2026-10-05 (`raw/rules/fetch_log.txt`), and no FEC 2024 House volume is served." Alternatively, request the 2024 results page now, log the answer in raw/maps/fetch_log.txt and cite that.

### 27. The register house_districts.md points to still marks the Table 1 PDFs 'cited', though us_house_prep.pl now reads them

- **Lens:** claims - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/president_returns.md:136

**The scenario.** house_districts.md's Register says the raw/returns/ and raw/apportionment/ pages are registered in president_returns.md. There, census_apportionment_table01_2010.pdf and _2020.pdf (lines 136 and 138) keep Use 'cited', but us_house_prep.pl parses both and pins them in HouseRawSources, and reading 11 says 'read from the PDFs'. The Clerk PDFs (lines 109-113) still say only 'the House recapitulation (US-5's House series)', and the FEC 2016-2022 workbooks (lines 123-126) only Table 5/7. Someone deciding whether replacing a Table 1 PDF needs a regeneration would read 'cited' and conclude it does not.

**The fix proposed.** Update those rows' Use cells to name us_house_prep.pl's reads: the Table 1 PDFs, each state's Clerk listing and recapitulation, and the FEC's '... US House Results by State' sheets.

**The skeptic's evidence.** The finding's facts all hold. The register is stale and contradicts the new record. The harm in its scenario is smaller than claimed, because the cheap bar would catch a swapped page.

- **The pointer.** house_districts.md:83 says: "The pages under `raw/returns/` (the Clerk's statistics, the FEC's workbooks) and `raw/apportionment/` (Table 1) are registered in `president_returns.md`". The new record has rows only for `raw/maps/`.
- **The stale cells.** president_returns.md:136 and :138 (`census_apportionment_table01_2010.pdf`, `_2020.pdf`) still say "cited". The diff touches only line 11 and reading 13 of that file, not the register. The register's own wording separates "READ by the tool" (for example :137, the .xls) from "cited" and "provenance", so "cited" says no tool reads the page.
- **What the tool reads.**
  - us_house_prep.pl:141 sets `my $file = "apportionment/census_apportionment_table01_$census.pdf";` and parses each row from `pdf_text($file)` into `%reps` (:143-153). `pdf_text` calls `page()`, which records the file in `%read`.
  - That list is HouseRawSources, and UsHouseDistricts.cs:19-20 lists both PDFs.
  - us_house_mutations.sh:58-62 mutates Table 1 "through the extractor's text".
  - `pdftotext -raw` on the 2010 PDF gives "Alabama 4,802,982 7 0 / TOTAL1 309,183,463 435", the lines the tool's regex matches.
  - house_districts.md:68, reading 11, says "Table 1's, read from the PDFs". The two records disagree.
- **The Clerk rows** (:109-113) name only "the House recapitulation (US-5's House series)". us_house_prep.pl:287 now reads each state's listing and its recapitulation.
- **The FEC 2016-2022 rows** (:123-126) name Tables 2, 5 and 7 only. us_house_prep.pl:572-575 reads the sheets '2016 US House Results by State', '2018 US House Results by State', '13. US House Results by State' and '8. US House Results by State'.
- **Precedent.** s788 (40583b01) updated these same Use cells when US-5 extended what was read:
  - `fec_federalelections2016.xlsx` gained "Table 7, the House by party (US-5's cross-check)";
  - `clerk_statistics2024.pdf` went from "cited by returns_2024.md" to "READ by the tool".
  
  So s790 skipped a step the repo follows.
- **What limits the harm.** `GeneratedCatalogCheck.CheckUsHouseDistricts` re-hashes every HouseRawSources page and reports "changed since generation". A replaced Table 1 PDF would therefore fail the cheap bar, not drift silently. No tool reads the register's Use column. The defect is a misleading record cell, not a data path.

**The skeptic's corrected fix.** Edit the register rows in president_returns.md. That record's "the tool" means us_returns_prep.pl, so name the second tool explicitly:
- **:136 and :138:** replace "cited" with "READ by Tools/us_house_prep.pl (US-11, house_districts.md reading 11): the Representatives per state".
- **:109-113:** append "; READ by us_house_prep.pl (US-11): each state's listing FOR UNITED STATES REPRESENTATIVE and its recapitulation, race by race".
- **:123-126:** append "; READ by us_house_prep.pl (US-11): the '<n.> US House Results by State' sheet, the winners' cross-check". For 2022, also note that the Alaska general column and its rounds are read.

A second option is for house_districts.md:83 to say in its own words what us_house_prep.pl reads from those delegated pages. Updating the cells follows the s788 precedent.

### 28. '(the full list heads the tool)' is not true, and 'each as its page shows it' overstates the Alaska check

- **Lens:** claims - **reviewer:** note - **skeptic:** minor
- **Where:** ElectionsData/usa/house_districts.md:16

**The scenario.** The tool's head (us_house_prep.pl:15-32) is shorter than the record's list. Its 'It writes' names three outputs and leaves out house_years.csv, which line 728 writes. Its 'dies on' list does not mention the Missouri note, the declared ranked-choice races, Alaska's 2022 FEC column, NC-9's ** marks or the unopposed row's mark. Also, line 22's 'the ranked-choice races are the declared ones ..., each as its page shows it' overstates for Alaska: the 'A' branch (line 525) checks nothing, and 2024 AK-AL is held to no page shape at all.

**The fix proposed.** Bring the tool's head up to the record's list (adding house_years.csv), or drop the parenthesis. State that Alaska 2022 is held only through the FEC column and 2024 only by declaration.

**The skeptic's evidence.** Both halves of the finding hold against the staged code (`git show :Tools/us_house_prep.pl`, `:ElectionsData/usa/house_districts.md`).

1. "(the full list heads the tool)" (record line 16) is false. The tool's head is the shorter of the two lists.
- Line 15 says: `# It writes ElectionsData/usa/house_districts.csv, ElectionsData/usa/house_maps.csv and Assets/Scripts/Elections/Generated/UsHouseDistricts.cs`. That is three outputs. But line 728 writes four: `write_all("$usa/house_districts.csv" => $csv_d, "$usa/house_maps.csv" => $csv_m, "$usa/house_years.csv" => $csv_y, ...)`. Lines 703 and 745 name house_years.csv as well.
- The head's "dies on" list (lines 16-32) leaves out several dies that the body has, that the record's own list names, and that the mutation suite proves:
  - line 250: "no note on Missouri's plan beside its asterisk" (mutation m06);
  - line 187: the [HH-DIV] vacancy footnote not naming the declared district (mutation d02). Line 189 (an undeclared footnote) is also missing from the head;
  - lines 443-444: the unopposed mark is not a footnote of its state, or its row is not empty and marked the same. The head says only "an unopposed seat beside another candidate";
  - lines 515 and 532: a ranked-choice line or round footnote in an undeclared race, or a declared race that was not read (mutation x04). Lines 518-522 (Maine's per-year shapes) are also missing;
  - line 631: NC-9 2018 not "**" on every general candidate (mutation x07);
  - lines 635 and 637: Alaska's 2022 FEC general column not equal to the Clerk's figures, or its rounds not read (mutation x06);
  - line 630: an FEC winner in a district the Clerk does not list.
- The head carries no condition that the record's list lacks. So the record's list, not the head, is the fuller one.

2. "the ranked-choice races are the declared ones (reading 6), each as its page shows it" (record line 22) overstates for Alaska.
- Line 512 gives the intended meaning as a check: `# ranked choice, as the Clerk prints it - declared race by race, each held to what its page shows`.
- The 'C' branch (lines 516-523) does hold each Maine year to its page.
- The 'A' branch holds nothing: line 525 is `elsif ($rk eq 'A') { $out->{flags}{A} = 1; }`.
- The page-side checks on line 515 run only `if (!$rk)`, meaning only for undeclared races.
- pdftotext -raw shows what Alaska's pages print in both 2022 and 2024: candidates plus a Write-in line, no footnote mark and no Continuing or Exhausted line. Nothing on either page ties the 'A' declaration to the page.
  - Deleting '2022 AK-AL' and '2024 AK-AL' from %ranked would fail nothing. Line 532 iterates only the declared keys, and line 515 needs a tally line or flag C.
  - A round-2 footnote on 2024 AK-AL that marks two finalists would also pass: flag C would simply sit beside A. So would a zero-figure "Continuing Ballots" line.
- 2022 AK-AL is held only through lines 632-638 (the FEC general column against the Clerk's figures, party by party). 2024 AK-AL is held to no page shape at all, only to the generic sum checks every race gets.
- Reading 6 and the BILLED section are honest about this, so the overstatement is local to line 22 and to the tool's line-512 comment.
- GeneratedCatalogCheck only re-reads the CSV rows against the C# literals, so it does not make up for this.

Re-graded to minor rather than note. Nothing in the data or behaviour changes. But this is a new committed SOURCED record making a false pointer claim, and a source comment that misstates the tool's outputs, under a claim convention that "outranks everything". Both are cheap to fix in this commit.

**The skeptic's corrected fix.** Tool head:
- Line 15: name house_years.csv among the outputs.
- Extend the dies-on list with what the body enforces:
  - the 120th tab without its Missouri note;
  - an [HH-DIV] footnote that is not the declared vacancy, or an undeclared footnote;
  - an unopposed mark that is no footnote of its state, or a row that is not empty and marked the same;
  - a ranked-choice tally line or round footnote in an undeclared race, a declared ranked race not read, or Maine's declared shape not on its page;
  - the FEC's NC-9 2018 not ** on every general candidate;
  - the FEC's Alaska 2022 general column not the Clerk's figures by party, or its rounds not read;
  - an FEC winner in a district the Clerk does not list.

Alternatively, drop "(the full list heads the tool)" from house_districts.md line 16.

Record line 22: replace "each as its page shows it" with wording that limits the page hold to Maine's 2nd. Something like: "Maine's 2nd each year held to its page (2018 the two finalists alone, 2022 its round-2 footnote, 2024 a Continuing Ballots line equal to the finalists' sum); Alaska's two are declared only - 2022 held through the FEC's general column (below), 2024 held to nothing beyond the declaration (its rounds BILLED)".

Tool line 512: make the same correction to its comment, "each held to what its page shows".

### 29. 'holds what a reader leans on (below)' points to nothing below

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/house_districts.md:10

**The scenario.** No later section describes what GeneratedCatalogCheck holds. 'What the tool proves' is the perl tool's list. CheckUsHouseDistricts holds a different set: 435 races a year, districts numbered by the maps' seats, no race's parts above its total, own lines no larger than the party's votes, the division with its vacancy, and the sums to the House rows by state. A reader will take the tool's list for the bar's.

**The fix proposed.** Name the check's holds in one sentence, or point to CheckUsHouseDistricts' summary comment.

**The skeptic's evidence.** The finding is confirmed. Nothing below line 10 of the record says what the bar holds.

**Line 10:** "`GeneratedCatalogCheck` (the cheap bar) re-hashes the pages, re-reads the CSVs figure by figure, and holds what a reader leans on (below)."

**The rest of the record never names the bar.** A search for `GeneratedCatalogCheck`, `cheap bar` or `leans on` finds only line 10. The four sections below it are:
- `## What the tool proves` (line 14), which begins "It writes nothing and dies on any mismatch ... unless all of these hold (the full list heads the tool)". This is the perl tool's list.
- `## Readings` (line 48).
- `## Not reached` (line 70).
- `## Register` (line 81).

None of them mentions the bar. The header of the generated `UsHouseDistricts.cs` (line 4, written by `us_house_prep.pl:704`) also says only that the check "re-reads the CSVs and compares every figure, and re-hashes every page listed".

**The bar's own set lives only in the check's summary.** `CheckUsHouseDistricts` in `Assets/Editor/GeneratedCatalogCheck.cs` lists it: "each election's races are 435, each state's numbered 1 to its seats that year (0 for one seat at large) by the maps' rows; no race's parts exceed its total, nor its own lines its party's votes; the winners count to the House Historian's division, the seat its footnote leaves vacant being the one race without a winner or the one winner it does not count; and the districts' own lines and totals sum, state by state, to the House rows by state (US-5)."

**That set differs from the tool's list in both directions:**
- The bar holds things the tool's list never names. One example is the code line `if (r.VotesR + r.VotesD + r.VotesOther > r.VotesTotal || r.OwnR > r.VotesR || r.OwnD > r.VotesD)`.
- The bar does not re-prove most of the tool's list. That includes the Census sentences, Table 1, the FEC winners, the page sequence and the footnote-mark reading. For those it only checks that the pages are unchanged and that the CSVs match the catalog.

So "(below)" has no clear target. A reader who follows it finds only the tool's list and may take that list for what the bar holds.

**Severity:** this is wording only. No code, data or output changes, and re-running the tool still runs every one of its own proofs. It stays a note.

**The skeptic's corrected fix.** Under the claim convention, point to where the fact lives instead of listing it. On line 10, replace "and holds what a reader leans on (below)" with "and holds what a reader of the districts leans on, as `CheckUsHouseDistricts`' summary in `Assets/Editor/GeneratedCatalogCheck.cs` names it. The list below is the tool's, which only a run of the tool proves again."

Another option follows `president_returns.md` line 12, which names its check's holds in one sentence. That sentence would be: "each election's races 435; each state's districts numbered to its seats by `house_maps.csv`; no race's parts above its total, nor its own lines above its party's votes; the winners to `[HH-DIV]` with its vacancy; and the districts' own lines and totals summed to `house_by_state.csv`, state by state." The pointer is safer, because it cannot go stale when the check changes.

### 30. The FEC is called 'the cross-check of the winners only', but the tool also checks Alaska 2022's votes and NC-9's marks

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/house_districts.md:50

**The scenario.** Reading 1 says 'the cross-check of the winners only', and the plan says 'the winners' cross-check' (USA_STAGE_PLAN.md:292 and :722). The tool also checks Alaska 2022's general column against the Clerk's first choices, party by party (us_house_prep.pl:632-638; mutation x06 proves it), and checks NC-9 2018's ** marks. The record's own 'What the tool proves' list and reading 6 say so.

**The fix proposed.** Say: the cross-check of the winners, and of Alaska 2022's first choices.

**The skeptic's evidence.** The record's claim is false in the staged code, and the same record contradicts it twice.

What the record says. house_districts.md:50 (reading 1, marked DECLARED) says: "the FEC's workbooks (2016-2022) are the cross-check of the winners only (reading 10)".

What the tool does. Tools/us_house_prep.pl also uses the FEC's vote figures, not just its winner marks:
- Lines 594-601 sum Alaska's 2022 GENERAL VOTES and its ranked-choice rounds, party by party.
- Lines 632-635 die unless the FEC's general vote per party equals the Clerk's first choices: problem("$file: Alaska's $p candidates' general vote ..., the Clerk's first choices ...") unless ($alaska_fec{gen}{$p} // -1) == ($clerk{$p} // -2).
- Lines 636-638 die if the rounds are not read.
- Lines 742-743 print the last round, which reading 6 then quotes (Democratic 137,263 to Republican 112,471).
- Mutation x06 (us_house_mutations.sh:110 and :116) changes one GENERAL VOTES cell (P59, 128553 to 128554) and expects "Alaska's D candidates' general vote 128554, the Clerk's first choices 128553". That is a check on votes, not winners.

The record says so itself in two places:
- Line 25: "Alaska's 2022 general column is the Clerk's first choices, party by party".
- Line 55 (reading 6): "the FEC's general column gives the same figures, party by party (the tool holds them)".
- The tool's own comments at :279 and :525 say "held below to the FEC's general column".

Either way you read "only", the sentence is wrong. If it means the FEC checks only winners and never votes, the Alaska check refutes it. If it means the FEC serves no other role, reading 6's FEC-sourced round figures refute it.

The NC-9 half is refuted. Line 604 reads the "**" marks from the GE WINNER INDICATOR column, and line 631 checks those marks. Mutation x07 also expects the ordinary winner mismatch "2018 NC-9: the FEC's winner D, the Clerk's none". Reading 10 (line 67) covers NC-9 as part of the winners check. So that check is a winners check and does not contradict "winners".

The plan's lines are not false. USA_STAGE_PLAN.md:292 says "as the winners' cross-check" and :722 says "US-11 (the winners' cross-check)". Neither says "only", so they are incomplete rather than wrong, which the claim convention allows.

A related gap: line 16 of the record calls the tool's header "the full list" of its checks. But the tool's header names the FEC only as "the winner cross-check" (:13), and its list of failures (:31) has only "a sheet without its headings, a district's winner by party not the Clerk's". It leaves out the Alaska general-column check, the rounds-not-read check and the NC-9 marks check.

Severity stays at note. The error understates the checks rather than overstating them, so nobody would rely on a check that doesn't exist. No data or code is affected, and the same record states the full role at lines 25 and 55.

**The skeptic's corrected fix.** At house_districts.md:50, drop "only" and name the Alaska check. For example: "the FEC's workbooks (2016-2022) are the cross-check of the winners (reading 10) and of Alaska 2022's first choices, party by party (reading 6), and no figure of theirs enters the catalog". The last part is true: alaska_fec is used only in the printf at us_house_prep.pl:742-743. Do not add NC-9; its "**" marks are part of the winners check that reading 10 already describes. The plan's lines (:292, :722) can stay as they are. For consistency with record line 16 ("the full list heads the tool"), also extend the tool's header. At line 13, change it to "the winner cross-check, and Alaska 2022's general column". At line 31, add: "Alaska's 2022 general vote by party not the Clerk's first choices, or its ranked-choice rounds not read; North Carolina's 9th of 2018 not ** on every general candidate".

### 31. S.L. 2023-145 is called 'the 2024 map' on inference, and the 118th tab is said to 'name no state'

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/house_districts.md:63

**The scenario.** ncsbe_uscongress_shapefiles_list.xml names 'SL 2023-145 US_Congress - Shapefile.zip' (last modified 2025-10-28) beside 'SL 2025-95 - Shapefile.zip' and older files, but says nothing about which election each plan served. The record (lines 63 and 74) and the register (line 98, 'the 2024 plan') state it as fact, whereas for Louisiana the record flags the same kind of inference ('rests on the Census tab's 2024 list'). Separately, the register's 118th row (line 94) says the tab 'names no state'. It names Colorado (a split block), Arkansas (corrected boundaries) and Connecticut (planning regions); it names no state whose lines changed.

**The fix proposed.** Add that S.L. 2023-145 being the 2024 plan rests on its number and the Census 2024 list. Reword the 118th Use cell to 'names no state whose lines changed'.

**The skeptic's evidence.** I could not refute either part. Both are wording in the record. No figure, CSV row or check depends on them.

PART 1: S.L. 2023-145 is called "the 2024 plan" on inference.
- The saved page `ElectionsData/usa/raw/maps/ncsbe_uscongress_shapefiles_list.xml` is an S3 bucket listing. It has only Keys, LastModified, ETags and sizes:
  - `2021-11-04 US_Congress_SL_2021-174.zip`
  - `US_CONGRESS_20220223.zip`
  - `SL 2023-145 US_Congress - Shapefile.zip` (LastModified 2025-10-28, after the 2024 election)
  - `SL 2025-95 - Shapefile.zip`
  - `US_Congress_2016.zip`
  - `US_Congress_HB_1029_3rdEdition_Shapefile_2019.zip`
- Nothing in the listing ties any plan to an election. The listing itself shows that a session law can be replaced before the next election: SL 2021-174 is followed by the 20220223 file.
- The 119th Census tab names North Carolina among the five states that changed for 2024. It names no instrument.
- So the pairing rests on the law's number plus the Census list, and the record does not say so:
  - house_districts.md:63: "2024, North Carolina: Session Law 2023-145, named by the State Board of Elections' file list ...; its text and date are BILLED"
  - :74: "(the 2024 map)"
  - :98 Use cell: "names S.L. 2023-145, the 2024 plan"
- The record does state this kind of basis elsewhere:
  - Louisiana, :61: "that the Act's map was the one used in 2024 rests on the Census tab's 2024 list, not on a saved instrument"
  - Alabama, :59: "the page says no more of either"
- By contrast, North Carolina's 2020 pairing is sourced. The saved SL 2019-249 text reads "...the Congress of the United States in 2020, the State of North Carolina shall be divided into 13...".
- The pairing is true in fact and is the only reasonable reading of the saved pages. It is display-only: `Tools/us_house_prep.pl:252-255` only checks the listing against its stored SHA-256, and house_maps.csv's NC rows cite the Census tabs. That makes it a note, not a defect.

PART 2: "names no state" for the 118th tab.
- The text of `census_rdo_congressional_districts_118th.html` names three states:
  - "Only one state--Colorado--split blocks in their 118th Congressional District plan"
  - "On December 14, 2022, Arkansas officially notified the Census Bureau that there was an error in the 118th Congressional District boundaries"
  - "the new county-equivalent planning regions in Connecticut"
- It names no state as having changed lines; it says only that boundaries are collected "from all states".
- The register (:94, "read; names no state (2022 is the geography page's)") is therefore literally wrong, though it is right in context. The same wording is in the tool's comment at `Tools/us_house_prep.pl:228` ("# the 118th's tab names no state - 2022 is the geography page's (below)"), which the finding missed.
- The generated file does not hash the tool, so the comment can be fixed without regenerating anything.

**The skeptic's corrected fix.** 1. house_districts.md:63: after "named by the State Board of Elections' file list (...)", add: "- the list names its plans without their elections, so that S.L. 2023-145 is the plan of 2024 rests on its number and the Census tab's 2024 list, not on a saved instrument;". Keep "its text and date are BILLED".
2. :74: change "(the 2024 map)" to "(the 2024 map by its number, reading 8)".
3. :98 Use cell: change it to "names S.L. 2023-145 (the 2024 plan by its number and the Census 2024 list, reading 8)".
4. :94 Use cell: change it to "read; names no state whose lines changed (2022 is the geography page's)".
5. Make the same rewording in the comment at Tools/us_house_prep.pl:228: "# the 118th's tab names no state whose lines changed - 2022 is the geography page's (below)". This is a comment only; no output bytes change and nothing needs regenerating.

### 32. Risk 9 and reading 8 say no district carries a result onto new lines; US-4 declares the opposite for ME/NE

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** docs/specs/USA_STAGE_PLAN.md:751

**The scenario.** Risk 9 now reads 'no vote data in the game can carry a result onto new lines: a district swing has no prior where the map moved', and reading 8 says 'no district is paired with its namesake'. US-4 (plan line 197) and the card's 'The districts' row (US_ELECTIONS.md:29) DECLARE the opposite for Maine and Nebraska: 'a district's previous row standing in for the next election's district of the same number across the change of lines between 2020 and 2024'. house_maps.csv confirms both states changed lines for 2022, and US-8 takes the states, ME-2 and NE-2 among them, by R-US14's methods.

**The fix proposed.** Limit the statement to US-11's House districts and name US-4's declared stand-in as the exception, or the reverse.

**The skeptic's evidence.** The finding is real, but only as a problem of wording and scope. Nothing is computed wrong and no check, figure or code path is affected. Its claim that US-4 declares "the opposite" goes too far.

Staged plan, USA_STAGE_PLAN.md:751 (risk 9), the new text: "A district's number is a label, not a place, and no vote data in the game can carry a result onto new lines: a district swing has no prior where the map moved." The old text at HEAD said: "a district swing misses where the map moved."

The plan's US-4 entry (line 197, unchanged) still DECLARES: "a district's previous row standing in for the next election's district of the same number across the change of lines between 2020 and 2024". The card, US_ELECTIONS.md:29, says: "A district's previous row stands in for the next election's district of the same number, whatever its lines ... so the 2020 → 2024 district misses carry the map as well as the swing".

The code does this. In Assets/Editor/UsStateSwingCheck.cs:170-185, `dPrev` and `dNext` are both ordered by `District` and paired by index `k`: `d2` is taken from `dPrev[k]`, swung to `d2Next` and called against `dNext[k]`. That is pairing by namesake, and it runs for 2020→2024. The staged house_maps.csv confirms the move: `2022,ME,2,1` and `2022,NE,3,1`. US-8 (plan line 252) takes "the states by R-US14", so a game started on the 2024 eve swings Maine's and Nebraska's districts from 2020's rows.

What weakens the finding: US-4 calls its row a "stand-in" and says its misses "carry the map". That concedes no true prior exists, so on substance it agrees with risk 9's first clause. Reading 8 sits in the House record, whose title limits it to House districts.

What remains:
- The new risk 9 is project-wide and categorical ("has no prior"). It reads as if the project never runs a district swing where the map moved. That drops the case the old wording covered: the built US-4 instrument, and US-8 through it.
- Reading 8's "across a change of lines no district is paired with its namesake" is unqualified. A sibling instrument pairs exactly those congressional districts for the presidential rows.

One more point that favours narrowing the sentence: raw/district holds maine_sos_president_by_town_2020.xlsx, and the by-CD workbooks list towns under each district. For Maine's presidential rows, "no vote data ... can carry a result onto new lines" is therefore true only of the game's catalogs, not of the saved pages. For the House it is true outright, because the Clerk prints nothing below the district.

**The skeptic's corrected fix.** In docs/specs/USA_STAGE_PLAN.md risk 9, replace the middle sentence with a version limited to the House that names US-4's stand-in. Add no new counts. For example:

"A district's number is a label, not a place. The Clerk prints no House vote below the district, so nothing in the game can carry a House result onto new lines: a House district swing has no prior where the map moved. The presidential districts of Maine and Nebraska are where a district swing does run across a moved map: US-4 DECLARES a district's previous row as the stand-in for its namesake across the 2022 lines, so its 2020 → 2024 district misses carry the map as well as the swing (`docs/reference/US_ELECTIONS.md`), and US-8's count inherits that through R-US14."

In ElectionsData/usa/house_districts.md reading 8, narrow the sentence the same way:

"across a change of lines no House district in this catalog is paired with its namesake (US-4's presidential rows for Maine and Nebraska are paired with theirs, DECLARED in the US card)."

Optional, not needed for consistency: the saved Maine by-town workbook could re-aggregate 2020's Maine rows onto the 2022 lines. That is a possible later improvement to US-4's stand-in.

### 33. 'How many of each party stood' is wrong for NC-9 2018

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/house_districts.md:7

**The scenario.** The 2018 NC-9 row has cands_r 0 and cands_d 0. The FEC 2018 House sheet lists Harris (R, 139,246) and McCready (D, 138,341) as general-election candidates. The column counts the candidates the Clerk printed, not those who stood, and the record and the CSV header both say 'stood'.

**The fix proposed.** Say 'printed' in the record and the CSV header, or fill NC-9's counts from the FEC with a note.

**The skeptic's evidence.** I could not refute it. The row is 0/0 and the text says "stood".

1. Staged `ElectionsData/usa/house_districts.csv`: line 7 of the header reads `# cands_r / cands_d: how many of each stood.` and line 744 is `2018,NC,9,0,0,0,0,0,0,0,0,-,N`. Staged `house_districts.md` line 7 says "how many of each party stood". The C# doc in `Tools/us_house_prep.pl` line 715 (`UsHouseDistricts.cs` line 48) says the same.

2. Where the 0/0 comes from. The Clerk's 2018 text (`pdftotext -raw`) prints only `1 9. See explanation below`, an empty recapitulation row, and footnote 1 ("On February 21, 2019, the North Carolina State Board of Elections ordered a new election..."). The tool's `$D->{unprinted}` branch sets `flags{N}` and calls `next` without pushing any candidates or setting `$out->{n}`. Lines 680-681 (`$nr = $x->{n} ? ... : scalar(grep {party eq 'R'} @{$x->{cands}})`) then give 0 and 0. So the column counts the candidates the Clerk printed, and here it printed none.

3. The tool's own input shows that candidates stood. In `raw/returns/fec_federalelections2018.xlsx`, sheet '2018 US House Results by State', rows 2987, 2991 and 2994 are Harris, Mark (R, general 139246), McCready, Dan (D, general 138341) and Scott, Jeff (LIB, general 5130), each marked `**`. Lines 604-605 collect exactly these rows into `%nc9_fec`, and line 631 requires at least 2 of them. Mutation x07 expects "'**' x2, 'W' x1", which confirms the run reads three general candidates. The record's own reading 10 says "the FEC marks each of its general candidates '**'". So the record contradicts its own line 7.

4. Why this is only a note. No code reads CandsR or CandsD: grep finds them only in the tool, the CSV, `UsHouseDistricts.cs` and the `SameRow` comparison in `GeneratedCatalogCheck`. The row carries flag N ("no votes printed, the election not certified"), and readings 5 and 10 explain it. Part two's 2018->2020 cycle cannot use North Carolina's 2018 districts under method (a) anyway, because `house_maps.csv` records NC's lines as changed for 2020. So the error is one flagged row, in wording, in a column nothing consumes yet.

**The skeptic's corrected fix.** Change the wording instead of the data. The catalog's basis is the Clerk, and filling NC-9 from the FEC would mix sources in a row that has no votes.

1. In `Tools/us_house_prep.pl`, edit the header string at line 669 to something like: "cands_r / cands_d: how many of each the Clerk lists - none where it prints no race (flag N; the FEC's 2018 sheet lists North Carolina's 9th's general candidates, reading 10)". Make the matching change to the doc string at line 715. Optionally, extend flag N's text to "no winner of record - no candidates or votes printed, the election not certified".

2. Re-run the tool. The CSV's SHA-256 (`HouseDistrictSourceDigest`) hashes the header bytes, so `house_districts.csv` and `UsHouseDistricts.cs` must be regenerated together. Then re-run `Tools/us_house_mutations.sh`.

3. Hand-edit `house_districts.md` line 7: replace "how many of each party stood" with "how many of each party the Clerk lists (none for North Carolina's 9th of 2018, reading 10)".

### 34. An edited table still marks FEC Federal Elections 2012 'not probed' (pre-existing)

- **Lens:** claims - **reviewer:** note - **skeptic:** note
- **Where:** docs/specs/USA_STAGE_PLAN.md:714

**The scenario.** The sources table this commit edits still marks 'FEC, Federal Elections 2012' 'not probed'. raw/returns/fetch_log.txt shows fec_federalelections2012.xls fetched with 200 at 2026-10-05T16:15:30Z, and us_returns_prep.pl reads it (president_returns.md register, line 122). The staleness predates this commit but sits beside the rows it edited.

**The fix proposed.** Mark it 'saved (§786)'.

**The skeptic's evidence.** Confirmed. The status is stale, but this commit did not cause it.

- Staged docs/specs/USA_STAGE_PLAN.md:714 reads `| US-3 | FEC, *Federal Elections 2012* | not probed |`. The table's header at :708 says "*not probed* means unknown".
- The volume is not unknown:
  - ElectionsData/usa/raw/returns/fetch_log.txt:14 shows `2026-10-05T16:15:30Z 200 1827840 fec_federalelections2012.xls`.
  - raw/returns/SHA256SUMS.txt:20 has its digest (`b92a2bd7...`).
  - Staged Tools/us_returns_prep.pl:398 reads it: `[2012, 'returns/fec_federalelections2012.xls', 'Table 2. Electoral &  Pop Vote', \&xls_sheet]`. Lines 439-450 also hold 2012's typed cells to Table 1 of the same workbook.
  - ElectionsData/usa/president_returns.md:122 records it as "READ by the tool: Table 2, the popular and electoral vote by state".

The case against it is weak. The header calls the status "the 2026-10-04 probe's", so taken literally the row only says the 10-04 probe skipped it. But the header also says "not probed means unknown", which is no longer true. The table is also no longer kept on the probe's terms: s788 re-stamped rows as "saved (§788 ...)", and this commit re-stamps :722 as "saved (2016 and 2020 §786, 2018 and 2022 §788 ...)" and :723 as "saved (§790 ...)". A reader will take "not probed" as current. That makes it a stale TRACKING claim under CLAUDE.md's convention: the work moved and the row did not.

It is older than this commit. `git show` at cba1bd04 (s777, the plan), 947921ab (s786, which actually fetched the file), 40583b01 and 14ee39b6 all carry the same row. The staged diff only touches rows 722-723 and the BILLED paragraph, not 714.

Other US-3 rows have the same gap, because §786 re-stamped none of them. The 2016/2020 FEC row still says "200" though both files are saved. The Maine/Nebraska row still says "else BILLED", but raw/district/ holds the Nebraska SoS canvass books for 2012-2024 and Wayback copies of §§32-710 and 32-1038, all fetched with 200.

No code, generated output or check reads this status column. It is documentation only and sits outside the rows this commit edited, so it stays a note.

**The skeptic's corrected fix.** Optional, and outside this commit's scope.

- Re-stamp line 714 as `saved (§786; ElectionsData/usa/raw/returns/)`, in the same form as the rows this commit edits.
- If the rest of the table is tidied too, also re-stamp:
  - the FEC 2016/2020 row: `saved (§786)`;
  - the 2024 FEC row: `200 ...; saved (§786)`;
  - the Maine/Nebraska row: `saved (§786; raw/district/ — the SoS canvass books and Wayback copies of the statutes)`.
- Alternatively, reword the header at :708: a row shows the 2026-10-04 probe's status until a § saves it, and then names that §.
- Reference by § and path only. No figures or line numbers, per the claim convention.

## The first pass - refuted by the skeptics

- [reader] Footnote kind is not checked on district-number or unopposed marks, and a footnote number seen twice in a state silently overwrites - *The code facts are accurate, but no failing path exists on the inputs the tool is pinned to, and the example misread is already rejected by two checks. File: G:/UNITY/Projects/PoliSim/Tools/us_house_prep.pl (staged = working tree).

1. Code facts confirmed:
- Lines 423-426 do not test `$D->{unprinted}` or the footnote's kind.
- Line 443 tests only `defined $foot_of{$unopp[0]{unopposed}}`.
- Line 417 `map { $_->{n} => $_->{text} }` lets a later number overwrite an earlier one.
- Line 496 rejects unknown kinds on the figure path only.

2. Storing the note does nothing. `$out->{note}` is set at 425, 450, 494 and 495, and `$out->{finalists}` at 503, but neither is ever read again. No CSV column (664-686), C# field (702-726) or report line carries them. A mark changes the outcome only through the figure-mark path (486-497).

3. The scenario needs Clerk text that does not exist. `pdf_text` (72-79) calls `page()` (59-71), which dies on any byte that differs from SHA256SUMS.txt. I ran an instrumented copy of the tool over a copy of the inputs in the scratchpad. It exited 0, and its four outputs match the staged files byte for byte. It listed every footnote the reader takes in the 50 states:
- There are 20 across the five volumes, and no number repeats within a state (LA 2016 has 1/2/3, MS 2018 1/2, GA 2020 1/2, every other state one).
- The only district-number mark is 2018 NC-9, the unprinted race ("ordered a new election").
- All 11 unopposed marks cite the FL, OK or LA rule that "the names of those with no opposition are not printed on the ballot".
- Figure marks appear only on 2016 LA-3, LA-4 and OH-8, 2020 LA-5 and 2022 ME-2.

4. The worked example is caught. With 2020 LA-5 printed unmarked, 50,812+32,186+9,432+7,136+49,183+30,124+23,887+22,496+9,834 = 235,090, which equals the row Total. The empty reading is then the only fit, and the pool elects Christophe (D). That flips a seat:
- Line 546 rejects it ([HH-DIV] 2020: R 212, D 222 after the NY-22 decrement at 544).
- Line 626 rejects it too (the FEC's 2020 winner, Letlow, is R).
- In 2024, line 546 still holds the 119th Congress at R 220 / D 215.

So any change of party is caught in every year, unless a second error cancels it.

5. The only variant no check catches is a same-party runoff in that made-up format, and there the winner's party cannot change. If 2016 LA-3 and LA-4 were printed unmarked, they would still elect Higgins (77,671) and Johnson (87,370): each December figure is above every November figure printed beside it. The only output change would be the missing L and M flags.

Re-graded as a hardening note, not a defect.*
- [checks] The 2022 one-seat comparison mixes code order with name order - *The reviewer is right about the code, but the failure it describes cannot happen. Line 238 compares `join(' ', sort @ex)`, which is sorted by state code, with `join(' ', @one)`, which keeps @order's state-name order (line 134: `my @order = map { $code{$_} } sort keys %code;`). The two orders do differ in general. Nothing can reach that difference, for four reasons:

(1) The check is fixed to one election and one census. Line 237 is `my @one = grep { seats(2022, $_) == 1 } @order;` and census_of(2022) returns 2020 (line 158: for 2010, `2022 < 2022` is false; for 2020, `2022 <= 2022 && 2022 < 2032` is true). The set compared is always the 2020 apportionment's one-seat states.

(2) That set comes from a pinned page. Running pdftotext -raw on raw/apportionment/census_apportionment_table01_2020.pdf gives seats of 1 only for Alaska, Delaware, North Dakota, South Dakota, Vermont and Wyoming. Montana, New Hampshire and Rhode Island have 2, among others. The PDF is read through pdf_text -> page(), which dies on any digest change before parsing (lines 67-68: `die "$f: SHA-256 $d, its SHA256SUMS.txt holds $want\n" unless $d eq $want;`). A one-seat set like {Nevada, New Hampshire} can only exist if a pinned historical table is edited and re-pinned. Even then the 435 total and the change column (lines 155 and 157) would have to stay consistent, and the geography page's 2022 sentence would have to name the same states.

(3) Both orders agree today. The page sentence is "with the exception of the six single-member states (Alaska, Delaware, North Dakota, South Dakota, Vermont, and Wyoming)". I ran the tool's own %code and @order out of tree: sort @ex = 'AK DE ND SD VT WY' and @one = 'AK DE ND SD VT WY', so they are equal. Adding each of the 2020 two-seat states (HI ID ME MT NH RI WV) to that set breaks the agreement only for NH. NH has 2 seats in the pinned table.

(4) No mutation case reaches the gap. m03 removes Wyoming, which makes a real set difference. Its expected text, "the exceptions AK DE ND SD VT are not the states the 2020 census gives one seat", is only the sorted @ex half and never depends on @one's order. a01 changes 2010 Alabama, which does not feed 2022. a02 changes only Texas's change column, not its seats. The staged house_maps.csv carries the 2022 rows (AK 0 and the others 1), which shows the control run passed this line.

The reviewer's other claim also holds: string equality of the joined code lists implies the same multiset of codes, so the check can never pass wrongly. The result is a harmless mismatch of orderings on inputs that are fixed by digest and refer to historical facts. It has no reachable failing path.*
- [checks] The FEC winner fold hides a second W on another candidate's row - *The behaviour described is accurate. Lines 607-614 record only which parties carry a W in a district:
  next unless $w =~ /^W\*?$/; ... next if $cd =~ /UNEXPIRED/i; ... next if $p eq 'Combined Parties:';
  $fw{"$st-" . ($n + 0)}{defined $major ? ($major =~ /^R/ ? 'R' : 'D') : 'O'} = 1;
Line 623 then turns {R, O} into 'R':
  my $fwin = $f{R} && $f{D} ? 'both R and D' : $f{R} ? 'R' : $f{D} ? 'D' : $f{O} ? 'O' : 'none';
No candidate identity is checked anywhere.

Why there is no failing path:
(1) The scenario never occurs in the tool's inputs. The tool reads a fixed list of sheets (line 573), each held to its digest by page() (lines 59-71; returns/SHA256SUMS.txt lines 21-24). A read-only probe using the tool's own xlsx_sheet and the same skips (UNEXPIRED, Combined Parties:) found:
  - districts with several kept W rows: 27/25/25/22 in 2016/2018/2020/2022;
  - districts whose kept W rows carry more than one FEC ID: 0/0/0/0;
  - W rows with a blank FEC ID: 0.
  The only districts with two IDs among all W rows are 2018 MI-13 (Tlaib H8MI13250, Jones H8MI13243 on '13-UNEXPIRED TERM'), PA-7 and PA-15. In each, the second ID is on an unexpired-term row, which line 609 skips. The one real W on a Libertarian row, 2020 NY-2 row 2250, is Garbarino's own fusion line (H0NY02234, the same ID as his R/CRV/SAM rows). The tolerance for minor-party lines is needed and correct.
(2) No output depends on the FEC sheets. The written rows take $x->{winner} (line 683), set from the Clerk's reading (lines 448 and 502). The winners are held to [HH-DIV] separately (lines 538-543). The FEC data reaches only the stdout printf at lines 742-743. A stray W could weaken the cross-check but could never change a written byte.
(3) The check does what it promises. The header (line 31) and house_districts.md reading 10 both define it as the winner by party: "takes the winner's party as the Republican or Democratic line among his marked lines". The mutation suite proves only the declared failure paths, and "two winners in one district" is not one of them.

This is accurate as a description of a blind spot, and the fix would pass today. It is optional hardening, not a defect in this commit.*
- [catalog] The 435, division and House-sum clauses run only over HouseRecord's years; the success line still claims the sums for a year with no House rows - *The finding describes the loops correctly. In GeneratedCatalogCheck.cs, the 435-race count (747), the district numbering (748-755), the division (757-763) and the House-sum clause (765-770) all run inside `foreach (var y in UsPresidentialReturns.HouseRecord)` (724). No C# line compares the year sets. But none of the failure routes it gives can happen. I tested the generators in a scratch copy of ElectionsData/usa plus both tools; the repo was not touched. Unmutated, both tools rebuild house_by_state.csv, UsPresidentialReturns.cs, the three House CSVs and UsHouseDistricts.cs byte for byte.

1. Deleting the 2024 rows from UsPresidentialReturns.House: the same GeneratedCatalogCheck run fails at line 587, `if (house.Count != UsPresidentialReturns.House.Length)` (250 CSV rows against 200). CheckUsPresidentialReturns (line 125) and CheckUsHouseDistricts (126) are summed into one `failures`, and the check is in the cheap bar (CheckSuite.cs:275). Deleting the rows from the CSV as well fails ReadUsCsv's digest test (801) unless someone forges HouseSourceDigest. 'RESULT 0' is CheckUsHouseDistricts run alone, not the bar.

2. 'us_returns_prep.pl re-run with fewer House years' dies and writes nothing:
   - 2024 dropped from @house_years (line 800): 'returns_2024.md: the House's Republican typed 74390864, the Clerk's Total row unread', 3 mismatches.
   - 2016, 2018 or 2022 dropped: 'House <year>: the Clerk's and the FEC's tables agree exactly on 0 of 50 states, under the 35' (line 879). The FEC list at 849-850 is fixed to 2016-2022.

3. A race year with no record row: us_house_prep.pl writes HouseDistricts (line 673) and HouseRecord (700) from the same `for my $year (@years)` in one run. Each CSV is held to the catalog row for row and by digest. Mutated:
   - @years without 2024 dies: '2024 AK-AL: declared ranked, no such race read' and 'house_by_state.csv: 200 state-years held, not 250' (565).
   - house_by_state.csv without its 2024 rows dies: '200 state-years held, not 250'.

4. The 2026 rows of HouseMaps: the tool builds them from the fixed 50-state @order (134, 695), with seats from Table 1, which it holds to 435 over 50 states (155). The success line (783-786) claims only that the map rows match the CSV, not their structure.

In the current tree the year sets agree: house_by_state 50 rows a year for 2016-2024, house_districts 435 a year for 2016-2024, house_years one row a year for 2016-2024, house_maps 50 a year for 2016-2026. So every clause compares every year.

Only one route remains, and it is a future scope change. Suppose someone adds a House year to us_house_prep.pl's @years (line 43) but not to us_returns_prep.pl's @house_years. That needs new Clerk pages and their SHA256SUMS lines plus a wider HH-DIV range (115..119). The generator would still pass, because `$n == 250` (565) is hard-coded and counts only the state-years that have races. The House-sum loop (765) would then make no comparison for the new year, while the success line still claims the sums. This is the same standard the s786 review applied to the presidential catalog's years ('no failing path exists without rewriting the generator'): a reviewed scope change, not a defect in this commit.*
- [catalog] The catalog's own vote columns give 2022 as R 223 / D 212; only the Winner column gives [HH-DIV] - *The facts in the finding check out. The design is deliberate, the exception is already named, and nothing in this commit can take the failing path.

1. The row is as quoted. house_districts.csv:1329 reads `2022,AK,0,129379,128553,4570,263610,129379,128553,2,1,D,A`. The Clerk's 2022 text gives Palin 67,866, Begich 61,513 and Peltola 128,553, and its recapitulation row is `At large ... 129,379 128,553 4,570 1,108 263,610`. Across all five years, R-vs-D plurality on the vote columns differs from the Winner column in only one race that has votes: 2022 AK-AL. Every other difference is a 0-0 row.

2. The finding's "R 223 / D 212" is not what the bare columns give. On 2022 they give R 221, D 212 and two 0-0 rows: 2022,FL,5 and 2022,LA,4, both flag U, unopposed with no votes printed. The 223 only comes out once those two rows are settled by cands or Winner. U rows occur every year (2, 4, 1, 2 and 2; 11 in all). So the vote columns alone never give the House in any year, and any part-two consumer has to read Winner and the flags anyway. Winner is meant to be the seat column; this is not a 2022 anomaly.

3. Winner is the plurality the done criterion asks for, computed on the record's own votes. In us_house_prep.pl, `my @pool = @final ? @final : @cands; my @rank = sort { $b->{total} <=> $a->{total} } @pool; ... $out->{winner} = $rank[0]{party};` takes the candidates' totals (Peltola leads) and not the party sums. The run then dies unless the count matches [HH-DIV]: `problem("House $year: the districts give R $r D $d other $o, [HH-DIV] ...") unless $r == $h->{R} && $d == $h->{D} && $o == $h->{other};`. The re-read, GeneratedCatalogCheck.CheckUsHouseDistricts, counts `r.Winner` against `y.SeatsR`/`y.SeatsD`.

4. The exception is named, both as data and in the record. The plan asks for "the ranked-choice races as the Clerk prints them (... Alaska's first choices)", and flag A ("Alaska's ranked-choice count printed at its first choices") is that data. house_districts.md reading 5 says the winner is "Not the order printed ... nor the recapitulation (a party column adds a same-party race's two candidates together, and Alaska's two Republicans in 2022 outpoll the Democrat who won)". Reading 6 says "the Clerk's first choices put the two Republicans together ahead ... In both years the candidates' plurality on the first choices names the winner of record." The run's pasted output prints the FEC's round 3 beside the Clerk's first choices. Flag A on both Alaska rows is correct: both were printed at first choices. A consumer that uses Winner for flag-A rows is right in both years.

5. Nothing consumes the vote columns for seats today. A grep of Assets for HouseDistricts finds only the generated part, UsPresidentialReturns.cs and the re-read. The seat methods and R-US18 are part two and are not in this commit. The miscount the finding describes belongs to future code that does not exist, and that code would have to handle the U rows anyway.

What is left is a documentation nicety, not a defect. The HouseDistricts summary in UsHouseDistricts.cs:47-49 does not say that Winner is the seat of record, or that the party columns are not the deciding count where a row is flagged A, L or U.*
- [catalog] The partial class makes UsPresidentialReturns ambiguous to DocumentClaimCheck - *The mechanism is real, but nothing fails, and the check behaves as it was designed to.

1. The two declarations are indexed as two files. At HEAD the type was `public static class UsPresidentialReturns` (UsPresidentialReturns.cs:13). In the staged change it is `public static partial class UsPresidentialReturns` in both UsPresidentialReturns.cs:14 and UsHouseDistricts.cs:10. The TypeDeclaration regex allows `partial`, so typeFiles["UsPresidentialReturns"] holds 2 paths. DocumentClaimCheck.cs:160 (not 152 as cited) then runs `if (declaring.Count > 1) { ambiguous++; continue; }`.

2. No live claim is affected.
   - The check reads only root *.md files (line 124, `SearchOption.TopDirectoryOnly`) and drops CLAUDE.md and COMPLETED.md as historical (line 133).
   - That leaves three live root documents: CLAUDE_DESIGN_ASSET_REQUEST.md, ERRANDS.md and POLISIM_FEATURE_LIST.md. Running `grep -c UsPresidentialReturns` on them gives 0 for each.
   - The only backticked reference in any root document is COMPLETED.md:37527, `UsPresidentialReturns.House`, which is excluded.
   - The staged change touches no root .md file (`git status` shows none).
   - The US documents this change edits (docs/specs/USA_STAGE_PLAN.md, docs/reference/US_ELECTIONS.md, ElectionsData/usa/*.md) are outside the check's scope regardless.
   - So candidates, ambiguous, MEMBER GONE and WRONG OWNER are identical before and after the change. No verification that used to run is lost.

3. Skipping such types is documented and visible. The class summary says: "A type declared MORE than once is skipped and counted: the reference is ambiguous, and guessing which file was meant would be the check inventing the claim it is supposed to verify." The enumeration line reports the count, and no ratchet applies to it; RatchetLedger reports only MEMBER_GONE and WRONG_OWNER.

4. This limitation already exists elsewhere. GameController is declared partial in 30 files under Assets, and POLISIM_FEATURE_LIST.md:155's `GameController.CanvasSelectorActive` is already skipped the same way.

5. The skip also prevents a wrong result. If the check resolved only `declaring[0]`, a member that lives in the other partial file would be falsely reported as MEMBER GONE.

The finding itself says "nothing is lost yet" and "None needed now". There is no failing path.*
- [catalog] The generated file cites COMPLETED.md s790, which is not yet in the staged COMPLETED.md - *The facts are right, but nothing here is a defect. Every change in this repo looks like this at review time.

Confirmed:
- In the staged tree, UsHouseDistricts.cs:8 reads `// PS-6 US-11 (COMPLETED.md s790): the House of Representatives by district, ...`. §790 is also cited at GeneratedCatalogCheck.cs:126 and :668, at house_districts.md:3, and at USA_STAGE_PLAN.md:219, 258, 292, 723, 735 and 751. The plan's citation is on line 292; line 291 is the US-11 heading.
- COMPLETED.md is neither staged nor modified. Its last section is "## 789." at line 37541, the same at HEAD, in the index and in the working tree.

Why it is not a defect:
1. The record is written after the review on purpose, because it records the review. §789's own section, at COMPLETED.md's tail, ends with a "**The review** (`Reviews/2026-10-06_s789_...`)" paragraph and a "**Bars**" paragraph. A section like that cannot exist before its review and bars have run.
2. The §790 record is already drafted. The session scratchpad holds rec790.md (written 06:00), headed "## 790. PS-6 US-11, PART ONE: THE HOUSE BY DISTRICT, 2016-2024, ...". It ends with the placeholders `REVIEW790` and `BARS790`. The commit message draft msg790.txt likewise ends with `REVIEWLINE` and `BARSLINE`.
3. Every pass commit lands with its own section. CLAUDE.md:67 says "RECORDS: ONE COMMIT PER PASS". 14ee39b6 (s789) and 40583b01 (s788) each add COMPLETED.md and their Reviews/ report in the same commit. I checked the last 60 commits whose subject starts with sN (s738 to s789): each has "## N." in COMPLETED.md at that commit, and none is missing.
4. This same finding was refuted for §788 (s788 review, :1135) and for §789 (s789 review, :1197-1208).
5. Nothing can fail because the section is missing. No check compares § pointers with COMPLETED.md headings: DocumentClaimCheck.cs:84 and ResidueCheck.cs:27 only exempt the file, and PlaySheetGenerator reads only §346. The staged GeneratedCatalogCheck.cs has no reference to COMPLETED at all, and .git/hooks holds only samples. The pointer at UsHouseDistricts.cs:8 is a comment.

The only risk is that the commit is made without the record, and the workflow already prevents that.*
- [claims] The §790 pointers have no record behind them: COMPLETED.md is not staged - *The facts are right, but they describe how every change looks while it is being reviewed. Nothing in the staged change is wrong.

Confirmed:
- COMPLETED.md is the same at HEAD, in the index and in the working tree (`git diff --quiet` and `git diff --cached --quiet` both clean). Its last section is "## 789." at line 37541. There is no §790 anywhere in it.
- The staged diff adds exactly 14 "§790"/"s790" citations across 7 files: USA_STAGE_PLAN.md (6, at lines 219, 258, 292, 723, 735 and 751), GeneratedCatalogCheck.cs (2), us_house_prep.pl (2), UsHouseDistricts.cs (1), us_house_mutations.sh (1), house_districts.md:3 (1) and president_returns.md:58, reading 13 (1).
- Commits 14ee39b6, 40583b01, 43169a7a, 947921ab, 5fe3c031, 1b8ba4ed, 7d71fcc4, b183a332, 99fdcf1c and cd1fb480 each add COMPLETED.md.

Why it is not a defect:
1. The record is written after the review on purpose, because it records the review. §789's committed section contains "**The review** (`Reviews/2026-10-06_s789_...`: the workflow `polisim-staged-review` ...): no defect; 12 minor, 17 notes, all acted on." and a "**Bars** (tier TOOLING, on this commit's own tree)" paragraph. A section like that cannot exist before its review and bars have run. The rule is CLAUDE.md:67, "RECORDS: ONE COMMIT PER PASS": the record goes in with the commit, not with the staging that gets reviewed. 14ee39b6 and 40583b01 each committed COMPLETED.md together with their Reviews/ report.
2. §790 is already drafted. The session scratchpad holds rec790.md (06:00, after HEAD's 04:14), headed "## 790. PS-6 US-11, PART ONE: THE HOUSE BY DISTRICT, 2016-2024 ...". It covers what every staged pointer refers to: reading 13, us_house_mutations, GeneratedCatalogCheck, USA_STAGE_PLAN, risk 9, BILLED, UsHouseDistricts, house_maps, us_returns_prep and the partial class. It ends with the placeholders "REVIEW790" and "BARS790", waiting for this review and the bars. §787, §788 and §789 were drafted the same way (rec787.md to rec789.md).
3. Earlier reviews rejected this same finding. Reviews/2026-10-06_s789_us5_national_vote.md:1197: "The facts are right, but they describe the normal state of every change under review. Nothing in this change is wrong." Reviews/2026-10-06_s788_us5_house_returns.md:1135: "Facts confirmed but no defect: this is how every change looks at review time". That review also checked the last 400 sN/§N commits and found every one with its "## N." section. The s786 and s787 reviews say the same.
4. Nothing can fail because of it. No check matches § pointers against COMPLETED.md headings. DocumentClaimCheck.cs:84 and ResidueCheck.cs:27 only exempt the file, and PlaySheetGenerator.cs:47 reads only §346. The pointers sit in comments and document text.

The finding's scenario, "committed as staged", is a commit made without the record. The workflow does not commit that way, and nothing in this change causes it to. The fix it asks for is the commit step that already follows.*

## What was done about the first pass

One defect survived the skeptics (1), with 17 minor findings and 16 notes; every one was acted on, and five of the eight the skeptics refuted were acted on as well. The tool was rewritten once, its suite grown from 28 cases to 53, and the record rewritten from the run.

- **Washington's "GOP" (1).** The defect: the Clerk prints 2018 WA-8's Republican as "Dino Rossi, GOP", which the reader filed as other - the race read as uncontested by Republicans, which part two's method (a) would have scored as an easy hold. Now every label naming a major party is declared, each to a class: "GOP" Republican, the write-in candidates' "Write-in (Republican)" and "Write-in (Democratic)" other; a new form stops the run. US-5's own line stays the recapitulation's own Republican column, so Rossi's votes are `votes_r` but not `own_r`, and the US-5 tie holds. One row of the catalog changed. Reading 3 names the race. Cases x09 (the GOP declaration withdrawn).
- **The figure order (2).** A transposition within a race passed every check. The tool now reads each volume twice: `pdftotext -table` sets each entry's label and figure on one line by where they stand on the page, and the two readings must agree, state by state, entry for entry - they do, in every state of every year. Case c10 transposes two figures in the `-raw` reading alone.
- **The fusion labels (3).** A bare label in a fusion state was always a line of the candidate above. Now only the twenty labels the volumes print are lines; any other stops the run. Case x10.
- **The footnote kinds (refuted, acted on).** A mark on a district's number is allowed only on the race printed without votes, its footnote ordering a new election; an unopposed seat's footnote must be a no-opposition law; a footnote number twice in a state stops the run. Cases c14, c15, c16.
- **The FEC's exception and marks (5; the refuted fold finding, acted on).** Indiana's 2nd in 2022 is now held to what it says - no winner marked on the full term, the Clerk's on the unexpired term - and a district's W marks must be one FEC id. Cases x17 (the reverse), x18.
- **Ranked choice (6).** Each declared race is tied to its evidence: 2016-2022 to the FEC's footnote giving the general election's later rounds, Maine's figures equal to the Clerk's; 2024 Maine to its Continuing Ballots line; an Alaska race only in Alaska, a Maine one only in Maine; a footnote giving rounds on an undeclared race stops the run (Maine's 2nd in 2020, whose footnote gives a primary's rounds, is correctly undeclared). Alaska's 2024 declaration rests on no saved page, and the record says so. Cases x11, x12, x13.
- **The deciding count (15; the refuted vote-columns finding, acted on).** The catalog gains `final_r` and `final_d`: the candidates' figures, but Louisiana's runoff finalists alone and Alaska 2022's FEC last round. The tool and `GeneratedCatalogCheck` require it to name every winner (the vacant seat's included), and a race printed without votes to carry none. The merged R-US18 spec needs these columns.
- **The geography page's 117th (7)** is compared with the 117th tab, not with a typed name, and the 2022 one-seat comparison sorts both lists (the refuted order finding, acted on). Case m07, with m08 and m09 for the page's 2022 sentence.
- **The state-year tie (8)** counts each of the 250 once. Case x08 (a copy of one state-year in place of another).
- **The mutation suite (9-12).** c04 anchored on New York's 4th, c06 on Louisiana's footnote 2; the control's comment corrected; cases added for every refusal the record names - the tie (c11), two readings closing a listing (c12), an ungrouped figure (c13), the vacant seat's winner (c17), the House Historian's row and an undeclared footnote (d03, d04), a perl warning (x15), a non-ASCII output (x16), the winner column's heading (x19), an unused unnamed declaration (x14). 53 of 53 caught.
- **The catalog check (13, 15; the refuted year-set finding, acted on).** It now requires the races' years to be the record's, each maps year the 50 states with 435 seats, 50 House rows an election, and every winner leading its race's deciding count.
- **The unwired ratchet (14).** `UnwiredSubsystemCheck` no longer counts a file that declares the same type as a mention of it, so the parts of a partial type cannot reach each other; the counts do not move (2 of 180, 2 of 288) - the House part is reached, like the presidential one, by its class's name in `ElectoralCollege.cs`, which the record states is itself unreached until US-8.
- **The glosses (16-20).** `cands_r` / `cands_d` are the Clerk's printed candidates; flag C says the finalists at their last round with 2022's eliminated candidate beside them; `vacant` is the seat the footnote leaves unassigned; the maps' source is the saved page under `ElectionsData/usa/` and the C# summary carries the 2026 caveat; the tool's head lists its four outputs and every refusal.
- **The plan (21, 23, 24, 25, 32, 34).** US-8's Needs restored - the 119th's opening roll call already holds its delegations, and the presidency does not wait on the districts; risk 14 rewritten (the Clerk's PDFs are read through pdftotext, their fonts Type1); "uncontested seats flagged" replaced by what the catalog carries; risk 9 points to `house_maps.csv` instead of copying its counts, and names US-4's declared exception for Maine's and Nebraska's districts; the FEC's 2012 volume marked saved.
- **The records (4, 22, 26-31, 33).** Maine 2022's mixed rounds worded the same way in reading 13, the plan, both generators and flag C; the "Not reached" section limited to what the logs record; the register's Use cells name the House tool's reads; the record names what the catalog check holds; the FEC's cross-check is of the winners, the ranked-choice counts and Alaska 2022; S.L. 2023-145's year rests on its number and the Census tab; the 118th tab "names no state whose lines changed"; reading 3 states the fusion metric as the run computes it.
- **Not acted on:** the refuted DocumentClaimCheck finding (the partial class makes `UsPresidentialReturns` ambiguous to `DocumentClaimCheck`, which reads only root documents and no claim there names the type) - no claim is lost; and the two refuted pointer findings (the §790 citations) are met by this commit's record.

**Found while acting on the review:** the widened tuple pushed the partial class's single static initializer past what Mono's JIT can compile - the first touch of `UsPresidentialReturns` threw a StackOverflowException (the 13-column version had passed). The generator now builds the races an election a method and joins them.
