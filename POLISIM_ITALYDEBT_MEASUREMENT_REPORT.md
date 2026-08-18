# Italy Debt Crisis — measured, and it SURVIVES (2026-08-18)

**Scenario content pass, behind Step 3's shipped format, third of the named slate — and the
first of the three to survive.** Wage Boom Management and The Disinflation both DROPPED on
`UnemploymentReversionSpeed` (0.7/turn), which forecloses any premise resting on moving the
unemployment gap away from NAIRU and holding it there, from either direction. Italy's difficulty
source is different in kind: the debt identity (post-erosion/maturity), the best-validated,
most-measured mechanism in the model. Per the standing practice, that mechanism is measured here
before a single objective number is authored — and a third drop remains an acceptable outcome if
the measurement had said otherwise. It didn't.

**Uses the CORRECT input field**, per the methodology error this pass found while building its own
diagnostic and corrected in `POLISIM_WAGEBOOM_MEASUREMENT_REPORT.md`'s addendum:
`PolicyDecision.SpendingLineChanges` (a dictionary), never the eight legacy float fields
(`InfrastructureSpendingChange` etc.), which are dead as inputs for every seeded country and were
what Wage Boom's own §8 silently no-op'd on.

## 1. The levers, measured first — and they bite hard

Italy seeded to 165% debt-to-GDP (up from its 138% base), seed 777, six configurations run 30
turns real day-loop, decisions applied once at turn 1 and left to compound:

| config | t1 | t5 | t10 | t20 | t30 |
|---|---|---|---|---|---|
| no policy | 127.94% | 106.14% | 106.19% | 107.99% | 109.60% |
| cut 10% (3 lines) | 127.94% | 105.01% | 104.51% | 106.46% | 108.02% |
| cut 20% | 127.94% | 103.85% | 98.86% | 92.60% | 89.77% |
| cut 30% | 127.94% | 102.32% | 92.46% | 73.46% | 52.63% |
| VAT +3pp (25%) | 127.94% | 104.93% | 105.02% | 106.75% | 108.32% |
| VAT +6pp (28%) | 127.94% | 103.68% | 100.29% | 99.04% | 104.23% |
| cut20 + VAT+3pp | 127.94% | 102.13% | 93.20% | 76.96% | 60.07% |

**All seven configurations are identical through t1** (`DebtToGdp=127.94%`, `Debt=2926.3`,
`GDP=2287.3`, `U=8.617` — to three decimals, across every run) — the FRF's own aggressive
first-turn reaction to a 165% start dominates regardless of policy, exactly the same clean
attribution signature the last three passes established. Every config diverges smoothly and
monotonically from t2 onward, with no jump discontinuities landing at the same turn across
configs at different magnitudes — no random `EventSystem` shock to separate out; the divergence
is the policy.

**Spending cuts compound; VAT hikes plateau.** Relief-per-unit at t30, against the no-policy
baseline (109.60%):

- Spending cuts: −0.16 pp debt-to-GDP per pp of cut at 10%, **accelerating to −0.99 pp/pp at
  20% and −1.90 pp/pp at 30%** — a debt-stock feedback (lower principal → lower interest cost →
  further reduction), the erosion/maturity identity's own compounding working *for* the player
  once the stock starts falling.
- VAT hikes: a much flatter −0.43 pp/pp at +3, −0.90 pp/pp at +6 — real, but roughly half the
  per-point bite of spending cuts, and non-accelerating (VAT raises revenue linearly; it does not
  touch the compounding stock the way a cut does).

**The −20% spending-only package alone clears a 95%-or-below terminal target by t20** (92.60%,
2.40 points of margin) with no VAT hike needed at all — this is the number the authored
`debt_down` objective's target and `EndTurn` are built against, cross-validated independently by
`ItalyDebtCrisisSliceDiagnostic`'s isolated run (identical 92.60% at t20).

## 2. Approval survival, measured early — the question that killed Disinflation independently

Per the standing practice (elevated inflation alone crashed Disinflation's approval past the
losing threshold in 3–7 turns, before any lever could plausibly work — an independent kill-shot
this pass had to check for before authoring anything):

| config | t1 approval | worst point (turn) | t30 approval |
|---|---|---|---|
| no policy | 49.02 | 41.92 (t30) | 41.92 |
| cut 20% | 48.68 | 42.71 (t30) | 42.71 |
| cut 30% | 48.51 | 43.11 (t30) | 43.11 |
| VAT +3pp | 44.52 | 40.91 (t30) | 40.91 |
| **VAT +6pp** | 40.02 | **39.48 (t3)** | 39.89 |
| cut20 + VAT+3pp | 44.18 | 41.70 (t30) | 41.70 |

**Debt does not independently crash approval the way elevated inflation did.** Confirmed by
reading the mechanism directly: `ApprovalAttribution`'s misery-index terms are
`MiseryUnemployment`, `MiseryInflation`, `MiseryCrime`, `MiseryCorruption` — **no debt term of any
kind**. A 165% debt-to-GDP start only reaches the player's approval through the levers used to
fix it, not through the debt figure itself.

**Spending cuts are nearly free**: a consistent **≈−0.017 approval points per point of spending
cut** at t1 (cut10 −0.17, cut20 −0.34, cut30 −0.51 against the no-policy baseline — linear, and
tiny). **VAT hikes cost roughly 90× more per nominal point**: **≈−1.50 approval points per point
of VAT** at t1 (+3pp → −4.50, +6pp → −8.99), a real and much steeper parliamentary cost, the same
order of magnitude finding as Disinflation's rate-hike-vs-approval tension — except here the
player has a strictly better tool available (cuts), which the Disinflation/Wage Boom pair never
did.

**The +6pp VAT-only line is the one tested configuration that genuinely risks the `keep_the_room`
objective** (approval ≥40 for 10 consecutive turns): it dips to 39.48–39.66 across roughly t2–t5
before recovering above 40 by t15, breaking any streak attempt in that window. This is a real,
quantified, playstyle-dependent tension — a VAT-heavy line is measurably harder to hold the room
on than a cuts-heavy line, which is the intended shape of "every tool costs something with
voters," not an oversight to smooth away. **No tested configuration comes remotely close to the
30-point `no_collapse` floor** — the worst point measured, 39.48, is still 9.5 points clear of it.

## 3. Country and mechanism ruling — fiscal-only by construction, framed as the feature

Applying the two constraints already on record from the prior two passes:

- **USA remains disqualified** (Q5/Wage Boom finding): the Fed Chair's Taylor-rule bias is
  dominated by a −14.5% structural output-gap distortion that pins the suggested rate near 0%
  regardless of the debt path, so a USA-seeded version would hand the player a monetary lever
  that cannot move.
- **Eurozone monetary agency is effectively nil** (Disinflation's own measurement): the shared
  rate auto-climbs via `EurozoneRateSystem`'s GDP-weighted Taylor blend with zero player input,
  and the player's own capped push (±0.75) is invisible against it. Italy is a Eurozone member —
  this is not a gap in this scenario's design, it is **the scenario's actual premise**: Rome has
  no central bank of its own to lean on, and every tool that matters is fiscal. The measurement in
  §1 confirms the fiscal tools that remain are more than sufficient on their own (the spending-cut
  channel alone clears the terminal target with margin to spare) — so the constraint is real,
  correctly reflects the mechanism as tuned, and is framed in the authored premise as the
  scenario's identity rather than apologized for.

## 4. Verdict: SURVIVES — authored and shipped

Unlike Wage Boom and The Disinflation, no lever here produces "every configuration gives the
identical result" — the seven configurations spread from 52.63% to 109.60% by t30, a 57-point
range, driven entirely by the player's own choice of instrument and magnitude. Unlike The
Disinflation, no tested configuration risks an early, un-actionable approval collapse — the worst
case dips 0.5 points under the sustained-objective threshold for a few turns and recovers with
room to spare before `no_collapse`'s 30-point floor. **The debt identity is a genuine, biting,
approval-survivable, fiscal-only challenge.** Authored as `ItalyDebtCrisis()` in
`ScenarioLibrary.cs`: Terminal (`debt_down` ≤95%), Sustained (`keep_the_room` ≥40 for 10 turns —
the format's `Sustained` form's first real exercise, per Step 3's own standing note that the first
scenario to use it is also its first test), NeverBreach (`no_collapse` ≥30). Full build record,
the bar, and the capture-driver finding are in `CLAUDE.md`'s "Italy Debt Crisis ships" entry.
