# Review — §713 every ballot a vote on persons; Art. 63(4) as written (2026-10-01)

Reviewed: `git diff --cached` against HEAD 4338124 (the change is staged, not in the working tree: `Assets/Scripts/Simulation/SimulationManager.cs` and `Assets/Editor/GermanFormationDiagnostic.cs`). I read:
- the staged `SimulationManager.cs`:
  - `Investiture` 3190-3210;
  - `PersonBallot` 3237-3310;
  - `Commitments`, `ProjectPersonBallot` and `FourteenDaysCandidates` 3318-3356;
  - `PluralityBallot` 3390-3490, with its new doc;
  - `ScheduleExtraElection` and `AdvanceConfidenceDay` 2640-2675;
  - the constructive vote's callers 2780-2830;
  - `AnswerOffer` 3755-3775;
- `ConfidenceProcedure.cs` 20-140 (`ConstructiveVote`);
- `CoalitionFormation.cs` 900-965 (`DerivedRedLines`);
- `GameController.FormationSheet.cs` 315-352 and `GameController.cs` 11518-11566 (the draft, the verdict and the cached projection);
- `ElectionsData/germany/records_by_date.md` 39, 49-53, 230-240 ([GG-63], [GG-39]) and `coalition_declarations_2025.md` 60-64, 85 ([BT-KW25]);
- GO-BT § 2 and § 4, from the saved print;
- `ConstructiveVoteDiagnostic.cs`, which drives no round;
- the logs `n713b.log` and `n713c.log` (all clean).

One independent reading, read-only; Unity not run (a bar is running). **Verdict READY WITH ONE STRING, with two rulings owed to Elias** before the record calls item 4 closed.

## Defects

1. **The phase-1 slip describes a ballot that phase 1 does not hold.** `ProjectPersonBallot` now projects phase 1 (the Bundespräsident's one candidate), and the sheet draws it. The slip still reads *"THE BALLOT ON THE NOMINATIONS STANDING: {0} FOR YOURS, {1} FOR THE OTHERS"* (`GameController.FormationSheet.cs:347`).
   - **Why it is wrong:** in phase 1 there are no nominations; GO-BT § 4 Abs. 2 governs the later ballots only. "FOR THE OTHERS" is always 0, so the bar reads "329 ⁄ 0".
   - **Fix:** a phase-1 line, e.g. "THE BUNDESPRÄSIDENT'S CANDIDATE: {0} FOR, {1} NEEDED". And `ProjectPersonBallot`'s doc ("False outside a Bundestag round in the fourteen days") should name phase 1 too.

## Owed to Elias (rulings, not code defects)

- **A. The reading of "acceptable". Judged defensible, but it must go to Elias as owed; it is not the author's to settle.** The author's reading takes the ruling's two clauses as a hard bar plus an undefined standard:
  - "never one they've declared against" is the hard bar;
  - "abstain if none is acceptable" leaves "acceptable" undefined, and the author lets every support-blocking line, declared or derived, define it.

  That is coherent: the declared bar is kept, and the model never votes across its own derived lines. It goes to Elias for four reasons:
  - **(a) It reverses the plan recorded at §712:** "Acceptability still uses all support-blocking lines (item 4 restricts it to declared ones)".
  - **(b) The ruling's only named criterion is a declaration.**
  - **(c) It puts an untested threshold in charge of German ballots.** A derived line blocks support only past a CHES galtan gap of 5.0 (`DerivedRedLines.SocialGap`, CoalitionFormation.cs:934). That threshold is [AUTHORED-DRAFT], calibrated on Sweden 2022, and under this reading it decides who abstains in every German chancellor ballot.
  - **(d) It decides outcomes.** In (d) the Greens abstain on Merz by a derived line, giving 329; declared-only gives 414. In (e) the author's reading elects Habeck by the most votes (269) on 9 April. Declared-only has the AfD vote for Habeck (as the author states), so he is elected in the fourteen days with 421.

  Put both readings, with these two results, in front of Elias.
- **B. Whether "every ballot" reaches Art. 67.** The constructive vote is the Bundestag's other chancellor ballot ("mit der Mehrheit seiner Mitglieder einen Nachfolger wählt", [GG-67]), and it still counts *"every other party … as the investiture has it"* (`ConfidenceProcedure.cs:75`, 125-135).
  - **The concrete clash:** on (e5)'s chamber, Art. 63 elects Merz with the SPD's person vote (328). In an Art. 67 vote for the same Merz, the SPD sits outside the successor's cabinet and votes by the investiture, so he would carry 208.
  - **The two sides:** the same nominee, on the same day and the same chamber, is elected under one article and not the other. The AI's daily constructive-vote search (§698) weighs successors by the investiture, while governments now arise by person votes.
  - **What Elias should rule:** extend the person tally to Art. 67 (one candidate, the successor, with the successor's tabled partners bound), or confine "every ballot" to Art. 63. The record should say which.

**Latent:**
- **A one-candidate ballot elects without a coalition.** In phase 1 every party with no support-blocking line votes for the Bundespräsident's candidate, so a nominee is elected without a coalition and may govern as a minority (e5: CDU+CSU on the SPD's 120). That is the ruling's literal effect and the record should state it. On an in-game chamber, a party whose galtan gap to the AfD is under 5.0 would vote for an AfD nominee proposed after passes.
- **The dissolution premise overstates.** Its reason, "a dissolution's new election … is not held in the game", is stronger than the code. The game holds extra elections (`ScheduleExtraElection`, Sweden's RF 6:5 and 6:7, three months); a German sixty-day one (Art. 39 Abs. 1 Satz 4) is simply not wired. "Not modelled for Germany" would be exact.
- **The final ballot's tie-break is the game's own.** A tie goes "to the earlier in the order". Art. 63(4) and GO-BT § 4 are silent on ties; the lot "durch die Hand des Alterspräsidenten" is § 2 Abs. 2, the President's election. §705's doc states the tie-break; "as written" should not be read as covering it.
- **Party-line votes in a secret ballot.** GO-BT § 4 Abs. 1 prescribes a secret ballot ("verdeckten Stimmzetteln"). The record's 2025 first ballot failed, 310 of 316, on defections ([BT-KW25]). The game elects Merz in phase 1 with 329. Party-line votes are the model's premise, and it stands.
- **No film of the new phase-1 bar.** The sheet now draws the person projection there, which the German film scope (drys705/drys706) should re-film.
- **The elected-but-no-government branch now reaches phase 1.** There it would open the fourteen days over a candidate elected by a majority. It is still unreachable, and the record will note it.

## Confirmed sound, stated

- **The Riksdag is byte-identical.** `Investiture` is now the Riksdag's only: its title string, `RecordInvestiture`, the rejection count, the log and `BreakOff` are unchanged once the Bundestag branch is gone. `ElectedGovernment` and `PersonBallot` are Bundestag-only. The sweep digest 86fbf292… is unchanged (n713b and n713c).
- **Phase 1 is Art. 63 Abs. 1-2 as written.** The Bundespräsident's one candidate is elected by "die Stimmen der Mehrheit der Mitglieder" (`MajorityOf`).
  - **Not elected:** the fourteen days open. The code moved verbatim from `Investiture` (Phase 2, `SecondPhaseUntil`, the Abs. 3 log line), and `Rejections++` is kept.
  - **Stood marks:** phase 1 marks no one as having stood, so its candidate can be nominated in the fourteen days, as before.
  - **Commitments:** they bind the tabled government's partners in phase 1. (d) gives 329 = CDU 164 + CSU 44 + SPD 120 + SSW 1, and the basis says "on the Bundespräsident's proposal … (Art. 63 Abs. 2 GG)".
- **The final ballot follows Art. 63 Abs. 4 as written.** The doc quotes Satz 2-3 exactly as [GG-63] on disk (records_by_date.md:236). The two branches are the article's: with a majority he "muß … binnen sieben Tagen … ernennen"; without one the choice is his, and the premise for it ([AUTHORED-DRAFT]: he appoints, at once, inside the seven days) is stated with its reasons on the title, the log and the basis. The reasons hold:
  - no Bundespräsident has faced the choice;
  - Art. 39 Abs. 1 Satz 4 is the sixty-day rule, quoted under [GG-39];
  - [BT-KW25] has Merz elected and appointed on 6 May 2025.
- **Wiring is consistent.**
  - `AnswerOffer`'s well-formed gate now applies in every Bundestag phase, consistent with the AI path, which always tables.
  - The sheet's projection cache is keyed on the draft version, which is new at every ask, so a phase change re-projects.
  - The (e) title assertion follows the new wording, and the new check (e5) proves a minority elected by a person vote in phase 1.
  - No random stream is drawn, and no saved field is added.

**Money path:** yes by name (`SimulationManager`), so the ledger wants this review's row. No arithmetic is touched; the reach is who governs, as `Install` already has it.

**Verdict: READY WITH ONE STRING** (defect 1, the phase-1 slip and the doc line). A and B are owed to Elias, with A's measured outcomes, before COMPLETED §713 calls item 4 closed. Neither blocks the commit if the record states them as owed.

## The author's note (2026-10-01)

- **The one string, fixed:** in phase 1 the sheet's slip reads *THE BUNDESPRÄSIDENT'S CANDIDATE: n FOR, A MAJORITY OF THE MEMBERS ELECTS*; `ProjectPersonBallot`'s doc names phase 1.
- **Latent, taken:** the dissolution premise reads "not modelled for Germany" (the game holds Sweden's extra elections).
- **Owed rulings A (the reading of "acceptable") and B (Art. 67's constructive vote still counts by the investiture)** are stated in COMPLETED §713 as Elias's; the other latent items (a one-candidate phase 1 elects without a coalition; the final ballot's tie-break the game's own; party-line votes in a secret ballot - Merz's real first ballot failed 310/316, the game elects him with 329) are stated there too.
- The simulation bar `sim713` ran before these string edits (no simulation check reads them); the cheap bar and the dry film run after.
