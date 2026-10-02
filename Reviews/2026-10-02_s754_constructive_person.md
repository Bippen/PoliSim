# Review — s754 the constructive vote is a vote on the successor, counted by person (Elias's ruling A4) (2026-10-02)

Reviewed: the staged diff against HEAD d672912 - `Assets/Scripts/Elections/ConfidenceProcedure.cs`, `Assets/Scripts/Simulation/SimulationManager.cs` (a money path by name), `Assets/Scripts/UI/GameController.ParliamentRows.cs`, `Assets/Editor/ConstructiveVoteDiagnostic.cs`. One independent read-only reader; its pass verbatim, then what the author did about it.

## The pass (verbatim)

Reviewed: the staged diff (378 lines, HEAD d672912). ConfidenceProcedure.cs:42-226. SimulationManager.cs:2584-2627, 2779-2939, 3038-3093, 3436-3481, 3574-3641, 3707-3722. FormationProposal.cs:54-237. GovernmentFormation.cs:363-379, 433-458. CoalitionFormation.cs:62-64, 134-155, 679-688. GameController.ParliamentRows.cs:440-484. ConstructiveVoteDiagnostic.cs:1-217. GermanFormationDiagnostic.cs:270-300. TrajectoryBaselineDump.cs:96-130. The n754 and n754b logs. COMPLETED.md §713.

Verdict: NOT READY

## Defects

1. **ConfidenceProcedure.cs:98-110 with 162/169/171-184: a sitting cabinet partner's loyalty test is measured against a government that will not form.** `stays` compares the partner's posts in the sitting cabinet with its posts in the *drafted* cabinet. That happens even when `PartnersAccept` is false, and then the government actually installed is GroupAlone, which has no place for the partner.
   - **The path:** a drafted sitting partner X would leave for the draft. A non-sitting cabinet partner Y refuses (for example on payoff). DraftSuccessor's redraft loop (SimulationManager.cs:2845) only removes refusers that sit in the sitting cabinet, so Y stays in the draft and it does not hold.
   - X then matches none of the rules: `bound` needs `PartnersAccept`, X is not in `stays`, and `sittingCabinet && !draftedIn` is false because X was drafted. So X falls through to the sincere comparison.
   - **What goes wrong:** if X is nearer the mover, X elects him. ConstructiveVoteOf then installs GroupAlone without X, so X has voted out its own chancellor for no place at all. That breaks the code's own stated rule at :125-126: "a sitting cabinet partner the successor does not seat better ... do not elect him".
   - **Fix:** when `!PartnersAccept`, count a drafted sitting partner as staying.
   - No check runs this path. It also makes the loop in (3) possible.
2. **Comment-only: stale text** (listed in (6)).

## The questions asked

**(1)** Yes, the count follows the ruling.
- `Carried => For >= Needed`. Nothing counts the votes against, and `PartnersAccept` no longer decides the election.
- The chancellor's party, and the partners that stay, "do not elect". Those are votes on the person, not a separate vote against the incumbent.
- Comparing each party with the sitting chancellor is defensible as a sincere binary choice: if the successor is not elected, the incumbent stays, and nothing is cast for the incumbent.
- It is still a premise the ruling does not settle. In Art. 63's phase 1, with one candidate, every party that does not refuse him elects him; that is how (e5) had the SPD vote for Merz, "preferring Merz to no one". The Art. 67 count instead elects him only where a party is nearer him than the chancellor. Elias should rule on which reading he wants.

**(2)**
- **GroupAlone extraction:** behaviour is identical. `seatedByGroup` was already false on entry, `player` holds the same value, and the statement order is the same.
- **NullReferenceException:** no path reaches one.
  - GroupAlone for a known mover always builds a well-formed proposal, so its `Investiture` is never null.
  - An unknown mover gets For = 0: `Refuses` returns true for it, and nobody is bound because the investiture is null.
- **Refusing parties:** refusing supporters are filtered out at 2886, and refusing cabinet partners send the result to GroupAlone. Two exceptions:
  - The group partner is seated even if it refused the draft (the crafted case's CSU). That matches the group premise and Art. 63.
  - GroupAlone can seat the player's party, as the AI mover's group partner, without asking it. That loosens defect 1's rule; check (d) uses SSW and does not cover it.

**(3)** Measuring the payoff on the government actually installed is right. But the test it feeds is empty:
- `now` is 0 by construction (`PayoffIn` returns 0 for a party outside the cabinet), and any cabinet the mover sits in gives more than 0.01. GroupAlone gives the most.
- So the AI moves whenever the person count carries.

Votes the AI can now move that it could not before:
- A carried vote whose draft does not hold now installs a GroupAlone minority government (before, `Carried` was false).
- The count itself changed: the sitting government's supporters, refusing drafted parties and parties outside both governments now vote by compatibility, not by the investiture.

Loop risk:
- An AI install sets `FormedOn` to today, so the next day is a weighing day. InstallSuccessor records no cooldown and no refusal.
- A straight A→C→A reversal cannot carry both ways, because the sincere voter sets are complementary — except through Defect 1. X elects C sincerely, then is bound to A's holding draft, then C's draft repeats identically: A and C swap every day.
- I built this from the code; it has not been observed, and no check runs more than one weighing day.

**(4)** Nothing breaks.
- Every other `.Carried` reader (AiMotionReach 109/255/264, ConfidenceDiagnostic 47/271/359, ChamberVerdictCacheCheck 234) reads Riksdag `Vote()` results. Those never set `PartnersAccept`, which defaults to true, so their results are unchanged.
- The other `.Carried`/`Refusal` hits are different types (PensionAgeStatute, PreCampaignRun).
- `PartnersAccept`, `SuccessorCabinet`, `SuccessorSupport` and `Refusers` are read only in SimulationManager (2845, 2864, 2886), the UI (473-475) and the diagnostic.
- MotionVote is not saved; divisions store `Title`/`Sides`, and the title format is unchanged.

**(5)** The Riksdag path is untouched.
- `Vote()` and both Riksdag branches are unchanged, and `Carried` gives the same result for Riksdag votes.
- The trajectory dump calls only `AdvanceDay`/`AdvanceTurn`. `AdvanceConfidenceDay` is reachable only through `AdvanceCountryDayTick` (GameController and UiScreenshotDriver), and `TryAiMotion` returns when no player country is set.
- `ElectedGovernment` runs only in Bundestag rounds, and its behaviour is unchanged anyway. The dump does not reach the changed code.

**(6)** Stale comments:
- ConfidenceProcedure.cs:51 still says "whose partners must accept it".
- :93 says "a partner that refuses the successor does not vote for it" — the crafted case's CSU refuses and still elects.
- :116 says "a refusing supporter only withholds its votes".
- SimulationManager.cs:2815-2818: the section banner does not mention GroupAlone.
- :2832 says the player's party "votes as its lines have it"; it now votes on lines and compatibility.
- The diagnostic's message at :128 says "by the sincere comparison", but it counts refusal reasons.
- Nothing in docs/ is stale.

## Notes for the record

- **No check runs the new GroupAlone branch.** The crafted case calls `ConfidenceProcedure.ConstructiveVote` directly, so neither ConstructiveVoteOf's GroupAlone branch nor InstallSuccessor's `SeatedByGroup` line is exercised. In cases (c) and (d) the drafts hold (the log shows no refusal). An end-to-end case through `MoveNoConfidence` is needed.
- **What n754 and n754b actually show:**
  - n754 failed ConstructiveVoteDiagnostic: "0 by the sincere comparison".
  - The fix added "(the game's premise: sincere votes)" to the refusal reasons. n754b's "2" are the AfD (refuses both) and the Linke (refuses the successor); no comparison happened for either.
  - The vote7 case does test the comparison properly.
  - The `personReasons` check is close to tautological: every reason the new code produces matches one of its substrings.
  - n754b ran only this diagnostic.
- **The player's path never checks the mover's seats** (2600-2612); the AI path skips seatless movers. With the sincere count, a seatless party (for example a created one) can be elected if the parties outside both governments are nearer it than the chancellor. Its "alone" draft holds trivially. Before this change the investiture decided that. Not confirmed in play.
- **Wrong reason for a mover with no position (SSW).** It gets "nearer the sitting chancellor", because compatibility 0 is the floor. The vote is right but the reason is wrong; Art. 63's `Tally` excludes candidates with no position explicitly.
- **Latent fallback at 2867.** If GroupAlone's investiture were null, ConstructiveVoteOf keeps the failed draft with `Carried` still true. It cannot happen today, but it should refuse the install instead.
- **Slip when not elected and the draft does not hold.** It names the drafted cabinet, not the GroupAlone government the candidate would form if he reached the majority.
- **Cost.** DraftSuccessor now prepares a chamber on each pass, though only `Refusers` is read. GroupAlone adds a full `Formateur.Answer` when the vote carries and the draft does not hold.
- **Already true before this change: a player CSU with an AI CDU mover.** The decline keeps the CSU out of the draft, and `PartnersAccept` ignores `SplitsJointGroup`. A holding draft therefore installs a CDU government without the CSU. What is new is the vote: the CSU now follows the CDU and votes for that government.

## What was done about the pass (the author's)

1. **Defect 1, fixed**: a sitting cabinet partner elects only where it is BOUND to a successor's government that holds; drafted into one that does not, it does not elect him - *"sits in the sitting cabinet, and the successor's drafted government does not hold - he would govern without it; it does not elect him"* (every non-bound sitting partner now takes the one branch, its reason saying which case). The path is now run: `ConstructiveVoteDiagnostic` plants a large FDP (200) beside a small Union and a diluted sitting cabinet, so the FDP would LEAVE for the draft (CDU+CSU+FDP+Grüne, the Greens refusing) - it does not elect, with that reason (`n754f`). The loop the pass built from the defect closes with it.
2. **The stale text**, fixed: `MotionVote.Constructive`'s summary, the refusers' comment, the cabinet-holds comment (`ConfidenceProcedure.cs`); the section banner (GroupAlone named) and the player's vote in `DraftSuccessor`'s comment (`SimulationManager.cs`); the check's wording ("by the sincere rule - a refusal, or the comparison").
3. **Notes acted on**: a successor whose party holds no surveyed position draws no sincere vote (Art. 63's rule, §715 - its own reason now, not "nearer the sitting chancellor"); the player's path refuses a seatless mover as the AI's does (*YOUR PARTY HOLDS NO SEAT*); where the draft does not hold the government he would form is computed whether or not the count reaches the majority (the slip names it), and a successor with no drawable government is never installed (latent; logged); the GroupAlone install and a player's group seat are driven through the private steps (`n754c`: *CDU+CSU led by CDU takes office ... the CSU player seated as the group's partner - recorded on the government*) - no planted chamber found makes `MoveNoConfidence`'s own draft fail to hold (the redraft drops a staying partner), stated.
4. **Notes stated, not changed**: the comparison with the sitting chancellor is a premise the ruling does not settle - Art. 63's one-candidate phase elects where no line refuses; owed to Elias (the reading of "a vote on the successor"); the AI's payoff test is empty by construction (`now` is 0 for a mover outside the cabinet - §698's, unchanged); GroupAlone may seat the player's party as an AI mover's group partner unasked (Art. 63's premise, recorded on the government); the player CSU / AI CDU split (pre-existing); the cost (a chamber per redraft pass, an Answer where the draft does not hold - the AI weighs on Mondays only).

