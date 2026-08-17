# PoliSim — Missing Prerequisites

**What this is:** every task that cannot proceed because something it needs does not exist yet. Created
2026-08-02 in the first consolidation pass.

**What this is not:** a backlog of work someone could pick up. Nothing here is startable. Work that is
merely *unbuilt* stays in `POLISIM_MASTER_ROADMAP.md`; work that is *waiting* lives here.

**The distinction that matters:** a task belongs here only if a named external party or a named upstream
task must act first. "Hard", "large" and "not scoped yet" are not blockers — Master Sequence items 6, 7
and 8 are weeks of work each and are **not** in this file, because nothing prevents starting them.

| Supplier | Items | Downstream |
|---|---|---|
| ~~**Elias — decision**~~ | ~~3~~ **0 — all resolved 2026-08-02** | — |
| ~~**Elias — database access**~~ | ~~16~~ **0 blocking** | ✅ **ALL FOUR BATCHES SOURCED 2026-08-02.** C1/C2/C3/C5 can be built. Three *quality debts* remain — see below — but no batch waits on a missing figure |
| ~~**Elias — visual review**~~ | ~~11~~ **0 — all confirmed 2026-08-02** | Step 5 CLOSED |
| ~~**Claude Design**~~ | **0 — delivered and imported 2026-08-02** | — |
| **Another task first** | ~~3~~ ~~2~~ **1** | Step C4 closure (waiting on the **parked fiscal-divergence pass** — see F1's 2026-08-12 reconciliation). *Round 4 scoping released 2026-08-02; cabinet portraits released 2026-08-17 by R4-4 (request written, D1 → E class)* |

---

# A. Waiting on Elias — a decision

## ✅ ALL THREE RESOLVED 2026-08-02. Nothing in section A is waiting.

Rulings and reasoning below, kept in full so none of these is reopened as an unanswered question later.

### A1 — RESOLVED: fix the rating thrash by REVIEW CADENCE, not by damping

**The primary recommendation (cap + multi-turn average) was rejected.** Elias took the alternative raised
almost in passing: the rating updates on a scheduled review cycle rather than every turn.

**Why damping was the weaker fix.** It makes the thrash *smaller* without removing why it exists — a
rating recomputed from a single turn's fiscal position will always track that position's volatility, and
every constant chosen to suppress that is a number nobody can justify from anything real. It also lands
directly on the term the 5-anchor calibration runs through, which is the risk this section identified.

**Why the review cycle is stronger — four reasons, all recorded:**
1. **It is what actually happens.** Agencies review sovereigns on a cycle rather than re-rating
   continuously as quarterly figures move. The scheduled review *is* the real-world mechanism that
   prevents real-world thrash, so modelling it reproduces the behaviour instead of approximating its
   absence.
2. **The machinery already exists.** Step A built the release-calendar and published-series system for
   exactly this shape — a value evolving continuously underneath, surfacing on a schedule.
3. **Precedent already in the game.** Central bank rate decisions run on ~8 scheduled meetings a year
   rather than continuously.
4. **It dissolves the problem by construction rather than tuning it.** Rating off a settled annual fiscal
   position is closer to what agencies do, so the 5-anchor calibration stays valid rather than needing
   re-derivation against a smoothed term.

**Implemented 2026-08-02 (`a4155ca`), and it produced a finding rather than a clean pass.**

- **5-anchor calibration: 5 of 5 PASS, unchanged** — run before the matrix as instructed, and now
  executable for the first time rather than existing only in a commit message.
- **Full matrix: 3,421 → 1,416 anomalies.** Reduced 59%, **not eliminated**. USA, Italy and Poland stayed
  at zero as required; the residual is Sweden 616, France 567, Germany 103.
- **The residual is not a rating defect.** The settled annual deficit ranges **−135.5% to +170.8% of GDP**
  because the underlying debt stock oscillates between 0% and ~45% within a year — the documented
  debt-to-zero bimodality, in exactly the documented set of countries. No review cadence can or should
  stabilise a rating over that input.

**The blocker therefore moves upstream — see section F below.** C4's own implementation is complete.

### A2 — RESOLVED: SWF emergency drawdown becomes a standalone tier-3 bill

**Recommendation accepted as written.** Emergency SWF drawdown uses 5d's existing tier-2/3 mechanism —
most naturally a fifth tier-3 type alongside Labor / CrimeJustice / Sector / Trade. Not bundled into the
annual budget; not fully exempt like the Fed/Eurozone carve-out.

Reasoning unchanged and needs no addition: real governments handle fiscal emergencies through expedited
votes rather than unilateral action, Norway's own GPFG withdrawal is an ordinary budget-process matter,
and this needs **zero new mechanism**.

**Still unbuilt, and worth doing soon despite blocking nothing** — the gap is live in the current build.
Since 5c, SWF rate and allocation changes ride the annual omnibus bill, so a genuine emergency can be
stuck behind a fiscal-year vote up to a year away. Elias's framing: *"a gameplay bug wearing the costume
of a design question."* Now tracked as live work in the roadmap, not here.

### A3 — RESOLVED: Cabinet appointments stay UNILATERAL

**No parliamentary vote to appoint a minister.** Reasoning recorded because none existed before:

- **It preserves a distinction the game already makes well.** Parliament gates *policy* — what the state
  does. Appointments are *executive* — who the player works through. One gate for both flattens a
  separation the gated-legislation model deliberately created.
- **There is already a cost.** Reshuffling carries an `ApprovalRating` hit, so Cabinet decisions have
  consequences without a second gate.
- **A vote would make Cabinet worse to play.** Interactive ministers bringing decisions are the point of
  Part A; a multi-week legislative process in front of every appointment turns a responsive system slow
  for no gameplay gain.
- **It is defensible in the real world.** Confirmation practice varies enormously across the six modelled
  countries; unilateral appointment is not unrealistic.

**Nothing to build.** The current behaviour is already unilateral, so this ruling confirms the code rather
than changing it.

---

<details>
<summary>Original section A text, as raised (kept for the record)</summary>

## A1. 🔴 Step C4's deficit term needs re-calibrating

**Task:** make the sovereign credit rating stop thrashing.

**Needs:** a ruling on how to damp the deficit term, then a re-run of the 5-anchor calibration check.

**Why it is blocked rather than merely unbuilt:** C4's `BurdenCurve` was calibrated against 5 of 5
verifiable real-world ratings (`76a8f35`), and the USA's AA+ *depends specifically* on its deficit
exceeding 3%. Every plausible fix — capping the contribution, averaging it over turns, rating off a
smoothed fiscal position — changes the exact term that calibration runs through. Choosing a smoothing
window without a decision would quietly invalidate the one thing that made C4 credible.

**The evidence:** `3d77b11`'s matrix run — 3,421 anomalies, every one a rating moving more than four
notches in a single turn. Sweden 1,761, France 1,117, Germany 240; USA, Italy and Poland zero. 282 are
full-ladder 16-notch moves. Present in plain `baseline` at both horizons.

**Recommendation:** cap the deficit contribution at ~2–3 notches *and* feed it a multi-turn average, then
re-run the 5-anchor check before the matrix. Also worth deciding whether a rating should update per turn
at all — an annual review cycle would dissolve the thrash by construction rather than damping it.

**Blocks:** Step C4 being called done. The tile ships meanwhile, correct for USA/Italy/Poland.

## ~~A2. SWF emergency drawdown fast-track~~ — ✅ RESOLVED *and* BUILT 2026-08-02 (`b1c077f`)

`SwfDrawdownBill`, the fifth tier-3 bill, exactly as recommended below: standalone, votable, zero new
mechanism. The original entry is kept because its reasoning is the spec the implementation followed.

**Task (original):** let an emergency SWF drawdown bypass the annual budget bill.

**Needs:** Elias to confirm or reject the recommendation below. **Load-bearing, not hypothetical** — SWF
rate/allocation changes have ridden the annual omnibus budget bill since 5c, so a genuine emergency can
currently be stuck behind that country's next fiscal-year vote, up to a year away.

**Recommendation (2026-07-31, unchanged):** make it a standalone bill using 5d's existing tier-2/3
mechanism — most naturally a fifth tier-3 type alongside Labor/CrimeJustice/Sector/Trade — rather than
bundling it into the budget, and **not** fully exempt like the Fed/Eurozone carve-out. Real governments
handle fiscal emergencies via expedited votes, not unilateral action; Norway's own GPFG withdrawal is an
ordinary budget-process matter. **Needs zero new mechanism.**

**Blocks:** nothing downstream. It is a live gameplay gap, not a dependency.

## A3. Cabinet appointment confirmation

**Task:** decide whether appointing a minister requires a parliamentary vote or stays unilateral.

**Needs:** Elias's ruling. No recommendation recorded — this is a pure design preference.

**Blocks:** nothing. Recorded so it is not silently dropped.

</details>

---

# B. ~~Waiting on Elias — database access~~ — ✅ EMPTY AS OF 2026-08-02

**Every figure that blocked a batch has been sourced**, across three sessions, via the Eurostat REST API,
the OECD SDMX API, BLS/FRED, and — for three items only — a stated, banded estimate under the fallback
ladder. Values, queries, dimension labels, status flags and confidence markers are all in
`POLISIM_SEED_DATA_MACRO_OVERHAUL.md`. **C1, C2, C3 and C5 are buildable.**

**🔴 WHAT REMAINS IS QUALITY DEBT, NOT GAPS — and the distinction matters.** None of these blocks a batch;
each would make one more trustworthy:

| Debt | Where | What would settle it |
|---|---|---|
| ~~**C5 is `[PRIMARY-UNANCHORED]`**~~ | seed §6 | ✅ **CLOSED 2026-08-02** — anchor found via DBnomics; both original seeds reproduce exactly; all six promoted to `[VERIFIED]` |
| **The real-wage row mixes THREE bases** | seed §5 | Re-source all six from OECD Taxing Wages 2025 (one basis, in SDMX). *Correct figures, incoherent set — the housing-overburden defect again* |
| **The AHD vintage behind C1's estimates is unrecorded** | seed §1 | Find the year of the four OECD anchors. **Unrecorded vintage is exactly what made 90.86 undecidable** — now the canonical example |

⚠ **Three C1/C2 figures are `[ESTIMATED]`, not sourced** — Italy/Sweden/Poland homeownership, Sweden real
wages, USA Gini. They are rung 3 of the fallback ladder, carry stated methods and bands, and are replaced
the moment real figures exist. **They are placeholders that play correctly, not facts.**

**The original section follows, retained because its basis warnings still govern any re-sourcing.**

---

**No session here has web search.** Every figure below must be sourced externally. **Do not invent, infer
from a range, or carry across from another basis.**

**Standing rule, three-for-three:** for any cross-country statistic, **assume an undocumented variant axis
exists** and record the basis alongside every value — indicator code, population base, threshold, year.
Housing overburden had 8 variants where its warning implied 3; youth unemployment 4 where it implied 2;
homeownership 4+ with no warning at all. A bare number is unfalsifiable later.

*Count reconciliation: 16 figures genuinely block a batch. `ELIAS_ACTION_LIST.md` previously said "17
blocking" — that total included C4's Poland rating, which is a validation anchor and blocks nothing.*

## ~~B1. Step C1 — Housing (3 figures)~~ — ✅ CLOSED 2026-08-02

Italy 74.4, Sweden 62.1, Poland 86.8 — all `[ESTIMATED]` from a four-country bridge off the Eurostat
population basis, 95% band ±7pp. See seed file section 1. **The basis warning below still governs any
re-sourcing**, and the AHD vintage behind the bridge is an open quality debt.

**Basis: OECD Affordable Housing Database, share of HOUSEHOLDS owning. This basis only.**

| Country | Need |
|---|---|
| **Poland** | `[PARTIAL]` — confirmed top-10 globally, but the ~87.9 in our files is a Eurostat *nationals* line, not same-basis |
| **Italy** | `[GAP]` |
| **Sweden** | `[GAP]` |

Have: USA 65.3, France 58.5, Germany 41.0. Same-basis sanity anchors: OECD avg 70.1, Slovakia 93.5,
Canada 68.6, Australia 62.7, Switzerland 38.2.

⚠ **The old indicative ranges — Italy ~72–73, Sweden ~63–65 — must not be used, even as sanity checks.**
They were on unknown bases, so they may be measuring something else entirely.

**Blocks:** Step C1 implementation, which has not started.

## ~~B2. Step C2 — Inequality + real wages (3 figures)~~ — ✅ CLOSED 2026-08-02

USA Gini **39.5** `[ESTIMATED]`, Sweden real wages **1.3** `[ESTIMATED]`, USA real wages **1.0**
`[VERIFIED]`. ⚠ **The real-wage ROW now mixes three bases** — a quality debt, not a gap; see seed §5.

| Stat | Country | Note |
|---|---|---|
| Real wage growth 2024 | **Sweden** | `[GAP]` — nominal was 3–5%, real not sourced |
| Real wage growth 2024 | **USA** | `[GAP]` — OECD calls it "stable"; real household income/capita +0.3% Q4 2024 |
| Gini | **Poland** | `[PARTIAL]` — Statista ~29; prefer a direct Eurostat figure to match the other four |

⚠ **Real wage growth has NOT been variant-checked** and is the last unverified indicator before C3. Watch
for real vs nominal, wages vs household income (Germany and Italy saw *falling* real household income
while real wages rose in 2024), and gross vs net.

Also required before seeding: **normalize the Gini scale.** US sources use 0–1 with different methodology
from Eurostat's 0–100.

**Blocks:** Step C2.

## ~~B3. Step C3 — Youth unemployment + life expectancy (6 figures)~~ — ✅ FULLY SOURCED 2026-08-02

All `[VERIFIED]` except France and Poland life expectancy, which carry provisional status flags. USA
youth unemployment (10.0 / 9.5, 16–24 basis) closed it last.

**Youth unemployment — 15–24 RATE only** (not ratio, not 15–29):

| Country | Note |
|---|---|
| **Germany** | `[GAP]` — the 3.6 found during sourcing is a **ratio**, do not use |
| **Poland** | `[GAP]` — the 3.5 found is likewise a **ratio** |
| **USA** | `[GAP]` — OECD-wide youth rate 11.2% (Jul 2025) as an anchor |

Have: Italy 20.1, France 18.7, Sweden 22.2 — all confirmed 15–24 rates.

**Life expectancy at birth:** France `[GAP]` (~83), Germany `[GAP]` (~81), Poland `[GAP]` (~78). Have:
USA 79.0, Italy 84.1, Sweden 84.1. Low variant risk — period vs cohort is the main axis.

**Blocks:** Step C3.

## ~~B4. Step C5 — Productivity (4 figures)~~ — ✅ ALL SIX SOURCED 2026-08-02. C5 IS NOT BLOCKED.

**Sourced exactly from the OECD SDMX API**, all six countries, 2022, one basis, one vintage — see seed
file section 6 for values, the fully-specified nine-dimension query, and why they carry
`[PRIMARY-UNANCHORED]` rather than `[VERIFIED]`.

**C5 can be built now.** What remains is a *confidence upgrade*, not a blocker: one exact,
independently-published OECD figure — any country, any year — reproduced in a session with live SDMX
access would satisfy rule 5f-bis and promote all six to `[VERIFIED]`. Every candidate anchor already in
the seed file was tried and failed (France 90.86, USA ~97, OECD average ~67.5 — none reproduce on either
price basis). The OECD Compendium of Productivity Indicators is the most likely home for a usable one.

⚠ **The `[GAP]` count in this document's header table is now 16 → 12.** C5's four are closed.

**The original entry, for the record:**

**Basis: OECD PPP, GDP per hour worked, all six countries on that one basis. Non-negotiable.**

Germany `[GAP]`, Italy `[GAP]`, and **re-source Sweden (~70) and Poland (~24.5)** — both came from
Statista and are almost certainly not PPP-adjusted on the same footing. Poland at $24.5 against an OECD
PPP average of $67.5 is implausible. *Confirmed emphatically: the real figures are 89.95 and 54.09 —
Poland was off by more than 2×.*

Have on OECD PPP: USA ~97, France 90.86. *Neither reproduces against the primary source; see seed file
section 6 on the 90.86 provenance question.*

**Blocks:** Step C5. *C5 exists because the directive named seven new stats and batched only six —
productivity appeared in no batch. An authoring error, corrected 2026-08-01, not a decision to drop it.*

### DECIDED 2026-08-02 (Elias, delegated) — KEEP C5, lowest priority of the four blocked batches

**Not cut.** The question was whether productivity is worth keeping given its sourcing problems. It is —
but it goes last.

**Why cutting it would achieve nothing.** C1, C2, C3 and C5 are blocked on **the same** prerequisite:
database access. Dropping C5 does not unblock C1–C3, frees no work that could start today, and does not
reduce the number of trips to the database — the figures would be sourced in one session either way.
**A cut that unblocks nothing is not a simplification.**

**Why it goes last of the four.** The OECD's own caution — that cross-country comparison of GDP per hour
worked is not meaningful, and the valid use is a country against its own past — is a real mark against it.
That does not make the stat worthless (the framing actually suits this game: seed each country's level,
then let the player watch their own trajectory), but among four batches competing for one scarce resource
it has the weakest claim.

**Revisit only if database access never materialises.** If the other three land and C5's four figures
remain unobtainable, dropping it then is reasonable — decided against evidence rather than in advance.

## ~~B5. Step C4 — Poland's sovereign rating~~ — ✅ CLOSED 2026-08-02: **A− / A2 / A−**

A **validation anchor only**, not a seed, since C4 is derived — so it never blocked anything.

🔴 **But it turned out to carry a model finding: Poland BREAKS the mapping's monotonicity in the opposite
direction to the USA** — lower debt than Germany, four notches worse. `RiskPremiumSensitivity` only
discounts and never penalises, so `CreditRatingSystem` will over-rate Poland. **Run the 5-anchor check as
a six-anchor check and expect Poland to fail first.** This belongs to F1's closure work; see seed §7.

## B6. Deferred — housing cost overburden (4 items, only if re-adopted)

Italy, France and Poland are `[BOUNDED]` 4.0–9.0 — real, honestly derived from Eurostat naming only
countries above 9.0 and below 4.0, but **not values and never to be seeded as such**. Exact figures need
direct `ilc_lvho07a` database access; summary articles cannot produce them.

Plus the **USA methodology decision**: Eurostat measures >40% of disposable income, US convention >30% or
>50%. Nothing matches. Options: import with the bias documented, mark USA `[GAP]` and seed the EU five
only, or use homeownership for the USA (65.3 exists and is comparable).

**Blocks:** nothing. Overburden lost to homeownership as C1's primary metric on data-honesty grounds.

## Not gaps — do not spend time on these

- **House Price Index** — resolved by convention: seed all six at index 100 and let divergence emerge.
- **Tier 0 derived stats** — computed from tracked values, no seeds needed.

---

# C. Waiting on Elias — visual review

## ✅ NOTHING. Section C is empty as of 2026-08-02.

**All eleven items are confirmed.** Elias reviewed them live as USA, then re-reviewed the five that had
failed or carried caveats — 3, 7, 8, 9 and 10 — and passed all five. Full record in `COMPLETED.md`
section 16. **`VISUAL_REVIEW_BACKLOG.md` has been deleted**, its content migrated, per the standing rule
that an emptied document drifts back into use.

**Master Sequence step 5 is CLOSED**, which releases D2 (Round 4 scoping) below.

**The two defects behind items 5 and 6 did not block closure, and are still live** — the label-clipping
class, tracked in the roadmap as P4. They belong there rather than here: a known defect is not an
unconfirmed screen, and nothing about them waits on a named party.

⚠ **Item 11 carries a known model defect** — the C4 thrash in A1 above. Review it on USA, Italy or
Poland; the defect is logged and should not be re-reported as a review finding.

---

# D. Waiting on another task

## 🟡 D1. Cabinet portraits — ✅ UNBLOCKED by R4-4; request **SENT 2026-08-17** (Elias, per the R4-5 directive). Awaiting delivery

**Task:** portrait art for Defense, Foreign Affairs and Education ministers — 9 portraits, request in
`CLAUDE_DESIGN_ASSET_REQUEST.md` §5, filenames derived from the signed names.

**History:** was blocked on the portfolios being authored (no names → no derivable filenames). R4-4
authored all nine (signed list, ruling R1); the request was written the same day and Elias sent it.
Delivery lands per the E2 convention when it lands — import per §3's treatment rules,
`ImporterSettingsCheck`/`DeliveredAssetCheck` pick up the 18 files (9 × 2).

**Blocks:** nothing. The game renders the procedural placeholder for the nine until art lands —
coverage of the EXISTING 16 (9 ministers + 7 Fed chairs) is unaffected.

## ~~D2. Round 4 scoping~~ — ✅ RELEASED 2026-08-02. Not blocked; moved to the roadmap as live work.

Its only gate was Master Sequence step 5, which closed when Elias confirmed the last five review items.
The dependency was real rather than cautious — Round 4 is scoped after step 5 so anything new is built
against the gated-legislation model from day one instead of being retrofitted — and it is now satisfied.

⚠ **Still gated on one thing, which is NOT a blocker in this register's sense:** the agreed execution
order puts item 8 (save/load) and item 7 (Continuous Time Phases 1–5) *before* Round 4, so that new
systems are built against both finished foundations rather than converted to daily granularity
afterwards. That is a sequencing decision already made, not a party to wait on.

---

# E. Waiting on Claude Design

## 🟡 E2. `mark_party_us_lib` — requested in `CLAUDE_DESIGN_ASSET_REQUEST.md` §1G, **WRITTEN, NOT SENT**

⚠ **This section read "✅ NOTHING, empty as of 2026-08-02" while §1G already requested this mark — the
THIRD instance of the `icon_stat_interestrate` class**: two documents describing the same outside world,
one stale, neither wrong when written. Rule 12 exists for exactly this, and it recurred the same day
rule 12 was cited twice.

**What is needed:** one sprite, `mark_party_us_lib.png`, @2× 128×128, **white-on-alpha** — the tinted
class, per §3.0a. Copying `emblem_party_*`'s importer settings is how four marks came to be
block-compressed. Original abstract art under **rule 9a**: never the party's own mark, never a
recognisable derivative of one.

**Why it is a real gap:** the US seed carries four parties and two marks. `PartyMarkCoverageCheck`
enumerates the party list and reports `2 without one` — **one is this request, one is deliberate**
(`Other and independent` is a residual bucket, not a party, and stays unmarked by decision).

⚠ **STATUS IS *WRITTEN*, NOT *SENT*.** It exists in a document and has not been transmitted to Claude
Design. **A request in a document and not in anyone's inbox is the same failure one step earlier**, so
this entry exists to keep that distinction visible. It closes when the sprite is on disk and
`PartyMarkCoverageCheck` reports it resolving at RGBA32.

**Blocks nothing today** — the Libertarian row renders as text, which is the designed degradation, and
the screen it appears on exists only on `stranded/politics-elections`.

## 🟡 E3. Design's rasterization diff — carried, previously recorded only in the request document

`CLAUDE_DESIGN_ASSET_REQUEST.md` §1F.1: Design asked that their strip-cut PNGs be diffed against our own
rasterization once before the pipeline is trusted. **No rasterizer exists on this machine**, so it has
never been run. Recorded here as well as there, because a blocker living in one document is the same
cached-status shape as E2 above. It closes when a rasterizer exists — **not** when the sprites look right
in a capture, which they already do.

**E1 — `icon_stat_interestrate`. DELIVERED AND IMPORTED**, the same day the request was sent. Elias
pointed out that it had already arrived, in `Policy rate icon design.zip` at the project root — this
register still said "awaiting delivery" because nothing watches for a delivery landing.

Imported to `Assets/Resources/Art/UI/Stats/` with a hand-written `.meta`, verified by loading through
`Resources.Load` rather than by finding the file on disk. Zip archived. Details in `COMPLETED.md`.

**The recurring pattern is worth naming, because it happened twice:** a delivered asset sitting unimported
while a document reports it as outstanding. The other was `menu_pattern_tile.png`, delivered in "PoliSim
GUI redesign.zip" — **also imported and archived 2026-08-02, so the project root now holds no zips at
all.** That state is itself the signal: a zip at the root means something in it is unfinished.

**A delivery is not self-announcing**, which is why both loops are now closed by checks rather than by
memory: `StatIconCoverageCheck` enumerates `StatNodeId` and reports any icon name that does not resolve,
and `DeliveredAssetCheck` compares every zip's contents against what exists under `Assets/`.

---

# F. Waiting on an upstream simulation defect

## F1. 🔴 Step C4's CLOSURE — SUPERSEDED 2026-08-11: it now waits on the UNBOUNDED DEBT DIVERGENCE work

⚠ **RECONCILED 2026-08-12.** This entry read *"the blocker is RESOLVED as of 2026-08-02. Awaiting
Elias's sign-off"* for eleven days after Elias's 2026-08-11 ruling superseded it (roadmap, Open
Questions): **the 1000-turn matrix showed all six countries' debt still climbing — every "equilibrium"
below is a WAYPOINT measured at 120 turns — and C4's deficit-term reading never settles, so its closure
waits on the fiscal-engine divergence work** (diagnosed: interest compounding against an asymmetrically
bounded stabiliser; parked pending its own dedicated pass, per `60233af` "the re-sweep needs its own
pass"). The supplier for this entry is therefore *that parked pass*, not a sign-off. Everything below
is retained as the measurement record it is, with its horizons — the 98.7% anomaly reduction was real
and remains the largest single fiscal improvement recorded; it closed the THRASH, not the trajectory.

**The original 2026-08-02 entry follows.**

**Rating anomalies across the full matrix: 1,416 → 19.** A 98.7% reduction, and the deficit-term
volatility that blocked C4 is gone with it. Nothing about C4 itself was changed — the fix was entirely
upstream, in how SWF returns reach the budget.

| Stage | `CreditRating moved` | `DebtToGdpRatio swung` |
|---|---|---|
| Original | 1,416 | 6,225 |
| Debt floor removed | 1,394 | 2,507 |
| SWF returns inside the multiplier | 1,020 | 3,508 |
| **Structural draw (smoothing)** | **19** | **140** |

**The decisive change was the double-count fix**, found while implementing the smoothing ruling: the
realised return was added to the fund's assets *and* booked as government revenue, so the money existed
twice. The budget now receives a 3%/year structural draw **withdrawn from** the fund — Norway's own
fiscal rule — so it moves rather than duplicating, and is smooth by construction.

**Sanity-checked as fixed rather than frozen:** no country pins the fiscal reaction multiplier any more
(was 104/120 turns for Sweden, 51/120 for France), no country goes net-creditor, and trajectories still
move — Sweden ~~settles~~ **reads at 120 turns** near 10.5% of GDP, France 92.3%, Germany 38.6%, Italy
115.2%, Poland 29.9%, USA 142.2% *(⚠ waypoints, not equilibria — the 1000-turn matrix of 2026-08-11
shows all six still climbing; "settles" was this document using the word the roadmap has since
banned)*. Inflation, unemployment and interest-rate anomaly counts are byte-identical throughout, which
is the evidence of no leakage into the macro engine.

⚠ **One thing worth Elias's eye, and it is a NEW question rather than a leftover:** Sweden's debt ratio is
now very flat — 13.3% → 10.7% across 120 turns. That is a plausible equilibrium for a country whose fund
draw roughly covers its deficit, but real debt ratios move more than that. **Possibly too quiet**, and a
different question from the one this fix addressed.

**What remains for closure: Elias confirming the rating behaves acceptably in play.** The defect is gone
from the logs; whether the tile now reads well is a judgment about the screen.

### The original entry, retained

⚠ **UPDATED, and the update is the point: the debt-to-zero bimodality is FIXED and was not the cause.**
The floor came off, debt-swing anomalies fell 60% (6,225 → 2,507) — and rating anomalies moved 1.6%
(1,416 → 1,394). Two independent measurements agree the debt stock is no longer driving the thrash;
`DebtClampDiagnostic` now reports 0 year-over-year notch moves in 117 years for four of the six countries.

**C4's closure therefore waits on the DEFICIT term's volatility**, which the A1 write-up already suspected
when it recorded a settled annual deficit ranging −135.5% to +170.8% of GDP. The floor was hiding it by
making the debt stock's own noise too large to see past. Everything below about C4 itself still stands —
the implementation is complete and the 5 anchors still pass 5 of 5, re-confirmed after the debt fix.

### The original entry, kept because its reasoning about what blocks what still applies

**Task:** close Step C4.

**Step C4's implementation is COMPLETE, and the cadence fix worked as intended.** Nothing about the
rating remains to be built or tuned. The scheduled annual review (`a4155ca`) does exactly what Elias's A1
ruling specified: it reads a settled year-over-year fiscal position instead of one turn's budget balance,
it changed *when* the rating is computed rather than *how*, and the 5-anchor calibration consequently
still passes **5 of 5, unchanged**. This entry exists because a finished feature is being held open by
something that is not the feature.

**Needs:** the debt trajectory for Sweden, France and Germany to stop oscillating between 0% and ~45% of
GDP. This is a **pre-existing simulation-model defect**, documented long before C4 existed — see
CLAUDE.md's "SpendingLine Amount Ceiling — Debt-to-Zero Fix", and roadmap failure pattern 4 (bimodal
attractors).

### C4 is the first instrument that makes this defect visible to a player

**This is the part worth carrying forward.** The debt-to-zero bimodality has been known for a while and
has, until now, been a **log-only** finding: it lived in anomaly counts, batch-run summaries and
CLAUDE.md prose. Nothing on screen reported it. A player could run a 100-turn game as Sweden and never be
told their national debt had gone to exactly zero and stayed there.

The credit rating changes that. It sits in the dashboard tile grid, visible on every tab, and it reports
its input faithfully — so a debt stock swinging 0% to 45% and back inside a year now surfaces as a rating
visibly collapsing and recovering. **The defect did not get worse; it got a display.**

Two consequences:
- **Its priority should rise.** It has gone from a background modelling concern to something that blocks
  a step AND is player-visible. It was never a step-blocker before.
- **The rating is doing its job.** A derived stat that stayed calm while its inputs did this would be the
  broken one. Damping it — the option rejected in A1 — would have returned the defect to log-only.

**Why it blocks C4 specifically.** The settled annual deficit the review reads ranges **−135.5% to
+170.8% of GDP**, because it is derived — correctly — from a debt stock that collapses to exactly 0.00%
and spikes back to ~44% inside a year.

**A sovereign whose debt genuinely moved like that would be downgraded repeatedly.** The rating is
reporting its input faithfully; the input is what is wrong. Damping the rating to hide it was explicitly
rejected in A1, and would have buried this.

**Evidence:** `Logs/a1_matrix_final_20260802.log`. Sweden's `DebtToGdpRatio` in plain `baseline`:
21.8% (turn 1) → 0.90% (turn 25) → **0.00%** (turns 50, 75, 100). The three affected countries are exactly
the documented set; USA, Italy and Poland have well-behaved debt and produce **zero** rating anomalies.

**Blocks:** Step C4 closure only. The rating tile ships and is correct for USA, Italy and Poland.

**Not blocked on Elias** — this is ordinary (if substantial) simulation work, listed here because C4's
completion genuinely waits on another task rather than on more rating work.
