# Review — §705 PS-4 follow-up 4, Germany's formation: the chancellor's election under Art. 63 GG (2026-09-30)

Reviewed: `git diff --cached` (all 22 files; the four PDFs and the Greens' HTML checked by size and SHA-256 against the register), plus Assets/Scripts/Simulation/SimulationManager.cs 2530-3438; SpeakerRound.cs 1-83; FormationProposal.cs 60-253; CoalitionFormation.cs 280-620 and 734-760; GovernmentRecord.cs 175-191, 215-309; GovernmentFormation.cs 376-402, 424-500; DeclaredRedLines.cs 134-209, 457-628; WorldClock.cs 380-459; ConfidenceProcedure.cs 77-137; GameController.cs 1410-1414, 6350-6354, 6776-6892, 7005-7126; GameController.ParliamentRows.cs 120-557; GameController.FormationSheet.cs (the diff, and grep of 60-322); GameController.CampaignDeclared.cs 1-252; GameController.CampaignCoalition.cs 280-310; ElectionNightFromModel.cs 38-39; NationalElection.cs 70-96; Assets/Editor/DeclarationDatesDiagnostic.cs 95-106; SaveGameService.cs 40-55; SaveGame.cs 20-32; ElectionsData/germany/records_by_date.md 99-256 (section 4 in full) and coalition_declarations_2025.md (whole); the logs gf705e.log (the five named checks on the staged state), drys705.log (grep), elec705.report (the German block), and formation_sweep_before705.txt against formation_sweep_mismatch.txt. One independent reading, read-only; Unity not run. **Verdict NOT READY.**

## Defects

1. **A suite check goes red on the staged state.** `Assets/Editor/DeclarationDatesDiagnostic.cs:102` asserts `DeclaredRedLines.StandingOn(CountryId.Germany, 2026-01-18).Count == 0` ("a country with no timeline stands nothing"). §705 gives Germany a timeline (`HasTimeline`, `TimelineOf`); all seven German facts are `Open`, so seven stand on that day. The file is not in the diff and the check was not among gf705e's five. **Fix:** assert Germany's seven, and move the "no timeline" case to a country that has none (Poland).

2. **The plurality ballot installs parties that never agreed.** `PluralityBallot` (SimulationManager.cs:3208-3231) keeps the proposal with the most `SupportedSeats` and never reads `AllAccept`. `Install` (3236-3244) takes `proposal.CabinetParties` whole.
   - **A refusing partner is installed.** A cabinet partner that refuses on payoff (FormationProposal.cs:179-185) is installed in the cabinet it refused, and its seats count in the tally as votes for the candidate.
   - **The player is never asked.** `Formateur.Answer` auto-accepts for the player's party (FormationProposal.cs:160-164, 197-216). An AI draft that seats the player, in cabinet or support, is installed without an offer. The Riksdag path, and phases 1-2 here, always turn such a draft into `OfferToPlayer` (3114-3119).
   - **The player's own party can be made chancellor.** `DraftProposal(country, round, player)` drafts a cabinet for the player's party, and it can be elected even after the player PASSed its turn.
   - **A CSU player's decline is undone.** The joint fallback (3003-3013) re-seats a player who declined the CDU's offer. The plurality ballot then installs the CSU in the cabinet it declined.

   **Fix:**
   - A refusing partner cannot stand: skip any draft without `AllAccept` for the AI parties.
   - Hold the player's party to its answers (its declines stand), and put a draft that seats it to the player as an offer first.
   - The player's own candidacy stands only if the player tabled or accepts it.
3. **The "most votes" tally is not the premise it states, and whenever phase 3 is reached it elects the largest Fraktion.**
   - **What the premise says.** The comment at 3198 states it as [AUTHORED-DRAFT]: "each party casts its seats for the strongest candidate whose government it accepts".
   - **What the code does.** The tally is `Evaluate(chamber, cabinet, support)` over the parties the draft lists. Phase 3 is reached only when no majority government holds, so `Holding` is empty and every draft is the fallback: the party alone (plus its joint partner), with no supporters. Each candidate is therefore tallied at its own group's seats; no other party's acceptance is ever asked.
   - **Proved on (e).** gf705e.log:835 elects Merz with the Union's 208. The Greens (85) and the Linke (64) are counted for nobody, although no planted or declared line bars either from the SPD's candidate (120 + 85 + 64 = 269 > 208 under the stated rule).
   - **Reachable in play.** In a game-held election where the AfD is the largest single group and no majority forms, the tally elects Weidel. Under the premise, the parties whose lines refuse her government would cast for another candidate.
   - **The check does not catch it.** Check (e) asserts only that someone was installed under Abs. 4, not who.

   **Fix:** tally per party. Each party goes to the candidate, among those whose government it would support (the evaluator's own support test), with the most such seats; drafts in the tally carry their derived supporters.
4. **The Bundestag's "same day" ballot is held the next day.** `Table` sets `VoteOn = CurrentDate` once the Bundestag has convened (3082). But the switch returns after `Table` (3121), and a player's `SubmitFormation` runs between ticks, so `Investiture` runs on the next day's tick.
   - **Proved.** gf705e.log:832-833: "2025-04-01: AfD forms AfD; the Bundestag elects the chancellor on 2025-04-01", then "2025-04-02: the Bundestag does not elect Alice Weidel".
   - **The desk says otherwise.** It reads "VOTE THE SAME DAY" / "TABLED, THE BUNDESTAG VOTES THE SAME DAY" (ParliamentRows.cs:193), and the VotePending stamp shows `VoteOn`.
   - **A day-14 proposal is balloted outside the window.** A proposal tabled on the fourteenth day is balloted on day 15, outside "binnen vierzehn Tagen". The plurality trigger skips it because `Stage == VotePending` (3107). Carried, it is installed as an Abs. 3 election (3139, 3241).
   - **The second phase-2 candidate never reaches a ballot.** Phase 2 holds two candidacies at most. The second one's consultation is cut off: gf705e.log:834 "the Bundestag weighs Olaf Scholz", and no ballot follows.

   **Fix:** hold the Bundestag's ballot on the tabling day. Run `Investiture` from `Table` when `VoteOn == CurrentDate`, or date `VoteOn` as the day it is held. Count the fourteen days against the ballot's actual day.
5. **The constructive vote stays open inside the chancellor's election.**
   - **The window.** Before the convening, the outgoing government is not a caretaker (3033, 3100). So the opposition's row draws ELECT A SUCCESSOR (ParliamentRows.cs:514-515), and `MoveNoConfidence` has no round guard (2548-2561). It works even while the player is PlayerAsked with the clock held.
   - **A carried vote breaks the round.** `InstallSuccessor` replaces `country.Government`. The open round is orphaned on the old record, and the successor's `FormedOn` is after the polling day, so no round reopens (3286). The successor is then never discharged at the convening, against Art. 69 Abs. 2 ("endigt in jedem Falle mit dem Zusammentritt eines neuen Bundestages").
   - **The vote reads the wrong chamber.** It is taken on the new chamber's seats (set on polling day) by what is in law still the old Bundestag.
   - **Only the AI is guarded.** The AI path is kept out by `AdvanceConfidenceDay`'s round branch (2663); the player's path is not.

   **Fix:** no constructive vote while `RoundOf(country)` is open, in both `MoveNoConfidence` and the row.
6. **Older German saves re-run the formation on load; a save-version bump is owed.**
   - **The old state.** Before §705, a German election night installed the formation's government with `FormedOn` = the polling day (GameController.cs:6856), and `ProcedureResumedAfter` stayed `MinValue` (`RoundsApply(Germany)` was false).
   - **The reopen.** A v37 save made after an in-game German election loads under `CurrentSaveVersion` 37. The next day `OpenRoundAfterElection` finds `held < g.FormedOn` false and `held <= ProcedureResumedAfter` false, so it opens a chancellor's election (3286-3295) against the government the election already formed.
   - **The cascade.** On the first tick past the 30th day, the sitting government is discharged, backdated to the convening (3100-3104). The formation re-runs and installs again, with a second `ResetArrivalBudgetWindow` (3250).
   - **The check misses it.** Its note (GermanFormationDiagnostic.cs:118, "before §705 no German round could exist in a save") covers rounds only. The project's rule bumps whenever an older save would load into a different game state (the 35 bump's reason, SaveGameService.cs:53).

   **Fix:** bump to 38, or treat a German record formed on the polling day as the procedure's result (`ProcedureResumedAfter = held`).
7. **The joint group breaks when one member holds no seat.** `JointMasks` builds the group from the roster, not the seats (GovernmentFormation.cs:488-497).
   - **Why a seatless CSU is reachable.** The game's German count has no Grundmandatsklausel (NationalElection.cs:84-85). The CSU took 5.2 % in 2021, and elec705.report puts it at 2.57 % and 1.72 % under two of its layers.
   - **The CDU can never govern.** With the CSU seatless, `SplitsJoint` rejects every cabinet that holds the CDU (CoalitionFormation.cs:435, 530-531), so the largest party can never govern.
   - **The formation can throw.** `JointAlike` re-adds the seatless CSU to any support mask the CDU is in (CoalitionFormation.cs:755, called at 457 and 533). `AssertNoSeatless` then throws in `Form` (359-367) whenever a viable government has the CDU supporting from outside.
   - **Only one path knows.** `DraftProposal`'s fallback guards this case (`SeatsOf(partner) > 0`, 3011); `Prepare` and `Evaluate` do not.

   **Fix:** drop a group from the masks when any member holds no seat (pass the seats to `JointMasks`, or mask out `SeatlessMask` in `SplitsJoint` and `JointAlike`).
8. **The DECLARED page opens for Germany and says what §705 ruled false.** `DeclaredPageAvailable` keys on `HasTimeline` (CampaignDeclared.cs:33-34), so the German Campaign HQ now carries the DECLARED chip (GameController.Campaign.cs:307).
   - **A false slip.** The candidacies slip says "THE REFUSAL BETWEEN RIVALS IS THE MODEL'S RULE" (212). For Germany, `CandidacyRefuses` is false.
   - **The wrong office.** Each candidacy row says "names Friedrich Merz for prime minister" (61, 110).
   - **Missing candidacies.** Three of the four 2025 candidacies are dated at polling day, so for the whole run-up the page shows the Greens' alone.
   - **No film.** No frame covers the page: drys705's German pass has no HQ or declared frame.

   **Fix:** make the slip and the verb follow `CandidacyRefuses` and the chamber, or keep the page Swedish until it is designed for Germany.
9. **The two [AUTHORED-DRAFT] premises never reach the player.** The appointment premise ("the Bundespräsident appoints rather than dissolve ... the game's premise") is written only to the round's log (3227-3228), and that round is Concluded in the same call. `RoundOf` then returns null, and the round lives on the replaced record, so no slip ever draws it. `GovernmentRecord.Basis` is shown nowhere in the UI. What the player can see is the division title, "elected with the most votes, short of a majority", under "Art. 63 Abs. 4 GG", which reads as the constitution's outcome. The tally premise exists only in a code comment. The Phase 3 wording (ParliamentRows.cs:183) is unreachable for the same reason. **Fix:** put both premises in the division's title or reasons, or on a record the Parliament tab draws.

**Latent:**
- **Riksdag words on German paths.**
  - Log and slip lines: "the new Riksdag's round opens" (3293), "the Speaker moves on" (3363, which reaches the round's slip).
  - Refusals the sheet shows: "THE SPEAKER HAS NOT ASKED YOUR PARTY" (3305, 3337).
  - The caretaker slip: "UNTIL A PROPOSAL WINS ITS INVESTITURE" (ParliamentRows.cs:150).
  - The sheet's "THE PRIME MINISTER'S PARTY" and "THE INVESTITURE" (FormationSheet.cs:148, 256-257, 322).
- **The Treasury lock contradicts the German record.** `HoldsTreasury` (3329) now refuses a German player's sheet unless the chancellor's party holds Finance. The record has Klingbeil (SPD) at Finance under Merz (records_by_date.md:177) and Lindner (FDP) under Scholz (:109). It is a spec premise, newly reachable; it needs a ruling.
- **"The largest party's candidacy" counts party seats, not the Fraktion's** (`PmOf` 2948-2956, `FromView` 267-269). With the SPD between the CDU and CDU+CSU, the SPD's candidate leads a CDU+CSU+SPD cabinet and the Union's draft falls back to CDU+CSU alone.
- **The joint fallback ignores `Declines`.** In `DraftSuccessor`, a CSU mover re-seats the player's CDU against "CDU>CSU" (§698's defect-1 shape). It carries only on a Union majority of its own.
- **A split Union.**
  - `LeaveGovernment` lets a CSU player leave a CDU+CSU+SPD cabinet, breaking "both or neither" by a player verb.
  - `SplitsJointGroup` is read nowhere, so a CDU+SPD sheet shows every partner accepting and the investiture failing with no reason.
- **A Bundestag round opened without `electionDay` runs the Riksdag's rules** (3030): discharge, the 4-day vote, `ProposalLimit`, and a break-off to "the Riksdag rejected" extra election. It is unreachable today (`DischargeAndRound` needs `NoConfidenceOn`); a future German mid-term round (a resignation is Art. 63 too) would take Sweden's procedure silently.
- **The night's Bundestag sentence never shows.** Germany has no night (ElectionNightFromModel.cs:38-39), and `CheckElection` applies and clears the verdict at once. Check (f) proves a string no screen draws.
- **Who may nominate** (GO-BT § 4, from recollection; not in the saved sources). Candidacies in phases 2-3 need a quarter of the members or a Fraktion of that size. The AfD's 152 of 630 would not qualify alone; the game lets any party in the order stand.
- **Gaps in the check.**
  - (e)'s player, the FDP, holds no seat in `table25`, a run-ending state live, so no player path in phases 2-3 is exercised.
  - `ballots` counts every division: it printed 4 where the log shows three ballots (03-25, 04-02, 04-09).
  - The plurality winner is not asserted.
  - The save case round-trips a new save; it never loads a pre-§705 post-election German one.
- **Cost.**
  - `DraftSuccessor`'s loop is up to five drafts, answers and trial votes per mover per Monday.
  - `DrawReferenceRow` rebuilds `TryReference` (a seat dictionary and a regex) on every OnGUI event, uncached.
- **Dating.** The CDU, SPD and AfD candidacies are dated at polling day, so the mid-term reading from the snap start to 22 Feb 2025 knows only Habeck's.
- **No real film.** The German round, sheet and reference row have a dry pass only (drys705), and a dry pass cannot claim their text. The sheet's head "ASKED BY THE BUNDESPRÄSIDENT" is eight characters longer than the Swedish head.

## Confirmed sound, stated

- **Sweden unchanged.**
  - `chamber.Joint` is null for Sweden, so `Prepare`, `Evaluate` and `JointAlike` reduce to the old code.
  - `JointGroups` is empty, so the `AiMotionCandidates` filter and the `DraftProposal` fallback do nothing.
  - `CandidacyRefuses(Sweden)` keeps the pairing lines, the answers' guard and the created-party backing line.
  - `PmOf` and `FromView` agree with the old first-match rule wherever at most one candidacy is in the cabinet. The one-way pairing lines guarantee that for every admissible Swedish cabinet.
  - `RecordInvestiture` appends the same title, sides and flag.
  - Every Riksdag log line and desk string comes out byte-identical through `RoundAsker`, `RoundRule`, `RoundVoteWhen`, `RoundPassLine`, the hold banner and `SpeakerLine(bundestag: false)`.
  - `DrawReferenceRow` returns before reserving a row.
  - `DraftSuccessor` is Bundestag-only.
- **The dated arithmetic.**
  - `Convenes` = polling day + 30 is 2025-03-25, the record's constituent sitting.
  - `SecondPhaseUntil` = the failed ballot + 14, and the ballot the most votes win comes on day 15 ("unverzüglich").
  - `MajorityOf` = 630 / 2 + 1 = 316, the Art. 121 majority of members.
  - The outgoing government stays in office to the eve and is discharged dated on the convening day (Art. 69 Abs. 2-3).
  - The phase-1 ballot falls on the convening day.
- **Saves.** The new fields ride Newtonsoft. A v37 Swedish round loads with `Convenes = MinValue`, i.e. the Riksdag's round, and no older save can hold a German round (defect 6 is the post-election record, not a round).
- **No break-off.** `BreakOff` and `ProposalLimit` are unreachable for the Bundestag's round: `Investiture`'s branch returns first, and the order never empties.
- **An election during an open Bundestag round** concludes it and opens the new one; a caretaker stays a caretaker.
- **Declarations.**
  - The CDU and CSU lines are symmetric and support-blocking, as the record argues, and the CSU's does not stand on 2021's polling day.
  - The raw sources are byte-exact: the sizes and SHA-256 of all five staged files match the register, and the HTML is stored with `text` unset.
- **The sweep re-pin.** The moved lines lie only in `Germany | seated` (lines 31970-32115 of the two texts). The pin is the measured digest 86fbf292... (gf705e.log:1619), and the other four named checks passed on the staged state.
- **The joint rule is correct for seated members:** `SplitsJoint`, `JointAlike`, and `Evaluate`'s votes-as-one block (larger member's side and reason, oppose mask aligned).
- **The reference row has no null path.**
  - `HeadSurname` is set whenever `HeadParty` is.
  - `GovernmentLine` is set in all three branches, and `Seats` comes from a sourced table.
  - The row returns for another chamber, and for a game-held election with no record.
  - Its strings are upper case.
- **Determinism:** no stream is drawn anywhere in the change.

**Money path:** yes by the project's definition (SimulationManager.cs is in `$money`). No arithmetic is touched and no stream is drawn. The change's reach into the book:
- **Timing.** It shifts who governs Germany from polling day to the chancellor's election. The outgoing government now governs, undischarged, for at least 30 days, where election night used to install the new one at once. The arrival budget window moves to the install's date; `Install` and `PluralityBallot` (through `Install`) reset it and close a non-governing player's window, as the Riksdag path does.
- **Exceptions.**
  - Defect 6 re-fires `ResetArrivalBudgetWindow` on a loaded old save, for a government that already had its arrival budget.
  - Defect 5 installs a government that the convening never discharges.
  - Defect 2 hands the book to a cabinet containing parties that refused it.

## Second pass (the fixes)

Reviewed: `git diff --cached` (29 files; the three new raw sources checked by size and SHA-256 against the register, and each quoted sentence found in its saved page). I read:
- **Assets/Scripts/Simulation/SimulationManager.cs:** 2540-2600, 2840-2870, 2900-3490 (the whole round, `TableAndVote`, the rewritten `PluralityBallot`, `Install`, `OpenRoundAfterElection`, `SubmitFormation`, `PassFormation`, `AnswerOffer`).
- **The rest of the diff:** FormationProposal.cs; GovernmentFormation.cs 484-501; GovernmentRecord.cs (`FromView`, `Largest`); CoalitionFormation.cs 64; DeclaredRedLines.cs (`GermanyTimeline`); GameController.CampaignDeclared.cs; GameController.FormationSheet.cs; GameController.ParliamentRows.cs; UiScreenshotDriver.cs; DeclarationDatesDiagnostic.cs; GermanFormationDiagnostic.cs; coalition_declarations_2025.md.
- **Logs:** gf705f.log (8 checks, started 23:46:53) and gf705g.log (3 checks, started 23:49:31). drys705d.log, the Swedish half, tests the harness change. sim705.log was still running when read.

### Per defect

1. **CLOSED.** DeclarationDatesDiagnostic.cs:102-103: the no-timeline case is Poland, and Germany stands all `GermanyTimeline.Count` facts. It is clean in gf705f.
2. **NOT CLOSED (one case left).** The AI side is fixed:
   - `PluralityBallot` adds the player's decline for the winner's draft only for that draft (SimulationManager.cs:3283-3289).
   - A draft involving the player, one without `AllAccept`, or one with no investiture falls back to the winner's party and group (3291-3301). So no refusing partner and no unasked player is installed.

   **Still open:** the player's own party. It stands as a candidate whenever it is in the order (3235), votes for itself (3250-3252), and, if it wins, is installed as the government "on its party and group alone" (3283, 3293-3297). That happens even after the player PASSed the Bundespräsident's or the Bundestag's ask (`PassFormation`, 3455-3463), so the player becomes chancellor of a minority cabinet it never tabled. This is the first pass's third case. The draft is gone, but the unasked candidacy remains. **Fix:** in phase 3 the player's party stands only if the player tabled a proposal in this round and did not pass. Otherwise it votes like any party.
3. **CLOSED.** The tally now implements its stated premise: own candidate, then the group's, then the nearest candidate the party does not refuse by any support-blocking line in the round, else abstain (3245-3271). The premise is written on every side's reason. Proved on (e): gf705g.log:926-934, CDU 208, AfD 152, SPD 120, Grüne 149 (Linke's 64 to Habeck).
   - **A consequence for Elias to rule on, not a defect.** Under "sincere votes" every party with a candidacy votes for itself. A chamber where the AfD is the largest group and no majority forms elects Weidel, though every other party's lines refuse her government. The division now says it is the premise.
4. **CLOSED.** `TableAndVote` (3102-3106) holds the ballot on the tabling day in a convened Bundestag. It covers all four tabling sites: the consultation (3139), `SubmitFormation` (3434), and `AnswerOffer`'s accept (3477) and re-draft (3484). gf705g.log:926 shows phase-2 ballots on 04-01 and 04-08, both inside the window. The desk's "VOTE THE SAME DAY" is now true. Before the convening, the ballot still waits for `Convenes` (3092).
5. **CLOSED for the open round:**
   - `MoveNoConfidence` refuses while `g.Round` is open (2555).
   - `TryAiConstructiveVote` returns (2856).
   - The row is hidden while `RoundOf` is set (ParliamentRows.cs:529).

   The day before the round opens is not covered: see new defect A.
6. **CLOSED, but the fix opens new defect A.** `OpenRoundAfterElection` skips `held == g.FormedOn` for the Bundestag only (3408); the Riksdag keeps `<`. A pre-§705 post-election German save loads as it was (the new (d) case, gf705g.log:925).
7. **CLOSED.**
   - `JointMasks` takes the seats and drops a group with a seatless member (GovernmentFormation.cs:491-501). Both runtime callers pass seats (FormationProposal.cs:99, GovernmentFormation.cs:427), and so does the sweep.
   - `SeatedGroupPartner` requires both members seated (3334-3342).
   - The sweep pin is unchanged, and (b) proves the CSU-out case.
8. **CLOSED.**
   - "For chancellor" in Germany (CampaignDeclared.cs:61, 72, 110).
   - The candidacies slip follows `CandidacyRefuses` (216).
   - The three candidacies are re-dated from sources saved whole: CSU 12 Oct 2024 (marked DERIVED), SPD-SH 25 Nov 2024, ZDF 7 Dec 2024. The sizes and SHA-256 match the register, the three quotes are present verbatim, and `text` is unset.

   The German DECLARED page is still unfilmed (latent).
9. **CLOSED.**
   - The Abs. 4 division's title carries "appointed, not dissolved (the game's premise)" (3319-3320).
   - Each side's reason carries the tally's premise (3267, 3317).
   - The basis says it too (3330).

### New defects the fixes introduce

A. **A constructive vote on polling day escapes the chancellor's election** (from fix 6).
   - **The path.** The count runs in the frame the date becomes polling day (GameController.cs:806-809), but the round opens only the next day (`e.Date < CurrentDate`, 3392). For the rest of polling day, `g.Round` is null. So the opposition's ELECT A SUCCESSOR draws (ParliamentRows.cs:529), and `MoveNoConfidence` has no polling-day guard (2551-2563; the AI's week-before guard at 2857 is AI-only). It votes on the new chamber's seats.
   - **The escape.** A carried vote installs the successor with `FormedOn` = the polling day. The next day, `held == g.FormedOn && IsBundestag` (3408) takes that government for the election's own, so no chancellor's election opens. The successor is never discharged at the convening, against Art. 69 Abs. 2, "in jedem Falle".
   - **Why fix 6 opened it.** Before fix 6, the equality reopened the round.

   **Fix:** tell the pre-§705 record apart by something other than its date (its `Basis` is `FromView`'s "the formation on the chamber the game elected"). Or refuse the constructive vote once an election has been held that no round has followed (`held >= g.FormedOn && held > g.ProcedureResumedAfter`), which also covers the AI.
B. **A CSU player's decline splits the Union in the government the Abs. 4 ballot installs.**
   - **The trigger.** With the player as the CSU and the CDU's candidate winning, `PluralityBallot` adds "CSU>CDU" (3285-3287).
   - **The CDU-alone draft.** Every admissible CDU cabinet holds the CSU, so `DraftProposal` finds no holding government. The fallback respects the decline and returns the CDU alone (3014-3021).
   - **It passes the filter.** That draft involves no player, every partner accepts, and its investiture is non-null. `Evaluate` marks it inadmissible (`SplitsJointGroup`), but 3291 never reads that, so a CDU-only cabinet is installed (3328).
   - **The contradiction.** The CSU's 44 are counted for Merz as "its parliamentary group's candidate" (3252), and the model forbids exactly this split everywhere else (`SplitsJoint`, `LeaveGovernment` at 2587-2592, the fallback's own comment at 3019-3020).

   **Fix:** where the winner's seated group partner is the player, put the group's cabinet to the player as an offer. Or at least refuse an inadmissible draft (`!verdict.Investiture.Admissible`) and let the fallback and the offer decide.

### Sweden, byte for byte

Unchanged in behaviour, helper by helper:
- **Joint groups.** `JointGroups(Sweden)` is empty. So `SeatedGroupPartner` is null, the `DraftProposal` fallback adds nothing, `GroupSeats` = `SeatsOf`, `Largest(byGroup)` adds nothing, `LeaveGovernment`'s guard never fires, `AiMotionCandidates`' filter is inert, and `JointMasks` returns null. The formation, `Evaluate` and `Joint`-null paths are identical.
- **Formation answers.** `FormationProposal`'s reason gains a `SplitsJointGroup` arm that is always false with no groups (FormationProposal.cs:230). `CandidacyRefuses(Sweden)` keeps K-1f in the lines, the answers and the created-party line.
- **The round.**
  - `TableAndVote` is `Table` plus a Bundestag-only vote (3105); the Riksdag's fourth-day vote is unchanged.
  - `OpenSpeakerRound`'s discharge condition reduces to the old one for the Riksdag (3043).
  - `OpenRoundAfterElection` keeps `<` for the Riksdag (3408).
  - `Install` differs only in the optional `basis` and the Bundestag log arm.
  - Every refusal, log line and desk string takes its Riksdag arm unchanged: "THE SPEAKER HAS NOT ASKED YOUR PARTY", "the Speaker moves on", "the new Riksdag's round opens", the caretaker slip "UNTIL A PROPOSAL WINS ITS INVESTITURE", the sheet's PM, TREASURY and INVESTITURE words, "for prime minister", and "THE REFUSAL BETWEEN RIVALS IS THE MODEL'S RULE".
- **Checks.** `SpeakerRoundDiagnostic` (1)-(7) is clean on the final SimulationManager (gf705g.log:1425-1441). Its (4) caught the both-chambers `<=` in gf705f.log:1421, and the fix restored `<` for the Riksdag.
- **The harness.** The tour's close-the-self-opened-sheet step (UiScreenshotDriver) is inert for Sweden. In drys705d, Sweden's tour round asks M (Consulting, line 12015), not the player; the only Swedish PlayerAsked is 07a's staging, which is exempted.

### TableAndVote cannot recurse or double-install

`TableAndVote`, then `Investiture`, then exactly one of three outcomes:
- `Install` concludes the round and replaces the government.
- Phase 2 past its window leads to `PluralityBallot`, which installs or concludes and never tables.
- Otherwise `AskNext`, which sets `Consulting` or `PlayerAsked` with a fresh `AskedOn` and never tables. The next proposal is 7 days away.

No call returns to `TableAndVote`, and the guard `country.Government?.Round == round` (3105) blocks a vote on an orphaned round. In the day tick, the switch runs one case and returns after `TableAndVote` (3139-3140), so a same-day ballot is never taken twice. A player's `SubmitFormation` between ticks votes once, and the next tick sees either a concluded round or a fresh ask.

### Latent (second pass)

- **Final edit not fully rechecked.** SimulationManager.cs was last written at 23:48:57, after gf705f started (23:46:53). Only three of the eight checks ran on the staged file (gf705g: German, Speaker's round, sweep). The other five last ran against the edit before, the both-chambers `<=`, now narrowed. sim705 was still running when read.
- **Older saves whose German election formed nothing.** They still reopen on load (`FormedOn < held`) and run Art. 63 against the record that stood. It is arguably the right new rule, but it is a load-time change with no bump, and it is unstated.
- **The Treasury refusal.** "THE PRIME MINISTER'S PARTY HOLDS THE TREASURY" still shows on the German sheet (3429, 3445), pending Elias's Treasury ruling. GO-BT § 4 is likewise Elias's.
- **Party seats, not the Fraktion's.** `SpeakerOrder`'s candidacy sort (2940) and `PmOf`'s no-candidacy fallback (2968-2970) still rank by party seats, not group seats. They set the phase-2 ask order and the plurality tie-break.
- **(e)'s player path.** The player is now the seated Linke, but it is never offered or asked in phases 2-3, so the player path through the Abs. 4 ballot (new defect B, and defect 2's remaining case) is untested.
- **Carried from the first pass.** The night's Bundestag sentence still never reaches a German player (no night). No real film exists of the German round, sheet, reference row or DECLARED page. `DraftSuccessor`'s loop cost is unchanged.

**Money path (second pass):** no arithmetic is touched and no stream is drawn. `PluralityBallot` still installs through `Install` (arrival window reset, a non-governing player's window closed). Defect 6's second arrival budget on load is closed. New defect A re-opens a government the convening never discharges. New defect B hands the book to a CDU-only cabinet the model says no chamber could elect.

**Verdict (second pass): NOT READY.** Defect 2's remaining case and new defects A and B stand. Defects 1 and 3-9 are closed.

## Third pass

Reviewed: `git diff` (unstaged) on SimulationManager.cs (2548-2560, 2850-2860, 3060-3075, 3240-3345, 3455-3470), SpeakerRound.cs, GameController.ParliamentRows.cs:529 and GermanFormationDiagnostic.cs, plus gf705k.log (the one check run on this state, lines 1100-1130).

- **Defect 2 (the player's unasked candidacy): CLOSED.**
  - `PlayerStood` is set only in `SubmitFormation` (SimulationManager.cs:3465), and the Abs. 4 candidates skip the player's party unless it stood (3255).
  - (e2) proves it: the CDU passed twice and stood no candidate (gf705k.log:1126).
  - Latent: a player who stood and later PASSed still stands, because `PassFormation` does not clear the flag.
- **A (the polling-day constructive vote): CLOSED.**
  - `ElectionAwaitsRound` (3067-3073) is true from the polling day until the round opens: `held > FormedOn` and `held > ProcedureResumedAfter`.
  - It is refused at the player's vote (2555), the AI's (2856) and the row (ParliamentRows.cs:529). All three sit in Bundestag branches, so the Riksdag is untouched.
  - The pre-§705 record (`FormedOn == held`) still reads false, so the old-save case stands.
  - (e2) proves the refusal on polling day (gf705k.log:1106).
- **B (the split Union): CLOSED for the split, NOT CLOSED for unasked seating.**
  - **What is fixed:**
    - A draft that splits the group is dropped (3314).
    - The player's declines hold in its vote: a declined partner gets no group vote (3273), and a declined candidate is refused (3286).
    - A declined winner whose group partner is the player draws no government (3316-3323).
  - **What remains: the fallback still seats the player unasked.** When the player has not declined the winner, the fallback adds the winner's group partner, the player, to the cabinet (3324-3328). Take a CSU player and a CDU winner:
    - The temporary "CSU>CDU" makes the draft CDU alone, which is split and dropped.
    - `Declined(CDU)` is false, so the fallback builds CDU+CSU. `Answer` accepts for the player (FormationProposal.cs:160-164), and `Install` seats it.
    - This is reachable whenever the CDU was never asked in phases 1-2 (fourth in the order, where only one phase-1 and two phase-2 asks fit), so no offer was ever made. It also happens when the player accepted a different CDU proposal earlier.
    - The comment at 3302-3303 ("never drafted in unasked") does not hold.

    **Fix:** where the winner's group partner is the player and the player has not accepted this winner in the round, put the group's cabinet to the player as an offer. Or take the no-government branch.

New defects:

C. **The no-government branch strands Germany under a caretaker for the rest of the run** (3316-3323).
   - **What is left.** The round concludes with the outgoing government discharged since the convening, and nothing can replace it:
     - The player's constructive vote refuses a caretaker (2548).
     - The AI's returns on one (2855).
     - A round reopens only after a new election, and Germany has no polling day after the 2025 snap.
   - **Silent.** The Abs. 4 ballot is never recorded as a division, because the return comes before the `Append` (3352). The only record is the log of a concluded round that nothing draws.
   - **Reachable on (e)'s own chamber.** Take a CSU player who declined the CDU's offer. The CDU's 164 still beats AfD 152, Grüne+Linke 149 and SPD 120, so the branch fires.
   - **Art. 63 Abs. 4.** The branch also ignores that an elected candidate "muß" be appointed when the vote is a majority of the members.

   **Fix:** reopen the ask, or offer the group's cabinet to the player. At least record the ballot and state the stranding.
D. **The Fraktion's vote splits in the Abs. 4 ballot when its larger member stands no candidate** (3269-3290; opened by the defect-2 fix).
   - **What happens.** The smaller member votes for its group's candidate only if that candidate stands. Otherwise it votes on its own compatibility.
   - **Against the model's own rule.** "Outside a cabinet the two vote as one - the larger's side" (`Evaluate`'s group vote, `JointAlike`).
   - **Proved by (e2).** The player's CDU passed and abstains, while the CSU's 44 go to Scholz: "SPD 164" = 120 + 44 (gf705k.log:1124-1126), and those 44 elect him. Under the group rule, the CSU abstains with the CDU, and the ballot's leader is the AfD's Weidel (152 against Grüne+Linke 149 and SPD 120). (e2) pins the split result.
   - **Fix:** a seated smaller member takes its larger member's choice (vote or abstention). Then re-pin (e2).

**Sweden:** unchanged.
- `ElectionAwaitsRound` is reached only from Bundestag branches, and `PluralityBallot` is Bundestag-only.
- `PlayerStood` is written by the Riksdag's `SubmitFormation` too, but nothing on the Riksdag path reads it. It is an additive, default-false field in the save, so no bump is needed.

**Checks:** only `GermanFormationDiagnostic` ran on this state (gf705k, clean). SpeakerRoundDiagnostic and the other seven were not re-run after these edits.

**Money path:** no arithmetic is touched and no stream is drawn.
- C leaves the book with a discharged government that no path replaces.
- B's remaining case installs a cabinet that holds the player's party unasked.

**Verdict (third pass): NOT READY.** B's unasked-seating case and new defects C and D stand. Defect 2 and A are closed.

## Fourth pass

Reviewed:
- **Code:** SimulationManager.cs 3228-3375 in the working tree, i.e. `PluralityBallot` as rewritten under "the Fraktion is one".
- **Logs:**
  - gf705m.log, lines 1487-1512 and 4585. The eight named checks started at 00:21:04, after the last edits (SimulationManager.cs 00:20:45, GermanFormationDiagnostic.cs 00:21:03).
  - sim705b.log, lines 423 and 20555. It started at 00:08:21, before the final ballot edit. The ballot runs only in phase 3 of a Bundestag round, and no simulation check reaches that, so the pass carries over.
- **UI:** grep of the UI for what draws a round's log or a government's breaks.

- **D (the group splits its vote): CLOSED.**
  - The tally runs the larger members first (SimulationManager.cs:3270-3272). A seated smaller member then takes its partner's recorded choice, whether a vote or an abstention (3281-3288, where `chose[key]` stores null for an abstention).
  - (e2) now elects Weidel with 152 once the CDU and the CSU both abstain (gf705m.log:1505-1507). That is the "sincere votes" consequence already put to Elias.
- **C (a stranded caretaker, an unrecorded ballot): CLOSED.**
  - The ballot is recorded as a division before any government is drawn (3331-3332).
  - The declining-CSU branch is gone.
  - The two remaining returns are unreachable while the four 2025 candidacies stand: no candidate (3258-3263), and no investiture (3363-3368, where the fallback proposal is always well formed).
  - Latent: if the second return ever fired, the division would already say "appointed" for a government that never formed.
- **B (the player seated unasked): CLOSED in behaviour, as a stated [AUTHORED-DRAFT] premise.**
  - The group's rule now seats the player's party as the elected candidate's partner (3350-3358), and the draft drops a split cabinet (3348).
  - (e3) proves it: CDU+CSU led by the CDU, Merz with 208, not a caretaker (gf705m.log:1508).

New defect:

E. **The premise that seats the player's party never reaches the player.**
   - **Where it is written.** Only in the round's log (3357), plus the code comment at 3334-3337.
   - **Why that is invisible.** `Install` (called at 3375) concludes the round (3409), and the round stays on the replaced record. `RoundOf` then returns null, so the round's slip, the only place that draws a round's log (ParliamentRows.cs:221), is never drawn. The government's breaks are drawn nowhere.
   - **What the player does see.** The Abs. 4 division gives the player's CSU the side reason "votes with its parliamentary group, as one (CDU's side)", with no "(the game's premise)". That reason is also the one place where the player's own decline is silently overridden. Nothing tells the player why its party is suddenly a junior partner in a cabinet it declined.
   - **The bar.** This is the same test the first pass's defect 9 set for the ballot's other two premises.

   **Fix, one line each:**
   - Append "(the game's premise: a group votes and governs as one)" to the smaller member's side reason at 3284.
   - Where 3357 fires, add the same sentence to the division title or to a break on the formed government.

**Sweden:** unchanged. `PluralityBallot` is reached only from Bundestag rounds (3140, 3191), and nothing else changed in this pass.

**Money path:** no arithmetic is touched and no stream is drawn. The Abs. 4 ballot always installs a government now, through `Install` (arrival window reset; a non-governing player's window closed).

**Verdict (fourth pass): NOT READY.** One item stands: E, a two-string visibility fix. B, C and D are closed.

## Applied after the review (the author's note - not the reviewer's words)

- **Fourth pass, E** - the group rule is on the division and on the government: the CSU's side reads "votes with its parliamentary group, as one (CDU's side) - the game's premise: a group votes and governs as one", and where it seats the player's party the new government carries the break "... sits in <candidate>'s cabinet as its parliamentary group's partner - a group votes and governs as one (the game's premise)". Check (e3) asserts both. `gf705n`: the eight named checks clean on the final `SimulationManager.cs`.
- **The rulings the passes named for Elias** - the Treasury lock and the GO-BT § 4 nomination threshold were ruled by Elias on 2026-10-01 and are §706, a commit of their own; the sincere-votes premise stays his to rule (check (e2): with the CDU standing no candidate the AfD's Weidel is elected with 152).
