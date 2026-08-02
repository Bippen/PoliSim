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

### 1. Housing — homeownership rate (% of HOUSEHOLDS owning, OECD Affordable Housing Database basis)

**Use this basis only.** The previous version of this row mixed three incompatible bases and was not a usable set — see the warning below.

| Country | Value | Confidence |
|---|---|---|
| USA | 65.3 | [VERIFIED] OECD |
| France | 58.5 | [VERIFIED] OECD |
| Germany | 41.0 | [VERIFIED] OECD — lowest among major economies, a genuine structural outlier |
| Poland | [GAP on this basis] — confirmed in the global top 10 alongside Lithuania, Bulgaria and Latvia (post-communist privatization legacy), but no exact OECD figure sourced | [PARTIAL] |
| Italy | [GAP on this basis] | [GAP] |
| Sweden | [GAP on this basis] | [GAP] |

Anchors on the same basis [VERIFIED]: OECD average 70.1, Slovakia highest at 93.5, Canada 68.6, Australia 62.7, Switzerland lowest at 38.2. OECD-wide, 71% of households owned outright or with a mortgage in 2022 versus 24% renting.

**⚠ THREE-WAY BASIS SPREAD — this is why the earlier set was unusable.** Germany appears in sources as:
- **41.0%** — OECD, share of *households* owning
- **~46.7%** — a 2022 dwelling-based figure
- **52.3%** — Eurostat, *nationals only*

An 11.3-point spread across three definitions of "the German homeownership rate," every one correct for its own source. A set mixing these would encode differences that are measurement artifacts rather than real. Eurostat additionally measures share of *population* in owner-occupied dwellings (68.4% EU 2024), a fourth basis again.

USA and France above independently match figures already recorded as OECD-sourced, confirming the basis is coherent — Germany was simply captured from a different one.

**Poland caution:** the ~87.9% figure elsewhere in this file is a Eurostat *nationals* line, not the OECD household basis. Directionally right (Poland genuinely is among the highest globally) but not a same-basis value.

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
| Sweden | **22.2** | [VERIFIED] Eurostat Feb 2026, 15–24 rate — see the note below |
| USA | [GAP] — OECD-wide youth rate was 11.2% (July 2025) as an anchor | [GAP] |

EU average 14.8%, euro area 14.4% (Sept 2025) [VERIFIED].

**Sweden: 22.2%** (Feb 2026, Eurostat, 15–24 rate) [VERIFIED] — genuinely high, confirming the "Nordic mixed picture" note; Sweden has averaged 16.95% since 1983, with an all-time high of 29.9% (July 2020). This is a real and counterintuitive feature worth preserving: a strong overall labour market alongside one of Europe's worst youth unemployment rates.

**✅ RE-CHECKED AND CONFIRMED (2026-08-01):** Italy 20.1 and France 18.7 are genuine RATES on the 15–24 basis, not ratios. Independently confirmed for June 2025 against an EU average of 14.8%. Eurostat's own definition, worth quoting: *the youth unemployment rate is the number of people aged 15 to 24 unemployed as a percentage of the labour force of the same age, and should not be interpreted as the share of jobless people in the overall youth population.* These seeds are sound.

Additional 15–24 rate anchors from the same June 2025 dataset [VERIFIED]: Estonia 26.9 (highest), Spain 24.0, Italy 20.1, Portugal 18.9, Greece 18.8, Malta 6.2 (lowest).

**⚠ SECOND VARIANT AXIS FOUND — AGE BRACKET.** The existing rate-vs-ratio warning below is necessary but NOT sufficient. Eurostat publishes both 15–24 and 15–29 series, giving a 2×2 matrix of four variants:

| Variant | EU 2025 value |
|---|---|
| 15–24 rate (standard) | 14.8% |
| 15–29 rate | 11.7% |
| 15–29 ratio | 6.3% |
| 15–24 ratio | (lower still) |

Sweden sits directly on this fault line: **22.2% is the 15–24 rate; 12.2% is the 15–29 ratio.** Both are real Eurostat figures, both correctly attributed, measuring different things, differing by nearly 2x. Any youth unemployment figure must record BOTH its age bracket AND rate-vs-ratio, or it is meaningless.

**CRITICAL METHODOLOGY WARNING (rate vs ratio):** youth unemployment *rate* (% of the youth labour force) and youth unemployment *ratio* (% of the youth population) are different measures and are frequently confused in published tables. Germany 3.6 and Poland 3.5 figures encountered during sourcing are **ratios, not rates** — do not mix them with the rate figures above. Use the 15–24 rate consistently.

Related useful figure [VERIFIED, 2025]: the 15–29 ratio ranged from 2.9% (Bulgaria, Czechia) to 12.2% (Sweden) across the EU.

---

## BONUS: additional stats found during sourcing (not requested, but genuinely useful)

These weren't part of the seven, but came up with real per-country data and are worth considering — especially the first, which is a strong candidate for the housing stat itself:

**Housing cost overburden rate (%, share of population in households spending >40% of disposable income on housing) [VERIFIED, Eurostat 2024, indicator ilc_lvho07a]:**

| Country | Whole-population rate | Confidence |
|---|---|---|
| Germany | 12.0 | [VERIFIED] |
| Sweden | 10.6 | [VERIFIED] |
| Italy | 5.1 | [VERIFIED] — Eurostat API, 2026-08-02 |
| France | 7.0 | [VERIFIED] — Eurostat API, 2026-08-02 |
| Poland | 5.2 | [VERIFIED] — Eurostat API, 2026-08-02 |
| USA | [GAP] — see methodology warning below; not a simple lookup | [GAP] |

**✅ THE THREE [BOUNDED] GAPS ARE CLOSED (2026-08-02), pulled directly from the Eurostat API.**

Query, identical for all three apart from `geo` — **every dimension stated explicitly**, which is what
makes the result unambiguous rather than merely correct:

```
https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/ilc_lvho07a
    ?lang=EN&unit=PC&rskpovth=TOTAL&age=TOTAL&sex=T&geo=IT&geo=FR&geo=PL&time=2024
```

| Dimension | Code | Returned label |
|---|---|---|
| `unit` | `PC` | Percentage |
| `rskpovth` | `TOTAL` | Total |
| `age` | `TOTAL` | Total |
| `sex` | `T` | Total |
| `time` | `2024` | 2024 |

**Status flags: none on any of the three.** Dataset last updated 2026-06-08.

**Two controls ran before these were trusted**, and both are why they read `[VERIFIED]` and not `[PARTIAL]`:

1. **Cross-check gate.** The same query shape reproduced Germany at exactly **12.0** — already
   `[VERIFIED]` here — *before* any new value was pulled. A query that cannot reproduce a known value is
   a broken query, and everything from it is of unknown basis.
2. **Decode test.** The trio came from a MULTI-geo query, where values arrive as a flat array and must be
   mapped back through the returned index — a step the single-country gate never exercised. The query was
   re-run with Germany and Sweden included: both landed on **12.0** and **10.6** at their own positions,
   proving the decode instead of assuming it. *A correct value in the wrong position is indistinguishable
   from a correct answer.*

### ✅ THE [BOUNDED] DERIVATION METHOD HELD — all three landed inside 4.0–9.0

**The original derivation:** Eurostat's 2024 article named exactly five countries above 9.0% (Greece 28.9,
Denmark 14.6, Germany 12.0, Sweden 10.6, Czechia 9.2) and three below 4.0% (Cyprus 2.4, Croatia 3.7,
Slovenia 3.8). Italy, France and Poland appeared in neither list, so each had to sit between them.

**France 7.0, Poland 5.2, Italy 5.1 — every one inside the bound.** This matters well beyond three
numbers. The derivation rested on an assumption it could not check: that the article's extremes lists were
COMPLETE. A single result outside 4.0–9.0 would have invalidated that reasoning everywhere it was used,
not merely this bound. It holds. **Bounding from published extremes is a sound technique for this file**,
now confirmed against ground truth rather than trusted — and it was testable precisely because the bound
was honest about being a range instead of being written down as a point estimate.

EU average 8.2%. Range anchors: Greece highest at 28.9%, then Denmark 14.6%, Germany 12.0, Sweden 10.6, Czechia 9.2; lowest are Cyprus 2.4, Croatia 3.7, Slovenia 3.8.

**⚠ CORRECTION — an earlier version of this file recorded the WRONG VARIANT.** The figures previously listed here (Germany 9.7, Poland 6.1, Sweden 5.1, France 3.9) are a different cut of the indicator, not the headline whole-population one. The difference is large: Sweden 5.1 versus **10.6** whole-population — more than 2x. Germany 9.7 versus 12.0. **Use the whole-population figures above.** Same trap this file already flagged for youth unemployment rate-vs-ratio, walked into anyway.

**⚠⚠ CORRECTION TO THE CORRECTION (2026-08-02, from the API's own structure).** The paragraph above used
to attribute those wrong figures to a **"two adults" household-type subset of `ilc_lvho07a`**. That
explanation is wrong, and wrong in a way that wastes the time of anyone acting on it: **`ilc_lvho07a` has
no household-type dimension at all.** Its real structure, read from the API:

> **`ilc_lvho07a` — "Housing cost overburden rate by age, sex and poverty status"**
> Dimensions: `freq` · `unit` · **`rskpovth`** · **`age`** · **`sex`** · `geo` · `time`
> For Germany 2024 alone this is **153 values** — 3 poverty thresholds × 17 age brackets × 3 sexes.

So the 9.7 figure came from a **different dataset code**, not a different filter on this one. The
correction's conclusion stands (12.0 right, 9.7 wrong); only its stated mechanism was invented. Someone
following the old note would go looking for a household-type dimension that does not exist, conclude the
warning was stale, and trust the wrong number.

**🔴 `rskpovth=B_60` IS THE MOST DANGEROUS VARIANT, and it was not previously named.** The poverty-threshold
dimension splits into `TOTAL`, `A_60` (above 60% of median income) and `B_60` (below it). Overburden among
the below-60% population runs FAR above the whole-population rate — it is the same indicator restricted to
those least able to afford housing. A `B_60` figure is a real Eurostat number, correctly attributed, and
several times too high. **It would read as entirely plausible in this table**, which is exactly what makes
it worse than an obviously broken value.

The full age dimension, for reference, since several of these are the variants the old note gestured at:
`TOTAL`, `Y_LT6`, `Y6-11`, `Y12-17`, `Y15-19`, `Y15-24`, `Y15-29`, `Y16-19`, `Y16-24`, `Y16-29`, `Y_LT18`,
`Y18-24`, `Y18-64`, `Y20-24`, `Y20-29`, `Y25-29`, `Y_GE65`.

**This indicator is unusually variant-prone — treat any figure for it as suspect until the variant is confirmed.** Beyond `ilc_lvho07a`'s own 153-cell grid, Eurostat publishes further cuts under the same NAME in other datasets: by household type, by tenure status (tenant at market price, tenant at reduced price, owner with mortgage, owner without), by degree of urbanisation (cities, towns, rural), and by income quintile. Sweden alone reads 5.1 / 10.6 / 10.8 / 17.9 depending which you pull.

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

---

### 5. 🔴 API SOURCING RULES (added 2026-08-02, when the Eurostat API turned out to be reachable)

**The variant problem is WORSE through an API than through an article, not better.** An article tells you
what it describes — "housing cost overburden, whole population, 2024" — in words that can contradict a
misreading. An API returns whatever dimensions you filtered on, as a bare number, with no prose to argue
with. Every safeguard below exists because the usual signal that something is wrong has been removed.

**a. Confirm the dataset's real structure before pulling from it. Never pattern-match a plausible code.**
`ilc_lvho07a` was assumed for a year to have a household-type dimension. It does not — it is
`rskpovth` × `age` × `sex`. A code that looks right and returns a number is the failure mode here.

**b. State EVERY dimension explicitly in every query.** Not just the ones you care about.

**c. PREVENTION AND DETECTION ARE DIFFERENT CONTROLS, and only one of them scales.**
- *Verifying returned labels* is **detection**: the response echoes each dimension's code and its label,
  so a query that asked for the wrong thing can be caught by reading what came back.
- *Stating every dimension* is **prevention**: it makes the query unambiguous in the first place.
- **Detection does not cover an omitted dimension.** Leave `rskpovth` out and the API returns three
  values — one per threshold — each correctly labelled. Nothing is *wrong* to catch. You simply pick one,
  and if you pick `B_60` you have recorded a real, correctly-attributed, several-times-too-high number.
  **The label check cannot save you, because no label is incorrect.** Only stating the dimension can.

**d. Record the full query URL alongside the value**, plus every dimension code AND its returned label.
A value without its query is unreproducible, and this file's whole method is reproducibility.

**e. 🔴 STATUS FLAGS ARE DATA, NOT DECORATION — and a flagged figure is NEVER silently `[VERIFIED]`.**
JSON-stat responses carry a per-observation `status`. The very first test query returned **`bep`** —
*"break in time series, estimated, provisional"*. A figure with a break flag is **not on the same basis as
the figure before it**, which is precisely the class of divergence this file's `[PARTIAL]` markers exist
to record. Record the flag next to the value; let it downgrade the confidence marker. `e` (estimated),
`p` (provisional), `b` (break), `d` (definition differs), `u` (low reliability) and their combinations all
mean the number needs a caveat, not a promotion.

**f. Cross-check against a known-verified value BEFORE sourcing anything new, and treat it as a GATE.**
If a query cannot reproduce a figure this file already verified, **the query is wrong — not the file** —
and everything from it is of unknown basis. Stop and report rather than proceeding. This ran for real on
2026-08-02: Germany's 12.0 was reproduced exactly before the Italy/France/Poland values were pulled.

**g. 🔴 WHEN THE QUERY SHAPE CHANGES, RE-RUN THE GATE IN THE NEW SHAPE.** A gate that passed in one shape
says nothing whatever about another. Each of these is a shape change and each needs its own anchored
re-check:

| From | To | The step that is newly unexercised |
|---|---|---|
| single-geo | multi-geo | values arrive as a flat array, mapped back through the returned index |
| single-period | time series | the same, along the `time` axis |
| one dimension filtered | several | interaction of filters, and which dimension varies in the array |

**Why this is a rule and not a nicety.** The single-country gate reproduced Germany's 12.0 perfectly and
proved the *variant* was right — but it never touched index decoding, because with one country there is
only position 0. The trio then arrived through a multi-geo query whose mapping step nothing had tested.
**A correct value in the wrong position is invisible to every other control in this process**: the
dimension labels are all correct, the status flags are all clean, the numbers are all real Eurostat
figures. Only an anchor landing on its own known value can catch it.

**Cheapest form: carry the anchor inside the real query.** Add a known-value country to the same call
rather than running a separate verification pass — the position check then rides along for free, on
exactly the query whose results you intend to keep, instead of on a proxy for it.
