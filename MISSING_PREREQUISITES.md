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
| **Elias — database access** | 16 blocking + 1 anchor | Steps C1, C2, C3, C5 |
| **Elias — visual review** | ~~11~~ **7 open** (4 closed 2026-08-02) | Master Sequence step 5 closure |
| ~~**Claude Design**~~ | **SENT 2026-08-02** — awaiting delivery | Cosmetic only |
| **Another task first** | 3 | Cabinet portraits, Round 4 scoping, Step C4 closure |

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

## A2. SWF emergency drawdown fast-track

**Task:** let an emergency SWF drawdown bypass the annual budget bill.

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

# B. Waiting on Elias — database access

**No session here has web search.** Every figure below must be sourced externally. **Do not invent, infer
from a range, or carry across from another basis.**

**Standing rule, three-for-three:** for any cross-country statistic, **assume an undocumented variant axis
exists** and record the basis alongside every value — indicator code, population base, threshold, year.
Housing overburden had 8 variants where its warning implied 3; youth unemployment 4 where it implied 2;
homeownership 4+ with no warning at all. A bare number is unfalsifiable later.

*Count reconciliation: 16 figures genuinely block a batch. `ELIAS_ACTION_LIST.md` previously said "17
blocking" — that total included C4's Poland rating, which is a validation anchor and blocks nothing.*

## B1. 🔴 Step C1 — Housing (3 figures)

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

## B2. Step C2 — Inequality + real wages (3 figures)

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

## B3. Step C3 — Youth unemployment + life expectancy (6 figures)

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

## B4. Step C5 — Productivity (4 figures)

**Basis: OECD PPP, GDP per hour worked, all six countries on that one basis. Non-negotiable.**

Germany `[GAP]`, Italy `[GAP]`, and **re-source Sweden (~70) and Poland (~24.5)** — both came from
Statista and are almost certainly not PPP-adjusted on the same footing. Poland at $24.5 against an OECD
PPP average of $67.5 is implausible.

Have on OECD PPP: USA ~97, France 90.86.

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

## B5. Step C4 — Poland's sovereign rating (1 figure, NON-blocking)

`[GAP]`, typically A range. A **validation anchor only**, not a seed, since C4 is derived. Its absence
blocks nothing; it would strengthen the calibration check that A1 above will need.

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

## ✅ REVIEWED 2026-08-02. Four items closed; seven still open.

**Elias reviewed all eleven live, as USA.** Items 1, 2, 4 and 11 passed clean and moved to `COMPLETED.md`
section 14. **Master Sequence step 5 still does NOT close** — it needs 1–9, and 3, 7, 8 and 9 failed.

**What each remaining item now waits on — they are no longer all the same thing:**

| Item | Waits on |
|---|---|
| 9 (black screen) | **Nothing — FIXED** (`e9e3f6a`). Waits only on Elias re-looking |
| 10 (B2 stat row) | Elias re-looking **after advancement** — its sparklines never rendered at turn 0, and the sparkline is what crashed item 9 |
| 3 (unit bug) | **Me** — investigated, one approach recommended; needs a go-ahead to change ~21 sites |
| 7, 8 (unreadable graphs) | **Item 3 landing first.** Cannot be judged while an axis reads "29k" for $29T |
| 5, 6 (clipping) | **Me** — audited, shared helper recommended |

*This section is no longer one undifferentiated "waiting on Elias" block: three of the seven wait on work,
not on review.*

**11 items, detailed in `VISUAL_REVIEW_BACKLOG.md`.** Not duplicated here; that document is the live
list and holds the per-item look-at/judgment/if-rejected detail.

**Eight need no game advancement** (items 1–6, 10, 11) — enter Play mode and they are on screen. Items
7–9 need one, two and three-to-four turns respectively and should be done in one sitting, because there
is still no save/load and closing Unity loses the advancement.

**Blocks:** **Master Sequence step 5 cannot close until items 1–9 are confirmed.** That in turn is what
gates scoping Round 4 (item 6) — see D2. Items 10 and 11 belong to step 9 and gate nothing.

⚠ **Item 11 carries a known model defect** — the C4 thrash in A1 above. Review it on USA, Italy or
Poland; the defect is logged and should not be re-reported as a review finding.

---

# D. Waiting on another task

## D1. Cabinet portraits for the three unimplemented portfolios

**Task:** portrait art for Defense, Foreign Affairs and Education ministers.

**Needs:** those three portfolios to be *authored first*. Portrait filenames derive from each minister's
generated name (`portrait_cabinet_<portfolio>_<slug>`), and those ministers do not exist yet — Part A
deliberately implemented 3 of 6 portfolios. **The request cannot be written without inventing names**,
which would violate the derive-filenames-from-real-values rule the asset requests are built on.

**Blocks:** nothing. Current coverage is complete — 9 ministers + 7 Fed chairs = 16 portraits, all
present and name-matched.

## D2. Round 4 scoping

**Task:** scope Master Sequence item 6.

**Needs:** Master Sequence step 5 to close, which needs section C's visual reviews 1–9.

**Why it is a real dependency rather than caution:** the roadmap's own rule is that Round 4 is scoped
only once step 5 is done, so anything new is built directly against the gated-legislation model from day
one rather than being retrofitted.

---

# E. Waiting on Claude Design

## E1. `icon_stat_interestrate` — ✅ REQUEST SENT 2026-08-02, awaiting delivery

**Task:** the Interest Rate chip on B2's contextual stat row draws no icon.

**Status: sent.** CLAUDE_DESIGN_ASSET_REQUEST.md has gone to Claude Design. This is no longer waiting on
Elias — it is waiting on delivery, then a security review and import following the established pattern.

**Needs:** one 256×256 PNG (renders at 22px). Full spec in CLAUDE_DESIGN_ASSET_REQUEST.md.

**Verified as the only outstanding asset (2026-08-02)**, not assumed: every literal icon name requested
anywhere in `Assets/Scripts` was cross-referenced against the 84 files on disk, and this was the sole
miss. Portrait and area-icon coverage is complete.

**Blocks:** nothing. `IconLibrary` returns null for a missing sprite and the chip's layout shifts left,
which is correct — a placeholder would imply the wrong stat.

---

# F. Waiting on an upstream simulation defect

## F1. 🔴 Step C4's CLOSURE waits on the debt-to-zero bimodality — C4 itself is finished

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
