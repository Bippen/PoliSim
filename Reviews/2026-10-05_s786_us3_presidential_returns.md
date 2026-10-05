# Review - §786, PS-6 US-3: the US presidential returns by jurisdiction from saved pages, and the Electoral College exact on four elections (2026-10-05)

A workflow review (`polisim-staged-review`) of the change that builds US-3: `Tools/us_returns_prep.pl` and its generated catalog `UsPresidentialReturns` with three CSVs, `ElectoralCollege.FromCatalog`, the catalog's block in `GeneratedCatalogCheck`, `SeatAllocationBacktest`'s four-year loop, the record `ElectionsData/usa/president_returns.md`, and the 2024 records brought to the raw-page standard. Not required by the tier (`Tools/bar_tier.ps1`: SIMULATION, no money path, no sentinel family); run because the catalog stands on parsers and readings. Two passes; every finding was put to a refute-first skeptic, who re-graded it; every report below is verbatim.

## The first pass - confirmed (verbatim)

The workflow `polisim-staged-review` (run wf_e2e19af4-c8c, 38 agents) over the staged diff at HEAD 5fe3c031: five lenses (the generator's readers, its failure paths, the C# side, the records' claims, an independent read of the figures), every finding put to a refute-first skeptic who re-graded it. Every report below is verbatim.

### 1. District identity is never held to any source: the Nebraska heading-to-Total binding and the Maine READ labels can be permuted and every check stays green

- **Lens:** parse - **reviewer:** minor - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_returns_prep.pl:465

**The scenario.** The checks on a district are: its sum against the FEC state row (472/489), the READ bound (521-527), no tie, and the split check at 561. That split check counts how many districts each party won, not WHICH ones won. FromCatalog uses the winners only as a list, so it is identity-blind too. Two runs on my scratch copy, both exit 0 with the CSV written. (a) READ transposition: I swapped 2016 ME D 212774 and 144817 between districts 1 and 2 in maine_districts_read_2012_2016.tsv. The output was 2016,ME,1,144817,154384,READ (Trump carries ME-1) and 2016,ME,2,212774,181177. (b) Nebraska: a pdftotext wrapper exchanged the 2020 book's 'Congressional District 1' and 'Congressional District 2' heading lines; line 465 binds each Total to the last heading seen. The output was 2020,NE,1,176468,154377 (Biden carries NE-1) and 2020,NE,2,132261,180290. Both contradict NARA's own notes, on pages the tool already hashes and reads. 2016 says 'Clinton/Kaine won in the First Congressional District ... Trump/Pence won the Second Congressional District', and 2020 says 'Biden-Harris won the Second Congressional District'. A heading/Total mis-binding of this kind arises if a re-issued book's content stream draws a heading after its table, or if the line-53 fallback runs a different pdftotext; the tool does not record which extractor ran. district_method_2024.md (its NE-2/ME-2 table, lines 59/62, and the new s786 section) now says these rows back its per-district figures 'figure for figure', so they would publish wrong district labels while every check passes. On the saved files the binding is correct: I read the xpdf 4.06 text around every heading in all four books. Each district's first two-number Total is the president's, Republican first, and the president's section comes before the House section.

**The fix proposed.** Parse NARA's district-winner sentences, already on the four results pages the tool reads. All five split cases carry one: ME 2016/2020/2024 and NE 2020/2024. Their form is '<ticket> won (in) the First|Second|Third [and Third] Congressional District(s)'; attribute each to its state by the note it sits in ('Maine appoints...' / 'Nebraska appoints...'). Call problem() unless each district's plurality winner in %dist matches. For the non-split cases (2012 ME/NE, 2016 NE), state in president_returns.md that district identity rests on the canvass heading or the READ label alone. Optionally have the tool print `pdftotext -v` with its result.

**The skeptic's evidence.** The mechanism is real and I reproduced it. The scenario does not happen on the committed inputs, nothing reads district identity, and the ruling does not ask for it. So this is a hardening note, not a defect.

How the code works (Tools/us_returns_prep.pl):
- Nebraska's district label comes from position only. Line 463 sets `$cd` from the last heading seen; line 465 binds the next `Total` to it.
- The only checks on a district are:
  - the sums (472 for NE, 489 for ME 2020/2024);
  - the READ bound (521-527);
  - tie and plurality (542-546);
  - the split check at 561, `$got{$d->{D} > $d->{R} ? 'D' : 'R'}++`, which line 566 compares per state against NARA's table. That is a count of districts won, not which ones.
- The downstream code is blind to identity too. `ElectoralCollege.FromCatalog` (line 68) only does `districtWinners.Add(...)`; the district number is never used. The backtest and GeneratedCatalogCheck only count.
- NARA's notes say which district each ticket won. The tool reads and hashes these pages but never parses those sentences:
  - 2016: "Clinton/Kaine won in the First Congressional District and took the state; Trump/Pence won the Second Congressional District."
  - 2020 and 2024: the same sentence for Maine, plus Nebraska: "...won in the First and Third Congressional Districts and took the state; Biden-Harris [Harris-Walz] won the Second Congressional District."
  - 2012 has no such note, and 2016 has none for Nebraska.

Reproduced on a scratch copy:
- (a) I swapped the 2016 Maine D figures between districts in the TSV. The tool exited 0 and wrote `2016,ME,1,144817,154384,READ`.
- (b) A pdftotext wrapper swapped the 2020 Nebraska book's district 1 and 2 headings. The tool exited 0 and wrote `2020,NE,1,176468,154377` and `2020,NE,2,132261,180290`.

What limits the risk:
- The current output is right. Under xpdf 4.06 (the only pdftotext on PATH), every heading in all four books binds to the president's Total. For example, 2020 lines 533→546 give 180,290 / 132,261, 565→578 give 154,377 / 176,468, and 2024 lines 615→636 give 148,905 / 163,541. The 2016 certificate's text layer lists First then Second as the TSV does, and all five split cases agree with NARA's notes.
- (b) is not realistic. The PDFs are pinned by SHA-256 at line 74, and `-raw` follows content-stream order. Any mis-binding other than a pure permutation fails the district-count check (468) or the sum (472).
- (a) needs a future same-party transposition in the hand-READ TSV. Even the proposed fix would not catch a transposition that keeps the winners: a 2016 R swap, or any 2012 swap.
- Nothing over-claims. The tool's header list and president_returns.md "What the tool proves" never say district identity is checked. Reading 9's sum claim is about R/D swaps and House totals, and it is accurate.
- The title overstates. Maine 2020/2024 identity is held twice to the workbook's own labels: the sheet `CG$cd` (478) and the row `/^CG\s*$cd\s+Total$/` (482).

Housekeeping: I ran nothing that writes inside the repo. My scratch copy (scratchpad/us3) was removed. I ran `rm -rf` on that path before creating it, and no file in the scratchpad referred to a us3/ directory.

**The skeptic's corrected fix.** This is optional hardening; the ruling does not require it.

1. Parse NARA's sentences. In the NARA loop, match `/(Maine|Nebraska) appoints its electors proportionally\. (.+?) won in the (.+?) Congressional Districts? and took the state; (.+?) won the (.+?) Congressional Districts?\./`. Map each ticket's first name (split on `-` or `/`) to D or R with `surname($n{name}{...})`, and map First, Second and Third (split on " and ") to 1, 2 and 3.
2. Check them after the Maine READ block:
   - `problem()` unless each named district's plurality winner in `%dist` matches;
   - unless the party that "took the state" is the statewide plurality winner;
   - and if the record finds a district won against its state that NARA does not name.
3. Say what stays uncovered. In president_returns.md readings 1 and 9, state that two kinds of identity rest on the canvass heading or the READ label alone: districts with no NARA sentence (2012 ME and NE, 2016 NE), and transpositions that keep the winners (for example the 2016 Maine R rows).
4. Optionally have the run print `pdftotext -v`.

### 2. 2012 Table 2's national All Others and Total Vote cells are cached SUM formulas over the state rows, so the 'sum to the FEC's national totals' check is self-referential for those two columns in 2012

- **Lens:** parse - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_returns_prep.pl:382

**The scenario.** xls_sheet (line 214) returns a FORMULA record's cached result as a plain number, indistinguishable from a typed cell. I decoded the token streams of the five FORMULA records in fec_federalelections2012.xls Table 2. R57C6 (Total row, All Others) is SUM(F6:F56); R57C7 (Total Vote) is SUM(D57:F57); R58C4-6 are percentages. So for 2012, line 382's `$sum{other} == $tot{6}` compares the state F cells with Excel's sum of the same cells. `$sum{total} == $tot{7}` then follows from the D and E checks plus the per-state D+E+F==G check. Both can catch a reader error but never a discrepancy in the source. The ruling's 'each year's jurisdictions sum to the FEC's national totals' holds independently in 2012 only for B57:E57, which are typed (RK/MULRK). Table 2 of 2016 and 2020 (sheet3.xml) and the 2024 workbook carry no formulas at all.

**The fix proposed.** Have xls_sheet tag FORMULA-derived cells (e.g. return a second hash of formula positions) and report, or refuse, a 'national total' that is a formula. At minimum, say in president_returns.md that 2012's national All Others and Total Vote are the sheet's own SUM of its state rows, typed nowhere.

**The skeptic's evidence.** CONFIRMED at the byte and token level. I dumped the hash-checked fec_federalelections2012.xls (b92a2bd7...) with a scratchpad copy of the tool's own BIFF8 parsing, which also tags record types and decodes token streams.

What Table 2 holds:
- It has exactly 5 FORMULA records:
  - F57: area(F6:F56) tAttrSum, i.e. SUM(F6:F56), cached 2236111
  - G57: SUM(D57:F57), cached 129085410
  - D58, E58, F58: ref/ref division (percentages)
- B57:E57 are one MULRK record, so those four cells are typed.
- Every state cell B6:G56 is RK or MULRK. G is typed in all 51 rows.

How the tool uses it:
- Line 214 stores a FORMULA's cached double exactly like a typed number: `else { $cells{$row}{$col} = unpack('d', $res); }`
- Line 366 puts F57 and G57 into %tot{6} and %tot{7}.
- Line 382 then tests `$sum{other} == $tot{6}`. That compares the sum of F6:F56 with Excel's own SUM of those same cells, so it cannot fail on the source's content. It can only catch a reader error.
- `$sum{total} == $tot{7}` is implied. Line 383's per-state D+E+F==G against typed G gives sum G = sum D + sum E + sum F. The typed D57/E57 checks then make that D57 + E57 + F57, which is G57.

The other workbooks:
- 2016 and 2020 Table 2 (both xl/worksheets/sheet3.xml) have 0 `<f>` elements.
- The 2024 sheet1.xml has 0 `<f>`.
- The Census 2010 .xls and 2020 .xlsx have no formulas either.
- So 2012's Table 2 is the only place the tool relies on a formula cell.

Why it is only a note:
1. The inputs are SHA-256-pinned, and for these bytes nothing is hidden. The same workbook's sheet 'Table 1. 2012 Pres Popular Vote' has 0 FORMULA records and types every national figure as RK: Obama at B6 and Romney at B7 (equal to Table 2's typed D57 and E57), 28 other candidates at B8:B35 (their sum equals F57's cached value), and 'Total:' at B36 (equal to G57's cached value). So the figures do sum to typed FEC national totals; the tool just proves it against the formula row.
2. Reader errors on F and G are still caught twice: by Excel's SUM and by the per-state check against typed G.
3. Nothing overclaims. The tool's header (line 30) and president_returns.md line 20 say only "each Table 2's state rows sum to its Total row", which is literally what is done. The stage plan's Built line makes no national-total claim.

The finding's fallback fix is factually wrong: 2012's national total vote is NOT "typed nowhere". Table 1 B36 types it, and Table 1's typed candidate rows give All Others too.

**The skeptic's corrected fix.** Do not put the finding's wording "typed nowhere" into the record: it is false, because Table 1 of the same 2012 workbook types every national figure.

Two cheap options:

(a) In the tool (preferred): read the 2012 workbook's sheet 'Table 1. 2012 Pres Popular Vote' with xls_sheet. It has no FORMULA records. Take its typed rows by label: the two nominees' rows, found by NARA's surnames, and the 'Total:' row. Hold Table 2's state sums of D, E and G to them, and the F sum to Total minus the two nominees. 2012's national check then runs against typed FEC figures, like 2016-2024's. To make this fail-closed for later sheets, have xls_sheet also return the positions of FORMULA records. Then the Total-row reader refuses (problem()) any national-total cell that is a formula unless a typed counterpart is checked.

(b) If the tool stays as it is: add one sentence to president_returns.md (reading 7, or the 'What the tool proves' list), with no figures transcribed, per the claim convention. Say that 2012's Table 2 prints its Total row's All Others and Total Vote as SUM formulas over its own rows, so for those two cells the check proves the reader only. Then say the national total vote is typed in the same workbook's Table 1 and agrees, if that was checked (it was, read-only, in this review).

### 3. The Maine 2012/2016 READ bound only catches gross errors: a hundreds-digit misread passes

- **Lens:** parse - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Tools/us_returns_prep.pl:527

**The scenario.** Lines 521-527 fail a READ figure only if a party's district sum exceeds the state, or if the D+R shortfall exceeds the certificate's unallocated count. 2012: the shortfall is D 273 + R 124 = 397 against 415 unallocated, so any figure may be over-read by up to its party's gap (D +273, R +124) or under-read by up to 18. 2016: D 144 + R 32 = 176 against 184, so +144 / +32 / -8. On my scratch copy I changed 2012 ME-1 D from 223035 to 223235 in the TSV. The tool exited 0 and wrote 2012,ME,1,223235,142937,READ. The record's wording at president_returns.md line 23 is accurate. The digits rest on the readers' agreement (three readers for 2012, two for 2016), not on the tool.

**The fix proposed.** No code change needed. In reading 1 of president_returns.md, state how much the bound tolerates (the per-party gaps and the 18 / 8 slack). That makes clear the multi-reader agreement, not the tool, is the evidence for the digits.

**The skeptic's evidence.** The only check on the READ figures is Tools/us_returns_prep.pl lines 521-527. Line 523 computes each party's gap `$g = $e->{"v$p"} - d1 - d2`. Line 524 fails only if `$g < 0`. Line 527 fails only if the D gap plus the R gap exceeds `$unalloc{$year}`. Line 509, `page($certrel)`, hashes the certificate but never reads its figures. Nothing downstream holds the digits:
- the tie and others checks (540, 543) and the winners check (561/566) catch only a flipped district;
- GeneratedCatalogCheck.CheckUsPresidentialReturns only compares the TSV's bytes to `ReadTranscriptionDigest` (drift since generation), checks the CSVs against the catalog, and runs the allocator split. A misread present at generation passes all three.

Arithmetic against president_by_state.csv (2012 ME D 401306 R 292276; 2016 D 357735 R 335593):
- 2012: gaps D 273, R 124, sum 397 against 415, slack 18.
- 2016: gaps D 144, R 32, sum 176 against 184, slack 8.

Reproduced on a scratch copy. The unmutated run gives byte-identical CSVs and .cs. Results:
- 2012 ME-1 D 223035→223235: exit 0, writes `2012,ME,1,223235,142937,READ`.
- Exact edges: D +273 passes, +274 fails ("exceed the state by 1"); -18 passes, -19 fails ("416 short ... 415"). 2012 R +124 passes. 2016 D +144, R +32 and R -8 pass; R -9 fails.
- Worse than the finding says: swapping 2012's two D district figures also exits 0, because sums and winners are unchanged.

Why it stays a note and not a defect: nothing in the change claims more than the bound.
- president_returns.md line 3 marks the READ rows PROVISIONAL pending "a second reading of the certificates", not a re-run of the tool.
- Line 23 states only "never exceed the state and fall short of it by no more than the overseas votes".
- Line 28's mutation list names only "raised past the state / lowered past the overseas count / flips its winner".
- Reading 1 (line 32) names the readers (three for 2012, two for 2016).
- The TSV header says the tool holds "each party's shortfall to that count", which is accurate.
- The ruling's done-when list (sums, 538, NARA's split) does not require the READ digits to be held exactly.

The proposed fix is wrong for this repo. The tolerances 273/124/18 and 144/32/8 are DERIVED (FEC state row minus the READ figures). CLAUDE.md lines 39-41 allow a DERIVED figure only as GENERATED, REFERENCED or DELETED: "Nobody transcribes, anywhere, in any file."

Better mechanism: the certificates' own text layer (`/mingw64/bin/pdftotext -raw`, which the tool already runs for Nebraska at line 457) carries the same digits. In each "The DEMOCRATIC/REPUBLICAN PARTY Electors" block, the first four digit runs, with spaces and commas dropped, are district 1, district 2 and the at-large figure twice. 2012 has "142,93 7"; 2016 has "212, 774". A scratch probe found all ten READ values equal to the text layer (eight district figures plus the footnote counts 415 and 184). The text layer's at-large figures also equal the FEC state rows.

**The skeptic's corrected fix.** Do not write the per-party gaps or the 18/8 slack into president_returns.md. They are DERIVED figures, which the claim convention (CLAUDE.md lines 39-41) forbids transcribing. The record already puts the digits on the readers (line 3's PROVISIONAL "a second reading of the certificates"; reading 1's named readers).

If wording is wanted, add one qualitative clause to reading 1 with no figures: "the bound is not a check on the digits - an over-read within the party's own overseas votes, or the two districts swapped, still passes; the readers' agreement carries the digits."

If the tool should hold the digits (optional, stronger): in the Maine READ block, after `page($certrel)`, run `$pdftotext -raw` on each certificate once and strip `\f`. In each "The DEMOCRATIC|REPUBLICAN PARTY Electors" block, skip the "District1" and "At-Large" heading lines. Take the trailing digit run of the next four figure lines, with spaces and commas dropped, as d1, d2, AL, AL. Then:
- `problem()` unless d1 and d2 equal the TSV's figures;
- `problem()` unless the at-large figure equals `$e->{vD}` or `$e->{vR}` (this proves the right block was read);
- `problem()` unless "There were (\d+) votes cast" equals the unallocated row.

Any genuine OCR disagreement would be declared like the 'Trunp' exception. All ten values agree today, so it passes, and the +200 and swap mutations would then fail.

### 4. Maine READ tolerance comes from the TSV and is never checked: a duplicate 'unallocated' row silently overrides, and any saved page counts as the certificate

- **Lens:** checks - **reviewer:** defect - **skeptic:** minor
- **Where:** Tools/us_returns_prep.pl:510

**The scenario.** Line 510 does `$unalloc{$year} = count(...)` with no 'twice' problem; the D/R rows get one at line 512. Line 509 only requires the certificate column to name *some* page in a SHA256SUMS.txt. Line 527 then uses that figure as the only bound on the READ districts, although the header (lines 35-37) says the bound is 'the certificate's overseas votes'. Run on a scratch copy:
- **M3:** append `2016 ME - unallocated 9184 raw/returns/fec_2024presgeresults.xlsx 9` (an FEC workbook standing in as the certificate) and lower ME-2 2016 R to 172177. Exit 0; the run writes `2016,ME,2,144817,172177,READ`.
- **M2:** set the 2012 count to 50415 and lower ME-1 2012 D by 50,000. Exit 0; the run writes `2012,ME,1,173035,142937,READ`.
GeneratedCatalogCheck would also pass, because the CSVs, the catalog and the recorded TSV digest all come from the same run. Even with the true counts the pooled bound leaves slack: 2012 shortfalls D 273 + R 124 = 397 of 415 (18 votes); 2016 D 144 + R 32 = 176 of 184 (8 votes).

**The fix proposed.** - Raise a problem on a second unallocated row for a year.
- Require the certificate column to be `raw/district/archives_ascertainment_maine_$year.pdf`.
- Read the footnote count from that certificate's own text layer with `pdftotext -raw`, as the tool already does for Nebraska, and hold the TSV to it. Both layers print it legibly: 'There were 415 votes cast…' (2012) and 'There were 184 votes cast…' (2016).
- Optionally transcribe every slate's district and at-large figures to tighten the bound: my parse of the 2016 layer gives six slates whose shortfalls sum to 182 of 184.

**The skeptic's evidence.** Tools/us_returns_prep.pl:509 `page($certrel);` accepts any page that appears in a SHA256SUMS.txt; the 7th 'page' field is never read. :510 `if ($p eq 'unallocated') { $unalloc{$year} = count($v, $read_tsv); next; }` has no 'twice' check, while :512 has one for the D/R rows. :527 bounds `$gap > ($unalloc{$year} // 0)` with the TSV's figure only. The header (:35-37) and president_returns.md (line 23 and reading 1, "the certificates' footnote gives the count ... the tool holds them to it") say the bound is the certificate's.

Reproduced on a scratch copy; the unchanged inputs give byte-identical CSVs and .cs.
- M3: exit 0, writes `2016,ME,2,144817,172177,READ`.
- M2: exit 0, writes `2012,ME,1,173035,142937,READ`.
- Accidental variant: an appended row `2016 ME - unallocated 1840 raw/district/archives_ascertainment_maine_2016.pdf 2`, citing the TRUE certificate, overrides 184 silently; with ME-1 2016 D lowered from 212774 to 211774 the run exits 0.

GeneratedCatalogCheck.cs ~544 only compares the TSV digest with ReadTranscriptionDigest from the same run, and its allocator check sees only winners, so it would pass too.

`pdftotext -raw` of the certificates prints "There were 415 votes cast for President..." twice (2012) and "There were 184 votes cast..." three times (2016), with no other count, so the bound could be parsed. Gaps against the FEC rows: 2012 D 273 + R 124 = 397 of 415; 2016 D 144 + R 32 = 176 of 184. The 2016 text layer gives slates 144 + 3 + 3 + 32 + 0 + 0 = 182 of 184.

Why minor, not defect:
- The committed TSV is correct (415/184 match the certificates), so no output is wrong today.
- The path needs a wrong edit to the hand-typed READ file plus a district misreading at the same time.
- The pooled bound is already loose upwards with the true counts: ME-1 2012 D raised by 200 and ME-2 R lowered by 5 also exits 0.
- The only runtime reader, ElectoralCollege.FromCatalog:68 `district.VotesR > district.VotesD`, uses winners only, and the tool's NARA split check holds every district winner separately, so no winner or split can change this way. Nothing in play reaches the allocator yet.
- What stands: an unchecked duplicate, an unbound citation, and a header/record claim of a certificate check that the code does not make. The planned R-K9 second reading of these rows is the edit that would hit it.

**The skeptic's corrected fix.** In the READ loop:
- Before line 509, die unless `$cert eq "raw/district/archives_ascertainment_maine_$year.pdf"` and the page field is a positive integer.
- At line 510, for an 'unallocated' row require `$cd eq '-'` and add `problem("$read_tsv: ME $year unallocated twice") if exists $unalloc{$year};` before assigning.

After the loop, for each of 2012 and 2016, run `$pdftotext -raw` on that certificate (already passed through page()) the way the Nebraska block does. Collect every `/There were ([\d,]+) votes cast for President and Vice President by Uniformed Service or Overseas voters/`. Raise a problem unless there is at least one match, all matches agree, and they equal `$unalloc{$year}`. Today 2012 gives 415 twice and 2016 gives 184 three times.

Either make that change or reword the header (lines 35-37) and president_returns.md (line 23, reading 1) so they say the bound is the TSV's READ count.

Optional: transcribe every slate's district and at-large figures to shrink the pooled slack (182 of 184 in 2016). The per-party split of the overseas votes is not printed, so some slack upwards stays whatever is done.

### 5. District labels are checked nowhere: swapping ME-1/ME-2 in the READ TSV passes and writes a wrong catalog

- **Lens:** checks - **reviewer:** minor - **skeptic:** note
- **Where:** Tools/us_returns_prep.pl:512

**The scenario.** Every district-level check is unchanged when district labels are permuted:
- the sums over districts (lines 472, 488, 523);
- the key set '1 2' / '1 2 3' (lines 468, 519);
- the per-district tie and others checks (lines 540, 543);
- the split, which only counts winners per state (`$got{...}++`, line 561).

**M1:** swap the district digit of the four 2016 rows in maine_districts_read_2012_2016.tsv. Exit 0; president_by_district.csv gets `2016,ME,1,144817,181177,READ` and `2016,ME,2,212774,154384,READ`, so Trump carries ME-1 and Clinton ME-2. The certificate's text layer prints the reverse: D 212,774 / 144,817 and R 154,384 / 181,177 under 'First' / 'Second Congressional District'. GeneratedCatalogCheck and the backtest would also pass (3-1). In 2012, both ME districts and all NE districts in 2012/2016 went the same way, so even a winner-preserving swap is invisible.

Nebraska's labels rest only on which Total line follows each heading in the pdftotext output. They are correct today with xpdf 4.06 (for example, 2020 L565 'Congressional District 2' is followed by 'Total 154,377 176,468'), but the tool does not pin pdftotext and asserts no label.

**The fix proposed.** Bind READ figures to their district wherever the certificate's text layer is legible:
- 2016 prints all four D/R district figures under First/Second in order.
- 2012 prints 223,035 (First, D), 177,998 (Second, D) and 149,215 (Second, R).

Hold the TSV's (district, party) values to those. For Nebraska:
- print and require the pdftotext version (the record names xpdf 4.06);
- assert that the first three headings come in the order 1, 2, 3, each closed by its own Total before the next heading.

**The skeptic's evidence.** REPRODUCED, but it is a declared limitation of READ data, not a defect in the tool. The data as staged are correct.

1. The path exists. I ran the tool unmodified on a scratch copy: exit 0, and the output was byte-identical to the working tree. I then swapped the district digit of the four 2016 rows in the scratch TSV. Result: exit 0, and the district CSV gained `2016,ME,1,144817,181177,READ` and `2016,ME,2,212774,154384,READ`, with the matching catalog literals.
   - The checks the finding lists are all blind to labels. Lines 518-519 check the key set, line 523 sums both districts per party, line 527 checks the gap, and lines 540/543 check ties and others. Line 561 only does `$got{...}++`.
   - Downstream is blind as well. `FromCatalog` builds `districtWinners` and `AllocateJurisdiction` only does `result[districtWinner]++`, so it gives 3-1 either way. `CheckUsPresidentialReturns` compares the CSVs with the catalog and the TSV with `ReadTranscriptionDigest`, and the same run regenerates both sides.

2. The staged data are right. `pdftotext -raw` of the 2016 certificate prints 'First Congressional District' and then 'Second Congressional District' for each slate. Under them it gives D `212, 774` / `144,817` and R `154,384` / `181,177`, and the appointed electors include 'Diane Denk' (D, First) and 'Richard Bennett of Oxford' (R, Second). The 2012 text layer gives R `142,93 7` / `149,215` and D `223,035` / `177,998` under First/Second. All of this matches the TSV.

3. Nothing promises label binding.
   - The tool's header (lines 35-37) and the record's 'What the tool proves' hold READ rows only to 'never exceed the state ... fall short by no more than the overseas votes'.
   - The ruling's Done-when asks for NARA's split, which is a count per state.
   - The record marks READ rows PROVISIONAL until 'a second reading of the certificates' (R-K9).
   - The label swap is the same class of error as digit misreads inside the slack, which also pass. The slack is 2012: gaps 273+124=397 of 415; 2016: 144+32=176 of 184.

4. Nothing in the tree reads labels today; the only reader of `Districts` besides the drift check is `FromCatalog`. US-4 (USA_STAGE_PLAN.md) will swing ME/NE districts year to year, so labels will start to matter there.

5. The Nebraska half is not real. Line 463 takes the label from the book's own heading number, in the content-stream order of a hash-pinned PDF. All four books give headings 1, 2, 3, each followed by its own Total (2012 L419/422, L441/444, L447/450; 2016 L459/475 ...; 2020 L533/546, L565/578, L582/595; 2024 L581/602 ...). A dropped heading would let a House race's Total in, and the exact sum at line 472 would catch it.

**The skeptic's corrected fix.** Optional hardening; not needed for US-3. Do it before US-4 reads district labels.
- Maine READ rows: the tool already calls `page()` on each certificate, so it can also run `pdftotext -raw` on it. For each slate, after its 'First Congressional District' heading, require that the TSV's (1, p) and (2, p) figures are the first two numeric lines in order, with spaces dropped. This holds for all four figures in 2016 and in 2012 (2012 R reads '142,93 7'). A lighter alternative: hold each district's winner to the appointed electors in the certificate's first paragraph (2016: Denk = D First, Bennett = R Second; 2012: all four D).
- If OCR stays out of the tool by design: add one line to the tool header and to the record's 'What the tool proves' saying that the district attribution of the READ rows rests on the readings alone, because a permutation of district labels passes every check.
- Nebraska: no change. The tool could print `pdftotext -v` with its summary for provenance, but `-raw` order is fixed by the hash-pinned PDF, so pinning the extractor is not needed.

### 6. A missing READ row reaches the checks as undef, passes with warnings only, and writes a C# catalog that does not compile

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_returns_prep.pl:516

**The scenario.** Nothing checks that each (2012|2016) × (1,2) × (D,R) cell exists. Line 519 checks only the district keys, and `use warnings` (line 46) is not fatal, so lines 523, 540, 541 and 561 treat undef as 0.

**M5:** delete the `2012 ME 1 R` row and set the 2012 unallocated count to 200000. Exit 0, no MISMATCH line, six 'Use of uninitialized value' warnings on stderr. The run writes:
- president_by_district.csv: `2012,ME,1,223035,,READ`
- UsPresidentialReturns.cs: `(2012, "ME", 1, 223035L, L, true),`, which does not compile.

This breaks the header's 'It writes nothing and dies, naming every one, on any mismatch'. It is reachable only together with the widened tolerance in the first finding; with the true 415, the dropped row fails the shortfall check.

**The fix proposed.** After the TSV loop, raise a problem for every missing (year, district, party) cell. Additionally use `use warnings FATAL => 'uninitialized'` (or 'all'), so any undef reaching a check or a writer stops the run before line 598.

**The skeptic's evidence.** The finding holds. I reproduced it exactly in a scratchpad copy. The baseline run gave CSVs and .cs byte-identical to the staged ones. The repo was not touched.

**Code (Tools/us_returns_prep.pl):**
- The TSV loop (501-515) stores each cell one at a time. Three checks look at presence, and none of them covers each (year, district, party) cell:
  - line 512: a cell READ twice;
  - line 519: the district keys `'1 2'`. One row is enough to create a district's key, because line 512's `exists` and line 514 autovivify `$dist{$year}{ME}{$cd}`;
  - line 520: the unallocated row.
- `use warnings` (line 46) is not fatal, so lines 523, 540, 541 and 561 treat undef as 0.
- The writers print undef as an empty string: line 616 (`join(',', ... $d->{R} ...)`) and line 645 (`"$_->[4]L"`).

**Runs:**
- **M5a** (delete `2012 ME 1 R`, keep 415): exit 255. `MISMATCH: ME 2012: the nominees' districts fall 143334 short of the state, more than the certificate's 415 unallocated`. Nothing written.
- **M5b** (the finding's own: delete that row, unallocated set to 200000): exit 0.
  - Six "Use of uninitialized value" warnings, at lines 523, 540, 541, 561, 616 and 645.
  - Stdout still prints `332 - 206 by the record's winners = NARA's table EXACT ... wrote ... UsPresidentialReturns.cs`.
  - president_by_district.csv gets `2012,ME,1,223035,,READ`.
  - UsPresidentialReturns.cs line 265 gets `(2012, "ME", 1, 223035L, L, true),`.

**The finding's "reachable only together with the widened tolerance" is too narrow.** The defect does not depend on the first finding.
- **M5c** keeps the true 184. It deletes `2016 ME 1 R` and puts both districts' R together in `2016 ME 2 R`, as 335561.
- The gaps are R 32 + D 144 = 176, which is within 184.
- District 1 becomes D 212774 against undef, so D. District 2 is R. Those district results give NARA's 3-1.
- Result: exit 0, the same six warnings, `2016,ME,1,212774,,READ` in the CSV and `(2016, "ME", 1, 212774L, L, true),` in the .cs.

**Re-graded minor, not a defect:**
- Any missing cell always yields `, L,` in the .cs, which never compiles. Unity's compile (the cheap bar) stops it before runtime and before GeneratedCatalogCheck runs.
- GeneratedCatalogCheck would not catch it anyway (lines 544-551). It only re-hashes the TSV and compares the CSVs with the catalog. It never re-validates the READ cells.
- Reaching the path takes a compound hand-edit error in a 10-row TSV, and the warnings do reach stderr.

What breaks is the tool's own contract at header line 26: "It writes nothing and dies, naming every one, on any mismatch". Instead it exits 0, prints "EXACT", and leaves a non-compiling generated file and an empty CSV field on disk.

**The skeptic's corrected fix.** **The fix: an explicit check that every cell was READ.**
1. In the READ block, after the TSV loop and before line 516, add:
   `for my $year (2012, 2016) { for my $cd (1, 2) { for my $p ('D', 'R') { problem("$read_tsv: ME $year $cd $p not READ") unless defined $dist{$year}{ME}{$cd}{$p}; } } }`
2. Skip the gap arithmetic (lines 521-527) for a year that has a missing cell, for example:
   `next if grep { !defined $dist{$year}{ME}{$_->[0]}{$_->[1]} } ([1,'D'],[1,'R'],[2,'D'],[2,'R']);`

The die at line 598 then names the missing cell before any writer runs. The later loops (540/541/561) would still warn, but nothing is written.

**On the finding's backstop, `use warnings FATAL => 'uninitialized'`:**
- On this path it still writes nothing, because the first undef is at line 523, before any write.
- It loses "naming every one": it dies at the first undef with Perl's message, not the cell's, and before the MISMATCH list prints.
- In the writer stage it can leave partial output. The CSVs are written at lines 617-619 before the C# text is built (621-646), so a fatal warning at line 645 leaves new CSVs and a stale .cs.

**If a backstop is wanted,** use `$SIG{__WARN__} = sub { problem('perl warning: ', @_) }` for the checks section only, so a stray undef there becomes a named MISMATCH. Then build all four output texts before the first `write_lf`. Do not swallow warnings after line 598.

### 7. The state_ev_2024.csv check counts rows instead of requiring each jurisdiction once

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_returns_prep.pl:595

**The scenario.** Lines 583-595 check each row present and then only `$rows == 51`. Line 586 also skips the first non-comment line as the header without reading it.

**M4:** delete `AL;R;9;0` and duplicate `AK;R;3;0`. 51 rows, exit 0. The record still says 'every row of state_ev_2024.csv … agrees with the 2024 rows', but the table no longer holds Alabama. Low impact: grep finds no reader of this file in Assets/ or Tools/ other than this tool.

**The fix proposed.** - Keep a `%seen` hash and raise a problem on a repeated code.
- After the loop, raise a problem for each code in `@order` not seen.
- Require the header line to equal `state;winner(R/D);winner_EV;other_EV`.

**The skeptic's evidence.** The gap is real. I reproduced it on a copy of the inputs in my scratchpad and wrote nothing in the repo.

The check, Tools/us_returns_prep.pl lines 582-595:
- 586: `if (!$header) { $header = 1; next; }` (the header line is skipped and never compared)
- 587: `my ($st, $w, $wev, $oev) = split /;/, $line;`
- 588: `my $e = $y{2024}{st}{$st} or do { problem(...is no jurisdiction); next; };`
- 591: `problem(...) unless $w eq $win && $wev == $we && $oev == $oe;`
- 592: `$rows++;`
- 595: `problem("$old: $rows rows, not 51") unless $rows == 51;`

Nothing records which codes were seen, so a repeated code counts toward the 51 like any other row. The full list of 51 codes already exists as `@order` (line 240), but this check never uses it.

**Unmutated copy:** exit 0. All four outputs match the repo's SHA-256s (by_state 8e528467..., by_year a9f2a47b..., by_district d2a5038b..., .cs c76df2df...).

**M4** (`AL;R;9;0` deleted, `AK;R;3;0` duplicated; 51 rows, 0 AL, 2 AK):
- Exit 0, no MISMATCH line, all four outputs written.
- The mutated table's columns now sum to R 306, D 226. The file's own header still says "51 rows (50 states + DC)" and "column sums ... = Trump 312, Harris 226".

**Related inputs the check also lets through:**
- V1, truncated row `AL;R;9`: exit 0. Only a Perl warning ("uninitialized value $oev in numeric eq"); undef == 0 is true.
- V2, header deleted plus a duplicated AK: exit 0. AL is silently swallowed as the header.
- V3, `AL;R;9;0;D;99`: exit 0, silently; extra fields are ignored.
- V4, header deleted only: caught, but only by the count ("50 rows, not 51").

**Why the impact is low (minor, not a defect):**
- The CSV is never used to build the outputs. Under M4 all four outputs are byte-identical, so the generated catalog cannot go wrong through this gap.
- A repo-wide grep finds no reader of state_ev_2024.csv outside the tool, only docs. At 5fe3c031 the one code reference was a comment in SeatAllocationBacktest.cs:186 above the typed 2024 arrays, and those arrays are retired in this change.
- GeneratedCatalogCheck does not hash this file: it is read with a plain `open`, not `page()`, so it never enters `%read`/RawSources.
- The committed file is complete today: 51 unique codes, 0 duplicates.

**What fails:** the guard that the ruling relies on to "bring state_ev_2024.csv to the raw-page standard" (record line 60: "every row is held by the tool to the saved pages"). That guard passes a table that has lost a jurisdiction. The literal contract in the tool's header ("a row ... that the 2024 rows contradict") is still met, which is why this is minor rather than a broken promise.

**The skeptic's corrected fix.** Hold the whole table to the saved pages: each jurisdiction exactly once, each row's text exact, and the header exact. In Tools/us_returns_prep.pl, replace lines 582-595 with:

    my ($rows, $header, %seen) = (0, 0);
    while (my $line = <$h>) {
        $line =~ s/\r?\n$//;
        next if $line eq '' || $line =~ /^#/;
        if (!$header) { $header = 1; problem("$old: header '$line', not 'state;winner(R/D);winner_EV;other_EV'") unless $line eq 'state;winner(R/D);winner_EV;other_EV'; next; }
        my ($st) = split /;/, $line;
        my $e = $y{2024}{st}{$st} or do { problem("$old: '$st' is no jurisdiction"); next; };
        problem("$old $st: a second row") if $seen{$st}++;
        my $win = $e->{evR} >= $e->{evD} ? 'R' : 'D';
        my ($we, $oe) = $win eq 'R' ? ($e->{evR}, $e->{evD}) : ($e->{evD}, $e->{evR});
        problem("$old $st: '$line', the saved pages give '$st;$win;$we;$oe'") unless $line eq "$st;$win;$we;$oe";
        $rows++;
    }
    close $h;
    problem("$old: no row for $_") for grep { !$seen{$_} } @order;
    problem("$old: $rows rows, not 51") unless $rows == 51;

**How each change maps to the runs above:**
- The `%seen` test and the loop over `@order` catch M4. M4 would print "AK: a second row" and "no row for AL".
- The exact `eq` comparison of the whole line catches V1 (truncated) and V3 (extra fields), which pass today with no MISMATCH line. A text comparison is safe on the current file: evR/evD stringify as plain integers, the same values president_by_state.csv prints (2024,AL,...,0,9,0).
- The header test catches V2 and names V4 directly, instead of relying on the indirect "50 rows" count.

**Afterwards:**
- Re-run on the unmutated copy: it should still pass and write the same bytes.
- Add M4 to the record's mutation list, as a tenth case in president_returns.md "What the tool proves".

### 8. The plurality bound is sound for plurality, but Maine 2020/2024 are ranked-choice contests; the RCV threshold is not checked though the tool already reads the needed column

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_returns_prep.pl:543

**The scenario.** The district bound holds for plurality: one minor candidate's district vote ≤ that candidate's statewide vote ≤ the state's pooled others. That pool is the FEC 'All Others' column, or in 2024 every column but the two nominees, each held to its row total.

Maine's statute, saved under raw/district/ and acknowledged in district_method_2024.md, makes electors an RCV contest: §1(27-C)(D) covers 'General elections for presidential electors', and §723-A(2) gives an outright win only above 50% of all ballots cast, blanks and overvotes included. A first round of 48/46/6 passes lines 535-543, yet would go to further rounds.

Today every Maine 2020/2024 contest clears the outright bar, measured on the TBC column (column 11) of the CG sheets the tool already opens:

| Contest | Leader / ballots | Share |
|---|---|---|
| ME-2 2020 | 196,692 / 380,324 | 51.7% |
| ME-1 2020 | 266,376 / 447,981 | 59.5% |
| ME-1 2024 | 258,863 / 439,574 | 58.9% |
| ME-2 2024 | 212,763 / 402,873 | 52.8% |
| Statewide 2020 | 435,072 / 828,305 | 52.5% |
| Statewide 2024 | 435,652 / 842,447 | 51.7% |

NARA's per-state split backs the outcome. Even so, reading 8, the header (lines 38-39) and the FromCatalog doc state 'the plurality' as the reason for every jurisdiction.

**The fix proposed.** For ME from 2020, require the leader > TBC/2 in each CG sheet and statewide (the sum of the two TBC figures). Alternatively, declare in reading 8 and the header that Maine 2020/2024 rest on the first-round majority.

**The skeptic's evidence.** The facts hold, but the scenario has no failing output path. What remains is a gap in what the docs declare.

CONFIRMED:
(1) Tools/us_returns_prep.pl 535-543 is a plurality-only bound: `unless $e->{other} < $big` and `unless $e->{other} < $db`. The Maine loop (474-485) loads each whole CG sheet with xlsx_sheet, but reads only the Democratic and Republican columns found in row 3. Row 1, column 11, headed 'TBC', is in memory and unused.
(2) The saved raw/district/maine_legislature_21a_1.html has 27-C(D): "General elections for presidential electors". The saved maine_legislature_21a_723-A.html, subsection 2: "more than 50% of all ballots cast ... including ... ballots on which ranking number one is blank, on which there is an overvote" ... "that candidate is declared the winner". This is the current text; the subsection was amended by PL 2023 c.628 and PL 2025 c.363.
(3) I read the CG Total rows with the tool's own reader, and every figure in the finding's table is exact:
- 2020 CG1: 266376 of TBC 447981
- 2020 CG2: 196692 of 380324
- 2024 CG1: 258863 of 439574
- 2024 CG2: 212763 of 402873
- statewide 2020: 435072 of 828305
- statewide 2024: 435652 of 842447
All six are above one half.

NO OUTPUT FAILURE:
- Every input is held to its SHA256SUMS.txt line (page(), line 74), so the 48/46/6 case cannot arise. A leader with more than half of all ballots wins under every text of 723-A, because the leader's count never falls and the continuing ballots never exceed the total.
- Any ranked-choice count that changed one contest's winner would be refused at line 566, which compares each state's D and R split with NARA's table, then dies. The only gap is an opposite flip of ME-1 and ME-2 at the same time, which nets to the same 3-1. The data rules that out. GeneratedCatalogCheck (Allocate over FromCatalog against CastR+OthersRSlate) and SeatAllocationBacktest compare national totals only, so line 566 is the real guard.

DOCS: true as written, incomplete where this change moved the scope.
- Reading 8, header 38-39 and the FromCatalog summary only say that comparing the two nominees is the plurality.
- ElectoralCollege.cs 32-36 (not changed by this diff) already declares "any ranked-choice tabulation" not modelled, but justifies it with "decided in round one in 2024" alone.
- This diff makes FromCatalog hand the allocator Maine's 2020 plurality winners as well, and no file records that 2020 was decided in round one. district_method_2024.md 81-91 covers 2024 only and warns "must not generalise that to future cycles". The saved 2020 SoS results page lists no RCV tabulation for the November general, which agrees.
- president_returns.md reading 3 declares faithless electors not modelled ("ElectoralCollege's own scope"), but no reading names Maine's RCV count. Its register lists both RCV statute pages (rows 117-118). USA_STAGE_PLAN risk 23 and §8 ask for "Maine's ranked-choice count for electors" to be "stated where it would show".

RELATED: returns_2024.md line 59 says Alaska's RCV general also applies to President (marked [UNCONFIRMED-direct-fetch]; the page is BILLED). Its 2024 row is framed the same way; the leader holds 184458 of 338177 FEC votes.

**The skeptic's corrected fix.** Prefer the check, because it keeps the claim convention (no derived shares in prose).

1. In the Maine 2020/2024 loop (lines 477-485), find the 'TBC' column by its row-1 heading, the same way the Democratic and Republican columns are found in row 3. Store the TBC of each CG Total row.
2. After the plurality block, for ME 2020 and 2024, call problem() unless:
   - 2 x the larger nominee > TBC in each district;
   - 2 x the statewide larger nominee > CG1 TBC + CG2 TBC.
   Message: "an RCV contest not decided in the first round - its rounds are not modelled". Add this to the header's failure list. The condition is sufficient under every text of 21-A §723-A.
3. Reword reading 8, or add a reading after it, in words only: "Maine's 2020 and 2024 contests are ranked-choice by statute (21-A §1(27-C)(D), §723-A(2)). The tool requires each leader to hold more than half of all ballots cast (the CG sheets' TBC), so the plurality winner is the statute's winner. Rounds are not modelled (ElectoralCollege's scope)."
4. Extend ElectoralCollege.cs's parenthetical from "decided in round one in 2024" to "in 2020 and 2024 (Tools/us_returns_prep.pl holds it)".
5. Optionally, declare Alaska 2024 the same way. It cannot be checked: no saved page holds Alaska's rule or its ballots-cast figure (BILLED).

### 9. Writes are not all-or-nothing, and close() is unchecked

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_returns_prep.pl:601

**The scenario.** Every problem() call (the last at line 595) precedes the gate at line 598. But write_lf tests ASCII on each file just before writing it, and writes the three CSVs before the C# file.
- A non-ASCII name in NARA's notes, which the tool decodes as UTF-8 at line 284, appears in president_by_state.csv but not president_by_year.csv. The tool would die after president_by_year.csv had already been rewritten.
- `print`/`close $w` are unchecked, so a failed write still returns the digest of the intended text, and that digest goes into the catalog.
- No field is tested for ',' (the CSV is split on commas) or for '"' (the C# string literals).

**The fix proposed.** Build all four texts first, then validate all of them: ASCII, no ',' in any CSV field, no '"' or '\\' in C# strings. Only then write, with `print $w $text or die` and `close $w or die`.

**The skeptic's evidence.** Tools/us_returns_prep.pl:601 `sub write_lf { ... die "$f: not ASCII\n" if $text =~ /[^\x00-\x7F]/; open my $w, '>:raw', $f or die "$f: $!\n"; print $w $text; close $w; return sha256_hex($text); }`. The ASCII test runs per file, at write time, and print and close are unchecked. The gate at 598 runs before the three CSV writes (617-619) and the .cs write (647). NARA's page is decoded at 284. A note's recipient goes only into cast_other_to (446), which is written to the state CSV (611) and the .cs (640), not the year CSV (606).

Reproduced in a scratch copy. A baseline run first passed, and all four outputs were byte-identical to the staged ones.
(1) The finding's own scenario: "Faith Spotted Eagle" became "Faith Sp\xC3\xB6tted Eagle" in both NARA's 2016 page and the FEC 2016 sharedStrings, so the comparison at 432 still agrees, and both pages were re-pinned in SHA256SUMS.txt. Every check before 598 passed. The run then died "ElectionsData/usa/president_by_state.csv: not ASCII" (exit 255) after president_by_year.csv had been rewritten; the state and district CSVs and the .cs were untouched. In this exact case the rewritten year CSV equalled the repo's bytes, so no harm resulted. The set is left mixed only when the same input change also alters a year row.
(2) A later output that cannot be opened (president_by_state.csv made a directory; on Windows, Excel holding the CSV open does the same) died "Is a directory" (exit 21) after president_by_year.csv had been rewritten.
(3) Line 601's write_lf, run verbatim against /dev/full, returned a digest and did not die. This perl reports print true and close false ("No space left on device").

Why it is only a note:
- Every page is SHA-pinned (page(), 65-77) and the committed run is byte-stable, so no committed input reaches these paths.
- Every listed mismatch is a problem() or die before 598, so the header's "writes nothing ... on any mismatch" holds. The ASCII guard is not in that list.
- Every after-effect is caught at the bar. GeneratedCatalogCheck.ReadUsCsv (608-610) re-hashes each CSV against YearSourceDigest, StateSourceDigest and DistrictSourceDigest; line 618 counts each row's fields; SameRow compares every row. A truncated .cs fails to compile.
- Leaving close unchecked is the repo's convention: presidential_returns_prep.pl:63, sejm_districts_prep.pl:76, pension_prep.pl:23, income_prep.pl:25, govcons_prep.pl:67, prestart_record_prep.pl:75/103 and energy_prep.pl all do it.

The finding's third point, no ',' or '"' test, is mostly refuted:
- Line 333 splits the notes on commas, so cast_other_to cannot hold one.
- A comma in a nominee's name makes surname() return a token such as "Biden,". That fails the FEC heading check (359, a problem() before 598) or the 2024 column lookup (398-399, a die), in both cases before any file is written.
- ReadUsCsv counts the fields of every row. The committed CSVs have 8 fields x5 lines, 11 x205 and 6 x21, heading included.
- A '"' inside a C# string literal is a compile error, so it cannot pass silently. A '\' either fails to compile (CS1009) or makes SameRow report a difference.

**The skeptic's corrected fix.** Low priority. From line 600 on:
1. Build $csv_y, $csv_s and $csv_d, take their sha256_hex digests, then build $cs. It needs only the digests, not the written files.
2. Test all four texts before opening any file: no non-ASCII character, and say which file and line holds the first one. Optionally, also reject a '"' or '\' in any name bound for a C# literal (NARA's nominee names and cast_other_to).
3. Write each file with `print $w $text or die "$f: $!\n"; close $w or die "$f: $!\n";`.

This removes the only way the data can cause a partial write. A later file that cannot be opened can still leave one. That failure is loud, and GeneratedCatalogCheck's digest re-hash catches it. If the four outputs must be fully atomic, write .tmp siblings and rename them only after all four have closed cleanly. A ',' test adds nothing, because line 333 and the surname checks at 359 and 398 already cover it. The unchecked close is the convention in every Tools/*.pl generator, so fix it there as a sweep rather than in this tool alone.

### 10. New comments copy facts about the catalog's contents into prose (claim convention; borderline)

- **Lens:** csharp - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/SeatAllocationBacktest.cs:32

**The scenario.** The new comments state facts about what the generated catalog holds: ElectoralCollege.cs:13 'the elections of record 2012 to 2024'; SeatAllocationBacktest.cs:32-33 '2012 TO 2024 ... the four elections of record'; :182 'ON THE FOUR ELECTIONS OF RECORD'; :38 '(2024's do)'; :190 '(2016's, named in the catalog and not modelled)'. Line 38 is also incomplete: 2020's moves cancel too (winner-take-all 232/306 equals the real result). When the tool gains a year, these comments become wrong, not merely incomplete, which is the case the convention's test rules out. This is borderline if the four years count as the ruled scope, i.e. an instruction rather than a derived fact.

**The fix proposed.** Point to where the fact lives instead, e.g. 'the years UsPresidentialReturns.Years holds', and let the per-year counterfactual line the backtest prints say which cycles cancel.

**The skeptic's evidence.** Real only in a narrow sense, and still a note. Every statement the finding cites is true today. The catalog's years are 2012, 2016, 2020 and 2024 (UsPresidentialReturns.cs:43-49). 2016's votes for other persons are named in CastOtherTo: HI "Bernie Sanders 1" at :117, TX "Ron Paul 1; John Kasich 1" at :149, WA "Colin Powell 3; Faith Spotted Eagle 1" at :153.

REFUTED parts:
(a) SeatAllocationBacktest.cs:38 "(2024's do)" cannot become wrong when the tool gains a year. It is a fact about the 2024 election, not about the code: in president_by_district.csv, 2024 ME-2 went R (212763 > 176789) against Maine's D statewide, and NE-2 went D (163541 > 148905) against Nebraska's R statewide. The convention's own census (COMPLETED.md s190 sA.3) calls DERIVED claims "about the world, not the code" "already correct practice". Rule 2 bans only counts of code things, line numbers, measured figures, build status and environment facts.
(b) The claim that ":38 is incomplete" is a misreading. 2020 does cancel too (ME-2 R 196692 > 168696; NE-2 D 176468 > 154377), but the parenthetical follows "can cancel" and is an example, not a list. The test also permits incomplete. The second half of the proposed fix already exists: lines 209-212 print for every year "moved nothing", "the moves CANCEL" or "moves by N".
(c) :190 "(2016's, named in the catalog and not modelled)" restates US-3's done-when (USA_STAGE_PLAN.md:192, "the electors who broke their pledge named and not modelled"), which is INSTRUCTION, plus a world fact. Adding a year could make it incomplete, not wrong.

WHAT SURVIVES: three phrases give the year set or count of what code iterates. They are :32-33 "2012 TO 2024 ... the four elections of record", :182 "ON THE FOUR ELECTIONS OF RECORD", and ElectoralCollege.cs:13 "which hands it the elections of record 2012 to 2024". The loop at :191 runs over whatever UsPresidentialReturns.Years holds, and FromCatalog filters States by year. That is the shape of the convention's own example, "53 parties", and CLAUDE.md says "This binds code comments too". The failing path: Tools/us_returns_prep.pl:52 `my @years = (2012, 2016, 2020, 2024);` gains a year, the loop runs five, and these lines are wrong. Nothing checks "four"; CommentClaimCheck only resolves backticked Type.Member references.

WHY ONLY A NOTE:
- The four years ARE the ruled scope (USA_STAGE_PLAN.md:151 "the Electoral College exact on four elections"; :192 names all four), so the finding's own borderline condition holds.
- @years is a hard-coded literal that changes only with new raw pages and a ruling. No plan item extends it: US-26 rolls the game's own history forward, not the record.
- No behaviour or bar is affected.
- The repo already accepts the same shape: NationalElection.cs:331 "GermanRegions: 2021's count before the 2025 chamber sat, 2025's after", and GeneratedCatalogCheck.cs:483 "the two Länder catalogs" (f6afe7f5, s688).
- The change's own GeneratedCatalogCheck.cs:521 already uses the pointer form: "FromCatalog over each year".

Side note outside the C# lens: us_returns_prep.pl:625 hard-codes "2012, 2016, 2020 and 2024" into the generated summary instead of building it from @years.

**The skeptic's corrected fix.** Comment-only; change just the three phrases that describe what code iterates, and keep :38 "(2024's do)" and :190 as they are, since both are world facts or restate the ruling.
- SeatAllocationBacktest.cs:32-33 becomes "USA ELECTORAL COLLEGE, THE ELECTIONS OF RECORD (2024 added Day-1, R-EL8; since PS-6 US-3, `COMPLETED.md` §786, every year <see cref="UsPresidentialReturns.Years"/> holds): ...".
- :182 becomes "... BY THE REAL RULE, ON EACH ELECTION OF RECORD THE CATALOG HOLDS (R-EL8, ...".
- ElectoralCollege.cs:13 becomes "... both through <see cref="FromCatalog"/>, which hands it any year <see cref="UsPresidentialReturns.Years"/> holds."
Both files already have `using PoliSim.Elections.Generated;`, so the cref resolves, and the pointer is checked by the compiler where "four" is checked by nothing. Optional: build the generated summary string at us_returns_prep.pl:625 from @years. The per-year counterfactual print (lines 209-212) already says which cycles cancel, so it needs no change.

### 11. Reading 11 lists only the 2023 and 2025 amendments to Maine §801/§803/§805, but the saved pages also show amendments in 2019 and 2021, between the four elections

- **Lens:** record - **reviewer:** defect - **skeptic:** minor
- **Where:** ElectionsData/usa/president_returns.md:42

**The scenario.** The saved Revisor pages' SECTION HISTORY lines read as follows. §801: PL 1985 c.161 (NEW), PL 2001 c.516, PL 2019 c.539, PL 2023 c.628, PL 2025 c.397 (all AMD). §803: PL 1989 c.166, PL 2021 c.273, PL 2023 c.628 (RPR), PL 2025 c.397. §805: PL 1989 c.166, PL 2019 c.539, PL 2023 c.628, PL 2025 c.397. Reading 11 ('The statutes' dates') says the three sections 'carry amendments of 2023 and 2025, after some or all of the four elections'. By that sentence's own test, PL 2019 c.539 (§801, §805; between the 2016 and 2020 elections) and PL 2021 c.273 (§803; between 2020 and 2024) also qualify, and both are left out. A re-verifier (R-K9) or US-4 using this reading to decide which saved wording governed 2020 and 2024 would treat §801/§805 as unchanged from 1985/1989 until 2023.

**The fix proposed.** Copy each section's full SECTION HISTORY from its saved page, as the record already does for §802. Say which amendment falls between which elections: 2019 for §801/§805, 2021 for §803 (whose 2023 change is RPR, a replacement), then 2023 and 2025.

**The skeptic's evidence.** THE CLAIM (ElectionsData/usa/president_returns.md:42, reading 11): "§802, the district method, has stood unamended since PL 1985, c. 161; §801, §803 and §805 carry amendments of 2023 and 2025, after some or all of the four elections. ... §32-710's sources run to Laws 2015, LB575."

THE SAVED PAGES' SECTION HISTORY lines (raw/district/):
- maine_legislature_21a_801.html:142: "PL 1985, c. 161, §6 (NEW). PL 2001, c. 516, §17 (AMD). PL 2019, c. 539, §4 (AMD). PL 2023, c. 628, §5 (AMD). PL 2025, c. 397, §49 (AMD)."
- maine_legislature_21a_803.html:160: "PL 1985, c. 161, §6 (NEW). PL 1989, c. 166, §6 (AMD). PL 2021, c. 273, §29 (AMD). PL 2023, c. 628, §6 (RPR). PL 2025, c. 397, §50 (AMD)."
- maine_legislature_21a_805.html:185: "PL 1985, c. 161, §6 (NEW). PL 1989, c. 166, §§7-9 (AMD). PL 2019, c. 539, §5 (AMD). PL 2023, c. 628, §§7, 8 (AMD). PL 2025, c. 397, §52 (AMD)."
The reading's test is "after some or all of the four elections". PL 2019, c. 539 (§801, §805) comes after 2012 and 2016, and PL 2021, c. 273 (§803) comes after 2012, 2016 and 2020, so both pass the test. Both are left out. The same reading applies the stricter standard to Nebraska: it names §32-710's one post-2012 source, LB575 of 2015 (wayback_nebraska_legislature_32_710_20260217.html source line: "Laws 1994, LB 76, § 210; Laws 1997, LB 764, § 70; Laws 2011, LB368, § 2; Laws 2015, LB575, § 18").

WHY THE OMISSION MATTERS:
- The ranked-choice wording is §801(2) ("Counting of ballots for candidates for President must proceed according to the ranked-choice method ... section 723-A") and §805(2) ("...the largest number of votes in the State according to the ranked-choice method..."). On the page these subsections carry only their latest tags, "[PL 2023, c. 628, §5 (AMD).]" and "[PL 2023, c. 628, §7 (AMD).]". So the reading plus those tags would date the ranked-choice wording to 2023, which would mean 2024 only.
- The same 2019 chapter also amended §1 and §723-A. The §1 history reads "PL 2019, c. 539, §§1, 2 (AMD). PL 2019, c. 539, §6 (AFF)"; the §723-A history reads "PL 2019, c. 539, §3 (AMD). PL 2019, c. 539, §6 (AFF). PL 2021, c. 273, §11 (AMD)". PL 2021, c. 273 also repealed and replaced §1(27-C), the definition that lists "General elections for presidential electors": "[PL 2021, c. 273, §1 (RPR).]".
- So both omitted chapters run through Maine's ranked-choice sections. That much is on disk. From memory only (web search budget spent, not re-checked): c. 539 is LD 1083, the act that brought presidential elections under ranked-choice voting, first used in November 2020.
- No record outside raw/ names c. 539 or c. 273, or says Maine's 2020 count was ranked-choice. The ranked-choice section of district_method_2024.md is titled "(the trap that isn't one, in 2024)" and covers 2024 only.

WHY MINOR, NOT DEFECT:
- The sentence is literally true. It is an incomplete list, not a false one.
- Nothing computed depends on it. Tools/us_returns_prep.pl and GeneratedCatalogCheck never read these pages: grepping for 'maine_legislature' or '21a_80' finds nothing in the tool or the check, and UsPresidentialReturns.cs lists none of these pages (0 matches).
- Maine's 2020 and 2024 winners are held to NARA's table anyway.
- USA_STAGE_PLAN declares "Maine's ranked-choice count for electors" not modelled in every year (US-4, line 739).
- The full histories sit on the pages the reading cites, and R-K9 re-reads those pages.
- What remains is an accuracy gap in the record's only summary of when these sections changed.

**The skeptic's corrected fix.** Rewrite reading 11's clause on §801/§803/§805 so it gives each section's whole SECTION HISTORY from its saved page, as the record already does for §802. A plain list is enough:
- §801: PL 1985, c. 161; PL 2001, c. 516; PL 2019, c. 539; PL 2023, c. 628; PL 2025, c. 397.
- §803: PL 1985, c. 161; PL 1989, c. 166; PL 2021, c. 273; PL 2023, c. 628 (repealed and replaced); PL 2025, c. 397.
- §805: PL 1985, c. 161; PL 1989, c. 166; PL 2019, c. 539; PL 2023, c. 628; PL 2025, c. 397.

Then say what follows:
- PL 2019 (§801, §805) falls after 2012 and 2016; PL 2021 (§803) after 2012, 2016 and 2020; PL 2023 and PL 2025 as now. The wording in force at any of the four elections is therefore not on disk for the amended parts.
- A subsection's tag names only its latest act. §801(2) and §805(2), the ranked-choice count, are tagged PL 2023, c. 628, yet PL 2019, c. 539 amended both sections earlier, and §1 and §723-A as well. The start of the ranked-choice count must not be dated from those tags.

Two cautions for the rewrite:
- "PL yyyy" is the Legislature's citation year. A chapter can be enacted, or take effect, the following year, and the saved pages print no effective dates. Place each chapter against an election by citation year, and say so, unless the chaptered laws are sourced (otherwise BILLED). This matters most for whether PL 2023, c. 628 was in force on 5 November 2024; the present "after some or all" wording correctly avoids that question.
- Optional, beyond this reading: a line where the record discusses the plurality (reading 8), or in district_method_2024.md, saying that Maine's 2020 presidential count was also ranked-choice under statute (c. 539), with the first-round leaders winning, as the match with NARA's table shows.

### 12. The s786 LB3 bullet says the hearing's testimony rests on unfetched press, but the saved Unicameral Update hearing page carries it

- **Lens:** record - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/district_method_2024.md:144

**The scenario.** raw/district/nebraska_unicameral_update_37216.html is saved, registered, and labelled '(the hearing)' at line 111. It reads: 'Two measures ... were heard by the Government, Military and Veterans Affairs Committee Jan. 30' and 'In a hearing lasting more than five hours, over 75 people testified — the majority of them in opposition to one or both measures'. That is exactly lines 102-103's 'Hearing 30 January 2025 (75+ testifiers, majority opposed)'. Line 144 nonetheless puts 'the hearing's testimony' among the facts that 'rest on the press named above, not fetched'. A re-verifier would treat a fact from a saved official page as press-only, and might BILL or drop it.

**The fix proposed.** Remove 'the hearing's testimony' from that list and cite 37216 for the hearing date and testimony count. Keep the cloture date, LR24CA's 2026 ballot status and the initiative as press-based. Note: BillTrack50 shows only a 'Lippincott MO158' dated 04/08/2025, so the cloture date is not stated on a saved page.

**The skeptic's evidence.** I could not refute the finding. The staged text (index equals working tree) contradicts the saved page that the same record cites.

1. ElectionsData/usa/district_method_2024.md:144, a line this change adds: "The cloture's date (8 April 2025), the hearing's testimony, the ballot status of LR24CA and the 2025 initiative rest on the press named above, not fetched."

2. The hearing facts the record states are at lines 102-103: "Hearing 30 January 2025 (75+ testifiers, majority opposed)". Line 111 already cites the saved page for them: "https://update.legislature.ne.gov/?p=37216 (the hearing)". The record states nothing else about the hearing, so "the hearing's testimony" can only mean those facts.

3. The saved page carries both facts. raw/district/nebraska_unicameral_update_37216.html has sha256 22c009f3...e6d904. That matches raw/district/SHA256SUMS.txt:28 and register row president_returns.md:141, and fetch_log.txt:30 shows "2026-10-05T19:45:51Z 200". Its published_time is 2025-01-31, and its own bytes read:
   - "heard by the Government, Military and Veterans Affairs Committee Jan. 30"
   - "In a hearing lasting more than five hours, over 75 people testified — the majority of them in opposition to one or both measures."
   So line 144 calls a fact "not fetched" and press-based when it is on a fetched, registered, official page. This contradicts line 111 of the same file and CLAUDE.md:112 ("A source is fetched, not remembered").

4. The two records also disagree. The register's BILLED list (president_returns.md:55) names only "WOWT, the Nebraska Examiner; the 2025 initiative and its withdrawal" as not fetched. It does not include the hearing.

5. The finding's other items check out, so they stay on the list:
   - **Cloture date:** 39341 (published 2025-07-11) says "The motion failed on a vote of 31-18. Thirty-three votes were needed." with no date. BillTrack50 shows only "Lippincott MO158" next to 04/08/2025 and never calls it cloture.
   - **LR24CA:** 39341 says only that it "was considered by the ... Committee but was not advanced" (as of July 2025). Its 2026 ballot status is not on any saved page.
   - **The initiative:** it is on no saved page.

Severity is minor, not a defect. The error under-claims: it calls a fact weaker-sourced than it is, invents nothing, and the fact stays in the record at lines 102-103. No code, CSV or generated catalog reads it, and LB3's NOT ENACTED verdict rests on 39341 and BillTrack50. It is still a real error in the section whose whole job, under the US-3 ruling, is to say which facts the saved pages carry.

**The skeptic's corrected fix.** Change line 144 of ElectionsData/usa/district_method_2024.md:
- Remove "the hearing's testimony" from the list of facts that "rest on the press named above, not fetched".
- Add that the hearing's date and testimony are on the saved 37216 page, using the page's own words: "heard by the Government, Military and Veterans Affairs Committee Jan. 30" and "over 75 people testified — the majority of them in opposition to one or both measures".
- Keep the cloture's date (8 April 2025), LR24CA's 2026 ballot status and the 2025 initiative on the press list. No saved page states the cloture's date: 39341 gives 31-18 without a date, and BillTrack50 shows only "Lippincott MO158" next to 04/08/2025 without calling it cloture.

Optional:
- Lines 102-103 say "majority opposed". The saved page says the majority opposed "one or both measures", not LB3 alone. Change them to "75+ testifiers, the majority opposed to one or both measures", as the change itself did for the two 39341 quotations.

No raw page, CSV, tool or code changes.

### 13. 'The pages this file cites are saved', but two cited pages were neither saved nor named as unfetched

- **Lens:** record - **reviewer:** minor - **skeptic:** minor
- **Where:** ElectionsData/usa/district_method_2024.md:139

**The scenario.** Line 108 rests a claim on 'the Nebraska Secretary of State's 2026 elections page' ('carries no notice of any change'). Line 123 rests one on FindLaw ('current as of January 01, 2024'). Neither appears in any fetch log; the saved nebraska_sos_previous_elections.html only links to '2026 Elections'. Yet line 139 says the pages this file cites are saved byte for byte under raw/. Line 144 names only the press as unfetched, and president_returns.md:55 and :61 likewise list only WOWT, the Nebraska Examiner and the initiative. A reader would take the 'no notice of any change' claim as resting on a saved page.

**The fix proposed.** In the s786 bullet and in president_returns.md's Not-reached list, name both pages as not fetched, or fetch and register them, or BILL them.

**The skeptic's evidence.** The finding holds. I could not refute it.

1. Two pages are cited but were never saved.
- ElectionsData/usa/district_method_2024.md:107-108 says "The Nebraska Secretary of State's 2026 elections page carries no notice of any change."
- :122-123 says "§32-1038's text was re-read from a commercial reproduction (FindLaw, "current as of January 01, 2024")".

2. Line 139 claims every cited page is saved. It reads: "Since §786 the pages this file cites are saved byte for byte under `raw/` and registered … in `president_returns.md`". The only bullet that admits a gap is :144, which names "the press named above, not fetched". That covers the cloture date, the hearing's testimony, LR24CA's ballot status and the 2025 initiative. Neither page from point 1 is named in any s786 bullet. A grep for "findlaw" outside raw/ finds only :123, and a grep for the 2026 elections page finds only :108.

3. Neither page appears in any fetch log.
- The only sos.nebraska.gov entries in raw/district/fetch_log.txt are https://sos.nebraska.gov/previous-elections and the four canvass books.
- No in-tree fetch log or out_of_tree.txt mentions findlaw or sos.nebraska.gov/elections.
- The out-of-tree sweep of pages cited by the 2024 records (PoliSim-captures/sources/usa_us3/cited2024/fetch_log.txt) never tried either one. Its only failures are Alaska (405) and nebraskalegislature.gov (000).

4. The saved Nebraska page is a different page. nebraska_sos_previous_elections.html is registered in president_returns.md:140 as "previous elections (page) … provenance". At line 669 it only links to the 2026 page: `<a href="/elections" class="sf-depth-2">2026 Elections</a>`. It carries no notice about LB3, winner-take-all or the electors. A reader who follows line 139 to the register would find this page and could mistake it for the source of the "no notice" claim.

5. president_returns.md misses them too. Its "Not reached — BILLED" list (:51-55) names nebraskalegislature.gov, Alaska, NARA's 2010-census capture, 2 U.S.C. 2b and the press items (:55: WOWT, the Nebraska Examiner, the initiative). Line :61 covers district_method_2024.md only through the statute quotations, the district table and the two Unicameral Update corrections. Neither page is named anywhere.

6. The 2026 page was not on a refused host. sos.nebraska.gov returned 200 five times on 2026-10-05 between 16:21:12 and 16:21:17, so the page could have been fetched. CLAUDE.md:112 says "A source is fetched, not remembered", so BILLED does not fit; the page was simply never fetched.

Why minor and not a defect: nothing the tool reads depends on either page. The LB3 verdict rests on saved pages: BillTrack50's "Indefinitely postponed (on 04/17/2026)" and the Unicameral Update. The §32-1038 wording is now on the saved Internet Archive capture (bullet :141), which takes FindLaw's place. The fault is a provenance claim stated too broadly, in a record whose owner ruled it be brought "to the raw-page standard". No figure or code path is wrong.

**The skeptic's corrected fix.** Two changes:

1. Fetch the Nebraska Secretary of State's 2026 elections page. The host answered on 2026-10-05, so the "no notice of any change" claim can be re-checked against a saved page. Save it as raw/district/nebraska_sos_elections_2026.html, add a fetch_log.txt line, a SHA256SUMS.txt line and a register row, and list it in the catalog's RawSources (GeneratedCatalogCheck re-hashes every page there). If the page now says something different, correct :107-108.

2. Name FindLaw as not fetched, because the Internet Archive capture now replaces it. Add a line to district_method_2024.md's s786 LB3 or statutes bullet, for example: "FindLaw's reproduction of §32-1038 (:123) was not fetched; the statute's words now rest on the saved Internet Archive capture." Mirror it in president_returns.md: add a "not fetched" bullet beside :55 and extend :61.

If the 2026 page is not fetched, name it as not fetched in both places too. Alternatively, narrow line 139 to "the pages this file cites are saved … except those named below".

### 14. Claim convention: comments and the spec transcribe the catalog's year range and counts

- **Lens:** record - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/ElectoralCollege.cs:13

**The scenario.** These lines state what UsPresidentialReturns.Years holds: 'which hands it the elections of record 2012 to 2024' (ElectoralCollege.cs:13); 'USA ELECTORAL COLLEGE, 2012 TO 2024 (... the four elections of record ...)' (SeatAllocationBacktest.cs:32); 'ON THE FOUR ELECTIONS OF RECORD' (SeatAllocationBacktest.cs:182); 'its three CSVs ... which GeneratedCatalogCheck runs over the four years' (USA_STAGE_PLAN.md:188). Borderline: 'three CSVs and one catalog' (GeneratedCatalogCheck.cs:517). The code loops over whatever the catalog holds. If a year is added to the tool's @years (e.g. 2008), every one of these sentences becomes false. CommentClaimCheck and DocumentClaimCheck would stay green: they resolve backticked Type.Member names only, and DocumentClaimCheck reads root .md files only.

**The fix proposed.** Point to the source instead of copying it, e.g. 'every election the generated catalog holds (`UsPresidentialReturns.Years`)', 'the generator's CSVs', 'over every year of the catalog'.

**The skeptic's evidence.** I could not refute it. The quoted text is in the staged files word for word, and it is true today: `UsPresidentialReturns.Years` holds four rows (2012, 2016, 2020, 2024). The problem is that it copies a fact about the catalog's contents into comments that sit over code which takes no year from anywhere but the catalog.

The year-bound sentences, each above code that reads the catalog:
- ElectoralCollege.cs:13 says "...through <see cref="FromCatalog"/>, which hands it the elections of record 2012 to 2024." `FromCatalog(int year)` filters `UsPresidentialReturns.States` and `UsPresidentialReturns.Districts` by year. Its own summary says "Throws for a year the catalog does not hold", which is already the referenced form.
- SeatAllocationBacktest.cs:32 says "USA ELECTORAL COLLEGE, 2012 TO 2024 (...the four elections of record...)".
- SeatAllocationBacktest.cs:182 says "ON THE FOUR ELECTIONS OF RECORD". The code below it, at :191, is `foreach (var record in UsPresidentialReturns.Years)`.
- USA_STAGE_PLAN.md:188 says "which `GeneratedCatalogCheck` runs over the four years in the cheap bar". The check loops `foreach (var y in UsPresidentialReturns.Years)`.

How these go stale:
- The only place the years are set is the tool's `my @years = (2012, 2016, 2020, 2024);` (Tools/us_returns_prep.pl:52) plus its per-year source specs (line 346 on).
- Widening or narrowing the scope therefore changes Tools/ and ElectionsData/ and regenerates the catalog. None of the three C# files above has to change, because each is written to work with any set of years.
- No check would notice:
  - CommentClaimCheck only resolves backticked `Type.Member`. Its regex is `` `([A-Z][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)` ``.
  - DocumentClaimCheck reads only `Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly)`, so docs/specs is never read.
- The count "four" would then be wrong, not merely incomplete.

Why "the scope was ruled" does not save it:
- CLAUDE.md lists a count and a "there are N of X" among DERIVED claims. Its example is "never '53 parties'", and that set was sourced and ruled too.
- These sentences describe what the code does (FromCatalog hands, the loop runs, the check runs over). They do not say what is ruled.
- Precedent: Reviews/2026-10-05_s784_konf_own_record.md finding 8 graded this same class minor (reviewer) and minor (skeptic): comments stating a data set's current content ("none since §784", "every Polish fact"), true when written.

Narrowing:
- GeneratedCatalogCheck.cs:517 ("three CSVs and one catalog") is not part of this. Adding a year does not change it, and it describes the method's own three hard-coded `ReadUsCsv` calls at :549-551. At most a note.
- The plan line is the weakest of the four. Its own item scopes "four elections" (plan :151), but it still describes what the check iterates.

A related site the finding missed: the generator writes its catalog summary from a hard-coded string, "the United States' presidential elections of 2012, 2016, 2020 and 2024" (Tools/us_returns_prep.pl:625, and so UsPresidentialReturns.cs:9). It is not built from `@years`, so the GENERATED summary is itself copied by hand into the template.

There is no runtime effect, and every statement is true when measured, so the severity is minor.

**The skeptic's corrected fix.** Point to the catalog instead of naming its years. CommentClaimCheck will then resolve the backticked name.
- ElectoralCollege.cs:13: "...both through <see cref="FromCatalog"/>, which hands it any election the generated catalog holds (<see cref="UsPresidentialReturns.Years"/>)."
- SeatAllocationBacktest.cs:32-33: "- USA ELECTORAL COLLEGE, every election of record the generated catalog holds (`UsPresidentialReturns.Years`; 2024 added Day-1, R-EL8; the catalog since PS-6 US-3, `COMPLETED.md` §786): ..."
- SeatAllocationBacktest.cs:182: "// --- USA ELECTORAL COLLEGE, BY THE REAL RULE, ON EVERY ELECTION OF RECORD THE CATALOG HOLDS (R-EL8, ruled 2026-08-29; the catalog since PS-6 US-3, COMPLETED.md s786)."
- USA_STAGE_PLAN.md:188: "...the catalog `UsPresidentialReturns` and its CSVs (by year, by jurisdiction, by district) ...; `ElectoralCollege.FromCatalog` feeds the allocator, which `GeneratedCatalogCheck` runs over every year of the catalog in the cheap bar."
- Also fix the generator's template (Tools/us_returns_prep.pl:625), so the generated summary really comes from the tool's year list: "the United States' presidential elections of " . join(', ', @years[0 .. $#years - 1]) . " and $years[-1] by jurisdiction - SOURCED ...". The output is byte-identical today, and no digest covers the .cs file.
- Leave GeneratedCatalogCheck.cs:517 as it is. "Three CSVs" describes the method's own three hard-coded reads, and adding a year does not change it.
- Optional, same class: ElectoralCollege.cs:11-13 lists its callers in prose. Prefer "its callers are editor code only, reaching it through <see cref="FromCatalog"/>", without naming them.

### 15. 'Refused every connection ... again on 2026-10-05': the fetch log records only status 000, not a refusal

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/president_returns.md:42

**The scenario.** usa_us3/cited2024/fetch_log.txt shows '19:46:21Z 000 0' for §32-710 and '19:46:42Z 000 0' for §32-1038. They follow the 19:45:59Z BillTrack50 fetch at about 21 s intervals, which looks like connect timeouts rather than an immediate refusal (the August text recorded ECONNREFUSED). The BILLED line at :51 ('no connection ... (curl status 000)') is accurate. Reading 11, district_method_2024.md:141 ('refused again') and USA_STAGE_PLAN.md:188 ('refused') claim more than the log shows.

**The fix proposed.** Use 'no connection (curl status 000)' in all three places.

**The skeptic's evidence.** The record's lines:
- president_returns.md:42 (reading 11): "Both are Internet Archive captures: the Legislature's own host refused every connection, in August and again on 2026-10-05."
- president_returns.md:51 (BILLED): "no connection on 2026-10-05 (curl status 000), as in August". This is accurate, and it disagrees with :42 about what happened.
- district_method_2024.md:118-119 records the August failure as "refused connections throughout (ECONNREFUSED on two IPs)". The new :141 says "the Legislature's host refused again on 2026-10-05", so "again" claims the same ECONNREFUSED failure.
- USA_STAGE_PLAN.md:188: "nebraskalegislature.gov (refused; Internet Archive captures stand in)".

What the day's evidence shows:
1. PoliSim-captures/sources/usa_us3/cited2024/fetch_log.txt has `19:45:59Z 200 billtrack50`, then `19:46:21Z 000 0 …statute=32-710` and `19:46:42Z 000 0 …statute=32-1038`. The fetch command, from the session transcript, was `fetch() { … code=$(curl -sL --max-time 90 …); … printf … "$(date -u …)" … }`. The time is stamped after curl returns, so each attempt took 21-22 s. That is well short of `--max-time 90` and matches the Windows OS TCP connect timeout (SYN retries at 3+6+12 s). A refused connection (RST) fails in about a second. `-s` without `-S` meant curl's error text was not kept.
2. The sourcing pass's own report on 2026-10-05 (workflow wf_4e6ba008-c39.json) says: "nebraskalegislature.gov, www.nebraskalegislature.gov, leg.ne.gov and www.leg.ne.gov all timed out (curl 28) on 443, and http on 80 timed out too". Its verifier says: "Live nebraskalegislature.gov and www.nebraskalegislature.gov still time out (curl 28)."

So on 2026-10-05 the connections timed out, which the session itself measured; they were not refused. Reading 11's "refused every connection … again" and district_method's "refused again" describe the August ECONNREFUSED failure as if it happened again.

Partial refutation (plan line only): USA_STAGE_PLAN.md:188 uses "refused" the way the ruling does ("a refused host BILLED"). The project uses the word loosely elsewhere too: returns_2024.md:73 calls Alaska's HTTP 405 "the one host that refused", and the plan's :728 says hosts "refuse this machine". The plan line reads as the category name, not a claim about the cause.

Impact: wording only. The substance is right: the host was not reached, it is BILLED, and the Internet Archive captures stand in. Nothing parsed or generated depends on it.

**The skeptic's corrected fix.** president_returns.md:42: change "the Legislature's own host refused every connection, in August and again on 2026-10-05" to "the Legislature's own host could not be reached: it refused connections in August (`district_method_2024.md`) and gave no connection on 2026-10-05 (curl status 000; see Not reached - BILLED)". district_method_2024.md:141: change "the Legislature's host refused again on 2026-10-05" to "the Legislature's host gave no connection on 2026-10-05 (curl status 000)". USA_STAGE_PLAN.md:188 can keep "refused" as the ruling's category, or match the others with "(no connection, curl status 000; Internet Archive captures stand in)". The sourcing pass's own "timed out (curl 28)" also supports "timed out", but "no connection (curl status 000)" is what the logged evidence shows and what the BILLED line at :51 already says.

### 16. The California top-two quotation is not verbatim: the saved page's second dash is a hyphen

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/returns_2024.md:58

**The scenario.** raw/rules/ca_sos_primary_elections.html reads 'primary election – regardless of party preference - move on' (an en dash, then a hyphen-minus). returns_2024.md:58 has en dashes in both places. The s786 read-back at :90 says 'Every quoted rule is on its saved page' and notes only NARA's dropped footnote marks, so a byte search for this quotation fails.

**The fix proposed.** Change the second dash to '-', as the Unicameral Update quotations were corrected, or note the difference alongside the footnote-mark note.

**The skeptic's evidence.** The finding is accurate. I could not refute it.

- **The saved page:** `raw/rules/ca_sos_primary_elections.html`, line 305, reads `primary election [E2 80 93] regardless of party preference [2D] move on`. That is an en dash, then a spaced hyphen-minus. The file passes `sha256sum -c`, its blob equals the index (0b6d134b…), and `president_returns.md`:151 registers it.
- **The record:** `returns_2024.md`:58 has `E2 80 93` in both places.
- **Byte search:** `grep -cF` with the record's string (two en dashes) finds 0 matches on the saved page. With the second dash as `-` it finds 1. No other copy of the sentence is on the page; the other "regardless of party preference" line, :329, is a different sentence.
- **The read-back:** at :90 (new in s786) it says "Every quoted rule is on its saved page — NARA's allocation page printing "538*" and "270*", footnote marks the quotation above drops". It lists that one difference and not the dash.

Context that limits the severity:
- **The words match.** The page's own dash pair is mismatched, an obvious typographic slip that quotation practice normally corrects without comment.
- **Nothing reads the quotation.** No check, tool or generated file uses it. Searching Assets/ and Tools/ for `returns_2024` or `ca_sos_primary` finds only unrelated France references.
- **Line 58 predates this change.** It was already at 5fe3c031, and :69 says it was "*(Filed verbatim from the research agent's return, 2026-08-28 night.)*".
- **There is a second unlisted difference of the same kind.** At :54 the FEC quotation reads "Total Electoral Vote = 538. Total Electoral Vote Needed to Win = 270" with one space. The saved xlsx's sharedStrings cell has two spaces after "538." (checked with `od -c`). So the read-back did not hold quotations to byte level everywhere, but the dash is a different character, not just spacing.

On the proposed fix: the parallel with the Unicameral Update corrections does not hold. Those (`district_method_2024.md`) swapped paraphrases for the page's actual words, and a note says so. This one is a single typographic character with identical wording.

**The skeptic's corrected fix.** Leave line 58 as it is: it sits under :69's "Filed verbatim from the research agent's return", so editing it in place would need its own note. Instead, extend the read-back sentence at :90 to list the differences in typography. For example: "...footnote marks the quotation above drops; California's page closes its dash pair with a spaced hyphen ("preference - move on"), which the quotation prints as a second en dash; and the FEC cell has two spaces after "538."." Alternatively, say plainly that quotations are held to their words, with dashes and spacing normalised. Either way, cover the FEC spacing too, so the list of differences is complete.

### 17. The claim that the cited FEC URLs redirect and serve the same bytes ('checked 2026-10-05') has no fetch-log entry

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/returns_2024.md:77

**The scenario.** No fetch_log.txt, either under raw/ or under PoliSim-captures/sources/usa_us3/, records a request to fec.gov/documents/5644/… or /5645/…. Only the cms-content URLs are logged. The check is asserted but no record of it was kept, against the rule 'a source is fetched, not remembered'.

**The fix proposed.** Fetch the two cited URLs and log the status, redirect target and digest. Or reword: 'saved from the cms-content URLs; the cited /documents/ URLs were not re-fetched'.

**The skeptic's evidence.** The literal premise holds, but the finding's conclusion and its second fix do not.

LITERALLY TRUE: no fetch log, in tree (ElectionsData/usa/raw/*/fetch_log.txt) or out of tree (PoliSim-captures/sources/usa_us3/{fec,nara,law,district,cited2024,vote2016}/fetch_log.txt), contains documents/5644 or /5645. raw/returns/fetch_log.txt has only:
`2026-10-05T16:11:11Z 200 21376 fec_2024presgeresults.xlsx https://www.fec.gov/resources/cms-content/documents/2024presgeresults.xlsx`
`2026-10-05T16:11:12Z 200 331790 fec_2024presgeresults.pdf https://www.fec.gov/resources/cms-content/documents/2024presgeresults.pdf`

REFUTED, those two lines ARE the requests to the cited URLs:
- The FEC sourcing agent's helper (scratchpad fetch.sh, written 16:10Z; the same logic survives in scratchpad/us3_fec_private/us3_fec_fetch.sh) runs `OUT=$(curl -sS -L -A "$UA" -o "$D/$F" -w "%{http_code} %{size_download} %{url_effective}" "$U")` and logs `$EFF`, the URL after redirects.
- Its call at 16:11:10.763Z (session subagents/workflows/wf_4e6ba008-c39/agent-a5fdbb94b84f3f290.jsonl line 43) was `"$SP/fetch.sh" fec_2024presgeresults.xlsx "https://www.fec.gov/documents/5645/2024presgeresults.xlsx"; "$SP/fetch.sh" fec_2024presgeresults.pdf "https://www.fec.gov/documents/5644/2024presgeresults.pdf"`. The saved bytes are therefore exactly what the cited URLs served.

The redirect was also checked explicitly:
- At 16:13:04Z (same transcript, lines 99-100), `curl -sS -I` on both cited URLs returned `HTTP/1.1 302 Moved Temporarily` with `Location: https://www.fec.gov/resources/cms-content/documents/2024presgeresults.xlsx`, and the same for the PDF.
- At 19:54:09Z the main session re-fetched both (main transcript c8a2f04f lines 21081-21082) with `curl -sL ... -w "%{http_code} %{url_effective}"` and got `200 .../cms-content/documents/2024presgeresults.xlsx 68acdee2924d771b` and `200 .../2024presgeresults.pdf c99a431e08cb7790`. These equal the saved files' digests (68acdee2…, c99a431e… in raw/returns/SHA256SUMS.txt).

So line 77's claim is true, and "a source is fetched, not remembered" (CLAUDE.md:112) is not breached. The finding's second fix ("saved from the cms-content URLs; the cited /documents/ URLs were not re-fetched") would write a false statement.

WHAT SURVIVES (note):
- Nothing on disk pairs the cited URLs with those log lines. The URL column is curl's url_effective, and no record says so; president_returns.md:5 says each group's fetch_log.txt "carries every URL, UTC time and HTTP status".
- The 302 header dump and the re-check exist only in session transcripts. Someone with only the repo cannot confirm line 77's "redirect ... (checked 2026-10-05)" without fetching again, which fails CLAUDE.md:111's three-way test ("would a reader with no memory of the session reach the same verdict from what is written down").

Fix constraints:
- The fetch logs are not in SHA256SUMS.txt and not read by Tools/us_returns_prep.pl, GeneratedCatalogCheck.cs or UsPresidentialReturns.cs (grep: no `fetch_log`).
- The register generator (scratchpad us3_register.pl) does `$log{$c[3]} = [@c]` (keyed by the file column, last row wins) and then `die ... unless $lg->[1] eq '200'`.

**The skeptic's corrected fix.** Do not reword to "the cited /documents/ URLs were not re-fetched"; that is false. Re-fetching is also unnecessary, because the saved files were fetched from the cited URLs. Instead, make the existing evidence readable from the repo:

(1) returns_2024.md:77: replace "the cited URLs redirect to `fec.gov/resources/cms-content/documents/` and serve the same bytes (checked 2026-10-05)" with wording like: "saved on 2026-10-05 by requesting the cited URLs; each answered 302 to `fec.gov/resources/cms-content/documents/`, the URL `raw/returns/fetch_log.txt` records at 16:11:11Z and 16:11:12Z (that column is the URL as reached after redirects)".

(2) president_returns.md:5: change "carries every URL, UTC time and HTTP status" to "carries every URL as reached after redirects, the UTC time and the final HTTP status".

(3) Optional, to put the 302 itself on disk: append a NOTE row in the house form to raw/returns/fetch_log.txt (and to the out-of-tree usa_us3/fec/fetch_log.txt), e.g. `2026-10-05T16:13:04Z<TAB>NOTE<TAB>-<TAB>-<TAB>https://www.fec.gov/documents/5645/2024presgeresults.xlsx and /documents/5644/2024presgeresults.pdf answered 302 to the cms-content URLs logged at 16:11:11Z and 16:11:12Z`. Keep column 4 as '-', not a saved file's name: us3_register.pl keys rows on that column, keeps the last row per file and dies unless its status is 200. No hash, catalog or tool change is needed, because the logs are not hashed or read by the tool.

### 18. Reading 10's quoted row labels and reading 9's 'same headings' do not hold for every year

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/president_returns.md:41

**The scenario.** The workbooks label the rows 'CG 1 Total' (2020, sheet CG1) and 'CG 2 Total' (2024, sheet CG2), not 'CG1 Total'/'CG2 Total' as quoted; the tool's /CG\s*n\s+Total/ matches both forms. Reading 9 (line 40) says 'the House races follow under the same headings', but the 2012 book puts them under 'District One - 2 yr term', and in 2016 only 'Congressional District 03' is reused. No figure changes, but a re-verifier searching for the quoted strings finds nothing on two sheets.

**The fix proposed.** Quote the labels as printed ('CG1 Total' / 'CG 1 Total'). Limit the heading claim to 2020 and 2024, plus district 3 in 2016.

**The skeptic's evidence.** I could not refute it. Both claims hold against the saved bytes. No figure or tool result depends on either sentence.

(1) Reading 10 (president_returns.md:41) quotes the rows as "CG1 Total" and "CG2 Total". I resolved column C of each sheet's XML through sharedStrings. In maine_sos_president_by_cd_2020.xlsx, CG1!C135 = "CG 1 Total" (its formula D32+D47+D67+D88+D100+D131+D133 includes row 133, "UOCAVA Dist 1") and CG2!C427 = "CG2 Total". In maine_sos_president_by_cd_2024.xlsx, CG1!C133 = "CG1 Total" and CG2!C425 = "CG 2 Total"; the sheet's other label is "CG2 UOCAVA Total". So no cell in the 2020 workbook contains "CG1 Total", and none in the 2024 workbook contains "CG2 Total". Each quoted string is right for only one of its two years. The tool's match at us_returns_prep.pl:482, `/^CG\s*$cd\s+Total$/`, accepts both forms, so the parse is correct. Only the quotation is wrong, and the rest of reading 10 (towns plus UOCAVA, summing to the state) is accurate.

(2) Reading 9 (line 40) says "the House races follow under the same headings". I ran `pdftotext -raw` (xpdf 4.06) on each book and traced the regex at line 463:
- **2012:** the president's headings are at text lines 419, 441 and 447 ("Congressional District One/Two/Three"). The House races are at 627-628 ("Member of the U.S. House of Representatives" / "District One - 2 yr term"), 650 ("District Two - 2 yr term") and 656 ("District Three - 2 yr term"). They never come under a "Congressional District" heading. The book also prints "TOTAL", not "Total"; the regex ignores case.
- **2016:** the House headings break across two lines: 609-610 "Congressional" / "District 02" and 621-622 "Congressional" / "District 01", so they do not match. Only line 646, "Congressional District 03", recurs. Its House race has one candidate ("Total 226,720", one number), so the tool's $cd stays 3 until text line 779, "Total 7,959 7,476", which belongs to a Legislature District 03 race. The `//=` at line 465 throws that line away.
- **2020 and 2024:** the headings do recur: 2020 at 807, 834 and 846; 2024 at 982, 1001 and 1012, with "- Two Year Term" added.

The tool's picks are the president's lines in every year: 2012 at 422/444/450, 2016 at 475/510/529, 2020 at 546/578/595 and 2024 at 602/636/662.

Grade: note. These are inexact quotations and one generalisation that is false for 2012, in a record that is still PROVISIONAL and due for re-verification. Any re-verifier would hit them, so they are worth fixing before commit. They change no figures, no checks and no behaviour.

**The skeptic's corrected fix.** Edit only president_returns.md, and add no figures.

Reading 10: "Maine's 2020 and 2024 districts are each district sheet's 'CG n Total' row of the Secretary of State's workbook by district (printed 'CG 1 Total' and 'CG2 Total' in 2020, 'CG1 Total' and 'CG 2 Total' in 2024; the tool's match allows the space) — the towns and the overseas (UOCAVA) votes within it — and sum exactly to the state."

Reading 9: keep the first clause, quoting the label as "Total" (2012 prints "TOTAL"; the match ignores case). Replace "the president's race comes first and the House races follow under the same headings" with: "the president's race comes first. In 2020 and 2024 the House races follow under the same headings (2024's add '- Two Year Term'). In 2016's text only district 3's heading recurs, because 01 and 02 break across two lines, and its House race has a single candidate, so the next two-number Total belongs to a later race. 2012 heads its House races 'District One - 2 yr term'. The tool keeps the first reading for each district, so no later Total can replace the president's."

The tool, CSVs and catalog need no change.

### 19. Two register titles differ from the documents' own titles

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/president_returns.md:114

**The scenario.** archives_ascertainment_maine_2012.pdf is headed 'Amended Certificate of Ascertainment of Electors' and signed 'this twentieth day of December' 2012; the register calls it 'Maine's 2012 Certificate of Ascertainment'. At line 136, nebraska_sos_general_canvass_2012.pdf calls itself 'Official Results of Nebraska General Election - November 6, 2012', and 'Board of State Canvassers' appears nowhere in its 59 pages' text; the register titles it 'Official Report of the Board of State Canvassers, 2012'. A re-verifier looking for the original certificate could end up checking against a different document.

**The fix proposed.** Use the documents' own titles, and say that the 2012 Maine certificate is the amended one.

**The skeptic's evidence.** Both factual claims hold. Both labels are hand-written, and nothing reads them.

1. Register, line 114: `archives_ascertainment_maine_2012.pdf | NARA - Maine's 2012 Certificate of Ascertainment (the Governor's; scan)`.
   - The document's own heading is different. `pdftotext -raw` page 1 gives "STATE OF MAINE / Amended Certificate ofAscertainment ofElectors". Page 4 gives "given under my hand, this twentieth day ofDecember in the year two thousand and twelve."
   - I checked both on the page JPEGs inside the PDF (objects 175 and 12, extracted to the scratchpad and viewed).
   - Partly defensible: the label follows NARA's own XMP title for the file, "Maine Certificate of Ascertainment 2012". It drops "Amended" all the same.
   - The 2016 certificate is the original. It is headed "Certificate of Ascertainment of Electors" and dated "this eighth day of December". The register styles the two rows alike, so the difference does not show.
   - The record's own standard says this kind of status: the register (line 137) and reading 9 both say the 2016 Nebraska book is "REVISED". `grep -i amend` over president_returns.md, the TSV and the tool finds nothing about the Maine amendment.
   - Smaller point: this row also leaves out the OCR text layer that the PDF carries. The TSV header admits that layer, and the 2016 row says "scan; OCR text layer".

2. Register, line 136: `nebraska_sos_general_canvass_2012.pdf | Nebraska Secretary of State - Official Report of the Board of State Canvassers, 2012`.
   - The PDF has 59 pages (59 form feeds and 59 `/Type /Page`) and no image XObjects, so no cover can hide in a picture.
   - Page 1 reads "Official Results of Nebraska General Election - November 6, 2012 / Table of Contents".
   - `grep -i 'canvass|board of state|official report'` finds 0 hits in the raw, default and layout modes.
   - The XMP title is "Microsoft Word - 2012 General Canvass Final with Recounts 12-11".
   - The publisher's own link (nebraska_sos_previous_elections.html:1377) labels it "General Election Official Results".
   - The phrase comes from the later books' covers: 2016 "REVISED OFFICIAL REPORT OF THE BOARD OF STATE CANVASSERS", 2020 "OFFICIAL REPORT OF THE NEBRASKA BOARD OF STATE CANVASSERS", 2024 "THE NEBRASKA BOARD OF STATE CANVASSERS OFFICIAL REPORT".
   - In the saved pages, "board of state canvassers" appears only in the statute capture of s32-1038.

Why only a note:
- No figure, check or tool behaviour depends on these labels. The tool and `UsPresidentialReturns.RawSources` name these pages by path and SHA-256 only. Both digests match SHA256SUMS.txt and the register.
- Each row carries its exact URL and digest, and the file is in the tree. A re-verifier therefore reads the right bytes, so the finding's "a different document" scenario is unlikely.
- NARA's 2012 page does carry a commented-out link, `ascertainment-maine.pdf`. Every state has the same pattern, so it is a site-wide URL change and not evidence of a second Maine certificate.
- The ruling's done-when asks for publisher, date and basis, and all three are correct.

**The skeptic's corrected fix.** Edit the record only. No tool, CSV or catalog change is needed, because nothing reads these labels.

1. Line 114, "Publisher - what" column: `NARA - Maine's 2012 Amended Certificate of Ascertainment of Electors (the Governor's, given 20 December 2012; scan; OCR text layer)`.

2. Line 136: `Nebraska Secretary of State - Official Results of Nebraska General Election - November 6, 2012 (linked as "General Election Official Results"; no Board of State Canvassers cover, unlike 2016-2024)`.

3. Add one sentence to reading 1, matching reading 9's "REVISED" note: the 2012 certificate NARA serves, and the one the READ rows stand on, is the Governor's amended certificate of 20 December 2012; the original was not saved and is not read.

4. Optional: change the comment at Tools/us_returns_prep.pl:453 from "the Board of State Canvassers' book" to "the Secretary of State's canvass book". This edits the tool, so rerun it in a temp copy to confirm it still writes the same bytes.

### 20. The ruling's 'electors who broke their pledge named' is re-read in the record instead of being put to the owner, and the justification goes beyond the saved pages

- **Lens:** record - **reviewer:** note - **skeptic:** minor
- **Where:** ElectionsData/usa/president_returns.md:34

**The scenario.** Ruling F8 asks for 'the electors who broke their pledge named and not modelled'. Reading 3, repeated in USA_STAGE_PLAN.md:188, re-reads that as 'the votes named by state and recipient', on the ground that 'naming the electors would rest on press, not on the record'. The saved pages support only a narrower point, which is true: no saved certificate links a vote to an elector. Whether only the press names them is not shown by any saved page. Unverified pointer, not checked here: the Supreme Court's 2020 opinion in Chiafalo v. Washington is an official record reported to name Washington's three Powell electors. Under ruling-first, changing what the owner's done-when requires is the owner's call.

**The fix proposed.** Put the reading to Elias as an open question, with the evidence limit (the certificates record ballots). Remove the 'press, not the record' sentence or give it a source.

**The skeptic's evidence.** I could not refute either half of the finding. I am raising it from note to minor.

1. The reading's justification is unsourced, and an official record contradicts it.
- ElectionsData/usa/president_returns.md:34 ends: "The plan's "the electors who broke their pledge named" is read as the votes named by state and recipient; naming the electors would rest on press, not on the record."
- raw/returns/fetch_log.txt has 17 fetches, all from NARA, the FEC, the Clerk and UF. No press page or court page was ever fetched, so the claim about where the names live was never sourced.
- I fetched it read-only and outside the repo: https://www.supremecourt.gov/opinions/19pdf/19-465_i425.pdf (sha256 4491bf0e0b0730793b9476a6b488fab6c1c93abcc88ac5b34a9622871cc96ec2), Chiafalo v. Washington (2020).
  - Syllabus: "Three Washington electors, Peter Chiafalo, Levi Guerra, and Esther John (the Electors), violated their pledges to support Hillary Clinton in the 2016 presidential election. In response, the State fined the Electors $1,000 apiece".
  - Opinion of the Court, slip op. 6-7: "Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John ... So the three Electors voted for Colin Powell for President. ... Only seven electors across the Nation cast faithless votes".
  - It also cites In re Guerra, 193 Wash. 2d 380 (2019).
- So an official record that is not press names 3 of the 7 and ties them to the Powell votes.

2. The narrower point is true.
- `pdftotext -raw` on the three saved Certificates of Vote:
  - Washington: "we have voted, by ballot ... Colin Powell . . . Three (3) Votes ... Faith Spotted Eagle ... One (1) Vote". Unlinked signatures follow (Esther John, Peter Chiafalo, Varisha Khan, Levi Guerra, Eric Herde...).
  - Texas: "voted by individual ballot".
  - Hawaii: "by distinct ballots".
- No saved page ties a vote to an elector. The plan's own line (USA_STAGE_PLAN.md:188, "no saved record names the electors who cast them") is accurate as scoped.

3. The owner's done-when is narrowed without being put to the owner.
- USA_STAGE_PLAN.md:192 reads "...304–227 cast, the electors who broke their pledge named and not modelled (2012's and 2016's figures from memory until NARA's pages are read)". The parenthetical covers only the figures.
- The staged record says "is read as" and the plan says "Read as built". Neither asks Elias.
- The draft record (scratchpad rec786.md) lists it under "Readings", with no ask.
- The session's own plan (us3_plan.md:22) framed this as a rewording: "Either fetch them or word the done-when as 'the recipients named'".
- The repo's practice when a session reads the owner's words is to mark the reading pending or put it to him:
  - CLAUDE.md:53 (F2): "read, pending Elias, as §652's condition words it".
  - COMPLETED.md:37433 (§784): "SEVEN OTHER READINGS PUT TO ELIAS".
  - The feature list's K-1i: "OPEN, Elias's ... wired on the builder's reading".

Why minor and not defect: no CSV, catalog figure, check or allocator path depends on the electors' names. Faithless electors are not modelled. The recipients and counts that are named are held NARA-to-FEC by the tool. The cost is a false statement in a [SOURCED] record, used as the ground for closing a done-when bullet the owner ruled, with no ask.

**The skeptic's corrected fix.** 1. In ElectionsData/usa/president_returns.md:34, delete "naming the electors would rest on press, not on the record". Keep the true, scoped sentence: each certificate records ballots ("by distinct ballots" / "by individual ballot" / "voted, by ballot"), so no saved page ties a vote to an elector.
   - If the record is to say where names exist, it needs a source. The U.S. Supreme Court's Chiafalo v. Washington (2020, supremecourt.gov/opinions/19pdf/19-465_i425.pdf) names Washington's three Powell electors (Peter Chiafalo, Levi Guerra, Esther John).
   - Either save that opinion under raw/returns/ (a fetch_log line, a SHA256SUMS line, a register row with basis official) and name those three, BILLING the other four (Washington's Faith Spotted Eagle vote, Texas's Paul and Kasich votes, Hawaii's Sanders vote) as having no official record found;
   - or carry no names and make no claim about where they live.

2. Put the narrowing to Elias as an open reading in the §786 record and the reply. Mark the plan's Built line "read as built, pending Elias". Give him the options and their costs:
   - (a) accept "named by state and recipient": as built, nothing more owed;
   - (b) name the three on the Supreme Court's opinion and BILL the other four: one page to save and register;
   - (c) name all seven from press sources, which the sourcing rules would need him to allow.

### 21. Older claim, now checkable against a saved page: LB3 also amends §32-713

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/district_method_2024.md:101

**The scenario.** The saved BillTrack50 page reads 'to amend sections 32-710, 32-714, and 32-1038, Reissue Revised Statutes of Nebraska, and section 32-713, Revised Statutes Cumulative Supplement, 2024'. Line 101 says LB3 was introduced 'to amend §§32-710, 32-714 and 32-1038'. The s786 pass that brought this file up to the raw-page standard did not catch it.

**The fix proposed.** Add §32-713, citing the saved BillTrack50 page.

**The skeptic's evidence.** The finding is real: line 101 lists three of the four sections LB3 amends. I could not refute it.

1) The record, ElectionsData/usa/district_method_2024.md:100-102 (older text; the s786 hunks are at 109-117 and 137-144):
   "LB3 (109th Legislature) was introduced 9 January 2025 by Sen. Loren Lippincott at Governor Pillen's request, to amend §§32-710, 32-714 and 32-1038 so that all five electors follow the statewide winner."

2) The saved page, ElectionsData/usa/raw/district/billtrack50_ne_lb3_1771479.html:214. Its sha256 a5cc5211...8093 matches raw/district/SHA256SUMS.txt, and it is registered at president_returns.md:116. The text sits under the page's "Bill Summary" heading, after "Introduced Session ... 109th Legislature", and it is the bill's own title, not the aggregator's "AI Summary" that the record already warns about:
   "A BILL FOR AN ACT relating to presidential electors; to amend sections 32-710, 32-714, and 32-1038, Reissue Revised Statutes of Nebraska, and section 32-713, Revised Statutes Cumulative Supplement, 2024; ..."
   Line 269 also gives "Introduced - 01/09/2025", which agrees with the record's date.

3) No other saved page settles the sections. Neither Unicameral Update page (37216, 39341) contains any "32-71x" string, and the Wayback §32-710 capture names only 32-710 and 32-711. BillTrack50 is the only saved source for what LB3 amends, and it names four sections. "§§32-710, 32-714 and 32-1038", with "and" before the last, reads as a complete list.

4) Repo-wide, line 101 is the only place outside raw/ that lists LB3's sections. COMPLETED.md's three LB3 mentions name no sections, and it has no §786 entry yet.

Why only a note:
- The text predates this change.
- The s786 read-back (president_returns.md:61) does not claim to have checked this sentence. It covers "the four statute quotations", the 2024 district table, and the two session-review quotations.
- Nothing downstream reads it. The verdict (not enacted), the district method, the catalog, ElectoralCollege.FromCatalog and every check are unaffected. The enumeration is incomplete, not wrong about any section it names.

One small aggravation: the new s786 bullet (district_method_2024.md:144) lists the LB3 facts that "rest on the press named above, not fetched", which suggests the rest of the paragraph is backed by the saved pages. The amended sections are on a saved page, and they disagree by one section.

**The skeptic's corrected fix.** At district_method_2024.md:101, change "to amend §§32-710, 32-714 and 32-1038" to "to amend §§32-710, 32-714 and 32-1038 (Reissue Revised Statutes) and §32-713 (Revised Statutes Cumulative Supplement, 2024)".

Record the correction in place, as s786 did for the two session-review quotations: for example, add to the LB3 bullet of the Raw pages section that the bill's title on the saved BillTrack50 page (raw/district/billtrack50_ne_lb3_1771479.html) names four sections, and that the earlier text named three. This is a documents-only change: no tool, CSV or catalog output changes.

### 22. 'Re-hashes every page' covers only the catalog's RawSources, and the register is called 'Generated' though no generator exists

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/president_returns.md:10

**The scenario.** GeneratedCatalogCheck re-hashes the 18 pages in UsPresidentialReturns.RawSources, the pages the tool read, not the 68 registered. No Editor check reads any SHA256SUMS.txt, so the other 50 pages (statutes, rules, certificates of vote, Unicameral Update, and so on) are protected only by a hand-run sha256sum -c. The register heading at :65 says 'Generated from each group's fetch_log.txt and SHA256SUMS.txt', but no committed command produces it, so it cannot be regenerated or checked. All 68 rows match today.

**The fix proposed.** Say 're-hashes every page the tool read (RawSources)'. Either commit the register generator or say 'compiled from' instead of 'Generated'.

**The skeptic's evidence.** I could not refute any of the facts in the finding. Only the wording of the record is wrong. No figure is wrong.

1. What the check re-hashes. `GeneratedCatalogCheck.cs` CheckUsPresidentialReturns, lines 536-541, loops only over `UsPresidentialReturns.RawSources`. That list (UsPresidentialReturns.cs:19-39) holds 18 entries, which its own summary calls "Every saved page the run read or relied on". These are the 16 pages the tool reads by code plus the two Maine certificates it hashes at us_returns_prep.pl:509, `page($certrel)`. The register lists 17 + 16 + 32 + 3 = 68 rows. `grep -rln SHA256SUMS Assets/ Tools/` finds only Tools/us_returns_prep.pl. In that tool, `page()` (lines 62-73) checks only the page it is about to read against its sums line; the other lines of %sums are never checked. So 50 registered pages (statutes, certificates of vote, the Unicameral Update pages, the rules pages, provenance pages) have no check in the bar.

2. The wording at president_returns.md:10. It says the catalog holds "each CSV's SHA-256 and every page's" and that `GeneratedCatalogCheck` "re-hashes every page". Line 5 introduces "pages saved byte for byte under `raw/` ... the register below names each page's publisher", so a reader of the record will take "every page" to mean every registered page. Line 18 scopes it correctly ("every page it reads"), and so does the catalog's own head comment ("the saved pages listed in RawSources"). Line 10 does not.

3. "Generated" at :65. It says "Generated from each group's `fetch_log.txt` and `SHA256SUMS.txt`". No script under Tools/ reads a fetch_log.txt, and us_returns_prep.pl writes no .md. The Publisher/Basis/Use columns cannot be derived from those two files at all, since the logs carry only time, status, bytes, file and URL. CLAUDE.md defines GENERATED as "emitted by a tool into a marked block, re-derivable by one command", so the label claims a status the register does not have. No other ElectionsData register (records_by_date.md, budget_procedure.md, confidence_rules.md and others) calls itself generated.

4. Why the grade is note. I checked every row read-only. A perl comparison of all 68 rows against each group's fetch_log.txt and SHA256SUMS.txt found no difference in time, URL or digest. `sha256sum -c` passes for returns, apportionment, district, rules and executive. `**/raw/** -text` keeps git from changing the bytes. The 50 uncovered pages are protected the same way as every other raw page in the project, which is what the ruling asks for ("every new raw page passes sha256sum -c"). So the coverage is not a defect; the wording overstates it.

5. One overstatement in the finding itself. "Cannot be regenerated or checked" goes too far: the register can be checked, and I just did it with a one-off script. What it lacks is a committed command.

**The skeptic's corrected fix.** In president_returns.md:10, change "every page's" and "re-hashes every page" to "every page the run read or relied on (the catalog's `RawSources`), and the READ transcription's", and "re-hashes those pages and the transcription". Optionally add: "the register's other pages are held by each group's `SHA256SUMS.txt` (`sha256sum -c`), as every raw page is". In president_returns.md:65, replace "Generated from" with "Compiled from each group's `fetch_log.txt` (time, URL) and `SHA256SUMS.txt` (digest); the publisher, basis and use columns are this pass's reading", or say "as fetched 2026-10-05". Do not commit a register generator for the three hand-written columns. The tool's head comment (us_returns_prep.pl:17-18) can take the same scoping for consistency, though it is not wrong in its context.

### 23. Reading 11 names only the 2023 and 2025 amendments, but the saved pages also show 2019 and 2021 amendments that came after the 2012 and 2016 elections

- **Lens:** data - **reviewer:** note - **skeptic:** minor
- **Where:** ElectionsData/usa/president_returns.md:42

**The scenario.** The record says '§801, §803 and §805 carry amendments of 2023 and 2025, after some or all of the four elections'. The SECTION HISTORY on the saved pages is longer. raw/district/maine_legislature_21a_801.html lists PL 2001 c.516, PL 2019 c.539, PL 2023 c.628 and PL 2025 c.397. _803.html lists PL 1989 c.166, PL 2021 c.273, PL 2023 c.628 (RPR) and PL 2025 c.397. _805.html lists PL 1989 c.166, PL 2019 c.539, PL 2023 c.628 and PL 2025 c.397. So §801 and §805 were also amended in 2019, and §803 in 2021, after the 2012 and 2016 elections. A reader who dates the saved text of §801 or §805 against 2012 or 2016 would think only the 2023 and 2025 changes separate it from the text then in force. The point of the sentence (the saved text is later than some of the elections) still holds; only the list of years is short.

**The fix proposed.** Name every amendment after 2012: '§801 and §805 carry amendments of 2019, 2023 and 2025 (PL 2019 c.539, PL 2023 c.628, PL 2025 c.397), §803 of 2021, 2023 and 2025'. Or point to the pages' SECTION HISTORY instead of listing the years.

**The skeptic's evidence.** I could not refute this finding. The record's wording is short of what its own saved pages say.

The claim is at G:/UNITY/Projects/PoliSim/ElectionsData/usa/president_returns.md line 42 (reading 11, "The statutes' dates"): "§802, the district method, has stood unamended since PL 1985, c. 161; §801, §803 and §805 carry amendments of 2023 and 2025, after some or all of the four elections."

The SECTION HISTORY on each saved page, which is new in this change, says more. I checked each file's sha256 against raw/district/SHA256SUMS.txt lines 6, 8 and 9.
- `raw/district/maine_legislature_21a_801.html`: "PL 1985, c. 161, §6 (NEW). PL 2001, c. 516, §17 (AMD). PL 2019, c. 539, §4 (AMD). PL 2023, c. 628, §5 (AMD). PL 2025, c. 397, §49 (AMD)."
- `_803.html`: "PL 1985, c. 161, §6 (NEW). PL 1989, c. 166, §6 (AMD). PL 2021, c. 273, §29 (AMD). PL 2023, c. 628, §6 (RPR). PL 2025, c. 397, §50 (AMD)."
- `_805.html`: "PL 1985, c. 161, §6 (NEW). PL 1989, c. 166, §§7-9 (AMD). PL 2019, c. 539, §5 (AMD). PL 2023, c. 628, §§7, 8 (AMD). PL 2025, c. 397, §52 (AMD)."

So §801 and §805 were also amended in 2019, and §803 in 2021. All of these come after the 2012 and 2016 elections, and the 2021 one also after 2020. The §802 history in the same sentence is correct (only "PL 1985, c. 161, §6 (NEW)"). Because the sentence gives §802's full history, the list for the other three sections reads as complete too.

The likely cause is that the bracket after each subsection shows only that subsection's latest amendment. For example, §801(2), "Counting of ballots ... ranked-choice method ... section 723-A", carries only "[PL 2023, c. 628, §5 (AMD).]". The 2019 act appears only in the SECTION HISTORY.

The omission matters more than the finding says. The saved histories of §1 and §723-A (the ranked-choice count) also list "PL 2019, c. 539 ... (AMD)", next to its §801 and §805 entries. From the record, a reader dating the ranked-choice wording in §801(2) and §805(2) would place it before 2023 with no earlier limit. That would mix up the 2012/2016 rule with the 2020/2024 one. Reading 3 relies on 2016 elector rules, and the register cites §805 as "the pledge".

Why the severity is only minor:
- The sentence's main point still holds. PL 2025, c. 397 amended all three sections after all four elections, so none of the saved texts is the one in force for any of them.
- The record already points to a contemporaneous source for the 2012 rule (the 2015 Wayback capture).
- Nothing reads these pages. `Tools/us_returns_prep.pl` has no match for "maine_legislature" or "21a_", and UsPresidentialReturns.cs has no entry for them. They are held only by sha256sum -c, so no figure, CSV, catalog check or allocator result depends on this reading.

It is a misleading-by-omission description of a raw page in a record held to the raw-page standard. The same change corrected two paraphrased Unicameral Update quotations for the same kind of mismatch.

**The skeptic's corrected fix.** In president_returns.md line 42, replace "§801, §803 and §805 carry amendments of 2023 and 2025, after some or all of the four elections" with wording that lists every amendment after 2012 from each saved page's SECTION HISTORY. For example: "§801 and §805 carry amendments of 2019, 2023 and 2025 (PL 2019, c. 539; PL 2023, c. 628; PL 2025, c. 397), §803 of 2021, 2023 and 2025 (PL 2021, c. 273; c. 628, which repealed and replaced it; c. 397). Each page's SECTION HISTORY lists them, and the last is after all four elections, so none of the three saved texts is the one in force for any of the four elections."

A shorter form that only refers to the pages, as the claim convention prefers, also works: "§801, §803 and §805 were amended after 2012 (each page's SECTION HISTORY), the last time by PL 2025, c. 397, after all four elections."

It is also worth saying that the per-subsection brackets show only each subsection's latest amendment, so the ranked-choice wording in §801(2) and §805(2) is not dated by its "PL 2023" bracket. This is a prose edit to the hand-written readings only. The tool does not write this file, and no raw page changes.

### 24. The READ guard only bounds Maine's 2012 and 2016 district figures: a small misreading passes and is written to the catalog

- **Lens:** data - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_returns_prep.pl:527

**The scenario.** Tested in a temp copy, never in the repo. I changed the TSV row '2012 ME 1 D 223035' to 223135. The tool exited 0 and wrote '2012,ME,1,223135,142937,READ', and the catalog digest then pins that wrong figure. Lines 516-528 check only two things: no party's district total exceeds the state, and the two parties' shortfalls together stay within the certificate's unallocated count. The slack that check allows: 2012, raises up to D 273 / R 124 and cuts up to 18 in total (415 - 397); 2016, raises up to D 144 / R 32 and cuts up to 8 (184 - 176). No figure is wrong now. I re-read all eight READ figures: 2012 from the page JPEGs inside the PDF (p2 R 142,937 / 149,215; p3 D 223,035 / 177,998; footnote 415) and 2016 from the OCR text layer plus the p3 background JPEG (D 212,774 / 144,817; R 154,384 / 181,177; footnote 184). All eight match the TSV.

**The fix proposed.** No code change is possible from the saved pages: the town-level workbooks only add looser lower bounds. These figures rest on the reading, so keep the [PROVISIONAL] READ label until the record's second reading is logged. This review's re-reading agrees with all eight.

**The skeptic's evidence.** I could not refute the claim. I reproduced it in a scratch copy, not in the repo (the repo tree is unchanged). The unmodified copy writes the same bytes as the staged CSVs and catalog. Changing the TSV row "2012 ME 1 D" from 223035 to 223135 gives exit 0. The district CSV then reads "2012,ME,1,223135,142937,READ", the catalog row is (2012, "ME", 1, 223135L, 142937L, true), and ReadTranscriptionDigest becomes the hash of the changed TSV. GeneratedCatalogCheck (lines 544-546) only compares the TSV with that regenerated digest, so it passes too.

For the READ rows, the only number checks are lines 521-527: a party's two districts may not exceed the state, and the D and R shortfalls together may not exceed the unallocated count. The finding's slack is correct:
- 2012: shortfall D 273, R 124, total 397 against 415 (18 votes of room).
- 2016: shortfall D 144, R 32, total 176 against 184 (8 votes of room).

The gap is larger than the finding says. I swapped 2016's district labels in the TSV (CD1 becomes D 144817 / R 181177, CD2 becomes D 212774 / R 154384) and the tool still exited 0. The party sums do not change, and Maine's state split stays 3-1, so the split check (lines 548-576), which compares per state, cannot see it. The catalog would then give CD1 to Trump and CD2 to Clinton.

This is not a defect:
- **The limit is declared.** The tool header (lines 36-38) and the record's "What the tool proves" (president_returns.md line 23) both describe the check as a bound. Line 3 already marks the READ rows [PROVISIONAL] until "a second reading of the certificates", which is the finding's proposed fix word for word. Line 28 lists only mutations that do fail.
- **No figure is wrong.** I ran `pdftotext -raw` on each page the TSV cites and joined the split digit groups. 2012 page 2 gives R 142937 and 149215 and the footnote 415; page 3 gives D 223035 and 177998. 2016 page 2 gives D 212774 and 144817 and the footnote 184; page 3 gives R 154384 and 181177. All eight figures and both counts equal the TSV, in First/Second district order. The at-large lines equal the FEC's state figures.
- **Nothing downstream can change.** FromCatalog reads only `district.VotesR > district.VotesD`, and Allocate returns totals per candidate. A slip within the slack (273 votes at most) is far below every READ district's margin. Any slip that changes a state's split fails.

One part of the finding is wrong: "No code change is possible from the saved pages". The certificates' text layer is itself a saved page. The tool already hashes it (`page($certrel)`, line 509) and already runs pdftotext for Nebraska (line 457).

**The skeptic's corrected fix.** Nothing is owed under the ruling. Keep the READ rows [PROVISIONAL] until the second reading, as president_returns.md line 3 already says. Do not write the slack figures into any doc: they are derived figures, which the claim convention rules out.

If the guard should cover transcription slips, here is an optional hardening that uses saved pages only:
1. In the READ block of Tools/us_returns_prep.pl, run `pdftotext -raw -f <page> -l <page>` on the certificate each TSV row cites.
2. Join split digit groups (`s/(\d)[ ,]+(?=\d)/$1/g`) and skip the heading lines, which carry the footnote mark "District1" and OCR text such as "Co11gressio11al".
3. Raise a problem() unless each party's CD1 and CD2 figures are the first and second vote figures in that party's block, and the unallocated count is the one in the footnote. Name the row so a reader goes back to the page image.

This passes on today's TSV. It would fail both the +100 mutation and the 2016 CD1/CD2 swap. A deliberate correction against a wrong OCR digit would then need a declared exception, as the 2020 "Trunp" misprint already has.

## The first pass - refuted by the skeptics

- [csharp] FromCatalog takes the district method and the at-large count from the data's shape, so a missing district row is absorbed silently and the guard at line 136 can never fire - *ACCURATE PART: ElectoralCollege.cs:75 passes `state.Electors - districtWinners.Count` as the at-large count. That makes line 136 (`AtLargeElectors + DistrictWinners.Length != TotalEv`) always true for FromCatalog's output. Since the typed arrays retired, FromCatalog is the only code that builds a Jurisdiction (grep: only lines 74 and 75 call either constructor). So the guard is now redundant. The finding's arithmetic is also right: 2012 with no district rows gives 206-332, 2016 without ME-1 gives 306-232, 2020 without NE-3 gives 306-232, 2024 without NE-2 gives 313-225.

WHY IT IS NOT A FAILING PATH: a catalog missing a district row or a jurisdiction cannot reach FromCatalog. The only writer is Tools/us_returns_prep.pl, and it refuses one:
- :468: Nebraska's districts must be exactly 1..representatives every year in @years.
- :477-479: for Maine 2020/2024 it dies unless there is exactly one 'CGn Total' row for each apportioned district.
- :519: Maine 2012/2016 READ districts must be exactly '1 2'.
- :559: for any state with districts, `$nd == representatives($year,$s)`, so the at-large count is the two Senators' electors (the comment at :556).
- :378/:416/:429: 51 jurisdictions.
- :611/:640: the States rows are written once per @order code, so a duplicate cannot be written.
- :561-562: every state's split is held to NARA's table, which is stricter than the C# check's national comparison.

MEASURED: I ran the tool on a temp copy with Maine's 2016 district 1 rows removed from maine_districts_read_2012_2016.tsv (the finding's case 2). It died with 7 mismatches, the first 'MISMATCH: ME 2016: districts '2' READ', and wrote nothing (Generated/ stayed empty). The unmutated copy passed and wrote a UsPresidentialReturns.cs byte-identical to the staged one (sha256 c76df2df...). It also wrote president_by_district.csv with d2a5038b..., which equals the catalog's DistrictSourceDigest.

A HAND EDIT IS CAUGHT ON THE C# SIDE:
- Removing a row from the catalog alone trips `districts.Count != UsPresidentialReturns.Districts.Length` and the row-by-row SameRow in CheckUsPresidentialReturns.
- Editing the CSV too trips ReadUsCsv's digest check against DistrictSourceDigest.
- Only two things reach the scenario: editing the catalog, the CSV and the recorded digest together (three edits to DO-NOT-EDIT files), or changing the tool to remove its guards. Neither is in this change; the finding itself says 'Today's tool cannot produce this case.'

THE DOC PHRASE: 'computed from the statutes' (SeatAllocationBacktest.cs:34-36) describes AllocateJurisdiction's rule. The statute's structure (districts only for ME and NE, one per Representative, two at large) is held in the generator, so the phrase is not false.

THE ONE REMAINING GAP is in the tool, not in C#, and only after a future edit. If a year is added to @years without a Maine reader, nothing requires Maine to have districts (:559 checks only states that have them). Maine would then come out winner-take-all. That gives the right electors unless a district split from the state, and a split is caught state by state at :561. So no wrong split could pass even then.*
- [csharp] SeatAllocationBacktest's USA loop has no check that it ran: an empty or shortened catalog silently drops the USA assertions - *The scenario the finding describes cannot produce a shorter catalog, and the empty case is already caught in the cheap bar.

1. Narrowing the tool's year list does not regenerate anything. `@years` (Tools/us_returns_prep.pl:52) is not the only place the four years are fixed:
   - The FEC Table 2 reader is a fixed list for 2012, 2016 and 2020 (lines 346-348). It holds each heading to `surname($nara{$year}{name}{$p})` (line 358), and NARA's page is only read for years in `@years`.
   - The 2024 workbook block always runs and needs `$nara{2024}{name}` (line 397).
   - The state_ev_2024.csv check needs `$y{2024}` (line 588), and the Maine READ block is fixed to 2012 and 2016 (line 516).

   I ran this in a scratch copy, with ElectionsData/usa and the tool copied out and the repo untouched:
   - The unmodified tool passes and rebuilds UsPresidentialReturns.cs byte for byte (c76df2df…, the same as the staged file).
   - With 2024 dropped (the finding's example) it dies with "returns/fec_2024presgeresults.xlsx: the electors, NARA's nominees' columns (, ) or the total not found".
   - With 2020 dropped: "2 mismatch(es) - nothing written".
   - With 2016 or 2012 dropped: 4 mismatches each, for example "column 2 heads 'Obama (D)', NARA's nominee is ".
   - With `@years = ()` it dies on the 2024 workbook.
   - In every mutated run no CSV and no .cs file existed afterwards.

   A three-year catalog would need several deliberate edits at once: the FEC list, the 2024 block, the state_ev_2024 check, the READ block, and the catalog summary text at line 625, which names the four years. That is a scope change someone has to review, not a silent regression.

2. An empty or hand-shortened catalog fails GeneratedCatalogCheck, which is in the cheap bar (CheckSuite.cs:275):
   - `if (UsPresidentialReturns.Years.Length == 0) { wrong.Add("the catalog holds no year, so the allocator was never run"); }` (GeneratedCatalogCheck.cs:586).
   - CSV row counts against the catalog's arrays (lines 553, 560, 567).
   - Each CSV's SHA-256 against the digest recorded in the catalog, and a "holds no data rows" failure (ReadUsCsv, line 622).

   Dropping rows from the generated .cs alone, or from both files, fails one of these unless someone also forges the digest constants.

3. The backtest loop (SeatAllocationBacktest.cs:191, `foreach (var record in UsPresidentialReturns.Years)`) is outside the bar on purpose (ElectionsBacktests.cs:11). The only case where it would print no USA lines, an empty catalog, is already a cheap-bar failure, so a zero-iteration guard there would only repeat line 586.

4. The other catalog blocks in the same file guard drift and emptiness, not scope: the enumeration rule at lines 105-111 and CheckGermanLaender at 486-511, which pins no count of Länder. CheckUsPresidentialReturns follows that pattern, and the four-year scope is held by the generator itself.

The finding is right on one narrow point: nothing in C# lists 2012/2016/2020/2024. But no failing path exists without rewriting the generator.*
- [csharp] FromCatalog's winner rule depends on premises its doc does not state (ties; Maine's ranked-choice contests) and contradicts the class doc - *Every fact the finding states checks out. None of them leads to a failing path. The one claim that would make it a doc defect, "contradicts the class doc", only works if the sentence is misquoted.

(a) Ties. It is true that ElectoralCollege.cs:68 and :72 (`VotesR > VotesD ? Republican : Democrat`) give a tie to D, while us_returns_prep.pl:555 and :561 (`vD > vR ? 'D' : 'R'`) give it to R. Neither branch can ever run:
- :535 and :540 raise problem() for a tie in every state and district of every year.
- :598 dies on any problem, and the only writes (write_lf, :601 onward) come after that line.
- FromCatalog(int year) reads only the generated static arrays. Its only callers are GeneratedCatalogCheck.cs:578 and SeatAllocationBacktest.cs:193, and both loop over UsPresidentialReturns.Years.
So "does not matter today only because" is wrong. The tie refusal is built into every catalog the tool can write; it does not depend on today's figures. The doc's wording "the larger of the two nominees" already assumes one nominee is larger.

(b) Ranked-choice voting (RCV). FromCatalog's doc (:52-54) claims only that the two-nominee comparison "is the plurality". That is true:
- :537 covers states: all others together are below the larger nominee.
- :543 covers districts: the state's others together bound any one candidate's district vote.
The doc never says the guard is §723-A(2)'s majority test, so the finding argues against a claim the code does not make. The class already models Maine as plurality (:22-24) and lists ranked-choice tabulation as not modelled (:33).

What actually keeps the RCV contests correct is enforced twice:
- :558-:566 hold each state's split (at-large plus districts) to NARA's state row, so a year in which the rounds overturned the plurality leader cannot be written. :574 holds the national totals.
- GeneratedCatalogCheck.cs:575-583 runs Allocate(FromCatalog(y)) again and compares it with CastR+OthersRSlate and CastD+OthersDSlate.
In the catalog's actual RCV contests the leader has more than half the votes in each (ME 2020 D 435072/R 360737/others 23652; ME 2024 435652/377977/17746; AK 2024 R 184458 of 338177), so round one decided them anyway.

(c) Class doc :34-36. The finding's paraphrase drops "forever". The text reads: "a model that assumed plurality forever would be wrong, and this one does not assume it". FromCatalog throws for any year outside the catalog (:78-81), and the tool has proved each catalog year's plurality winners equal to NARA's table. So no unchecked cycle is ever read by plurality. "It simply takes the winner it is handed" refers to the allocator. The class summary edited in this same change (:11-13) names FromCatalog as what "hands it the elections of record". No sentence became false. At most the parenthetical is incomplete: it mentions Maine 2024 only, while FromCatalog also reads Maine 2020 by plurality, and Alaska 2024, which returns_2024.md:59 records as RCV (still marked UNCONFIRMED). CLAUDE.md's claim convention allows this ("no document becomes wrong - only incomplete"), and FromCatalog's doc points to the tool, whose header (:38-41) and president_returns.md:24-25 state both refusals.

The finding's fix also overstates one point. The tool holds each state's summed split to NARA, not each district's winner, because NARA prints no districts (:566).*
- [csharp] The C# comments cite COMPLETED.md section 786, which does not exist yet - *The facts are right, but nothing is wrong yet. Every pass looks like this while it is under review.

FACTS CONFIRMED
- COMPLETED.md is unchanged in both the index and the working tree. `git status` shows only ProjectSettings/ProjectAuditorSettings.asset as unstaged.
- Its last heading is `## 785.` at line 37454.
- `git grep --cached` finds the §786/s786 pointers at:
  - ElectoralCollege.cs:12
  - SeatAllocationBacktest.cs:33 and :183
  - GeneratedCatalogCheck.cs:125 and :517
  - Generated/UsPresidentialReturns.cs:9 (written by Tools/us_returns_prep.pl:625)
  - Tools/us_returns_prep.pl:2
  - ElectionsData/.gitattributes:4
  - district_method_2024.md:114, :137, :139
  - returns_2024.md:71, :73
  - state_ev_2024.csv:8
  - president_returns.md:3
  - maine_districts_read_2012_2016.tsv:1
  - USA_STAGE_PLAN.md:188 ("**Built: `COMPLETED.md` §786**")

WHY IT IS NOT A DEFECT
1. The record can only be written after the review. A § record holds the review's outcome and a "**Bars** (... on this commit's own tree)" line; §785 at COMPLETED.md:37454-37464 has both. The rules that require this:
   - CLAUDE.md: "RECORDS: ONE COMMIT PER PASS ... what changed · the evidence line · the commit"
   - Reviews/README.md: "what was done about its findings is told in COMPLETED.md"
2. This tree is not ready to commit yet. The ruling's "Done when" requires the cheap and simulation bars to be green. The change summary does not report them as run, and §786 is where their results will be recorded.
3. The convention holds in practice. 59 of the last 60 commits on main add to COMPLETED.md. The one that does not, 5f372d4c, only adds a .meta file as a §736 follow-up; it is not a new pass.
   - HEAD's plan lines 169 and 174 ("Built: `COMPLETED.md` §782" and "§783") followed the same pattern. Their sections landed in b183a332 and 7d71fcc4, the same commits that wrote those lines.
4. §786 is not claimed anywhere else. No branch has a `## 786.` heading: not main, wip/2026-10-02-c2b-polling-day, or any origin/* branch.
5. The same finding was refuted for §772, §780 and §783. See Reviews/2026-10-05_s783_us2_record_by_date.md:1549-1565.

CAVEAT
In the §783 case, ReviewLedgerCheck would have blocked the commit through SimulationManager.cs. Here, no staged file name matches the `$money` pattern in Tools/bar_tier.ps1 (I checked this case-insensitively). No check matches § pointers against COMPLETED.md headings either. So the only thing stopping a commit without §786 is the normal step of writing the record when the pass closes.*
- [record] The stage plan still says the allocator's only caller rebuilds 312–226 from typed arrays, and that the 2024 records have no raw bytes; this change made both false - *The facts the finding describes are right. At G:/UNITY/Projects/PoliSim/Assets/Editor/GeneratedCatalogCheck.cs:578 and SeatAllocationBacktest.cs:193-194, both callers now reach Allocate through FromCatalog, and the typed usIsR/usNames/usTotalEv arrays are deleted. Lines 97 and 106 of docs/specs/USA_STAGE_PLAN.md are unchanged; the staged diff touches only line 188.

The document already handles this, so nobody is misled:
1. Line 3 of USA_STAGE_PLAN.md, unchanged since the plan landed (cba1bd04, and the same at 5fe3c031), says: "The code facts in §2 are a reading of HEAD `5edca04` on 2026-10-04 (TRACKING: they go stale as PS-6 lands); this plan carries pointers, never line numbers or counts of code." Lines 97 (§2.2) and 106 (§2.3) sit inside that dated reading. Read as the document tells you to read them, they are still true: at 5edca04 the only caller was SeatAllocationBacktest, working from typed arrays.
2. The update the plan's convention owes is the item's own Built line, and this change adds it at line 188: "**Built: `COMPLETED.md` §786** - the catalog `UsPresidentialReturns` and its three CSVs ... `ElectoralCollege.FromCatalog` feeds the allocator, which `GeneratedCatalogCheck` runs over the four years".
3. This is established practice. US-1 (b183a332) and US-2 (7d71fcc4) edited only their own item paragraphs. §2.5 item 1 (line 123: the brief says "Polling day is 5 November 2024") has been stale since US-1, now that StartBrief.cs:115 has the NoElectionYet branch. §2.5 item 2 (line 124: "nothing seats the 119th Congress ... or the president of record") has been stale since US-2 added SeatTheRecordOnItsDate (SimulationManager.cs:475, :3994). Both commits passed two-pass reviews.
4. The reviews have ruled on this exact pattern:
   - Reviews/2026-10-05_s783_us2_record_by_date.md:951-967 refuted "[record] The stage plan still describes US-2 as a transition" because the item paragraph carries the Built marker. It also noted that the PS-6 row's stale "nothing built" was "not introduced by this diff".
   - Reviews/2026-10-04_s776_pl_declarations.md:1560 says: "TRACKING text that is incomplete because the work moved is exactly what the convention's test allows".
   - CLAUDE.md:37 says: "only incomplete, and only where TRACKING has genuinely moved."
5. Most of line 106 is still literally true. "filed 2026-08-28/29 from an agent's return with no raw bytes" is a past event. "12 states carry winner shares" and "no jurisdiction-by-candidate counts for all 51" still describe those three files: the 51-jurisdiction counts live in the new president_by_state.csv, not in them. The three files' own new "Raw pages (§786)" sections, and line 188's pointer to president_returns.md, carry the update.
6. The proposed fix would make things worse. Rewriting only lines 97 and 106 to today's state would make line 3's "a reading of HEAD 5edca04" false for those two lines, while §2.5's stale items stayed. §2 would become an inventory of mixed dates, which is worse than one consistently dated reading.*
- [record] DC's three electors are a typed constant, but the record presents them under 'DERIVED from the apportionment' - *The finding is refuted. The literal is real, but the label matches the ruling, the record names DC's source, and the figure is checked every year.

1. The literal is real, and so is the Senators' 2. Tools/us_returns_prep.pl:262 reads `sub electors { my ($year, $st) = @_; return 3 if $st eq 'DC'; return $reps{census_of($year)}{$st} + 2; }`. The two Senators are typed in the same way. If DC's 3 counts as "typed", so does the +2, and the ruling plainly accepts both.

2. The record's wording follows the ruling's. The ruling (docs/specs/USA_STAGE_PLAN.md:188) says: "the electors in force each year, derived from the apportionment (Senators plus Representatives; DC by the 23rd Amendment), not typed". Reading 4 has the same shape (ElectionsData/usa/president_returns.md:35): the heading "DERIVED from the apportionment", then in its body "plus two Senators each; the District of Columbia three (the Twenty-third Amendment, `raw/executive/archives_amendments_11-27.html`)". DC's basis is named in that sentence and is never credited to Table 1. The tool's header (lines 20-24) and the CSV header ("plus two (DC three), held to NARA's") say the same.

3. "Not typed" was aimed at the per-jurisdiction arrays this change retires. The staged diff of Assets/Editor/SeatAllocationBacktest.cs removes `int[] usTotalEv = { 9, 3, 11, 6, 54, ... }` and `bool[] usIsR`. Those per-state tables are now derived from Census Table 1.

4. DC's 3 is checked against two printed sources every year:
   - Lines 436-437: `my $el = electors($year, $s); problem("$year $s: NARA gives $ns->{electors} electors, the apportionment $el") unless $ns->{electors} == $el;`. This loops over @order, which includes DC (`'District of Columbia' => 'DC'` in %code). NARA's row regex accepts that name, and the table must hold 51 jurisdictions.
   - Line 412 checks 2024 against the FEC workbook's ELECTORAL VOTES column.
   - Line 427 requires each year's total to be 538.
   - A wrong DC figure would make the tool stop. The CSVs carry 2012/2016/2020/2024 DC rows of 3.

5. The fix's "derive it from the same Table 1" does not hold. Both Census tables say "The apportionment population excludes the population of the District of Columbia" (seen in the 2020 xlsx sharedStrings and the 2010 xls). So the amendment's "to which the District would be entitled if it were a State" cannot be read there. Taking the minimum over states of reps+2 gives 3 only with Art. I §2's floor of one Representative per State. That is a reasoning step the code would hold as a rule, just like +2. It would not be a reading.

6. DECLARED is the wrong label. The plan sorts premises as "DECLARED or ruled" (USA_STAGE_PLAN.md:137). DC's 3 is ruled (F8, "DC by the 23rd Amendment") and sourced: the amendment page is saved and registered in records_by_date.md as [CONST-AM]/[EX-AM], sha 95560fca.... The tool does not read the page, but reading 4 cites it.

The only fair point is editorial: the amendment's text does not say "three". It caps DC at the least populous State's electors. Nothing misleads a reader and no figure can come out wrong.*
- [record] 'No district table exists' for 2016 rests on a results page that links an 'Electoral College' document nobody opened - *The finding's premise is correct, but the scenario it depends on does not happen.

PREMISE (true): raw/district/maine_sos_election_results_2016-2017.html line 195 has `<h3>Electoral College</h3><ul><li><a href="/sos/sites/maine.gov.sos/files/content/assets/electoralcollegeinfo16.docx">Description of the Electoral College (Word)</a></li></ul>`. No fetch_log.txt names that file, neither under ElectionsData/usa/raw/ nor under PoliSim-captures/sources/usa_us3/, and no .md/.pl/.cs/.tsv mentions it.

SCENARIO (refuted): I fetched the .docx to my scratchpad (HTTP 200, 15275 bytes, sha256 ad5019a188925035ba512605fe799472cda1d00685c22b442dbc89b9a5fff14b, docProps created 2016-11-15). Its word/document.xml has 0 `<w:tbl>` elements. The only numbers in it are 2016, 8, 2nd, 19 and 1787. The text is "2016 Electoral College / Information for Maine / Prepared by the Office of the Secretary of State" followed by six paragraphs of description, e.g. "After the popular vote is cast November 8, 2016, the electors will meet ... The candidate who wins the most popular votes in the First Congressional District wins one elector" and "will meet December 19, 2016". It is almost word for word the 2012 page the record already saved (wayback_maine_sos_2012electoral_20150809.html). It holds no figures, so the READ basis is not superseded.

The 2012 page's Electoral College block has the same gap: "Description of the Electoral College" (no link) plus "List of Presidential Electors (pdf)" (presidentialelectorslist.pdf), which was not fetched either. I rendered that PDF through pdf.js. It is the "2012 STATE OF MAINE CERTIFICATION OF PRESIDENTIAL ELECTORS (21-A MRS §322)": each party's four elector nominees by district and at large, with names and towns and no votes.

The workbooks back the record's "no district column":
- president.xlsx (2016): its strings are county codes and towns, "STATE UOCAVA", "Total:" and "Grand Totals: (includes UOCAVA)", with no CG rows.
- The two 2012 .xls files: "<County> County Totals", "STATE UOCAVA", "TOTAL VOTES CAST", "Totals:", with no district rows.

So "The Secretary of State published no district table for either year" (president_returns.md:32; the TSV's header lines 1-2, which say "published", not "exists" as the finding quotes) holds against every item both results pages link for the presidential race and the Electoral College. The shorthand "no district table of either year exists" (us_returns_prep.pl:12, UsPresidentialReturns.cs:261-262, USA_STAGE_PLAN.md:188) is the same claim in context. It refers to the publisher's tables, and the only official district tabulation is the Governor's certificate, which is the source the record READs.

The tool's behaviour is unaffected: the READ rows are still held to the state totals and the overseas count.*
- [record] Every new pointer targets COMPLETED.md §786, which exists neither in the index nor in the working tree - *The facts in the finding are correct, but they describe the normal state of a pass while it is under review. They are not a defect in the change.

What I confirmed:
- COMPLETED.md is the same in HEAD, the index and the working tree (`git diff` and `git diff --cached` on it are both empty). It has 37464 lines, and its last heading is `## 785.` at line 37454. `git grep -e '§786' -e 's786' HEAD` finds nothing.
- The pointers are where the finding says:
  - GeneratedCatalogCheck.cs:125 and :517
  - SeatAllocationBacktest.cs:33 and :183
  - ElectoralCollege.cs:12
  - UsPresidentialReturns.cs:9 (written by us_returns_prep.pl:625)
  - us_returns_prep.pl:2
  - ElectionsData/.gitattributes:4
  - district_method_2024.md:114, :137 and :139
  - returns_2024.md:71 and :73
  - state_ev_2024.csv:8
  - the TSV, line 1
  - president_returns.md:3
  - USA_STAGE_PLAN.md:188 ("**Built: `COMPLETED.md` §786**")

Why it is not a defect:
1. **The record can only be written after this review and the bars.** CLAUDE.md:67 says a record is "what changed · the evidence line · the commit". Reviews/README.md says "what was done about its findings is told in COMPLETED.md". §783 (COMPLETED.md:37426 and :37431) has a "**The review** (`Reviews/2026-10-05_s783_...`)" block and a "**Bars** (... on this commit's own tree)" block. Neither can exist before this review runs. This review's own report (Reviews/2026-10-05_s786_*) is not in the tree yet either.
2. **Every pass commits its record together with the files that point at it.**
   - 5fe3c031 (s785): COMPLETED.md +12, alongside the code.
   - 7d71fcc4 (s783): COMPLETED.md +26, its Reviews/ report, and USA_STAGE_PLAN.md's "Built: `COMPLETED.md` §783".
   - b183a332 (s782): COMPLETED.md +22, and "Built: `COMPLETED.md` §782".
   - 1b8ba4ed and 99fdcf1c: the same pattern.
3. **No other work claims §786.** Nothing is untracked. The only unstaged change is ProjectSettings/ProjectAuditorSettings.asset.
4. **Nothing reads the record.** Every pointer is a comment or a line in a document. The tool, GeneratedCatalogCheck and the allocator never open COMPLETED.md.
5. **This exact finding has been refuted before** in Reviews/2026-10-04_s772_e1_factors.md:550, ..._s780_...:1547, ..._s783_...:964 and :1549, and ..._s784_...:1102. All of those reached the same verdict: "this is how every pass looks while it is under review".

Caveat (the same as at s784): nothing in the tooling enforces this.
- `.git/hooks` has no active hook.
- No staged file name matches bar_tier.ps1:37's `$money` pattern, so ReviewLedgerCheck gives no UNREVIEWED block.
- No check compares § pointers with COMPLETED.md's headings. DocumentClaimCheck reads only root *.md files and exempts COMPLETED.md.

So the only safeguard is the process step, and that step is the finding's own fix.*
- [data] The change cites COMPLETED.md §786 in many places, but no §786 exists and COMPLETED.md is not staged - *The facts in the finding are right, but the scenario it describes is how every pass looks while it is under review. Nothing in the change is wrong.

What I confirmed:
- COMPLETED.md's last heading is `## 785.` at line 37454 (37464 lines). It is unchanged against HEAD and not staged. `git status` shows only ProjectSettings/ProjectAuditorSettings.asset as an unstaged change.
- `git grep --cached` finds §786/s786 cited in 12 staged files, which is more than the finding lists:
  - GeneratedCatalogCheck.cs:125 and :517
  - SeatAllocationBacktest.cs:33 and :183
  - ElectoralCollege.cs:12
  - UsPresidentialReturns.cs:9
  - ElectionsData/.gitattributes:4
  - district_method_2024.md:114, :137 and :139
  - maine_districts_read_2012_2016.tsv:1
  - president_returns.md:3
  - returns_2024.md:71 and :73
  - state_ev_2024.csv:8
  - us_returns_prep.pl:2 and :625
  - USA_STAGE_PLAN.md:188
- §786 is not taken by any other work. There is no stash, and the only wip branch is 2026-10-02-c2b.

Why it is not a defect:
1. The record has to come after this review. CLAUDE.md:67 says "RECORDS: ONE COMMIT PER PASS ... a § record is what changed · the evidence line · the commit". The §785 record (COMPLETED.md:37454-37464) carries "**Not reviewed by the workflow:**" and "**Bars** (tier UI, on this commit's own tree): ...". Neither block can be written until the review and the bars have run. The memory note says to append the record before the final cheap bar, so that MojibakeCheck scans it.
2. Every recent commit adds its record in the same commit. `git show --stat` shows COMPLETED.md in each of 5fe3c031 (+12), 1b8ba4ed (+21), 7d71fcc4 (+26), b183a332 (+22), 99fdcf1c (+14), cd1fb480 (+48), d00b66aa (+31), f03784e7 (+50) and cba1bd04 (+49).
3. This exact finding has been refuted before. Reviews/2026-10-05_s783_us2_record_by_date.md:1549-1565 says "nothing is wrong yet. This is how every pass looks while it is under review". It also cites earlier refutations in s772 (:550) and s780 (:1547). In each case the commit then landed with its section; for example, 7d71fcc4 holds `## 783.` at line 37407.

One caveat, which is why this was graded down rather than refuted outright:
- No check matches § pointers against COMPLETED.md's headings. The s783 verifier noted the same thing.
- Unlike s783, this change has no money path. I ran every staged name against bar_tier.ps1's `$money` regex and none matched. So ReviewLedgerCheck would not block a commit made before the record is written.
- The only safeguard is the process step, which the last nine commits all took. That is a process reminder, not a fault in the staged code or data.*

## What was done about the first pass

No defect survived the skeptics (8 minor, 16 notes); every finding was acted on.

- **The READ rows (1, 3, 4, 5, 6, 24).** The tool now holds every Maine 2012 and 2016 READ figure to the certificate's own OCR text layer (`pdftotext -raw`): each Democratic and Republican slate's headings First, Second, At-Large, At-Large and its four figures in that order, split digit groups joined; the at-large figures the FEC's state figure; the cited page the figure's page; the overseas count every footnote's. The TSV's certificate column must be that year's certificate and its page a positive integer; an unallocated row needs district "-" and may not repeat; every (year, district, party) cell must be READ. All ten READ values agree with the text layer.
- **District identity (1, 5).** NARA's notes name the district each ticket carried wherever a state split (Maine 2016, 2020, 2024; Nebraska 2020, 2024); the tool parses them and requires each district's winner, and that every district against its state is named; with no note, no district may go against its state.
- **The 2012 formula cells (2).** Both readers record formula cells; a Total-row cell the tool reads that a formula fills must have a typed counterpart - 2012's state rows are held to Table 1 of the same workbook (typed nominee and Total rows); a formula anywhere else in a Total row fails.
- **`state_ev_2024.csv` (7).** Held line for line: the exact heading, each line's whole text as the 2024 rows give it, each jurisdiction once and none missing.
- **Maine's ranked-choice count (8).** For 2020 and 2024 each leader, by district and statewide, must hold more than half of the workbooks' TBC column; `ElectoralCollege`'s parenthetical and reading 8 say so.
- **Writes and warnings (9).** All four outputs are built and ASCII-tested before the first is written, every print and close checked; a perl warning during the checks is a named mismatch; a name bound for a C# literal may not hold a quote or backslash.
- **The comments (10, 14).** `ElectoralCollege.cs` and `SeatAllocationBacktest.cs` point at `UsPresidentialReturns.Years` instead of naming four years; the generated summary's year list is built from `@years`.
- **The records (11-13, 15-23).** Reading 11 gives each Maine section's whole history, with the citation-year and subsection-tag cautions (11, 23); the hearing's testimony cited to the saved hearing page (12); Nebraska's 2026 elections page saved and FindLaw named as not fetched (13); "no connection (curl status 000)" for 2026-10-05 (15); the California dash and the FEC's double space listed (16); the FEC redirect stated from the log, with a NOTE line in `raw/returns/fetch_log.txt` (17); readings 9 and 10 reworded (18); the Maine 2012 certificate's title (the *amended* certificate, given 20 December 2012) and the Nebraska 2012 PDF's own title in the register (19); LB3's fourth section, §32-713 (21); "re-hashes the pages RawSources lists" and "compiled", not "generated" (22).
- **The faithless electors (20).** The Supreme Court's *Chiafalo v. Washington* saved and registered; it names the three Washington electors who voted for Colin Powell; reading 3 no longer reads the done-when for Elias but puts it to him, OPEN, with options.
- **Proof.** A mutation suite of twenty-five cases (the first run's nine and sixteen from this pass) caught every one, the control reproducing the repository's bytes.

## The second pass - confirmed (verbatim)

The same workflow (run wf_f234a635-5b3, 35 agents) over the staged diff after the first pass's fixes: four lenses (the new checks, the rewritten record, the first pass closed or not, the C# side), every finding put to a refute-first skeptic. Every report below is verbatim.

### 1. The ranked-choice test has no lower bound: a missing, zero or too-small TBC passes

- **Lens:** newchecks - **reviewer:** minor - **skeptic:** minor
- **Where:** Tools/us_returns_prep.pl:549

**The scenario.** Every CG Total row's TBC is a formula cell (2020 shared formula, 2024 SUM(D:J)), and count() reads an empty cell as 0 (line 237). On a scratch copy I deleted the cached <v>380324</v> of the 2020 CG2!K427 cell and re-pinned the workbook: exit 0, all four files written, because 2*196692 > 0. Setting it to <v>1000</v> also gave exit 0. Only the upward case fails (R14, and my 400000 control). So a re-fetched workbook saved without cached values, or a TBC column holding the wrong figure, passes. That covers the case the header (lines 37-38), president_returns.md's 'What the tool proves' and ElectoralCollege.cs:35-36 ('requires every Maine leader ... to hold more than half of all ballots cast') present as guaranteed.

**The fix proposed.** Refuse a TBC below its row's two nominees, or one not equal to the sum of the row's columns D..J (true on all 1,070 data rows of the four sheets). Hold the statewide TBC minus the CG rows' Blank to the FEC's typed state total (equal in both years: 819,461 and 831,375); that is the typed counterpart fix (4) asks for when a check leans on a formula cell. Add the downward case to us3_mutate.sh.

**The skeptic's evidence.** The gap is real. It shows that first-pass finding 8's fix (staged fix 3) is incomplete: the check has no lower bound. One half of the finding's scenario is wrong, and no wrong output can follow.

**The code.**
- Tools/us_returns_prep.pl:533-536 reads `tbc => count($c->{$tr[0]}{$tbc}, ...)`.
- TBC is used only at :549 `unless 2 * $lead > $d->{tbc}` and :553 `unless 2 * $lead > $tbc`. A grep finds no other use, it is in no CSV or UsPresidentialReturns.cs, and no C# file mentions it.
- xlsx_sheet:117 skips a cell that has no `<v>` (`next unless defined $v;`). count():233-237 then turns the undef into 0 (`$v // ''`, then `return 0 if $s eq ''`). No perl warning is raised, so the $SIG{__WARN__} net at :65 ("an undefined value reaching a check is a mismatch, not a passing 0") never fires.
- %formula_at records the TBC cell as a formula, but the Maine block never consults it.

**The raw XML.** Every CG Total-row cell from D to K is a formula with a cached value:
- 2020 K135 is `<f t="shared" si="6"/><v>447981</v>`, from the shared formula D32+D47+...
- 2020 K427 is `<f t="shared" si="11"/><v>380324</v>`.
- 2024 K133 is `<f>SUM(K32,K45,K65,K86,K98,K129,K131)</f>`, and K425 is the same kind of shared SUM.
- The finding's "2024 SUM(D:J)" is the town rows' formula (K4 `SUM(D4:J4)`). The Total rows' TBC sums the county subtotals' TBC cells.

**Reproduced on a copy of the inputs.**
- Control: exit 0, and the outputs are the repository's bytes.
- M1, 2020 CG2 `<v>380324</v>` deleted and re-pinned: exit 0, all four files written. Only the .cs differs, through the page's hash.
- M2, the same cell set to `<v>1000</v>`: exit 0, four files written.
- M3, set to `<v>400000</v>` (the R14 case): caught.

**What is refuted.** "A re-fetched workbook saved without cached values ... passes" is wrong. The Total row's D and R cells are formulas too. Stripping every formula cell's cached value (152 cells in 2020, 666 in 2024) gives exit 255 with 12 mismatches. The first is "ME 2020: the districts give D 0 R 0, the FEC's state row D 435072 R 360737". Only a TBC value missing on its own, or a TBC that is too small, passes.

**Why minor, not defect.**
- A ranked-choice count that changed any winner would still be refused by NARA's state split and by NARA's district notes (fix 2).
- A wrong TBC can only let through a contest that went to rounds and was still won by its first-round leader. The catalog rows would still be right.
- The pinned TBCs are correct. On all 1,070 rows that carry a TBC figure (125+412+123+410), TBC equals the sum of columns D..J, the four Total rows among them.
- CG1+CG2 TBC minus Blank is 828305-8844=819461 (2020) and 842447-11072=831375 (2024). Both equal the FEC state-row totals, which are typed cells (2020 Table 2 G24 and 2024 AE21 carry no `<f>`). They also agree column by column, e.g. 2024 Oliver 2802+2484 = Q21 5286.

**Why it stands.**
- Three places present the check as a guarantee: the header at :37-38, president_returns.md:23 ('What the tool proves') with reading 8, and ElectoralCollege.cs:35-36 ("requires every Maine leader ... to hold more than half of all ballots cast").
- The record's "25 mutations" include only the upward case.
- TBC is the only Maine figure read from a formula cell with no typed counterpart, which is the rule fix (4) installs, and the FEC types one.

**The skeptic's corrected fix.** In the Maine 2020/2024 loop (Tools/us_returns_prep.pl:523-554):
(1) Refuse an empty TBC cell before count() turns it into 0 (test `defined $c->{$tr[0]}{$tbc}`).
(2) Hold each CG Total row's TBC to the sum of every column between the municipality column and the TBC column, found by their headings (today D..J: the candidates, Others and Blank). This is exact on all 1,070 TBC rows of the pinned sheets.
(3) Find the Blank column by its row-1 heading, matched case-insensitively ('Blank' in 2020, 'BLANK' in 2024). Hold CG1+CG2 TBC minus CG1+CG2 Blank to `$y{$year}{st}{ME}{total}`, the FEC's typed state total. That is fix (4)'s typed counterpart for a formula-filled Total-row cell.
(4) Add both refusals to the header's failure list, with no figures transcribed.
(5) Add the downward cases to us3_mutate.sh: 2020 CG2 `<v>380324</v>` -> '' and -> `<v>1000</v>`, each expecting the new problem text. Update the record's mutation count and list to match.

A floor on its own (TBC >= D+R) would catch M1 and M2, but not a TBC understated by less than the other candidates' votes. The two equalities are stronger and hold today.

Correct the finding's own text in two places. The 'saved without cached values' sub-case is already caught, with 12 mismatches. 'SUM(D:J)' is the town rows' formula; the Total rows' TBC sums the county subtotals' TBC cells.

### 2. 'TBC, blank and overvoted ballots counted' is an undeclared premise: the workbook's TBC is SUM(candidates, Others, Blank)

- **Lens:** newchecks - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_returns_prep.pl:543

**The scenario.** The saved s723-A(2) counts ballots 'on which there is an overvote at ranking number one' in the denominator. In both workbooks TBC equals D..J on every row (2024's cells carry SUM(D4:J4)), and there is no overvote column. No saved page says Maine's election-night 'Blank' includes overvoted ballots: maine_sos_rcv.html calls a ballot with no first choice 'blank' and keeps overvotes as a separate category in its RCV tables. If overvotes sit outside Blank, the tool's denominator is smaller than the statute's, so the test is weaker than line 543 and reading 8 (president_returns.md:42) say. This does not matter at today's margins: ME-2 2020 would need more than 13,060 overvotes.

**The fix proposed.** Say what TBC is (the sum of the candidate, Others and Blank columns) and DECLARE the reading that Blank holds overvoted ballots, or drop 'and overvoted' from line 543 and reading 8.

**The skeptic's evidence.** CLAIM. Tools/us_returns_prep.pl:542-544 says: "a leader holding more than half of ALL ballots cast (the workbook's TBC, blank and overvoted ballots counted) wins outright by s723-A(2)". Reading 8 (president_returns.md:42) says the statute counts "blank and overvoted ballots", then that the tool requires this "(the district workbooks' TBC column)". The code: :533-536 finds 'TBC' by its row-1 heading; :549 and :553 test `2 * $lead > $d->{tbc}` and `2 * $lead > $tbc`.

STATUTE. The saved raw/district/maine_legislature_21a_723-A.html §723-A(2) reads: "more than 50% of all ballots cast for the particular office ... including but not limited to ballots on which ranking number one is blank, on which there is an overvote at ranking number one or on which ranking number one was assigned to an excluded candidate". So overvoted ballots are in the statute's denominator.

WORKBOOKS. I read them with the tool's own XML approach.
- In both years, row 1 is: D-H candidates, I=Others, J=Blank (BLANK in 2024), K=TBC. There is no overvote column.
- K equals the sum of D..J on every row: 2020 has 537 rows (518 typed, 19 formula) and 0 mismatches; 2024 has 533 rows, every one a formula such as K4 `SUM(D4:J4)`, and 0 mismatches.
- So TBC includes overvoted ballots only if Blank does.

NOTHING ON DISK SAYS BLANK HOLDS OVERVOTES.
- The workbooks' shared strings carry no note.
- Only two files in ElectionsData/usa/raw mention overvotes: the statute and maine_sos_rcv.html. That page says "if a voter did not mark any candidate as their first choice, their ballot was counted as blank on election night". Its RCV grid keeps "Ballot Exhausted by Overvotes" apart from "...by Undervotes".
- The out-of-tree Maine 2020 and 2024 certificates of ascertainment say nothing about blanks or overvotes.
- The UF file's Maine figure, TOTAL_BALLOTS_COUNTED 842447, is exactly 439574 + 402873, the two TBC totals, and it cites maine.gov's results page. It is the same number, not independent evidence.

SAME ASSUMPTION ELSEWHERE. district_method_2024.md:86-87 makes the same claim; it was there before this change ("all ballots cast *including blanks and overvotes*, and Harris took 435,652 of 842,447 ballots"). ElectoralCollege.cs:36 and president_returns.md:23 say "all ballots cast".

WHY IT IS NOT A DEFECT.
- No output can be wrong: the tool holds each state's split to NARA's table, and the closest contest has room to spare.
- ME-2 2020: 2×196692 − 380324 = 13060. The test fails only at X ≥ 13060 overvotes outside TBC, so the finding's "more than 13,060" is off by one.
- Without the premise, the test still gives the leader a majority of round-one continuing ballots (§723-A(1)(D) exhausts a ballot overvoted at rank one). But read literally, the statute lets such a ballot continue once both of its rank-one candidates are removed. So the "wins outright" claim needs the premise, or a bound on overvotes.

FIRST PASS. Finding 8 measured "all ballots cast" on TBC without asking what TBC holds. Fix (3) therefore asserted the assumption as fact. This finding is new.

**The skeptic's corrected fix.** Keep reading 8's first sentence: "blank and overvoted ballots counted" is §723-A(2)'s own denominator, and dropping "and overvoted" there would misstate the statute. The finding's second option is wrong for reading 8, and on its own it is not enough at :543, because "wins outright" would still be unproven.

Fix the step that equates TBC with "all ballots cast" instead. Words only, no code change:
1. At us_returns_prep.pl:542-544, say that TBC is each row's sum of the candidate, Others and Blank columns. 2024's cells are that formula, and there is no overvote column. Then DECLARE the reading that the workbook's Blank holds ballots overvoted at ranking one, so that TBC is the statute's "all ballots cast". The Secretary of State's RCV page defines "blank" only as a ballot with no first choice.
2. Put the same DECLARED wording in reading 8's second sentence.
3. Put it where district_method_2024.md:86-87 equates the TBC sum with "all ballots cast including blanks and overvotes".
4. Optionally add that, on any reading, the outcome is held independently by NARA's per-state split.

### 3. First-pass finding 1's fix is incomplete: Nebraska 2020/2024 districts 1 and 3 (both Republican) can trade figures, and reading 9 says split years are held

- **Lens:** newchecks - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/president_returns.md:43

**The scenario.** I used a pdftotext wrapper in R12's style that exchanges the 'Congressional District 1' and '3' heading lines. Result: exit 0, four files written, and president_by_district.csv got 2020,NE,1,65854,222179 and 2020,NE,3,132261,180290 (2024 likewise: NE-1 70301/238245). NARA's notes give 'the First and Third' together to the Republican, so they bind only NE-2. Reading 9 ('Where Nebraska split (2020, 2024) NARA's notes name which ticket carried which district and the tool holds the districts to them; where it did not (2012, 2016), ... rests on the book's headings alone') and the tool comment at Tools/us_returns_prep.pl:664-666 both imply that split years are fully held. The first pass's skeptic (finding 1, point 3) asked the record to say that 'transpositions that keep the winners' stay uncovered. The fix said this only for the years with no note.

**The fix proposed.** In reading 9 and at lines 664-666, say that NE-1 and NE-3 rest on the book's headings in every year. Alternatively, bind them the way Maine's READ rows are bound: NARA's Nebraska certificates of ascertainment (kept out of tree) print each slate's 'First Congressional District Total Votes Received: 180,290' and so on in their text layer.

**The skeptic's evidence.** I could not refute the mechanism, and I reproduced it.

How the code works (Tools/us_returns_prep.pl, staged; the working tree equals the index):
- Line 512 sets $cd from the heading's number only. Line 514 binds the next "Total" line to that number.
- Lines 358-362 parse NARA's note into { took => R, R => [1,3], D => [2] }. NARA's 2020 and 2024 pages say "Trump-Pence [Trump-Vance] won in the First and Third Congressional Districts and took the state; Biden-Harris [Harris-Walz] won the Second Congressional District."
- Lines 676-680 check only each listed district's winner, and that the districts against the state equal the named set.
- Line 697 counts winners.
- Lines 517 and 521 check the count and the sums.
- An exchange of district 1 and district 3 keeps R, D, R, so it passes all of these.

The run: I copied the inputs to a scratch directory. The control exited 0 and its outputs match the repo's bytes. I then added a pdftotext wrapper in the style of the suite's R12 that exchanges '^Congressional District 1' and '3'. The tool exited 0 and wrote all four files. The district CSV changed to:
- 2020,NE,1,65854,222179
- 2020,NE,3,132261,180290
- 2024,NE,1,70301,238245
- 2024,NE,3,136153,177666

R12 swapped districts 1 and 2, which flips the winners, so it was caught.

What the record says:
- Reading 9 (line 43): "Where Nebraska split (2020, 2024) NARA's notes name which ticket carried which district and the tool holds the districts to them; where it did not (2012, 2016), which figures stand under which district label rests on the book's headings alone." Taken literally this is true. But the contrast through "alone" implies that in the split years the binding rests on more than the headings, and for NE-1 and NE-3 it does not.
- Line 31 lists "Nebraska 2020's district headings exchanged by the text extractor" among the mutations caught, without saying it was 1 and 2.

First-pass finding 1 (the skeptic's corrected fix, point 3) asked the record to name "transpositions that keep the winners" as uncovered. Since then:
- The Maine READ rows are held to the text layer (R2 catches a swap that keeps the winners).
- Maine 2020 and 2024 have two districts with different winners, plus sheet and row labels.
- So NE-1 and NE-3 in 2020 and 2024 is the only uncovered transposition that keeps the winners, and the record does not say so. The first pass's fix is incomplete.

Why this is a note and not a defect:
- On the saved, SHA-pinned books under xpdf 4.06, the binding is correct. The control rows match the text layer of NARA's Nebraska certificates, which are kept out of tree. For 2020 they read "First Congressional District Total Votes Received: 180,290" for the R slate and 132,261 for the D slate, and Third 222,179 and 65,854. For 2024, First is 177,666 and 136,153, and Third is 238,245 and 70,301.
- ElectoralCollege.FromCatalog only counts district winners, so nothing in play reads district identity.

The tool comment at 664-666 adds little. Its "where it did not" clause even names "the workbook's sheet names", which fits no non-split year, because both Maine workbook years (2020 and 2024) split. Reading 9 is the claim that matters.

**The skeptic's corrected fix.** Change only the record's words, with no figures (the claim convention).
1. In reading 9, replace the split-year sentence with: "Where Nebraska split (2020, 2024), NARA's notes name which ticket carried which district, and the tool holds each district to its ticket. That binds the Second. NARA names the First and Third together, both the Republican's, so which of those two stands under which label rests on the book's headings alone. In 2012 and 2016 every district's label rests on the headings alone."
2. At line 31, make the mutation item specific: "Nebraska 2020's First and Second district headings exchanged by the text extractor (the winners flip)."
3. Optionally reword the comment at Tools/us_returns_prep.pl:665-666 to match: "where a note names two districts for one ticket (Nebraska's First and Third) or the state did not split, which figures stand under which label rests on the source's own labels".

Optional hardening, beyond what the ruling asks: bring NARA's Nebraska 2020 and 2024 certificates in tree and register them. Hold each slate's 'First/Second/Third Congressional District Total Votes Received' lines (pdftotext -raw) to the canvass rows, as the Maine READ rows are held to their certificates.

### 4. Mutations 1 and 2 no longer pin the shortfall bound they are named for

- **Lens:** newchecks - **reviewer:** note - **skeptic:** note
- **Where:** C:/Users/elias/AppData/Local/Temp/claude/C--Users-elias/c8a2f04f-adf9-462b-8470-14c260fdbdb5/scratchpad/us3_mutate.sh:28

**The scenario.** Both cases now expect 'the certificate's text layer', which any change to a TSV figure trips. In a copy with the bound's two problem() calls disabled (Tools/us_returns_prep.pl:640 and 643 turned into `0 && problem(`), M1 and M2 still print the expected string (exit 255, 'ME 2012 1 R READ 152937, the certificate's text layer 142937'), so the suite would still report 25/25. The bound can fire only when the text layer and the TSV agree on a figure. A wrapper printing 323,035 for Diane Denk, with the TSV also at 323035, gives 'ME 2012 D: the districts exceed the state by 99727' on its own, but no case in the suite does this. president_returns.md:31 still lists 'a READ figure raised past the state; one lowered past the overseas count' among what the suite proved.

**The fix proposed.** Expect 'exceed the state' in M1 and 'short of the state' in M2; both lines are printed today. Or add a wrapper case in R12's style that changes one figure in the text layer and in the TSV together and expects the bound's message.

**The skeptic's evidence.** I could not refute it. Every claim in the finding reproduced on private copies. The repo was not touched, and its working tree still equals the index.

The code:
- us3_mutate.sh:28-29: M1 and M2 both expect "the certificate's text layer".
- us_returns_prep.pl:626: `problem(... "READ $dist{..}, the certificate's text layer $fig") unless $fig == $dist{$year}{ME}{$cd}{$p};` This fires on any change to a district figure in the TSV.
- The bound is lines 640 and 643. Line 640: `problem("ME $year $p: the districts exceed the state by " . -$g) if $g < 0;`. Line 643: `problem("ME $year: the nominees' districts fall $gap short of the state, more than the certificate's $unalloc{$year} unallocated") if $gap > $unalloc{$year};`.
- `problem()` (line 64) only collects messages, and line 742 prints them all.
- None of the 25 expected strings is either bound message.

What I ran (scripts in the scratchpad: sk786_bound_probe.sh, sk786_mutate_nobound.sh, sk786_nobound.pl):
1. **M1 as the suite runs it:** exit 255, nothing written. It printed two lines: "...ME 2012 1 R READ 152937, the certificate's text layer 142937" and "ME 2012 R: the districts exceed the state by 9876".
2. **M2 as the suite runs it:** exit 255. It printed the text-layer line and "ME 2016: the nominees' districts fall 376 short of the state, more than the certificate's 184 unallocated". So both bound lines are printed today; the suite just doesn't require them.
3. **Lines 640 and 643 turned into `0 && problem(`** (both edits confirmed): M1 and M2 still exit 255, write nothing and print the text-layer line, so they count as CAUGHT.
4. **The full suite with the bound disabled in every copy:** "caught 25, missed or broken 0". The control still reproduced the repo's bytes.
5. **The bound is live and can be the only guard.** I used a pdftotext wrapper printing "Diane Denk, Kennebunk 323,035" and set the TSV to 2012 ME 1 D 323035.
   - With the bound: exit 255 and exactly one MISMATCH, "ME 2012 D: the districts exceed the state by 99727".
   - Without the bound: exit 0, all four files written, and president_by_district.csv carries `2012,ME,1,323035,142937,READ`.

Why it stays a note:
- The tool works today.
- president_returns.md:31 is literally true: the tool fails on both mutations and names them.
- The suite is a scratch file run by hand.
- What is lost is the suite's evidence for line 24's claim ("they never exceed the state nor fall short of it by more than the overseas votes"). A regression of the bound would also pass the suite unseen.

This is not in the first pass. Its #3, #4 and #24 are about how loose the bound is and where the count comes from, not about the suite's expected strings. Those strings changed along with fix (1).

**The skeptic's corrected fix.** Change the expected string at us3_mutate.sh:28 (M1) to "exceed the state" and at :29 (M2) to "short of the state". Both are printed today, and both disappear when lines 640 and 643 are disabled, so M1 and M2 would then report MISSED. No coverage of the text-layer check is lost: R1-R3 still pin it. R3 (+200 on 2012 D1) leaves the D shortfall at 73 and the total at 197 of 415, so only line 626 fires there.

Optionally, add a wrapper case in R12's style that changes the text layer and the TSV together. The 2012 Diane Denk figure 223,035 to 323,035 in both, expecting "exceed the state by 99727", is the case where the bound alone stops a wrong figure reaching the catalog. If the record is touched, it can keep its line 31 wording as is.

### 5. Mutation 9 changes Alaska's row, not a Maine elector

- **Lens:** newchecks - **reviewer:** note - **skeptic:** note
- **Where:** C:/Users/elias/AppData/Local/Temp/claude/C--Users-elias/c8a2f04f-adf9-462b-8470-14c260fdbdb5/scratchpad/us3_mutate.sh:36

**The scenario.** The regex anchors on the page's first 'Maine', which is in the Notes paragraph above the table ('Both Maine and Nebraska split their electoral votes'). The lazy .*? runs on into the table and changes Alaska's Trump cell (raw/executive/archives_electoral_college_2024.html line 420) from 3 to 2. The run is caught by '2024 AK: the record's winners give R 3, NARA's table 2', so the suite counts it. But 'an elector moved between parties on NARA's table' (the label, and president_returns.md:31) was never run. A real move on Maine's row (3/1 to 2/2) is also caught: '2024 ME: cast D 3 R 1 by the FEC, 2 and 2 by NARA' plus both split lines. Smaller point: R12 reaches the 2024 book as well as 2020's, not only 2020's as its label says.

**The fix proposed.** Anchor the regex on Maine's row (`ascertainment-maine\.pdf">Maine</a>`) and change both For President cells. Or relabel the case as an elector dropped from Alaska's row, and correct the record's list.

**The skeptic's evidence.** I could not refute this. I reproduced it on scratch copies, outside the repo, and each control gave the repository's four output files byte for byte.

1. **Where the regex lands.** Line 36 of us3_mutate.sh runs `s{(Maine.*?</td>\s*<td[^>]*>.*?</td>\s*<td[^>]*>\s*(?:<p>)?)3}{${1}2}s`. The first 'Maine' on raw/executive/archives_electoral_college_2024.html is at line 364, in the Notes paragraph (`<p>Both Maine and Nebraska split...`). Maine's own row is at line 561. I applied the substitution to a copy and diffed it against the original. The only change is line 420, `<td align="right">3</td>` becoming `2`. Lines 417-422 are Alaska's row (State, EV, Harris '-', Trump 3, Walz '-', Vance 3), so the mutation drops one elector from Alaska's Trump cell. No elector moves between parties. Anchored on Maine's row, the same pattern would still change only the Harris cell (3 to 2), so even then it would drop an elector rather than move one.

2. **What the tool says about that run.** I resummed the page as the suite does and ran the tool. It exited 255 and wrote nothing:
   - `MISMATCH: 2024 AK: cast D 0 R 3 by the FEC, 0 and 2 by NARA`
   - `MISMATCH: 2024 AK: the record's winners give R 3, NARA's table 2 (2 cast, 0 for other persons)`

   The suite's expected text is "NARA's table", which the second line contains, so it counts the case as CAUGHT.

3. **The record's claim.** ElectionsData/usa/president_returns.md:31 is new in this change (the first-pass record listed only three mutations). It says that on 2026-10-05 the tool failed on "an elector moved between parties on NARA's table". That mutation was never run.

4. **The property still holds.** I ran a real move on Maine's row, Harris 3/Trump 1 changed to 2/2 in both For President cells (lines 563-564). The tool exited 255, wrote nothing and named:
   - `2024 ME: cast D 3 R 1 by the FEC, 2 and 2 by NARA`
   - `2024 ME: the record's winners give D 3, NARA's table 2 (2 cast, 0 for other persons)`
   - `2024 ME: the record's winners give R 1, NARA's table 2 (2 cast, 0 for other persons)`

   So the tool is sound. Only the description of one measured case is wrong.

5. **The R12 point is confirmed and harmless.** The wrapper's case-sensitive `^Congressional District 1\b` rewrites the 2020 and 2024 books. It leaves 2012 ('Congressional District One') and 2016 ('Congressional District 01') alone, and the Maine certificates are also unchanged. The run named three 2020 lines and three 2024 lines (NE-2, NE-1, "districts against the state are '1', NARA's note names '2'"). The 2020 swap is therefore caught on its own lines, and the label is only narrower than what was run.

6. **Not a first-pass item.** The first-pass report has no mention of this. The NARA page dates from 8efdec1f and is untouched by this change, so the earlier runs of mutation 9 also hit Alaska.

**Grade: note.** No code, data or catalog figure is wrong. A dated measurement record misdescribes one of its 25 cases, and the case it describes does fail as claimed.

**The skeptic's corrected fix.** Prefer fixing the suite over relabelling: then the record's sentence becomes true as written, and the count stays 25 of 25. In us3_mutate.sh line 36, anchor on Maine's row and move one elector by changing both For President cells:

`f=ElectionsData/usa/raw/executive/archives_electoral_college_2024.html; perl -0777 -i -pe 's{(ascertainment-maine\.pdf">Maine</a>.*?</td>\s*<td[^>]*>.*?</td>\s*<td[^>]*>)3(</td>\s*<td[^>]*>)1(</td>)}{${1}2${2}2${3}}s' $f; resum $f`

Set the expected text to `2024 ME: cast D 3 R 1 by the FEC, 2 and 2 by NARA`. I measured this exact case: exit 255, three mismatches, nothing written.

Harden the suite so a mutation that lands in the wrong place reports as BROKEN instead of counting as CAUGHT. For example, after each page edit, require that the intended text changed (here `grep -A3 'ascertainment-maine.pdf' $f` shows 2/2), or diff against the source and require exactly the expected line numbers.

If the suite is not changed, relabel case 9 as "an elector dropped from Alaska's Trump cell on NARA's table" in both the suite and president_returns.md:31.

Optional: relabel R12 as "Nebraska 2020's and 2024's district headings exchanged by the text extractor", in the suite's label and in the record. Its 2020 lines are already named on their own, so this is wording only.

### 6. The READ check makes the extractor's page breaks load-bearing; an extractor without them fails right inputs and blames the TSV

- **Lens:** newchecks - **reviewer:** note - **skeptic:** note
- **Where:** Tools/us_returns_prep.pl:603

**The scenario.** Page numbers come only from the \f in pdftotext's output. The same xpdf 4.06 run with -nopgbrk fails the unmutated inputs with 10 mismatches. An xpdfrc with 'textPageBreaks no' would do the same, because the tool passes no -cfg. The messages include 'ME 2012 1 D cites page 3, the figure is on page 1' and 'ME 2012 unallocated cites page 2, which carries no footnote', so the transcription is blamed for an extractor difference. Line 62 falls back to whatever pdftotext is on PATH, nothing records which one ran, and reading 1 names no version (reading 9 names xpdf 4.06). The failure is closed: no wrong figure can pass this way. Before the fix, only Nebraska depended on the extractor, and not on page breaks.

**The fix proposed.** Refuse a certificate text layer that has no form feed, with a message naming the extractor; the TSV cites pages up to 3, so a one-page layer is wrong. Print `pdftotext -v` with the summary, and name xpdf 4.06 in reading 1.

**The skeptic's evidence.** The mechanism is real and I reproduced the failing path. It fails closed, it needs a config file that does not exist on this machine today, and the cost is a message that blames the wrong file. Note is the right grade.

Code, Tools/us_returns_prep.pl:
- Line 597: `open my $pt, '-|', $pdftotext, '-raw', "$raw/$cert", '-'`. No -cfg is passed, so xpdf reads whatever xpdfrc it finds.
- Lines 601-603: `my $pageno = 1; for my $line (@l) { $pageno += ($line =~ tr/\f//);`. Form feeds are the only source of page numbers.
- Line 626 (`$read_tsv: ME $year $cd $p cites page ..., the figure is on page $pg`) and line 633 (`... unallocated cites page ..., which carries no footnote`) both start with the TSV's path.
- Line 62 falls back to PATH. Neither summary printf (lines 811, 814) nor RawSources records which extractor ran.
- The TSV cites only pages 2 and 3.

Reproduction, on a copy of the inputs in my scratchpad (removed afterwards):
- Control: exit 0, the repo's bytes (district d2a5038b, state 8e528467, year a9f2a47b, .cs 9a73695f).
- I copied /mingw64/bin/pdftotext.exe (xpdf 4.06) next to an `xpdfrc` file holding `textPageBreaks no`, and pointed the copy's line 62 at it. Result: exit 255, "10 mismatch(es) - nothing written". All ten name the TSV, for example "maine_districts_read_2012_2016.tsv: ME 2012 1 D cites page 3, the figure is on page 1" and "ME 2016 unallocated cites page 2, which carries no footnote". Nebraska passes.
- That text is byte-identical to the default text with its form feeds removed (2012 has 4, 2016 has 5), and to the `-nopgbrk` output. So page numbering is the only thing that changes.

Reachability today:
- No xpdfrc exists in C:\Program Files\Git\mingw64\bin or in the profile. AppData\Roaming\xpdf exists but is empty.
- Setting HOME or USERPROFILE to a folder holding an xpdfrc changed nothing. The failure takes a config file placed by hand.

Records: reading 1 (president_returns.md:35) says only `pdftotext -raw`. Reading 9 (:43) names xpdf 4.06.

Overlap with the first pass:
- The PATH fallback, the unrecorded extractor and 'print pdftotext -v' are already in first-pass findings 1 and 5 (optional, not adopted).
- What is new: fix (1) made the extractor's page-break setting load-bearing. Finding 5's skeptic judged pinning unnecessary because "-raw order is fixed by the hash-pinned PDF". That holds for line order. Page numbers now come from extractor configuration, not from the PDF, so the rationale no longer covers the Maine block.

**The skeptic's corrected fix.** Stop the extractor's page-break setting from being blamed on the TSV.

Minimal fix, in the Maine block right after `my @l = <$pt>;`:
- Count the form feeds in @l.
- If there are none, or fewer than the highest page that year's rows cite, call `problem("$cert: the text layer from $pdftotext carries no page breaks (xpdfrc textPageBreaks no?) - the page check cannot run")`.
- Then skip only the page comparisons (lines 626 and 633). Keep the figure, heading, at-large and footnote comparisons, which do not depend on page breaks.

Stronger fix: count pages independently of the setting, by running `$pdftotext -raw -f $n -l $n` for each cited page. Both xpdf and poppler support -f and -l. Do not pin with `-cfg <empty file>`: that option is xpdf's own, and poppler's pdftotext (what the line-62 PATH fallback finds on other systems) does not list it.

Provenance, already proposed in first-pass findings 1 and 5:
- Print the `$pdftotext -v` line with the summary.
- Name xpdf 4.06 in reading 1 as reading 9 does, and say the cited page is counted from the extractor's page breaks.

### 7. Reading 3, the OPEN reading put to Elias: option (b)'s one example (In re Guerra) is, by the saved opinion, the three already-named Electors' own case, and no option states its cost

- **Lens:** record - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/president_returns.md:37

**The scenario.** Elias reads option (b): 'search further for official records of the four (the Washington Supreme Court's In re Guerra was tried at a guessed URL ... and not found)'. He would take it that a lead on the four was tried and failed. The saved opinion (raw/returns/supremecourt_chiafalo_v_washington_19-465.pdf, slip op. 6-7) defines 'the Electors' as Chiafalo, Guerra and John only. It says: 'The Electors challenged their fines in state court ... the State's Supreme Court affirmed that judgment. See In re Guerra, 193 Wash. 2d 380'. So In re Guerra is the three named Electors' appeal. The opinion never mentions Washington's Faith Spotted Eagle elector, and a Washington case could not name Texas's or Hawaii's electors. No lead on the four was actually tried. None of the three options gives its cost either. Under (a), no BILLED row covers the four missing names: the 'Not reached' list at :52-60 BILLs In re Guerra, not the names. Under (b), no candidate record is named, although the saved Texas certificate itself points to one ('as set forth in record of the Electoral College'). Under (c), the record does not say this needs a ruling on the sourcing standard. Every other file agrees with the reading: :3, the plan's Built line, and the code comments, which say 'recipients named, not modelled'.

**The fix proposed.** Reword (b) to name where the four could actually be recorded: the states' own records of their electors' meetings, for example the Texas 'record of the Electoral College' that the saved certificate cites. Add that In re Guerra, tried at a guessed URL and not found, is by the opinion's own account the three Electors' appeal. Give each option its cost in a clause. (a): the four names BILLED as 'no official record found', with that row added to 'Not reached'; nothing else owed. (b): an open search with no lead in hand. (c): press pages saved and registered as secondary, under a ruling that allows them to name electors.

**The skeptic's evidence.** The finding holds once two overclaims are trimmed. Part of it is new: the In re Guerra attempt only came in with the fix. The rest is first-pass finding 20's fix left incomplete.

THE RECORD
- president_returns.md:37 says: "**(a)** accept it as built — the three named on the Supreme Court's opinion, the other four by recipient only; **(b)** search further for official records of the four (the Washington Supreme Court's *In re Guerra* was tried at a guessed URL, which served the court site's error page, and not found); **(c)** allow press sources for the four."
  - None of the three options has a cost clause.
  - Nothing in the record says what In re Guerra is.
- :57 says only: "the URL tried served the court site's error page; the opinion was not found (reading 3)".

THE SAVED OPINION
- File: raw/returns/supremecourt_chiafalo_v_washington_19-465.pdf, read with pdftotext -raw. Its digest is 4491bf0e..., the same as SHA256SUMS.txt:17.
- Slip op. 6: "Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John (the Elec-tors)."
- Slip op. 7: "The Electors challenged their fines in state court ... the State's Supreme Court affirmed that judgment. See In re Guerra, 193 Wash. 2d 380, 441 P. 3d 807 (2019)."
- So, by the saved opinion, In re Guerra is the three named Electors' own appeal.
- The text contains no "Spotted", "Eagle" or "Satiacum", and no Texas or Hawaii elector.

WHAT WAS FETCHED
- PoliSim-captures/sources/usa_us3/review786/fetch_log.txt:
  - "20:46:23Z 200 3623 wa_courts_in_re_guerra_953478.pdf https://www.courts.wa.gov/opinions/pdf/953478.pdf"
  - A NOTE line: "discarded: ... the courts site's error page".
- vote2016/fetch_log.txt holds only NARA's three certificates.
- Nothing was tried for Texas's two electors or Hawaii's one.

THE TEXAS CERTIFICATE
- The saved Texas certificate has four replacement-elector notices, each ending "as set forth in record of the Electoral College". The phrase is real and points to a record that is not on disk.
- Whether that record ties ballots to electors is unknown; the certificate says they "voted by individual ballot".

THE RULE AND THE FIRST PASS
- CLAUDE.md:109: "Ruling-first ... state the options with their costs, and stop."
- In the first-pass report, line 878 is finding 20's corrected fix: "Give him the options and their costs: (a) ... nothing more owed; (b) ... one page to save and register; (c) ... which the sourcing rules would need him to allow."
- The applied fix kept the options and dropped the costs. The same is true of rec786.md:19, the "Owed to Elias" draft for COMPLETED.md.

OVERCLAIMS TRIMMED
- "No lead on the four was actually tried" goes too far. A Washington Supreme Court opinion on the fines might name Washington's fourth elector in its facts, and that is not on disk either way. The right claim: In re Guerra was at most a lead on one of the four, and none was tried for the other three.
- "Could not name Texas's or Hawaii's electors" should read: it is not a record of them.
- Option (c)'s "allow" already marks it as Elias's permission. What is missing there is only the cost.
- The code comments read "named in the catalog and not modelled" (SeatAllocationBacktest.cs:191 and :208), not "recipients named". This does not change the finding.
- The BILLED row is optional. CLAUDE.md:112 BILLs a source that cannot be reached, and reading 3 already says what is missing and why.

NEW SLIP ON THE SAME LINE, BROUGHT IN BY THE FIX
- Reading 3 quotes "Three Washington electors, Peter Chiafalo, Levi Guerra, and Esther John (the Electors), violated their pledges to support Hillary Clinton" as the opinion's words.
- In the saved PDF that sentence is only in the Syllabus, at text lines 27-28. The opinion itself begins "JUSTICE KAGAN delivered the opinion of the Court" at line 121.
- The slip's own NOTE says: "The syllabus constitutes no part of the opinion of the Court".

WHY MINOR
- No CSV, catalog figure, check or allocator reads reading 3.
- But it is the owner-facing ask. As written, option (b) reads as if the obvious lead was tried and failed, when untried candidate records exist.

**The skeptic's corrected fix.** Edit president_returns.md:37 (reading 3) and :57.

1. Option (b): replace the parenthetical with this. By the saved opinion (slip op. 7, "The Electors challenged their fines in state court ... See In re Guerra, 193 Wash. 2d 380"), In re Guerra is the three named Electors' own appeal. It was tried at a guessed URL (courts.wa.gov/opinions/pdf/953478.pdf), which served the court site's error page, and was not found. Whether it names Washington's fourth elector is not on disk. Nothing has been tried for Texas's two electors or Hawaii's one. One candidate not yet tried: the "record of the Electoral College" that the saved Texas certificate's replacement notices cite. Add the same identification at :57 ("the three Electors' own appeal, by the Supreme Court's account").

2. Give each option its cost in a clause (CLAUDE.md:109; first-pass finding 20's corrected fix, report line 878):
   - (a) Nothing more owed. Add the four names to "Not reached" as "no official record found".
   - (b) An open search: the Washington opinion at its right URL, Texas's meeting record, Hawaii's. Each page found is saved and registered; each not found is BILLED. No page in hand is known to name the four.
   - (c) A ruling on the sourcing standard: press pages saved and registered as secondary, with the basis stated on each name.
   Carry the same cost clauses into COMPLETED §786's "Owed to Elias" paragraph when it is appended (rec786.md:19 has the bare options). The plan's Built line needs no edit; it points at reading 3.

3. In the same edit, fix the attribution. The first quotation is the Reporter's syllabus, which "constitutes no part of the opinion of the Court". Either attribute it to the syllabus, or quote the opinion's own words (slip op. 6): "Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John (the Electors). All three pledged to support Hillary Clinton in the Electoral College." Keep "So the three Electors voted for Colin Powell for President" and "Only seven electors across the Nation cast faithless votes"; both are the opinion's own words.

Do not claim that In re Guerra could not name any of the four. Say only what the saved opinion says it is.

### 8. Reading 8 and line 23 read the workbook's undefined 'TBC' as 'all ballots cast, blank and overvoted ballots counted', which no saved page says

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/president_returns.md:42

**The scenario.** Reading 8 says a leader holding 'more than half of all ballots cast, blank and overvoted ballots counted' wins outright, and that 'the tool requires that of every leader ... (the district workbooks' TBC column)'. Line 23 says the same. No saved page defines TBC: not the workbooks, and not the three saved Maine SoS results/RCV pages. On all four CG Total rows the tool reads, TBC is exactly the five candidate columns plus Others plus Blank (2020: 447981 and 380324; 2024: 439574 and 402873). No overvote column exists. Whether ballots overvoted at rank one, which §723-A(2) counts, are inside TBC is therefore not on disk. 'Overvoted ballots counted' is an unlabelled READING, and the same reading sits in Tools/us_returns_prep.pl:543 and ElectoralCollege.cs:36. The conclusion still holds: a leader above half of TBC is above half of the continuing ballots in every round. Only the description over-claims.

**The fix proposed.** In reading 8 and line 23, say the tool holds each leader to more than half of the workbook's TBC. Mark it as a READING: the sheets do not define TBC; it equals the candidate, Others and Blank columns; whether overvoted ballots are among them is not on the saved pages. Add one clause on why the plurality winner is the statute's winner either way (the leader's tally never falls and the continuing ballots never exceed TBC). Use the same wording at us_returns_prep.pl:543 and ElectoralCollege.cs:36.

**The skeptic's evidence.** CONFIRMED, documentation only (no output path). This is new: first-pass finding 8 equated TBC with "all ballots cast" itself and never asked what TBC contains. Its fix (3) wrote that equation into the record and the code.

1) Where the reading sits.
- president_returns.md:42 says the statute counts a leader "holding more than half of all ballots cast, blank and overvoted ballots counted", then "the tool requires that of every leader ... (the district workbooks' TBC column)".
- president_returns.md:23 says each leader "holds more than half of all ballots cast".
- us_returns_prep.pl:543 says "(the workbook's TBC, blank and overvoted ballots counted)". The same gloss appears at :534 ("'TBC' (total ballots cast)"), at :38, and in the :549 and :553 messages ("ballots cast").
- ElectoralCollege.cs:35-36 says "hold more than half of all ballots cast". It has no overvote wording, but it makes the same identification.

2) The statute paraphrase is right. Saved 723-A(2): "more than 50% of all ballots cast for the particular office ..., including but not limited to ballots on which ranking number one is blank, on which there is an overvote at ranking number one ...". The gap is only in the TBC identification.

3) No saved page defines TBC.
- CG1/CG2 row 1, 2020 and 2024: CG | CTY | MUNICIPALITY | five candidates | Others | Blank (BLANK in 2024) | TBC. There is no overvote column.
- None of the following expands TBC: the sharedStrings of all four 2020/2024 workbooks, the 2016 xlsx, the 2012 xls pair, docProps/connections/customXml (no comments parts exist), or the Maine SoS results pages.
- maine_sos_rcv.html says only that a ballot with no first choice "was counted as blank on election night". It defines Overvote for the RCV grid, not for the town tally.

4) What TBC is.
- 2024: every TBC cell on both CG sheets is a formula, SUM(Dn:Jn) (shared K4:K31 and the rest). TBC is the sum of the tallied columns by construction.
- 2020: TBC is typed, and D..J equals K in every row.
- Total rows:
  - 2020 CG1: 266376+649+4654+7343+164045+45+4869 = 447981
  - 2020 CG2: = 380324
  - 2024 CG1: = 439574
  - 2024 CG2: = 402873
- So whether rank-one overvotes are inside Blank, and therefore inside TBC, is not on disk.

5) Why it is only a note. The winner never rests on TBC: us_returns_prep.pl:664-683 holds each Maine 2020/2024 district, and the state, to NARA's notes, and the split block holds each state to NARA's table. The same reading is older than this change at district_method_2024.md:86-87 ("including blanks and overvotes ... 842,447 ballots", bd34c8c8; not in this diff's hunks).

6) The finding's own mitigation needs the same reading. "Continuing ballots never exceed TBC" holds only if TBC holds every ballot. Saved 723-A(1)(D)/(E), read literally, judge exhaustion at "the highest continuing ranking" each round. A ballot overvoted at rank one between two candidates who are both later removed could then count for a lower-ranked candidate. If Blank omits overvotes, continuing ballots could exceed TBC. Minor slip: the finding says "three" SoS pages; five are saved, and none defines TBC.

**The skeptic's corrected fix.** At president_returns.md reading 8 and line 23, us_returns_prep.pl :38, :534, :542-544, :549 and :553, and ElectoralCollege.cs:35-36:

1. Say the tool holds each leader to more than half of the workbook's TBC.
2. Mark that as a READING. No saved page expands TBC. On the CG sheets it is the sum of the candidate, Others and Blank columns (a formula in 2024; typed but equal in 2020), and there is no overvote column. Whether ballots overvoted at rank one are counted in Blank is therefore not on the saved pages. Only on that reading is the check §723-A(2)'s outright majority.
3. Ground "the plurality winner is the statute's winner" on what the tool does hold. NARA's notes name the winner of each Maine 2020/2024 district and of the state, and the tool holds the record's figures to them (lines 664-683 and the split block).
4. Do not add "the continuing ballots never exceed TBC" as a fact. It rests on the same reading, and on a literal reading of 723-A(1)(D) as well.

Optionally, give district_method_2024.md:86-87 (an older sentence, but the ruling brings that file to the raw-page standard) the same READING label.

### 9. 'Dies, naming every mismatch': the READ block's new checks die at once and drop any mismatch already found

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/president_returns.md:16

**The scenario.** Measured on a temp copy. First, NARA's 2016 Texas note alone was made to disagree with the FEC's; the run printed it and ended '2 mismatch(es) - nothing written'. Then a TSV row citing raw/returns/fec_2024presgeresults.xlsx was added as well. That run printed only 'a 2016 row cites ... not that year's certificate' (the die at us_returns_prep.pl:572), and the queued NARA/FEC mismatch was never printed. The die()s at :568-575 and :581 behave the same way. Some are new with fix 1 (the certificate, page and unallocated-district checks). The tool header at :27 ('naming every one') over-claims the same way.

**The fix proposed.** Turn the row checks at :572/:573/:575/:581 into problem() and skip the row, keeping die() only for an unreadable file or heading. Or reword :16 and the tool header to 'dies on any mismatch, naming every one found before the first structural fault'.

**The skeptic's evidence.** I tried to refute this and could not. I reproduced the failing path.

The claims: Tools/us_returns_prep.pl:27 says "It writes nothing and dies, naming every one, on any mismatch". president_returns.md:16 says "It writes nothing and dies, naming every mismatch, unless all of these hold". In the code, :64 `sub problem { push @problems, ... }` queues each mismatch, and only :742 prints the queue (`print STDERR "MISMATCH: $_\n" for @problems; die ...`). A grep finds no $SIG{__DIE__} handler, no END block and no eval. So any die() between the first problem() and :742 exits with only its own message. The note comparison at :481 is a problem() and runs before the READ loop, whose checks at :568, :569, :571, :572, :573, :575 and :581 are die()s.

I ran each case on a fresh temp copy of the inputs. The control exited 0 and wrote four files.
- **A:** NARA's 2016 Texas note changed to "Ted Cruz 1", the page re-pinned. Two MISMATCH lines, then "2 mismatch(es) - nothing written", exit 255, 0 files.
- **B:** A, plus the 2016 ME-2 R row citing raw/returns/fec_2024presgeresults.xlsx. The run printed only "...: a 2016 row cites 'raw/returns/fec_2024presgeresults.xlsx', not that year's certificate", exit 255, 0 files. The queued note mismatch was never printed.

The over-claim is wider than the row checks the finding names:
- **C:** A, plus one byte appended to archives_ascertainment_maine_2016.pdf. Only the SHA-256 die at :84 was printed. page() reaches that die at :596, after :481 has queued. The digest check is the first condition on the record's own list.
- **D:** A, plus a READ figure '223O35'. Only count()'s die at :238 was printed ("is not a count"). count() is called in the READ loop itself.

This is not covered by the first pass. The copy it reviewed (scratchpad us3base) already had the same sentence at its tool :26 and record :16, and dies at its :504, :505, :507 and :511. The dies at :572, :573 and :575 are new with fix 1, and they follow the corrected fix of first-pass finding 4 ("die unless $cert eq ..."). The first pass never raised this. Its finding 6 skeptic, though, treated "naming every one" as the tool's contract and rejected FATAL warnings because they die "before the MISMATCH list prints". The finding-4 dies now cause that same loss.

Why only a note: "writes nothing" and "dies on any mismatch" held in all four runs, and the ruling's done-when asks only that the tool fail on any mismatch. Only the diagnostic wording is false; no output and no gate is affected.

**The skeptic's corrected fix.** The finding's first option is incomplete. Turning only :572, :573, :575 and :581 into problem() still leaves these dies dropping queued mismatches:
- :84, the digest die, which page() reaches at :596 after the note checks;
- :238, count()'s die, called on the TSV's votes field in the READ loop;
- :530-:534, and the other dies that read a page's layout.

So the sentence would stay false. The fix belongs in the wording, at the tool's :27 and the record's :16. For example: "It writes nothing and dies on any mismatch, naming every one it has found; a page off its digest, or an input it cannot read, stops it at that point and is named alone."

Optionally, also print the queue on every die:
1. Near :65, add `$SIG{__DIE__} = sub { return if $^S; print STDERR "MISMATCH: $_\n" for splice @problems; };`. The `$^S` guard skips a module's own evals.
2. At :742, use `my $n = @problems; print STDERR "MISMATCH: $_\n" for splice @problems; die "$n mismatch(es) - nothing written\n";` so the final die does not print the list twice.

With that in place, the READ row checks at :569, :571, :572, :573, :575 and :581 can become problem() followed by next; the existing 'not READ' problems then follow. The wording still needs "found", because a die stops every later check.

Outside this finding: :481 loops over sort(keys %others_fec), sort(keys %others_nara) without removing duplicates. One state that disagrees is printed twice and counted as "2 mismatch(es)" (run A).

### 10. returns_2024.md says the tool holds its state table and national figures, but the tool never reads returns_2024.md

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/returns_2024.md:90

**The scenario.** The s786 read-back ends: 'The state table and the national figures are also held by Tools/us_returns_prep.pl (president_by_state.csv's 2024 rows)'. No file under Tools/ or Assets/ reads ElectionsData/usa/returns_2024.md; git grep finds only France's returns_2024.md in comments. The winner shares and the FEC's percentages are in no CSV. A typo in this file's PA row, say 50.20 written as 50.02, or in the 49.7961% line would fail no tool and no check. The phrasing parallels state_ev_2024.csv, which the tool does hold row by row (:715-734), so a reader would take this table as checked too.

**The fix proposed.** Say instead: 'The same votes and electors are president_by_state.csv's 2024 rows, which the tool holds to the saved pages; this file's table and percentages are checked by no tool (read back by hand on 2026-10-05).'

**The skeptic's evidence.** I could not refute it. The sentence claims a tool check over this file, and nothing reads the file. No figure is wrong today, so it stays a note.

1. The claim. returns_2024.md:90 ends: "The state table and the national figures are also held by `Tools/us_returns_prep.pl` (`president_by_state.csv`'s 2024 rows)."

2. Nothing reads returns_2024.md.
   - The tool opens only these: :70 the SHA256SUMS.txt files, :78 page() on raw/<group>/ files, :506 and :597 pdftotext on raw PDFs, :560 the READ TSV, :717 state_ev_2024.csv, and :753 its own outputs.
   - `git grep --cached returns_2024 -- Tools Assets` finds only France's file, in comments at PartySystem.cs:779 and WorldClock.cs:296.
   - GeneratedCatalogCheck.cs:536-551 reads only the RawSources pages, the TSV and the three CSVs. RawSources lists raw/ pages only.
   - So the finding's example holds: PA 50.20 typed as 50.02, or an edited 49.7961%, fails nothing.

3. The change's own wording separates "holds" (checks) from "carries" (contains).
   - state_ev_2024.csv:8-9: "Tools/us_returns_prep.pl holds every row to the saved pages ...; president_by_state.csv carries the same figures and more". The tool does check it, at :714-735.
   - president_returns.md:65: state_ev_2024.csv is "held by the tool to the saved pages".
   - president_returns.md:64: returns_2024.md was only "Read back against the saved pages on 2026-10-05".
   - district_method_2024.md:144 states its table as equal to the CSV rows "figure for figure", and makes no claim of a tool check.
   - Read in that vocabulary, :90's "held by" claims a check that the record itself does not claim.

4. Even the generous reading ("the CSV holds the same facts") is only partly true.
   - The three CSVs contain no decimal value (grep count 0 in each) and no Stein, Kennedy or Oliver (count 0).
   - The CSVs have no national row; the national figures are only sums of the state rows.
   - So the winner shares, the FEC's percentages and the minor candidates' votes are not in president_by_state.csv's 2024 rows.

5. Why only a note.
   - Every figure is right today. I recomputed from the 2024 rows of president_by_state.csv: 51 rows; D 75017613, R 77302580, total 155238302; cast 226/312; R 49.7961, D 48.3242. All twelve table rows match the CSV on electors, winner and share (PA 19 R 50.20, GA 16 R 50.72, MI 15 R 49.73, WI 10 R 49.60, AZ 11 R 52.22, NV 6 R 50.59, NC 16 R 50.86, CA 54 D 58.47, TX 40 R 56.14, NY 28 D 55.91, FL 30 R 56.09, OH 17 R 55.14).
   - No output, check or allocator path depends on the sentence.
   - It is the same class as first-pass finding 22 ("re-hashes every page"), which was graded note.

6. This is new, not a repeat. The sentence was already present when the first pass ran: scratchpad us3base/ElectionsData/usa/returns_2024.md:90 (sha d02c9f38). No first-pass finding covers it; findings 16 and 17 deal with :58, :77 and the typography in :90.

**The skeptic's corrected fix.** Change only the last sentence of ElectionsData/usa/returns_2024.md:90. Use the change's own verbs, as state_ev_2024.csv:8-9 does ("holds ... to" means checks; "carries" means contains), and write no figures. Suggested text: "No tool reads this file: its figures rest on the read-back above. `president_by_state.csv`'s 2024 rows carry the same votes and electors - each jurisdiction's two nominees, all others together, its total, its electors and its electoral votes as cast; the national figures are their sums - and `Tools/us_returns_prep.pl` holds those rows to the saved FEC workbook and NARA's page. The winner shares, the FEC's percentages, Stein, Kennedy and Oliver are in no CSV."

The finding's own fix ("The same votes and electors are president_by_state.csv's 2024 rows...") is nearly right, but "the same votes" would include Stein's, which the CSV only counts within votes_other. It also drops the point that the national figures are sums, since the CSV has no national row. president_returns.md:64 is already accurate ("Read back"), so it needs no change. This is a documents-only edit: no tool, CSV, catalog or bar change.

### 11. Reading 3 attributes a syllabus sentence to 'the Supreme Court's opinion'

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/president_returns.md:37

**The scenario.** The first quotation, 'Three Washington electors, Peter Chiafalo, Levi Guerra, and Esther John (the Electors), violated their pledges to support Hillary Clinton', comes from the Syllabus on page 1 of the saved PDF. The PDF itself says 'The syllabus constitutes no part of the opinion of the Court but has been prepared by the Reporter of Decisions'. A reader citing it as the Court's words would be citing the Reporter. The second and third quotations are the opinion's own (slip op. 6-7), and so is its naming of the three: 'Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John (the Electors). All three pledged to support Hillary Clinton in the Electoral College.'

**The fix proposed.** Quote the opinion's own sentence above in place of the syllabus line, or label the first quotation 'the syllabus (the Reporter of Decisions', no part of the opinion)'.

**The skeptic's evidence.** I could not refute this. The misattribution came in with first-pass fix (9), the fix for first-pass finding 20. That finding's own evidence had labelled the sentence correctly: review786_first.md:849 reads "Syllabus: 'Three Washington electors, Peter Chiafalo, ...'" and :850 reads "Opinion of the Court, slip op. 6-7: 'Among those Democratic electors were petitioners ...'". So the finding is a fault in a first-pass fix, not a repeat of a first-pass finding. The record is new in this change: it has no Chiafalo text at 5fe3c031, and reading 3 is a '+' line in the staged diff.

1. The record, ElectionsData/usa/president_returns.md:37: "The Supreme Court's opinion in *Chiafalo v. Washington* (No. 19-465, decided 6 July 2020; saved) does: "Three Washington electors, Peter Chiafalo, Levi Guerra, and Esther John (the Electors), violated their pledges to support Hillary Clinton", and "the three Electors voted for Colin Powell for President" — the opinion adds that "Only seven electors across the Nation cast faithless votes"."

2. The saved PDF, raw/returns/supremecourt_chiafalo_v_washington_19-465.pdf, read with `pdftotext -raw`:
   - Page 1 is headed "Syllabus". Lines 4-7: "NOTE: ... The syllabus constitutes no part of the opinion of the Court but has been prepared by the Reporter of Decisions for the convenience of the reader."
   - Syllabus lines 27-29: "Three Washington electors, Peter Chiafalo, Levi Guerra, and Esther John (the Electors), violated their pledges to support Hillary Clinton in the 2016 presidential election."
   - I joined the hyphenated line breaks and scanned the whole text. The phrase "Three Washington electors," occurs only in the syllabus. It is not in the opinion of the Court (which begins at line 121, "JUSTICE KAGAN delivered the opinion of the Court") and not in THOMAS, J.'s concurrence (from line 765).

3. The opinion's own text at slip op. 6 (lines 313-327, running head "6 CHIAFALO v. WASHINGTON / Opinion of the Court") words it differently: "This case involves three Washington electors who violated their pledges in the 2016 presidential election. ... Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John (the Electors). All three pledged to support Hillary Clinton in the Electoral College. ... So the three Electors voted for Colin Powell for President. But their effort failed. Only seven electors [p.7] across the Nation cast faithless votes".
   - The second and third quotations are the opinion's own, verbatim.

Why only a note:
- The record's substance is true. The opinion itself names the three, and its definition "(the Electors)" ties them to the Powell votes.
- These statements stay correct: the register row at line 92 ("slip opinion"; "cited: names the three Washington electors who voted for Colin Powell"), option (a) "the three named on the Supreme Court's opinion", and the heading's "three of the electors named by the record".
- No tool check, CSV, catalog figure or C# path reads this wording.
- CLAUDE.md has no rule on quotation attribution. The cost is a sentence that a reader would cite as the Court's words when the Reporter of Decisions wrote it.

**The skeptic's corrected fix.** In ElectionsData/usa/president_returns.md:37, replace the first quotation with the opinion's own sentence (slip op. 6).

Suggested wording: "The Supreme Court's opinion in *Chiafalo v. Washington* (No. 19-465, decided 6 July 2020; saved) does: "Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John (the Electors). All three pledged to support Hillary Clinton in the Electoral College", and "the three Electors voted for Colin Powell for President" (slip op. 6) — the opinion adds that "Only seven electors across the Nation cast faithless votes" (slip op. 6-7)."

If the syllabus sentence is kept, label it: "the syllabus (prepared by the Reporter of Decisions; 'no part of the opinion of the Court')".

Leave the register row (line 92), option (a) and the heading unchanged; they are accurate. No tool rerun is needed, because no check, CSV or C# file reads this text.

### 12. LR24CA's 2026 ballot status is on the page saved this pass, yet both records list it as press not fetched

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/district_method_2024.md:146

**The scenario.** raw/district/nebraska_sos_elections.html, saved at 20:46:24Z and registered, lists 'Ballot Measures for 2026 General Election': 'Initiative Nos. 440-442' and 'Constitutional Amendment passed by the Nebraska Legislature for the 2026 General Election LR19CA'. LR24CA is not among them. Yet district_method_2024.md:146 and president_returns.md:60 both list 'the ballot status of LR24CA' as resting on press that was not fetched. A re-verifier would treat ':107 ... is not on the 2026 ballot' as press-only. This is the same class as first-pass finding 12, now arising from the page that finding's fix saved.

**The fix proposed.** Cite raw/district/nebraska_sos_elections.html for LR24CA's absence from the 2026 ballot (the page lists LR19CA as the Legislature's 2026 amendment). Remove 'the ballot status of LR24CA' from both not-fetched lists.

**The skeptic's evidence.** I could not refute it. Every quoted line is in the staged text, and the index equals the working tree.

1. **What the records say.**
   - district_method_2024.md:106-107 (August text): "The companion constitutional amendment **LR24CA** was never floor-debated and is **not** on the 2026 ballot".
   - :146 (staged, s786): "**Not fetched:** the press named above - the cloture's date (...), the ballot status of LR24CA, the 2025 initiative; ...".
   - president_returns.md:60 (staged): "The press items of `district_method_2024.md` (WOWT, the Nebraska Examiner — the cloture's date, the 2025 initiative and its withdrawal, LR24CA's ballot status) were not fetched."

2. **What the saved pages say.** Two pages together establish the ballot status.
   - raw/district/nebraska_sos_elections.html is fetched, hashed and registered: fetch_log.txt:33 `2026-10-05T20:46:24Z 200 70827`; SHA256SUMS.txt:23 a6a09dac..., which sha256sum reproduces; register row president_returns.md:142. Its HTML lines 1221 and 1226 read "Ballot Measures for 2026 General Election / Initiative Nos. 440-442" and "Constitutional Amendment passed by the Nebraska Legislature for the 2026 General Election / LR19CA". `grep -c LR24CA` on the page gives 0.
   - raw/district/nebraska_unicameral_update_39341.html (published 2025-07-11) says: "If approved by the Legislature, LR24CA ... would place the question of reinstating winner-take-all on the 2026 general election ballot."
   - Together: the Secretary of State lists LR19CA as the only amendment the Legislature passed for 2026, so LR24CA was not approved and is not on the ballot. The fact is on disk.

3. **What the record cites the Secretary of State's page for.** Only "no notice of a change to the electors": district_method_2024.md:145, president_returns.md:66, and the register's Use column at :142. That is the same inference from absence, made from the same page. The page is never tied to LR24CA.
   - So line 146 does what the section already does elsewhere: for the cloture's date it notes what saved BillTrack50 shows, and for FindLaw it names the saved capture that replaces it. It gives no such pointer for LR24CA.
   - A reader with no memory of the session would take the ballot status as press-only.

4. **How this arose (scope).** First-pass finding 12's skeptic wrote "LR24CA ... Its 2026 ballot status is not on any saved page" and fixed it with "Keep ... LR24CA's 2026 ballot status ... on the press list". That was true then. The page was then fetched by finding 13's fix, not finding 12's as this finding says, and nobody re-checked the press list against it. Finding 12's fix is therefore now incomplete.

5. **Severity: a note.** The claim itself is right, and no code, CSV or catalog reads it. This is provenance claimed too weakly, not a false statement. Line 146 is literally true: that press was not fetched. The evidence is absence from an official list, not the page's own words. Finding 21 of the first pass (an older claim, now checkable against a saved page) was graded a note.

6. **Caution for the fix.** The page lists Initiative Nos. 440-442 and LR19CA without saying what they are, and neither the ballot-measures pamphlet nor LR19CA's ballot statement was saved. The page therefore proves only that LR24CA is absent. It does not prove that no 2026 measure touches the electors.
   - "Never floor-debated" in 2026 is on no saved page either. 39341 covers only 2025: "considered by the ... Committee but was not advanced".

**The skeptic's corrected fix.** Change only the records; leave raw pages and code alone.

1. **district_method_2024.md:145 or :146.** Cite the two saved pages for the ballot status:
   - the session review (`_39341.html`): "If approved by the Legislature, LR24CA ... would place the question of reinstating winner-take-all on the 2026 general election ballot";
   - the Secretary of State's 2026 page (`raw/district/nebraska_sos_elections.html`), which lists the "Ballot Measures for 2026 General Election" as "Initiative Nos. 440-442" and the "Constitutional Amendment passed by the Nebraska Legislature for the 2026 General Election" as "LR19CA". LR24CA is not among them.
   - Say also that the page names these measures without describing them. It proves LR24CA's absence and nothing about the measures' subjects.

2. **The not-fetched list, :146.** Replace "the ballot status of LR24CA" with what no saved page carries: "that LR24CA was never floor-debated in 2026 (the saved session review is of 2025: 'considered by the ... Committee but was not advanced')".

3. **president_returns.md:60.** Make the same replacement: "LR24CA's ballot status" becomes "LR24CA's 2026 session".

4. **president_returns.md:142 and :66.** Extend the register row's Use column and the summary bullet: "no notice of a change to the electors; the 2026 ballot measures, LR24CA not among them".

A lighter alternative that matches how the cloture's date is treated: keep the item on both press lists, and add the same parenthetical pointing to nebraska_sos_elections.html.

### 13. The not-fetched list still leaves out three LB3 facts that rest on no saved page

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/district_method_2024.md:146

**The scenario.** Line :140 says the pages this file cites are saved, 'all but those the last bullet names'. The last bullet (and president_returns.md:60) names only the cloture's date, LR24CA and the initiative. Three facts are missing. (1) 'two Republicans voting against' (:105): :117 gives the roll call to the press, but neither list names it. (2) 'at the biennium's sine die' (:106): BillTrack50 gives only 'Indefinitely postponed (on 04/17/2026)', and 'sine die' appears on no saved page. (3) 'the 110th Legislature convenes January 2027 and the Governor has said he intends to keep pursuing winner-take-all before 2028' (:134-135): no source is named, and '2028' appears on none of BillTrack50, 37216, 39341 or the NE SOS page. A reader would take all three as backed by a saved page.

**The fix proposed.** Add the roll call's party breakdown, the sine-die timing and the Governor's 2028 intent (with the 110th Legislature date) to the not-fetched bullet and to president_returns.md:60, each with its press source or BILLED.

**The skeptic's evidence.** The first pass did not report this. It is an incomplete first-pass fix. The not-fetched bullet was built from finding 12, whose skeptic kept only the cloture's date, LR24CA and the initiative. Finding 13 added FindLaw and the SOS page. Neither finding checked the rest of the LB3 paragraph. Finding 21 (report :910) already noted that the bullet "suggests the rest of the paragraph is backed by the saved pages".

The record (staged; index equals working tree):
- district_method_2024.md:104-106: "**cloture FAILED 31–18 on 8 April 2025** — two short of the 33 required, two Republicans voting against — and it was never rescheduled. **Indefinitely postponed 17 April 2026** at the biennium's sine die."
- :117: "contemporaneous press (WOWT, Nebraska Examiner) for the roll call."
- :134-135: "the 110th Legislature convenes January 2027 and the Governor has said he intends to keep pursuing winner-take-all before 2028." These are context lines from August that this change left as they were, and they name no source.
- :140: "the pages this file cites - all but those the last bullet names as not fetched - are saved".
- :146: "Not fetched: the press named above - the cloture's date (8 April 2025; ...), the ballot status of LR24CA, the 2025 initiative; and FindLaw's reproduction".
- president_returns.md:60: "The press items of district_method_2024.md (WOWT, the Nebraska Examiner — the cloture's date, the 2025 initiative and its withdrawal, LR24CA's ballot status) were not fetched."

The saved pages. Each matches its line in SHA256SUMS.txt (39341 03e1e81f..., BillTrack50 a5cc5211..., SOS a6a09dac...).
- 39341 was published 2025-07-11T18:55:08Z, so it cannot speak to 2026. It says only "The motion failed on a vote of 31-18. Thirty-three votes were needed." and "LB3 was not scheduled for further debate and remains on general file." The word "Republican" does not appear on it.
- BillTrack50 has "Dead 04/17/2026", "Last Action Indefinitely postponed (on 04/17/2026)" and "Introduced Session 109th Legislature". Its "Votes" tab holds no vote data, and the page has no "sine die".
- The SOS elections page has no 2028, LB3 or elector notice.
- 37216 uses "Republican" only for the testifiers from the Nebraska Republican Party and the Cheyenne County Republican Party.

Greps over all of ElectionsData/usa/raw:
- "sine die" appears only in raw/records/wayback_senate_* (the US Senate).
- "110th" appears only in US Congress history pages.
- "2028" in district/ matches only the bytes of two canvass PDFs. Their pdftotext text has none.
- No WOWT or Nebraska Examiner page exists in raw/, in any fetch_log.txt, or in the out-of-tree captures (cited2024, review786, district).

How the three facts stand:
- (1) is partly covered: :117 ties the roll call to the press, and :146 names "the press named above". But 39341 now carries the 31-18 and the 33, and :146 singles out only the date from that sentence. A reader would take "two Republicans voting against" as being on 39341, and it is not.
- (2) and (3) cite no page, so :140's claim about cited pages is not literally false. They are unsourced facts (CLAUDE.md:112, "A source is fetched, not remembered") in a record the ruling said must meet the raw-page standard, and the not-fetched list does not flag them. (3) carries the most weight: it reports a statement by the Governor and names no source anywhere.

Why the grade is note: no code, CSV, catalog figure or the NOT ENACTED verdict depends on these facts; the verdict rests on BillTrack50 and 39341. Findings 12 and 13 were minor because their claim was literally wrong. Here :140's literal claim holds, so this sits closer to finding 17 (a note).

**The skeptic's corrected fix.** Do not use "BILLED". CLAUDE.md:112 keeps BILLED for a source that cannot be reached, and no source for these facts was ever tried; finding 13's skeptic said the same. For (2) and (3) the record names no source, so a "not fetched" entry has no page to name.

1) In district_method_2024.md:146 and president_returns.md:60, add the cloture vote's party breakdown to the press items: "the vote's party breakdown (two Republicans against; 39341 gives only 31-18 and the 33 needed)".

2) For "at the biennium's sine die" (:106) and "the Governor has said he intends to keep pursuing winner-take-all before 2028" (:134-135), do one of these:
- fetch a source, save it and register it (the Legislature's journal is on the refused host; the Governor's statement would come from the press or his office);
- name both in the bullet as resting on no saved page and no named source;
- delete both. "Indefinitely postponed 17 April 2026" stands on BillTrack50 alone, and "Re-check before modelling any cycle after 2026" stands without the Governor's intent.

"The 110th Legislature convenes January 2027" is a calendar fact that is not on any saved page. Drop it with the clause, or name its basis.

3) An aside on the same bullet. The SOS elections page that the fix saved lists LR19CA as the one "Constitutional Amendment passed by the Nebraska Legislature for the 2026 General Election". So LR24CA's absence from the 2026 ballot is now on a saved page. The bullet's "the ballot status of LR24CA" item understates its sourcing, the same kind of error finding 12 caught for the hearing. That LR24CA was "never floor-debated" after July 2025 is still on no saved page.

### 14. Reading 8 says the governing text for 2012/2016 is not on disk, but saved pages show neither election was a ranked-choice count

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/president_returns.md:42

**The scenario.** Reading 8 ends: 'For 2012 and 2016 the READ rows carry no ballots-cast count, and which text governed those elections is not on disk.' Two saved pages settle the question. The saved §723-A(6), tagged only 'IB 2015, c. 3, §5 (NEW); IB 2015, c. 3, §6 (AFF)', reads 'This section applies to elections held on or after January 1, 2018'. The saved 2012 SoS page (wayback_maine_sos_2012electoral_20150809.html) states 'The candidate who wins the most popular votes in the First Congressional District wins one elector'. As written, an R-K9 or US-4 reader would treat the plurality basis of the 2012/2016 READ winners as unsourced and could BILL or re-source it.

**The fix proposed.** Add: 'the ranked-choice count did not govern them: §723-A(6) (IB 2015, c. 3, unamended) applies it to elections on or after 1 January 2018, and the Secretary's 2012 page states the plurality rule'.

**The skeptic's evidence.** I could not refute this. The facts check out, nothing computed depends on them, and part of the gap is already covered elsewhere in the record.

THE CLAIM. president_returns.md:42 (reading 8) first says the count is ranked-choice "under the statute as saved (21-A §1(27-C)(D), §723-A(2))". It then ends: "For 2012 and 2016 the READ rows carry no ballots-cast count, and which text governed those elections is not on disk." Read in that context, the sentence leaves open whether a ranked-choice count governed 2012 and 2016.

I grepped every non-raw file for '723-A(6)', 'January 1, 2018' and '1 January 2018': the record, district_method_2024.md, returns_2024.md, us_returns_prep.pl, ElectoralCollege.cs, Assets/Editor and docs. None of them hits. No record cites the saved subsection that answers the question.

WHAT THE SAVED PAGES SAY:
- raw/district/maine_legislature_21a_723-A.html (sha256 e1e8bcee..., equal to its SHA256SUMS line; staged A; register row 124), lines 288-293: "6. Application. This section applies to elections held on or after January 1, 2018." Its tag is `[IB 2015, c. 3, §5 (NEW); IB 2015, c. 3, §6 (AFF).]`. Reading 11's own rule is that a subsection's tag names only its latest act. This tag names only the act that created the subsection, so subsection 6 has never been amended.
- The date is in the statute's own words. It does not come from a citation year or a tag, so neither of reading 11's cautions applies to it.
- §801(2) and §805(2) send the presidential count through "the ranked-choice method of counting votes described in section 723-A".
- So §723-A could not govern 2012 (it did not exist yet: NEW by IB 2015) or 2016 (it applies only from 2018).
- raw/district/wayback_maine_sos_2012electoral_20150809.html (df18b6db..., staged A), line 184: "The candidate who wins the most popular votes in the First Congressional District wins one elector. The candidate with the most popular votes in the Second Congressional District wins one elector. The candidate who wins the most votes statewide wins the two at-large electors."

WHAT LIMITS IT:
- Reading 11 (line 45) already names the 2012 page as "a contemporaneous statement of the rule as applied in 2012", so the 2012 half is covered there. Reading 8 just does not point to it.
- The 2016 half has no source in the record. Only §723-A(6) settles it, and only negatively (no ranked-choice count; it does not state the plurality rule).
- The saved 2016 results page links "Description of the Electoral College (Word)" (electoralcollegeinfo16.docx). That file was never saved: 0 lines in fetch_log.txt and SHA256SUMS.txt.
- No output is affected. The majority check runs only `for my $year (2020, 2024)` (us_returns_prep.pl:523, 541-553). The 2012 and 2016 winners are still held to NARA's split, and for Maine 2016 to NARA's district note.

HOW IT RELATES TO THE FIRST PASS. This shows the first-pass fixes for #8 and #23 are incomplete. The #23 skeptic warned that the record could "mix up the 2012/2016 rule with the 2020/2024 one", with "no earlier limit" on the ranked-choice count. The applied fix (reading 11's "must not be dated from the tags", plus reading 8's "not on disk") leaves that limit open, although §723-A(6) is the earlier limit and it is on disk. Neither the first pass nor the fixes cite it.

SEVERITY: note. The sentence is literally true, because the full wording in force in 2012 and 2016 is not on disk. It is incomplete: it leaves open a premise that a registered page settles, in a record whose job is to say what the saved pages carry.

**The skeptic's corrected fix.** In president_returns.md reading 8 (line 42), replace the last sentence with words only (no derived figures). For example: "For 2012 and 2016 the READ rows carry no ballots-cast count, and none is needed: no ranked-choice count governed them. §723-A, the count the statute sends the presidential race through (§801(2), §805(2)), says in its subsection 6, whose tag names IB 2015, c. 3 alone: 'This section applies to elections held on or after January 1, 2018.' For 2012 the Secretary of State's 2012 page (reading 11) states the most-votes rule. The other wording in force then is not on disk (reading 11)."

Do not date the start of the presidential ranked-choice count from this. Only the earliest bound is on disk; when PL 2019, c. 539 took effect is not.

Optional:
(1) Fetch, save and register the Secretary of State's "Description of the Electoral College (Word)" for 2016, which the saved maine_sos_election_results_2016-2017.html links (electoralcollegeinfo16.docx). Give it a fetch_log.txt line, a SHA256SUMS.txt line and a register row. Then 2016's most-votes rule has a positive source, as 2012's does.
(2) In reading 11's caution ("the count's start must not be dated from the tags"), add that §723-A(6)'s own text gives the earliest possible start.

This is a prose change only. No raw page, tool, CSV or C# changes.

### 15. Reading 7 says both 2012 formula cells sum 'its own rows', but Total Vote sums the Total row's own cells

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/president_returns.md:41

**The scenario.** Decoding Table 2's FORMULA records (scratchpad s786/t2_2012.txt) gives F57 = SUM(F6:F56), the 51 state rows, but G57 = SUM(D57:F57), the Total row's own D, E and F cells. Reading 7 calls both 'SUM formulas over its own rows'. A re-verifier decoding G57 would not find a sum over the state rows. Reading 7's 'refuses a formula in any Total-row cell that has no typed counterpart' is also too broad: for 2024, us_returns_prep.pl:451 tests only the five Total-row cells it reads, not the candidate columns or ELECTORAL VOTES. The tool's comments carry the same imprecision (:32 'Excel's sum of the same rows'; :409 'Excel's sum of the very rows it is held to').

**The fix proposed.** Write: 'fills All Others with SUM(F6:F56), its state rows, and Total Vote with SUM(D57:F57), its own Total-row cells'. Change the refusal to 'refuses a formula in any Total-row cell it reads'.

**The skeptic's evidence.** CONFIRMED on both halves. It is a wording defect only: no output or check is affected.

How this relates to the first pass: reading 7's sentence is close to the wording of fix (b) that the skeptic proposed for first-pass finding 2 ("SUM formulas over its own rows"). That skeptic's own scenario had already decoded G57 as SUM(D57:F57). The refusal from fix (a) was built only for the Total-row cells the tool reads. So this is first-pass finding 2's fix being imprecise, not a new defect.

(1) G57 is not a sum over state rows. I re-ran the scratchpad's s786/dump_xls.pl (read-only) on the SHA-pinned raw/returns/fec_federalelections2012.xls, sheet 'Table 2. Electoral &  Pop Vote':
  F57 FORMULA cached=2236111 tokens: area(F6:F56) attr(0x10)  -> SUM(F6:F56), the 51 state rows
  G57 FORMULA cached=129085410 tokens: area(D57:F57) attr(0x10) -> SUM(D57:F57), the Total row's own cells
  D57 and E57 are MULRK (typed). Table 1 has 0 FORMULA records.
Several texts describe G57 wrongly or loosely:
- president_returns.md:41 says "fills its Total row's "All Others" and "Total Vote" cells with SUM formulas over its own rows". For G57 that is literally defensible, since row 57 is the table's own row, but it reads as a column sum.
- us_returns_prep.pl:91 says "(2012's Table 2 sums its own state rows into two of its Total row's cells)". This is false for G57.
- us_returns_prep.pl:409 says "A Total-row cell a formula fills is Excel's sum of the very rows it is held to". This is false for G57: line 407 holds it to the state rows' G sum (`$sum{total} == $tot{7}`), but its formula is SUM(D57:F57).
- us_returns_prep.pl:32 says "(Excel's sum of the same rows)".
The record's conclusion still holds for G57, but by redundancy, not by identity. Line 408 checks each state's D+E+F==G, and D57/E57 are typed. Together they reduce SUM(G6:G56)==G57 to the D and E checks.

(2) "refuses a formula in any Total-row cell that has no typed counterpart" (line 41) and "a Total-row cell that a formula fills answers to a typed national figure" (line 20) are broader than the code for 2024:
- us_returns_prep.pl:451 is `for my $k ($evR, $evD, $vR, $vD, $vT) { problem(...) if $formula_at{...}{"$r,$k"}; }`.
- That covers 5 of row 53's 30 value cells (B53..AE53).
I tested this on a temp copy (scratchpad/skeptic_r7_formula_scope, removed afterwards), using us3_xlsx_poke.pl and a re-sum:
- With `<f>` added to B53 (ELECTORAL VOTES) and E53 (AYYADURAI): exit 0, all four outputs written, president_by_state.csv byte-identical. So the tool's header (line 32, "dies ... on any mismatch") over-claims too.
- Control, `<f>` in Y53 (TRUMP): exit 255, "the Total row's column 25 is a formula, with no typed national figure to hold the state rows to".
For 2012-2020 there is no gap. Their Table 2 Total rows hold A..G only (2012 row 57, 2016 row 56, 2020 row 56), so line 413's columns 2..7 cover every cell.

Why only a note: 2016/2020 sheet3.xml and 2024 sheet1.xml contain 0 `<f>` elements, so "(2016's, 2020's and 2024's carry none)" is true. The cells line 451 skips are cells the tool never reads, so a formula there cannot make any check self-referential.

**The skeptic's corrected fix.** Change the wording only (or widen the 2024 guard). No cell addresses are needed, which keeps the claim convention moot.

(a) president_returns.md reading 7: "2012's Table 2 fills two of its Total row's cells with SUM formulas — "All Others" over its state rows, "Total Vote" over that row's own three vote cells (the two nominees' typed figures and that All Others sum) — so neither is a national figure the FEC typed, and holding the state rows to them would prove only the reader: the tool holds 2012's state rows to Table 1 ..., and refuses a formula in any Total-row cell it reads (2016's, 2020's and 2024's sheets carry no formula)." Line 20: "a Total-row cell the tool reads that a formula fills answers to a typed national figure".

(b) us_returns_prep.pl:
- line 91: "(2012's Table 2 fills two Total-row cells with SUM formulas: All Others over its state rows, Total Vote over the Total row's own vote cells)"
- line 409: "A Total-row cell a formula fills is Excel's arithmetic on the sheet's own cells - holding the state rows to it proves the reader, not the source."
- line 32: "a Total-row cell the tool reads that a formula fills (Excel's arithmetic on the sheet's own cells)"

Alternative to the narrowing in (a) and (b): make line 451 loop over every cell of the 2024 Total row except column 1, so that "any Total-row cell" becomes true. Add the B53/E53 mutation above to us3_mutate.sh either way.

### 16. returns_2024.md's s786 claim that 'every page this file cites is saved under raw/ and registered' misses the FEC's 2020 PDF

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/returns_2024.md:73

**The scenario.** returns_2024.md:62 cites fec.gov/resources/cms-content/documents/federalelections2020.pdf. That PDF is kept out of tree (raw/out_of_tree.txt: usa_us3/fec/fec_federalelections2020.pdf). It is not under raw/, has no register row, and has no row in the s786 table, yet :73 says every cited page is saved under raw/ and registered except Alaska. president_returns.md:64 makes the same claim and adds '(register below)', although the 2024 NARA page it covers is registered in records_by_date.md.

**The fix proposed.** Add a table row: 'fec.gov/resources/cms-content/documents/federalelections2020.pdf | kept out of tree (raw/out_of_tree.txt); its tables are raw/returns/fec_federalelections2020.xlsx'. In president_returns.md:64, write '(register below; the NARA page in records_by_date.md)'.

**The skeptic's evidence.** The finding holds, and the first pass did not report it. A search of review786_first.md for "federalelections2020" finds nothing. First-pass finding 13 is the same kind of problem, but it covered only district_method_2024.md.

1. **The file cites the URL.** ElectionsData/usa/returns_2024.md:62 says: "(the 2020 edition exists at fec.gov/resources/cms-content/documents/federalelections2020.pdf; the 2024 path returns 403)".

2. **The claim at :73.** "Since §786 every page this file cites is saved byte for byte under `raw/` and registered — publisher, fetch time, basis, SHA-256 — in `president_returns.md` (the 2024 NARA page in `records_by_date.md`), but for the one host that refused:". The table at :75-88 has 12 rows, which are exactly the source-register URLs at :12-14. I pulled every URL out of lines 1-69. Only one is missing from the table: fec.gov/resources/cms-content/documents/federalelections2020.pdf.

3. **The PDF is not under raw/ and not registered.**
   - raw/returns/ holds fec_federalelections2020.xlsx but no PDF.
   - raw/returns/fetch_log.txt has no line for the PDF.
   - The register in president_returns.md (:72-160) has no row for it.
   - It exists only out of tree. raw/out_of_tree.txt:4 reads `e4e6d5ef... 12531393 usa_us3/fec/fec_federalelections2020.pdf`. The fetch log out of tree has `2026-10-05T16:15:26Z 200 12531393 fec_federalelections2020.pdf https://www.fec.gov/resources/cms-content/documents/federalelections2020.pdf`, and its sha256sum matches.
   - president_returns.md:50 ("Kept out of tree") mentions it only as one of "the three *Federal Elections* PDFs (2012, 2016, 2020 ...)".
   - So for this page, :73's "under raw/ and registered" is false.

4. **president_returns.md:64 is only partly as the finding says.** It reads: "every page it cites is saved (register below) except the Alaska page (BILLED)".
   - "saved" is true for the PDF, because it is saved out of tree.
   - "(register below)" does not cover it: there is no row below, and the mention is in the section above.
   - The NARA half of the finding is refuted. :70 already says "`raw/executive/` and `raw/records/` are registered in `records_by_date.md`", and records_by_date.md:215 and :296 register archives_electoral_college_2024.html.

5. **Next to it, also unchecked:** the same caveat's "the 2024 path returns 403" has no fetch-log entry, in tree or out. A search for "federalelections2024" under PoliSim-captures/sources finds nothing. Its "not yet posted" is supported by the saved index raw/returns/fec_election_results_and_voting_information.html, whose list of biennial volumes stops at 2022.

**Why this is a note:**
- The page is saved, and its digest is in the tree.
- Nothing reads it.
- The caveat is a side remark left from August.
- The saved FEC 2020 page (raw/returns/fec_federal_elections_2020.html) links /documents/4227/federalelections2020.pdf.
- The fault is only that the provenance sentence and its table claim too much.

**The skeptic's corrected fix.** The finding's fix adds a row to the table but leaves the sentence at :73 as it is. The new row would then contradict that sentence ("saved byte for byte under raw/ and registered").

1. **returns_2024.md:73.** Reword it to: "Since §786 every page this file cites is saved byte for byte — under `raw/` and registered (publisher, fetch time, basis, SHA-256) in `president_returns.md` (the 2024 NARA page in `records_by_date.md`), or, for the FEC's 2020 volume, kept out of tree by SHA-256 and bytes in `raw/out_of_tree.txt` — but for the one host that refused:".

2. **Add a table row:**
   "fec.gov/resources/cms-content/documents/federalelections2020.pdf | kept out of tree (`raw/out_of_tree.txt`; fetched 2026-10-05 from this URL, HTTP 200); its tables are `raw/returns/fec_federalelections2020.xlsx`, and the FEC's 2020 page (`raw/returns/fec_federal_elections_2020.html`) links it. The caveat's 'the 2024 path returns 403' was not re-fetched; the saved FEC index (`raw/returns/fec_election_results_and_voting_information.html`) lists the biennial volumes to 2022 only".

3. **president_returns.md:64.** Change it to: "every page it cites is saved (register below; the FEC's 2020 volume kept out of tree, above) except the Alaska page (BILLED)". The NARA pointer the finding proposes is optional, because :70 already sends raw/executive/ to records_by_date.md.

### 17. The plan's Built line says the 2024 key is 'not DERIVED from NARA', but the code derives it from NARA

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/docs/specs/USA_STAGE_PLAN.md:188

**The scenario.** us_returns_prep.pl:442-444 sets $sR = uc surname($nara{2024}{name}{R}) from NARA's [R]/[D] nominees. It then looks up "ELECTORAL VOTE: $sR (R)" and the vote column $sR. So each 2024 vote column's party comes from NARA, and the workbook's '(R)'/'(D)' labels must agree or the run dies. Reading 5's body says this correctly ('The tool takes the surnames from NARA's nominees and finds both forms'). The Built line's 'not DERIVED from NARA' would tell Elias his ruled derivation was dropped, when it was kept and cross-checked.

**The fix proposed.** Write: 'Read as built: the key is NARA's (its [R]/[D] nominees find the columns), and the workbook's own electoral-vote labels must agree (ELECTORAL VOTE: TRUMP (R)) - the workbook does carry party labels'.

**The skeptic's evidence.** The finding is not in the first pass's report (a search for "party key", "workbook's own", "DERIVED from NARA" and "surname" finds nothing on the 2024 key).

DOC (staged; the whole Built line is new against 5fe3c031), docs/specs/USA_STAGE_PLAN.md:188: "Read as built: the 2024 candidate-to-party key is the workbook's own (its electoral-vote columns carry the party), not DERIVED from NARA".

CODE, Tools/us_returns_prep.pl:
- 305-306 take the party from NARA's own tag: `if ($t[0] eq 'President' && ($t[1] // '') =~ /^(.+?) \[(D|R)\]$/) { $n{name}{$2} = $1; ...}` and the same for 'Main Opponent'. The saved raw/executive/archives_electoral_college_2024.html prints "President | Donald J. Trump [R]" and "Main Opponent | Kamala D. Harris [D]".
- 442: `my ($sR, $sD) = (uc surname($nara{2024}{name}{R}), uc surname($nara{2024}{name}{D}));`
- 443: `$evR = $col{"ELECTORAL VOTE: $sR (R)"}, $evD = $col{"ELECTORAL VOTE: $sD (D)"}, $vR = $col{$sR}, $vD = $col{$sD}`
- 444: `die ... unless $cE && $evR && $evD && $vR && $vD && $vT;`

The workbook's sharedStrings.xml (read with unzip -p) holds "ELECTORAL VOTE: TRUMP (R)" and "ELECTORAL VOTE: HARRIS (D)". Its vote columns are bare surnames ("TRUMP", "HARRIS", "STEIN", ...).

So the R and D vote columns, which carry no label, get their party from NARA's [R]/[D] nominees. That is the ruled derivation ("DERIVED from NARA's certificates and stated"). The workbook's electoral-vote labels must name the same party, or 444 dies, so the key is held to both sources.

The data flow is the reverse of 2012-2020. At 380-384 those years read the party from the FEC heading (`/\((D|R)\)/`) and hold the surname to NARA's nominee. That is "the FEC's own, held to NARA". 2024 is "NARA's, held to the workbook's labels". The Built line describes 2024 in the 2012-2020 shape and flatly denies NARA's part.

The same change's record, president_returns.md:39 (reading 5), says the mechanism correctly in its body: "The tool takes the surnames from NARA's nominees and finds both forms". Its heading ("the workbook's own") and its "The plan expected the key to be DERIVED from NARA" use the same departure framing, though.

Refutation tried: the ruling's "DERIVED" means inferred because no label is printed. Because the workbook does print the party, "not DERIVED" could be read as only "the ruling's premise failed". This does not save the line. The code still assigns the vote columns by NARA's tag, and a reader of the Built line alone learns that NARA plays no part.

Why only a note: no figure, check or allocator path depends on the wording. The error points the safe way, reporting a departure that did not happen. The ruled derivation is built and also cross-checked. The record states the mechanism.

One small inaccuracy in the finding: "each 2024 vote column's party comes from NARA" applies only to the two nominees' columns. The others are pooled without a party (445, "@cand").

**The skeptic's corrected fix.** At docs/specs/USA_STAGE_PLAN.md:188, replace "the 2024 candidate-to-party key is the workbook's own (its electoral-vote columns carry the party), not DERIVED from NARA" with:

"the 2024 workbook does carry the party, on its electoral-vote columns ("ELECTORAL VOTE: TRUMP (R)", "ELECTORAL VOTE: HARRIS (D)") though not on its vote columns, so the ruling's premise did not hold. Its derivation stands: the tool finds the two nominees' columns by the surnames of NARA's [R]/[D] nominees, and the run dies unless the workbook's own labels name the same party (the record's reading 5)."

Optional, for consistency: make reading 5's heading in ElectionsData/usa/president_returns.md:39 match its body. For example, "The 2024 candidate-to-party key - NARA's nominees, held to the workbook's own labels", with "The plan expected the key to be DERIVED from NARA, the workbook naming candidates by surname only" becoming "The plan expected the workbook to carry no party labels". Leave the plan's §6 sourcing row (line 683, "no party labels - matched to NARA's certificates") alone. It sits outside the diff and is the dated pre-build expectation; the Built line is where the correction belongs. No tool, CSV or catalog change is needed.

### 18. First-pass finding 14's fix is incomplete: the plan still counts 'its three CSVs'

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/docs/specs/USA_STAGE_PLAN.md:188

**The scenario.** Finding 14 quoted 'its three CSVs ... the four years' at this line. The skeptic's corrected wording dropped both counts: 'its CSVs (by year, by jurisdiction, by district) ... every year of the catalog'. The applied fix replaced the years but kept 'its three CSVs'. If the tool writes a fourth CSV, the sentence becomes wrong, not merely incomplete, which is the convention's test. DocumentClaimCheck does not read docs/specs.

**The fix proposed.** Write 'its CSVs (by year, by jurisdiction, by district)'.

**The skeptic's evidence.** I could not refute it. The fix for first-pass finding 14 is incomplete at this line.

1. What the line says now. The staged docs/specs/USA_STAGE_PLAN.md:188 (index and working tree are the same) reads: "**Built: `COMPLETED.md` §786** - the catalog `UsPresidentialReturns` and its three CSVs (by year, by jurisdiction, by district), ...; `ElectoralCollege.FromCatalog` feeds the allocator, which `GeneratedCatalogCheck` runs over every year of the catalog in the cheap bar."

2. What the fix changed. The original edit script (scratchpad us3_plan_edit.pl:6) wrote "...its three CSVs (by year, by jurisdiction, by district), ... runs over the four years in the cheap bar." The fix changed only "the four years". The count "three" is still there.

3. What the first pass asked for. Finding 14 included this line in its scenario (review786_first.md:624, title "...year range and counts"). Its proposed fix said "the generator's CSVs" (:626). Its skeptic's corrected fix (:661) says "the catalog `UsPresidentialReturns` and its CSVs (by year, by jurisdiction, by district)", without "three".

4. Why "three" breaks the rules.
- CLAUDE.md:37 sets the test: "the code can change freely and no document becomes wrong — only *incomplete*".
- CLAUDE.md:39 lists "a count" as DERIVED.
- CLAUDE.md:40 allows a DERIVED claim only GENERATED, REFERENCED or DELETED, and says "most counts in prose are" decoration.
- CLAUDE.md:41 exempts only COMPLETED.md.
- The plan's own head (USA_STAGE_PLAN.md:3, unchanged since 5fe3c031) says "this plan carries pointers, never line numbers or counts of code."

5. The count is set by code. Tools/us_returns_prep.pl:802 is `write_all("$usa/president_by_year.csv" => $csv_y, "$usa/president_by_state.csv" => $csv_s, "$usa/president_by_district.csv" => $csv_d, ...`. A fourth output would be a change to that code, which is exactly what the convention's test is about. After it, "three" would be wrong. The parenthetical alone would only be incomplete, which the test allows.

6. No check would notice. DocumentClaimCheck.cs:124 reads `Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly)`, so it never opens docs/specs.

7. Refutations I tried, and why each fails:
- **"A Built line is a dated record."** Only COMPLETED.md is exempt (CLAUDE.md:41). The first pass already held this same sentence to the convention when it fixed "the four years". The earlier Built lines (§782 at :169, §783 at :174) carry no counts of code.
- **"The first pass said to keep 'three CSVs'."** That was for GeneratedCatalogCheck.cs:517, a comment sitting over that method's own three hard-coded `ReadUsCsv` calls. A sentence in a spec has no such code beside it.
- **"The fix pass kept it on purpose."** No note in the scratchpad records that decision. rec786.md is the draft COMPLETED record, which is exempt.

8. Severity: note. The sentence is true today (the tool writes exactly the three files at :802). It has no runtime effect and no check reads it. It is one redundant numeral next to its own three-item list, and it is left over from the minor finding 14, at the site whose skeptic called this plan line "the weakest of the four".

9. The same count elsewhere, taken up by neither pass:
- ElectionsData/.gitattributes:4: "the generator's three CSVs"
- ElectionsData/usa/president_returns.md:10: "the three as C# literals" and "re-reads the three CSVs"
- ElectionsData/usa/president_returns.md:12: "keeps the three CSVs"
- Tools/us_returns_prep.pl:19, and the template at :778, which writes UsPresidentialReturns.cs:4: "re-reads the three CSVs"

**The skeptic's corrected fix.** At docs/specs/USA_STAGE_PLAN.md:188, change "its three CSVs (by year, by jurisdiction, by district)" to "its CSVs (by year, by jurisdiction, by district)". That is the first-pass skeptic's wording (review786_first.md:661). Leave GeneratedCatalogCheck.cs:517 as it is, as the first pass ruled.

Optional, the same class:
- ElectionsData/.gitattributes:4: change "the generator's three CSVs" to "the generator's CSVs".
- president_returns.md:10: change "the three as C# literals" to "the CSVs as C# literals", and "re-reads the three CSVs" to "re-reads the CSVs".
- president_returns.md:12: change "keeps the three CSVs" to "keeps the CSVs".
- Tools/us_returns_prep.pl:19 and the template at :778: change "re-reads the three CSVs" to "re-reads the CSVs". The template change means re-running the tool so that UsPresidentialReturns.cs:4 follows; the CSVs stay byte-identical.

### 19. A paraphrase is labelled 'the saved hearing page's words'

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/district_method_2024.md:104

**The scenario.** Line 104 reads '(75+ testifiers, the majority opposed to one or both measures - the saved hearing page's words, §786)'. The page (37216) reads 'over 75 people testified — the majority of them in opposition to one or both measures'. The same pass corrected two other paraphrases that were presented as the page's words (:113-115). The verbatim form is already in the s786 bullet at :145.

**The fix proposed.** Quote the page: 'the majority of them in opposition to one or both measures'. Or drop 'the saved hearing page's words'.

**The skeptic's evidence.** I could not refute it. The label is new in the fixes, so this is a flaw in how the optional item of first-pass finding 12 was applied. It is not a repeat of that finding. Finding 12 (review786_first.md:577) proposed the paraphrase "75+ testifiers, the majority opposed to one or both measures". The fixer adopted it and added " - the saved hearing page's words, §786". Nothing in the first pass proposed calling that wording the page's words.

1. The record, district_method_2024.md:103-104 (staged; the index equals the working tree): "Hearing 30 January 2025 (75+ testifiers, the majority opposed to one or both measures - the saved hearing page's words, §786)". There are no quotation marks, and the label follows the whole parenthetical.

2. The saved page is raw/district/nebraska_unicameral_update_37216.html. Its sha256 is 22c009f3...e6d904, which matches SHA256SUMS.txt:29. It reads: "In a hearing lasting more than five hours, over 75 people testified — the majority of them in opposition to one or both measures." The dash is an em dash, E2 80 94. Byte counts with grep -cF on the page:
   - "75+": 0
   - "75+ testifiers": 0
   - "majority opposed": 0
   - "opposed to one or both": 0
   - "the majority opposed to one or both measures": 0
   - "over 75 people testified": 2 (the HTML text and its JSON copy)
   - "the majority of them in opposition to one or both measures": 2
   Only the tail "one or both measures" is verbatim. "75+ testifiers" and "opposed to" are the first pass's own wording.

3. The record's own usage makes "the page's words" mean verbatim:
   - :113-115, in the same paragraph, says "both quotations corrected to the saved page's words at COMPLETED.md §786, the earlier ones having paraphrased it". Those corrections are in quotation marks.
   - president_returns.md:66 says the 39341 quotations "were not the page's words".
   - The first pass itself (review786_first.md:715) described the Unicameral Update fix as having "swapped paraphrases for the page's actual words".
   So :104 applies that label to the kind of paraphrase the same paragraph says it removed.

4. The verbatim form is already in the file, in quotation marks, at :145 (the LB3 bullet). It matches the page byte for byte, em dash included (checked with od -c).

Why it stays a note:
- No fact is wrong. "75+" follows from "over 75", and "opposed to" means the same as "in opposition to".
- No tool, check, CSV or generated file reads the line.
- COMPLETED.md has no §786 yet, so no second record repeats the label.
- This is the same class as first-pass finding 16 (a non-verbatim quotation, graded note). It is milder, since :104 has no quotation marks, and it sits in a file the US-3 ruling brings to the raw-page standard.

**The skeptic's corrected fix.** Edit ElectionsData/usa/district_method_2024.md:103-104 and nothing else. There are no raw page, CSV, tool or code changes. Take one of two options.

(a) Quote the page's whole clause, copying the bytes of :145, em dash included: `Hearing 30 January 2025 ("over 75 people testified — the majority of them in opposition to one or both measures" - the saved hearing page's words, §786); advanced to General File; ...`. This is better than the finding's first option, which quotes only the second half and leaves the paraphrase "75+ testifiers" next to the quotation under the same label.

(b) Keep the paraphrase and stop calling it the page's words: `Hearing 30 January 2025 (75+ testifiers, the majority opposed to one or both measures - paraphrasing the saved hearing page, whose words the Raw pages bullet quotes, §786)`.

Either option leaves :113-115's "corrected to the saved page's words" claim true across the whole paragraph.

### 20. The 'Not reached' statuses, including the new In re Guerra line, are logged only out of tree, and the record names no log

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/president_returns.md:57

**The scenario.** The In re Guerra attempt (https://www.courts.wa.gov/opinions/pdf/953478.pdf, a 3623-byte error page at 20:46:23Z, with a NOTE at 20:46:37Z) is logged only in PoliSim-captures/sources/usa_us3/review786/fetch_log.txt. Alaska's 405 and the two 000s are only in .../cited2024/fetch_log.txt, and NARA's three 429s only in .../nara/fetch_log.txt. None is in the in-tree raw/*/fetch_log.txt, and the record names none of these logs. A reader with only the repo cannot check them (CLAUDE.md:111). This is the standard first-pass finding 17 applied to the FEC redirect, which got an in-tree NOTE line.

**The fix proposed.** Add house-form NOTE lines (file column '-') to raw/returns/fetch_log.txt and raw/district/fetch_log.txt for each attempt not reached. Or name the out-of-tree logs in the 'Not reached' section.

**The skeptic's evidence.** I could not refute any premise. Every one checks out on disk, and this is not in the first pass's report: its 24 confirmed findings and the refuted list contain nothing on it. The In re Guerra line is also new in this pass (fix 9).

Where the attempts are logged (read-only checks):
- PoliSim-captures/sources/usa_us3/review786/fetch_log.txt has `2026-10-05T20:46:23Z 200 3623 wa_courts_in_re_guerra_953478.pdf https://www.courts.wa.gov/opinions/pdf/953478.pdf` and `20:46:37Z NOTE - wa_courts_in_re_guerra_953478.pdf (discarded: the URL served the courts site's error page with HTTP 200, 3623 bytes ...)`.
- cited2024/fetch_log.txt has `19:45:39Z 405 2301 alaska_elections_rcv.html`, `19:46:21Z 000 0 ...statute=32-710` and `19:46:42Z 000 0 ...statute=32-1038`.
- nara/fetch_log.txt has three `429 620 wayback_archives_electoral_college_allocation_20201016.html` lines (16:14:10Z, 16:17:05Z, 16:23:37Z) and a NOTE at 16:23:58Z.
- None of these is in the four in-tree logs (raw/returns, raw/district, raw/rules, raw/apportionment), all of them new in this change. grep for 953478, courts.wa.gov, 405, 429 and 000 finds nothing.

How the in-tree logs were built: the lines were copied byte for byte from the out-of-tree logs, keeping only files saved in tree. raw/returns/fetch_log.txt keeps review786's Chiafalo line (20:46:21Z), and raw/district/fetch_log.txt keeps its nebraska_sos_elections line (20:46:24Z). The Guerra line between them (20:46:23Z) and its NOTE were dropped. The only non-200 row in tree is the FEC NOTE at 16:13:04Z, which is first-pass finding 17's fix.

The record names no log for any of this:
- `git grep --cached` for usa_us3, cited2024, review786 and out_of_tree finds only president_returns.md:50 and out_of_tree.txt. Line 50 names `PoliSim-captures/sources/usa_us3/` only as the home of the kept files, and out_of_tree.txt lists files, not logs.
- Lines 54-57 state 000, 405, "429 three times" and "the URL tried served the court site's error page" with no source.
- Reading 3 (line 37) says Guerra "was tried at a guessed URL" but never gives the URL, even though option (b) is put to Elias.
- returns_2024.md:87 ("HTTP 405 on 2026-10-05") and district_method_2024.md:142 ("curl status 000") also cite no log.

This breaks the repo's own house form. The USA's own raw/records/fetch_log.txt (US-2) keeps its `429 620 wayback_...` and `403 151` rows in tree, with a `NOTE ... discarded` line. raw/debt_limit/fetch_log.md keeps `403 [NOT KEPT]` rows. France's log says "attempts kept in this log", and Italy's records_by_date.md:300 says "Fetched and not kept (all in the fetch log)". Past reviews accept out-of-tree evidence only when the citation names it (Reviews/2026-10-05_s780...:706, "mark each citation 'out of tree: PoliSim-captures/...'").

Limits on the claim, which keep it a note:
- CLAUDE.md:112 is met: each line is BILLED with what is missing and why.
- The evidence is written down on disk out of tree, unlike finding 17's transcript-only 302. A reader who searches under line 50's folder can find it, so "cannot check" is too strong; "nothing points to it" is accurate.
- No tool, CSV, catalog or check reads fetch logs or depends on these pages.

**The skeptic's corrected fix.** Do either one; both match the house form.

(A) Copy each failed attempt's own line byte for byte from the sweep's log into the in-tree log of the group it belongs to, then its NOTE, as US-2's raw/records/fetch_log.txt does:
- Alaska's 405 goes in raw/rules/fetch_log.txt, beside the other rules pages returns_2024.md cites, not raw/returns.
- The two nebraskalegislature.gov 000 lines go in raw/district/fetch_log.txt.
- NARA's three 429s with their 16:23:58Z NOTE, and the Guerra 20:46:23Z line with its 20:46:37Z NOTE, go in raw/returns/fetch_log.txt.

Rows that name unsaved files are safe for the scratchpad register generator, which looks up only names that appear in SHA256SUMS.txt. A NOTE must never carry a saved file's name in column 4: the last row wins, so the saved file would get a non-200 status and the run would die.

(B) Add one sentence to the 'Not reached' section of president_returns.md: each attempt is logged with its time and status in the sweep's out-of-tree logs, PoliSim-captures/sources/usa_us3/cited2024/fetch_log.txt (Alaska, the Legislature), nara/fetch_log.txt (the 2010-census capture) and review786/fetch_log.txt (In re Guerra).

Either way, write the Guerra URL tried (https://www.courts.wa.gov/opinions/pdf/953478.pdf) into line 57 or reading 3, so Elias can judge option (b). None of this changes a hash, the catalog or the tool.

### 21. The mutation list: four of the 25 edit the tool, not 'a copy of its inputs', and the suite is not in the repo

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/ElectionsData/usa/president_returns.md:31

**The scenario.** I re-ran us3_mutate.sh on my own copy: the control was byte-identical and all 25 were caught, so the count is right. But four of the runs change the tool itself, not its inputs: 'the declared misprint withdrawn' edits %typo; R12 swaps $pdftotext for a wrapper; R15 empties %typed; R16 crosses the Table 1 row binding. R15 and R16 are described as input states ('formula Total cells without their typed counterpart', 'Table 1's nominee rows crossed'). The suite (us3_mutate.sh, us3_xlsx_poke.pl) lives only in the session scratchpad. R-K9's second session therefore cannot re-run 'twenty-five mutations' from the repo (CLAUDE.md §576).

**The fix proposed.** Write 'On each of twenty-five mutations of such a copy, four of them to the tool itself (...)'. Commit the suite under Tools/, or say it was a one-off and not kept.

**The skeptic's evidence.** CONFIRMED on its facts, with one premise wrong. Not in the first pass: review786_first.md mentions mutations only in finding 3 ("Line 28's mutation list names only...") and finding 7 ("Add M4 to the record's mutation list"). Neither raises tool-side mutations or the suite's location.

(1) Four of the 25 edit the tool's copy. The suite's setup copies the tool along with the inputs (us3_mutate.sh:8, `cp "$SRC/Tools/us_returns_prep.pl" "$T/Tools/"`). Four runs then edit that copy:
- us3_mutate.sh:34 `sed -i "s/my %typo = ('2020 R' => 'Trunp');/my %typo = ();/" Tools/us_returns_prep.pl` hits tool line 369 `my %typo = ('2020 R' => 'Trunp');`.
- R12 (:49-53) writes pdft.sh and seds `^my \$pdftotext = .*` to point at it. That is tool line 62 `my $pdftotext = -x '/mingw64/bin/pdftotext' ? ...`.
- R15 (:57) sets `my %typed = ();`. That is tool line 412 `my %typed = $year == 2012 ? map { $_ => 1 } 4 .. 7 : ();`.
- R16 (:58) sets `$row{$p eq q(D) ? q(R) : q(D)} = $r ...`. That is tool line 421, `for my $p ('D', 'R') { $row{$p} = $r if index($a, "$nara{2012}{name}{$p} (") == 0; }`.
- The other 21 touch only inputs: the TSV, state_ev_2024.csv, a raw page re-summed by `resum`, SHA256SUMS.txt, or the 2020 Maine xlsx through us3_xlsx_poke.pl (R14).

The record's line 31 says "a copy of its inputs reproduces the repository's bytes. On each of twenty-five mutations of such a copy it failed". It words R15 and R16 as data states: "2012's formula Total cells without their typed counterpart" and "Table 1's nominee rows crossed". The inaccuracy is about mechanism only:
- R16's binding is by label (line 421), so crossed rows in the input give the same mismatch (line 426).
- R15's refusal is generic over years and Total-row columns (lines 413 and 451).
- So no reader reaches a wrong verdict about how the tool behaves.

(2) The suite is not kept anywhere a later reader can find it.
- `git ls-files Tools` (index included) lists only us_returns_prep.pl for this work. The staged stat has 16 non-raw paths and no suite.
- `PoliSim-captures/sources/usa_us3/review786/` holds fetched pages only (Chiafalo PDF, Nebraska SOS page, fetch_log).
- `PoliSim-captures/logs/` has cheap786, sim786, sab786, n786a and n786b, and no mutation log.
- grep 'mutat' over the staged docs and the tool hits only president_returns.md:31.
- Precedent records one-off proofs as "never committed" (COMPLETED.md lines 29382, 29559, 29590, 29604, 29997, 33463, 34021), or names their logs (§545 `logs/pa_sentinel_mut.log`; §773 "n773m1, n773m2"). Line 31 does neither.

(3) The finding's premise is wrong. R-K9 is defined at president_returns.md:3 as "for every parsed figure ... a re-run of the tool over the saved pages, for the READ rows a second reading of the certificates". Re-running the mutations is not part of R-K9. §576's long form (docs/archive/CLAUDE_HEAD_LONGFORM.md:205) also says "the scratchpad is for intermediate results a pass throws away". So committing the suite is optional. The real gap is that the record does not say which of the two was done.

Severity stays note: wording and a provenance pointer. Nothing reaches the catalog or a check.

**The skeptic's corrected fix.** In president_returns.md line 31, move the four tool-side mutations out of the "mutations of such a copy" group into their own group, without adding new counts. For example, end the sentence: "...; and, made to the tool's own copy rather than its inputs: its declared misprint withdrawn, its text extractor wrapped to exchange Nebraska 2020's district headings, its declaration that Table 1 types 2012's formula Total cells withdrawn, and its binding of Table 1's nominee rows crossed." Then add one clause saying where the proof lives. Option 1: commit us3_mutate.sh and us3_xlsx_poke.pl under Tools/ in this commit and name them there (§576's test: the next pass that edits the tool would have to rewrite them). Option 2: say, as the record's precedent does, that the suite was a one-off and never committed, and keep its output under PoliSim-captures/logs/ with the log named. Drop the R-K9 rationale. Line 3 defines R-K9's re-verification as a re-run of the tool and a second reading of the certificates, and the mutations are no part of it.

### 22. Finding 1 partly closed: in split years a same-winner district transposition (NE-1/NE-3, 2020 and 2024) still passes, but reading 9 and the tool's comment imply split years hold every label

- **Lens:** completeness - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/president_returns.md:43

**The scenario.** Measured on a copy of the inputs, never the repo. A pdftotext wrapper exchanged the 2020 canvass book's 'Congressional District 1' and '3' headings; both districts were carried by Trump. Result: exit 0, no MISMATCH, and president_by_district.csv written with 2020,NE,1,65854,222179 and 2020,NE,3,132261,180290. The repository has NE-1 132261/180290 and NE-3 65854/222179. The 2024 book behaves the same: it writes 2024,NE,1,70301,238245 and 2024,NE,3,136153,177666. The new NARA-notes check (Tools/us_returns_prep.pl:667-682) compares winners only, and NE-1 and NE-3 are both Republican. Yet reading 9 says 'Where Nebraska split (2020, 2024) NARA's notes name which ticket carried which district and the tool holds the districts to them; where it did not (2012, 2016), which figures stand under which label rests on the book's headings alone'. The comment at us_returns_prep.pl:665-666 draws the same contrast. So a re-verifier, or US-4 when it swings districts, would take the 2020/2024 NE-1/NE-3 labels as held. The first-pass skeptic's corrected fix (item 3) asked the record to state both uncovered kinds: districts with no note, and transpositions that keep the winners. Only the first is stated. The Maine READ half of that item is now closed by the text-layer check. Realism is as in the first pass: the PDFs are hash-pinned, so this needs a different extractor.

**The fix proposed.** Wording only, in reading 9 and in the comment at us_returns_prep.pl:665-666. For example: 'NARA's notes hold each district's winner. Which of one ticket's districts stands under which label (NE-1 and NE-3 in 2020 and 2024) rests on the book's headings, as every label does where the state did not split (2012, 2016).' The comment changes no output; rerun the tool in a temp copy to confirm the same bytes.

**The skeptic's evidence.** I could not refute it. I reproduced the gap exactly, and the wording over-implies as described.

How Nebraska's district labels are bound and checked (Tools/us_returns_prep.pl):
- :512-514: a district's label comes only from the last 'Congressional District N' heading seen. `$dist{$year}{NE}{$cd} //= {...}` binds the next Total line to that label.
- Every later check is blind to a permutation that keeps the winners:
  - :516-521, the key set '1 2 3' and the sums against the FEC state row;
  - :654-659, the tie and others checks;
  - :671-680, the new NARA-notes check, which compares winners only. `%won` holds D/R per district, and the note gives R [1,3], D [2], so `@against` = `@named` = '2';
  - :693-697, the split, which counts winners only.
- Downstream code is identity-blind as well:
  - ElectoralCollege.cs:65-77 keeps the district winners as a plain list.
  - GeneratedCatalogCheck.cs:551-571 compares the CSV with the catalog, and the same run writes both.
  - Nothing reads district_method_2024.md's typed NE-1 and NE-3 rows (lines 61 and 63); grep finds no reader.

Reproduced in scratchpad/skep_ne13_m2, never in the repo:
- Control (separate copy): it reproduced the repo's bytes.
- Mutation: a pdftotext wrapper swapped 'Congressional District 1' and '3' at line start.
- Result: rc=0, 0 MISMATCH lines, all four outputs written.
- president_by_district.csv: 2020,NE,1,65854,222179; 2020,NE,3,132261,180290; 2024,NE,1,70301,238245; 2024,NE,3,136153,177666.
- The repo has 2020 NE-1 132261/180290 and NE-3 65854/222179; 2024 NE-1 136153/177666 and NE-3 70301/238245.
- 2012 and 2016 use 'One' and '01' headings, so the wrapper left them unchanged.

The wording:
- Reading 9 (president_returns.md:43) says: 'Where Nebraska split (2020, 2024) NARA's notes name which ticket carried which district and the tool holds the districts to them; where it did not (2012, 2016), which figures stand under which district label rests on the book's headings alone.' Read by contrast, 2020/2024 labels are held. That is true for NE-2 only; NE-1 and NE-3 rest on the headings alone.
- The comment at :665-666 draws the same contrast, with 'then' scoping the caveat to the case with no split.
- First-pass finding 1, skeptic's corrected fix item 3 (report line 49), named both uncovered kinds: districts with no NARA sentence, and transpositions that keep the winners.
  - A grep of president_returns.md, district_method_2024.md, returns_2024.md, the tool and the plan for transpos/keep the winners/headings alone finds only the no-split sentence and the comment.
  - The Maine half is closed by the text-layer check (:615-627, mutation R2 caught).
  - Maine 2020/2024 have two split districts, so any swap flips a winner.
- Before the fix, the first-pass skeptic recorded 'Nothing over-claims'. The new contrast introduced the implicature.

Why only a note:
- The explicit lists are accurate: the tool header (:42-43) and 'What the tool proves' (md:25) speak of each district's winner.
- The PDFs are hash-pinned, and -raw follows the content-stream order. With xpdf 4.06 the binding is right, so this needs a different extractor.
- No consumer reads labels yet. US-4 (USA_STAGE_PLAN.md:196-199) will swing districts from the previous election and report misses, and there labels matter.

Housekeeping:
- Two scratch copies remain: scratchpad/skep_ne13_x7q and scratchpad/skep_ne13_m2.
- The safety check blocked one rm with a relative glob inside skep_ne13_x7q; it was not run.
- The repo is untouched: git status is clean apart from staged files, and the district CSV digest is still d2a5038b... = DistrictSourceDigest.

**The skeptic's corrected fix.** Wording only, as proposed.

1. Reading 9 (president_returns.md:43). Replace the 'Where Nebraska split ... headings alone.' sentence with: 'NARA's notes (2020, 2024) name which ticket carried which district, and the tool holds each district's winner to them. That holds a winner, not a label: which of the districts one ticket carried stands under which label rests on the book's headings alone. In 2020 and 2024 that is the two the notes give the same ticket; in 2012 and 2016, when Nebraska did not split, it is all three.'

2. The comment at Tools/us_returns_prep.pl:665-666. Make it say the notes hold winners, not labels: '(the notes hold each district's winner, not its label: among the districts one ticket carried - every district, where a state did not split - which figures stand under which label rests on the canvass's headings, the workbook's sheet names, or - for the READ rows - the certificate's text layer above)'. Keep it generic. Do not name NE-1/NE-3 in the comment, per the claim convention.

3. Rerun the tool on a temp copy and confirm the same bytes. The catalog pins no digest of the tool, so a comment edit cannot change its outputs.

Optional hardening, not needed for US-3, best done before US-4 reads labels: NARA's Nebraska certificates of ascertainment, kept out of tree per raw/out_of_tree.txt, print each slate's 'First/Second/Third Congressional District Total Votes Received: N' for all four years. The 2012 text layer is noisy: '140 976', 'Recei ed', 'TotaJ'. Bringing them in-tree and holding each Nebraska district's figures under its label to that text layer, as the Maine READ check does, would hold identity in every year. Reading 9 could then drop the caveat.

### 23. Fixes 12 and 13 together: LR24CA's 2026 ballot status is now on a saved official page, but both records still call it unfetched press

- **Lens:** completeness - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/district_method_2024.md:146

**The scenario.** The finding-13 fix saved raw/district/nebraska_sos_elections.html: fetch_log 2026-10-05T20:46:24Z, sha256 a6a09dac... equal to SHA256SUMS, register row president_returns.md:142. Its 'Ballot Measures for 2026 General Election' list names 'Initiative Nos. 440-442' and 'Constitutional Amendment passed by the Nebraska Legislature for the 2026 General Election LR19CA', and nothing else. So district_method_2024.md:107's 'LR24CA ... is not on the 2026 ballot' is shown, by omission, on a saved official page. Yet :146 ('Not fetched: the press named above - ..., the ballot status of LR24CA, the 2025 initiative') and president_returns.md:60 ('... LR24CA's ballot status) were not fetched') still rest it on press. This is the under-claim first-pass finding 12 corrected for the hearing (graded minor there). It now recurs for a fact the newly saved page carries. A re-verifier would hunt for press, or BILL a fact the repo already holds.

**The fix proposed.** In the LB3 bullet (:145), add that the saved 2026 elections page lists the general election's ballot measures - Initiative Nos. 440-442 and one legislative amendment, LR19CA - in its own words, so LR24CA is not on the ballot. Drop 'the ballot status of LR24CA' from :146 and from president_returns.md:60. Keep the cloture date and the initiative's withdrawal as press: the page names the initiatives by number only.

**The skeptic's evidence.** The saved page lists the 2026 ballot measures, and LR24CA is not among them.
- raw/district/nebraska_sos_elections.html:1221 reads `<strong>Ballot Measures for 2026&nbsp;General Election</strong><br><em>Initiative Nos. 440-442</em>`. Lines 1222-1225 are two 'Informational Pamphlet' links.
- Line 1226 reads `<strong>Constitutional Amendment&nbsp;passed by the Nebraska Legislature for the 2026&nbsp;General Election</strong>`, followed by LR19CA alone. 'Still have questions?' comes next.
- No other initiative, LR or ballot-measure text appears anywhere on the page. The 'hidden' hits are field labels, a skip link and a message fallback, so nothing is concealed.
- The page is saved and registered: sha256 a6a09dac...0de1 matches SHA256SUMS.txt:23, fetch_log.txt:33 has `2026-10-05T20:46:24Z 200 70827 ... https://sos.nebraska.gov/elections`, and the register row is president_returns.md:142.

Two saved pages say how LR24CA could reach that ballot. Both Unicameral Update articles say, word for word: "If approved by the Legislature, LR24CA, sponsored by Sen. Myron Dorn of Adams, would place the question of reinstating winner-take-all on the 2026 general election ballot." That is 37216:878 (published 2025-01-31) and 39341:900 (published 2025-07-11). 39341 adds that it "was considered by the ... Committee but was not advanced". So from saved official pages alone, LR24CA is not on the 2026 ballot: the one legislative amendment listed is LR19CA. This rests on omission from the list, not on words that name LR24CA.

The records still place this fact on the press list:
- district_method_2024.md:107 says LR24CA "is **not** on the 2026 ballot".
- :145 cites the SOS page only for "carries no notice of a change to the electors".
- :146 reads "Not fetched: the press named above - the cloture's date (8 April 2025; BillTrack50 lists a motion ... without calling it cloture), the ballot status of LR24CA, the 2025 initiative; and FindLaw's reproduction of §32-1038, whose place the saved capture of the Legislature's own page takes". On the same line the author marks where a saved page bears on a fact (the cloture's date) or replaces a source (FindLaw). Saying nothing like that for LR24CA reads as press-only.
- president_returns.md:60, under "Not reached — BILLED, or not fetched", says: "... LR24CA's ballot status) were not fetched".
- :66 and the use column at :142 credit the SOS page only for "no notice of a change to the electors".

The first pass covered this fact before the page existed:
- In finding 12 (review786_first.md:566 and :574) the skeptic wrote "Its 2026 ballot status is not on any saved page" and kept it on the press list.
- Finding 13's fix (:613) said to fetch the page and, "If the page now says something different, correct :107-108". It gave no instruction for a page that confirms :107. So fix 12's reason for keeping the fact on the press list lapsed when fix 13 landed.
- COMPLETED.md has no s786 entry, staged or in the working tree, that records keeping the fact as press.

Refutation attempts that failed:
- :146 and :60 are literally true, since the press was not fetched. But they list the fact as the press's content, and the line's own cloture and FindLaw pattern tells the reader no saved page carries it.
- The evidence is by omission. But :145 already accepts absence on this same page ("no notice of a change"), and here the list enumerates the measures and the route to the ballot is on two saved pages.
- RawSources is not relevant: it lists only the tool's reads, and BillTrack50 and the Unicameral pages are absent from it too.

Why a note: no figure, CSV, catalog, check or verdict depends on this; :107 states the fact correctly; and nothing false is asserted. Finding 12 was graded minor because its line said the facts "rest on the press", which was false. Here the under-claim is only implied, and the fact rests on omission rather than on the page's words.

**The skeptic's corrected fix.** Records only. No change to any raw page, CSV, the tool or the C#.

1. district_method_2024.md:145 (the LB3 bullet). After the SOS page, add that it lists the 2026 general election's ballot measures in its own words: 'Initiative Nos. 440-442', and under 'Constitutional Amendment passed by the Nebraska Legislature for the 2026 General Election', LR19CA alone. Note that the page's non-breaking spaces become plain spaces if quoted. Also add that LR24CA could reach that ballot only 'If approved by the Legislature' (the saved 37216 and 39341 pages, both in those words). Its absence from the list therefore shows it is not on the 2026 ballot. Say plainly that this rests on omission and that the page does not name LR24CA.

2. Drop 'the ballot status of LR24CA' from :146, and 'LR24CA's ballot status' from president_returns.md:60.

3. Extend president_returns.md:142's use column, and the SOS-page clause at :66, to say the page also gives LR24CA's 2026 ballot status, because LR24CA is not among the listed measures.

4. Keep these on the press list:
- The cloture's date.
- The 2025 initiative and its withdrawal. The page gives the initiatives by number only, so it cannot show what Nos. 440-442 are.

Do not extend the SOS page's support to 'never floor-debated' at :107. The page shows only that LR24CA was not passed for the 2026 ballot. 39341's 'not advanced' (July 2025) and the press remain that phrase's sources.

### 24. Fix 13's narrowed claim at district_method_2024.md:140 still misses two cited, unsaved pages

- **Lens:** completeness - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/district_method_2024.md:140

**The scenario.** :140 now reads 'the pages this file cites - all but those the last bullet names as not fetched - are saved byte for byte under raw/'. The file cites, at :37-38, the Internet Archive captures 'web/20241212015406/…32-1038' and 'web/20260216110605/…32-710' as the source of its verbatim statute quotations. Neither is saved: the saved captures are 20260206163923 and 20260217082722 (raw/district/fetch_log.txt:24, :26). The last bullet (:146) names only the press and FindLaw. Only the first bullet (:142) says the saved copies are 'not the 2026-02-16 and 2024-12-12 captures quoted above'. So the exception clause is literally false for two cited pages. It is the overstatement of first-pass finding 13 in narrower form.

**The fix proposed.** Name the two August captures in :146, for example: 'the captures quoted above, web/20241212015406 (§32-1038) and web/20260216110605 (§32-710), were not fetched; the saved 2026-02-06 and 2026-02-17 captures carry the quoted words'. Or change :140 to 'all but those named below as not saved'.

**The skeptic's evidence.** The finding is accurate as stated, and it shows that first-pass fix 13 is incomplete. I could not refute it. Lines are from the staged ElectionsData/usa/district_method_2024.md, where the working tree equals the index.

1. Line :140 limits its exceptions to one bullet: "the pages this file cites - all but those the last bullet names as not fetched - are saved byte for byte under `raw/`".

2. Lines :36-38 cite two specific captures as the source of the verbatim quotes: "Both quotes are verbatim Internet Archive captures of those exact primary URLs (`web/20241212015406/…32-1038`, `web/20260216110605/…32-710`)". The record treats these as cited sources. Line :142 itself calls them "the 2026-02-16 and 2024-12-12 captures quoted above" and sets them apart from "the saved copies".

3. Neither capture is saved.
- raw/district/fetch_log.txt:24 has `web/20260206163923id_/…32-1038` and :26 has `web/20260217082722id_/…32-710`.
- SHA256SUMS.txt:32-33 and president_returns.md:151-152 register only those two captures.
- A search for 20241212015406 or 20260216110605 finds them only at district_method_2024.md:37-38. They appear nowhere else in ElectionsData/usa (raw/out_of_tree.txt included) or in PoliSim-captures/sources/usa_us3/.
- The scratchpad's us3d_wb_lookup.out shows how the October pass chose its captures: it asked the Wayback availability API for the "closest" snapshot, got 20260217082722 and 20260206163923, and fetched those (us3d_wb_fetch.sh). The cited timestamps were never requested.

4. The last bullet (:146) names only "the press named above … and FindLaw's reproduction of §32-1038". The two captures are disclosed only in the FIRST bullet (:142). So :140's exception clause is literally false for two pages the file cites.

5. Why the fix fell short: first-pass finding 13's skeptic proposed "except those named below", which would have taken in :142. The fix as staged narrowed it to "the last bullet" instead.

Why only a note:
- The substitution is stated in full one bullet below :140.
- The quoted words are on the saved pages. I checked with tags stripped: the §32-710 sentence is on wayback_nebraska_legislature_32_710_20260217.html, and the §32-1038(1) sentences are on wayback_nebraska_legislature_32_1038_20260206.html.
- No figure, code path or check is affected. Unlike finding 13, where the pages were named nowhere, here the only fault is :140 pointing the reader to the wrong bullet.

Side observations (no action needed):
- The same strict reading also makes :24/:28's live nebraskalegislature.gov URLs exceptions. Those are disclosed only at :142 too.
- §32-714's content claim at :8-9 is backed by the saved 2024 canvass book: "Faithless electors who vote contrary to how they were elected immediately vacate their office and are replaced. §32-713, §32-714".

**The skeptic's corrected fix.** Widen :140 so its exception clause covers every bullet that names a gap. For example: "Since §786 the pages this file cites - all but those the bullets below name as not saved or not fetched - are saved byte for byte under `raw/` ...". That one change takes in :142's disclosure, which covers the 2024-12-12 and 2026-02-16 captures and the unreachable live nebraskalegislature.gov URLs, as well as :146's.

The alternative is to keep :140 and add the captures to :146: "... and the two Internet Archive captures the statutes section quotes from (`web/20241212015406/…32-1038`, `web/20260216110605/…32-710`), not fetched: the saved 2026-02-06 and 2026-02-17 captures carry the quoted words in their place."

If the second form is used, also add a matching clause to president_returns.md's "Not reached" line for nebraskalegislature.gov (:54), the way the FindLaw line (:59) was added. Do not call these the "August captures": they are dated 2024-12-12 and 2026-02-16 and were only cited in August.

### 25. Fix 20: reading 3 attributes the Chiafalo syllabus's sentence to 'the opinion'

- **Lens:** completeness - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/president_returns.md:37

**The scenario.** Reading 3 says 'The Supreme Court's opinion in Chiafalo v. Washington ... does: "Three Washington electors, Peter Chiafalo, Levi Guerra, and Esther John (the Electors), violated their pledges to support Hillary Clinton"'. In the saved PDF (sha256 4491bf0e..., pdftotext -raw) that sentence is in the Syllabus on page 1. The PDF's own headnote says the syllabus 'constitutes no part of the opinion of the Court but has been prepared by the Reporter of Decisions'. The opinion's own words, at slip op. 6, are 'Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John (the Electors). All three pledged to support Hillary Clinton in the Electoral College.' The second and third quotations ('the three Electors voted for Colin Powell for President'; 'Only seven electors across the Nation cast faithless votes') are the opinion's. The substance holds, since the opinion names the same three; only the source of the first quotation is wrong. That matters in a record held to the raw-page standard, and the first-pass skeptic's evidence kept 'Syllabus' and 'Opinion of the Court' apart.

**The fix proposed.** Attribute each quotation, for example: 'the slip opinion's syllabus: "Three Washington electors, ..."; the opinion (slip op. 6-7): "Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John" ... "the three Electors voted for Colin Powell for President"'. Or quote the opinion's sentence alone.

**The skeptic's evidence.** The record, ElectionsData/usa/president_returns.md:37: 'The Supreme Court's opinion in *Chiafalo v. Washington* (No. 19-465, decided 6 July 2020; saved) does: "Three Washington electors, Peter Chiafalo, Levi Guerra, and Esther John (the Electors), violated their pledges to support Hillary Clinton", and "the three Electors voted for Colin Powell for President" - the opinion adds that "Only seven electors across the Nation cast faithless votes".'

The saved PDF: raw/returns/supremecourt_chiafalo_v_washington_19-465.pdf. Its sha256 4491bf0e...6ec2 equals SHA256SUMS.txt:17, and it is register row :92. I read `/mingw64/bin/pdftotext -raw`:
- Lines 3-8 (page 1): 'Syllabus / NOTE: ... The syllabus constitutes no part of the opinion of the Court but has been prepared by the Reporter of Decisions for the convenience of the reader.'
- Lines 27-28, under 'Syllabus': 'Three Washington electors, Peter Chiafalo, Levi Guerra, and Esther / John (the Electors), violated their pledges to support Hillary Clinton / in the 2016 presidential election.'
- On the joined text layer, 'Three Washington electors' and 'violated their pledges to support Hillary Clinton' each occur exactly once. That one place is at offset 1468, before 'JUSTICE KAGAN delivered the opinion of the Court.' at 7011, so it is in the syllabus only.

The opinion of the Court (line 295 '6 CHIAFALO v. WASHINGTON / Opinion of the Court'):
- Lines 313-321: 'This case involves three Washington electors who violated their pledges in the 2016 presidential election. ... Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John (the Electors). All three pledged to support Hillary Clinton in the Electoral College.'
- Lines 327-328: 'So the three Electors voted for Colin Powell for President. But their effort failed. Only seven electors'
- Line 336, after footnote 3 and the page-7 running head: 'across the Nation cast faithless votes'.
- So the 2nd and 3rd quotations are the opinion's own words, and the 1st is the syllabus's.

First pass: review786_first.md:849-850 (finding 20's skeptic) kept 'Syllabus:' and 'Opinion of the Court, slip op. 6-7:' apart, and its corrected fix (:874) said to name the three 'on the Supreme Court's opinion'. The staged fix took the syllabus's sentence and credited it to the opinion. This is first-pass finding 20's fix carried out imperfectly, not a new defect class.

Refutation tried and failed:
- (1) 'Opinion' meaning the whole saved slip-opinion document: the register row does label it '(slip opinion)'. But line 37 credits the words to 'The Supreme Court's opinion', and the document's own headnote says the syllabus is no part of the opinion of the Court and is the Reporter's. So the attribution is wrong on the document's own terms.

Why only a note:
- The substance holds. The opinion of the Court names the same three (slip op. 6) and ties them to the Powell votes. So these still stand: reading 3's heading 'three of the electors named by the record', option (a), register :92 'names the three Washington electors who voted for Colin Powell', and USA_STAGE_PLAN.md:188 'three of the electors by the Supreme Court's Chiafalo v. Washington (saved)'.
- No tool, check or catalog line reads the quotation. GeneratedCatalogCheck only re-hashes the PDF.
- It matches first-pass finding 16 (a quotation's dash differing from its saved page), which was graded a note.

**The skeptic's corrected fix.** This is a text-only edit to ElectionsData/usa/president_returns.md:37. No tool rerun is needed. The register row, option (a) and USA_STAGE_PLAN.md:188 stay as they are. Replace the first quotation with the opinion's own words and cite pages, for example: 'The Supreme Court's opinion in *Chiafalo v. Washington* (No. 19-465, decided 6 July 2020; saved) does (slip op. 6): "Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John (the Electors). All three pledged to support Hillary Clinton in the Electoral College", and "the three Electors voted for Colin Powell for President" - the opinion adds (slip op. 6-7) that "Only seven electors across the Nation cast faithless votes".' Or keep the sentence and attribute it correctly: 'its syllabus (the Reporter of Decisions', which the slip opinion says "constitutes no part of the opinion of the Court"): "Three Washington electors, ..."; the opinion itself (slip op. 6): "Among those Democratic electors were petitioners Peter Chiafalo, Levi Guerra, and Esther John" ...'.

### 26. Fix 12's optional rewording labels a paraphrase as 'the saved hearing page's words'

- **Lens:** completeness - **reviewer:** note - **skeptic:** note
- **Where:** ElectionsData/usa/district_method_2024.md:104

**The scenario.** :103-104 now read '(75+ testifiers, the majority opposed to one or both measures - the saved hearing page's words, §786)'. The saved raw/district/nebraska_unicameral_update_37216.html says 'over 75 people testified — the majority of them in opposition to one or both measures'. A byte search finds 'opposed to one or both measures' 0 times and 'in opposition to one or both measures' twice. So text labelled as the page's words is a paraphrase - the slip this change corrected for the two 39341 quotations at :113-115. The s786 LB3 bullet (:145) quotes the page verbatim, so only :104's label is off.

**The fix proposed.** Quote the page: '"the majority of them in opposition to one or both measures"' - the saved hearing page, §786. Or drop 'words', for example '- per the saved hearing page, §786'.

**The skeptic's evidence.** I tried to refute this and could not. The finding is new: it is about wording that first-pass finding 12's fix added.

1. The record's text. In ElectionsData/usa/district_method_2024.md:103-104 the staged file and the working tree match, and `git diff` on that path is empty. The lines read: "Hearing 30 January 2025 (75+ testifiers, the majority opposed to one or both measures - the saved hearing page's words, §786)". The staged diff shows the change replaced "majority opposed" with this text, so the label "the saved hearing page's words" is new in this change.

2. What the saved page says. raw/district/nebraska_unicameral_update_37216.html has: "In a hearing lasting more than five hours, over 75 people testified — the majority of them in opposition to one or both measures." Byte counts in that file:
- "opposed": 0 (the word does not appear on the page at all)
- "opposed to one or both measures": 0
- "in opposition to one or both measures": 2 (once in the HTML body, once in its JSON copy)
- "testifiers": 0
- "75+": 0

The only words in the record's text that are verbatim are "the majority" and "one or both measures". The label also fails on the narrow reading where it covers only "the majority opposed to one or both measures".

3. In these records, "the page's words" means verbatim, as opposed to paraphrase:
- district_method_2024.md:113-115: "both quotations corrected to the saved page's words ... the earlier ones having paraphrased it".
- president_returns.md:66: "two quotations from the Unicameral Update's session review were not the page's words".

So the label at :104 commits the slip that the change corrects at :113-115 and records at :66.

4. The rest of the finding checks out:
- The s786 LB3 bullet at :145 quotes the page byte for byte. A `grep -F -c` of the whole sentence, with its U+2014 em dash, finds it once on the page and once in the record. "heard by the Government, Military and Veterans Affairs Committee Jan. 30" also matches.
- Both 39341 quotations match the page's rendered text. In the raw bytes the committee name in the second one ("was considered by the Government, Military and Veterans Affairs Committee but was not advanced") is wrapped in an `<a>` tag, so a raw `grep -F` finds 0, but the text matches once the tags are stripped.
- Only :104's label is wrong.

5. First-pass finding 12's optional fix only proposed the paraphrase, "75+ testifiers, the majority opposed to one or both measures". The label calling it "the saved hearing page's words" came with the fix, and the first pass never raised it.

Severity stays at note:
- The facts are faithful to the page: over 75 testified, and the majority opposed one or both measures.
- The verbatim sentence is already in the same file at :145.
- No CSV, tool or C# reads this prose.
- No done-criterion of the US-3 ruling depends on it.
- The only harm is that someone checking the label against the page will not find these words there.

**The skeptic's corrected fix.** Edit only ElectionsData/usa/district_method_2024.md:103-104. Use either of these:

(a) Quote the page, keeping its U+2014 em dash so the quote is byte-equal to the page: Hearing 30 January 2025 (the saved hearing page, §786: "over 75 people testified — the majority of them in opposition to one or both measures")

(b) Keep the paraphrase and drop the claim that these are the page's words: (75+ testifiers, the majority opposed to one or both measures - per the saved hearing page, §786; its words are quoted under "Raw pages")

Do not change any raw page, CSV, the tool, or the C#.

### 27. HOUSEKEEPING, not a code finding: my scratchpad cleanup deleted files under scratchpad/rev786b while a parallel us3_mutate.sh run was working there

- **Lens:** completeness - **reviewer:** note - **skeptic:** note
- **Where:** C:/Users/elias/AppData/Local/Temp/claude/C--Users-elias/c8a2f04f-adf9-462b-8470-14c260fdbdb5/scratchpad/rev786b:

**The scenario.** I created scratchpad/rev786b as my first input copy. At about 23:28:30 local time I ran rm -rf on it. A us3_mutate.sh run that is not mine was working in that folder at the time: it started at 23:27:18 local, with cwd rev786b, so its T was rev786b/us3_mut. rm reported a busy file in that run's copy. The suite rebuilds its copy for every mutation, so only the mutation in progress at that moment, around the middle of the suite by timing, can have been disturbed. A vanished input makes the tool die without the expected text, which the suite prints as MISSED. That run had ended by 23:29:21. The repo was not touched: git status shows only the pre-existing unstaged ProjectSettings/ProjectAuditorSettings.asset.

**The fix proposed.** If that run's tally shows any MISSED or BROKEN line, discount it and re-run the suite in its own directory. My own full run of the same suite, in scratchpad/rev786_suite, caught 25 of 25 with the control byte-identical.

**The skeptic's evidence.** I could not refute it. The event happened as the finding describes. It is not a defect in the change: the repo was not touched and the disturbed result has already been checked again.

How the suite works (scratchpad/us3_mutate.sh): `T=$PWD/us3_mut`. Before every run, setup() does `rm -rf "$T"; mkdir -p ...; cp -r "$SRC/ElectionsData/usa" ...`. Each mutation therefore starts from a fresh copy taken from the repo. Line 55 sets `POKE=$PWD/us3_xlsx_poke.pl`, a path that depends on the working directory.

Timeline from the workflow's transcripts (subagents/workflows/wf_f234a635-5b3), times UTC, local = UTC+2:
- 21:09:03, find:completeness (the finding's author) creates rev786b as its input copy.
- 21:15:48, find:newchecks runs `R="$SP/rev786b"; rm -rf "$R"; mkdir -p "$R/base/..."`. The two agents picked the same name, and this already wiped the author's copy. That did no harm: the author last used it at 21:14:57.
- 21:27:17, find:newchecks runs `cd "$R" && bash "$SP/us3_mutate.sh"` with R=rev786b.
- 21:28:30, the author runs `rm -rf "$SP/rev786b" "$SP/rev786_suite" "$SP/rev786p"`. rm reports: "cannot remove '.../rev786b/us3_mut/ElectionsData/usa/raw/district/nebraska_sos_general_canvass_book_2024.pdf': Device or resource busy".
- 23:28:41 local, ps shows the suite (pid 427868, started 23:27:18) and a new `perl Tools/us_returns_prep.pl` running.
- 21:29:16.761, newchecks's tally comes back: "MISSED R7 a READ cell deleted (rc 2, wrote 0) / ElectionsData/usa/raw/district/maine_sos_president_by_cd_2020.xlsx: No such file or directory", then "Can't open perl script \".../rev786b/us3_xlsx_poke.pl\": No such file or directory / MISSED R14 ... (rc 0, wrote 4)", and finally "caught 23, missed or broken 2".
- On disk, rev786b is now empty with mtime 23:29:16.734, which is when the suite's closing `rm -rf "$T"` ran.

Only R7, the mutation running at 23:28:30, was disturbed by the deletion; the mutations from R8 on ran on fresh copies.

Two things the finding leaves out:
1. **The second MISSED has a different cause.** R14 failed because of the working directory, not the deletion. rev786b never held us3_xlsx_poke.pl: no copy into rev786b appears in any transcript, and newchecks always called `$SP/us3_xlsx_poke.pl` directly.
2. **The deletion also took newchecks's own tools.** Its base/, work/, mut.sh and helper scripts in rev786b went too. At 21:29:34 its next call failed with "rev786b/mut.sh: No such file or directory".

Both results are already re-checked:
- newchecks rebuilt in scratchpad/newchecks_lens_x7q4. At 21:30:17 its "R7 again" run gave rc=255, wrote=0, "ME 2012 1 R not READ" (caught). At 21:30:41 its R14 poke control (TBC set to 400000) gave rc=255, "...not more than half of the 400000 ballots cast" (caught).
- The author's own run in rev786_suite (21:19-21:21, poke script copied in) gave "PASSES control ... the repo's bytes" and "caught 25, missed or broken 0".

Repo state: `GIT_OPTIONAL_LOCKS=0 git status --short` shows only ` M ProjectSettings/ProjectAuditorSettings.asset` beyond the staged paths. 95 paths are staged and HEAD is 5fe3c031.

**The skeptic's corrected fix.** Nothing needs to change in the repo and no re-run is owed.

Treat both MISSED lines from find:newchecks's 23:27:18 run as invalid, for different reasons:
- **R7** was disturbed by the deletion.
- **R14** failed because us3_mutate.sh sets `POKE=$PWD/us3_xlsx_poke.pl`, and rev786b never held that script.

The finding's fix ("re-run the suite in its own directory") would hit the R14 failure again unless us3_xlsx_poke.pl is copied into that directory, or the suite is run from the scratchpad root. rev786_suite and r2rec_lens both copied it in.

Both results are already re-checked. newchecks re-ran R7 (caught, "not READ") and the R14 poke (caught) in scratchpad/newchecks_lens_x7q4. The author's rev786_suite run caught 25 of 25 and its control reproduced the repo's bytes.

To make the record complete, also note:
- The deletion also removed newchecks's own working files in rev786b; it rebuilt them within 20 seconds.
- The clash went both ways: newchecks's own `rm -rf rev786b` at 23:15:48 had already wiped the author's first input copy, harmlessly.

To prevent this:
- Give parallel agents unique scratch directory names, e.g. a random suffix like newchecks_lens_x7q4.
- Have us3_mutate.sh find POKE next to the script, via `$(dirname "$0")`, instead of from `$PWD`.

### 28. The backtest asserts every USA year, while its class doc and closing line say deviations are findings, not failures

- **Lens:** csharp - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/SeatAllocationBacktest.cs:214

**The scenario.** Lines 13-14 say 'A deviation is a FINDING, not a failure: the run exits 0 whenever the harness itself ran'. Line 249 prints 'synthetic {ALL PASS|N FAILED}; the country tables above are FINDINGS (deviations reported, not asserted)'. Yet line 214 adds each catalog year's Expect to `failures`, and line 250 exits 1 on it. Example: an allocator edit that moves 2016 by one elector prints 'FAIL USA 2016 EC by the real rule'. The run then closes with 'synthetic 1 FAILED; ... (deviations reported, not asserted)' and exits 1, so a USA deviation is reported as a failed synthetic vector. HEAD's single 2024 Expect already did this; the loop now does it for every year the catalog holds. The backtest is outside the bar (ElectionsBacktests), so only someone reading its output is misled.

**The fix proposed.** Say what the code does. In the class doc, state that the USA college is asserted: the tool has already held each split to NARA's table, so a deviation here can only come from the allocator. Change line 249 to count asserted failures ('synthetic vectors and the USA college') instead of 'synthetic'. Alternatively, print the USA rows without adding to `failures`, since GeneratedCatalogCheck already asserts the same split in the cheap bar.

**The skeptic's evidence.** The finding's facts hold in Assets/Editor/SeatAllocationBacktest.cs:
- :13-14 (class doc) says: "A deviation is a FINDING, not a failure: the run exits 0 whenever the harness itself ran; the tables are the result."
- :209 is the per-year USA line, newly written in this diff. On a mismatch it prints `"DEVIATES, a finding"`. The finding does not mention this line, and it is the sharpest form of the problem: the change's own new text calls the deviation a finding, and five lines later it counts it as a failure.
- :214 `failures += Expect($"USA {record.Year} EC by the real rule", usReal, new[] { pledgedR, pledgedD });`
- :249 `Debug.Log($"=== SeatAllocationBacktest: synthetic {(failures == 0 ? "ALL PASS" : failures + " FAILED")}; the country tables above are FINDINGS (deviations reported, not asserted) ===");`
- :250 `CheckExit.Finish(failures == 0 ? 0 : 1);`

Trace for a 2016 deviation caused by an allocator edit:
1. :209 prints "DEVIATES, a finding".
2. Expect (:257) prints "  FAIL USA 2016 EC by the real rule: got [..], expected [..]" and returns 1.
3. :249 prints "synthetic 1 FAILED; ... (deviations reported, not asserted)".
4. The run exits 1. The class doc said it would exit 0.

Pre-existing: at 5fe3c031, line 216 already read `failures += Expect("USA EC by the real rule", usReal, new[] { 312, 226 });`. bd34c8c8 (2026-08-29) added it under the doc lines from f524eeb2 (2026-08-28). This diff extends the assertion to every year in UsPresidentialReturns.Years and rewrites :209 but keeps "a finding". The first-pass report does not cover this. Its only backtest items are #10 (comment wording) and the refuted zero-iteration guard.

Why it stays a note:
(a) The behaviour is right. The exit code is the conjunction of what was asserted, which CheckExit's ruling 1 requires (CheckSuite.cs:24-25). The FAIL line names "USA 2016", so nothing is hidden; only the summary label and the class doc are wrong.
(b) The backtest is in no bar. ElectionsBacktests.cs:11-14 says it is "Deliberately NOT in `CheckSuite`", and CheckSuite.cs has no reference to SeatAllocationBacktest. Its exit code reaches only ElectionsBacktests.RunBatch (:35-37) and EntrantRuleMeasure (:30-35), and both exit with the worst code of the set. COMPLETED.md:25942 says "in the simulation bar below", but that is a historical §445 row.
(c) GeneratedCatalogCheck.cs:576-583, in the cheap bar (CheckSuite.cs:275), runs the same comparison with an accurate message: `Allocate(FromCatalog(y.Year), 2)` against `CastR+OthersRSlate` and `CastD+OthersDSlate`. Any USA deviation therefore fails the cheap bar first.

**The skeptic's corrected fix.** Fix the text only and keep the assertion. Dropping `failures +=` at :214 would change the exit code of ElectionsBacktests.RunBatch and EntrantRuleMeasure, since both exit with the worst code of the set. Make the three statements say what the code does:
(1) Class doc :13-15: "A deviation in a COUNTRY TABLE is a FINDING, not a failure; the tables are the result. ASSERTED, and the run exits 1 on either: the synthetic vectors, and the USA college. The college is a statute's rule over the generated catalog, which `Tools/us_returns_prep.pl` already held to NARA's table state by state, so a deviation can only come from ElectoralCollege's reading of the catalog or from its allocator. `GeneratedCatalogCheck` asserts the same split."
(2) :209: change "DEVIATES, a finding" to "DEVIATES (asserted below)".
(3) :249: change "synthetic {..}" to "synthetic vectors and the USA college {ALL PASS|N FAILED}; the country tables above are FINDINGS (deviations reported, not asserted)".
Do not add figures or year lists, per the claim convention. PartyInkHarness.cs:132 and COMPLETED.md:5649 cite "SeatAllocationBacktest's idiom" for the country tables, and both stay true under this fix.
The finding's alternative (print the USA rows without adding to `failures`) is a behaviour change. It is defensible, because GeneratedCatalogCheck already gates the split, but it should be a deliberate choice, not part of a text fix.

### 29. Expect's expected array is positional {R, D}, while the rest of the loop indexes by the named constants

- **Lens:** csharp - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/SeatAllocationBacktest.cs:214

**The scenario.** Line 209 reads usReal[ElectoralCollege.Republican] and usReal[ElectoralCollege.Democrat]. Line 214 passes `new[] { pledgedR, pledgedD }` to Expect, which compares got[i] with want[i] by position. Lines 212-213 also index [0]. It is correct today because Republican = 0 and Democrat = 1. If the published constants were reordered (Republican = 1, Democrat = 0), FromCatalog and GeneratedCatalogCheck would stay right and line 209 would print EXACT. Line 214 would then report FAIL for every year, e.g. 2012 'got [332,206], expected [206,332]'. This is latent; no output is wrong now.

**The fix proposed.** Build the expected array by index: `var want = new int[2]; want[ElectoralCollege.Republican] = pledgedR; want[ElectoralCollege.Democrat] = pledgedD;` and pass `want`. Use ElectoralCollege.Republican in place of [0] on lines 212-213 for consistency. Those comparisons are symmetric with two candidates, so they are harmless as written.

**The skeptic's evidence.** Every claim in the finding checks against the staged code (the working tree equals the index). The first-pass report (review786_first.md) has no finding on this, confirmed or refuted, so it is new.

The code:
- ElectoralCollege.cs:47-48 adds `public const int Republican = 0; public const int Democrat = 1;`. Both are new in this change. FromCatalog (:70, :74) assigns winners only through these constants.
- SeatAllocationBacktest.cs:209-210 index by the names: `usReal[ElectoralCollege.Republican]`, `usReal[ElectoralCollege.Democrat]`, `usWtaOnly[ElectoralCollege.Republican]`.
- :212-213 index by position: `usWtaOnly[0] == usReal[0]` and `Math.Abs(usReal[0] - usWtaOnly[0])`.
- :214 is `failures += Expect($"USA {record.Year} EC by the real rule", usReal, new[] { pledgedR, pledgedD });`.
- Expect (:253-258) compares by position: `for (int i = 0; i < want.Length; i++) { ok &= got[i] == want[i]; }`.
- GeneratedCatalogCheck.cs:581 also uses the names: `college[...ElectoralCollege.Republican]`, `college[...ElectoralCollege.Democrat]`.
- No document or save fixes the index order anywhere else. A repo grep finds only the constants.

Measured on a scratch copy, outside the repo:
- Setup: the staged ElectoralCollege.cs (cmp-identical) and Generated/UsPresidentialReturns.cs, compiled with dotnet 10 in scratchpad/ec_order_demo/{asis,swapped}. A Program.cs mirrors lines 194-214 and GeneratedCatalogCheck:578-582 with the same comparisons.
- As staged: all four years print `ok`, for example 2012 `got [206,332], expected [206,332]`. Failures 0, exit 0.
- With the constants swapped (R=1, D=0): every year's line 209 still says EXACT and the GeneratedCatalogCheck:581 comparison stays ok. Expect FAILs all four years: 2012 `got [332,206], expected [206,332]`, 2016 `[232,306]` vs `[306,232]`, 2020 `[306,232]` vs `[232,306]`, 2024 `[226,312]` vs `[312,226]`. Failures 4. The 2012 message is exactly the finding's example.
- Lines 212-213: in both runs index 0 and index 1 give the same truth value and the same move count (2016 moves 1 elector; the other years 0). Both arrays always sum to 538: Allocate throws otherwise, and AllocateAsIfWinnerTakeAll sums the same TotalEv. So with two candidates these lines are symmetric and harmless, as the finding says.

Why only a note:
- No output is wrong now.
- The only trigger is a deliberate swap of two public constants. Nothing in this change or in USA_STAGE_PLAN does that. Adding a third index such as Other=2 would not break :214, because Expect loops over want.Length=2.
- Under the swap the failure is loud: a false FAIL with a nonzero exit, never a false pass.
- It would show only in the ElectionsBacktests launch, which is deliberately kept out of CheckSuite (ElectionsBacktests.cs:10-13). The cheap bar's GeneratedCatalogCheck would stay right.
- What makes it worth noting: this same change brought in the named constants, then left the one assertion in the loop tied to their values. That assertion is the only place in the new code that silently assumes R=0, D=1.

**The skeptic's corrected fix.** The finding's fix is correct. A slightly better one keeps the printed arrays in R, D order (matching line 209's print order) whatever the constants' values: `failures += Expect($"USA {record.Year} EC by the real rule", new[] { usReal[ElectoralCollege.Republican], usReal[ElectoralCollege.Democrat] }, new[] { pledgedR, pledgedD });`. Optionally, for consistency only, write `usWtaOnly[ElectoralCollege.Republican] == usReal[ElectoralCollege.Republican]` on :212 and the same index on :213. Both are symmetric with two candidates, so they cannot be wrong as written.

## The second pass - refuted by the skeptics

- [newchecks] The READ TSV's header still describes only the shortfall bound and calls the layer the tool now trusts 'unreliable for digits'; no declared-exception path - *REFUTED. The header is incomplete but nothing in it is false, and the "no declared-exception path" point does not hold up.

1. The TSV header makes no false statement.
- Lines 7-8 say: "Tools/us_returns_prep.pl holds each party's shortfall to that count and to the FEC's statewide figures." The tool still does this at us_returns_prep.pl:635-643 (`$g < 0` names a district above the state; `$gap > $unalloc{$year}` names a shortfall beyond the count).
- The header never says this is the only check, so a reader is not told the check is just the loose bound.
- That count is now read from the certificate's own footnote (:640-641), which makes the sentence more accurate than it was at the first pass. The first pass already called the sentence "accurate" (review786_first.md:125).
- CLAUDE.md:37 allows this: "no document becomes wrong — only *incomplete*".
- The new check is described where readers of the check look: the tool header at :39-41 ("a Maine 2012 or 2016 figure READ that is not the certificate's own text layer - figure, district, page"), president_returns.md:12 ("held by the tool to those certificates' own text layer") and :24 and :35. A reader who edits the TSV gets :626's message, which names the text layer.

2. "Unreliable for digits" is not contradicted. The tool does not trust the text layer; it compares two readings. The comment at :558 says "the two readings must agree figure for figure", and any disagreement stops the run, so the check fails safe. The phrase is also true of the saved pages. `pdftotext -raw` gives:
- 2012: "Linda Bean, Port Clyde 142,93 7" and "Second Co11gressio11al District1", where the letter n is read as the digits 11;
- 2016: "212, 774", "357, 735" and "7, 563".

3. A declared-exception table would not remove the need to change code.
- The precedent is itself code: `my %typo = ('2020 R' => 'Trunp');` at :369.
- An empty table of the same kind would still need a new entry in the tool to record a slip, so the scenario's "can be recorded only by changing code" is equally true with the proposed fix.
- First-pass findings 3 (review786_first.md:141) and 24 (:1007) already set the remedy: declare an exception "as the 2020 'Trunp' misprint already has" when a real disagreement appears. Fix (1) follows that, so it is not incomplete.

4. The scenario is very unlikely, and if it happens the tool fails loudly.
- I read the text layer. Every D and R district figure equals the TSV row: 142937, 149215, 223035, 177998, 212774, 144817, 154384 and 181177.
- Both footnotes match: 415 (twice) and 184 (three times).
- So the tool passes today. For R-K9 to find an OCR slip, the three 2012 readers (two for 2016) and the OCR would all have to have produced the same wrong digit.
- Even then the outcome is the stop the ruling asks for ("fails on any mismatch"), not a wrong figure written out.*
- [csharp] RawSources is checked only against the disk: a deleted or re-pointed row passes, and the bar then stops hashing that page - *The mechanism is as the finding describes. GeneratedCatalogCheck.cs:536-541 hashes only the rows `UsPresidentialReturns.RawSources` lists, the only guard on the list itself is `RawSources.Length == 0` (:543), and nothing else in the check opens a raw page. But no failing path exists unless two files the project forbids anyone to edit are both edited.

1. Only the tool writes the list, and it cannot leave out a page it read. `page()` (us_returns_prep.pl:75-86) holds each page to its SHA256SUMS.txt line (:84 dies on a mismatch) and then records it in `%read`. I checked every raw read in the tool, at :260, :296, :374, :439, :505-506, :525 and :596-597: each goes through `page()`, including the two pdftotext reads. RawSources is written as `sort keys %read` (:787). UsPresidentialReturns.cs:1 says "GENERATED ... DO NOT EDIT BY HAND", and president_by_year.csv:1 says "DO NOT EDIT".

2. The harm also needs an edit to a byte-exact raw page, and that page is still pinned elsewhere. Any re-run of the tool dies on it at `page()` :84, and re-running the tool is exactly what the check's own failure message says to do (GeneratedCatalogCheck.cs:590). `sha256sum -c` also fails on it, and that is the ruling's own standard. The record states this scope as it stands (president_returns.md:10: the check "re-hashes the pages `RawSources` lists ... The register's other pages are held by their groups' SHA256SUMS.txt (sha256sum -c), as every raw page is"), so the doc overclaims nothing. The 518-519 wording has the same premise as the class doc at :16, which names drift as the one failure mode ("the source moved and the catalog did not"); a hand-edited catalog is not drift.

3. Every catalog check in this file already rests on this premise, so the change introduces no new gap. `CheckProjections` deliberately enumerates its own catalog's table (`foreach ... in PopulationProjections.SourcePath`, :430; doc at :357, "the PartyMarkCoverageCheck idiom"). Deleting one of its entries silently stops that source being hashed, which is the finding's exact scenario. In `CheckItanes`, `CheckEnergy`, `CheckCohortIncome` and `CheckGovernmentConsumption`, one edit to a recorded digest constant defeats the check. RawSources follows that standard. The cross-check against the CSVs is protection that only the data rows get, on top of the standard.

4. A shortened list does not pass unseen. The bar's summary prints `RawSources.Length` (:595-597).

5. The proposed fix cannot catch anything the generator could do. The CSV header lines would be written from the same `%read`, so they would also miss the only path that does not involve a hand edit: a future read that skips `page()`. All the fix does is turn a deliberate one-file hand edit into a three-edit change across two DO-NOT-EDIT files. It would also rewrite president_by_year.csv and YearSourceDigest.*

## What was done about the second pass

No defect survived the skeptics (2 minor, 27 notes; several pairs report one thing - 3 and 22, 11 and 25, 12 and 23, 13 and 24, 19 and 26); every finding was acted on except 27 (housekeeping: a reviewer's scratch cleanup raced a parallel suite run; the author's own run was unaffected, nothing in the repository).

- **The TBC test (1, 2, 8).** An empty TBC cell now stops the run; each Total row's TBC is held to the sum of its candidate, Others and Blank columns (found by their headings); the districts' TBC less their Blank is held to the FEC's typed state total. That TBC is §723-A(2)'s denominator is labelled a READING in the tool, the record, `ElectoralCollege.cs` and `district_method_2024.md` - no saved page says whether "Blank" holds ballots overvoted at the first rank; who carried the state and each district is held independently by NARA.
- **"Naming every mismatch" (9).** A `__DIE__` hook prints every queued mismatch before any later die ends the run (not inside a module's eval); the final gate empties the queue as it prints; the notes comparison no longer prints a disagreeing state twice. The tool's header and the record say what holds: a page off its digest, or an input it cannot read, stops the run there, and the mismatches found before it are named with it.
- **Page breaks (6).** A certificate's text layer without page breaks is now named as the extractor's problem and the page comparisons skipped, never blamed on the TSV; the run prints which pdftotext it used.
- **Nebraska's First and Third (3, 22).** Reading 9 and the tool's comment say that NARA names those two together, both the Republican's, so which stands under which label rests on the book's headings even in the split years.
- **The mutation suite (4, 5, 21, 27).** Committed as `Tools/us_returns_mutations.sh` with `Tools/xlsx_poke.pl` (§576: tooling a pass depends on lives under Tools/): location-independent, run in a temporary directory, each case requiring the exact mismatch it means, so a mutation that lands elsewhere is MISSED, never CAUGHT. Case 1 and 2 now pin the shortfall bound they are named for; case 9 moves a Maine elector on NARA's 2024 table (the first run's regex had changed Alaska's row); four new cases (a TBC emptied, a TBC understated, a mismatch queued before a die, an extractor without page breaks); the tool-side cases labelled TOOL. Twenty-nine of twenty-nine caught, the control reproducing the repository's bytes; the record names the suite and splits its cases.
- **Reading 3 (7, 11, 25).** The quotation is the opinion's own (slip op. 6), not the syllabus's; *In re Guerra* is identified, by the opinion's own words, as the three named Electors' appeal; each option carries its cost; the Texas certificate's "record of the Electoral College" named as an untried lead; "no official record found" for the four in "Not reached".
- **The records (10, 12-20, 23, 24, 26).** `returns_2024.md` says no tool reads it, and which of its figures a CSV carries (10); LR24CA's 2026 ballot status cited to the two saved pages that carry it (12, 23); the LB3 sentences that rest on no saved page named - the vote's party breakdown, "at the biennium's sine die", the 110th Legislature and the Governor's intent (13); reading 8 cites §723-A(6) ("This section applies to elections held on or after January 1, 2018") and the 2012 page for 2012 and 2016 (14); reading 7 says what each 2012 formula sums (15); the FEC's 2020 volume named as kept out of tree in `returns_2024.md` (16); the plan's Built line says the 2024 derivation from NARA stands and only its premise failed (17); "three CSVs" gone from the plan, the record, the tool and `.gitattributes` (18); the hearing quoted as the page prints it (19, 26); every failed attempt the record names copied byte for byte into its group's in-tree `fetch_log.txt` (20); `district_method_2024.md`'s exception clause covers every gap its bullets name (24).
- **The backtest (28, 29).** Its class doc and closing line say the USA college is asserted, as the code does; `Expect` compares by the named indices.
