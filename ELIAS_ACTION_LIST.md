# Everything waiting on Elias

**Compiled 2026-08-01. Section A closed 2026-08-01; counts refreshed 2026-08-02.** Four categories:
**decisions** (open questions), **visual reviews**, **figures to source**, and **external deliveries**.
Nothing here can move without you.

Ordered within each section by what unblocks the most downstream work.

---

# A. DECISIONS — ✅ ALL FIVE ANSWERED (`8291662`), NONE LIVE

**Nothing in this section is waiting on you any more.** Kept below as the record of what was asked and
why, since each answer is load-bearing for code that now exists. The authoritative resolutions live in
`POLISIM_MASTER_ROADMAP.md`'s Open Questions section.

| # | Decision | Built against it |
|---|---|---|
| A1 | **Counting shim** for `SimulationRandom` across save/load — reversible beats permanent | Nothing yet; implement with Master Sequence item 8 |
| A2 | Swing coverage **stays at five fields**; the fix is documentary — see CLAUDE.md's READ FIRST note | `CLAUDE.md` header |
| A3 | Policy screens show **LIVE**, not published | `5701a04`, wired `4869476` |
| A4 | **Keep all** `PeriodClosingValues`, no pruning | Flatten-on-save design recorded |
| A5 | **Build C4 out of order** — genuinely independent *and* externally blocked, both required | `76a8f35` |

*The original text of all five follows, unedited.*

## The five as originally asked

## A1. How should `SimulationRandom` stream position survive a save/load? 🔴 blocks save/load

`System.Random` cannot expose or restore its internal position. Saving the master seed and re-seeding on
load rewinds **every stream to turn zero** — a game reloaded at turn 50 replays the events, Fed-chair
candidates and cabinet decisions already seen, in order, while still looking deterministic. This is a
*replay*, not a reroll, so it is a correctness failure rather than a save-scum exploit — and easy to
misdiagnose as the latter.

| Option | Cost |
|---|---|
| **Counting shim** — record draws per stream, fast-forward on load | O(draws) loop on every load, forever. Preserves every existing baseline |
| **Serializable PRNG** (64-bit xorshift) — state is two integers | Constant-time load, state visible in diffs. **Breaks every recorded baseline once** |

**My recommendation: the counting shim** — reversible, and this project has already absorbed several
baseline discontinuities in one day. *Raised rather than decided because both costs are permanent.*

## A2. Should the harness's swing check cover more than 5 of 29 tracked values?

`CheckSwing` covers GDP, Unemployment, Inflation, InterestRate and DebtToGdpRatio only — because
`Snapshot` stores just those five. A runaway in PovertyRate, Population, Consumption, Investment,
CrimeIndex or 19 others produces **zero swing anomalies and a clean-looking run**. `CheckFinite` is
complete at 29/29, so NaNs cannot slip through.

The problem is that "N anomalies detected" is quoted throughout this project's history as a
whole-simulation health signal, and it is a 5-field measure.

| Option | Cost |
|---|---|
| Extend to all 29 | Third baseline discontinuity; several fields (NetMigrationRate, PopulationGrowthRate) legitimately cross 20% and would bury real signal |
| Extend to a chosen subset with per-field thresholds | Requires choosing ~24 thresholds — a design task |
| **Leave at five, stop calling it a health measure** | **Free.** Removes the misreading, fixes nothing |

*No recommendation — this is a judgement about how much you want the harness to police.*

## A3. Should B2 show LIVE or PUBLISHED values on policy screens? 🔴 blocks B2 rendering

**This is a deviation I made and should have raised.** The directive says each policy screen shows the
stat's *"current published value."* I built `ReadLiveValue` and documented a rationale in code instead.

In fairness the instruction is only partly satisfiable: **`PublishedStat` has 6 members against
`StatNodeId`'s 18**, so 12 of 18 policy-screen stats have no published series to show at all.

| Option | Consequence |
|---|---|
| **Live** (what I built) | Shows what your levers are doing *now*. Consistent across all 18 stats. Diverges from the directive |
| **Published where available, live otherwise** | Follows the directive where possible, but the same row mixes two meanings — arguably the worst option |
| **Published only** | Directive-faithful; only 6 of 18 stats can appear, gutting most policy screens |

*My recommendation: live, with the published/lagged view staying on the Statistics tab where it belongs.*
Mixing a lagged preliminary figure into a "what am I doing right now" panel misrepresents it.

## A4. Retention rule for `PublishedData.PeriodClosingValues`?

Grows one entry per stat per period, forever, and **must** be saved — revisions converge on it, and
omitting it reintroduces an already-fixed bug on every load. Once every publication referencing a period
is `Final`, is that period's closing value still worth keeping? Cheap to answer now, awkward once save
files exist in the wild.

## A5. Should C4 (credit rating) be built out of order?

C1, C2 and C3 are all blocked on figures. **C4 needs no seed data at all** — it is `[DERIVE]`, computed
from debt-to-GDP, deficit trajectory and growth, reusing the existing reserve-currency mechanism.

Building it now means departing from the roadmap's "top to bottom, do not skip ahead" rule; not building
it means Step C stalls entirely until you have database access. *My recommendation: build it*, since the
blocker on C1–C3 is external and may persist.

---

# B. VISUAL REVIEWS — 10 items, one session

Full detail in `VISUAL_REVIEW_BACKLOG.md`. **Seven need no game advancement**; the rest fit one sitting
with three fast-forwards. Review **item 2 before item 3** — the restructure gates the graph redesign.

| # | Item | Advancement |
|---|---|---|
| 1 | Statistics nav icon sizing *(resized after your "colored speck" note; never re-checked at 1.0x)* | none |
| 2 | Statistics restructure — Domestic/International, graphs out of left column | none |
| 3 | Published graph, empty state | none |
| 4 | Amber draft cue — drag any slider | none |
| 5 | Policy/Laws tab restyle | none |
| 6 | Budget tab full-screen | none |
| 7 | First release + reporting lag | 1 turn → 2026-05-02 |
| 8 | **Revision treatment — the payoff for Step A + B1** | 2 turns → 2026-08-31 |
| 9 | Budget Process restyle — **closes Master Sequence step 5** | 3 turns (USA) / 4 (EU) |
| 10 | **B2 stat row** *(new 2026-08-02)* — the only item where rejection means something is factually wrong, not just ugly | none |

**Item 8 is the one that matters.** Everything else asks "does it look right." That one asks whether a
player can *see* a revision happen without being told. If not, Step A is built but not communicated.

**Keep the `[DEBUG]` dump** (`GameController.cs:2589`) until item 8 passes — it is your cross-check that
the picture matches the data. Tell me when it passes and I'll strip it.

*Note: no save/load exists, so closing Unity mid-review loses advancement. Items 1–6 are free to redo;
do 7–9 in one sitting.*

---

# C. FIGURES TO SOURCE — 17 blocking, 4 deferred

**Standing rule, now three-for-three:** for any cross-country statistic, **assume an undocumented variant
axis exists**, and record the basis alongside every value — indicator code, population base, threshold,
year. A bare number is unfalsifiable later. Housing overburden had 8 variants where its warning implied
3; youth unemployment 4 where it implied 2; homeownership 4+ with no warning at all.

## C1 — Housing 🔴 blocks Step C1 (3 figures)

**Basis: OECD Affordable Housing Database, share of HOUSEHOLDS owning. This basis only.**

| Country | Need |
|---|---|
| **Poland** | Currently `[PARTIAL]` — top-10 globally confirmed, but the ~87.9 in our files is a Eurostat *nationals* line, not same-basis |
| **Italy** | `[GAP]` |
| **Sweden** | `[GAP]` |

Have: USA 65.3, France 58.5, Germany 41.0. Same-basis sanity anchors: OECD avg 70.1, Slovakia 93.5,
Canada 68.6, Australia 62.7, Switzerland 38.2.

⚠ **The old indicative ranges — Italy ~72–73, Sweden ~63–65 — must not be used, even as sanity checks.**
They were on unknown bases, so they may be measuring something else entirely.

## C2 — Inequality + real wages (3 figures)

| Stat | Country | Note |
|---|---|---|
| Real wage growth 2024 | **Sweden** | `[GAP]` — nominal was 3–5%, real not sourced |
| Real wage growth 2024 | **USA** | `[GAP]` — OECD calls it "stable"; real household income/capita +0.3% Q4 2024 |
| Gini | **Poland** | `[PARTIAL]` — Statista ~29; prefer a direct Eurostat figure to match the other four |

⚠ **Real wage growth has NOT been variant-checked** and is the last unverified indicator before C3. Watch
for: real vs nominal, wages vs household income (the file already warns these diverged in 2024 —
Germany and Italy saw *falling* real household income while real wages rose), and gross vs net.

## C3 — Youth unemployment + life expectancy (6 figures)

**Youth unemployment — 15–24 RATE only** (not ratio, not 15–29):

| Country | Note |
|---|---|
| **Germany** | `[GAP]` — the 3.6 found during sourcing is a **ratio**, do not use |
| **Poland** | `[GAP]` — the 3.5 found is likewise a **ratio** |
| **USA** | `[GAP]` — OECD-wide youth rate 11.2% (Jul 2025) as an anchor |

Have: Italy 20.1, France 18.7, Sweden 22.2 — all confirmed 15–24 rates.
*Minor file inconsistency: the table still lists Sweden as `[GAP]` while the note below it records 22.2
as `[VERIFIED]`. The note is correct.*

**Life expectancy at birth** — France `[GAP]` (~83), Germany `[GAP]` (~81), Poland `[GAP]` (~78).
Have: USA 79.0, Italy 84.1, Sweden 84.1. Likely low variant risk (period vs cohort is the main axis).

## C4 — Credit rating (1 figure, non-blocking)

**Poland's sovereign rating** — `[GAP]`, typically A range. Used only as a **validation anchor**, not a
seed, since C4 is derived. Its absence does not block building C4.

## C5 — Productivity (4 figures) ⚠ has no batch assigned

**Flagging a gap in the directive itself:** the seven new stats are housing, inequality, real wages,
productivity, youth unemployment, life expectancy and credit rating — but the batches are C1 housing,
C2 inequality+real wages, C3 youth+life expectancy, C4 credit rating. **Productivity appears in no
batch.** It needs one, or an explicit decision to drop it.

Needed on a consistent **OECD PPP** basis: Germany `[GAP]`, Italy `[GAP]`, and re-sourcing **Sweden
(~70)** and **Poland (~24.5)**, which came from Statista and are almost certainly not PPP-adjusted on
the same basis — Poland at $24.5 against an OECD PPP average of $67.5 is implausible.

Have on OECD PPP: USA ~97, France 90.86.

## Deferred — housing cost overburden (4 items, only if re-adopted as a secondary metric)

Italy, France and Poland are `[BOUNDED]` 4.0–9.0 — real, honestly derived from Eurostat naming only
countries above 9.0 and below 4.0, but **not values and never to be seeded as such**. Exact figures need
direct `ilc_lvho07a` database access; summary articles cannot produce them.

Plus the **USA methodology decision**: Eurostat measures >40% of disposable income, US convention >30% or
>50%. Nothing matches. Options: import with the bias documented, mark USA `[GAP]` and seed the EU five
only, or use homeownership for the USA (65.3 exists and is comparable).

## Not gaps — do not spend time on these

- **House Price Index** — resolved by convention: seed all six at index 100 and let divergence emerge.
- **Tier 0 derived stats** — computed from tracked values, no seeds needed.

---

# D. EXTERNAL DELIVERIES — 1 item

**`icon_stat_interestrate`** from Claude Design. Missing from both the manifest and the 42-asset delivery.
Interest rate is one of the 18 stats reachable on a policy screen, has its own policy node
(`InterestRateDecision`), drives the Taylor Rule, and headlines the Fed and Eurozone screens.

*The other 41 assets are delivered and imported — currently unwired, ready for B2's rendering.*

---

# Summary

| Category | Count | Unblocks |
|---|---|---|
| ~~**Decisions**~~ | ~~5~~ **0 — all answered `8291662`** | — |
| **Visual reviews** | **10** | Master Sequence step 5 closure |
| **Figures** | 17 blocking + 4 deferred | Steps C1, C2, C3, C5 |
| **External** | 1 | Cosmetic only |

**Fastest path to unblocking the most work, as of 2026-08-02:** the **seven zero-advancement visual
reviews** (1–6 and 10) — they need nothing but Play mode, and item 10 doubles as a correctness check on
the Policy Web's edge list. Then items 7–9 in one sitting, which closes Master Sequence step 5. The
C-step figures remain the long pole and the only items needing database access rather than judgement.

*Decisions are no longer on the critical path. A3 freed B2 rendering, which is now built, wired and
awaiting only review item 10; A1's counting shim is scoped but deliberately unbuilt until Master
Sequence item 8.*
