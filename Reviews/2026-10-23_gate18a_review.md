# Review: GATE-18 piece one (q2 with A-LOOP q24 5b), `wave/gate18a`, 2026-10-23

An independent subagent reviewed the diff from base `3efd9519` to `70da6430` without editing anything. It compiled the tree (GATE OK) and did not run Unity. Its findings are below, each with what the implementer then did.

## H

- **H1. The CL-1 film expected the run-up to refuse the office.** `UiScreenshotDriver` counted a queued office as a failure. **Fixed**: the film now queues the office with the TV buy and the hire, and fails only if any of the three is refused.

## M

- **M1. Poland's count at σ 0 can drift from the shares it was handed.** `PolishSejmAllocation.SwungVotes` floors a list at zero and does not re-solve. **Fixed**: Poland's count returns the shares it was handed plus what the count moved (the count minus the same count at σ 0 with no doors). The floor's residue therefore never becomes a share. Declared.
- **M2. The noise is biased upward for small parties.** `RegionalAggregation.ApplyNoise` clamps each share at 0 and then renormalises. This is W-D1's declared form. **Declared and asked, not changed.** Changing the form of the noise is a model ruling. The candidates are log-ratio noise, or σ scaled by √(s(1−s)).
- **M3. Every Swedish and German campaign measurement moves**, because the AI now values doors. **Owed as Unity runs**: re-declare the campaign harnesses and the audits.

## L

- **L1. Germany's count uses the seated election's register; the night uses the election's own register.** The count is consistent with itself (each Land's valid votes over its register). **Declared.**
- **L2. The `NoiseAtZero` scope is process-wide.** **Declared.** Piece two must open and dispose it around its own idle game.
- **L3. Poland's highest turnout read 1 because of the frame.** **Fixed**: it now reads NaN.
- **L4. An out-of-range region got the wrong refusal text.** **Fixed**: it is refused with "no such region".
- **L5. Comments still said turnout is worth zero.** **Fixed** in `CampaignAi`, `CampaignActions`, `CampaignStrategyHarness` and `CampaignPersuasionDiagnostic`.
- **L6. The print floor had no provenance tag.** **Fixed**: it is now a CONVENTION constant.
- **L7. The new OFFICE line sits in the block of refusal lines.** **Kept**: one line, with the same height as the line it replaces.
- **L8. The caller guard depended on file order.** **Fixed**: it now compares the callers as a set.

## Found correct

- Region and party alignment between the count and the campaign.
- The CSU gets no supporters and no noise outside Bayern.
- No division by zero and no NaN.
- The noise is keyed, so forks and loads draw the same numbers.
- The readout sums to the headline.
- The save step to 56 is reasoned.
- Every backticked reference exists.
- The trajectory sentinel cannot move, because the dump seats no player.
