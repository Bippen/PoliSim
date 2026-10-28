# D-NIGHT review 3: the decline producer and the Lows (wave/dnight, 8bed2309..110c3b82)

This is an independent delta review of 8086c3fc (N1, the N3 data, N8) and 110c3b82 (N3-N7), checked against review 2 (N1 Medium, N3-N8 Low). I did not run Unity. `Tools/cgate/gate.sh` on the worktree printed `GATE OK`. I read the full diff, plus the producer side it relies on: `Formateur.Answer` (FormationProposal.cs:110-245), `CoalitionFormation.Evaluate` (where a cabinet bit is always `InCabinet`, CoalitionFormation.cs:559), `DraftProposal` (SimulationManager.cs:4821), the projection's Riksdag loop (FormationProjection.cs:95-134), and the screen's draw helpers (`Gap`, `Rule`, `HRow`, the bridge, `LaneBlocks`, `NeedsKey`, `GovernsWhy`).

**Files reviewed:** Assets/Editor/FormationProjectionDiagnostic.cs, Assets/Scripts/Elections/GovernmentFormation.cs, Assets/Scripts/Simulation/SimulationManager.FormationProjection.cs, Assets/Scripts/Testing/UiScreenshotDriver.cs, Assets/Scripts/UI/ElectionNightScreen.Governs.cs (the five files the two commits touched). I also read, unchanged: Assets/Scripts/Elections/FormationProposal.cs, Assets/Scripts/Elections/CoalitionFormation.cs (Evaluate), Assets/Scripts/Simulation/SimulationManager.cs (DraftProposal), Assets/Scripts/UI/CanvasRows.cs (HRow), Assets/Scripts/UI/ElectionNightScreen.cs (Build, Masthead).

## 1. N1: the decline producer. ANSWERED

`VoteOf` (FormationProjection.cs:198-229) now sets `Fell = verdict.Passes ? null : verdict.Reason`. After the sides are built, it walks `verdict.Answers` and calls `NightVote.Decline(answer.Party, answer.Reason)` for every `InCabinet && !Accepts` answer.

- **Read-only of the verdict.** It only reads `verdict.Answers`, `verdict.Passes` and `verdict.Reason`. No answer is re-run and nothing in the verdict is written. `Decline` mutates only the new `NightVote`. `VoteOf` is called only on the projection's two Riksdag paths (the expected verdict at :118, `lastRefused` at :132). `NightVote` is read only by the board, the screenshot driver and the diagnostic, so neither the formation sweep nor any baseline reads these fields. I find no path by which the sweep's output could change. I have not shown byte-identity by a run.
- **The decliner reaches `Decline`.** `Answer` evaluates the investiture on the whole proposed cabinet mask, decliners included (FormationProposal.cs:241), and `Evaluate` marks every cabinet bit `InCabinet` (CoalitionFormation.cs:559). So a declining partner is on the for side when `Decline` runs, and the switch fires.
- **The sums.** `Decline` moves `s.Seats` from `For` to `Abstain` and changes `Sides[i]` together with them, so `For + Abstain + Against` is unchanged and still equals `Members`. `VoteFault`'s side-by-side sum also still holds.
- **Against and OpposedSeats.** `Against` is untouched. On the expected path `Passes` implies `AllAccept`, so no decline exists there, and the against clause (applied only where a government is expected) is unaffected.
- **Carried.** It stays `verdict.Passes`, which is false whenever any answer refuses.
- **The decliner's side.** It is drawn abstaining, not left as it stands. Review 2 allowed either choice provided the counts move together, and they do. The decliner's reason reads "declines its seat in the cabinet - <its own answer>".
- **The screen.** The single-lane count row (Governs.cs:536-541) draws `✓/✗` from `v.Carried`, then "<ShortList(Decliners)> declined to join ·", then the figure (`Against` under the negative rule) and "against". The slip and the † view add "The proposal falls before the vote: X declined to join (Fell)". Every element comes from `Decliners`, `Fell`, `Carried` and the counts. The old self-contradicting "140 against · it takes 175 against to refuse it" is no longer drawn when a decline exists.
- **`DeclineFault` (Diagnostic:117).** It refuses four cases:
  - a decliner with no side;
  - a decliner still `InCabinet` or `Supports`;
  - a decline with an empty `Fell`;
  - a decline on a carried vote.

  It runs on every non-Italian projected vote. The planted decline (Diagnostic:400-411) checks that `Decline` leaves the partner abstaining, lowers For by exactly its seats, keeps Against, keeps `VoteFault` clean, and passes `DeclineFault`. The unmarked plant (a decliner added with its side still for) is refused.

  Only the "still counted for" clause is isolated by a plant, though. The "no reason", "carried" and "holds no side" clauses never bite alone (N10).

One gap remains in the same shape as M2: a refusing *supporter* is not a decliner (N9).

## 2. The Lows

- **N3, answered.**
  - `FirstBallot`'s fall-through marks `ballot.ElectedWithoutGovernment = ballot.Carried` (FormationProjection.cs:172). That is reached only when the candidate is elected and either `ElectedGovernment` gave nothing or the verdict has no investiture, which is exactly the case.
  - `NoneWords` becomes "ELECTED - NO GOVERNMENT DRAWN ON ITS PARTY". The slip and † line now read "NO GOVERNMENT EXPECTED TO FORM · ELECTED - NO GOVERNMENT DRAWN ON ITS PARTY". That is repetitive but true, and the not-elected case keeps "THE BALLOTS GO ON", which is right.
  - `NoneLines` keys on the flag. Both branches return two lines, so the plan's `ReasonRows` is unchanged.
  - `RoundFault`'s message is now keyed on which side is missing (Diagnostic:75).
- **N8, answered.** `NightFault` and the round check exempt a carried Bundestag vote with no government only when `ElectedWithoutGovernment` is set (Diagnostic:96, :369). The new plant pair proves the exemption both ways: unmarked is refused and marked passes. Neither plant can fail for an unrelated reason, since `VoteFault` does not test `Carried`. The reverse misuse (the flag set on a vote with a government, or outside the Bundestag) is ignored rather than refused. See N12.
- **N4, answered** for the slip and †, where "Not drawn in its block (Camera): ..." is now filtered by `b.Chamber == v.Chamber`. Answered for the key row too: entries are prefixed with the chamber and deduped by chamber + name + key. The prefix lengthens an unwrapped, unmeasured row, though (N11).
- **N5, answered, with the slack assessed.**
  - After the build, `ForceRebuildLayoutImmediate` runs and the column's preferred height is compared with `plan.Final * BoardPx`. Any difference over 1.5 units logs `CANVAS:` as an error. The driver's `OnRunLog` counts every error, so a film fails on it.
  - Every column height comes from a `LayoutElement`, so it does not depend on width. `HRow`, `Gap` and `Rule` all use whole multiples of 1.5 units, and every `HRow` literal is even, so `Px` is exact.
  - The bridge is the only legitimate drift. The plan rounds its natural height to a whole board px and the draw takes `min(plan * 1.5, Round(units))`, so the drift is at most about 1.25 units. A 1.5 slack therefore cannot false-fire.
  - The slack can hide a desync of exactly one board px (1.5 units), depending on that night's bridge rounding. Any desync of 2 px or more (3 units, minus at most 1.25 of drift) always fires. The column has roughly 11 units of headroom against Design's measured 785, so a hidden 1 px desync cannot overflow.
  - One trap is left for later: a future odd `HRow` height (`Px(25)` = 38 against a plan of 37.5) adds 0.5 per row, silently, until it adds up past the slack.
- **N6, partly answered.** The tick and the duplicated Art. 63 clause are gone (Art. 63 already has its own line at Governs.cs:198). "a block per Fraktion" is still there, although review 2 asked for it to be dropped, and the † view draws no blocks. The `string.Format` call has no arguments. That is harmless, but it would throw if a brace were ever added.
- **N7, answered.** The masthead reads "RETURNING OFFICER · ITALY · STAGED" in every Italian frame. `countryName` is used only by the masthead, and its "GERMANY" comparison is unaffected. The ledger is dropped, so Why draws "Why · no campaign ledger" / "no live campaign was run to this election", and the plan's no-ledger case (12+26+4+24) matches that draw.

## 3. Regressions and traps

- **Gate and line endings.** `GATE OK`. All five files are CRLF throughout (446/446, 611/611, 520/520, 7931/7931, 1459/1459).
- **Added lines.** None has `FontStyle.Bold` (the §648 guard), and none has a `{0}'s` or `"'s` possessive (MetaTextCheck). Every new number goes through `CultureInfo.InvariantCulture` or `F()` (NumberLocaleCheck): `{1:F1}`/`{2:F1}` in the CANVAS line are inside an invariant `string.Format`.
- **`Copy` and the plants.** `Copy` carries the new flag, so the plants copy it faithfully.
- **Films.** No new frame exists from either commit. The staged Italian frames, the decline row and the CANVAS parity line are all unfilmed, so the 1280 reading of the decline row ("KD, L declined to join · 140 against" is not width-measured) and of the key row is still unseen.

## New findings

### Low

**N9. A refusing supporter fails a proposal with no reason given** (FormationProjection.cs:224; FormationProposal.cs:205-235). `VoteOf` only declines `InCabinet` answers. A supporter that refuses (the formation would seat it elsewhere, or `SharedSupport` drops it) is already off the support mask, so it is not for. `AllAccept` is false, so `Passes` and `Carried` are false, and `Decliners` is empty.

Failure scenario: KD drafts KD+M with SD's support, and SD refuses because a cabinet elsewhere is worth more to it. The investiture without SD still wins (140 against, 175 needed to refuse). The board draws ✗ beside "140 against · it takes 175 against to refuse it" with no reason: M2's symptom, for a supporter. `DeclineFault` cannot see it, because it returns null when `Decliners` is empty.

`DraftProposal` accepts every tabled demand, so this needs the walk-out or shared-support refusal, which is rarer but reachable. Fix: either name refusing supporters (in `Decliners` without moving any seats, since they hold none for), or add a clause that a vote that is not carried, under a government-less view whose investiture would win, carries decliners.

**N10. Three of `DeclineFault`'s four clauses have no plant of their own** (Diagnostic:117-128, :409). The unmarked plant adds a decliner whose side is still `InCabinet`, so it returns at "still counted for" before the `Fell` or `Carried` clause is reached. A regression that drops the `Fell` assignment in `VoteOf` while `Decline` still works would leave the diagnostic green, and so would one that made a declined vote carried. Add one plant per clause: a moved decliner with `Fell = null`, a moved decliner with `Carried = true`, and a decliner absent from `Sides`.

**N11. The key row's width is unmeasured, and the chamber prefix lengthens it** (Governs.cs:574-584). The key is one `HRow` that does not wrap, inside `GovernsWidth` = 495 units. Each entry is a 27-unit swatch plus text at `Px(14)`, and the gap is 21 units. With Italy's lanes narrowed by the chamber prefix, an abstain block can fold in both chambers. "Camera abstain 12" + "Senato abstain 4" then comes to about 520 units, so the cells compress and their text wraps inside 27 units of height. No plan step or fallback measures this. Fix: measure the row and fall back to the slip (`plan.Key = false`) when it does not fit, or put the chamber on a shared label.

**N12. Small items.**
- `ElectedWithoutGovernment` on a vote with a government, or on a non-Bundestag vote, is silently ignored rather than refused (Diagnostic:96).
- `Fell` is always the generic "an invited party refuses" on a decline. The decliner's own answer appears only on its side line in †, so the slip's parenthesis adds little.
- N6's leftover "a block per Fraktion".

## Prior findings, summary

| finding | status |
|---|---|
| N1 (M2) decline producer | answered |
| N3 elected-without-government words | answered |
| N4 key per chamber | answered (width: N11) |
| N5 plan-vs-built parity | answered (1 px may hide; >= 2 px always fires) |
| N6 Fraktion legend | partly |
| N7 staged Italian frames | answered |
| N8 Bundestag exemption | answered |

Nothing in the delta is above Low. The decliner path is now live, consistent with its own diagnostic, and drawn from its fields. What remains is N9's narrower supporter case, the plant coverage and the unfilmed frames, which the integrator should look at once they are filmed.

**Verdict:** READY
