# Review — §706 the Treasury lock lifted and GO-BT § 4 nominations (2026-10-01)

Reviewed: `git diff --cached` (all 16 files), plus the staged `Assets/Scripts/Simulation/SimulationManager.cs` 1285-1390, 2395-2470, 2990-3120, 3116-3275, 3340-3480, 3480-3640, 6450-6460; `GovernmentRecord.cs` 1-335 (whole); `FormationProposal.cs` 1-250; `PortfolioSalience.cs` (whole); `GameController.FormationSheet.cs` 85-175, 229-275; `GameController.ParliamentRows.cs` 196-300; `GameController.cs` 768-778, 1410-1414, 6344-6362; `GameController.Desk.cs` 300-315; `CabinetSystem.cs` 170-215; `PortfolioEffectiveness.cs` 78-86; `CabinetPortfolio.cs` 20-40; `WorldClock.cs` 250-256; `PartySystem.cs` 448-462 (Italy); `MetaTextCheck.cs` 55-260; `CheckSuite.cs` 200-225; the diagnostics `PortfolioSalienceDiagnostic.cs` (whole), `PortfolioAllocationDiagnostic.cs` 1-80, `SpeakerRoundDiagnostic.cs` 100-125, `GermanFormationDiagnostic.cs` 40-56, 140-230, 285; `ElectionsData/portfolios/portfolio_salience.md` (whole); `docs/reference/GAMSON_PORTFOLIOS.md` 1-60; `docs/specs/POLITICAL_SYSTEM_SPEC.md` 80-130; `POLISIM_FEATURE_LIST.md:61`; `Tools/bar_tier.ps1` 1-40; the tail of `Tools/review_ledger.tsv`; `ElectionsData/germany/raw/records/SHA256SUMS.txt`; `Reviews/2026-09-25_s634_portfolios.md`; the text of both saved PDFs (DW05's Appendix 2 for Germany, Sweden, France (V) and Italy; GO-BT § 4 Abs. 1-4) extracted with `pdftext.pl`; the author's log `PoliSim-captures/logs/ps706b.log` (9 of 9 clean). One independent reading, read-only; Unity not run. **Verdict NOT READY.**

## Defects

1. **In phase 2 a player whose party is the only standing nomination can never leave the formation sheet.** `AskNext` (SimulationManager.cs:3089-3097) cycles the order until it reaches a party in `standing`. When the player's party is the only standing nomination, it wraps back to the player, and `Stage = PlayerAsked` on the same date. That happens after a PASS (`PassFormation` 3584-3591 → `AskNext`) and after a failed tabling (`TableAndVote` votes the same day in a convened Bundestag, then `Investiture` 3212 → `AskNext`). `FormationHolds` (GameController.cs:1410-1414, 773) holds the clock unconditionally on `PlayerAsked`. So the fourteen days never elapse, the Art. 63 Abs. 4 ballot is never reached, and the only exit is a proposal that wins a majority. None exists on the planted chamber of check (e), where the Union alone reaches a quarter.
   - **Proof:** ps706b.log:1682-1684 (check e2, the CDU player): *"2025-03-25: the Bundestag weighs Friedrich Merz (CDU) … - the player's party"* / *"2025-03-25: CDU does not form a government"* / the same ask again, on the same day.
   - **Why the check passes:** the diagnostic moves the date itself (`GermanFormationDiagnostic.cs:202, 285`: `AdvanceDay` and one pass a day), so it never sees the hold.
   - **Why it is new:** under §705 a pass moved to the order's next party, and that party's seven-day consultation moved the clock.
   - **The row's text:** the PASS slip (`ParliamentRows.cs:199`, used at 330 and FormationSheet.cs:363) still promises "THE NEXT PARTY IN THE ORDER STANDS ITS CANDIDATE".
   - **Fix:** in phase 2, a pass (and a failed tabling) withdraws the player's nomination for the rest of the fourteen days, falling to the next standing party or to the "NO NOMINATION STANDS" state. The phase-2 slip should then say so. Add a check that the date moves after a phase-2 pass under the hold's rule.
2. **A party too small to nominate alone can never gather another nominating party's signatures.** `Nominated` (SimulationManager.cs:3446-3452) drops every nomination below a quarter in one pass (`kept = standing.FindAll(...)`). A falling nomination's members therefore move only to nominations that already hold a quarter by themselves. Two small nominations can never pool: SPD 120 and Grüne 85 fall together, and neither ever sees the other's members. The only signatures a small nomination can collect are those of parties outside the Bundespräsident's order (the Linke, the SSW).
   - **Against the stated rule:** the doc comment's own premise (3424-3426: *"iterated, since a nomination that falls moves the votes it held"*) and the ruling (*"including what a party too small to nominate alone must do"*) both call for the votes of a falling nomination to reach the others.
   - **Proof (e2), ps706b.log:1713-1715:** in the ballot the most votes win, the pool is AfD 152, SPD 120 and Grüne 149 (85 plus the Linke's 64). All three fall in one pass, the Abs. 3 fallback admits them all, and *"Alice Weidel (AfD) is elected with 152 … AfD takes office"*.
   - **The alternative:** SPD, Grüne and Linke hold 269 members, over the 158 needed. If the SPD's nearest non-refused candidate is the Greens (the tally's own premise), dropping the weakest nomination and re-tallying stands a Green nomination under Abs. 2. It would then be the only candidate, and Abs. 3 never arises.
   - **The check pins the defect:** check (e2) "GO-BT § 4 Abs. 3" (GermanFormationDiagnostic.cs:209-210) asserts this outcome.
   - **Fix:** eliminate one nomination at a time, the one with the fewest signatures first (a tie to the later in the order), re-tallying after each, until every nomination left holds a quarter. Add a check where a sub-quarter party stands on another nominating party's signatures.
3. **The cheap bar goes red on the new Parliament-row slip.** `GameController.ParliamentRows.cs:256` adds the literal `"OR A FRAKTION OF A QUARTER (GO-BT § 4)"`.
   - **The rule:** MetaTextCheck bans "§" in every string literal under `Assets/Scripts/UI` (`MetaTextCheck.cs:65`; the diagnostic-line exemption applies only to the widened patterns, 229-230). No allow row covers ParliamentRows.cs.
   - **The only hit:** this is the only "§" literal in the UI root. The other occurrence, GameController.cs:9600, sits in a comment, which the check strips.
   - **Why the author's run missed it:** ps706b ran nine named checks, and MetaTextCheck (CheckSuite.cs:222, the cheap bar) was not among them.
   - **Fix:** name the rule without the sign ("THE BUNDESTAG'S RULES OF PROCEDURE"), or add an allow row of the TaxSchedule "EStG" kind with its reason.
4. **The sheet and the partner's reason count posts; acceptance now weighs them.** `Formateur.Answer` scales the payoff by `offeredWeight / expectedWeight` (FormationProposal.cs:180-186). But the reason it prints (190-192) and the sheet's `OFFERED ⁄ DUE`, its ✕ and its slip (FormationSheet.cs:150-157) still compare counts.
   - **Case (a), 2025 chamber, CDU formateur:** the SPD is due INTERIOR+TREASURY (3.87). Offered HEALTH+FOREIGN (3.42), the sheet shows "2 ⁄ 2" with no ✕, yet the SPD's payoff is cut to 0.884 of its value. If it refuses, the reason reads *"offered 2 post(s), Gamson's law allocates it 2 here"*.
   - **Case (b), the same chamber:** the CSU is due DEFENCE+EDUCATION (1.94). Offered INTERIOR alone (2.29), the row shows ✕ "1 ⁄ 2 · LESS LOWERS ITS ACCEPTANCE", while the factor is 1.0.
   - **Effect:** the player reads a verdict the model does not apply, on the one surface where the ruling's "counts for more than one post" is played.
   - **Fix:** show and state weights, or both measures, on the row and in the reason.
5. **The salience mapping does not apply its own rule to Sweden.** The game's HealthSocialAffairs sums the labour ministry in France (Employment 1.13), Germany (Labour & Social Affairs 1.21) and Italy (Labour & Social Security 1.06) (PortfolioSalience.cs:40, 62, 73).
   - **Sweden's omission:** DW05's Swedish appendix rates *Labour/Employment 1.26*, but Sweden takes Health & Social Affairs 1.22 alone (PortfolioSalience.cs:51). The record's table (portfolio_salience.md, "Labour / social" row) lists Sweden's 1.22 there and writes "(in the above)" for Health. The rated 1.26 appears nowhere.
   - **Either reading leaves one column wrong.** If the game's post includes labour, Sweden's weight is 2.48. The 2022 allocation then becomes M HEALTH/TREASURY/DEFENCE, KD FOREIGN/INTERIOR, L EDUCATION (by hand; Finance stays M's). The Poland/USA mean becomes 2.215, not 1.90. If the post excludes labour (§634 gives labour levers to no portfolio), then France's, Germany's and Italy's labour terms are the wrong ones.
   - **Fix:** one rule, applied to all four columns, and the omitted score shown.
6. **Stale statements of the rule.** Each of these now describes a rule the code no longer follows:
   - `GovernmentRecord.cs:53-57` (the `Portfolios` field): *"apportioned by largest remainder; the prime minister's party … takes Finance first … the rest handed out in the enum's order"*.
   - `PortfolioAllocationDiagnostic.cs:15-16`: *"largest remainder to M 4, KD 1, L 1 with Finance to M"*.
   - `POLISIM_FEATURE_LIST.md:61`: *"(the Treasury stays the prime minister's party's)"*.
   - `POLITICAL_SYSTEM_SPEC.md` §5.3 (the Gamson line and premise 2) still describes unweighted proportion.
   - The new doc comment (GovernmentRecord.cs:138) and the record's step 4 justify the head's guaranteed post with *"a head with no post holds no lever"*. That is false: the prime minister's party passes the one-argument gate for every lever whatever it holds (`PlayerGoverns` → `PlayerMayIntroduce`, SimulationManager.cs:2426-2434, 2448).
7. **The sources are cited but not recorded.**
   - **The GO-BT print:** the key `[GOBT-4]` (SimulationManager.cs:3418) resolves to no source row in any record. `gobt_2026-07-11.pdf` has no line in the folder's `SHA256SUMS.txt`, and no URL, fetch date or byte count anywhere. Measured here: 766,917 bytes, SHA-256 `10efe43c…c2db8`.
   - **The calibration row:** portfolio_salience.md:55 and PortfolioSalience.cs:11 put Poland's and the USA's mean "on the calibration list as `S-P1`". §346 (the play-calibration list) has no such row, and `[S-P1]` is already Socialdemokraterna's source key (DeclaredRedLines.cs:169, 175).
   - **The method:** the allocation method is [AUTHORED-DRAFT], and by POLITICAL_SYSTEM_SPEC.md:186 it belongs on that list too.
   - **The COMPLETED entry:** `COMPLETED.md` is not in the staged set, though every new comment cites §706 as the record.

**Latent:**
- **The merged-ministry rule departs from its source.** DW05 (p. 24) calls the scores "net" ratings: a combined holding is "the sum of the two scores", and a partial portfolio gets "a correspondingly small importance score". By the paper's reading, Italy's MEF weighs 1.64 + 1.32 + 0.98 = 3.94, not the highest (1.64).
  - **Italy:** on the game's Meloni cabinet (FdI 119, Lega 66, FI 45, NM 7) that moves Finance from the Lega (Giorgetti, the record) to FdI. The author's rule fits the record here, but it is not the paper's rule and should be stated as DERIVED against it.
  - **Germany:** the author's own rule is not applied to Education. Science & Education (0.82) and Research & Technology (0.93) merged into the BMBF in 1994, so by "a merged ministry its highest" the weight is 0.93. By hand this leaves no outcome changed in 2021 or 2025.
  - **France:** the Budget (1.25), a separate ministry in current French cabinets, is left out of FinanceTreasury.
- **A head with no post, the edge case.** The move of "the lightest post of the richest" (GovernmentRecord.cs:170-186) takes only from a party holding more than one post, which is right: it never takes a party's only post and never leaves one with nothing. But if the head's party ends with none and every other party holds at most one (a cabinet of seven or more), the head's party keeps none, contrary to the doc comment. No check exercises the move at all.
- **Tests that prove less than they say.**
  - `PortfolioSalienceDiagnostic`'s doc claims "the lock itself: a formateur's sheet may give Finance to a partner"; it has no such check.
  - `SpeakerRoundDiagnostic` (3) now exercises only `PreviewFormation`, not `SubmitFormation`, the second site the lock was removed from.
  - Check (e) replaced the "sincere votes" assertion with "every side carries its reason" but still prints the old variable, so an "ok" line reads *"(NOT STATED)"* (ps706b.log:1647).
  - The phase-2 "NO NOMINATION STANDS" state (`Asked == null`) is never reached by any check, and the new row is not on film.
  - (e)'s planted chamber makes every other party refuse every candidate, so co-signing is exercised nowhere.
- **Who may nominate.** The pool is the Bundespräsident's order (3433-3439), so the Linke never nominates, even where Abs. 3 opens nominations to every member. That is a premise beyond § 4's text; the doc comment states it, but it is not in the ruling's words.
- **Finance's levers are nominal for an AI partner under a player chancellor.** The player's party passes every gate, so a player CDU chancellor on the 2025 chamber still introduces the tax programme the record gives Klingbeil. That is §634's rule, as the ruling cites it; worth one line to Elias.
- **The budget stays the chancellor's.** Conversely, a player junior partner holding Finance is refused the budget bill (the one-argument gate, 1292). The lock text then reads "THE PRIME MINISTER'S LEVER · YOURS: INTERIOR, TREASURY". That is §634's stated exclusion, but it reads against "whoever holds Finance holds its levers".
- **Saves and films.** Portfolios ride the save and nothing re-allocates on load. A pre-§706 save keeps Finance with the head's party, and Sweden's 4/1/1, until `LeaveCabinet` or a new government re-allocates. The play protocol's saves and any film staging a Swedish junior partner show the old split until re-cut. No format bump is needed; the shape is unchanged.
- **Fragile margin.** 2021's FOREIGN goes to the SPD over the Grüne by 0.001 of entitlement (1.2104 against 1.2094). The ruling's Finance tests are not fragile: 0.32 and 1.30 clear.

## Confirmed sound, stated

- **Every DW05 figure matches the appendix.** Germany: Chancellor 2.12, Finance 1.58, Interior 1.27, Justice 1.02, Labour 1.21, Health 0.80, Defence 1.12, Foreign 1.41, Science & Education 0.82. Sweden: 2.19, 1.68, Justice 0.99, H&SA 1.22, Defence 0.99, Foreign 1.27, Education 1.07. France (V): 2.75, 1.92, 1.63, 1.48, 1.13, 0.99, 1.38, 1.45, 1.40. Italy: 2.48, Treasury 1.64, Finance 1.32, Budget 0.98, 1.78, 1.23, 1.06, 1.19, 1.19, 1.69, 1.10. The Poland/USA means are exact (2.385, 1.705, 2.35, 1.90, 1.17, 1.455, 1.0975). The DW05 PDF is 137,729 bytes with the recorded SHA-256.
- **`AllocatePortfolios` is correct.**
  - Every post is allocated exactly once: an empty cabinet returns at 144, so `taker` is never null.
  - A seatless member never takes a post by the greedy: the outstanding entitlements sum to the posts still to allocate, which is always positive.
  - A zero-seat cabinet splits equally, and a one-party cabinet holds all six.
  - The post sort is a total order (weight, then the enum). A tie in entitlement stays with the earlier party in the seat order. No random stream is drawn.
  - It matches its doc comment, apart from the edge noted under Latent.
  - By hand: 2025 gives CDU HEALTH/FOREIGN, SPD INTERIOR/TREASURY, CSU DEFENCE/EDUCATION; 2021 gives SPD INTERIOR/FOREIGN, Grüne HEALTH/DEFENCE, FDP TREASURY/EDUCATION; Sweden 2022 gives M TREASURY/FOREIGN/INTERIOR, KD HEALTH/DEFENCE, L EDUCATION. All three equal the log.
- **An AI draft's acceptance is unchanged.** `DraftProposal` and `Formateur.Answer` call `GamsonPosts` on the same cabinet and formateur, so for an AI draft offered equals expected and the factor stays 1.0. The FormationSweep digest is unchanged.
- **The lock is gone everywhere.** No reader of `HoldsTreasury` remains in code, and the sheet's lock glyph and slips are removed.
- **GO-BT § 4 is read correctly apart from defect 2.**
  - The quoted Abs. 2 and 3 match the print word for word.
  - The quarter is a ceiling, 158 of 630, and 5 % is 32.
  - A Fraktion of a quarter is read through `GroupSeats`, with the Union as one group.
  - Abs. 3's fallback applies only to the Abs. 4 ballot (`pluralityBallot`), and "es sei denn" is read as "only nominations a Fraktion or 5 % signs".
  - Phase 1 stays the Bundespräsident's proposal: the filter applies from phase 2 only.
- **The `Asked == null` state is safe.**
  - It is dereferenced nowhere: the row's guarded case comes first, `Consulting` returns early, and the Abs. 4 check at 3159 runs before the switch.
  - `CandidateOf` and `BreakOff` are never reached with null, and `FormationHolds` reads only the stage.
  - It serializes as null.
- **Sweden is unchanged apart from the rulings' own changes.** The phase-2 block, `Nominated`, `Tally` and `PluralityBallot` are Bundestag-only. The new `Consulting` early return cannot be reached in a Riksdag round, because an empty order breaks off. `Tally` is a faithful extraction, with an added `ci < 0` guard. The Riksdag's behaviour changes only in the allocation, the lock's removal and the weighting of the player's offers, all three the rulings'.

**Money path:** yes by the project's definition: `SimulationManager` is in `$money`, and the ledger will want this review's row. In substance, no arithmetic is touched. The new reach is a player junior partner holding Finance, now possible (the SPD in Germany's start government). Through §634's gate that partner gets:
- the tax-programme bill (1385);
- fiscal, monetary and electricity-tax laws (2467);
- Finance's cabinet decisions, which are one-off budget impacts;
- the Finance minister's appointment, whose competence the revenue path reads (6457).

The budget bill stays the prime minister's (1292), and the AI ministry keeps writing rates and spending by bill; §634's review found the fields do not collide. So the book's author is still right as §634 defines it. What differs from the ruling's plain words is stated under Latent.

**Verdict: NOT READY.** Defects 1-3 block: the phase-2 soft-lock, the co-signing rule that elects the AfD in (e2) against the stated premise, and the cheap bar's MetaTextCheck. Defects 4-7 are owed before the record calls §706 closed.

## Second pass (2026-10-01, the staged state after the first pass's defects)

Reviewed: `git diff --cached` (all 23 files; the D-DE work in `stash@{0}`, the tree §706 only), with the changes since the first pass read line by line:
- `SpeakerRound.cs` (`StoodInPhase2`);
- the staged `Assets/Scripts/Simulation/SimulationManager.cs` 3086-3115 (`AskNext`), 3160-3190, 3200-3222 (`Investiture`), 3270-3290, 3355-3480 (`Tally`, `Nominated`), 3595-3632 (`PassFormation`, `AnswerOffer`);
- `FormationProposal.cs` 170-195, `GameController.FormationSheet.cs` 10-25, 110-165, and `GameController.ParliamentRows.cs` 196-260;
- `PortfolioSalience.cs` (whole) and `portfolio_salience.md` (whole);
- `coalition_declarations_2025.md` ([GOBT-4]) and `SHA256SUMS.txt`, with `sha256sum -c` run here: 61 OK. Every file in the folder has its line except `fetch_log.txt`.
- the spec §5.3 and premise 2, and `POLISIM_FEATURE_LIST.md:61`;
- the diffs of `GermanFormationDiagnostic`, `PortfolioAllocationDiagnostic` and `SpeakerRoundDiagnostic`, and `Tools/film_scope.tsv` (drys706);
- `MetaTextCheck.cs` 55-95, `PlayProtocolStaging.cs` 140-160, `CLAUDE.md:90`, `ElectionsData/sweden/records_by_date.md` 35-40, `sweden/2026/government_2026.md`, and `docs/play/PLAY_SHEET.md`;
- the logs `n706b.log` (5 checks, German failing) and `n706c.log` (German alone, clean).

**Verdict READY WITH ONE STRING**, on the bars the coordinator is launching.

### The first pass's defects

1. **CLOSED.** A nomination stands once in the fourteen days (`StoodInPhase2`, removed in `AskNext` 3097).
   - **The three paths are covered:** a failed ballot records its formateur (3209), a pass records the player's party (3605), and a decline the asked party cannot survive records that party (3630). Every re-entry of `AskNext` in phase 2 is therefore counted.
   - **Proof:** n706c (e2): the CDU player passes in phase 2 on 2025-03-25, and the same day the round goes to the no-nomination state. The date then runs to the Abs. 4 ballot on 04-09; it is no longer re-asked.
   - **Wording:** the PASS slip now says what a pass does in each phase (ParliamentRows.cs:199).
   - **Saves:** the new field rides the save. A v37 round without it loads with an empty list, which at worst allows one extra ask of a party that already stood. That is harmless, so no format bump is owed. Nothing pins save bytes.
2. **CLOSED.** `Nominated` drops one nomination at a time, the fewest signatures first and a tie to the later in the order (3457-3465). It re-tallies after each drop and terminates within `pool.Count + 1` passes.
   - **Proof:** n706c (e). The SPD's and the Linke's members pool behind Habeck (269 ≥ 158); the fourteen days ballot Merz and Habeck once each; the plurality elects Habeck with 269 against 208. (e2) now plants the left apart to reach Abs. 3, and (e3) plants the Linke apart.
3. **CLOSED.** The slip reads "(THE BUNDESTAG'S RULES OF PROCEDURE)" (ParliamentRows.cs:257).
   - **Re-scan:** no "§" literal remains under `Assets/Scripts/UI`. The other new literals (the sheet's weight lines, the two PASS lines) match none of the banned or widened patterns.
   - **Still owed:** MetaTextCheck has not yet run on this state; the bar will.
4. **CLOSED in substance, one string left (new defect 8 below).**
   - **The row:** it now shows offered ⁄ due as weights, with ✕ where the offer weighs more than 0.005 short. The figures use the invariant point format, which is the rule by CLAUDE.md:90.
   - **The reason:** the partner's reason appends *"(the posts weighed: X offered against Y due)"*. The player now reads what the model applies.
5. **CLOSED by the "include labour" reading.** Sweden's HealthSocialAffairs is 1.22 + 1.26 = 2.48, the table shows the 1.26, and the Poland/USA mean is 2.215 (8.86 ⁄ 4, checked here).
   - **Sweden 2022:** n706b:1626-1629 gives M HEALTH/TREASURY/DEFENCE, KD FOREIGN/INTERIOR, L EDUCATION, the split I computed by hand in the first pass.
   - **See new note B:** this reading moves Finance on Sweden's other chamber.
6. **CLOSED.** The rule is restated in each of these:
   - `GovernmentRecord.cs` (the `Portfolios` field doc, and the "no lever" rationale corrected to "its levers pass the gates anyway, the post is its minister's");
   - `PortfolioAllocationDiagnostic.cs`'s doc;
   - `POLISIM_FEATURE_LIST.md:61`;
   - spec §5.3 and premise 2.

   The PortfolioAllocationDiagnostic doc still names the 2026 chamber (M 70, KD 22, L 19) while the check runs on the 2022 one (68 ⁄ 19 ⁄ 16). That is pre-existing, and both chambers give 3 ⁄ 2 ⁄ 1.
7. **CLOSED, with the COMPLETED entry still to be appended.**
   - **[GOBT-4]:** it has its source row (URL, access date, "Stand: 11. Juli 2026", 766,917 bytes, SHA-256 matching the file), and the manifest lists it and every §704/§705 record.
   - **The calibration entries:** the Poland/USA key is `SAL-POL`, no longer colliding with Sweden's [S-P1]; it is the play-calibration list's 23rd entry, and the method is the 24th. Both go into COMPLETED §706 when it is appended.
   - **They will not reach the play sheet:** as with the 21st and 22nd, `PLAY_SHEET.md` is generated from §346 and carries only its twenty, so these two entries will not appear on the page where Elias writes. That is the convention, stated.

### New

8. **DEFECT (one string, no re-review needed).** The sheet's column head still reads `"SEATS · IN · POSTS OFFERED ⁄ DUE"` (FormationSheet.cs:113) over figures that are now weights ("3.42 ⁄ 3.87"). The class doc (FormationSheet.cs:16-17) still says the ✕ marks any departure, *"over-offered is as much a departure as short"*, but the ✕ now marks a short offer only (156-158). **Fix:** "WEIGHT OFFERED ⁄ DUE", or similar, and one doc sentence.

**Notes:**
- **A. The phase-2 ballot and the signing premise (the coordinator's question).** Judged: **this item's to state, Elias's to rule; not blocking.**
  - **The problem:** before §706 no model claimed that anyone but the formateur's coalition backed a candidate in the fourteen days. §706's [AUTHORED-DRAFT] "the members who would vote for a candidate sign it" makes that claim. The Art. 63 Abs. 3 ballot then counts the formateur's investiture instead (n706c: Habeck *"85 for … Grune forms Grune"*), so the division record shows the SPD and the Linke signing his nomination and then not voting for him a week later.
  - **The fix is a ruling, not a patch:** is an Abs. 3 ballot a vote on the nominated person by the sincere tally (as §705's defect 3 made the Abs. 4 ballot), or a formateur's investiture? A single-candidate sincere tally would change phase 2 widely, electing candidates no coalition backs.
  - **Effect:** it changes no chancellor in (e), (e2) or (e3). In general it can: a pooled nomination with a majority of the members loses its phase-2 ballot, and a later formateur's coalition wins one. It should be named in COMPLETED §706 and in what Elias is owed.
- **B. The Labour fix moves Sweden's Finance on the 2026 chamber.** On the Riksdag elected 2026-09-13 (records_by_date.md:40: M 70, KD 22, L 19), M+KD+L now allocates by hand as follows. W = 10.67. M takes HEALTH (2.48), leaving 2.059 against KD's 2.115, so TREASURY goes to **KD**; then FOREIGN and INTERIOR go to M, EDUCATION and DEFENCE to L.
  - **Before the fix,** Finance was M's there.
  - **The margin** is 0.056 of entitlement.
  - **The checks pin only the 2022 chamber,** where M keeps Finance.
  - **Scope:** the real 2026 government is not yet on record (a caretaker since 17 September, government_2026.md), so this is the model's answer, not a contradiction. But a Swedish M player after the 2026 election will now be told that KD is due Finance.
  - **Ask:** pin this chamber's result in `PortfolioSalienceDiagnostic`, and put it in front of Elias with the ruling's Germany tests.
- **C. The run does not cover the staged state whole.** `GermanFormationDiagnostic.cs` was edited after n706c ended (01:18:03 against 01:17:53). The (e2) message's `Find` now looks for "Abs. 3", where n706c printed an Abs. 2 line under that check. The other four named checks last ran in n706b, which already carried the new runtime (Habeck elected with 269, "has stood"). The bars owe the rest: MetaTextCheck, PlaySheetCheck, ReviewLedgerCheck with this review's row, and the drys706 film (the sheet's wider figure pair and the new row's width).
- **D. Wording.** After a pass, the no-nomination log line reads *"every nomination signed by a quarter of the members has stood"* (n706c (e2): the CDU passed, it never stood). The row's slip then explains the quarter rule, although the player's Union held 208. "has stood or passed" would be exact.
- **E. Premises the record should state.**
  - "A nomination stands once in the fourteen days" is the game's, not Art. 63's, which allows repeated ballots. A nomination that has already stood keeps its signers in the pooling tally, so no other small nomination can pool them afterwards.
  - The player's party's signatures are the model's (the tally, unless it declined), as its Abs. 4 vote already was under §705.
- **Carried from the first pass, unchanged:**
  - Italy's merged-ministry rule is still the author's "highest", not DW05's sum; the record should say it departs from the paper.
  - The head-with-no-post edge (seven or more parties) is still unexercised.
  - `PortfolioSalienceDiagnostic`'s doc still claims "the lock itself" check it does not have.
  - `SpeakerRoundDiagnostic` (3) still runs Preview only.
  - The pool is still the Bundespräsident's order.

**Money path:** unchanged from the first pass. No arithmetic is touched; the added code is round bookkeeping, a saved list and display. The Finance-holding junior partner's reach is §634's. Note B moves which Swedish party can be that partner, not what the partner can do.

**Verdict (second pass): READY WITH ONE STRING.** Defects 1-7 are closed. Fix defect 8's head and doc sentence before the commit; they need no re-review. The bars must pass on the staged state (note C). Notes A and B go to Elias in COMPLETED §706.

## The author's note after the second pass (2026-10-01)

- **Defect 8, fixed:** the sheet's column head reads "SEATS · IN · WEIGHT OFFERED ⁄ DUE"; the class doc says the ✕ marks an offer that weighs less than the Gamson share, the one departure that lowers a partner's acceptance (`postsFactor` is capped at 1.0, so an over-offer raises none).
- **Note B, pinned:** `PortfolioSalienceDiagnostic` asserts the 2026 Riksdag (M 70 · KD 22 · L 19) gives Finance to KD by the method, printed as PINNED and Elias's to look at; COMPLETED §706 names it in what is owed.
- **Note D, fixed:** the no-nomination line reads "has stood or passed".
- **Notes A and E and the carried items:** stated in COMPLETED §706 (premises, owed items).
- **Note C:** the bars run on the staged state after these edits: the cheap bar (MetaTextCheck, PlaySheetCheck, ReviewLedgerCheck with this row), the simulation bar, and drys706 again.
