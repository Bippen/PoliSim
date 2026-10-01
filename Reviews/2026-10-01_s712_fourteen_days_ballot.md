# Review — §712 the fourteen days' ballot as a vote on persons (2026-10-01)

Reviewed: `git diff` against HEAD 648010c (both files: `Assets/Scripts/Simulation/SimulationManager.cs` and `Assets/Editor/GermanFormationDiagnostic.cs`). In the working `SimulationManager.cs` I read:
- `AskNext` 3085-3120;
- `Table` / `TableAndVote` / `AdvanceSpeakerRound` 3125-3190;
- `Investiture` 3191-3230;
- `FourteenDaysBallot` 3254-3315 and `ElectedGovernment` 3316-3360;
- `PluralityBallot` 3377-3440, compared line by line with HEAD's 3269-3355;
- `Tally` 3445-3500 and `Nominated`;
- `SubmitFormation` 3650-3665 and `AnswerOffer` 3695-3716.

Also read: `GameController.FormationSheet.cs` 315-345; `GameController.cs` 3584-3590, 10196-10212 and `GameController.Statistics.cs` 240-256 (the readers of divisions); `SigningScreen.cs` 325-405; `SupportAgreement.cs` 145-150; and the author's log `PoliSim-captures/logs/n712a.log` (3 of 3 clean).

One independent reading, read-only. Unity was not run (a bar is running). **Verdict NOT READY.** Defects 2 and 4 are small. Defects 1 and 3 change who is elected in phase 2.

## Defects

1. **A partner that accepted the formateur's cabinet is not counted for the formateur.**
   - **How the ballot counts:** `FourteenDaysBallot` (3266-3279) counts votes with `Tally` alone. A party whose own nomination is on the ballot votes for it (`choice = candidates.Contains(key) ? key : null`, 3465). Every other party votes for the candidate nearest it by compatibility.
   - **What it ignores:** the tabled proposal itself, whose partners and supporters have just accepted the formateur's government. That acceptance is either `verdict.AllAccept` (the player's tabling, `SubmitFormation` 3650-3665) or the AI draft's holding government.
   - **Before §712:** the investiture counted those parties for the formateur.
   - **Failing scenario (structural):** a phase-2 formateur tables a coalition whose majority rests on a partner. If that partner's own nomination also stands, it votes for its own candidate. If the partner's nearest candidate on the ballot is another one, it votes for that one. Either way the coalition the formation itself formed, and every partner accepted, fails its ballot. Example: the SPD tables SPD+Grüne+FDP while the Union's nomination stands, and the FDP is nearer the CDU than the SPD.
   - **Why the checks miss it:** check (e)'s Greens table "Grune forms Grune", a cabinet with no partner, so no check puts a partner through the ballot.
   - **The rule it breaks:** a party should vote by its own answer (§698's review, defect 4). The ruling says "members choose among the nominated candidates"; a party that has accepted a seat under one of them has chosen.
   - **Fix:** in this ballot, the tabled proposal's accepting cabinet parties and supporters vote for the formateur, and a partner withdraws its own nomination. Or, if Elias means the votes to be purely by compatibility even inside an agreed coalition, rule it and state it.
   - **Before item 4:** settle it either way. Once item 4 makes phase 1 a vote on persons, every coalition election will depend on it.
2. **A declined offer in phase 2 still decides by the investiture, so a nomination can be consumed without a ballot.** `AnswerOffer` (3709-3714) drafts the formateur's government without the player. It tables that government only `if (verdict != null && verdict.Passes)`, where `Passes` means the investiture wins. Otherwise it marks the formateur as having stood (3713) and asks the next party.
   - **Why that is wrong now:** under §712 a nomination that stands needs no coalition majority to be put to the vote. The AI's own path (`AdvanceSpeakerRound`, Consulting → `TableAndVote`) always tables.
   - **Failing scenario:** the player's party is offered a place in an AI formateur's phase-2 cabinet and declines, and the formateur's government without it falls short of a majority by investiture. That candidate is never balloted in the fourteen days, while every AI nomination asked without an offer is.
   - **Fix:** in phase 2, table the government without the player whenever it is well formed and every invited party accepts (`verdict.Investiture != null && verdict.AllAccept`). The ballot decides the rest.
3. **The player's party's nomination is kept off the AI's ballot, then balloted alone.**
   - **The two steps:** at 3273 the player's standing nomination is removed from a ballot an AI formateur called, and it is not marked as having stood. Every AI nomination on that ballot is marked (3297). `AskNext` (3097) then asks the player's party, the only nomination left. If it tables, `FourteenDaysBallot` holds a one-candidate ballot.
   - **Why a one-candidate ballot inflates:** in `Tally` every party that does not refuse the only candidate votes for it. Those include the parties that split their votes among several candidates a week earlier.
   - **Failing scenario (schematic):** phase 2, order X (AI), P (player). X is asked first and the ballot is [X, Y]; nobody reaches a majority. P is asked and the ballot is [P] alone, so P collects every non-refusing party's votes. Had P been on the first ballot, it would have drawn only its share. The same nomination faces different opposition depending on whether the player or the AI holds it, and only an order that asks the AI first produces the solo ballot.
   - **Fix:** ask every standing nomination before the single ballot, with the player choosing to stand or pass. Or put the player's un-asked nomination to the ballot as "not standing" and mark it stood. Either way, no nomination should get a ballot of its own after the others have been consumed.
4. **The formation sheet shows a phase-2 player the wrong vote.** `GameController.FormationSheet.cs:317-341` still draws "THE CHANCELLOR'S ELECTION" from `verdict.Investiture`: the tick, "IT WOULD PASS ⁄ IT WOULD FAIL", and "AS IT WOULD STAND: N CARRYING IT, M AGAINST". In phase 2 the tabling now goes to `FourteenDaysBallot`, which uses a different tally: every standing nomination on one ballot, with partners voting by compatibility (defect 1).
   - **Effect:** the sheet can say "IT WOULD PASS" for a ballot that elects nobody, or another candidate. It is the one place the player sees the vote before acting.
   - **Fix:** in phase 2, project the ballot's own tally, shared with `FourteenDaysBallot` the way `PreviewFormation` shares the investiture. At the least, drop the pass/fail verdict there.

**Latent:**
- **A formateur can stand without a quarter's signatures.** "The formateur's tabling is its nomination" (3274) inserts the formateur without checking the quarter again. A decline recorded since the ask (`round.Declines`, read by `Tally`) can take the player's signatures off it. GO-BT § 4 Abs. 2 then gives it no standing nomination, yet it is balloted, and the log quotes Nominated's text for it.
- **An elected candidate can be set aside.** "Elected, and no government can be drawn; the round goes on" (3308) is reachable only when the fallback proposal is malformed, which is effectively never. If it is reached, a candidate elected by a majority of the members is set aside, which Art. 63 does not allow. Assert instead, or install the party and group alone.
- **The failed ballot's division records every vote as -1** (3286), so a for/against reading of it would show 0 for. Motion divisions feed no aggregate (the list reads title and verdict; the signing screen skips motions), and each side's reason names its candidate, so this is cosmetic today.
- **The log still uses investiture language.** Before a ballot on persons it reads "Grune forms Grune; the Bundestag elects the chancellor on …" and "… forms the government it would lead" (the Consulting and Table lines).
- **The fourteen days now hold at most one AI ballot.** It puts every standing nomination at once and marks all of them as having stood. That is §706's "stands once" premise taken to its end, and the record should say so.
- **Untested paths.** No check reaches the elected branch (acknowledged), a coalition partner in this ballot (defect 1), the decline path (defect 2), or the player's solo ballot (defect 3).
- **Acknowledged and deferred, not reviewed as defects:** phase 1 is still an investiture, acceptability still uses every support-blocking line, and the pool is still the Bundespräsident's order.

## Confirmed sound, stated

- **`PluralityBallot` with `tabled = null` is behaviour-identical.** With `tabled` null, `ElectedGovernment`'s first branch is skipped and the `winner != player` draft branch is HEAD's, temporary decline and conditions included. The fallback (party and group, the seated-by-group log line, `GamsonPosts`, `FreezeTabled`, `Answer`) is HEAD's, and its log string is the same `CandidateOf(winner)` text that `candidate` held. The null exit (`verdict?.Investiture == null` → the same log, Concluded, return) maps one to one. `round.Proposal`, the tally line, `Install` with the same bases, and the Breaks line for `seatedByGroup` are unchanged. The sweep digest 86fbf292… is unchanged (n712a).
- **The ballot follows the ruling's words.** Every standing nomination is put at once, its signers vote for their nominee, and the other members choose among the candidates (`Tally`). Election needs "mehr als die Hälfte seiner Mitglieder" (`MajorityOf`: half the seats plus one). A tie for the lead cannot matter, since two candidates cannot both have a majority. Check (e) proves the multi-candidate ballot: Merz 208, Habeck 269, none elected, the SPD voting for Habeck (n712a). The days then run out to the Abs. 4 ballot, which elects Habeck with 269.
- **`ElectedGovernment`'s new branch.** A winner's own tabled proposal is used where every invited party accepts and it splits no group. Otherwise the winner gets its party and group alone, not a re-draft, which for the player is §705's rule and for an AI formateur re-uses the draft it tabled. The player's party is never drafted in unasked, because the decline is added temporarily and `Involves` is checked.
- **Continuation and state are unchanged.** `Rejections++` and the next step (`AskNext`, or `PluralityBallot` once the fourteen days are out) match the old investiture path. No random stream is drawn. No saved field is added (`Phase` and `StoodInPhase2` exist). No code parses the changed division titles; only the diagnostic's `Contains("Art. 63 Abs. 3")` does.
- **Sweden is untouched.** `Investiture`'s new early return is Bundestag-only, phase 2 and up. `ElectedGovernment` is called only from the two Bundestag ballots, and `SpeakerRoundDiagnostic` is clean.

**Money path:** yes by name (`SimulationManager` is in `$money`), so the ledger wants this review's row. In substance no arithmetic is touched. The ballot reaches the book only by deciding who governs, the same reach `Install` already has.

**Verdict: NOT READY.** Defects 2 and 4 are small and should be fixed in this item. Defects 1 and 3 decide who is elected in phase 2. Fix them here, or carry them into item 4 as named defects with the record saying so, before item 4 puts phase 1 on the same ballot.

## Second pass (2026-10-01, the working tree against HEAD 648010c)

Reviewed: `git diff` (three files). From the working `Assets/Scripts/Simulation/SimulationManager.cs` I read:
- `AskNext` 3085-3123;
- `FourteenDaysBallot` 3266-3315, `Commitments` 3318-3331 and `ProjectPersonBallot` 3333-3351;
- `Tally` 3482-3530;
- `AnswerOffer` 3740-3765.

Also read: `GameController.FormationSheet.cs` 315-352 (the diff); `GameController.cs` 11496-11549 (`FormationVerdict`, the sheet's cache); `CoalitionFormation.cs` 280-300 (`Form`, which has no cache); the new check (e4) in `GermanFormationDiagnostic.cs`; and the log `n712b.log` (3 of 3 clean, e4 at 1490-1502).

**Verdict NOT READY**, on two new defects: one against the ruling's own words, and one a cost on the sheet. Both fixes are small.

### The first pass's defects

1. **CLOSED.** `Commitments` (3320) binds every cabinet partner and supporter of a tabled proposal whose invited parties all accept. The player's party is bound only where it accepted: the player answers by `AnswerOffer`, and `without` excludes it. In `Tally`, the binding comes after the group-follow and before the own-candidate rule (3513).
   - **Proof:** n712b (e4): CDU+SPD+CSU with the SSW's support gives Merz 329; the SPD's side reads "accepted CDU's government"; he is elected in the fourteen days on Art. 63 Abs. 3.
   - **Supporters are bound too:** the SSW, which holds no surveyed position, now votes for Merz.
2. **CLOSED.** In a Bundestag round in phase 2 or later, the government without the player is tabled when it is well formed and every invited party accepts (3757-3758). The Riksdag path keeps `Passes`.
3. **CLOSED.** Where the player's nomination stands, the player is asked first in phase 2 (3102). It tables or passes before any ballot, so it is never left off one and balloted alone.
   - **Proof:** e4 asks the CDU on 2025-03-25, the day phase 2 opens. The order alone would have asked the Greens, whose pooled nomination stands.
   - **What it creates:** see note B.
4. **CLOSED in what it shows, with a new cost (defect 6).** In phase 2 the sheet's bar uses `ProjectPersonBallot`. That reads the same candidates, `Tally` and `Commitments` that `FourteenDaysBallot` reads, on the same draft object and the same day, so the projection equals the ballot. The new strings match no MetaTextCheck pattern.

### New

5. **DEFECT: a party bound to the formateur still signs a rival's nomination.**
   - **Where:** `FourteenDaysBallot` (3275) and `ProjectPersonBallot` (3343) build the candidates with `Nominated`, whose signing tally runs without the commitments. Only the ballot's own `Tally` (3278, 3346) receives them.
   - **Proof:** n712b (e4), log line 1500. The ballot reads *"Friedrich Merz (CDU) 329, Robert Habeck (Grune) 149"*. Habeck's phase-2 nomination needs a quarter, 158. It stood only because pooling counted the SPD's 120 as signing for him (85 + 64 + 120 = 269). On the ballot, the SPD is bound to Merz and votes for him.
   - **Against the ruling:** the SPD signed Habeck's nomination and voted for Merz, contrary to "signers vote for their nominee". Counted by the commitments, Habeck has 149 signatures, so GO-BT § 4 Abs. 2 gives him no standing nomination.
   - **Effect:** in e4 the result is the same, since Merz has a majority either way. In general a nomination that should not stand stays on the ballot. It can draw the votes of uncommitted parties that would otherwise go to the formateur or abstain, and so decide whether the formateur reaches a majority.
   - **Fix:** pass the same commitments into `Nominated`'s tally in both callers, so a bound party signs its formateur's nomination.
6. **DEFECT: the sheet re-runs a whole formation on every IMGUI event.** `GameController.FormationSheet.cs:327` calls `ProjectPersonBallot` uncached on every draw of the phase-2 sheet: Layout, Repaint and every input event.
   - **What each call costs:** two chamber preparations (its own `ChamberOf`, and `Answer`'s); `Nominated` with up to `pool.Count + 1` tallies; and `Commitments` → `Formateur.Answer` → `CoalitionFormation.Form` with `Holding`. `Form` has no cache.
   - **The sheet's own pattern:** the verdict beside it is cached precisely to avoid this (`FormationVerdict`, keyed on `_formationDraftVersion`, GameController.cs:11541-11549).
   - **Fix:** compute the projection with the verdict, under the same version key, and invalidate it on the day as well. No film has measured it yet.

**Notes:**
- **A. Only the formateur's coalition is bound.** The rival nominations on the same ballot are never consulted, so their would-be partners vote by compatibility. This is §712's model and should be stated for Elias with the record's "the fourteen days hold one ballot".
- **B. The player gains from being asked first.** Combined with note A, a player whose nomination stands is always the formateur of the fourteen days' only ballot: its coalition is bound, and its AI rivals' coalitions are not. Before item 4 rules how members choose, Elias should see this.
- **C. One rule written twice.** `ProjectPersonBallot` repeats `FourteenDaysBallot`'s candidate rule line for line (3343-3345 against 3275-3277). A shared helper would keep the projection and the ballot from drifting.
- **D. An edge of the player-first rule.** If a decline recorded after the ask moves the pooling, the player's nomination can come to stand only after an AI ballot. It would then be asked alone, which is defect 3's case at an edge. Low.
- **E. The log still uses investiture language before a person ballot** (*"CDU forms CDU+SPD+CSU with SSW; the Bundestag elects the chancellor on …"*), from the first pass's latent list. The record's notes cover "one ballot" and "elected but no government can be drawn".

**Confirmed sound:**
- (e4) reaches the elected branch: `ElectedGovernment` installs the tabled CDU+CSU+SPD, the basis is Art. 63 Abs. 3, and the CDU leads.
- `Commitments` is all or nothing on `AllAccept`, which matches `ElectedGovernment`'s use of the tabled proposal. One refusing party releases every accepting one; binding by each party's own answer would be finer, and that is a note, not a defect.
- No random stream is drawn, and no saved field is added.
- Sweden is untouched: the commitments reach only the Bundestag ballot, `personBallot` is Bundestag-only, and the player-first rule sits inside the Bundestag phase-2 block. The sweep digest 86fbf292… is unchanged (n712b).

**Money path:** unchanged from the first pass. No arithmetic is touched.

**Verdict (second pass): NOT READY, two small fixes.** Defects 1-4 are closed. Fix defect 5 (the commitments in `Nominated`'s tally, so the bound parties sign their formateur's nomination; e4's Habeck should then not stand) and defect 6 (cache the projection with the verdict). Then this is ready without another full pass: e4's ballot line and one sheet frame-time reading will show both fixes. Notes A and B go to Elias with the record.

## The author's note after the second pass (2026-10-01)

- **Defect 5, fixed:** `Nominated` takes the commitments into its signing tallies (both, Abs. 2 and Abs. 3), so a bound party signs its formateur's nomination and never a rival's; the ballot and the sheet's projection read ONE candidate rule (`FourteenDaysCandidates`, the drift note). In (e4) Habeck no longer stands - the SPD's 120 are Merz's: *"Friedrich Merz (CDU) 329 - Friedrich Merz (CDU) elected, a majority of the members"* (n712c).
- **Defect 6, fixed:** the sheet's projection is cached by draft version and day (`GameController.FormationPersonBallot`), as the verdict beside it is.
- **For Elias, stated in COMPLETED §712:** only the tabling formateur's coalition is bound - a rival nominee's would-be partners vote by compatibility; a player whose nomination stands is asked first and so is always that formateur (an advantage, stated); the fourteen days hold one ballot.
