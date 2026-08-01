# PoliSim — Real-World Seed Data: Macro Data & Release Calendar Overhaul

**Why this file exists:** Claude Code has no web search. Every real-world figure below was sourced externally and is provided here so the overhaul can be grounded in real data rather than invented numbers. This project's standing discipline is "ground new mechanics in real data, label anything stylized honestly" — this file is what makes that possible for this step.

**How to read the confidence markers:**
- `[VERIFIED]` — sourced directly, use as-is
- `[GAP]` — not yet sourced, must NOT be invented; flag to Elias for sourcing before that stat ships
- `[DERIVE]` — should be computed from existing tracked state, not seeded as an independent variable

---

## PART 1 — Release schedules (rule-based, implement as rules, not fixed dates)

### United States [VERIFIED]
| Stat | Rule |
|---|---|
| Unemployment / jobs | First Friday of each month (BLS Employment Situation), 8:30 AM ET |
| Inflation (CPI) | Mid-month, ~12th (BLS) |
| GDP — advance estimate | ~30 days after quarter end (BEA) |
| GDP — second estimate | ~t+60 after quarter end |
| GDP — third estimate | ~t+90 after quarter end |

### EU — Germany, France, Italy, Poland, Sweden [VERIFIED]
| Stat | Rule |
|---|---|
| Inflation — flash estimate | Last working day of the reference month (Eurostat HICP flash) |
| Inflation — full figures | 15–18 days after reference month end (~17th) |
| GDP — preliminary flash | 30 days after quarter end |
| GDP — t+45 flash | 45 days after quarter end |
| GDP — regular estimates | ~t+65 and ~t+110 |

### Already established elsewhere in this project (do not re-derive)
- Fiscal year start: USA October 1; all five European countries January 1.
- Central bank rate decisions: ~8 scheduled meetings per year (Fed/ECB pattern).
- Annual-cadence stats (poverty, population, demographics, crime, infrastructure): publish once per year.

**The revision mechanic (Elias confirmed IN SCOPE):** real agencies publish a preliminary figure that is later revised (BEA advance→second→third; Eurostat flash→final). The player should sometimes act on a figure that later turns out to have been wrong. Model preliminary and revised values as distinct published entries for the same reference period.

---

## PART 2 — Seed data for the seven new tracked stats

### 1. Housing — homeownership rate (%)
| Country | Value | Confidence |
|---|---|---|
| USA | 65.3–65.9 | [VERIFIED] OECD / 2025 figures |
| France | 58.5 | [VERIFIED] OECD |
| Germany | ~47 (46.7 in 2022; Eurostat nationals-only 52.3 in 2024) | [VERIFIED] — lowest in Europe, a genuine outlier: strong rental culture, high rent control, no mortgage interest deduction |
| Poland | ~87 | [VERIFIED] directionally — top-10 globally, post-communist privatization legacy |
| Italy | [GAP] typically ~72–73, needs sourcing | [GAP] |
| Sweden | [GAP] ~63–65 range, needs sourcing | [GAP] |

OECD average: 70.1% [VERIFIED]. Germany being far below every peer is real and worth preserving — it makes housing policy play differently there.

**House Price Index:** [GAP] — no per-country figures sourced yet. Recommend seeding all six at an index value of 100 at game start (a standard index convention) and letting divergence emerge from simulation, rather than inventing differing starting levels. This is honest and avoids fake precision.

### 2. Inequality — Gini coefficient
| Country | Value | Confidence |
|---|---|---|
| Italy | 32.2 | [VERIFIED] Eurostat 2024, equivalised disposable income |
| France | 30.0 | [VERIFIED] Eurostat 2024 |
| Germany | 29.5 | [VERIFIED] Eurostat 2024 |
| Sweden | 27.6 | [VERIFIED] Eurostat 2024 |
| Poland | ~29 | [PARTIAL] Statista 2024 (0.29 on 0–1 scale) — plausible against the EU average of 29.4, but not sourced directly from Eurostat like the four above; prefer an Eurostat figure if one can be obtained |
| USA | ~0.39–0.40 (i.e. ~39–40 on the same 0–100 scale) | [VERIFIED] directionally — OECD reports the US as having the highest income inequality among major developed nations |

**METHODOLOGY WARNING:** the Eurostat figures are equivalised disposable income on a 0–100 scale. US figures commonly appear on a 0–1 scale and from a different source (OECD/World Bank) with different methodology. Normalize to one scale and document which, or the US will look artificially different for measurement reasons rather than real ones.

EU average: 29.4; Euro area: 29.9 (2024) [VERIFIED] — useful sanity anchors.

### 3. Youth unemployment rate (%, ages 15–24, share of labour force)
| Country | Value | Confidence |
|---|---|---|
| Italy | 20.1 | [VERIFIED] June 2025 |
| France | 18.7 | [VERIFIED] June 2025 |
| Germany | [GAP] — known to be low, among the smallest youth/adult gaps in the OECD | [GAP] |
| Poland | [GAP] | [GAP] |
| Sweden | [GAP] — reported among the higher Nordic rates despite a strong labour market | [GAP] |
| USA | [GAP] — OECD-wide youth rate was 11.2% (July 2025) as an anchor | [GAP] |

EU average 14.8%, euro area 14.4% (Sept 2025) [VERIFIED].

**Sweden: 22.2%** (Feb 2026, Eurostat) [VERIFIED] — genuinely high, confirming the "Nordic mixed picture" note; Sweden has averaged 16.95% since 1983, with an all-time high of 29.9% (July 2020). This is a real and counterintuitive feature worth preserving: a strong overall labour market alongside one of Europe's worst youth unemployment rates.

**CRITICAL METHODOLOGY WARNING:** youth unemployment *rate* (% of the youth labour force) and youth unemployment *ratio* (% of the youth population) are different measures and are frequently confused in published tables. Germany 3.6 and Poland 3.5 figures encountered during sourcing are **ratios, not rates** — do not mix them with the rate figures above. Use rate consistently.

---

## BONUS: additional stats found during sourcing (not requested, but genuinely useful)

These weren't part of the seven, but came up with real per-country data and are worth considering — especially the first, which is a strong candidate for the housing stat itself:

**Housing cost overburden rate (%, share of population in households spending >40% of disposable income on housing) [VERIFIED, Eurostat 2024, indicator ilc_lvho07a]:**

| Country | Whole-population rate | Confidence |
|---|---|---|
| Germany | 12.0 | [VERIFIED] |
| Sweden | 10.6 | [VERIFIED] |
| Italy | [BOUNDED] between 4.0 and 9.0 — see derivation below | [PARTIAL] |
| France | [BOUNDED] between 4.0 and 9.0 | [PARTIAL] |
| Poland | [BOUNDED] between 4.0 and 9.0 | [PARTIAL] |
| USA | [GAP] — see methodology warning below; not a simple lookup | [GAP] |

**Derivation of the 4.0–9.0 bound:** Eurostat's 2024 article names exactly five countries above 9.0% (Greece 28.9, Denmark 14.6, Germany 12.0, Sweden 10.6, Czechia 9.2) and three below 4.0% (Cyprus 2.4, Croatia 3.7, Slovenia 3.8). Italy, France and Poland appear in neither list, so each sits between those thresholds. This is a real constraint honestly derived from published data — not a guess — but it is not a precise value and must not be recorded as one.

**Attempts to close these three gaps by search failed.** Eurostat's own summary article only names the extremes, and every alternative source returns a DIFFERENT VARIANT of the indicator rather than the headline figure (cities, tenant-at-market-price, below-60%-of-median, two adults, 18–64 years). Anyone with database access can pull the exact values from Eurostat `ilc_lvho07a` directly; they are not obtainable from summary articles.

EU average 8.2%. Range anchors: Greece highest at 28.9%, then Denmark 14.6%, Germany 12.0, Sweden 10.6, Czechia 9.2; lowest are Cyprus 2.4, Croatia 3.7, Slovenia 3.8.

**⚠ CORRECTION — an earlier version of this file recorded the WRONG VARIANT.** The figures previously listed here (Germany 9.7, Poland 6.1, Sweden 5.1, France 3.9) are the **"Two adults" household-type subset**, not the headline whole-population indicator. The difference is large: Sweden is 5.1 on the two-adults measure versus **10.6** whole-population — more than 2x. Germany 9.7 versus 12.0. **Use the whole-population figures above.** This is the same trap already flagged in this file for youth unemployment rate-vs-ratio, and it was walked into anyway.

**This indicator is unusually variant-prone — treat any figure for it as suspect until the variant is confirmed.** Eurostat publishes at least eight variants under the same name: whole population, two adults, 18–64 years, 65+, cities, rural areas, tenant at market price, tenant at reduced price, owner with mortgage, owner without mortgage, and by income quintile. Sweden alone reads 5.1 / 10.6 / 10.8 / 17.9 depending on which you pull.

Secondary sources compound this. Visual Capitalist, explicitly citing Eurostat 2024, publishes Denmark at 22.7% and Norway at 21.0% — against Eurostat's own 14.6% for Denmark. A reputable outlet, correct attribution, different variant, no label.

**Practical rule: record WHICH variant alongside every value, never just the number.** A bare figure for this indicator carries no meaning.

**USA methodology warning (this is a DECISION, not a lookup):** Eurostat measures >40% of disposable income; US sources conventionally measure >30% ("cost-burdened") or >50% ("severely cost-burdened"). No US figure is directly comparable. Three options, none free:
1. Import a US figure with the bias documented (same approach the file already takes for Gini)
2. Mark USA `[GAP]` and seed only the five EU countries
3. Use homeownership rate for USA instead, where a genuinely comparable figure exists (65.3%)

**Related affordability indicator [VERIFIED, Eurostat 2024]:** average share of disposable income spent on housing — EU 19%, Greece 36%, Denmark 26%, Sweden and Germany both 25%, Cyprus 11% (lowest). A softer, more complete measure than the overburden threshold, and available for more countries.

**Homeownership, EU-wide [VERIFIED, Eurostat 2024]:** 68.4% of people in the EU live in owner-occupied dwellings (44.2% outright, 24.3% with a mortgage), 31.6% rent. Poland nationals 87.9%.

**Long-term unemployment rate (%, of active population 15–74) [VERIFIED, Eurostat 2024]:** Greece 5.4, Spain 3.8, Italy 3.3, Portugal 2.4, Sweden 1.7, Austria 1.1, Poland 0.8, Denmark 0.8. A useful complement to headline unemployment — Italy's structural problem looks very different from Poland's.

### 4. Life expectancy at birth (years)
| Country | Value | Confidence |
|---|---|---|
| USA | 79.0 (2024, highest-ever; up from 78.4 in 2023) | [VERIFIED] CDC NCHS |
| Italy | 84.1 | [VERIFIED] Eurostat 2024 — joint highest in EU |
| Sweden | 84.1 | [VERIFIED] Eurostat 2024 — joint highest in EU |
| France | [GAP] — Eurostat notes a slight +0.1yr increase in 2024; typically ~83 | [GAP] |
| Germany | [GAP] — typically ~81 | [GAP] |
| Poland | [GAP] — typically ~78 | [GAP] |

EU average: 81.7 (2024) [VERIFIED]. The US sitting ~3.7 years below comparable countries is real and worth preserving.

### 5. Real wage growth (%, 2024)
| Country | Value | Confidence |
|---|---|---|
| Poland | 9.0 | [VERIFIED] EU DG EMPL — among the EU's biggest increases, alongside Romania (10.2) and Hungary (8.7); driven by strong nominal growth (12.3%) plus rapidly falling inflation |
| Italy | 2.7 | [VERIFIED] OECD Taxing Wages 2025 — highest among Europe's five largest economies |
| Germany | 2.2 | [VERIFIED] OECD Taxing Wages 2025 |
| France | 0.7 | [VERIFIED] OECD Taxing Wages 2025 — lowest among the major economies |
| Sweden | [GAP] — nominal wage growth was in the 3–5% band in 2024; real figure not directly sourced | [GAP] |
| USA | [GAP] — OECD describes US real wage growth as "stable"; real household income per capita +0.3% in Q4 2024 | [GAP] |

Useful anchors: OECD average real household income per capita growth 1.8% in 2024 [VERIFIED]. Note Germany and Italy both saw *declining* real household income in 2024 even while real wages rose — wages and household income are different measures; don't conflate them.

### 6. Productivity — GDP per hour worked (USD, PPP)
| Country | Value | Confidence |
|---|---|---|
| USA | ~97 | [VERIFIED] OECD — above average, but behind several smaller high-income economies |
| France | 90.86 (2024) | [VERIFIED] OECD |
| Germany | [GAP] | [GAP] |
| Italy | [GAP] — OECD notes Italian productivity growth stagnated 2012–2022 | [GAP] |
| Sweden | ~70 (2024, Statista) — see warning below | [PARTIAL] |
| Poland | ~24.5 (2024, Statista) — see warning below; OECD separately notes substantial productivity *gains* 2012–2022 from a lower base, alongside some of the OECD's longest working hours | [PARTIAL] |

**SOURCE-CONFLICT WARNING on Sweden/Poland:** those two figures come from a different source (Statista) than the USA/France figures (OECD PPP) and are almost certainly NOT PPP-adjusted on the same basis — Poland at $24.5 is implausibly low against an OECD PPP average of $67.5. Do not mix them into one table as-is. Either source all six from OECD PPP consistently, or treat these two as placeholders needing replacement.

OECD average: ~$67.5/hour (2022) [VERIFIED]. Ireland tops the ranking at ~$151 but is heavily distorted by multinational accounting — a good example of why raw cross-country comparison misleads.

**METHODOLOGY WARNING:** the OECD explicitly cautions against comparing GDP per hour worked across countries at face value, since there is still no uniform measurement method; it considers longitudinal comparison (a country against its own past) the valid use. For this game that's actually convenient — seed each country's own level, then let the player watch their own trajectory rather than treating cross-country rank as meaningful.

Also relevant: euro-area labour productivity *fell* 0.9% in 2023, the steepest drop since 2009, against a modest +0.6% OECD average — a real and widening euro-area/US divergence worth preserving in how these seeds trend.

### 7. Credit rating — [DERIVE, do not seed as an independent variable]
A sovereign credit rating is a *judgment about fiscal position*, not an independent economic variable. Agencies derive it from debt-to-GDP, deficit trajectory, and growth — all already tracked in this project. Implement as a derived function of existing state (mapping to a standard AAA/AA+/AA/… ladder), NOT as a mean-reverting variable with its own seed. This makes it cheaper to build and impossible to desync from the fiscal reality it describes.

Real-world anchors [VERIFIED], which form a natural calibration curve for that mapping:

| Country | Debt-to-GDP (this game's seed) | Real rating |
|---|---|---|
| Sweden | ~35% | AAA |
| Germany | ~63% | AAA |
| France | ~116% | AA−/Negative (S&P, mid-2025) |
| USA | ~124% | AA+ (S&P since 2011; Fitch downgraded 2023; Moody's held AAA longest) |
| Italy | ~138% | BBB+/Stable (S&P, mid-2025) |
| Poland | [GAP] — typically in the A range; S&P covers it under CEE sovereigns but no figure sourced | [GAP] |

**KEY INSIGHT for the mapping:** the curve is nearly monotonic in debt-to-GDP — *except the USA*, which carries HIGHER debt than France yet rates BETTER. That's the reserve-currency premium. This project already models exactly that effect (`BaseDebtInterestRateOverride` = 3.3% and reduced `RiskPremiumSensitivity` for USA). The rating derivation should reuse that SAME reserve-currency factor rather than introducing a second, parallel notion of it.

Also worth modeling: France carries a *negative outlook* while southern European sovereigns are stable — outlook is a real signal distinct from the rating itself, and a cheap way to telegraph a downgrade before it lands.

---

## PART 3 — Tier 0 derived stats (no seeding needed, zero simulation risk)

Computed at display time from already-tracked values. No new state, no new ceilings, no validation risk beyond arithmetic correctness:

- GDP per capita = GDP ÷ Population (both already tracked)
- Tax burden as % of GDP
- Spending as % of GDP
- Deficit as % of GDP
- Real GDP growth
- Sector shares of GDP

---

## PART 4 — Standing warnings for whoever implements this

1. **The StatTile large-number bug precedent.** GDP once displayed as "9,3" instead of ~29000 after a purely visual change. Any number formatting/abbreviation work must be verified against real values at multiple magnitudes. A display change must never alter what a number means.

2. **Don't let published values leak into the simulation.** The player-facing UI reads the published (lagged, possibly-revised) series. Internal systems — Okun's Law, the Phillips Curve, the Fiscal Reaction Function — must keep reading live values. Verify this explicitly; it is the main correctness risk of the whole release-calendar change.

3. **Every new stat that nudges an existing tracked variable must fold into that variable's existing combined ceiling**, audited first, per standing rule 11. `PotentialGrowthRate` and `LaborForceParticipationRate` are both already heavily stacked.

4. **Gaps are gaps.** Every `[GAP]` above must be sourced by Elias (or another web-search-capable session) before the stat it belongs to ships. Do not fill them with plausible-looking invented numbers — that would violate this project's core data-honesty rule and would be very hard to detect later.
