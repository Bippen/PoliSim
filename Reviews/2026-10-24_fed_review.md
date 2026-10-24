# Review: wave/fed - K15-K17, the Fed's Vice Chair acting, the nomination before the Senate, the crossing count (save format 63)

**Branch:** `wave/fed` in `G:/UNITY/Projects/PoliSim-wt/fed`. The range is `f0977344..0e18d8ea`, three commits: `797c13f4` (the sources and the model, format 63), `d106f112` (the UI and the film's 90h/90i staging) and `0e18d8ea` (`FedChairConsentDiagnostic`). The rulings read are K15-K17 in `docs/RULINGS_2026-10-23.md`.

**Reviewer:** independent and read-only. No Unity was run, nothing was pushed, and no code was edited. What was run:
- the compile gate, `bash Tools/cgate/gate.sh G:/UNITY/Projects/PoliSim-wt/fed`: `GATE OK`;
- `sha256sum -c ElectionsData/usa/raw/fed/SHA256SUMS.txt`: every row OK. Every file in the folder is listed except `SHA256SUMS.txt` and `fetch_log.txt`;
- an independent perl re-derivation of K17's constants and K16's lag from the raw XML and JSON (the script is in the review scratchpad, not the repo);
- a perl scan of every touched file for CRLF/LF mix and doubled CR, plus greps of the added lines for new `<see cref`, `//` inside string literals, gendered words and lost escapes.

**Files reviewed (the branch's own):** `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Simulation/UsFedConsent.cs`, `Assets/Scripts/Simulation/FederalReserveSystem.cs`, `Assets/Scripts/Simulation/RatePathProjection.cs`, `Assets/Scripts/Simulation/CentralBankTerms.cs`, `Assets/Scripts/Data/Country.cs`, `Assets/Scripts/Data/FedChair.cs`, `Assets/Scripts/Data/WorldFactory.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Editor/FedChairConsentDiagnostic.cs`, `Assets/Editor/PreviewParityDiagnostic.cs`, `Assets/Scripts/Testing/UiScreenshotDriver.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/UI/GameController.CongressV35.cs`, `Assets/Scripts/UI/GameController.DocketV35.cs`, `Assets/Scripts/UI/GameController.PoliticsV35.cs`, `ElectionsData/usa/raw/fed/SHA256SUMS.txt`, `ElectionsData/usa/raw/fed/fetch_log.txt`, and the raw files under `ElectionsData/usa/raw/fed/` that the rule and the lag are read from (`wayback_senate_vote_*.xml`, `voteview_RS*.json`, `congressgov_api_nomination_*.json`, `us_12usc244_govinfo2024.htm`, `federalreserve_pressrelease_other20220204a.htm`, `federalreserve_pressrelease_other20220110a.htm`).

Read for tracing, not changed by the branch: `Assets/Scripts/Simulation/ShadowBaseline.cs`, `Assets/Scripts/Simulation/PolicyImpactLedger.cs`, `Assets/Scripts/Simulation/Congress.cs` (`PresidentOn`), `Assets/Scripts/Elections/UsTermLimit.cs` (`TryPresidentOn`), `Assets/Scripts/Elections/UsCongressElection.cs` (`SenateOn`, `SeatSides`), `Assets/Scripts/Elections/UsSenate.cs` (`On`), `Assets/Scripts/Elections/UsPresidentialElection.cs` (`TryInOffice`, `Hold`), `Assets/Scripts/Elections/WorldClock.cs`, `Assets/Editor/NoPolicyCentury.cs`, `Assets/Editor/PlayerLeverLedgerDiagnostic.cs`, `Assets/Editor/ForkAnswersDiagnostic.cs`, `Assets/Editor/AuditDriver.cs`, `Assets/Editor/ConstantProvenanceCheck.cs`, `CLAUDE.md`, `Tools/cgate/IMPL_BRIEF.md`.

## Verdict: NOT READY

There are two H findings:
- **H-1** is a money-path regression. Now that the lever LODGES, the impact ledger's counterfactual worlds seat the AI's keyed nominee where the played world has the Vice Chair acting and then the player's pick. So during the window, or for the whole term, every family's line carries a Fed-chair difference that no policy made.
- **H-2** is player-visible broken text: the Docket prints the literal characters `00B7` where a middle dot belongs. That is the heredoc backslash trap.

Both are small to fix. The rest of the claims hold:
- The restructured AI branch is equivalent to the old one.
- The constants and the lag re-derive exactly from the files.
- "No Senate of 100 refuses" is true.
- The format step is done the project's way.
- The 2022 record is described accurately.

## The claims, one by one

### 1. The money path

- **`if (!fed || ConsentsToAiNominee(...)) seat` against the old `if (fed && !Consents) continue; seat;`:** equivalent. In both, `ConsentsToAiNominee` is evaluated only for the Fed (short-circuit). The old `continue` was the loop body's last statement before the closing `continue`, so nothing was skipped that the new form runs (`SimulationManager.cs:3908`).
- **The vote-day branch** (`SimulationManager.cs:3895-3903`) runs only for the USA, and only where `FedNominationPending != null` and the day has reached `VoteOn`. `own` is re-read after `Resolve`, so a confirmed nominee's fresh term is what the AI branch then tests. Correct.
- **The acting flag** is written only `if (fed)` (`:3914`), so it is the USA's alone. `PolicyHead` (`FederalReserveSystem.cs:173`) returns `CurrentFedChair` whenever the flag is false or no Vice Chair is seated.
- **`ApplyFedChairInterestRate` and the preview** read `PolicyHead(country).RateBias` / `PolicyHead(previewCountry)`. On a day the flag is false, that is the same object and the same field load inside the same expression, with no new local and no new store, so §400's inertness holds by shape (the dump diff is still the proof). `RatePathProjection.Project` and the desk readout read the same head.
- **`ClonePreviewCountry`** (`:7495-7498`) copies the flag by value and shares the three references. The preview never lodges or votes, so sharing is safe. `PreviewParityDiagnostic` covers the bool.
- **The AI path clearing a pending nomination** (`:3911`): it clears only where the AI seats, and the AI seats only where `PlayerAppointsCentralBankHead` is false. In the played world, that means the player has left the role gate (the "lapses" reading is reasonable). **In a counterfactual world it is always false, and that is H-1.**
- **Replay determinism of the lever itself:** fine. A replay lodges `candidate.Copy()` with the same party and day, and `VoteOn` is computed from the day, not read from the live world. **Ledger determinism is not intact: see H-1.**

### 2. The no-policy dump's byte-identity

I tried to break it four ways.

- **(a) No player, no hold.** In a world with no player (the dump's kind), `PlayerAppointsCentralBankHead` is false, so the AI branch runs on every term day. The president there is never the game's: `HoldUsPresidentialCount` returns at once without a player country, so the president comes from the record's government, and past the record from the sitting `country.Government` (`UsTermLimit.TryPresidentOn`). The Senate is the record's frozen at its reach (`UsSenate.On`).
- **(b) Bloc and crossing agree on the term days.** The old bloc confirms wherever the president's caucus holds more than half (or half plus the Vice President). In every such Senate the crossing rule also confirms. So the two builds seat the same AI nominee on the same day **exactly when the old bloc confirmed on every term day of the stepped century**. Where the bloc refused, the old build kept the outgoing chair. The new build seats the AI's pick (the crossing rule confirms everywhere), and from that day the trajectory moves.
- **(c) The Vice Chair never acts.** On a confirming day the chair is seated before the flag is computed, so the Vice Chair acts for no day. No stream is drawn: `AiNominee` is keyed, and `Vote`/`Yeas` are pure. `PolicyHead` returns the same object.
- **(d) Save content.** The dump hashes `EconomyState`, not the save. The format step changes only what a save carries.

So the argument is sound in form. But the instrument that is supposed to hold it no longer can (M-1). Separately, `NoPolicyCentury` seats `PlayerCountryId = player`: for the USA, the seeded chair's term then ends unfilled for the rest of the century. The old build kept Harriet Ellsworth there and the new one has Rowan Achterberg acting. The rate is identical only because both leans are `0f` (L-4).

### 3. The constants, re-derived (perl, from the raw files)

The independent re-derivation reproduces every constant from the files.

On the four roll calls of the majority-cloture era (the Senate's XML, independents with the Democrats):

| vote | president's caucus (yeas-nays) | other caucus (yeas-nays) | margin | crossing |
|---|---|---|---|---|
| Yellen 2014 | 55 (45-0) | 45 (11-26) | +10 | 0.297297 |
| Powell 2018 | 51 (44-4) | 49 (40-9) | +2 | 0.816327 |
| Powell 2022 | 50 (44-6) | 50 (36-13) | 0 | 0.734694 |
| Warsh 2026 | 53 (53-0) | 47 (1-45) | +6 | 0.021739 |

- **The fitted constants:** pooled in-party 186/196 = 0.948980; least-squares intercept **0.748131**, slope **-0.062359** per seat. These are `UsFedConsent`'s constants exactly.
- **The out-of-sample roll calls:**

| vote | margin | crossing |
|---|---|---|
| Greenspan 1996 | -6 | 52-0 (1.000) |
| Greenspan 2000 | -10 | 50-0 (1.000) |
| Bernanke 2010 | +20 | 22-18 (0.550) |

- **The lags, received date to vote** (congress.gov `receivedDate`, the XML `vote_date`): 10, 15, 70, **89**, 97, 128, 133. The median is **89**, which is `NominationToVoteDays`.
- **The diagnostic re-derives rather than restates.** Section (k) recomputes the pooled share and the least-squares line from `SenateSplit` over the saved XML and holds each constant to 5e-6. Section (l) parses `receivedDate` and `vote_date` and holds the median.
- **SHA256SUMS.txt** checks clean. The two-crlf/thirty-lf/ten-none EOL mix in the folder is the fetched bytes (`-text`), and the digests match the working tree.

### 4. "No Senate of 100 refuses"

True. For own seats k of 100, the crossing share is clamp(6.984031 - 0.124718k):
- **k ≤ 47:** the share clamps to 1, and the yeas are 100 - 0.051k, at least 97.
- **k = 48:** 97.4 yeas.
- **k ≥ 50:** the president's caucus alone carries.

The narrowest count is **53-47 at k = 56**. There the crossing goes just below zero and clamps to 0, so only the 0.949 share of 56 votes yes. No k refuses. The record agrees: all ten roll calls confirmed, including three hostile Senates. But it means K16's refusal can only be reached through a null count (M-2).

### 5. Save format 63

- **The step:** 62 → 63, with ruling A's refusal. The existing "format before this build's is refused" checks are written on `CurrentSaveVersion - 1`, so they follow the step.
- **The new fields:** all four are public fields on `Country`. `FedNomination` and `FedSenateVote` are `[Serializable]` with public fields. The save is Newtonsoft with `PreserveReferencesHandling.Objects`, so the fields travel without a line in `SaveGameService`, as `CurrentFedChair` always has.
- **Not proved by any check:** a round trip of a save cut INSIDE the window (pending nomination, flag true) that loads into the game and votes on the day (L-5).

### 6. The source reading

- **12 U.S.C. 244 as fetched** says the vice chairman presides "in his absence" at meetings of the Board. Reading an ended designation with no successor confirmed as the Chairman's absence is a DECLARED stretch, and the fetch log says so: neither section says an expired designation is an absence. It is reasonable under K15, which rules the outcome.
- **What 244 does not cover:** it governs who presides at Board meetings. The policy rate is the FOMC's, and the FOMC chooses its own chair (12 U.S.C. 263 is not fetched). The game reads "the head whose lean the rule reads" off 244. That is K15's ruling in effect, and it is worth one line to Elias (L-6).
- **The 2022 case is described accurately.** Both releases are quoted exactly. Clarida announced on 10 Jan that he would resign on 14 Jan 2022, and the Board named Powell Chair Pro Tempore effective 5 Feb, "pending Senate confirmation". That is 244's third sentence (the Board electing a member in the absence of both), not a Vice Chair acting. The class note says so. Elias cited 2022 as support for K15, so he should hear that it is the other case (L-6).

### 7. UI text

- No gendered text is added. The only "his" words in added lines are quotations of the statute and of the Fed's release. "the game shows him" / "where he decides" / "His term has ended" are removed.
- The new Congress, bank and Docket lines read correctly, **except H-2**.
- No added line puts `//` inside a string literal.
- The one `<see cref` on an added line (`SimulationManager.cs`, the AF-7 summary) is a pre-existing cref on a rewrapped line, not a new one.
- Every touched file is uniformly CRLF with no lone LF and no doubled CR.

## Findings

### H-1: the impact ledger's except-worlds seat the AI's nominee where the played world has the Vice Chair acting and then the player's pick
`Assets/Scripts/Simulation/SimulationManager.cs:3864-3865` (`PlayerAppointsCentralBankHead` is false in any `IsCounterfactual` world), `:3905-3912`, `:3850`; `Assets/Scripts/Simulation/ShadowBaseline.cs:313-316` (`AdvanceDay`, then `DoScheduled`).

Before this branch, the verb seated at once. An except-world reaching the term day seated the AI's keyed nominee in its own `AdvanceDay`, then the replayed lever overwrote it with the player's copy the same day, so the turn-close rate matched the played world's. Now the replayed lever only LODGES, and an except-world never holds. There are two concrete failures:

1. **An except-world that existed before the term ended** (any family the player had touched earlier) runs day T's `AdvanceDay`: the chair's term has ended and the world is counterfactual, so the AI's nominee is seated with its own lean. `DoScheduled` then lodges the player's nominee, which votes on T+89 and replaces the AI's nominee. The played world has the Vice Chair (lean 0) for those 89 days. Any turn close inside the window applies a different rate in the except-world, and the difference is booked to that family's line and the interaction.
2. **A family first touched DURING the window** forks from the played world's save, with the nomination pending and the flag true. On the fork's next day the AI path seats the AI's nominee and clears the pending nomination (`:3911`, "lapses"). The player's pick is never seated there. For the rest of the term, every rate that fork applies differs from the played world's, and the ledger attributes four years of Fed-chair difference to, for example, a tax dial.

The claim "counterfactual forks seat on the vote day" is false for both. `PlayerLeverLedgerDiagnostic`'s script pulls the head lever on turn 2 day 50, before the seeded term ends, so its "except-world EXACT" check cannot see this.

**What a fix must do:** while the played world's nomination is pending, or its hold is up for the player, a world that replays the player's levers must not run the AI's pick for the player's own bank. Only the CentralBankHead family's own except-world, which withholds the lever, should take the AI's pick. Add a case to `PlayerLeverLedgerDiagnostic` (or `FedChairConsentDiagnostic`) with a lever pulled at the term's end and a family first touched inside the window, each except-world held equal to the seed's world without its family.

### H-2: the Docket prints `00B7` where the middle dot belongs
`Assets/Scripts/UI/GameController.DocketV35.cs:63`, `:64`, `:70`.

The source holds `" 00B7 "`, not `"\u00B7"` or `" · "`. That is CLAUDE.md's heredoc trap: the backslash was eaten. Every pending nomination's Docket title reads "Before the Senate 00B7 <name>". Its slip reads "... BY A MAJORITY 00B7 ACTING: THE VICE CHAIR, ...", and the refusal slip reads "ACTING: ... 00B7 THE CHOICE ...". The film's `90h_docket_before_senate` frame will show it, but no guard reads the text. The fix is the literal `·` that the neighbouring lines use. A grep of the branch finds no other lost escape.

### M-1: `FedChairConsentDiagnostic` (f) can no longer fail on the thing its message claims
`Assets/Editor/FedChairConsentDiagnostic.cs:428-458`.

(f) was the review's M4 guard that "no start's trajectory moves". Under the crossing rule every count confirms (finding 4), so (f) now fails only on a null count. It cannot see a term day where the OLD bloc count would have refused, which is the only way the two builds part. Two further gaps:
- It counts on an unstepped world: the government at creation, the Senate frozen at the record's reach.
- Its pass line still says "this start's trajectory does not move by US-29".

**What is owed:**
- The no-policy dump diff and `TrajectorySentinelCheck` must be run, and nothing else stands in for them.
- (f) should also hold the bloc count, `own > other` or the Vice President's tie, on each term day, so the guard keeps its teeth while the old rule is the baseline's.
- (f)'s message should say what it holds.

### M-2: K16's "refusal plays as a visible event" is unreachable in play
`Assets/Scripts/Simulation/UsFedConsent.cs:160` and finding 4.

Under the fitted rule no Senate the game can seat refuses. A refusal arises only where `Vote` returns null: no president found, or no Senate seated or on record. That is a defect, and it is then recorded and shown to the player as **"The Senate refused X, 0-0"**: a fake 0-0 tally presented as a real vote.

- **Where the rule comes from:** the line is fitted on margins 0..+10 only. Its hostile-Senate behaviour (the share clamped to 1) is extrapolation. It agrees with the record's three hostile Senates (Greenspan 1987, 1996, 2000) but was not fitted on them. The [AUTHORED-DRAFT] label is honest.
- **For Elias:** the ruled event exists only in the film and the diagnostic, where it is planted. Is that what K16 meant?
- **Separately:** a null count should not be written as a refusal with a 0-0 tally. Log it as the defect, and either leave the nomination pending or record it with a flag that the UI words differently.

### L-1: a standing refusal outlives the AI's confirmation
`UsFedConsent.cs:179-184`; `GameController.DocketV35.cs:66-72`.

`StandingRefusal` returns the last vote while it was a refusal and nothing is pending. Scenario:
- the player's nominee is refused;
- the player leaves the presidency;
- the AI's nominee is seated by the AI branch, which records no `FedSenateVote`.

The Docket then keeps "The Senate refused X ... the Vice Chair acts" for the rest of the game, although a chair sits and no one acts. Rare (it needs M-2's null count), but wrong when it happens.

### L-2: `UsFedConsent.Confirms(Country, DateTime)` has no caller left
`UsFedConsent.cs:139`. It is dead public surface: remove it, or say why it stays.

### L-3: a refusal redraws the player's shortlist on the counted stream
`GameController.cs:3872-3876`.

The key now carries the votes' count, so after a refusal `FederalReserveSystem.GenerateCandidates` draws again from `RandomSource`, the counted Federal Reserve stream, from the UI. The first draw already worked this way, so the pattern is old. A second draw is new stream movement in the played world that no fork makes. Low, because it is reachable only through M-2.

### L-4: `NoPolicyCentury`'s USA is identical only because both leans are 0
`WorldFactory.cs:1025-1028`; `Assets/Editor/NoPolicyCentury.cs:70`.

With `PlayerCountryId = USA` and no one ever picking, the term ends unfilled for the rest of the century. The old build kept the seeded chair (lean `0f`), and the new one has the Vice Chair acting (lean `0f`). That is equal today. A later change to either seed lean would move the USA's no-policy century with no Fed decision anywhere. The Vice Chair's comment says it is seated at the same bias "so a holdover's rate reads the rule alone". It should also name this dependency, or a check should pin the two leans equal.

### L-5: no check round-trips a save cut inside the window
Nothing loads a v63 save cut with `FedNominationPending` set and the flag true into the GAME (§557: not just a manager), and then holds that it votes on `VoteOn` and seats the nominee with the same `For`/`Against`. The serializer should carry all of this. The project's rule is that a check proves it.

### L-6: two things Elias should hear about K15's sources
1. **What K15 rules in effect:** 244 says who presides at Board meetings, while the rate in the game is the FOMC's, whose chair the FOMC elects (12 U.S.C. 263, not fetched). So K15 in effect rules "the Vice Chair's lean sets the rule's bias".
2. **The 2022 case:** Elias cited it as support, but it is the Board electing the outgoing chair pro tempore with the Vice Chair seat empty. It is not an instance of a Vice Chair acting, and the game never models the Board's election. Both are stated in the class note, and neither is in an ask yet.

### L-7: `AuditDriver`'s "HOLD chair: appointed <name>" now names the outgoing chair for the USA
`Assets/Editor/AuditDriver.cs:867-871`. The log reads `c.CurrentFedChair.Name` right after the verb, which now lodges, so the audit log names the outgoing chair as "appointed". Cosmetic, but the audit ledger reads these lines.

## Runs owed before merge (none were run here)
- **The checks:**
  - `FedChairConsentDiagnostic`: every section, with (k) printing the constants above and (l) printing the median 89;
  - `PreviewParityDiagnostic`;
  - `PlayerLeverLedgerDiagnostic` and `ForkAnswersDiagnostic` (the lever's semantics changed under them);
  - `CentralBankTermCheck`;
  - `SaveLoadRoundTripDiagnostic`;
  - `TrajectorySentinelCheck` and the no-policy dump diff (M-1);
  - the cheap bar.
- **The films:** the dry film and one real width for 90h/90i, read for H-2's text after the fix.
