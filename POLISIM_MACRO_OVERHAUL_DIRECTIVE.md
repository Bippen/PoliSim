# PoliSim — Macro Data & Release Calendar Overhaul (Steps A–D)

**Companion file:** `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` contains every real-world figure this work depends on, with explicit `[VERIFIED]` / `[PARTIAL]` / `[GAP]` markers. Claude Code has no web search — do not invent any figure marked `[GAP]`, and do not silently merge figures flagged with a source conflict.

**Why four steps instead of one:** seven new tracked stats + a revision mechanic + per-period lag tracking + six countries publishing independently is the largest single change ever attempted on this project — larger than Demographics (5 fields, which needed two structural bug fixes and three correction rounds) and larger than the Parliament gating pilot. Every prior successful split on this project followed the same principle: prove the risky foundation alone, then layer on top of it.

**Decisions already confirmed by Elias:** realistic reporting lag (published value reflects the period it covers, not the publication moment); preliminary→revised figures modeled; all six countries get real release calendars; seven new stats (housing, inequality, real wages, productivity, youth unemployment, life expectancy, credit rating).

---

## STEP A — Release calendar, revisions, and Tier 0 derived stats
*The risky foundation. No new tracked variables at all.*

**A1. Release calendar (rule-based, per country)**
Implement the schedules from the seed data file as rules, not hardcoded dates:
- USA: unemployment first Friday monthly; CPI mid-month (~12th); GDP advance ~t+30 after quarter end, second ~t+60, third ~t+90.
- EU five: inflation flash on the last working day of the reference month; full inflation t+15–18; GDP preliminary flash t+30, t+45 flash, regular estimates ~t+65 and ~t+110.
- Annual-cadence stats (poverty, population, demographics, crime, infrastructure) publish once yearly.
- Central bank rate decisions: ~8 scheduled meetings/year (already established elsewhere).

**A2. Published-series data model**
Each tracked stat gains a published series distinct from its live simulation value. Each published entry carries: reference period, publication date, value, and revision status (preliminary / revised / final).

**A3. Revision mechanic**
Preliminary figures are published first and later revised, per the real BEA advance→second→third and Eurostat flash→final patterns. The player should sometimes act on a figure that later turns out to have been wrong. Revisions should be small and plausible, not arbitrary — derive the revised value from the true underlying simulation value, with the preliminary being a noisy early estimate of it.

**A4. Tier 0 derived stats** (zero simulation risk — pure display arithmetic from already-tracked values)
GDP per capita (GDP ÷ Population), tax burden % GDP, spending % GDP, deficit % GDP, real GDP growth, sector shares of GDP.

**THE CRITICAL CORRECTNESS RISK — verify explicitly, do not assume:**
The player-facing UI reads the PUBLISHED (lagged, possibly-revised) series. Every internal simulation system — Okun's Law, the Phillips Curve, the Fiscal Reaction Function, sector integration, everything — must keep reading LIVE values. If published values leak into the simulation, the model starts consuming its own stale output and the effect may not surface for hundreds of turns. Prove this with a before/after comparison showing identical simulation trajectories pre- and post-change.

**Validation:** full scenario matrix at both horizons, PLUS the identical-trajectory proof above. This step must not change a single simulation number.

---

## STEP B — Graph overhaul and contextual policy-screen stats
*Depends on A. Pure display, lower risk.*

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
Each with its current published value plus a compact sparkline reusing `GraphRenderer`.

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

**Before each batch:** report which `[GAP]` figures that batch needs, so Elias can source them. Do not proceed on invented numbers.

**Validation per batch:** full scenario matrix (these are fiscal-adjacent), plus a dedicated stress scenario per batch.

---

## STEP D — Sprite asset request
*A document, not code — compile early, run in parallel.*

Same rigorous format as `CLAUDE_DESIGN_ASSET_REQUEST_5E.md`: exact filenames derived from real enum values, exact dimensions, format spec, and the corrected `.meta` import settings already worked out for the chrome pack (nPOTScale, alphaIsTransparency, no block compression, no mipmaps, Clamp wrapping).

**BE PRECISE ABOUT WHAT ACTUALLY NEEDS ART.** Graph axes, gridlines, plot lines, threshold lines, bars, and fills are all PROCEDURAL — `GraphRenderer` already draws these and they stay code, per rule 10's data-visualization carve-out. Sprites are needed only for:
- One small icon per tracked stat type (derive the exact list from the real stat enum once Step C's stats exist)
- Trend arrows (up / down / flat), tintable white-on-transparent like the existing chrome pack
- A release-marker/pin sprite for publication points on graphs
- A small badge distinguishing preliminary from revised figures

---

## Sequencing summary

1. **Step A** — foundation, highest risk, must prove it changes no simulation numbers.
2. **Step B** — display layer on top of A.
3. **Step C** — four batches, each independently validated.
4. **Step D** — document, compile anytime; assets land before the icon work in B/C needs them.

Standing rules apply throughout: real Unity validation before anything is "done," one commit per unit of work, escalate genuine design forks to Open Questions rather than deciding silently, never mark a phase done without a live check.
