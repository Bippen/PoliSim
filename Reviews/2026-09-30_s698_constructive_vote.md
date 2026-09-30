# Review — §698 PS-4, the constructive vote of no confidence (2026-09-30)

Reviewed: `git diff --cached` (all 7 files), plus ConfidenceProcedure.cs 20-165; SimulationManager.cs 270-330, 1160-1340, 2173-2260, 2395-2455, 2520-3237; FormationProposal.cs 1-247; SpeakerRound.cs 1-62; GovernmentRecord.cs 1-309; GovernmentFormation.cs 300-409; CoalitionFormation.cs 286-707; SupportAgreement.cs 55-136; DeclaredRedLines.cs 444-586; GameController.ParliamentRows.cs 200-445; GameController.cs 760-828, 1400-1419, 6835-6884, 7935-7960, 11360-11410; FiscalYearData.cs; Tools/bar_tier.ps1 36-40; the tail of Tools/review_ledger.tsv; the logs cvd698c.log (run on the staged state) and sim698.log. One independent reading, read-only. **Verdict NOT READY.**

## Defects

1. **An AI constructive vote seats the player's party without asking it.** `SimulationManager.cs:2773` passes `country.PlayerPartyAbbrev` to `Formateur.Answer` for an AI mover, and `FormationProposal.cs:155-159, 192/211` auto-accept for the player's party. The transient round (2765) has no `Declines`, so `DraftProposal` keeps the player's party in the cabinets and support it drafts, and `TryAiConstructiveVote` then installs it with an agreement (GovernmentRecord.cs:182-190). Proved in cvd698c.log:574, check (d), the player being SSW: *"CDU led by CDU with CSU+SSW+FDP takes office"*. The player went from Opposition to Support: `MoveNoConfidence` now refuses it (*"WITHDRAW YOUR SUPPORT FIRST"*), and a budget alternative it tables counts as a break. On the start chamber, a player playing the FDP (junior partner in SPD+Grüne+FDP) is moved out of its government into the CDU's by any AI successor that drafts the FDP. The Riksdag path never does this: a proposal involving the player becomes `OfferToPlayer` (3012-3017). **Fix:** for an AI mover, add `player + ">" + mover` to the transient round's `Declines`; the diagnostic's check (d) should assert the player's role is unchanged.
2. **The budget window soft-locks on a government change.** `TryOpenBudgetProcess` runs before `AdvanceConfidenceDay` (SimulationManager.cs:308-309). On 1 January, Germany's fiscal-year start, the player chancellor's window opens (2241-2244); the same tick, `TryAiConstructiveVote` installs a successor. The player is now in opposition: `IntroduceBudgetBill` refuses at the role gate (1292), and the desk draws the lever lock. Only lines 1173 and 1300 ever remove the country from `_pendingBudgetProcessByCountry`. With `HoldOnBudgetWindow` on, `BudgetWindowHolds` (GameController.cs:1402) holds the clock permanently. With it off, the clock runs but `TryOpenBudgetProcess` returns at 2195 every day, so the new AI government never tables a budget; with the hold off, this happens on any day the player's window is still open. **Fix:** `InstallSuccessor` drops the outgoing government's open window when the player stops governing. The Riksdag `Install` (3059-3071) has the same gap: not a Sweden regression, but newly reachable through the daily AI vote.
3. **A sitting partner's answer never weighs the government it already sits in.** `BestElsewhere` (FormationProposal.cs:132-141) compares only against the formation's holding set, and ConfidenceProcedure.cs:96-97 then lets a sitting partner "leave". Proved in cvd698c.log:618, check (c): the FDP holds cabinet posts in SPD+Grüne+FDP (a payoff above 0), yet accepts supporting a CDU cabinet from outside, worth 0 by the model's own rule (*"support buys no posts"*), only because no holding government seats the FDP. `Carried` requires the FDP's acceptance (line 80), so the carried result rests on a defection the model's own payoff says the FDP does not want; AI votes can carry on defections no party prefers. **Fix:** include a sitting partner's `PayoffIn(g.Cabinet)` in its alternative.
4. **A refusing partner is recorded as voting for the successor.** ConfidenceProcedure.cs:96-97 counts every drafted partner and supporter as For, whatever its answer; only line 80 gates `Carried` on `AllAccept`. A drafted supporter that refuses because another holding government seats it (FormationProposal.cs:195-196) is listed as voting for, and the title can read *"not elected, 380 of 736 members for, 369 needed"*. The row then shows "380 ⁄ 369 NEEDED" (ParliamentRows.cs:319) while the chip loses. **Fix:** a refusing party votes as the investiture has it.

**Latent:**
- **Cost.** Every German day, each AI candidate gets a full formation: `DraftProposal` and `Answer`, two chamber preparations and two `Form` passes over 512 cabinets. That includes seatless movers (the BSW at the start), with no cheap pre-filter like the Riksdag's `CanBeTakenUp` and `Vote`. `FreezeTabled` → `SupportAgreement.Demand` keys its template on the date (SupportAgreement.cs:86), and `World.SupportAgreementTemplates` is never cleared, so it grows without bound.
- **Instability.** Nothing stops the fallen chancellor's party moving back the next day, so the chancellorship could swap back and forth daily.
- **Basic Law.** After an election that forms no government the old record stands and is not a caretaker (GameController.cs:6841/6865), so a constructive vote can be moved against what Art. 69(3) makes an acting chancellor. The outcome matches an Art. 63 majority election; the premise should be stated.
- **Who may move.** "Any party" is a stated premise. If GO-BT §97(1) is sourced (recollection: a quarter of the members, or a parliamentary group of that size), it also cuts the daily candidate loop.
- **UI.** The row's cache is keyed on the record's reference and the date, not on `GovernmentRecord.Version`; that is safe today. No film of the new row exists, and its label is 11 characters longer than the Riksdag's.

## Confirmed sound, stated

- **Sweden unchanged.** `RulesOf(Sweden)` is still Riksdag, and the Bundestag branches are skipped before any Riksdag line runs. `PartnersAccept` defaults to true and `Vote` never sets it; `MotionVote` is not serialized. `DraftProposal`, `RoundReading`, `RoundLines` and `Formateur` are untouched, and `RoundsApply` stays Riksdag-only. `TrajectorySentinelCheck` passes in sim698.
- **The transient round is safe.** `MidTerm` routes the reading to `SittingReading`, so its `Vintage` and `ReadsOn` defaults are never read. Its lists are initialised, and refusals are copied, not aliased.
- **The chancellor's party is kept out.** "PmParty>mover" is a one-way line that `TryFindInternalRedLine` treats as internal, so no successor cabinet contains the chancellor's party and its support is blocked too.
- **`InstallSuccessor` matches `Install`.** Investiture is non-null whenever the vote carries. Posts, agreements, Kind, Executive and refusals are all carried, and the arrival window is reset. No saved field is added.
- **The forbidden cases are covered.**
  - A caretaker is never set in Germany, and both paths check for one.
  - Rounds never open in Germany.
  - The role checks hold, and AI candidates exclude the cabinet, the support and the player.
  - With no successor government, the vote carries only on the mover's own majority.
  - The week before polling day is guarded.
- **Determinism holds:** no stream is drawn.
- **Strings follow the rules:** upper case, with no § or "Art." on the row.
- **Tests:** `ConstructiveVoteDiagnostic` ran clean on the staged state, and the `ConfidenceDiagnostic` edit is correct.

**Money path:** yes by the project's definition (`SimulationManager` is in `$money`). In substance, no arithmetic is touched. A carried vote reaches the book only by changing who governs and by re-arming the arrival budget, the same reach `Install` has. The exception is defect 2's stuck window, which stops budgets altogether.

## Applied after the review (the author's note - not the reviewer's words)

- **Defect 1** - an AI mover's transient round adds `player>mover` to its `Declines`: the player's party is kept out of the draft's cabinet and
  support and votes as its lines have it. `ConstructiveVoteDiagnostic` (d) holds the player's role unchanged (SSW Opposition before and after).
- **Defect 2** - `CloseBudgetWindowIfNotGoverning` after `InstallSuccessor` and after the Riksdag `Install` (the same gap): a government change
  that leaves the player out of the chancellery closes the player's open window. (e) proves it: the SPD player's window open, the CDU's AI
  successor installed, the window closed.
- **Defect 3** - a sitting CABINET partner drafted into the successor compares `PayoffIn(the sitting cabinet)` with `PayoffIn(the successor's)`
  and stays unless the successor pays better (a supporter's place pays nothing): the planted chamber's FDP now stays and votes against.
- **Defect 4** - every drafted party votes by its own answer; a refuser abstains (never for). A refusing SUPPORTER only withholds its votes;
  a refusing CABINET partner sinks the successor (`PartnersAccept` reads the cabinet). The installed government carries no refuser.
- **Cost (latent)** - a stated, stateless cadence: the AI weighs a constructive vote on Mondays and on the day after a government forms (a cached
  key would re-weigh on a load day and part a loaded run from a continuous one); seatless parties move nothing. (d)'s cadence case holds it.
- **Instability, Basic Law, who may move, UI (latent)** - stated in the record; the row's placement not on film (the German film's player is a
  supporter, and the row draws in opposition only).
