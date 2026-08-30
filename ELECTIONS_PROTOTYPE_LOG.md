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

---

## W-B11 — Get-Out-The-Vote (§26): mobilization as volunteer-bound contacts, per region and per party (2026-08-29)

Files: `Assets/Scripts/Elections/GotvModel.cs` (`GotvOperation`, `GotvSpec`, `GotvModel`,
`RegionalMobilization`), `Assets/Editor/GotvHarness.cs`, `ElectionsData/sweden/turnout_history.md`
(SOURCED, PROVISIONAL); door-to-door in the AI campaign rewired to real contacts.

**Done-when.** *Mobilisation spending measurably moves turnout in targeted regions only* — S
door-knocks three valkretsar (80 000 doors each): Stockholms län 84.21 → 85.03 %, Skåne läns södra
→ 86.41 %, Gotlands län → 89.07 %; the other 26 at exactly base turnout bit for bit; the other
seven parties' supporters' turnout unchanged in all 29; S's vote share in Stockholm 30.80 → 31.47 %
with SD's votes unchanged to the vote. *National turnout inside historically plausible bounds* —
every party's whole chest and 60 000 volunteer-hours on the doors nationwide: 85.26 %, within
2002–2022's [80.11, 87.18] widened by two points; unlimited lifts for everyone everywhere: 100 %
and not a vote more (stated, not hidden). 10 of 10.

### Decisions taken and logged (R-N1)

- **Mobilization is per REGION and per PARTY, and it is contacts.** `mobilization = 50 + 50 ×
  (1 − exp(−(weighted contacts / eligible) / 0.5))`: 50 is `TurnoutModel`'s neutral, so an
  unworked region is at base by construction, and the curve is §35's, so no budget passes 100.
  `TurnoutModel` keeps no party term (its doc's rule stands); GOTV is party-specific through the
  mobilization INPUT — a party's supporters turn out at its own mobilization, everyone else's at
  50 — and a region's turnout is the preference-weighted mean.
- **Contacts cost money AND volunteer-hours, and volunteers bind:** 1 m kr with 0 hours knocks 0
  doors (asserted). §10's offices (W-B4) are what grow them.
- **Base turnout is SOURCED and uniform across valkretsar** (2022: 84.21 %); eligible per valkrets
  is DERIVED as 2018 valid votes ÷ 87.18 % (7 429 141 against the true 7 775 390) because
  per-valkrets eligible counts are not on disk — billed in `turnout_history.md` (val.se's
  `Röstberättigade` per valdistrikt). Engagement, enthusiasm and salience at the neutral 50 in the
  harness so GOTV is the only thing moving.
- **The historical series** (2002 80.11 · 2006 81.99 · 2010 84.63 · 2014 85.81 · 2018 87.18 · 2022
  84.21) is filed `[SOURCED] [PROVISIONAL]`: 2014/2018/2022 agree with the two files already on
  disk; 2002–2010 are the recorder's knowledge of val.se's series, to be read back (R-K9).
- **Door-to-door in the AI campaign now reaches the doors the volunteers can knock**
  (`GotvModel.Contacts(DoorKnocking, spend, volunteer-hours left today)`), for the world's response
  and the AI's estimate alike; each party's `Volunteers` (staging 800 each, equal by design)
  supplies 2 400 volunteer-hours a day. W-B3's 2 %-of-a-region placeholder no longer applies to
  door-to-door (rallies and town halls still draw on the region — W-B4's).
- **GOTV on election day itself is W-D1's:** `RegionalMobilization` is the state a campaign builds
  up to polling day; the AI run stops the day before, so its GOTV verbs (`GetOutTheVote`, legal
  only on election day) are not exercised there yet.

### [AUTHORED-DRAFT] values, one line each (calibration entry 10)

Per contact — phone banking 3 kr / 0.10 h / weight 0.5 · door knocking 5 kr / 0.25 h / 1.0 ·
transport 60 kr / 0.50 h / 3.0 · election-day reminders 1 kr / 0.02 h / 0.25 · `MobilizationScale
0.5` · staging volunteers 800 per party.

### C1's PEND lines — the clearance report the list asked for

W-B11 was named on `PEND 2c` (door-to-door's largest share) beside W-B4, and on `2a-iii/2a-iv`'s
grassroots half. **Cleared: none. Changed: one, honestly for the worse.** With doors counted as
volunteers can knock them, a door-to-door action reaches ~3 000 doors, and at W-B3's per-contact
persuasion weight (0.55) that is not worth five hours against a post to the party's whole
following — so no rational personality knocks doors (chaotic 20 %, the rest 0 %), and the
grassroots separation W-B9 produced (0.71 / 0.61, asserted as `2a-iv`) is gone (0.20 / 0.17).
**`2a-iv` goes back to PEND** with its true blockers: the ground game's SCALE (W-B4: offices grow
volunteers — 800 is a guess) and the persuasion a personal contact is worth (calibration entry 10;
the canvassing literature says far more per contact than a broadcast impression — billed). `2c`
stays PEND on the same two. Nothing was raised to keep the line green.

### Findings carried forward

1. **The grassroots separation was the placeholder's.** 16 000 doors an afternoon made
   door-knocking look worth it; 3 000 do not. The honest reach exposed that the model's persuasion
   per personal contact is the open question, not the count.
2. **One party's whole ground game moves the nation by a third of a point** (+0.32 pp; its own
   supporters +1.05) and a worked small valkrets by nearly five — the regional lever §10 promises
   exists, at these magnitudes.
3. **The eligible-per-valkrets gap** understates the electorate by 4.5 % (7.43 m derived against
   7.78 m actual); a data line, not a model line.

### Riders

- **W-D1** — election day runs `RegionalMobilization.RegionVotes` per valkrets with the campaign's
  accumulated contacts; the GOTV verbs become the AI's election-day plan.
- **W-B4** — offices grow volunteers; `PEND 2a-iv / 2c` re-measured there.
- **W-F4** — per-valkrets eligible counts and, with voter groups, per-group base turnout.
- **W-B3's weights** — the persuasion per personal contact, calibration entry 10.

**R-N2 held at this boundary:** `traj_wb11_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the eight checks exit 0; harnesses `gotv_wb11_20260829.log`, `campaignai_wb11_20260829.log`.

---

## W-D1 — election day (§27): every valkrets counted, noise on the `ElectionNoise` stream, 1/√n proven on the real 29 (2026-08-29)

Files: `Assets/Scripts/Elections/ElectionDay.cs`, `Assets/Editor/ElectionDayHarness.cs`;
`CampaignRun` now accumulates the campaign's door-knocking into a `RegionalMobilization` that its
`Result` exposes (`Gotv`, `RegionNames`) — the W-B11 rider, discharged.

**Done-when.** *The same seed reproduces a result exactly* — seed 777 on `ElectionNoise` twice:
digest `7b7ce512348e9941` both times, every regional vote count identical; seed 778 differs.
*400 replays show the noise matching its declared σ* — regional share σ 1.167 pp against the
declared 1.2 (re-normalisation's shrink, 0.97 of it); national σ **0.259 pp against 0.260
predicted** by σ/√N_eff, N_eff = 20.15 of the 29 valkretsar by eligible weight — the 1/√n
behaviour Day-1 measured on eight equal regions, now on the real, unequal ones, to a third of a
percent. 10 of 10.

### Decisions taken and logged (R-N1)

- **§27 per region is W-B11's `RegionVotes`:** eligible × preference × each party's supporters'
  turnout — the ground game lands exactly here and nowhere else; with σ = 0 the count IS the
  expected result to the vote, region by region (asserted).
- **Noise on the SHARES, votes from the shares** (`ApplyNoise` at Day-1's declared 1.2 pp, then
  × the region's votes cast, rounded to whole votes). National shares are the vote-weighted sum
  of regions, never a mean of regional shares — the trap the chain harness already guards.
- **`EffectiveRegions = 1 / Σ w²`** is the number the national σ divides by; stated in code so a
  future re-districting is a measurement, not a surprise.
- **One preference vector per region** (the 2022 vector repeated) until W-A2's per-region priors
  and W-F4's groups make them differ — the caller hands `ElectionDay.Count` a per-region array
  already, so that change is data.
- **Rounding to whole votes** makes a region's party votes sum to its votes cast only to ±4
  votes (one per party); asserted at that tolerance and printed (6 272 372 of 6 272 383).
- **Seats are W-D2's** — `SeatAllocation` is exact and waits for this result; nothing allocated here.

### Findings carried forward

1. **National uncertainty is a quarter of a point.** At the declared regional σ the nation moves
   ±0.26 pp per party per replay — inside the "good strategy matters, cannot be perfectly
   predicted" band §27 asks for, at these magnitudes; whether a quarter-point is *felt* as
   uncertainty is a play question (a calibration line under entry 1's neighbourhood, not a new
   entry: `RegionalNoiseSigmaPp` is Day-1's declared constant).
2. **The turnout the count reports (84.43 %) is W-B11's exactly** — the three worked valkretsar
   lift the nation 0.22 pp above base.

### Riders

- **W-D2** — `SeatAllocation` on `ElectionDay.Result` through the LIVE path (per-valkrets fixed
  seats, the adjustment seats, the thresholds), Sweden 2022 still seat-for-seat.
- **W-E6** — election night draws `Result.Regions` arriving in a seeded order.
- **W-A2 / W-F4** — per-region preference vectors replace the repeated national one.

**R-N2 held at this boundary:** `traj_wd1_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the eight checks exit 0; harnesses `electionday_wd1_20260829.log`, `campaignai_wd1_20260829.log`.

---

## W-D2 — vote-to-seat on the live path (§28): Sweden's own procedure, 2022 seat-for-seat (2026-08-29)

Files: `Assets/Scripts/Elections/SeatConversion.cs`, `Assets/Editor/SeatConversionHarness.cs`.
`SeatAllocation` (the divisor arithmetic, exact for five chambers since the overnight) is what it
calls; this file is the Swedish PROCEDURE around it — vallagen 14 kap. — which the backtest never
needed because a national totalfördelning gives the same party totals as the full procedure
whenever no seat is returned.

**Done-when.** *Sweden's 2022 returns reproduce seat-for-seat through the live path* — the exact
2022 national counts regionalised over the 29 valkretsar (by the 2018 per-valkrets distribution;
national sums exact to the vote), fed as an `ElectionDay.Result` into `SeatConversion.Sweden`:
**107 / 73 / 68 / 24 / 24 / 19 / 18 / 16**, fixed 105 / 69 / 67 / 17 / 23 / 10 / 11 / 8 (= 310) +
adjustment 2 / 4 / 1 / 7 / 1 / 9 / 7 / 8 (= 39), no seat returned. 12 of 12 (the 12 % rule, the
återföring branch firing, determinism, W-D1's counted election converting to 349).

### The procedure, as built (R-N1 calls inside it)

1. **Eligibility** — 4 % nationally, or 12 % in a valkrets for that valkrets's fixed seats only.
2. **310 fixed seats per valkrets** — the statute's "one seat per 310th part of the national
   eligible electorate, the remainder by largest surplus" (`FixedSeatsPerRegion`: Stockholms län
   39, Gotlands län 2), then the modified odd-number method (1.2, 3, 5, …) within each valkrets
   among the parties eligible there.
3. **Totalfördelning** — 349 over the nationally-eligible parties as one valkrets, the same
   divisors, seats held by 12 %-only parties deducted first.
4. **Återföring** — a party over its total gives back its lowest-comparison-number fixed seats
   first; each returned seat is re-allocated within its valkrets to the next comparison number
   among parties still under their totals (the 2018 reform's rule). ⚠ **The first synthetic for
   this exercised nothing:** fixed seats follow ELIGIBLE voters, so a party concentrated in one
   valkrets stays under its total (KD all in Stockholm: 12 fixed of 19). What makes fixed seats
   exceed a total is a valkrets where few OTHER votes are cast relative to its electorate — every
   other party's Stockholm vote cut by 70 % and KD's whole vote there: 24 fixed against 21
   entitled, **3 returned**, every party at its total, Stockholm still at its 39.
5. **39 adjustment seats** — each party's total minus its fixed seats, placed valkrets by valkrets
   where its next comparison number is highest.

- **The 12 % rule, exercised:** L at 35 % of Gotland and nothing elsewhere (0.25 % nationally)
  takes 1 of Gotland's 2 fixed seats and nothing else, no adjustment seat, 349 in all.
- **DERIVED and billed:** eligible per valkrets (2018 valid ÷ 87.18 %) and therefore the fixed seats
  per valkrets are `[DERIVED] [PROVISIONAL]`; the real 2022 per-valkrets seat table and val.se's
  per-valkrets eligible counts are billed for verification. The party TOTALS do not depend on
  either unless a seat is returned — which is why the live path is exact from a derived
  regionalisation.
- **Personal votes (§ 93's candidate ordering) are not modelled** — seats, not names.

### Findings carried forward

1. **The adjustment tier does real work for the small parties:** KD 9 of its 19 seats, L 8 of 16,
   MP 7 of 18, V 7 of 24 come from the 39 — the fixed tier under-represents them by the divisor's
   1.2 first step, exactly as designed.
2. **W-D1's counted election (seed 777, noise on) converts to S 108 / SD 73 / M 67 / V 24 / C 25 /
   KD 18 / MP 18 / L 16** — a quarter-point of noise moves three seats; §27's "cannot be perfectly
   predicted" is now visible in seats, not shares.

### Riders

- **W-E6 / W-E7** — election night draws `RegionSeats` filling per valkrets; the results screen
  reads `Seats`, `FixedSeatsWon`, `AdjustmentSeats`.
- **W-D3** — coalition arithmetic reads `Seats` (175 the majority line).
- **W-F1** — the real 2022 per-valkrets counts and seat table replace the derived regionalisation
  and verify the per-valkrets seat table.

**R-N2 held at this boundary:** `traj_wd2_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the eight checks exit 0; harnesses `seats_wd2_20260829.log`, `campaignai_wd2_20260829.log`.

---

## W-E2 — the campaign map: 29 valkretsar as polled, the swing index, and §36's gate drawn as absence (2026-08-29)

Files: `Assets/Scripts/Elections/SwingRegions.cs` (`SwingRegions`, `MapRegionReading`, `MapTile`,
`CampaignMapSnapshot`, `SwedenCartogram`), `Assets/Scripts/UI/GameController.CampaignMap.cs` (the
screen), the OnGUI branch in `GameController.cs`, the driver's `CaptureCampaignMap` +
`ReadValkretsVectors` pass; films `we2_campaign_<w>_e2_campaign_map_{unbought,regional,full}` at
1280 / 1600 / 1920 / 2560.

**Done-when.** *The map renders 29 constituencies* — the 5 × 10 cartogram carries all 29
valkretsar (the driver errors if the file gives any other count). *Uncertainty is visually
distinct from data* — an unbought valkrets is a hatched tile figured "?" with nothing behind it
(no shares, no leader, no index — `MapRegionReading.Measured` false), a bought one is shaded by
the player's polled share with its ± and framed by what the poll can say. *Buying polling visibly
sharpens it* — the same day filmed three times: nothing bought (29 hatched, the ledger empty and
the gate text naming the two offers and what each buys, ±10 / ±6 on the player's share by
`MarginOfErrorPp`); the regional breakdown (n = 2 400 / 29 = 82 per valkrets: 29 read, 11 swing
regions, **19 too close to call** — the lead inside its own ±); the full programme (n = 206 per
valkrets: 12 swing, **13 too close to call**). Six of the nineteen dashed frames become solid; the
figures' ± halve.

### Decisions taken and logged (R-N1)

- **The gate is ABSENCE, not blur.** §36 says the map must not tell the player where the race is
  close until they pay to find out. An averaged or rounded regional reading would be telling them
  anyway; an unbought valkrets therefore carries no reading at all, and the tile says "?" over the
  draft hatch — the established idiom for "not real yet" (`ui_hatch_draft`, the ledger rows' draft
  band), not a new sprite.
- **A reading is a Poll of the valkrets, or nothing** — `SwingRegions.FromPoll` derives leader,
  runner-up, gap, the gap's own error (the two ± combined in quadrature) and §25's index from the
  polled shares and nothing else; `TooCloseToCall` is `gap ≤ gapError`, so a 40.5 / 39.8 on a
  small sample is drawn as what it is — undecidable — with a dashed frame in the caution ink.
- **§25's index:** `100 × max(0, 1 − gap / 20)` — 100 at a tie, 0 at a 20-point lead.
  `[AUTHORED-DRAFT] FullScaleGapPp = 20` (the lead at which a valkrets stops being worth
  contesting); `[AUTHORED-DRAFT] CampaignMapSwingFrameIndex = 60` (the index at which a tile is
  framed bold). Both strikeable; the key on the sheet names them.
- **The per-valkrets sample is the national sample over 29.** A "regional breakdown" of n = 2 400
  affords 82 respondents per valkrets, and the honest ± at that size is ±10 on a 30 % share — which
  is why nineteen of twenty-nine leads are inside their error at that price. The screen does not
  pretend the breakdown buys more than it does; that IS the §21 trade.
- **The cartogram is a reading aid, not geography** — `SwedenCartogram.Layout`, hand-laid, north at
  the top, no borders, `[AUTHORED-DRAFT]`. A drawn map is a Track H Design line.
- **The truth polled is SOURCED per valkrets:** 2018's absolute counts for all eight parties
  (`valkrets_votes_2018.csv`) as each valkrets's preference vector — the only eight-party
  per-valkrets vector on disk (the 2022 table carries five; W-F1 bills the rest). Shares are of the
  eight-party vote; weights are each valkrets's share of the national valid vote.
- **The shade is the player's own polled share**, scaled to its strongest valkrets, so the darkest
  tile is where the party is strongest; the frames are about the RACE (the index), not the party.
  A leader-coloured choropleth would need eight party inks the palette does not have
  (`PoliSimTheme.Party` keys four archetypes) — a Design line, not an invention here.
- **The horse-race poll on the AI's view now has a regional companion it could read** — the
  professional's "targets swing voters" rider (W-C1) is NOT discharged here: local actions are
  worthless in the placeholder environment (W-B4), so wiring the index into the AI's importance
  would change nothing measurable. It waits for W-B4 with the rest.

### Fix-forward found by this film

**P-A1's "1280" campaign film was not at 1280.** `pa_campaign_1280_*` measures 1918 × 953 — the
first GUI launch after a killed Editor kept the previous session's window size (the environment
quirk on record), while the sweep and the three other widths were at their stated sizes. The true
1280 run came with this item's film and found what the 1918 one could not: P-A1's rewritten
caption *HOW A MESSAGE BECOMES VOTES — EVERY STAGE MULTIPLIES* is 1.1 px too wide for the action
screen's 250-board-px plate at the 8 px floor (249.4 in 248.3). Fixed here — *HOW A MESSAGE BECOMES
VOTES — EACH STAGE MULTIPLIES* — and the 1280 campaign family re-filmed on the fixed code
(`we2_campaign_1280_*`, 16 frames, 0 overflows). §61's film line is corrected to say so.

### Riders

- **W-B4** — the AI's local targeting reads `SwingIndex` once local actions can matter.
- **W-E6 / W-E7** — the same cartogram draws results arriving and the final count.
- **W-F1** — the 2022 eight-party per-valkrets counts replace 2018 as the map's truth.
- **W-H4** — a drawn valkrets map and eight party inks are Design lines.

**R-N2 held at this boundary:** `traj_we2_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the nine checks exit 0 (`MetaTextCheck` clean with the map screen); `campaignai_we2_20260829.log`.

---

## W-B7 — debates (§15): exchanges from attributes, preparation, ownership, the clash and one seeded draw; the result a coverage and momentum shock, never a share (2026-08-29)

Files: `Assets/Scripts/Elections/Debates.cs` (`DebateMove`, `DebatePreparation`, `DebateExchange`,
`DebateResult`, `Debates`), `Assets/Editor/DebateHarness.cs`, `SimulationRandom.Stream.Debate = 9`
APPENDED; the AI campaign holds two debates (`CampaignRun`: `DebateDays`, `DebatePlanFor`, a
`Candidate` per party).

**Done-when.** *The same seed and choices reproduce a debate exactly* — seed 777 on the `Debate`
stream twice: six exchanges bit-identical (points, events, topics); seed 778 differs; the same
seed with a different plan differs. *Performance moves media coverage and momentum rather than
vote share directly* — structurally (`DebateResult` has no share / vote / preference / party
member, by reflection) and behaviourally: applied to `MediaCoverage` and `MomentumTracker` the
result moves both (+0.66 coverage, ±2.17 pp momentum), and **the preference recomputed after the
debate is bit-identical** — a debate has no route to a vote share; the polls move because
momentum shifts where the race APPEARS to be. 14 of 14.

### Decisions taken and logged (R-N1)

- **A debate is a sequence of exchanges**, each a pair of §15's seven moves resolved as
  `skill × prepared × ownership × clash + event`: the move's §16 attribute blend, §35's curve on
  preparation hours between a 0.7 floor and 1, the topic's ownership between 0.8 and 1.2, the
  move-pair table, and one Gaussian draw at σ 4 index points. The performance index is the mean;
  **the margin is the difference and both shocks scale with it** — a close debate makes little news
  and moves nothing, a rout does both, bounded by the index's range.
- **The clash table is §15's verbs as consequences:** an attack into `IgnoreAttack` is wasted
  (×0.6), into a `Counterattack` dangerous (×0.8 against ×1.25), into `DefendPolicy` blunted (×0.9,
  the defence ×1.15); a counterattack with nothing to counter is empty (×0.7); `ChangeSubject`
  hands the next topic to the changer and lands against statistics (×1.1).
- **§22's worked example is the momentum rate's anchor:** `MomentumPpPerMarginPoint = 0.20`, so a
  10-point rout is +2.0 pp — the spec's "strong debate" — decaying to 0.5 pp after two weeks on the
  tracker's own half-life. `CoveragePerMarginPoint = 0.10`: a 10-point rout is a full news day.
- **The AI campaign holds two debates** (days 20 and 41) between the two parties leading the
  PUBLISHED poll — chosen from the tracker, never the truth — each on its personality's plan
  (populist: appeal, attack, change the subject; professional: statistics, defend, counter;
  establishment: defend, statistics, ignore; grassroots: appeal, defend, statistics; chaotic:
  attack, counter, change) on its own ground (its most salient contested issue) with a fixed 8
  hours' preparation (the AI does not plan hours — W-B5's staff would). Seed 777: day 20 S v M,
  margin −2.8 (M), coverage +0.28, ±0.55 pp; day 41 S v SD, margin −9.5 (SD), +0.95, ±1.89 pp. The
  C1 harness asserts the two were held and shocked both; its digest moves; every C1 line holds.
- **Candidates are `[AUTHORED-DRAFT]` per personality, unnamed** (§16's attributes as the
  personality's emphasis — the orator populist, the wonk establishment — game fiction; W-F6 labels
  real leaders). The harness's two are the spec's own example shape: charisma 90 / knowledge 45
  against knowledge 92 / charisma 45.
- **Ownership is the party's true issue-match on the topic** (the run is the world); a player's
  debate would price it through the same polled measurement the action screen uses.

### [AUTHORED-DRAFT] values, one line each (calibration entry 11)

`PreparationScale 12 h` · `PreparationFloor 0.7` · `OwnershipFloor 0.8 / OwnershipSpan 0.4` ·
`EventSigma 4` · `CoveragePerMarginPoint 0.10` · `MomentumPpPerMarginPoint 0.20` · the seven
attribute blends · the clash table · `DebateExchanges 6` · `DebatePreparationHours 8` · the five
plans · the five candidate profiles.

### Findings carried forward

1. **The orator wins the emotional debate 400 of 400** against the wonk and the prepared twin
   beats the unprepared 400 of 400 — at σ 4 the event term is small against a 20-point skill gap;
   whether upsets should be possible is a play question (entry 11).
2. **`ChangeSubject` at 100.0 twice** in the seed-777 debate — the orator changing the subject on
   home ground hits the index ceiling; the clamp at 100 hides how far over it went. A ceiling that
   never binds would be honest; recorded, not tuned.
3. **A test that measured nothing, caught:** the first ownership test gave the twin a different
   topic list, which made the exchanges alternate between both grounds and the pair exactly
   symmetric (199 of 400). The corrected test holds the topic and varies only ownership (400 of 400).

### Riders

- **W-E5** — the debate screen draws `DebateExchange[]` as the running state and `DebateResult`
  as the verdict; the pre-debate choices are `DebatePreparation`.
- **W-B8** — a scandal is a `MediaCoverage.AddShock` with a credibility cost, the same seam.
- **W-B5** — preparation hours become a staff decision.
- **W-C2** — the AI's plan reacts to the opponent's last move (today it is a fixed cycle).

**R-N2 held at this boundary:** `traj_wb7_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the nine checks exit 0; harnesses `debate_wb7_20260829.log`, `campaignai_wb7_20260829.log`.

---

## W-B8 — scandals (§17): a lifecycle, seven responses with distinct outcome distributions, damage on two stocks at two speeds, nothing scripted as game over (2026-08-29)

Files: `Assets/Scripts/Elections/Scandals.cs` (`ScandalKind`, `ScandalSeverity`, `ScandalResponse`,
`Scandal`, `ScandalOutcome`, `Scandals`), `Assets/Editor/ScandalHarness.cs`,
`SimulationRandom.Stream.Scandal = 10` APPENDED; the AI campaign carries a live credibility per party
and answers a staged scandal by personality (`CampaignRun`: `Setup.Scandals`,
`ScandalResponseFor`).

**Done-when.** *The lifecycle runs deterministically under a seed* — seed 777, a MAJOR corruption
scandal at evidence 0.5, `Deny`: every day's coverage, the momentum shock and the credibility cost
identical twice; across 200 seeds 75 denials are caught out and 125 survive (the evidence surfaces
on some seeds, not all). *Each response has a measured DISTINCT outcome distribution* — over 400
seeds per response the seven differ pairwise in mean or spread by at least 0.90 (the closest pair
Explain / SacrificeStaffMember); §17's two sentences hold as measurements. *Nothing scripted as
game over* — `ScandalOutcome` has no member that could end anything (by reflection), a
resignation replaces the candidate and the campaign goes on, and a catastrophic scandal on the
worst response with certain evidence costs at most 45 % of credibility — a large cost, not an
ending. 15 of 15.

**The table, measured (a MAJOR corruption scandal, evidence 0.5, 400 seeds; damage = 100 ×
credibility cost − momentum pp):** Deny 11.5 ± 9.0 (152 of 400 caught out) · Apologize 9.9 ± 0 ·
Explain 10.8 ± 0 · AttackSource 12.3 ± 4.0 (181 caught) · Ignore 13.2 ± 0 · Resign 7.2 ± 0 ·
SacrificeStaffMember 11.7 ± 0. The apology: the largest momentum decline of the responses that keep
the candidate (−3.9 pp) and among the smallest lasting costs (0.060) — *"a transparent apology may
reduce long-term damage but cause a short-term polling decline."* The denial: the smallest immediate
cost (−1.07 pp) and the widest spread (9.0); against STRONG evidence (0.95) the worst response on
average (16.0 against the next worst 14.4), against WEAK (0.05) the best (5.3 against 7.2) — *"a
denial can work if evidence is weak but become catastrophic if evidence later appears."*

### Decisions taken and logged (R-N1)

- **Damage lands on two stocks at two speeds and on nothing else.** A momentum shock (§22 — the
  short-term polling decline, decays on its own half-life), a coverage shock per day of the story
  (§13 — the media system decays it on the news cycle), and a CREDIBILITY cost — the lasting one,
  on the stock §42's chain multiplies by. Applied, the preference recomputed from the same
  compatibility is bit-identical; the same rally then persuades less in exact proportion to the
  credibility lost (382 → 350, 91.6 %). A scandal reaches the vote only through the chain.
- **The evidence is a hidden variable (§36):** the party responds on `EvidenceAsSeen` — the truth
  plus a uniform ± 0.25 — never on the truth. The AI's rule: deny when the evidence LOOKS weak
  (below 0.3 as seen), otherwise by personality (the professional explains, the establishment and
  the grassroots party apologise, the populist attacks the source, the chaotic denies).
- **Escalation is the catch-out:** `Deny` and `AttackSource` are exposed; each aftermath day the
  evidence surfaces with probability `evidence × 0.15`; caught, the credibility cost multiplies
  (×6 for the denial, ×2 for the attack), the momentum cost ×1.5, and the story restarts.
  ⚠ The first table had the denial's escalation at ×3, and a caught denial was then no worse than
  simply ignoring the story — §17's "catastrophic" not realised. Set to ×6 at design time so a
  caught denial is the worst outcome on the table; recorded as the shape the spec's own sentence
  demands, not as a tuning against play.
- **A staff sacrifice for a scandal no staff member could carry reads as cynical** (×1.6 on the
  lasting cost): an offensive statement 0.096 against a finance violation's 0.060.
- **The AI campaign stages one scandal** (day 30, the leading party, MAJOR corruption at evidence
  0.5); S (professional) sees 0.59 and explains; −2.4 pp momentum, six days of coverage, credibility
  0.600 → 0.550 on its live figure and nowhere else; the campaign runs to the end. Dynamic generation
  — a probability per day from §36's hidden variables — is a later item (the gap table's §17 row
  says so); today the harness stages them.
- **Distinct distributions, not distinct means:** a denial and a resignation can average alike (a
  gamble and a certainty) and still be different things to choose between; the assertion compares
  mean OR spread.

### [AUTHORED-DRAFT] values, one line each (calibration entry 12)

Base by severity (momentum pp / credibility / days / coverage): Minor 0.5 / 0.02 / 2 / 0.15 ·
Moderate 1.5 / 0.06 / 4 / 0.40 · Major 3.0 / 0.12 / 7 / 0.80 · Catastrophic 5.0 / 0.25 / 12 / 1.50 ·
the response table (momentum × / credibility × / days × / exposed / escalation / coverage ×): Deny
0.3 / 0.3 / 1.0 / yes / 6 / 1.0 · Apologize 1.3 / 0.5 / 0.6 / no / – / 0.8 · Explain 0.8 / 0.7 / 0.9
/ no / – / 0.9 · AttackSource 0.5 / 0.6 / 1.3 / yes / 2 / 1.5 · Ignore 0.8 / 0.9 / 1.5 / no / – / 1.1
· Resign 1.6 / 0.2 / 0.7 / no / – / 1.4 · SacrificeStaffMember 0.7 / 0.5 / 0.8 / no / – / 1.0 ·
`SurfaceRatePerDay 0.15` · `EvidenceEstimateError 0.25` · `CynicalSacrificeMultiplier 1.6` · the
AI's deny-threshold 0.3 and its responses by personality · the staged scandal.

### Findings carried forward

1. **Ignoring is never right at these numbers** (13.2, the worst mean of the seven): a story left
   alone runs half again as long. Whether a quiet week should sometimes be the right call is a
   play question (entry 12).
2. **The resignation is cheapest and no one takes it** — the AI never resigns; W-C2's reactivity
   and a candidate the player cares about (W-F6) are what would make it a real choice.

### Riders

- **W-C2** — the AI weighs the evidence it sees against the response table rather than following a
  personality rule.
- **W-E5 / W-E1** — the scandal's day on the screens: the story, the seven responses, the evidence
  as seen (a range, never a figure).
- **§36 / W-B5** — dynamic generation with a scandal-resistance draw per candidate (§16's
  attribute is on the profile and unread).
- **W-B7** — the same shock seam; a debate the day after a scandal is where `ScandalResistance`
  and `Integrity` should bite.

**R-N2 held at this boundary:** `traj_wb8_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the nine checks exit 0; harnesses `scandal_wb8_20260829.log`, `campaignai_wb8_20260829.log`.

---

## W-A4 — tactical voting, threshold form (§23): the belief from the published poll, lending where the race is in play, abandonment where it is hopeless (2026-08-29)

Files: `Assets/Scripts/Elections/TacticalVoting.cs` (`TacticalSpec`, `TacticalFlow`,
`TacticalResult`, `TacticalVoting.Apply/ApplyToRegions/NormalCdf`), `Assets/Editor/TacticalVotingHarness.cs`,
`ElectionsData/sweden/psu_2018_2022.md` (SOURCED, new). Pure and unwired (R-N2); no stream
appended.

**Done-when.** *A party polling 3.5–4.5 % shows measurable support inflow* — on the 2022
staging with L set to 3.5 / 3.75 / 4.0 / 4.25 / 4.5 % it gains +1.18 / +1.18 / +1.00 / +0.72 /
+0.43 pp net from its bloc; at 6 % nothing moves; at 1.5 % (§23's own example, no realistic
chance) it LOSES 0.62 pp net to its bloc (0.72 out, 0.09 in). *The effect vanishes in the absence
of a threshold* — threshold 0 returns the input to the bit with no flows; so does awareness 0;
a party outside any bloc (SD in 2018) neither lends nor receives; mass is conserved to 1e-15;
29 regions reading the one national poll shift exactly as the nation does. *2022's near-threshold
behaviour reproduced no worse than without the layer* — SCB's May 2022 PSU as the poll and as the
preference, Valmyndigheten's September count as the answer: the near-threshold error (KD, MP, L)
falls 3.12 → 1.00 pp and the whole-vector L1 error 13.27 → 10.08 pp; L 3.47 → 4.63 (count 4.68),
MP 3.37 → 4.46 (count 5.16); KD, polling clear, needs nothing and only lends (0.15 pp). 12 of 12.

**The data.** `psu_2018_2022.md`: Statistics Sweden's Partisympatiundersökningen via the PxWeb
API (`Vid10` "val idag" with ± at 95 %, and `Rostningssympati170`, vote intention against
"best party") for May 2018 and May 2022 — the only official probability-sample pre-election
figure. In each election year the party polling below 4 % in May finished above it in
September (KD 2018 3.0 → 6.32; MP and L 2022 3.3 → 5.08 and 3.4 → 4.61), each time with the
bloc's largest partner falling. The May cross-tab shows the lending still small then (M → L
0.7 % of M's sympathisers, inside its own margin) — a lower bound on the final week's, which
no PSU observes; a final-week poll of record is billed.

### Decisions taken and logged (R-N1)

- **The voter's belief is the poll, widened.** P(clear) = Φ((polled − T) / σ) with
  σ² = (MoE / 1.96)² + `BeliefSigmaPp`² — §20's rule that polls miss by more than their
  margin, so no sample size removes the doubt. `BeliefSigmaPp` = 1.0 pp was fixed BEFORE the
  2022 run from the worklist's own window (3.5–4.5 % must show inflow, so the doubt spans at
  least ±0.5 pp); it is [AUTHORED-DRAFT] and strikeable (calibration entry 13).
- **Two behaviours, one variable.** Where the outcome is in play the bloc LENDS: each partner's
  aware, willing voters (awareness × `MaxLendFraction` 0.15 × affinity) move to the threatened
  party, up to what it NEEDS to stand one belief-sigma clear, weighted by 4P(1 − P) — the
  pivotality of a vote. Where it is hopeless the party's OWN aware voters ABANDON it for the
  bloc, weighted by ((1 − P)(1 − 2P))² below even odds and by nothing above. A party polling
  clear needs nothing and loses nothing. The two forms were chosen with the PSU figures on the
  table; the constants were not moved after the first run, and the record says so — the 2022
  test is in-sample for the FORM (a 2026 May PSU against the 2026 count would be the first
  out-of-sample test).
- **Voter ideology as affinity:** 1 − |Δlrgen| / 10 within the bloc (CHES 2024, SOURCED), 0
  outside — M lends to L more readily (0.92) than SD does (0.82); C to MP least on the left
  (0.68).
- **Blocs are the harness's staging, not a catalog:** 2022's two sides (M/KD/L/SD; S/V/C/MP)
  and 2018's (the Alliance M/C/KD/L; S/V/MP; SD outside). A bloc catalog belongs to W-D4
  (coalitions) or the Track H ask.
- **Threshold form only.** §23's other forms — a district's two-horse race under FPTP, a
  runoff — are not this item; the layer is the identity without a national threshold, asserted.
- **Wiring:** the layer takes the last PUBLISHED poll (`Poll` shares and ±) and never the truth;
  the seam is the count (`ElectionDay.Count` takes the shifted regional vectors). Not wired
  (R-N2); W-G1 wires it between the final tracker and the count.

### [AUTHORED-DRAFT] values, one line each (calibration entry 13)

`BeliefSigmaPp` 1.0 pp · `MaxLendFraction` 0.15 · awareness 0.5 (the harness's staging; W-F4's
groups would carry it per group) · the weights 4P(1 − P) and ((1 − P)(1 − 2P))² · the need
target T + one belief-sigma · the blocs as staged.

### Findings carried forward

1. **The lending overshoots at even odds:** L at 4.0 % goes to 5.0 % (the full need at full
   pivotality) — more than L at 3.75 % ends with. Whether a bloc lends to the target or to the
   threshold is a play question; the target is strikeable.
2. **2018's KD case is a quarter reproduced** (3.09 → 3.84 against 6.42): four months of
   campaign and a May poll far from the day; the layer is not the whole of KD's 2018.
3. **The lenders pay where the count says they gained:** SD 2022 lent 0.48 pp and finished
   +3.5 pp above its May poll — the error on SD grows (3.51 → 3.94) even as the vector's
   improves. The layer is not a forecast of the campaign; it is the last week's switch.

### Riders

- **W-G1** — wire between the final published tracker and the count; the AI's blind parties
  see nothing (no poll → no belief → the layer must not run on truth).
- **W-E1 / W-E5** — a "stödröst" reading on the polling screen: which partner is in danger,
  as a range, never the layer's figure.
- **W-F4** — awareness per voter group.
- **Track H / DATA_BILL** — a final-week poll of record for 2018 and 2022 (the newspapers'
  commissioned polls or the SVT/Valu exit poll) so the last week's switch can be measured
  rather than bounded from May.

**R-N2 held at this boundary:** `traj_wa4_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the nine checks exit 0; harness `tactical_wa4b_20260829.log` (12 of 12 — the bar's first run failed one mis-specified assertion, 3d, which asserted KD "receives nothing" when L's leavers bring it 0.02 pp; the assertion was corrected to what the design says and the harness re-run).

---

## W-B4 — campaign offices (§10): organisation as local reach, volunteers recruited not bought, a daily operation into the ground game, maintenance paid or starved (2026-08-30)

Files: `Assets/Scripts/Elections/CampaignOffices.cs` (`CampaignOffices`, `CampaignOffice`,
`OfficeNetwork`), `Assets/Editor/CampaignOfficesHarness.cs`; the AI campaign carries one network
per party (`CampaignRun`: `PartySetup.Offices/OfficeOperationsPerDay`, the offices' day, local
audience by influence, office hours on the door-to-door ceiling, `PartyLedger.Office*`,
`Result.Offices`); `RegionAudience.VolunteerHours` (`CampaignAi`); the C1 harness stages an office
plan per personality and asserts the offices ran (1h). No stream appended.

**Done-when.** *Offices measurably change regional door-to-door reach* — the office region has
450 volunteer-hours a day a region without has none of; the same 50 000 kr door-to-door action
knocks 2 400 doors on headquarters' 200 volunteers and 4 200 with the office's hours; the office's
own daily operation knocks 81 900 doors over 60 days in its region and none anywhere else; a
rally's local audience with a full office is four times a visit's (934 883 against 233 721 of
Stockholms län). *And GOTV* — the office region's mobilisation ends at 58.0 against the untouched
50 everywhere else; the party's turnout there 87.18 → 89.98 %, +7 859 votes. *Concentration in
few regions beats spreading thin in a measured scenario* — the same money, three offices in the
three largest valkretsar against ten spread thin: at 0.9 / 1.5 / 2.4 M kr three offices mobilise
4 740 / 14 248 / 22 534 votes against 0 / 0 / 4 087; asserted at the prototype's ground budget
(1.5 M of a 2.4 M war chest). **Measured, not asserted: spreading first wins at 4 M kr** (31 536
against 22 534) — the fixed costs against §35's concavity. The economics: a second office the party
cannot afford is not opened and nothing is paid; when the money runs out the office starves (six
starved days of ten, influence 0.00, 0 kr left of 111 000, nothing spent that was not there). 11 of
11.

**In the AI campaign** (seed 777, every C1 line holds; 1h added): every party opened its staged
offices, paid for them day by day and recruited them to capacity — S 3 offices 910 000 kr 59 200
doors; SD 4 / 1 125 000 / 70 200; M 2 / 636 600 / 42 520; V 6 / 1 665 000 / 103 000; C 1 /
278 300 / 17 260; MP 6 / 1 665 000 / 103 000; L 3 / 910 000 / 59 200.

### C1's PEND lines, re-measured with offices (reported per the standing order)

- **2a-iv CLEARED** — the grassroots personality's mix differs from both media personalities'
  (prof/grass 0.490, est/grass 0.450 ≥ 0.30). Honestly stated: it separates by its RALLIES —
  six offices make six full regions its local audience — not by door-knocking; the doors are
  knocked by the offices' own operations, outside the action mix the line measures. Converted
  from PEND to an assertion.
- **2a-ii still PEND** (populist min 0.274): its four staged offices give its rallies four full
  regions, the grassroots party's six give more. Blocker re-labelled W-B5/W-C2 — where a party
  sites its offices is the staged plan's.
- **2b still PEND** (rally + social: pop 50 %, grass 78 %, est 57 %) — the same cause; re-labelled
  W-B5/W-C2.
- **2c still PEND** (door-to-door share: grass 0 %, chaos 19 %) — a door-to-door ACTION at 15 000 kr
  for 3 000 doors is still not worth its hours to any rational personality; the ground game's doors
  are the offices'. Re-labelled calibration entry 10.
- 2a-iii, 2d, 2e, 2e-ii unchanged (W-B5's budget plan). **8 → 7 PEND.**

### Decisions taken and logged (R-N1)

- **Organisation is what local reach was pretending to be.** W-B3's placeholder gave every rally
  the whole region wherever it was held — an office everywhere at full strength. Now the local
  audience is the electorate × (`VisitFraction` 0.25 + 0.75 × influence): a visit with no
  organisation draws a quarter of a full office's. Where there is no office, local reach FELL.
- **Influence is recruited, not bought:** volunteers over capacity, +5 a day to 150 — half at 15
  days, full at 30; an office opened late is worth less; a starved office loses 0.10 a day.
- **§10's five provisions land as:** local organisation = influence; volunteer recruitment = the
  recruit rate; door-to-door = the office's hours on the region's ceiling AND its own daily
  operation (`GotvOperation.DoorKnocking`, funded by `OperationsPerDay`); election-day turnout =
  those contacts in `RegionalMobilization` (W-B11 → W-D1); local polling = an office region
  counts as polled at `LocalPollSampleSize` 300 — a RIDER to W-E2's snapshot, not wired here.
- **§10's five attributes:** `OpenCost` 100 000 kr, `StaffCapacity` 3 (W-B5 fills it),
  `VolunteerCapacity` 150, influence, `MaintenancePerDay` 2 000 — paid every day or the office
  starves; nothing is spent the party does not have.
- **The office plan is STAGED per personality** ([AUTHORED-DRAFT], §32's ground-game descriptions:
  grassroots 6, populist 4, professional 3, establishment 2, chaotic 1, each in the largest
  valkretsar; operations 2 000 kr a day each). Siting by swing (W-E2's index) is W-B5's plan and
  W-C2's reactivity; the harness says so.
- **Concentration is an economics, not a rule:** fixed costs per office against §35's concave
  mobilisation; the harness measures the crossover (4 M kr) rather than asserting a side.

### [AUTHORED-DRAFT] values, one line each (calibration entry 14)

`OpenCost` 100 000 · `MaintenancePerDay` 2 000 · `StaffCapacity` 3 · `VolunteerCapacity` 150 ·
`RecruitPerDay` 5 · `VisitFraction` 0.25 · `StarvationPerDay` 0.10 · `LocalPollSampleSize` 300 ·
the staged plan (6/4/3/2/1) and 2 000 kr a day of operations · the harness's 200 headquarters
volunteers and 1.5 M ground budget.

### Findings carried forward

1. **A concentrated network saturates on its volunteers:** three offices plateau at 22 534 votes
   from 2.4 M kr up — 450 h a day is 1 800 doors an office, and money past that is unspent. The
   volunteer capacity, not the money, is the ceiling of a small network (entry 14).
2. **Ten offices thin at a prototype budget mobilise nothing:** 10 × 220 000 kr of fixed cost
   exceeds 1.5 M — the opening alone starves the operations. §10's "concentrate in a few swing
   regions" is what the economics say at this scale.
3. **The AI's door-to-door action is still dead** (2c): with offices knocking 400 doors a day each
   on their own, the ACTION's price (15 000 kr for 3 000 doors, 5 hours) buys nothing a rally
   does not — calibration entry 10's question, sharpened.

### Riders

- **W-B5** — staff fill `StaffCapacity`; the field organiser raises an office's recruit rate or
  capacity; the budget plan sites offices.
- **W-C2** — the AI opens and closes offices on the published swing index, not on day 0.
- **W-E2** — an office region as a bought reading at `LocalPollSampleSize`.
- **W-E1 / W-E5** — the office as a screen object: cost, capacity, influence as a range.
- **W-D1** — election day reads the offices' contacts already (through `Result.Gotv`).

**R-N2 held at this boundary:** `traj_wb4_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the nine checks exit 0; harnesses `offices_wb4b_20260830.log` (11 of 11), `campaignai_wb4b_20260830.log` (all assertions pass, 1h added, 2a-iv cleared, 7 PEND).

---

## W-B5 — staff (§9, prototype depth): five roles, a salary a day on the ledger, a bonus on the action the role touches, the manager's budget plan; §37 deferred (2026-08-30)

Files: `Assets/Scripts/Elections/CampaignStaff.cs` (`StaffRole`, `CampaignStaffMember`, `BudgetPlan`,
`CampaignStaff`, `StaffRoster`), `Assets/Editor/CampaignStaffHarness.cs`; the AI campaign carries one
roster per party (`CampaignRun`: `PartySetup.Staff/TelevisionBuys`, payday before the actions, the
plan's saving from the day's release, the advisor's and strategist's multipliers on the audiences
the AI is handed, the pollster's larger sample, the fund paying a television buy first,
`PartyLedger.Staff*`, `Result.Staff`); `AiView.TelevisionFund` (spendable on television and on
nothing else); `OfficeNetwork.Day(... scale)` for the field organizer; the Campaign HQ screen's
ORGANISATION ledger draws a PAYROLL row (`CampaignSnapshot.StaffMember.SalaryPerDay`, the driver's
staging salaried). No stream appended.

**Done-when.** *Hiring changes the relevant action's effectiveness* — the media advisor: an
interview persuades 9 720 → 11 664 (×1.20), a rally to the bit the same; the digital strategist: a
post reaches 75 854 → 94 818, an interview the same; the pollster: the party's own poll 1 200 →
1 800 respondents at the same 120 000 kr, ± 2.59 → 2.12 pp at 30 %; the field organizer: an office
holds 225 volunteers not 150; the campaign manager: the plan holds the 500 000 kr television money
on day 24 of an even 40 000 kr-a-day release, which a party without a manager never has (no plan,
no fund); the buy is paid from the fund first. With all five hired, the actions no role touches
(rally, town hall, door-to-door, television) reach exactly what they did. *The payroll appears in
the resource ledger* — five on the roster is 9 000 kr a day, paid from the party's money to the
krona; a party that cannot pay everyone pays whom it can and the unpaid give nothing that day;
`PartyLedger.StaffMoney` carries it in the AI campaign; the HQ screen's ORGANISATION ledger carries
a PAYROLL row, filmed at four widths. *§37's progression is deferred and the deferral recorded* —
`CampaignStaffMember` has no experience, level, speciality or growth member (by reflection). 10 of
10.

**In the AI campaign** (seed 777, every C1 line holds; 1i added): the staged hires stand and the
payroll is on the ledger to the krona with the unpaid days counted — S 2 hired 183 600 kr (10
unpaid staff-days), TV 1 of 1 planned; SD 133 200 (38 unpaid), 1 of 1; M 190 800 (6), 2 of 2; V 1
hired 79 200 (12); C none; KD as M; MP as V; L as S.

### C1's PEND lines, re-measured with staff (reported per the standing order)

- **2a-ii CLEARED** — the populist's mix differs from every other's (min 0.419): its digital
  strategist and its manager's one television buy.
- **2d CLEARED** — the grassroots party's advertising 0 % of spend against the professional's
  30 % and the establishment's 40 %: the others' managers plan television, the grassroots party
  plans none.
- **2e-ii CLEARED** — the establishment buys the most television (2 against 1, 1, 0, 0): its
  manager's plan holds two buys and it makes them. Honestly stated: the count is the STAGED plan's
  — that is what a budget plan is (calibration entry 15).
- **2a-iii still PEND** (prof/est 0.061): two rational planners on equal money converge on the
  same schedule; re-labelled W-C2 / W-F5.
- **2e still PEND** (television + interview: pop 54 %, est 32 %): television is bought by plan
  now, but the interview half is the media's — they book the newsworthy and the populist makes
  more news; re-labelled W-B9's interest → W-C2 / W-F5.
- 2b, 2c unchanged. **7 → 4 PEND.**

### Decisions taken and logged (R-N1)

- **A hire is a multiplier on the actions its role touches, a salary a day, and nothing else** —
  except the manager, whose effect is a DECISION RULE (the plan), because that is what a manager
  is. W-B9's finding stands verbatim in `CampaignAi.Evaluate`: no greedy saving rule; the plan is
  the manager's.
- **The plan is television only, at the prototype's depth:** `ManagerFundShare` 0.5 of each day's
  release set aside while a planned buy is short; the fund is spendable on television and on
  nothing else (`AiView.TelevisionFund`); the fund pays first. A plan over every fixed cost
  (offices, payroll) is the fuller job — finding 2.
- **An unpaid member gives nothing that day and is not dismissed;** the ledger counts the unpaid
  days rather than hiding them.
- **Five roles, not nine:** the worklist's roster; §9's policy advisor, fundraiser, press
  secretary and volunteer coordinator wait with §37, recorded in the enum's doc.
- **The staging** ([AUTHORED-DRAFT], §32): the professional a manager and a pollster (1 buy); the
  populist a manager and a digital strategist (1); the establishment a manager and a media advisor
  (2); the grassroots party a field organizer; the chaotic nobody.
- **The HQ ledger's payroll row** is the sum of the staged salaries (the driver's three filled posts
  at `SalaryPerDay`, the vacant pollster at 0); the screen's staff rows and office rows are as
  they were; filmed at 1280 / 1600 / 1920 / 2560, edge-checked.

### [AUTHORED-DRAFT] values, one line each (calibration entry 15)

`SalaryPerDay` 1 800 · `MediaAdvisorReach` 1.20 · `DigitalReach` 1.25 · `PollsterSample` 1.5 ·
`FieldOrganizerScale` 1.5 · `ManagerFundShare` 0.5 · the staged hires and buys per personality.

### Findings carried forward

1. **The field organizer is capacity, not speed:** ×1.5 on the recruit rate and ×1.5 on the
   capacity nearly cancel — full strength on day 29 instead of 30, but at 225 volunteers. If the
   role should make an office FAST, the two multipliers must differ.
2. **The parties go broke before polling day:** offices, their operations and the payroll are
   fixed daily costs the pace does not see; the front-loaded populist leaves 38 of 120 staff-days
   unpaid, the even-paced professional 10 of 120. The manager's plan covers television only; a
   plan over ALL fixed costs — pay the organisation first, release the rest — is what §9's
   "campaign manager" means in full (W-C2).
3. **Equal money with a plan makes the two rational personalities the same campaign** (0.061
   apart): what separates a professional from an establishment party is money and reaction (W-F5,
   W-C2), not an affinity table.

### Riders

- **W-C2** — the manager's plan over every fixed cost, reacting to the published race; the AI
  hires and dismisses.
- **W-E1** — the staff rows show the role's bonus and its salary (the payroll row lands today);
  hiring as a player action.
- **§37 / a later item** — progression between elections; the four deferred roles.
- **W-F5** — unequal war chests.

**R-N2 held at this boundary:** `traj_wb5_*` ≡ `traj_run_*` 6/6 by SHA-256, zero ATTRIB; the nine checks exit 0; harnesses `staff_wb5b_20260830.log` (10 of 10), `campaignai_wb5c_20260830.log` (all assertions pass, 1i added, 2a-ii / 2d / 2e-ii cleared, 4 PEND); the HQ film `wb5_hq_{1280,1600,1920,2560}_e1_campaign_hq_*.png`, `ScreenEdgeCheck` 64 captures 0 clipped (`edge_wb5.log`).

## W-C2 — opponent reactivity (§32/§33 on §36's terms): a contested region defended, an attack answered, and the personality deciding what a reaction is made of (2026-08-30)

**What it is.** `CampaignReactivity.cs` — `PublicActivity`, the public record of what every party can
SEE of every other: counts of visible local acts per region (a rally 1, a town hall or a canvassing
day 0.5 — the press was there; never the doors knocked) and of attacks, by attacker on target,
decayed on a `HalfLifeDays` 7 news half-life. **It can express no truth**: the harness asserts by
reflection that no public member's name carries *truth / actual / preference / share / vote* — the
same bar `AiView` is held to. A party reads three things off it: a region's PRESSURE (every other
party's activity there), its PUSH (the largest single opponent's concentration there — one party
working a region is a threat in a way that eight parties passing through is not), and the attacks
aimed at itself, by attacker.

**The rule decides WHERE and WHETHER; the personality decides WHAT.** §33's expected value cannot
see a threatened region (a party holds no regional standing in its own books, and a small valkrets's
audience is a fraction of an office region's), so reallocation is a RULE, as the manager's plan is
(`CampaignAi.Reactions`). A party whose `Reactivity` is above 0 and that sees a PUSH past its
threshold (`DefenceThreshold` 1.0 ÷ reactivity) into a region it has **no office in** defends it: an
office there if it can afford to keep one (`OfficeUpkeepDaysReserved` 10 days of maintenance and
operations), and meanwhile the local act **its own affinities prefer among those its pace can carry
today**. A party attacked past its threshold answers with **the message its own affinities prefer**,
on **the issue its own measurement makes most salient** — never the world's salience, which no party
can see (§36). §32's reactivities: professional 1.0, establishment 0.7, grassroots 0.6, populist 0.5,
chaotic 0.0 — the chaotic party never looks.

**The done-when, asserted (`CampaignReactivityHarness`, 9 of 9).** The scenario: W-C1's eight-party
Swedish staging with **L replaced by a scripted party** — the player's stand-in — which from day 5 to
45 works **Blekinge län** every day (a town hall and a canvassing day) and announces policy against S
every day; Blekinge is the region no AI would otherwise choose, so anything an AI does there is a
reaction. The same ten seeds (777–786) run without the script are the control.

- *The professional reallocates and the chaotic party does not:* over ten seeds S puts **8 of its 9
  local acts** into Blekinge with the script and **0 of 8** without (0.0 % → 88.9 % pooled); C is
  **0 of 598** against 0 of 595, on a campaign of hundreds. M (0.7) is 4 of 8 against 0 of 7.
- *The reallocation is real money:* seed 777, S **opens an office in Blekinge** with the script and
  never without, C opens none either way (S 1 office in reaction / 1 defence / 7 answers; M 1 / 1 / 0;
  C 0 / 0 / 0). S acts in Blekinge in **8 of 10 seeds**, first act a mean **2.0 days** after the
  script begins — the tempo, measured and not asserted.
- *The attack is answered by the party that looks and never by the party that does not:* S answers at
  its tempo in both arms (6.9 → 7.0); **C, reactivity 0, answers not once** in either.
- *The reaction is on the public record and reproduces:* the scripted run's digest is
  `9ca2e429d9d641e4` twice; the record carries pressure 3.18 on Blekinge as S sees it, 14.95 attacks
  on S, L's own 3.17 there; the scripted party played its 120 actions and 3 600 000 kr through the
  same seams as any AI's (paid, resolved, seen, logged).

### Findings carried forward

1. **The rational personalities scarcely campaign locally at all** — S makes **0.9 local acts a
   campaign**, M 0.8, against C's 59.8. §33's expected value buys a national audience with the same
   hours, so a professional party goes to a region only when a rule sends it: **the reaction is 8 of
   S's 9 local acts over ten campaigns.** A percentage of that denominator is not a figure the
   scenario can carry, so 2a counts acts and prints the denominator; the mean of per-seed shares is
   reported beside the pooled one so the two can be read against each other.
2. **Answers are tempo-bound, not attack-bound.** One answer a week is a ceiling near 8 in a 60-day
   campaign, and the chaotic party's negative campaign already presses S against it in the CONTROL.
   What an attack changes is **whom** a party answers, not how often — so the harness asserts the
   reaction (S answers at its tempo, C never answers) rather than a rise that the ceiling forbids.
3. **The establishment never crosses its own answer threshold in the C1 staging** (0.4 → 0.0
   answers): negative campaigning is aimed at the POLLED LEADER, and M is not it.
4. **A per-attacker cooldown makes five personalities into one.** The first form of the rule let a
   party answer each attacker separately; with eight parties attacking, S made **15 answers in a
   60-day campaign** and three C1 lines failed (2a-ii, 2a-iv, 2f). The fix is structural, not a moved
   constant: **one answer at a time** on the party's own weekly cooldown, and **the reaction paid
   from the day's pace** rather than out of the war chest. 2a-ii and 2f came back; 2a-iv did not.

### C1's lines, re-measured with reactivity (the standing order)

- **1h re-derived, not weakened:** the staged plan is no longer the whole network — a defence opens
  an office, `PartyLedger.OfficesOpenedInReaction` counts it, and the assertion is now staged +
  reacted, with the volunteers **bounded** rather than equated (an office opened late has not
  recruited to capacity by polling day).
- **⚠ 2a-iv: CLEARED at W-B4, back to PENDING at W-C2 — est/grass 0.291 against the 0.300 line**
  (prof/grass 0.347). Reactivity puts the ESTABLISHMENT on the ground: a broadcast party that sees a
  push into a region it has no office in defends it with a town hall, which it otherwise almost never
  holds, and its mix moves toward the grassroots party's. **Nine thousandths, and not to be recovered
  by moving a threshold, a cooldown or an affinity** — that is the whole point of the line. What
  separates two parties that both react is what they can afford to react WITH: **W-F5** (unequal war
  chests and unequal paces). Held as a PEND with its measurement, per this harness's own precedent
  when W-B11 knocked the same line out.
- Every other line holds. **PEND 4 → 5**; the harness's summary now names W-F5 and calibration entry
  10 as what remains.

### Decisions taken and logged (R-N1)

- **The rule decides where and whether; the personality decides what.** A rule that answered every
  attack with the same act would make every reactive personality the same campaign, which is what §32
  exists to prevent. The populist rallies and posts, the establishment holds a town hall and
  announces, the grassroots party knocks doors.
- **A reaction is priority, not extra money:** the act is chosen among those **the day's pace** can
  carry and is paid from the same reserve as anything else, and it simply goes first. The OFFICE is
  capital and is not pace-bound, as the staged offices are not.
- **One answer at a time**, on the party's own cooldown (`AnswerCooldownDays` 7), however many
  parties are attacking.
- **Ties in affinity go to §12's order** — the professional's affinities are flat, so it takes the
  list as written (`DefenceActs` rally → town hall → door-to-door; `AnswerActs` announcement →
  digital ad → social post). *Strikeable: a professional could instead break its ties on cost, which
  is what "allocates money efficiently" might mean.*
- **The answer's issue is the party's OWN most salient measurement**, and a party that has measured
  nothing makes a general message — never `TrueSalience`, which no party can see.
- **An office opened in reaction ends the defence cooldown for that region:** the office IS the
  defence from then on, and the AI's own weighing now sees a full region there.

### [AUTHORED-DRAFT] values, one line each (calibration entry 16)

`HalfLifeDays` 7 · a rally on the record 1.0, a town hall or a canvassing day 0.5 ·
`DefenceThreshold` 1.0 and `AnswerThreshold` 1.0, each ÷ reactivity · `OfficeUpkeepDaysReserved` 10 ·
`DefenceCooldownDays` 3 · `AnswerCooldownDays` 7 · `PressuredRegions` 2 · reactivity
1.0 / 0.7 / 0.6 / 0.5 / 0.0 by §32 personality.

### Riders

- **W-F5** — unequal war chests and unequal paces: what separates two parties that both react
  (2a-iii and 2a-iv both wait there now).
- **W-E1 / W-E5** — the player's own reading of `PublicActivity`: what its opponents are visibly
  doing, which is the same public record and no more.
- **NOT built here, and re-homed:** W-B5's finding 2 — *the campaign manager's plan over EVERY fixed
  cost (offices, operations, payroll), so a party stops going broke before polling day* — was
  written down against W-C2. W-C2's done-when is opponent reactivity and that is what shipped; the
  budget plan still covers television only. It belongs with **W-F5** (money) or an item of its own,
  and is recorded in `MISSING_PREREQUISITES.md` rather than left implied.

## W-D3 — coalition formation (§29): compatibility and negotiating power derived, red lines derived AND declared, and the chamber's own investiture rule doing the work (2026-08-30)

**What it is.** `CoalitionFormation.cs` — §29's seven inputs, each from data or from a stated
derivation, and **no authored coalition score anywhere**:

- **Compatibility** (`CoalitionCompatibility`) — DERIVED from CHES 2024: ideological from `lrgen`,
  policy from `lrecon` / `galtan` / `eu_position` (the 1–7 EU scale rescaled to 0–10 so a gap means
  the same on every axis). An axis either party leaves NaN is SKIPPED, never centred — `Compatibility`'s
  own rule for §7, for the same reason.
- **Seat strength and negotiating power** (`CoalitionMath`) — DERIVED from the seat distribution
  alone: a party's share of the chamber, and its **Banzhaf pivotality** — the share of subsets of
  the others it turns from losing to winning.
- **Red lines** (`RedLine`) — in TWO kinds and TWO strengths. Kinds: DERIVED (a position distance)
  and DECLARED (a party's own dated public commitment, with its citation). Strengths: *will not sit
  in cabinet with you* and *will not sit in or support a government that depends on you*. **A red
  line cannot be constructed without a basis** — the constructor refuses an empty one, which is
  what keeps §29 off an authored score.
- **Leader compatibility and personal relationships** — **DEFERRED, with the reason recorded:**
  there is no source, candidate attributes are already [AUTHORED-DRAFT] game fiction (W-B7), and
  inventing a leaders'-relations matrix is exactly the authored score §29 must not have. The
  harness asserts by reflection that no member carries them, so they cannot be filled in by accident.

**The mechanism is the chamber's own rule, not a preference of ours.** Sweden runs NEGATIVE
PARLIAMENTARISM: a prime-ministerial candidate is elected unless an absolute majority votes
against. So a minority cabinet governs on the votes it does not provoke — which is why Swedish
minority government is the norm rather than a branch the model has to arrange. Parties that neither
support nor oppose ABSTAIN; without abstention the negative rule would be arithmetic in disguise.

**Two further rules, both derived, both needed.** *Support is negotiated, not assumed:* absence of
a red line is not support, or every party in a Western chamber would prop up every government. A
party supports a cabinet only if that cabinet suits it at least as well as any party left out of it
does — a comparison, so no constant is chosen and none can be tuned — and where two would-be
supporters red-line each other, the one with the greater **negotiating power** stays. *And a
government must be one nobody walks out of:* a party's payoff is its share of the cabinet's seats
(portfolios follow seat share — Gamson's law, an empirical regularity) scaled by the cabinet's own
agreement, zero for a party outside the cabinet; any government some member or supporter would beat
elsewhere is struck, iterated to a fixed point.

### The done-when, asserted (`CoalitionHarness`, 16 of 16)

- **THE SECOND CLAUSE, which carries the item: a red line blocks a coalition seat arithmetic alone
  would permit.** S 107 + SD 73 = **180 of 349**, a comfortable absolute majority, and the
  formation refuses it — `DERIVED: CHES lrgen gap 4.79 > 4.50`. **120** such arithmetic majorities
  are refused in total, each printed with the line that refused it and that line's basis. And the
  counterfactual makes it a demonstration rather than an assertion: **remove the red lines, change
  nothing else, and S+SD becomes a viable majority cabinet in the same chamber.**
- **The 2022 distribution produces a plausible government set — the one that actually formed.**
  Cabinet **M+KD+L, 103 seats, carried from outside by SD's 73 = 176 of 349**, cohesion 88.9:
  the Tidö arrangement of 14 October 2022, seat for seat, as `ConfidenceAndSupply`. Nothing about
  it is stored; it falls out of sourced positions, sourced seats, sourced declarations and the
  investiture rule.
- **The deadlock is reproduced, not stored:** adding C to that cabinet COSTS SD's support (C will
  not be in a government SD carries), so the larger cabinet cannot govern at all — the 2022
  impasse in one line.
- **A new election is REACHABLE and not designed out.** A test chamber of 150/100/99 in which every
  pair refuses every other returns `NewElection` with zero viable options; **the same seats with
  the red lines removed form a government** (7 viable options). It is a consequence of refusals and
  arithmetic, never a branch taken for its own sake.

### Findings carried forward

1. **Negotiating power is not seat count.** Banzhaf pivotality for 2022: S 34.5 %, **SD 23.6 % and
   M 23.6 % — identical**, though SD holds 73 seats and M 68; and V, C, KD, MP and L are **3.6 %
   each**, though they range from 16 to 24 seats. Five parties spanning a 50 % seat difference are
   interchangeable in majority arithmetic, and SD's five extra seats over M buy no extra leverage.
   This is the measure working, not failing — but it is worth knowing before any UI shows a player
   "negotiating power" next to a seat count and invites them to read one off the other.
2. **The sourced positions really do carry Sweden's cordon — on ONE axis, with slack.** A single
   `lrgen` threshold anywhere in **[1.79, 2.58)** — a window **0.79 wide** — separates exactly the
   four parties that refused the Sweden Democrats (C, S, MP, V) from exactly the three that governed
   with them (L, M, KD). The shipped two-axis thresholds are still a fit chosen knowing the answer,
   and the harness says so; the window is what can be measured without circularity.
3. **Only one of the two declarations is load-bearing, and the harness measures which.** Dropped one
   at a time: **C ↔ SD is CORROBORATED, not load-bearing** — the derived galtan rule already
   separates C from SD (6.05 > 5.00) and the outcome is unchanged. **M, KD, L ↔ SD (no SD ministers)
   is LOAD-BEARING** — drop it and SD becomes admissible in cabinet and the outcome is a five-party
   grand coalition, the Tidö shape gone. A declaration that changes nothing here still earns its
   place: it is the only mechanism that can express the Liberals' reversal between 2018 and 2022,
   which **no position distance moved**.
4. **Without the defection rule the formation returns arithmetic, not politics.** Its first form
   ranked on cohesion, seats and pivotality and returned **S+M+C+KD+L, 234 seats** — a five-party
   bloc spanning left and right that no one proposed. Seats dominate any such ranking; what removes
   it is that M does far better in a 103-seat cabinet it leads (payoff 0.588) than as one of five
   in a 234-seat one (0.235), and walks.

### Decisions taken and logged (R-N1)

- **A red line has two KINDS and two STRENGTHS.** Kinds because a derived line generalises to any
  country with sourced positions while a declared one is a dated fact that can be withdrawn;
  strengths because Sweden 2022 turns on the difference between "I will not sit with you" and "I
  will not depend on you". *Strikeable: a single strength would be simpler and would lose the Tidö
  arrangement entirely.*
- **Support is a comparison, not a threshold** — a party supports a cabinet if none of the parties
  left out of it suits it better. No constant, so nothing here can be tuned to make a government
  form.
- **Supporter conflicts resolve by negotiating power** (§29's own term), which keeps SD and drops C
  in 2022 from the arithmetic rather than from a stored answer. *Strikeable: resolving by seats
  instead gives the same result here and a different one in a chamber where pivotality and size
  diverge — which, per finding 1, is most chambers.*
- **A party's payoff is office share × cabinet agreement, and zero outside the cabinet.** Portfolios
  following seat share is Gamson's law, an empirical regularity rather than our preference; the
  zero is what makes being IN a government beat propping one up, and is why the small right parties
  sit and SD — which has no admissible cabinet of its own to hold out for — does not.
- **`DefectionMargin` 0.01 [AUTHORED-DRAFT]:** below it two governments are the same offer and
  nobody moves.
- **The deferred §29 terms are asserted absent**, not merely left out.

### [AUTHORED-DRAFT] values, one line each (calibration entry 17)

`WeightIdeological` 0.55 / `WeightPolicy` 0.45 · `DerivedRedLines.IdeologicalGap` 4.5 (lrgen) ·
`SocialGap` 5.0 (galtan) · `WeightCohesion` 0.5 / `WeightSeatStrength` 0.3 / `WeightPower` 0.2 ·
`DefectionMargin` 0.01. **The two gaps were chosen knowing Sweden 2022's answer and are a
calibration, not a prediction — finding 2 states the slack that can be measured without
circularity.**

### Riders

- **W-D4** — post-election attribution reads this: which coalitions were arithmetically open and
  which a red line closed is half of "why you are not in government".
- **A later screen (Track E)** — the formation's ranking, its refusals and their bases are already
  a list a screen can draw; §36's gate applies, since a player should see the DECLARED lines (they
  are public) and not the model's derived distances.
- **W-F5 / W-F6** — the declared lines are Sweden-specific data; a second playable country needs
  its own, and the derived rule is what carries the rest.
- **Not built, and named:** government COLLAPSE (§29's fifth outcome) is defined in the enum and
  never produced — nothing in the prototype yet advances time inside a mandate for a government to
  fall. It belongs with a governing-phase item, not here.

## W-D4 — post-election attribution (§31): the approval ledger pointed at vote share, and an identity rather than a tolerance (2026-08-30)

**What it is.** `VoteAttribution.cs` — §31's "why you won / why you lost" as a LEDGER, built in the
`ApprovalAttribution` idiom: contributions **recorded where they land, never recomputed**, and lines
that sum to the movement they explain. `VoteAttributionSource` names the ten sources a party's share
can be moved by — its own eight §12 actions, the attacks aimed at it, and everything every other
party did as one bloc. `CampaignRun` records them at the write sites (`PartyLedger.PersuasionByAction`
per action kind, `PersuasionAgainstMe`, `Result.PersuasionPerParty`).

**The identity is exact, and that is the design.** The share a party ends with is NOT linear in the
pressures — compatibility is, but `PreferenceModel` normalises across parties — so leaving one
source out at a time would not sum to the total and would need a residual line to hide the gap. The
decomposition is therefore **Shapley over the ten sources**: its efficiency axiom makes
`Σ lines == close − baseline` an identity, and its symmetry axiom means the order sources are
considered in cannot change the answer, which a sequential decomposition cannot say. The cost is
2^10 = 1 024 preference evaluations per party, which is why the opponents are one bloc rather than
sixty-four separate sources — an aggregation that is honest about what it does not know ("your
rivals' campaigning moved your share by this much") instead of pretending to name which rally did it.

### The done-when, asserted (`VoteAttributionHarness`, 8 of 8)

- **THE FIRST CLAUSE — the lines sum to the deviation from baseline.** Largest residual across all
  eight parties: **1.77 × 10⁻¹⁶** of a share, against a stated tolerance of 1 × 10⁻¹². That is
  floating-point noise, not modelling slack, and the harness prints the worst case rather than
  asserting a bound nobody can check.
- **The ledger opens and closes on the campaign's own numbers**, not on figures of its own: largest
  difference from `Result.BaselineShares` and `Result.FinalShares` 5.55 × 10⁻¹⁷.
- **THE SECOND CLAUSE — no line is authored prose.** Asserted structurally: the instrument carries
  **no string field or property at all** (by reflection), so a label can only be a
  `VoteAttributionSource` enum name — a mechanism the model applies. Every declared source is swept,
  so no line can read zero merely because it was forgotten.
- **A source the party never used contributes exactly nothing.** SD never held a rally, a town hall,
  a door-to-door day, a digital ad or a policy announcement, and all five lines are exactly 0 — the
  ledger cannot attribute movement to something that did not happen.
- **It is a reading of the run, not of the reader:** the same seed returns the same lines.

### The ledger, printed (seed 777, the C1 staging)

```
SD  (largest gain)  20.86 % -> 22.31 %  (+1.45 pp)
    Interview            +2.744 pp
    OpponentCampaigns    -1.425 pp
    SocialPost           +0.086 pp
    TelevisionAd         +0.041 pp
    TOTAL                +1.446 pp   (residual 1.77e-16)

S   (largest loss)  30.80 % -> 29.91 %  (-0.90 pp)
    OpponentCampaigns    -1.342 pp
    Interview            +0.627 pp
    AttacksReceived      -0.288 pp
    PolicyAnnouncement   +0.077 pp
    SocialPost           +0.019 pp
    TelevisionAd         +0.009 pp
    TOTAL                -0.899 pp   (residual 6.94e-18)
```

### Findings carried forward

1. **The free interview dominates the ledger, in a third instrument.** SD's interviews are +2.744 pp
   of a **+1.45 pp** net movement — the single largest line either party has, and larger than the
   result itself. W-B3 and W-E3 recorded this as a mechanism question (bounded reach,
   repeated-exposure decay) and C1's PEND lines rest on it; §31's ledger now says the same thing
   from the other end, and a player reading this screen would conclude that campaigning is
   interviews. Recorded, not adjusted.
2. **The attribution corroborates the standing design question** opened against W-B4/W-B11 from
   W-C2: SD's ledger has **five of its eight action lines at exactly zero**, all of them the local
   and paid ones. That is the second independent measurement of the same thing, and it belongs in
   evidence when that question is answered — the ruling of 2026-08-30 stands: measure which of the
   two causes it is before adjusting either.
3. **A party's own campaigning is not the biggest thing that happens to it.** `OpponentCampaigns` is
   the largest line in S's ledger (−1.342 pp) and the second largest in SD's. §31's example gives
   the player only their own doings; the model says the rivals' campaign is comparable in size, and
   a screen that hides it would be misattributing the result to the player.

### Decisions taken and logged (R-N1)

- **Shapley, not leave-one-out or a sequential pass.** Leave-one-out does not sum and would need a
  residual line; a sequential pass sums but is order-dependent and would quietly privilege whichever
  source was applied first. *Strikeable: Shapley costs 2^n, which is what caps the source list at
  ten.*
- **The opponents are ONE source.** Sixty-four separate sources would be exact and unaffordable
  (2^72); naming them individually without the sweep would be a guess. *Strikeable: a second
  aggregate — "the party I was attacked by most" — would cost one more bit.*
- **Momentum, coverage, debates and scandals have NO line, and that is correct here.** The true
  preference is moved only by persuasion pressure; debates and scandals move coverage and momentum,
  which move the POLL and hence what the campaign chose to do. Their effect is already inside the
  action lines, and giving them a line of their own would double-count. Recorded because it is the
  first thing a reader will ask.
- **Turnout and tactical voting are a later stage and have no line here** — this ledger explains the
  PREFERENCE share. W-D1's election day applies turnout and W-A4's tactical layer on top of it;
  attributing the seat result rather than the share is a second instrument, named as a rider.

### Riders

- **A Track E screen** draws this ledger directly — the rows are already computed, signed and
  ordered, and §36 does not bite (a party's own campaign and the published aggregate of others' are
  both things it may know).
- **A second ledger for the SEAT result** — turnout (W-B11), tactical voting (W-A4) and the seat
  allocation stand between share and seats, and each is a stage this instrument does not cross.
- **W-F4's voter groups** will make each line decomposable per group, which is what §31's "young
  voters / urban voters" lines actually ask for; today's electorate is one group (W-F4 retires it).

## W-E5 — the debate screen (§15 on §36's terms): three states, the model's own ceiling drawn, and an exchange that has not happened drawn as absent (2026-08-30)

**What it is.** `DebateScreenSnapshot.cs` (the screen's read model, pure data) and
`GameController.CampaignDebate.cs` (the drawing) — the fifth of the Track E class, on the same
1156 × 680 board, the same 440 / 250 / 440 columns and the same primitives as Campaign HQ, the
action screen, the polling screen and the map. ⚠ **HARNESS ONLY — R-N2 holds until W-G1.**

- **Left, THE PODIUMS:** each candidate's §16 attributes, side by side — the five a debate move
  actually draws on. The note says attributes are game fiction and that the blend is the model's,
  not an average the screen takes.
- **Middle, PREPARATION:** hours, topics, and **§35's curve quoted as the multiplier it is**
  (`×0.93` against `×0.85` in the filmed staging) — a player who buys hours should see what the
  hours bought, not a bar with no units.
- **Right, THE FLOOR and THE VERDICT:** every exchange with its topic, the two moves and the two
  point figures; then the performance indices, the margin, and **the two shocks**.

**The screen's ceiling is the model's ceiling.** `DebateResult` carries no share, no preference and
no party standing (asserted by reflection since W-B7), so the verdict column reports the indices,
the margin, the coverage shock and the momentum shock — and stops, saying in as many words that a
debate moves coverage and momentum and no vote share directly. A screen that ended "+1.2 % in the
polls" would invent the one number §15 refuses to produce and teach the player to read a debate as
votes.

**An exchange that has not happened is an em dash, never a zero** (the Desk's Year-0 convention,
1m-r2): mid-debate the remaining rows are absent rather than scored, because a zero claims the
candidates said nothing and that is a different statement. For the same reason the mid-debate figure
is the **running mean, labelled with how many exchanges it is over** — the performance index is a
mean over ALL exchanges, so quoting it early would quote a number the debate has not produced.

### The done-when, filmed

Three states, **filmed at 1280 / 1600 / 1920 / 2560**, twelve captures
(`we5_debate_{width}_e5_campaign_debate_{prep,midway,verdict}.png`):

- **prep** — nothing has been said: the floor reads "THE DEBATE HAS NOT BEGUN", and margin,
  coverage shock and momentum shock each read "— NOT YET".
- **midway** — a genuine PREFIX of the finished debate (3 of 6 exchanges), not a separately invented
  picture: the debate is RUN once through `Debates.Resolve` on the `Debate` stream at `FilmSeed` 777
  and the midway film is its first half, so the two states cannot disagree.
- **verdict** — 6 exchanges, performance **65.3 / 52.9**, margin **12.4 pts** to Andersson, coverage
  shock **1.24**, momentum shock **2.49 pp**.

**Guards silent, and they were not silent TWICE over.** The first film exited 1 with **12 text
overflows** — the three footnotes were drawn with `PoliSimWidgets.MeasuredLabel`, a SINGLE-LINE
widget that shrinks to fit and trips the guard when it cannot; the wrapped-note idiom the sibling
screens use is `GUI.Label` with the height measured by `CalcHeight` for the width, plus a
`UiContainmentGuard.Check`. Converted to it, all four widths exit 0 with **0 text overflows and 0
containment escapes**, and `ScreenEdgeCheck -edgepattern=we5_debate_*.png` exits 0 over **76
captures**. The guard catching this is the guard working; it is recorded rather than quietly fixed.

### Decisions taken and logged (R-N1)

- **The debate is RUN, not mocked, for the film.** One `Debates.Resolve` at a fixed seed produces
  all three states, so the midway film is provably the verdict's own prefix. *Strikeable: staging
  three unrelated pictures would film faster and could disagree with itself.*
- **The two candidates are deliberately UNEQUAL** (14 h against 6 h of preparation, different
  ownership) so the verdict has a margin worth reading rather than a draw that shows nothing.
  Attributes are `[AUTHORED-DRAFT]`, W-F6's to source; the names are placeholders, not real leaders.
- **The middle column quotes the preparation MULTIPLIER, not the hours alone** — §35's curve is the
  thing the player is buying, and an hours figure without it is a price with no product.

### Riders

- **W-E5 is a modal on the sheet in the worklist's words; it is drawn as a full stage here**, the
  same as its four siblings, because the campaign screens are stages and a modal would need a
  scrim, a dismiss affordance and a return target that no other Track E screen has yet. If Design
  wants it as a modal, that is a re-skin of the same board (the R-E2 precedent).
- **The exchange rows have no ownership or clash column** — both are in the model and neither is on
  the screen, because eight columns do not fit at 1280 and the point figures already carry them.
  A later pass may add a per-exchange detail line.
- **W-F6** sources the real party leaders' names; the attributes stay game fiction and labelled.

### And a second guard fired, on the same screen (W-E5, recorded 2026-08-30)

**`MetaTextCheck` — P-A1's ninth check — exited 1 on the finished screen:** the podium column's
ledger head read `"THE PODIUMS — SECTION 16 ATTRIBUTES"`, and an internal spec reference on a player
surface is exactly the class P-A1 censused and armed a guard against. One hit, named with its file
and line.

Fixed, and the same class fixed where the token list does NOT reach: `"ATTRIBUTES ARE GAME FICTION
AND LABELLED SO — A MOVE'S BLEND OF THEM IS THE MODEL'S…"` addresses a builder, not a player, and so
does `"…AND THIS SCREEN WILL NOT CLAIM IT."` Both were rewritten into player language ("THESE
RATINGS ARE THE GAME'S OWN INVENTION, NOT A MEASUREMENT OF ANYONE"; "WHATEVER THE POLLS DO IN THE
DAYS AFTER IS THE MOMENTUM'S DOING"). The guard is the floor, not the ceiling — it catches the
tokens it knows, and the intent behind it is what the other two violated.

**Re-run after the fix: `MetaTextCheck` exit 0, all four widths exit 0 (0 overflows, 0 containment
escapes), `ScreenEdgeCheck` exit 0 over 76 captures.** Two guards fired on this screen and both were
right; that is a better outcome than a screen that passed first time, because it is evidence the
guards bind on new work rather than only on the work they were written for.

## W-E6 — election night (board 1h, §30): the count arriving by constituency, a gate that makes a premature result impossible, and calls that cannot be contradicted (2026-08-30)

**What it is.** `ElectionNight.cs` (the model), `ElectionNightScreen.cs` (**board 1h — the last
unbuilt board of §A.14, the Canvas slot reserved for it since v2**), `ElectionNightFilm.cs` (the
staging both the harness and the film read, so the two count ONE election),
`ElectionNightHarness.cs` (**11 of 11**), and `SimulationRandom.Stream.ElectionNight = 11`
(APPENDED, never inserted — the ElectionNoise and CampaignAi precedent, re-proven byte-identical).

**Two rules govern the item, and both are STRUCTURAL rather than conventions to be remembered.**

**1 — No result exists before its constituency declares.** The running tally is not a number
revealed gradually; it is COMPUTED, at every instant, from the declared constituencies and nothing
else. An undeclared constituency carries `null` votes and no valid count, so a screen cannot draw a
figure that has not happened — not because the screen is careful but because the number does not
exist. *Asserted by independent re-derivation:* the harness sums the declared set itself and
compares vote for vote, over **392 states across 8 seeds — 0 early appearances, absence never zero,
every tally agreeing**. A scripted reveal would pass a screenshot review and fail this.

**2 — A call, once made, cannot be contradicted.** Not "is unlikely to be" — cannot. Each
undeclared constituency is bounded by its own ELIGIBLE electorate (published before the night, and a
hard cap since turnout cannot exceed 100 %), and a call is made only when the claim holds at BOTH
extremes: every outstanding vote to the claim's enemy, and none of them anywhere. The seats at those
extremes come from **`SeatAllocation` — the allocation the backtest reproduces Sweden 2022 with
seat-for-seat** — so the call cannot rest on arithmetic the final tally does not use. *Asserted:*
**1 849 call-instants across 8 seeds, 0 contradicted.** And the harness re-proves the allocation on
the same page: S 107/107, SD 73/73, M 68/68, V 24/24, C 24/24, KD 19/19, MP 18/18, L 16/16.

### The night the model produces (seed 777, Sweden 2022's own returns)

| state | minute | declared | counted | calls safe |
|---|---|---|---|---|
| early | 30 | 4 of 29 | 515 869 | 0 |
| partial | 59 | 16 of 29 | 2 510 364 | 3 |
| called | 151 | 28 of 29 | 5 583 801 | 7 |
| final | 240 | 29 of 29 | 6 377 718 | 11 |

When each call becomes safe, over the seeds: **S from 6 of 29**, SD from 8, M from 11, C from 23,
V from 25, KD from 27, MP from 28 — and **L's clearance, the largest party and the bloc majority
only at 29 of 29.** That is the 2022 chamber telling the truth about itself: a 176–173 result turning
on L's 4.61 % cannot be called until L is in.

**Filmed in four states at 1280 / 1600 / 1920 / 2560** — sixteen captures,
`we6_night_{width}_e6_election_night_{early,partial,called,final}.png`; all four widths exit 0 with
**0 text overflows and 0 containment escapes**, `MetaTextCheck` exit 0, and `ScreenEdgeCheck
-edgepattern=we6_night_*.png` exit 0 over **32 captures**. The four states are four instants of ONE
seeded night, so they cannot disagree with each other.

### Findings carried forward

1. **A guarantee calls later than a projection would, and that is the trade the item bought.** A
   network calls on a model and is sometimes wrong; this calls on a bound and is never wrong. The
   cost is visible above: the marquee calls land with the last constituency. **Tightening it would
   need a SOURCED turnout ceiling** ("no Swedish valkrets has exceeded X % since 2002") — which is
   empirical, not certain, and would weaken "cannot" to "has never". Recorded as a rider, not taken.
2. **The threshold calls carry the drama instead, and they carry it well.** Seven of eleven calls
   land before the end, spread from 6 of 29 to 28 of 29, so the night has a shape without any of it
   being scripted: each one is a bound crossing a line, at the minute the arithmetic crosses it.
3. **The chip's spec wording did not survive contact with the system.** §A.14 specifies a
   "348 OF 350 SEATS DECLARED" chip; seats in this system are allocated NATIONALLY and are not
   declared one at a time, so the chip reads **constituencies** declared. The mock-up's wording
   implied a different electoral system than the one the game models.

### Decisions taken and logged (R-N1)

- **Its own random stream.** The night is a separate act from the count: an election already counted
  must be re-playable as a night, in a different declaration order, without moving a single vote.
  Borrowing `ElectionNoise` would make the arrival order and the result share one draw sequence, so
  restaging the night would silently change who won.
- **The schedule is monotone in the electorate plus a seeded jitter** — small constituencies count
  faster, which is a fact about counting rather than a dramatic choice — and the LAST arrival is
  pinned to the final minute so "final" is a state the night reaches rather than approaches.
- **The four filmed states are chosen by DECLARED COUNT, not by clock time**, because that is what
  the screen is about. *Strikeable: fixed minutes would be simpler and would film a different night
  under a different seed.*
- **The bound concentrates the whole outstanding electorate on ONE party** (the strongest opponent
  for a worst case, the strongest member for a best case). Concentrating it is what makes the bound
  extreme, and an extreme bound is what makes the call safe.

### Declared deviations from §A.14's board (V-N series)

- **V-N1** — flat paper and a single dark plate for the shadow: the CSS gradient and double shadow
  have no delivered sprite (1g's V-S1, the same absence, and the absence guard is honoured).
- **V-N2** — the declaration-wave, count-up and stamp-thunk BEATS are not animated. The board is
  filmed in four states, and an animation no capture can check is a claim no reviewer can test; the
  states are the honest subset and the beats stay in the spec for the wiring item.
- **V-N3** — the swing column is OMITTED: a swing needs the previous election's per-constituency
  result beside this one's, and the night's model carries one election. Named rather than faked.

### Riders

- **W-E7** draws §30's full breakdown and W-D4's ledger; this board is the arrival, not the analysis.
- **A sourced turnout ceiling** would tighten the bound and let the marquee calls land earlier —
  finding 1, and a Track F data item if it is wanted.
- **The beats (V-N2)** belong to the wiring item, where a clock exists to play them against.

## W-E7 — results and attribution (§30 + W-D4's ledger): every figure counted, derived or published, and the one §30 asks for that this screen refuses to invent (2026-08-30)

**What it is.** `ResultsScreenSnapshot.cs` (the read model) and
`GameController.CampaignResults.cs` (the drawing) — the sixth Track E screen and the last IMGUI one,
on the same 1156×680 board and the same 440 / 250 / 440 columns as its five siblings. ⚠ **HARNESS
ONLY — R-N2 holds until W-G1.**

- **Left, THE COUNT:** every party's votes, share, seats, and the movement against a NAMED prior
  election — `+2.10 pp / +7` for S, `−0.89 pp / −4` for L.
- **Middle, THE CONSTITUENCIES:** §30's regional results, largest first, each with its leader.
- **Right, WHY:** W-D4's ledger unaltered — every line a mechanism, the labels enum names, and the
  lines summing to the movement as an identity. The screen adds no interpretation; if it did, the
  sum would stop being a proof and become a story.

### The done-when: every figure traces, and the three classes are kept apart

- **COUNTED** — the votes, the seats (`SeatAllocation`, the allocation that reproduces 2022
  seat-for-seat), the regional table.
- **DERIVED, and proven against a sourced answer** — 2018's seats are computed by that same
  allocation rather than typed in, and the comparison column validates itself: **S shows +7 (100 →
  107) and L shows −4 (20 → 16), which are the real 2018→2022 changes.** A second arithmetic that
  merely agreed would have been worth much less.
- **PUBLISHED** — turnout **84.21 %** of **7 775 390** eligible, Valmyndigheten's own basis.

⚠ **Two figures were wrong on the first film and are recorded rather than quietly fixed.** The
screen read turnout **85.88 %** — the eight parties' votes over a *derived* electorate — and
labelled 6 377 718 as "VALID VOTES" when the official valid total is 6 477 970 (the difference is
minor parties this model does not carry). Neither error touched a seat, but **a results screen is
precisely where a derived figure gets mistaken for a published one**, so turnout now quotes the
source and the total says what it is: *the votes these parties took*, with the footer stating that
shares are of that total and not of all ballots cast.

### §30's demographic block is drawn ABSENT, and that is the item's most deliberate line

§30 asks for young / older / urban / rural / income voters. **The electorate is ONE GROUP** —
`ElectionDay.cs:28`, `CampaignRun.cs:91`, `TacticalVoting.cs:19` and four more sites all name W-F4
as where that retires. Five plausible rows split out of one group would be invented demographics,
which §0.4 forbids outright. So the five rows are drawn as em dashes under a stated reason, the
same convention as W-E6's V-N3 and W-E2's §36 gate: absence is a fact, and a zero is a different
claim.

### Filmed

Two states — **`largest`** (the player as S, the largest party) and **`lost_ground`** (the player as
L, which lost four seats) — **at 1280 / 1600 / 1920 / 2560**, eight captures. *A results screen only
ever seen on a win is one where nobody has checked the signs.* All four widths exit 0 with **0 text
overflows and 0 containment escapes**, `MetaTextCheck` exit 0, and `ScreenEdgeCheck
-edgepattern=we7_results_*.png` exit 0 over **84 captures**. The W-E5 lesson carried: wrapped notes
use `GUI.Label` with `CalcHeight`, never the single-line `MeasuredLabel`, and the guards were silent
first time.

### Decisions taken and logged (R-N1)

- **The comparison election is carried WHOLE, not as pre-computed deltas**, so the screen names what
  it compares against instead of showing a bare arrow. *Strikeable: deltas alone would be smaller
  and would let a reader assume the wrong baseline.*
- **2018's seats are DERIVED by the same allocation**, never typed from the record — so the column
  cannot silently disagree with the one beside it, and when it matches the real distribution that is
  evidence rather than coincidence.
- **Turnout and the electorate are the PUBLISHED figures**, not this model's own ratio.
- **Two filmed states, one of them a loss.**

### [AUTHORED-DRAFT] values

**None.** Every figure on this screen is counted, derived from a sourced input, or published — which
is the whole of its done-when.

### Riders

- **W-F4** fills the voter-group block; the five em-dash rows are its acceptance test.
- **W-F1** replaces the regional table's 2018-distributed counts with the real 2022 per-valkrets
  returns, and the shares stop being of the eight-party subtotal.
- **W-E8** takes the coalition that forms from these seats.

## W-E8 — the coalition screen (§29 on §36's terms): the arithmetic, what it would have allowed, and the difference between a refusal someone uttered and a distance this model measured (2026-08-30)

**What it is.** `CoalitionScreenSnapshot.cs` (the read model), `GameController.CampaignCoalition.cs`
(the drawing) and `CoalitionFilm.cs` (the staging the screen and the HARNESS now share) — **the
seventh and last Track E screen**. ⚠ **HARNESS ONLY — R-N2 holds until W-G1.**

- **Left, THE ARITHMETIC:** every party's seats, its Banzhaf pivotality, and its role in the
  government that formed — in cabinet, supporting, or neither.
- **Middle, MAJORITIES THE SEATS ALLOWED:** the combinations the arithmetic would have carried and
  the line that refused each, smallest first.
- **Right, RED LINES:** declared and measured, kept apart.

### The screen's one real decision, and §36 decides it

**A DECLARED line is public.** A party said it, the citation is on disk, and the screen states it
flatly and names who holds it. **A DERIVED line is this model reading a gap** between two parties —
nobody in the world has uttered it — so it is drawn as the DISTANCE it is, with the axis and the
figure, under a heading that says so: *"MEASURED — A DISTANCE, NOT A REFUSAL ANYONE UTTERED"*.
Showing a measured gap as a refusal would put words in a party's mouth and hand the player a
certainty that does not exist. The two kinds sit in the same column, separated by a rule, because
the difference between them is the thing worth teaching.

**The middle column is the item's other argument.** A coalition screen that showed only what CAN
form teaches the player that the arithmetic is the whole story. This one shows the **120 arithmetic
majorities a red line refused** on Sweden 2022, each with the pair that refused it and whether that
was declared or measured — which is what makes a red line legible as politics rather than as a
missing option.

### The done-when: three outcome states, and all three fall out of the model

Nothing is staged; each is `CoalitionFormation.Form` on real inputs.

| state | outcome | cabinet | viable | refused |
|---|---|---|---|---|
| `confidence_and_supply` | ConfidenceAndSupply | **M+KD+L, 103 seats, carried by SD** | 1 | 120 |
| `new_election` | **NewElection** | — | 0 | 4 |
| `majority` | MajorityCoalition | S+M+C+KD+L, 234 seats | 14 | 119 |

The first is Sweden 2022 as it happened. The second is the 150/100/99 chamber where every pair
refuses every other — the harness's own reachability proof, drawn. The third is **the same 2022
seats with the DECLARED lines dropped**, which is the counterfactual that shows what those
declarations are actually doing: without them SD is admissible in cabinet and the result is a
five-party bloc nobody proposed. That agrees exactly with W-D3's own measurement.

**Filmed at 1280 / 1600 / 1920 / 2560** — twelve captures — all four widths exit 0 with **0 text
overflows and 0 containment escapes**, `MetaTextCheck` exit 0, and `ScreenEdgeCheck
-edgepattern=we8_coalition_*.png` exit 0.

### Decisions taken and logged (R-N1)

- **The harness and the screen were pulled onto ONE staging** (`CoalitionFilm`): the same
  compatibility matrix and the same red lines. Two surfaces disagreeing about which coalitions are
  possible would be worse than either being wrong alone — the `ElectionNightFilm` precedent from
  W-E6. All 16 coalition assertions still hold on the shared path.
- **Declared and measured lines are drawn differently, and the screen says which is which.**
  *Strikeable: one undifferentiated list would be shorter and would quietly promote a measurement
  into a quotation.*
- **The refused majorities are shown smallest first**, because the tightest refusals are the ones a
  reader can hold in mind — a 234-seat refusal teaches less than a two-party one.

### [AUTHORED-DRAFT] values

**None.** Every figure is the formation's own: seats counted, pivotality derived from the seats,
compatibility derived from sourced CHES positions, red lines derived-or-cited.

### Riders

- **W-E7** hands this screen its seats; **W-D3** is the model it draws.
- **W-H4** — the coalition screen invents no sprite; if Design wants party marks in the arithmetic
  column, that is a line in the ask, not art invented here.

## W-B12 — the campaign manager's full cost plan (§9): pay the organisation first, release the rest (2026-08-30)

**What it is.** W-B5 built the manager's `BudgetPlan` over television alone and measured what that
left out: **every party in the AI campaign went broke before polling day** — of 120 staff-days,
SD 38 unpaid, V 12, S 10, M 6. The cause was structural, not a shortage: **the spending pace
released money for ACTIONS against the whole war chest, while the payroll and the offices were
charged from the same chest afterwards, so the two claims on the money never met until the money
ran out.**

`BudgetPlan` now carries `DailyFixedCost` — the payroll (`StaffRoster.DailySalaryBill`) plus every
office's maintenance and daily operation (`OfficeNetwork.DailyCost`) — set each morning, because a
party that hires or opens an office today has a different bill tomorrow. `CommittedToOrganisation
(daysLeft)` is what must still be kept back to reach polling day, and the pace's release is capped
by `money − committed` instead of by `money`. **A party without a manager has no plan and no such
discipline, which is exactly the difference §9 says a campaign manager makes.**

### Measured, on the staging that found the defect (seed 777, 60 days, 120 staff-days each)

| party | manager? | unpaid staff-days BEFORE | AFTER |
|---|---|---|---|
| S (professional) | yes | 10 | **0** |
| M (establishment) | yes | 6 | **0** |
| KD (establishment) | yes | — | **0** |
| L (professional) | yes | — | **0** |
| SD (populist) | yes | 38 | **6** |
| V (grassroots) | **no** | 12 | 12 |
| MP (grassroots) | **no** | — | 12 |
| C (chaotic) | no staff | 0 | 0 |

**Four of the five managed parties now pay their organisation in full to polling day.** The two
unmanaged parties are unchanged at 12, which is the intended shape: the plan is the manager's
effect, and a party that hired no manager gets no discipline.

⚠ **The done-when is not fully met, and this is stated rather than rounded up.** SD retains **6
unpaid staff-days**. It is **not** a reaction office — the 1h line shows SD at *4 staged + 0 in
reaction*, so W-C2 is not the cause. What is left is the populist's pace: `spendPace` 1.6 is the
most front-loaded of the five, and its 4 offices at 100 000 kr of opening capital plus a planned
television buy leave the tightest margin of any managed party. The remaining six days are a
**residual to be measured, not a constant to be moved** — the plan protects against the bill it can
see each morning, and capital commitments (an office opened, a buy planned) are not in that bill.

### C1's PEND lines, re-measured (the standing order, and the 2026-08-30 ruling)

- **⚠ `prof/est` CROSSED 0.30 FOR THE FIRST TIME: 0.061 → 0.306.** W-B5 recorded the two rational
  planners converging on the same schedule with equal money; the ruling sent that to W-C2/W-F5.
  **What actually separated them was the manager's plan** — a planner and a non-planner spend
  differently even on equal money, which is a better answer than "unequal war chests would separate
  them" and was not the one predicted.
- **2a-iii still PENDs**, because it needs all three pairs and `est/grass` is 0.269.
- **⚠ `est/grass` moved AWAY from the line: 0.291 → 0.269.** The ruling of 2026-08-30 anticipated
  exactly this shape — *"if W-F5 lands and it still reads 0.291, that is a finding about the model"*
  — and here it was W-B12, not W-F5, that moved it, and in the wrong direction. **Nothing was tuned.**
  The line stays a PEND with its measurement, and the finding is that giving the establishment
  budget discipline moved its action mix TOWARD the grassroots party's rather than away.
- The harness reads **ALL ASSERTIONS PASS; 5 PENDING**.

### Decisions taken and logged (R-N1)

- **The cap is on the RESERVE, not on the actions.** The pace still decides how fast money is
  released; the plan only decides how much there is to release. *Strikeable: capping each action
  instead would make the manager a censor rather than a treasurer.*
- **`DailyFixedCost` is set every morning**, not once, because offices and hires change it.
- **Capital is deliberately NOT in the daily bill** — an office's 100 000 kr opening and a planned
  television buy are one-off commitments, and folding them into a per-day figure would misstate both.
  That choice is what SD's residual six days sit on, and it is named here rather than hidden.

### [AUTHORED-DRAFT] values

**None.** `DailyFixedCost` is summed from figures that already exist (`SalaryPerDay`,
`MaintenancePerDay`, the staged operations rate); the item introduces no new constant.

### Riders

- **SD's six days** — measure whether capital commitments belong in the plan's horizon.
- **W-F5** — unequal war chests; `est/grass` and 2a-iii still wait there.

## W-F1 — Sweden's 2022 returns by constituency: the election the model counts is now the election that happened (2026-08-30)

**What was wrong.** `ElectionsData/sweden/returns_2022.md` carried all 29 valkretsar but only **five
party columns, and only as shares**. The only per-constituency ABSOLUTE counts on disk for all eight
parties were **2018's**, and **nine code paths read that 2018 file as if it were the electorate** —
the seat conversion, election day, GOTV, the offices, the AI campaign's region audiences, election
night, and the campaign map on screen. Two more read it as a PRIOR, which is correct and was left
alone (below).

**What was fetched.** Valmyndigheten's own per-constituency backend,
`https://resultat.val.se/data/resultat/val2022/RD_{NN}_S.json` for NN = 01…29, final count
(`rakningstillfalle: "slutlig"`), 29 files. **Code 30 returns 404 — that is the completeness check**:
Sweden has exactly 29 riksdagsvalkretsar and the source agrees.

**How it was checked, and this is the strongest part.** The 29 per-constituency files and the
national `RD_S.json` are **independent downloads**. Aggregating the 29 and comparing against the
national file and against `returns_2022.md` gives **eleven column sums, all eleven exact**: valid
6 477 970 · eligible 7 775 390 · cast 6 547 801 · S 1 964 474 · M 1 237 428 · SD 1 330 325 ·
C 434 945 · V 437 050 · KD 345 712 · L 298 542 · MP 329 242. One pass validates the parse, the
aggregation and the file's completeness together.

### The headline: W-D2's claim got much stronger, and its internals were wrong

W-D2 asserts *"Sweden 2022 reproduces seat-for-seat through the live path"*. Until today it did that
on a **synthetic** chamber — the 2022 NATIONAL counts spread over 29 constituencies by 2018's
distribution. The totals were right **by construction**: the national counts were exact, so the
national totalfördelning could hardly miss. **The 29-constituency procedure — the fixed seats, the
12 % rule, where the 39 adjustment seats land — was being asked a smoothed question.**

On the real counts it **still reproduces 107/73/68/24/24/19/18/16, 8 of 8 exact**. But the fixed /
adjustment split moved for four parties:

| party | fixed before → after | adjustment before → after |
|---|---|---|
| **KD** | 10 → **13** | 9 → **6** |
| **S** | 105 → **104** | 2 → **3** |
| **V** | 17 → **16** | 7 → **8** |
| **MP** | 11 → **10** | 7 → **8** |
| SD / M / C / L | unchanged | unchanged |

**KD's fixed seats were understated by three.** And Stockholms län's fixed seats went **39 → 40**,
because the constituency's seat entitlement is computed from ELIGIBLE voters and eligible was
derived, not sourced. The seat table was right; the account of HOW Sweden produces it was not.

### The second retirement: eligible voters

Per-valkrets eligible counts were **DERIVED as 2018 valid votes ÷ a national turnout**. That put the
national electorate at **7 429 141 against the published 7 775 390 — 346 249 voters, 4.5 % low** —
because one national turnout was applied uniformly to constituencies whose real turnout ran from
**77.22 % (Malmö kommun) to 87.56 % (Västra Götalands läns västra), a 10.3 pp spread**, so the error
did not cancel. Column 11 is now Valmyndigheten's own `antalRostberattigade`. `Turnout2018` is
retired as an eligible-derivation at all four sites that used it, and `DATA_BILL.md`'s bill in
`turnout_history.md` is marked **PAID**.

### ⚠ Two of the "ten consumers" were reading 2018 CORRECTLY, and were NOT repointed

`CompositionHarness` and `GateReRun` read the 2018 file as **the PRIOR for a backtest of 2022** —
exactly as Germany's 2021 Land votes are the prior for its 2025 case. **Repointing them would have
let the model see the answer it is scored against**, and the MAD it reports would have stopped
meaning anything. Both keep the 2018 file, with a comment at each site saying why. Verified after
the change: SWEDEN 2022's MAD is unmoved (both-layers 1.47 pp, §8-only 1.47 pp), which is the proof
the backtest was not contaminated.

### Everything that moved on a player-facing screen, reported (risk 4 of the plan)

- **The campaign map** (W-E2) now reads **13 swing constituencies at the regional buy and 16 at the
  full programme** (was 11 and 12), and **20 / 18 too close to call** (was 19 / 13). Sweden's real
  2022 regional variation is **more contested** than 2018's pattern implied.
- **Election night** calls S's threshold clearance from **8 of 29** (was 6) and C's from **25**
  (was 23) — the real distribution is less smooth than the synthesis, so the guarantee is slightly
  slower. 0 of 1834 call-instants contradicted, unchanged.
- **Election day** counts **6 564 111 of 7 775 390** (was 6 272 383 of 7 429 141); turnout 84.42 %
  either way, because the derivation was self-consistent even while being wrong.
- **The offices**: a full office's local audience in Stockholms län is **1 006 456** (was 934 883).
- **The AI campaign**: national audience 6 477 970 (was 6 476 725); every decision digest changed
  (`0c98e88d…` → `817143c1…`) as the inputs did.
- ⚠ **C1's PEND lines did NOT move**: prof/est 0.306, est/grass 0.269, identical to W-B12's reading.
  **That is worth stating** — it rules out the data vintage as the explanation and leaves the
  separation question exactly where the standing ruling put it, on W-F5 and on behaviour.

### Decisions taken and logged (R-N1)

- **The 2022 file keeps the 2018 file's ROW ORDER**, not the JSON backend's (which puts Stockholms
  kommun first). Every consumer keys regions by index, so this makes the repoint change the NUMBERS
  and nothing else. *Strikeable: adopting the source's order would have silently reordered 29
  regions and made every moved figure unattributable.*
- **Columns 1–10 are the 2018 file's shape exactly**, so a consumer repoints by changing a filename;
  eligible and cast are appended as 11 and 12. The shared `ReadCsv` takes a MINIMUM column count, so
  appending is safe.
- **Capital-C `valid` is `rosterPaverkaMandat`** — the count the seat allocation actually runs on —
  not ballots cast. The file's header says so, and says the party columns do not sum to it.

### [AUTHORED-DRAFT] values

**None.** W-F1 retires a derivation and introduces no authored figure.

## W-F6 — the party leaders: the names are sourced, the characters are not (2026-08-30)

**What was on disk.** Nothing. Two placeholder surnames lived in the screenshot driver
(`"Andersson"`, `"Kristersson"`) with a comment saying W-F6 would source them.

**What was fetched, and why this source and not an encyclopaedia.** Each of the eight leaders is
taken from **that party's own website as it stood within days of the election**, retrieved through
the Internet Archive so every citation carries an exact capture timestamp. **A party is the
authority on who leads it**, and an archived capture is the only way to ask that question of
September 2022 rather than of today — five of the eight have changed leader since.

| party | leader at 2022-09-11 | the party's own words |
|---|---|---|
| S | Magdalena Andersson | *"partiordförande och Sveriges statsminister"* (capture 2022-08-10) |
| SD | Jimmie Åkesson | the page's own title: *…sverigedemokraternas-partiledare* (2022-09-30) |
| M | Ulf Kristersson | named five times on the front page, election night (2022-09-12) |
| V | Nooshi Dadgostar | *"är Vänsterpartiets partiledare sedan den 31 oktober 2020"* (2022-09-05) |
| C | Annie Lööf | named five times on the front page, election night (2022-09-12) |
| KD | Ebba Busch | ⚠ **the weakest citation, stated as such** (below) |
| L | Johan Pehrson | named three times on the front page, election night (2022-09-11) |
| MP | Märta Stenevi **and** Per Bolund | *"Språkrör och riksdagsledamot"* (2022-09-11) |

⚠ **KD's citation is weaker than the other seven and this is written down rather than smoothed
over.** KD's own site is JavaScript-rendered, so its archived captures carry **no static role text**
at all. What the party's own site does give is a representatives page at
`/om-oss/foretradare/ebba/` and a news archive publishing her speeches as the party's voice in the
Riksdag. The **office** is carried instead by the **Tidö agreement** — a primary document already
SOURCED on disk (`coalition_declarations_2022.md`) and signed by the four party leaders. Seven
citations are the party saying it in its own words; one is an inference from two primary documents,
and the file says which is which.

### The line this item does NOT cross

**What is sourced is the NAME and the OFFICE. Nothing else.** The nine `CandidateProfile` attributes
beside each name — charisma, competence, authenticity and the rest — **remain `[AUTHORED-DRAFT]`
game fiction and the debate screen keeps saying so.** Sourcing a real person's *name* does not
license inventing their *character*, and §0.4's ban on invented data is not suspended because a
public figure is famous. The comment at the staging site now says exactly that, so the next reader
cannot mistake a sourced label for a sourced number.

⚠ **Leader RELATIONS stay deferred and stay asserted ABSENT.** `CoalitionHarness.cs` proves by
reflection that no leader-relationship field exists anywhere in the coalition instrument. W-F6 does
not weaken that assertion — it was tempting to add a relations matrix now that the names are real,
and that is precisely the temptation §36 exists to refuse: a relationship nobody has measured is not
data.

### ⚠ The finding: MP has two leaders and the model has room for one

Miljöpartiet is led by **two språkrör** and has been since 1984. `CandidateProfile` is one person;
§15's debate seats one candidate; §29's leader compatibility compares one to one. **Taking "the
first one" would silently drop Per Bolund and print something false about a real party.** The file
names both, says the model carries one, and the gap is billed in `MISSING_PREREQUISITES.md` as a
design question for §15/§29 rather than resolved by a data item whose done-when is to source names.

### Decisions taken and logged (R-N1)

- **The vintage is 2022-09-11, not today.** Five leaders have changed since. A "current leaders"
  file is a different item; this one exists to name the people who fought the election the model
  replays. *Strikeable: a current file would be wrong for every screen in this prototype.*
- **The debate staging keeps its deliberately UNEQUAL attributes**, so the verdict has a margin
  worth reading. Only the labels changed.

### [AUTHORED-DRAFT] values

**None added.** The nine attributes per candidate were already `[AUTHORED-DRAFT]` and remain so;
W-F6 changes two labels from placeholder surnames to sourced full names.
