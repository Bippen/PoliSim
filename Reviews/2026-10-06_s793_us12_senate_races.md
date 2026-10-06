# Review - §793, PS-6 US-12 part two: the Senate races of 2018 and 2024, and Nebraska's of 2020 (2026-10-06)

A workflow review (`polisim-staged-review`) of the change that builds US-12's race half: `Tools/us_senate_races_prep.pl`, which reads every Senate race of 2018 and 2024 and Nebraska's Class II race of 2020 from the Clerk's statistics twice and holds them to the recapitulations, the FEC's 2018 sheet and §792's roster; the catalog (`senate_races.csv`, `UsSenateRaces.cs`); `Tools/us_senate_races_mutations.sh`; `GeneratedCatalogCheck.CheckUsSenateRaces`; the record's race half in `senate_record.md`; the plan's US-12 line. Not required by the tier (no money path); run because R-US19's measurement and US-16's Senate count rest on this catalog. Every report below is verbatim; what was done follows.

## The first pass - confirmed (verbatim)

One pass over the staged tree, 30 agents: three lenses (the tool against its sources, the C# check and the generated part, the record and the suite), every finding put to a refute-first skeptic.

### 1. The record says Nebraska's 2024 special is held to 3 Jan 2025, but the code holds it to 9 Dec 2024, and the class-2 branch can never run

- **Lens:** tool-vs-sources - **reviewer:** defect - **skeptic:** defect
- **Where:** ElectionsData/usa/senate_record.md:120

**The scenario.** Reading 10 says Nebraska's 2024 special is checked on 3 Jan 2025. In Tools/us_senate_races_prep.pl:357 (and the same expression at Assets/Editor/GeneratedCatalogCheck.cs:1056), the test `term eq 'unexpired' ? '2024-12-09'` comes before `class == 2 ? '2025-01-03'`. The 2024 NE row has class 2 and term unexpired, so it is held to 2024-12-09. The 2025-01-03 branch never runs, because no 2024 race is class 2 and full. The record therefore makes a false claim. The outcome is still Ricketts, because senate_seats.csv has him from 2023-01-23 with no end date. So the check only confirms that the appointee was still seated, on a different day from the one the record names. The tool's comment at 355-356 does not mention Nebraska at all.

**The fix proposed.** Either put the class-2 test before the unexpired test in both the tool and the C# check, so Nebraska's special really is held to 2025-01-03, or correct reading 10 to say 9 Dec 2024 and delete the dead branch. State in the record that for Nebraska either day finds the 2023 appointee.

**The skeptic's evidence.** Tools/us_senate_races_prep.pl:357: `my $d = $x->{year} < 2024 ? '2023-01-03' : $x->{term} eq 'unexpired' ? '2024-12-09' : $x->{class} == 2 ? '2025-01-03' : '2025-01-21';`. Assets/Editor/GeneratedCatalogCheck.cs (CheckUsSenateRaces) has the same expression: `r.Year < 2024 ? "2023-01-03" : r.Term == "unexpired" ? "2024-12-09" : r.Class == 2 ? "2025-01-03" : "2025-01-21"`. In senate_races.csv the only 2024 row with class 2 is `2024,NE,2,unexpired,...,Pete Ricketts,R,-`. It matches the unexpired test first, so it is held to 2024-12-09. No 2024 row is both class 2 and full, so the '2025-01-03' branch is dead code in both the tool and the check. senate_record.md:120 (reading 10) says "Nebraska's 2024 special on 3 Jan 2025". That is a false statement of the day the check uses. The tool's own comment at 355-356 names only California's special for the 9 Dec day. The outcome does not change: senate_seats.csv:78 has Ricketts in NE class 2 from 2023-01-23 with no end date, so he holds the seat on either day, and no winner, class or race is wrong. The defect is the record's false claim about what the check does, plus the dead branch.

**The skeptic's corrected fix.** Choose one of two fixes. (a) Move the class-2 test ahead of the unexpired test, in the tool and in GeneratedCatalogCheck.cs alike: `year<2024 ? 2023-01-03 : class==2 ? 2025-01-03 : term eq unexpired ? 2024-12-09 : 2025-01-21`. Nebraska's special is then really held to 3 Jan 2025, as the record says. (b) Change reading 10 to say that Nebraska's 2024 special, like California's, is held to 9 Dec 2024, delete the class-2 branch from both expressions, and extend the comment at prep.pl:355-356 to cover Nebraska. With either fix, add one sentence to the record saying that on either day the 2023 appointee Ricketts holds the seat.

### 2. CheckUsSenateRaces claims 'every winner leading', but it only checks that the winner's class has more than zero votes

- **Lens:** tool-vs-sources - **reviewer:** defect - **skeptic:** defect
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:1054

**The scenario.** The summary at line 1021 says 'every winner leading his race's candidates by class', and senate_record.md:133 says 'every winner leading in his class's votes'. The code at 1054 only fails when the winner's class column is 0 or less. Example: a CSV row where winner_class is R and votes_r is smaller than votes_d (say a regenerated row with D and R swapped while the winner field is left alone). The check passes, as long as the CSV digest and the catalog agree, which they do when the generator produced both.

**The fix proposed.** Implement the claim: the winner's class column must be at least every other class column. That holds for all 71 current rows, the MS special included at its printed figures. Otherwise, reword the summary and the record to say 'the winner's class holds votes'.

**The skeptic's evidence.** The staged GeneratedCatalogCheck.CheckUsSenateRaces does not do what its summary says. The summary claims "every winner leading his race's candidates by class...". The only vote test in the loop is:
  long lead = r.WinnerClass == "R" ? r.VotesR : r.WinnerClass == "D" ? r.VotesD : r.WinnerClass == "I" ? r.VotesI : r.VotesOther;
  if (lead <= 0) { wrong.Add(... "the winner's class " + r.WinnerClass + " holds no votes"); }
Nothing compares the winner's class column with votes_r/votes_d/votes_i/votes_other. ElectionsData/usa/senate_record.md:133 repeats the claim ("every winner leading in his class's votes"), so the record overstates the cheap bar too.

The scenario holds. The other checks are SameRow (CSV against catalog), the digest (taken from the same generator run), the per-year counts and Class I set, and the seat holder by last name. None of them looks at which column is largest. A row with winner_class R and votes_r < votes_d passes if the generator wrote it that way.

Upstream the risk is lower but not zero. In the tool, the winner is the candidates' plurality ($x->{winner} = $c[0], line 265), and its class is held to the FEC W party at line 340. That only covers 2018. For 2024 and NE 2020, a wrong class on the winner is caught by nothing in the tool or the bar: the seat check compares last names only.

Two things it does not affect: the success line printed by sb.Append claims only the seat-holder test, not leading, and the committed data is not shown to be wrong. So the defect is the false claim about the check (the rubric counts a false claim as a defect), not a wrong figure. On the fix: requiring the winner's class column to be at least every other class column fits all the current rows. In the MS 2018 special the R column holds Hyde-Smith and McDaniel, which is more than Espy's D. The alternative is to reword the summary and record line 133 to "the winner's class holds votes".

**The skeptic's corrected fix.** In CheckUsSenateRaces, replace the lead<=0 test with: long[] cols = { r.VotesR, r.VotesD, r.VotesI, r.VotesOther }; if (lead <= 0 || cols.Any(c => c > lead)) wrong.Add(r.Year + " " + r.State + ": the winner's class " + r.WinnerClass + " " + lead + " does not lead every class"). Alternatively, reword the <summary> and senate_record.md:133 to "the winner's class holds votes". Optionally add a mutation case that swaps votes_r/votes_d in the CSV and catalog together.

### 3. The Mississippi 2018 special row mixes runoff and first-round figures, and the footnotes are never read

- **Lens:** tool-vs-sources - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_races_prep.pl:235

**The scenario.** votes_r and votes_d for 2018 MS class 2 add the finalists' 27 Nov runoff figures (Hyde-Smith 486,769, Espy 420,819) to the eliminated candidates' 6 Nov figures (McDaniel 154,878, Bartee 13,852). So the row's columns are not the count of any one election. The same saved volume prints the finalists' 6 Nov counts in its footnotes ('This vote count is from the runoff ... Mike Espy received 386,742 votes in the general election'; Hyde-Smith 389,995), and the FEC sheet the tool already reads has them in GENERAL VOTES. The tool strips the leading digit as a mark and stores it in $e->{mark}, but never uses it. It never reads the footnote text either, so 'these are runoff figures' rests on the declaration alone. The M flag declares the mix, but any later reader taking shares from that row gets a hybrid.

**The fix proposed.** Read the two footnotes, and fail unless each mark number has a footnote naming that candidate and 'runoff'. Then either write the 6 Nov round as the row, with the runoff only deciding the winner, or say plainly in the CSV header and the record that this row's vote columns are not one count.

**The skeptic's evidence.** The mechanism is as the finding describes, but part of it is overstated.

What holds:
- Tools/us_senate_races_prep.pl:148 declares `%marked = ('2018 MS 2' => 2)`. Line 235 removes a leading digit from a figure that is not grouped by thousands and stores it in `$e->{mark}`. Nothing reads `$e->{mark}` after that. Line 261 only checks that the declared number of marks was used up.
- The footnote text is never read. read_volume (lines 151-191) treats prose as `next; # prose between entries`. pdftotext -raw of clerk_statistics2018.pdf prints the footnotes at lines 1933 and 1935 ("1This vote count is from the runoff election held on November 27, 2018. Mike Espy received 386,742 votes in the general election..."; "2... Cindy Hyde-Smith received 389,995..."). So the tool never checks that mark 1 goes with Espy and mark 2 with Hyde-Smith, or that either note says "runoff".
- The row mixes two counts. senate_races.csv:31 is `2018,MS,2,unexpired,641647,434671,...,M`. That is 486,769 + 154,878 and 420,819 + 13,852: the finalists' 27 Nov runoff figures plus the eliminated candidates' 6 Nov figures.

What is overstated:
1. The mix is already stated plainly in the output, which is the finding's own fallback fix. The CSV header's M flag text (line 380) reads "Mississippi's 2018 special: its two finalists at the 27 Nov runoff, their figures beside the others' 6 Nov ones as the Clerk prints them". The winner line reads "the candidates' plurality (Mississippi's special at its runoff)". senate_record.md reading 5 (line 115) says the same, and calls the marks DECLARED.
2. "These are runoff figures" does not rest on the declaration alone, at least for the winner. In the FEC check, lines 327 and 341 take the GE RUNOFF ELECTION VOTES (MS Senate) column when it is filled. They fail unless the W's votes equal the Clerk's winner total. So Hyde-Smith's marked figure is cross-checked against the FEC's runoff count. Only Espy's figure, and the mark-to-footnote pairing, go unchecked.

No figure, class, winner or race in the catalog is wrong; it carries what the Clerk prints, and says so. What remains is a verification gap: the footnote text and the stored mark digits are never checked against each other.

**The skeptic's corrected fix.** Keep the row as the Clerk prints it; the CSV header and record reading 5 already say it is two counts. To close the gap, read the footnote lines that follow the Mississippi Senate listing in both readings (pattern `^(\d)This vote count is from the runoff election held on (\w+ \d+, \d{4})\. (.+?) received ([\d,]+) votes in the general election`). For each `$e->{mark}`, fail unless a footnote with that digit exists, says runoff, and names the candidate whose label carries that mark. Optionally, also check Espy's marked figure against the FEC runoff column for his row, not only the W row. Do not substitute the 6 Nov figures: the winner is decided at the runoff, and the FEC W check depends on the runoff votes.

### 4. Named write-in candidates sit in votes_non, which contradicts the header's 'every write-in candidate' is other

- **Lens:** tool-vs-sources - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_races_prep.pl:141

**The scenario.** The noncand list includes 'Others', 'Write-in', 'Other Write-ins' and 'Write-ins', so whole lines the Clerk prints without a name go to votes_non. The FEC sheet the tool reads names candidates inside those lines. Maine 2018 'Others' 64 is the FEC's 'Riley, James N., Jr.' at 64. MN 2018 special 'Write-in' 1,101 is Emery 6, Kuitu 3, Nelson 2, Hartnett O'Connor 1 and Scattered 1,089; MN full 'Write-in' 931 likewise. RI 'Write-in' 840 and VT 'Write-in' 294 also contain named write-ins. Meanwhile FL's, OH's and UT's named write-ins go to votes_other. The CSV header (lines 386-388) says other is 'every write-in candidate'. Reading 4 of the record lists 'blanks, over and under votes, void, others, scatterings, None of These Candidates' and does not mention the bare 'Write-in' and 'Other Write-ins' totals.

**The fix proposed.** State the rule in the CSV header and in reading 4: a write-in total printed without a name is votes_non, whatever candidates it contains, so the other/non split follows how each state prints its write-ins. Alternatively, file those lines as other.

**The skeptic's evidence.** Tools/us_senate_races_prep.pl line 147-149: %noncand contains 'Others', 'All Others', 'Write-in', 'Write-in (Scattering)', 'Write-in, Scatterings', 'Other Write-ins', 'Write-ins', 'Scattered'. Line 248, `if ($noncand{lc $label}) { $x->{non} += $v; next; }`, runs before any candidate parsing. So a write-in total the Clerk prints without a name goes to votes_non. A named line such as 'Fred Linck, Write-in' splits into name and label, and label_class (line 141, /^Write-in(?: \(.*\))?$/) makes it 'O'. Running pdftotext -raw on clerk_statistics2018.pdf confirms the Clerk prints the unnamed totals the finding describes: Maine 'Others ... 64', Minnesota 'Write-in ... 1,101' and '... 931', Rhode Island 'Write-in ... 840', plus 'Other Write-ins ... 1,650' and 'Write-in ... 2,042'. Other states print each write-in candidate by name ('Fred Linck, Write-in 70'; 'Lateresa L. A. Jones, Write-in 467'). So which column a write-in candidate's votes land in depends on how the state prints its write-ins. The CSV header (lines 391-394) says "other any other party and every write-in candidate" and defines votes_non as "the lines that are not candidates (blanks, over and under votes, scatterings, None of These Candidates)". Reading 4 of the record lists "blanks, over and under votes, void, others, scatterings and Nevada's None of These Candidates". Neither mentions that the bare 'Write-in', 'Write-ins' and 'Other Write-ins' totals are counted as non-candidates. Unnamed totals like these hold real write-in candidates' votes (the Clerk's own heading for them is 'Write-in'), so "every write-in candidate" overstates. The figures in the CSV are faithful to what the Clerk prints, no winner or class changes, and nothing measures with these columns yet. So this is a gap in how the rule is stated, not a wrong figure: minor.

**The skeptic's corrected fix.** State the rule in the CSV header (prep.pl lines 391-394) and in reading 4 of senate_record.md: other holds every write-in candidate the Clerk names; a write-in total printed without a name ('Write-in', 'Write-ins', 'Other Write-ins', 'Others', 'All Others', scatterings) is votes_non, whatever candidates it holds, so the split between other and non follows each state's printing. Alternatively, move the unnamed write-in totals into votes_other and keep votes_non for blanks, over and under votes, void and None of These Candidates.

### 5. The recapitulation test cannot see a figure moved between candidates, classes, or the two races of one state

- **Lens:** tool-vs-sources - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_races_prep.pl:276

**The scenario.** The test sums the Senator row's numeric cells against their Total, and the Total against all of the state's entries, both races together. It never reads the row's party column headings. Example: California 2024's Garvey and Schiff figures swapped between the full and unexpired races, or a term heading read after an entry so the entry lands in the other race. Both readings parse alike. The recap Total is unchanged, Schiff still wins both, the holder check still passes, and there is no FEC sheet for 2024, so every check passes. The same applies to a D/R figure swap within a race. Only the agreement between -raw and -table guards against it, and both readings go through one parser. I checked the current catalog against the text and it is correct. This is a gap in the checks, not wrong data.

**The fix proposed.** Read the recapitulation's party column headings (the 'Title of candidate ...' or party line) and hold each party's cell to the sum of that class's entries. For two-race states, at least hold the full race to the FEC or another source where one exists, or declare that the split between the two races is unchecked.

**The skeptic's evidence.** I could not refute the finding. The recapitulation test is sum-only, as the finding says.

- Tools/us_senate_races_prep.pl:189 keeps only the numeric cells of the Senator row (`grep { /^[\d,]+$/ } split / /, $1`) and drops every column heading.
- Lines 276-284: `$tot = pop @cells`; it checks that the cells sum to `$tot`, then that `$tot` equals the sum of `$race{..}{sum}` over every race of that year and state, so both races of a two-race state are pooled.

Nothing checks a party column against the entries of a class or a race. A figure moved between R and D inside one race, or between California 2024's full and unexpired races, leaves every sum unchanged.

The other checks also miss it:
- 2024 has no FEC sheet (lines 309-339 cover 2018 only).
- The holder check (lines 343-356) compares only the winner's surname, and Schiff holds both California seats.
- The Maine majority check applies to ME only.
- The tie check only stops the run when first and second are equal.

The only guard is the -raw/-table agreement at lines 199-208, and both readings go through one parser, `read_volume`.

How likely a real failure is:
- In both readings the label and figure sit on one line (`^(.*?) ?\.{2,}\s*([\d,]+)$`), so a swap inside a race would need the PDF text layer itself to be wrong.
- In a two-race state, an entry read before the first term heading makes a third race, which the `scalar(@rs)` check at line 222 refuses.
- A heading read late inside the second race goes unchecked, but -raw (reading order) and -table (position) would have to misplace it the same way.

So the gap is real, but whether wrong data could slip through is hypothetical. The finding itself says the current catalog is correct.

The repo does not overstate the check. Reading 8 in senate_record.md says "the Total is the state's races' entries, both races together in a state of two", and the tool header says the same, so there is no false claim and this is not a defect.

The fix is partly feasible. In the -table reading, California's 2024 recapitulation prints party column headings ("Republican", "Democratic", "American Independent", "Green", "Libertarian", "No Party Preference", "Peace and Freedom", "Write-in"), though some span two lines. In the -raw reading the recapitulation's cells are split across lines; for example, the 2024 Alabama Senator row continues on the next line. So a per-party check would have to read -table. Even then it could only hold the pooled per-party sum: the recapitulation has one Senator row per state, so it cannot tell California's full race from its unexpired one.

**The skeptic's corrected fix.** 1. Read the party column headings of the recapitulation from the -table reading, joining headings that span two lines.
2. Hold each party column of the Senator row to the sum of that party's entries across the state's races. Keep the Total check as it is.
3. Record in senate_record.md reading 8 that, in a two-race state (2018 MN, MS; 2024 CA, NE), no source checks how the votes split between the full and unexpired races; only the -raw/-table agreement guards it.

### 6. A line in the Senate section that is not an entry is skipped silently, so a wrapped entry can become a non-candidate line

- **Lens:** tool-vs-sources - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_races_prep.pl:187

**The scenario.** Any line with no dot leader and no trailing dots is dropped (`next; # prose ... none expected`). Example: a long entry wraps as 'Jonathan Ringham B,' then 'Write-in (The Old Republic) ...... 46', or 'Name, Party' then 'Write-in ..... 46'. The kept label is the tail. If the tail is a noncand label ('Write-in', 'Scattering', 'Others') or a fusion label in CT/NY, the candidate is silently filed as votes_non or merged into the previous candidate. The figure is still counted, so the recapitulation passes, and both readings may wrap alike. I checked every line in the 2018, 2024 and 2020-NE Senate sections: the only lines that are not entries are page numbers, state headings and term headings, so the current data is unaffected.

**The fix proposed.** Since no prose is expected, call problem() on any unrecognised line inside a SENATE section that is not a page number or a state or --Continued heading.

**The skeptic's evidence.** Staged Tools/us_senate_races_prep.pl line 187, inside `if ($sect eq 'SENATE')`: after the term-heading branch, the entry branch `if ($t =~ /^(.*?) ?\.{2,}\s*([\d,]+)$/)` and the no-figure branch `if ($t =~ /\.{2,}\s*$/) { problem(...) }`, every other line hits `next;   # prose between entries (none expected; the -raw/-table comparison holds the entries)`. That comparison (the $a ne $b join of label=fig per race) only catches cases where the two readings differ. If both readings wrap a long entry the same way, the first half is dropped with no message. The entry line keeps only the tail as its label. In the race loop, `if ($noncand{lc $label}) { $x->{non} += $v; next; }` files a tail such as 'Write-in' or 'Scattering' as votes_non. `if ($fusion_label{$label} && @parts == 1)` adds a tail such as 'Independence' (CT/NY) to the previous candidate's total. A tail such as 'Jr., Republican' becomes a candidate named 'Jr.'. The figure still goes into $x->{sum}, so the recapitulation check (`$tot == $races`) passes. The FEC W check and s792's holder check only look at the winner, so a minor candidate filed wrongly passes every check. A tail like 'Republican' alone is caught (@parts==1, `problem(... no name ...)`), so only the noncand, fusion and suffix forms get through. Tools/us_senate_races_mutations.sh has 19 cases and none of them wraps an entry. Against today's data I ran the staged tool on a temp copy, with line 187 changed to print each line it skips, for all three volumes in both modes. No line reached that branch, and the run still wrote its 71 races. So the catalog is not wrong today (the reviewer's 'current data unaffected' holds), and turning the skip into problem() costs nothing on the present volumes. This is a latent unguarded path, not a wrong figure.

**The skeptic's corrected fix.** At line 187, replace the silent `next;` with `problem("$file ($mode) $state line " . ($i + 1) . ": a line in the Senate listing that is no entry: '$t'"); next;`. Page numbers, state and --Continued headings, FOR headings and term headings are all handled before this point, and no line in the 2018, 2020-NE or 2024 volumes reaches it in either mode (checked on a temp copy), so the control still passes. Add a mutation case (wrap, both readings) that splits one entry across two lines, for example 'X, Y ..... n' into 'X,' and 'Write-in ..... n', so the new problem is proved to fire. Also change the comment, which now claims the -raw/-table comparison protects the entries; it does not when both readings wrap alike.

### 7. The full-term heading's date is never checked against the year

- **Lens:** tool-vs-sources - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_races_prep.pl:225

**The scenario.** Only an unexpired race's 'ending' date is compared, with the special's declared end. The 'full term beginning <date>' heading of a two-race state is recorded in $r->{when} and never compared. A 2024 volume printing '(For full term beginning January 3, 2019)' would pass. The tool's header (line 15) says it dies on 'a race of a term the year does not hold'.

**The fix proposed.** For a full-term heading, require 'January 3, <year+1>'.

**The skeptic's evidence.** The finding holds as a gap. It is not a defect: the catalog has no wrong figure, class, race or winner.

Code, Tools/us_senate_races_prep.pl:
- L176-177 reads both heading forms into `$race = { term => $1, when => $2, ... }`. For an implicit single race, L182 sets `when => ''`.
- L220 checks that a single race carries no heading (`problem(... "one race under a term heading") if $rs[0]{when} ne ''`).
- L225-227 compare `when` only for an unexpired race: `problem("$year $st: an unexpired term ending $r->{when}, declared $sp->{ends}") if $sp && $r->{when} ne $sp->{ends}`.
- For a full term, L228 is just `} else { $class = 1; }`. Grepping `when` over the file finds no other use, so a full-term heading's date is never checked.

The header (L15-16) says the tool dies on "a race of a term the year does not hold". A '(For full term beginning January 3, 2019)' heading inside a 2024 volume would pass. The header claim is therefore broader than the code, though one can read it as being about the specials' terms only. The record (senate_record.md reading 1, L111) only describes the heading forms and claims no check, so the doc makes no false claim.

Why it is minor: the pages are pinned by SHA256SUMS (sub page, L49-60), so this cannot happen with the committed inputs. I ran pdftotext -raw on the volumes (read-only, to stdout):
- 2018: full term beginning January 3, 2019 (x2); unexpired term ending January 3, 2021 (x2)
- 2020: full term beginning January 3, 2021
- 2024: full term beginning January 3, 2025 (x2); unexpired term ending January 3, 2027 and January 3, 2025

Every full-term heading matches year+1, and the 2020 unexpired headings fall outside the `$only{2020}` NE filter. The winners-against-s792 check (reading 10) would also catch a race given the wrong term. So this is an unguarded invariant and a header that over-claims. No wrong data passes.

**The skeptic's corrected fix.** In the else branch at L228, require the full-term date to match the year. For example: `} else { $class = 1; problem("$year $st: a full term beginning $r->{when}, not January 3, " . ($year + 1)) if $r->{when} ne '' && $r->{when} ne 'January 3, ' . ($year + 1); }`. Optionally add a mutation case that edits a full-term heading's year. Otherwise, narrow the header's L15-16 wording to "a special's unexpired term not the one declared".

### 8. The record's list of refusals with no mutation case is incomplete

- **Lens:** tool-vs-sources - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/senate_record.md:131

**The scenario.** The record lists five uncovered refusals. Other problem() sites in us_senate_races_prep.pl also have no case: continued-with-none-begun (168), the -table reading alone (210), one race with a special declared (219), one race under a term heading (220), three or more races (221), a fusion line with no candidate before it (245), marks declared and not found (261), no Senator row (272), the Senator row's cells not summing (277), an unnamed entry declared and not found (281), the Class I count not 33 (295), Class I states missing or extra (300), FEC W count not one (336), and an FEC difference declared and not met (344).

**The fix proposed.** List them all, or reword the sentence so it does not read as the complete list.

**The skeptic's evidence.** The finding holds, and I grade it minor. senate_record.md:131 ends: "Refusals with no case: a listing twice, an entry without a figure, a term heading of another form, a race of no candidate, Maine's majority." Read as a plain list, that names every uncovered refusal, but it names only five.

I traced the 19 cases in Tools/us_senate_races_mutations.sh against the problem() sites in Tools/us_senate_races_prep.pl. Each case hits one site:
- r01 -> :207
- r02 -> :278
- r03 -> :236
- r04 -> :258
- r05 -> :251
- r06 -> :244
- r07 -> :264
- r08 -> :217
- r09 -> :227
- r10 -> :236 (an empty %marked gives "not grouped by thousands", not :261)
- n01 -> :303
- f01 -> :342 (votes_differ emptied, so :344's loop has nothing to check)
- f02 -> :340
- w01 -> :360
- t01 -> :39
- t02 -> :372
- t03 -> :376

These problem() sites have no case and are missing from the record's list:
- :168, a listing continued with none begun
- :210, a listing in the -table reading alone. r01 only changes a figure.
- :219, one race with a special declared
- :220, one race under a term heading
- :221, three or more races
- :245, a fusion line with no candidate before it
- :261, marks declared and not found
- :272, no Senator row
- :277, the Senator row's cells not summing to its Total. r02 changes the listing, not the recapitulation, so it reaches :278 only.
- :281, an unnamed entry declared and not found
- :295, the Class I count not 33
- :300, Class I states missing or extra
- :336, a race without exactly one FEC W
- :344, a declared FEC difference not met

The die sites at :81/:83/:88/:90 (workbook parts), :315/:318/:321 (FEC sheet heading and columns) and :288 also have no case.

s792 set the precedent. Review 2026-10-06_s792 finding 21 was graded minor for the same coverage overstatement, and the s792 record line (:102) now carries a "Refusals with no case:" list that is meant to be complete. The same phrasing here therefore reads as a complete list, and it is not. No catalog figure, class, winner or race is wrong, so this is a gap in the coverage statement, not a defect.

**The skeptic's corrected fix.** Fix line 131 one of two ways.

1. List every uncovered refusal by its header wording:
- a listing continued with none begun
- a listing in the -table reading alone
- a race count other than one or two
- a one-race state with a special declared or a term heading
- a fusion line with no candidate before it
- marks declared and not found
- no Senator row, or its cells not summing to its Total
- an unnamed entry declared and not found
- the Class I count not 33, or Class I states missing or extra
- an FEC race without one W
- a declared FEC difference not met
- the workbook's or the FEC sheet's structure dies

2. Or add the cheap TOOL cases and list what remains:
- add a mark to %marked for a state that has none (:261)
- add an entry to %unnamed_ok that is never found (:281)
- add a votes_differ entry for a race that matches (:344)
- declare a special in a one-race state (:219)
- wrap the recapitulation's Senator cell for one state (:277)

Either way, the sentence should state that the list is the complete set of uncovered refusals.

### 9. Arizona 2018's unnamed 'Green' declaration cites the FEC sheet but is not checked against it

- **Lens:** tool-vs-sources - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_senate_races_prep.pl:144

**The scenario.** The comment says the FEC names her Angela Green, W(GRE)/GRE, and the saved sheet does have 'Green, Angela | W(GRE)/GRE | 57442'. The tool reads that sheet but only looks at W rows, so the declaration is a comment rather than a check.

**The fix proposed.** Hold the unnamed entry's figure to the FEC row with the same state and party.

**The skeptic's evidence.** Tools/us_senate_races_prep.pl (staged) line 144: `my %unnamed_ok = ('2018 AZ' => 'Green');   # the FEC names her: Angela Green, W(GRE)/GRE`. The only places it is used are line 250 (the nameless entry 'Green' becomes `{ name => "(unnamed, Green)", class => 'O', total => $v }` with flag U) and line 281 ("declared and not found"). The FEC block (lines 311-344) only looks at the W row of each 2018 race: `@p != 1` W marks, the W's party against the Clerk's winner's class, and the W's votes against `$x->{winner}{total}`. Nothing compares the unnamed entry's figure or party with a FEC row. So the observation is accurate: the FEC citation is a comment, not a check. I confirmed that the saved sheet does hold 'Green, Angela' and 'W(GRE)/GRE' (in xl/sharedStrings.xml of fec_federalelections2018.xlsx), so the comment's claim is true. The record also labels it correctly: senate_record.md line 113 says the FEC names her Angela Green, "DECLARED". By the repo's convention, a premise stated as DECLARED is not a false claim. The catalog's figures do not depend on the FEC anyway. The entry's figure comes from the Clerk's volume. It is held by the -raw/-table agreement and by the recapitulation check (the Senator row's Total must equal the sum of the races' entries). The class is O for any minor-party label, so a FEC check could not change any figure, class, winner or race in senate_races.csv (2018,AZ row: others 58008, winner Sinema D, flag U). Nothing is wrong and no check passes on wrong data. The finding is a wording or completeness note: the comment cites a source the tool reads but does not hold the entry to.

**The skeptic's corrected fix.** Optional. In the FEC block, for each `%unnamed_ok` key in 2018, find the sheet's row with the same STATE ABBREVIATION, a non-W indicator and a PARTY whose code matches the declared label (GRE for 'Green'). Require exactly one such row, with GENERAL VOTES equal to the unnamed entry's total, and stop otherwise. Then add a mutation case for it. The other choice is to leave the code as it is and reword the comment to "DECLARED: the FEC names her Angela Green (GRE); not checked", which matches the record's wording.

### 10. Small wording mismatches between headers and code

- **Lens:** tool-vs-sources - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_senate_races_prep.pl:22

**The scenario.** (a) The header says the run dies on 'a finalist's votes not the Clerk's', but only the W row's votes are compared (lines 333-343). (b) The I list in the CSV and record headers leaves out 'Nonpartisan', which the class table (line 128) maps to I; no listing uses it today. (c) Flag U says 'a candidate as the FEC names it', but the CSV carries no name for her. (d) The 2018 Class II specials (MN, MS) are held to the holder on 2023-01-03, so they pass because Smith and Hyde-Smith were re-elected in 2020, not on the strength of the 2018 race itself.

**The fix proposed.** Reword (a) to 'the winner's votes', add 'Nonpartisan' to the header's I list or drop it from the table, and in (c) and (d) say what the flag and the check actually establish.

**The skeptic's evidence.** I checked all four parts against the code and found each one accurate. None of them is a wrong figure or a check that passes on wrong data, so the severity stays at note.

(a) Header line 22 says the run dies on "a finalist's votes not the Clerk's but where declared". The FEC loop (lines ~318-343) builds one [party, votes] pair per W row, then runs `problem("FEC 2018 $k: the W's votes $v, the Clerk's winner ... $x->{winner}{total}") unless $v == $x->{winner}{total}`. No other candidate's votes are compared, so "finalist" overstates the check. Record reading 9 says it correctly: "its votes the Clerk's winner's".

(b) Line 128 maps 'Nonpartisan' => 'I'. The I list is printed in three places, and all three leave Nonpartisan out: the CSV header ("Independent, Unaffiliated, No Political Party, No Party Affiliation, By Petition"), record reading 2, and the generated CSV. I ran pdftotext -raw on all three volumes. The only "Nonpartisan" candidate lines are 2018 Calvin C. "G" Griffin, 2020 Ron Burrus and 2024 Randall Kelly Meyer, and all three sit under FOR UNITED STATES REPRESENTATIVE. The other hits are Democratic-Nonpartisan League, which is a D label. So no Senate row uses it today, and the gap only matters if a future volume does.

(c) Line 381 defines flag U as "an entry with no name, a candidate as the FEC names it". The candidate is stored as name "(unnamed, Green)" with class O (line ~243). Nothing in the CSV carries the FEC's name (Angela Green), and her name would only reach the winner column if she won. The FEC name appears only in the comment on line 144 and in record reading 3. The flag text therefore promises a name the catalog does not hold.

(d) Lines ~355-360: `my $d = $x->{year} < 2024 ? '2023-01-03' : ...` checks every 2018 race, the Class II specials for MN and MS included, against the 2023-01-03 holder. Those specials ran to 3 Jan 2021, so the check passes only because Smith and Hyde-Smith won again in 2020. It does not tie the 2018 race to the seat it filled. Record reading 10 names the day honestly ("2018's and 2020's on 3 Jan 2023 (the catalog's first day)"), so nothing is false. A wrong 2018 winner would only slip through if it matched the 2023 holder's last name, and any other mismatch stops the run.

**The skeptic's corrected fix.** (a) At line 22, change "a finalist's votes" to "the W's votes (the winner's)". (b) Add Nonpartisan to the I list in the CSV header and in record reading 2, or drop it from %label_class. Dropping it is safe: no Senate listing uses it, and label_class() would still return O for it rather than stop, because "Nonpartisan" matches none of democrat|republic|independ|gop. (c) Reword U to "an entry with no name: a candidate (the FEC names her; the CSV keeps no name)". (d) In reading 10 and the tool comment, say that the 2018 Class II specials (MN, MS) are held to the seat's 2023 holder, so the check confirms that the 2018 winner still held the seat in 2023 after the 2020 race, not the 2018 race on its own.

### 11. The 'winner leading in his class's votes' claim is never tested: the check only asks that the winner's class has more than zero votes

- **Lens:** csharp-check - **reviewer:** defect - **skeptic:** defect
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:1052

**The scenario.** Line 1052 sets `long lead = r.WinnerClass == "R" ? r.VotesR : ... : r.VotesOther;` and line 1053 fails only when `lead <= 0`. The method's docstring (line 1020, 'every winner leading his race's candidates by class') and senate_record.md:133 ('every winner leading in his class's votes') both claim more than that. Take a catalog whose 2024 MI row reads winner 'Elissa Slotkin', winner_class 'R' (VotesR 2693680 > 0), or whose 2018 ME row has winner_class 'D' for King (VotesD 66268 > 0). The CSV comparison passes because the tool wrote both files. The holder check passes because it compares only the last name. So the bar is green on a wrong class. Any unknown class string, such as '' or 'X', falls through to VotesOther and passes too, as long as that column is above zero.

**The fix proposed.** For each row, require the winner's class total to be strictly greater than each of the other three class totals. Every current row already meets this; CA 2018 is the one row with only one class on the ballot. Also restrict WinnerClass to {R, D, I, O}. You could also check it against the seat row's Party: its first letter, so D/I counts as D. Otherwise, make the docstring and record line 133 say only what is actually tested.

**The skeptic's evidence.** The finding holds. The line numbers are off by one: the code is at GeneratedCatalogCheck.cs:1053-1054 (staged), not 1052-1053.
  long lead = r.WinnerClass == "R" ? r.VotesR : r.WinnerClass == "D" ? r.VotesD : r.WinnerClass == "I" ? r.VotesI : r.VotesOther;
  if (lead <= 0) { wrong.Add(... "the winner's class " + r.WinnerClass + " holds no votes"); }
This only asks that the winner's class has a positive total. Two texts claim more than that:
- The docstring above the method: "every winner leading his race's candidates by class".
- senate_record.md, the CheckUsSenateRaces paragraph: "every winner leading in his class's votes".
The success line (1079-1080) does not make the claim. It says only "every winner his seat's holder by the seat rows".

The failing path is real. The row comparison (SameRow) only holds the catalog to the CSV, and the tool writes both. So if the tool put the wrong winner_class on a row, both files would agree and the comparison would pass. The holder check (1065) compares only last names (Last()), so it never looks at the class. Any class string other than R, D or I, including '' or 'X', falls through to VotesOther and passes whenever that column is above zero. For 2018 the tool cross-checks the class against the FEC's W party (prep.pl:340). The 2024 and 2020 rows have no independent check of the class anywhere on the bar.

The current data is correct. I ran awk over senate_races.csv: in every row the winner's class has the largest of the four class totals, and the only classes used are D 40, R 27 and I 4. So no row in the catalog is wrong today. The defect is the false claim, plus a check that would pass on a wrong class.

One caution about the proposed fix. The tool picks the winner as the plurality candidate (prep.pl:265 and the CSV header: "winner: the candidates' plurality"), not the plurality class. Requiring the winner's class to have the largest class total is therefore stronger than the definition guarantees, even though every current row meets it.

**The skeptic's corrected fix.** Make the check do what the docstring and record say:
1. Fail any row whose WinnerClass is not in {R, D, I, O}.
2. Fail any row where the winner's class total is not greater than or equal to each of the other three class totals. Use >= rather than >, because the catalog's winner is the plurality candidate, and CA 2018 had only one class on the ballot.
3. Optionally, for each full-term race, compare the winner's class with the first letter of the seat row's Party. That gives 2024 the independent class check that 2018 already gets from the FEC sheet.
Then name the class test in the success line too.

If the check is not changed, reword the docstring and senate_record.md to say only "every winner's class holds votes", which is all that is tested.

### 12. The record says Nebraska's 2024 special is held to 3 Jan 2025; the check and the tool hold it to 9 Dec 2024, and the Class II branch can never run

- **Lens:** csharp-check - **reviewer:** defect - **skeptic:** defect
- **Where:** ElectionsData/usa/senate_record.md:120

**The scenario.** The day expression at GeneratedCatalogCheck.cs:1055 and the identical one at Tools/us_senate_races_prep.pl:357 test `r.Term == "unexpired"` before `r.Class == 2`. The 2024 NE row is (class 2, 'unexpired'), so it gets '2024-12-09'. No 2024 row has class 2 and term 'full', so the '2025-01-03' branch is unreachable. Reading 10 says 'Nebraska's 2024 special on 3 Jan 2025'. That is a false claim about the day this race is held to. No result changes, because Ricketts (took 2023-01-23, never left) holds the seat on both days. The tool's own comment at line 355 also names only California's special.

**The fix proposed.** Either put the class test first in both places (`r.Year < 2024 ? ... : r.Class == 2 ? "2025-01-03" : r.Term == "unexpired" ? "2024-12-09" : "2025-01-21"`), or change reading 10 to say Nebraska's special is held to 9 Dec 2024 like California's and remove the dead branch.

**The skeptic's evidence.** I could not refute this. The ternary checks the term before the class, in both the tool and the check:
- Tools/us_senate_races_prep.pl:357: `my $d = $x->{year} < 2024 ? '2023-01-03' : $x->{term} eq 'unexpired' ? '2024-12-09' : $x->{class} == 2 ? '2025-01-03' : '2025-01-21';`
- Assets/Editor/GeneratedCatalogCheck.cs:1056: `string day = r.Year < 2024 ? "2023-01-03" : r.Term == "unexpired" ? "2024-12-09" : r.Class == 2 ? "2025-01-03" : "2025-01-21";`

The generated ElectionsData/usa/senate_races.csv:70 is `2024,NE,2,unexpired,...,Pete Ricketts,R,-`, so Nebraska's 2024 special takes the 'unexpired' branch and is held to 2024-12-09. The 2024 rows are CA 1 full, CA 1 unexpired, NE 1 full and NE 2 unexpired, plus the full-term Class I rows. None of them is class 2 with term 'full', so the '2025-01-03' branch can never run in either place.

ElectionsData/usa/senate_record.md:120 (reading 10) says "Nebraska's 2024 special on 3 Jan 2025". That is false: the tool and the check both test it on 9 Dec 2024. The tool's comment at line 356 also names only California's special for the unexpired day.

No result changes. senate_seats.csv:78 has Ricketts taking the seat on 2023-01-23 with left '-', so he holds the seat on both days and the check passes either way. The finding's own grading allows defect for a false claim, but the only error is in what the record says about the check; no figure, winner or race in the catalog is wrong.

**The skeptic's corrected fix.** Choose one of two fixes.

(a) Put the class test first, in the tool (prep.pl:357) and the check (GeneratedCatalogCheck.cs:1056) together: `year < 2024 ? '2023-01-03' : class == 2 ? '2025-01-03' : term eq 'unexpired' ? '2024-12-09' : '2025-01-21'`. Then extend the comment at prep.pl:355-356 to name Nebraska's special.

(b) Leave the code as it is. Change reading 10 to say Nebraska's 2024 special is held to 9 Dec 2024 like California's, or better, to the day it was actually decided by the record. Remove the dead `class == 2` branch in both places.

Fix (a) matches the record's stated intent. Either fix must change the tool and the check identically.

### 13. The specials and the 2020 race are only counted, not named; the success line claims more than was checked

- **Lens:** csharp-check - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:1063

**The scenario.** The check pins three things: the number of rows per year (35/1/35), the set of states with a full-term Class I race (fullI is a set, so a duplicate is invisible), and each winner's holder. It never checks that (year, state, class, term) is unique, never names the four specials, and never checks that the 2020 row is Nebraska's. Two cases would pass. (1) In 2018, the MS Class II row is dropped and the MN Class II row is written twice: still 35 rows, fullI unchanged, both MN rows hold Smith. (2) The 2020 row is CO class 2 with winner Hickenlooper: still one row, and he holds the seat on 2023-01-03. Both would print 'the specials ... 2020 Nebraska alone' (line 1079). The tool checks all of this itself (prep.pl:300-303 and its special declarations), but this check claims it re-verifies it.

**The fix proposed.** Fail on a duplicate (year, state, class, term) key. Require the specials to be exactly {2018 MN 2, 2018 MS 2, 2024 CA 1, 2024 NE 2}, each with term 'unexpired'. Require the 2020 row to be (NE, 2, full). Or soften the success line so it does not claim these.

**The skeptic's evidence.** Staged Assets/Editor/GeneratedCatalogCheck.cs, CheckUsSenateRaces. The structural checks are only these: `perYear` counts (`y18 != 35 || y24 != 35 || y20 != 1`); `fullI[y].SetEquals(classI)`, which is filled only from rows with `r.Term == "full" && r.Class == 1`; and a holder check per row (`Last(holder) != Last(r.Winner)` on `day = r.Year < 2024 ? "2023-01-03" : ...`). Nothing checks that (year, state, class, term) is unique. Nothing names the four non-full or non-Class-I rows (the CSV holds 2018 MN 2 unexpired, 2018 MS 2 unexpired, 2024 CA 1 unexpired, 2024 NE 2 unexpired). Nothing reads the 2020 row's state. `SameRow` only holds the CSV and the catalog to each other, so a regenerated wrong pair still agrees. Both scenarios pass. (1) In 2018, drop MS 2 and duplicate MN 2: there are still 35 rows, fullI is untouched, and Smith holds MN class 2 on 2023-01-03. (2) Make the 2020 row CO 2 with Hickenlooper: there is still one 2020 row, and he holds the seat on 2023-01-03. The success line still prints "and the specials, 2020 Nebraska alone". The record line (senate_record.md, the CheckUsSenateRaces paragraph) also says "Nebraska's 2020 alone" as something the check verifies. The tool does guard both: prep.pl's `problem("2020: ... not Nebraska's alone") unless @y2020 == 1 && $y2020[0]{st} eq 'NE'`, and the specials are read keyed per race. The shipped data is correct, so no wrong figure is in the catalog today. This is a gap in the re-verification and an overclaim in the success line, not a wrong row. Minor stands.

**The skeptic's corrected fix.** In CheckUsSenateRaces, build a HashSet of $"{Year} {State} {Class} {Term}" and fail on a duplicate. Require the rows with Term != "full" or Class != 1 to be exactly {"2018 MN 2 unexpired", "2018 MS 2 unexpired", "2024 CA 1 unexpired", "2024 NE 2 unexpired", "2020 NE 2 full"}, with the 2020 one the only 2020 row. Alternatively, cut "the specials, 2020 Nebraska alone" from the success line and from the record's CheckUsSenateRaces paragraph, so they claim only the counts that are checked.

### 14. The last-name match fails on 'III': Manchin's match compares 'iii' with 'iii'

- **Lens:** csharp-check - **reviewer:** note - **skeptic:** minor
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:1048

**The scenario.** `Last` strips only a trailing ' Jr' or ' Sr'. For 'Joe Manchin III' (the 2018 WV winner and the WV Class I seat row) it returns 'iii' on both sides. So that row's holder match would accept any winner whose name ends in 'III'. The tool's last_word (prep.pl:353) has the same gap. No current row is wrong because of it.

**The fix proposed.** Also strip a trailing roman-numeral suffix (II, III, IV) in both Last and last_word.

**The skeptic's evidence.** Staged GeneratedCatalogCheck.cs: `string Last(string n) { ... t = Regex.Replace(t, @" (Jr|Sr)$", ""); string[] w = t.Split(' '); return w[w.Length - 1].ToLowerInvariant(); }` and the test `if (holders != 1 || Last(holder) != Last(r.Winner))`. Staged prep.pl:359: `sub last_word { ... $n =~ s/,? (?:Jr|Sr)\.?\s*$//; ... return lc((split / /, $n)[-1]); }`, used at line 367 `last_word($h->{senator}) eq last_word($x->{winner}{name})`. prep.pl does declare `%suffix = ('Jr.', 'Sr.', 'II', 'III', 'IV')` at line 152 for the reading, but last_word does not use it. Data: senate_races.csv row `2018,WV,1,full,...,Joe Manchin III,D,-`, and senate_seats.csv row `WV,1,Joe Manchin III,D/I,...`. So both sides reduce to "iii". The check passes today because Manchin really is the holder. No current row is wrong: the only other suffixed winners are King Jr. and Casey Jr., and those strip correctly to "king" and "casey". The finding is real, though. For the 2018 WV row, the holder test cannot tell Manchin from any other winner whose name ends in "III", so a wrong winner name in that row would pass both the tool's check and the Editor check. The holder lookup is fixed by state, class and day, so only the winner string could go wrong unseen. This is a gap in what the check can catch, not a wording issue, so I grade it minor rather than note. It is not a defect, because no wrong data passes now.

**The skeptic's corrected fix.** In Last, strip `@" (Jr|Sr|II|III|IV)$"` (after the '.' and ',' removal). In last_word, strip `s/,? (?:Jr|Sr|II|III|IV)\.?\s*$//`, or reuse %suffix. Then 'Joe Manchin III' matches on 'manchin'. Optionally, add a mutation case that sets the 2018 WV winner to another '... III' name and expects a failure.

### 15. 2018 winners are matched to the seat holder on 2023-01-03, so the two 2018 Class II specials are confirmed by their 2020 wins

- **Lens:** csharp-check - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:1055

**The scenario.** senate_seats.csv starts on 3 Jan 2023, so the 2018 MN and MS specials (terms ending 3 Jan 2021) are matched by name against Smith and Hyde-Smith as they held their seats in 2023, after they won again in 2020. This is a weaker test than it reads: it confirms the right person, not that she was seated by the 2018 race. Reading 10 does say why the day is 3 Jan 2023 (the catalog's first day). The record could say outright that, for the 2018 Class II specials, this is a test of identity and not of the term.

**The fix proposed.** One clause in reading 10 or in the check's docstring is enough.

**The skeptic's evidence.** GeneratedCatalogCheck.cs (staged, CheckUsSenateRaces): `string day = r.Year < 2024 ? "2023-01-03" : r.Term == "unexpired" ? "2024-12-09" : r.Class == 2 ? "2025-01-03" : "2025-01-21";`. The prep tool does the same at Tools/us_senate_races_prep.pl:357 ("2018 and 2020 winners on the window's first day"). senate_races.csv: `2018,MN,2,unexpired,...,Tina Smith,D,-` and `2018,MS,2,unexpired,...,Cindy Hyde-Smith,R,M`. Both are terms to 3 Jan 2021, as reading 1 says. senate_seats.csv: `MN,2,Tina Smith,D,-,2018-01-03,-,before the window` and `MS,2,Cindy Hyde-Smith,R,-,2018-04-09,-,before the window`. On 2023-01-03 each holds the seat through her 2020 full-term win, not through the 2018 special. So for these two rows the check confirms the person (by last name) and not the 2018 term she won. The scenario is accurate.

It is not a defect. The check cannot pass on a wrong winner because of this: a wrong name still fails. The only effect is that the check could fail when the data is right, for example if a 2018 special winner had lost in 2020, and that did not happen here. Reading 10 and the check's own text also name the day plainly ("2018's and 2020's on 3 Jan 2023 (the catalog's first day)"). So nothing false is claimed. What is missing is a statement that, for the two Class II specials, this day falls after the term the race filled. That makes it a wording note.

The 2020 Nebraska row is fine: Sasse holds the seat on 2023-01-03 (he left 2023-01-08) and is still inside the term that race filled.

**The skeptic's corrected fix.** Add one clause to reading 10 in ElectionsData/usa/senate_record.md, and the same to the CheckUsSenateRaces docstring. Suggested wording: "for 2018's two Class II specials (terms ended 3 Jan 2021) that day falls after the term the race filled, so the test is of identity, not of the term: Smith and Hyde-Smith hold the seat by their 2020 wins."

### 16. The record holds Nebraska's 2024 special to 3 Jan 2025, but the tool and the bar hold it to 9 Dec 2024; the Class II branch never runs

- **Lens:** docs-suite - **reviewer:** defect - **skeptic:** defect
- **Where:** ElectionsData/usa/senate_record.md:120

**The scenario.** In Tools/us_senate_races_prep.pl:357 (and the same ternary at GeneratedCatalogCheck.cs:1056), the test order is `year<2024 ? 2023-01-03 : term eq 'unexpired' ? 2024-12-09 : class==2 ? 2025-01-03 : 2025-01-21`. senate_races.csv's row `2024,NE,2,unexpired` is unexpired, so it takes 2024-12-09. No 2024 race is full-term Class II, so the 2025-01-03 branch can never run. Reading 10 says 'Nebraska's 2024 special on 3 Jan 2025', which is false. The check passes anyway only because Ricketts held the seat on both days. Had the special's winner been sworn in on 3 Jan 2025 (an elected newcomer), the check would compare him with the earlier holder and refuse a correct catalog.

**The fix proposed.** Test the class before the term (`class==2 ? 2025-01-03 : term eq 'unexpired' ? 2024-12-09 : 2025-01-21`) in both the tool and CheckUsSenateRaces, or change the record to say 9 Dec 2024 and delete the dead branch. Also add the NE day to the tool's comment at lines 355-356.

**The skeptic's evidence.** Tools/us_senate_races_prep.pl (staged) line ~357: `my $d = $x->{year} < 2024 ? '2023-01-03' : $x->{term} eq 'unexpired' ? '2024-12-09' : $x->{class} == 2 ? '2025-01-03' : '2025-01-21';`. GeneratedCatalogCheck.cs:1056 has the same ternary: `r.Year < 2024 ? "2023-01-03" : r.Term == "unexpired" ? "2024-12-09" : r.Class == 2 ? "2025-01-03" : "2025-01-21"`. senate_races.csv line 70: `2024,NE,2,unexpired,...,Pete Ricketts,R,-`, so Nebraska's special takes the 'unexpired' branch and is held to 2024-12-09. The only other 2024 rows are full-term Class I (e.g. line 69 `2024,NE,1,full`) or CA's unexpired Class I, so the `class == 2 ? 2025-01-03` branch never runs in either the tool or the bar. senate_record.md line 120 (reading 10) says "Nebraska's 2024 special on 3 Jan 2025". The tool does not do that, so the record makes a false claim. The comment at lines 355-356 names only California's special day and says nothing about Nebraska. The check still passes on today's data because senate_seats.csv line 78 has Ricketts (took 2023-01-23, left '-') holding NE class 2 on both days. That means no wrong winner gets through now. The finding's hypothetical (a newcomer sworn on 3 Jan 2025) is not tested by any current data. What is real is the gap between the record and the code, plus the dead branch.

**The skeptic's corrected fix.** Make the record and the code say the same thing. Option one: test the class before the term, in both Tools/us_senate_races_prep.pl and GeneratedCatalogCheck.CheckUsSenateRaces: `year<2024 ? 2023-01-03 : class==2 ? 2025-01-03 : term eq 'unexpired' ? 2024-12-09 : 2025-01-21`. Then extend the tool's comment to give Nebraska's day. Option two: change reading 10 to say Nebraska's special is held to 9 Dec 2024 and delete the dead class==2 branch in both places. Whichever you choose, regenerate and run the mutations script. One possible mutation case: a NE class-2 seat row whose holder changes between 9 Dec 2024 and 3 Jan 2025. That would prove which day the bar really uses.

### 17. CheckUsSenateRaces says every winner leads his class's votes, but it only checks that his class has some votes

- **Lens:** docs-suite - **reviewer:** defect - **skeptic:** defect
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:1054

**The scenario.** The doc-comment (line 1021, 'every winner leading his race's candidates by class') and senate_record.md:133 ('every winner leading in his class's votes') claim a lead check. The code is only `if (lead <= 0)`. The seat-holder check compares surnames only. Suppose a generator error writes 2018 TX's winner_class as D. Then votes_d (Cruz still the named winner) is greater than 0, and Last('Ted Cruz') matches the seat row. The bar passes a wrong winner_class. Nothing in the tool or the bar holds a 2024 winner's class to anything except the label map: the FEC cross-check covers 2018 only, and the seat rows' party column is never consulted.

**The fix proposed.** Either check that the winner's class has the largest class total, unless flag S is set or another class's total is split across several candidates (e.g. VT/TN I totals), or compare WinnerClass with the seat row's party on the race's day (D/I and I mapped to I). Otherwise reword both claims to 'the winner's class holds votes'.

**The skeptic's evidence.** The finding holds. In the staged Assets/Editor/GeneratedCatalogCheck.cs, CheckUsSenateRaces checks the winner's class with only this:
  long lead = r.WinnerClass == "R" ? r.VotesR : r.WinnerClass == "D" ? r.VotesD : r.WinnerClass == "I" ? r.VotesI : r.VotesOther;
  if (lead <= 0) { wrong.Add(... "the winner's class " + r.WinnerClass + " holds no votes"); }
Nothing compares that total with the other classes' totals, so this is a "holds votes" test, not a lead test.

The seat-holder test is `Last(holder) != Last(r.Winner)`. It compares surnames only, and it never reads the seat rows' party.

Two places claim more than the code does:
- The doc-comment says "every winner leading his race's candidates by class".
- ElectionsData/usa/senate_record.md:133 says "every winner leading in his class's votes".
The bar's success line ("every winner his seat's holder by the seat rows") does not claim a lead, so the overstatement is in the comment and the record.

Tracing the scenario: the bar takes winner_class from the CSV and the catalog, and the same generator writes both. Change 2018 TX's winner_class to D in both and the CSV/catalog equality still passes, votes_d is above 0, and "cruz" still matches the seat row. The bar passes the wrong class.

The tool does lower the chance of such a row. It takes winner_class from the plurality candidate's own record ($x->{winner}{class}, prep.pl:398), and for 2018 it holds that class to the FEC W's party (prep.pl:340). Nothing does the same for 2024 or Nebraska 2020. Still, the bar is the standing check, and on this field it passes wrong data while its comment and the record say it checks a lead. That makes this a false claim about a check, which the rubric grades as a defect.

**The skeptic's corrected fix.** Choose one of these:
(a) In CheckUsSenateRaces, hold WinnerClass to the class of the seat row the winner matched, on the race's day. Map D/I and I both to I. That needs the seat row's party read from UsPresidentialReturns.SenateSeats.
(b) Test that the winner's class total is at least every other class total. Exempt rows where the leading rival total is split across several candidates (cands_x > 1, e.g. VT, NE 2024 I, CA 2018 D-D) and where flag S is set.
(c) If neither is wanted, change the doc-comment and senate_record.md:133 to say "the winner's class holds votes" rather than "leading".

### 18. Reading 2 says New York's 'Independence' is declared other; in the data it is a fusion line summed into the Democrat

- **Lens:** docs-suite - **reviewer:** defect - **skeptic:** minor
- **Where:** ElectionsData/usa/senate_record.md:112

**The scenario.** In the 2018 NY listing, the nameless 'Independence' line (99,325) comes after Gillibrand. It matches %fusion_label (tool:139) with @parts==1 in a fusion state, so it is added to Gillibrand's total and lands in votes_d (the CSV's 2018 NY votes_other is 0). label_class's 'Independence' => 'O' (tool:129) applies only to a named 'X, Independence' entry, and no saved listing has one. A reader of reading 2 would expect New York's Independence votes in votes_other. Reading 3 contradicts it.

**The fix proposed.** Remove 'New York's' from reading 2, so it reads '"Independence" on a named candidate's own line is other'. Or say that New York's nameless Independence line is a fusion line (reading 3).

**The skeptic's evidence.** The scenario is real, and the catalog is right. senate_record.md:112 (reading 2) ends with: ("Independent American", "Independence-Alliance" and New York's "Independence" are declared other). The tool:129 entry 'Independence' => 'O' in %label_class only takes effect through label_class($lab), which runs only on entries with a name (@parts > 1, tool ~258). A nameless entry with the label 'Independence' gets caught first at tool ~243 (if ($fusion_label{$label} && @parts == 1)), because 'Independence' is in %fusion_label (tool:139) and NY is in %fusion_state. That code adds the line to the candidate just before it ($of->{total} += $v).

I ran pdftotext -raw on scratch copies of the Clerk PDFs and searched every Senate listing for "independ". In 2018 the only 'Independence' entry is the nameless line "Independence ..... 99,325" after Gillibrand. No listing in 2018 or 2024 has a named 'X, Independence' entry. The 2024 matches are 'Independent', 'Independent American' and 'Independence-Alliance' only. So the tool:129 'Independence' => 'O' entry never fires, and New York's Independence votes go to the Democrat. senate_races.csv has 2018,NY,...,0,195735,... with Gillibrand as D, and the 99,325 sits in her total, not in votes_other.

The figures, classes and winners are all correct. Reading 3 (line 113) correctly lists Independence as a fusion line. So the fault is a misleading sentence in the record, not wrong data, and no check passes on wrong data. As written, reading 2 tells a reader that New York's Independence line counts as other, while the code sums it into the candidate's total, which conflicts with reading 3. I grade it minor (an ambiguity, plus a table entry that never fires) rather than a defect. Read literally, the label table does declare 'Independence' as other; it just never applies to New York's data.

**The skeptic's corrected fix.** Reading 2: drop "New York's" and make clear that the declaration covers only a label on a named candidate's own line: ("Independent American", "Independence-Alliance", and "Independence" on a named candidate's line, which no listing has, are declared other; New York's nameless Independence line is a fusion line, reading 3). Another option is to remove 'Independence' => 'O' from %label_class at tool:129, since it never fires. A named 'X, Independence' entry would then match /independ/i and stop the run, which follows the tool's rule of stopping on any form not declared.

### 19. The record's list of refusals without a case is incomplete; several refusals the header names have no case

- **Lens:** docs-suite - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/senate_record.md:131

**The scenario.** 'Refusals with no case' names five. These die sites have no case and are not named: a recapitulation's cells not summing to its Total (tool:277; header line 19), a state with no Senator row (272), a fusion line with no candidate before it (245; header line 16), one race under a term heading (220; header line 15), one race with a special declared (219), more than two races (221), a listing in the -table reading alone (210), 'continued with none begun' (168), a Class I state without a full-term race or a race outside Class I (300; header line 20), the seat catalog not 33 Class I states (295), an FEC race without exactly one W (336; header line 22), a declared FEC difference not met (344), declared footnote marks not found (261), and the declared unnamed entry not found (281). For example, deleting one cell from a state's Senator recap row is a refusal the header claims and no case proves.

**The fix proposed.** Add cases at least for the header-named ones: recap cells off with Total kept, a fusion line moved first, a single race given a heading, Class I coverage (drop a state's listing), and the FEC W removed. Then list the rest in the record's no-case sentence.

**The skeptic's evidence.** The record (staged senate_record.md, race half) says "it caught all nineteen cases ... Refusals with no case: a listing twice, an entry without a figure, a term heading of another form, a race of no candidate, Maine's majority." That is presented as the list of uncovered refusals, but it leaves many out. Each of the 19 cases in Tools/us_senate_races_mutations.sh requires exactly one message. Mapped to the problem() sites in Tools/us_senate_races_prep.pl, the cases cover:
- 59 and 58 (p01, p02)
- 207 (r01), 278 (r02), 236 (r03 and r10), 258 (r04), 251 (r05), 244 (r06), 264 (r07), 217 (r08), 227 (r09)
- 303 (n01), 342 (f01), 340 (f02), 360 (w01)
- the warning, ASCII and field-count checks (t01-t03)

No case reaches these sites, and the record's no-case sentence does not name them:
- 168 "continued with none begun"; 210 "a Senate listing in the -table reading alone"
- 219 "one race, a special declared"; 220 "one race under a term heading"; 221 "N races"
- 245 "the party line ... with no candidate before it"
- 261 "footnote mark(s) declared and not found". r10 empties %marked, so it reaches 236, never 261.
- 272 "no Senator row"
- 277 "the Senator row's cells sum to $s, its Total $tot". r02 changes a listing figure, not the recap row, so only 278 fires.
- 281 "the unnamed ... declared and not found"
- 295 "Class I states, not 33"; 300 "the Class I states without a full-term race ... races outside Class I"
- 336 "W marks, not one"; 344 "a difference declared and not met"

Several of these are refusals the tool's own header names: line 15 (a state of one race carrying a heading), line 17 (a fusion line with no candidate before it), line 19 (cells not summing to the Total), line 20 (the races not the declared ones), line 22 (a race without one W).

So the no-case list is incomplete, and a reader would take every other refusal as proved by a case. The tool does refuse correctly at each site, so no figure, class, winner or race is wrong. The fault is in what the record says about the suite, which makes this a documentation gap: minor. If the sentence is read as exhaustive, it could be graded a false claim.

**The skeptic's corrected fix.** Either list every uncovered site in the record's 'Refusals with no case' sentence (168, 210, 219, 220, 221, 245, 261, 272, 277, 281, 295, 300, 336, 344, plus the five already named), or add cases for the ones the header names and list the rest. Suggested cases:
- the AZ 2018 recap row with one cell changed and its Total kept (277)
- a CT or NY fusion line moved before its candidate (245)
- a "(For full term ...)" heading injected into a one-race state (220)
- one Class I state's listing dropped (300)
- a W mark removed from the FEC sheet (336)
- %marked declared for a state with no marks (261)
- a fake %votes_differ entry (344)

### 20. Header claims a refusal for 'a race of a term the year does not hold'; full-term headings' dates are never checked

- **Lens:** docs-suite - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_races_prep.pl:15

**The scenario.** The heading's date ($r->{when}) is compared only for unexpired races with a declared special (line 227). A 2018 two-race state's '(For full term beginning ...)' date is captured and never compared with the year. If the 2018 volume printed 'beginning January 3, 2025', or a 2024 one 'beginning January 3, 2019', the run would pass.

**The fix proposed.** Hold each full-term heading's date to 'January 3, <year+1>' and add a case. Otherwise narrow the header to the unexpired term's end.

**The skeptic's evidence.** The gap is real, but it is narrow and the data is correct.

What the header claims: Tools/us_senate_races_prep.pl lines 15-16 (staged) list among the die conditions "a race of a term the year does not hold".

What the code checks: read_volume (line 176) captures the heading's date as $r->{when} for both forms: `^\(For (unexpired|full) term (?:ending|beginning) ([A-Z][a-z]+ \d+, \d{4})\)$`. That date is compared in only one place, line 227:
`problem("$year $st: an unexpired term ending $r->{when}, declared $sp->{ends}") if $sp && $r->{when} ne $sp->{ends};`
That runs only inside `elsif ($r->{term} eq 'unexpired')`. A full-term race takes `else { $class = 1; }` and its `when` is never read again.
- Line 220 tests only whether `when` is empty (a one-race state carrying a heading).
- The declared-races check (lines 296-300) looks at year, class and term, not the date.
- The winner check uses a date built from the year (line 357), not the heading.
- The mutations script has no full-term date case. r09 / case at line 74 mutates only Minnesota's unexpired `ends`.

So if a two-race state's volume printed "(For full term beginning January 3, 2025)" in 2018, the run would still pass. The header's wording is broader than what is checked.

Why it is minor, not a defect: the saved volumes are pinned by SHA256SUMS, and pdftotext -raw shows every full-term heading the tool reads carries the right date:
- 2018 MN and MS: "beginning January 3, 2019"
- 2020 NE: "beginning January 3, 2021"
- 2024 CA and NE: "beginning January 3, 2025"

No catalog figure, class, winner or race is wrong. Also, single-race states print no heading at all (when = ''), so the only full-term dates to check are those of the five two-race readings. That makes this a gap or ambiguity between the header and the check, not a check passing on wrong data.

**The skeptic's corrected fix.** Either hold every non-empty full-term `when` to "January 3, " . ($year + 1), e.g. in the `else { $class = 1; }` branch: `problem("$year $st: a full term beginning $r->{when}") if $r->{when} ne '' && $r->{when} ne 'January 3, ' . ($year + 1);`, and add a mutation case that edits a copied -raw/-table reading, or a declared constant if one is introduced. Or narrow the header line 15-16 to what is checked: "an unexpired term ending other than its declared special's end".

### 21. Aggregate write-in lines go into votes_non, but the record and the CSV header leave them out of 'not candidates'

- **Lens:** docs-suite - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/senate_record.md:114

**The scenario.** %noncand (tool:141-143) includes 'Write-in', 'Write-ins', 'Other Write-ins', 'Write-in (Scattering)', 'Write-in, Scatterings' and 'Miscellaneous'. Nameless write-in lines in MN, ND, RI, VA, VT, WY, WA 2024, MD and NE are therefore counted in votes_non. Reading 4 and the CSV header (tool:388) list only blanks, over/under votes, void, others, scatterings and None of These Candidates. Reading 2's 'every write-in candidate ... other' then leads a reader to expect, say, WA 2024's write-ins in votes_other.

**The fix proposed.** Add 'write-in lines naming no candidate' to reading 4 and to the CSV header's votes_non description.

**The skeptic's evidence.** Tools/us_senate_races_prep.pl:141-143 puts 'Write-in', 'Write-ins', 'Other Write-ins', 'Write-in (Scattering)', 'Write-in, Scatterings' and 'Miscellaneous' in %noncand. At :242, `if ($noncand{lc $label}) { $x->{non} += $v; next; }` runs before any candidate classification, so a nameless write-in line goes into votes_non. These lines do occur in Senate listings. `pdftotext -raw` of clerk_statistics2024.pdf prints WASHINGTON's listing as "FOR UNITED STATES SENATOR / Raul Garcia, Republican / Maria Cantwell, Democrat / Write-in ... 10,627", and the 2018 listing for VIRGINIA ends with "Write-in ... 5,125". The record's reading 4 (senate_record.md, staged diff line 26) names only "blanks, over and under votes, void, others, scatterings and Nevada's None of These Candidates". The CSV header (tool ~line 388) says "votes_non: the lines that are not candidates (blanks, over and under votes, scatterings, None of These Candidates)". Neither names nameless write-in lines or 'Miscellaneous', and the header also leaves out void and others. Reading 2's "every write-in candidate ... other" can be read as applying only to named write-in candidates, so the readings don't contradict each other, and no figure, class or winner is wrong. The gap is that the declared list of non-candidate lines is incomplete or ambiguous, so it is a minor finding, not a defect.

**The skeptic's corrected fix.** In reading 4 and in the CSV header's votes_non sentence, name the write-in lines that name no candidate ("Write-in", "Write-ins", "Other Write-ins", "Write-in (Scattering)", "Write-in, Scatterings") and "Miscellaneous". Make the header's list match reading 4 (add void and others). Then regenerate.

### 22. The 'Nonpartisan' => I mapping is declared in the tool but not in the record or the CSV header

- **Lens:** docs-suite - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_senate_races_prep.pl:128

**The scenario.** %label_class maps 'Nonpartisan' to I. Reading 2 and the CSV header (lines 386-387) list the I labels without it. No saved listing uses it (Mississippi's special prints party labels; only the FEC codes it N). If a future page printed 'X, Nonpartisan', it would be filed as an independent silently, under a premise the record never states.

**The fix proposed.** Remove the unused entry, so an undeclared label stops the run, or name it in reading 2 and the CSV header.

**The skeptic's evidence.** The mismatch is real, but the entry is never used, so no figure, class or winner in the catalog is wrong.

- **The tool maps it.** Staged Tools/us_senate_races_prep.pl line 128 maps `'Nonpartisan' => 'I'` in %label_class.
- **The record and header leave it out.** Reading 2 of the staged ElectionsData/usa/senate_record.md (line 112) lists the I labels as "Independent, Unaffiliated, No Political Party, No Party Affiliation, By Petition". The CSV header text at lines 386-387 of the tool gives the same five. Neither names Nonpartisan.
- **It is unused.** I searched all three Clerk volumes with pdftotext -raw for "Nonpartisan" (case-insensitive). Every bare ", Nonpartisan" candidate is in Hawaii's FOR UNITED STATES REPRESENTATIVE section: 2018 Calvin C. "G" Griffin, 2020 Ron Burrus, 2024 Randall Kelly Meyer. Every other hit is "Democratic-Nonpartisan League", which has its own D entry, or a recap heading. No Senate race reaches the entry.
- **Why it is still a gap.** The tool's table holds a premise the record does not state, which goes against the rule that every declared premise is said so. If a Senate listing ever printed ", Nonpartisan", that candidate would be filed as I with no stated basis.
- **The proposed removal does not do what the finding says.** label_class() stops the run (returns undef) only for labels matching /democrat|republic|independ|gop\b/i. "Nonpartisan" matches none of those, so with the entry deleted it would fall through to `return 'O'` and be filed as other, again silently, not stop the run.

**The skeptic's corrected fix.** Either name Nonpartisan among the I labels in reading 2 and in the CSV header text (lines 386-387), or delete the entry and add nonpartisan to the undeclared-form regex in label_class(), e.g. /democrat|republic|independ|nonpartisan|gop\b/i. Exempt the declared 'Democratic-Nonpartisan League' first, which already happens because the exists-check runs before the regex. Only that second route makes a future 'X, Nonpartisan' stop the run; deleting the entry on its own would file it as other, still silently.

### 23. CheckUsSenateRaces claims 'Nebraska's 2020 alone' and 'the specials' but counts rows only

- **Lens:** docs-suite - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/GeneratedCatalogCheck.cs:1068

**The scenario.** The check requires one 2020 row, 35 rows per even year, and the full-term Class I set. It never requires the 2020 row's state to be NE, or the extra rows to be the declared specials (2018 MN/MS Class II, 2024 CA 1u/NE 2). A catalog whose 2020 row was another state's Class II race, or whose 2018 special was a different state's, would pass if the winner's surname matched that seat's holder. The record line 133 and the success message claim more than this.

**The fix proposed.** Hold the 2020 row to NE class 2 and the unexpired rows to the declared (year, state, class) set, or narrow the wording.

**The skeptic's evidence.** This could not be refuted. In GeneratedCatalogCheck.CheckUsSenateRaces, the only year and state structure tests are these:
- `perYear` counts: `y18 != 35 || y24 != 35 || y20 != 1`.
- `fullI[y].SetEquals(classI)` for 2018 and 2024, which only collects rows where `r.Term == "full" && r.Class == 1`.
- Per row, the winner's surname is matched against the seat holder of `(r.State, r.Class)` on the race's day.

No line tests that the 2020 row has `State == "NE"` or `Class == 2`. No line tests that the two extra rows in 2018 and 2024 are the declared set: 2018 MN/2 and MS/2 unexpired, 2024 CA/1 unexpired and NE/2 unexpired.

Failing path: replace the 2020 row in both the catalog and the CSV with another state's 2020 Class II race (for example GA/2, winner Ossoff, who holds that seat on 2023-01-03). All the checks still pass: per-year count 1, CSV equals catalog, winner equals holder. The same happens if a 2018 special row is swapped for another state's seat whose holder matches. The set semantics of `fullI` also allow both extra rows to be a second full-term Class I row of an existing state.

Yet the success message prints "...and the specials, 2020 Nebraska alone; every winner his seat's holder". The summary says "Nebraska's Class II race of 2020 alone". The record's CheckUsSenateRaces paragraph (senate_record.md, new line ~133) says "and Nebraska's 2020 alone". These claim more than the check verifies.

Mitigation, which keeps this minor rather than a defect: the generator enforces both properties.
- `Tools/us_senate_races_prep.pl` line 303: `problem(...) unless @y2020 == 1 && $y2020[0]{st} eq 'NE'`
- the `%special` / `%only` tables (lines 119-123) and the per-state special checks (lines 215-227)

The committed data is also correct: CSV rows 2020,NE,2; 2018 MN/MS 2 unexpired; 2024 CA 1 unexpired; NE 2 unexpired. So no wrong row exists today. The issue is that the cheap-bar check overclaims what it independently holds.

**The skeptic's corrected fix.** In CheckUsSenateRaces, hold the non-full-Class-I rows to the declared set. Build a set of `year|state|class|term` from the rows that are not (`Term == "full" && Class == 1`). Require it to equal {"2018|MN|2|unexpired", "2018|MS|2|unexpired", "2020|NE|2|full", "2024|CA|1|unexpired", "2024|NE|2|unexpired"}. Also require the full-term Class I rows of each year to number exactly 33, so duplicates fail. Alternatively, narrow the summary, the success message and senate_record.md's CheckUsSenateRaces line to the row counts and the Class I set that the check actually verifies.

### 24. The winner-holds-seat checks match surnames only, which is vacuous for a 'III' suffix

- **Lens:** docs-suite - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_senate_races_prep.pl:353

**The scenario.** last_word (tool) and Last (check) strip only Jr/Sr. For 2018 WV, 'Joe Manchin III' gives 'iii' on both sides, so any winner named '... III' would match. The record's reading 10 does not say the match is by surname.

**The fix proposed.** Strip II/III/IV as %suffix does, and say 'by surname' in reading 10.

**The skeptic's evidence.** The finding holds, but nothing wrong passes today. At Tools/us_senate_races_prep.pl:353, `last_word` strips only Jr/Sr: `$n =~ s/,? (?:Jr|Sr)\.?\s*$//`. The same tool's `%suffix` at line 146 does list ('Jr.', 'Sr.', 'II', 'III', 'IV'). The C# check in GeneratedCatalogCheck.cs has the same gap: `t = Regex.Replace(t, @" (Jr|Sr)$", "")` in `Last`, compared at `Last(holder) != Last(r.Winner)`. The data has exactly one such name. senate_seats.csv has `WV,1,Joe Manchin III,...` and senate_races.csv has `2018,WV,1,full,...,Joe Manchin III,D,-`, so both sides reduce to "iii". For that race the reading-10 check therefore compares only the suffix. A different winner whose name also ended in III would pass, but none exists. The comparison is against the holder of that one state and class on that day (`holder_on`). The 2018 WV winner is also checked by votes and party against the FEC sheet (lines 340-342) and by leading in votes. The real winner, Joe Manchin III, is correct, so no wrong winner or figure is in the catalog. This is a check that is weaker than reading 10 says ("Every winner holds his seat"). It is not a check passing on wrong data, so it is graded a note. The match is by surname in general, which the record does not say. The other suffixed names (King Jr., Casey Jr. with '(Bob)') reduce to their surnames correctly.

**The skeptic's corrected fix.** In the Perl `last_word`, change the regex to `s/,? (?:Jr|Sr|II|III|IV)\.?\s*$//`. In the C# `Last`, change it to `Regex.Replace(t, @" (Jr|Sr|II|III|IV)$", "")`. 'Joe Manchin III' then gives 'manchin' on both sides. Add 'by surname' to the record's reading 10 and to the CheckUsSenateRaces paragraph.

### 25. The header promises a finalist's votes against the FEC; only the W row is compared

- **Lens:** docs-suite - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_senate_races_prep.pl:22

**The scenario.** Lines 333-343 compare only the FEC's W row: the winner, and for Mississippi Hyde-Smith's runoff. Espy's runoff figure and every losing candidate are not compared. For Ohio, the FEC's Republican and write-in figures also differ from the Clerk's (unexamined). Reading 9's 'Ohio is the one difference' is true only of winners.

**The fix proposed.** Change the header to 'the winner's votes'. In reading 9, say the comparison is of the W row alone.

**The skeptic's evidence.** The finding is real, but only for the tool's header. The record's half of it does not hold.

The header (Tools/us_senate_races_prep.pl, line 22) says the tool dies on "a race without one W, the W's party not the Clerk's winner's, or a finalist's votes not the Clerk's but where declared".

The code compares only rows the FEC marks W:
- `next unless trim($c->{$r}{$col{'GE WINNER INDICATOR'}} // '') eq 'W';` keeps only the W rows.
- Each race's single W row gives one vote figure (runoff, else fused total, else general).
- `problem(... ) unless $v == $x->{winner}{total};` holds that figure to the Clerk's winner only.

No loser's figure is compared. In Mississippi's special that includes Espy, the other runoff finalist. So "a finalist's votes" claims more than the code checks; it should read "the W's (the winner's) votes". The check itself is correct and passes on correct data, so this is wording, not a defect.

Reading 9 in senate_record.md is accurate as written. It says "one winner mark a race, its party the Clerk's winner's, its votes the Clerk's winner's". So "Ohio is the one difference: the FEC prints its winner's votes larger" is already limited to the winner comparison, and that part of the suggested fix is not needed.

The claim that Ohio's other FEC figures differ from the Clerk's was not checked here and is not needed to support this finding.

**The skeptic's corrected fix.** In the header at line 22, replace "or a finalist's votes not the Clerk's but where declared" with "or the W's votes (the fused total, the runoff, or the general) not the Clerk's winner's but where declared; no loser's figure is compared". Reading 9 can stay as it is, because it already speaks of the winner's votes. Optionally add "(the W row alone)" after "its votes the Clerk's winner's".

## The first pass - refuted by the skeptics

- [tool-vs-sources] Delaware 2024: Katz is filed I from the Clerk's 'Independent', and no saved page tests whether he stood for a party - *The tool's classification of Katz matches the Clerk's 2024 volume twice over, and the record already says that the class is read from the printed label.

1. The listing in clerk_statistics2024.pdf (pdftotext -raw) prints the label as plain 'Independent': `Michael "Dr. Mike" Katz, Independent .... 19,555`. Tools/us_senate_races_prep.pl line 128 maps 'Independent' => 'I'.

2. The Clerk's own Delaware recapitulation keeps Katz apart from the minor party. Its columns are Republican, Democratic, Conservative, Independent, Independent Party of Delaware, Libertarian, Write-in, Total. The raw Senator row is `Senator ............. 197,753 283,298 ........................... 19,555 ........................... ........................... ........................... 500,606`. So 19,555 sits in the 'Independent' column (after the empty Conservative cell), and the 'Independent Party of Delaware' cell is dots. The presidential IPoD line (4,636) is in that separate column. The source does not file Katz as an IPoD candidate. The tool already checks each state's recapitulation Senator row, so that row is part of what the run tests.

3. The finding's suggested declaration is already there. Reading 2 of the record (senate_record.md, the 'Each candidate to a class' item) maps each printed label to a class: I = Independent / Unaffiliated / No Political Party / No Party Affiliation / By Petition. A label not declared stops the run. The CSV header comment says the same. So 'I follows the Clerk's printed label' is stated in substance.

4. None of the saved sources (raw/returns: Clerk volumes, the FEC 2018-2022 workbooks, FEC 2024 presidential only) names a different party for Katz. A failing path would need such a source. The finding offers none and calls its own case a match to the declared reading.

The row (2024,DE,1,full,197753,283298,19555,0,...) is therefore faithful to the source and to the declared rule. Under the review's severity scale this is at most optional wording, not a defect or a gap.*
- [docs-suite] Readings 6 and 7 restate run results in prose - *The finding is anchored one line off. Line 116 of ElectionsData/usa/senate_record.md is reading 6, the Vermont/Nebraska line. Line 117 is reading 7, the Maine line. Both sentences are accurate and both are allowed.

(1) Reading 6 ("California 2018 is two Democrats (its top two); Vermont (both years) and Nebraska's 2024 Class I race print no Democrat") agrees with the pasted run's flags line (2018 CA S; 2018 VT N; 2024 VT N; 2024 NE N) and with Tools/us_senate_races_prep.pl:268 (`$x->{flags}{N} = 1 unless $n{D};`). These are facts about the source PDFs, which are historical and fixed, not facts about the code. The tool re-hashes those PDFs, as does GeneratedCatalogCheck.CheckUsSenateRaces ("every page re-hashed"). The claim convention's test in CLAUDE.md is "the code can change freely and no document becomes wrong". These sentences pass it: they can only become wrong if the hashed sources change, and then the bar fails. In the record the sentence does a job: it explains why the winner rule is the candidates' plurality and not a D-vs-R contest. It does not merely repeat the flags.

(2) Reading 7 ("it is a final count only if its winner has a majority ..., which each year's has - held") states what the tool enforces. A missing majority stops the run, at prep.pl:308: `problem("$x->{year} ME: ... no majority ...") unless 2 * $x->{winner}{total} > $c;`. The record also lists "Maine's majority" among the tool's refusals. So the sentence cannot become false while the run still passes. The section is a dated record of a run ("The run (2026-10-06, pasted)").

(3) The same style was already in place in §792's half of this file. Reading 6 there says the Directory's chosen days "differ ... in all four of its rows (Ricketts, Butler, Husted, Moody)" and "as they do". So this commit adds nothing new in kind. DocumentClaimCheck scans only root-level *.md (Assets/Editor/DocumentClaimCheck.cs:124, TopDirectoryOnly), so ElectionsData records are outside its scope.

Nothing here is false, missing or ambiguous. At most it is a matter of style, and it does not reach note level under the given rubric.*

## What was done about the pass

Twenty-five findings survived, several of them one defect seen through two or three lenses; every one was acted on. The tool was changed in eight places, the catalog check in four, the suite grown from 19 cases to 25, and the record and the tool's header corrected.

- **Nebraska's 2024 special was held to the wrong day (1, 12, 16).** The day expression tested the term before the class, so the Class II branch never ran and Nebraska's special was held to 9 Dec 2024, not the 3 Jan 2025 the record names. The class is now tested first, in the tool and in the check alike.
- **"Every winner leading" was never tested (2, 11, 17).** The check asked only that the winner's class held votes. Now the winner's class must be one of R, D, I and other, and its votes no fewer than any other class's (option (b) of 17; the class of the seat's holder is not compared).
- **The specials and Nebraska's 2020 race were counted, not named (13, 23).** The rows beside the full Class I terms must now be exactly the five declared; the full Class I races 33 a year; no race twice.
- **The surname match was vacuous for "III" (14, 24).** `last_word` and `Last` set aside Jr., Sr., II, III and IV, so Manchin III is matched on "manchin". Case w02 (another holder under the same III).
- **Mississippi's footnotes were never read (3).** The -raw reading now captures each runoff footnote by its digit, and each mark glued to a figure must be held to the footnote naming that candidate. Case r13.
- **A line in the listing that is no entry was skipped silently (6).** It now stops the run. Case r11.
- **The full-term heading's date was never checked (7, 20).** A full term must begin on 3 January after the election. Case r12.
- **Arizona's unnamed "Green" (9).** Held to the FEC's one `W(GRE)/GRE` line in that race: her surname the label, her general votes the entry's. Case f03.
- **The labels (10, 18, 22).** `Nonpartisan` and New York's `Independence` are gone from the label table (no candidate's label in either year is either); "nonpartisan" joins the undeclared-form stop, so a candidate labelled so stops the run; reading 2 says New York's nameless Independence line is fusion.
- **The write-in totals (4, 21).** The CSV header and reading 4 name the write-in totals with no name as not candidates, with the nineteen such lines listed.
- **The recapitulation's reach (5).** Not built as code: the party columns' headings wrap across lines in both readings. Reading 8 now states the limit - the Total alone is held, so a figure moved between candidates, classes or a state's two races would pass it - and what guards those instead (the two readings' agreement, the term headings). Case r14 proves a cell changed.
- **The 2018 Class II specials (15).** Reading 10 and the check's summary say that on 3 Jan 2023 Smith and Hyde-Smith hold their seats by their 2020 elections, so for them the day tests the person, not the 2018 term; Nebraska's 2020 winner is Sasse, held in his term's third year.
- **The headers and the refusal list (8, 19, 25).** The header's FEC line says the W's votes are compared and no loser's; the record's "Refusals with no case" names every site the tool holds without a case.
