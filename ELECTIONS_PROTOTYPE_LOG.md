# Elections prototype — the running log (worklist `ELECTIONS_PROTOTYPE_WORKLIST.md` — Elias's kickoff of 2026-08-29 13:02, installed at root verbatim 2026-08-29 evening; until then it lived only in the session transcript)

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

---

## Standing rulings from the W-A / W-B1–2 review (2026-08-29)

- **Germany's Day-3 regression stands as reported, not chased.** Derived loyalty beat Day-1 and
  lost to the uniform 60 on that one pair (5.36 vs 4.66). A principled method occasionally worse
  than a lucky constant is still the better method — that constant was right for Germany by
  accident and catastrophically wrong for Italy, which is how it failed Day-2's gate. Recorded in
  `LoyaltyModel`'s doc as **a known limit of two-election volatility**: if a playable Germany ever
  needs it closed, the fix is **a longer election window**, never a re-introduced constant.
- **§27's regional value is correctly concentrated in regionally-confined parties.** That is the
  layer working, not a shortfall: a regional layer that also moved nationally-uniform parties would
  be adding noise dressed as signal. The scope is now written into `RegionalVoteModel`'s doc with
  an explicit instruction not to "generalise" it, and the `ElectorateOverride` hook is reserved for
  a **non-circular** source of regional variation and no other.
- **Italy's FdI is the standing MISSING-MECHANISM TEST CASE.** ~19 pp under-predicted at any
  loyalty, because 4.35 → 29.27 % is a surge no pre-2022 data contains. Registered as an **open
  test against §13 media, §18 event salience and §22 momentum**: when W-B9, W-B10 and event
  salience land, re-run Italy 2022 and report whether the surge becomes reachable. **Reachable =
  the strongest validation this model can get; unreachable = a named ceiling, recorded as such.**
  Do not tune toward it — the value of the test is destroyed the moment anything is fitted to pass it.

## W-B3 — §12's eight actions through §42's chain · DONE

`CampaignActions.cs` (pure, unwired). **The item's bar is met architecturally, not by convention:**
`ChainTrace` has no share, preference or party member, so an action has **nowhere to put a vote
delta**. Its only outputs are persuasion and enthusiasm *pressures*; `CampaignPressure` converts
persuasion into a **compatibility bonus**, and the preference is then recomputed by the same
`PreferenceModel` that runs with no campaign at all. The campaign moves the inputs; the output is
always derived.

The chain multiplies at every stage — reach × attention → exposure; salience × issue-match →
relevance; × credibility × weight → persuasion — which is what makes "it travelled the chain"
checkable rather than asserted.

**Proven three ways** (one would have been too easy):
- **structural** — reflection over `ChainTrace` finds no member that could hold a share;
- **behavioural** — for all eight actions, zeroing any ONE of audience / salience / issue-match /
  credibility drives persuasion to exactly zero (8 × 4 = 32 cases, 0 failures). A direct `+2 %`
  would survive all four;
- **end to end** — a week of campaigning moves party 0 from 36.550 % to 36.741 %, shares still sum
  to 1, and **the decisive test**: the identical campaign with the chain severed (zero audience)
  moves *nothing*.

**[AUTHORED-DRAFT]** the eight actions' costs, hours and weights (hours follow §9's own figures
where it gives them: rally 4, interview 2, policy announcement 3); `PersuasionPerCompatibilityPoint
= 40 000`; `EnthusiasmPerTurnoutPoint = 60 000`.

**Two calibration findings, recorded not smoothed:**
1. **The magnitude is probably too small to be playable yet.** A hard week — three rallies, two
   town halls, a national TV buy, daily door-knocking — moves the party **+0.19 pp**. Over a full
   8-week campaign that compounds to roughly a point and a half. The mechanism is right and the
   *scale* is a play-calibration question owned by `PersuasionPerCompatibilityPoint`; it should be
   set once there is a full campaign loop to feel, **not** guessed at now.
2. **Free actions get full reach, which makes the interview dominant per hour** (2 268 persuasion
   for 2 hours and no money, against a TV buy's 1 124 for 500 k). Defensible — an interview's cost
   is time and access, not money — but it means a player who only does interviews is currently
   optimal, which §34 says the design should not reward. The fix is an availability/fatigue limit
   on earned media (a §13 concern), noted for W-B9 rather than patched here.

## W-B10 — polling and momentum (§20–§22) · DONE

`Polling.cs` (pure, unwired). **The structural rule: the UI never sees the truth.** A `Poll` carries
sampled shares, sample size, MoE, field date and house — and no reference to the true preference
vector, proven by reflection. `Conduct` is the only function that touches truth, and it can only
return a `Poll`.

**Three error sources kept separate because they behave differently:** sampling error (random,
shrinks with √n, and is what the MoE describes), **house effect** (systematic, per-house,
per-party, and does NOT shrink with sample size), and turnout error (deliberately absent — it is
§26's at election time, named so the omission is visible).

**Measured, over 2 000 replays of an unbiased pollster:**
- **coverage 94.63 %** against a nominal 95 % — the margin of error is *honest*, which is the only
  test of a MoE that means anything;
- **0 of 2 000** polls equalled the truth exactly;
- a house's lean **survives a 10× larger sample** (2.34 pp at n=1000, 2.35 pp at n=10 000) while
  sampling precision improves by **exactly √10** (±2.92 → ±0.92, ratio 3.16);
- §21's purchase is real: ±3.67 pp for 40 000 kr against ±1.42 pp for 350 000 kr.

**[AUTHORED-DRAFT]** the house roster and their leans; `MomentumHalfLifeDays = 7.0`.

**Finding — §22's own worked example is internally inconsistent.** Its three points for a +2.0
shock (~1.4 after several days, ~0.4 after two weeks, ~0.0 after a month) imply half-lives of
~9.7, ~6.0 and ~5.3 days respectively: the spec describes decay that *accelerates*. Rather than
invent a bespoke curve to hit three mutually contradictory numbers, this keeps a plain exponential
at the best-compromise 7 days (+2.0 → 1.22 → 0.50 → 0.10) and the harness asserts **the shape the
spec actually requires** — monotone, materially reduced within a fortnight, substantially gone
within a month. If play shows the tail is too fat, the honest upgrade is a **named second
mechanism** (a news-cycle half-life distinct from a reputation one, §38), not a fudged exponent.

---

## Standing rulings from the W-B3 / W-B10 review (2026-08-29)

- **Campaign magnitude stays untouched until there is a loop to feel.** +0.19 pp for a hard week
  (~1.5 points over eight weeks) is roughly right for a real campaign and probably wrong for a
  game — but feel cannot be calibrated against a loop that does not exist.
  `CampaignPressure.PersuasionPerCompatibilityPoint` is left alone through W-E1/E3/E4, after which
  a **play-calibration list** opens in the records with this constant as its first entry. It will
  not be the last thing that needs a human's hands.
- **Interview dominance is W-B9's, as a MECHANISM, not a nerf.** Earned media is free because
  *someone else decides whether to book you*, so the scarce resource is **media interest**.
  Implement it in §13 as availability driven by newsworthiness (coverage, momentum, recent events)
  — never as a flat cost or cap bolted onto the action. This also hands §13's coverage loop a real
  input: **a party nobody covers cannot buy its way onto the air.**
- **§22's arithmetic contradiction stands recorded, not fitted.** The compromise 7-day half-life
  with the inconsistency documented in the constant's own doc is correct. If play later wants
  accelerating decay, that is a **deliberate design choice with its own reason** — never a
  reconstruction of §22's inconsistent illustrative example.

## The screen class (Track E, from W-E1 on)

These are the first screens of a new class, not re-skins. Each is its own item: filmed at
1280/1600/1920/2560, guards silent, `ScreenEdgeCheck` clean, any new label-clipping instance
treated as the known class. Built structurally in the v3 idiom — rail, one full-bleed sheet,
ledger rows, instruments, existing sprites; **no sprite is invented**, and a gap becomes a line
for the Track H Design ask. Every figure is derived; the v3 stage's text budget applies.
**R-N2 holds: the screens read the model through harness-staged state and are reachable from no
gameplay path** — the `DrawInstrumentLadder` precedent, where a private field only the screenshot
driver sets swaps the frame for one capture at a time.

---

## W-E1 — Campaign HQ (2026-08-29)

The first screen of the Track E class. Files: `Assets/Scripts/UI/GameController.Campaign.cs` (the
screen), `Assets/Scripts/Elections/CampaignSnapshot.cs` (what it is handed),
`IconLibrary.GetPartyMark` (the five delivered marks' first call site), and the driver's
`-shotcampaign` pass.

**How R-N2 is held.** The screen draws only when `_campaignScreen` has a value, and the only setter
is `internal void SetCampaignScreen(CampaignSnapshot?)`, called by the screenshot driver. The branch
sits beside `_onDesk` inside the frame's content column, so the rail is the real rail and the sheet
is composed in the frame it will ship in — but there is no rail cell, no tab, no save hook and no
gameplay path. Wiring at W-G1 is *adding the rail cell*, and nothing else.

**What is derived, stated rather than blurred.** The poll is a real `PollingSystem.Conduct` draw
against Sweden's SOURCED 2022 vector (`ElectionsData/sweden/returns_2022.md`; Valmyndigheten's final
count); the ± comes out of that draw; momentum is a real `MomentumTracker` shock decayed on §22's
half-life; every queued action's cost is read from `CampaignActions.Spec`; the legality list is
`CampaignLegality.LegalActions`, never restated; the perceived-economy index is
`PerceivedPerformance.Perceived` read off the LIVE warmed-up country. The war chest, volunteer
counts and office upkeep are **[AUTHORED-DRAFT] staging** and the pass logs them as such (W-F5 will
source real party finances). No spec illustration ships as data.

### Decisions taken and logged (R-N1)

- **~~A mark key is a suffix~~ → a mark key is the FULL file stem.** `IconLibrary.GetPartyMark`
  takes `"mark_party_se_s"`, not `"se_s"`, so it is the literal one-line wrapper over the
  `Resources.Load` call `PartyMarkCoverageCheck` already makes and compares against file names.
  A second naming convention beside the check's would have been the worse outcome; when the party
  system's seeds land on `main`, their mark names feed both without translation.
- **Staff rows carry no personal names.** Inventing people is inventing data. The ledger shows the
  post, whether it is filled, and the draft bonus. Names belong to W-B5 with their own sourcing.
- **The race bars scale to the leader's band, not to 100 %.** Eight parties none of which clears a
  third would otherwise read as eight stubs. The axis is NAMED in the methodology line, because an
  unlabelled rescaled bar is a lie by omission.
- **The ± band is drawn under the point estimate, not printed beside it.** W-B10's rule that the UI
  never sees the truth is carried into the view by the type it is handed (a `Poll`, which cannot
  express a truth); showing the interval as the bar's own width makes the uncertainty part of the
  reading rather than a footnote to it.
- **The masthead chips are drawn `disabled`.** Nothing is wired, and a chip that looked live while
  doing nothing would be the worse lie.
- **An over-committed queue is shown as over-committed.** `ResourcePool.TrySpend` refuses rather
  than clamping (W-B2), so a queue genuinely can be unaffordable; the screen says so in the caution
  ink instead of showing a plausible total.

### A note on the film harness

`-shotcampaign` demands `-shotcountry=Sweden` and fails the run otherwise: the staged returns are
sourced *as Swedish*, and filming them under another country's frame would put real Valmyndigheten
figures beside the wrong flag — exactly the quiet wrongness the data classes exist to prevent.

---

## W-E3 — the action screen (2026-08-29)

Files: `Assets/Scripts/UI/GameController.CampaignAction.cs` (the screen),
`Assets/Scripts/Elections/ActionScreenSnapshot.cs` (what it is handed),
`CampaignActions.ChainBand` + `ResolveBand` (the model half),
`Assets/Editor/ChainBandHarness.cs` (the proof).

**The estimate's range is a propagated measurement.** The band spans §42's chain evaluated at the
ends of the player's own polling error on salience and issue-match; audience is structural,
credibility is the party's own record, spend is exact. `ChainBandHarness` sweeps 41×41 points per
action across all eight (13 448 interior points) to prove the two-corner shortcut really bounds the
box, and `ResolveBand`'s doc states that a future non-monotone stage forces it to become a sweep.

### Findings carried forward

- **Enthusiasm carries NO measurement uncertainty.** §42 derives it from exposure and credibility,
  neither of which is polled, so its band is a point. Possibly wrong about the world — it is odd
  that caring more about an issue changes persuasion but not motivation to turn out — but a change
  here is a MODEL change with its own reason and harness, never a width invented at the drawing
  layer. Recorded, not fixed.
- **Interview dominance is now visible on a screen**, not just in a harness log: 0 kr, 2 h, one of
  the longest bands on the sheet. This is the input W-B9's ruling asks for — media INTEREST as the
  scarce resource (§13 availability driven by newsworthiness), never a flat cost or cap on the
  action.
- **Five defects on this item passed every guard.** All five fitted their rects; all five were wrong.
  The screen class's four-width film and a human reading it are what caught them — recorded because
  it is the standing argument for the cost of that film.
- **The dead-space class recurs on both Track E screens**, worse before the per-option bands were
  added. Stays a ruling and a Track H Design line, not a fix.

### Next

The play-calibration list opens after W-E4, per the W-B3/W-B10 review ruling, with
`CampaignPressure.PersuasionPerCompatibilityPoint` as its first entry.

---

## W-E4 — the polling screen (2026-08-29)

Files: `Assets/Scripts/UI/GameController.CampaignPolling.cs`,
`Assets/Scripts/Elections/PollingScreenSnapshot.cs`.

**§21's decision made arithmetic.** Kronor against percentage points of precision, every ± DERIVED
from the offer's sample size by `PollingSystem.MarginOfErrorPp` — the same function a conducted poll
reports with, so the price list cannot promise an accuracy the polls fail to deliver. Sample sizes
and prices are `[AUTHORED-DRAFT]`; the ladder's SHAPE (each point costing more than the last) is √n
and is not authored at all.

### Calls logged (R-N1)

- **Regional / demographic / turnout depth is NOT folded into the cost-per-point figure.** They are
  different KINDS of answer, not narrower ones; averaging a capability into a precision score would
  hide the trade §21 exists to pose. Named on the row, excluded from the price, and the footnote
  says why.
- **The ± is quoted at a NAMED party's NAMED share**, because a poll's margin depends on the
  proportion measured — one number for a whole poll would be wrong.
- **§20's other error sources are printed on the screen that sells precision** — late swings,
  turnout, undecided voters, tactical voting, house lean — because a price list that sold precision
  without naming what it does not cover would be selling a false promise.

### A durable layout fact (now three instances)

**Pagella and the mono document face do not share a line box.** At 1920 an 11 px mono caption is
21.2 px tall against a 13 px body's 20.1, and `DeskPx`'s integer rounding makes the crossover
width-dependent. A row that measures its height from one face and draws a label in the other is a
latent clip. **Rule: such a row takes `Mathf.Max` of both measured heights.** W-E1's momentum
caption, W-E3's options ledger and W-E4's offer head all now do.

**Both genuine overflow classes today appeared at exactly ONE of the four widths** — the mono/body
mismatch only at 1920, the over-long caption only at 1280. That is the concrete argument for the
screen class's four-width film.

### The list is open

`ELECTIONS_PLAY_CALIBRATION.md` created on the completion of W-E1/E3/E4, per the W-B3/W-B10 ruling.
Six entries; `PersuasionPerCompatibilityPoint` first; the enthusiasm-vs-salience model question and
earned-media dominance both carried there with their rulings attached.

---

## W-C1 — AI parties (§32) and expected-value decisions (§33) (2026-08-29)

Files: `Assets/Scripts/Elections/CampaignAi.cs` (the personalities, the view, the scoring),
`Assets/Scripts/Elections/CampaignRun.cs` (the AI-only campaign — the one place the truth lives),
`Assets/Editor/CampaignAiHarness.cs` (the proof); `SimulationRandom.Stream.CampaignAi = 8`
APPENDED (the ElectionNoise precedent; nothing live draws from it).

**The done-when, clause by clause.** (1) *An AI-only campaign completes deterministically* — MET:
seed 777 twice gives digest `d7670f735d1b8864` and bit-identical final shares, 56 of 56 campaign
days, no money negative. (2) *The five personalities produce measurably different action mixes* —
**MET for what the environment can distinguish, PENDING for the rest**: the chaotic and populist
mixes differ from every other's (min L1 0.604 / 0.504), the grassroots party buys the least
broadcast, the establishment party is the only one that saves up for the 500 000 kr television buy
(3 of them), the professional buys the most polling (8) and never acts blind, the populist ends the
campaign with 0 kr while the professional keeps 1.44 m, the chaotic mix varies most seed to seed
(0.161 against ≤ 0.039). **Professional, establishment and grassroots are indistinguishable (L1
0.013–0.024): all three interview all day.** That is the environment's fact, not the AI's — see the
findings — and the harness prints it as `PEND`, with its measurement, never as a pass. (3) *No AI
accesses hidden state the player cannot buy* — MET structurally (reflection over `AiView` finds no
truth-shaped member; `Evaluate` takes the view, the personality, a pool and the reserve and
nothing else) and behaviourally (no poll → every candidate estimate is BLIND; the never-polling
chaotic party's 284 decisions were all blind).

### Decisions taken and logged (R-N1)

- **§33's score is in ONE unit — the model's own compatibility points — and there is no authored
  kronor-to-votes exchange rate.** The first draft priced money as a fraction of a daily budget
  against a normalised gain, and that made every money action unaffordable in score terms (a
  500 000 kr buy against a 120 000 kr day never scored). Replaced: money is priced at the action's
  OWN efficiency at its smallest outlay (§35 is concave, so a bigger outlay is always less
  efficient, and `CostWeight` says how much less the personality tolerates); money is otherwise a
  CONSTRAINT — a reserve the party's `SpendPace` releases over the days left, so a television buy is
  saved for rather than priced against one day; hours are the binding daily resource, so candidates
  rank by value per hour.
- **The horse-race poll is on the view and NOT in the score.** "Targets swing voters" needs §25's
  swing index (W-E2) and "reacts quickly to events" needs §18's events; neither is invented, so the
  professional's personality today is polling discipline, risk aversion and pacing.
- **No attack verb.** §33's worked example scores "Attack Opponent"; §12's eight have none and §11's
  negative campaign is W-B8's. Not invented.
- **A personality that will not act blind waits.** `ActsBlind` false (the professional) means no
  estimate → no action, so it idles until its reserve affords the first poll (day 3 at even pacing)
  — a visible, correct behaviour rather than a guess dressed as a decision.
- **Blind means a flat prior, not a wide estimate:** salience and match at 0.5 ± 0.5, so the band
  runs from nothing to everything and optimism / risk aversion decide whether to act on it.
- **Public tracker every 7 days, free to every party; the commissioned poll measures issues too.**
  `CampaignIntelligence.MeasureIssues` is the only new function that touches a truth and returns
  measurements with the ± their sample size buys — the `PollingSystem.Conduct` idiom.
- **Candidate bounds:** local actions evaluated in the 4 largest regions by public audience; the
  general message plus the 2 most salient measured issues; the populist the top issue only.
- **The staging's data classes, stated:** SOURCED — Sweden 2022 prior and 2018 shares
  (Valmyndigheten), loyalty derived from them (W-A1), the 29 valkretsar's 2018 valid votes as
  audiences, EB105 Spring 2026 salience (climate .26, crime .18, defence .17, education .16 — the
  "% naming among the two most important" read as salience on 0–1; *"threats to democracy"* has no
  §6 slot and is billed); DERIVED — compatibility at the fixed point where `PersuadedShares == prior`
  (`c_i = 70 × (prior_i / max)^(1/3)`, so an idle campaign reproduces the 2022 result exactly —
  asserted); `[AUTHORED-DRAFT]` — issue-match 0.5 flat (W-F2), credibility 0.6 flat (W-F6), war
  chest 2 400 000 kr each and EQUAL by design so the mixes differ by personality alone (W-F5).
- **The rational three's collapse is recorded as PENDING, not forced.** An affinity large enough
  to make the grassroots party knock doors in this environment would be a number chosen to make a
  test pass — the exact thing the calibration list forbids.
- **Pre-campaign days are not simulated** (§3's preparation verbs have no price yet); **momentum
  takes no shock** (nothing that shocks it exists yet) — the view shows zeros honestly.

### [AUTHORED-DRAFT] values, one line each (all strikeable; the play-calibration list carries the block)

- `PersonalityCatalog` — affinities in `TheEight`'s order (rally, town hall, door, TV, digital,
  social, interview, policy) · temperature · risk aversion · optimism · cost weight · spend pace ·
  enthusiasm value · poll every N days · focus on top salience · acts blind · spend multipliers:
  - Professional: all 1.0 · 0 · 1.0 · 0.35 · 1.0 · 1.0 · 0.5 · 7 · no · **no** · {0.5, 1, 2}
  - Populist: 1.8/0.8/0.8/0.9/1.1/1.8/1.2/0.6 · 0.15 · 0.3 · 0.7 · 0.4 · 1.6 · 1.0 · 14 · **yes** · yes · {1, 2, 3}
  - Establishment: 0.8/1.0/0.6/1.8/1.0/0.6/1.6/1.4 · 0.05 · 1.2 · 0.5 · 0.7 · 1.0 · 0.4 · 14 · no · yes · {0.5, 1, 2}
  - Grassroots: 1.0/1.5/2.2/0.2/0.5/1.2/1.0/0.8 · 0.10 · 0.8 · 0.5 · 1.0 · 0.7 · 1.6 · 21 · no · yes · {0.5, 1}
  - Chaotic: all 1.0 · 1.0 · **−0.6** · 1.0 · 0.2 · 2.5 · 0.8 · **never** · no · yes · {0.5, 1, 3}
- `CampaignAi.RiskScale = 0.25` · `PollingHours = 1` · `LocalCandidateRegions = 4` · `IssueCandidates = 2`.
- Harness staging: `FlatIssueMatch 0.5` · `FlatCredibility 0.6` · `WarChest 2 400 000` ·
  `CompatibilityCeiling 70` (the derived fixed point's level; its SHAPE is not authored).

### Findings carried forward

1. **The chain saturates at the real national audience.** Every rational party delivers +197 to
   +225 compatibility points over the campaign (`persuasion / 40 000`), and `ElectionScales` clamps
   every party at 100 — so the final-share column is the clamp's arithmetic, not the campaign's
   difference. W-B3 measured +0.19 pp for a hard week at a 100 000 audience; at 6.5 million the same
   chain is 65× that, because reach is linear in audience AND in repetition (the same electorate
   "reached" six times a day). **A mechanism question before a calibration one** — bounded reach,
   repeated-exposure decay, W-B9's media interest — and the play-calibration list's entry 1 gains
   this measurement, not a new value.
2. **Local reach is W-B3's placeholder, and it shows.** Door-to-door reaches 2 % of a REGION per
   five hours (16 000 doors in Stockholms län), yet against a national channel's whole electorate no
   local action is ever worth its hours. The right model is an ABSOLUTE count — volunteer-hours ×
   doors per hour, §10's offices — which is **W-B4/B11's**; a rider on their done-when: the
   harness's `PEND 2b/2c` flip to assertions and pass.
3. **Interview dominance, measured a third way.** 99–100 % of every rational personality's actions.
   Stays W-B9's as a MECHANISM (the standing ruling); rider on W-B9's done-when: `PEND 2a-iii/2e`
   flip to assertions and pass.
4. **"Threats to democracy"** — Sweden's joint-top EB105 issue has no §6 slot. Billed, not mapped.

### Riders placed on later items (recorded here so they are not lost)

- **W-B9** — done-when gains: `CampaignAiHarness` lines `2a-iii` (professional / establishment /
  grassroots separate) and `2e` (establishment leads television + interview) become assertions
  and pass with no affinity changed.
- **W-B4 / W-B11** — done-when gains: lines `2b` (populist rallies) and `2c` (grassroots door-knocking)
  become assertions and pass; door-to-door reach re-modelled as an absolute count.
- **W-E2** — the professional's "targets swing voters" reads §25's swing index into the score.
- **W-B8** — the attack verb, if §11's negative campaign brings one, is scored by the same §33 terms.

**R-N2 held at this boundary:** `traj_wc1_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the eight checks exit 0 (`check_*_wc1.log`); harness `campaignai_wc1d_20260829.log`.

---

## W-B6 — campaign strategy (§11): the five as modifiers over the whole chain (2026-08-29)

Files: `Assets/Scripts/Elections/CampaignStrategy.cs` (`CampaignStrategy`, `StrategyModifiers`,
`CampaignStrategyModel`), `CampaignPressure.AddAgainst` (in `CampaignActions.cs`),
`Assets/Editor/CampaignStrategyHarness.cs`; the AI (W-C1) given a strategy per personality and
its run applying the modifiers on both sides of the seam.

**Done-when.** Each strategy's stated trade-off shown (7 of 7 assertions on the same rally for a
loyal group at 85 and a swing group at 20, on and off the focus issue) and **no strategy dominates
a 30-electorate sweep** (loyal share 0.1–0.9 × focused/diffuse issues × opponent 0.4/1.0/1.6):
Base Mobilization wins 21, Broad Appeal 6, Populist 3, **Swing Voter and Negative Campaign win
nowhere** — recorded below, not tuned.

### Decisions taken and logged (R-N1)

- **A strategy is multipliers on §42's stages, never an action and never a vote delta.** Reach and
  credibility multiply the chain's INPUTS (so a zero anywhere still annihilates the effect);
  persuasion, enthusiasm and salience-shift multiply its outputs; the result is the same
  `ChainTrace` every consumer reads. `None` is the identity — every earlier measurement stands
  (asserted).
- **Every multiplier depends on WHO the group is** — loyalty (0–100, `swing = 1 − loyalty/100`)
  and whether the group prioritises the message's focus issue (`Prioritises`: the issue's weight at
  or above the group's mean weight). That is what makes each strategy a trade-off: the same
  strategy lifts one group and lowers another.
- **The negative campaign's only route to an opponent is `CampaignPressure.AddAgainst`** — a
  negative pressure on the opponent's compatibility, recomputed by `PreferenceModel` like every
  other pressure. Backlash is an expected cost (credibility ×0.9), not a seeded event — the event
  is §17/§18's (W-B8). Media attention ×1.5 is CARRIED on the modifiers for W-B9 and read by nothing
  yet.
- **The AI runs strategies now** (`PersonalityProfile.Strategy`): professional → Swing Voter,
  populist → Populist, establishment → Broad Appeal, grassroots → Base Mobilization, chaotic →
  Negative Campaign (its target the leading OTHER party in the latest poll it has seen — chosen
  from a `Poll`, never the truth). The modifiers apply to the AI's own estimate (`Points`) exactly
  as to the world's response (`CampaignRun`), so a party cannot mis-price its own strategy. The
  electorate is ONE group at W-A1's size-weighted mean loyalty (`Setup.ElectorateLoyalty`, 89.7 for
  Sweden — a public derivation) until W-F4's voter groups give the strategies per-group targets;
  "prioritised" for a one-group electorate means the message is on its most salient issue.
  W-C1's digest moves accordingly (`d7670f73…` → `463560d1…`); every C1 assertion still passes.
- **The sweep's outcome metric is the model's own unit conversion** — own persuasion / 40 000 +
  enthusiasm / 60 000, minus the opponent's, with negative pressure counted through
  `CampaignPressure`. A different weighting of turnout against persuasion would move the table;
  this one is the model's, not the harness's.

### [AUTHORED-DRAFT] magnitudes, one line each (the shapes are the spec's; strikeable)

Broad: reach ×1.15, persuasion ×0.85, polarisation ×0.5 · Base: enthusiasm ×(1 + 0.6·loyalty),
persuasion ×(1 − 0.5·swing) · Swing: persuasion ×(0.7 + 0.8·swing), enthusiasm ×(1 − 0.3·loyalty) ·
Negative: opponent share 0.6, own persuasion ×0.8, credibility ×0.9, media ×1.5, polarisation ×1.5 ·
Populist: focus persuasion ×1.5 and enthusiasm ×1.3, other persuasion ×0.6.

### Findings carried forward

1. **Swing Voter and Negative Campaign win no electorate in the sweep.** Swing's loyal-group cost
   (enthusiasm ×0.745 at loyalty 85) outweighs its swing-group gain at every loyal share; Negative's
   60 % against the opponent is worth less than its own 20 % persuasion cut plus the credibility
   cost when the opponent runs an identical week. Both are the model's statement at these
   magnitudes and this metric — a play-calibration line (entry 8), never tuned to make the table
   prettier. §32's professional runs Swing Voter regardless, because "targets swing voters" is what
   the spec says it does; whether that is a losing choice is now a measured question.
2. **Base Mobilization's 21 of 30 is the enthusiasm conversion.** One enthusiasm point costs 60 000
   pressure against 40 000 for persuasion, and Base lifts enthusiasm by up to 60 %; at a loyal
   share above 0.5 nothing beats that. Entry 8 carries it beside finding 1.
3. **Media attention is a multiplier nothing reads yet.** It is on the modifiers so W-B9 has its
   input the day it lands.

### Riders

- **W-B9** — read `StrategyModifiers.MediaAttentionMultiplier` into media interest.
- **W-B8** — the backlash as a seeded event on its own stream, replacing the expected-cost
  credibility multiplier or standing beside it (a design choice with its own reason).
- **W-F4** — the strategies target VOTER GROUPS; the one-group electorate retires when the groups
  exist, and `CampaignRun` should then apply modifiers per group.

**R-N2 held at this boundary:** `traj_wb6_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the eight checks exit 0; harnesses `strategy_wb6b_20260829.log` and `campaignai_wb6_20260829.log`.

---

## W-B9 — the media system (§13) and audience segmentation (§14): media interest as availability (2026-08-29)

Files: `Assets/Scripts/Elections/MediaSystem.cs` (`MediaOutlet`, `MediaSystem`, `MediaCoverage`,
`MediaInterest` + its `BookingLedger`, `MediaCatalog`), `Assets/Editor/MediaHarness.cs`; the AI
campaign (`CampaignRun`, `CampaignAi`, `CampaignAiHarness`) now runs under the media.

**Done-when.** *A coverage spike decays on the measured curve* — a saturated spike of 1.0 follows
the declared 3-day half-life to 1.7e-16 over 30 days and is at 0.10 % after a month. *The same
message performs differently by outlet audience* — one climate television message: ×1.90 per
person reached through the young-urban outlet against the older-rural one; a crime message ×2.00
the other way; through the general-population outlet the two are identical. Both met; 14 of 14
assertions.

**The standing ruling (W-B3 / W-B10 review), executed here and nowhere else.** Media INTEREST is
availability: each day the outlets allocate their interview slots by how newsworthy each party is
(coverage, |momentum|, the PUBLISHED race, §18's events when they exist), one bounded figure
`1 − exp(−Σ weights)`. A party at 4 % with no coverage is booked by no outlet whatever it would
pay; the same party after a policy announcement and a rally is booked seven times; a bigger party
is booked more on a quiet day. **The interview's spec is untouched — 0 kr, no cap, no fee**
(asserted). Coverage is a stock that decays on the news cycle and grows only through a saturating
gain, so it cannot spiral (§13's requirement): a year of maximal news peaks at exactly the stated
ceiling 4.85, and interest stays under 1. Coverage creates momentum (§13's chain) as a shock of
1.5 pp per unit of the day's GAIN — bounded because the gain is.

### Decisions taken and logged (R-N1)

- **Bookings are a LEDGER, not a daily draw and not a daily rounding.** The first allocation gave
  each outlet's slots to its most interesting parties one per round, and with four outlets each
  restarting at the top the two most newsworthy parties took every slot in the country. The second
  (largest-remainder per day) starved the fourth: a party at 19 % went eight weeks without an
  interview. The ledger carries each party's fractional entitlement per outlet from day to day
  (asserted: at steady interest 0.99/0.99/0.70/0.45/… the four eligible parties are booked 82/82/57/38
  of 270 slots over a month; the two under every threshold never).
- **Own channels reach a party's following, the press reaches in proportion to interest, paid
  channels reach their platform's ceiling** (`MediaSystem.NationalAudience`): television = the
  television outlets' combined reach (0.80); a digital ad = `PlatformReach` 0.55; a social post =
  polled share × `FollowingRatio` 0.30 (a party nobody follows posts to nobody); a policy
  announcement = the press's interest in the party. W-B3's placeholder had every national action
  address the whole electorate; the AI's compatibility bonuses fell from +197…+225 (everyone
  clamped at 100) to +7…+37. **This is the mechanism half of the C1 saturation finding**, and the
  constant `PersuasionPerCompatibilityPoint` was not touched.
- **A television buy runs across the television outlets** (`IsTelevision`), not through one — the
  first draft ceilinged it at the largest outlet and television was strictly dominated by digital.
- **Outlets are ARCHETYPES** (public broadcaster, commercial television, tabloid, broadsheet) with
  authored reach, slots, thresholds and two-group compositions; no real outlet name carries an
  authored number. Real Swedish reach (Kantar / Orvesto) and follower counts are billed.
- **Social newsworthiness 0.03, not 0.08:** a post is not news unless it travels; virality is a §13
  hook, not modelled. Set during this item's design, before any measurement was fitted.
- **`StrategyModifiers.MediaAttentionMultiplier` is read** (W-B6's rider discharged): the negative
  campaign makes 1.5× the news of the same action.
- **The AI under the media (W-C1 extended):** the view carries the party's bookings (the outlet
  reach of each), the per-kind national audiences (public facts or its own), the poll's price; an
  interview is a candidate only with a booking; the poll's price is kept back once a poll is due
  (the first B9 run had every party spend its reserve daily and never poll again); money is priced
  on the INCREMENT above the smallest outlay (the first draft priced the whole spend, so at
  `CostWeight` 1.0 every money action scored exactly zero and floating-point noise decided a whole
  campaign). **No saving rule** — two were tried and both were worse than none (idling; a week of
  social posts to afford one buy); a big-ticket buy needs a BUDGET PLAN, which is W-B5's campaign
  manager's, recorded there.

### [AUTHORED-DRAFT] values, one line each (the play-calibration list's entry 9)

`CoverageHalfLifeDays 3` · `CoverageScale 1` · `MomentumPpPerCoverage 1.5` · interest weights
coverage 1 / |momentum| 0.15 per pp / polled share 0.8 / events 1 · newsworthiness policy .25,
interview .20, rally .15, television .10, town hall .05, digital .05, social .03, door .01 ·
`PlatformReach .55` · `FollowingRatio .30` · the archetypes: public broadcaster .45 / 3 / .15 (all
groups), commercial television .35 / 2 / .25 (30/70), tabloid .30 / 2 / .10 (75/25), broadsheet
.15 / 2 / .30 (65/35) · in the AI: `CheapSpendFraction` and `SavingRatio` removed with the rule.

### What the C1 harness says now (digest `5152fe7bc2b41c0c`, 20 assertions, 7 PEND)

Every party is booked 50–81 times; the mixes: professional — posts, interviews, announcements;
populist — interviews and posts, chest spent by day 34; establishment — posts, interviews,
announcements, town halls; grassroots — **doors (26–33), interviews, announcements**; chaotic —
town halls, doors, posts, interviews, blind throughout. **Asserted:** chaotic distinct from all
(0.477), grassroots distinct from both media personalities (0.71 / 0.61 — the W-B9 rider, half
discharged), professional polls most and never blind, populist front-loads (80 % spent by day 34
against the professional's 44), chaotic the most inconsistent day to day (1.035 against ≤ 0.888).
**PEND, blockers named:** professional ≈ establishment (0.101) and populist vs the rest (0.292)
— both large parties' days are the media's bookings; separation waits on a budget plan for
television (**W-B5**) and rallies with real local reach (**W-B4**); the advertising claims (nobody
advertises but the unbooked; no party can afford a 500 000 kr buy on the day under even pacing) —
**W-B5**; door-to-door "largest share" — **W-B4/B11** (holds early: 12 % against the chaotic's 19 %).

### Findings carried forward

1. **A budget plan is the missing campaign-manager mechanism.** Even pacing plus greedy daily
   choice cannot produce a television buy, and every saving heuristic tried produced a worse
   pathology than none. §9's staff (W-B5) is where a plan — a share of the chest per channel —
   belongs. Rider on W-B5's done-when: the `PEND 2d / 2e / 2e-ii / 2a-iii` lines.
2. **Fair bookings equalise the rational personalities.** Once every party gets its proportional
   airtime and no one can advertise, the professional and the establishment do the same things;
   affinities of 1.4–1.8 do not overcome what the environment makes available. Not tuned.
3. **Seed-to-seed variability collapsed** (professional 0.652 → 0.029) once the knife-edge zero-
   score money actions were repriced — the earlier cross-seed instability was the scoring bug,
   not the polls.
4. **The saturation finding is mostly audiences.** With reach bounded by the media landscape, the
   bonuses are +7…+37 — calibration entry 1 is re-read accordingly.

### Riders

- **W-B5** — a budget plan per channel; the four `PEND` lines above flip there.
- **W-B4 / W-B11** — local reach as an absolute count; `PEND 2b / 2c / 2a-ii`.
- **W-B8 / W-B7 / §18** — `MediaCoverage.AddShock` is their input (a debate, a scandal, an event).
- **W-F5 / W-F6** — real outlet reach and follower counts replace the archetypes' figures.
- **W-E2 / W-E3** — the action screen gains the booking diary and the per-kind audiences; the
  interview row reads "no booking today" rather than a price.

**R-N2 held at this boundary:** `traj_wb9_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the eight checks exit 0; harnesses `media_wb9b_20260829.log`, `campaignai_wb9b_20260829.log`.
