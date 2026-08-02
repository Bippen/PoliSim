# PoliSim — Macro Data & Release Calendar Overhaul (Steps A–D)

**Companion file:** `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` contains every real-world figure this work depends on, with explicit `[VERIFIED]` / `[PARTIAL]` / `[GAP]` markers. Claude Code has no web search — do not invent any figure marked `[GAP]`, and do not silently merge figures flagged with a source conflict.

**Why four steps instead of one:** seven new tracked stats + a revision mechanic + per-period lag tracking + six countries publishing independently is the largest single change ever attempted on this project — larger than Demographics (5 fields, which needed two structural bug fixes and three correction rounds) and larger than the Parliament gating pilot. Every prior successful split on this project followed the same principle: prove the risky foundation alone, then layer on top of it.

**Decisions already confirmed by Elias:** realistic reporting lag (published value reflects the period it covers, not the publication moment); preliminary→revised figures modeled; all six countries get real release calendars; seven new stats (housing, inequality, real wages, productivity, youth unemployment, life expectancy, credit rating).

---

## STEP A — Release calendar, revisions, and Tier 0 derived stats

**A1–A3: DONE 2026-08-01.** Release calendar, published-series model and the revision mechanic. Full
record in `COMPLETED.md` section 6 — including the one-directional rule (`PublicationSystem` writes
`Country.Published` and reads `Country.State`, never the reverse) and the two real bugs fixed.

**A4 (Tier 0 derived stats): LIVE, not done.** Built (`70798e9`) and trajectory-validated (`3d77b11`),
but it surfaces nothing to the player — and this step defines A4 as *"pure display arithmetic"*. See the
roadmap for the remaining work.

⚠ **The critical correctness risk this step was built around — still binding on everything downstream.**
The player-facing UI reads the PUBLISHED (lagged, possibly-revised) series. Every internal system —
Okun's Law, the Phillips Curve, the Fiscal Reaction Function, sector integration — must keep reading
LIVE values. A leak makes the model consume its own stale output, and the effect may not surface for
hundreds of turns. Retained here rather than consolidated because it governs Step C too.

*Remaining A1–A3 spec text consolidated out 2026-08-02; git history holds it in full.*

---

## STEP B — Graph overhaul and contextual policy-screen stats
*Depends on A. Pure display, lower risk.*

**Status: B1 and B2 both BUILT, neither CONFIRMED.** B1's graph overhaul (`dd7e323`) and B2's
contextual stat row (`5701a04`, wired `4869476`) await visual review items 3 and 10. **Spec retained in
full below** rather than consolidated, because a rejected review sends the work straight back to it.

**B1. Graph overhaul** — extend `GraphRenderer`, do NOT build a parallel system:
- Real calendar date axis instead of turn numbers.
- Visible release-point markers so the player can see when a figure was published versus the period it covers.
- Distinct visual treatment for preliminary vs. revised values (this is the payoff of A3 — the player should be able to *see* a revision happen).
- Selectable time ranges (1yr / 5yr / all).
- Retain existing threshold-line support (NAIRU, comfortable debt) and the direction-aware green/red convention.

**B2. Contextual stats on policy screens**
Show, on each policy screen, the stats that policy actually affects — sourced from the REAL effect relationships already audited for the Policy Web, not invented:
- Tax → revenue, tax burden % GDP, debt trajectory
- Spending → the affected category's outcome stat, deficit, debt
- Welfare → poverty rate, inequality (once added), program cost
- Labor → unemployment, labor force participation, real wages (once added)
- Crime → crime index, incarceration, corruption
- Housing-related → the new housing stats (once added)
Each with its current value plus a compact sparkline reusing `GraphRenderer`.

**CORRECTED 2026-08-01 — use LIVE values here, not published ones.** This directive originally said
"published value". Elias overruled it after the implementation raised the conflict: a lagged, possibly
preliminary figure sitting in a "what am I doing right now" panel misrepresents itself, and the
instruction was only ever partly satisfiable — `PublishedStat` has 6 members against `StatNodeId`'s 18,
so 12 of 18 policy-screen stats have no published series at all. The published, lagged view stays where
it belongs, on the Statistics tab's graphs. Recorded here so the directive is not left contradicting the
code.

**THE CRITICAL BUG PRECEDENT:** the `StatTile` formatting bug displayed GDP as "9,3" instead of ~29000 after a purely visual change. Any number formatting or abbreviation work must be verified against real values at multiple magnitudes before shipping. A display change must never alter what a number means.

**Validation:** single-scenario smoke check plus live-Editor screenshots — including one showing a revision visibly landing on a graph.

---

## STEP C — The seven new tracked stats
*Split into batches. Do NOT build all seven at once.*

Standing requirement for every batch: any effect on an existing tracked variable folds into that variable's existing combined ceiling, audited first (rule 11). `PotentialGrowthRate` and `LaborForceParticipationRate` are both already heavily stacked.

**C1. Housing** (confirmed first — rate-sensitive, so it interacts interestingly with existing levers)
Seed data available for homeownership rate (4 of 6 countries) and housing cost overburden rate (all EU five). **Recommend housing cost overburden rate as the PRIMARY housing metric** rather than homeownership: it measures affordability *stress* rather than tenure, responds directly to interest rates and housing assistance (both already in the game), and has complete EU coverage. Homeownership rate can ride alongside as a slower-moving structural stat.
House Price Index: no per-country seeds sourced — seed all six at index 100 at game start (standard index convention) and let divergence emerge, rather than inventing starting levels.
Germany's ~47% homeownership (lowest in Europe, real cultural/policy outlier) should survive into the model — it makes housing policy genuinely play differently there.

**C2. Inequality + real wages** (both have good seed coverage; naturally related)
Gini: Eurostat 2024 figures for Italy 32.2, France 30.0, Germany 29.5, Sweden 27.6, Poland ~29, USA ~39–40 — **normalize the scale first**, US sources use 0–1 and different methodology from Eurostat's 0–100.
Real wage growth 2024: Poland 9.0, Italy 2.7, Germany 2.2, France 0.7; Sweden and USA are gaps.

**C3. Youth unemployment + life expectancy** (both straightforward, good coverage)
Youth unemployment: Italy 20.1, France 18.7, **Sweden 22.2** — use *rate* consistently, never *ratio* (see the seed file's warning; Germany 3.6 / Poland 3.5 figures found during sourcing are ratios and would badly distort the model).
Life expectancy: USA 79.0, Italy 84.1, Sweden 84.1; France/Germany/Poland are gaps.

**C4. Credit rating** (DERIVED — do not seed as an independent variable)
Compute from existing state (debt-to-GDP, deficit trajectory, growth). Calibrate against the real curve in the seed file: Sweden ~35%→AAA, Germany ~63%→AAA, France ~116%→AA−, USA ~124%→AA+, Italy ~138%→BBB+.
**Reuse the existing reserve-currency mechanism** (`BaseDebtInterestRateOverride`, reduced `RiskPremiumSensitivity`) to explain why the USA rates better than France despite higher debt — do not introduce a second, parallel notion of reserve-currency status.
Consider modeling outlook (stable/negative) as a separate signal — a cheap way to telegraph a downgrade before it lands.

**C5. Productivity — GDP per hour worked** *(added 2026-08-01, correcting an error in this directive)*
This directive named **seven** new stats and then batched only **six**: productivity appeared in no batch at all. That was an authoring error, not a decision to drop it. Elias's ruling: **add it as C5 rather than leave it unassigned.**

**Basis requirement, non-negotiable: OECD PPP, GDP per hour worked, all six countries on that one basis.** The seed file currently mixes sources — USA ~97 and France 90.86 are OECD PPP, but Sweden ~70 and Poland ~24.5 came from Statista and are almost certainly not PPP-adjusted on the same footing (Poland at $24.5 against an OECD PPP average of $67.5 is implausible). Germany and Italy are `[GAP]`. So all four of Germany, Italy, Sweden and Poland need sourcing or re-sourcing before C5 can start.

Note the OECD's own caution that cross-country comparison of this measure is not meaningful — it considers a country against its own past the valid use. That suits this game: seed each country's level, then let the player watch their own trajectory rather than treating rank as meaningful.

**Before each batch:** report which `[GAP]` figures that batch needs, so Elias can source them. Do not proceed on invented numbers.

**Validation per batch:** full scenario matrix (these are fiscal-adjacent), plus a dedicated stress scenario per batch.

---

## STEP D — Sprite asset request

**DONE 2026-08-01.** 42 assets requested, delivered, security-reviewed and imported; wired 2026-08-02.
Record in `COMPLETED.md` section 6.

**One gap, root-caused 2026-08-02:** `icon_stat_interestrate` was never requested, because this step's
"derive the list from the real stat enum" instruction was satisfied against `EconomyState`'s 29 fields —
and `InterestRate` is not one of them. It lives on `CurrencyZone`. **Enumerate the display enum
(`StatNodeId`), not the storage struct.** Now the sole item in `CLAUDE_DESIGN_ASSET_REQUEST.md`.

*Spec consolidated out 2026-08-02; git history holds it in full.*

---

## Sequencing summary

1. ~~**Step A**~~ — A1–A3 done; A4 built but not surfaced.
2. ~~**Step B**~~ — B1 and B2 built; both await visual confirmation.
3. **Step C** — FIVE batches (C5 added 2026-08-01), each independently validated. **C1, C2, C3 and C5 are blocked on figures; C4 is built and blocked on a re-calibration decision** — see `MISSING_PREREQUISITES.md`. This is the only genuinely outstanding step.
4. ~~**Step D**~~ — delivered, imported and wired.

Standing rules apply throughout: real Unity validation before anything is "done," one commit per unit of work, escalate genuine design forks to Open Questions rather than deciding silently, never mark a phase done without a live check.
