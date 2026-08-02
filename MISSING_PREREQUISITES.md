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
| **Elias — decision** | 3 | Step C4 closure, SWF emergencies, Cabinet appointments |
| **Elias — database access** | 16 blocking + 1 anchor | Steps C1, C2, C3, C5 |
| **Elias — visual review** | 11 | Master Sequence step 5 closure |
| **Claude Design** | 1 | Cosmetic only |
| **Another task first** | 2 | Cabinet portraits, Round 4 scoping |

---

# A. Waiting on Elias — a decision

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

## E1. `icon_stat_interestrate`

**Task:** the Interest Rate chip on B2's contextual stat row draws no icon.

**Needs:** one 64×64 PNG. Full spec in `CLAUDE_DESIGN_ASSET_REQUEST.md`.

**Verified as the only outstanding asset (2026-08-02)**, not assumed: every literal icon name requested
anywhere in `Assets/Scripts` was cross-referenced against the 84 files on disk, and this was the sole
miss. Portrait and area-icon coverage is complete.

**Blocks:** nothing. `IconLibrary` returns null for a missing sprite and the chip's layout shifts left,
which is correct — a placeholder would imply the wrong stat.
