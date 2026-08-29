# Elections prototype — the running log (worklist `ELECTIONS_PROTOTYPE_WORKLIST.md`)

**Purpose (W-H3):** every `[AUTHORED-DRAFT]` value and every reversible decision, one strikeable
line each, in one place. Items are recorded as they are consumed, in execution order.

---

## Standing rulings recorded into their owning documents

- **Non-circularity is an INVARIANT, not a convention** (2026-08-29) — loyalty always derives from
  the two elections *preceding* the one modelled. Written into `LoyaltyModel`'s own class doc so a
  future session cannot regress it: modelling the next election uses the two most recent results;
  backtesting 2022 uses 2013 and 2018, never 2018 and 2022. W-A3 runs in the backtest direction.
- **LOW CONFIDENCE reaches the gate, not just the log** (2026-08-29) — each country's name-join
  coverage prints beside its MAD in the gate table, and a MAD change in a low-coverage country is
  stated as weaker evidence. A gate passing on the high-coverage countries while a low-coverage one
  stays noisy is reported as **a real pass with a stated scope**, never as four equal countries.
  Recorded in `GateReRun`'s doc and in the verdict text itself.
- **USA and France carry NO loyalty value at all** (2026-08-29) — one election on disk means
  volatility is uncomputable, and an absent value the model refuses to run on is honest where a
  silent default would reinstate the very constant W-A1 removed. `LoyaltyModel.CanDerive` states
  it in code. Billed: the USA's second election as a data line; France stays out of scope (R-EL10).

---

## W-A1 — loyalty derived from volatility · DONE (`10d76fc`)

- **[call]** Formula: `loyalty = 100 × (1 − |v(T−1) − v(T−2)| / max(v(T−1), v(T−2)))`. Relative,
  not absolute, so a 5-point move means the right thing to a 40 % party and a 6 % one; the larger
  of the two as denominator so doubling and halving are symmetric (both 50). **Zero authored
  constants** — the only inputs are two sourced elections.
- **[call]** A party absent at T−2 scores loyalty **0** — the correct statement of newness (nobody
  had a habit of voting for it), not a fallback.
- **Result:** Sweden's size-weighted mean loyalty 89.3, Italy's 48.4; Italy's FdI **16.7** and M5S
  **47.2** against Day-2's global 60 — the constant that crushed FdI in Day-2's gate.
- **Coverage constraint discovered and recorded:** name-join continuity covers ~99 % of the vote in
  Sweden, ~95 % Germany, **~53 % Italy, ~38 % Poland**. Below ~80 % the input is contaminated by
  organisational reshuffling. Independently supports Sweden as the prototype target (0.1).
- **Shortfall against the done-when's "all six":** USA and France not computable — billed.

## W-A2 — per-region priors, §27+§8 composition · DONE

- **[call]** Region **electorate weights come from the PRIOR election**, not the target — stricter
  than Day-2, which used the target's own weights. The target's turnout therefore cannot leak in.
- **[call]** Party **availability** comes from the target election's ballot access, which is known
  before any vote is cast and so is not a prediction.
- **[call]** "No worse" is judged at a **declared tolerance of 0.01 pp**, with the raw delta printed
  at four decimals so the tolerance can never hide a regression. Needed because where regions are
  homogeneous in availability (Sweden: all eight parties in all 29 valkretsar) §27 is correctly a
  **no-op**, and the only residual is whether damping is applied per region then summed, or once
  nationally — Jensen-type aggregation order, not model quality.
- **Result:** Germany **5.17 vs 5.85** §8-only (−0.68 pp, the composition fixed); Sweden delta
  **+0.0037 pp** inside tolerance. Proven on two countries, as the done-when requires.

## W-A3 — the gate re-run · DONE

**No parameter was re-fitted.** Spatial electorates are Day-1's; loyalties are computed from
sourced returns; the run uses the **backtest direction** throughout.

| country | coverage | Day-1 | Day-2 | Day-3 | verdict |
|---|---|---|---|---|---|
| SWEDEN | 99 % | 3.25 | 1.75 | **1.46** | IMPROVED |
| GERMANY-8 | 95 % | 5.78 | 4.66 | **5.36** | IMPROVED |
| POLAND | 38 % | 6.99 | 3.84 | **3.15** | IMPROVED · LOW CONFIDENCE |
| ITALY | 53 % | 5.61 | 6.69 | **7.14** | REGRESSED · LOW CONFIDENCE |

**VERDICT: PASS WITH STATED SCOPE** — both high-coverage countries improved on Day-1. Italy still
regresses, and its loyalty input is known to be contaminated, so that is weak evidence against the
model rather than a model failure. The pass is real; its scope is the two countries whose data
supports the claim.

**Three findings reported rather than smoothed:**

1. **Germany's Day-3 (5.36) is WORSE than its Day-2 (4.66).** Derived loyalty beat Day-1 but lost
   to the uniform 60 for Germany specifically. The gate's rule is improvement on Day-1, which is
   met — but the honest reading is that Germany's 2017→2021 volatility is not a good predictor of
   2021→2025 behaviour, and saying so is worth more than the passing number.
2. **§27's value is concentrated in regionally-confined parties.** In W-A2's nine-party set (with
   SSW) both-layers beat §8-alone; in W-A3's eight-party like-for-like set (SSW excluded, per
   Day-1's basis) it does not (5.80 vs 5.36). SSW is the party §27 exists to fix — remove it and
   the layer has little left to correct in Germany.
3. **Italy's FdI is not a loyalty problem.** Even at its derived loyalty of 45.1 it is under-
   predicted by 19 pp, because FdI went 4.35 → 29.27 % on a surge no pre-2022 data contains. That
   is a **missing-mechanism** finding (leadership, opposition positioning, a collapsed government),
   not a calibration one, and no loyalty value will fix it.

## Billed to Track F as a consequence

- **USA second election** (2020 + 2016 House national shares) — would make the USA's loyalty
  computable; the only country of the two where it would pay for itself.
- **Successor maps for Italy and Poland** — sourced bookkeeping lifting the name-join: the PD
  lineage, Lega's transformation, PiS/United Right composition, KO's assembly. **Build only if
  either becomes a playable target**; until then their gate rows stay LOW CONFIDENCE by design.

## W-A5 — perceived vs actual performance (§19) · DONE

The gap table called §19 an EXISTS row, and it was right: `PublicationSystem` already writes
`Country.Published` on the real release calendar with a noisy preliminary print and a later
revision, while `Country.State` holds the truth. **The vote model now reads `Published` and never
`State`** — `PerceivedPerformance.Actual()` exists only so the divergence can be reported, and
nothing feeding a vote share may call it.

- **[AUTHORED-DRAFT]** `UnemploymentNeutral = 6.0 %`, span 6.0 — 6 % reads neutral, 0 % reads 100.
- **[AUTHORED-DRAFT]** `InflationNeutral = 2.0 %`, span 6.0 — and **deviation either way is
  punished**: deflation is not a bonus, which a naive lower-is-better mapping would wrongly imply.
- **[AUTHORED-DRAFT]** `GrowthNeutral = 2.0 %`, span 6.0.
- **[AUTHORED-DRAFT]** `IncumbentSwingSpan = 0.15` — ±15 % on the incumbent's preference at the
  extremes. Deliberately modest: §39 forbids any single variable dominating, and a government that
  could win on published statistics alone would make the campaign layer pointless.
- **[call]** A stat that has **never been published** drops out of the average rather than being
  filled from `State` — the electorate has no figure to react to, and leaking the truth in would
  defeat the whole mechanism.

**Proven on a real six-year run** (Sweden, the release calendar producing the lag rather than any
injection): 36 of 36 samples show the published figure differing from the live one; **36 of 36
match an earlier true value more closely than the current one**, i.e. perception tracks the
publication rather than being merely noisy; the incumbent's modelled share differs at every sample
depending on which series drives it; and the divergence prints as a signed §31-style attribution
line, every term derived.

**Finding, recorded rather than dressed up:** in a *calm* economy the effect is small — the largest
published-vs-true gap over six years was **0.052 pp** of unemployment and the largest incumbent
effect **0.111 pp** of its own share. The mechanism is real, correctly wired to perception, and
currently quiet; it is a **shock** (a recession scenario, a sharp inflation turn) that would
exercise it properly, because that is when preliminary prints and revisions diverge most. Worth
re-measuring under `Italy Debt Crisis` or a comparable scenario before judging the magnitude.

## W-B1 — the campaign clock and calendar (§3) · DONE

`CampaignClock.cs` (pure, unwired) lays §3's phases on the game's existing day clock:
**Dormant → PreCampaign → Campaign → ElectionDay → Concluded**. Nothing advances anything; the
type answers "what phase is this date" and "what is legal in it", and the turn loop is untouched.

- **[AUTHORED-DRAFT]** `DefaultCampaignWeeks = 8` — the worklist's figure for Sweden's window.
- **[AUTHORED-DRAFT]** `DefaultPreCampaignWeeks = 26` — long enough for §3's preparation verbs to
  matter, short enough that the game is not a spreadsheet for half a year. Strikeable.
- **[call]** Phases **gate legality**, and a verb outside its window is *unavailable*, not merely
  weaker — a rally in the pre-campaign is not a thing. That is what makes the pre-campaign a
  different game rather than a slower version of the same one.
- **[call]** Which verbs continue into the campaign: fundraising, hiring, polling, opening an
  office and changing strategy do (§11 says strategy may change during); **candidate training,
  ad preparation and policy development do not** — by the campaign they are what you already have.
- **[call]** Election day leaves only the ground game (§26): GOTV and door-knocking. Persuasion is
  over; turnout is not.

**Proven** by walking all 279 days from before the pre-campaign to after polling day: five
transitions, each on its computed date (pre-campaign 2026-01-18, campaign 2026-07-19, election
2026-09-13, concluded 2026-09-14), the sequence monotonic with no phase revisited; legality flips
at the boundaries; **a snap election works as pure data** (3-week campaign, 1-week run-up, no code
change). Legal-action counts by phase: Dormant 0 · PreCampaign 8 · Campaign 13 · ElectionDay 2 ·
Concluded 0.

**One self-inflicted test error, caught and recorded:** assertion 5's own date arithmetic was off
by a day (it asserted the campaign was open on 2027-02-27 when the campaign opens on the 28th).
The code was right; the test was wrong, and it was the test that changed.

## W-B2 — resources and §35's diminishing returns · DONE

`CampaignResources.cs` (pure, unwired). Three resources because they constrain differently:
**money** (raisable, spends on everything, obeys §35), **time** (a fixed hours budget per campaign
day that cannot be saved, borrowed or bought — the resource that makes a campaign a series of
choices rather than a shopping list), and **volunteers** (§26's ground-game stock).

**§35 as a DECLARED CURVE, not a table of magic numbers:**
`effectiveness(spend) = 1 − exp(−spend / scale)`. Smooth, bounded, with marginal return strictly
decreasing everywhere — so the first krona beats the millionth *by construction*, and there is no
threshold a player can game by spending just over it.

- **[AUTHORED-DRAFT]** `MoneyScale = 500 000` SEK — chosen so §35's four prose bands fall out of
  ONE formula: 18.1 % of the effect at 100k, 63.2 % at 500k, 98.2 % at 2m, ~100 % at 10m.
  ⚠ Re-derive from real party spending when W-F5 sources it.
- **[AUTHORED-DRAFT]** `HoursPerCampaignDay = 12` — long enough to be a real day, short enough that
  §9's action costs (rally 4, interview 2, debate prep 6, tour 8) force daily trade-offs.
- **[AUTHORED-DRAFT]** `VolunteerHoursPerDay = 3`.
- **[call]** An unaffordable spend is **REFUSED, not clamped** — a clamp would let an over-budget
  campaign act at a silent discount, which is how a resource system becomes decorative.
- **[call, threshold]** The done-when's "first krona ≫ millionth" is asserted at **>5×** and comes
  out at **7.4×**, not the 100× a first draft assumed. The reason is a real trade-off, not a
  weakened test: at a 500k scale the millionth krona is only two scale-lengths out. A smaller scale
  would make the ratio look far more impressive (200k → ~150×) **and would be the wrong curve**,
  because it flattens the 500k→2m band that §35 explicitly calls "moderate impact" into nothing.
  The scale reproduces the spec's bands; the ratio is a consequence. The dramatic figure is
  reported alongside: the five-millionth krona is worth **22 026×** less than the first.

**Proven:** the curve strictly increasing and strictly concave over a 0–5m sweep (201 samples);
return per krona falling monotonically across all four bands (1.81E-6 → 2.29E-9); an unaffordable
spend refused with the pool untouched; a negative pool impossible to construct; hours resetting
daily while money and volunteers carry; and a worked campaign day where the fourth action is
correctly refused for want of hours.
