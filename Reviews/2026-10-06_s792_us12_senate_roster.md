# Review - §792, PS-6 US-12 part one: the Senate by state and by date, the 118th and 119th Congresses (2026-10-06)

A workflow review (`polisim-staged-review`) of the change that builds US-12's roster half: the new raw group `raw/senate` (the Congressional Directory of 2024 and 2026, the Senate's lists and its 50 state pages through the Internet Archive, the Senate Democrats' member lists); `Tools/us_senate_prep.pl`, which builds every seat's holders and their days from them; the catalog (`senate_seats.csv`, `senate_changes.csv`, `senate_on.csv`, `senate_division.csv`, `UsSenateRecord.cs`); `Tools/us_senate_mutations.sh`; `GeneratedCatalogCheck.CheckUsSenate`, the roster diagnostic; the record `senate_record.md`; `records_by_date.md`; the plan's US-12, R-US10 and two sources rows. Not required by the tier (no money path); run because the roster is the base US-15 seats and R-US10's reading rests on it. Every report below is verbatim; what was done follows.

## The first pass - confirmed (verbatim)

One pass over the staged tree, 35 agents: four lenses (the tool against its sources, the C# check and the generated part, the record and the plan and the register, the refusals and the mutation suite), every finding put to a refute-first skeptic.

### 1. The Third or Minor Parties check never compares party, though the header says it refuses on a party mismatch

- **Lens:** tool-vs-sources - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_prep.pl:289

**The scenario.** Lines 25-27 of the header say the tool dies when the Third or Minor Parties page names someone an independent in the window 'whose party here is not independent on those days'. The loop at lines 281-290 only checks the start year (substr($first,0,4) == $from). It never checks that $x->{party} is I or D/I, and it never checks the end year. I proved this on a scratch copy: I added a row 'Peter Welch (VT) Independent 2023-present' to the Third/Minor page and re-pinned its digest. The run exited 0 and wrote every output, with Welch still D. The mutation suite has no case for this path. Today the class pages and Directory counts would catch a real mislabel on the state page, but the promised cross-check against this page does not exist.

**The fix proposed.** Inside the loop, add: problem(...) unless $x->{party} eq 'I' || $x->{party} eq 'D/I'. Also hold $to against substr($x->{left},0,4) when $to ne 'present'. Add a mutation case (a D senator named Independent) to us_senate_mutations.sh.

**The skeptic's evidence.** Tools/us_senate_prep.pl, header lines 26-27: it promises to die on "a senator the Third or Minor Parties page names an independent in the window whose party here is not independent on those days, or the reverse". The loop that does this (about lines 279-292) runs, for each row of the page: `my $x = find_holder(...)`, then `$named{$x} = 1;`, then `my $first = $x->{party} eq 'D/I' ? $x->{i_from} : $x->{took};`, then `problem(...) unless substr($first, 0, 4) == $from || ($from < 2023 && $first le $window);`. Nothing in that loop reads $x->{party} against 'I' or 'D/I', and the end year `$to` is used only to skip rows that ended before 2023. If the page names a D holder who took his seat in the year it gives, that holder passes and keeps D. Welch (VT, D, took 2023-01-03) with "Independent 2023-present" is such a case. Only "the reverse" is checked: `problem("...an independent here, not on the Third or Minor Parties page") if ($x->{party} eq 'I' || $x->{party} eq 'D/I') && !$named{$x}`. The mutation suite has one case on this page, c03 (King removed), which tests only the reverse. So the forward check the header promises does not exist. I grade it minor, not defect, because the catalog cannot come out wrong through this gap alone. The class-page anchor compares each senator's party on the page's date (`problem(... unless ($roster{$k} // '') eq $page{$k})`), the Directory checks both its D/R/I counts and each Class listing's party per entry, and the Changed Parties block plus the D/I label check pin Sinema and Manchin. A real mislabel on a state page would trip those anchors. The record's own list (senate_record.md:99, "an independent off the Third or Minor Parties page") claims only the direction that is implemented. Only the tool's header comment overclaims.

**The skeptic's corrected fix.** Inside the Third or Minor Parties loop, after find_holder, add `problem("Third or Minor Parties: $name ($st) independent from $from, here $x->{party}") unless $x->{party} eq 'I' || $x->{party} eq 'D/I';`. When $to ne 'present', also check that $x->{left} ne '-' and substr($x->{left},0,4) == $to (for a D/I holder, the independent stretch ends at left). Add a mutation case to Tools/us_senate_mutations.sh that adds a D senator, for example Welch, as an Independent row on that page, re-pins the digest, and expects the new message. The other option is to cut the header's claim down to the reverse direction plus the start year, which is all the code checks today.

### 2. 'Took' is described as the oath day, but before the window it is the state page's began day, which can be the appointment day

- **Lens:** tool-vs-sources - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_prep.pl:543

**The scenario.** The generated UsSenateRecord.cs summary (line 543 of the tool) calls Took 'the day of his oath'. senate_record.md says the same: 'What the catalog holds: ... the day of his oath'. For senators already seated before 3 Jan 2023, line 213 uses the state page's TERM BEGAN instead. Padilla is a checkable case: senate_seats.csv has took 2021-01-18, his appointment day. The saved New Senators page (117th section) says 'Appointed January 20, 2021', and the Appointed page says sworn Jan 20, 2021. The CSV header states the exception correctly ('else the day his state's page gives'). The C# summary and the record's prose do not. No roster figure in the window changes.

**The fix proposed.** Change the generated summary and the record's catalog line to 'the day of his oath for a senator new in the 118th/119th Congress, else his state page's began day'. This matches the CSV header.

**The skeptic's evidence.** The finding holds. In Tools/us_senate_prep.pl the New Senators loop only reads the 118th and 119th sections (`next unless defined $congress && ($congress == 118 || $congress == 119)`). After it, `$x->{took} = $x->{sworn} // $x->{began};` sets Took. So a senator seated before 3 Jan 2023 gets his state page's TERM BEGAN day, whatever that day stands for.

Padilla shows it in the staged files:
- senate_seats.csv has `CA,3,Alex Padilla,D,-,2021-01-18,-,before the window,...`.
- The saved New Senators page (wayback_senate_NewSenators_20260906053851.html, 117th section) reads "Alex Padilla D-CA Appointed January 20, 2021". That is the day he was sworn.
- California's state page reads "Harris (D) Jan. 3, 2017 Jan. 18, 2021 ... Resigned ... Alex Padilla (D) Jan. 18, 2021 Present Appointed, then elected". Took is this began day, which is also Harris's last day. It is not his oath.

Three places describe the field differently:
- The CSV header gets it right: "took: the day of his oath by the New Senators page for a senator new in the 118th or 119th Congress, else the day his state's page gives (before the window)".
- The generated C# summary (tool line ~543) calls it "the day of his oath (Took)" with no exception. It only points to the CSV header in a closing parenthesis.
- senate_record.md line 6 says "the day of his oath" with no exception. Its reading 1 says a seat is held from the oath, and the same reading notes that the state pages' "term began" days are mixed (some appointments, some oaths).

No roster figure in the window changes, because Took only counts on days on or after 3 Jan 2023 and every row seated before the window is already serving then. So this is an inaccurate description of a catalog field, not a wrong count. It stays minor: the CSV header is accurate and the C# summary defers to it, but a later item that reads Took as an oath day for these rows would be misled. One could argue for "defect" under "a false claim in the docs".

**The skeptic's corrected fix.** Change the C# summary in us_senate_prep.pl and senate_record.md line 6 to: "the day of his oath by the New Senators page for a senator new in the 118th or 119th Congress, else the day his state's page gives (before the window; that may be the appointment day, e.g. Padilla 2021-01-18 against his oath on 20 Jan 2021)". Optionally do not name a specific example, to stay within the claim convention.

### 3. The Changed Parties regex silently skips 8 of the page's 23 chapters

- **Lens:** tool-vs-sources - **reviewer:** note - **skeptic:** minor
- **Where:** Tools/us_senate_prep.pl:260

**The scenario.** I ran the line-260 regex on the saved page. It matches 15 entries. The page's table of contents lists 23 chapters, and Stewart, La Follette Jr., Shipstead, Byrd Jr., Robert Smith and Specter are among those missed: 'Name, Jr., of State' and spans without a comma do not fit the pattern. The 'undeclared change' check (line 265) only sees entries the regex matches. A future window senator whose entry is written like Byrd's would pass that check silently. The D/I label check and the Third/Minor year check would still be backstops. Neither missed entry is in the window today.

**The fix proposed.** Count the 'Chapter N:' entries in the table of contents and die unless the body matches equal that count. Alternatively, loosen the name and span patterns, e.g. allow ', Jr.,' and an optional comma before the years.

**The skeptic's evidence.** I reproduced the finding. I ran the staged line-260 regex, `/([A-Z][\w.' ]+?) of ([A-Z][a-z]+(?: [A-Z][a-z]+)?):? ((?:[A-Z][\w ]+?, \d{4}-(?:\d{4}|present) ?)+)/g`, over words() of raw/senate/wayback_senate_SenatorsWhoChangedPartiesDuringSenateService_20261004054837.html in a scratch copy, using the tool's own words()/ascii(). It matched 15 entries. The table of contents lists 23 chapters.

The 8 entries it misses:
- Stewart: the body writes "Republican, 1864-75", a two-digit end year.
- Mantle
- La Follette Sr. and La Follette Jr.: written "Name, Sr.,/Jr., of State".
- Shipstead
- Byrd Jr.: written "Harry F. Byrd, Jr., of Virginia".
- Robert Smith
- Specter

Sinema and Manchin both match, so the catalog today is right.

Some matches pick up leading prose in $1, for example "Stewart was widely criticized ... John P. Jones". That does no harm, because find_holder keys on surname_key (the last word).

The gap that is real: the check `problem("Changed Parties: $name ($st) changed party, not declared") unless $change{$k}` only runs on matched entries. A later capture whose window-senator entry is written in the Jr./Sr. or two-digit-year style would not be checked. Nothing counts the matches against the chapters.

The backstops:
- The `labelled D, I with no declared change` check covers only D-to-I as labelled on the state page.
- The Third/Minor year check covers only someone who becomes an independent.

An R-to-D or D-to-R change written in a missed style would get past all three checks. That makes senate_record.md reading 4's "Any other change of a senator of the window stops the run" stronger than the code guarantees.

None of this is in the window today, so it is a gap a later item could trip on, not a wrong catalog row. I re-grade it from note to minor because of the doc claim.

**The skeptic's corrected fix.** Count the table of contents' 'Chapter N:' entries. Count the body summary lines with a pattern that also accepts ', Jr.,' or ', Sr.,' after the name and two-digit end years ('\d{4}-(?:\d{2,4}|present)'). Call problem() unless the two counts are equal. Or anchor the loop on the chapter names: for each one, find its summary line, and stop the run when one is not found.

### 4. The record names only the Directory's change-day differences, not its appointment-day differences

- **Lens:** tool-vs-sources - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/senate_record.md:

**The scenario.** Reading 6 names Sasse (6 vs 8 Jan 2023) and Rubio (25 vs 20 Jan 2025). The Directory's 'Elected or [Appointed]' column also differs from the Appointed Senators page for all four appointees: Ricketts [1-8-23] vs Jan 12, 2023; Butler [10-2-23] vs Oct 1, 2023; Husted 1-17-25 vs Jan 18, 2025; Moody 1-16-25 vs Jan 21, 2025. senate_changes.csv uses the Appointed page's days. The pasted run prints the Directory's 'chosen' days, but the prose does not say which source decides an appointment day.

**The fix proposed.** Add one sentence to reading 6: appointment days come from the Appointed Senators page, and the Directory's bracketed days are printed, not used. Name the four differences.

**The skeptic's evidence.** Part of the finding is refuted. senate_record.md:38, reading 6, already says which source decides a day: "Which source decides a day. The Senate's own pages". The Appointed Senators page is one of the Senate's pages, and senate_changes.csv follows it (line 7: Ricketts "appointed 2023-01-12"; line 9: Butler "appointed 2023-10-01"; line 21: Husted "appointed 2025-01-18"; line 20: Moody "appointed 2025-01-21"). So the claim that "the prose does not say which source decides an appointment day" is wrong.

Appointment days also never touch the roster. Reading 1 (line 33) holds a seat from the oath, and the Directory's oaths are checked against the New Senators page. In us_senate_prep.pl:407-413, the Directory's `$chosen` day only goes into a printed string ("$succ chosen " . day($chosen,'CDIR')). Nothing compares it or uses it. No wrong date, seat or count can come from this.

The part that stands: reading 6 lists only the two resignation-day differences (Sasse, Rubio) as the Directory's differing days. The pasted run (record lines 82-85) shows the Directory's chosen days differ from the appointment days in all four rows: Ricketts 2023-01-08 vs 01-12, Butler 2023-10-02 vs 10-01, Husted 2025-01-17 vs 01-18, Moody 2025-01-16 vs 01-21. The prose neither names these nor says the chosen days are only printed. That leaves the list of differences incomplete, which is a wording gap and not a false claim.

**The skeptic's corrected fix.** Do not add a sentence saying which source decides an appointment day, because reading 6 already says so. Add one clause to reading 6 after the Sasse/Rubio list: "its 'chosen' days for the four appointees (Ricketts, Butler, Husted, Moody) differ from the Appointed Senators page's in every row and are printed, not used: a seat is held from the oath (reading 1)."

### 5. Some matches use state + surname only, so the two SC Grahams are told apart in one place only

- **Lens:** tool-vs-sources - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_senate_prep.pl:353

**The scenario.** The class-page anchor (lines 353-356), the Democrats' list (324-328) and the Directory class listing key entries by '$st surname'. On the Class II page's date (2026-07-14), a roster holding Lindsey Graham instead of Darline would still pass that anchor. find_holder (186) uses the first initial only when two holders match, so a New Senators or Appointed entry is matched without checking the given name when the state page shows only one Graham. That would happen if the page were captured before a same-surname successor. Today the Died in Office check (Lindsey's day) and the Directory's no-vacancy count catch every such slip, and SC's page was captured 2026-10-01.

**The fix proposed.** Key the class-page and Directory anchors on surname plus first initial wherever a state holds two of a surname in the window. In find_holder, also compare first_initial when there is one match, and report a mismatch instead of accepting it.

**The skeptic's evidence.** The weakness is in the code, but wrong data cannot get through it today, so this is a note.

What is true:
- The class-page anchor builds both sides on `"$st " . surname_key(...)` alone: `$page{"$st " . surname_key($nm)} = $p` and `%roster = map { ("$_->{st} " . surname_key($_->{x}{name}) => $_->{party}) }` (around lines 353-356).
- The Class II page is dated Tuesday, July 14, 2026 and lists "Graham, Darline (R-SC)". On that day "SC graham" => R would match whichever Graham the roster held.
- The Directory class listing (`$entry{"$3 " . surname_key($1)}`) and the Democrats' list (`$listed{"$st " . surname_key($nm)}`) are keyed the same way. Neither can meet both Grahams: the Directory closes 2024-04-25 and 2025-10-01, before Lindsey died on 2026-07-11, and SC has no Democrats.
- `find_holder` filters on `first_initial` only `if @m > 1`, so a single surname match is accepted without comparing the given name.
- The comment at line 130 says "where one state has two of a surname in the window, by his first given name's initial as well (South Carolina's Grahams)". That holds only for find_holder, not for the three keyed anchors, so it overstates a little.

Why the failing path does not get through:
- For Lindsey to hold SC class 2 on 2026-07-14, his `left` must be after 07-14. Then he ended after Darline began (2026-07-13 on SC's page), and line 173 fires: `problem(... ended ..., after ... began ...) if ... ended gt ... began`.
- If Darline's oath were moved later, the seat reads vacant ("SC -") and the anchor fires.
- `find_holder` sees two Grahams today (senate_seats.csv rows 104-105), so it uses the initial. A single-match misattribution would need Darline missing from SC's page. Her New Senators oath (2026-07-14) would then land on Lindsey, which makes his `took` 2026-07-14 and leaves the seat vacant from 2023 on. The Directory's vacant==0 counts and the [SEN-DIV] stretches would then fail, and so would the Died in Office check (mutation x02 covers Lindsey).

So no wrong catalog row passes. What remains is a hardening gap plus slightly loose comment wording.

**The skeptic's corrected fix.** Optional hardening:
1. Key the class-page anchor on "$st surname initial" whenever %holders has two of that state and surname in the window. The Directory and Democrats'-list keys can stay as they are, since they never meet both Grahams.
2. In find_holder, when there is one match and the name has an initial, compare first_initial too and report a mismatch.
3. Otherwise, narrow the line-130 comment to say the initial is used by find_holder only, and that the class-page anchor is surname-keyed.

### 6. The [SEN-DIV] stretch test checks only each stretch's ends and the days beside them, so a merged stretch or a missing one passes

- **Lens:** csharp-check - **reviewer:** defect - **skeptic:** minor
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:947

**The scenario.** Lines 944-952 check four things for each senate_division row: the line holds on HoldsFrom, it holds on HoldsTo, it breaks the day before (inside the Congress), and it breaks the day after (unless the 118th's last day or the reach). Nothing checks the days inside a stretch, and nothing checks that every day the line holds is covered by some stretch. I mirrored the check in perl (scratchpad emu.pl) on the committed CSVs, and every one of these wrong divisions PASSES: (a) the 118th's 2024-09-09..2024-12-07 and 2024-12-09..2025-01-02 merged into one row 2024-09-09..2025-01-02, which hides the 8 Dec vacancies in CA and NJ (the day before 09-09 breaks, and the end is the Congress's last day, so it is exempt); (b) the three 119th rows merged into 2025-01-21..2026-09-06, which hides the Oklahoma vacancy (23 Mar 2026) and the South Carolina one (11-13 Jul 2026); (c) the row 2026-03-24..2026-07-10 dropped. The perl tool builds the stretches day by day (us_senate_prep.pl:443-453). The C# check is the standing guard that is supposed to re-derive them, so a bug in that loop or a hand-edit would get past it. The summary line then prints these stretches as reproduced.

**The fix proposed.** Re-derive the stretches in C# instead of only probing their ends. Walk every day from each Congress's first day (2023-01-03 / 2025-01-03) to its last (2025-01-02 / the reach), run Holds(day), build the runs, and compare the list of runs to SenateDivision row for row. That is at most about 1,350 days × 100 seats × 119 rows, cheap in an editor check. At minimum, check every day inside each stretch, and check that the days between consecutive stretches fail.

**The skeptic's evidence.** The gap is real, but I'm grading it minor, not defect.

The check only tests the edges of each stretch. In Assets/Editor/GeneratedCatalogCheck.cs, the [SEN-DIV] loop (about lines 943-952) does three things for each row:
- `Holds(HoldsFrom)` and `Holds(HoldsTo)`;
- `Holds(Shift(HoldsFrom,-1))` must fail, unless `HoldsFrom` is the Congress's first day;
- `Holds(Shift(HoldsTo,1))` must fail, unless the row ends on 2025-01-02 (`lastOfCongress`) or on the last named day (`lastOfReach`, which is 2026-09-06 in senate_on.csv).

Nothing tests the days inside a stretch, and nothing checks that the stretches cover every day the line holds.

All three of the reviewer's scenarios pass, which I traced against the committed rows:
- (a) One merged 118th row, 2024-09-09..2025-01-02. It holds on 09-09 (Helmy took 2024-09-09). On 09-08 NJ is vacant (Menendez left 08-20), so the day before breaks. The end is exempt as `lastOfCongress`. The 12-08 vacancies are never looked at: Butler and Helmy left 2024-12-08, Schiff and Kim took 12-09.
- (b) One merged 119th row, 2025-01-21..2026-09-06. On 01-20 OH is vacant (Vance left 01-10, Husted took 01-21), so the day before breaks. The end is exempt as `lastOfReach`. The OK vacancy on 03-23 (Mullin) and the SC vacancy 07-11..07-13 (Graham) are never looked at.
- (c) The row 2026-03-24..2026-07-10 deleted. Nothing requires the rows to cover every day.

The prep tool does build the stretches in full: Tools/us_senate_prep.pl walks every day of each Congress through `count(roster($d))` (the `for (my $d = $from; $d le $to; ...)` loop). So the C# check is the weaker of the two.

Why minor and not defect:
1. The committed stretches are correct, and the perl loop is a plain day-by-day walk.
2. Wrong stretches cannot get in by hand-editing the CSV. `ReadUsCsv` holds senate_division.csv to `SenateDivisionSourceDigest`, and every row is compared with `UsPresidentialReturns.SenateDivision`. Only a bug in the generator, regenerated through, would reach the check.
3. Nothing claims more than the check does. senate_record.md:103 describes it exactly: "each [SEN-DIV] stretch holding at both ends and broken on the days beside it inside its Congress". The code comment says the same.
4. The finding overstates one point. The summary line does not print the stretches "as reproduced". It prints only the number of stretches (`{4} [SEN-DIV] stretch(es), every figure the CSVs'`).

Still, the ruling's done-when asks for [SEN-DIV] to be reproduced, and this is the standing guard that is supposed to re-derive it. A later edit to the prep loop could merge or drop a stretch and this check would not catch it, so it is a gap worth closing.

**The skeptic's corrected fix.** Re-derive the stretches in C# and compare them row for row. For each Congress, walk every day from its first day (2023-01-03 for the 118th, 2025-01-03 for the 119th) to its last (2025-01-02 for the 118th, the last named day for the 119th). Call `Holds(day, D, R, I)` with that Congress's [SEN-DIV] line, collect the runs of days on which it holds, and require them to equal the `SenateDivision` rows for that Congress, in order, with the same `HoldsFrom` and `HoldsTo`. Name the first run that differs.

`Count` currently loops over every seat for every seat key, roughly 100 × 128 per day. Over about 1,350 days that is about 17M comparisons, acceptable in the cheap bar. Building a per-key holder list once would make it cheaper.

When this lands, update the code comment and senate_record.md:103 to say that the stretches are derived again, not just probed at their ends.

### 7. No check that a seat has at most one holder on a day; the per-day '100 seats' test cannot fail

- **Lens:** csharp-check - **reviewer:** defect - **skeptic:** minor
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:889

**The scenario.** Count (lines 877-898) adds exactly one entry to n for every (state, class) key, a party or vacant, so seatsThatDay always equals seatKeys.Count. Once the 50-states × 2-classes test at 905-907 passes, the 'not 100' test at line 925 can never fire, yet the summary prints '100 seats on every named day' as a roster finding. Inside a key, the inner loop takes the LAST row that serves on the day, so two holders overlapping in one seat are never noticed (the tool takes the first match, us_senate_prep.pl:300). Emulated: set Laphonza Butler's left to 2024-12-10 while Adam Schiff took the seat on 2024-12-09 (CA has two senators in class 1 on 12-09), and the check PASSES. Rows with Took after Left are not checked either. The tool's own s02 stops an overlap at generation, but this check is meant to be the independent roster diagnostic, and the done-when criterion relies on it.

**The fix proposed.** In Count, or in a pass over the seat rows, count the rows that serve each key on the day and add an error when more than one does. Separately, check each key's rows in order: Took < Left, and each Left <= the next row's Took. Then either drop the per-day '100 seats' test or make it count holders plus vacancies, with each key contributing exactly one.

**The skeptic's evidence.** The mechanics are as the reviewer says. GeneratedCatalogCheck.cs, Count(): `foreach (string key in seatKeys) { ... string party = "-"; foreach (var s in SenateSeats) { if (... not serving ...) continue; party = ...; demCaucus = ...; } int col = ...; n[(cl - 1) * 4 + col]++; }`. Each (state, class) key adds exactly one to the 12 class/party cells, so `seatsThatDay` always equals seatKeys.Count. The structural tests just before it (`statesOf.Count != 50`, each state `kv.Value.Count != 2`) already pin that count at 100. So `if (seatsThatDay != 100)` can never fire once those pass. Inside a key the loop keeps the LAST serving row, and nothing counts how many rows serve on a day. Nothing checks Took < Left or Left <= the next Took either. A same-party overlap (Butler D and Schiff D both serving CA class 1) gives the same counts as the tool's first-match roster (us_senate_prep.pl:300, `my ($x) = grep {...}`), so the senate_on comparison and the [SEN-DIV] Holds() tests pass too. The check cannot catch a seat with two holders on its own.

Why minor and not defect: no wrong data can reach the catalog today. The CSVs are digest-locked to the catalog, and the only producer is the tool. The tool stops an overlap at us_senate_prep.pl:172-174 (`problem("$st class $cl: ... ended ..., after ... began ...") if $s->[$i-1]{ended} eq '-' || ... gt $s->[$i]{began}`), and mutation case s02 proves that stop. I also ran an independent pass over the committed senate_seats.csv: 100 keys, no row with left <= took, no overlap between consecutive holders. So the catalog is right. The gap is in the check's independence plus overclaiming wording, not a wrong figure. The summary line ("the roster derived again from the seats: 100 seats on every named day") and the record (senate_record.md:103, "on every named day - 100 seats") describe a per-day test that is a tautology. The tool's overlap stop also reads the page's began/ended, not the final oath-based took/left. A later edit that moves took (for example the oath-day reading) could therefore produce an overlap the tool does not see. If it did, this check would not see it either. That is a gap a later item could trip on.

**The skeptic's corrected fix.** In Count(), count the rows serving each key on the day and add an error to `wrong` when more than one serves ("{day}: {state} class {cl} has {k} holders"). Before the per-day loop, walk each key's rows in Took order and require Took < Left (when Left != "-"), Left <= the next row's Took, and only the last row open. Then either drop `seatsThatDay != 100` or replace it with a holders-plus-vacancies count that the one-holder test makes meaningful. Reword the summary and senate_record.md:103 to say "100 seats, each with at most one holder, on every named day" only once that test exists.

### 8. Division rows: any Congress other than 118 is treated as the 119th, nothing requires both Congresses to be present, and 'the reach' is whatever the last named day is

- **Lens:** csharp-check - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:946

**The scenario.** `first = v.Congress == 118 ? "2023-01-03" : "2025-01-03"`, so a row for Congress 117 or 120 is tested against the 119th's opening. If senate_division.csv lost every 118th row, the check still passes (ReadUsCsv flags only zero rows, and the count comparison is CSV against catalog). lastOfReach (line 950) assumes SenateOn's last row is the reach and never checks its What. If a later named day were appended after the reach, a 119th stretch ending at the reach would fail with a misleading 'holds the day after' message.

**The fix proposed.** Require the set of Congresses in SenateDivision to be exactly {118, 119}, and add an error for any other value. Name the reach explicitly: the SenateOn row whose What starts 'the record's reach'. Require HoldsFrom <= HoldsTo and both inside the Congress's span.

**The skeptic's evidence.** The finding is accurate about the C# check, but the shipped data is not wrong and no current path gets wrong data past it. That makes it a latent gap that a later item could trip on, not a defect.

What the check does (GeneratedCatalogCheck.CheckUsSenate, staged diff):
- `string first = v.Congress == 118 ? "2023-01-03" : "2025-01-03";`: any Congress that is not 118 is given the 119th's opening.
- `bool lastOfReach = UsPresidentialReturns.SenateOn.Length > 0 && v.HoldsTo == UsPresidentialReturns.SenateOn[UsPresidentialReturns.SenateOn.Length - 1].Day;`: the reach is taken to be the last named day by position. Its `what` is never read.
- No line requires the set of Congresses in SenateDivision to be {118, 119}. ReadUsCsv flags only zero rows. The count comparison is CSV against catalog, so if a Congress's rows were dropped consistently from both, the check would not notice.

Why it does not fail today:
- senate_division.csv holds only 118 and 119 rows. The 118th's last stretch ends 2025-01-02 (lastOfCongress). The 119th's last ends 2026-09-06, which is the last SenateOn row ("the record's reach (the New Senators page captured)").
- The generator is what guarantees the rest:
  - us_senate_prep.pl:427 calls `problem(... unless keys %div == 3)` when [SEN-DIV] lacks its 117th-119th lines.
  - :440-452 builds holds only for `%congress = (118 => [...], 119 => ['2025-01-03', $reach])` and calls `problem("[SEN-DIV] $c: ... holds on no day ...") unless $holds{$c}`. Mutation case v01 covers that.
  - :456-458 hard-codes @named with `[$reach, "the record's reach ..."]` last.
- ReadUsCsv compares each CSV's SHA-256 with the digest recorded in the catalog. A hand edit that drops the 118th rows therefore fails unless the CSV, the C# rows and the digest are all forged together.

What could go wrong later: CheckUsSenate is the roster diagnostic that the done-when relies on ("reproducing [SEN-DIV]'s division per Congress"), yet it cannot tell when a Congress is missing. If a later item extends the record to a 120th Congress, or adds a named day after the reach, the check would silently use the 119th's opening for it, or would report a misleading "holds the day after" error. Only the generator's own problem() lines stop that today.

**The skeptic's corrected fix.** In CheckUsSenate:
- Collect the distinct v.Congress values and add an error unless they are exactly {118, 119}.
- Map each Congress explicitly to its span (118: 2023-01-03 to 2025-01-02; 119: 2025-01-03 to the reach). Add an error for any other Congress, and for any row where HoldsFrom > HoldsTo or either end falls outside its Congress's span.
- Find the reach as the SenateOn row whose What starts with "the record's reach" (an error if there is none, or more than one), not the last row by position.

No data or generator change is needed. The current rows already satisfy all of these.

### 9. The record mixes D/R/I and R/D/I order in one sentence

- **Lens:** csharp-check - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/senate_record.md:95

**The scenario.** 'On the 119th's opening, 3 Jan 2025, 45 / 52 / 2 with West Virginia's seat vacant; `[SEN-DIV]`'s 53 / 45 / 2 holds from 21 Jan 2025' gives the first triple as D/R/I and the second as R/D/I (senate_division.csv says d 45, r 53, i 2), with no labels. A reader comparing the two triples would think R went from 52 to 45.

**The fix proposed.** Label the parties, or print [SEN-DIV]'s line in D/R/I order as 45 / 53 / 2.

**The skeptic's evidence.** The finding holds. Line 95 of the staged senate_record.md sets up D/R/I order: "48 Democrats, 49 Republicans and 3 independents" and then "[SEN-DIV]'s undated 47 / 49 / 4", which is D 47 R 49 I 4 and matches senate_division.csv row `118,47,49,4`. The same paragraph then gives "3 Jan 2025, 45 / 52 / 2 with West Virginia's seat vacant", which is still D/R/I (D 45, R 52 because Justice was not yet sworn, I 2). Right after that it says "`[SEN-DIV]`'s 53 / 45 / 2 holds from 21 Jan 2025", which is R/D/I. The facts give the order: senate_division.csv has `congress,d,r,i` / `119,45,53,2`, and the record's own pasted run (lines 55-61) prints "D 45 R 53 I 2" and "[SEN-DIV] 119th: D 45 R 53 I 2". The R-first order likely comes from records_by_date.md row 53 ("REP 53, DEM 45, 2 I", majority listed first), but that row is labelled and line 95 is not. A reader who keeps the paragraph's own order reads D 53 / R 45 and concludes the Republicans fell from 52 to 45. The data is right: the CSV, the generated rows and the run lines all agree. Only the wording is ambiguous, so this is a note, not a false claim about the catalog.

**The skeptic's corrected fix.** At senate_record.md line 95, write `[SEN-DIV]`'s 119th line in the paragraph's own D/R/I order, "45 / 53 / 2", or label every triple in the paragraph (for example "D 45 / R 52 / I 2" and "D 45 / R 53 / I 2") so they match the run lines' "D 45 R 53 I 2" format.

### 10. records_by_date.md still dates [SEN-DIV]'s lines over whole Congresses and keeps a DERIVED '47 + 4 = 51' claim that the new catalog shows is false

- **Lens:** docs-claims - **reviewer:** defect - **skeptic:** minor
- **Where:** ElectionsData/usa/records_by_date.md:52

**The scenario.** The commit corrects only the §4 cross-check (line 186) to 48 D + 3 I on 2024-03-12. Table 1b still says that from 2023-01-03 to 2025-01-03 the 118th was 'DEM 47, REP 49, 4 I' (line 52), and that from 2025-01-03 the 119th was 'REP 53, DEM 45, 2 I' (line 53). Line 62's DERIVED reads 'the 118th's majority was 47 + 4 = 51 only with the four independents'. senate_division.csv shows the 118th line holds only from 2024-06-05, and not on 2024-08-20..09-08 or 2024-12-08. It shows the 119th line holds from 2025-01-21, not on 03-23 or 07-11..07-13 of 2026. So the same file now says 48+3 at line 186 and 47+4 from the opening at line 52. A builder who reads table 1b for the 118th at the US start (US-15 seats 'the 118th at the US start') gets 47/49/4, which the catalog contradicts. These lines are older than the commit, but the commit's own record shows they are wrong and G7 now says the lines 'are dated there' without touching the table.

**The fix proposed.** Annotate table 1b's 118th and 119th rows and the line-62 DERIVED: the line is [SEN-DIV]'s undated one, the dated stretches are in senate_division.csv / senate_record.md, and on the 118th's opening the Senate was 48 D + 3 I. Do not leave the table's from/to columns presenting the undated line as holding over the whole Congress.

**The skeptic's evidence.** The scenario holds, but it is older than this commit and no build reads it, so I grade it minor, not defect.

What holds:
- In ElectionsData/usa/records_by_date.md, table 1b line 52 dates the 118th row "2023-01-03 | 2025-01-03 ... 100 seats: **DEM 47, REP 49, 4 I**".
- Line 53 dates the 119th row "2025-01-03 | (open) ... **REP 53, DEM 45, 2 I**".
- Line 62 says: "**DERIVED**: the 118th's majority was 47 + 4 = 51 only with the four independents".
- The staged diff changes only line 186 (the §4 cross-check), to "48 DEM + 3 I / 49 REP on that day - corrected at US-12, §792: [SEN-DIV]'s undated 47 DEM + 4 I holds only from Manchin's change to independent on 2024-06-05", and adds that the majority rests on "three on the start date, four from 2024-06-05".
- It also closes G7 with "[SEN-DIV]'s undated lines are dated there". Table 1b and the line-62 DERIVED are left as they were.
- The new senate_division.csv confirms the stretches. The 118th line holds 2024-06-05..08-19, 2024-09-09..12-07 and 2024-12-09..2025-01-02. The 119th line holds 2025-01-21..2026-03-22, 2026-03-24..07-10 and 2026-07-14..09-06.
- So, after this commit, the file says 48+3 at line 186 and 47+4 over the whole 118th at lines 52 and 62. The DERIVED claim at line 62 ("only with the four independents") is false for 2023-01-03..2024-06-04.

Why minor and not defect:
1. The commit did not introduce these lines.
2. Table 1b's column header frames the row as senate.gov's "Party Division" figure, given once per Congress.
3. The failure path the finding names, a US-15 builder reading 47/49/4 at the US start, does not happen. docs/specs/USA_STAGE_PLAN.md line 385 tells US-15 to seat "by date from US-12's table — the 118th at the US start". That table is the catalog (senate_on.csv / UsSenateRecord.cs), not records_by_date.md table 1b. No generated or checked artefact reads table 1b.

What remains is an inconsistency inside the file. It is a gap a later reader could trip on, and it should be fixed in the same file the commit already touches.

**The skeptic's corrected fix.** In records_by_date.md, change only the text; leave the generated files as they are.
- Table 1b, 118th and 119th rows: mark each figure as [SEN-DIV]'s undated line. Add that the dated stretches are in senate_division.csv / senate_record.md, and that the 118th opened at 48 D + 3 I (Sinema I from 2023-01-03), reaching 47 D + 4 I from 2024-06-05.
- Line 62's DERIVED: restate it as a reading of the page's own figures, or say the majority rested on three independents until 2024-06-05 and four after.
- Optionally, add a pointer from G7's closing sentence to table 1b.

### 11. The caucus is applied to a senator's whole service, not 'on its capture day' as reading 5 and R-US10 (a)'s 'dated' say

- **Lens:** docs-claims - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_prep.pl:342

**The scenario.** Reading 5 (senate_record.md:37) says an independent caucuses with the Democrats 'where the Senate Democrats' own list carries him on its capture day'. In the code, $x->{caucus}='D' is set once and has no dates. roster()/senate_on.csv then count Sinema in dem_caucus on every day from 2023-01-03 to 2025-01-02 on the strength of one list dated 2024-03-18. That includes 2023-01-03, the very day the Senate's page dates her 'would not participate in either party caucus'. senate_on.csv's 2023-01-03 row (DEM 51) and senate_seats.csv's caucus_by carry no mark that this rests on a reading still put to Elias. US-15, seating 'by date from US-12's table', would take DEM 51 as sourced.

**The fix proposed.** Either declare in reading 5 that a list's caucus is extended over the senator's whole service (and say so in the CSV headers), or carry a caucus-from/to (or a 'reading, R-US10 open' marker for Sinema) in senate_seats.csv so the open reading travels with the figure.

**The skeptic's evidence.** Tools/us_senate_prep.pl:336-343 sets the caucus once per holder with no span: `if ($caucus_seen{$x}) { $x->{caucus_by} = "the Senate Democrats' list of " . join(...) } ... $x->{caucus} = 'D';`. Line 468 then counts every party-I seat whose holder has caucus 'D' on every named day: `$dem_caucus = $k->{D} + scalar(grep { $_->{party} eq 'I' && ($_->{x}{caucus} // '') eq 'D' } @$r)`. Sinema is party I from 2023-01-03 (senate_seats.csv:16 `AZ,1,Kyrsten Sinema,D/I,2023-01-03,...,D,the Senate Democrats' list of 2024-03-18`), so senate_on.csv's 2023-01-03 row shows DEM 51 (senate_record.md:48), on the strength of a list captured 14 months later. Reading 5 (senate_record.md:37) and the plan's Design (USA_STAGE_PLAN diff line 15) word the rule as "by the Senate Democrats' own list on its capture day". Neither says the caucus is then applied over the senator's whole service, and neither do the senate_seats.csv header (prep.pl:508-509: "caucus: an independent's, by the Senate Democrats' own list (R-US10 (a)), else -") or the C# summary (prep.pl:542-544). So the span the figure is applied over is not DECLARED. King and Sanders are covered by both lists, so this hardly matters for them; it matters for Sinema.

Part of the finding is weaker than written. The open Sinema reading IS recorded: reading 5 quotes the Senate's own 3 Jan 2023 words ("would not participate in either party caucus") and says "Sinema is a reading put to Elias ... The catalog counts her with the Democrats", and R-US10's "Read at US-12" line in the plan repeats it and names US-15. So a later item working from the plan's register will see it. What remains is a gap: the generated rows (caucus_by, senate_on's dem_caucus) carry no mark that this figure rests on an open reading, and nothing declares that the caucus is extended over the whole service. A gap that a later item could trip on is minor. No check passes on data that is wrong under the declared rule, and no count differs from what the record says the catalog does.

**The skeptic's corrected fix.** In reading 5, the plan's Design line and the senate_seats.csv header, declare that a caucus read from a list (or from the Senate's words) is applied over the independent's whole service in the window. Mark Sinema's caucus_by in the generated rows as open, e.g. "the Senate Democrats' list of 2024-03-18 (a reading put to Elias, R-US10)", so the open reading travels with senate_seats.csv and senate_on.csv's dem_caucus into US-15. Optionally add caucus_from/caucus_to columns. Dating Sinema's caucus only from the list's capture day would change counts the record already explains, so that should wait for Elias's ruling.

### 12. Reading 6 names two Directory differences, but its appointment days differ from the Appointed Senators page on all four rows, and none of these are named or checked

- **Lens:** docs-claims - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/senate_record.md:38

**The scenario.** Reading 6 lists the Directory's differing change days as Sasse (6 vs 8 Jan 2023) and Rubio (25 vs 20 Jan 2025). The Directory's 'Elected or [Appointed]' column (CDIR tables, pdftotext -raw) gives Ricketts [1-8-23], Butler [10-2-23], Husted 1-17-25 and Moody 1-16-25. The Appointed Senators page gives Jan 12 2023, Oct 1 2023, Jan 18 2025 and Jan 21 2025. Moody's Directory day falls before Rubio's resignation. The run prints these as 'chosen ...' but never shows the Senate's day beside them, and the tool never compares them (line 399: 'printed, not used'). A reader of reading 6 would take the two named resignations as the only disagreements.

**The fix proposed.** Print the Appointed Senators page's day next to the Directory's 'chosen' day in the run, and name the four appointment-day differences in reading 6. No seat changes, because the oath decides.

**The skeptic's evidence.** The facts hold. I checked them with pdftotext -raw on the staged CDIR PDFs. The 118th table reads "Pete Ricketts 1 ... [1-8-23] 1-23-23" and "Laphonza Butler 1 ... [10-2-23] 10-3-23". The 119th table reads "Jon Husted ... 1-17-25 1-21-25" and "Ashley Moody ... 1-16-25 1-21-25"; its column is headed "Elected", not "[Appointed]". The Appointed Senators page, read at us_senate_prep.pl lines 219-231 into $x->{appointed}, gives 2023-01-12, 2023-10-01, 2025-01-18 and 2025-01-21. The record's own pasted run shows the same four. The change lines read "Ricketts appointed 2023-01-12", "Butler appointed 2023-10-01", "Husted appointed 2025-01-18" and "Moody appointed 2025-01-21". The Directory lines read "Ricketts chosen 2023-01-08", "Butler chosen 2023-10-02", "Husted chosen 2025-01-17" and "Moody chosen 2025-01-16". All four differ, and Moody's Directory day falls before Rubio's resignation on 20 Jan.

Reading 6 (senate_record.md line 38) says "the Congressional Directory's change days are printed where they differ" and lists only Sasse and Rubio. The tool never compares the chosen day with the appointed day. Line 399 says the change days "are printed, not used"; line 411 checks only the oath. The Senate's appointment day is not just printed. It reaches the catalog: senate_changes.csv has "2025-01-21,FL,3,sworn,Ashley Moody,appointed 2025-01-21", and line 475 builds that detail field. So the catalog carries four days on which its two sources disagree, and the record names none of them. A later item that reads the appointment detail (Florida or Ohio timing, G6's specials) could trip on this.

Part of the finding is wrong. The run does show the Senate's day: it is on the "change:" lines, not beside the Directory's day on the same line. Nothing in the roster or the counts is wrong, because the oath decides the seat and reading 1 is DECLARED. Reading 6's opening, "the Senate's own pages" decide, already settles which day the catalog uses. So this is an incomplete list, not a false claim. Minor, not a defect.

**The skeptic's corrected fix.** In reading 6, name the four appointment-day differences next to Sasse's and Rubio's: Ricketts 8 Jan 2023 in the Directory vs 12 Jan on the Appointed Senators page; Butler 2 Oct 2023 vs 1 Oct; Husted 17 Jan 2025 vs 18 Jan; Moody 16 Jan 2025 vs 21 Jan (the Directory's day comes before Rubio's resignation). Say that the catalog's "appointed" detail in senate_changes.csv is the Senate's day and that the oath decides the seat. Optionally, make the "the Directory's table" line also print the Appointed Senators page's day ($x->{appointed}) beside "chosen", so the run shows each pair on one line. No seat or count changes.

### 13. Reading 7 and the plan's Design say changes after the reach are 'not read', but a later-captured state page either stops the run or is read

- **Lens:** docs-claims - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/senate_record.md:39

**The scenario.** Several state pages were captured after the reach of 2026-09-06: CA 10-04, LA/MO/OR/SC 10-01, KS 09-30, AK 09-29, PA 09-24, MT 09-10. Suppose a re-fetch of one of them shows a senator sworn after 09-06. Line 212 then reports 'began ... with no oath on the New Senators page' and the whole run refuses. A resignation or death after 09-06 on such a page is read: it is written into senate_seats.csv's left column and senate_changes.csv. The plan's Design bullet (USA_STAGE_PLAN.md:365, 'later changes are not read') and reading 7 ('an oath after it is not read') describe neither behaviour.

**The fix proposed.** State the reach as the code applies it: named days and [SEN-DIV] stretches end at the reach; an oath after it on a later-captured state page stops the run; an end after it is recorded. Alternatively, have the tool drop or clip rows past the reach.

**The skeptic's evidence.** Nothing is wrong on today's data. No row in senate_seats.csv has a took or left day after 2026-09-06, so reading 7 and the plan's "later changes are not read" are true for the committed raw. The gap only opens if a state page is captured or re-fetched later: Tools/us_senate_prep.pl never clips anything at $reach.

1. The state-page reader (lines 140-170) keeps every row whose `ended` falls after the window's first day. It has no upper bound at the reach.
2. The changes loop (lines 472-473) pushes `[$x->{left}, ..., $x->{how_out}, ...]` for every ended holder, again with no `le $reach` test. senate_changes.csv's own header (line 514) says "a seat change ... to the record's reach". So a resignation or death dated after 09-06 on a later-captured page (CA 10-04, LA/MO/OR/SC 10-01, KS 09-30, AK 09-29, PA 09-24, MT 09-10) would be written into senate_seats.csv's `left` column and into senate_changes.csv. No check would fire. This breaks "later changes are not read" (USA_STAGE_PLAN.md:365) and the CSV header. The roster counts would not be wrong, because @named and %congress stop at $reach.
3. A senator sworn after the reach on such a page fails line 212 (`began ... inside the window, with no oath on the New Senators page`), and the whole run refuses. Reading 7 calls that "not read". The run fails loudly, so this half is wording, not wrong data.

Since the scenario needs a later capture, and the CSV and doc claims are false only in that case, this is a gap a later item could trip on. It is not a defect in the catalog as committed.

**The skeptic's corrected fix.** Choose one of two ways. Either clip at the reach in the tool, or state the reach as the code applies it.

To clip in the tool: treat `left gt $reach` as '-' (still serving at the reach), or refuse such a row with a problem(). Skip a state-page row whose began date is after $reach and which has no oath, rather than stopping the run. Then reading 7 and the plan's "later changes are not read" become true by construction.

To state the reach as the code applies it, in reading 7, the plan's Design bullet and senate_changes.csv's header: the named days and [SEN-DIV] stretches end at the reach; an oath after it on a later-captured state page stops the run; an end after it is recorded.

### 14. G6 and 'Not reached' name only Ohio's and Florida's Class 3 seats as the 2026 specials; the 2026 Class 2 appointments are left out

- **Lens:** docs-claims - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/records_by_date.md:241

**The scenario.** The catalog holds two more appointees: Armstrong (OK 2, sworn 2026-03-24) and Darline Graham (SC 2, sworn 2026-07-14). Whether those states hold a special for the remainder of the term ending 2027-01-03 (as California did for Schiff's seat in 2024) is just as unsourced. G6 says that 'the two Class 3 seats ... are the seats a special would fill', and senate_record.md:108 repeats only OH and FL. US-16's 2026 race list could therefore drop OK and SC's special question.

**The fix proposed.** Name all four appointee seats in G6 and in 'Not reached'. Say that OK and SC's Class 2 seats are up in Nov 2026 anyway, and that a special for the remaining weeks is BILLED like the other two.

**The skeptic's evidence.** The catalog the same commit generates holds two more seats filled by appointment inside the window. In senate_seats.csv, row 95 is `OK,2,Alan Armstrong,R,-,2026-03-24,-,appointed,...` and row 105 is `SC,2,Darline Graham,R,-,2026-07-14,-,appointed,...`. senate_changes.csv lines 22-25 list both changes (Mullin resigned 2026-03-23; Graham died 2026-07-11, and his successor was sworn 2026-07-14). senate_record.md lines 79, 81 and 95 name both changes.

The new G6 sentence still reads as a complete list. At records_by_date.md:241 it says: "Named at US-12 (§792): the two Class 3 seats filled by appointment in January 2025 - Ohio's (Husted) and Florida's (Moody) ... - are the seats a special would fill; whether and when each state holds one stays BILLED." senate_record.md:108 under "Not reached" repeats only "Ohio's and Florida's Class 3 seats".

The older wording "and any other" in G6 is generic and does not remove the gap. The US-12 sentence names a definite set of seats, and that set leaves out OK 2 and SC 2. Those are appointee-held seats whose term ends 2027-01-03. The 3 Nov 2026 regular election fills the next full term. A separate special for the weeks left of the current term (the shape California used in 2024) is just as unsourced as Ohio's and Florida's.

This matters downstream. US-16 in USA_STAGE_PLAN.md:395 elects "the specials where sourced, else BILLED — G6", and the sources table at :1012 and :1022 bills "the 2026 specials (G6)". So US-16 reads G6's named list, and a reader of it would not carry the OK and SC question forward.

This is a gap and an exhaustive-sounding wording, not a wrong date, seat or count in the catalog. That makes it minor, not a defect.

**The skeptic's corrected fix.** In records_by_date.md G6 (line 241, and the duplicate at diff line 20) and in senate_record.md's "Not reached" (line 108), name all four seats held by appointment at the record's reach:
- Ohio 3 (Husted) and Florida 3 (Moody), whose terms run to 2029, so a special would fill the rest of the term.
- Oklahoma 2 (Armstrong, sworn 2026-03-24) and South Carolina 2 (Darline Graham, sworn 2026-07-14). Their Class II terms end 2027-01-03, so the regular election of 3 Nov 2026 fills the next term. A special for the weeks left of the current term is a separate question.

Say that whether and when each of the four states holds a special stays BILLED. Optionally, also name OK and SC beside Ohio in the DERIVED paragraph at records_by_date.md:77.

### 15. The Directory's 'as of the end of the legislative day of X' is never checked against the closing date the count is compared on

- **Lens:** docs-claims - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_prep.pl:367

**The scenario.** The regex matches 'legislative day of [A-Za-z]+ \d+, \d{4}' without capturing it. The count is then compared to roster($closing). Both current editions agree (April 25, 2024; October 1, 2025). In an edition whose legislative day differs from its closing date, the anchor would compare the wrong day, and the record's 'party count on the closing day' would be wrong without anything noticing.

**The fix proposed.** Capture the legislative day, compare roster() on that day, and refuse if it is not the declared closing date.

**The skeptic's evidence.** The finding is real but has no effect today, so minor is the right grade. At Tools/us_senate_prep.pl line 367 (staged), the regex matches `as of the end of the legislative day of [A-Za-z]+ \d+, \d{4}\.` but does not capture that date. The count is then compared against `count(roster($closing))`. `$closing` is checked only against the separate "Closing date for compilation of the Congressional Directory was ..." sentence (lines 364-365). The mutation suite also covers only that sentence (us_senate_mutations.sh line 115: "closing date 2024-04-26, declared 2024-04-25"). Nothing checks the date the count is actually stated for, and no mutation alters it.

Running pdftotext on the two committed PDFs shows the dates match today:
- govinfo_cdir_2024-04-25.pdf: closing date April 25, 2024; the Senate count's legislative day is also April 25, 2024. Its one other legislative-day line ("January 6, 2021") is in a different context, and the regex requires the "Democrats in roman" tail.
- govinfo_cdir_2026-02-20.pdf: closing date October 1, 2025; legislative day October 1, 2025 (twice).

So the comparison runs on the correct day for both editions. The record's claim in senate_record.md line 24 ("the Senate's party count on the closing day (25 Apr 2024; 1 Oct 2025)") is true, and nothing in the catalog is wrong.

The gap is still there. A Senate legislative day can run on past its calendar day through recesses, so a later edition, or a corrupted or altered copy of one of these, could state its count for a different day. The run would still pass and compare against the wrong day's roster. A mutation that changed only "legislative day of April 25" would go uncaught. This is a check that could pass on wrongly dated data but does not do so on the committed data, which the grading scale calls minor, not defect.

**The skeptic's corrected fix.** Capture the date in the count regex (`legislative day of ([A-Za-z]+ \d+, \d{4})\.`) and call problem("CDIR $edition: count as of " . day($ld,'CDIR') . ", closing $closing") unless day($ld,'CDIR') eq $closing. Then add a mutation case to Tools/us_senate_mutations.sh that changes only the legislative day in the extracted text and expects that problem line.

### 16. '30 of 30 caught' is an undated transcribed count about the code

- **Lens:** docs-claims - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/senate_record.md:101

**The scenario.** The checks section states the suite's tally with no date. Adding a case makes it stale. The House record's precedent dates it ('On 2026-10-06 it caught all fifty-three'). This conflicts with the claim convention's spirit (DERIVED counts go GENERATED, REFERENCED or dated as a measurement).

**The fix proposed.** Write 'On 2026-10-06 it caught all thirty' (a dated measurement), or point to the suite's output.

**The skeptic's evidence.** The finding holds, at note severity.

`senate_record.md` in the index, under "## The checks", says: "**`Tools/us_senate_mutations.sh`** proves those refusals on a copy, as `us_house_mutations.sh` does for US-11: **30 of 30** caught, each printing its exact mismatch". That count is a fact about the suite, and no date sits beside it. The only dated heading near it belongs to the section before: "## The run (2026-10-06, pasted)", which covers the generator's output, not the mutation suite.

The sibling records all date this same kind of claim:
- `house_districts.md:29`: "On 2026-10-06 it caught all fifty-three."
- `president_returns.md:37`: "On 2026-10-05 it caught all twenty-nine", then "On 2026-10-06 (US-5, §788) it caught all fifty-seven".

CLAUDE.md lines 39-41 call a count DERIVED. A DERIVED claim may only be GENERATED, REFERENCED or DELETED, and only a dated record of what was measured is exempt.

The count is correct today: the staged script has 30 `case_` calls besides the function definition, so nothing in the record is false now. No check enforces the rule here either: `DocumentClaimCheck` only scans `*.md` at the repository root (TopDirectoryOnly), so `ElectionsData/usa/*.md` is outside it. The risk is only that the line goes stale when a case is added, which makes it a wording or convention note, not a defect.

**The skeptic's corrected fix.** Make it a dated measurement, in the sibling records' form: "... as `us_house_mutations.sh` does for US-11. On 2026-10-06 it caught all thirty, each printing its exact mismatch; the control reproduced the repository's bytes."

### 17. The claim 'not bioguide (Cloudflare 403)' is not in any fetch log

- **Lens:** docs-claims - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/senate_record.md:29

**The scenario.** raw/senate/fetch_log.txt records the king/sanders 403s and their discarded captures but has no bioguide attempt, and neither does any other log under ElectionsData/usa. The register states a host status that its own log does not support.

**The fix proposed.** Add the bioguide probe line to fetch_log.txt, or drop the parenthetical.

**The skeptic's evidence.** The finding holds. senate_record.md:29 (staged) reads: "**Logged and not kept:** `king.senate.gov` and `sanders.senate.gov` (403 directly; ...). The tool reads no other source: not bioguide (Cloudflare 403), not the unofficial congress-legislators data."

ElectionsData/usa/raw/senate/fetch_log.txt does log the king and sanders 403s. King's /about appears at lines 60-63 (attempts 1-4, "nothing kept"). Sanders' /about-bernie/ appears at lines 65-68. Line 72 is the NOTE that the two captures were read and then discarded. No line in that file mentions bioguide or Cloudflare.

`git grep -i --cached bioguide` over the whole index finds only two things: this sentence in senate_record.md and the bioguide hyperlinks inside raw/records/clerk_house_vote_2025_roll001.html. Neither is a fetch attempt. `git grep -i cloudflare` finds only Italy's ERRANDS E-46 entry, France's fetch_log line 18 and some CDN script tags. Nothing records a bioguide.congress.gov request or its status.

So the "Cloudflare 403" status is stated without any logged attempt behind it, and it sits in a paragraph headed "Logged and not kept". This does not affect the catalog, the tool or any check: the tool reads no bioguide data either way. The sentence's main claim, that the tool reads no other source, is still true. The defect is limited to the register asserting a host status that no log supports, so the grade stays at note.

**The skeptic's corrected fix.** Choose one of these. (a) Add the bioguide probe to raw/senate/fetch_log.txt in the format of the king/sanders lines, giving the time, 403, the URL tried and "nothing kept". This only works if the attempt really was made in the session; do not invent a time. (b) Otherwise, drop "(Cloudflare 403)" so the line reads "The tool reads no other source: not bioguide, not the unofficial congress-legislators data." With (b), think about moving that sentence out of the "Logged and not kept" paragraph, because bioguide was never logged.

### 18. Reading 4 dates Sinema's change by her seniority sentence, not by the page's party line

- **Lens:** docs-claims - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/senate_record.md:36

**The scenario.** The quoted words ('Effective January 3, 2023 ... she received her seniority and committee assignments through the Democratic Conference') date her organizing arrangement. The page's party line is 'Independent, 2023-present', and the change was announced on 9 Dec 2022. The day is the window's first day either way, so no count changes, but the quote does not say what it is cited for.

**The fix proposed.** Cite 'Independent, 2023-present' with the announcement of 9 Dec 2022, and say that the day is taken as the 118th's opening.

**The skeptic's evidence.** The raw page (wayback_senate_SenatorsWhoChangedPartiesDuringSenateService_20261004054837.html) reads: "Democrat, 2019–2022 Independent, 2023–present ... On December 9, 2022, she announced that she would be switching her affiliation to Independent. Effective January 3, 2023, at the beginning of the 118th Congress, she received her seniority and committee assignments through the Democratic Conference ...". In that sentence, "Effective January 3, 2023" applies to "she received her seniority and committee assignments", not to her becoming an independent. senate_record.md line 36 cites only the clause "Effective January 3, 2023, at the beginning of the 118th Congress" as its source for "Sinema an independent from 3 Jan 2023". Because the quote stops before the verb, it reads as if the page dated her independence to that day. The tool does the same: Tools/us_senate_prep.pl line 253 anchors 'AZ sinema' day => '2023-01-03' on qr/Effective January 3, 2023, ... she received her seniority/. No count changes. The window opens on 2023-01-03, and line 478 records a change only when i_from gt $window, so the catalog comes out the same whether the day is the announcement (9 Dec 2022), any day in 2023-present before the window, or 3 Jan 2023. The day is right in effect, but the quote cited for it says something else and the reading does not say it chose the day. That is a wording defect, not a data defect.

**The skeptic's corrected fix.** In reading 4, cite the page's party line "Independent, 2023–present" and the announcement of 9 Dec 2022 as the source for the change. Say that the day is taken as the 118th's opening (3 Jan 2023), the window's first day, so any earlier day counts the same. Keep the seniority sentence only for reading 5 (the caucus). Optionally, add a matching comment at us_senate_prep.pl line 253 saying what the anchored words are and what they date.

### 19. Tool header comment is garbled

- **Lens:** docs-claims - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_senate_prep.pl:23

**The scenario.** 'an appointment of 2023 on whose oath is not the New Senators page's' reads as a typo for 'an appointment from 2023 on whose oath ...'.

**The fix proposed.** Reword the comment.

**The skeptic's evidence.** Staged Tools/us_senate_prep.pl line 23: "#   - (the appointments) an appointment of 2023 on whose oath is not the New Senators page's, or whose appointment follows its oath;". The code it describes (lines 225-230) reads only rows whose appointment year is 2023 or later (`next unless $td[1] =~ /(\d{4})/ && $1 >= 2023;`). It then fails on an oath that differs from the New Senators page's (`problem(... unless ($x->{sworn} // '') eq $sworn)`) and on an appointment dated after its oath (`if $appointed gt $sworn`). So "of 2023 on" means "from 2023 onward", but with no comma or "onward" the line reads as "on whose oath", which is garbled. The meaning of the comment is correct and only the wording is broken, so this is a note and not a false claim. The reviewer's suggested reading "from 2023 on whose oath" has the same problem. The comment also leaves out a third failure the code has: an appointee of the window who matches no holder (line 228).

**The skeptic's corrected fix.** Reword line 23 as: "(the appointments) an appointment of 2023 or later matching no holder of its state, or whose oath is not the New Senators page's, or whose appointment follows its oath;"

### 20. A declared party change's day is never held to the Changed Parties page: a wrong Manchin or Sinema day passes and writes

- **Lens:** coverage - **reviewer:** defect - **skeptic:** defect
- **Where:** Tools/us_senate_prep.pl:254

**The scenario.** The header (lines 25-26) says the tool dies when 'either declared change's day [is] not the page's'. The code never does this. %change{...}{day} is a separate constant, and line 259 only checks that the hand-written `said` regex (which hard-codes 'June 5, 2024') still matches the page. Nothing compares `day` with the date in those words. I ran it on a copy: I changed only `'WV manchin' => { day => '2024-06-05'` to '2024-06-06'. The tool exited 0 and wrote all five outputs. The class pages, both Directory counts and the Democrats' lists all agreed, because none of their dates fall between 5 and 6 June 2024. The only visible effect was that [SEN-DIV]'s 118th stretch moved to 'holds 2024-06-06 to 2024-08-19'. senate_seats.csv, senate_changes.csv and senate_division.csv would carry the wrong day. GeneratedCatalogCheck.CheckUsSenate cannot see it either: it re-derives from IndependentFrom and does not pin it. Mutation case c01 ('Manchin's change a day off its words') mutates the page, not the tool's day, so the suite never tests this direction.

**The fix proposed.** Take the day from the words: give each `said` regex a capture of its date, run day() on it, and refuse unless it equals $change{$k}{day}. For Sinema the words give 'Effective January 3, 2023'. Add a TOOL mutation that changes only the declared `day` (for example Manchin 2024-06-06) and expects the new mismatch.

**The skeptic's evidence.** I could not refute this. The header at Tools/us_senate_prep.pl:25-26 says the tool dies on "either declared change's day not the page's". The code at :252-256 declares `%change`, and each entry holds two separate constants, `day => '2024-06-05'` and `said => qr/On June 5, 2024, .../`. The only test against the page is at :259: `problem(...) unless $t =~ $change{$k}{said}`. Nothing compares `$change{$k}{day}` with the date written in `said` or on the page. At :274 `$x->{i_from} = $c->{day}` then feeds party_on, the roster, senate_changes.csv, senate_seats.csv and the [SEN-DIV] stretches.

I reproduced it on a temp copy (raw/senate, raw/records and the tool). I changed only Manchin's `day` to '2024-06-06'. The tool exited 0 with nothing on stderr and wrote:
- senate_changes.csv: `2024-06-06,WV,1,independent,Joe Manchin III,the Changed Parties page`
- senate_seats.csv: `WV,1,Joe Manchin III,D/I,2024-06-06,...`
- senate_division.csv: `118,47,49,4,2024-06-06,2024-08-19`

No anchor falls between 5 and 6 June 2024. The 2024 Directory closes in April, and neither the class pages nor the Democrats' captures fall between those days, so nothing catches the shift. Mutation c01 (us_senate_mutations.sh:90) edits the page's words, which the regex catches. The opposite case, where the tool's declared day disagrees with its own regex, is never tested.

The committed outputs are correct today, because '2024-06-05' agrees with "June 5, 2024". The defect is that the header claims a refusal the code does not make. A later edit to `day` alone, or an edit to the Sinema entry, would write a wrong date without any error. That fits the rubric's "false claim in the docs / a check that can pass on wrong data".

**The skeptic's corrected fix.** Make the regex the only place the date is written. Give each `said` a capture of its date: Sinema `/Effective (January 3, 2023), at the beginning of the 118th Congress/` and Manchin `/On (June 5, 2024), he switched his affiliation to Independent/`. On a match, run the existing day() parser on `$1` and refuse with `problem("Changed Parties: $k declared from $change{$k}{day}, the page says <parsed>")` unless the two are equal. Better still, set `$change{$k}{day}` from the capture and drop the hand-written constant. Then add a tool mutation that changes only the declared `day` (Manchin 2024-06-06). If the constant is removed, the mutation should instead change the date inside the regex. Expect the new mismatch line or "the words ... not found".

### 21. Record says the suite 'proves those refusals', but several refusals the record and header list have no case

- **Lens:** coverage - **reviewer:** defect - **skeptic:** minor
- **Where:** ElectionsData/usa/senate_record.md:101

**The scenario.** Record line 99 lists the refusals, and line 101 says the mutation suite 'proves those refusals ... 30 of 30 caught'. Several refusals on that list have no case: a state-page row without a class (prep.pl:161), without a party in parentheses (163) or without a day (day() die, 114); and '[SEN-DIV] missing a line' (427). The tool header lists more refusals with no case: two pages for one state (one(), 75); a page in no SHA256SUMS.txt by page()'s own check (66; p02 reaches one()'s '0 saved pages match' instead, and only the two hard-coded PDF paths can reach line 66); a state not holding exactly two classes or a third class (170) and the 33/33/34 split (179); a New Senators entry matching two holders (187); a Third or Minor Parties span mismatch (289); and the Democrats'-list direction 'listed and holds no D or I seat' (329). Refusals with no case that the header does not list either: New Senators party disagreement (209), sworn twice (205), entry not read (203), the record's reach (310), the Directory changes-table read count (415), its 'no table / no Sworn in heading' (400-401), the closing date or count not found (364, 368), class listings not 3 (377), the 'summary line not found' check (268), a declared change labelled other than D, I (275), and the [SEN-DIV] 119th notes not found (433, 438).

**The fix proposed.** Either add the cases (at least: a row with no party, a second page for one state, a state with a third class, the reach (TOOL: $reach off by a day), a Directory changes row unread, a Third or Minor Parties start year off, a Democrats' list naming a Republican, and a New Senators party flip), or reword line 101 to say which refusals the 30 cases prove.

**The skeptic's evidence.** The finding holds, though narrower than stated. I'd grade it minor, not defect: it overstates the test coverage, and no catalog value is wrong.

**What line 101 claims.** senate_record.md:101 says "`Tools/us_senate_mutations.sh` proves those refusals on a copy ... **30 of 30** caught". "Those" points back to line 99's full list. The suite also calls itself "THE US SENATE GENERATOR'S FAILURE PATHS, PROVED" (mutations.sh:2).

**The 30 cases.** I counted them in us_senate_mutations.sh:
- p01-p03, the digests and a missing page
- s01-s04: an undeclared label (Kim DFL), an overlap, a last holder ended, a footnote with no note
- o01-o03
- a01, a02, x01, x02
- c01-c03
- k01, k02
- n01-n06
- v01, v02
- t01-t03

**Refusals on line 99's own list that have no case:**
- **A state-page row without a class, a name, a party or a day.** The code is prep.pl:161 `defined $class or problem(...)`, prep.pl:163 `... or do { problem("$st: '$who' has no party in parentheses"); next; }`, and day()'s `die "$what: not a day"` (prep.pl:114). s01 only reaches the undeclared-label branch at prep.pl:164.
- **"[SEN-DIV] missing a line".** The code is prep.pl ~427: `problem("[SEN-DIV]: lines for ... not the 117th-119th") unless keys %div == 3`. v01 changes a count, so it hits the "holds on no day" check instead.
- **"A Democrats' list not exactly the roster's Democrats ...", one direction only.** k02 removes a Democrat from the list. No case puts a Republican or a non-member onto the list.

So "proves those refusals" is literally false for at least these items.

**Where the finding overreaches.** Its second and third groups are header-only refusals: two pages for one state (one(), prep.pl:75), the 33/33/34 split (179), the third class (170), and similar. Line 101 never claims the suite proves those. They are gaps in coverage, not false statements. The same goes for the "in no SHA256SUMS.txt" branch at prep.pl:66: the record's list says only "a page off its digest", and p01 covers that.

**Why minor.** The tool's refusals are all present in the code. The data and the control's bytes are unaffected, and no check can pass on wrong data because of this. The fault is that the record's verification sentence claims more coverage than the suite gives. That is a later-trip gap, not a wrong date, seat, party or count.

The house record shows the stricter wording to follow. house_districts.md:29 says the suite "proves the failure paths" and then lists each case by name, so that record does not overclaim.

**The skeptic's corrected fix.** Choose one of two fixes.

(a) Reword senate_record.md:101 so it lists what the 30 cases prove, by group, the way house_districts.md:29 does. Say plainly that the following refusals have no case:
- a row with no class, party or day
- [SEN-DIV] missing a line
- a Democrats' list naming a non-Democrat
- the header-only ones: two pages for one state, a class count other than two, the 33/33/34 split, a New Senators entry matching two holders, a New Senators party disagreement, the Directory changes table not read, and the 119th notes not found

(b) Add cases for the line-99 items at least:
- strip "(D)" from one state-page row (re-sum it), expecting "has no party in parentheses"
- delete the 117th Congress block from [SEN-DIV], expecting "[SEN-DIV]: lines for 118 119, not the 117th-119th"
- add a Republican's MemberBox to the 2026 Democrats' list, expecting its extra-member mismatch
- optionally, a duplicate state page in SHA256SUMS.txt (expecting "2 saved pages match") and a row moved outside its class

Then update the count.

### 22. The Third or Minor Parties check never checks party or the span's end, so the header's 'not independent on those days' refusal does not exist

- **Lens:** coverage - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_prep.pl:288

**The scenario.** Header lines 26-27: dies on 'a senator the Third or Minor Parties page names an independent in the window whose party here is not independent on those days'. Lines 286-289 only find the holder and compare the start year. They never read $x->{party}, and never compare $to with the roster. I ran two experiments on copies. (1) King relabelled (D) on the ME page: the run failed only through the anchors (Class I page, both Directory counts, [SEN-DIV]); no 'Third or Minor Parties' message. (2) King's span on the Third page changed from 2013-present to 2013-2024: exit 0 and all outputs written, though the roster keeps him independent through 2026. The anchors happen to cover King, but an independent whose span disagrees between anchor dates would pass.

**The fix proposed.** In the loop, refuse unless party_on($x, d) is 'I' on the span's first and last days inside the window, and the span's end year equals substr($x->{left},0,4) (or 'present' with left '-'). Add a mutation for each direction.

**The skeptic's evidence.** I could not refute it. In the staged Tools/us_senate_prep.pl, lines 26-27 of the header say the tool dies on "a senator the Third or Minor Parties page names an independent in the window whose party here is not independent on those days, or the reverse". The loop at lines 280-290 does none of the forward half:
  my ($name, $st, $from, $to) = ($1, $2, $4, $5);
  next if $to ne 'present' && $to < 2023;          # the only use of $to: it filters rows, it is never compared to the roster
  my $x = find_holder(...);  $named{$x} = 1;
  my $first = $x->{party} eq 'D/I' ? $x->{i_from} : $x->{took};
  problem(...) unless substr($first,0,4) == $from || ($from < 2023 && $first le $window);
- The loop never reads $x->{party} as 'I'. If the holder is labelled 'D', $first is just $x->{took}, and the start-year test passes on King's 2013.
- Nothing compares $to with $x->{left}.
- Line 291 does only the reverse half: every 'I' or 'D/I' holder must be named on the page.
- The mutation suite tests only that reverse half: c03, line 92/95 of us_senate_mutations.sh, removes King from the page. No case relabels a named independent or moves the end of his span.
- senate_record.md line 99 claims only "an independent off the Third or Minor Parties page", which is the reverse half and is accurate.
So the false part is the tool's own header, which promises a refusal that is not in the code.

Why minor and not defect: on the committed data the catalog stays right. The independents are King, Sanders and Sinema, and each holds on every anchor date: the class pages, both Directory counts, [SEN-DIV] and the Democrats' lists. That is why the reviewer's experiment 1 (King relabelled D) failed through the anchors. In experiment 2 only the source page was mutated, so the roster was still right; the tool just did not notice the page disagreeing with it. Wrong data would only get through for an independent whose label or end falls entirely between anchor dates, and none does in the 118th or 119th Congress. The gap is real for any later item that reuses the tool.

**The skeptic's corrected fix.** In the Third or Minor Parties loop, after find_holder:
- Refuse unless party_on($x, $d) eq 'I' on the span's first day inside the window, max($from-01-01 or $first, $window).
- Refuse unless it is also 'I' on the span's last day inside the window: $x->{left} minus one day, or the last day the record reaches if left is '-'.
- Refuse unless ($to eq 'present') == ($x->{left} eq '-'), and when $to is a year, $to == substr($x->{left},0,4).
Add two mutations to us_senate_mutations.sh: King's state-page label I->D, which must die naming 'Third or Minor Parties' before the anchors (or check that message is among the MISMATCH lines), and King's span on the Third page changed 'present'->'2024'. If the code is not changed, cut the header's lines 26-27 back to what the code does: the page's start year, and the reverse.

### 23. The Directory changes-table count counts only rows its own regex could read, so a row with another reason word drops silently

- **Lens:** coverage - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_prep.pl:404

**The scenario.** $rows counts \b(Resigned|Died)\b, so the 'read n of rows' refusal (line 415) only catches a Resigned or Died row the \G regex fails on. I ran this on a copy, wrapping pdftotext with s/ Died Laphonza/ Deceased Laphonza/ in the 2024 edition. Then rows=1 and n=1 (Sasse only), the run exited 0 and wrote everything, and Butler's oath in the Directory was never held to the New Senators page. The same happens to any Directory row with another reason ('Expelled', 'Appointed', a party change) or a successor not yet sworn. Separately, the footnote strip `s/\n\d .*\z//s` (402) runs before the count, so a table cut at a wrapped line starting with a digit and a space would shrink n and rows together.

**The fix proposed.** Count the table's rows by their state-and-date shape (for example / [A-Z]{2} \d{1,2}-\d{1,2}-\d\d /), not by the reason word, and refuse any reason outside Resigned and Died. Add a wrap mutation for each.

**The skeptic's evidence.** The weakness is real, but only as a gap a later input could hit. Nothing is wrong with the committed data.

Tools/us_senate_prep.pl (staged): `my $rows = () = $tab =~ /\b(?:Resigned|Died)\b/g;`. The parsing loop also only accepts `(Resigned|Died)`, and the refusal is `problem(... "read $n of its $rows rows") unless $n && $n == $rows;`. Both the count and the parse use the same reason words. So a row with any other reason word is invisible to both, and the n == rows test cannot catch it. The reviewer's 'Died' -> 'Deceased' run behaves as they describe: rows=1, n=1, exit 0, and Butler's row is never checked against the Directory.

Why it does not reach the catalog today:
(1) The raw PDFs are pinned in SHA256SUMS. Running `pdftotext -raw` on both committed editions shows exactly 4 Senate change rows, and all 4 say Resigned or Died: Sasse/Ricketts and Feinstein/Butler in the 2024 edition, Vance/Husted and Rubio/Moody in the 2026 edition. All 4 are read, and senate_record.md lines 82-85 print them.
(2) The Directory oath is only a cross-check. Butler's oath is already held to the New Senators page and the Appointed Senators page elsewhere in the tool. Skipping the Directory row would not make the catalog wrong unless the oath were wrong as well.

One part of the finding is wrong. A row whose successor is not yet sworn still carries Resigned or Died, so it is counted in $rows. The `\G` match then fails on it, or its lazy `(.+?)` runs into the next row and swallows it. Either way n < rows and the tool refuses, so that case is not silent.

The footnote-strip concern (`s/\n\d .*\z//s`) is speculative. In -raw output the wrapped name lines ('James David ``JD''' / 'Vance.') and the row lines start with letters, and the only line that starts with a digit and a space is the footnote ('1 Appointed as an interim Senator.').

No mutation case covers an unread row (n01-n06 cover counts, an oath, the listings, the omission and the closing date). So the gap is untested, but no wrong data reaches the catalog.

**The skeptic's corrected fix.** Count the table's rows by their shape instead of the reason word, for example `my $rows = () = $tab =~ /\s[A-Z]{2} \d{1,2}-\d{1,2}-\d\d \S/g;`. Separately, refuse any reason word other than Resigned or Died with a named problem, so an unknown reason is a refusal and not a dropped row. Add one mutation that renames a reason word, for example 'Died Laphonza' -> 'Deceased Laphonza' in a wrapped copy of the pdftotext output, and expect 'read 1 of its 2 rows' or the unknown-reason refusal. A not-yet-sworn row needs no new case, because it is already refused.

### 24. No reverse check between New Senators' 'Appointed' and the Appointed Senators page

- **Lens:** coverage - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_prep.pl:228

**The scenario.** how_in='appointed' comes from the New Senators page (207), but only the Appointed page's own rows are checked (forward only). If the Appointed page lacked Helmy's (or Husted's or Moody's) row, nothing would refuse. senate_changes.csv's detail would silently lose the ' 2024-08-23' appointment day (line 475), and the header does not say this.

**The fix proposed.** After the Appointed loop, refuse any holder with how_in eq 'appointed' and took gt $window but no {appointed}. Add the case.

**The skeptic's evidence.** The finding holds, with one correction to the fix. Tools/us_senate_prep.pl (staged) sets how_in only from the New Senators page at line 207: `$x->{how_in} = $how =~ /Appointed/ ? 'appointed' : ...`. The Appointed Senators loop (lines 221-232) walks only that page's own rows: it calls find_holder, checks the oath (line 229) and checks appointed-before-sworn (line 230), then sets `$x->{appointed}`. No later loop asks whether every holder with how_in eq 'appointed' and took gt $window got an {appointed}. The header (line 23, 'an appointment of 2023 on whose oath is not the New Senators page's, or whose appointment follows its oath') and senate_record.md line 99 claim only the forward check, so neither makes a false claim.

The scenario is real. If Helmy's row (raw line 1228) is removed, nothing refuses. His state page's began (2024-08-23) differs from his oath (2024-09-09), but line 475 only reports that difference; it is not a check. Line 475 (`$x->{how_in} . ($x->{appointed} ? " $x->{appointed}" : '')`) would then print 'appointed; NJ's page has 2024-08-23' and silently drop the appointment day. No mutation case covers this: a01 and a02 only edit Helmy's dates.

The gap already shows in the real data. The New Senators page lists Adam Schiff as 'Appointed & Special election & General election' (raw line 518) and Andy Kim as 'Appointed & General election' (raw line 521). Neither has a row on the Appointed Senators page (grep finds only Helmy, Husted, Moody and Armstrong among recent rows). The tool gives both how_in='appointed' with no day, and the pasted run shows it: senate_record.md lines 71-72 read 'sworn Adam B. Schiff appointed; CA's page has 2024-12-08'. No reading in the record explains why Schiff and Kim have no appointment day.

The impact is limited to the detail text of senate_changes.csv. Seats are held from the oath (`took = sworn // began`, line 213), so the roster, the counts, the anchors and the C# tables do not depend on {appointed}. No wrong date, seat, party or count can result, which makes this minor, not a defect.

The proposed fix would refuse on today's data: Schiff and Kim are 'appointed' with no {appointed}, so a bare reverse check dies on the committed sources.

**The skeptic's corrected fix.** After the Appointed Senators loop, refuse any holder with how_in eq 'appointed', took gt $window and no {appointed}, except a DECLARED list: Schiff (CA) and Kim (NJ). Their New Senators entries read 'Appointed & ... election', and the Appointed Senators page does not list them. The declared list should itself be held to the page, refusing if a listed senator turns up on the Appointed page or is not 'appointed' on New Senators. Name the two in a reading in senate_record.md, so that the 'appointed' with no day on lines 71-72 is explained. Add a mutation case that deletes Helmy's Appointed row and expects '...appointed on the New Senators page, not on the Appointed Senators page'. Also add the reverse refusal to the header and to the record's refusal list.

### 25. The header omits refusals the code makes; the 'of 2023 on' wording is ambiguous

- **Lens:** coverage - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_senate_prep.pl:21

**The scenario.** The code refuses on things the header's list (lines 17-33) does not name: the New Senators party disagreement (209), sworn twice (205), entry not read (203), a class's last holder ended (175; s03 tests it), a CSV value with a comma (494; t03 tests it), and the record's reach (310). Line 23's 'an appointment of 2023 on' means from 2023 onward (line 225 reads >= 2023), but it reads like 'of the year 2023'.

**The fix proposed.** List them in the header, and write 'an appointment of 2023 or later'.

**The skeptic's evidence.** Staged Tools/us_senate_prep.pl, lines 17-33: "It dies, writing nothing, on:" is followed by a list, and that list leaves out several refusals the code makes. Line 203: problem("New Senators $congress: an entry not read"). Line 205: "sworn twice". Line 209: "New Senators: $name is $pp, ${st}'s page $x->{label}" (the party disagreement). Line 175: "its last holder has ended". Lines 179-180: the 33/33/34 class count. Line 187: find_holder's "matches N holders" (partly covered by "or two" in the header). Line 310: the record's reach is not the New Senators capture. Lines 377/391: the CDIR class listings (the header names only "the Congressional Directory's counts"). Lines 432-438: the [SEN-DIV] 119th notes (oath and resignation) not matched or not found. Line 464: the day's roster not 100 seats. Line 494: a CSV row whose field count differs from its heading. Line 259/268: the Changed Parties words or summary line not found. None of these are bugs: every one goes through problem() or die, and line 483 dies before anything is written. So the header is incomplete, not wrong about what it lists. It never says "only on", so no stated claim is false. That makes this a note, not a defect. On line 23's "an appointment of 2023 on", the code at line 225 is `$1 >= 2023` (onward) and line 227 skips oaths before $window. "of 2023 on" can be misread as "of the year 2023", so it is a wording ambiguity only. The behaviour is correct and matches the record's intent.

**The skeptic's corrected fix.** Add the missing refusals to the header list. These are: a New Senators entry not read, or its party not the state page's, or sworn twice; a class whose last holder has ended; the classes not 33/33/34; a day's roster not 100 seats; the CDIR class listings not the roster's; [SEN-DIV]'s 119th notes not the roster's or not found; the reach not the New Senators capture; an output row whose field count is not its heading's. Alternatively, mark the list "among others". Reword line 23 to "an appointment of 2023 or later".

### 26. Two cases' expected text is looser than the refusal they mean

- **Lens:** coverage - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_senate_mutations.sh:127

**The scenario.** t01 expects only 'a perl warning', which any warning anywhere in the run would print. It cannot pass on a missed mutation today, because the control prints no warning, but it would not show whether the warning came from line 307. p02 is labelled 'a page its sums do not list is never read' but proves one()'s pattern lookup ('0 saved pages match'), not page()'s 'in no SHA256SUMS.txt' refusal (prep.pl:66).

**The fix proposed.** Expect 'a perl warning: Use of uninitialized value in concatenation' for t01. Add a case that removes govinfo_cdir_2024-04-25.pdf's sums line and expects 'in no SHA256SUMS.txt - the tool reads saved pages only'.

**The skeptic's evidence.** I ran the staged suite (`bash Tools/us_senate_mutations.sh`, on a temp copy; the worktree matches the index). The control passes and 30 cases are caught. Both halves of the finding hold, and neither lets a case pass on wrong data.

t01: the expected text in Tools/us_senate_mutations.sh:127 is only "a perl warning". The tool turns every warning into a problem (`$SIG{__WARN__} = sub { problem('a perl warning: ', ...) }`, prep.pl:47). I ran t01 by hand and it prints `MISMATCH: a perl warning: Use of uninitialized value in concatenation (.) or string at Tools/us_senate_prep.pl line 307.` followed by `1 mismatch(es) - nothing written`. The control raises no warning, and the mutation keeps $reach equal to '2026-09-06', so line 307 is the only warning today. The case is sound. Its text just does not pin where the warning comes from.

p02 (`sed -i '/wayback_senate_SenatorsDiedinOffice_/d' $S/SHA256SUMS.txt`): `one()` searches the keys of %sums, not the directory (`my @f = sort grep { /^$g\/$re$/ } keys %sums;`, prep.pl:~74). So the case dies in `one()` with "0 saved pages match, not one". It never reaches `page()`'s `my $want = $sums{$rel} or die "$f: in no SHA256SUMS.txt - the tool reads saved pages only\n";` (prep.pl:66). That refusal can only fire where `page()` is called with a literal path, as in `pdf_text("senate/govinfo_cdir_$edition.pdf")` (prep.pl:363). The tool's header lists it as a refusal ("a page off its digest, or in no SHA256SUMS.txt", prep.pl:18), but no case in the suite proves it.

I checked that it is reachable. On a temp copy I deleted govinfo_cdir_2024-04-25.pdf's sums line. The tool exits 255, writes nothing (0 csv files), and prints `ElectionsData/usa/raw/senate/govinfo_cdir_2024-04-25.pdf: in no SHA256SUMS.txt - the tool reads saved pages only`. The refusal works; only its proof in the suite is missing.

p02's label is still literally true for pages found by pattern. The whole thing is a coverage and wording gap, not wrong data. Severity: note, with an argument for minor, since a declared refusal has no proving case.

**The skeptic's corrected fix.** Change t01's expected text to "a perl warning: Use of uninitialized value in concatenation (.) or string at Tools/us_senate_prep.pl line 307". Add a pages case, for example `p04() { sed -i '/govinfo_cdir_2024-04-25\.pdf/d' $S/SHA256SUMS.txt; }`, with the label "a page read by its name and not in its sums" and the expected text "govinfo_cdir_2024-04-25.pdf: in no SHA256SUMS.txt - the tool reads saved pages only". I checked that this mutation exits non-zero, writes nothing and prints exactly that line. Optionally relabel p02 as "a page its sums do not list is never found by its pattern". The suite then counts 31 cases.

## The first pass - refuted by the skeptics

- [tool-vs-sources] The Appointed filter goes by appointment year, so an appointment in late 2022 sworn in 2023 would be skipped - *The code does work the way the finding says, but nothing in the saved data can trigger it. The finding admits that too.

Staged Tools/us_senate_prep.pl:
- l.225 `next unless $td[1] =~ /(\d{4})/ && $1 >= 2023;   # appointed before the window's year: not read (old rows print "--" or footnoted days)` drops rows by appointment year. l.227 `next if $sworn lt $window;` (window = '2023-01-03', l.117) only runs after that.
- I listed every row of the committed Appointed Senators capture (ElectionsData/usa/raw/senate/wayback_senate_AppointedSenators_*.html) dated 2020 or later:
  - Loeffler: appointed 2020, sworn 6 Jan 2020.
  - Padilla: appointed 18 Jan 2021, sworn 20 Jan 2021.
  - In the window: Ricketts (12/23 Jan 2023), Butler (1/3 Oct 2023), Helmy (23 Aug/9 Sep 2024), Husted (18/21 Jan 2025), Moody (21/21 Jan 2025), Armstrong (24 Mar 2026), Graham (13/14 Jul 2026).
- No row has a 2022 appointment with a 2023 oath. The 118th Senate had no such appointee in fact either. The raw page is a fixed, hash-pinned capture, so no run of the committed sources can reach this path.

The rule is also stated in the line's own comment, so it is DECLARED rather than hidden.

If such a row ever existed, it would still not put a wrong roster figure in the catalog:
- The oath and the seat come from the New Senators page (l.206-207).
- A holder who began inside the window without an oath is still flagged (l.212).
- The only losses are the Appointed-page oath cross-check (l.229) and the appointment day in senate_changes' detail text (l.475 `$x->{appointed} ? " $x->{appointed}" : ''`).

That makes it a robustness remark at most: a note, not a defect.*
- [csharp-check] The caucus fold is never checked against the seat rows' completeness: an independent with no caucus, or a dem+rep total short of 100, passes - *The check does what the finding says: GeneratedCatalogCheck.cs Count() sets `demCaucus = s.Caucus == "D"` and the per-day test is `d + n[12] != o.DemCaucus || r != o.RepCaucus`. It never asserts caucus/caucus_by are filled on I and D/I rows, and never asserts dem_caucus + rep_caucus + vacant == 100. But the failing path the finding describes does not happen.

1. The tool cannot write an independent with no caucus. Tools/us_senate_prep.pl lines 335-343: every holder whose party is 'I' or 'D/I' gets caucus_by from the Democrats' list captures, or from the Changed Parties words (Manchin). Otherwise it calls `problem("...an independent of the window on no Democrats' list, his caucus unsourced")`. Line 483 (`if (@problems) { ... die "$n mismatch(es) - nothing written\n"; }`) then refuses to write anything. Mutation case k01 in Tools/us_senate_mutations.sh exercises exactly this refusal. The rows written (senate_seats.csv lines 16 and 57, plus Sanders and Manchin) all carry caucus D and a sourced caucus_by.

2. The emulated scenario needs a forger. Setting King's caucus to '-' and lowering dem_caucus on every named day means hand-editing senate_seats.csv and senate_on.csv. It also means editing the generated UsSenateRecord.cs rows and both recorded digests (SenateSeatSourceDigest, SenateOnSourceDigest), because ReadUsCsv compares each file's SHA-256 to the catalog's digest and SameRow compares every cell. A forgery that consistent defeats every generated-catalog check in the repo; it is not a path in this change. The check's role is the catalog against its CSVs plus re-deriving the roster from the seat rows. Sourcing is the tool's gate, and senate_record.md line 99 states that ("an independent with no caucus source" is a refusal). Line 103 describes the check's caucus test only as "the counts ... with the caucus the generated rows'", which is accurate, so no doc claim is false.

3. The "R-caucusing independent would FAIL" scenario is not reachable. The tool hard-codes `$x->{caucus} = 'D'` (line 342) and writes rep_caucus as `$k->{R}` (line 469), so the check mirrors the tool exactly. If a later item taught the tool an R caucus, the check would fail loudly rather than pass wrong data. That is the safe direction, not a defect.

What remains is a cheap hardening (a note): on I and D/I rows require caucus in {D,R} with caucus_by not '-', and '-' on every other row. Derive RepCaucus as r plus the R-caucus independents, and assert dem_caucus + rep_caucus + vacant == 100. That would make the check carry R-US10 (a)'s "sourced" itself instead of relying on the tool's refusal.*
- [docs-claims] The plan's Built line says 'the caucus sourced' while Sinema's caucus is a reading put to Elias, and it does not name the caucus-source departure - *The finding reads the Built line as if nothing followed it. In the staged USA_STAGE_PLAN.md, the lines right after it (~359-365, under "*Design, DECLARED (§792; `senate_record.md`, its readings):*") say: "*an independent's caucus* by the Senate Democrats' own list on its capture day, or by the Senate's own words where the list is silent - Sinema's a reading put to Elias (R-US10)". So the plan already names both the caucus source (the Democrats' list, not senate.gov) and Sinema's reservation, in the same §792 block. A reader checking the Done-when would see both.

"The caucus sourced" is also true as written. Every independent has a dated source:
- senate_record.md, register line 25: the democrats.senate.gov captures of 2024-03-18 and 2026-09-05.
- Reading 5: King and Sanders by both lists, Sinema by the 2024 list, and Manchin by the Changed Parties page's own words.
- The run's lines 88-90: "caucus: AZ Kyrsten Sinema (D/I) with the Democrats, by the Senate Democrats' list of 2024-03-18".

The Done-when asks for "the caucus sourced or BILLED", and R-US10 (a) asks for "sourced and dated". Both are met. Sinema's open question is not whether she has a source (she is on the list). It is which unit to count her in, given the Senate's "would not participate in either party caucus". The R-US10 "Read at US-12" paragraph and reading 5 put that question to Elias openly.

The only real point left is wording. The US-12 bullet's "from the Senate's own record" was met by democrats.senate.gov plus the Senate's Changed Parties page. That is named in the Design bullet but not called a departure the way the base change is. Debatable at most, since the Democrats' page sits under senate.gov and Manchin's caucus does rest on the Senate's own page.*
- [docs-claims] The Directory's 2026 edition is called 'the Directory of 1 Oct 2025', but its govinfo package is CDIR-2026-02-20 - *The wording is accurate, and it names both editions the same way. The 2026 edition's own text says "Closing date for compilation of the Congressional Directory was October 1, 2025" (pdftotext of raw/senate/govinfo_cdir_2026-02-20.pdf, line 22). Its MODS gives dateIssued 2026-02-20 and the package id CDIR-2026-02-20.

The 2024 edition is also named by its closing date ("April 25, 2024"), which happens to be the same as its dateIssued of 2024-04-25. So "the Directory of 25 Apr 2024 and of 1 Oct 2025" names both editions by closing date. That is the date the tool actually anchors on: us_senate_prep.pl:361 reads `for my $ed (['2024-04-25', '2024-04-25'], ['2026-02-20', '2025-10-01'])` (edition, closing), and it keys `$cdir_said{$closing}`. The counts are the roster on the closing day, so naming the edition by that day is the meaningful choice.

The plan row at USA_STAGE_PLAN.md:1011 points the reader to "`senate_record.md`'s register". senate_record.md:24 gives the file name `govinfo_cdir_2026-02-20.pdf` and says both closing days. The MODS saved beside the PDF carries the govinfo URI CDIR-2026-02-20. Someone re-fetching gets the package id from the register the row cites, so nobody is led to a non-existent CDIR-2025-10-01.

At most this is optional wording polish: adding "(issued 20 Feb 2026)" would do no harm. It is not a false claim or a real gap.*
- [coverage] Reviewer side effect: one of my scratch commands probably deleted an older scratchpad folder 'e1' (and possibly 'base') - *I could not refute or confirm the deletion directly, and the claim is not a finding against commit 792 in either case. It is about the reviewer's own scratch work. Nothing in the staged change or anywhere in G:/UNITY/Projects/PoliSim is involved, so it cannot be a defect in the catalog, the checks or the docs.

What the scratchpad shows, read-only (ls/find with full timestamps):
- `scratchpad/e1/` and `scratchpad/base/` both hold only today's copies, made 2026-10-06 10:48: `Assets/Scripts`, `ElectionsData/usa` and `Tools/us_senate_prep.pl`. Nothing older survives in them, so I cannot prove either way whether an older `e1/` existed.
- The circumstantial evidence goes against the claim:
  - The 2026-10-04 E1 work is still in the scratchpad as top-level files, not in an `e1/` folder: `e1_author.md` (21:07), `e1_author2.md` (21:45), `e1_review.json` (20:59) and `e1_review2.json` (21:41).
  - E2's 2026-10-04 work follows the same pattern: `e2_author*.md`, `e2_pass2.md`, `e2_pass3.md` and `e2_review.json` are top-level files. `scratchpad/e2/` and `e3/`, which the reviewer says it did not rm, contain only today's 10:50 copies and no older files. So no `e2/` or `e3/` folder existed before today, or had anything in it.
  - The only folder in that series with older content is `e4/`. It holds `b1_backtest.pl`, `csvlib.pl`, `explore.pl`, `json.pl`, `json_out.txt` and `lists.pl`, all dated 2026-10-04 around 20:15-20:22, and they are intact.
- Taken together, the 2026-10-04 E-series work was kept as files, apart from E4's scripts, so an older `e1/` folder probably never existed. If one did, it was session scratch, which the project has no record of needing.*

## What was done about the pass

Twenty-six findings survived; every one was acted on. The tool was changed in seven places, the catalog check rewritten in two, the suite grown from 30 cases to 39, and the record, the register and the plan corrected.

- **The C# check could pass on a wrong catalog (6, 7, 8).** It compared only each `[SEN-DIV]` stretch's ends and the days beside them, and it never held a seat to one holder on a day. Now each seat's holders are put in order (each took before he left, left no later than the next took, only the last still serving) and never two on one day; the stretches are derived again over every day of each Congress to the record's reach - the day named so, not the last row - and held to the rows one for one; only the 118th and 119th Congresses are allowed.
- **A declared party change's day (20).** It was typed beside the page's words and never held to them. Now the day is captured from the words themselves; the typed day is gone.
- **The Changed Parties page (3).** The summary-line regex missed eight of its 23 chapters. Now every chapter of its contents is read by number, and a senator of the window among them who is not a declared change stops the run.
- **The Third or Minor Parties page (1, 22).** The tool never checked that a senator it names an independent is one here. Now he must be an independent on his span's first and last days inside the window, and his service must end in its last year. Cases c04 (Welch named), c05 (Sinema's span ended early).
- **The Directory (15, 23).** Its count's "as of" day is held to its closing day; its table's rows are counted by their shape, and a reason other than a resignation or a death stops the run. Cases n07, n08. Its "chosen" days now print beside the Appointed Senators page's (12).
- **The appointments (24).** The reverse check exists: an appointee by the New Senators page whom the Appointed Senators page does not list stops the run, but the two DECLARED - Schiff and Kim, elected to the next term and then appointed to the old one's last weeks - each held to what it says. Case a03.
- **The reach (13).** The tool now applies it: a row beginning after it without an oath is set aside, a service ending after it is written as serving, both printed.
- **The caucus (11).** Declared as applied over a senator's whole service in the window; Sinema's row carries its open reading in `caucus_by`.
- **The descriptions (2, 19, 25).** `Took` is the oath only for a senator new in the window, the state page's day otherwise (the C# summary, the record); the tool's header lists every refusal it makes.
- **The record and the register (4, 9, 10, 12, 14, 16, 17, 18, 21).** Reading 4 sources Sinema's day from the page's party line; reading 6 names the Directory's appointment-day differences; the D / R / I order made one; `records_by_date.md`'s table 1b marks `[SEN-DIV]`'s lines as undated with their dated reality, and its DERIVED "47 + 4" corrected; G6 and "Not reached" name all four seats held by appointment; the suite's count dated and its cases listed by group, the refusals without one named; the bioguide line limited to what was logged.
- **The suite (21, 26).** Cases added for a row without its party (s05), a New Senators party disagreement (o04), `[SEN-DIV]` missing a line (v03), a page read by name and not in its sums (p04); the perl-warning case's expected text made exact; the Schatz case requires both of its messages. 39 of 39.
- **Not acted on as code (5):** the class-page anchor stays keyed on state and surname - it never meets the two South Carolina Grahams on one page - and the tool's comment says the initial is find_holder's alone.
